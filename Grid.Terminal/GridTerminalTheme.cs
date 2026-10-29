namespace Grid.Terminal;

public enum TerminalRole { Background, Header, Section, Title, Command, Input, Description, Muted, Border, Notice, Success, Error, Approval }
public enum TerminalColorMode { Plain, Console, TrueColor }
public sealed record TerminalStyle(string Hex, ConsoleColor Fallback)
{
    public string Rgb => $"{Convert.ToInt32(Hex[1..3],16)};{Convert.ToInt32(Hex[3..5],16)};{Convert.ToInt32(Hex[5..7],16)}";
}
public sealed record TerminalCapabilities(TerminalColorMode Mode, bool OwnsSurface = false, bool Unicode = true)
{
    public static TerminalCapabilities Detect(bool color, bool ownsSurface = false)
    {
        if (!color || Console.IsOutputRedirected) return new(TerminalColorMode.Plain, ownsSurface);
        var colorterm = Environment.GetEnvironmentVariable("COLORTERM");
        var rgb = colorterm is "truecolor" or "24bit" || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WT_SESSION"));
        return new(rgb ? TerminalColorMode.TrueColor : TerminalColorMode.Console, ownsSurface, Console.OutputEncoding.CodePage == 65001);
    }
}
public static class GridTerminalTheme
{
    public static readonly IReadOnlyDictionary<TerminalRole, TerminalStyle> Palette = new System.Collections.ObjectModel.ReadOnlyDictionary<TerminalRole, TerminalStyle>(new Dictionary<TerminalRole,TerminalStyle>
    {
        [TerminalRole.Background] = new("#101820",ConsoleColor.Black), [TerminalRole.Header] = new("#172C3C",ConsoleColor.DarkBlue),
        [TerminalRole.Section] = new("#234A5F",ConsoleColor.DarkBlue), [TerminalRole.Title] = new("#59D8F3",ConsoleColor.Cyan),
        [TerminalRole.Command] = new("#57D7F1",ConsoleColor.Cyan), [TerminalRole.Input] = new("#FFFFFF",ConsoleColor.White),
        [TerminalRole.Description] = new("#C2CBD4",ConsoleColor.Gray), [TerminalRole.Muted] = new("#8C9FAE",ConsoleColor.DarkGray),
        [TerminalRole.Border] = new("#34505F",ConsoleColor.DarkCyan), [TerminalRole.Notice] = new("#F3C76A",ConsoleColor.Yellow),
        [TerminalRole.Success] = new("#63DB9C",ConsoleColor.Green), [TerminalRole.Error] = new("#F07878",ConsoleColor.Red),
        [TerminalRole.Approval] = new("#C8AD72",ConsoleColor.DarkYellow)
    });
    public static TerminalRole Role(string text)
    {
        var line = text.Trim();
        if(new[] { "ACTIVE", "RUNNING", "READY", "APPLIED", "COMPLETED", "APPROVED" }.Contains(line.ToUpperInvariant())) return TerminalRole.Success;
        if(new[] { "FAILED", "DENIED", "REJECTED", "INTERRUPTED", "TIMEDOUT" }.Contains(line.ToUpperInvariant())) return TerminalRole.Error;
        if(new[] { "IDLE", "UNKNOWN" }.Contains(line.ToUpperInvariant())) return TerminalRole.Muted;
        if(System.Text.RegularExpressions.Regex.IsMatch(line, @"^(?:Error|Failed|Failure|Access denied|Unknown command|.* error:|.* refused:|AI response rejected|Local AI unavailable|SHUTDOWN ERROR|\[ERROR\]|\[FAILED\])", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return TerminalRole.Error;
        if(System.Text.RegularExpressions.Regex.IsMatch(line, @"^(?:Approved|Applied|Saved|Restored|Connected|Completed|Success|Ready|Simulation running|\[SUCCESS\]|\[READY\]|\[ACTIVE\])\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return TerminalRole.Success;
        if(line.StartsWith("Approval",StringComparison.OrdinalIgnoreCase) || line.StartsWith("Approve:") || line.StartsWith("Risk:") || line.StartsWith("Authority level:")) return TerminalRole.Approval;
        if(line.StartsWith("PROJECT GRID") || line is "COMMAND REFERENCE" or "COMMAND TERMINAL" || line.StartsWith("COMMAND / ") || line.StartsWith("GRID7736 //")) return TerminalRole.Title;
        if(line.Length > 0 && line.All(c => "-=+_|─═│┼ ".Contains(c))) return TerminalRole.Border;
        if(line.Contains("# USAGE:")) return TerminalRole.Command;
        if(line.StartsWith("- ") || line.Contains("# EXAMPLE:")) return TerminalRole.Command;
        if(line.StartsWith("[UNKNOWN]") || line.StartsWith("[IDLE]") || line.StartsWith("Sources") || line.StartsWith("Before SHA256") || line.StartsWith("After SHA256")) return TerminalRole.Muted;
        if(line.StartsWith("[INFO]") || line.StartsWith("[SYSTEM]")) return TerminalRole.Title;
        return TerminalRole.Notice;
    }
    public static bool IsSection(string text) => new[] { "SIMULATION CONTROL", "WORLD & POPULATION", "MONITORING & ANALYTICS", "IDENTITY & ADMINISTRATION", "CONVERSATIONS", "SYSTEM & TERMINAL", "EXTENSIONS", "COMMAND OPTIONS" }.Contains(text.Trim());
}
