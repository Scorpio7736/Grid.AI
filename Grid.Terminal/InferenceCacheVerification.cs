using System.Diagnostics;
using System.Text.Json;
using Grid.AI;
using Grid.Communication;

namespace Grid.Terminal;

public static class InferenceCacheVerification
{
    public static async Task<int> RunAsync()
    {
        await using var runtime = await BundledAiRuntime.StartAsync(BundledAiRuntime.FindRoot(AppContext.BaseDirectory));
        if (!runtime.Ready) { Console.WriteLine(runtime.Status); return 1; }
        using var backend = new OllamaLocalBackend(endpoint: runtime.Endpoint,
            availableMemory: WindowsHostTelemetryProvider.AvailablePhysicalMemory);
        await using var service = new SharedLocalInference(backend);
        var scope = new InferenceCacheScope(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), 0, Guid.NewGuid(), "cache-verification", "ai.analyze"), 0, 0, 0);
        var client = service.WithCacheScope(scope, () => true);
        const string prompt = "Captured fact: the release code is INDIGO. What is the release code? Answer with the code only.";
        var before = WindowsHostTelemetryProvider.AvailablePhysicalMemory();
        var timer = Stopwatch.StartNew();
        var cold = await client.GenerateAsync(prompt);
        var coldMs = timer.ElapsedMilliseconds;
        var hits = new List<long>();
        var same = true;
        for (var i = 0; i < 10; i++)
        {
            timer.Restart();
            var reply = await client.GenerateAsync(prompt);
            hits.Add(timer.ElapsedMilliseconds);
            same &= reply.Available && reply.Text == cold.Text;
        }
        var count = service.Diagnostics.Snapshot().Count(row => row.Outcome == InferenceOutcome.CacheHit);
        var passed = cold.Available && cold.Text.Contains("INDIGO", StringComparison.OrdinalIgnoreCase) &&
            coldMs <= 120000 && same && count == 10 && hits.Max() <= 500;
        Console.WriteLine(JsonSerializer.Serialize(new { passed, os = Environment.OSVersion.ToString(),
            processors = Environment.ProcessorCount, coldMs, cacheHitMs = hits, cacheHits = count,
            availableRamBefore = before, availableRamAfter = WindowsHostTelemetryProvider.AvailablePhysicalMemory(),
            maximumCacheBytes = ScopedInferenceCache.MaximumBytes, maximumCacheEntries = ScopedInferenceCache.MaximumEntries,
            model = OllamaLocalBackend.Model, allocation = await backend.StatusAsync(default) }, new JsonSerializerOptions { WriteIndented = true }));
        return passed ? 0 : 1;
    }
}
