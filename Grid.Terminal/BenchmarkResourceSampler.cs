using System.Diagnostics;
using Grid.AI;

namespace Grid.Terminal;

public sealed record BenchmarkProcessCounter(int Id, long StartedTicks, double CpuMilliseconds);
public sealed class BenchmarkResourceTotals
{
    private Dictionary<(int, long), double> previous = new();
    public long? WorkingSetPeakBytes { get; private set; }
    public double? CpuMilliseconds { get; private set; }
    public long? ModelAllocationPeakBytes { get; private set; }
    public long? GpuAllocationPeakBytes { get; private set; }
    public void Observe(long? workingSet, IReadOnlyList<BenchmarkProcessCounter> processes, ModelResources? model)
    {
        if (workingSet is >= 0) WorkingSetPeakBytes = Math.Max(WorkingSetPeakBytes ?? 0, workingSet.Value);
        var current = processes.ToDictionary(p => (p.Id, p.StartedTicks), p => p.CpuMilliseconds);
        foreach (var (id, cpu) in current)
            if (previous.TryGetValue(id, out var prior) && cpu >= prior) CpuMilliseconds = (CpuMilliseconds ?? 0) + cpu - prior;
        previous = current;
        if (model?.AllocatedBytes is >= 0) ModelAllocationPeakBytes = Math.Max(ModelAllocationPeakBytes ?? 0, model.AllocatedBytes.Value);
        if (model?.GpuBytes is >= 0) GpuAllocationPeakBytes = Math.Max(GpuAllocationPeakBytes ?? 0, model.GpuBytes.Value);
    }
}

public sealed class BenchmarkResourceSampler : IAsyncDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly Task worker;
    public BenchmarkResourceTotals Totals { get; } = new();
    public BenchmarkResourceSampler(ILocalModelProvider provider, LocalModel model) => worker = RunAsync(provider, model);
    private async Task RunAsync(ILocalModelProvider provider, LocalModel model)
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                ModelResources? resource = null;
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop.Token); deadline.CancelAfter(TimeSpan.FromSeconds(2));
                try { resource = await provider.ResourcesAsync(model, deadline.Token); }
                catch (Exception e) when (e is IOException or HttpRequestException or OperationCanceledException or System.Text.Json.JsonException) { }
                var counters = new List<BenchmarkProcessCounter>(); long memory = 0;
                foreach (var process in Process.GetProcessesByName("ollama").Concat(Process.GetProcessesByName("llama-server")))
                {
                    using (process)
                    {
                        try { counters.Add(new(process.Id, process.StartTime.ToUniversalTime().Ticks, process.TotalProcessorTime.TotalMilliseconds)); memory += process.WorkingSet64; }
                        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException) { }
                    }
                }
                Totals.Observe(counters.Count == 0 ? null : memory, counters, resource);
                await Task.Delay(200, stop.Token);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { break; }
        }
    }
    public async ValueTask DisposeAsync() { stop.Cancel(); await worker; stop.Dispose(); }
}
