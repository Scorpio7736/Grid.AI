using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;

namespace Grid.Communication;

public sealed record InferenceReply(bool Available, string Text, string Reason, long ElapsedMilliseconds = 0);
public interface ILocalInference
{
    Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default);
}
public interface IInferenceBackend
{
    Task<string> GenerateAsync(string prompt, CancellationToken token);
}

/// <summary>One bounded FIFO worker per host, shared by future agent consumers. Never invokes tools.</summary>
public sealed class SharedLocalInference : ILocalInference, IAsyncDisposable
{
    private sealed record Request(string Prompt, CancellationTokenSource Stop, TaskCompletionSource<InferenceReply> Result);
    private readonly Channel<Request> queue;
    private readonly CancellationTokenSource shutdown = new();
    private readonly Task worker;
    private readonly IInferenceBackend backend;
    private readonly TimeSpan deadline;
    private readonly bool enabled;
    private int pending;
    private int active;
    public int Pending => Volatile.Read(ref pending);
    public bool Enabled => enabled;
    public bool Active => Volatile.Read(ref active) != 0;
    public SharedLocalInference(IInferenceBackend backend, bool enabled = true, int capacity = 8, TimeSpan? deadline = null)
    {
        if (capacity is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.backend = backend; this.enabled = enabled; this.deadline = deadline ?? TimeSpan.FromSeconds(120);
        queue = Channel.CreateBounded<Request>(new BoundedChannelOptions(capacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
        worker = RunAsync();
    }
    public async Task<InferenceReply> GenerateAsync(string prompt, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 8192) throw new ArgumentException("Prompt must contain 1..8192 characters.");
        if (!enabled) return new(false, "", "Local AI disabled; deterministic GRID remains available.");
        if (shutdown.IsCancellationRequested) return new(false, "", "Local AI stopped.");
        var stop = CancellationTokenSource.CreateLinkedTokenSource(token, shutdown.Token); stop.CancelAfter(deadline);
        var requestToken = stop.Token;
        var completion = new TaskCompletionSource<InferenceReply>(TaskCreationOptions.RunContinuationsAsynchronously);
        Interlocked.Increment(ref pending);
        if (!queue.Writer.TryWrite(new(prompt, stop, completion)))
        { Interlocked.Decrement(ref pending); stop.Dispose(); return new(false, "", "Local AI queue full or stopped; try later."); }
        try { return await completion.Task.WaitAsync(requestToken); }
        catch (OperationCanceledException) { return new(false, "", "Local AI cancelled or timed out; deterministic GRID remains available."); }
    }
    private async Task RunAsync()
    {
        await foreach (var request in queue.Reader.ReadAllAsync())
        {
            Interlocked.Decrement(ref pending);
            var timer = Stopwatch.StartNew();
            try
            {
                request.Stop.Token.ThrowIfCancellationRequested(); Interlocked.Exchange(ref active, 1);
                var text = await backend.GenerateAsync(request.Prompt, request.Stop.Token);
                request.Stop.Token.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(text) || text.Length > 8192) throw new InvalidDataException("Empty or excessive model output.");
                request.Result.TrySetResult(new(true, text, "Local model output; unverified, no action executed.", timer.ElapsedMilliseconds));
            }
            catch (Exception e)
            { request.Result.TrySetResult(new(false, "", "Local AI unavailable (" + e.GetType().Name + "); deterministic GRID remains available.", timer.ElapsedMilliseconds)); }
            finally { Interlocked.Exchange(ref active, 0); request.Stop.Dispose(); }
        }
    }
    public async ValueTask DisposeAsync()
    { queue.Writer.TryComplete(); shutdown.Cancel(); await worker; shutdown.Dispose(); }
}

/// <summary>Fixed loopback-only endpoint; no proxy, redirect, cloud model or model download path.</summary>
public sealed class OllamaLocalBackend : IInferenceBackend, IDisposable
{
    public const string Model = "qwen3:4b";
    public const string Endpoint = "http://127.0.0.1:11435/";
    private readonly HttpClient client;
    public OllamaLocalBackend(HttpMessageHandler? handler = null)
    {
        client = new(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false })
        { BaseAddress = new Uri(Endpoint), Timeout = TimeSpan.FromSeconds(120) };
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
    private Task<JsonDocument> Post(string path, object body, CancellationToken token) => JsonAsync(new(HttpMethod.Post,path) { Content = JsonContent.Create(body) },token);
    public async Task<string> GenerateAsync(string prompt, CancellationToken token)
    {
        using var model = await Post("api/show", new { model = Model }, token);
        if (model.RootElement.TryGetProperty("remote_model", out _) || model.RootElement.TryGetProperty("remote_host", out _))
            throw new InvalidDataException("Cloud-backed models are not supported.");
        using var result = await Post("api/generate", new { model = Model, prompt, stream = false, think = false, keep_alive = "1m",
            system = "You are a local GRID inference test. Return text only. You have no tools or authority to execute actions. /no_think",
            options = new { num_ctx = 4096, num_predict = 512, temperature = 0 } },token);
        var root = result.RootElement;
        if (!root.TryGetProperty("done",out var done) || done.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty("response",out var text) || text.ValueKind != JsonValueKind.String ||
            root.TryGetProperty("done_reason",out var reason) && reason.GetString() == "length")
            throw new InvalidDataException("Incomplete local AI response.");
        var final = text.GetString()!;
        // Some Qwen templates embed a thinking suffix despite think=false; expose only final text.
        var end = final.LastIndexOf("</think>", StringComparison.OrdinalIgnoreCase);
        if (end >= 0) final = final[(end + 8)..];
        if (final.Contains("<think>",StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Incomplete thinking block.");
        return final.Trim();
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
            return $"Endpoint: {Endpoint} | Ollama {version.RootElement.GetProperty("version").GetString()}\nModel: {Model} | Installed: {installed}\n{allocation}";
        }
        catch (Exception e) when (e is HttpRequestException or IOException or InvalidDataException or JsonException or OperationCanceledException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { return "Local AI unavailable: " + e.GetType().Name + ". Run Start-GridLocalAI.ps1; deterministic GRID remains available."; }
    }
    public void Dispose() => client.Dispose();
}
