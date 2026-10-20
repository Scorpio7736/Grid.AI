# Project GRID - Current Implementation and Verification

## Terminal conversations completion - 4.2 (2026-10-20)

**Implemented and verified.** Use connect (default ARCHON), talk, conversations, transcript, resume and disconnect. ARCHON is created on first explicit connection as a persistent coordinator Agent; a stable world-local operator handle supports reopening saved conversations. Replies to status/who are you are labeled verified GRID state; other replies are labeled deterministic simulated dialogue and claim no task execution/delegation. Input/reply pairs commit atomically and reuse schema v10.

**353 tests passed**, plus a three-launch isolated terminal save/resume/end smoke. See [Terminal Conversations](Project_GRID_Terminal_Conversations.md) for syntax, identity/capacity limits and completion evidence. This local operator handle grants no authentication or admin authority; 4.2A login, real task delegation and LLM dialogue remain planned. Earlier dated pending-terminal-chat/agent-creation statements describe previous milestones.

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

## Bare Grid7736 command support - 2026-10-20

Startup now also installs a managed `Grid7736.cmd` launcher in `%LOCALAPPDATA%\GRID\bin` and adds that directory to the Windows user PATH, preserving existing entries and avoiding duplicates. Type `Grid7736` directly in PowerShell or Command Prompt; arguments such as `--start` are forwarded. The persistent `Grid7736` environment variable remains supported. An existing unmanaged launcher is not overwritten. This extends the original variable-only 1.14 behavior.

Restart the parent terminal app to inherit the new PATH. For the currently open PowerShell session, refresh it without closing the window:

```powershell
$env:Path += ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
Grid7736
```

Verification: **89 terminal tests passed**. The bare command resolved to the installed launcher and successfully ran/exited with `--ephemeral --no-color`; no saved world was opened.

## Startup launcher environment variable - 1.14 (2026-10-20)

**Implemented and verified.** On Windows startup, GRID ensures a persistent **user-scope** environment variable named `Grid7736` points to the terminal `.exe`. A missing, empty or broken path is registered using the current build's apphost; an existing valid absolute path is preserved, including another installed build. The current GRID process receives the same value. Values are stored without shell quotes, so paths with spaces remain usable. If no executable is available or Windows denies the environment update, startup reports a warning and GRID continues. Other platforms skip Windows registration. No administrator elevation, shell-profile changes or automatic simulation start are required. The follow-up above also adds a user-PATH launcher.

PowerShell launch (from a fresh environment):

```powershell
& $env:Grid7736
# Optional: begin simulation immediately
& $env:Grid7736 --start
```

Command Prompt launch:

```bat
"%Grid7736%"
```

The environment variable is a path value; the managed PATH launcher above separately provides the bare `Grid7736` command. Already-open shells do not inherit changes from GRID. Restart the parent terminal/IDE, or sign out/in if necessary. To refresh only the current PowerShell shell without restarting:

```powershell
$env:Grid7736 = [Environment]::GetEnvironmentVariable('Grid7736', 'User')
```

A development-build path remains tied to that build's `.exe`; rebuild that target to pick up changes. Launching another build preserves a still-valid registered installation. If the target is removed, launching GRID directly registers the current available apphost again. This is a convenience launcher, not an installer or authentication mechanism.

**Verification:** 87 terminal tests passed, including missing/empty/broken values, paths with spaces, preservation of an existing installation and rejection of a dotnet-host launcher. An ephemeral Release launch registered the actual Windows user variable and verified that its executable exists. No saved world was opened or modified. Current verified value: `C:\Users\jack\source\repos\Grid7736\Grid.Terminal\bin\Release\net10.0\Grid.Terminal.exe`.

## NGE implementation - 1.13 (2026-10-19)

**Initial local-terminal NGE implemented.** Type `nge` in an interactive saved-world session, review the world identity and exact file list, then enter `y` to confirm or `n`/blank to cancel. Invalid answers reprompt; EOF cancels deletion and follows normal EOF shutdown; Ctrl+C cancels deletion and follows normal graceful shutdown. Arguments such as `nge y` are rejected. Redirected/unattended input and ephemeral sessions cannot delete saves. This is local-operator confirmation, **not authenticated admin enforcement**; human-admin-only access remains future login/authorization work.

Confirmed NGE stops/awaits scheduler, autosave and telemetry, serializes against in-flight saves, revalidates world/backup identity, then removes only the active database, rolling `.backup` and exact `-journal`, `-wal`, `-shm` paths. It retains the session lease through completion and refuses symbolic-link/junction targets. Historical legacy/damaged archives, staging artifacts of uncertain ownership, external copies, other worlds and PC applications remain; the warning explicitly states this scope. The harmless session `.lock` file may remain after release. Interrupted recovery must be resolved before NGE can be prepared.

A flushed `.nullifying` marker is created before deletion. Partial failure retains that marker, blocks startup/save/recovery, reports remaining files and exits nonzero without a final save. **There is no automatic retry/resume command yet:** inspect the marker and remaining exact world artifacts before explicit manual cleanup; never remove the marker merely to bypass an unresolved operation. Successful NGE removes the marker and exits with the GRID7736 artwork and `GRID NULLIFIED // EXISTENCE TERMINATED`, never a saved-world claim. Next normal startup follows existing hardware-city creation; it does not reuse a managed backup. Independent historical/manual copies remain outside this deletion scope.

Verification: **306 Release tests passed** (14 Core, 127 Simulation, 83 Persistence, 82 Terminal). New coverage checks cancellation/blank/invalid/EOF input, uppercase confirmation, redirected/ephemeral rejection, worker shutdown before deletion, no final save on success/failure, owned-file deletion, unrelated-file preservation, rejected forged plans, blocked future writes/recovery, new identity after reopen and a Windows locked-file failure retaining the marker. All deletion tests use freshly created disposable worlds; no user save was deleted. Existing hardware-city creation tests cover the startup path; no manual destructive run against the user's world was performed. Visual Studio still owns its post-exit footer.

## Closing artwork and default color - 2026-10-19

The farewell now uses the user's supplied large GRID7736 ASCII artwork and blank-line spacing. Successful persistent exits retain `Stopping GRID.`, `World saved.`, then the artwork and `WORLD SAVED // USER DISCONNECTED`. Error and ephemeral messages remain truthful. Consoles narrower than 78 columns use a compact GRID7736 farewell. Ordinary terminal output and input echo default to light blue (bright cyan / ConsoleColor.Cyan); status, warning and error accents remain. The prior shell foreground is restored on exit. `--no-color`, NO_COLOR, TERM=dumb and redirected output continue to disable application color changes. Terminal verification: **75 tests passed**; no claim of visual color matching across user console themes.

## Terminal commands and shutdown complete - 1.12 (2026-10-19)

Implemented grouped `help`, display-only `clear`, `programs -active`, `programs -possible`, `programs -sleeping`, and a GRID7736 ASCII farewell. Active selects fresh Running applications; sleeping selects fresh On call or Sleeping applications; possible includes current applications plus the saved known-application catalog. `ids` works with one filter. Unknown/stale observations are never classified as active or sleeping. Bare `programs` still includes agents and other residents.

**Schema v6** adds a separate ApplicationCatalog table (key, name, source, last catalog observation UTC). The catalog is captured with the simulation snapshot and saved atomically with world/history; backup/recovery and schema validation include it. Installed/observed metadata survives restart; ordinary residents, PID lifetimes, activity and resource readings are not restored. Last observation is catalog provenance, not proof of a launch or current installation. Entries missing from fresh discovery display Historical / unknown. Existing schema-v5-only statements below describe earlier milestones.

The ASCII banner appears after graceful shutdown and final save. Save/shutdown errors retain diagnostics and a nonzero exit code without a success message; ephemeral sessions explicitly say NOT SAVED. There is no keypress wait. Visual Studio can still append its own debug-console footer. Help uses portable ASCII separators and registered command metadata, including extensions; no-color/redirection behavior is preserved.

**Verification:** `dotnet test -c Release --no-restore`: **297 passed** (14 Core, 127 Simulation, 81 Persistence, 75 Terminal). Tests include filters/IDs/invalid flags/empty results, clear preserving the world and tick, successful/failed farewell, catalog save/reopen/recovery and v5-to-v6 migration. A disposable built-terminal run exercised all command groups, filters, redirected clear fallback and successful save/reopen of the same city. An ephemeral PTY run preserved tick1 through clear and displayed the NOT SAVED banner. This tool's PTY uses the clear fallback; physical screen clearing/color appearance and Visual Studio footer presentation were not visually verified in the user's console. No IDE settings or existing user worlds were modified.

## Latest population revision - 2026-10-19

The current population includes discoverable installed applications and current-session background processes. States are **Sleeping**, **On call**, **Running**, or **unknown** when evidence is insufficient. This supersedes earlier windowed-only/removal-on-exit descriptions below; those remain historical completion records. Ordinary application state is still rediscovered rather than persisted, and the saved city is reused. Full-machine coverage is not complete: see [Installed Application Availability](Project_GRID_Installed_Application_Availability.md) for implemented scope, Windows inventory limits, remaining work and **290-test** verification.

## Terminal usability completion - 1.11 (2026-10-19)

Implemented aligned/wrapped tables for programs, districts, resources, behaviors and history. Narrow output falls back to labeled records; empty tables are explicit. `programs ids` and `districts ids` expose full identifiers; `resources details`, `history details` and `inspect` retain detailed provenance. Live view retains paging and resize handling, with prominent Esc/Q Back hints and a return-to-prompt message.

Colors supplement labels: green for running/available/work, yellow for unknown/stale/waiting/warnings, red for errors, cyan for navigation/table headings; default terminal color otherwise. Use `--no-color`, `NO_COLOR`, or `TERM=dumb` for monochrome. Redirected output suppresses application color changes. Compact tables sanitize controls and use ASCII-safe text; original names remain available in inspection.

Esc/Q return to commands; `exit` or Ctrl+C shuts down. Automated tests cover repeated live/prompt round trips, same-world and clock preservation, no save/dispose on Back, final save on exit, width20/60/120, long values, empty data, color policy and plain output. Existing tests cover resize, paging and high event volume. The actual PTY smoke used the single-frame fallback because this tool's terminal did not meet interactive redraw requirements; it returned to a Running prompt and advanced to tick5. Full-screen visual color/contrast assessment in the user's terminal remains a manual check, not claimed as verified. Combined suite: **290 tests passed**.

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
