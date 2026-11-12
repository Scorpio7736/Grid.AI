using Grid.AI;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;

namespace Grid.Communication;

public sealed record InferenceReply(bool Available, string Text, string Reason, long ElapsedMilliseconds = 0)
{
    public bool ResponseRejected { get; init; }
    public Guid? RequestId { get; init; }
    public InferenceUnavailable? Unavailable { get; init; }
}
public sealed record InferenceUnavailable(string Code, string Constraint, IReadOnlyList<string> OfflineAlternatives);
public interface ILocalInference
{
    ILocalInference WithCacheScope(InferenceCacheScope scope, Func<bool> current) => this;
    ILocalInference ForAgent(string agent) => this;
    Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default);
    Task<InferenceReply> GenerateFileAsync(string prompt, CancellationToken token = default) => GenerateStructuredAsync(prompt, token);
    Task<InferenceReply> GenerateStructuredAsync(string prompt, CancellationToken token = default) => GenerateAsync(prompt, token);
    Task<InferenceReply> GenerateStructuredAsync(string prompt, string schema, CancellationToken token = default) => GenerateStructuredAsync(prompt, token);
}
public interface IInferenceBackend
{
    string? CacheVersion(string agent) => null;
    Task<string> GenerateProfileAsync(string agent, string prompt, bool structured, string? schema, bool fileDraft, CancellationToken token)
        => fileDraft ? GenerateFileAsync(prompt, token) : schema is not null ? GenerateStructuredAsync(prompt, schema, token) : structured ? GenerateStructuredAsync(prompt, token) : GenerateAsync(prompt, token);
    Task<string> GenerateAsync(string prompt, CancellationToken token);
    Task<string> GenerateFileAsync(string prompt, CancellationToken token) => GenerateStructuredAsync(prompt, token);
    Task<string> GenerateStructuredAsync(string prompt, CancellationToken token) => GenerateAsync(prompt, token);
    Task<string> GenerateStructuredAsync(string prompt, string schema, CancellationToken token) => GenerateStructuredAsync(prompt, token);
}

/// <summary>One bounded FIFO worker per host, shared by future agent consumers. Never invokes tools.</summary>
public sealed class SharedLocalInference : ILocalInference, IAsyncDisposable
{
    private sealed record Request(string Prompt, bool Structured, CancellationTokenSource Stop, TaskCompletionSource<InferenceReply> Result, string? Schema, bool FileDraft, string Agent, Guid Id, long QueuedAt, CancellationToken CallerToken, InferenceCacheScope? CacheScope, Func<bool>? Current);
    private readonly Channel<Request> queue;
    private readonly CancellationTokenSource shutdown = new();
    private readonly Task worker;
    private readonly IInferenceBackend backend;
    private readonly TimeSpan deadline;
    private readonly bool enabled;
    private readonly string? unavailableReason;
    private int pending;
    private int active;
    private readonly TimeProvider clock;
    private int runtimeFailures;
    private DateTimeOffset retryAfter;
    private readonly ScopedInferenceCache cache;
    public InferenceDiagnostics Diagnostics { get; } = new();
    public int Pending => Volatile.Read(ref pending);
    public bool Enabled => enabled;
    public bool Active => Volatile.Read(ref active) != 0;
    public SharedLocalInference(IInferenceBackend backend, bool enabled = true, int capacity = 8, TimeSpan? deadline = null, string? unavailableReason = null, TimeProvider? clock = null)
    {
        if (capacity is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.backend = backend; this.enabled = enabled; this.deadline = deadline ?? TimeSpan.FromSeconds(120);
        this.unavailableReason=unavailableReason;
        this.clock = clock ?? TimeProvider.System;
        cache = new(this.clock);
        queue = Channel.CreateBounded<Request>(new BoundedChannelOptions(capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
        worker = RunAsync();
    }
    public Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default) => Enqueue(prompt,false,token);
    public Task<InferenceReply> GenerateStructuredAsync(string prompt, CancellationToken token = default) => Enqueue(prompt,true,token);
    public Task<InferenceReply> GenerateStructuredAsync(string prompt, string schema, CancellationToken token = default) => Enqueue(prompt,true,token,schema);
    public Task<InferenceReply> GenerateFileAsync(string prompt, CancellationToken token = default) => Enqueue(prompt, true, token, fileDraft: true);
    public ILocalInference ForAgent(string agent) => new ProfileClient(this, AgentModelProfiles.Get(agent).Agent);
    public ILocalInference WithCacheScope(InferenceCacheScope scope, Func<bool> current) => new ProfileClient(this, "MCP", scope, current);
    private sealed class ProfileClient(SharedLocalInference owner, string agent, InferenceCacheScope? scope = null, Func<bool>? current = null) : ILocalInference
    {
        public ILocalInference ForAgent(string name) => new ProfileClient(owner, AgentModelProfiles.Get(name).Agent, scope, current);
        public ILocalInference WithCacheScope(InferenceCacheScope value, Func<bool> valid) => new ProfileClient(owner, agent, value, valid);
        public Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default) => owner.Enqueue(prompt,false,token,agent:agent,cacheScope:scope,current:current);
        public Task<InferenceReply> GenerateStructuredAsync(string prompt, CancellationToken token = default) => owner.Enqueue(prompt,true,token,agent:agent);
        public Task<InferenceReply> GenerateStructuredAsync(string prompt,string schema,CancellationToken token=default) => owner.Enqueue(prompt,true,token,schema,agent:agent);
        public Task<InferenceReply> GenerateFileAsync(string prompt,CancellationToken token=default) => owner.Enqueue(prompt,true,token,fileDraft:true,agent:agent);
    }
    private async Task<InferenceReply> Enqueue(string prompt,bool structured,CancellationToken token,string? schema=null, bool fileDraft=false,string agent="MCP", InferenceCacheScope? cacheScope=null, Func<bool>? current=null)
    {
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > (fileDraft ? 32768 : 8192)) throw new ArgumentException("Prompt must contain 1..8192 characters.");
        var id = Guid.NewGuid(); var queuedAt = Stopwatch.GetTimestamp();
        var kind = fileDraft ? InferenceKind.FileDraft : structured ? InferenceKind.Structured : InferenceKind.Chat;
        InferenceReply Immediate(InferenceOutcome outcome, string message)
        {
            using var trace = Diagnostics.Begin(id, agent, kind, queuedAt); trace.Complete(outcome);
            return new(false, "", message) { RequestId = id, Unavailable = Failure(outcome.ToString(), message) };
        }
        if (!enabled) return Immediate(InferenceOutcome.Disabled, unavailableReason ?? "Local AI disabled; deterministic GRID remains available.");
        if (InferenceContentGuard.ContainsCredentials(prompt))
            return Immediate(InferenceOutcome.InvalidOutput, "Input contains recognizable credential material. Remove credentials from the request or shared context and retry.") with { ResponseRejected = true };
        if (shutdown.IsCancellationRequested) return Immediate(InferenceOutcome.Stopped, "Local AI stopped.");
        var stop = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token); stop.CancelAfter(fileDraft ? TimeSpan.FromSeconds(300) : deadline);
        var requestToken = stop.Token;
        var completion = new TaskCompletionSource<InferenceReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        Interlocked.Increment(ref pending);
        if (!queue.Writer.TryWrite(new(prompt, structured, stop, completion, schema, fileDraft, agent, id, queuedAt, token, cacheScope, current)))
        { Interlocked.Decrement(ref pending); stop.Dispose(); return Immediate(InferenceOutcome.Busy, "Local AI queue full or stopped; try later."); }
        try { return await completion.Task.WaitAsync(requestToken); }
        catch (OperationCanceledException)
        {
            const string message = "Local AI cancelled or timed out; deterministic GRID remains available.";
            return new(false, "", message) { RequestId = id, Unavailable = Failure("CancelledOrTimeout", message) };
        }
    }
    private static InferenceUnavailable Failure(string code, string constraint) =>
        new(code, constraint, Array.AsReadOnly(new[] { "Inspect world status", "Read granted captured context", "Use reviewed host task and file tools" }));
    private async Task RunAsync()
    {
        await foreach (var request in queue.Reader.ReadAllAsync())
        {
            Interlocked.Decrement(ref pending);
            var timer = Stopwatch.StartNew();
            var span = Diagnostics.Begin(request.Id, request.Agent, request.FileDraft ? InferenceKind.FileDraft : request.Structured ? InferenceKind.Structured : InferenceKind.Chat, request.QueuedAt);
            if (!request.Stop.IsCancellationRequested && clock.GetUtcNow() < retryAfter)
            {
                const string message = "Local AI recovery cooldown is active after repeated runtime failures. Retry in up to 30 seconds or inspect ai status. Offline world, context and reviewed tools remain available.";
                span.Complete(InferenceOutcome.Unavailable);
                span.Dispose();
                request.Stop.Dispose();
                request.Result.TrySetResult(new(false, "", message) { RequestId = request.Id, Unavailable = Failure("CircuitOpen", message) });
                continue;
            }
            InferenceReply reply;
            try
            {
                request.Stop.Token.ThrowIfCancellationRequested(); Interlocked.Exchange(ref active, 1);
                if (request.Current is not null && !request.Current()) throw new UnauthorizedAccessException("Inference scope expired.");
                var version = backend.CacheVersion(request.Agent);
                var key = request.Current is not null && request.CacheScope is { Identity.Capability: "ai.analyze" } scope && version is not null
                    ? ScopedInferenceCache.Key(scope, version, request.Agent, request.Prompt) : null;
                var cached = key is null ? null : cache.Read(key);
                if (cached is not null)
                {
                    span.Complete(InferenceOutcome.CacheHit);
                    request.Result.TrySetResult(new(true, cached, "Scoped exact cache hit.", timer.ElapsedMilliseconds) { RequestId = request.Id });
                    continue;
                }
                var text = await backend.GenerateProfileAsync(request.Agent, request.Prompt, request.Structured, request.Schema, request.FileDraft, request.Stop.Token);
                request.Stop.Token.ThrowIfCancellationRequested();
                if (request.Current is not null && !request.Current()) throw new UnauthorizedAccessException("Inference scope expired.");
                InferenceContentGuard.ValidateOutput(text, request.FileDraft ? 131072 : 8192);
                if (key is not null && version == backend.CacheVersion(request.Agent)) cache.Store(key, text);
                runtimeFailures = 0;
                retryAfter = default;
                span.Complete(InferenceOutcome.Success);
                reply = new(true, text, "Local model output; unverified, no action executed.", timer.ElapsedMilliseconds);
            }
            catch (InvalidDataException e)
            {
                runtimeFailures = 0;
                var reason = (e.Data["GRID_FAILURE"] as string) switch
                {
                    "length" => request.FileDraft ? "The file draft exceeded the extended output budget. No partial file was accepted. Request fewer sections or split the document into files." : "The model reached the response limit after one larger retry. Ask for a shorter answer or fewer proposal steps.",
                    "incomplete" => "The model returned an incomplete response. Retry the request.",
                    "context" => "Input exceeds the reduced-memory context. Split the file/request; nothing was truncated or accepted.",
                    "thinking" => "The model did not finish its final answer. Retry the request.",
                    "credentials" => "The response contained recognizable credential material and was withheld. Remove credentials from shared context before retrying.",
                    "memory" => "Less than 1 GiB of RAM is available. Close memory-heavy applications and retry. World inspection, captured context and reviewed host tools remain available.",
                    _ => "The model response failed validation. Retry the request; check ai status if it persists."
                };
                span.Complete((e.Data["GRID_FAILURE"] as string) switch {
                    "length" => InferenceOutcome.OutputLimit, "incomplete" => InferenceOutcome.Incomplete,
                    "context" => InferenceOutcome.ContextLimit, _ => InferenceOutcome.InvalidOutput });
                reply = new(false,"",reason,timer.ElapsedMilliseconds) { ResponseRejected = true };
            }
            catch (Exception e)
            {
                if (e is HttpRequestException or IOException ||
                    e is OperationCanceledException && Active && !request.CallerToken.IsCancellationRequested && !shutdown.IsCancellationRequested)
                {
                    if (++runtimeFailures >= 3) retryAfter = clock.GetUtcNow().AddSeconds(30);
                }
                span.Complete(e is OperationCanceledException
                    ? request.CallerToken.IsCancellationRequested ? InferenceOutcome.Cancelled : shutdown.IsCancellationRequested ? InferenceOutcome.Stopped : InferenceOutcome.Timeout
                    : e is HttpRequestException or IOException ? InferenceOutcome.Transport : InferenceOutcome.Unavailable);
                reply = new(false, "", "Local AI unavailable (" + e.GetType().Name + "); deterministic GRID remains available.", timer.ElapsedMilliseconds);
            }
            finally { span.Dispose(); Interlocked.Exchange(ref active, 0); request.Stop.Dispose(); }
            request.Result.TrySetResult(reply with { RequestId = request.Id,
                Unavailable = reply.Available ? null : Failure(reply.ResponseRejected ? "Rejected" : "RuntimeUnavailable", reply.Reason) });
        }
    }
    public async ValueTask DisposeAsync()
    { queue.Writer.TryComplete(); shutdown.Cancel(); await worker; shutdown.Dispose(); }
}

/// <summary>Fixed loopback-only endpoint; no proxy, redirect, cloud model or model download path.</summary>
public sealed class OllamaLocalBackend : IInferenceBackend, IDisposable
{
    public string CacheVersion(string agent) => JsonSerializer.Serialize(new { Model,
        revision = "31D9857B5705086D352D4D6DB04E86C5D0F6A2B5D59A4DC7CE5CDB271CCF9141",
        persona = Personas.Assemble(agent, false), lowMemoryMode, cpuOnlyMode, liveLite = AgentModelProfiles.LowMemory });
    public const string Model = "qwen3:4b";
    public const string Endpoint = "http://127.0.0.1:11435/";
    private readonly HttpClient client;
    private readonly OllamaModelProvider provider;
    private readonly bool lowMemoryMode, cpuOnlyMode;
    private readonly Func<long?>? availableMemory;
    public Grid.Agent.AgentPersonas Personas { get; }
    public OllamaLocalBackend(HttpMessageHandler? handler = null, Uri? endpoint = null, bool? lowMemory = null, bool? cpuOnly = null, Grid.Agent.AgentPersonas? personas = null, Func<long?>? availableMemory = null)
    {
        Personas = personas ?? new();
        this.availableMemory = availableMemory;
        lowMemoryMode = lowMemory ?? AgentModelProfiles.LowMemory;
        cpuOnlyMode = lowMemoryMode || (cpuOnly ?? AgentModelProfiles.CpuOnly);
        endpoint ??= new Uri(Endpoint);
        if (endpoint.Scheme != "http" || endpoint.Host != "127.0.0.1" || endpoint.AbsolutePath != "/" || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query))
            throw new ArgumentException("Local AI requires an IPv4 loopback HTTP endpoint.");
        provider = new OllamaModelProvider(endpoint, handler);
        client = new(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false })
        { BaseAddress = endpoint, Timeout = TimeSpan.FromSeconds(120) };
    }
    private async Task<JsonDocument> JsonAsync(HttpRequestMessage request, CancellationToken token)
    {
        using (request)
        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token))
        {
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var bytes = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = await stream.ReadAsync(buffer, token)) > 0)
            { if (bytes.Length + count > 1024 * 1024) throw new InvalidDataException("Local AI response exceeds limit."); await bytes.WriteAsync(buffer.AsMemory(0,count),token); }
            return JsonDocument.Parse(bytes.ToArray());
        }
    }
    public Task<string> GenerateProfileAsync(string agent,string prompt,bool structured,string? schema,bool fileDraft,CancellationToken token)
        => GenerateCore(prompt,structured,token,schema,fileDraft,AgentModelProfiles.Get(agent));
    public Task<string> GenerateAsync(string prompt,CancellationToken token) => GenerateCore(prompt,false,token);
    public Task<string> GenerateStructuredAsync(string prompt,CancellationToken token) => GenerateCore(prompt,true,token);
    public Task<string> GenerateStructuredAsync(string prompt,string schema,CancellationToken token) => GenerateCore(prompt,true,token,schema);
    public Task<string> GenerateFileAsync(string prompt, CancellationToken token) => GenerateCore(prompt, true, token, fileDraft: true);
    private async Task<string> GenerateCore(string prompt,bool structured,CancellationToken token,string? schema=null, bool fileDraft=false, AgentModelProfile? profile=null)
    {
        using var recoveryDeadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        recoveryDeadline.CancelAfter(TimeSpan.FromSeconds(fileDraft ? 300 : 120));
        token = recoveryDeadline.Token;
        var freeMemory = availableMemory?.Invoke();
        if (freeMemory is >= 0 and < 1_073_741_824)
            throw Rejected("memory", "Less than 1 GiB of physical memory is available.");
        var lean = lowMemoryMode || AgentModelProfiles.LowMemory || freeMemory is >= 0 and < 4_294_967_296;
        try
        {
            return await GenerateAttempt(prompt, structured, token, schema, fileDraft, profile, lean, cpuOnlyMode || AgentModelProfiles.ForceLite);
        }
        catch (HttpRequestException error) when (!cpuOnlyMode && error.StatusCode is System.Net.HttpStatusCode.InternalServerError or System.Net.HttpStatusCode.ServiceUnavailable)
        {
            // Release and verify the failed allocation before one CPU/Lite attempt with the installed model.
            token.ThrowIfCancellationRequested();
            await provider.UnloadAsync(new LocalModel(Model), token);
            token.ThrowIfCancellationRequested();
            return await GenerateAttempt(prompt, structured, token, schema, fileDraft, profile, true, true);
        }
    }

    private async Task<string> GenerateAttempt(string prompt, bool structured, CancellationToken token, string? schema,
        bool fileDraft, AgentModelProfile? profile, bool lean, bool cpuOnly)
    {
        profile ??= AgentModelProfiles.Get("MCP");
        if (lean && fileDraft && prompt.Length > 12000) throw Rejected("context", "File input exceeds reduced-memory context. Split the request.");
        var persona = Personas.Assemble(profile.Agent, structured);
        InferenceSpan.Current?.ConfigureBaseline(persona.Version);
        // Retry only an explicitly truncated response, once, within the existing queue deadline.
        // Never return a truncated answer or submit a partial JSON proposal.
        for (var attempt=0;attempt<2;attempt++)
        {
            token.ThrowIfCancellationRequested();
            var request = new ModelRequest(prompt,
                ContextTokens: fileDraft ? (lean ? 8192 : 16384) : (lean ? Math.Min(4096, profile.ContextTokens) : profile.ContextTokens),
                MaxOutputTokens: fileDraft ? (lean ? (attempt == 0 ? 2048 : 4096) : (attempt == 0 ? 4096 : 8192)) : (attempt == 0 ? profile.InitialTokens : profile.RetryTokens),
                Json: structured, JsonSchema: schema, CpuOnly: cpuOnly)
                { Timeout = TimeSpan.FromSeconds(fileDraft ? 300 : 120), SystemInstruction = persona.SystemInstruction, PromptVersion = persona.Version, PromptTemplate = persona.Template };
            InferenceSpan.Current?.Attempt();
            var result = await provider.GenerateAsync(new LocalModel(Model), request, token);
            InferenceSpan.Current?.Metrics(result);
            if (result.FinishReason == "length")
            {
                if (attempt == 0) continue;
                throw Rejected("length", "Local AI response reached its output limit.");
            }
            if (!result.Complete) throw Rejected("incomplete", "Incomplete local AI response.");
            var final = result.Text;
            var end=final.LastIndexOf("</think>",StringComparison.OrdinalIgnoreCase);
            if(end>=0) final=final[(end+8)..];
            if(final.Contains("<think>",StringComparison.OrdinalIgnoreCase)) throw Rejected("thinking","Incomplete thinking block.");
            return final.Trim();
        }
        throw Rejected("length","Local AI response reached its output limit.");
    }
    private static InvalidDataException Rejected(string code,string message)
    {
        var error=new InvalidDataException(message); error.Data["GRID_FAILURE"]=code; return error;
    }
    public async Task<string> StatusAsync(CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            using var version = await JsonAsync(new(HttpMethod.Get,"api/version"), deadline.Token);
            using var tags = await JsonAsync(new(HttpMethod.Get,"api/tags"), deadline.Token);
            var installed = tags.RootElement.GetProperty("models").EnumerateArray().Any(m => m.GetProperty("name").GetString() == Model);
            using var running = await JsonAsync(new(HttpMethod.Get,"api/ps"), deadline.Token);
            var model = running.RootElement.GetProperty("models").EnumerateArray().FirstOrDefault(m => m.GetProperty("name").GetString() == Model);
            var allocation = model.ValueKind == JsonValueKind.Undefined ? "Model not loaded (idle)." :
                $"Runtime-reported model allocation: {model.GetProperty("size").GetInt64():N0} bytes; GPU allocation: {model.GetProperty("size_vram").GetInt64():N0} bytes (allocation, not GPU utilization).";
            return $"Endpoint: {client.BaseAddress} | Ollama {version.RootElement.GetProperty("version").GetString()}\nModel: {Model} | Installed: {installed}\n{allocation}";
        }
        catch (Exception e) when (e is HttpRequestException or IOException or InvalidDataException or JsonException or OperationCanceledException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { return "Local AI unavailable at " + client.BaseAddress + ": " + e.GetType().Name + ": " + e.Message + ". Check bundled runtime setup; deterministic GRID remains available."; }
    }
    public void Dispose() { client.Dispose(); provider.DisposeAsync().GetAwaiter().GetResult(); }
}
