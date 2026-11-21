using System.Text.RegularExpressions;

namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    // Resolve only direct human control phrases, before any interpreter can consume them.
    private CommandResult? ResolveOperationReference(ref string input)
    {
        var progress = Regex.IsMatch(input, @"^(?:how is (?:it|that) (?:going|doing)|show (?:its results|the results|its progress))[?.!]*$", RegexOptions.IgnoreCase);
        var delegation = Regex.IsMatch(input, @"^ask MAXIS to do (?:it|that)[.!]?$", RegexOptions.IgnoreCase);
        var match = Regex.Match(input, @"^(approve|apply|run|do|cancel|undo|revise) (?:it|that)[.!]?$", RegexOptions.IgnoreCase);
        if (!delegation && !progress && !match.Success) return null;
        var clarification = followUp is { Kind: "task-clarification" or "intent-clarification" };
        var count = (fileRequest is null ? 0 : 1) + (reviewedPlan is null ? 0 : 1) +
            (assistanceDraft is null ? 0 : 1) + (pendingIntent is null ? 0 : 1) + (clarification ? 1 : 0);
        if (count > 1)
            return Explain("More than one operation is pending. Specify the file change, reviewed task plan, task draft, or interpreted request. Nothing approved or executed.");
        if (count == 0)
            return Explain("Which operation do you mean? Review a file change or task plan first. Nothing executed or approved.");
        if (clarification)
        {
            if (match.Success && match.Groups[1].Value.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            {
                followUp = null; assistanceRequest++; intentRequest++;
                return Explain("Clarification request cancelled. Nothing created, approved or executed.");
            }
            return Explain("Answer the clarification first, or say cancel it. There is no reviewed operation to approve or execute.");
        }
        if (progress)
        {
            if (reviewedPlan is null) return Explain("Review a task plan first, or ask for the file preview. No task plan is selected.");
            input = "review this plan";
            return null;
        }
        if (delegation)
        {
            if (reviewedPlan is not { } plan || plan.Tasks.Any(t => !t.Capability.StartsWith("maxis.", StringComparison.Ordinal)))
                return Explain("Review a plan assigned entirely to MAXIS first. This request cannot reassign work or approve it.");
            input = "run this plan"; // Existing execution path rechecks current scope and task approvals.
            return null;
        }
        if (match.Groups[1].Value.Equals("revise", StringComparison.OrdinalIgnoreCase))
            return Explain("What should change? Describe the revision to the file change, or discard the task draft and describe a replacement plan. Nothing changed or executed.");
        if (pendingIntent is not null && match.Groups[1].Value.Equals("cancel", StringComparison.OrdinalIgnoreCase))
            input = "cancel this request";
        return null;
    }
}
