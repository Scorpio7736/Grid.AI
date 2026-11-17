using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Persistence.Tests;

public sealed class NullificationPersistenceTests
{
    [Fact]
    public async Task DeletesOnlyOwnedFilesBlocksFurtherSaveAndAllowsFreshIdentityAfterReopen()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridNge-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "world.db"); var other = Path.Combine(folder, "other.db");
            File.WriteAllText(other, "unrelated");
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); var state = engine.GetSnapshot();
            var info = new WorldInfo(state.WorldId, "Disposable", DateTimeOffset.UtcNow);
            using (var store = new WorldStore(path))
            {
                store.Create(info, state);
                Directory.CreateDirectory(path + ".skills");
                File.WriteAllText(Path.Combine(path + ".skills", "manifest.json"), "skill sentinel");
                var session = new WorldPersistenceSession(store, info, engine);
                var plan = session.PrepareNullification();
                Assert.Throws<InvalidOperationException>(() => store.Nullify(plan with { WorldId = Guid.NewGuid() }));
                await session.NullifyAsync(plan);
                Assert.All(plan.Paths, p => { Assert.False(File.Exists(p)); Assert.False(Directory.Exists(p)); });
                Assert.False(File.Exists(path + ".nullifying"));
                await Assert.ThrowsAsync<InvalidOperationException>(() => session.SaveAsync());
                Assert.Throws<IOException>(() => store.Recover());
            }
            Assert.Equal("unrelated", File.ReadAllText(other));
            using var fresh = new WorldStore(path); fresh.EnsureCanCreate();
            var replacement = new GridSimulationEngine(GridWorldFactory.CreateGenesis()).GetSnapshot();
            fresh.Create(new(replacement.WorldId, "New", DateTimeOffset.UtcNow), replacement);
            Assert.NotEqual(info.Id, fresh.Load().Info.Id);
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void LockedPrimaryLeavesMarkerThatBlocksStartupAndRecovery()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridNgeLocked-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "world.db");
            var state = new GridSimulationEngine(GridWorldFactory.CreateGenesis()).GetSnapshot();
            using (var store = new WorldStore(path))
            {
                store.Create(new(state.WorldId, "Disposable", DateTimeOffset.UtcNow), state);
                var plan = store.PrepareNullification(state.WorldId);
                // Reads work for identity validation, deletion is denied on Windows.
                using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (OperatingSystem.IsWindows())
                {
                    Assert.Throws<IOException>(() => store.Nullify(plan));
                    Assert.True(File.Exists(path + ".nullifying"));
                }
            }
            if (OperatingSystem.IsWindows())
            {
                using var reopened = new WorldStore(path);
                Assert.Throws<IOException>(() => reopened.Load());
                Assert.Throws<IOException>(() => reopened.EnsureCanCreate());
                Assert.Throws<IOException>(() => reopened.Recover());
            }
        }
        finally { Directory.Delete(folder, true); }
    }
}
