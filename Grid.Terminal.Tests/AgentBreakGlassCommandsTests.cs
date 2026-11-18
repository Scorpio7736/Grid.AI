using Grid.Core;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class AgentBreakGlassCommandsTests
{
    private sealed class Fixture
    {
        public GridSimulationEngine Engine { get; }
        public TerminalIdentitySession Identity { get; }
        public AgentBreakGlassCommands Commands { get; }
        public SystemAgent Maxis { get; }
        public SystemAgent Jn { get; }
        public TaskRecord MaxisAwaiting { get; }
        public TaskRecord MaxisQueued { get; }
        public TaskRecord JnAwaiting { get; }
        public int Saves { get; private set; }

        public Fixture(bool interactive = true)
        {
            Identity = new TerminalIdentitySession(() => "agent-break-glass-test");
            Engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: Identity);
            Identity.Attach(Engine);
            Assert.StartsWith("Local credentials", Identity.ConfigureCredentials("owner", "password1", "password1",
            [
                ("one", "answer-one", "answer-one"),
                ("two", "answer-two", "answer-two"),
                ("three", "answer-three", "answer-three")
            ]));
            Assert.StartsWith("Logged in", Identity.LoginPassword("owner", "password1"));
            Engine.StartAgents(Identity.Reference);
            var state = Engine.TasksFor(Identity.Reference);
            Maxis = state.Agents.Single(agent => agent.Role == AgentRole.MAXIS);
            Jn = state.Agents.Single(agent => agent.Role == AgentRole.JN);
            MaxisAwaiting = Engine.ProposeTask(Identity.Reference, Maxis.Id, TaskCapabilities.For(Maxis.Role), "maxis private argument", "maxis-awaiting");
            MaxisQueued = Engine.ProposeTask(Identity.Reference, Maxis.Id, TaskCapabilities.For(Maxis.Role), "maxis queued argument", "maxis-queued");
            Engine.ApproveTask(Identity.Reference, MaxisQueued.Id);
            JnAwaiting = Engine.ProposeTask(Identity.Reference, Jn.Id, TaskCapabilities.For(Jn.Role), "jn private argument", "jn-awaiting");
            Assert.StartsWith("Active agent", Identity.Switch(Maxis.Id.ToString()));
            Commands = new(Engine, Identity, interactive, _ => { Saves++; return Task.CompletedTask; });
        }
    }

    [Fact]
    public void SelfStatusAndEvidenceExposeOnlyBoundedOwnRecords()
    {
        var fixture = new Fixture();

        Assert.True(fixture.Commands.TryExecute("::grid-agent-break self-status", out var status));
        Assert.True(fixture.Commands.TryExecute("::grid-agent-break evidence-self", out var evidence));

        Assert.Contains("Role: MAXIS", status.Text);
        Assert.Contains("awaiting 1", status.Text);
        Assert.Contains("queued 1", status.Text);
        Assert.Contains(fixture.MaxisAwaiting.Id.ToString(), evidence.Text);
        Assert.Contains(fixture.MaxisQueued.Id.ToString(), evidence.Text);
        Assert.DoesNotContain(fixture.JnAwaiting.Id.ToString(), evidence.Text);
        Assert.DoesNotContain("private argument", evidence.Text);
        Assert.DoesNotContain("jn", evidence.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AbortSelfCancelsOnlyOwnPendingWorkAndSaves()
    {
        var fixture = new Fixture();

        Assert.True(fixture.Commands.TryExecute("::grid-agent-break abort-self", out var result));
        Assert.StartsWith("Active identity: Master Control", fixture.Identity.Switch("master"));
        var state = fixture.Engine.TasksFor(fixture.Identity.Reference);

        Assert.Equal(Grid.Simulation.Tasks.TaskStatus.Cancelled, state.Records.Single(task => task.Id == fixture.MaxisAwaiting.Id).Status);
        Assert.Equal(Grid.Simulation.Tasks.TaskStatus.Cancelled, state.Records.Single(task => task.Id == fixture.MaxisQueued.Id).Status);
        Assert.Equal(Grid.Simulation.Tasks.TaskStatus.AwaitingApproval, state.Records.Single(task => task.Id == fixture.JnAwaiting.Id).Status);
        Assert.Equal(1, fixture.Saves);
        Assert.Contains("No other agent was affected", result.Text);
        Assert.DoesNotContain(fixture.JnAwaiting.Id.ToString(), result.Text);
    }

    [Fact]
    public void DetachSelfLocksTerminalWithoutMasterTransition()
    {
        var fixture = new Fixture();

        Assert.True(fixture.Commands.TryExecute("::grid-agent-break detach-self", out var result));

        Assert.False(fixture.Identity.IsAuthenticated);
        Assert.False(fixture.Identity.IsMaster);
        Assert.Equal("grid[locked]> ", fixture.Identity.Prompt);
        Assert.Equal("Login required.", fixture.Identity.Switch("master"));
        Assert.Contains("Master Control was not activated", result.Text);
    }

    [Fact]
    public void MasterLockedAndUnattendedContextsReceiveSameDenial()
    {
        var master = new Fixture();
        Assert.StartsWith("Active identity: Master Control", master.Identity.Switch("master"));
        Assert.True(master.Commands.TryExecute("::grid-agent-break self-status", out var masterResult));

        var locked = new Fixture();
        locked.Identity.Logout();
        Assert.True(locked.Commands.TryExecute("::grid-agent-break self-status", out var lockedResult));

        var unattended = new Fixture(interactive: false);
        Assert.True(unattended.Commands.TryExecute("::grid-agent-break self-status", out var unattendedResult));

        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, masterResult.Text);
        Assert.Equal(masterResult.Text, lockedResult.Text);
        Assert.Equal(masterResult.Text, unattendedResult.Text);
    }

    [Fact]
    public void MalformedOrUnknownAgentBreakGlassInputIsGenericallyDenied()
    {
        var fixture = new Fixture();

        Assert.True(fixture.Commands.TryExecute("::grid-agent-break self-status \"", out var malformed));
        Assert.True(fixture.Commands.TryExecute("::grid-agent-break become-master", out var unknown));

        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, malformed.Text);
        Assert.Equal(malformed.Text, unknown.Text);
        Assert.False(fixture.Identity.IsMaster);
    }

    [Fact]
    public void EngineRejectsMasterAndAttachedAgentLeasesIndependentlyOfTerminalHandler()
    {
        var fixture = new Fixture();
        var attachment = fixture.Identity.CreateAttachment();

        Assert.Throws<UnauthorizedAccessException>(() => fixture.Engine.AgentBreakGlassStatus(attachment.Reference));
        Assert.StartsWith("Active identity: Master Control", fixture.Identity.Switch("master"));
        Assert.Throws<UnauthorizedAccessException>(() => fixture.Engine.AgentBreakGlassAbortSelf(fixture.Identity.Reference));
    }
}
