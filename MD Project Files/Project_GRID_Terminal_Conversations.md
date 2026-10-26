# Project GRID - Terminal Conversations (4.2)

**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**Automatic conversation recall added — 2026-10-25:** Ordinary saved discussions and their relevant details now carry across conversations/restarts, alongside explicit memory. Private recall stays within the same agent/project; shared meeting recall requires the exact same group. `memory history` reviews candidates; `memory forget-history` excludes prior discussions while retaining explicit memories and transcripts. **728 tests passed**; bundled-model recall/clearing verified. [Behavior, bounds and test](Project_GRID_Automatic_Conversation_Recall.md). This supersedes earlier explicit-only recall descriptions.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


## Agent lifecycle — 5.9 completed (2026-10-23)

`grid -start` initializes the six named agents on first use and resumes their activity thereafter. `grid -pause` blocks new agent replies, MCP coordination and task dispatch while retaining identities, memory, conversations and task history. `agents initialize` now gives migration guidance; connecting to MCP no longer creates it implicitly. `agents` reports whether activity is active or paused. Ordinary startup/reload remains paused; the explicit `--start` launch option takes the same initialization/save path after authenticated owner login.

In-flight dialogue is cancelled and cannot commit across a pause/resume generation. In-flight task attempts are cancelled cooperatively and recorded as Interrupted when the lifecycle changed; partial external effects may remain, so retry requires inspection and fresh approval. Queued tasks are not automatically dispatched on resume; pause retains existing queued approvals, while restart invalidates them through the existing recovery policy. Authenticated attached Master Control terminals can start/pause the shared host, including while its original terminal is busy. Transfer/NGE ownership restrictions remain unchanged.

**Verification:** 672 tests passed (14 Core, 157 Simulation, 99 Persistence, 402 Terminal), including eleven new lifecycle/window/startup cases. Existing historical fixtures now explicitly start their worlds before agent work. Schema remains v21. See [lifecycle contract and acceptance commands](Project_GRID_Agent_Lifecycle_Implementation.md). Earlier descriptions of independent initialization/lazy MCP or planned 5.9 coupling are historical and superseded.


## Fresh-clone runtime repair — 2026-10-23

Implemented tracked runtime preparation, self-contained Windows package building and isolated verification. GRID now owns a bundled AI server/model cache and uses bundled MAXIS tooling; no personal Ollama service is required. Typewriter now works with fallback replies and inline/attached chat, with explicit on/off overriding the startup preference. **648 tests passed**, plus a real portable AI/chat probe with global runtime paths removed. Full two-PC/voice/device/installer acceptance remains planned. See [setup, package layout, evidence and limits](Project_GRID_Fresh_Clone_and_Portable_Runtime.md). Earlier global-runtime instructions below are historical.


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


## Grounded agent conversations — 6.2 completed (2026-10-22)

`talk` and confirmed `voice send` now use shared local AI in inline and dedicated conversation windows. Replies separate timestamped engine facts, unverified AI suggestions/dialogue and supplied sources. Context includes scoped conversation excerpts, a bounded extractive history summary and permitted saved simulation memories; six named agents have distinct profiles. Session revocation/stale turns are rejected; unavailable AI falls back deterministically. No automatic task execution or captured-file ingestion. **641 tests passed**; a real local MCP conversation completed in an isolated test world. Schema remains v20. Meetings (6.2A) and AI task assistance (6.3) remain planned. See [implementation, limits and acceptance test](Project_GRID_Grounded_Conversations_Implementation.md). Earlier milestone statements below remain historical.


## Shared local AI — 6.1 completed (2026-10-22)

GRID now has a bounded shared local inference queue and a fixed-loopback Ollama adapter using installed Qwen3 4B. The dedicated server uses port 11435 with cloud disabled; the existing personal Ollama instance is unchanged. `ai status/test` provides original-host Master Control diagnostics and sampled CPU/memory/GPU allocation. **620 tests passed**; the real C# readiness probe returned GRID LOCAL READY in 3.232 seconds. Conversations remain deterministic until 6.2; no automatic task/tool execution, model context ingestion or schema change (v20). Runtime/model packaging inside Flash GRID remains planned; this installation uses the external Ollama cache. See [setup, licensing, limits and verification](Project_GRID_Shared_Local_AI_Implementation.md). Earlier milestone/schema/count statements below remain historical.


## Conversation windows — 4.2E delivered (2026-10-21)

Default `connect` and `resume` now open dedicated attached conversation windows. Agent replies animate with a skippable typewriter effect; `typewriter off` and `GRID_TYPEWRITER=off` provide instant text. Exit detaches without ending the saved conversation; parent revocation closes access. Explicit `connect MCP -inline` / `resume ID -inline` retains the original-host voice workflow and instant text. **572 tests passed** (14 Core, 157 Simulation, 97 Persistence, 304 Terminal). Schema remains v19; no AI/meeting implementation is implied. Multi-agent integration remains pending in 6.2A. See [implementation and acceptance test](Project_GRID_Conversation_Windows_Implementation.md). Earlier milestone statements below remain historical.


## VOLT and RELAY — 5.6 completed (2026-10-21)

VOLT now reports existing host telemetry and supports approved normal/eco/auto pacing of GRID's automatic simulation. Unknown/unsupported GPU data is preserved; Windows resources and manual tick behavior are unchanged. RELAY provides a saved local inbox, recorded-task briefings, human acknowledgements and explicitly approved offline speech with delivery/failure receipts. Schema v19 preserves mode and inbox history without automatic playback/replay. **551 tests passed** (14 Core, 157 Simulation, 97 Persistence, 283 Terminal). See [VOLT/RELAY implementation and acceptance test](Project_GRID_VOLT_RELAY_Implementation.md) for commands and boundaries. Next: **5.7 end-to-end verification**. Earlier milestone/schema/test statements below remain historical.


## JN storage — 5.5 completed (2026-10-21)

JN now inventories approved paths, reports possible duplicates, and prepares cleanup proposals without deleting source files. Defined GRID-managed storage is measured against an initial 20 GB decimal logical budget; MCP quota escalations require separate Master Control approval to change it. This is tracking/governance, not a hard write cap or disk reservation. Schema v18 preserves scope and quota history. **535 tests passed** (14 Core, 157 Simulation, 97 Persistence, 267 Terminal). See [JN implementation and acceptance test](Project_GRID_JN_Storage_Implementation.md) for limits, accounting roots, report retention and remaining planned automation. Next: **5.6 VOLT and RELAY**. Earlier milestone/schema/test statements below remain historical.


## SENTINEL security — 5.4 completed (2026-10-21)

SENTINEL now reads local Defender protection/scan timestamps, effective firewall profiles, and bounded Security-log metadata through individually approved tasks. Timestamped results explicitly distinguish real host observations, warnings and unavailable data from simulated incidents. No scans, setting changes, elevation or remediation are enabled. Schema v17 persists these results through the existing broker. **518 tests passed** (14 Core, 157 Simulation, 97 Persistence, 250 Terminal), including 13 new security cases and live read-only provider verification. See [SENTINEL implementation](Project_GRID_SENTINEL_Security_Implementation.md). Next: **5.5 JN — Storage and files**. Earlier milestone/schema/test statements below remain historical.


## MAXIS development — 5.3 completed (2026-10-21)

MAXIS now supports registered-workspace inspection, fixed .NET build/test execution, saved diagnostics and explicit replacement suggestions staged outside original source. Every task requires human approval; build/test executes trusted project code as the current Windows user (no OS/network sandbox). Schema v16 persists immutable, revocable workspace registrations. Other specialists remain demonstrations. **505 tests passed** (14 Core, 157 Simulation, 97 Persistence, 237 Terminal). See [MAXIS implementation](Project_GRID_MAXIS_Development_Implementation.md) for commands, offline prerequisites, artifact retention and limits. Next: **5.4 SENTINEL**. Earlier milestone/schema/test statements below remain historical.


## MCP administration — 5.2 completed (2026-10-21)

Explicit `mcp request` clauses now create atomic saved specialist task plans. MCP reports actual child states/results; each child requires human approval and original-host dispatch. Adapters remain demonstrations. Natural-language planning and real specialist actions remain planned. Schema v15 preserves plan links and existing cities. **485 tests passed**, including 13 new coordination cases. Next: **5.3 MAXIS — Development**. See [MCP coordination implementation](Project_GRID_MCP_Coordination_Implementation.md). Earlier milestone statements and schema/test counts below remain historical.


## Agent/task infrastructure - 5.1 completed (2026-10-21)

Explicit `agents initialize` registers MCP, MAXIS, SENTINEL, JN, VOLT and RELAY by stable ID. Schema v14 persists a bounded task queue, immutable proposals, one-attempt human approvals, results and audit. `tasks propose/inspect/approve/cancel/retry/run` enforce role-specific allowlists and live session authority at the service boundary. The broker saves a running record before invocation, prevents concurrent dispatch, supports cancellation/timeouts and requires fresh approval for retries/restart recovery. Agent sessions cannot self-approve or borrow capabilities. **Only labeled demonstration adapters are enabled; no real PC task, shell execution, natural-language delegation or autonomous producer is implemented.** Chat/voice messages remain conversation data.

**472 tests passed**, including 15 new task integration cases covering authorization, persistence barriers, cancellation/retry, deduplication, SQL rollback and restart/recovery. Existing v13 worlds migrate without regenerating the city or automatically creating agents. Next planned module: 5.2 MCP coordination. See [task broker implementation](Project_GRID_Task_Broker_Implementation.md) for commands, bounds and limits. Earlier milestone/schema/test counts below remain historical.


## Communications verification - 4.5 completed (2026-10-21)

All seven 4.5 checklist items have automated coverage. **455 tests passed** (14 Core, 155 Simulation, 97 Persistence, 189 Terminal), including three new end-to-end tests for authenticated conversation/context recovery through SQLite, malformed input without transcript mutation, and unrelated-agent privacy with logout revocation. Existing suites cover recipient routing, durable history/recovery, reviewed speech/typed-message equivalence, audio failure fallback and deterministic communication without AI. No production-code/schema change was needed. Physical microphone/playback/device-unplug checks remain manual; advanced voice and task-delegation features remain planned. See [verification matrix and limits](Project_GRID_Communications_Verification.md). Earlier milestone counts below are historical.


## Offline voice - 4.4 Windows MVP completed (2026-10-21)

The original interactive host supports `voice devices/microphone/language/listen/review/edit/send/cancel/mute/speak/profile`. Hold Space to record (20-second maximum), release to transcribe with an installed offline Windows recognizer, review/correct, then explicitly send. Typed and microphone messages use the same dialogue service; speech never executes terminal commands. Playback starts muted and occurs only on explicit request. Saved registered-Agent profiles select Windows voice/rate/volume. Escape interrupts; identity/conversation changes prevent stale sends. Raw audio stays in transient memory and is not saved. Missing hardware or language/voice assets leave typed chat available.

Profiles are an additive validated field in existing schema-v13 communication JSON; legacy JSON defaults to no profiles. Restart and recovery preserve profiles and sent microphone-origin messages, not drafts/audio/device selection/mute state. Windows System.Speech is the implemented provider; whisper.cpp/Piper, attached-terminal audio, output-device selection and advanced voice options remain planned. 448 tests passed (18 new voice cases); real offline provider inventory succeeded. Physical microphone/speaker operation still needs an interactive smoke check. Earlier voice-as-planned statements below describe prior milestones. See [offline voice implementation](Project_GRID_Offline_Voice_Implementation.md).


## Context convenience - 4.3A completed (2026-10-21)

`context add -search` now opens a local Windows multi-file picker (default: temporary, `Inbox`, owner only); optional retention/project/registered-Agent arguments are supported. Read/revoke/remove accept exact filenames or IDs. Multi-file `context read` and session-local `context select` support reading several captures together. Names must be unambiguous; reads recheck access and selections clear on identity change. Picker completion revalidates the session; batches validate before import and persistent batches save once. Main and attached terminals use the same host-owned context service, with no new schema or network requirement. Drag/drop and remote uploads remain unimplemented.

Completion verification: 430 tests passed, including 10 new 4.3A tests. Automated tests inject dialog results; visible Windows dialog interaction still needs manual verification. See the context implementation/help for command syntax and limits. Prior 4.3 notes below describe the original milestone.


## User-supplied context - 4.3 completed (2026-10-20)

Implemented explicit local `context add/list/read/revoke/remove` with project names, source SHA-256/provenance, temporary or persistent retention, and optional registered-Agent grants. Master Control selects files; agents only read granted captures. Strict UTF-8 text/source and DOCX main-body text are supported with bounded parsing. Schema v13 stores persistent captures transactionally; temporary bodies are excluded from save/backup. `talk context` reports availability without executing/ingesting file contents. **420 tests passed.** See [context implementation and limits](Project_GRID_User_Supplied_Context_Implementation.md). Earlier dated schema/status notes below retain their historical scope.


## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all GRID runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 4.2B (implemented), 8.6 and 8.7 (planned). See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

## Local login and identity completion - 4.2A (2026-10-20)

**Implemented and verified.** Start with `login master`: the first successful Windows-token login binds the world owner; later logins must match. Sessions expire after eight hours. Master Control can register existing Agent identities, switch roles, and preview/confirm one-time program edits. Prompts/help/commands are role-aware; agent chats are participant-scoped and global/admin controls are denied. Switching/logout clears active chats and previews. Schema v11 preserves owner/agent registration without restoring authentication.

**362 tests passed**, plus isolated Windows login/agent restriction/confirmed edit/restart smoke. See [Local Login and Identity Implementation](Project_GRID_Local_Login_and_Identity_Implementation.md) for commands, service authorization, audit and security limits. This authenticates the current Windows account, grants no OS elevation, and provides no independent agent passwords or real task service. NGE now requires Master Control; --start waits for login. Earlier dated pending-login/editing statements refer to prior milestones.

**Implemented and verified - 2026-10-20**

## Commands

```text
connect                         Start a new conversation with MCP.
connect <program-id|name>        Start a new directed conversation.
talk <message>                  Send a turn to the connected program.
conversations                   List this local operator's conversations.
transcript [conversation-id]     Read the active or selected transcript.
resume <conversation-id>        Reconnect to an open saved conversation.
disconnect                      End the active conversation; retain its transcript.
```

Example:

```text
connect
talk status
talk who are you
talk help
transcript
disconnect
```

Names with spaces are accepted by connect, as are full resident IDs. Use IDs for ambiguous names. `talk` consumes the rest of the parsed command; quote text containing apostrophes or literal quote characters according to the existing command parser (for example `talk "What's next?"`). Parsed tokens are joined with spaces, so this is not a byte-exact raw-input editor. Stored Unicode text is retained; transcript display replaces control characters with spaces and preserves ordinary Unicode. Quoting/unknown arguments, missing recipients and invalid conversation IDs produce helpful errors.

There is one active conversation per terminal instance. Ordinary commands such as status, step, live and exit continue working while connected; text without `talk` is still interpreted as a command. Starting/resuming another conversation requires ending the active one first. `disconnect` permanently ends that conversation; it is not a temporary detach. Normal exit leaves the conversation open so another launch can use conversations/resume. After restart the active selection is empty; open/ended transcripts and sequence numbers remain saved. Ended conversations can be read but not resumed.

## MCP and operator identity

The first `connect` (or `connect MCP`) creates a persistent Agent resident named MCP in the motherboard/Core district, with a stable world-derived UUID and occupation Local coordinator. In worlds without hardware geometry it uses the first district by stable ID. Existing uniquely named MCP agents are reused; conflicting non-agent names or ambiguous names fail without promoting/overwriting anyone. A reserved-ID collision also fails. Merely launching the terminal does not create MCP.

MCP is the early general coordination contact. It supplies deterministic local guidance and state summaries, not task delegation, an LLM, operating-system privileges or an autonomous job assignment. It has no automatic work routine or social opt-in. Other recipients use the same explicitly labeled dialogue adapter; host applications do not actually read or author these replies.

A Local operator human UUID is derived from the saved world UUID and a versioned namespace. It remains stable across restarts and differs across worlds. This is a convenience handle, **not login, authentication or Master Admin authority**. Conversation list/read/resume commands select this handle's conversations; this is not a security boundary against direct trusted API access. 4.2A login, role-specific access and identity switching remain planned.

## Deterministic replies and atomic turns

- `status` returns **[VERIFIED GRID STATE at tick N]** with clock state, elapsed simulation time and district/resident counts from one consistent snapshot.
- `who are you` (optionally with a question mark) returns labeled resident identity/kind/occupation and explicitly identifies the deterministic GRID adapter.
- `help` supplies **[SIMULATED DIALOGUE]** describing supported questions and current limitations.
- Other text receives **[SIMULATED DIALOGUE]** acknowledging storage and explicitly stating that no task was executed or delegated.

Matching trims surrounding whitespace and ignores case. Replies never interpret arbitrary terminal commands or claim successful host tasks. The verified label refers to GRID's current snapshot, not external facts or freshness of every host measurement.

Engine Talk stages both the human message and generated reply, validates the human participant and live program recipient, checks two free message slots and cancellation, then commits both under the world lock. A full transcript/cancelled turn stores neither half. Reply origin is SystemEvent; input origin is Terminal. IDs and timestamps are generated metadata, while reply content for the same query/state is deterministic. DialogueTurnStored history contains metadata, not message bodies; CoordinatorCreated records the initial resident creation.

## Persistence and scope

Existing schema v10 holds the unchanged conversation model; no migration is required for these commands. MCP persists as an Agent, conversations save with the world, and ordinary applications retain discovery-only population semantics. Autosave and orderly exit provide durability; unsaved turns can be lost on abrupt termination. Missing original program recipients block new turns while allowing transcript reads and human closure. Capacity remains 128 conversations, 2,048 total messages and 4,096 input characters; a dialogue turn consumes two messages. Transcript retention/deletion tools remain future work, so closed conversations still count toward limits.

## Completion verification

**353 tests passed:** 14 Core, 155 Simulation, 88 Persistence and 96 Terminal. Each project ran with `dotnet test <project>/<project>.csproj -c Verification --no-restore`, without build warnings. New coverage verifies default/directed conversations, deterministic and verified labels, normal commands during chat, cancellation/capacity atomicity, coordinator collision, no task execution, invalid commands, Unicode rendering, restored operator identity, open conversation continuation and ended-conversation rejection.

A real Verification terminal smoke used a fresh temporary world over three launches: connect/create MCP, talk status/help, save/exit, restart/resume, talk who are you, read/end, then reopen/read and reject resume of the ended conversation. The user's default save and running terminal were untouched. Rebuild/restart the normal launcher target to use these commands.

Still planned: 4.2A production login, authenticated admin/agent sessions, real task delegation, LLM-generated dialogue, voice/viewer adapters, richer intent recognition, group conversations and transcript retention UI. Earlier completion notes describing terminal chat/agent creation as deferred refer to earlier milestones; this milestone adds only the explicitly requested lazy MCP coordinator.
