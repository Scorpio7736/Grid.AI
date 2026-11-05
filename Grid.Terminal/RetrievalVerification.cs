using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Simulation;

namespace Grid.Terminal;

public static class RetrievalVerification
{
    public static async Task<int> RunAsync()
    {
        var sessions = new TerminalSessionRegistry(); var owner = new TerminalIdentitySession(() => "retrieval-verification"); sessions.Add(owner);
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: sessions);
        owner.Attach(engine); owner.Login(); engine.StartAgents(owner.Reference);
        var marker = "COBALT42";
        var text = string.Concat(Enumerable.Repeat("General background for the verification document.\n", 120)) + "The release verification code is " + marker + ".";
        var doc = new ContextDocument(Guid.NewGuid(), "general", "synthetic-deep-document.txt", Grid.AI.RetrievalText.Hash(text), text.Length,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "utf8", text, engine.ResolveProgram("MCP").Id);
        engine.AddContext(owner.Reference, doc, false);
        var hits = ContextRetrieval.Search(engine.ContextFor(doc.GrantedAgentId, owner.SessionId), "general", "release verification code");
        var deep = hits.Any(h => h.Chunk.Start > 700 && h.Chunk.Text.Contains(marker));
        var root = BundledAiRuntime.FindRoot(AppContext.BaseDirectory);
        await using var runtime = await BundledAiRuntime.StartAsync(root);
        if (!runtime.Ready) throw new IOException(runtime.Status);
        using var backend = new OllamaLocalBackend(endpoint: runtime.Endpoint); await using var inference = new SharedLocalInference(backend);
        var commands = DefaultCommands.Create(engine); ChatCommands.Register(commands, engine, owner, inference); commands.UseIdentity(owner);
        commands.Execute("connect MCP -inline");
        var request = commands.Execute("assist analyze What is the release verification code?");
        var reply = request.BrokerOperation is { } operation ? await operation(default) : request;
        var answered = reply.Text.Contains(marker, StringComparison.OrdinalIgnoreCase);
        engine.ChangeContext(owner.Reference, doc.Id, false);
        var revoked = ContextRetrieval.Search(engine.ContextFor(doc.GrantedAgentId, owner.SessionId), "general", "release verification code").Count == 0;
        var passed = deep && answered && revoked;
        var file = Path.Combine(Path.GetTempPath(), "GRID-620-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(file, JsonSerializer.Serialize(new { Passed = passed, DeepRetrieval = deep, AnswerContainedExpectedMarker = answered,
            Revocation = revoked, Citations = hits.Select(ContextRetrieval.Cite).ToArray(), Reply = reply.Text,
            Limits = "Synthetic fixtures, disposable world, lexical feature vectors. No claim of semantic embedding quality or original-file freshness." }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Deep retrieval: {deep}; answer: {answered}; revocation: {revoked}\n{reply.Text}\nRetrieval report: {file}");
        return passed ? 0 : 1;
    }
}
