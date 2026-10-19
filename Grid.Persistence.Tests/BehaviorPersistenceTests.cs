using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Behavior;
using Grid.Simulation.Resources;
using Xunit;
namespace Grid.Persistence.Tests;

public sealed class BehaviorPersistenceTests
{
    [Fact]
    public void WorkBudgetAndProgressSurviveReopenButObservationsDoNot()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridBehavior-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "world.db");
            var world = new GridWorld(); var district = new GridDistrict { Name = "Test" }; world.AddDistrict(district);
            var agent = new GridProgram { Name = "Persistent", Occupation = "Unassigned", Kind = ResidentKind.Agent }; world.AddProgram(agent, district.Id);
            var engine = new GridSimulationEngine(world);
            engine.ConfigureRoutine(agent.Id, new(RoutineOccupation.Technician, TimeSpan.FromSeconds(2).Ticks, TimeSpan.FromSeconds(2).Ticks,
                TimeSpan.FromSeconds(1).Ticks, 0, 24, [new(ResourceMetric.RamAvailableBytes, "host", 100, "Fixture budget")]));
            engine.UpdateHostResources([new(ResourceMetric.RamAvailableBytes, "host", 100, ObservationStatus.Available, DateTimeOffset.UtcNow, "Fake")]);
            engine.Step(); var before = engine.GetSnapshot();
            using (var store = new WorldStore(path)) store.Create(new(world.Id, "Behavior", DateTimeOffset.UtcNow), before);
            using var reopened = new WorldStore(path); var restored = GridSimulationEngine.Restore(reopened.Load().State);
            var state = restored.GetSnapshot(); Assert.Equal(1, state.TickCount); Assert.False(state.IsRunning);
            Assert.Equal(1, state.Programs[0].Behavior!.CompletedUnits); Assert.Equal("Fixture budget", state.Programs[0].Behavior!.Routine!.Requirements[0].Provenance);
            Assert.Null(state.Programs[0].Condition!.Score); restored.Step();
            Assert.Equal(ResidentAction.Unknown, restored.GetSnapshot().Programs[0].Behavior!.Action);
            Assert.Equal(1, restored.GetSnapshot().Programs[0].Behavior!.CompletedUnits);
            restored.UpdateHostResources([new(ResourceMetric.RamAvailableBytes, "host", 100, ObservationStatus.Available, DateTimeOffset.UtcNow, "Fresh")]);
            restored.Step(); Assert.Equal(2, restored.GetSnapshot().Programs[0].Behavior!.CompletedUnits);
            File.WriteAllText(path, "damaged"); reopened.Recover();
            Assert.Equal(1, reopened.Load().State.Programs[0].Behavior!.CompletedUnits);
        }
        finally { foreach (var file in Directory.GetFiles(folder)) File.Delete(file); Directory.Delete(folder); }
    }
}
