using Grid.Core;
using Grid.Simulation.Editing;
using Grid.TestSupport;
using Xunit;

namespace Grid.Simulation.Tests;

public sealed class RecentEventTests
{
    [Fact]
    public void BufferRetainsEveryIntermediateTickUntilCapacityThenReportsLoss()
    {
        var world = TestEdit.Genesis(p => p.Status = GridProgramStatus.Working);
        var engine = new GridSimulationEngine(world, recentEventCapacity: 50);
        var empty = engine.GetLiveSnapshot();
        for (var i = 0; i < 3; i++) engine.Step();
        var live = engine.GetLiveSnapshot();
        Assert.Empty(empty.Events);
        Assert.Equal(3, live.State.TickCount);
        Assert.Equal(63, live.LatestSequence); // Twenty energy changes and one completion per tick.
        Assert.Equal(13, live.DroppedEvents);
        Assert.Equal(Enumerable.Range(14, 50).Select(n => (long)n), live.Events.Select(e => e.Sequence));
        Assert.Equal(new long[] { 1, 2, 3 }, live.Events.Where(e => e.Kind == "Tick").Select(e => e.Tick));
        Assert.All(live.State.Programs, p => Assert.Equal(97, p.Energy));
        Assert.All(live.Events, e => Assert.Equal(TimeSpan.FromSeconds(e.Tick), e.SimulationTime));
        Assert.Throws<NotSupportedException>(() => ((IList<GridRecentEvent>)live.Events).Clear());
        engine.Step();
        Assert.Equal(63, live.LatestSequence);
    }

    [Fact]
    public void IdleTicksDoNotFabricateActionsAndReadsDoNotAdvanceTime()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        engine.Step();
        var before = engine.GetLiveSnapshot();
        for (var i = 0; i < 10; i++) engine.GetLiveSnapshot();
        var after = engine.GetLiveSnapshot();
        Assert.Single(after.Events);
        Assert.Equal("Tick", after.Events[0].Kind);
        Assert.Equal(before.LatestSequence, after.LatestSequence);
        Assert.Equal(1, after.State.TickCount);
        Assert.False(after.State.IsRunning);
    }

    [Fact]
    public void TransferAndEditEventsDescribeCommittedChangesOnly()
    {
        var world = GridWorldFactory.CreateGenesis();
        var authority = new TestEditAuthority(world.InstanceId);
        var engine = new GridSimulationEngine(world, editAuthorizer: authority);
        var program = world.Programs.Values.First();
        var previous = program.CurrentDistrictId;
        var destination = world.Districts.Keys.First(id => id != previous);
        world.MoveProgram(program.Id, destination);
        world.MoveProgram(program.Id, destination); // No-op.
        Assert.Throws<KeyNotFoundException>(() => world.MoveProgram(program.Id, Guid.NewGuid()));
        var prepared = engine.PrepareEdit(authority.Session, new(program.Id, new[]
        {
            new ProgramFieldEdit("name", new TextValue("New Citizen")),
            new ProgramFieldEdit("status", new StatusValue(GridProgramStatus.Recharging)),
            new ProgramFieldEdit("district", new DistrictValue(previous))
        }));
        Assert.Single(engine.GetLiveSnapshot().Events); // Preview emits nothing.
        Assert.Equal(ProgramEditOutcome.Applied, engine.ApplyEdit(authority.Session, prepared.Preview!).Outcome);
        var events = engine.GetLiveSnapshot().Events;
        Assert.Equal(new[] { "Transfer", "Edit" }, events.Select(e => e.Kind));
        Assert.Contains(destination.ToString(), events[0].Detail);
        Assert.Equal(program.Id, events[1].ProgramId);
        Assert.Equal("New Citizen", events[1].ProgramName);
        Assert.Contains("Idle -> Recharging", events[1].Detail);
        Assert.Contains(previous.ToString(), events[1].Detail);
        Assert.Equal(0, engine.TickCount);
        Assert.Null(engine.LastTick);
    }

    [Fact]
    public void NoOpsDeniedCancelledInvalidAndStaleEditsProduceNoCommittedEvents()
    {
        var world = GridWorldFactory.CreateGenesis();
        var authority = new TestEditAuthority(world.InstanceId);
        var engine = new GridSimulationEngine(world, editAuthorizer: authority);
        var program = world.Programs.Values.First();
        ProgramEditRequest Request(float energy) => new(program.Id, new[] { new ProgramFieldEdit("energy", new EnergyValue(energy)) });
        var noOp = engine.PrepareEdit(authority.Session, Request(100)).Preview!;
        Assert.Equal(ProgramEditOutcome.NoChange, engine.ApplyEdit(authority.Session, noOp).Outcome);
        Assert.Equal(ProgramEditOutcome.Conflict, engine.ApplyEdit(authority.Session, noOp).Outcome);
        Assert.Equal(ProgramEditOutcome.ValidationFailed, engine.PrepareEdit(authority.Session, Request(-1)).Outcome);
        var preview = engine.PrepareEdit(authority.Session, Request(50)).Preview!;
        Assert.Equal(ProgramEditOutcome.Cancelled, engine.ApplyEdit(authority.Session, preview, new(true)).Outcome);
        authority.Revoke();
        Assert.Equal(ProgramEditOutcome.Forbidden, engine.ApplyEdit(authority.Session, preview).Outcome);
        Assert.Empty(engine.GetLiveSnapshot().Events);
    }

    [Fact]
    public void StartAndPauseRecordOnlyActualTransitions()
    {
        var engine = new GridSimulationEngine(new GridWorld());
        engine.Pause(); engine.Start(); engine.Start(); engine.Pause(); engine.Pause();
        var live = engine.GetLiveSnapshot();
        Assert.Equal(2, live.Events.Count);
        Assert.All(live.Events, e => Assert.Equal("Clock", e.Kind));
        Assert.False(live.State.IsRunning);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10001)]
    public void InvalidCapacityIsRejected(int capacity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridSimulationEngine(new GridWorld(), recentEventCapacity: capacity));

    [Fact]
    public async Task ConcurrentReadsSeeMatchingStateAndEventCommit()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), recentEventCapacity: 8);
        var writer = Task.Run(() => { for (var i = 0; i < 1000; i++) engine.Step(); });
        do
        {
            var live = engine.GetLiveSnapshot();
            Assert.Equal(live.State.TickCount, live.LatestSequence);
            if (live.Events.Count != 0) Assert.Equal(live.State.TickCount, live.Events[^1].Tick);
            Assert.Equal(live.LatestSequence, live.DroppedEvents + live.Events.Count);
        } while (!writer.IsCompleted);
        await writer;
    }
}
