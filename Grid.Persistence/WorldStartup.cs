using System.Security.Cryptography;
using Grid.Core.Generation;
using Grid.Simulation;

namespace Grid.Persistence;

public sealed record WorldStartupResult(SavedWorld Saved, bool Created);
public static class WorldStartup
{
    public static async Task<WorldStartupResult> LoadOrCreateAsync(WorldStore store, IHardwareDiscovery discovery,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(store.DatabasePath)) return new(store.Load(), false);
        store.EnsureCanCreate();
        var profile = await discovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var seed = RandomNumberGenerator.GetInt32(int.MaxValue);
        var world = HardwareCityGenerator.Create(profile, seed);
        var engine = new GridSimulationEngine(world);
        var info = new WorldInfo(world.Id, "GRID City", DateTimeOffset.UtcNow, seed, HardwareCityGenerator.Version);
        store.Create(info, engine.GetSnapshot(), cancellationToken);
        return new(store.Load(), true);
    }
}
