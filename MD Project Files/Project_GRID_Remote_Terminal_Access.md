# GRID remote terminal access over private networks

## Status and intent

**Planned — 2026-11-17. No remote listener, Tailscale control, public endpoint or glasses integration is implemented by this document.**

The user wants to reach a running Grid.AI host remotely through the existing terminal experience. The initial transport candidate is Tailscale because the user already operates Tailscale commands and endpoints. A user-authored startup command may be the first manual entry point; later Grid.AI releases should provide reviewed built-in discovery, enable/disable, session and diagnostic controls.

The design must support small text-oriented clients, including a phone or wearable relay such as the user's Even Realities G2 workflow, without treating the wearable as a trusted administrator or claiming a native G2 integration. The authoritative world, AI runtime, files, permissions and task execution remain on the host PC.

## Architecture

Remote access extends the existing attached-terminal boundary rather than starting another Grid.AI process or opening the world database remotely:

```text
remote text client / phone / wearable relay
                    |
          private Tailscale transport
                    |
       versioned GRID remote-session gateway
                    |
      authoritative original Grid.AI host
```

- One host owns the simulation, world lease, persistence, local AI and tool gateway.
- Remote clients exchange bounded versioned requests and terminal responses; they never mount or copy `world.db` for live access.
- The gateway should reuse terminal command metadata, conversation routing, session authorization, cancellation and audit contracts. It must not expose an arbitrary shell, raw named pipe, database port or model server.
- The protocol should be client-agnostic and low-bandwidth, with plain-text/structured-message options, resumable conversation IDs, explicit reconnect state and concise output suitable for narrow displays.
- Loss of the remote connection detaches the view. It does not stop the host, replay commands, approve pending work or create a replacement world.

## Tailscale boundary

Tailscale is an optional transport, not a mandatory Grid.AI runtime dependency. Grid.AI must continue to start and operate locally when Tailscale is missing, stopped, signed out or disconnected.

- Prefer a private tailnet-only endpoint. Do not enable Tailscale Funnel or another public-Internet endpoint in the initial implementation.
- Bind the GRID gateway to loopback by default. A reviewed Tailscale mode may use a selected local Tailscale address or a user-configured `tailscale serve` mapping; never silently bind to `0.0.0.0` or all LAN interfaces.
- Do not install Tailscale, sign into an account, change tailnet ACLs, advertise routes, enable exit-node behavior or modify DNS automatically.
- If Grid.AI invokes the Tailscale CLI later, use a typed adapter with fixed allowlisted arguments and parsed/versioned results. Never execute model-generated or free-form Tailscale commands.
- Read-only discovery may report CLI availability, local tailnet addresses, selected Serve status and endpoint health. It must distinguish host observation from cached configuration.
- A user-authored startup script remains outside GRID's authority. Documentation may show how to point it at the bounded gateway, but GRID must not claim that script is installed, audited or portable.

## Authentication and authority

Tailnet membership is transport-level evidence, not sufficient GRID authentication.

- Require normal Grid.AI human authentication or an explicit short-lived pairing flow in addition to the Tailscale connection.
- Pairing codes/tokens must be random, single-use, short-lived, hidden from logs and invalid after restart or revocation unless a separately reviewed durable-device credential is created.
- Record a stable approved-device identity separately from the tailnet IP, because addresses and devices can change. Show the authenticated GRID identity, remote device label, transport and session expiry.
- Default a new remote client to conversation/read-only capabilities. Any broader capability grant must be explicit, bounded and immediately revocable.
- Keep original-host-only operations local: credential replacement, NGE, transfer/move cleanup, remote-access administration, listener reconfiguration and recovery overrides.
- Consequential file/device/service actions still use their existing typed plans, grants, previews and confirmations. A remote connection never turns conversation text into approval or grants filesystem/device scope.
- Define whether a fully authenticated remote Master Control session may approve ordinary existing tasks during implementation. It must never inherit original-host-only controls, and sensitive actions may require a contemporaneous local confirmation.

## Planned terminal controls

Names remain subject to implementation review, but the user-facing workflow should be discoverable:

```text
remote status
remote options
remote plan tailscale [port]
remote enable <review-id>
remote disable
remote sessions
remote revoke <session-or-device>
remote pairing create
remote pairing cancel
remote doctor
```

`remote plan` must show the resolved bind address, port, transport, authentication mode, allowed capabilities, lifetime, firewall/listener implications and rollback before anything listens. `remote enable` consumes that exact review. Status must identify private/listening/offline/stale states without printing secrets. Disable/revoke must stop new requests promptly, invalidate pairing material and preserve an audit receipt.

## Persistence, portability and recovery

- Store non-secret remote configuration and audit locally with the authoritative host. Store private keys/tokens using an appropriate host secret store, never in command arguments, transcripts or portable world data.
- Copy/transfer must disable listeners, discard active sessions and machine-bound endpoint credentials, and require fresh review on the destination device.
- Startup should remain local-only unless the user explicitly enables persistent remote access. A saved preference still requires current identity, endpoint, Tailscale and port validation on every start.
- Apply bounded connections, request sizes, output sizes, idle/session lifetimes, authentication attempts and per-client rate limits. One slow client must not block the simulation or other terminal sessions.
- On sleep/wake, network changes, Tailscale restart or host shutdown, reject stale requests and require a clean reconnect. Never replay an unconfirmed request after recovery.

## Acceptance and security tests

Before claiming remote access is available:

1. Verify private tailnet access from a second approved device and refusal from a non-tailnet/LAN/public route.
2. Verify Tailscale missing, signed out, stopped, address-changed and disconnected behavior while local Grid.AI remains usable.
3. Verify wrong/expired/reused pairing material, credential failure, brute-force throttling, logout, device revocation and host-side emergency disable.
4. Verify remote sessions cannot invoke original-host-only commands or escape typed file/device/task scopes.
5. Verify exact approval behavior for ordinary tasks and prove that conversation text, reconnects and duplicated messages cannot approve or replay work.
6. Verify multiple clients, cancellation, idle expiry, slow clients, bounded output, host restart, sleep/wake and abrupt network loss.
7. Verify copy/transfer starts remote-disabled with no reusable session or machine credential.
8. Run a narrow text-client exercise representative of a phone/wearable relay, including readable status, conversation continuation, explicit errors and safe disconnection. This is protocol acceptance, not a claim of native Even Realities support.
9. Record endpoint exposure with OS/Tailscale inspection and confirm no public Funnel, broad LAN listener, raw model port or database port is reachable.

Remote access is an optional connected capability and cannot satisfy or weaken Grid.AI's mandatory offline/local acceptance. Local terminal use must remain fully functional when every network interface is unavailable.

Agent-initiated pings and accepted voice sessions are a separate layer above this gateway. See [Remote Contact and Voice Calls](Project_GRID_Remote_Contact_and_Voice_Calls.md) and checklist 6.36B; enabling remote terminal access alone must not enable calls, background notifications or microphone access.
