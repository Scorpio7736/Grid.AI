using Grid.Simulation;

namespace Grid.Terminal;

/// <summary>Owns input/output and scheduler lifetime; commands are independently registered and tested.</summary>
public sealed class TerminalHost(GridSimulationEngine engine, CommandDispatcher commands,
    TextReader input, TextWriter output, bool showPrompt = true, ILiveTerminal? liveTerminal = null)
{
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var scheduler = engine.RunAsync(shutdown.Token);
        var exitCode = 0;
        try
        {
            await output.WriteLineAsync("PROJECT GRID — GENESIS");
            await output.WriteLineAsync("Type help for commands. This session is in memory; exit does not save the world.");
            while (!shutdown.IsCancellationRequested)
            {
                if (showPrompt) { await output.WriteAsync("grid> "); await output.FlushAsync(); }
                // Console.In may block synchronously even through ReadLineAsync. Keep that read
                // off the control thread so Ctrl+C and scheduler faults can still end the session.
                var read = Task.Run(() => input.ReadLine()).WaitAsync(shutdown.Token);
                var completed = await Task.WhenAny(read, scheduler);
                if (completed == scheduler)
                {
                    break;
                }
                var line = await read;
                if (line is null) break;
                var result = commands.Execute(line);
                if (result.Text.Length != 0) await output.WriteLineAsync(result.Text);
                if (result.ExitRequested) break;
                if (result.LiveViewRequested)
                {
                    // No ReadLine is outstanding while live mode owns the keyboard.
                    using var liveCancellation = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
                    var live = new TerminalLiveView(engine, output, liveTerminal).RunAsync(liveCancellation.Token);
                    try
                    {
                        if (await Task.WhenAny(live, scheduler) == scheduler) break;
                        await live;
                    }
                    finally
                    {
                        liveCancellation.Cancel();
                        try { await live; }
                        catch (OperationCanceledException) when (liveCancellation.IsCancellationRequested) { }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
        finally
        {
            shutdown.Cancel();
            try { await scheduler; }
            catch (Exception exception)
            {
                await output.WriteLineAsync($"Simulation stopped with an error: {exception.Message}");
                exitCode = 1;
            }
        }
        return exitCode;
    }
}
