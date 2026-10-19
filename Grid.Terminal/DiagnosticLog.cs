using System.Text.Json;

namespace Grid.Terminal;

/// <summary>Operational failures remain visible even when the world database cannot be written.</summary>
internal static class DiagnosticLog
{
    public static Task ErrorAsync(TextWriter output, string operation, Exception exception, Guid? worldId = null) =>
        output.WriteLineAsync(JsonSerializer.Serialize(new
        {
            recordedAtUtc = DateTimeOffset.UtcNow, severity = "Error", component = "Grid.Terminal",
            operation, worldId, exceptionType = exception.GetType().FullName, message = exception.Message
        }));
}
