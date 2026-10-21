# Project GRID

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

**Implemented and verified.** Use connect (default ARCHON), talk, conversations, transcript, resume and disconnect. ARCHON is created on first explicit connection as a persistent coordinator Agent; a stable world-local operator handle supports reopening saved conversations. Replies to status/who are you are labeled verified GRID state; other replies are labeled deterministic simulated dialogue and claim no task execution/delegation. Input/reply pairs commit atomically and reuse schema v10.

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

The shared contract replaces permanently saved ordinary fictional citizens with transient running-application representatives, persistent agents/special programs, and host-related resource requirements. The legacy illustrative model and civic examples below must be interpreted under that lifecycle distinction.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


## GRID-REQ-004 - Hardware-generated city contract

**Requirement recorded 2026-10-18; headless implementation completed 2026-10-19 in 2.6.** First-time world creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates a unique city from those components, saves the resulting world and reuses that saved world on subsequent startups.

### Creation and load contract

1. Resolve the selected world's local save location before generation. If an initialized world exists, validate and load its saved layout, identities, simulation state and clock.
2. Only for a genuinely new world, collect a read-only hardware profile covering all six component categories. Distinguish unavailable information from absent hardware; record discovery provenance and partial results.
3. Normalize component ordering and units. Use the profile with a new per-world seed and versioned generation rules to create a bounded, valid city. Identical profile, seed and generator version must produce repeatable initial generation; hardware alone is not a globally unique identity.
4. Persist a stable world ID, creation profile, seed, generator/schema versions, component-to-city associations, generated layout, entities and initial simulation state as one consistent initialization. Mark creation complete only after a successful durable commit.
5. Later launches load the actual saved world, including subsequent edits and history. Do not regenerate it from the seed on startup. Start/pause behavior remains independent of the create/load decision, with no offline catch-up.
6. Missing components, hardware upgrades, driver changes or changed discovery ordering must not replace a saved world. Detecting hardware changes may inform future explicit migration proposals; it is not authorization to rebuild.
7. Unreadable, corrupt or incompatible saves enter a visible recovery/migration path. A failed load is not proof that no world exists. Interrupted initialization and concurrent launches must not create two authoritative worlds or overwrite a valid save.

### Architecture and component mapping

A read-only Windows discovery adapter supplies normalized data to a C# generation service; Core holds world data, Simulation operates it, and Persistence owns durable storage. Godot consumes saved layouts and snapshots and never generates a competing authoritative world. Tests use supplied hardware fixtures, not the developer's live machine.

| Discovered category | Design intent; implemented mapping is in Hardware City Generation |
|---|---|
| Motherboard | Overall connectivity and regional arrangement |
| CPU | Processing/industry capacity and district structure |
| GPU | Graphics/visual-computation facilities and landmarks |
| RAM | Memory/research areas and bounded initial scale |
| Storage | Archive/storage areas, with multiple devices represented coherently |
| Network hardware | Gateways and communications connections |

These are semantic mappings, not an assertion that Windows exposes precise physical board geometry. Motherboard layout images are artistic references, not authoritative measurements. Integrated/discrete GPUs, multiple disks/adapters, virtual adapters and unavailable motherboard data need explicit normalization/fallback rules. Discovery does not scan network peers or read user files. Store only the local inventory fields needed for generation; raw serial numbers and MAC addresses are not required for world identity.

Generation must honor the existing architecture, readable streets, human-scale neighborhoods and atmosphere guides. Capacity influences are bounded; more hardware must not imply unlimited citizens or assets. Live load/temperature/traffic telemetry can later affect fictional activity through separate rules, but must not continually reshape or regenerate the city.

### Acceptance and unresolved design choices

- First creation covers all six hardware categories, produces a valid hardware-influenced layout and saves it before normal world use.
- Save, close and reopen preserve layout, entity IDs, memberships, edited values and simulation time without invoking generation again.
- Repeatable fixture tests verify canonical ordering, seed/version determinism and profile-sensitive output. Distinct worlds have distinct saved identities; visually different output is a design goal, not a guarantee of globally unique geometry.
- Test partial discovery, unsupported APIs, multiple components, hardware changes, corrupt saves, failed writes and simultaneous initialization.
- Exact discovery APIs, field schema, component deduplication, fallback UX, scale limits and mapping algorithms remain decisions to implement and verify.

The existing fixed Genesis factory remains the implemented baseline and a useful test fixture. Grid.Persistence now provides the section 2.1 SQLite schema and migration foundation; section 2.2 now saves and reuses fixed Genesis worlds. Hardware discovery and hardware-derived generation remain planned. Checklist 2.6 tracks implementation; Phase 7 renders the saved result and Phase 8 integrates lifecycle behavior.

## Implementation status - 2026-10-17

Implemented scope is the headless .NET world, fixed-step clock/engine, extensible terminal and post-Genesis editing API. The broader architecture, sample interfaces, communications commands, persistence, host integration, autonomy and renderer below remain targets unless explicitly identified as implemented. The master checklist controls milestone scope; the original broad prototype proposal is not the acceptance scope of the released Genesis tag.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Hybrid Digital Civilization and Autonomous PC Agent System**  
**Consolidated Design Specification — Draft v0.4**  
**Date:** October 15, 2026  
**Status:** Genesis and editing API implemented; expanded architecture remains proposed

---

## 1. Vision

Project GRID is a persistent, TRON-inspired digital civilization hosted on a Windows PC. The world contains simulated districts, autonomous programs, a resource economy, relationships, history, and a visual 3D environment. It is also a practical personal computing interface: selected system programs can observe approved aspects of Windows, perform tightly scoped tasks, and report results through terminal and voice communication.

**Three core principles:**

1. **Autonomy:** Digital citizens make choices, interact, work, and develop history without constant direction from the User.
2. **Host influence:** Real PC metrics and approved activities influence the simulated environment, without allowing the simulation to interfere with host operation.
3. **Powered existence:** Simulation time advances only while its engine runs. When the PC shuts down, the Grid stops advancing; its state remains saved and resumes on startup. There is no offline catch-up simulation by default.

The Grid is **not** only a screensaver, chatbot collection, graphical task manager, or Windows replacement. Its fictional city and useful system-facing agents share a coherent world model.

### Inspiration

The fictional roles of **TRON** (security) and **CLU** (administration / digital counterpart to Flynn) inspire the idea of specialized programs with identities and responsibilities. Original GRID program names and personalities can be used in the implementation.

---

## 2. World Design

### 2.1 Initial city districts

| District | Windows influence | Simulation role |
|---|---|---|
| The Core | General host/system state | Central reactor, administration, simulated power distribution |
| Processing Sector | CPU utilization | Industrial manufacturing and research |
| Memory Banks | Memory utilization / availability | Research and short-term resource storage |
| Archive Sector | Disk activity / approved storage statistics | Historic records, archives, construction materials |
| Gateway | Network throughput / connectivity metadata | Communication, exploration, trade and transit |

A later city may add a GPU/Arena district, residential zones, defense facilities, maintenance depots, and research campuses.

### 2.2 World mechanics

- Application resources reflect observed CPU, RAM, GPU/VRAM, storage and network conditions and explicit requirement estimates. Energy is a labeled GRID interpretation of resource sufficiency; its formula remains planned. These observations do not allocate or withhold actual hardware resources.
- Buildings and transit routes can be constructed, expanded, repaired, or abandoned.
- Citizens navigate the world and carry out jobs.
- Program decisions may lead to emergent situations, including negotiations, scarcity, innovation, and district development.
- An event log preserves the causes and consequences of significant changes.
- Physical visualization should reflect the simulation state rather than fabricate world events.

### 2.3 Example Windows-to-world mappings

| Host activity | Possible Grid response |
|---|---|
| CPU load increases | Processing Sector activity rises; energy production and maintenance pressure change |
| Memory pressure rises | Memory Banks temporarily prioritize essential work |
| Network traffic surges | Gateway dispatches additional simulated data carriers |
| Development tools open | Research and development work becomes active |
| GPU-intensive game launches | Arena or GPU district receives activity |
| PC is mostly idle | Maintenance or recovery opportunities increase |
| Approved test suite finishes | Forge may record a research event and report the real outcome |
| Defender reports an alert | Sentinel opens a simulated incident, without treating a fictional event as proof of actual danger |

All responses should be tuned, smoothed, bounded, and based on measured host conditions. For example, intense gaming should not automatically destroy the civilization.

---

## 3. Program Population

### 3.1 Application programs (GRID-REQ-005 target)

These represent currently running PC applications. They appear through live discovery and leave after confirmed application exit; individual ordinary resident state is not saved for later restoration. They have **no direct Windows privileges**. Their activity and resource presentation must be grounded in observations, not invented jobs or arbitrary energy drain. Autonomous civic roles belong to persistent agents and explicitly designated special programs.

### 3.2 Persistent agents and special programs

These retain identities and state across restarts. System agents can be both in-world characters and concrete permission-scoped services; other special programs require explicit classification. Their decisions can result in real tasks only through approved capabilities and execution policies. A name or process label cannot grant persistent status or authority.

### 3.3 Program identity and state

The sample below is a legacy conceptual model, not the complete GRID-REQ-005 target; classification, observation bindings and persistence policy must be added. Persistent personality/memory fields apply to agents/special programs, not automatic saved ordinary residents. Current world-owned GridProgram setters reject direct mutation; validated edits use the engine API and per-program revisions.

Each program may have:

- Identity and display name: durable for agents/special programs, runtime-incarnation identity for ordinary application residents
- Role, occupation, district, and capabilities
- Personality and preferences
- Energy, health, needs, goals, and priorities
- Location and movement state
- Relationships with other programs
- Memories and experiences with provenance
- Conversation history and voice profile
- Assigned tasks and execution status

Illustrative C# model:

```csharp
public sealed class GridProgram
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Occupation { get; set; } = "";
    public float Energy { get; set; } = 100;
    public float Health { get; set; } = 100;
    public Guid CurrentDistrictId { get; set; }
    public List<ProgramGoal> Goals { get; set; } = [];
    public List<ProgramMemory> Memories { get; set; } = [];
    public List<ProgramRelationship> Relationships { get; set; } = [];
}
```

`ProgramGoal`, `ProgramMemory`, and `ProgramRelationship` are conceptual types to define in the implementation.

### 3.4 Decision-making

A proposed per-agent loop:

1. Observe the environment, personal state, and nearby programs.
2. Generate allowed candidate actions (work, rest, repair, socialize, explore, negotiate).
3. Score actions using goals, needs, rules, and context (utility-based decision-making; potentially state machines and pathfinding).
4. Execute the selected action through the world simulation.
5. Store resulting state changes, memories, and meaningful events.

Routine simulation does **not** require a large language model for every action. Optional local AI is better reserved for natural-language dialogue and higher-level planning. AI-generated proposals never bypass the deterministic rules engine.

---

## 4. Initial Named System Programs

| Program | Role | Real capabilities (scoped) | Example update |
|---|---|---|---|
| **SENTINEL** | Security, inspired by TRON | Read Windows Defender/firewall status and permitted security events, investigate and report anomalies | “A security alert was recorded. No protective action has been taken.” |
| **ARCHON** | Coordinator/administrator, inspired by CLU | Assign tasks, gather summaries, track queues, coordinate simulation administration | “Sentinel completed its review; Forge has three tasks pending.” |
| **ARCHIVIST** | Data management | Inventory approved folders, identify duplicates, prepare organization proposals and perform approved reversible moves | “18 possible duplicates are ready for review.” |
| **FORGE** | Development and builds | Inspect explicitly authorized repositories, run configured tests/builds, produce diagnostics and propose patches in isolated workspaces | “Two unit tests failed; a report is available.” |
| **VOLT** | Resource monitoring | Observe CPU/GPU/RAM/disk/thermal metrics where supported and adjust *Grid’s own* resource budget | “I reduced Grid background work while host load is high.” |
| **RELAY** | Communication | Route messages, transcribe audio, synthesize replies, manage notifications, and deliver briefings | “Forge's report is complete. Shall I read it aloud?” |

**Organizational rule:** ARCHON may coordinate other agents but does not inherit their permissions. SENTINEL should independently monitor security conditions. RELAY is the universal communications layer.

Potential first trio for useful system functionality: **ARCHON + SENTINEL + FORGE**. Basic RELAY functionality must still exist early to deliver terminal messages.

### 4.1 Agent personality

Personality should change how a program communicates, not grant it extra access. For example:

- SENTINEL: precise, vigilant, concise.
- ARCHON: calm, strategic, administrative.
- ARCHIVIST: thorough and methodical.
- FORGE: curious, engineering-focused.
- VOLT: factual, resource-conscious.
- RELAY: clear and responsive.

---

## 5. Real Task Execution

Example request: “Archon, have Forge run my unit tests and ask Archivist to check Downloads.”

1. RELAY receives the typed or spoken message.
2. The communication layer identifies the requested intent and recipient(s).
3. ARCHON breaks the request into discrete task proposals.
4. The system validates each proposal against specific tools, directories, repositories, and approved parameters.
5. FORGE runs tests through a configured test runner in an authorized workspace; ARCHIVIST inventories the approved Downloads folder.
6. Results and failures are stored in durable task/event records.
7. ARCHON combines the results into a report; RELAY presents the text and optionally reads it aloud.

### 5.1 Task lifecycle

Suggested states: `Pending`, `AwaitingApproval`, `Running`, `Completed`, `Failed`, `Cancelled` (optionally `Paused`, `RetryScheduled`).

```csharp
public enum GridTaskStatus
{
    Pending,
    AwaitingApproval,
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed class GridTask
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = "";
    public string AssignedProgram { get; init; } = "";
    public GridTaskStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string CapabilityId { get; init; } = "";
    public string ParametersJson { get; init; } = "{}";
}
```

A durable SQLite-backed queue should retain requests over restarts, use transaction-safe claims, and avoid duplicate side effects through idempotency and explicit recovery rules. A task due while the PC is powered off should be handled under a defined resume policy when the Grid restarts, not executed retroactively without user permission.

### 5.2 Permission tiers

| Tier | Meaning | Example |
|---|---|---|
| 0 — Observer | Read permitted data | SENTINEL reads security status |
| 1 — Reporter | Analyze and notify | VOLT reports high sustained CPU use |
| 2 — Operator | Perform narrowly preapproved, bounded actions | FORGE executes a fixed test command in an allowed repository |
| 3 — Approval required | Propose higher-risk changes; execute only after user authorization | ARCHIVIST proposes deletions or major file moves |

These are **application policy levels**, not Windows administrator privileges. Actual access must be separately enforced by capability-specific tool interfaces, operating-system permissions, allowlists, and user approval.

### 5.3 Security boundaries

- No self-escalation of permissions by agents.
- No direct execution of untrusted AI-generated PowerShell, SQL, or shell commands.
- No unrestricted filesystem access or browsing of private content.
- No security software disabling or firewall alteration without separate, explicit, appropriate authorization.
- No silent deletion or file changes outside approved scopes.
- Use dry runs and reversibility for file organization where possible.
- Log actions, parameters, approvals, outcomes, and errors.
- Keep the simulated world unable to command Windows without passing the same task broker checks.
- Minimize collected telemetry and never infer security guarantees from fictional Grid status.

---

## 6. Unified Grid Communications System (GCS)

**Goal:** One persistent conversation network reachable by **Windows Terminal**, **microphone**, and **Godot 3D viewer**. A conversation begun in one interface can continue in another.

### 6.1 Interfaces

1. **Terminal:** Inspect the Grid, issue commands, have conversations, inspect agent tasks, and control voice output.
2. **Voice console:** Push-to-talk microphone input, transcription, live text responses, and speech playback.
3. **3D viewer:** Select programs or districts, open communication channels, display dialogue, and play assigned program voices.

### 6.2 Proposed communications commands

These are future command examples. Current commands are help, status, programs, districts, inspect, start, pause, live, step/tick and exit; see the [README](../README.md).

```text
grid status
grid programs list
grid agent inspect archon
grid connect CL-047
grid talk CL-047 "How is the reactor?"
grid broadcast "Prepare for system maintenance."
grid voice on
grid voice off
grid listen
grid history CL-047
grid observe CL-047
grid events --recent
```

Administrative and agent-specific commands may include `grid tasks list`, `grid task inspect <id>`, `grid approvals list`, and `grid brief`.

### 6.3 Message pipeline

```text
Typed text ────────────────────────────────┐
                                           v
Microphone → Capture → Speech-to-text → Message Router
                                           |
                                   Intent / recipient
                                           |
                         +-----------------+-----------------+
                         |                                   |
                 Deterministic command                  Conversation
                         |                                   |
                 Permission validation            Retrieve program context,
                         |                         state, and memory
                         |                                   |
                         |                        Dialogue generation
                         |                                   |
                         +----------- Action proposals -----+
                                             |
                                      Rules + capability
                                       validation broker
                                             |
                                       World/task engine
                                             |
                                 Message + event persistence
                                             |
                           +-----------------+-----------------+
                           |                                   |
                        Text UI                           Speech output
```

A conversation that requests no action can return a response without changing simulation state. Messages from AI models are never themselves authoritative world facts or executable instructions.

### 6.4 Shared message model

```csharp
public enum MessageOrigin { Terminal, Microphone, WorldViewer, SystemEvent }
public enum MessageKind { Conversation, Command, Broadcast, Notification }

public sealed record GridMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public Guid ConversationId { get; init; }
    public string SenderId { get; init; } = "";
    public string RecipientId { get; init; } = "";
    public MessageOrigin Origin { get; init; }
    public MessageKind Kind { get; init; }
    public string Content { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public long SimulationTick { get; init; }
    public bool SpeakResponse { get; init; }
}
```

Additional implementation metadata should include correlation and causation IDs, delivery status, action proposal IDs, transcription language/confidence when available, and sender authentication/authorization context.

### 6.5 Speech recognition and synthesis

**Input options**

- **Default:** Push-to-talk, with visible microphone activity indicator.
- **Optional:** Opt-in local wake-word mode, which requires ongoing microphone analysis.
- **Disabled:** Text-only mode.

**Speech recognition candidate:** local `whisper.cpp`; audio capture through a Windows audio library such as NAudio.

**Text-to-speech candidates:** Windows `System.Speech.Synthesis` for a simple MVP; local Piper voices for richer character differentiation later.

Each program may have a stable voice profile: voice/model identifier, speech rate, pitch (where supported), volume, and style. Voice output should have a global mute switch, interruption/cancellation, single-speaker arbitration, device selection, and quiet-hours controls.

Default microphone audio should be discarded after transcription, while the transcript remains subject to user-defined retention. Avoid surprise speech during unattended background operation.

Example service interfaces:

```csharp
public interface ISpeechRecognitionService
{
    Task<string> TranscribeAsync(Stream audio, CancellationToken ct);
}

public interface ISpeechSynthesisService
{
    Task SpeakAsync(string text, string voiceId, CancellationToken ct);
}

public interface IDialogueService
{
    Task<GridMessage> GenerateReplyAsync(GridMessage input, CancellationToken ct);
}

public interface IConversationRepository
{
    Task SaveAsync(GridMessage message, CancellationToken ct);
    Task<IReadOnlyList<GridMessage>> GetHistoryAsync(Guid conversationId, CancellationToken ct);
}

public interface IGridCommunicationHub
{
    Task<GridMessage> SendAsync(GridMessage message, CancellationToken ct);
}
```

### 6.6 Conversation channels

| Channel | Use | Authority |
|---|---|---|
| Private | User speaks to one program | Responses and validated proposals only |
| District | User addresses a department | Same scoped broker rules |
| Broadcast | User sends informational announcements | No implicit administrator privileges |
| Administration | Approved system management operations | Explicit privileges and approvals |
| Emergency | Program reports critical simulated or real alerts | Predefined alert paths; sensitive remediation requires authorization |

### 6.7 Program voice/personality examples

- Administrative voice: calm, authoritative, slightly synthetic.
- Security voice: clipped and precise.
- Research voice: curious and conversational.
- Energy management voice: methodical.
- Ordinary citizens: varied natural voices.

### 6.8 Incoming transmissions

Agents may initiate notifications after significant events: security alerts, failed builds, task completion, unusual load, or approval requests. Outputs may include terminal notifications, Windows notifications, in-world radio messages, or opt-in spoken alerts. Configure severity thresholds, quiet hours, rate limits, and per-agent notification preferences. Routine events should usually be logged silently.

---

## 7. Persistence, Memory, and History

### 7.1 Distinct data categories

- **World snapshots:** Positions, resources, buildings, program identities and states, current simulation clock.
- **World event history:** Construction, decisions, incidents, relationships, task outcomes, and real-host-derived world events.
- **Conversation history:** The actual messages exchanged across all interfaces.
- **Program memories:** Curated, attributed summaries of important exchanges and experiences that may influence future decisions.
- **Task audit:** Approvals, permitted tools, results, failures, retries, and user notifications.

Conversation history is **not equivalent** to program memory. A program can remember that a User prefers archives to industrial expansion without that preference changing world policy automatically.

### 7.2 Proposed SQLite tables

| Table | Important fields |
|---|---|
| `Programs` | ProgramId, Name, Type, Role, DistrictId, State |
| `Districts` | DistrictId, Name, ResourceLevels, Infrastructure |
| `WorldSnapshots` | SnapshotId, SimulationTick, Data, CreatedAt |
| `WorldEvents` | EventId, SimulationTick, Type, SourceId, Payload |
| `Conversations` | ConversationId, Type, CreatedAt, LastActivityAt |
| `Messages` | MessageId, ConversationId, SenderId, RecipientId, Content, Origin, Timestamp, SimulationTick |
| `ProgramMemories` | MemoryId, ProgramId, Summary, Importance, SourceMessageId |
| `VoiceProfiles` | ProgramId, VoiceId, Rate, Pitch, Volume |
| `CommunicationEvents` | EventId, MessageId, EventType, Result, SimulationTick |
| `Tasks` | TaskId, OwnerProgramId, Status, CapabilityId, Parameters, CreatedAt |
| `TaskApprovals` | ApprovalId, TaskId, RequestedAction, Decision, AuthorizedAt |
| `TaskExecutionLog` | ExecutionId, TaskId, Tool, InputsDigest, Outcome, Time |

Use database transactions, migrations, indexes, and suitable SQLite WAL settings. Snapshot recovery and event replay must be designed to prevent doubled side effects or mismatched world/conversation histories. Conversation deletion and derived memory deletion should be user-controllable.

---

## 8. Windows Lifecycle and Host Integration

### 8.1 Proposed integrated startup sequence

Current startup is manual through dotnet run. It creates and saves Genesis only when no world artifacts exist, otherwise validates and loads the saved world, paused unless --start is supplied. Section 2.2 provides autosave, orderly shutdown saves and explicit --recover backup restoration. There is no Windows startup service yet.

1. User logs in to Windows and the Grid background host starts.
2. Grid validates persistent data and loads the latest consistent saved state; only genuinely new worlds use the GRID-REQ-004 hardware discovery, generation and durable initialization flow. Failed loads use recovery rather than regeneration.
3. The host establishes read-only telemetry subscriptions or polling.
4. The simulation engine starts its clock and agent scheduler.
5. RELAY and ARCHON can offer an optional concise startup briefing.
6. The terminal, voice console, and 3D viewer may connect independently.

A cinematic full-screen startup sequence **after Windows login** is practical. Rendering a custom interactive Grid *before* Windows login is a substantially different, constrained operating-system integration and is **not** an MVP dependency. The motherboard/Gigabyte firmware splash screen is separate from the Grid application.

### 8.2 Suspension and shutdown

- Persist snapshots at regular intervals and after material events.
- Save cleanly on normal application exit, session shutdown, or suspend when the OS allows.
- Avoid relying solely on last-second shutdown callbacks; power loss is possible.
- Stop the simulation clock while the host is not running.
- On resume, restore the last durable tick without time-skipping.
- Distinguish pending, interrupted, and completed real tasks and reconcile retries safely.

### 8.3 Performance policy

- Run core simulation independently of graphics.
- Use low-frequency logical ticks for most tasks, not rendering frame rates.
- Run the 3D renderer at its own frame rate when visible.
- Throttle background activities under host load.
- Avoid continuous heavyweight AI inference or voice processing unless needed.
- Suggested initial engineering target: **under 1% average CPU for the idle headless engine**, subject to profiling on the specific PC; this is a target, not a guarantee.

---

## 9. Architecture and Technology Stack

**Primary recommendation:** C#/.NET for the core, interfaces, telemetry, and task services; Godot .NET for 3D; WPF for native Windows controls; SQLite for durable records.

| Subsystem | Candidate technology | Notes |
|---|---|---|
| World simulation | C# / .NET | Independent of UI or game engine |
| 3D world | Godot .NET | City, camera, animated agents, environmental effects |
| Desktop controls | WPF | Dashboard, voice settings, approvals, task details |
| Terminal | .NET CLI | Commands, conversations, status and audit |
| Persistence | SQLite | World state, tasks, communication and histories |
| Interprocess transport | Named pipes (Windows) | Local IPC, scoped access; optional local WebSocket for viewer integration |
| Telemetry | Supported Windows APIs | Resource sampling; approved process and security information |
| Audio capture | NAudio or equivalent | Push-to-talk and microphone device management |
| Offline STT | whisper.cpp | Local speech-to-text candidate |
| Initial TTS | Windows `System.Speech` | Simple initial synthesis |
| Neural TTS | Piper | Optional higher-quality local voices |
| AI dialogue | Pluggable local language model adapter | Optional; facts always grounded in world state |
| Tests | xUnit | Deterministic engine, broker, data and IPC coverage |

### 9.1 Proposed solution tree

```text
GRID/
├── Grid.Core/                   # World objects, resource rules, agent states
├── Grid.Simulation/             # World clock, ticks, autonomous decision systems
├── Grid.Host/                   # Windows telemetry and event translation
├── Grid.Persistence/            # SQLite schema, snapshots, event history
├── Grid.Agents/                 # Agent registry, memory, decision engine, scheduler
├── Grid.SystemPrograms/
│   ├── Sentinel/
│   ├── Archon/
│   ├── Archivist/
│   ├── Forge/
│   ├── Volt/
│   └── Relay/
├── Grid.Communication/          # Message hub, routing, sessions, dialogue
├── Grid.Audio/                  # STT/TTS providers, voice profiles, playback
├── Grid.Terminal/               # CLI commands and interactive chats
├── Grid.Viewer/                 # Godot 3D city and in-world conversations
├── Grid.Control/                # WPF settings, task and agent dashboard
├── Grid.Security/               # Capability broker, policy and approvals
└── Grid.Tests/                  # Unit/integration/acceptance tests
```

### 9.2 Execution boundaries

- **World engine:** Owns simulated resources, time, identities and world events.
- **Communication hub:** Owns message routing and conversation sessions.
- **Task broker:** Sole supported path from agent requests to host actions.
- **Tool adapters:** Implement approved operations; validate inputs and host permissions.
- **Persistence:** Provides durable commits and recovery semantics.
- **Clients:** Terminal, WPF voice console, and Godot viewer are views/controllers, not independent authorities.

The communication hub belongs to the always-running Grid host, **not** to the viewer. Closing the viewer should not end the world or erase conversations.

---

## 10. Example User Experiences

### 10.1 Morning startup briefing

> **ARCHON:** “Welcome back, User. The Grid has resumed. All six system programs are responding. Sentinel has no outstanding alerts from its last completed check. Forge has three development tasks pending. Would you like a full briefing?”

The briefing must distinguish the time of the latest check from actual live scan results.

### 10.2 Spoken task delegation

**User:** “Archon, have Forge run my unit tests and ask Archivist to check Downloads.”

**Process:** RELAY transcribes → ARCHON coordinates → permissions checked → FORGE and ARCHIVIST operate only within granted scopes → task records update → results delivered by text and speech.

### 10.3 Program conversation

**User:** “CL-047, how is the reactor?”

**CL-047:** “The reactor is operating normally. Processing demand has increased, but reserve capacity remains stable.”

That reply must derive from the simulation's current reactor state, not be a plausible-sounding invented measurement.

### 10.4 Incoming security transmission

> **SENTINEL:** “User, a new security alert has appeared. I have prepared the details. No remediation has been attempted.”

The incident can appear in the 3D security district as an in-world event and in the real task/audit interface as a verifiable host alert.

### 10.5 Emergent urban growth

During a long PC session, citizens coordinate material supply, negotiate district power allocation, and build a research facility. The next time the User opens the viewer, the new structure appears; the event history can explain the actual simulation decisions that created it.

---

## 11. Prototype Scope and Acceptance Criteria

### 11.1 Original broad prototype proposal (beyond released Genesis)

Released v0.1.0-genesis covers the world, clock, energy ticks and terminal. Task selection, telemetry, persistence and throttling below belong to later checklist phases.

Proposed MVP scale:

- One city, five districts, twenty simulated programs.
- Host-derived application resource categories and an explicitly specified energy interpretation; earlier Energy/Materials/Data mechanics are superseded by GRID-REQ-005.
- Agent movement and a few utility-based task choices.
- Persistent SQLite world state and event history.
- Fake telemetry source for deterministic testing, then real read-only host telemetry.
- Ability to run without a viewer.

**Acceptance criteria:**

- Twenty agents can independently select legal tasks.
- Telemetry inputs change bounded simulation conditions.
- Programs respond to simulated resource shortages.
- The world remains active without a renderer.
- Saving/restarting preserves state with no offline time jump.
- Events explain significant changes.
- Background resource usage can be measured and throttled.

### 11.2 GCS v0.1 — Text communication

- Communication hub, message types, CLI, recipients, conversations.
- SQLite message persistence and cross-session retrieval.
- Deterministic agent responses for initial development.
- Authorized command dispatch and permission checks.

### 11.3 GCS v0.2 — Speech

- Push-to-talk and microphone selection.
- Offline speech recognition.
- Transcript display and storage.
- Text-to-speech with persistent per-agent profiles.
- Interruption, mute, and error handling.

### 11.4 GCS v0.3 — Intelligent dialogue

- Local/model-agnostic dialogue provider.
- Context retrieval from current world state and attributed memories.
- Auditable action proposals, not direct model execution.
- Consistent conversation continuity across interfaces.

### 11.5 Agent task release

- ARCHON coordinator, SENTINEL security status, FORGE test/build capabilities.
- Durable task queue, approvals and execution audit.
- RELAY delivery and text/voice notifications.
- Add ARCHIVIST and VOLT capabilities next.

### 11.6 3D integration

- Godot digital city with controllable observer camera.
- Program bodies, locations and animated activity.
- Select a citizen to inspect or converse.
- District lights and environmental activity reflect simulation state.
- Optional cinematic post-login startup.

---

## 12. Test Strategy

**Simulation tests**

- Deterministic ticks with seeded randomness.
- Legal state transitions and resource conservation/invariants.
- Agent decision scoring and movement rules.
- Snapshot/restart and event replay.
- Powered-off intervals do not advance simulation time.

**Communication tests**

- Routing by ID, district and broadcast target.
- Unknown recipients and malformed command handling.
- Conversation ordering, deduplication, and recovery.
- Text and transcription produce equivalent logical requests.
- Voice mapping and cancellation.
- Cross-interface history consistency.

**Security and task tests**

- Deny unauthorized directory, file and repository access.
- Reject unapproved operations, argument injection, and arbitrary shell execution.
- Verify dry-run and approval flows.
- Crash recovery and idempotent task dispatch.
- Separate simulated world permissions from actual Windows privileges.
- Verify no model-produced text can bypass tool validation.

**Performance tests**

- Headless agent-count scaling.
- Memory and CPU budgets under high host load.
- Microphone/TTS resource consumption and cancellation.
- Database throughput and snapshot integrity.

---

## 13. Outstanding Design Decisions

The preceding sections describe the desired concept and recommended implementation, not an already finished product. These items still require explicit decisions or measurements:

1. **Visual style:** faithful TRON-like neon imagery versus an original visual identity inspired by digital circuits.
2. **World size and procedural generation:** GRID-REQ-004 selects hardware-informed first creation and saved-world reuse. Exact mappings, scale limits, fallback behavior and later neighborhood expansion remain open; fixed Genesis generation remains the implemented baseline.
3. **Resource mechanics:** exact definitions, production/consumption equations, and recovery rules.
4. **Agent AI:** amount of deterministic utility-based logic versus local-model-assisted planning.
5. **Dialogue model:** local runtime, model size, hardware budget, and fallback when unavailable.
6. **Host telemetry permissions:** exactly what may be observed, retained, and displayed.
7. **Approval policy:** which tasks require one-time approval versus per-run approval.
8. **Schedules:** which tasks may be recurring, and how missed tasks are handled after shutdown.
9. **Notifications:** incoming transmission severity, quiet hours, and permitted spoken alerts.
10. **World governance:** how autonomous programs vote, allocate resources, and propose infrastructure.
11. **Privacy:** retention of conversation transcripts, derived program memories, and telemetry.
12. **Deployment:** separate headless service/process versus user-session application; IPC security model.
13. **Hardware optimization:** precise tick rate, AI inference limits, GPU budget, and throttling thresholds.

---

## 14. Original implementation sequence and current next work

The headless Genesis foundation and post-release editing API are complete. Checklist 1.10 live view and bounded recent events are implemented as of 2026-10-18; sections 2.1 persistence architecture and 2.2 save/recovery are complete; next is 2.3 history and observability. Session visibility filtering remains deferred until login integration. The following original sequence remains context for later communications, agents and viewer work; it is not a list of entirely unstarted projects.

Start with the **headless C# simulation and durable communication kernel**, not the entire 3D city:

1. Create `Grid.Core`, `Grid.Simulation`, `Grid.Persistence`, `Grid.Communication`, `Grid.Security`, `Grid.Terminal`, and `Grid.Tests`.
2. Implement a world clock, five districts, three simulated resources, and twenty simple citizens.
3. Add deterministic fake host telemetry and event logging.
4. Add persistent CLI conversations (`grid status`, `grid programs list`, `grid connect`, `grid talk`, `grid events`).
5. Introduce ARCHON as a coordinator and a task broker that initially exposes only safe mock capabilities.
6. Implement SENTINEL's read-only status inspection and FORGE's narrowly configured test runner.
7. Add push-to-talk, speech recognition, and TTS when terminal communications are stable.
8. Add the Godot renderer last, observing the same world state and conversations.

**Definition of success:** GRID behaves like a genuine simulated society; contains useful, clearly scoped PC agents; supports typed and spoken communication with persistent memory; and remains safe, auditable, recoverable, and unobtrusive on the host machine.

---

## 15. Concept Summary

**GRID = autonomous simulation + host-derived environmental signals + delegated real tasks + persistent conversations + voice interaction + visual digital world.**

Its programs have **three overlapping identities**:

1. **Character:** A personality, relationships, location, and history in the Grid.
2. **Responsibility:** A continuing role within the civilization or host-support team.
3. **Capability:** A tested, permission-controlled service that may perform actual work.

The aspiration is to turn on a PC and find a living digital world that has meaningful things to say, can help accomplish real tasks, and evolves for understandable reasons—without surrendering control of Windows to autonomous AI.
