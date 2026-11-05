using System.Text.Json;
using Grid.Communication;
using Grid.Simulation.Tasks;

namespace Grid.Terminal;

public sealed record ToolDiagnostic(Guid TaskId, Guid AgentId, AgentRole Role, Grid.Simulation.Tasks.TaskStatus Status,
    int Attempts, DateTimeOffset? StartedUtc, DateTimeOffset? FinishedUtc);
public static class AiDiagnosticsCommand
{
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
        return new("AI REQUEST DIAGNOSTICS\n" + TerminalTable.Render(["Request", "Agent", "Outcome", "Queue ms", "Run ms", "Tries", "Tokens/s"],
            rows.Select(r => new[] { r.Id.ToString()[..8], r.Agent, r.Outcome.ToString(), r.QueueMilliseconds.ToString(), r.RunMilliseconds.ToString(),
                r.Attempts.ToString(), r.TokensPerSecond?.ToString("F1") ?? "unknown" })) +
            $"\n{tools.Count} recent saved task records available in ai diagnostics details. Prompt/output/document/path/exception text is excluded.\n" +
            "Retention: 256 inference records / 24 hours, memory only; cleared on host exit. ai diagnostics clear clears this buffer.\n" +
            "Tokens/s uses provider generation counters, not total request latency. CPU/GPU utilization and sampled peak memory remain unavailable.");
    }
}
