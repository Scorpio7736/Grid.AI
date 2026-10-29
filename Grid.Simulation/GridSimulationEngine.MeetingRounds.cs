using Grid.Communication;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public IReadOnlyList<GroundedTurn> PrepareMeetingRound(Guid id, Recipient actor, string input, MessageOrigin origin)
    {
        lock (gate)
        {
            var c = GetConversation(id);
            if (!c.IsMeeting || !MeetingRoundRequested(input)) throw new ArgumentException("An explicit request to all meeting participants is required.");
            if (communications.Conversations.Sum(x => x.Messages.Count) + c.Participants.Count > CommunicationState.MaxMessages)
                throw new InvalidOperationException("Not enough message slots for a complete meeting round.");
            return c.Participants.Where(p => p.Kind == ParticipantKind.Program).Select(p => PrepareGroundedTurn(id, actor, "@" + p.Name + " " + input, origin)).ToArray();
        }
    }
    public IReadOnlyList<GridMessage> CompleteMeetingRound(IReadOnlyList<GroundedTurn> turns, string input, IReadOnlyList<InferenceReply> replies, CancellationToken token)
    {
        lock (gate)
        {
            if (turns.Count == 0 || replies.Count != turns.Count) throw new ArgumentException("Incomplete meeting round.");
            var first = turns[0]; var c = GetConversation(first.ConversationId);
            var fresh = PrepareMeetingRound(c.Id, first.Actor, input, first.Origin);
            if (fresh.Count != turns.Count || fresh.Where((t, i) => t.ResponderId != turns[i].ResponderId || t.Input != turns[i].Input).Any()) throw new InvalidOperationException("Meeting participants/request changed.");
            if (turns.Any(t => t.ConversationId != c.Id || t.Actor != first.Actor || t.MessageCount != c.Messages.Count || t.ActivityGeneration != runVersion || !IsMemoryCurrent(t)))
                throw new InvalidOperationException("Meeting changed during the round; no partial replies stored.");
            var utc = timeProvider.GetUtcNow().ToUniversalTime();
            var messages = new List<GridMessage> { new(Guid.NewGuid(), c.Messages.Count + 1, Canonical(first.Actor), input, first.Origin, clock.TickCount, utc) };
            for (var i = 0; i < turns.Count; i++)
            {
                var reply = replies[i]; var turn = turns[i];
                var content = reply.Available && CommunicationState.ValidContent(reply.Text)
                    ? $"[OBSERVED FACTS — captured before inference] {turn.Facts}\n[AI SUGGESTION / FICTIONAL DIALOGUE — unverified; no action executed]\n{Clip(reply.Text, 2400)}\n[SOURCES supplied to model] {turn.Sources}"
                    : "[LOCAL FALLBACK] Local AI unavailable or response rejected; no answer or task execution claimed.";
                messages.Add(new(Guid.NewGuid(), c.Messages.Count + messages.Count + 1, Canonical(c.Participants.Single(p => p.Id == turn.ResponderId)), content, MessageOrigin.SystemEvent, clock.TickCount, utc));
            }
            token.ThrowIfCancellationRequested();
            ReplaceConversation(c with { Messages = Array.AsReadOnly(c.Messages.Concat(messages).ToArray()) });
            RecordEvent("MeetingRoundStored", c.Id, null, "One user message and individually attributed responses stored atomically; no tasks executed.");
            return messages.AsReadOnly();
        }
    }
}
