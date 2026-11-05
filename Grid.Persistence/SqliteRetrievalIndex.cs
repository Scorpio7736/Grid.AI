using System.Text.Json;
using Grid.AI;
using Microsoft.Data.Sqlite;

namespace Grid.Persistence;

/// <summary>Disposable derived index; only trusted, already-authorized captures enter each search.</summary>
public sealed class SqliteRetrievalIndex : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:;Pooling=False");
    private readonly ILocalTextEmbedding embedding = new FeatureHashEmbedding();
    public SqliteRetrievalIndex()
    {
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA temp_store=MEMORY; CREATE TABLE chunks (scope TEXT NOT NULL, id INTEGER PRIMARY KEY, provenance TEXT NOT NULL, vector BLOB NOT NULL); CREATE INDEX chunk_scope ON chunks(scope);";
        command.ExecuteNonQuery();
    }
    public IReadOnlyList<RetrievalHit> Search(string scope, IReadOnlyList<RetrievalDocument> authorized, string query, int limit = 6, bool allowOverview = false)
    {
        if (string.IsNullOrWhiteSpace(scope) || scope.Length > 256 || string.IsNullOrWhiteSpace(query) || query.Length > 1200 || limit is < 1 or > 12 ||
            authorized.Count > 64 || authorized.Any(d => d is null || d.Text is null) || authorized.Sum(d => (long)d.Text.Length) > 1024 * 1024 || authorized.Select(d => d.Id).Distinct().Count() != authorized.Count)
            throw new ArgumentException("Retrieval scope, query or capture budget exceeded.");
        // Rebuild from the current grant-filtered snapshot. Never reuse a previous authorization decision.
        using (var transaction = connection.BeginTransaction())
        {
            using var clear = connection.CreateCommand(); clear.Transaction = transaction; clear.CommandText = "DELETE FROM chunks"; clear.ExecuteNonQuery();
            var count = 0;
            foreach (var document in authorized)
            foreach (var chunk in RetrievalText.Chunk(document))
            {
                if (++count > 4096) throw new ArgumentException("Retrieval exceeds 4,096 chunks; narrow the captured collection.");
                var vector = embedding.Embed(chunk.Text); var bytes = new byte[vector.Length * sizeof(float)]; Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
                using var insert = connection.CreateCommand(); insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO chunks(scope, provenance, vector) VALUES($scope,$provenance,$vector)";
                insert.Parameters.AddWithValue("$scope", scope); insert.Parameters.AddWithValue("$provenance", JsonSerializer.Serialize(chunk)); insert.Parameters.AddWithValue("$vector", bytes);
                insert.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        var rows = new List<(RetrievalChunk Chunk, float[] Vector, string[] Tokens)>();
        using (var select = connection.CreateCommand())
        {
            select.CommandText = "SELECT provenance,vector FROM chunks WHERE scope=$scope ORDER BY id"; select.Parameters.AddWithValue("$scope", scope);
            using var reader = select.ExecuteReader();
            while (reader.Read())
            {
                var chunk = JsonSerializer.Deserialize<RetrievalChunk>(reader.GetString(0))!;
                var bytes = (byte[])reader[1]; var vector = new float[embedding.Dimensions]; Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
                if (RetrievalText.Hash(chunk.Text) != chunk.ChunkHash) throw new InvalidDataException("Retrieval chunk integrity check failed.");
                rows.Add((chunk, vector, RetrievalText.Tokens(chunk.Text)));
            }
        }
        var terms = RetrievalText.Tokens(query).Distinct().Take(64).ToArray(); var queryVector = embedding.Embed(query);
        var average = rows.Count == 0 ? 1 : Math.Max(1, rows.Average(r => r.Tokens.Length));
        var frequency = terms.ToDictionary(t => t, t => rows.Count(r => r.Tokens.Contains(t)));
        var ranked = rows.Select(row =>
        {
            double keyword = 0;
            foreach (var term in terms)
            {
                var count = row.Tokens.Count(t => t == term); if (count == 0) continue;
                var idf = Math.Log(1 + (rows.Count - frequency[term] + .5) / (frequency[term] + .5));
                keyword += idf * count * 2.2 / (count + 1.2 * (.25 + .75 * row.Tokens.Length / average));
            }
            // A lexical match is mandatory: hash collisions cannot independently retrieve private content.
            var score = keyword <= 0 ? 0 : .7 * keyword / (1 + keyword) + .2 * Math.Max(0, RetrievalText.Cosine(queryVector, row.Vector)) +
                (row.Chunk.Text.Contains(query, StringComparison.OrdinalIgnoreCase) ? .1 : 0);
            return new RetrievalHit(row.Chunk, score, false);
        }).Where(h => h.Score > 0).OrderByDescending(h => h.Score).ThenBy(h => h.Chunk.DocumentId).ThenBy(h => h.Chunk.Start)
            .GroupBy(h => h.Chunk.DocumentId).SelectMany(g => g.Take(2)).OrderByDescending(h => h.Score).ThenBy(h => h.Chunk.DocumentId).ThenBy(h => h.Chunk.Start).Take(limit).ToArray();
        if (ranked.Length == 0 && allowOverview)
            return rows.Where(r => r.Chunk.Start == 0).OrderByDescending(r => r.Chunk.CapturedUtc).ThenBy(r => r.Chunk.DocumentId)
                .Take(limit).Select(r => new RetrievalHit(r.Chunk, 0, true)).ToArray();
        return ranked;
    }
    public void Dispose() => connection.Dispose();
}
