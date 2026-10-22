namespace Grid.Simulation.Tasks;

public sealed record DevelopmentWorkspace(string Name, string Root, string Project, string Machine, bool Enabled);
public static class DevelopmentPolicy
{
    public static bool IsReal(string capability) => capability is "maxis.inspect" or "maxis.build" or "maxis.test" or "maxis.suggest";
    public static bool Name(string? value) => value is not null && value.Length is >= 1 and <= 32 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    public static bool Parameters(string capability, string argument)
    {
        var parts = argument.Split('|');
        if (!Name(parts[0])) return false;
        return capability switch
        {
            "maxis.build" or "maxis.test" => parts.Length == 1,
            "maxis.inspect" => parts.Length == 2 && Relative(parts[1]),
            "maxis.suggest" => parts.Length == 4 && Relative(parts[1]) && parts[2].Length > 0,
            _ => false
        };
    }
    public static bool Relative(string path) => !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path) &&
        !path.Contains(':') && !path.Contains('|') && !path.Split(['/', '\\']).Any(p => p is ".." or "." or "");
    public static string CheckedPath(string root, string relative = "")
    {
        var fullRoot = Path.GetFullPath(root);
        if (!Path.IsPathFullyQualified(root) || root.StartsWith("\\\\", StringComparison.Ordinal) || fullRoot == Path.GetPathRoot(fullRoot)) throw new ArgumentException("Register a specific workspace directory, not a drive root.");
        if (relative.Length > 0 && !Relative(relative)) throw new ArgumentException("Use a workspace-relative path without traversal, streams or absolute paths.");
        var path = relative.Length == 0 ? fullRoot : Path.GetFullPath(Path.Combine(fullRoot, relative));
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            if (File.Exists(current) || Directory.Exists(current))
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new IOException("Linked/reparse paths are not allowed.");
        }
        return path;
    }
    public static void Validate(IReadOnlyList<DevelopmentWorkspace> workspaces)
    {
        if (workspaces is null || workspaces.Count > 32 || workspaces.Any(w => w is null || !Name(w.Name) || !TaskState.Label(w.Root, 1024) || !Path.IsPathFullyQualified(w.Root) ||
            !Relative(w.Project) || !w.Project.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(w.Machine)) ||
            workspaces.Select(w => w.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != workspaces.Count)
            throw new InvalidDataException("Invalid MAXIS workspace registry.");
    }
}
