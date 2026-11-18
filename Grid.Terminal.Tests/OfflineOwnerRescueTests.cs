using System.Text.RegularExpressions;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class OfflineOwnerRescueTests
{
    private sealed class Input(StringWriter output, params object?[] values) : ITerminalInput
    {
        private readonly Queue<object?> values = new(values);
        public bool Interactive { get; init; } = true;
        public List<bool> SecretReads { get; } = [];

        public Task<string?> ReadAsync(bool secret, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            SecretReads.Add(secret);
            var value = values.Count == 0 ? null : values.Dequeue();
            return Task.FromResult(value is Func<string> dynamicValue ? dynamicValue() : value as string);
        }

        public string ExactConfirmation()
        {
            var match = Regex.Match(output.ToString(), @"World: (?<world>[0-9a-f-]{36})\r?\nNonce: (?<nonce>[0-9A-F]{32})",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            Assert.True(match.Success, "Expected rescue confirmation was not displayed.");
            return match.Groups["world"].Value + " " + match.Groups["nonce"].Value;
        }
    }

    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "grid-offline-rescue-" + Guid.NewGuid().ToString("N"));
        public string WorldPath { get; }
        public string KeyPath { get; }
        public TerminalIdentitySession Identity { get; }
        public GridSimulationEngine Engine { get; }
        public WorldPersistenceSession Persistence { get; }
        public WorldStore Store { get; private set; }
        private bool storeClosed;

        public Fixture(bool enroll = true)
        {
            Directory.CreateDirectory(Root);
            WorldPath = Path.Combine(Root, "world.db");
            KeyPath = Path.Combine(Root, "owner.grid-recovery.json");
            var sessions = new TerminalSessionRegistry();
            Identity = new TerminalIdentitySession(() => "offline-rescue-test");
            sessions.Add(Identity);
            Engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: sessions);
            Identity.Attach(Engine);
            Assert.StartsWith("Local credentials", Identity.ConfigureCredentials("owner", "password1", "password1",
            [
                ("private prompt one", "private response one", "private response one"),
                ("private prompt two", "private response two", "private response two"),
                ("private prompt three", "private response three", "private response three")
            ]));
            Assert.StartsWith("Logged in", Identity.LoginPassword("owner", "password1"));
            Store = new WorldStore(WorldPath);
            var info = new WorldInfo(Engine.GetSnapshot().WorldId, "Offline rescue test", DateTimeOffset.UtcNow);
            Store.Create(info, Engine.GetSnapshot());
            Persistence = new WorldPersistenceSession(Store, info, Engine, TimeSpan.FromHours(1));
            Identity.PersistIdentity = () => Persistence.SaveAsync().GetAwaiter().GetResult();
            if (enroll) Assert.Contains("created", Identity.IssueOfflineRecoveryFile(KeyPath), StringComparison.OrdinalIgnoreCase);
        }

        public void CloseStore()
        {
            if (storeClosed) return;
            Store.Dispose();
            storeClosed = true;
        }

        public void Dispose()
        {
            CloseStore();
            try { Directory.Delete(Root, true); } catch { }
        }
    }

    [Fact]
    public void EnrollmentStoresOnlyVerifierAndRefusesOverwrite()
    {
        using var fixture = new Fixture();
        var key = OfflineRecoveryKeyFile.Read(fixture.KeyPath);
        var snapshotJson = System.Text.Json.JsonSerializer.Serialize(fixture.Engine.GetSnapshot());

        Assert.DoesNotContain(key.Secret, snapshotJson);
        Assert.DoesNotContain(fixture.KeyPath, snapshotJson);
        Assert.Contains(key.KeyId.ToString(), snapshotJson);
        Assert.Throws<IOException>(() => fixture.Identity.IssueOfflineRecoveryFile(fixture.KeyPath));
    }

    [Fact]
    public async Task VerifyOnlyRequiresCorrectKeyAndWritesContentFreeAudit()
    {
        using var fixture = new Fixture();
        var key = OfflineRecoveryKeyFile.Read(fixture.KeyPath);
        fixture.CloseStore();
        var output = new StringWriter();
        var input = new Input(output, fixture.KeyPath, "1");

        var result = await OfflineOwnerRescue.RunAsync(fixture.WorldPath, input, output);

        Assert.Equal(0, result);
        Assert.Contains("Verification complete", output.ToString());
        Assert.DoesNotContain(key.Secret, output.ToString());
        var audit = File.ReadAllText(fixture.WorldPath + ".operator-audit.jsonl");
        Assert.Contains("KeyAccepted", audit);
        Assert.Contains("VerifyOnly", audit);
        Assert.DoesNotContain(key.Secret, audit);
        Assert.DoesNotContain(fixture.KeyPath, audit);
    }

    [Fact]
    public async Task WrongOrNoninteractiveKeyCannotEnterRescue()
    {
        using var fixture = new Fixture();
        var wrongPath = Path.Combine(fixture.Root, "wrong.grid-recovery.json");
        _ = OfflineRecoveryKeyFile.Create(wrongPath, Guid.NewGuid());
        fixture.CloseStore();
        var output = new StringWriter();

        Assert.Equal(2, await OfflineOwnerRescue.RunAsync(fixture.WorldPath, new Input(output, wrongPath), output));
        Assert.Contains("authorization failed", output.ToString(), StringComparison.OrdinalIgnoreCase);
        var redirected = new Input(output) { Interactive = false };
        Assert.Equal(2, await OfflineOwnerRescue.RunAsync(fixture.WorldPath, redirected, output));
        Assert.Empty(redirected.SecretReads);
    }

    [Fact]
    public async Task RotationInvalidatesPriorRecoveryFile()
    {
        using var fixture = new Fixture();
        var replacement = Path.Combine(fixture.Root, "replacement.grid-recovery.json");
        Assert.Contains("created", fixture.Identity.IssueOfflineRecoveryFile(replacement), StringComparison.OrdinalIgnoreCase);
        fixture.CloseStore();
        var output = new StringWriter();

        Assert.Equal(2, await OfflineOwnerRescue.RunAsync(fixture.WorldPath,
            new Input(output, fixture.KeyPath), output));
        Assert.Contains("authorization failed", output.ToString(), StringComparison.OrdinalIgnoreCase);
        output.GetStringBuilder().Clear();
        Assert.Equal(0, await OfflineOwnerRescue.RunAsync(fixture.WorldPath,
            new Input(output, replacement, "1"), output));
        Assert.Contains("Verification complete", output.ToString());
    }

    [Fact]
    public async Task PasswordResetRequiresExactWorldNonceAndPreservesRecoveryAuthority()
    {
        using var fixture = new Fixture();
        var recovery = fixture.Engine.GetSnapshot().Identities.OfflineRecovery;
        fixture.CloseStore();
        var output = new StringWriter();
        Input? input = null;
        input = new Input(output, fixture.KeyPath, "2", "new-owner", "new-password", "new-password",
            (Func<string>)(() => input!.ExactConfirmation()));

        var result = await OfflineOwnerRescue.RunAsync(fixture.WorldPath, input, output);

        Assert.Equal(0, result);
        Assert.Equal(2, input.SecretReads.Count(value => value));
        using var reopened = new WorldStore(fixture.WorldPath);
        var identities = reopened.Load().State.Identities;
        Assert.True(identities.Credential!.Verify("new-owner", "new-password"));
        Assert.False(identities.Credential.Verify("owner", "password1"));
        Assert.Null(identities.ChallengeCredentials);
        Assert.Equal(recovery, identities.OfflineRecovery);
    }

    [Fact]
    public async Task BackupRestoreRequiresSameRecoveryAuthorityAndArchivesPrimary()
    {
        using var fixture = new Fixture();
        var oldBackup = Path.Combine(fixture.Root, "old-backup.db");
        File.Copy(fixture.Store.BackupPath, oldBackup);
        fixture.Engine.Pause();
        fixture.Engine.Step();
        await fixture.Persistence.SaveAsync();
        Assert.Equal(1, fixture.Store.Load().State.TickCount);
        File.Copy(oldBackup, fixture.Store.BackupPath, true);
        fixture.CloseStore();
        var output = new StringWriter();
        Input? input = null;
        input = new Input(output, fixture.KeyPath, "3", (Func<string>)(() => input!.ExactConfirmation()));

        var result = await OfflineOwnerRescue.RunAsync(fixture.WorldPath, input, output);

        Assert.Equal(0, result);
        using var reopened = new WorldStore(fixture.WorldPath);
        Assert.Equal(0, reopened.Load().State.TickCount);
        Assert.NotEmpty(Directory.GetFiles(fixture.Root, "world.db.damaged-*"));
    }

    [Fact]
    public async Task ActiveWorldLeaseAndUnenrolledWorldAreRefused()
    {
        using var active = new Fixture();
        var output = new StringWriter();
        Assert.Equal(2, await OfflineOwnerRescue.RunAsync(active.WorldPath, new Input(output, active.KeyPath), output));
        Assert.Contains("Exclusive access", output.ToString(), StringComparison.OrdinalIgnoreCase);

        using var unenrolled = new Fixture(enroll: false);
        unenrolled.CloseStore();
        output.GetStringBuilder().Clear();
        Assert.Equal(2, await OfflineOwnerRescue.RunAsync(unenrolled.WorldPath, new Input(output), output));
        Assert.Contains("not enrolled", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No universal recovery key", output.ToString());
    }
}
