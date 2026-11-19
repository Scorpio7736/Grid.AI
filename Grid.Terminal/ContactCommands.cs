using Grid.Simulation;

namespace Grid.Terminal;

public static class ContactCommands
{
    public static void Register(CommandDispatcher commands, TerminalIdentitySession identity, RemoteTerminalGateway gateway,
        RemoteContactService contacts, bool interactive)
    {
        (Guid Id, ContactPolicy Policy, long Generation, DateTimeOffset Expires)? pending = null;
        identity.Changed += () => pending = null;
        commands.Register(new("contact", "contact <status|policies|policy|ping|attempts|inspect|cancel|pause|resume|doctor> ...",
            "Manage reviewed foreground text pings through paired private remote clients.", args =>
        {
            if (!interactive || !identity.IsMaster || identity.IsAttachment) return new("Contact administration requires interactive original-host Master Control.");
            try
            {
                var state = contacts.Snapshot();
                if (args.Count == 0 || args.SequenceEqual(["status"])) return new($"Remote contact: {(state.Paused ? "paused" : "available when gateway/client are foreground")} | gateway {(gateway.Enabled ? "listening" : "disabled")} | policies {state.Policies.Length} | active attempts {state.Attempts.Count(value => value.State is ContactState.Authorized or ContactState.Delivering or ContactState.Ringing or ContactState.Connected)}\nChannel: private text ping only. Background wake-up, microphone, audio, PSTN/SMS and emergency calling are unavailable.");
                if (args.SequenceEqual(["policies"])) return new(string.Join("\n", state.Policies.Select(value => $"{value.Id} | device {value.Device} | agents {string.Join(',', value.Agents)} | triggers {string.Join(',', value.Triggers)} | {(value.Enabled ? "enabled" : "disabled")} | expires {value.ExpiresUtc:O}")));
                if (args.Count == 10 && args[0] == "policy" && args[1] == "plan")
                {
                    var policy = new ContactPolicy(args[2], args[3], args[4].Split(',', StringSplitOptions.RemoveEmptyEntries).Select(value => value.ToUpperInvariant()).ToArray(), args[5].Split(',', StringSplitOptions.RemoveEmptyEntries), int.Parse(args[6]), int.Parse(args[7]), int.Parse(args[8]), DateTimeOffset.UtcNow.AddDays(int.Parse(args[9])), false);
                    pending = (Guid.NewGuid(), policy, identity.Generation, DateTimeOffset.UtcNow.AddMinutes(2));
                    return new($"PLAN ONLY - no standing contact authority.\nPolicy: {policy.Id}\nDevice identity: {policy.Device} (not an IP)\nAgents: {string.Join(',', policy.Agents)}\nTriggers: {string.Join(',', policy.Triggers)}\nQuiet UTC hours: {policy.QuietStartUtcHour}:00 to {policy.QuietEndUtcHour}:00\nMaximum/day: {policy.MaxPerDay}\nExpires: {policy.ExpiresUtc:O}\nChannel: foreground text ping only; every ping still needs fresh local approval.\nAdd disabled policy: contact policy add {pending.Value.Id}");
                }
                if (args.Count == 3 && args[0] == "policy" && args[1] == "add")
                {
                    var plan = pending; pending = null; if (plan is not { } value || !Guid.TryParse(args[2], out var id) || id != value.Id || value.Generation != identity.Generation || DateTimeOffset.UtcNow >= value.Expires) return new("Policy plan missing, expired or invalidated. Nothing added.");
                    contacts.AddPolicy(value.Policy); return new("Contact policy added disabled. Review with contact policies, then explicitly enable it.");
                }
                if (args.Count == 3 && args[0] == "policy" && args[1] is "enable" or "disable" or "remove") { contacts.ControlPolicy(args[2], args[1]); return new("Contact policy " + args[1] + " completed."); }
                if (args.Count >= 7 && args[0] == "ping" && args[1] == "plan")
                {
                    var attempt = contacts.Propose(args[2], args[3], args[4], args[5], string.Join(' ', args.Skip(6)));
                    return new($"PING PREVIEW - not authorized or delivered.\nAttempt: {attempt.Id}\nPolicy: {attempt.Policy}\nDevice: {attempt.Device}\nAgent: {attempt.Agent}\nTrigger: {attempt.Trigger}\nReason: {attempt.Reason}\nApprove this exact attempt locally: contact ping approve {attempt.Id}");
                }
                if (args.Count == 4 && args[0] == "ping" && args[1] == "approve" && Guid.TryParse(args[2], out _)) return new("Usage: contact ping approve <attempt-id>");
                if (args.Count == 3 && args[0] == "ping" && args[1] == "approve" && Guid.TryParse(args[2], out var attemptId)) { var attempt = contacts.Authorize(attemptId); return new($"Ping {attempt.Id} authorized. A paired foreground client for {attempt.Device} may retrieve it once; ringing does not mean read."); }
                if (args.SequenceEqual(["attempts"])) return new(string.Join("\n", state.Attempts.Select(value => $"{value.Id} | {value.Policy} | {value.Agent} | {value.State} | {value.UpdatedUtc:O}")));
                if (args.Count == 2 && args[0] == "inspect" && Guid.TryParse(args[1], out var inspect)) { var value = state.Attempts.SingleOrDefault(item => item.Id == inspect) ?? throw new ArgumentException("Unknown attempt."); return new($"{value.Id} | policy {value.Policy} | device {value.Device} | agent {value.Agent} | trigger {value.Trigger} | reason {value.Reason} | state {value.State} | created {value.CreatedUtc:O} | updated {value.UpdatedUtc:O}"); }
                if (args.Count == 2 && args[0] == "cancel" && Guid.TryParse(args[1], out var cancel)) { contacts.Cancel(cancel); return new("Contact attempt cancelled if active."); }
                if (args.SequenceEqual(["pause"])) { contacts.SetPaused(true); return new("Remote contact paused; active attempts cancelled. Gateway status access is unchanged."); }
                if (args.SequenceEqual(["resume"])) { contacts.SetPaused(false); return new("Remote contact resumed. No attempt was created or replayed."); }
                if (args.SequenceEqual(["doctor"])) return new($"Gateway: {(gateway.Enabled ? "listening" : "disabled")} | active paired sessions: {gateway.Sessions.Length} | foreground polling: implemented | background wake-up: unavailable | audio/WebRTC: not selected | microphone: never opened | local STT/TTS: not connected to remote contact | text fallback: implemented | PSTN/SMS/emergency calls: unavailable");
                return new("Use contact status/policies; policy plan/add/enable/disable/remove; ping plan/approve; attempts/inspect/cancel; pause/resume; doctor.");
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or UnauthorizedAccessException or IOException or InvalidDataException)
            { return new("Contact request refused: " + TerminalTable.Safe(error.Message)); }
        }) { Capability = "world.admin" });
    }
}
