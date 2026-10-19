namespace Grid.Simulation.Resources;

public enum ResourceMetric
{
    CpuAvailablePercent, RamAvailableBytes, RamCapacityBytes, GpuAvailablePercent, VramAvailableBytes,
    StorageAvailableBytes, StorageCapacityBytes, StorageReadBytesPerSecond, StorageWriteBytesPerSecond,
    NetworkReceiveBytesPerSecond, NetworkSendBytesPerSecond, NetworkConnected
}
public enum ObservationStatus { Available, WarmingUp, Unsupported, Unavailable, Stale }

public sealed record ResourceReading(ResourceMetric Metric, string Scope, double? Value,
    ObservationStatus Status, DateTimeOffset ObservedAtUtc, string Source, string Detail = "")
{
    public string Unit => Metric switch
    {
        ResourceMetric.CpuAvailablePercent or ResourceMetric.GpuAvailablePercent => "percent",
        ResourceMetric.NetworkConnected => "boolean",
        ResourceMetric.StorageReadBytesPerSecond or ResourceMetric.StorageWriteBytesPerSecond or
            ResourceMetric.NetworkReceiveBytesPerSecond or ResourceMetric.NetworkSendBytesPerSecond => "bytes/s",
        _ => "bytes"
    };

    public ResourceReading At(DateTimeOffset now, TimeSpan maximumAge) =>
        Status == ObservationStatus.Available && (now < ObservedAtUtc || now - ObservedAtUtc > maximumAge)
            ? this with { Value = null, Status = ObservationStatus.Stale, Detail = "Observation expired or clock moved backwards." } : this;

    public void Validate()
    {
        if (!Enum.IsDefined(Metric) || !Enum.IsDefined(Status) || string.IsNullOrWhiteSpace(Scope) ||
            string.IsNullOrWhiteSpace(Source) || ObservedAtUtc.Offset != TimeSpan.Zero ||
            (Status == ObservationStatus.Available) != Value.HasValue ||
            Value is { } value && (!double.IsFinite(value) || value < 0 ||
                Unit == "percent" && value > 100 || Unit == "boolean" && value is not (0 or 1)))
            throw new ArgumentException("Invalid resource observation.");
    }
}

public interface IHostTelemetryProvider
{
    Task<IReadOnlyList<ResourceReading>> SampleAsync(CancellationToken cancellationToken);
}

public interface IHostTelemetryMonitor
{
    Task RunAsync(CancellationToken cancellationToken);
}

/// <summary>Headroom required for additional work, in the metric's units; not inferred minimum app requirements.</summary>
public sealed record ResourceRequirement(ResourceMetric Metric, string Scope, double Required, string Provenance);
public sealed record ResourceCondition(double? Score, string Explanation);

/// <summary>GRID interpretation v1: weakest required headroom ratio, EMA 0.25; unknown input resets smoothing.</summary>
public sealed class ResourceConditionPolicy
{
    private double? previous;
    private ResourceRequirement[]? previousRequirements;
    private ResourceReading[]? previousReadings;
    public ResourceCondition Evaluate(IReadOnlyList<ResourceReading> readings, IReadOnlyList<ResourceRequirement> requirements,
        DateTimeOffset now)
    {
        var copy = requirements.ToArray();
        if (copy.Length == 0) return Unknown("No explicit resource requirements are known.");
        if (copy.Select(r => (r.Metric, r.Scope)).Distinct().Count() != copy.Length)
            throw new ArgumentException("Duplicate requirements.");
        foreach (var r in copy)
            if (r.Metric is not (ResourceMetric.CpuAvailablePercent or ResourceMetric.RamAvailableBytes or
                ResourceMetric.GpuAvailablePercent or ResourceMetric.VramAvailableBytes or ResourceMetric.StorageAvailableBytes or ResourceMetric.NetworkConnected)
                || !double.IsFinite(r.Required) || r.Required <= 0 || string.IsNullOrWhiteSpace(r.Provenance) || string.IsNullOrWhiteSpace(r.Scope)
                || r.Metric is ResourceMetric.CpuAvailablePercent or ResourceMetric.GpuAvailablePercent && r.Required > 100
                || r.Metric == ResourceMetric.NetworkConnected && r.Required != 1)
                throw new ArgumentException("Requirements must specify valid, sourced headroom in matching units.");
        if (previousRequirements is null || !previousRequirements.SequenceEqual(copy)) { previous = null; previousReadings = null; }
        previousRequirements = copy;
        var score = 100d;
        var selected = new List<ResourceReading>();
        foreach (var requirement in copy)
        {
            var matches = readings.Where(r => r.Metric == requirement.Metric && r.Scope == requirement.Scope).ToArray();
            if (matches.Length != 1) return Unknown("Required observation is missing or ambiguous.");
            matches[0].Validate();
            var reading = matches[0].At(now, TimeSpan.FromSeconds(10));
            if (reading.Value is not { } value) return Unknown($"{requirement.Metric}: {reading.Status}.");
            if (previousReadings?.Any(r => r.Metric == reading.Metric && r.Scope == reading.Scope && r.ObservedAtUtc > reading.ObservedAtUtc) == true)
                return Unknown("Out-of-order observations.");
            selected.Add(reading);
            score = Math.Min(score, Math.Clamp(value / requirement.Required, 0, 1) * 100);
        }
        if (previous is not null && previousReadings is not null && previousReadings.SequenceEqual(selected))
            return new(previous, "Same observation; no smoothing advancement.");
        previousReadings = selected.ToArray();
        previous = previous is { } old ? Math.Clamp(old + 0.25 * (score - old), 0, 100) : score;
        return new(previous, "GRID headroom interpretation v1; not measured application health or allocated resources.");
    }

    private ResourceCondition Unknown(string reason) { previous = null; previousRequirements = null; previousReadings = null; return new(null, reason); }
}
