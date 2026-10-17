# Project GRID — Session Context and Authorization Contract

**Scope:** Section 1.9.2; GRID-REQ-003  
**Status:** Design complete; authentication, sessions and enforcement are not implemented  
**Recorded:** 2026-10-16  
**Related:** [Identity plan](Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md), [Program Editing Rules](Project_GRID_Program_Editing_Rules.md)

## 1. Identity model

Separate the authenticated operator from the identity used for an action:

- **Human operator:** stable local account ID established by a trusted authentication provider.
- **Active identity:** either the human's Master Control role or a registered agent program GUID. Master Control is not an agent program and must not require a fabricated program ID.
- **Effective permissions:** the intersection of operator grants, active-identity policy, and resource/world scope. Host actions also require their existing capability and approval checks.
- **Execution source:** HumanSession, AutonomousAgent, or SimulationSystem. Autonomous and simulation execution contexts are issued by their own trusted services, not created by typing a terminal identity name.

"Master Admin" and "Master Control" are display aliases for the same proposed administrator role. Display names and occupations never determine permissions. ARCHON remains an agent coordinator, not the human administrator.

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

For 1.9.3, design the editing API to depend on an authorization/session resolver. A test fixture may supply an explicit fake resolver. The normal application must deny administrative edits when no authenticated session provider is configured; do not silently create a production Master Control session to make tests or demos convenient. Genesis commands can retain their current behavior until session integration, but newly added administrative edits must not be exposed without enforcement.

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

## 8. Required verification scenarios for later implementation

- Missing/forged session claims cannot authorize an edit.
- Authenticated Master Control can perform a valid scoped edit.
- The same operator acting as FORGE cannot perform that administrative edit.
- ARCHON and autonomous agents cannot self-promote or approve their own escalation.
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

This completes the six planning items in section 1.9.2. No source code was changed and no runtime tests were run. The next coding task is 1.9.3. Local credential choice, session timeouts/recovery, durable role storage and login UI remain open; none is represented as implemented authentication.
