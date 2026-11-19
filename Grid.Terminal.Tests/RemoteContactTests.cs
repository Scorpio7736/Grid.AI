using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class RemoteContactTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "grid-contact-" + Guid.NewGuid().ToString("N"));
    private string State => Path.Combine(root, "contacts.json");
    public RemoteContactTests() => Directory.CreateDirectory(root);
    public void Dispose() { try { Directory.Delete(root, true); } catch { } }
    private static ContactPolicy Policy(int max = 3) => new("owner-ping", "phone-one", ["MCP", "RELAY"], ["manual-test", "task-failed"], 0, 0, max, DateTimeOffset.UtcNow.AddDays(7), false);
    private RemoteContactService Service() { var service = new RemoteContactService(State); service.AddPolicy(Policy()); service.ControlPolicy("owner-ping", "enable"); return service; }
    private static (GridSimulationEngine Engine, TerminalIdentitySession Identity) Fixture()
    { var identity = new TerminalIdentitySession(() => "owner"); var registry = new TerminalSessionRegistry(); registry.Add(identity); var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); identity.Attach(engine); identity.Login(); return (engine, identity); }
    private static int Port() { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var result = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return result; }
    private static async Task<RemoteResponse> Send(StreamWriter writer, StreamReader reader, RemoteRequest request)
    { await writer.WriteLineAsync(JsonSerializer.Serialize(request)); return JsonSerializer.Deserialize<RemoteResponse>((await reader.ReadLineAsync())!)!; }

    [Fact]
    public void PolicyRequiresFreshAuthorizationAndStateMachineIsIdempotent()
    {
        var service = Service(); var proposed = service.Propose("owner-ping", "MCP", "manual-test", "Foreground test", "event-1");
        Assert.Equal(ContactState.Proposed, proposed.State); Assert.Equal(proposed.Id, service.Propose("owner-ping", "MCP", "manual-test", "ignored", "event-1").Id);
        Assert.Null(service.Poll("phone-one")); service.Authorize(proposed.Id);
        Assert.Null(service.Poll("wrong-device")); var ringing = service.Poll("phone-one")!; Assert.Equal(ContactState.Ringing, ringing.State);
        var connected = service.Accept(ringing.Id, "phone-one"); Assert.Equal(ContactState.Connected, connected.State);
        var ended = service.Finish(ringing.Id, "phone-one"); Assert.Equal(ContactState.Ended, ended.State);
        Assert.Contains(service.Snapshot().Receipts, value => value.State == "Ringing");
        Assert.Contains(service.Snapshot().Receipts, value => value.State == "Connected");
    }

    [Fact]
    public void PolicyEscapeRatePauseAndRevocationFailClosed()
    {
        var service = Service();
        Assert.Throws<UnauthorizedAccessException>(() => service.Propose("owner-ping", "MAXIS", "manual-test", "no", "x"));
        Assert.Throws<UnauthorizedAccessException>(() => service.Propose("owner-ping", "MCP", "invented", "no", "y"));
        for (var i = 0; i < 3; i++) _ = service.Propose("owner-ping", "MCP", "manual-test", "test", "rate-" + i);
        Assert.Throws<UnauthorizedAccessException>(() => service.Propose("owner-ping", "MCP", "manual-test", "test", "rate-4"));
        service.SetPaused(true); Assert.Throws<UnauthorizedAccessException>(() => service.Propose("owner-ping", "MCP", "manual-test", "test", "paused"));
        Assert.All(service.Snapshot().Attempts, value => Assert.Equal(ContactState.Cancelled, value.State));
        service.SetPaused(false); service.ControlPolicy("owner-ping", "disable");
        Assert.Throws<UnauthorizedAccessException>(() => service.Propose("owner-ping", "MCP", "manual-test", "test", "disabled"));
    }

    [Fact]
    public void RestartInterruptsActiveAttemptAndNeverReplaysIt()
    {
        var service = Service(); var attempt = service.Propose("owner-ping", "MCP", "manual-test", "test", "restart"); service.Authorize(attempt.Id);
        var reopened = new RemoteContactService(State); var saved = reopened.Snapshot().Attempts.Single();
        Assert.Equal(ContactState.Interrupted, saved.State); Assert.Null(reopened.Poll("phone-one"));
    }

    [Fact]
    public async Task PairedForegroundClientPollsAcceptsAndEndsTextContact()
    {
        var contacts = Service(); var attempt = contacts.Propose("owner-ping", "MCP", "manual-test", "Hello from GRID", "gateway"); contacts.Authorize(attempt.Id);
        var (engine, _) = Fixture(); await using var gateway = new RemoteTerminalGateway(engine, contacts); var plan = gateway.Plan("loopback", Port()); gateway.Enable(plan, plan.Nonce);
        using var client = new TcpClient(); await client.ConnectAsync(plan.Address, plan.Port); using var reader = new StreamReader(client.GetStream()); using var writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
        Assert.True((await Send(writer, reader, new(1, Guid.NewGuid(), "pair", gateway.CreatePairing(), "phone-one"))).Success);
        var poll = await Send(writer, reader, new(1, Guid.NewGuid(), "contact-poll")); Assert.True(poll.Success); Assert.Contains("Hello from GRID", poll.Text);
        var accept = await Send(writer, reader, new(1, Guid.NewGuid(), "contact-accept", Value: attempt.Id.ToString())); Assert.Contains("Connected", accept.Text);
        var end = await Send(writer, reader, new(1, Guid.NewGuid(), "contact-end", Value: attempt.Id.ToString())); Assert.Contains("Ended", end.Text);
        Assert.Contains("Microphone/audio remain unavailable", end.Text);
    }

    [Fact]
    public async Task ContactCommandsKeepPolicyAndPingApprovalLocal()
    {
        var contacts = new RemoteContactService(State); var (engine, identity) = Fixture(); await using var gateway = new RemoteTerminalGateway(engine, contacts);
        var commands = DefaultCommands.Create(engine); ContactCommands.Register(commands, identity, gateway, contacts, true); commands.UseIdentity(identity);
        var plan = commands.ExecuteTokens(["contact", "policy", "plan", "test", "phone-one", "MCP", "manual-test", "0", "0", "2", "7"]).Text;
        var planId = plan.Split("contact policy add ").Last(); Assert.Contains("added disabled", commands.Execute("contact policy add " + planId).Text);
        Assert.Contains("enable completed", commands.Execute("contact policy enable test").Text);
        var ping = commands.ExecuteTokens(["contact", "ping", "plan", "test", "MCP", "manual-test", "dedupe", "test message"]).Text;
        var id = ping.Split("contact ping approve ").Last(); Assert.Contains("authorized", commands.Execute("contact ping approve " + id).Text);
        Assert.Contains("foreground polling: implemented", commands.Execute("contact doctor").Text);
        Assert.Contains("microphone: never opened", commands.Execute("contact doctor").Text);
    }
}
