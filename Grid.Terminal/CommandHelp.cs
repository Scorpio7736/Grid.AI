namespace Grid.Terminal;

public static class CommandHelp
{
    public static IReadOnlyList<string> Options(TerminalCommand c) => BaseOptions(c).Concat(c.Name == "tasks" ? Enum.GetNames<Grid.Simulation.Tasks.AgentRole>().SelectMany(a => new[] { "tasks -" + a, "tasks -completed -" + a, "tasks -list -" + a, "tasks list -" + a, "tasks list -archived -" + a, "tasks approve -all -" + a, "tasks run -all -" + a, "tasks clear -all -" + a }) : Array.Empty<string>()).Concat(WindowCommands.Supports(c.Name) ? new[] { c.Name + " -opninwin" } : Array.Empty<string>()).ToArray();
    private static IReadOnlyList<string> BaseOptions(TerminalCommand c) => c.Name switch {
        "transcript" => ["transcript", "transcript --details", "transcript <conversation-id> --details"],
        "ai" => ["ai status", "ai test", "ai diagnostics", "ai authorization", "ai diagnostics details", "ai diagnostics clear", "ai profiles", "ai personas", "ai personas MAXIS v1", "ai personas MAXIS v2", "ai personas MAXIS rollback", "ai storage", "ai registry list", "ai registry acceptance", "ai registry device", "ai registry register-baseline", "ai registry verify", "ai registry import \"absolute-manifest.json\"", "ai registry rollback <id>", "ai registry force-lite on", "ai registry force-lite off"],
        "grid" => ["grid -start", "grid -pause", "grid -copyto -select", "grid -transferto -select","grid -copyto \"location\"", "grid -transferto \"location\""],
        "connect" => ["connect", "connect <program-id|name>", "connect -inline [program-id|name]"],
        "resume" => ["resume <conversation-id>", "resume -inline <conversation-id>"],
        "talk" => ["talk participants", "talk invite MAXIS", "talk invite someone for code debugging", "talk accept invitation", "talk decline invitation", "talk remove participant MAXIS", "talk confirm removal","talk <message>", "talk analyze my document", "talk select files", "talk continue", "talk details", "talk look at my screen", "talk capture", "talk cancel", "talk submit plan", "talk discard plan", "talk approve task <id>", "talk run task <id>", "talk edit a file", "talk approve edit <id> [high-risk]", "talk apply edit <id>", "talk rollback edit <id>", "talk project <name>", "talk @MAXIS <message>", "talk @JN <message>"],
        "typewriter" => ["typewriter on", "typewriter off"],
        "volt" => ["volt status"],
        "relay" => ["relay inbox", "relay inspect <notification-reference>", "relay deliveries", "relay ack <notification-reference>"],
        "jn" => ["jn scopes", "jn register <alias> \"absolute-file-or-directory\"", "jn revoke <alias>", "jn quota", "jn quota inspect <proposal-reference>", "jn quota approve <proposal-reference>"],
        "changes" => ["changes generate <scope> <file.docx|file.pdf> <title> <body>", "changes image <destination-scope> <file.png> <source-scope> <source.png>", "changes draft-create <workspace> <relative-file> <instructions>", "changes draft-update <workspace> <relative-file> <instructions>", "changes create <workspace> <relative-file> <content>", "changes read <workspace> <relative-file>", "changes update <workspace> <relative-file> <content>", "changes delete <workspace> <relative-file>", "changes recover <workspace> <change-id>", "changes stage <workspace> <relative-file> <context-id|name>", "changes suggest <workspace> <relative-file> <context-id|name> <request>", "changes list", "changes preview <id>", "changes approve <id> [high-risk]", "changes apply <id>", "changes rollback <id>", "changes discard <id>", "changes verify <id> <build|test>"],
        "maxis" => ["maxis workspaces", "maxis register <alias> \"absolute-root\" \"relative/project.csproj\"", "maxis revoke <alias>"],
        "mcp" => ["mcp plan <request>", "mcp authority", "mcp capabilities", "mcp run <plan-id>", "mcp repeat <plan-id>", "mcp cancel <plan-id>","mcp request \"SPECIALIST: demo text; SPECIALIST: demo text\" [request-key]", "mcp plans", "mcp ledger <plan-id>", "mcp inspect <plan-id>", "mcp brief [plan-id]"],
        "screen" => ["screen options", "screen monitoring <id>", "screen ask <request>", "screen ask @MAXIS <request>", "screen status", "screen stop"],
        "assist" => ["assist advice <request>", "assist analyze <request>", "assist propose <request>", "assist submit", "assist discard"],
        "memory" => ["memory list", "memory history", "memory forget-history", "memory scope <project>", "memory save preference <key> <text>", "memory save decision <key> <text>", "memory save fact <key> <text>", "memory save unfinished <key> <text>", "memory correct <key> <text>", "memory forget <key>", "memory save-summary <context-id> <key> <summary> --retain", "memory save-outcome <task-id> <key> --retain", "memory grant <key> <agent> <project>", "memory revoke <key> <agent> <project>", "memory expiry <key> <days|never>"],
        "meetings" => ["meetings MCP MAXIS JN", "meetings MCP MAXIS -inline"],
        "agent" => ["agent meetings MCP MAXIS JN", "agent meetings MCP MAXIS -inline"],
        "agents" => ["agents"],
        "tasks" => ["tasks", "tasks -completed", "tasks -list", "tasks deny <id> \"reason\"","tasks list", "tasks list -archived", "tasks approve -all", "tasks run -all", "tasks clear -all", "tasks propose SENTINEL sentinel.defender status", "tasks propose SENTINEL sentinel.firewall status", "tasks propose SENTINEL sentinel.events recent", "tasks inspect <id>", "tasks propose <agent-id|name> <capability> \"argument\" [request-key]", "tasks approve <id>", "tasks cancel <id>", "tasks retry <id>", "tasks run", "tasks run <id>"],
        "voice" => ["voice status", "voice devices", "voice microphone <-1|id>", "voice <microphone-id>", "voice language <culture>", "voice listen", "voice review", "voice edit <text>", "voice send", "voice cancel", "voice mute <on|off>", "voice speak", "voice profile [agent-id|name \"voice name\" rate volume]"],
        "help" => ["help", "help -md"],
        "context" => ["context search \"project\" \"query\"", "context add \"file\" <temporary|persistent> \"project\" [registered-agent-id|name]", "context list [project]", "context add -search [temporary|persistent \"project\" [agent]]", "context select <id|name> [id|name ...]", "context select clear", "context read [id|name ...|-selected]", "context revoke <id|name>", "context remove <id|name>"],
        "login" => ["login", "login master"],
        "programs" => ["programs", "programs -active", "programs -possible", "programs -sleeping", "programs ids"],
        "districts" => ["districts", "districts ids"],
        "resources" => ["resources", "resources details"],
        "history" => ["history", "history details", "history kind=<kind>", "history entity=<guid>", "history severity=<level>", "history since=<UTC>", "history limit=<1..500>", "history offset=<0..>"],
        "switch" => ["switch master", "switch <agent-id|name>"],
        "inspect" => ["inspect program <id|name>", "inspect district <id|name>"],
        _ => [c.Usage]
    };
    public static string Render(TerminalCommand command)
    {
        var width = TerminalTable.Width;
        var options = Options(command).Select(option => new[] { command.Name,
            option.StartsWith(command.Name + " ", StringComparison.Ordinal) ? option[(command.Name.Length + 1)..] : option == command.Name ? "" : option,
            Description(command, option) });
        return "COMMAND / " + command.Name + "\n" +
            string.Join("\n", TerminalLayout.Paragraph("# USAGE: " + command.Usage, width)) + "\n" +
            TerminalTable.Render(["COMMAND", "OPTIONS", "DESCRIPTION"], options, width, wrapWords: true) + "\n" +
            string.Join("\n", TerminalLayout.Paragraph("# EXAMPLE: " + Example(command), width));
    }
    private static string Description(TerminalCommand command, string option) => option.EndsWith(" -opninwin") ? "Open permitted data in a separate window; live updates, other data is a snapshot. Combine with supported command arguments." : option switch {
        "changes generate <scope> <file.docx|file.pdf> <title> <body>" => "Stage a bounded plain report and native/HTML previews for visual review and exact approval.",
        "changes image <destination-scope> <file.png> <source-scope> <source.png>" => "Validate and stage a plain RGB/RGBA PNG from an explicitly scoped source.",
        "mcp ledger <plan-id>" => "Show saved child-task status, attempts, timestamps and request/result fingerprints.",
        "changes stage <workspace> <relative-file> <context-id|name>" => "Draft a complete replacement/new file from captured context in the workspace project. No files written.",
        "changes suggest <workspace> <relative-file> <context-id|name> <request>" => "Ask local MAXIS AI for a draft using explicitly granted context. Review and approval remain mandatory.",
        "changes preview <id>" => "Show the complete before/after content and hashes, or applied state and recovery path.",
        "changes approve <id> [high-risk]" => "Approve exact hashes. Deletion and code/configuration require the literal high-risk acknowledgement; nothing is applied yet.",
        "changes apply <id>" => "Apply the approved file with durable backup and stale-source checks.",
        "changes rollback <id>" => "Restore the original, or remove a newly created file, only if unchanged since apply. Backups remain.",
        "changes verify <id> <build|test>" => "Propose a separately approved MAXIS verification task. Does not start a process automatically.",
        "screen options" => "List current monitors; refresh IDs and clear any previous selection. No capture.",
        "screen monitoring <id>" => "Select a listed monitor for this connected agent or meeting. No capture until screen ask.",
        "screen ask <request>" => "Capture one frame from the selected monitor; analyze its OCR text offline. No recording, history or actions.",
        "screen ask @MAXIS <request>" => "Direct one-shot screen assistance to a participant in the current meeting.",
        "screen status" => "Show this terminal's selection and capture state.",
        "screen stop" => "Clear selection and cancel pending screen assistance in this terminal.",
        "help -md" => "Read the bundled Markdown help reference offline in this terminal.",
        "programs -active" => "List freshly observed Running applications.",
        "programs -possible" => "List current and previously known application candidates.",
        "programs -sleeping" => "List freshly observed On call or Sleeping applications.",
        "programs ids" or "districts ids" => "Show full identifiers; programs permits one availability filter too.",
        _ => command.Description
    };
    private static string Example(TerminalCommand c) => c.Name switch {
        "volt" => "volt status", "relay" => "relay inbox", "jn" => "jn quota", "maxis" => "maxis workspaces", "mcp" => "mcp plans", "tasks" => "tasks list",
        "changes" => "changes list",
        "screen" => "screen options",
        "programs" => "programs -active ids", "inspect" => "inspect program MCP", "talk" => "talk status",
        "edit" => "edit MCP occupation \"Local coordinator\"", "switch" => "switch master", "login" => "login",
        "grid" => "grid -copyto \"E:\\GRID7736 Copy\"",
        "tranexist" => "grid -transferto \"E:\\GRID7736\"", "copexist" => "grid -copyto \"E:\\GRID7736 Copy\"",
        "context" => "context list", "connect" => "connect MCP", "history" => "history kind=Edit limit=20", _ => c.Usage
    };
    public static string Markdown()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "docs", "Project_GRID_Terminal_Help.md");
        try { return $"OFFLINE MARKDOWN HELP: {path}\n" + File.ReadAllText(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return $"Error: bundled help is missing or unreadable at {path}. Use help for registered commands."; }
    }
    public static IReadOnlyList<string> Complete(string line, IReadOnlyList<TerminalCommand> commands)
    {
        // Only public metadata/capability-filtered commands; never secret values or arbitrary paths/IDs.
        if (line.Contains('"') || line.Contains('\'')) return [];
        if (!line.Contains(' ')) return commands.Select(c => c.Name).Where(n => n.StartsWith(line, StringComparison.OrdinalIgnoreCase)).Order().ToArray();
        var name = line.Split(' ')[0]; var command = commands.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (command is null) return [];
        return Options(command).Select(o => o.Split(" <")[0].Split(" [")[0].Split(" \"")[0]).Where(o => !o.Contains('[') && !o.Contains('<') && o.StartsWith(line, StringComparison.OrdinalIgnoreCase)).Distinct().Order().ToArray();
    }
}
