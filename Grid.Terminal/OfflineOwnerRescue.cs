using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grid.Communication;
using Grid.Persistence;

namespace Grid.Terminal;

/// <summary>Exclusive no-AI/no-network/no-agent offline owner rescue entry point.</summary>
public static class OfflineOwnerRescue
{
    public static async Task<int> RunAsync(string worldPath, ITerminalInput input, TextWriter output,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(output);
        if (!input.Interactive)
        {
            await output.WriteLineAsync("Offline owner rescue requires a local interactive console. Redirected/unattended input is refused.");
            return 2;
        }
        try
        {
            worldPath = ValidateWorldPath(worldPath);
            using var store = new WorldStore(worldPath); // Exclusive lease proves the normal host is stopped.
            var state = store.ReadOfflineRescueState();
            if (state.Recovery is null)
            {
                await output.WriteLineAsync("Offline owner rescue is not enrolled for this world. No universal recovery key exists.");
                return 2;
            }
            await output.WriteLineAsync($"GRID.AI // OFFLINE OWNER RESCUE\nWorld: {state.WorldId}\nSchema: {state.SchemaVersion}\n" +
                "Primary integrity and identity verified read-only. No AI, network, agents, conversations or shell are active.");
            var keyPath = await Read(input, output, "Recovery key file: ", false, token);
            if (keyPath is null) return 2;
            OfflineRecoveryKeyDocument key;
            try { key = OfflineRecoveryKeyFile.Read(keyPath); }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or JsonException)
            { await output.WriteLineAsync("Recovery authorization failed. No world or credential state changed."); WriteAudit(store, state.WorldId, "KeyRejected", false); return 2; }
            if (key.WorldId != state.WorldId || key.KeyId != state.Recovery.KeyId || !state.Recovery.Verify(key.Secret))
            {
                await output.WriteLineAsync("Recovery authorization failed. No world or credential state changed.");
                WriteAudit(store, state.WorldId, "KeyRejected", false); return 2;
            }
            WriteAudit(store, state.WorldId, "KeyAccepted", true);
            await output.WriteLineAsync("Recovery material verified. It authenticates this rescue session only; it does not start GRID or grant a shell.\n" +
                "1. Verify and exit\n2. Reset local username/password\n3. Restore validated backup\nChoose 1, 2 or 3:");
            var choice = await Read(input, output, "> ", false, token);
            return choice?.Trim() switch
            {
                "1" => await VerifyOnly(output, store, state),
                "2" => await ResetPassword(input, output, store, state, token),
                "3" => await RestoreBackup(input, output, store, state, token),
                _ => await Refused(output, store, state.WorldId, "InvalidChoice", "Invalid rescue choice. Nothing changed.")
            };
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or System.Data.Common.DbException)
        {
            await output.WriteLineAsync("Offline rescue refused. Exclusive access to a stopped GRID world is required. " +
                "Close every GRID process using this world and verify the supplied files before retrying. Details: " + error.Message +
                " No automatic retry was performed.");
            return 2;
        }
    }

    private static async Task<int> VerifyOnly(TextWriter output, WorldStore store, OfflineRescueState state)
    {
        var report = store.VerifyReadOnly(state.WorldId);
        await output.WriteLineAsync($"Verification complete. Primary: {report.Primary.Status}; Backup: {report.Backup.Status}; recovery marker: {report.RecoveryMarkerPresent}; nullification marker: {report.NullificationMarkerPresent}. Nothing changed.");
        WriteAudit(store, state.WorldId, "VerifyOnly", true); return 0;
    }

    private static async Task<int> ResetPassword(ITerminalInput input, TextWriter output, WorldStore store,
        OfflineRescueState state, CancellationToken token)
    {
        var username = await Read(input, output, "New username: ", false, token); if (username is null) return 2;
        var password = await Read(input, output, "New password: ", true, token); if (password is null) return 2;
        var confirm = await Read(input, output, "Confirm password: ", true, token); if (confirm is null) return 2;
        if (password != confirm) { await output.WriteLineAsync("Passwords do not match. Nothing changed."); return 2; }
        PasswordCredential credential;
        try { credential = PasswordCredential.Create(username, password); }
        finally { password = ""; confirm = ""; }
        if (!await Confirm(input, output, state.WorldId, "RESET LOCAL PASSWORD", token)) return 2;
        store.ResetOwnerCredentialOffline(state.WorldId, state.Recovery!, credential);
        WriteAudit(store, state.WorldId, "PasswordReset", true);
        await output.WriteLineAsync("Local username/password reset. Challenge sessions and challenge credentials were revoked. Registered agents, transcripts and world data were retained. Restart Grid.AI and log in normally.");
        return 0;
    }

    private static async Task<int> RestoreBackup(ITerminalInput input, TextWriter output, WorldStore store,
        OfflineRescueState state, CancellationToken token)
    {
        var plan = store.PrepareOfflineRecovery(state.WorldId, state.Recovery!);
        await output.WriteLineAsync("Backup is valid, matches this world and carries the same authenticated recovery authority. The current primary and sidecars will be archived, not silently erased.");
        if (!await Confirm(input, output, state.WorldId, "RESTORE BACKUP", token)) return 2;
        store.Recover(plan);
        WriteAudit(store, state.WorldId, "BackupRestored", true);
        await output.WriteLineAsync("Validated backup restored. Displaced primary/sidecars were archived. Restart Grid.AI; no final save was performed.");
        return 0;
    }

    private static async Task<bool> Confirm(ITerminalInput input, TextWriter output, Guid world, string action,
        CancellationToken token)
    {
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        await output.WriteLineAsync($"{action}\nWorld: {world}\nNonce: {nonce}\nType the exact world ID, one space, and nonce to commit:");
        var answer = await Read(input, output, "> ", false, token);
        var expected = world.ToString("D") + " " + nonce;
        var a = Encoding.UTF8.GetBytes(answer ?? ""); var b = Encoding.UTF8.GetBytes(expected);
        try
        {
            var valid = a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
            if (!valid) await output.WriteLineAsync("Confirmation failed. Nothing changed.");
            return valid;
        }
        finally { CryptographicOperations.ZeroMemory(a); CryptographicOperations.ZeroMemory(b); }
    }

    private static async Task<int> Refused(TextWriter output, WorldStore store, Guid world, string action, string message)
    { await output.WriteLineAsync(message); WriteAudit(store, world, action, false); return 2; }

    private static async Task<string?> Read(ITerminalInput input, TextWriter output, string prompt, bool secret, CancellationToken token)
    { await output.WriteAsync(prompt); await output.FlushAsync(); return await input.ReadAsync(secret, token); }

    private static string ValidateWorldPath(string value)
    {
        if (!Path.IsPathFullyQualified(value)) throw new ArgumentException("Use an absolute world database path.");
        var path = Path.GetFullPath(value);
        if (!File.Exists(path) || Directory.Exists(path)) throw new IOException("World database does not exist.");
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Offline rescue refuses links and junctions.");
        return path;
    }

    private static void WriteAudit(WorldStore store, Guid world, string action, bool success)
    {
        var path = store.DatabasePath + ".operator-audit.jsonl";
        if (File.Exists(path) && new FileInfo(path).Length > 4_000_000) throw new IOException("Operator audit is full; archive it before rescue.");
        var line = JsonSerializer.Serialize(new { Contract = "Grid.AI.OfflineOwnerRescueAudit.v1", TimestampUtc = DateTimeOffset.UtcNow,
            Action = action, Success = success, WorldId = world, RegistryVersion = DevelopmentBreakGlassRegistry.Version }) + "\n";
        using var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        file.Seek(0, SeekOrigin.End); var bytes = Encoding.UTF8.GetBytes(line); file.Write(bytes); file.Flush(true);
    }
}
