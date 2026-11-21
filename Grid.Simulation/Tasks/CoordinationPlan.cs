namespace Grid.Simulation.Tasks;

public sealed record CoordinationPlan(Guid Id, string RequestKey, string Request, Guid CoordinatorId, Guid OperatorId,
    Guid? SubmittedAsAgent, DateTimeOffset CreatedUtc, IReadOnlyList<Guid> TaskIds)
{
    public static void Validate(TaskState state)
    {
        if (state.Plans is null || state.Plans.Count > 64) throw new InvalidDataException("Invalid coordination plans.");
        var ids = new HashSet<Guid>(); var keys = new HashSet<string>(StringComparer.Ordinal); var children = new HashSet<Guid>();
        foreach (var p in state.Plans)
        {
            if (p is null || p.Id == Guid.Empty || !ids.Add(p.Id) || !TaskState.Label(p.RequestKey, 80) || !keys.Add(p.RequestKey) ||
                !TaskState.Label(p.Request, 2048) || p.OperatorId == Guid.Empty || p.SubmittedAsAgent is { } actor && actor != p.CoordinatorId ||
                p.CreatedUtc.Offset != TimeSpan.Zero || p.TaskIds is null || p.TaskIds.Count is < 1 or > 8 ||
                !state.Agents.Any(a => a.Id == p.CoordinatorId && a.Role == AgentRole.MCP)) throw new InvalidDataException("Invalid coordination plan identity or scope.");
            foreach (var id in p.TaskIds)
            {
                var task = state.Records.SingleOrDefault(t => t.Id == id);
                if (!children.Add(id) || task is null || task.OperatorId != p.OperatorId || task.SubmittedAsAgent != p.SubmittedAsAgent || task.AgentId == p.CoordinatorId)
                    throw new InvalidDataException("Invalid delegated task linkage.");
            }
        }
    }
}

public sealed record DelegationStep(AgentRole Role, string Capability, string Argument);
public static class CoordinationRequest
{
    // A small explicit grammar, not a natural-language/AI guess at executable intent.
    public static IReadOnlyList<DelegationStep> Parse(string request)
    {
        if (!TaskState.Label(request, 2048)) throw new ArgumentException("Request must contain 1..2048 printable characters.");
        var clauses = request.Split(';');
        if (clauses.Length is < 1 or > 8) throw new ArgumentException("Use 1..8 semicolon-separated specialist clauses.");
        var steps = new List<DelegationStep>();
        foreach (var clause in clauses)
        {
            var split = clause.IndexOf(':');
            if (split <= 0 || !Enum.TryParse<AgentRole>(clause[..split].Trim(), true, out var role) || !Enum.IsDefined(role) || role == AgentRole.MCP || int.TryParse(clause[..split].Trim(), out _))
                throw new ArgumentException("Use a specialist name followed by a colon: MAXIS, TRON, JN, VOLT or RELAY. MCP cannot delegate recursively.");
            var body = clause[(split + 1)..].Trim(); var separator = body.IndexOf(' ');
            if (separator < 1) throw new ArgumentException("Specify an explicit supported action and argument, e.g. MAXIS: demo hello.");
            var action = body[..separator].ToLowerInvariant(); var argument = body[(separator + 1)..].Trim();
            var capability = role.ToString().ToLowerInvariant() + "." + action;
            if (!TaskCapabilities.Valid(role, capability, argument))
                throw new ArgumentException($"Unsupported action/argument for {role}. Use demo, MAXIS delay, registered MAXIS inspect/build/test/suggest, TRON defender/firewall status and events recent, JN inventory/duplicates/cleanup <scope>, storage status, quota <GB>; VOLT status current or throttle normal/eco/auto; RELAY notify <text>, brief <task-guid>, speak <notification-guid>.");
            steps.Add(new(role, capability, argument));
        }
        return Array.AsReadOnly(steps.ToArray());
    }
}
