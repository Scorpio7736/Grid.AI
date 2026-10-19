using Grid.Core;
using Grid.Simulation;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Grid.Persistence.Tests;

public sealed class WorldStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "GridSaveTests-" + Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(directory, "world;test.db");
    public WorldStoreTests() => Directory.CreateDirectory(directory);
    public void Dispose() { foreach (var file in Directory.GetFiles(directory)) File.Delete(file); Directory.Delete(directory); }
    private static WorldInfo Info(GridSimulationEngine engine) => new(engine.GetSnapshot().WorldId, "Test", DateTimeOffset.UtcNow, 7736, "test-v1");
    private static GridSimulationEngine Engine() => new(GridWorldFactory.CreateGenesis(), TimeSpan.FromMilliseconds(250));
    private void Sql(string sql)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = DatabasePath, Pooling = false }.ToString());
        connection.Open(); using var command = connection.CreateCommand(); command.CommandText = sql; command.ExecuteNonQuery();
    }

    [Fact]
    public void RoundTripRetainsIdentityMembershipValuesRevisionsResourcesAndClock()
    {
        var world = GridWorldFactory.CreateGenesis();
        var first = world.Programs.Values.First();
        world.MoveProgram(first.Id, world.Districts.Keys.First(id => id != first.CurrentDistrictId));
        world.Districts.Values.First().Resources["Test energy"] = 12.5f;
        var engine = new GridSimulationEngine(world, TimeSpan.FromMilliseconds(250));
        engine.Step(); engine.Step(); engine.Start();
        var original = engine.GetSnapshot();
        using var store = new WorldStore(DatabasePath);
        var info = Info(engine);
        store.Create(info, original);
        var loaded = store.Load();
        var restored = GridSimulationEngine.Restore(loaded.State);
        Assert.Equal(info, loaded.Info);
        Assert.True(loaded.State.IsRunning);
        Assert.False(restored.IsRunning);
        Assert.Null(restored.LastTick);
        var actual = restored.GetSnapshot();
        Assert.Equal(original.WorldId, actual.WorldId);
        Assert.Equal(original.Programs, actual.Programs);
        Assert.Equal(original.TickCount, actual.TickCount);
        Assert.Equal(original.ElapsedSimulationTime, actual.ElapsedSimulationTime);
        Assert.Equal(original.TickDuration, actual.TickDuration);
        foreach (var d in original.Districts)
        {
            var copy = actual.Districts.Single(x => x.Id == d.Id);
            Assert.Equal(d.Name, copy.Name); Assert.Equal(d.Description, copy.Description);
            Assert.Equal(d.ProgramIds.Order(), copy.ProgramIds.Order());
            Assert.Equal(d.Resources.OrderBy(x => x.Key), copy.Resources.OrderBy(x => x.Key));
        }
        Assert.Equal(actual.TickCount, GridSimulationEngine.Restore(store.Load().State).TickCount);
    }

    [Fact]
    public void MidWriteSqlFailureRollsBackAllRowsAndRetainsBackup()
    {
        using var store = new WorldStore(DatabasePath);
        var engine = Engine(); var info = Info(engine); store.Create(info, engine.GetSnapshot());
        var backup = File.ReadAllBytes(store.BackupPath);
        Sql("CREATE TRIGGER fail_save BEFORE INSERT ON SimulationStates BEGIN SELECT RAISE(ABORT, 'Injected failure'); END;");
        engine.Step();
        Assert.Throws<SqliteException>(() => store.Save(info, engine.GetSnapshot()));
        Assert.Equal(0, store.Load().State.TickCount);
        Assert.Equal(20, store.Load().State.Programs.Count);
        Assert.Equal(backup, File.ReadAllBytes(store.BackupPath));
    }

    [Fact]
    public void CancelledSaveLeavesLastCommitAndBackup()
    {
        using var store = new WorldStore(DatabasePath);
        var engine = Engine(); var info = Info(engine); store.Create(info, engine.GetSnapshot());
        engine.Step();
        Assert.Throws<OperationCanceledException>(() => store.Save(info, engine.GetSnapshot(), new CancellationToken(true)));
        Assert.Equal(0, store.Load().State.TickCount);
        store.Recover(); Assert.Equal(0, store.Load().State.TickCount);
    }

    [Fact]
    public void CorruptPrimaryCannotBeLoadedOrOverwrittenAndRecoveryArchivesIt()
    {
        using var store = new WorldStore(DatabasePath);
        var engine = Engine(); var info = Info(engine); engine.Step(); store.Create(info, engine.GetSnapshot());
        File.WriteAllText(DatabasePath, "damaged primary");
        Assert.ThrowsAny<Exception>(() => store.Load());
        Assert.ThrowsAny<Exception>(() => store.Save(info, engine.GetSnapshot()));
        store.Recover();
        Assert.Equal(1, store.Load().State.TickCount);
        Assert.Equal("damaged primary", File.ReadAllText(Directory.GetFiles(directory, "*.damaged-*").Single()));
    }

    [Fact]
    public void InvalidBackupDoesNotReplacePrimary()
    {
        using var store = new WorldStore(DatabasePath);
        var engine = Engine(); store.Create(Info(engine), engine.GetSnapshot());
        File.WriteAllText(store.BackupPath, "invalid backup");
        Assert.ThrowsAny<Exception>(() => store.Recover());
        Assert.Equal(0, store.Load().State.TickCount);
    }

    [Fact]
    public void BackupFailureReportsCommittedPrimaryAndRetainsPreviousBackup()
    {
        using var store = new WorldStore(DatabasePath);
        var engine = Engine(); var info = Info(engine); store.Create(info, engine.GetSnapshot());
        var previous = File.ReadAllBytes(store.BackupPath);
        engine.Step();
        using (var locked = new FileStream(store.BackupPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var error = Assert.Throws<IOException>(() => store.Save(info, engine.GetSnapshot()));
            Assert.Contains("save committed", error.Message);
        }
        Assert.Equal(1, store.Load().State.TickCount);
        Assert.Equal(previous, File.ReadAllBytes(store.BackupPath));
        store.Recover(); Assert.Equal(0, store.Load().State.TickCount);
    }

    [Fact]
    public void MissingPrimaryWithBackupRequiresExplicitRecovery()
    {
        using var store = new WorldStore(DatabasePath);
        var engine = Engine(); var info = Info(engine); store.Create(info, engine.GetSnapshot());
        File.Delete(DatabasePath);
        Assert.Throws<IOException>(() => store.Create(info, engine.GetSnapshot()));
        store.Recover(); Assert.Equal(info, store.Load().Info);
    }

    [Fact]
    public void InterruptedRecoveryBlocksNormalUseAndCanBeRetried()
    {
        using var store = new WorldStore(DatabasePath);
        var engine = Engine(); var info = Info(engine); store.Create(info, engine.GetSnapshot());
        File.WriteAllText(DatabasePath + ".recovering", "interrupted");
        Assert.Throws<IOException>(() => store.Load());
        Assert.Throws<IOException>(() => store.Save(info, engine.GetSnapshot()));
        store.Recover(); Assert.Equal(info, store.Load().Info);
        Assert.False(File.Exists(DatabasePath + ".recovering"));
    }

    [Theory]
    [InlineData("DELETE FROM SimulationStates")]
    [InlineData("UPDATE Worlds SET CreatedAtUtc='yesterday'")]
    [InlineData("PRAGMA ignore_check_constraints=ON; UPDATE Programs SET ProgramId='not-a-guid' WHERE rowid=1")]
    [InlineData("PRAGMA ignore_check_constraints=ON; UPDATE SimulationStates SET ElapsedTicks=123")]
    [InlineData("PRAGMA ignore_check_constraints=ON; UPDATE Programs SET Status='42'")]
    [InlineData("PRAGMA foreign_keys=OFF; DELETE FROM Districts")]
    [InlineData("UPDATE Programs SET Name=' untrimmed '")]
    public void InvalidStoredStateIsRejected(string sql)
    {
        using var store = new WorldStore(DatabasePath);
        var engine = Engine(); store.Create(Info(engine), engine.GetSnapshot());
        Sql(sql);
        Assert.ThrowsAny<Exception>(() => store.Load());
    }

    [Fact]
    public void LeasePreventsTwoSessionsAndReleasesOnDispose()
    {
        using (var store = new WorldStore(DatabasePath)) Assert.Throws<IOException>(() => new WorldStore(DatabasePath));
        using var reopened = new WorldStore(DatabasePath);
    }

    [Fact]
    public void AnotherWorldCannotReplaceExistingWorld()
    {
        using var store = new WorldStore(DatabasePath);
        var engine = Engine(); store.Create(Info(engine), engine.GetSnapshot());
        var other = Engine();
        Assert.Throws<InvalidDataException>(() => store.Save(Info(other), other.GetSnapshot()));
        Assert.Equal(engine.GetSnapshot().WorldId, store.Load().Info.Id);
    }

    [Fact]
    public async Task PeriodicSavePersistsAConsistentTickAndStopsOnCancellation()
    {
        using var store = new WorldStore(DatabasePath);
        var engine = Engine(); var info = Info(engine); store.Create(info, engine.GetSnapshot()); engine.Step();
        var session = new WorldPersistenceSession(store, info, engine, TimeSpan.FromMilliseconds(10));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var periodic = session.RunPeriodicAsync(cancellation.Token);
        while (store.Load().State.TickCount != 1) await Task.Delay(10, cancellation.Token);
        cancellation.Cancel(); await periodic;
        Assert.Equal(engine.ElapsedSimulationTime, store.Load().State.ElapsedSimulationTime);
    }
}
