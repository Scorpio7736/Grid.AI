# Project GRID — Program Editing Rules

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
- "One-time" does not mean lost during save: once Phase 2 persistence exists, the current edited state is saved normally. Today's Genesis runtime still has no save support.

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

Previews are scoped to a world/session instance and must not survive process restart or world replacement. A future persisted revision does not make an old session preview reusable. The exact preview-token/confirmation command syntax belongs to later API/UI implementation.

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

Check access before disclosing protected target details. Once a commit starts, complete it atomically rather than cancelling midway. An edit does not advance simulation time or replace the engine's last tick result; it produces a distinct administrative edit result for later audit integration. Durable audit and persistence remain future work.

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

A trusted authorization provider must supply a live lease; the default denies administrative edits. Receipts are in-memory returned values, not persistence or a durable audit log. Forty-eight editing test cases passed as part of the 117-test solution run. Terminal editing and credential-based login are not implemented.
