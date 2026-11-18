namespace Grid.Terminal;

public static class GridTerminalRenderer
{
    public static void Segment(TextWriter output, string text, TerminalRole role, TerminalCapabilities capabilities, TerminalRole? background = null)
    {
        if(capabilities.Mode == TerminalColorMode.Plain) { output.Write(text); return; }
        if(capabilities.Mode == TerminalColorMode.TrueColor)
        {
            output.Write("\x1b[38;2;" + GridTerminalTheme.Palette[role].Rgb + "m");
            var back = background ?? (capabilities.OwnsSurface ? TerminalRole.Background : (TerminalRole?)null);
            if(back is { } b) output.Write("\x1b[48;2;" + GridTerminalTheme.Palette[b].Rgb + "m");
            try { output.Write(text); } finally { output.Write("\x1b[39;49m"); }
            return;
        }
        var previous = Console.ForegroundColor; var oldBackground = Console.BackgroundColor;
        try
        {
            Console.ForegroundColor = GridTerminalTheme.Palette[role].Fallback;
            if(background is { } b) Console.BackgroundColor = GridTerminalTheme.Palette[b].Fallback;
            else if(capabilities.OwnsSurface) Console.BackgroundColor = GridTerminalTheme.Palette[TerminalRole.Background].Fallback;
            output.Write(text);
        }
        finally { Console.ForegroundColor = previous; Console.BackgroundColor = oldBackground; }
    }
    public static void Text(TextWriter output, string text, TerminalCapabilities capabilities)
    {
        var lines = text.Split('\n');
        for(var i=0;i<lines.Length;i++)
        {
            var line = lines[i]; var trimmed = line.Trim();
            if(GridTerminalTheme.IsSection(trimmed)) Segment(output, line, TerminalRole.Input, capabilities, TerminalRole.Section);
            else if(trimmed.StartsWith("GRID.AI")) Segment(output, line, TerminalRole.Title, capabilities, TerminalRole.Header);
            else if(line.Contains(" | "))
            {
                var cells = line.Split(" | ");
                for(var c=0;c<cells.Length;c++) { if(c>0) Segment(output," | ",TerminalRole.Border,capabilities); var header = cells[0].Trim() is "COMMAND" or "ID" or "Name" or "Key";
                    var semantic = GridTerminalTheme.Role(cells[c]);
                    Segment(output,cells[c], header ? TerminalRole.Title : semantic is TerminalRole.Success or TerminalRole.Error or TerminalRole.Muted ? semantic : c==0 ? TerminalRole.Command : cells.Length == 3 && c==2 ? TerminalRole.Description : TerminalRole.Input,capabilities); }
            }
            else if(line.Contains(": ") && GridTerminalTheme.Role(line) is not (TerminalRole.Error or TerminalRole.Success or TerminalRole.Command))
            {
                var colon = line.IndexOf(": ",StringComparison.Ordinal);
                Segment(output,line[..(colon+2)],GridTerminalTheme.Role(line)==TerminalRole.Approval ? TerminalRole.Approval : TerminalRole.Muted,capabilities);
                Segment(output,line[(colon+2)..],TerminalRole.Input,capabilities);
            }
            else Segment(output,line,GridTerminalTheme.Role(line),capabilities);
            if(i < lines.Length-1) output.Write('\n');
        }
    }
}
