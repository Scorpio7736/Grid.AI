using Grid.Core;
using Grid.Communication;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Resources;
using Grid.Simulation.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;
using Status=Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;
public sealed class VoltRelayTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now=new(2026,10,21,12,0,0,TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow()=>Now;
    }
    private sealed class Voice : IRelayVoice
    {
        public int Calls; public string? Text; public bool Fail; public bool Wait;
        public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task SpeakAsync(string text,VoiceProfile? profile,CancellationToken token)
        { Calls++; Text=text; Started.TrySetResult(); if(Fail) throw new IOException("audio unavailable"); if(Wait) await Task.Delay(Timeout.Infinite,token); token.ThrowIfCancellationRequested(); }
    }
    private readonly Clock clock=new();
    private readonly TerminalSessionRegistry registry=new();
    private readonly TerminalIdentitySession owner=new(()=>"volt-relay-owner");
    private readonly GridSimulationEngine engine;
    public VoltRelayTests()
    {
        registry.Add(owner); engine=new(GridWorldFactory.CreateGenesis(),timeProvider:clock,editAuthorizer:registry); owner.Attach(engine); owner.Login(); engine.InitializeSystemAgents(owner.Reference);
    }
    private Task Save(CancellationToken token) { token.ThrowIfCancellationRequested(); GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot()); return Task.CompletedTask; }
    private TaskRecord Propose(string role,string capability,string argument)=>engine.ProposeTask(owner.Reference,engine.ResolveProgram(role).Id,capability,argument,Guid.NewGuid().ToString("N"));
    private async Task<TaskRecord> Run(string role,string capability,string argument)
    {
        var task=Propose(role,capability,argument); engine.ApproveTask(owner.Reference,task.Id); await engine.RunNextTaskAsync(owner.Reference,Save,default);
        return engine.GetSnapshot().Tasks.Records.Single(t=>t.Id==task.Id);
    }
    private void Telemetry(double cpu=80,double ram=500,double gpu=90)
    {
        engine.UpdateHostResources([
            new(ResourceMetric.CpuAvailablePercent,"cpu:primary-group",cpu,ObservationStatus.Available,clock.Now,"fixture"),
            new(ResourceMetric.RamAvailableBytes,"host",ram,ObservationStatus.Available,clock.Now,"fixture"),
            new(ResourceMetric.RamCapacityBytes,"host",1000,ObservationStatus.Available,clock.Now,"fixture"),
            new(ResourceMetric.GpuAvailablePercent,"gpu",gpu,ObservationStatus.Available,clock.Now,"fixture")]);
    }
    [Fact]
    public async Task ThrottleRequiresApprovalAndChangesOnlyAutomaticWait()
    {
        var task=Propose("VOLT","volt.throttle","eco"); Assert.Equal(engine.TickDuration,engine.AutomaticTickDelay);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>engine.RunNextTaskAsync(owner.Reference,Save,default));
        engine.ApproveTask(owner.Reference,task.Id); await engine.RunNextTaskAsync(owner.Reference,Save,default);
        Assert.Equal(engine.TickDuration*4,engine.AutomaticTickDelay); engine.Step(); Assert.Equal(engine.TickDuration,engine.ElapsedSimulationTime);
        await Run("VOLT","volt.throttle","normal"); Assert.Equal(engine.TickDuration,engine.AutomaticTickDelay);
    }
    [Fact]
    public async Task AutoPacingHandlesPressureHoldAndExpiredTelemetry()
    {
        await Run("VOLT","volt.throttle","auto"); Assert.Equal(engine.TickDuration*2,engine.AutomaticTickDelay);
        Telemetry(cpu:5); Assert.Equal(engine.TickDuration*4,engine.AutomaticTickDelay);
        clock.Now=clock.Now.AddSeconds(5); Telemetry(); Assert.Equal(engine.TickDuration*4,engine.AutomaticTickDelay);
        clock.Now=clock.Now.AddSeconds(6); Telemetry(); Assert.Equal(engine.TickDuration,engine.AutomaticTickDelay);
        clock.Now=clock.Now.AddSeconds(11); Assert.Equal(engine.TickDuration*2,engine.AutomaticTickDelay);
        Assert.Contains("Stale",engine.VoltStatus(owner.Reference)); Assert.Contains("unknown",engine.VoltStatus(owner.Reference));
    }
    [Theory]
    [InlineData(80,50,90)]
    [InlineData(80,500,5)]
    public async Task RamOrSupportedGpuPressureSlowsGrid(double cpu,double ram,double gpu)
    { await Run("VOLT","volt.throttle","auto"); Telemetry(cpu,ram,gpu); Assert.Equal(engine.TickDuration*4,engine.AutomaticTickDelay); }
    [Fact]
    public async Task UnsupportedGpuRemainsUnknownWithoutInventedPressure()
    {
        Telemetry(); var readings=engine.GetHostResources().Where(r=>r.Metric!=ResourceMetric.GpuAvailablePercent).Append(new ResourceReading(ResourceMetric.GpuAvailablePercent,"gpu",null,ObservationStatus.Unsupported,clock.Now,"fixture")).ToArray(); engine.UpdateHostResources(readings);
        await Run("VOLT","volt.throttle","auto"); Assert.Equal(engine.TickDuration,engine.AutomaticTickDelay); Assert.Contains("Unsupported",engine.VoltStatus(owner.Reference));
    }
    [Fact]
    public async Task NotificationDeliveryAndAcknowledgementAreSeparateAndNoSpeechOccurs()
    {
        var voice=new Voice(); engine.ConfigureRelayVoice(voice); await Run("RELAY","relay.notify","hello GRID");
        var state=engine.RelayFor(owner.Reference); var note=Assert.Single(state.Notifications); Assert.Null(note.AcknowledgedBy);
        Assert.Equal(RelayDeliveryStatus.Delivered,Assert.Single(state.Deliveries).Status); Assert.Equal(0,voice.Calls);
        engine.AcknowledgeRelay(owner.Reference,note.Id); Assert.NotNull(engine.RelayFor(owner.Reference).Notifications.Single().AcknowledgedBy);
    }
    [Fact]
    public async Task BriefingCopiesActualRecordedResultAndRejectsPendingWork()
    {
        var source=await Run("VOLT","volt.status","current"); await Run("RELAY","relay.brief",source.Id.ToString());
        var note=Assert.Single(engine.RelayFor(owner.Reference).Notifications); Assert.Contains(source.Result!,note.Text); Assert.Equal(source.Id,note.SourceTaskId);
        var pending=Propose("MAXIS","maxis.demo","not run"); var brief=Propose("RELAY","relay.brief",pending.Id.ToString()); engine.ApproveTask(owner.Reference,brief.Id); await engine.RunNextTaskAsync(owner.Reference,Save,default);
        Assert.Equal(Status.Failed,engine.GetSnapshot().Tasks.Records.Single(t=>t.Id==brief.Id).Status); Assert.Single(engine.RelayFor(owner.Reference).Notifications);
    }
    [Fact]
    public async Task SpeechRequiresExplicitTaskAndTracksProviderSuccessOrFailure()
    {
        await Run("RELAY","relay.notify","reviewed speech"); var note=engine.RelayFor(owner.Reference).Notifications.Single(); var voice=new Voice(); engine.ConfigureRelayVoice(voice);
        var speech=await Run("RELAY","relay.speak",note.Id.ToString()); Assert.Equal(Status.Completed,speech.Status); Assert.Equal("reviewed speech",voice.Text);
        voice.Fail=true; var failure=await Run("RELAY","relay.speak",note.Id.ToString()); Assert.Equal(Status.Failed,failure.Status);
        Assert.Equal(new[]{RelayDeliveryStatus.Delivered,RelayDeliveryStatus.Failed},engine.RelayFor(owner.Reference).Deliveries.Where(d=>d.Channel=="speech").Select(d=>d.Status));
    }
    [Fact]
    public async Task FailedDeliveryIntentSavePreventsAudioAndRetryDoesNotDuplicateInbox()
    {
        var voice=new Voice(); engine.ConfigureRelayVoice(voice); var notify=Propose("RELAY","relay.notify","hello"); engine.ApproveTask(owner.Reference,notify.Id); var saves=0;
        await engine.RunNextTaskAsync(owner.Reference,_=>++saves==2 ? Task.FromException(new IOException("intent failed")) : Task.CompletedTask,default);
        engine.RetryTask(owner.Reference,notify.Id); engine.ApproveTask(owner.Reference,notify.Id); await engine.RunNextTaskAsync(owner.Reference,Save,default);
        Assert.Single(engine.RelayFor(owner.Reference).Notifications); Assert.Equal(2,engine.RelayFor(owner.Reference).Deliveries.Count);
        var speech=Propose("RELAY","relay.speak",engine.RelayFor(owner.Reference).Notifications.Single().Id.ToString()); engine.ApproveTask(owner.Reference,speech.Id); saves=0;
        await engine.RunNextTaskAsync(owner.Reference,_=>++saves==2 ? Task.FromException(new IOException("intent failed")) : Task.CompletedTask,default); Assert.Equal(0,voice.Calls);
    }
    [Fact]
    public async Task CancellationStopsSpeechAndRecordsPartialOutcome()
    {
        await Run("RELAY","relay.notify","cancel me"); var voice=new Voice { Wait=true }; engine.ConfigureRelayVoice(voice);
        var task=Propose("RELAY","relay.speak",engine.RelayFor(owner.Reference).Notifications.Single().Id.ToString()); engine.ApproveTask(owner.Reference,task.Id);
        var run=engine.RunNextTaskAsync(owner.Reference,Save,default); await voice.Started.Task.WaitAsync(TimeSpan.FromSeconds(2)); engine.CancelTask(owner.Reference,task.Id); await run.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(RelayDeliveryStatus.Cancelled,engine.RelayFor(owner.Reference).Deliveries.Last().Status); Assert.Equal(Status.Cancelled,engine.GetSnapshot().Tasks.Records.Single(t=>t.Id==task.Id).Status);
    }
    [Fact]
    public async Task RolesCannotBorrowCapabilitiesOrAcknowledgeAsHuman()
    {
        await Run("RELAY","relay.notify","private"); var agent=owner.CreateAttachment(); registry.Add(agent); agent.Switch(engine.ResolveProgram("MAXIS").Id.ToString());
        Assert.Throws<UnauthorizedAccessException>(()=>engine.RelayFor(agent.Reference)); Assert.Throws<UnauthorizedAccessException>(()=>engine.VoltStatus(agent.Reference)); Assert.Empty(engine.TasksFor(agent.Reference).Relay.Notifications);
        Assert.Throws<ArgumentException>(()=>engine.ProposeTask(owner.Reference,agent.CurrentRecipient.Id,"relay.notify","borrow","borrow"));
        agent.Switch(engine.ResolveProgram("RELAY").Id.ToString()); Assert.Throws<UnauthorizedAccessException>(()=>engine.AcknowledgeRelay(agent.Reference,engine.RelayFor(agent.Reference).Notifications.Single().Id));
    }
    [Fact]
    public async Task SqlitePreservesModeInboxReceiptsAndAcknowledgement()
    {
        await Run("VOLT","volt.throttle","eco"); await Run("RELAY","relay.notify","saved"); engine.AcknowledgeRelay(owner.Reference,engine.RelayFor(owner.Reference).Notifications.Single().Id);
        var folder=Path.Combine(Path.GetTempPath(),"GridVoltRelay-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var store=new WorldStore(Path.Combine(folder,"world.db")); store.Create(new WorldInfo(engine.GetSnapshot().WorldId,"Relay",clock.Now),engine.GetSnapshot());
            var restored=GridSimulationEngine.Restore(store.Load().State); Assert.Equal(restored.TickDuration*4,restored.AutomaticTickDelay); Assert.NotNull(restored.GetSnapshot().Tasks.Relay.Notifications.Single().AcknowledgedBy);
            Assert.Equal(RelayDeliveryStatus.Delivered,restored.GetSnapshot().Tasks.Relay.Deliveries.Single().Status);
        }
        finally { Directory.Delete(folder,true); }
    }
    [Fact]
    public async Task RestartMarksInProgressSpeechInterruptedWithoutReplay()
    {
        await Run("RELAY","relay.notify","restart"); var voice=new Voice { Wait=true }; engine.ConfigureRelayVoice(voice);
        var task=Propose("RELAY","relay.speak",engine.RelayFor(owner.Reference).Notifications.Single().Id.ToString()); engine.ApproveTask(owner.Reference,task.Id);
        var run=engine.RunNextTaskAsync(owner.Reference,Save,default); await voice.Started.Task.WaitAsync(TimeSpan.FromSeconds(2)); var restored=GridSimulationEngine.Restore(engine.GetSnapshot());
        Assert.Equal(RelayDeliveryStatus.Interrupted,restored.GetSnapshot().Tasks.Relay.Deliveries.Last().Status); Assert.Equal(Status.Interrupted,restored.GetSnapshot().Tasks.Records.Single(t=>t.Id==task.Id).Status);
        engine.CancelTask(owner.Reference,task.Id); await run; Assert.Equal(1,voice.Calls);
    }
    [Fact]
    public void VersionEighteenMigratesWithNormalPacingAndEmptyInbox()
    {
        var folder=Path.Combine(Path.GetTempPath(),"GridRelayUpgrade-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var store=new WorldStore(Path.Combine(folder,"world.db")); store.Create(new WorldInfo(engine.GetSnapshot().WorldId,"Old",clock.Now),engine.GetSnapshot());
            using(var connection=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=store.DatabasePath,Pooling=false}.ToString()))
            { connection.Open(); using var command=connection.CreateCommand(); command.CommandText="UPDATE WorldTasks SET StateJson=json_remove(StateJson,'$.VoltMode','$.Relay'); DELETE FROM SchemaMigrations WHERE Version>=19; PRAGMA user_version=18;"; command.ExecuteNonQuery(); }
            var saved=store.Load().State; Assert.Equal("normal",saved.Tasks.VoltMode); Assert.Empty(saved.Tasks.Relay.Notifications);
        }
        finally { Directory.Delete(folder,true); }
    }
    [Fact]
    public async Task TerminalCommandsExposeReadableInboxAndExplicitSpeechProposal()
    {
        var commands=DefaultCommands.Create(engine); VoltRelayCommands.Register(commands,engine,owner,Save); commands.UseIdentity(owner);
        await Run("RELAY","relay.notify","terminal hello"); var note=engine.RelayFor(owner.Reference).Notifications.Single();
        Assert.Contains("normal",commands.Execute("volt status").Text); Assert.Contains("terminal hello",commands.Execute("relay inbox").Text);
        Assert.Contains("tasks propose RELAY relay.speak "+note.Id,commands.Execute("relay inspect "+note.Id.ToString()[..8]).Text);
        Assert.Contains("saved",commands.Execute("relay ack "+note.Id.ToString()[..8]).Text); Assert.Contains("Delivered",commands.Execute("relay deliveries").Text);
    }
    [Fact]
    public async Task SnapshotRejectsForgedNotificationAndSpeechTargetLinkage()
    {
        await Run("RELAY","relay.notify","original"); var voice=new Voice(); engine.ConfigureRelayVoice(voice);
        var note=engine.RelayFor(owner.Reference).Notifications.Single(); await Run("RELAY","relay.speak",note.Id.ToString());
        var snapshot=engine.GetSnapshot();
        Assert.Throws<InvalidDataException>(()=>GridSimulationEngine.Restore(snapshot with { Tasks=snapshot.Tasks with { Relay=snapshot.Tasks.Relay with { Notifications=[note with { Text="forged" }] } } }));
        var deliveries=snapshot.Tasks.Relay.Deliveries.ToArray(); deliveries[1]=deliveries[1] with { Attempt=3 };
        Assert.Throws<InvalidDataException>(()=>GridSimulationEngine.Restore(snapshot with { Tasks=snapshot.Tasks with { Relay=snapshot.Tasks.Relay with { Deliveries=deliveries } } }));
    }
}
