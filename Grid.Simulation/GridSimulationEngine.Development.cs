using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
namespace Grid.Simulation;
public sealed partial class GridSimulationEngine
{
    private MaxisRuntime? maxisRuntime;
    private string? desktopFileRoot;
    private string? documentsFileRoot;
    private IReadOnlyList<string>? fileSearchRoots;
    public void ConfigureMaxisRuntime(string artifactsRoot, string? bundledDotnetRoot = null, string? desktopRoot = null, string? documentsRoot = null, IReadOnlyList<string>? folderSearchRoots = null) { lock(gate) { if (maxisRuntime is not null) throw new InvalidOperationException("MAXIS host already configured."); maxisRuntime = new(artifactsRoot, bundledDotnetRoot); desktopFileRoot = desktopRoot; documentsFileRoot = documentsRoot; fileSearchRoots = folderSearchRoots?.ToArray(); } }
    public void RegisterDevelopmentWorkspace(EditSessionReference session, string name, string root, string project)
    {
        using var lease = TaskLease(session,true);
        lock(gate)
        {
            if (!DevelopmentPolicy.Name(name) || !DevelopmentPolicy.Relative(project) || !project.EndsWith(".csproj",StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Use an alphanumeric workspace alias and a relative .csproj path.");
            if (tasks.Workspaces.Count >= 32 || tasks.Workspaces.Any(w=>w.Name.Equals(name,StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Workspace alias already used or registry full. Aliases cannot be rebound.");
            var workspace = new DevelopmentWorkspace(name,DevelopmentPolicy.CheckedPath(root),project,Environment.MachineName,true);
            (maxisRuntime ?? throw new InvalidOperationException("Saved MAXIS runtime required.")).CheckWorkspace(workspace);
            tasks = tasks with { Workspaces = Array.AsReadOnly(tasks.Workspaces.Append(workspace).ToArray()) };
            RecordEvent("DevelopmentWorkspaceRegistered",lease.Identity.OperatorId,null,$"MAXIS workspace {name} registered; every task still requires approval.");
        }
    }
    public void RevokeDevelopmentWorkspace(EditSessionReference session, string name)
    {
        using var lease = TaskLease(session,true); lock(gate)
        {
            if (!tasks.Workspaces.Any(w=>w.Name.Equals(name,StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Unknown workspace.");
            tasks = tasks with { Workspaces = Array.AsReadOnly(tasks.Workspaces.Select(w=>w.Name.Equals(name,StringComparison.OrdinalIgnoreCase) ? w with { Enabled=false } : w).ToArray()) };
            RecordEvent("DevelopmentWorkspaceRevoked",lease.Identity.OperatorId,null,"MAXIS workspace revoked: " + name);
        }
    }
    private DevelopmentWorkspace DevelopmentTarget(string capability,string argument)
    {
        if (!DevelopmentPolicy.Parameters(capability,argument)) throw new ArgumentException("Invalid MAXIS parameters.");
        var name = argument.Split('|')[0];
        var workspace = tasks.Workspaces.SingleOrDefault(w=>w.Name.Equals(name,StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException("Register the MAXIS workspace first.");
        (maxisRuntime ?? throw new InvalidOperationException("MAXIS runtime is unavailable.")).CheckWorkspace(workspace);
        return workspace;
    }
}
