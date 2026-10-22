using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private static bool SameTask(TaskRecord current, TaskRecord expected) =>
        (current with { Audit = expected.Audit }) == expected && current.Audit.SequenceEqual(expected.Audit);

    /// <summary>Atomically approves or archives precisely the records reviewed by the human.</summary>
    public void UpdateTaskBatch(EditSessionReference session, IReadOnlyList<TaskRecord> reviewed, bool archive)
    {
        using var lease = TaskLease(session, true);
        lock (gate)
        {
            if (reviewed.Count is < 1 or > 256 || reviewed.Select(t => t.Id).Distinct().Count() != reviewed.Count)
                throw new ArgumentException("Invalid task batch.");
            var replacements = new Dictionary<Guid, TaskRecord>();
            foreach (var expected in reviewed)
            {
                var current = FindTask(expected.Id);
                if (!SameTask(current, expected) || current.Archived) throw new InvalidOperationException("Task batch changed since preview; review it again.");
                if (current.Status == Status.Running) throw new InvalidOperationException("Cannot clear running tasks; cancel and wait for the broker to finish first.");
                if (archive && taskExecuting) throw new InvalidOperationException("Wait for the active broker attempt to settle before clearing tasks.");
                if (current.Audit.Count >= 60) throw new InvalidOperationException("Task audit capacity reached; history is retained.");
                if (!archive)
                {
                    if (current.Status != Status.AwaitingApproval) throw new InvalidOperationException("Only awaiting-approval tasks may be bulk approved.");
                    if (DevelopmentPolicy.IsReal(current.Capability)) _ = DevelopmentTarget(current.Capability,current.Argument);
                    if (StoragePolicy.IsReal(current.Capability)) _ = StorageTarget(current.Capability,current.Argument);
                }
                var pending = current.Status is Status.AwaitingApproval or Status.Queued;
                var detail = archive ? "Human cleared task from the operational list; pending work cancelled, history retained." : "Human approved this reviewed batch request for one attempt.";
                var next = archive ? current with { Archived = true, Status = pending ? Status.Cancelled : current.Status,
                    Result = pending ? "Cancelled by human batch clear; history retained." : current.Result } :
                    current with { Status = Status.Queued, ApprovedBy = lease.Identity.OperatorId };
                replacements.Add(next.Id, next with { Audit = Array.AsReadOnly(next.Audit.Append(new TaskAudit(timeProvider.GetUtcNow().ToUniversalTime(), archive ? "Archived" : "Approved", lease.Identity.OperatorId, null, detail)).ToArray()) });
            }
            var updated = tasks with { Records = Array.AsReadOnly(tasks.Records.Select(t => replacements.GetValueOrDefault(t.Id, t)).ToArray()) };
            TaskState.Validate(updated); tasks = updated;
            foreach (var task in replacements.Values) RecordEvent(archive ? "TaskArchived" : "TaskApproved", task.Id, null, task.Audit.Last().Detail);
        }
    }
}
