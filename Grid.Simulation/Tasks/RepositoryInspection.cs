using System.Security.Cryptography;
using System.Text;

namespace Grid.Simulation.Tasks;

public sealed record RepositoryText(string Path, string Sha256, string Text);
public sealed record RepositoryCapture(string Workspace, IReadOnlyList<string> Files, bool Incomplete,
    IReadOnlyList<RepositoryText> Instructions);
public sealed record RepositorySearchHit(string Path, string Sha256, int Line, string Text);
public sealed record RepositorySearchCapture(IReadOnlyList<RepositorySearchHit> Hits, bool Incomplete);

/// <summary>Bounded local discovery. Captured text is data and cannot grant execution authority.</summary>
public static class RepositoryInspection
{
    public static RepositorySearchCapture Search(DevelopmentWorkspace workspace, string query)
    {
        if (!TaskState.Label(query, 120)) throw new ArgumentException("Use 1–120 printable characters for a literal source search.");
        var capture = Capture(workspace); var hits = new List<RepositorySearchHit>();
        var incomplete = capture.Incomplete || capture.Files.Count > 64; var total = 0;
        foreach (var relative in capture.Files.Take(64))
        {
            byte[]? bytes;
            try { bytes = WorkspaceFileChange.Read(WorkspaceFileChange.Target(workspace.Root, relative)); }
            catch (IOException) { incomplete = true; continue; }
            if (bytes is null) continue;
            total += bytes.Length; if (total > 512 * 1024) { incomplete = true; break; }
            var lines = WorkspaceFileChange.Decode(bytes).Split('\n');
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
                hits.Add(new(relative, hash, i + 1, lines[i].Length <= 500 ? lines[i] : lines[i][..500] + " [line truncated]"));
                if (hits.Count == 20) return new(hits, true);
            }
        }
        return new(hits, incomplete);
    }
    private static readonly HashSet<string> Skipped = new(StringComparer.OrdinalIgnoreCase)
        { "bin", "obj", "node_modules", "packages", "TestResults", "portable", "workspace-edits" };

    public static IReadOnlyList<RepositoryText> Instructions(string root, string relative)
    {
        _ = WorkspaceFileChange.Target(root, relative);
        var directories = new List<string> { "" };
        var parent = Path.GetDirectoryName(relative);
        while (!string.IsNullOrEmpty(parent)) { directories.Add(parent); parent = Path.GetDirectoryName(parent); }
        var result = new List<RepositoryText>(); var bytes = 0;
        foreach (var directory in directories.OrderBy(d => d.Length))
        {
            var name = Path.Combine(directory, "AGENTS.md");
            var content = WorkspaceFileChange.Read(DevelopmentPolicy.CheckedPath(root, name));
            if (content is null) continue;
            bytes += content.Length;
            if (bytes > 12000) throw new IOException("Repository instructions exceed the 12,000-byte capture limit; narrow or shorten the instructions before drafting.");
            result.Add(new(name, Convert.ToHexString(SHA256.HashData(content)), WorkspaceFileChange.Decode(content)));
        }
        return result;
    }

    public static RepositoryCapture Capture(DevelopmentWorkspace workspace)
    {
        var pending = new Queue<(string Path, int Depth)>(); pending.Enqueue((workspace.Root, 0));
        var files = new List<string>(); var incomplete = false; var visited = 0;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (pending.Count > 0)
        {
            if (++visited > 256 || timer.Elapsed > TimeSpan.FromSeconds(2)) { incomplete = true; break; }
            var (directory, depth) = pending.Dequeue();
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                if (files.Count >= 256 || pending.Count >= 256 || timer.Elapsed > TimeSpan.FromSeconds(2)) { incomplete = true; break; }
                var name = Path.GetFileName(path);
                if (name.StartsWith('.') || Skipped.Contains(name)) continue;
                FileAttributes attributes;
                try { attributes = File.GetAttributes(path); }
                catch (IOException) { incomplete = true; continue; }
                if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System)) != 0) continue;
                if ((attributes & FileAttributes.Directory) != 0)
                { if (depth < 6) pending.Enqueue((DevelopmentPolicy.CheckedPath(path), depth + 1)); else incomplete = true; continue; }
                var relative = Path.GetRelativePath(workspace.Root, path);
                try { _ = WorkspaceFileChange.Target(workspace.Root, relative); files.Add(relative); }
                catch (ArgumentException) { }
            }
        }
        files.Sort(StringComparer.OrdinalIgnoreCase);
        return new(workspace.Name, files, incomplete, Instructions(workspace.Root, workspace.Project));
    }
}
