using Grid.Simulation;

namespace Grid.Persistence;

public interface IWorldPersistenceSession
{
    Task SaveAsync(CancellationToken token = default);
    Task RunPeriodicAsync(CancellationToken token);
}

public sealed record WorldRecoveryPlan(Guid WorldId, string DatabasePath, string PrimarySha256, string BackupSha256);
public interface IWorldRecoverySession
{
    WorldRecoveryPlan PrepareRecovery();
    Task RecoverAsync(WorldRecoveryPlan plan);
}

/// <summary>Serializes snapshot capture and disk writes. The host stops the scheduler before its final save.</summary>
public sealed class WorldPersistenceSession(WorldStore store, WorldInfo info, GridSimulationEngine engine,
    TimeSpan? interval = null, TimeProvider? timeProvider = null) : IWorldPersistenceSession, IWorldNullificationSession, IWorldRecoverySession
{
    private bool nullificationRequested;
    private bool recoveryRequested;
    public NullificationPlan PrepareNullification() => store.PrepareNullification(info.Id);
    public async Task NullifyAsync(NullificationPlan plan)
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try { nullificationRequested = true; store.Nullify(plan); }
        finally { gate.Release(); }
    }
    public WorldRecoveryPlan PrepareRecovery() => store.PrepareRecovery(info.Id);
    public async Task RecoverAsync(WorldRecoveryPlan plan)
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try { recoveryRequested = true; store.Recover(plan); }
        finally { gate.Release(); }
    }
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task SaveAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (nullificationRequested) throw new InvalidOperationException("Saving is disabled after NGE confirmation.");
            if (recoveryRequested) throw new InvalidOperationException("Saving is disabled after recovery confirmation.");
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
