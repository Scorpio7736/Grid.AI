using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grid.Simulation;

namespace Grid.Terminal;

public static class SkillCommands
{
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession identity, string catalogPath)
    {
        (Guid Id, string Source, string Hash, long Generation, DateTimeOffset Expires)? pending = null;
        identity.Changed += () => pending = null;
        commands.Register(new("skills", "skills <list|show|dependencies|review|install|enable|disable|configure|grant|revoke|rollback|remove|run|history> ...",
            "Reviewed local declarative skills with typed capabilities, dependency checks and explicit agent grants; no scripts.", args =>
        {
            if (!identity.IsAuthenticated || identity.IsAttachment) return new("Skills require original Master Control or an explicitly granted registered agent in the original terminal.");
            try
            {
                var snapshot = engine.GetSnapshot();
                var binding = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
                    Machine = Environment.MachineName, Path = Path.GetFullPath(catalogPath), snapshot.WorldId,
                    snapshot.Identities.OwnerSubject, snapshot.Identities.Credential }))));
                var catalog = new LocalSkills(catalogPath, binding);
                var actor = identity.IsMaster ? "MASTER" : engine.TasksFor(identity.Reference).Agents
                    .SingleOrDefault(agent => agent.Id == identity.CurrentRecipient.Id)?.Role.ToString()
                    ?? throw new UnauthorizedAccessException("The active identity is not a registered system-agent role.");
                var master = actor == "MASTER";
                if (!master && !snapshot.IsRunning)
                    throw new UnauthorizedAccessException("GRID agents are paused. Master Control must use grid -start before an agent can use a granted skill.");
                bool Visible(LocalSkillEntry entry) => master || entry.Enabled && entry.GrantedAgents.Contains(actor, StringComparer.Ordinal);
                if (!master && (args.Count == 0 || args[0] is not ("list" or "show" or "dependencies" or "run")))
                    throw new UnauthorizedAccessException("This skill operation requires original Master Control.");
                if (args.Count == 0 || args.SequenceEqual(new[] { "list" }))
                    return new("Local typed skills (world.status, resources.status); no model prompts, scripts, general files, processes or direct network authority.\n" +
                        string.Join("\n", catalog.Read().Skills.Where(Visible).Select(s => s.Manifest.Id + " | " + s.Manifest.Revision + " | " +
                            (s.Enabled ? "enabled" : "disabled") + " | agents: " + (s.GrantedAgents.Length == 0 ? "none" : string.Join(',', s.GrantedAgents)))));
                if (args.SequenceEqual(new[] { "history" }))
                {
                    if (!master) throw new UnauthorizedAccessException("Skill history requires original Master Control.");
                    return new("Last 100 content-free receipts. A started attempt without completion is uncertain and never replayed.\n" +
                        string.Join("\n", catalog.Read().Receipts.Select(r => $"{r.AtUtc:O} | {r.Skill} | {r.Action} | {r.Outcome}")));
                }
                if (args.Count == 2 && args[0] == "review")
                {
                    if (!master) throw new UnauthorizedAccessException("Skill review requires original Master Control.");
                    var (manifest, hash) = LocalSkills.Inspect(args[1]);
                    var id = Guid.NewGuid(); pending = (id, args[1], hash, identity.Generation, DateTimeOffset.UtcNow.AddMinutes(2));
                    return new(Describe(manifest) + "\nSource SHA256: " + hash + "\nImport/update replaces the selected skill, retains one rollback revision, and disables it.\n" +
                        "To install these exact bytes within two minutes: skills install " + id);
                }
                if (args.Count == 2 && args[0] == "install")
                {
                    if (!master) throw new UnauthorizedAccessException("Skill installation requires original Master Control.");
                    var review = pending; pending = null;
                    if (review is not { } plan || !Guid.TryParse(args[1], out var id) || id != plan.Id || plan.Generation != identity.Generation || DateTimeOffset.UtcNow >= plan.Expires)
                        return new("Review missing, expired or invalidated. Nothing installed.");
                    catalog.Install(plan.Source, plan.Hash); return new("Installed disabled. Use skills show <id>, then enable with its exact catalog hash.");
                }
                if (args.Count == 2 && args[0] == "show")
                {
                    var entry = catalog.Read().Skills.SingleOrDefault(s => s.Manifest.Id == args[1]) ?? throw new ArgumentException("Unknown skill.");
                    if (!Visible(entry)) throw new UnauthorizedAccessException("This skill is not granted to the active agent.");
                    return new(Describe(entry.Manifest) + "\nEnabled: " + entry.Enabled + "\nCanonical catalog SHA256: " + entry.Hash +
                        "\nOutput limit: " + entry.OutputLimit + "\nGranted agents: " + (entry.GrantedAgents.Length == 0 ? "none" : string.Join(", ", entry.GrantedAgents)) +
                        (master ? "\nEnable this revision: skills enable " + entry.Manifest.Id + " " + entry.Hash : ""));
                }
                if (args.Count == 2 && args[0] == "dependencies")
                {
                    var entry = catalog.Read().Skills.SingleOrDefault(s => s.Manifest.Id == args[1]) ?? throw new ArgumentException("Unknown skill.");
                    if (!Visible(entry)) throw new UnauthorizedAccessException("This skill is not granted to the active agent.");
                    return new(string.Join("\n", LocalSkills.Dependencies(entry.Manifest).Select(dependency =>
                        $"{dependency.Id} | {(dependency.Available ? "available" : "unavailable")} | {dependency.Detail}")));
                }
                if (args.Count == 3 && args[0] == "enable")
                {
                    if (!master) throw new UnauthorizedAccessException("Skill enablement requires original Master Control.");
                    var entry = catalog.Read().Skills.SingleOrDefault(s => s.Manifest.Id == args[1]);
                    if (entry is null || entry.Hash != args[2]) throw new ArgumentException("Review the exact current revision with skills show first.");
                    catalog.Control(args[1], "enable"); return new("Enabled for manual read-only execution in this original host. No background work started.");
                }
                if (args.Count == 2 && args[0] is "disable" or "rollback" or "remove")
                {
                    if (!master) throw new UnauthorizedAccessException("Skill lifecycle changes require original Master Control.");
                    catalog.Control(args[1], args[0]); return new("Skill " + args[0] + " completed. Grants are revoked when disabled; rollbacks remain disabled; receipts are retained.");
                }
                if (args.Count == 5 && args[0] == "configure")
                {
                    if (!master) throw new UnauthorizedAccessException("Skill configuration requires original Master Control.");
                    var entry = catalog.Read().Skills.SingleOrDefault(s => s.Manifest.Id == args[1]);
                    if (entry is null || entry.Hash != args[4]) throw new ArgumentException("Review the exact current revision with skills show first.");
                    catalog.Configure(args[1], args[2], args[3]); return new("Skill configuration updated for this revision. No skill was run.");
                }
                if (args.Count == 4 && args[0] == "grant")
                {
                    if (!master) throw new UnauthorizedAccessException("Skill grants require original Master Control.");
                    var entry = catalog.Read().Skills.SingleOrDefault(s => s.Manifest.Id == args[1]);
                    if (entry is null || entry.Hash != args[3]) throw new ArgumentException("Review the exact current revision with skills show first.");
                    catalog.Grant(args[1], args[2], true); return new("Skill granted to " + args[2].ToUpperInvariant() + " for this exact enabled revision on this host.");
                }
                if (args.Count == 3 && args[0] == "revoke")
                {
                    if (!master) throw new UnauthorizedAccessException("Skill revocation requires original Master Control.");
                    catalog.Grant(args[1], args[2], false); return new("Skill grant revoked from " + args[2].ToUpperInvariant() + ".");
                }
                if (args.Count == 2 && args[0] == "run")
                    return new(catalog.Run(args[1], actor, invocation => invocation.Action switch {
                        "world.status" => WorldStatus(engine),
                        "resources.status" => ResourcesCommand.Execute(engine, ["details"]).Text,
                        _ => throw new UnauthorizedAccessException("Unsupported skill action.") }));
                return new("Use skills list/history; skills review <absolute-json-path>; skills install <review-id>; skills show/dependencies/run <id>; skills enable <id> <hash>; skills configure <id> output-limit <512..8000> <hash>; skills grant <id> <agent> <hash>; skills revoke <id> <agent>; skills disable/rollback/remove <id>.");
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or JsonException)
            { return new("Skill request refused: " + TerminalTable.Safe(error.Message)); }
        }) { Capability = "session" });
    }
    private static string WorldStatus(GridSimulationEngine engine)
    {
        var state = engine.GetSnapshot();
        return $"State: {(state.IsRunning ? "Running" : "Paused")}{Environment.NewLine}" +
            $"Tick: {state.TickCount} | Simulation time: {state.ElapsedSimulationTime:c} | Tick duration: {state.TickDuration:c}{Environment.NewLine}" +
            $"Districts: {state.Districts.Count} | Programs: {state.Programs.Count}{Environment.NewLine}Application discovery: {state.ApplicationDiscoveryStatus}";
    }
    private static string Describe(LocalSkillManifest m) => $"{m.Id} revision {m.Revision} | manifest v{m.Version}\nOrigin (unverified publisher claim): {m.Origin}\nLicense: {m.License}\nDescription (untrusted data, never a prompt): {m.Description}\nTyped actions: {string.Join(", ", m.Steps)}\nDeclared dependencies: {string.Join(", ", LocalSkills.Dependencies(m).Select(d => d.Id))}\nNetwork: {(m.RequiresNetwork ? "required; unavailable in offline interpreter" : "not required")}\nEligible agent roles: {(m.AllowedAgents.Length == 0 ? "none" : string.Join(", ", m.AllowedAgents))}\nNo scripts, model instructions, arbitrary arguments, general filesystem or process authority.";
}
