using System.IO.Pipes;
namespace Grid.Terminal;

/// <summary>UI-only process: no engine, world file, password or runtime workers are created here.</summary>
public static class AttachedTerminal
{
    public static async Task<int> RunAsync(string name, bool noColor = false)
    {
        if (!name.StartsWith(GridBrand.AttachmentPrefix, StringComparison.Ordinal) || !Guid.TryParseExact(name[GridBrand.AttachmentPrefix.Length..], "N", out _))
        { Console.Error.WriteLine("Invalid GRID attachment endpoint."); return 1; }
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        var color = Console.ForegroundColor;
        try
        {
            await using var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.ConnectAsync(10000, stop.Token);
            var output = new TerminalOutput(Console.Out, TerminalTheme.Enabled(noColor));
            async Task<WindowReply> Receive(bool inference = false)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop.Token); timeout.CancelAfter(TimeSpan.FromSeconds(inference ? 140 : 10));
                return await WindowProtocol.ReadAsync<WindowReply>(pipe, timeout.Token);
            }
            async Task<WindowReply> Send(WindowRequest request)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop.Token); timeout.CancelAfter(TimeSpan.FromSeconds(10));
                await WindowProtocol.WriteAsync(pipe, request, timeout.Token); return await Receive(request.Kind == "command");
            }
            var reply = await Receive();
            if (!Console.IsOutputRedirected) Console.Title = "GRID.AI // " + reply.Mode;
            await output.WriteLineAsync(reply.Text);
            if (reply.Closed) return 1;
            async Task<string?> Read(ConsoleTerminalInput input, string prompt)
            {
                await TerminalTheme.PromptAsync(output, prompt); await output.FlushAsync();
                using var readStop = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                var read = input.ReadAsync(false, readStop.Token);
                try
                {
                    while (!read.IsCompleted)
                    {
                        if (await Task.WhenAny(read, Task.Delay(500, stop.Token)) == read) break;
                        stop.Token.ThrowIfCancellationRequested();
                        var heartbeat = await Send(new("ping"));
                        if (heartbeat.Closed) throw new IOException(heartbeat.Text);
                    }
                    return await read;
                }
                finally { readStop.Cancel(); try { await read; } catch (OperationCanceledException) { } }
            }
            async Task Live()
            {
                var programPage = 0; var eventPage = 0;
                do
                {
                    if (!Console.IsInputRedirected && Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key is ConsoleKey.Q or ConsoleKey.Escape) break;
                        if (key == ConsoleKey.PageDown) programPage++;
                        if (key == ConsoleKey.PageUp) programPage = Math.Max(0, programPage - 1);
                        if (key == ConsoleKey.LeftArrow) eventPage++;
                        if (key == ConsoleKey.RightArrow) eventPage = Math.Max(0, eventPage - 1);
                    }
                    var width = Console.IsOutputRedirected ? 120 : Console.WindowWidth;
                    var height = Console.IsOutputRedirected ? 40 : Math.Max(18, Console.WindowHeight - 2);
                    var frame = await Send(new("frame", ProgramPage: programPage, EventPage: eventPage, Width: width, Height: height));
                    if (frame.Closed) throw new IOException(frame.Text);
                    if (!Console.IsOutputRedirected) Console.Clear();
                    await output.WriteLineAsync("LIVE // connected to original GRID host // Q/Esc returns or closes this view");
                    await output.WriteLineAsync(frame.Text);
                    if (!frame.Live || Console.IsInputRedirected || Console.IsOutputRedirected) break;
                    await Task.Delay(250, stop.Token);
                } while (!stop.IsCancellationRequested);
            }
            if (reply.Mode == "live") { await Live(); await Send(new("close")); return 0; }
            if (reply.Mode == "snapshot")
            {
                await output.WriteLineAsync("Scroll to inspect this snapshot. Press Enter to close.");
                await Read(new ConsoleTerminalInput(new CommandDispatcher(), TerminalTheme.Enabled(noColor)), "");
                await Send(new("close")); return 0;
            }
            while (!stop.IsCancellationRequested && !reply.Closed)
            {
                var metadata = new CommandDispatcher();
                foreach (var command in reply.Commands ?? []) metadata.Register(new(command.Name, command.Usage, command.Description, _ => new("")));
                var input = new ConsoleTerminalInput(metadata, TerminalTheme.Enabled(noColor));
                var line = await Read(input, reply.Prompt);
                if (line is null) break;
                if (line.TrimStart().StartsWith("screen ask ", StringComparison.OrdinalIgnoreCase))
                    await output.WriteLineAsync("SCREEN CAPTURE REQUESTED — one selected-monitor frame; offline OCR/local AI, no continuous recording.");
                var sent = Send(new("command", line));
                reply = await (Console.IsOutputRedirected ? sent
                    : TerminalWorkStatus.AwaitWithNoticeAsync(sent, output, stop.Token));
                if (reply.PickFiles)
                {
                    if (!string.IsNullOrWhiteSpace(reply.Text)) await output.WriteLineAsync(reply.Text);
                    using var pickerStop = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                    var pick = ContextFilePicker.PickAsync(pickerStop.Token);
                    try
                    {
                        while (!pick.IsCompleted)
                        {
                            if (await Task.WhenAny(pick, Task.Delay(500, stop.Token)) == pick) break;
                            stop.Token.ThrowIfCancellationRequested();
                            var heartbeat = await Send(new("ping"));
                            if (heartbeat.Closed) throw new IOException(heartbeat.Text);
                        }
                        reply = await Send(new("context-files", Files: await pick));
                    }
                    catch (IOException e) when (pick.IsCompleted)
                    {
                        await output.WriteLineAsync("Context error: " + e.Message);
                        reply = await Send(new("context-files", Files: []));
                    }
                    finally { pickerStop.Cancel(); try { await pick; } catch (Exception e) when (e is OperationCanceledException or IOException) { } }
                }
                if (reply.ConfirmLogout)
                {
                    while (true)
                    {
                        var answer = await Read(input, "Log out and detach this window? [y/N] ");
                        if (answer is null || string.IsNullOrWhiteSpace(answer) || answer.Trim().Equals("n", StringComparison.OrdinalIgnoreCase)) { await output.WriteLineAsync("Logout cancelled."); break; }
                        if (answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)) { reply = await Send(new("logout")); break; }
                        await output.WriteLineAsync("Enter y or n.");
                    }
                }
                if (reply.Clear && !Console.IsOutputRedirected) Console.Clear();
                if (reply.Text.Length > 0)
                {
                    var animate = reply.Typewriter && TypewriterRenderer.Enabled(Console.IsInputRedirected, Console.IsOutputRedirected,
                        Environment.GetEnvironmentVariable("TERM"), null);
                    await TypewriterRenderer.WriteAsync(output, reply.Text, reply.AnimateFrom, animate,
                        TypewriterRenderer.SkipRequested,
                        async () => { if (reply.Closed) return; var heartbeat = await Send(new("ping")); if (heartbeat.Closed) throw new IOException(heartbeat.Text); }, stop.Token, presentation: reply.Presentation);
                }
                if (reply.Live) await Live();
            }
            return 0;
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { return 0; }
        catch (Exception e) when (e is IOException or TimeoutException or OperationCanceledException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            if (!Console.IsOutputRedirected) Console.Clear();
            TerminalTheme.Write(Console.Error, "GRID DISCONNECTED: " + e.Message + "\nThis window will not reload or create a world.\n", TerminalTheme.Enabled(noColor));
            return 1;
        }
        finally { Console.CancelKeyPress -= cancel; Console.ForegroundColor = color; }
    }
}
