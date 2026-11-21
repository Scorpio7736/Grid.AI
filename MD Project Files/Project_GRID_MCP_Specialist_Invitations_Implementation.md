# Project GRID — MCP Specialist Invitations (6.12)

Implemented and verified 2026-10-29. This completes 6.12; 6.13–6.15 remain required before Phase 7.

## User workflow

Start GRID and connect to MCP, inline or in an attached conversation window. Type these messages directly, or prefix them with `talk`:

```text
grid -start
connect MCP -inline
invite someone for code debugging
accept invitation
participants
@MAXIS explain how you would investigate a failing build
remove participant MAXIS
confirm removal
```

Use `invite MAXIS`, `invite JN`, `invite TRON`, `invite VOLT` or `invite RELAY` for an exact specialist. Recommendation uses registered capability purposes and role expertise; ambiguous requests ask for a name. The preview explains the role and sharing boundary. `decline invitation` leaves membership unchanged. MCP remains coordinator. Only human Master Control can confirm membership. A model reply cannot itself add someone.

## Direct work and MCP reports

With VOLT invited, `@VOLT plan a current resource observation using volt.status current only` prepares reviewed work. `talk submit plan` creates unapproved broker tasks. Inspect and approve the displayed task IDs, then run the displayed plan ID with `mcp run <plan-id>` in the original host terminal. `MCP progress`, `mcp plans` or `mcp brief <plan-id>` shows recorded progress/results in the conversation window. Read the preview: local AI may choose an unsuitable action or reject a request.

Explicit `plan`/`propose` requests enter task planning. Ordinary questions remain conversation. Invited specialists do not gain Master Control privileges or automatic execution. Task/edit approval remains separate from invitation confirmation. Original-host dispatch is still required in attached windows. Existing scoped file-edit review remains its own workflow; joining a group does not grant private files or make group-wide private-file editing available. Proposals mentioning absent specialists suggest invitations; accepting one clears the unsubmitted proposal, so request the plan again with the resulting group.

## History and context boundary

Confirmation continues in the same window with a **new conversation ID and empty shared-history segment**. The old segment ends and its transcript is retained. Earlier automatic recall for the resulting participant set/project is suppressed at the boundary, including when a specialist is removed then reinvited. New messages are shared with current members. No private file or memory grant changes, and shared task planning excludes captured private document excerpts. Explicit user text in the shared request is visible to the group.

Membership changes clear unsubmitted file/task drafts, follow-up selections and screen capture state. Invitation confirmation is bound to identity, conversation, message count, project, memory revision and agent activity. Changed scope or pause/resume requires a fresh preview. Unrelated input cancels a pending invitation; `participants` does not. Decline does not change membership. Confirmed membership persists in the world database; pending confirmations do not survive restart. Resume the latest segment, not the ended earlier ID. Removal does not retract information already shared or cancel independently approved tasks. Existing direct-chat memory rules resume when only MCP remains.

Existing schema v24 is sufficient. Each transition consumes a conversation slot; the existing 128-conversation limit is enforced. No unbounded history or new database schema is introduced.

## Completion verification

- Release solution suite: **1,031 passed, zero failed/skipped** (Core 14, Simulation 157, Persistence 99, Terminal 761).
- Nine new regression cases cover recommendation/decline, private-history exclusion, direct specialist replies, removal/reinvite, project/activity changes, restart, authority checks, shared planning without private uploads, and actual named-pipe attached-window planning/reporting.
- Bundled qwen3:4b: **six checks passed** against a disposable SQLite world: runtime availability, recommendation/decline, confirmed specialist reply, reviewed planning without automatic execution, approved VOLT execution with recorded MCP report, removal/save/reload.
- Run the opt-in packaged check with `Grid.Terminal.exe --verify-invitations`. It uses a synthetic world and writes its report under `%TEMP%/Grid612Verification-<id>/report.json`; it does not use the user's world. Source report for this completion: `Grid612Verification-e7b69fced5ac411a89e8606119190ce9/report.json`.
- Recorded telemetry can legitimately be unknown before sampling. Model conversation is not factual verification. These checks do not satisfy the separate second-device portability or role-model acceptance gates in 6.13–6.15.
