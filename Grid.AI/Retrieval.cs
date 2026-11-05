using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Grid.AI;

public sealed record RetrievalDocument(Guid Id, string SourceHash, string Name, string Format, string Text, DateTimeOffset CapturedUtc);
public sealed record RetrievalChunk(Guid DocumentId, string SourceHash, string TextHash, string ChunkHash,
    string Name, string Format, DateTimeOffset CapturedUtc, int Start, int Length, int FirstLine, int LastLine, int? Page, string Text)
{
    public string Citation => $"{DocumentId:N}:{SourceHash[..12]}:{TextHash[..12]}:{Start}-{Start + Length}:{ChunkHash[..12]}";
    public string ChunkingVersion => RetrievalText.Version;
}
public sealed record RetrievalHit(RetrievalChunk Chunk, double Score, bool Overview)
{
    public string RankingVersion => "bm25-tokenhash-cosine-v1";
    public string EmbeddingVersion => "token-hash-128-v1";
}
public interface ILocalTextEmbedding
{
    string Version { get; }
    int Dimensions { get; }
    float[] Embed(string text);
}

/// <summary>Deterministic lexical feature vectors, not learned semantic embeddings.</summary>
public sealed class FeatureHashEmbedding : ILocalTextEmbedding
{
    public string Version => "token-hash-128-v1";
    public int Dimensions => 128;
    public float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        foreach (var group in RetrievalText.Tokens(text).GroupBy(t => t))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(group.Key));
            vector[BinaryPrimitives.ReadUInt32LittleEndian(hash) % Dimensions] += (float)(1 + Math.Log(group.Count())) * ((hash[4] & 1) == 0 ? 1 : -1);
        }
        var length = Math.Sqrt(vector.Sum(v => (double)v * v));
        if (length > 0) for (var i = 0; i < vector.Length; i++) vector[i] /= (float)length;
        return vector;
    }
}

public static class RetrievalText
{
    public const string Version = "chunks-600-overlap-80-v1";
    private static readonly Regex Words = new(@"[\p{L}\p{Nd}]+", RegexOptions.Compiled);
    private static readonly Regex Pages = new(@"(?m)^\[PDF page (\d{1,3}) of \d{1,3}\]\r?$", RegexOptions.Compiled);
    private static readonly HashSet<string> Stop = new("a an the and or of to in on for is it this that what where how please me my file document analyze read about with".Split(' '), StringComparer.Ordinal);
    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    public static string[] Tokens(string text) => Words.Matches(text.ToLowerInvariant()).Select(m => m.Value)
        .Where(w => w.Length is > 1 and <= 80 && !Stop.Contains(w)).ToArray();
    public static IReadOnlyList<RetrievalChunk> Chunk(RetrievalDocument document)
    {
        if (document.Id == Guid.Empty || document.SourceHash is not { Length: 64 } || document.SourceHash.Any(c => !Uri.IsHexDigit(c)) ||
            string.IsNullOrWhiteSpace(document.Name) || document.Name.Length > 4096 || document.Name.Any(char.IsControl) ||
            document.Format is not ("utf8" or "docx-body" or "pdf-pages") || document.Text is null || document.Text.Length > 256 * 1024)
            throw new ArgumentException("Invalid captured retrieval document.");
        var text = document.Text; var hash = Hash(text); var result = new List<RetrievalChunk>();
        var previousStart = 0; var lineNumber = 1;
        var pages = document.Format == "pdf-pages" ? Pages.Matches(text).Select(m => (Offset: m.Index, Page: int.Parse(m.Groups[1].Value))).ToArray() : [];
        for (var start = 0; start < text.Length;)
        {
            var end = Math.Min(text.Length, start + 600);
            var nextPage = pages.FirstOrDefault(p => p.Offset > start);
            if (nextPage.Offset > 0) end = Math.Min(end, nextPage.Offset);
            if (end < text.Length && char.IsHighSurrogate(text[end - 1])) end--;
            var excerpt = text[start..end];
            lineNumber += text.AsSpan(previousStart, start - previousStart).Count('\n'); previousStart = start;
            var firstLine = lineNumber;
            var page = pages.LastOrDefault(p => p.Offset <= start);
            result.Add(new(document.Id, document.SourceHash.ToUpperInvariant(), hash, Hash(excerpt), document.Name, document.Format, document.CapturedUtc,
                start, end - start, firstLine, firstLine + excerpt.Count(c => c == '\n'), page.Page > 0 ? page.Page : null, excerpt));
            if (end == text.Length) break;
            start = end == nextPage.Offset ? end : Math.Max(start + 1, end - 80);
            if (char.IsLowSurrogate(text[start])) start++;
        }
        return result.AsReadOnly();
    }
    public static double Cosine(float[] a, float[] b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Embedding dimensions differ.");
        double dot = 0; for (var i = 0; i < a.Length; i++) dot += a[i] * b[i]; return Math.Clamp(dot, -1, 1);
    }
}
