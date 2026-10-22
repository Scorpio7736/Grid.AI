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
        var transfer=new GridTransfer(app,world); var destination=Path.Combine(root,"with-jn"); transfer.Execute(transfer.Prepare(new(destination,false)),()=>true);
        Assert.Equal("{}",File.ReadAllText(Path.Combine(destination,"data","world.db.jn-artifacts","report.json")));
    }
    [Fact]
    public void CopyIncludesSelectedWorldMaxisArtifacts()
    {
        var artifacts = world + ".maxis-artifacts"; Directory.CreateDirectory(artifacts);
        File.WriteAllText(Path.Combine(artifacts,"report.txt"),"saved MAXIS report");
        var transfer = new GridTransfer(app,world); var destination = Path.Combine(root,"with-maxis");
        transfer.Execute(transfer.Prepare(new(destination,false)),()=>true);
        Assert.Equal("saved MAXIS report",File.ReadAllText(Path.Combine(destination,"data","world.db.maxis-artifacts","report.txt")));
    }
    [Fact]
    public void CopyPreservesSourceAndCreatesVerifiedLoadablePackage()
    {
        var transfer = new GridTransfer(app, world); var destination = Path.Combine(root, "copy with spaces");
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
        var transfer = new GridTransfer(app, world);
        foreach (var path in new[] { app, root, Path.Combine(app, "nested"), "relative-folder", Path.Combine(root, "source data", "nested") })
            Assert.Throws<IOException>(() => transfer.Prepare(new(path, true)));
        Assert.True(File.Exists(world));
    }
    [Fact]
    public void RevocationAndInventoryChangesNeverDeleteSource()
    {
        var transfer = new GridTransfer(app, world); var destination = Path.Combine(root, "denied"); var plan = transfer.Prepare(new(destination, true));
        Assert.Throws<UnauthorizedAccessException>(() => transfer.Execute(plan, () => false)); Assert.False(Directory.Exists(destination));
        File.WriteAllText(Path.Combine(app, "new.txt"), "changed");
        Assert.Throws<IOException>(() => transfer.Execute(plan, () => true)); Assert.True(File.Exists(world));
    }
    [Fact]
    public void MoveCleansOnlyVerifiedInventoryAndRetainsUnrelatedFiles()
    {
        var transfer = new GridTransfer(app, world); var destination = Path.Combine(root, "move");
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
        var transfer = new GridTransfer(app, world); var destination = Path.Combine(root, "move");
        transfer.Execute(transfer.Prepare(new(destination, true)), () => true, _ => { });
        var receipt = System.Text.Json.JsonSerializer.Deserialize<TransferReceipt>(File.ReadAllText(Path.Combine(destination, "transfer-manifest.json")))!;
        using (var active = new WorldStore(world)) Assert.Throws<IOException>(() => GridTransfer.Cleanup(receipt.Plan));
        File.WriteAllText(Path.Combine(destination, "app", "asset.txt"), "corrupted");
        Assert.Throws<IOException>(() => GridTransfer.Cleanup(receipt.Plan));
        Assert.True(File.Exists(world)); Assert.True(File.Exists(Path.Combine(app, "asset.txt")));
    }
    [Fact]
    public void TransferCommandsRequireMasterAndParseQuotedPaths()
    {
        var identity = new TerminalIdentitySession(() => "test-owner"); identity.Attach(engine);
        var commands = DefaultCommands.Create(engine); GridTransfer.Register(commands); commands.UseIdentity(identity);
        Assert.Contains("denied", commands.Execute("copexist \"E:\\GRID Copy\"").Text);
        identity.Login();
        Assert.Equal(new TransferRequest("E:\\GRID Copy", false), commands.Execute("copexist \"E:\\GRID Copy\"").Transfer);
        Assert.True(commands.Execute("tranexist \"E:\\GRID\"").Transfer!.Move);
        Assert.Contains("Usage", commands.Execute("copexist E:\\GRID Copy").Text);
        var agent = engine.EnsureCoordinator(); engine.RegisterAgentIdentity(agent.Id); identity.Switch(agent.Id.ToString());
        Assert.Contains("denied", commands.Execute("tranexist \"E:\\GRID\"").Text);
    }
    [Fact]
    public async Task HostCancellationAndRedirectedInputDoNotTransfer()
    {
        var commands = DefaultCommands.Create(engine); GridTransfer.Register(commands);
        var transfer = new GridTransfer(app, world); var destination = Path.Combine(root, "cancelled");
        var output = new StringWriter();
        using var store = new WorldStore(world); var saved = store.Load();
        var persistence = new WorldPersistenceSession(store, saved.Info, engine);
        await new TerminalHost(engine, commands, new StringReader($"copexist \"{destination}\"\nn\nexit\n"), output,
            showPrompt: false, persistence: persistence, interactiveConfirmation: true, transfer: transfer).RunAsync();
        Assert.Contains("Transfer cancelled", output.ToString()); Assert.False(Directory.Exists(destination));
        output.GetStringBuilder().Clear();
        await new TerminalHost(engine, commands, new StringReader($"copexist \"{destination}\"\ny\nexit\n"), output,
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
        var exit = await new TerminalHost(engine, commands, new StringReader($"step\ncopexist \"{destination}\"\ny\n"), output,
            showPrompt: false, persistence: persistence, interactiveConfirmation: true, transfer: new GridTransfer(app, world)).RunAsync();
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
        var destination = Path.Combine(root, "helper move"); var transfer = new GridTransfer(app, world);
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
}

