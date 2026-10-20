# Project GRID — Master Development Checklist

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

## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

New work is tracked in revised 2.4 and added 2.7, with Phase 3/7 acceptance criteria aligned to a changing application population. All completed Genesis and 2.1-2.2 entries remain completed for their original scope; this requirement is not implemented.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


## Documentation integration - 2026-10-18

GRID-REQ-004 is planned: first-time world creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates a unique city using those components, and saves the resulting world for reuse on later startups. Section 2.6 tracks the work; no implementation is claimed. The [design specification](Project_GRID_Design_Specification.md#grid-req-004---hardware-generated-city-contract) is the shared contract for all eight documents.

The user identified this repository's MD Project Files folder as the supplied document location. Its current eight overview/checklist/design/development/storage/city/environment/fashion documents are the integration baseline. Older Downloads copies were compared and contain no GRID-REQ-004; they were not used to overwrite newer repository content. There is no separate updated copy in that location to merge against. The explicit requirement in the user's message supplies the new functionality.

Preserved all existing completed checklist entries, historical test results, editing/session contracts, newer concept images and source changes. At that documentation review, Grid.Persistence was a project scaffold (commit 42dc69a). Sections 2.1-2.2 below record subsequent schema and save/recovery implementation. That earlier requirement-integration update changed Markdown only and did not run runtime tests; later implementation evidence is recorded in the relevant sections.

Resolved planning overlaps: fixed Genesis generation remains the implemented baseline; hardware-derived first creation is future work. Existing saves take priority over regeneration, and hardware does not override the established visual guides. Exact component mappings, discovery APIs and fallback policy remain open design decisions.

## Implementation status - 2026-10-17

Documentation reconciled on 2026-10-17 against local HEAD 0b659e6 and current working-tree changes. Genesis 1.1-1.8 is complete; 1.9.1-1.9.2 are completed designs and 1.9.3 is implemented. At that review, section 1.10 was unimplemented; see the 2026-10-18 completion notes below for its subsequent implementation. Current solution verification passed 117 tests (8 Core, 85 Simulation, 24 Terminal). Historical completion counts below describe their original verification dates, not the current total.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Project:** GRID — Local-first Digital Civilization and Personal Task Assistant  
**Roadmap:** Eight milestones  
**Checklist baseline:** Local repository HEAD `0b659e6` plus working-tree changes reviewed on 2026-10-17. Released Genesis is tagged `v0.1.0-genesis` at `0c1de64`; post-release API/startup changes are not part of that tag. Remote synchronization was not checked during this documentation audit.
**Status convention:** `[x]` = observed in the reviewed repository or current working tree (independent verification is noted where performed); `[ ]` = pending or not verified.
**Scope:** Includes GRID-REQ-002 task assistance, GRID-REQ-003 terminal identities and program editing, and the requested live status/events view in 1.10.

> **Architecture principles:** C#/.NET owns authoritative world state; agent behavior does not require AI; Windows tasks are explicitly permission-scoped and auditable; essential operation is local-first and requires no subscriptions; the 3D viewer observes the simulation instead of owning it. Simulation time advances only while GRID operates.

## Overall roadmap

| Milestone | Name | Deliverables | Original rough solo part-time estimate |
|---|---|---|---|
| 1 | Genesis | Repository, world models, simulation tick, terminal, unit tests | 1–2 weeks |
| 2 | The Core | SQLite persistence, snapshots, events, recovery | 2–3 weeks |
| 3 | First Programs | Needs, jobs, movement, utility-based autonomy | 2–4 weeks |
| 4 | Communications | Messaging, context intake, conversation persistence, voice | 2–4 weeks |
| 5 | System Agents | ARCHON, FORGE, SENTINEL, ARCHIVIST, VOLT, RELAY | 3–5 weeks |
| 6 | Intelligence | Shared local AI, grounded assistance, safe edit proposals | 2–4 weeks |
| 7 | The City | Godot 3D streets, districts, inhabitants, interactions | 4–8 weeks |
| 8 | Integration | Windows lifecycle, resource control, backups, release | 3–5 weeks |

*Estimates are provisional. The added assistant and file-editing capabilities, plus asset production, may expand effort.*

---

# Phase 1 — Genesis

## 1.1 Repository and environment

- [x] Install Visual Studio and .NET 10 SDK
- [x] Create the Grid7736 Git repository
- [x] Create the Visual Studio solution
- [x] Create Grid.Core (.NET class library)
- [x] Create Grid.Simulation (.NET class library)
- [x] Create Grid.Terminal (.NET console application)
- [x] Configure Terminal → Simulation → Core references
- [x] Configure .gitignore and .gitattributes
- [x] Add and organize project design documents

**Completed — commit history reviewed and current implementation verified on 2026-10-16**

- Repository configuration was introduced in [`0f86ab4`](https://github.com/Scorpio7736/Grid7736/commit/0f86ab4fe49401282fd398f4d019e6ce597e848f) (`.gitignore` and `.gitattributes`).
- [`562e51d`](https://github.com/Scorpio7736/Grid7736/commit/562e51d4e4ed006fc462e4fb1818a4c26e38c3f8) established the solution and three .NET 10 SDK projects. [`442632c`](https://github.com/Scorpio7736/Grid7736/commit/442632ca7c25398c5eb441928c06a2806992f524) configured Grid.Terminal as an executable with a console entry point.
- [`0ccb1e2`](https://github.com/Scorpio7736/Grid7736/commit/0ccb1e2dda4d852f28473800008cac729ccc03e0) established Terminal → Simulation → Core project references. [`f365151`](https://github.com/Scorpio7736/Grid7736/commit/f365151a8425314ebb531259cdd42941f0192f6d) added the design documents and solution document folder.
- Verification: GitHub `master` was checked with `git ls-remote` and matches local commit `f5ee323`. Installed SDK reports `10.0.401`; the current solution built successfully in this session with zero warnings and zero errors.
- Git history establishes project setup, not a historical installation record. Visual Studio installation was not independently checked; the existing installation checkbox is retained from the original checklist.

## 1.2 Program model

- [x] Implement GridProgram.cs
- [x] Add unique Guid identity
- [x] Add name and occupation
- [x] Add energy and current district ID
- [x] Implement GridProgramStatus enum
- [x] Add terminal checks for program IDs and initial defaults *(historical demo source verified in f365151; current automated verification documented below)*

**Completed — commit history reviewed and current implementation verified on 2026-10-16**

- [`f365151`](https://github.com/Scorpio7736/Grid7736/commit/f365151a8425314ebb531259cdd42941f0192f6d) introduced `GridProgram.cs` and `GridProgramStatus` with Idle, Working, Moving, Recharging, and Offline states.
- The model provides an init-only GUID identity, name, occupation, energy defaulting to 100, district ID, and status defaulting to Idle.
- That commit included a two-citizen terminal demonstration printing identity uniqueness, default energy, and default status. Its source was inspected; its historical execution was not independently observed. The demonstration was subsequently replaced by the district demo.
- Verification: the eight current World Management tests passed in this session, including factory citizen identities, energy/status defaults, valid district membership, and duplicate-ID rejection. These are current automated results, not evidence of a historical terminal run.
- Section 1.4 subsequently restricted the district setter and introduced world ownership to protect placement consistency.

## 1.3 District model

- [x] Implement GridDistrict.cs
- [x] Add identity, name and description
- [x] Add district population IDs
- [x] Add fictional resource dictionary
- [x] Initialize the five starting district objects
- [x] Verify district GUID uniqueness in terminal *(executed successfully on 2026-10-16; result: True)*

**Completed — commit history reviewed and terminal verification performed on 2026-10-16**

- [`a11f394`](https://github.com/Scorpio7736/Grid7736/commit/a11f3940a1ec87fd720b9820f5720f3a400679bf) introduced `GridDistrict.cs` with init-only GUID identity, name, description, program membership IDs, and a fictional resource dictionary.
- The same commit initialized The Core, Processing Sector, Memory Banks, Archive Sector, and Gateway in the terminal, printing their details and checking district ID uniqueness.
- Verification: `dotnet run --project Grid.Terminal/Grid.Terminal.csproj --no-build --no-restore` completed successfully in this session and printed `Districts: 5` and `Unique district IDs: True`. Each standalone demo district reported zero population, as expected.
- Current automated World Management tests also passed, covering factory district initialization, membership, duplicate rejection, and transfers. Section 1.4 subsequently made district membership read-only to external callers.
- At section 1.3 completion, the terminal demonstration was separate from the factory-created world; section 1.7 now connects the terminal to that world.

## 1.4 World management

- [x] Create GridWorld.cs
- [x] Store authoritative program and district collections
- [x] Implement AddDistrict() with duplicate prevention
- [x] Implement AddProgram() with district validation
- [x] Implement GetProgram() and GetDistrict()
- [x] Implement MoveProgram() with membership consistency
- [x] Restrict direct modifications that bypass world rules
- [x] Create GridWorldFactory.cs
- [x] Initialize all five districts through the factory
- [x] Generate twenty uniquely identified programs
- [x] Distribute citizens across valid districts
- [x] Verify transfers preserve population totals

**Completed and verified — 2026-10-16**

- `GridWorld` owns read-only program and district dictionaries. Additions reject null entities, empty or duplicate IDs, invalid district references, and entities already owned by a world.
- `MoveProgram()` updates source membership, destination membership, and the program location together. Moving to the current district is a no-op; invalid requests leave membership unchanged.
- `GridDistrict.ProgramIds` is read-only to callers, and `GridProgram.CurrentDistrictId` has an internal setter to prevent external placement changes.
- `GridWorldFactory.CreateGenesis(seed)` creates five districts with four citizens each. Equal seeds reproduce IDs and placement in independent world instances.
- Added `Grid.Core.Tests` to the solution. Eight automated tests cover initialization, seeded reproducibility, transfers and population totals, invalid requests, duplicate IDs, world ownership, and read-only membership access.
- Verification: `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false` passed all eight tests. `dotnet build Grid7736.slnx --no-restore --verbosity minimal` succeeded with zero warnings and zero errors.

At section 1.4 completion, the terminal still used its original district demonstration; factory integration is now complete in section 1.7.

## 1.5 Simulation clock

- [x] Create GridClock.cs
- [x] Track simulation tick count and elapsed simulation time
- [x] Implement Start() and Pause()
- [x] Implement manual single-step advancement
- [x] Make tick duration configurable, initially one second
- [x] Keep simulation time independent of the real Windows clock

**Completed and verified — 2026-10-16**

- Added `Grid.Simulation/GridClock.cs` with read-only tick duration, tick count, elapsed simulation time, and running state.
- New clocks start paused at zero ticks. Tick duration is configured at construction, defaults to one second, and must be positive; keeping it fixed preserves the meaning of elapsed ticks.
- `Start()` and `Pause()` are idempotent and preserve accumulated time. `TryAdvance()` advances exactly one fixed tick while running and returns false without changes while paused.
- `Step()` advances one tick while paused and leaves the clock paused. Calling it while running throws without changing time.
- Time advances only through explicit advancement calls; there are no Windows clock reads, background timers, or offline catch-up. Callers must use a single simulation thread.
- Elapsed time uses exact TimeSpan ticks. Overflow is checked before either counter changes.
- Added `Grid.Simulation.Tests` to the solution. All 13 clock test cases passed, covering defaults, invalid durations, exact fixed steps, repeated start/pause, resume, manual steps, and overflow.
- Verification: `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false` passed all 21 tests (13 simulation clock and eight World Management). `dotnet build Grid7736.slnx --no-restore --verbosity minimal` succeeded with zero warnings and zero errors.

Automatic scheduling and world updates are now implemented in section 1.6; terminal clock commands are now implemented in section 1.7.

## 1.6 Simulation engine

- [x] Create GridSimulationEngine.cs
- [x] Advance the authoritative world with fixed simulation steps
- [x] Define a clear world-update sequence
- [x] Generate actual world events during ticks
- [x] Prevent updates while paused
- [x] Support safe cancellation and graceful stopping
- [x] Avoid dependence on any 3D renderer or AI service

**Completed and verified — 2026-10-16**

- Added `Grid.Simulation/GridSimulationEngine.cs` around the authoritative `GridWorld` and a privately owned `GridClock`. Engine operations serialize updates; callers must not mutate the world directly or share it between engines while it is in use.
- Defined the tick sequence: calculate the next simulation timestamp; validate programs and stage changes in stable GUID order; check cancellation; advance the clock and apply all staged energy changes; expose the completed tick's immutable event batch. Validation failures, pre-commit cancellation, and time overflow leave world changes, clock counters, and the previous result untouched.
- Initial Genesis energy rules: Working and Moving consume one energy unit per simulated second; Recharging restores two; Idle and Offline leave energy unchanged. Values clamp to 0–100, and invalid energy/status inputs are rejected. These are initial deterministic rules; autonomous job selection, travel, and needs behavior remain in Phase 3.
- Added `GridTickResult`, `ProgramEnergyChanged`, and `SimulationTickCompleted`. Events contain actual committed changes and simulation timestamps; unchanged energy emits no change event. The engine retains only the latest tick result, avoiding an unbounded in-memory history.
- `Start()` and `Pause()` control normal ticks. `Tick()` returns null while paused; `Step()` explicitly advances once while paused and rejects use while running. Pause waits for any active commit to finish.
- `RunAsync(cancellationToken)` provides cancellable scheduling with at most one fixed simulation step per completed delay, without missed-time catch-up. Pause/resume invalidates a pending wait. Cancellation completes normally, pauses the clock, and releases scheduler ownership; other failures propagate after the same cleanup. Callers await the run task before releasing the world.
- Duplicate schedulers and competing external `Tick()` calls during scheduling are rejected. Automatic scheduling accepts durations from 1 ms through 4294967294 ms; manual advancement supports the clock's wider positive TimeSpan range.
- Scheduling uses an injectable `TimeProvider`; the engine has no AI, renderer, or host telemetry dependency. Tests control timer completion directly rather than sleeping.
- Verification: `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false` passed all 45 tests: eight World Management, 13 clock, and 24 engine cases. Coverage includes energy rules and bounds, genuine events, deterministic results, membership preservation, pause/resume, manual stepping, cancellation, scheduler exclusivity, invalid-state recovery, and overflow.
- `dotnet build Grid7736.slnx --no-restore --verbosity minimal` succeeded with zero warnings and zero errors.

Terminal command integration is now complete in section 1.7. Persistence and durable event history remain in Phase 2.

## 1.7 Terminal interface

- [x] Replace temporary sample code with a command loop
- [x] Implement help, status and programs commands
- [x] Implement inspect command for a program or district
- [x] Implement start and pause commands
- [x] Implement manual tick/step command
- [x] Implement exit with graceful shutdown
- [x] Handle invalid commands without crashing
- [x] Display genuine world state, never fabricated status

**Completed and verified — 2026-10-16**

- Replaced the district demo with a factory-created world, simulation engine, and interactive terminal. The initial world is paused with five districts and twenty citizens.
- Added a command registry (`TerminalCommand`, `CommandDispatcher`), built-in command registration (`DefaultCommands`), and a separate `TerminalHost` for input/output and scheduler lifetime. New commands register their metadata and handler; help updates automatically without changing the input loop.
- Implemented `help`, `status`, `programs`, `districts`, `inspect`, `start`, `pause`, `step`, `tick`, and `exit`. Inspect accepts a program/district ID or name; names can contain spaces or quotes. Duplicate names require a full ID.
- Command names and name matching ignore case. Blank input is harmless; unknown commands, invalid argument counts, unmatched quotes, missing entities, and stepping while running produce readable responses without ending the session.
- Added `GridSimulationEngine.GetSnapshot()` and detached read-only snapshot records. Status and inspection use a consistent view captured under the same lock as tick commits, without pausing the running simulation or exposing mutable world collections.
- Start enables the scheduler; pause stops automatic advancement; step/tick advances once while paused. Output reports actual simulation state and committed events. Idle citizens remain Idle rather than receiving fabricated activity.
- Exit, EOF, and Ctrl+C cancel and await scheduler shutdown. Scheduler failures are reported with a nonzero process result. Console reads run off the control thread so blocked input cannot prevent shutdown; a pending background read can remain until input arrives or the process exits.
- Added `Grid.Terminal.Tests` to the solution and a README with run instructions, command examples, lifecycle behavior, and the command extension pattern.
- Verification: `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false` passed all 69 tests (eight World Management, 37 clock/engine, and 24 terminal cases). Terminal coverage includes parsing, actual output, extension registration/help, snapshots, invalid input, exit, EOF, blocked-input cancellation, and scheduler failure.
- `dotnet build Grid7736.slnx --no-restore --verbosity minimal` succeeded with zero warnings and zero errors. Executable smoke checks verified help, status, district/program inspection, two manual steps, invalid-command recovery, and clean exit. A live redirected process advanced automatically to tick 2, paused, reported its real state, and exited successfully.

World state remains in memory; persistence is planned for Phase 2. The extension point is an in-process command registry, not a dynamic plugin loader. Genesis release is marked by the annotated v0.1.0-genesis tag.

### Terminal startup completion update - 2026-10-17

- [x] Add optional case-insensitive `--start` launch argument; default remains paused.
- [x] Verify `status` reports Running when launched with `dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start`.
- Full solution regression verification after this change: 117 passed, zero failed or skipped. This starts simulation ticking; it does not implement the 1.10 live view.

## 1.8 Genesis tests and release

- [x] Create Grid.Core.Tests xUnit project
- [x] Create Grid.Simulation.Tests xUnit project
- [x] Test uniqueness of IDs
- [x] Test district/program initialization and membership
- [x] Test duplicate and invalid district/program rejection
- [x] Test movement and population invariants
- [x] Test simulation clock and fixed-step advancement
- [x] Test pause, resume and manual stepping
- [x] Test terminal command parsing and outputs
- [x] Run dotnet build successfully
- [x] Run dotnet test successfully
- [x] Add Genesis usage instructions to README
- [x] Tag v0.1.0-genesis

**Genesis release verification — 2026-10-16**

- Release tag: `v0.1.0-genesis` (annotated), covering world management, the simulation clock and engine, and the expandable terminal interface.
- Re-ran all 69 tests successfully and built the full solution with zero warnings and zero errors before tagging.
- Existing executable checks verified real factory state, inspection, manual stepping, automatic ticks, pause, invalid-command recovery, and clean shutdown.
- The release starts a fresh in-memory world; persistence remains Phase 2 work.

**Genesis release gate:** A console simulation with five districts and twenty unique programs; start, pause, tick, status, programs, inspect, and exit work using actual state; automated tests pass.

---

## 1.9 Post-Genesis preparation — Terminal identities and administrative editing

**Follow-up status:** Sections 1.9.1–1.9.2 designs and the 1.9.3 editing API are complete. Authenticated terminal editing and login remain deferred. These preparations support GRID-REQ-003 and the User's Master Admin editing request. They are follow-up work after the completed `v0.1.0-genesis` release, not missing Genesis acceptance criteria. Preserve the existing release tag.

### 1.9.1 Define editable values and behavior

- [x] Specify initial editable fields: name, occupation, energy, status, and district assignment
- [x] Document field types, valid ranges/statuses, text constraints, and invalid-input behavior
- [x] Keep stable IDs and ownership references outside ordinary editing; keep authorization settings separate from profile values
- [x] Define one-time edits versus persistent overrides and how autonomous updates may change edited values
- [x] Define preview and conflict behavior when the target changes before an edit is applied
- [x] Design editable-field metadata so future fields can be added without rewriting the terminal loop

**Design completed — 2026-10-16**

- Defined the implementation contract in [Program Editing Rules](Project_GRID_Program_Editing_Rules.md), including the five initial fields, text limits, allowed statuses, finite 0–100 energy, district validation, and invalid-input results.
- Selected one-time assignments for the first version; ordinary edits do not pin values against future simulation behavior. Persistent overrides remain deferred.
- Defined exact previews and whole-program revision checks, including conflict rejection, atomic multi-field updates, no-op behavior, explicit pause control, and world/session scope.
- Defined an expandable typed field registry for metadata, parsing, formatting, validation, capability references, and staged mutations. Identity and authorization settings remain separate from profile values.
- Verification: reviewed current GridProgram properties/status values, GridWorld transfer rules, GridSimulationEngine energy updates, and terminal name/ID lookup behavior against the specification. Included concrete acceptance scenarios for later implementation tests.
- This completes design only. No source code was changed or runtime tests run for 1.9.1; field enforcement, revision tracking, and editing operations were pending at design completion and are now implemented in 1.9.3. Session-context planning is now defined in 1.9.2.

### 1.9.2 Plan session context and authorization boundaries

- [x] Define a session-context contract separating authenticated human operator, active program identity, and effective capabilities
- [x] Define how command handlers and engine/application services receive session context
- [x] Specify Master Control editing permissions and denial behavior for agent or unauthenticated sessions
- [x] Define authorization checks at the service boundary rather than relying on command visibility
- [x] Identify future audit fields: operator, active identity, target, old/new values, simulation timestamp, and outcome
- [x] Document that session contracts alone do not implement authentication; do not label a placeholder identity as authenticated

**Design completed — 2026-10-16**

- Defined [Session Context and Authorization Contract](Project_GRID_Session_Context_and_Authorization.md): verified human operator, active Master Control/agent identity, session generation, world scope, policy version, and effective capabilities.
- Defined explicit context propagation through terminal dispatch into services, with a trusted session resolver and authorization checks at preview and apply. Command visibility alone cannot authorize an action.
- Defined initial capability policy: administrative program edits and world control require Master Control; acting as an agent does not inherit the human's administrator rights.
- Defined logout, switching, revocation, preview invalidation, concurrent-session isolation, and authorization/commit coordination.
- Specified audit attribution and old/new value records, distinguishing human-as-agent, autonomous agent, and simulation-system actions. Durable audit implementation remains deferred.
- Verification: reviewed TerminalCommand and CommandDispatcher signatures, engine integration boundaries, and the 1.9.1 editing contract for consistency. Added required acceptance scenarios for future implementation.
- Design only: no source changes or runtime test execution. Actual credential verification and login are not implemented; absent a trusted provider, new administrative operations must deny access. Test-only session resolvers must not become a default production administrator.

### 1.9.3 Implement validated program editing API - complete

- [x] Add typed program-edit requests and results through the engine/application boundary
- [x] Validate all requested values before committing any part of an edit
- [x] Serialize edits with simulation ticks and reject stale/conflicting requests
- [x] Route district changes through world transfer rules to preserve membership and population
- [x] Review public setters on program values and restrict mutation paths that bypass validation, while preserving factory initialization and engine updates
- [x] Return actual committed old/new values for later terminal output and audit integration
- [x] Keep this API separate from future login UI and credential storage; do not expose unauthenticated administrative terminal commands
- [x] Test valid edits, invalid values, all-or-nothing changes, unchanged IDs, district consistency, tick coordination, and authorization enforcement when session checks are connected
- [x] Run the existing world, clock, engine, and terminal regression tests after changes
- [x] Add completion notes with observed test/build results once this preparation is implemented

**Implemented and verified — 2026-10-17**

- Added typed field values, edit requests, immutable previews, structured outcomes, and committed receipts through `GridSimulationEngine.PrepareEdit()` and `ApplyEdit()`.
- Added an explicit typed field registry for name, occupation, energy, status, and district, including normalization, metadata, decimal parsing, round-trip formatting, and validation. Energy text is limited to 1024 characters and bounds are checked exactly before float conversion.
- Added per-program revisions and a world instance ID. Successful changing transactions increment revision once; ticks and transfers invalidate stale previews, while no-ops and unrelated changes do not.
- Edits and world mutations share the engine/world lock. Multi-field edits validate fully before commit, preserve IDs and population, and return actual before/after values. Edits do not advance time or replace the last tick result.
- World-owned program setters now reject direct external changes; unowned initialization remains supported. District changes use the world transfer path. Existing tests were adapted to initialize before ownership or use authorized test editing.
- Added an injectable authorization lease contract with default-deny behavior. Trusted providers must verify authentication, current programs.edit capability and world scope, and hold authorization validity through commit. The engine also checks Master Control identity and session/world bindings. The test provider is compiled only into test projects.
- Preview/apply check current authorization separately. Revoked sessions, agent sessions, changed session generations, foreign previews, replayed consumed previews, stale revisions and invalid values do not commit changes. Cancellation is honored before commit, never midway through a mutation.
- Receipts carry request/operator/session identity, revisions, old/new values, simulation tick/time and UTC time for later audit integration; these receipts are not a durable audit log or live-event buffer.
- Verification: `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false` passed all 117 tests: eight World Management, 85 simulation (including 48 new editing cases), and 24 terminal cases. Tests cover validation, atomicity, no-ops, one-time behavior, membership, authorization, revocation/commit ordering, preview isolation, concurrency with ticks, and unchanged terminal behavior.
- `dotnet build Grid7736.slnx --no-restore --verbosity minimal` succeeded with zero warnings and zero errors. The executable status/step/inspect/exit smoke check also passed.
- Login, credential verification, a production authenticated-session provider, terminal edit commands, persistence, and durable audit remain deferred. Existing Genesis commands keep their current behavior; newly added administrative APIs deny access by default.

**Deferred implementation:** Actual login and credential handling, identity switching UI, saved roles, and durable audit history remain in the planned persistence/session/system-agent work. Sections 4.2A and 5.1 describe that later integration; do not mark those capabilities complete merely because these contracts or APIs exist.

---

## 1.10 Post-Genesis terminal live view

**Implemented and verified - 2026-10-18.** Requested on 2026-10-17. Session-dependent visibility remains deferred until login integration.

- [x] Add a live-view command or option to enter the updating display
- [x] Continuously show running/paused state, simulation tick, elapsed simulation time, and population
- [x] Show current program activities, statuses, locations, energy, and actual changes
- [x] Include a compact recent-events list with simulation timestamps and relevant program names/IDs
- [x] Show real program actions, state changes, transfers, and administrative edits when those events exist; do not invent autonomous activity
- [x] Distinguish completed events from ongoing activity; initially show Idle honestly where no behavior has been implemented
- [x] Add a bounded recent-event buffer with stable sequence IDs so events between display refreshes are not lost or duplicated; keep durable history in Phase 2
- [x] Keep rendering refresh rate separate from simulation ticks; use consistent snapshots without advancing or pausing simulation merely to redraw
- [x] Add an easy key to leave live view and return to the command prompt without stopping the world
- [x] Handle Ctrl+C, terminal resizing, and redirected/unsupported output with a clean fallback
- [x] Avoid flooding scrollback or retaining an unbounded event list; keep refresh/resource usage modest
- [ ] Apply session visibility rules once login is implemented (deferred dependency; no login/provider exists yet)
- [x] Test refresh consistency, event ordering, bounded history, paused display, and return-to-prompt behavior
- [x] Add completion notes and verification results after implementation

**Completion notes - 2026-10-18**

- Added `live` through command registration, with pure formatting and an injectable interactive terminal surface. Refresh runs every 250 ms independently of ticks; Q/Esc returns to commands without changing clock state. Page Up/Down pages citizens; Left/Right pages retained events.
- Added an atomic live snapshot and a bounded 256-event buffer (configurable 1-10,000), stable per-engine sequence IDs and explicit eviction count. Records actual tick/energy changes, clock transitions, direct world transfers and applied edit summaries. No-op/failed edits do not fabricate changes.
- Shows real current statuses, energy, occupation, district and simulation time; Idle does not imply autonomous work. Prior LastTick and edit receipt contracts remain intact. Recent events are not durable history.
- Redraw fits the console, updates only changed rows, restores the cursor and handles resize, cancellation and scheduler failure. Redirected/TERM=dumb/unsupported or too-small surfaces show one snapshot and return to commands. Text is sanitized to single-cell ASCII and clipping is marked.
- Verification: `dotnet test Grid7736.slnx --no-restore --verbosity minimal -p:NuGetAudit=false` passed 140 tests (8 Core, 93 Simulation, 39 Terminal), zero failed/skipped. Added 23 event and live-view cases. Solution build succeeded with zero warnings/errors.
- Interactive smoke checks verified running ticks, Q return, paused view and exit; TERM=dumb fallback also passed. Resize/cancellation and scheduler-failure paths have injected-console tests.
- See [Terminal Live View](Project_GRID_Terminal_Live_View.md) for controls, architecture, limitations and verification details. Login visibility, persistence, autonomous actions and GRID-REQ-004 remain deferred; earlier completion notes remain historical.

---

## 1.11 Post-Genesis terminal usability and live-view navigation

**Implemented and automatically verified - 2026-10-19.** Improve readability and make navigation easier to discover across the command prompt and live view. This extends completed 1.7/1.10 without changing their completion history.

- [x] Improve spacing, headings, concise labels and help text so important information is easy to scan
- [x] Present comparable data in aligned tables where helpful: programs, districts, resources, behaviors and event/history listings
- [x] Choose useful default columns; keep full IDs and detailed provenance available through inspection rather than crowding summary rows
- [x] Adapt tables and live panels to terminal width/resize with sensible wrapping, clipping indicators or paging; retain readable narrow-terminal/plain-text fallbacks
- [x] Add consistent color coding for statuses, healthy/warning/error conditions, unknown/stale observations and event types
- [x] Keep text labels alongside colors, use readable contrast, and provide a no-color mode; suppress terminal color/control sequences in redirected or unsupported output
- [x] Make live-view navigation prominent: display an unambiguous Back to command prompt control and distinguish it from exiting GRID
- [x] Preserve Q/Esc as back-to-prompt controls, restore the cursor/prompt cleanly, and allow repeated live → prompt → live navigation
- [x] Verify leaving live view preserves running/paused state, telemetry, autosave and the world session; quitting remains a separate explicit action
- [x] Verify usability in interactive and redirected output, narrow/resized terminals, long names/IDs, empty tables and high event volume
- [x] Update help, README and live-view documentation with the final controls, palette/table conventions and dated completion evidence

**Completion verification:** 290 Release tests passed, including repeated live/prompt navigation, retained clock/world/persistence session, table widths, safe/plain output and color policy. A disposable PTY smoke confirmed fallback-to-prompt with the clock still running. Full-screen colors/contrast in the user's terminal remain a manual visual check; the PTY did not support live redraw. See [Terminal Live View](Project_GRID_Terminal_Live_View.md#terminal-usability-completion---111-2026-10-19) for controls, palette and evidence.

---

## 1.12 Terminal command reference, population filters and farewell

**Implemented and verified - 2026-10-19.** Extends completed 1.11 without changing its completion record.

- [x] Add display-only `clear` with safe plain-output fallback
- [x] Group `help` into Simulation Control, World & Population, Monitoring & Analytics, System & Terminal, and Command Options
- [x] Add `programs -active` for Running application residents
- [x] Add `programs -possible` for installed/previously observed applications, retaining unknown/provenance labels
- [x] Add `programs -sleeping` for On call and Sleeping application residents, keeping their labels distinct
- [x] Preserve bare `programs`, support `ids` with one filter, and validate flags/empty results
- [x] Persist minimal previously seen application catalog metadata across restarts separately from live/resident state; verify migration and stale/uninstalled entries
- [x] Add a GRID7736 ASCII shutdown banner with truthful save/failure/ephemeral messages and no blocking key prompt; updated to the user-supplied artwork with light-blue default text (75 terminal tests passed)
- [x] Verify normal-terminal versus Visual Studio host behavior; do not claim the application can replace Visual Studio's post-exit footer
- [x] Test help/filter/clear/catalog/shutdown behavior and update dated completion notes when implemented

See [Terminal Command and Shutdown Plan](Project_GRID_Terminal_Command_and_Shutdown_Plan.md) for the command layout, filter semantics, proposed artwork, persistence dependency and verification criteria. Completion evidence: **297 Release tests passed**; built-terminal save/reopen/filter/help/farewell smoke passed. Clear success/fallback is tested with an injected screen action; the tool PTY used fallback. User-console visual assessment remains separate. Schema v6 catalogs metadata without restoring ordinary resident activity. Visual Studio retains control of its post-exit footer.

---

# Phase 2 — The Core

## 2.1 Persistence architecture

**Implemented and verified - 2026-10-18.** SQLite schema and migration foundation complete; engine save/load remains 2.2.

- [x] Create persistence layer/project when needed
- [x] Select and integrate SQLite
- [x] Design tables for districts, programs and resources
- [x] Design simulation-state and clock storage
- [x] Establish primary keys and relationships
- [x] Enable transactions and foreign-key enforcement
- [x] Create a versioned database schema and migrations

**Completion notes - 2026-10-18**

- Replaced the empty Grid.Persistence scaffold with SqliteGridDatabase and GridSchema, retaining Microsoft.Data.Sqlite 10.0.13. Added persistence and its test project to Grid7736.slnx.
- Added STRICT schema v1 for Worlds, Districts, Programs, DistrictResources and SimulationStates, plus migration history. Composite keys enforce same-world membership; program district references are the single stored membership source.
- Stores energy/status/revision and integer simulation clock values with constraints. Durable world identity is distinct from runtime instance/session identity. Seed/version provenance is optional; hardware profiles/layouts remain future migrations.
- Explicit initialization creates schema only. OpenExisting refuses missing/uninitialized databases. Every managed connection enables foreign keys, FULL synchronization, private cache and a bounded busy timeout.
- Migration metadata, DDL and version markers share an immediate transaction. Unknown/future/inconsistent databases are rejected; failed upgrades roll back. Tests cover concurrent initialization and synthetic next-version migration; only v1 ships.
- Verification: 31 new persistence tests passed. Full solution `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false` passed 171 tests (8 Core, 93 Simulation, 39 Terminal, 31 Persistence), zero failed/skipped. Solution build passed with zero warnings/errors.
- Tests verify SQL row preservation, constraints, relationships, rollback and schema lifecycle. They do not claim engine save/load, crash recovery or power-loss verification. At that milestone, sections 2.2-2.6 and GRID-REQ-004 remained pending; the subsequent 2.2 completion is recorded below.
- See [Persistence Architecture and Schema](Project_GRID_Persistence_Architecture.md) for tables, connection/migration policy, identity conventions and future audit/hardware integration.

## 2.2 Save and recovery

- [x] Save world state transactionally
- [x] Load and validate saved world state
- [x] Add periodic consistent snapshots
- [x] Implement safe startup and shutdown saves
- [x] Handle interrupted writes and corrupt files
- [x] Restore from last known-good snapshot
- [x] Preserve identities, district memberships and clock state
- [x] Ensure no simulation progress during powered-off time

**Completion notes - 2026-10-18**

- Added WorldStore transactional save/load over the unchanged schema v1, with complete state validation and parameterized SQL. Durable GridWorld.Id is separate from the fresh runtime InstanceId. Programs retain exact values, revisions and memberships; resources and clock state round-trip.
- Normal terminal startup creates and persists Genesis once, then restores the existing world paused unless --start is passed. No wall-clock catch-up occurs. Default storage is %LOCALAPPDATA%\GRID\world.db; --world selects another file and --ephemeral explicitly opts out of saving.
- Autosave captures a consistent snapshot every 30 seconds without holding the engine lock during disk I/O. Exit, EOF and Ctrl+C await scheduler/autosave shutdown before a final save. Failures are visible and produce a nonzero result.
- Saves roll back on SQL failure/cancellation before commit. A validated rolling .backup is published after each successful primary commit. --recover validates that backup and preserves damaged primary/sidecar files; interrupted recovery requires an explicit retry. Missing/corrupt saves are never silently regenerated. An exclusive session lease prevents competing GRID instances for one file.
- Verification: `dotnet test Grid7736.slnx --verbosity minimal --no-restore` passed **200 tests** (8 Core, 93 Simulation, 50 Terminal, 49 Persistence), zero failed/skipped. `dotnet build Grid7736.slnx --no-restore --verbosity minimal` passed with zero warnings/errors. Added 29 cases covering world round trips, injected transaction failure, cancellation, invalid data, backup/recovery failures, periodic saving, ownership and terminal lifecycle.
- Built-terminal smoke verification used an isolated temporary database: create, step twice, exit/save, reopen paused at tick 2 with the same identity, reject corruption, explicitly recover, and verify --ephemeral --start. No default user world was opened.
- Limits: abrupt termination can lose work after the last committed snapshot; tests do not claim physical power-loss validation. Backups are one rolling snapshot, not historical retention. Durable events/audit (2.3), hardware-generated cities (2.6), login and Windows service integration remain pending. See [Persistence Architecture](Project_GRID_Persistence_Architecture.md).

## 2.3 History and observability

- [x] Create persistent event log
- [x] Record changes with timestamps and involved entities
- [x] Record movement, resources and significant world events
- [x] Support filtering and querying history
- [x] Avoid unnecessary logging of routine ticks
- [x] Add structured logs with severity and context

**Completion notes - 2026-10-18**

- Added schema v2 WorldEvents with transactional upgrade; v1 migration is unchanged. Events and consistent world snapshots commit together during autosave/shutdown, with idempotent event IDs and acknowledgement after successful persistence.
- Records UTC and simulation timestamps, involved entity IDs/names, severity, details and JSON context for clock transitions, transfers, energy/resource changes and changing edits. Historical references do not require a currently active resident and never resurrect one.
- Added SetDistrictResource for validated, observable resource changes; ordinary tick-completed messages and no-op updates are excluded from durable history. Current resources are fictional; 2.4 host telemetry is not implemented.
- Added history command/API filters for kind, entity, severity, since time and bounded limit/offset. Queries show committed history only. Added structured Error diagnostics for scheduler, save and startup/session failures.
- Pending history is independent of live-view retention, capped at 10,000 events with explicit HistoryGap warnings. Abrupt termination can lose uncommitted history; automatic disk retention and exhaustive security audit remain future work.
- Verification: 208 solution tests passed (8 Core, 93 Simulation, 56 Persistence, 51 Terminal), zero failed/skipped; build passed with zero warnings/errors. Added eight history/migration/query/failure cases. Isolated terminal restart smoke recovered Clock history and excluded Tick records.
- See [History and Observability](Project_GRID_History_and_Observability.md) for API contracts, retention/failure boundaries and evidence. GRID-REQ-005 remains planned in 2.4/2.7; previous completion records retain their original scope.

## 2.4 Resources and host state

- [x] Define CPU, RAM, GPU/VRAM, storage and network resource categories for application residents (GRID-REQ-005); distinguish measured usage, capacity, pressure and estimated requirements
- [x] Establish resource limits and update invariants
- [x] Create abstract interfaces for host telemetry
- [x] Add fake telemetry provider for repeatable tests
- [x] Add approved read-only Windows telemetry
- [x] Specify and test bounded, smoothed energy/resource-sufficiency interpretation, including missing/stale metrics and unknown requirements (policy API; application binding depends on 2.7)
- [x] Prevent fictional resource changes from affecting Windows allocations

**Telemetry/policy completion notes - 2026-10-19. Application integration remains pending below.**

- Added typed metric/scope/unit/status/source/timestamp contracts, a read-only provider interface, two-second monitoring and detached engine observations with ten-second freshness. Invalid/nonfinite/duplicate readings are rejected; unsupported/stale/unavailable values remain unknown.
- Windows collection reads CPU idle headroom, available/total physical RAM, system-volume free/total space, and per-interface network rates/link status. CPU has an initial warm-up; network resets and stale gaps do not fabricate rates. GPU/VRAM and disk throughput are explicitly Unsupported. No host allocations or process state are changed.
- Added a provenance-required additional-work headroom policy: weakest required ratio, bounded 0–100, EMA 0.25 on distinct samples, unknown/reset on missing/stale required data. This does not infer real application minimum requirements or health.
- Added resources command and CPU/RAM summary in live view. Monitoring remains independent of simulation ticks and is cancelled/awaited at shutdown; sampling is not saved as current host state or logged on every interval.
- Verification: 223 tests passed (8 Core, 106 Simulation, 56 Persistence, 53 Terminal), zero failed/skipped; build zero warnings/errors. Fifteen added cases cover fake providers, invariants, freshness, policy smoothing and terminal lifecycle. A real Windows disposable-session smoke verified CPU warm-up/available readings, RAM, explicit unsupported GPU and tick zero while paused.
- [ ] Bind resource requirements/policy to discovered application residents and replace their arbitrary Genesis energy drain after 2.7 classification/lifecycle exists. Existing fictional citizens retain their old rules; application energy is displayed as unknown rather than a fabricated score.
- [ ] Extend collectors for GPU/VRAM, disk throughput and additional drives when supported; retain explicit unknown fallback and scoped attribution.

See [Resources and Host State](Project_GRID_Resources_and_Host_State.md) for exact units, formula, coverage and remaining integration. These completion notes do not mark GRID-REQ-005 or the 2.7 retrofit implemented.

## 2.5 Verification

- [x] Test save → close → reopen consistency
- [x] Test transaction rollback and crash recovery
- [x] Test migration from an older schema
- [x] Test no offline catch-up
- [x] Test invalid or missing database behavior
- [x] Verify persistent world runs without graphics or AI

**Completion notes - 2026-10-19**

- Added eight isolated integration/verification cases covering real terminal save/close/reopen and backup recovery, a populated v1-to-v2 migration, simulated years offline, and missing/empty/foreign/future database rejection without replacement.
- Added a test-only subprocess that holds an uncommitted SQLite transaction with spilled pages. The parent terminates that child after a readiness handshake; reopening restores the last committed world/history, removes uncommitted schema/data, passes integrity_check and retains the backup unchanged. Existing injected-failure tests continue to verify production save/history transaction rollback and retry.
- Verified restored world/program IDs, revisions, values, district membership/resources and clock state. Restoration starts paused with a fresh runtime identity, no stale host telemetry and no offline advancement; a manual step advances exactly once.
- Headless terminal subprocesses operate with redirected streams and no viewer/model dependency. Tests use unique temporary worlds and never the user's default database. No production behavior changes were needed.
- Verification: `dotnet test Grid7736.slnx --verbosity minimal` and final `--no-restore` run passed **231 tests** (8 Core, 106 Simulation, 64 Persistence, 53 Terminal), zero failed/skipped. `dotnet build Grid7736.slnx --no-restore --verbosity minimal` passed with zero warnings/errors.
- Scope: process-crash/journal recovery is tested, not physical power loss or exhaustive storage-failure coverage. Pending application-energy binding (2.4/2.7), hardware generation (2.6), and application-resident retrofit (2.7) remain pending.
- See [Phase 2 Verification](Project_GRID_Phase_2_Verification.md) for the requirement-to-test mapping, helper isolation and limitations.

---

## 2.6 Hardware-generated first-time world creation (GRID-REQ-004)

**Implemented and verified - 2026-10-19.** Integrated with persistence; completed Genesis criteria and the fixed demo/test factory are preserved.

- [x] Define a read-only host discovery adapter for motherboard, CPU, GPU, RAM, storage and network hardware
- [x] Specify normalized profile fields, units, stable ordering, multiple-device handling and recorded partial-discovery fallbacks
- [x] Keep local adapter discovery separate from network scanning, user-file access and live telemetry
- [x] Define bounded component-to-city mappings that preserve the city, atmosphere and fashion design guides
- [x] Implement headless C# generation using a normalized creation profile, per-world seed and generator version
- [x] Persist world ID, creation profile, seed, versions, component associations, generated layout, entities and simulation state atomically
- [x] Mark first creation complete only after durable save; handle simultaneous initialization without duplicate authoritative worlds
- [x] Load the actual saved world on later startups instead of rerunning generation, preserving edits, identities, memberships and clock state
- [x] Preserve established worlds after hardware upgrades, missing devices, changed drivers or changed discovery order
- [x] Distinguish genuinely absent saves from corrupt, inaccessible or incompatible saves; recover or migrate without silent replacement
- [x] Keep explicit new-world creation separate from normal startup and preserve prior worlds/history
- [x] Test deterministic fixtures, profile-sensitive generation, partial discovery, multiple devices and bounded world size
- [x] Test first creation and reopen, changed-hardware reuse, interrupted saves, full disk, corrupt data and concurrent initialization
- [x] Add completion notes with actual implementation and verification evidence when finished

**Completion notes - 2026-10-19**

- Added six-category Windows CIM discovery, normalized profiles, explicit partial/unsupported fallbacks and bounded deterministic hardware-city-v1 generation. Six districts, connected streets and building lots are semantic data; no renderer is claimed.
- Schema v3 transactionally saves actual layout, provenance, associations and initial state. Later load/recovery skips discovery; existing worlds remain intact. Explicit separate creation uses --world with a new filename.
- New generated worlds contain zero residents pending 2.7, preserving the revised host-application vision. Legacy worlds and --ephemeral retain the original Genesis population.
- Verification: dotnet test passed all 250 tests; dotnet build --no-restore had zero warnings/errors. Nineteen new cases cover determinism, bounded/fallback geometry, save/reopen/recovery, corrupt/artifact refusal, cancellation, concurrent creation and genuine SQLite SQLITE_FULL rollback through a test-only page limit. Existing interrupted-update subprocess tests also pass; physical disk exhaustion and power-loss are not claimed.
- Real Windows disposable creation captured all six categories with CPU units and RAM/storage capacities; six districts, six streets and 56 lots reopened unchanged at tick 1, paused. The default world was not modified.
- See [Hardware City Generation](Project_GRID_Hardware_City_Generation.md) for full implementation details and limits. Changes are local working-tree changes at verification, not a release or remote publication claim.

**Later integration:** Phase 7 renders saved layouts; Phase 8 startup uses this same create/load decision. GPU or hardware capacity must not require graphics or AI for headless generation. Hardware details are provenance, not authentication; persisted world IDs do not preserve old session/preview validity after restart.

---

## 2.7 Host application population and persistence policy (GRID-REQ-005)

**Implemented and verified - 2026-10-19.** Integrated with the 2.4 resource foundation before extending Phase 3. Section 2.2 remains completed for its original all-citizen save/load behavior. See [shared contract](Project_GRID_Host_Programs_and_Resources.md).

- [x] Add explicit transient application versus persistent agent/special-program classification without deriving authority from names or labels
- [x] Define application grouping, multi-process ownership, independent instances, background/system-process inclusion and bounded discovery costs
- [x] Add fake and read-only host process discovery adapters with process-lifetime identity, PID-reuse protection, sample freshness and unavailable-data handling
- [x] Reconcile starts, confirmed exits and relaunches atomically with ticks and district membership; incomplete observations must not imply exit
- [x] Load persistent residents and discover the currently running application population at startup; never restore a closed application from a prior save
- [x] Persist city/layout, persistent agents/special programs and clock while omitting ordinary resident state and dangling membership references
- [x] Add a versioned migration and explicit legacy conversion/archive policy; retain a recoverable original and never relabel old citizens as detected apps
- [x] Apply live reconciliation after backup recovery without resurrecting historic application residents
- [x] Integrate 2.4 resource observations and requirement provenance; define energy/unknown-state behavior and separate optional civic resources
- [x] Keep historical lifecycle records distinct from resident saves; define minimal metadata, retention and no unobserved offline activity
- [x] Update terminal snapshots/inspection/live view for resident type, incarnation, resource freshness and known versus estimated values
- [x] Ensure observations and simulated shortages never stop, suspend, restart or throttle host applications without a separate authorized capability
- [x] Test grouping, PID reuse, rapid starts/exits, zero applications, denied/stale telemetry, resource interpretation and invalidated previews
- [x] Test changed application sets across restart, persistent-agent continuity, legacy migration/rollback and recovery reconciliation
- [x] Add dated implementation/completion verification only after the behavior is built and tested

### 2.7.1 Retrofit existing modules before extending citizen behavior

**Retrofit implemented and verified - 2026-10-19.** See [retrofit mapping and dependency order](Project_GRID_Host_Programs_and_Resources.md#existing-implementation-retrofit---planned-2026-10-18). Preserve original completion entries for 1.2–1.10 and 2.2.

- [x] Extend GridProgram with explicit resident classification and application-run identity distinct from durable identity
- [x] Add safe world/engine removal and reconciliation, including atomic membership cleanup and rejection of operations on expired incarnations
- [x] Retain the Genesis twenty-citizen factory as an explicit demo/test fixture and replace production population seeding with discovery
- [x] Adapt 2.2 save/load and backup recovery with classification-aware persistence and a recoverable, versioned legacy migration
- [x] Switch production startup only once lifecycle reconciliation and the new persistence policy work together
- [x] Replace fixed application energy drain/recharge only after 2.4 specifies measurable inputs, requirement provenance and unknown-data behavior
- [x] Adapt editing metadata and preview validation for observation-owned fields, transient edits and application exit/relaunch
- [x] Extend terminal inspection/live view for resident type, resource freshness and unknown measurements
- [x] Preserve clock/no-offline-time rules, transactions, backups, command registration and consistent snapshots; add regressions around their integration
- [x] Verify the complete retrofit before expanding Phase 3 behavior; retain Genesis tests as fixtures rather than production population requirements

**Order:** classification/policy → safe lifecycle with fake discovery → coordinated persistence/startup migration → real telemetry and resource interpretation → editing/UI integration and end-to-end verification. Section 2.3 can proceed against this contract without treating all event subjects as permanent residents.

**Completion notes - 2026-10-19**

- Added explicit Legacy/Application/Agent/Special classification, new runtime incarnations, current-session windowed executable grouping, read-only Windows discovery and injectable fake-provider tests. Same-executable helpers/windows share one resident; background-only services and cross-executable child ownership are outside the initial policy.
- Lifecycle reconciliation shares the engine lock with ticks/edits/snapshots. Confirmed exits clean membership; incomplete samples retain unknown residents/helpers. PID reuse/relaunch invalidates the prior resident and its edit previews. No unobserved offline activity is invented.
- Schema v4 filters application rows and membership from saves. Agent/Special identity/state persists. Production conversion archives legacy citizens in a validated unique .legacy-<id>.db file before removal; cancellation/failure preserves recoverable data. Recovery rediscovers current applications.
- Bound 2.4's optional sourced headroom policy to non-demo residents; unknown requirements remain unknown. Removed arbitrary non-demo tick energy changes. Observation-owned energy/status reject edits; aliases/district edits remain transient. Inspection/live/status expose classification, incarnation, freshness and measurement limits.
- Verification: 268 tests passed (14 Core, 117 Simulation, 78 Persistence, 59 Terminal); build zero warnings/errors. Eighteen added cases cover grouping, lifecycle/denials/PID reuse, concurrent churn/ticks, condition interpretation, invalidated edits, save/restart/recovery, persistent residents and archive failure/cancellation. Real Windows disposable startup and reopen observed nine groups without changing the default user world.
- See [Application Residents Implementation](Project_GRID_Application_Residents_Implementation.md) for precise scope and evidence. Per-application CPU/GPU/disk/network attribution, persistent service budgets, agent task services and broader app discovery remain future extensions. Changes are local working-tree changes, not a release or publication claim.

**Planning integration notes - 2026-10-18:** Updated the design, resource/persistence contracts, terminal/identity/editing plans, visual guides and agent handoff. Replaced fixed production-population targets with running applications plus persistent residents; retained twenty-citizen fixtures and original completion history. No C# or database changes and no runtime tests were performed. Earlier 200-test verification belongs to 2.2, not GRID-REQ-005.

---


## 2.7.2 Installed-program availability revision

- [x] Expand discovery to registered installed applications and accessible current-session background processes
- [x] Display Sleeping / On call / Running with explicit unknown/stale fallback
- [x] Retain session catalog entries through process exit and preserve resident identity through sleep/wake
- [x] Preserve ordinary-app rediscovery, saved city, and Agent/Special persistence contracts
- [x] Verify lifecycle, classification, incomplete observation safety and real Windows startup/reopen
- [ ] Extend full-machine coverage to Store-only/portable apps, richer executable mappings, uninstall reconciliation and populations beyond the current guard

**Completion notes - 2026-10-19:** Implemented the discoverable-inventory baseline; the broader “all PC programs” goal remains limited by discovery coverage. 290 tests passed; Windows smoke showed 49 Sleeping, 65 On call, 13 Running, 96 unknown. See [Installed Application Availability](Project_GRID_Installed_Application_Availability.md). This supersedes the earlier 2.7 windowed-only/remove-on-exit policy without erasing its completion history.

---

# Phase 3 — First Programs

Planned behavior follows GRID-REQ-005: ordinary application residents are transient and observation-driven; persistent agents/special programs retain autonomous roles. Relationships and memories for ordinary residents are session-local; durable historical events do not restore them as active residents.

## 3.1 Program needs and behavior

- [x] Implement the approved resource-based energy/condition policy for application residents and persistent agents; do not use movement/open duration as arbitrary drain
- [x] Implement persistent-agent/special-program work/rest policies and observation-grounded application activity
- [x] Add needs, goals and action priorities
- [x] Define legal states and state transitions
- [x] Implement occupations and simple daily work
- [x] Use deterministic state machines or utility scores

**Completion notes - 2026-10-19**

- Added a deterministic priority state machine for application observation and persistent-resident needs/goals, shifts, resource waits, work bursts and rest. Applications never acquire fictional work; Agent/Special routines require explicit trusted configuration and sourced headroom budgets.
- Administrator, Archivist, Researcher and Technician routines track simulated work units. Work/rest durations, shift windows and 60/40 resource hysteresis are explicit. Unknown evidence freezes progress. No arbitrary energy drain, offline catch-up, movement, OS actions or named agent services were introduced.
- Schema v5 persists routine/configuration and progress atomically while omitting application behavior. Restored routines wait for fresh measurements. Added transition/completion history, revision tracking, read-only behaviors and inspection details.
- Verification: 278 tests passed in Release (14 Core, 125 Simulation, 79 Persistence, 60 Terminal); Release build zero warnings/errors. Tests cover state selection, cycles, shifts, cancellation, overflow rollback, persistence/recovery and UI. A disposable real terminal step/reopen preserved tick 1 and showed observation-only applications.
- Debug binaries were locked by running GRID/Visual Studio sessions; they were not stopped. Added the existing test-only crash probe to the solution to ensure matching Release configuration. See [Program Needs and Behavior](Project_GRID_Program_Behavior.md) for evidence and scope. Changes are local, not a release/publication claim.

## 3.2 Movement and occupations

- [ ] Define district connections and movement rules
- [ ] Add movement goals and route selection
- [ ] Add jobs and workstations
- [ ] Assign programs to suitable jobs
- [ ] Connect application presentation to measured resource activity; keep optional fictional civic production separate
- [ ] Prevent impossible or invalid movements

## 3.3 Social civilization

- [ ] Implement program-to-program interactions
- [ ] Introduce relationships and social preferences
- [ ] Add simple memories and meaningful experiences
- [ ] Track occupation and social history
- [ ] Allow cooperative and competing goals
- [ ] Introduce bounded resource scarcity and recovery

## 3.4 Autonomy and tests

- [ ] Implement program decision loop
- [ ] Use seeded randomness for reproducibility
- [ ] Ensure each action follows world rules
- [ ] Run a changing fake application population and persistent agents for an hour; cover zero applications, starts/exits and high churn
- [ ] Verify coherent movement and event records
- [ ] Measure CPU and memory consumption
- [ ] Verify autonomy works without an LLM
- [ ] Keep the simulation responsive when background load increases

---

# Phase 4 — Communications

## 4.1 Communications foundation

- [ ] Create communication hub and routing contracts
- [ ] Define message, conversation and recipient models
- [ ] Support program identity addressing
- [ ] Support messages to named agents and civilian programs
- [ ] Implement conversation start/continue/end
- [ ] Persist messages in SQLite
- [ ] Recover conversations after restart

## 4.2 Terminal communication

- [ ] Build interactive GRID chat commands
- [ ] Support directed messages and replies
- [ ] Support ARCHON as general coordination point
- [ ] Implement deterministic replies for early development
- [ ] Distinguish verified state from simulated dialogue
- [ ] Add command validation and helpful errors
- [ ] Support ongoing conversations in the same session

## 4.2A Planned terminal login and identity sessions (GRID-REQ-003)

**Planning addition — 2026-10-16; not implemented.** See [Terminal Login and Master Control Plan](Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md). This extends the terminal after Genesis and does not change section 1.7's completed status.

- [ ] Choose local authentication method and final Master Control/Master Admin label
- [ ] Define persistent operator/agent identities, role policies, and audit records with Phase 2 storage
- [ ] Begin future terminal sessions with login
- [ ] Support the User's Master Control identity and each registered agent identity
- [ ] Pass operator identity, active program identity, and capabilities through command/session context
- [ ] Show active identity in prompts and implement whoami, logout, and authorized switching
- [ ] Provide role-specific views of real tasks, messages, history, and world state
- [ ] Extend command registration with capabilities and identity-aware help
- [ ] Clear or rescope context on logout and identity changes
- [ ] Verify login failures, switching, logout, context isolation, and expandable identity registration

### Planned Master Admin program editing (GRID-REQ-003)

**Terminal integration remains planned; backend API completed in 1.9.3.** Master Admin and Master Control refer to the same planned human administrator role. Checked items below refer to backend functionality, not a working login/editing UI.

- [ ] Add terminal commands to inspect and edit programs and their values as Master Control
- [ ] Initially support name, occupation, energy, status, and district assignment
- [x] Define an expandable registry of editable fields, types, and validation rules
- [x] Enforce Master Control authorization in the editing service
- [ ] Preview target and old/new values; report committed results
- [x] Serialize edits with ticks and reject stale/conflicting updates
- [x] Use world-management transfers for district changes; preserve stable identity and membership
- [x] Separate profile edits from credential, permission, and host-capability management
- [ ] Audit edits and integrate persistence; define one-time edits versus ongoing overrides
- [ ] Test authorized/denied edits, invalid values, atomicity, population consistency, and saved results

## 4.3 User-supplied context (GRID-REQ-002)

- [ ] Support selecting or uploading local files as task context
- [ ] Support text, Markdown, source code and documents initially
- [ ] Record source file paths, versions and provenance
- [ ] Define temporary versus persistent project context
- [ ] Prevent context access outside approved scopes
- [ ] Allow users to revoke or remove shared context
- [ ] Implement safe handling of oversized or unsupported files

## 4.4 Voice communication

- [ ] Implement microphone selection and push-to-talk
- [ ] Integrate local speech-to-text, e.g. whisper.cpp
- [ ] Add speech transcript review
- [ ] Integrate local text-to-speech, e.g. Piper
- [ ] Create persistent voice profiles for named agents
- [ ] Implement mute, interruption and cancellation
- [ ] Route typed and spoken commands through the same communication hub

## 4.5 Verification

- [ ] Test message recipient routing
- [ ] Test persistent conversation history
- [ ] Test invalid recipients and malformed messages
- [ ] Test cross-session context recovery
- [ ] Test STT-to-message equivalence
- [ ] Test audio unavailable fallback
- [ ] Verify communication still works without AI

---

# Phase 5 — System Agents

## 5.1 Agent and task infrastructure

- [ ] Create named system program registry
- [ ] Define task requests, status, results and failures
- [ ] Build durable task queue
- [ ] Implement task broker as the only permitted host-action path
- [ ] Define agent-specific capabilities and allowed parameters
- [ ] Add permission policies and explicit approval requests
- [ ] Implement cancellation, timeouts and safe retries
- [ ] Persist task execution audit records

### Planned Master Control and agent action permissions (GRID-REQ-003)

- [ ] Define Master Control administration and each agent's allowed capabilities
- [ ] Enforce effective operator/agent permissions in services as well as terminal commands
- [ ] Keep human Master Control authority distinct from ARCHON coordination
- [ ] Audit the human operator and active agent identity for manually initiated actions
- [ ] Preserve scoped host-task approvals when operating through an agent session
- [ ] Define coordination between manual agent sessions and autonomous task execution
- [ ] Test denied actions, privilege escalation attempts, and manual/autonomous attribution

## 5.2 ARCHON — Administration

- [ ] Implement task coordination and delegation
- [ ] Receive user requests and split into approved subtasks
- [ ] Assign work to authorized specialist agents
- [ ] Track ongoing tasks and summarize results
- [ ] Request permissions rather than bypass them
- [ ] Produce verified operational briefings

## 5.3 FORGE — Development

- [ ] Register approved repositories and workspaces
- [ ] Implement read-only source inspection
- [ ] Implement fixed build/test capabilities
- [ ] Capture diagnostics, logs and test results
- [ ] Prepare suggested code changes
- [ ] Add sandbox/working-copy editing before source changes
- [ ] Request approval according to edit policy

## 5.4 SENTINEL — Security

- [ ] Read permitted Windows Defender status
- [ ] Read permitted firewall and security event information
- [ ] Separate real host alerts from fictional incidents
- [ ] Report verifiable warnings and scan times
- [ ] Keep initial security functions read-only
- [ ] Require approval for sensitive remediation

## 5.5 ARCHIVIST — Storage and files

- [ ] Inventory explicitly approved files and directories
- [ ] Monitor GRID database, model and asset storage
- [ ] Detect possible duplicate files
- [ ] Prepare non-destructive cleanup proposals
- [ ] Track the initial 20 GB logical GRID quota
- [ ] Escalate quota expansion proposals through ARCHON
- [ ] Require user approval for sensitive deletion and quota changes

## 5.6 VOLT and RELAY

- [ ] Implement CPU/RAM/GPU monitoring where supported
- [ ] Introduce safe GRID workload throttling
- [ ] Prioritize normal Windows performance
- [ ] Handle message transmission and notification delivery
- [ ] Support voice output and agent briefings
- [ ] Track notification delivery and failures

## 5.7 End-to-end task verification

- [ ] Ask ARCHON to delegate a unit-test run to FORGE
- [ ] Validate scope and permissions
- [ ] Execute approved test operation
- [ ] Record actual results and logs
- [ ] Return results through RELAY
- [ ] Test rejected and unauthorized requests
- [ ] Test cancellation, crash recovery and duplicate dispatch
- [ ] Confirm no agent obtains another agent's privileges

---

# Phase 6 — Intelligence

## 6.1 Shared local AI

- [ ] Install and configure local model runtime
- [ ] Choose and license-check an appropriate local model
- [ ] Implement shared local inference interface
- [ ] Queue concurrent requests to a single model service
- [ ] Measure memory, GPU and CPU use
- [ ] Allow deterministic operation when AI is unavailable

## 6.2 Grounded agent conversations

- [ ] Build prompt and context construction layer
- [ ] Retrieve current authoritative world state
- [ ] Retrieve relevant conversation and agent memories
- [ ] Attribute retrieved information to its sources
- [ ] Use distinct personality profiles for named agents
- [ ] Differentiate observed facts, suggestions and fictional dialogue
- [ ] Add long-conversation summarization and context limits

## 6.3 Task assistance and context (GRID-REQ-002)

- [ ] Interpret user requests as advice, analysis or proposed execution
- [ ] Read and analyze user-supplied context within permitted scope
- [ ] Provide recommendations without changing files when requested
- [ ] Support project-context retrieval across conversations
- [ ] Allow agents to collaborate through ARCHON
- [ ] Produce structured typed task proposals
- [ ] Validate each proposal against registered capabilities

## 6.4 Authorized editing workflows

- [ ] Support creating new output files in approved workspaces
- [ ] Generate patches or revised files from supplied context
- [ ] Show diffs or previews before sensitive edits
- [ ] Require approval according to file and operation risk
- [ ] Apply approved edits with rollback/backup where feasible
- [ ] Run optional builds and tests after edits
- [ ] Report exactly what changed and any failures
- [ ] Avoid silent deletion or unrestricted command execution

## 6.5 Intelligence verification

- [ ] Test grounded factual answers against known world state
- [ ] Test hallucinated state and unsupported assertions
- [ ] Test prompt injection contained in uploaded files
- [ ] Test permission enforcement against model-generated actions
- [ ] Test model timeout and offline fallback
- [ ] Test context/provenance retention and removal
- [ ] Test file modification approvals and rollback
- [ ] Demonstrate advice-only and approved editing workflows

---

# Phase 7 — The City

## 7.1 Godot foundation

- [ ] Render the persisted GRID-REQ-004 city layout without regenerating or taking ownership of authoritative world state
- [ ] Create Godot .NET viewer project
- [ ] Establish local communication with C# engine
- [ ] Render existing world state instead of simulating separately
- [ ] Build camera movement and observation controls
- [ ] Implement entity positions and selection
- [ ] Keep simulation running when viewer is closed

## 7.2 First visual vertical slice

- [ ] Model one realistic European stone street
- [ ] Add human-scale stone/brick facades and storefronts
- [ ] Use restrained digital lines in paving and entrances
- [ ] Implement fixed early-autumn evening sky
- [ ] Implement medium/heavy gray-blue clouds
- [ ] Create damp surfaces and believable puddles
- [ ] Add warm street and window lighting
- [ ] Add autumn trees, gentle wind and light haze

## 7.3 Citizen and agent appearance

- [ ] Build shared humanoid character rig
- [ ] Model light-gray civilian jacket and dark trousers
- [ ] Model structured gray officer tunic and standing collar
- [ ] Add black belt and tall black boots for officers
- [ ] Implement subtle luminous garment seams
- [ ] Create job-specific civilian variants
- [ ] Create named system-agent uniform variations
- [ ] Add walking, idle, working and talking animations

## 7.4 Districts and architecture

- [ ] Build five navigable districts
- [ ] Add credible roads, courtyards and public spaces
- [ ] Create Core landmark/reactor
- [ ] Create Processing Sector workshops and facilities
- [ ] Create Memory Banks and Archive institutional buildings
- [ ] Create Gateway communications/transit infrastructure
- [ ] Differentiate administrative and security architectural zones as world expands
- [ ] Use restrained original signage and insignia

## 7.5 World interaction

- [ ] Render the reconciled application population plus persistent residents from live simulation state; use twenty-resident fixtures only for tests
- [ ] Animate movement and program activity
- [ ] Click citizens to inspect actual status and history
- [ ] Display conversations with agents and residents
- [ ] Reflect real simulation events in lighting and activity
- [ ] Show resources and district conditions when requested
- [ ] Avoid invented on-screen events

## 7.6 Visual quality and performance

- [ ] Build reusable assets and materials
- [ ] Use LOD and instancing for repeated geometry
- [ ] Set adjustable graphics quality levels
- [ ] Profile lighting, reflections and fog costs
- [ ] Verify readable details at dusk
- [ ] Ensure the permanent evening look remains consistent
- [ ] Test viewer close/reopen without losing simulation state

---

# Phase 8 — Integration

## 8.1 Windows lifecycle

- [ ] Integrate GRID-REQ-004 first creation versus saved-world loading; hardware changes and failed loads must not silently regenerate the city
- [ ] Run GRID headless at Windows sign-in if enabled
- [ ] Allow independent startup and shutdown of viewer
- [ ] Handle Windows sleep and wake
- [ ] Handle clean shutdown and unexpected termination
- [ ] Resume saved state without offline simulation catch-up
- [ ] Coordinate local service startup order

## 8.2 Resource and storage governance

- [ ] Enforce host resource consumption limits
- [ ] Throttle simulation, AI and rendering appropriately
- [ ] Enforce GRID logical storage quota
- [ ] Track model caches and external asset storage
- [ ] Trigger quota warnings and ARCHIVIST proposals
- [ ] Approve quota increases through ARCHON → User
- [ ] Prevent GRID from exhausting host disk free space

## 8.3 Safety and reliability

- [ ] Implement recoverable backups and restore workflow
- [ ] Audit all file-editing and host-task capabilities
- [ ] Apply least-privilege execution
- [ ] Test full-disk and invalid-snapshot scenarios
- [ ] Test unavailable model and audio services
- [ ] Test task retry, cancellation and idempotency
- [ ] Test agent restart and communication reconnection

## 8.4 User experience and diagnostics

- [ ] Create optional WPF control/dashboard interface
- [ ] Show programs, tasks, health and permissions
- [ ] Implement configurable notifications and quiet hours
- [ ] Make meaningful status and error messages accessible
- [ ] Provide task history and change-review interface
- [ ] Document setup, recovery, backup and uninstall procedures

## 8.5 Release validation

- [ ] Run extended headless simulation
- [ ] Perform end-to-end assistant workflows
- [ ] Run security and unauthorized-access tests
- [ ] Profile memory, CPU, storage and rendering use
- [ ] Validate backup/restart consistency
- [ ] Verify offline operation without required subscriptions
- [ ] Package a reproducible Windows release
- [ ] Write final setup and user guide

---

# Release Gates

| Release | Required result |
|---|---|
| **GRID Genesis** (`v0.1.0-genesis`) | Five districts, twenty citizens, fixed-step clock, real terminal commands, passing tests |
| **GRID Foundation** | Persistent civilization, autonomous activity, persistent communications |
| **GRID Operator** | ARCHON delegates authorized PC tasks to specialist agents and reports verified results |
| **GRID Assistant** *(proposed additional release gate)* | Grounded advice, supplied context, approved editing and task assistance |
| **GRID City** | Interactive Godot 3D viewer displaying actual authoritative simulation state |

# Completed Genesis Steps

1. [x] Implement `Grid.Core/GridWorld.cs`.
2. [x] Implement `Grid.Core/GridWorldFactory.cs`.
3. [x] Initialize five districts and twenty citizens through the factory.
4. [x] Enforce valid memberships and coherent district transfers.
5. [x] Replace temporary terminal district demo with a genuine world-state interface.
6. [x] Implement `GridSimulationEngine` with fixed-step progression using the completed `GridClock`.
7. [x] Add command-loop terminal features and terminal tests; all 69 tests are passing.
8. [x] Build and run tests, then commit and tag Genesis when all acceptance criteria pass.

# Immediate Next Steps — Post-Genesis Recommendations

1. [x] Complete editable-field and session-context planning in sections 1.9.1–1.9.2.
2. [x] Implement and verify the validated editing API in section 1.9.3.
3. [x] Implement the terminal live view in section 1.10; session visibility follows login integration.
4. [x] Complete Phase 2.1 persistence architecture with identity, edit-result and audit extension requirements documented. Section 2.2 save/recovery is also complete; 2.3 history/observability is now complete; next implement 2.4 resources/host state with the 2.7 retrofit dependencies.
5. [ ] Integrate login and Master Control terminal editing in the later GRID-REQ-003 work; preserve the completed Genesis release.
6. [ ] Record completion notes and verification evidence as each item is finished.

**Note:** This checklist reproduces the full eight-phase planning scope as of this conversation. Completion indicators should be updated after each repository review; generated code and documentation are not evidence that a test has passed.
