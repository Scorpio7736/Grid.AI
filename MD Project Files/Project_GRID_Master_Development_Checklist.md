# Project GRID — Master Development Checklist

**Project:** GRID — Local-first Digital Civilization and Personal Task Assistant  
**Roadmap:** Eight milestones  
**Checklist baseline:** GitHub repository `Scorpio7736/Grid7736`, reviewed commit `f5ee323` (local HEAD and GitHub master matched); sections 1.1–1.5 completion evidence updated on 2026-10-16.
**Status convention:** `[x]` = observed in the reviewed repository or current working tree (independent verification is noted where performed); `[ ]` = pending or not verified.
**Scope:** Includes GRID-REQ-002 conversational advice, uploaded context, authorized task execution, and file editing.

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
- The terminal demonstration remains separate from the factory-created world of twenty citizens.

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

The terminal still uses its original district demonstration; connecting it to the world factory remains pending.

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

Automatic scheduling and world updates remain in section 1.6; terminal clock commands remain in section 1.7.

## 1.6 Simulation engine

- [ ] Create GridSimulationEngine.cs
- [ ] Advance the authoritative world with fixed simulation steps
- [ ] Define a clear world-update sequence
- [ ] Generate actual world events during ticks
- [ ] Prevent updates while paused
- [ ] Support safe cancellation and graceful stopping
- [ ] Avoid dependence on any 3D renderer or AI service

## 1.7 Terminal interface

- [ ] Replace temporary sample code with a command loop
- [ ] Implement help, status and programs commands
- [ ] Implement inspect command for a program or district
- [ ] Implement start and pause commands
- [ ] Implement manual tick/step command
- [ ] Implement exit with graceful shutdown
- [ ] Handle invalid commands without crashing
- [ ] Display genuine world state, never fabricated status

## 1.8 Genesis tests and release

- [x] Create Grid.Core.Tests xUnit project
- [x] Create Grid.Simulation.Tests xUnit project
- [x] Test uniqueness of IDs
- [x] Test district/program initialization and membership
- [x] Test duplicate and invalid district/program rejection
- [x] Test movement and population invariants
- [x] Test simulation clock and fixed-step advancement
- [x] Test pause, resume and manual stepping
- [ ] Test terminal command parsing and outputs
- [x] Run dotnet build successfully
- [x] Run dotnet test successfully
- [ ] Add Genesis usage instructions to README
- [ ] Tag v0.1.0-genesis

**Genesis release gate:** A console simulation with five districts and twenty unique programs; start, pause, tick, status, programs, inspect, and exit work using actual state; automated tests pass.

---

# Phase 2 — The Core

## 2.1 Persistence architecture

- [ ] Create persistence layer/project when needed
- [ ] Select and integrate SQLite
- [ ] Design tables for districts, programs and resources
- [ ] Design simulation-state and clock storage
- [ ] Establish primary keys and relationships
- [ ] Enable transactions and foreign-key enforcement
- [ ] Create a versioned database schema and migrations

## 2.2 Save and recovery

- [ ] Save world state transactionally
- [ ] Load and validate saved world state
- [ ] Add periodic consistent snapshots
- [ ] Implement safe startup and shutdown saves
- [ ] Handle interrupted writes and corrupt files
- [ ] Restore from last known-good snapshot
- [ ] Preserve identities, district memberships and clock state
- [ ] Ensure no simulation progress during powered-off time

## 2.3 History and observability

- [ ] Create persistent event log
- [ ] Record changes with timestamps and involved entities
- [ ] Record movement, resources and significant world events
- [ ] Support filtering and querying history
- [ ] Avoid unnecessary logging of routine ticks
- [ ] Add structured logs with severity and context

## 2.4 Resources and host state

- [ ] Define initial Energy, Materials and Data resource rules
- [ ] Establish resource limits and update invariants
- [ ] Create abstract interfaces for host telemetry
- [ ] Add fake telemetry provider for repeatable tests
- [ ] Add approved read-only Windows telemetry
- [ ] Smooth and bound effects of host metrics on fictional resources
- [ ] Prevent fictional resource changes from affecting Windows allocations

## 2.5 Verification

- [ ] Test save → close → reopen consistency
- [ ] Test transaction rollback and crash recovery
- [ ] Test migration from an older schema
- [ ] Test no offline catch-up
- [ ] Test invalid or missing database behavior
- [ ] Verify persistent world runs without graphics or AI

---

# Phase 3 — First Programs

## 3.1 Program needs and behavior

- [ ] Implement energy expenditure and recharge
- [ ] Implement work/rest routines
- [ ] Add needs, goals and action priorities
- [ ] Define legal states and state transitions
- [ ] Implement occupations and simple daily work
- [ ] Use deterministic state machines or utility scores

## 3.2 Movement and occupations

- [ ] Define district connections and movement rules
- [ ] Add movement goals and route selection
- [ ] Add jobs and workstations
- [ ] Assign programs to suitable jobs
- [ ] Connect work to fictional resource production/consumption
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
- [ ] Run twenty citizens autonomously for an hour
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

- [ ] Render twenty citizens from live simulation state
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

# Immediate Next Steps — Updated After Simulation Clock 1.5

1. [x] Implement `Grid.Core/GridWorld.cs`.
2. [x] Implement `Grid.Core/GridWorldFactory.cs`.
3. [x] Initialize five districts and twenty citizens through the factory.
4. [x] Enforce valid memberships and coherent district transfers.
5. [ ] Replace temporary terminal district demo with a genuine world-state demo.
6. [ ] Implement `GridSimulationEngine` with fixed-step progression using the completed `GridClock`.
7. [ ] Add command-loop terminal features and engine/terminal tests; Grid.Core.Tests and the clock tests in Grid.Simulation.Tests are implemented and passing.
8. [ ] Build and run tests, then commit and tag Genesis when all acceptance criteria pass.

**Note:** This checklist reproduces the full eight-phase planning scope as of this conversation. Completion indicators should be updated after each repository review; generated code and documentation are not evidence that a test has passed.
