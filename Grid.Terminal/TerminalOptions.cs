namespace Grid.Terminal;

public sealed record TerminalOptions(bool Start, bool Ephemeral, bool Recover, string? WorldPath)
{
    public bool NoColor { get; init; }
    public static TerminalOptions Parse(string[] args)
    {
        bool start = false, ephemeral = false, recover = false, noColor = false;
        string? path = null;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var option = args[i].ToLowerInvariant();
            if (!seen.Add(option)) throw new ArgumentException($"Duplicate option: {args[i]}");
            switch (option)
            {
                case "--no-color": noColor = true; break;
                case "--start": start = true; break;
                case "--ephemeral": ephemeral = true; break;
                case "--recover": recover = true; break;
                case "--world":
                    if (++i >= args.Length || string.IsNullOrWhiteSpace(args[i]) || args[i].StartsWith("--"))
                        throw new ArgumentException("--world requires a database file path.");
                    path = args[i]; break;
                default: throw new ArgumentException($"Unknown option: {args[i]}. Use --start, --world <path>, --recover,  --ephemeral, or --no-color.");
            }
        }
        if (ephemeral && (recover || path is not null)) throw new ArgumentException("--ephemeral cannot be combined with --world or --recover.");
        return new(start, ephemeral, recover, path) { NoColor = noColor };
    }
}
