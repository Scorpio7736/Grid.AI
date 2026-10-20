# Project GRID - Terminal commands and shutdown

## Closing artwork and default color - 2026-10-19

The farewell now uses the user's supplied large GRID7736 ASCII artwork and blank-line spacing. Successful persistent exits retain `Stopping GRID.`, `World saved.`, then the artwork and `WORLD SAVED // USER DISCONNECTED`. Error and ephemeral messages remain truthful. Consoles narrower than 78 columns use a compact GRID7736 farewell. Ordinary terminal output and input echo default to light blue (bright cyan / ConsoleColor.Cyan); status, warning and error accents remain. The prior shell foreground is restored on exit. `--no-color`, NO_COLOR, TERM=dumb and redirected output continue to disable application color changes. Terminal verification: **75 tests passed**; no claim of visual color matching across user console themes.

## Terminal commands and shutdown complete - 1.12 (2026-10-19)

Implemented grouped `help`, display-only `clear`, `programs -active`, `programs -possible`, `programs -sleeping`, and a GRID7736 ASCII farewell. Active selects fresh Running applications; sleeping selects fresh On call or Sleeping applications; possible includes current applications plus the saved known-application catalog. `ids` works with one filter. Unknown/stale observations are never classified as active or sleeping. Bare `programs` still includes agents and other residents.

**Schema v6** adds a separate ApplicationCatalog table (key, name, source, last catalog observation UTC). The catalog is captured with the simulation snapshot and saved atomically with world/history; backup/recovery and schema validation include it. Installed/observed metadata survives restart; ordinary residents, PID lifetimes, activity and resource readings are not restored. Last observation is catalog provenance, not proof of a launch or current installation. Entries missing from fresh discovery display Historical / unknown. Existing schema-v5-only statements below describe earlier milestones.

The ASCII banner appears after graceful shutdown and final save. Save/shutdown errors retain diagnostics and a nonzero exit code without a success message; ephemeral sessions explicitly say NOT SAVED. There is no keypress wait. Visual Studio can still append its own debug-console footer. Help uses portable ASCII separators and registered command metadata, including extensions; no-color/redirection behavior is preserved.

**Verification:** `dotnet test -c Release --no-restore`: **297 passed** (14 Core, 127 Simulation, 81 Persistence, 75 Terminal). Tests include filters/IDs/invalid flags/empty results, clear preserving the world and tick, successful/failed farewell, catalog save/reopen/recovery and v5-to-v6 migration. A disposable built-terminal run exercised all command groups, filters, redirected clear fallback and successful save/reopen of the same city. An ephemeral PTY run preserved tick1 through clear and displayed the NOT SAVED banner. This tool's PTY uses the clear fallback; physical screen clearing/color appearance and Visual Studio footer presentation were not visually verified in the user's console. No IDE settings or existing user worlds were modified.

## User notes and accepted behavior - 2026-10-19

**Implemented and verified.** Follow-up to completed 1.11, delivered as checklist 1.12. The following requirements are retained with implementation notes. Interpret “cear terminal cmd” as a `clear` command.

## Grouped command reference

`help` should display PROJECT GRID TERMINAL / COMMAND REFERENCE, with the following groups, readable spacing and separators. Implementation uses portable ASCII rules on all consoles; retain narrow-terminal wrapping and no-color/redirected output support. Keep the help generated from registered commands so extensions remain discoverable.

| Group | Commands and descriptions |
| --- | --- |
| Simulation control | `start`: start/resume automatic simulation; `pause`: pause; `step`: one tick while paused; `tick`: alias for step |
| World & population | `status`: world state and clock; `city`: layout/hardware coverage; `districts [ids]`: districts/populations; `programs [ids]`: resident table; `inspect <type> <id\|name>`: program/district details |
| Monitoring & analytics | `live`: live status/events; `behaviors`: needs/goals/work progress; `resources [details]`: hardware measurements; `history [filters]`: saved events |
| System & terminal | `clear`: clear the visible terminal; `help`: this reference; `exit`: graceful shutdown |

A separate COMMAND OPTIONS section should explain `districts ids`, `programs ids`, `inspect program <id|name>`, `inspect district <id|name>`, `resources details` (measurement source, freshness, timestamps and detail), `history details` and the actual supported history filters. Preserve existing options; do not advertise planned flags as working until implemented.

`clear` must only affect display: it must not reset the world, clock, history, catalog or simulation state. Return a usable prompt. Redirected/unsupported terminals should use a plain-text acknowledgement rather than emit clear-screen escape sequences or fail. Clearing scrollback is not guaranteed by every console.

## Program filter contract

Preserve bare `programs` as the complete current resident table and `programs ids` as the identifier view.

| Command | Requested meaning |
| --- | --- |
| `programs -active` | Application residents currently **Running** (windowed); excludes background-only On call, Sleeping and Unknown |
| `programs -possible` | Union of discoverable installed applications and applications previously observed running; includes known installed applications never yet launched, rather than requiring both conditions |
| `programs -sleeping` | Application residents **On call OR Sleeping**, as explicitly requested; retain separate state labels in rows |

These filters apply to host applications. Persistent agents/special programs remain visible in bare `programs`; do not infer their host-process state from simulated work. Unknown/stale entries must not be silently presented as active or sleeping; they may appear in the possible catalog with their actual unknown label. Support `ids` with one filter, reject conflicting/unknown flags with a helpful usage message, and show explicit empty results. Use shared snapshot/formatting rules rather than independently querying processes from a command.

**History requirement:** “have run in the past” should survive GRID restarts. Schema v6 now persists a minimal catalog of stable application identity, display name, provenance and last catalog observation time. Keep it separate from saved ordinary resident state and live PID/incarnation/resource observations. Reconcile saved catalog entries with fresh discovery; an old last-seen timestamp is never evidence that an app is currently running or still installed. Previously seen/uninstalled entries can remain listed as historical/availability unknown. Do not claim a program is launchable merely because it was previously seen. Catalog entries grant no execution authority. Follow the existing persistence migration, validation and recovery rules.

Existing installed-discovery coverage limitations still apply: see [Installed Application Availability](Project_GRID_Installed_Application_Availability.md). The possible list is a known catalog, not proof that every executable on every disk has been found.

## Custom shutdown banner

Print a GRID7736 ASCII banner with a cyan, circuit-like aesthetic after shutdown work completes. Example artwork to refine during implementation:

```text
+============================================================================+
|                                                                            |
|   ______   ______    _____   ______     ______  ______   ______   ______   |
|  / ____/  / __   \  |_   _| |  __  \   |____  ||____  | |____  | / ____/  |
| | |  ___ | |__)  |    | |   | |  \  |       / /     / /      / / | |___    |
| | | |_  ||  _   /     | |   | |   | |      / /     / /    __/ /  |  __ \   |
| | |__|  || | \  \    _| |_  | |__/  |     / /     / /    |__  \  | |__| |  |
|  \______/ |_|  \_\  |_____| |______/     /_/     /_/    |_____/  \____/   |
|                                                                            |
|============================================================================|
|                                                                            |
|                        GRID7736 // DISCONNECTED                            |
|                                                                            |
+============================================================================+
```

Keep monochrome/narrow/redirected variants readable. Print successful-save wording only after a successful save. Ephemeral sessions must say they were not saved; shutdown/save failures must remain visible, retain a nonzero exit code and never be covered by a success banner. Do not add a blocking “press any key” step. Preserve graceful stop, final save and diagnostic behavior on exit/EOF/Ctrl+C.

**Visual Studio boundary:** The quoted executable/process/exit-code and “Press any key to close this window” text is emitted by Visual Studio's debug-console host after GRID exits. GRID can print its own banner before returning, but cannot replace that host-owned footer from its normal application output. Running from an ordinary terminal avoids that Visual Studio footer; debug-console closing behavior is controlled by the user's Visual Studio settings. Do not change IDE settings as part of this requirement. The custom banner is an application farewell, not a promise to suppress the debugger footer.

## Acceptance verification

- Grouped help retains all registered commands, aliases/options and extension discoverability.
- Clear preserves world, time, events and session state; redirected output remains plain.
- Filters cover Running, On call, Sleeping, Unknown/stale, agents, empty tables, IDs and invalid flags.
- Possible-catalog save/reopen preserves historical identity without restoring active state; migration and failure handling are verified.
- Banner output covers successful save, ephemeral exit and failed shutdown; no success claim on failure and no new input wait.
- Smoke-test a normal terminal separately from Visual Studio; document which output each host owns.

**Completion verification:** See the dated evidence above. Existing completed milestones are preserved; remaining discovery-coverage work is unchanged.
