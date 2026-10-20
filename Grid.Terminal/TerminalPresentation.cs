namespace Grid.Terminal;

public static class TerminalPresentation
{
    public static string Help(IReadOnlyList<TerminalCommand> commands)
    {
        var groups = new (string Title, string[] Names)[] {
            ("SIMULATION CONTROL", ["start", "pause", "step", "tick"]),
            ("WORLD & POPULATION", ["status", "city", "districts", "programs", "inspect", "jobs"]),
            ("MONITORING & ANALYTICS", ["live", "behaviors", "activity", "resources", "history"]),
            ("SYSTEM & TERMINAL", ["clear", "help", "nge", "exit"])
        };
        var lines = new List<string> { "PROJECT GRID TERMINAL", "COMMAND REFERENCE", new('=', Math.Min(68, TerminalTable.Width)) };
        var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            var members = group.Names.Select(n => commands.FirstOrDefault(c => c.Name.Equals(n, StringComparison.OrdinalIgnoreCase)))
                .OfType<TerminalCommand>().ToArray();
            if (members.Length == 0) continue;
            lines.Add("\n" + group.Title);
            lines.Add(TerminalTable.Render(["Command", "Description"], members.Select(c => new[] { c.Name, c.Description })));
            listed.UnionWith(members.Select(c => c.Name));
        }
        var extensions = commands.Where(c => !listed.Contains(c.Name)).ToArray();
        if (extensions.Length > 0)
        {
            lines.Add("\nEXTENSIONS");
            lines.Add(TerminalTable.Render(["Command", "Description"], extensions.Select(c => new[] { c.Name, c.Description })));
        }
        lines.Add("\nCOMMAND OPTIONS");
        // Keep exact registered syntax available, including extension usage and history filters.
        lines.Add(string.Join("\n", commands.Select(c => c.Usage)));
        lines.Add("inspect program <id|name>\ninspect district <id|name>\nprograms ids\ndistricts ids");
        lines.Add("resources details: include source, freshness, timestamps and measurement detail.");
        lines.Add("programs -active: Running | -sleeping: On call or Sleeping | -possible: known installed/observed catalog.");
        return string.Join("\n", lines);
    }

    public static string Farewell(bool success, bool persistent, int? width = null)
    {
        var status = !success ? "SHUTDOWN ERROR - review diagnostics above." :
            persistent ? "WORLD SAVED // USER DISCONNECTED" : "EPHEMERAL SESSION // NOT SAVED";
        if ((width ?? TerminalTable.Width) < 78) return $"\nGRID7736 // DISCONNECTED\n\n{status}";
        return """

            +============================================================================+
            |                                                                            |
            |   ______   ______    _____   ______     ______  ______   ______   ______   |
            |  / ____/  / __   \  |_   _| |  __  \   |____  ||____  | |____  | / ____/  |
            | | |  ___ | |__)  |    | |   | |  \  |       / /     / /      / / | |___    |
            | | | |_  ||  _   /     | |   | |   | |      / /     / /    __/ /  |  __ \   |
            | | |__|  || | \  \    _| |_  | |__/  |     / /     / /    |__  \  | |__| |  |
            |  \______/ |_|  \_\  |_____| |______/     /_/     /_/    |_____/  \____/   |
            |                                                                            |
            |============================================================================|
            |                                                                            |
            |                        GRID7736 // DISCONNECTED                            |
            |                                                                            |
            +============================================================================+
            """ + "\n\n" + status;
    }
}
