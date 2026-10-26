using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;

public sealed class TaskBatchTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "GridBatch-" + Guid.NewGuid().ToString("N"));
    private readonly TerminalSessionRegistry registry = new();
    private readonly TerminalIdentitySession owner = new(() => "batch-owner");
    private readonly GridSimulationEngine engine;
    private readonly WorldStore store;
    private readonly WorldInfo info;
    private readonly CommandDispatcher commands;
    public TaskBatchTests()
    {
        Directory.CreateDirectory(folder); registry.Add(owner);
        engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
        store = new(Path.Combine(folder,"world.db")); info = new(engine.GetSnapshot().WorldId,"Batch",DateTimeOffset.UtcNow); store.Create(info,engine.GetSnapshot());
        commands = Commands(owner);
    }
    private CommandDispatcher Commands(TerminalIdentitySession identity)
    { var c = DefaultCommands.Create(engine); TaskCommands.Register(c,engine,identity,Save); c.UseIdentity(identity); return c; }
    private Task Save(CancellationToken token) { token.ThrowIfCancellationRequested(); store.Save(info,engine.GetSnapshot()); return Task.CompletedTask; }
    private TaskRecord Propose(string role="MAXIS", string? capability=null, string argument="test") => engine.ProposeTask(owner.Reference,engine.ResolveProgram(role).Id,capability ?? role.ToLowerInvariant()+".demo",argument,Guid.NewGuid().ToString("N"));
    private TaskRecord Record(Guid id) => engine.GetSnapshot().Tasks.Records.Single(t=>t.Id==id);
    private CommandResult Confirm(string line) => commands.Execute(line).ConfirmTaskBatch!();
    public void Dispose() { store.Dispose(); Directory.Delete(folder,true); }

    [Fact]
    public async Task FilteredApprovalAndRunUseFrozenBatchesAndCannotDispatchTwice()
    {
        var a=Propose(); var other=Propose("JN");
        var preview=commands.Execute("tasks approve -all -maxis"); Assert.NotNull(preview.ConfirmTaskBatch);
        Assert.Contains(a.Argument,preview.Text); Assert.Equal(Status.AwaitingApproval,Record(a.Id).Status);
        var later=Propose(); Assert.Contains("1 tasks approved",preview.ConfirmTaskBatch!().Text);
        Assert.Contains("already consumed",preview.ConfirmTaskBatch!().Text);
        Assert.Equal(Status.AwaitingApproval,Record(later.Id).Status); Assert.Equal(Status.AwaitingApproval,Record(other.Id).Status);
        var batch=commands.Execute("tasks run -all -MAXIS").BrokerOperation!;
        engine.ApproveTask(owner.Reference,later.Id); engine.ApproveTask(owner.Reference,other.Id);
        var result=await batch(default); Assert.Contains("Completed: 1",result.Text);
        Assert.Equal(Status.Completed,Record(a.Id).Status); Assert.Equal(Status.Queued,Record(later.Id).Status); Assert.Equal(Status.Queued,Record(other.Id).Status);
        Assert.Contains("already consumed",(await batch(default)).Text); Assert.Equal(1,Record(a.Id).Attempts);
    }
    [Fact]
    public void ChangedPreviewRejectsWholeApprovalWithoutPartialMutation()
    {
        var a=Propose(); var b=Propose(); var preview=commands.Execute("tasks approve -all");
        engine.CancelTask(owner.Reference,b.Id);
        Assert.Contains("changed",preview.ConfirmTaskBatch!().Text);
        Assert.Equal(Status.AwaitingApproval,Record(a.Id).Status);
    }
    [Fact]
    public void RevokedWorkspaceRejectsWholeBatchBeforeApprovingAnyTask()
    {
        var source=Path.Combine(folder,"source"); Directory.CreateDirectory(source); File.WriteAllText(Path.Combine(source,"sample.csproj"),"<Project />");
        engine.ConfigureMaxisRuntime(Path.Combine(folder,"artifacts")); engine.RegisterDevelopmentWorkspace(owner.Reference,"sample",source,"sample.csproj");
        var a=Propose(); var b=Propose(capability:"maxis.build",argument:"sample"); var preview=commands.Execute("tasks approve -all");
        engine.RevokeDevelopmentWorkspace(owner.Reference,"sample");
        Assert.Contains("error",preview.ConfirmTaskBatch!().Text);
        Assert.Equal(Status.AwaitingApproval,Record(a.Id).Status); Assert.Equal(Status.AwaitingApproval,Record(b.Id).Status);
    }
    [Fact]
    public async Task ClearRetainsPlanAndRelayHistoryThroughSQLiteAndRestart()
    {
        var plan=engine.Coordinate(owner.Reference,"RELAY: notify retained-message","plan");
        Confirm("tasks approve -all"); await commands.Execute("tasks run -all").BrokerOperation!(default);
        var pending=Propose(); var preview=commands.Execute("tasks clear -all"); var later=Propose("JN");
        Assert.Contains("2 tasks archived",preview.ConfirmTaskBatch!().Text);
        Assert.True(Record(plan.TaskIds[0]).Archived); Assert.True(Record(pending.Id).Archived); Assert.Equal(Status.Cancelled,Record(pending.Id).Status);
        Assert.False(Record(later.Id).Archived);
        Assert.Contains(pending.Id.ToString()[..8],commands.Execute("tasks list").Text);
        Assert.Contains(pending.Id.ToString()[..8],commands.Execute("tasks list -archived -MAXIS").Text);
        Assert.Contains("Archived:    True",commands.Execute("tasks inspect "+pending.Id).Text);
        var restored=GridSimulationEngine.Restore(store.Load().State);
        Assert.Equal(plan.Id,Assert.Single(restored.GetSnapshot().Tasks.Plans).Id);
        Assert.Equal("retained-message",Assert.Single(restored.GetSnapshot().Tasks.Relay.Notifications).Text);
        Assert.Equal(2,restored.GetSnapshot().Tasks.Records.Count(t=>t.Archived));
        Assert.Throws<InvalidOperationException>(()=>engine.RetryTask(owner.Reference,pending.Id));
        Assert.Throws<InvalidOperationException>(()=>engine.ApproveTask(owner.Reference,pending.Id));
    }
    [Fact]
    public async Task RunningOrUnsettledAttemptRejectsClearWithoutHidingWork()
    {
        var task=Propose(capability:"maxis.delay",argument:"500"); engine.ApproveTask(owner.Reference,task.Id);
        var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var first=true;
        var run=engine.RunNextTaskAsync(owner.Reference,async token=> { if(first) {first=false;entered.SetResult();await release.Task.WaitAsync(token);} },default);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Null(commands.Execute("tasks clear -all").ConfirmTaskBatch);
            engine.CancelTask(owner.Reference,task.Id);
            Assert.Contains("settle",Confirm("tasks clear -all").Text); Assert.False(Record(task.Id).Archived);
        }
        finally { release.TrySetResult(); await run; }
        Assert.Contains("archived",Confirm("tasks clear -all").Text);
    }
    [Fact]
    public async Task TimeoutStopsBatchAndLeavesRemainingTasksQueued()
    {
        var first=Propose(capability:"maxis.delay",argument:"5000"); var second=Propose("JN"); Confirm("tasks approve -all");
        var result=await commands.Execute("tasks run -all").BrokerOperation!(default);
        Assert.Contains("Skipped: 1",result.Text); Assert.Contains("TimedOut",result.Text);
        Assert.Equal(Status.TimedOut,Record(first.Id).Status); Assert.Equal(Status.Queued,Record(second.Id).Status); Assert.Equal(0,Record(second.Id).Attempts);
    }
    [Fact]
    public async Task PreCancelledBatchDoesNotClaimAnyAttempt()
    {
        var a=Propose(); Confirm("tasks approve -all"); using var stop=new CancellationTokenSource();stop.Cancel();
        Assert.Contains("Skipped: 1",(await commands.Execute("tasks run -all").BrokerOperation!(stop.Token)).Text);
        Assert.Equal(Status.Queued,Record(a.Id).Status);Assert.Equal(0,Record(a.Id).Attempts);
    }
    [Fact]
    public void StaleSessionCannotConfirmPreview()
    {
        var a=Propose(); var preview=commands.Execute("tasks approve -all"); owner.Logout();owner.Login();
        Assert.Contains("error",preview.ConfirmTaskBatch!().Text);Assert.Equal(Status.AwaitingApproval,Record(a.Id).Status);
    }
    [Fact]
    public void AgentFiltersCannotExposeOtherRolesOrApproveClearRunBatches()
    {
        var own=Propose(); var other=Propose("JN"); var attachment=owner.CreateAttachment();registry.Add(attachment);attachment.Switch(engine.ResolveProgram("MAXIS").Id.ToString());
        var c=Commands(attachment);
        Assert.Contains(own.Id.ToString()[..8],c.Execute("tasks list -MAXIS").Text);
        Assert.DoesNotContain(other.Id.ToString()[..8],c.Execute("tasks list -JN").Text);
        foreach(var action in new[]{"approve","clear","run"}) Assert.Contains("denied",c.Execute("tasks "+action+" -all").Text);
        Assert.Throws<UnauthorizedAccessException>(()=>engine.UpdateTaskBatch(attachment.Reference,[own],true));
        var masterChild=owner.CreateAttachment();registry.Add(masterChild);
        Assert.Contains("original host",Commands(masterChild).Execute("tasks approve -all").Text);
    }
    [Theory]
    [InlineData("tasks list -unknown")]
    [InlineData("tasks list -MAXIS -JN")]
    [InlineData("tasks approve -all -all")]
    [InlineData("tasks run -all -unknown")]
    [InlineData("tasks clear")]
    [InlineData("tasks clear -MAXIS")]
    public void InvalidBatchSyntaxNeverMutates(string line)
    {
        var a=Propose();var result=commands.Execute(line);Assert.Null(result.ConfirmTaskBatch);Assert.Null(result.BrokerOperation);
        Assert.Contains("error",result.Text,StringComparison.OrdinalIgnoreCase);Assert.Equal(Status.AwaitingApproval,Record(a.Id).Status);
    }
    [Fact]
    public async Task HostRequiresInteractiveConfirmationAndHonorsNoOrYes()
    {
        var a=Propose();var output=new StringWriter();
        await new TerminalHost(engine,commands,new StringReader("tasks approve -all\nexit\n"),output,showPrompt:false).RunAsync();
        Assert.Contains("interactive confirmation",output.ToString());Assert.Equal(Status.AwaitingApproval,Record(a.Id).Status);
        await new TerminalHost(engine,commands,new StringReader("tasks approve -all\nn\nexit\n"),output,showPrompt:false,interactiveConfirmation:true).RunAsync();
        Assert.Equal(Status.AwaitingApproval,Record(a.Id).Status);
        await new TerminalHost(engine,commands,new StringReader("tasks approve -all\ny\nexit\n"),output,showPrompt:false,interactiveConfirmation:true).RunAsync();
        Assert.Equal(Status.Queued,Record(a.Id).Status);
    }
    [Fact]
    public async Task VersionNineteenLoadsMissingArchiveFieldWithoutChangingTaskIdentity()
    {
        var task=Propose();await Save(default);
        using(var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=store.DatabasePath,Pooling=false}.ToString()))
        {c.Open();using var q=c.CreateCommand();q.CommandText="UPDATE WorldTasks SET StateJson=json_remove(StateJson,'$.Records[0].Archived'); DELETE FROM SchemaMigrations WHERE Version>=20; PRAGMA user_version=19;";q.ExecuteNonQuery();}
        var loaded=store.Load().State;Assert.Equal(task.Id,Assert.Single(loaded.Tasks.Records).Id);Assert.False(loaded.Tasks.Records[0].Archived);
    }
    [Fact]
    public async Task SaveFailureStopsDispatchBeforeLaterTasksAndReportsUncertainty()
    {
        var first=Propose();var second=Propose("JN");Confirm("tasks approve -all");
        var failing=DefaultCommands.Create(engine);TaskCommands.Register(failing,engine,owner,_=>Task.FromException(new IOException("disk unavailable")));failing.UseIdentity(owner);
        var result=await failing.Execute("tasks run -all").BrokerOperation!(default);
        Assert.Contains("Outcome not confirmed",result.Text);Assert.Contains("Skipped: 1",result.Text);
        Assert.Equal(Status.Failed,Record(first.Id).Status);Assert.Equal(0,Record(second.Id).Attempts);
    }
    [Fact]
    public void ClearFilterAndStalePreviewKeepOtherRecordsVisible()
    {
        var first=Propose();var other=Propose("JN");var preview=commands.Execute("tasks clear -all -MAXIS");
        engine.ApproveTask(owner.Reference,first.Id);
        Assert.Contains("changed",preview.ConfirmTaskBatch!().Text);Assert.False(Record(first.Id).Archived);
        Assert.Contains("1 tasks archived",Confirm("tasks clear -all -maxis").Text);
        Assert.False(Record(other.Id).Archived);Assert.Contains(other.Id.ToString()[..8],commands.Execute("tasks list -JN").Text);
        Assert.Contains("tasks run -all -MAXIS",CommandHelp.Complete("tasks run -all -M",commands.Commands));
    }

    [Fact]
    public void ArchiveValidationRejectsActiveOrUnauditedArchives()
    {
        var task=Propose();var snapshot=engine.GetSnapshot();
        Assert.Throws<InvalidDataException>(()=>GridSimulationEngine.Restore(snapshot with{Tasks=snapshot.Tasks with{Records=[task with{Archived=true}]}}));
    }
    [Fact]
    public async Task ChangedQueuedTaskCannotBeReusedByOldRunBatch()
    {
        var a=Propose();Confirm("tasks approve -all");var batch=commands.Execute("tasks run -all").BrokerOperation!;
        engine.CancelTask(owner.Reference,a.Id);
        Assert.Contains("Stopped",(await batch(default)).Text);Assert.Equal(0,Record(a.Id).Attempts);
    }

    [Fact]
    public async Task NewTaskViewsRetainArchivesAndDistinguishDeniedFromCancelled()
    {
        var denied = Propose(); var completed = Propose(); var cancelled = Propose("JN"); var pending = Propose();
        Assert.Contains("denied and saved", commands.Execute($"tasks deny {denied.Id} \"Not needed\"").Text);
        engine.ApproveTask(owner.Reference, completed.Id); await engine.RunNextTaskAsync(owner.Reference, Save, default);
        engine.CancelTask(owner.Reference, cancelled.Id);
        engine.UpdateTaskBatch(owner.Reference, [Record(completed.Id)], true);
        string Ref(TaskRecord t) => t.Id.ToString()[..8];
        foreach (var t in new[] { denied, completed, cancelled, pending }) Assert.Contains(Ref(t), commands.Execute("tasks").Text);
        var done = commands.Execute("tasks -completed -MAXIS").Text;
        Assert.Contains(Ref(completed), done); Assert.Contains("Yes", done); Assert.DoesNotContain(Ref(denied), done);
        var open = commands.Execute("tasks -list").Text;
        Assert.Contains(Ref(cancelled), open); Assert.Contains(Ref(pending), open);
        Assert.DoesNotContain(Ref(denied), open); Assert.DoesNotContain(Ref(completed), open);
        Assert.Contains("error", commands.Execute("tasks -completed -list").Text);
    }
    [Fact]
    public void DenialRequiresHumanReasonAndCannotBeRetriedOrApproved()
    {
        var task = Propose();
        var child = owner.CreateAttachment(); registry.Add(child); child.Switch(engine.ResolveProgram("MAXIS").Id.ToString());
        Assert.Throws<UnauthorizedAccessException>(() => engine.DenyTask(child.Reference, task.Id, "no"));
        Assert.Throws<ArgumentException>(() => engine.DenyTask(owner.Reference, task.Id, ""));
        engine.DenyTask(owner.Reference, task.Id, "Not requested");
        Assert.Equal(Status.Denied, Record(task.Id).Status); Assert.Equal("Not requested", Record(task.Id).Result);
        Assert.Null(Record(task.Id).ApprovedBy);
        Assert.Throws<InvalidOperationException>(() => engine.ApproveTask(owner.Reference, task.Id));
        Assert.Throws<InvalidOperationException>(() => engine.RetryTask(owner.Reference, task.Id));
        Assert.Throws<InvalidOperationException>(() => engine.CancelTask(owner.Reference, task.Id));
        var queued = Propose(); engine.ApproveTask(owner.Reference, queued.Id);
        Assert.Throws<InvalidOperationException>(() => engine.DenyTask(owner.Reference, queued.Id, "too late"));
    }
    [Fact]
    public async Task DenialSurvivesSQLiteAndVersionTwentyUpgradePreservesHistory()
    {
        var task = Propose(); await Save(default); store.Dispose();
        using (var db = new SqliteConnection("Pooling=False;Data Source=" + Path.Combine(folder,"world.db")))
        {
            db.Open(); using var command = db.CreateCommand();
            command.CommandText = "DELETE FROM SchemaMigrations WHERE Version >= 21; PRAGMA user_version=20;"; command.ExecuteNonQuery();
        }
        using var upgraded = new WorldStore(Path.Combine(folder,"world.db"));
        var saved = upgraded.Load(); Assert.Equal(Status.AwaitingApproval, Assert.Single(saved.State.Tasks.Records).Status);
        engine.DenyTask(owner.Reference, task.Id, "Human declined"); upgraded.Save(info, engine.GetSnapshot());
        var restored = GridSimulationEngine.Restore(upgraded.Load().State);
        var denied = Assert.Single(restored.GetSnapshot().Tasks.Records);
        Assert.Equal(Status.Denied, denied.Status); Assert.Equal("Human declined", denied.Result);
        Assert.Equal("Denied", denied.Audit.Last().Action);
        Assert.Equal(task.Id, denied.Id);
    }
}
