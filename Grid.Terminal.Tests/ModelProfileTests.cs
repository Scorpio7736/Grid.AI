using System.Net;
using System.Text.Json;
using Grid.Communication;
using Xunit;
namespace Grid.Terminal.Tests;

public sealed class ModelProfileTests
{
    private sealed class Capture : IInferenceBackend
    {
        public List<string> Agents { get; } = new();
        public int Concurrent, Maximum;
        public Task<string> GenerateAsync(string p,CancellationToken t)=>Task.FromResult("legacy");
        public async Task<string> GenerateProfileAsync(string agent,string prompt,bool structured,string? schema,bool file,CancellationToken token)
        { Maximum=Math.Max(Maximum,Interlocked.Increment(ref Concurrent)); Agents.Add(agent); await Task.Delay(10,token); Interlocked.Decrement(ref Concurrent); return agent; }
    }
    [Fact]
    public async Task ProfileRequestsRemainIsolatedAndShareOneWorker()
    {
        var backend=new Capture(); await using var ai=new SharedLocalInference(backend);
        var replies=await Task.WhenAll(AgentModelProfiles.All.Select(p=>ai.ForAgent(p.Agent).GenerateAsync("test")));
        Assert.Equal(AgentModelProfiles.All.Select(p=>p.Agent),replies.Select(r=>r.Text)); Assert.Equal(1,backend.Maximum);
        Assert.Throws<ArgumentException>(()=>ai.ForAgent("cloud"));
    }
    private sealed class Handler : HttpMessageHandler
    {
        public JsonElement? Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken t)
        {
            if(r.RequestUri!.AbsolutePath=="/api/show") return new(HttpStatusCode.OK){Content=new StringContent("{}")};
            using var json=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(t)); Body=json.RootElement.Clone();
            return new(HttpStatusCode.OK){Content=new StringContent("{\"done\":true,\"response\":\"answer\"}")};
        }
    }
    [Theory]
    [InlineData("MCP")][InlineData("MAXIS")][InlineData("JN")][InlineData("SENTINEL")][InlineData("VOLT")][InlineData("RELAY")]
    public async Task RoleReachesModelWithoutDuplicatedWeights(string role)
    {
        var handler=new Handler(); using var backend=new OllamaLocalBackend(handler); await using var ai=new SharedLocalInference(backend);
        Assert.True((await ai.ForAgent(role).GenerateAsync("Question")).Available);
        Assert.Equal("qwen3:4b",handler.Body!.Value.GetProperty("model").GetString());
        Assert.Contains("Role profile: "+role,handler.Body.Value.GetProperty("prompt").GetString());
        Assert.Equal(4096,handler.Body.Value.GetProperty("options").GetProperty("num_ctx").GetInt32());
    }
    [Fact]
    public async Task LowMemoryFileRequestsUseCpuAndSmallerContext()
    {
        var handler=new Handler(); using var backend=new OllamaLocalBackend(handler,lowMemory:true); await using var ai=new SharedLocalInference(backend);
        Assert.True((await ai.ForAgent("MAXIS").GenerateFileAsync("Draft a small file")).Available);
        var options=handler.Body!.Value.GetProperty("options"); Assert.Equal(0,options.GetProperty("num_gpu").GetInt32());Assert.Equal(8192,options.GetProperty("num_ctx").GetInt32());Assert.Equal(2048,options.GetProperty("num_predict").GetInt32());
    }
    [Fact]
    public async Task LowMemoryOverlongInputIsRefusedBeforeInferenceWithoutTruncation()
    {
        var handler=new Handler(); using var backend=new OllamaLocalBackend(handler,lowMemory:true); await using var ai=new SharedLocalInference(backend);
        var result=await ai.ForAgent("MAXIS").GenerateFileAsync(new string('x',12001));
        Assert.False(result.Available);Assert.True(result.ResponseRejected);Assert.Contains("Split",result.Reason);Assert.Null(handler.Body);
    }
    [Theory]
    [InlineData(127743997000L,true)] [InlineData(127743997001L,false)]
    [InlineData(20000000001L,true)]
    public void ManagedCeilingIncludesGrowthReserveAndLegacyBoundary(long bytes,bool allowed)
    {
        var size=new PackageFootprint(bytes,1000,2000,3);
        Assert.Equal(bytes+3000,size.TotalBytes);
        if(allowed) PackageBudget.RequireBase(size); else Assert.Throws<IOException>(()=>PackageBudget.RequireBase(size));
    }
    [Fact]
    public void MeasurementSeparatesGrowthButIncludesUnknownAssetsAndSource()
    {
        var root=Path.Combine(Path.GetTempPath(),"GridBudget-"+Guid.NewGuid());Directory.CreateDirectory(root);
        try {
            foreach(var path in new[]{"app/main.exe","source/core.cs","runtime/models/blob","extractor/library","data/world.db","runtime/temp/cache","runtime/ai-runtime.log","old-versions/app.exe"}) {
                var full=Path.Combine(root,path);Directory.CreateDirectory(Path.GetDirectoryName(full)!);File.WriteAllBytes(full,new byte[10]);
            }
            var size=PackageBudget.Measure(root);Assert.Equal(40,size.BaseBytes);Assert.Equal(30,size.MutableBytes);Assert.Equal(10,size.StagingBytes);Assert.Equal(80,size.TotalBytes);Assert.Equal(8,size.FileCount);
        } finally { Directory.Delete(root,true); }
    }
    [Fact]
    public void UnbenchmarkedModelManifestIsRefused()
    {
        var root=Path.Combine(Path.GetTempPath(),"GridModel-"+Guid.NewGuid());var path=Path.Combine(root,"models/manifests/registry.ollama.ai/library/qwen3/4b"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,"{}");
        try { Assert.Throws<InvalidDataException>(()=>PackageBudget.VerifyModel(root,false)); } finally { Directory.Delete(root,true); }
    }
}
