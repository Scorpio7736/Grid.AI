using Grid.Simulation.Tasks;
using TaskStatus = Grid.Simulation.Tasks.TaskStatus;

namespace Grid.Agent;

/// <summary>Pure durable task invariants. The host supplies its capability policy and remains the only writer/executor.</summary>
public static class TaskWorkflow
{
    public static bool Label(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max && !value.Any(char.IsControl);

    public static void Validate(IReadOnlyList<SystemAgent> agents, IReadOnlyList<TaskRecord> records,
        Func<AgentRole, string, string, bool> capabilityAllowed)
    {
        ArgumentNullException.ThrowIfNull(capabilityAllowed);
        if (agents is null || records is null || agents.Count > 6 || records.Count > 256 ||
            agents.Any(a => a is null || a.Id == Guid.Empty || !Enum.IsDefined(a.Role)) ||
            agents.Select(a => a.Id).Distinct().Count() != agents.Count || agents.Select(a => a.Role).Distinct().Count() != agents.Count)
            throw new InvalidDataException("Invalid system-agent registry.");
        var ids = new HashSet<Guid>(); var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in records)
        {
            if (t is null || t.Id == Guid.Empty || !ids.Add(t.Id) || !Label(t.RequestKey, 80) || !keys.Add(t.RequestKey) || t.OperatorId == Guid.Empty || t.SubmittedAsAgent == Guid.Empty ||
                !Enum.IsDefined(t.Status) || t.Attempts is < 0 or > 3 || t.ApprovedBy == Guid.Empty || !Label(t.Argument, 256) ||
                t.Result is { } result && !Label(result, 2048) || t.Audit is null || t.Audit.Count is < 1 or > 64 ||
                t.Audit.Any(a => a is null || a.AtUtc.Offset != TimeSpan.Zero || !Label(a.Action, 40) || !Label(a.Detail, 512) || a.OperatorId == Guid.Empty || a.ActiveAgentId == Guid.Empty))
                throw new InvalidDataException("Invalid durable task record.");
            if ((t.Status is TaskStatus.Queued or TaskStatus.Running) && t.Audit.Count > 62 ||
                (t.Status is TaskStatus.Completed or TaskStatus.Failed or TaskStatus.Cancelled or TaskStatus.TimedOut or TaskStatus.Interrupted or TaskStatus.Denied) && t.Result is null ||
                t.Status == TaskStatus.Completed && t.ApprovedBy is null)
                throw new InvalidDataException("Invalid task result or reserved lifecycle audit space.");
            if (t.Status == TaskStatus.Denied && (t.ApprovedBy is not null || !t.Audit.Any(a => a.Action == "Denied" && a.OperatorId == t.OperatorId && a.ActiveAgentId is null)))
                throw new InvalidDataException("Denied tasks require a human denial audit and no approval.");
            if (t.Archived && (t.Status is TaskStatus.AwaitingApproval or TaskStatus.Queued or TaskStatus.Running || !t.Audit.Any(a => a.Action == "Archived")))
                throw new InvalidDataException("Archived tasks must be terminal and retain their archive audit.");
            var agent = agents.SingleOrDefault(a => a.Id == t.AgentId) ?? throw new InvalidDataException("Task target is not a system agent.");
            if (!capabilityAllowed(agent.Role, t.Capability, t.Argument)) throw new InvalidDataException("Invalid task capability or parameters.");
            if ((t.Status is TaskStatus.Queued or TaskStatus.Running) && t.ApprovedBy is null || t.Status == TaskStatus.AwaitingApproval && t.ApprovedBy is not null ||
                (t.Status is TaskStatus.Running or TaskStatus.Completed or TaskStatus.Failed or TaskStatus.TimedOut or TaskStatus.Interrupted) && t.Attempts == 0)
                throw new InvalidDataException("Invalid task approval/attempt state.");
        }
    }
}
