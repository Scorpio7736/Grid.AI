# GRID7736

## Implementation status - 2026-10-17

The current implementation record tracks the released Genesis baseline, post-release changes, verification evidence, and remaining work. The default launch is paused; use --start to begin automatic ticking immediately.

See [Current implementation and verification](MD%20Project%20Files/Project_GRID_Current_Implementation_Status.md).

A local-first digital civilization prototype built with .NET 10. Genesis currently creates five districts and twenty citizens, with a fixed-step simulation engine and an interactive terminal.

## Run

Install the .NET 10 SDK, then from this repository:

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj
```

The world starts paused with a one-second tick duration. Each run creates a fresh in-memory world; saving and restoration are planned for Phase 2. Citizens initially remain Idle, so ticking advances simulation time without inventing activities or energy changes.

To start automatic ticks immediately:

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start
```

The application flag is case-insensitive. `--` passes it through dotnet run. `--no-build` may be added before `--` after a successful build. This starts the clock; status output still updates only when requested until live view is implemented.

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
| `step` or `tick` | Advance exactly once while paused. |
| `exit` | Cancel and await the scheduler, then exit. |

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

World-owned program setters now reject direct changes. Initialize programs before adding them to a world; use validated engine editing for later administrative changes. Ticks and district transfers update revisions so stale previews cannot overwrite newer state. Receipts are returned in memory; durable audit and save/load remain future work.

See the Program Editing Rules and Session Context and Authorization documents under MD Project Files for the contracts. Section 1.10 of the master checklist plans a continuously updating terminal live view and recent-events list.
