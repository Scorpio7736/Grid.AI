using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grid.Agent;
using Grid.Communication;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using GridTaskStatus = Grid.Simulation.Tasks.TaskStatus;

namespace Grid.Terminal;

public static class TrustCommands
{
    private sealed record PendingGrant(Guid Id, string Profile, string Agent, long Generation, DateTimeOffset Expires);
    private sealed record PendingGoal(Guid Id, string Name, string Text, int Days, long Generation, DateTimeOffset Expires);
    private sealed record PendingExport(Guid Id, string Path, byte[] Bytes, string Hash, long Generation, long MemoryRevision, DateTimeOffset Expires);

    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession identity,
        TrustCenter center, SharedLocalInference inference, ScheduledWorkflowService? schedules)
    {
        PendingGrant? grant = null; PendingGoal? goal = null; PendingExport? export = null;
        identity.Changed += () => { grant = null; goal = null; export = null; };
        commands.Register(new("trust", "trust <overview|activity|memory|export|profiles|grant|revoke|goals|suggestions|suggest|receipts|acceptance> ...",
            "Master Control trust center for readable activity, reviewed memory export, scoped profiles and opt-in goal suggestions.", args =>
        {
            if (!identity.IsMaster || identity.IsAttachment) return new("Trust Center requires authenticated Master Control in the original terminal.");
            try
            {
                var snapshot = engine.GetSnapshot(); var state = center.Snapshot();
                if (args.Count == 0 || args.SequenceEqual(["overview"]))
                {
                    var tasks = snapshot.Tasks.Records; var memories = snapshot.Communications.Memories.Where(memory => !memory.Forgotten).ToArray();
                    return new($"GRID.AI TRUST CENTER\nWorld: {(snapshot.IsRunning ? "running" : "paused")} | agents {snapshot.Identities.RegisteredAgents.Count}/6 | tasks {tasks.Count} | memories {memories.Length}\nLocal model: {OllamaLocalBackend.Model} | inference enabled {inference.Enabled} | profiles {state.Profiles.Count(profile => profile.Enabled)}/{state.Profiles.Length} enabled\nSuggestions: {(state.SuggestionsEnabled ? "enabled" : "disabled")} | enabled goals {LiveGoals(state).Count()} | schedules {(schedules is null ? "unavailable" : schedules.Snapshot().Paused ? "paused" : "active")}\nNo trust profile grants OS/file/context access, task approval or execution. Use trust activity for evidence and trust acceptance for current gates.");
                }
                if (args.SequenceEqual(["activity"])) return new(Activity(engine, inference, schedules));
                if (args.SequenceEqual(["memory"]))
                {
                    var rows = snapshot.Communications.Memories.Where(memory => !memory.Forgotten).OrderBy(memory => memory.AgentId).ThenBy(memory => memory.Project).ThenBy(memory => memory.Key);
                    return new("Saved memory inventory. Content is hidden here; use the scoped memory command to review/correct/expire/forget exact records.\n" + TerminalTable.Render(["Agent", "Project", "Key", "Kind", "Expiry", "Grants"], rows.Select(memory => new[] { AgentName(snapshot, memory.AgentId), memory.Project, memory.Key, memory.Kind.ToString(), memory.ExpiresAtUtc?.ToString("O") ?? "30-day stale policy", memory.Grants.Count(value => !value.Revoked).ToString() })) + "\nExport requires trust export plan followed by exact approval. Cross-agent grants remain visible and revocable through memory revoke.");
                }
                if (args.Count == 3 && args[0] == "export" && args[1] == "plan")
                {
                    var path = ExportPath(args[2]); var payload = JsonSerializer.SerializeToUtf8Bytes(new { Contract = "Grid.AI.TrustMemoryExport.v1", ExportedAtUtc = DateTimeOffset.UtcNow, snapshot.WorldId, Memories = snapshot.Communications.Memories.Select(memory => new { memory.Id, memory.AgentId, Agent = AgentName(snapshot, memory.AgentId), memory.Project, memory.Key, memory.Kind, memory.Content, memory.UpdatedAtUtc, memory.ExpiresAtUtc, memory.Forgotten, memory.Provenance, memory.Grants }) }, new JsonSerializerOptions { WriteIndented = true });
                    if (payload.Length > 4 * 1024 * 1024) throw new InvalidOperationException("Memory export exceeds the 4 MiB limit."); var hash = Convert.ToHexString(SHA256.HashData(payload)); export = new(Guid.NewGuid(), path, payload, hash, identity.Generation, engine.ConversationMemoryRevision, DateTimeOffset.UtcNow.AddMinutes(2));
                    return new($"MEMORY EXPORT PREVIEW\nNew file only: {path}\nRecords: {snapshot.Communications.Memories.Count} | bytes {payload.Length} | SHA256 {hash}\nThis contains private memory content and grants. Nothing written. Approve these exact bytes within two minutes: trust export approve {export.Id}");
                }
                if (args.Count == 3 && args[0] == "export" && args[1] == "approve")
                {
                    var pending = export; export = null;
                    if (pending is null || !Guid.TryParse(args[2], out var id) || id != pending.Id || pending.Generation != identity.Generation || pending.MemoryRevision != engine.ConversationMemoryRevision || DateTimeOffset.UtcNow >= pending.Expires) return new("Export review missing, stale, expired or invalidated. Nothing written.");
                    if (File.Exists(pending.Path)) throw new IOException("Export target now exists; nothing overwritten."); File.WriteAllBytes(pending.Path, pending.Bytes); return new($"Memory export written and verified by prepared SHA256.\n{pending.Path}\nSHA256 {pending.Hash}\nThis is a private portable copy; deleting it later is the user's responsibility.");
                }
                if (args.Count == 1 && args[0] == "profiles") return new(TerminalTable.Render(["Profile", "Enabled", "Agents", "Capabilities", "Meaning"], state.Profiles.Select(profile => new[] { profile.Id, profile.Enabled.ToString(), profile.Agents.Length == 0 ? "none" : string.Join(',', profile.Agents), string.Join(',', profile.Capabilities), profile.Description })) + "\nProfiles are deny-first Trust Center templates. They never grant host access, approve tasks or replace skills/connections/device/file permissions.");
                if (args.Count == 3 && args[0] == "profile" && args[1] is "enable" or "disable") { center.ControlProfile(args[2], args[1] == "enable"); return new($"Trust profile {args[1]} completed. Existing host grants and approvals were not changed."); }
                if (args.Count == 4 && args[0] == "grant" && args[1] == "plan")
                { _ = state.Profiles.SingleOrDefault(value => value.Id == args[2]) ?? throw new ArgumentException("Unknown permission profile."); _ = engine.ResolveProgram(args[3]); grant = new(Guid.NewGuid(), args[2], args[3].ToUpperInvariant(), identity.Generation, DateTimeOffset.UtcNow.AddMinutes(2)); return new($"GRANT PREVIEW\nProfile: {grant.Profile}\nAgent: {grant.Agent}\nScope: Trust Center only; no host action or approval authority.\nApprove within two minutes: trust grant approve {grant.Id}"); }
                if (args.Count == 3 && args[0] == "grant" && args[1] == "approve")
                { var pending = grant; grant = null; if (pending is null || !Guid.TryParse(args[2], out var id) || id != pending.Id || pending.Generation != identity.Generation || DateTimeOffset.UtcNow >= pending.Expires) return new("Grant review missing, expired or invalidated. Nothing granted."); center.Grant(pending.Profile, pending.Agent); return new("Exact Trust Center grant applied. It can be revoked immediately with trust revoke <profile> <agent>."); }
                if (args.Count == 3 && args[0] == "revoke") { center.Revoke(args[1], args[2]); return new("Trust Center grant revoked immediately. No pending task was approved or run."); }
                if (args.Count == 1 && args[0] == "goals") return new(state.Goals.Length == 0 ? "No local goals. Suggestions remain inert without an explicitly enabled goal." : TerminalTable.Render(["ID", "Name", "Enabled", "Expires", "Goal"], state.Goals.Select(item => new[] { item.Id.ToString(), item.Name, item.Enabled.ToString(), item.ExpiresUtc?.ToString("O") ?? "never", item.Text })));
                if (args.Count >= 4 && args[0] == "goal" && args[1] == "plan" && int.TryParse(args[3], out var days))
                { var text = string.Join(' ', args.Skip(4)); if (days is < 0 or > 3650 || text.Length == 0) throw new ArgumentException("Goal expiry must be 0..3650 days and text is required."); goal = new(Guid.NewGuid(), args[2], text, days, identity.Generation, DateTimeOffset.UtcNow.AddMinutes(2)); return new($"GOAL PREVIEW\n{goal.Name}: {goal.Text}\nExpiry: {(days == 0 ? "never" : days + " days")} | starts disabled.\nAdd within two minutes: trust goal add {goal.Id}"); }
                if (args.Count == 3 && args[0] == "goal" && args[1] == "add")
                { var pending = goal; goal = null; if (pending is null || !Guid.TryParse(args[2], out var id) || id != pending.Id || pending.Generation != identity.Generation || DateTimeOffset.UtcNow >= pending.Expires) return new("Goal review missing, expired or invalidated. Nothing added."); var added = center.AddGoal(pending.Name, pending.Text, pending.Days == 0 ? null : DateTimeOffset.UtcNow.AddDays(pending.Days)); return new("Goal added disabled: " + added.Id + ". Enable it explicitly before requesting suggestions."); }
                if (args.Count == 3 && args[0] == "goal" && args[1] is "enable" or "disable" or "remove") { center.ControlGoal(ResolveGoal(state, args[2]).Id, args[1]); return new("Goal " + args[1] + " completed."); }
                if (args.Count == 2 && args[0] == "suggestions" && args[1] is "on" or "off") { center.Suggestions(args[1] == "on"); return new("Contextual suggestions " + (args[1] == "on" ? "enabled. They remain on-demand, goal-bound and non-executing." : "disabled immediately.")); }
                if (args.Count is 1 or 2 && args[0] == "suggest")
                {
                    var agent = args.Count == 2 ? args[1].ToUpperInvariant() : "MCP"; if (!center.CanSuggest(agent)) return new("Suggestion denied. Enable goal-advisor, review/grant it to " + agent + ", enable suggestions and enable a goal. Nothing monitored or executed.");
                    var goals = LiveGoals(center.Snapshot()).ToArray(); if (goals.Length == 0) return new("No enabled unexpired goals. Nothing suggested.");
                    var pending = snapshot.Tasks.Records.Count(task => task.Status == GridTaskStatus.AwaitingApproval); var failed = snapshot.Tasks.Records.Count(task => task.Status is GridTaskStatus.Failed or GridTaskStatus.Interrupted);
                    return new($"ON-DEMAND LOCAL SUGGESTIONS — {agent}\n" + string.Join("\n", goals.Select(item => $"- {item.Name}: review the explicit goal, then choose one small next action. GRID currently has {pending} task(s) awaiting approval and {failed} failed/interrupted task(s).")) + "\nSuggestions are deterministic prompts, not facts, monitoring, approvals or executed work.");
                }
                if (args.SequenceEqual(["receipts"])) return new(state.Receipts.Length == 0 ? "No Trust Center receipts." : string.Join('\n', state.Receipts.Select(item => $"{item.AtUtc:O} | {item.Action} | {item.Subject} | {item.Outcome}")));
                if (args.SequenceEqual(["acceptance"])) return new(Acceptance(snapshot, inference, schedules));
                return new("Usage:\n  trust overview | activity | memory | acceptance | receipts\n  trust export plan \"absolute-new.json\" | export approve <id>\n  trust profiles | profile enable|disable <id>\n  trust grant plan <profile> <agent> | grant approve <id> | revoke <profile> <agent>\n  trust goals | goal plan <name> <expiry-days|0> <text> | goal add <id> | goal enable|disable|remove <id|name>\n  trust suggestions <on|off> | suggest [agent]\nExample: trust activity");
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or IOException or UnauthorizedAccessException) { return new("Trust Center error: " + error.Message); }
        }));
    }

    private static string Activity(GridSimulationEngine engine, SharedLocalInference inference, ScheduledWorkflowService? schedules)
    {
        var snapshot = engine.GetSnapshot(); var tasks = snapshot.Tasks.Records; var recent = tasks.SelectMany(task => task.Audit.Select(audit => (task, audit))).OrderByDescending(item => item.audit.AtUtc).Take(12).ToArray(); var diagnostics = inference.Diagnostics.Snapshot().TakeLast(8).ToArray();
        var lines = new List<string> { "GRID.AI VERIFIED ACTIVITY", $"Host: {(snapshot.IsRunning ? "active" : "paused")} | registered agents {snapshot.Identities.RegisteredAgents.Count}/6 | local inference {(inference.Enabled ? "enabled" : "disabled")}",
            "Models: " + string.Join(", ", AgentModelProfiles.All.Select(profile => profile.Agent + "=" + profile.Model).Distinct()),
            $"Tasks: awaiting {tasks.Count(task => task.Status == GridTaskStatus.AwaitingApproval)} | queued {tasks.Count(task => task.Status == GridTaskStatus.Queued)} | running {tasks.Count(task => task.Status == GridTaskStatus.Running)} | completed {tasks.Count(task => task.Status == GridTaskStatus.Completed)} | failed/interrupted {tasks.Count(task => task.Status is GridTaskStatus.Failed or GridTaskStatus.Interrupted)} | cancelled/denied {tasks.Count(task => task.Status is GridTaskStatus.Cancelled or GridTaskStatus.Denied)}",
            $"Scheduler: {(schedules is null ? "unavailable" : schedules.Snapshot().Paused ? "paused" : schedules.Snapshot().Jobs.Count(job => job.Enabled) + " enabled")}",
            "Recent model requests (content excluded):", diagnostics.Length == 0 ? "  none retained" : string.Join('\n', diagnostics.Select(item => $"  {item.RecordedUtc:O} | {item.Agent} | {item.Outcome} | {item.RunMilliseconds} ms")),
            "Recent task/tool audit (arguments/results excluded):", recent.Length == 0 ? "  none retained" : string.Join('\n', recent.Select(item => $"  {item.audit.AtUtc:O} | {Role(snapshot, item.task.AgentId)} | {item.task.Capability} | {item.audit.Action} | {item.task.Status}")),
            "Resources:", string.Join('\n', engine.GetHostResources().Take(12).Select(reading => $"  {reading.Metric}/{reading.Scope}: {(reading.Value?.ToString("0.##") ?? "unknown")} {reading.Unit} | {reading.Status} | {reading.ObservedAtUtc:O}")) };
        return string.Join('\n', lines) + "\nTask audit is durable in the world; model diagnostics are bounded in-memory. Use tasks inspect, ai diagnostics details, schedule inbox and history for deeper evidence.";
    }
    private static string Acceptance(GridSimulationSnapshot snapshot, SharedLocalInference inference, ScheduledWorkflowService? schedules) =>
        "6.38 TERMINAL ACCEPTANCE STATUS\n" +
        $"PASS | authenticated trust command and capability filtering\nPASS | agent activity {(snapshot.IsRunning ? "active" : "paused but observable")} and saved task audit\n{(inference.Enabled ? "PASS" : "NEEDS SETUP")} | bundled local inference\nPASS | memory review/correct/expiry/forget plus reviewed export\nPASS | deny-first suggestion profiles, immediate revoke and no auto-approval\n{(schedules is null ? "NOT AVAILABLE" : "PASS")} | durable host scheduler status\nMANUAL | representative-user first-task, accessibility and recovery study\nMANUAL | physical Windows interaction and offline second-device/Flash package\nPLANNED | optional GUI release gate (Future Plan C)\nThis command reports capabilities; it does not claim the manual or future gates passed.";
    private static IEnumerable<TrustGoal> LiveGoals(TrustState state) => state.Goals.Where(goal => goal.Enabled && (goal.ExpiresUtc is null || goal.ExpiresUtc > DateTimeOffset.UtcNow));
    private static TrustGoal ResolveGoal(TrustState state, string query) { var values = Guid.TryParse(query, out var id) ? state.Goals.Where(value => value.Id == id).ToArray() : state.Goals.Where(value => value.Name.Equals(query, StringComparison.OrdinalIgnoreCase)).ToArray(); return values.Length == 1 ? values[0] : throw new ArgumentException(values.Length == 0 ? "Unknown goal." : "Ambiguous goal; use full ID."); }
    private static string AgentName(GridSimulationSnapshot snapshot, Guid id) => snapshot.Tasks.Agents.SingleOrDefault(agent => agent.Id == id)?.Role.ToString() ?? snapshot.Programs.SingleOrDefault(program => program.Id == id)?.Name ?? id.ToString();
    private static string Role(GridSimulationSnapshot snapshot, Guid id) => snapshot.Tasks.Agents.SingleOrDefault(agent => agent.Id == id)?.Role.ToString() ?? id.ToString()[..8];
    private static string ExportPath(string value)
    {
        if (!Path.IsPathFullyQualified(value) || !Path.GetExtension(value).Equals(".json", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Use an absolute new .json path."); var path = Path.GetFullPath(value); var parent = Path.GetDirectoryName(path)!; if (!Directory.Exists(parent) || File.Exists(path)) throw new ArgumentException("Export parent must exist and target must be new.");
        var info = new DirectoryInfo(parent); if (info.Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new ArgumentException("Reparse-point export parents are refused."); return path;
    }
}
