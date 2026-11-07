using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class AuthorizationRouteTests
{
    private sealed class Model : ILocalInference, IInferenceBackend
    {
        public int Calls;
        public Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default)
        {
            Calls++;
            return Task.FromResult(new InferenceReply(true, "Ready.", "test"));
        }
        Task<string> IInferenceBackend.GenerateAsync(string prompt, CancellationToken token)
        {
            Calls++;
            return Task.FromResult("GRID LOCAL READY");
        }
    }

    [Theory]
    [InlineData("meeting")]
    [InlineData("screen")]
    [InlineData("file")]
    [InlineData("diagnostic")]
    public async Task ChatQuotaIsSharedByEveryInferenceRoute(string route)
    {
        var folder = Path.Combine(Path.GetTempPath(), "GridAuthorization-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var source = Path.Combine(folder, "source");
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "demo.csproj"), "<Project />");
            var identity = new TerminalIdentitySession(() => "authorization-owner");
            var sessions = new TerminalSessionRegistry();
            sessions.Add(identity);
            var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: sessions);
            identity.Attach(engine);
            identity.Login();
            engine.StartAgents(identity.Reference);
            engine.ConfigureMaxisRuntime(Path.Combine(folder, "artifacts"));
            engine.RegisterDevelopmentWorkspace(identity.Reference, "demo", source, "demo.csproj");
            var model = new Model();
            var screens = new ScreenTests.Provider();
            await using var inference = new SharedLocalInference(model);
            using var backend = new OllamaLocalBackend();
            var commands = DefaultCommands.Create(engine);
            ChatCommands.Register(commands, engine, identity, model, screenProvider: screens);
            WorkspaceChangeCommands.Register(commands, engine, identity, model);
            LocalAiCommands.Register(commands, identity, inference, backend);
            commands.UseIdentity(identity);
            async Task<CommandResult> Run(string command)
            {
                var result = commands.Execute(command);
                return result.BrokerOperation is { } operation ? await operation(default) : result;
            }
            await Run("connect MCP -inline");
            for (var i = 0; i < 60; i++) await Run("talk hello");
            Assert.Equal(60, model.Calls);
            string request;
            switch (route)
            {
                case "meeting":
                    await Run("agent meetings MCP MAXIS -inline");
                    request = "talk each of you state a random fact";
                    break;
                case "screen":
                    await Run("screen options");
                    await Run("screen monitoring 1");
                    request = "screen ask explain the captured text";
                    break;
                case "file":
                    request = "changes draft-create demo output.md \"write a greeting\"";
                    break;
                default:
                    request = "ai test";
                    break;
            }
            var refused = await Run(request);
            Assert.Contains("request limit", refused.Text);
            Assert.Equal(60, model.Calls);
            Assert.Equal(0, screens.Captures);
            Assert.False(File.Exists(Path.Combine(source, "output.md")));
            var audit = await Run("ai authorization");
            Assert.Contains("quota", audit.Text);
            Assert.DoesNotContain("write a greeting", audit.Text);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    private sealed class BlockingBackend : IInferenceBackend
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<string> GenerateAsync(string prompt, CancellationToken token)
        {
            Entered.TrySetResult();
            await Task.Delay(Timeout.Infinite, token);
            return "SECRET RESULT";
        }
    }

    [Fact]
    public async Task LogoutCancelsRunningDiagnosticAndDiscardsItsOutput()
    {
        var identity = new TerminalIdentitySession(() => "diagnostic-owner");
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        identity.Attach(engine);
        identity.Login();
        var model = new BlockingBackend();
        await using var inference = new SharedLocalInference(model);
        using var backend = new OllamaLocalBackend();
        var commands = DefaultCommands.Create(engine);
        LocalAiCommands.Register(commands, identity, inference, backend);
        commands.UseIdentity(identity);
        var operation = commands.Execute("ai test").BrokerOperation!(default);
        await model.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        identity.Logout();
        var result = await operation.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.DoesNotContain("SECRET RESULT", result.Text);
        Assert.True(result.Text.Contains("revoked") || result.Text.Contains("cancelled"));
    }

    [Fact]
    public async Task DiagnosticOperationCannotBeReplayedAfterCompletionOrLogout()
    {
        var identity = new TerminalIdentitySession(() => "diagnostic-owner");
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        identity.Attach(engine);
        identity.Login();
        var model = new Model();
        await using var inference = new SharedLocalInference(model);
        using var backend = new OllamaLocalBackend();
        var commands = DefaultCommands.Create(engine);
        LocalAiCommands.Register(commands, identity, inference, backend);
        commands.UseIdentity(identity);
        var operation = commands.Execute("ai test").BrokerOperation!;
        await operation(default);
        Assert.Contains("already consumed", (await operation(default)).Text);
        Assert.Equal(1, model.Calls);
        var pending = commands.Execute("ai test").BrokerOperation!;
        identity.Logout();
        Assert.Contains("revoked", (await pending(default)).Text);
        Assert.Equal(1, model.Calls);
    }
}
