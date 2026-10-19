using Grid.Core;
using Grid.Simulation.Behavior;
using Grid.Simulation.Resources;
using Xunit;
namespace Grid.Simulation.Tests;

public sealed class ResidentBehaviorTests
{
    private static WorkRoutine Routine => new(RoutineOccupation.Archivist, TimeSpan.FromSeconds(2).Ticks,
        TimeSpan.FromSeconds(2).Ticks, TimeSpan.FromSeconds(1).Ticks, 0, 24,
        [new(ResourceMetric.RamAvailableBytes, "host", 100, "Configured test work budget")]);
    private static ResidentBehavior Next(ResidentBehavior b, double? condition = 100, int seconds = 1) =>
        ResidentBehaviorPolicy.Next(ResidentKind.Agent, b, false, condition, TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(1));

    [Fact]
    public void DeterministicWorkRestCycleCountsUnitsWithoutEnergyDrain()
    {
        var start = ResidentBehavior.Initial with { Routine = Routine };
        var a = Next(start); var b = Next(a); var c = Next(b); var d = Next(c); var e = Next(d);
        Assert.Equal(ResidentAction.Working, a.Action); Assert.Equal(1, a.CompletedUnits);
        Assert.Equal(2, b.CompletedUnits); Assert.Equal(Routine.RestTicks, b.RestRemainingTicks);
        Assert.Equal(ResidentAction.Resting, c.Action); Assert.Equal(2, d.CompletedUnits);
        Assert.Equal(ResidentAction.Working, e.Action); Assert.Equal(3, e.CompletedUnits);
        Assert.Equal(a, Next(start)); Assert.Contains("archive", a.Goal);
    }
    [Fact]
    public void UnknownAndShortageFreezeProgressWithHysteresis()
    {
        var initial = ResidentBehavior.Initial with { Routine = Routine };
        Assert.Equal(ResidentAction.Unknown, Next(initial, null).Action);
        Assert.Equal(ResidentAction.WaitingForResources, Next(initial, 59).Action);
        var working = Next(initial, 60);
        Assert.Equal(ResidentAction.Working, Next(working, 40).Action);
        var waiting = Next(working, 39);
        Assert.Equal(working.WorkTicks, waiting.WorkTicks);
        Assert.Equal(ResidentAction.WaitingForResources, Next(waiting, 59).Action);
        Assert.Equal(ResidentAction.Working, Next(waiting, 60).Action);
    }
    [Fact]
    public void ShiftWrapsMidnightAndUsesSimulationTimeOnly()
    {
        var b = ResidentBehavior.Initial with { Routine = Routine with { ShiftStartHour = 22, ShiftHours = 4 } };
        ResidentBehavior At(int hour) => ResidentBehaviorPolicy.Next(ResidentKind.Special, b, false, 100, TimeSpan.FromHours(hour), TimeSpan.FromSeconds(1));
        Assert.Equal(ResidentAction.Working, At(23).Action); Assert.Equal(ResidentAction.Working, At(25).Action);
        Assert.Equal(ResidentAction.OffDuty, At(26).Action); Assert.Equal(ResidentAction.OffDuty, At(21).Action);
    }
    [Fact]
    public void ApplicationsOnlyObserveAndNeverAcquireFictionalWork()
    {
        var observed = ResidentBehaviorPolicy.Next(ResidentKind.Application, ResidentBehavior.Initial, true, 100, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        var stale = ResidentBehaviorPolicy.Next(ResidentKind.Application, observed, false, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        Assert.Equal(ResidentAction.Observing, observed.Action); Assert.Equal(ResidentAction.Unknown, stale.Action);
        Assert.Equal(0, stale.CompletedUnits); Assert.Null(stale.Routine);
        Assert.Throws<InvalidDataException>(() => ResidentBehavior.Validate(ResidentBehavior.Initial with { Routine = Routine }, ResidentKind.Application));
    }
    [Fact]
    public void InvalidRoutineProgressAndUnknownEnumsAreRejected()
    {
        var b = ResidentBehavior.Initial with { Routine = Routine };
        Assert.Throws<InvalidDataException>(() => ResidentBehavior.Validate(b with { Routine = Routine with { RestTicks = 0 } }, ResidentKind.Agent));
        Assert.Throws<InvalidDataException>(() => ResidentBehavior.Validate(b with { WorkTicks = -1 }, ResidentKind.Agent));
        Assert.Throws<InvalidDataException>(() => ResidentBehavior.Validate(b with { Action = (ResidentAction)999 }, ResidentKind.Agent));
        Assert.Throws<InvalidDataException>(() => ResidentBehavior.Validate(b with { RestRemainingTicks = 1 }, ResidentKind.Agent));
    }
    [Fact]
    public void EngineCommitsProgressHistoryAndRevisionsButSnapshotReadsAndCancelledTicksDoNotAdvance()
    {
        var world = new GridWorld(); var d = new GridDistrict { Name = "Test" }; world.AddDistrict(d);
        var agent = new GridProgram { Name = "Agent", Occupation = "Unassigned", Kind = ResidentKind.Agent }; world.AddProgram(agent, d.Id);
        var engine = new GridSimulationEngine(world); engine.ConfigureRoutine(agent.Id, Routine);
        engine.UpdateHostResources([new(ResourceMetric.RamAvailableBytes, "host", 100, ObservationStatus.Available, DateTimeOffset.UtcNow, "Fake")]);
        var before = engine.GetSnapshot().Programs[0]; _ = engine.GetSnapshot();
        Assert.Throws<OperationCanceledException>(() => engine.Step(new CancellationToken(true)));
        Assert.Equal(before.Behavior, engine.GetSnapshot().Programs[0].Behavior);
        engine.Step(); var after = engine.GetSnapshot().Programs[0];
        Assert.Equal(1, after.Behavior!.CompletedUnits); Assert.Equal(before.Energy, after.Energy); Assert.True(after.Revision > before.Revision);
        Assert.Contains(engine.CaptureHistory().Events, e => e.Kind == "WorkCompleted");
        Assert.Throws<InvalidOperationException>(() => engine.SetResidentRequirements(agent.Id, []));
    }
    [Fact]
    public void MissingBudgetsNeverProduceWorkAndNoRoutineRemainsIdle()
    {
        var world = new GridWorld(); var d = new GridDistrict { Name = "Test" }; world.AddDistrict(d);
        var agent = new GridProgram { Name = "Agent", Occupation = "Unassigned", Kind = ResidentKind.Special }; world.AddProgram(agent, d.Id);
        var engine = new GridSimulationEngine(world); engine.Step(); Assert.Equal(ResidentAction.Idle, engine.GetSnapshot().Programs[0].Behavior!.Action);
        engine.ConfigureRoutine(agent.Id, Routine with { Requirements = [] }); engine.Step();
        Assert.Equal(ResidentAction.Unknown, engine.GetSnapshot().Programs[0].Behavior!.Action);
        Assert.Equal(0, engine.GetSnapshot().Programs[0].Behavior!.CompletedUnits);
    }
    [Fact]
    public void OverflowInAnyResidentAbortsWholeTickBeforeMutatingOthers()
    {
        var world = new GridWorld(); var d = new GridDistrict { Name = "Test" }; world.AddDistrict(d);
        foreach (var name in new[] { "First", "Second" }) world.AddProgram(new() { Name = name, Occupation = "Test", Kind = ResidentKind.Agent }, d.Id);
        var original = new GridSimulationEngine(world);
        foreach (var p in original.GetSnapshot().Programs) original.ConfigureRoutine(p.Id, Routine);
        var snapshot = original.GetSnapshot(); var programs = snapshot.Programs.ToArray();
        programs[1] = programs[1] with { Behavior = programs[1].Behavior! with { CompletedUnits = long.MaxValue } };
        var engine = GridSimulationEngine.Restore(snapshot with { Programs = programs });
        engine.UpdateHostResources([new(ResourceMetric.RamAvailableBytes, "host", 100, ObservationStatus.Available, DateTimeOffset.UtcNow, "Fake")]);
        Assert.Throws<OverflowException>(() => engine.Step()); Assert.Equal(0, engine.TickCount);
        Assert.Equal(0, engine.GetSnapshot().Programs[0].Behavior!.CompletedUnits);
    }
}
