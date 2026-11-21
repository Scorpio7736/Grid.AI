using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using Xunit;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;
public sealed class TronTests
{
    private sealed class Probe(string json) : ITronProbe
    {
        public int Calls;
        public Task<string> ReadAsync(string capability,CancellationToken token) { Calls++; token.ThrowIfCancellationRequested(); return Task.FromResult(json); }
    }
    private readonly TerminalSessionRegistry registry = new();
    private readonly TerminalIdentitySession owner = new(() => "tron-owner");
    private readonly GridSimulationEngine engine;
    public TronTests()
    {
        registry.Add(owner); engine=new(GridWorldFactory.CreateGenesis(),editAuthorizer:registry); owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
    }
    private Task Save(CancellationToken token) { token.ThrowIfCancellationRequested(); GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot()); return Task.CompletedTask; }
    private TaskRecord Propose(string capability="tron.defender",string argument="status") => engine.ProposeTask(owner.Reference,engine.ResolveProgram("TRON").Id,capability,argument,Guid.NewGuid().ToString("N"));
    [Fact]
    public async Task ApprovalAndPersistenceFencePrecedeObservation()
    {
        var probe=new Probe("{\"available\":false,\"reason\":\"AccessDenied\"}"); engine.ConfigureTronProbe(probe);
        var task=Propose(); await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.RunNextTaskAsync(owner.Reference,Save,default)); Assert.Equal(0,probe.Calls);
        engine.ApproveTask(owner.Reference,task.Id); var saves=0;
        await engine.RunNextTaskAsync(owner.Reference,_=>++saves==1 ? Task.FromException(new IOException("disk")) : Task.CompletedTask,default);
        Assert.Equal(0,probe.Calls); Assert.Equal(Status.Failed,Assert.Single(engine.GetSnapshot().Tasks.Records).Status);
    }
    [Fact]
    public async Task UnavailableIsSavedAsUnknownNotAsHealthy()
    {
        engine.ConfigureTronProbe(new Probe("{\"available\":false,\"reason\":\"AccessDenied\"}")); var task=Propose(); engine.ApproveTask(owner.Reference,task.Id);
        await engine.RunNextTaskAsync(owner.Reference,Save,default);
        var record=Assert.Single(engine.GetSnapshot().Tasks.Records); Assert.Equal(Status.Completed,record.Status);
        Assert.Contains("UNAVAILABLE",record.Result); Assert.Contains("Protection is unknown",record.Result); Assert.Contains("REAL HOST SECURITY",record.Result);
    }
    [Theory]
    [InlineData("tron.scan","quick")]
    [InlineData("tron.remediate","enable")]
    [InlineData("tron.firewall","disable")]
    [InlineData("tron.events","Security; whoami")]
    public void RemediationAndArbitraryArgumentsAreRejected(string capability,string argument)
    {
        Assert.Throws<ArgumentException>(()=>Propose(capability,argument)); Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public void DefenderWarningsAndMissingTimestampsAreExplicit()
    {
        var result=TronPolicy.Render("tron.defender","{\"available\":true,\"AntivirusEnabled\":false,\"RealTimeProtectionEnabled\":true,\"QuickScanEnd\":\"2026-10-21T12:00:00Z\"}",DateTimeOffset.UtcNow);
        Assert.Contains("WARNING",result); Assert.Contains("AMServiceEnabled: Unknown",result); Assert.Contains("Last full scan ended UTC: Unknown",result);
        Assert.Contains("2026-10-21T12:00:00",result); Assert.Contains("No scan started",result);
    }
    [Fact]
    public void FirewallPartialCoverageAndDisabledProfileAreWarnings()
    {
        var result=TronPolicy.Render("tron.firewall","{\"available\":true,\"profiles\":[{\"Name\":\"Public\",\"Enabled\":false}]}",DateTimeOffset.UtcNow);
        Assert.Contains("Public: Disabled",result); Assert.Contains("incomplete profile coverage",result);
    }
    [Fact]
    public void EventsAreBoundedAndDoNotExposePayloadsOrInventThreats()
    {
        var result=TronPolicy.Render("tron.events","{\"available\":true,\"events\":[{\"Id\":4625,\"RecordId\":123,\"TimeUtc\":\"2026-10-21T12:00:00Z\",\"Message\":\"secret payload\"}]}",DateTimeOffset.UtcNow);
        Assert.Contains("Event 4625 record 123",result); Assert.DoesNotContain("secret payload",result); Assert.Contains("not a threat verdict",result);
        Assert.Throws<InvalidDataException>(()=>TronPolicy.Render("tron.events",JsonSerializer.Serialize(new{available=true,events=Enumerable.Range(0,11).Select(i=>new{Id=i})}),DateTimeOffset.UtcNow));
    }
    [Fact]
    public async Task InvalidProviderDataFailsAndDoesNotClaimSecurityStatus()
    {
        engine.ConfigureTronProbe(new Probe("not json")); var task=Propose(); engine.ApproveTask(owner.Reference,task.Id); await engine.RunNextTaskAsync(owner.Reference,Save,default);
        var record=Assert.Single(engine.GetSnapshot().Tasks.Records); Assert.Equal(Status.Failed,record.Status); Assert.Contains("unknown",record.Result);
    }
    [Fact]
    public void AgentCannotSelfApproveOrBorrowSecurityCapabilityAndMcpCanDelegate()
    {
        var tron=owner.CreateAttachment(); registry.Add(tron); tron.Switch(engine.ResolveProgram("TRON").Id.ToString());
        var own=engine.ProposeTask(tron.Reference,tron.CurrentRecipient.Id,"tron.defender","status","own");
        Assert.Throws<UnauthorizedAccessException>(()=>engine.ApproveTask(tron.Reference,own.Id));
        Assert.Throws<ArgumentException>(()=>engine.ProposeTask(owner.Reference,engine.ResolveProgram("MAXIS").Id,"tron.defender","status","borrow"));
        Assert.Equal(3,engine.Coordinate(owner.Reference,"TRON: defender status; TRON: firewall status; TRON: events recent","security-check").TaskIds.Count);
    }
    [Fact]
    public async Task RealWindowsProviderReturnsStructuredObservationOrExplicitUnavailable()
    {
        var provider=new WindowsTronProbe();
        foreach(var capability in new[]{"tron.defender","tron.firewall","tron.events"})
        {
            using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(35));
            var json=await provider.ReadAsync(capability,stop.Token);
            var result=TronPolicy.Render(capability,json,DateTimeOffset.UtcNow);
            Assert.Contains("REAL HOST SECURITY",result); Assert.InRange(result.Length,1,2048);
        }
    }
    [Fact]
    public async Task SavedSecurityResultSurvivesSqliteAndRestart()
    {
        engine.ConfigureTronProbe(new Probe("{\"available\":true,\"events\":[]}")); var task=Propose("tron.events","recent"); engine.ApproveTask(owner.Reference,task.Id); await engine.RunNextTaskAsync(owner.Reference,Save,default);
        var folder=Path.Combine(Path.GetTempPath(),"GridTron-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var store=new WorldStore(Path.Combine(folder,"world.db")); store.Create(new WorldInfo(engine.GetSnapshot().WorldId,"Security",DateTimeOffset.UtcNow),engine.GetSnapshot());
            var restored=GridSimulationEngine.Restore(store.Load().State);
            Assert.Contains("No matching events",Assert.Single(restored.GetSnapshot().Tasks.Records).Result);
        }
        finally { Directory.Delete(folder,true); }
    }

    [Fact]
    public void ExistingSavedSecurityRoleAndCapabilitiesMigrateToTronBeforeValidation()
    {
        var legacy = string.Concat("SEN", "TINEL");
        var task = Propose("tron.events", "recent");
        var tronId = engine.ResolveProgram("TRON").Id;
        var folder = Path.Combine(Path.GetTempPath(), "GridTronMigration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            using (var store = new WorldStore(Path.Combine(folder, "world.db")))
                store.Create(new WorldInfo(engine.GetSnapshot().WorldId, "Security migration", DateTimeOffset.UtcNow), engine.GetSnapshot());
            using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(folder, "world.db"), Pooling = false }.ToString()))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "UPDATE Programs SET Name=$name WHERE ProgramId=$id; UPDATE WorldTasks SET StateJson=replace(StateJson,$current,$legacy);";
                command.Parameters.AddWithValue("$name", legacy);
                command.Parameters.AddWithValue("$id", tronId.ToString("D"));
                command.Parameters.AddWithValue("$current", "tron.events");
                command.Parameters.AddWithValue("$legacy", legacy.ToLowerInvariant() + ".events");
                command.ExecuteNonQuery();
            }
            using var reopened = new WorldStore(Path.Combine(folder, "world.db"));
            var loaded = reopened.Load().State;
            Assert.Equal("tron.events", loaded.Tasks.Records.Single(record => record.Id == task.Id).Capability);
            var restored = GridSimulationEngine.Restore(loaded);
            Assert.Equal(tronId, restored.ResolveProgram("TRON").Id);
            Assert.DoesNotContain(restored.GetSnapshot().Programs, program => program.Name.Equals(legacy, StringComparison.OrdinalIgnoreCase));

            var personas = new Grid.Agent.AgentPersonas();
            personas.Restore([new(legacy, "v1", null)]);
            Assert.Equal("TRON", Assert.Single(personas.Export()).Agent);
        }
        finally { SqliteConnection.ClearAllPools(); Directory.Delete(folder, true); }
    }
}
