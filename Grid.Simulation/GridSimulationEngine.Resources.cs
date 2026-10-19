using Grid.Simulation.Resources;

namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private IReadOnlyList<ResourceReading> hostResources = Array.Empty<ResourceReading>();

    public IReadOnlyList<ResourceReading> GetHostResources()
    {
        lock (gate) return Array.AsReadOnly(hostResources.Select(r => r.At(timeProvider.GetUtcNow(), TimeSpan.FromSeconds(10))).ToArray());
    }

    public void UpdateHostResources(IReadOnlyList<ResourceReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);
        var copy = readings.ToArray();
        if (copy.Length > 512 || copy.Select(r => (r.Metric, r.Scope)).Distinct().Count() != copy.Length)
            throw new ArgumentException("Too many or duplicate resource observations.");
        foreach (var reading in copy) reading.Validate();
        lock (gate) hostResources = Array.AsReadOnly(copy);
    }

    public void MarkHostResourcesUnavailable(string reason)
    {
        lock (gate) hostResources = Array.AsReadOnly(hostResources.Select(r => r with
            { Value = null, Status = ObservationStatus.Unavailable, Detail = reason }).ToArray());
    }
}
