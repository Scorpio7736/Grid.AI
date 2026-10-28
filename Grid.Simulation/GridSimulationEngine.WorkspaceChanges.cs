using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public FileFolderMatches FindFileFolders(EditSessionReference session, string query)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            RequireAgentActivity();
            if (fileSearchRoots is not null) return FileFolderLookup.Find(query, fileSearchRoots);
            var roots = new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) };
            foreach (var name in new[] { "@desktop", "@documents", "@downloads", "@pictures", "@music", "@videos" })
                try { roots.Add(ResolveFileDestinationRoot(name)); }
                catch (Exception e) when (e is IOException or ArgumentException or UnauthorizedAccessException or System.Security.SecurityException) { }
            roots.AddRange(tasks.Workspaces.Where(w => w.Enabled && w.Machine == Environment.MachineName).Select(w => w.Root));
            return FileFolderLookup.Find(query, roots);
        }
    }

    private string ResolveFileDestinationRoot(string alias)
    {
        var root = alias.ToLowerInvariant() switch
            {
                "@desktop" => desktopFileRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "@documents" => documentsFileRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "@pictures" => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "@music" => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                "@videos" => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "@downloads" => OperatingSystem.IsWindows() ? Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", null) as string ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                _ => alias
            };
        return Environment.ExpandEnvironmentVariables(root);
    }

    public static bool IsFileDestination(string value) => Path.IsPathFullyQualified(value) ||
        new[] { "@desktop", "@documents", "@downloads", "@pictures", "@music", "@videos" }.Contains(value, StringComparer.OrdinalIgnoreCase);
    // Explicit operator file destinations are never build/test workspaces.
    public T WithFileDestination<T>(EditSessionReference session, string alias, Func<DevelopmentWorkspace, string, T> operation)
    {
        if (!IsFileDestination(alias)) return WithDevelopmentWorkspace(session, alias, operation);
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            RequireAgentActivity();
            var root = ResolveFileDestinationRoot(alias);
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) throw new IOException("Destination folder is unavailable. Supply an existing absolute folder path.");
            WorkspaceFileChange.ValidateDestinationRoot(root);
            root = DevelopmentPolicy.CheckedPath(root, fileDestination: true);
            var artifacts = (maxisRuntime ?? throw new InvalidOperationException("Saved file runtime required.")).EditArtifactsRoot;
            var artifactPrefix = artifacts.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (root.Equals(artifacts, StringComparison.OrdinalIgnoreCase) || root.StartsWith(artifactPrefix, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Destination and file recovery artifacts must be separate directories.");
            return operation(new DevelopmentWorkspace(alias, root, "", Environment.MachineName, true), artifacts);
        }
    }

    // Trusted host boundary: holds the same identity lease and engine gate used by task approval.
    public T WithDevelopmentWorkspace<T>(EditSessionReference session, string alias, Func<DevelopmentWorkspace, string, T> operation)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            RequireAgentActivity();
            var workspace = DevelopmentTarget("maxis.inspect", alias + "|review.txt");
            return operation(workspace, maxisRuntime!.EditArtifactsRoot);
        }
    }
}

