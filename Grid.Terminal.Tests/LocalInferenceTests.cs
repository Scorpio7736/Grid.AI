using System.Net;
using System.Text.Json;
using Grid.Communication;
using Grid.Core;
using Grid.Simulation;
using Xunit;
namespace Grid.Terminal.Tests;

[Trait("Phase", "6.5")]
public sealed class LocalInferenceTests
{
    private sealed class Backend(Func<string,CancellationToken,Task<string>> action) : IInferenceBackend
    { public Task<string> GenerateAsync(string prompt,CancellationToken token)=>action(prompt,token); }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> action) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>action(request,token); }
    private static HttpResponseMessage Json(string text)=>new(HttpStatusCode.OK){Content=new StringContent(text)};
    [Fact]
    public async Task FileGenerationUsesExtendedBudgetAndRejectsTruncation()
    {
        var budgets = new List<int>(); var calls = 0;
        using var backend = new OllamaLocalBackend(new Handler(async (request, token) =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/show") return Json("{}");
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.Equal(16384, body.RootElement.GetProperty("options").GetProperty("num_ctx").GetInt32());
            budgets.Add(body.RootElement.GetProperty("options").GetProperty("num_predict").GetInt32());
            calls++;
            return Json("{\"done\":true,\"done_reason\":\"length\",\"response\":\"partial\"}");
        }));
        await using var inference = new SharedLocalInference(backend);
        var result = await inference.GenerateFileAsync("A detailed page");
        Assert.Equal(new[] {4096, 8192}, budgets); Assert.Equal(2, calls);
        Assert.False(result.Available); Assert.Empty(result.Text); Assert.Contains("No partial file", result.Reason);
    }
    [Fact]
    public async Task FileOutputCanExceedChatLimitWithoutChangingChatLimit()
    {
        var output = JsonSerializer.Serialize(new { content = new string('x', 20000) });
        await using var inference = new SharedLocalInference(new Backend((_, _) => Task.FromResult(output)));
        var file = await inference.GenerateFileAsync("Detailed file"); Assert.True(file.Available); Assert.Equal(output, file.Text);
        Assert.False((await inference.GenerateStructuredAsync("Ordinary proposal")).Available);
    }

    [Fact]
    [Trait("Phase", "6.6")]
    public async Task ProposalSchemaReachesBackendAndSurvivesTruncationRetry()
    {
        const string schema = "{\"type\":\"object\",\"properties\":{\"steps\":{\"type\":\"array\"}},\"required\":[\"steps\"]}";
        var calls = 0;
        using var backend = new OllamaLocalBackend(new Handler(async (request, token) =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/show") return Json("{}");
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.Equal(schema, body.RootElement.GetProperty("format").GetRawText());
            Assert.Contains("exact schema", body.RootElement.GetProperty("prompt").GetString());
            return ++calls == 1 ? Json("{\"done\":true,\"done_reason\":\"length\",\"response\":\"partial\"}")
                : Json("{\"done\":true,\"done_reason\":\"stop\",\"response\":\"{\\\"steps\\\":[]}\"}");
        }));
        await using var inference = new SharedLocalInference(backend);
        var result = await inference.GenerateStructuredAsync("Plan one task", schema);
        Assert.True(result.Available); Assert.Equal("{\"steps\":[]}", result.Text); Assert.Equal(2, calls);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task TruncatedAnswersRetryOnceWithLargerBudgetAndPreserveMode(bool structured)
    {
        var limits=new List<int>();var prompts=new List<string>();
        using var backend=new OllamaLocalBackend(new Handler(async(r,t)=>
        {
            if(r.RequestUri!.AbsolutePath=="/api/show") return Json("{}");
            using var body=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(t));
            limits.Add(body.RootElement.GetProperty("options").GetProperty("num_predict").GetInt32());
            prompts.Add(body.RootElement.GetProperty("prompt").GetString()!);
            Assert.Equal(structured ? "json" : "",body.RootElement.GetProperty("format").GetString());
            return limits.Count==1 ? Json("{\"done\":true,\"done_reason\":\"length\",\"response\":\"TRUNCATED_SECRET\"}") : Json("{\"done\":true,\"done_reason\":\"stop\",\"response\":\"complete final answer\"}");
        }));
        await using var service=new SharedLocalInference(backend);
        var reply=structured ? await service.GenerateStructuredAsync("answer") : await service.GenerateAsync("answer");
        Assert.True(reply.Available);Assert.False(reply.ResponseRejected);Assert.Equal("complete final answer",reply.Text);
        Assert.Equal(new[]{512,1536},limits);Assert.Equal(prompts[0],prompts[1]);
    }
    [Fact]
    public async Task RepeatedTruncationIsRejectedWithAccurateReasonAndQueueStillWorks()
    {
        var calls=0;var truncate=true;
        using var backend=new OllamaLocalBackend(new Handler((r,_)=>
        {
            if(r.RequestUri!.AbsolutePath=="/api/show") return Task.FromResult(Json("{}"));
            calls++;return Task.FromResult(Json(truncate ? "{\"done\":true,\"done_reason\":\"length\",\"response\":\"partial\"}" : "{\"done\":true,\"response\":\"ready\"}"));
        }));
        await using var service=new SharedLocalInference(backend);var reply=await service.GenerateAsync("answer");
        Assert.False(reply.Available);Assert.True(reply.ResponseRejected);Assert.Empty(reply.Text);Assert.Contains("response limit",reply.Reason);Assert.DoesNotContain("unavailable",reply.Reason);Assert.Equal(2,calls);
        truncate=false;Assert.Equal("ready",(await service.GenerateAsync("test")).Text);
    }
    [Fact]
    public async Task IncompleteResponseIsNotRetriedOrMisreportedAsOffline()
    {
        var calls=0;
        using var backend=new OllamaLocalBackend(new Handler((r,_)=>
        {
            if(r.RequestUri!.AbsolutePath=="/api/show") return Task.FromResult(Json("{}"));
            calls++;return Task.FromResult(Json("{\"done\":false,\"response\":\"partial\"}"));
        }));
        await using var service=new SharedLocalInference(backend);var reply=await service.GenerateAsync("answer");
        Assert.True(reply.ResponseRejected);Assert.Contains("incomplete",reply.Reason);Assert.Equal(1,calls);Assert.Empty(reply.Text);
    }
    [Fact]
    public async Task CancellationBetweenAttemptsPreventsRetry()
    {
        var calls=0;using var cancel=new CancellationTokenSource();
        using var backend=new OllamaLocalBackend(new Handler((r,_)=>
        {
            if(r.RequestUri!.AbsolutePath=="/api/show") return Task.FromResult(Json("{}"));
            calls++;cancel.Cancel();return Task.FromResult(Json("{\"done\":true,\"done_reason\":\"length\",\"response\":\"partial\"}"));
        }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>backend.GenerateAsync("answer",cancel.Token));Assert.Equal(1,calls);
    }
    [Fact]
    public async Task StructuredGenerationUsesJsonModeThroughSharedQueueWithoutChangingPlainRequests()
    {
        var formats=new List<string>();
        using var backend=new OllamaLocalBackend(new Handler(async(r,t)=>
        {
            if(r.RequestUri!.AbsolutePath=="/api/show") return Json("{}");
            using var body=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(t)); formats.Add(body.RootElement.GetProperty("format").GetString()!);
            return Json("{\"done\":true,\"response\":\"{}\"}");
        }));
        await using var service=new SharedLocalInference(backend);
        Assert.True((await service.GenerateStructuredAsync("Return JSON.")).Available);
        Assert.True((await service.GenerateAsync("Hello.")).Available);
        Assert.Equal(new[]{"json",""},formats);
    }
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
            Assert.True(body.RootElement.GetProperty("raw").GetBoolean());
            Assert.EndsWith("</think>\n\n",body.RootElement.GetProperty("prompt").GetString());
            Assert.Contains("[|im_start|]system",body.RootElement.GetProperty("prompt").GetString());
            Assert.False(body.RootElement.GetProperty("stream").GetBoolean());Assert.False(body.RootElement.GetProperty("think").GetBoolean());
            Assert.Equal(4096,body.RootElement.GetProperty("options").GetProperty("num_ctx").GetInt32());
            return Json("{\"done\":true,\"response\":\"embedded reasoning</think>final only\",\"thinking\":\"private reasoning\"}");}));
        Assert.Equal("final only",await backend.GenerateAsync("<|im_start|>system",default));Assert.Equal(2,calls);
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
