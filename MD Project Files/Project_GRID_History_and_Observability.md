# Project GRID - History and observability

## Communications foundation completion - 4.1 (2026-10-20)

**Implemented and verified.** Grid.Communication now defines recipient/message/conversation models and a local hub contract implemented by the simulation host. Exact name/full-ID routing, explicit participant identities, start/send/end/read operations, ordered bounded messages and schema v10 transactional transcript persistence are available. Conversations survive restart and backup recovery even when a historical application resident is absent; new sends require live original program identities.

**346 tests passed.** See [Communications Foundation](Project_GRID_Communication_Foundation.md) for APIs, identity boundaries, bounds and recovery evidence. These trusted local APIs store messages without authenticating the caller, executing text, claiming external delivery or generating replies. Terminal chat is still 4.2; voice, real delegation, LLM dialogue and production login remain planned. Earlier dated pending-communication/schema statements describe previous milestones.

## Autonomy completion - 3.4 (2026-10-20)

**Implemented and verified.** The engine composes resource/shift/work/rest/travel and social decisions without an LLM. Equally ranked social choices now use a stable rank seeded by saved world identity, round and participant IDs; restore retains reproducibility without extra random state. Schema v9 gates this policy change. Preferences, affinity and world rules still take precedence.

**338 tests passed.** An accelerated one-hour simulation covered eight persistent agents, zero-to-512 fake application churn, resource scarcity/recovery, valid movement/events and concurrent snapshot readers. CPU, memory and latency measurements are recorded in [Autonomy and Verification](Project_GRID_Autonomy_and_Verification.md). This is a simulated-hour test, not a real-time hour-long production soak. Host applications remain observation-only; automatic agent creation, admin configuration, real task execution and Phase 4 communications remain planned. Previous dated pending-autonomy/schema statements describe earlier milestones.

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

## Phase 2 verification update - 2026-10-19

Section 2.5 is complete for the current implementation. **231 tests passed** (8 Core, 106 Simulation, 64 Persistence, 53 Terminal), with zero build warnings/errors. Eight new isolated cases verify headless terminal save/reopen/recovery, populated older-schema restoration, no offline catch-up, invalid/missing-file preservation and abrupt writer termination with SQLite journal recovery. A test-only helper under TestSupport is built by the persistence tests; production projects do not reference it.

See [Phase 2 Verification](Project_GRID_Phase_2_Verification.md) for evidence and limits. Earlier test counts remain historical. Process-crash verification does not claim physical power-loss coverage; pending application energy/classification/lifecycle work and hardware-generated cities remain unimplemented. No production behavior was changed for 2.5.


**Section 2.3 implemented and verified - 2026-10-18.** This extends the current Genesis runtime. GRID-REQ-005 application discovery, resident classification and resource interpretation remain planned in 2.4/2.7; history does not implement those features.

## Captured events

The engine records meaningful committed changes: clock start/pause, direct district transfers, changing administrative edits, energy changes and district resource changes through `SetDistrictResource`. Ordinary tick-completed messages remain in live view but are excluded from durable history. No-op resource assignments and unchanged/no-op/failed edits do not create committed-change events. The resource API updates fictional district state, not Windows allocations; external callers must not mutate an engine-owned world directly.

Each record contains a stable event GUID, observation UTC timestamp, simulation tick/time, kind, severity, optional entity ID/name, detail and JSON context. Transfer context includes source/destination district IDs; energy/resource context includes before/after values; changing edits include request/operator/session attribution, revisions and formatted field changes. This is committed-change history, not a comprehensive security audit of denied requests, no-ops or all attempted operations. No authenticated terminal login or host-app lifecycle events are invented.

Entity references deliberately have no foreign key to active program rows. A future application can disappear while its historical events remain queryable. Querying or restoring events never creates or replays residents. Schema v2 also keeps history independent of the current snapshot-replacement deletes; one world per file remains the application rule.

## Commit, failure and retention policy

Schema migration 2 adds `WorldEvents` and world/time/entity indexes without editing migration 1. Existing v1 files upgrade transactionally on open. History and the captured world state are written in the same transaction on the existing 30-second autosave and final shutdown save. After successful save/backup publication, the engine acknowledges only the captured event prefix; newer events remain pending. Stable event IDs make a retry after a committed primary save but failed backup refresh idempotent.

The pending journal is separate from the 256-entry live-view buffer and holds at most 10,000 meaningful events. Overflow discards the oldest pending entries and includes an explicit `HistoryGap` Warning with a dropped count in the next captured batch. This is bounded, best-effort history, not a lossless audit stream. Capture/acknowledgement methods are trusted persistence orchestration APIs; callers must acknowledge only a successfully saved batch from that engine.

Abrupt termination can lose pending events since the last committed snapshot. Transaction failures preserve the previous saved state/history and leave pending entries for retry. Recovery restores the history contained in the selected backup and can lose newer primary commits; damaged files remain archived under the existing recovery policy. No automatic disk-history pruning, archive quota or full security-audit retention policy is implemented yet. Database growth and high-volume energy changes require later measured retention/aggregation work.

Direct `WorldStore.Save` callers must provide the captured event batch to persist history; `WorldPersistenceSession` does this automatically. The low-level state-only API remains available for trusted callers/tests. Resource changes made outside the engine API are not observed retroactively. Ordinary process-local live events/sequence numbers still reset on restart, while committed history survives.

## Querying history

The persistent terminal registers:

```text
history
history kind=Transfer limit=20
history severity=Warning
history entity=<full-guid> limit=50 offset=50
history since=2026-10-18T00:00:00.0000000+00:00
```

Filters can be combined. `since` accepts a round-trip timestamp; kind/severity matching ignores case. Results are newest recorded UTC first, with insertion order breaking ties. Defaults: 50 rows, offset 0; maximum 500 rows per query. Offset pagination is a live query, not a fixed transaction spanning multiple commands. Values are SQL parameters; terminal control characters are sanitized. Queries read saved history only, so pending changes appear after autosave or the next launch after orderly exit. `--ephemeral` has no saved-history command.

The `HistoryQuery` API provides the same kind, entity, severity, since, limit and offset filters. History does not grant access beyond the existing local terminal; login-based filtering remains future work.

## Structured operational diagnostics

Scheduler, autosave, shutdown-save and top-level startup/session failures emit a JSON diagnostic alongside the readable error: UTC timestamp, Error severity, component, operation, optional world ID, exception type and message. Startup/top-level diagnostics use stderr; host lifecycle diagnostics use the host output writer. These records remain observable even when SQLite is unavailable. They are not automatically stored in the failed world database or a separate rotating log file; a caller can capture output as needed.

## Completion verification

- `dotnet test Grid7736.slnx --no-restore --verbosity minimal`: **208 passed**, zero failed/skipped (Core 8, Simulation 93, Persistence 56, Terminal 51).
- `dotnet build Grid7736.slnx --no-restore --verbosity minimal`: zero warnings/errors.
- Added eight cases covering history across repeated saves/reopen/recovery, entity/kind/time/paging queries, transaction failure and retry, backup failure deduplication, acknowledgement ordering, independent pending/live buffers and overflow warnings, populated v1 migration, inactive entity references and terminal argument validation.
- Isolated terminal smoke: start/pause/step/exit, reopen the same world, query two saved Clock events, and verify no Tick records. No user default world was opened for this verification.

The prior 200-test result remains historical evidence for 2.2. Next work is 2.4 resources/host state and the 2.7 resident retrofit; these completion notes do not mark them implemented.
