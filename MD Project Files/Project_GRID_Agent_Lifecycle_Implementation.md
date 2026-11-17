# Grid.AI agent lifecycle — 5.9

## Agent lifecycle — 5.9 completed (2026-10-23)

`grid -start` initializes the six named agents on first use and resumes their activity thereafter. `grid -pause` blocks new agent replies, MCP coordination and task dispatch while retaining identities, memory, conversations and task history. `agents initialize` now gives migration guidance; connecting to MCP no longer creates it implicitly. `agents` reports whether activity is active or paused. Ordinary startup/reload remains paused; the explicit `--start` launch option takes the same initialization/save path after authenticated owner login.

In-flight dialogue is cancelled and cannot commit across a pause/resume generation. In-flight task attempts are cancelled cooperatively and recorded as Interrupted when the lifecycle changed; partial external effects may remain, so retry requires inspection and fresh approval. Queued tasks are not automatically dispatched on resume; pause retains existing queued approvals, while restart invalidates them through the existing recovery policy. Authenticated attached Master Control terminals can start/pause the shared host, including while its original terminal is busy. Transfer/NGE ownership restrictions remain unchanged.

**Verification:** 672 tests passed (14 Core, 157 Simulation, 99 Persistence, 402 Terminal), including eleven new lifecycle/window/startup cases. Existing historical fixtures now explicitly start their worlds before agent work. Schema remains v21. See [lifecycle contract and acceptance commands](Project_GRID_Agent_Lifecycle_Implementation.md). Earlier descriptions of independent initialization/lazy MCP or planned 5.9 coupling are historical and superseded.

## Runtime contract

- Master Control starts/resumes through `grid -start`. The engine authorizes this operation and preserves existing agent IDs instead of recreating them. The terminal checkpoints initialization; a failed checkpoint pauses the clock. Agent-role sessions cannot start GRID.
- Reading status, agent lists, task history, transcripts and administrative review remains available while paused. Human task preparation/approval/cancellation remains explicit; agent-role proposals, coordination, replies and actual dispatch require a running GRID. Starting is never task execution approval.
- Pause advances the activity generation. Dialogue carries that generation through inference and final commit, so even an immediate resume cannot accept a pre-pause reply. The task broker checks activity before claiming work, after the durable-start save, while waiting and before recording the final outcome.
- Cancellation is cooperative, not an operating-system freeze or rollback. Running helpers are asked to stop; already-performed filesystem/build/speech effects cannot be undone. While a task settles, another dispatch is refused. Interrupted tasks require retry and a new approval; queued work still requires an explicit run command. A completed result already committed before pause remains completed.
- Scheduler shutdown pauses the clock and changes the generation; attached requests are revoked/drained by the existing host shutdown flow. World reload keeps stable identities/history but starts paused and does not replay tasks or catch up missed time.
- The shared local model server can remain loaded/idle while paused; it is infrastructure, not an independent agent. Explicit `ai status/test` diagnostics remain available to Master Control. Dialogue cannot run until Grid.AI starts. A manual `step` remains an explicit simulation tick for civic behavior testing; it does not enable AI, task dispatch or autonomous scheduling.
- Low-level engine `Start` remains a trusted simulation-clock API for fixtures; production startup and terminal commands use the authenticated `StartAgents` path. Communication storage/read APIs do not themselves execute agents. Normal connect/resume is activity-gated and does not create residents.

## Verification

Nine new direct lifecycle tests cover first start/idempotence, no lazy creation, paused activity/read-only access, fast pause/resume inference cancellation, stale reply rejection, active task interruption/fresh approval, pause during the durable-start fence, SQLite reload/identity preservation, agent-role denial, and scheduler shutdown. One shared-window test verifies attached Master Control start/pause while retaining transfer restrictions. One real terminal subprocess test verifies authenticated `--start` initializes six agents, reports active then paused, and saves without creating tasks. Existing save/reopen subprocess coverage also verifies six identities survive normal `grid -start`.

All 672 tests passed; no user world was opened or relocated by these tests. Platform release tests and long-duration task/helper cancellation under every possible external failure remain broader Phase 8 work.

## Terminal test

After restarting the updated terminal and logging in:

```text
agents
connect MCP -inline
grid -start
agents
connect MCP -inline
talk Hello MCP.
grid -pause
talk Are you there?
transcript
grid -start
talk We are back.
disconnect
```

Before starting, connect should report that agents are paused. Starting registers six agents (or retains their saved IDs). The paused talk should be refused without adding a turn; transcript remains readable. After resuming, the same conversation works again.

To check task gating:

```text
tasks propose MAXIS maxis.demo "Lifecycle test" lifecycle-test-1
tasks
```

Inspect the returned task reference, approve it explicitly, then run `grid -pause` and `tasks run`. Dispatch should be refused. Run `grid -start` and inspect the task: it remains queued until you explicitly run `tasks run`. Use a new request key for a fresh test. No real host task is performed by this demo capability.

## Portable verification

The refreshed self-contained Release package passed Test-GridPortable.ps1 after this change: authenticated in-memory initialization, real bundled Qwen/MCP reply and animation metadata succeeded with the usual global runtime/model paths removed. No saved user world was opened. This verifies local package behavior, not another physical PC or firewall isolation.
