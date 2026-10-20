using Grid.Communication;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine : IGridCommunicationHub
{
    private CommunicationState communications = CommunicationState.Empty;

    public Recipient ResolveProgram(string idOrName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idOrName);
        lock (gate)
        {
            var matches = world.Programs.Values.Where(p => Guid.TryParse(idOrName, out var id) ? p.Id == id :
                p.Name.Equals(idOrName, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1) throw new ArgumentException(matches.Length == 0 ? "Unknown program recipient." : "Ambiguous name; use a full program ID.");
            return new(ParticipantKind.Program, matches[0].Id, matches[0].Name);
        }
    }

    private Recipient Canonical(Recipient participant)
    {
        if (!CommunicationState.ValidRecipient(participant)) throw new ArgumentException("Invalid participant identity.");
        return participant.Kind == ParticipantKind.Program ? ResolveProgram(participant.Id.ToString("D")) : participant;
    }

    public Guid StartConversation(Recipient sender, Recipient recipient, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            sender = Canonical(sender); recipient = Canonical(recipient);
            if (CommunicationState.SameIdentity(sender, recipient)) throw new ArgumentException("Conversation requires distinct participants.");
            if (communications.Conversations.Count >= CommunicationState.MaxConversations) throw new InvalidOperationException("Conversation capacity reached.");
            var conversation = new Conversation(Guid.NewGuid(), Array.AsReadOnly(new[] { sender, recipient }), ConversationStatus.Open,
                clock.TickCount, null, Array.AsReadOnly(Array.Empty<GridMessage>()));
            cancellationToken.ThrowIfCancellationRequested();
            communications = new(Array.AsReadOnly(communications.Conversations.Append(conversation).ToArray()));
            RecordEvent("ConversationStarted", conversation.Id, null, "Local conversation started; no external delivery or generated reply.");
            return conversation.Id;
        }
    }

    public GridMessage SendMessage(Guid conversationId, Recipient sender, string content, MessageOrigin origin = MessageOrigin.Terminal, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var c = GetConversation(conversationId);
            sender = Canonical(sender);
            if (c.Status != ConversationStatus.Open) throw new InvalidOperationException("Conversation has ended.");
            if (!c.Participants.Any(p => CommunicationState.SameIdentity(p, sender))) throw new ArgumentException("Sender is not a conversation participant.");
            if (!CommunicationState.ValidContent(content) || !Enum.IsDefined(origin)) throw new ArgumentException("Invalid message text or origin.");
            // Resolve the original resident ID; never silently redirect to a different same-name resident.
            foreach (var recipient in c.Participants.Where(p => p.Kind == ParticipantKind.Program)) _ = Canonical(recipient);
            if (communications.Conversations.Sum(x => x.Messages.Count) >= CommunicationState.MaxMessages) throw new InvalidOperationException("Message capacity reached; transcript was not truncated.");
            var message = new GridMessage(Guid.NewGuid(), c.Messages.Count + 1, sender, content, origin, clock.TickCount, timeProvider.GetUtcNow().ToUniversalTime());
            var changed = c with { Messages = Array.AsReadOnly(c.Messages.Append(message).ToArray()) };
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceConversation(changed);
            RecordEvent("MessageStored", conversationId, null, $"Local message {message.Sequence} stored; no execution or delivery acknowledgement.");
            return message;
        }
    }

    public void EndConversation(Guid conversationId, Recipient actor, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var c = GetConversation(conversationId); actor = Canonical(actor);
            if (!c.Participants.Any(p => CommunicationState.SameIdentity(p, actor))) throw new ArgumentException("Actor is not a conversation participant.");
            cancellationToken.ThrowIfCancellationRequested();
            if (c.Status == ConversationStatus.Ended) return;
            ReplaceConversation(c with { Status = ConversationStatus.Ended, EndedTick = clock.TickCount });
            RecordEvent("ConversationEnded", conversationId, null, "Local conversation ended.");
        }
    }

    private void ReplaceConversation(Conversation changed) => communications = new(Array.AsReadOnly(
        communications.Conversations.Select(c => c.Id == changed.Id ? changed : c).ToArray()));

    public Conversation GetConversation(Guid conversationId)
    {
        lock (gate) return communications.Conversations.SingleOrDefault(c => c.Id == conversationId)
            ?? throw new KeyNotFoundException("Unknown conversation.");
    }
}
