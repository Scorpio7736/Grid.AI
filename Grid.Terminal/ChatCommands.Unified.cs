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
    private CommandResult Explain(string text) => ConversationPresentation.AgentReply("GRID", text) with { AnimateFrom = null };
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
        if (!CommunicationState.ValidContent(text) || string.IsNullOrWhiteSpace(text.Trim().Trim('"', '\''))) return new("Error: Please provide a valid message.");
        if (active is null)
        {
            if (identity?.IsMaster != true) return new("No conversation connected. Use connect [program-id|name], or resume <conversation-id>.");
            var connected = Connect(["MCP"]);
            if (active is null) return connected;
        }
        var id = active!.Value;
        engine.RequireAgentActivity();
        var input = text.Trim();
        if (ResumeSavedPlan(input) is { } resumedPlan) return resumedPlan;
        if (fileRequest?.Change is { HighRisk: true } && Starts(input, @"^(?:I )?approve this code change[.!]?$")) input = "approve high-risk";
        if (TryPlanConversation(input, out var planReply)) return planReply;
        if (Starts(input, @"^(show (?:our |the )?(?:conversation|transcript)|what did we discuss)[.!]?$")) return Transcript([]);
        if (Starts(input, @"^show conversation details[.!]?$")) return Transcript(["--details"]);
        if (Starts(input, @"^(show my tasks|how are my tasks doing|task progress)[.!]?$")) return Dispatch("tasks");
        if (Starts(input, @"^(show my plans|show plan progress)[.!]?$")) return Dispatch("mcp", "brief");
        if (Starts(input, @"^(turn off (?:the )?(?:typewriter|animation)|stop animating)[.!]?$")) return Dispatch("typewriter", "off");
        if (Starts(input, @"^turn on (?:the )?(?:typewriter|animation)[.!]?$")) return Dispatch("typewriter", "on");
        if (Starts(input, @"^listen to my voice[.!]?$")) return Dispatch("voice", "listen");
        if (Starts(input, @"^(approve|apply|undo|cancel) (?:that|it)[.!]?$"))
        {
            if (fileRequest is not null && assistanceDraft is not null)
                return Explain("There is both a file request and a task draft. Specify the file change or the task draft first.");
            if (fileRequest is null)
            {
                if (assistanceDraft is not null && Starts(input, @"^cancel\b")) return Assist(["discard"]);
                return Explain("Which operation do you mean? Describe the file change or task draft you want to review first.");
            }
            input = Regex.Replace(input, @"\b(?:that|it)[.!]?$", "this change", RegexOptions.IgnoreCase);
        }
        var planAction = Regex.Match(input, @"^(run|cancel|inspect|brief) plan ([a-f0-9-]{8,36})$", RegexOptions.IgnoreCase);
        if (planAction.Success && identity?.IsMaster == true)
            return Dispatch("mcp", planAction.Groups[1].Value.ToLowerInvariant(), planAction.Groups[2].Value);
        if (Starts(input, @"^(cancel|never mind|nevermind|cancel that|cancel this change|stop that)[.!]?$"))
        { invitation = null; followUp = null; assistanceDraft = null; assistanceRequest++; CancelFileRequest(); return Explain("Follow-up cancelled. No new action taken; completed changes are not undone."); }
        if (InvitationTalk(input, out var invitationReply)) return invitationReply;
        var progressRequest = Regex.Match(input, @"^@?MCP\s+(progress|plans|brief)(?:\s+([a-f0-9-]{8,36}))?$", RegexOptions.IgnoreCase);
        if (progressRequest.Success) return progressRequest.Groups[2].Success ? Dispatch("mcp", "brief", progressRequest.Groups[2].Value) : Dispatch("mcp", "brief");
        if (Starts(input, @"^creat[.!]?$"))
            return Explain("To draft a file, say: create a file on my desktop called test.md and fill it with 10 random numbers. Review the preview, approve this change, then apply this change. You do not need to create the file manually.");
        if (Starts(input, @"^reate (?:a |the )?file\b"))
            return Explain("Did you mean create file? Resend: c" + input + "\nNothing was created or approved.");
        if (Starts(input, @"^(?:where\b.*\b(?:file|saved?|written|created|put|it)\b|(?:what|show|tell)\b.*\b(?:save location|file location|file path|absolute path)\b)"))
            return FileLocation();
        if (TryFileConversation(input, out var fileReply)) return fileReply;
        if (followUp is { } pending)
        {
            followUp = null;
            if (!Current(pending)) return Explain("The session or scope changed. Please repeat your request; nothing was performed.");
            if (Starts(input, @"^(go back|back|change (the )?(file|target)|choose another file)[.!]?$"))
                return Ask(pending.Kind is "workspace" or "target" or "edit-files" or "edit-context" ? "workspace" : "files",
                    pending.Request, "Choose another target. For a document, say select files; for an edit, give the registered workspace name.");
            if (IsFollowUp(pending, input)) return ContinueTalk(pending, input);
            // A different sentence replaces the pending request; it is never coerced into a target or consent.

        }
        input = Regex.Replace(input, @"^(?:(?:can|could|would) you\s+|please\s+|I (?:want|need) you to\s+)+", "", RegexOptions.IgnoreCase).Trim();
        // Directed meeting turns retain the established participant/recall routing.
        var directedPlan = Regex.Match(input, @"^@?(MCP|MAXIS|JN|VOLT|SENTINEL|RELAY)[,:]?\s+(plan|propose)\b(.*)$", RegexOptions.IgnoreCase);
        if (Find(id).IsMeeting && directedPlan.Success)
        {
            var role = directedPlan.Groups[1].Value.ToUpperInvariant();
            if (!Find(id).Participants.Any(p => p.Kind == ParticipantKind.Program && p.Name.Equals(role, StringComparison.OrdinalIgnoreCase))) return Explain("That agent is not in this meeting. Invite them first.");
            return TalkHints(Assist(["propose", "User directs " + role + ": " + directedPlan.Groups[2].Value + directedPlan.Groups[3].Value], repairFormat: true));
        }
        if (input.StartsWith('@')) return Send(id, input, MessageOrigin.Terminal);
        if (identity?.IsMaster != true)
            return identity is not null && Starts(input, @"^(build|test|plan|edit|approve|run|apply|delete|select files|look at .*screen)\b")
                ? Explain("Access denied. Operational assistance requires human Master Control.") : Send(id, input, MessageOrigin.Terminal);
        if (RepositoryConversation(input) is { } repositoryReply) return repositoryReply;
        if (DirectDevelopmentPlan(input) is { } developmentPlan) return developmentPlan;
        var fileSearch = LocalFileSearch.Parse(input);
        if (fileSearch is not null)
        {
            if (Find(id).IsMeeting) return Explain("Use a one-to-one conversation to search your local folders.");
            if (fileSearch.Location.Length == 0)
                return Explain("Where should I look? For example: find " + fileSearch.Query + " on my desktop, or find " + fileSearch.Query + " in my documents. I search names only; I won't open or change the files.");
            var scope = Pending("file-search", input);
            var root = LocalFileSearch.Root(fileSearch.Location);
            var found = LocalFileSearch.Search(root, fileSearch.Query);
            if (!Current(scope)) return Explain("The session changed during search. Please repeat your request.");
            return Explain((found.Paths.Count == 0 ? "I found no matching names" : "I found these matching paths") +
                " under " + TerminalTable.Safe(root) + ":\n" + string.Join("\n", found.Paths.Select(TerminalTable.Safe)) +
                (found.Incomplete ? "\nSome folders were skipped or a search limit was reached; this is not a complete inventory." : "") +
                "\nI searched file and folder names only. No contents were opened, shared with the model or changed.");
        }
        if (LooksLikePath(input)) return ConfirmFile(Pending("files", "Analyze the selected file."), input);
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
        var remembered = Regex.Match(input, @"^remember (?:that )?(.+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (remembered.Success)
        {
            var body = remembered.Groups[1].Value.Trim();
            var key = "note-" + Grid.AI.ModelAcceptance.Hash(body)[..16].ToLowerInvariant();
            return Memory(["save", "Fact", key, body]);
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
        if (Starts(input, @"^(analyze|summari[sz]e|select files|upload files|add files|find and explain|find and summarize|look up and explain|search (?:my |the |these |uploaded |captured )*(?:files|documents|context)|(?:find|explain|tell me about)\b.*\b(?:in|from) (?:my |the |these |uploaded |captured )*(?:files?|documents?|context)|review (the |my )?(files?|documents?)|read (the |my )?(files?|documents?))\b"))
        {
            if (Find(id).IsMeeting) return Explain("Use a one-to-one conversation for private file analysis; meeting grants are not inferred.");
            var c = Find(id); var agent = c.Participants.Single(p => p.Kind == ParticipantKind.Program);
            if (Starts(input, @"^(select|upload|add) files\b")) return SelectFiles(Pending("files", "Analyze the selected files."));
            if (!engine.ContextFor(agent.Id, identity.SessionId).Any(d => d.Project.Equals(c.ProjectScope, StringComparison.OrdinalIgnoreCase)))
                return Ask("files", input, $"I don't have any documents shared with {agent.Name} in this project yet. Say select files to choose what I should read, or give me a file path. I'll keep your question while you choose. You can cancel or ask something else.");
            return Analysis(input);
        }
        if (Starts(input, @"^(build|test|run tests|plan|propose|check (the )?(resources|firewall|defender)|inspect workspace|notify|inventory)\b"))
        {
            if (Find(id).IsMeeting && !Starts(input, @"^(plan|propose)\b")) return Send(id, input, MessageOrigin.Terminal);
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
        if (p.Kind == "task-clarification")
        {
            if (Starts(input, @"^(approve|apply|run|execute)\b")) return Ask(p.Kind, p.Request, "Answer the clarification first. Nothing is ready for approval or execution.", target: p.Target);
            return TalkHints(Assist(["propose", p.Request + " Clarification: " + p.Target + " User answer: " + input], repairFormat: true));
        }
        if (p.Kind is "target" or "edit-context" or "workspace" or "task-workspace") input = input.Trim().Trim('"', '\'');
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
        if (p.Kind is "files" or "edit-files" or "file-confirm")
        {
            if (Starts(input, @"^(select|upload|add) files[.!]?$")) return SelectFiles(p with { Kind = p.Kind == "edit-files" ? "edit-files" : "files" });
            if (p.Kind == "file-confirm" && Starts(input, @"^(yes|yes please|confirm)[.!]?$")) return SelectFiles(p, [p.Target]);
            if (LooksLikePath(input)) return ConfirmFile(p, input);
            return Ask(p.Kind, p.Request, "Choose files to share first. For example: select files. Or ask another question.", p.Workspace, p.Target);
        }
        if (p.Kind == "analyze")
        {
            if (Starts(input, @"^(select|upload|add) files[.!]?$")) return SelectFiles(Pending("files", p.Request));
            if (LooksLikePath(input)) return ConfirmFile(p, input);
            return Analysis(p.Request);
        }
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
    private static bool LooksLikePath(string input)
    {
        var path = input.Trim().Trim('"', '\'');
        if (Starts(input, @"^(what|how|why|who|can|could|please|explain|write|make|analy[sz]e|review|read|help|tell|is|does|do)\s")) return false;
        return !path.Contains('\n') && (Regex.IsMatch(path, @"^(?:[A-Za-z]:[\\/]|\\\\|\.?\.?[\\/]|Documents[\\/])") ||
            Regex.IsMatch(path, @"^[^<>?*|]+\.(?:pdf|docx|txt|md|html|css|cs|json)[""']?$", RegexOptions.IgnoreCase));
    }
    private bool IsFollowUp(FollowUp p, string input)
    {
        if (p.Kind == "task-clarification") return true;
        if (Starts(input, @"^(hello|hi|thanks|thank you)[.!?]?$")) return false;
        if (Starts(input, @"^(select|upload|add) files[.!]?$|^(continue(?: to analy[sz]e)?|analy[sz]e(?: the file uploaded| (?:the|my) (?:file|document)s?)?)[.!]?$"))
            return p.Kind is "files" or "file-confirm" or "edit-files" or "analyze";
        if (p.Kind == "file-confirm" && Starts(input, @"^(yes|yes please|confirm)[.!]?$")) return true;
        if ((p.Kind is "files" or "file-confirm" or "edit-files" or "target" or "analyze") && LooksLikePath(input)) return true;
        if (p.Kind == "monitor") return int.TryParse(input, out _);
        if (p.Kind == "capture") return input.Equals("capture", StringComparison.OrdinalIgnoreCase);
        if (p.Kind is "workspace" or "task-workspace")
            return engine.TasksFor(identity!.Reference).Workspaces.Any(w => w.Enabled && w.Name.Equals(input, StringComparison.OrdinalIgnoreCase)) || !input.Contains(' ');
        if (p.Kind == "target") return !input.Contains(' ');
        if (p.Kind == "edit-context") return Guid.TryParse(input, out _) || LooksLikePath(input);
        return false;
    }
    private CommandResult ConfirmFile(FollowUp p, string path)
    {
        if (Find(p.Conversation).IsMeeting) return Explain("Use a one-to-one conversation for private file analysis.");
        var agent = Find(p.Conversation).Participants.Single(x => x.Kind == ParticipantKind.Program).Name;
        var target = path.Trim().Trim('"', '\'');
        return Ask("file-confirm", p.Request, $"Import {TerminalTable.Safe(target)} as temporary context for {agent} in {p.Project}, then analyze it? Say yes to confirm, select files to choose another file, or cancel. Nothing has been read yet.", target: target);
    }
    private CommandResult SelectFiles(FollowUp p, IReadOnlyList<string>? supplied = null)
    {
        var c = Find(p.Conversation);
        var agent = p.Kind == "edit-files" ? "MAXIS" : c.Participants.Single(x => x.Kind == ParticipantKind.Program).Name;
        var project = p.Kind == "edit-files" ? p.Workspace : p.Project;
        var selection = Dispatch("context", "add", "-search", "temporary", project, agent);
        if (selection.ContextPicker is not { } picker) return selection;
        followUp = p;
        var used = 0;
        CommandResult Complete(IReadOnlyList<string> paths)
        {
            if (Interlocked.Exchange(ref used, 1) != 0 || !Current(p) || !ReferenceEquals(followUp, p))
                return Explain("File selection expired; nothing imported. Repeat the request.");
            var imported = picker(paths);
            if (!imported.Text.StartsWith("Imported ", StringComparison.Ordinal))
            {
                followUp = Pending(p.Kind, p.Request, p.Workspace, p.Target);
                return imported with { Text = imported.Text + "\nYour request is retained. Try select files again, give another path, or cancel." };
            }
            if (p.Kind == "edit-files") return Ask("edit-context", p.Request,
                imported.Text + "\nWhich captured context should MAXIS use? Give its filename or ID.", p.Workspace, p.Target);
            return Analysis(p.Request);
        }
        return supplied is null ? selection with {
            Text = $"Select files to share temporarily with {agent} in project {project}. Selecting confirms this grant and starts analysis; Cancel imports nothing.",
            ContextPicker = Complete } : Complete(supplied);
    }
    private static CommandResult After(CommandResult result, Func<CommandResult, CommandResult> next) => result.BrokerOperation is { } operation
        ? result with { BrokerOperation = async token => next(await operation(token)) } : next(result);
    private CommandResult Analysis(string request)
    {
        var scope = Pending("analyze", request);
        followUp = scope;
        talkDetails = null;
        var analysis = Assist(["analyze", request]);
        if (analysis.BrokerOperation is { } operation)
            analysis = analysis with { BrokerOperation = token => Current(scope) && ReferenceEquals(followUp, scope)
                ? operation(token) : Task.FromResult(Explain("Analysis was replaced, cancelled or expired.")) };
        return After(analysis, result =>
        {
            if (!Current(scope)) return Explain("Analysis expired; the session or context changed.");
            if (!ReferenceEquals(followUp, scope)) return Explain("Analysis was replaced or cancelled.");
            if (result.Presentation?.FirstOrDefault(s => s.Tone == ConversationTone.Agent) is not { } body)
                return result with { Text = result.Text + "\nYour analysis request is retained. Say continue to retry, or ask something else." };
            followUp = null;
            talkDetails = (scope, result.Text);
            return ConversationPresentation.AgentReply(Contacts(Find(scope.Conversation)), result.Text.Substring(body.Start, body.Length), "Say details to see sources. File analysis is not added to conversation memory.");
        });
    }
    private static CommandResult TalkHints(CommandResult result) => After(result, r => r with { Text = r.Text
        .Replace("assist submit or assist discard", "talk submit plan or talk discard plan", StringComparison.Ordinal)
        .Replace("Use tasks inspect, tasks approve, then tasks run in the original terminal.", "In the original terminal: talk inspect task <id>, talk approve task <id>, then talk run task <id>.", StringComparison.Ordinal)
        .Replace("Approve: changes approve ", "Approve: talk approve edit ", StringComparison.Ordinal)
        .Replace("Then: changes apply ", "Then: talk apply edit ", StringComparison.Ordinal) });
}
