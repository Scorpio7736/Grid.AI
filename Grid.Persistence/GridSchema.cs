using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Grid.Persistence;

internal sealed record SchemaMigration(int Version, string Name, string Sql)
{
    public string Checksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Sql.Replace("\r\n", "\n"))));
}

public static class GridSchema
{
    public const int CurrentVersion = 1;
    public const int ApplicationId = 0x47524944; // ASCII GRID

    internal static readonly IReadOnlyList<SchemaMigration> Migrations = Array.AsReadOnly(new[]
    {
        new SchemaMigration(1, "Initial world storage", """
            CREATE TABLE Worlds (
                WorldId TEXT NOT NULL PRIMARY KEY CHECK(length(WorldId) = 36 AND WorldId <> '00000000-0000-0000-0000-000000000000'),
                Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
                CreatedAtUtc TEXT NOT NULL,
                GenerationSeed INTEGER,
                GeneratorVersion TEXT
            ) STRICT;

            CREATE TABLE Districts (
                WorldId TEXT NOT NULL,
                DistrictId TEXT NOT NULL CHECK(length(DistrictId) = 36 AND DistrictId <> '00000000-0000-0000-0000-000000000000'),
                Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
                Description TEXT NOT NULL,
                PRIMARY KEY (WorldId, DistrictId),
                FOREIGN KEY (WorldId) REFERENCES Worlds(WorldId) ON DELETE CASCADE
            ) STRICT;

            CREATE TABLE Programs (
                WorldId TEXT NOT NULL,
                ProgramId TEXT NOT NULL CHECK(length(ProgramId) = 36 AND ProgramId <> '00000000-0000-0000-0000-000000000000'),
                Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
                Occupation TEXT NOT NULL CHECK(length(trim(Occupation)) > 0),
                Energy REAL NOT NULL CHECK(Energy BETWEEN 0 AND 100),
                Status TEXT NOT NULL CHECK(Status IN ('Idle','Working','Moving','Recharging','Offline')),
                CurrentDistrictId TEXT NOT NULL,
                Revision INTEGER NOT NULL CHECK(Revision >= 0),
                PRIMARY KEY (WorldId, ProgramId),
                FOREIGN KEY (WorldId) REFERENCES Worlds(WorldId) ON DELETE CASCADE,
                FOREIGN KEY (WorldId, CurrentDistrictId) REFERENCES Districts(WorldId, DistrictId)
            ) STRICT;
            CREATE INDEX IX_Programs_District ON Programs(WorldId, CurrentDistrictId);

            CREATE TABLE DistrictResources (
                WorldId TEXT NOT NULL,
                DistrictId TEXT NOT NULL,
                ResourceKey TEXT NOT NULL CHECK(length(trim(ResourceKey)) > 0),
                Amount REAL NOT NULL CHECK(Amount BETWEEN -3.4028234663852886e38 AND 3.4028234663852886e38),
                PRIMARY KEY (WorldId, DistrictId, ResourceKey),
                FOREIGN KEY (WorldId, DistrictId) REFERENCES Districts(WorldId, DistrictId) ON DELETE CASCADE
            ) STRICT;

            CREATE TABLE SimulationStates (
                WorldId TEXT NOT NULL PRIMARY KEY,
                TickCount INTEGER NOT NULL CHECK(TickCount >= 0),
                ElapsedTicks INTEGER NOT NULL CHECK(ElapsedTicks >= 0),
                TickDurationTicks INTEGER NOT NULL CHECK(TickDurationTicks > 0),
                WasRunning INTEGER NOT NULL CHECK(WasRunning IN (0,1)),
                SavedAtUtc TEXT NOT NULL,
                CHECK(TickCount <= 9223372036854775807 / TickDurationTicks),
                CHECK(ElapsedTicks = TickCount * TickDurationTicks),
                FOREIGN KEY (WorldId) REFERENCES Worlds(WorldId) ON DELETE CASCADE
            ) STRICT;
            """)
    });

    internal static void Migrate(SqliteConnection connection, bool allowCreate = true) => Migrate(connection, Migrations, allowCreate);

    // Immediate transaction serializes concurrent initializers before either reads schema state.
    // An entire pending migration chain, including its markers, succeeds or rolls back together.
    internal static void Migrate(SqliteConnection connection, IReadOnlyList<SchemaMigration> migrations, bool allowCreate = true)
    {
        if (migrations.Count == 0 || migrations.Where((m, i) => m.Version != i + 1).Any())
            throw new ArgumentException("Migrations must be contiguous from version 1.", nameof(migrations));
        using var transaction = connection.BeginTransaction(deferred: false);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        long Number(string sql) { command.CommandText = sql; return Convert.ToInt64(command.ExecuteScalar()); }
        void Execute(string sql) { command.CommandText = sql; command.ExecuteNonQuery(); }
        var application = Number("PRAGMA application_id;");
        var version = Number("PRAGMA user_version;");
        if (version < 0 || version > migrations.Count)
            throw new InvalidDataException($"Unsupported database schema version {version}; maximum supported is {migrations.Count}.");
        if (application != ApplicationId && (application != 0 || version != 0))
            throw new InvalidDataException("This file is not a recognized GRID database.");
        if (version == 0)
        {
            if (!allowCreate) throw new InvalidDataException("Database is not initialized. Explicit initialization is required.");
            if (application != 0 || Number("SELECT COUNT(*) FROM sqlite_schema WHERE name NOT LIKE 'sqlite_%';") != 0)
                throw new InvalidDataException("Refusing to initialize an unversioned, nonempty database.");
            Execute("""
                CREATE TABLE SchemaMigrations (
                    Version INTEGER NOT NULL PRIMARY KEY CHECK(Version > 0),
                    Name TEXT NOT NULL,
                    Checksum TEXT NOT NULL,
                    AppliedAtUtc TEXT NOT NULL
                ) STRICT;
                """);
            Execute($"PRAGMA application_id = {ApplicationId};");
        }
        else
        {
            command.CommandText = "SELECT Version, Name, Checksum FROM SchemaMigrations ORDER BY Version;";
            using var reader = command.ExecuteReader();
            var count = 0;
            while (reader.Read())
            {
                if (count >= version || reader.GetInt64(0) != count + 1 ||
                    reader.GetString(1) != migrations[count].Name || reader.GetString(2) != migrations[count].Checksum)
                    throw new InvalidDataException("Schema migration history does not match this application.");
                count++;
            }
            if (count != version) throw new InvalidDataException("Schema version and migration history disagree.");
        }

        foreach (var migration in migrations.Skip((int)version))
        {
            Execute(migration.Sql);
            command.CommandText = "INSERT INTO SchemaMigrations VALUES ($version, $name, $checksum, $utc);";
            command.Parameters.AddWithValue("$version", migration.Version);
            command.Parameters.AddWithValue("$name", migration.Name);
            command.Parameters.AddWithValue("$checksum", migration.Checksum);
            command.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.ExecuteNonQuery();
            command.Parameters.Clear();
            Execute($"PRAGMA user_version = {migration.Version};");
        }
        command.CommandText = "PRAGMA foreign_key_check;";
        using (var violations = command.ExecuteReader())
            if (violations.Read()) throw new InvalidDataException("Database contains foreign-key violations.");
        transaction.Commit();
    }
}
