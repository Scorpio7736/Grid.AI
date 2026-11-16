# Project GRID - Move and Copy Implementation

## Duplicate-source cleanup repair — 2026-11-15

The user's flash-drive move reached publication, then `transfer-status.txt` reported cleanup incomplete on `Test-GridPortable.ps1`. Read-only inspection of the receipt confirmed the same repository script supplies both `source/Test-GridPortable.ps1` and root `Test-GridPortable.ps1`, with `DeleteSource=false` on both. Cleanup previously opened each entry with `FileShare.Delete` and held all handles; the second open conflicted with GRID's own first reader. This was not evidence that another application held the file. The failure occurred in the acquire/verify phase before source deletion.

Cleanup now canonicalizes source paths and retains one protected reader/hash per unique source while independently opening and hashing every destination entry. Each entry's expected hash must match; external locks, changed copies and unsafe boundaries still refuse cleanup before deletion. Unique movable sources are deleted once, and retained repository/runtime sources stay retained. No sharing restrictions or hash checks were relaxed.

Four regression cases cover duplicate sources with removal and retention, corruption of the second destination copy, and a genuine external exclusive lock. Focused transfer tests: **39 passed**. Full solution: **1,440 passed, zero failed, four symbolic-link privilege skips** using `dotnet test Grid7736.slnx --verbosity quiet -p:NuGetAudit=false`. `git diff --check` passed. These development results do not satisfy the zero-skip `main` promotion gate.

The existing flash-drive package was inspected only and still contains the old cleanup executable. It was not repaired, deleted or marked complete. Do not blindly rerun its old helper or clear its status: rebuilt/edited source files can invalidate its original receipt. A fresh transfer with the updated executable needs a new empty destination and enough space; preserve the failed package until its world and recovery disposition have been reviewed. Physical flash-drive cleanup with the fixed executable remains unverified. Automatic recovery of an existing failed receipt is still not implemented.

**2026-10-30 transfer/AI follow-up:** Removed repeated full-content transfer checks while retaining independent destination verification and pinned model/manifest matching. Hashing now reports byte progress; ETA uses measured copy/verification work. AI diagnostics retain startup errors, and bundled startup tolerates slow media. Original-PC AI passed an isolated test; the reported failing instance remains undiagnosed pending its exact error. [Implementation, evidence and limits](Project_GRID_Transfer_Checks_and_AI_Diagnostics.md). Physical second-PC acceptance and 6.15 remain pending.

**Transfer progress follow-up — 2026-10-29:** Copy/move previews show an approximate duration range before confirmation. A separate Windows dashboard shows progress, elapsed/remaining time and a bounded live activity feed; validation and move cleanup cannot prematurely show completion. Closing the viewer leaves transfer running; text fallback remains available. See [progress window and verification](Project_GRID_Transfer_Progress_Window.md).

**6.14 repair — 2026-10-29:** Development-build transfers now include bundled runtime/models/.NET and available source; launch paths are destination-relative, assets/hash/budget are verified before publication, and device/location changes require grant/approval review. A synthetic-world second-PC package and [acceptance instructions](Project_GRID_Portable_Transfer_Repair_and_Second_PC_Test.md) are ready. Local verification passed 1,049 regression tests, 14 live profile checks and six MCP workflow checks. Second-PC offline/drive-letter/removable-media acceptance remains pending; PDF extraction stays in 6.15. Phase 7 remains blocked.

**6.13 completed — 2026-10-29:** Six explicit agent profiles share one pinned Qwen3 4B Q4_K_M snapshot. CPU-only/reduced-memory operation, role routing and the 20,000,000,000-byte base ceiling are implemented. `ai profiles` and `ai storage` expose configuration and base/data/staging/headroom. **14 live role checks passed**; physical low-RAM/second-device certification remains separate. See [profiles, measured evidence and limits](Project_GRID_Agent_Model_Profiles_and_Base_Budget.md). Earlier planned/fixed-model descriptions are historical; 6.14–6.15 still block Phase 7.

**Reported blocker / planned repair — 2026-10-27:** The user reports AI failure on another device after copying GRID. 6.14 requires manifest-complete actual copyto/transferto outputs, destination-relative runtime/model paths, CPU fallback and offline second-device acceptance before Phase 7. Existing local verification is insufficient. [Repair/acceptance plan](Project_GRID_Pre_Phase_7_Readiness_Plan.md).

**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


### 4.2G folder-picker repair — 2026-10-23

Fixed a user-reported Windows dialog failure: FolderBrowserDialog supports Description, not Title. Removed the unsupported property, explicitly emit JSON null on cancellation (Windows PowerShell otherwise emits no JSON for null), suppress first-use progress and keep raw helper stderr/CLIXML out of terminal errors. **392 terminal tests passed**, including three new tests executing the actual Windows PowerShell helper with a real Windows Forms dialog object. Tests substitute only the modal ShowDialog result for selection/cancellation and verify concise failure output. Physical dialog interaction remains a manual check. Earlier injected-picker tests did not cover this platform failure. Restart GRID to load the corrected binary. Schema remains v21.


## 4.2G completed — 2026-10-23

`grid -start` / `grid -pause` replace the old start/pause commands, which now give migration guidance without changing state. `tasks` and `tasks list` show all authorized retained history, including labeled archives. `tasks -completed` shows Completed only; `tasks -list` excludes Completed and Denied (cancelled/failed and archived records remain visible). All views accept one agent filter. Master Control can use `tasks deny <reference> "reason"` on an AwaitingApproval task; denial is terminal, separately audited and saved in schema v21. Existing v20 history upgrades without rewriting cancellations as denials.

Transfers accept drive roots as containers: `grid -copyto "E:\"` targets `E:\GRID7736`. `grid -copyto -select` / `grid -transferto -select` select a container and create its new GRID7736 child. Existing targets are refused; no merging or overwriting. Original-host Master Control, interactive preview/y/n, saved-world verification and move cleanup safeguards remain. No files move when selection is cancelled, authority changes or media is unavailable.

**Verification:** 658 tests passed (14 Core, 157 Simulation, 98 Persistence, 389 Terminal), including ten new regression cases; zero skipped. Native dialog appearance and physical flash-drive removal/cross-PC acceptance remain manual release checks. The picker uses Windows' built-in local folder dialog, with no new external package/service. The saved city and user files were not relocated by verification. Agent lifecycle coupling (5.9), startup test gate (8.8), changed-device handling (8.9), meetings (6.2A), and full portable/disaster acceptance (8.6) remain planned. See [implementation and test commands](Project_GRID_Terminal_Controls_Implementation.md).


## Command rename — 4.2F completed (2026-10-22)

The supported commands are now `grid -copyto "location"` and `grid -transferto "location"`. The old `copexist` and `tranexist` names only display migration guidance; they never initiate a transfer. Operation switches are case-insensitive. Supply exactly one operation and one destination; missing, unknown, repeated/combined switches, empty destinations and extra arguments are rejected. Paths with spaces must be quoted. These are commands inside the `grid>` terminal, not new Windows launch switches.

Master Control and original-host restrictions are retained. Preview, interactive y/n, final authorization checks, worker/attachment shutdown, final save, verified staging and deferred move cleanup use the existing transfer implementation. No schema or transfer-inventory change was made. Existing safety and packaging limits below still apply.

**Verification:** 582 tests passed (14 Core, 157 Simulation, 97 Persistence, 314 Terminal), zero failures/skips. Ten new cases cover invalid syntax, case handling, completion/help and non-executing legacy guidance. Existing host confirmation/cancellation/copy tests now use the new syntax; attached-window tests reject both operations. Real move-helper tests use disposable fixtures only. No user installation or world was relocated. Earlier 4.2D test counts below are historical.

To check the rename without transferring files, run `help`, type `grid -c` then Tab, or invoke `copexist "E:\GRID Test"` to see migration guidance. An actual transfer uses the examples below and requires confirmation; answer `n` to leave files in place. Use a new absolute destination whose parent exists.


## 4.2D delivery - 2026-10-20

Master Control (Master Admin) can run `grid -transferto "location"` to move GRID or `grid -copyto "location"` to copy GRID. The destination must be a new absolute directory whose parent already exists. Agent and logged-out sessions are denied. Commands preview the source, selected world, destination, file count and size, then require interactive y/n confirmation. Both commands close the terminal after stopping workers and saving the latest state. Redirected confirmation is refused.

Examples:

```text
grid -copyto "E:\GRID7736 Copy"
grid -transferto "E:\GRID7736"
```

### Actual file scope

The transfer inventories every file under the running application directory, including bundled dependencies and Markdown documentation, plus the selected world's database, backup, historical archives and matching sidecars. Live `.lock` files are excluded. An existing transferred package also includes its managed folders such as models, configuration, assets, logs and data. Destination files are organized under `app/` and `data/`, with `launch.cmd`, `grid-package.json`, `transfer-manifest.json` and `transfer-status.txt` at the root.

The source Git repository, unrelated host applications/user documents, other installed worlds, and shared user PATH launcher are not part of the movable installation inventory. Runtime/session handles are not transferred. The shared managed launcher is retargeted after a move only when it and the user Grid7736 variable point at the moved installation; unrelated registrations are preserved. Empty source directories, lease files and old generated package metadata can remain. Completion describes removal of the verified source file inventory, not recursive deletion of the entire source directory.

### Verified transfer and move handoff

One host retains its world lease while workers stop, final save completes and files are copied. The service rechecks Master Control before copying and publication, refuses changed file inventories, overlapping/existing destinations, missing parents, junctions/symlinks and insufficient free space. It stages the copy, verifies SHA-256 hashes against the source, loads the destination database through WorldStore, and then publishes the destination directory. Failure retains the source; failed staging directories remain visibly incomplete for diagnosis.

`grid -copyto` retains the original. Its destination is an independent copy of the same saved world, with a separate physical database and lease; it is not another terminal attached to the original running simulation. Login is required. Host observations refresh on startup and the saved city is preserved.

`grid -transferto` starts a hidden local cleanup helper from the verified destination. It waits for the original process to exit, reacquires the source world lease, and locks/verifies every inventory file and corresponding copy before deleting any source inventory file. Active sources, altered data, or locked files cause refusal. The helper writes success/failure to `transfer-status.txt`; the parent reports cleanup as pending, never as already completed. Starting the destination is blocked while cleanup is pending. A later deletion/launcher-update error can leave some source files behind, with the verified destination retained and an incomplete status. A move is not an atomic cross-volume operation. Do not remove the destination during cleanup.

A helper timeout or failed launch leaves an incomplete status and source files; a crash before updating status may leave a pending marker. Inspect the manifest/status and verify the destination before manual recovery. Automatic resume/rollback of partial source deletion is not implemented.

### Starting the transferred GRID

Use the destination's `launch.cmd`. Package startup selects `data/world.db` and skips automatic host PATH/registry installation. Existing Windows-owner worlds still require their owner account; configure portable password credentials using `credentials` before moving between PCs. Password hashes and saved identities/transcripts travel with the database; authenticated sessions do not.

The current launcher/helper uses `dotnet`, so a compatible .NET 10 runtime must already be installed. This feature does not provision runtime/models or fulfill the full PC GRID/Flash GRID installer and offline release gates. Package transfer requires its standard `data/world.db`; transferring an arbitrary alternate world from an existing package is refused.

### Verification and remaining integration

377 tests passed: 14 Core, 155 Simulation, 91 Persistence and 117 Terminal. Nine new transfer tests cover quoted paths/Master Control denial, cancellation, redirected refusal, destination validation, revocation and changed inventory, copy integrity and world loading, latest-state final save, changed-copy/active-source cleanup refusal, exact source inventory cleanup, and the actual cleanup subprocess using disposable files. Build: zero warnings/errors. No user installation or world was moved during testing.

4.2C now disconnects/drains attachments before transfer; commands remain on the original host. Future work remains for 8.6 full PC/Flash edition packaging and migration, exhaustive low-space/power-loss/cross-machine tests, and automatic interrupted-move recovery. The existing current-host transfer path is implemented; those broader gates remain unchecked.
