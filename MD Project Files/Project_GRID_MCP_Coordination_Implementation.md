# Project GRID — 5.2 MCP Administration

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
