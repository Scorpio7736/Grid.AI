using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

/// <summary>Phase 4.5: exercise real command, identity, engine and SQLite boundaries together.</summary>
public sealed class CommunicationVerificationTests
{
    private sealed class Session
    {
        public readonly TerminalSessionRegistry Registry = new();
        public readonly TerminalIdentitySession Identity = new(() => "verification-owner");
        public readonly GridSimulationEngine Engine;
        public readonly CommandDispatcher Commands;
        public readonly ChatCommands Chat;
        public Session(GridSimulationSnapshot? snapshot = null)
        {
            Registry.Add(Identity);
            Engine = snapshot is null ? new(GridWorldFactory.CreateGenesis(), editAuthorizer: Registry) : GridSimulationEngine.Restore(snapshot, editAuthorizer: Registry);
            Identity.Attach(Engine);
            Commands = DefaultCommands.Create(Engine); Chat = ChatCommands.Register(Commands, Engine.PrepareChatFixture(), Identity);
            ContextCommands.Register(Commands, Engine, Identity, () => { }); Commands.UseIdentity(Identity);
        }
    }
    [Fact]
    public void SavedConversationAndContextReopenWithAuthenticationAndRevocableGrants()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridPhase45-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var source = Path.Combine(folder, "notes.md"); File.WriteAllText(source, "PERSISTENT CAPTURE");
            var first = new Session(); first.Identity.Login(); first.Commands.Execute("connect MCP");
            var agent = first.Engine.CreateRunningCoordinator(); first.Engine.RegisterAgentIdentity(agent.Id);
            first.Commands.Execute("talk remember this conversation"); var conversation = first.Chat.ActiveConversation!.Value;
            Assert.Contains("Imported", first.Commands.Execute($"context add \"{source}\" persistent project MCP").Text);
            File.WriteAllText(source, "TEMPORARY CAPTURE");
            Assert.Contains("Imported", first.Commands.Execute($"context add \"{source}\" temporary project MCP").Text);
            first.Engine.SetVoiceProfile(first.Identity.Reference, new(agent.Id, "Portable Voice Preference", 1, 75));
            var info = new WorldInfo(first.Engine.GetSnapshot().WorldId, "Verification", DateTimeOffset.UtcNow);
            var path = Path.Combine(folder, "world.db");
            using (var store = new WorldStore(path)) store.Create(info, first.Engine.GetSnapshot());
            File.Delete(source);
            using var reopened = new WorldStore(path); var next = new Session(reopened.Load().State);
            Assert.False(next.Identity.IsAuthenticated); Assert.Null(next.Chat.ActiveConversation);
            Assert.Contains("denied", next.Commands.Execute("transcript " + conversation).Text);
            next.Identity.Login(); Assert.Contains("Resumed", next.Commands.Execute("resume " + conversation).Text);
            Assert.Contains("remember this conversation", next.Commands.Execute("transcript").Text);
            Assert.Contains("PERSISTENT CAPTURE", next.Commands.Execute("context read notes.md").Text);
            Assert.Single(next.Engine.ContextFor(null, next.Identity.SessionId));
            Assert.Single(next.Engine.GetSnapshot().Communications.VoiceProfiles);
            var agentIdentity = next.Identity.CreateAttachment(); next.Registry.Add(agentIdentity); agentIdentity.Switch(agent.Id.ToString());
            var agentCommands = DefaultCommands.Create(next.Engine); ContextCommands.Register(agentCommands, next.Engine, agentIdentity); agentCommands.UseIdentity(agentIdentity);
            Assert.Contains("PERSISTENT CAPTURE", agentCommands.Execute("context read notes.md").Text);
            Assert.Contains("revoked", next.Commands.Execute("context revoke notes.md").Text);
            Assert.Contains("scope", agentCommands.Execute("context read notes.md").Text);
            next.Commands.Execute("talk status"); reopened.Save(info, next.Engine.GetSnapshot());
            var saved = reopened.Load().State;
            Assert.Null(Assert.Single(saved.Context.Documents).GrantedAgentId);
            Assert.Equal(4, Assert.Single(saved.Communications.Conversations).Messages.Count);
            Assert.Single(saved.Communications.VoiceProfiles);
        }
        finally { Directory.Delete(folder, true); }
    }
    [Fact]
    public void MalformedTerminalMessagesAndRecipientsNeverAlterConversationHistory()
    {
        var session = new Session(); session.Identity.Login();
        Assert.Contains("Error", session.Commands.Execute("connect missing-recipient").Text);
        Assert.Empty(session.Engine.GetSnapshot().Communications.Conversations);
        session.Commands.Execute("connect"); var id = session.Chat.ActiveConversation!.Value;
        foreach (var command in new[] { "talk", "talk \"\"", "talk \"unterminated", "talk " + new string('x', 4097), "talk bad\u001b[2J", "resume malformed-id", "transcript " + Guid.NewGuid() })
        {
            var result = session.Commands.Execute(command);
            Assert.False(string.IsNullOrWhiteSpace(result.Text));
            Assert.Empty(session.Engine.GetConversation(id).Messages);
        }
        session.Commands.Execute("talk \"Hello café 世界\"");
        var messages = session.Engine.GetConversation(id).Messages;
        Assert.Equal("Hello café 世界", messages[0].Content); Assert.Equal(2, messages.Count);
        Assert.Equal(new long[] { 1, 2 }, messages.Select(m => m.Sequence));
    }
    [Fact]
    public void UnrelatedAgentCannotReadOrResumeOwnersOtherConversation()
    {
        var session = new Session(); session.Identity.Login();
        var civilian = session.Engine.GetSnapshot().Programs.First();
        session.Commands.Execute("connect " + civilian.Id); session.Commands.Execute("talk OWNER PRIVATE MESSAGE");
        var privateId = session.Chat.ActiveConversation!.Value;
        var archon = session.Engine.CreateRunningCoordinator(); session.Engine.RegisterAgentIdentity(archon.Id);
        var identity = session.Identity.CreateAttachment(); session.Registry.Add(identity); identity.Switch(archon.Id.ToString());
        var commands = DefaultCommands.Create(session.Engine); ChatCommands.Register(commands, session.Engine.PrepareChatFixture(), identity); commands.UseIdentity(identity);
        Assert.DoesNotContain(privateId.ToString(), commands.Execute("conversations").Text);
        Assert.DoesNotContain("OWNER PRIVATE MESSAGE", commands.Execute("transcript " + privateId).Text);
        Assert.Contains("No conversation", commands.Execute("resume " + privateId).Text);
        Assert.Contains("No conversation", commands.Execute("talk unauthorized turn").Text);
        Assert.Equal(2, session.Engine.GetConversation(privateId).Messages.Count);
        session.Commands.Execute("disconnect"); session.Commands.Execute("connect MCP"); session.Commands.Execute("talk SHARED MESSAGE");
        var shared = session.Chat.ActiveConversation!.Value;
        Assert.Contains("SHARED MESSAGE", commands.Execute("transcript " + shared).Text);
        session.Identity.Logout(); Assert.Contains("denied", commands.Execute("transcript " + shared).Text);
    }
}
