using Microsoft.Data.Sqlite;

namespace Grid.Persistence;

/// <summary>Explicit file-backed schema lifecycle. Does not save or rehydrate a simulation world.</summary>
public sealed class SqliteGridDatabase
{
    public string DatabasePath { get; }

    public SqliteGridDatabase(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (databasePath == ":memory:" || databasePath.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Use a filesystem path, not a SQLite URI or in-memory database.", nameof(databasePath));
        DatabasePath = Path.GetFullPath(databasePath);
    }

    /// <summary>Explicitly creates an empty schema or upgrades a recognized GRID schema. Parent folder must exist.</summary>
    public void Initialize()
    {
        using var connection = Open(SqliteOpenMode.ReadWriteCreate);
        GridSchema.Migrate(connection);
    }

    /// <summary>Opens and validates/upgrades an existing database; never creates a missing file. Caller disposes.</summary>
    public SqliteConnection OpenExisting()
    {
        var connection = Open(SqliteOpenMode.ReadWrite);
        try
        {
            GridSchema.Migrate(connection, allowCreate: false);
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }

    /// <summary>Opens an existing database without creating or migrating it. Intended for bounded diagnostics.</summary>
    public SqliteConnection OpenReadOnly()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath, Mode = SqliteOpenMode.ReadOnly, ForeignKeys = true,
            Pooling = false, Cache = SqliteCacheMode.Private, DefaultTimeout = 5
        }.ToString());
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA query_only = ON; PRAGMA foreign_keys;";
            if (Convert.ToInt64(command.ExecuteScalar()) != 1)
                throw new InvalidOperationException("SQLite foreign-key enforcement is required.");
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }

    private SqliteConnection Open(SqliteOpenMode mode)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath, Mode = mode, ForeignKeys = true,
            Pooling = false, Cache = SqliteCacheMode.Private, DefaultTimeout = 5
        }.ToString());
        try
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA synchronous = FULL;";
            command.ExecuteNonQuery();
            command.CommandText = "PRAGMA foreign_keys;";
            if (Convert.ToInt64(command.ExecuteScalar()) != 1)
                throw new InvalidOperationException("SQLite foreign-key enforcement is required.");
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }
}
