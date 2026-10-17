namespace Grid.Simulation;

/// <summary>
/// Tracks fixed simulation steps, independently of wall time. Start enables advancement;
/// it does not start a timer. The simulation engine must call TryAdvance for each step.
/// Use from a single simulation thread.
/// </summary>
public sealed class GridClock
{
    public TimeSpan TickDuration { get; }
    public long TickCount { get; private set; }
    public TimeSpan ElapsedSimulationTime { get; private set; }
    public bool IsRunning { get; private set; }

    public GridClock(TimeSpan? tickDuration = null)
    {
        var duration = tickDuration ?? TimeSpan.FromSeconds(1);
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(tickDuration), "Tick duration must be positive.");
        TickDuration = duration;
    }

    /// <summary>Enables normal advancement without resetting or advancing simulation time.</summary>
    public void Start() => IsRunning = true;

    /// <summary>Disables normal advancement without resetting simulation time.</summary>
    public void Pause() => IsRunning = false;

    /// <summary>Advances exactly one tick when running; returns false without changes when paused.</summary>
    public bool TryAdvance()
    {
        if (!IsRunning)
            return false;
        AdvanceOneTick();
        return true;
    }

    /// <summary>Advances exactly one tick while paused and leaves the clock paused.</summary>
    /// <exception cref="InvalidOperationException">Pause the clock before manually stepping.</exception>
    public void Step()
    {
        if (IsRunning)
            throw new InvalidOperationException("Pause the clock before manually stepping.");
        AdvanceOneTick();
    }

    private void AdvanceOneTick()
    {
        // Compute both values before committing so overflow leaves the clock unchanged.
        var nextTick = checked(TickCount + 1);
        var nextElapsed = checked(ElapsedSimulationTime.Ticks + TickDuration.Ticks);
        TickCount = nextTick;
        ElapsedSimulationTime = TimeSpan.FromTicks(nextElapsed);
    }
}
