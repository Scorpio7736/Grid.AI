using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grid.Persistence;
using Grid.Simulation;

namespace Grid.Terminal;

/// <summary>Original-console Master Control emergency plans. No nullification exists here.</summary>
public sealed class MasterBreakGlassCommands
{
    private static readonly TimeSpan PlanLifetime = TimeSpan.FromMinutes(2);
    private readonly GridSimulationEngine engine;
    private readonly TerminalIdentitySession identity;
    private readonly TerminalSessionRegistry sessions;
    private readonly WorldStore? store;
    private readonly IWorldRecoverySession? recovery;
    private readonly EmergencyControlState emergency;
    private readonly bool interactiveOriginalConsole;
    private readonly Func<Task>? stopAttachments;
    private readonly Func<CancellationToken, Task>? save;
    private readonly TimeProvider clock;
    private Plan? active;

    private sealed record Plan(Guid Id, string Action, string Nonce, DateTimeOffset ExpiresUtc,
        Grid.Simulation.Editing.EditSessionReference Session, Guid WorldId, Guid WorldInstanceId,
        long ActivityGeneration, string? Target = null, WorldRecoveryPlan? Recovery = null);

    public MasterBreakGlassCommands(GridSimulationEngine engine, TerminalIdentitySession identity,
        TerminalSessionRegistry sessions, WorldStore? store, IWorldRecoverySession? recovery,
        EmergencyControlState emergency, bool interactiveOriginalConsole,
        Func<Task>? stopAttachments = null, Func<CancellationToken, Task>? save = null,
        TimeProvider? clock = null)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
        this.sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        this.store = store;
        this.recovery = recovery;
        this.emergency = emergency ?? throw new ArgumentNullException(nameof(emergency));
        this.interactiveOriginalConsole = interactiveOriginalConsole;
        this.stopAttachments = stopAttachments;
        this.save = save;
        this.clock = clock ?? TimeProvider.System;
    }

    public bool TryExecute(string line, out CommandResult result)
    {
        var text = line.Trim();
        var boundary = text.IndexOfAny([' ', '\t', '\r', '\n']);
        var first = boundary < 0 ? text : text[..boundary];
        if (!first.Equals("::grid-breakglass", StringComparison.OrdinalIgnoreCase))
        { result = new(""); return false; }
        IReadOnlyList<string> tokens;
        try { tokens = CommandDispatcher.Parse(line); }
        catch (ArgumentException) { result = Denied(); return true; }
        if (!Eligible()) { result = Denied(); return true; }
        PurgeExpired();
        try { result = Route(tokens); }
        catch (Exception error) when (error is IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException or System.Data.Common.DbException)
        { result = new("Emergency operation failed or was refused: " + error.Message + " Review emergency status; no automatic retry was attempted."); }
        return true;
    }

    private CommandResult Route(IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 2 && tokens[1].Equals("status", StringComparison.OrdinalIgnoreCase)) return Status();
        if (tokens.Count == 2 && tokens[1].Equals("safe-mode-plan", StringComparison.OrdinalIgnoreCase)) return Issue("safe-mode");
        if (tokens.Count == 4 && tokens[1].Equals("safe-mode-commit", StringComparison.OrdinalIgnoreCase)) return Commit("safe-mode", tokens[2], tokens[3]);
        if (tokens.Count == 3 && tokens[1].Equals("recovery-plan", StringComparison.OrdinalIgnoreCase) && tokens[2].Equals("backup", StringComparison.OrdinalIgnoreCase))
            return IssueRecovery();
        if (tokens.Count == 4 && tokens[1].Equals("recovery-commit", StringComparison.OrdinalIgnoreCase)) return Commit("recovery", tokens[2], tokens[3]);
        if (tokens.Count == 3 && tokens[1].Equals("cancel", StringComparison.OrdinalIgnoreCase)) return Cancel(tokens[2]);
        if (tokens.Count == 3 && tokens[1].Equals("evidence-export", StringComparison.OrdinalIgnoreCase)) return IssueEvidence(tokens[2]);
        if (tokens.Count == 5 && tokens[1].Equals("evidence-export", StringComparison.OrdinalIgnoreCase)) return CommitEvidence(tokens[2], tokens[3], tokens[4]);
        if (tokens.Count == 2 && IsImmediate(tokens[1])) return Issue(tokens[1].ToLowerInvariant());
        if (tokens.Count == 4 && IsImmediate(tokens[1])) return Commit(tokens[1].ToLowerInvariant(), tokens[2], tokens[3]);
        return Denied();
    }

    private static bool IsImmediate(string action) => action.ToLowerInvariant() is "freeze" or "isolate" or "revoke-sessions" or "disable-remote";
    private bool Eligible() => interactiveOriginalConsole && !identity.IsAttachment && identity.IsAuthenticated && identity.IsMaster;
    private static CommandResult Denied() => new(DevelopmentBreakGlassRegistry.GenericDenial);

    private CommandResult Status()
    {
        var snapshot = engine.GetSnapshot();
        return new($"MASTER CONTROL EMERGENCY STATUS\nWorld: {snapshot.WorldId}\nInstance: {engine.WorldInstanceId}\n" +
            $"Activity: {(snapshot.IsRunning ? "Running" : "Frozen")}\nRemote acceptance: {(emergency.RemoteDisabled ? "Disabled" : "No listener active; emergency gate open")}\n" +
            $"Safe mode: {emergency.SafeMode}\nPending plan: {(active is null ? "none" : active.Action + " until " + active.ExpiresUtc.ToString("O"))}\n" +
            "No credential material, transcript bodies, prompts, private context or task arguments/results were inspected.");
    }

    private CommandResult Issue(string action, string? target = null, WorldRecoveryPlan? recoveryPlan = null)
    {
        Span<byte> bytes = stackalloc byte[24]; RandomNumberGenerator.Fill(bytes);
        var snapshot = engine.GetSnapshot();
        active = new(Guid.NewGuid(), action, Convert.ToHexString(bytes), clock.GetUtcNow().Add(PlanLifetime),
            identity.Reference, snapshot.WorldId, engine.WorldInstanceId, engine.ActivityGeneration, target, recoveryPlan);
        engine.AuditMasterBreakGlass(identity.Reference, "Plan issued: " + action + "; no nonce or private content recorded.");
        try { WriteAudit("PlanIssued", action, active.Id, true); }
        catch { active = null; throw; }
        return new($"EMERGENCY PLAN - NO CHANGE APPLIED\nAction: {action}\nOperation: {active.Id}\nWorld: {active.WorldId}\nInstance: {active.WorldInstanceId}\n" +
            (target is null ? "" : $"Target: {target}\n") + $"Nonce: {active.Nonce}\nExpires: {active.ExpiresUtc:O}\n" +
            $"Commit only after reviewing the exact target: ::grid-breakglass {CommitSyntax(action, target)} {active.Id} {active.Nonce}");
    }

    private CommandResult IssueRecovery()
    {
        if (recovery is null) return new("Recovery unavailable: no persistent world session. Nothing changed.");
        try { var plan = recovery.PrepareRecovery(); return Issue("recovery", "validated backup", plan); }
        catch (Exception error) when (error is IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        { return new("Recovery plan refused: " + error.Message + " Nothing changed."); }
    }

    private CommandResult IssueEvidence(string target)
    {
        try { return Issue("evidence", ValidateEvidencePath(target)); }
        catch (Exception error) when (error is IOException or ArgumentException or UnauthorizedAccessException)
        { return new("Evidence plan refused: " + error.Message + " Nothing changed."); }
    }

    private CommandResult CommitEvidence(string target, string operation, string nonce)
    {
        string path;
        try { path = ValidateEvidencePath(target, requireAbsent: false); }
        catch (Exception error) when (error is IOException or ArgumentException or UnauthorizedAccessException)
        { return new("Evidence export refused: " + error.Message + " Nothing changed."); }
        var plan = Consume("evidence", operation, nonce, path);
        if (plan is null) return Denied();
        if (File.Exists(path) || Directory.Exists(path)) return new("Evidence export refused: target changed or now exists. Nothing was overwritten.");
        var snapshot = engine.GetSnapshot();
        var verification = store?.VerifyReadOnly(snapshot.WorldId);
        var evidence = new
        {
            Contract = "Grid.AI.MasterBreakGlassEvidence.v1", GeneratedUtc = clock.GetUtcNow(),
            RegistryVersion = DevelopmentBreakGlassRegistry.Version, WorldId = snapshot.WorldId,
            WorldInstanceId = engine.WorldInstanceId, snapshot.TickCount, snapshot.IsRunning,
            RemoteDisabled = emergency.RemoteDisabled, SafeMode = emergency.SafeMode,
            Tasks = new { Total = snapshot.Tasks.Records.Count,
                Awaiting = snapshot.Tasks.Records.Count(t => t.Status == Grid.Simulation.Tasks.TaskStatus.AwaitingApproval),
                Queued = snapshot.Tasks.Records.Count(t => t.Status == Grid.Simulation.Tasks.TaskStatus.Queued),
                Running = snapshot.Tasks.Records.Count(t => t.Status == Grid.Simulation.Tasks.TaskStatus.Running) },
            WorldFiles = verification is null ? null : new { Primary = verification.Primary.Status, Backup = verification.Backup.Status,
                verification.RecoveryMarkerPresent, verification.NullificationMarkerPresent, SidecarCount = verification.SidecarsPresent.Count }
        };
        var json = JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true });
        using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough))
        { var data = Encoding.UTF8.GetBytes(json); file.Write(data); file.Flush(true); }
        engine.AuditMasterBreakGlass(identity.Reference, "Bounded evidence exported; content bodies and secrets excluded.");
        Save(); WriteAudit("Completed", "evidence", plan.Id, true);
        return new($"Evidence exported: {path}\nSHA256: {Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))}\nNo credentials, challenge material, transcript bodies, prompts, private context, task arguments or results were exported.");
    }

    private CommandResult Commit(string action, string operation, string nonce)
    {
        var plan = Consume(action, operation, nonce);
        if (plan is null) return Denied();
        var reference = identity.Reference;
        switch (action)
        {
            case "freeze":
                engine.MasterBreakGlassFreeze(reference, "Freeze committed"); Save();
                WriteAudit("Completed", action, plan.Id, true);
                return new("Emergency freeze committed and audited. GRID activity is paused; no unrelated host process was stopped.");
            case "disable-remote":
                engine.MasterBreakGlassDisableRemoteAuthority(reference, "Remote disable committed"); emergency.DisableRemote(); Save();
                WriteAudit("Completed", action, plan.Id, true);
                return new("Remote acceptance disabled and transient challenge endpoints/claims revoked. No network listener was active in this build.");
            case "revoke-sessions": return Revoke(reference, isolate: false, plan.Id);
            case "isolate": return Revoke(reference, isolate: true, plan.Id);
            case "safe-mode":
                engine.MasterBreakGlassFreeze(reference, "Safe mode committed");
                _ = engine.MasterBreakGlassRevokeApprovals(reference);
                emergency.EnterSafeMode(); StopAttachments(); Save();
                WriteAudit("Completed", action, plan.Id, true);
                return new("Safe mode active. Simulation, agents, conversations, inference, voice, remote acceptance and ordinary task commands are disabled until restart.");
            case "recovery":
                if (plan.Recovery is null) return Denied();
                engine.MasterBreakGlassFreeze(reference, "Recovery commit accepted; shutdown without final save requested.");
                return new("Validated backup recovery committed. GRID will stop without a final save, revalidate the bound hashes, recover the world and require restart.",
                    WorldRecovery: new(plan.Recovery, () => identity.IsMaster && identity.Reference == reference,
                        (stage, success) => WriteAudit(stage, action, plan.Id, success)));
            default: return Denied();
        }
    }

    private CommandResult Revoke(Grid.Simulation.Editing.EditSessionReference reference, bool isolate, Guid operation)
    {
        engine.MasterBreakGlassFreeze(reference, isolate ? "Isolation committed" : "Session revocation committed");
        var revoked = engine.MasterBreakGlassRevokeApprovals(reference);
        if (isolate) emergency.DisableRemote();
        Save(); StopAttachments();
        var count = sessions.RevokeAll();
        WriteAudit("Completed", isolate ? "isolate" : "revoke-sessions", operation, true);
        return new($"{(isolate ? "Isolation" : "Session revocation")} committed. Sessions revoked: {count}; queued approvals revoked: {revoked.QueuedApprovalsRevoked}; running tasks cancelled: {revoked.RunningTasksCancelled}. Terminal is locked.");
    }

    private Plan? Consume(string action, string operation, string nonce, string? target = null)
    {
        if (!Guid.TryParse(operation, out var id) || active is not { } plan || plan.Id != id || plan.Action != action ||
            plan.ExpiresUtc <= clock.GetUtcNow() || plan.Session != identity.Reference || plan.WorldId != engine.GetSnapshot().WorldId ||
            plan.WorldInstanceId != engine.WorldInstanceId || plan.ActivityGeneration != engine.ActivityGeneration ||
            target is not null && !string.Equals(plan.Target, target, StringComparison.Ordinal) || !FixedEquals(plan.Nonce, nonce)) return null;
        active = null;
        WriteAudit("CommitAccepted", action, plan.Id, true);
        return plan;
    }

    private CommandResult Cancel(string operation)
    {
        if (!Guid.TryParse(operation, out var id) || active?.Id != id) return new("No matching active emergency plan. Nothing changed.");
        var action = active.Action; active = null;
        engine.AuditMasterBreakGlass(identity.Reference, "Plan cancelled: " + action + ".");
        WriteAudit("Cancelled", action, id, true);
        return new("Emergency plan cancelled. Nothing changed.");
    }

    private void PurgeExpired() { if (active?.ExpiresUtc <= clock.GetUtcNow()) active = null; }
    private static bool FixedEquals(string expected, string actual)
    {
        var a = Encoding.UTF8.GetBytes(expected); var b = Encoding.UTF8.GetBytes(actual);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
    private static string CommitSyntax(string action, string? target) => action switch
    {
        "safe-mode" => "safe-mode-commit", "recovery" => "recovery-commit",
        "evidence" => $"evidence-export \"{target}\"", _ => action
    };
    private string ValidateEvidencePath(string value, bool requireAbsent = true)
    {
        if (!Path.IsPathFullyQualified(value) || !Path.GetExtension(value).Equals(".json", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Use an absolute new .json file path.");
        var path = Path.GetFullPath(value);
        if (requireAbsent && (File.Exists(path) || Directory.Exists(path))) throw new IOException("Evidence target already exists; overwrite is refused.");
        var parent = Path.GetDirectoryName(path) ?? throw new ArgumentException("Evidence target requires a parent folder.");
        if (!Directory.Exists(parent)) throw new IOException("Evidence parent folder does not exist.");
        for (var current = parent; current is not null; current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new IOException("Evidence target cannot traverse a link or junction.");
        if (store is not null && (path.Equals(store.DatabasePath, StringComparison.OrdinalIgnoreCase) || path.Equals(store.BackupPath, StringComparison.OrdinalIgnoreCase)))
            throw new IOException("Evidence target cannot replace a world file.");
        return path;
    }
    private void StopAttachments() { stopAttachments?.Invoke().GetAwaiter().GetResult(); }
    private void Save() { save?.Invoke(CancellationToken.None).GetAwaiter().GetResult(); }
    private void WriteAudit(string stage, string action, Guid? operation, bool success)
    {
        if (store is null) return;
        var path = store.DatabasePath + ".operator-audit.jsonl";
        if (File.Exists(path) && new FileInfo(path).Length > 4_000_000)
            throw new IOException("Operator audit reached its bounded size; archive it before another emergency operation.");
        var snapshot = engine.GetSnapshot();
        var line = JsonSerializer.Serialize(new
        {
            Contract = "Grid.AI.MasterBreakGlassAudit.v1", TimestampUtc = clock.GetUtcNow(),
            Stage = stage, Action = action, OperationId = operation, Success = success,
            WorldId = snapshot.WorldId, WorldInstanceId = engine.WorldInstanceId,
            SessionGeneration = identity.Generation, RegistryVersion = DevelopmentBreakGlassRegistry.Version
        }) + "\n";
        using var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        file.Seek(0, SeekOrigin.End); var bytes = Encoding.UTF8.GetBytes(line); file.Write(bytes); file.Flush(true);
    }
}
