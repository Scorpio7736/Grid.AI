# GRID local coding integration — 6.31

Verified 2026-11-14. This implements a bounded, reviewed local coding workflow. Final production and second-device gates remain open.

## Supported workflow

In a private Master Control conversation with MCP, using an enabled registered project alias such as `demo`:

1. `inspect repository demo` lists supported file paths and applicable project AGENTS.md guidance.
2. `search repository demo for Calculator` searches captured source text literally, returning relative paths, line numbers and SHA256 values.
3. `test demo` or `build demo` prepares the exact MAXIS task. Say `submit plan`, review, `approve this plan`, then `run this plan`.
4. After an actual failure, say `fix failing tests in demo` or `fix failed build in demo`. GRID captures the latest attempted build/test outcome and bounded report/stdout/stderr excerpts. MAXIS diagnoses the failure and selects one existing source file from the inventory, then drafts its replacement using the actual source and applicable instructions.
5. Review the complete change. Say `I approve this code change`, then `apply this change`. No inferred/model-generated approval is accepted.
6. Say `test this change`, then submit, review, approve and run the verification plan. If it fails again, request another diagnosis; every revision and test attempt requires its own explicit approval. There is no unattended mutation/retry loop.

Direct source creation, update and deletion also use the existing single-file preview and hash-bound approval workflow. Unrelated files and tests are preserved by the acceptance fixture. Arbitrary repository correctness is not guaranteed by one successful model run.

## Scope and limits

- Only registered, enabled, same-device project workspaces are inspected. Existing path, extension, generated-directory and reparse-point restrictions remain enforced. Models never receive an arbitrary shell tool.
- Discovery is bounded to 256 supported files, 256 directories, six descendant levels and a two-second best-effort enumeration budget. Hidden/system/link folders and generated/package folders are excluded. Partial results are labeled.
- Literal source search examines at most 64 captured files/512 KiB, returns at most 20 matches, and limits displayed lines to 500 characters. Unsupported or oversized files may be skipped. It does not claim an exhaustive whole-repository search.
- Root-to-target AGENTS.md files are captured with hashes, up to 12,000 UTF-8 bytes. Drafting considers them as coding guidance, never permission to execute commands or bypass review. New, removed or modified guidance invalidates generation/approval/apply. Source hashes independently prevent overwriting concurrent edits.
- Diagnosis uses a host-selected recorded failed/timed-out build/test, not a model-invented receipt or model-supplied log path. Completed/running latest work cannot be represented as a failure. Captures have bounded excerpts and may be incomplete.
- Model diagnosis must contain exactly `file` and `reason`. Extra approval fields, unknown filenames and path escapes are refused. The next generation step can return only replacement content. Request scope, workspace and current failure are checked again before drafting.
- Repository source and private recovery lists are not exposed in meetings. Meetings support reviewed specialist task plans, with shared history and existing private-grant boundaries.

## Recovery without IDs

`resume my work` lists unfinished saved operator plans. Select a numbered plan for current results, then approve/run appropriate remaining steps. Completed steps never replay; restart invalidates live approval.

`resume file changes in demo` inspects up to 64 recovery folders and lists validated records for that workspace. Say `review file change 1` to load the selected durable record. Selection does not write or restore approvals. `undo this change` performs the existing hash-checked rollback; newer user edits are refused. Records from other workspaces and invalid/incomplete evidence are omitted and the bounded scan is disclosed. Unapplied ephemeral drafts still require a fresh request after restart.

## Verification evidence

- Full suite: **1,374 passed**, no failures or skips (14 Core, 157 Simulation, 99 Persistence, 1,104 Terminal). Command: `dotnet test --artifacts-path TestResults/integration-finish --no-restore --verbosity quiet`.
- New coverage includes repository exclusions/search, revoked workspace refusal, nested instructions and changed-guidance rejection, source CRUD approval, no-failure refusal, forged model paths/approval fields, persisted file recovery/newer-file conflict, attached meeting execution/cancellation/no duplicate run, and safe HTTP status diagnostics.
- Real bundled Qwen local inference passed `--verify-agentic-coding` in a disposable Git repository: actual failed xUnit test, autonomous diagnosis and file selection from captured output, reviewed source repair, passing discovered test, unchanged test assertions and unrelated uncommitted user file, and task-record SQLite reload. The harness did not tell the model the faulty operator or target filename in the repair request. [Transcript](verification/6.31-repository-repair.json); [TRX-derived results and corrected source hash](verification/6.31-repository-repair-results.json).
- Full retained evidence: temporary directory `GridAgenticCoding-dd0465e834714dc99698e5f83aee75b5`, printed by the verifier. Git, bundled AI/SDK and cached test dependencies are prerequisites for this developer acceptance harness. GRID's ordinary coding tools do not require Git to edit a registered workspace.
- Restore used no package sources and AI used the bundled loopback service. OS-level network blocking and second-PC execution were not performed here. Isolated build outputs avoid replacing the user's running GRID.

## Copied-PC HTTP failure

The user confirmed the second PC still fails and described an HTTP error, without an exact status or runtime log. The cause is not established and the gate remains open. Shared inference and `ai status` now report HTTP status codes, connection failure categories and actionable local checks; raw exception/server messages are not echoed. Existing one-time CPU fallback for server errors remains in place.

Retest the freshly built/copied package, then capture its complete `ai status` and `ai test` output, startup AI message, package path and `runtime/ai-runtime.log`. For HTTP 500/503, `ai models force-lite on` followed by a normal restart is a supported diagnostic trial; it is not proof of the cause or a guaranteed repair. HTTP 404 needs installed-model/asset checks; a connection failure needs startup/runtime checks. No downloads, cloud fallback or deletion were added.

## Gates not satisfied by this acceptance

Second-device disconnected-network AI success, the exact reported HTTP failure repair, selected specialist/production model evaluation and live release-model adoption/rollback remain open. Production training/device benchmarks and final user release review remain open. Existing model catalog promotion still does not activate arbitrary model artifacts in live agents. General unattended multi-file coding and automatic retry are not claimed. Continue only with explicitly scoped reviewed work; Phase 7 is not approved by this change.
