using Grid.Communication;
using Grid.Simulation;
namespace Grid.Terminal;

/// <summary>Per-terminal active conversation; transcripts and the local operator handle belong to the saved world.</summary>
public sealed partial class ChatCommands
{
    private readonly GridSimulationEngine engine;
    private Guid? active;
    private ILocalInference? inference;
    private Func<CancellationToken, Task>? save;
    private TerminalIdentitySession? identity;
    private Recipient Actor => identity?.CurrentRecipient ?? engine.LocalOperator;
    private ChatCommands(GridSimulationEngine engine) => this.engine = engine;
    public static ChatCommands Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession? identity = null, ILocalInference? inference = null, Func<CancellationToken, Task>? save = null)
    {
        var chat = new ChatCommands(engine) { identity = identity, inference = inference, save = save };
        if (identity?.IsAttachment != true)
            commands.Register(new("typewriter", "typewriter <on|off>", "Animate inline agent replies; any key except Enter skips. Redirected output remains instant.", args =>
            {
                if (args.Count != 1 || args[0].ToLowerInvariant() is not ("on" or "off")) return new("Usage: typewriter <on|off>");
                commands.Typewriter = args[0].Equals("on", StringComparison.OrdinalIgnoreCase);
                return new("Typewriter " + (commands.Typewriter ? "on." : "off."));
            }) { Capability = "chat" });
        commands.Register(new("memory", "memory <list|scope|save|correct|forget> ...", "Review and manage explicit private agent/project memory. User-confirmed context is not verified host evidence.", chat.Memory) { Capability = "chat" });
        commands.Register(new("assist", "assist <advice|analyze|propose|submit|discard> ...", "Scoped AI guidance and reviewed MCP task drafts; never automatic execution.", chat.Assist) { Capability = "chat" });
        commands.ConversationConnected = () => chat.active is not null;
        if (identity is not null) identity.Changed += () => chat.active = null;
        commands.Register(new("agent", "agent meetings <AGENT> <AGENT> [...] [-inline]", "Open a shared meeting in a new window. Use talk @AGENT message to direct a turn.", args => chat.Meeting(args, commands)) { Capability = "chat" });
        commands.Register(new("connect", "connect [program-id|name] [-inline]", "Open a dedicated conversation window; default contact is MCP. -inline keeps voice-compatible chat here.", chat.Connect));
        commands.Register(new("talk", "talk <message>", "Send to the connected program and receive grounded local AI dialogue or a labeled deterministic fallback.", chat.Talk));
        commands.Register(new("conversations", "conversations", "List the active identity's stored conversations.", chat.List));
        commands.Register(new("transcript", "transcript [conversation-id] [--details]", "Read the conversation; --details includes saved facts, sources and message metadata.", chat.Transcript));
        commands.Register(new("resume", "resume <conversation-id> [-inline]", "Open a conversation window for a saved conversation; -inline resumes here.", chat.Resume));
        commands.Register(new("disconnect", "disconnect", "End the connected conversation and return to ordinary commands.", chat.Disconnect));
        return chat;
    }
    public Guid? ActiveConversation => active;
    public CommandResult SendVoice(Guid conversation, string text)
    {
        if (active != conversation) return new("Voice draft belongs to a different conversation; record again.");
        return Send(conversation, text, MessageOrigin.Microphone);
    }
    private bool Owned(Conversation c) => c.Participants.Any(p => CommunicationState.SameIdentity(p, Actor));
    private Conversation Find(Guid id)
    {
        var c = engine.GetSnapshot().Communications.Conversations.SingleOrDefault(c => c.Id == id && Owned(c));
        return c ?? throw new ArgumentException("No conversation for this identity with that ID. Use conversations.");
    }
    private CommandResult Meeting(IReadOnlyList<string> args, CommandDispatcher commands)
    {
        if (args.Count < 3 || !args[0].Equals("meetings", StringComparison.OrdinalIgnoreCase)) return new("Usage: agent meetings MCP MAXIS [JN ...] [-inline]");
        if (identity is null || !identity.IsMaster) return new("Access denied. Human Master Control starts meetings.");
        if (active is not null) return new("Already connected. Disconnect before opening a meeting here.");
        var inline = args.Count(a => a.Equals("-inline",StringComparison.OrdinalIgnoreCase));
        if (inline > 1) return new("Use -inline only once.");
        var names = args.Skip(1).Where(a => !a.Equals("-inline",StringComparison.OrdinalIgnoreCase)).ToArray();
        try
        {
            var participants = engine.MeetingParticipants(identity.Reference,names);
            if (inline == 0 && commands.OpenConversation is not null)
                return commands.OpenConversation(new[] { "agent", "meetings" }.Concat(names).ToArray());
            active = engine.StartMeeting(identity.Reference,names);
            save?.Invoke(CancellationToken.None).GetAwaiter().GetResult();
            return new("Meeting: " + string.Join(", ",participants.Select(p => TerminalTable.Safe(p.Name))) +
                "\nOne participant replies per turn. Use talk <message> or talk @MAXIS <message>.\nThis transcript is shared. Private files and memories are not automatically included. Chat does not execute tasks.");
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or UnauthorizedAccessException) { return new("Meeting error: " + e.Message); }
        catch (Exception e) when (e is IOException or Microsoft.Data.Sqlite.SqliteException) { return new("Meeting save failed; it may remain in memory. Resolve persistence before continuing."); }
    }
    private string Contacts(Conversation c) => string.Join(", ", c.Participants.Where(p => !CommunicationState.SameIdentity(p, Actor)).Select(p => TerminalTable.Safe(p.Name)));
    private CommandResult Connect(IReadOnlyList<string> args)
    {
        engine.RequireAgentActivity();
        if (active is not null) return new("Already connected. Use disconnect to end it before connecting elsewhere.");
        if (engine.GetSnapshot().Communications.Conversations.Count >= CommunicationState.MaxConversations)
            return new("Conversation capacity reached. Use resume with an existing open conversation.");
        var name = args.Count == 0 ? "MCP" : string.Join(" ", args);
        var recipient = engine.ResolveProgram(name);
        active = engine.StartConversation(Actor, recipient);
        return new($"Connected to {TerminalTable.Safe(recipient.Name)}\nAI conversation; replies can be mistaken. Chat does not execute tasks.\nUse talk <message>. Earlier conversations are recalled automatically; memory save pins specific details. Read history with transcript; use transcript --details for sources and technical details.");
    }
    private CommandResult Talk(IReadOnlyList<string> args)
    {
        if (args.Count == 0) return new("Usage: talk <message>");
        if (active is not { } id) return new("No conversation connected. Use connect [program-id|name], or resume <conversation-id>.");
        return Send(id, string.Join(" ", args), MessageOrigin.Terminal);
    }
    private CommandResult Send(Guid id, string text, MessageOrigin origin)
    {
        if (inference is null || identity is null) return Response(engine.Talk(id, Actor, text, origin: origin));
        var turn = engine.PrepareGroundedTurn(id, Actor, text, origin);
        var reference = identity.Reference;
        var consumed = 0;
        bool Current() => engine.ConversationMemoryRevision == turn.MemoryRevision && engine.IsRunning && engine.ActivityGeneration == turn.ActivityGeneration && identity.IsAuthenticated && identity.Reference == reference && active == id;
        return new("", BrokerOperation: async token =>
        {
            if (Interlocked.Exchange(ref consumed, 1) != 0) return new("Conversation request already consumed.");
            if (!Current()) return new("Conversation stopped or session revoked; nothing stored.");
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
            try
            {
                var pending = turn.Prompt.Length == 0
                    ? Task.FromResult(new InferenceReply(false, "", "Input exceeds AI context budget."))
                    : inference.GenerateAsync(turn.Prompt, stop.Token);
                while (!pending.IsCompleted)
                {
                    await Task.WhenAny(pending, Task.Delay(50, stop.Token));
                    if (token.IsCancellationRequested || !Current()) { stop.Cancel(); break; }
                }
                var reply = await pending;
                if (token.IsCancellationRequested || !Current()) return new("Conversation cancelled or session revoked; nothing stored.");
                var result = identity.Execute("chat", () =>
                {
                    if (!Current()) return new("Conversation stopped or session revoked; nothing stored.");
                    var result = Response(engine.CompleteGroundedTurn(turn, reply, token));
                    var fallbackNotice = reply.ResponseRejected ? "AI response rejected: " + reply.Reason + " Using a basic reply.\n" : "Local AI is unavailable; using a basic reply.\n";
                    return reply.Available && CommunicationState.ValidContent(reply.Text) ? result : result with
                    { Text = fallbackNotice + result.Text, Presentation = new[] { new ConversationSpan(0,fallbackNotice.Length,ConversationTone.Error) }.Concat(ConversationPresentation.Shift(result.Presentation,fallbackNotice.Length)).ToArray(), AnimateFrom = result.AnimateFrom is { } offset ? offset + fallbackNotice.Length : null };
                }, "grounded-reply");
                if (save is not null) await save(token);
                return result;
            }
            catch (OperationCanceledException) { return new("Conversation cancelled; no incomplete turn stored."); }
            catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException)
            { return new("Conversation could not complete: " + e.Message); }
            catch (Exception e) when (e is IOException or Microsoft.Data.Sqlite.SqliteException or UnauthorizedAccessException) { return new("Conversation save failed; the completed turn may remain in memory. Resolve persistence before continuing."); }
        });
    }
    private CommandResult List(IReadOnlyList<string> args) => args.Count != 0 ? new("Usage: conversations") :
        new(TerminalTable.Render(["Conversation ID", "Contact", "Status", "Messages"], engine.GetSnapshot().Communications.Conversations.Where(Owned)
            .Select(c => new[] { c.Id.ToString(), Contacts(c), c.Status.ToString(), c.Messages.Count.ToString() })));
    private CommandResult Transcript(IReadOnlyList<string> args)
    {
        var details = args.Contains("--details", StringComparer.OrdinalIgnoreCase);
        var ids = args.Where(a => !a.Equals("--details", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (args.Count > 2 || ids.Length > 1 || ids.Length == 1 && !Guid.TryParse(ids[0], out _)) return new("Usage: transcript [conversation-id] [--details]");
        var id = ids.Length == 1 ? Guid.Parse(ids[0]) : active;
        if (id is null) return new("No conversation connected. Use transcript <conversation-id>; find IDs with conversations.");
        var c = Find(id.Value);
        var heading=details ? $"Conversation {c.Id} | {c.Status}\n" : "Conversation with " + TerminalTable.Safe(Contacts(c)) + "\n\n";
        return new(heading+(c.Messages.Count==0 ? "No messages." : Render(c.Messages,details)),Presentation:MessagePresentation(c.Messages,details,heading.Length));
    }
    private CommandResult Resume(IReadOnlyList<string> args)
    {
        if (args.Count != 1 || !Guid.TryParse(args[0], out var id)) return new("Usage: resume <conversation-id>");
        engine.RequireAgentActivity();
        if (active is not null) return new("Already connected. Use disconnect before resuming another conversation.");
        var c = Find(id);
        if (c.Status != ConversationStatus.Open) return new("Conversation has ended. Use connect to start a new one.");
        active = id;
        return new($"Resumed conversation {id}. Use talk, transcript or disconnect. Absent program recipients cannot receive new turns.");
    }
    private CommandResult Disconnect(IReadOnlyList<string> args)
    {
        if (args.Count != 0) return new("Usage: disconnect");
        if (active is not { } id) return new("No conversation connected.");
        engine.EndConversation(id, Actor); active = null;
        return new($"Ended conversation {id}. Transcript retained.");
    }
    private CommandResult Response(IEnumerable<GridMessage> messages)
    {
        var rows = messages.ToArray();
        var text = Render(rows);
        // A turn is already stored in full by the engine; animation is display metadata only.
        return new(text, AnimateFrom: rows.Length > 1 ? Render(rows.Take(1)).Length + 2 : null, Presentation: MessagePresentation(rows));
    }
    private string Render(IEnumerable<GridMessage> messages, bool details = false) => string.Join("\n\n", messages.Select(m =>
    {
        var content = details || m.Sender.Kind != ParticipantKind.Program ? m.Content : ConversationText(m.Content);
        var label = details ? $"#{m.Sequence} {TerminalTable.Safe(m.Sender.Name)} (tick {m.Tick}, {m.Origin})" :
            CommunicationState.SameIdentity(m.Sender, Actor) ? "You" : TerminalTable.Safe(m.Sender.Name);
        return label + ": " + new string(content.Select(c => char.IsControl(c) && c != '\n' ? ' ' : c).ToArray());
    }));
    private IReadOnlyList<ConversationSpan> MessagePresentation(IEnumerable<GridMessage> messages,bool details=false,int offset=0)
    {
        var spans=new List<ConversationSpan>();
        foreach(var message in messages)
        {
            var rendered=Render([message],details);
            var label=details ? $"#{message.Sequence} {TerminalTable.Safe(message.Sender.Name)} (tick {message.Tick}, {message.Origin})" : CommunicationState.SameIdentity(message.Sender,Actor) ? "You" : TerminalTable.Safe(message.Sender.Name);
            spans.Add(new(offset,label.Length+2,ConversationTone.Heading));
            spans.Add(new(offset+label.Length+2,rendered.Length-label.Length-2,message.Sender.Kind==ParticipantKind.Human || CommunicationState.SameIdentity(message.Sender,Actor) ? ConversationTone.User : ConversationTone.Agent));
            offset+=rendered.Length+2;
        }
        return spans.AsReadOnly();
    }
    // Display only: preserve the complete original envelope in storage and --details.
    private static string ConversationText(string text)
    {
        if (!text.StartsWith("[OBSERVED FACTS", StringComparison.Ordinal)) return text;
        var marker = text.IndexOf("\n[AI SUGGESTION / FICTIONAL DIALOGUE", StringComparison.Ordinal);
        if (marker < 0) return text;
        var start = text.IndexOf('\n', marker + 1);
        var end = text.LastIndexOf("\n[SOURCES supplied to model]", StringComparison.Ordinal);
        return start >= 0 && end > start ? text[(start + 1)..end].Trim() : text;
    }
}
