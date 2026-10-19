using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Applications;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class ApplicationTests
{
    [Fact]
    public void ExecutableGroupingIncludesHelpersMergesIndependentWindowsAndExcludesUnanchoredServices()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(-1);
        ApplicationProcess Row(string key, int pid, bool window, long? memory = 100) => new(key, key, new(pid, now), memory, window);
        var rows = new[] { Row("editor", 1, true), Row("editor", 2, false), Row("editor", 3, true), Row("service", 4, false), Row("other-image", 5, true) };
        var grouped = ApplicationGrouping.Group(rows, new HashSet<string>(), true);
        Assert.Equal(2, grouped.Count); var editor = grouped.Single(a => a.Key == "editor");
        Assert.Equal(3, editor.Processes.Count); Assert.Equal(300, editor.WorkingSetBytes);
        Assert.Null(ApplicationGrouping.Group(rows, new HashSet<string>(), false)[0].WorkingSetBytes);
        var hidden = ApplicationGrouping.Group([Row("editor", 2, false)], new HashSet<string> { "editor" }, true);
        Assert.Single(hidden);
    }
    [Fact]
    public void InspectionAndLiveShowTransientIdentityAndUnknownConditionWithoutClaimingIdleHealth()
    {
        var world = new GridWorld(); world.AddDistrict(new() { Name = "Test" }); var engine = new GridSimulationEngine(world);
        var now = DateTimeOffset.UtcNow;
        engine.ReconcileApplications(new(now, true, [new("key", "Editor", [new(100, now.AddSeconds(-1))], 1024)], "Fake"));
        var commands = DefaultCommands.Create(engine); var text = commands.Execute("inspect program Editor").Text;
        Assert.Contains("Application", text); Assert.Contains("Incarnation:", text); Assert.Contains("Energy: unknown", text);
        Assert.Contains("1024 bytes", text); Assert.Contains("Observed running", text);
        Assert.Contains("unknown", string.Join("\n", LiveViewRenderer.Render(engine.GetLiveSnapshot(), 240, 35)));
        Assert.Equal(0, engine.TickCount);
    }
    [Fact]
    public async Task DiscoveryRespectsPreCancellation() => await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        await new WindowsApplicationDiscovery().SampleAsync(new CancellationToken(true)));
}
