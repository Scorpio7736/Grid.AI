using Grid.Core;
using Grid.Simulation;
using Grid.Terminal;

var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
if (args.Contains("--start", StringComparer.OrdinalIgnoreCase)) engine.Start();
var commands = DefaultCommands.Create(engine);
using var shutdown = new CancellationTokenSource();
ConsoleCancelEventHandler cancel = (_, args) =>
{
    args.Cancel = true;
    shutdown.Cancel();
};
Console.CancelKeyPress += cancel;
try
{
    return await new TerminalHost(engine, commands, Console.In, Console.Out,
        showPrompt: !Console.IsInputRedirected).RunAsync(shutdown.Token);
}
finally
{
    Console.CancelKeyPress -= cancel;
}
