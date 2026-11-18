# GRID challenge-code authentication planning

## Status and immediate goal

**Local enrollment foundation implemented — 2026-11-17. Remote challenge sessions and voiceprint verification remain planned.**

Add three challenge prompt/response pairs to initial local-credential setup and authenticated credential maintenance. Their first use is a bounded knowledge-factor check when the user is speaking with GRID through an authenticated remote call. This extends the existing username/password identity; it does not replace the password, device pairing or session authorization.

A spoken challenge response is not a biometric voice signature. It shows that the speaker knows an enrolled secret, but does not prove that the response came from a particular voice. Future speaker/voiceprint verification, if adopted after consent, privacy and spoof-resistance evaluation, must be an additional factor with its own enrollment and revocation.

## Setup experience

After a new user creates and confirms the username and password, setup requires exactly three distinct challenge entries:

```text
Challenge 1 of 3
Prompt: Family
Response: <masked input>
Confirm response: <masked input>
```

- The user creates each short prompt and its private response; GRID does not ship guessable questions such as birth city, school or mother's maiden name.
- Setup warns against public facts, reused passwords, names visible on social media and answers another person could easily research.
- Prompts may be displayed or spoken during a challenge. Responses are always collected as secrets and never echoed, completed, logged, transcribed or sent to an AI model.
- Prompts must be distinct after normalized comparison. Responses must meet a documented length/character policy and must not equal the username, password or another challenge response after normalized comparison.
- Setup is atomic: username, password and all three challenges are validated before any new credential state is saved. Cancellation or failure leaves the prior credential state unchanged.
- Existing users add challenges only after authenticating with the current password. Rotation replaces the entire three-challenge set atomically and invalidates outstanding challenge sessions.

The example prompt `Family` is supported, but a more specific private cue may be easier for the user to remember. GRID must explain that the displayed prompt itself is not secret.

## Storage and trust boundary

Introduce a versioned challenge credential record owned by the local identity state. Store only:

- a stable challenge ID and display prompt;
- an independent random salt, approved password-derivation parameters and derived response verifier;
- creation/rotation version and non-secret policy metadata.

Plaintext responses must not enter the world database, backup, audit history, transcript, inference prompt, contact receipt or diagnostic output. Verification occurs in a trusted host authentication service using fixed-time comparison. MCP, RELAY, speaking agents and remote clients receive only the selected prompt, opaque attempt ID and final success/failure result—not the expected response or hash.

Challenge records travel with an explicitly authenticated world/credential transfer, but moving/copying a world revokes remote pairings and active challenge sessions. A challenge response is not a recovery key: losing the password does not become recoverable merely by answering these challenges in the initial scope.

## Challenge session protocol

Every verification uses a short-lived, single-purpose server-created session bound to the user, host/world generation, authenticated paired endpoint, contact/call ID and requested assurance purpose.

1. The trusted host randomly selects challenges without revealing all three at once. Initial policy should require two distinct correct responses from the enrolled set of three.
2. The speaking agent reads the host-supplied prompt. The client marks the following utterance as secret challenge input and sends it directly to the host verifier outside ordinary transcription/inference.
3. The host returns only `passed`, `failed`, `expired`, `locked`, or `cancelled`. Failure does not reveal which response was wrong.
4. A passed session creates a short-lived authentication claim for its exact purpose. It cannot approve a task, widen a grant, reset a password, change a contact policy or authorize a different call.
5. Reconnect, replay, duplicate messages, agent handoff, endpoint change, host restart or call termination invalidates the session.

Until a client can provide a protected secret-input path, verbal challenges must remain disabled for that client. Ordinary call transcription must pause or exclude the challenge-response interval, and raw audio recording remains prohibited by default. GRID should offer typed masked entry on the paired phone as the safer fallback when privacy is uncertain.

## Abuse resistance and privacy

- Apply bounded failures per session, account, endpoint and time window; after the configured limit, cancel the session and impose an increasing cooldown.
- Do not let an agent choose the challenge, normalize the response, override a failure, see per-question correctness or claim the user passed without a signed host result.
- Do not expose whether a username or challenge set exists to an unauthenticated remote party.
- Warn the user before speaking a response where another person, speakerphone, voicemail, recording system or transcription service may capture it.
- Never conduct a challenge in an agent-originated call until the paired client has independently authenticated the host and visibly identifies the purpose. This reduces social-engineering calls that imitate GRID.
- Audit only content-free lifecycle events: enrollment/rotation, session issued, passed/failed/expired/locked/cancelled, endpoint, purpose and policy version.
- Provide immediate authenticated rotation and a local Master Control disable switch. Suspected disclosure requires replacing all three responses.

## Planned controls

Names are provisional and secret collection remains interactive:

```text
credentials challenges status
credentials challenges setup
credentials challenges rotate
credentials challenges disable
credentials challenges test
```

The status command shows enrollment/version/last rotation and lockout status, never hashes or responses. Prompts appear only inside the authenticated management or verification flow. Setup and rotation require masked interactive input; redirected arguments and command history are refused.

## Implementation stages

1. Define and test the challenge credential contract, normalization, independent hashing, validation and atomic persistence migration.
2. Extend new-user username/password setup to require all three challenges; add authenticated setup/rotation for existing users.
3. Add a local interactive self-test that proves responses never appear in output, audit, save diagnostics or inference routes.
4. Implement short-lived challenge sessions and purpose-bound claims at the trusted host boundary.
5. Integrate protected typed entry with the paired remote client, then evaluate spoken capture with transcription suppression.
6. Connect the result to remote-call identity display without granting command approval or account recovery.
7. Separately evaluate opt-in voiceprint authentication for consent, local template storage, false accept/reject rates, replay/deepfake resistance, illness/noise and accessibility. Do not call challenge codes a voice signature.

## Acceptance coverage

- Fresh setup requires exactly three valid, distinct prompt/response pairs and saves nothing on cancellation or partial failure.
- Existing credential and schema migrations remain readable; existing users are directed to enroll before remote verbal verification is available.
- Each response has an independent salt/verifier; equivalent answers do not produce equivalent stored values.
- Case, whitespace, Unicode and punctuation behavior is explicit and stable; the display prompt never changes verification semantics.
- Responses never appear in terminal output/history, audit, JSON diagnostics, database plaintext scans, backups, transcripts, model prompts or call receipts.
- Wrong, replayed, reordered, duplicated, expired and cross-call responses fail without revealing the failed challenge.
- Rate limits/cooldowns survive restart as designed without permanently denying the legitimate local owner.
- A passed challenge cannot approve work, reset credentials, pair a device, change policies or survive endpoint/agent/call/host changes.
- MCP, RELAY and dynamic agents cannot read hashes, select easier questions or forge a successful host result.
- Speakerphone, recording/transcription enabled, noisy audio, speech disability and unavailable microphone paths provide a masked typed fallback or fail closed.
- Credential rotation, world copy/transfer, backup recovery and device loss invalidate outstanding sessions and remote authority appropriately.

## Dependencies and open decisions

The local enrollment foundation extends [Local Login and Identity Sessions](Project_GRID_Local_Login_and_Identity_Implementation.md) and [Guided Setup](Project_GRID_Guided_Setup.md). Remote use depends on authenticated pairing in [Remote Terminal Access](Project_GRID_Remote_Terminal_Access.md) and protected media/input handling in [Remote Contact and Voice Calls](Project_GRID_Remote_Contact_and_Voice_Calls.md).

Before implementation is called complete, choose and document response normalization, minimum/maximum response size, lockout timing, whether local login may optionally use challenge codes, and whether prompts are safe to display in normal status views. Password recovery, third-party identity verification and biometric voiceprints remain separate decisions.

## Local foundation completion evidence — 2026-11-17

Schema v25 adds a version gate for three challenge credentials inside the existing local identity record. Fresh interactive setup and authenticated `credentials` rotation now require three distinct prompts and confirmed masked responses. Each normalized response uses its own random 16-byte salt, PBKDF2-HMAC-SHA256 verifier at 600,000 iterations, and fixed-time verification. Password plus all challenges validate before one engine update/persist action; a failed confirmation saves nothing. Password-only trusted legacy replacement clears any old challenge set.

Implemented normalization collapses surrounding/repeated whitespace, uses Unicode Form KC and case-insensitive response comparison to support eventual speech transcription. Prompts use Unicode Form C with collapsed whitespace. As amended by the user on 2026-11-17, both prompts and responses accept 1..64 printable characters. Responses cannot match each other, the normalized username, or password. The stored verifier format and normalization remain credential contract version 1; this change only broadens enrollment length acceptance.

Verification passed **1,441 solution tests**: 14 Core, 157 Simulation, 99 Persistence and 1,171 Terminal, zero failed/skipped. Coverage includes atomic mismatch refusal, distinct salts, normalization, wrong responses, masked setup output, content-free audit, persistence, backup recovery and v23/v24 schema upgrades. The full run used isolated `TestResults/challenge-codes` build artifacts.

Not implemented: `credentials challenges ...` subcommands, separate disable/self-test controls, remote challenge sessions, durable challenge rate limits, paired-client protected input, transcription suppression, call-bound claims, password recovery or biometric voiceprints. Current challenges are enrolled and rotated with the full `credentials` flow but are not accepted as login or action approval.

## Setup/login retry repair — 2026-11-17

Credential setup now displays the complete 1..64-character prompt-and-response policy before collection. If password confirmation or final challenge validation fails, GRID states that no account was created or changed and asks the user to retry setup or remain logged out. It no longer silently returns to the command prompt in a state where the next `login` appears to create another account. After a successful atomic save, `login` follows the username/password path and never re-enters setup.

Regression coverage reproduces a rejected empty challenge response, completes a controlled retry, then verifies ordinary password login. Boundary coverage accepts exact 1- and 64-character prompts/responses and rejects 0 and 65 characters. Focused verification passed 21 terminal UX tests and three persistence tests; the full solution passed **1,443 tests** (14 Core, 157 Simulation, 99 Persistence and 1,173 Terminal), with zero failures/skips.
