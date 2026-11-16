using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class TransferTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "GridTransferTests-" + Guid.NewGuid().ToString("N"));
    private readonly string app;
    private readonly string world;
    private readonly GridSimulationEngine engine;
    public TransferTests()
    {
        app = Path.Combine(root, "application"); Directory.CreateDirectory(app);
        var data = Path.Combine(root, "source data"); Directory.CreateDirectory(data); world = Path.Combine(data, "custom.db");
        File.WriteAllText(Path.Combine(app, "asset.txt"), "offline asset");
        engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        using var store = new WorldStore(world); store.Create(new(engine.GetSnapshot().WorldId, "Transfer", DateTimeOffset.UtcNow), engine.GetSnapshot());
    }
    public void Dispose() { Directory.Delete(root, true); }
    [Fact]
    public void CopyIncludesSelectedWorldJnReports()
    {
        var artifacts=world+".jn-artifacts"; Directory.CreateDirectory(artifacts); File.WriteAllText(Path.Combine(artifacts,"report.json"),"{}");
        var transfer=new GridTransfer(app,world,skipRuntimeValidation:true); var destination=Path.Combine(root,"with-jn"); transfer.Execute(transfer.Prepare(new(destination,false)),()=>true);
        Assert.Equal("{}",File.ReadAllText(Path.Combine(destination,"data","world.db.jn-artifacts","report.json")));
    }
    [Fact]
    public void CopyIncludesSelectedWorldMaxisArtifacts()
    {
        var artifacts = world + ".maxis-artifacts"; Directory.CreateDirectory(artifacts);
        File.WriteAllText(Path.Combine(artifacts,"report.txt"),"saved MAXIS report");
        var transfer = new GridTransfer(app,world,skipRuntimeValidation:true); var destination = Path.Combine(root,"with-maxis");
        transfer.Execute(transfer.Prepare(new(destination,false)),()=>true);
        Assert.Equal("saved MAXIS report",File.ReadAllText(Path.Combine(destination,"data","world.db.maxis-artifacts","report.txt")));
    }
    [Fact]
    public void CopyPreservesSourceAndCreatesVerifiedLoadablePackage()
    {
        var transfer = new GridTransfer(app, world,skipRuntimeValidation:true); var destination = Path.Combine(root, "copy with spaces");
        var plan = transfer.Prepare(new(destination, false));
        Assert.Contains("COPY GRID", GridTransfer.Preview(plan));
        Assert.Contains("Copy complete", transfer.Execute(plan, () => true));
        Assert.True(File.Exists(world)); Assert.True(File.Exists(Path.Combine(app, "asset.txt")));
        using var copied = new WorldStore(Path.Combine(destination, "data", "world.db"));
        Assert.Equal(engine.GetSnapshot().WorldId, copied.Load().State.WorldId);
        Assert.Equal("offline asset", File.ReadAllText(Path.Combine(destination, "app", "asset.txt")));
        Assert.True(File.Exists(Path.Combine(destination, "launch.cmd")));
        Assert.Equal(destination, GridTransfer.PackageRoot(Path.Combine(destination, "app")));
    }
    [Fact]
    public void DestinationValidationRejectsExistingNestedAndRelativePaths()
    {
        var transfer = new GridTransfer(app, world,skipRuntimeValidation:true);
        foreach (var path in new[] { app, root, Path.Combine(app, "nested"), "relative-folder", Path.Combine(root, "source data", "nested") })
            Assert.Throws<IOException>(() => transfer.Prepare(new(path, true)));
        Assert.True(File.Exists(world));
    }
    [Fact]
    public void RevocationAndInventoryChangesNeverDeleteSource()
    {
        var transfer = new GridTransfer(app, world,skipRuntimeValidation:true); var destination = Path.Combine(root, "denied"); var plan = transfer.Prepare(new(destination, true));
        Assert.Throws<UnauthorizedAccessException>(() => transfer.Execute(plan, () => false)); Assert.False(Directory.Exists(destination));
        File.WriteAllText(Path.Combine(app, "new.txt"), "changed");
        Assert.Throws<IOException>(() => transfer.Execute(plan, () => true)); Assert.True(File.Exists(world));
    }
    [Fact]
    public void MoveCleansOnlyVerifiedInventoryAndRetainsUnrelatedFiles()
    {
        var transfer = new GridTransfer(app, world,skipRuntimeValidation:true); var destination = Path.Combine(root, "move");
        var unrelated = Path.Combine(root, "source data", "unrelated.txt"); File.WriteAllText(unrelated, "keep");
        var launched = false;
        transfer.Execute(transfer.Prepare(new(destination, true)), () => true, _ => launched = true);
        Assert.True(launched); Assert.True(File.Exists(world));
        var receipt = System.Text.Json.JsonSerializer.Deserialize<TransferReceipt>(File.ReadAllText(Path.Combine(destination, "transfer-manifest.json")))!;
        GridTransfer.Cleanup(receipt.Plan);
        Assert.False(File.Exists(world)); Assert.False(File.Exists(Path.Combine(app, "asset.txt")));
        Assert.True(File.Exists(unrelated)); Assert.True(File.Exists(Path.Combine(destination, "data", "world.db")));
    }
    [Fact]
    public void ChangedDestinationOrActiveSourceLeaseAbortsMoveCleanup()
    {
        var transfer = new GridTransfer(app, world,skipRuntimeValidation:true); var destination = Path.Combine(root, "move");
        transfer.Execute(transfer.Prepare(new(destination, true)), () => true, _ => { });
        var receipt = System.Text.Json.JsonSerializer.Deserialize<TransferReceipt>(File.ReadAllText(Path.Combine(destination, "transfer-manifest.json")))!;
        using (var active = new WorldStore(world)) Assert.Throws<IOException>(() => GridTransfer.Cleanup(receipt.Plan));
        File.WriteAllText(Path.Combine(destination, "app", "asset.txt"), "corrupted");
        Assert.Throws<IOException>(() => GridTransfer.Cleanup(receipt.Plan));
        Assert.True(File.Exists(world)); Assert.True(File.Exists(Path.Combine(app, "asset.txt")));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void DuplicateSourceCleanupRetainsLocksAndVerifiesEveryCopy(bool retainSource, bool corruptCopy, bool externalLock)
    {
        var source = Path.Combine(app, "Test-GridPortable.ps1"); File.WriteAllText(source, "verified script");
        var destination = Path.Combine(root, "duplicate-move");
        var transfer = new GridTransfer(app, world, skipRuntimeValidation: true);
        transfer.Execute(transfer.Prepare(new(destination, true)), () => true, _ => { });
        var receipt = System.Text.Json.JsonSerializer.Deserialize<TransferReceipt>(File.ReadAllText(Path.Combine(destination, "transfer-manifest.json")))!;
        var original = receipt.Plan.Files.Single(f => f.Source == source) with { DeleteSource = !retainSource };
        var duplicate = original with { Relative = "source/Test-GridPortable.ps1" };
        Directory.CreateDirectory(Path.Combine(destination, "source"));
        var secondCopy = Path.Combine(destination, duplicate.Relative);
        File.Copy(source, secondCopy);
        var plan = receipt.Plan with { Files = receipt.Plan.Files.Select(f => f.Source == source ? original : f).Append(duplicate).ToArray() };
        if (corruptCopy) File.WriteAllText(secondCopy, "corrupt script");
        using (var locked = externalLock ? new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None) : null)
        {
            if (corruptCopy || externalLock)
            {
                Assert.Throws<IOException>(() => GridTransfer.Cleanup(plan));
                Assert.True(File.Exists(world)); Assert.True(File.Exists(Path.Combine(app, "asset.txt"))); Assert.True(File.Exists(source));
                return;
            }
            GridTransfer.Cleanup(plan);
        }
        Assert.Equal(retainSource, File.Exists(source));
        Assert.False(File.Exists(world));
        Assert.Equal("verified script", File.ReadAllText(secondCopy));
    }
    [Fact]
    public void TransferCommandsRequireMasterAndParseQuotedPaths()
    {
        var identity = new TerminalIdentitySession(() => "test-owner"); identity.Attach(engine);
        var commands = DefaultCommands.Create(engine); GridTransfer.Register(commands); commands.UseIdentity(identity);
        Assert.Contains("denied", commands.Execute("grid -copyto \"E:\\GRID Copy\"").Text);
        identity.Login();
        Assert.Equal(new TransferRequest("E:\\GRID Copy", false), commands.Execute("grid -copyto \"E:\\GRID Copy\"").Transfer);
        Assert.True(commands.Execute("grid -transferto \"E:\\GRID\"").Transfer!.Move);
        Assert.Contains("Usage", commands.Execute("grid -copyto E:\\GRID Copy").Text);
        var agent = engine.CreateRunningCoordinator(); engine.RegisterAgentIdentity(agent.Id); identity.Switch(agent.Id.ToString());
        Assert.Contains("denied", commands.Execute("grid -transferto \"E:\\GRID\"").Text);
    }
    [Theory]
    [InlineData("grid")]
    [InlineData("grid -copyto")]
    [InlineData("grid -transferto")]
    [InlineData("grid -unknown destination")]
    [InlineData("grid -copyto -transferto")]
    [InlineData("grid -copyto -transferto destination")]
    [InlineData("grid -copyto destination extra")]
    [InlineData("grid -copyto \"\"")]
    public void InvalidTransferSwitchesNeverProduceRequests(string line)
    {
        var commands = DefaultCommands.Create(engine); GridTransfer.Register(commands);
        var result = commands.Execute(line); Assert.Null(result.Transfer); Assert.Contains("Usage", result.Text);
    }
    [Fact]
    public void LegacyNamesGiveMigrationGuidanceWithoutExecuting()
    {
        var commands = DefaultCommands.Create(engine); GridTransfer.Register(commands);
        foreach (var (oldName, replacement) in new[] { ("copexist", "-copyto"), ("tranexist", "-transferto") })
        {
            var result = commands.Execute(oldName + " \"E:\\Unused GRID\"");
            Assert.Null(result.Transfer); Assert.Contains("grid " + replacement, result.Text);
            Assert.Contains("No files changed", result.Text);
        }
    }
    [Fact]
    public void TransferHelpCompletionAndCaseInsensitiveSwitchesUseNewNames()
    {
        var commands = DefaultCommands.Create(engine); GridTransfer.Register(commands);
        Assert.False(commands.Execute("GRID -COPYTO \"E:\\GRID Copy\"").Transfer!.Move);
        Assert.True(commands.Execute("grid -TRANSFERTO \"E:\\GRID\"").Transfer!.Move);
        Assert.Contains("grid -copyto", CommandHelp.Complete("grid -c", commands.Commands));
        Assert.Contains("grid -transferto", CommandHelp.Complete("grid -t", commands.Commands));
        Assert.Contains("grid -copyto", commands.Execute("help").Text);
        Assert.Contains("grid -transferto", commands.Execute("help").Text);
    }

    [Fact]
    public async Task HostCancellationAndRedirectedInputDoNotTransfer()
    {
        var commands = DefaultCommands.Create(engine); GridTransfer.Register(commands);
        var transfer = new GridTransfer(app, world,skipRuntimeValidation:true); var destination = Path.Combine(root, "cancelled");
        var output = new StringWriter();
        using var store = new WorldStore(world); var saved = store.Load();
        var persistence = new WorldPersistenceSession(store, saved.Info, engine);
        await new TerminalHost(engine, commands, new StringReader($"grid -copyto \"{destination}\"\nn\nexit\n"), output,
            showPrompt: false, persistence: persistence, interactiveConfirmation: true, transfer: transfer).RunAsync();
        Assert.Contains("Transfer cancelled", output.ToString()); Assert.False(Directory.Exists(destination));
        output.GetStringBuilder().Clear();
        await new TerminalHost(engine, commands, new StringReader($"grid -copyto \"{destination}\"\ny\nexit\n"), output,
            showPrompt: false, persistence: persistence, transfer: transfer).RunAsync();
        Assert.Contains("interactive confirmation", output.ToString()); Assert.False(Directory.Exists(destination));
    }

    [Fact]
    public async Task ConfirmedHostCopySavesLatestStateBeforeTransfer()
    {
        var commands = DefaultCommands.Create(engine); GridTransfer.Register(commands);
        var destination = Path.Combine(root, "confirmed"); var output = new StringWriter();
        using var store = new WorldStore(world); var saved = store.Load();
        var persistence = new WorldPersistenceSession(store, saved.Info, engine);
        var exit = await new TerminalHost(engine, commands, new StringReader($"step\ngrid -copyto \"{destination}\"\ny\n"), output,
            showPrompt: false, persistence: persistence, interactiveConfirmation: true, transfer: new GridTransfer(app, world,skipRuntimeValidation:true)).RunAsync();
        Assert.Equal(0, exit); Assert.Contains("Copy complete", output.ToString());
        using var copy = new WorldStore(Path.Combine(destination, "data", "world.db"));
        Assert.Equal(engine.TickCount, copy.Load().State.TickCount);
    }
    [Fact]
    public async Task RealMoveHelperRemovesDisposableSourceAfterParentExit()
    {
        foreach (var file in Directory.GetFiles(AppContext.BaseDirectory, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(app, Path.GetRelativePath(AppContext.BaseDirectory, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(file, target);
        }
        var destination = Path.Combine(root, "helper move"); var transfer = new GridTransfer(app, world,skipRuntimeValidation:true);
        transfer.Execute(transfer.Prepare(new(destination, true)), () => true, _ => { });
        var manifest = Path.Combine(destination, "transfer-manifest.json");
        var receipt = System.Text.Json.JsonSerializer.Deserialize<TransferReceipt>(File.ReadAllText(manifest))!;
        File.WriteAllText(manifest, System.Text.Json.JsonSerializer.Serialize(receipt with { ParentId = int.MaxValue }));
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden };
        start.ArgumentList.Add(Path.Combine(destination, "app", "Grid.Terminal.dll")); start.ArgumentList.Add("--finish-move");
        using var process = System.Diagnostics.Process.Start(start)!;
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(0, process.ExitCode);
        Assert.StartsWith("Move complete", File.ReadAllText(Path.Combine(destination, "transfer-status.txt")));
        Assert.False(File.Exists(world)); Assert.False(File.Exists(Path.Combine(app, "Grid.Terminal.dll")));
        var launch = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        launch.ArgumentList.Add(Path.Combine(destination, "app", "Grid.Terminal.dll")); launch.ArgumentList.Add("--no-color");
        using var terminal = System.Diagnostics.Process.Start(launch)!;
        var stdout = terminal.StandardOutput.ReadToEndAsync(); var stderr = terminal.StandardError.ReadToEndAsync();
        await terminal.StandardInput.WriteLineAsync("help"); await terminal.StandardInput.WriteLineAsync("exit"); terminal.StandardInput.Close();
        await terminal.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));
        Assert.True(terminal.ExitCode == 0, await stderr); Assert.Contains("World saved", await stdout);
        using var restored = new WorldStore(Path.Combine(destination, "data", "world.db"));
        Assert.Equal(engine.GetSnapshot().WorldId, restored.Load().State.WorldId);

    }

    [Fact]
    public void RootDestinationsResolveToDedicatedFolderWithoutTouchingDrive()
    {
        var drive = Path.GetPathRoot(root)!;
        Assert.Equal(Path.Combine(drive, "GRID7736"), GridTransfer.ResolveDestination(drive));
        Assert.Equal(Path.Combine(root, "new"), GridTransfer.ResolveDestination(Path.Combine(root, "new")));
        Assert.Throws<IOException>(() => GridTransfer.ResolveDestination("relative"));
    }
    [Fact]
    public void GridControlsReplaceLegacyCommandsAndAttachedTransfersAreRefused()
    {
        var commands = DefaultCommands.Create(engine);
        commands.Execute("start"); Assert.False(engine.GetSnapshot().IsRunning);
        commands.Execute("grid -start"); Assert.True(engine.GetSnapshot().IsRunning);
        commands.Execute("pause"); Assert.True(engine.GetSnapshot().IsRunning);
        commands.Execute("grid -pause"); Assert.False(engine.GetSnapshot().IsRunning);
        Assert.Contains("grid -start", CommandHelp.Complete("grid -s", commands.Commands));
        GridTransfer.Register(commands, engine, false);
        Assert.Null(commands.Execute("grid -copyto -select").Transfer);
        commands.Execute("grid -start"); Assert.True(engine.GetSnapshot().IsRunning); engine.Pause();
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FolderPickerCancellationAndPreviewDoNotTouchFiles(bool select)
    {
        var commands = DefaultCommands.Create(engine); var output = new StringWriter();
        using var store = new WorldStore(world); var saved = store.Load();
        var persistence = new WorldPersistenceSession(store, saved.Info, engine);
        await new TerminalHost(engine, commands, new StringReader("grid -copyto -select\nn\nexit\n"), output,
            showPrompt: false, persistence: persistence, interactiveConfirmation: true, transfer: new GridTransfer(app, world,skipRuntimeValidation:true),
            transferPicker: _ => Task.FromResult<string?>(select ? root : null)).RunAsync();
        Assert.Contains("Transfer cancelled", output.ToString());
        if (select) Assert.Contains(Path.Combine(root, "GRID7736"), output.ToString());
        Assert.False(Directory.Exists(Path.Combine(root, "GRID7736"))); Assert.True(File.Exists(world));
    }
    [Fact]
    public async Task SelectedDestinationCopySavesAndPreservesUnrelatedContainerFiles()
    {
        var selected = Path.Combine(root, "selected"); Directory.CreateDirectory(selected);
        File.WriteAllText(Path.Combine(selected, "keep.txt"), "unrelated");
        var commands = DefaultCommands.Create(engine); var output = new StringWriter();
        using var store = new WorldStore(world); var saved = store.Load();
        await new TerminalHost(engine, commands, new StringReader("grid -copyto -select\ny\n"), output,
            showPrompt: false, persistence: new WorldPersistenceSession(store, saved.Info, engine), interactiveConfirmation: true,
            transfer: new GridTransfer(app, world,skipRuntimeValidation:true), transferPicker: _ => Task.FromResult<string?>(selected)).RunAsync();
        Assert.Contains("Copy complete", output.ToString());
        Assert.Equal("unrelated", File.ReadAllText(Path.Combine(selected,"keep.txt")));
        using var copied = new WorldStore(Path.Combine(selected, "GRID7736", "data", "world.db"));
        Assert.Equal(engine.GetSnapshot().WorldId, copied.Load().State.WorldId);
        Assert.Throws<IOException>(() => new GridTransfer(app, world,skipRuntimeValidation:true).Prepare(new(Path.Combine(selected,"GRID7736"),false)));
    }
    [Fact]
    public async Task UnavailableSelectedMediaRetainsSource()
    {
        var commands = DefaultCommands.Create(engine); var output = new StringWriter();
        using var store = new WorldStore(world); var saved = store.Load();
        await new TerminalHost(engine, commands, new StringReader("grid -transferto -select\nexit\n"), output,
            showPrompt: false, persistence: new WorldPersistenceSession(store, saved.Info, engine), interactiveConfirmation: true,
            transfer: new GridTransfer(app, world,skipRuntimeValidation:true), transferPicker: _ => Task.FromResult<string?>(Path.Combine(root,"removed-media"))).RunAsync();
        Assert.Contains("Transfer refused", output.ToString()); Assert.True(File.Exists(world));
    }

    [Fact]
    public async Task PickerCannotContinueAfterLogoutOrOpenFromRedirectedSession()
    {
        var identity = new TerminalIdentitySession(() => "picker-owner"); identity.Attach(engine); identity.Login();
        var commands = DefaultCommands.Create(engine); commands.UseIdentity(identity);
        var output = new StringWriter(); var calls = 0;
        using var store = new WorldStore(world); var saved = store.Load();
        var persistence = new WorldPersistenceSession(store, saved.Info, engine);
        Task<string?> Pick(CancellationToken token) { calls++; identity.Logout(); return Task.FromResult<string?>(root); }
        await new TerminalHost(engine, commands, new StringReader("grid -copyto -select\nexit\n"), output,
            showPrompt: false, persistence: persistence, interactiveConfirmation: true,
            transfer: new GridTransfer(app, world,skipRuntimeValidation:true), transferPicker: Pick).RunAsync();
        Assert.Equal(1, calls); Assert.Contains("session changed", output.ToString());
        Assert.False(Directory.Exists(Path.Combine(root, "GRID7736")));
        identity.Login(); output.GetStringBuilder().Clear();
        await new TerminalHost(engine, commands, new StringReader("grid -copyto -select\nexit\n"), output,
            showPrompt: false, persistence: persistence, transfer: new GridTransfer(app, world,skipRuntimeValidation:true), transferPicker: Pick).RunAsync();
        Assert.Equal(1, calls); Assert.Contains("interactive confirmation", output.ToString());
    }
}

