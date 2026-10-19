using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class BehaviorCommandTests
{
    [Fact]
    public void BehaviorCommandReportsNeedsWithoutAdvancingOrGrantingConfiguration()
    {
        var world = new GridWorld(); var d = new GridDistrict { Name = "Test" }; world.AddDistrict(d);
        world.AddProgram(new() { Name = "Agent", Occupation = "Unassigned", Kind = ResidentKind.Agent }, d.Id);
        var engine = new GridSimulationEngine(world); var commands = DefaultCommands.Create(engine); BehaviorCommand.Register(commands, engine);
        Assert.Contains("Assignment", commands.Execute("behaviors").Text); Assert.Contains("Usage", commands.Execute("behaviors configure").Text);
        Assert.Equal(0, engine.TickCount);
        Assert.Contains("Behavior (last tick)", commands.Execute("inspect program Agent").Text);
    }
}
