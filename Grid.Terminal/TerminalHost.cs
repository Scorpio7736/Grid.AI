using Grid.Simulation;
using Grid.Persistence;
using Grid.Simulation.Resources;

namespace Grid.Terminal;

/// <summary>Owns input/output and scheduler lifetime; commands are independently registered and tested.</summary>
public sealed class TerminalHost(GridSimulationEngine engine, CommandDispatcher commands,
    TextReader input, TextWriter output, bool showPrompt = true, ILiveTerminal? liveTerminal = null, IWorldPersistenceSession? persistence = null,
    IHostTelemetryMonitor? telemetry = null, Func<bool>? clearScreen = null, bool interactiveConfirmation = false, TerminalSessionUi? sessionUi = null, GridTransfer? transfer = null, Func<Task>? stopAttachments = null, Func<CancellationToken, Task<string[]>>? contextPicker = null, Func<CancellationToken, Task<string?>>? transferPicker = null)
{
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var scheduler = engine.RunAsync(shutdown.Token);
        var autosave = persistence?.RunPeriodicAsync(shutdown.Token) ?? Task.Delay(Timeout.Infinite, shutdown.Token);
        var monitoring = telemetry?.RunAsync(shutdown.Token) ?? Task.Delay(Timeout.Infinite, shutdown.Token);
        var exitCode = 0;
        NullificationPlan? nullification = null;
        TransferPlan? transferPlan = null;
        try
        {
            if (sessionUi is not null && !await sessionUi.StartAsync(shutdown.Token)) shutdown.Cancel();
            await output.WriteLineAsync("PROJECT GRID");
            await output.WriteLineAsync(persistence is null ? "Type help for commands. This session is in memory; exit does not save the world." : "Type help for commands. Autosave every 30 seconds; exit saves after the clock stops.");
            if (commands.LoginRequired && sessionUi is null) await output.WriteLineAsync("Login required: login master authenticates the current Windows account.");
            await output.WriteLineAsync("Navigation: live opens dashboard | Esc/Q returns to commands | exit or Ctrl+C shuts down GRID.");
            while (!shutdown.IsCancellationRequested)
            {
                if (sessionUi is not null) await sessionUi.RefreshAsync();
                if (showPrompt) { await TerminalTheme.PromptAsync(output, commands.Prompt); await output.FlushAsync(); }
                // Console.In may block synchronously even through ReadLineAsync. Keep that read
                // off the control thread so Ctrl+C and scheduler faults can still end the session.
                var read = sessionUi is null ? Task.Run(() => input.ReadLine()).WaitAsync(shutdown.Token) : sessionUi.Input.ReadAsync(false, shutdown.Token);
                var completed = await Task.WhenAny(read, scheduler, autosave, monitoring);
                if (completed != read)
                {
                    break;
                }
                var line = await read;
                if (line is null) break;
                if (sessionUi is not null) await sessionUi.RefreshAsync();
                var result = commands.ExecuteUserInput(line);
                if (result.ConfirmTaskBatch is { } confirmBatch)
                {
                    if (!interactiveConfirmation) result = new("Bulk approval/clear requires interactive confirmation in the original host. Nothing changed.");
                    else
                    {
                        await output.WriteLineAsync(result.Text);
                        while (true)
                        {
                            await TerminalTheme.PromptAsync(output, "Confirm this task batch? [y/N] "); await output.FlushAsync();
                            var answerTask = sessionUi is null ? Task.Run(() => input.ReadLine()).WaitAsync(shutdown.Token) : sessionUi.Input.ReadAsync(false, shutdown.Token);
                            if (await Task.WhenAny(answerTask, scheduler, autosave, monitoring) != answerTask) { shutdown.Cancel(); break; }
                            var answer = await answerTask;
                            if (string.IsNullOrWhiteSpace(answer) || answer.Trim().Equals("n", StringComparison.OrdinalIgnoreCase))
                            { result = new("Task batch cancelled. Nothing changed."); break; }
                            if (answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)) { result = confirmBatch(); break; }
                            await output.WriteLineAsync("Enter y or n.");
                        }
                        if (shutdown.IsCancellationRequested) break;
                    }
                }
                if (result.BrokerOperation is { } dispatch)
                {
                    if (!string.IsNullOrWhiteSpace(result.Text)) await output.WriteLineAsync(result.Text);
                    using var brokerStop = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
                    var operation = dispatch(brokerStop.Token);
                    if (await Task.WhenAny(operation, scheduler, autosave, monitoring) != operation) brokerStop.Cancel();
                    try { result = await operation; }
                    catch (OperationCanceledException) { result = new("Task dispatch cancelled."); }
                    if (scheduler.IsCompleted || autosave.IsCompleted || monitoring.IsCompleted) break;
                }
                if (result.Voice is { } voice)
                {
                    if (!interactiveConfirmation) result = new("Voice requires an interactive host terminal; typed talk remains available.");
                    else
                    {
                        using var voiceStop = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
                        voiceStop.CancelAfter(TimeSpan.FromMinutes(2));
                        var operation = voice.Run(output, voiceStop.Token);
                        try
                        {
                            while (!operation.IsCompleted)
                            {
                                if (await Task.WhenAny(operation, scheduler, autosave, monitoring, Task.Delay(40, voiceStop.Token)) == operation) break;
                                if (voiceStop.IsCancellationRequested || !voice.IsValid() || WindowsMicrophone.EscapePressed || scheduler.IsCompleted || autosave.IsCompleted || monitoring.IsCompleted)
                                { voiceStop.Cancel(); break; }
                            }
                            result = await operation;
                            if (!voice.IsValid()) result = new("Voice session expired; operation stopped.");
                        }
                        catch (OperationCanceledException) { result = new("Voice cancelled. Nothing sent automatically."); }
                        catch (Exception e) when (e is IOException or InvalidOperationException or UnauthorizedAccessException or ArgumentException)
                        { result = new("Voice unavailable: " + e.Message + (e.Message.Contains("typed talk remains available", StringComparison.OrdinalIgnoreCase) ? "" : " Typed talk remains available.")); }
                        finally { voiceStop.Cancel(); }
                        if (scheduler.IsCompleted || autosave.IsCompleted || monitoring.IsCompleted) break;
                    }
                }
                if (result.ContextPicker is { } completeSelection)
                {
                    if (!string.IsNullOrWhiteSpace(result.Text)) await output.WriteLineAsync(result.Text);
                    if (!interactiveConfirmation) result = new("File picker requires an interactive terminal; use context add with an absolute file path.");
                    else
                    {
                        using var pickerStop = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
                        try
                        {
                            var pick = (contextPicker ?? ContextFilePicker.PickAsync)(pickerStop.Token);
                            if (await Task.WhenAny(pick, scheduler, autosave, monitoring) != pick)
                            {
                                pickerStop.Cancel();
                                try { await pick; } catch (OperationCanceledException) { }
                                break;
                            }
                            result = completeSelection(await pick);
                            if (result.BrokerOperation is { } analyze)
                            {
                                var analysis = analyze(pickerStop.Token);
                                if (await Task.WhenAny(analysis, scheduler, autosave, monitoring) != analysis) pickerStop.Cancel();
                                result = await analysis;
                            }
                        }
                        catch (IOException e) { result = new("Context error: " + e.Message); }
                    }
                }
                if (sessionUi is not null && !await sessionUi.HandleAsync(result, shutdown.Token)) break;
                if (result.Text.Length != 0)
                {
                    var generation = commands.IdentityGeneration;
                    await TypewriterRenderer.WriteAsync(output, result.Text, result.AnimateFrom,
                        interactiveConfirmation && commands.Typewriter && TypewriterRenderer.Enabled(Console.IsInputRedirected, Console.IsOutputRedirected,
                            Environment.GetEnvironmentVariable("TERM"), null),
                        TypewriterRenderer.SkipRequested,
                        () => { if (result.AnimateFrom is not null && (!commands.IsAllowed("talk") || commands.IdentityGeneration != generation)) throw new OperationCanceledException("Conversation session revoked."); return Task.CompletedTask; }, shutdown.Token, presentation: result.Presentation);
                }
                if (result.Transfer is { } request)
                {
                    if (transfer is null || persistence is null) await output.WriteLineAsync("Transfer unavailable: a persistent world is required.");
                    else if (!interactiveConfirmation) await output.WriteLineAsync("Transfer refused: interactive confirmation is required.");
                    else
                    {
                        try
                        {
                            if (request.Destination.Equals("-select", StringComparison.OrdinalIgnoreCase))
                            {
                                var generation = commands.IdentityGeneration;
                                using var pickerStop = CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
                                pickerStop.CancelAfter(TimeSpan.FromMinutes(2));
                                var pick = (transferPicker ?? TransferFolderPicker.PickAsync)(pickerStop.Token);
                                while (!pick.IsCompleted)
                                {
                                    await Task.WhenAny(pick, Task.Delay(50, shutdown.Token));
                                    if (!commands.IsAllowed("grid") || commands.IdentityGeneration != generation || scheduler.IsCompleted || autosave.IsCompleted || monitoring.IsCompleted)
                                        pickerStop.Cancel();
                                }
                                var selected = await pick;
                                if (!commands.IsAllowed("grid") || commands.IdentityGeneration != generation) throw new UnauthorizedAccessException("Transfer session changed while selecting a folder.");
                                if (selected is null) { await output.WriteLineAsync("Transfer cancelled. Files unchanged."); continue; }
                                request = request with { Destination = Path.Combine(selected, "GRID.AI") };
                            }
                            var plan = transfer.Prepare(request);
                            await output.WriteLineAsync(GridTransfer.Preview(plan)); await output.FlushAsync();
                            while (true)
                            {
                                await TerminalTheme.PromptAsync(output, "Confirm transfer? [y/N] "); await output.FlushAsync();
                                var confirmation = sessionUi is null ? Task.Run(() => input.ReadLine()).WaitAsync(shutdown.Token) : sessionUi.Input.ReadAsync(false, shutdown.Token);
                                if (await Task.WhenAny(confirmation, scheduler, autosave, monitoring) != confirmation) { shutdown.Cancel(); break; }
                                var answer = await confirmation;
                                if (string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!commands.IsAllowed("grid")) throw new UnauthorizedAccessException("Transfer authorization revoked.");
                                    transferPlan = plan; break;
                                }
                                if (string.IsNullOrWhiteSpace(answer) || answer.Trim().Equals("n", StringComparison.OrdinalIgnoreCase)) { await output.WriteLineAsync("Transfer cancelled. Files unchanged."); break; }
                                await output.WriteLineAsync("Enter y to transfer or n to cancel. [y/N]");
                            }
                        }
                        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
                        { await output.WriteLineAsync("Transfer refused: " + e.Message); }
                        catch (OperationCanceledException) when (!shutdown.IsCancellationRequested) { await output.WriteLineAsync("Transfer selection cancelled. Files unchanged."); }
                    }
                    if (transferPlan is not null) break;
                    continue;
                }
                if (result.NullificationRequested)
                {
                    if (persistence is not IWorldNullificationSession destroyer)
                        await output.WriteLineAsync("NGE unavailable: this session has no persistent world to delete.");
                    else if (!interactiveConfirmation)
                        await output.WriteLineAsync("NGE requires interactive confirmation; redirected/unattended input is rejected.");
                    else
                    {
                        try
                        {
                            var plan = destroyer.PrepareNullification();
                            await output.WriteLineAsync($"WARNING: NGE - NULLIFY GRID EXISTENCE\nWorld: {plan.WorldName} ({plan.WorldId})\nFiles to remove (if present):\n" +
                                string.Join("\n", plan.Paths) +
                                "\nSaved city, agents, clock, history and catalog will be permanently lost.\nHost applications, other worlds, historical archives and external copies remain.\nGRID will close without saving. Next startup creates a new world.\nContinue? [y/N]");
                            await output.FlushAsync();
                            while (true)
                            {
                                await TerminalTheme.PromptAsync(output, "Nullify GRID? [y/N] "); await output.FlushAsync();
                                var confirmationRead = sessionUi is null ? Task.Run(() => input.ReadLine()).WaitAsync(shutdown.Token) : sessionUi.Input.ReadAsync(false, shutdown.Token);
                                if (await Task.WhenAny(confirmationRead, scheduler, autosave, monitoring) != confirmationRead)
                                { shutdown.Cancel(); shutdown.Token.ThrowIfCancellationRequested(); }
                                var answer = await confirmationRead;
                                if (string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                                { if (!commands.IsAllowed("nge")) throw new UnauthorizedAccessException("NGE authorization was revoked."); nullification = plan; break; }
                                if (string.IsNullOrWhiteSpace(answer) || string.Equals(answer.Trim(), "n", StringComparison.OrdinalIgnoreCase))
                                { await output.WriteLineAsync("NGE cancelled. World retained."); break; }
                                await output.WriteLineAsync("Enter y to delete or n to cancel. [y/N]");
                                await output.FlushAsync();
                            }
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
                        { await output.WriteLineAsync("NGE refused: " + ex.Message); }
                    }
                    if (nullification is not null) break;
                    continue;
                }
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
                    var authorizationWatch = Task.Run(async () => {
                        try {
                            while (!liveCancellation.IsCancellationRequested) {
                                await Task.Delay(1000, liveCancellation.Token);
                                if (!commands.IsAllowed("live")) liveCancellation.Cancel();
                            }
                        } catch (OperationCanceledException) when (liveCancellation.IsCancellationRequested) { }
                    });
                    try
                    {
                        if (await Task.WhenAny(live, scheduler, autosave, monitoring) != live) break;
                        try { await live; } catch (OperationCanceledException) when (liveCancellation.IsCancellationRequested) { }
                        await output.WriteLineAsync("Back at command prompt. World session remains open; type live to return or exit to shut down.");
                    }
                    finally
                    {
                        liveCancellation.Cancel();
                        try { await live; }
                        catch (OperationCanceledException) when (liveCancellation.IsCancellationRequested) { }
                        await authorizationWatch;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { }
        finally
        {
            shutdown.Cancel();
            if (stopAttachments is not null)
            {
                try { await stopAttachments(); }
                catch (Exception e) { await output.WriteLineAsync("Attached window shutdown failed: " + e.Message); exitCode = 1; }
            }
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
            if (nullification is not null)
            {
                if (exitCode == 0)
                {
                    try { if (!commands.IsAllowed("nge")) throw new UnauthorizedAccessException("NGE authorization expired or was revoked."); await ((IWorldNullificationSession)persistence!).NullifyAsync(nullification); }
                    catch (Exception ex)
                    { await output.WriteLineAsync("NGE incomplete/refused: " + ex.Message); exitCode = 1; }
                }
                else await output.WriteLineAsync("NGE aborted because a worker failed. No final save performed; inspect existing files.");
            }
            else if (persistence is not null)
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
        if (transferPlan is not null)
        {
            if (exitCode == 0)
            {
                using var dashboard=TransferDashboard.Open(transferPlan);
                if(dashboard?.Opened==true) await output.WriteLineAsync("Transfer progress is open in a separate window. Closing that window does not cancel the transfer.");
                else await output.WriteLineAsync("The progress window could not open; transfer progress will appear here.");
                try { await output.WriteLineAsync(transfer!.Execute(transferPlan, () => commands.IsAllowed("grid"), progress: dashboard is null ? message => output.WriteLineAsync(message).GetAwaiter().GetResult() : null, observe: dashboard is null ? null : dashboard.Report)); }
                catch (Exception e) { dashboard?.Report(new("Transfer failed",0,transferPlan.Files.Sum(f=>f.Length),0,transferPlan.Files.Length,e.Message,Finished:true,Failed:true)); await output.WriteLineAsync("Transfer incomplete/refused: " + e.Message); exitCode = 1; }
            }
            else await output.WriteLineAsync("Transfer aborted because shutdown/save failed. Source retained.");
        }
        var farewell = TerminalPresentation.Farewell(exitCode == 0, persistence is not null);
        if (nullification is not null)
            farewell = farewell.Replace("WORLD SAVED // USER DISCONNECTED", "GRID NULLIFIED // EXISTENCE TERMINATED\nNext startup creates a new world.");
        await output.WriteLineAsync(farewell);
        return exitCode;
    }
}
