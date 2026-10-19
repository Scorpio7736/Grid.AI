using Grid.Core;
using Grid.Core.Generation;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class CityCommandTests
{
    [Fact]
    public void CityCommandReportsSavedLayoutAndDiscoveryCoverageWithoutExposingHardwareLabels()
    {
        var profile = new HardwareProfile(DateTimeOffset.UtcNow,
            [new(HardwareKind.Ram, DiscoveryStatus.Available, "Fixture", "", [new("Private hardware label", 32L * 1073741824)])]);
        var engine = new GridSimulationEngine(HardwareCityGenerator.Create(profile, 12));
        var commands = DefaultCommands.Create(engine); CityCommand.Register(commands, engine);
        var output = commands.Execute("city").Text;
        Assert.Contains("hardware-city-v1", output); Assert.Contains("6 districts", output);
        Assert.Contains("Capacity fields: 1", output); Assert.Contains("Unavailable", output);
        Assert.DoesNotContain("Private hardware label", output); Assert.Contains("Usage", commands.Execute("city extra").Text);
        Assert.Equal(0, engine.TickCount);
    }

    [Fact]
    public void LegacyCityCommandDoesNotGenerate()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); var commands = DefaultCommands.Create(engine);
        CityCommand.Register(commands, engine); Assert.Contains("Existing world preserved", commands.Execute("city").Text);
        Assert.Null(engine.GetSnapshot().City);
    }

    [Fact]
    public async Task CancellationBeforeDiscoveryDoesNotStartCollector() =>
        await Assert.ThrowsAsync<OperationCanceledException>(() => new WindowsHardwareDiscovery().DiscoverAsync(new CancellationToken(true)));
}
