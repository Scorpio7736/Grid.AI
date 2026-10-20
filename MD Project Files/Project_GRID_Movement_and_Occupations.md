# Project GRID - Movement, occupations and workstations

## Autonomy completion - 3.4 (2026-10-20)

**Implemented and verified.** The engine composes resource/shift/work/rest/travel and social decisions without an LLM. Equally ranked social choices now use a stable rank seeded by saved world identity, round and participant IDs; restore retains reproducibility without extra random state. Schema v9 gates this policy change. Preferences, affinity and world rules still take precedence.

**338 tests passed.** An accelerated one-hour simulation covered eight persistent agents, zero-to-512 fake application churn, resource scarcity/recovery, valid movement/events and concurrent snapshot readers. CPU, memory and latency measurements are recorded in [Autonomy and Verification](Project_GRID_Autonomy_and_Verification.md). This is a simulated-hour test, not a real-time hour-long production soak. Host applications remain observation-only; automatic agent creation, admin configuration, real task execution and Phase 4 communications remain planned. Previous dated pending-autonomy/schema statements describe earlier milestones.

## Social civilization completion - 3.3 (2026-10-20)

**Implemented and verified.** Explicitly opted-in Agent/Special residents now have cooperative, competitive or reserved preferences; bounded relationships, memories and occupation history; and simulated district civic tokens with scarcity/recovery. Decisions run on simulation time, respect current work/travel and commit atomically with ticks. Host applications remain observation-only and receive no fictional social activity or resource consumption.

Schema v8 persists social state and partial rounds, upgrades existing worlds without inventing participants, and supports validated backup recovery. `social [id|name]` and program inspection provide read-only views. Trusted engine APIs configure social participation/pools; production login, terminal admin configuration, automatic agent creation, actual communication/delegation and Phase 3.4 sustained autonomy remain future work. **336 tests passed**, plus isolated terminal create/save/reopen. See [Social Civilization](Project_GRID_Social_Civilization.md) for complete rules, limits and evidence. Earlier dated statements that social functionality is pending describe previous milestones.

## 3.2 completion - 2026-10-20

**Implemented and verified.** Configured Agent/Special residents can receive compatible civic workstations, select deterministic district routes over saved city roads, travel on simulation ticks and begin work after arrival. Ordinary host applications remain observation-driven. Terminal `jobs` lists workstations and assignments; `activity` shows fresh application availability and measured working sets. These are read-only views, not host task execution or administrator controls.

## Workplaces and assignment

Workstation definitions derive deterministically from the saved city blueprint. Each matching building lot supplies one capacity-one workstation. Administrator work belongs to the motherboard/Core district; Archivist work to Storage/Archive; Researcher work to RAM/Memory Banks; Technician work to CPU/Processing. Station IDs derive from district identity, typed occupation and saved lot index. No hardware rediscovery or city regeneration occurs. Other occupation categories and detailed workstation equipment remain later extensions.

Older/demo worlds without a road blueprint expose abstract local stations for each occupation and district. They do not invent inter-district road connections. Legacy demo citizens and host applications cannot be assigned civic jobs.

Trusted services first configure an Agent/Special `WorkRoutine` using the existing API, then call `AssignWorkplace(programId, optionalStationId)`. Omission chooses the nearest reachable, compatible, unoccupied workstation; ties use stable station-ID order. Explicit selection rejects unsuitable, occupied, nonexistent or unreachable stations without changing state. Assignment reserves capacity while traveling, resting, off duty or awaiting telemetry. Existing 3.1 routines without workstation assignments retain their abstract simulated-work behavior for compatibility; they are not silently bound to a location. No fictional agents are spawned, and ordinary software is never automatically given a civic job.

`ReleaseWorkplace` frees capacity and cancels remaining travel at the current district. Reconfiguring a routine resets its progress and releases any prior assignment. Both assignment and release increment the resident revision and record events. These APIs are trusted configuration boundaries, not login authorization. Future authenticated terminal job assignment and named agent services remain planned; current `jobs` is inspection only.

## Route and tick rules

Routing uses deterministic breadth-first search on undirected saved CityRoad edges, with stable neighbor ordering. It selects the shortest number of district edges, not Euclidean distance or a claim of physical pedestrian navigation. A route includes the current district and final workstation district. Each edge takes **five simulated seconds**. A tick spends at most one edge's remaining duration and crosses at most one road; excess time in a large tick is not replayed across multiple roads or counted as work.

Configured shift and sourced-resource rules are checked before travel. Off-duty, stale/unknown or insufficient resources freeze the route and work counters. Eligible travel uses Moving/Workplace, priority70. Traveling never increments simulated work or drains fictional application energy. On the arrival tick, movement completes and membership transfers; work starts on a subsequent eligible tick. Existing work/rest/shift rules then apply at the station. Release/reassignment preserves routine counters; ConfigureRoutine resets them as before.

Every tick stages all behavior before mutating the clock, membership, revisions or history. Route membership and capacity are validated before staging; revision/counter overflow and cancellation abort the whole tick. Commit transfers the resident through world-owned membership mutation and increments its revision once. A failure in another resident cannot commit a partial move. Invalid route endpoints, repeated/unknown districts, missing roads, incompatible jobs and duplicate occupancy are rejected. Assigned residents cannot be manually transferred through administrative editing until their workplace assignment is released; pre-assignment edit previews become stale through revision changes.

This is **district-level movement**, not 3D positions, walking animation, collision avoidance, congestion, traffic or physical lot-entry movement. Rest and off-duty behavior stays at the current district; home/leisure travel belongs to later simulation work.

## Application presentation and resource boundaries

`activity` projects the engine snapshot: current Running/On call/Sleeping/unknown labels, confirmed fresh WorkingSetBytes where measured, and observation source. Partial or stale measurements stay unknown. Shared pages may overlap; working sets are not inferred minimum requirements or evidence of CPU/GPU/I/O usage. Per-application CPU/GPU/storage/network attribution remains unknown. Host software does not follow fictional commute routes or earn simulated civic production. `jobs`, `activity`, `behaviors`, `inspect` and live view do not advance simulation time.

## Saved state and history

Schema **v7** records a compatibility migration for the expanded bounded ResidentBehaviors JSON contract. Existing tables already store the required behavior data; no duplicate station table is needed. The version gate prevents earlier binaries from silently dropping assignment/travel fields on save. Historical migrations are unchanged. Saves include station ID, remaining route and edge time with persistent Agent/Special behavior. Station definitions reconstruct from the unchanged saved city; transient application residents remain excluded.

Restore validates station identity, typed occupation, exclusivity, endpoints, road adjacency and edge-time bounds and copies route collections into immutable snapshots. Restart remains paused and does not catch up offline travel. Fresh resource evidence is required before further movement. Old saves without assignments retain unassigned behavior. Backup/recovery preserves the same route and assignment. Workplaces use existing save atomicity and failure handling.

`WorkplaceAssigned`, `WorkplaceReleased`, `Movement`, existing `Behavior` transitions and `WorkCompleted` feed durable/live event history. Movement events identify prior/next districts and the workstation. Sub-edge timer changes do not produce movement events. Manual transfers retain their existing separate Transfer/Edit history.

## Verification evidence - 2026-10-20

**323 tests passed:** 14 Core, 135 Simulation, 84 Persistence, 90 Terminal. Each project was run with `dotnet test <project>/<project>.csproj -c Verification --no-restore`. The solution accepts only its configured Debug/Release configurations, so the four projects were invoked directly for the isolated Verification build. No build warnings remained in the successful verification runs.

New tests cover legal routes and per-edge timing, district membership, no work while moving, large-step behavior, missing evidence, cancellation, occupation suitability/capacity/release, invalid routes/time bounds, no invented demo roads, off-duty freeze, routine reconfiguration, whole-tick overflow rollback at a ready movement edge, mid-route save/reopen/recovery, and read-only terminal jobs/activity output with measured evidence. Prior behavior, application lifecycle, editing, schema migration, NGE and terminal tests also passed.

A disposable built-terminal smoke created world 224af104-c5ba-496c-bdfd-d013ea9771e4, displayed jobs/activity, saved, and reopened the same city under v7. Normal population was221 observed/catalog residents at that moment; that count is machine/time-specific. Movement of configured agents is fixture-tested; production startup does not fabricate agents to demonstrate it.

The user's running Release GRID process locked Release output. It was left running; Verification output was used for checks. The user must close the old terminal and rebuild their normal Release/Debug target before that installed launcher picks up these changes. Existing user world files were not opened or migrated by smoke tests. Section3.3 social behavior, 3.4 sustained autonomy/performance acceptance, interactive administrative job configuration and real delegated OS work remain future work.
