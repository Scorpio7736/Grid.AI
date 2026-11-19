using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Grid.Terminal;

public sealed record LocalSkillManifest(int Version, string Id, string Revision, string Origin,
    string License, string Description, string[] Steps);
public sealed record LocalSkillEntry(LocalSkillManifest Manifest, string Hash, bool Enabled, LocalSkillManifest? Previous = null);
public sealed record LocalSkillReceipt(DateTimeOffset AtUtc, string Skill, string Action, string Outcome);
public sealed record LocalSkillState(int Version, string Binding, LocalSkillEntry[] Skills, LocalSkillReceipt[] Receipts);

/// <summary>Declarative read-only skills. No executable content, prompts, paths, arguments or external dependencies.</summary>
public sealed class LocalSkills
{
    private readonly string path;
    private readonly string binding;
    private readonly object gate = new();
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 12 };
    public static IReadOnlyList<string> SupportedSteps { get; } = Array.AsReadOnly(new[] { "world.status", "resources.status" });
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
            }
            foreach (var receipt in state.Receipts)
                if (receipt is null || !Text(receipt.Skill,48) || !Text(receipt.Action,32) || !Text(receipt.Outcome,32))
                    throw new InvalidDataException("Invalid receipt.");
            return state.Binding == binding ? state : state with { Binding = binding,
                Skills = state.Skills.Select(s => s with { Enabled = false }).ToArray() };
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
                "enable" => entry with { Enabled = true },
                "disable" => entry with { Enabled = false },
                "rollback" when entry.Previous is { } previous => new(previous, Hash(previous), false, entry.Manifest),
                "remove" => entry,
                _ => throw new ArgumentException("Use enable, disable, remove, or rollback with a retained revision.")
            };
            Save(Receipt(state with { Skills = state.Skills.Where(s => s.Manifest.Id != id).Concat(action == "remove" ? [] : new[] { next }).ToArray() }, id, action, "completed"));
        }
    }
    public string Run(string id, Func<string, string> readStatus)
    {
        lock (gate)
        {
            var state = Read(); var entry = state.Skills.SingleOrDefault(s => s.Manifest.Id == id && s.Enabled)
                ?? throw new UnauthorizedAccessException("Skill missing or disabled. Review and enable on this device first.");
            // A persisted attempt precedes execution. There is no retry, scheduler, inference or arbitrary command dispatch.
            Save(Receipt(state, id, "run", "started"));
            var output = new List<string>();
            try
            {
                foreach (var step in entry.Manifest.Steps)
                {
                    var result = readStatus(step);
                    output.Add(step + "\n" + result[..Math.Min(result.Length, 8000)]);
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
    private static string Hash(LocalSkillManifest manifest) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(manifest, Json)));
    private static bool Text(string? text, int max) => !string.IsNullOrWhiteSpace(text) && text.Length <= max && !text.Any(char.IsControl);
    private static void Validate(LocalSkillManifest manifest)
    {
        if (manifest is null || manifest.Version != 1 || !Text(manifest.Id,48) || !Regex.IsMatch(manifest.Id, "^[a-z][a-z0-9-]*$", RegexOptions.CultureInvariant) ||
            !Text(manifest.Revision,64) || !Text(manifest.Origin,256) || !Text(manifest.License,128) || !Text(manifest.Description,512) ||
            manifest.Steps is null || manifest.Steps.Length is < 1 or > 4 || manifest.Steps.Any(s => !SupportedSteps.Contains(s)))
            throw new InvalidDataException("Invalid v1 skill. Only world.status and resources.status are supported (1-4 steps); no executable content or extra fields.");
    }
}
