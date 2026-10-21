# Project GRID — Session Context and Authorization Contract

## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all GRID runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 4.2B (implemented), 8.6 and 8.7 (planned). See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

## Local login and identity completion - 4.2A (2026-10-20)

**Implemented and verified.** Start with `login master`: the first successful Windows-token login binds the world owner; later logins must match. Sessions expire after eight hours. Master Control can register existing Agent identities, switch roles, and preview/confirm one-time program edits. Prompts/help/commands are role-aware; agent chats are participant-scoped and global/admin controls are denied. Switching/logout clears active chats and previews. Schema v11 preserves owner/agent registration without restoring authentication.

**362 tests passed**, plus isolated Windows login/agent restriction/confirmed edit/restart smoke. See [Local Login and Identity Implementation](Project_GRID_Local_Login_and_Identity_Implementation.md) for commands, service authorization, audit and security limits. This authenticates the current Windows account, grants no OS elevation, and provides no independent agent passwords or real task service. NGE now requires Master Control; --start waits for login. Earlier dated pending-login/editing statements refer to prior milestones.

## Terminal conversations completion - 4.2 (2026-10-20)

**Implemented and verified.** Use connect (default MCP), talk, conversations, transcript, resume and disconnect. MCP is created on first explicit connection as a persistent coordinator Agent; a stable world-local operator handle supports reopening saved conversations. Replies to status/who are you are labeled verified GRID state; other replies are labeled deterministic simulated dialogue and claim no task execution/delegation. Input/reply pairs commit atomically and reuse schema v10.

**353 tests passed**, plus a three-launch isolated terminal save/resume/end smoke. See [Terminal Conversations](Project_GRID_Terminal_Conversations.md) for syntax, identity/capacity limits and completion evidence. This local operator handle grants no authentication or admin authority; 4.2A login, real task delegation and LLM dialogue remain planned. Earlier dated pending-terminal-chat/agent-creation statements describe previous milestones.

## NGE implementation - 1.13 (2026-10-19)

**Initial local-terminal NGE implemented.** Type `nge` in an interactive saved-world session, review the world identity and exact file list, then enter `y` to confirm or `n`/blank to cancel. Invalid answers reprompt; EOF cancels deletion and follows normal EOF shutdown; Ctrl+C cancels deletion and follows normal graceful shutdown. Arguments such as `nge y` are rejected. Redirected/unattended input and ephemeral sessions cannot delete saves. This is local-operator confirmation, **not authenticated admin enforcement**; human-admin-only access remains future login/authorization work.

Confirmed NGE stops/awaits scheduler, autosave and telemetry, serializes against in-flight saves, revalidates world/backup identity, then removes only the active database, rolling `.backup` and exact `-journal`, `-wal`, `-shm` paths. It retains the session lease through completion and refuses symbolic-link/junction targets. Historical legacy/damaged archives, staging artifacts of uncertain ownership, external copies, other worlds and PC applications remain; the warning explicitly states this scope. The harmless session `.lock` file may remain after release. Interrupted recovery must be resolved before NGE can be prepared.

A flushed `.nullifying` marker is created before deletion. Partial failure retains that marker, blocks startup/save/recovery, reports remaining files and exits nonzero without a final save. **There is no automatic retry/resume command yet:** inspect the marker and remaining exact world artifacts before explicit manual cleanup; never remove the marker merely to bypass an unresolved operation. Successful NGE removes the marker and exits with the GRID7736 artwork and `GRID NULLIFIED // EXISTENCE TERMINATED`, never a saved-world claim. Next normal startup follows existing hardware-city creation; it does not reuse a managed backup. Independent historical/manual copies remain outside this deletion scope.

Verification: **306 Release tests passed** (14 Core, 127 Simulation, 83 Persistence, 82 Terminal). New coverage checks cancellation/blank/invalid/EOF input, uppercase confirmation, redirected/ephemeral rejection, worker shutdown before deletion, no final save on success/failure, owned-file deletion, unrelated-file preservation, rejected forged plans, blocked future writes/recovery, new identity after reopen and a Windows locked-file failure retaining the marker. All deletion tests use freshly created disposable worlds; no user save was deleted. Existing hardware-city creation tests cover the startup path; no manual destructive run against the user's world was performed. Visual Studio still owns its post-exit footer.

## Program behavior completion - 2026-10-19

**3.1 implemented and verified.** Non-demo residents now have deterministic needs/goals/actions. Applications reflect fresh or unknown observations; explicitly configured Agent/Special residents perform resource-gated simulated occupation work/rest on simulation-time shifts. Schema v5 persists their routines, sourced budgets and progress without restoring live observations or adding offline work. Use behaviors and inspect for last-tick behavior. No OS task execution, movement or named agent services are claimed.

See [Program Needs and Behavior](Project_GRID_Program_Behavior.md) for policy, APIs, limits and completion evidence: **278 Release tests passed**, build zero warnings/errors, disposable terminal smoke passed. Debug output was locked by running user sessions, which were left running. Earlier dated deferred-behavior/schema statements describe previous milestones; existing history and completed scope are preserved.

## Application resident completion - 2026-10-19

**2.7 / 2.7.1 implemented and verified:** production population now follows current-session windowed executable groups through read-only discovery and serialized lifecycle reconciliation. Application residents are transient; Agent/Special residents persist in schema v4. Before production conversion, legacy fictional citizens are preserved in a uniquely named validated archive. City/layout and clock remain intact. Unknown resource requirements display unknown condition; no host-control authority is implied.

Verification: **268 tests passed**, build zero warnings/errors, and a disposable Windows startup/reopen detected nine application groups. See [Application Residents Implementation](Project_GRID_Application_Residents_Implementation.md) for grouping limits, discovery/freshness, storage/migration, editing, resource interpretation and completion evidence. Earlier dated statements that 2.7 is planned, production cities stay empty, or all residents persist describe previous milestones. Agent delegation/services, login, rendering and additional per-application telemetry remain future work.

## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

Application representatives are observed entities, not authenticated users or agents. Process names and persistent classifications grant no roles. Future agent login must use trusted persistent identity; any ordinary-program view must be read-scoped and bound to a current incarnation. Exit/relaunch invalidates incarnation-bound previews and access bindings. Existing default-deny authorization remains unchanged.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


## Implementation status - 2026-10-17

The editing authorization seam is implemented, with default denial and test-only authorization providers. The general session contract and lifecycle below remain the integration specification. A production provider must implement authentication, expiry, current capability/scope checks and revocation ordering; the engine does not supply those services.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Scope:** Section 1.9.2; GRID-REQ-003  
**Status:** Design complete; editing authorization boundary implemented in 1.9.3; authentication and session lifecycle remain deferred
**Recorded:** 2026-10-16  
**Related:** [Identity plan](Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md), [Program Editing Rules](Project_GRID_Program_Editing_Rules.md)

## 1. Identity model

Separate the authenticated operator from the identity used for an action:

- **Human operator:** stable local account ID established by a trusted authentication provider.
- **Active identity:** either the human's Master Control role or a registered agent program GUID. Master Control is not an agent program and must not require a fabricated program ID.
- **Effective permissions:** the intersection of operator grants, active-identity policy, and resource/world scope. Host actions also require their existing capability and approval checks.
- **Execution source:** HumanSession, AutonomousAgent, or SimulationSystem. Autonomous and simulation execution contexts are issued by their own trusted services, not created by typing a terminal identity name.

"Master Admin" and "Master Control" are display aliases for the same proposed administrator role. Display names and occupations never determine permissions. MCP remains an agent coordinator, not the human administrator.

## 2. Proposed session contract

The session service owns mutable lifecycle state. Each command receives an immutable request context derived from that service.

| Field | Purpose and rule |
|---|---|
| SessionId | Unique ID for a session; not itself proof of authentication. |
| SessionGeneration | Changes on identity switch, lock, logout or revocation, invalidating previously captured contexts and previews. |
| OperatorId | Verified human account ID for an authenticated human session; absent before authentication. |
| ActiveIdentity | Tagged union: MasterControl(operator ID) or Agent(program GUID). No active identity while unauthenticated. |
| WorldInstanceId | Scope of the running world; changes when a world is replaced/reloaded so old previews cannot cross instances. |
| AuthenticationState | Unauthenticated, Authenticated, Locked, or Ended; set only by the session service. |
| IssuedAtUtc / ExpiresAtUtc | Authentication lifecycle timestamps, when expiry is configured; separate from simulation time. |
| PolicyVersion | Version of authorization policy used to derive the context; services resolve current policy at execution. |
| CapabilityView | Immutable, scoped capabilities for UI/help; not a client-supplied authorization grant. |

Each invocation also carries a RequestId and execution source for correlation. Never put passwords, credential material or raw authentication tokens into command arguments, snapshots or audit records.

Initial human login can target one local operator, but use stable operator IDs rather than a global "current admin" flag. Authentication method and concrete expiry duration remain open provider/policy choices.

## 3. Issuance and trust boundary

The eventual flow is:

1. A local authentication provider verifies the human and returns a verified operator to the session service.
2. The session service checks which identities that operator may adopt and issues a session.
3. TerminalHost resolves a current context for each invocation and passes it explicitly to CommandDispatcher and the handler.
4. The application service resolves that session's current state and policy before reading protected data or mutating state.
5. The validated operation commits through the engine's serialized mutation boundary.

A caller-provided role string, capability collection, program name or IsAdmin flag must not be enough to authorize an action. Session identifiers must be resolved by the trusted service; in-process types alone are not a security boundary against arbitrary code running inside the process.

The 1.9.3 editing API now depends on an authorization lease provider. A test fixture may supply an explicit fake resolver. The normal application must deny administrative edits when no authenticated session provider is configured; do not silently create a production Master Control session to make tests or demos convenient. Genesis commands can retain their current behavior until session integration, but newly added administrative edits must not be exposed without enforcement.

## 4. Initial capability policy

Capability keys below are proposed stable identifiers, separate from command names.

| Operation | Capability | Unauthenticated/locked | Human as Master Control | Human as agent / autonomous agent |
|---|---|---|---|---|
| Public help, login, exit | Session bootstrap policy | Allowed; no protected world details | Allowed | Allowed |
| Show own identity | Session self-inspection | Report unauthenticated/locked only | Allowed | Allowed |
| Read ordinary world snapshots | world.read | Denied once login integration is enabled | Allowed within world scope | Allowed only with operator and agent grants; private context is separate |
| Start/pause/manual simulation step | world.control | Denied | Allowed within world scope | Denied by initial policy |
| Preview/apply program-value edits | programs.edit | Denied | Allowed within world scope | Denied, including edits to the active agent itself |
| Adopt another identity | identities.assume | Denied | Only permitted identities | Only if the human's grants permit it; no automatic inheritance |
| Manage roles/identities | identities.manage | Denied | Separately authorized administrator operation | Denied |
| Run specialist host tasks | Task-specific capability | Denied | Requires registered capability, scope and any required approval | Requires corresponding specialist/operator grants, scope and approvals |

Master Control does not receive an unrestricted Windows command channel. New capabilities are denied until explicitly added to policy. Private messages, imported documents and agent memories require their own scoped access rules; world.read does not imply access to all content.

A human who can enter Master Control still has only the active agent's effective permissions while acting as that agent. To edit as administrator, explicitly switch to Master Control through the session service. Autonomous agents cannot assume human identities.

## 5. Enforcement and editing sequence

Command registration can declare required capabilities for discoverability and early rejection. The editing service must enforce the same policy even if a caller bypasses the terminal dispatcher.

For both preview and apply:

1. Resolve a live authenticated session in the correct world and generation.
2. Verify effective capability and target scope before disclosing program details.
3. Normalize and validate the operation using the field contract.
4. At apply, recheck current session/policy, preview binding, and program revision before commit.
5. Serialize authorization validity and commit so logout/revocation cannot race between the final check and mutation.

The implementation must define a consistent authorization/engine lock order or equivalent serialized operation lease. A mutation already admitted to its atomic commit may finish; once logout/revocation returns, no new commit may be admitted under the invalidated generation. Avoid holding these locks across input, network calls, or awaits.

Missing, expired, locked, ended, revoked or incorrectly scoped contexts fail closed. Return a safe Forbidden result without protected target details. If the session is still valid but the program revision changed, return Conflict as defined in 1.9.1. Store detailed denial reasons only in appropriately protected diagnostics/audit data.

Edit previews bind to operator, session ID/generation, active identity, world instance, target program, normalized changes and expected program revision. Reauthentication or switching away and back requires a fresh preview. Permissions are checked again even if the preview was previously allowed.

## 6. Session transitions and task lifetime

- **Login:** establish the operator, then choose an identity they are permitted to adopt. Never default an unverified operator to Master Control.
- **Switch:** check identity adoption rights; update active identity, increment generation, discard pending previews, and rescope conversation/request context. The operator ID remains the same.
- **Logout:** invalidate the session and pending previews; clear interactive access. Return to the login view rather than restoring administrator rights.
- **Lock/expiry/revocation:** deny protected commands until the required authentication/authorization succeeds again.
- **Multiple terminals:** independent session IDs/generations; no global mutable active role. Shared-world edits still use program revisions and engine serialization.
- **Restart/world replacement:** invalidate old session/world-instance bindings. Persisted account records do not automatically restore logged-in sessions.

Switching into an agent initially opens an operator view; it does not automatically pause the agent or take ownership of its autonomous queue. Any later takeover/pause control needs an explicit authorized operation.

Queued work keeps its original initiating operator, active identity and scope; identity changes cannot retag or enlarge its permissions. Recheck authorization before queued work starts. Pending interactive edits are invalidated on logout/switch. Already committed changes are not undone; longer-running host tasks follow explicit cancellation and approval policies and retain original attribution.

## 7. Audit contract

Record structured events for login success/failure, logout/lock, identity switches, authorization denials, and administrative actions. Include:

- Event ID/type, request correlation ID, outcome and safe reason code.
- Human operator ID if verified, session ID/generation, and execution source.
- Active agent GUID or Master Control identity; previous/new identity for switches.
- World instance ID, target program ID, field keys and actual old/new values for committed edits.
- Program revision before/after, policy/capability checked, and relevant task/approval references.
- UTC event time plus simulation tick/elapsed time where applicable.

An autonomous action has an agent identity and no falsely attributed human operator. System ticks use SimulationSystem attribution rather than a fake Master Control session. Failed login records must not treat an entered username as a verified operator.

Do not retain credentials or secret tokens. Failure/no-op records must be distinguishable from committed changes. Durable storage, retention, protected log access and audit write-failure policy are Phase 2/agent integration work; an edit-result object alone is not a durable audit trail.

## 8. Verification contract

The editing suite covers default denial, forged references, agent denial, switching/generations, revocation ordering, preview isolation and atomicity with a test provider. Real credentials, expiry, multi-terminal session propagation and durable audit still require integration tests when implemented.

- Missing/forged session claims cannot authorize an edit.
- Authenticated Master Control can perform a valid scoped edit.
- The same operator acting as MAXIS cannot perform that administrative edit.
- MCP and autonomous agents cannot self-promote or approve their own escalation.
- Direct service calls without terminal dispatch still enforce authorization.
- Logout, expiry, identity switches and policy revocation invalidate pending edits.
- Switching away and back does not revive an old preview.
- Different terminals have isolated active identities while shared-world conflicts remain enforced.
- Renaming a program to Master Control does not grant privileges.
- A concurrent logout either follows an already admitted atomic commit or prevents it, without partial state.
- Audit distinguishes human-as-agent, Master Control, autonomous agent and simulation-system actions.
- Unauthenticated production configuration denies new admin operations; fake grants exist only in tests.

## 9. Completion evidence and remaining decisions

Reviewed the current TerminalCommand handler signature, CommandDispatcher routing, terminal/engine separation, and 1.9.1 preview and revision rules. Current handlers accept arguments only: the session-aware signature and trusted resolver described here are future implementation work.

This completes the six planning items in section 1.9.2. No source code was changed and no runtime tests were run. The subsequent coding task 1.9.3 is now complete; see section 10. Local credential choice, session timeouts/recovery, durable role storage and login UI remain open; none is represented as implemented authentication.

## 10. Editing boundary implementation — 2026-10-17

Section 1.9.3 adds EditSessionReference, ProgramEditIdentity, IProgramEditAuthorizer and IProgramEditLease for the editing boundary. A production provider must verify live authentication, capability and world scope and hold validity until the operation finishes; lock order is authorization lease followed by world/engine lock. The engine checks Master Control identity and preview bindings and defaults to denial when no provider is configured.

A test-only provider verifies revocation ordering, role switching and session generations. No production login, credential store, general session service, expiry mechanism or terminal session propagation is claimed. Those remain future integrations under this contract. The wider human/agent/system audit model remains planned; edit receipts currently capture human Master Control attribution for later audit storage.
