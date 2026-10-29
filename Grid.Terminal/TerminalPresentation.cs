namespace Grid.Terminal;

public static class TerminalPresentation
{
    public static string Help(IReadOnlyList<TerminalCommand> commands)
    {
        var groups = new (string Title, string[] Names)[] {
            ("SIMULATION CONTROL", ["grid", "step", "tick"]),
            ("WORLD & POPULATION", ["status", "city", "districts", "programs", "inspect", "jobs"]),
            ("MONITORING & ANALYTICS", ["live", "behaviors", "activity", "social", "resources", "history"]),
            ("IDENTITY & ADMINISTRATION", ["login", "credentials", "logout", "whoami", "switch", "self", "register-agent", "edit", "apply-edit", "cancel-edit"]),
            ("CONVERSATIONS", ["assist", "memory", "agent", "connect", "talk", "conversations", "transcript", "resume", "disconnect"]),
            ("SYSTEM & TERMINAL", ["clear", "help", "nge", "exit"])
        };
        var lines = new List<string> { "PROJECT GRID TERMINAL", "COMMAND REFERENCE", new('=', Math.Min(68, TerminalTable.Width)) };
        var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            var members = group.Names.Select(n => commands.FirstOrDefault(c => c.Name.Equals(n, StringComparison.OrdinalIgnoreCase)))
                .OfType<TerminalCommand>().ToArray();
            if (members.Length == 0) continue;
            lines.Add("\n" + TerminalLayout.Pad(group.Title, TerminalTable.Width));
            lines.Add(string.Join("\n\n", members.Select(CommandHelp.Render)));
            listed.UnionWith(members.Select(c => c.Name));
        }
        var extensions = commands.Where(c => !listed.Contains(c.Name)).ToArray();
        if (extensions.Length > 0)
        {
            lines.Add("\n" + TerminalLayout.Pad("EXTENSIONS", TerminalTable.Width));
            lines.Add(string.Join("\n\n", extensions.Select(CommandHelp.Render)));
        }
        lines.Add("\n" + TerminalLayout.Pad("COMMAND OPTIONS", TerminalTable.Width));
        // Keep exact registered syntax available, including extension usage and history filters.
        lines.Add(string.Join("\n", commands.Select(c => c.Usage)));
        if (listed.Contains("inspect")) lines.Add("inspect program <id|name>\ninspect district <id|name>\nprograms ids\ndistricts ids");
        if (listed.Contains("resources")) lines.Add("resources details: include source, freshness, timestamps and measurement detail.");
        if (listed.Contains("programs")) lines.Add("programs -active: Running | -sleeping: On call or Sleeping | -possible: known installed/observed catalog.");
        return string.Join("\n", lines.SelectMany(block => block.Split('\n')).SelectMany(line => TerminalLayout.Wrap(line, TerminalTable.Width)));
    }

    public static string Farewell(bool success, bool persistent, int? width = null)
    {
        var status = !success ? "SHUTDOWN ERROR - review diagnostics above." :
            persistent ? "WORLD SAVED // USER DISCONNECTED" : "EPHEMERAL SESSION // NOT SAVED";
        var banner = GridBanner.Render(width ?? TerminalTable.Width);
        if (banner == "GRID7736 // WELCOME") banner = "GRID7736";
        return "\n" + banner + "\n\nGRID7736 // DISCONNECTED\n\n" + status;
    }
}
