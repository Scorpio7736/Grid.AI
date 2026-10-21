using System.Buffers.Binary;
using System.Text.Json;
namespace Grid.Terminal;

public sealed record WindowRequest(string Kind, string? Command = null, int ProgramPage = 0, int EventPage = 0, int Width = 120, int Height = 40);
public sealed record WindowCommandInfo(string Name, string Usage, string Description);
public sealed record WindowReply(string Text, string Prompt = "", WindowCommandInfo[]? Commands = null, bool Closed = false, bool Live = false, bool Clear = false, bool ConfirmLogout = false, string Mode = "terminal");
public static class WindowProtocol
{
    public const int MaximumFrame = 2 * 1024 * 1024;
    public static async Task WriteAsync<T>(Stream stream, T value, CancellationToken token)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        if (bytes.Length > MaximumFrame) throw new IOException("Window output exceeds 2 MiB; use a narrower query.");
        var header = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
        await stream.WriteAsync(header, token); await stream.WriteAsync(bytes, token); await stream.FlushAsync(token);
    }
    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken token)
    {
        var header = new byte[4]; await stream.ReadExactlyAsync(header, token);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (length < 1 || length > MaximumFrame) throw new IOException("Invalid window message size.");
        var bytes = new byte[length]; await stream.ReadExactlyAsync(bytes, token);
        return JsonSerializer.Deserialize<T>(bytes) ?? throw new IOException("Invalid window message.");
    }
}
public static class WindowCommands
{
    private static readonly HashSet<string> Data = new(StringComparer.OrdinalIgnoreCase) { "status", "city", "districts", "programs", "inspect", "behaviors", "resources", "history", "jobs", "activity", "social", "self", "whoami", "conversations", "transcript", "help", "live" };
    public static bool Supports(string name) => Data.Contains(name);
}
