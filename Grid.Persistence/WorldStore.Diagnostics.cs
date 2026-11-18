using Microsoft.Data.Sqlite;

namespace Grid.Persistence;

public sealed record WorldFileVerification(bool Present, bool IntegrityValid, int? SchemaVersion, Guid? WorldId, bool WorldMatches, string Status);
public sealed record WorldRecoveryVerification(string DatabasePath, WorldFileVerification Primary,
    WorldFileVerification Backup, bool RecoveryMarkerPresent, bool NullificationMarkerPresent,
    IReadOnlyList<string> SidecarsPresent);

public sealed partial class WorldStore
{
    /// <summary>
    /// Performs read-only SQLite and identity checks. It never creates, migrates,
    /// repairs, replaces or saves either world file.
    /// </summary>
    public WorldRecoveryVerification VerifyReadOnly(Guid expectedWorldId)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var sidecars = new[] { "-journal", "-wal", "-shm" }
                .Where(suffix => File.Exists(DatabasePath + suffix)).ToArray();
            return new(DatabasePath, VerifyFile(DatabasePath, expectedWorldId),
                VerifyFile(BackupPath, expectedWorldId), File.Exists(RecoveryMarker),
                File.Exists(NullificationMarker), Array.AsReadOnly(sidecars));
        }
    }

    private static WorldFileVerification VerifyFile(string path, Guid expectedWorldId)
    {
        if (!File.Exists(path)) return new(false, false, null, null, false, "Missing");
        try
        {
            using var connection = new SqliteGridDatabase(path).OpenReadOnly();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var integrity = string.Equals(command.ExecuteScalar() as string, "ok", StringComparison.Ordinal);
            command.CommandText = "PRAGMA user_version;";
            var schema = Convert.ToInt32(command.ExecuteScalar());
            command.CommandText = "SELECT WorldId FROM Worlds LIMIT 2;";
            var ids = new List<Guid>();
            using (var reader = command.ExecuteReader())
                while (reader.Read() && ids.Count < 2)
                    if (Guid.TryParse(reader.GetString(0), out var id)) ids.Add(id);
                    else return new(true, integrity, schema, null, false, "Invalid world identifier");
            Guid? worldId = ids.Count == 1 ? ids[0] : null;
            var status = !integrity ? "Integrity check failed" :
                schema != GridSchema.CurrentVersion ? "Schema version mismatch" :
                ids.Count != 1 ? "World identity count invalid" :
                worldId != expectedWorldId ? "World identity mismatch" : "Valid";
            return new(true, integrity, schema, worldId, worldId == expectedWorldId, status);
        }
        catch (SqliteException) { return new(true, false, null, null, false, "Unreadable SQLite database"); }
        catch (IOException) { return new(true, false, null, null, false, "I/O verification failure"); }
        catch (UnauthorizedAccessException) { return new(true, false, null, null, false, "Access denied during verification"); }
        catch (InvalidOperationException) { return new(true, false, null, null, false, "Invalid database structure"); }
    }
}
