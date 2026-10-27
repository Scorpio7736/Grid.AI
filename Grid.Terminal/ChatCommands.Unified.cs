using System.Text.RegularExpressions;
using Grid.Communication;
using Grid.Simulation.Editing;

namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    private CommandDispatcher dispatcher = null!;
    private sealed record FollowUp(string Kind, string Request, EditSessionReference Session, Guid Conversation,
        string Project, long Activity, long Context, long Memory, string Workspace = "", string Target = "");
    private FollowUp? followUp;
    private (FollowUp Scope, string Text)? talkDetails;
    private static bool Starts(string text, string pattern) => Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private CommandResult Explain(string text) => ConversationPresentation.AgentReply("GRID", text);
    private CommandResult Dispatch(params string[] tokens) => dispatcher.ExecuteTokens(tokens);
    private FollowUp Pending(string kind, string request, string workspace = "", string target = "") =>
        new(kind, request, identity!.Reference, active!.Value, Find(active.Value).ProjectScope,
            engine.ActivityGeneration, engine.ContextRevision, engine.ConversationMemoryRevision, workspace, target);
    private bool Current(FollowUp p) => identity?.IsAuthenticated == true && identity.IsMaster && identity.Reference == p.Session &&
        active == p.Conversation && engine.IsRunning && engine.ActivityGeneration == p.Activity && engine.ContextRevision == p.Context &&
        engine.ConversationMemoryRevision == p.Memory && Find(p.Conversation).Status == ConversationStatus.Open && Find(p.Conversation).ProjectScope == p.Project;
    private CommandResult Ask(string kind, string request, string question, string workspace = "", string target = "")
    { followUp = Pending(kind, request, workspace, target); return Explain(question); }

    // Deliberately bounded routing of the operator's message only. No model output, uploaded text,
    // recalled message or OCR text is ever interpreted as a command or authorization.
    private CommandResult UnifiedTalk(string text)
    {
        if (!CommunicationState.ValidContent(text)) return new("Error: Please provide a valid message.");
        if (active is null)
        {
            if (identity?.IsMaster != true) return new("No conversation connected. Use connect [program-id|name], or resume <conversation-id>.");
            var connected = Connect(["MCP"]);
            if (active is null) return connected;
        }
        var id = active!.Value;
        engine.RequireAgentActivity();
        var input = text.Trim();
        if (input.Equals("cancel", StringComparison.OrdinalIgnoreCase))
        { followUp = null; return Explain("Follow-up cancelled. No action taken."); }
        if (followUp is { } pending)
        {
            followUp = null;
            if (!Current(pending)) return Explain("The session or scope changed. Please repeat your request; nothing was performed.");
            return ContinueTalk(pending, input);
        }
        input = Regex.Replace(input, @"^(?:(?:can|could|would) you\s+|please\s+|I (?:want|need) you to\s+)+", "", RegexOptions.IgnoreCase).Trim();
        // Directed meeting turns retain the established participant/recall routing.
        if (input.StartsWith('@')) return Send(id, input, MessageOrigin.Terminal);
        if (identity?.IsMaster != true)
            return identity is not null && Starts(input, @"^(build|test|plan|edit|approve|run|apply|delete|select files|look at .*screen)\b")
                ? Explain("Access denied. Operational assistance requires human Master Control.") : Send(id, input, MessageOrigin.Terminal);
        if (input.Equals("details", StringComparison.OrdinalIgnoreCase))
        {
            if (talkDetails is { } details && Current(details.Scope)) return new(details.Text);
            talkDetails = null;
            return Explain("No current analysis details. Analyze again if the session or context changed.");
        }
        if (input.Equals("submit plan", StringComparison.OrdinalIgnoreCase)) return TalkHints(Assist(["submit"]));
        if (input.Equals("discard plan", StringComparison.OrdinalIgnoreCase)) return Assist(["discard"]);
        if (input.Equals("forget conversation history", StringComparison.OrdinalIgnoreCase)) return Memory(["forget-history"]);
        if (input.Equals("show memories", StringComparison.OrdinalIgnoreCase)) return Memory(["list"]);
        var project = Regex.Match(input, @"^project\s+(.+)$", RegexOptions.IgnoreCase);
        if (project.Success) return Memory(["scope", project.Groups[1].Value]);
        var memory = Regex.Match(input, @"^(remember(?: preference| fact| decision| unfinished)?|correct memory)\s+([^\s:]+):\s*(.+)$|^(forget memory)\s+([^\s]+)$", RegexOptions.IgnoreCase);
        if (memory.Success)
        {
            if (memory.Groups[4].Success) return Memory(["forget", memory.Groups[5].Value]);
            var action = memory.Groups[1].Value.ToLowerInvariant(); var key = memory.Groups[2].Value; var body = memory.Groups[3].Value;
            if (body.Length == 0) return Explain("Give a memory key and its text, for example: talk remember preference style: Keep replies concise.");
            return action.StartsWith("remember", StringComparison.Ordinal)
                ? Memory(["save", action.Contains(' ') ? action.Split(' ')[1] : "Fact", key, body]) : Memory(["correct", key, body]);
        }
        var actionMatch = Regex.Match(input, @"^(review|approve|apply|rollback|discard) edit ([a-f0-9-]{36})( high-risk)?$", RegexOptions.IgnoreCase);
        if (actionMatch.Success && Guid.TryParse(actionMatch.Groups[2].Value, out _))
        {
            var action = actionMatch.Groups[1].Value.ToLowerInvariant();
            if (actionMatch.Groups[3].Success && action != "approve") return Explain("High-risk acknowledgement is only used with approve edit.");
            var tokens = new List<string> { "changes", action == "review" ? "preview" : action, actionMatch.Groups[2].Value };
            if (actionMatch.Groups[3].Success) tokens.Add("high-risk");
            return TalkHints(Dispatch(tokens.ToArray()));
        }
        var task = Regex.Match(input, @"^(approve|run|inspect|cancel) task ([a-f0-9-]{36})$", RegexOptions.IgnoreCase);
        if (task.Success && Guid.TryParse(task.Groups[2].Value, out _)) return Dispatch("tasks", task.Groups[1].Value.ToLowerInvariant(), task.Groups[2].Value);
        if (input.Equals("stop screen", StringComparison.OrdinalIgnoreCase)) return Screen(["stop"]);
        if (Starts(input, @"^(look at|check|help (me )?with|analyze) (my |the )?screen\b"))
        {
            if (ScreenSelected()) return Screen(["ask", input]);
            var pendingScreen = Pending("monitor", input);
            return After(Screen(["options"]), result =>
            {
                if (!Current(pendingScreen)) return Explain("Screen request expired; repeat it.");
                if (screenMonitors.Count == 0) return result;
                followUp = pendingScreen;
                return result with { Text = result.Text + "\nWhich monitor? Reply talk <number>. Selection does not capture anything." };
            });
        }
        if (Starts(input, @"^(edit|rewrite|update (the |a )?file|change (the |a )?file)\b"))
        {
            if (Find(id).IsMeeting) return Explain("File editing requires a one-to-one conversation in the original terminal.");
            if (!dispatcher.IsAllowed("changes")) return Explain("Editing is available only in the original terminal. Use talk there; this window cannot edit files.");
            var workspaces = engine.TasksFor(identity.Reference).Workspaces.Where(w => w.Enabled).Select(w => w.Name).ToArray();
            if (workspaces.Length == 0) return Explain("Register a MAXIS workspace before editing. No files changed.");
            return Ask("workspace", input, "Which registered workspace? Reply talk <name>.\n" + string.Join(", ", workspaces));
        }
        if (Starts(input, @"^(analyze|summari[sz]e|select files|upload files|add files|review (the |my )?(files?|documents?)|read (the |my )?(files?|documents?))\b"))
        {
            if (Find(id).IsMeeting) return Explain("Use a one-to-one conversation for private file analysis; meeting grants are not inferred.");
            var c = Find(id); var agent = c.Participants.Single(p => p.Kind == ParticipantKind.Program);
            if (Starts(input, @"^(select|upload|add) files\b") || !engine.ContextFor(agent.Id, identity.SessionId).Any(d => d.Project.Equals(c.ProjectScope, StringComparison.OrdinalIgnoreCase)))
                return Ask("files", input, $"No files are granted to {agent.Name} in project {c.ProjectScope}. Reply talk select files to choose temporary files and grant them to this agent, or talk cancel.");
            return Analysis(input);
        }
        if (Starts(input, @"^(build|test|run tests|plan|propose|check (the )?(resources|firewall|defender)|inspect workspace|notify|inventory)\b"))
        {
            if (Find(id).IsMeeting) return Send(id, input, MessageOrigin.Terminal);
            if (Starts(input, @"^(build|test|run tests)\b"))
            {
                var workspaces = engine.TasksFor(identity.Reference).Workspaces.Where(w => w.Enabled).Select(w => w.Name).ToArray();
                if (workspaces.Length == 0) return Explain("Register a MAXIS workspace before planning a build or test. No task created.");
                if (!workspaces.Any(w => Regex.IsMatch(input, @"\b" + Regex.Escape(w) + @"\b", RegexOptions.IgnoreCase)))
                    return Ask("task-workspace", input, "Which registered workspace should the plan use? Reply talk <name>.\n" + string.Join(", ", workspaces));
            }
            return TalkHints(Assist(["propose", input], repairFormat: true));
        }
        if (Starts(input, @"^(approve|run|apply|delete|remove|execute|nge|transfer|copy|grant|revoke|fix|do (it|that))\b"))
            return Explain("Which operation and target do you mean? I can plan a task or draft a file edit. Approval needs an exact ID: talk approve task <id> or talk approve edit <id>. Nothing executed.");
        if (Starts(input, @"^(how (do|can|should|would|to)|advice\b|help me (understand|learn)|explain\b|what is (the best|a good))"))
            return Send(id, input, MessageOrigin.Terminal);
        return Send(id, input, MessageOrigin.Terminal);
    }

    private CommandResult ContinueTalk(FollowUp p, string input)
    {
        if (p.Kind == "task-workspace")
        {
            var workspace = engine.TasksFor(identity!.Reference).Workspaces.SingleOrDefault(w => w.Enabled && w.Name.Equals(input, StringComparison.OrdinalIgnoreCase));
            return workspace is null ? Ask("task-workspace", p.Request, "Choose a registered workspace, or talk cancel.")
                : TalkHints(Assist(["propose", p.Request + " in registered workspace " + workspace.Name], repairFormat: true));
        }
        if (p.Kind == "monitor")
        {
            if (!int.TryParse(input, out var number) || number < 1 || number > screenMonitors.Count)
                return Ask("monitor", p.Request, "Choose a listed monitor number, or talk cancel.");
            return After(Screen(["monitoring", input]), result => ScreenSelected() && Current(p)
                ? Ask("capture", p.Request, "Capture one frame from the selected monitor for local OCR assistance? Reply talk capture or talk cancel.") : result);
        }
        if (p.Kind == "capture") return input.Equals("capture", StringComparison.OrdinalIgnoreCase)
            ? Screen(["ask", p.Request]) : Ask("capture", p.Request, "Reply talk capture for one frame, or talk cancel.");
        if (p.Kind is "files" or "edit-files")
        {
            if (!input.Equals("select files", StringComparison.OrdinalIgnoreCase))
                return Ask(p.Kind, p.Request, "Reply talk select files to open the picker, or talk cancel.", p.Workspace, p.Target);
            var c = Find(p.Conversation); var agent = p.Kind == "edit-files" ? "MAXIS" : c.Participants.Single(x => x.Kind == ParticipantKind.Program).Name;
            var project = p.Kind == "edit-files" ? p.Workspace : p.Project;
            var selection = Dispatch("context", "add", "-search", "temporary", project, agent);
            if (selection.ContextPicker is not { } picker) return selection;
            return selection with { ContextPicker = paths =>
            {
                if (!Current(p)) return Explain("File selection expired; nothing imported. Repeat the request.");
                var imported = picker(paths);
                if (!imported.Text.StartsWith("Imported ", StringComparison.Ordinal)) return imported;
                followUp = Pending(p.Kind == "edit-files" ? "edit-context" : "analyze", p.Request, p.Workspace, p.Target);
                return imported with { Text = imported.Text + (p.Kind == "edit-files" ? "\nReply talk <context filename or ID> to draft the edit." : "\nReply talk continue to analyze these files, or talk cancel.") };
            }};
        }
        if (p.Kind == "analyze") return input.Equals("continue", StringComparison.OrdinalIgnoreCase) ? Analysis(p.Request)
            : Ask("analyze", p.Request, "Reply talk continue to analyze, or talk cancel.");
        if (p.Kind == "workspace")
        {
            var workspace = engine.TasksFor(identity!.Reference).Workspaces.SingleOrDefault(w => w.Enabled && w.Name.Equals(input, StringComparison.OrdinalIgnoreCase));
            return workspace is null ? Ask("workspace", p.Request, "Choose a registered workspace name, or talk cancel.")
                : Ask("target", p.Request, "Which relative file path in " + workspace.Name + "? Reply talk <relative-path>.", workspace.Name);
        }
        if (p.Kind == "target")
        {
            var docs = engine.ContextFor(engine.ResolveProgram("MAXIS").Id, identity!.SessionId).Where(d => d.Project.Equals(p.Workspace, StringComparison.OrdinalIgnoreCase)).ToArray();
            return docs.Length == 0 ? Ask("edit-files", p.Request, "MAXIS needs explicitly granted context for this workspace. Reply talk select files to choose temporary context for MAXIS, or talk cancel.", p.Workspace, input)
                : Ask("edit-context", p.Request, "Which captured context should MAXIS use? Reply talk <filename or ID>.\n" + string.Join("\n", docs.Select(d => d.Id + " | " + Path.GetFileName(d.SourcePath))), p.Workspace, input);
        }
        if (p.Kind == "edit-context") return TalkHints(Dispatch("changes", "suggest", p.Workspace, p.Target, input, p.Request));
        return Explain("Please repeat your request.");
    }
    private static CommandResult After(CommandResult result, Func<CommandResult, CommandResult> next) => result.BrokerOperation is { } operation
        ? result with { BrokerOperation = async token => next(await operation(token)) } : next(result);
    private CommandResult Analysis(string request)
    {
        var scope = Pending("details", request);
        talkDetails = null;
        return After(Assist(["analyze", request]), result =>
        {
            if (!Current(scope)) return Explain("Analysis expired; the session or context changed.");
            if (result.Presentation?.FirstOrDefault(s => s.Tone == ConversationTone.Agent) is not { } body) return result;
            talkDetails = (scope, result.Text);
            return ConversationPresentation.AgentReply(Contacts(Find(scope.Conversation)), result.Text.Substring(body.Start, body.Length), "Use talk details for sources. File analysis is not added to conversation memory.");
        });
    }
    private static CommandResult TalkHints(CommandResult result) => After(result, r => r with { Text = r.Text
        .Replace("assist submit or assist discard", "talk submit plan or talk discard plan", StringComparison.Ordinal)
        .Replace("Use tasks inspect, tasks approve, then tasks run in the original terminal.", "In the original terminal: talk inspect task <id>, talk approve task <id>, then talk run task <id>.", StringComparison.Ordinal)
        .Replace("Approve: changes approve ", "Approve: talk approve edit ", StringComparison.Ordinal)
        .Replace("Then: changes apply ", "Then: talk apply edit ", StringComparison.Ordinal) });
}
