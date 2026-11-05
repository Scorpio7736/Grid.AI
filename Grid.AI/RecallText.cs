using System.Text.RegularExpressions;
namespace Grid.AI;

/// <summary>Deterministic lexical helpers. These do not select authorized data or generate facts.</summary>
public static class RecallText
{
    public const string Version = "lexical-recall-v1";
    public static IReadOnlySet<string> Terms(string input, bool history = false) => Regex.Matches(input.ToLowerInvariant(), @"[\p{L}\p{N}]+")
        .Select(m => m.Value).Where(w => w.Length > 3 && !new[] { "what", "that", "this", "with", "have", "does", "about", "your", "please" }.Contains(w) &&
            (!history || !new[] { "remember", "recall", "discuss", "discussed", "conversation", "previous", "earlier" }.Contains(w))).ToHashSet();
    public static int Score(string text, IEnumerable<string> terms) => terms.Count(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));
    public static bool BroadHistoryRequest(string input) => Regex.IsMatch(input, @"\b(remember|recall|discussed|previous|earlier|last time)\b", RegexOptions.IgnoreCase);
    public static string Excerpt(string text, string input)
    {
        var words = Regex.Matches(input, @"[\p{L}\p{N}]+").Select(m => m.Value).Where(w => w.Length > 4);
        var match = words.Select(w => text.IndexOf(w, StringComparison.OrdinalIgnoreCase)).Where(i => i >= 0).DefaultIfEmpty(0).Min();
        var start = Math.Max(0, match - 50); var tail = text[start..];
        return (start > 0 ? "[excerpt] " : "") + (tail.Length <= 240 ? tail : tail[..240] + "… [excerpt]");
    }
    public static IReadOnlyList<T> SummaryExcerpts<T>(IEnumerable<T> messages, string input, Func<T, string> text, Func<T, long> sequence)
    {
        var terms = input.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length >= 4).Take(12).ToArray();
        return messages.OrderByDescending(m => Score(text(m), terms)).ThenByDescending(sequence).Take(2).OrderBy(sequence).ToArray();
    }
}
