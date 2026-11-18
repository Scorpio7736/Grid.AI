using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
using Status = Grid.Simulation.Tasks.TaskStatus;

namespace Grid.Simulation;

public sealed record AgentSelfTaskEvidence(Guid TaskId, Status Status, int Attempts);
public sealed record AgentSelfEvidence(Guid AgentId, string AgentName, string Role, bool GridRunning,
    int AwaitingApproval, int Queued, int Running, int Terminal,
    IReadOnlyList<AgentSelfTaskEvidence> RecentTasks, bool Truncated);

public sealed partial class GridSimulationEngine
{
    private ProgramEditIdentity AgentBreakGlassIdentity(EditSessionReference session, out IProgramEditLease lease)
    {
        lease = TaskLease(session);
        var actor = lease.Identity;
        if (actor.IsMasterControl || actor.AgentId is null || !actor.IsOriginalHost ||
            !identities.RegisteredAgents.Contains(actor.AgentId.Value))
        {
            lease.Dispose();
            throw DeniedTask(actor, "Original-host registered-agent self authority required.");
        }
        return actor;
    }

    /// <summary>Returns bounded, content-free evidence for only the active registered agent.</summary>
    public AgentSelfEvidence AgentBreakGlassStatus(EditSessionReference session)
    {
        var actor = AgentBreakGlassIdentity(session, out var lease);
        using (lease)
        lock (gate)
        {
            var id = actor.AgentId!.Value;
            var program = world.GetProgram(id);
            var role = tasks.Agents.SingleOrDefault(agent => agent.Id == id)?.Role.ToString() ?? "Registered agent";
            var own = tasks.Records.Where(task => task.AgentId == id).ToArray();
            var recent = own.TakeLast(32).Select(task => new AgentSelfTaskEvidence(task.Id, task.Status, task.Attempts)).ToArray();
            return new(id, program.Name, role, clock.IsRunning,
                own.Count(task => task.Status == Status.AwaitingApproval),
                own.Count(task => task.Status == Status.Queued),
                own.Count(task => task.Status == Status.Running),
                own.Count(task => task.Status is Status.Completed or Status.Failed or Status.Denied or Status.Cancelled or Status.TimedOut or Status.Interrupted),
                Array.AsReadOnly(recent), own.Length > recent.Length);
        }
    }

    /// <summary>Cancels only non-terminal work targeted to the active registered agent.</summary>
    public IReadOnlyList<AgentSelfTaskEvidence> AgentBreakGlassAbortSelf(EditSessionReference session)
    {
        var actor = AgentBreakGlassIdentity(session, out var lease);
        using (lease)
        lock (gate)
        {
            var id = actor.AgentId!.Value;
            var selected = tasks.Records.Where(task => task.AgentId == id && !task.Archived &&
                task.Status is Status.AwaitingApproval or Status.Queued or Status.Running).ToArray();
            foreach (var task in selected)
                ChangeTask(task with { Status = Status.Cancelled, ApprovedBy = null,
                    Result = "Cancelled by the registered agent's self-only break-glass control." },
                    "Cancelled", actor, "Self-only break-glass cancellation; no cross-agent task was changed.");
            if (selected.Length == 0)
                RecordEvent("AgentSelfBreakGlass", id, world.GetProgram(id).Name, "No active self task required cancellation.");
            return Array.AsReadOnly(selected.Select(task => new AgentSelfTaskEvidence(task.Id, Status.Cancelled, task.Attempts)).ToArray());
        }
    }
}
