# Project Grid.AI — 5.2 MCP Administration

**6.12 completed — 2026-10-29:** MCP can recommend/invite registered specialists with human confirmation, show participants, accept direct specialist planning requests and display recorded MCP progress/results in the same window. Membership changes start a fresh saved history segment; earlier transcripts and private context are not automatically shared. Decline, removal, scope changes, restart and attached-window workflows are tested. **1,031 automated tests and six bundled-model checks passed.** See [workflow, privacy boundary and verification](Project_GRID_MCP_Specialist_Invitations_Implementation.md). Earlier statements that invitations are planned are historical; 6.13–6.15 still block Phase 7.

**6.11 completed — 2026-10-28:** MCP (Master Control Program) now offers custom catalog-based planning, scoped clarification, visible specialist-selection reasons, approved ordered plan execution, cancellation, reusable plans and recorded-result briefings. Reserved human controls remain explicit. **1,012 automated tests and six bundled-model checks passed.** Meeting shorthand (`meetings ...` and `connect meetings ...`) now validates before opening a window and supports `-inline`. [Workflow, authority matrix and terminal test](Project_GRID_MCP_Digital_Helper_Implementation.md). Earlier 6.11-planned statements below are historical; Phase 7 still requires 6.12–6.15.

**Planned expansion — 2026-10-27:** MCP = Master Control Program, the user-facing digital helper. 6.11 adds clarification, specialist selection/assignment, human approval, verified execution and final briefing for extensible custom tasks; near-Master-Control delegated permissions must use an explicit authority matrix and cannot self-approve or impersonate the human. 6.12 adds in-chat specialist invitations. [Pre-Phase-7 plan](Project_GRID_Pre_Phase_7_Readiness_Plan.md).

**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


## Bulk tasks — 5.8 completed (2026-10-22)

Agent-filtered lists and `tasks approve/run/clear -all [-AGENT]` are implemented. Bulk mutations/dispatch require original-host Master Control; approval and clear require reviewed interactive confirmation. Batches capture fixed selections, revalidate state/authority and stop dispatch on the first unsuccessful attempt. Clear archives records and cancels pending work, retaining audit/results and MCP/RELAY links; active/unsettled work is refused. `tasks list -archived` reads retained history. **Schema v20; 603 tests passed** (14 Core, 157 Simulation, 97 Persistence, 335 Terminal). Archiving does not free the 256-task history capacity. See [commands, verification and limits](Project_GRID_Bulk_Task_Implementation.md). Earlier milestone/schema/count statements below remain historical.


## End-to-end task verification — 5.7 completed (2026-10-21)

MCP delegation, human approval, real MAXIS unit-test execution and RELAY recorded-result delivery are verified together, including actual passing/failing TRX evidence and SQLite reload. Nine new cases also cover all six agent roles, cancellation, durable-start recovery and duplicate dispatch prevention. **560 tests passed** (14 Core, 157 Simulation, 97 Persistence, 292 Terminal), zero failed/skipped. No runtime/schema change was needed; schema remains v19. See [verification matrix, boundaries and terminal test](Project_GRID_End_to_End_Task_Verification.md). Next: **6.1 Shared local AI**. Earlier milestone counts and next-step statements below are historical.


## VOLT and RELAY — 5.6 completed (2026-10-21)

VOLT now reports existing host telemetry and supports approved normal/eco/auto pacing of Grid.AI's automatic simulation. Unknown/unsupported GPU data is preserved; Windows resources and manual tick behavior are unchanged. RELAY provides a saved local inbox, recorded-task briefings, human acknowledgements and explicitly approved offline speech with delivery/failure receipts. Schema v19 preserves mode and inbox history without automatic playback/replay. **551 tests passed** (14 Core, 157 Simulation, 97 Persistence, 283 Terminal). See [VOLT/RELAY implementation and acceptance test](Project_GRID_VOLT_RELAY_Implementation.md) for commands and boundaries. Next: **5.7 end-to-end verification**. Earlier milestone/schema/test statements below remain historical.


## JN storage — 5.5 completed (2026-10-21)

JN now inventories approved paths, reports possible duplicates, and prepares cleanup proposals without deleting source files. Defined GRID-managed storage is measured against an initial 20 GB decimal logical budget; MCP quota escalations require separate Master Control approval to change it. This is tracking/governance, not a hard write cap or disk reservation. Schema v18 preserves scope and quota history. **535 tests passed** (14 Core, 157 Simulation, 97 Persistence, 267 Terminal). See [JN implementation and acceptance test](Project_GRID_JN_Storage_Implementation.md) for limits, accounting roots, report retention and remaining planned automation. Next: **5.6 VOLT and RELAY**. Earlier milestone/schema/test statements below remain historical.


## SENTINEL security — 5.4 completed (2026-10-21)

SENTINEL now reads local Defender protection/scan timestamps, effective firewall profiles, and bounded Security-log metadata through individually approved tasks. Timestamped results explicitly distinguish real host observations, warnings and unavailable data from simulated incidents. No scans, setting changes, elevation or remediation are enabled. Schema v17 persists these results through the existing broker. **518 tests passed** (14 Core, 157 Simulation, 97 Persistence, 250 Terminal), including 13 new security cases and live read-only provider verification. See [SENTINEL implementation](Project_GRID_SENTINEL_Security_Implementation.md). Next: **5.5 JN — Storage and files**. Earlier milestone/schema/test statements below remain historical.


## MAXIS development — 5.3 completed (2026-10-21)

MAXIS now supports registered-workspace inspection, fixed .NET build/test execution, saved diagnostics and explicit replacement suggestions staged outside original source. Every task requires human approval; build/test executes trusted project code as the current Windows user (no OS/network sandbox). Schema v16 persists immutable, revocable workspace registrations. Other specialists remain demonstrations. **505 tests passed** (14 Core, 157 Simulation, 97 Persistence, 237 Terminal). See [MAXIS implementation](Project_GRID_MAXIS_Development_Implementation.md) for commands, offline prerequisites, artifact retention and limits. Next: **5.4 SENTINEL**. Earlier milestone/schema/test statements below remain historical.


## Implemented — 2026-10-21

MCP coordinates explicit specialist requests through the 5.1 broker: atomic saved plans, linked proposals, individual human approval and briefings derived from actual task records. **Adapters remain demonstrations only.** Natural-language decomposition, autonomous dispatch, dependent task graphs and real specialist actions remain planned.

## Workflow

```text
agents initialize
mcp request "MAXIS: demo hello; JN: demo inventory" example-plan-1
mcp plans
mcp inspect <plan-id>
tasks inspect <child-task-id>
tasks approve <child-task-id>
tasks run
mcp brief <plan-id>
```

Initialize once as Master Control. Requests use 1..8 semicolon-separated clauses: SPECIALIST: action argument. Names/actions are case-insensitive. MAXIS, SENTINEL, JN, VOLT and RELAY support demo with 1..256 printable characters. MAXIS also supports delay (0..5000 milliseconds) for timeout tests; the broker deadline is one second. Semicolons are reserved delimiters, without escaping. Unknown actions, recursive MCP targets, invalid clauses or capacity failures reject the entire request without partial delegation.

Requests require a saved world. An optional 1..80-character key deduplicates identical requests from the same operator and active identity; changed text/identity is rejected. Omitting the key creates a new plan each time. Requests and arguments are persisted; do not include secrets.

Every child starts AwaitingApproval. Human Master Control inspects and approves individual children, then dispatches from the original host with tasks run. This runs the broker's next queued task, including older direct proposals, rather than selecting a particular plan. There is no blanket plan approval. Existing cancellation, three-attempt retry limit, durable execution barrier and live authorization checks remain in force.

## Authority and briefings

Only Master Control and the registered MCP role can coordinate or read whole-plan briefings. MCP can inspect, cancel or request retries for linked children, but cannot approve or run them, or access unrelated direct specialist proposals. Specialists see their own tasks, not sibling tasks or plan briefings. Authority follows registered IDs, not names.

mcp, mcp plans and mcp brief report all plans; mcp inspect <plan-id> and mcp brief <plan-id> report one. Counts/results come from current broker records under the engine lock. Missing results explicitly claim no completion. Pending work shows approval/dispatch instructions; retries require fresh approval. Briefings verify recorded state, not external PC outcomes or successful disk persistence after an I/O error. Results retain DEMONSTRATION ONLY labels. Normal chat/voice does not silently delegate. Attached sessions can coordinate within their authority; execution remains on the original host.

## Persistence and limits

Schema v15 marks the new contract so older binaries refuse the newer world. Plans reside in the existing transactional WorldTasks JSON: stable IDs, original request/key, coordinator/operator/active identity, UTC creation time and ordered child IDs. v14 JSON lacking Plans loads an empty collection, preserving the existing city. Limits are 64 plans, 256 total task records, 8 children per plan and 2048 printable request characters; archival is not yet implemented.

Validation rejects missing, repeated, cross-actor or recursive links. Snapshots defensively copy plan collections. Backup/recovery includes plans. Restart preserves links but clears queued consent and interrupts running attempts; no automatic replay occurs.

## Completion verification

485 tests passed: Core 14, Simulation 157, Persistence 97, Terminal 217. Thirteen new cases cover atomic refusal, deduplication, identity boundaries, individual approval, mixed-result briefings, snapshot links/copies, SQLite restart, v14 migration and terminal save/inspection. Existing broker tests cover timeout, cancellation, retry, SQL rollback and interrupted execution.

Next: 5.3 MAXIS — Development, introducing separately authorized real capabilities.

## Task and MCP readability follow-up — 2026-10-21

Implemented compact agent/task tables with role names, unique short references, a compact `mcp plans` overview, and separated plan/task details, results, next commands and full audit identities. `mcp brief` still includes all plan details; `mcp inspect <reference>` reads one plan. Full IDs remain in detail views. Displayed short references (at least eight characters) are accepted by task inspect/approve/cancel/retry and MCP inspect/brief; ambiguous prefixes are rejected, and resolution is restricted to the current authorized scope. A copied trailing period or comma is tolerated. Plan/task mixups produce specific guidance. Approval boundaries and persisted data are unchanged. Persistence warnings now appear only on I/O/database errors.

Use `tasks list`, then `tasks inspect <displayed-task-reference>`. Review its argument before using the displayed approval command. Command examples are printed on separate lines without sentence punctuation. Verification: 219 terminal tests passed, including short-reference, punctuation, plan/task mismatch and cross-agent scope checks.
