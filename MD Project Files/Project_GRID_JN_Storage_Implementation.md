# Project Grid.AI — 5.5 JN Storage and Files

## Implemented — 2026-10-21

JN inventories explicitly registered files/directories, finds possible duplicates with bounded hashing, and prepares non-destructive cleanup reports. It measures defined GRID-managed storage against an initial **20,000,000,000-byte logical budget (20 decimal GB)**. Expansion requests are recorded as MCP escalations and require a separate human Master Control approval before changing the budget. No file deletion capability is enabled.

This milestone is manual, approval-driven measurement and governance. Continuous sampling, growth forecasts, disk-reserve enforcement, write throttling, automatic cleanup, and a full disk-wide storage controller remain planned. The logical budget does not reserve space, repartition disks, or enforce a hard write cap.

## Registration and scope

```text
jn
    - jn scopes
      # USAGE: List registered storage paths visible to Master Control, MCP or JN.
    - jn register <alias> "absolute-file-or-directory"
      # USAGE: Master Control registers an existing local file or directory for read-only JN tasks.
    - jn revoke <alias>
      # USAGE: Block further execution and cancel ongoing work for the scope.
    - jn quota
      # USAGE: Show the logical budget and MCP quota proposals; this is not a fresh disk measurement.
    - jn quota inspect <proposal-reference>
      # USAGE: Review amount, observation, submitting operator, MCP identity and source task.
    - jn quota approve <proposal-reference>
      # USAGE: Master Control separately approves a completed pending expansion proposal.
    # EXAMPLE: jn quota
```

Up to 32 immutable aliases are supported, including revoked aliases. Aliases cannot be rebound; register a new one after a move or machine change. Registry records the absolute path, machine name and enabled flag. Drive roots, UNC paths, reparse/symbolic-link paths and overlap with JN's report directory are refused. Directory enumeration does not follow links; unreadable, changed, missing or limited coverage is reported as partial. Registered personal folders are not attributed to the Grid.AI logical budget.

Registration/revocation and quota approval require Master Control. MCP and JN may inspect governance state; other agents cannot. JN proposes only its own capabilities; MCP may delegate them. Every execution still requires individual task approval and the original host's `tasks run`. Read-only means source files are not modified; reports are written to Grid.AI's artifact directory.

## Task capabilities

```text
tasks
    - tasks propose JN jn.inventory <scope-alias> [key]
      # USAGE: Save path, size and last-write metadata for the approved scope.
    - tasks propose JN jn.duplicates <scope-alias> [key]
      # USAGE: Compare same-sized files by SHA-256 within explicit limits.
    - tasks propose JN jn.cleanup <scope-alias> [key]
      # USAGE: Produce duplicate-group review candidates; never delete or select a survivor.
    - tasks propose JN jn.storage status [key]
      # USAGE: Measure defined GRID-managed database, model, asset and report storage.
    - tasks propose JN jn.quota <whole-decimal-GB> [key]
      # USAGE: Measure managed storage and escalate a larger-budget proposal to MCP without applying it.
    # EXAMPLE: tasks propose JN jn.storage status
```

MCP accepts the same actions, e.g. `mcp request "JN: inventory jntest; JN: storage status"`. Arguments cannot be arbitrary paths/scripts; inventory, duplicates and cleanup require a registered alias. Inspect the exact task, approve it, then dispatch it. Sensitive deletion is rejected, even with a task approval. Adding deletion later requires its own exact-file review and human approval policy.

## Inventory, duplicates and reports

Each attempt examines at most 2,000 filesystem entries and 32 directory levels. Reports include roots, file paths, byte lengths, UTC write timestamps, coverage status and up to 32 coverage issue descriptions. Duplicate candidates must have matching lengths and SHA-256 content hashes. Hashing is limited to files of at most 16 MiB and 128 MiB of total candidate data per attempt. Metadata is checked before/after hashing and access/change failures are skipped explicitly. Limits or skipped candidates make the report partial. Runtime checks are not an OS sandbox against hostile concurrent filesystem manipulation.

Equal content is only a possible duplicate: files may serve distinct purposes or share physical storage through hard links. Cleanup reports therefore make no safe-deletion verdict, guaranteed reclaim estimate or preferred-survivor selection. Source files remain unchanged.

Reports are JSON under `<database-path>.jn-artifacts/<task-id-without-hyphens>-<attempt>/report.json`. They contain full local paths and should be handled as local private data. Task summaries identify real storage observations and link to reports. Attempts have a 120-second deadline, use the durable-start barrier, observe cancellation/revocation, and never overwrite previous attempt reports. A cancelled attempt may leave a partial report; its task status determines whether the attempt completed.

Grid.AI copy/move includes the selected world's JN artifact directory. SQLite backups contain registrations, quota history and task summaries, not external JSON report contents; back up the artifact directory as well. Absolute historical report paths are not rewritten after transfer—find the same task/attempt under the destination's companion directory. NGE keeps its existing database/backup deletion scope; JN reports remain for separate manual cleanup. No automatic report pruning is implemented.

## Managed storage accounting

The measurement roots are the selected database, its .backup, -wal, -shm and -journal files, its MAXIS and JN companion artifact directories, and models/assets directories under the portable package root or installed application directory. The report lists exact roots. Optional missing folders/files count as absent; unreadable paths or a missing primary database make coverage partial. Unregistered models elsewhere, external SDKs, Windows caches, source repositories, other world saves and unrelated personal files are outside this measurement.

File lengths are logical bytes, not physical allocated blocks. Shared files/hard links can be counted more than once; a shared installation's models/assets are attributed to each measured world's budget, not aggregated across worlds. Measurement is an on-demand, non-atomic sample; concurrent database saves and other writes can change sizes. The report being created and subsequent database saves are not included in the preceding measurement. Partial totals are lower bounds; JN does not assert below-quota status from incomplete coverage. At/above-quota measurements produce a warning.

## Expansion workflow

```text
mcp request "JN: quota 25" jn-quota-test
```

1. Inspect/approve the resulting JN task and run it.
2. JN samples managed storage and creates a proposal bound to that task and the saved MCP identity. The budget remains unchanged.
3. Use `jn quota` and `jn quota inspect <proposal-reference>` to review the historical measurement, coverage and requested amount. The task report provides evidence.
4. If desired, human Master Control runs `jn quota approve <proposal-reference>`. MCP/JN cannot approve it themselves.
5. Grid.AI records the approving operator/time and saves the new logical budget. It does not allocate disk space or delete anything.

Requests must expand the current budget, use whole decimal GB, and lie between 21 and 1,000 GB. Up to 32 proposals are retained. Only proposals from completed tasks can be approved; reused, stale/non-expanding and already-approved requests are refused. Retrying the same task does not create a second proposal. There is no quota reduction, proposal deletion or forecast-based automatic expansion. Disk free-space/reserve validation and proactive notifications described in the long-term architecture remain future work; users must assess physical capacity separately before adding data.

Schema v18 persists scopes, quota and immutable proposal/approval history in WorldTasks. Validation binds proposals to their original task amount/operator and MCP role, and checks the current budget against approved history. v17 worlds migrate with 20 GB and no scopes/proposals. Restart never replays tasks, restores queued work to awaiting approval and retains completed governance history. Save failures are surfaced; in-memory changes may remain until storage is repaired.

## Terminal acceptance test

The repository includes exactly three safe sample files in TestSupport/JN-SampleFiles: alpha.txt and beta.txt are identical; unique.txt differs. These are suitable for a repeatable non-destructive test.

```text
agents initialize
jn register jntest "C:\Users\jack\source\repos\Grid7736\TestSupport\JN-SampleFiles"
jn quota
mcp request "JN: inventory jntest; JN: duplicates jntest; JN: cleanup jntest; JN: storage status" jn-test-1
tasks list
```

For each of the four new task references, replace TASK with the displayed short reference:

```text
tasks inspect TASK
tasks approve TASK
tasks run
```

Run from the original host; older queued tasks may be dispatched first. Finally run `mcp brief`. Expect three inventoried sample files, one duplicate group, a proposal-only cleanup result, and a managed-storage measurement against the current logical budget. The sample files must remain unchanged. The optional 25 GB workflow above tests separate approval; stopping before `jn quota approve` keeps the budget unchanged. Reuse a registered alias rather than registering it again; use a new request key for a fresh run.

## Completion verification

**535 tests passed:** Core 14, Simulation 157, Persistence 97, Terminal 267. Sixteen new JN cases cover metadata inventory, single-file scope, duplicate/cleanup source preservation, invalid/destructive requests, managed byte accounting, role restrictions, MCP escalation, separate quota approval, revocation, persistence fencing, cancellation, link/entry/hash limits, SQLite recovery, forged history rejection, v17 migration and short-reference command usage. One transfer test verifies JN report inclusion. Release build is checked separately before delivery.

Next: 5.6 VOLT and RELAY. Advanced storage-controller behavior remains explicitly planned above.
