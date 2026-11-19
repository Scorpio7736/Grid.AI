using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Grid.Simulation.Tasks;

namespace Grid.Terminal;

public sealed record LocalSkillManifest(int Version, string Id, string Revision, string Origin,
    string License, string Description, string[] Steps)
{
    public string[] AllowedAgents { get; init; } = [];
    public string[] Dependencies { get; init; } = [];
    public bool RequiresNetwork { get; init; }
}
public sealed record LocalSkillEntry(LocalSkillManifest Manifest, string Hash, bool Enabled, LocalSkillManifest? Previous = null)
{
    public string[] GrantedAgents { get; init; } = [];
    public int OutputLimit { get; init; } = 8000;
}
public sealed record LocalSkillReceipt(DateTimeOffset AtUtc, string Skill, string Action, string Outcome);
public sealed record LocalSkillState(int Version, string Binding, LocalSkillEntry[] Skills, LocalSkillReceipt[] Receipts);
public sealed record LocalSkillDependencyStatus(string Id, bool Available, string Detail);
public sealed record LocalSkillInvocation(string SkillId, string Revision, string Actor, string Action, int OutputLimit);

/// <summary>Typed declarative skills. Imported text is data; only fixed host mappings can execute.</summary>
public sealed class LocalSkills
{
    private readonly string path;
    private readonly string binding;
    private readonly object gate = new();
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 12 };
    public static IReadOnlyList<string> SupportedSteps { get; } = Array.AsReadOnly(new[] { "world.status", "resources.status" });
    public static IReadOnlyList<string> SupportedDependencies { get; } = Array.AsReadOnly(new[] { "grid.world", "grid.resources", "network" });
    public LocalSkills(string path, string binding) { this.path = LocalPath(path); this.binding = binding; }
    public LocalSkillState Read()
    {
        lock (gate)
        {
            if (!File.Exists(path)) return new(1, binding, [], []);
            var state = JsonSerializer.Deserialize<LocalSkillState>(ReadBounded(path, 1024 * 1024), Json)
                ?? throw new InvalidDataException("Invalid catalog.");
            if (state.Version != 1 || state.Skills is null || state.Skills.Length > 64 || state.Receipts is null || state.Receipts.Length > 100)
                throw new InvalidDataException("Unsupported or oversized catalog.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in state.Skills)
            {
                if (entry is null) throw new InvalidDataException("Missing skill.");
                Validate(entry.Manifest);
                if (!ids.Add(entry.Manifest.Id) || entry.Hash != Hash(entry.Manifest)) throw new InvalidDataException("Catalog integrity mismatch.");
                if (entry.Previous is { } previous) { Validate(previous); if (previous.Id != entry.Manifest.Id) throw new InvalidDataException("Rollback ID mismatch."); }
                if (entry.OutputLimit is < 512 or > 8000 || entry.GrantedAgents is null ||
                    entry.GrantedAgents.Distinct(StringComparer.Ordinal).Count() != entry.GrantedAgents.Length ||
                    entry.GrantedAgents.Any(role => !Agent(entry.Manifest, role)))
                    throw new InvalidDataException("Invalid skill configuration or agent grant.");
            }
            foreach (var receipt in state.Receipts)
                if (receipt is null || !Text(receipt.Skill,48) || !Text(receipt.Action,32) || !Text(receipt.Outcome,32))
                    throw new InvalidDataException("Invalid receipt.");
            return state.Binding == binding ? state : state with { Binding = binding,
                Skills = state.Skills.Select(s => s with { Enabled = false, GrantedAgents = [] }).ToArray() };
        }
    }
    public static (LocalSkillManifest Manifest, string SourceHash) Inspect(string source)
    {
        var bytes = ReadBounded(LocalPath(source), 16384);
        var manifest = JsonSerializer.Deserialize<LocalSkillManifest>(bytes, Json) ?? throw new InvalidDataException("Missing manifest.");
        Validate(manifest);
        return (manifest, Convert.ToHexString(SHA256.HashData(bytes)));
    }
    public void Install(string source, string reviewedHash)
    {
        lock (gate)
        {
            var inspected = Inspect(source);
            if (inspected.SourceHash != reviewedHash) throw new InvalidDataException("Manifest changed since review. Review it again.");
            var state = Read(); var old = state.Skills.SingleOrDefault(s => s.Manifest.Id == inspected.Manifest.Id);
            if (old is null && state.Skills.Length >= 64) throw new InvalidOperationException("Catalog limit: 64 skills.");
            var next = new LocalSkillEntry(inspected.Manifest, Hash(inspected.Manifest), false, old?.Manifest);
            Save(Receipt(state with { Skills = state.Skills.Where(s => s.Manifest.Id != next.Manifest.Id).Append(next).ToArray() }, next.Manifest.Id, "install", "disabled"));
        }
    }
    public void Control(string id, string action)
    {
        lock (gate)
        {
            var state = Read(); var entry = state.Skills.SingleOrDefault(s => s.Manifest.Id == id) ?? throw new ArgumentException("Unknown skill.");
            var next = action switch
            {
                "enable" when Dependencies(entry.Manifest).All(d => d.Available) => entry with { Enabled = true },
                "enable" => throw new InvalidOperationException("Required skill dependencies are unavailable."),
                "disable" => entry with { Enabled = false, GrantedAgents = [] },
                "rollback" when entry.Previous is { } previous => new(previous, Hash(previous), false, entry.Manifest),
                "remove" => entry,
                _ => throw new ArgumentException("Use enable, disable, remove, or rollback with a retained revision.")
            };
            Save(Receipt(state with { Skills = state.Skills.Where(s => s.Manifest.Id != id).Concat(action == "remove" ? [] : new[] { next }).ToArray() }, id, action, "completed"));
        }
    }
    public void Configure(string id, string key, string value)
    {
        lock (gate)
        {
            var state = Read(); var entry = state.Skills.SingleOrDefault(s => s.Manifest.Id == id) ?? throw new ArgumentException("Unknown skill.");
            if (key != "output-limit" || !int.TryParse(value, out var limit) || limit is < 512 or > 8000)
                throw new ArgumentException("Supported configuration: output-limit <512..8000>.");
            Save(Receipt(state with { Skills = state.Skills.Where(s => s.Manifest.Id != id).Append(entry with { OutputLimit = limit }).ToArray() }, id, "configure", "completed"));
        }
    }
    public void Grant(string id, string role, bool grant)
    {
        lock (gate)
        {
            role = role.ToUpperInvariant();
            var state = Read(); var entry = state.Skills.SingleOrDefault(s => s.Manifest.Id == id) ?? throw new ArgumentException("Unknown skill.");
            if (!Agent(entry.Manifest, role)) throw new UnauthorizedAccessException("That manifest does not permit this registered agent role.");
            if (!entry.Enabled || !Dependencies(entry.Manifest).All(d => d.Available))
                throw new UnauthorizedAccessException("Enable the current skill revision with available dependencies before granting it.");
            var grants = grant ? entry.GrantedAgents.Append(role).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray() :
                entry.GrantedAgents.Where(item => item != role).ToArray();
            Save(Receipt(state with { Skills = state.Skills.Where(s => s.Manifest.Id != id).Append(entry with { GrantedAgents = grants }).ToArray() }, id,
                grant ? "grant" : "revoke", "completed"));
        }
    }
    public static LocalSkillDependencyStatus[] Dependencies(LocalSkillManifest manifest)
    {
        Validate(manifest);
        var declared = manifest.Version == 1 ? RequiredForSteps(manifest.Steps) : manifest.Dependencies;
        return declared.Select(id => id switch
        {
            "grid.world" => new LocalSkillDependencyStatus(id, true, "Built-in read-only world snapshot."),
            "grid.resources" => new LocalSkillDependencyStatus(id, true, "Built-in read-only resource snapshot."),
            "network" => new LocalSkillDependencyStatus(id, false, "Optional network dependency is unavailable in the offline skill interpreter."),
            _ => throw new InvalidDataException("Unknown skill dependency.")
        }).ToArray();
    }
    public string Run(string id, Func<string, string> readStatus)
        => Run(id, "MASTER", invocation => readStatus(invocation.Action));
    public string Run(string id, string actor, Func<LocalSkillInvocation, string> invoke)
    {
        lock (gate)
        {
            var state = Read(); var entry = state.Skills.SingleOrDefault(s => s.Manifest.Id == id && s.Enabled)
                ?? throw new UnauthorizedAccessException("Skill missing or disabled. Review and enable on this device first.");
            actor = actor.ToUpperInvariant();
            if (actor != "MASTER" && (!Agent(entry.Manifest, actor) || !entry.GrantedAgents.Contains(actor, StringComparer.Ordinal)))
                throw new UnauthorizedAccessException("This exact skill revision is not granted to the active registered agent.");
            if (!Dependencies(entry.Manifest).All(d => d.Available))
                throw new InvalidOperationException("Required skill dependencies are unavailable. Nothing ran.");
            // A persisted attempt precedes execution. There is no retry, scheduler, inference or arbitrary command dispatch.
            Save(Receipt(state, id, "run", "started"));
            var output = new List<string>();
            try
            {
                foreach (var step in entry.Manifest.Steps)
                {
                    var invocation = new LocalSkillInvocation(entry.Manifest.Id, entry.Manifest.Revision, actor, step, entry.OutputLimit);
                    var result = invoke(invocation) ?? throw new InvalidDataException("Skill adapter returned no result.");
                    output.Add(step + "\n" + result[..Math.Min(result.Length, entry.OutputLimit)]);
                }
                Save(Receipt(Read(), id, "run", "completed"));
                return string.Join("\n", output);
            }
            catch { Save(Receipt(Read(), id, "run", "failed")); throw; }
        }
    }
    private static LocalSkillState Receipt(LocalSkillState state, string id, string action, string outcome) =>
        state with { Receipts = state.Receipts.Append(new(DateTimeOffset.UtcNow, id, action, outcome)).TakeLast(100).ToArray() };
    private void Save(LocalSkillState state)
    {
        LocalPath(path);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            { JsonSerializer.Serialize(file, state, Json); file.Flush(true); }
            LocalPath(path); File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static byte[] ReadBounded(string path, int max)
    {
        LocalPath(path);
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > max) throw new InvalidDataException("File exceeds size limit.");
        var bytes = new byte[(int)file.Length]; file.ReadExactly(bytes); return bytes;
    }
    private static string LocalPath(string path)
    {
        if (!Path.IsPathFullyQualified(path) || path.StartsWith(@"\\") || path.Any(char.IsControl)) throw new IOException("Choose an absolute local path.");
        path = Path.GetFullPath(path);
        if (path[Path.GetPathRoot(path)!.Length..].Contains(':')) throw new IOException("Alternate streams are unsupported.");
        GridTransfer.CheckPath(path); return path;
    }
    private static string Hash(LocalSkillManifest manifest)
    {
        object canonical = manifest.Version == 1
            ? new { manifest.Version, manifest.Id, manifest.Revision, manifest.Origin, manifest.License, manifest.Description, manifest.Steps }
            : manifest;
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(canonical, Json)));
    }
    private static bool Text(string? text, int max) => !string.IsNullOrWhiteSpace(text) && text.Length <= max && !text.Any(char.IsControl);
    private static void Validate(LocalSkillManifest manifest)
    {
        if (manifest is null || manifest.Version is not (1 or 2) || !Text(manifest.Id,48) || !Regex.IsMatch(manifest.Id, "^[a-z][a-z0-9-]*$", RegexOptions.CultureInvariant) ||
            !Text(manifest.Revision,64) || !Text(manifest.Origin,256) || !Text(manifest.License,128) || !Text(manifest.Description,512) ||
            manifest.Steps is null || manifest.Steps.Length is < 1 or > 4 || manifest.Steps.Any(s => !SupportedSteps.Contains(s)))
            throw new InvalidDataException("Invalid skill manifest. Only versions 1-2 and fixed world.status/resources.status actions are supported (1-4 steps); no executable content or extra fields.");
        if (manifest.AllowedAgents is null || manifest.Dependencies is null || manifest.AllowedAgents.Length > 6 ||
            manifest.AllowedAgents.Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.AllowedAgents.Length ||
            manifest.AllowedAgents.Any(role => !Enum.TryParse<AgentRole>(role, true, out var parsed) || !Enum.IsDefined(parsed)) ||
            manifest.Dependencies.Length > 3 || manifest.Dependencies.Distinct(StringComparer.Ordinal).Count() != manifest.Dependencies.Length ||
            manifest.Dependencies.Any(dependency => !SupportedDependencies.Contains(dependency)))
            throw new InvalidDataException("Invalid agent or dependency declaration.");
        if (manifest.Version == 1 && (manifest.AllowedAgents.Length != 0 || manifest.Dependencies.Length != 0 || manifest.RequiresNetwork))
            throw new InvalidDataException("Version 1 does not support agent or dependency declarations.");
        if (manifest.Version == 2)
        {
            var required = RequiredForSteps(manifest.Steps);
            if (required.Except(manifest.Dependencies, StringComparer.Ordinal).Any() ||
                manifest.RequiresNetwork != manifest.Dependencies.Contains("network", StringComparer.Ordinal))
                throw new InvalidDataException("Version 2 dependencies must include every action dependency and accurately declare network use.");
        }
    }
    private static bool Agent(LocalSkillManifest manifest, string role) =>
        Enum.TryParse<AgentRole>(role, true, out var parsed) && Enum.IsDefined(parsed) &&
        manifest.AllowedAgents.Contains(parsed.ToString(), StringComparer.OrdinalIgnoreCase);
    private static string[] RequiredForSteps(IEnumerable<string> steps) => steps.Select(step => step switch
    {
        "world.status" => "grid.world",
        "resources.status" => "grid.resources",
        _ => throw new InvalidDataException("Unsupported skill action.")
    }).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
}
