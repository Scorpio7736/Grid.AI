# GRID 6.31 integration acceptance

Updated 2026-11-14. Integration is implemented for the verified workflows below; the final Phase 7 gate remains open.

Latest follow-up: [local repository diagnosis, real Git coding acceptance, file recovery, meeting checks and copied-PC HTTP status](Project_GRID_Local_Coding_Integration.md). Full current verification: **1,374 tests passed**. Earlier counts below are historical.

## Ownership and compatibility

The [contract inventory](Project_GRID_Integration_Contracts.md) remains the architectural reference. Grid.Agent now owns conversation/context/memory, identity/session/voice and task contract definitions and task validation. Legacy Grid.Communication and Grid.Simulation assembly names forward to those same types. The simulation host remains the single authoritative writer; Persistence saves its snapshots. No second registry, conversation history or task queue is introduced.

Schema v24, GUIDs, enum values and JSON meanings are unchanged. Ownership tests cover legacy assembly resolution and JSON defaults, shared legacy/typed histories, duplicate dispatch prevention, saved identities, grants, memory and meeting history. Reopening a world requires authentication; queued approval is deliberately reset for human review. Existing backup/version-refusal/recovery tests passed. This is an assembly ownership change, not a new database migration. Retain matching executable/data backups for rollback; second-device migration/rollback acceptance remains open.

## Saved-world backup, migration and rollback acceptance — 2026-11-14

The 6.31 local preservation gate is verified by six populated-world scenarios in `AgentOwnershipMigrationTests`: normal reopen, corrupt-primary recovery, unsupported-future-version refusal and recovery, invalid-backup refusal, successful v23-to-v24 migration, and failed migration followed by explicit backup recovery. All 31 ownership cases pass. The full solution run passed 1,352 tests (14 Core, 157 Simulation, 99 Persistence, 1,082 Terminal); no tests failed or were skipped. The v23 fixture retains current payloads and rewinds only the metadata-only v24 migration marker; it is a controlled compatibility fixture, not a historical old-executable capture.

Every scenario checks world/agent/operator IDs, identity registration, conversation/meeting history, context grants and memory provenance/grants. Task IDs and history survive; live sessions do not. Queued tasks return to AwaitingApproval with a recovery audit and no execution attempt. The backup remains byte-for-byte unchanged through attempted migration and recovery. Unsupported primary bytes are retained, invalid backups never replace the primary, and failed migration leaves version 23 with no partial v24 marker. Explicit recovery archives the failed primary for inspection.

Rollback procedure:

1. Stop all GRID sessions before replacing a deployment. Keep the matching executable/package, database backup and required file-edit recovery artifacts together. Record their location/version; do not mix independently updated packages and data.

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

In a one-to-one conversation as Master Control, say `resume my work` (also `continue my work` or `show unfinished plans`). GRID lists saved, non-archived operator plans with unfinished steps across conversations. Say `resume plan 1` to select the numbered entry and review its current tasks/results, then `approve this plan` and `run this plan` when appropriate. Selection never approves or executes work. Completed, cancelled and denied-only plans are omitted.

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

Full verification passed 1,361 tests (14 Core, 157 Simulation, 99 Persistence, 1,091 Terminal) using `dotnet test --artifacts-path TestResults/resume-fix --no-restore --verbosity quiet`. The user's running GRID locked the normal executable, so verification used isolated outputs. Process-based persistence tests now locate either conventional or isolated build outputs. The running process was not stopped or replaced; close it normally and rebuild before retrying the updated normal executable.

## MCP executable testing follow-up - 2026-11-14

The bundled-model `--verify-mcp-workflow` run completed five of six checks. Local Qwen startup, multi-specialist draft, denial before approval, approved VOLT/RELAY execution, and SQLite evidence retention passed. Missing-workspace clarification failed with `Assistance refused: Invalid clarification response.` This remains an open model-response/clarification defect; do not mark MCP acceptance complete.

Retained source evidence: `C:\Users\jack\AppData\Local\Temp\Grid611Verification-33d634a8a46149569d026db4e52739e5\report.json`.

Interactive testing was blocked because the running GRID console was absent from the computer-control window inventory. Login and mouse/keyboard workflows were not verified. The file-operations attempt was interrupted; no completed Desktop HTML artifact or OneDrive synchronization was confirmed. These are limitations, not passing checks.
