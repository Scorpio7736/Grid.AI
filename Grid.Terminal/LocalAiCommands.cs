using System.Diagnostics;
using Grid.Communication;
namespace Grid.Terminal;

public static class LocalAiCommands
{
    public static void Register(CommandDispatcher commands, TerminalIdentitySession identity, SharedLocalInference service, OllamaLocalBackend backend, Func<string>? runtimeStatus = null, Func<Grid.Simulation.Tasks.TaskState>? tasks = null)
    {
        commands.Register(new("ai", "ai <status|test|diagnostics [details|clear|benchmark]|authorization|profiles|personas|storage|registry>", "Inspect the shared local inference foundation or run a text-only smoke test. Conversations share this service with deterministic fallback.", args =>
        {
            if (identity.IsAttachment) return new("Use the original host terminal for local AI diagnostics.");
            if (args.Count == 1 && args[0] == "authorization")
            {
                if (!identity.IsMaster) return new("Access denied. Master Control is required.");
                var entries = TerminalRequestAuthorization.For(identity.Engine).Audit.TakeLast(20);
                return new("AI request limits: 2 concurrent, 60 starts per minute per world owner.\n" +
                    TerminalTable.Render(["Time", "Capability", "Outcome", "Session", "Generation"],
                        entries.Select(entry => new[] { entry.Time.ToString("HH:mm:ss"), entry.Capability,
                            entry.Outcome, entry.Session.ToString(), entry.Generation.ToString() })));
            }
            if (args.Count > 0 && args[0] == "diagnostics") return AiDiagnosticsCommand.Execute(args, identity, service, tasks);
            if (args.Count > 0 && args[0] == "personas") return PersonaSettings.Execute(args, identity, service, backend);
            if (args.Count > 0 && args[0] == "registry") return ModelRegistryCommands.Execute(args, identity);
            if (args.Count != 1 || args[0] is not ("status" or "test" or "profiles" or "storage")) return new("Usage: ai status | ai test | ai diagnostics | ai authorization | ai profiles | ai personas | ai storage | " + ModelRegistryCommands.Usage);
            if(args[0]=="profiles") return new(string.Join("\n\n",AgentModelProfiles.All.Select(p=>$"{p.Agent} | {p.Model} | context {p.ContextTokens} | output {p.InitialTokens}/{p.RetryTokens}\n{p.Purpose}\n{p.Fallback}")) + $"\nCPU-only: {AgentModelProfiles.CpuOnly}; reduced memory: {AgentModelProfiles.LowMemory}. One queue, one loaded model, shared weights. Model manifest pinned by SHA256.");
            if(args[0]=="storage") {
                var root=GridTransfer.PackageRoot(AppContext.BaseDirectory);
                if(root is null) return new("Use the portable package for complete base/data/staging accounting; source checkout dependencies are not a release package.");
                try { return new(PackageBudget.Measure(root).ToString()+$"\nDrive free space: {new DriveInfo(Path.GetPathRoot(root)!).AvailableFreeSpace:N0} bytes. Existing JN managed-data governance is separate from the base ceiling.\nMutable data has no automatic deletion policy. Inspect JN governance and explicit retention commands; no data deleted by this diagnostic."); }
                catch(Exception e) when(e is IOException or UnauthorizedAccessException) { return new("Storage audit failed: "+e.Message); }
            }
            var reference = identity.Reference;
            var used = 0;
            return new("", BrokerOperation: async token =>
            {
                if (Interlocked.Exchange(ref used, 1) != 0 || !identity.IsMaster || identity.Reference != reference)
                    return new("AI diagnostic session revoked or request already consumed.");
                try
                {
                    using var authorization = TerminalRequestAuthorization.AcquireStandalone(identity, "ai.diagnostic",
                        () => identity.IsMaster && identity.Reference == reference);
                    using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
                    var before = Sample(); var timer = Stopwatch.StartNew();
                    var operation = args[0] == "status" && service.Enabled ? backend.StatusAsync(stop.Token) : Test(service, stop.Token);
                    while (!operation.IsCompleted)
                    {
                        await Task.WhenAny(operation,Task.Delay(50,token));
                        if (token.IsCancellationRequested || !authorization.IsCurrent) { stop.Cancel(); break; }
                    }
                    var text = await operation;
                    if (!service.Enabled) text += "\nAI service is disabled; no HTTP probe was attempted. Resolve the startup cause before testing connectivity.";
                    if(runtimeStatus is not null) text=runtimeStatus()+"\n"+text;
                    if (!authorization.IsCurrent || token.IsCancellationRequested) return new("AI diagnostic session revoked; output discarded.");
                    var after = Sample();
                    var cpu = before.Cpu is { } b && after.Cpu is { } a && a >= b ? $"{(a-b).TotalMilliseconds:N0} CPU ms over {timer.ElapsedMilliseconds:N0} wall ms" : "unavailable";
                    return new($"LOCAL AI FOUNDATION | {DateTimeOffset.UtcNow:O}\n{text}\nEnabled: {service.Enabled} | Queue waiting: {service.Pending} | Active: {service.Active}\nOllama process-family sample: working set {after.Memory?.ToString("N0") ?? "unavailable"} bytes; {cpu}. Includes other Ollama clients; not exclusive GRID attribution.\nGPU allocation is available in ai status while the model is loaded. No host action or conversation change was performed.");
                }
                catch (OperationCanceledException) { return new("AI diagnostic cancelled."); }
                catch (Exception error) when (error is UnauthorizedAccessException or InvalidOperationException)
                { return new("AI diagnostic refused: " + error.Message); }
            });
        }));
    }
    private static async Task<string> Test(ILocalInference service,CancellationToken token)
    {
        var reply = await service.GenerateAsync("Reply with exactly: GRID LOCAL READY",token);
        return reply.Available ? "LOCAL MODEL OUTPUT (unverified): " + TerminalTable.Safe(reply.Text) + $"\nInference elapsed: {reply.ElapsedMilliseconds} ms" : reply.Reason;
    }
    private static (long? Memory,TimeSpan? Cpu) Sample()
    {
        long memory=0; var cpu=TimeSpan.Zero;var found=false;
        try
        {
            foreach(var process in Process.GetProcessesByName("ollama").Concat(Process.GetProcessesByName("llama-server")))
            { using(process) { memory+=process.WorkingSet64;cpu+=process.TotalProcessorTime;found=true; } }
            return found ? (memory,cpu) : (null,null);
        }
        catch(Exception e) when(e is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException) { return (null,null); }
    }
}
