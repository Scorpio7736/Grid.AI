# Project GRID — Terminal Login and Master Control Plan

## Communications foundation completion - 4.1 (2026-10-20)

**Implemented and verified.** Grid.Communication now defines recipient/message/conversation models and a local hub contract implemented by the simulation host. Exact name/full-ID routing, explicit participant identities, start/send/end/read operations, ordered bounded messages and schema v10 transactional transcript persistence are available. Conversations survive restart and backup recovery even when a historical application resident is absent; new sends require live original program identities.

**346 tests passed.** See [Communications Foundation](Project_GRID_Communication_Foundation.md) for APIs, identity boundaries, bounds and recovery evidence. These trusted local APIs store messages without authenticating the caller, executing text, claiming external delivery or generating replies. Terminal chat is still 4.2; voice, real delegation, LLM dialogue and production login remain planned. Earlier dated pending-communication/schema statements describe previous milestones.

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

Future login-as-agent applies to persistent trusted agents/special programs. An ordinary running application is not automatically a login principal. Application inspection must distinguish transient representation from agent identity; observation or a Master Control label does not authorize stopping/throttling host apps. Define incarnation expiry and field editability before adding such controls.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


## Implementation status - 2026-10-17

The 1.9.3 backend editing boundary is implemented and denies access by default. There is no production authentication provider, login/identity-switching UI, or terminal edit command. Existing Genesis inspection/control commands remain available without login. Acceptance criteria below concern the future integrated terminal experience; API completion evidence is recorded separately. Live view is implemented under checklist 1.10 as of 2026-10-18. It currently shares the existing Genesis world visibility; consistent session-based filtering of state and events remains deferred until login is implemented.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Requirement:** GRID-REQ-003 — Terminal identities and operator sessions  
**Status:** Login and terminal integration planned; backend editing API implemented after Genesis
**Recorded:** 2026-10-16  
**Working name:** Master Control (also described as master admin; final label remains open)

## Goal

Eventually, opening a GRID terminal should begin with a login. The User can enter as their own Master Control identity or operate through a named agent program: ARCHON, FORGE, SENTINEL, ARCHIVIST, VOLT, or RELAY. Each agent session provides that program's view of the world and its available actions.

The identity list should be expandable as new agent programs are added. This is a future addition to the existing terminal, not a change to the completed v0.1.0-genesis release.

## Session experience

1. Open a terminal and authenticate the human operator using a local method to be decided.
2. Choose an authorized identity: Master Control or a named agent.
3. Show the active identity in the prompt and status output, such as `master-control>` or `forge>`.
4. Display commands, tasks, messages, history, and relevant world information for that identity.
5. Execute actions within the selected identity's capabilities.
6. Log out or switch identities through an explicit session action. Switching to Master Control requires its authorization.

Proposed commands include `login`, `logout`, `whoami`, and `switch`; exact syntax remains a design decision. Logout ends access through that session, rather than silently restoring Master Control. Whether the headless world continues after logout will be defined with service lifecycle work.

## Master Control and agent sessions

| Identity | Intended experience |
|---|---|
| Master Control | The User's top-level GRID administration identity: inspect the whole system, manage identities and role policies, review activity, approve scoped tasks, and enter authorized agent sessions. |
| ARCHON | View coordination, assignments, briefings, and approval requests. |
| FORGE | View development projects, builds, tests, and authorized code tasks. |
| SENTINEL | View security findings and permitted monitoring actions. |
| ARCHIVIST | View archives, storage, retained context, and authorized file tasks. |
| VOLT | View resource readings and GRID performance controls. |
| RELAY | View communications, conversations, notifications, and permitted messaging actions. |

These are intended role views, not claims that these services already exist. Master Control is the human administrator role within GRID; ARCHON remains a separate agent responsible for coordination. Master Control does not automatically grant Windows administrator privileges.

## Identity and action model

Section 1.9.2 now defines the [Session Context and Authorization Contract](Project_GRID_Session_Context_and_Authorization.md), including trusted session issuance, effective permissions, identity switching, preview invalidation and audit attribution. The editing authorization boundary is now implemented in 1.9.3 with default denial; authentication and general session lifecycle remain pending.

- Keep the authenticated human operator separate from the active program identity. A human operating as FORGE is recorded as that human acting through FORGE, rather than as an autonomous FORGE action.
- Carry a session context through command dispatch and task execution, including operator ID, active identity ID, and effective capabilities.
- Agent sessions expose their own permitted views and actions. The effective permissions must fit both the operator's authorization and the chosen identity's role; adopting an agent identity does not inherit Master Control powers.
- Enforce authorization in the underlying service/capability layer as well as in command presentation. Hiding a command from help is not access control.
- Existing host task approvals and scoped workspace rules still apply. An autonomous program cannot promote itself to Master Control or approve its own escalation.
- Record login/logout, identity switches, actions, outcomes, and the responsible operator/agent in local audit history. Do not record authentication secrets.
- Keep conversations, session context, and access to retained information correctly scoped when switching identities.
- Define how manual operation interacts with an agent's autonomous task queue, avoiding duplicate execution or conflicting control.

## Planned Master Admin program editing

**User-requested extension to GRID-REQ-003 — planning only.**

The User, signed in as Master Admin / Master Control, should be able to inspect and edit programs and their values directly from the terminal. This includes civilian programs and editable simulation/profile fields of named agents.

Initial editable fields are name, occupation, energy, status, and district assignment. The completed section 1.9.1 design is specified in [Program Editing Rules](Project_GRID_Program_Editing_Rules.md), including validation, one-time behavior, previews, conflict detection, and expandable field metadata. The editing API is now implemented in 1.9.3; login and terminal editing integration remain pending.

Proposed usage (illustrative syntax; not currently implemented):

```text
program edit <program-id> energy 75
program edit <program-id> name "New Name"
program edit <program-id> status Recharging
program move <program-id> <district-id>
```

- Provide a way to list editable fields and inspect current values.
- Enforce Master Admin / Master Control authorization in the editing service. An agent session does not gain administrative editing authority merely because the human can also enter Master Control.
- Validate field types and domain rules: finite energy within its supported range, valid statuses, and existing destination districts. Invalid edits leave state unchanged.
- Route district changes through world-management operations to preserve membership and population. Stable IDs and ownership references are not ordinary editable fields.
- Keep profile/simulation edits separate from credentials, role permissions, and host capabilities. Editing a name or occupation must not change authorization.
- Preview the target and old/new values, then report the actual committed result. The exact confirmation workflow remains a UI design choice.
- Serialize edits with simulation ticks through validated engine operations. Use per-program revision checks to reject stale previews; the User may explicitly pause the simulation before preparing an edit.
- Audit the authenticated operator, target, old/new values, outcome, and simulation timestamp. Integrate saved edits with Phase 2 persistence.
- The first version uses one-time edits: energy or status may change again during subsequent updates. Persistent overrides are deferred; previews must explain this behavior.

### Program-editing acceptance criteria

- [ ] Master Control can inspect editable fields and change supported program values.
- [ ] Agent and logged-out sessions cannot invoke administrative edits.
- [ ] Invalid or conflicting edits do not partially change a program.
- [ ] District edits preserve membership and total population.
- [ ] Edits appear in subsequent inspection and survive save/reload once persistence exists.
- [ ] Audit records preserve operator attribution and old/new values.
- [ ] Additional editable fields can be registered without rewriting the terminal loop.
- [ ] Tests cover authorization, validation, tick coordination, membership, and audit attribution.

## Expandable implementation direction

Extend the existing command registry with session context and capability metadata. Build identity registration and role policies separately from console input/output, so future voice, desktop, or city interfaces can use the same authentication and authorization services.

Use stable identity IDs rather than hardcoding privileges to display names. Keep credential verification behind a local authentication interface so the authentication method can be selected later without rewriting command handlers. A cosmetic identity selector may be useful for a prototype, but must be labeled as such until authentication and enforcement exist.

## Roadmap placement

- **Phase 2 — Persistence:** plan durable operator/agent identities, role policy records, and audit storage alongside the existing schema. Persist session-related data only according to a defined retention and restart policy.
- **Phase 4 — Communications:** implement the login/session experience, identity-aware prompt, whoami/logout/switch behavior, and scoped conversation views.
- **Phase 5 — System Agents:** enforce role capabilities for actual agent actions, add Master Control administration and approvals, and verify human-versus-autonomous attribution.
- Add a future milestone or revise placement if the User wants login earlier. Do not reopen completed Genesis items or imply login is already available.

## Acceptance criteria

- [ ] A new terminal starts in an unauthenticated state and presents a login path.
- [ ] The authorized User can enter Master Control or each registered agent identity.
- [ ] Prompt and whoami clearly identify the operator and active role.
- [ ] Agent views show actual permitted state, messages, tasks, and history.
- [ ] A valid scoped action succeeds; an unauthorized action is rejected by the service layer.
- [ ] Logout clears session access; switching identities does not leak prior context or privileges.
- [ ] Audit records distinguish autonomous actions from a human acting as an agent.
- [ ] A new agent identity and its command capabilities can be registered without rewriting the terminal loop.
- [ ] Tests cover failed login, role boundaries, switching, logout, attribution, and task-control conflicts.
- [ ] Core login and identity handling work locally without required paid services.

## Decisions still open

- Final display name: Master Control or Master Admin.
- Authentication method: existing Windows identity, a separate local credential, or another local method.
- Initial design: authenticate the human, then select an authorized agent identity; credential provider and any later separate agent credentials remain undecided.
- Initial single-user versus future multi-user support.
- Session expiry, locking, recovery, and concurrent terminal policy.
- Initial world-control policy is defined in the session contract: Master Control can control simulation and edit programs; later role-specific exceptions require explicit policy.
- Initial agent adoption opens an operator view without automatically pausing autonomous tasks; explicit takeover controls remain future work.
