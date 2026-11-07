using Grid.Agent;
using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    public void CancelCoordination(EditSessionReference session, Guid planId)
    {
        using var lease = TaskLease(session);
        lock (gate)
        {
            var coordinator = Coordinator(lease.Identity);
            var plan = tasks.Plans.Single(p => p.Id == planId && p.CoordinatorId == coordinator.Id);
            foreach (var task in plan.TaskIds.Select(FindTask).Where(t => !t.Archived && t.Status is Status.AwaitingApproval or Status.Queued or Status.Running).ToArray())
                CancelTask(session, task.Id);
            RecordEvent("CoordinationRevoked", plan.Id, "MCP", "Pending and running plan work cancelled; completed effects are not undone.");
        }
    }

    public async Task<string> RunCoordinationAsync(EditSessionReference session, Guid planId, Func<CancellationToken, Task> save, CancellationToken token, IReadOnlyList<TaskRecord>? expected = null)
    {
        ArgumentNullException.ThrowIfNull(save);
        TaskRecord[] selected;
        long activity;
        using (var lease = TaskLease(session, true))
        lock (gate)
        {
            RequireAgentActivity(); activity = runVersion;
            var coordinator = Coordinator(lease.Identity);
            var plan = tasks.Plans.SingleOrDefault(p => p.Id == planId && p.CoordinatorId == coordinator.Id) ?? throw new ArgumentException("Unknown MCP plan.");
            selected = plan.TaskIds.Select(FindTask).Where(t => t.Status != Status.Completed).ToArray();
            if (expected is not null && (selected.Length != expected.Count || selected.Where((t, i) => !SameTask(t, expected[i])).Any()))
                throw new InvalidOperationException("Plan changed since dispatch was requested; inspect it and request dispatch again.");
            if (selected.Length == 0) return CoordinationBrief(session, planId);
            if (selected.Any(t => t.Archived || t.Status != Status.Queued || t.ApprovedBy != lease.Identity.OperatorId))
                throw new InvalidOperationException("Review and approve every remaining plan task first. Denied/cancelled/failed work blocks dispatch; retry eligible failures with fresh approval. Nothing dispatched.");
            RecordEvent("CoordinationDispatchRequested", plan.Id, "MCP", "Human delegated ordered dispatch of exactly the approved plan tasks; no new approval granted.");
        }
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(ToolDelegationPolicy.PlanDeadline);
        var note = "All selected tasks completed with recorded results.";
        foreach (var task in selected)
        {
            try
            {
                using (var lease = TaskLease(session, true))
                lock (gate) { if (activity != runVersion) throw new OperationCanceledException("GRID activity changed."); }
                budget.Token.ThrowIfCancellationRequested();
                var outcome = await RunNextTaskAsync(session, save, budget.Token, task);
                if (!outcome.StartsWith("Completed:", StringComparison.Ordinal)) { note = "Plan stopped after an unsuccessful attempt; remaining tasks were not dispatched."; break; }
            }
            catch (Exception e) when (e is OperationCanceledException or UnauthorizedAccessException or InvalidOperationException or IOException or System.Data.Common.DbException)
            {
                note = "Plan stopped: " + e.Message + ". Completion/durability is not confirmed for an interrupted or failed save. No automatic retry.";
                break;
            }
        }
        // Never expose results through a session that was revoked while execution was in flight.
        return note + "\n" + CoordinationBrief(session, planId);
    }
}
