namespace Grid.Simulation.Tasks;

public enum AgentRole { MCP, MAXIS, SENTINEL, JN, VOLT, RELAY }
public enum TaskStatus { AwaitingApproval, Queued, Running, Completed, Failed, Cancelled, TimedOut, Interrupted }
public sealed record SystemAgent(Guid Id, AgentRole Role);
public sealed record TaskAudit(DateTimeOffset AtUtc, string Action, Guid? OperatorId, Guid? ActiveAgentId, string Detail);
public sealed record TaskRecord(Guid Id, string RequestKey, Guid AgentId, string Capability, string Argument,
    Guid OperatorId, Guid? SubmittedAsAgent, TaskStatus Status, int Attempts, Guid? ApprovedBy,
    string? Result, IReadOnlyList<TaskAudit> Audit);
public sealed record TaskState(IReadOnlyList<SystemAgent> Agents, IReadOnlyList<TaskRecord> Records)
{
    public IReadOnlyList<CoordinationPlan> Plans { get; init; } = Array.Empty<CoordinationPlan>();
    public IReadOnlyList<DevelopmentWorkspace> Workspaces { get; init; } = Array.Empty<DevelopmentWorkspace>();
    public static TaskState Empty => new(Array.Empty<SystemAgent>(), Array.Empty<TaskRecord>());
    public static TaskState Copy(TaskState state) => new(Array.AsReadOnly(state.Agents.ToArray()),
        Array.AsReadOnly(state.Records.Select(t => t with { Audit = Array.AsReadOnly(t.Audit.ToArray()) }).ToArray())) { Workspaces = Array.AsReadOnly(state.Workspaces.ToArray()), Plans = Array.AsReadOnly(state.Plans.Select(p => p with { TaskIds = Array.AsReadOnly(p.TaskIds.ToArray()) }).ToArray()) };
    public static bool Label(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Length <= max && !value.Any(char.IsControl);
    public static void Validate(TaskState state)
    {
        if (state?.Agents is null || state.Records is null || state.Agents.Count > 6 || state.Records.Count > 256 ||
            state.Agents.Any(a => a is null || a.Id == Guid.Empty || !Enum.IsDefined(a.Role)) ||
            state.Agents.Select(a => a.Id).Distinct().Count() != state.Agents.Count || state.Agents.Select(a => a.Role).Distinct().Count() != state.Agents.Count)
            throw new InvalidDataException("Invalid system-agent registry.");
        var ids = new HashSet<Guid>(); var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in state.Records)
        {
            if (t is null || t.Id == Guid.Empty || !ids.Add(t.Id) || !Label(t.RequestKey, 80) || !keys.Add(t.RequestKey) || t.OperatorId == Guid.Empty || t.SubmittedAsAgent == Guid.Empty ||
                !Enum.IsDefined(t.Status) || t.Attempts is < 0 or > 3 || t.ApprovedBy == Guid.Empty || !Label(t.Argument, 256) ||
                t.Result is { } result && !Label(result, 2048) || t.Audit is null || t.Audit.Count is < 1 or > 64 ||
                t.Audit.Any(a => a is null || a.AtUtc.Offset != TimeSpan.Zero || !Label(a.Action, 40) || !Label(a.Detail, 512) || a.OperatorId == Guid.Empty || a.ActiveAgentId == Guid.Empty))
                throw new InvalidDataException("Invalid durable task record.");
            if ((t.Status is TaskStatus.Queued or TaskStatus.Running) && t.Audit.Count > 62 ||
                (t.Status is TaskStatus.Completed or TaskStatus.Failed or TaskStatus.Cancelled or TaskStatus.TimedOut or TaskStatus.Interrupted) && t.Result is null ||
                t.Status == TaskStatus.Completed && t.ApprovedBy is null)
                throw new InvalidDataException("Invalid task result or reserved lifecycle audit space.");
            var agent = state.Agents.SingleOrDefault(a => a.Id == t.AgentId) ?? throw new InvalidDataException("Task target is not a system agent.");
            if (!TaskCapabilities.Valid(agent.Role, t.Capability, t.Argument)) throw new InvalidDataException("Invalid task capability or parameters.");
            if ((t.Status is TaskStatus.Queued or TaskStatus.Running) && t.ApprovedBy is null || t.Status == TaskStatus.AwaitingApproval && t.ApprovedBy is not null ||
                (t.Status is TaskStatus.Running or TaskStatus.Completed or TaskStatus.Failed or TaskStatus.TimedOut or TaskStatus.Interrupted) && t.Attempts == 0)
                throw new InvalidDataException("Invalid task approval/attempt state.");
        }
        CoordinationPlan.Validate(state);
        DevelopmentPolicy.Validate(state.Workspaces);
    }
}

/// <summary>Role-specific allowlist. Real adapters are dispatched separately through the approved broker.</summary>
public static class TaskCapabilities
{
    public static string For(AgentRole role) => role.ToString().ToLowerInvariant() + ".demo";
    public static bool Valid(AgentRole role, string capability, string argument) => TaskState.Label(argument, 256) &&
        (role == AgentRole.SENTINEL && SentinelPolicy.Valid(capability,argument) || role == AgentRole.MAXIS && DevelopmentPolicy.IsReal(capability) && DevelopmentPolicy.Parameters(capability,argument) || capability == For(role) || role == AgentRole.MAXIS && capability == "maxis.delay" && int.TryParse(argument, out var delay) && delay is >= 0 and <= 5000);
    internal static async Task<string> ExecuteAsync(AgentRole role, string capability, string argument, CancellationToken token)
    {
        if (!Valid(role, capability, argument)) throw new ArgumentException("Capability or argument is not allowed for this agent.");
        if (capability == "maxis.delay") await Task.Delay(int.Parse(argument), token);
        token.ThrowIfCancellationRequested();
        return $"[DEMONSTRATION ONLY] {role}: {argument}. No host action was performed.";
    }
}
