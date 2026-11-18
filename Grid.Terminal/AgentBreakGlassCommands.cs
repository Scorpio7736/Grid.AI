using Grid.Simulation;

namespace Grid.Terminal;

/// <summary>Original-console emergency self-controls for the active registered agent.</summary>
public sealed class AgentBreakGlassCommands
{
    private readonly GridSimulationEngine engine;
    private readonly TerminalIdentitySession identity;
    private readonly bool interactiveOriginalConsole;
    private readonly Func<CancellationToken, Task>? save;

    public AgentBreakGlassCommands(GridSimulationEngine engine, TerminalIdentitySession identity,
        bool interactiveOriginalConsole, Func<CancellationToken, Task>? save = null)
    {
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
        this.interactiveOriginalConsole = interactiveOriginalConsole;
        this.save = save;
    }

    public bool TryExecute(string line, out CommandResult result)
    {
        var text = line.Trim();
        var boundary = text.IndexOfAny([' ', '\t', '\r', '\n']);
        var first = boundary < 0 ? text : text[..boundary];
        if (!first.Equals("::grid-agent-break", StringComparison.OrdinalIgnoreCase))
        { result = new(""); return false; }
        IReadOnlyList<string> tokens;
        try { tokens = CommandDispatcher.Parse(line); }
        catch (ArgumentException) { result = new(DevelopmentBreakGlassRegistry.GenericDenial); return true; }
        if (!Eligible()) { result = new(DevelopmentBreakGlassRegistry.GenericDenial); return true; }
        result = tokens.Count == 2 ? tokens[1].ToLowerInvariant() switch
        {
            "self-status" => Status(false),
            "evidence-self" => Status(true),
            "abort-self" => Abort(),
            "detach-self" => Detach(),
            _ => new(DevelopmentBreakGlassRegistry.GenericDenial)
        } : new(DevelopmentBreakGlassRegistry.GenericDenial);
        return true;
    }

    private bool Eligible() => interactiveOriginalConsole && !identity.IsAttachment &&
        identity.IsAuthenticated && !identity.IsMaster;

    private CommandResult Status(bool details)
    {
        var status = engine.AgentBreakGlassStatus(identity.Reference);
        var text = $"REGISTERED AGENT SELF-STATUS\nAgent: {status.AgentName}\nRole: {status.Role}\nAgent ID: {status.AgentId}\n" +
            $"GRID activity: {(status.GridRunning ? "Active" : "Paused")}\nOwn tasks: awaiting {status.AwaitingApproval} | queued {status.Queued} | running {status.Running} | terminal {status.Terminal}";
        if (details)
            text += "\nSELF EVIDENCE\n" + (status.RecentTasks.Count == 0 ? "No self task identifiers." :
                string.Join('\n', status.RecentTasks.Select(task => $"{task.TaskId} | {task.Status} | attempts {task.Attempts}"))) +
                (status.Truncated ? "\nEvidence truncated to the newest 32 self tasks." : "");
        return new(text + "\nNo task arguments, results, private context, other-agent records or Master Control data were exposed.");
    }

    private CommandResult Abort()
    {
        var cancelled = engine.AgentBreakGlassAbortSelf(identity.Reference);
        if (cancelled.Count == 0) return new("No active self tasks required cancellation. No other agent was affected.");
        string persistence;
        try
        {
            if (save is null) persistence = " This memory-only session has no durable save.";
            else { save(CancellationToken.None).GetAwaiter().GetResult(); persistence = " Cancellation state saved."; }
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or System.Data.Common.DbException)
        { persistence = " Cancellation took effect in memory, but saving failed; keep GRID open and resolve persistence before shutdown."; }
        return new($"Cancelled {cancelled.Count} active self task(s). No other agent was affected.{persistence}\n" +
            string.Join('\n', cancelled.Select(task => $"{task.TaskId} | Cancelled")));
    }

    private CommandResult Detach()
    {
        _ = engine.AgentBreakGlassStatus(identity.Reference); // Recheck exact original-host agent authority.
        identity.Logout();
        return new("Agent detached. Terminal is locked; Master Control was not activated. Use normal login to authenticate again.");
    }
}
