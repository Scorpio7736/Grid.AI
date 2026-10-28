using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
namespace Grid.Terminal;

/// <summary>Opt-in synthetic conversation-only file operations. No saved user world or workspace is opened.</summary>
public static class FileOperationsVerification
{
    public static async Task<int> RunAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Grid69Verification-" + Guid.NewGuid().ToString("N"));
        var root = Path.Combine(folder, "source"); Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "sample.csproj"), "<Project />");
        var artifacts = Path.Combine(folder, "artifacts"); var target = Path.Combine(root, "probe.md");
        var checks = new List<IntelligenceVerification.Check>();
        void Check(string name, bool passed, string text) => checks.Add(new(name, "Synthetic file workflow", passed, "No user files accessed", text));
        try
        {
            await using var runtime = await BundledAiRuntime.StartAsync(BundledAiRuntime.FindRoot(AppContext.BaseDirectory));
            Check("Bundled runtime", runtime.Ready, runtime.Status);
            if (runtime.Ready)
            {
                using var backend = new OllamaLocalBackend(endpoint: runtime.Endpoint); await using var ai = new SharedLocalInference(backend);
                var sessions = new TerminalSessionRegistry(); var owner = new TerminalIdentitySession(() => "file-probe"); sessions.Add(owner);
                var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: sessions);
                owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference); engine.ConfigureMaxisRuntime(artifacts);
                engine.RegisterDevelopmentWorkspace(owner.Reference, "demo", root, "sample.csproj");
                CommandDispatcher Create(GridSimulationEngine e, TerminalIdentitySession identity)
                {
                    var terminal = DefaultCommands.Create(e); WorkspaceChangeCommands.Register(terminal, e, identity, ai);
                    ChatCommands.Register(terminal, e, identity, ai); terminal.UseIdentity(identity); terminal.Execute("connect MCP -inline"); return terminal;
                }
                var commands = Create(engine, owner);
                var pending = commands.Execute("create file probe.md in demo with instructions Write exactly one line: Indigo69");
                var draft = pending.BrokerOperation is { } generate ? await generate(default) : pending;
                Check("AI creates reviewable draft only", draft.FileChange?.State == "Draft" && draft.Text.Contains("Indigo69") && !File.Exists(target), draft.Text);
                var denied = commands.Execute("apply this change"); Check("Unapproved application refused", denied.Text.Contains("Approval required") && !File.Exists(target), denied.Text);
                commands.Execute("approve this change"); var applied = commands.Execute("apply this change");
                Check("Approved create", File.Exists(target) && File.ReadAllText(target).Trim() == "Indigo69", applied.Text);
                var read = commands.Execute("read file probe.md in demo"); Check("Scoped read", read.Text.Contains("Indigo69"), read.Text);
                commands.Execute("update file probe.md in demo with content UPDATED69"); commands.Execute("approve this change"); applied = commands.Execute("apply this change");
                Check("Approved update", File.ReadAllText(target) == "UPDATED69", applied.Text);
                var deletion = commands.Execute("delete file probe.md in demo");
                Check("Exact recoverable delete preview", deletion.FileChange is not null && deletion.Text.Contains("DELETE (1 file") && File.Exists(target), deletion.Text);
                commands.Execute("approve high-risk"); var removed = commands.Execute("apply this change"); Check("Approved delete", !File.Exists(target), removed.Text);
                using var store = new WorldStore(Path.Combine(folder, "probe.db"));
                store.Create(new WorldInfo(engine.GetSnapshot().WorldId, "File probe", DateTimeOffset.UtcNow), engine.GetSnapshot());
                var restoredSessions = new TerminalSessionRegistry(); var next = new TerminalIdentitySession(() => "file-probe"); restoredSessions.Add(next);
                var restored = GridSimulationEngine.Restore(store.Load().State, editAuthorizer: restoredSessions); restored.ConfigureMaxisRuntime(artifacts);
                next.Attach(restored); next.Login(); restored.StartAgents(next.Reference);
                var reopened = Create(restored, next);
                var recovered = reopened.Execute("recover change " + deletion.FileChange!.Id + " in demo");
                Check("Recovery after reload", recovered.Text.Contains("Recovery loaded") && !File.Exists(target), recovered.Text);
                var undo = reopened.Execute("undo this change"); Check("Exact original restored", File.ReadAllText(target) == "UPDATED69", undo.Text);
                pending = reopened.Execute("create me a index.html in demo that talks about the grid and what it does");
                draft = pending.BrokerOperation is { } htmlGenerate ? await htmlGenerate(default) : pending;
                var htmlPath = Path.Combine(root, "index.html");
                Check("Combined HTML request generates a preview", draft.FileChange?.State == "Draft" && draft.Text.Contains("<html", StringComparison.OrdinalIgnoreCase) && !File.Exists(htmlPath), draft.Text);
                reopened.Execute("approve high-risk");
                var typo = reopened.Execute("aplly this change");
                var retained = reopened.Execute("changes list");
                Check("Typo retains approved draft without applying", typo.BrokerOperation is null && typo.Text.Contains("retained") && retained.Text.Contains("State: Approved") && !File.Exists(htmlPath), typo.Text + "\n" + retained.Text);
                applied = reopened.Execute("apply this change");
                Check("Generated HTML applied after explicit command", File.Exists(htmlPath) && File.ReadAllText(htmlPath).Contains("<html", StringComparison.OrdinalIgnoreCase), applied.Text);
                reopened.Execute("create file followup.html in demo");
                pending = reopened.Execute("this should have html to display a page that talks about the grid and what it does.");
                draft = pending.BrokerOperation is { } followGenerate ? await followGenerate(default) : pending;
                Check("Description follow-up generates HTML", draft.FileChange?.State == "Draft" && draft.Text.Contains("<html", StringComparison.OrdinalIgnoreCase) && !File.Exists(Path.Combine(root, "followup.html")), draft.Text);
                reopened.Execute("approve high-risk");
                pending = reopened.Execute("make a full web page about all the parts of the grid. Expand the previous draft to about 1000 words, with eight substantive sections, navigation and CSS. Discuss saved worlds, clock, hardware city, residents, resources, agents and memory, local AI, and reviewed tasks. Use supplied facts; do not invent health metrics.");
                draft = pending.BrokerOperation is { } expand ? await expand(default) : pending;
                Check("Detailed revision stays reviewable", draft.FileChange?.State == "Draft" && draft.Text.Contains("<html", StringComparison.OrdinalIgnoreCase) && !File.Exists(Path.Combine(root, "followup.html")), "Preview characters: " + draft.Text.Length + "\n" + draft.Text);
                denied = reopened.Execute("apply this change");
                Check("Revision requires fresh approval", denied.Text.Contains("Approval required") && !File.Exists(Path.Combine(root, "followup.html")), denied.Text);
                reopened.Execute("cancel");
                Check("No tasks executed", restored.GetSnapshot().Tasks.Records.Count == 0, "No build, test or host task dispatched.");
            }
        }
        catch (Exception e) { Check("Probe exception", false, e.GetType().Name + ": " + e.Message); }
        var passed = checks.Count == 17 && checks.All(c => c.Passed);
        var report = Path.Combine(folder, "report.json");
        await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { RunUtc = DateTimeOffset.UtcNow, Passed = passed,
            Scope = "Synthetic temporary workspace and saved world only. Bundled offline AI drafts text; exact operations are approved by the test harness. No user world opened.",
            Limitation = "One generated example is not a guarantee of model correctness. Preview, approval, hashes, scope and backups are enforced independently.", Checks = checks }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(await File.ReadAllTextAsync(report)); Console.WriteLine("Verification report: " + report);
        return passed ? 0 : 1;
    }
}
