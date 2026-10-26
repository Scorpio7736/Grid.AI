namespace Grid.Communication;

public enum ParticipantKind { Human, Program }
public enum MessageOrigin { Terminal, Microphone, WorldViewer, SystemEvent }
public enum ConversationStatus { Open, Ended }
public sealed record Recipient(ParticipantKind Kind, Guid Id, string Name);
public sealed record GridMessage(Guid Id, long Sequence, Recipient Sender, string Content, MessageOrigin Origin, long Tick, DateTimeOffset CreatedAtUtc);
public sealed record Conversation(Guid Id, IReadOnlyList<Recipient> Participants, ConversationStatus Status,
    long StartedTick, long? EndedTick, IReadOnlyList<GridMessage> Messages)
{
    public bool IsMeeting { get; init; }
    public string ProjectScope { get; init; } = "general";
    public long RecallAfterSequence { get; init; }
}
public sealed record CommunicationState(IReadOnlyList<Conversation> Conversations)
{
    public IReadOnlyList<VoiceProfile> VoiceProfiles { get; init; } = Array.Empty<VoiceProfile>();
    public IReadOnlyList<ConversationMemory> Memories { get; init; } = Array.Empty<ConversationMemory>();
    public long MemoryRevision { get; init; }
    public static CommunicationState Empty => new([]);
    public const int MaxConversations = 128, MaxMessages = 2048, MaxContent = 4096;
    public static CommunicationState Copy(CommunicationState state) => new(Array.AsReadOnly(state.Conversations.Select(c => c with {
        Participants = Array.AsReadOnly(c.Participants.ToArray()), Messages = Array.AsReadOnly(c.Messages.ToArray()) }).ToArray())) { VoiceProfiles = Array.AsReadOnly(state.VoiceProfiles.ToArray()), MemoryRevision = state.MemoryRevision,
        Memories = Array.AsReadOnly(state.Memories.Select(m => m with { Sources = Array.AsReadOnly(m.Sources.ToArray()) }).ToArray()) };
    public static bool SameIdentity(Recipient a, Recipient b) => a.Kind == b.Kind && a.Id == b.Id;
    public static bool ValidRecipient(Recipient? r) => r is not null && Enum.IsDefined(r.Kind) && r.Id != Guid.Empty &&
        !string.IsNullOrWhiteSpace(r.Name) && r.Name.Length <= 80 && !r.Name.Any(char.IsControl);
    public static bool ValidContent(string? text) => !string.IsNullOrWhiteSpace(text) && text.Length <= MaxContent &&
        !text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t'));
    public static void Validate(CommunicationState state, long tick)
    {
        if (state is null || state.Conversations is null || state.Conversations.Count > MaxConversations || state.Conversations.Any(c => c is null))
            throw new InvalidDataException("Invalid communication state.");
        VoiceProfile.Validate(state.VoiceProfiles);
        var ids = new HashSet<Guid>(); var messages = new HashSet<Guid>();
        foreach (var c in state.Conversations)
        {
            if (c.Id == Guid.Empty || !ids.Add(c.Id) || !Enum.IsDefined(c.Status) || c.StartedTick < 0 || c.StartedTick > tick ||
                c.Participants is null || (c.IsMeeting ? c.Participants.Count is < 3 or > 7 || c.Participants.Count(p => p?.Kind == ParticipantKind.Human) != 1 : c.Participants.Count != 2) || c.Participants.Any(p => !ValidRecipient(p)) ||
                c.Participants.Select(p => (p.Kind,p.Id)).Distinct().Count() != c.Participants.Count || c.Messages is null || c.Messages.Count > MaxMessages || !ConversationMemory.ValidLabel(c.ProjectScope) || c.ProjectScope != c.ProjectScope.ToLowerInvariant() || c.RecallAfterSequence < 0 || c.RecallAfterSequence > c.Messages.Count ||
                (c.Status == ConversationStatus.Open ? c.EndedTick is not null : c.EndedTick is null || c.EndedTick < c.StartedTick || c.EndedTick > tick))
                throw new InvalidDataException("Invalid conversation.");
            long previous = c.StartedTick;
            for (var i = 0; i < c.Messages.Count; i++)
            {
                var m = c.Messages[i];
                if (m is null || m.Id == Guid.Empty || !messages.Add(m.Id) || messages.Count > MaxMessages || m.Sequence != i + 1 ||
                    !ValidRecipient(m.Sender) || !c.Participants.Any(p => SameIdentity(p, m.Sender)) || !ValidContent(m.Content) ||
                    !Enum.IsDefined(m.Origin) || m.Tick < previous || m.Tick > (c.EndedTick ?? tick) || m.CreatedAtUtc.Offset != TimeSpan.Zero)
                    throw new InvalidDataException("Invalid conversation message.");
                previous = m.Tick;
            }
        }
        ConversationMemory.Validate(state);
    }
}

/// <summary>Trusted local host contract. Callers supply authenticated identity in a future adapter; this API does not authenticate.</summary>
public interface IGridCommunicationHub
{
    Recipient ResolveProgram(string idOrName);
    Guid StartConversation(Recipient sender, Recipient recipient, CancellationToken cancellationToken = default);
    GridMessage SendMessage(Guid conversationId, Recipient sender, string content, MessageOrigin origin = MessageOrigin.Terminal, CancellationToken cancellationToken = default);
    void EndConversation(Guid conversationId, Recipient actor, CancellationToken cancellationToken = default);
    Conversation GetConversation(Guid conversationId);
}
