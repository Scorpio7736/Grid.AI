using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grid.Simulation;

namespace Grid.Terminal;

public static class ConnectionCommands
{
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession identity, string catalogPath)
    {
        (Guid Id, string Alias, string Source, string Hash, long Generation, DateTimeOffset Expires)? pendingAdd = null;
        LocalConnectionDraft? pendingDraft = null;
        identity.Changed += () => { pendingAdd = null; pendingDraft = null; };
        commands.Register(new("connections", "connections <providers|list|show|status|plan|add|enable|disable|grant|revoke|remove|read|draft-add|approve|commit|history> ...",
            "Manage explicit offline local connections and disclose unavailable optional service providers.", args =>
        {
            if (!identity.IsAuthenticated || identity.IsAttachment) return new("Connections require original Master Control or a granted registered agent in the original terminal.");
            try
            {
                var snapshot = engine.GetSnapshot();
                var binding = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
                    Environment.MachineName, Path = Path.GetFullPath(catalogPath), snapshot.WorldId,
                    snapshot.Identities.OwnerSubject, snapshot.Identities.Credential }))));
                var catalog = new LocalConnections(catalogPath, binding);
                var actor = identity.IsMaster ? "MASTER" : engine.TasksFor(identity.Reference).Agents
                    .SingleOrDefault(agent => agent.Id == identity.CurrentRecipient.Id)?.Role.ToString()
                    ?? throw new UnauthorizedAccessException("Active identity is not a registered system agent.");
                var master = actor == "MASTER";
                if (!master && !snapshot.IsRunning) throw new UnauthorizedAccessException("GRID agents are paused.");
                bool Visible(LocalConnection item) => master || item.Enabled && item.ReadGrants.Contains(actor);
                if (args.SequenceEqual(["providers"])) return new(Providers());
                if (args.Count == 0 || args.SequenceEqual(["list"])) return new(string.Join("\n", catalog.Read().Connections.Where(Visible)
                    .Select(item => $"{item.Id} | {item.Kind} | {(item.Enabled ? "enabled" : "disabled")} | {(item.ReadGrants.Length == 0 ? "agents: none" : "agents: " + string.Join(',', item.ReadGrants))}")));
                if (!master && args[0] is not ("list" or "show" or "status" or "read")) throw new UnauthorizedAccessException("Connection management and writes require original Master Control.");
                if (args.Count == 2 && args[0] is "show" or "status")
                {
                    var item = catalog.Read().Connections.SingleOrDefault(value => value.Id == args[1]) ?? throw new ArgumentException("Unknown connection.");
                    if (!Visible(item)) throw new UnauthorizedAccessException("Connection is not granted to the active agent.");
                    var freshness = "unavailable"; var detail = "source cannot be read";
                    try { var inspected = LocalConnections.InspectBoard(item.Source); freshness = inspected.Hash == item.SourceHash ? "fresh/unchanged" : "fresh/changed"; detail = $"{inspected.Board.Items.Length} tasks | SHA256 {inspected.Hash}"; } catch (Exception error) when (error is IOException or InvalidDataException or JsonException) { detail = TerminalTable.Safe(error.Message); }
                    return new($"{item.Id} | {item.Kind} | {(item.Enabled ? "enabled" : "disabled")}\nScopes: {string.Join(", ", item.Scopes)}\nCredential storage: none\nAccount/source isolation: {item.Source}\nTransport: local/offline\nStatus: {freshness} | {detail}\nShared with agents: {(item.ReadGrants.Length == 0 ? "none" : string.Join(", ", item.ReadGrants))}");
                }
                if (args.Count == 2 && args[0] == "read") return new(catalog.ReadTasks(args[1], actor));
                if (!master) return new("Connection request refused.");
                if (args.Count == 4 && args[0] == "plan" && args[1] == "local-task-board")
                {
                    var inspected = LocalConnections.InspectBoard(args[3]); var id = Guid.NewGuid();
                    pendingAdd = (id, args[2], args[3], inspected.Hash, identity.Generation, DateTimeOffset.UtcNow.AddMinutes(2));
                    return new($"PLAN ONLY - no connection added.\nAlias: {args[2]}\nProvider: local-task-board (offline)\nSource: {Path.GetFullPath(args[3])}\nScopes: tasks.read, tasks.draft, tasks.commit\nCredential storage: none\nSource SHA256: {inspected.Hash}\nConsent expires in two minutes. Add exact reviewed source with: connections add {id}");
                }
                if (args.Count == 2 && args[0] == "add")
                {
                    var plan = pendingAdd; pendingAdd = null;
                    if (plan is not { } value || !Guid.TryParse(args[1], out var id) || id != value.Id || value.Generation != identity.Generation || DateTimeOffset.UtcNow >= value.Expires)
                        return new("Connection plan missing, expired or invalidated. Nothing added.");
                    catalog.Add(value.Alias, value.Source, value.Hash); return new("Connection added disabled. Review with connections show, then enable using its exact source hash.");
                }
                if (args.Count == 3 && args[0] == "enable") { catalog.Control(args[1], "enable", args[2]); return new("Connection enabled locally. No data was sent or changed."); }
                if (args.Count == 2 && args[0] is "disable" or "remove") { catalog.Control(args[1], args[0]); return new(args[0] == "remove" ? "Connection removed; source data and retained recovery artifacts were not deleted." : "Connection disabled and agent grants revoked."); }
                if (args.Count == 3 && args[0] is "grant" or "revoke") { catalog.Control(args[1], args[0], role: args[2]); return new($"Read grant {args[0]} completed for {args[2].ToUpperInvariant()}. Write authority was not granted."); }
                if (args.Count >= 3 && args[0] == "draft-add")
                {
                    pendingDraft = catalog.DraftAdd(args[1], string.Join(' ', args.Skip(2)));
                    var added = pendingDraft.After.Items.Except(pendingDraft.Before.Items).Single();
                    return new($"DRAFT ONLY - source unchanged.\nDraft: {pendingDraft.Id}\nConnection: {pendingDraft.Connection}\nAdd task: {added.Id} | {added.Title}\nSource SHA256: {pendingDraft.SourceHash}\nApprove exact preview: connections approve {pendingDraft.Id}\nThen commit separately: connections commit {pendingDraft.Id}");
                }
                if (args.Count == 2 && args[0] == "approve" && pendingDraft is { } draft && Guid.TryParse(args[1], out var approve) && approve == draft.Id)
                { pendingDraft = draft with { Approved = true }; return new("Exact draft approved in this session. Source unchanged; use connections commit " + draft.Id); }
                if (args.Count == 2 && args[0] == "commit" && pendingDraft is { } approved && Guid.TryParse(args[1], out var commit) && commit == approved.Id)
                { pendingDraft = null; catalog.Commit(approved); return new("Task addition committed to the local source. A before-image was retained in connection artifacts."); }
                if (args.SequenceEqual(["history"])) return new(string.Join("\n", catalog.Read().Receipts.Select(receipt => $"{receipt.AtUtc:O} | {receipt.Connection} | {receipt.Action} | {receipt.Outcome}")));
                return new("Use connections providers/list/show/status; plan local-task-board <alias> <absolute-json>; add/enable/disable/grant/revoke/remove/read; draft-add/approve/commit; history.");
            }
            catch (Exception error) when (error is IOException or InvalidDataException or JsonException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
            { return new("Connection request refused: " + TerminalTable.Safe(error.Message)); }
        }) { Capability = "session" });
    }
    private static string Providers() => "local-task-board | available | local/offline | scopes: tasks.read, tasks.draft, tasks.commit | credentials: none\n" +
        "calendar-service | unavailable | optional provider not installed; no account or network access\n" +
        "email-service | unavailable | optional provider not installed; no account or network access\n" +
        "external-tasks | unavailable | optional provider not installed; no account or network access\n" +
        "browser-data | unavailable | no reviewed browser adapter or profile consent; no access";
}
