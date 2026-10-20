# Project GRID - Host programs, lifecycles and resources

## Social civilization completion - 3.3 (2026-10-20)

**Implemented and verified.** Explicitly opted-in Agent/Special residents now have cooperative, competitive or reserved preferences; bounded relationships, memories and occupation history; and simulated district civic tokens with scarcity/recovery. Decisions run on simulation time, respect current work/travel and commit atomically with ticks. Host applications remain observation-only and receive no fictional social activity or resource consumption.

Schema v8 persists social state and partial rounds, upgrades existing worlds without inventing participants, and supports validated backup recovery. `social [id|name]` and program inspection provide read-only views. Trusted engine APIs configure social participation/pools; production login, terminal admin configuration, automatic agent creation, actual communication/delegation and Phase 3.4 sustained autonomy remain future work. **336 tests passed**, plus isolated terminal create/save/reopen. See [Social Civilization](Project_GRID_Social_Civilization.md) for complete rules, limits and evidence. Earlier dated statements that social functionality is pending describe previous milestones.

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

## Hardware city completion - 2026-10-19

**2.6 / GRID-REQ-004 implemented and verified.** First-time creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates bounded district/street/building-lot geometry and saves actual layout and provenance in schema v3. Later startups reuse the saved world without discovery or regeneration. Existing worlds are preserved; new generated cities start empty pending 2.7. Inventory is creation provenance, separate from live resource telemetry and application identity. Rendering, atmosphere, fashion and application-resident integration remain planned.

Verification: **250 tests passed** (14 Core, 106 Simulation, 74 Persistence, 56 Terminal); build had zero warnings/errors. A fresh Windows city and unchanged reopen were verified in a temporary directory. Use city to inspect saved generation details; --world with a new filename explicitly creates a separate world.

See [Hardware City Generation](Project_GRID_Hardware_City_Generation.md) for architecture, mappings, fallbacks, persistence and completion evidence. Earlier dated statements that hardware generation is planned or that normal creation always makes twenty fictional citizens describe previous milestones; this update supersedes those status statements while preserving their history.

## Resource/host foundation update - 2026-10-19

Section 2.4 now provides read-only host observations, explicit units/sources/freshness, fake-provider tests and a provenance-required additional-work resource-condition policy. Use resources for details or live for a compact CPU/RAM summary. Unsupported measurements and unknown application requirements remain unknown. Current observations are transient and cannot be edited into Windows allocations or restored from a save as fresh data.

Verification: 223 tests passed, build zero warnings/errors, and real Windows disposable-session sampling passed while simulation time stayed paused. See [Resources and Host State](Project_GRID_Resources_and_Host_State.md) for collector coverage, the exact policy and limitations. Application discovery/classification, per-application resource attribution and replacing application energy drain remain dependent on 2.7. Existing fictional Genesis citizens retain their old rules. Earlier deferred-telemetry statements below describe prior milestones, not this update.


## History implementation update - 2026-10-18

Section 2.3 now persists meaningful committed changes with world snapshots in schema v2. The history command supports entity/kind/severity/time filters and bounded paging. Routine Tick records are excluded; pending-buffer overflow is reported explicitly. Changing edits include attribution/context, but denied attempts and no-ops are not a comprehensive security audit. Structured operational errors are emitted to terminal output. See [History and Observability](Project_GRID_History_and_Observability.md) for retention, failure and API details.

Verification: 208 tests passed; solution build had zero warnings/errors; isolated terminal restart/query smoke passed. Earlier schema-v1 and deferred-history statements below describe the prior milestones. Live-view history remains process-local; the new saved-history path is separate. Application discovery, resource interpretation, resident classification and persistence filtering remain planned in 2.4/2.7.


**GRID-REQ-005 — planned, 2026-10-18.** This records the user's revised population and resource model. It supersedes earlier plans for a permanently saved population of ordinary fictional citizens. This update changes documentation only; the implementation still creates twenty fictional citizens, saves all of them, and uses the Genesis energy rules.

## Population contract

Ordinary GRID programs represent applications currently running on the PC. Starting an application brings its representative into the world; confirmed application exit removes that active representative. GRID does not populate the production city with arbitrary ordinary citizens to reach a fixed population target.

Agents and explicitly designated special programs are persistent residents. Their identity, role, configuration, authorized capabilities and applicable history survive restart. Persistence is an explicit entity classification; a process name, occupation, uniform or self-reported agent label cannot grant it or confer authorization. Named agents remain planned services, not implemented merely because this contract describes them.

Ordinary application residents are transient. Do not save their individual energy, position, needs, relationships or other resident state for resurrection on the next launch. A new application run gets a new runtime incarnation. Historical events can refer to a former incarnation without making it an active resident or retaining a full ordinary-program save record. Application grouping configuration and shared visual templates are separate from saved individual residents.

## Discovery and lifecycle

1. Load the saved city and persistent residents, retaining the durable simulation clock without offline advancement.
2. Discover currently running applications through a read-only host adapter.
3. Reconcile successful observations with the active population, then observe starts, exits and resource changes throughout the session.
4. Create residents for newly observed applications and remove them after confirmed exit. Restarting GRID must never restore an application merely because it ran during the previous session.

An application may own multiple processes. The proposed default is one application representative with associated child processes; exact grouping, multiple independent instances, background services and system-process inclusion require an explicit, tested policy. Avoid double-counting grouped resource use. Identity matching must distinguish process lifetimes and PID reuse; a PID or executable display name alone is insufficient.

Access-denied, incomplete or stale observations mean unknown availability, not confirmed application exit or zero resource use. Define a bounded reconciliation/grace policy. Short-lived applications and missed intervals must not produce invented activity. When GRID was closed, report only what is observed on restart; do not reconstruct unobserved activity or advance simulation time to fill the gap.

The telemetry adapter supplies observations; C# simulation operations serialize population changes with ticks, membership updates and snapshots. The viewer never enumerates processes to mutate the world independently. Fake providers support repeatable lifecycle tests without depending on applications installed on the test PC.

## Resources and energy

Resources describe what an application uses or needs to function and the relevant capacity or pressure of the host. Proposed categories are:

| Category | Meaning | World presentation |
|---|---|---|
| CPU | Processing activity and available processing capacity | Processing-sector work |
| RAM | Active memory usage and memory pressure | Workspace in the Memory Banks |
| GPU / VRAM | Supported graphics/compute and video-memory measurements | Graphics/compute district activity |
| Storage | Capacity, free space and read/write activity as distinct measurements | Archive capacity and transfer activity |
| Network | Connectivity and transfer activity | Gateway communications |

Keep measured usage, host capacity, pressure and configured/estimated requirements separate, with units, sampling time, source and freshness. Current usage is not proof of an application's exact minimum requirement. Missing measurements are unknown, never fabricated zeroes. Not every metric will be available per application; unsupported attribution must be visible. Do not equate all occupied RAM with harmful pressure or treat lack of network traffic as failure for an offline application.

Energy is a GRID interpretation of resource sufficiency or operating condition, not a literal Windows energy meter or proof of application health. A normalized 0–100 display may be retained, but its formula, category weights, thresholds, smoothing, recovery and unknown-data behavior must be specified and tested before implementation. Requirements must have stated provenance; an energy score must not imply certainty about unknown requirements.

The current fixed drain for Working/Moving and fixed Recharging gain are Genesis placeholders, not the target model for host application residents. Movement or merely remaining open must not drain an application's energy without a resource-based reason. Recharging, if retained as a presentation state, reflects recovery of the relevant condition rather than consuming an invented universal fuel pool. Persistent agents need an explicit resource policy for their actual services and tasks, not assumed per-character OS processes.

Earlier Energy/Materials/Data economy proposals are superseded as the primary host-resource model. Any later fictional construction or civic resource must be clearly separate, optional world mechanics; it must not claim to measure or allocate real PC resources.

Observation does not control the application. Low GRID energy must not close, suspend, throttle, restart or otherwise alter a host application. Such operations belong to a separately authorized task capability. GRID may budget its own work under host pressure. No permission is implied by an application appearing in the city.

## Persistence and migration

Save the city, districts, generated layout/provenance, persistent agents/special programs and durable world clock. Derive current district memberships for transient residents from the reconciled live population. Never leave a saved membership pointing to an omitted resident. Reconcile newcomers into valid districts without regenerating the city.

Section 2.2 currently persists every GridProgram; its implementation and verification remain valid for the old model. Add a versioned migration and explicit save/load filtering before claiming GRID-REQ-005 is implemented. Do not edit the released schema v1 migration or silently delete existing saved citizens. Define a documented legacy conversion/archive policy and preserve a recoverable original before conversion; legacy citizens must not be mislabeled as discovered applications or automatically promoted to special programs.

Backup recovery restores durable world state, then rediscovers live applications. It must not resurrect application residents from the backup. Historical lifecycle events, if retained by section 2.3, are distinct from active population state; define retention and minimal identifying metadata separately from sensitive command lines or document contents.

GRID-REQ-004 still generates the city once from motherboard, CPU, GPU, RAM, storage and network hardware. GRID-REQ-005 updates its occupants and observed resource conditions continuously. Application starts/exits and changing telemetry do not regenerate the saved city.

## Implementation placement and acceptance

- **2.3:** history can record starts, confirmed exits and meaningful resource changes with provenance; avoid logging every sample. Historical residents never become active through replay.
- **2.4:** read-only telemetry, resource definitions, requirement provenance and energy interpretation.
- **2.7:** entity classification, application grouping/lifecycle reconciliation, filtered persistence and legacy migration. Complete this foundation before extending Phase 3 population behavior.
- **Phase 3:** behavior and presentation consistent with real observed application state; persistent agents/special programs retain their intended autonomy. Fake populations remain test fixtures.
- **Phases 4–6:** agent delegation, identity and task authorization remain separate. An ordinary application representative is not automatically an authenticated agent.
- **Phases 7–8:** render the changing population and integrate startup/shutdown through the same authoritative engine.

Required verification includes launch/exit/relaunch, grouped processes and independent instances, PID reuse, denied/stale telemetry, unknown resource requirements, atomic membership updates, restart with a different application set, persistent-agent continuity, legacy migration/rollback, backup recovery followed by live reconciliation, and no host-app mutation from simulated shortages. Zero applications must be a supported state; twenty fictional citizens are a test fixture, not a production acceptance requirement.

**Documentation verification — 2026-10-18:** revised the shared design, checklist, persistence, resource, editing, terminal and visual planning documents. Preserved completed checklist entries and dated implementation evidence. No C# changes, schema migration or runtime tests were performed for this planning update. The prior 200-test result belongs to section 2.2 and does not verify GRID-REQ-005.

## Existing implementation retrofit - planned, 2026-10-18

The new requirement changes assumptions in completed modules; it does not invalidate their original implementation evidence. Keep completed checkboxes and dated tests intact. Track the following work under 2.7 and resource integration under 2.4, and verify each retrofit before claiming compliance.

| Existing area | Required adaptation | Foundation retained |
|---|---|---|
| 1.2 program model | Explicit application/agent/special classification, observation bindings and application-run identity distinct from durable resident identity | Validated fields, revision tracking and default-deny editing |
| 1.4 world management | Atomic application removal and reconciliation; remove district membership consistently; reject edits/transfers targeting expired incarnations | World ownership, membership invariants and shared engine lock |
| 1.4 Genesis factory / terminal startup | Retain twenty fictional citizens as an explicit demo/test fixture; production builds ordinary population from discovery, including a valid empty population | Deterministic fixtures and stable saved city |
| 1.5–1.6 clock and energy | Replace application work/movement drain and fixed recharge with the specified 2.4 resource-condition policy; do not manufacture a score when requirements are unknown | Fixed-step clock, no offline catch-up, serialized updates and consistent snapshots |
| 2.2 save/load and recovery | Filter resident persistence, add versioned classification/migration, preserve recoverable legacy saves and reconcile live applications after load/recovery | Transactions, validation, rolling backup, explicit recovery and orderly shutdown |
| 1.9.3 editing | Separate observation-owned and user-editable fields; expire incarnation-bound previews after exit/relaunch; ordinary edits remain transient | Typed field registry, revision conflict checks and authorization leases |
| 1.7 / 1.10 terminal | Display resident class, observation freshness, unknown values and lifecycle changes without inventing host behavior | Extensible commands, snapshot-based reads and bounded live-event display |
| Existing tests | Keep Genesis fixture tests; add lifecycle, persistence-policy, telemetry and editing regression cases | Historical completion evidence and reusable fake-provider/snapshot testing patterns |

### Dependency order

1. Define classification, durable versus runtime identities, field ownership and the legacy migration policy (2.7).
2. Implement safe removal/reconciliation with fake application discovery and membership/preview expiry tests (2.7).
3. Adapt persistence and startup together: versioned migration, filtering, demo/production distinction and recovery reconciliation (2.7). Do not switch production startup while save/load still treats all residents as permanent.
4. Connect read-only host discovery and resource telemetry; specify and test the energy policy before replacing the placeholder rules (2.4 and 2.7).
5. Update editing, inspection/live view and end-to-end verification across restart, recovery and changing application sets; only then extend Phase 3 population behavior.

These are dependency priorities, not a renumbering of the roadmap. Section 2.3 history work can proceed using the lifecycle contract, but events must not assume every program is durable. No code retrofit is completed by this documentation update.
