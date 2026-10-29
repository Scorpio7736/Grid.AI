using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
using Status = Grid.Simulation.Tasks.TaskStatus;
namespace Grid.Terminal;

/// <summary>Opt-in bundled-model workflow acceptance, using only an isolated synthetic world.</summary>
public static class McpWorkflowVerification
{
    public static async Task<int> RunAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Grid611Verification-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var checks = new List<IntelligenceVerification.Check>();
        void Check(string name, bool passed, string observed) => checks.Add(new(name, "6.11 MCP workflow", passed, "Isolated synthetic world", observed));
        try
        {
            await using var runtime = await BundledAiRuntime.StartAsync(BundledAiRuntime.FindRoot(AppContext.BaseDirectory));
            Check("Bundled runtime", runtime.Ready, runtime.Status);
            if (runtime.Ready)
            {
                using var backend = new OllamaLocalBackend(endpoint: runtime.Endpoint); await using var ai = new SharedLocalInference(backend);
                var registry = new TerminalSessionRegistry(); var owner = new TerminalIdentitySession(() => "mcp-probe"); registry.Add(owner);
                var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
                owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
                using var store = new WorldStore(Path.Combine(folder, "world.db"));
                var info = new WorldInfo(engine.GetSnapshot().WorldId, "MCP verification", DateTimeOffset.UtcNow); store.Create(info, engine.GetSnapshot());
                Task Save(CancellationToken token) { store.Save(info, engine.GetSnapshot(), token); return Task.CompletedTask; }
                var commands = DefaultCommands.Create(engine); TaskCommands.Register(commands, engine, owner, Save);
                ChatCommands.Register(commands, engine, owner, ai, Save); commands.UseIdentity(owner); commands.Execute("connect MCP -inline");
                async Task<CommandResult> Run(string text) { var result = commands.Execute(text); return result.BrokerOperation is {} op ? await op(default) : result; }
                var reply = await Run("mcp plan Build my project. I have not specified which workspace.");
                Check("Clarify missing registered target", reply.Text.Contains("No tasks created") && engine.GetSnapshot().Tasks.Records.Count == 0, reply.Text);
                await Run("talk cancel");
                reply = await Run("mcp plan Read current host resource observations with VOLT and record a local RELAY notification saying Resource review requested. Do not do anything else.");
                Check("Custom multi-specialist draft", reply.Text.Contains("DRAFT ONLY") && reply.Text.Contains("volt.status") && reply.Text.Contains("relay.notify"), reply.Text);
                await Run("talk submit plan"); var plan = engine.GetSnapshot().Tasks.Plans.Single();
                reply = await Run("mcp run " + plan.Id);
                Check("No dispatch before approval", reply.Text.Contains("approve every") && engine.GetSnapshot().Tasks.Records.All(t => t.Attempts == 0), reply.Text);
                foreach (var id in plan.TaskIds) await Run("tasks approve " + id);
                reply = await Run("mcp run " + plan.Id);
                Check("Approved real capability results", engine.GetSnapshot().Tasks.Records.All(t => t.Status == Status.Completed && t.Attempts == 1) && engine.GetSnapshot().Tasks.Relay.Notifications.Count > 0, reply.Text);
                var restored = store.Load().State;
                Check("Saved evidence and no replay", restored.Tasks.Plans.Single().Id == plan.Id && restored.Tasks.Records.All(t => t.Status == Status.Completed && t.Result is not null), "SQLite retains plan IDs, approvals, attempts and real adapter receipts; no user world opened.");
            }
        }
        catch (Exception e) { Check("Verification exception", false, e.GetType().Name + ": " + e.Message); }
        var passed = checks.Count == 6 && checks.All(c => c.Passed);
        var report = Path.Combine(folder, "report.json");
        await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { Passed = passed, Checks = checks }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(await File.ReadAllTextAsync(report)); Console.WriteLine("Verification report: " + report);
        return passed ? 0 : 1;
    }
}
