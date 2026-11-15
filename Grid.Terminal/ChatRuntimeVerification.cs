using System.IO.Pipes;
using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Simulation;

namespace Grid.Terminal;

/// <summary>Live local-model acceptance through the real dispatcher and attachment protocol; isolated synthetic world.</summary>
public static class ChatRuntimeVerification
{
    public static async Task<int> RunAsync()
    {
        var checks = new List<IntelligenceVerification.Check>();
        void Check(string name, bool passed, string observed) => checks.Add(new(name, "Local conversation startup", passed, "Synthetic world; no user credentials or world opened", observed));
        try
        {
            var root = BundledAiRuntime.FindRoot(AppContext.BaseDirectory);
            ModelRegistryCommands.LoadOverride(root);
            await using var runtime = await BundledAiRuntime.StartAsync(root);
            Check("Bundled runtime", runtime.Ready, runtime.Status);
            using var backend = new OllamaLocalBackend(endpoint: runtime.Endpoint);
            await using var inference = new SharedLocalInference(backend, runtime.Ready, unavailableReason: runtime.Status);
            var registry = new TerminalSessionRegistry();
            var owner = new TerminalIdentitySession(() => "chat-runtime-verification"); registry.Add(owner);
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
            owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
            CommandDispatcher Create(TerminalIdentitySession session)
            {
                var commands = DefaultCommands.Create(engine);
                ChatCommands.Register(commands, engine, session, inference);
                commands.UseIdentity(session); return commands;
            }
            var commands = Create(owner); string endpoint = "";
            await using var hub = new WindowHub(engine, registry, Create, name => { endpoint = name; return Environment.ProcessId; });
            hub.Register(commands, owner);
            commands.Execute("connect MCP -inline");
            var inline = commands.Execute("Hello MCP");
            if (inline.BrokerOperation is { } operation) inline = await operation(default);
            bool ModelReply(string text) => !text.Contains("basic reply", StringComparison.OrdinalIgnoreCase) && text.Contains("MCP:", StringComparison.Ordinal);
            Check("Inline real model greeting", runtime.Ready && ModelReply(inline.Text), inline.Text);
            commands.Execute("return to terminal");
            var opened = commands.Execute("connect MCP");
            if (!opened.Text.Contains("Opened", StringComparison.Ordinal)) throw new IOException(opened.Text);
            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await using var pipe = new NamedPipeClientStream(".", endpoint, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.ConnectAsync(deadline.Token);
            _ = await WindowProtocol.ReadAsync<WindowReply>(pipe, deadline.Token);
            await WindowProtocol.WriteAsync(pipe, new WindowRequest("command", "Hello MCP"), deadline.Token);
            var attached = await WindowProtocol.ReadAsync<WindowReply>(pipe, deadline.Token);
            Check("Attached protocol real model greeting", runtime.Ready && ModelReply(attached.Text), attached.Text);
            if (!runtime.Ready)
                Check("Failure retained in both conversations", inline.Text.Contains(TerminalTable.Safe(runtime.Status), StringComparison.Ordinal) && attached.Text.Contains(TerminalTable.Safe(runtime.Status), StringComparison.Ordinal), "Unavailable runtime must stay unavailable; no global/cloud fallback.");
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or UnauthorizedAccessException or OperationCanceledException or ArgumentException)
        { Check("Verification exception", false, e.GetType().Name + ": " + e.Message); }
        var passed = checks.Count == 3 && checks.All(c => c.Passed);
        Console.WriteLine(JsonSerializer.Serialize(new { Passed = passed, Limitation = "Tests real local inference and named-pipe attachment dispatch, not physical window rendering or a second PC.", Checks = checks }, new JsonSerializerOptions { WriteIndented = true }));
        return passed ? 0 : 1;
    }
}
