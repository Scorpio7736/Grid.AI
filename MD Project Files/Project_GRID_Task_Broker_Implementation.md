# Project GRID - 5.1 Agent and Task Infrastructure

## VOLT and RELAY — 5.6 completed (2026-10-21)

VOLT now reports existing host telemetry and supports approved normal/eco/auto pacing of GRID's automatic simulation. Unknown/unsupported GPU data is preserved; Windows resources and manual tick behavior are unchanged. RELAY provides a saved local inbox, recorded-task briefings, human acknowledgements and explicitly approved offline speech with delivery/failure receipts. Schema v19 preserves mode and inbox history without automatic playback/replay. **551 tests passed** (14 Core, 157 Simulation, 97 Persistence, 283 Terminal). See [VOLT/RELAY implementation and acceptance test](Project_GRID_VOLT_RELAY_Implementation.md) for commands and boundaries. Next: **5.7 end-to-end verification**. Earlier milestone/schema/test statements below remain historical.


## JN storage — 5.5 completed (2026-10-21)

JN now inventories approved paths, reports possible duplicates, and prepares cleanup proposals without deleting source files. Defined GRID-managed storage is measured against an initial 20 GB decimal logical budget; MCP quota escalations require separate Master Control approval to change it. This is tracking/governance, not a hard write cap or disk reservation. Schema v18 preserves scope and quota history. **535 tests passed** (14 Core, 157 Simulation, 97 Persistence, 267 Terminal). See [JN implementation and acceptance test](Project_GRID_JN_Storage_Implementation.md) for limits, accounting roots, report retention and remaining planned automation. Next: **5.6 VOLT and RELAY**. Earlier milestone/schema/test statements below remain historical.


## SENTINEL security — 5.4 completed (2026-10-21)

SENTINEL now reads local Defender protection/scan timestamps, effective firewall profiles, and bounded Security-log metadata through individually approved tasks. Timestamped results explicitly distinguish real host observations, warnings and unavailable data from simulated incidents. No scans, setting changes, elevation or remediation are enabled. Schema v17 persists these results through the existing broker. **518 tests passed** (14 Core, 157 Simulation, 97 Persistence, 250 Terminal), including 13 new security cases and live read-only provider verification. See [SENTINEL implementation](Project_GRID_SENTINEL_Security_Implementation.md). Next: **5.5 JN — Storage and files**. Earlier milestone/schema/test statements below remain historical.


## MAXIS development — 5.3 completed (2026-10-21)

MAXIS now supports registered-workspace inspection, fixed .NET build/test execution, saved diagnostics and explicit replacement suggestions staged outside original source. Every task requires human approval; build/test executes trusted project code as the current Windows user (no OS/network sandbox). Schema v16 persists immutable, revocable workspace registrations. Other specialists remain demonstrations. **505 tests passed** (14 Core, 157 Simulation, 97 Persistence, 237 Terminal). See [MAXIS implementation](Project_GRID_MAXIS_Development_Implementation.md) for commands, offline prerequisites, artifact retention and limits. Next: **5.4 SENTINEL**. Earlier milestone/schema/test statements below remain historical.


## MCP administration — 5.2 completed (2026-10-21)

Explicit `mcp request` clauses now create atomic saved specialist task plans. MCP reports actual child states/results; each child requires human approval and original-host dispatch. Adapters remain demonstrations. Natural-language planning and real specialist actions remain planned. Schema v15 preserves plan links and existing cities. **485 tests passed**, including 13 new coordination cases. Next: **5.3 MAXIS — Development**. See [MCP coordination implementation](Project_GRID_MCP_Coordination_Implementation.md). Earlier milestone statements and schema/test counts below remain historical.


## Implemented foundation - 2026-10-21

The six named system-agent roles now have an explicit saved registry and a durable, permission-checked task broker. This follows the design's initial safe-mock capability stage. **Every currently enabled adapter is a demonstration: no shell command, process launch, file change, network call, security scan, build/test operation or other real host action is performed.** Existing administrative world operations remain their established separate service paths; new agent task adapters must enter through the broker. MCP natural-language delegation and specialist actions are still 5.2 onward.

## Commands

```text
agents initialize
agents
tasks propose MAXIS maxis.demo "hello GRID" example-request-1
tasks list
tasks inspect <task-id>
tasks approve <task-id>
tasks run
tasks inspect <task-id>
```

`agents initialize` requires authenticated Master Control and a saved world. It creates missing MCP/MAXIS/SENTINEL/JN/VOLT/RELAY Agent residents, reuses unambiguous same-named Agent residents, explicitly registers their session identities, and binds stable IDs to roles. It is idempotent. Colliding civilian/application names are refused rather than promoted. Existing MCP coordinator identity is retained. Merely naming or renaming a program never changes a saved role binding or grants task capability.

`agents` lists role, ID and supported capabilities. Each role has its own `<lowercase-role>.demo` capability (`mcp.demo`, `maxis.demo`, `sentinel.demo`, `jn.demo`, `volt.demo`, `relay.demo`) with one 1..256-character printable argument. MAXIS additionally has `maxis.delay`, taking an integer from 0 through 5000 milliseconds for timeout/cancellation verification. The one-second attempt deadline means longer delays time out intentionally. Results always say DEMONSTRATION ONLY and that no host action occurred. There are no configurable shell/executable/path arguments.

`tasks propose <agent-id|name> <capability> "argument" [request-key]` creates an immutable proposal. The optional key (1..80 printable characters) deduplicates identical same-operator/same-active-agent proposals; reusing it for a different target/capability/argument/actor is rejected. Without a supplied key, GRID generates a new one. Inspect displays the exact target, arguments, status, request key, operator, submitting agent, approver, result and audit trail. Do not put credentials/secrets in arguments: proposals and results are saved.

`tasks approve <id>` requires human Master Control and approves exactly one attempt of that immutable proposal. There is no wildcard/session-wide approval. `tasks run` dispatches one approved task from the original host terminal; it does not start a background worker. Queue order follows approval/update order. Attached terminals may propose, inspect, approve or cancel within their effective role, but cannot dispatch. During a running delay, another authorized attached terminal can cancel it with `tasks cancel <id>`; Ctrl+C in the host cancels on shutdown. The host's command loop waits for that bounded attempt, while simulation/autosave and other terminals continue.

`tasks retry <id>` moves a failed, timed-out or interrupted demonstration task back to AwaitingApproval; fresh human approval is mandatory. At most three attempts are allowed. Completed and cancelled tasks cannot be retried through this command. There are no automatic retries. The current adapters have no external side effects and are replay-safe; future real adapters need their own explicit idempotency and unknown-outcome policy.

## Authority and attribution

Task-service authorization leases verify world instance, session ID/generation, current authentication, policy and registered agent identity. Task leases are separate from program-edit leases; permitting an agent proposal never grants program editing or Master Control. Master Control can inspect/manage all tasks. Agent sessions can see, propose, cancel or request retries only for tasks addressed to their own registered system-agent ID and allowlisted role capability. Agents cannot approve, dispatch, initialize the registry or borrow another role's capability.

Records preserve the authenticated human operator, optional active agent, target agent and approving operator. Audit entries distinguish proposal, approval, broker start, finish, cancellation, retry and restart recovery. Service-level authorization denials also emit `TaskDenied` history records with available verified operator/agent attribution. No autonomous task source is currently accepted or generated. Automatic MCP planning, coordination with autonomous task producers and autonomous-attribution tests remain planned.

Normal `talk` and reviewed `voice send` remain deterministic conversation messages. They do not propose, approve or execute tasks. The task command workflow is deliberately explicit; chat text such as 'MAXIS run my tests' cannot silently become host authority.

## State, durability and recovery

States: AwaitingApproval, Queued, Running, Completed, Failed, Cancelled, TimedOut and Interrupted. The broker permits only one active attempt per engine. Before invoking even a demonstration adapter it claims the task under the engine lock and saves the Running record. Failure of this save prevents invocation. Invocation occurs outside identity/engine locks; authorization is checked again after the save, during waiting and before completion. Cancellation wins over late completion; the adapter is cancelled/drained before the broker accepts another attempt. Completion is saved after the attempt. If a save fails, the terminal reports it and in-memory changes remain; resolve storage before continuing or restarting.

Schema **v14** adds `WorldTasks.StateJson`, transactionally storing role registry, immutable requests, approval/status/results and per-task audit alongside the world. Existing v13 worlds migrate with an empty registry/queue; no city regeneration or automatic resident creation occurs. Registry initialization is explicit. Snapshots validate role/Agent bindings, capability/argument scope, unique request keys, state/attempt/approval consistency and bounded audit/text. Backups include task records.

On restore, Queued tasks return to AwaitingApproval with approval cleared. Running tasks become Interrupted with approval cleared and an explicit unknown-outcome result. They never resume/retry automatically. Completed results remain completed. Recovery adds an audit entry with no human actor rather than fabricating a new user action. The next normal save persists these recovery transitions. This is not a promise of exactly-once external effects after a crash.

Bounds: six system roles, 256 retained tasks, 64 audit entries per task (new approvals/retries stop at 60 to reserve lifecycle/recovery space), three attempts, 256 argument characters and 2048 result characters. At capacity, operations fail without silently deleting audit/history. No archive/prune UI is included yet. A repeated approve/restart cycle can reach the audit bound; expanding retention or task archival is future work.

## Completion verification

**472 tests passed:** 14 Core, 157 Simulation, 97 Persistence, 204 Terminal. Fifteen new `TaskBrokerTests` cover explicit/idempotent registration, exact approval, demonstration labeling, service scope/denial auditing, capability/argument rejection, request deduplication, failed pre-execution save, bounded timeout/reapproval, concurrent dispatch prevention, cancellation, session revocation, host shutdown, SQLite restart/backup recovery, schema-v13 upgrade, corrupted snapshot rejection, detached snapshot collections, terminal dispatch, chat non-dispatch and SQL rollback. Fixtures use disposable worlds; no real user task was executed.

## Planned next

5.2 adds MCP coordination/delegation. 5.3 onward adds reviewed, scoped real specialist adapters. Each real adapter must define allowed parameters, approval scope, cancellation/timeout behavior, process-tree cleanup where relevant, result bounds, idempotency and crash-recovery semantics before it can be enabled. Autonomous task production, background queue scheduling, global workspace policies, task archival and full real-action acceptance remain planned.

## Task and MCP readability follow-up — 2026-10-21

Implemented compact agent/task tables with role names, unique short references, a compact `mcp plans` overview, and separated plan/task details, results, next commands and full audit identities. `mcp brief` still includes all plan details; `mcp inspect <reference>` reads one plan. Full IDs remain in detail views. Displayed short references (at least eight characters) are accepted by task inspect/approve/cancel/retry and MCP inspect/brief; ambiguous prefixes are rejected, and resolution is restricted to the current authorized scope. A copied trailing period or comma is tolerated. Plan/task mixups produce specific guidance. Approval boundaries and persisted data are unchanged. Persistence warnings now appear only on I/O/database errors.

Use `tasks list`, then `tasks inspect <displayed-task-reference>`. Review its argument before using the displayed approval command. Command examples are printed on separate lines without sentence punctuation. Verification: 219 terminal tests passed, including short-reference, punctuation, plan/task mismatch and cross-agent scope checks.
