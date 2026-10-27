using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
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

