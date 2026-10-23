using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using System.Text.Json;
using Xunit;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;
public sealed class SentinelTests
{
    private sealed class Probe(string json) : ISentinelProbe
    {
        public int Calls;
        public Task<string> ReadAsync(string capability,CancellationToken token) { Calls++; token.ThrowIfCancellationRequested(); return Task.FromResult(json); }
    }
    private readonly TerminalSessionRegistry registry = new();
    private readonly TerminalIdentitySession owner = new(() => "sentinel-owner");
    private readonly GridSimulationEngine engine;
    public SentinelTests()
    {
        registry.Add(owner); engine=new(GridWorldFactory.CreateGenesis(),editAuthorizer:registry); owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
    }
    private Task Save(CancellationToken token) { token.ThrowIfCancellationRequested(); GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot()); return Task.CompletedTask; }
    private TaskRecord Propose(string capability="sentinel.defender",string argument="status") => engine.ProposeTask(owner.Reference,engine.ResolveProgram("SENTINEL").Id,capability,argument,Guid.NewGuid().ToString("N"));
    [Fact]
    public async Task ApprovalAndPersistenceFencePrecedeObservation()
    {
        var probe=new Probe("{\"available\":false,\"reason\":\"AccessDenied\"}"); engine.ConfigureSentinelProbe(probe);
        var task=Propose(); await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.RunNextTaskAsync(owner.Reference,Save,default)); Assert.Equal(0,probe.Calls);
        engine.ApproveTask(owner.Reference,task.Id); var saves=0;
        await engine.RunNextTaskAsync(owner.Reference,_=>++saves==1 ? Task.FromException(new IOException("disk")) : Task.CompletedTask,default);
        Assert.Equal(0,probe.Calls); Assert.Equal(Status.Failed,Assert.Single(engine.GetSnapshot().Tasks.Records).Status);
    }
    [Fact]
    public async Task UnavailableIsSavedAsUnknownNotAsHealthy()
    {
        engine.ConfigureSentinelProbe(new Probe("{\"available\":false,\"reason\":\"AccessDenied\"}")); var task=Propose(); engine.ApproveTask(owner.Reference,task.Id);
        await engine.RunNextTaskAsync(owner.Reference,Save,default);
        var record=Assert.Single(engine.GetSnapshot().Tasks.Records); Assert.Equal(Status.Completed,record.Status);
        Assert.Contains("UNAVAILABLE",record.Result); Assert.Contains("Protection is unknown",record.Result); Assert.Contains("REAL HOST SECURITY",record.Result);
    }
    [Theory]
    [InlineData("sentinel.scan","quick")]
    [InlineData("sentinel.remediate","enable")]
    [InlineData("sentinel.firewall","disable")]
    [InlineData("sentinel.events","Security; whoami")]
    public void RemediationAndArbitraryArgumentsAreRejected(string capability,string argument)
    {
        Assert.Throws<ArgumentException>(()=>Propose(capability,argument)); Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public void DefenderWarningsAndMissingTimestampsAreExplicit()
    {
        var result=SentinelPolicy.Render("sentinel.defender","{\"available\":true,\"AntivirusEnabled\":false,\"RealTimeProtectionEnabled\":true,\"QuickScanEnd\":\"2026-10-21T12:00:00Z\"}",DateTimeOffset.UtcNow);
        Assert.Contains("WARNING",result); Assert.Contains("AMServiceEnabled: Unknown",result); Assert.Contains("Last full scan ended UTC: Unknown",result);
        Assert.Contains("2026-10-21T12:00:00",result); Assert.Contains("No scan started",result);
    }
    [Fact]
    public void FirewallPartialCoverageAndDisabledProfileAreWarnings()
    {
        var result=SentinelPolicy.Render("sentinel.firewall","{\"available\":true,\"profiles\":[{\"Name\":\"Public\",\"Enabled\":false}]}",DateTimeOffset.UtcNow);
        Assert.Contains("Public: Disabled",result); Assert.Contains("incomplete profile coverage",result);
    }
    [Fact]
    public void EventsAreBoundedAndDoNotExposePayloadsOrInventThreats()
    {
        var result=SentinelPolicy.Render("sentinel.events","{\"available\":true,\"events\":[{\"Id\":4625,\"RecordId\":123,\"TimeUtc\":\"2026-10-21T12:00:00Z\",\"Message\":\"secret payload\"}]}",DateTimeOffset.UtcNow);
        Assert.Contains("Event 4625 record 123",result); Assert.DoesNotContain("secret payload",result); Assert.Contains("not a threat verdict",result);
        Assert.Throws<InvalidDataException>(()=>SentinelPolicy.Render("sentinel.events",JsonSerializer.Serialize(new{available=true,events=Enumerable.Range(0,11).Select(i=>new{Id=i})}),DateTimeOffset.UtcNow));
    }
    [Fact]
    public async Task InvalidProviderDataFailsAndDoesNotClaimSecurityStatus()
    {
        engine.ConfigureSentinelProbe(new Probe("not json")); var task=Propose(); engine.ApproveTask(owner.Reference,task.Id); await engine.RunNextTaskAsync(owner.Reference,Save,default);
        var record=Assert.Single(engine.GetSnapshot().Tasks.Records); Assert.Equal(Status.Failed,record.Status); Assert.Contains("unknown",record.Result);
    }
    [Fact]
    public void AgentCannotSelfApproveOrBorrowSecurityCapabilityAndMcpCanDelegate()
    {
        var sentinel=owner.CreateAttachment(); registry.Add(sentinel); sentinel.Switch(engine.ResolveProgram("SENTINEL").Id.ToString());
        var own=engine.ProposeTask(sentinel.Reference,sentinel.CurrentRecipient.Id,"sentinel.defender","status","own");
        Assert.Throws<UnauthorizedAccessException>(()=>engine.ApproveTask(sentinel.Reference,own.Id));
        Assert.Throws<ArgumentException>(()=>engine.ProposeTask(owner.Reference,engine.ResolveProgram("MAXIS").Id,"sentinel.defender","status","borrow"));
        Assert.Equal(3,engine.Coordinate(owner.Reference,"SENTINEL: defender status; SENTINEL: firewall status; SENTINEL: events recent","security-check").TaskIds.Count);
    }
    [Fact]
    public async Task RealWindowsProviderReturnsStructuredObservationOrExplicitUnavailable()
    {
        var provider=new WindowsSentinelProbe();
        foreach(var capability in new[]{"sentinel.defender","sentinel.firewall","sentinel.events"})
        {
            using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(35));
            var json=await provider.ReadAsync(capability,stop.Token);
            var result=SentinelPolicy.Render(capability,json,DateTimeOffset.UtcNow);
            Assert.Contains("REAL HOST SECURITY",result); Assert.InRange(result.Length,1,2048);
        }
    }
    [Fact]
    public async Task SavedSecurityResultSurvivesSqliteAndRestart()
    {
        engine.ConfigureSentinelProbe(new Probe("{\"available\":true,\"events\":[]}")); var task=Propose("sentinel.events","recent"); engine.ApproveTask(owner.Reference,task.Id); await engine.RunNextTaskAsync(owner.Reference,Save,default);
        var folder=Path.Combine(Path.GetTempPath(),"GridSentinel-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var store=new WorldStore(Path.Combine(folder,"world.db")); store.Create(new WorldInfo(engine.GetSnapshot().WorldId,"Security",DateTimeOffset.UtcNow),engine.GetSnapshot());
            var restored=GridSimulationEngine.Restore(store.Load().State);
            Assert.Contains("No matching events",Assert.Single(restored.GetSnapshot().Tasks.Records).Result);
        }
        finally { Directory.Delete(folder,true); }
    }
}
