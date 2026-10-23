using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Grid.Simulation;
namespace Grid.Terminal;

/// <summary>The original host alone owns workers/storage. Windows are scoped, process-bound local clients.</summary>
public sealed class WindowHub(GridSimulationEngine engine, TerminalSessionRegistry registry,
    Func<TerminalIdentitySession, CommandDispatcher> createCommands, Func<string, int>? launcher = null, bool noColor = false) : IAsyncDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly object gate = new();
    private readonly List<Task> clients = new();
    private bool stopping;
    private int active;
    public void Register(CommandDispatcher commands, TerminalIdentitySession identity)
    {
        commands.Register(new("ntm", "ntm", "Open a terminal attached to this GRID; exit there only detaches.", args =>
            args.Count == 0 ? Open(identity, commands, null) : new("Usage: ntm")) { Capability = "session" });
        commands.OpenWindow = tokens => Open(identity, commands, tokens);
        commands.OpenConversation = tokens => Open(identity, commands, tokens, conversation: true);
    }
    public CommandResult Open(TerminalIdentitySession owner, CommandDispatcher source, IReadOnlyList<string>? view, bool conversation = false)
    {
        lock (gate)
        {
            if (stopping) return new("Error: GRID host is shutting down.");
            if (active >= 16) return new("Error: maximum of 16 attached windows reached; close one first.");
            if (!owner.IsAuthenticated) return new("Access denied. Login before opening windows.");
            if (view is not null && (view.Count == 0 || !(conversation ? view[0].ToLowerInvariant() is "connect" or "resume" : WindowCommands.Supports(view[0])) || !source.IsAllowed(view[0]))) return new("Access denied or unsupported data command.");
            TerminalIdentitySession? identity = null; NamedPipeServerStream? pipe = null;
            try
            {
                var mode = conversation ? "conversation" : view is null ? "terminal" : view[0].Equals("live", StringComparison.OrdinalIgnoreCase) ? "live" : "snapshot";
                if (mode == "live" && view!.Count != 1) return new("Usage: live -opninwin");
                var snapshot = mode == "snapshot" ? $"SNAPSHOT captured {DateTimeOffset.UtcNow:O}; not continuously refreshed.\n" + source.ExecuteTokens(view!).Text : "";
                identity = owner.CreateAttachment(); registry.Add(identity);
                var commands = createCommands(identity);
                if (!conversation) Register(commands, identity);
                else
                {
                    var allowed = new[] { "help", "exit", "clear", "logout", "connect", "talk", "resume", "disconnect", "transcript", "conversations", "context", "whoami" };
                    foreach (var command in commands.Commands.ToArray())
                        if (!allowed.Contains(command.Name)) commands.Remove(command.Name);
                }
                var name = "GRID7736-" + Guid.NewGuid().ToString("N");
                pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                var pid = (launcher ?? Launch)(name);
                var task = Serve(pipe, pid, identity, commands, mode, snapshot, view?.FirstOrDefault(), conversation ? view : null);
                clients.RemoveAll(t => t.IsCompleted); clients.Add(task); active++;
                return new("Opened " + mode + " window connected to this GRID. Closing it only detaches.");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
            {
                pipe?.Dispose(); if (identity is not null) { registry.Remove(identity); identity.Logout(); }
                return new("Error: window could not open: " + e.Message);
            }
        }
    }
    private async Task Serve(NamedPipeServerStream pipe, int pid, TerminalIdentitySession identity, CommandDispatcher commands, string mode, string snapshot, string? viewCommand, IReadOnlyList<string>? initialConversation = null)
    {
        await Task.Yield();
        try
        {
            using var connect = CancellationTokenSource.CreateLinkedTokenSource(stop.Token); connect.CancelAfter(TimeSpan.FromSeconds(60));
            await pipe.WaitForConnectionAsync(connect.Token);
            if (OperatingSystem.IsWindows() && (!GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var clientPid) || clientPid != pid))
                throw new UnauthorizedAccessException("Attachment process mismatch.");
            using var io = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            io.CancelAfter(TimeSpan.FromSeconds(10));
            var typewriter = true;
            if (mode == "conversation") commands.Register(new("typewriter", "typewriter <on|off>", "Animate agent replies; any key skips the current animation. Off displays instantly.", args =>
            {
                if (args.Count != 1 || args[0].ToLowerInvariant() is not ("on" or "off")) return new("Usage: typewriter <on|off>");
                typewriter = args[0].Equals("on", StringComparison.OrdinalIgnoreCase);
                return new("Typewriter " + (typewriter ? "on. Any key skips animation." : "off. Responses display instantly."));
            }) { Capability = "chat" });
            WindowReply Reply(string text = "", bool closed = false, bool live = false, bool clear = false, bool logout = false) =>
                new(text, mode == "conversation" ? "You> " : commands.Prompt, commands.Commands.Select(c => new WindowCommandInfo(c.Name, c.Usage, c.Description)).ToArray(), closed, live, clear, logout, mode, Typewriter: typewriter);
            if (!identity.IsAuthenticated || viewCommand is not null && !commands.IsAllowed(viewCommand))
            { await WindowProtocol.WriteAsync(pipe, Reply("Session revoked before attachment.", closed: true), io.Token); return; }
            var conversationText = "";
            if (initialConversation is not null)
            {
                conversationText = commands.ExecuteTokens(initialConversation).Text.Replace("Ordinary GRID commands still work.", "Use the original terminal for ordinary GRID commands.");
                if (commands.ConversationConnected?.Invoke() != true)
                { await WindowProtocol.WriteAsync(pipe, Reply(conversationText, closed: true), io.Token); return; }
                conversationText += "\nTypewriter: any key skips; typewriter off disables it. Exit closes this window; disconnect ends the conversation.";
            }
            await WindowProtocol.WriteAsync(pipe, Reply(mode == "conversation" ? "GRID7736 // CONVERSATION\n\n" + conversationText :
                $"GRID7736 // ATTACHED {mode.ToUpperInvariant()}\n{identity.Describe()}\nOriginal host owns simulation/save. Closing this window only detaches.\n" +
                (mode == "snapshot" ? snapshot : conversationText)), io.Token);
            Func<IReadOnlyList<string>, CommandResult>? pendingPicker = null;
            while (!stop.IsCancellationRequested)
            {
                io.CancelAfter(TimeSpan.FromSeconds(30));
                var request = await WindowProtocol.ReadAsync<WindowRequest>(pipe, io.Token);
                WindowReply reply;
                if (!identity.IsAuthenticated || viewCommand is not null && !commands.IsAllowed(viewCommand))
                    reply = Reply("Session revoked or expired. Reopen from an authenticated terminal.", closed: true);
                else if (request.Kind == "close") reply = Reply("Detached. GRID continues in the original host.", closed: true);
                else if (request.Kind == "ping") reply = Reply();
                else if (request.Kind == "frame" && (mode == "terminal" || mode == "live"))
                    reply = commands.IsAllowed("live") ? Reply(string.Join("\n", LiveViewRenderer.Render(engine.GetLiveSnapshot(), Math.Clamp(request.Width, 60, 200), Math.Clamp(request.Height, 18, 100), Math.Clamp(request.ProgramPage, 0, 10000), Math.Clamp(request.EventPage, 0, 10000))), live: true) : Reply("Access denied: live requires Master Control.");
                else if (mode != "terminal" && mode != "conversation") reply = Reply("Data windows are read-only.");
                else if (request.Kind == "context-files")
                {
                    var complete = pendingPicker; pendingPicker = null;
                    reply = complete is null ? Reply("Error: no file selection pending.") : Reply(complete(request.Files ?? []).Text);
                }
                else if (request.Kind == "logout") { identity.Logout(); reply = Reply("Logged out; attached window detached.", closed: true); }
                else if (request.Kind == "command" && request.Command is { Length: <= 8192 } line)
                {
                    // Lifecycle changes stay on the owning host, so there is one final save/delete/transfer authority.
                    string name;
                    try { name = CommandDispatcher.Parse(line).FirstOrDefault() ?? ""; }
                    catch (ArgumentException e) { await WindowProtocol.WriteAsync(pipe, Reply("Error: " + e.Message), io.Token); continue; }
                    if (new[] { "login", "credentials", "nge", "grid", "tranexist", "copexist" }.Contains(name, StringComparer.OrdinalIgnoreCase))
                        reply = Reply("Use the original host terminal for login, credentials, NGE or transfers. This window does not own the save.");
                    else
                    {
                        pendingPicker = null;
                        var result = commands.Execute(line);
                        if (result.BrokerOperation is { } operation && name.Equals("talk", StringComparison.OrdinalIgnoreCase))
                        {
                            io.CancelAfter(TimeSpan.FromSeconds(140));
                            using var inferenceStop = CancellationTokenSource.CreateLinkedTokenSource(io.Token);
                            var completion = operation(inferenceStop.Token);
                            while (!completion.IsCompleted)
                            {
                                await Task.WhenAny(completion, Task.Delay(100, io.Token));
                                try { using var client = Process.GetProcessById(pid); if (client.HasExited) inferenceStop.Cancel(); }
                                catch (ArgumentException) { inferenceStop.Cancel(); }
                                if (inferenceStop.IsCancellationRequested) break;
                            }
                            result = await completion;
                        }
                        pendingPicker = result.ContextPicker;
                        reply = Reply(result.Text, result.ExitRequested, result.LiveViewRequested, result.ClearRequested, result.LogoutRequested) with { PickFiles = pendingPicker is not null, AnimateFrom = result.AnimateFrom };
                    }
                }
                else reply = Reply("Error: unsupported window request.");
                io.CancelAfter(TimeSpan.FromSeconds(10));
                await WindowProtocol.WriteAsync(pipe, reply, io.Token);
                if (reply.Closed) break;
            }
        }
        catch (Exception e) when (e is IOException or OperationCanceledException or UnauthorizedAccessException or System.Text.Json.JsonException or ArgumentException or InvalidOperationException) { }
        finally
        {
            pipe.Dispose(); registry.Remove(identity); identity.Logout();
            lock (gate) active--;
        }
    }
    private int Launch(string pipe)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("New console windows currently require Windows.");
        var executable = Path.ChangeExtension(typeof(WindowHub).Assembly.Location, ".exe");
        if (!File.Exists(executable)) throw new IOException("Grid.Terminal.exe is missing; build/publish the apphost first.");
        // Visible window is explicitly requested by ntm/-opninwin; no shell command interpolation.
        var start = new ProcessStartInfo(executable) { UseShellExecute = true, WorkingDirectory = AppContext.BaseDirectory, WindowStyle = ProcessWindowStyle.Normal };
        start.ArgumentList.Add("--attach"); start.ArgumentList.Add(pipe);
        if (noColor) start.ArgumentList.Add("--no-color");
        using var process = Process.Start(start) ?? throw new IOException("Window launch failed.");
        return process.Id;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe, out uint clientProcessId);
    public async Task StopAsync()
    {
        Task[] pending;
        lock (gate) { stopping = true; stop.Cancel(); pending = clients.ToArray(); }
        await Task.WhenAll(pending);
    }
    public async ValueTask DisposeAsync() { await StopAsync(); stop.Dispose(); }
}
