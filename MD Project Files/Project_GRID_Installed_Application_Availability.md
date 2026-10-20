# Project GRID - Installed application availability

## Current contract - 2026-10-19

The user's revised GRID-REQ-005 intent is to represent programs on the PC, including installed programs that are not open. This supersedes the original 2.7 windowed-only/disappear-on-exit population policy. Historical completion records remain evidence for their original scope.

| Display state | Evidence |
| --- | --- |
| Sleeping | Known executable absent from a complete process-name inventory, or a complete executable observation |
| On call | Observed process group without an application window |
| Running | Observed process group with at least one main window (not necessarily keyboard focus) |
| Observation unknown | Unmapped inventory entry, inaccessible/partial evidence that cannot establish availability, or stale observation |

## Implemented scope

Windows inventory reads HKCU/HKLM uninstall registrations in both registry views, excluding entries explicitly marked SystemComponent. Existing executable DisplayIcon targets supply candidate executable mappings; other entries remain unknown. Inventory refreshes every five minutes. Accessible processes in the current interactive session are sampled every two seconds, including background-only executables. GRID excludes its own process and other sessions/services in session zero. It never launches, stops or changes host programs.

Multiple processes/windows of one executable share a representative; different executables can remain separate even when marketed as one application. Known installed entries and applications encountered during the session remain in an in-memory catalog after exit. A complete process-name inventory can establish absence even if unrelated executable paths are inaccessible. Partial observations never invent a sleeping state. Unknown observations retain unresolved lifetimes for subsequent reconciliation.

Sleep/wake keeps the resident ID; waking creates a fresh runtime incarnation. Running/On call transitions update the display and event history. A disjoint running-process lifetime can still create a replacement resident under the existing incarnation policy. Discovery runs while the clock is paused. Observations expire after ten seconds. Behavior remains observational and produces no simulated work for host applications.

The existing schema v5 remains unchanged: ordinary application records and observations are not saved; installed inventory is rediscovered at startup. Agents and special programs retain their existing persistence rules. City layout, history and clock remain saved and reused. Sleeping does not imply zero measured usage or full energy: unknown requirements/usage stay unknown.

## Coverage limits and follow-up

This is **discoverable installed applications plus current-session processes**, not a claim to enumerate every executable on every disk. Store-only packages without uninstall registrations, never-run portable applications, other users/sessions and inaccessible programs are not fully covered. Icon executables are candidate mappings and may represent launchers rather than every executable in a suite; ambiguous entries remain unknown. Catalog entries without a mapping can coexist with a separately observed executable. Uninstallation is reflected on restart; the session catalog is additive. The existing 512-resident/4096-observed-process guard remains, with active groups prioritized and partial coverage reported. Full-machine inventory, richer package/shortcut mappings, removal reconciliation and capacity expansion remain planned acceptance work for the user's broader “all programs” goal.

## Verification - 2026-10-19

`dotnet test -c Release --no-restore`: **290 passed** (14 Core, 127 Simulation, 79 Persistence, 70 Terminal). Coverage includes sleep/wake identity and incarnation, background/window classification, invalid empty running observations, incomplete discovery, per-executable absence despite unrelated access denial, and prior persistence/behavior regressions.

Windows disposable-world startup/reopen reused city d3074c26-4d5a-41bf-87d3-c82224aab1ba and saved successfully. Final observation: **223 residents**, with **49 Sleeping, 65 On call, 13 Running and 96 unknown**. Counts are machine/time-specific; partial coverage was explicitly reported. Existing user saves and running GRID sessions were not changed.
