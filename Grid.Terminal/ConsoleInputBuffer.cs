using System.Collections.Concurrent;
namespace Grid.Terminal;

/// <summary>One-off typeahead from a skipped reply; never command history or secret input.</summary>
internal static class ConsoleInputBuffer
{
    private static readonly ConcurrentQueue<ConsoleKeyInfo> keys = new();
    public static void Preserve(ConsoleKeyInfo key) => keys.Enqueue(key);
    public static bool TryRead(out ConsoleKeyInfo key) => keys.TryDequeue(out key);
    public static void Clear() { while (keys.TryDequeue(out _)) { } }
}
