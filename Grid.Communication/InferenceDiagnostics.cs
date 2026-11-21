using System.Diagnostics;
using Grid.AI;

namespace Grid.Communication;

public enum InferenceOutcome { Success, Disabled, Busy, Cancelled, Timeout, Stopped, OutputLimit, Incomplete, ContextLimit, InvalidOutput, Transport, Unavailable, CacheHit }
public enum InferenceKind { Chat, Structured, FileDraft }
public sealed record InferenceTrace(Guid Id, DateTimeOffset RecordedUtc, string Agent, InferenceKind Kind,
    string? Model, string? ModelRevision, string? PromptVersion, InferenceOutcome Outcome,
    long QueueMilliseconds, long RunMilliseconds, int Attempts, long? OutputTokens, long? GenerationNanoseconds)
{
    // Null means the local provider did not supply this measurement. Times cover all attempts.
    public long? ModelLoadMilliseconds { get; init; }
    public long? PromptEvaluationMilliseconds { get; init; }
    public long? PromptTokens { get; init; }
    public long? ProviderTotalMilliseconds { get; init; }
    public long? FirstVisibleTokenMilliseconds { get; init; } // Buffered generation cannot measure this.
    public long EndToEndMilliseconds => QueueMilliseconds + RunMilliseconds;
    public double? TokensPerSecond => OutputTokens is { } count && GenerationNanoseconds is > 0
        ? count * 1e9 / GenerationNanoseconds.Value : null;
}

/// <summary>Bounded, content-free host diagnostics. No prompt, response, exception text, path or schema fields.</summary>
public sealed class InferenceDiagnostics(int capacity = 256, TimeProvider? clock = null)
{
    private readonly object gate = new();
    private readonly Queue<InferenceTrace> traces = new();
    private readonly TimeProvider time = clock ?? TimeProvider.System;
    private readonly int maximum = capacity is >= 1 and <= 256 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);
    private void Prune()
    {
        var now = time.GetUtcNow();
        while (traces.TryPeek(out var row) && (now - row.RecordedUtc > Retention || row.RecordedUtc > now)) traces.Dequeue();
    }
    internal void Add(InferenceTrace trace)
    {
        lock (gate) { Prune(); while (traces.Count >= maximum) traces.Dequeue(); traces.Enqueue(trace with { RecordedUtc = time.GetUtcNow() }); }
    }
    public IReadOnlyList<InferenceTrace> Snapshot() { lock (gate) { Prune(); return Array.AsReadOnly(traces.ToArray()); } }
    public void Clear() { lock (gate) traces.Clear(); }
    public InferenceSpan Begin(Guid id, string agent, InferenceKind kind, long queuedAt) => new(this, id, agent, kind, queuedAt);
}

public sealed class InferenceSpan : IDisposable
{
    private static readonly AsyncLocal<InferenceSpan?> current = new();
    public static InferenceSpan? Current => current.Value;
    private readonly InferenceSpan? previous;
    private readonly InferenceDiagnostics owner;
    private readonly Guid id;
    private readonly string agent;
    private readonly InferenceKind kind;
    private readonly long started = Stopwatch.GetTimestamp();
    private readonly long queue;
    private string? model, revision, prompt;
    private int attempts;
    private int measuredAttempts;
    private long? tokens = 0, duration = 0, loadDuration = 0, promptTokens = 0, promptDuration = 0, providerDuration = 0;
    private InferenceOutcome outcome = InferenceOutcome.Unavailable;
    private bool disposed;
    internal InferenceSpan(InferenceDiagnostics owner, Guid id, string agent, InferenceKind kind, long queuedAt)
    {
        this.owner = owner; this.id = id; this.kind = kind;
        this.agent = agent is "MCP" or "MAXIS" or "JN" or "TRON" or "VOLT" or "RELAY" ? agent : "unknown";
        queue = Math.Max(0, (long)Stopwatch.GetElapsedTime(queuedAt, started).TotalMilliseconds);
        previous = current.Value; current.Value = this;
    }
    public void ConfigureBaseline(string promptVersion)
    {
        model = "qwen3:4b"; revision = "31D9857B5705086D352D4D6DB04E86C5D0F6A2B5D59A4DC7CE5CDB271CCF9141";
        prompt = promptVersion == agent + "/v1" || promptVersion == agent + "/v2" ? promptVersion : "unknown";
    }
    public void Attempt() { attempts++; }
    public void Metrics(ModelResult result)
    {
        measuredAttempts++;
        static long? Sum(long? a, long? b) => a is >= 0 && b is >= 0 && a <= long.MaxValue - b ? a + b : null;
        tokens = Sum(tokens, result.OutputTokens);
        duration = Sum(duration, result.GenerationNanoseconds);
        loadDuration = Sum(loadDuration, result.LoadNanoseconds);
        promptTokens = Sum(promptTokens, result.PromptTokens);
        promptDuration = Sum(promptDuration, result.PromptEvaluationNanoseconds);
        providerDuration = Sum(providerDuration, result.TotalNanoseconds);
    }
    public void Complete(InferenceOutcome result) => outcome = result;
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        current.Value = previous;
        var complete = attempts != 0 && measuredAttempts == attempts;
        static long? Milliseconds(long? nanoseconds) => nanoseconds is >= 0 ? nanoseconds.Value / 1_000_000 : null;
        owner.Add(new(id, default, agent, kind, model, revision, prompt, outcome, queue,
            (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds, attempts,
            complete ? tokens : null, complete ? duration : null)
        {
            ModelLoadMilliseconds = complete ? Milliseconds(loadDuration) : null,
            PromptEvaluationMilliseconds = complete ? Milliseconds(promptDuration) : null,
            PromptTokens = complete ? promptTokens : null,
            ProviderTotalMilliseconds = complete ? Milliseconds(providerDuration) : null
        });
    }
}
