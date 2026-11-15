using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class BundledRuntimeTests
{
    [Fact]
    public async Task MissingRuntimeWarnsBeforeFirstMessageAndOnResume()
    {
        await using var runtime = await BundledAiRuntime.StartAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var warning = AiReadinessNotice.Format(runtime.Ready, runtime.Status);
        Assert.StartsWith("WARNING: AI unavailable", warning);
        Assert.Contains("Setup-GridClone.ps1 -Download", warning);
        Assert.Contains("only basic fallback replies", warning);
        Assert.Contains("ai status", warning);
        Assert.Contains("\nMissing:", warning);
        Assert.Equal("", AiReadinessNotice.Format(true, "ready"));
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()).PrepareChatFixture();
        var identity = new TerminalIdentitySession(() => "test"); identity.Attach(engine); identity.Login();
        var commands = DefaultCommands.Create(engine);
        var chat = ChatCommands.Register(commands, engine, identity, aiWarning: () => warning); commands.UseIdentity(identity);
        Assert.StartsWith(warning, commands.Execute("connect MCP -inline").Text);
        var id = chat.ActiveConversation!.Value;
        var resumed = DefaultCommands.Create(engine);
        ChatCommands.Register(resumed, engine, identity, aiWarning: () => warning); resumed.UseIdentity(identity);
        Assert.StartsWith(warning, resumed.Execute("resume " + id + " -inline").Text);
    }

    [Fact]
    public async Task AttachedConversationWarnsInParentAndInitialWindowReply()
    {
        const string warning = "WARNING: AI unavailable — setup or repair required.\n";
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis()).PrepareChatFixture();
        var registry = new TerminalSessionRegistry();
        var owner = new TerminalIdentitySession(() => "test"); registry.Add(owner); owner.Attach(engine); owner.Login();
        CommandDispatcher Create(TerminalIdentitySession session)
        {
            var commands = DefaultCommands.Create(engine);
            ChatCommands.Register(commands, engine, session, aiWarning: () => warning); commands.UseIdentity(session); return commands;
        }
        var parent = Create(owner); string endpoint = "";
        await using var hub = new WindowHub(engine, registry, Create, name => { endpoint = name; return Environment.ProcessId; }, aiWarning: () => warning);
        hub.Register(parent, owner);
        Assert.StartsWith(warning, parent.Execute("connect MCP").Text);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var pipe = new System.IO.Pipes.NamedPipeClientStream(".", endpoint, System.IO.Pipes.PipeDirection.InOut, System.IO.Pipes.PipeOptions.Asynchronous | System.IO.Pipes.PipeOptions.CurrentUserOnly);
        await pipe.ConnectAsync(deadline.Token);
        var reply = await WindowProtocol.ReadAsync<WindowReply>(pipe, deadline.Token);
        Assert.Contains(warning, reply!.Text);
    }

    [Fact]
    public async Task CorruptPinnedManifestPreservesValidationFailureWithoutStartingServer()
    {
        var root = Path.Combine(Path.GetTempPath(), "GridBadRuntime-" + Guid.NewGuid());
        var manifest = Path.Combine(root, "models", "manifests", "registry.ollama.ai", "library", "qwen3", "4b");
        Directory.CreateDirectory(Path.GetDirectoryName(manifest)!);
        Directory.CreateDirectory(Path.Combine(root, "ollama"));
        try
        {
            File.WriteAllText(Path.Combine(root, "assets.json"), "{}");
            File.WriteAllText(Path.Combine(root, "ollama", "ollama.exe"), "not executable");
            File.WriteAllText(manifest, "{}");
            await using var runtime = await BundledAiRuntime.StartAsync(root);
            Assert.False(runtime.Ready);
            Assert.Contains("Unbenchmarked model manifest", runtime.Status);
            Assert.Equal(1, runtime.Endpoint.Port);
            Assert.Null(runtime.LogPath);
        }
        finally { Directory.Delete(root, true); }
    }
    private sealed class NoNetwork : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; throw new InvalidOperationException("Disabled runtime must not probe HTTP."); }
    }
    [Theory]
    [InlineData("status")]
    [InlineData("test")]
    public async Task MissingAssetsReportStartupCauseWithoutProbingSentinelEndpoint(string diagnostic)
    {
        using var handler = new NoNetwork();
        using var backend = new OllamaLocalBackend(handler, new Uri("http://127.0.0.1:1/"));
        const string reason = "Local AI assets missing. Missing: assets.json";
        await using var service = new SharedLocalInference(backend, enabled: false, unavailableReason: reason);
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis());
        var identity = new TerminalIdentitySession(() => "test"); identity.Attach(engine); identity.Login();
        var commands = DefaultCommands.Create(engine); LocalAiCommands.Register(commands, identity, service, backend, () => reason); commands.UseIdentity(identity);
        var result = await commands.Execute("ai " + diagnostic).BrokerOperation!(default);
        Assert.Contains(reason, result.Text); Assert.Contains("no HTTP probe", result.Text);
        Assert.DoesNotContain("ConnectionRefused", result.Text); Assert.Equal(0, handler.Calls);
    }
    [Fact]
    public async Task DisabledChatAndDiagnosticsRetainStartupFailure()
    {
        using var backend=new OllamaLocalBackend();
        const string reason="Bundled local AI unavailable: missing asset. Runtime log: test-log";
        await using var service=new SharedLocalInference(backend,enabled:false,unavailableReason:reason);
        Assert.Equal(reason,(await service.GenerateAsync("hello")).Reason);
        var engine=new GridSimulationEngine(GridWorldFactory.CreateGenesis());var identity=new TerminalIdentitySession(()=>"test");identity.Attach(engine);identity.Login();
        var commands=DefaultCommands.Create(engine);LocalAiCommands.Register(commands,identity,service,backend,()=>reason);commands.UseIdentity(identity);
        var result=await commands.Execute("ai test").BrokerOperation!(default);
        Assert.Contains(reason,result.Text);
    }
    [Theory]
    [InlineData("https://127.0.0.1:1234/")]
    [InlineData("http://example.com/")]
    [InlineData("http://127.0.0.1:1234/other")]
    public void BackendRejectsNonlocalOrNoncanonicalEndpoints(string endpoint)
    { Assert.Throws<ArgumentException>(()=>new OllamaLocalBackend(endpoint:new Uri(endpoint))); }
    [Fact]
    public async Task MissingAssetsDoNotUseGlobalOllama()
    {
        await using var runtime=await BundledAiRuntime.StartAsync(Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString()));
        Assert.False(runtime.Ready);Assert.Equal(1,runtime.Endpoint.Port);
    }
    [Fact]
    public void RuntimePathsResolveRelativeToMovedPackageOrSource()
    {
        var root=Path.Combine(Path.GetTempPath(),"GridPaths-"+Guid.NewGuid());Directory.CreateDirectory(root);
        try
        {
            var app=Path.Combine(root,"app");Directory.CreateDirectory(app);File.WriteAllText(Path.Combine(root,GridTransfer.Marker),"{}");
            Assert.Equal(Path.Combine(root,"runtime"),BundledAiRuntime.FindRoot(app));
            File.Delete(Path.Combine(root,GridTransfer.Marker));File.WriteAllText(Path.Combine(root,"Grid7736.slnx"),"");
            Assert.Equal(Path.Combine(root,".grid-runtime"),BundledAiRuntime.FindRoot(app));
        }
        finally { Directory.Delete(root,true); }
    }
    private sealed class Unavailable : ILocalInference
    { public Task<InferenceReply> GenerateAsync(string prompt,CancellationToken token=default)=>Task.FromResult(new InferenceReply(false,"","offline")); }
    [Fact]
    public async Task FallbackRetainsAnimationAndInlinePreferenceCanBeChanged()
    {
        var engine=new GridSimulationEngine(GridWorldFactory.CreateGenesis());var identity=new TerminalIdentitySession(()=>"test");identity.Attach(engine);identity.Login();
        var commands=DefaultCommands.Create(engine);ChatCommands.Register(commands,engine.PrepareChatFixture(),identity,new Unavailable());commands.UseIdentity(identity);
        commands.Execute("connect MCP");commands.Execute("typewriter off");Assert.False(commands.Typewriter);
        commands.Execute("typewriter on");Assert.True(commands.Typewriter);
        var reply=await commands.Execute("talk hello").BrokerOperation!(default);
        Assert.NotNull(reply.AnimateFrom);Assert.StartsWith("MCP:",reply.Text[reply.AnimateFrom!.Value..]);
    }
}
