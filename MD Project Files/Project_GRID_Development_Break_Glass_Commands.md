# GRID development-only break-glass terminal command framework

## Status and intent

**Foundation plus locked-local, registered-agent self, original-host Master Control and offline owner-rescue boundaries implemented — 2026-11-18.** The registry records visible safeguards and reviewed hidden inventory while the ordinary dispatcher rejects every `::grid-*` registration. Separate host-owned executors enforce each session class. Master Control emergency mutations use short-lived two-step plans. Offline rescue requires separately enrolled user-held material and a stopped world; hidden nullification remains unavailable.

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
::grid-breakglass freeze <operation-id> <nonce>
::grid-breakglass isolate
::grid-breakglass isolate <operation-id> <nonce>
::grid-breakglass revoke-sessions
::grid-breakglass revoke-sessions <operation-id> <nonce>
::grid-breakglass disable-remote
::grid-breakglass disable-remote <operation-id> <nonce>
::grid-breakglass safe-mode-plan
::grid-breakglass safe-mode-commit <operation-id> <nonce>
::grid-breakglass recovery-plan backup
::grid-breakglass recovery-commit <operation-id> <nonce>
::grid-breakglass evidence-export <approved-local-path>
::grid-breakglass evidence-export <approved-local-path> <operation-id> <nonce>
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

Every implemented state-changing form above is two-step even when its action name does not contain `plan`/`commit`: the short form issues a preview and the repeated form with operation ID and nonce commits it. Nullification forms remain planned only.

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

Implemented syntax is `Grid.Terminal.exe --operator-rescue <absolute-world-path>`. Entry is processed before runtime/AI setup and requires interactive input, an exclusive stopped-world lease and user-held recovery material explicitly enrolled by authenticated Master Control. Challenge-code knowledge alone is insufficient. The rescue tool displays the exact world identity and supports only read-only verification, local password reset or restoration of a validated same-world backup carrying the same recovery authority. It cannot connect to AI, accept conversational input, initialize agents, open remote listeners or execute general shell/filesystem commands.

Enrollment uses `credentials recovery setup "<absolute-new-file.grid-recovery.json>"`. GRID creates a new file only, refuses reparse paths and overwrites, and stores only a versioned key ID plus a salted PBKDF2-SHA256 verifier in schema v27. The file contains a random 256-bit secret bound to one world. Rotation replaces the verifier and invalidates the old file. GRID does not retain its path or plaintext. State-changing rescue choices require the exact world ID and a fresh 128-bit nonce; password reset clears challenge credentials/rate limits while preserving agents, transcripts and world state. Backup restore revalidates matching recovery authority and archives the displaced primary. The external operator audit is bounded and excludes recovery material, paths, credentials and private content.

There is no universal vendor recovery key. Losing all enrolled owner/recovery material may make protected recovery impossible; documentation must say so before setup completes.

## Registration and non-discovery rules

- Hidden commands live in a separate registry, not ordinary `CommandDispatcher.Commands` metadata.
- Ordinary `help`, `help -md`, command groups, errors, suggestions, Tab completion, fuzzy matching, telemetry labels and capability catalogs never enumerate names or counts.
- Exact input dispatch checks local console provenance and session class before parsing arguments. Unauthorized callers receive one generic denial without confirming that a command exists.
- Commands are disabled in attached terminals, redirected input, named-pipe requests, remote gateways, voice, meetings, scheduled tasks and model/tool calls.
- Release builds may include the reviewed recovery subset; development-only diagnostics must be compiled out or separately signed/configured. Build flavor is not authorization.
- Technical documentation and tests list the inventory. Public user help may say only that an operator recovery procedure exists and where to obtain the correct version's trusted documentation.

### Implemented foundation boundary — 2026-11-18

- `Grid.Terminal/DevelopmentBreakGlassRegistry.cs` is the versioned source inventory. Its entries are contracts, not handlers; listing a syntax there does not make it executable.
- `CommandDispatcher.Register` rejects the entire reserved `::grid-*` namespace. This prevents an ordinary extension, alias or plugin from quietly acquiring a hidden-command-shaped route.
- Both ordinary parsing and connected-conversation parsing intercept the reserved namespace before unknown text can be sent to `talk` or a local model.
- Every reserved input receives the same generic unavailable response. The dispatcher does not disclose whether a particular family or action is planned.
- `help`, `help -md`, terminal completion and `CommandDispatcher.Commands` remain based only on ordinary registered commands and therefore expose no hidden names or counts.
- The ordinary reserved-command registry still contains no offline-rescue or hidden-nullification handler. Offline rescue is instead an exclusive pre-start mode; hidden nullification remains unavailable pending quarantine/path-ownership and destructive acceptance.

### Implemented locked-local boundary — 2026-11-18

- The original host constructs `LockedRecoveryCommands` with explicit interactive-console provenance. Attached windows, redirected/unattended sessions, authenticated users, agents, conversations and ordinary dispatcher calls cannot enter it.
- `status` reports only registry/world/path/run state and primary/backup presence. It does not enumerate accounts, prompts, responses, tasks, transcripts or captured context.
- `verify-world` opens primary and backup SQLite files read-only, runs integrity/schema/world-ID checks and reports recovery/nullification markers and sidecar counts. It never creates, migrates, saves, repairs, replaces or recovers a file; tests verify unchanged SHA-256 values.
- `freeze-request` pauses the simulation and invalidates running agent activity through the existing activity generation. It does not log in, kill unrelated processes, delete data or grant a restart capability.
- `recovery-challenge` creates a random two-minute, process-local operation ID and nonce. It is preparation only: no password reset, identity claim, task approval, world recovery or nullification authority exists. Restart, expiry or `cancel` removes it. A future offline rescue flow must also require separate user-held recovery material.
- Malformed syntax and unrecognized/reserved families return the same generic denial. The nonce is never written to audit/history; only content-free preparation/cancellation events are recorded.
- Eleven focused tests and the full solution pass (**1,464 passed, 0 failed, 0 skipped**).

### Implemented registered-agent self boundary — 2026-11-18

- `AgentBreakGlassCommands` is reachable only from an authenticated registered agent on the original interactive console and never while an inline conversation owns input. Attached, redirected, logged-out and Master Control contexts receive the same generic denial.
- The simulation engine independently requires an original-host non-Master agent lease bound to the active registered agent; terminal filtering is not the security boundary.
- `self-status` reports only the agent's own name/role/ID, GRID activity state and counts of its task states.
- `evidence-self` adds at most 32 of that agent's task IDs, statuses and attempt counts. Arguments, results, capabilities, private context, other-agent records and Master Control data are excluded.
- `abort-self` changes only awaiting, queued or running tasks whose target is exactly the active agent. Cancellation is audited and saved when persistence is available; a save failure is reported without claiming durability.
- `detach-self` performs logout and leaves `grid[locked]`. It cannot switch or restore Master Control.
- Engine-level tests prove Master Control and attached-agent leases are rejected and a MAXIS abort leaves an otherwise eligible JN task unchanged.
- Seventeen focused 6.34B tests and the full solution pass (**1,470 passed, 0 failed, 0 skipped**).

### Implemented original-host Master Control boundary — 2026-11-18

- `MasterBreakGlassCommands` requires an authenticated human Master Control lease from the original interactive console and is bypassed while a conversation owns input. Agent, locked, attached and redirected callers receive the generic denial.
- Every mutation first creates one active two-minute plan containing a random operation ID/nonce bound to session generation, world ID, world instance, activity generation, action and exact target. The commit must repeat the exact values; new plans supersede old plans, commits consume plans, and wrong/stale/replayed/cross-session/activity-changed inputs fail closed.
- `freeze` pauses simulation/agent work without stopping unrelated processes. `isolate` freezes, disables remote acceptance, revokes transient challenge authority, task dispatch approvals and all terminal sessions. `revoke-sessions` performs the same session/approval revocation without setting the persistent process remote gate. `disable-remote` sets that gate and revokes transient challenge endpoints, attempts and claims; this build has no remote listener to stop.
- Safe mode freezes activity, revokes approvals/challenge authority, closes attached windows, disables remote acceptance and gates ordinary commands, conversations, inference, voice and task controls until restart. Only minimal terminal lifecycle commands and reviewed emergency controls remain available.
- Recovery supports only a validated matching backup. Plan creation records primary/backup SHA-256 values; commit freezes GRID and exits without final save. After the scheduler/autosave/attachments stop, hashes and world identity are revalidated, the displaced primary/sidecars are archived, the backup is restored and restart is required. Changed hashes or lost authorization refuse recovery.
- Evidence export creates only a new absolute `.json` file through a non-reparse parent. It contains contract/version, world/instance/tick/run state, aggregate task counts, emergency gates and world-file status—never credentials, challenge material, transcript/prompt/context bodies or task arguments/results.
- Plan issuance, commit acceptance and outcomes write bounded content-free JSONL beside the world so recovery cannot erase its own operator evidence. Nonces and target paths are excluded; the audit refuses growth beyond 4 MB until manually archived.
- Twenty-seven focused 6.34B tests and the full solution pass (**1,480 passed, 0 failed, 0 skipped**).

### Implemented offline owner-rescue boundary — 2026-11-18

- Authenticated Master Control on the original persistent host can explicitly create/rotate a world-bound 256-bit recovery file; only its key ID and salted verifier persist. There is no vendor key, compiled phrase, challenge-code bypass or automatic enrollment.
- The pre-start `--operator-rescue` route executes before model/runtime setup, refuses redirected input and active worlds, validates the current schema and exact world/key identity, and never constructs inference, conversation, agent, remote, shell or arbitrary-file services.
- Its complete operation set is verification-and-exit, local username/password reset and validated backup restoration. Mutations require an exact world-ID/fresh-nonce confirmation. Backup authority must match both primary and backup.
- Recovery files and world paths never enter the content-free operator audit. Password reset preserves world/agent/transcript state while revoking challenge credentials and cooldowns. Backup restore retains the displaced primary archive.
- Seven focused tests and the full solution pass (**1,487 passed, 0 failed, 0 skipped**), including rotation and cross-world rejection.

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

Steps 1–5 plus the locked-local, agent-self, original-host Master Control and offline owner-rescue portions are implemented. The next boundary is recoverable world retirement. Hidden nullification/removal remains unavailable; knowing inventoried names grants no authority.

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

The visible `nge` command remains the ordinary authenticated nullification path and must retain its current Master Control, original-host and interactive confirmation behavior. `--recover` remains the documented backup recovery path. The implemented break-glass framework is for diagnosis and exceptional operator recovery, not a more convenient or less protected duplicate. Hidden retirement/removal remains unavailable until its separate safeguards and disposable-root acceptance are complete.

This work depends on [Local Login and Identity Sessions](Project_GRID_Local_Login_and_Identity_Implementation.md), [Challenge-code Authentication](Project_GRID_Challenge_Code_Authentication.md), [Persistence Architecture](Project_GRID_Persistence_Architecture.md), [Terminal UX and Portable Credentials](Project_GRID_Terminal_UX_Implementation.md), and the remote-session revocation design in [Remote Terminal Access](Project_GRID_Remote_Terminal_Access.md).
