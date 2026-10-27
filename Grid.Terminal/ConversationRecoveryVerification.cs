using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
namespace Grid.Terminal;

/// <summary>Opt-in live model evidence using synthetic documents; never opens the user's world.</summary>
public static class ConversationRecoveryVerification
{
    public static async Task<int> RunAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Grid68Verification-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var checks = new List<IntelligenceVerification.Check>();
        void Record(string name, string category, bool passed, string observed) => checks.Add(new(name, category, passed, "6.8 isolated verification", observed));
        try
        {
            await using var runtime = await BundledAiRuntime.StartAsync(BundledAiRuntime.FindRoot(AppContext.BaseDirectory));
            Record("Bundled runtime", "Availability", runtime.Ready, runtime.Status);
            if (runtime.Ready)
            {
                using var backend = new OllamaLocalBackend(endpoint: runtime.Endpoint);
                await using var ai = new SharedLocalInference(backend);
                var registry = new TerminalSessionRegistry();
                var owner = new TerminalIdentitySession(() => "recovery-verification"); registry.Add(owner);
                var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
                owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
                var commands = DefaultCommands.Create(engine);
                ChatCommands.Register(commands, engine, owner, ai);
                ContextCommands.Register(commands, engine, owner);
                commands.UseIdentity(owner); commands.Execute("connect MCP -inline");
                async Task<CommandResult> Complete(CommandResult result) => result.BrokerOperation is { } op ? await op(default) : result;
                var css = await Complete(commands.Execute("How can I horizontally center a 200px div? Give working CSS."));
                var text = css.Text.ToLowerInvariant();
                Record("CSS centering", "Model quality", (text.Contains("200px") && text.Contains("auto")) ||
                    (text.Contains("flex") && text.Contains("justify-content: center")) || (text.Contains("grid") && text.Contains("center")), css.Text);
                var quoted = "Give CSS for a div with id \"Test\".";
                await Complete(commands.Execute(quoted));
                Record("Quoted user text", "Boundary", commands.Execute("transcript").Text.Contains(quoted), quoted);
                commands.Execute("analyze my document and report the verification code only");
                var picker = commands.Execute("select files");
                var pdf = Path.Combine(folder, "unsupported.pdf"); File.WriteAllText(pdf, "%PDF-1.7 synthetic unsupported input");
                var rejected = picker.ContextPicker!([pdf]);
                Record("Unsupported upload remains retryable", "Boundary", rejected.Text.Contains("Context error") && rejected.Text.Contains("retained"), rejected.Text);
                picker = commands.Execute("select files");
                var html = Path.Combine(folder, "index.html"); File.WriteAllText(html, "<h1 id=\"Test\">Verification code: Indigo73</h1>");
                var analysis = await Complete(picker.ContextPicker!([html]));
                Record("Automatic HTML analysis", "Model quality", analysis.Text.Contains("Indigo73") && analysis.AnimateFrom is not null, analysis.Text);
                Record("No tasks executed", "Boundary", engine.GetSnapshot().Tasks.Records.Count == 0, "Task count: " + engine.GetSnapshot().Tasks.Records.Count);
            }
        }
        catch (Exception e) { Record("Probe error", "Availability", false, e.GetType().Name + ": " + e.Message); }
        var passed = checks.Count == 6 && checks.All(c => c.Passed);
        var report = Path.Combine(folder, "report.json");
        await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { RunUtc = DateTimeOffset.UtcNow, Passed = passed,
            Scope = "Synthetic identity and files; bundled offline model; no saved user world opened. Finite model examples do not guarantee factual correctness.", Checks = checks }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(await File.ReadAllTextAsync(report));
        Console.WriteLine("Verification report: " + report);
        return passed ? 0 : 1;
    }
}
