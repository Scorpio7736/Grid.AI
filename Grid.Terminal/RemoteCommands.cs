using Grid.Simulation;
using System.Net.Sockets;

namespace Grid.Terminal;

public static class RemoteCommands
{
    public static void Register(CommandDispatcher commands, GridSimulationEngine engine, TerminalIdentitySession identity,
        RemoteTerminalGateway gateway, bool interactive)
    {
        RemoteEndpointPlan? pending = null; identity.Changed += () => pending = null;
        commands.Register(new("remote", "remote <status|options|plan|enable|disable|sessions|revoke|pairing|doctor|audit> ...",
            "Manage the bounded loopback/Tailscale read-only remote gateway.", args =>
        {
            if (!interactive || !identity.IsMaster || identity.IsAttachment) return new("Remote administration requires interactive original-host Master Control.");
            try
            {
                if (args.Count == 0 || args.SequenceEqual(["status"])) return new(gateway.Enabled
                    ? $"Remote gateway: listening | {gateway.Endpoint} | sessions {gateway.Sessions.Length} | read-only v1"
                    : "Remote gateway: disabled | local GRID remains available | no persistent startup preference");
                if (args.SequenceEqual(["options"])) return new("Modes: loopback (default test/local relay), tailscale (private address only).\n" +
                    "Tailscale addresses observed: " + (RemoteTerminalGateway.TailscaleAddresses().Length == 0 ? "none" : string.Join(", ", RemoteTerminalGateway.TailscaleAddresses().Select(value => value.ToString()))) +
                    "\nRefused: 0.0.0.0, ::, LAN/public addresses, Funnel, shell, database, model endpoint.");
                if (args.Count is 2 or 3 && args[0] == "plan")
                {
                    var port = args.Count == 3 ? int.Parse(args[2]) : 7736; pending = gateway.Plan(args[1].ToLowerInvariant(), port);
                    return new($"PLAN ONLY - listener disabled.\nOperation: {pending.Id}\nBind: {pending.Address}:{pending.Port}\nTransport: {pending.Mode}\nProtocol: GRID remote v1, 4 KiB requests, 2 KiB replies, 20 requests/session, five-minute session\nAuthentication: one-time two-minute GRID pairing after enable\nCapabilities: help, read-only world status, disconnect\nNo firewall, Tailscale, Funnel, DNS, LAN, shell, database or model configuration will be changed.\nNonce: {pending.Nonce}\nEnable exact plan: remote enable {pending.Id} {pending.Nonce}");
                }
                if (args.Count == 3 && args[0] == "enable" && pending is { } plan && Guid.TryParse(args[1], out var id) && id == plan.Id)
                { pending = null; gateway.Enable(plan, args[2]); return new("Remote gateway enabled for this process only. Create a single-use code with remote pairing create."); }
                if (args.SequenceEqual(["disable"])) { gateway.DisableAsync().GetAwaiter().GetResult(); pending = null; return new("Remote gateway disabled; sessions and pairing material revoked."); }
                if (args.SequenceEqual(["pairing", "create"])) return new("Single-use pairing code (two minutes; do not paste into chat/logs): " + gateway.CreatePairing());
                if (args.SequenceEqual(["pairing", "cancel"])) { gateway.Revoke(); return new("Pairing material and active sessions revoked."); }
                if (args.SequenceEqual(["sessions"])) return new(gateway.Sessions.Length == 0 ? "No active remote sessions." : string.Join("\n", gateway.Sessions.Select(value => $"{value.Id} | {value.Device} | {value.Transport} | expires {value.ExpiresUtc:O} | read-only")));
                if (args.Count == 2 && args[0] == "revoke" && Guid.TryParse(args[1], out var session)) { gateway.Revoke(session); return new("Remote session revoked if it existed."); }
                if (args.SequenceEqual(["revoke", "all"])) { gateway.Revoke(); return new("All remote sessions and pairing material revoked."); }
                if (args.SequenceEqual(["doctor"])) return new("Protocol: v1 | gateway: " + (gateway.Enabled ? "listening" : "disabled") +
                    " | Tailscale local addresses: " + RemoteTerminalGateway.TailscaleAddresses().Length +
                    " | public/LAN bind: refused by policy | durable device credential: none | conversation/actions: unavailable");
                if (args.SequenceEqual(["audit"])) return new(string.Join("\n", gateway.Audit.Select(value => $"{value.AtUtc:O} | {value.Action} | {value.Outcome}")));
                return new("Use remote status/options; plan <loopback|tailscale> [port]; enable <operation-id> <nonce>; pairing create/cancel; sessions; revoke <id|all>; disable; doctor; audit.");
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or UnauthorizedAccessException or SocketException)
            { return new("Remote request refused: " + TerminalTable.Safe(error.Message)); }
        }) { Capability = "world.admin" });
    }
}
