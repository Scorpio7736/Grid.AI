using System.Text.Json;
using Grid.AI;

namespace Grid.Terminal;

public static class ModelProviderVerification
{
    public static async Task<int> RunAsync()
    {
        var root = BundledAiRuntime.FindRoot(AppContext.BaseDirectory) ?? throw new IOException("Provision local runtime assets first.");
        PackageBudget.VerifyModel(root, true);
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "models/manifests/registry.ollama.ai/library/qwen3/4b")));
        var weights = manifest.RootElement.GetProperty("layers").EnumerateArray().MaxBy(layer => layer.GetProperty("size").GetInt64());
        var modelFile = Path.Combine(root, "models", "blobs", weights.GetProperty("digest").GetString()!.Replace(':', '-'));
        var model = new LocalModel("qwen3:4b", modelFile);
        var server = Path.Combine(root, "llamacpp", "b12110", "llama-server.exe");
        if (!File.Exists(server)) throw new IOException("Provision the pinned llama.cpp test runtime first. No automatic downloads.");
        await using var runtime = await BundledAiRuntime.StartAsync(root);
        if (!runtime.Ready) throw new IOException(runtime.Status);
        await using var ollama = new OllamaModelProvider(runtime.Endpoint);
        await using var llama = new LlamaCppModelProvider(server);
        await using var session = new LocalModelSession();
        var checks = new List<object>();
        var passed = true;
        var request = new ModelRequest("Return a JSON object with status equal to ready.", MaxOutputTokens: 64, Json: true,
            JsonSchema: "{\"type\":\"object\",\"properties\":{\"status\":{\"type\":\"string\",\"enum\":[\"ready\"]}},\"required\":[\"status\"],\"additionalProperties\":false}");
        foreach (var provider in new ILocalModelProvider[] { ollama, llama })
        {
            await session.SwitchAsync(provider, model);
            var allocation = await provider.ResourcesAsync(model, default);
            var reply = await session.GenerateAsync(request);
            var text = ""; var finished = false;
            await foreach (var chunk in session.StreamAsync(request)) { text += chunk.Text; finished = chunk.Finished && chunk.FinishReason == "stop"; }
            var cancelled = false;
            using (var stop = new CancellationTokenSource())
            {
                try
                {
                    await foreach (var chunk in session.StreamAsync(request, stop.Token))
                        if (chunk.Text.Length > 0) stop.Cancel();
                }
                catch (OperationCanceledException) { cancelled = true; }
            }
            var next = await session.GenerateAsync(request);
            var release = await session.UnloadAsync();
            var ok = reply.Complete && reply.Text.Contains("ready") && finished && text.Contains("ready") &&
                cancelled && next.Complete && !release.Loaded && allocation.Loaded;
            checks.Add(new { Provider = provider.Name, Passed = ok, Reply = reply.Text, reply.Complete, reply.FinishReason, Stream = text, StreamFinished = finished, Cancelled = cancelled, NextComplete = next.Complete, Allocation = allocation, Released = release });
            Console.WriteLine($"{provider.Name}: passed={ok}; allocation={allocation.AllocatedBytes}; released={!release.Loaded}; cancellation={cancelled}");
            passed &= ok;
        }
        var report = Path.Combine(Path.GetTempPath(), "GRID-616-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(report, JsonSerializer.Serialize(new { Passed = passed, Checks = checks }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Provider verification report: " + report);
        return passed ? 0 : 1;
    }
}
