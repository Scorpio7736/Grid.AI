using Grid.Core;
using Grid.Core.Generation;
using Grid.Simulation;
using Grid.Simulation.Behavior;
using Grid.Simulation.Resources;
using Xunit;
namespace Grid.Persistence.Tests;

public sealed class WorkplacePersistenceTests
{
    [Fact]
    public void MidRouteSaveReopenAndRecoveryKeepAssignmentWithoutOfflineMovement()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridTravel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "world.db");
            var world = HardwareCityGenerator.Create(new(DateTimeOffset.UtcNow, []), 23);
            var agent = new GridProgram { Name = "Archivist", Occupation = "Unassigned", Kind = ResidentKind.Agent };
            world.AddProgram(agent, world.City!.Read().Districts.Single(d => d.Category == HardwareKind.Network).Id);
            var engine = new GridSimulationEngine(world);
            engine.ConfigureRoutine(agent.Id, new(RoutineOccupation.Archivist, TimeSpan.TicksPerSecond * 10,
                TimeSpan.TicksPerSecond, TimeSpan.TicksPerSecond, 0, 24,
                [new(ResourceMetric.RamAvailableBytes, "host", 100, "Fixture")]));
            var station = engine.AssignWorkplace(agent.Id);
            engine.UpdateHostResources([new(ResourceMetric.RamAvailableBytes, "host", 100, ObservationStatus.Available, DateTimeOffset.UtcNow, "Fake")]);
            engine.Step(); engine.Step();
            var before = engine.GetSnapshot();
            var info = new WorldInfo(world.Id, "Travel", DateTimeOffset.UtcNow, 23, HardwareCityGenerator.Version);
            using (var store = new WorldStore(path)) store.Create(info, before);
            using var reopened = new WorldStore(path);
            var restored = GridSimulationEngine.Restore(reopened.Load().State);
            var travel = restored.GetSnapshot().Programs.Single().Behavior!.Travel!;
            Assert.Equal(station.Id, travel.WorkstationId);
            Assert.Equal(TimeSpan.TicksPerSecond * 3, travel.RemainingEdgeTicks);
            Assert.Equal(before.Programs[0].DistrictId, restored.GetSnapshot().Programs[0].DistrictId);
            restored.Step();
            Assert.Equal(ResidentAction.Unknown, restored.GetSnapshot().Programs[0].Behavior!.Action);
            Assert.Equal(travel, restored.GetSnapshot().Programs[0].Behavior!.Travel);
            File.WriteAllText(path, "damaged"); reopened.Recover();
            Assert.Equal(travel.Route, reopened.Load().State.Programs.Single().Behavior!.Travel!.Route);
        }
        finally { Directory.Delete(folder, true); }
    }
}
