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
        commands.Register(new("skills", "skills <list|show|review|install|enable|disable|rollback|remove|run|history> ...",
            "Reviewed offline JSON skills. Original Master Control only; fixed read-only status actions, no scripts or AI.", args =>
        {
            if (!identity.IsMaster || identity.IsAttachment) return new("Skills require the original Master Control terminal.");
            try
            {
                var snapshot = engine.GetSnapshot();
                var binding = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
                    Machine = Environment.MachineName, Path = Path.GetFullPath(catalogPath), snapshot.WorldId,
                    snapshot.Identities.OwnerSubject, snapshot.Identities.Credential }))));
                var catalog = new LocalSkills(catalogPath, binding);
                if (args.Count == 0 || args.SequenceEqual(new[] { "list" }))
                    return new("Local read-only skills (world.status, resources.status); no network, model, file content or process authority.\n" +
                        string.Join("\n", catalog.Read().Skills.Select(s => s.Manifest.Id + " | " + s.Manifest.Revision + " | " + (s.Enabled ? "enabled" : "disabled"))));
                if (args.SequenceEqual(new[] { "history" }))
                    return new("Last 100 content-free receipts. A started attempt without completion is uncertain and never replayed.\n" +
                        string.Join("\n", catalog.Read().Receipts.Select(r => $"{r.AtUtc:O} | {r.Skill} | {r.Action} | {r.Outcome}")));
                if (args.Count == 2 && args[0] == "review")
                {
                    var (manifest, hash) = LocalSkills.Inspect(args[1]);
                    var id = Guid.NewGuid(); pending = (id, args[1], hash, identity.Generation, DateTimeOffset.UtcNow.AddMinutes(2));
                    return new(Describe(manifest) + "\nSource SHA256: " + hash + "\nImport/update replaces the selected skill, retains one rollback revision, and disables it.\n" +
                        "To install these exact bytes within two minutes: skills install " + id);
                }
                if (args.Count == 2 && args[0] == "install")
                {
                    var review = pending; pending = null;
                    if (review is not { } plan || !Guid.TryParse(args[1], out var id) || id != plan.Id || plan.Generation != identity.Generation || DateTimeOffset.UtcNow >= plan.Expires)
                        return new("Review missing, expired or invalidated. Nothing installed.");
                    catalog.Install(plan.Source, plan.Hash); return new("Installed disabled. Use skills show <id>, then enable with its exact catalog hash.");
                }
                if (args.Count == 2 && args[0] == "show")
                {
                    var entry = catalog.Read().Skills.SingleOrDefault(s => s.Manifest.Id == args[1]) ?? throw new ArgumentException("Unknown skill.");
                    return new(Describe(entry.Manifest) + "\nEnabled: " + entry.Enabled + "\nCanonical catalog SHA256: " + entry.Hash +
                        "\nEnable this revision: skills enable " + entry.Manifest.Id + " " + entry.Hash);
                }
                if (args.Count == 3 && args[0] == "enable")
                {
                    var entry = catalog.Read().Skills.SingleOrDefault(s => s.Manifest.Id == args[1]);
                    if (entry is null || entry.Hash != args[2]) throw new ArgumentException("Review the exact current revision with skills show first.");
                    catalog.Control(args[1], "enable"); return new("Enabled for manual read-only execution in this original host. No background work started.");
                }
                if (args.Count == 2 && args[0] is "disable" or "rollback" or "remove")
                { catalog.Control(args[1], args[0]); return new("Skill " + args[0] + " completed. Rollbacks remain disabled; receipts are retained."); }
                if (args.Count == 2 && args[0] == "run")
                    return new(catalog.Run(args[1], step => step switch {
                        "world.status" => commands.ExecuteTokens(["status"]).Text,
                        "resources.status" => commands.ExecuteTokens(["resources", "details"]).Text,
                        _ => throw new UnauthorizedAccessException("Unsupported skill action.") }));
                return new("Use skills list/history; skills review <absolute-json-path>; skills install <review-id>; skills show <id>; skills enable <id> <hash>; skills disable/rollback/remove/run <id>.");
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or JsonException)
            { return new("Skill request refused: " + TerminalTable.Safe(error.Message)); }
        }));
    }
    private static string Describe(LocalSkillManifest m) => $"{m.Id} revision {m.Revision}\nOrigin (unverified publisher claim): {m.Origin}\nLicense: {m.License}\nDescription (untrusted data): {m.Description}\nRead-only steps: {string.Join(", ", m.Steps)}\nOffline; no external dependencies. Scope: original owner's world/resource status only.";
}
