using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class InvitationTests
{
    private sealed class Model : ILocalInference
    {
        public string Prompt = "";
        public string Reply = "Shared response.";
        public Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default) { Prompt = prompt; return Task.FromResult(new InferenceReply(true, Reply, "test")); }
    }
    private sealed class Fixture
    {
        public readonly TerminalSessionRegistry Registry = new();
        public readonly TerminalIdentitySession Owner = new(() => "invitation-owner");
        public readonly GridSimulationEngine Engine;
        public readonly Model Ai = new();
        public readonly CommandDispatcher Commands;
        public readonly ChatCommands Chat;
        public Fixture(GridSimulationSnapshot? snapshot = null)
        {
            Registry.Add(Owner); Engine = snapshot is null ? new(GridWorldFactory.CreateGenesis(), editAuthorizer: Registry) : GridSimulationEngine.Restore(snapshot, editAuthorizer: Registry);
            Owner.Attach(Engine); Owner.Login(); Engine.StartAgents(Owner.Reference);
            Commands = DefaultCommands.Create(Engine); TaskCommands.Register(Commands, Engine, Owner, _ => Task.CompletedTask);
            Chat = ChatCommands.Register(Commands, Engine, Owner, Ai, _ => Task.CompletedTask); Commands.UseIdentity(Owner);
            if (snapshot is null) Commands.Execute("connect MCP -inline");
        }
        public async Task<CommandResult> Talk(string input) { var r = Commands.ExecuteTokens(["talk", input]); return r.BrokerOperation is {} op ? await op(default) : r; }
        public Guid Id => Chat.ActiveConversation!.Value;
    }
    [Fact]
    public async Task RecommendationNeedsConfirmationAndDeclinePreservesConversation()
    {
        var f = new Fixture(); var id = f.Id;
        Assert.Contains("suggests inviting MAXIS", (await f.Talk("invite someone for code debugging")).Text);
        Assert.Equal(id, f.Id); Assert.False(f.Engine.GetConversation(id).IsMeeting);
        Assert.Contains("declined", (await f.Talk("decline invitation")).Text);
        Assert.Equal(id, f.Id); Assert.Single(f.Engine.GetSnapshot().Communications.Conversations);
        Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task ConfirmedInviteKeepsWindowButExcludesEarlierPrivateHistory()
    {
        var f = new Fixture(); await f.Talk("PRIVATE_BEFORE_INVITATION"); var old = f.Id;
        await f.Talk("invite MAXIS"); var result = await f.Talk("accept invitation");
        Assert.Contains("Current participants: MCP, MAXIS", result.Text);
        Assert.NotEqual(old, f.Id); Assert.Equal(ConversationStatus.Ended, f.Engine.GetConversation(old).Status);
        Assert.Equal(2, f.Engine.GetConversation(old).Messages.Count);
        await f.Talk("@MAXIS What did we discuss earlier?");
        Assert.DoesNotContain("PRIVATE_BEFORE_INVITATION", f.Ai.Prompt);
        Assert.Contains("respond only as MAXIS", f.Ai.Prompt);
        Assert.Empty(f.Engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public async Task RemovalEndsOldMembershipAndReinviteDoesNotRecallOldGroupMessages()
    {
        var f = new Fixture(); await f.Talk("invite MAXIS"); await f.Talk("accept invitation");
        await f.Talk("OLD_GROUP_SECRET"); var group = f.Id;
        await f.Talk("remove participant MAXIS"); Assert.Contains("Removed MAXIS", (await f.Talk("confirm removal")).Text);
        Assert.False(f.Engine.GetConversation(f.Id).IsMeeting);
        Assert.Equal(ConversationStatus.Ended, f.Engine.GetConversation(group).Status);
        await f.Talk("invite MAXIS"); await f.Talk("accept invitation"); await f.Talk("@MAXIS recall OLD_GROUP_SECRET");
        Assert.DoesNotContain("[conversation #", f.Ai.Prompt); Assert.DoesNotContain("PRIOR DISCUSSION", f.Ai.Prompt);
    }
    [Fact]
    public async Task ScopeChangeInvalidatesConfirmation()
    {
        var f = new Fixture(); var id = f.Id; await f.Talk("invite MAXIS");
        f.Engine.SetMemoryProject(f.Owner.Reference, id, "other");
        Assert.Contains("expired", (await f.Talk("accept invitation")).Text); Assert.Equal(id, f.Id);
    }
    [Fact]
    public async Task PausingInvalidatesConfirmationAndUnrelatedInputClearsIt()
    {
        var f = new Fixture(); var id = f.Id; await f.Talk("invite MAXIS"); f.Engine.Pause(); f.Engine.Start();
        Assert.Contains("expired", (await f.Talk("accept invitation")).Text);
        await f.Talk("invite MAXIS"); await f.Talk("hello");
        Assert.Contains("No pending", (await f.Talk("accept invitation")).Text); Assert.Equal(id, f.Id);
    }
    [Fact]
    public async Task InvitationPersistsConfirmedMembershipButNotPendingConsent()
    {
        var f = new Fixture(); await f.Talk("invite MAXIS"); await f.Talk("accept invitation"); var id = f.Id;
        await f.Talk("invite JN");
        var folder = Path.Combine(Path.GetTempPath(), "GridInvitation-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var store = new WorldStore(Path.Combine(folder, "world.db")); var snapshot = f.Engine.GetSnapshot();
            store.Create(new(snapshot.WorldId, "Invitation", DateTimeOffset.UtcNow), snapshot);
            var next = new Fixture(store.Load().State); next.Commands.Execute("resume " + id + " -inline");
            Assert.Contains("No pending", (await next.Talk("accept invitation")).Text);
            Assert.Equal(new[] { "Local operator", "MCP", "MAXIS" }, next.Engine.GetConversation(id).Participants.Select(p => p.Name));
        }
        finally { Directory.Delete(folder, true); }
    }
    [Fact]
    public async Task MeetingPlanningDoesNotIncludePrivateCapturedContextAndStillNeedsApproval()
    {
        var f = new Fixture(); var now = DateTimeOffset.UtcNow;
        f.Engine.AddContext(f.Owner.Reference, new ContextDocument(Guid.NewGuid(), "general", "private.txt", new string('A', 64), 14, now, now, "utf8", "PRIVATE_UPLOAD", f.Engine.ResolveProgram("MCP").Id), false);
        await f.Talk("invite VOLT"); await f.Talk("accept invitation");
        f.Ai.Reply = "{\"steps\":[{\"agent\":\"VOLT\",\"capability\":\"volt.status\",\"argument\":\"current\"}]}";
        Assert.Contains("DRAFT ONLY", (await f.Talk("@VOLT plan a current resource check")).Text);
        Assert.DoesNotContain("PRIVATE_UPLOAD", f.Ai.Prompt);
        Assert.Contains("await approval", (await f.Talk("submit plan")).Text);
        Assert.All(f.Engine.GetSnapshot().Tasks.Records, t => Assert.Equal(Grid.Simulation.Tasks.TaskStatus.AwaitingApproval, t.Status));
        Assert.Contains("VERIFIED BROKER STATE", (await f.Talk("MCP progress")).Text);
    }
    [Fact]
    public async Task AgentCannotConfirmMembershipOrRemoveCoordinator()
    {
        var f = new Fixture(); var c = f.Engine.GetConversation(f.Id);
        var agent = f.Owner.CreateAttachment(); f.Registry.Add(agent); agent.Switch(f.Engine.ResolveProgram("MCP").Id.ToString());
        Assert.Throws<UnauthorizedAccessException>(() => f.Engine.ChangeConversationMembership(agent.Reference, c.Id, "MAXIS", false, 0, c.ProjectScope, f.Engine.ConversationMemoryRevision, f.Engine.ActivityGeneration));
        Assert.Contains("stays", (await f.Talk("remove participant MCP")).Text);
    }
}
