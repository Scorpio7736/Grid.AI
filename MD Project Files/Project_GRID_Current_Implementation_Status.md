# Project GRID - Current Implementation and Verification

## Planned terminal usability follow-up - 2026-10-19

Checklist **1.11** now tracks clearer layouts, aligned tables, accessible color coding and obvious live-view Back navigation. Q/Esc already return to the command prompt in the current source; the pending task improves discoverability and verifies/fixes any environment-specific quitting behavior. This is a documentation-only request, not a completed UI change. See [Terminal Live View](Project_GRID_Terminal_Live_View.md#planned-usability-and-navigation-improvements---111).

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

Documentation now targets transient residents for running PC applications, persistent agents/special programs, and resource-based energy. None of that new behavior is implemented yet. Runtime still uses twenty fictional citizens, saves them all and applies fixed Genesis energy rates. The 200-test result below verifies 2.2, not this new requirement.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


**Reviewed:** 2026-10-17. Covers local HEAD `0b659e6` and the current working tree, including uncommitted changes. Remote synchronization was not checked.

## Persistence architecture update - 2026-10-18

Section 2.1 is complete: SQLite schema v1, explicit initialization, existing-file validation, foreign keys and atomic migrations. At the 2.1 milestone, 171 tests passed (including 31 persistence tests), with zero build warnings/errors. Section 2.2 subsequently completed engine save/load, snapshots and recovery; durable audit and GRID-REQ-004 remain pending. See [Persistence Architecture](Project_GRID_Persistence_Architecture.md). Earlier totals below describe their original verification milestones.

## Live-view update - 2026-10-18

Section 1.10 is implemented. The full solution now passes 140 tests (8 Core, 93 Simulation, 39 Terminal), and builds with zero warnings/errors. Historical 117-test evidence below remains dated to 1.9.3. See [Terminal Live View](Project_GRID_Terminal_Live_View.md) for controls, limitations and verification. No login, persistence or hardware-generated city functionality is added.

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
| 1.10 | Live status, bounded recent events, paging and console fallback implemented 2026-10-18; session visibility awaits login. |

## Runtime and startup

The .NET 10 solution `Grid7736.slnx` contains Grid.Core, Grid.Simulation, Grid.Terminal and Grid.Persistence, plus a matching test project for each. The factory uses seed 7736 and creates twenty citizens distributed across five districts, initially Idle with energy 100. Normal launches create and save Genesis once, then reload `%LOCALAPPDATA%\GRID\world.db`. Use --world <path> for a separate database, --recover for explicit validated backup restoration, or --ephemeral for a disposable in-memory world.

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj
dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start
```

The first command starts paused; the second enables automatic ticking immediately. The flag is case-insensitive. Normal terminal commands are `help`, `status`, `programs`, `districts`, `inspect`, `start`, `pause`, `live`, `step`/`tick`, and `exit`. There is no login or edit command yet. Enter `live` for updating state/events; Q/Esc returns to commands without changing running/paused state. Exit, EOF and Ctrl+C cancel and await the scheduler and autosave before a final save. Autosave runs every 30 seconds; failures are visible and return a nonzero result. A blocked console read can remain on a background thread until input or process exit. Scheduler failures are reported with a nonzero result.

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

Typed tick events include energy changes and tick completion. LastTick retains the latest tick; a separate bounded recent-event buffer now retains committed events across ticks. Programs initially Idle produce no invented activity. Checklist 1.10 implements a 250 ms live view, a default 256-entry event buffer with stable sequence IDs and eviction counts, committed transfer/edit summaries, paging, Q/Esc return, resize/cancellation handling and one-snapshot console fallback. Visibility filtering remains future login integration. Rendering must not drive simulation ticks.

Sections 2.1-2.2 now provide a persistent world, consistent snapshots and recovery. Durable events/audit remain 2.3 work. Offline catch-up is deliberately excluded; undo, persistent overrides and bulk multi-program editing are not implemented. The 20 GB quota remains a planning budget. Autonomous jobs, host telemetry/tasks, named-agent services, conversations, uploads, AI, voice, Godot/WPF clients and Windows startup integration remain later phases.

## Verification evidence

On 2026-10-17, after the --start change, `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false` passed **117 tests**, with zero failures or skips: Core 8, Simulation 85, Terminal 24. Simulation includes 48 editing cases covering validation, decimal boundaries, atomicity, membership, no-ops, revisions, stale/replayed/foreign previews, cancellation, authorization denial, generation changes, revocation ordering, typed extension and concurrency with ticks.

The previously recorded solution and terminal builds succeeded with zero warnings/errors. Startup smoke verification reported Running with --start; default status/step/inspect/exit smoke verification also passed. These checks establish current implementation behavior, not completion of future login; later persistence evidence is recorded below. Live-view verification was subsequently completed on 2026-10-18.

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

## Save and recovery completion - 2026-10-18

Section 2.2 is implemented. Normal startup creates and saves fixed Genesis once or validates and restores the existing world. Autosave runs every 30 seconds. Shutdown awaits the scheduler and periodic saver before the final save. World identities, program revisions, values, memberships, resources and clock state survive restart, with fresh runtime/session bindings and no offline advancement.

Verification: **200 tests passed**, zero failures/skips (8 Core, 93 Simulation, 50 Terminal, 49 Persistence). The solution build passed with zero warnings/errors. A temporary-world terminal smoke test verified creation, two steps, shutdown save, paused reopen at tick 2, corruption rejection and explicit --recover restoration. --ephemeral --start also passed. The user's default database was not used for verification. See [Persistence Architecture](Project_GRID_Persistence_Architecture.md) for operational details and limits; physical power-loss testing is not claimed.

Remaining work begins with 2.3 durable history/observability. Hardware discovery/generated cities remain 2.6, and login, terminal editing commands, archive retention, quota enforcement and Windows service startup remain planned.


## Existing-code adaptation plan - 2026-10-18

The existing implementation needs a planned retrofit of program classification, safe removal/reconciliation, production factory use, all-resident persistence, fixed energy rules, and editing/live-view field semantics. Clock behavior, transactional saves, backup recovery, command structure and consistent snapshots remain reusable. None of these retrofits is implemented by this note.

See [existing implementation retrofit](Project_GRID_Host_Programs_and_Resources.md#existing-implementation-retrofit---planned-2026-10-18) and checklist 2.7.1. Documentation only; implementation remains pending.
