using Grid.Core;
using Grid.Communication;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Xunit;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal.Tests;

public sealed class McpWorkflowTests
{
    private readonly TerminalSessionRegistry registry = new();
    private readonly TerminalIdentitySession owner = new(() => "mcp-workflow-owner");
    private readonly GridSimulationEngine engine;
    private readonly CommandDispatcher commands;
    private readonly Model model = new();
    private sealed class Model : ILocalInference
    {
        public string Reply = "{\"steps\":[{\"agent\":\"VOLT\",\"capability\":\"volt.status\",\"argument\":\"current\"},{\"agent\":\"RELAY\",\"capability\":\"relay.notify\",\"argument\":\"Review finished\"}]}";
        public string Prompt = "";
        public Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default) { Prompt = prompt; return Task.FromResult(new InferenceReply(true, Reply, "controlled test")); }
    }
    public McpWorkflowTests()
    {
        registry.Add(owner); engine = new(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
        owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
        commands = DefaultCommands.Create(engine); TaskCommands.Register(commands, engine, owner, Save);
        ChatCommands.Register(commands, engine, owner, model, Save); commands.UseIdentity(owner);
        commands.Execute("connect MCP -inline");
    }
    private Task Save(CancellationToken token) { GridSimulationEngine.ValidateSnapshot(engine.GetSnapshot()); return Task.CompletedTask; }
    private async Task<CommandResult> Run(string message) { var r = commands.Execute(message); return r.BrokerOperation is {} op ? await op(default) : r; }
    private CoordinationPlan Plan(string request = "VOLT: status current; RELAY: notify Review finished") => engine.Coordinate(owner.Reference, request, Guid.NewGuid().ToString("N"));
    private void Approve(CoordinationPlan plan) { foreach (var id in plan.TaskIds) engine.ApproveTask(owner.Reference, id); }

    [Fact]
    public async Task ToolLedgerBindsParentRequestAndRecordedOutcomeAcrossRestore()
    {
        var plan = Plan("VOLT: status current");
        var before = Assert.Single(engine.ToolReceipts(owner.Reference, plan.Id));
        Assert.Equal(plan.Id, before.ParentPlanId);
        Assert.Null(before.ResultHash);
        Approve(plan);
        await Run("mcp run " + plan.Id);
        var after = Assert.Single(engine.ToolReceipts(owner.Reference, plan.Id));
        Assert.Equal(before.RequestHash, after.RequestHash);
        Assert.Equal("Completed", after.State);
        Assert.NotNull(after.ResultHash);
        Assert.NotNull(after.StartedUtc);
        Assert.NotNull(after.FinishedUtc);
        var restored = GridSimulationEngine.Restore(engine.GetSnapshot(), editAuthorizer: registry);
        owner.Attach(restored);
        Assert.Equal(after, Assert.Single(restored.ToolReceipts(owner.Reference, plan.Id)));
    }

    [Fact]
    public async Task CustomRequestDraftAssignsSpecialistsAndExecutesOnlyApprovedPlanWithEvidence()
    {
        var draft = await Run("mcp plan Review my machine resources and leave a local notification.");
        Assert.Contains("DRAFT ONLY", draft.Text); Assert.Contains("Why this specialist", draft.Text);
        Assert.Contains("volt.status", model.Prompt); Assert.Empty(engine.GetSnapshot().Tasks.Records);
        await Run("talk submit plan"); var plan = Assert.Single(engine.GetSnapshot().Tasks.Plans);
        Assert.Contains("approve every", (await Run("mcp run " + plan.Id)).Text);
        Assert.All(engine.GetSnapshot().Tasks.Records, t => Assert.Equal(0, t.Attempts));
        Approve(plan);
        var unrelated = engine.ProposeTask(owner.Reference, engine.ResolveProgram("RELAY").Id, "relay.notify", "Unrelated", "unrelated");
        engine.ApproveTask(owner.Reference, unrelated.Id);
        var result = await Run("talk run plan " + plan.Id);
        Assert.Contains("VERIFIED BROKER STATE", result.Text); Assert.Contains("Completed", result.Text);
        Assert.All(engine.GetSnapshot().Tasks.Records.Where(t => plan.TaskIds.Contains(t.Id)), t => { Assert.Equal(Status.Completed, t.Status); Assert.Equal(1, t.Attempts); Assert.NotNull(t.Result); });
        Assert.Equal(Status.Queued, engine.GetSnapshot().Tasks.Records.Single(t => t.Id == unrelated.Id).Status);
        await Run("mcp run " + plan.Id);
        Assert.All(engine.GetSnapshot().Tasks.Records.Where(t => plan.TaskIds.Contains(t.Id)), t => Assert.Equal(1, t.Attempts));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClarificationRetainsRequestAndCannotApproveOrExecute(bool versioned)
    {
        model.Reply = "{" + (versioned ? "\"version\":1," : "") + "\"question\":\"Which resource observation do you need?\"}";
        Assert.Contains("Which resource", (await Run("mcp plan review the machine")).Text);
        Assert.Contains("clarification first", (await Run("talk approve this")).Text);
        model.Reply = "{\"steps\":[{\"agent\":\"VOLT\",\"capability\":\"volt.status\",\"argument\":\"current\"}]}";
        Assert.Contains("DRAFT ONLY", (await Run("talk Current hardware resources")).Text);
        Assert.Contains("review the machine", model.Prompt); Assert.Contains("Current hardware resources", model.Prompt);
        await Run("talk cancel"); Assert.Contains("No draft", (await Run("talk submit plan")).Text);
        Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Theory]
    [InlineData("{\"version\":2,\"question\":\"Which workspace?\"}")]
    [InlineData("{\"version\":\"1\",\"question\":\"Which workspace?\"}")]
    [InlineData("{\"question\":\"Which workspace?\",\"approved\":true}")]
    [InlineData("{\"question\":\"Which workspace?\",\"steps\":[]}")]
    [InlineData("{\"question\":\"First?\",\"question\":\"Second?\"}")]
    [InlineData("{\"question\":\" \"}")]
    public async Task InvalidClarificationCannotCreateDraftOrGrantAuthority(string response)
    {
        model.Reply = response;
        Assert.Contains("Invalid clarification", (await Run("mcp plan build a project")).Text);
        Assert.Contains("No draft", (await Run("talk submit plan")).Text);
        Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task McpAndSpecialistsCannotSelfApproveOrDispatchUnderHumanIdentity()
    {
        var plan = Plan(); Approve(plan);
        var agent = owner.CreateAttachment(); registry.Add(agent); agent.Switch(engine.ResolveProgram("MCP").Id.ToString());
        Assert.Throws<UnauthorizedAccessException>(() => engine.ApproveTask(agent.Reference, plan.TaskIds[0]));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => engine.RunCoordinationAsync(agent.Reference, plan.Id, Save, default));
        Assert.Contains("Reserved human controls", commands.Execute("mcp authority").Text);
        Assert.DoesNotContain(McpCapabilityCatalog.Entries, e => e.Capability.Contains("nge") || e.Capability.Contains("grant"));
    }
    [Fact]
    public async Task DenialOrRevocationBlocksAllRemainingDispatch()
    {
        var plan = Plan(); engine.ApproveTask(owner.Reference, plan.TaskIds[0]); engine.DenyTask(owner.Reference, plan.TaskIds[1], "Not needed");
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RunCoordinationAsync(owner.Reference, plan.Id, Save, default));
        engine.CancelCoordination(owner.Reference, plan.Id);
        Assert.Equal(Status.Cancelled, engine.GetSnapshot().Tasks.Records.Single(t => t.Id == plan.TaskIds[0]).Status);
        Assert.All(engine.GetSnapshot().Tasks.Records, t => Assert.Equal(0, t.Attempts));
    }
    [Fact]
    public async Task CancellationDuringWorkStopsSubsequentStepsAndRetainsResults()
    {
        var plan = Plan("MAXIS: delay 500; RELAY: notify Must not run"); Approve(plan);
        var running = engine.RunCoordinationAsync(owner.Reference, plan.Id, Save, default);
        engine.CancelCoordination(owner.Reference, plan.Id);
        var brief = await running;
        Assert.Contains("Cancelled", brief); Assert.Contains("stopped", brief);
        Assert.Equal(0, engine.GetSnapshot().Tasks.Records.Single(t => t.Id == plan.TaskIds[1]).Attempts);
    }
    [Fact]
    public async Task DeferredDispatchCannotConsumeLaterApproval()
    {
        var plan = Plan(); var pending = commands.Execute("mcp run " + plan.Id);
        Approve(plan);
        var result = await pending.BrokerOperation!(default);
        Assert.Contains("changed since dispatch", result.Text);
        Assert.All(engine.GetSnapshot().Tasks.Records, t => Assert.Equal(0, t.Attempts));
    }
    [Fact]
    public async Task SessionRevocationPreventsDeferredDispatch()
    {
        var plan = Plan(); Approve(plan); var pending = commands.Execute("mcp run " + plan.Id);
        owner.Logout();
        var result = await pending.BrokerOperation!(default);
        Assert.Contains("Task error", result.Text);
        Assert.All(engine.GetSnapshot().Tasks.Records, t => Assert.Equal(0, t.Attempts));
    }
    [Fact]
    public void ReusingPlanCreatesNewUnapprovedTasks()
    {
        var plan = Plan(); Approve(plan);
        Assert.Contains("Fresh approval", commands.Execute("mcp repeat " + plan.Id).Text);
        var copy = engine.GetSnapshot().Tasks.Plans.Single(p => p.Id != plan.Id);
        Assert.Equal(plan.Request, copy.Request);
        Assert.All(engine.GetSnapshot().Tasks.Records.Where(t => copy.TaskIds.Contains(t.Id)), t => { Assert.Equal(Status.AwaitingApproval, t.Status); Assert.Null(t.ApprovedBy); Assert.Equal(0, t.Attempts); });
    }
    [Fact]
    public async Task FailedDurableStartNeverInvokesAdapterOrClaimsCompletion()
    {
        var plan = Plan(); Approve(plan);
        var result = await engine.RunCoordinationAsync(owner.Reference, plan.Id, _ => throw new IOException("disk unavailable"), default);
        Assert.Contains("durability is not confirmed", result);
        Assert.DoesNotContain(engine.GetSnapshot().Tasks.Records, t => t.Status == Status.Completed);
        Assert.Equal(0, engine.GetSnapshot().Tasks.Records.Single(t => t.Id == plan.TaskIds[1]).Attempts);
    }
}
