using System.Threading.Channels;
using Grid.Core;
using Xunit;

namespace Grid.Simulation.Tests;

public sealed class GridSimulationEngineTests
{
    private static (GridWorld World, GridProgram Program) CreateWorld(
        GridProgramStatus status = GridProgramStatus.Working, float energy = 50)
    {
        var world = new GridWorld();
        var district = new GridDistrict();
        world.AddDistrict(district);
        var program = new GridProgram { Status = status, Energy = energy };
        world.AddProgram(program, district.Id);
        return (world, program);
    }

    [Fact]
    public void PausedEngineDoesNotUpdateWorldClockOrEvents()
    {
        var (world, program) = CreateWorld();
        var engine = new GridSimulationEngine(world);
        Assert.Null(engine.Tick());
        Assert.Equal(50f, program.Energy);
        Assert.Equal(0, engine.TickCount);
        Assert.Null(engine.LastTick);
    }

    [Theory]
    [InlineData(GridProgramStatus.Working, 49.5f)]
    [InlineData(GridProgramStatus.Moving, 49.5f)]
    [InlineData(GridProgramStatus.Recharging, 51f)]
    [InlineData(GridProgramStatus.Idle, 50f)]
    [InlineData(GridProgramStatus.Offline, 50f)]
    public void TickAppliesStatusRateAndReportsOnlyRealChanges(GridProgramStatus status, float expected)
    {
        var (world, program) = CreateWorld(status);
        var engine = new GridSimulationEngine(world, TimeSpan.FromMilliseconds(500));
        engine.Start();
        var result = engine.Tick()!;
        Assert.Equal(expected, program.Energy);
        Assert.Equal(1, result.Tick);
        Assert.Equal(TimeSpan.FromMilliseconds(500), result.SimulationTime);
        Assert.Equal(result.Tick, engine.TickCount);
        Assert.Same(result, engine.LastTick);
        var completed = Assert.IsType<SimulationTickCompleted>(result.Events.Last());
        Assert.Equal(1, completed.ProgramCount);
        Assert.Equal(1, completed.DistrictCount);
        if (expected == 50)
            Assert.Single(result.Events);
        else
        {
            Assert.Equal(2, result.Events.Count);
            var change = Assert.IsType<ProgramEnergyChanged>(result.Events.First());
            Assert.Equal(program.Id, change.ProgramId);
            Assert.Equal(50f, change.PreviousEnergy);
            Assert.Equal(expected, change.Energy);
        }
        Assert.All(result.Events, e =>
        {
            Assert.Equal(result.Tick, e.Tick);
            Assert.Equal(result.SimulationTime, e.SimulationTime);
        });
    }

    [Theory]
    [InlineData(GridProgramStatus.Working, 0.25f, 0f)]
    [InlineData(GridProgramStatus.Recharging, 99f, 100f)]
    public void EnergyClampsAndStopsEmittingChangesAtBoundary(GridProgramStatus status, float initial, float expected)
    {
        var (world, program) = CreateWorld(status, initial);
        var engine = new GridSimulationEngine(world);
        engine.Step();
        Assert.Equal(expected, program.Energy);
        Assert.Single(engine.Step().Events);
    }

    [Fact]
    public void StepPauseAndResumePreserveCountsAndPopulation()
    {
        var world = GridWorldFactory.CreateGenesis();
        var engine = new GridSimulationEngine(world);
        engine.Step();
        Assert.False(engine.IsRunning);
        engine.Start();
        engine.Start();
        engine.Tick();
        Assert.Throws<InvalidOperationException>(() => engine.Step());
        engine.Pause();
        engine.Pause();
        Assert.Null(engine.Tick());
        engine.Step();
        engine.Start();
        engine.Tick();
        Assert.Equal(4, engine.TickCount);
        Assert.Equal(TimeSpan.FromSeconds(4), engine.ElapsedSimulationTime);
        Assert.Equal(20, world.Programs.Count);
        Assert.Equal(20, world.Districts.Values.Sum(d => d.ProgramIds.Count));
    }

    [Fact]
    public void EventsAreImmutableSnapshotsInStableIdentityOrder()
    {
        var world = GridWorldFactory.CreateGenesis();
        foreach (var program in world.Programs.Values)
            program.Status = GridProgramStatus.Working;
        var engine = new GridSimulationEngine(world);
        var first = engine.Step();
        var changes = first.Events.OfType<ProgramEnergyChanged>().ToArray();
        Assert.Equal(world.Programs.Keys.Order(), changes.Select(e => e.ProgramId));
        engine.Step();
        Assert.All(changes, e => Assert.Equal(99f, e.Energy));
        Assert.Throws<NotSupportedException>(() => ((IList<GridSimulationEvent>)first.Events).Clear());
    }

    [Fact]
    public void EqualSeedsAndCommandsProduceEqualResults()
    {
        var firstWorld = GridWorldFactory.CreateGenesis(12);
        var secondWorld = GridWorldFactory.CreateGenesis(12);
        foreach (var program in firstWorld.Programs.Values.Concat(secondWorld.Programs.Values))
            program.Status = GridProgramStatus.Working;
        var first = new GridSimulationEngine(firstWorld);
        var second = new GridSimulationEngine(secondWorld);
        for (var i = 0; i < 10; i++)
            Assert.Equal(first.Step().Events, second.Step().Events);
    }

    [Fact]
    public void InvalidProgramPreventsAnyPartialUpdate()
    {
        var world = GridWorldFactory.CreateGenesis();
        var programs = world.Programs.Values.OrderBy(p => p.Id).ToArray();
        programs[0].Status = GridProgramStatus.Working;
        programs[^1].Energy = float.NaN;
        var engine = new GridSimulationEngine(world);
        Assert.Throws<InvalidOperationException>(() => engine.Step());
        Assert.Equal(100f, programs[0].Energy);
        Assert.Equal(0, engine.TickCount);
        Assert.Null(engine.LastTick);
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(101f)]
    [InlineData(float.PositiveInfinity)]
    public void OutOfRangeEnergyIsRejected(float energy)
    {
        var (world, _) = CreateWorld(energy: energy);
        Assert.Throws<InvalidOperationException>(() => new GridSimulationEngine(world).Step());
    }

    [Fact]
    public void InvalidStatusAndNullWorldAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new GridSimulationEngine(null!));
        var (world, _) = CreateWorld((GridProgramStatus)99);
        Assert.Throws<InvalidOperationException>(() => new GridSimulationEngine(world).Step());
    }

    [Fact]
    public void CancellationAndOverflowLeaveWorldAndEventsUnchanged()
    {
        var (world, program) = CreateWorld();
        var engine = new GridSimulationEngine(world, TimeSpan.MaxValue);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => engine.Step(cancellation.Token));
        engine.Start();
        Assert.Throws<OperationCanceledException>(() => engine.Tick(cancellation.Token));
        Assert.Equal(50f, program.Energy);
        Assert.Null(engine.LastTick);
        engine.Pause();
        var first = engine.Step();
        program.Energy = 50;
        Assert.Throws<OverflowException>(() => engine.Step());
        Assert.Equal(50f, program.Energy);
        Assert.Equal(1, engine.TickCount);
        Assert.Same(first, engine.LastTick);
    }

    [Fact]
    public void EmptyWorldStillReportsAnActualCompletedTick()
    {
        var engine = new GridSimulationEngine(new GridWorld());
        var completed = Assert.IsType<SimulationTickCompleted>(Assert.Single(engine.Step().Events));
        Assert.Equal(0, completed.ProgramCount);
        Assert.Equal(0, completed.DistrictCount);
    }

    [Fact]
    public async Task SchedulerTicksOncePerWaitAndCancelsCleanly()
    {
        var (world, program) = CreateWorld();
        var time = new ManualTimeProvider();
        var engine = new GridSimulationEngine(world, timeProvider: time);
        using var cancellation = new CancellationTokenSource();
        engine.Start();
        var run = engine.RunAsync(cancellation.Token);
        var firstWait = await time.Next();
        Assert.Equal(TimeSpan.FromSeconds(1), firstWait.DueTime);
        Assert.Equal(0, engine.TickCount);
        firstWait.Fire();
        var secondWait = await time.Next();
        Assert.Equal(1, engine.TickCount);
        Assert.Equal(49f, program.Energy);
        cancellation.Cancel();
        await run.WaitAsync(TimeSpan.FromSeconds(5));
        secondWait.Fire();
        Assert.Equal(1, engine.TickCount);
        Assert.False(engine.IsRunning);
        engine.Step(); // Runner ownership has been released.
        Assert.Equal(2, engine.TickCount);
    }

    [Fact]
    public async Task PauseResumeDiscardsPendingWaitAndManualStepRemainsAvailable()
    {
        var (world, _) = CreateWorld();
        var time = new ManualTimeProvider();
        var engine = new GridSimulationEngine(world, timeProvider: time);
        using var cancellation = new CancellationTokenSource();
        engine.Start();
        var run = engine.RunAsync(cancellation.Token);
        try
        {
            var pending = await time.Next();
            engine.Pause();
            pending.Fire();
            var pausedWait = await time.Next();
            Assert.Equal(0, engine.TickCount);
            engine.Step();
            engine.Start();
            pausedWait.Fire();
            var resumedWait = await time.Next();
            Assert.Equal(1, engine.TickCount); // No stale scheduled tick after resume.
            resumedWait.Fire();
            await time.Next();
            Assert.Equal(2, engine.TickCount);
        }
        finally
        {
            cancellation.Cancel();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task DuplicateRunnerAndExternalTicksAreRejected()
    {
        var engine = new GridSimulationEngine(new GridWorld(), timeProvider: new ManualTimeProvider());
        using var cancellation = new CancellationTokenSource();
        var run = engine.RunAsync(cancellation.Token);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunAsync(cancellation.Token));
            Assert.Throws<InvalidOperationException>(() => engine.Tick());
        }
        finally
        {
            cancellation.Cancel();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task SchedulerFailurePausesAndReleasesOwnershipForRecovery()
    {
        var (world, program) = CreateWorld(energy: float.NaN);
        var time = new ManualTimeProvider();
        var engine = new GridSimulationEngine(world, timeProvider: time);
        engine.Start();
        var run = engine.RunAsync(CancellationToken.None);
        (await time.Next()).Fire();
        await Assert.ThrowsAsync<InvalidOperationException>(() => run.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.False(engine.IsRunning);
        Assert.Equal(0, engine.TickCount);
        program.Energy = 50;
        engine.Step();
        Assert.Equal(1, engine.TickCount);
    }

    [Fact]
    public async Task AlreadyCancelledSchedulerDoesNotAdvanceAndCanRestart()
    {
        var time = new ManualTimeProvider();
        var engine = new GridSimulationEngine(new GridWorld(), timeProvider: time);
        engine.Start();
        await engine.RunAsync(new CancellationToken(true));
        Assert.Equal(0, engine.TickCount);
        Assert.False(engine.IsRunning);
        using var cancellation = new CancellationTokenSource();
        engine.Start();
        var run = engine.RunAsync(cancellation.Token);
        await time.Next();
        cancellation.Cancel();
        await run.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UnsupportedSchedulerIntervalDoesNotPreventManualSteps()
    {
        var engine = new GridSimulationEngine(new GridWorld(), TimeSpan.FromTicks(1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunAsync(CancellationToken.None));
        engine.Step();
        Assert.Equal(TimeSpan.FromTicks(1), engine.ElapsedSimulationTime);
    }

    // Explicitly release scheduled delays: no sleeps or dependence on wall-clock timing.
    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly Channel<ManualTimer> timers = Channel.CreateUnbounded<ManualTimer>();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(callback, state, dueTime);
            timers.Writer.TryWrite(timer);
            return timer;
        }

        public async Task<ManualTimer> Next() =>
            await timers.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class ManualTimer(TimerCallback callback, object? state, TimeSpan dueTime) : ITimer
    {
        private int disposed;
        public TimeSpan DueTime { get; } = dueTime;
        public void Fire()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
                callback(state);
        }
        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
        public void Dispose() => Interlocked.Exchange(ref disposed, 1);
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
