using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Persistence.Tests;

public sealed class RetirementTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "Grid-retire-" + Guid.NewGuid());
    private string World => Path.Combine(root, "data", "world.db");
    public RetirementTests() => Directory.CreateDirectory(Path.Combine(root, "data"));
    public void Dispose() => Directory.Delete(root, true);
    private WorldStore Create()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var store = new WorldStore(World);
        var info = new WorldInfo(engine.GetSnapshot().WorldId, "Retirement fixture", DateTimeOffset.UtcNow);
        store.Create(info, engine.GetSnapshot()); store.Save(info, engine.GetSnapshot()); return store;
    }
    [Fact]
    public void RetirementPreservesFilesAndBlocksSaveRestartRecoveryAndReplay()
    {
        using var store = Create(); var saved = store.Load();
        var plan = store.PrepareRetirement(saved.Info.Id, Path.Combine(root, "quarantine"));
        var original = File.ReadAllBytes(World);
        Assert.Throws<InvalidOperationException>(() => store.Retire(plan with { Id = Guid.NewGuid() }));
        var receipt = store.Retire(plan);
        Assert.True(File.Exists(receipt)); Assert.False(File.Exists(World));
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(plan.Destination, "world.db")));
        Assert.Throws<IOException>(() => store.Load()); Assert.Throws<IOException>(() => store.EnsureCanCreate());
        Assert.Throws<IOException>(() => store.Recover()); Assert.Throws<IOException>(() => store.Save(saved.Info, saved.State));
        Assert.Throws<IOException>(() => store.Retire(plan));
    }
    [Fact]
    public void ChangedWorldOrDestinationRefusesWithoutMovingAnything()
    {
        using var store = Create(); var saved = store.Load();
        var plan = store.PrepareRetirement(saved.Info.Id, Path.Combine(root, "quarantine"));
        Directory.CreateDirectory(plan.Destination);
        Assert.Throws<IOException>(() => store.Retire(plan)); Assert.True(File.Exists(World));
        Assert.False(File.Exists(World + ".retired"));
    }
    [Fact]
    public void InterruptionAfterPrimaryMoveLeavesExactReceiptAndStartupBlocked()
    {
        using var store = Create();
        var plan = store.PrepareRetirement(store.Load().Info.Id, Path.Combine(root, "quarantine"));
        // A non-GRID process holding a backup prevents its move after the primary has been quarantined.
        using (var locked = new FileStream(store.BackupPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.Throws<IOException>(() => store.Retire(plan));
        Assert.True(File.Exists(World + ".retired"));
        Assert.True(File.Exists(Path.Combine(plan.Destination, "retirement.json")));
        Assert.True(File.Exists(Path.Combine(plan.Destination, "world.db")));
        Assert.True(File.Exists(store.BackupPath)); Assert.Throws<IOException>(() => store.EnsureCanCreate());
    }
    [Fact]
    public void SidecarAppearingAfterReviewIsPreservedAndRefused()
    {
        using var store = Create();
        var plan = store.PrepareRetirement(store.Load().Info.Id, Path.Combine(root, "quarantine"));
        File.WriteAllText(World + "-wal", "canary");
        Assert.Throws<IOException>(() => store.Retire(plan));
        Assert.Equal("canary", File.ReadAllText(World + "-wal"));
        Assert.True(File.Exists(World)); Assert.False(Directory.Exists(plan.Destination));
    }
    [Fact]
    public void RestartAndOfflineRescueCannotBypassRetirementMarker()
    {
        using (var store = Create()) store.Retire(store.PrepareRetirement(store.Load().Info.Id, Path.Combine(root, "quarantine")));
        using var reopened = new WorldStore(World);
        Assert.Throws<IOException>(() => reopened.EnsureCanCreate());
        Assert.Throws<IOException>(() => reopened.ReadOfflineRescueState());
        Assert.Throws<IOException>(() => reopened.Recover());
    }
    [Fact]
    public void ActiveWriterAndUnsafeQuarantineAreRefused()
    {
        using var store = Create(); var id = store.Load().Info.Id;
        Assert.Throws<IOException>(() => new WorldStore(World));
        Assert.Throws<IOException>(() => store.PrepareRetirement(id, Path.GetPathRoot(root)!));
        Assert.Throws<IOException>(() => store.PrepareRetirement(id, Path.Combine(root, "data", "inside")));
        Assert.Throws<InvalidDataException>(() => store.PrepareRetirement(Guid.NewGuid(), Path.Combine(root, "other")));
        File.WriteAllText(World + "-wal", "canary");
        Assert.Throws<IOException>(() => store.PrepareRetirement(id, Path.Combine(root, "quarantine")));
    }
}
