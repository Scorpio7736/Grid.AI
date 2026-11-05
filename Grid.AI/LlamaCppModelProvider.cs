using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Grid.AI;

/// <summary>Owns a single local CPU llama-server process and an explicitly supplied GGUF.</summary>
public sealed class LlamaCppModelProvider(string executable) : ILocalModelProvider
{
    private Process? process;
    private LocalHttp? http;
    private LocalModel? loaded;
    public string Name => "llama.cpp CPU";
    public ModelCapabilities Capabilities { get; } = new(true, false, true, true);

    public async Task LoadAsync(LocalModel model, CancellationToken token)
    {
        if (model.Kind != ModelOutputKind.Text) throw new NotSupportedException("This llama.cpp adapter supports text output only.");
        ValidateLocalFile(executable);
        ValidateLocalFile(model.FilePath);
        using (var file = File.OpenRead(model.FilePath!))
        {
            var signature = new byte[4]; file.ReadExactly(signature);
            if (!signature.AsSpan().SequenceEqual("GGUF"u8)) throw new InvalidDataException("Model file is not GGUF.");
        }
        await UnloadAsync(model, token);
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Path.GetDirectoryName(executable)!
        };
        foreach (var argument in new[] { "--model", model.FilePath!, "--host", "127.0.0.1", "--port", port.ToString(),
            "--ctx-size", "16384", "--parallel", "1", "--n-gpu-layers", "0", "--no-warmup", "--no-webui", "--offline", "--cache-ram", "0", "--no-context-shift" }) start.ArgumentList.Add(argument);
        foreach (var key in start.Environment.Keys.Where(key => key.StartsWith("LLAMA_ARG_", StringComparison.OrdinalIgnoreCase)).ToArray())
            start.Environment.Remove(key);
        process = Process.Start(start) ?? throw new IOException("Local llama-server did not start.");
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine(); process.BeginErrorReadLine();
        http = new(new Uri($"http://127.0.0.1:{port}/"));
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token); stop.CancelAfter(TimeSpan.FromMinutes(2));
        try
        {
            while (!process.HasExited)
            {
                try
                {
                    using var result = await http.JsonAsync(HttpMethod.Get, "health", null, stop.Token);
                    if (result.RootElement.GetProperty("status").GetString() == "ok") { loaded = model; return; }
                }
                catch (HttpRequestException) { }
                await Task.Delay(100, stop.Token);
            }
            throw new IOException("Local llama-server exited during loading.");
        }
        catch { await UnloadAsync(model, CancellationToken.None); throw; }
    }
    private static void ValidateLocalFile(string? path)
    {
        if (path is null || !Path.IsPathFullyQualified(path) || path.StartsWith(@"\\", StringComparison.Ordinal))
            throw new IOException("Provide installed absolute local llama-server and GGUF paths.");
        if (new DriveInfo(Path.GetPathRoot(path)!).DriveType == DriveType.Network)
            throw new IOException("Network runtime/model paths are unsupported.");
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                throw new IOException("Runtime/model links are unsupported.");
        if (!File.Exists(path)) throw new IOException("Installed runtime/model file missing.");
    }
    public async Task UnloadAsync(LocalModel model, CancellationToken token)
    {
        if (process is not null)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) when (process.HasExited) { }
            await process.WaitForExitAsync(token);
            process.Dispose(); process = null;
        }
        http?.Dispose(); http = null; loaded = null;
    }
    public Task<ModelResources> ResourcesAsync(LocalModel model, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            if (process is not null && !process.HasExited)
            {
                process.Refresh();
                return Task.FromResult(new ModelResources(true, process.WorkingSet64, 0, "Owned CPU llama-server working set, not model allocation."));
            }
        }
        catch (InvalidOperationException) when (process?.HasExited == true) { }
        return Task.FromResult(new ModelResources(false, 0, 0, "Owned llama-server process has exited."));
    }
    private LocalHttp Require(LocalModel model, ModelRequest request)
    {
        request.Validate(Capabilities);
        if (request.ContextTokens > 16384) throw new ArgumentException("Requested context exceeds the loaded llama.cpp capacity.");
        if (model != loaded || http is null || process is null || process.HasExited) throw new InvalidOperationException("Requested local GGUF is not loaded.");
        return http;
    }
    private static object Body(ModelRequest request, bool stream) => new
    {
        prompt = request.Prompt, n_predict = request.MaxOutputTokens, temperature = 0, stream, cache_prompt = false,
        json_schema = request.JsonSchema is not null ? JsonSerializer.Deserialize<JsonElement>(request.JsonSchema)
            : request.Json ? JsonSerializer.Deserialize<JsonElement>("{\"type\":\"object\"}") : (JsonElement?)null
    };
    private static string Finish(JsonElement root)
    {
        if (root.TryGetProperty("truncated", out var truncated) && truncated.GetBoolean()) return "length";
        if (root.TryGetProperty("stop_type", out var type) && type.GetString() == "limit") return "length";
        if (root.TryGetProperty("stopped_limit", out var limit) && limit.GetBoolean()) return "length";
        return "stop";
    }
    private static async Task CheckContextAsync(LocalHttp transport, ModelRequest request, CancellationToken token)
    {
        using var counted = await transport.JsonAsync(HttpMethod.Post, "tokenize", new { content = request.Prompt, add_special = true }, token);
        if (counted.RootElement.GetProperty("tokens").GetArrayLength() + request.MaxOutputTokens > request.ContextTokens)
            throw new InvalidDataException("Prompt plus output budget exceeds requested context; split the input.");
    }
    public async Task<ModelResult> GenerateAsync(LocalModel model, ModelRequest request, CancellationToken token)
    {
        var transport = Require(model, request);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token); stop.CancelAfter(request.Timeout);
        await CheckContextAsync(transport, request, stop.Token);
        using var result = await transport.JsonAsync(HttpMethod.Post, "completion", Body(request, false), stop.Token);
        var root = result.RootElement;
        var finished = root.TryGetProperty("stop", out var done) && done.GetBoolean();
        var reason = finished ? Finish(root) : "incomplete";
        return new(root.GetProperty("content").GetString() ?? "", finished && reason != "length", reason);
    }
    public async IAsyncEnumerable<ModelChunk> StreamAsync(LocalModel model, ModelRequest request, [EnumeratorCancellation] CancellationToken token)
    {
        var transport = Require(model, request);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token); stop.CancelAfter(request.Timeout);
        await CheckContextAsync(transport, request, stop.Token);
        var finished = false;
        await foreach (var line in transport.LinesAsync("completion", Body(request, true), stop.Token))
        {
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;
            if (finished) throw new InvalidDataException("Data after completed stream.");
            using var frame = JsonDocument.Parse(line[6..]);
            var root = frame.RootElement;
            finished = root.GetProperty("stop").GetBoolean();
            yield return new(root.GetProperty("content").GetString() ?? "", finished, finished ? Finish(root) : "");
        }
        if (!finished) throw new InvalidDataException("Incomplete model stream.");
    }
    public async ValueTask DisposeAsync() => await UnloadAsync(loaded ?? new("none"), CancellationToken.None);
}
