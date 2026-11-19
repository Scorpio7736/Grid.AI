using System.Text.Json;
using System.Text.Json.Serialization;
using Grid.Simulation;

namespace Grid.Terminal;

public enum WorkflowScheduleKind { Once, Recurring, Conditional }
public enum WorkflowRunState { Never, Completed, Failed, Interrupted, Cancelled, Expired, Skipped }

public sealed record ScheduledWorkflow(Guid Id, string Name, Guid OwnerId, Guid WorldId, string Scope,
    WorkflowScheduleKind Schedule, DateTimeOffset NextRunUtc, int IntervalMinutes, string Condition,
    string Action, string Argument, bool Enabled, int QuietStartUtcHour, int QuietEndUtcHour,
    DateTimeOffset ExpiresUtc, DateTimeOffset? LastRunUtc, WorkflowRunState LastState, string LastResult,
    Guid? ActiveRunId, bool Cancelled);
public sealed record WorkflowInboxItem(Guid Id, Guid JobId, Guid RunId, DateTimeOffset AtUtc,
    WorkflowRunState State, string Summary, bool Read);
public sealed record WorkflowReceipt(Guid RunId, Guid JobId, DateTimeOffset StartedUtc,
    DateTimeOffset FinishedUtc, WorkflowRunState State);
public sealed record ScheduledWorkflowState(int Version, bool Paused, ScheduledWorkflow[] Jobs,
    WorkflowInboxItem[] Inbox, WorkflowReceipt[] Receipts);

/// <summary>Durable, bounded, offline scheduler for explicitly configured local read/notification jobs.</summary>
public sealed class ScheduledWorkflowService
{
    private readonly string path; private readonly Guid ownerId; private readonly Guid worldId;
    private readonly GridSimulationEngine engine; private readonly object gate = new();
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, MaxDepth = 16, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public ScheduledWorkflowService(string path, Guid ownerId, GridSimulationEngine engine)
    {
        this.path = Path.GetFullPath(path); GridTransfer.CheckPath(this.path); this.ownerId = ownerId;
        this.engine = engine; worldId = engine.GetSnapshot().WorldId; RecoverInterrupted();
    }
    public ScheduledWorkflowState Snapshot() { lock (gate) return Read(); }
    public ScheduledWorkflow Add(ScheduledWorkflow job)
    {
        lock (gate)
        {
            Validate(job); var state = Read(); if (state.Jobs.Length >= 64) throw new InvalidOperationException("Scheduled workflow limit reached.");
            if (state.Jobs.Any(value => value.Id == job.Id || value.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Workflow ID or name already exists.");
            job = job with { OwnerId = ownerId, WorldId = worldId, Scope = "local-world", Enabled = false, ActiveRunId = null, Cancelled = false, LastState = WorkflowRunState.Never, LastResult = "Never run" };
            Save(state with { Jobs = state.Jobs.Append(job).ToArray() }); return job;
        }
    }
    public void Control(Guid id, string action)
    {
        lock (gate)
        {
            var state = Read(); var job = Find(state, id); var next = action switch
            {
                "enable" when !job.Cancelled => job with { Enabled = true },
                "disable" => job with { Enabled = false },
                "cancel" => job with { Enabled = false, Cancelled = true, ActiveRunId = null, LastState = WorkflowRunState.Cancelled, LastResult = "Cancelled by Master Control" },
                "remove" => null,
                _ => throw new ArgumentException("Workflow cannot perform that lifecycle action.")
            };
            Save(state with { Jobs = state.Jobs.Where(value => value.Id != id).Concat(next is null ? [] : [next]).OrderBy(value => value.NextRunUtc).ToArray() });
        }
    }
    public void SetQuietHours(Guid id, int start, int end)
    {
        if (start is < 0 or > 23 || end is < 0 or > 23) throw new ArgumentException("Quiet hours must be UTC hours 0..23; equal values disable quiet hours.");
        Update(id, job => job with { QuietStartUtcHour = start, QuietEndUtcHour = end });
    }
    public void SetExpiration(Guid id, DateTimeOffset expiration) => Update(id, job => job with { ExpiresUtc = expiration.ToUniversalTime() });
    public void SetPaused(bool paused)
    {
        lock (gate) { var state = Read(); Save(state with { Paused = paused, Jobs = paused ? state.Jobs.Select(job => job.ActiveRunId is null ? job : job with { ActiveRunId = null, LastState = WorkflowRunState.Interrupted, LastResult = "Interrupted by host pause" }).ToArray() : state.Jobs }); }
    }
    public int RunDue(DateTimeOffset now)
    {
        lock (gate)
        {
            var state = Read(); if (state.Paused) return 0; var executed = 0;
            foreach (var original in state.Jobs.Where(job => job.Enabled && !job.Cancelled && job.NextRunUtc <= now).OrderBy(job => job.NextRunUtc).Take(8).ToArray())
            {
                var job = original; var run = Guid.NewGuid();
                if (job.OwnerId != ownerId || job.WorldId != worldId) { state = Finish(state, job, run, now, WorkflowRunState.Failed, "Ownership or world binding changed; nothing executed."); executed++; continue; }
                if (now >= job.ExpiresUtc) { state = Finish(state, job with { Enabled = false }, run, now, WorkflowRunState.Expired, "Schedule expired; nothing executed."); executed++; continue; }
                if (Quiet(job, now.Hour)) { state = Replace(state, Advance(job, now) with { LastState = WorkflowRunState.Skipped, LastResult = "Quiet hours active" }); continue; }
                if (!Condition(job)) { state = Replace(state, Advance(job, now) with { LastState = WorkflowRunState.Skipped, LastResult = "Condition not met" }); continue; }
                job = job with { ActiveRunId = run }; state = Replace(state, job); Save(state); // durable start marker
                try { state = Finish(state, job, run, now, WorkflowRunState.Completed, Execute(job)); }
                catch (Exception error) when (error is InvalidOperationException or ArgumentException) { state = Finish(state, job, run, now, WorkflowRunState.Failed, error.Message); }
                executed++;
            }
            Save(state); return executed;
        }
    }
    public void Retry(Guid id, DateTimeOffset now)
    {
        Update(id, job => job.LastState is WorkflowRunState.Failed or WorkflowRunState.Interrupted
            ? job with { Enabled = true, Cancelled = false, NextRunUtc = now, ActiveRunId = null }
            : throw new ArgumentException("Only failed or interrupted workflows can be retried."));
    }
    public void MarkInboxRead()
    { lock (gate) { var state = Read(); Save(state with { Inbox = state.Inbox.Select(item => item with { Read = true }).ToArray() }); } }
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try { RunDue(DateTimeOffset.UtcNow); while (await timer.WaitForNextTickAsync(cancellationToken)) RunDue(DateTimeOffset.UtcNow); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
    private string Execute(ScheduledWorkflow job)
    {
        var snapshot = engine.GetSnapshot();
        return job.Action switch
        {
            "world.status" => $"World {(snapshot.IsRunning ? "running" : "paused")}; tick {snapshot.TickCount}; districts {snapshot.Districts.Count}; programs {snapshot.Programs.Count}.",
            "resources.status" => Bound(ResourcesCommand.Execute(engine, []).Text, 1000),
            "notify" => "Notification: " + Bound(job.Argument, 300),
            _ => throw new InvalidOperationException("Unsupported action. Scheduled work cannot execute files, processes, models, network calls or consequential host changes; use a fresh reviewed approval workflow.")
        };
    }
    private bool Condition(ScheduledWorkflow job) => job.Condition switch { "always" => true, "world-running" => engine.GetSnapshot().IsRunning, "world-paused" => !engine.GetSnapshot().IsRunning, _ => false };
    private static bool Quiet(ScheduledWorkflow job, int hour) => job.QuietStartUtcHour != job.QuietEndUtcHour && (job.QuietStartUtcHour < job.QuietEndUtcHour ? hour >= job.QuietStartUtcHour && hour < job.QuietEndUtcHour : hour >= job.QuietStartUtcHour || hour < job.QuietEndUtcHour);
    private static ScheduledWorkflow Advance(ScheduledWorkflow job, DateTimeOffset now) => job.Schedule == WorkflowScheduleKind.Once ? job with { Enabled = false } : job with { NextRunUtc = now.AddMinutes(job.IntervalMinutes) };
    private static string Bound(string text, int limit) { text = new(text.Select(value => char.IsControl(value) && value is not '\n' and not '\r' and not '\t' ? ' ' : value).ToArray()); return text.Length <= limit ? text : text[..limit] + "..."; }
    private ScheduledWorkflowState Finish(ScheduledWorkflowState state, ScheduledWorkflow job, Guid run, DateTimeOffset now, WorkflowRunState outcome, string result)
    {
        result = Bound(result, 1000); job = Advance(job, now) with { ActiveRunId = null, LastRunUtc = now, LastState = outcome, LastResult = result };
        var inbox = state.Inbox.Append(new WorkflowInboxItem(Guid.NewGuid(), job.Id, run, now, outcome, result, false)).TakeLast(256).ToArray();
        var receipts = state.Receipts.Append(new WorkflowReceipt(run, job.Id, now, DateTimeOffset.UtcNow, outcome)).TakeLast(500).ToArray();
        return state with { Jobs = Replace(state, job).Jobs, Inbox = inbox, Receipts = receipts };
    }
    private void RecoverInterrupted()
    {
        lock (gate)
        {
            if (!File.Exists(path)) return; var state = Read();
            foreach (var job in state.Jobs.Where(value => value.ActiveRunId is not null).ToArray())
            {
                var run = job.ActiveRunId!.Value; state = Finish(state, job with { Enabled = false, ActiveRunId = null }, run, DateTimeOffset.UtcNow, WorkflowRunState.Interrupted, "Prior host stopped during this run; no automatic replay.");
            }
            Save(state);
        }
    }
    private void Update(Guid id, Func<ScheduledWorkflow, ScheduledWorkflow> change)
    { lock (gate) { var state = Read(); var next = change(Find(state, id)); Validate(next); Save(Replace(state, next)); } }
    private static ScheduledWorkflow Find(ScheduledWorkflowState state, Guid id) => state.Jobs.SingleOrDefault(value => value.Id == id) ?? throw new ArgumentException("Unknown workflow.");
    private static ScheduledWorkflowState Replace(ScheduledWorkflowState state, ScheduledWorkflow job) => state with { Jobs = state.Jobs.Where(value => value.Id != job.Id).Append(job).OrderBy(value => value.NextRunUtc).ToArray() };
    private static void Validate(ScheduledWorkflow job)
    {
        if (job.Id == Guid.Empty || string.IsNullOrWhiteSpace(job.Name) || job.Name.Length > 60 || job.Name.Any(char.IsControl)) throw new ArgumentException("Workflow name or ID is invalid.");
        if (job.Action is not ("world.status" or "resources.status" or "notify")) throw new ArgumentException("Allowed offline actions: world.status, resources.status, notify.");
        if (job.Condition is not ("always" or "world-running" or "world-paused")) throw new ArgumentException("Allowed conditions: always, world-running, world-paused.");
        if (job.Schedule != WorkflowScheduleKind.Once && job.IntervalMinutes is < 1 or > 10080) throw new ArgumentException("Recurring interval must be 1..10080 minutes.");
        if (job.Argument.Length > 300) throw new ArgumentException("Workflow argument exceeds 300 characters.");
    }
    private ScheduledWorkflowState Read()
    {
        if (!File.Exists(path)) return new(1, false, [], [], []);
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read); if (file.Length > 2 * 1024 * 1024) throw new InvalidDataException("Scheduled workflow state exceeds its limit.");
        var state = JsonSerializer.Deserialize<ScheduledWorkflowState>(file, Json) ?? throw new InvalidDataException("Invalid scheduled workflow state.");
        if (state.Version != 1 || state.Jobs.Length > 64 || state.Inbox.Length > 256 || state.Receipts.Length > 500) throw new InvalidDataException("Unsupported or oversized scheduled workflow state.");
        foreach (var job in state.Jobs) Validate(job); return state;
    }
    private void Save(ScheduledWorkflowState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(state, Json)); File.Move(temporary, path, true); }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
    }
}
