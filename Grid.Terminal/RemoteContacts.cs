using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grid.Terminal;

public enum ContactState { Proposed, Authorized, Delivering, Ringing, Accepted, Connected, Declined, Ended, Cancelled, Expired, Failed, Interrupted }
public sealed record ContactPolicy(string Id, string Device, string[] Agents, string[] Triggers, int QuietStartUtcHour,
    int QuietEndUtcHour, int MaxPerDay, DateTimeOffset ExpiresUtc, bool Enabled);
public sealed record ContactAttempt(Guid Id, string Policy, string Device, string Agent, string Trigger, string Reason,
    string DeduplicationKey, ContactState State, DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc);
public sealed record ContactReceipt(DateTimeOffset AtUtc, Guid Attempt, string State);
public sealed record ContactStateFile(int Version, ContactPolicy[] Policies, ContactAttempt[] Attempts, ContactReceipt[] Receipts)
{ public bool Paused { get; init; } }

public sealed class RemoteContactService
{
    private readonly string path; private readonly object gate = new();
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, MaxDepth = 12, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public RemoteContactService(string path) { this.path = Path.GetFullPath(path); GridTransfer.CheckPath(this.path); RecoverInterrupted(); }
    public ContactStateFile Snapshot() { lock (gate) return Read(); }
    public void SetPaused(bool paused)
    {
        lock (gate) { var state = Read(); Save(state with { Paused = paused, Attempts = paused ? state.Attempts.Select(value => Active(value.State) ? Transition(value, ContactState.Cancelled) : value).ToArray() : state.Attempts }); }
    }
    public void AddPolicy(ContactPolicy policy)
    {
        lock (gate) { Validate(policy); var state = Read(); if (state.Policies.Any(value => value.Id == policy.Id)) throw new ArgumentException("Policy already exists."); Save(state with { Policies = state.Policies.Append(policy with { Enabled = false }).ToArray() }); }
    }
    public void ControlPolicy(string id, string action)
    {
        lock (gate)
        {
            var state = Read(); var policy = state.Policies.SingleOrDefault(value => value.Id == id) ?? throw new ArgumentException("Unknown contact policy.");
            ContactPolicy? next = action switch { "enable" => policy with { Enabled = true }, "disable" => policy with { Enabled = false }, "remove" => null, _ => throw new ArgumentException("Invalid policy action.") };
            var attempts = action is "disable" or "remove" ? state.Attempts.Select(value => value.Policy == id && Active(value.State) ? Transition(value, ContactState.Cancelled) : value).ToArray() : state.Attempts;
            Save(state with { Policies = state.Policies.Where(value => value.Id != id).Concat(next is null ? [] : [next]).ToArray(), Attempts = attempts });
        }
    }
    public ContactAttempt Propose(string policyId, string agent, string trigger, string reason, string dedupe)
    {
        lock (gate)
        {
            var state = Read(); if (state.Paused) throw new UnauthorizedAccessException("Remote contact is paused."); var policy = state.Policies.SingleOrDefault(value => value.Id == policyId && value.Enabled) ?? throw new UnauthorizedAccessException("Contact policy missing or disabled.");
            var now = DateTimeOffset.UtcNow; agent = agent.ToUpperInvariant();
            if (now >= policy.ExpiresUtc || !policy.Agents.Contains(agent) || !policy.Triggers.Contains(trigger)) throw new UnauthorizedAccessException("Contact request is outside policy.");
            if (Quiet(policy, now.Hour)) throw new UnauthorizedAccessException("Contact policy quiet hours are active.");
            if (state.Attempts.Count(value => value.Policy == policyId && value.CreatedUtc.UtcDateTime.Date == now.UtcDateTime.Date) >= policy.MaxPerDay) throw new UnauthorizedAccessException("Contact policy daily limit reached.");
            var prior = state.Attempts.SingleOrDefault(value => value.DeduplicationKey == dedupe);
            if (prior is not null) return prior;
            if (!Text(reason, 200) || !Text(dedupe, 80)) throw new ArgumentException("Reason or deduplication key is invalid.");
            var attempt = new ContactAttempt(Guid.NewGuid(), policyId, policy.Device, agent, trigger, reason, dedupe, ContactState.Proposed, now, now);
            Save(Append(state, attempt)); return attempt;
        }
    }
    public ContactAttempt Authorize(Guid id) => Change(id, ContactState.Proposed, ContactState.Authorized);
    public ContactAttempt? Poll(string device)
    {
        lock (gate)
        {
            var state = Read(); var attempt = state.Attempts.FirstOrDefault(value => value.Device == device && value.State == ContactState.Authorized); if (attempt is null) return null;
            attempt = Transition(Transition(attempt, ContactState.Delivering), ContactState.Ringing); Save(Replace(state, attempt)); return attempt;
        }
    }
    public ContactAttempt Accept(Guid id, string device)
    {
        lock (gate) { var state = Read(); var attempt = state.Attempts.SingleOrDefault(value => value.Id == id && value.Device == device && value.State == ContactState.Ringing) ?? throw new UnauthorizedAccessException("Contact invitation is unavailable."); attempt = Transition(Transition(attempt, ContactState.Accepted), ContactState.Connected); Save(Replace(state, attempt)); return attempt; }
    }
    public ContactAttempt Finish(Guid id, string device, bool decline = false)
    {
        lock (gate) { var state = Read(); var attempt = state.Attempts.SingleOrDefault(value => value.Id == id && value.Device == device) ?? throw new UnauthorizedAccessException("Contact attempt is unavailable."); var target = decline && attempt.State == ContactState.Ringing ? ContactState.Declined : attempt.State == ContactState.Connected ? ContactState.Ended : throw new UnauthorizedAccessException("Contact transition is not allowed."); attempt = Transition(attempt, target); Save(Replace(state, attempt)); return attempt; }
    }
    public void Cancel(Guid id)
    {
        lock (gate) { var state = Read(); var attempt = state.Attempts.SingleOrDefault(value => value.Id == id) ?? throw new ArgumentException("Unknown contact attempt."); if (!Active(attempt.State)) return; Save(Replace(state, Transition(attempt, ContactState.Cancelled))); }
    }
    private ContactAttempt Change(Guid id, ContactState expected, ContactState target)
    { lock (gate) { var state = Read(); var attempt = state.Attempts.SingleOrDefault(value => value.Id == id && value.State == expected) ?? throw new UnauthorizedAccessException("Contact transition is not allowed."); attempt = Transition(attempt, target); Save(Replace(state, attempt)); return attempt; } }
    private void RecoverInterrupted()
    {
        lock (gate) { if (!File.Exists(path)) return; var state = Read(); var changed = state.Attempts.Select(value => Active(value.State) ? Transition(value, ContactState.Interrupted) : value).ToArray(); if (!changed.SequenceEqual(state.Attempts)) Save(state with { Attempts = changed }); }
    }
    private ContactStateFile Read()
    {
        if (!File.Exists(path)) return new(1, [], [], []);
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read); if (file.Length > 1024 * 1024) throw new InvalidDataException("Contact state exceeds its limit.");
        var state = JsonSerializer.Deserialize<ContactStateFile>(file, Json) ?? throw new InvalidDataException("Invalid contact state.");
        if (state.Version != 1 || state.Policies.Length > 32 || state.Attempts.Length > 500 || state.Receipts.Length > 500) throw new InvalidDataException("Unsupported or oversized contact state.");
        foreach (var policy in state.Policies) Validate(policy); return state;
    }
    private void Save(ContactStateFile state)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp"; try { using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough)) { JsonSerializer.Serialize(file, state with { Receipts = state.Receipts.TakeLast(500).ToArray() }, Json); file.Flush(true); } File.Move(temp, path, true); } finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private static ContactStateFile Append(ContactStateFile state, ContactAttempt attempt) => state with { Attempts = state.Attempts.Append(attempt).ToArray(), Receipts = state.Receipts.Append(new(attempt.UpdatedUtc, attempt.Id, attempt.State.ToString())).ToArray() };
    private static ContactStateFile Replace(ContactStateFile state, ContactAttempt attempt) => state with { Attempts = state.Attempts.Where(value => value.Id != attempt.Id).Append(attempt).ToArray(), Receipts = state.Receipts.Append(new(attempt.UpdatedUtc, attempt.Id, attempt.State.ToString())).ToArray() };
    private static ContactAttempt Transition(ContactAttempt attempt, ContactState state) => attempt with { State = state, UpdatedUtc = DateTimeOffset.UtcNow };
    private static bool Active(ContactState state) => state is ContactState.Proposed or ContactState.Authorized or ContactState.Delivering or ContactState.Ringing or ContactState.Accepted or ContactState.Connected;
    private static bool Quiet(ContactPolicy policy, int hour) => policy.QuietStartUtcHour == policy.QuietEndUtcHour ? false : policy.QuietStartUtcHour < policy.QuietEndUtcHour ? hour >= policy.QuietStartUtcHour && hour < policy.QuietEndUtcHour : hour >= policy.QuietStartUtcHour || hour < policy.QuietEndUtcHour;
    private static bool Text(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max && !value.Any(char.IsControl);
    private static void Validate(ContactPolicy policy)
    {
        if (!Text(policy.Id, 48) || !Text(policy.Device, 64) || policy.Agents is null || policy.Agents.Length is < 1 or > 6 || policy.Agents.Any(value => value is not ("MCP" or "RELAY" or "MAXIS" or "JN" or "VOLT" or "SENTINEL")) || policy.Triggers is null || policy.Triggers.Length is < 1 or > 8 || policy.Triggers.Any(value => !Text(value, 64)) || policy.QuietStartUtcHour is < 0 or > 23 || policy.QuietEndUtcHour is < 0 or > 23 || policy.MaxPerDay is < 1 or > 20 || policy.ExpiresUtc.Offset != TimeSpan.Zero) throw new InvalidDataException("Invalid contact policy.");
    }
}
