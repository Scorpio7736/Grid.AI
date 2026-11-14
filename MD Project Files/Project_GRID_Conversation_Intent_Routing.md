# Conversation-first intent and action routing — 6.32

Implemented 2026-11-14. This extends the existing reviewed file/task routes; model output never grants authority. See [local coding integration](Project_GRID_Local_Coding_Integration.md) for repository repair, file recovery and original-host execution.

## Implemented behavior

- Connected inline and attached conversations accept ordinary messages. Established deterministic requests retain their routes. Additional action wording (for example `put`, `prepare`, `organize`, `locate`, `update`, and `help me write`) can use local AI to produce a strict version-1 intent. This is bounded interpretation, not a guarantee of understanding every phrase.
- The intent contract accepts exactly version, kind, scope, target and text. Supported kinds are single-file create/update/delete/read, name search, specialist task planning, clarification and unsupported. Unknown versions, fields, types and kinds are refused. No command, execution or approval field exists.
- Interpretation receives the current request and its clarification only. It receives no private file contents or memories. Missing destinations or filenames prompt a clarification. The user reviews the interpretation before GRID resolves the destination or prepares a task/file preview. A different request discards the pending interpretation; cancel, disconnect, identity changes and expired context cannot authorize it later.
- `review this request` prepares the host-validated next step. File drafts show complete content and absolute target, then require existing hash-bound approval and separate apply. Read/name-search requests remain read-only. Explicit workspace grants, file-destination policy, traversal protections, original-host authority and current session checks still apply. Private file tools do not acquire meeting access.
- Task plans accept explicit version 1, with legacy versionless v1 producer compatibility. Existing one-to-eight-step specialist planning, agent assignment, immutable task review, human approval and separate run remain authoritative. Plan previews identify source records, effect category, targets and task state; model output is not completion evidence.
- `what can you do?` explains the available catalog, necessary registrations/context grants, approval steps and unsupported authority. Denials retain the underlying reason and do not silently acquire access.
- `review this plan`, then `retry this plan`, can return exact failed/timed-out/interrupted **read-only observations** to AwaitingApproval. A fresh approval and separate run are required. The allowlist is resource/status observations, security status/events, source inspection, and storage inventory/duplicate/status reads. Attempts remain bounded at three. Changed task records, revoked grants, archived records and audit limits refuse retry. Completed steps never rerun.
- Builds, tests, notifications and other potentially consequential operations cannot use this conversational retry shortcut. Review possible effects and prepare a fresh plan. Existing durable plan/file recovery remains available; restoration never silently replays an approval. Cancellation does not undo completed side effects; file undo remains explicit and hash checked.

## Quick check

After starting GRID and connecting to MCP, with a registered workspace named `demo`:

1. Ask `what can you do?`.
2. Ask `put a short greeting into welcome.txt in demo` (use a new filename).
3. Check the interpreted destination/target, then say `review this request`.
4. Review the full draft, say `approve this change`, then `apply this change`.
5. Ask `where is the file saved?` for the recorded receipt.

Local AI must be available for unfamiliar wording and generated content. A refusal, clarification or invalid model response creates no file/task execution. Real model accuracy varies; deterministic regression tests do not certify every model or device.

## Verification

Focused verification: 118 conversation/intent/assistance tests passed. Added coverage includes strict version rejection, forged approval fields, unknown capabilities, missing targets, clarification, reviewed interpretation, separate file approval, workspace traversal/absolute escape, revoked workspace, cancellation, read-only retry, partial completion and side-effect retry refusal. Existing tests cover prompt injection, stale context, attached windows, immutable plans, saved resumption and file conflicts.

Full solution verification: **1,393 passed, zero failed/skipped** (14 Core, 157 Simulation, 99 Persistence, 1,123 Terminal), using `dotnet test --artifacts-path TestResults/intent-routing -p:NuGetAudit=false -p:RestoreIgnoreFailedSources=true --verbosity quiet`. Tests use isolated outputs under `TestResults/intent-routing` because an existing GRID process may lock its usual executable.

## Deferred and separate work

On 2026-11-14 the user deferred copy/transfer and second-PC acceptance because the transfer device is not ready. Those checks remain open; this milestone does not certify portable AI or approve Phase 7. Device integration is next under 6.33. Arbitrary shell access, unattended multi-file edits and automatic retries are not implemented by 6.32.
