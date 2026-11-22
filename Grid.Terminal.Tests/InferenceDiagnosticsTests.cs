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
                ++attempts == 1 ? "{\"done\":true,\"done_reason\":\"length\",\"response\":\"PRIVATE\",\"eval_count\":10,\"eval_duration\":1000000000,\"load_duration\":2000000,\"prompt_eval_count\":7,\"prompt_eval_duration\":3000000,\"total_duration\":1009000000}" :
                "{\"done\":true,\"response\":\"PRIVATE\",\"eval_count\":20,\"eval_duration\":1000000000,\"load_duration\":4000000,\"prompt_eval_count\":11,\"prompt_eval_duration\":5000000,\"total_duration\":1011000000}";
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
        Assert.Equal(6, trace.ModelLoadMilliseconds);
        Assert.Equal(8, trace.PromptEvaluationMilliseconds);
        Assert.Equal(18, trace.PromptTokens);
        Assert.Equal(2020, trace.ProviderTotalMilliseconds);
        Assert.Null(trace.FirstVisibleTokenMilliseconds); // Buffered path cannot measure a visible first token.
        Assert.True(trace.EndToEndMilliseconds >= trace.QueueMilliseconds);
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
    public async Task ProviderTimingCountersStayUnknownWhenNotReturned()
    {
        await using var service = new SharedLocalInference(new Backend());
        Assert.True((await service.GenerateAsync("private")).Available);
        var trace = Assert.Single(service.Diagnostics.Snapshot());
        Assert.Null(trace.ModelLoadMilliseconds);
        Assert.Null(trace.PromptEvaluationMilliseconds);
        Assert.Null(trace.PromptTokens);
        Assert.Null(trace.ProviderTotalMilliseconds);
        Assert.Null(trace.FirstVisibleTokenMilliseconds);
        Assert.DoesNotContain("PRIVATE", JsonSerializer.Serialize(trace));
    }
    [Fact]
    public void LatencyPercentileIsNearestRankAndSupportsEmptySamples()
    {
        Assert.Null(AiDiagnosticsCommand.Percentile95(Array.Empty<long>()));
        Assert.Equal(95, AiDiagnosticsCommand.Percentile95(Enumerable.Range(1, 100).Reverse().Select(i => (long)i)));
        Assert.Equal(12, AiDiagnosticsCommand.Percentile95(new long[] { 1, 12, 3 }));
    }
    private sealed class FixedBenchmarkBackend : IInferenceBackend
    {
        public int Calls;
        public Task<string> GenerateAsync(string prompt, CancellationToken token)
        {
            Interlocked.Increment(ref Calls);
            if (prompt.Contains("GRID BENCHMARK READY", StringComparison.Ordinal)) return Task.FromResult("GRID BENCHMARK READY");
            if (prompt.Contains("GRID BENCHMARK WARM", StringComparison.Ordinal)) return Task.FromResult("GRID BENCHMARK WARM");
            if (prompt.Contains("GRID BENCHMARK CODE", StringComparison.Ordinal)) return Task.FromResult("GRID BENCHMARK CODE");
            throw new InvalidOperationException("Unexpected diagnostic prompt");
        }
    }
    [Fact]
    public async Task LiveBenchmarkUsesOnlyExistingInferenceQueueAndDoesNotRevealOutputs()
    {
        var backend = new FixedBenchmarkBackend();
        await using var service = new SharedLocalInference(backend);
        var report = await AiDiagnosticsCommand.BenchmarkAsync(service, () => true, CancellationToken.None);
        Assert.Equal(3, backend.Calls);
        Assert.Equal(3, service.Diagnostics.Snapshot().Count);
        Assert.Contains("MCP: exact", report);
        Assert.Contains("MAXIS: exact", report);
        Assert.DoesNotContain("Reply with", report); // Prompt bodies never appear in diagnostics.
        Assert.Contains("Buffered chat does not expose first visible token timing", report);
    }
    [Fact]
    public async Task BenchmarkRevokedHostDoesNotCallLocalModel()
    {
        var backend = new FixedBenchmarkBackend();
        await using var service = new SharedLocalInference(backend);
        var report = await AiDiagnosticsCommand.BenchmarkAsync(service, () => false, CancellationToken.None);
        Assert.Contains("revoked", report);
        Assert.Equal(0, backend.Calls);
    }
    [Fact]
    public async Task BenchmarkClassifiesInvalidOutputWithoutDisplayingPrivateResponse()
    {
        await using var service = new SharedLocalInference(new Backend());
        var report = await AiDiagnosticsCommand.BenchmarkAsync(service, () => true, CancellationToken.None);
        Assert.Contains("non-exact", report);
        Assert.DoesNotContain("PRIVATE RESPONSE", report);
    }
    [Fact]
    public async Task BenchmarkCommandRequiresOriginalHostMasterAndDoesNotCreateTasks()
    {
        var engine = new Grid.Simulation.GridSimulationEngine(Grid.Simulation.GridWorldFactory.CreateGenesis());
        var identity = new Grid.Terminal.TerminalIdentitySession(() => "benchmark-owner");
        identity.Attach(engine);
        var backend = new FixedBenchmarkBackend();
        await using var service = new SharedLocalInference(backend);
        Assert.Contains("original host", AiDiagnosticsCommand.Execute(["diagnostics", "benchmark"], identity, service, null).Text);
        Assert.Equal(0, backend.Calls);
        identity.Login();
        var command = AiDiagnosticsCommand.Execute(["diagnostics", "benchmark"], identity, service, null);
        Assert.NotNull(command.BrokerOperation);
        var result = await command.BrokerOperation!(CancellationToken.None);
        Assert.Contains("LIVE LOCAL INFERENCE PROBE", result.Text);
        Assert.Equal(3, backend.Calls);
        Assert.Empty(engine.GetSnapshot().Tasks.Records);
    }
    [Fact]
    public void ToolProjectionDoesNotExposeArgumentsResultsOrAuditDetails()
    {
        var agent = new SystemAgent(Guid.NewGuid(), AgentRole.MAXIS);
        var task = new TaskRecord(Guid.NewGuid(), "PRIVATE REQUEST KEY", agent.Id, "PRIVATE CAPABILITY", "PRIVATE PATH", Guid.NewGuid(), null,
            Grid.Simulation.Tasks.TaskStatus.Completed, 1, null, "PRIVATE RESULT", [new(DateTimeOffset.UtcNow, "Finished", null, null, "PRIVATE DETAIL")]);
        var rows = AiDiagnosticsCommand.Tools(new([agent], [task])); Assert.Single(rows);
        Assert.DoesNotContain("PRIVATE", JsonSerializer.Serialize(rows));
        Assert.Null(rows[0].ElapsedMilliseconds); // Missing start timestamp is not a zero-duration operation.
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
