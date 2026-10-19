namespace Grid.Simulation;

/// <summary>Committed, process-local event. Sequence is stable within this engine instance.</summary>
public sealed record GridRecentEvent(long Sequence, long Tick, TimeSpan SimulationTime,
    string Kind, Guid? ProgramId, string? ProgramName, string Detail);

/// <summary>State and retained events copied together. DroppedEvents makes bounded-history loss explicit.</summary>
public sealed record GridLiveSnapshot(GridSimulationSnapshot State, IReadOnlyList<GridRecentEvent> Events,
    long LatestSequence, long DroppedEvents)
{
    public IReadOnlyList<Resources.ResourceReading> HostResources { get; init; } = Array.Empty<Resources.ResourceReading>();
}

public sealed partial class GridSimulationEngine
{
    private readonly Queue<GridRecentEvent> recentEvents = new();
    private readonly int recentEventCapacity;
    private long eventSequence;
    private long droppedEvents;

    public GridLiveSnapshot GetLiveSnapshot()
    {
        lock (gate)
            return new(GetSnapshot(), Array.AsReadOnly(recentEvents.ToArray()), eventSequence, droppedEvents)
                { HostResources = GetHostResources() };
    }

    // All callers already own the shared world/engine lock; publish only committed changes.
    private void RecordEvent(string kind, Guid? programId, string? programName, string detail, string? context = null)
    {
        if (recentEvents.Count == recentEventCapacity)
        {
            recentEvents.Dequeue();
            droppedEvents++;
        }
        recentEvents.Enqueue(new(++eventSequence, clock.TickCount, clock.ElapsedSimulationTime,
            kind, programId, programName, detail));
        QueueHistory(kind, programId, programName, detail, context);
    }
}
