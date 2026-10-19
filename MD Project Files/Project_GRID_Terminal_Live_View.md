# Project GRID - Terminal live view

## Planned usability and navigation improvements - 1.11

**Requested 2026-10-19; planned, not implemented by this update.** See checklist 1.11 for clearer formatting, color coding and tables across both command output and live view. Prioritize readable status summaries and aligned comparable data; reserve long IDs/provenance for detail views. Support resizing, paging/wrapping and monochrome/plain-text output. Colors must supplement explicit labels so unknown/stale data is never mistaken for zero or healthy state.

Live view must show a prominent **Back to command prompt: Esc / Q** hint, distinct from **Exit GRID**. Returning restores the prompt without stopping the simulation, changing running/paused state or interrupting telemetry/autosave. Reentering live must work repeatedly. Ctrl+C is the existing shutdown signal, not a navigation shortcut.

**Current implementation:** Q/Esc already return from TerminalLiveView to TerminalHost's command loop; existing tests cover this path. Improve discoverability and verify it in the user's actual terminal. If the reported quit behavior is reproducible, fix that discrepancy. Do not mark this usability task complete solely because the underlying return path already exists.

Acceptance: readable tables at supported sizes, clear empty/unknown states, accessible color labels and no-color behavior, no ANSI pollution in redirected output, and a live → prompt → live round trip that preserves the same world and clock state. This update changes planning documentation only; no runtime tests were run.

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

Future live view will identify application residents versus persistent residents, show starts/confirmed exits and observed resource conditions, and label stale/unsupported measurements as unknown. Energy must be labeled as a GRID interpretation with a defined policy. UI refresh observes engine snapshots and must not discover processes or invent events independently. The current live view still displays fictional Genesis citizens.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


**Implemented and verified:** 2026-10-18, checklist section 1.10. Session-specific visibility filtering remains deferred until login exists.

## Use

Launch GRID normally, or add `--start` to begin automatic ticking:

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

- `dotnet test Grid7736.slnx --no-restore --verbosity minimal -p:NuGetAudit=false`: 140 passed, zero failed/skipped (Core 8, Simulation 93, Terminal 39). This adds 8 event-buffer cases and 15 terminal/live-view cases to the previous 117 tests.
- Tests cover bounded retention/eviction, sequence ordering, coherent concurrent snapshots, actual transfers/edits, no-op/denial/cancellation behavior, idle truthfulness, rendering bounds, text sanitization, paging, changed snapshots, resize fallback, Q/Esc, return to commands, cancellation and scheduler-failure cleanup.
- `dotnet build Grid7736.slnx --no-restore --verbosity minimal`: zero warnings/errors.
- Interactive console smoke checks verified live running ticks, return to commands, paused display and normal exit. TERM=dumb smoke verification produced a single fallback snapshot and accepted later commands. Automated injected-console tests cover resize and cancellation paths; this does not claim exhaustive validation of every terminal emulator.

See [Master Development Checklist](Project_GRID_Master_Development_Checklist.md) and [Current Implementation Status](Project_GRID_Current_Implementation_Status.md) for broader scope and historical verification.
