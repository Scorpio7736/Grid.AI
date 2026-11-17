using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Grid.Agent;
using Grid.Simulation;
using Grid.Simulation.Tasks;

namespace Grid.Terminal;

public sealed record InstalledSkill(SkillManifest Manifest, string Hash, bool Enabled, IReadOnlyDictionary<string, string> Configuration);
public sealed record SkillImportReview(Guid Id, string Source, string Hash, SkillManifest Manifest, DateTimeOffset ExpiresUtc);

public sealed class LocalSkillCatalog
{
    private sealed record State(bool Enabled, Dictionary<string, string> Configuration);
    private readonly string root;
    private readonly Dictionary<Guid, SkillImportReview> pending = new();
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public LocalSkillCatalog(string root)
    {
        this.root = Path.GetFullPath(root);
    }
    public string Root => root;
    public IReadOnlyList<InstalledSkill> List() => !Directory.Exists(root) ? [] : Directory.GetDirectories(root)
        .Where(path => !Path.GetFileName(path).Equals("history", StringComparison.OrdinalIgnoreCase))
        .Select(ReadDirectory).OrderBy(s => s.Manifest.Id, StringComparer.Ordinal).ToArray();
    public InstalledSkill Get(string id) => List().SingleOrDefault(s => s.Manifest.Id.Equals(id, StringComparison.Ordinal))
        ?? throw new ArgumentException("Skill is not installed: " + id);
    public bool IsUpdate(Guid reviewId) => pending.TryGetValue(reviewId, out var review) && List().Any(skill => skill.Manifest.Id == review.Manifest.Id);

    public SkillImportReview Review(string path, DateTimeOffset now)
    {
        if (!Path.IsPathFullyQualified(path) || path.StartsWith("\\\\", StringComparison.Ordinal) || path.StartsWith("\\\\?\\", StringComparison.Ordinal))
            throw new ArgumentException("Select an absolute local skill manifest path; network/device paths are unsupported.");
        path = Path.GetFullPath(path);
        var rootLength = Path.GetPathRoot(path)!.Length;
        if (path.AsSpan(rootLength).Contains(':') || !Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            throw new ArgumentException("Select an existing local .json skill manifest.");
        RejectReparse(path);
        var bytes = ReadBounded(path); var manifest = SkillManifest.Parse(bytes); ValidateCapabilities(manifest);
        var review = new SkillImportReview(Guid.NewGuid(), path, Hash(bytes), manifest, now.AddMinutes(5));
        if (pending.Count >= 16) pending.Remove(pending.OrderBy(p => p.Value.ExpiresUtc).First().Key);
        pending[review.Id] = review;
        return review;
    }
    public InstalledSkill Install(Guid reviewId, DateTimeOffset now)
    {
        if (!pending.Remove(reviewId, out var review) || review.ExpiresUtc <= now) throw new UnauthorizedAccessException("Skill review expired or was already used. Import again.");
        var bytes = ReadBounded(review.Source);
        if (Hash(bytes) != review.Hash) throw new InvalidOperationException("Skill manifest changed after review. Import again.");
        var manifest = SkillManifest.Parse(bytes); ValidateCapabilities(manifest);
        var target = DirectoryPath(manifest.Id); var existing = Directory.Exists(target) ? ReadDirectory(target) : null;
        if (existing is not null && existing.Manifest.SkillVersion == manifest.SkillVersion && existing.Hash == review.Hash)
            throw new InvalidOperationException("This exact skill version is already installed.");
        if (existing is not null) Archive(existing);
        Directory.CreateDirectory(Path.Combine(root, "history")); Directory.CreateDirectory(target);
        WriteAtomic(Path.Combine(target, "manifest.json"), bytes);
        var configuration = existing?.Configuration.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
        configuration = configuration.Where(p => manifest.ConfigurationKeys.Contains(p.Key, StringComparer.Ordinal)).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        WriteState(target, new(false, configuration));
        return ReadDirectory(target);
    }
    public InstalledSkill SetEnabled(string id, bool enabled)
    {
        var skill = Get(id); if (enabled) Dependencies(skill);
        WriteState(DirectoryPath(id), new(enabled, skill.Configuration.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal)));
        return Get(id);
    }
    public InstalledSkill Configure(string id, string key, string value)
    {
        var skill = Get(id);
        if (!skill.Manifest.ConfigurationKeys.Contains(key, StringComparer.Ordinal)) throw new ArgumentException("Configuration key is not declared by this skill.");
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256 || value.Any(char.IsControl) || value.Contains("${", StringComparison.Ordinal)) throw new ArgumentException("Configuration value must be 1–256 printable characters.");
        var values = skill.Configuration.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal); values[key] = value;
        WriteState(DirectoryPath(id), new(false, values));
        return Get(id);
    }
    public string Invocation(string id, TaskState tasks)
    {
        var skill = Get(id); if (!skill.Enabled) throw new InvalidOperationException("Enable the skill before invocation.");
        return Resolve(skill, tasks);
    }
    public string Check(string id, TaskState tasks) => Resolve(Get(id), tasks);
    private string Resolve(InstalledSkill skill, TaskState tasks)
    {
        Dependencies(skill);
        var lines = new List<string>();
        foreach (var step in skill.Manifest.Steps)
        {
            var argument = Regex.Replace(step.Argument, "\\$\\{(?<key>[a-z][a-z0-9.-]{0,31})\\}", match =>
                skill.Configuration.TryGetValue(match.Groups["key"].Value, out var value) ? value : throw new InvalidOperationException("Configure " + match.Groups["key"].Value + " before invocation."));
            if (!Enum.TryParse<AgentRole>(step.Agent, true, out var role) || !TaskCapabilities.Valid(role, step.Capability, argument))
                throw new InvalidOperationException("Skill arguments are incompatible with the current capability contract.");
            if (DevelopmentPolicy.IsReal(step.Capability) && !tasks.Workspaces.Any(w => w.Enabled && w.Name.Equals(argument.Split('|')[0], StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Required development workspace is unavailable: " + argument.Split('|')[0]);
            if (StoragePolicy.IsReal(step.Capability) && !tasks.Storage.Scopes.Any(s => s.Enabled && s.Name.Equals(argument, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Required storage scope is unavailable: " + argument);
            lines.Add(role + ": " + step.Capability[(step.Capability.IndexOf('.') + 1)..] + " " + argument);
        }
        return string.Join("; ", lines);
    }
    public void Uninstall(string id)
    {
        var skill = Get(id); if (skill.Enabled) throw new InvalidOperationException("Disable/revoke the skill before uninstalling it.");
        Archive(skill); Directory.Delete(DirectoryPath(id), true);
    }
    public InstalledSkill Rollback(string id)
    {
        InstalledSkill? current = null; try { current = Get(id); } catch (ArgumentException) { }
        if (current?.Enabled == true) throw new InvalidOperationException("Disable/revoke the skill before rollback.");
        var history = History(id).OrderByDescending(Path.GetFileName, StringComparer.Ordinal).ToArray();
        if (history.Length == 0) throw new InvalidOperationException("No retained skill revision is available.");
        var bytes = ReadBounded(history[0]); var manifest = SkillManifest.Parse(bytes); ValidateCapabilities(manifest);
        if (current is not null) Archive(current);
        var target = DirectoryPath(id); Directory.CreateDirectory(target); WriteAtomic(Path.Combine(target, "manifest.json"), bytes); WriteState(target, new(false, new(StringComparer.Ordinal)));
        return Get(id);
    }
    public static string Describe(InstalledSkill skill) => $"{skill.Manifest.Id} {skill.Manifest.SkillVersion} | {(skill.Enabled ? "Enabled" : "Disabled")} | {(skill.Manifest.NetworkRequired ? "Optional network required; unavailable offline" : "Offline capable")}\n" +
        $"{skill.Manifest.Name}: {skill.Manifest.Description}\nOrigin: {skill.Manifest.Origin}\nLicense: {skill.Manifest.License}\nSHA256: {skill.Hash}\nPermissions: {string.Join(", ", skill.Manifest.Permissions)}\nConfiguration: {(skill.Manifest.ConfigurationKeys.Count == 0 ? "none" : string.Join(", ", skill.Manifest.ConfigurationKeys.Select(k => k + "=" + (skill.Configuration.ContainsKey(k) ? "set" : "missing"))))}\nExecution: declarative existing capabilities only; no skill scripts, shell, network or model instructions are executed.";

    private void Dependencies(InstalledSkill skill)
    {
        if (skill.Manifest.NetworkRequired) throw new InvalidOperationException("This optional network-dependent skill is unavailable offline; connector execution arrives in 6.36.");
        var missing = skill.Manifest.ConfigurationKeys.Where(k => !skill.Configuration.ContainsKey(k)).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException("Missing configuration: " + string.Join(", ", missing));
    }
    private InstalledSkill ReadDirectory(string directory)
    {
        var bytes = ReadBounded(Path.Combine(directory, "manifest.json")); var manifest = SkillManifest.Parse(bytes); ValidateCapabilities(manifest);
        var statePath = Path.Combine(directory, "state.json"); var state = File.Exists(statePath) ? JsonSerializer.Deserialize<State>(ReadBounded(statePath), Json) ?? throw new InvalidDataException("Invalid skill state.") : new(false, new());
        if (state.Configuration is null || state.Configuration.Keys.Any(k => !manifest.ConfigurationKeys.Contains(k, StringComparer.Ordinal))) throw new InvalidDataException("Invalid skill configuration state.");
        return new(manifest, Hash(bytes), state.Enabled, new Dictionary<string, string>(state.Configuration, StringComparer.Ordinal));
    }
    private static void ValidateCapabilities(SkillManifest manifest)
    {
        foreach (var step in manifest.Steps)
            if (!Enum.TryParse<AgentRole>(step.Agent, true, out var role) || !McpCapabilityCatalog.Entries.Any(entry => entry.Agent == role && entry.Capability == step.Capability))
                throw new ArgumentException("Skill requests an unsupported capability or invalid argument shape.");
    }
    private void Archive(InstalledSkill skill)
    {
        var directory = Path.Combine(root, "history", skill.Manifest.Id); Directory.CreateDirectory(directory);
        var source = Path.Combine(DirectoryPath(skill.Manifest.Id), "manifest.json");
        File.Copy(source, Path.Combine(directory, DateTimeOffset.UtcNow.UtcTicks.ToString("D19") + "-" + Guid.NewGuid().ToString("N") + "-" + skill.Hash + ".json"), false);
    }
    private IEnumerable<string> History(string id) { var path = Path.Combine(root, "history", id); return Directory.Exists(path) ? Directory.GetFiles(path, "*.json") : []; }
    private string DirectoryPath(string id) => Path.Combine(root, id);
    private static byte[] ReadBounded(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
        if (stream.Length is < 2 or > 65536) throw new InvalidDataException("Skill file exceeds its 64 KiB bound.");
        var bytes = new byte[stream.Length]; stream.ReadExactly(bytes); return bytes;
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static void WriteState(string directory, State state) => WriteAtomic(Path.Combine(directory, "state.json"), JsonSerializer.SerializeToUtf8Bytes(state, Json));
    private static void WriteAtomic(string path, byte[] bytes)
    {
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N"); File.WriteAllBytes(temporary, bytes); File.Move(temporary, path, true);
    }
    private static void RejectReparse(string path)
    {
        for (var current = new FileInfo(path) as FileSystemInfo; current is not null; current = current is FileInfo f ? f.Directory : (current as DirectoryInfo)?.Parent)
            if ((current.Attributes & FileAttributes.ReparsePoint) != 0) throw new UnauthorizedAccessException("Skill manifest paths cannot contain links or junctions.");
    }
}
