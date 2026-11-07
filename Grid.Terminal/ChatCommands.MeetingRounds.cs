using Grid.Communication;
namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    private CommandResult SendMeetingRound(Guid id, string input, MessageOrigin origin)
    {
        var turns = engine.PrepareMeetingRound(id, Actor, input, origin);
        var reference = identity!.Reference; var used = 0;
        bool Current() => identity.IsAuthenticated && identity.Reference == reference && active == id && engine.IsRunning &&
            turns.All(t => engine.IsMemoryCurrent(t) && engine.ActivityGeneration == t.ActivityGeneration) && engine.GetConversation(id).Messages.Count == turns[0].MessageCount;
        return new("", BrokerOperation: async token =>
        {
            if (Interlocked.Exchange(ref used, 1) != 0 || !Current()) return new("Meeting round expired; no replies stored.");
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
            try
            {
                var replies = new List<InferenceReply>();
                foreach (var turn in turns)
                {
                    if (!Current()) return new("Meeting round cancelled or scope changed; no partial replies stored.");
                    using var authorization = AuthorizeInference(id, turn.ResponderId, "ai.chat", Current);
                    var pending = turn.Prompt.Length == 0 || inference is null ? Task.FromResult(new InferenceReply(false, "", "Context budget exceeded")) : inference!.ForAgent(engine.ResolveProgram(turn.ResponderId.ToString()).Name).GenerateAsync(turn.Prompt, stop.Token);
                    while (!pending.IsCompleted) { await Task.WhenAny(pending, Task.Delay(50, stop.Token)); if (token.IsCancellationRequested || !Current() || !authorization.IsCurrent) { stop.Cancel(); break; } }
                    replies.Add(await pending);
                    if (token.IsCancellationRequested || !Current() || !authorization.IsCurrent) return new("Meeting round cancelled or scope changed; no partial replies stored.");
                }
                var result = identity.Execute("chat", () => Current() ? Response(engine.CompleteMeetingRound(turns, input, replies, token)) : new("Meeting round expired; no replies stored."), "meeting-round");
                if (save is not null) await save(token);
                return result;
            }
            catch (OperationCanceledException) { return new("Meeting round cancelled; no partial replies stored."); }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or UnauthorizedAccessException) { return new("Meeting round stopped: " + e.Message); }
            catch (Exception e) when (e is IOException or Microsoft.Data.Sqlite.SqliteException) { return new("Meeting save failed; completed replies may remain in memory. Resolve persistence before continuing."); }
        });
    }
}
