using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Terminal;
using Grid.Simulation.Resources;
using Grid.Simulation.Applications;

if (args.Length == 2 && args[0] == "--extract-document") return DocumentExtraction.RunWorker(args[1]);
if (args.SequenceEqual(new[] { "--verify-model-providers" })) return await ModelProviderVerification.RunAsync();
if (args.SequenceEqual(new[] { "--evaluate-model-baseline" })) return await ModelAcceptanceVerification.RunAsync();
if (args.SequenceEqual(new[] { "--verify-personas" })) return await PersonaVerification.RunAsync();
if (args.SequenceEqual(new[] { "--verify-generated-artifacts" })) return ArtifactVerification.Run();
if (args.SequenceEqual(new[] { "--verify-retrieval" })) return await RetrievalVerification.RunAsync();
if (args.SequenceEqual(new[] { "--verify-model-registry" }))
{
    var root = BundledAiRuntime.FindRoot(AppContext.BaseDirectory) ?? throw new IOException("No bundled runtime.");
    var manifest = ModelRegistryCommands.Baseline(root);
    var state = new Grid.AI.ModelRegistryState(1, [new(manifest, null)], new() { [manifest.Id] = manifest.Revision }, false);
    var report = await new Grid.AI.ModelRegistry(root).VerifyAsync(state);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report));
    Console.WriteLine(await ModelDeviceDiscovery.DescribeAsync(root, default));
    Console.WriteLine("Pinned baseline inventory verified; registry and saved world unchanged.");
    return 0;
}

if(args.Length==2 && args[0]=="--seal-portable-assets") { PortableAssets.Seal(args[1]); return 0; }
if(args.Length==2 && args[0]=="--prepare-portable-test") return PortableTransferVerification.Create(args[1]);
if(args.Length==3 && args[0]=="--relocate-portable-test") return PortableTransferVerification.Relocate(args[1],args[2]);
if(args.SequenceEqual(new[]{"--verify-portable"})) return await PortableTransferVerification.VerifyAsync();
if (args.SequenceEqual(new[] { "--verify-model-profiles" })) return await ModelProfileVerification.RunAsync();
if (args.Length == 2 && args[0] == "--audit-package") return PackageBudget.Audit(args[1]);
if (args.SequenceEqual(new[] { "--verify-invitations" })) return await SpecialistInvitationVerification.RunAsync();
if (args.SequenceEqual(new[] { "--verify-mcp-workflow" })) return await McpWorkflowVerification.RunAsync();
if (args.SequenceEqual(new[] { "--verify-durable-memory" })) return await DurableMemoryVerification.RunAsync();
if (args.SequenceEqual(new[] { "--verify-file-operations" })) return await FileOperationsVerification.RunAsync();
if (args.SequenceEqual(new[] { "--verify-conversation-recovery" })) return await ConversationRecoveryVerification.RunAsync();
if (args.SequenceEqual(new[] { "--verify-talk" })) return await IntelligenceVerification.RunAsync(unifiedTalk: true);
if (args.SequenceEqual(new[] { "--verify-intelligence" })) return await IntelligenceVerification.RunAsync();

if (args.SequenceEqual(new[] { "--verify-assistance" })) return await RuntimeVerification.RunAsync(assistanceOnly:true);

if (args.SequenceEqual(new[] { "--verify-runtime" })) return await RuntimeVerification.RunAsync();

if ((args.Length == 2 || args.Length == 3 && args[2] == "--no-color") && args[0] == "--attach") return await AttachedTerminal.RunAsync(args[1], args.Length == 3);
if (args.SequenceEqual(new[] { "--finish-move" })) return await GridTransfer.FinishMoveAsync(AppContext.BaseDirectory);
var packageRoot = GridTransfer.PackageRoot(AppContext.BaseDirectory);
if (packageRoot is not null && File.Exists(Path.Combine(packageRoot, "transfer-status.txt")) &&
    File.ReadAllText(Path.Combine(packageRoot, "transfer-status.txt")).Contains("pending", StringComparison.OrdinalIgnoreCase))
{ Console.Error.WriteLine("Move cleanup is pending. Check transfer-status.txt before launching."); return 1; }
using var shutdown = new CancellationTokenSource();
ConsoleCancelEventHandler cancel = (_, eventArgs) => { eventArgs.Cancel = true; shutdown.Cancel(); };
Console.CancelKeyPress += cancel;
WorldStore? store = null;
ConsoleColor? originalForeground = null;
try
{
    var options = TerminalOptions.Parse(args);
    if (TerminalTheme.Enabled(options.NoColor))
    {
        originalForeground = Console.ForegroundColor;
        Console.ForegroundColor = GridTerminalTheme.Palette[TerminalRole.Notice].Fallback;
    }
    if (packageRoot is null && TerminalEnvironment.Ensure() is { } environmentMessage)
        Console.WriteLine(environmentMessage);
    var startupMessages = new List<string>();
    if(packageRoot is not null && PortableAssets.RequiresManifest(packageRoot) && !File.Exists(Path.Combine(packageRoot,PortableAssets.Manifest))) throw new IOException("Portable asset manifest missing. Restore a complete verified package; existing world preserved.");
    var portableBoundary = packageRoot is not null && (PortableAssets.RequiresManifest(packageRoot) || File.Exists(Path.Combine(packageRoot,PortableAssets.Manifest))) && PortableDevice.Changed(packageRoot,PortableDevice.Fingerprint());
    Grid.Core.Generation.HardwareProfile? visitingHardware=null;
    if(portableBoundary) {
        startupMessages.Add("Device or package location changed. Saved city retained; machine-bound scopes and pending approvals require review. CPU-only fallback enabled for this visit.");
        visitingHardware=await new WindowsHardwareDiscovery().DiscoverAsync(shutdown.Token);
        Environment.SetEnvironmentVariable("GRID_AI_CPU_ONLY","1");
    }
    var identity = new TerminalIdentitySession(TerminalIdentitySession.WindowsSubject);
    var identities = new TerminalSessionRegistry(); identities.Add(identity);
    GridSimulationEngine engine;
    IWorldPersistenceSession? persistence = null;
    if (options.Ephemeral) engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: identities);
    else
    {
        var path = options.WorldPath ?? (packageRoot is null ? null : Path.Combine(packageRoot, "data", "world.db"));
        if (path is null)
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GRID");
            Directory.CreateDirectory(folder);
            path = Path.Combine(folder, "world.db");
        }
        store = new WorldStore(path);
        if (options.Recover) store.Recover();
        var startup = await WorldStartup.LoadOrCreateAsync(store, new WindowsHardwareDiscovery(), shutdown.Token);
        var info = startup.Saved.Info;
        var converted = store.ArchiveLegacyResidents(shutdown.Token);
        if (converted.ArchivePath is { } archive) startupMessages.Add($"Legacy fictional residents archived to {archive}; production population now follows observed applications.");
        var restoredState=portableBoundary ? PortableDevice.Reauthorize(converted.Saved.State) : converted.Saved.State;
        engine = GridSimulationEngine.Restore(restoredState, editAuthorizer: identities);
        if(portableBoundary) store.Save(info,engine.GetSnapshot(),shutdown.Token);
        startupMessages.Add($"{(startup.Created ? "Created" : "Loaded")} {info.Name} ({info.Id}) at tick {engine.TickCount}.");
        if (startup.Created)
        {
            var profile = startup.Saved.State.City!.Read().Profile;
            foreach (var category in profile.Categories.Where(c => c.Status != Grid.Core.Generation.DiscoveryStatus.Available || c.Truncated))
                startupMessages.Add($"Hardware {category.Kind}: {category.Status}; truncated={category.Truncated}. Saved fallback/partial layout; use city for coverage.");
        }
        startupMessages.Add($"World database: {store.DatabasePath}");
        persistence = new WorldPersistenceSession(store, info, engine);
    }
    if (store is not null) engine.ConfigureMaxisRuntime(store.DatabasePath + ".maxis-artifacts", Path.Combine(BundledAiRuntime.FindRoot(AppContext.BaseDirectory) ?? AppContext.BaseDirectory, "dotnet"));
    if (store is not null) engine.ConfigureJnRuntime(store.DatabasePath, packageRoot ?? AppContext.BaseDirectory);
    if (store is not null) engine.ConfigureRelayVoice(new LocalRelayVoice());
    identity.Attach(engine);
    if (persistence is not null) identity.PersistIdentity = () => persistence.SaveAsync().GetAwaiter().GetResult();
    void StartGrid(TerminalIdentitySession session)
    {
        engine.StartAgents(session.Reference);
        try { if (persistence is not null) persistence.SaveAsync().GetAwaiter().GetResult(); }
        catch { engine.Pause(); throw; }
    }
    var pendingStart = options.Start;
    identity.LoginSucceeded += () => { foreach (var message in startupMessages) Console.WriteLine(message); startupMessages.Clear();
        if(visitingHardware is {} hardware && packageRoot is not null) {
            Console.WriteLine("Discovered this device's hardware. Save as another device [s], or use one time [Enter]? Existing city/world is unchanged.");
            if(!Console.IsInputRedirected && string.Equals(Console.ReadLine()?.Trim(),"s",StringComparison.OrdinalIgnoreCase)) {
                PortableDevice.Remember(packageRoot,PortableDevice.Fingerprint(),hardware);Console.WriteLine("Device profile saved. Re-register local workspaces/storage scopes explicitly; nothing is retargeted automatically.");
            } else Console.WriteLine("Using one time. Hardware profile remains session-only; permission revocations remain saved.");
            visitingHardware=null;
        }
        if (pendingStart) { pendingStart = false; StartGrid(identity); } };
    Console.WriteLine("Checking bundled local AI...");
    startupMessages.Add(ModelRegistryCommands.LoadOverride(BundledAiRuntime.FindRoot(AppContext.BaseDirectory)));
    await using var aiRuntime = await BundledAiRuntime.StartAsync(BundledAiRuntime.FindRoot(AppContext.BaseDirectory), shutdown.Token);
    startupMessages.Add(aiRuntime.Status);
    var personas = new Grid.Agent.AgentPersonas();
    try { if (BundledAiRuntime.FindRoot(AppContext.BaseDirectory) is { } personaRoot) personas = PersonaSettings.Read(personaRoot); }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException)
    { startupMessages.Add("Persona settings unreadable; using bundled v2 defaults. Original settings retained: " + e.Message); }
    using var aiBackend = new Grid.Communication.OllamaLocalBackend(endpoint: aiRuntime.Endpoint, personas: personas);
    await using var localAi = new Grid.Communication.SharedLocalInference(aiBackend, enabled: aiRuntime.Ready && Environment.GetEnvironmentVariable("GRID_AI_DISABLED") != "1", unavailableReason: aiRuntime.Status);
    CommandDispatcher CreateCommands(TerminalIdentitySession session)
    {
        var dispatcher = DefaultCommands.Create(engine);
        if (!session.IsAttachment) LocalAiCommands.Register(dispatcher, session, localAi, aiBackend, () => "Startup: "+aiRuntime.Status, () => engine.TasksFor(session.Reference));
        CityCommand.Register(dispatcher, engine); BehaviorCommand.Register(dispatcher, engine);
        WorkplaceCommands.Register(dispatcher, engine); SocialCommand.Register(dispatcher, engine);
        VoltRelayCommands.Register(dispatcher, engine, session, persistence is null ? null : token => persistence.SaveAsync(token));
        JnCommands.Register(dispatcher, engine, session, persistence is null ? null : token => persistence.SaveAsync(token));
        WorkspaceChangeCommands.Register(dispatcher, engine, session, localAi);
        MaxisCommands.Register(dispatcher, engine, session, persistence is null ? null : token => persistence.SaveAsync(token));
        TaskCommands.Register(dispatcher, engine, session, persistence is null ? null : token => persistence.SaveAsync(token));
        var chat = ChatCommands.Register(dispatcher, engine, session, localAi, persistence is null ? null : token => persistence.SaveAsync(token)); ResourcesCommand.Register(dispatcher, engine);
        VoiceCommands.Register(dispatcher, engine, session, chat, save: persistence is null ? null : () => persistence.SaveAsync().GetAwaiter().GetResult());
        ContextCommands.Register(dispatcher, engine, session, persistence is null ? null : () => persistence.SaveAsync().GetAwaiter().GetResult(), shutdown.Token);
        if (store is not null) HistoryCommand.Register(dispatcher, store);
        IdentityCommands.Register(dispatcher, engine, session, interactiveUi: true);
        GridTransfer.Register(dispatcher, engine, !session.IsAttachment, () => StartGrid(session));
        if (session.IsAttachment) foreach (var command in new[] { "login", "credentials", "nge" }) dispatcher.Remove(command);
        dispatcher.UseIdentity(session); return dispatcher;
    }
    var commands = CreateCommands(identity);
    await using var windows = new WindowHub(engine, identities, CreateCommands, noColor: options.NoColor);
    windows.Register(commands, identity);
    var telemetry = new HostTelemetryMonitor(new WindowsHostTelemetryProvider(), engine);
    await telemetry.SampleOnceAsync(shutdown.Token);
    var applications = options.Ephemeral ? null : new ApplicationMonitor(new WindowsApplicationDiscovery(), engine);
    if (applications is not null) await applications.SampleOnceAsync(shutdown.Token);
    bool ClearScreen()
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected || Environment.GetEnvironmentVariable("TERM") == "dumb") return false;
        Console.Clear(); return true;
    }
    var output = new TerminalOutput(Console.Out, TerminalTheme.Enabled(options.NoColor));
    var terminalInput = new ConsoleTerminalInput(commands, TerminalTheme.Enabled(options.NoColor));
    var sessionUi = new TerminalSessionUi(identity, terminalInput, output, ClearScreen);
    return await new TerminalHost(engine, commands, Console.In, output,
        showPrompt: terminalInput.Interactive, interactiveConfirmation: terminalInput.Interactive,
        liveTerminal: new ConsoleLiveTerminal(options.NoColor), persistence: persistence,
        telemetry: applications is null ? telemetry : new CombinedHostMonitor(telemetry, applications),
        clearScreen: ClearScreen, sessionUi: sessionUi, stopAttachments: windows.StopAsync, transfer: store is null ? null : new GridTransfer(AppContext.BaseDirectory, store.DatabasePath)).RunAsync(shutdown.Token);
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { return 0; }
catch (Exception exception)
{
    Console.Error.WriteLine($"GRID stopped: {exception.Message}");
    await DiagnosticLog.ErrorAsync(Console.Error, "startup-or-session", exception);
    Console.Error.WriteLine("Existing worlds are never regenerated automatically. For a damaged save, use --recover with the same --world path (or the default path). Close other GRID sessions before retrying.");
    return 1;
}
finally
{
    store?.Dispose();
    Console.CancelKeyPress -= cancel;
    if (originalForeground is { } foreground) Console.ForegroundColor = foreground;
}
