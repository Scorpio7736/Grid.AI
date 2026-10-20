# Project GRID - Local Login and Identity Sessions (4.2A)

**Implemented and verified - 2026-10-20**

## Authentication choice and startup

The production terminal uses the current Windows process token's user SID as its local authentication source (WindowsIdentity.GetCurrent), never an environment variable or a user-typed identifier. The administrative role is **Master Control**. No GRID password is collected, no Windows elevation occurs, and an agent identity is a role selected by the authenticated human rather than a separate OS account.

Every launch begins locked. Use `login master`; on a new/unbound world this binds its owner to the verified Windows SID. Existing bound worlds require an exact match. For saved worlds the owner registry is saved successfully before login grants permissions. An unavailable OS identity, different owner or failed owner save cannot grant access. Non-Windows production login currently fails closed; alternate credential providers are not implemented. Test providers are injected only in tests.

This trusts the already signed-in Windows account and the local desktop operator; it is not a second-factor password prompt or protection against malicious code running as that account. Protect the world file with normal OS permissions. Trusted in-process engine APIs remain service boundaries, not network-facing authentication endpoints. A user who can directly alter the database, run an older/custom binary, or use the same Windows account is outside this terminal authorization boundary. Account recovery/owner transfer and remote multi-user access remain future work.

The prompt shows locked, Master Control or a short active-agent ID. Startup world details are shown after login. Public commands are help, clear, exit, login, logout and whoami. Exit/Ctrl+C still closes the local host and saves normally; this is not a separate always-running multi-user service. Telemetry/autosave can run while locked. `--start` is deferred until the first successful login; later logout does not stop an already running simulation.

## Commands and session context

```text
login master
whoami
connect                         # lazily creates/reuses ARCHON under Master Control
register-agent ARCHON           # registers an existing Agent resident
switch ARCHON
self
switch master
logout
```

`register-agent <id|name>` requires Master Control and an existing Agent kind. Renaming a resident, editing an occupation or calling an application ARCHON does not confer rights. Registry capacity is 256 agents. Additional agents can be registered without modifying the input loop. Creation of arbitrary agents and registration removal/policy editing are not provided by this milestone.

Sessions carry the stable world-local operator ID, random session ID, monotonically increasing generation, active agent ID and capability policy version. They expire eight hours after login. Expiry blocks protected commands and edit leases; login again renews authentication and clears old context. Logout/switch invalidates pending previews and clears the active chat selection without deleting or ending its transcript. A restored world always starts with a new unauthenticated session.

The authenticated Windows owner can explicitly switch back to master. While an agent is selected, Master Control capabilities are disabled; this does not mean the human has lost their ability to deliberately choose master again. No autonomous agent task is given access to this terminal identity-selection path.

## Capabilities and views

Command metadata declares capabilities; extensions default to world.admin. Help filters by current capabilities, and dispatch checks the capability again under the session lock. Hiding help entries alone is not enforcement. Commands, switches and edit authorization leases use authorization-before-engine lock ordering.

| Identity | Allowed views/actions |
| --- | --- |
| Locked | Login/session description, help, clear, local-host exit |
| Master Control | World administration, global state/history/live view, simulation control, NGE, agent registration, program-edit previews/confirmation, chat |
| Registered agent | Session description/switch/logout, own resident state, conversations in which its identity participates, directed dialogue |

Agent sessions cannot use global status/programs/history/live, simulation controls, NGE, registration or editing. `self` shows own resident identity/location/action. Real task services are not yet implemented and are reported as unavailable; no fictional task ownership is inferred. Conversation list/read/resume are scoped to the active participant, including both sides of conversations shared with the operator. Unrelated agents cannot read those transcripts. Agent dialogue `status` returns only labeled own-state information rather than global counts. Directed recipient identity lookup remains available for chat.

Live view checks ongoing authorization about once per second and returns when it expires/is revoked. NGE checks authorization at dispatch, confirmation and before deletion, while retaining the existing interactive y/n warning. Login grants no Windows administrator rights or OS task-execution capabilities.

## Master Control editing

```text
edit <program-id|quoted-name> <field> <value>
apply-edit <preview-id>
cancel-edit
```

Each edit prepares one typed field through the existing expandable field registry. The preview shows old/new values, resident revision and a unique confirmation ID. Only apply-edit with that current ID commits. New previews replace the old one; logout/switch clears it; expiry/revocation or concurrent resident changes prevent committing it. Multi-field service edits remain supported by the existing backend, while this initial terminal command exposes one field per preview.

Fields: name, occupation, energy, status, district. District values use full district IDs. Energy/status remain observation/service-owned for non-Legacy residents and are rejected; this protects actual application measurements and configured agents. Assigned workstation travel must be released before manual district reassignment. There is no new release command in this milestone. Edits remain one-time assignments, not sustained overrides, credentials or permissions. Later simulation/configuration can change those values.

IProgramEditAuthorizer now has a production local-session adapter. It verifies current owner, expiry, role, session/generation and world-instance scope; its lease holds revocation/switching until the edit completes. Default engine instances still deny edits unless given an authorizer. A preview can never carry authorization forward after logout or switching. Successful receipts report before/after revisions and persist through the existing transactional world/history save.

## Storage and audit

Schema v11 adds WorldIdentities: a bounded JSON row containing owner SID and registered Agent IDs. Existing migrations retain their checksums. Upgraded worlds are initially unbound and obtain the owner on first successful Windows login. Registry validation rejects missing/non-Agent registered residents. Owner bindings/registered agents survive save and backup recovery; session credentials, active identity and edit previews do not.

OwnerBound, AgentIdentityRegistered and IdentitySession record identity lifecycle/command capability outcomes. Session records include operator ID, active agent ID, generation, action name and allowed/denied outcome; command arguments, transcript text and credentials are not logged. CommandAllowed means capability dispatch was permitted, not that a domain result succeeded; domain failures may be reported by their own result/event. Existing Edit events include request identity, revisions and changed fields. The existing bounded journal/autosave loss window applies to audit records. Agent registration and edits save on normal autosave/exit; owner binding is saved before granting production login.

The same world database/backup/NGE boundaries apply to the identity registry. A pre-login --recover operation remains a local file-recovery operation governed by OS file access; it does not grant a logged-in session or bypass the restored owner's login check. No transcript encryption or tamper-evident audit is claimed.

## Completion verification and limits

**362 tests passed:** 14 Core, 155 Simulation, 90 Persistence, 103 Terminal. Each test project ran with `dotnet test <project>/<project>.csproj -c Verification --no-restore`. New coverage includes locked commands/scoped help, default-deny extensions, wrong/missing OS identity, owner restore, registered agent restrictions, unrelated transcript isolation, chat/preview reset, service-level edit denial, stale leases, expiry, failed owner persistence, observation-owned fields, schema v10 migration and registry/edit backup recovery. Existing editing tests retain invalid values, concurrency, revision, atomicity and membership coverage.

A real Windows-token terminal smoke used a disposable world: denied pre-login status; login; create/register ARCHON; switch to agent and deny pause; return to master; preview/confirm an occupation edit; logout and deny status; restart requiring login; verify retained edit/registration and agent switching. The user's default save and running GRID instance were untouched. Build/restart the normal launcher target to use this login flow.

Remaining: independent passwords/MFA or alternate OS providers, owner-transfer/recovery UI, agent deregistration/custom policy UI, transcript retention UI, real task services and their scoped views, autonomous agent execution, remote clients and stronger storage protections. Earlier dated notes that login/admin editing are planned remain historical milestones; this implementation supplies the Windows local-terminal scope described here.
