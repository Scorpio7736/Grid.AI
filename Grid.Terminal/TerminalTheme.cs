using System.Text;
namespace Grid.Terminal;

public static class TerminalTheme
{
    public static bool SupportsColor(bool noColor, bool redirected, string? term, string? environmentNoColor) =>
        !noColor && !redirected && !string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase) && environmentNoColor is null;
    public static bool Enabled(bool noColor) => SupportsColor(noColor, Console.IsOutputRedirected,
        Environment.GetEnvironmentVariable("TERM"), Environment.GetEnvironmentVariable("NO_COLOR"));
    public static ConsoleColor? Color(string line) => GridTerminalTheme.Palette[GridTerminalTheme.Role(line)].Fallback;
    public static Task PromptAsync(TextWriter writer, string text) => writer is IConversationOutput colored
        ? colored.WriteConversationAsync(text, GridTerminalTheme.Palette[TerminalRole.Title].Fallback) : writer.WriteAsync(text);
    public static void Write(TextWriter writer, string text, bool enabled, bool ownsSurface = false) =>
        GridTerminalRenderer.Text(writer, text, TerminalCapabilities.Detect(enabled, ownsSurface));
}

public sealed class TerminalOutput : TextWriter, IConversationOutput
{
    private readonly TextWriter inner;
    private readonly TerminalCapabilities capabilities;
    public TerminalOutput(TextWriter inner, bool color, TerminalCapabilities? capabilities = null)
    { this.inner = inner; this.capabilities = color ? capabilities ?? TerminalCapabilities.Detect(true) : new(TerminalColorMode.Plain); }
    public override Encoding Encoding => inner.Encoding;
    public Task WriteConversationAsync(string text, ConsoleColor tone)
    {
        var role = tone switch { ConsoleColor.Cyan => TerminalRole.Title, ConsoleColor.White => TerminalRole.Input, ConsoleColor.Red => TerminalRole.Error, _ => TerminalRole.Notice };
        GridTerminalRenderer.Segment(inner, text, role, capabilities); return Task.CompletedTask;
    }
    public override Task WriteAsync(string? value) { GridTerminalRenderer.Text(inner,value ?? "",capabilities); return Task.CompletedTask; }
    public override Task WriteLineAsync(string? value) { GridTerminalRenderer.Text(inner,value ?? "",capabilities); inner.WriteLine(); return Task.CompletedTask; }
    public override Task FlushAsync() => inner.FlushAsync();
}
