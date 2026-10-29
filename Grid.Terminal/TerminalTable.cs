namespace Grid.Terminal;

/// <summary>Plain-text tables; narrow screens use labeled records. No ANSI or hidden truncation.</summary>
public static class TerminalTable
{
    public static int Width
    {
        get { try { return Console.IsOutputRedirected ? 120 : Math.Clamp(Console.WindowWidth - 1, 20, 239); } catch (IOException) { return 120; } }
    }
    public static string Safe(string? s) => new(TerminalLayout.Plain(s ?? "").Select(c => char.IsControl(c) ? '?' : c).ToArray());
    public static string Render(string[] headers, IEnumerable<string[]> values, int? viewport = null, bool wrapWords = false)
    {
        var width = Math.Clamp(viewport ?? Width, 20, 239);
        var rows = values.Select(r => r.Select(Safe).ToArray()).ToArray();
        if (headers.Length == 0 || rows.Any(r => r.Length != headers.Length)) throw new ArgumentException("Table shape mismatch.");
        headers = headers.Select(Safe).ToArray();
        if (rows.Length == 0) return "No entries.";
        var sizes = headers.Select((h, i) => Math.Min(40, Math.Max(TerminalLayout.Width(h), rows.Max(r => TerminalLayout.Width(r[i]))))).ToArray();
        var lines = new List<string>();
        if (headers.Length * 12 + (headers.Length - 1) * 3 > width)
        {
            foreach (var row in rows)
            {
                for (var i = 0; i < headers.Length; i++) lines.AddRange(TerminalLayout.Wrap(headers[i] + ": " + row[i], width - 2).Select(line => "| " + line));
                lines.Add("");
            }
            return string.Join(Environment.NewLine, lines).TrimEnd();
        }
        while (sizes.Sum() + (sizes.Length - 1) * 3 > width)
        {
            var largest = Array.IndexOf(sizes, sizes.Max()); sizes[largest]--;
        }
        void Add(string[] row)
        {
            var cells = row.Select((text, i) => (wrapWords ? TerminalLayout.Paragraph(text, sizes[i]) : TerminalLayout.Wrap(text, sizes[i])).DefaultIfEmpty("").ToArray()).ToArray();
            for (var line = 0; line < cells.Max(c => c.Length); line++)
                lines.Add(string.Join(" | ", cells.Select((c, i) => TerminalLayout.Pad(line < c.Length ? c[line] : "", sizes[i]))).TrimEnd());
        }
        Add(headers); lines.Add(string.Join("-+-", sizes.Select(n => new string('-', n))));
        foreach (var row in rows) Add(row);
        return string.Join(Environment.NewLine, lines);
    }
}
