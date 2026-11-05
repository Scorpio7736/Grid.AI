using Grid.Agent;
namespace Grid.Communication;

public static class ConversationMemoryValidation
{
    public static void Validate(CommunicationState state)
    {
        if (state.Memories is null || state.Memories.Count > 256 || state.MemoryRevision < 0) throw new InvalidDataException("Invalid conversation memories.");
        var ids = new HashSet<Guid>(); var keys = new HashSet<(Guid,string,string)>();
        foreach (var m in state.Memories)
        {
            if (m is null || m.Id == Guid.Empty || !ids.Add(m.Id) || m.AgentId == Guid.Empty || !ConversationMemory.ValidLabel(m.Project) || !ConversationMemory.ValidLabel(m.Key) ||
                m.Project != m.Project.ToLowerInvariant() || m.Key != m.Key.ToLowerInvariant() || !keys.Add((m.AgentId,m.Project,m.Key)) ||
                !Enum.IsDefined(m.Kind) || m.UpdatedAtUtc.Offset != TimeSpan.Zero || m.Sources is null || m.Sources.Count is < 1 or > 32 ||
                m.Sources.Distinct().Count() != m.Sources.Count || (m.Forgotten ? m.Content != "" : !CommunicationState.ValidContent(m.Content) || m.Content.Length > 600))
                throw new InvalidDataException("Invalid conversation memory.");
            if (m.Grants is null || m.Grants.Count > 32 || m.Grants.Any(g => g is null || g.AgentId == Guid.Empty || !ConversationMemory.ValidLabel(g.Project) || g.Project != g.Project.ToLowerInvariant()) ||
                m.Grants.Select(g => (g.AgentId, g.Project)).Distinct().Count() != m.Grants.Count || m.ExpiresAtUtc is { Offset: var offset } && offset != TimeSpan.Zero)
                throw new InvalidDataException("Invalid memory delegation or expiry.");
            if (m.Provenance is { } evidence && (evidence.Kind is not ("context" or "task") || evidence.SourceId == Guid.Empty || evidence.SourceHash is null || evidence.SourceHash.Length != 64 || evidence.SourceHash.Any(c => !Uri.IsHexDigit(c))))
                throw new InvalidDataException("Invalid durable memory provenance.");
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
