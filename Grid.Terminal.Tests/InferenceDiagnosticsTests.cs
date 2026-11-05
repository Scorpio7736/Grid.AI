using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Grid.AI;
using Grid.Communication;
using Grid.Simulation.Tasks;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class InferenceDiagnosticsTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    [Fact]
    public void RetentionIsBoundedAndExpiresAndClearWorks()
    {
        var clock = new Clock(); var diagnostics = new InferenceDiagnostics(2, clock);
        for (var i = 0; i < 3; i++) { using var span = diagnostics.Begin(Guid.NewGuid(), "MCP", InferenceKind.Chat, Stopwatch.GetTimestamp()); span.Complete(InferenceOutcome.Success); }
        Assert.Equal(2, diagnostics.Snapshot().Count);
        clock.Now += TimeSpan.FromHours(25); Assert.Empty(diagnostics.Snapshot());
        using (var span = diagnostics.Begin(Guid.NewGuid(), "MCP", InferenceKind.Chat, Stopwatch.GetTimestamp())) { }
        diagnostics.Clear(); Assert.Empty(diagnostics.Snapshot());
    }
    private sealed class Backend(bool fail = false) : IInferenceBackend
    {
        public Task<string> GenerateAsync(string prompt, CancellationToken token) => fail
            ? throw new InvalidDataException("PRIVATE EXCEPTION /secret/file") : Task.FromResult("PRIVATE RESPONSE");
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task RequestAndExceptionContentNeverEnterDiagnostics(bool fail)
    {
        await using var service = new SharedLocalInference(new Backend(fail));
        var reply = await service.ForAgent("JN").GenerateAsync("PRIVATE PROMPT /secret/file");
        var trace = Assert.Single(service.Diagnostics.Snapshot());
        Assert.Equal(reply.RequestId, trace.Id); Assert.Equal("JN", trace.Agent);
        Assert.Equal(fail ? InferenceOutcome.InvalidOutput : InferenceOutcome.Success, trace.Outcome);
        var json = JsonSerializer.Serialize(trace); Assert.DoesNotContain("PRIVATE", json); Assert.DoesNotContain("/secret", json);
    }
    [Fact]
    public async Task DisabledRequestsAreRecordedWithoutPrivateReason()
    {
        await using var service = new SharedLocalInference(new Backend(), enabled: false, unavailableReason: "PRIVATE DISABLED REASON");
        await service.GenerateAsync("PRIVATE PROMPT");
        var trace = Assert.Single(service.Diagnostics.Snapshot()); Assert.Equal(InferenceOutcome.Disabled, trace.Outcome);
        Assert.DoesNotContain("PRIVATE", JsonSerializer.Serialize(trace));
    }
    [Fact]
    public void FailedAttemptWithoutMetricsRemainsUnknown()
    {
        var diagnostics = new InferenceDiagnostics();
        using (var failed = diagnostics.Begin(Guid.NewGuid(), "MCP", InferenceKind.Chat, Stopwatch.GetTimestamp())) failed.Attempt();
        Assert.Null(Assert.Single(diagnostics.Snapshot()).OutputTokens);
    }
    private sealed class Delayed : IInferenceBackend
    {
        public async Task<string> GenerateAsync(string prompt, CancellationToken token) { await Task.Delay(10000, token); return "done"; }
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task CallerCancellationAndDeadlineAreDistinguished(bool caller)
    {
        var service = new SharedLocalInference(new Delayed(), deadline: TimeSpan.FromMilliseconds(caller ? 1000 : 30));
        using var stop = new CancellationTokenSource(); if (caller) stop.CancelAfter(30);
        await service.GenerateAsync("private", stop.Token);
        for (var i = 0; i < 100 && service.Diagnostics.Snapshot().Count == 0; i++) await Task.Delay(5);
        Assert.Equal(caller ? InferenceOutcome.Cancelled : InferenceOutcome.Timeout, Assert.Single(service.Diagnostics.Snapshot()).Outcome);
        await service.DisposeAsync();
    }
    private sealed class Handler : HttpMessageHandler
    {
        private int attempts;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var body = request.RequestUri!.AbsolutePath == "/api/show" ? "{}" :
                ++attempts == 1 ? "{\"done\":true,\"done_reason\":\"length\",\"response\":\"PRIVATE\",\"eval_count\":10,\"eval_duration\":1000000000}" :
                "{\"done\":true,\"response\":\"PRIVATE\",\"eval_count\":20,\"eval_duration\":1000000000}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }
    [Fact]
    public async Task RetriedProviderCallRecordsVersionsAndAggregatedCounters()
    {
        using var backend = new OllamaLocalBackend(new Handler()); await using var service = new SharedLocalInference(backend);
        await service.ForAgent("MAXIS").GenerateAsync("private"); var trace = Assert.Single(service.Diagnostics.Snapshot());
        Assert.Equal(2, trace.Attempts); Assert.Equal(30, trace.OutputTokens); Assert.Equal(15, trace.TokensPerSecond);
        Assert.Equal("MAXIS/v2", trace.PromptVersion); Assert.Equal("qwen3:4b", trace.Model); Assert.Equal(64, trace.ModelRevision!.Length);
    }
    [Fact]
    public void InvalidCountersAndMetadataCannotLeakOrOverflow()
    {
        var diagnostics = new InferenceDiagnostics();
        using (var span = diagnostics.Begin(Guid.NewGuid(), "PRIVATE", InferenceKind.Chat, Stopwatch.GetTimestamp()))
        {
            span.ConfigureBaseline("PRIVATE"); span.Attempt(); span.Metrics(new("PRIVATE", true, "stop") { OutputTokens = long.MaxValue });
            span.Attempt(); span.Metrics(new("PRIVATE", true, "stop") { OutputTokens = 1 });
        }
        var trace = Assert.Single(diagnostics.Snapshot()); Assert.Null(trace.OutputTokens); Assert.Null(trace.TokensPerSecond);
        Assert.DoesNotContain("PRIVATE", JsonSerializer.Serialize(trace));
    }
    [Fact]
    public void ToolProjectionDoesNotExposeArgumentsResultsOrAuditDetails()
    {
        var agent = new SystemAgent(Guid.NewGuid(), AgentRole.MAXIS);
        var task = new TaskRecord(Guid.NewGuid(), "PRIVATE REQUEST KEY", agent.Id, "PRIVATE CAPABILITY", "PRIVATE PATH", Guid.NewGuid(), null,
            Grid.Simulation.Tasks.TaskStatus.Completed, 1, null, "PRIVATE RESULT", [new(DateTimeOffset.UtcNow, "Finished", null, null, "PRIVATE DETAIL")]);
        var rows = AiDiagnosticsCommand.Tools(new([agent], [task])); Assert.Single(rows);
        Assert.DoesNotContain("PRIVATE", JsonSerializer.Serialize(rows));
    }
    [Fact]
    public void EmbeddedSuiteAndThresholdsMatchEvidencePolicy()
    {
        var suite = TextBenchmarkSuite.Baseline;
        Assert.Equal(ModelAcceptance.TextSuite, suite.Version); Assert.Equal(ModelAcceptance.RequiredCases, suite.Cases.Select(c => c.Id));
        Assert.Equal(30000, suite.ColdLimitMilliseconds); Assert.Equal(10000, suite.WarmLimitMilliseconds);
        Assert.Equal(5000, suite.FirstTextLimitMilliseconds); Assert.Equal(8_000_000_000, suite.AllocationLimitBytes);
    }
    [Fact]
    public void ResourceSamplingDoesNotTreatReusedProcessIdsAsContinuousCpu()
    {
        var totals = new BenchmarkResourceTotals();
        totals.Observe(100, [new(1, 10, 100)], new(true, 30, 20, "test"));
        totals.Observe(200, [new(1, 10, 140)], new(true, 40, 10, "test"));
        totals.Observe(50, [new(1, 11, 300)], null);
        Assert.Equal(40, totals.CpuMilliseconds); Assert.Equal(200, totals.WorkingSetPeakBytes);
        Assert.Equal(40, totals.ModelAllocationPeakBytes); Assert.Equal(20, totals.GpuAllocationPeakBytes);
    }
}
