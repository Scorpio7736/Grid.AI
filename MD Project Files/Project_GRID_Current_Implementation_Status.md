# Project GRID - Current Implementation and Verification

**Reviewed:** 2026-10-17. Covers local HEAD `0b659e6` and the current working tree, including uncommitted changes. Remote synchronization was not checked.

## Release and completion record

`v0.1.0-genesis` identifies commit `0c1de64`, the original verified Genesis release (69 tests at that milestone). Later changes do not move or alter that tag. Current verification totals supersede historical counts only for the current tree.

| Checklist area | Current completion |
|---|---|
| 1.1-1.3 | Repository, solution/project references and domain models complete; historical commit verification is in the master checklist. |
| 1.4 | World ownership, district membership, transfers and deterministic five-district/twenty-citizen factory complete. |
| 1.5 | Fixed-step clock, start/pause, manual steps and elapsed simulation time complete. |
| 1.6 | Serialized energy updates, scheduler, cancellation, tick events and snapshots complete. |
| 1.7-1.8 | Extensible terminal and original Genesis verification/tag complete. Optional automatic startup added after release. |
| 1.9.1-1.9.2 | Editing rules and identity/session authorization design complete. |
| 1.9.3 | Validated administrative editing API, revisions and authorization lease boundary implemented and tested. |
| 1.10 | Live status display and quick recent-events list planned, not implemented. |

## Runtime and startup

The .NET 10 solution `Grid7736.slnx` contains Grid.Core, Grid.Simulation and Grid.Terminal, plus a matching test project for each. The factory uses seed 7736 and creates twenty citizens distributed across five districts, initially Idle with energy 100. Each process starts a fresh in-memory world.

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj
dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start
```

The first command starts paused; the second enables automatic ticking immediately. The flag is case-insensitive. Normal terminal commands are `help`, `status`, `programs`, `districts`, `inspect`, `start`, `pause`, `step`/`tick`, and `exit`. There is no login, edit, or live-view command yet. Exit, EOF and Ctrl+C cancel and await the scheduler. A blocked console read can remain on a background thread until input or process exit. Scheduler failures are reported with a nonzero result.

The default tick duration is one simulation second. Working and Moving consume one energy unit per second; Recharging gains two; Idle and Offline leave energy unchanged. Values clamp to 0-100 on ticks. Status alone does not launch a job, travel route or Windows task. The scheduler advances once per delay without catching up missed wall-clock time, permits one runner and rejects external Tick calls while that runner owns automatic ticking. Manual Step requires a paused clock. Snapshots copy consistent read-only state under the world/engine lock.

## Changes after the Genesis release

World-owned program values now reject direct setter writes. Initialization before ownership remains supported; it is not a complete authenticated creation API. Internal mutation access is granted from Core to Simulation. GridWorld exposes an instance ID, and programs/snapshots expose a revision. Changing commits increment revision once, including tick-driven energy changes and world transfers; no-op transactions do not. Revision overflow is checked before mutation.

`GridSimulationEngine.PrepareEdit(session, request, cancellationToken)` and `ApplyEdit(session, preview, cancellationToken)` use typed contracts in `Grid.Simulation.Editing`. `EditableFields` exposes the explicit field registry. Preparation validates and stages the whole request without mutation. Apply reauthorizes, validates engine/world/operator/session bindings and target revision, then atomically commits all changes under the shared world lock. Unrelated program changes do not cause conflicts; any target value change does, even if later reversed. Successful apply and no-op apply consume the preview once.

Initial fields are name, occupation, energy, status and district. Name/occupation trim whitespace, reject controls and enforce 80/120 UTF-16-unit limits. Energy must be finite and in 0-100; decimal input has a 1024-character limit, forbids exponents/thousands separators and checks exact bounds before float conversion. Invariant formatting expands scientific notation to parser-compatible decimal. Status accepts defined names, not numeric enums. District must exist in the same world and transfers preserve membership/population. Unknown or duplicate fields fail the whole request.

Outcomes are Prepared, Applied, NoChange, ValidationFailed, NotFound, Forbidden, Conflict and Cancelled. Cancellation is checked before commit, never partway through mutation. An edit neither advances simulation time nor overwrites LastTick. Receipts contain request/operator/session attribution, target, old/new values, revisions, simulation tick/time and UTC timestamp. They are returned in memory; no durable audit or event buffer is implied.

One-time assignments do not pin values: the next tick can change edited energy. Labels and occupations do not grant permissions. Field registration is a trusted typed extension point with parser, normalization, read and staged-value functions; adding a domain field also requires mutation support and tests. Terminal commands have their own explicit registration mechanism. Neither registry loads arbitrary plugins.

## Authorization boundary and deferred login

The engine accepts an optional IProgramEditAuthorizer. Its default denies administrative edits. A trusted provider must resolve current authentication, programs.edit permission, world scope and session generation, returning an IProgramEditLease that holds authorization valid through the operation. Lock order is authorization lease, then engine/world lock. The engine additionally checks human Master Control identity and preview bindings. Role strings supplied by callers are not authentication.

TestSupport supplies a fake provider compiled only into test projects. Production has no credential store, login provider, session expiry implementation or general session service. Existing Genesis commands remain unauthenticated; the new editing API fails closed. Future agent login is an operator view with separate human attribution and active-agent permissions, without implicit administrator authority or takeover of autonomous work.

## Events, persistence and next work

Typed tick events include energy changes and tick completion, but only LastTick is retained. Programs initially Idle produce no invented activity. Checklist 1.10 plans a continuously refreshed status view with a compact recent-events list, a bounded buffer with stable ordering, real action/transfer/edit reporting, return-to-prompt control, resize/cancellation/redirected-output behavior and future visibility filtering. Rendering must not drive simulation ticks.

Phase 2 must add save/load, consistent snapshots, durable events/audit and recovery. No offline catch-up, persistent world, undo, persistent overrides or bulk multi-program editing is implemented. The 20 GB quota remains a planning budget. Autonomous jobs, host telemetry/tasks, named-agent services, conversations, uploads, AI, voice, Godot/WPF clients and Windows startup integration remain later phases.

## Verification evidence

On 2026-10-17, after the --start change, `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false` passed **117 tests**, with zero failures or skips: Core 8, Simulation 85, Terminal 24. Simulation includes 48 editing cases covering validation, decimal boundaries, atomicity, membership, no-ops, revisions, stale/replayed/foreign previews, cancellation, authorization denial, generation changes, revocation ordering, typed extension and concurrency with ticks.

The previously recorded solution and terminal builds succeeded with zero warnings/errors. Startup smoke verification reported Running with --start; default status/step/inspect/exit smoke verification also passed. These checks establish current implementation behavior, not completion of future login, persistence or live-view acceptance criteria.

## Documentation map

- [README](../README.md)
- [City Design Style Guide](Project_GRID_City_Design_Style_Guide.md)
- [Design Specification](Project_GRID_Design_Specification.md)
- [Development and Languages Addendum](Project_GRID_Development_and_Languages_Addendum.md)
- [Environment Atmosphere Style Guide](Project_GRID_Environment_Atmosphere_Style_Guide.md)
- [Local First and Storage Addendum](Project_GRID_Local_First_and_Storage_Addendum.md)
- [Master Development Checklist](Project_GRID_Master_Development_Checklist.md)
- [Master Overview](Project_GRID_Master_Overview.md)
- [Program Editing Rules](Project_GRID_Program_Editing_Rules.md)
- [Program Fashion Style Guide](Project_GRID_Program_Fashion_Style_Guide.md)
- [Session Context and Authorization](Project_GRID_Session_Context_and_Authorization.md)
- [Terminal Identity and Master Control Addendum](Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md)
