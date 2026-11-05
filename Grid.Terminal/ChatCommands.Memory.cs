using Grid.Agent;
using Grid.Communication;
namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    private CommandResult Memory(IReadOnlyList<string> args)
    {
        if (identity is null || !identity.IsMaster) return new("Access denied: memory controls require human Master Control.");
        if (active is not { } id) return new("Connect to an agent first. Memory is scoped to that agent and the conversation project (default: general).");
        const string usage = "Usage: memory history | memory forget-history | memory list | memory scope <project> (empty conversation) | memory save <preference|decision|fact|unfinished> <key> <text> | memory correct <key> <text> | memory forget <key> | memory save-summary <context-id> <key> <approved-summary> --retain | memory save-outcome <task-id> <key> --retain | memory grant|revoke <key> <agent> <project> | memory expiry <key> <0-3650-days|never>";
        if (args.Count == 0) return new(usage);
        try
        {
            var action = args[0].ToLowerInvariant();
            if (action == "history" && args.Count == 1)
                return new("Automatic recall candidates (latest 20; model selects relevant bounded excerpts):\n" + TerminalTable.Render(["Conversation","Date UTC","Speaker","Excerpt"],
                    engine.ReviewConversationRecall(identity.Reference,id).Select(x => new[] { x.ConversationId.ToString(),x.Message.CreatedAtUtc.ToString("u"),x.Message.Sender.Name,x.Message.Sender.Kind == ParticipantKind.Program ? ConversationText(x.Message.Content) : x.Message.Content })));
            if (action == "forget-history" && args.Count == 1)
            {
                engine.ForgetConversationRecall(identity.Reference,id);
                save?.Invoke(CancellationToken.None).GetAwaiter().GetResult();
                return new("Earlier conversation recall cleared for this agent/project or meeting group. Transcripts and explicit memories retained; future messages can be recalled.");
            }
            if (action == "list" && args.Count == 1)
            {
                var memories = engine.ReviewMemories(identity.Reference,id);
                var c = engine.GetConversation(id);
                return new("Project: " + c.ProjectScope + " | User-confirmed context; not independently verified. Default expiry: 30 days; use memory expiry to change it. Received shares are read-only.\n" +
                    TerminalTable.Render(["Key","Kind","State","Content"],memories.Select(m => new[] { m.Key,m.Kind.ToString(),m.Forgotten ? "Forgotten" : m.IsStale(DateTimeOffset.UtcNow) ? "Stale" : "Active",m.Content })) +
                    string.Join("", memories.Select(m => $"\n{m.Key}: updated {m.UpdatedAtUtc:O}; sources " + string.Join(", ",m.Sources.Select(s => $"conversation {s.ConversationId} message {s.MessageId}")) +
                        $"; owner {m.AgentId}/{m.Project}; expiry {m.ExpiresAtUtc?.ToString("O") ?? "30 days after update"}" +
                        (m.Provenance is { } p ? $"; source {p.Kind} {p.SourceId}; SHA256 {p.SourceHash}; originally temporary {p.SourceWasTemporary}; raw file not retained by this memory" : "") +
                        string.Join("", m.Grants.Select(g => $"; grant {g.AgentId}/{g.Project}: {(g.Revoked ? "revoked" : "authorized (subject to memory expiry)")}")))));
            }
            string result;
            if (action == "save-summary" && args.Count >= 4 && Guid.TryParse(args[1], out var sourceId))
            {
                var retain = args[^1] == "--retain";
                var summary = string.Join(" ", args.Skip(3).Take(args.Count - 3 - (retain ? 1 : 0)));
                var memory = engine.RetainContextSummary(identity.Reference, id, sourceId, args[2], summary, retain);
                result = "Saved durable approved summary: " + memory.Key + ". Raw temporary file remains temporary. Source revocation/removal invalidates this summary and its grants.";
            }
            else if (action == "save-outcome" && args.Count is 3 or 4 && Guid.TryParse(args[1], out var taskId))
            {
                var memory = engine.RetainTaskOutcome(identity.Reference, id, taskId, args[2], args.Count == 4 && args[3] == "--retain");
                result = "Saved recorded task outcome: " + memory.Key + ". Task output/body was not retained.";
            }
            else if (action is "grant" or "revoke" && args.Count == 4)
            {
                var agent = engine.ResolveProgram(args[2]);
                engine.GrantMemory(identity.Reference, id, args[1], agent.Id, args[3], action == "revoke");
                result = action == "grant" ? "Granted this approved memory to " + agent.Name + " in project " + args[3] + ". No raw document or private transcript shared." : "Memory grant revoked. Earlier recipient-scope history excluded from AI recall; transcripts and historical backups remain.";
            }
            else if (action == "expiry" && args.Count == 3)
            {
                int? days = args[2].Equals("never", StringComparison.OrdinalIgnoreCase) ? null : int.TryParse(args[2], out var parsed) ? parsed : throw new ArgumentException("Use days or never.");
                engine.SetMemoryExpiry(identity.Reference, id, args[1], days);
                result = "Memory expiry updated. Existing recipient history excluded from recall; transcripts retained.";
            }
            else if (action == "scope" && args.Count == 2)
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
