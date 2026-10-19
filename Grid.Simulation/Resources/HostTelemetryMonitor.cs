namespace Grid.Simulation.Resources;

/// <summary>Reads observations independently of simulation ticks. No Windows mutation capability.</summary>
public sealed class HostTelemetryMonitor(IHostTelemetryProvider provider, GridSimulationEngine engine,
    TimeProvider? timeProvider = null) : IHostTelemetryMonitor
{
    public async Task SampleOnceAsync(CancellationToken token = default)
    {
        try { engine.UpdateHostResources(await provider.SampleAsync(token).ConfigureAwait(false)); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        { engine.MarkHostResourcesUnavailable(ex.GetType().Name); }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2), timeProvider ?? TimeProvider.System);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                await SampleOnceAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
}
