using System.Net;
using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class LocalInferenceTests
{
    private sealed class Backend(Func<string,CancellationToken,Task<string>> action) : IInferenceBackend
    { public Task<string> GenerateAsync(string prompt,CancellationToken token)=>action(prompt,token); }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> action) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>action(request,token); }
    private static HttpResponseMessage Json(string text)=>new(HttpStatusCode.OK){Content=new StringContent(text)};
    [Fact]
    public async Task ConcurrentRequestsAreFifoAndOnlyOneBackendCallIsActive()
    {
        var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var seen=new List<string>();var active=0;var maximum=0;
        await using var service=new SharedLocalInference(new Backend(async(p,t)=> {maximum=Math.Max(maximum,++active);seen.Add(p);if(p=="first"){entered.SetResult();await release.Task.WaitAsync(t);}active--;return p;}));
        var first=service.GenerateAsync("first");await entered.Task;var second=service.GenerateAsync("second");var third=service.GenerateAsync("third");
        Assert.Equal(2,service.Pending);release.SetResult();var replies=await Task.WhenAll(first,second,third);
        Assert.Equal(new[]{"first","second","third"},seen);Assert.Equal(1,maximum);Assert.All(replies,r=>Assert.True(r.Available));
    }
    [Fact]
    public async Task FullQueueRejectsWithoutUnboundedWaiting()
    {
        var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var service=new SharedLocalInference(new Backend(async(p,t)=>{entered.TrySetResult();await release.Task.WaitAsync(t);return p;}),capacity:1);
        var first=service.GenerateAsync("first");await entered.Task;var second=service.GenerateAsync("second");
        Assert.Contains("full",(await service.GenerateAsync("third")).Reason);release.SetResult();await Task.WhenAll(first,second);
    }
    [Fact]
    public async Task CancelledQueuedRequestNeverReachesModel()
    {
        var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var calls=0;
        await using var service=new SharedLocalInference(new Backend(async(p,t)=>{calls++;entered.TrySetResult();await release.Task.WaitAsync(t);return p;}));
        var first=service.GenerateAsync("first");await entered.Task;using var cancel=new CancellationTokenSource();var second=service.GenerateAsync("second",cancel.Token);cancel.Cancel();
        Assert.False((await second).Available);release.SetResult();await first;await service.GenerateAsync("third");Assert.Equal(2,calls);
    }
    [Fact]
    public async Task TimeoutAndRuntimeFailureAreExplicitFallbacks()
    {
        await using(var service=new SharedLocalInference(new Backend(async(_,t)=>{await Task.Delay(Timeout.Infinite,t);return "never";}),deadline:TimeSpan.FromMilliseconds(30)))
            Assert.False((await service.GenerateAsync("hello")).Available);
        await using(var service=new SharedLocalInference(new Backend((_,_)=>throw new HttpRequestException("offline"))))
            Assert.Contains("deterministic",(await service.GenerateAsync("hello")).Reason);
    }
    [Fact]
    public async Task DisabledServiceNeverCallsBackendAndValidatesInputs()
    {
        await using var service=new SharedLocalInference(new Backend((_,_)=>throw new Exception("must not call")),enabled:false);
        Assert.False((await service.GenerateAsync("hello")).Available);
        await Assert.ThrowsAsync<ArgumentException>(()=>service.GenerateAsync(new string('x',8193)));
    }
    [Fact]
    public async Task ShutdownSettlesRunningAndQueuedRequests()
    {
        var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service=new SharedLocalInference(new Backend(async(_,t)=>{entered.TrySetResult();await Task.Delay(Timeout.Infinite,t);return "never";}));
        var first=service.GenerateAsync("first");await entered.Task;var second=service.GenerateAsync("second");
        await service.DisposeAsync();Assert.False((await first).Available);Assert.False((await second).Available);
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyResponsesAreNeverReportedAsSuccess(string text)
    {await using var service=new SharedLocalInference(new Backend((_,_)=>Task.FromResult(text)));Assert.False((await service.GenerateAsync("hello")).Available);}
    [Fact]
    public async Task AdapterUsesFixedLocalEndpointBoundedOptionsAndNoThinkingOutput()
    {
        var calls=0;
        using var backend=new OllamaLocalBackend(new Handler(async(r,t)=>{
            Assert.Equal("127.0.0.1",r.RequestUri!.Host);Assert.Equal(11435,r.RequestUri.Port);calls++;
            if(r.RequestUri.AbsolutePath=="/api/show")return Json("{}");
            using var body=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(t));
            Assert.False(body.RootElement.GetProperty("stream").GetBoolean());Assert.False(body.RootElement.GetProperty("think").GetBoolean());
            Assert.Equal(4096,body.RootElement.GetProperty("options").GetProperty("num_ctx").GetInt32());
            return Json("{\"done\":true,\"response\":\"embedded reasoning</think>final only\",\"thinking\":\"private reasoning\"}");}));
        Assert.Equal("final only",await backend.GenerateAsync("test",default));Assert.Equal(2,calls);
    }
    [Fact]
    public async Task CloudBackedModelIsRejectedBeforeGeneration()
    {
        var calls=0;using var backend=new OllamaLocalBackend(new Handler((_,_)=>{calls++;return Task.FromResult(Json("{\"remote_model\":\"cloud-model\"}"));}));
        await Assert.ThrowsAsync<InvalidDataException>(()=>backend.GenerateAsync("hello",default));Assert.Equal(1,calls);
    }
    [Theory]
    [InlineData("{\"done\":false,\"response\":\"partial\"}")]
    [InlineData("{\"done\":true,\"response\":\"partial\",\"done_reason\":\"length\"}")]
    [InlineData("{\"done\":true}")]
    public async Task IncompleteResponsesAreRefused(string json)
    {
        using var backend=new OllamaLocalBackend(new Handler((r,_)=>Task.FromResult(Json(r.RequestUri!.AbsolutePath=="/api/show"?"{}":json))));
        await Assert.ThrowsAsync<InvalidDataException>(()=>backend.GenerateAsync("test",default));
    }
    [Fact]
    public async Task ExcessiveHttpResponseIsBounded()
    {
        using var backend=new OllamaLocalBackend(new Handler((_,_)=>Task.FromResult(Json(new string('x',1024*1024+1)))));
        await Assert.ThrowsAsync<InvalidDataException>(()=>backend.GenerateAsync("test",default));
    }
    [Fact]
    public async Task ProviderExceptionDoesNotKillQueueWorker()
    {
        var calls=0;await using var service=new SharedLocalInference(new Backend((_,_)=>++calls==1?throw new InvalidDataException("bad output"):Task.FromResult("recovered")));
        Assert.False((await service.GenerateAsync("first")).Available);Assert.Equal("recovered",(await service.GenerateAsync("second")).Text);
    }
    [Fact]
    public async Task MalformedStatusReturnsUnavailableInsteadOfCrashingTerminal()
    {
        using var backend=new OllamaLocalBackend(new Handler((_,_)=>Task.FromResult(Json(new string('x',1024*1024+1)))));
        Assert.Contains("unavailable",await backend.StatusAsync(default));
    }
    [Theory]
    [InlineData("1.5")]
    [InlineData("9223372036854775808")]
    [InlineData("\"unknown\"")]
    public async Task InvalidAllocationReturnsUnavailableAndNextStatusRecovers(string allocation)
    {
        var malformed = true;
        using var backend = new OllamaLocalBackend(new Handler((request, _) =>
        {
            var body = request.RequestUri!.AbsolutePath switch
            {
                "/api/version" => "{\"version\":\"test\"}",
                "/api/tags" => "{\"models\":[{\"name\":\"qwen3:4b\"}]}",
                _ => "{\"models\":[{\"name\":\"qwen3:4b\",\"size\":" + (malformed ? allocation : "1024") + ",\"size_vram\":512}]}"
            };
            return Task.FromResult(Json(body));
        }));
        Assert.Contains("unavailable", await backend.StatusAsync(default));
        malformed = false;
        Assert.Contains("Installed: True", await backend.StatusAsync(default));
    }
    [Fact]
    public async Task DiagnosticRequiresMasterAndDoesNotCreateTasksOrConversations()
    {
        var engine=new GridSimulationEngine(GridWorldFactory.CreateGenesis());var identity=new TerminalIdentitySession(()=>"ai-owner");identity.Attach(engine);
        using var backend=new OllamaLocalBackend(new Handler((_,_)=>Task.FromResult(Json("{}"))));
        await using var service=new SharedLocalInference(new Backend((_,_)=>Task.FromResult("GRID LOCAL READY")));
        var commands=DefaultCommands.Create(engine);LocalAiCommands.Register(commands,identity,service,backend);commands.UseIdentity(identity);
        Assert.Contains("denied",commands.Execute("ai test").Text);identity.Login();
        var operation=commands.Execute("ai test").BrokerOperation!;Assert.Contains("GRID LOCAL READY",(await operation(default)).Text);
        Assert.Empty(engine.GetSnapshot().Tasks.Records);Assert.Empty(engine.GetSnapshot().Communications.Conversations);
        identity.Logout();Assert.Contains("revoked",(await operation(default)).Text);
    }
}
