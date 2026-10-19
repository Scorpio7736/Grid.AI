using Grid.Core;
using Grid.Simulation.Resources;
namespace Grid.Simulation.Applications;

public sealed record ObservedApplication(string Key, string Name, IReadOnlyList<ProcessLifetime> Processes, long? WorkingSetBytes);
public sealed record ApplicationObservation(DateTimeOffset ObservedAtUtc, bool Complete,
    IReadOnlyList<ObservedApplication> Applications, string Source)
{
    public IReadOnlyList<ProcessLifetime> ConfirmedExited { get; init; } = [];
}
public interface IApplicationDiscovery
{
    Task<ApplicationObservation> SampleAsync(CancellationToken cancellationToken);
}

public sealed class ApplicationMonitor(IApplicationDiscovery provider, GridSimulationEngine engine,
    TimeProvider? timeProvider = null) : IHostTelemetryMonitor
{
    public async Task SampleOnceAsync(CancellationToken token = default)
    {
        try { engine.ReconcileApplications(await provider.SampleAsync(token).ConfigureAwait(false)); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        { engine.ReconcileApplications(new((timeProvider ?? TimeProvider.System).GetUtcNow(), false, [], ex.GetType().Name)); }
    }
    public async Task RunAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2), timeProvider ?? TimeProvider.System);
        try { while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false)) await SampleOnceAsync(token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
}

public sealed class CombinedHostMonitor(params IHostTelemetryMonitor[] monitors) : IHostTelemetryMonitor
{
    public async Task RunAsync(CancellationToken token)
    {
        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(token);
        var tasks = monitors.Select(m => m.RunAsync(shutdown.Token)).ToArray();
        try { await await Task.WhenAny(tasks).ConfigureAwait(false); }
        finally { shutdown.Cancel(); await Task.WhenAll(tasks).ConfigureAwait(false); }
    }
}
