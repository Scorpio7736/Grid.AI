using Grid.AI;
namespace Grid.Agent;

/// <summary>Agent memory policy over host-validated saved records. Not an authentication boundary.</summary>
public static class MemoryPolicy
{
    public const int RecentMessages = 6;
    public const string Version = "agent-memory-v1";
    public static bool Visible(ConversationMemory memory, Guid agent, string project, DateTimeOffset now) =>
        !memory.Forgotten && !memory.IsStale(now) && (memory.AgentId == agent && memory.Project == project ||
            memory.Grants.Any(g => !g.Revoked && g.AgentId == agent && g.Project == project));
    public static IReadOnlyList<ConversationMemory> Relevant(IEnumerable<ConversationMemory> memories, Guid agent, string project, string input, DateTimeOffset now)
    {
        var words = RecallText.Terms(input); var recall = words.Contains("remember") || words.Contains("recall");
        return memories.Where(m => Visible(m, agent, project, now))
            .Select(m => (Memory: m, Score: RecallText.Score(m.Key + " " + m.Content, words)))
            .Where(x => recall || x.Score > 0 || x.Memory.Kind == MemoryKind.Preference)
            .OrderByDescending(x => x.Score).ThenByDescending(x => x.Memory.UpdatedAtUtc).ThenBy(x => x.Memory.Key, StringComparer.Ordinal)
            .Take(3).Select(x => x.Memory).ToArray();
    }
    public static bool SameRecallScope(string project, bool meeting, IEnumerable<(int Kind, Guid Id)> participants,
        string otherProject, bool otherMeeting, IEnumerable<(int Kind, Guid Id)> otherParticipants) =>
        project == otherProject && meeting == otherMeeting && participants.ToHashSet().SetEquals(otherParticipants);
}
