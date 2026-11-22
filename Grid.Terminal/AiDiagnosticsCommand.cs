using System.Diagnostics;
using System.Text.Json;
using Grid.Communication;
using Grid.Simulation.Tasks;

namespace Grid.Terminal;

public sealed record ToolDiagnostic(Guid TaskId, Guid AgentId, AgentRole Role, Grid.Simulation.Tasks.TaskStatus Status,
    int Attempts, DateTimeOffset? StartedUtc, DateTimeOffset? FinishedUtc)
{
    public long? ElapsedMilliseconds => StartedUtc is { } started && FinishedUtc is { } finished && finished >= started
        ? (long)(finished - started).TotalMilliseconds : null;
}
public static class AiDiagnosticsCommand
{
    // Nearest-rank percentile over recent completed observations, never a latency guarantee.
    public static long? Percentile95(IEnumerable<long> samples)
    {
        var sorted = samples.OrderBy(x => x).ToArray();
        return sorted.Length == 0 ? null : sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1];
    }
    // These are fixed, public, non-sensitive prompts; no user source or conversation content enters the benchmark.
    // The existing SharedLocalInference FIFO, host lease, token limits and diagnostics remain authoritative.
    public static async Task<string> BenchmarkAsync(SharedLocalInference service, Func<bool> current, CancellationToken token)
    {
        var probes = new (string Agent, string Prompt, string Expected)[]
        {
            ("MCP", "Reply with exactly GRID BENCHMARK READY. No punctuation.", "GRID BENCHMARK READY"),
            ("MCP", "Reply with exactly GRID BENCHMARK WARM. No punctuation.", "GRID BENCHMARK WARM"),
            ("MAXIS", "Reply with exactly GRID BENCHMARK CODE. No punctuation.", "GRID BENCHMARK CODE")
        };
        var lines = new List<string> {
            "LIVE LOCAL INFERENCE PROBE — three fixed prompts, no files, tasks, or cloud calls.",
            "The first request is not guaranteed to be cold. Use --evaluate-model-baseline with normal GRID sessions closed for controlled cold/streamed first-text evidence."
        };
        foreach (var probe in probes)
        {
            token.ThrowIfCancellationRequested();
            if (!current()) return "Benchmark stopped: host authorization was revoked. No model output displayed.";
            var stopwatch = Stopwatch.StartNew();
            var reply = await service.ForAgent(probe.Agent).GenerateAsync(probe.Prompt, token);
            token.ThrowIfCancellationRequested();
            if (!current()) return "Benchmark stopped: host authorization was revoked. No model output displayed.";
            var trace = reply.RequestId is { } id
                ? service.Diagnostics.Snapshot().LastOrDefault(t => t.Id == id) : null;
            var exact = reply.Available && string.Equals(reply.Text.Trim(), probe.Expected, StringComparison.Ordinal);
            // Never print raw model output or transport errors; show only classification and safe timing fields.
            lines.Add($"{probe.Agent}: {(exact ? "exact" : reply.Available ? "non-exact" : "unavailable")} | wall {stopwatch.ElapsedMilliseconds} ms" +
                $" | queue {trace?.QueueMilliseconds.ToString() ?? "unknown"} ms" +
                $" | model load {trace?.ModelLoadMilliseconds?.ToString() ?? "unknown"} ms" +
                $" | prefill {trace?.PromptEvaluationMilliseconds?.ToString() ?? "unknown"} ms");
            if (!reply.Available) break;
        }
        lines.Add("This is a tiny diagnostic sample, not a p95 benchmark or production-model evaluation. Buffered chat does not expose first visible token timing.");
        return string.Join("\n", lines);
    }

    public static IReadOnlyList<ToolDiagnostic> Tools(TaskState state) => state.Records.TakeLast(20).Select(task => new ToolDiagnostic(
        task.Id, task.AgentId, state.Agents.Single(a => a.Id == task.AgentId).Role, task.Status, task.Attempts,
        task.Audit.LastOrDefault(a => a.Action == "Started")?.AtUtc,
        task.Audit.LastOrDefault(a => a.Action is "Finished" or "Cancelled" or "Recovered")?.AtUtc)).ToArray();

    public static CommandResult Execute(IReadOnlyList<string> args, TerminalIdentitySession identity,
        SharedLocalInference service, Func<TaskState>? tasks)
    {
        if (!identity.IsMaster || identity.IsAttachment) return new("Use the original host terminal as Master Control.");
        if (args.Count > 2 || args.Count == 2 && args[1] is not ("details" or "clear" or "benchmark")) return new("Usage: ai diagnostics [details|clear|benchmark]");
        if (args.Count == 2 && args[1] == "benchmark")
        {
            var reference = identity.Reference;
            var used = 0;
            return new("", BrokerOperation: async token =>
            {
                if (Interlocked.Exchange(ref used, 1) != 0 || !identity.IsMaster || identity.Reference != reference)
                    return new("Benchmark authorization expired or was already consumed.");
                try
                {
                    using var authorization = TerminalRequestAuthorization.AcquireStandalone(identity, "ai.diagnostic",
                        () => identity.IsMaster && identity.Reference == reference);
                    using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
                    stop.CancelAfter(TimeSpan.FromSeconds(90));
                    bool Current() => authorization.IsCurrent && identity.IsMaster && identity.Reference == reference;
                    var run = BenchmarkAsync(service, Current, stop.Token);
                    while (!run.IsCompleted)
                    {
                        await Task.WhenAny(run, Task.Delay(50, token));
                        if (token.IsCancellationRequested || !Current()) { stop.Cancel(); break; }
                    }
                    var report = await run;
                    if (token.IsCancellationRequested || !Current())
                        return new("Benchmark authorization expired; results withheld.");
                    return new(report);
                }
                catch (OperationCanceledException) { return new("Benchmark cancelled or exceeded the 90-second budget."); }
                catch (Exception error) when (error is UnauthorizedAccessException or InvalidOperationException)
                { return new("Benchmark refused: " + error.GetType().Name); }
            });
        }
        if (args.Count == 2 && args[1] == "clear") { service.Diagnostics.Clear(); return new("In-memory inference diagnostics cleared. Existing task audit records remain unchanged."); }
        var rows = service.Diagnostics.Snapshot().TakeLast(20).ToArray();
        var tools = tasks is null ? [] : Tools(tasks());
        if (args.Count == 2) return new(JsonSerializer.Serialize(new { SchemaVersion = 1, Inference = rows, Tools = tools,
            Limits = "Inference: last 256 / 24 hours, memory only. Tool projection: last 20 existing saved tasks. No request-to-task correlation is inferred. CPU/GPU utilization and sampled peak memory are unavailable here; ai status and baseline reports label their separate measurements." }, new JsonSerializerOptions { WriteIndented = true }));
        var queueP95 = Percentile95(rows.Select(r => r.QueueMilliseconds));
        var totalP95 = Percentile95(rows.Select(r => r.EndToEndMilliseconds));
        return new("AI REQUEST DIAGNOSTICS\n" + TerminalTable.Render(["Request", "Agent", "Outcome", "Queue", "Run", "Load", "Prefill", "Tok/s"],
            rows.Select(r => new[] { r.Id.ToString()[..8], r.Agent, r.Outcome.ToString(), r.QueueMilliseconds.ToString(), r.RunMilliseconds.ToString(),
                r.ModelLoadMilliseconds?.ToString() ?? "?", r.PromptEvaluationMilliseconds?.ToString() ?? "?",
                r.TokensPerSecond?.ToString("F1") ?? "?" })) +
            $"\nRecent inference (n={rows.Length}): p95 queue {queueP95?.ToString() ?? "unknown"} ms, p95 end-to-end {totalP95?.ToString() ?? "unknown"} ms.\n" +
            $"{tools.Count} recent saved task records available in ai diagnostics details (with timestamp-derived elapsed durations). Prompt/output/document/path/exception text is excluded.\n" +
            "Retention: 256 inference records / 24 hours, memory only; cleared on host exit. ai diagnostics clear clears this buffer.\n" +
            "Load/Prefill are Ollama provider timings in ms (unknown when not returned). First visible token is unavailable for buffered responses. Tokens/s excludes queue and prompt time. Tool records are not linked to inference requests; no causal correlation is inferred.");
    }
}
