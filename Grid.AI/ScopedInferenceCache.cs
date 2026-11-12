using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Grid.AI;

public sealed record InferenceCacheScope(TrustedRequestContext Identity, long Activity, long ContextRevision, long MemoryRevision);

/// <summary>Short-lived exact response cache owned by one local inference service.</summary>
public sealed class ScopedInferenceCache(TimeProvider? clock = null)
{
    private sealed record Entry(string Text, DateTimeOffset Expires, int Bytes);
    private readonly object gate = new();
    private readonly Dictionary<string, Entry> entries = new();
    private readonly TimeProvider time = clock ?? TimeProvider.System;
    private int bytes;
    public const int MaximumEntries = 64;
    public const int MaximumBytes = 524288;
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);

    public static string Key(InferenceCacheScope scope, string backendVersion, string agent, string prompt) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { scope, backendVersion, agent, prompt }))));

    private void Prune()
    {
        var now = time.GetUtcNow();
        foreach (var item in entries.Where(pair => pair.Value.Expires <= now || pair.Value.Expires > now + Lifetime).ToArray())
        {
            bytes -= item.Value.Bytes;
            entries.Remove(item.Key);
        }
    }

    public string? Read(string key)
    {
        lock (gate)
        {
            Prune();
            return entries.GetValueOrDefault(key)?.Text;
        }
    }

    public void Store(string key, string text)
    {
        var size = Encoding.UTF8.GetByteCount(text);
        if (size > MaximumBytes) return;
        lock (gate)
        {
            Prune();
            if (entries.Remove(key, out var old)) bytes -= old.Bytes;
            while (entries.Count >= MaximumEntries || bytes + size > MaximumBytes)
            {
                var oldest = entries.MinBy(pair => pair.Value.Expires);
                bytes -= oldest.Value.Bytes;
                entries.Remove(oldest.Key);
            }
            entries[key] = new(text, time.GetUtcNow() + Lifetime, size);
            bytes += size;
        }
    }
}
