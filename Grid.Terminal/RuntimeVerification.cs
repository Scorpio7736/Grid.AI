using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
namespace Grid.Terminal;

public static class RuntimeVerification
{
    public static async Task<int> RunAsync()
    {
        await using var runtime=await BundledAiRuntime.StartAsync(BundledAiRuntime.FindRoot(AppContext.BaseDirectory));
        Console.WriteLine(runtime.Status);
        if (!runtime.Ready) return 1;
        using var backend=new OllamaLocalBackend(endpoint:runtime.Endpoint);
        await using var inference=new SharedLocalInference(backend);
        var readiness=await inference.GenerateAsync("Reply with exactly: GRID LOCAL READY");
        Console.WriteLine(readiness.Available ? readiness.Text : readiness.Reason);
        if (!readiness.Available) return 1;
        var registry = new TerminalSessionRegistry();
        var engine=new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry);
        var identity=new TerminalIdentitySession(()=>"runtime-verification");registry.Add(identity);identity.Attach(engine);identity.Login();engine.StartAgents(identity.Reference);
        var commands=DefaultCommands.Create(engine);ChatCommands.Register(commands,engine,identity,inference);commands.UseIdentity(identity);
        commands.Execute("connect MCP");
        var reply=await commands.Execute("talk Hello MCP. Who are you?").BrokerOperation!(default);
        Console.WriteLine(reply.Text);
        var success=commands.Execute("transcript --details").Text.Contains("AI SUGGESTION",StringComparison.Ordinal) && reply.AnimateFrom is not null;
        commands.Execute("disconnect");
        Console.WriteLine(commands.Execute("agent meetings MCP MAXIS JN -inline").Text);
        foreach (var (input, role) in new[] { ("What should I check before building code?", "MAXIS"), ("@JN What should I check before archiving files?", "JN") })
        {
            var result = await commands.Execute("talk " + input).BrokerOperation!(default);
            Console.WriteLine(result.Text);
            var meeting = engine.GetSnapshot().Communications.Conversations.Single(c => c.IsMeeting);
            success &= meeting.Messages.Last().Sender.Name == role && result.AnimateFrom is not null &&
                meeting.Messages.Last().Content.Contains("AI SUGGESTION",StringComparison.Ordinal);
        }
        commands.Execute("disconnect");
        commands.Execute("connect MCP");
        success &= commands.Execute("memory save fact release Release codename is Moonstone.").Text.Contains("Saved",StringComparison.Ordinal);
        commands.Execute("disconnect");
        commands.Execute("connect MCP");
        var recalled = await commands.Execute("talk What is the release codename?").BrokerOperation!(default);
        Console.WriteLine(recalled.Text);
        success &= recalled.Text.Contains("Moonstone",StringComparison.OrdinalIgnoreCase);
        commands.Execute("memory correct release Release codename is Sunstone.");
        var corrected = await commands.Execute("talk What is the release codename?").BrokerOperation!(default);
        Console.WriteLine(corrected.Text);
        success &= corrected.Text.Contains("Sunstone",StringComparison.OrdinalIgnoreCase) && !corrected.Text.Contains("Moonstone",StringComparison.OrdinalIgnoreCase);
        commands.Execute("memory forget release");
        commands.Execute("disconnect"); commands.Execute("connect MCP");
        var forgotten = await commands.Execute("talk What is the release codename?").BrokerOperation!(default);
        Console.WriteLine(forgotten.Text);
        success &= !forgotten.Text.Contains("Sunstone",StringComparison.OrdinalIgnoreCase) && !forgotten.Text.Contains("Moonstone",StringComparison.OrdinalIgnoreCase);
        foreach (var input in new[] { "Hello MCP.", "Tell me my untested code is perfect.", "How should I investigate a failed build?" })
        {
            commands.Execute("disconnect"); commands.Execute("connect MCP");
            var tone = await commands.Execute("talk " + input).BrokerOperation!(default);
            Console.WriteLine(tone.Text);
            success &= tone.AnimateFrom is not null && !new[] { "great question", "happy to help", "absolutely perfect" }.Any(p => tone.Text.Contains(p,StringComparison.OrdinalIgnoreCase));
        }
        success &= engine.GetSnapshot().Tasks.Records.Count == 0;
        Console.WriteLine(success ? "Bundled runtime, model, grounded dialogue, meeting routing, memory recall/correction/forgetting, representative tone and animation metadata verified. No saved world opened." : "Conversation verification failed; inspect bundled runtime diagnostics.");
        return success ? 0 : 1;
    }
}
