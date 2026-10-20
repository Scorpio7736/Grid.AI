using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Terminal;
using Grid.Simulation.Resources;
using Grid.Simulation.Applications;

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
    if (TerminalEnvironment.Ensure() is { } environmentMessage)
        Console.WriteLine(environmentMessage);
    var startupMessages = new List<string>();
    var identity = new TerminalIdentitySession(TerminalIdentitySession.WindowsSubject);
    GridSimulationEngine engine;
    IWorldPersistenceSession? persistence = null;
    if (options.Ephemeral) engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: identity);
    else
    {
        var path = options.WorldPath;
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
        engine = GridSimulationEngine.Restore(converted.Saved.State, editAuthorizer: identity);
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
    identity.Attach(engine);
    if (persistence is not null) identity.PersistIdentity = () => persistence.SaveAsync().GetAwaiter().GetResult();
    var pendingStart = options.Start;
    identity.LoginSucceeded += () => { foreach (var message in startupMessages) Console.WriteLine(message); startupMessages.Clear(); if (pendingStart) { pendingStart = false; engine.Start(); } };
    var commands = DefaultCommands.Create(engine);
    CityCommand.Register(commands, engine);
    BehaviorCommand.Register(commands, engine);
    WorkplaceCommands.Register(commands, engine);
    SocialCommand.Register(commands, engine);
    ChatCommands.Register(commands, engine, identity);
    if (store is not null) HistoryCommand.Register(commands, store);
    var telemetry = new HostTelemetryMonitor(new WindowsHostTelemetryProvider(), engine);
    await telemetry.SampleOnceAsync(shutdown.Token);
    var applications = options.Ephemeral ? null : new ApplicationMonitor(new WindowsApplicationDiscovery(), engine);
    if (applications is not null) await applications.SampleOnceAsync(shutdown.Token);
    ResourcesCommand.Register(commands, engine);
    IdentityCommands.Register(commands, engine, identity);
    commands.UseIdentity(identity);
    return await new TerminalHost(engine, commands, Console.In, new TerminalOutput(Console.Out, TerminalTheme.Enabled(options.NoColor)),
        showPrompt: !Console.IsInputRedirected, interactiveConfirmation: !Console.IsInputRedirected && !Console.IsOutputRedirected, liveTerminal: new ConsoleLiveTerminal(options.NoColor), persistence: persistence,
        telemetry: applications is null ? telemetry : new CombinedHostMonitor(telemetry, applications), clearScreen: () =>
        {
            if (Console.IsOutputRedirected || Console.IsInputRedirected || Environment.GetEnvironmentVariable("TERM") == "dumb") return false;
            Console.Clear(); return true;
        }).RunAsync(shutdown.Token);
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
