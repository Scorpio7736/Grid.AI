# GRID7736

## Planned population change - GRID-REQ-005 (2026-10-18)

The target design now represents running PC applications as transient ordinary residents, with persistent agents and explicitly designated special programs. Resources will reflect observed application use, explicit/estimated requirements and host conditions; energy will be a defined GRID interpretation rather than arbitrary stamina. This is **planned, not implemented**: current code still creates twenty fictional citizens, saves every citizen and uses fixed Genesis energy rates.

See [Host programs, lifecycles and resources](MD%20Project%20Files/Project_GRID_Host_Programs_and_Resources.md) and checklist 2.4/2.7. The existing save/recovery implementation and historical test results below retain their original scope. This update changes Markdown only.


## Implementation status - 2026-10-17

The current implementation record tracks the released Genesis baseline, post-release changes, verification evidence, and remaining work. The default launch is paused; use --start to begin automatic ticking immediately.

See [Current implementation and verification](MD%20Project%20Files/Project_GRID_Current_Implementation_Status.md).

A local-first digital civilization prototype built with .NET 10. Genesis currently creates five districts and twenty citizens, with a fixed-step simulation engine and an interactive terminal.

## Run

Install the .NET 10 SDK, then from this repository:

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj
```

The world starts paused with a one-second tick duration. Normal launches create and save Genesis once, then load the same world from `%LOCALAPPDATA%\GRID\world.db`. Autosave runs every 30 seconds; exit, EOF and Ctrl+C save after the scheduler stops. Restored worlds start paused unless `--start` is supplied, with no offline time advancement. Citizens initially remain Idle, so ticking advances simulation time without inventing activities or energy changes.

To start automatic ticks immediately:

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start
```

The application flag is case-insensitive. `--` passes it through dotnet run. `--no-build` may be added before `--` after a successful build. This starts the clock. Enter `live` at the prompt for continuously updating status and recent events.

## Commands

| Command | Behavior |
|---|---|
| `help` | Show registered commands and usage. |
| `status` | Show clock state, tick count, simulation time, and population. |
| `programs` | List citizens, IDs, energy, status, and location. |
| `districts` | List districts, IDs, and populations. |
| `inspect program <id or name>` | Show a citizen's actual state. |
| `inspect district <id or name>` | Show description, population, resources, and residents. |
| `start` | Enable automatic ticks. |
| `pause` | Stop automatic advancement. |
| `live` | Live status and recent events; Q/Esc returns to commands without pausing. |
| `step` or `tick` | Advance exactly once while paused. |
| `exit` | Stop the scheduler, finish autosave, save the world, then exit. |

Commands and name lookups ignore case. Full GUIDs disambiguate duplicate names. Multiword names can be quoted or supplied as the remaining words:

```text
inspect district "The Core"
inspect program Citizen 001
step
status
start
pause
exit
```

End-of-input and Ctrl+C also stop the scheduler. Redirected input omits interactive prompts. No commands invoke a system shell.

## Extend the terminal

The terminal separates command registration, dispatch, and session lifecycle:

- `TerminalCommand` supplies a name, usage, description, and handler returning `CommandResult`.
- `CommandDispatcher.Register` adds commands and rejects duplicate names. `help` lists the registry automatically.
- `DefaultCommands.Create` wires the built-in commands to the engine.
- `TerminalHost` owns input/output and scheduler shutdown, accepting `TextReader` and `TextWriter` for tests.
- `GridSimulationEngine.GetSnapshot()` copies a consistent read-only view under the tick lock. Commands should use snapshots for reads rather than exposing mutable world entities.

For example, after creating the default dispatcher:

```csharp
commands.Register(new TerminalCommand(
    "about", "about", "Describe GRID.",
    args => new CommandResult(args.Count == 0 ? "GRID Genesis" : "Usage: about")));
```

New world-changing commands should use validated engine operations that serialize with ticks. The registry is an in-process extension point, not a runtime plugin loader. The host is intended for one process session; a blocked console read may remain on a background thread until input arrives or the process exits after cancellation.

## Verify

```powershell
dotnet test Grid7736.slnx
dotnet build Grid7736.slnx
```

Tests cover world ownership, clock behavior, engine updates and cancellation, terminal parsing and output, extension registration, snapshots, and session shutdown. See `MD Project Files/Project_GRID_Master_Development_Checklist.md` for completion evidence and remaining work. On 2026-10-17, all 117 tests passed: 8 Core, 85 Simulation (including 48 editing cases), and 24 Terminal.

## Administrative editing API (post-Genesis)

The engine now exposes PrepareEdit and ApplyEdit with typed values from Grid.Simulation.Editing. EditableFields provides field metadata and parsing. Previews are immutable, tied to the world/engine/session, and checked against the program revision. Multi-field edits commit atomically and return attributed old/new value receipts without advancing simulation time.

Administrative editing denies access by default. A trusted IProgramEditAuthorizer must supply a live authorization lease through commit; no production login provider or terminal edit command exists yet. Test authorization providers live only in TestSupport and are compiled only into test assemblies.

World-owned program setters now reject direct changes. Initialize programs before adding them to a world; use validated engine editing for later administrative changes. Ticks and district transfers update revisions so stale previews cannot overwrite newer state. Receipts are returned in memory; current edited values and revisions are saved by section 2.2; durable audit remains future work.

See the Program Editing Rules and Session Context and Authorization documents under MD Project Files for the contracts. Section 1.10 is implemented: see [Terminal Live View](MD%20Project%20Files/Project_GRID_Terminal_Live_View.md) for paging controls, event retention, console fallback and extension details. On 2026-10-18, all 140 tests passed and the solution built with zero warnings/errors.

## Persistence foundation (Phase 2.1)

Grid.Persistence now supplies a file-backed SQLite schema and transactional version migrations. `SqliteGridDatabase.Initialize()` explicitly creates schema; `OpenExisting()` validates/upgrades an initialized file and never creates a missing database. Tables cover worlds, districts, programs, district resources and simulation clock state. Foreign keys and constraints enforce stored relationships and basic invariants.

Section 2.2 now connects the terminal to transactional saves, validated restoration, periodic saves and explicit backup recovery. Durable event/audit writing remains section 2.3 and later. See [Persistence Architecture](MD%20Project%20Files/Project_GRID_Persistence_Architecture.md) for schema and extension details.

Verification on 2026-10-18: 171 solution tests passed (31 persistence cases); solution build succeeded with zero warnings/errors.

## Save and recovery (Phase 2.2)

Implemented and verified on 2026-10-18. Normal launches reuse `%LOCALAPPDATA%\GRID\world.db`, autosave every 30 seconds and save after the clock stops on exit, EOF or Ctrl+C. Restored worlds start paused unless `--start` is passed. World/program identities, revisions, memberships, resources and simulation time survive restart; powered-off time is never simulated.

- `--world <path>` selects a separate world file in an existing folder.
- `--recover` explicitly restores that file's validated `.backup`, preserving damaged files. Use it with the same `--world` path, or alone for the default world.
- `--ephemeral` runs a disposable in-memory session; it cannot be combined with `--world` or `--recover`.
- A second process cannot open the same world while the first holds its session lease.
- Invalid saves stop startup rather than generating a replacement. Abrupt termination can lose changes since the last committed snapshot.

Verification: **200 tests passed** (8 Core, 93 Simulation, 50 Terminal, 49 Persistence), with a successful real-terminal create/reopen/corruption/recovery smoke test using an isolated temporary database. See [Persistence Architecture](MD%20Project%20Files/Project_GRID_Persistence_Architecture.md) for recovery behavior and test limits. Durable event history (2.3), hardware-generated cities (2.6), login and Windows service integration remain planned.
