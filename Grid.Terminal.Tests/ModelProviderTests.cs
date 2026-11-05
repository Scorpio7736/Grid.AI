using System.Net;
using System.Runtime.CompilerServices;
using Grid.AI;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class ModelProviderTests
{
    [Theory]
    [InlineData("relative.exe")]
    [InlineData("\\\\server\\share\\llama-server.exe")]
    public async Task LlamaRejectsNonlocalRuntimePaths(string path)
    {
        await using var provider = new LlamaCppModelProvider(path);
        await Assert.ThrowsAsync<IOException>(() => provider.LoadAsync(new("model", "relative.gguf"), default));
    }
    private sealed class FakeProvider : ILocalModelProvider
    {
        public string Name => "test";
        public ModelCapabilities Capabilities { get; } = new(true, false, true, true);
        public List<string> Events { get; } = [];
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool FailLoad, Block, Incomplete, Loaded;
        public Task LoadAsync(LocalModel model, CancellationToken token)
        {
            Events.Add("load:" + model.Id); Loaded = true;
            if (FailLoad) throw new IOException("load failed");
            return Task.CompletedTask;
        }
        public Task UnloadAsync(LocalModel model, CancellationToken token) { Events.Add("unload:" + model.Id); Loaded = false; return Task.CompletedTask; }
        public Task<ModelResources> ResourcesAsync(LocalModel model, CancellationToken token) => Task.FromResult(new ModelResources(Loaded, Loaded ? 100 : 0, 0, "test allocation"));
        public async Task<ModelResult> GenerateAsync(LocalModel model, ModelRequest request, CancellationToken token)
        {
            Events.Add("generate"); Entered.TrySetResult();
            if (Block) await Release.Task.WaitAsync(token);
            return new("complete", true, "stop");
        }
        public async IAsyncEnumerable<ModelChunk> StreamAsync(LocalModel model, ModelRequest request, [EnumeratorCancellation] CancellationToken token)
        {
            Entered.TrySetResult();
            yield return new("first", false);
            if (Block) await Release.Task.WaitAsync(token);
            if (!Incomplete) yield return new("", true, "stop");
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => action(request, token);
    }
    private static HttpResponseMessage Json(string text) => new(HttpStatusCode.OK) { Content = new StringContent(text) };

    [Fact]
    public async Task SwitchWaitsForGenerationAndReleasesPreviousModel()
    {
        var provider = new FakeProvider { Block = true };
        await using var session = new LocalModelSession();
        await session.SwitchAsync(provider, new("one"));
        var generated = session.GenerateAsync(new("hello"));
        await provider.Entered.Task;
        var switched = session.SwitchAsync(provider, new("two"));
        Assert.False(switched.IsCompleted);
        provider.Release.SetResult();
        await generated; await switched;
        Assert.Equal(new[] { "load:one", "generate", "unload:one", "load:two" }, provider.Events);
        var released = await session.UnloadAsync();
        Assert.False(released.Loaded); Assert.Equal(0, released.AllocatedBytes);
    }
    [Fact]
    public async Task FailedLoadIsUnloadedAndSessionCanRecover()
    {
        var provider = new FakeProvider { FailLoad = true };
        await using var session = new LocalModelSession();
        await Assert.ThrowsAsync<IOException>(() => session.SwitchAsync(provider, new("bad")));
        Assert.False(provider.Loaded);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.GenerateAsync(new("hello")));
        provider.FailLoad = false; await session.SwitchAsync(provider, new("good"));
        Assert.True((await session.GenerateAsync(new("hello"))).Complete);
    }
    [Fact]
    public async Task TimeoutReleasesGenerationLease()
    {
        var provider = new FakeProvider { Block = true };
        await using var session = new LocalModelSession(); await session.SwitchAsync(provider, new("one"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.GenerateAsync(new("hello") { Timeout = TimeSpan.FromMilliseconds(25) }));
        provider.Block = false;
        Assert.True((await session.GenerateAsync(new("next"))).Complete);
    }
    [Fact]
    public async Task CancellingStreamAllowsUnloadAndNeverSignalsCompletion()
    {
        var provider = new FakeProvider { Block = true };
        await using var session = new LocalModelSession(); await session.SwitchAsync(provider, new("one"));
        using var stop = new CancellationTokenSource();
        await using (var iterator = session.StreamAsync(new("hello"), stop.Token).GetAsyncEnumerator())
        {
            Assert.True(await iterator.MoveNextAsync()); Assert.False(iterator.Current.Finished);
            stop.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => iterator.MoveNextAsync().AsTask());
        }
        Assert.False((await session.UnloadAsync()).Loaded);
    }
    [Fact]
    public async Task AbandoningStreamReleasesLease()
    {
        var provider = new FakeProvider();
        await using var session = new LocalModelSession(); await session.SwitchAsync(provider, new("one"));
        await foreach (var _ in session.StreamAsync(new("hello"))) break;
        Assert.False((await session.UnloadAsync()).Loaded);
    }
    [Fact]
    public async Task IncompleteStreamIsRejected()
    {
        var provider = new FakeProvider { Incomplete = true };
        await using var session = new LocalModelSession(); await session.SwitchAsync(provider, new("one"));
        await Assert.ThrowsAsync<InvalidDataException>(async () => { await foreach (var _ in session.StreamAsync(new("hello"))) { } });
    }
    [Fact]
    public async Task ImageCapabilityIsCheckedBeforeLoadingOrGenerating()
    {
        var provider = new FakeProvider();
        await using var session = new LocalModelSession();
        await Assert.ThrowsAsync<NotSupportedException>(() => session.SwitchAsync(provider, new("image", Kind: ModelOutputKind.Image)));
        Assert.Empty(provider.Events);
        await session.SwitchAsync(provider, new("text"));
        await Assert.ThrowsAsync<NotSupportedException>(() => session.GenerateAsync(new("draw", Kind: ModelOutputKind.Image)));
    }
    [Fact]
    public async Task DisposalCancelsActiveRequestAndUnloads()
    {
        var provider = new FakeProvider { Block = true };
        var session = new LocalModelSession(); await session.SwitchAsync(provider, new("one"));
        var generating = session.GenerateAsync(new("hello")); await provider.Entered.Task;
        await session.DisposeAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => generating);
        Assert.False(provider.Loaded);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.GenerateAsync(new("again")));
    }
    [Fact]
    public async Task DisposalCanStopStreamEvenWhileConsumerIsPaused()
    {
        var provider = new FakeProvider { Block = true };
        var session = new LocalModelSession(); await session.SwitchAsync(provider, new("one"));
        await using var iterator = session.StreamAsync(new("hello")).GetAsyncEnumerator();
        Assert.True(await iterator.MoveNextAsync());
        await session.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(provider.Loaded);
    }
    [Fact]
    public async Task OllamaLoadAndUnloadUseNumericKeepAliveAndConfirmRelease()
    {
        var loaded = false;
        await using var provider = new OllamaModelProvider(new("http://127.0.0.1:1/"), new Handler(async (request, token) =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/show") return Json("{}");
            if (request.RequestUri.AbsolutePath == "/api/ps")
                return Json(loaded ? "{\"models\":[{\"name\":\"test\",\"size\":100,\"size_vram\":0}]}" : "{\"models\":[]}");
            using var body = System.Text.Json.JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            loaded = body.RootElement.GetProperty("keep_alive").GetInt32() == -1;
            return Json("{\"done\":true}");
        }));
        await provider.LoadAsync(new("test"), default);
        Assert.True((await provider.ResourcesAsync(new("test"), default)).Loaded);
        await provider.UnloadAsync(new("test"), default);
        Assert.False((await provider.ResourcesAsync(new("test"), default)).Loaded);
    }
    [Theory]
    [InlineData("https://127.0.0.1/")]
    [InlineData("http://example.com/")]
    [InlineData("http://127.0.0.1/path")]
    [InlineData("http://127.0.0.1/#fragment")]
    public void TransportRejectsNonlocalAndAmbiguousEndpoints(string url)
        => Assert.Throws<ArgumentException>(() => new LocalHttp(new Uri(url)));

    [Fact]
    public async Task OllamaStreamIncludesExplicitFinalStateAndRejectsMissingFinal()
    {
        var complete = true;
        await using var provider = new OllamaModelProvider(new("http://127.0.0.1:1/"), new Handler((request, _) => Task.FromResult(Json(
            request.RequestUri!.AbsolutePath == "/api/show" ? "{}" : "{\"response\":\"hello\",\"done\":false}\n" + (complete ? "{\"response\":\"\",\"done\":true,\"done_reason\":\"stop\"}\n" : "")))));
        var chunks = new List<ModelChunk>();
        await foreach (var chunk in provider.StreamAsync(new("test"), new("prompt"), default)) chunks.Add(chunk);
        Assert.True(chunks.Last().Finished); Assert.Equal("hello", chunks[0].Text);
        complete = false;
        await Assert.ThrowsAsync<InvalidDataException>(async () => { await foreach (var _ in provider.StreamAsync(new("test"), new("prompt"), default)) { } });
    }

    [Theory]
    [InlineData("10", "1000000000", 10L, 1000000000L)]
    [InlineData("null", "null", null, null)]
    [InlineData("\"unknown\"", "-1", null, null)]
    public async Task OllamaKeepsTokenMetricsUnknownUnlessValid(string count, string duration, long? tokens, long? nanoseconds)
    {
        await using var provider = new OllamaModelProvider(new("http://127.0.0.1:1/"), new Handler((request, _) => Task.FromResult(Json(
            request.RequestUri!.AbsolutePath == "/api/show" ? "{}" : $"{{\"response\":\"hello\",\"done\":true,\"eval_count\":{count},\"eval_duration\":{duration}}}"))));
        var result = await provider.GenerateAsync(new("test"), new("prompt"), default);
        Assert.Equal(tokens, result.OutputTokens); Assert.Equal(nanoseconds, result.GenerationNanoseconds);
    }
}
