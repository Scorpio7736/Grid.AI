using System.Text.Json;
using Grid.Communication;

namespace Grid.Persistence;

public sealed record OfflineRescueState(Guid WorldId, int SchemaVersion, OfflineRecoveryCredential? Recovery);

public sealed partial class WorldStore
{
    public OfflineRescueState ReadOfflineRescueState(string? source = null)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var path = source ?? DatabasePath;
            if (!File.Exists(path)) throw new IOException("World file is missing.");
            using var connection = new SqliteGridDatabase(path).OpenReadOnly();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            if (!string.Equals(command.ExecuteScalar() as string, "ok", StringComparison.Ordinal))
                throw new InvalidDataException("World integrity check failed.");
            command.CommandText = "PRAGMA user_version;";
            var schema = Convert.ToInt32(command.ExecuteScalar());
            if (schema != GridSchema.CurrentVersion) throw new InvalidDataException("Offline rescue requires the current world schema.");
            command.CommandText = "SELECT WorldId FROM Worlds LIMIT 2;";
            var ids = new List<Guid>();
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                    if (Guid.TryParse(reader.GetString(0), out var id)) ids.Add(id);
                    else throw new InvalidDataException("World identifier is invalid.");
            if (ids.Count != 1) throw new InvalidDataException("World identity count is invalid.");
            command.CommandText = "SELECT StateJson FROM WorldIdentities WHERE WorldId = $world";
            command.Parameters.AddWithValue("$world", ids[0].ToString("D"));
            var json = command.ExecuteScalar() as string ?? throw new InvalidDataException("World identity state is missing.");
            var identities = JsonSerializer.Deserialize<LocalIdentityState>(json) ?? throw new InvalidDataException("World identity state is invalid.");
            LocalIdentityState.Validate(identities);
            return new(ids[0], schema, identities.OfflineRecovery);
        }
    }

    public void ResetOwnerCredentialOffline(Guid expectedWorld, OfflineRecoveryCredential expectedRecovery,
        PasswordCredential replacement)
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            PasswordCredential.Validate(replacement); OfflineRecoveryCredential.Validate(expectedRecovery);
            var saved = Read(DatabasePath);
            if (saved.Info.Id != expectedWorld || saved.State.Identities.OfflineRecovery != expectedRecovery)
                throw new UnauthorizedAccessException("Offline recovery authority changed; reset refused.");
            var identities = saved.State.Identities with
            {
                Credential = replacement,
                ChallengeCredentials = null,
                ChallengeRateLimits = ChallengeRateLimitState.Empty
            };
            LocalIdentityState.Validate(identities);
            Save(saved.Info, saved.State with { Identities = identities, IsRunning = false });
        }
    }

    public WorldRecoveryPlan PrepareOfflineRecovery(Guid expectedWorld, OfflineRecoveryCredential expectedRecovery)
    {
        lock (gate)
        {
            var primary = ReadOfflineRescueState(DatabasePath);
            var backup = ReadOfflineRescueState(BackupPath);
            if (primary.WorldId != expectedWorld || backup.WorldId != expectedWorld ||
                primary.Recovery != expectedRecovery || backup.Recovery != expectedRecovery)
                throw new UnauthorizedAccessException("Primary and backup do not share the authenticated recovery authority.");
            return PrepareRecovery(expectedWorld);
        }
    }
}
