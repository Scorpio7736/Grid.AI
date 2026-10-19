namespace Grid.Terminal;

/// <summary>Interactive surface only; kept separate from commands and simulation for testing/extension.</summary>
public interface ILiveTerminal
{
    bool TryGetSize(out int width, out int height);
    bool TryReadKey(out ConsoleKey key);
    void Begin();
    void Draw(IReadOnlyList<string> lines);
    void End();
}

public sealed class ConsoleLiveTerminal : ILiveTerminal
{
    private bool previousCursorVisible;
    private bool active;
    private int previousWidth;
    private int previousHeight;
    private string[] previousLines = [];

    public bool TryGetSize(out int width, out int height)
    {
        width = height = 0;
        if (Console.IsInputRedirected || Console.IsOutputRedirected ||
            string.Equals(Environment.GetEnvironmentVariable("TERM"), "dumb", StringComparison.OrdinalIgnoreCase)) return false;
        width = Math.Min(Console.WindowWidth, 240);
        height = Math.Min(Console.WindowHeight, 100);
        return width >= 60 && height >= 18;
    }

    public bool TryReadKey(out ConsoleKey key)
    {
        key = default;
        if (!Console.KeyAvailable) return false;
        key = Console.ReadKey(intercept: true).Key;
        return true;
    }

    public void Begin()
    {
        previousCursorVisible = OperatingSystem.IsWindows() ? Console.CursorVisible : true;
        active = true;
        Console.CursorVisible = false;
        previousWidth = previousHeight = 0;
        previousLines = [];
    }

    public void Draw(IReadOnlyList<string> lines)
    {
        if (!TryGetSize(out var width, out var height)) throw new InvalidOperationException("Terminal is too small or unavailable.");
        if (width != previousWidth || height != previousHeight)
        {
            Console.Clear();
            previousWidth = width;
            previousHeight = height;
            previousLines = new string[height - 1];
        }
        // Never write the last row/column: avoid wrapping and scrollback growth.
        for (var row = 0; row < height - 1; row++)
        {
            var line = row < lines.Count ? lines[row] : "";
            var fitted = line[..Math.Min(line.Length, width - 1)].PadRight(width - 1);
            if (previousLines[row] == fitted) continue;
            Console.SetCursorPosition(0, row);
            Console.Write(fitted);
            previousLines[row] = fitted;
        }
    }

    public void End()
    {
        if (!active) return;
        active = false;
        try { Console.Clear(); }
        finally { Console.CursorVisible = previousCursorVisible; }
    }
}
