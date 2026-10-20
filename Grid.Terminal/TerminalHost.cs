using Grid.Simulation;
using Grid.Persistence;
using Grid.Simulation.Resources;

namespace Grid.Terminal;

/// <summary>Owns input/output and scheduler lifetime; commands are independently registered and tested.</summary>
public sealed class TerminalHost(GridSimulationEngine engine, CommandDispatcher commands,
    TextReader input, TextWriter output, bool showPrompt = true, ILiveTerminal? liveTerminal = null, IWorldPersistenceSession? persistence = null,
    IHostTelemetryMonitor? telemetry = null, Func<bool>? clearScreen = null)
{
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var scheduler = engine.RunAsync(shutdown.Token);
        var autosave = persistence?.RunPeriodicAsync(shutdown.Token) ?? Task.Delay(Timeout.Infinite, shutdown.Token);
        var monitoring = telemetry?.RunAsync(shutdown.Token) ?? Task.Delay(Timeout.Infinite, shutdown.Token);
        var exitCode = 0;
        try
        {
            await output.WriteLineAsync("PROJECT GRID");
            await output.WriteLineAsync(persistence is null ? "Type help for commands. This session is in memory; exit does not save the world." : "Type help for commands. Autosave every 30 seconds; exit saves after the clock stops.");
            await output.WriteLineAsync("Navigation: live opens dashboard | Esc/Q returns to commands | exit or Ctrl+C shuts down GRID.");
            while (!shutdown.IsCancellationRequested)
            {
                if (showPrompt) { await output.WriteAsync("grid> "); await output.FlushAsync(); }
                // Console.In may block synchronously even through ReadLineAsync. Keep that read
                // off the control thread so Ctrl+C and scheduler faults can still end the session.
                var read = Task.Run(() => input.ReadLine()).WaitAsync(shutdown.Token);
                var completed = await Task.WhenAny(read, scheduler, autosave, monitoring);
                if (completed != read)
                {
                    break;
                }
                var line = await read;
                if (line is null) break;
                var result = commands.Execute(line);
                if (result.Text.Length != 0) await output.WriteLineAsync(result.Text);
                if (result.ClearRequested)
                {
                    var cleared = false;
                    try { cleared = clearScreen?.Invoke() == true; }
                    catch (Exception ex) when (ex is IOException or PlatformNotSupportedException or InvalidOperationException) { }
                    if (!cleared) await output.WriteLineAsync("Clear unavailable in this output; GRID state is unchanged.");
                }
                if (result.ExitRequested) break;
                if (result.LiveViewRequested)
                {
                    // No ReadLine is outstanding while live mode owns the keyboard.
                    using var liveCancellation = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
                    var live = new TerminalLiveView(engine, output, liveTerminal).RunAsync(liveCancellation.Token);
                    try
                    {
                        if (await Task.WhenAny(live, scheduler, autosave, monitoring) != live) break;
                        await live;
                        await output.WriteLineAsync("Back at command prompt. World session remains open; type live to return or exit to shut down.");
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
                await DiagnosticLog.ErrorAsync(output, "scheduler", exception, engine.GetSnapshot().WorldId);
                exitCode = 1;
            }
            try { await autosave; }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
            catch (Exception exception)
            {
                await output.WriteLineAsync($"Autosave failed: {exception.Message}");
                await DiagnosticLog.ErrorAsync(output, "autosave", exception, engine.GetSnapshot().WorldId);
                exitCode = 1;
            }
            try { await monitoring; }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
            catch (Exception exception)
            {
                await output.WriteLineAsync($"Host telemetry failed: {exception.Message}");
                await DiagnosticLog.ErrorAsync(output, "host-telemetry", exception, engine.GetSnapshot().WorldId);
                exitCode = 1;
            }
            if (persistence is not null)
            {
                try { await persistence.SaveAsync(); await output.WriteLineAsync("World saved."); }
                catch (Exception exception)
                {
                    await output.WriteLineAsync($"Shutdown save failed: {exception.Message}");
                    await DiagnosticLog.ErrorAsync(output, "shutdown-save", exception, engine.GetSnapshot().WorldId);
                    exitCode = 1;
                }
            }
        }
        await output.WriteLineAsync(TerminalPresentation.Farewell(exitCode == 0, persistence is not null));
        return exitCode;
    }
}
