using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
namespace Grid.Terminal;

/// <summary>Opt-in synthetic persistence and local-model check; never opens the user's world.</summary>
public static class DurableMemoryVerification
{
    public static async Task<int> RunAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Grid610Verification-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var checks = new List<IntelligenceVerification.Check>();
        void Check(string name, bool passed, string observed) => checks.Add(new(name, "6.10 durable memory", passed, "Synthetic world only", observed));
        try
        {
            await using var runtime = await BundledAiRuntime.StartAsync(BundledAiRuntime.FindRoot(AppContext.BaseDirectory));
            Check("Bundled local runtime", runtime.Ready, runtime.Status);
            if (runtime.Ready)
            {
                using var backend = new OllamaLocalBackend(endpoint: runtime.Endpoint); await using var ai = new SharedLocalInference(backend);
                var registry = new TerminalSessionRegistry(); var owner = new TerminalIdentitySession(() => "durable-probe"); registry.Add(owner);
                var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
                owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
                var commands = DefaultCommands.Create(engine); var chat = ChatCommands.Register(commands, engine, owner, ai); commands.UseIdentity(owner);
                commands.Execute("connect MCP -inline");
                var now = DateTimeOffset.UtcNow;
                var document = new ContextDocument(Guid.NewGuid(), "general", "synthetic-source.txt", new string('A',64), 20, now, now, "utf8", "PRIVATE_RAW_610", engine.ResolveProgram("MCP").Id);
                engine.AddContext(owner.Reference, document, false);
                var saved = commands.Execute($"memory save-summary {document.Id} release The approved release codename is Pinecone610. --retain");
                commands.Execute("memory grant release MAXIS general"); commands.Execute("memory expiry release never");
                Check("Explicit approved summary and grant", engine.GetSnapshot().Communications.Memories.Count == 1 && engine.GetSnapshot().Communications.Memories[0].Grants.Count == 1, saved.Text);
                commands.Execute("disconnect");
                var path = Path.Combine(folder,"world.db"); var copyPath = Path.Combine(folder,"transferred.db");
                using(var store = new WorldStore(path)) store.Create(new WorldInfo(engine.GetSnapshot().WorldId,"Memory probe", now), engine.GetSnapshot());
                File.Copy(path, copyPath);
                using var copy = new WorldStore(copyPath); var snapshot = copy.Load().State;
                Check("Copied database keeps summary not temporary body", snapshot.Context.Documents.Count == 0 && !JsonSerializer.Serialize(snapshot).Contains("PRIVATE_RAW_610") && snapshot.Communications.Memories[0].Provenance?.SourceId == document.Id, "Copied isolated SQLite database; provenance retained.");
                var nextRegistry = new TerminalSessionRegistry(); var next = new TerminalIdentitySession(() => "durable-probe"); nextRegistry.Add(next);
                var restored = GridSimulationEngine.Restore(snapshot, editAuthorizer: nextRegistry); next.Attach(restored); next.Login(); restored.StartAgents(next.Reference);
                var terminal = DefaultCommands.Create(restored); var restoredChat = ChatCommands.Register(terminal, restored, next, ai); terminal.UseIdentity(next);
                async Task<CommandResult> Ask() { var reply = terminal.Execute("talk What is the approved release codename? Reply with the codename or UNKNOWN."); return reply.BrokerOperation is { } op ? await op(default) : reply; }
                terminal.Execute("connect MAXIS -inline"); var reply = await Ask();
                Check("Delegated recall with bundled model after reload", reply.Text.Contains("Pinecone610"), reply.Text);
                terminal.Execute("disconnect"); terminal.Execute("connect JN -inline"); reply = await Ask();
                Check("Unshared agent cannot recall summary", !reply.Text.Contains("Pinecone610"), reply.Text);
                terminal.Execute("disconnect"); terminal.Execute("connect MCP -inline"); terminal.Execute("memory revoke release MAXIS general");
                terminal.Execute("disconnect"); terminal.Execute("connect MAXIS -inline"); reply = await Ask();
                Check("Revoked recipient excludes prior derived recall", !reply.Text.Contains("Pinecone610"), reply.Text);
            }
        }
        catch(Exception e) { Check("Verification exception", false, e.GetType().Name + ": " + e.Message); }
        var passed = checks.Count == 6 && checks.All(c => c.Passed); var report = Path.Combine(folder,"report.json");
        await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { RunUtc=DateTimeOffset.UtcNow, Passed=passed, Scope="Synthetic world, context and copied SQLite database. No user files or saved world accessed. Model samples are not universal guarantees.", Checks=checks }, new JsonSerializerOptions { WriteIndented=true }));
        Console.WriteLine(await File.ReadAllTextAsync(report)); Console.WriteLine("Verification report: " + report);
        return passed ? 0 : 1;
    }
}
