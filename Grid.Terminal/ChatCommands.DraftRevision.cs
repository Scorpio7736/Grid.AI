using System.Text;
using System.Text.RegularExpressions;

namespace Grid.Terminal;

public sealed partial class ChatCommands
{
    private CommandResult? ReplaceSavedPlan(string input)
    {
        var match = Regex.Match(input, @"^replace (?:this|the) plan with (.+)$", RegexOptions.IgnoreCase);
        if (!match.Success) return null;
        if (reviewedPlan is not { } plan || !Current(plan.Scope) || identity?.IsMaster != true || save is null)
            return Explain("Review a current saved plan first. Nothing changed.");
        if (assistanceDraft is not null || pendingIntent is not null || fileRequest is not null)
            return Explain("Finish or discard the other pending draft/request first. Saved plan unchanged.");
        var request = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(request) || Encoding.UTF8.GetByteCount(request) > 1200)
            return Explain("Give a complete replacement request within 1,200 UTF-8 bytes. Saved plan unchanged.");
        try
        {
            engine.CancelReviewedUnstartedPlan(identity.Reference, plan.Id, plan.Tasks);
            reviewedPlan = null; savedPlanChoices = null; followUp = null;
            save(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception error) when (error is IOException or Microsoft.Data.Sqlite.SqliteException)
        { return Explain("Saving cancellation failed; the old plan may be cancelled in memory. No replacement drafted. Resolve persistence and review current task state. " + TerminalTable.Safe(error.Message)); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        { return Explain("Plan replacement refused: " + TerminalTable.Safe(error.Message)); }
        var result = DirectDevelopmentPlan(request) ?? TalkHints(Assist(["propose", request], repairFormat: true));
        return result with { Text = "Old unapproved plan cancelled and saved; history retained. Replacement needs fresh submission and approval. If drafting fails, the old plan stays cancelled.\n" + result.Text };
    }

    private CommandResult? ReviseTaskDraft(string input)
    {
        var match = Regex.Match(input, @"^(?:revise|replace) (?<target>(?:this|the) draft|it|that)(?:\s*:\s*(?<request>.*)|\s+with\s+(?<request>.+))?$", RegexOptions.IgnoreCase);
        if (!match.Success) return null;
        var explicitDraft = match.Groups["target"].Value.Contains("draft", StringComparison.OrdinalIgnoreCase);
        if (!explicitDraft && (fileRequest is null ? 0 : 1) + (reviewedPlan is null ? 0 : 1) +
            (pendingIntent is null ? 0 : 1) + (assistanceDraft is null ? 0 : 1) > 1)
            return Explain("More than one operation is pending. Specify the file change or task draft. Nothing changed or executed.");
        if (!explicitDraft && (fileRequest is not null || reviewedPlan is not null || pendingIntent is not null)) return null;
        if (identity?.IsMaster != true) return Explain("Task draft revision requires Master Control.");
        if (assistanceDraft is not { } draft)
            return explicitDraft ? Explain("No unsubmitted task draft is selected. Saved plans are unchanged; review and cancel them separately before replacing their work.") : null;
        if (draft.Session != identity.Reference || draft.Guard.ConversationId != active ||
            draft.Guard.Activity != engine.ActivityGeneration || draft.Guard.Context != engine.ContextRevision ||
            draft.Guard.Memory != engine.ConversationMemoryRevision)
        {
            assistanceDraft = null; assistanceRequest++;
            return Explain("Task draft expired with its session or scope. Describe the request again. Nothing executed.");
        }
        var replacement = match.Groups["request"].Value.Trim();
        if (replacement.Length == 0)
            return Explain("What should the replacement draft do? Say revise this draft: followed by the complete revised request. The current draft is unchanged and nothing executed.");
        if (Encoding.UTF8.GetByteCount(replacement) > 1200)
            return Explain("Keep the replacement request within 1,200 UTF-8 bytes. The current draft is unchanged.");
        // A replacement is a new proposal, never authority to mutate saved tasks or run the old draft.
        assistanceDraft = null; assistanceRequest++; followUp = null;
        var result = DirectDevelopmentPlan(replacement) ?? TalkHints(Assist(["propose", replacement], repairFormat: true));
        return result with { Text = "Previous unsubmitted draft discarded. Replacement requires fresh submission, review and approval; saved plans are unchanged.\n" + result.Text };
    }
}
