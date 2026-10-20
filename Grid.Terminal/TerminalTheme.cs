using System.Text;
namespace Grid.Terminal;

public static class TerminalTheme
{
    public static bool SupportsColor(bool noColor, bool redirected, string? term, string? environmentNoColor) =>
        !noColor && !redirected && !string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase) && environmentNoColor is null;
    public static bool Enabled(bool noColor) => SupportsColor(noColor, Console.IsOutputRedirected,
        Environment.GetEnvironmentVariable("TERM"), Environment.GetEnvironmentVariable("NO_COLOR"));
    public static ConsoleColor? Color(string line)
    {
        if (line.Contains("failed", StringComparison.OrdinalIgnoreCase) || line.Contains("error", StringComparison.OrdinalIgnoreCase)) return ConsoleColor.Red;
        if (new[] { "unknown", "stale", "unavailable", "waiting", "warning", "unsupported" }.Any(w => line.Contains(w, StringComparison.OrdinalIgnoreCase))) return ConsoleColor.Yellow;
        if (new[] { "Running", "On call", "Available", "saved", "Working", "Observing" }.Any(w => line.Contains(w, StringComparison.OrdinalIgnoreCase))) return ConsoleColor.Green;
        if (line.Contains("Back", StringComparison.OrdinalIgnoreCase) || line.StartsWith("GRID") || line.StartsWith("PROJECT") || line.Contains(" | ")) return ConsoleColor.Cyan;
        return null;
    }
    public static void Write(TextWriter writer, string text, bool enabled)
    {
        if (!enabled || Color(text) is not { } color) { writer.Write(text); return; }
        var previous = Console.ForegroundColor;
        try { Console.ForegroundColor = color; writer.Write(text); }
        finally { Console.ForegroundColor = previous; }
    }
}

public sealed class TerminalOutput(TextWriter inner, bool color) : TextWriter
{
    public override Encoding Encoding => inner.Encoding;
    public override Task WriteAsync(string? value) { TerminalTheme.Write(inner, value ?? "", color); return Task.CompletedTask; }
    public override Task WriteLineAsync(string? value)
    {
        foreach (var line in (value ?? "").Replace("\r\n", "\n").Split('\n'))
        { TerminalTheme.Write(inner, line, color); inner.WriteLine(); }
        return Task.CompletedTask;
    }
    public override Task FlushAsync() => inner.FlushAsync();
}
