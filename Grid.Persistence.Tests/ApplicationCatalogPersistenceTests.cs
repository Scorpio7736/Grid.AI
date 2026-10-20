using Grid.Core;
using Grid.Simulation;
using Microsoft.Data.Sqlite;
using Xunit;
namespace Grid.Persistence.Tests;

public sealed class ApplicationCatalogPersistenceTests
{
    [Fact]
    public void CatalogSurvivesReopenAndRecoveryWithoutRestoringLiveResidents()
    {
        var directory = Path.Combine(Path.GetTempPath(), "GridCatalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "world.db");
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
            var now = DateTimeOffset.UtcNow;
            engine.ReconcileApplications(new(now, true, [new("app-key", "App", [new(99, now.AddSeconds(-1))], 100)], "Test"));
            var info = new WorldInfo(engine.GetSnapshot().WorldId, "Catalog", now);
            using (var store = new WorldStore(path)) store.Create(info, engine.GetSnapshot());
            using var reopened = new WorldStore(path);
            var snapshot = reopened.Load().State;
            Assert.Equal("app-key", Assert.Single(snapshot.ApplicationCatalog).Key);
            Assert.DoesNotContain(snapshot.Programs, p => p.Kind == ResidentKind.Application);
            var restored = GridSimulationEngine.Restore(snapshot);
            Assert.Equal("App", Assert.Single(restored.GetSnapshot().ApplicationCatalog).Name);
            reopened.Save(info, restored.GetSnapshot());
            File.WriteAllText(path, "corrupt"); reopened.Recover();
            Assert.Equal(now, Assert.Single(reopened.Load().State.ApplicationCatalog).LastSeenUtc);
            Assert.Throws<InvalidDataException>(() => reopened.Save(info, snapshot with {
                ApplicationCatalog = [snapshot.ApplicationCatalog[0] with { Source = "" }] }));
            Assert.Single(reopened.Load().State.ApplicationCatalog);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void VersionFiveMigratesToCatalogSchemaWithoutInventingEntries()
    {
        var path = Path.Combine(Path.GetTempPath(), "GridCatalogMigration-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            { connection.Open(); GridSchema.Migrate(connection, GridSchema.Migrations.Take(5).ToArray()); }
            using var upgraded = new SqliteGridDatabase(path).OpenExisting();
            using var command = upgraded.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM ApplicationCatalog";
            Assert.Equal(0L, command.ExecuteScalar());
            command.CommandText = "PRAGMA user_version"; Assert.Equal(6L, command.ExecuteScalar());
        }
        finally { File.Delete(path); }
    }
}
