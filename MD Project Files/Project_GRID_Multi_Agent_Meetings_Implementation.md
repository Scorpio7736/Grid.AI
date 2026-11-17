# Multi-agent meetings — 6.2A

**6.12 completed — 2026-10-29:** MCP can recommend/invite registered specialists with human confirmation, show participants, accept direct specialist planning requests and display recorded MCP progress/results in the same window. Membership changes start a fresh saved history segment; earlier transcripts and private context are not automatically shared. Decline, removal, scope changes, restart and attached-window workflows are tested. **1,031 automated tests and six bundled-model checks passed.** See [workflow, privacy boundary and verification](Project_GRID_MCP_Specialist_Invitations_Implementation.md). Earlier statements that invitations are planned are historical; 6.13–6.15 still block Phase 7.

**Planned before Phase 7 — 2026-10-27:** 6.12 lets MCP invite specialists into its current chat and lets the user direct them there. Membership must not automatically disclose private history/context; define confirmation, task ownership and revocation. Existing fixed meetings remain implemented history. [Plan](Project_GRID_Pre_Phase_7_Readiness_Plan.md).

**6.3A implemented — 2026-10-26:** `screen options/monitoring/ask/status/stop` provides one-shot selected-monitor assistance using offline Windows OCR and local AI. Human Master Control only; no recording, saved screen context or actions. **799 Release tests passed**, including attached-window and synthetic native-OCR checks. User desktop acceptance remains manual. [Commands, limits and verification](Project_GRID_Screen_Assistance_Implementation.md).

**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**Automatic conversation recall added — 2026-10-25:** Ordinary saved discussions and their relevant details now carry across conversations/restarts, alongside explicit memory. Private recall stays within the same agent/project; shared meeting recall requires the exact same group. `memory history` reviews candidates; `memory forget-history` excludes prior discussions while retaining explicit memories and transcripts. **728 tests passed**; bundled-model recall/clearing verified. [Behavior, bounds and test](Project_GRID_Automatic_Conversation_Recall.md). This supersedes earlier explicit-only recall descriptions.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


Implemented 2026-10-25. Schema v22 adds a saved meeting marker and validates one human plus 2–6 distinct registered agents. Existing two-person conversations remain unchanged; migration retains their history.

## Commands and acceptance test

Log in as Master Control, then run:

```text
grid -start
agent meetings MCP MAXIS JN
```

A dedicated conversation window opens. In that window:

```text
talk What should I check before building code?
talk @JN What should I consider before archiving files?
talk @MCP How should we plan the next tasks?
transcript
transcript --details
exit
```

Expected speakers: MAXIS, JN, then MCP. One agent responds per turn with the existing skippable typewriter effect. `typewriter off` disables animation. `exit` detaches and preserves an open meeting; use `conversations`, then `resume <conversation-id>` in the original terminal to reopen it. `disconnect` ends it. Parent logout revokes the attached window. Pausing Grid.AI blocks new replies; stale or simultaneous responses cannot overwrite a newer turn.

For reviewed offline voice, use `agent meetings MCP MAXIS JN -inline` in the original host, then the existing `voice listen`, `voice review`, `voice send` workflow. Dedicated windows remain typed interfaces; microphone capture stays in the original host. Voice depends on installed local Windows speech support, as before.

## Selection and limits

Names are case-insensitive registered roles: MCP, MAXIS, SENTINEL, JN, VOLT and RELAY. Duplicate, unknown, uninitialized and out-of-range participant lists fail before opening a window. `talk @MAXIS <message>` (also `@MAXIS: <message>`) overrides selection only when MAXIS belongs to this meeting; a missing question or nonparticipant is rejected without appending a turn.

Automatic selection counts distinct specialty keywords: coordination/planning/tasks for MCP; code/build/tests for MAXIS; security/firewall/threats for SENTINEL; files/storage/archive for JN; CPU/memory/performance for VOLT; notifications/delivery for RELAY. Highest score wins. Ties prefer MCP if tied, then alphabetical role. No matches use MCP if present, otherwise the first alphabetical role. This is deterministic keyword routing, not a semantic comparison of proposed AI answers. Unclear questions instruct the selected model to clarify. Unavailable local inference produces a labeled fallback and no invented answer. Detailed transcript sources include routing reasons.

## History, access and actions

Each accepted turn saves the human message and one labeled agent reply atomically. Meetings use their own shared transcript and bounded history summary. Private conversations, saved social memories and captured files are not automatically supplied to meeting prompts. Text the operator explicitly types into a meeting is shared with its participants. This conservative isolation preserves private scopes; selective sharing/import is not implemented here.

Only the authenticated human Master Control session creates meetings and requests replies. Selected agents can access the shared conversation through existing participant checks; nonparticipants cannot. Participation never grants another agent's capabilities, approvals or execution authority. AI text does not create or run tasks. Persistent cross-conversation user/project memory remains planned in 6.2B; AI task/context assistance remains planned in 6.3.

## Completion verification

Full solution: **697 passed, zero failed/skipped** (14 Core, 157 Simulation, 99 Persistence, 427 Terminal). Twenty-five meeting cases cover validation, specialty/tie/directed routing, shared ordering, private-history exclusion, unavailable AI, stale turns, pause/resume, authorization, reviewed voice, SQLite v21-to-v22 migration, malformed saved meetings and attached-window detach/resume/logout. After an additional null-participant validation guard, all 25 targeted meeting cases passed again. Tests use isolated worlds and do not alter the user's saved world.

The portable runtime verification now exercises automatic MAXIS selection and a directed JN turn with the actual bundled model, checking speaker identity, AI evidence, animation metadata and absence of tasks. Native window appearance and physical microphone/device acceptance remain manual checks.

**Portable verification completed 2026-10-25:** Release publish succeeded. `Test-GridPortable.ps1 -PackageRoot .\portable\GRID7736-standalone` passed with bundled Qwen3:4b and restricted global runtime paths. A general code question selected MAXIS; a directed archiving question selected JN. Both returned actual local-model responses with animation metadata, and no tasks were created. This checks runtime integration, not answer quality, clean-machine installation or firewall isolation. The user's saved world was never opened.
