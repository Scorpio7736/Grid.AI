using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Resources;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class ResourcesTests
{
    [Fact]
    public void ResourceCommandDistinguishesMeasuredUnknownAndStaleValues()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        engine.UpdateHostResources([
            new(ResourceMetric.RamAvailableBytes, "host", 4096, ObservationStatus.Available, DateTimeOffset.UtcNow, "Fake"),
            new(ResourceMetric.GpuAvailablePercent, "host", null, ObservationStatus.Unsupported, DateTimeOffset.UtcNow, "Fake"),
            new(ResourceMetric.CpuAvailablePercent, "cpu", 99, ObservationStatus.Available, DateTimeOffset.UtcNow.AddMinutes(-1), "Fake")]);
        var commands = DefaultCommands.Create(engine); ResourcesCommand.Register(commands, engine);
        var text = commands.Execute("resources").Text;
        Assert.Contains("4096 bytes", text); Assert.Contains("Unsupported", text); Assert.Contains("Stale", text);
        Assert.DoesNotContain("99 percent", text); Assert.Contains("Application energy: unknown", text);
        Assert.Contains("Usage", commands.Execute("resources extra").Text);
        var lines = LiveViewRenderer.Render(engine.GetLiveSnapshot(), 240, 30);
        Assert.Contains(lines, l => l.Contains("HOST |") && l.Contains("unknown (Stale)"));
    }

    [Fact]
    public async Task HostStopsTelemetryAndReportsUnexpectedFailures()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var telemetry = new TestMonitor();
        var output = new StringWriter();
        Assert.Equal(0, await new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("exit\n"), output,
            false, telemetry: telemetry).RunAsync());
        Assert.True(telemetry.Stopped);
        var broken = new TestMonitor { Fail = true };
        Assert.Equal(1, await new TerminalHost(engine, DefaultCommands.Create(engine), new StringReader("exit\n"), output,
            false, telemetry: broken).RunAsync());
        Assert.Contains("host-telemetry", output.ToString());
        Assert.True(broken.Stopped);
    }

    private sealed class TestMonitor : IHostTelemetryMonitor
    {
        public bool Fail { get; init; }
        public bool Stopped { get; private set; }
        public async Task RunAsync(CancellationToken token)
        {
            try
            {
                if (Fail) throw new InvalidOperationException("Injected telemetry defect");
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            finally { Stopped = true; }
        }
    }
}
