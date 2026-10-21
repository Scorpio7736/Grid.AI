using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;

public sealed class CoordinationTests
{
    private readonly TerminalSessionRegistry registry = new();
    private readonly TerminalIdentitySession owner = new(() => "coordination-owner");
    private readonly GridSimulationEngine engine;
    public CoordinationTests()
    {
        registry.Add(owner); engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
        owner.Attach(engine); owner.Login(); engine.InitializeSystemAgents(owner.Reference);
    }
    private Task Save(CancellationToken token) { GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot()); return Task.CompletedTask; }
    private TerminalIdentitySession Agent(string role)
    {
        var agent = owner.CreateAttachment(); registry.Add(agent); agent.Switch(engine.ResolveProgram(role).Id.ToString()); return agent;
    }
    private CoordinationPlan Plan(string request = "MAXIS: demo hello; JN: demo inventory", string key = "request") =>
        engine.Coordinate(owner.Reference, request, key);

    [Fact]
    public void PlanIsAtomicIdempotentAndRequiresPerChildApproval()
    {
        var plan = Plan(); Assert.Equal(plan.Id, Plan().Id);
        Assert.Equal(2, plan.TaskIds.Count); Assert.Single(engine.GetSnapshot().Tasks.Plans);
        Assert.All(engine.GetSnapshot().Tasks.Records, t => { Assert.Equal(Status.AwaitingApproval, t.Status); Assert.Null(t.ApprovedBy); Assert.Equal(0, t.Attempts); });
        Assert.Throws<ArgumentException>(() => Plan("JN: demo changed"));
        var mcp = Agent("MCP");
        Assert.Throws<ArgumentException>(() => engine.Coordinate(mcp.Reference, plan.Request, plan.RequestKey));
        Assert.Equal(2, engine.GetSnapshot().Tasks.Records.Count);
    }
    [Theory]
    [InlineData("MAXIS: demo good; JN: delete files")]
    [InlineData("MAXIS: demo good; MCP: demo recursion")]
    [InlineData("MAXIS: demo good;")]
    [InlineData("1: demo numeric")]
    [InlineData("MAXIS: delay 5001")]
    [InlineData("MAXIS: demo a; JN: demo b; JN: demo c; JN: demo d; JN: demo e; JN: demo f; JN: demo g; JN: demo h; JN: demo i")]
    public void InvalidRequestCreatesNoPartialWork(string request)
    {
        Assert.Throws<ArgumentException>(() => Plan(request));
        Assert.Empty(engine.GetSnapshot().Tasks.Plans); Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task CoordinatorScopeDoesNotGrantApprovalOrUnrelatedTaskAccess()
    {
        var mcp = Agent("MCP"); var jn = Agent("JN");
        var plan = engine.Coordinate(mcp.Reference, "JN: demo delegated; MAXIS: demo other", "mcp");
        var direct = engine.ProposeTask(owner.Reference, jn.CurrentRecipient.Id, "jn.demo", "private", "direct");
        Assert.Equal(2, engine.TasksFor(mcp.Reference).Records.Count);
        Assert.DoesNotContain(engine.TasksFor(mcp.Reference).Records, t => t.Id == direct.Id);
        Assert.Throws<UnauthorizedAccessException>(() => engine.CancelTask(mcp.Reference, direct.Id));
        Assert.Throws<UnauthorizedAccessException>(() => engine.ApproveTask(mcp.Reference, plan.TaskIds[0]));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => engine.RunNextTaskAsync(mcp.Reference, Save, default));
        Assert.Throws<UnauthorizedAccessException>(() => engine.Coordinate(jn.Reference, "MAXIS: demo forbidden", "forbidden"));
        Assert.Throws<UnauthorizedAccessException>(() => engine.CoordinationBrief(jn.Reference));
        Assert.Empty(engine.TasksFor(jn.Reference).Plans);
        Assert.All(engine.TasksFor(jn.Reference).Records, t => Assert.Equal(jn.CurrentRecipient.Id, t.AgentId));
        engine.CancelTask(mcp.Reference, plan.TaskIds[0]);
        Assert.Contains("Cancelled", engine.CoordinationBrief(mcp.Reference, plan.Id));
    }
    [Fact]
    public async Task BriefingUsesActualMixedResultsAndDoesNotClaimPendingSuccess()
    {
        var plan = Plan();
        engine.ApproveTask(owner.Reference, plan.TaskIds[0]); await engine.RunNextTaskAsync(owner.Reference, Save, default);
        var brief = engine.CoordinationBrief(owner.Reference, plan.Id);
        Assert.Contains("Completed: 1", brief); Assert.Contains("AwaitingApproval: 1", brief);
        Assert.Contains("DEMONSTRATION ONLY", brief); Assert.Contains("No result recorded; no completion claimed.", brief);
        Assert.Contains("Human approval required", brief);
        engine.CancelTask(owner.Reference, plan.TaskIds[1]);
        Assert.Contains("Cancelled: 1", engine.CoordinationBrief(owner.Reference, plan.Id));
    }
    [Fact]
    public void SnapshotRejectsOrphansAndDuplicateChildrenAndCopiesLinks()
    {
        var plan = Plan(); var snapshot = engine.GetSnapshot();
        var ids = plan.TaskIds.ToArray();
        var restored = GridSimulationEngine.Restore(snapshot with { Tasks = snapshot.Tasks with { Plans = [plan with { TaskIds = ids }] } });
        ids[0] = Guid.NewGuid(); Assert.Equal(plan.TaskIds[0], restored.GetSnapshot().Tasks.Plans[0].TaskIds[0]);
        Assert.Throws<InvalidDataException>(() => GridSimulationEngine.Restore(snapshot with { Tasks = snapshot.Tasks with { Plans = [plan with { TaskIds = ids }] } }));
        Assert.Throws<InvalidDataException>(() => GridSimulationEngine.Restore(snapshot with { Tasks = snapshot.Tasks with { Plans = [plan with { TaskIds = [plan.TaskIds[0], plan.TaskIds[0]] }] } }));
    }
    [Fact]
    public void SQLiteRetainsPlanAndRestartClearsQueuedConsent()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridCoordination-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var plan = Plan(); engine.ApproveTask(owner.Reference, plan.TaskIds[0]);
            using var store = new WorldStore(Path.Combine(folder, "world.db"));
            store.Create(new WorldInfo(engine.GetSnapshot().WorldId, "Plans", DateTimeOffset.UtcNow), engine.GetSnapshot());
            var restored = GridSimulationEngine.Restore(store.Load().State).GetSnapshot();
            Assert.Equal(plan.TaskIds, Assert.Single(restored.Tasks.Plans).TaskIds);
            Assert.All(restored.Tasks.Records, t => { Assert.Equal(Status.AwaitingApproval, t.Status); Assert.Null(t.ApprovedBy); });
        }
        finally { Directory.Delete(folder, true); }
    }
    [Fact]
    public void VersionFourteenWithoutPlanFieldMigrates()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridPlanUpgrade-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "world.db");
            using var store = new WorldStore(path);
            store.Create(new WorldInfo(engine.GetSnapshot().WorldId, "Old", DateTimeOffset.UtcNow), engine.GetSnapshot());
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
            {
                connection.Open(); using var command = connection.CreateCommand();
                command.CommandText = "UPDATE WorldTasks SET StateJson=json_remove(StateJson,'$.Plans'); DELETE FROM SchemaMigrations WHERE Version=15; PRAGMA user_version=14;";
                command.ExecuteNonQuery();
            }
            var loaded = store.Load().State; Assert.Empty(loaded.Tasks.Plans); Assert.Equal(6, loaded.Tasks.Agents.Count);
        }
        finally { Directory.Delete(folder, true); }
    }
    [Fact]
    public void TerminalRequestSavesAndSupportsPlanInspection()
    {
        var saves = 0; var commands = DefaultCommands.Create(engine);
        TaskCommands.Register(commands, engine, owner, _ => { saves++; return Task.CompletedTask; }); commands.UseIdentity(owner);
        Assert.Contains("AwaitingApproval: 2", commands.Execute("mcp request \"MAXIS: demo hello; JN: demo inventory\" terminal").Text);
        Assert.Equal(1, saves); var plan = Assert.Single(engine.GetSnapshot().Tasks.Plans);
        Assert.Contains(plan.Id.ToString(), commands.Execute("mcp inspect " + plan.Id).Text);
        Assert.Contains("VERIFIED BROKER STATE", commands.Execute("mcp brief").Text);
    }
    [Fact]
    public void ShortReferencesAndCopiedPunctuationResolveWithoutConfusingPlansAndTasks()
    {
        var plan = Plan(); var commands = DefaultCommands.Create(engine);
        TaskCommands.Register(commands, engine, owner, Save); commands.UseIdentity(owner);
        var task = plan.TaskIds[0]; var shortTask = task.ToString()[..8];
        Assert.Contains("MAXIS", commands.Execute("tasks list").Text);
        Assert.DoesNotContain(engine.ResolveProgram("MAXIS").Id.ToString(), commands.Execute("tasks list").Text);
        Assert.Contains("Full ID:     " + task, commands.Execute("tasks inspect " + shortTask + ".").Text);
        Assert.Contains("Queued", commands.Execute("tasks approve " + shortTask).Text);
        Assert.Contains("Unknown plan reference", commands.Execute("mcp inspect " + task).Text);
        Assert.Contains("Full plan ID: " + plan.Id, commands.Execute("mcp inspect " + plan.Id.ToString()[..8]).Text);
        Assert.DoesNotContain("If saving failed", commands.Execute("mcp inspect <plan-id>").Text);
        Assert.DoesNotContain("Full plan ID", commands.Execute("mcp plans").Text);
    }
    [Fact]
    public void ShortReferencesCannotAccessAnotherSpecialistsTask()
    {
        var plan = Plan(); var jn = Agent("JN"); var commands = DefaultCommands.Create(engine);
        TaskCommands.Register(commands, engine, jn, Save); commands.UseIdentity(jn);
        Assert.Contains("Unknown task reference", commands.Execute("tasks approve " + plan.TaskIds[0].ToString()[..8]).Text);
        Assert.Equal(Status.AwaitingApproval, engine.GetSnapshot().Tasks.Records.Single(t => t.Id == plan.TaskIds[0]).Status);
    }
}
