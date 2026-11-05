using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Grid.AI;

public sealed class OllamaModelProvider : ILocalModelProvider
{
    private readonly LocalHttp http;
    public string Name => "Ollama";
    public ModelCapabilities Capabilities { get; } = new(true, false, true, true);
    public OllamaModelProvider(Uri endpoint, HttpMessageHandler? handler = null) => http = new(endpoint, handler);

    private async Task CheckLocalAsync(LocalModel model, CancellationToken token)
    {
        if (model.Kind != ModelOutputKind.Text) throw new NotSupportedException("This Ollama adapter supports text output only.");
        if (string.IsNullOrWhiteSpace(model.Id) || model.Id.Length > 200 || model.Id.Any(char.IsControl)) throw new ArgumentException("Invalid model ID.");
        using var info = await http.JsonAsync(HttpMethod.Post, "api/show", new { model = model.Id }, token);
        if (info.RootElement.TryGetProperty("remote_model", out _) || info.RootElement.TryGetProperty("remote_host", out _))
            throw new InvalidDataException("Cloud-backed models are not supported.");
    }
    public async Task LoadAsync(LocalModel model, CancellationToken token)
    {
        await CheckLocalAsync(model, token);
        using var result = await http.JsonAsync(HttpMethod.Post, "api/generate", new { model = model.Id, stream = false, keep_alive = -1 }, token);
        if (!result.RootElement.GetProperty("done").GetBoolean()) throw new IOException("Model load did not complete.");
    }
    public async Task UnloadAsync(LocalModel model, CancellationToken token)
    {
        using var result = await http.JsonAsync(HttpMethod.Post, "api/generate", new { model = model.Id, stream = false, keep_alive = 0 }, token);
        for (var i = 0; i < 100; i++)
        {
            if (!(await ResourcesAsync(model, token)).Loaded) return;
            await Task.Delay(100, token);
        }
        throw new IOException("Ollama still reports the model loaded.");
    }
    public async Task<ModelResources> ResourcesAsync(LocalModel model, CancellationToken token)
    {
        using var result = await http.JsonAsync(HttpMethod.Get, "api/ps", null, token);
        var loaded = result.RootElement.GetProperty("models").EnumerateArray().FirstOrDefault(m => m.GetProperty("name").GetString() == model.Id);
        return loaded.ValueKind == JsonValueKind.Undefined ? new(false, 0, 0, "Ollama api/ps has no matching loaded model.")
            : new(true, loaded.GetProperty("size").GetInt64(), loaded.GetProperty("size_vram").GetInt64(), "Ollama allocation report, not exclusive process utilization.");
    }
    private static object Body(LocalModel model, ModelRequest request, bool stream) => new
    {
        model = model.Id, prompt = request.Prompt, raw = true, stream, think = false, keep_alive = "1m",
        format = request.JsonSchema is null ? (object)(request.Json ? "json" : "") : JsonSerializer.Deserialize<JsonElement>(request.JsonSchema),
        options = new { num_ctx = request.ContextTokens, num_predict = request.MaxOutputTokens, temperature = 0, num_gpu = request.CpuOnly ? 0 : -1 }
    };
    public async Task<ModelResult> GenerateAsync(LocalModel model, ModelRequest request, CancellationToken token)
    {
        request.Validate(Capabilities);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(request.Timeout);
        await CheckLocalAsync(model, deadline.Token);
        using var result = await http.JsonAsync(HttpMethod.Post, "api/generate", Body(model, request, false), deadline.Token);
        var root = result.RootElement;
        if (!root.TryGetProperty("done", out var done) || done.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty("response", out var response) || response.ValueKind != JsonValueKind.String)
            return new("", false, "incomplete");
        var reason = root.TryGetProperty("done_reason", out var finish) ? finish.GetString() ?? "stop" : "stop";
        static long? Counter(JsonElement root, string name) => root.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt64(out var value) && value >= 0 ? value : null;
        return new(response.GetString()!, reason != "length", reason)
        { OutputTokens = Counter(root, "eval_count"), GenerationNanoseconds = Counter(root, "eval_duration") };
    }
    public async IAsyncEnumerable<ModelChunk> StreamAsync(LocalModel model, ModelRequest request, [EnumeratorCancellation] CancellationToken token)
    {
        request.Validate(Capabilities);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(request.Timeout);
        await CheckLocalAsync(model, deadline.Token);
        var finished = false;
        await foreach (var line in http.LinesAsync("api/generate", Body(model, request, true), deadline.Token))
        {
            if (line.Length == 0) continue;
            if (finished) throw new InvalidDataException("Data after completed stream.");
            using var frame = JsonDocument.Parse(line);
            var root = frame.RootElement;
            if (root.TryGetProperty("error", out _)) throw new IOException("Local model stream failed.");
            finished = root.GetProperty("done").GetBoolean();
            var text = root.GetProperty("response").GetString() ?? "";
            var reason = finished ? (root.TryGetProperty("done_reason", out var why) ? why.GetString() ?? "stop" : "stop") : "";
            yield return new(text, finished, reason);
        }
        if (!finished) throw new InvalidDataException("Incomplete model stream.");
    }
    public ValueTask DisposeAsync() { http.Dispose(); return ValueTask.CompletedTask; }
}
