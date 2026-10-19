# Project GRID — Development Roadmap & Language Architecture

## GRID-REQ-004 - Implementation sequencing (planned, 2026-10-18)

First-time world creation must discover motherboard, CPU, GPU, RAM, storage and network hardware, procedurally generate a unique hardware-informed city, and save it. Later startups must load and reuse that world rather than regenerate it. See the [generation contract](Project_GRID_Design_Specification.md#grid-req-004---hardware-generated-city-contract).

- **Phase 2:** define the normalized read-only discovery contract and test fixtures; establish durable creation/load storage; implement versioned C# generation and the initialize-once transaction. Store the creation profile, seed, world ID, versions, generated layout and evolving state.
- **Phase 7:** render the saved city using the existing architecture, atmosphere and fashion guides. Rendering must not own generation or persistence.
- **Phase 8:** connect startup, recovery and hardware-change reporting to the same saved-world flow. A hardware change cannot silently rebuild an existing city.

Preserve the completed Genesis work and the live-view implementation completed on 2026-10-18. The fixed Genesis factory remains the implemented baseline. Grid.Persistence now implements the 2.1 SQLite schema/migration foundation; engine save/load and GRID-REQ-004 remain unimplemented. C# owns discovery orchestration, validation and generation; Python, C++, AI and Godot are not dependencies for initial creation. Procedural world sizing must be bounded by explicit performance budgets.

Implementation tests must cover deterministic profile/seed/version inputs, partial hardware discovery, multiple devices, first creation, reuse after restart and hardware changes, and recovery without replacing a valid world. No such tests are claimed to have run in this documentation update.

## Implementation status - 2026-10-17

Genesis is implemented and tagged v0.1.0-genesis. Sections 1.9.1 and 1.9.2 completed design contracts; 1.9.3 now implements the validated editing API. The actual solution is Grid7736.slnx with root-level Grid.Core, Grid.Simulation, Grid.Terminal, Grid.Persistence and their four matching test projects. Python/C++ integrations and the expanded layouts below remain proposals. Checklist 1.10 live view is now implemented (2026-10-18); section 2.1 persistence architecture is also complete; next is 2.2 save and recovery.

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
7. **Respect host performance and storage.** Rendering and AI yield to ordinary PC activity, and the 20 GB initial storage quota remains governed by ARCHIVIST → ARCHON → User approval.

## 2. Eight-milestone development roadmap

| Milestone | Name | Deliverables | Rough solo part-time estimate |
|---|---|---|---|
| 1 | Genesis | Repository, solution, domain models, simulation tick, terminal, unit tests | 1–2 weeks |
| 2 | The Core | SQLite persistence, snapshots, event log, pause/recovery, clock | 2–3 weeks |
| 3 | First Programs | Autonomous citizens, needs, movement, jobs, utility decisions | 2–4 weeks |
| 4 | Communications | Terminal messaging, persistence, microphone/STT, TTS | 2–4 weeks |
| 5 | System Agents | ARCHON coordination; FORGE, SENTINEL, ARCHIVIST; VOLT/RELAY functions | 3–5 weeks |
| 6 | Intelligence | One shared local model, grounded dialogue, memory retrieval, safe action proposals | 2–4 weeks |
| 7 | The City | Godot 3D scene, districts, agent visualization, interaction | 4–8 weeks |
| 8 | Integration | Startup, throttling, quotas, backups, notifications, robustness | 3–5 weeks |

*Estimates are planning ranges, not commitments. Feature overlap and visual ambition can change schedules substantially.*

### Release gates

- **GRID Genesis (`v0.1.0-genesis`):** console simulation with five districts and twenty uniquely identified citizens; start/pause/tick/status; passing tests.
- **GRID Foundation:** persistent world, autonomous activity, conversational terminal, and optional voice.
- **GRID Operator:** authorized local task execution coordinated by ARCHON, including FORGE builds/tests and SENTINEL/ARCHIVIST reports.
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

- **ARCHON:** assign work, coordinate programs, synthesize status and approval requests.
- **FORGE:** execute only approved build/test capabilities in approved local repositories.
- **SENTINEL:** monitor permitted Windows security status, initially read-only.
- **ARCHIVIST:** monitor data usage and prepare cleanup or quota-expansion proposals.
- **VOLT:** observe system resources and throttle the Grid's own workload.
- **RELAY:** route messages, voice output, briefings, and notifications.

First end-to-end task: `ARCHON, ask FORGE to run the GRID unit tests.` The typed command becomes a validated task, FORGE executes a fixed allowable operation, and ARCHON reports the verified result. Use audited, idempotent task execution with cancellation and safe retries.

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
- ARCHIVIST monitors the **20 GB initial logical quota**, proposes cleanup or ~10 GB increments through ARCHON, and requires User approval. Quota expansion is **not** drive repartitioning.
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

**Genesis is complete:** an executable, unit-tested C# world with 5 districts, 20 programs, a clock and terminal inspection. The editing API is also complete. Live view is implemented; the SQLite schema/migration foundation is also implemented; proceed to section 2.2 save and recovery. Session visibility filtering will accompany future login integration. Maintain the Python and C++ integration plan and optionally create a small interoperability smoke test, but do not let cross-language plumbing delay a functioning civilization.

After Genesis, add persistence, meaningful autonomous behavior, and a communication hub. This produces a sound foundation for the later AI, agent, and 3D phases.
