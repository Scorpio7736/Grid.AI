using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Terminal;
using Grid.Simulation.Resources;
using Grid.Simulation.Applications;

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
        Console.ForegroundColor = ConsoleColor.Cyan;
    }
    if (packageRoot is null && TerminalEnvironment.Ensure() is { } environmentMessage)
        Console.WriteLine(environmentMessage);
    var startupMessages = new List<string>();
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
        engine = GridSimulationEngine.Restore(converted.Saved.State, editAuthorizer: identities);
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
    if (store is not null) engine.ConfigureMaxisRuntime(store.DatabasePath + ".maxis-artifacts");
    if (store is not null) engine.ConfigureJnRuntime(store.DatabasePath, packageRoot ?? AppContext.BaseDirectory);
    if (store is not null) engine.ConfigureRelayVoice(new LocalRelayVoice());
    identity.Attach(engine);
    if (persistence is not null) identity.PersistIdentity = () => persistence.SaveAsync().GetAwaiter().GetResult();
    var pendingStart = options.Start;
    identity.LoginSucceeded += () => { foreach (var message in startupMessages) Console.WriteLine(message); startupMessages.Clear(); if (pendingStart) { pendingStart = false; engine.Start(); } };
    using var aiBackend = new Grid.Communication.OllamaLocalBackend();
    await using var localAi = new Grid.Communication.SharedLocalInference(aiBackend, enabled: Environment.GetEnvironmentVariable("GRID_AI_DISABLED") != "1");
    CommandDispatcher CreateCommands(TerminalIdentitySession session)
    {
        var dispatcher = DefaultCommands.Create(engine);
        if (!session.IsAttachment) LocalAiCommands.Register(dispatcher, session, localAi, aiBackend);
        CityCommand.Register(dispatcher, engine); BehaviorCommand.Register(dispatcher, engine);
        WorkplaceCommands.Register(dispatcher, engine); SocialCommand.Register(dispatcher, engine);
        VoltRelayCommands.Register(dispatcher, engine, session, persistence is null ? null : token => persistence.SaveAsync(token));
        JnCommands.Register(dispatcher, engine, session, persistence is null ? null : token => persistence.SaveAsync(token));
        MaxisCommands.Register(dispatcher, engine, session, persistence is null ? null : token => persistence.SaveAsync(token));
        TaskCommands.Register(dispatcher, engine, session, persistence is null ? null : token => persistence.SaveAsync(token));
        var chat = ChatCommands.Register(dispatcher, engine, session); ResourcesCommand.Register(dispatcher, engine);
        VoiceCommands.Register(dispatcher, engine, session, chat, save: persistence is null ? null : () => persistence.SaveAsync().GetAwaiter().GetResult());
        ContextCommands.Register(dispatcher, engine, session, persistence is null ? null : () => persistence.SaveAsync().GetAwaiter().GetResult());
        if (store is not null) HistoryCommand.Register(dispatcher, store);
        IdentityCommands.Register(dispatcher, engine, session, interactiveUi: true);
        if (!session.IsAttachment) GridTransfer.Register(dispatcher);
        else foreach (var command in new[] { "login", "credentials", "nge" }) dispatcher.Remove(command);
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
