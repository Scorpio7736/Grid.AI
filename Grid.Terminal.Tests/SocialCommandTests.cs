using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Social;
using System.Text.Json;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class SocialCommandTests
{
    [Fact]
    public void SummaryDetailsAndInspectAreReadOnlyAndExplainScope()
    {
        var world = new GridWorld(); var district = new GridDistrict { Name = "Community" }; world.AddDistrict(district);
        for (var i = 0; i < 2; i++) world.AddProgram(new GridProgram { Name = $"Agent {i}", Occupation = "Unassigned", Kind = ResidentKind.Agent }, district.Id);
        var engine = new GridSimulationEngine(world, TimeSpan.FromSeconds(10));
        foreach (var id in world.Programs.Keys) engine.ConfigureSocial(id, SocialPreference.Cooperative);
        engine.Step();
        var before = JsonSerializer.Serialize(engine.GetSnapshot());
        var commands = DefaultCommands.Create(engine); SocialCommand.Register(commands, engine);
        Assert.Contains("not CPU/RAM", commands.Execute("social").Text);
        Assert.Contains("Community", commands.Execute("social").Text);
        var detail = commands.Execute("social Agent 0").Text;
        Assert.Contains("Cooperated", detail); Assert.Contains("RELATIONSHIPS", detail); Assert.Contains("OCCUPATION HISTORY", detail);
        Assert.Contains("Cooperated", commands.Execute("inspect program Agent 0").Text);
        Assert.Contains("No matching resident", commands.Execute("social missing").Text);
        Assert.Contains("social", commands.Execute("help").Text);
        Assert.Equal(before, JsonSerializer.Serialize(engine.GetSnapshot()));
    }

    [Fact]
    public void DefaultWorldDoesNotInventSocialResidents()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var commands = DefaultCommands.Create(engine); SocialCommand.Register(commands, engine);
        Assert.Contains("No automatic opt-in", commands.Execute("social").Text);
        Assert.Contains("not opted", commands.Execute("social " + engine.GetSnapshot().Programs[0].Id).Text);
        Assert.Empty(engine.GetSnapshot().Social.Residents);
    }
}

