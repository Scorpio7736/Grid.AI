using System.Globalization;
using Grid.Persistence;

namespace Grid.Terminal;

public static class HistoryCommand
{
    public static void Register(CommandDispatcher commands, WorldStore store)
    {
        const string usage = "history [details] [kind=<kind>] [entity=<guid>] [severity=<level>] [since=<UTC>] [limit=1..500] [offset=0..]";
        commands.Register(new("history", usage, "Query saved history (newest first; pending events appear after autosave).", args =>
        {
            try
            {
                var query = new HistoryQuery();
                var details = false;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var arg in args)
                {
                    if (arg.Equals("details", StringComparison.OrdinalIgnoreCase))
                    { if (details) throw new ArgumentException(usage); details = true; continue; }
                    var pair = arg.Split('=', 2);
                    if (pair.Length != 2 || !seen.Add(pair[0])) throw new ArgumentException(usage);
                    query = pair[0].ToLowerInvariant() switch
                    {
                        "kind" => query with { Kind = pair[1] },
                        "entity" => query with { EntityId = Guid.Parse(pair[1]) },
                        "severity" => query with { Severity = pair[1] },
                        "since" => query with { SinceUtc = DateTimeOffset.ParseExact(pair[1], "O", CultureInfo.InvariantCulture) },
                        "limit" => query with { Limit = int.Parse(pair[1], CultureInfo.InvariantCulture) },
                        "offset" => query with { Offset = int.Parse(pair[1], CultureInfo.InvariantCulture) },
                        _ => throw new ArgumentException(usage)
                    };
                }
                var events = store.QueryHistory(query);
                return new(events.Count == 0 ? "No saved events match." : !details ? TerminalTable.Render(["UTC", "Level", "Kind", "Tick", "Event"], events.Select(e => new[] { e.RecordedAtUtc.ToString("u"), e.Severity, e.Kind, e.Tick.ToString(), (e.EntityName is null ? "" : e.EntityName + ": ") + e.Detail })) + "\nUse history details with the same filters for identifiers/context. More: increase offset or limit." : string.Join(Environment.NewLine, events.Select(e =>
                    $"{e.RecordedAtUtc:O} | {e.Severity} | {e.Kind} | tick {e.Tick} | {e.EntityId} | {Safe(e.EntityName)} | {Safe(e.Detail)} | {Safe(e.ContextJson)}")));
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException or IOException or Microsoft.Data.Sqlite.SqliteException)
            { return new($"History query failed: {Safe(ex.Message)}"); }
        }));
    }

    private static string Safe(string? value) => new((value ?? "").Select(c => char.IsControl(c) ? ' ' : c).ToArray());
}
