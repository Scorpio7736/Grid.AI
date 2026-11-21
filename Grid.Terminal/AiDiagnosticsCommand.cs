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
    public static IReadOnlyList<ToolDiagnostic> Tools(TaskState state) => state.Records.TakeLast(20).Select(task => new ToolDiagnostic(
        task.Id, task.AgentId, state.Agents.Single(a => a.Id == task.AgentId).Role, task.Status, task.Attempts,
        task.Audit.LastOrDefault(a => a.Action == "Started")?.AtUtc,
        task.Audit.LastOrDefault(a => a.Action is "Finished" or "Cancelled" or "Recovered")?.AtUtc)).ToArray();

    public static CommandResult Execute(IReadOnlyList<string> args, TerminalIdentitySession identity,
        SharedLocalInference service, Func<TaskState>? tasks)
    {
        if (!identity.IsMaster || identity.IsAttachment) return new("Use the original host terminal as Master Control.");
        if (args.Count > 2 || args.Count == 2 && args[1] is not ("details" or "clear")) return new("Usage: ai diagnostics [details|clear]");
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
