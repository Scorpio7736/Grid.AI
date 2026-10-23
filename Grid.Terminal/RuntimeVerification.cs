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
        Console.WriteLine(success ? "Bundled runtime, model, grounded dialogue and animation metadata verified. No saved world opened." : "Conversation verification failed; inspect bundled runtime diagnostics.");
        return success ? 0 : 1;
    }
}
