using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Editing;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class IdentitySessionTests
{
    private static (GridSimulationEngine Engine, TerminalIdentitySession Session, CommandDispatcher Commands) Fixture(Func<string?>? provider = null)
    {
        var session = new TerminalIdentitySession(provider ?? (() => "test-os-owner"));
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: session); session.Attach(engine);
        return (engine, session, Commands(engine, session));
    }
    private static CommandDispatcher Commands(GridSimulationEngine engine, TerminalIdentitySession session)
    {
        var commands = DefaultCommands.Create(engine); ChatCommands.Register(commands, engine.PrepareChatFixture(), session);
        IdentityCommands.Register(commands, engine, session); commands.UseIdentity(session); return commands;
    }
    [Fact]
    public void StartupDeniesWorldActionsAndHelpIsScoped()
    {
        var (engine, _, commands) = Fixture();
        Assert.Contains("denied", commands.Execute("step").Text); Assert.Equal(0, engine.TickCount);
        Assert.Contains("denied", commands.Execute("nge").Text); Assert.Contains("denied", commands.Execute("connect").Text);
        var help = commands.Execute("help").Text;
        Assert.Contains("master", help); Assert.DoesNotContain("apply-edit", help); Assert.DoesNotContain("inspect program", help);
        Assert.Contains("locked", commands.Prompt);
        commands.Register(new("extension", "extension", "New administrative extension", _ => new("executed")));
        Assert.Contains("denied", commands.Execute("extension").Text);
        commands.Execute("login master"); Assert.Equal("executed", commands.Execute("extension").Text);
        Assert.Contains("Master Control", commands.Prompt);
    }
    [Fact]
    public void WrongOrUnavailableOsIdentityCannotLoginAndOwnerSurvivesRestore()
    {
        string? subject = "owner"; var (engine, session, commands) = Fixture(() => subject);
        commands.Execute("login master"); Assert.Equal("owner", engine.GetSnapshot().Identities.OwnerSubject);
        commands.Execute("logout"); subject = "intruder";
        Assert.Contains("denied", commands.Execute("login master").Text); Assert.False(session.IsMaster);
        subject = null; Assert.Contains("denied", commands.Execute("login master").Text);
        var second = new TerminalIdentitySession(() => "intruder"); var restored = GridSimulationEngine.Restore(engine.GetSnapshot(), editAuthorizer: second); second.Attach(restored);
        Assert.Contains("denied", second.Login()); Assert.False(second.IsMaster);
        Assert.Contains(engine.CaptureHistory().Events, e => e.Kind == "IdentitySession" && e.Detail.Contains("denied"));
    }
    [Fact]
    public void RegisteredAgentLosesAdminCapabilitiesAndChatContextIsIsolated()
    {
        var (engine, session, commands) = Fixture(); commands.Execute("login master"); commands.Execute("connect"); commands.Execute("talk hello");
        var conversation = engine.GetSnapshot().Communications.Conversations.Single().Id;
        Assert.Contains("not registered", commands.Execute("switch MCP").Text);
        commands.Execute("register-agent MCP"); commands.Execute("switch MCP");
        Assert.False(session.IsMaster); Assert.Contains("agent:", commands.Prompt);
        foreach (var cmd in new[] { "status", "step", "live", "nge", "programs", "edit whatever name x" })
            Assert.Contains("denied", commands.Execute(cmd).Text);
        Assert.Contains("No conversation", commands.Execute("talk hi").Text);
        // MCP participates in the earlier conversation, so that transcript belongs to its scope too.
        Assert.Contains("hello", commands.Execute("transcript " + conversation).Text);
        Assert.Contains("MCP", commands.Execute("self").Text);
        Assert.DoesNotContain("apply-edit", commands.Execute("help").Text);
        commands.Execute("switch master"); Assert.True(session.IsMaster);
        Assert.Contains("MCP", commands.Execute("talk hi").Text);
        Assert.Equal(2, engine.GetSnapshot().Communications.Conversations.Count);
    }
    [Fact]
    public void OtherAgentCannotReadUnrelatedConversationAndStatusDoesNotExposeGlobalState()
    {
        var world = new GridWorld(); var d = new GridDistrict { Name = "Core" }; world.AddDistrict(d);
        for (var i = 0; i < 2; i++) world.AddProgram(new GridProgram { Name = "Agent" + i, Kind = ResidentKind.Agent, Occupation = "Unassigned" }, d.Id);
        var session = new TerminalIdentitySession(() => "owner"); var engine = new GridSimulationEngine(world, editAuthorizer: session); session.Attach(engine);
        var commands = Commands(engine, session); commands.Execute("login master"); commands.Execute("connect Agent0"); commands.Execute("talk private");
        var id = engine.GetSnapshot().Communications.Conversations[0].Id;
        commands.Execute("register-agent Agent1"); commands.Execute("switch Agent1");
        Assert.Contains("No conversation", commands.Execute("transcript " + id).Text);
        Assert.DoesNotContain(id.ToString(), commands.Execute("conversations").Text);
        commands.Execute("connect Agent0"); Assert.Contains("VERIFIED OWN STATE", commands.Execute("talk status").Text);
    }
    [Fact]
    public void EditPreviewRequiresConfirmationAndLogoutInvalidatesServiceLease()
    {
        var (engine, session, commands) = Fixture(); commands.Execute("login master"); var program = engine.GetSnapshot().Programs[0];
        var result = commands.Execute($"edit {program.Id} name Changed").Text;
        Assert.Contains("Preview", result); Assert.Equal(program.Name, engine.GetSnapshot().Programs[0].Name);
        var id = result.Split(' ')[1]; Assert.Contains("Applied", commands.Execute("apply-edit " + id).Text);
        Assert.Equal("Changed", engine.GetSnapshot().Programs[0].Name);
        var reference = session.Reference;
        var preview = engine.PrepareEdit(reference, new(program.Id, [new("name", new TextValue("Stale"))])).Preview!;
        commands.Execute("logout"); commands.Execute("login master");
        Assert.Equal(ProgramEditOutcome.Forbidden, engine.ApplyEdit(reference, preview).Outcome);
        Assert.Contains("No matching preview", commands.Execute("apply-edit " + id).Text);
        Assert.Contains(engine.CaptureHistory().Events, e => e.Kind == "Edit");
    }
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    [Fact]
    public void ExpiredSessionAndFailedOwnerSaveGrantNoPermissions()
    {
        var clock = new Clock(); var session = new TerminalIdentitySession(() => "owner", clock);
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: session); session.Attach(engine);
        session.PersistIdentity = () => throw new IOException("Injected owner save failure");
        Assert.Throws<IOException>(() => session.Login()); Assert.False(session.IsMaster);
        session.PersistIdentity = null; session.Login(); var old = session.Reference;
        clock.Now = clock.Now.AddHours(8);
        Assert.False(session.IsMaster); Assert.False(session.Allows("programs.edit"));
        Assert.Null(session.Acquire(old, engine.WorldInstanceId));
        Assert.Contains("Logged in", session.Login()); Assert.True(session.IsMaster);
        Assert.NotEqual(old.Generation, session.Reference.Generation);
    }

    [Fact]
    public void AgentCannotAcquireEditLeaseAndObservationOwnedFieldsStayProtected()
    {
        var (engine, session, commands) = Fixture(); commands.Execute("login master"); commands.Execute("connect");
        var archon = engine.ResolveProgram("MCP");
        Assert.Contains("observation/service-owned", commands.Execute($"edit {archon.Id} energy 5").Text);
        commands.Execute("register-agent MCP"); commands.Execute("switch MCP");
        Assert.Null(session.Acquire(session.Reference, engine.WorldInstanceId));
        Assert.Equal(ProgramEditOutcome.Forbidden, engine.PrepareEdit(session.Reference, new(archon.Id, [new("name", new TextValue("Changed"))])).Outcome);
    }
}
