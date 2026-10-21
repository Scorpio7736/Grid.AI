namespace Grid.Terminal;

/// <summary>Plain-text tables; narrow screens use labeled records. No ANSI or hidden truncation.</summary>
public static class TerminalTable
{
    public static int Width
    {
        get { try { return Console.IsOutputRedirected ? 120 : Math.Clamp(Console.WindowWidth - 1, 20, 239); } catch (IOException) { return 120; } }
    }
    public static string Safe(string? s) => new((s ?? "").Select(c => c is >= ' ' and <= '~' ? c : '?').ToArray());
    public static string Render(string[] headers, IEnumerable<string[]> values, int? viewport = null)
    {
        var width = Math.Clamp(viewport ?? Width, 20, 239);
        var rows = values.Select(r => r.Select(Safe).ToArray()).ToArray();
        if (headers.Length == 0 || rows.Any(r => r.Length != headers.Length)) throw new ArgumentException("Table shape mismatch.");
        headers = headers.Select(Safe).ToArray();
        if (rows.Length == 0) return "No entries.";
        var sizes = headers.Select((h, i) => Math.Min(40, Math.Max(h.Length, rows.Max(r => r[i].Length)))).ToArray();
        var lines = new List<string>();
        IEnumerable<string> Wrap(string text, int limit)
        {
            if (text.Length == 0) { yield return ""; yield break; }
            for (var start = 0; start < text.Length; start += limit) yield return text.Substring(start, Math.Min(limit, text.Length - start));
        }
        if (headers.Length * 12 + (headers.Length - 1) * 3 > width)
        {
            foreach (var row in rows)
            {
                for (var i = 0; i < headers.Length; i++) lines.AddRange(Wrap(headers[i] + ": " + row[i], width - 2).Select(line => "| " + line));
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
            var cells = row.Select((text, i) => Wrap(text, sizes[i]).ToArray()).ToArray();
            for (var line = 0; line < cells.Max(c => c.Length); line++)
                lines.Add(string.Join(" | ", cells.Select((c, i) => (line < c.Length ? c[line] : "").PadRight(sizes[i]))).TrimEnd());
        }
        Add(headers); lines.Add(string.Join("-+-", sizes.Select(n => new string('-', n))));
        foreach (var row in rows) Add(row);
        return string.Join(Environment.NewLine, lines);
    }
}
