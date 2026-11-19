using System.Globalization;
using Grid.Simulation;

namespace Grid.Terminal;

public static class ScheduledWorkflowCommands
{
    private sealed record Pending(Guid Id, ScheduledWorkflow Job, long Generation, DateTimeOffset Expires);
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession identity, ScheduledWorkflowService service)
    {
        Pending? pending = null; identity.Changed += () => pending = null;
        commands.Register(new("schedule", "schedule <list|show|plan|add|enable|disable|cancel|remove|quiet|expiry|inbox|retry|run-due|doctor> ...",
            "Durable offline one-time, recurring and conditional workflows with explicit Master Control lifecycle.", args =>
        {
            if (!identity.IsMaster || identity.IsAttachment) return new("Scheduled workflows require authenticated Master Control in the original terminal.");
            try
            {
                var state = service.Snapshot();
                if (args.Count == 0 || args.SequenceEqual(["list"]))
                    return new(state.Jobs.Length == 0 ? "No scheduled workflows." : TerminalTable.Render(["ID", "Name", "Schedule", "Next UTC", "Enabled", "Last"], state.Jobs.Select(job => new[] { job.Id.ToString(), job.Name, job.Schedule.ToString(), job.NextRunUtc.ToString("O"), job.Enabled.ToString(), job.LastState.ToString() })));
                if (args.Count == 2 && args[0] == "show")
                {
                    var job = Resolve(state, args[1]);
                    return new($"{job.Name} | {job.Id}\nOwner: {job.OwnerId} | World: {job.WorldId} | Scope: {job.Scope}\n{job.Schedule} | next {job.NextRunUtc:O} | interval {job.IntervalMinutes}m | condition {job.Condition}\nAction: {job.Action} | enabled {job.Enabled} | cancelled {job.Cancelled} | expires {job.ExpiresUtc:O}\nQuiet UTC: {job.QuietStartUtcHour:00}:00-{job.QuietEndUtcHour:00}:00 | last {job.LastState} at {job.LastRunUtc:O}\nResult: {job.LastResult}");
                }
                if (args.Count >= 5 && args[0] == "plan")
                {
                    var schedule = args[1] switch { "once" => WorkflowScheduleKind.Once, "recurring" => WorkflowScheduleKind.Recurring, "conditional" => WorkflowScheduleKind.Conditional, _ => throw new ArgumentException("Schedule must be once, recurring or conditional.") };
                    var name = args[2]; var position = 3; DateTimeOffset next; var interval = 0; var condition = "always";
                    if (schedule == WorkflowScheduleKind.Once)
                    { if (!DateTimeOffset.TryParse(args[position++], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out next)) throw new ArgumentException("Use an ISO-8601 UTC time."); next = next.ToUniversalTime(); }
                    else
                    { if (!int.TryParse(args[position++], out interval)) throw new ArgumentException("Interval must be minutes."); next = DateTimeOffset.UtcNow.AddMinutes(interval); if (schedule == WorkflowScheduleKind.Conditional) condition = args[position++]; }
                    if (position >= args.Count) throw new ArgumentException("Action is required."); var action = args[position++]; var argument = string.Join(" ", args.Skip(position));
                    var job = new ScheduledWorkflow(Guid.NewGuid(), name, engine.LocalOperator.Id, engine.GetSnapshot().WorldId, "local-world", schedule, next, interval, condition, action, argument, false, 0, 0, DateTimeOffset.UtcNow.AddDays(30), null, WorkflowRunState.Never, "Never run", null, false);
                    pending = new(Guid.NewGuid(), job, identity.Generation, DateTimeOffset.UtcNow.AddMinutes(2));
                    return new($"DRAFT ONLY - no workflow created.\n{job.Name} | {job.Schedule} | next {job.NextRunUtc:O} | condition {job.Condition} | {job.Action}\nExpires in 30 days; quiet hours disabled; installs disabled. Only bounded offline reads/local notifications are supported.\nTo add this exact plan within two minutes: schedule add {pending.Id}");
                }
                if (args.Count == 2 && args[0] == "add")
                {
                    var plan = pending; pending = null;
                    if (plan is null || !Guid.TryParse(args[1], out var id) || id != plan.Id || plan.Generation != identity.Generation || DateTimeOffset.UtcNow >= plan.Expires)
                        return new("Workflow plan missing, expired or invalidated. Nothing added.");
                    var added = service.Add(plan.Job); return new($"Workflow added disabled: {added.Id}. Review with schedule show {added.Id}, then explicitly enable it.");
                }
                if (args.Count == 2 && args[0] is "enable" or "disable" or "cancel" or "remove")
                { var job = Resolve(state, args[1]); service.Control(job.Id, args[0]); return new($"Workflow {args[0]} completed: {job.Id}."); }
                if (args.Count == 4 && args[0] == "quiet" && int.TryParse(args[2], out var start) && int.TryParse(args[3], out var end))
                { var job = Resolve(state, args[1]); service.SetQuietHours(job.Id, start, end); return new("UTC quiet hours updated; equal values disable quiet hours."); }
                if (args.Count == 3 && args[0] == "expiry" && DateTimeOffset.TryParse(args[2], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiration))
                { var job = Resolve(state, args[1]); service.SetExpiration(job.Id, expiration); return new("Workflow expiration updated."); }
                if (args.Count == 1 && args[0] == "inbox")
                    return new(state.Inbox.Length == 0 ? "Workflow inbox is empty." : TerminalTable.Render(["Time UTC", "Job", "State", "Summary"], state.Inbox.OrderByDescending(item => item.AtUtc).Select(item => new[] { item.AtUtc.ToString("O"), item.JobId.ToString(), item.State.ToString(), item.Summary })));
                if (args.Count == 2 && args[0] == "inbox" && args[1] == "read") { service.MarkInboxRead(); return new("Workflow inbox marked read; durable receipts retained."); }
                if (args.Count == 2 && args[0] == "retry") { var job = Resolve(state, args[1]); service.Retry(job.Id, DateTimeOffset.UtcNow); return new("Failed/interrupted workflow queued once for the next host-service pass. It was not executed yet."); }
                if (args.Count == 1 && args[0] == "run-due") return new($"Host-service pass complete; {service.RunDue(DateTimeOffset.UtcNow)} due workflow(s) attempted. No offline catch-up loop was run.");
                if (args.Count == 1 && args[0] == "doctor") return new($"Scheduler: {(state.Paused ? "paused" : "active host service")} | jobs {state.Jobs.Length}/64 | inbox {state.Inbox.Length}/256 | receipts {state.Receipts.Length}/500\nActions: world.status, resources.status, notify. No files, processes, model inference, network, automatic approvals or simulated-tick coupling. Maximum eight due jobs per 30-second pass; late recurring jobs run once, never catch up repeatedly.");
                return new("Usage:\n  schedule list\n  schedule show <id|name>\n  schedule plan once <name> <ISO-UTC> <world.status|resources.status|notify> [text]\n  schedule plan recurring <name> <minutes> <action> [text]\n  schedule plan conditional <name> <minutes> <always|world-running|world-paused> <action> [text]\n  schedule add <plan-id> | enable|disable|cancel|remove <id|name>\n  schedule quiet <id|name> <start-hour-utc> <end-hour-utc>\n  schedule expiry <id|name> <ISO-UTC>\n  schedule inbox [read] | retry <id|name> | run-due | doctor\nExample: schedule plan recurring health 60 world.status");
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or InvalidDataException or IOException or UnauthorizedAccessException) { return new("Scheduled workflow error: " + error.Message); }
        }));
    }
    private static ScheduledWorkflow Resolve(ScheduledWorkflowState state, string query)
    {
        var matches = Guid.TryParse(query, out var id) ? state.Jobs.Where(job => job.Id == id).ToArray() : state.Jobs.Where(job => job.Name.Equals(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        return matches.Length == 1 ? matches[0] : throw new ArgumentException(matches.Length == 0 ? "Unknown workflow." : "Ambiguous workflow name; use the full ID.");
    }
}
