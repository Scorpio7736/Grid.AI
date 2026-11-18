# GRID development-only break-glass terminal command framework

## Status and intent

**User-requested planning framework — 2026-11-17. No hidden/break-glass command is implemented by this document.**

GRID needs a small, explicitly reviewed set of terminal recovery and emergency-control operations that are omitted from normal `help`, `help -md`, command palettes, completion, conversational routing and agent tool catalogs. Their exact syntax and behavior belong only in development/operator documentation and source tests.

These commands are undiscoverable through the ordinary interface, but they are not trusted because they are hidden. Secrecy is not authentication. Every operation still requires the authority, local-presence proof, target binding, audit and confirmation defined below. There must be no compiled master password, magic phrase, undocumented network endpoint or command that silently grants Master Control.

## Session classes

The current terminal has three relevant authenticated states plus a separate offline rescue mode:

| State | Meaning | Maximum break-glass authority |
| --- | --- | --- |
| **Locked / logged out** | Host is running but no authenticated GRID session exists | Read-only diagnosis and preparation. It may request a host freeze or produce a recovery challenge, but cannot inspect private content, change identity or nullify a world by command name alone. |
| **Authenticated agent** | A registered agent is the active terminal identity; Master Control capabilities are disabled | Self-scoped stop/detach and content-free diagnostic capture only. It cannot recover credentials, override policy, revoke other identities or destroy GRID. |
| **Human Master Control** | The authenticated human owner is active in the original local host terminal | Reviewed freeze/isolate/revoke/recovery operations and preparation of destructive actions. Highest-risk commits require an additional local factor and exact confirmation; Master Control status alone is insufficient for emergency nullification. |
| **Offline owner rescue** | Normal GRID host is stopped; an exclusive local rescue entry point validates the world and user-held recovery material | Bounded recovery or emergency nullification when normal login cannot operate. This is not a logged-out backdoor and is unavailable remotely or to agents. |

Attached terminals, remote clients, conversation text, model output, scheduled jobs and agent task adapters can never invoke or approve this command family. Production parsing must reject aliases, abbreviations, natural-language equivalents and pasted multi-command batches.

## Provisional hidden command inventory

Names are deliberately clear in this technical document so tests and audits can reason about them. They remain provisional until implementation. A common reserved prefix prevents collisions with normal commands.

### Locked / logged-out local host

```text
::grid-rescue status
::grid-rescue verify-world
::grid-rescue freeze-request
::grid-rescue recovery-challenge
::grid-rescue cancel <operation-id>
```

- `status`: content-free host/world path, running/paused/locked state, recovery-artifact presence and integrity availability; no transcripts, identities, prompts or secrets.
- `verify-world`: read-only database/application ID, schema, integrity, backup and sidecar checks; never migrates, repairs or replaces data.
- `freeze-request`: stops new agent work and remote/contact acceptance while retaining the locked host for local authentication. It cannot kill unrelated processes or delete data.
- `recovery-challenge`: creates a short-lived nonce for the separate offline owner-rescue flow. It does not reveal whether a username/challenge response was correct and does not authenticate the caller.
- `cancel`: cancels an uncommitted rescue plan created in the same local process.

No locked command may reset a password, enroll challenges, bind a new owner, expose private content, enable networking, change permissions or commit nullification.

### Logged in as a registered agent

```text
::grid-agent-break self-status
::grid-agent-break abort-self
::grid-agent-break detach-self
::grid-agent-break evidence-self
```

- These operations affect only the active registered agent identity and work attributable to it.
- `abort-self` cooperatively cancels the agent's current/queued work without approving rollback or deleting durable history.
- `detach-self` returns control to the locked/original-owner boundary; it cannot switch to Master Control.
- `evidence-self` emits bounded content-free health/task identifiers for operator diagnosis.

An agent cannot invoke the Master Control or offline rescue inventory, even if it knows the exact syntax or places it in a transcript/tool result.

### Logged in as human Master Control on the original host

```text
::grid-breakglass status
::grid-breakglass freeze
::grid-breakglass isolate
::grid-breakglass revoke-sessions
::grid-breakglass disable-remote
::grid-breakglass safe-mode-plan
::grid-breakglass safe-mode-commit <operation-id> <nonce>
::grid-breakglass recovery-plan <primary|backup>
::grid-breakglass recovery-commit <operation-id> <nonce>
::grid-breakglass evidence-export <approved-local-path>
::grid-breakglass nullify-plan <world-id>
::grid-breakglass nullify-commit <operation-id> <world-id> <nonce>
::grid-breakglass cancel <operation-id>
```

- `freeze`: stop new tasks/inference/contact attempts and cooperatively cancel running GRID work; do not stop unrelated host applications.
- `isolate`: freeze, disable remote listeners/contact delivery and invalidate remote sessions while retaining local recovery access.
- `revoke-sessions`: invalidate GRID identity, attachment, remote, challenge and approval sessions; it does not erase accounts or history.
- `safe-mode`: restart/continue with simulation, agents, inference, voice, remote access and scheduled work disabled for diagnosis.
- `recovery`: prepare and apply only an existing validated primary/backup recovery path; never silently regenerate a world.
- `evidence-export`: export bounded integrity/audit metadata without credentials, challenge hashes, transcript bodies or model prompts.
- `nullify`: permanently retire the exact selected GRID world and its GRID-managed runtime identity. It is distinct from ordinary visible `nge` and must not be a shortcut around NGE safeguards.

## Emergency nullification contract

“Nullify GRID's existence” must have an exact scope. The initial design distinguishes:

1. **World retirement:** stop the host, revoke sessions/pairings, mark the world retired and move its database/backups to an operator-selected quarantine. Recoverable until explicitly purged.
2. **Managed-instance removal:** retire the world and remove only files proven by the signed manifest to belong to that GRID instance. Never touch source repositories, unrelated models, shared runtimes or arbitrary parent folders.
3. **Credential destruction:** delete/revoke GRID-owned local secret-store entries and pairing keys after retirement evidence is written.
4. **Irrecoverable purge:** a separately reviewed future operation. Filesystem deletion cannot honestly guarantee cryptographic erasure on SSDs, snapshots, backups or synchronized storage; the UI and documentation must not claim otherwise.

Default `nullify-plan` is recoverable world retirement, not immediate erasure. A commit requires all of the following:

- original physical host or offline owner-rescue mode; never attached/remote;
- human Master Control plus a fresh independent recovery factor (not an agent, conversation, static hidden phrase or challenge code alone);
- exact world ID, canonical managed root and manifest/hash preview;
- host frozen and exclusive world lease acquired;
- a randomly generated short-lived nonce shown only after the plan;
- retyping the world ID and nonce, with no y/N shorthand;
- a cooling-off window for managed-instance removal or credential destruction;
- durable content-free receipt outside the retiring world, when a safe approved location exists;
- fail closed on uncertain ownership, reparse points, path changes, active writers, incomplete manifests or evidence-write failure.

The operation must never recursively target a workspace root, user profile, drive root, repository root or unresolved environment-variable path. Recovery material and quarantine location must be validated before mutation. Partial completion reports exact completed/pending steps and never retries automatically.

## Offline owner-rescue entry point

Normal locked commands cannot solve a lost password without becoming a backdoor. Plan a separate local executable/startup mode that is absent from ordinary help and works only while the regular host is stopped:

```text
Grid.Terminal.exe --operator-rescue <absolute-world-path>
```

The final syntax may change. Entry requires an exclusive file lease, verified OS/local-device context where supported, and user-held recovery material enrolled during setup. Challenge-code knowledge alone is insufficient. The rescue tool displays the exact package/world identity, supports read-only verification first, and creates typed plans. It cannot connect to AI, accept conversational input, open remote listeners or execute general shell/filesystem commands.

There is no universal vendor recovery key. Losing all enrolled owner/recovery material may make protected recovery impossible; documentation must say so before setup completes.

## Registration and non-discovery rules

- Hidden commands live in a separate registry, not ordinary `CommandDispatcher.Commands` metadata.
- Ordinary `help`, `help -md`, command groups, errors, suggestions, Tab completion, fuzzy matching, telemetry labels and capability catalogs never enumerate names or counts.
- Exact input dispatch checks local console provenance and session class before parsing arguments. Unauthorized callers receive one generic denial without confirming that a command exists.
- Commands are disabled in attached terminals, redirected input, named-pipe requests, remote gateways, voice, meetings, scheduled tasks and model/tool calls.
- Release builds may include the reviewed recovery subset; development-only diagnostics must be compiled out or separately signed/configured. Build flavor is not authorization.
- Technical documentation and tests list the inventory. Public user help may say only that an operator recovery procedure exists and where to obtain the correct version's trusted documentation.

## Authorization, audit and recovery factors

- Use versioned capability IDs and exact operation scopes, never string role labels.
- Require fresh authentication for each state-changing plan; stale login sessions, challenge claims and previous approvals do not carry over.
- Challenge codes from 6.34A may contribute only as one explicitly allowed knowledge factor after remote challenge protection exists. They never authorize nullification by themselves.
- Prefer a setup-generated offline recovery key/file held by the user, with hash/identifier stored locally and rotation/revocation support. Do not store its plaintext beside the world.
- Audit attempts and outcomes without command secrets, recovery factors, nonces or private content. Where the world may be destroyed, place the final receipt in an explicitly approved external operator log.
- Every plan expires, is single-use, binds executable/version, world ID, instance ID, canonical paths, manifest hash, host generation and requested steps.

## Implementation sequence

1. Inventory existing visible recovery/destructive paths (`--recover`, `nge`, transfer, revoke, shutdown) and prevent hidden aliases from weakening them.
2. Define the separate hidden registry and exact local-console provenance contract; add non-discovery tests before any mutating command.
3. Implement locked read-only `status`/`verify-world` and Master Control `freeze`/`isolate`/`revoke-sessions` first.
4. Add safe-mode and evidence export with bounded, secret-free schemas.
5. Design setup-time offline recovery material and the exclusive offline rescue entry point.
6. Implement recoverable retirement planning/commit with quarantine; test against disposable roots only.
7. Consider managed-instance removal and credential destruction only after manifest/path/reparse/partial-failure acceptance.
8. Keep irrecoverable purge deferred until storage-specific guarantees and limitations are reviewed.

## Acceptance matrix

- All hidden names are absent from normal help Markdown, live help, completion, palettes, intent routing, model prompts and agent capability catalogs.
- Exact names issued from conversation, attached/remote terminal, redirected input, scheduled jobs or agent tools are generically denied and perform no state change.
- Locked state exposes only bounded status/verification and cannot infer private identity existence or reset credentials.
- Registered agents can affect only themselves and cannot switch, revoke others, recover or nullify.
- Master Control operations require the original host, current capability, plan binding, fresh factor and exact commit nonce appropriate to risk.
- Wrong/stale/replayed/cross-world nonces, changed paths, copied plans, restart, logout, device change and version mismatch fail closed.
- Freeze/isolate stop GRID work and networking without terminating unrelated processes or losing the last committed world.
- Recovery never regenerates, overwrites a newer valid primary or consumes the only backup without a validated replacement.
- Nullification tests use disposable, manifest-bounded fixtures and verify refusal of profile/root/repository paths, symlinks/reparse points, shared assets, active writers and incomplete ownership.
- Interrupted retirement/removal produces an exact resumable or manually recoverable receipt; it never broadens deletion on retry.
- Technical documentation, source inventory and tests agree exactly; release review fails on an undocumented hidden command.

## Relationship to existing controls

The visible `nge` command remains the ordinary authenticated nullification path and must retain its current Master Control, original-host and interactive confirmation behavior. `--recover` remains the documented backup recovery path. The planned hidden framework is for diagnosis and exceptional operator recovery, not a more convenient or less protected duplicate.

This work depends on [Local Login and Identity Sessions](Project_GRID_Local_Login_and_Identity_Implementation.md), [Challenge-code Authentication](Project_GRID_Challenge_Code_Authentication.md), [Persistence Architecture](Project_GRID_Persistence_Architecture.md), [Terminal UX and Portable Credentials](Project_GRID_Terminal_UX_Implementation.md), and the remote-session revocation design in [Remote Terminal Access](Project_GRID_Remote_Terminal_Access.md).
