using Grid.Communication;
using Grid.Simulation.Editing;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public long ConversationMemoryRevision { get { lock (gate) return communications.MemoryRevision; } }
    private (Conversation Conversation, Guid Agent) MemoryScope(Guid conversationId)
    {
        var c = GetConversation(conversationId);
        if (c.IsMeeting || !c.Participants.Any(p => CommunicationState.SameIdentity(p, LocalOperator)))
            throw new InvalidOperationException("Private memory requires a one-to-one conversation with the human operator.");
        var agent = c.Participants.Single(p => p.Kind == ParticipantKind.Program).Id;
        if (!tasks.Agents.Any(a => a.Id == agent) || !identities.RegisteredAgents.Contains(agent))
            throw new InvalidOperationException("Memory requires a registered agent.");
        return (c, agent);
    }
    private static string MemoryLabel(string value)
    {
        if (!ConversationMemory.ValidLabel(value)) throw new ArgumentException("Use 1–48 letters, digits, dots, underscores or hyphens for project/key.");
        return value.ToLowerInvariant();
    }
    public void SetMemoryProject(EditSessionReference session, Guid conversationId, string project)
    {
        using var lease = TaskLease(session,true);
        lock (gate)
        {
            var (c, _) = MemoryScope(conversationId);
            if (c.Messages.Count != 0 || c.Status != ConversationStatus.Open) throw new InvalidOperationException("Choose a project in a new, empty conversation.");
            var revision = checked(communications.MemoryRevision + 1);
            ReplaceConversation(c with { ProjectScope = MemoryLabel(project) });
            communications = communications with { MemoryRevision = revision };
        }
    }
    public IReadOnlyList<ConversationMemory> ReviewMemories(EditSessionReference session, Guid conversationId)
    {
        using var lease = TaskLease(session,true);
        lock (gate)
        {
            var (c, agent) = MemoryScope(conversationId);
            return Array.AsReadOnly(communications.Memories.Where(m => m.AgentId == agent && m.Project == c.ProjectScope).OrderBy(m => m.Key).ToArray());
        }
    }
    public ConversationMemory SaveMemory(EditSessionReference session, Guid conversationId, string key, string content, MemoryKind kind = MemoryKind.Fact, bool correct = false)
    {
        using var lease = TaskLease(session,true);
        lock (gate)
        {
            var (c, agent) = MemoryScope(conversationId); key = MemoryLabel(key);
            if (!Enum.IsDefined(kind) || !CommunicationState.ValidContent(content) || content.Length > 600) throw new ArgumentException("Memory text must contain 1–600 valid characters.");
            var old = communications.Memories.SingleOrDefault(m => m.AgentId == agent && m.Project == c.ProjectScope && m.Key == key);
            if (old?.Forgotten == true) throw new InvalidOperationException("This key was forgotten and is blocked from reuse. Choose a new key only to explicitly save new information.");
            if (correct != (old is not null)) throw new InvalidOperationException(correct ? "Unknown memory key." : "Memory key exists; use memory correct to replace conflicting information.");
            if (old?.Sources.Count >= 32 || old is null && communications.Memories.Count >= 256) throw new InvalidOperationException("Memory capacity reached; no source stored.");
            var revision = checked(communications.MemoryRevision + 1);
            var message = SendMessage(c.Id, LocalOperator, content);
            var memory = new ConversationMemory(old?.Id ?? Guid.NewGuid(), agent, c.ProjectScope, key, old?.Kind ?? kind, content,
                message.CreatedAtUtc, Array.AsReadOnly((old?.Sources ?? []).Append(new MemorySource(c.Id,message.Id)).ToArray()));
            communications = communications with { Memories = Array.AsReadOnly(communications.Memories.Where(m => m.Id != memory.Id).Append(memory).ToArray()), MemoryRevision = revision };
            if (old is not null) ExcludeOldMemoryHistory(agent,c.ProjectScope);
            RecordEvent(correct ? "MemoryCorrected" : "MemorySaved", memory.Id, null, "Explicit user-confirmed context; no model claim promoted to fact.");
            return memory;
        }
    }
    public void ForgetMemory(EditSessionReference session, Guid conversationId, string key)
    {
        using var lease = TaskLease(session,true);
        lock (gate)
        {
            var (c, agent) = MemoryScope(conversationId); key = MemoryLabel(key);
            var old = communications.Memories.SingleOrDefault(m => m.AgentId == agent && m.Project == c.ProjectScope && m.Key == key)
                ?? throw new ArgumentException("Unknown memory key.");
            communications = communications with { MemoryRevision = checked(communications.MemoryRevision + 1),
                Memories = Array.AsReadOnly(communications.Memories.Select(m => m.Id == old.Id ? m with { Content = "", Forgotten = true } : m).ToArray()) };
            ExcludeOldMemoryHistory(agent,c.ProjectScope);
            RecordEvent("MemoryForgotten",old.Id,null,"Recall disabled; original transcript retained, prior scoped history excluded from AI prompts.");
        }
    }
    private void ExcludeOldMemoryHistory(Guid agent, string project)
    {
        communications = communications with { Conversations = Array.AsReadOnly(communications.Conversations.Select(c =>
            !c.IsMeeting && c.ProjectScope == project && c.Participants.Any(p => p.Kind == ParticipantKind.Program && p.Id == agent)
                ? c with { RecallAfterSequence = c.Messages.Count } : c).ToArray()) };
    }
    private IEnumerable<GridMessage> RecallHistory(Conversation c)
    {
        var sources = communications.Memories.SelectMany(m => m.Sources).Select(s => s.MessageId).ToHashSet();
        var stale = communications.Memories.Where(m => m.Forgotten || m.IsStale(timeProvider.GetUtcNow())).Select(m => "user memory " + m.Id).ToArray();
        return c.Messages.Where(m => m.Sequence > c.RecallAfterSequence && !sources.Contains(m.Id) && !stale.Any(label => m.Content.Contains(label,StringComparison.Ordinal)));
    }
    private IEnumerable<ConversationMemory> RelevantMemories(Guid agent, string project, string input)
    {
        var words = System.Text.RegularExpressions.Regex.Matches(input.ToLowerInvariant(), @"[\p{L}\p{N}]+").Select(m => m.Value).Where(w => w.Length > 3 && !new[] { "what","that","this","with","have","does","about","your","please" }.Contains(w)).ToHashSet();
        var recall = words.Contains("remember") || words.Contains("recall");
        return communications.Memories.Where(m => m.AgentId == agent && m.Project == project && !m.Forgotten && !m.IsStale(timeProvider.GetUtcNow()))
            .Select(m => (Memory:m, Score:words.Count(w => (m.Key + " " + m.Content).Contains(w,StringComparison.OrdinalIgnoreCase))))
            .Where(x => recall || x.Score > 0 || x.Memory.Kind == MemoryKind.Preference)
            .OrderByDescending(x => x.Score).ThenByDescending(x => x.Memory.UpdatedAtUtc).ThenBy(x => x.Memory.Key,StringComparer.Ordinal).Take(3).Select(x => x.Memory);
    }
}
