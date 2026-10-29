# 6.11 — MCP digital-helper workflow

**2026-10-28 meeting round/addressing follow-up:** Explicit `each of you`, `everyone`, `all of you`, and `you all` requests now generate independent replies for every meeting participant, committed together with one user message. Cancellation/scope changes prevent partial round storage; unavailable inference is labeled per participant. `hey/hi/hello MAXIS ...` routes to MAXIS. While a meeting is connected, `maxis what is your favorite color` is chat; recognized management subcommands such as `maxis workspaces` and `maxis register ...` retain command behavior. Group introductions still use registered roles without model inference. Shared prompts prohibit impersonating other participants, distinguish fictional preferences from observed facts, and ask for a source when a reading request has none. No private context, task execution or automatic invitations are granted. Verification: **1,022 regression tests passed** (752 Terminal), plus bundled-model replay of the reported messages. Local-model answers can still be factually wrong; correct routing is not factual verification.

**2026-10-28 meeting identity repair:** Leading participant names now direct replies (`maxis`, `MAXIS, introduce yourself`) without requiring `@`; absent named agents are refused rather than silently routed to MCP. `each of you introduce yourselves` stores one user message followed by every participant’s registered role introduction, atomically and without AI inference. Ordinary meeting AI receives the authoritative participant roster and roles, with shared history only. General discussion still selects one speaker per turn; this does not add automatic invitations or general multi-agent broadcasts. Verification: 1,017 regression tests passed across the four suites (747 Terminal), and the bundled model correctly handled the reported introduction/name sequence in an isolated world. Evidence: `%TEMP%\GridMeetingIdentityProbe\report.txt`.

Implemented 2026-10-28. MCP means **Master Control Program**, the user's primary digital helper. Phase 7 still requires 6.12–6.15. Automatic specialist invitations into an existing chat belong to 6.12; this release does not implement them.

## Request to result

1. Connect to MCP and use `mcp plan <custom request>` or `talk plan <custom request>`. Local AI analyzes the request against the capability catalog and available registered targets. Missing information can produce a scoped clarification question rather than a task.
2. Answer a clarification using `talk <answer>`, or cancel. The original request and question are retained during the follow-up; identity, conversation, project, activity, context or memory changes invalidate it.
3. The draft shows the original request, specialist, exact capability, argument and catalog-based reason for the assignment. Check that every requested action is present. AI-generated plans can omit or misunderstand work; the preview is not evidence of completion.
4. `talk submit plan` saves immutable child tasks awaiting human approval. `talk discard plan` or `talk cancel` discards an unsubmitted draft.
5. Inspect each task and approve its exact request. In the original host, `mcp run <plan-id>` delegates ordered execution of only that plan's remaining approved tasks. `talk run plan <plan-id>` is equivalent. No task approval is inferred from a conversation or model output.
6. Execution stops on failure, cancellation, activity/session change or a persistence problem. The final briefing reads broker states and adapter receipts, including unknown telemetry and failed/unconfirmed outcomes. It does not generate success claims with AI or freshly inspect external files.

`mcp inspect <plan-id>` and `mcp brief <plan-id>` show progress and recorded results. `mcp cancel <plan-id>` revokes pending/running work without undoing completed effects. Failed/interrupted tasks require `tasks retry <task-id>` and fresh approval. Cancelled/denied plans are not silently resumed. `mcp repeat <plan-id>` makes a new plan from the recorded exact steps, with new task IDs and no carried-over approval. Existing plan/task capacity limits remain 64/256; no automatic dependency graph, result interpolation or rollback is implied by ordered execution.

## Delegated authority matrix

| Operation | MCP workflow authority | Human control |
| --- | --- | --- |
| Read operational state, plans and results | Existing coordinator visibility | Authenticated scope applies |
| Analyze context and retain memory | Existing agent/project grants and retention rules | Explicit grants/corrections/revocation remain |
| Select specialists and prepare custom work | Catalog-listed capabilities; validate roles and arguments | Review draft and exact child requests |
| Run approved operational tasks | Original-host human delegates a fixed plan to MCP orchestration; broker uses the real operator session | Every child independently approved, one attempt; cancellation/logout/pause revokes active work |
| Create/update/delete files | Existing MAXIS previews, exact hashes and recovery receipts | Separate file approval/apply; high-risk acknowledgement where required |
| Approve tasks, edits or quota changes | No self-approval | Reserved to human Master Control |
| Credentials, identity/agent permissions, context grants | No authority expansion or impersonation | Reserved human commands |
| NGE, copy/transfer, GRID start/pause | Not available as generated plan capabilities | Direct human commands and existing confirmation rules |
| Windows elevation or arbitrary shell access | None | No new OS privileges |

“Near-Master-Control” means broad assistance through implemented operational tools and explicitly granted data, not unrestricted administrative identity. MCP does not receive a permanent master token: the orchestration service executes approved work under the requesting human's authenticated session. Switching identity to MCP cannot approve or dispatch human work. `mcp authority` displays these boundaries; `mcp capabilities` lists the planning catalog.

`McpCapabilityCatalog` centralizes planning metadata, structured-model schema choices and selection explanations. Extending it requires a corresponding validated `TaskCapabilities` implementation/adapter and tests; adding model text or metadata alone never authorizes new execution. Existing adapters support MAXIS development, SENTINEL observations, JN storage analysis, VOLT resource/pacing operations and RELAY local notifications. File operations keep their separate reviewed workflow.

## Persistence and execution binding

Schema remains v24. Existing saved coordination plans, task approvals/audits, event records and adapter receipts are reused. The natural-language draft and clarification are transient; the saved plan contains exact normalized specialist requests. Approval binds immutable child requests. A deferred run also binds the selected task records at request time, preventing it from consuming later approvals or retries. The broker revalidates session/activity and target grants during each attempt and saves the start before invoking an adapter. Restart clears queued consent and interrupts running work under the existing recovery policy; nothing automatically replays.

## Verification

**1,012 tests passed**: Core 14, Simulation 157, Persistence 99, Terminal 742. New workflow cases cover multi-specialist drafting/execution, missing-target clarification, no self-approval/elevation, exact selection, unrelated queued work, stale approval refusal, session revocation, cancellation during work, durable-start failure, plan reuse and evidence-backed results. Existing coordination/persistence tests cover restart and target revocation.

Six bundled `qwen3:4b` acceptance checks passed using a synthetic SQLite world: local runtime, missing-workspace clarification, two-specialist custom draft, refusal before approval, real VOLT/RELAY execution and saved receipts. Report: `%TEMP%\Grid611Verification-67e86385e25b40cbab12932f506810b3\report.json`. Reproduce with `Grid.Terminal.exe --verify-mcp-workflow`; it does not open the user's world. These are sampled model checks, not a guarantee of complete plans for every request. The initial model test omitted the notification; the corrected prompt emphasizes all requested actions and the preview explicitly asks the user to verify coverage.

## Terminal test

Run these in the original host terminal after restarting the updated build:

```text
grid -start
connect MCP -inline
mcp authority
mcp plan Read current host resource observations with VOLT and record a local RELAY notification saying Resource review requested. Do not do anything else.
```

Expect a draft containing **VOLT / volt.status / current** and **RELAY / relay.notify / Resource review requested**. If correct:

```text
talk submit plan
mcp plans
tasks list
tasks inspect <first-task-id>
tasks approve <first-task-id>
tasks inspect <second-task-id>
tasks approve <second-task-id>
mcp run <plan-id>
mcp brief <plan-id>
relay inbox
```

Replace placeholders with displayed IDs/references. Expect two completed tasks and a recorded notification; resource values may legitimately be unknown. The plan must refuse dispatch before both approvals. `mcp repeat <plan-id>` creates new unapproved tasks if you want to repeat the work.

## Meeting syntax repair

The reported `meetings ...` and `connect meetings ...` commands now route through the existing meeting validator before launching a window. All three forms support `-inline`:

```text
meetings MCP MAXIS JN -inline
agent meetings MCP MAXIS JN -inline
connect meetings MCP MAXIS JN -inline
```

Use one form, after `disconnect` if already connected. Omit `-inline` for a new window. Invalid participants never launch a misleading conversation window. Four regression cases cover inline/window aliases and refusal of unknown participants. This is syntax compatibility for existing meetings, not the automatic specialist invitations planned in 6.12.
