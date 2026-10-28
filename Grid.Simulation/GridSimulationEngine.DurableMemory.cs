using System.Security.Cryptography;
using System.Text;
using Grid.Communication;
using Grid.Simulation.Editing;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private static bool MemoryVisible(ConversationMemory memory, Guid agent, string project, DateTimeOffset now) =>
        !memory.Forgotten && !memory.IsStale(now) && (memory.AgentId == agent && memory.Project == project || memory.Grants.Any(g => !g.Revoked && g.AgentId == agent && g.Project == project));
    private void InvalidateMemoryHistory(ConversationMemory memory)
    {
        ExcludeOldMemoryHistory(memory.AgentId, memory.Project);
        foreach (var grant in memory.Grants) ExcludeOldMemoryHistory(grant.AgentId, grant.Project);
    }
    private ConversationMemory OwnedMemory(Guid conversationId, string key)
    {
        var (c, agent) = MemoryScope(conversationId);
        return communications.Memories.SingleOrDefault(m => m.AgentId == agent && m.Project == c.ProjectScope && m.Key == MemoryLabel(key)) ?? throw new ArgumentException("Unknown owned memory key. Received grants cannot be re-shared or modified.");
    }
    private void ReplaceMemory(ConversationMemory memory)
    {
        communications = communications with { MemoryRevision = checked(communications.MemoryRevision + 1),
            Memories = Array.AsReadOnly(communications.Memories.Select(m => m.Id == memory.Id ? memory : m).ToArray()) };
    }
    public ConversationMemory RetainContextSummary(EditSessionReference session, Guid conversationId, Guid documentId, string key, string summary, bool retain)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            if (!retain) throw new InvalidOperationException("Use --retain to explicitly approve durable summary retention. Raw temporary documents will not be retained.");
            var (c, agent) = MemoryScope(conversationId);
            var document = context.Documents.SingleOrDefault(d => d.Id == documentId);
            var temporary = false;
            if (document is null && temporaryContext.TryGetValue(documentId, out var item) && item.Session == session.SessionId)
            { document = item.Document; temporary = true; }
            if (document is null || document.GrantedAgentId != agent || !document.Project.Equals(c.ProjectScope, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("The selected source must be granted to this agent and project in your context scope.");
            return SaveMemoryCore(conversationId, key, summary, MemoryKind.Summary, false, new("context", document.Id, document.SourceHash, temporary));
        }
    }
    public ConversationMemory RetainTaskOutcome(EditSessionReference session, Guid conversationId, Guid taskId, string key, bool retain)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            if (!retain) throw new InvalidOperationException("Use --retain to explicitly save this task outcome.");
            var task = tasks.Records.SingleOrDefault(t => t.Id == taskId) ?? throw new ArgumentException("Unknown task.");
            if (task.Status is Tasks.TaskStatus.AwaitingApproval or Tasks.TaskStatus.Queued or Tasks.TaskStatus.Running) throw new InvalidOperationException("Only a finished task outcome can be retained.");
            var summary = $"Task {task.Id}: {task.Capability}; status {task.Status}; attempts {task.Attempts}. This is the recorded task outcome, not a guarantee of external state.";
            return SaveMemoryCore(conversationId, key, summary, MemoryKind.Outcome, false,
                new("task", task.Id, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(summary))), false));
        }
    }
    public void GrantMemory(EditSessionReference session, Guid conversationId, string key, Guid agent, string project, bool revoke)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            var old = OwnedMemory(conversationId, key); project = MemoryLabel(project);
            if (!identities.RegisteredAgents.Contains(agent) || !tasks.Agents.Any(a => a.Id == agent)) throw new ArgumentException("Choose a registered agent.");
            if (!revoke && (old.Forgotten || old.IsStale(timeProvider.GetUtcNow()))) throw new InvalidOperationException("Forgotten or expired memories cannot be granted.");
            if (agent == old.AgentId && project == old.Project) throw new ArgumentException("This is the owner scope; use memory forget or expiry instead.");
            var previous = old.Grants.SingleOrDefault(g => g.AgentId == agent && g.Project == project);
            if (revoke && previous is null) throw new ArgumentException("No matching grant.");
            if (previous is null && old.Grants.Count >= 32) throw new InvalidOperationException("Memory grant capacity reached.");
            var grants = old.Grants.Where(g => g.AgentId != agent || g.Project != project).Append(new MemoryGrant(agent, project, revoke)).ToArray();
            ReplaceMemory(old with { Grants = Array.AsReadOnly(grants) });
            if (revoke) ExcludeOldMemoryHistory(agent, project);
            RecordEvent(revoke ? "MemoryGrantRevoked" : "MemoryGranted", old.Id, agent.ToString(), "Explicit user-authorized summary sharing; raw documents and private conversations not shared.");
        }
    }
    public void SetMemoryExpiry(EditSessionReference session, Guid conversationId, string key, int? days)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            if (days is < 0 or > 3650) throw new ArgumentException("Use 0–3650 days or never.");
            var old = OwnedMemory(conversationId, key);
            if (old.Forgotten) throw new InvalidOperationException("Forgotten memory cannot be restored.");
            ReplaceMemory(old with { ExpiresAtUtc = days is { } value ? timeProvider.GetUtcNow().AddDays(value) : DateTimeOffset.MaxValue });
            InvalidateMemoryHistory(old);
            RecordEvent("MemoryExpiryChanged", old.Id, null, "Explicit memory retention expiry changed.");
        }
    }
    private void RevokeContextMemories(Guid source)
    {
        foreach (var memory in communications.Memories.Where(m => !m.Forgotten && m.Provenance is { Kind: "context" } p && p.SourceId == source).ToArray())
        {
            ReplaceMemory(memory with { Forgotten = true, Content = "", Grants = Array.AsReadOnly(memory.Grants.Select(g => g with { Revoked = true }).ToArray()) });
            InvalidateMemoryHistory(memory);
        }
    }
}
