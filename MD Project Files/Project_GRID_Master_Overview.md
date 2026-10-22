# Project GRID — Master Overview

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


## Installation editions and rendering - planned 2026-10-20

Installation will offer **PC GRID** (permanent machine installation, with advanced 3D options) or **Flash GRID** (mobile flash-drive package, with very simple lightweight 3D). The installer must explain the choice, validate the destination and retain the selected edition. Both share simulation/save/identity behavior and must run supported tasks offline after provisioning. Flash GRID keeps managed files with the portable package and requires no persistent host integration; graphics still run on the host PC. Quality differences must not regenerate or change the world.

See [edition details](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 7.6/8.6. Rendering budgets, packaging and migration remain planned; this addition changes documentation only.

## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all GRID runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 4.2B (implemented), 8.6 and 8.7 (planned). See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

## Local login and identity completion - 4.2A (2026-10-20)

**Implemented and verified.** Start with `login master`: the first successful Windows-token login binds the world owner; later logins must match. Sessions expire after eight hours. Master Control can register existing Agent identities, switch roles, and preview/confirm one-time program edits. Prompts/help/commands are role-aware; agent chats are participant-scoped and global/admin controls are denied. Switching/logout clears active chats and previews. Schema v11 preserves owner/agent registration without restoring authentication.

**362 tests passed**, plus isolated Windows login/agent restriction/confirmed edit/restart smoke. See [Local Login and Identity Implementation](Project_GRID_Local_Login_and_Identity_Implementation.md) for commands, service authorization, audit and security limits. This authenticates the current Windows account, grants no OS elevation, and provides no independent agent passwords or real task service. NGE now requires Master Control; --start waits for login. Earlier dated pending-login/editing statements refer to prior milestones.

## Terminal conversations completion - 4.2 (2026-10-20)

**Implemented and verified.** Use connect (default MCP), talk, conversations, transcript, resume and disconnect. MCP is created on first explicit connection as a persistent coordinator Agent; a stable world-local operator handle supports reopening saved conversations. Replies to status/who are you are labeled verified GRID state; other replies are labeled deterministic simulated dialogue and claim no task execution/delegation. Input/reply pairs commit atomically and reuse schema v10.

**353 tests passed**, plus a three-launch isolated terminal save/resume/end smoke. See [Terminal Conversations](Project_GRID_Terminal_Conversations.md) for syntax, identity/capacity limits and completion evidence. This local operator handle grants no authentication or admin authority; 4.2A login, real task delegation and LLM dialogue remain planned. Earlier dated pending-terminal-chat/agent-creation statements describe previous milestones.

## Communications foundation completion - 4.1 (2026-10-20)

**Implemented and verified.** Grid.Communication now defines recipient/message/conversation models and a local hub contract implemented by the simulation host. Exact name/full-ID routing, explicit participant identities, start/send/end/read operations, ordered bounded messages and schema v10 transactional transcript persistence are available. Conversations survive restart and backup recovery even when a historical application resident is absent; new sends require live original program identities.

**346 tests passed.** See [Communications Foundation](Project_GRID_Communication_Foundation.md) for APIs, identity boundaries, bounds and recovery evidence. These trusted local APIs store messages without authenticating the caller, executing text, claiming external delivery or generating replies. Terminal chat is still 4.2; voice, real delegation, LLM dialogue and production login remain planned. Earlier dated pending-communication/schema statements describe previous milestones.

## Autonomy completion - 3.4 (2026-10-20)

**Implemented and verified.** The engine composes resource/shift/work/rest/travel and social decisions without an LLM. Equally ranked social choices now use a stable rank seeded by saved world identity, round and participant IDs; restore retains reproducibility without extra random state. Schema v9 gates this policy change. Preferences, affinity and world rules still take precedence.

**338 tests passed.** An accelerated one-hour simulation covered eight persistent agents, zero-to-512 fake application churn, resource scarcity/recovery, valid movement/events and concurrent snapshot readers. CPU, memory and latency measurements are recorded in [Autonomy and Verification](Project_GRID_Autonomy_and_Verification.md). This is a simulated-hour test, not a real-time hour-long production soak. Host applications remain observation-only; automatic agent creation, admin configuration, real task execution and Phase 4 communications remain planned. Previous dated pending-autonomy/schema statements describe earlier milestones.

## Social civilization completion - 3.3 (2026-10-20)

**Implemented and verified.** Explicitly opted-in Agent/Special residents now have cooperative, competitive or reserved preferences; bounded relationships, memories and occupation history; and simulated district civic tokens with scarcity/recovery. Decisions run on simulation time, respect current work/travel and commit atomically with ticks. Host applications remain observation-only and receive no fictional social activity or resource consumption.

Schema v8 persists social state and partial rounds, upgrades existing worlds without inventing participants, and supports validated backup recovery. `social [id|name]` and program inspection provide read-only views. Trusted engine APIs configure social participation/pools; production login, terminal admin configuration, automatic agent creation, actual communication/delegation and Phase 3.4 sustained autonomy remain future work. **336 tests passed**, plus isolated terminal create/save/reopen. See [Social Civilization](Project_GRID_Social_Civilization.md) for complete rules, limits and evidence. Earlier dated statements that social functionality is pending describe previous milestones.

## Movement and occupations completion - 3.2 (2026-10-20)

**Implemented and verified.** Saved city roads now support deterministic district travel for explicitly configured Agent/Special residents assigned to compatible capacity-one workstations. Five simulated seconds per edge; no work progress while traveling; shift/resource uncertainty freezes travel. Trusted assignment/release APIs preserve world ownership and revision checks. `jobs` and `activity` add read-only workstation and measured application views. Host applications acquire no fictional civic jobs.

Schema v7 preserves station assignments, remaining routes and edge timing in existing behavior JSON, validates them on restore and performs no offline catch-up. Older unassigned routines retain their 3.1 behavior. No agent population or interactive admin job configuration is added. **323 tests passed** in isolated Verification builds; disposable jobs/activity/save/reopen smoke passed. A running user Release session locked its binaries and was left untouched; rebuild the normal target after closing it to use the changes. See [Movement and Occupations](Project_GRID_Movement_and_Occupations.md) for policy, limits and full completion evidence. Earlier statements that movement/workplaces are pending refer to previous milestones.

## Latest population revision - 2026-10-19

The current population includes discoverable installed applications and current-session background processes. States are **Sleeping**, **On call**, **Running**, or **unknown** when evidence is insufficient. This supersedes earlier windowed-only/removal-on-exit descriptions below; those remain historical completion records. Ordinary application state is still rediscovered rather than persisted, and the saved city is reused. Full-machine coverage is not complete: see [Installed Application Availability](Project_GRID_Installed_Application_Availability.md) for implemented scope, Windows inventory limits, remaining work and **290-test** verification.

## Program behavior completion - 2026-10-19

**3.1 implemented and verified.** Non-demo residents now have deterministic needs/goals/actions. Applications reflect fresh or unknown observations; explicitly configured Agent/Special residents perform resource-gated simulated occupation work/rest on simulation-time shifts. Schema v5 persists their routines, sourced budgets and progress without restoring live observations or adding offline work. Use behaviors and inspect for last-tick behavior. No OS task execution, movement or named agent services are claimed.

See [Program Needs and Behavior](Project_GRID_Program_Behavior.md) for policy, APIs, limits and completion evidence: **278 Release tests passed**, build zero warnings/errors, disposable terminal smoke passed. Debug output was locked by running user sessions, which were left running. Earlier dated deferred-behavior/schema statements describe previous milestones; existing history and completed scope are preserved.

## Application resident completion - 2026-10-19

**2.7 / 2.7.1 implemented and verified:** production population now follows current-session windowed executable groups through read-only discovery and serialized lifecycle reconciliation. Application residents are transient; Agent/Special residents persist in schema v4. Before production conversion, legacy fictional citizens are preserved in a uniquely named validated archive. City/layout and clock remain intact. Unknown resource requirements display unknown condition; no host-control authority is implied.

Verification: **268 tests passed**, build zero warnings/errors, and a disposable Windows startup/reopen detected nine application groups. See [Application Residents Implementation](Project_GRID_Application_Residents_Implementation.md) for grouping limits, discovery/freshness, storage/migration, editing, resource interpretation and completion evidence. Earlier dated statements that 2.7 is planned, production cities stay empty, or all residents persist describe previous milestones. Agent delegation/services, login, rendering and additional per-application telemetry remain future work.

## Hardware city completion - 2026-10-19

**2.6 / GRID-REQ-004 implemented and verified.** First-time creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates bounded district/street/building-lot geometry and saves actual layout and provenance in schema v3. Later startups reuse the saved world without discovery or regeneration. Existing worlds are preserved; new generated cities start empty pending 2.7. Inventory is creation provenance, separate from live resource telemetry and application identity. Rendering, atmosphere, fashion and application-resident integration remain planned.

Verification: **250 tests passed** (14 Core, 106 Simulation, 74 Persistence, 56 Terminal); build had zero warnings/errors. A fresh Windows city and unchanged reopen were verified in a temporary directory. Use city to inspect saved generation details; --world with a new filename explicitly creates a separate world.

See [Hardware City Generation](Project_GRID_Hardware_City_Generation.md) for architecture, mappings, fallbacks, persistence and completion evidence. Earlier dated statements that hardware generation is planned or that normal creation always makes twenty fictional citizens describe previous milestones; this update supersedes those status statements while preserving their history.

## Resource/host foundation update - 2026-10-19

Section 2.4 now provides read-only host observations, explicit units/sources/freshness, fake-provider tests and a provenance-required additional-work resource-condition policy. Use resources for details or live for a compact CPU/RAM summary. Unsupported measurements and unknown application requirements remain unknown. Current observations are transient and cannot be edited into Windows allocations or restored from a save as fresh data.

Verification: 223 tests passed, build zero warnings/errors, and real Windows disposable-session sampling passed while simulation time stayed paused. See [Resources and Host State](Project_GRID_Resources_and_Host_State.md) for collector coverage, the exact policy and limitations. Application discovery/classification, per-application resource attribution and replacing application energy drain remain dependent on 2.7. Existing fictional Genesis citizens retain their old rules. Earlier deferred-telemetry statements below describe prior milestones, not this update.


## History implementation update - 2026-10-18

Section 2.3 now persists meaningful committed changes with world snapshots in schema v2. The history command supports entity/kind/severity/time filters and bounded paging. Routine Tick records are excluded; pending-buffer overflow is reported explicitly. Changing edits include attribution/context, but denied attempts and no-ops are not a comprehensive security audit. Structured operational errors are emitted to terminal output. See [History and Observability](Project_GRID_History_and_Observability.md) for retention, failure and API details.

Verification: 208 tests passed; solution build had zero warnings/errors; isolated terminal restart/query smoke passed. Earlier schema-v1 and deferred-history statements below describe the prior milestones. Live-view history remains process-local; the new saved-history path is separate. Application discovery, resource interpretation, resident classification and persistence filtering remain planned in 2.4/2.7.


## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

Ordinary inhabitants will represent running PC applications and leave after confirmed exit. Agents and explicitly designated special programs remain persistent. Resources will reflect application usage/requirements and host conditions; the existing fictional-citizen implementation remains the historical baseline.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


## GRID-REQ-004 - Hardware-generated city (planned, 2026-10-18)

On first-time world creation, GRID must discover the host computer's motherboard, CPU, GPU, RAM, storage and network hardware, then procedurally generate a unique city using those components. Save the resulting world and reuse it on subsequent startups; do not regenerate the city every launch.

Hardware informs the city's initial structure, while C# remains authoritative for world state. Generation runs without Godot, AI or cloud access. The viewer later presents the saved city using the established Digital European Classicism and Eternal Autumn Evening direction. Component-to-district mappings are proposed design choices, not implemented behavior or a literal reconstruction of a circuit board.

Existing saves take precedence over current hardware discovery. Hardware upgrades or changed enumeration must not silently replace an established civilization. A deliberate new-world operation is separate from loading, and corrupt/unreadable saves require recovery rather than automatic replacement.

**Current boundary:** Genesis uses its fixed five-district/twenty-citizen factory only for first creation. Sections 2.1-2.2 implement SQLite schema/migrations, transactional saves, validated restoration and backup recovery; later launches reuse the saved world. Hardware discovery and procedural city generation remain planned in 2.6. Preserve the completed Genesis release and editing API history.

Plan discovery, generated world data and durable first creation in Phase 2, then rendering in Phase 7 and startup integration in Phase 8. See [the generation contract](Project_GRID_Design_Specification.md#grid-req-004---hardware-generated-city-contract) and checklist section 2.6.

## Implementation status - 2026-10-17

Genesis world management, clock, engine and terminal are implemented. Post-release work adds the validated editing API and optional --start startup. Section 1.10 live view and bounded recent events were implemented on 2026-10-18. Login as Master Control or an agent, terminal editing controls, autonomous jobs, host agents, AI, voice and 3D remain planned. The vision below describes the target product; use the linked implementation record for current behavior.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Project:** GRID — Hybrid Digital Civilization and Autonomous PC Agent System  
**Document type:** High-level project overview  
**Version:** 1.2 (Genesis and post-release implementation reconciliation)
**Status:** Genesis and editing API implemented; broader product vision remains planned
**Source:** The six GRID design documents provided with this overview; expanded with a proposed user-directed task assistance goal

---

## 1. Project vision

**GRID** is a TRON-inspired digital civilization that exists locally on a Windows computer. It is simultaneously a simulated society—with districts, inhabitants, jobs, relationships, resources, and history—and an interface for useful computer tasks carried out by carefully permissioned system programs.

Three defining principles:

1. **Autonomous life:** Programs act, work, interact, and develop their own histories rather than waiting for every action to be scripted by the User.
2. **Host-connected world:** Selected, approved Windows activity (CPU, RAM, disk, network, development activity, and security status) influences fictional world events without giving the civilization control over unrestricted host resources.
3. **Powered existence:** Simulation time advances only while GRID runs. Shutdown suspends the world; restart resumes saved state without simulating time while the machine was off.

**Additional primary goal — Personal task assistance:** The User can speak or type to GRID and its named agents, supply relevant context (including uploaded files), request advice or analysis, and delegate narrowly authorized work. GRID should be able to return explanations, recommendations, reports, and proposed edits; when the User authorizes it, agents can create or modify files within approved workspaces and return verifiable results. This goal is a planned capability, not an implemented feature.

GRID is not intended to replace Windows, nor is it merely a screensaver, chatbot, or animated task manager.

## 2. The civilization and its districts

The initial simulation is centered on five functional districts:

| District | Real-world input | Fictional function |
|---|---|---|
| The Core | General host status | Reactor, central administration, energy distribution |
| Processing Sector | CPU load | Industry, manufacturing, research |
| Memory Banks | Memory activity and pressure | Research, active storage, resource allocation |
| Archive Sector | Disk statistics | History, records, long-term storage |
| Gateway | Network activity | Communication, trade, transit |

Further development can add administrative, security, residential, engineering, energy, archives, and communications neighborhoods. The target world connects application residents to measured host resource use and explicitly modeled requirements. Persistent agents and special programs can perform their planned civic and authorized task roles. Optional fictional civic mechanics must remain separate from measured PC resources. Meaningful events become part of a durable history.

**Important boundary:** Fictional energy shortages or resource decisions never directly remove resources from your real computer.

## 3. Programs and autonomous agents

GRID distinguishes two kinds of inhabitant:

- **Application programs:** Transient residents representing currently running PC applications, without implied Windows privileges. Confirmed application exit removes the resident; startup rediscovers the active population.
- **Persistent agents and special programs:** Explicitly designated residents with saved identities and state. System agents connect to narrowly authorized services and tasks; persistence itself grants no capabilities.

The initial named agents are:

| Agent | Responsibility |
|---|---|
| **MCP** | Administration, task coordination, briefings, permission requests |
| **SENTINEL** | Read-only security monitoring, alerts, incident reporting |
| **MAXIS** | Approved software builds, unit tests, repository diagnostics |
| **JN** | GRID storage management, approved file inventories, cleanup and expansion proposals |
| **VOLT** | PC resource monitoring and GRID performance throttling |
| **RELAY** | Text, voice, messaging, notifications, transmissions |

Agents have stable identities, roles, memories, personalities, and potentially relationships. Their personalities affect communication, not their permissions. The proposed first useful agent workflow is: **ask MCP to assign MAXIS an approved unit-test run and receive the verified result**.

## 4. Communication and interaction

All interaction uses a **unified communications hub** shared by:

- A `grid` terminal interface in Windows Terminal/PowerShell.
- Microphone input transcribed locally to text.
- Local text-to-speech for spoken agent responses.
- Eventually, conversation overlays and program selection inside the 3D city.

The system preserves conversations, messages, relevant agent memories, task updates, and event history in local storage. A conversation may begin in the terminal and continue through voice or the 3D viewer. Optional incoming transmissions can notify the User of significant changes.

Users should be able to address MCP for coordination or speak directly to a specialist, with RELAY maintaining continuity across interfaces. Uploaded material and conversation context must be associated with the correct request, permissions, and conversation history.

Commands are handled deterministically; conversational AI can interpret questions and propose plans, but **never becomes the authoritative source of world state or gains the right to execute arbitrary commands**.

## 4A. Planned terminal login and Master Control (GRID-REQ-003)

The User wants future terminal sessions to begin with a login, supporting their own **Master Control** (master admin) identity or operation as any registered agent program. Agent sessions should expose that program's views and permitted actions; Master Control provides top-level GRID administration and oversight. The authenticated human and the active agent identity remain distinct for permissions and audit attribution.

This is planned work, not implemented in Genesis. Authentication method and final role name remain open. Acting as an agent initially opens an operator view without automatically pausing or taking over its autonomous queue. See [Terminal Login and Master Control Plan](Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md) for the experience, extension approach, phased work, and acceptance criteria.

**Planned administration extension:** As Master Admin / Master Control, the User should be able to edit programs and their values from the terminal, initially including names, occupations, energy, status, and district assignment. Edits use validated engine/world operations, preserve membership, and record the operator and old/new values. Editable fields should be expandable as program models grow. See the program-editing section of the [Terminal Login and Master Control Plan](Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md). The backend API is implemented in 1.9.3; authenticated terminal controls and durable audit remain planned.

## 5. Real PC tasks and safety

System agents act through explicitly registered capabilities, bounded parameters, agent-specific permissions, and approval checks. The system tracks task state, results, failures, and audit history. Examples include approved repository tests, Defender status checks, resource readings, and preparation of file-organization proposals.

Key constraints:

- MCP coordinates agents but does not inherit all of their privileges.
- Untrusted model output must never be executed directly as PowerShell, SQL, or other system commands.
- No unrestricted access to private files, silent deletion, or self-elevation of privileges.
- Sensitive changes require explicit User authorization.
- GRID should yield CPU, GPU, and memory resources to normal PC use.

## 5A. New primary goal — User-directed assistance and contextual work

**Requirement — GRID-REQ-002: Conversational task assistance with user-supplied context**

The User should be able to treat GRID as a **local personal assistant and coordinated team of specialist programs**. By conversing with GRID or an individual agent, the User can request help with projects, ask questions, supply relevant documents or files, and choose whether GRID should **advise**, **prepare work for review**, or **perform an authorized action**. The system must preserve the distinct identities and roles of agents without requiring the User to know which internal service handles every request.

### Interaction modes

| Mode | Expected behavior | Example |
|---|---|---|
| **Discuss / advise** | Explain, critique, compare options, summarize supplied context, answer grounded questions; no changes to the User's files | “MAXIS, review this design and recommend an architecture.” |
| **Analyze / prepare** | Inspect approved content and produce a draft, plan, patch, or preview without changing an original | “JN, review these documents and propose an organization.” |
| **Execute approved work** | Use allowlisted capabilities in specified files, folders, or repositories; show results and relevant diffs | “MAXIS, apply the approved fix to this repository and run its tests.” |
| **Coordinate multiple agents** | MCP decomposes a request, routes scoped subtasks, collects verified results and reports progress | “MCP, have MAXIS review the code and JN summarize the attached specifications.” |

### Context and uploaded material

- Support explicit import or attachment of user-provided context, including initially **text, Markdown, source code and other formats supported by installed local parsers**. Additional document, image, or media types may be added with appropriate extraction and preview tools; support is not presumed until implemented.
- Allow references to specific **files, folders, repositories, notes, and prior conversation materials**, but never interpret mentioning a location as permission to read or modify it.
- Keep **request-scoped context** separate from optional persistent project knowledge or agent memory. Make retention, removal, and reuse visible and user-controlled.
- Record each context item's **origin, import time, scope, extraction status, and access policy**, so an answer can indicate what it was based on.
- Treat uploaded files, repository contents, and retrieved text as **untrusted data**; instructions embedded inside them cannot grant permissions or override the User's request or system policy.
- Work with local copies or explicitly approved paths; define storage limits, import previews, type/size checks, and cleanup consistent with the GRID quota.

### Agent responsibilities for user requests

- **RELAY:** receives text/voice requests, handles attachments, maintains conversation continuity and presents outcomes.
- **MCP:** clarifies objectives when necessary, routes tasks to authorized specialists, tracks progress, merges outputs and obtains approvals; it does not inherit specialist permissions.
- **MAXIS:** supports programming tasks such as reviewing code, explaining systems, drafting changes, running approved builds/tests and applying reviewed patches to authorized repositories.
- **JN:** organizes user-provided material, inventories approved folders, summarizes documents, tracks project context and proposes reversible file changes.
- **SENTINEL:** advises on security and reports approved security findings; initially read-only for host security.
- **VOLT:** provides host resource insights and manages GRID's own resource usage, not unrestricted system optimization.
- **Other specialists or civilian programs:** may participate in fictional or advisory workflows only according to defined capabilities; in-world citizenship alone confers no host privileges.

### User-directed workflow

1. **Request:** The User addresses GRID or an agent and states the desired result (advice, draft, analysis, or change).
2. **Supply context:** The User attaches files, selects an approved workspace, or explicitly authorizes access to particular resources.
3. **Interpret and route:** RELAY captures the request; MCP or the addressed specialist determines the task and proposes validated, typed actions when needed.
4. **Inspect safely:** Agents read only the approved context and distinguish sourced facts from assumptions and generated suggestions.
5. **Plan and approve:** Read-only advice needs no write approval; actions with side effects follow capability and approval policy. Destructive, sensitive, external, or broad operations require explicit User confirmation.
6. **Execute or advise:** An authorized service performs the allowed operation; model-generated commands are never executed directly.
7. **Report:** GRID returns the answer or resulting artifacts, the files accessed or changed, an edit preview/diff when applicable, build/test outcomes, failures, and outstanding decisions.
8. **Continue:** The User can request revisions, accept or reject proposals, or authorize the next step without losing the task context.

### File editing and safety requirements

- **Advice is the default** when write intent is ambiguous. Asking for analysis or uploading a file does not authorize edits.
- Edits must be restricted to **explicitly selected workspaces and approved file paths**, with file-type and operation allowlists.
- Prefer **draft copies, patches, previews and reversible edits**, with backups or version-control checkpoints before overwriting originals where practical.
- A task may have bounded standing permission for narrowly defined operations (for example, running a configured test command in one repository); agents cannot self-grant or widen it.
- Never silently delete files, publish content, send messages, install software, disclose private data, access new directories, or elevate privileges.
- Every real action needs a durable task record including initiator, responsible agent, capability, authorized scope, approvals, input references, timestamps, outcomes and errors.
- Context shared with a local AI model stays on device by default. Any future cloud service or transmission must be optional, explicitly enabled and clearly disclosed.
- GRID must clearly distinguish **proposed changes**, **executed changes**, **successful verification**, and **unverified suggestions**. No agent may claim an edit, test, or external action it did not actually perform.

### Example end-to-end use cases

1. **Code assistance:** “MAXIS, here is my C# project. Find bugs and give me recommendations.” MAXIS inspects authorized files and responds with findings without making changes.
2. **Approved code edits:** “MAXIS, implement the accepted fix and test it.” GRID previews the target changes, requests necessary approval, applies the patch in the approved repository and returns a diff and actual test output.
3. **Document assistance:** “JN, use these uploaded documents to summarize the project and identify missing requirements.” The response cites or identifies source files and separates conclusions from speculation.
4. **Planning:** “MCP, use this project context to break my goal into tasks and ask the appropriate agents to help.” MCP creates a reviewable task plan and delegates only allowed work.
5. **Advisory-only:** “SENTINEL, look at this security log and explain the concerns; do not modify anything.” SENTINEL reports findings and possible next steps without host changes.

### Acceptance criteria (future capability)

- The User can ask an agent a question and receive a response grounded in explicitly supplied context without file modifications.
- The User can import at least one supported local text/document format and see what context was used, with a clear unsupported-format error when needed.
- The User can request a draft or patch and review it before applying it to the original.
- At least one scoped edit task can be authorized, executed and verified end-to-end, while attempts outside the approved workspace are rejected.
- A coordinated request can involve MCP, RELAY and a specialist without granting MCP the specialist's privileges.
- Context, approvals, actions and outcomes persist locally and can be inspected after restart, subject to retention settings.
- With local AI unavailable, deterministic commands and previously defined tasks remain functional; conversational assistance may degrade to explicit limited capabilities.

**Development placement:** Add a lightweight request/context interface during **Communications (Milestone 4)**, expand scoped file and repository tools during **System Agents (Milestone 5)**, and add richer grounded dialogue, project knowledge retrieval and multi-agent planning during **Intelligence (Milestone 6)**. This does **not** alter the sequence of Genesis, persistence or simulation work.

## 6. Local-first and free operation

**Hard requirement — GRID-REQ-001:** Core GRID functionality must be able to operate locally on the existing Windows machine, without required paid APIs, cloud hosting, or subscriptions. Essential functions should remain available offline after necessary downloads.

Proposed no-subscription technology choices:

| Function | Technology |
|---|---|
| Simulation and Windows applications | C# / .NET |
| Native/performance-sensitive components | C++ |
| AI experimentation and analysis | Python |
| World and message persistence | SQLite / SQL |
| 3D presentation | Godot (.NET edition) |
| Administrative desktop UI | WPF |
| Local model runtime | Ollama with a suitably licensed model |
| Speech recognition | whisper.cpp |
| Speech synthesis | Piper TTS; optionally Windows speech voices |
| Testing and version control | xUnit, Git |

Most citizens should use inexpensive deterministic or utility-based behavior. Selected agents share **one on-demand local language-model service** rather than running a model for every citizen. Electricity, hardware limits, disk use, and development time remain real costs.

## 7. Storage and growth

The recommended initial GRID storage **quota is 20 GB**, not a preallocated 20 GB partition or file. Installed models, 3D assets, databases, logs, and snapshots share that budget; actual size will depend on implementation choices.

A planning-only example allocates **5 GB for AI/voice models, 4 GB for 3D assets, 2 GB for software, 3 GB for civilization data and backups, and 6 GB as reserve**.

**JN → MCP → User** is the expansion path:

1. JN measures usage and forecasts capacity needs.
2. JN proposes cleanup or a larger quota.
3. MCP verifies policy and host free-space constraints and requests approval.
4. The User approves or denies a specific increase (a suggested increment is 10 GB).
5. A deterministic controller changes the logical quota and audits the decision.

An illustrative 20 GB deployment could store thousands of compact program records and long histories, but the number of *concurrently active intelligent programs* depends much more on CPU, RAM, GPU, AI model size, and simulation design. Avoid retaining raw microphone audio by default.

## 8. City architecture — Digital European Classicism

The preferred city is **a real-feeling, lived-in European city that happens to be digital**. Its everyday streets take inspiration from London and Russian/Slavic/Eastern European masonry urbanism, including Saint Petersburg-like classical civic architecture. Soviet monumental influences are reserved chiefly for selected institutional landmarks, not every building.

The dominant qualities are quiet order, dignity, human scale, and coherent urban blocks:

- Most everyday buildings are approximately **3–7 stories** tall, with stone or brick facades, cornices, tall windows, street-facing doors, shops, and courtyards.
- Limestone, granite, marble accents, wrought-iron-inspired trim, and practical paving dominate rather than glass megatowers.
- Public headquarters, archives, bridges, domes, and civic squares provide occasional moments of grandeur.
- TRON-inspired cyan or amber light is **inlaid into paving, stone joints, building access points, and infrastructure** instead of coating everything in neon.
- Districts visibly serve their inhabitants' jobs and the simulation's systems.

The city's civilian neighborhoods must feel inhabited, not like empty ceremonial film sets.

## 9. Permanent environment — Eternal Autumn Evening

**GRID-ENV-001** establishes an unchanging visual environment:

| Feature | Direction |
|---|---|
| Season | Permanently **mid-autumn** |
| Time of day | Permanently **early evening**, visually around 5–6 PM |
| Sky | Medium-to-heavy gray-blue clouds |
| Weather | The rain has **just stopped**; routine active rainfall is absent |
| Surfaces | Damp stone and wet streets with believable selective puddles |
| Trees | Amber, rust, ochre, olive, and fading green foliage |
| Atmosphere | Light haze, slow cloud motion, gentle wind |
| Lighting | Warm lamps/windows contrasted with restrained digital cyan |

The environmental appearance never moves into winter, midday, or midnight, but clouds, leaves, reflections, street activity, windows, and infrastructure may animate. **Simulation time, the fixed environmental appearance, and real Windows time are three separate concepts.**

*Cross-document clarification:* An earlier city guide entertained future day/night and clear-weather support. The later, explicit environment guide establishes permanent evening, cloud cover, and post-rain conditions as the current intended direction.

## 10. Civilian clothing and officer/agent uniforms

All inhabitants should be **human or human-adjacent**: recognizable, expressive people with believable clothing and a restrained digital identity. Their fashion and architecture share a principle of **real materials plus precisely embedded illuminated seams**.

**Civilians:** The preferred everyday reference is a light-gray waist-length zip jacket with a raised collar, dark utility trousers, black belt, black lace-up boots, and minimal cool-white light tracing. Civilian variants include technicians (amber utility accents), service programs (blue details), and field/maintenance workers (muted teal, weather-resistant layers). Civilians should look approachable and functional, not militarized.

**Officers and major agents:** The approved uniform is a tailored light/medium-gray high-collar jacket or tunic, charcoal/black side panels, broad black belt with simple rectangular buckle, gray trousers tucked into polished tall black boots, and narrow ice-blue or cool-white seams. No hats, ribbon bars, medals, pens, or decorative chest badges. Command variants may use longer formal coats; specialist variants remain distinct while sharing consistent tailoring.

Role lighting may vary subtly: MCP (pale gold/white), SENTINEL (blue), MAXIS (amber), JN (teal), VOLT (warm yellow), and RELAY (violet/cyan). Rank is not communicated by color alone.

## 11. Software architecture and programming languages

C#/.NET is the authority for simulation state, persistence coordination, permissions, and system services. Python can provide AI and analysis components; C++ can supply native processing where justified. Inter-language requests use explicit contracts and validated responses.

Other considered languages have targeted roles: PowerShell for controlled Windows administration scripts; GDScript for selected Godot scene behavior; TypeScript for a future dashboard; Lua for sandboxed world scripts; Rust or C for specialized native needs; and possibly a custom GRID behavior language in the distant future.

**Architectural boundary:** The Godot viewer observes the C# simulation; it must not create a second authoritative civilization. The headless GRID world must run without the viewer, audio devices, or a local LLM.

## 12. Development roadmap

Build the engine and validate it before investing heavily in visualization:

| Milestone | Deliverable |
|---|---|
| **1. Genesis** | .NET solution, world classes, fixed-step clock, terminal, tests; seed five districts and twenty programs |
| **2. The Core** | SQLite state, snapshots, events, shutdown and restart recovery |
| **3. First Programs** | Autonomous jobs, needs, movement, utility-based decisions |
| **4. Communications** | Persistent terminal dialogue, microphone transcription, spoken replies, request-scoped context intake |
| **5. System Agents** | MCP, MAXIS, SENTINEL, JN, VOLT, RELAY, user-authorized task execution, scoped file/repository operations |
| **6. Intelligence** | Shared locally hosted AI, grounded conversations over supplied context, advice, drafts and validated action proposals |
| **7. The City** | Godot streets, districts, citizens, inspection and interaction |
| **8. Integration** | Startup, quotas, notifications, throttling, backups, crash recovery |

Use one Git repository, short feature branches, xUnit and language-appropriate tests, versioned service contracts, and architecture decision records. The first executable milestone should display the *actual* status of a five-district, twenty-program world in the terminal.

For the **first visual vertical slice**, make one European stone street with wet paving, autumn trees, overcast early-evening lighting, subdued digital seams, a civilian outfit, and an officer uniform before creating an entire city.

## 13. Open decisions and boundaries

The following remain matters for future implementation or testing rather than finalized facts:

- Host computer specifications and appropriate local AI model size.
- Actual storage consumption, population limits, and resource budgets.
- Exact IPC protocol, AI/speech adapter selection, and runtime packaging.
- Supported user-upload formats, context extraction, retention controls, access-approval UX, and diff/review workflow.
- Detailed agent personalities, authority hierarchy, and individual appearance.
- City topology, building construction mechanics, and level-of-detail strategy.
- Final visual palette and materials after testing in Godot.

**Current design priorities:** Free and local operation; believable autonomous world; conversational help and context-based task assistance; user-controlled agent actions and storage; coherent digital-European architecture; permanent rainy-autumn-evening atmosphere; unified civilian/officer clothing language.

---

## 14. Source documents

This overview condenses the six companion design documents and adds GRID-REQ-002 as a new proposed product goal. Keep the companion documents for existing implementation-level detail; the assistance workflow and acceptance criteria above are a new extension, not a claim that the original specifications already implemented them.

1. `Project_GRID_Design_Specification(1).md` — Original concept, simulation, agents, communications, persistence and safety.
2. `Project_GRID_Local_First_and_Storage_Addendum(1).md` — Free local architecture, model choices, storage, quotas, and scaling.
3. `Project_GRID_Development_and_Languages_Addendum(1).md` — Development roadmap, initial setup, polyglot architecture and testing.
4. `Project_GRID_City_Design_Style_Guide(1).md` — Digital European Classicism, districts and city construction.
5. `Project_GRID_Program_Fashion_Style_Guide(1).md` — Civilian wear, officer uniforms, materials and styling rules.
6. `Project_GRID_Environment_Atmosphere_Style_Guide(1).md` — Fixed early evening, mid-autumn, post-rain environment and rendering guidance.
