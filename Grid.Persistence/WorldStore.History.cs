using Grid.Simulation;

namespace Grid.Persistence;

public sealed record HistoryQuery(string? Kind = null, Guid? EntityId = null, string? Severity = null,
    DateTimeOffset? SinceUtc = null, int Limit = 50, int Offset = 0);

public sealed partial class WorldStore
{
    /// <summary>Newest saved events first. Entity IDs are historical references, not live-resident foreign keys.</summary>
    public IReadOnlyList<GridHistoryEvent> QueryHistory(HistoryQuery? query = null)
    {
        query ??= new();
        if (query.Limit is < 1 or > 500 || query.Offset < 0) throw new ArgumentOutOfRangeException(nameof(query));
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            EnsureNotRecovering();
            var world = Read(DatabasePath).Info.Id;
            using var connection = new SqliteGridDatabase(DatabasePath).OpenExisting();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT EventId,RecordedAtUtc,Tick,ElapsedTicks,Kind,Severity,EntityId,EntityName,Detail,ContextJson
                FROM WorldEvents WHERE WorldId=$world
                AND ($kind IS NULL OR Kind=$kind COLLATE NOCASE)
                AND ($entity IS NULL OR EntityId=$entity)
                AND ($severity IS NULL OR Severity=$severity COLLATE NOCASE)
                AND ($since IS NULL OR RecordedAtUtc >= $since)
                ORDER BY RecordedAtUtc DESC, rowid DESC LIMIT $limit OFFSET $offset
                """;
            command.Parameters.AddWithValue("$world", world.ToString("D"));
            command.Parameters.AddWithValue("$kind", (object?)query.Kind ?? DBNull.Value);
            command.Parameters.AddWithValue("$entity", (object?)query.EntityId?.ToString("D") ?? DBNull.Value);
            command.Parameters.AddWithValue("$severity", (object?)query.Severity ?? DBNull.Value);
            command.Parameters.AddWithValue("$since", query.SinceUtc is { } since ? Utc(since.ToUniversalTime()) : DBNull.Value);
            command.Parameters.AddWithValue("$limit", query.Limit);
            command.Parameters.AddWithValue("$offset", query.Offset);
            var results = new List<GridHistoryEvent>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
                results.Add(new(Id(reader.GetString(0)), ParseUtc(reader.GetString(1)), reader.GetInt64(2),
                    TimeSpan.FromTicks(reader.GetInt64(3)), reader.GetString(4), reader.GetString(5),
                    reader.IsDBNull(6) ? null : Id(reader.GetString(6)), reader.IsDBNull(7) ? null : reader.GetString(7),
                    reader.GetString(8), reader.GetString(9)));
            return results.AsReadOnly();
        }
    }
}
