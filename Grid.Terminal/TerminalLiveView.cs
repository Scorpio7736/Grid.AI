using Grid.Simulation;

namespace Grid.Terminal;

public sealed class TerminalLiveView(GridSimulationEngine engine, TextWriter output, ILiveTerminal? terminal)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var fallback = false;
        try
        {
            if (terminal is null || !terminal.TryGetSize(out _, out _)) fallback = true;
            else
            {
                terminal.Begin();
                var programPage = 0;
                var eventPage = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Bound work even if many keys have been buffered.
                    for (var i = 0; i < 32 && terminal.TryReadKey(out var key); i++)
                    {
                        switch (key)
                        {
                            case ConsoleKey.Q or ConsoleKey.Escape: return;
                            case ConsoleKey.PageDown: programPage++; break;
                            case ConsoleKey.PageUp: programPage = Math.Max(0, programPage - 1); break;
                            case ConsoleKey.LeftArrow: eventPage++; break;
                            case ConsoleKey.RightArrow: eventPage = Math.Max(0, eventPage - 1); break;
                        }
                    }
                    if (!terminal.TryGetSize(out var width, out var height)) { fallback = true; break; }
                    var snapshot = engine.GetLiveSnapshot();
                    // Cap navigation state so repeated keys cannot accumulate past the retained data.
                    var sizes = LiveViewRenderer.PageSizes(height);
                    programPage = Math.Min(programPage, Math.Max(0, (snapshot.State.Programs.Count - 1) / sizes.Programs));
                    eventPage = Math.Min(eventPage, Math.Max(0, (snapshot.Events.Count - 1) / sizes.Events));
                    terminal.Draw(LiveViewRenderer.Render(snapshot, width, height, programPage, eventPage));
                    await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or
            ArgumentOutOfRangeException or PlatformNotSupportedException)
        {
            fallback = true;
        }
        finally
        {
            try { terminal?.End(); }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or
                ArgumentOutOfRangeException or PlatformNotSupportedException) { }
        }
        if (fallback && !cancellationToken.IsCancellationRequested)
        {
            await output.WriteLineAsync("Live redraw unavailable (requires interactive terminal, at least 60x18). Showing one snapshot.");
            foreach (var line in LiveViewRenderer.Render(engine.GetLiveSnapshot(), 120, 60, interactive: false))
                await output.WriteLineAsync(line);
        }
    }
}
