using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grid.Terminal;

public sealed record TrustPermissionProfile(string Id, string Description, string[] Capabilities, bool Enabled, string[] Agents);
public sealed record TrustGoal(Guid Id, string Name, string Text, bool Enabled, DateTimeOffset CreatedUtc, DateTimeOffset? ExpiresUtc);
public sealed record TrustReceipt(DateTimeOffset AtUtc, string Action, string Subject, string Outcome);
public sealed record TrustState(int Version, bool SuggestionsEnabled, TrustPermissionProfile[] Profiles, TrustGoal[] Goals, TrustReceipt[] Receipts);

/// <summary>Durable opt-in trust preferences. These grants apply only to Trust Center suggestions, never to host action approval.</summary>
public sealed class TrustCenter
{
    private readonly string path; private readonly object gate = new();
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, MaxDepth = 12, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public TrustCenter(string path) { this.path = Path.GetFullPath(path); GridTransfer.CheckPath(this.path); }
    public TrustState Snapshot() { lock (gate) return Read(); }
    public void ControlProfile(string id, bool enabled)
    {
        lock (gate) { var state = Read(); var profile = Find(state, id); Save(WithReceipt(state with { Profiles = state.Profiles.Select(value => value.Id == id ? value with { Enabled = enabled } : value).ToArray() }, enabled ? "ProfileEnabled" : "ProfileDisabled", id, "Completed")); }
    }
    public void Grant(string id, string agent)
    {
        agent = NormalizeAgent(agent); lock (gate) { var state = Read(); var profile = Find(state, id); if (!profile.Enabled) throw new InvalidOperationException("Enable the profile before granting it.");
            Save(WithReceipt(state with { Profiles = state.Profiles.Select(value => value.Id == id ? value with { Agents = value.Agents.Append(agent).Distinct(StringComparer.Ordinal).Order().ToArray() } : value).ToArray() }, "Grant", id + ":" + agent, "Completed")); }
    }
    public void Revoke(string id, string agent)
    {
        agent = NormalizeAgent(agent); lock (gate) { var state = Read(); _ = Find(state, id); Save(WithReceipt(state with { Profiles = state.Profiles.Select(value => value.Id == id ? value with { Agents = value.Agents.Where(item => item != agent).ToArray() } : value).ToArray() }, "Revoke", id + ":" + agent, "Completed")); }
    }
    public TrustGoal AddGoal(string name, string text, DateTimeOffset? expires)
    {
        if (!Label(name, 48) || !Text(text, 300) || expires is { } value && value <= DateTimeOffset.UtcNow) throw new ArgumentException("Goal name/text/expiration is invalid.");
        lock (gate) { var state = Read(); if (state.Goals.Length >= 32 || state.Goals.Any(value => value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Goal name exists or the 32-goal limit was reached.");
            var goal = new TrustGoal(Guid.NewGuid(), name, text, false, DateTimeOffset.UtcNow, expires?.ToUniversalTime()); Save(WithReceipt(state with { Goals = state.Goals.Append(goal).ToArray() }, "GoalAdded", goal.Id.ToString(), "Disabled")); return goal; }
    }
    public void ControlGoal(Guid id, string action)
    {
        lock (gate) { var state = Read(); var goal = state.Goals.SingleOrDefault(value => value.Id == id) ?? throw new ArgumentException("Unknown goal."); TrustGoal? next = action switch { "enable" => goal with { Enabled = true }, "disable" => goal with { Enabled = false }, "remove" => null, _ => throw new ArgumentException("Invalid goal action.") };
            Save(WithReceipt(state with { Goals = state.Goals.Where(value => value.Id != id).Concat(next is null ? [] : [next]).ToArray() }, "Goal" + action, id.ToString(), "Completed")); }
    }
    public void Suggestions(bool enabled)
    { lock (gate) { var state = Read(); Save(WithReceipt(state with { SuggestionsEnabled = enabled }, "Suggestions", "local", enabled ? "Enabled" : "Disabled")); } }
    public bool CanSuggest(string agent)
    {
        var state = Snapshot(); agent = NormalizeAgent(agent); return state.SuggestionsEnabled && state.Profiles.Any(profile => profile.Id == "goal-advisor" && profile.Enabled && profile.Agents.Contains(agent, StringComparer.Ordinal));
    }
    private TrustState Read()
    {
        if (!File.Exists(path)) return new(1, false, DefaultProfiles(), [], []);
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read); if (file.Length > 512 * 1024) throw new InvalidDataException("Trust state exceeds its limit.");
        var state = JsonSerializer.Deserialize<TrustState>(file, Json) ?? throw new InvalidDataException("Invalid trust state.");
        if (state.Version != 1 || state.Profiles.Length != 3 || state.Goals.Length > 32 || state.Receipts.Length > 256) throw new InvalidDataException("Unsupported or oversized trust state.");
        foreach (var expected in DefaultProfiles()) { var profile = state.Profiles.SingleOrDefault(value => value.Id == expected.Id) ?? throw new InvalidDataException("Trust profile catalog changed."); if (!profile.Capabilities.SequenceEqual(expected.Capabilities)) throw new InvalidDataException("Trust profile capabilities changed."); }
        return state;
    }
    private void Save(TrustState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(state, Json)); File.Move(temporary, path, true); }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
    }
    private static TrustState WithReceipt(TrustState state, string action, string subject, string outcome) => state with { Receipts = state.Receipts.Append(new(DateTimeOffset.UtcNow, action, subject, outcome)).TakeLast(256).ToArray() };
    private static TrustPermissionProfile Find(TrustState state, string id) => state.Profiles.SingleOrDefault(value => value.Id == id) ?? throw new ArgumentException("Unknown permission profile.");
    private static string NormalizeAgent(string value) { value = value.ToUpperInvariant(); return value is "MCP" or "MAXIS" or "JN" or "TRON" or "VOLT" or "RELAY" ? value : throw new ArgumentException("Use a registered system-agent role."); }
    private static bool Label(string value, int max) => value.Length is > 0 && value.Length <= max && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    private static bool Text(string value, int max) => value.Length is > 0 && value.Length <= max && value.All(character => !char.IsControl(character));
    private static TrustPermissionProfile[] DefaultProfiles() =>
    [
        new("goal-advisor", "May produce on-demand suggestions from explicitly enabled local goals.", ["goal.read", "suggestion.propose"], false, []),
        new("workspace-reviewer", "Describes a common reviewed workspace flow; does not grant file access or approval.", ["context.read", "file.draft.propose"], false, []),
        new("task-coordinator", "Describes a common reviewed coordination flow; does not approve or run tasks.", ["world.read", "task.propose"], false, [])
    ];
}
