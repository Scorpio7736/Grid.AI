using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Grid.Core;
using Grid.Simulation;
using Xunit;

namespace Grid.Terminal.Tests;

public sealed class RemoteTerminalTests
{
    private static (GridSimulationEngine Engine, TerminalIdentitySession Identity) Fixture()
    {
        var identity = new TerminalIdentitySession(() => "owner"); var registry = new TerminalSessionRegistry(); registry.Add(identity);
        var engine = new GridSimulationEngine(GridWorldFactory.CreateGenesis(), editAuthorizer: registry); identity.Attach(engine); identity.Login();
        return (engine, identity);
    }
    private static int Port()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port;
    }
    private static async Task<RemoteResponse> Send(StreamWriter writer, StreamReader reader, RemoteRequest request)
    { await writer.WriteLineAsync(JsonSerializer.Serialize(request)); return JsonSerializer.Deserialize<RemoteResponse>((await reader.ReadLineAsync())!)!; }

    [Fact]
    public async Task ReviewedLoopbackPairingCreatesBoundedReadOnlySession()
    {
        var (engine, _) = Fixture(); await using var gateway = new RemoteTerminalGateway(engine);
        var plan = gateway.Plan("loopback", Port());
        Assert.Throws<UnauthorizedAccessException>(() => gateway.Enable(plan, "wrong"));
        gateway.Enable(plan, plan.Nonce); var code = gateway.CreatePairing();
        using var client = new TcpClient(); await client.ConnectAsync(plan.Address, plan.Port);
        using var reader = new StreamReader(client.GetStream(), Encoding.UTF8); using var writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
        var pair = await Send(writer, reader, new(1, Guid.NewGuid(), "pair", code, "narrow-client"));
        Assert.True(pair.Success); Assert.Contains("read-only", pair.Text);
        var status = await Send(writer, reader, new(1, Guid.NewGuid(), "status")); Assert.True(status.Success); Assert.Contains("tick", status.Text);
        var denied = await Send(writer, reader, new(1, Guid.NewGuid(), "nge")); Assert.False(denied.Success); Assert.Contains("not available", denied.Text);
        var close = await Send(writer, reader, new(1, Guid.NewGuid(), "disconnect")); Assert.True(close.Success);
    }

    [Fact]
    public async Task PairingIsSingleUseAndReplayIsRejected()
    {
        var (engine, _) = Fixture(); await using var gateway = new RemoteTerminalGateway(engine); var plan = gateway.Plan("loopback", Port()); gateway.Enable(plan, plan.Nonce);
        var code = gateway.CreatePairing();
        async Task<RemoteResponse> Pair()
        {
            using var client = new TcpClient(); await client.ConnectAsync(plan.Address, plan.Port);
            using var reader = new StreamReader(client.GetStream()); using var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
            return await Send(writer, reader, new(1, Guid.NewGuid(), "pair", code, "device"));
        }
        Assert.True((await Pair()).Success); Assert.False((await Pair()).Success);
    }

    [Fact]
    public async Task DuplicateRequestAndHostRevocationFailClosed()
    {
        var (engine, _) = Fixture(); await using var gateway = new RemoteTerminalGateway(engine); var plan = gateway.Plan("loopback", Port()); gateway.Enable(plan, plan.Nonce);
        using var client = new TcpClient(); await client.ConnectAsync(plan.Address, plan.Port);
        using var reader = new StreamReader(client.GetStream()); using var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
        Assert.True((await Send(writer, reader, new(1, Guid.NewGuid(), "pair", gateway.CreatePairing(), "device"))).Success);
        var id = Guid.NewGuid(); Assert.True((await Send(writer, reader, new(1, id, "help"))).Success);
        Assert.False((await Send(writer, reader, new(1, id, "help"))).Success);
        gateway.Revoke(); Assert.False((await Send(writer, reader, new(1, Guid.NewGuid(), "status"))).Success);
    }

    [Fact]
    public async Task DisableStopsListenerClearsSessionsAndDoesNotAffectLocalWorld()
    {
        var (engine, _) = Fixture(); await using var gateway = new RemoteTerminalGateway(engine); var plan = gateway.Plan("loopback", Port()); gateway.Enable(plan, plan.Nonce);
        gateway.Revoke(); await gateway.DisableAsync();
        Assert.False(gateway.Enabled); Assert.Empty(gateway.Sessions); Assert.Equal(0, engine.TickCount);
        using var client = new TcpClient(); await Assert.ThrowsAnyAsync<SocketException>(() => client.ConnectAsync(plan.Address, plan.Port));
    }

    [Fact]
    public async Task AdministrationRequiresInteractiveMasterAndRefusesBroadModes()
    {
        var (engine, identity) = Fixture(); await using var gateway = new RemoteTerminalGateway(engine);
        Assert.Throws<ArgumentException>(() => gateway.Plan("lan", Port()));
        var commands = DefaultCommands.Create(engine); RemoteCommands.Register(commands, engine, identity, gateway, interactive: false); commands.UseIdentity(identity);
        Assert.Contains("interactive original-host", commands.Execute("remote status").Text);
        var interactive = DefaultCommands.Create(engine); RemoteCommands.Register(interactive, engine, identity, gateway, interactive: true); interactive.UseIdentity(identity);
        Assert.Contains("disabled", interactive.Execute("remote status").Text);
        Assert.Contains("Refused", interactive.Execute("remote options").Text);
    }
}
