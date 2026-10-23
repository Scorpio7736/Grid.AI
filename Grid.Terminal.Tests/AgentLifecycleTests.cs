using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;

public sealed class AgentLifecycleTests
{
    private sealed class Fixture
    {
        public readonly TerminalSessionRegistry Registry = new();
        public readonly TerminalIdentitySession Owner = new(() => "lifecycle-owner");
        public readonly GridSimulationEngine Engine;
        public readonly CommandDispatcher Commands;
        public int Saves;
        public Fixture(ILocalInference? ai = null, GridSimulationSnapshot? snapshot = null)
        {
            Registry.Add(Owner);
            Engine = snapshot is null ? new(GridWorldFactory.CreateGenesis(), editAuthorizer: Registry) : GridSimulationEngine.Restore(snapshot, editAuthorizer: Registry);
            Owner.Attach(Engine); Owner.Login();
            Commands = DefaultCommands.Create(Engine);
            GridTransfer.Register(Commands, Engine, startAgents: () => { Engine.StartAgents(Owner.Reference); Saves++; });
            TaskCommands.Register(Commands, Engine, Owner, Save);
            ChatCommands.Register(Commands, Engine, Owner, ai, Save);
            Commands.UseIdentity(Owner);
        }
        public Task Save(CancellationToken token) { token.ThrowIfCancellationRequested(); Saves++; return Task.CompletedTask; }
        public void Start() => Commands.Execute("grid -start");
        public Grid.Simulation.Tasks.TaskRecord Propose(string argument = "hello", string capability = "maxis.demo") =>
            Engine.ProposeTask(Owner.Reference, Engine.ResolveProgram("MAXIS").Id, capability, argument, Guid.NewGuid().ToString("N"));
    }
    private sealed class PendingInference : ILocalInference
    {
        public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Cancelled;
        public async Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default)
        {
            Entered.TrySetResult();
            try { await Task.Delay(Timeout.Infinite, token); return new(true,"unexpected",""); }
            catch (OperationCanceledException) { Cancelled = true; throw; }
        }
    }
    [Fact]
    public void OnlyStartInitializesAndResumePreservesSixIdentities()
    {
        var f = new Fixture();
        Assert.False(f.Engine.IsRunning);
        Assert.Contains("paused", f.Commands.Execute("connect MCP").Text);
        Assert.Contains("grid -start", f.Commands.Execute("agents initialize").Text);
        Assert.Empty(f.Engine.GetSnapshot().Tasks.Agents);
        Assert.Throws<InvalidOperationException>(() => f.Engine.InitializeSystemAgents(f.Owner.Reference));
        Assert.Throws<InvalidOperationException>(() => f.Engine.EnsureCoordinator());
        f.Start(); var ids = f.Engine.GetSnapshot().Tasks.Agents.Select(a => a.Id).ToArray();
        Assert.Equal(6, ids.Length); Assert.True(f.Saves > 0);
        f.Commands.Execute("grid -pause"); Assert.Contains("paused", f.Commands.Execute("agents").Text);
        f.Start(); f.Start(); Assert.Equal(ids, f.Engine.GetSnapshot().Tasks.Agents.Select(a => a.Id));
    }
    [Fact]
    public async Task PauseBlocksNewActivityButRetainsHistoryAndApprovalWithoutDispatch()
    {
        var f = new Fixture(); f.Start(); f.Commands.Execute("connect MCP"); f.Commands.Execute("talk hello");
        var task = f.Propose(); f.Engine.ApproveTask(f.Owner.Reference, task.Id);
        f.Engine.Pause();
        Assert.Contains("hello", f.Commands.Execute("transcript").Text);
        Assert.Contains("paused", f.Commands.Execute("talk after pause").Text);
        Assert.Throws<InvalidOperationException>(() => f.Engine.Coordinate(f.Owner.Reference,"MAXIS: demo hello","paused"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Engine.RunNextTaskAsync(f.Owner.Reference,f.Save,default));
        f.Start(); Assert.Equal(Status.Queued, Assert.Single(f.Engine.GetSnapshot().Tasks.Records).Status);
        Assert.Equal(0, Assert.Single(f.Engine.GetSnapshot().Tasks.Records).Attempts);
        Assert.Equal(2, Assert.Single(f.Engine.GetSnapshot().Communications.Conversations).Messages.Count);
    }
    [Fact]
    public async Task PauseThenImmediateResumeCancelsInferenceWithoutStoringTurn()
    {
        var ai = new PendingInference(); var f = new Fixture(ai); f.Start(); f.Commands.Execute("connect MCP");
        var operation = f.Commands.Execute("talk hello").BrokerOperation!(default);
        await ai.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        f.Engine.Pause(); f.Start();
        Assert.Contains("cancelled", (await operation.WaitAsync(TimeSpan.FromSeconds(5))).Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(ai.Cancelled); Assert.Empty(Assert.Single(f.Engine.GetSnapshot().Communications.Conversations).Messages);
    }
    [Fact]
    public void PreparedReplyCannotCommitAcrossPauseResume()
    {
        var f = new Fixture(); f.Start(); var id = f.Engine.StartConversation(f.Engine.LocalOperator,f.Engine.ResolveProgram("MCP"));
        var turn = f.Engine.PrepareGroundedTurn(id,f.Engine.LocalOperator,"hello");
        f.Engine.Pause(); f.Start();
        Assert.Throws<InvalidOperationException>(() => f.Engine.CompleteGroundedTurn(turn,new(true,"stale reply","test")));
        Assert.Empty(f.Engine.GetConversation(id).Messages);
    }
    [Fact]
    public async Task PauseDuringTaskInterruptsAndRequiresFreshApprovalForRetry()
    {
        var f = new Fixture(); f.Start(); var task = f.Propose("5000","maxis.delay"); f.Engine.ApproveTask(f.Owner.Reference,task.Id);
        var run = f.Engine.RunNextTaskAsync(f.Owner.Reference,f.Save,default);
        Assert.Equal(Status.Running, Assert.Single(f.Engine.GetSnapshot().Tasks.Records).Status);
        f.Engine.Pause(); f.Start();
        Assert.StartsWith("Interrupted", await run.WaitAsync(TimeSpan.FromSeconds(5)));
        var saved = Assert.Single(f.Engine.GetSnapshot().Tasks.Records); Assert.Equal(1,saved.Attempts);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Engine.RunNextTaskAsync(f.Owner.Reference,f.Save,default));
        f.Engine.RetryTask(f.Owner.Reference,task.Id);
        saved = Assert.Single(f.Engine.GetSnapshot().Tasks.Records);
        Assert.Equal(Status.AwaitingApproval,saved.Status); Assert.Null(saved.ApprovedBy);
    }
    [Fact]
    public async Task PauseDuringDurableStartNeverInvokesTaskAdapter()
    {
        var f = new Fixture(); f.Start(); var task = f.Propose(); f.Engine.ApproveTask(f.Owner.Reference,task.Id);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var first = true;
        async Task Save(CancellationToken token) { if (first) { first=false; entered.SetResult(); await release.Task.WaitAsync(token); } }
        var run = f.Engine.RunNextTaskAsync(f.Owner.Reference,Save,default);
        await entered.Task; f.Engine.Pause(); f.Start(); release.SetResult();
        var result = await run; Assert.StartsWith("Interrupted",result); Assert.DoesNotContain("DEMONSTRATION ONLY",result);
    }
    [Fact]
    public void ReloadRetainsIdentitiesAndTranscriptButStartsPausedWithoutApprovalReplay()
    {
        var f = new Fixture(); f.Start(); f.Commands.Execute("connect MCP"); f.Commands.Execute("talk retained");
        var task = f.Propose(); f.Engine.ApproveTask(f.Owner.Reference,task.Id);
        var folder = Path.Combine(Path.GetTempPath(),"GridLifecycle-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder,"world.db"); var state = f.Engine.GetSnapshot();
            using (var store = new WorldStore(path)) store.Create(new(state.WorldId,"Lifecycle",DateTimeOffset.UtcNow),state);
            using var reopened = new WorldStore(path); var next = new Fixture(snapshot:reopened.Load().State);
            Assert.False(next.Engine.IsRunning); Assert.Equal(6,next.Engine.GetSnapshot().Tasks.Agents.Count);
            Assert.Equal(2,Assert.Single(next.Engine.GetSnapshot().Communications.Conversations).Messages.Count);
            next.Start(); Assert.Equal(f.Engine.GetSnapshot().Tasks.Agents.Select(a=>a.Id),next.Engine.GetSnapshot().Tasks.Agents.Select(a=>a.Id));
            Assert.Equal(Status.AwaitingApproval,Assert.Single(next.Engine.GetSnapshot().Tasks.Records).Status);
        }
        finally { Directory.Delete(folder,true); }
    }
    [Fact]
    public void AgentIdentityCannotStartOrInitializeGrid()
    {
        var f = new Fixture(); f.Start(); f.Owner.Switch(f.Engine.ResolveProgram("MAXIS").Id.ToString()); f.Engine.Pause();
        Assert.Contains("denied",f.Commands.Execute("grid -start").Text);
        Assert.Throws<UnauthorizedAccessException>(()=>f.Engine.StartAgents(f.Owner.Reference));
        Assert.False(f.Engine.IsRunning);
    }
    [Fact]
    public async Task SchedulerShutdownInvalidatesActiveReply()
    {
        var ai = new PendingInference(); var f = new Fixture(ai); f.Start();
        using var stop = new CancellationTokenSource(); var scheduler=f.Engine.RunAsync(stop.Token);
        f.Commands.Execute("connect MCP"); var reply=f.Commands.Execute("talk hello").BrokerOperation!(default);
        await ai.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); stop.Cancel(); await scheduler;
        await reply.WaitAsync(TimeSpan.FromSeconds(5)); Assert.True(ai.Cancelled); Assert.False(f.Engine.IsRunning);
        Assert.Empty(Assert.Single(f.Engine.GetSnapshot().Communications.Conversations).Messages);
    }
}
