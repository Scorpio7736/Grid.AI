using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace Grid.Terminal;

public static class GridBanner
{
    // Exact user-supplied ANSI artwork, gzip-compressed and embedded so startup has no
    // Python, file-system, network or other runtime dependency.
    private const string LogoAsset = "H4sIAAAAAAAACt1ZO27DMAzdewUvPUKAFimCHKVn8B08ePCQIZYAH9AnKRTH+lCkRFFyGzTw0KbS49PjE0W53ffp+vHVv5tPZ34+9+syJh4z6OIN6iwAazoO5UA4RAIK+9Sy6CgE+iFxUxCFC+jf7Hx/5rAug7+8yym1vOfIdRm3wefrp8HRSII0HxWlbYiBRXOxCZYqSuMk4BhMIYgSqeVqfbAGzglpIabHMxCP+WuoBVDZ46oQ6UwQdgS3DJ8sNwS6fX3OcyzIYYwRayTUB8Gm31bl6RXz631dbhbgvlHDMIAmsU+jbQCzkBGHct423+0b+8Tr36TcluOvrTp6+fzdbtWcoauQfNmUIakaQaDj9AltxVUoWCqeaVc3K7l6NSxdcmMASLU2eiOdJNTzZerV9St1GurVTMfH6FX5gTVE2wHdV4N/SIA6Cr4U6ud1c0XJS/lPNfCflssYp1FCqErL0Ita5uaC1jrXBGeXk2yDY6WiHrhILVJqJTuAHcDcwHk4idxVobGhgsmhm5TMj0GP2KR/Lq8XuRvW3pA4O+ebEuk58xgq2ZmSUqcJwzFJZG7+0u2m64wo2mzVeWtTKA6PTlpOsn0l1FXKcupPuzsl9t12vbFAtxjIlY7wtEK7A1Eu7NgAbS5TNH7Ph8NypSKKQTHFDFYpCNHRQlzy+ukG4WUQlVLAGOskXzkzyFs96jXCUNKzoG0jcUg5DOxdKdaBtACqxFECHOwZstexbADWm+v/pr33j4nAugmXwt2OdqHeqZbulfCblny6aPZcPJvC60Mr8tSoo/JSKvY/w38Z/W8cAAA=";
    public static string Logo { get; } = DecodeLogo();
    public static string PlainLogo { get; } = TerminalLayout.Plain(Logo);
    private const int FramedWidth = 78;
    private static readonly string[] GridMark =
    [
        " ██████╗ ██████╗ ██╗██████╗ ",
        "██╔════╝ ██╔══██╗██║██╔══██╗",
        "██║  ███╗██████╔╝██║██║  ██║",
        "██║   ██║██╔══██╗██║██║  ██║",
        "╚██████╔╝██║  ██║██║██████╔╝",
        " ╚═════╝ ╚═╝  ╚═╝╚═╝╚═════╝ "
    ];
    private static readonly string[] AiMark =
    [
        " █████╗ ██╗",
        "██╔══██╗██║",
        "███████║██║",
        "██╔══██║██║",
        "██║  ██║██║",
        "╚═╝  ╚═╝╚═╝"
    ];

    private static string DecodeLogo()
    {
        using var source = new MemoryStream(Convert.FromBase64String(LogoAsset));
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static string Render(int width, double progress = 1, bool? color = null)
    {
        var colored = color ?? !Console.IsOutputRedirected;
        if (width >= 140) return RenderExpanded(width, progress, colored);
        if (width >= FramedWidth) return RenderFramed(width, progress, colored);
        if (width < 26) return GridBrand.Upper;
        var title = progress < 1 ? "GRID.AI // INITIALIZING" : "GRID.AI // LOCAL INTELLIGENCE";
        return string.Join("\n", TerminalLayout.Wrap(title, width)) + "\n" + new string('=', Math.Min(width, 78)) + "\nCOMMAND TERMINAL";
    }

    private static string RenderExpanded(int width, double progress, bool colored)
    {
        var artwork = (colored ? Logo : PlainLogo).Replace("\r", "").Split('\n');
        var logoWidth = artwork.Max(TerminalLayout.Width);
        var revealed = (int)Math.Ceiling(logoWidth * Math.Clamp(progress, 0, 1));
        var indent = new string(' ', Math.Max(0, (width - logoWidth) / 2));
        return string.Join("\n", artwork.Select(line => indent +
            TerminalLayout.Pad(colored ? FitAnsi(line, revealed) : TerminalLayout.Fit(line, revealed), TerminalLayout.Width(line))));
    }

    private static string RenderFramed(int width, double progress, bool colored)
    {
        const string reset = "\u001b[0m";
        string Paint(string value, string code) => colored ? code + value + reset : value;
        string Interior(string content)
        {
            var remaining = FramedWidth - 2 - TerminalLayout.Width(content);
            var left = remaining / 2;
            var right = remaining - left;
            return Paint("║", "\u001b[36m") + new string(' ', left) + content + new string(' ', right) + Paint("║", "\u001b[36m");
        }
        var lines = new List<string>
        {
            Paint("╔" + new string('═', FramedWidth - 2) + "╗", "\u001b[36m"),
            Interior(" ")
        };
        for (var row = 0; row < GridMark.Length; row++)
        {
            var mark = Paint(GridMark[row], "\u001b[96m") + Paint("  ◆  ", "\u001b[37m") + Paint(AiMark[row], "\u001b[36m");
            lines.Add(Interior(mark));
        }
        lines.Add(Interior(" "));
        lines.Add(Interior(Paint("G R I D . A I   //   L O C A L   I N T E L L I G E N C E", "\u001b[90m")));
        lines.Add(Interior(" "));
        lines.Add(Paint("╚" + new string('═', FramedWidth - 2) + "╝", "\u001b[36m"));

        var revealed = (int)Math.Ceiling(FramedWidth * Math.Clamp(progress, 0, 1));
        var indent = new string(' ', Math.Max(0, (width - FramedWidth) / 2));
        return string.Join("\n", lines.Select(line => indent +
            TerminalLayout.Pad(colored ? FitAnsi(line, revealed) : TerminalLayout.Fit(line, revealed), FramedWidth)));
    }

    private static string FitAnsi(string text, int width)
    {
        var result = new StringBuilder();
        var visible = 0;
        for (var i = 0; i < text.Length && visible < width;)
        {
            if (text[i] == '\u001b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                var end = text.IndexOf('m', i + 2);
                if (end < 0) break;
                result.Append(text, i, end - i + 1);
                i = end + 1;
                continue;
            }
            result.Append(text[i++]);
            visible++;
        }
        return result.Append("\u001b[0m").ToString();
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
