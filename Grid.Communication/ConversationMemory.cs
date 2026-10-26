namespace Grid.Communication;

public enum MemoryKind { Preference, Decision, Fact, Unfinished }
public sealed record MemorySource(Guid ConversationId, Guid MessageId);
/// <summary>User-confirmed context, never evidence of a host action or independently verified fact.</summary>
public sealed record ConversationMemory(Guid Id, Guid AgentId, string Project, string Key, MemoryKind Kind,
    string Content, DateTimeOffset UpdatedAtUtc, IReadOnlyList<MemorySource> Sources, bool Forgotten = false)
{
    public bool IsStale(DateTimeOffset now) => now - UpdatedAtUtc >= TimeSpan.FromDays(30);
    public static bool ValidLabel(string? value) => value is { Length: > 0 and <= 48 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
    public static void Validate(CommunicationState state)
    {
        if (state.Memories is null || state.Memories.Count > 256 || state.MemoryRevision < 0) throw new InvalidDataException("Invalid conversation memories.");
        var ids = new HashSet<Guid>(); var keys = new HashSet<(Guid,string,string)>();
        foreach (var m in state.Memories)
        {
            if (m is null || m.Id == Guid.Empty || !ids.Add(m.Id) || m.AgentId == Guid.Empty || !ValidLabel(m.Project) || !ValidLabel(m.Key) ||
                m.Project != m.Project.ToLowerInvariant() || m.Key != m.Key.ToLowerInvariant() || !keys.Add((m.AgentId,m.Project,m.Key)) ||
                !Enum.IsDefined(m.Kind) || m.UpdatedAtUtc.Offset != TimeSpan.Zero || m.Sources is null || m.Sources.Count is < 1 or > 32 ||
                m.Sources.Distinct().Count() != m.Sources.Count || (m.Forgotten ? m.Content != "" : !CommunicationState.ValidContent(m.Content) || m.Content.Length > 600))
                throw new InvalidDataException("Invalid conversation memory.");
            foreach (var source in m.Sources)
            {
                var c = state.Conversations.SingleOrDefault(c => c.Id == source?.ConversationId);
                var message = c?.Messages.SingleOrDefault(x => x.Id == source!.MessageId);
                if (c is null || c.IsMeeting || c.ProjectScope != m.Project || !c.Participants.Any(p => p.Kind == ParticipantKind.Program && p.Id == m.AgentId) ||
                    message is null || message.Sender.Kind != ParticipantKind.Human || message.CreatedAtUtc > m.UpdatedAtUtc)
                    throw new InvalidDataException("Invalid memory source.");
            }
            if (!m.Forgotten && state.Conversations.Single(c => c.Id == m.Sources[^1].ConversationId).Messages.Single(x => x.Id == m.Sources[^1].MessageId).Content != m.Content)
                throw new InvalidDataException("Memory must preserve its user source.");
        }
    }
}
