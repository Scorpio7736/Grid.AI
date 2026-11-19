using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Simulation.Tasks;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class MasterBreakGlassCommandsTests
{
    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "grid-master-breakglass-" + Guid.NewGuid().ToString("N"));
        public TerminalSessionRegistry Sessions { get; } = new();
        public TerminalIdentitySession Identity { get; }
        public GridSimulationEngine Engine { get; }
        public WorldStore Store { get; }
        public WorldPersistenceSession Persistence { get; }
        public EmergencyControlState Emergency { get; } = new();
        public MasterBreakGlassCommands Commands { get; }
        public int Stops { get; private set; }

        public Fixture(bool interactive = true, ManualTimeProvider? clock = null)
        {
            Directory.CreateDirectory(Root);
            Identity = new TerminalIdentitySession(() => "master-breakglass-test", clock);
            Sessions.Add(Identity);
            Engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), timeProvider: clock, editAuthorizer: Sessions);
            Identity.Attach(Engine);
            Assert.StartsWith("Local credentials", Identity.ConfigureCredentials("private-owner", "password1", "password1",
            [
                ("private prompt one", "private answer one", "private answer one"),
                ("private prompt two", "private answer two", "private answer two"),
                ("private prompt three", "private answer three", "private answer three")
            ]));
            Assert.StartsWith("Logged in", Identity.LoginPassword("private-owner", "password1"));
            Engine.StartAgents(Identity.Reference);
            Store = new WorldStore(Path.Combine(Root, "world.db"));
            var info = new WorldInfo(Engine.GetSnapshot().WorldId, "Emergency test", DateTimeOffset.UtcNow);
            Store.Create(info, Engine.GetSnapshot());
            Persistence = new(Store, info, Engine, TimeSpan.FromHours(1), clock);
            Commands = new(Engine, Identity, Sessions, Store, Persistence, Emergency, interactive,
                () => { Stops++; return Task.CompletedTask; }, token => Persistence.SaveAsync(token), clock);
        }

        public (string Operation, string Nonce) Plan(string command)
        {
            Assert.True(Commands.TryExecute("::grid-breakglass " + command, out var result));
            Assert.Contains("NO CHANGE APPLIED", result.Text);
            string Value(string prefix) => result.Text.Split('\n').Single(line => line.StartsWith(prefix, StringComparison.Ordinal))[prefix.Length..];
            return (Value("Operation: "), Value("Nonce: "));
        }

        public CommandResult Commit(string action, (string Operation, string Nonce) plan)
        {
            Assert.True(Commands.TryExecute($"::grid-breakglass {action} {plan.Operation} {plan.Nonce}", out var result));
            return result;
        }

        public void Dispose()
        {
            Store.Dispose();
            try { Directory.Delete(Root, true); } catch { }
        }
    }

    [Fact]
    public void FreezeRequiresExactFreshPlanAndCannotReplay()
    {
        using var fixture = new Fixture();
        var plan = fixture.Plan("freeze");

        var wrong = fixture.Commit("freeze", (plan.Operation, "WRONG"));
        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, wrong.Text);
        Assert.True(fixture.Engine.IsRunning);

        var committed = fixture.Commit("freeze", plan);
        Assert.False(fixture.Engine.IsRunning);
        Assert.Contains("freeze committed", committed.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, fixture.Commit("freeze", plan).Text);
    }

    [Fact]
    public void PlanExpiresAndActivityOrSessionChangesInvalidateIt()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
        using var fixture = new Fixture(clock: clock);
        var activityPlan = fixture.Plan("disable-remote");
        fixture.Engine.Pause();
        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, fixture.Commit("disable-remote", activityPlan).Text);

        fixture.Engine.Start();
        var expired = fixture.Plan("disable-remote");
        clock.Now = clock.Now.AddMinutes(3);
        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, fixture.Commit("disable-remote", expired).Text);

        var sessionPlan = fixture.Plan("disable-remote");
        fixture.Identity.Logout();
        Assert.StartsWith("Logged in", fixture.Identity.LoginPassword("private-owner", "password1"));
        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, fixture.Commit("disable-remote", sessionPlan).Text);
    }

    [Fact]
    public void DisableRemoteRevokesTransientChallengeAuthority()
    {
        using var fixture = new Fixture();
        var endpoint = fixture.Engine.PairChallengeEndpoint(fixture.Identity.Reference, "test-endpoint");
        Assert.Equal(Grid.Communication.ChallengeSessionStatus.Pending,
            fixture.Engine.BeginRemoteChallenge(endpoint, "contact", "remote-call-identity").Status);

        var result = fixture.Commit("disable-remote", fixture.Plan("disable-remote"));

        Assert.True(fixture.Emergency.RemoteDisabled);
        Assert.Contains("Remote acceptance disabled", result.Text);
        Assert.Equal(Grid.Communication.ChallengeSessionStatus.Cancelled,
            fixture.Engine.BeginRemoteChallenge(endpoint, "contact", "remote-call-identity").Status);
    }

    [Fact]
    public void RevokeSessionsRemovesApprovalsStopsAttachmentsAndLocksEveryone()
    {
        using var fixture = new Fixture();
        var maxis = fixture.Engine.TasksFor(fixture.Identity.Reference).Agents.Single(agent => agent.Role == AgentRole.MAXIS);
        var task = fixture.Engine.ProposeTask(fixture.Identity.Reference, maxis.Id, TaskCapabilities.For(maxis.Role), "bounded", "revoke-test");
        fixture.Engine.ApproveTask(fixture.Identity.Reference, task.Id);
        var attachment = fixture.Identity.CreateAttachment(); fixture.Sessions.Add(attachment);

        var result = fixture.Commit("revoke-sessions", fixture.Plan("revoke-sessions"));

        Assert.False(fixture.Identity.IsAuthenticated);
        Assert.False(attachment.IsAuthenticated);
        Assert.Equal(1, fixture.Stops);
        Assert.StartsWith("Logged in", fixture.Identity.LoginPassword("private-owner", "password1"));
        Assert.Equal(Grid.Simulation.Tasks.TaskStatus.AwaitingApproval,
            fixture.Engine.TasksFor(fixture.Identity.Reference).Records.Single(row => row.Id == task.Id).Status);
        Assert.Contains("queued approvals revoked: 1", result.Text);
    }

    [Fact]
    public void IsolationFreezesDisablesRemoteAndRevokesSessions()
    {
        using var fixture = new Fixture();
        var result = fixture.Commit("isolate", fixture.Plan("isolate"));

        Assert.False(fixture.Engine.IsRunning);
        Assert.True(fixture.Emergency.RemoteDisabled);
        Assert.False(fixture.Identity.IsAuthenticated);
        Assert.Contains("Isolation committed", result.Text);
    }

    [Fact]
    public void SafeModeDisablesOrdinaryCommandsAndRevokesQueuedApproval()
    {
        using var fixture = new Fixture();
        var maxis = fixture.Engine.TasksFor(fixture.Identity.Reference).Agents.Single(agent => agent.Role == AgentRole.MAXIS);
        var task = fixture.Engine.ProposeTask(fixture.Identity.Reference, maxis.Id, TaskCapabilities.For(maxis.Role), "bounded", "safe-test");
        fixture.Engine.ApproveTask(fixture.Identity.Reference, task.Id);

        var result = fixture.Commit("safe-mode-commit", fixture.Plan("safe-mode-plan"));
        var dispatcher = DefaultCommands.Create(fixture.Engine);
        dispatcher.EmergencySafeMode = () => fixture.Emergency.SafeMode;

        Assert.True(fixture.Emergency.SafeMode);
        Assert.False(fixture.Engine.IsRunning);
        Assert.Contains("Safe mode active", result.Text);
        Assert.Contains("safe mode is active", dispatcher.Execute("status").Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COMMAND REFERENCE", dispatcher.Execute("help").Text);
        Assert.Equal(Grid.Simulation.Tasks.TaskStatus.AwaitingApproval,
            fixture.Engine.TasksFor(fixture.Identity.Reference).Records.Single(row => row.Id == task.Id).Status);
    }

    [Fact]
    public void EvidenceExportIsExactNewFileAndContainsNoPrivateBodies()
    {
        using var fixture = new Fixture();
        var maxis = fixture.Engine.TasksFor(fixture.Identity.Reference).Agents.Single(agent => agent.Role == AgentRole.MAXIS);
        _ = fixture.Engine.ProposeTask(fixture.Identity.Reference, maxis.Id, TaskCapabilities.For(maxis.Role), "SECRET-TASK-ARGUMENT", "evidence-test");
        var path = Path.Combine(fixture.Root, "evidence.json");
        var plan = fixture.Plan($"evidence-export \"{path}\"");

        var result = fixture.Commit($"evidence-export \"{path}\"", plan);
        var json = File.ReadAllText(path);

        Assert.Contains("Grid.AI.MasterBreakGlassEvidence.v1", json);
        Assert.DoesNotContain("private-owner", json);
        Assert.DoesNotContain("private prompt", json);
        Assert.DoesNotContain("private answer", json);
        Assert.DoesNotContain("SECRET-TASK-ARGUMENT", json);
        Assert.Contains("SHA256", result.Text);
        var audit = File.ReadAllText(fixture.Store.DatabasePath + ".operator-audit.jsonl");
        Assert.Contains("PlanIssued", audit);
        Assert.Contains("CommitAccepted", audit);
        Assert.DoesNotContain(plan.Nonce, audit);
        Assert.DoesNotContain(path, audit);
        Assert.DoesNotContain("private", audit, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SECRET-TASK-ARGUMENT", audit);
        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, fixture.Commit($"evidence-export \"{path}\"", plan).Text);
    }

    [Fact]
    public async Task RecoveryCommitStopsWithoutFinalSaveAndRestoresBoundBackup()
    {
        using var fixture = new Fixture();
        fixture.Engine.Pause();
        var oldBackup = Path.Combine(fixture.Root, "old-backup.db");
        File.Copy(fixture.Store.BackupPath, oldBackup);
        fixture.Engine.Step();
        await fixture.Persistence.SaveAsync();
        File.Copy(oldBackup, fixture.Store.BackupPath, true);
        Assert.Equal(1, fixture.Store.Load().State.TickCount);

        var committed = fixture.Commit("recovery-commit", fixture.Plan("recovery-plan backup"));
        Assert.NotNull(committed.WorldRecovery);
        var dispatcher = DefaultCommands.Create(fixture.Engine);
        dispatcher.Register(new("recover-now", "recover-now", "test seam", _ => committed));
        dispatcher.UseIdentity(fixture.Identity);
        var output = new StringWriter();
        var host = new TerminalHost(fixture.Engine, dispatcher, new StringReader("recover-now\n"), output,
            showPrompt: false, persistence: fixture.Persistence);

        Assert.Equal(0, await host.RunAsync());
        Assert.Equal(0, fixture.Store.Load().State.TickCount);
        Assert.Contains("WORLD RECOVERED // RESTART REQUIRED", output.ToString());
        Assert.Contains(Directory.GetFiles(fixture.Root), path => path.Contains(".damaged-", StringComparison.Ordinal));
        Assert.Contains("RecoveryCompleted", File.ReadAllText(fixture.Store.DatabasePath + ".operator-audit.jsonl"));
    }

    [Fact]
    public async Task RecoveryRefusesChangedBackupHash()
    {
        using var fixture = new Fixture();
        fixture.Engine.Pause();
        var committed = fixture.Commit("recovery-commit", fixture.Plan("recovery-plan backup"));
        await File.AppendAllTextAsync(fixture.Store.BackupPath, "changed");

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Persistence.RecoverAsync(committed.WorldRecovery!.Plan));
    }

    [Fact]
    public void AgentLockedAndUnattendedContextsReceiveGenericDenial()
    {
        using var agent = new Fixture();
        var maxis = agent.Engine.TasksFor(agent.Identity.Reference).Agents.Single(row => row.Role == AgentRole.MAXIS);
        agent.Identity.Switch(maxis.Id.ToString());
        Assert.True(agent.Commands.TryExecute("::grid-breakglass status", out var agentResult));

        using var locked = new Fixture(); locked.Identity.Logout();
        Assert.True(locked.Commands.TryExecute("::grid-breakglass status", out var lockedResult));
        using var unattended = new Fixture(interactive: false);
        Assert.True(unattended.Commands.TryExecute("::grid-breakglass status", out var unattendedResult));

        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, agentResult.Text);
        Assert.Equal(agentResult.Text, lockedResult.Text);
        Assert.Equal(agentResult.Text, unattendedResult.Text);
    }
}
