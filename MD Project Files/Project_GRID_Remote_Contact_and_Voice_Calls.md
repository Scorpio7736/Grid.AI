# GRID remote contact, ping and voice-call planning

## Status and goal

**Foreground text-contact foundation implemented — 2026-11-19.** GRID now supports user-owned contact policies, fresh locally approved text-ping attempts, durable idempotent states and retrieval/accept/end through an authenticated foreground remote client. No phone notification service, background wake-up, audio/WebRTC, remote microphone, PSTN/SMS or agent-autonomous call is implemented.

The user wants Grid.AI—primarily MCP—to be able to ping the user's phone or invite the user into a voice conversation over Wi-Fi/private Internet data. Once remote agent voice is ready, an accepted call should carry a live conversation between the user and MCP or another clearly identified agent. The first target is contact with the user's own approved device, not arbitrary third-party calling.

The planned [challenge-code authentication](Project_GRID_Challenge_Code_Authentication.md) adds a separate knowledge-factor check for identifying the user during an authenticated call. Agents may speak host-selected prompts but never receive stored responses or per-question correctness. A passed challenge identifies knowledge of enrolled secrets for one bounded purpose; it is not a biometric voice signature and cannot approve spoken commands or widen call authority.

This should use the optional private remote-session gateway and Tailscale plan in [Remote Terminal Access](Project_GRID_Remote_Terminal_Access.md). It must not weaken Grid.AI's local/offline requirements: local operation remains available with networking disabled, while remote contact is explicitly unavailable.

## Cost and network boundary

The intended path is application data over Wi-Fi or an existing Internet connection, not carrier voice minutes, SMS or the public switched telephone network. Grid.AI cannot promise that a call is free: the user may still incur ISP, Wi-Fi, mobile-data, roaming, Tailscale relay/plan or optional service-provider charges. Status and setup must say which transport/provider is active and whether a component is known to require a paid account.

- Initial scope: private calls to an authenticated GRID companion/client owned by the user.
- Out of initial scope: dialing telephone numbers, receiving a public phone number, SMS/MMS, emergency calling, 911/112, voicemail-provider integration and calls to other people.
- SIP/PSTN/hosted VoIP can be evaluated later as an optional connector, with separate provider, pricing, consent, credential and legal review. It must never be silently substituted for the private data path.
- Tailscale provides private reachability, not call signaling, ringing UI, background execution, audio codecs or guaranteed cost-free transport.

## Roles and authority

MCP is the primary contact coordinator, but it does not receive unrestricted permission to interrupt or call the user.

- **MCP:** evaluates an event against the user's saved contact policy, chooses the relevant verified briefing and proposes/initiates an allowed contact request. MCP cannot expand its policy or invent urgency.
- **RELAY:** owns endpoint delivery, ping/call state, retries, receipts, missed-call records and the bounded audio/session adapter. This extends RELAY's existing local inbox and explicit speech responsibilities.
- **Speaking agent:** MCP is the default caller. Another registered agent may speak only when the policy/request names it; caller identity must be visible and spoken at connection start. Inviting another agent does not give it MCP or RELAY authority.
- **Master Control:** creates, reviews, enables, pauses and revokes contact policies and paired devices. No conversation or model output can create standing contact permission.

New installations and newly paired devices start with remote contact disabled. A manual user test call is different from proactive agent contact. Proactive contact requires an explicit saved policy with allowed agents, event types, urgency levels, endpoint, time window, rate limits and expiration.

### Provisional primary/local versus dynamic authority model

The user is considering two contact-authority classes. Names remain open until the broader agent model is finalized:

| Provisional class | Intended meaning | Call authority |
| --- | --- | --- |
| **Primary/local authority** | A persistent user-designated coordinator on the authoritative local host, initially expected to be MCP (the “head” or master contact coordinator) | May initiate pings/call invitations only under an explicit standing user-created policy with fixed triggers, endpoint, hours, rate limits and expiry. It cannot invent new reasons, recipients or urgency. |
| **Dynamic/delegated agent** | A temporary, invited, generated, specialist or context-specific participant whose presence or purpose may change | Has no standing outbound-contact authority. It must request an exact per-call approval naming the endpoint, speaking identity, reason and lifetime. |

“Primary” must not mean unrestricted. Designating MCP as the primary contact coordinator does not grant it original-host controls, arbitrary calling, policy editing or emergency authority. Only human Master Control creates or widens a standing policy. Removing the primary designation, changing devices, moving the world or revoking the policy ends that authority immediately.

A dynamic agent may ask MCP to prepare a contact proposal, but neither agent may approve it. If approved, RELAY places the call and identifies the dynamic agent before audio begins. The grant applies only to that attempt; retry, recipient change, another speaking agent or a later call requires a new approval. Dynamic-agent content cannot silently become the displayed call reason—MCP/RELAY must use a bounded, attributable summary and label unverified claims.

The implementation should use stable capability records rather than trusting labels such as “master,” “head,” “local” or “dynamic.” Tests must cover forged role names, agent replacement, invitation/removal, policy expiry, identity migration and attempts to convert a one-call approval into standing permission.

## Contact policy

A policy should support:

- allowed channels: silent ping, visible/audible ping, call invitation;
- allowed initiators and speaking agents, initially MCP/RELAY only unless expanded explicitly;
- exact triggers, such as a named scheduled job failure, approved long task completion or a user-created reminder;
- quiet hours, time zone, temporary do-not-disturb, maximum attempts per event/day and retry spacing;
- urgency labels defined by the user, not inferred solely from emotional model language;
- preferred paired endpoint and fallback order;
- expiry, enable/disable and immediate revoke;
- whether a call needs a fresh local approval or is pre-authorized for that narrow trigger.

The default is no proactive ping or call. General simulation events, ordinary agent thoughts, background conversation suggestions and unverified model claims cannot trigger contact. Emergency-service or personal-safety monitoring is not claimed.

## Proposed call protocol

Each contact attempt has a unique immutable ID and an idempotent state machine:

```text
Proposed -> Authorized -> Delivering -> Ringing
                                  |       |
                                  |       +-> Declined / Missed / Expired
                                  +-> Failed
Ringing -> Accepted -> NegotiatingAudio -> Connected -> Ended
                                      |                |
                                      +-> Failed       +-> Interrupted
```

- Signaling travels through the authenticated remote-session gateway; it never exposes the model or database port.
- The client displays caller agent, verified reason, host identity and requested media before answer. Decline sends no microphone audio.
- Acceptance creates a new short-lived media/session authorization bound to the contact ID, user, device, agent and host generation.
- Duplicate/reordered messages are idempotent. Reconnect cannot answer, redial, approve work or reopen the microphone automatically.
- Either side may end the call. Host logout, remote disable, device revoke, transfer, NGE or session expiry terminates it.
- Pings and calls produce delivery receipts distinct from human acknowledgement. Delivered/ringing does not mean read/heard; connected does not prove comprehension.

Implementation should evaluate WebRTC signaling/media first because it provides established encrypted real-time audio and NAT-handling primitives. A simpler bounded audio stream over the private gateway may be evaluated for a tailnet-only prototype. The final choice requires measured latency, reconnect, codec, library, licensing, firewall and mobile-background tests; this document does not select a library.

## Mobile wake-up and ringing constraint

A Tailscale connection alone may not wake an iOS/Android application suspended by the operating system. Reliable background ringing may require one of these explicitly selected modes:

1. a foreground companion app or user-enabled background service maintaining the private GRID connection;
2. bounded polling while the client is allowed to run;
3. optional APNs/FCM or another push provider carrying only an opaque wake-up/contact ID, followed by authenticated private retrieval;
4. no background wake-up, with calls available only while the client is open.

Mode 3 introduces a cloud dependency, provider credentials, metadata disclosure and possible cost; it must be optional and cannot count as offline capability. Notification contents should remain local/private wherever possible. The UI must state whether the phone can currently ring in the background rather than promising unreachable delivery.

## Voice conversation path

After answer:

- the approved phone/client microphone supplies bounded audio only for the active call;
- the host performs local speech recognition where supported and sends reviewed/final transcripts into the selected conversation using the existing conversation authorization boundary;
- the selected agent generates its response through the normal local inference queue;
- the host's configured agent voice produces audio returned to the client, with readable text available as a fallback;
- single-speaker arbitration, cancellation and resource limits prevent overlapping agents or unbounded inference;
- task commands spoken during a call remain conversation input, never direct terminal execution or approval.

Raw audio recording and retention are off by default. Store only explicitly permitted transcripts and bounded call receipts. If optional recording is ever added, require a visible per-call consent state and retention/deletion controls. Never activate a microphone before answer or after hang-up.

## Planned terminal controls

Names are provisional:

```text
contact status
contact endpoints
contact pair
contact policies
contact policy plan ...
contact policy enable <review-id>
contact pause
contact resume
contact ping test <endpoint>
contact call test <endpoint> MCP
contact attempts
contact inspect <attempt-id>
contact cancel <attempt-id>
contact revoke <endpoint-or-policy>
contact doctor
```

The first implementation should support a manual self-test ping before proactive policies. Test calls must state that they will ring/play audio and require explicit confirmation. `contact doctor` should report remote gateway, Tailscale reachability, client foreground/background capability, signaling, microphone/speaker permission state, local STT/TTS availability and expected fallback without exposing secrets.

Implemented controls are `contact status/policies/policy plan/policy add/policy enable/policy disable/policy remove/ping plan/ping approve/attempts/inspect/cancel/pause/resume/doctor`. Policy plans bind a stable client device label (never its IP), exact agent roles and triggers, UTC quiet hours, maximum attempts per day and expiry. Policies install disabled. Every ping—including MCP—still requires a fresh original-host Master Control approval; this is intentionally stricter than future standing proactive authority.

An approved attempt is retrieved only when the matching authenticated foreground client sends `contact-poll`. Retrieval records `Delivering` then `Ringing`; explicit client actions move to `Accepted`/`Connected`, `Declined` or `Ended`. Duplicate deduplication keys return the original attempt. Restart marks every active attempt `Interrupted`, and polling never replays it. Pause, policy disable/removal, transfer and device/session revocation cancel active work.

## Persistence, safety and recovery

- Persist policies, paired-device public identity, attempts, acknowledgements and bounded receipts locally. Keep private keys/tokens in the host/device secret stores.
- Copy/transfer disables all contact policies, removes active sessions and requires fresh endpoint pairing on the destination.
- Restart does not replay missed pings or calls. A scheduled workflow may create a new attempt only after rechecking its current policy and deduplication key.
- Rate-limit per event, policy, endpoint and day. Provide a global pause/kill switch usable from the original host even while a call is active.
- Phone loss/revocation invalidates its credentials and pending calls immediately. A tailnet IP or device name alone is not identity.
- Keep contact reasons and notification previews free of unnecessary private task/file content; retrieve details only after authenticated answer/open.
- Do not let a remote caller or compromised client select another recipient, widen scopes or invite agents outside the saved policy.

## Acceptance questions and tests

Before claiming the feature works:

1. Select the phone/client technology and prove foreground ping delivery over a private tailnet.
2. Measure whether background ringing works on each supported mobile OS and document its provider/battery/cost implications.
3. Verify answer/decline/missed/expiry/cancel, duplicate signaling, reconnect and host/client abrupt loss.
4. Verify wrong device, revoked pairing, expired media authorization and non-tailnet/public-route refusal.
5. Verify quiet hours, do-not-disturb, rate limits, policy expiry, trigger deduplication and no restart catch-up storm.
6. Verify MCP cannot call outside policy, forge urgency, leak private context or convert spoken text into approval.
7. Verify primary/local authority works only inside a saved policy and dynamic/delegated agents require a fresh exact approval for every attempt, retry, endpoint and speaking identity.
8. Verify RELAY receipts distinguish proposed, delivered, ringing, accepted, connected, ended, missed and failed.
9. Verify microphone activation begins only after answer and ends on hang-up/revoke; raw audio is not retained by default.
10. Verify local STT/TTS, text-only fallback, voice unavailability, high latency, packet loss and call cancellation under load.
11. Test a phone-to-Even-Realities relay path only after the phone client works; record display/audio limitations and do not claim native G2 calling unless separately implemented.
12. Verify challenge responses bypass ordinary transcription/inference, are purpose/call/endpoint bound, rate limited and replay resistant, with masked typed fallback when protected speech capture is unavailable.

## Dependencies and sequencing

This work depends on the authenticated remote gateway in 6.36A, persistent/scheduled policies in 6.37, trust/activity controls in 6.38 and remote-capable voice provider work building on 4.4. A sensible order is: manual foreground ping -> paired foreground call invitation -> accepted text session -> accepted voice stream -> bounded proactive policies -> optional background wake-up. PSTN/SIP and third-party calling remain separate later decisions.

**Automated evidence — 2026-11-19:** Five focused cases and the complete solution pass at **1,539 passed, 0 failed, 0 skipped**. Tests cover policy escape, exact trigger/agent/device, daily limits, pause/disable, deduplication, local authorization, durable state receipts, restart interruption/no replay, real paired loopback polling, accept/connect/end and doctor disclosure. This is protocol-level foreground text evidence—not a physical phone/Tailscale, background notification, audio, WebRTC, STT/TTS or wearable test.
