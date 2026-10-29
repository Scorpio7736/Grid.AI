using Grid.Communication;
using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public Guid ChangeConversationMembership(EditSessionReference session, Guid conversationId, string agentName, bool remove,
        int expectedMessages, string expectedProject, long expectedMemory, long expectedActivity)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            RequireAgentActivity();
            var current = GetConversation(conversationId);
            if (current.Status != ConversationStatus.Open || !current.Participants.Any(p => CommunicationState.SameIdentity(p, LocalOperator)) ||
                current.Messages.Count != expectedMessages || current.ProjectScope != expectedProject || communications.MemoryRevision != expectedMemory || runVersion != expectedActivity)
                throw new InvalidOperationException("Invitation scope changed. Request a fresh preview; no membership changed.");
            var mcp = tasks.Agents.Single(a => a.Role == AgentRole.MCP);
            if (!current.Participants.Any(p => p.Id == mcp.Id)) throw new InvalidOperationException("Connect to MCP before changing specialist membership.");
            var specialist = tasks.Agents.SingleOrDefault(a => a.Role.ToString().Equals(agentName, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException("Unknown specialist.");
            if (specialist.Role == AgentRole.MCP) throw new ArgumentException("MCP remains the coordinator; disconnect to end the conversation.");
            if (!identities.RegisteredAgents.Contains(specialist.Id)) throw new UnauthorizedAccessException("Specialist registration was revoked.");
            var present = current.Participants.Any(p => p.Id == specialist.Id);
            if (present != remove) throw new ArgumentException(remove ? "Specialist is not present." : "Specialist is already present.");
            var names = current.Participants.Where(p => p.Kind == ParticipantKind.Program && (!remove || p.Id != specialist.Id))
                .Select(p => tasks.Agents.Single(a => a.Id == p.Id).Role.ToString()).ToList();
            if (!remove) names.Add(specialist.Role.ToString());
            var members = names.Count == 1 ? new[] { ResolveProgram(mcp.Id.ToString()) } : MeetingParticipants(session, names).ToArray();
            if (communications.Conversations.Count >= CommunicationState.MaxConversations) throw new InvalidOperationException("Conversation capacity reached; membership unchanged.");
            var next = new Conversation(Guid.NewGuid(), Array.AsReadOnly(new[] { LocalOperator }.Concat(members).ToArray()), ConversationStatus.Open, clock.TickCount, null, [])
                { IsMeeting = members.Length > 1, ProjectScope = current.ProjectScope };
            var memberIds = next.Participants.Select(p => (p.Kind, p.Id)).ToHashSet();
            // Retain transcripts while explicitly disabling cross-segment automatic recall.
            var prior = communications.Conversations.Select(c => c.Id == current.Id
                ? c with { Status = ConversationStatus.Ended, EndedTick = clock.TickCount, RecallAfterSequence = c.Messages.Count }
                : c.ProjectScope == next.ProjectScope && c.Participants.Select(p => (p.Kind, p.Id)).ToHashSet().SetEquals(memberIds)
                    ? c with { RecallAfterSequence = c.Messages.Count } : c).ToArray();
            communications = communications with { Conversations = Array.AsReadOnly(prior.Append(next).ToArray()), MemoryRevision = checked(communications.MemoryRevision + 1) };
            RecordEvent(remove ? "SpecialistRemoved" : "SpecialistInvited", next.Id, specialist.Role.ToString(),
                $"Human confirmed membership transition from {current.Id} to {next.Id}; new messages only, no private context grants.");
            return next.Id;
        }
    }
}
