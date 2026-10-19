using Grid.Core;
using Grid.Simulation;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Grid.Persistence.Tests;

public sealed class HistoryTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "GridHistory-" + Guid.NewGuid().ToString("N"));
    private string PathName => Path.Combine(directory, "world.db");
    public HistoryTests() => Directory.CreateDirectory(directory);
    public void Dispose() { foreach (var file in Directory.GetFiles(directory)) File.Delete(file); Directory.Delete(directory); }
    private void Sql(string sql)
    {
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = PathName, Pooling = false }.ToString());
        c.Open(); using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery();
    }
    private static WorldInfo Info(GridSimulationEngine engine) => new(engine.GetSnapshot().WorldId, "History", DateTimeOffset.UtcNow);

    [Fact]
    public async Task MovementResourcesAndClockSurviveSavesReopenAndRecoveryWithoutRoutineTicks()
    {
        var world = GridWorldFactory.CreateGenesis(); var engine = new GridSimulationEngine(world); var info = Info(engine);
        Guid programId;
        using (var store = new WorldStore(PathName))
        {
            store.Create(info, engine.GetSnapshot());
            var session = new WorldPersistenceSession(store, info, engine);
            var p = world.Programs.Values.First(); programId = p.Id;
            world.MoveProgram(p.Id, world.Districts.Keys.First(x => x != p.CurrentDistrictId));
            engine.SetDistrictResource(world.Districts.Keys.First(), "Power", 12);
            engine.SetDistrictResource(world.Districts.Keys.First(), "Power", 12);
            engine.Step(); engine.Start(); engine.Pause();
            await session.SaveAsync(); await session.SaveAsync();
            Assert.Equal(4, store.QueryHistory().Count);
            Assert.Empty(engine.CaptureHistory().Events);
            Assert.Single(store.QueryHistory(new(EntityId: p.Id)));
            Assert.Single(store.QueryHistory(new(Kind: "resource")));
            Assert.Empty(store.QueryHistory(new(Kind: "Tick")));
            Assert.Empty(store.QueryHistory(new(SinceUtc: DateTimeOffset.UtcNow.AddDays(1))));
            Assert.Equal(2, store.QueryHistory(new(Limit: 2, Offset: 1)).Count);
            Assert.Contains("fromDistrictId", store.QueryHistory(new(Kind: "Transfer")).Single().ContextJson);
        }
        using var reopened = new WorldStore(PathName);
        Assert.Single(reopened.QueryHistory(new(EntityId: programId)));
        File.WriteAllText(PathName, "broken"); reopened.Recover();
        Assert.Equal(4, reopened.QueryHistory().Count);
    }

    [Fact]
    public async Task EventInsertFailureRollsBackStateAndKeepsPendingEventsForRetry()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); var info = Info(engine);
        using var store = new WorldStore(PathName); store.Create(info, engine.GetSnapshot());
        var session = new WorldPersistenceSession(store, info, engine);
        engine.Step(); engine.Start(); engine.Pause();
        Sql("CREATE TRIGGER fail_history BEFORE INSERT ON WorldEvents BEGIN SELECT RAISE(ABORT, 'injected'); END;");
        await Assert.ThrowsAsync<SqliteException>(() => session.SaveAsync());
        Assert.Equal(0, store.Load().State.TickCount); Assert.Empty(store.QueryHistory());
        Assert.Equal(2, engine.CaptureHistory().Events.Count);
        Sql("DROP TRIGGER fail_history"); await session.SaveAsync();
        Assert.Equal(1, store.Load().State.TickCount); Assert.Equal(2, store.QueryHistory().Count);
    }

    [Fact]
    public async Task BackupFailureRetryDoesNotDuplicateCommittedEvents()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); var info = Info(engine);
        using var store = new WorldStore(PathName); store.Create(info, engine.GetSnapshot());
        var session = new WorldPersistenceSession(store, info, engine); engine.Start(); engine.Pause();
        using (var locked = new FileStream(store.BackupPath, FileMode.Open, FileAccess.Read, FileShare.None))
            await Assert.ThrowsAsync<IOException>(() => session.SaveAsync());
        Assert.Equal(2, engine.CaptureHistory().Events.Count);
        await session.SaveAsync(); Assert.Equal(2, store.QueryHistory().Count);
        Assert.Empty(engine.CaptureHistory().Events);
    }

    [Fact]
    public void CaptureAcknowledgementDoesNotDiscardNewerEvents()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        engine.Start(); var batch = engine.CaptureHistory(); engine.Pause(); engine.AcknowledgeHistory(batch);
        Assert.Single(engine.CaptureHistory().Events);
        Assert.Contains("paused", engine.CaptureHistory().Events.Single().Detail);
    }

    [Fact]
    public void PendingHistoryIsIndependentOfLiveBufferAndReportsOverflow()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), recentEventCapacity: 1);
        for (var i = 0; i < 5001; i++) { engine.Start(); engine.Pause(); }
        var batch = engine.CaptureHistory();
        Assert.Equal(10001, batch.Events.Count);
        var gap = Assert.Single(batch.Events, e => e.Kind == "HistoryGap");
        Assert.Equal("Warning", gap.Severity); Assert.Contains("\"dropped\":2", gap.ContextJson);
        engine.AcknowledgeHistory(batch); Assert.Empty(engine.CaptureHistory().Events);
    }

    [Fact]
    public void V1UpgradePreservesExistingWorldAndAddsEmptyHistory()
    {
        using (var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = PathName, Pooling = false }.ToString()))
        {
            c.Open(); GridSchema.Migrate(c, [GridSchema.Migrations[0]]);
            using var command = c.CreateCommand(); command.CommandText = "INSERT INTO Worlds VALUES ('11111111-1111-1111-1111-111111111111','Legacy','2026-10-18T00:00:00.0000000+00:00',NULL,NULL)"; command.ExecuteNonQuery();
        }
        using var upgraded = new SqliteGridDatabase(PathName).OpenExisting();
        using var query = upgraded.CreateCommand(); query.CommandText = "SELECT Name FROM Worlds";
        Assert.Equal("Legacy", query.ExecuteScalar()); query.CommandText = "SELECT COUNT(*) FROM WorldEvents";
        Assert.Equal(0L, query.ExecuteScalar()); query.CommandText = "PRAGMA user_version"; Assert.Equal((long)GridSchema.CurrentVersion, query.ExecuteScalar());
    }

    [Fact]
    public void HistoricalEntityDoesNotRequireAnActiveResident()
    {
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()); var info = Info(engine);
        using var store = new WorldStore(PathName); store.Create(info, engine.GetSnapshot());
        var entity = Guid.NewGuid();
        var item = new GridHistoryEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, 0, TimeSpan.Zero, "ApplicationExit", "Information", entity, "Former app", "Exited", "{}");
        store.Save(info, engine.GetSnapshot(), events: [item]);
        Assert.Single(store.QueryHistory(new(EntityId: entity)));
        Assert.DoesNotContain(store.Load().State.Programs, p => p.Id == entity);
    }
}
