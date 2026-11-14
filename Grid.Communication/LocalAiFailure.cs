using System.Net;
using System.Net.Sockets;

namespace Grid.Communication;

/// <summary>Actionable diagnostics without echoing exception bodies, prompts or credentials.</summary>
public static class LocalAiFailure
{
    public static string Describe(Exception error)
    {
        if (error is HttpRequestException http)
        {
            if (http.StatusCode is { } code)
            {
                var guidance = code switch
                {
                    HttpStatusCode.NotFound => "Check ai status for Installed: True and verify the copied model assets; no model will be downloaded automatically.",
                    HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable => "The local server rejected the request. Check runtime/ai-runtime.log in the copied package. Try ai models force-lite on, then restart GRID; the exact cause still requires the runtime log.",
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "The local endpoint refused access. Check the configured loopback endpoint and any local security software; do not send credentials to another server.",
                    _ => "Capture ai status, ai test and the copied package's runtime/ai-runtime.log to identify the server failure."
                };
                return $"Local AI HTTP {(int)code} ({code}). {guidance}";
            }
            var socket = http.GetBaseException() as SocketException;
            return "Local AI connection failed" + (socket is null ? "" : " (" + socket.SocketErrorCode + ")") +
                ". The bundled loopback server could not be reached. Check the startup AI message and runtime/ai-runtime.log, then restart GRID. No cloud fallback was attempted; deterministic GRID remains available.";
        }
        if (error is OperationCanceledException) return "Local AI request timed out or was cancelled. Check ai status and the bundled runtime log; no host action was inferred.";
        return "Local AI unavailable (" + error.GetType().Name + "); check ai status and the bundled runtime log. Deterministic GRID remains available.";
    }
}
