using Grid.Core;
using Grid.Core.Generation;
using Grid.Simulation.Behavior;
using Grid.Simulation.Resources;
using Xunit;
namespace Grid.Simulation.Tests;

public sealed class WorkplaceTests
{
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.Parse("2026-10-20T00:00:00Z");
    }
    private static WorkRoutine Routine => new(RoutineOccupation.Archivist, TimeSpan.TicksPerSecond * 20,
        TimeSpan.TicksPerSecond * 2, TimeSpan.TicksPerSecond, 0, 24,
        [new(ResourceMetric.RamAvailableBytes, "host", 100, "Test budget")]);
    private static (GridSimulationEngine Engine, Guid Agent) Fixture(TimeSpan? step = null)
    {
        var world = HardwareCityGenerator.Create(new(new Clock().GetUtcNow(), []), 42);
        var agent = new GridProgram { Name = "Agent", Kind = ResidentKind.Agent, Occupation = "Unassigned" };
        world.AddProgram(agent, world.City!.Read().Districts.Single(d => d.Category == HardwareKind.Network).Id);
        var engine = new GridSimulationEngine(world, step, new Clock());
        engine.ConfigureRoutine(agent.Id, Routine);
        Fresh(engine);
        return (engine, agent.Id);
    }
    private static void Fresh(GridSimulationEngine engine) =>
        engine.UpdateHostResources([new(ResourceMetric.RamAvailableBytes, "host", 100, ObservationStatus.Available, new Clock().GetUtcNow(), "Test")]);

    [Fact]
    public void RouteUsesSavedRoadsAndTravelPrecedesWorkWithConsistentMembership()
    {
        var (engine, id) = Fixture(); var station = engine.AssignWorkplace(id);
        var initial = engine.GetSnapshot().Programs.Single();
        var route = initial.Behavior!.Travel!.Route.ToArray();
        Assert.True(route.Length > 1);
        Assert.Equal(station.DistrictId, route[^1]);
        for (var i = 0; i < (route.Length - 1) * 5; i++)
        {
            engine.Step();
            var state = engine.GetSnapshot(); var p = state.Programs.Single();
            Assert.Equal(ResidentAction.Moving, p.Behavior!.Action); Assert.Equal(0, p.Behavior.CompletedUnits);
            Assert.Equal(route[(i + 1) / 5], p.DistrictId);
            Assert.Single(state.Districts, d => d.ProgramIds.Contains(id));
            GridSimulationEngine.ValidateSnapshot(state);
        }
        engine.Step();
        Assert.Equal(ResidentAction.Working, engine.GetSnapshot().Programs.Single().Behavior!.Action);
        Assert.Equal(1, engine.GetSnapshot().Programs.Single().Behavior!.CompletedUnits);
        Assert.Contains(engine.CaptureHistory().Events, e => e.Kind == "Movement");
    }

    [Fact]
    public void LargeTickCrossesAtMostOneEdgeAndDoesNotAlsoWork()
    {
        var (engine, id) = Fixture(TimeSpan.FromSeconds(60));
        engine.AssignWorkplace(id);
        var route = engine.GetSnapshot().Programs.Single().Behavior!.Travel!.Route;
        engine.Step();
        var p = engine.GetSnapshot().Programs.Single();
        Assert.Equal(route[1], p.DistrictId); Assert.Equal(0, p.Behavior!.CompletedUnits);
    }

    [Fact]
    public void MissingEvidenceAndCancellationDoNotAdvanceTravel()
    {
        var (engine, id) = Fixture(); engine.AssignWorkplace(id);
        var restored = GridSimulationEngine.Restore(engine.GetSnapshot(), new Clock());
        restored.Step();
        var p = restored.GetSnapshot().Programs.Single();
        Assert.Equal(ResidentAction.Unknown, p.Behavior!.Action);
        Assert.Equal(WorkplacePolicy.EdgeTicks, p.Behavior.Travel!.RemainingEdgeTicks);
        var before = restored.TickCount;
        Assert.Throws<OperationCanceledException>(() => restored.Step(new CancellationToken(true)));
        Assert.Equal(before, restored.TickCount);
        Fresh(restored); restored.Step();
        Assert.Equal(WorkplacePolicy.EdgeTicks - TimeSpan.TicksPerSecond,
            restored.GetSnapshot().Programs.Single().Behavior!.Travel!.RemainingEdgeTicks);
    }

    [Fact]
    public void AssignmentsAreCompatibleExclusiveAndReleaseAllowsReuse()
    {
        var (engine, id) = Fixture();
        var wrong = WorkplacePolicy.Stations(engine.GetSnapshot()).First(s => s.Occupation == RoutineOccupation.Technician);
        Assert.Throws<InvalidOperationException>(() => engine.AssignWorkplace(id, wrong.Id));
        var station = engine.AssignWorkplace(id);
        var snapshot = engine.GetSnapshot(); var other = snapshot.Programs.Single() with { Id = Guid.NewGuid(), Name = "Second",
            Behavior = ResidentBehavior.Initial with { Routine = Routine } };
        snapshot = snapshot with { Programs = [.. snapshot.Programs, other],
            Districts = snapshot.Districts.Select(d => d.Id == other.DistrictId ? d with { ProgramIds = [.. d.ProgramIds, other.Id] } : d).ToArray() };
        var restored = GridSimulationEngine.Restore(snapshot);
        Assert.Throws<InvalidOperationException>(() => restored.AssignWorkplace(other.Id, station.Id));
        restored.ReleaseWorkplace(id);
        Assert.Equal(station.Id, restored.AssignWorkplace(other.Id, station.Id).Id);
        Assert.Null(restored.GetSnapshot().Programs.Single(p => p.Id == id).Behavior!.Travel);
    }

    [Fact]
    public void RestoreRejectsForgedRoutesAndSharedWorkstations()
    {
        var (engine, id) = Fixture(); engine.AssignWorkplace(id); var state = engine.GetSnapshot();
        var p = state.Programs.Single();
        var invalid = p with { Behavior = p.Behavior! with { Travel = p.Behavior!.Travel! with { Route = [Guid.NewGuid()] } } };
        Assert.Throws<InvalidDataException>(() => GridSimulationEngine.Restore(state with { Programs = [invalid] }));
        invalid = p with { Behavior = p.Behavior! with { Travel = p.Behavior!.Travel! with { RemainingEdgeTicks = -1 } } };
        Assert.Throws<InvalidDataException>(() => GridSimulationEngine.Restore(state with { Programs = [invalid] }));
    }

    [Fact]
    public void LegacyWorldsDoNotInventRoadsAndUnconfiguredResidentsCannotBeAssigned()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var state = engine.GetSnapshot();
        Assert.Null(WorkplacePolicy.Route(state, state.Districts[0].Id, state.Districts[1].Id));
        Assert.Throws<InvalidOperationException>(() => engine.AssignWorkplace(state.Programs[0].Id));
        Assert.Equal(0, engine.TickCount);
    }

    [Fact]
    public void AnotherResidentsOverflowRollsBackAReadyMovementHop()
    {
        var (engine, id) = Fixture(); engine.AssignWorkplace(id);
        for (var i = 0; i < 4; i++) engine.Step();
        var before = engine.GetSnapshot();
        var other = before.Programs.Single() with { Id = Guid.NewGuid(), Name = "Overflow",
            Behavior = ResidentBehavior.Initial with { Routine = Routine, CompletedUnits = long.MaxValue } };
        var state = before with { Programs = [.. before.Programs, other],
            Districts = before.Districts.Select(d => d.Id == other.DistrictId ? d with { ProgramIds = [.. d.ProgramIds, other.Id] } : d).ToArray() };
        var restored = GridSimulationEngine.Restore(state, new Clock()); Fresh(restored);
        Assert.Throws<OverflowException>(() => restored.Step());
        Assert.Equal(before.TickCount, restored.TickCount);
        var agent = restored.GetSnapshot().Programs.Single(p => p.Id == id);
        Assert.Equal(before.Programs[0].DistrictId, agent.DistrictId);
        Assert.Equal(TimeSpan.TicksPerSecond, agent.Behavior!.Travel!.RemainingEdgeTicks);
        Assert.Empty(restored.CaptureHistory().Events);
    }

    [Fact]
    public void OffDutyFreezesTravelAndReconfigurationReleasesStation()
    {
        var (engine, id) = Fixture();
        engine.ConfigureRoutine(id, Routine with { ShiftStartHour = 12, ShiftHours = 1 });
        engine.AssignWorkplace(id); engine.Step();
        var p = engine.GetSnapshot().Programs.Single();
        Assert.Equal(ResidentAction.OffDuty, p.Behavior!.Action);
        Assert.Equal(WorkplacePolicy.EdgeTicks, p.Behavior.Travel!.RemainingEdgeTicks);
        engine.ConfigureRoutine(id, Routine);
        Assert.Null(engine.GetSnapshot().Programs.Single().Behavior!.Travel);
    }
}
