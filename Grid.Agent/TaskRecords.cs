namespace Grid.Simulation.Tasks;

public enum AgentRole { MCP, MAXIS, SENTINEL, JN, VOLT, RELAY }
public enum TaskStatus { AwaitingApproval, Queued, Running, Completed, Failed, Cancelled, TimedOut, Interrupted, Denied }
public sealed record SystemAgent(Guid Id, AgentRole Role);
public sealed record TaskAudit(DateTimeOffset AtUtc, string Action, Guid? OperatorId, Guid? ActiveAgentId, string Detail);
public sealed record TaskRecord(Guid Id, string RequestKey, Guid AgentId, string Capability, string Argument,
    Guid OperatorId, Guid? SubmittedAsAgent, TaskStatus Status, int Attempts, Guid? ApprovedBy,
    string? Result, IReadOnlyList<TaskAudit> Audit)
{
    public bool Archived { get; init; }
}
