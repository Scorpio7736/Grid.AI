using Grid.Simulation.Tasks;
using Status = Grid.Simulation.Tasks.TaskStatus;

namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    private sealed record ReviewedPlan(FollowUp Scope, Guid Id, IReadOnlyList<TaskRecord> Tasks);
    private ReviewedPlan? reviewedPlan;
    private sealed record SavedPlanChoices(FollowUp Scope, IReadOnlyList<Guid> Ids);
    private SavedPlanChoices? savedPlanChoices;

    private CommandResult? ResumeSavedPlan(string input)
    {
        var list = System.Text.RegularExpressions.Regex.IsMatch(input,
            @"^(resume my work|show unfinished plans|continue my work)[.!]?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var selection = System.Text.RegularExpressions.Regex.Match(input, @"^(?:resume|review) plan ([0-9]+)[.!]?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!list && !selection.Success) return null;
        if (identity?.IsMaster != true) return Explain("Only Master Control can review saved operator plans.");
        if (Find(active!.Value).IsMeeting) return Explain("Review saved operator plans in a one-to-one conversation; their requests may contain private information.");
        var state = engine.TasksFor(identity.Reference);
        if (list)
        {
            reviewedPlan = null;
            var plans = state.Plans.Where(p => p.OperatorId == engine.LocalOperator.Id && p.SubmittedAsAgent is null &&
                p.TaskIds.Any(id => state.Records.Any(t => t.Id == id && !t.Archived && t.Status is not (Status.Completed or Status.Cancelled or Status.Denied))))
                .OrderByDescending(p => p.CreatedUtc).ThenBy(p => p.Id).ToArray();
            savedPlanChoices = new(Pending("saved-plan-selection", input), plans.Select(p => p.Id).ToArray());
            if (plans.Length == 0) return Explain("There are no unfinished saved operator plans. Nothing executed.");
            return Explain("Saved operator plans across conversations (private chat context is not restored):\n" +
                string.Join("\n", plans.Select((p, i) => $"{i + 1}. {TerminalTable.Safe(p.Request)}")) +
                "\nSay resume plan followed by its number to review current tasks and results. Nothing is approved or executed by selecting a plan.");
        }
        if (savedPlanChoices is not { } choices || !Current(choices.Scope))
        {
            savedPlanChoices = null;
            return Explain("The saved-plan selection expired. Say resume my work for a current list.");
        }
        if (!int.TryParse(selection.Groups[1].Value, out var number) || number < 1 || number > choices.Ids.Count)
            return Explain("Choose a number from the saved-plan list, or say resume my work to refresh it.");
        var selected = state.Plans.SingleOrDefault(p => p.Id == choices.Ids[number - 1] && p.OperatorId == engine.LocalOperator.Id && p.SubmittedAsAgent is null);
        if (selected is null) return Explain("That saved plan is no longer available. Say resume my work to refresh the list.");
        return new("Saved plan selected for review. No approval or execution was restored.\n" + FocusPlan(selected.Id));
    }

    private CommandResult? DirectDevelopmentPlan(string input)
    {
        var demonstration = System.Text.RegularExpressions.Regex.Match(input,
            @"^(?:plan to (?:have )?)?MAXIS demonstrate (?:the message )?(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (demonstration.Success)
        {
            var message = demonstration.Groups[1].Value.Trim().Trim('"');
            if (!TaskCapabilities.Valid(AgentRole.MAXIS, "maxis.demo", message))
                return Explain("Use a demonstration message of 1–256 printable characters. Nothing created.");
            assistanceRequest++;
            assistanceDraft = new(new AssistanceProposal([new(AgentRole.MAXIS, "maxis.demo", message)]),
                new AssistanceGuard(active!.Value, engine.ActivityGeneration, engine.ContextRevision, engine.ConversationMemoryRevision),
                identity!.Reference, Guid.NewGuid().ToString("N"));
            return Explain("DRAFT ONLY — MAXIS demonstration message: " + message +
                ". No file, build or host action is requested; no workspace is needed. Say submit plan to review it.");
        }
        var match = System.Text.RegularExpressions.Regex.Match(input,
            @"^(?:plan to )?(build|test|run tests for) (?:the )?(?:workspace |project )?([A-Za-z0-9._-]+)[.!]?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success) return null;
        var workspace = engine.TasksFor(identity!.Reference).Workspaces.SingleOrDefault(w => w.Enabled && w.Name.Equals(match.Groups[2].Value.TrimEnd('.'), StringComparison.OrdinalIgnoreCase));
        if (workspace is null) return null;
        var capability = match.Groups[1].Value.Equals("build", StringComparison.OrdinalIgnoreCase) ? "maxis.build" : "maxis.test";
        var proposal = new AssistanceProposal([new(AgentRole.MAXIS, capability, workspace.Name)]);
        var guard = new AssistanceGuard(active!.Value, engine.ActivityGeneration, engine.ContextRevision, engine.ConversationMemoryRevision);
        assistanceRequest++;
        assistanceDraft = new(proposal, guard, identity.Reference, Guid.NewGuid().ToString("N"));
        return Explain("DRAFT ONLY — MAXIS " + capability + " for workspace " + workspace.Name + ".\nSay submit plan to review the task, or discard plan to cancel the draft.");
    }

    private string FocusPlan(Guid id)
    {
        var state = engine.TasksFor(identity!.Reference);
        var plan = state.Plans.Single(p => p.Id == id);
        var tasks = plan.TaskIds.Select(taskId => state.Records.Single(t => t.Id == taskId)).ToArray();
        reviewedPlan = new(Pending("plan-review", plan.Request), id, tasks);
        return "PLAN v1 | Source: saved human-submitted task records; model output is not execution evidence.\n" +
            string.Join("\n\n", tasks.Select(task => McpCapabilityCatalog.Effect(task.Capability) + "\n" + TaskDisplay.Inspect(state, task))) +
            "\nSay approve this plan to approve these exact tasks, run this plan to execute approved tasks, or cancel this plan.";
    }

    private bool TryPlanConversation(string input, out CommandResult result)
    {
        result = new("");
        var match = System.Text.RegularExpressions.Regex.Match(input,
            @"^(approve|run|cancel|review|inspect|show|apply|do|retry) (?:(?:this|that|the) plan|that|it)[.!]?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success) return false;
        var explicitPlan = input.Contains("plan", StringComparison.OrdinalIgnoreCase);
        if (!explicitPlan && fileRequest is not null)
        {
            if (reviewedPlan is null) return false;
            result = Explain("There is a file request and a task plan. Say approve this plan or approve this change to identify the operation.");
            return true;
        }
        if (reviewedPlan is not { } plan)
        {
            if (!explicitPlan) return false;
            result = Explain("Review and submit a task draft first so I can identify the exact plan.");
            return true;
        }
        if (!Current(plan.Scope) || identity?.IsMaster != true || save is null)
        {
            reviewedPlan = null;
            result = Explain("The plan review expired with its session or context. Review the plan again before acting.");
            return true;
        }
        try
        {
            var action = match.Groups[1].Value.ToLowerInvariant();
            if (action is "review" or "inspect" or "show") result = new(FocusPlan(plan.Id));
            else if (action == "retry")
            {
                var failed = plan.Tasks.Where(t => t.Status is Status.Failed or Status.TimedOut or Status.Interrupted).ToArray();
                engine.RetryReviewedObservations(identity.Reference, failed);
                save(CancellationToken.None).GetAwaiter().GetResult();
                result = new("Read-only retry prepared. Fresh approval is required; nothing ran.\n" + FocusPlan(plan.Id));
            }
            else if (action == "approve")
            {
                var pending = plan.Tasks.Where(t => t.Status == Status.AwaitingApproval && !t.Archived).ToArray();
                if (pending.Length == 0)
                { result = Explain("These tasks are already approved or have changed state. Review this plan for its current results."); return true; }
                engine.UpdateTaskBatch(identity.Reference, pending, archive: false);
                save(CancellationToken.None).GetAwaiter().GetResult();
                result = new("Approved the reviewed tasks. Say run this plan to execute them.\n" + FocusPlan(plan.Id));
            }
            else if (action == "cancel") result = Dispatch("mcp", "cancel", plan.Id.ToString());
            else result = Dispatch("mcp", "run", plan.Id.ToString());
            return true;
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or UnauthorizedAccessException or IOException or Microsoft.Data.Sqlite.SqliteException)
        {
            result = Explain("Plan operation stopped: " + error.Message + " Review the current plan state before retrying.");
            return true;
        }
    }
}
