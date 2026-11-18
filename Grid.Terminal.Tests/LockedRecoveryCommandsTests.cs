using System.Security.Cryptography;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class LockedRecoveryCommandsTests
{
    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "grid-locked-recovery-" + Guid.NewGuid().ToString("N"));
        public GridSimulationEngine Engine { get; }
        public TerminalIdentitySession Identity { get; }
        public WorldStore Store { get; }
        public LockedRecoveryCommands Commands { get; }

        public Fixture(bool interactive = true, TimeProvider? clock = null)
        {
            Directory.CreateDirectory(Root);
            Identity = new TerminalIdentitySession(() => "test-subject", clock);
            Engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: Identity);
            Identity.Attach(Engine);
            Assert.StartsWith("Local credentials", Identity.ConfigureCredentials("private-user", "password1", "password1",
            [
                ("private prompt one", "private answer one", "private answer one"),
                ("private prompt two", "private answer two", "private answer two"),
                ("private prompt three", "private answer three", "private answer three")
            ]));
            Store = new WorldStore(Path.Combine(Root, "world.db"));
            Store.Create(new(Engine.GetSnapshot().WorldId, "Recovery test", DateTimeOffset.UtcNow), Engine.GetSnapshot());
            Commands = new(Engine, Identity, Store, interactive, clock);
        }

        public void Dispose()
        {
            Store.Dispose();
            try { Directory.Delete(Root, true); } catch { }
        }
    }

    [Fact]
    public void LockedStatusIsContentFree()
    {
        using var fixture = new Fixture();

        Assert.True(fixture.Commands.TryExecute("::grid-rescue status", out var result));

        Assert.Contains("Locked original console", result.Text);
        Assert.Contains(fixture.Engine.GetSnapshot().WorldId.ToString(), result.Text);
        Assert.DoesNotContain("private-user", result.Text);
        Assert.DoesNotContain("private prompt", result.Text);
        Assert.DoesNotContain("private answer", result.Text);
        Assert.False(fixture.Identity.IsAuthenticated);
    }

    [Fact]
    public void VerifyWorldIsReadOnlyAndChecksPrimaryAndBackup()
    {
        using var fixture = new Fixture();
        var primaryBefore = SHA256.HashData(File.ReadAllBytes(fixture.Store.DatabasePath));
        var backupBefore = SHA256.HashData(File.ReadAllBytes(fixture.Store.BackupPath));

        Assert.True(fixture.Commands.TryExecute("::grid-rescue verify-world", out var result));

        Assert.Contains("Primary: Valid", result.Text);
        Assert.Contains("Backup: Valid", result.Text);
        Assert.Contains("No migration, repair, replacement, save or recovery", result.Text);
        Assert.Equal(primaryBefore, SHA256.HashData(File.ReadAllBytes(fixture.Store.DatabasePath)));
        Assert.Equal(backupBefore, SHA256.HashData(File.ReadAllBytes(fixture.Store.BackupPath)));
    }

    [Fact]
    public void FreezePausesActivityWithoutAuthenticating()
    {
        using var fixture = new Fixture();
        fixture.Engine.Start();

        Assert.True(fixture.Commands.TryExecute("::grid-rescue freeze-request", out var result));

        Assert.False(fixture.Engine.IsRunning);
        Assert.False(fixture.Identity.IsAuthenticated);
        Assert.Contains("activity frozen", result.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecoveryPreparationExpiresAndNeverAuthenticates()
    {
        var clock = new ManualTimeProvider(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));
        using var fixture = new Fixture(clock: clock);

        Assert.True(fixture.Commands.TryExecute("::grid-rescue recovery-challenge", out var prepared));
        var operationLine = prepared.Text.Split('\n').Single(line => line.StartsWith("Operation: ", StringComparison.Ordinal));
        var operation = operationLine["Operation: ".Length..];
        Assert.Contains("does not authenticate anyone", prepared.Text);
        Assert.DoesNotContain("private prompt", prepared.Text);
        Assert.False(fixture.Identity.IsAuthenticated);

        clock.Now = clock.Now.AddMinutes(3);
        Assert.True(fixture.Commands.TryExecute($"::grid-rescue cancel {operation}", out var cancelled));
        Assert.Contains("No matching active", cancelled.Text);
        Assert.False(fixture.Identity.IsAuthenticated);
    }

    [Fact]
    public void AuthenticatedOrUnattendedUseGetsSameGenericDenial()
    {
        using var authenticated = new Fixture();
        Assert.StartsWith("Logged in", authenticated.Identity.LoginPassword("private-user", "password1"));
        Assert.True(authenticated.Commands.TryExecute("::grid-rescue status", out var loggedIn));

        using var unattended = new Fixture(interactive: false);
        Assert.True(unattended.Commands.TryExecute("::grid-rescue status", out var redirected));

        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, loggedIn.Text);
        Assert.Equal(loggedIn.Text, redirected.Text);
    }

    [Fact]
    public void MalformedReservedInputIsDeniedWhileOrdinaryInputFallsThrough()
    {
        using var fixture = new Fixture();

        Assert.True(fixture.Commands.TryExecute("::grid-rescue status \"", out var malformed));
        Assert.Equal(DevelopmentBreakGlassRegistry.GenericDenial, malformed.Text);
        Assert.False(fixture.Commands.TryExecute("inspect program \"", out _));
    }

    [Fact]
    public async Task OriginalTerminalHostInterceptsLockedRecoveryBeforeDispatcher()
    {
        using var fixture = new Fixture();
        var output = new StringWriter();
        var dispatcher = DefaultCommands.Create(fixture.Engine);
        dispatcher.UseIdentity(fixture.Identity);
        var host = new TerminalHost(fixture.Engine, dispatcher,
            new StringReader("::grid-rescue status\nexit\n"), output,
            showPrompt: false, interactiveConfirmation: true, lockedRecovery: fixture.Commands);

        Assert.Equal(0, await host.RunAsync());
        Assert.Contains("LOCKED LOCAL RECOVERY STATUS", output.ToString());
        Assert.DoesNotContain(DevelopmentBreakGlassRegistry.GenericDenial, output.ToString());
    }
}
