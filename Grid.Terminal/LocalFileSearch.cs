using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Grid.Terminal;

public static class LocalFileSearch
{
    public sealed record Request(string Query, string Location);
    public sealed record Result(IReadOnlyList<string> Paths, bool Incomplete);
    public static Request? Parse(string input)
    {
        var text = Regex.Replace(input.Trim(), @"^(?:(?:hey|hi)\s+MCP[, ]*|MCP[, ]+|(?:can|could|would) you\s+|please\s+)+", "", RegexOptions.IgnoreCase);
        if (Regex.IsMatch(text, @"^find and (?:explain|summarize)\b", RegexOptions.IgnoreCase)) return null;
        var match = Regex.Match(text, @"^(?:find|locate) (.+?) (?:on|in) (?:my |the )?(desktop|documents|downloads)\s*[?.!]*$", RegexOptions.IgnoreCase);
        if (match.Success) return new(match.Groups[1].Value, match.Groups[2].Value.ToLowerInvariant());
        match = Regex.Match(text, @"^search my files for (.+?)[?.!]*$", RegexOptions.IgnoreCase);
        return match.Success ? new(match.Groups[1].Value, "") : null;
    }
    public static string Root(string location) => location switch
    {
        "desktop" => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "documents" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "downloads" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
        _ => ""
    };
    public static Result Search(string root, string query)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return new([], true);
        var term = Regex.Replace(query.Trim(), @"^(?:any|an?|the)\s+|\s+(?:files?|folders?)$", "", RegexOptions.IgnoreCase).Trim().Trim('"');
        var extension = Regex.IsMatch(query, @"\bfiles?\b", RegexOptions.IgnoreCase) && Regex.IsMatch(term, @"^\.?[a-z0-9]{1,8}$", RegexOptions.IgnoreCase);
        if (term.Length == 0) return new([], true);
        var paths = new List<string>(); var pending = new Stack<(string Path, int Depth)>(); pending.Push((Path.GetFullPath(root), 0));
        var timer = Stopwatch.StartNew(); var visited = 0; var incomplete = false;
        while (pending.Count > 0 && paths.Count < 50 && visited < 10000 && timer.Elapsed < TimeSpan.FromSeconds(2))
        {
            var current = pending.Pop();
            try
            {
                if ((File.GetAttributes(current.Path) & FileAttributes.ReparsePoint) != 0) { incomplete = true; continue; }
                foreach (var entry in Directory.EnumerateFileSystemEntries(current.Path))
                {
                    if (++visited > 10000 || timer.Elapsed >= TimeSpan.FromSeconds(2) || paths.Count >= 50) { incomplete = true; break; }
                    var attributes = File.GetAttributes(entry);
                    if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System)) != 0) { incomplete = true; continue; }
                    var directory = (attributes & FileAttributes.Directory) != 0;
                    if (extension ? !directory && Path.GetExtension(entry).Equals("." + term.TrimStart('.'), StringComparison.OrdinalIgnoreCase) : Path.GetFileName(entry).Contains(term, StringComparison.OrdinalIgnoreCase))
                        paths.Add(entry);
                    if (directory && current.Depth < 8) pending.Push((entry, current.Depth + 1));
                    else if (directory) incomplete = true;
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { incomplete = true; }
        }
        return new(paths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray(), incomplete || pending.Count > 0);
    }
}
