using Grid.Communication;
namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    private CommandResult Memory(IReadOnlyList<string> args)
    {
        if (identity is null || !identity.IsMaster) return new("Access denied: memory controls require human Master Control.");
        if (active is not { } id) return new("Connect to an agent first. Memory is scoped to that agent and the conversation project (default: general).");
        const string usage = "Usage: memory list | memory scope <project> (empty conversation) | memory save <preference|decision|fact|unfinished> <key> <text> | memory correct <key> <text> | memory forget <key>";
        if (args.Count == 0) return new(usage);
        try
        {
            var action = args[0].ToLowerInvariant();
            if (action == "list" && args.Count == 1)
            {
                var memories = engine.ReviewMemories(identity.Reference,id);
                var c = engine.GetConversation(id);
                return new("Project: " + c.ProjectScope + " | User-confirmed context; not independently verified. Stale after 30 days.\n" +
                    TerminalTable.Render(["Key","Kind","State","Content"],memories.Select(m => new[] { m.Key,m.Kind.ToString(),m.Forgotten ? "Forgotten" : m.IsStale(DateTimeOffset.UtcNow) ? "Stale" : "Active",m.Content })) +
                    string.Join("", memories.Select(m => $"\n{m.Key}: updated {m.UpdatedAtUtc:O}; sources " + string.Join(", ",m.Sources.Select(s => $"conversation {s.ConversationId} message {s.MessageId}")))));
            }
            string result;
            if (action == "scope" && args.Count == 2)
            { engine.SetMemoryProject(identity.Reference,id,args[1]); result = "Memory project: " + engine.GetConversation(id).ProjectScope + "."; }
            else if (action == "save" && args.Count >= 4 && Enum.TryParse<MemoryKind>(args[1],true,out var kind) && Enum.IsDefined(kind))
            { var m = engine.SaveMemory(identity.Reference,id,args[2],string.Join(" ",args.Skip(3)),kind); result = "Saved user-confirmed " + m.Kind + ": " + m.Key + "."; }
            else if (action == "correct" && args.Count >= 3)
            { var m = engine.SaveMemory(identity.Reference,id,args[1],string.Join(" ",args.Skip(2)),correct:true); result = "Corrected: " + m.Key + ". Earlier scoped history is excluded from AI recall; transcript retained."; }
            else if (action == "forget" && args.Count == 2)
            { engine.ForgetMemory(identity.Reference,id,args[1]); result = "Forgotten: " + args[1] + ". Recall is blocked; original transcripts remain available. Earlier scoped history is excluded from AI prompts."; }
            else return new(usage);
            save?.Invoke(CancellationToken.None).GetAwaiter().GetResult();
            return new(result);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or UnauthorizedAccessException) { return new("Memory error: " + e.Message); }
        catch (Exception e) when (e is IOException or Microsoft.Data.Sqlite.SqliteException) { return new("Memory save failed; the change may remain in memory. Resolve persistence before restarting."); }
    }
}
