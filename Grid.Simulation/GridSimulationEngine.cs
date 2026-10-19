using Grid.Core;
using Grid.Simulation.Editing;

namespace Grid.Simulation;

/// <summary>
/// Serializes fixed-step updates to a world. While in use, callers must not mutate the supplied
/// world directly or share it with another engine. UI readers should use GetSnapshot for a consistent view.
/// No rendering, AI, host telemetry, or offline catch-up is involved.
/// </summary>
public sealed partial class GridSimulationEngine
{
    private readonly object gate;
    private readonly GridWorld world;
    private readonly GridClock clock;
    private readonly TimeProvider timeProvider;
    private bool loopActive;
    private long runVersion;
    private GridTickResult? lastTick;

    public GridSimulationEngine(GridWorld world, TimeSpan? tickDuration = null, TimeProvider? timeProvider = null, IProgramEditAuthorizer? editAuthorizer = null, ProgramEditFieldRegistry? editFields = null, int recentEventCapacity = 256)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (recentEventCapacity is < 1 or > 10000) throw new ArgumentOutOfRangeException(nameof(recentEventCapacity));
        this.recentEventCapacity = recentEventCapacity;
        this.world = world;
        gate = world.SyncRoot;
        this.editAuthorizer = editAuthorizer ?? new DenyProgramEdits();
        this.editFields = editFields ?? ProgramEditFieldRegistry.CreateDefault();
        clock = new GridClock(tickDuration);
        this.timeProvider = timeProvider ?? TimeProvider.System;
        world.ProgramMoved += (program, previous, destination) => RecordEvent("Transfer", program.Id, program.Name,
            $"{world.GetDistrict(previous).Name} ({previous}) -> {world.GetDistrict(destination).Name} ({destination})");
    }

    public TimeSpan TickDuration => clock.TickDuration;
    public long TickCount { get { lock (gate) return clock.TickCount; } }
    public TimeSpan ElapsedSimulationTime { get { lock (gate) return clock.ElapsedSimulationTime; } }
    public bool IsRunning { get { lock (gate) return clock.IsRunning; } }
    public GridTickResult? LastTick { get { lock (gate) return lastTick; } }

    /// <summary>Copies a consistent world view under the same lock used to commit ticks.</summary>
    public GridSimulationSnapshot GetSnapshot()
    {
        lock (gate)
        {
            var programs = world.Programs.Values.OrderBy(p => p.Name).ThenBy(p => p.Id)
                .Select(p => new GridProgramSnapshot(p.Id, p.Name, p.Occupation, p.Energy, p.CurrentDistrictId, p.Status, p.Revision))
                .ToArray();
            var districts = world.Districts.Values.OrderBy(d => d.Name).ThenBy(d => d.Id)
                .Select(d => new GridDistrictSnapshot(d.Id, d.Name, d.Description,
                    Array.AsReadOnly(d.ProgramIds.ToArray()),
                    new System.Collections.ObjectModel.ReadOnlyDictionary<string, float>(
                        new Dictionary<string, float>(d.Resources)))).ToArray();
            return new(clock.TickCount, clock.ElapsedSimulationTime, clock.TickDuration, clock.IsRunning,
                Array.AsReadOnly(programs), Array.AsReadOnly(districts)) { WorldId = world.Id };
        }
    }

    public void Start()
    {
        lock (gate)
        {
            if (!clock.IsRunning)
            {
                runVersion++;
                clock.Start();
                RecordEvent("Clock", null, null, "Simulation started.");
            }
        }
    }

    /// <summary>Returns after any in-progress tick finishes; prevents subsequent automatic updates.</summary>
    public void Pause()
    {
        lock (gate)
        {
            if (clock.IsRunning)
            {
                runVersion++;
                clock.Pause();
                RecordEvent("Clock", null, null, "Simulation paused.");
            }
        }
    }

    /// <summary>Advances one normal tick, or returns null while paused. Not allowed during RunAsync.</summary>
    public GridTickResult? Tick(CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (loopActive)
                throw new InvalidOperationException("RunAsync already owns normal tick advancement.");
            return clock.IsRunning ? Advance(false, cancellationToken) : null;
        }
    }

    /// <summary>Explicitly advances one tick while paused, without starting the clock.</summary>
    public GridTickResult Step(CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (clock.IsRunning)
                throw new InvalidOperationException("Pause the engine before manually stepping.");
            return Advance(true, cancellationToken);
        }
    }

    /// <summary>
    /// Runs the scheduler until cancellation, completing normally on requested cancellation.
    /// Call Start to enable ticks; Pause leaves the scheduler alive for a later resume.
    /// Each delay permits at most one fixed step; delayed execution never catches up missed time.
    /// Await the returned task after cancellation before releasing the world.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            if (loopActive)
                throw new InvalidOperationException("Only one scheduler may run at a time.");
            // Task.Delay supports this range. Larger simulation steps remain usable via Tick/Step.
            if (TickDuration.TotalMilliseconds < 1 || TickDuration.TotalMilliseconds > uint.MaxValue - 1)
                throw new InvalidOperationException("Automatic scheduling requires a tick duration from 1 ms to 4294967294 ms.");
            loopActive = true;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                long version;
                lock (gate) version = runVersion;
                await Task.Delay(TickDuration, timeProvider, cancellationToken).ConfigureAwait(false);
                lock (gate)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // A pause/resume during the wait invalidates that wait; no stale tick is replayed.
                    if (clock.IsRunning && version == runVersion)
                        Advance(false, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation is a graceful scheduler shutdown, not a partially completed tick.
        }
        finally
        {
            lock (gate)
            {
                clock.Pause();
                runVersion++;
                loopActive = false;
            }
        }
    }

    private GridTickResult Advance(bool manual, CancellationToken cancellationToken)
    {
        // 1. Validate and stage all updates, using stable identity order.
        var nextTick = checked(clock.TickCount + 1);
        var nextTime = TimeSpan.FromTicks(checked(clock.ElapsedSimulationTime.Ticks + TickDuration.Ticks));
        var changes = new List<(GridProgram Program, float Energy)>();
        var events = new List<GridSimulationEvent>();
        foreach (var program in world.Programs.Values.OrderBy(p => p.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!float.IsFinite(program.Energy) || program.Energy < 0 || program.Energy > 100 ||
                !Enum.IsDefined(program.Status) ||
                !world.GetDistrict(program.CurrentDistrictId).ProgramIds.Contains(program.Id))
                throw new InvalidOperationException($"Invalid state for program {program.Id}.");

            // Minimal Genesis rule: rates per simulated second; no autonomous decisions yet.
            var rate = program.Status switch
            {
                GridProgramStatus.Working or GridProgramStatus.Moving => -1d,
                GridProgramStatus.Recharging => 2d,
                _ => 0d
            };
            var energy = (float)Math.Clamp(program.Energy + rate * TickDuration.TotalSeconds, 0d, 100d);
            if (energy != program.Energy)
            {
                _ = checked(program.Revision + 1);
                changes.Add((program, energy));
                events.Add(new ProgramEnergyChanged(nextTick, nextTime, program.Id, program.Energy, energy));
            }
        }
        events.Add(new SimulationTickCompleted(nextTick, nextTime, world.Programs.Count, world.Districts.Count));
        var result = new GridTickResult(nextTick, nextTime, events.AsReadOnly());

        // 2. Check cancellation once more before committing. Never interrupt a partial commit.
        cancellationToken.ThrowIfCancellationRequested();
        if (manual)
            clock.Step();
        else
            clock.TryAdvance();

        // 3. Apply staged changes, then expose events describing only the committed state.
        foreach (var change in changes)
            world.CommitProgram(change.Program, change.Program.Values with { Energy = change.Energy });
        lastTick = result;
        foreach (var change in events.OfType<ProgramEnergyChanged>())
            RecordEvent("Energy", change.ProgramId, world.GetProgram(change.ProgramId).Name,
                FormattableString.Invariant($"{change.PreviousEnergy:R} -> {change.Energy:R}"));
        RecordEvent("Tick", null, null, $"Tick completed; {changes.Count} energy changes.");
        return result;
    }
}
