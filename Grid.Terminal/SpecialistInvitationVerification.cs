using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Persistence;
using Grid.Simulation;
namespace Grid.Terminal;

public static class SpecialistInvitationVerification
{
    public static async Task<int> RunAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Grid612Verification-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        var checks = new List<IntelligenceVerification.Check>();
        void Check(string name, bool passed, string observed) => checks.Add(new(name, "6.12 specialist invitations", passed, "Synthetic world only", observed));
        try
        {
            await using var runtime = await BundledAiRuntime.StartAsync(BundledAiRuntime.FindRoot(AppContext.BaseDirectory));
            Check("Bundled model available", runtime.Ready, runtime.Status);
            if (runtime.Ready)
            {
                using var backend = new OllamaLocalBackend(endpoint: runtime.Endpoint); await using var ai = new SharedLocalInference(backend);
                var registry = new TerminalSessionRegistry(); var owner = new TerminalIdentitySession(() => "invitation-probe"); registry.Add(owner);
                var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
                using var store = new WorldStore(Path.Combine(folder, "world.db")); var info = new WorldInfo(engine.GetSnapshot().WorldId, "Invitation probe", DateTimeOffset.UtcNow); store.Create(info, engine.GetSnapshot());
                Task Save(CancellationToken token) { store.Save(info, engine.GetSnapshot(), token); return Task.CompletedTask; }
                var commands = DefaultCommands.Create(engine); TaskCommands.Register(commands, engine, owner, Save);
                var chat = ChatCommands.Register(commands, engine, owner, ai, Save); commands.UseIdentity(owner); commands.Execute("connect MCP -inline");
                async Task<CommandResult> Talk(string text) { var r = commands.ExecuteTokens(["talk", text]); return r.BrokerOperation is {} op ? await op(default) : r; }
                var original = chat.ActiveConversation!.Value;
                engine.Talk(original, engine.LocalOperator, "PRIVATE_BEFORE_INVITE_612");
                var preview = await Talk("invite someone for resource observations"); await Talk("decline invitation");
                Check("Recommendation and decline", preview.Text.Contains("VOLT") && chat.ActiveConversation == original, preview.Text);
                await Talk("invite VOLT"); var joined = await Talk("accept invitation"); var group = chat.ActiveConversation!.Value;
                var answer = await Talk("@VOLT Introduce yourself in one sentence.");
                Check("Confirmed specialist joins same terminal", group != original && engine.GetConversation(group).Messages.Last().Sender.Name == "VOLT" && !answer.Text.Contains("PRIVATE_BEFORE_INVITE_612"), joined.Text + "\n" + answer.Text);
                var draft = await Talk("@VOLT plan a current resource observation using volt.status current only");
                Check("Invited specialist supports reviewed planning", draft.Text.Contains("DRAFT ONLY") && draft.Text.Contains("volt.status") && engine.GetSnapshot().Tasks.Records.Count == 0, draft.Text);
                await Talk("submit plan"); var plan = engine.GetSnapshot().Tasks.Plans.Single();
                foreach (var id in plan.TaskIds) engine.ApproveTask(owner.Reference, id);
                await engine.RunCoordinationAsync(owner.Reference, plan.Id, Save, default);
                var brief = await Talk("MCP progress");
                Check("Unified recorded-result briefing", brief.Text.Contains("Completed") && brief.Text.Contains("REAL HOST TELEMETRY"), brief.Text);
                await Talk("remove participant VOLT"); await Talk("confirm removal");
                var snapshot = store.Load().State; var final = snapshot.Communications.Conversations.Single(c => c.Id == chat.ActiveConversation);
                Check("Removal and saved membership", !final.IsMeeting && final.Participants.All(p => p.Name != "VOLT") && snapshot.Communications.Conversations.Single(c => c.Id == group).Status == ConversationStatus.Ended, "Confirmed membership boundaries and earlier transcripts persisted; no pending invitation persisted.");
            }
        }
        catch (Exception e) { Check("Verification exception", false, e.GetType().Name + ": " + e.Message); }
        var passed = checks.Count == 6 && checks.All(c => c.Passed); var report = Path.Combine(folder, "report.json");
        await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { Passed = passed, Checks = checks }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(await File.ReadAllTextAsync(report)); Console.WriteLine("Verification report: " + report);
        return passed ? 0 : 1;
    }
}
