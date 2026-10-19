using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Grid.Terminal;

using var shutdown = new CancellationTokenSource();
ConsoleCancelEventHandler cancel = (_, eventArgs) => { eventArgs.Cancel = true; shutdown.Cancel(); };
Console.CancelKeyPress += cancel;
WorldStore? store = null;
try
{
    var options = TerminalOptions.Parse(args);
    GridSimulationEngine engine;
    IWorldPersistenceSession? persistence = null;
    if (options.Ephemeral) engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
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
        WorldInfo info;
        if (File.Exists(store.DatabasePath))
        {
            var saved = store.Load();
            info = saved.Info;
            engine = GridSimulationEngine.Restore(saved.State);
            Console.WriteLine($"Loaded {info.Name} ({info.Id}) at tick {engine.TickCount}.");
        }
        else
        {
            engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
            info = new(engine.GetSnapshot().WorldId, "Genesis", DateTimeOffset.UtcNow, 7736, "genesis-v1");
            store.Create(info, engine.GetSnapshot(), shutdown.Token);
            Console.WriteLine($"Created {info.Name} ({info.Id}).");
        }
        Console.WriteLine($"World database: {store.DatabasePath}");
        persistence = new WorldPersistenceSession(store, info, engine);
    }
    if (options.Start) engine.Start();
    return await new TerminalHost(engine, DefaultCommands.Create(engine), Console.In, Console.Out,
        showPrompt: !Console.IsInputRedirected, liveTerminal: new ConsoleLiveTerminal(), persistence: persistence).RunAsync(shutdown.Token);
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { return 0; }
catch (Exception exception)
{
    Console.Error.WriteLine($"GRID stopped: {exception.Message}");
    Console.Error.WriteLine("Existing worlds are never regenerated automatically. For a damaged save, use --recover with the same --world path (or the default path). Close other GRID sessions before retrying.");
    return 1;
}
finally
{
    store?.Dispose();
    Console.CancelKeyPress -= cancel;
}
