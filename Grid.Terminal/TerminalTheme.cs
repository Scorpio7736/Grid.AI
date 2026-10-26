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
        if (new[] { "error", "failed", "denied", "invalid", "refused" }.Any(w => line.Contains(w, StringComparison.OrdinalIgnoreCase))) return ConsoleColor.Red;
        if (line.Contains('|') || line.Contains("  ") && line.Contains("---") || line.Trim().All(c => c is '=' or '-' or '+' or ' ')) return ConsoleColor.Yellow;
        return ConsoleColor.Cyan;
    }
    public static void Write(TextWriter writer, string text, bool enabled)
    {
        if (!enabled || Color(text) is not { } color) { writer.Write(text); return; }
        var previous = Console.ForegroundColor;
        try { Console.ForegroundColor = color; writer.Write(text); }
        finally { Console.ForegroundColor = previous; }
    }
}

public sealed class TerminalOutput(TextWriter inner, bool color) : TextWriter, IConversationOutput
{
    public override Encoding Encoding => inner.Encoding;
    public Task WriteConversationAsync(string text,ConsoleColor tone)
    {
        if(!color) { inner.Write(text); return Task.CompletedTask; }
        var previous=Console.ForegroundColor;
        try { Console.ForegroundColor=tone; inner.Write(text); }
        finally { Console.ForegroundColor=previous; }
        return Task.CompletedTask;
    }

    public override Task WriteAsync(string? value) { TerminalTheme.Write(inner, value ?? "", color); return Task.CompletedTask; }
    public override Task WriteLineAsync(string? value)
    {
        foreach (var line in (value ?? "").Replace("\r\n", "\n").Split('\n'))
        { TerminalTheme.Write(inner, line, color); inner.WriteLine(); }
        return Task.CompletedTask;
    }
    public override Task FlushAsync() => inner.FlushAsync();
}
