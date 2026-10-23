# GRID terminal controls, task views and destinations — 4.2G

## Agent lifecycle — 5.9 completed (2026-10-23)

`grid -start` initializes the six named agents on first use and resumes their activity thereafter. `grid -pause` blocks new agent replies, MCP coordination and task dispatch while retaining identities, memory, conversations and task history. `agents initialize` now gives migration guidance; connecting to MCP no longer creates it implicitly. `agents` reports whether activity is active or paused. Ordinary startup/reload remains paused; the explicit `--start` launch option takes the same initialization/save path after authenticated owner login.

In-flight dialogue is cancelled and cannot commit across a pause/resume generation. In-flight task attempts are cancelled cooperatively and recorded as Interrupted when the lifecycle changed; partial external effects may remain, so retry requires inspection and fresh approval. Queued tasks are not automatically dispatched on resume; pause retains existing queued approvals, while restart invalidates them through the existing recovery policy. Authenticated attached Master Control terminals can start/pause the shared host, including while its original terminal is busy. Transfer/NGE ownership restrictions remain unchanged.

**Verification:** 672 tests passed (14 Core, 157 Simulation, 99 Persistence, 402 Terminal), including eleven new lifecycle/window/startup cases. Existing historical fixtures now explicitly start their worlds before agent work. Schema remains v21. See [lifecycle contract and acceptance commands](Project_GRID_Agent_Lifecycle_Implementation.md). Earlier descriptions of independent initialization/lazy MCP or planned 5.9 coupling are historical and superseded.


### 4.2G folder-picker repair — 2026-10-23

Fixed a user-reported Windows dialog failure: FolderBrowserDialog supports Description, not Title. Removed the unsupported property, explicitly emit JSON null on cancellation (Windows PowerShell otherwise emits no JSON for null), suppress first-use progress and keep raw helper stderr/CLIXML out of terminal errors. **392 terminal tests passed**, including three new tests executing the actual Windows PowerShell helper with a real Windows Forms dialog object. Tests substitute only the modal ShowDialog result for selection/cancellation and verify concise failure output. Physical dialog interaction remains a manual check. Earlier injected-picker tests did not cover this platform failure. Restart GRID to load the corrected binary. Schema remains v21.


## 4.2G completed — 2026-10-23

`grid -start` / `grid -pause` replace the old start/pause commands, which now give migration guidance without changing state. `tasks` and `tasks list` show all authorized retained history, including labeled archives. `tasks -completed` shows Completed only; `tasks -list` excludes Completed and Denied (cancelled/failed and archived records remain visible). All views accept one agent filter. Master Control can use `tasks deny <reference> "reason"` on an AwaitingApproval task; denial is terminal, separately audited and saved in schema v21. Existing v20 history upgrades without rewriting cancellations as denials.

Transfers accept drive roots as containers: `grid -copyto "E:\"` targets `E:\GRID7736`. `grid -copyto -select` / `grid -transferto -select` select a container and create its new GRID7736 child. Existing targets are refused; no merging or overwriting. Original-host Master Control, interactive preview/y/n, saved-world verification and move cleanup safeguards remain. No files move when selection is cancelled, authority changes or media is unavailable.

**Verification:** 658 tests passed (14 Core, 157 Simulation, 98 Persistence, 389 Terminal), including ten new regression cases; zero skipped. Native dialog appearance and physical flash-drive removal/cross-PC acceptance remain manual release checks. The picker uses Windows' built-in local folder dialog, with no new external package/service. The saved city and user files were not relocated by verification. Agent lifecycle coupling (5.9), startup test gate (8.8), changed-device handling (8.9), meetings (6.2A), and full portable/disaster acceptance (8.6) remain planned. See [implementation and test commands](Project_GRID_Terminal_Controls_Implementation.md).

## Commands to try

```text
grid -start
status
grid -pause
tasks
tasks -completed
tasks -list -MAXIS
agents initialize
tasks propose MAXIS maxis.demo "Denial test" 42g-denial-test
tasks
```

Copy the returned task reference, then run `tasks deny <reference> "Not needed for this run"`. Check `tasks inspect <reference>` and `tasks -list`; the denied task remains in history but is excluded from that view. Denial does not execute the task and cannot approve/retry it. A new proposal requires a new request key. Only human Master Control may deny; agent-scoped views still cannot expose another role's tasks.

For a harmless folder-picker test, run `grid -copyto -select`, choose a container, review the proposed child path, then answer `n`. Both copy and move require confirmation and close the original host after saving when accepted. A normal non-root explicit path is the exact new destination; only root paths and picker containers gain a GRID7736 child. Select another container or use a new explicit path if that child already exists.

## Persistence and verification boundaries

Schema v21 adds the Denied enum value after existing numeric values, retaining old task identities, status meanings, results and audit. No SQL table shape changes are required; the migration records the newer reader contract. Denied results retain the reason and human audit, with no approval. Queued/running tasks must be cancelled rather than denied. Archive and broker logic cannot dispatch denied records. Existing saves are migrated on normal opening; no production save was opened for these tests.

Ten new automated cases cover task view/archive semantics, denial authority/reason/state restrictions, v20 upgrade and SQLite reload, drive-root resolution, command migration/attached-transfer rejection, picker cancellation and preview, selected copy with unrelated-file preservation and collision rejection, unavailable media, and logout/redirected-input safeguards. Existing transfer tests cover changed inventory/hash, source lease and cleanup refusal. Physical drive failure, power-loss recovery, native dialog interaction and cross-PC deployment are not represented as complete.
