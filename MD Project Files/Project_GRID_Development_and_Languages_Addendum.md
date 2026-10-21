# Project GRID — Development Roadmap & Language Architecture

## Offline voice - 4.4 Windows MVP completed (2026-10-21)

The original interactive host supports `voice devices/microphone/language/listen/review/edit/send/cancel/mute/speak/profile`. Hold Space to record (20-second maximum), release to transcribe with an installed offline Windows recognizer, review/correct, then explicitly send. Typed and microphone messages use the same dialogue service; speech never executes terminal commands. Playback starts muted and occurs only on explicit request. Saved registered-Agent profiles select Windows voice/rate/volume. Escape interrupts; identity/conversation changes prevent stale sends. Raw audio stays in transient memory and is not saved. Missing hardware or language/voice assets leave typed chat available.

Profiles are an additive validated field in existing schema-v13 communication JSON; legacy JSON defaults to no profiles. Restart and recovery preserve profiles and sent microphone-origin messages, not drafts/audio/device selection/mute state. Windows System.Speech is the implemented provider; whisper.cpp/Piper, attached-terminal audio, output-device selection and advanced voice options remain planned. 448 tests passed (18 new voice cases); real offline provider inventory succeeded. Physical microphone/speaker operation still needs an interactive smoke check. Earlier voice-as-planned statements below describe prior milestones. See [offline voice implementation](Project_GRID_Offline_Voice_Implementation.md).


## Installation editions and rendering - planned 2026-10-20

Installation will offer **PC GRID** (permanent machine installation, with advanced 3D options) or **Flash GRID** (mobile flash-drive package, with very simple lightweight 3D). The installer must explain the choice, validate the destination and retain the selected edition. Both share simulation/save/identity behavior and must run supported tasks offline after provisioning. Flash GRID keeps managed files with the portable package and requires no persistent host integration; graphics still run on the host PC. Quality differences must not regenerate or change the world.

See [edition details](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 7.6/8.6. Rendering budgets, packaging and migration remain planned; this addition changes documentation only.

## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all GRID runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 4.2B (implemented), 8.6 and 8.7 (planned). See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

## Communications foundation completion - 4.1 (2026-10-20)

**Implemented and verified.** Grid.Communication now defines recipient/message/conversation models and a local hub contract implemented by the simulation host. Exact name/full-ID routing, explicit participant identities, start/send/end/read operations, ordered bounded messages and schema v10 transactional transcript persistence are available. Conversations survive restart and backup recovery even when a historical application resident is absent; new sends require live original program identities.

**346 tests passed.** See [Communications Foundation](Project_GRID_Communication_Foundation.md) for APIs, identity boundaries, bounds and recovery evidence. These trusted local APIs store messages without authenticating the caller, executing text, claiming external delivery or generating replies. Terminal chat is still 4.2; voice, real delegation, LLM dialogue and production login remain planned. Earlier dated pending-communication/schema statements describe previous milestones.

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


## Resource/host foundation update - 2026-10-19

Section 2.4 now provides read-only host observations, explicit units/sources/freshness, fake-provider tests and a provenance-required additional-work resource-condition policy. Use resources for details or live for a compact CPU/RAM summary. Unsupported measurements and unknown application requirements remain unknown. Current observations are transient and cannot be edited into Windows allocations or restored from a save as fresh data.

Verification: 223 tests passed, build zero warnings/errors, and real Windows disposable-session sampling passed while simulation time stayed paused. See [Resources and Host State](Project_GRID_Resources_and_Host_State.md) for collector coverage, the exact policy and limitations. Application discovery/classification, per-application resource attribution and replacing application energy drain remain dependent on 2.7. Existing fictional Genesis citizens retain their old rules. Earlier deferred-telemetry statements below describe prior milestones, not this update.


## History implementation update - 2026-10-18

Section 2.3 now persists meaningful committed changes with world snapshots in schema v2. The history command supports entity/kind/severity/time filters and bounded paging. Routine Tick records are excluded; pending-buffer overflow is reported explicitly. Changing edits include attribution/context, but denied attempts and no-ops are not a comprehensive security audit. Structured operational errors are emitted to terminal output. See [History and Observability](Project_GRID_History_and_Observability.md) for retention, failure and API details.

Verification: 208 tests passed; solution build had zero warnings/errors; isolated terminal restart/query smoke passed. Earlier schema-v1 and deferred-history statements below describe the prior milestones. Live-view history remains process-local; the new saved-history path is separate. Application discovery, resource interpretation, resident classification and persistence filtering remain planned in 2.4/2.7.


## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

C# will own application classification, lifecycle reconciliation and telemetry interpretation through fake/Windows adapters. Persistence will save only agents/special residents alongside the world; transient ordinary residents are rebuilt from live discovery. Implement checklist 2.7 before expanding Phase 3 behavior. Python, AI and the viewer are not required for this foundation.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


## GRID-REQ-004 - Implementation sequencing (planned, 2026-10-18)

First-time world creation must discover motherboard, CPU, GPU, RAM, storage and network hardware, procedurally generate a unique hardware-informed city, and save it. Later startups must load and reuse that world rather than regenerate it. See the [generation contract](Project_GRID_Design_Specification.md#grid-req-004---hardware-generated-city-contract).

- **Phase 2:** define the normalized read-only discovery contract and test fixtures; establish durable creation/load storage; implement versioned C# generation and the initialize-once transaction. Store the creation profile, seed, world ID, versions, generated layout and evolving state.
- **Phase 7:** render the saved city using the existing architecture, atmosphere and fashion guides. Rendering must not own generation or persistence.
- **Phase 8:** connect startup, recovery and hardware-change reporting to the same saved-world flow. A hardware change cannot silently rebuild an existing city.

Preserve the completed Genesis work and the live-view implementation completed on 2026-10-18. The fixed Genesis factory remains the implemented baseline. Grid.Persistence now implements the 2.1 SQLite schema/migration foundation; section 2.2 now implements engine save/load and recovery. GRID-REQ-004 remains unimplemented. C# owns discovery orchestration, validation and generation; Python, C++, AI and Godot are not dependencies for initial creation. Procedural world sizing must be bounded by explicit performance budgets.

Implementation tests must cover deterministic profile/seed/version inputs, partial hardware discovery, multiple devices, first creation, reuse after restart and hardware changes, and recovery without replacing a valid world. No such tests are claimed to have run in this documentation update.

## Implementation status - 2026-10-17

Genesis is implemented and tagged v0.1.0-genesis. Sections 1.9.1 and 1.9.2 completed design contracts; 1.9.3 now implements the validated editing API. The actual solution is Grid7736.slnx with root-level Grid.Core, Grid.Simulation, Grid.Terminal, Grid.Persistence and their four matching test projects. Python/C++ integrations and the expanded layouts below remain proposals. Checklist 1.10 live view is now implemented (2026-10-18); sections 2.1 persistence architecture and 2.2 save/recovery are complete; next is 2.3 history and observability.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Design addendum v0.6**  
**Date:** October 15, 2026  
**Status:** Planning; proposals are not implemented unless explicitly stated.  
**Companion documents:** `Project_GRID_Design_Specification.md` and `Project_GRID_Local_First_and_Storage_Addendum.md`.

## 1. Purpose and principles

This addendum consolidates the development-path and programming-language decisions discussed after the prior GRID Markdown addendum. Project GRID is a local-first, zero-required-subscription Windows-based simulated digital civilization, with autonomous citizens and specialized agents performing permitted real-world PC tasks.

Principles:

1. **Simulation first, useful agents second, 3D city third.** Create a real, testable world before giving it cinematic representation.
2. **C# owns authoritative state.** Python, C++, and visualization clients return information or proposed actions; only the authorized engine changes simulation state.
3. **Local and free by default.** No required cloud services, paid AI API, hosted database, or subscription.
4. **No dependency on AI for basic operation.** Core behavior, task routing, and deterministic commands run without a language model.
5. **Permission-controlled task execution.** AI-generated text never becomes arbitrary PowerShell, SQL, or native commands.
6. **Build incrementally, test each subsystem, profile before optimizing.** Add languages and processes when their value is clear.
7. **Respect host performance and storage.** Rendering and AI yield to ordinary PC activity, and the 20 GB initial storage quota remains governed by JN → MCP → User approval.

## 2. Eight-milestone development roadmap

| Milestone | Name | Deliverables | Rough solo part-time estimate |
|---|---|---|---|
| 1 | Genesis | Repository, solution, domain models, simulation tick, terminal, unit tests | 1–2 weeks |
| 2 | The Core | SQLite persistence, snapshots, event log, pause/recovery, clock | 2–3 weeks |
| 3 | First Programs | Autonomous citizens, needs, movement, jobs, utility decisions | 2–4 weeks |
| 4 | Communications | Terminal messaging, persistence, microphone/STT, TTS | 2–4 weeks |
| 5 | System Agents | MCP coordination; MAXIS, SENTINEL, JN; VOLT/RELAY functions | 3–5 weeks |
| 6 | Intelligence | One shared local model, grounded dialogue, memory retrieval, safe action proposals | 2–4 weeks |
| 7 | The City | Godot 3D scene, districts, agent visualization, interaction | 4–8 weeks |
| 8 | Integration | Startup, throttling, quotas, backups, notifications, robustness | 3–5 weeks |

*Estimates are planning ranges, not commitments. Feature overlap and visual ambition can change schedules substantially.*

### Release gates

- **GRID Genesis (`v0.1.0-genesis`):** console simulation with five districts and twenty uniquely identified citizens; start/pause/tick/status; passing tests.
- **GRID Foundation:** persistent world, autonomous activity, conversational terminal, and optional voice.
- **GRID Operator:** authorized local task execution coordinated by MCP, including MAXIS builds/tests and SENTINEL/JN reports.
- **GRID City:** 3D observer connected to the existing authoritative simulation, not a parallel simulation.

## 3. Milestone 1: Genesis — detailed plan

### Original small-solution plan (Genesis implemented)

```text
Project-GRID/
├── README.md
├── LICENSE
├── .gitignore
├── GRID.slnx
├── docs/
│   ├── architecture/
│   ├── requirements/
│   ├── roadmap/
│   └── decisions/
├── src/
│   ├── Grid.Core/
│   ├── Grid.Simulation/
│   └── Grid.Terminal/
├── tests/
│   ├── Grid.Core.Tests/
│   └── Grid.Simulation.Tests/
├── assets/
└── tools/
```

Recommended start: .NET 10, C#, xUnit, Git; add SQLite in Milestone 2. Create further libraries when features warrant them rather than making many unused placeholders.

```powershell
mkdir Project-GRID
cd Project-GRID
git init
dotnet new sln -n GRID
mkdir src, tests, docs
dotnet new classlib -n Grid.Core -f net10.0 -o src/Grid.Core
dotnet new classlib -n Grid.Simulation -f net10.0 -o src/Grid.Simulation
dotnet new console -n Grid.Terminal -f net10.0 -o src/Grid.Terminal
dotnet new xunit -n Grid.Core.Tests -f net10.0 -o tests/Grid.Core.Tests
dotnet new xunit -n Grid.Simulation.Tests -f net10.0 -o tests/Grid.Simulation.Tests

dotnet sln add src/Grid.Core/Grid.Core.csproj
dotnet sln add src/Grid.Simulation/Grid.Simulation.csproj
dotnet sln add src/Grid.Terminal/Grid.Terminal.csproj
dotnet sln add tests/Grid.Core.Tests/Grid.Core.Tests.csproj
dotnet sln add tests/Grid.Simulation.Tests/Grid.Simulation.Tests.csproj

dotnet add src/Grid.Simulation reference src/Grid.Core
dotnet add src/Grid.Terminal reference src/Grid.Simulation
dotnet add tests/Grid.Core.Tests reference src/Grid.Core
dotnet add tests/Grid.Simulation.Tests reference src/Grid.Simulation
dotnet build
dotnet test
```

### First domain objects

- `GridWorld`: authoritative civilization state.
- `GridProgram`: unique ID, name, occupation, location, energy, status.
- `GridDistrict`: district ID, name, resources and program membership.
- `GridClock`: start, pause, step, simulation tick count independent of wall time.
- `GridSimulationEngine`: advances one fixed simulation step, emits events.

### Genesis acceptance criteria

- A five-district world and twenty uniquely identified programs initialize from a seed.
- A 1-second starting tick interval can be configured, paused, and advanced manually.
- Terminal commands show genuine state rather than hardcoded sample text (`status`, `programs`, `inspect`, `start`, `pause`, `exit`).
- Tests verify unique IDs, world creation, ticking, pause behavior, and command outputs.
- The solution builds; tests pass; a release tag can be created.

## 4. Milestones 2–8: implementation details

### Milestone 2 — Persistent Core

Use SQLite for state and event history. Add periodic consistent snapshots, transactional changes, clean shutdown, restart recovery, crash recovery, and sleep/resume semantics. **No offline catch-up**: simulation time advances only while the engine actually runs. Seed random generators for reproducible tests. Establish migration/versioning for saved-world schema.

### Milestone 3 — Autonomous Programs

Implement needs (energy, recharge, work), goals, occupations, district movement, simple relationships, and memory. A rule/utility-based action selector chooses between movement, work, recharging, transferring resources, and social interactions. Validate by running twenty autonomous citizens for an hour and inspecting coherent events. Local AI must not be required.

### Milestone 4 — Communications

Build terminal command parsing; persistent messages, conversations, and program addresses; then add microphone capture and offline speech-to-text and text-to-speech. Interfaces share one communication hub. Demonstrate starting in terminal, continuing through voice, and recovering conversation text after restart. Rule-based replies precede generative conversation.

#### Planned terminal identity extension (GRID-REQ-003)

Add a future login/session layer supporting the User as Master Control and authorized operation through each registered agent identity. Extend the existing command registry with session context and capability metadata; keep authentication, authorization, and identity registration independent of console I/O. Plan identity/audit persistence in Milestone 2, session UX in Milestone 4, and enforce real agent capabilities in Milestone 5. Authentication method and final naming remain undecided.

See [Terminal Login and Master Control Plan](Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md). This is planning only; Genesis has no login and its completed release scope remains unchanged.

The planned Master Control terminal also includes program-value editing. Add typed editable-field metadata and command presentation during terminal/session work, with service-layer authorization and validated engine mutations. Coordinate edits with ticks, use world transfers for location changes, audit old/new values, and connect edits to persistence. Initial fields are name, occupation, energy, status, and district; program IDs and authorization settings are separate concerns.

### Milestone 5 — System Agents

- **MCP:** assign work, coordinate programs, synthesize status and approval requests.
- **MAXIS:** execute only approved build/test capabilities in approved local repositories.
- **SENTINEL:** monitor permitted Windows security status, initially read-only.
- **JN:** monitor data usage and prepare cleanup or quota-expansion proposals.
- **VOLT:** observe system resources and throttle the Grid's own workload.
- **RELAY:** route messages, voice output, briefings, and notifications.

First end-to-end task: `MCP, ask MAXIS to run the GRID unit tests.` The typed command becomes a validated task, MAXIS executes a fixed allowable operation, and MCP reports the verified result. Use audited, idempotent task execution with cancellation and safe retries.

### Milestone 6 — Local Intelligence

Use a **shared local LLM inference service** (e.g., Ollama plus an appropriately licensed model). Retrieve authoritative world data and relevant memories; let the model generate text or structured *proposals*. Validate all action requests against a capability allowlist, agent identity, and approval policy. Conversations cannot invent authoritative world facts or execute arbitrary commands. Degrade gracefully if inference is unavailable.

### Milestone 7 — The City

Use Godot's .NET/C# tooling (GDScript selectively if useful) to render a five-district digital city with a reactor, simple structures, paths and agents. Render state supplied by the simulation, including positions and activity. Begin with twenty visible programs, camera controls, selecting/inspecting programs, and conversation overlays. Do not implement a second authoritative world inside Godot.

### Milestone 8 — Windows Integration

Launch at sign-in, retain headless operation when viewer closes, throttle workload based on host activity, handle sleep/shutdown, create reliable backups, configure notification policies, and enforce storage quotas. Test extended running, unavailable AI service, full disk, invalid snapshots, process failures, unexpected reboot, and recovery. Keep normal PC operation prioritized.

## 5. Development workflow

Keep one repository and a stable, buildable `main` branch. Use narrow feature branches:

```text
main
├── feature/world-clock
├── feature/program-identity
├── feature/simulation-ticks
├── feature/sqlite-persistence
└── feature/terminal-commands
```

Every completed feature should include tests, documentation, and a demonstration or reproducible result. Track work as issues and record architectural choices in Architecture Decision Records (ADRs), especially persistence, inter-language contracts, permission boundaries, and viewer separation.

### First Genesis sprint checklist

- [x] Initialize repository, README, .gitignore and .NET solution.
- [ ] Choose and add a repository license (no license file found in this review).
- [x] Define world, program and district domain models.
- [x] Implement deterministic, pauseable world clock.
- [x] Generate five districts and twenty programs.
- [x] Create terminal commands for inspection and control.
- [x] Add xUnit coverage and run all tests.
- [x] Tag a verified Genesis build.

**Postpone:** photorealism, full autonomous coding, thousands of expensive model-driven agents, intricate government structures, and unrestricted shell execution.

## 6. Primary multi-language architecture

GRID is intentionally polyglot, but **language boundaries follow responsibilities**, not novelty.

| Language | Planned responsibility | Adoption |
|---|---|---|
| **C# / .NET** | Authoritative world, simulation clock, agents, permissions, communications, task scheduler, Windows integration, CLI, WPF | Primary from Genesis |
| **Python** | Local AI adapters, speech/ML experiments, analysis, safe automation utilities | Start with contract proof; expand when useful |
| **C++** | High-performance algorithms, native integrations, optimized batch computation | Start with contract proof; optimize only when justified |
| **SQL (SQLite)** | Persistence queries, agent memories, tasks, histories, events | Milestone 2 |

C# owns all authoritative simulation mutations. Python and C++ act as specialized helpers returning results. This preserves testability, crash isolation, and coherent world state.

### Integration patterns

| Boundary | Recommended transport |
|---|---|
| C# ↔ Python | Local HTTP API, standard input/output, or another versioned IPC protocol |
| C# ↔ C++ | Native library with stable C ABI, consumed by .NET P/Invoke |
| C# ↔ SQLite | `Microsoft.Data.Sqlite` or suitable database layer |
| Python ↔ Ollama | Loopback HTTP API |
| C# simulation ↔ Godot viewer | Shared versioned events/snapshots over local IPC |

Use **structured, versioned schemas** in `contracts/schemas`. Define request ID, version, inputs, outputs, errors, and timeouts. Avoid direct database writes from Python or C++. Keep services bound locally by default and avoid unnecessary permanent background processes.

### Cross-language proof of concept

1. C# creates twenty programs and produces a world-status object.
2. Python receives structured status and returns a simple analysis (initially deterministic; no LLM needed).
3. C++ computes a batch energy-distribution calculation with a stable native interface.
4. C# validates/merges returned values and prints a terminal report.
5. Tests prove malformed payloads, timeouts, missing dependencies, and calculation errors are handled safely.

## 7. Expanded monorepo structure

Introduce subfolders when the associated feature is active:

```text
Project-GRID/
├── GRID.slnx
├── README.md
├── docs/
│   ├── architecture/
│   ├── requirements/
│   ├── roadmap/
│   └── decisions/
├── contracts/
│   └── schemas/
├── src/
│   ├── dotnet/
│   │   ├── Grid.Core/
│   │   ├── Grid.Simulation/
│   │   ├── Grid.Persistence/
│   │   ├── Grid.Communication/
│   │   ├── Grid.Host/
│   │   ├── Grid.Agents/
│   │   └── Grid.Terminal/
│   ├── python/
│   │   ├── grid_ai/
│   │   │   ├── dialogue/
│   │   │   ├── planning/
│   │   │   ├── memory/
│   │   │   └── service.py
│   │   ├── tests/
│   │   └── pyproject.toml
│   ├── cpp/
│   │   └── Grid.Native/
│   │       ├── include/
│   │       ├── src/
│   │       ├── tests/
│   │       └── CMakeLists.txt
│   └── godot/
│       └── Grid.Viewer/
├── tests/
├── tools/
└── assets/
```

**Note:** The original smaller Genesis layout and this expanded structure are compatible alternatives; choose the final folder placement once at project initialization, then avoid unnecessary reorganizations.

## 8. Additional languages considered

| Language | Useful GRID role | Decision |
|---|---|---|
| **TypeScript** | React/browser-based command dashboard, live graphs, optional authenticated LAN/mobile access | Recommended later if web UI is desired |
| **Lua** | Lightweight embedded scripts for citizen behaviors, districts, events and modding | Interesting future expansion |
| **GDScript** | Godot scene interactions, visual logic, animation glue | Optional alongside Godot C# |
| **PowerShell** | Windows installation, diagnostics, deployment and tightly controlled administrative scripts | Practical supporting tool |
| **Rust** | Safe native telemetry/network/processing modules | Defer; overlaps with planned C++ |
| **C** | C ABI and specific low-level OS/native integrations | Only if needed |

### Lua example: fictional behavior

```lua
function on_energy_low(program)
    program:request_recharge()
    program:notify_supervisor()
end
```

Lua would operate in a sandbox with no unrestricted filesystem, network, OS commands, or ability to bypass GRID permissions. Only exposed, validated simulation APIs would be callable.

### TypeScript dashboard possibility

An optional local site such as `http://localhost:5000` could display population, energy, tasks, agent status, transmissions, historical charts, and storage proposals. A browser interface also creates a possible future LAN/phone experience. Any non-loopback exposure must require appropriate authentication, authorization and transport security; no public network access by default.

### Other tools

- **PowerShell** for reproducible Windows developer and deployment operations, not open-ended AI-generated commands.
- **GDScript** for Godot-specific work where advantageous over C#.
- **Rust** only for a demonstrated need not well handled by C++.
- **C** for exported interop functions or specialized native tasks, rather than a separate principal implementation language.

## 9. Optional GRID domain-specific language (DSL)

A future human-readable declarative DSL could represent program policies without rebuilding source code:

```text
PROGRAM CL-047
ROLE EnergyDistributor
DISTRICT CentralReactor

WHEN Energy < 25
    REQUEST Recharge
    NOTIFY Supervisor
END

WHEN WorkAvailable
    PERFORM AssignedTask
END
```

This is **conceptual syntax**, not an existing compiler or language. A future parser could translate permitted statements into validated actions. It must not permit arbitrary code execution or bypass the simulation's capability system.

## 10. Priority and language adoption sequence

| Stage | Languages/tooling |
|---|---|
| **Genesis** | C#, xUnit, CLI; optionally minimal Python/C++ interop proofs |
| **Persistent World** | Add SQL/SQLite |
| **Autonomous Intelligence** | Python for experiments and local AI; C++ where profiling shows benefit |
| **3D City** | Godot C#, selectively GDScript |
| **Administration** | PowerShell utilities; optionally TypeScript dashboard |
| **Expansion/Modding** | Potentially Lua or custom DSL; consider Rust only on demonstrated need |

An illustrative distribution of development effort might be **65% C#, 25% Python, 10% C++** for these three principal languages. This is not a required line-count ratio and can shift as the project evolves.

## 11. Constraints retained from earlier specifications

- Free/local-first, with optional downloadable offline models and no mandatory paid cloud integrations.
- C# simulation authoritative; deterministic core functions even if AI is unavailable.
- One shared model serves major conversational agents; not one large model per citizen.
- Agent tasks pass through permission, capability, audit and approval checks.
- Real-world system effects are distinct from fictional world events.
- World history persists across PC restarts while *simulated time remains frozen* when the engine is stopped.
- JN monitors the **20 GB initial logical quota**, proposes cleanup or ~10 GB increments through MCP, and requires User approval. Quota expansion is **not** drive repartitioning.
- Godot viewer may close while a lightweight simulation service continues.
- Prefer storing text transcripts, not raw microphone recordings, by default.

## 12. Open implementation decisions

1. Select a canonical monorepo layout before creating source projects.
2. Confirm target Windows version, available CPU/RAM/GPU, and compiler toolchain.
3. Choose first inter-language payload contract (simple JSON over subprocess or local HTTP).
4. Decide when C++ performance work becomes justified by measurements.
5. Decide between WPF-only controls and a later TypeScript dashboard.
6. Decide whether Lua scripting or a custom DSL merits a dedicated milestone.
7. Specify model licensing, memory budget, and startup/throttling behavior for local AI.
8. Choose the first end-to-end cross-language acceptance test and keep it part of CI.

## 13. Immediate next step

**Genesis is complete:** an executable, unit-tested C# world with 5 districts, 20 programs, a clock and terminal inspection. The editing API is also complete. Live view is implemented; the SQLite schema/migration foundation is also implemented; section 2.2 save/recovery is now complete; proceed to section 2.3 history and observability. Session visibility filtering will accompany future login integration. Maintain the Python and C++ integration plan and optionally create a small interoperability smoke test, but do not let cross-language plumbing delay a functioning civilization.

After Genesis, add persistence, meaningful autonomous behavior, and a communication hub. This produces a sound foundation for the later AI, agent, and 3D phases.


## Existing-code adaptation plan - 2026-10-18

Prioritize classification and lifecycle foundations, then coordinated persistence/startup migration, then real telemetry and the defined resource-condition policy. Integrate editing and live view and verify the complete path before extending Phase 3. Keep the twenty-citizen factory as a deterministic demo/test fixture; retain the clock, transaction, recovery and snapshot infrastructure.

See [existing implementation retrofit](Project_GRID_Host_Programs_and_Resources.md#existing-implementation-retrofit---planned-2026-10-18) and checklist 2.7.1. Documentation only; implementation remains pending.
