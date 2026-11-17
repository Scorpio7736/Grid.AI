# Project GRID — Bulk task operations and agent filters (5.8)

**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


## 4.2G completed — 2026-10-23

`grid -start` / `grid -pause` replace the old start/pause commands, which now give migration guidance without changing state. `tasks` and `tasks list` show all authorized retained history, including labeled archives. `tasks -completed` shows Completed only; `tasks -list` excludes Completed and Denied (cancelled/failed and archived records remain visible). All views accept one agent filter. Master Control can use `tasks deny <reference> "reason"` on an AwaitingApproval task; denial is terminal, separately audited and saved in schema v21. Existing v20 history upgrades without rewriting cancellations as denials.

Transfers accept drive roots as containers: `grid -copyto "E:\"` targets `E:\GRID.AI`. `grid -copyto -select` / `grid -transferto -select` select a container and create its new GRID.AI child. Existing targets are refused; no merging or overwriting. Original-host Master Control, interactive preview/y/n, saved-world verification and move cleanup safeguards remain. No files move when selection is cancelled, authority changes or media is unavailable.

**Verification:** 658 tests passed (14 Core, 157 Simulation, 98 Persistence, 389 Terminal), including ten new regression cases; zero skipped. Native dialog appearance and physical flash-drive removal/cross-PC acceptance remain manual release checks. The picker uses Windows' built-in local folder dialog, with no new external package/service. The saved city and user files were not relocated by verification. Agent lifecycle coupling (5.9), startup test gate (8.8), changed-device handling (8.9), meetings (6.2A), and full portable/disaster acceptance (8.6) remain planned. See [implementation and test commands](Project_GRID_Terminal_Controls_Implementation.md).


**Implemented and verified — 2026-10-22.** Human-controlled bulk operations use the existing task broker. No automatic approval or new agent privileges are introduced. Schema v20 adds a retained archive flag to task records in WorldTasks JSON; old records default to unarchived.

## Command contract

Enter these commands inside GRID. Substitute a registered agent role such as MCP, MAXIS, SENTINEL, JN, VOLT or RELAY for AGENT. Agent switches are case-insensitive and resolve to stable registered IDs. Unknown/uninitialized agents and multiple/conflicting filters are rejected.

| Command | Behavior |
| --- | --- |
| `tasks list` | List visible, unarchived tasks. |
| `tasks list -MAXIS` | List visible, unarchived MAXIS tasks. |
| `tasks list -archived [-AGENT]` | Read retained archived tasks, optionally filtered. |
| `tasks approve -all [-AGENT]` | Preview all selected AwaitingApproval tasks, then ask y/n. |
| `tasks run -all [-AGENT]` | Sequentially dispatch selected approved Queued tasks. Does not approve anything. |
| `tasks clear -all [-AGENT]` | Preview and confirm archival of all selected unarchived tasks; pending work is cancelled. |

Bulk approval, clearing and dispatch require Master Control in the original host. Attached terminals can still list/filter and use existing authorized single-task operations; they direct bulk mutations/dispatch to the original host. Conversation windows do not gain task commands. Filters never expand an agent's existing visibility.

## Review and dispatch

Selections are snapshots taken when the command is entered. New tasks arriving later are excluded. Approval and clear previews contain the selected count, agent scope and each full task inspection, including exact arguments, registered workspaces and relevant side-effect notices. Approval is not execution. Only interactive y/n confirmation permits mutation; blank/n/EOF cancels and redirected/unattended confirmation is refused.

Confirmation revalidates the captured session generation and every selected record. Any changed record, invalid state, revoked workspace or exhausted audit capacity rejects the entire mutation before any selected record changes. Each approved/archived record gets its own audit entry. A confirmation and a run operation can each be consumed only once. If saving fails after a mutation, in-memory changes may remain; the error says so. Resolve persistence before continuing. The existing durable start barrier still prevents adapter invocation unless its running attempt is saved.

`tasks run -all` dispatches the captured Queued records sequentially, using the existing per-task authorization, cancellation, timeout and persistence checks. A changed selected record is rejected rather than executing a newly retried/reapproved version under the old batch. The existing broker guard prevents simultaneous execution. Another dispatcher may change a later task; the batch then stops safely at that task.

The first failed, timed-out, cancelled, interrupted or unconfirmed attempt stops the batch. Remaining records are reported Skipped and remain queued if nothing else changed them. Each row identifies its task and outcome; totals count selected, completed, unsuccessful/unconfirmed and skipped tasks. `tasks inspect ID` supplies full untruncated details. Ctrl+C uses existing host shutdown cancellation; an attached authorized terminal can cancel a running task individually. There is no persistent batch job, automatic continuation or auto-retry. Restart retains individual outcomes and invalidates queued/running approvals according to existing broker recovery.

## Clear means retained archive

Clear removes records from the operational list without deleting task history. AwaitingApproval/Queued tasks become Cancelled before archival; completed/failed/cancelled/timed-out/interrupted records retain their results. The archived flag, archive audit, request keys, plan references, notification links and execution artifacts remain. `tasks list -archived` and `tasks inspect ID` expose this retained history within normal role scope. MCP plans and RELAY briefings continue to reference valid records.

A clear selection containing Running work is refused. Service confirmation also refuses clearing while any broker attempt is still settling, even if its task has already been marked Cancelled. Cancel it, wait for the broker to return, then request a fresh clear preview. Archived tasks cannot be approved, cancelled, retried or dispatched. Duplicate request keys remain reserved; clearing does not authorize replay.

**Archive does not free the existing 256-record task limit or enlarge the 64-plan/history limits.** No history is pruned and no user files, artifacts, conversations or world saves are deleted. Archive compaction/export and restoration to the operational queue remain future work. Audit-capacity exhaustion refuses mutation rather than deleting evidence.

## Verification

**603 tests passed:** 14 Core, 157 Simulation, 97 Persistence, 335 Terminal, zero failed/skipped. Twenty-one new TaskBatchTests cases cover frozen selections, case-insensitive agent filters, invalid/conflicting flags, approval/clear confirmation, noninteractive refusal, stale sessions, unrelated-agent visibility, original-host restrictions, revoked scopes, atomic changed-preview rejection, duplicate dispatch, running/unsettled clear refusal, timeout/stop/skipped outcomes, cancellation before dispatch, save failure, SQLite archive/plan/RELAY retention and v19 migration. Existing broker tests cover active cancellation, concurrent dispatch, restart recovery and persistence barriers.

Run `dotnet test <project>/<project>.csproj -c Verification --no-restore --verbosity minimal` serially for Grid.Core.Tests, Grid.Simulation.Tests, Grid.Persistence.Tests and Grid.Terminal.Tests. For focused checks, add `--filter FullyQualifiedName~TaskBatchTests` to the Terminal command. Tests use disposable databases and approved demonstration operations; the user's task queue is not altered. Release verification: `dotnet build Grid.Terminal/Grid.Terminal.csproj -c Release --no-restore --verbosity minimal` succeeded with zero warnings/errors.

## Safe terminal acceptance test

As Master Control, start with `tasks list -MAXIS` and check existing records. The `-all -MAXIS` operations include all eligible MAXIS tasks, so use a disposable world for a fully isolated test, or review the preview carefully before confirming.

```text
agents initialize
tasks propose MAXIS maxis.demo "Batch test one"
tasks propose MAXIS maxis.demo "Batch test two"
tasks list -MAXIS
tasks approve -all -MAXIS
```

Review the complete preview. Enter `y` only for the intended tasks. Then:

```text
tasks run -all -MAXIS
tasks list -MAXIS
tasks clear -all -MAXIS
```

Review the clear preview and enter `y` to archive those selected records. Finally:

```text
tasks list -MAXIS
tasks list -archived -MAXIS
```

The demonstrations perform no host action. With just those two tasks, expect two Completed outcomes, then an empty operational MAXIS list and two retained archived records. Use their displayed references with `tasks inspect` to see results and archive audits. Other agents' records remain untouched by the MAXIS filter.
