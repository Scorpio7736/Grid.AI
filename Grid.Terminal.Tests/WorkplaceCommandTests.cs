using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class WorkplaceCommandTests
{
    [Fact]
    public void JobsAndActivityExposeVacanciesAndMeasuredEvidenceWithoutAdvancingWorld()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var now = DateTimeOffset.UtcNow;
        engine.ReconcileApplications(new(now, true, [new("app", "ObservedApp", [new(99, now.AddMinutes(-1))], 4096)], "Fixture"));
        var commands = DefaultCommands.Create(engine); WorkplaceCommands.Register(commands, engine);
        Assert.Contains("Vacant", commands.Execute("jobs").Text);
        Assert.Contains("Archivist", commands.Execute("jobs").Text);
        var activity = commands.Execute("activity").Text;
        Assert.Contains("ObservedApp", activity); Assert.Contains("4096", activity);
        Assert.Contains("attribution and requirements remain unknown", activity);
        Assert.Contains("Usage: activity", commands.Execute("activity invalid").Text);
        Assert.Contains("Usage: jobs", commands.Execute("jobs invalid").Text);
        Assert.Contains("activity", commands.Execute("help").Text);
        Assert.Equal(0, engine.TickCount);
        Assert.All(engine.GetSnapshot().Programs, p => Assert.Null(p.Behavior?.Travel));
    }
}
