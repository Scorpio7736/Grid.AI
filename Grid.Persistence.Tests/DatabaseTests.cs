using Microsoft.Data.Sqlite;
using Xunit;

namespace Grid.Persistence.Tests;

public sealed class DatabaseTests
{
    private const string World = "11111111-1111-1111-1111-111111111111";
    private const string OtherWorld = "22222222-2222-2222-2222-222222222222";
    private const string District = "33333333-3333-3333-3333-333333333333";
    private const string Program = "44444444-4444-4444-4444-444444444444";

    private static object? Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand(); command.CommandText = sql; return command.ExecuteScalar();
    }
    private static void Execute(SqliteConnection connection, string sql, SqliteTransaction? transaction = null)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = sql; command.ExecuteNonQuery();
    }
    private static void Seed(SqliteConnection connection)
    {
        Execute(connection, $"""
            INSERT INTO Worlds VALUES ('{World}', 'Test world', '2026-10-18T00:00:00Z', 7736, 'genesis');
            INSERT INTO Districts VALUES ('{World}', '{District}', 'The Core', 'Description');
            INSERT INTO Programs VALUES ('{World}', '{Program}', 'Citizen', 'Administrator', 42.5, 'Idle', '{District}', 7);
            INSERT INTO DistrictResources VALUES ('{World}', '{District}', 'Energy', 10.5);
            INSERT INTO SimulationStates VALUES ('{World}', 2, 20000000, 10000000, 0, '2026-10-18T00:00:00Z');
            """);
    }

    [Fact]
    public void ExplicitInitializationCreatesEmptyVersionedSchemaAndReopenPreservesRows()
    {
        using var files = new DatabaseFiles();
        Assert.False(File.Exists(files.Path));
        files.Database.Initialize();
        using (var connection = files.Database.OpenExisting())
        {
            Assert.Equal((long)GridSchema.CurrentVersion, Scalar(connection, "PRAGMA user_version;"));
            Assert.Equal((long)GridSchema.ApplicationId, Scalar(connection, "PRAGMA application_id;"));
            Assert.Equal(0L, Scalar(connection, "SELECT COUNT(*) FROM Worlds;"));
            Seed(connection);
        }
        files.Database.Initialize();
        using var reopened = files.Database.OpenExisting();
        Assert.Equal((long)GridSchema.CurrentVersion, Scalar(reopened, "SELECT COUNT(*) FROM SchemaMigrations;"));
        Assert.Equal(7L, Scalar(reopened, "SELECT Revision FROM Programs;"));
        Assert.Equal(42.5, Scalar(reopened, "SELECT Energy FROM Programs;"));
        Assert.Equal(20000000L, Scalar(reopened, "SELECT ElapsedTicks FROM SimulationStates;"));
    }

    [Fact]
    public void EveryConnectionEnforcesForeignKeysAndFullSynchronization()
    {
        using var files = new DatabaseFiles(); files.Database.Initialize();
        for (var i = 0; i < 2; i++)
        {
            using var connection = files.Database.OpenExisting();
            Assert.Equal(1L, Scalar(connection, "PRAGMA foreign_keys;"));
            Assert.Equal(2L, Scalar(connection, "PRAGMA synchronous;"));
            Assert.Throws<SqliteException>(() => Execute(connection,
                $"INSERT INTO Districts VALUES ('{World}', '{District}', 'Orphan', '');"));
        }
    }

    [Theory]
    [InlineData("UPDATE Programs SET Energy = -1;")]
    [InlineData("UPDATE Programs SET Energy = 101;")]
    [InlineData("UPDATE Programs SET Energy = 1e999;")]
    [InlineData("UPDATE Programs SET Energy = NULL;")]
    [InlineData("UPDATE Programs SET Status = 'Unknown';")]
    [InlineData("UPDATE Programs SET Revision = -1;")]
    [InlineData("UPDATE Programs SET Revision = 0.5;")]
    [InlineData("UPDATE SimulationStates SET WasRunning = 2;")]
    [InlineData("UPDATE SimulationStates SET TickDurationTicks = 0;")]
    [InlineData("UPDATE SimulationStates SET ElapsedTicks = 1;")]
    [InlineData("UPDATE SimulationStates SET TickCount = 9223372036854775807;")]
    [InlineData("UPDATE DistrictResources SET Amount = 1e999;")]
    [InlineData("UPDATE DistrictResources SET ResourceKey = '';")]
    public void InvalidStoredValuesAreRejected(string sql)
    {
        using var files = new DatabaseFiles(); files.Database.Initialize();
        using var connection = files.Database.OpenExisting(); Seed(connection);
        var error = Assert.Throws<SqliteException>(() => Execute(connection, sql));
        Assert.Equal(19, error.SqliteErrorCode);
        Assert.Equal(7L, Scalar(connection, "SELECT Revision FROM Programs;"));
    }

    [Fact]
    public void MembershipCannotCrossWorldsAndDistrictDeletionCannotOrphanPrograms()
    {
        using var files = new DatabaseFiles(); files.Database.Initialize();
        using var connection = files.Database.OpenExisting(); Seed(connection);
        Execute(connection, $"INSERT INTO Worlds VALUES ('{OtherWorld}', 'Other', '2026-10-18', NULL, NULL);");
        Assert.Throws<SqliteException>(() => Execute(connection,
            $"UPDATE Programs SET WorldId = '{OtherWorld}';"));
        Assert.Throws<SqliteException>(() => Execute(connection, "DELETE FROM Districts;"));
        // Deterministic Genesis IDs may repeat across separate worlds, but never within one.
        Execute(connection, $"""
            INSERT INTO Districts VALUES ('{OtherWorld}', '{District}', 'Other Core', '');
            INSERT INTO Programs VALUES ('{OtherWorld}', '{Program}', 'Other Citizen', 'Researcher', 100, 'Idle', '{District}', 0);
            """);
        Assert.Equal(2L, Scalar(connection, "SELECT COUNT(*) FROM Programs;"));
        Assert.Throws<SqliteException>(() => Execute(connection, "INSERT INTO Programs SELECT * FROM Programs;"));
        Execute(connection, $"DELETE FROM Worlds WHERE WorldId = '{World}';");
        Assert.Equal(1L, Scalar(connection, "SELECT COUNT(*) FROM Programs;"));
        Assert.Equal(0L, Scalar(connection, "SELECT COUNT(*) FROM DistrictResources;"));
        Assert.Equal(0L, Scalar(connection, "SELECT COUNT(*) FROM SimulationStates;"));
    }

    [Fact]
    public void CallerTransactionRollsBackWholeUnitOnFailure()
    {
        using var files = new DatabaseFiles(); files.Database.Initialize();
        using var connection = files.Database.OpenExisting(); Seed(connection);
        using (var transaction = connection.BeginTransaction())
        {
            Execute(connection, "UPDATE Programs SET Energy = 12;", transaction);
            Assert.Throws<SqliteException>(() => Execute(connection, "UPDATE Programs SET Status = 'INVALID';", transaction));
            // Disposal without Commit rolls back the earlier successful write as well.
        }
        Assert.Equal(42.5, Scalar(connection, "SELECT Energy FROM Programs;"));
    }

    [Fact]
    public void LaterMigrationIsAppliedOnceAndFailureRollsBackDdlAndVersion()
    {
        using var files = new DatabaseFiles(); files.Database.Initialize();
        using var connection = files.Database.OpenExisting(); Seed(connection);
        var failed = new SchemaMigration(GridSchema.CurrentVersion + 1, "Broken upgrade", "CREATE TABLE Partial(Value TEXT); INSERT INTO Missing VALUES (1);");
        Assert.Throws<SqliteException>(() => GridSchema.Migrate(connection, [.. GridSchema.Migrations, failed]));
        Assert.Equal((long)GridSchema.CurrentVersion, Scalar(connection, "PRAGMA user_version;"));
        Assert.Equal(0L, Scalar(connection, "SELECT COUNT(*) FROM sqlite_schema WHERE name = 'Partial';"));
        Assert.Equal((long)GridSchema.CurrentVersion, Scalar(connection, "SELECT COUNT(*) FROM SchemaMigrations;"));
        var second = new SchemaMigration(GridSchema.CurrentVersion + 1, "Test extension", "CREATE TABLE Extension(Value TEXT) STRICT;");
        GridSchema.Migrate(connection, [.. GridSchema.Migrations, second]);
        GridSchema.Migrate(connection, [.. GridSchema.Migrations, second]);
        Assert.Equal((long)GridSchema.CurrentVersion + 1, Scalar(connection, "PRAGMA user_version;"));
        Assert.Equal((long)GridSchema.CurrentVersion + 1, Scalar(connection, "SELECT COUNT(*) FROM SchemaMigrations;"));
        Assert.Equal(42.5, Scalar(connection, "SELECT Energy FROM Programs;"));
    }

    [Fact]
    public void FailedFirstMigrationLeavesNoVersionOrPartialSchema()
    {
        using var files = new DatabaseFiles(); using var connection = files.Raw();
        Assert.Throws<SqliteException>(() => GridSchema.Migrate(connection,
            [new SchemaMigration(1, "Failure", "CREATE TABLE Partial(Value TEXT); INVALID SQL;")]));
        Assert.Equal(0L, Scalar(connection, "PRAGMA user_version;"));
        Assert.Equal(0L, Scalar(connection, "PRAGMA application_id;"));
        Assert.Equal(0L, Scalar(connection, "SELECT COUNT(*) FROM sqlite_schema;"));
        files.Database.Initialize(); // A deliberate retry can succeed.
    }

    [Fact]
    public void ExistingOpenNeverCreatesOrInitializesMissingOrEmptyFiles()
    {
        using var files = new DatabaseFiles();
        Assert.Throws<SqliteException>(() => files.Database.OpenExisting());
        Assert.False(File.Exists(files.Path));
        using (files.Raw()) { }
        Assert.Throws<InvalidDataException>(() => files.Database.OpenExisting());
        using var connection = files.Raw();
        Assert.Equal(0L, Scalar(connection, "PRAGMA user_version;"));
    }

    [Fact]
    public void UnknownDatabaseIsNotOverwritten()
    {
        using var files = new DatabaseFiles();
        using (var connection = files.Raw()) Execute(connection, "CREATE TABLE Other(Value TEXT); INSERT INTO Other VALUES ('keep');");
        Assert.Throws<InvalidDataException>(files.Database.Initialize);
        using var reopened = files.Raw();
        Assert.Equal("keep", Scalar(reopened, "SELECT Value FROM Other;"));
        Assert.Equal(0L, Scalar(reopened, "PRAGMA user_version;"));
    }

    [Theory]
    [InlineData("PRAGMA user_version = 99;")]
    [InlineData("PRAGMA application_id = 1234;")]
    [InlineData("UPDATE SchemaMigrations SET Checksum = 'changed';")]
    [InlineData("DELETE FROM SchemaMigrations;")]
    public void FutureForeignOrInconsistentMetadataIsRejected(string change)
    {
        using var files = new DatabaseFiles(); files.Database.Initialize();
        using (var connection = files.Raw()) { Seed(connection); Execute(connection, change); }
        Assert.Throws<InvalidDataException>(() => files.Database.OpenExisting());
        using var reopened = files.Raw();
        Assert.Equal(42.5, Scalar(reopened, "SELECT Energy FROM Programs;"));
    }

    [Fact]
    public void InvalidFileIsNotReplaced()
    {
        using var files = new DatabaseFiles(); File.WriteAllText(files.Path, "not a sqlite database");
        Assert.Throws<SqliteException>(files.Database.Initialize);
        Assert.Equal("not a sqlite database", File.ReadAllText(files.Path));
    }

    [Fact]
    public void ExistingForeignKeyViolationsAreRejected()
    {
        using var files = new DatabaseFiles(); files.Database.Initialize();
        using (var connection = files.Raw())
        {
            Seed(connection);
            Execute(connection, "PRAGMA foreign_keys = OFF; DELETE FROM Districts;");
        }
        Assert.Throws<InvalidDataException>(() => files.Database.OpenExisting());
    }

    [Fact]
    public async Task ConcurrentInitializersProduceOneMigrationHistory()
    {
        using var files = new DatabaseFiles();
        await Task.WhenAll(Task.Run(files.Database.Initialize), Task.Run(files.Database.Initialize));
        using var connection = files.Database.OpenExisting();
        Assert.Equal((long)GridSchema.CurrentVersion, Scalar(connection, "SELECT COUNT(*) FROM SchemaMigrations;"));
    }

    [Theory]
    [InlineData(":memory:")]
    [InlineData("file:world.db?mode=memory")]
    [InlineData("")]
    public void NonFileDatabaseArgumentsAreRejected(string path) =>
        Assert.Throws<ArgumentException>(() => new SqliteGridDatabase(path));

    private sealed class DatabaseFiles : IDisposable
    {
        private readonly string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GridPersistenceTests-" + Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(directory, "world;test.db");
        public SqliteGridDatabase Database { get; }
        public DatabaseFiles() { Directory.CreateDirectory(directory); Database = new(Path); }
        public SqliteConnection Raw()
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path, Pooling = false }.ToString());
            connection.Open(); return connection;
        }
        public void Dispose()
        {
            foreach (var file in Directory.GetFiles(directory)) File.Delete(file);
            Directory.Delete(directory);
        }
    }
}
