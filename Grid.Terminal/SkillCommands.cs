using Grid.Simulation;

namespace Grid.Terminal;

public static class SkillCommands
{
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession identity,
        LocalSkillCatalog? catalog, Func<CancellationToken, Task>? save = null)
    {
        commands.Register(new("skills", "skills <catalog|show|import|update|install|check|enable|disable|revoke|configure|invoke|uninstall|rollback> ...",
            "Discover and manage reviewed local declarative skills. Skill invocation only proposes existing GRID tasks.", args =>
        {
            if (!identity.IsMaster || identity.IsAttachment) return new("Skills require the original Master Control terminal.");
            if (catalog is null) return new("Persistent local skill storage is unavailable in this session.");
            try
            {
                engine.AuthorizeSkillAdministration(identity.Reference);
                if (args.Count == 0 || args.Count == 1 && args[0] == "catalog")
                {
                    var skills = catalog.List();
                    return new(skills.Count == 0 ? "No local skills installed. Use skills import <absolute-manifest.json>." :
                        TerminalTable.Render(["Skill", "Version", "State", "Availability", "Permissions"], skills.Select(s => new[] {
                            s.Manifest.Id, s.Manifest.SkillVersion, s.Enabled ? "Enabled" : "Disabled",
                            s.Manifest.NetworkRequired ? "Optional network; unavailable offline" : "Offline capable", string.Join(", ", s.Manifest.Permissions) })));
                }
                if (args.Count == 2 && args[0] == "show") return new(LocalSkillCatalog.Describe(catalog.Get(args[1])));
                if (args.Count == 2 && args[0] is "import" or "update")
                {
                    var review = catalog.Review(args[1], DateTimeOffset.UtcNow);
                    engine.AuditSkillLifecycle(identity.Reference, "Imported", review.Manifest.Id, review.Hash);
                    return new("SKILL IMPORT REVIEW " + review.Id + "\n" + LocalSkillCatalog.Describe(new(review.Manifest, review.Hash, false, new Dictionary<string, string>())) +
                        "\nSource: " + TerminalTable.Safe(review.Source) + "\nExpires: " + review.ExpiresUtc.ToString("O") +
                        "\nNo installation or execution occurred. Inspect origin, license, hash, permissions and steps. To install this exact file: skills install " + review.Id);
                }
                if (args.Count == 2 && args[0] == "install" && Guid.TryParse(args[1], out var reviewId))
                {
                    var updating = catalog.IsUpdate(reviewId);
                    var installed = catalog.Install(reviewId, DateTimeOffset.UtcNow);
                    engine.AuditSkillLifecycle(identity.Reference, updating ? "Updated" : "Installed", installed.Manifest.Id, installed.Hash);
                    return new((updating ? "Updated" : "Installed") + " disabled; no task executed.\n" + LocalSkillCatalog.Describe(installed) + "\nConfigure required values, then enable explicitly.");
                }
                if (args.Count == 2 && args[0] is "enable" or "disable" or "revoke")
                {
                    var skill = catalog.SetEnabled(args[1], args[0] == "enable");
                    engine.AuditSkillLifecycle(identity.Reference, args[0] == "enable" ? "Enabled" : args[0] == "revoke" ? "Revoked" : "Disabled", skill.Manifest.Id, skill.Hash);
                    return new((skill.Enabled ? "Enabled" : "Disabled/revoked") + " " + skill.Manifest.Id + ". Existing task approvals are separate and are not created or restored.");
                }
                if (args.Count == 2 && args[0] == "check")
                {
                    var skill = catalog.Get(args[1]); var request = catalog.Check(args[1], engine.TasksFor(identity.Reference));
                    return new("Dependencies available for " + skill.Manifest.Id + ". No task created.\nResolved typed steps: " + TerminalTable.Safe(request));
                }
                if (args.Count == 4 && args[0] == "configure")
                {
                    var skill = catalog.Configure(args[1], args[2], args[3]);
                    engine.AuditSkillLifecycle(identity.Reference, "Configured", skill.Manifest.Id, skill.Hash);
                    return new("Configuration saved; skill disabled for fresh review. Value is not displayed.\n" + LocalSkillCatalog.Describe(skill));
                }
                if (args.Count == 2 && args[0] == "invoke")
                {
                    var skill = catalog.Get(args[1]); var request = catalog.Invocation(args[1], engine.TasksFor(identity.Reference));
                    var key = "skill-" + skill.Manifest.Id + "-" + Guid.NewGuid().ToString("N")[..12];
                    var plan = engine.Coordinate(identity.Reference, request, key);
                    engine.AuditSkillLifecycle(identity.Reference, "Invoked", skill.Manifest.Id, skill.Hash);
                    save?.Invoke(CancellationToken.None).GetAwaiter().GetResult();
                    return new("Skill proposed plan " + plan.Id + ". No task approved or run.\nManifest SHA256: " + skill.Hash +
                        "\nPermissions used: " + string.Join(", ", skill.Manifest.Permissions) + "\nReview with mcp status " + plan.Id + " or tasks list; approve each exact task separately, then run explicitly.");
                }
                if (args.Count == 2 && args[0] == "uninstall")
                {
                    var skill = catalog.Get(args[1]); catalog.Uninstall(args[1]); engine.AuditSkillLifecycle(identity.Reference, "Uninstalled", skill.Manifest.Id, skill.Hash);
                    return new("Uninstalled active skill files after revocation; retained revision metadata remains available for explicit rollback. Existing task records remain auditable and are not replayed.");
                }
                if (args.Count == 2 && args[0] == "rollback")
                {
                    var skill = catalog.Rollback(args[1]); engine.AuditSkillLifecycle(identity.Reference, "RolledBack", skill.Manifest.Id, skill.Hash);
                    return new("Restored prior revision disabled. Review configuration and enable explicitly.\n" + LocalSkillCatalog.Describe(skill));
                }
                return new("Use skills catalog; show <id>; import/update <absolute.json>; install <review-id>; configure <id> <key> <value>; check <id>; enable/disable/revoke <id>; invoke <id>; uninstall <id>; rollback <id>.");
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or IOException or UnauthorizedAccessException or System.Text.Json.JsonException or System.Data.Common.DbException)
            { return new("Skill request refused: " + TerminalTable.Safe(error.Message) + " No unreviewed skill action executed."); }
        }) { Capability = "world.admin" });
    }
}
