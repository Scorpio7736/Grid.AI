# Project GRID — Program Editing Rules

## Movement and occupations completion - 3.2 (2026-10-20)

**Implemented and verified.** Saved city roads now support deterministic district travel for explicitly configured Agent/Special residents assigned to compatible capacity-one workstations. Five simulated seconds per edge; no work progress while traveling; shift/resource uncertainty freezes travel. Trusted assignment/release APIs preserve world ownership and revision checks. `jobs` and `activity` add read-only workstation and measured application views. Host applications acquire no fictional civic jobs.

Schema v7 preserves station assignments, remaining routes and edge timing in existing behavior JSON, validates them on restore and performs no offline catch-up. Older unassigned routines retain their 3.1 behavior. No agent population or interactive admin job configuration is added. **323 tests passed** in isolated Verification builds; disposable jobs/activity/save/reopen smoke passed. A running user Release session locked its binaries and was left untouched; rebuild the normal target after closing it to use the changes. See [Movement and Occupations](Project_GRID_Movement_and_Occupations.md) for policy, limits and full completion evidence. Earlier statements that movement/workplaces are pending refer to previous milestones.

## Latest population revision - 2026-10-19

The current population includes discoverable installed applications and current-session background processes. States are **Sleeping**, **On call**, **Running**, or **unknown** when evidence is insufficient. This supersedes earlier windowed-only/removal-on-exit descriptions below; those remain historical completion records. Ordinary application state is still rediscovered rather than persisted, and the saved city is reused. Full-machine coverage is not complete: see [Installed Application Availability](Project_GRID_Installed_Application_Availability.md) for implemented scope, Windows inventory limits, remaining work and **290-test** verification.

## Program behavior completion - 2026-10-19

**3.1 implemented and verified.** Non-demo residents now have deterministic needs/goals/actions. Applications reflect fresh or unknown observations; explicitly configured Agent/Special residents perform resource-gated simulated occupation work/rest on simulation-time shifts. Schema v5 persists their routines, sourced budgets and progress without restoring live observations or adding offline work. Use behaviors and inspect for last-tick behavior. No OS task execution, movement or named agent services are claimed.

See [Program Needs and Behavior](Project_GRID_Program_Behavior.md) for policy, APIs, limits and completion evidence: **278 Release tests passed**, build zero warnings/errors, disposable terminal smoke passed. Debug output was locked by running user sessions, which were left running. Earlier dated deferred-behavior/schema statements describe previous milestones; existing history and completed scope are preserved.

## Application resident completion - 2026-10-19

**2.7 / 2.7.1 implemented and verified:** production population now follows current-session windowed executable groups through read-only discovery and serialized lifecycle reconciliation. Application residents are transient; Agent/Special residents persist in schema v4. Before production conversion, legacy fictional citizens are preserved in a uniquely named validated archive. City/layout and clock remain intact. Unknown resource requirements display unknown condition; no host-control authority is implied.

Verification: **268 tests passed**, build zero warnings/errors, and a disposable Windows startup/reopen detected nine application groups. See [Application Residents Implementation](Project_GRID_Application_Residents_Implementation.md) for grouping limits, discovery/freshness, storage/migration, editing, resource interpretation and completion evidence. Earlier dated statements that 2.7 is planned, production cities stay empty, or all residents persist describe previous milestones. Agent delegation/services, login, rendering and additional per-application telemetry remain future work.

## Resource/host foundation update - 2026-10-19

Section 2.4 now provides read-only host observations, explicit units/sources/freshness, fake-provider tests and a provenance-required additional-work resource-condition policy. Use resources for details or live for a compact CPU/RAM summary. Unsupported measurements and unknown application requirements remain unknown. Current observations are transient and cannot be edited into Windows allocations or restored from a save as fresh data.

Verification: 223 tests passed, build zero warnings/errors, and real Windows disposable-session sampling passed while simulation time stayed paused. See [Resources and Host State](Project_GRID_Resources_and_Host_State.md) for collector coverage, the exact policy and limitations. Application discovery/classification, per-application resource attribution and replacing application energy drain remain dependent on 2.7. Existing fictional Genesis citizens retain their old rules. Earlier deferred-telemetry statements below describe prior milestones, not this update.


## History implementation update - 2026-10-18

Section 2.3 now persists meaningful committed changes with world snapshots in schema v2. The history command supports entity/kind/severity/time filters and bounded paging. Routine Tick records are excluded; pending-buffer overflow is reported explicitly. Changing edits include attribution/context, but denied attempts and no-ops are not a comprehensive security audit. Structured operational errors are emitted to terminal output. See [History and Observability](Project_GRID_History_and_Observability.md) for retention, failure and API details.

Verification: 208 tests passed; solution build had zero warnings/errors; isolated terminal restart/query smoke passed. Earlier schema-v1 and deferred-history statements below describe the prior milestones. Live-view history remains process-local; the new saved-history path is separate. Application discovery, resource interpretation, resident classification and persistence filtering remain planned in 2.4/2.7.


## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

Current editing APIs remain unchanged. Future host usage, process lifetime and measured condition fields are observation-owned; editing a label or energy value must not falsify telemetry or control Windows. Ordinary resident edits are transient and expire with that incarnation; persistent-agent/special-program state is saved. Revalidate previews after application exit/relaunch and expose future field editability explicitly.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


## Implementation status - 2026-10-17

Sections 1.9.1 and 1.9.3 are complete as design and API implementation respectively. The current field registry, preview/apply behavior, revisions and authorization lease are implemented; terminal-generated editing help, confirmation commands, production login and durable receipts remain future integration.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Scope:** Section 1.9.1; GRID-REQ-003  
**Status:** Editing API implemented and verified in 1.9.3 on 2026-10-17; terminal editing/login remain deferred
**Recorded:** 2026-10-16  
**Related plan:** [Terminal Login and Master Control](Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md)

## 1. Initial field contract

All edits target one existing program by its stable GUID. A UI may resolve a name first, but must reject ambiguous matches and show the resolved ID in its preview. Field keys ignore case and are normalized to the canonical keys below.

| Key | Model property / type | Validation and normalization | Meaning |
|---|---|---|---|
| `name` | `Name` / string | Trim leading/trailing whitespace; require 1–80 UTF-16 code units after trimming; reject control characters, including tabs, newlines and escape characters. Preserve case, internal spaces and Unicode text. | Display name; duplicate names are allowed, with GUIDs used to disambiguate. |
| `occupation` | `Occupation` / string | Same text rules as name, with a 1–120 code-unit limit. | Descriptive occupation; does not grant a role or capability and does not start a job. |
| `energy` | `Energy` / float | Finite value in the inclusive range 0–100. Text input accepts a decimal using a dot separator and optional sign; no thousands separators, exponent notation, NaN or infinity. Reject out-of-range values before conversion; store the resulting finite float. | Current fictional energy; reject rather than clamp administrative input. |
| `status` | `Status` / GridProgramStatus | Accept a named value, case-insensitively: Idle, Working, Moving, Recharging or Offline. Reject numeric enum input, undefined values and combined names. | Current simulation state; subsequent energy updates follow the engine's existing status rules. |
| `district` | `CurrentDistrictId` / Guid | Require a non-empty GUID identifying a district in the same world. A UI can resolve an exact district name to a GUID before submitting; ambiguous or missing matches fail. | Immediate administrative relocation through world transfer rules. |

Text lengths are measured using the .NET string length convention for an unambiguous initial implementation. Trimming is allowed normalization, not silent truncation. Null, empty, whitespace-only text and embedded controls are invalid. Unknown field keys and duplicate keys in one request are errors.

Energy uses the current float model. Preview/result formatting must show the actual stored float with invariant round-trip precision, rather than the terminal's abbreviated energy display. For example, a value too small to survive float conversion is previewed as its stored result, not as a falsely precise decimal.

## 2. Identity and invariants

- Program GUID, world ownership, district membership lists, simulation clock, and revision metadata are not editable fields.
- Credentials, security roles, effective capabilities and host-task approvals belong to separate administration APIs.
- Renaming FORGE or changing an occupation to "Master Control" never changes authorization. Stable identity IDs carry identity; display text does not.
- Named agents and civilians share these profile/simulation rules where applicable. Setting Offline does not stop a real Windows service or cancel an agent task. Setting Moving does not create a journey.
- District reassignment updates the source membership, destination membership, and program location together. It preserves total population and does not implicitly change status or energy.
- Same-value edits and moves to the current district return `NoChange`; they do not emit a fabricated change or increment the program revision.

## 3. One-time edits and future overrides

Version 1 supports one-time state assignments only. An edit changes the present value; it does not pin it or disable future behavior.

- Name and occupation remain until another authorized update changes them.
- Energy continues to follow normal simulation rules on the next tick.
- Status remains until another authorized command or future behavior system changes it.
- District remains until a later move. An administrative relocation does not create a travel route.
- A future persistent override would need a separate explicit policy, duration/release behavior, and visible indicator. Pinning is deferred; it must not be inferred from an ordinary edit.
- "One-time" does not mean lost during save: section 2.2 saves current edited values and revisions normally. Edit receipts and live-event summaries remain transient until durable audit is implemented.

Example: editing a Working program to energy 75 immediately yields 75; its next one-second working tick yields 74. Editing its status to Recharging lets later ticks restore energy according to the engine rules.

## 4. Preview and conflict contract

The implemented API has separate prepare/preview and apply operations:

1. Resolve the target GUID, check eligibility/authorization, normalize inputs, and validate the entire request.
2. Under the engine's mutation lock, capture the program revision and actual old values. Return a preview showing the target ID/name, canonical fields, stored proposed values, one-time behavior, and any no-op fields.
3. Apply the exact prepared changes using the captured revision. Recheck authorization and field/world validation at apply time; a preview is not an authorization grant.
4. If the target program revision differs, return `Conflict` with no changes. Require a fresh preview. Do not automatically overwrite or silently retry.
5. Commit all fields in the request as one operation under the same lock used by ticks and district transfers. Return the actual old/new values, resulting revision, and commit tick/time.

A monotonically increasing per-program revision is implemented for this API. Increment it once per successful mutation transaction that changes any target program value, whether caused by an administrative edit, energy tick, or world transfer. A change followed by a reversal still advances the revision. No-op transactions do not. Revisions and their mutation paths are implemented in 1.9.3.

An unrelated citizen changing, or a tick that leaves the target program unchanged, does not invalidate the preview. A target energy change invalidates even a pending rename: the first version intentionally uses whole-program conflict detection.

The terminal must not pause or resume simulation implicitly. To avoid repeated conflicts on active citizens, the User can explicitly pause, prepare and apply an edit, then resume. Other sessions can still edit while paused, so revision checks remain necessary.

Previews are scoped to a world/session instance and must not survive process restart or world replacement. A persisted revision does not make an old session preview reusable. The exact preview-token/confirmation command syntax belongs to later API/UI implementation.

## 5. Invalid input and atomic results

One request targets one program and contains one or more unique fields. Multi-program bulk editing is deferred. Validate all fields and cross-field/world constraints before mutation; failure must not update any requested field, membership, revision, clock, or last committed tick result.

Implemented result categories (prepare returns `Prepared` with a preview and no mutation):

| Result | Meaning |
|---|---|
| `Applied` | At least one value changed; actual changes and new revision returned. |
| `NoChange` | All requested values already match after normalization. |
| `ValidationFailed` | Invalid key, duplicate field, invalid value, or invalid destination; field-specific reasons returned. |
| `NotFound` | Target program does not exist in the selected world. |
| `Forbidden` | Session lacks required editing authority; return no protected program details. |
| `Conflict` | Target changed since preview or preview belongs to a different world/session instance. |
| `Cancelled` | Request cancelled before commit; no partial application. |

Check access before disclosing protected target details. Once a commit starts, complete it atomically rather than cancelling midway. An edit does not advance simulation time or replace the engine's last tick result; it produces a distinct administrative edit result for later audit integration. Durable audit remains future work; section 2.2 persists the resulting world values and revisions.

## 6. Expandable field metadata

Use an explicit registration contract rather than setting arbitrary properties by reflection. Each field descriptor defines:

- Stable key, display label, description, and schema/descriptor version.
- Typed value kind, allowed values/ranges/text constraints, and normalization rules.
- Parser and invariant formatter for terminal input and accurate previews.
- Required capability reference, applicability, and one-time behavior description.
- Typed value access, validation, and staged mutation support; district uses a transfer operation.
- Equality/no-op comparison after normalization.

Registry startup rejects duplicate keys (case-insensitive). Unknown keys remain rejected until explicitly registered. The future terminal editing UI must list descriptors and generate field help from this registry; it does not duplicate validation rules. The engine/application service remains authoritative for validation and authorization.

Adding a field requires a descriptor, domain mutation support, and focused tests, but no rewrite of the terminal input loop. Metadata describes capabilities; it does not itself authenticate users or grant permissions.

## 7. Verification scenarios for implementation

These scenarios define the 1.9.3 validation contract. The implemented editing suite now passes 48 cases; see section 9 for completion evidence:

| Input/scenario | Expected result |
|---|---|
| Name `"  Citizen Alpha  "` | Preview/store `Citizen Alpha`. |
| Empty name, embedded newline/escape, or 81-code-unit name | Validation failure; no mutation. |
| Occupation of 120 code units / 121 code units | Accept / reject. |
| Energy 0, 75.5, 100 | Accept. |
| Energy -1, 100.01, NaN, infinity, or `1,000` | Reject without clamping. |
| Status `recharging` / `1` | Normalize to Recharging / reject numeric enum. |
| Valid district / unknown or empty district GUID | Consistent transfer / reject. |
| Rename plus invalid energy in one request | Neither field changes. |
| Target energy changes after preview | Conflict; no rename or energy overwrite. |
| Target changes and later returns to the old values | Conflict because revision advanced. |
| Another program changes; target unchanged | Preview can still apply if all other checks pass. |
| Same-value update | NoChange; revision and population unchanged. |
| Rename to an agent/admin label | Display changes only; privileges unchanged. |
| Apply while ticks are active | Serialized commit or conflict; no partially visible edit. |

## 8. Design completion evidence

Section 1.9.1 is documentation/design work. The contract was checked against the current `GridProgram` properties, enum values, energy behavior in `GridSimulationEngine`, membership rules in `GridWorld`, and the terminal's existing name/ID lookup behavior. Text limits, field metadata, revisions and the editing workflow were design decisions at 1.9.1 completion; enforcement is now implemented in 1.9.3.

No source code was changed and no new runtime tests were run for this section. Session contracts were subsequently completed in 1.9.2, and implementation/testing completed in 1.9.3. This paragraph records the earlier design-only milestone.

## 9. Implementation update — 2026-10-17

The 1.9.3 API now implements prepare/apply, typed values, validation, revisions, atomic commits, scoped immutable previews and committed receipts. The field registry supplies metadata and decimal parsing; energy text has a 1024-character limit. Formatting expands scientific notation to a precise decimal accepted by the parser. Applied and no-op previews are consumed once; reusing one returns Conflict. The additional Prepared outcome returns a preview without mutation.

The engine shares the world's synchronization lock. Direct writes to world-owned program values throw; unowned initialization remains available. The Core-to-Simulation internal mutation boundary is trusted application code, not an authentication mechanism.

A trusted authorization provider must supply a live lease; the default denies administrative edits. Receipts are in-memory returned values, not persistence or a durable audit log. Section 1.10 now records applied edit summaries in a separate bounded live-event buffer; no-op and failed edits do not add committed-change events. Forty-eight editing test cases passed as part of the 117-test solution run. Terminal editing and credential-based login are not implemented.
