namespace Grid.Terminal;

public static class CommandHelp
{
    public static IReadOnlyList<string> Options(TerminalCommand c) => BaseOptions(c).Concat(WindowCommands.Supports(c.Name) ? new[] { c.Name + " -opninwin" } : Array.Empty<string>()).ToArray();
    private static IReadOnlyList<string> BaseOptions(TerminalCommand c) => c.Name switch {
        "maxis" => ["maxis workspaces", "maxis register <alias> \"absolute-root\" \"relative/project.csproj\"", "maxis revoke <alias>"],
        "mcp" => ["mcp request \"SPECIALIST: demo text; SPECIALIST: demo text\" [request-key]", "mcp plans", "mcp inspect <plan-id>", "mcp brief [plan-id]"],
        "agents" => ["agents", "agents initialize"],
        "tasks" => ["tasks list", "tasks propose SENTINEL sentinel.defender status", "tasks propose SENTINEL sentinel.firewall status", "tasks propose SENTINEL sentinel.events recent", "tasks inspect <id>", "tasks propose <agent-id|name> <capability> \"argument\" [request-key]", "tasks approve <id>", "tasks cancel <id>", "tasks retry <id>", "tasks run"],
        "voice" => ["voice status", "voice devices", "voice microphone <-1|id>", "voice <microphone-id>", "voice language <culture>", "voice listen", "voice review", "voice edit <text>", "voice send", "voice cancel", "voice mute <on|off>", "voice speak", "voice profile [agent-id|name \"voice name\" rate volume]"],
        "help" => ["help", "help -md"],
        "context" => ["context add \"file\" <temporary|persistent> \"project\" [registered-agent-id|name]", "context list [project]", "context add -search [temporary|persistent \"project\" [agent]]", "context select <id|name> [id|name ...]", "context select clear", "context read [id|name ...|-selected]", "context revoke <id|name>", "context remove <id|name>"],
        "login" => ["login", "login master"],
        "programs" => ["programs", "programs -active", "programs -possible", "programs -sleeping", "programs ids"],
        "districts" => ["districts", "districts ids"],
        "resources" => ["resources", "resources details"],
        "history" => ["history", "history details", "history kind=<kind>", "history entity=<guid>", "history severity=<level>", "history since=<UTC>", "history limit=<1..500>", "history offset=<0..>"],
        "switch" => ["switch master", "switch <agent-id|name>"],
        "inspect" => ["inspect program <id|name>", "inspect district <id|name>"],
        _ => [c.Usage]
    };
    public static string Render(TerminalCommand command) => command.Name + "\n" + string.Join("\n", Options(command).Select(option =>
        $"    - {option}\n      # USAGE: {Description(command, option)}")) + $"\n    # EXAMPLE: {Example(command)}";
    private static string Description(TerminalCommand command, string option) => option.EndsWith(" -opninwin") ? "Open permitted data in a separate window; live updates, other data is a snapshot. Combine with supported command arguments." : option switch {
        "help -md" => "Read the bundled Markdown help reference offline in this terminal.",
        "programs -active" => "List freshly observed Running applications.",
        "programs -possible" => "List current and previously known application candidates.",
        "programs -sleeping" => "List freshly observed On call or Sleeping applications.",
        "programs ids" or "districts ids" => "Show full identifiers; programs permits one availability filter too.",
        _ => command.Description
    };
    private static string Example(TerminalCommand c) => c.Name switch {
        "maxis" => "maxis workspaces", "mcp" => "mcp plans", "tasks" => "tasks list",
        "programs" => "programs -active ids", "inspect" => "inspect program MCP", "talk" => "talk status",
        "edit" => "edit MCP occupation \"Local coordinator\"", "switch" => "switch master", "login" => "login",
        "tranexist" => "tranexist \"E:\\GRID7736\"", "copexist" => "copexist \"E:\\GRID7736 Copy\"",
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
