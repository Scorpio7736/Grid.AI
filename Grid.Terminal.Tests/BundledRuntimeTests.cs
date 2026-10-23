using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class BundledRuntimeTests
{
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
