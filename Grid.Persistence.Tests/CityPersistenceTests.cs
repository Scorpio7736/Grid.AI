using Grid.Core;
using Grid.Core.Generation;
using Grid.Simulation;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Grid.Persistence.Tests;

public sealed class CityPersistenceTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "GridCity-" + Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(directory, "world.db");
    public CityPersistenceTests() => Directory.CreateDirectory(directory);
    public void Dispose() { foreach (var file in Directory.GetFiles(directory)) File.Delete(file); Directory.Delete(directory); }
    private static HardwareProfile Profile() => new(DateTimeOffset.Parse("2026-10-19T00:00:00Z"),
        Enum.GetValues<HardwareKind>().Select(k => new HardwareCategory(k, DiscoveryStatus.Available, "Fixture", "",
            [new HardwareComponent(k.ToString(), 16L * 1073741824, 8)])).ToArray());

    [Fact]
    public async Task CreateSaveReopenAndRecoveryPreserveActualLayoutWithoutRediscovery()
    {
        var discovery = new FakeDiscovery(); SavedWorld saved;
        using (var store = new WorldStore(DatabasePath))
        {
            var startup = await WorldStartup.LoadOrCreateAsync(store, discovery); Assert.True(startup.Created);
            var engine = GridSimulationEngine.Restore(startup.Saved.State); engine.Step();
            engine.SetDistrictResource(engine.GetSnapshot().Districts[0].Id, "Data", 7.25f);
            await new WorldPersistenceSession(store, startup.Saved.Info, engine).SaveAsync();
            saved = store.Load();
        }
        Assert.Equal(1, discovery.Calls);
        using var reopened = new WorldStore(DatabasePath);
        var changedHardware = new FakeDiscovery { FailIfCalled = true };
        var loaded = await WorldStartup.LoadOrCreateAsync(reopened, changedHardware);
        Assert.False(loaded.Created); Assert.Equal(0, changedHardware.Calls);
        Assert.Equal(saved.Info, loaded.Saved.Info); Assert.Equal(saved.State.City!.Json, loaded.Saved.State.City!.Json);
        Assert.Equal(1, loaded.Saved.State.TickCount); Assert.Contains(loaded.Saved.State.Districts, d => d.Resources.GetValueOrDefault("Data") == 7.25f);
        File.WriteAllText(DatabasePath, "damaged city"); reopened.Recover();
        var restored = await WorldStartup.LoadOrCreateAsync(reopened, changedHardware);
        Assert.Equal(saved.State.City.Json, restored.Saved.State.City!.Json); Assert.Equal(0, changedHardware.Calls);
    }

    [Fact]
    public async Task LegacyWorldIsNeverRegeneratedOrAssignedNewHardwareProvenance()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        using var store = new WorldStore(DatabasePath);
        store.Create(new(engine.GetSnapshot().WorldId, "Legacy", DateTimeOffset.UtcNow, 7736, "genesis-v1"), engine.GetSnapshot());
        var discovery = new FakeDiscovery { FailIfCalled = true };
        var result = await WorldStartup.LoadOrCreateAsync(store, discovery);
        Assert.False(result.Created); Assert.Null(result.Saved.State.City); Assert.Equal(20, result.Saved.State.Programs.Count); Assert.Equal(0, discovery.Calls);
    }

    [Theory]
    [InlineData("corrupt")]
    [InlineData("backup")]
    [InlineData("sidecar")]
    [InlineData("recovering")]
    public async Task ExistingArtifactsBlockDiscoveryAndRegeneration(string artifact)
    {
        var path = artifact switch { "backup" => DatabasePath + ".backup", "sidecar" => DatabasePath + "-journal", "recovering" => DatabasePath + ".recovering", _ => DatabasePath };
        File.WriteAllText(path, "preserve");
        using var store = new WorldStore(DatabasePath); var discovery = new FakeDiscovery { FailIfCalled = true };
        await Assert.ThrowsAnyAsync<Exception>(() => WorldStartup.LoadOrCreateAsync(store, discovery));
        Assert.Equal(0, discovery.Calls); Assert.Equal("preserve", File.ReadAllText(path));
    }

    [Fact]
    public async Task SQLiteFullDuringCreationLeavesNoPartialWorldAndDoesNotRegenerateOnRetry()
    {
        using (var store = new WorldStore(DatabasePath, c =>
        {
            using var cmd = c.CreateCommand(); cmd.CommandText = "PRAGMA page_count";
            var pages = Convert.ToInt64(cmd.ExecuteScalar()); cmd.CommandText = $"PRAGMA max_page_count={pages}"; cmd.ExecuteScalar();
        }))
        {
            var error = await Assert.ThrowsAsync<SqliteException>(() => WorldStartup.LoadOrCreateAsync(store, new FakeDiscovery()));
            Assert.Equal(13, error.SqliteErrorCode); // SQLITE_FULL, constrained SQLite capacity rather than filling the host disk.
        }
        using var c = new SqliteGridDatabase(DatabasePath).OpenExisting(); using var cmd = c.CreateCommand();
        foreach (var table in new[] { "Worlds", "Districts", "Programs", "SimulationStates", "WorldGeneration" })
        { cmd.CommandText = $"SELECT COUNT(*) FROM {table}"; Assert.Equal(0L, cmd.ExecuteScalar()); }
        Assert.False(File.Exists(DatabasePath + ".backup"));
        using var retry = new WorldStore(DatabasePath); var discovery = new FakeDiscovery { FailIfCalled = true };
        await Assert.ThrowsAsync<InvalidDataException>(() => WorldStartup.LoadOrCreateAsync(retry, discovery));
        Assert.Equal(0, discovery.Calls);
    }

    [Fact]
    public async Task CancellationBeforeCreationLeavesNoPrimaryAndNoDiscovery()
    {
        using var store = new WorldStore(DatabasePath); var discovery = new FakeDiscovery();
        await Assert.ThrowsAsync<OperationCanceledException>(() => WorldStartup.LoadOrCreateAsync(store, discovery, new CancellationToken(true)));
        Assert.False(File.Exists(DatabasePath)); Assert.Equal(0, discovery.Calls);
    }

    [Fact]
    public async Task ConcurrentCreatorCannotAcquireWorldLeaseDuringDiscovery()
    {
        using var store = new WorldStore(DatabasePath);
        var waiting = new WaitingDiscovery(); var creating = WorldStartup.LoadOrCreateAsync(store, waiting);
        await waiting.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try { Assert.Throws<IOException>(() => new WorldStore(DatabasePath)); }
        finally { waiting.Proceed.TrySetResult(); }
        Assert.True((await creating).Created);
        Assert.NotNull(store.Load().State.City);
    }

    [Fact]
    public async Task OrdinarySaveCannotRemoveOrReplaceLayoutAndMalformedLayoutFailsLoad()
    {
        using var store = new WorldStore(DatabasePath); var startup = await WorldStartup.LoadOrCreateAsync(store, new FakeDiscovery());
        Assert.Throws<InvalidDataException>(() => store.Save(startup.Saved.Info, startup.Saved.State with { City = null }));
        using var c = new SqliteGridDatabase(DatabasePath).OpenExisting(); using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE WorldGeneration SET BlueprintJson='{}'"; cmd.ExecuteNonQuery();
        Assert.Throws<InvalidDataException>(() => store.Load());
    }

    private sealed class FakeDiscovery : IHardwareDiscovery
    {
        public int Calls { get; private set; }
        public bool FailIfCalled { get; init; }
        public Task<HardwareProfile> DiscoverAsync(CancellationToken cancellationToken)
        {
            Calls++; if (FailIfCalled) throw new InvalidOperationException("Discovery must not run on load.");
            return Task.FromResult(Profile());
        }
    }
    private sealed class WaitingDiscovery : IHardwareDiscovery
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Proceed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<HardwareProfile> DiscoverAsync(CancellationToken cancellationToken)
        { Entered.TrySetResult(); await Proceed.Task.WaitAsync(cancellationToken); return Profile(); }
    }
}
