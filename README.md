# GRID7736

## Fresh clone: install a new GRID instance

From 64-bit PowerShell in the clone, run ` .\Setup-GridClone.ps1 -Download`, then `.\Start-GridClone.ps1`. The explicit installer downloads pinned dependencies, builds GRID and requires successful real-model conversation checks. The launcher uses `.grid-data/world.db`, with fresh local identity/history on first launch. Existing instances are not imported or erased. `grid -copyto` remains the existing-instance transfer path.

Use `-Plan` for a read-only setup preview. Offline setup and verification limits: [fresh clone installation](MD%20Project%20Files/Project_GRID_Fresh_Clone_Installation.md). PC2 end-to-end download acceptance remains pending. These instructions supersede the historical fresh-clone notes below.

**AI response recovery — 2026-10-26:** Truncated answers now get one bounded larger retry, and rejected responses are distinguished from an offline service. Advice and analysis prompts were refined; MCP greetings no longer receive unrelated district counts. **759 Release tests passed**, and the bundled model answered the reported HTML question successfully. [Fix, limits and regression test](MD%20Project%20Files/Project_GRID_AI_Response_Recovery.md).


**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](MD%20Project%20Files/Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**Automatic conversation recall added — 2026-10-25:** Ordinary saved discussions and their relevant details now carry across conversations/restarts, alongside explicit memory. Private recall stays within the same agent/project; shared meeting recall requires the exact same group. `memory history` reviews candidates; `memory forget-history` excludes prior discussions while retaining explicit memories and transcripts. **728 tests passed**; bundled-model recall/clearing verified. [Behavior, bounds and test](MD%20Project%20Files/Project_GRID_Automatic_Conversation_Recall.md). This supersedes earlier explicit-only recall descriptions.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](MD%20Project%20Files/Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](MD%20Project%20Files/Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


**5.9 completed (2026-10-23):** `grid -start` initializes/resumes agents; `grid -pause` stops new agent activity and interrupts in-flight work. Identities/history persist; no tasks are replayed or approved on resume. Normal startup is paused. 672 tests passed. [Lifecycle and test commands](MD%20Project%20Files/Project_GRID_Agent_Lifecycle_Implementation.md).

**4.2G completed (2026-10-23):** Use `grid -start` / `grid -pause`, `tasks -completed` / `tasks -list`, and `tasks deny <reference> "reason"`. Transfer roots and `-select` create a new GRID7736 child without overwriting existing files. Schema v21; 658 tests passed. [Commands and verification](MD%20Project%20Files/Project_GRID_Terminal_Controls_Implementation.md).

## Fresh-clone runtime repair — 2026-10-23

Implemented tracked runtime preparation, self-contained Windows package building and isolated verification. GRID now owns a bundled AI server/model cache and uses bundled MAXIS tooling; no personal Ollama service is required. Typewriter now works with fallback replies and inline/attached chat, with explicit on/off overriding the startup preference. **648 tests passed**, plus a real portable AI/chat probe with global runtime paths removed. Full two-PC/voice/device/installer acceptance remains planned. See [setup, package layout, evidence and limits](MD%20Project%20Files/Project_GRID_Fresh_Clone_and_Portable_Runtime.md). Earlier global-runtime instructions below are historical.

For a source checkout, run `Prepare-GridRuntime.ps1` once, then launch GRID normally. Build a prepared release with `Build-GridPortable.ps1`; users launch that package's `launch.cmd`. Runtime/model binaries are not committed to Git. A fresh clone is source, not the prepared release.


## Grounded agent conversations — 6.2 completed (2026-10-22)

`talk` and confirmed `voice send` now use shared local AI in inline and dedicated conversation windows. Replies separate timestamped engine facts, unverified AI suggestions/dialogue and supplied sources. Context includes scoped conversation excerpts, a bounded extractive history summary and permitted saved simulation memories; six named agents have distinct profiles. Session revocation/stale turns are rejected; unavailable AI falls back deterministically. No automatic task execution or captured-file ingestion. **641 tests passed**; a real local MCP conversation completed in an isolated test world. Schema remains v20. Meetings (6.2A) and AI task assistance (6.3) remain planned. See [implementation, limits and acceptance test](MD%20Project%20Files/Project_GRID_Grounded_Conversations_Implementation.md). Earlier milestone statements below remain historical.


## Shared local AI — 6.1 completed (2026-10-22)

GRID now has a bounded shared local inference queue and a fixed-loopback Ollama adapter using installed Qwen3 4B. The dedicated server uses port 11435 with cloud disabled; the existing personal Ollama instance is unchanged. `ai status/test` provides original-host Master Control diagnostics and sampled CPU/memory/GPU allocation. **620 tests passed**; the real C# readiness probe returned GRID LOCAL READY in 3.232 seconds. Conversations remain deterministic until 6.2; no automatic task/tool execution, model context ingestion or schema change (v20). Runtime/model packaging inside Flash GRID remains planned; this installation uses the external Ollama cache. See [setup, licensing, limits and verification](MD%20Project%20Files/Project_GRID_Shared_Local_AI_Implementation.md). Earlier milestone/schema/count statements below remain historical.


## Bulk tasks — 5.8 completed (2026-10-22)

Agent-filtered lists and `tasks approve/run/clear -all [-AGENT]` are implemented. Bulk mutations/dispatch require original-host Master Control; approval and clear require reviewed interactive confirmation. Batches capture fixed selections, revalidate state/authority and stop dispatch on the first unsuccessful attempt. Clear archives records and cancels pending work, retaining audit/results and MCP/RELAY links; active/unsettled work is refused. `tasks list -archived` reads retained history. **Schema v20; 603 tests passed** (14 Core, 157 Simulation, 97 Persistence, 335 Terminal). Archiving does not free the 256-task history capacity. See [commands, verification and limits](MD%20Project%20Files/Project_GRID_Bulk_Task_Implementation.md). Earlier milestone/schema/count statements below remain historical.


## Transfer command rename — 4.2F completed (2026-10-22)

Use `grid -copyto "location"` or `grid -transferto "location"` inside the GRID terminal. `copexist` and `tranexist` now only show migration guidance and never execute a transfer. Existing Master Control/original-host restrictions, preview, confirmation, final save and verified move/copy safeguards remain. **582 tests passed** (14 Core, 157 Simulation, 97 Persistence, 314 Terminal). No schema change or user-world relocation. See [transfer behavior and limits](MD%20Project%20Files/Project_GRID_Transfer_Implementation.md). Earlier command names and counts below describe historical milestones.


## Conversation windows — 4.2E delivered (2026-10-21)

Default `connect` and `resume` now open dedicated attached conversation windows. Agent replies animate with a skippable typewriter effect; `typewriter off` and `GRID_TYPEWRITER=off` provide instant text. Exit detaches without ending the saved conversation; parent revocation closes access. Explicit `connect MCP -inline` / `resume ID -inline` retains the original-host voice workflow and instant text. **572 tests passed** (14 Core, 157 Simulation, 97 Persistence, 304 Terminal). Schema remains v19; no AI/meeting implementation is implied. Multi-agent integration remains pending in 6.2A. See [implementation and acceptance test](MD%20Project%20Files/Project_GRID_Conversation_Windows_Implementation.md). Earlier milestone statements below remain historical.


## End-to-end task verification — 5.7 completed (2026-10-21)

MCP delegation, human approval, real MAXIS unit-test execution and RELAY recorded-result delivery are verified together, including actual passing/failing TRX evidence and SQLite reload. Nine new cases also cover all six agent roles, cancellation, durable-start recovery and duplicate dispatch prevention. **560 tests passed** (14 Core, 157 Simulation, 97 Persistence, 292 Terminal), zero failed/skipped. No runtime/schema change was needed; schema remains v19. See [verification matrix, boundaries and terminal test](MD%20Project%20Files/Project_GRID_End_to_End_Task_Verification.md). Next: **6.1 Shared local AI**. Earlier milestone counts and next-step statements below are historical.


## VOLT and RELAY — 5.6 completed (2026-10-21)

VOLT now reports existing host telemetry and supports approved normal/eco/auto pacing of GRID's automatic simulation. Unknown/unsupported GPU data is preserved; Windows resources and manual tick behavior are unchanged. RELAY provides a saved local inbox, recorded-task briefings, human acknowledgements and explicitly approved offline speech with delivery/failure receipts. Schema v19 preserves mode and inbox history without automatic playback/replay. **551 tests passed** (14 Core, 157 Simulation, 97 Persistence, 283 Terminal). See [VOLT/RELAY implementation and acceptance test](MD%20Project%20Files/Project_GRID_VOLT_RELAY_Implementation.md) for commands and boundaries. Next: **5.7 end-to-end verification**. Earlier milestone/schema/test statements below remain historical.


## JN storage — 5.5 completed (2026-10-21)

JN now inventories approved paths, reports possible duplicates, and prepares cleanup proposals without deleting source files. Defined GRID-managed storage is measured against an initial 20 GB decimal logical budget; MCP quota escalations require separate Master Control approval to change it. This is tracking/governance, not a hard write cap or disk reservation. Schema v18 preserves scope and quota history. **535 tests passed** (14 Core, 157 Simulation, 97 Persistence, 267 Terminal). See [JN implementation and acceptance test](MD%20Project%20Files/Project_GRID_JN_Storage_Implementation.md) for limits, accounting roots, report retention and remaining planned automation. Next: **5.6 VOLT and RELAY**. Earlier milestone/schema/test statements below remain historical.


## SENTINEL security — 5.4 completed (2026-10-21)

SENTINEL now reads local Defender protection/scan timestamps, effective firewall profiles, and bounded Security-log metadata through individually approved tasks. Timestamped results explicitly distinguish real host observations, warnings and unavailable data from simulated incidents. No scans, setting changes, elevation or remediation are enabled. Schema v17 persists these results through the existing broker. **518 tests passed** (14 Core, 157 Simulation, 97 Persistence, 250 Terminal), including 13 new security cases and live read-only provider verification. See [SENTINEL implementation](MD%20Project%20Files/Project_GRID_SENTINEL_Security_Implementation.md). Next: **5.5 JN — Storage and files**. Earlier milestone/schema/test statements below remain historical.


## MAXIS development — 5.3 completed (2026-10-21)

MAXIS now supports registered-workspace inspection, fixed .NET build/test execution, saved diagnostics and explicit replacement suggestions staged outside original source. Every task requires human approval; build/test executes trusted project code as the current Windows user (no OS/network sandbox). Schema v16 persists immutable, revocable workspace registrations. Other specialists remain demonstrations. **505 tests passed** (14 Core, 157 Simulation, 97 Persistence, 237 Terminal). See [MAXIS implementation](MD%20Project%20Files/Project_GRID_MAXIS_Development_Implementation.md) for commands, offline prerequisites, artifact retention and limits. Next: **5.4 SENTINEL**. Earlier milestone/schema/test statements below remain historical.


## MCP administration — 5.2 completed (2026-10-21)

Explicit `mcp request` clauses now create atomic saved specialist task plans. MCP reports actual child states/results; each child requires human approval and original-host dispatch. Adapters remain demonstrations. Natural-language planning and real specialist actions remain planned. Schema v15 preserves plan links and existing cities. **485 tests passed**, including 13 new coordination cases. Next: **5.3 MAXIS — Development**. See [MCP coordination implementation](MD%20Project%20Files/Project_GRID_MCP_Coordination_Implementation.md). Earlier milestone statements and schema/test counts below remain historical.


## Agent/task infrastructure - 5.1 completed (2026-10-21)

Explicit `agents initialize` registers MCP, MAXIS, SENTINEL, JN, VOLT and RELAY by stable ID. Schema v14 persists a bounded task queue, immutable proposals, one-attempt human approvals, results and audit. `tasks propose/inspect/approve/cancel/retry/run` enforce role-specific allowlists and live session authority at the service boundary. The broker saves a running record before invocation, prevents concurrent dispatch, supports cancellation/timeouts and requires fresh approval for retries/restart recovery. Agent sessions cannot self-approve or borrow capabilities. **Only labeled demonstration adapters are enabled; no real PC task, shell execution, natural-language delegation or autonomous producer is implemented.** Chat/voice messages remain conversation data.

**472 tests passed**, including 15 new task integration cases covering authorization, persistence barriers, cancellation/retry, deduplication, SQL rollback and restart/recovery. Existing v13 worlds migrate without regenerating the city or automatically creating agents. Next planned module: 5.2 MCP coordination. See [task broker implementation](MD%20Project%20Files/Project_GRID_Task_Broker_Implementation.md) for commands, bounds and limits. Earlier milestone/schema/test counts below remain historical.


## System-agent names updated - 2026-10-21

ARCHON is now **MCP** (coordination), FORGE is now **MAXIS** (development), and ARCHIVIST is now **JN** (storage/files). Current commands, examples, plans and checklist labels use the new names. These names do not grant privileges or implement the planned specialist services. The human **Master Control** role remains separate from MCP.

On world load, legacy-named Agent residents are renamed without changing IDs, registrations, context grants, voice-profile links or conversation ownership. Conversation participant labels update; historical messages/sender labels and event history remain intact. The coordinator retains its original identity-generation seed to avoid duplicate residents. Changes persist on the next normal save. A conflicting target name or exhausted revision skips the rename and records `AgentRenameSkipped`; resolve by ID rather than merging identities. Ordinary application/civilian names and the generic Archivist occupation are unchanged.

Verification: 457 tests passed (14 Core, 157 Simulation, 97 Persistence, 189 Terminal), including new legacy-name/idempotence and collision cases. Earlier completion dates and test counts remain historical evidence; documentation examples now use current names.


## Communications verification - 4.5 completed (2026-10-21)

All seven 4.5 checklist items have automated coverage. **455 tests passed** (14 Core, 155 Simulation, 97 Persistence, 189 Terminal), including three new end-to-end tests for authenticated conversation/context recovery through SQLite, malformed input without transcript mutation, and unrelated-agent privacy with logout revocation. Existing suites cover recipient routing, durable history/recovery, reviewed speech/typed-message equivalence, audio failure fallback and deterministic communication without AI. No production-code/schema change was needed. Physical microphone/playback/device-unplug checks remain manual; advanced voice and task-delegation features remain planned. See [verification matrix and limits](MD%20Project%20Files/Project_GRID_Communications_Verification.md). Earlier milestone counts below are historical.


## Voice transcription repair - 2026-10-21

A user-reported microphone transcription failure was reproduced with generated speech through the real C# helper connection: repeated synchronous `Recognize()` calls could reach an already-finished stream and throw `InvalidOperationException` (0x80131509), discarding recognized text. Recognition now uses `RecognizeAsync(Multiple)` and waits for `RecognizeCompleted`, gathering recognized phrases until completion. Speech/audio remain local and in memory. Explicit typed constructors avoid PowerShell overload ambiguity.

Failures now report a bounded stage, exception type and HRESULT rather than hiding the error or exposing raw helper stderr/audio. Capture reports duration and rejects recordings shorter than 0.25 seconds with a hold-Space hint. `voice 0` is a shortcut for `voice microphone 0`; no-draft hints recognize an already-connected conversation, and fallback advice is no longer duplicated.

**Verification:** 186 terminal tests pass, including four new real silent-PCM/generated-speech/invalid-audio and UI shortcut cases. The generated-speech case reproduced the failure before the completion-event fix and passes after it. No physical microphone recording or speaker playback was used; physical-device verification remains interactive. Earlier 448-test completion counts below are the original milestone's history.


## Offline voice - 4.4 Windows MVP completed (2026-10-21)

The original interactive host supports `voice devices/microphone/language/listen/review/edit/send/cancel/mute/speak/profile`. Hold Space to record (20-second maximum), release to transcribe with an installed offline Windows recognizer, review/correct, then explicitly send. Typed and microphone messages use the same dialogue service; speech never executes terminal commands. Playback starts muted and occurs only on explicit request. Saved registered-Agent profiles select Windows voice/rate/volume. Escape interrupts; identity/conversation changes prevent stale sends. Raw audio stays in transient memory and is not saved. Missing hardware or language/voice assets leave typed chat available.

Profiles are an additive validated field in existing schema-v13 communication JSON; legacy JSON defaults to no profiles. Restart and recovery preserve profiles and sent microphone-origin messages, not drafts/audio/device selection/mute state. Windows System.Speech is the implemented provider; whisper.cpp/Piper, attached-terminal audio, output-device selection and advanced voice options remain planned. 448 tests passed (18 new voice cases); real offline provider inventory succeeded. Physical microphone/speaker operation still needs an interactive smoke check. Earlier voice-as-planned statements below describe prior milestones. See [offline voice implementation](MD%20Project%20Files/Project_GRID_Offline_Voice_Implementation.md).


## Context convenience - 4.3A completed (2026-10-21)

`context add -search` now opens a local Windows multi-file picker (default: temporary, `Inbox`, owner only); optional retention/project/registered-Agent arguments are supported. Read/revoke/remove accept exact filenames or IDs. Multi-file `context read` and session-local `context select` support reading several captures together. Names must be unambiguous; reads recheck access and selections clear on identity change. Picker completion revalidates the session; batches validate before import and persistent batches save once. Main and attached terminals use the same host-owned context service, with no new schema or network requirement. Drag/drop and remote uploads remain unimplemented.

Completion verification: 430 tests passed, including 10 new 4.3A tests. Automated tests inject dialog results; visible Windows dialog interaction still needs manual verification. See the context implementation/help for command syntax and limits. Prior 4.3 notes below describe the original milestone.


## Local task context - 4.3 (2026-10-20)

Implemented `context add/list/read/revoke/remove` for explicit file captures, scoped Agent grants, project labels and temporary/persistent retention. Schema v13 and 420 passing tests. Read [context implementation](<MD Project Files/Project_GRID_User_Supplied_Context_Implementation.md>) for syntax, limits and privacy/backup behavior; AI ingestion/task execution remain future work.


## Shared terminal windows - 4.2C (2026-10-20)

Implemented `ntm` and `<command> -opninwin` with one authoritative host and scoped per-window contexts. Attached exit detaches; original-host exit disconnects clients before save. Lifecycle/credential commands remain host-only. 404 tests passed. See [window implementation](<MD Project Files/Project_GRID_Shared_Terminal_Windows_Implementation.md>) for commands, transport, authorization and verification limits.


## Installation editions and rendering - planned 2026-10-20

Installation will offer **PC GRID** (permanent machine installation, with advanced 3D options) or **Flash GRID** (mobile flash-drive package, with very simple lightweight 3D). The installer must explain the choice, validate the destination and retain the selected edition. Both share simulation/save/identity behavior and must run supported tasks offline after provisioning. Flash GRID keeps managed files with the portable package and requires no persistent host integration; graphics still run on the host PC. Quality differences must not regenerate or change the world.

See [edition details](MD%20Project%20Files/Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 7.6/8.6. Rendering budgets, packaging and migration remain planned; this addition changes documentation only.

## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](MD%20Project%20Files/Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all GRID runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](MD%20Project%20Files/Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 4.2B (implemented), 8.6 and 8.7 (planned). See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

## Local login and identity completion - 4.2A (2026-10-20)

**Implemented and verified.** Start with `login master`: the first successful Windows-token login binds the world owner; later logins must match. Sessions expire after eight hours. Master Control can register existing Agent identities, switch roles, and preview/confirm one-time program edits. Prompts/help/commands are role-aware; agent chats are participant-scoped and global/admin controls are denied. Switching/logout clears active chats and previews. Schema v11 preserves owner/agent registration without restoring authentication.

**362 tests passed**, plus isolated Windows login/agent restriction/confirmed edit/restart smoke. See [Local Login and Identity Implementation](MD%20Project%20Files/Project_GRID_Local_Login_and_Identity_Implementation.md) for commands, service authorization, audit and security limits. This authenticates the current Windows account, grants no OS elevation, and provides no independent agent passwords or real task service. NGE now requires Master Control; --start waits for login. Earlier dated pending-login/editing statements refer to prior milestones.

## Terminal conversations completion - 4.2 (2026-10-20)

**Implemented and verified.** Use connect (default MCP), talk, conversations, transcript, resume and disconnect. MCP is created on first explicit connection as a persistent coordinator Agent; a stable world-local operator handle supports reopening saved conversations. Replies to status/who are you are labeled verified GRID state; other replies are labeled deterministic simulated dialogue and claim no task execution/delegation. Input/reply pairs commit atomically and reuse schema v10.

**353 tests passed**, plus a three-launch isolated terminal save/resume/end smoke. See [Terminal Conversations](MD%20Project%20Files/Project_GRID_Terminal_Conversations.md) for syntax, identity/capacity limits and completion evidence. This local operator handle grants no authentication or admin authority; 4.2A login, real task delegation and LLM dialogue remain planned. Earlier dated pending-terminal-chat/agent-creation statements describe previous milestones.

## Communications foundation completion - 4.1 (2026-10-20)

**Implemented and verified.** Grid.Communication now defines recipient/message/conversation models and a local hub contract implemented by the simulation host. Exact name/full-ID routing, explicit participant identities, start/send/end/read operations, ordered bounded messages and schema v10 transactional transcript persistence are available. Conversations survive restart and backup recovery even when a historical application resident is absent; new sends require live original program identities.

**346 tests passed.** See [Communications Foundation](MD%20Project%20Files/Project_GRID_Communication_Foundation.md) for APIs, identity boundaries, bounds and recovery evidence. These trusted local APIs store messages without authenticating the caller, executing text, claiming external delivery or generating replies. Terminal chat is still 4.2; voice, real delegation, LLM dialogue and production login remain planned. Earlier dated pending-communication/schema statements describe previous milestones.

## Autonomy completion - 3.4 (2026-10-20)

**Implemented and verified.** The engine composes resource/shift/work/rest/travel and social decisions without an LLM. Equally ranked social choices now use a stable rank seeded by saved world identity, round and participant IDs; restore retains reproducibility without extra random state. Schema v9 gates this policy change. Preferences, affinity and world rules still take precedence.

**338 tests passed.** An accelerated one-hour simulation covered eight persistent agents, zero-to-512 fake application churn, resource scarcity/recovery, valid movement/events and concurrent snapshot readers. CPU, memory and latency measurements are recorded in [Autonomy and Verification](MD%20Project%20Files/Project_GRID_Autonomy_and_Verification.md). This is a simulated-hour test, not a real-time hour-long production soak. Host applications remain observation-only; automatic agent creation, admin configuration, real task execution and Phase 4 communications remain planned. Previous dated pending-autonomy/schema statements describe earlier milestones.

## Social civilization completion - 3.3 (2026-10-20)

**Implemented and verified.** Explicitly opted-in Agent/Special residents now have cooperative, competitive or reserved preferences; bounded relationships, memories and occupation history; and simulated district civic tokens with scarcity/recovery. Decisions run on simulation time, respect current work/travel and commit atomically with ticks. Host applications remain observation-only and receive no fictional social activity or resource consumption.

Schema v8 persists social state and partial rounds, upgrades existing worlds without inventing participants, and supports validated backup recovery. `social [id|name]` and program inspection provide read-only views. Trusted engine APIs configure social participation/pools; production login, terminal admin configuration, automatic agent creation, actual communication/delegation and Phase 3.4 sustained autonomy remain future work. **336 tests passed**, plus isolated terminal create/save/reopen. See [Social Civilization](MD%20Project%20Files/Project_GRID_Social_Civilization.md) for complete rules, limits and evidence. Earlier dated statements that social functionality is pending describe previous milestones.

## Movement and occupations completion - 3.2 (2026-10-20)

**Implemented and verified.** Saved city roads now support deterministic district travel for explicitly configured Agent/Special residents assigned to compatible capacity-one workstations. Five simulated seconds per edge; no work progress while traveling; shift/resource uncertainty freezes travel. Trusted assignment/release APIs preserve world ownership and revision checks. `jobs` and `activity` add read-only workstation and measured application views. Host applications acquire no fictional civic jobs.

Schema v7 preserves station assignments, remaining routes and edge timing in existing behavior JSON, validates them on restore and performs no offline catch-up. Older unassigned routines retain their 3.1 behavior. No agent population or interactive admin job configuration is added. **323 tests passed** in isolated Verification builds; disposable jobs/activity/save/reopen smoke passed. A running user Release session locked its binaries and was left untouched; rebuild the normal target after closing it to use the changes. See [Movement and Occupations](MD%20Project%20Files/Project_GRID_Movement_and_Occupations.md) for policy, limits and full completion evidence. Earlier statements that movement/workplaces are pending refer to previous milestones.

Use `jobs` for civic workstations and `activity` for measured host-application availability/working sets.

## Bare Grid7736 command support - 2026-10-20

Startup now also installs a managed `Grid7736.cmd` launcher in `%LOCALAPPDATA%\GRID\bin` and adds that directory to the Windows user PATH, preserving existing entries and avoiding duplicates. Type `Grid7736` directly in PowerShell or Command Prompt; arguments such as `--start` are forwarded. The persistent `Grid7736` environment variable remains supported. An existing unmanaged launcher is not overwritten. This extends the original variable-only 1.14 behavior.

Restart the parent terminal app to inherit the new PATH. For the currently open PowerShell session, refresh it without closing the window:

```powershell
$env:Path += ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
Grid7736
```

Verification: **89 terminal tests passed**. The bare command resolved to the installed launcher and successfully ran/exited with `--ephemeral --no-color`; no saved world was opened.

## Startup launcher environment variable - 1.14 (2026-10-20)

**Implemented and verified.** On Windows startup, GRID ensures a persistent **user-scope** environment variable named `Grid7736` points to the terminal `.exe`. A missing, empty or broken path is registered using the current build's apphost; an existing valid absolute path is preserved, including another installed build. The current GRID process receives the same value. Values are stored without shell quotes, so paths with spaces remain usable. If no executable is available or Windows denies the environment update, startup reports a warning and GRID continues. Other platforms skip Windows registration. No administrator elevation, shell-profile changes or automatic simulation start are required. The follow-up above also adds a user-PATH launcher.

PowerShell launch (from a fresh environment):

```powershell
& $env:Grid7736
# Optional: begin simulation immediately
& $env:Grid7736 --start
```

Command Prompt launch:

```bat
"%Grid7736%"
```

The environment variable is a path value; the managed PATH launcher above separately provides the bare `Grid7736` command. Already-open shells do not inherit changes from GRID. Restart the parent terminal/IDE, or sign out/in if necessary. To refresh only the current PowerShell shell without restarting:

```powershell
$env:Grid7736 = [Environment]::GetEnvironmentVariable('Grid7736', 'User')
```

A development-build path remains tied to that build's `.exe`; rebuild that target to pick up changes. Launching another build preserves a still-valid registered installation. If the target is removed, launching GRID directly registers the current available apphost again. This is a convenience launcher, not an installer or authentication mechanism.

**Verification:** 87 terminal tests passed, including missing/empty/broken values, paths with spaces, preservation of an existing installation and rejection of a dotnet-host launcher. An ephemeral Release launch registered the actual Windows user variable and verified that its executable exists. No saved world was opened or modified. Current verified value: `C:\Users\jack\source\repos\Grid7736\Grid.Terminal\bin\Release\net10.0\Grid.Terminal.exe`.

## NGE implementation - 1.13 (2026-10-19)

**Initial local-terminal NGE implemented.** Type `nge` in an interactive saved-world session, review the world identity and exact file list, then enter `y` to confirm or `n`/blank to cancel. Invalid answers reprompt; EOF cancels deletion and follows normal EOF shutdown; Ctrl+C cancels deletion and follows normal graceful shutdown. Arguments such as `nge y` are rejected. Redirected/unattended input and ephemeral sessions cannot delete saves. This is local-operator confirmation, **not authenticated admin enforcement**; human-admin-only access remains future login/authorization work.

Confirmed NGE stops/awaits scheduler, autosave and telemetry, serializes against in-flight saves, revalidates world/backup identity, then removes only the active database, rolling `.backup` and exact `-journal`, `-wal`, `-shm` paths. It retains the session lease through completion and refuses symbolic-link/junction targets. Historical legacy/damaged archives, staging artifacts of uncertain ownership, external copies, other worlds and PC applications remain; the warning explicitly states this scope. The harmless session `.lock` file may remain after release. Interrupted recovery must be resolved before NGE can be prepared.

A flushed `.nullifying` marker is created before deletion. Partial failure retains that marker, blocks startup/save/recovery, reports remaining files and exits nonzero without a final save. **There is no automatic retry/resume command yet:** inspect the marker and remaining exact world artifacts before explicit manual cleanup; never remove the marker merely to bypass an unresolved operation. Successful NGE removes the marker and exits with the GRID7736 artwork and `GRID NULLIFIED // EXISTENCE TERMINATED`, never a saved-world claim. Next normal startup follows existing hardware-city creation; it does not reuse a managed backup. Independent historical/manual copies remain outside this deletion scope.

Verification: **306 Release tests passed** (14 Core, 127 Simulation, 83 Persistence, 82 Terminal). New coverage checks cancellation/blank/invalid/EOF input, uppercase confirmation, redirected/ephemeral rejection, worker shutdown before deletion, no final save on success/failure, owned-file deletion, unrelated-file preservation, rejected forged plans, blocked future writes/recovery, new identity after reopen and a Windows locked-file failure retaining the marker. All deletion tests use freshly created disposable worlds; no user save was deleted. Existing hardware-city creation tests cover the startup path; no manual destructive run against the user's world was performed. Visual Studio still owns its post-exit footer.

## Closing artwork and default color - 2026-10-19

The farewell now uses the user's supplied large GRID7736 ASCII artwork and blank-line spacing. Successful persistent exits retain `Stopping GRID.`, `World saved.`, then the artwork and `WORLD SAVED // USER DISCONNECTED`. Error and ephemeral messages remain truthful. Consoles narrower than 78 columns use a compact GRID7736 farewell. Ordinary terminal output and input echo default to light blue (bright cyan / ConsoleColor.Cyan); status, warning and error accents remain. The prior shell foreground is restored on exit. `--no-color`, NO_COLOR, TERM=dumb and redirected output continue to disable application color changes. Terminal verification: **75 tests passed**; no claim of visual color matching across user console themes.

## Terminal commands and shutdown complete - 1.12 (2026-10-19)

Implemented grouped `help`, display-only `clear`, `programs -active`, `programs -possible`, `programs -sleeping`, and a GRID7736 ASCII farewell. Active selects fresh Running applications; sleeping selects fresh On call or Sleeping applications; possible includes current applications plus the saved known-application catalog. `ids` works with one filter. Unknown/stale observations are never classified as active or sleeping. Bare `programs` still includes agents and other residents.

**Schema v6** adds a separate ApplicationCatalog table (key, name, source, last catalog observation UTC). The catalog is captured with the simulation snapshot and saved atomically with world/history; backup/recovery and schema validation include it. Installed/observed metadata survives restart; ordinary residents, PID lifetimes, activity and resource readings are not restored. Last observation is catalog provenance, not proof of a launch or current installation. Entries missing from fresh discovery display Historical / unknown. Existing schema-v5-only statements below describe earlier milestones.

The ASCII banner appears after graceful shutdown and final save. Save/shutdown errors retain diagnostics and a nonzero exit code without a success message; ephemeral sessions explicitly say NOT SAVED. There is no keypress wait. Visual Studio can still append its own debug-console footer. Help uses portable ASCII separators and registered command metadata, including extensions; no-color/redirection behavior is preserved.

**Verification:** `dotnet test -c Release --no-restore`: **297 passed** (14 Core, 127 Simulation, 81 Persistence, 75 Terminal). Tests include filters/IDs/invalid flags/empty results, clear preserving the world and tick, successful/failed farewell, catalog save/reopen/recovery and v5-to-v6 migration. A disposable built-terminal run exercised all command groups, filters, redirected clear fallback and successful save/reopen of the same city. An ephemeral PTY run preserved tick1 through clear and displayed the NOT SAVED banner. This tool's PTY uses the clear fallback; physical screen clearing/color appearance and Visual Studio footer presentation were not visually verified in the user's console. No IDE settings or existing user worlds were modified.

Quick command examples:

```text
help
clear
programs -active
programs -sleeping
programs -possible
programs -possible ids
exit
```

## Latest population revision - 2026-10-19

The current population includes discoverable installed applications and current-session background processes. States are **Sleeping**, **On call**, **Running**, or **unknown** when evidence is insufficient. This supersedes earlier windowed-only/removal-on-exit descriptions below; those remain historical completion records. Ordinary application state is still rediscovered rather than persisted, and the saved city is reused. Full-machine coverage is not complete: see [Installed Application Availability](MD%20Project%20Files/Project_GRID_Installed_Application_Availability.md) for implemented scope, Windows inventory limits, remaining work and **290-test** verification.

## Terminal usability completion - 1.11 (2026-10-19)

Implemented aligned/wrapped tables for programs, districts, resources, behaviors and history. Narrow output falls back to labeled records; empty tables are explicit. `programs ids` and `districts ids` expose full identifiers; `resources details`, `history details` and `inspect` retain detailed provenance. Live view retains paging and resize handling, with prominent Esc/Q Back hints and a return-to-prompt message.

Colors supplement labels: green for running/available/work, yellow for unknown/stale/waiting/warnings, red for errors, cyan for navigation/table headings; default terminal color otherwise. Use `--no-color`, `NO_COLOR`, or `TERM=dumb` for monochrome. Redirected output suppresses application color changes. Compact tables sanitize controls and use ASCII-safe text; original names remain available in inspection.

Esc/Q return to commands; `exit` or Ctrl+C shuts down. Automated tests cover repeated live/prompt round trips, same-world and clock preservation, no save/dispose on Back, final save on exit, width20/60/120, long values, empty data, color policy and plain output. Existing tests cover resize, paging and high event volume. The actual PTY smoke used the single-frame fallback because this tool's terminal did not meet interactive redraw requirements; it returned to a Running prompt and advanced to tick5. Full-screen visual color/contrast assessment in the user's terminal remains a manual check, not claimed as verified. Combined suite: **290 tests passed**.

## Program behavior - 3.1 (2026-10-19)

Use `behaviors` to see last-tick needs, goals, priorities and simulated work units. Applications reflect observations; configured agents/special programs use sourced resource budgets and deterministic simulated work/rest schedules. Schema v5 preserves routine progress without offline advancement. Routine configuration is a trusted service API, not an unauthenticated terminal command. Movement and real task execution remain future work.

Verification: **278 Release tests passed**, build zero warnings/errors. See [Program Needs and Behavior](MD%20Project%20Files/Project_GRID_Program_Behavior.md) for details. Debug output was locked by running GRID/Visual Studio sessions; verification used Release without stopping them.

## Application population - 2.7 (2026-10-19)

Normal startup discovers current-session windowed executable groups and reconciles starts/exits every two seconds, including while paused. Applications are transient; Agent/Special residents persist. Multiple windows and same-executable helpers share one representative. Background-only services, cross-executable helpers and per-window identity are outside the initial policy.

Schema v4 omits ordinary application state from saves. Before production conversion, old fictional citizens are preserved in a unique validated `.legacy-<id>.db` archive whose path is printed. The city and clock remain intact. `--ephemeral` keeps the Genesis demo.

Use `programs`, `inspect program <id|name>`, `status` and `live` for resident type, observation state and interpreted condition. Unknown requirements show **unknown** energy; discovering an app never authorizes controlling it. See [Application Residents Implementation](MD%20Project%20Files/Project_GRID_Application_Residents_Implementation.md) for policy, migration and limitations.

Verification: **268 tests passed**, build zero warnings/errors, Windows disposable startup/reopen passed. Agent task services, login, rendering and additional app-specific telemetry remain future work.

## Implementation status - 2026-10-17

The current implementation record tracks the released Genesis baseline, post-release changes, verification evidence, and remaining work. The default launch is paused; use --start to begin automatic ticking immediately.

See [Current implementation and verification](MD%20Project%20Files/Project_GRID_Current_Implementation_Status.md).

A local-first digital civilization prototype built with .NET 10. New saved worlds create six hardware-informed districts and populate them from observed applications. The Genesis demo retains five districts and twenty fictional citizens. Both use the fixed-step engine and interactive terminal.

## Hardware-generated worlds - 2.6 (2026-10-19)

Schema v3 saves the creation profile, seed and actual district/street/building-lot layout. Existing worlds load without rediscovery or regeneration, including after hardware changes. Use `--world "C:\path\to\new-world.db"` with a new filename for a separate city; `--ephemeral` keeps the fixed Genesis demo. Corrupt or incomplete saves are not silently replaced. The layout remains headless data; 3D rendering remains planned. Application residents are now implemented in 2.7 above.

Verification: 250 tests passed and build completed with zero warnings/errors. See [Hardware City Generation](MD%20Project%20Files/Project_GRID_Hardware_City_Generation.md) for the full completion record.

## Run

Install the .NET 10 SDK, then from this repository:

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj
```

The world starts paused with a one-second tick duration. Normal launches discover host hardware and save a generated city once, then load the same world from `%LOCALAPPDATA%\GRID\world.db`. Autosave runs every 30 seconds; exit, EOF and Ctrl+C save after the scheduler stops. Restored worlds start paused unless `--start` is supplied, with no offline time advancement. Citizens initially remain Idle, so ticking advances simulation time without inventing activities or energy changes.

To start automatic ticks immediately:

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start
```

The application flag is case-insensitive. `--` passes it through dotnet run. `--no-build` may be added before `--` after a successful build. This starts the clock. Enter `live` at the prompt for continuously updating status and recent events.

## Commands

| Command | Behavior |
|---|---|
| `behaviors` | Show last-tick needs, goals, priorities and simulated work progress. |
| `help` | Show registered commands and usage. |
| `city` | Inspect the saved hardware-generated layout and discovery coverage; legacy worlds remain preserved. |
| `status` | Show clock state, tick count, simulation time, and population. |
| `programs` | List citizens, IDs, energy, status, and location. |
| `districts` | List districts, IDs, and populations. |
| `inspect program <id or name>` | Show a citizen's actual state. |
| `inspect district <id or name>` | Show description, population, resources, and residents. |
| `start` | Enable automatic ticks. |
| `pause` | Stop automatic advancement. |
| `live` | Live status and recent events; Q/Esc returns to commands without pausing. |
| `step` or `tick` | Advance exactly once while paused. |
| `exit` | Stop the scheduler, finish autosave, save the world, then exit. |

Commands and name lookups ignore case. Full GUIDs disambiguate duplicate names. Multiword names can be quoted or supplied as the remaining words:

```text
inspect district "The Core"
inspect program Citizen 001
step
status
start
pause
exit
```

End-of-input and Ctrl+C also stop the scheduler. Redirected input omits interactive prompts. No commands invoke a system shell.

## Extend the terminal

The terminal separates command registration, dispatch, and session lifecycle:

- `TerminalCommand` supplies a name, usage, description, and handler returning `CommandResult`.
- `CommandDispatcher.Register` adds commands and rejects duplicate names. `help` lists the registry automatically.
- `DefaultCommands.Create` wires the built-in commands to the engine.
- `TerminalHost` owns input/output and scheduler shutdown, accepting `TextReader` and `TextWriter` for tests.
- `GridSimulationEngine.GetSnapshot()` copies a consistent read-only view under the tick lock. Commands should use snapshots for reads rather than exposing mutable world entities.

For example, after creating the default dispatcher:

```csharp
commands.Register(new TerminalCommand(
    "about", "about", "Describe GRID.",
    args => new CommandResult(args.Count == 0 ? "GRID Genesis" : "Usage: about")));
```

New world-changing commands should use validated engine operations that serialize with ticks. The registry is an in-process extension point, not a runtime plugin loader. The host is intended for one process session; a blocked console read may remain on a background thread until input arrives or the process exits after cancellation.

## Verify

```powershell
dotnet test Grid7736.slnx
dotnet build Grid7736.slnx
```

Tests cover world ownership, clock behavior, engine updates and cancellation, terminal parsing and output, extension registration, snapshots, and session shutdown. See `MD Project Files/Project_GRID_Master_Development_Checklist.md` for completion evidence and remaining work. On 2026-10-17, all 117 tests passed: 8 Core, 85 Simulation (including 48 editing cases), and 24 Terminal.

## Administrative editing API (post-Genesis)

The engine now exposes PrepareEdit and ApplyEdit with typed values from Grid.Simulation.Editing. EditableFields provides field metadata and parsing. Previews are immutable, tied to the world/engine/session, and checked against the program revision. Multi-field edits commit atomically and return attributed old/new value receipts without advancing simulation time.

Administrative editing denies access by default. A trusted IProgramEditAuthorizer must supply a live authorization lease through commit; no production login provider or terminal edit command exists yet. Test authorization providers live only in TestSupport and are compiled only into test assemblies.

World-owned program setters now reject direct changes. Initialize programs before adding them to a world; use validated engine editing for later administrative changes. Ticks and district transfers update revisions so stale previews cannot overwrite newer state. Receipts are returned in memory; current edited values and revisions are saved by section 2.2; durable audit remains future work.

See the Program Editing Rules and Session Context and Authorization documents under MD Project Files for the contracts. Section 1.10 is implemented: see [Terminal Live View](MD%20Project%20Files/Project_GRID_Terminal_Live_View.md) for paging controls, event retention, console fallback and extension details. On 2026-10-18, all 140 tests passed and the solution built with zero warnings/errors.

## Persistence foundation (Phase 2.1)

Grid.Persistence now supplies a file-backed SQLite schema and transactional version migrations. `SqliteGridDatabase.Initialize()` explicitly creates schema; `OpenExisting()` validates/upgrades an initialized file and never creates a missing database. Tables cover worlds, districts, programs, district resources and simulation clock state. Foreign keys and constraints enforce stored relationships and basic invariants.

Section 2.2 now connects the terminal to transactional saves, validated restoration, periodic saves and explicit backup recovery. Durable event/audit writing remains section 2.3 and later. See [Persistence Architecture](MD%20Project%20Files/Project_GRID_Persistence_Architecture.md) for schema and extension details.

Verification on 2026-10-18: 171 solution tests passed (31 persistence cases); solution build succeeded with zero warnings/errors.

## Save and recovery (Phase 2.2)

Implemented and verified on 2026-10-18. Normal launches reuse `%LOCALAPPDATA%\GRID\world.db`, autosave every 30 seconds and save after the clock stops on exit, EOF or Ctrl+C. Restored worlds start paused unless `--start` is passed. World/program identities, revisions, memberships, resources and simulation time survive restart; powered-off time is never simulated.

- `--world <path>` selects a separate world file in an existing folder.
- `--recover` explicitly restores that file's validated `.backup`, preserving damaged files. Use it with the same `--world` path, or alone for the default world.
- `--ephemeral` runs a disposable in-memory session; it cannot be combined with `--world` or `--recover`.
- A second process cannot open the same world while the first holds its session lease.
- Invalid saves stop startup rather than generating a replacement. Abrupt termination can lose changes since the last committed snapshot.

Verification: **200 tests passed** (8 Core, 93 Simulation, 50 Terminal, 49 Persistence), with a successful real-terminal create/reopen/corruption/recovery smoke test using an isolated temporary database. See [Persistence Architecture](MD%20Project%20Files/Project_GRID_Persistence_Architecture.md) for recovery behavior and test limits. Durable event history (2.3), hardware-generated cities (2.6), login and Windows service integration remain planned.

## Durable history (2.3, implemented 2026-10-18)

Normal persistent sessions now save meaningful events with world snapshots. Use `history`, `history kind=Transfer limit=20`, or `history severity=Warning` to query committed history. Filters also include `entity=<guid>`, `since=<round-trip timestamp>`, and `offset=<number>`. Pending events appear after autosave; `--ephemeral` has no saved history command.

Clock transitions, transfers, energy/resource changes and changing edits have timestamps, entity references, severity and structured context. Routine ticks are excluded. A bounded pending journal reports overflow; abrupt termination can lose events since the last save. This is not an exhaustive security audit. Schema v2 upgrades v1 without editing its migration. The application-resident retrofit remains planned.

Verification: **208 tests passed**, zero build warnings/errors; isolated terminal history across restart passed. See [History and Observability](MD%20Project%20Files/Project_GRID_History_and_Observability.md).

## Host resources (2.4 foundation, 2026-10-19)

Use `resources` to inspect read-only CPU headroom, RAM, Windows system-drive space and per-interface network counters. `live` includes a compact CPU/RAM summary. Samples update every two seconds even while simulation time is paused; values expire after ten seconds. GPU/VRAM and disk throughput currently report Unsupported.

A tested resource-condition policy compares explicitly sourced additional-work requirements with available headroom. Application-specific energy remains unknown until the 2.7 application discovery/classification work supplies those bindings and requirements. Existing fictional Genesis citizens still use their old energy rules. No resource observation changes Windows allocations or starts/stops applications.

Verification: **223 tests passed**, build zero warnings/errors, and a real Windows disposable-session smoke passed. See [Resources and Host State](MD%20Project%20Files/Project_GRID_Resources_and_Host_State.md) for units, freshness, formula and integration work still pending.

## Phase 2 verification (2.5, completed 2026-10-19)

**231 tests passed**, with zero build warnings/errors. Added isolated tests for terminal save/reopen and recovery, populated schema migration, no offline catch-up, invalid/missing database preservation, and SQLite recovery after terminating a writer with uncommitted changes. Tests never open the default user world and require no viewer or AI service. The crash helper is test-only.

See [Phase 2 Verification](MD%20Project%20Files/Project_GRID_Phase_2_Verification.md) for evidence and limits. Physical power-loss coverage is not claimed. The 2.7 application-resident retrofit and dependent energy binding, and 2.6 hardware-generated cities, remain planned.
