using System.Text.Json;
using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class ScheduledWorkflowTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "grid-jobs-" + Guid.NewGuid().ToString("N"));
    private string State => Path.Combine(root, "jobs.json");
    public ScheduledWorkflowTests() => Directory.CreateDirectory(root);
    public void Dispose() { try { Directory.Delete(root, true); } catch { } }
    private static (GridSimulationEngine Engine, TerminalIdentitySession Identity) Fixture()
    { var identity = new TerminalIdentitySession(() => "owner"); var registry = new TerminalSessionRegistry(); registry.Add(identity); var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); identity.Attach(engine); identity.Login(); return (engine, identity); }
    private ScheduledWorkflowService Service(GridSimulationEngine engine) => new(State, engine.LocalOperator.Id, engine);
    private static ScheduledWorkflow Job(GridSimulationEngine engine, WorkflowScheduleKind schedule = WorkflowScheduleKind.Once, string action = "world.status", string condition = "always", int interval = 0) =>
        new(Guid.NewGuid(), "health", engine.LocalOperator.Id, engine.GetSnapshot().WorldId, "local-world", schedule, DateTimeOffset.UtcNow.AddMinutes(-5), interval, condition, action, "hello", false, 0, 0, DateTimeOffset.UtcNow.AddDays(1), null, WorkflowRunState.Never, "Never run", null, false);

    [Fact]
    public void OnceRequiresExplicitEnableAndRunsExactlyOnce()
    {
        var (engine, _) = Fixture(); var service = Service(engine); var job = service.Add(Job(engine));
        Assert.Equal(0, service.RunDue(DateTimeOffset.UtcNow)); service.Control(job.Id, "enable");
        Assert.Equal(1, service.RunDue(DateTimeOffset.UtcNow)); Assert.Equal(0, service.RunDue(DateTimeOffset.UtcNow.AddHours(1)));
        var state = new ScheduledWorkflowService(State, engine.LocalOperator.Id, engine).Snapshot(); var saved = Assert.Single(state.Jobs);
        Assert.False(saved.Enabled); Assert.Equal(WorkflowRunState.Completed, saved.LastState); Assert.Equal(engine.LocalOperator.Id, saved.OwnerId); Assert.Equal(engine.GetSnapshot().WorldId, saved.WorldId);
        Assert.Single(state.Inbox); Assert.Single(state.Receipts);
    }

    [Fact]
    public void LateRecurringWakeRunsOnceWithoutCatchUp()
    {
        var (engine, _) = Fixture(); var service = Service(engine); var job = service.Add(Job(engine, WorkflowScheduleKind.Recurring, interval: 60)); service.Control(job.Id, "enable");
        var late = DateTimeOffset.UtcNow.AddDays(2); Assert.Equal(1, service.RunDue(late)); Assert.Equal(0, service.RunDue(late));
        var saved = service.Snapshot().Jobs.Single(); Assert.Equal(late.AddMinutes(60), saved.NextRunUtc); Assert.Single(service.Snapshot().Receipts);
    }

    [Fact]
    public void ConditionalQuietExpiryAndCancelFailClosed()
    {
        var (engine, _) = Fixture(); engine.Pause(); var service = Service(engine);
        var conditional = service.Add(Job(engine, WorkflowScheduleKind.Conditional, condition: "world-running", interval: 5)); service.Control(conditional.Id, "enable");
        var now = DateTimeOffset.UtcNow; Assert.Equal(0, service.RunDue(now)); Assert.Equal(WorkflowRunState.Skipped, service.Snapshot().Jobs.Single().LastState);
        engine.Start(); Assert.Equal(1, service.RunDue(now.AddMinutes(6)));
        service.SetExpiration(conditional.Id, now.AddMinutes(1)); Assert.Equal(1, service.RunDue(now.AddMinutes(12))); Assert.Equal(WorkflowRunState.Expired, service.Snapshot().Jobs.Single().LastState);
        service.Control(conditional.Id, "cancel"); Assert.Equal(0, service.RunDue(now.AddDays(1))); Assert.Equal(WorkflowRunState.Cancelled, service.Snapshot().Jobs.Single().LastState);
    }

    [Fact]
    public void RestartInterruptsUncertainRunAndNeverReplays()
    {
        var (engine, _) = Fixture(); var service = Service(engine); var job = service.Add(Job(engine, WorkflowScheduleKind.Recurring, interval: 10)); service.Control(job.Id, "enable");
        var state = service.Snapshot(); var run = Guid.NewGuid(); state = state with { Jobs = [state.Jobs.Single() with { ActiveRunId = run }] };
        File.WriteAllText(State, JsonSerializer.Serialize(state)); var reopened = Service(engine); var saved = reopened.Snapshot();
        Assert.Equal(WorkflowRunState.Interrupted, saved.Jobs.Single().LastState); Assert.False(saved.Jobs.Single().Enabled); Assert.Single(saved.Inbox); Assert.Equal(0, reopened.RunDue(DateTimeOffset.UtcNow.AddDays(1)));
        reopened.Retry(job.Id, DateTimeOffset.UtcNow); Assert.Equal(1, reopened.RunDue(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void UnsupportedConsequentialActionsCannotBeInstalled()
    {
        var (engine, _) = Fixture(); var service = Service(engine);
        Assert.Throws<ArgumentException>(() => service.Add(Job(engine, action: "file.write")));
        var notification = service.Add(Job(engine, action: "notify")); service.Control(notification.Id, "enable"); Assert.Equal(1, service.RunDue(DateTimeOffset.UtcNow));
        Assert.Contains("Notification: hello", service.Snapshot().Inbox.Single().Summary);
    }

    [Fact]
    public void QuietHoursDeferWithoutCreatingNotificationNoise()
    {
        var (engine, _) = Fixture(); var service = Service(engine); var job = service.Add(Job(engine, WorkflowScheduleKind.Recurring, interval: 15)); service.Control(job.Id, "enable");
        var now = DateTimeOffset.UtcNow; service.SetQuietHours(job.Id, now.Hour, (now.Hour + 1) % 24);
        Assert.Equal(0, service.RunDue(now)); Assert.Empty(service.Snapshot().Inbox); Assert.Equal(WorkflowRunState.Skipped, service.Snapshot().Jobs.Single().LastState);
    }

    [Fact]
    public void CommandsUseExpiringPlanAndInstallDisabled()
    {
        var (engine, identity) = Fixture(); var service = Service(engine); var commands = DefaultCommands.Create(engine); ScheduledWorkflowCommands.Register(commands, engine, identity, service); commands.UseIdentity(identity);
        var plan = commands.ExecuteTokens(["schedule", "plan", "recurring", "health", "60", "world.status"]).Text; Assert.Contains("DRAFT ONLY", plan);
        var id = plan.Split("schedule add ").Last(); Assert.Contains("added disabled", commands.Execute("schedule add " + id).Text);
        Assert.False(service.Snapshot().Jobs.Single().Enabled); Assert.Contains("active host service", commands.Execute("schedule doctor").Text);
    }
}
