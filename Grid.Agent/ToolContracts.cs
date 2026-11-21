namespace Grid.Agent;

public sealed record TaskToolRequest(Guid AgentId, string Capability, string Argument, string RequestKey);
public enum FileToolOperation { Read, Create, Update, Delete }
public sealed record FileToolRequest(string Scope, string RelativePath, FileToolOperation Operation, string? Content = null);
public sealed record FileReadReceipt(string AbsolutePath, string Sha256, string Content);
public sealed record ToolExecutionReceipt(Guid TaskId, Guid? ParentPlanId, Guid AgentId, string Capability,
    string State, int Attempts, string RequestHash, string? ResultHash, DateTimeOffset? StartedUtc, DateTimeOffset? FinishedUtc);

/// <summary>Deterministic limits for the currently supported MCP-to-specialist delegation graph.</summary>
public static class ToolDelegationPolicy
{
    public const int MaximumDepth = 1;
    public const int MaximumChildren = 8;
    public const int MaximumActiveJobs = 1;
    public static TimeSpan PlanDeadline => TimeSpan.FromMinutes(10);

    public static void Validate(string coordinator, IReadOnlyList<string> children, int depth = 1)
    {
        if (coordinator != "MCP" || depth != MaximumDepth || children.Count is < 1 or > MaximumChildren)
            throw new ArgumentException("Delegation requires MCP and one level of 1–8 specialist tasks.");
        if (children.Any(role => role is not ("MAXIS" or "TRON" or "JN" or "VOLT" or "RELAY")))
            throw new ArgumentException("Delegation target is outside the specialist allowlist or creates a cycle.");
    }
}
