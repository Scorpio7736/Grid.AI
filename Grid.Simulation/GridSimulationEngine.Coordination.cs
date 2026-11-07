using Grid.Agent;
using Grid.Simulation.Editing;
using Grid.Simulation.Tasks;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Simulation;

public sealed partial class GridSimulationEngine
{
    private SystemAgent Coordinator(ProgramEditIdentity actor)
    {
        var mcp = tasks.Agents.SingleOrDefault(a => a.Role == AgentRole.MCP) ?? throw new InvalidOperationException("Use grid -start before requesting MCP coordination.");
        if (!actor.IsMasterControl && actor.AgentId != mcp.Id) throw DeniedTask(actor, "Only Master Control or the registered MCP role can coordinate requests.");
        return mcp;
    }
    public CoordinationPlan Coordinate(EditSessionReference session, string request, string requestKey, bool previewOnly = false, AssistanceGuard? guard = null)
    {
        using var lease = TaskLease(session);
        lock (gate)
        {
            RequireAgentActivity();
            if(guard is not null)
            {
                var conversation=GetConversation(guard.ConversationId);
                if(!lease.Identity.IsMasterControl || conversation.IsMeeting && !conversation.Participants.Any(p=>tasks.Agents.Any(a=>a.Role==AgentRole.MCP && a.Id==p.Id)) || conversation.Status!=Grid.Communication.ConversationStatus.Open ||
                    !conversation.Participants.Any(p=>Grid.Communication.CommunicationState.SameIdentity(p,LocalOperator)) ||
                    guard.Activity!=runVersion || guard.Context!=contextRevision || guard.Memory!=communications.MemoryRevision)
                    throw new InvalidOperationException("Assistance scope changed; request a new draft.");
            }
            var mcp = Coordinator(lease.Identity);
            if (!TaskState.Label(requestKey, 80)) throw new ArgumentException("Request key must contain 1..80 printable characters.");
            var previous = tasks.Plans.SingleOrDefault(p => p.RequestKey == requestKey);
            if (previous is not null)
            {
                if (previous.Request != request || previous.OperatorId != lease.Identity.OperatorId || previous.SubmittedAsAgent != lease.Identity.AgentId)
                    throw new ArgumentException("Coordination key already belongs to a different request/actor.");
                return previous;
            }
            var steps = CoordinationRequest.Parse(request);
            ToolDelegationPolicy.Validate("MCP", steps.Select(step => step.Role.ToString()).ToArray());
            if (tasks.Plans.Count >= 64 || tasks.Records.Count + steps.Count > 256) throw new InvalidOperationException("Plan/task capacity reached; nothing delegated.");
            var planId = Guid.NewGuid(); var utc = timeProvider.GetUtcNow().ToUniversalTime(); var children = new List<TaskRecord>();
            foreach (var step in steps)
            {
                if (StoragePolicy.IsReal(step.Capability)) _ = StorageTarget(step.Capability,step.Argument);
                if (DevelopmentPolicy.IsReal(step.Capability)) _ = DevelopmentTarget(step.Capability,step.Argument);
                var agent = tasks.Agents.SingleOrDefault(a => a.Role == step.Role) ?? throw new ArgumentException("Specialist role is not initialized: " + step.Role);
                if (!identities.RegisteredAgents.Contains(agent.Id) || world.GetProgram(agent.Id).Kind != Grid.Core.ResidentKind.Agent)
                    throw DeniedTask(lease.Identity, "Specialist identity is no longer authorized.");
                children.Add(new(Guid.NewGuid(), $"plan-{planId:N}-{children.Count}", agent.Id, step.Capability, step.Argument,
                    lease.Identity.OperatorId, lease.Identity.AgentId, Status.AwaitingApproval, 0, null, null,
                    Array.AsReadOnly(new[] { new TaskAudit(utc, "Delegated", lease.Identity.OperatorId, lease.Identity.AgentId, $"MCP plan {planId}; exact specialist request awaits human approval.") })));
            }
            var plan = new CoordinationPlan(planId, requestKey, request, mcp.Id, lease.Identity.OperatorId, lease.Identity.AgentId, utc,
                Array.AsReadOnly(children.Select(t => t.Id).ToArray()));
            var next = tasks with { Records = Array.AsReadOnly(tasks.Records.Concat(children).ToArray()), Plans = Array.AsReadOnly(tasks.Plans.Append(plan).ToArray()) };
            TaskState.Validate(next); if(previewOnly) return plan; tasks = next; // Commit the whole plan, never a partially parsed request.
            RecordEvent("CoordinationPlanned", plan.Id, "MCP", $"{children.Count} subtasks delegated; none approved or executed.",
                System.Text.Json.JsonSerializer.Serialize(new { operatorId = lease.Identity.OperatorId, activeAgentId = lease.Identity.AgentId, coordinatorId = mcp.Id, planId }));
            return plan;
        }
    }
    public string CoordinationBrief(EditSessionReference session, Guid? planId = null)
    {
        using var lease = TaskLease(session);
        lock (gate)
        {
            _ = Coordinator(lease.Identity);
            var plans = planId is null ? tasks.Plans : tasks.Plans.Where(p => p.Id == planId).ToArray();
            if (planId is not null && plans.Count == 0) throw new ArgumentException("Unknown MCP plan ID.");
            var lines = new List<string> { $"[VERIFIED BROKER STATE] MCP briefing at tick {clock.TickCount}. {plans.Count} plan(s). Approved specialist capabilities perform real work; demos remain labeled." };
            foreach (var plan in plans)
            {
                var children = plan.TaskIds.Select(FindTask).ToArray();
                var counts = string.Join(", ", children.GroupBy(t => t.Status).OrderBy(g => g.Key).Select(g => $"{g.Key}: {g.Count()}"));
                lines.Add($"Plan {plan.Id} | key {plan.RequestKey} | {counts}");
                lines.Add($"Request: {plan.Request}");
                foreach (var t in children)
                {
                    var role = tasks.Agents.Single(a => a.Id == t.AgentId).Role;
                    lines.Add($"  {t.Id} | {role} | {t.Capability} | {t.Status} | attempts {t.Attempts}");
                    lines.Add($"  Result: {t.Result ?? "No result recorded; no completion claimed."}");
                    if (t.Status == Status.AwaitingApproval) lines.Add($"  Human approval required: tasks inspect {t.Id}, then tasks approve {t.Id}.");
                    else if (t.Status == Status.Queued) lines.Add("  Approved and queued; original-host Master Control can use tasks run for one attempt.");
                    else if (t.Status is Status.Failed or Status.TimedOut or Status.Interrupted) lines.Add("  Review failure; a permitted retry requires fresh human approval.");
                }
            }
            return string.Join("\n", lines);
        }
    }
}
