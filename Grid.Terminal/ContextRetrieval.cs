using Grid.AI;
using Grid.Communication;
using Grid.Persistence;

namespace Grid.Terminal;

public static class ContextRetrieval
{
    public static IReadOnlyList<RetrievalHit> Search(IReadOnlyList<ContextDocument> authorized, string project, string query, bool overview = false)
    {
        var documents = authorized.Where(d => d.Project.Equals(project, StringComparison.OrdinalIgnoreCase)).Select(d =>
            new RetrievalDocument(d.Id, d.SourceHash, Path.GetFileName(d.SourcePath), d.Format, d.Text, d.CapturedUtc)).ToArray();
        using var index = new SqliteRetrievalIndex();
        return index.Search(RetrievalText.Hash(project.ToUpperInvariant()), documents, query, allowOverview: overview);
    }
    public static string Cite(RetrievalHit hit) => $"{hit.Chunk.Name} | source {hit.Chunk.Citation} | lines {hit.Chunk.FirstLine}-{hit.Chunk.LastLine}" +
        (hit.Chunk.Page is { } page ? $" | captured page marker {page}" : "") + $" | {hit.Chunk.Format} | captured {hit.Chunk.CapturedUtc:O}" +
        (hit.Overview ? " | overview fallback" : "");
}
