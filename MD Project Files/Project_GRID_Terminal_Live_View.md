# Project Grid.AI - Terminal live view

## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all Grid.AI runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md), checklist 4.2B (implemented), active Phase 7 and future plans B.6–B.7. See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

## Local login and identity completion - 4.2A (2026-10-20)

**Implemented and verified.** Start with `login master`: the first successful Windows-token login binds the world owner; later logins must match. Sessions expire after eight hours. Master Control can register existing Agent identities, switch roles, and preview/confirm one-time program edits. Prompts/help/commands are role-aware; agent chats are participant-scoped and global/admin controls are denied. Switching/logout clears active chats and previews. Schema v11 preserves owner/agent registration without restoring authentication.

**362 tests passed**, plus isolated Windows login/agent restriction/confirmed edit/restart smoke. See [Local Login and Identity Implementation](Project_GRID_Local_Login_and_Identity_Implementation.md) for commands, service authorization, audit and security limits. This authenticates the current Windows account, grants no OS elevation, and provides no independent agent passwords or real task service. NGE now requires Master Control; --start waits for login. Earlier dated pending-login/editing statements refer to prior milestones.

## Movement and occupations completion - 3.2 (2026-10-20)

**Implemented and verified.** Saved city roads now support deterministic district travel for explicitly configured Agent/Special residents assigned to compatible capacity-one workstations. Five simulated seconds per edge; no work progress while traveling; shift/resource uncertainty freezes travel. Trusted assignment/release APIs preserve world ownership and revision checks. `jobs` and `activity` add read-only workstation and measured application views. Host applications acquire no fictional civic jobs.

Schema v7 preserves station assignments, remaining routes and edge timing in existing behavior JSON, validates them on restore and performs no offline catch-up. Older unassigned routines retain their 3.1 behavior. No agent population or interactive admin job configuration is added. **323 tests passed** in isolated Verification builds; disposable jobs/activity/save/reopen smoke passed. A running user Release session locked its binaries and was left untouched; rebuild the normal target after closing it to use the changes. See [Movement and Occupations](Project_GRID_Movement_and_Occupations.md) for policy, limits and full completion evidence. Earlier statements that movement/workplaces are pending refer to previous milestones.

## Closing artwork and default color - 2026-10-19

The farewell now uses the user's supplied large Grid.AI ASCII artwork and blank-line spacing. Successful persistent exits retain `Stopping GRID.`, `World saved.`, then the artwork and `WORLD SAVED // USER DISCONNECTED`. Error and ephemeral messages remain truthful. Consoles narrower than 78 columns use a compact Grid.AI farewell. Ordinary terminal output and input echo default to light blue (bright cyan / ConsoleColor.Cyan); status, warning and error accents remain. The prior shell foreground is restored on exit. `--no-color`, NO_COLOR, TERM=dumb and redirected output continue to disable application color changes. Terminal verification: **75 tests passed**; no claim of visual color matching across user console themes.

## Terminal commands and shutdown complete - 1.12 (2026-10-19)

Implemented grouped `help`, display-only `clear`, `programs -active`, `programs -possible`, `programs -sleeping`, and a Grid.AI ASCII farewell. Active selects fresh Running applications; sleeping selects fresh On call or Sleeping applications; possible includes current applications plus the saved known-application catalog. `ids` works with one filter. Unknown/stale observations are never classified as active or sleeping. Bare `programs` still includes agents and other residents.

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

## Resource/host foundation update - 2026-10-19

Section 2.4 now provides read-only host observations, explicit units/sources/freshness, fake-provider tests and a provenance-required additional-work resource-condition policy. Use resources for details or live for a compact CPU/RAM summary. Unsupported measurements and unknown application requirements remain unknown. Current observations are transient and cannot be edited into Windows allocations or restored from a save as fresh data.

Verification: 223 tests passed, build zero warnings/errors, and real Windows disposable-session sampling passed while simulation time stayed paused. See [Resources and Host State](Project_GRID_Resources_and_Host_State.md) for collector coverage, the exact policy and limitations. Application discovery/classification, per-application resource attribution and replacing application energy drain remain dependent on 2.7. Existing fictional Genesis citizens retain their old rules. Earlier deferred-telemetry statements below describe prior milestones, not this update.


## History implementation update - 2026-10-18

Section 2.3 now persists meaningful committed changes with world snapshots in schema v2. The history command supports entity/kind/severity/time filters and bounded paging. Routine Tick records are excluded; pending-buffer overflow is reported explicitly. Changing edits include attribution/context, but denied attempts and no-ops are not a comprehensive security audit. Structured operational errors are emitted to terminal output. See [History and Observability](Project_GRID_History_and_Observability.md) for retention, failure and API details.

Verification: 208 tests passed; solution build had zero warnings/errors; isolated terminal restart/query smoke passed. Earlier schema-v1 and deferred-history statements below describe the prior milestones. Live-view history remains process-local; the new saved-history path is separate. Application discovery, resource interpretation, resident classification and persistence filtering remain planned in 2.4/2.7.


## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

Future live view will identify application residents versus persistent residents, show starts/confirmed exits and observed resource conditions, and label stale/unsupported measurements as unknown. Energy must be labeled as a Grid.AI interpretation with a defined policy. UI refresh observes engine snapshots and must not discover processes or invent events independently. The current live view still displays fictional Genesis citizens.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


**Implemented and verified:** 2026-10-18, checklist section 1.10. Session-specific visibility filtering remains deferred until login exists.

## Use

Launch Grid.AI normally, or add `--start` to begin automatic ticking:

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start
```

At the `grid>` prompt, enter `live`. Use Q or Esc to return to the command prompt; the simulation keeps its current running/paused state. Use `start` or `pause` at the prompt to control the clock, then reenter `live`.

| Control | Effect |
|---|---|
| Q / Esc | Return to commands without stopping the world |
| Page Up / Page Down | Previous/next citizen page |
| Left / Right arrow | Older/newer retained event pages |
| Ctrl+C | End the whole terminal session and await scheduler shutdown |

The view refreshes every 250 ms independently of simulation ticks. It shows running/paused state, tick, elapsed simulation time, program/district counts, current program statuses, energy, occupation and district. Citizens initially remain Idle; the display does not claim autonomous jobs, travel routes or host tasks exist.

Recent events show sequence, simulation tick/time, event type, relevant program name/ID and actual change details. They include tick completion, energy changes, start/pause transitions, direct world transfers and applied administrative edits. Edit events include changed fields and old/new values, including status and district changes. Previews, denied/invalid/cancelled/conflicting edits and no-ops do not produce fabricated committed changes. No terminal edit command or authentication provider was added by this work.

## Display behavior and limits

- Interactive redraw requires unredirected input/output, a usable console at least 60 columns by 18 rows, and TERM other than `dumb`.
- Redirected, unsupported or too-small consoles print one plain-text snapshot and return to normal command processing. A redraw error also restores the surface and takes this fallback path.
- The view adapts to size changes and pages citizens/events to fit. It reserves the last row/column, writes only changed rows and restores cursor visibility when leaving. This avoids repeatedly appending frames to scrollback. Entering/leaving clears the active screen; previous command output is not restored.
- Display is capped at 240 columns/100 rows. Compact text uses printable ASCII; controls and non-ASCII glyphs appear as `?` and clipped lines end with `+`. The source names/events remain intact in memory. Use ordinary inspection for full program details.
- The fallback is a single 120-column/60-row page, not a continuously updating stream or a complete history export.
- The event buffer retains 256 events by default. Engine construction can configure 1-10,000 entries with `recentEventCapacity`. Oldest events are evicted; the display reports the eviction count rather than pretending complete history.
- Sequence IDs increase within one engine instance. They are not persisted or globally unique. The retained snapshot contains each sequence once; intermediate events remain available until eviction. High activity can evict events between refreshes, so this is not a lossless audit trail.

## Implementation and extension

`GridSimulationEngine.GetLiveSnapshot()` copies state and the bounded event buffer under the shared world/engine lock. LastTick still describes the latest tick; the new journal retains additional committed events independently. State and event reads do not change time or values. Direct world transfers notify the engine through an internal observer; administrative transfers are represented by their edit event. Preserve the existing one-engine-per-active-world ownership convention.

Command registration returns a `LiveViewRequested` result. TerminalHost delegates to TerminalLiveView only after its normal line read has completed, so two readers do not compete for keyboard input. The scheduler runs independently; cancellation or scheduler failure cancels live mode and restores the surface before host shutdown.

LiveViewRenderer is pure formatting. ILiveTerminal separates console capabilities, keys and painting from engine and command logic. New committed action types should be recorded at their authoritative commit boundary, never inferred from a periodic screen diff. Future authenticated interfaces must filter state and events consistently before rendering; current live mode has the same unauthenticated world visibility as the existing Genesis commands. It does not establish a new permission policy.

Section 2.2 now saves/restores world state and clock values, autosaves every 30 seconds (including during live view), and saves after shutdown. Autosave failures end the session through the same live-view cleanup path. Recent events and sequence IDs still reset on restart; durable event history and GRID-REQ-004 hardware-generated cities remain future work. Edit receipts retain their separate attribution contract; live event summaries are not replacements for receipts or durable audit.

## Verification

- `dotnet test Grid.AI.slnx --no-restore --verbosity minimal -p:NuGetAudit=false`: 140 passed, zero failed/skipped (Core 8, Simulation 93, Terminal 39). This adds 8 event-buffer cases and 15 terminal/live-view cases to the previous 117 tests.
- Tests cover bounded retention/eviction, sequence ordering, coherent concurrent snapshots, actual transfers/edits, no-op/denial/cancellation behavior, idle truthfulness, rendering bounds, text sanitization, paging, changed snapshots, resize fallback, Q/Esc, return to commands, cancellation and scheduler-failure cleanup.
- `dotnet build Grid.AI.slnx --no-restore --verbosity minimal`: zero warnings/errors.
- Interactive console smoke checks verified live running ticks, return to commands, paused display and normal exit. TERM=dumb smoke verification produced a single fallback snapshot and accepted later commands. Automated injected-console tests cover resize and cancellation paths; this does not claim exhaustive validation of every terminal emulator.

See [Master Development Checklist](Project_GRID_Master_Development_Checklist.md) and [Current Implementation Status](Project_GRID_Current_Implementation_Status.md) for broader scope and historical verification.
