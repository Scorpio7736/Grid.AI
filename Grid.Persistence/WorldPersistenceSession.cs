using Grid.Simulation;

namespace Grid.Persistence;

public interface IWorldPersistenceSession
{
    Task SaveAsync(CancellationToken token = default);
    Task RunPeriodicAsync(CancellationToken token);
}

/// <summary>Serializes snapshot capture and disk writes. The host stops the scheduler before its final save.</summary>
public sealed class WorldPersistenceSession(WorldStore store, WorldInfo info, GridSimulationEngine engine,
    TimeSpan? interval = null, TimeProvider? timeProvider = null) : IWorldPersistenceSession
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task SaveAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var batch = engine.CaptureHistory();
            await Task.Run(() => store.Save(info, batch.State, token, batch.Events), token).ConfigureAwait(false);
            engine.AcknowledgeHistory(batch);
        }
        finally { gate.Release(); }
    }

    public async Task RunPeriodicAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(interval ?? TimeSpan.FromSeconds(30), timeProvider ?? TimeProvider.System);
        try { while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false)) await SaveAsync(token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
}
