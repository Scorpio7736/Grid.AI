using Grid.Communication;
using Grid.Simulation;
namespace Grid.Terminal;

/// <summary>Per-terminal active conversation; transcripts and the local operator handle belong to the saved world.</summary>
public sealed class ChatCommands
{
    private readonly GridSimulationEngine engine;
    private Guid? active;
    private TerminalIdentitySession? identity;
    private Recipient Actor => identity?.CurrentRecipient ?? engine.LocalOperator;
    private ChatCommands(GridSimulationEngine engine) => this.engine = engine;
    public static ChatCommands Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession? identity = null)
    {
        var chat = new ChatCommands(engine) { identity = identity };
        if (identity is not null) identity.Changed += () => chat.active = null;
        commands.Register(new("connect", "connect [program-id|name]", "Start a conversation; default contact is ARCHON.", chat.Connect));
        commands.Register(new("talk", "talk <message>", "Send to the connected program and receive a labeled deterministic reply.", chat.Talk));
        commands.Register(new("conversations", "conversations", "List the active identity's stored conversations.", chat.List));
        commands.Register(new("transcript", "transcript [conversation-id]", "Read a stored conversation; defaults to the connected one.", chat.Transcript));
        commands.Register(new("resume", "resume <conversation-id>", "Reconnect to an open conversation by its full ID.", chat.Resume));
        commands.Register(new("disconnect", "disconnect", "End the connected conversation and return to ordinary commands.", chat.Disconnect));
        return chat;
    }
    public Guid? ActiveConversation => active;
    public CommandResult SendVoice(Guid conversation, string text)
    {
        if (active != conversation) return new("Voice draft belongs to a different conversation; record again.");
        return new(Render(engine.Talk(conversation, Actor, text, origin: MessageOrigin.Microphone)));
    }
    private bool Owned(Conversation c) => c.Participants.Any(p => CommunicationState.SameIdentity(p, Actor));
    private Conversation Find(Guid id)
    {
        var c = engine.GetSnapshot().Communications.Conversations.SingleOrDefault(c => c.Id == id && Owned(c));
        return c ?? throw new ArgumentException("No conversation for this identity with that ID. Use conversations.");
    }
    private CommandResult Connect(IReadOnlyList<string> args)
    {
        if (active is not null) return new("Already connected. Use disconnect to end it before connecting elsewhere.");
        if (engine.GetSnapshot().Communications.Conversations.Count >= CommunicationState.MaxConversations)
            return new("Conversation capacity reached. Use resume with an existing open conversation.");
        var name = args.Count == 0 ? "ARCHON" : string.Join(" ", args);
        var recipient = name.Equals("ARCHON", StringComparison.OrdinalIgnoreCase) && (identity is null || identity.IsMaster) ? engine.EnsureCoordinator() : engine.ResolveProgram(name);
        active = engine.StartConversation(Actor, recipient);
        return new($"Connected to {TerminalTable.Safe(recipient.Name)} | Conversation {active}\nReplies are deterministic GRID dialogue; no host tasks are executed.\nUse talk <message>, transcript, or disconnect. Ordinary GRID commands still work.");
    }
    private CommandResult Talk(IReadOnlyList<string> args)
    {
        if (args.Count == 0) return new("Usage: talk <message>");
        if (active is not { } id) return new("No conversation connected. Use connect [program-id|name], or resume <conversation-id>.");
        var pair = engine.Talk(id, Actor, string.Join(" ", args));
        return new(Render(pair));
    }
    private CommandResult List(IReadOnlyList<string> args) => args.Count != 0 ? new("Usage: conversations") :
        new(TerminalTable.Render(["Conversation ID", "Contact", "Status", "Messages"], engine.GetSnapshot().Communications.Conversations.Where(Owned)
            .Select(c => new[] { c.Id.ToString(), c.Participants.Single(p => !CommunicationState.SameIdentity(p, Actor)).Name, c.Status.ToString(), c.Messages.Count.ToString() })));
    private CommandResult Transcript(IReadOnlyList<string> args)
    {
        if (args.Count > 1 || args.Count == 1 && !Guid.TryParse(args[0], out _)) return new("Usage: transcript [conversation-id]");
        var id = args.Count == 1 ? Guid.Parse(args[0]) : active;
        if (id is null) return new("No conversation connected. Use transcript <conversation-id>; find IDs with conversations.");
        var c = Find(id.Value);
        return new($"Conversation {c.Id} | {c.Status}\n" + (c.Messages.Count == 0 ? "No messages." : Render(c.Messages)));
    }
    private CommandResult Resume(IReadOnlyList<string> args)
    {
        if (args.Count != 1 || !Guid.TryParse(args[0], out var id)) return new("Usage: resume <conversation-id>");
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
    private static string Render(IEnumerable<GridMessage> messages) => string.Join("\n", messages.Select(m =>
        $"#{m.Sequence} {TerminalTable.Safe(m.Sender.Name)} (tick {m.Tick}, {m.Origin}): {new string(m.Content.Select(c => char.IsControl(c) ? ' ' : c).ToArray())}"));
}
