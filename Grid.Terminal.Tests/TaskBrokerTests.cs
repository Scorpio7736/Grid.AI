using Grid.Core;
using Grid.Communication;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;

public sealed class TaskBrokerTests
{
    private readonly TerminalSessionRegistry registry = new();
    private readonly TerminalIdentitySession owner = new(() => "broker-owner");
    private readonly GridSimulationEngine engine;
    private readonly CommandDispatcher commands;
    private int saves;
    public TaskBrokerTests()
    {
        registry.Add(owner); engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); owner.Attach(engine); owner.Login();
        commands = DefaultCommands.Create(engine); TaskCommands.Register(commands, engine, owner, Save); GridTransfer.Register(commands, engine, startAgents: () => engine.StartAgents(owner.Reference)); commands.UseIdentity(owner);
    }
    private Task Save(CancellationToken token) { token.ThrowIfCancellationRequested(); saves++; GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot()); return Task.CompletedTask; }
    private void Initialize() => engine.StartAgents(owner.Reference);
    private TaskRecord Proposal(string role = "MAXIS", string? capability = null, string argument = "test", string? key = null) =>
        engine.ProposeTask(owner.Reference, engine.ResolveProgram(role).Id, capability ?? role.ToLowerInvariant() + ".demo", argument, key ?? Guid.NewGuid().ToString("N"));
    private TaskRecord Record(Guid id) => engine.GetSnapshot().Tasks.Records.Single(t => t.Id == id);
    private TerminalIdentitySession Agent(string role)
    {
        var child = owner.CreateAttachment(); registry.Add(child); child.Switch(engine.ResolveProgram(role).Id.ToString()); return child;
    }
    [Fact]
    public async Task HostDispatchesBrokerOperationAndReturnsToCommandLoop()
    {
        Initialize(); var task = Proposal(); engine.ApproveTask(owner.Reference, task.Id);
        var output = new StringWriter();
        var host = new TerminalHost(engine, commands, new StringReader("tasks run\ntasks list\nexit\n"), output);
        Assert.Equal(0, await host.RunAsync());
        Assert.Contains("DEMONSTRATION ONLY", output.ToString()); Assert.Equal(Status.Completed, Record(task.Id).Status);
    }
    [Fact]
    public void SqlFailureRollsBackTaskApprovalAndRetainsPriorAudit()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridTaskRollback-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            Initialize(); var task = Proposal(); var info = new WorldInfo(engine.GetSnapshot().WorldId, "Rollback", DateTimeOffset.UtcNow);
            var path = Path.Combine(folder, "world.db"); using var store = new WorldStore(path); store.Create(info, engine.GetSnapshot());
            using (var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
            { c.Open(); using var q = c.CreateCommand(); q.CommandText = "CREATE TRIGGER reject_tasks BEFORE INSERT ON WorldTasks BEGIN SELECT RAISE(ABORT,'injected'); END;"; q.ExecuteNonQuery(); }
            engine.ApproveTask(owner.Reference, task.Id);
            Assert.Throws<SqliteException>(() => store.Save(info, engine.GetSnapshot()));
            var saved = Assert.Single(store.Load().State.Tasks.Records);
            Assert.Equal(Status.AwaitingApproval, saved.Status); Assert.Null(saved.ApprovedBy); Assert.Single(saved.Audit);
        }
        finally { Directory.Delete(folder, true); }
    }
    [Fact]
    public void RegistryIsExplicitIdempotentAndDoesNotImplyWorkingSpecialists()
    {
        Assert.Empty(engine.GetSnapshot().Tasks.Agents); Assert.Contains("grid -start", commands.Execute("agents initialize").Text);
        Assert.Empty(engine.GetSnapshot().Tasks.Agents); commands.Execute("grid -start");
        var ids = engine.GetSnapshot().Tasks.Agents.Select(a => a.Id).ToArray();
        Assert.Equal(6, ids.Length); engine.StartAgents(owner.Reference);
        Assert.Equal(ids, engine.GetSnapshot().Tasks.Agents.Select(a => a.Id));
        Assert.All(ids, id => Assert.Contains(id, engine.GetSnapshot().Identities.RegisteredAgents));
        Assert.Contains("maxis.delay", commands.Execute("agents").Text);
    }
    [Fact]
    public async Task ApprovalRequiredAndCompletedResultsAreClearlyDemonstrations()
    {
        Initialize(); var t = Proposal(argument: "delete all files");
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunNextTaskAsync(owner.Reference, Save, default));
        Assert.Equal(Status.AwaitingApproval, Record(t.Id).Status);
        engine.ApproveTask(owner.Reference, t.Id);
        var result = await engine.RunNextTaskAsync(owner.Reference, Save, default);
        Assert.Contains("DEMONSTRATION ONLY", result); Assert.Contains("No host action", result);
        Assert.Equal(Status.Completed, Record(t.Id).Status); Assert.Equal(1, Record(t.Id).Attempts); Assert.Equal(2, saves);
        Assert.Equal(new[] { "Proposed", "Approved", "Started", "Finished" }, Record(t.Id).Audit.Select(a => a.Action));
        Assert.All(Record(t.Id).Audit, a => Assert.Equal(engine.LocalOperator.Id, a.OperatorId));
    }
    [Fact]
    public void AgentScopeAndCapabilitiesCannotBeEscalatedByNamesOrArguments()
    {
        Initialize(); var maxis = Agent("MAXIS"); var jn = engine.ResolveProgram("JN").Id;
        Assert.Throws<UnauthorizedAccessException>(() => engine.ProposeTask(maxis.Reference, jn, "jn.demo", "test", "other"));
        Assert.Throws<ArgumentException>(() => engine.ProposeTask(maxis.Reference, maxis.CurrentRecipient.Id, "jn.demo", "test", "wrong"));
        Assert.Throws<ArgumentException>(() => Proposal(capability: "shell", argument: "cmd /c anything"));
        Assert.Throws<ArgumentException>(() => Proposal(capability: "maxis.delay", argument: "5001"));
        var own = engine.ProposeTask(maxis.Reference, maxis.CurrentRecipient.Id, "maxis.demo", "test", "own");
        Assert.Equal(maxis.CurrentRecipient.Id, own.SubmittedAsAgent);
        Assert.Throws<UnauthorizedAccessException>(() => engine.ApproveTask(maxis.Reference, own.Id));
        var other = Proposal("JN"); Assert.DoesNotContain(engine.TasksFor(maxis.Reference).Records, t => t.Id == other.Id);
        Assert.Throws<UnauthorizedAccessException>(() => engine.CancelTask(maxis.Reference, other.Id));
        Assert.Contains(engine.CaptureHistory().Events, e => e.Kind == "TaskDenied" && e.ContextJson.Contains(maxis.CurrentRecipient.Id.ToString()));
        Assert.Null(registry.Acquire(maxis.Reference, engine.WorldInstanceId)); // Task leases do not grant edit privileges.
    }
    [Fact]
    public void RequestKeysDeduplicateOnlyIdenticalSameActorProposals()
    {
        Initialize(); var t = Proposal(key: "same"); Assert.Equal(t.Id, Proposal(key: "same").Id);
        Assert.Throws<ArgumentException>(() => Proposal(argument: "changed", key: "same"));
        var agent = Agent("MAXIS"); Assert.Throws<ArgumentException>(() => engine.ProposeTask(agent.Reference, t.AgentId, t.Capability, t.Argument, "same"));
        Assert.Single(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task PersistenceFencePreventsAdapterInvocationAndRequiresReapproval()
    {
        Initialize(); var t = Proposal(); engine.ApproveTask(owner.Reference, t.Id);
        var calls = 0;
        var result = await engine.RunNextTaskAsync(owner.Reference, _ => ++calls == 1 ? Task.FromException(new IOException("disk unavailable")) : Task.CompletedTask, default);
        Assert.Equal(Status.Failed, Record(t.Id).Status); Assert.DoesNotContain("DEMONSTRATION ONLY", result);
        engine.RetryTask(owner.Reference, t.Id); Assert.Equal(Status.AwaitingApproval, Record(t.Id).Status); Assert.Null(Record(t.Id).ApprovedBy);
    }
    [Fact]
    public async Task TimeoutRetriesAreBoundedAndEachNeedsFreshApproval()
    {
        Initialize(); var t = Proposal(capability: "maxis.delay", argument: "5000");
        for (var i = 0; i < 3; i++)
        {
            engine.ApproveTask(owner.Reference, t.Id); await engine.RunNextTaskAsync(owner.Reference, Save, default);
            Assert.Equal(Status.TimedOut, Record(t.Id).Status);
            if (i < 2) engine.RetryTask(owner.Reference, t.Id);
        }
        Assert.Throws<InvalidOperationException>(() => engine.RetryTask(owner.Reference, t.Id));
        Assert.Equal(3, Record(t.Id).Attempts);
    }
    [Fact]
    public async Task ConcurrentDispatchCannotDuplicateExecutionAndCancellationWins()
    {
        Initialize(); var t = Proposal(capability: "maxis.delay", argument: "500"); engine.ApproveTask(owner.Reference, t.Id);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var first = true;
        var run = engine.RunNextTaskAsync(owner.Reference, async token => { if (first) { first = false; started.SetResult(); await release.Task.WaitAsync(token); } }, default);
        await started.Task;
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunNextTaskAsync(owner.Reference, Save, default));
        engine.CancelTask(owner.Reference, t.Id); release.SetResult(); await run;
        Assert.Equal(Status.Cancelled, Record(t.Id).Status); Assert.Equal(1, Record(t.Id).Attempts);
    }
    [Fact]
    public async Task LogoutDuringRunInterruptsAndDoesNotReuseOldSession()
    {
        Initialize(); var t = Proposal(capability: "maxis.delay", argument: "500"); engine.ApproveTask(owner.Reference, t.Id);
        var reference = owner.Reference;
        await engine.RunNextTaskAsync(reference, _ => { owner.Logout(); return Task.CompletedTask; }, default);
        Assert.Equal(Status.Interrupted, Record(t.Id).Status);
        owner.Login(); Assert.Throws<UnauthorizedAccessException>(() => engine.ApproveTask(reference, t.Id));
    }
    [Fact]
    public async Task ShutdownCancellationIsRecordedAndSaved()
    {
        Initialize(); var t = Proposal(capability: "maxis.delay", argument: "500"); engine.ApproveTask(owner.Reference, t.Id);
        using var stop = new CancellationTokenSource();
        await engine.RunNextTaskAsync(owner.Reference, _ => { stop.Cancel(); return Task.CompletedTask; }, stop.Token);
        Assert.Equal(Status.Cancelled, Record(t.Id).Status);
    }
    [Fact]
    public async Task SQLiteRestartRetainsAuditAndRequiresFreshApprovalForQueuedOrRunningWork()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridTasks-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            Initialize(); var t = Proposal(); engine.ApproveTask(owner.Reference, t.Id);
            var info = new WorldInfo(engine.GetSnapshot().WorldId, "Tasks", DateTimeOffset.UtcNow); var path = Path.Combine(folder, "world.db");
            using var store = new WorldStore(path); store.Create(info, engine.GetSnapshot());
            var loaded = GridSimulationEngine.Restore(store.Load().State);
            Assert.Equal(Status.AwaitingApproval, Assert.Single(loaded.GetSnapshot().Tasks.Records).Status);
            var calls = 0;
            await engine.RunNextTaskAsync(owner.Reference, _ => { if (++calls == 1) store.Save(info, engine.GetSnapshot()); return Task.CompletedTask; }, default);
            var interrupted = GridSimulationEngine.Restore(store.Load().State);
            var task = Assert.Single(interrupted.GetSnapshot().Tasks.Records);
            Assert.Equal(Status.Interrupted, task.Status); Assert.Null(task.ApprovedBy); Assert.Equal(1, task.Attempts);
            Assert.Contains(task.Audit, a => a.Action == "Recovered");
            store.Save(info, engine.GetSnapshot());
            Assert.Equal(Status.Completed, Assert.Single(store.Load().State.Tasks.Records).Status);
            File.WriteAllText(path, "damaged"); store.Recover();
            Assert.Equal(Status.Completed, Assert.Single(store.Load().State.Tasks.Records).Status);
        }
        finally { Directory.Delete(folder, true); }
    }
    [Fact]
    public void VersionThirteenMigratesToEmptyQueueWithoutChangingExistingWorld()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridTaskUpgrade-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "world.db"); var info = new WorldInfo(engine.GetSnapshot().WorldId, "Old", DateTimeOffset.UtcNow);
            using var store = new WorldStore(path); store.Create(info, engine.GetSnapshot());
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
            { connection.Open(); using var command = connection.CreateCommand(); command.CommandText = "DROP TABLE WorldTasks; DELETE FROM SchemaMigrations WHERE Version>=14; PRAGMA user_version=13;"; command.ExecuteNonQuery(); }
            var saved = store.Load(); Assert.Equal(info.Id, saved.Info.Id); Assert.Empty(saved.State.Tasks.Records); Assert.Empty(saved.State.Tasks.Agents);
            Assert.Equal(engine.GetSnapshot().Identities.OwnerSubject, saved.State.Identities.OwnerSubject);
        }
        finally { Directory.Delete(folder, true); }
    }
    [Fact]
    public void SnapshotRejectsForgedCapabilityOrRegistryAndCopiesAuditCollections()
    {
        Initialize(); var t = Proposal(); var state = engine.GetSnapshot();
        Assert.Throws<InvalidDataException>(() => GridSimulationEngine.Restore(state with { Tasks = state.Tasks with { Records = [t with { Capability = "shell" }] } }));
        Assert.Throws<InvalidDataException>(() => GridSimulationEngine.Restore(state with { Tasks = state.Tasks with { Agents = [new(Guid.NewGuid(), AgentRole.MAXIS)] } }));
        var audit = t.Audit.ToArray(); var restored = GridSimulationEngine.Restore(state with { Tasks = state.Tasks with { Records = [t with { Audit = audit }] } });
        audit[0] = audit[0] with { Detail = "mutated" }; Assert.DoesNotContain(restored.GetSnapshot().Tasks.Records[0].Audit, a => a.Detail == "mutated");
    }
    [Fact]
    public async Task TerminalWorkflowRunsExplicitlyAndChatNeverDispatchesTasks()
    {
        commands.Execute("grid -start"); ChatCommands.Register(commands, engine.PrepareChatFixture(), owner); commands.UseIdentity(owner);
        commands.Execute("connect MCP"); commands.Execute("talk MAXIS run my tests"); Assert.Empty(engine.GetSnapshot().Tasks.Records);
        Assert.Contains("AwaitingApproval", commands.Execute("tasks propose MAXIS maxis.demo \"hello\" terminal-key").Text);
        var t = Assert.Single(engine.GetSnapshot().Tasks.Records); commands.Execute("tasks approve " + t.Id);
        var operation = commands.Execute("tasks run").BrokerOperation; Assert.NotNull(operation);
        Assert.Contains("Completed", (await operation!(default)).Text);
        Assert.Contains("DEMONSTRATION ONLY", commands.Execute("tasks inspect " + t.Id).Text);
    }
}
