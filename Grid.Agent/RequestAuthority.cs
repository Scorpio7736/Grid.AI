using Grid.AI;
namespace Grid.Agent;

/// <summary>In-process host gate. The host authenticates and revalidates each scope; this is not a public network API.</summary>
public sealed class RequestAuthority(TimeProvider? clock = null)
{
    private readonly object gate = new();
    private readonly TimeProvider time = clock ?? TimeProvider.System;
    private readonly Dictionary<(Guid, Guid), Budget> budgets = new();
    private readonly Queue<RequestAudit> audit = new();
    private sealed class Budget { public readonly Queue<DateTimeOffset> Starts = new(); public int Active; }
    public sealed record RequestAudit(DateTimeOffset Time, Guid World, Guid User, Guid Agent, Guid Session, long Generation, Guid Conversation, string Capability, string Outcome);
    public IReadOnlyList<RequestAudit> Audit { get { lock(gate) return audit.ToArray(); } }
    private void Record(TrustedRequestContext scope, string outcome)
    {
        lock(gate) { audit.Enqueue(new(time.GetUtcNow(), scope.WorldId, scope.UserId, scope.AgentId, scope.SessionId, scope.Generation, scope.ConversationId, scope.Capability is "ai.chat" or "ai.analyze" or "ai.propose" ? scope.Capability : "unknown", outcome)); while(audit.Count > 128) audit.Dequeue(); }
    }
    public Lease Acquire(TrustedRequestContext scope, Func<bool> hostAllows)
    {
        if (scope.WorldId == Guid.Empty || scope.UserId == Guid.Empty || scope.CallerId == Guid.Empty || scope.AgentId == Guid.Empty ||
            scope.SessionId == Guid.Empty || scope.ConversationId == Guid.Empty || scope.Generation < 0 || !ConversationMemory.ValidLabel(scope.Project) ||
            scope.Capability is not ("ai.chat" or "ai.analyze" or "ai.propose") || !hostAllows())
        { Record(scope, "denied"); throw new UnauthorizedAccessException("Request identity, capability or scope is no longer authorized."); }
        var key = (scope.WorldId, scope.UserId);
        lock(gate)
        {
            var now = time.GetUtcNow();
            foreach (var old in budgets.ToArray())
            { while(old.Value.Starts.Count > 0 && now - old.Value.Starts.Peek() >= TimeSpan.FromMinutes(1)) old.Value.Starts.Dequeue(); if(old.Value.Active == 0 && old.Value.Starts.Count == 0) budgets.Remove(old.Key); }
            if (!budgets.TryGetValue(key, out var budget))
            { if(budgets.Count >= 128) { Record(scope,"quota"); throw new InvalidOperationException("AI caller capacity reached; try later."); } budgets[key] = budget = new(); }
            if (budget.Active >= 2 || budget.Starts.Count >= 60) { Record(scope,"quota"); throw new InvalidOperationException("AI request limit reached; wait for current work or try again shortly."); }
            budget.Active++; budget.Starts.Enqueue(now); Record(scope,"allowed");
        }
        return new Lease(this, scope, hostAllows, () => { lock(gate) budgets[key].Active--; });
    }
    public sealed class Lease : IDisposable
    {
        private readonly RequestAuthority owner; private readonly Func<bool> validate; private Action? release;
        public TrustedRequestContext Context { get; }
        internal Lease(RequestAuthority owner, TrustedRequestContext scope, Func<bool> validate, Action release)
        { this.owner = owner; Context = scope; this.validate = validate; this.release = release; }
        public bool IsCurrent
        {
            get { var current = release is not null && validate(); if(!current) owner.Record(Context,"revoked-or-expired"); return current; }
        }
        public void Dispose() { var action = Interlocked.Exchange(ref release, null); if(action is not null) { action(); owner.Record(Context,"released"); } }
    }
}
