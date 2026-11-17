# Project Grid.AI — Grounded agent conversations (6.2)

**AI response recovery — 2026-10-26:** Truncated answers now get one bounded larger retry, and rejected responses are distinguished from an offline service. Advice and analysis prompts were refined; MCP greetings no longer receive unrelated district counts. **759 Release tests passed**, and the bundled model answered the reported HTML question successfully. [Fix, limits and regression test](Project_GRID_AI_Response_Recovery.md).


**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**Automatic conversation recall added — 2026-10-25:** Ordinary saved discussions and their relevant details now carry across conversations/restarts, alongside explicit memory. Private recall stays within the same agent/project; shared meeting recall requires the exact same group. `memory history` reviews candidates; `memory forget-history` excludes prior discussions while retaining explicit memories and transcripts. **728 tests passed**; bundled-model recall/clearing verified. [Behavior, bounds and test](Project_GRID_Automatic_Conversation_Recall.md). This supersedes earlier explicit-only recall descriptions.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


## Conversation readability follow-up — implemented 2026-10-22

Normal chat and `transcript` now display spaced `You:` / agent-name replies. Saved snapshot facts, source labels, ticks, origins and IDs are hidden from the default display and remain accessible with `transcript --details` or `transcript CONVERSATION_ID --details`. Existing transcripts retain their full original contents; this is a display change with no schema migration. Human text is not parsed as an agent metadata envelope.

Conversation windows have a shorter greeting and `You> ` prompt. `whoami` remains available for identity/authority details. Typewriter animation and authorization checks remain intact. Chat still uses `talk <message>`; ordinary terminal commands are not silently interpreted as chat. AI-generated text is identified once on connection, and unavailable AI shows a brief basic-reply notice.

The model prompt now requests short, natural first-person conversation: greet greetings, introduce the agent simply, use facts only when relevant, and omit internal instructions/IDs/ticks/source boilerplate unless asked. Historical generated-message wrappers are removed from model history excerpts so real dialogue, rather than repeated metadata, supplies continuity. Full saved evidence is unchanged. Model wording remains variable; chat still does not execute tasks.

**Verification:** all 373 terminal tests passed, covering compact transcripts, full detail recovery, attached-window prompt/greeting, authorization and typewriter behavior. Release build succeeded with zero warnings/errors. An isolated real-model greeting returned a short natural response without the technical envelope in the normal display. Earlier full-suite totals remain historical.

```text
talk Hello MCP.
talk Who are you?
transcript
transcript --details
```

Restart the updated host terminal before testing; previously open windows use the running host's old code. To revisit an existing conversation, use `conversations` then `resume CONVERSATION_ID`.


**Implemented — 2026-10-22.** Typed `talk` and reviewed `voice send` now use the host's shared local inference service. Inline and dedicated conversation windows use the same implementation. Meetings remain planned in 6.2A; AI task proposals, captured-file retrieval and execution integration remain planned in 6.3. No schema change: v20 stores complete labeled replies in existing transcripts.

## Context and attribution

The engine captures an immutable, scoped context before inference. Master Control conversations receive snapshot tick, clock status, district/resident counts and the contacted resident's identity, kind, occupation and status. Program identities receive their own identity label, without global counts or the contacted agent's private memories. Facts are explicitly timestamped; simulation can continue while the model answers.

Only the selected participant-owned conversation is retrieved. Recent history includes up to six excerpted messages, labeled with sequence numbers and sender names. Older history uses a deterministic extractive summary: count/range plus up to two excerpts ranked by matching request words, with recency as a tie-breaker. This is lossy context compression, not an authoritative account or a new permanent memory. Full transcripts remain saved and accessible through `transcript`; no messages are pruned. After restart, context and summaries are rebuilt from the saved transcript.

For a human conversation with a resident that has opted into social simulation, up to two of that resident's latest saved social memories are included, labeled **SIMULATED experience**, with tick, outcome, peer and district IDs. Other residents' memories are not searched. Ordinary agents with no social records do not acquire invented experiences. No cross-conversation memory search, vector database, file ingestion, credentials, raw process lists, tasks/results or screen images are included.

Named profiles distinguish MCP (coordination), MAXIS (engineering), SENTINEL (security evidence), JN (archives/provenance), VOLT (resources) and RELAY (accurate messaging). Registered roles take precedence over names. Personality grants no permission. Other residents use an explicitly fictional character profile and do not impersonate host applications.

The stored response has separate sections:

- **OBSERVED FACTS**: engine-supplied snapshot information captured before inference.
- **AI SUGGESTION / FICTIONAL DIALOGUE — unverified**: model output; no action executed.
- **SOURCES supplied to model**: snapshot tick, selected conversation ID and any included social-memory labels.

Sources identify supplied evidence, not an independent verification of every generated claim. History and input are treated as untrusted; earlier AI replies never become authoritative observations. Prompt instructions and special-token escaping reduce confusion but do not guarantee factual correctness or eliminate prompt injection. The architectural boundary is text-only: no task proposal, approval, capability dispatch or file operation is reachable from generated text.

## Bounds and runtime behavior

Prompts are limited to 3,200 UTF-8 bytes, with at most 1,200 bytes of current user input for inference. Sections/excerpts are omitted when they do not fit. Oversized but otherwise valid user input uses deterministic fallback intact; it is not silently truncated. Existing 4,096-character message and world transcript capacities still apply. The backend retains its 4,096-token context, 512-token output budget, bounded HTTP/output handling, single active request, eight waiting slots and 120-second queue-inclusive deadline. Replies request at most 100 words; model compliance is not guaranteed. Stored model text is excerpted at 2,400 characters with an explicit excerpt marker if needed.

Live testing found that this installed Qwen template opens a thinking block even with `think=false`. The fixed-model adapter now supplies explicit Qwen role framing in raw mode and an empty completed thinking block. Caller special-token delimiters are neutralized before framing. Embedded/separate reasoning is still excluded, and incomplete/token-limited answers are rejected. This framing is specific to the configured Qwen model and must be reviewed if that model changes. No runtime/model replacement or download was required.

Unavailable, disabled, full, timed-out or invalid model responses use labeled deterministic dialogue. Cancelled/revoked requests store no incomplete pair. AI completion rechecks the session generation, active conversation, current participant/recipient, open status, message capacity and unchanged transcript length. Concurrent changes reject the stale response and ask the user to retry. A dispatched turn cannot be replayed. Input/reply commit atomically; the production callback saves afterward. A save failure is reported and the completed pair may remain in memory. No pending model job is persisted or replayed.

Dedicated windows now await asynchronous talk responses, with a 140-second transport allowance around the 120-second inference deadline. Host shutdown, parent/session revocation and observed client-process exit cancel pending work. Typewriter animation begins after the complete reply is stored; inline output remains instant. Confirmed microphone text follows the same path, retains Microphone origin and does not cause automatic speech playback. Other windows still share one queue and one authoritative engine.

## Verification

**641 tests passed:** 14 Core, 157 Simulation, 98 Persistence and 372 Terminal; zero failures/skips. Eighteen new cases cover profile selection, scoped history/memory retrieval, prompt bounds, fallback, revocation, stale transcript rejection, single-use dispatch, cancellation, voice origin, dedicated-window async rendering and SQLite persistence/reload. Existing adapter tests also verify raw framing and caller-token escaping. Release terminal build succeeded with zero warnings/errors. The full four suites ran serially under Verification; the terminal suite was rerun after formatting/prompt adjustments. After the final conservative byte-budget and same-length delimiter escaping adjustment, all 59 focused conversation/adapter/window tests passed and Release was rebuilt successfully.

The opt-in real probe now exercises `ai status/test` followed by an actual grounded MCP conversation in an in-memory world. It returned a model-written answer identifying the coordinator and correctly referring to the supplied paused clock, five fixture districts and 21 fixture residents, with source labels and no created tasks. These are test-world values, not the user's saved city's values. Initial live probes hit output limits and fell back; the final framing produced a completed answer. Runtime/model failure continues to be an expected fallback path.

```powershell
.\Start-GridLocalAI.ps1
dotnet run --project TestSupport/Grid.AI.Probe/Grid.AI.Probe.csproj -c Verification -- --local-ai
```

## Manual acceptance

Restart the updated terminal, log in as Master Control, then:

```text
agents initialize
ai status
connect MCP
```

In the opened conversation window:

```text
talk Who are you and what can you observe about GRID right now?
talk Suggest one useful next step. Do not execute anything.
transcript
typewriter off
talk What did I ask you earlier?
```

Expect separately labeled facts, unverified AI text and sources. The original terminal remains usable; `tasks list` should show no task created by these messages. Exit the conversation window to keep it open, then use `conversations` and `resume CONVERSATION_ID` in the original terminal to verify retained history. Use `disconnect` only when you want to end the conversation.

For the voice-compatible inline workflow use `connect MCP -inline`. The existing record/review/edit/send confirmation remains required. For deterministic fallback, launch Grid.AI with `GRID_AI_DISABLED=1` and repeat `talk status`; remove that temporary setting before normal use. Never launch a second owner against the same world database just to test.
