using System.Diagnostics;

namespace Grid.Terminal;

public static class GridBanner
{
    public const string Logo = """
        +======================================================================+
        |                                                                      |
        |   ██████╗ ██████╗ ██╗██████╗     ███████╗███████╗██████╗  ██████╗    |
        |  ██╔════╝ ██╔══██╗██║██╔══██╗    ╚════██║╚════██║╚════██╗██╔════╝    |
        |  ██║ ███╗██████╔╝██║██║  ██║        ██╔╝    ██╔╝ █████╔╝███████╗    |
        |  ██║   ██╗██╔══██╗██║██║  ██║       ██╔╝    ██╔╝  ╚═══██╗██╔══██╗    |
        |  ╚██████╔╝██║  ██║██║██████╔╝       ██║     ██║  ██████╔╝╚██████╔╝    |
        |   ╚═════╝ ╚═╝  ╚═╝╚═╝╚═════╝        ╚═╝     ╚═╝  ╚═════╝  ╚═════╝    |
        |                                                                      |
        +======================================================================+
        """;
    public static string Render(int width, double progress = 1)
    {
        var lines = Logo.Replace("\r\n", "\n").Split('\n');
        if (width < lines.Max(l => l.Length)) return width < 26 ? "GRID7736" : "GRID7736 // WELCOME";
        var columns = (int)Math.Ceiling(lines.Max(l => l.Length) * Math.Clamp(progress, 0, 1));
        return string.Join("\n", lines.Select((line, row) => row < 2 || row > 7 ? line :
            new string(line.Select((c, column) => column <= columns || c == '|' ? c : ' ').ToArray())));
    }
}

/// <summary>Elapsed-time transition; never reads a line or waits for Enter after authentication.</summary>
public static class GridLoginAnimation
{
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(5);
    public static bool Enabled(bool interactive, string? term, string? preference) => interactive &&
        !string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(preference, "off", StringComparison.OrdinalIgnoreCase);
    public static bool SkipKey()
    {
        try { if (!Console.IsInputRedirected && Console.KeyAvailable) { Console.ReadKey(true); return true; } }
        catch (Exception e) when (e is IOException or InvalidOperationException) { }
        return false;
    }
    public static async Task RunAsync(TextWriter output, string welcome, bool animate, Func<bool>? clear,
        CancellationToken token, Func<bool>? skip = null, Func<TimeSpan>? elapsed = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null, int? width = null)
    {
        var clock = Stopwatch.StartNew();
        elapsed ??= () => clock.Elapsed;
        delay ??= (duration, cancellation) => Task.Delay(duration, cancellation);
        skip ??= SkipKey;
        bool Clear()
        {
            try { return clear?.Invoke() == true; }
            catch (Exception e) when (e is IOException or InvalidOperationException or PlatformNotSupportedException) { return false; }
        }
        token.ThrowIfCancellationRequested();
        if (animate && Clear())
        {
            while (elapsed() < Duration)
            {
                token.ThrowIfCancellationRequested();
                if (skip()) break;
                var progress = elapsed().TotalSeconds / Duration.TotalSeconds;
                await output.WriteLineAsync(GridBanner.Render(width ?? TerminalTable.Width, progress));
                await output.WriteLineAsync(welcome);
                var fill = Math.Clamp((int)(progress * 20), 0, 20);
                await output.WriteLineAsync("ENTERING GRID [" + new string('=', fill) + new string(' ', 20 - fill) + "]  Any key skips");
                await output.FlushAsync();
                var remaining = Duration - elapsed();
                if (remaining <= TimeSpan.Zero) break;
                await delay(remaining < TimeSpan.FromMilliseconds(125) ? remaining : TimeSpan.FromMilliseconds(125), token);
                if (!Clear()) break;
            }
            Clear();
        }
        token.ThrowIfCancellationRequested();
        await output.WriteLineAsync(GridBanner.Render(width ?? TerminalTable.Width));
        await output.WriteLineAsync(welcome);
        await output.FlushAsync();
    }
}
