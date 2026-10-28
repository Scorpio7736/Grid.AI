using System.Diagnostics;
namespace Grid.Simulation.Tasks;

public sealed record FileFolderMatches(IReadOnlyList<string> Paths, bool Incomplete);
public static class FileFolderLookup
{
    // Enumerate directory names only; no file contents, links or arbitrary whole-disk scan.
    public static FileFolderMatches Find(string query, IEnumerable<string> roots, int maximumDirectories = 2000)
    {
        var wanted = query.Trim().Replace('\\', '/').Trim('/');
        if (wanted.Length == 0 || wanted.Split('/').Any(p => p is "." or ".." || p.IndexOfAny(['*', '?', ':']) >= 0)) return new([], false);
        var queue = new Queue<(string Path, int Depth)>();
        foreach (var root in roots.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase)) queue.Enqueue((root, 0));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var matches = new List<string>();
        var timer = Stopwatch.StartNew(); var incomplete = false;
        while (queue.Count > 0)
        {
            if (seen.Count >= maximumDirectories || timer.ElapsedMilliseconds > 1500 || matches.Count >= 20) { incomplete = true; break; }
            var (candidate, depth) = queue.Dequeue();
            try
            {
                WorkspaceFileChange.ValidateDestinationRoot(candidate);
                var path = DevelopmentPolicy.CheckedPath(candidate, fileDestination: true);
                if (!seen.Add(path) || !Directory.Exists(path)) continue;
                var normalized = path.Replace('\\', '/').TrimEnd('/');
                if (normalized.EndsWith("/" + wanted, StringComparison.OrdinalIgnoreCase)) matches.Add(path);
                if (depth >= 6) continue;
                foreach (var child in Directory.EnumerateDirectories(path))
                {
                    if (queue.Count + seen.Count >= maximumDirectories || timer.ElapsedMilliseconds > 1500) { incomplete = true; break; }
                    if ((File.GetAttributes(child) & (FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System)) == 0)
                        queue.Enqueue((child, depth + 1));
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException) { }
        }
        return new(matches.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray(), incomplete);
    }
}
