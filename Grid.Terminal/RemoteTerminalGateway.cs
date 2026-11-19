using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Grid.Simulation;

namespace Grid.Terminal;

public sealed record RemoteEndpointPlan(Guid Id, string Mode, IPAddress Address, int Port, string Nonce, DateTimeOffset ExpiresUtc);
public sealed record RemoteSessionInfo(Guid Id, string Device, string Transport, DateTimeOffset ConnectedUtc, DateTimeOffset ExpiresUtc);
public sealed record RemoteAudit(DateTimeOffset AtUtc, string Action, string Outcome);
public sealed record RemoteRequest(int Version, Guid RequestId, string Action, string? PairingCode = null, string? Device = null, string? Value = null);
public sealed record RemoteResponse(int Version, Guid RequestId, bool Success, string Text);

public sealed class RemoteTerminalGateway(GridSimulationEngine engine, RemoteContactService? contacts = null) : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly ConcurrentDictionary<Guid, RemoteSessionInfo> sessions = new();
    private readonly List<RemoteAudit> audit = [];
    private TcpListener? listener; private CancellationTokenSource? stop; private Task? worker;
    private string? pairingHash; private DateTimeOffset pairingExpiry;
    private int pairingFailures; private DateTimeOffset pairingFailureWindow;
    public bool Enabled { get { lock (gate) return listener is not null; } }
    public IPEndPoint? Endpoint { get { lock (gate) return listener?.LocalEndpoint as IPEndPoint; } }
    public RemoteSessionInfo[] Sessions => sessions.Values.OrderBy(value => value.ConnectedUtc).ToArray();
    public RemoteAudit[] Audit { get { lock (gate) return audit.ToArray(); } }
    public static IPAddress[] TailscaleAddresses() => NetworkInterface.GetAllNetworkInterfaces()
        .SelectMany(item => item.GetIPProperties().UnicastAddresses).Select(item => item.Address)
        .Where(IsTailscale).Distinct().ToArray();
    public static bool IsTailscale(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
        { var bytes = address.GetAddressBytes(); return bytes[0] == 100 && bytes[1] is >= 64 and <= 127; }
        return address.AddressFamily == AddressFamily.InterNetworkV6 && address.ToString().StartsWith("fd7a:115c:a1e0:", StringComparison.OrdinalIgnoreCase);
    }
    public RemoteEndpointPlan Plan(string mode, int port)
    {
        if (port is < 1024 or > 65535) throw new ArgumentException("Port must be 1024..65535.");
        IPAddress address = mode switch
        {
            "loopback" => IPAddress.Loopback,
            "tailscale" => TailscaleAddresses().SingleOrDefault() ?? throw new InvalidOperationException("No local Tailscale address is available."),
            _ => throw new ArgumentException("Use loopback or tailscale. Broad LAN/public binds are refused.")
        };
        return new(Guid.NewGuid(), mode, address, port, Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), DateTimeOffset.UtcNow.AddMinutes(2));
    }
    public void Enable(RemoteEndpointPlan plan, string nonce)
    {
        lock (gate)
        {
            if (listener is not null) throw new InvalidOperationException("Remote gateway is already enabled.");
            if (DateTimeOffset.UtcNow >= plan.ExpiresUtc || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(plan.Nonce), Encoding.UTF8.GetBytes(nonce)))
                throw new UnauthorizedAccessException("Remote plan confirmation is invalid or expired.");
            if (!IPAddress.IsLoopback(plan.Address) && !IsTailscale(plan.Address)) throw new UnauthorizedAccessException("Only loopback or a local Tailscale address may be bound.");
            listener = new(plan.Address, plan.Port); listener.Start(8); stop = new(); worker = AcceptAsync(listener, stop.Token); Record("enable", plan.Mode);
        }
    }
    public string CreatePairing()
    {
        lock (gate)
        {
            if (listener is null) throw new InvalidOperationException("Enable the gateway first.");
            var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)); pairingHash = Hash(code); pairingExpiry = DateTimeOffset.UtcNow.AddMinutes(2);
            Record("pairing", "created"); return code;
        }
    }
    public void Revoke(Guid? id = null)
    {
        if (id is null) { foreach (var key in sessions.Keys) sessions.TryRemove(key, out _); lock (gate) { pairingHash = null; Record("revoke", "all"); } }
        else { sessions.TryRemove(id.Value, out _); lock (gate) Record("revoke", "session"); }
    }
    public async Task DisableAsync()
    {
        Task? pending;
        lock (gate) { stop?.Cancel(); listener?.Stop(); listener = null; pairingHash = null; sessions.Clear(); pending = worker; Record("disable", "completed"); }
        if (pending is not null) try { await pending.ConfigureAwait(false); } catch (OperationCanceledException) { } catch (SocketException) { }
    }
    private async Task AcceptAsync(TcpListener server, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client; try { client = await server.AcceptTcpClientAsync(token).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
            _ = HandleAsync(client, token);
        }
    }
    private async Task HandleAsync(TcpClient client, CancellationToken hostToken)
    {
        using (client)
        {
            client.ReceiveTimeout = 15000; client.SendTimeout = 15000;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(hostToken); timeout.CancelAfter(TimeSpan.FromMinutes(5));
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8, false, 4096, leaveOpen: true);
            using var writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };
            Guid sessionId = Guid.Empty; var requests = new HashSet<Guid>();
            for (var count = 0; count < 20 && !timeout.IsCancellationRequested; count++)
            {
                var line = await reader.ReadLineAsync(timeout.Token).ConfigureAwait(false); if (line is null) break;
                if (Encoding.UTF8.GetByteCount(line) > 4096) { await Reply(writer, Guid.Empty, false, "Request too large."); break; }
                RemoteRequest? request; try { request = JsonSerializer.Deserialize<RemoteRequest>(line); } catch { await Reply(writer, Guid.Empty, false, "Invalid request."); break; }
                if (request is null || request.Version != 1 || request.RequestId == Guid.Empty || !requests.Add(request.RequestId)) { await Reply(writer, request?.RequestId ?? Guid.Empty, false, "Invalid or replayed request."); continue; }
                if (sessionId == Guid.Empty)
                {
                    if (sessions.Count >= 8 || request.Action != "pair" || !UsePairing(request.PairingCode) || string.IsNullOrWhiteSpace(request.Device) || request.Device.Length > 64)
                    { await Reply(writer, request.RequestId, false, "Pairing refused."); break; }
                    sessionId = Guid.NewGuid(); sessions[sessionId] = new(sessionId, request.Device, IPAddress.IsLoopback(((IPEndPoint)client.Client.LocalEndPoint!).Address) ? "loopback" : "tailscale", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5));
                    await Reply(writer, request.RequestId, true, $"Paired read-only session {sessionId}. Allowed: help, status, contact-poll/accept/decline/end, disconnect. No microphone or command authority."); continue;
                }
                if (!sessions.ContainsKey(sessionId)) { await Reply(writer, request.RequestId, false, "Session revoked."); break; }
                if (request.Action == "disconnect") { await Reply(writer, request.RequestId, true, "Disconnected."); break; }
                if (request.Action == "help") { await Reply(writer, request.RequestId, true, "GRID remote v1 | help | status | contact-poll/accept/decline/end | disconnect | text only; no shell, approvals, files, microphone or administration"); continue; }
                if (request.Action == "status") { var state = engine.GetSnapshot(); await Reply(writer, request.RequestId, true, $"GRID {(state.IsRunning ? "running" : "paused")} | tick {state.TickCount} | districts {state.Districts.Count} | programs {state.Programs.Count}"); continue; }
                var device = sessions[sessionId].Device;
                if (request.Action == "contact-poll" && contacts is not null) { var attempt = contacts.Poll(device); await Reply(writer, request.RequestId, true, attempt is null ? "No pending contact." : $"CONTACT {attempt.Id} | {attempt.Agent} | {attempt.Trigger} | {attempt.Reason} | state {attempt.State} | text-only foreground invitation"); continue; }
                if (request.Action is "contact-accept" or "contact-decline" or "contact-end" && contacts is not null && Guid.TryParse(request.Value, out var contactId))
                { var attempt = request.Action == "contact-accept" ? contacts.Accept(contactId, device) : contacts.Finish(contactId, device, request.Action == "contact-decline"); await Reply(writer, request.RequestId, true, $"Contact {attempt.Id} state {attempt.State}. Microphone/audio remain unavailable."); continue; }
                await Reply(writer, request.RequestId, false, "Action is not available to remote read-only sessions.");
            }
            if (sessionId != Guid.Empty) sessions.TryRemove(sessionId, out _);
        }
    }
    private bool UsePairing(string? code)
    {
        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            if (now >= pairingFailureWindow.AddMinutes(1)) { pairingFailureWindow = now; pairingFailures = 0; }
            if (pairingFailures >= 10) return false;
            var valid = pairingHash is not null && DateTimeOffset.UtcNow < pairingExpiry && code is not null &&
                CryptographicOperations.FixedTimeEquals(Convert.FromHexString(pairingHash), Convert.FromHexString(Hash(code)));
            if (!valid) pairingFailures++;
            pairingHash = null; return valid;
        }
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static Task Reply(StreamWriter writer, Guid id, bool success, string text) => writer.WriteLineAsync(JsonSerializer.Serialize(new RemoteResponse(1, id, success, text[..Math.Min(2048, text.Length)])));
    private void Record(string action, string outcome) { audit.Add(new(DateTimeOffset.UtcNow, action, outcome)); if (audit.Count > 100) audit.RemoveAt(0); }
    public async ValueTask DisposeAsync() => await DisableAsync();
}
