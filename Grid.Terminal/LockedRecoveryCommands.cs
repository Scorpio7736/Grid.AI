using System.Security.Cryptography;
using Grid.Persistence;
using Grid.Simulation;

namespace Grid.Terminal;

/// <summary>
/// Original-console, locked-session recovery preparation. This deliberately
/// bypasses the ordinary command registry but grants no authentication.
/// </summary>
public sealed class LockedRecoveryCommands
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(2);
    private readonly GridSimulationEngine engine;
    private readonly TerminalIdentitySession identity;
    private readonly WorldStore? store;
    private readonly bool interactiveOriginalConsole;
    private readonly TimeProvider clock;
    private PreparedChallenge? challenge;
    private sealed record PreparedChallenge(Guid Id, string Nonce, DateTimeOffset ExpiresUtc);

    public LockedRecoveryCommands(GridSimulationEngine engine, TerminalIdentitySession identity, WorldStore? store,
        bool interactiveOriginalConsole, TimeProvider? clock = null)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
        this.store = store;
        this.interactiveOriginalConsole = interactiveOriginalConsole;
        this.clock = clock ?? TimeProvider.System;
    }

    public bool TryExecute(string line, out CommandResult result)
    {
        var text = line.Trim();
        var boundary = text.IndexOfAny([' ', '\t', '\r', '\n']);
        var first = boundary < 0 ? text : text[..boundary];
        if (!first.Equals("::grid-rescue", StringComparison.OrdinalIgnoreCase))
        { result = new(""); return false; }
        IReadOnlyList<string> tokens;
        try { tokens = CommandDispatcher.Parse(line); }
        catch (ArgumentException) { result = new(DevelopmentBreakGlassRegistry.GenericDenial); return true; }
        if (!Eligible())
        { result = new(DevelopmentBreakGlassRegistry.GenericDenial); return true; }
        PurgeExpired();
        result = tokens.Count switch
        {
            2 when tokens[1].Equals("status", StringComparison.OrdinalIgnoreCase) => Status(),
            2 when tokens[1].Equals("verify-world", StringComparison.OrdinalIgnoreCase) => VerifyWorld(),
            2 when tokens[1].Equals("freeze-request", StringComparison.OrdinalIgnoreCase) => Freeze(),
            2 when tokens[1].Equals("recovery-challenge", StringComparison.OrdinalIgnoreCase) => PrepareChallenge(),
            3 when tokens[1].Equals("cancel", StringComparison.OrdinalIgnoreCase) => Cancel(tokens[2]),
            _ => new(DevelopmentBreakGlassRegistry.GenericDenial)
        };
        return true;
    }

    private bool Eligible() => interactiveOriginalConsole && !identity.IsAttachment && !identity.IsAuthenticated;

    private CommandResult Status()
    {
        var snapshot = engine.GetSnapshot();
        var path = store?.DatabasePath ?? "Unavailable (memory-only session)";
        var primary = store is not null && File.Exists(store.DatabasePath);
        var backup = store is not null && File.Exists(store.BackupPath);
        return new($"LOCKED LOCAL RECOVERY STATUS\nRegistry: v{DevelopmentBreakGlassRegistry.Version}\n" +
            $"Session: Locked original console\nWorld: {snapshot.WorldId}\nWorld path: {path}\n" +
            $"Simulation: {(snapshot.IsRunning ? "Running" : "Paused")}\nPrimary present: {primary}\nBackup present: {backup}\n" +
            "No account, transcript, prompt, response, task content or private context was inspected.");
    }

    private CommandResult VerifyWorld()
    {
        if (store is null) return new("World verification unavailable: this is a memory-only session. Nothing changed.");
        var report = store.VerifyReadOnly(engine.GetSnapshot().WorldId);
        static string FileStatus(string label, WorldFileVerification file) =>
            $"{label}: {file.Status} | Present: {file.Present} | Integrity: {file.IntegrityValid} | Schema: {(file.SchemaVersion?.ToString() ?? "unknown")} | World match: {file.WorldMatches}";
        return new("READ-ONLY WORLD VERIFICATION\n" +
            FileStatus("Primary", report.Primary) + "\n" + FileStatus("Backup", report.Backup) + "\n" +
            $"Recovery marker: {report.RecoveryMarkerPresent} | Nullification marker: {report.NullificationMarkerPresent} | SQLite sidecars: {report.SidecarsPresent.Count}\n" +
            "No migration, repair, replacement, save or recovery was performed.");
    }

    private CommandResult Freeze()
    {
        var wasRunning = engine.IsRunning;
        engine.Pause();
        engine.AuditIdentity("LockedRecoveryFreeze", null, identity.Generation, true);
        return new(wasRunning
            ? "GRID activity frozen. Simulation and new agent activity are paused. Login is still required for any privileged action."
            : "GRID activity was already frozen. Login is still required for any privileged action.");
    }

    private CommandResult PrepareChallenge()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        challenge = new(Guid.NewGuid(), Convert.ToHexString(bytes), clock.GetUtcNow().Add(ChallengeLifetime));
        engine.AuditIdentity("LockedRecoveryChallengePrepared", null, identity.Generation, true);
        return new($"Recovery preparation created.\nOperation: {challenge.Id}\nNonce: {challenge.Nonce}\nExpires: {challenge.ExpiresUtc:O}\n" +
            "This preparation does not authenticate anyone, reset credentials, approve work, expose private content, recover a world or authorize nullification. A future offline owner-rescue flow must require separate user-held recovery material.");
    }

    private CommandResult Cancel(string operationId)
    {
        if (!Guid.TryParse(operationId, out var id) || challenge?.Id != id)
            return new("No matching active recovery preparation. Nothing changed.");
        challenge = null;
        engine.AuditIdentity("LockedRecoveryPreparationCancelled", null, identity.Generation, true);
        return new("Recovery preparation cancelled. No authentication or world state was changed.");
    }

    private void PurgeExpired()
    {
        if (challenge?.ExpiresUtc <= clock.GetUtcNow()) challenge = null;
    }
}
