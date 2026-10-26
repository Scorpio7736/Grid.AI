using Grid.Communication;
using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public IReadOnlyList<Recipient> MeetingParticipants(EditSessionReference session, IReadOnlyList<string> names)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            RequireAgentActivity();
            if (names.Count is < 2 or > 6) throw new ArgumentException("Choose 2–6 registered agents, for example: agent meetings MCP MAXIS JN.");
            var participants = new List<Recipient>();
            foreach (var name in names)
            {
                var agent = tasks.Agents.SingleOrDefault(a => a.Role.ToString().Equals(name, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ArgumentException("Unknown or uninitialized agent: " + name + ". Use grid -start, then agents.");
                if (participants.Any(p => p.Id == agent.Id)) throw new ArgumentException("Duplicate meeting participant: " + name);
                if (!identities.RegisteredAgents.Contains(agent.Id) || world.GetProgram(agent.Id).Kind != Grid.Core.ResidentKind.Agent)
                    throw new InvalidOperationException("Meeting participant is no longer a registered agent: " + name);
                participants.Add(ResolveProgram(agent.Id.ToString()));
            }
            return participants.AsReadOnly();
        }
    }
    public Guid StartMeeting(EditSessionReference session, IReadOnlyList<string> names)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            var participants = MeetingParticipants(session, names);
            if (communications.Conversations.Count >= CommunicationState.MaxConversations) throw new InvalidOperationException("Conversation capacity reached; resume an existing meeting.");
            var meeting = new Conversation(Guid.NewGuid(), Array.AsReadOnly(new[] { LocalOperator }.Concat(participants).ToArray()),
                ConversationStatus.Open, clock.TickCount, null, Array.Empty<GridMessage>()) { IsMeeting = true };
            communications = communications with { Conversations = Array.AsReadOnly(communications.Conversations.Append(meeting).ToArray()) };
            RecordEvent("MeetingStarted", meeting.Id, null, "Shared meeting opened; participation grants no task authority or private context.");
            return meeting.Id;
        }
    }
    private (Recipient Recipient, string Reason) DialogueResponder(Conversation conversation, Recipient actor, string input)
    {
        if (!conversation.IsMeeting) return (Canonical(conversation.Participants.Single(p => !CommunicationState.SameIdentity(p, actor))), "Direct conversation");
        if (actor.Kind != ParticipantKind.Human || !CommunicationState.SameIdentity(actor, LocalOperator) ||
            !conversation.Participants.Any(p => CommunicationState.SameIdentity(p, actor)))
            throw new UnauthorizedAccessException("Only the meeting's human operator can request a shared reply.");
        var candidates = conversation.Participants.Where(p => p.Kind == ParticipantKind.Program).Select(p =>
        {
            var agent = tasks.Agents.SingleOrDefault(a => a.Id == p.Id);
            if (agent is null || !identities.RegisteredAgents.Contains(p.Id) || world.GetProgram(p.Id).Kind != Grid.Core.ResidentKind.Agent)
                throw new InvalidOperationException("Meeting participant is unavailable; no silent substitution.");
            return (Recipient: Canonical(p), Role: agent.Role);
        }).ToArray();
        var trimmed = input.TrimStart();
        if (trimmed.StartsWith('@'))
        {
            var end = trimmed.IndexOfAny([' ', ':', '\t', '\n']);
            if (end < 2 || string.IsNullOrWhiteSpace(trimmed[(end + 1)..])) throw new ArgumentException("Use talk @MAXIS your question (choose an agent in this meeting).");
            var name = trimmed[1..end];
            var selected = candidates.Where(c => c.Role.ToString().Equals(name,StringComparison.OrdinalIgnoreCase)).ToArray();
            if (selected.Length != 1) throw new ArgumentException("Directed agent is not in this meeting: " + name);
            return (selected[0].Recipient, "Directed to " + selected[0].Role);
        }
        var words = System.Text.RegularExpressions.Regex.Matches(input.ToLowerInvariant(), @"[\p{L}\p{N}]+").Select(m => m.Value).ToHashSet();
        string[] Terms(AgentRole role) => role switch
        {
            AgentRole.MAXIS => ["code","coding","build","compile","compiler","debug","bug","tests","test","project","programming"],
            AgentRole.SENTINEL => ["security","defender","firewall","malware","virus","threat","protection","vulnerability"],
            AgentRole.JN => ["files","file","storage","disk","duplicates","duplicate","archive","backup","quota","cleanup"],
            AgentRole.VOLT => ["cpu","gpu","ram","memory","energy","power","resources","performance","throttle","load"],
            AgentRole.RELAY => ["notify","notification","notifications","message","messages","inbox","delivery","briefing","speak"],
            _ => ["coordinate","coordination","delegate","delegation","plan","planning","tasks","task","priorities"]
        };
        var ranked = candidates.Select(c => (c.Recipient, c.Role, Score: Terms(c.Role).Count(words.Contains)))
            .OrderByDescending(c => c.Score).ThenBy(c => c.Role == AgentRole.MCP ? 0 : 1).ThenBy(c => c.Role.ToString(),StringComparer.Ordinal).ToArray();
        var best = ranked[0];
        return (best.Recipient, best.Score == 0 ? "No specialty match; deterministic fallback to " + best.Role : "Specialty keyword match: " + best.Role + "; ties prefer MCP, then alphabetical role");
    }
}
