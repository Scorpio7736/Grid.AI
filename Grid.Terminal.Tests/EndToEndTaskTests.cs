using System.Diagnostics;
using System.Xml.Linq;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Xunit;
using Status = Grid.Simulation.Tasks.TaskStatus;

namespace Grid.Terminal.Tests;

/// <summary>Real offline unit-test execution through MCP, the approval broker, MAXIS and RELAY.</summary>
public sealed class EndToEndTaskTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "GridE2E-" + Guid.NewGuid().ToString("N"));
    private readonly TerminalSessionRegistry registry = new();
    private readonly TerminalIdentitySession owner = new(() => "e2e-owner");
    private readonly GridSimulationEngine engine;
    private readonly WorldStore store;
    private readonly WorldInfo info;
    private string Source => Path.Combine(folder, "source");
    private string Artifacts => Path.Combine(folder, "artifacts");

    public EndToEndTaskTests()
    {
        Directory.CreateDirectory(Source);
        registry.Add(owner);
        engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
        owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
        engine.ConfigureMaxisRuntime(Artifacts);
        info = new(engine.GetSnapshot().WorldId, "End-to-end verification", DateTimeOffset.UtcNow);
        store = new(Path.Combine(folder, "world.db"));
        store.Create(info, engine.GetSnapshot());
    }

    private Task Save(CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); store.Save(info, engine.GetSnapshot()); return Task.CompletedTask;
    }
    private TaskRecord Record(Guid id) => engine.GetSnapshot().Tasks.Records.Single(t => t.Id == id);
    private TerminalIdentitySession Agent(string role)
    {
        var agent = owner.CreateAttachment(); registry.Add(agent);
        agent.Switch(engine.ResolveProgram(role).Id.ToString()); return agent;
    }

    private async Task CreateRealTestProject(bool fail)
    {
        // Clear package sources: use only the packages already needed by GRID's test suite.
        File.WriteAllText(Path.Combine(Source, "NuGet.Config"), "<configuration><packageSources><clear /></packageSources></configuration>");
        File.WriteAllText(Path.Combine(Source, "Verification.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework><IsTestProject>true</IsTestProject><NuGetAudit>false</NuGetAudit></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.9.0" />
                <PackageReference Include="xunit" Version="2.9.3" />
                <PackageReference Include="xunit.runner.visualstudio" Version="4.0.0" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(Source, "ActualTests.cs"),
            "public class ActualTests { [Xunit.Fact] public void Arithmetic() { Xunit.Assert.Equal(4, 2 + 2); } " +
            "[Xunit.Fact] public void ExpectedOutcome() { Xunit.Assert.True(" + (fail ? "false" : "true") + "); } }");
        var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe"))
        { WorkingDirectory = Source, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "restore", "Verification.csproj", "--configfile", "NuGet.Config", "--verbosity", "quiet" }) start.ArgumentList.Add(arg);
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); throw; }
        Assert.True(process.ExitCode == 0, await stdout + await stderr);
        engine.RegisterDevelopmentWorkspace(owner.Reference, "fixture", Source, "Verification.csproj");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealUnitTestsReturnRecordedSuccessOrFailureThroughRelayAndSurviveRestart(bool fail)
    {
        await CreateRealTestProject(fail);
        var mcp = Agent("MCP");
        var plan = engine.Coordinate(mcp.Reference, "MAXIS: test fixture", "actual-tests");
        var id = Assert.Single(plan.TaskIds);
        Assert.Equal(plan.Id, engine.Coordinate(mcp.Reference, plan.Request, plan.RequestKey).Id);
        Assert.Equal(Status.AwaitingApproval, Record(id).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunNextTaskAsync(owner.Reference, Save, default));
        Assert.False(Directory.Exists(Artifacts));
        Assert.Throws<UnauthorizedAccessException>(() => engine.ApproveTask(mcp.Reference, id));
        engine.ApproveTask(owner.Reference, id);
        await Save(default);
        await engine.RunNextTaskAsync(owner.Reference, Save, default);
        var completed = Record(id);
        Assert.Equal(fail ? Status.Failed : Status.Completed, completed.Status);
        Assert.Equal(1, completed.Attempts);
        Assert.Equal(new[] { "Delegated", "Approved", "Started", "Finished" }, completed.Audit.Select(a => a.Action));
        var attempt = Path.Combine(Artifacts, id.ToString("N") + "-1");
        var trx = XDocument.Load(Path.Combine(attempt, "tests.trx"));
        var counters = trx.Descendants().Single(e => e.Name.LocalName == "Counters");
        Assert.Equal("2", counters.Attribute("total")!.Value);
        Assert.Equal(fail ? "1" : "2", counters.Attribute("passed")!.Value);
        Assert.Equal(fail ? "1" : "0", counters.Attribute("failed")!.Value);
        Assert.Contains("Exit code: " + (fail ? "1" : "0"), File.ReadAllText(Path.Combine(attempt, "report.txt")));
        Assert.NotEmpty(File.ReadAllText(Path.Combine(attempt, "stdout.txt")));
        Assert.True(File.Exists(Path.Combine(attempt, "stderr.txt")));
        Assert.Contains("report:", completed.Result!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(completed.Result, store.Load().State.Tasks.Records.Single(t => t.Id == id).Result);

        var relayPlan = engine.Coordinate(mcp.Reference, "RELAY: brief " + id, "deliver-result");
        var relayId = Assert.Single(relayPlan.TaskIds);
        engine.ApproveTask(owner.Reference, relayId);
        await engine.RunNextTaskAsync(owner.Reference, Save, default);
        Assert.Equal(Status.Completed, Record(relayId).Status);
        var notification = Assert.Single(engine.GetSnapshot().Tasks.Relay.Notifications);
        Assert.Equal(id, notification.SourceTaskId);
        Assert.Contains(completed.Status.ToString(), notification.Text);
        Assert.Contains(completed.Result!, notification.Text);
        Assert.Equal(completed.Result, Record(id).Result);
        var restored = GridSimulationEngine.Restore(store.Load().State);
        Assert.Equal(notification, Assert.Single(restored.GetSnapshot().Tasks.Relay.Notifications));
        Assert.Equal(completed.Result, restored.GetSnapshot().Tasks.Records.Single(t => t.Id == id).Result);
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunNextTaskAsync(owner.Reference, Save, default));
        Assert.Equal(1, Record(id).Attempts);
        Assert.Single(Directory.GetFiles(Artifacts, "tests.trx", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("MCP")]
    [InlineData("MAXIS")]
    [InlineData("TRON")]
    [InlineData("JN")]
    [InlineData("VOLT")]
    [InlineData("RELAY")]
    public async Task EveryAgentIsDeniedHumanApprovalDispatchAndOtherRolesCapabilities(string role)
    {
        var agent = Agent(role);
        var targetRole = role == "MAXIS" ? "JN" : "MAXIS";
        var target = engine.ResolveProgram(targetRole).Id;
        var task = engine.ProposeTask(owner.Reference, target, targetRole.ToLowerInvariant() + ".demo", "test", "human");
        Assert.Throws<UnauthorizedAccessException>(() => engine.ProposeTask(agent.Reference, target, targetRole.ToLowerInvariant() + ".demo", "test", "borrow"));
        Assert.Throws<ArgumentException>(() => engine.ProposeTask(agent.Reference, agent.CurrentRecipient.Id, targetRole.ToLowerInvariant() + ".demo", "test", "forged"));
        Assert.Throws<UnauthorizedAccessException>(() => engine.ApproveTask(agent.Reference, task.Id));
        engine.ApproveTask(owner.Reference, task.Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => engine.RunNextTaskAsync(agent.Reference, Save, default));
        Assert.Equal(0, Record(task.Id).Attempts);
        Assert.Null(registry.Acquire(agent.Reference, engine.WorldInstanceId));
        await Save(default);
        Assert.Single(store.Load().State.Tasks.Records);
    }

    [Fact]
    public async Task CancelledDelegationCannotDispatchTwiceAndDurableRunningStateRequiresFreshConsent()
    {
        File.WriteAllText(Path.Combine(Source, "sample.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(Source, "sample.cs"), "class Sample {}");
        engine.RegisterDevelopmentWorkspace(owner.Reference, "fixture", Source, "sample.csproj");
        var plan = engine.Coordinate(Agent("MCP").Reference, "MAXIS: inspect fixture|sample.cs", "cancel");
        var id = Assert.Single(plan.TaskIds);
        engine.ApproveTask(owner.Reference, id);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = true;
        var run = engine.RunNextTaskAsync(owner.Reference, async token =>
        {
            if (!first) return;
            first = false; await Save(token); entered.SetResult(); await release.Task.WaitAsync(token);
        }, default);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunNextTaskAsync(owner.Reference, Save, default));
            var recoveredRegistry = new TerminalSessionRegistry();
            var recoveredOwner = new TerminalIdentitySession(() => "e2e-owner"); recoveredRegistry.Add(recoveredOwner);
            var recovered = GridSimulationEngine.Restore(store.Load().State, editAuthorizer: recoveredRegistry);
            recoveredOwner.Attach(recovered); recoveredOwner.Login(); recovered.ConfigureMaxisRuntime(Artifacts);
            var interrupted = Assert.Single(recovered.GetSnapshot().Tasks.Records);
            Assert.Equal(Status.Interrupted, interrupted.Status); Assert.Null(interrupted.ApprovedBy);
            recovered.RetryTask(recoveredOwner.Reference, id);
            await Assert.ThrowsAsync<InvalidOperationException>(() => recovered.RunNextTaskAsync(recoveredOwner.Reference, _ => Task.CompletedTask, default));
            Assert.Equal(Status.AwaitingApproval, Assert.Single(recovered.GetSnapshot().Tasks.Records).Status);
            Assert.False(Directory.Exists(Artifacts));
            engine.CancelTask(owner.Reference, id);
        }
        finally { release.TrySetResult(); await run; }
        Assert.Equal(Status.Cancelled, Record(id).Status); Assert.False(Directory.Exists(Artifacts));
        await Save(default);
        Assert.Equal(Status.Cancelled, Assert.Single(store.Load().State.Tasks.Records).Status);
    }

    public void Dispose() { store.Dispose(); Directory.Delete(folder, true); }
}
