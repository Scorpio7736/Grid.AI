using Grid.Agent;
using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class DeviceCapabilityTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Provider : IDeviceIntegration
    {
        public string DeviceId { get; set; } = "test-device";
        public string Fingerprint = "v1";
        public int Executions, Discoveries;
        public bool Fail;
        public DeviceDiscovery Discover(bool windowsOnly)
        { Discoveries++; return new([new("12", "App", "Window; title withheld")], false, "fixture", DateTimeOffset.UtcNow); }
        public DeviceTarget Resolve(DeviceRequest request) => new(request.Target, Fingerprint, "selected application");
        public string Execute(DeviceRequest request, DeviceTarget expected)
        { Executions++; if (Fail) throw new IOException("Unavailable fixture."); return "Observed completed action"; }
    }
    private sealed class Fixture
    {
        public readonly Clock Time = new();
        public readonly Provider Runtime = new();
        public readonly TerminalSessionRegistry Registry = new();
        public readonly TerminalIdentitySession Owner = new(() => "device-owner");
        public readonly GridSimulationEngine Engine;
        public Fixture()
        {
            Registry.Add(Owner);
            Engine = new(GridWorldFactory.CreateGenesis(), timeProvider: Time, editAuthorizer: Registry);
            Owner.Attach(Engine); Owner.Login(); Engine.StartAgents(Owner.Reference); Engine.ConfigureDeviceIntegration(Runtime);
        }
        public DeviceReview Select() => Engine.SelectDevice(Owner.Reference, new(1, DeviceOperation.Launch, "fixture.exe"));
        public Task<DeviceReceipt> Run(Guid id, Func<CancellationToken, Task>? save = null) =>
            Engine.ExecuteDeviceAsync(Owner.Reference, id, save ?? (_ => Task.CompletedTask), default);
    }
    [Theory]
    [InlineData(2, 0, "app")]
    [InlineData(1, 99, "app")]
    [InlineData(1, 0, "")]
    [InlineData(1, 0, "app\ncommand")]
    public void InvalidVersionOperationOrTargetIsRejected(int version, int operation, string target)
    {
        var f = new Fixture();
        Assert.Throws<ArgumentException>(() => f.Engine.SelectDevice(f.Owner.Reference, new(version, (DeviceOperation)operation, target)));
        Assert.Equal(0, f.Runtime.Executions);
    }
    [Fact]
    public async Task DiscoveryAndSelectionDoNotAuthorizeExecutionAndReceiptsAreSingleUse()
    {
        var f = new Fixture();
        Assert.Single(f.Engine.DiscoverDevices(f.Owner.Reference, true).Items);
        var review = f.Select();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Run(review.Id));
        Assert.Equal(0, f.Runtime.Executions);
        f.Engine.ApproveDevice(f.Owner.Reference, review.Id);
        var saves = 0;
        var result = await f.Run(review.Id, _ => { saves++; if (saves == 1) Assert.Equal(0, f.Runtime.Executions); return Task.CompletedTask; });
        Assert.Equal("Completed", result.Status); Assert.Equal(2, saves); Assert.Equal(1, f.Runtime.Executions);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Run(review.Id));
        var events = f.Engine.CaptureHistory().Events;
        Assert.Contains(events, e => e.Kind == "DeviceClaimed" && e.ContextJson.Contains("requestHash"));
        Assert.Contains(events, e => e.Kind == "DeviceCompleted");
        Assert.DoesNotContain(events.Where(e => e.Kind.StartsWith("Device")), e => e.Detail.Contains("fixture.exe") || e.ContextJson.Contains("fixture.exe"));
    }
    [Theory]
    [InlineData("revoke")]
    [InlineData("expire")]
    [InlineData("device")]
    [InlineData("logout")]
    [InlineData("pause")]
    [InlineData("target")]
    public async Task LiveChangesInvalidateReviewedAuthority(string change)
    {
        var f = new Fixture(); var review = f.Select(); f.Engine.ApproveDevice(f.Owner.Reference, review.Id);
        switch (change)
        {
            case "revoke": f.Engine.RevokeDevice(f.Owner.Reference, review.Id); break;
            case "expire": f.Time.Now = f.Time.Now.AddMinutes(3); break;
            case "device": f.Runtime.DeviceId = "different-device"; break;
            case "logout": f.Owner.Logout(); f.Owner.Login(); break;
            case "pause": f.Engine.Pause(); f.Engine.StartAgents(f.Owner.Reference); break;
            case "target": f.Runtime.Fingerprint = "changed"; break;
        }
        await Assert.ThrowsAnyAsync<Exception>(() => f.Run(review.Id));
        Assert.Equal(0, f.Runtime.Executions);
    }
    [Fact]
    public void AttachedAndAgentSessionsCannotGrantOrBorrowDeviceAccess()
    {
        var f = new Fixture(); var review = f.Select();
        var child = f.Owner.CreateAttachment(); f.Registry.Add(child);
        Assert.Throws<UnauthorizedAccessException>(() => f.Engine.ApproveDevice(child.Reference, review.Id));
        Assert.Throws<UnauthorizedAccessException>(() => f.Engine.DiscoverDevices(child.Reference, true));
        f.Owner.Switch("MAXIS");
        Assert.Throws<UnauthorizedAccessException>(() => f.Engine.SelectDevice(f.Owner.Reference, review.Request));
    }
    [Fact]
    public async Task FailedSaveConsumesAttemptWithoutCallingTheOperatingSystem()
    {
        var f = new Fixture(); var review = f.Select(); f.Engine.ApproveDevice(f.Owner.Reference, review.Id);
        await Assert.ThrowsAsync<IOException>(() => f.Run(review.Id, _ => Task.FromException(new IOException("save failed"))));
        Assert.Equal(0, f.Runtime.Executions);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Run(review.Id));
    }
    [Fact]
    public async Task RevocationDuringPersistenceAndConcurrentRunsCannotExecute()
    {
        var f = new Fixture(); var review = f.Select(); f.Engine.ApproveDevice(f.Owner.Reference, review.Id);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = f.Run(review.Id, async _ => { entered.TrySetResult(); await release.Task; });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => f.Run(review.Id));
            f.Engine.RevokeDevice(f.Owner.Reference, review.Id);
        }
        finally { release.TrySetResult(); }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => first);
        Assert.Equal(0, f.Runtime.Executions);
    }
    [Fact]
    public async Task UnavailableOrUnsavedResultsAreNotSilentlyRetried()
    {
        var f = new Fixture(); var review = f.Select(); f.Engine.ApproveDevice(f.Owner.Reference, review.Id);
        f.Runtime.Fail = true;
        Assert.Equal("Unavailable", (await f.Run(review.Id)).Status);
        Assert.Equal(1, f.Runtime.Executions);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Run(review.Id));
        f.Runtime.Fail = false; var next = f.Select(); f.Engine.ApproveDevice(f.Owner.Reference, next.Id);
        var saves = 0;
        var receipt = await f.Run(next.Id, _ => ++saves == 2 ? Task.FromException(new IOException("result save failed")) : Task.CompletedTask);
        Assert.Equal("OutcomeNotSaved", receipt.Status);
        Assert.Contains("do not retry blindly", receipt.Detail);
        Assert.Equal(2, f.Runtime.Executions);
    }
    [Fact]
    public async Task RestartDoesNotRestoreDeviceGrantsOrApprovals()
    {
        var f = new Fixture(); var review = f.Select(); f.Engine.ApproveDevice(f.Owner.Reference, review.Id);
        var owner = new TerminalIdentitySession(() => "device-owner"); var registry = new TerminalSessionRegistry(); registry.Add(owner);
        var next = GridSimulationEngine.Restore(f.Engine.GetSnapshot(), editAuthorizer: registry);
        owner.Attach(next); owner.Login(); next.StartAgents(owner.Reference); next.ConfigureDeviceIntegration(f.Runtime);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => next.ExecuteDeviceAsync(owner.Reference, review.Id, _ => Task.CompletedTask, default));
        Assert.Equal(0, f.Runtime.Executions);
    }
    [Fact]
    public async Task CommandsRequireSeparateApprovalAndProduceObservedReceipt()
    {
        var f = new Fixture(); var commands = DefaultCommands.Create(f.Engine);
        DeviceCommands.Register(commands, f.Engine, f.Owner, _ => Task.CompletedTask); commands.UseIdentity(f.Owner);
        Assert.Contains("Disabled", commands.Execute("device catalog").Text);
        Assert.Contains("No window titles", commands.Execute("device windows").Text);
        Assert.Contains("no contents read", commands.Execute("device paths").Text);
        var response = commands.Execute("device select launch fixture.exe");
        var id = Guid.Parse(response.Text.Split('\n')[0]["REVIEW ".Length..]);
        var run = commands.Execute("device run " + id);
        Assert.Contains("approve", (await run.BrokerOperation!(default)).Text);
        Assert.Equal(0, f.Runtime.Executions);
        Assert.Contains("approved", commands.Execute("device approve " + id).Text);
        Assert.Contains("Completed", (await commands.Execute("device run " + id).BrokerOperation!(default)).Text);
    }
    [Fact]
    public void WindowsDiscoveryAndSelectedFolderCreationUseRealProvider()
    {
        if (!OperatingSystem.IsWindows()) return;
        var provider = new WindowsDeviceIntegration();
        var rows = provider.Discover(false);
        Assert.InRange(rows.Items.Count, 1, 256);
        if (!rows.Incomplete) Assert.Contains(rows.Items, p => p.Id == Environment.ProcessId.ToString());
        Assert.All(rows.Items, row => Assert.DoesNotContain('\n', row.Name));
        var root = Path.Combine(Path.GetTempPath(), "GridDevice-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var request = new DeviceRequest(1, DeviceOperation.CreateFolder, Path.Combine(root, "approved"));
            var target = provider.Resolve(request);
            Assert.False(Directory.Exists(request.Target));
            Assert.Contains("Created empty folder", provider.Execute(request, target));
            Assert.True(Directory.Exists(request.Target));
            Assert.Throws<IOException>(() => provider.Execute(request, target));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task FolderSelectionUsesExistingFileGatewayAndRevalidatesBeforeWrite()
    {
        if (!OperatingSystem.IsWindows()) return;
        var root = Path.Combine(Path.GetTempPath(), "GridDeviceGateway-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var owner = new TerminalIdentitySession(() => "folder-owner"); var registry = new TerminalSessionRegistry(); registry.Add(owner);
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
            owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
            engine.ConfigureMaxisRuntime(Path.Combine(root, "artifacts"));
            engine.ConfigureDeviceIntegration(new WindowsDeviceIntegration());
            var request = new DeviceRequest(1, DeviceOperation.CreateFolder, Path.Combine(root, "new-folder"));
            var review = engine.SelectDevice(owner.Reference, request);
            engine.ApproveDevice(owner.Reference, review.Id);
            var receipt = await engine.ExecuteDeviceAsync(owner.Reference, review.Id, _ => Task.CompletedTask, default);
            Assert.Equal("Completed", receipt.Status); Assert.True(Directory.Exists(request.Target));
            var blocked = new DeviceRequest(1, DeviceOperation.CreateFolder, Path.Combine(root, ".git"));
            Assert.Throws<ArgumentException>(() => engine.SelectDevice(owner.Reference, blocked));
        }
        finally { Directory.Delete(root, true); }
    }
    [Fact]
    public void WindowsLaunchVerifiesExactExecutableAndRejectsCommandHosts()
    {
        if (!OperatingSystem.IsWindows()) return;
        var provider = new WindowsDeviceIntegration();
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "where.exe");
        var request = new DeviceRequest(1, DeviceOperation.Launch, executable);
        var target = provider.Resolve(request);
        Assert.Equal(64, target.Fingerprint.Length);
        Assert.Contains("Started process", provider.Execute(request, target));
        Assert.Throws<ArgumentException>(() => provider.Resolve(new(1, DeviceOperation.Launch,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"))));
    }

    [Theory]
    [InlineData("relative.exe")]
    [InlineData(@"\\server\share\app.exe")]
    [InlineData(@"C:\test.exe:stream")]
    public void WindowsProviderRejectsUnsupportedPaths(string path)
    {
        if (!OperatingSystem.IsWindows()) return;
        Assert.Throws<ArgumentException>(() => new WindowsDeviceIntegration().Resolve(new(1, DeviceOperation.Launch, path)));
    }
}
