using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
namespace Grid.Terminal;

public static class RuntimeVerification
{
    public static async Task<int> RunAsync(bool assistanceOnly = false)
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
        var success=true;
        if(!assistanceOnly)
        {
        var reply=await commands.Execute("talk Hello MCP. Who are you?").BrokerOperation!(default);
        Console.WriteLine(reply.Text);
        success=commands.Execute("transcript --details").Text.Contains("AI SUGGESTION",StringComparison.Ordinal) && reply.AnimateFrom is not null;
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
        commands.Execute("disconnect"); commands.Execute("connect MCP");
        await commands.Execute("talk Our project mascot is Copperfin.").BrokerOperation!(default);
        commands.Execute("disconnect"); commands.Execute("connect MCP");
        var automatic = await commands.Execute("talk What is our project mascot?").BrokerOperation!(default);
        Console.WriteLine(automatic.Text);
        success &= automatic.Text.Contains("Copperfin",StringComparison.OrdinalIgnoreCase);
        commands.Execute("memory forget-history");
        commands.Execute("disconnect"); commands.Execute("connect MCP");
        var cleared = await commands.Execute("talk What is our project mascot?").BrokerOperation!(default);
        Console.WriteLine(cleared.Text);
        success &= !cleared.Text.Contains("Copperfin",StringComparison.OrdinalIgnoreCase);
        }
        var greeting=await commands.Execute("talk hello?").BrokerOperation!(default);
        Console.WriteLine(greeting.Text);
        success &= !System.Text.RegularExpressions.Regex.IsMatch(greeting.Text,@"(?i)district\s+\d+");
        var html=await commands.Execute("assist advice how i can center a div in html").BrokerOperation!(default);
        Console.WriteLine(html.Text);
        success &= html.Text.StartsWith("AI advice",StringComparison.Ordinal) &&
            (html.Text.Contains("flex",StringComparison.OrdinalIgnoreCase) || html.Text.Contains("grid",StringComparison.OrdinalIgnoreCase) || html.Text.Contains("margin",StringComparison.OrdinalIgnoreCase));
        var doc=new ContextDocument(Guid.NewGuid(),"general","captured-release.txt",new string('A',64),48,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,"utf8","The release verification code is Cobalt42.",engine.ResolveProgram("MCP").Id);
        engine.AddContext(identity.Reference,doc,true);
        var analysis=await commands.Execute("assist analyze What is the release verification code in the captured file?").BrokerOperation!(default);
        Console.WriteLine(analysis.Text);
        success &= analysis.Text.Contains("Cobalt42",StringComparison.OrdinalIgnoreCase);
        var proposal=await commands.Execute("assist propose Create one VOLT volt.status current step to inspect resource status.").BrokerOperation!(default);
        Console.WriteLine(proposal.Text);
        success &= proposal.Text.Contains("DRAFT ONLY",StringComparison.Ordinal) && proposal.Text.Contains("volt.status",StringComparison.Ordinal);
        success &= engine.GetSnapshot().Tasks.Records.Count == 0;
        Console.WriteLine(success ? (assistanceOnly ? "Bundled runtime, scoped context analysis and typed task drafting verified. No saved world opened." : "Bundled runtime, model, grounded dialogue, meeting routing, memory recall/correction/forgetting, automatic conversation recall, representative tone, scoped context analysis, typed task drafting and animation metadata verified. No saved world opened.") : "Conversation verification failed; inspect bundled runtime diagnostics.");
        return success ? 0 : 1;
    }
}
