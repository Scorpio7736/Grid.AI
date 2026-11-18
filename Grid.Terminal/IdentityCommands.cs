using Grid.Simulation;
using Grid.Simulation.Editing;
namespace Grid.Terminal;

public static class IdentityCommands
{
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession session, bool interactiveUi = false)
    {
        ProgramEditPreview? preview = null; session.Changed += () => preview = null;
        commands.Register(new("login", "login [master]", "Authenticate the world owner using the current Windows identity.", a =>
            a.Count > 1 || a.Count == 1 && !a[0].Equals("master", StringComparison.OrdinalIgnoreCase) ? new("Usage: login [master]. Never put credentials in command arguments.") : interactiveUi ? new("", LoginRequested: true) : new(session.Login())) { Capability = "public" });
        commands.Register(new("logout", "logout", "Revoke the session and clear active context.", a => a.Count != 0 ? new("Usage: logout") : interactiveUi ? new("", LogoutRequested: true) : new(session.Logout())) { Capability = "public" });
        commands.Register(new("credentials", "credentials", "Master Control: interactively set or rotate local username, password, and three challenge codes.", a =>
            a.Count != 0 ? new("Usage: credentials. Secrets must never be command arguments.") : new("", SetupRequested: true)) { Capability = "programs.edit" });
        commands.Register(new("whoami", "whoami", "Show authenticated operator, active identity and capabilities.", a => new(a.Count == 0 ? session.Describe() : "Usage: whoami")) { Capability = "public" });
        commands.Register(new("switch", "switch <master|agent-id|name>", "Select Master Control or a registered agent identity.", a => new(a.Count > 0 ? session.Switch(string.Join(" ", a)) : "Usage: switch <master|agent-id|name>")) { Capability = "session" });
        commands.Register(new("register-agent", "register-agent <id|name>", "Master Control: register an existing Agent for identity switching.", a => {
            if (a.Count == 0) return new("Usage: register-agent <id|name>");
            var id = engine.ResolveProgram(string.Join(" ", a)); engine.RegisterAgentIdentity(id.Id); return new($"Registered agent identity: {id.Name} ({id.Id}).");
        }));
        commands.Register(new("self", "self", "Show the active identity and its own resident state.", a => {
            if (a.Count != 0) return new("Usage: self");
            var identity = session.CurrentRecipient; var resident = engine.GetSnapshot().Programs.SingleOrDefault(p => p.Id == identity.Id);
            return new(resident is null ? session.Describe() : $"{TerminalTable.Safe(resident.Name)} ({resident.Id}) | {resident.Kind} | district {resident.DistrictId} | {resident.Behavior?.Action}\nNo real task service is implemented.");
        }) { Capability = "self" });
        commands.Register(new("edit", "edit <program-id|quoted-name> <field> <value>", "Master Control: preview one validated program field change.", a => {
            preview = null;
            if (a.Count < 3) return new("Usage: edit <program-id|quoted-name> <field> <value>. Fields: name, occupation, energy, status, district.");
            var target = engine.ResolveProgram(a[0]); var parsed = engine.EditableFields.Parse(a[1], string.Join(" ", a.Skip(2)));
            if (!parsed.Success) return new($"Invalid edit: {parsed.Error!.Message}");
            var result = engine.PrepareEdit(session.Reference, new(target.Id, [parsed.Edit!])); preview = result.Preview;
            if (preview is null) return new(Describe(result));
            return new($"Preview {preview.RequestId} for {TerminalTable.Safe(preview.ProgramName)} revision {preview.ExpectedRevision}\n" +
                string.Join("\n", preview.Changes.Select(c => $"{c.Field}: {TerminalTable.Safe(ProgramEditFormatting.Format(c.Before))} -> {TerminalTable.Safe(ProgramEditFormatting.Format(c.After))}")) +
                $"\n{preview.Behavior}\nType apply-edit {preview.RequestId} to confirm, or cancel-edit.");
        }) { Capability = "programs.edit" });
        commands.Register(new("apply-edit", "apply-edit <preview-id>", "Master Control: commit the current preview if its revision and identity still match.", a => {
            if (a.Count != 1 || !Guid.TryParse(a[0], out var id)) return new("Usage: apply-edit <preview-id>");
            if (preview is null || preview.RequestId != id) return new("No matching preview. Run edit again.");
            var chosen = preview; preview = null; return new(Describe(engine.ApplyEdit(session.Reference, chosen)));
        }) { Capability = "programs.edit" });
        commands.Register(new("cancel-edit", "cancel-edit", "Discard the pending edit preview.", a => {
            if (a.Count != 0) return new("Usage: cancel-edit"); preview = null; return new("Edit preview cancelled.");
        }) { Capability = "programs.edit" });
    }
    private static string Describe(ProgramEditResult result) => $"Edit: {result.Outcome}" +
        (result.Receipt is { } receipt ? $"; revision {receipt.BeforeRevision} -> {receipt.Revision}. One-time assignment committed; saved on autosave/exit." : "") +
        (result.Errors is { } errors ? "\n" + string.Join("\n", errors.Select(e => e.Field + ": " + e.Message)) : "");
}
