using System.Text.Json;
using System.Text.RegularExpressions;
using Grid.Simulation.Tasks;

namespace Grid.Agent;

public sealed record SkillStep(string Agent, string Capability, string Argument);
public sealed record SkillManifest(int ManifestVersion, string Id, string Name, string SkillVersion, string Description,
    string Origin, string License, bool NetworkRequired, IReadOnlyList<string> Permissions,
    IReadOnlyList<string> ConfigurationKeys, IReadOnlyList<SkillStep> Steps)
{
    public const int CurrentVersion = 1;
    private static readonly Regex IdPattern = new("^[a-z][a-z0-9.-]{2,63}$", RegexOptions.CultureInvariant);
    private static readonly Regex VersionPattern = new("^[0-9]+\\.[0-9]+\\.[0-9]+(?:-[a-z0-9.-]+)?$", RegexOptions.CultureInvariant);
    private static readonly Regex KeyPattern = new("^[a-z][a-z0-9.-]{0,31}$", RegexOptions.CultureInvariant);

    public static SkillManifest Parse(byte[] bytes)
    {
        if (bytes.Length is < 2 or > 65536) throw new ArgumentException("Skill manifest must be 2–65,536 bytes.");
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        Fields(root, "manifestVersion", "id", "name", "skillVersion", "description", "origin", "license", "networkRequired", "permissions", "configurationKeys", "steps");
        var version = Integer(root, "manifestVersion");
        if (version != CurrentVersion) throw new ArgumentException("Unsupported skill manifest version.");
        var id = Text(root, "id", 64); var name = Text(root, "name", 80); var skillVersion = Text(root, "skillVersion", 40);
        var description = Text(root, "description", 512); var origin = Text(root, "origin", 512); var license = Text(root, "license", 120);
        if (!IdPattern.IsMatch(id) || !VersionPattern.IsMatch(skillVersion)) throw new ArgumentException("Invalid skill ID or semantic version.");
        if (root.GetProperty("networkRequired").ValueKind is not JsonValueKind.True and not JsonValueKind.False)
            throw new ArgumentException("networkRequired must be true or false.");
        var permissions = Strings(root, "permissions", 16, 80, KeyPattern);
        var configuration = Strings(root, "configurationKeys", 16, 32, KeyPattern);
        if (configuration.Any(key => Regex.IsMatch(key, "(?:password|secret|credential|token|api[-.]?key)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
            throw new ArgumentException("Skill configuration cannot request credentials or secrets. Use a future scoped connector instead.");
        var stepArray = root.GetProperty("steps");
        if (stepArray.ValueKind != JsonValueKind.Array || stepArray.GetArrayLength() is < 1 or > 8) throw new ArgumentException("A skill requires 1–8 typed steps.");
        var steps = new List<SkillStep>();
        foreach (var item in stepArray.EnumerateArray())
        {
            Fields(item, "agent", "capability", "argument");
            var agent = Text(item, "agent", 16); var capability = Text(item, "capability", 80); var argument = Text(item, "argument", 256, allowPlaceholders: true);
            if (!Enum.TryParse<AgentRole>(agent, true, out var role) || role == AgentRole.MCP || !Enum.IsDefined(role) ||
                !capability.StartsWith(role.ToString().ToLowerInvariant() + ".", StringComparison.Ordinal) || !permissions.Contains(capability, StringComparer.Ordinal))
                throw new ArgumentException("Each step must name a specialist capability declared in permissions.");
            foreach (Match match in Regex.Matches(argument, "\\$\\{(?<key>[a-z][a-z0-9.-]{0,31})\\}", RegexOptions.CultureInvariant))
                if (!configuration.Contains(match.Groups["key"].Value, StringComparer.Ordinal)) throw new ArgumentException("Step references an undeclared configuration key.");
            if (Regex.Replace(argument, "\\$\\{[a-z][a-z0-9.-]{0,31}\\}", "").Contains("${", StringComparison.Ordinal))
                throw new ArgumentException("Invalid configuration placeholder.");
            steps.Add(new(agent.ToUpperInvariant(), capability, argument));
        }
        if (permissions.Count != steps.Select(s => s.Capability).Distinct(StringComparer.Ordinal).Count())
            throw new ArgumentException("Permissions must exactly match the distinct invoked capabilities.");
        return new(version, id, name, skillVersion, description, origin, license, root.GetProperty("networkRequired").GetBoolean(),
            Array.AsReadOnly(permissions.ToArray()), Array.AsReadOnly(configuration.ToArray()), Array.AsReadOnly(steps.ToArray()));
    }

    private static void Fields(JsonElement element, params string[] names)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new ArgumentException("Expected a skill manifest object.");
        var actual = element.EnumerateObject().Select(p => p.Name).ToArray();
        if (actual.Length != names.Length || actual.Distinct().Count() != actual.Length || actual.Except(names).Any())
            throw new ArgumentException("Unexpected or duplicate skill manifest fields.");
    }
    private static int Integer(JsonElement root, string name) => root.GetProperty(name).ValueKind == JsonValueKind.Number && root.GetProperty(name).TryGetInt32(out var value)
        ? value : throw new ArgumentException(name + " must be an integer.");
    private static string Text(JsonElement root, string name, int max, bool allowPlaceholders = false)
    {
        var field = root.GetProperty(name); if (field.ValueKind != JsonValueKind.String) throw new ArgumentException(name + " must be text.");
        var value = field.GetString()!;
        if (string.IsNullOrWhiteSpace(value) || value.Length > max || value.Any(char.IsControl) || (!allowPlaceholders && value.Contains("${", StringComparison.Ordinal)))
            throw new ArgumentException("Invalid " + name + ".");
        return value;
    }
    private static IReadOnlyList<string> Strings(JsonElement root, string name, int maximum, int maxLength, Regex format)
    {
        var field = root.GetProperty(name);
        if (field.ValueKind != JsonValueKind.Array || field.GetArrayLength() > maximum) throw new ArgumentException("Invalid " + name + " list.");
        var values = field.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String ? item.GetString()! : throw new ArgumentException(name + " entries must be text.")).ToArray();
        if (values.Any(v => v.Length > maxLength || !format.IsMatch(v)) || values.Distinct(StringComparer.Ordinal).Count() != values.Length)
            throw new ArgumentException("Invalid or duplicate " + name + " entries.");
        return values;
    }
}
