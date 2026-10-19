using System.Text.Json;

namespace Grid.Simulation;

public sealed record GridHistoryEvent(Guid Id, DateTimeOffset RecordedAtUtc, long Tick,
    TimeSpan SimulationTime, string Kind, string Severity, Guid? EntityId, string? EntityName,
    string Detail, string ContextJson);
public sealed record GridHistoryBatch(GridSimulationSnapshot State, IReadOnlyList<GridHistoryEvent> Events,
    long ThroughSequence, long DroppedCount);

public sealed partial class GridSimulationEngine
{
    private readonly Queue<(long Sequence, GridHistoryEvent Event)> pendingHistory = new();
    private long historyDropped;
    private long acknowledgedDropped;
    private Guid gapId = Guid.NewGuid();

    public GridHistoryBatch CaptureHistory()
    {
        lock (gate)
        {
            var events = pendingHistory.Select(x => x.Event).ToList();
            if (historyDropped > acknowledgedDropped)
                events.Add(new(gapId, timeProvider.GetUtcNow(), clock.TickCount, clock.ElapsedSimulationTime,
                    "HistoryGap", "Warning", null, null, "Pending history capacity exceeded.",
                    JsonSerializer.Serialize(new { dropped = historyDropped - acknowledgedDropped })));
            return new(GetSnapshot(), events.AsReadOnly(), eventSequence, historyDropped);
        }
    }

    /// <summary>Acknowledge only after the captured state and history have committed successfully.</summary>
    public void AcknowledgeHistory(GridHistoryBatch batch)
    {
        lock (gate)
        {
            while (pendingHistory.TryPeek(out var item) && item.Sequence <= batch.ThroughSequence) pendingHistory.Dequeue();
            acknowledgedDropped = Math.Max(acknowledgedDropped, batch.DroppedCount);
        }
    }

    private void QueueHistory(string kind, Guid? entityId, string? name, string detail, string? context)
    {
        if (kind == "Tick") return;
        if (pendingHistory.Count == 10000)
        {
            pendingHistory.Dequeue(); historyDropped++; gapId = Guid.NewGuid();
        }
        pendingHistory.Enqueue((eventSequence, new(Guid.NewGuid(), timeProvider.GetUtcNow(), clock.TickCount,
            clock.ElapsedSimulationTime, kind, "Information", entityId, name, detail,
            context ?? JsonSerializer.Serialize(new { engineInstanceId = world.InstanceId }))));
    }

    /// <summary>Authoritative fictional resource update; does not allocate host resources.</summary>
    public void SetDistrictResource(Guid districtId, string key, float amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!float.IsFinite(amount)) throw new ArgumentOutOfRangeException(nameof(amount));
        lock (gate)
        {
            var district = world.GetDistrict(districtId);
            float? previous = district.Resources.TryGetValue(key, out var old) ? old : null;
            if (previous == amount) return;
            district.Resources[key] = amount;
            RecordEvent("Resource", districtId, district.Name, $"{key} changed.",
                JsonSerializer.Serialize(new { entityType = "District", resource = key, before = previous, after = amount }));
        }
    }
}
