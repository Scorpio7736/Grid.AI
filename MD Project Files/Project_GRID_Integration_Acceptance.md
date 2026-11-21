# Grid.AI 6.31 integration acceptance

Updated 2026-11-20. Integration is implemented for the verified workflows below; the final Phase 7 gate remains open.

Latest follow-up: [local repository diagnosis, real Git coding acceptance, file recovery, meeting checks and copied-PC HTTP status](Project_GRID_Local_Coding_Integration.md). Full current verification: **1,602 tests passed**. Earlier counts below are historical.

## Offline coding dependency and verifier repair — 2026-11-20

The clean-clone setup now restores the entire pinned `Grid.AI.slnx` into GRID's private NuGet cache instead of restoring only the terminal project. This includes the test SDK and transitive packages needed for offline MAXIS build/test work and the agentic-coding acceptance verifier. Restore disables build servers, first-run work and multi-level SDK lookup so the verifier can run reliably under either the executable or a `dotnet` host without waiting on a competing build server. A regression check protects the whole-solution cache requirement.

The current private runtime was repaired from already installed, exact-version local packages; its 12-package disposable-test closure restores with cleared package sources. The rebuilt real-model verifier passed at `GridAgenticCoding-470e31a8c2084b52aa2fc204a525078f`: a discovered xUnit test failed, local Qwen diagnosed subtraction instead of addition, GRID previewed the exact high-risk code change, separate human approval applied only `Calculator.cs`, and the second approved MAXIS test completed successfully. The full solution then passed **1,602 tests** (14 Core, 163 Simulation, 107 Persistence, 1,318 Terminal), with zero failures or skips.

Manual MAXIS execution from an interactive GRID host exposed an environment-sensitive fresh-clone test: its child PowerShell inherited the host console, waited for setup approval and timed out instead of exercising redirected-input refusal. The test harness now redirects and closes the child standard input explicitly. The exact interactive-parent reproduction passes all eight fresh-clone cases, and the complete isolated solution rerun again passes **1,602 tests** with zero failures/skips. The earlier failed MAXIS attempt remains valid diagnostic evidence rather than being rewritten as a test pass.

This proves local cached-package operation on this PC; it does not claim an OS-level network-disconnected run, copied-device repair, Android support or physical second-PC acceptance. The newer `fix that failure` conversational shortcut still needs its own real-model demonstration; this verifier exercises the established inspected-repository repair route.

## Reviewed-failure repair — 2026-11-20

In a private MCP conversation, review a failed saved plan and say `fix that failure` or `repair this failure`. The current authenticated review must identify exactly one attempted, failed/timed-out MAXIS build/test. Other pending requests, missing/expired review and meeting disclosure are refused. Diagnosis binds to that task ID and checks the latest workspace evidence both before inference and after the diagnosis; a newer result requires fresh review instead of silently changing the repair target.

The existing bounded repository diagnosis/file-draft path retains workspace grants, captured instructions, inventory validation and code-change approval. No file is applied or build retried by the shortcut. After reviewing/approving/applying the draft, `test this change` uses the existing separately approved test-plan flow.

Seven regression cases cover both phrases, no review, unstarted work, competing newer failure, evidence changing during inference, and meeting/pending-file refusal with the original draft preserved. **176 focused and all 1,601 solution tests passed** (14 Core, 163 Simulation, 107 Persistence, 1,317 Terminal), zero failures/skips. Controlled inference fixtures verify routing and approval boundaries; this increment does not claim a new real-model repair, Android, physical second-PC or disconnected-network acceptance. Changes remain uncommitted/unpushed for user testing.

## Ownership and compatibility

The [contract inventory](Project_GRID_Integration_Contracts.md) remains the architectural reference. Grid.Agent now owns conversation/context/memory, identity/session/voice and task contract definitions and task validation. Legacy Grid.Communication and Grid.Simulation assembly names forward to those same types. The simulation host remains the single authoritative writer; Persistence saves its snapshots. No second registry, conversation history or task queue is introduced.

Schema v24, GUIDs, enum values and JSON meanings are unchanged. Ownership tests cover legacy assembly resolution and JSON defaults, shared legacy/typed histories, duplicate dispatch prevention, saved identities, grants, memory and meeting history. Reopening a world requires authentication; queued approval is deliberately reset for human review. Existing backup/version-refusal/recovery tests passed. This is an assembly ownership change, not a new database migration. Retain matching executable/data backups for rollback; second-device migration/rollback acceptance remains open.

## Saved-world backup, migration and rollback acceptance — 2026-11-14

The 6.31 local preservation gate is verified by six populated-world scenarios in `AgentOwnershipMigrationTests`: normal reopen, corrupt-primary recovery, unsupported-future-version refusal and recovery, invalid-backup refusal, successful v23-to-v24 migration, and failed migration followed by explicit backup recovery. All 31 ownership cases pass. The full solution run passed 1,352 tests (14 Core, 157 Simulation, 99 Persistence, 1,082 Terminal); no tests failed or were skipped. The v23 fixture retains current payloads and rewinds only the metadata-only v24 migration marker; it is a controlled compatibility fixture, not a historical old-executable capture.

Every scenario checks world/agent/operator IDs, identity registration, conversation/meeting history, context grants and memory provenance/grants. Task IDs and history survive; live sessions do not. Queued tasks return to AwaitingApproval with a recovery audit and no execution attempt. The backup remains byte-for-byte unchanged through attempted migration and recovery. Unsupported primary bytes are retained, invalid backups never replace the primary, and failed migration leaves version 23 with no partial v24 marker. Explicit recovery archives the failed primary for inspection.

Rollback procedure:

1. Stop all Grid.AI sessions before replacing a deployment. Keep the matching executable/package, database backup and required file-edit recovery artifacts together. Record their location/version; do not mix independently updated packages and data.

2. Work on a separate copy when testing upgrades. Preserve the original package and save. This ownership release uses schema v24 and does not introduce a schema upgrade.

3. If loading fails, retain the primary and diagnostics. Use the existing explicit recovery path only with a validated backup; it stages and validates the backup before replacing the primary and archives damaged files.

4. For executable rollback, restore the matching complete package/data set. Do not force a newer schema open with an older executable. Reauthenticate, inspect recovered tasks and file state, and approve only work you intend to run. No stale execution approval is replayed.

Local database recovery is verified. Running an older executable against a historical package, restoring external file-edit artifact bundles, and offline second-PC release acceptance remain separate gates; no second-device completion is implied.

## Conversation behavior

Both inline and attached user input passes through the conversation boundary. Terminal command names, quotes and code fences cannot accidentally invoke administrative commands while connected. The trusted internal command API remains available to host adapters. Chat tab completion no longer suggests administrative commands.

- Say `return to terminal` or `/terminal` to return to ordinary commands. `/close` closes the host or detaches an attached window; ordinary `exit` is conversation text while connected.

- Say `show our conversation`, `show conversation details`, `show my tasks`, `remember that ...`, `turn off animation`, or `listen to my voice` for supported actions.

- Exact `build demo` / `test demo` requests target the registered workspace and MAXIS directly. The model cannot replace these explicit capabilities with an unrelated plan. General planning still uses local inference and validation.

- Submit the draft, then say `approve this plan`, `run this plan`, or `cancel this plan`. Approvals use the exact reviewed task snapshot and current authenticated scope. Attached requests execute through the original host broker.

- File changes retain full preview, exact hashes, explicit high-risk approval and recovery. `approve that`, `apply it` and `cancel it` resolve the current file operation; competing plan/file references require clarification.

These are supported conversational intents. Arbitrary multi-step autonomous coding, resumed file-edit focus, complete meeting tool parity and general natural-language delegation still require acceptance. A successful model reply alone never proves an action occurred.

## Saved-plan conversational resumption — 2026-11-14

In a one-to-one conversation as Master Control, say `resume my work` (also `continue my work` or `show unfinished plans`). Grid.AI lists saved, non-archived operator plans with unfinished steps across conversations. Say `resume plan 1` to select the numbered entry and review its current tasks/results, then `approve this plan` and `run this plan` when appropriate. Selection never approves or executes work. Completed, cancelled and denied-only plans are omitted.

This uses the existing durable coordination/task records; no schema change or model inference is required. Numbers bind to the displayed list, not a newly sorted query. Selection expires after changes to identity, conversation, project, agent activity, context or memory. Private operator plan requests are not listed in meetings or agent identities. Selecting a plan does not restore earlier private chat/context grants or file-edit approval.

On database reload, queued work requires fresh approval under existing recovery rules. Partial plans approve only their reviewed awaiting-approval steps; completed steps are retained and never dispatched again. Approval rejects tasks changed after preview. Failed/interrupted tasks retain their results and still require the existing explicit retry workflow; selection does not infer that replaying side effects is safe.

Regression coverage includes actual SQLite save/reload followed by conversational approval/execution, ambiguous numbered choice, expired selection, meeting privacy, completed-step preservation and changed-task refusal. Five new regression cases pass. Full solution verification passed 1,357 tests (14 Core, 157 Simulation, 99 Persistence, 1,087 Terminal), with no failures or skips. General autonomous coding resume and restored file-edit recovery remain open.

## Verification

- Full solution: `dotnet test --no-restore --verbosity quiet` passed **1,347 tests**: 14 Core, 157 Simulation, 99 Persistence, 1,077 Terminal. This includes ownership, inline/attached conversation, meeting, voice, memory, authorization, cancellation and persistence regressions. The earlier contract-baseline terminal failures are resolved.

- Real local coding: run `Grid.Terminal.exe --verify-agentic-coding` after provisioning the bundled runtime, model, SDK and cached test dependencies. The verifier retains its disposable project, transcript, source, database, execution reports and TRX files in the printed temporary evidence directory.

- Verified run: `GridAgenticCoding-b8a33bcba2cb4846a82be69f0076cbf2`. A real discovered xUnit test failed for subtraction; local Qwen inference drafted a C# addition fix; explicit code approval applied it; a second real test passed. TRX assertions require both Failed and Passed results for the named test. Task records survived SQLite reload. [Retained conversation evidence](verification/6.31-agentic-coding.json) and [discovered-test outcomes with corrected source/hash](verification/6.31-coding-test-results.json).

- The harness provides the observed failure and desired repair to the agent through ordinary messages. This demonstrates a reviewed local coding cycle, not autonomous diagnosis of arbitrary repositories. It uses a disposable project rather than a Git checkout. Restore has no package sources and inference uses bundled loopback assets; OS-level disconnected-network and second-device tests were not performed.

## Release gates still open

All 15 platform components have implementation references in the [platform plan](Project_GRID_AI_and_Agent_Platform_Plan.md); development milestones do not imply production acceptance. Its 12 release checks remain pending final evidence/review:

| Acceptance | Current evidence and remaining work |
| --- | --- |
| Offline inference | Bundled local coding works here; disconnected second-PC run pending. |
| Multiple models | Provider/registry tests exist; selected specialist artifacts and live adoption pending. |
| Identity/memory isolation | Regression coverage passes; final release review pending. |
| Retrieval provenance | Captured-context/retrieval tests pass; release corpus evaluation pending. |
| Unauthorized operations | Host authorization and approval regressions pass; final adversarial suite pending. |
| Failure recovery | Recovery/cancellation regressions pass; failing-instance/device evidence pending. |
| Safe diagnostics | Existing redaction tests pass; complete candidate diagnostics review pending. |
| Performance/storage | 128 GB package target retained; production candidate/device benchmarks pending. |
| Evaluations | Development suite passes; production model evaluations pending. |
| Model rollback | Development catalog gates pass; live agent adoption/rollback pending. |
| Portability | Local copy evidence exists; second supported PC offline acceptance pending. |
| Repeatable training | Synthetic CPU acceptance exists; selected production training/model evidence pending. |

The follow-up coding reference records repository-instruction handling, source CRUD, reviewed diagnosis/repair and saved-plan/file recovery. Unattended multi-file work, production model adoption and device acceptance remain open. Continue terminal-first milestones 6.32–6.38 and obtain user release review before Phase 7. No production-readiness or second-device completion is claimed.

## User-test follow-up — 2026-11-14

The reported `plan to have maxis demonstrate the message "resume test"` request now maps directly to the labeled `maxis.demo` capability. It requires neither a registered workspace nor model inference. The exact reported sequence is covered through submission, reconnect, numbered resumption, blocked unapproved execution and single approved execution.

Malformed main-terminal `resume` arguments are validated before opening an attached window. `resume my work` there explains how to enter an inline conversation; valid conversation IDs retain the existing window behavior.

Full verification passed 1,361 tests (14 Core, 157 Simulation, 99 Persistence, 1,091 Terminal) using `dotnet test --artifacts-path TestResults/resume-fix --no-restore --verbosity quiet`. The user's running Grid.AI locked the normal executable, so verification used isolated outputs. Process-based persistence tests now locate either conventional or isolated build outputs. The running process was not stopped or replaced; close it normally and rebuild before retrying the updated normal executable.

## Gate 1 conversational references — 2026-11-19

Direct human control phrases now resolve before intent interpretation. If a file request, reviewed plan, unsubmitted draft or interpreted request competes for the reference, GRID asks the user to identify it without approving, cancelling or executing anything. An unspecified revision asks what should change and retains the review.

`ask MAXIS to do it` targets only a uniquely selected plan whose tasks are all MAXIS capabilities. It uses the existing current-scope and approval checks, never reassigns work, grants approval or replays completed tasks. `how is it going?`, `show its progress` and `show the results` refresh the uniquely selected plan from actual task records without inference or execution. Logout clears the selection. Attached-meeting tests exercise natural approval, MAXIS execution, cancellation and duplicate-run refusal through the real named-pipe protocol.

Verification: **194 focused tests passed; 1,562 full-solution tests passed, zero failures/skips**, using `TestResults/gate1`. These are bounded improvements within Gate 1, not completion of its broader multi-step coding, group coordination, revision/resumption or network-blocked real-model acceptance. No new physical-device or offline real-model run is claimed.

## Task-draft replacement — 2026-11-19

### Saved-plan replacement and clarification controls — 2026-11-20

After reviewing a saved operator plan, `replace this plan with <complete replacement request>` cancels its exact reviewed task set only if every task is unapproved, unarchived and has zero attempts. Validation occurs under the engine's task authorization lease and state lock; changed or incomplete task selections cause no partial cancellation. Other pending drafts/requests must be resolved first. Cancellation is saved before replacement drafting begins; the original plan and cancellation audit remain available. A replacement needs fresh submission, review and approval. If drafting fails, the old plan stays cancelled. If saving fails, no replacement is drafted and the terminal explicitly reports uncertain durability; resolve storage and inspect state before proceeding. Approved, running or previously attempted plans require their separate cancellation/recovery workflow.

Pending task/intent clarification now participates in conversational reference resolution. `cancel it` dismisses a unique clarification without inference. `approve that` or `ask MAXIS to do it` asks for the missing information rather than creating authority. Multiple pending operations still require explicit selection.

Seven new cases cover inline/meeting saved-plan replacement, partial/stale/approved task-set refusal, failed drafting, failed persistence and both clarification routes. **169 focused and all 1,594 solution tests passed, zero failed/skipped** (Core 14, Simulation 163, Persistence 107, Terminal 1,310), using `TestResults/gate1`. No real-model offline or physical-device acceptance is claimed. Changes remain uncommitted/unpushed for user testing.

In an inline conversation or meeting, say `revise this draft: ask MAXIS to test demo` or `replace this draft with <complete replacement request>`. Bare `revise this draft` asks for the complete revised request without discarding anything. Replacement discards only the unsubmitted draft, uses existing deterministic development or bounded AI proposal routes, and creates no tasks until explicit submission. Submission and approval are still required; existing saved plans, their tasks and results are unchanged. Failure leaves no old draft silently available for submission. Ambiguous `revise it: ...` is rejected when multiple operations are pending; expired session/context drafts are refused. Logout clears drafts and invalidates in-flight proposal requests.

Five new regression cases cover inline/meeting replacement, denial before approval, failed replacement, ambiguity with a saved plan, and logout. **145 focused tests and all 1,587 solution tests pass, zero failed/skipped**, using `TestResults/gate1`. This is complete replacement of an unsubmitted draft, not automatic editing/cancellation of approved or running plans. Saved-plan revision, broader multi-step coding and real-model offline acceptance remain open. Changes are uncommitted/unpushed for user testing.

## Explicit MAXIS workspace delegation — 2026-11-19

`ask MAXIS to build <workspace>`, `ask MAXIS to test <workspace>` and `ask MAXIS to run tests for <workspace>` now create the existing bounded development draft without model interpretation. An enabled registered workspace is required; unknown names, paths, missing targets and arbitrary shell requests are refused. In a meeting, MAXIS must already be a participant. Submission, review, approval and execution remain separate; naming an agent is not approval.

Eight new regression cases cover inline/meeting drafts, exact capability selection, blocked execution before approval, conversational cancellation, unknown/missing/path targets, shell-like requests and missing meeting membership. All four tracked test projects are included in `Grid.AI.slnx`. **Every solution test passed: 1,582 passed, zero failed/skipped** (Core 14, Simulation 163, Persistence 107, Terminal 1,298), using `TestResults/gate1`. These changes remain uncommitted and unpushed, awaiting user testing. The broader Gate 1 release and physical/offline acceptance remain open.

## Dynamic named-MAXIS intent routing — 2026-11-20

Ordinary `ask MAXIS to <work>` requests that are not the existing exact build/test forms now reach MCP's versioned local-AI intent planner. No HTML, filename, destination or requested content is encoded in the router. MCP can return a typed single-file operation, a specialist task plan, one focused clarification or an unsupported explanation. Clarification keeps the original request and is reinterpreted with the user's answer. The host then validates the typed destination and target and uses the established MAXIS generation, complete preview, hash-bound approval and apply workflow. Exact workspace build/test requests remain deterministic; shell, authority, transfer and destructive control phrases remain refused before inference.

The regression uses the user's HTML sentence only as one example of the general route: controlled MCP inference asks for missing filename/destination, the answer yields a typed `CreateFile` intent, MAXIS drafts the content, and no file exists before separate high-risk approval and apply. Existing named build/test delegation and unsafe-request protections remain green. The earlier artifact-specific shortcut and its test were removed rather than retained alongside this route. **Nine focused cases and all 1,603 solution tests passed** (14 Core, 163 Simulation, 107 Persistence, 1,319 Terminal), zero failures/skips, using isolated `TestResults/dynamic-maxis-intent` and `TestResults/dynamic-maxis-intent-full` outputs. This is a dynamic planning increment, not proof that every free-form request or local-model response will succeed; unsupported capabilities and ambiguous operations must still clarify or fail closed.

**Partial-answer repair follow-up:** A reported live exchange supplied a Desktop folder after MCP asked for a complete file path. The next model response attempted an invalid file intent without a filename, and the host correctly refused it but ended the flow. Intent interpretation now uses a bounded three-attempt cycle after JSON or semantic validation failure. Each retry receives the host validation reason and complete conversation request, and must return either a valid version-1 intent or one short `Clarify` question for the next missing fact. The host does not supply a filename, destination or artifact-specific question. A focused regression reproduces the folder-only answer, verifies a model-authored filename question, confirms no tasks/actions, and then accepts the next answer as a reviewed intent. Three invalid outputs fail closed. **Seven focused cases and all 1,604 solution tests passed** (14 Core, 163 Simulation, 107 Persistence, 1,320 Terminal), zero failures/skips, using `TestResults/dynamic-intent-repair` and `TestResults/dynamic-intent-repair-full`.

**Polite delegation routing follow-up:** A reported `hey can you task an agent to make me a simple ... Python file ...` request was sent to MCP's casual dialogue model because the greeting and delegation wrapper prevented action routing. Dialogue correctly had no execution tools but returned an unhelpful “cannot create files” response. The router now removes polite greeting/request wrappers and recognizes general requests to task, ask, have or assign an agent. Those requests enter the same dynamic typed-intent planner described above. No language, extension, filename, content or destination is encoded in this route. A regression uses the reported Python request and controlled MCP responses to verify dynamic filename clarification and a reviewed `CreateFile` interpretation, with no task or write during clarification. **Three focused cases and all 1,605 solution tests passed** (14 Core, 163 Simulation, 107 Persistence, 1,321 Terminal), zero failures/skips, using `TestResults/polite-delegation-intent` and `TestResults/polite-delegation-intent-full`.

**Multi-turn clarification strengthening:** After a C++ request, the local model asked for an agent despite reviewed file work already having a MAXIS route, forgot that Desktop was supplied, and ultimately produced another incomplete intent. The planner contract now requires it to combine the original request with every accumulated `User clarification`, retain answered facts, and ask only for the next genuinely missing value. Natural destinations such as Desktop/Documents are passed to the host resolver without demanding an absolute path. File type/programming language is artifact information, not an agent identity. The bounded validation loop permits two corrected attempts after the initial response; all three remain inference-only and cannot approve or execute. Focused regressions cover the reported routing family, partial-answer recovery and three-invalid-output refusal. **Four focused cases and all 1,606 solution tests passed** (14 Core, 163 Simulation, 107 Persistence, 1,322 Terminal), zero failures/skips, using `TestResults/multiturn-dynamic-intent` and `TestResults/multiturn-dynamic-intent-full`.

**Conversational plan presentation follow-up:** Valid typed intents are no longer printed as `INTERPRETATION v1`, destination/target tables, assigned-route metadata and source disclaimers during normal chat. MCP instead explains the proposed operation naturally, states that nothing has changed, and asks whether to prepare the full preview. Natural replies are now interpreted by a bounded MCP follow-up classifier as preview/cancel/unclear; this classification can prepare a preview but can never approve, apply or run work. Explicit `review this request` remains a deterministic fallback. `details` keeps the pending request intact and exposes the internal planning record on demand. Grounding, path validation, exact-hash review and separate approval/application remain unchanged. Earlier fixed-word confirmation evidence is superseded by the pipeline evidence below.

**Private validation-to-question boundary:** Intent validation failures now remain private host-to-agent context. After normal bounded repair is exhausted, MCP receives one final clarification-only inference turn containing the validation reason and accumulated user conversation. Only MCP's natural question is displayed. Raw messages such as `File intents require...`, grounded-value failures, parser errors, typed route metadata and repair prompts are not shown in ordinary chat. If the clarification-only response is unavailable or invalid, MCP gives a plain safe explanation and confirms that nothing changed. Focused tests reproduce the reported Documents-without-filename exchange, verify an MCP-authored filename question, and confirm that no task/file action or internal validation text is exposed. **Five focused cases and all 1,608 solution tests passed** (14 Core, 163 Simulation, 107 Persistence, 1,324 Terminal), zero failures/skips, using `TestResults/mcp-clarification-dialogue-2` and `TestResults/private-agent-clarification-full`.

**First-person operational request routing:** A reported `I need a txt file to be made...` request bypassed action interpretation and reached MCP's tool-less dialogue response, which incorrectly advised using the operating-system file manager. First-person need/want/would-like requests that mention an artifact, task, agent or supported action now enter the same dynamic typed-intent planner. This is a general routing rule, not a TXT/Pictures/content special case. A regression reproduces the reported structure and verifies that MCP names MAXIS, summarizes the filename and user-named destination, asks to prepare the preview, creates no task, and never claims it cannot create files directly. **Four focused cases and all 1,609 solution tests passed** (14 Core, 163 Simulation, 107 Persistence, 1,325 Terminal), zero failures/skips, using `TestResults/need-artifact-routing` and `TestResults/need-artifact-routing-full`.

**Agent → GRID → agent preview pipeline:** Connected reviewed file/document work now follows the intended boundary: MCP interprets the user request into a typed operation; MCP interprets the user's natural follow-up as preview/cancel/unclear; GRID alone validates and stages the capability result; the raw receipt is retained privately; MCP receives a separate non-authorizing result-interpretation turn; and the user sees MCP's concise explanation plus host-authoritative preview paths and next approval step. Raw hashes, internal commands and tool receipts remain behind `details`. Approval and apply remain explicit GRID controls and are never inferred by the result model.

The DOCX regression requests exact text, uses `sure` as a model-interpreted preview confirmation, stages a native/layout preview without creating the destination, requires separate high-risk approval and apply, then opens the resulting DOCX through GRID's extractor and verifies the requested text. The HTML regression exercises the same pipeline with MAXIS content drafting. **Three focused pipeline cases and all 1,610 solution tests passed** (14 Core, 163 Simulation, 107 Persistence, 1,326 Terminal), zero failures/skips, using `TestResults/agent-grid-agent-pipeline-2` and `TestResults/agent-grid-agent-pipeline-full`. This verifies reviewed file/document preview flow; consistent application to every conversation/meeting capability remains open.

**Natural reviewed-change continuation repair — 2026-11-20:** A staged preview previously lost its active conversational workflow when the operator replied naturally (`Nice! lets do it` or `no make the file`). The reply fell into ordinary dialogue, allowing the model to discuss unrelated prior context while the file remained absent. Reviewed Draft/Approved states now route ordinary action-like follow-ups through a bounded MCP classifier. GRID revalidates the exact change ID and state, performs approval or apply itself, and returns the receipt privately for a second MCP explanation. High-risk approval still requires explicit acknowledgement. Host output suppresses any model claim that a Draft/Approved file was created, saved, written, applied or completed. Unrelated questions do not execute or preserve a stale approval. The regression deliberately supplies false model claims, verifies that the draft and approved states leave the destination absent, then verifies the applied file and exact content. Focused compatibility coverage passed 4/4, unified conversation coverage passed 133/133, and **all 1,611 solution tests passed** (14 Core, 163 Simulation, 107 Persistence, 1,327 Terminal), zero failures/skips, using `TestResults/natural-reviewed-compat`, `TestResults/natural-reviewed-conversations` and `TestResults/natural-reviewed-full-2`.

## Description-based resumption — 2026-11-19

In a private conversation, `resume the plan about <description>` and `review the plan for <description>` search saved unfinished operator requests as literal text, without inference or task IDs. A unique match receives a fresh task review; multiple matches ask for a more precise description or offer a numbered list. Failed/ambiguous lookup clears the prior reviewed focus, preventing later vague approval from targeting an unrelated old plan. Completed/cancelled/denied/archived plans are excluded, including when state changes after a numbered list was shown. Meeting lookup remains blocked to protect private operator requests.

Database-reload coverage verifies that description selection restores neither old approval nor execution. Tests also cover ambiguity, stale selection, control-like search text, no model calls and meeting privacy. **132 focused and all 1,574 solution tests passed**, zero failed/skipped, using `TestResults/gate1`. This increment and the preceding clarification repair remain uncommitted for user testing. Broader multi-file resume and real-model offline acceptance remain open.

## MCP executable testing follow-up - 2026-11-14

**Local clarification repair — 2026-11-19 (uncommitted, awaiting user testing):** The host now accepts a question-only response with an optional integer `version:1`, consistent with the proposal schema. Unknown/duplicate properties, unsupported versions, blank questions and mixed question/action/approval payloads remain rejected. The single bounded format-repair retry can return a clarification rather than being forced into a task proposal. Clarification retains the original request and cannot approve, submit or execute tasks. **110 focused tests and all 1,570 solution tests passed, zero failed/skipped** using `TestResults/gate1`. This fixes tested parser/retry cases; the historical real-model failure below still needs a fresh real-model run before it can be closed. No commit or push was performed for this repair.

The bundled-model `--verify-mcp-workflow` run completed five of six checks. Local Qwen startup, multi-specialist draft, denial before approval, approved VOLT/RELAY execution, and SQLite evidence retention passed. Missing-workspace clarification failed with `Assistance refused: Invalid clarification response.` This remains an open model-response/clarification defect; do not mark MCP acceptance complete.

Retained source evidence: `C:\Users\jack\AppData\Local\Temp\Grid611Verification-33d634a8a46149569d026db4e52739e5\report.json`.

Interactive testing was blocked because the running Grid.AI console was absent from the computer-control window inventory. Login and mouse/keyboard workflows were not verified. The file-operations attempt was interrupted; no completed Desktop HTML artifact or OneDrive synchronization was confirmed. These are limitations, not passing checks.
