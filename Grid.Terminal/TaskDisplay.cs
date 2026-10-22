using Grid.Simulation.Tasks;
namespace Grid.Terminal;

internal static class TaskDisplay
{
    internal static string Ref(Guid id, IEnumerable<Guid> scope)
    {
        var all = scope.ToArray(); var text = id.ToString();
        for (var length = 8; length < 36; length++)
            if (all.Count(x => x.ToString().StartsWith(text[..length], StringComparison.OrdinalIgnoreCase)) == 1) return text[..length];
        return text;
    }
    internal static Guid Resolve(string input, IEnumerable<Guid> scope, string kind)
    {
        input = input.Trim().TrimEnd('.', ',');
        var matches = scope.Where(id => id.ToString().StartsWith(input, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (input.Length >= 8 && matches.Length == 1) return matches[0];
        throw new ArgumentException(matches.Length > 1 ? "Ambiguous " + kind + " reference; use more characters or the full ID." :
            "Unknown " + kind + " reference. Use " + (kind == "plan" ? "mcp plans for PLAN references; tasks inspect reads TASK references." : kind == "notification" ? "relay inbox for notification references." : kind == "quota proposal" ? "jn quota for quota proposal references." : "tasks list for TASK references.") + " Replace placeholders with a displayed reference.");
    }
    internal static string Role(TaskState state, TaskRecord task) => state.Agents.Single(a => a.Id == task.AgentId).Role.ToString();
    internal static string Tasks(TaskState state, IEnumerable<TaskRecord>? records = null) =>
        TerminalTable.Render(["Task", "Agent", "State", "Tries"], (records ?? state.Records.Where(t => !t.Archived)).Select(t => new[] {
            Ref(t.Id, state.Records.Select(x => x.Id)), Role(state,t), t.Status.ToString(), t.Attempts.ToString() }));
    internal static string Inspect(TaskState state, TaskRecord t)
    {
        var reference = Ref(t.Id, state.Records.Select(x => x.Id));
        var workspace = state.Workspaces.SingleOrDefault(w => w.Name.Equals(t.Argument.Split('|')[0],StringComparison.OrdinalIgnoreCase));
        var scope = DevelopmentPolicy.IsReal(t.Capability) && workspace is not null ? $"\nWORKSPACE\nRoot: {workspace.Root}\nProject: {workspace.Project}\nEnabled: {workspace.Enabled}\n" +
            (t.Capability is "maxis.build" or "maxis.test" ? "REAL CODE EXECUTION: project-defined code runs as your Windows user; outputs may be written. No OS sandbox/network isolation.\n" : "Source remains unchanged; reports/suggestions are written to GRID artifacts.\n") : "";
        if(StoragePolicy.IsReal(t.Capability))
        {
            var registered=state.Storage.Scopes.SingleOrDefault(s=>s.Name.Equals(t.Argument,StringComparison.OrdinalIgnoreCase));
            scope+="\nJN STORAGE SCOPE\n"+(registered is not null ? registered.Path+" | enabled: "+registered.Enabled : "GRID-managed storage roots / logical quota governance")+"\nNo files will be deleted. Quota proposals require separate Master Control approval.\n";
        }
        if(t.Capability=="relay.speak" && Guid.TryParse(t.Argument,out var spokenId))
        {
            var note=state.Relay.Notifications.SingleOrDefault(n=>n.Id==spokenId);
            scope+="\nNOTIFICATION TEXT TO SPEAK\n"+(note?.Text ?? "Notification unavailable; execution will fail.")+"\n";
            scope+="\nAUDIO OUTPUT: this approved task will speak the saved notification on the original host. No automatic/background speech.\n";
        }
        if(t.Capability=="volt.throttle") scope+="\nGRID PACING: changes only the automatic simulation wait. Does not change Windows allocations, manual steps or simulated tick length.\n";
        return $"TASK {reference} - {Role(state,t)}\n" + new string('-', 54) +
            $"\nArchived:    {t.Archived}\nState:       {t.Status}\nCapability:  {t.Capability}\nArgument:    {t.Argument}\nAttempts:    {t.Attempts} / 3\nRequest key: {t.RequestKey}\nFull ID:     {t.Id}\n\nRESULT\n{(SentinelPolicy.IsReal(t.Capability) ? t.Result?.Replace(" | ", "\n") : t.Result) ?? "No result recorded; no completion claimed."}\n\n" +
            scope + Next(t, reference) + $"\n\nIDENTITY & AUDIT\nOperator: {t.OperatorId}\nSubmitted as: {t.SubmittedAsAgent?.ToString() ?? "Master Control"}\nApproved by: {t.ApprovedBy?.ToString() ?? "Not approved"}\n" +
            string.Join("\n\n", t.Audit.Select(a => $"{a.AtUtc:O}  {a.Action}\n  {a.Detail}\n  Operator: {a.OperatorId}\n  Agent: {a.ActiveAgentId}"));
    }
    private static string Next(TaskRecord t, string reference) => t.Archived ? "Archived: retained for history; no further execution or retry." : t.Status switch
    {
        Grid.Simulation.Tasks.TaskStatus.AwaitingApproval => $"NEXT - human Master Control approval required\ntasks approve {reference}",
        Grid.Simulation.Tasks.TaskStatus.Queued => "NEXT - run the next queued task from the original host\ntasks run",
        Grid.Simulation.Tasks.TaskStatus.Failed or Grid.Simulation.Tasks.TaskStatus.TimedOut or Grid.Simulation.Tasks.TaskStatus.Interrupted => $"NEXT - review the result; retry requires fresh approval\ntasks retry {reference}",
        _ => "No approval or dispatch pending for this task."
    };
    internal static string Plans(TaskState state, Guid? selected, bool summary)
    {
        var plans = selected is null ? state.Plans : state.Plans.Where(p => p.Id == selected).ToArray();
        string Counts(CoordinationPlan p) => string.Join(", ", state.Records.Where(t => p.TaskIds.Contains(t.Id)).GroupBy(t => t.Status).Select(g => $"{g.Key}: {g.Count()}"));
        var heading = "MCP PLANS | VERIFIED BROKER STATE\nApproved specialist capabilities perform real work; demos remain labeled.\n\n";
        if (summary) return heading + TerminalTable.Render(["Plan", "Request key", "Progress"], plans.Select(p => new[] { Ref(p.Id,state.Plans.Select(x=>x.Id)), p.RequestKey, Counts(p) })) + "\n\nDetails: mcp inspect <plan-reference>\nUse a PLAN reference here; tasks inspect takes a TASK reference.";
        return heading + string.Join("\n\n", plans.Select(p =>
        {
            var children = p.TaskIds.Select(id => state.Records.Single(t => t.Id == id)).ToArray();
            return $"PLAN {Ref(p.Id,state.Plans.Select(x=>x.Id))} | {p.RequestKey}\n" + new string('=',54) +
                $"\nFull plan ID: {p.Id}\nRequest: {p.Request}\nProgress: {Counts(p)}\n\n" + Tasks(state,children) + "\n\n" +
                string.Join("\n\n",children.Select(t => { var r=Ref(t.Id,state.Records.Select(x=>x.Id)); return $"TASK {r} - {Role(state,t)}\nResult: {(SentinelPolicy.IsReal(t.Capability) ? t.Result?.Replace(" | ", "\n") : t.Result) ?? "No result recorded; no completion claimed."}\n\nInspect this task:\ntasks inspect {r}\n\n" + Next(t,r); }));
        }));
    }
}
