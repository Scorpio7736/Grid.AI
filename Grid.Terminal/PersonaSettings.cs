using System.Text.Json;
using Grid.Agent;
using Grid.Communication;

namespace Grid.Terminal;

public sealed record PersonaSettingsState(int SchemaVersion, PersonaSelection[] Selections);
public static class PersonaSettings
{
    public static AgentPersonas Read(string root)
    {
        var catalog = new AgentPersonas(); var path = Path.Combine(root, "agent-personas.json"); GridTransfer.CheckPath(path);
        if (!File.Exists(path)) return catalog;
        if (new FileInfo(path).Length > 16384) throw new IOException("Persona settings exceed limit.");
        var settings = JsonSerializer.Deserialize<PersonaSettingsState>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty persona settings.");
        if (settings.SchemaVersion != 1 || settings.Selections is null || settings.Selections.Length > 6) throw new InvalidDataException("Unsupported persona settings.");
        catalog.Restore(settings.Selections); return catalog;
    }
    public static void Save(string root, AgentPersonas catalog)
    {
        GridTransfer.CheckPath(root); Directory.CreateDirectory(root);
        var path = Path.Combine(root, "agent-personas.json"); GridTransfer.CheckPath(path);
        var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { JsonSerializer.Serialize(file, new PersonaSettingsState(1, catalog.Export())); file.Flush(true); }
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public static CommandResult Execute(IReadOnlyList<string> args, TerminalIdentitySession identity, SharedLocalInference service, OllamaLocalBackend backend)
    {
        if (!identity.IsMaster || identity.IsAttachment) return new("Use the original host terminal as Master Control.");
        if (args.Count == 1) return new(string.Join("\n", AgentPersonas.Names.Select(a => { var p = backend.Personas.Get(a); return $"{p.Agent} | {p.Version} | {p.Identity}"; })) + "\nUsage: ai personas <agent> <v1|v2|rollback>. Bundled definitions only; no permission changes.");
        if (args.Count != 3) return new("Usage: ai personas <agent> <v1|v2|rollback>");
        if (service.Active || service.Pending > 0) return new("Wait for queued inference to finish before changing persona versions.");
        try
        {
            var next = new AgentPersonas(); next.Restore(backend.Personas.Export());
            if (args[2] == "rollback") next.Rollback(args[1]); else next.Select(args[1], args[2]);
            var root = BundledAiRuntime.FindRoot(AppContext.BaseDirectory) ?? throw new IOException("No runtime settings location.");
            Save(root, next); backend.Personas.Restore(next.Export());
            var selected = next.Get(args[1]);
            return new($"{selected.Agent} persona {selected.Version} saved. New requests use this version; existing transcripts are unchanged. Host permissions and approvals are unchanged.");
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
        { return new("Persona change refused: " + e.Message); }
    }
}
