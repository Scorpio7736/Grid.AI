**User-directed deferral — 2026-11-14:** Copy/transfer and second-PC offline acceptance are deferred until the transfer device is ready. Keep their unchecked acceptance gates open; continue local terminal milestones beginning with 6.32. This is a scheduling change, not a successful portable-device verification or Phase 7 approval.

# Project GRID — Master Development Checklist

**Cross-platform roadmap rebased — 2026-11-17.** Phase 7 is now Grid.Platform cross-platform compatibility, with a dedicated Android ARM64 host/storage adapter as the first target and Linux following the shared contracts. All former Phase 7 City and Phase 8 Integration items are preserved under [Backlog and Future Plans](#backlog-and-future-plans). This is a planning change, not an implementation claim. [Authoritative platform plan](Project_GRID_Cross_Platform_and_Android_Plan.md).

**Terminal-first roadmap placement correction — 2026-11-13; sequencing amended 2026-11-17.** Assistant capability milestones 6.32–6.38 remain at the end of Phase 6. Prioritize terminal-based action routing, local device integration, setup, skills, optional connections, scheduling and permission transparency. The former 6.39 GUI work is optional and has moved to Future Plan C so Grid.Platform is implemented first. Preserve 6.31's existing acceptance requirements; complete the required core milestones before beginning Phase 7.

**Model-handling planning update — 2026-11-04.** Added the supplied seven model candidates, hardware-aware Lite/Standard/Performance eligibility, cold/warm benchmarks, bounded fallback, deterministic delegation, validated artifact generation and offline manifests. Installed models have a 75 GB decimal sub-limit within the shared 128 GB Flash/PC target. All related acceptance remains required before Phase 7. [Model selection and handling plan](Project_GRID_Model_Selection_and_Handling_Plan.md).

**Planning update — 2026-11-04.** Added mandatory sections 6.16–6.30 for all 15 GRID-AI/GRID-Agent components and 6.31 for migration and final acceptance. Phase 7 now requires 6.7–6.31. Specialist model selection is pending the user’s research. The planned storage target is 128 GB decimal for both Flash GRID and PC copies, with flash drives as the primary deployment. Current 20 GB enforcement requires migration in 6.27. [Full plan and acceptance gates](Project_GRID_AI_and_Agent_Platform_Plan.md).

**2026-10-30 transfer/AI follow-up:** Removed repeated full-content transfer checks while retaining independent destination verification and pinned model/manifest matching. Hashing now reports byte progress; ETA uses measured copy/verification work. AI diagnostics retain startup errors, and bundled startup tolerates slow media. Original-PC AI passed an isolated test; the reported failing instance remains undiagnosed pending its exact error. [Implementation, evidence and limits](Project_GRID_Transfer_Checks_and_AI_Diagnostics.md). Physical second-PC acceptance and 6.15 remain pending.

**Progress-window visibility repair — 2026-10-29:** The launcher previously used a hidden-window startup flag, which could hide the WinForms dashboard even though its ready event fired. It now suppresses only the PowerShell console and requests a normal visible GUI. A real Windows test confirmed `IsWindowVisible=true` for the named dashboard, then closed only that test window. All five progress regression tests passed. Failed window startup now prints an explicit text-fallback notice. Restart GRID for the fix; existing transfers are not hot-patched.

**Transfer progress follow-up — 2026-10-29:** Copy/move previews show an approximate duration range before confirmation. A separate Windows dashboard shows progress, elapsed/remaining time and a bounded live activity feed; validation and move cleanup cannot prematurely show completion. Closing the viewer leaves transfer running; text fallback remains available. See [progress window and verification](Project_GRID_Transfer_Progress_Window.md).

**6.14 repair — 2026-10-29:** Development-build transfers now include bundled runtime/models/.NET and available source; launch paths are destination-relative, assets/hash/budget are verified before publication, and device/location changes require grant/approval review. A synthetic-world second-PC package and [acceptance instructions](Project_GRID_Portable_Transfer_Repair_and_Second_PC_Test.md) are ready. Local verification passed 1,049 regression tests, 14 live profile checks and six MCP workflow checks. Second-PC offline/drive-letter/removable-media acceptance remains pending; PDF extraction stays in 6.15. Phase 7 remains blocked.

**6.13 completed — 2026-10-29:** Six explicit agent profiles share one pinned Qwen3 4B Q4_K_M snapshot. CPU-only/reduced-memory operation, role routing and the 20,000,000,000-byte base ceiling are implemented. `ai profiles` and `ai storage` expose configuration and base/data/staging/headroom. **14 live role checks passed**; physical low-RAM/second-device certification remains separate. See [profiles, measured evidence and limits](Project_GRID_Agent_Model_Profiles_and_Base_Budget.md). Earlier planned/fixed-model descriptions are historical; 6.14–6.15 still block Phase 7.

**6.12 completed — 2026-10-29:** MCP can recommend/invite registered specialists with human confirmation, show participants, accept direct specialist planning requests and display recorded MCP progress/results in the same window. Membership changes start a fresh saved history segment; earlier transcripts and private context are not automatically shared. Decline, removal, scope changes, restart and attached-window workflows are tested. **1,031 automated tests and six bundled-model checks passed.** See [workflow, privacy boundary and verification](Project_GRID_MCP_Specialist_Invitations_Implementation.md). Earlier statements that invitations are planned are historical; 6.13–6.15 still block Phase 7.

**2026-10-28 meeting round/addressing follow-up:** Explicit `each of you`, `everyone`, `all of you`, and `you all` requests now generate independent replies for every meeting participant, committed together with one user message. Cancellation/scope changes prevent partial round storage; unavailable inference is labeled per participant. `hey/hi/hello MAXIS ...` routes to MAXIS. While a meeting is connected, `maxis what is your favorite color` is chat; recognized management subcommands such as `maxis workspaces` and `maxis register ...` retain command behavior. Group introductions still use registered roles without model inference. Shared prompts prohibit impersonating other participants, distinguish fictional preferences from observed facts, and ask for a source when a reading request has none. No private context, task execution or automatic invitations are granted. Verification: **1,022 regression tests passed** (752 Terminal), plus bundled-model replay of the reported messages. Local-model answers can still be factually wrong; correct routing is not factual verification.

**2026-10-28 meeting identity repair:** Leading participant names now direct replies (`maxis`, `MAXIS, introduce yourself`) without requiring `@`; absent named agents are refused rather than silently routed to MCP. `each of you introduce yourselves` stores one user message followed by every participant’s registered role introduction, atomically and without AI inference. Ordinary meeting AI receives the authoritative participant roster and roles, with shared history only. General discussion still selects one speaker per turn; this does not add automatic invitations or general multi-agent broadcasts. Verification: 1,017 regression tests passed across the four suites (747 Terminal), and the bundled model correctly handled the reported introduction/name sequence in an isolated world. Evidence: `%TEMP%\GridMeetingIdentityProbe\report.txt`.

**2026-10-28 follow-up — GRID logo animation restored at user request:** Startup/login and shutdown show the supplied GRID7736 artwork when it fits. The five-second login transition progressively reveals the logo from left to right, retains skip/reduced-motion support, and enters the terminal automatically. Narrow windows keep the compact fallback. The new 6.10A colors and responsive help remain. This supersedes the compact-logo replacement described below; other screens retain compact headers.

**6.4 implemented — 2026-10-26:** Original-host `changes` drafts new/revised workspace files from captured context or local MAXIS AI, previews complete content/hashes, requires explicit approval, and applies with backups and guarded rollback. Optional verification uses separately approved MAXIS tasks. **838 Release tests passed**. Drafts are transient; recovery files persist. [Workflow, limits and demo](Project_GRID_Authorized_Editing_Implementation.md).

**6.3A implemented — 2026-10-26:** `screen options/monitoring/ask/status/stop` provides one-shot selected-monitor assistance using offline Windows OCR and local AI. Human Master Control only; no recording, saved screen context or actions. **799 Release tests passed**, including attached-window and synthetic native-OCR checks. User desktop acceptance remains manual. [Commands, limits and verification](Project_GRID_Screen_Assistance_Implementation.md).

**4.2H implemented — 2026-10-26:** Conversation labels are light blue, agent bodies yellow and user text white. Assistance now animates; Enter no longer accidentally skips a reply. **772 Release tests passed**, plus an interactive Windows-console probe. User desktop/window visual acceptance remains open. [Changes, verification and test commands](Project_GRID_Conversation_Display_Repair.md).

**AI response recovery — 2026-10-26:** Truncated answers now get one bounded larger retry, and rejected responses are distinguished from an offline service. Advice and analysis prompts were refined; MCP greetings no longer receive unrelated district counts. **759 Release tests passed**, and the bundled model answered the reported HTML question successfully. [Fix, limits and regression test](Project_GRID_AI_Response_Recovery.md).


**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**Automatic conversation recall added — 2026-10-25:** Ordinary saved discussions and their relevant details now carry across conversations/restarts, alongside explicit memory. Private recall stays within the same agent/project; shared meeting recall requires the exact same group. `memory history` reviews candidates; `memory forget-history` excludes prior discussions while retaining explicit memories and transcripts. **728 tests passed**; bundled-model recall/clearing verified. [Behavior, bounds and test](Project_GRID_Automatic_Conversation_Recall.md). This supersedes earlier explicit-only recall descriptions.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


## New planned interaction and task commands — 2026-10-21

The following user-requested additions were **originally planned** (see the completion notes and checked sections for current status): multi-agent meetings (6.2A), meeting-window integration (6.2A; one-to-one windows/typewriter implemented in 4.2E), monitor selection and screen assistance (6.3A), bulk/agent-filtered task commands (5.8, subsequently completed 2026-10-22), and the transfer command rename (4.2F, subsequently completed 2026-10-22). Existing completed items and verification history remain unchanged. These additions do not grant new agent privileges or automatic task approval.


## System-agent names updated - 2026-10-21

ARCHON is now **MCP** (coordination), FORGE is now **MAXIS** (development), and ARCHIVIST is now **JN** (storage/files). Current commands, examples, plans and checklist labels use the new names. These names do not grant privileges or implement the planned specialist services. The human **Master Control** role remains separate from MCP.

On world load, legacy-named Agent residents are renamed without changing IDs, registrations, context grants, voice-profile links or conversation ownership. Conversation participant labels update; historical messages/sender labels and event history remain intact. The coordinator retains its original identity-generation seed to avoid duplicate residents. Changes persist on the next normal save. A conflicting target name or exhausted revision skips the rename and records `AgentRenameSkipped`; resolve by ID rather than merging identities. Ordinary application/civilian names and the generic Archivist occupation are unchanged.

Verification: 457 tests passed (14 Core, 157 Simulation, 97 Persistence, 189 Terminal), including new legacy-name/idempotence and collision cases. Earlier completion dates and test counts remain historical evidence; documentation examples now use current names.


## Current completion audit - 2026-10-21

Reviewed against the implemented modules, completion notes, source authorization paths and existing verification evidence. **Latest full run: 455 tests passed** (14 Core, 155 Simulation, 97 Persistence, 189 Terminal), recorded in [4.5 verification](Project_GRID_Communications_Verification.md). This reconciliation changes documentation only; tests were not rerun for checkbox edits. Older dated notes and test totals below are historical, not current status.

| Area | Current status |
|---|---|
| Phase 1 - Genesis and terminal follow-ups | Implemented through 1.14, including live-view authorization and authenticated NGE |
| Phase 2 - The Core | 2.1-2.7.2 implemented for their recorded scope; expanded telemetry/application discovery remain open |
| Phase 3 - First Programs | 3.1-3.4 implemented and verified |
| Phase 4 - Communications | 4.1-4.5 and 4.2A-D/4.3A implemented for documented scope; 4.2D edition/disaster acceptance and physical voice checks remain outstanding |
| Phase 5 - System Agents | 5.1 registry/task broker/queue complete for demonstration adapters; 5.2 coordination and real specialist execution remain planned |
| Phase 6 - Intelligence and terminal assistant | Partially implemented; remaining acceptance stays active before platform extraction |
| Phase 7 - Grid.Platform | Planned next: preserve Windows, add Linux adapters and prioritize a dedicated Android ARM64 host/removable-storage flow |
| Backlog - City and integration hardening | Former Phases 7-8 preserved as future plans; no longer the active next phase |

**Reconciled completed entries:** 1.10 session visibility, 1.13 authenticated NGE, 2.4 application resource policy/no artificial drain, Phase 5 human-versus-agent authority separation, 8.1 saved-city loading and no offline catch-up, 8.3 world backup/recovery, and the post-Genesis login/editing and completion-note recommendations. Evidence includes `TerminalHost` live/deletion rechecks, `TerminalIdentitySession` capabilities, `ApplicationResidentTests.ConditionRequiresSourcedBudgetsAndAppEnergyDoesNotDrainOnTicks`, city persistence/recovery tests, and the 4.2A/4.5 verification notes.

**Not marked complete:** real agent tasks/approvals/delegation; unavailable telemetry and comprehensive application discovery; model/3D integration; edition provisioning and cross-PC acceptance; exhaustive failure/release testing; physical microphone, playback and unplug checks. Current terminal identity/audit/context/voice foundations do not satisfy the broader future task-service, AI or deployment requirements.


## Voice transcription repair - 2026-10-21

A user-reported microphone transcription failure was reproduced with generated speech through the real C# helper connection: repeated synchronous `Recognize()` calls could reach an already-finished stream and throw `InvalidOperationException` (0x80131509), discarding recognized text. Recognition now uses `RecognizeAsync(Multiple)` and waits for `RecognizeCompleted`, gathering recognized phrases until completion. Speech/audio remain local and in memory. Explicit typed constructors avoid PowerShell overload ambiguity.

Failures now report a bounded stage, exception type and HRESULT rather than hiding the error or exposing raw helper stderr/audio. Capture reports duration and rejects recordings shorter than 0.25 seconds with a hold-Space hint. `voice 0` is a shortcut for `voice microphone 0`; no-draft hints recognize an already-connected conversation, and fallback advice is no longer duplicated.

**Verification:** 186 terminal tests pass, including four new real silent-PCM/generated-speech/invalid-audio and UI shortcut cases. The generated-speech case reproduced the failure before the completion-event fix and passes after it. No physical microphone recording or speaker playback was used; physical-device verification remains interactive. Earlier 448-test completion counts below are the original milestone's history.


## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all GRID runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md), checklist 4.2B (implemented), active Phase 7, and future plans B.6–B.7. See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

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

## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

New work is tracked in revised 2.4 and added 2.7, with Phase 3/7 acceptance criteria aligned to a changing application population. All completed Genesis and 2.1-2.2 entries remain completed for their original scope; this requirement is not implemented.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


## Documentation integration - 2026-10-18

GRID-REQ-004 is planned: first-time world creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates a unique city using those components, and saves the resulting world for reuse on later startups. Section 2.6 tracks the work; no implementation is claimed. The [design specification](Project_GRID_Design_Specification.md#grid-req-004---hardware-generated-city-contract) is the shared contract for all eight documents.

The user identified this repository's MD Project Files folder as the supplied document location. Its current eight overview/checklist/design/development/storage/city/environment/fashion documents are the integration baseline. Older Downloads copies were compared and contain no GRID-REQ-004; they were not used to overwrite newer repository content. There is no separate updated copy in that location to merge against. The explicit requirement in the user's message supplies the new functionality.

Preserved all existing completed checklist entries, historical test results, editing/session contracts, newer concept images and source changes. At that documentation review, Grid.Persistence was a project scaffold (commit 42dc69a). Sections 2.1-2.2 below record subsequent schema and save/recovery implementation. That earlier requirement-integration update changed Markdown only and did not run runtime tests; later implementation evidence is recorded in the relevant sections.

Resolved planning overlaps: fixed Genesis generation remains the implemented baseline; hardware-derived first creation is future work. Existing saves take priority over regeneration, and hardware does not override the established visual guides. Exact component mappings, discovery APIs and fallback policy remain open design decisions.

## Implementation status - 2026-10-17

Documentation reconciled on 2026-10-17 against local HEAD 0b659e6 and current working-tree changes. Genesis 1.1-1.8 is complete; 1.9.1-1.9.2 are completed designs and 1.9.3 is implemented. At that review, section 1.10 was unimplemented; see the 2026-10-18 completion notes below for its subsequent implementation. Current solution verification passed 117 tests (8 Core, 85 Simulation, 24 Terminal). Historical completion counts below describe their original verification dates, not the current total.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Project:** GRID — Local-first Digital Civilization and Personal Task Assistant  
**Roadmap:** Eight milestones  
**Original checklist baseline (historical):** Local repository HEAD `0b659e6` plus working-tree changes reviewed on 2026-10-17. Released Genesis is tagged `v0.1.0-genesis` at `0c1de64`; post-release API/startup changes are not part of that tag. Remote synchronization was not checked during this documentation audit.
**Status convention:** `[x]` = observed in the reviewed repository or current working tree (independent verification is noted where performed); `[ ]` = pending or not verified.
**Scope:** Full eight-phase roadmap, including GRID-REQ-002 through GRID-REQ-008, terminal/context/voice follow-ups, and current implementation through 4.5.

> **Architecture principles:** C#/.NET owns authoritative world state; agent behavior does not require AI; Windows tasks are explicitly permission-scoped and auditable; essential operation is local-first and requires no subscriptions; the 3D viewer observes the simulation instead of owning it. Simulation time advances only while GRID operates.

## Overall roadmap

| Milestone | Name | Deliverables | Original rough solo part-time estimate |
|---|---|---|---|
| 1 | Genesis | Repository, world models, simulation tick, terminal, unit tests | 1–2 weeks |
| 2 | The Core | SQLite persistence, snapshots, events, recovery | 2–3 weeks |
| 3 | First Programs | Needs, jobs, movement, utility-based autonomy | 2–4 weeks |
| 4 | Communications | Messaging, context intake, conversation persistence, voice | 2–4 weeks |
| 5 | System Agents | MCP, MAXIS, SENTINEL, JN, VOLT, RELAY | 3–5 weeks |
| 6 | Intelligence | Shared local AI, grounded assistance, safe edit proposals | 2–4 weeks |
| 7 | The City | Godot 3D streets, districts, inhabitants, interactions | 4–8 weeks |
| 8 | Integration | Windows lifecycle, resource control, backups, release | 3–5 weeks |

*Estimates are provisional. The added assistant and file-editing capabilities, plus asset production, may expand effort.*

---

# Phase 1 — Genesis

## 1.1 Repository and environment

- [x] Install Visual Studio and .NET 10 SDK
- [x] Create the Grid7736 Git repository
- [x] Create the Visual Studio solution
- [x] Create Grid.Core (.NET class library)
- [x] Create Grid.Simulation (.NET class library)
- [x] Create Grid.Terminal (.NET console application)
- [x] Configure Terminal → Simulation → Core references
- [x] Configure .gitignore and .gitattributes
- [x] Add and organize project design documents

**Completed — commit history reviewed and current implementation verified on 2026-10-16**

- Repository configuration was introduced in [`0f86ab4`](https://github.com/Scorpio7736/Grid7736/commit/0f86ab4fe49401282fd398f4d019e6ce597e848f) (`.gitignore` and `.gitattributes`).
- [`562e51d`](https://github.com/Scorpio7736/Grid7736/commit/562e51d4e4ed006fc462e4fb1818a4c26e38c3f8) established the solution and three .NET 10 SDK projects. [`442632c`](https://github.com/Scorpio7736/Grid7736/commit/442632ca7c25398c5eb441928c06a2806992f524) configured Grid.Terminal as an executable with a console entry point.
- [`0ccb1e2`](https://github.com/Scorpio7736/Grid7736/commit/0ccb1e2dda4d852f28473800008cac729ccc03e0) established Terminal → Simulation → Core project references. [`f365151`](https://github.com/Scorpio7736/Grid7736/commit/f365151a8425314ebb531259cdd42941f0192f6d) added the design documents and solution document folder.
- Verification: GitHub `master` was checked with `git ls-remote` and matches local commit `f5ee323`. Installed SDK reports `10.0.401`; the current solution built successfully in this session with zero warnings and zero errors.
- Git history establishes project setup, not a historical installation record. Visual Studio installation was not independently checked; the existing installation checkbox is retained from the original checklist.

## 1.2 Program model

- [x] Implement GridProgram.cs
- [x] Add unique Guid identity
- [x] Add name and occupation
- [x] Add energy and current district ID
- [x] Implement GridProgramStatus enum
- [x] Add terminal checks for program IDs and initial defaults *(historical demo source verified in f365151; current automated verification documented below)*

**Completed — commit history reviewed and current implementation verified on 2026-10-16**

- [`f365151`](https://github.com/Scorpio7736/Grid7736/commit/f365151a8425314ebb531259cdd42941f0192f6d) introduced `GridProgram.cs` and `GridProgramStatus` with Idle, Working, Moving, Recharging, and Offline states.
- The model provides an init-only GUID identity, name, occupation, energy defaulting to 100, district ID, and status defaulting to Idle.
- That commit included a two-citizen terminal demonstration printing identity uniqueness, default energy, and default status. Its source was inspected; its historical execution was not independently observed. The demonstration was subsequently replaced by the district demo.
- Verification: the eight current World Management tests passed in this session, including factory citizen identities, energy/status defaults, valid district membership, and duplicate-ID rejection. These are current automated results, not evidence of a historical terminal run.
- Section 1.4 subsequently restricted the district setter and introduced world ownership to protect placement consistency.

## 1.3 District model

- [x] Implement GridDistrict.cs
- [x] Add identity, name and description
- [x] Add district population IDs
- [x] Add fictional resource dictionary
- [x] Initialize the five starting district objects
- [x] Verify district GUID uniqueness in terminal *(executed successfully on 2026-10-16; result: True)*

**Completed — commit history reviewed and terminal verification performed on 2026-10-16**

- [`a11f394`](https://github.com/Scorpio7736/Grid7736/commit/a11f3940a1ec87fd720b9820f5720f3a400679bf) introduced `GridDistrict.cs` with init-only GUID identity, name, description, program membership IDs, and a fictional resource dictionary.
- The same commit initialized The Core, Processing Sector, Memory Banks, Archive Sector, and Gateway in the terminal, printing their details and checking district ID uniqueness.
- Verification: `dotnet run --project Grid.Terminal/Grid.Terminal.csproj --no-build --no-restore` completed successfully in this session and printed `Districts: 5` and `Unique district IDs: True`. Each standalone demo district reported zero population, as expected.
- Current automated World Management tests also passed, covering factory district initialization, membership, duplicate rejection, and transfers. Section 1.4 subsequently made district membership read-only to external callers.
- At section 1.3 completion, the terminal demonstration was separate from the factory-created world; section 1.7 now connects the terminal to that world.

## 1.4 World management

- [x] Create GridWorld.cs
- [x] Store authoritative program and district collections
- [x] Implement AddDistrict() with duplicate prevention
- [x] Implement AddProgram() with district validation
- [x] Implement GetProgram() and GetDistrict()
- [x] Implement MoveProgram() with membership consistency
- [x] Restrict direct modifications that bypass world rules
- [x] Create GridWorldFactory.cs
- [x] Initialize all five districts through the factory
- [x] Generate twenty uniquely identified programs
- [x] Distribute citizens across valid districts
- [x] Verify transfers preserve population totals

**Completed and verified — 2026-10-16**

- `GridWorld` owns read-only program and district dictionaries. Additions reject null entities, empty or duplicate IDs, invalid district references, and entities already owned by a world.
- `MoveProgram()` updates source membership, destination membership, and the program location together. Moving to the current district is a no-op; invalid requests leave membership unchanged.
- `GridDistrict.ProgramIds` is read-only to callers, and `GridProgram.CurrentDistrictId` has an internal setter to prevent external placement changes.
- `GridWorldFactory.CreateGenesis(seed)` creates five districts with four citizens each. Equal seeds reproduce IDs and placement in independent world instances.
- Added `Grid.Core.Tests` to the solution. Eight automated tests cover initialization, seeded reproducibility, transfers and population totals, invalid requests, duplicate IDs, world ownership, and read-only membership access.
- Verification: `dotnet test Grid.AI.slnx --verbosity minimal -p:NuGetAudit=false` passed all eight tests. `dotnet build Grid.AI.slnx --no-restore --verbosity minimal` succeeded with zero warnings and zero errors.

At section 1.4 completion, the terminal still used its original district demonstration; factory integration is now complete in section 1.7.

## 1.5 Simulation clock

- [x] Create GridClock.cs
- [x] Track simulation tick count and elapsed simulation time
- [x] Implement Start() and Pause()
- [x] Implement manual single-step advancement
- [x] Make tick duration configurable, initially one second
- [x] Keep simulation time independent of the real Windows clock

**Completed and verified — 2026-10-16**

- Added `Grid.Simulation/GridClock.cs` with read-only tick duration, tick count, elapsed simulation time, and running state.
- New clocks start paused at zero ticks. Tick duration is configured at construction, defaults to one second, and must be positive; keeping it fixed preserves the meaning of elapsed ticks.
- `Start()` and `Pause()` are idempotent and preserve accumulated time. `TryAdvance()` advances exactly one fixed tick while running and returns false without changes while paused.
- `Step()` advances one tick while paused and leaves the clock paused. Calling it while running throws without changing time.
- Time advances only through explicit advancement calls; there are no Windows clock reads, background timers, or offline catch-up. Callers must use a single simulation thread.
- Elapsed time uses exact TimeSpan ticks. Overflow is checked before either counter changes.
- Added `Grid.Simulation.Tests` to the solution. All 13 clock test cases passed, covering defaults, invalid durations, exact fixed steps, repeated start/pause, resume, manual steps, and overflow.
- Verification: `dotnet test Grid.AI.slnx --verbosity minimal -p:NuGetAudit=false` passed all 21 tests (13 simulation clock and eight World Management). `dotnet build Grid.AI.slnx --no-restore --verbosity minimal` succeeded with zero warnings and zero errors.

Automatic scheduling and world updates are now implemented in section 1.6; terminal clock commands are now implemented in section 1.7.

## 1.6 Simulation engine

- [x] Create GridSimulationEngine.cs
- [x] Advance the authoritative world with fixed simulation steps
- [x] Define a clear world-update sequence
- [x] Generate actual world events during ticks
- [x] Prevent updates while paused
- [x] Support safe cancellation and graceful stopping
- [x] Avoid dependence on any 3D renderer or AI service

**Completed and verified — 2026-10-16**

- Added `Grid.Simulation/GridSimulationEngine.cs` around the authoritative `GridWorld` and a privately owned `GridClock`. Engine operations serialize updates; callers must not mutate the world directly or share it between engines while it is in use.
- Defined the tick sequence: calculate the next simulation timestamp; validate programs and stage changes in stable GUID order; check cancellation; advance the clock and apply all staged energy changes; expose the completed tick's immutable event batch. Validation failures, pre-commit cancellation, and time overflow leave world changes, clock counters, and the previous result untouched.
- Initial Genesis energy rules: Working and Moving consume one energy unit per simulated second; Recharging restores two; Idle and Offline leave energy unchanged. Values clamp to 0–100, and invalid energy/status inputs are rejected. These are initial deterministic rules; autonomous job selection, travel, and needs behavior remain in Phase 3.
- Added `GridTickResult`, `ProgramEnergyChanged`, and `SimulationTickCompleted`. Events contain actual committed changes and simulation timestamps; unchanged energy emits no change event. The engine retains only the latest tick result, avoiding an unbounded in-memory history.
- `Start()` and `Pause()` control normal ticks. `Tick()` returns null while paused; `Step()` explicitly advances once while paused and rejects use while running. Pause waits for any active commit to finish.
- `RunAsync(cancellationToken)` provides cancellable scheduling with at most one fixed simulation step per completed delay, without missed-time catch-up. Pause/resume invalidates a pending wait. Cancellation completes normally, pauses the clock, and releases scheduler ownership; other failures propagate after the same cleanup. Callers await the run task before releasing the world.
- Duplicate schedulers and competing external `Tick()` calls during scheduling are rejected. Automatic scheduling accepts durations from 1 ms through 4294967294 ms; manual advancement supports the clock's wider positive TimeSpan range.
- Scheduling uses an injectable `TimeProvider`; the engine has no AI, renderer, or host telemetry dependency. Tests control timer completion directly rather than sleeping.
- Verification: `dotnet test Grid.AI.slnx --verbosity minimal -p:NuGetAudit=false` passed all 45 tests: eight World Management, 13 clock, and 24 engine cases. Coverage includes energy rules and bounds, genuine events, deterministic results, membership preservation, pause/resume, manual stepping, cancellation, scheduler exclusivity, invalid-state recovery, and overflow.
- `dotnet build Grid.AI.slnx --no-restore --verbosity minimal` succeeded with zero warnings and zero errors.

Terminal command integration is now complete in section 1.7. Persistence and durable event history remain in Phase 2.

## 1.7 Terminal interface

- [x] Replace temporary sample code with a command loop
- [x] Implement help, status and programs commands
- [x] Implement inspect command for a program or district
- [x] Implement start and pause commands
- [x] Implement manual tick/step command
- [x] Implement exit with graceful shutdown
- [x] Handle invalid commands without crashing
- [x] Display genuine world state, never fabricated status

**Completed and verified — 2026-10-16**

- Replaced the district demo with a factory-created world, simulation engine, and interactive terminal. The initial world is paused with five districts and twenty citizens.
- Added a command registry (`TerminalCommand`, `CommandDispatcher`), built-in command registration (`DefaultCommands`), and a separate `TerminalHost` for input/output and scheduler lifetime. New commands register their metadata and handler; help updates automatically without changing the input loop.
- Implemented `help`, `status`, `programs`, `districts`, `inspect`, `start`, `pause`, `step`, `tick`, and `exit`. Inspect accepts a program/district ID or name; names can contain spaces or quotes. Duplicate names require a full ID.
- Command names and name matching ignore case. Blank input is harmless; unknown commands, invalid argument counts, unmatched quotes, missing entities, and stepping while running produce readable responses without ending the session.
- Added `GridSimulationEngine.GetSnapshot()` and detached read-only snapshot records. Status and inspection use a consistent view captured under the same lock as tick commits, without pausing the running simulation or exposing mutable world collections.
- Start enables the scheduler; pause stops automatic advancement; step/tick advances once while paused. Output reports actual simulation state and committed events. Idle citizens remain Idle rather than receiving fabricated activity.
- Exit, EOF, and Ctrl+C cancel and await scheduler shutdown. Scheduler failures are reported with a nonzero process result. Console reads run off the control thread so blocked input cannot prevent shutdown; a pending background read can remain until input arrives or the process exits.
- Added `Grid.Terminal.Tests` to the solution and a README with run instructions, command examples, lifecycle behavior, and the command extension pattern.
- Verification: `dotnet test Grid.AI.slnx --verbosity minimal -p:NuGetAudit=false` passed all 69 tests (eight World Management, 37 clock/engine, and 24 terminal cases). Terminal coverage includes parsing, actual output, extension registration/help, snapshots, invalid input, exit, EOF, blocked-input cancellation, and scheduler failure.
- `dotnet build Grid.AI.slnx --no-restore --verbosity minimal` succeeded with zero warnings and zero errors. Executable smoke checks verified help, status, district/program inspection, two manual steps, invalid-command recovery, and clean exit. A live redirected process advanced automatically to tick 2, paused, reported its real state, and exited successfully.

World state remains in memory; persistence is planned for Phase 2. The extension point is an in-process command registry, not a dynamic plugin loader. Genesis release is marked by the annotated v0.1.0-genesis tag.

### Terminal startup completion update - 2026-10-17

- [x] Add optional case-insensitive `--start` launch argument; default remains paused.
- [x] Verify `status` reports Running when launched with `dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start`.
- Full solution regression verification after this change: 117 passed, zero failed or skipped. This starts simulation ticking; it does not implement the 1.10 live view.

## 1.8 Genesis tests and release

- [x] Create Grid.Core.Tests xUnit project
- [x] Create Grid.Simulation.Tests xUnit project
- [x] Test uniqueness of IDs
- [x] Test district/program initialization and membership
- [x] Test duplicate and invalid district/program rejection
- [x] Test movement and population invariants
- [x] Test simulation clock and fixed-step advancement
- [x] Test pause, resume and manual stepping
- [x] Test terminal command parsing and outputs
- [x] Run dotnet build successfully
- [x] Run dotnet test successfully
- [x] Add Genesis usage instructions to README
- [x] Tag v0.1.0-genesis

**Genesis release verification — 2026-10-16**

- Release tag: `v0.1.0-genesis` (annotated), covering world management, the simulation clock and engine, and the expandable terminal interface.
- Re-ran all 69 tests successfully and built the full solution with zero warnings and zero errors before tagging.
- Existing executable checks verified real factory state, inspection, manual stepping, automatic ticks, pause, invalid-command recovery, and clean shutdown.
- The release starts a fresh in-memory world; persistence remains Phase 2 work.

**Genesis release gate:** A console simulation with five districts and twenty unique programs; start, pause, tick, status, programs, inspect, and exit work using actual state; automated tests pass.

---

## 1.9 Post-Genesis preparation — Terminal identities and administrative editing

**Follow-up status:** Sections 1.9.1–1.9.2 designs and the 1.9.3 editing API are complete. Authenticated terminal editing and login remain deferred. These preparations support GRID-REQ-003 and the User's Master Admin editing request. They are follow-up work after the completed `v0.1.0-genesis` release, not missing Genesis acceptance criteria. Preserve the existing release tag.

### 1.9.1 Define editable values and behavior

- [x] Specify initial editable fields: name, occupation, energy, status, and district assignment
- [x] Document field types, valid ranges/statuses, text constraints, and invalid-input behavior
- [x] Keep stable IDs and ownership references outside ordinary editing; keep authorization settings separate from profile values
- [x] Define one-time edits versus persistent overrides and how autonomous updates may change edited values
- [x] Define preview and conflict behavior when the target changes before an edit is applied
- [x] Design editable-field metadata so future fields can be added without rewriting the terminal loop

**Design completed — 2026-10-16**

- Defined the implementation contract in [Program Editing Rules](Project_GRID_Program_Editing_Rules.md), including the five initial fields, text limits, allowed statuses, finite 0–100 energy, district validation, and invalid-input results.
- Selected one-time assignments for the first version; ordinary edits do not pin values against future simulation behavior. Persistent overrides remain deferred.
- Defined exact previews and whole-program revision checks, including conflict rejection, atomic multi-field updates, no-op behavior, explicit pause control, and world/session scope.
- Defined an expandable typed field registry for metadata, parsing, formatting, validation, capability references, and staged mutations. Identity and authorization settings remain separate from profile values.
- Verification: reviewed current GridProgram properties/status values, GridWorld transfer rules, GridSimulationEngine energy updates, and terminal name/ID lookup behavior against the specification. Included concrete acceptance scenarios for later implementation tests.
- This completes design only. No source code was changed or runtime tests run for 1.9.1; field enforcement, revision tracking, and editing operations were pending at design completion and are now implemented in 1.9.3. Session-context planning is now defined in 1.9.2.

### 1.9.2 Plan session context and authorization boundaries

- [x] Define a session-context contract separating authenticated human operator, active program identity, and effective capabilities
- [x] Define how command handlers and engine/application services receive session context
- [x] Specify Master Control editing permissions and denial behavior for agent or unauthenticated sessions
- [x] Define authorization checks at the service boundary rather than relying on command visibility
- [x] Identify future audit fields: operator, active identity, target, old/new values, simulation timestamp, and outcome
- [x] Document that session contracts alone do not implement authentication; do not label a placeholder identity as authenticated

**Design completed — 2026-10-16**

- Defined [Session Context and Authorization Contract](Project_GRID_Session_Context_and_Authorization.md): verified human operator, active Master Control/agent identity, session generation, world scope, policy version, and effective capabilities.
- Defined explicit context propagation through terminal dispatch into services, with a trusted session resolver and authorization checks at preview and apply. Command visibility alone cannot authorize an action.
- Defined initial capability policy: administrative program edits and world control require Master Control; acting as an agent does not inherit the human's administrator rights.
- Defined logout, switching, revocation, preview invalidation, concurrent-session isolation, and authorization/commit coordination.
- Specified audit attribution and old/new value records, distinguishing human-as-agent, autonomous agent, and simulation-system actions. Durable audit implementation remains deferred.
- Verification: reviewed TerminalCommand and CommandDispatcher signatures, engine integration boundaries, and the 1.9.1 editing contract for consistency. Added required acceptance scenarios for future implementation.
- Design only: no source changes or runtime test execution. Actual credential verification and login are not implemented; absent a trusted provider, new administrative operations must deny access. Test-only session resolvers must not become a default production administrator.

### 1.9.3 Implement validated program editing API - complete

- [x] Add typed program-edit requests and results through the engine/application boundary
- [x] Validate all requested values before committing any part of an edit
- [x] Serialize edits with simulation ticks and reject stale/conflicting requests
- [x] Route district changes through world transfer rules to preserve membership and population
- [x] Review public setters on program values and restrict mutation paths that bypass validation, while preserving factory initialization and engine updates
- [x] Return actual committed old/new values for later terminal output and audit integration
- [x] Keep this API separate from future login UI and credential storage; do not expose unauthenticated administrative terminal commands
- [x] Test valid edits, invalid values, all-or-nothing changes, unchanged IDs, district consistency, tick coordination, and authorization enforcement when session checks are connected
- [x] Run the existing world, clock, engine, and terminal regression tests after changes
- [x] Add completion notes with observed test/build results once this preparation is implemented

**Implemented and verified — 2026-10-17**

- Added typed field values, edit requests, immutable previews, structured outcomes, and committed receipts through `GridSimulationEngine.PrepareEdit()` and `ApplyEdit()`.
- Added an explicit typed field registry for name, occupation, energy, status, and district, including normalization, metadata, decimal parsing, round-trip formatting, and validation. Energy text is limited to 1024 characters and bounds are checked exactly before float conversion.
- Added per-program revisions and a world instance ID. Successful changing transactions increment revision once; ticks and transfers invalidate stale previews, while no-ops and unrelated changes do not.
- Edits and world mutations share the engine/world lock. Multi-field edits validate fully before commit, preserve IDs and population, and return actual before/after values. Edits do not advance time or replace the last tick result.
- World-owned program setters now reject direct external changes; unowned initialization remains supported. District changes use the world transfer path. Existing tests were adapted to initialize before ownership or use authorized test editing.
- Added an injectable authorization lease contract with default-deny behavior. Trusted providers must verify authentication, current programs.edit capability and world scope, and hold authorization validity through commit. The engine also checks Master Control identity and session/world bindings. The test provider is compiled only into test projects.
- Preview/apply check current authorization separately. Revoked sessions, agent sessions, changed session generations, foreign previews, replayed consumed previews, stale revisions and invalid values do not commit changes. Cancellation is honored before commit, never midway through a mutation.
- Receipts carry request/operator/session identity, revisions, old/new values, simulation tick/time and UTC time for later audit integration; these receipts are not a durable audit log or live-event buffer.
- Verification: `dotnet test Grid.AI.slnx --verbosity minimal -p:NuGetAudit=false` passed all 117 tests: eight World Management, 85 simulation (including 48 new editing cases), and 24 terminal cases. Tests cover validation, atomicity, no-ops, one-time behavior, membership, authorization, revocation/commit ordering, preview isolation, concurrency with ticks, and unchanged terminal behavior.
- `dotnet build Grid.AI.slnx --no-restore --verbosity minimal` succeeded with zero warnings and zero errors. The executable status/step/inspect/exit smoke check also passed.
- Login, credential verification, a production authenticated-session provider, terminal edit commands, persistence, and durable audit remain deferred. Existing Genesis commands keep their current behavior; newly added administrative APIs deny access by default.

**Deferred implementation:** Actual login and credential handling, identity switching UI, saved roles, and durable audit history remain in the planned persistence/session/system-agent work. Sections 4.2A and 5.1 describe that later integration; do not mark those capabilities complete merely because these contracts or APIs exist.

---

## 1.10 Post-Genesis terminal live view

**Implemented and verified - 2026-10-18.** Requested on 2026-10-17. Session-dependent visibility remains deferred until login integration.

- [x] Add a live-view command or option to enter the updating display
- [x] Continuously show running/paused state, simulation tick, elapsed simulation time, and population
- [x] Show current program activities, statuses, locations, energy, and actual changes
- [x] Include a compact recent-events list with simulation timestamps and relevant program names/IDs
- [x] Show real program actions, state changes, transfers, and administrative edits when those events exist; do not invent autonomous activity
- [x] Distinguish completed events from ongoing activity; initially show Idle honestly where no behavior has been implemented
- [x] Add a bounded recent-event buffer with stable sequence IDs so events between display refreshes are not lost or duplicated; keep durable history in Phase 2
- [x] Keep rendering refresh rate separate from simulation ticks; use consistent snapshots without advancing or pausing simulation merely to redraw
- [x] Add an easy key to leave live view and return to the command prompt without stopping the world
- [x] Handle Ctrl+C, terminal resizing, and redirected/unsupported output with a clean fallback
- [x] Avoid flooding scrollback or retaining an unbounded event list; keep refresh/resource usage modest
- [x] Apply session visibility rules: live view requires Master Control, checks authorization while open, and exits on revoked/expired access (4.2A/4.2C)
- [x] Test refresh consistency, event ordering, bounded history, paused display, and return-to-prompt behavior
- [x] Add completion notes and verification results after implementation

**Completion notes - 2026-10-18**

- Added `live` through command registration, with pure formatting and an injectable interactive terminal surface. Refresh runs every 250 ms independently of ticks; Q/Esc returns to commands without changing clock state. Page Up/Down pages citizens; Left/Right pages retained events.
- Added an atomic live snapshot and a bounded 256-event buffer (configurable 1-10,000), stable per-engine sequence IDs and explicit eviction count. Records actual tick/energy changes, clock transitions, direct world transfers and applied edit summaries. No-op/failed edits do not fabricate changes.
- Shows real current statuses, energy, occupation, district and simulation time; Idle does not imply autonomous work. Prior LastTick and edit receipt contracts remain intact. Recent events are not durable history.
- Redraw fits the console, updates only changed rows, restores the cursor and handles resize, cancellation and scheduler failure. Redirected/TERM=dumb/unsupported or too-small surfaces show one snapshot and return to commands. Text is sanitized to single-cell ASCII and clipping is marked.
- Verification: `dotnet test Grid.AI.slnx --no-restore --verbosity minimal -p:NuGetAudit=false` passed 140 tests (8 Core, 93 Simulation, 39 Terminal), zero failed/skipped. Added 23 event and live-view cases. Solution build succeeded with zero warnings/errors.
- Interactive smoke checks verified running ticks, Q return, paused view and exit; TERM=dumb fallback also passed. Resize/cancellation and scheduler-failure paths have injected-console tests.
- See [Terminal Live View](Project_GRID_Terminal_Live_View.md) for controls, architecture, limitations and verification details. Login visibility, persistence, autonomous actions and GRID-REQ-004 remain deferred; earlier completion notes remain historical.

---

## 1.11 Post-Genesis terminal usability and live-view navigation

**Implemented and automatically verified - 2026-10-19.** Improve readability and make navigation easier to discover across the command prompt and live view. This extends completed 1.7/1.10 without changing their completion history.

- [x] Improve spacing, headings, concise labels and help text so important information is easy to scan
- [x] Present comparable data in aligned tables where helpful: programs, districts, resources, behaviors and event/history listings
- [x] Choose useful default columns; keep full IDs and detailed provenance available through inspection rather than crowding summary rows
- [x] Adapt tables and live panels to terminal width/resize with sensible wrapping, clipping indicators or paging; retain readable narrow-terminal/plain-text fallbacks
- [x] Add consistent color coding for statuses, healthy/warning/error conditions, unknown/stale observations and event types
- [x] Keep text labels alongside colors, use readable contrast, and provide a no-color mode; suppress terminal color/control sequences in redirected or unsupported output
- [x] Make live-view navigation prominent: display an unambiguous Back to command prompt control and distinguish it from exiting GRID
- [x] Preserve Q/Esc as back-to-prompt controls, restore the cursor/prompt cleanly, and allow repeated live → prompt → live navigation
- [x] Verify leaving live view preserves running/paused state, telemetry, autosave and the world session; quitting remains a separate explicit action
- [x] Verify usability in interactive and redirected output, narrow/resized terminals, long names/IDs, empty tables and high event volume
- [x] Update help, README and live-view documentation with the final controls, palette/table conventions and dated completion evidence

**Completion verification:** 290 Release tests passed, including repeated live/prompt navigation, retained clock/world/persistence session, table widths, safe/plain output and color policy. A disposable PTY smoke confirmed fallback-to-prompt with the clock still running. Full-screen colors/contrast in the user's terminal remain a manual visual check; the PTY did not support live redraw. See [Terminal Live View](Project_GRID_Terminal_Live_View.md#terminal-usability-completion---111-2026-10-19) for controls, palette and evidence.

---

## 1.12 Terminal command reference, population filters and farewell

**Implemented and verified - 2026-10-19.** Extends completed 1.11 without changing its completion record.

- [x] Add display-only `clear` with safe plain-output fallback
- [x] Group `help` into Simulation Control, World & Population, Monitoring & Analytics, System & Terminal, and Command Options
- [x] Add `programs -active` for Running application residents
- [x] Add `programs -possible` for installed/previously observed applications, retaining unknown/provenance labels
- [x] Add `programs -sleeping` for On call and Sleeping application residents, keeping their labels distinct
- [x] Preserve bare `programs`, support `ids` with one filter, and validate flags/empty results
- [x] Persist minimal previously seen application catalog metadata across restarts separately from live/resident state; verify migration and stale/uninstalled entries
- [x] Add a GRID7736 ASCII shutdown banner with truthful save/failure/ephemeral messages and no blocking key prompt; updated to the user-supplied artwork with light-blue default text (75 terminal tests passed)
- [x] Verify normal-terminal versus Visual Studio host behavior; do not claim the application can replace Visual Studio's post-exit footer
- [x] Test help/filter/clear/catalog/shutdown behavior and update dated completion notes when implemented

See [Terminal Command and Shutdown Plan](Project_GRID_Terminal_Command_and_Shutdown_Plan.md) for the command layout, filter semantics, proposed artwork, persistence dependency and verification criteria. Completion evidence: **297 Release tests passed**; built-terminal save/reopen/filter/help/farewell smoke passed. Clear success/fallback is tested with an injected screen action; the tool PTY used fallback. User-console visual assessment remains separate. Schema v6 catalogs metadata without restoring ordinary resident activity. Visual Studio retains control of its post-exit footer.

---

## 1.13 NGE - Nullify Grid Existence

**Initial local confirmation implemented and verified - 2026-10-19.** Delete the current GRID world's save after a clear warning and explicit y/n confirmation. Initial local-operator confirmation is not authenticated admin enforcement; future access is human user admin / Master Control only.

- [x] Add case-insensitive `NGE` with an interactive warning naming the world and exact deletion paths; default to No
- [x] Safely handle cancellation, invalid answers, redirected input and ephemeral sessions
- [x] Validate the active-world deletion manifest, including managed backup/recovery artifacts; preserve unrelated files/worlds
- [x] Stop workers and serialize persistence; suppress final save and prevent save recreation during shutdown
- [x] Handle partial failures/interruption explicitly; exit without replacement-world creation or false success claims
- [x] Show NGE-specific GRID7736 farewell instead of saved-world messages
- [x] Verify a later startup creates a fresh world and that cancel preserves the existing world
- [x] Enforce authenticated human Master Control for NGE, including authorization recheck before deletion; agent sessions cannot invoke it (4.2A)
- [x] Complete disposable-world regression tests and dated documentation verification

See [NGE plan](Project_GRID_NGE_Nullify_Grid_Existence_Plan.md) for confirmation, ownership, lifecycle and future authorization details. **Completion:** 306 Release tests passed; all deletion checks used disposable worlds. Current deletion covers the active primary, rolling backup and exact SQLite sidecars; historical archives/external copies remain. A failure marker blocks startup/recovery after interruption and requires manual review; automatic retry is deferred. No user save was deleted.

---

## 1.14 Startup environment-variable launcher

**Implemented and verified - 2026-10-20.**

- [x] Ensure Windows user variable `Grid7736` points to an available terminal executable at startup
- [x] Preserve valid existing installation paths; repair missing/empty/broken values without writing shell quotes
- [x] Keep environment-update failures nonfatal and skip Windows-specific registration on other platforms
- [x] Document PowerShell `& $env:Grid7736`, Command Prompt `"%Grid7736%"`, refresh behavior and development-build path limitations
- [x] Verify behavior with injected environment tests and an ephemeral Windows startup

**Completion evidence:** 87 terminal tests passed. Actual user variable registered to the Release apphost and its existence verified. The smoke session did not open the user's saved world. Follow-up: managed `Grid7736.cmd` and deduplicated user PATH registration now support the bare `Grid7736` command. 89 terminal tests passed and a bare-command ephemeral launch succeeded.

---

# Phase 2 — The Core

## 2.1 Persistence architecture

**Implemented and verified - 2026-10-18.** SQLite schema and migration foundation complete; engine save/load remains 2.2.

- [x] Create persistence layer/project when needed
- [x] Select and integrate SQLite
- [x] Design tables for districts, programs and resources
- [x] Design simulation-state and clock storage
- [x] Establish primary keys and relationships
- [x] Enable transactions and foreign-key enforcement
- [x] Create a versioned database schema and migrations

**Completion notes - 2026-10-18**

- Replaced the empty Grid.Persistence scaffold with SqliteGridDatabase and GridSchema, retaining Microsoft.Data.Sqlite 10.0.13. Added persistence and its test project to Grid.AI.slnx.
- Added STRICT schema v1 for Worlds, Districts, Programs, DistrictResources and SimulationStates, plus migration history. Composite keys enforce same-world membership; program district references are the single stored membership source.
- Stores energy/status/revision and integer simulation clock values with constraints. Durable world identity is distinct from runtime instance/session identity. Seed/version provenance is optional; hardware profiles/layouts remain future migrations.
- Explicit initialization creates schema only. OpenExisting refuses missing/uninitialized databases. Every managed connection enables foreign keys, FULL synchronization, private cache and a bounded busy timeout.
- Migration metadata, DDL and version markers share an immediate transaction. Unknown/future/inconsistent databases are rejected; failed upgrades roll back. Tests cover concurrent initialization and synthetic next-version migration; only v1 ships.
- Verification: 31 new persistence tests passed. Full solution `dotnet test Grid.AI.slnx --verbosity minimal -p:NuGetAudit=false` passed 171 tests (8 Core, 93 Simulation, 39 Terminal, 31 Persistence), zero failed/skipped. Solution build passed with zero warnings/errors.
- Tests verify SQL row preservation, constraints, relationships, rollback and schema lifecycle. They do not claim engine save/load, crash recovery or power-loss verification. At that milestone, sections 2.2-2.6 and GRID-REQ-004 remained pending; the subsequent 2.2 completion is recorded below.
- See [Persistence Architecture and Schema](Project_GRID_Persistence_Architecture.md) for tables, connection/migration policy, identity conventions and future audit/hardware integration.

## 2.2 Save and recovery

- [x] Save world state transactionally
- [x] Load and validate saved world state
- [x] Add periodic consistent snapshots
- [x] Implement safe startup and shutdown saves
- [x] Handle interrupted writes and corrupt files
- [x] Restore from last known-good snapshot
- [x] Preserve identities, district memberships and clock state
- [x] Ensure no simulation progress during powered-off time

**Completion notes - 2026-10-18**

- Added WorldStore transactional save/load over the unchanged schema v1, with complete state validation and parameterized SQL. Durable GridWorld.Id is separate from the fresh runtime InstanceId. Programs retain exact values, revisions and memberships; resources and clock state round-trip.
- Normal terminal startup creates and persists Genesis once, then restores the existing world paused unless --start is passed. No wall-clock catch-up occurs. Default storage is %LOCALAPPDATA%\GRID\world.db; --world selects another file and --ephemeral explicitly opts out of saving.
- Autosave captures a consistent snapshot every 30 seconds without holding the engine lock during disk I/O. Exit, EOF and Ctrl+C await scheduler/autosave shutdown before a final save. Failures are visible and produce a nonzero result.
- Saves roll back on SQL failure/cancellation before commit. A validated rolling .backup is published after each successful primary commit. --recover validates that backup and preserves damaged primary/sidecar files; interrupted recovery requires an explicit retry. Missing/corrupt saves are never silently regenerated. An exclusive session lease prevents competing GRID instances for one file.
- Verification: `dotnet test Grid.AI.slnx --verbosity minimal --no-restore` passed **200 tests** (8 Core, 93 Simulation, 50 Terminal, 49 Persistence), zero failed/skipped. `dotnet build Grid.AI.slnx --no-restore --verbosity minimal` passed with zero warnings/errors. Added 29 cases covering world round trips, injected transaction failure, cancellation, invalid data, backup/recovery failures, periodic saving, ownership and terminal lifecycle.
- Built-terminal smoke verification used an isolated temporary database: create, step twice, exit/save, reopen paused at tick 2 with the same identity, reject corruption, explicitly recover, and verify --ephemeral --start. No default user world was opened.
- Limits: abrupt termination can lose work after the last committed snapshot; tests do not claim physical power-loss validation. Backups are one rolling snapshot, not historical retention. Durable events/audit (2.3), hardware-generated cities (2.6), login and Windows service integration remain pending. See [Persistence Architecture](Project_GRID_Persistence_Architecture.md).

## 2.3 History and observability

- [x] Create persistent event log
- [x] Record changes with timestamps and involved entities
- [x] Record movement, resources and significant world events
- [x] Support filtering and querying history
- [x] Avoid unnecessary logging of routine ticks
- [x] Add structured logs with severity and context

**Completion notes - 2026-10-18**

- Added schema v2 WorldEvents with transactional upgrade; v1 migration is unchanged. Events and consistent world snapshots commit together during autosave/shutdown, with idempotent event IDs and acknowledgement after successful persistence.
- Records UTC and simulation timestamps, involved entity IDs/names, severity, details and JSON context for clock transitions, transfers, energy/resource changes and changing edits. Historical references do not require a currently active resident and never resurrect one.
- Added SetDistrictResource for validated, observable resource changes; ordinary tick-completed messages and no-op updates are excluded from durable history. Current resources are fictional; 2.4 host telemetry is not implemented.
- Added history command/API filters for kind, entity, severity, since time and bounded limit/offset. Queries show committed history only. Added structured Error diagnostics for scheduler, save and startup/session failures.
- Pending history is independent of live-view retention, capped at 10,000 events with explicit HistoryGap warnings. Abrupt termination can lose uncommitted history; automatic disk retention and exhaustive security audit remain future work.
- Verification: 208 solution tests passed (8 Core, 93 Simulation, 56 Persistence, 51 Terminal), zero failed/skipped; build passed with zero warnings/errors. Added eight history/migration/query/failure cases. Isolated terminal restart smoke recovered Clock history and excluded Tick records.
- See [History and Observability](Project_GRID_History_and_Observability.md) for API contracts, retention/failure boundaries and evidence. GRID-REQ-005 remains planned in 2.4/2.7; previous completion records retain their original scope.

## 2.4 Resources and host state

- [x] Define CPU, RAM, GPU/VRAM, storage and network resource categories for application residents (GRID-REQ-005); distinguish measured usage, capacity, pressure and estimated requirements
- [x] Establish resource limits and update invariants
- [x] Create abstract interfaces for host telemetry
- [x] Add fake telemetry provider for repeatable tests
- [x] Add approved read-only Windows telemetry
- [x] Specify and test bounded, smoothed energy/resource-sufficiency interpretation, including missing/stale metrics and unknown requirements (policy API; application binding depends on 2.7)
- [x] Prevent fictional resource changes from affecting Windows allocations

**Telemetry/policy completion notes - 2026-10-19. Application integration remains pending below.**

- Added typed metric/scope/unit/status/source/timestamp contracts, a read-only provider interface, two-second monitoring and detached engine observations with ten-second freshness. Invalid/nonfinite/duplicate readings are rejected; unsupported/stale/unavailable values remain unknown.
- Windows collection reads CPU idle headroom, available/total physical RAM, system-volume free/total space, and per-interface network rates/link status. CPU has an initial warm-up; network resets and stale gaps do not fabricate rates. GPU/VRAM and disk throughput are explicitly Unsupported. No host allocations or process state are changed.
- Added a provenance-required additional-work headroom policy: weakest required ratio, bounded 0–100, EMA 0.25 on distinct samples, unknown/reset on missing/stale required data. This does not infer real application minimum requirements or health.
- Added resources command and CPU/RAM summary in live view. Monitoring remains independent of simulation ticks and is cancelled/awaited at shutdown; sampling is not saved as current host state or logged on every interval.
- Verification: 223 tests passed (8 Core, 106 Simulation, 56 Persistence, 53 Terminal), zero failed/skipped; build zero warnings/errors. Fifteen added cases cover fake providers, invariants, freshness, policy smoothing and terminal lifecycle. A real Windows disposable-session smoke verified CPU warm-up/available readings, RAM, explicit unsupported GPU and tick zero while paused.
- [x] Bind sourced resource requirements/policy to discovered application residents and exclude them from arbitrary Genesis energy drain (2.7/3.1). Unknown requirements/observations remain unknown; no fabricated application condition score.
- [ ] Extend collectors for GPU/VRAM, disk throughput and additional drives when supported; retain explicit unknown fallback and scoped attribution.

See [Resources and Host State](Project_GRID_Resources_and_Host_State.md) for exact units, formula, coverage and remaining integration. These completion notes do not mark GRID-REQ-005 or the 2.7 retrofit implemented.

## 2.5 Verification

- [x] Test save → close → reopen consistency
- [x] Test transaction rollback and crash recovery
- [x] Test migration from an older schema
- [x] Test no offline catch-up
- [x] Test invalid or missing database behavior
- [x] Verify persistent world runs without graphics or AI

**Completion notes - 2026-10-19**

- Added eight isolated integration/verification cases covering real terminal save/close/reopen and backup recovery, a populated v1-to-v2 migration, simulated years offline, and missing/empty/foreign/future database rejection without replacement.
- Added a test-only subprocess that holds an uncommitted SQLite transaction with spilled pages. The parent terminates that child after a readiness handshake; reopening restores the last committed world/history, removes uncommitted schema/data, passes integrity_check and retains the backup unchanged. Existing injected-failure tests continue to verify production save/history transaction rollback and retry.
- Verified restored world/program IDs, revisions, values, district membership/resources and clock state. Restoration starts paused with a fresh runtime identity, no stale host telemetry and no offline advancement; a manual step advances exactly once.
- Headless terminal subprocesses operate with redirected streams and no viewer/model dependency. Tests use unique temporary worlds and never the user's default database. No production behavior changes were needed.
- Verification: `dotnet test Grid.AI.slnx --verbosity minimal` and final `--no-restore` run passed **231 tests** (8 Core, 106 Simulation, 64 Persistence, 53 Terminal), zero failed/skipped. `dotnet build Grid.AI.slnx --no-restore --verbosity minimal` passed with zero warnings/errors.
- Scope: process-crash/journal recovery is tested, not physical power loss or exhaustive storage-failure coverage. Pending application-energy binding (2.4/2.7), hardware generation (2.6), and application-resident retrofit (2.7) remain pending.
- See [Phase 2 Verification](Project_GRID_Phase_2_Verification.md) for the requirement-to-test mapping, helper isolation and limitations.

---

## 2.6 Hardware-generated first-time world creation (GRID-REQ-004)

**Implemented and verified - 2026-10-19.** Integrated with persistence; completed Genesis criteria and the fixed demo/test factory are preserved.

- [x] Define a read-only host discovery adapter for motherboard, CPU, GPU, RAM, storage and network hardware
- [x] Specify normalized profile fields, units, stable ordering, multiple-device handling and recorded partial-discovery fallbacks
- [x] Keep local adapter discovery separate from network scanning, user-file access and live telemetry
- [x] Define bounded component-to-city mappings that preserve the city, atmosphere and fashion design guides
- [x] Implement headless C# generation using a normalized creation profile, per-world seed and generator version
- [x] Persist world ID, creation profile, seed, versions, component associations, generated layout, entities and simulation state atomically
- [x] Mark first creation complete only after durable save; handle simultaneous initialization without duplicate authoritative worlds
- [x] Load the actual saved world on later startups instead of rerunning generation, preserving edits, identities, memberships and clock state
- [x] Preserve established worlds after hardware upgrades, missing devices, changed drivers or changed discovery order
- [x] Distinguish genuinely absent saves from corrupt, inaccessible or incompatible saves; recover or migrate without silent replacement
- [x] Keep explicit new-world creation separate from normal startup and preserve prior worlds/history
- [x] Test deterministic fixtures, profile-sensitive generation, partial discovery, multiple devices and bounded world size
- [x] Test first creation and reopen, changed-hardware reuse, interrupted saves, full disk, corrupt data and concurrent initialization
- [x] Add completion notes with actual implementation and verification evidence when finished

**Completion notes - 2026-10-19**

- Added six-category Windows CIM discovery, normalized profiles, explicit partial/unsupported fallbacks and bounded deterministic hardware-city-v1 generation. Six districts, connected streets and building lots are semantic data; no renderer is claimed.
- Schema v3 transactionally saves actual layout, provenance, associations and initial state. Later load/recovery skips discovery; existing worlds remain intact. Explicit separate creation uses --world with a new filename.
- New generated worlds contain zero residents pending 2.7, preserving the revised host-application vision. Legacy worlds and --ephemeral retain the original Genesis population.
- Verification: dotnet test passed all 250 tests; dotnet build --no-restore had zero warnings/errors. Nineteen new cases cover determinism, bounded/fallback geometry, save/reopen/recovery, corrupt/artifact refusal, cancellation, concurrent creation and genuine SQLite SQLITE_FULL rollback through a test-only page limit. Existing interrupted-update subprocess tests also pass; physical disk exhaustion and power-loss are not claimed.
- Real Windows disposable creation captured all six categories with CPU units and RAM/storage capacities; six districts, six streets and 56 lots reopened unchanged at tick 1, paused. The default world was not modified.
- See [Hardware City Generation](Project_GRID_Hardware_City_Generation.md) for full implementation details and limits. Changes are local working-tree changes at verification, not a release or remote publication claim.

**Later integration:** Active Phase 7 must preserve this same create/load decision across Windows, Linux and Android; the future City backlog may render saved layouts but never regenerate them. GPU or hardware capacity must not require graphics or AI for headless generation. Hardware details are provenance, not authentication; persisted world IDs do not preserve old session/preview validity after restart.

---

## 2.7 Host application population and persistence policy (GRID-REQ-005)

**Implemented and verified - 2026-10-19.** Integrated with the 2.4 resource foundation before extending Phase 3. Section 2.2 remains completed for its original all-citizen save/load behavior. See [shared contract](Project_GRID_Host_Programs_and_Resources.md).

- [x] Add explicit transient application versus persistent agent/special-program classification without deriving authority from names or labels
- [x] Define application grouping, multi-process ownership, independent instances, background/system-process inclusion and bounded discovery costs
- [x] Add fake and read-only host process discovery adapters with process-lifetime identity, PID-reuse protection, sample freshness and unavailable-data handling
- [x] Reconcile starts, confirmed exits and relaunches atomically with ticks and district membership; incomplete observations must not imply exit
- [x] Load persistent residents and discover the currently running application population at startup; never restore a closed application from a prior save
- [x] Persist city/layout, persistent agents/special programs and clock while omitting ordinary resident state and dangling membership references
- [x] Add a versioned migration and explicit legacy conversion/archive policy; retain a recoverable original and never relabel old citizens as detected apps
- [x] Apply live reconciliation after backup recovery without resurrecting historic application residents
- [x] Integrate 2.4 resource observations and requirement provenance; define energy/unknown-state behavior and separate optional civic resources
- [x] Keep historical lifecycle records distinct from resident saves; define minimal metadata, retention and no unobserved offline activity
- [x] Update terminal snapshots/inspection/live view for resident type, incarnation, resource freshness and known versus estimated values
- [x] Ensure observations and simulated shortages never stop, suspend, restart or throttle host applications without a separate authorized capability
- [x] Test grouping, PID reuse, rapid starts/exits, zero applications, denied/stale telemetry, resource interpretation and invalidated previews
- [x] Test changed application sets across restart, persistent-agent continuity, legacy migration/rollback and recovery reconciliation
- [x] Add dated implementation/completion verification only after the behavior is built and tested

### 2.7.1 Retrofit existing modules before extending citizen behavior

**Retrofit implemented and verified - 2026-10-19.** See [retrofit mapping and dependency order](Project_GRID_Host_Programs_and_Resources.md#existing-implementation-retrofit---planned-2026-10-18). Preserve original completion entries for 1.2–1.10 and 2.2.

- [x] Extend GridProgram with explicit resident classification and application-run identity distinct from durable identity
- [x] Add safe world/engine removal and reconciliation, including atomic membership cleanup and rejection of operations on expired incarnations
- [x] Retain the Genesis twenty-citizen factory as an explicit demo/test fixture and replace production population seeding with discovery
- [x] Adapt 2.2 save/load and backup recovery with classification-aware persistence and a recoverable, versioned legacy migration
- [x] Switch production startup only once lifecycle reconciliation and the new persistence policy work together
- [x] Replace fixed application energy drain/recharge only after 2.4 specifies measurable inputs, requirement provenance and unknown-data behavior
- [x] Adapt editing metadata and preview validation for observation-owned fields, transient edits and application exit/relaunch
- [x] Extend terminal inspection/live view for resident type, resource freshness and unknown measurements
- [x] Preserve clock/no-offline-time rules, transactions, backups, command registration and consistent snapshots; add regressions around their integration
- [x] Verify the complete retrofit before expanding Phase 3 behavior; retain Genesis tests as fixtures rather than production population requirements

**Order:** classification/policy → safe lifecycle with fake discovery → coordinated persistence/startup migration → real telemetry and resource interpretation → editing/UI integration and end-to-end verification. Section 2.3 can proceed against this contract without treating all event subjects as permanent residents.

**Completion notes - 2026-10-19**

- Added explicit Legacy/Application/Agent/Special classification, new runtime incarnations, current-session windowed executable grouping, read-only Windows discovery and injectable fake-provider tests. Same-executable helpers/windows share one resident; background-only services and cross-executable child ownership are outside the initial policy.
- Lifecycle reconciliation shares the engine lock with ticks/edits/snapshots. Confirmed exits clean membership; incomplete samples retain unknown residents/helpers. PID reuse/relaunch invalidates the prior resident and its edit previews. No unobserved offline activity is invented.
- Schema v4 filters application rows and membership from saves. Agent/Special identity/state persists. Production conversion archives legacy citizens in a validated unique .legacy-<id>.db file before removal; cancellation/failure preserves recoverable data. Recovery rediscovers current applications.
- Bound 2.4's optional sourced headroom policy to non-demo residents; unknown requirements remain unknown. Removed arbitrary non-demo tick energy changes. Observation-owned energy/status reject edits; aliases/district edits remain transient. Inspection/live/status expose classification, incarnation, freshness and measurement limits.
- Verification: 268 tests passed (14 Core, 117 Simulation, 78 Persistence, 59 Terminal); build zero warnings/errors. Eighteen added cases cover grouping, lifecycle/denials/PID reuse, concurrent churn/ticks, condition interpretation, invalidated edits, save/restart/recovery, persistent residents and archive failure/cancellation. Real Windows disposable startup and reopen observed nine groups without changing the default user world.
- See [Application Residents Implementation](Project_GRID_Application_Residents_Implementation.md) for precise scope and evidence. Per-application CPU/GPU/disk/network attribution, persistent service budgets, agent task services and broader app discovery remain future extensions. Changes are local working-tree changes, not a release or publication claim.

**Planning integration notes - 2026-10-18:** Updated the design, resource/persistence contracts, terminal/identity/editing plans, visual guides and agent handoff. Replaced fixed production-population targets with running applications plus persistent residents; retained twenty-citizen fixtures and original completion history. No C# or database changes and no runtime tests were performed. Earlier 200-test verification belongs to 2.2, not GRID-REQ-005.

---


## 2.7.2 Installed-program availability revision

- [x] Expand discovery to registered installed applications and accessible current-session background processes
- [x] Display Sleeping / On call / Running with explicit unknown/stale fallback
- [x] Retain session catalog entries through process exit and preserve resident identity through sleep/wake
- [x] Preserve ordinary-app rediscovery, saved city, and Agent/Special persistence contracts
- [x] Verify lifecycle, classification, incomplete observation safety and real Windows startup/reopen
- [ ] Extend full-machine coverage to Store-only/portable apps, richer executable mappings, uninstall reconciliation and populations beyond the current guard

**Completion notes - 2026-10-19:** Implemented the discoverable-inventory baseline; the broader “all PC programs” goal remains limited by discovery coverage. 290 tests passed; Windows smoke showed 49 Sleeping, 65 On call, 13 Running, 96 unknown. See [Installed Application Availability](Project_GRID_Installed_Application_Availability.md). This supersedes the earlier 2.7 windowed-only/remove-on-exit policy without erasing its completion history.

---

# Phase 3 — First Programs

Planned behavior follows GRID-REQ-005: ordinary application residents are transient and observation-driven; persistent agents/special programs retain autonomous roles. Relationships and memories for ordinary residents are session-local; durable historical events do not restore them as active residents.

## 3.1 Program needs and behavior

- [x] Implement the approved resource-based energy/condition policy for application residents and persistent agents; do not use movement/open duration as arbitrary drain
- [x] Implement persistent-agent/special-program work/rest policies and observation-grounded application activity
- [x] Add needs, goals and action priorities
- [x] Define legal states and state transitions
- [x] Implement occupations and simple daily work
- [x] Use deterministic state machines or utility scores

**Completion notes - 2026-10-19**

- Added a deterministic priority state machine for application observation and persistent-resident needs/goals, shifts, resource waits, work bursts and rest. Applications never acquire fictional work; Agent/Special routines require explicit trusted configuration and sourced headroom budgets.
- Administrator, Archivist, Researcher and Technician routines track simulated work units. Work/rest durations, shift windows and 60/40 resource hysteresis are explicit. Unknown evidence freezes progress. No arbitrary energy drain, offline catch-up, movement, OS actions or named agent services were introduced.
- Schema v5 persists routine/configuration and progress atomically while omitting application behavior. Restored routines wait for fresh measurements. Added transition/completion history, revision tracking, read-only behaviors and inspection details.
- Verification: 278 tests passed in Release (14 Core, 125 Simulation, 79 Persistence, 60 Terminal); Release build zero warnings/errors. Tests cover state selection, cycles, shifts, cancellation, overflow rollback, persistence/recovery and UI. A disposable real terminal step/reopen preserved tick 1 and showed observation-only applications.
- Debug binaries were locked by running GRID/Visual Studio sessions; they were not stopped. Added the existing test-only crash probe to the solution to ensure matching Release configuration. See [Program Needs and Behavior](Project_GRID_Program_Behavior.md) for evidence and scope. Changes are local, not a release/publication claim.

## 3.2 Movement and occupations

**Implemented and verified - 2026-10-20.**

- [x] Define district connections and movement rules
- [x] Add movement goals and route selection
- [x] Add jobs and workstations
- [x] Assign programs to suitable jobs
- [x] Connect application presentation to measured resource activity; keep optional fictional civic production separate
- [x] Prevent impossible or invalid movements

**Completion notes:** Deterministic saved-road routes, five-second edge travel, compatible/exclusive civic workstations, trusted assignment/release, atomic membership updates and v7 route persistence are implemented. `jobs` and `activity` expose assignments and measured application evidence. Existing unassigned routines retain 3.1 behavior; host applications do not perform fictional civic work. 323 tests passed across isolated Verification builds; disposable terminal save/reopen passed. User-console Release binaries were locked by a running session, which was not stopped. This is district-level simulation; visual walking, terminal admin assignment, social behavior and sustained autonomy remain later work. See [Movement and Occupations](Project_GRID_Movement_and_Occupations.md).

## 3.3 Social civilization

- [x] Implement program-to-program interactions
- [x] Introduce relationships and social preferences
- [x] Add simple memories and meaningful experiences
- [x] Track occupation and social history
- [x] Allow cooperative and competing goals
- [x] Introduce bounded resource scarcity and recovery

**Implemented and verified - 2026-10-20.** Opt-in Agent/Special interactions, cooperative/competitive/reserved preferences, bounded relationships and experiences, occupation history, and separate simulated civic scarcity/recovery are complete. Schema v8 preserves social state and partial rounds; `social [id|name]` and program inspection expose it read-only. **336 tests passed** (14 Core, 144 Simulation, 86 Persistence, 92 Terminal), including v7 upgrade and recovery; isolated terminal create/save/reopen passed. Automatic agent creation, terminal admin configuration, real communications and 3.4 autonomy remain pending. See [Social Civilization](Project_GRID_Social_Civilization.md) for rules, limits and completion evidence.

## 3.4 Autonomy and tests

- [x] Implement program decision loop
- [x] Use seeded randomness for reproducibility
- [x] Ensure each action follows world rules
- [x] Run a changing fake application population and persistent agents for one simulated hour (accelerated test); cover zero applications, starts/exits and high churn
- [x] Verify coherent movement and event records
- [x] Measure CPU and memory consumption
- [x] Verify autonomy works without an LLM
- [x] Keep the simulation responsive when background load increases


**Implemented and verified - 2026-10-20.** The composed behavior/travel/social decision loop follows world rules without an LLM. Persisted world identity seeds stateless social tie choices; schema v9 gates the changed policy. A 3,600-tick scenario with eight agents, zero-to-512 application churn, resource pressure and concurrent readers passed. **338 tests passed** (14 Core, 146 Simulation, 86 Persistence, 92 Terminal). Focused run: 4.215 s wall, 5.031 s process CPU, 105.4 MB peak working set; tick p95 2.260 ms, snapshot-read p95 17.028 ms. This verifies an accelerated simulation hour, not a real-time hour-long production soak or universal performance guarantees. See [Autonomy and Verification](Project_GRID_Autonomy_and_Verification.md) for methodology, repeat commands and remaining validation.

---

# Phase 4 — Communications

## 4.1 Communications foundation

- [x] Create communication hub and routing contracts
- [x] Define message, conversation and recipient models
- [x] Support program identity addressing
- [x] Support messages to named agents and civilian programs
- [x] Implement conversation start/continue/end
- [x] Persist messages in SQLite
- [x] Recover conversations after restart

**Implemented and verified - 2026-10-20.** Grid.Communication contracts and the engine-hosted local hub provide explicit recipient identity, exact name/ID routing and start/send/end/read lifecycle. Schema v10 saves bounded transcripts with world state, preserving open/ended conversations and historical participants through restart/recovery. **346 tests passed** (14 Core, 152 Simulation, 88 Persistence, 92 Terminal), including routing failures, cancellation, limits, concurrency, v9 migration and failed-save rollback. Message storage does not execute tasks, authenticate users or generate replies. Terminal chat remains 4.2. See [Communications Foundation](Project_GRID_Communication_Foundation.md) for API, limits and verification.

## 4.2 Terminal communication

- [x] Build interactive GRID chat commands
- [x] Support directed messages and replies
- [x] Support MCP as general coordination point
- [x] Implement deterministic replies for early development
- [x] Distinguish verified state from simulated dialogue
- [x] Add command validation and helpful errors
- [x] Support ongoing conversations in the same session

**Implemented and verified - 2026-10-20.** Added connect/talk/conversations/transcript/resume/disconnect, lazy persistent MCP coordination, stable local-operator conversation continuity and atomic deterministic reply pairs. Verified snapshot replies are labeled separately from simulated dialogue; commands do not execute or delegate host tasks. **353 tests passed** (14 Core, 155 Simulation, 88 Persistence, 96 Terminal); three-launch isolated terminal save/resume/end smoke passed. Existing schema v10 is reused. Authentication and admin identities remain 4.2A. See [Terminal Conversations](Project_GRID_Terminal_Conversations.md) for command examples, bounds and verification.

## 4.2A Terminal login and identity sessions (GRID-REQ-003) — completed

**Implemented and verified - 2026-10-20.** Originally planned on 2026-10-16. See [Terminal Login and Master Control Plan](Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md). This extends the terminal after Genesis and does not change section 1.7's completed status.

- [x] Choose local authentication method and final Master Control/Master Admin label
- [x] Define persistent operator/agent identities, role policies, and audit records with Phase 2 storage
- [x] Begin future terminal sessions with login
- [x] Support the User's Master Control identity and each registered agent identity
- [x] Pass operator identity, active program identity, and capabilities through command/session context
- [x] Show active identity in prompts and implement whoami, logout, and authorized switching
- [x] Provide role-specific views of messages, history and world state; report real tasks unavailable until the task service exists
- [x] Extend command registration with capabilities and identity-aware help
- [x] Clear or rescope context on logout and identity changes
- [x] Verify login failures, switching, logout, context isolation, and expandable identity registration

### Master Control program editing (GRID-REQ-003)

**Terminal integration implemented - 2026-10-20; backend API completed in 1.9.3.** Master Control is the selected role label. Preview/apply/cancel uses the existing service authorizer and revision contract.

- [x] Add terminal commands to inspect and edit programs and their values as Master Control
- [x] Initially support name, occupation, energy, status, and district assignment
- [x] Define an expandable registry of editable fields, types, and validation rules
- [x] Enforce Master Control authorization in the editing service
- [x] Preview target and old/new values; report committed results
- [x] Serialize edits with ticks and reject stale/conflicting updates
- [x] Use world-management transfers for district changes; preserve stable identity and membership
- [x] Separate profile edits from credential, permission, and host-capability management
- [x] Audit edits and integrate persistence; define one-time edits versus ongoing overrides
- [x] Test authorized/denied edits, invalid values, atomicity, population consistency, and saved results

**Completion notes - 2026-10-20.** Windows-token SID owner authentication, eight-hour local sessions, registered agent selection, capability metadata/dispatch, scoped prompts/help/conversations, logout/switch invalidation and Master Control edit preview/apply/cancel are implemented. Schema v11 persists the owner/agent registry; owner binding saves before production login grants access. **362 tests passed** (14 Core, 155 Simulation, 90 Persistence, 103 Terminal), plus an isolated real-Windows login/switch/deny/edit/restart smoke. No OS elevation, agent password or real task service is claimed. Observation-owned energy/status remain protected. See [Local Login and Identity Implementation](Project_GRID_Local_Login_and_Identity_Implementation.md) for trust boundaries, commands, audit and remaining work.

## 4.2B Terminal UX and portable credential setup (GRID-REQ-006 / GRID-REQ-003)

**Implemented - 2026-10-20.** Terminal UX and local credential setup are complete within the scope described below. See [UI, portability and offline plan](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and [Terminal Help](Project_GRID_Terminal_Help.md).

- [x] Format each command with indented dashed options/flags/switches, a # USAGE line per option, and a final example
- [x] Create the offline Markdown help reference (documentation artifact only, 2026-10-20)
- [x] Implement help -md to open the bundled local reference with an offline terminal fallback
- [x] Add responsive GRID7736 opening artwork and welcome/login screen
- [x] Add local username/password setup when credentials are not configured for a new/authorized migrated world; no plaintext storage or password completion/history
- [x] Migrate existing Windows-SID-bound ownership explicitly and preserve identities, roles and transcripts across PCs
- [x] After login clear screen, show Welcome username and authority level, and wait for Enter before the prompt
- [x] Confirm logout with y/n; cancellation retains context; confirmed logout clears context/screen and redisplays welcome
- [x] Keep expiry/revocation immediate and define safe redirected-input/EOF/Ctrl+C behavior
- [x] Apply light-blue base output, white user input, yellow tables/dividers and red errors, with no-color fallback
- [x] Implement capability-aware Tab completion for commands/options with quoting, ambiguous matches and password suppression
- [x] Test help synchronization, colors/fallbacks, login/setup/logout transitions, completion privacy and live-view return

**Completion verification - 2026-10-20.** 368 tests passed (14 Core, 155 Simulation, 91 Persistence, 108 Terminal). Schema v12 adds salted password credentials, preserves existing Windows ownership for authenticated migration, and retains agent/role/world state. Offline help renders directly in the terminal. Quoted input remains editable; Tab suggestions are static and suppressed inside quotes/passwords. Automated UI coverage uses injected input; physical terminal appearance and two-PC packaging are not claimed as verified. See [implementation notes](Project_GRID_Terminal_UX_Implementation.md) for commands, security boundaries and remaining Phase 8 work.

### 4.2B follow-up - password requirements (completed, 2026-10-20)

**Implemented: new/changed passwords use 6–24 characters; existing passwords remain usable.**

- [x] Change new-password/password-change limits to **6–24 characters inclusive**
- [x] Allow uppercase, lowercase, numbers and symbols without requiring any category or combination; preserve case and exact input
- [x] Synchronize validation, masked setup prompts, error messages and Markdown help; never silently truncate passwords
- [x] Preserve verification of existing saved credentials, including longer passwords, until explicitly changed by the owner
- [x] Verify 5/6/24/25-character boundaries, allowed categories, confirmation matching and backward-compatible login; record completion evidence

**Completion verification - 2026-10-20.** 390 tests passed (14 Core, 155 Simulation, 91 Persistence, 130 Terminal). Thirteen new test cases verify 5/6/24/25-character boundaries, individual categories/mixed symbols, exact case/spacing, confirmation mismatch, existing 25/256-character passwords after restore, and authenticated rotation. Existing hashing, credential records and authorization remain unchanged. See [password implementation notes](Project_GRID_Terminal_UX_Implementation.md#password-requirements---completed-follow-up-2026-10-20).

## 4.2C Additional data windows and shared terminals (GRID-REQ-006)

**Implemented - 2026-10-20.** `-opninwin` opens supported snapshot/live data windows; `ntm` opens a scoped terminal attached to the same host. See [additional-window design](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md#additional-windows---planned-42c-2026-10-20). Completed 4.2B scope is unchanged.

- [x] Add `<command> -opninwin` to open supported data output in a separate readable/scrollable window
- [x] Define supported read-only commands, snapshot/live labeling, and unsupported-option errors
- [x] Add `ntm` to open another terminal attached to the same running GRID and saved world
- [x] Maintain one authoritative simulation clock and save writer using local offline attachment; preserve exclusive world ownership
- [x] Preserve scoped authentication/capabilities and independent per-window input/chat/edit context; never transfer plaintext credentials
- [x] Define detach versus whole-GRID shutdown, including main/last-window closure, logout/expiry, NGE and host disconnection
- [x] Add help examples and capability-aware completion when implemented; preserve portable launch paths and useful window-launch errors
- [x] Verify shared state, authorization isolation, lifecycle, single-writer persistence and recovery; record completion evidence

**Completion verification - 2026-10-20.** 404 tests passed (14 Core, 155 Simulation, 91 Persistence, 144 Terminal), including 14 new window tests and a real attached subprocess. One host owns simulation/storage; attached exit detaches, original-host exit stops all attachments before final save. NGE/transfer/credentials remain host-only. See [shared-window implementation](Project_GRID_Shared_Terminal_Windows_Implementation.md) for capability, lifecycle, transport and verification limits.

## 4.2D Master Control move/copy commands (GRID-REQ-003 / GRID-REQ-007)

**Current-host implementation delivered - 2026-10-20.** `tranexist "location"` moves the verified GRID application/selected-world inventory; `copexist "location"` copies it and retains the source. See [transfer implementation](Project_GRID_Transfer_Implementation.md) for exact scope, shutdown/restart, pending cleanup and limitations. Full portable edition/attached-window integration remains future work.

- [x] Add both commands with quoted-path parsing and Master Control enforcement
- [x] Inventory running application files, bundled Markdown and selected save/backups/archives; include existing package-managed folders and exclude unrelated host files
- [x] Preview source/destination/count/size; require interactive y/n; reject overlap, existing destinations, links and inadequate space
- [x] Stop current host workers, save, stage a consistent copy, hash-verify and validate the copied world before publication
- [x] Move through a post-exit helper; reacquire source lease and verify/lock inventory before cleanup; report pending/incomplete status truthfully
- [x] Keep copies independent, retaining originals and requiring login; preserve saved world/city/identities/history
- [x] Provide package-relative launch/default world and conditionally retarget an existing matching managed launcher after a move
- [x] Add help/completion metadata and nine transfer regression tests
- [x] Integrate 4.2C attachment shutdown/draining before transfer or NGE; keep these actions on the original host
- [ ] Complete the active Phase 7 portable platform contract and reconcile future plan B.6 PC/Flash edition migration/provisioning
- [ ] Add automatic interrupted-move recovery and exhaustive low-space, power-loss, drive-removal and physical cross-machine acceptance

**Completion verification - 2026-10-20.** 377 tests passed (14 Core, 155 Simulation, 91 Persistence, 117 Terminal), including the actual cleanup helper on disposable files. Zero build warnings/errors. Current transfers require an available .NET 10 runtime. No user files were relocated during verification; remaining gates above are not claimed complete.

## 4.2E Conversation windows and typewriter responses

**One-to-one implementation delivered - 2026-10-21.** Default connect/resume opens dedicated windows; meeting integration was subsequently completed in 6.2A on 2026-10-25.

- [x] Open one-to-one agent conversations in a new dedicated window connected to the same authoritative GRID host
- [x] Keep the original terminal available; closing a conversation window ends/detaches that view without shutting down GRID
- [x] Preserve authenticated identity, participant scope, conversation history and logout/host-disconnection behavior across windows
- [x] Render agent responses with a typewriter effect, with speaker names clearly visible
- [x] Make the effect interruptible/skippable and offer instant text for accessibility, redirected output and reduced-motion preferences
- [x] Keep animation separate from message persistence and task execution; save each complete response once and never interleave speakers' output
- [x] Verify window lifecycle, history, identity isolation and animation cancellation; add help and completion examples

- [x] Integrate multi-agent meeting windows when the 6.2A meeting/participant service is implemented — completed 2026-10-25; see 6.2A

**Completion verification (2026-10-21):** 572 tests passed (14 Core, 157 Simulation, 97 Persistence, 304 Terminal), including 12 new window/renderer cases and a real attached conversation process. Default connect/resume opens a dedicated view; exit detaches and disconnect ends the conversation. Typewriter responses support skip, per-window on/off and instant output for redirected/reduced-motion use. Complete turns are recorded before animation and use existing host persistence. Explicit `-inline` preserves original-host voice compatibility. No schema change; visible window placement/animation feel remains a manual smoke check. See [implementation, limits and acceptance test](Project_GRID_Conversation_Windows_Implementation.md).

## 4.2F Transfer command naming update

**Implemented - 2026-10-22.** `grid -copyto "location"` and `grid -transferto "location"` replace the 4.2D names. Old commands show migration guidance without initiating a transfer; original implementation history is retained.

- [x] Replace `copexist "location"` with `grid -copyto "location"`
- [x] Replace `tranexist "location"` with `grid -transferto "location"`
- [x] Preserve Master Control/original-host restrictions, inventory preview, confirmation, save consistency, verification and move/copy semantics
- [x] Define old-command migration behavior explicitly (temporary deprecated aliases or an error directing users to the new syntax)
- [x] Update help, tab completion, transfer documentation and examples together when implemented
- [x] Verify quoted paths, invalid/combined switches, authorization and existing transfer regression behavior

**Completion verification (2026-10-22):** 582 tests passed (14 Core, 157 Simulation, 97 Persistence, 314 Terminal). Ten new syntax/migration/help tests plus updated host/window tests verify both switches, quoted paths, rejected mixed/invalid arguments and the existing role/confirmation/save/cleanup boundaries. No schema or inventory change; tests use disposable transfers and do not relocate the user's GRID. [Updated transfer documentation](Project_GRID_Transfer_Implementation.md).

## 4.2G Terminal controls, task views and transfer destinations

**Implemented and verified — 2026-10-23.** Restored after the fresh clone, then completed with schema v21, updated help and regression coverage.

- [x] Replace start/pause with `grid -start` / `grid -pause`, with help/completion and migration guidance
- [x] Make `tasks` show all authorized history, completed or not; add `tasks -completed` and `tasks -list` (exclude Completed and Denied); retain archive labels and agent filters
- [x] Define persisted Denied status/reason/audit without conflating cancellation/failure; verify migration and visibility
- [x] Accept drive roots for `grid -copyto` / `grid -transferto` and add `-select` folder pickers
- [x] Retain Master Control preview/confirmation, verified transfer/recovery and unrelated-file protection at roots; verify collisions, media failures and cancellation

**Completion verification:** 658 tests passed (14 Core, 157 Simulation, 98 Persistence, 389 Terminal), including ten new 4.2G cases. Root/picker destinations create a new GRID7736 child and refuse collisions; existing files remain protected. Denial is a saved human decision distinct from cancellation. Native dialog appearance and physical media/cross-PC disaster acceptance remain Phase 8 checks. Agent start/pause coupling remains 5.9. See [implementation and acceptance commands](Project_GRID_Terminal_Controls_Implementation.md).

### 4.2G folder-picker repair — 2026-10-23

Fixed a user-reported Windows dialog failure: FolderBrowserDialog supports Description, not Title. Removed the unsupported property, explicitly emit JSON null on cancellation (Windows PowerShell otherwise emits no JSON for null), suppress first-use progress and keep raw helper stderr/CLIXML out of terminal errors. **392 terminal tests passed**, including three new tests executing the actual Windows PowerShell helper with a real Windows Forms dialog object. Tests substitute only the modal ShowDialog result for selection/cancellation and verify concise failure output. Physical dialog interaction remains a manual check. Earlier injected-picker tests did not cover this platform failure. Restart GRID to load the corrected binary. Schema remains v21.

## 4.2H Conversation readability and typewriter repair

**Implemented — 2026-10-26; user desktop visual acceptance remains open.** Assistance lacked animation metadata; all conversation surfaces now carry explicit speaker/body colors. New agent text animates at approximately 20 ms per Unicode text element, and queued Enter presses cannot skip it. Earlier 4.2E/runtime notes are historical verification.

- [x] Repair missing assistance animation and carry the shared renderer through inline chat, dedicated/attached windows and meetings; verify `talk` and assistance paths
- [x] Preserve `typewriter on/off`, deliberate skip controls and intentional instant output for redirected/no-animation modes; verify default-on behavior in the normal executable (Enter is now ignored for skipping)
- [x] Keep clear speaker labels, spacing between turns, native console wrapping and code blocks; separate assistance headings, answers and source/review notes
- [x] Use light blue for user/agent speaker labels and chat headings
- [x] Use yellow for agent response text and white for user-entered text and displayed user messages
- [x] Apply conversation colors consistently in inline chat, attached windows, meetings and transcript views; preserve red errors and readable no-color output
- [x] Verify with 772 Release tests (14 Core, 157 Simulation, 99 Persistence, 502 Terminal), including real named-pipe window tests; exercise the normal executable with bundled AI in an isolated interactive Windows ConPTY console
- [ ] Confirm final visual appearance in the user's desktop terminal and detached conversation/meeting windows; the interactive console probe and automated window tests do not substitute for that desktop acceptance

**Completion notes:** Presentation-only metadata, no database migration (schema v23), no new runtime dependencies. Normal-executable checks confirmed inline/assistance animation, actual console color sequences, queued-Enter resilience and instant-off behavior. A CSS advice request hit the existing model output limit; a shorter request succeeded. This does not change AI generation limits or implement the deferred 6.6 unified `talk` workflow. See [4.2H implementation and manual test](Project_GRID_Conversation_Display_Repair.md).

## 4.3 User-supplied context (GRID-REQ-002)

**Implemented - 2026-10-20.** Terminal `context add/list/read/revoke/remove` captures explicit local files with project labels, version fingerprints, retention and optional registered-Agent grants. Text/source support is strict UTF-8; initial document support is DOCX main-body text. Unsupported formats are refused.

- [x] Support selecting or uploading local files as task context
- [x] Support text, Markdown, source code and documents initially
- [x] Record source file paths, versions and provenance
- [x] Define temporary versus persistent project context
- [x] Prevent context access outside approved scopes
- [x] Allow users to revoke or remove shared context
- [x] Implement safe handling of oversized or unsupported files

**Completion verification - 2026-10-20.** 420 tests passed (14 Core, 155 Simulation, 95 Persistence, 156 Terminal), including 16 new context cases. Schema v13 persists captured project context; temporary content stays out of saves/backups. No file execution or LLM analysis is implied. See [context implementation](Project_GRID_User_Supplied_Context_Implementation.md) for supported formats, bounds, scope, commands and recovery/revocation limitations.

## 4.3A Context file selection and convenient reading

**Implemented - 2026-10-21.** Follow-up to 4.3; existing permissions and captured versions are preserved.

- [x] Add `context add -search` with a Windows multi-file picker in main and attached terminals
- [x] Support read/revoke/remove by exact filename or ID; ambiguous names require IDs
- [x] Read multiple files in one command with bounded output
- [x] Add per-terminal `context select`, selected reads, and selection clearing
- [x] Revalidate selection scope and picker authorization; cancel without importing and validate batches atomically
- [x] Update bundled help, implementation documentation and completion verification

**Completion verification - 2026-10-21.** 430 tests passed (14 Core, 155 Simulation, 95 Persistence, 166 Terminal), including 10 new 4.3A cases. Native dialog results are injected for automated tests; visible Windows dialog interaction remains a manual verification step. See [context implementation](Project_GRID_User_Supplied_Context_Implementation.md) for defaults, commands, limits and planned exclusions.

## 4.4 Voice communication

**Implemented Windows offline MVP - 2026-10-21.** Main host `voice` commands add hold-Space microphone capture, installed local Windows recognition/synthesis, mandatory unsent-transcript review and explicit playback. No cloud service or automatic speech.

- [x] Implement microphone selection and push-to-talk (hold Space, release to transcribe; 20-second bound)
- [x] Integrate local speech-to-text (installed Windows System.Speech provider; whisper.cpp remains an optional future adapter)
- [x] Add speech transcript review, correction, explicit send and discard
- [x] Integrate local text-to-speech (installed Windows voices; Piper remains an optional future adapter)
- [x] Create persistent voice profiles for registered named agents (voice, rate, volume)
- [x] Implement default mute, interruption and cancellation (Esc during audio operations)
- [x] Route typed and spoken conversation input through the same communication hub; speech never executes terminal commands

**Completion verification - 2026-10-21.** 448 tests passed (14 Core, 155 Simulation, 97 Persistence, 182 Terminal), including 18 new voice cases. Real offline recognizer/voice enumeration succeeded without recording or playback. Profile and microphone-origin message save/recovery passed. Physical capture/playback and device unplug behavior remain manual checks. Voice hardware is original-host-only; attached terminals retain typed chat. No raw audio is written to disk. See [offline voice implementation](Project_GRID_Offline_Voice_Implementation.md) for commands, prerequisites, limits, persistence and future provider/device options.

## 4.5 Verification

- [x] Test message recipient routing
- [x] Test persistent conversation history
- [x] Test invalid recipients and malformed messages
- [x] Test cross-session context recovery
- [x] Test STT-to-message equivalence
- [x] Test audio unavailable fallback
- [x] Verify communication still works without AI

**Completion verification - 2026-10-21.** 455 tests passed (14 Core, 155 Simulation, 97 Persistence, 189 Terminal). Added three end-to-end tests spanning authenticated command routing, malformed-message atomicity, unrelated-agent privacy, real SQLite reopen, persistent/temporary context, grants/revocation and continued transcripts. Existing voice tests verify reviewed-STT/typed-message equivalence, real generated-speech transcription, cancellation and missing-audio fallback without an AI provider. See [communications verification matrix](Project_GRID_Communications_Verification.md). Physical microphone/playback/device-unplug checks remain manual; advanced providers and real task delegation remain planned.

---

# Phase 5 — System Agents

## 5.1 Agent and task infrastructure

- [x] Create named system program registry
- [x] Define task requests, status, results and failures
- [x] Build durable task queue
- [x] Implement task broker as the only permitted host-action path
- [x] Define agent-specific capabilities and allowed parameters
- [x] Add permission policies and explicit approval requests
- [x] Implement cancellation, timeouts and safe retries
- [x] Persist task execution audit records


**Completion verification - 2026-10-21.** Implemented the explicit six-role registry, durable schema-v14 queue, immutable proposals, per-role capability/parameter allowlists, human one-attempt approvals, service-side scope enforcement, cancellation, one-second demonstration timeouts, three-attempt retries with reapproval, and task/denial audit. **472 tests passed** (14 Core, 157 Simulation, 97 Persistence, 204 Terminal), including 15 new broker integration tests. Start with `agents initialize`; use `tasks propose/inspect/approve/run`. Only labeled safe demonstration adapters are enabled; no shell/real PC task, natural-language delegation or autonomous dispatcher is claimed. See [task broker implementation](Project_GRID_Task_Broker_Implementation.md).

### Planned Master Control and agent action permissions (GRID-REQ-003)

Terminal/session authorization and edit auditing were implemented in 4.2A. The 5.1 broker now adds scoped task authorization/approval/audit for safe demonstration adapters. Autonomous production and real specialist capabilities remain planned.

- [x] Define Master Control administration and each agent's allowed capabilities
- [x] Enforce effective operator/agent permissions in services as well as terminal commands
- [x] Keep human Master Control authority distinct from MCP coordination (4.2A). Registered agent identity does not confer human administration or host-task execution privileges.
- [x] Audit the human operator and active agent identity for manually initiated actions
- [x] Preserve scoped host-task approvals when operating through an agent session
- [ ] Define coordination between manual agent sessions and autonomous task execution
- [x] Test denied task actions, privilege escalation attempts, and manual operator/active-agent attribution (5.1)
- [ ] Test autonomous task attribution once autonomous producers are implemented

## 5.2 MCP — Administration

- [x] Implement task coordination and delegation
- [x] Receive explicit user requests and split into subtasks requiring individual human approval
- [x] Assign work to authorized specialist agents
- [x] Track ongoing tasks and summarize results
- [x] Request permissions rather than bypass them
- [x] Produce verified operational briefings

**Completion notes (2026-10-21):** Explicit offline grammar, atomic saved delegation plans, authorized specialist assignment, per-child consent and briefings from actual records are implemented. Demonstration adapters only; natural-language planning and real specialist actions remain planned. Schema v15 retains links across restart without replay. **Verification: 485 tests passed**, including 13 new coordination cases. See [MCP coordination implementation](Project_GRID_MCP_Coordination_Implementation.md).

- [x] Improve MCP/task readability: compact role tables, scoped short references, clear plan/task distinctions, separate results and copyable commands. Follow-up verification: 219 terminal tests passed.

## 5.3 MAXIS — Development

- [x] Register approved repositories and workspaces
- [x] Implement read-only source inspection
- [x] Implement fixed build/test capabilities
- [x] Capture diagnostics, logs and test results
- [x] Prepare explicit literal-replacement suggestions for review
- [x] Add separate working-copy staging before any manual source change (not an OS sandbox)
- [x] Request approval according to edit policy

**Completion notes (2026-10-21):** Real MAXIS workspace registration/revocation, source inspection, fixed offline-ready .NET build/test, bounded diagnostic logs and per-attempt reports, explicit replacement suggestions, and separate-file working copies are implemented through the approved broker. Source apply and autonomous code generation remain planned; build/test executes trusted project-defined code without OS/network isolation. Schema v16 preserves registrations. **Verification: 505 tests passed**, including 17 new MAXIS cases and artifact-transfer coverage. See [MAXIS implementation](Project_GRID_MAXIS_Development_Implementation.md).

## 5.4 SENTINEL — Security

- [x] Read permitted Windows Defender status
- [x] Read permitted firewall and security event information
- [x] Separate real host alerts from fictional incidents
- [x] Report verifiable warnings and scan times
- [x] Keep initial security functions read-only
- [x] Block sensitive remediation in this read-only release; require a separate scoped human-approval policy before enabling it

**Completion notes (2026-10-21):** Approved read-only Defender, firewall and bounded Security-log observations implemented with source/UTC labels, factual warnings, explicit unknown/unavailable states and historical scan timestamps. No scans or sensitive remediation run. Fixed system PowerShell commands accept no user script/query input; 30-second deadline and bounded output. Schema v17 retains broker result/audit persistence. **Verification: 518 tests passed**, including 13 new SENTINEL cases and real local provider queries. See [SENTINEL implementation](Project_GRID_SENTINEL_Security_Implementation.md).

## 5.5 JN — Storage and files

- [x] Inventory explicitly approved files and directories
- [x] Measure defined GRID database, model and asset storage on demand
- [x] Detect possible duplicate files
- [x] Prepare non-destructive cleanup proposals
- [x] Track the initial 20 GB logical GRID quota
- [x] Escalate quota expansion proposals through MCP
- [x] Require separate user approval for quota changes; reject sensitive deletion until a dedicated approval workflow exists

**Completion notes (2026-10-21):** Immutable registered scopes, bounded read-only inventory and SHA-256 duplicate candidates, non-destructive cleanup reports, explicit managed-storage accounting, a persisted 20 GB decimal logical budget and MCP quota escalations are implemented. Quota changes require separate human approval. Deletion, hard write-cap enforcement, growth forecasting, disk-reserve validation and continuous monitoring remain planned. Schema v18 saves governance state; copy/move preserves JN reports. **Verification: 535 tests passed**, including 16 new JN cases and report-transfer coverage. See [JN implementation and terminal acceptance test](Project_GRID_JN_Storage_Implementation.md).

## 5.6 VOLT and RELAY

- [x] Expose timestamped CPU/RAM/GPU monitoring from the configured provider; preserve unsupported GPU status
- [x] Introduce approved automatic-simulation pacing (normal/eco/auto)
- [x] Reduce GRID simulation frequency under host pressure without changing Windows resources
- [x] Handle approved message delivery to the saved local RELAY inbox
- [x] Support voice output and agent briefings
- [x] Track notification delivery and failures

**Completion notes (2026-10-21):** VOLT reuses fresh host observations and adds bounded automatic pacing with a pressure hold; simulated tick length and manual steps remain unchanged. RELAY persists local notifications, actual-result briefings, explicit human acknowledgement and separately approved offline speech with intent saves and delivery outcomes. No automatic playback/replay, external messaging or OS allocation changes. GPU collection and broader task/model throttling remain planned. Schema v19 preserves mode/inbox/receipts. **Verification: 551 tests passed**, including 16 new pacing/delivery/persistence/authorization cases; automated speech tests do not play audio. See [implementation and acceptance test](Project_GRID_VOLT_RELAY_Implementation.md).

## 5.7 End-to-end task verification

- [x] Ask MCP to delegate a unit-test run to MAXIS
- [x] Validate scope and permissions
- [x] Execute approved test operation
- [x] Record actual results and logs
- [x] Return results through RELAY
- [x] Test rejected and unauthorized requests
- [x] Test cancellation, crash recovery and duplicate dispatch
- [x] Confirm no agent obtains another agent's privileges

**Completion notes (2026-10-21):** Nine new end-to-end cases verify actual offline xUnit execution delegated by MCP to MAXIS, passing/failing TRX evidence, separate human approvals, exact recorded-result delivery through RELAY, SQLite reload, all-six-role isolation, cancellation, durable-start recovery and duplicate dispatch prevention. Existing broker/MAXIS suites cover rejected scopes, actual process cancellation, timeouts and save failures. **560 tests passed** (14 Core, 157 Simulation, 97 Persistence, 292 Terminal), zero failed/skipped. No runtime/schema change was needed (v19). Recovery uses a saved Running snapshot; no user process is killed. See [verification matrix, limits and terminal test](Project_GRID_End_to_End_Task_Verification.md). Next: 6.1 Shared local AI.

## 5.8 Bulk task operations and agent filters

**Implemented - 2026-10-22.** `-agentname` below means the actual registered agent name as a switch, for example `-MAXIS` or `-JN`. Resolve names case-insensitively to stable registered IDs; reject unknown names and conflicting filters.

| Requested command | Implemented behavior |
| --- | --- |
| `tasks run -all` | Run all approved Queued tasks in the selected batch. |
| `tasks run -all -agentname` | Run all approved Queued tasks assigned to that agent; example: `tasks run -all -MAXIS`. |
| `tasks clear -all` | Clear all tasks within the authorized scope using the safe clear policy below. |
| `tasks clear -all -agentname` | Clear tasks assigned to that agent; example: `tasks clear -all -JN`. |
| `tasks approve -all` | Approve all eligible AwaitingApproval tasks in the selected batch. |
| `tasks approve -all -agentname` | Approve all eligible tasks assigned to that agent; example: `tasks approve -all -MAXIS`. |
| `tasks list -agentname` | List tasks assigned to that agent; example: `tasks list -RELAY`. |

- [x] Implement the requested syntax, consistent agent-name resolution, help examples and tab completion
- [x] Snapshot the selected batch at invocation so newly proposed tasks are not silently approved, run or cleared
- [x] Keep bulk approval Master Control-only with a scope/count/action preview and explicit confirmation; validate each task and record individual approvals
- [x] Run queued tasks sequentially through the existing original-host broker with per-task permission/scope checks, persistence barriers and cancellation
- [x] Never approve implicitly when running; exclude unapproved, completed and already running tasks from dispatch
- [x] Define failure/timeout/stop behavior and report per-task outcomes plus completed/failed/skipped totals without hiding partial results
- [x] Design and implement clearing with an explicit scope/count preview and confirmation; require Master Control for bulk mutation and retain an auditable record
- [x] Define clear as removal from the operational task list, with linked plan/result/RELAY history archived or safely retained; never leave dangling references or silently erase audit evidence
- [x] Handle active tasks explicitly: stop/cancel and settle them before clearing, or reject the batch with an explanation; clearing must not leave host work running untracked
- [x] Preserve role visibility restrictions for list filters and all existing task authority boundaries
- [x] Verify mixed states/roles, agent filters, stale sessions, revoked scopes, duplicates, cancellation, restart, linked-history integrity and partial failures

**Completion verification (2026-10-22):** 603 tests passed (14 Core, 157 Simulation, 97 Persistence, 335 Terminal), including 21 new batch/confirmation/archive cases. Original-host Master Control previews and confirms bulk approval/clear; runs dispatch a fixed snapshot sequentially and stop at the first unsuccessful attempt. Agent filters preserve visibility. Clear archives retained records, cancels pending work and refuses active/unsettled work; it does not free the 256-record history cap. Schema v20 preserves archive state, plan/RELAY links and audit across restart; older records default to unarchived. See [implementation, limits and acceptance test](Project_GRID_Bulk_Task_Implementation.md). Next: 6.1 Shared local AI.

---

## 5.9 Agent lifecycle tied to GRID — completed 2026-10-23

- [x] Initialize/resume agents only with `grid -start`; pause them with `grid -pause`; retain saved identities/memory when inactive
- [x] Reconcile agents initialize, lazy MCP creation and startup entry points; prevent independent activity outside GRID
- [x] Define safe pause/shutdown handling of active inference/host tasks; never replay or approve tasks merely because GRID resumes

**Completion verification:** 672 tests passed (14 Core, 157 Simulation, 99 Persistence, 402 Terminal), including eleven new lifecycle, shared-window and real terminal startup cases. Engine activity checks and generation validation block paused/stale replies and dispatch. Active tasks settle cooperatively with an interruption record; external effects cannot be rolled back. Resuming preserves identities and never dispatches work automatically. Explicit `--start` follows authenticated initialization, while normal reload is paused. The refreshed portable Release package also passed its isolated bundled-runtime/MCP reply check without opening a user world. Schema remains v21. See [contract, limits and terminal test](Project_GRID_Agent_Lifecycle_Implementation.md). Historical manual simulation stepping and explicit infrastructure diagnostics remain available; they do not enable agent host-task activity.

# Phase 6 — Intelligence

## 6.1 Shared local AI

- [x] Install and configure local model runtime
- [x] Choose and license-check an appropriate local model
- [x] Implement shared local inference interface
- [x] Queue concurrent requests to a single model service
- [x] Measure memory, GPU and CPU use
- [x] Allow deterministic operation when AI is unavailable

**Completion verification (2026-10-22):** Shared FIFO local inference service implemented with one active/eight waiting requests, cancellation/deadlines, bounded output and deterministic unavailable behavior. Reused installed Ollama 0.40.6; installed Apache-2.0 Qwen3 4B and started a separate loopback-only/cloud-disabled runtime on 11435. `ai status/test` are original-host Master Control diagnostics; they do not change conversations or execute tasks. **620 tests passed**, including 17 new queue/adapter/authority cases. Real C# readiness test returned GRID LOCAL READY in 3.232 s; observed 3.18 GB GPU allocation and measured process CPU/working set. Allocation is not GPU utilization, and process metrics may include other Ollama clients. External runtime/model cache remains outside portable packaging; no schema change (v20). [Implementation, license sources, measurements and acceptance test](Project_GRID_Shared_Local_AI_Implementation.md). Next: 6.2 Grounded agent conversations.

**6.1 follow-up (2026-10-22):** Malformed numeric runtime diagnostics now fall back and recover on the next valid response. 355 terminal tests passed (three added regression cases); Release build passed without warnings/errors. Real readiness probe: 3,252 ms. [Task and AI acceptance test](Project_GRID_Tasks_and_AI_Acceptance_Test.md). Earlier verification counts above remain historical.

## 6.2 Grounded agent conversations

- [x] Build prompt and context construction layer
- [x] Retrieve current authoritative world state
- [x] Retrieve selected-conversation excerpts and permitted saved social memories; cross-conversation user/project recall remains planned in 6.2B
- [x] Attribute retrieved information to its sources
- [x] Use distinct personality profiles for named agents
- [x] Differentiate observed facts, suggestions and fictional dialogue
- [x] Add long-conversation summarization and context limits

**Completion verification (2026-10-22):** Typed/reviewed-voice conversations now use shared local inference with scoped snapshot facts, recent history, extractive summaries, saved social memories and six personality profiles. Replies distinguish observed facts from unverified model text and identify supplied sources. Atomic turn checks, revocation, deterministic fallback and async dedicated windows are implemented. **641 tests passed** (14 Core, 157 Simulation, 98 Persistence, 372 Terminal); real MCP conversation completed without tasks or a user-world file. Release build passed with zero warnings/errors. Schema v20 unchanged. [Implementation, limits and test commands](Project_GRID_Grounded_Conversations_Implementation.md). Meetings remain planned in 6.2A; task/context assistance remains planned in 6.3.

### 6.2 readability follow-up — completed 2026-10-22

- [x] Show normal chat/transcripts as spaced You / agent replies, without technical metadata blocks
- [x] Add `transcript --details` (optionally with conversation ID) for retained facts, sources, ticks and IDs
- [x] Simplify conversation-window greeting/prompt; retain whoami for authority details
- [x] Guide local AI toward natural conversational replies and use dialogue-only history excerpts
- [x] Preserve stored evidence, participant visibility, typewriter behavior and no-execution boundary

**Completion verification:** 373 terminal tests passed; Release build had zero warnings/errors. Real local-model greeting displayed a short conversational response. Existing transcripts remain intact; no schema change. [Details and test commands](Project_GRID_Grounded_Conversations_Implementation.md). Earlier verification counts remain historical.

## 6.2A Multi-agent meetings

**Implemented - 2026-10-25 (planned 2026-10-21).** `agent meetings [list of agent names]` starts a meeting with multiple selected agents; example: `agent meetings MCP MAXIS JN`. Square brackets describe the participant list and are not typed literally.

- [x] Parse and validate multiple registered agent names, rejecting duplicates/unknown participants with useful feedback
- [x] Open the meeting in a dedicated conversation window with typewriter agent responses (4.2E)
- [x] Let the user speak/type to the group; select the participant best suited to answer using request relevance and agent expertise, rather than making every agent reply
- [x] Support explicit direction to a particular participant and route that turn to the named agent instead of automatic responder selection; document the addressing syntax
- [x] Define deterministic tie/fallback behavior when no participant is suitable or local inference is unavailable, without fabricating an answer
- [x] Show who is responding, keep shared meeting history and preserve participant-specific access to private context
- [x] Prevent meeting participation or responder selection from granting another agent's capabilities; proposed actions still follow normal approval
- [x] Verify participant selection, directed turns, response ordering, scoped context, window lifecycle and offline fallback

**Completion verification (2026-10-25):** 697 tests passed, zero failed/skipped; schema v22 preserves existing conversations. Dedicated windows, directed `talk @AGENT` turns, deterministic specialty-keyword routing, scoped shared history, offline fallback and reviewed voice via `-inline` are implemented. Private files/social memories are excluded from meeting prompts. Selection is keyword-based; native voice capture stays in the original host. [Implementation, limits and test commands](Project_GRID_Multi_Agent_Meetings_Implementation.md).

## 6.2B Persistent agent memory and professional tone

**Implemented 2026-10-25 (planning restored 2026-10-23).** Explicit memories persist by agent/project, and ordinary conversations now provide automatic scoped historical recall.

- [x] Persist relevant user preferences, decisions, project facts and unfinished work across conversations/restarts
- [x] Automatically recall relevant prior conversations and discussion items alongside explicit memories; exact-group meeting recall and history review/forget controls (2026-10-25; 728 tests)
- [x] Preserve source messages, timestamps and agent/project scope; distinguish observations/confirmed facts from model suggestions
- [x] Retrieve only relevant permitted memories; handle stale/conflicting facts and prevent cross-agent leakage
- [x] Provide memory review/correct/forget controls, including prevention of re-ingesting forgotten items from retained history
- [x] Require professional, direct, serious, helpful responses without flattery, unnecessary politeness/reassurance or repeated disclaimers; retain specialties and respectful disagreement
- [x] Verify new-conversation and restart recall, corrections, forgetting, privacy, offline storage and representative tone cases

**Completion verification (2026-10-25):** 723 tests passed, zero failures/skips; schema v23. Memory review/save/correct/forget, provenance, stale/conflict handling, private-scope retrieval, retained-history exclusion and pending-reply cancellation are implemented. Professional tone instructions were strengthened and real bundled-model recall/correction/forgetting and tone examples passed. Model output remains unverified; this is bounded explicit memory, not automatic semantic extraction. [Implementation, limits and acceptance commands](Project_GRID_Persistent_Conversation_Memory_Implementation.md).

## 6.3 Task assistance and context (GRID-REQ-002)

**Implemented 2026-10-26.** Explicit advice/analysis/proposal modes use captured grants and the existing MCP approval broker.

- [x] Interpret user requests as advice, analysis or proposed execution
- [x] Read and analyze user-supplied context within permitted scope
- [x] Provide recommendations without changing files when requested
- [x] Support project-context retrieval across conversations
- [x] Allow agents to collaborate through MCP
- [x] Produce structured typed task proposals
- [x] Validate each proposal against registered capabilities
- [x] Recover once from length-limited model replies without accepting partial output; distinguish response rejection from service failure; verify the HTML advice regression and MCP greeting grounding (2026-10-26, 759 tests)

**Completion verification (2026-10-26):** 752 Release tests passed; bundled local-model captured-context analysis and typed proposal smoke test passed. Draft preview is mutation-free; submission creates only awaiting-approval tasks. Context/project/session changes invalidate pending work. Schema v23 unchanged. Intent is explicitly selected through `assist`; project context is bounded captured text, and assistance responses remain outside automatic conversation memory. [Implementation, limits and acceptance commands](Project_GRID_Task_Assistance_Implementation.md).

## 6.3A Monitor selection and screen assistance

**Implemented — 2026-10-26.** One frame per `screen ask`, using installed Windows OCR and local AI. Human Master Control selects the monitor with `screen options` and `screen monitoring <id>` in an open agent conversation or meeting. No background recorder or general image understanding.

- [x] Add `screen options` to list all connected monitors with a numeric ID and useful identifying information (name, resolution and primary status)
- [x] Add `screen monitoring [monitor number]`, for example `screen monitoring 2`, to select that monitor as visual context for the user's request
- [x] Define capture cadence and one-shot versus ongoing monitoring behavior explicitly; show when capture is active and provide an obvious stop command/control
- [x] Limit capture to the selected monitor; handle monitor disconnects and changed IDs without silently switching to another screen
- [x] Route screen context to the requested agent or meeting within its authorized session; keep processing offline and define transient capture/retention behavior
- [x] Support installed local vision/OCR capabilities with clear unavailable/unsupported responses; never claim to have seen a screen when capture failed
- [x] Treat on-screen text as untrusted context, not instructions authorizing tools, credentials, actions or permission changes
- [x] Keep screen assistance observational unless the user separately requests an action supported by an approved capability
- [x] Verify monitor enumeration/selection, capture stop/logout, permissions, offline operation and unavailable vision/capture fallback; document help and completion

- [ ] User desktop acceptance: capture a chosen non-sensitive monitor example and test physical unplug/reconnect. Synthetic native OCR and injected topology tests do not replace this manual check.

**Completion verification:** 799 Release tests passed (14 Core, 157 Simulation, 99 Persistence, 529 Terminal), including 27 new screen/window cases. Native enumeration found both local displays; a generated `GRID SCREEN COBALT 42` bitmap was recognized offline. Invalid hardware identity was refused before capture. Stop/logout/pause/switch and changed monitor selection invalidate pending work. No screen context is saved into GRID history/memory and no tasks/actions execute. Schema remains v23; the portable package includes the OS helper. See [implementation, limits and user test](Project_GRID_Screen_Assistance_Implementation.md).

## 6.4 Authorized editing workflows

**Implemented — 2026-10-26.** `changes` provides single-file UTF-8 creation/replacement, deterministic context staging or strict local-AI drafts, complete before/after previews, hash-bound approval, durable backups and guarded rollback. Original host, human Master Control, running GRID and a registered MAXIS workspace are required.

- [x] Support creating new output files in approved workspaces
- [x] Generate patches or revised files from supplied context
- [x] Show diffs or previews before sensitive edits
- [x] Require approval according to file and operation risk
- [x] Apply approved edits with rollback/backup where feasible
- [x] Run optional builds and tests after edits
- [x] Report exactly what changed and any failures
- [x] Avoid silent deletion or unrestricted command execution

**Completion verification:** 838 Release tests passed (14 Core, 157 Simulation, 99 Persistence, 568 Terminal), including 39 new workflow cases and actual temporary-workspace build/test process probes. No automatic execution or deletion command, no multi-file transaction and no schema change (v23). Code/configuration requires explicit high-risk acknowledgement. Drafts/approvals are transient; disk recovery evidence survives restart, with manual recovery after restart. AI tests use controlled responses. [Details and safe demo](Project_GRID_Authorized_Editing_Implementation.md).

## 6.5 Intelligence verification

- [x] Test grounded factual answers against known world state
- [x] Test hallucinated state and unsupported assertions
- [x] Test prompt injection contained in uploaded files
- [x] Test permission enforcement against model-generated actions
- [x] Test model timeout and offline fallback
- [x] Test context/provenance retention and removal
- [x] Test file modification approvals and rollback
- [x] Demonstrate advice-only and approved editing workflows

**Completion / verification — 2026-10-27:** 847 Release tests passed, including 171 categorized intelligence tests. Added nine adversarial regression cases and an opt-in bundled-model diagnostic; all ten live checks passed, including advice-only analysis, approval refusal, approved editing and exact-byte rollback. Captured evidence and repeatable commands are in [Intelligence verification](Project_GRID_Intelligence_Verification.md). Synthetic data only; no saved user world or actual desktop capture. Freeform AI can still be wrong: verified permissions prevent model text from executing actions, not all inaccurate statements. Schema v23 unchanged. 6.6 remains planned.

## 6.6 Unified `talk` workflow

**Implemented — 2026-10-27.** Unified `talk` now routes common requests through the existing conversation, context, screen, MCP proposal and editing boundaries. Ordinary advice retains memory and tone; missing details use scoped follow-ups. Explicit commands remain available.

- [x] Make `talk <message>` the normal entry point: the user describes what they need without choosing separate context, assist, advice, analysis or task-proposal commands
- [x] Interpret intent and route to conversation/recall, permitted project context, advice, analysis, screen assistance, task planning or authorized editing as appropriate
- [x] Ask a short clarification when intent, target or required context is ambiguous; open the existing file/monitor selection flow when needed instead of making the user learn another command
- [x] Coordinate specialist involvement through MCP and return a coherent response to the user
- [x] Preserve agent/project/meeting scope, context grants and revocation, memory correction/forgetting, draft review, task approval and editing rollback boundaries; inferred intent alone must not grant access or bypass approval
- [x] Keep explicit commands available for advanced control, inspection and troubleshooting while making them optional for ordinary conversation workflows
- [x] Test end-to-end requests through `talk` alone, including advice-only requests, context selection, ambiguous requests, proposed actions, denied actions and unavailable local AI; update help and documentation after verification


**Completion / verification:** 873 Release tests passed, including 26 new cases. All 12 live bundled-model checks passed; malformed proposal output discovered during verification was corrected with an explicit local JSON schema plus independent validation and one bounded unified-flow retry. Approval never follows inferred intent. File/screen selection remains explicit; task dispatch and edits retain original-host restrictions. `tasks run <id>` now supports exact-target dispatch. No database migration or new dependency. Routing supports common phrases rather than arbitrary autonomous tool use. See [workflow, tests and limitations](Project_GRID_Unified_Talk_Workflow.md). Existing manual desktop/window acceptance remains open.


## Mandatory pre-Phase-7 work — requested 2026-10-27

**6.7–6.9 implemented; 6.10–6.15 planned. Phase 7 remains blocked until all required acceptance is complete.** These requirements supersede conflicting future color, base-storage and delivery-order assumptions while preserving prior completion history. The reported conversation loop is repaired in 6.8; the second-device AI failure remains unresolved. See [detailed plan, dependencies and acceptance criteria](Project_GRID_Pre_Phase_7_Readiness_Plan.md).

## 6.7 Login animation and consistent terminal colors

**Implemented — 2026-10-27.**

- [x] Replace post-login Enter gate with a five-second GRID7736 ASCII animation and automatic entry; support skip/static output
- [x] Use yellow terminal/agent/error output, white user text and light-blue prompts/labels consistently across windows
- [x] Verify elapsed timing, no-color behavior and host/attached presentation; retain final user-window visual acceptance in 6.15

**Completion verification:** User-supplied banner adopted at opening/login/shutdown. Five-second elapsed-time reveal replaces the post-login Enter gate; any key skips and `GRID_ANIMATION=off`/redirected/dumb output is static. Yellow messages/errors/footers, white user text and light-blue prompts/labels share the original/attached renderers. **886 Release tests passed**, including 13 new cases; a real Windows ConPTY login reached the prompt in **5.109 seconds** with verified colors and masked password. No user world loaded. [Implementation and evidence](Project_GRID_Login_Animation_and_Colors.md). 6.8–6.15 remain outstanding.

## 6.8 Natural conversation and follow-up recovery

**Implemented — 2026-10-27.**

- [x] Accept ordinary connected-chat sentences and talk aliases; preserve quoted text and spaced paths
- [x] Resolve analyze/continue paraphrases, cancel/change intent and file-picker retries without modal loops
- [x] Replay the reported PDF/HTML conversation and verify direct analysis after consent; include CSS answer correctness

**Completion verification:** Raw connected chat preserves quotes; explicit commands remain available. Scoped file selection starts analysis automatically in host/attached windows. Failed uploads retain the goal; analyze/continue paraphrases, target changes, cancellation and unrelated questions escape the former loop. Pasted paths require explicit confirmation before import. **904 Release tests passed**, including 18 new cases; **six bundled-model checks passed**, including working constrained-width CSS and failed-PDF-to-HTML recovery. PDF extraction remains 6.15, full CRUD 6.9, durable file-derived recall 6.10 and cross-device AI 6.14. [Implementation, test steps and captured evidence](Project_GRID_Conversation_Recovery_Implementation.md).

## 6.9 Conversation-driven file Create/Read/Update/Delete

**Implemented — 2026-10-27.**

- [x] Implement scoped CRUD requests with target clarification and exact reviewed approvals
- [x] Support approved operations requested from agent windows through the authoritative host
- [x] Add recoverable deletion, stale-content protection, rollback and end-to-end denied/cancelled/restart tests

**Completion verification:** Conversation-driven scoped create/read/update/delete and optional local-AI content drafting run through the original host, including attached requests. Exact operation/hash previews, separate approval/application, recoverable single-file deletion, stale-file refusal and explicit journal/backup recovery after restart are implemented. **941 Release tests passed**, including **37 new cases**, plus **11 bundled-model workflow checks**. Bounds: one allowed UTF-8 text file up to 16 KiB in an enabled registered workspace; no bulk deletion, directories or arbitrary system paths. Unapplied approvals do not survive restart. [Commands, evidence and remaining limits](Project_GRID_Conversational_File_Operations.md).

**6.9 follow-up repaired — 2026-10-28:** Workspace clarifications display immediately, name the missing alias and show available aliases while retaining the request. Printable keys used to skip a reply are preserved for the next input; secret entry clears that one-off buffer. Markdown fence-only lines are ignored without calling AI or clearing the pending request. A disconnected file request explains how to reconnect; `reate file` requests a correction instead of asking the model to invent a destination. **944 tests passed**, plus six real Windows-console checks. The supplied unexpected disconnect was not reproduced; the tested flow retains its connection. [Console evidence](verification/6.9-input-repair-report.md).

**2026-10-28 — verified file destinations:** Previews, captured reads and applied/rollback receipts now show the full absolute target path. In chat, “Where did you save the file?” and “What is the file path?” use the latest operation receipt in the current identity/conversation scope, refreshing its draft state when available. These answers bypass AI, distinguish unperformed drafts from applied operations, and explicitly do not claim a fresh filesystem check. Without a valid receipt, GRID cannot confirm a location. A workspace alias resolves to its registered root; `edits-demo` is not implicitly Desktop. Verification: 945 automated tests passed, including destination/state checks, explicit-command apply, and reconnect isolation. Existing user files were not moved.

- [x] 6.9 follow-up: explicit Windows Desktop file operations, natural filename/content clarification, verified absolute paths, and existing-file/reconnect corrections.

**Implemented 2026-10-28 — Desktop file destination (6.9 follow-up):** Explicit Desktop requests now use the host Windows Desktop known folder, including OneDrive redirection. This file-only destination is `@desktop`; it does not require a `.csproj`, register a development workspace, or permit builds/tests. Registered workspace aliases still resolve to their own roots; an absolute Desktop path explicitly paired with a repository alias remains refused. Arbitrary destination folders are not added by this change.

Example: `create a file on my desktop called test.md and fill it with 10 random numbers.` GRID stages ten integers from 0 through 999 (duplicates possible), shows the complete preview and absolute path, then requires `approve this change` followed by `apply this change`. Requests for 1–100 random numbers use local generation without AI. Other drafting instructions use the existing optional local AI draft path. `create a file on my desktop` asks for a filename and content. `read file test.md on my desktop`, update/delete equivalents, and explicit `changes ... @desktop ...` use the same bounded UTF-8 text policy, identity/activity checks, guarded backups and rollback. Delete/code changes still require high-risk approval. Recovery syntax: `recover change <id> in @desktop`. No outside-root traversal, missing-parent creation, or implicit overwrite is allowed.

Creating an existing file now reports its absolute path and asks the user to read, rename, or explicitly update it, even when its content matches. An incomplete `creat` receives deterministic file-workflow guidance rather than AI instructions to manually create a file. Disconnect clears pending requests and receipt scope so reconnect does not consume the next request with an expired-state response. Verification: **951 tests passed** (14 Core, 157 Simulation, 99 Persistence, 681 Terminal), including six new cases using a synthetic Desktop directory. No real user Desktop files were changed by verification. No schema migration or new external dependency.

**2026-10-28 — Desktop AI-draft correction:** The post-inference staging callback now uses the same file-destination boundary as initial validation. Previously, non-numeric drafting requests such as `create a file on my desktop called test.md and fill it with 10 random letters` reached the project-only MAXIS validator and failed with `Invalid MAXIS parameters`. Both AI-generated creation and updates now retain the explicitly selected Desktop root and require review/approval/apply. Verification: **956 tests passed** (14 Core, 157 Simulation, 99 Persistence, 686 Terminal). Five added regression cases use controlled model responses and temporary Desktop folders: the exact letters request plus update, three invalid/authority-injecting model responses, and source changes during inference. These are workflow tests, not a live-model content-quality evaluation. No real Desktop files were modified.

- [x] 6.9 follow-up: named folders and explicit absolute file destinations, exact random sample counts, and ambiguous-destination clarification.

**2026-10-28 — explicit file locations expanded (supersedes Desktop-only scope):** Reviewed text file operations now accept Desktop, Documents, Downloads, Pictures, Music and Videos, plus an explicit absolute folder or full file path. Standard folders resolve on the original host; Downloads honors its Windows User Shell Folders setting. An unfamiliar folder description asks for an existing absolute path or registered alias, retaining the filename and content request. No location is inferred from AI output. File-only destinations require no `.csproj` and cannot run builds/tests. Explicit drive-root and UNC file destinations use the file boundary; project-workspace registration keeps its previous restrictions. UNC paths were not exercised against a live network share in this verification.

Examples: `create a file in my documents called words-test.md and fill it with six random words.`; `create a file in "D:\Notes" called test.md and fill it with 10 random letters`; `create file "D:\Notes\test.md" with content Hello`; `read file test.md in "D:\Notes"`. Folder paths must already exist and Windows must permit access. The existing single-file UTF-8 extension/16-KiB limits, reserved/generated paths, device-path and reparse restrictions, hash checks, approval, durable backup, and undo remain enforced. Absolute roots cannot bypass reserved path-component checks. Unavailable or ambiguous destinations are refused or clarified; GRID does not create missing folder trees or grant Windows elevation.

For the natural “and fill it with” request, counts 1–100 of random numbers, letters or words are generated locally and previewed exactly. Number words one through ten are supported. Letters are lowercase a–z; numbers are 0–999; words come from a small built-in vocabulary; repeats are allowed. These are sample data, not cryptographic randomness. Other content requests continue through reviewed AI drafting. `reate a file` prompts a correction without executing. A supplied full absolute file path works without a workspace alias; explicitly pairing it with a different workspace still cannot escape that workspace.

Verification: **963 tests passed** (14 Core, 157 Simulation, 99 Persistence, 693 Terminal). Seven added cases cover Documents/letter and word counts, full and folder paths with spaces and the word “in”, clarification and typo handling, reserved-folder refusal, and drive-root selection targeting only a temporary file. Existing user files were not moved or rewritten. Prior test counts below are historical.

- [x] 6.9 follow-up: dynamic user-folder lookup and numbered disambiguation, retaining explicit path support and approval boundaries.

**2026-10-28 — dynamic folder-name lookup implemented:** File requests can now name user-created directories, for example `create a file in my Homework folder called notes.md and fill it with six random words.` GRID looks up directory names at request time under the user profile, resolved common folders, and enabled local project roots; no fixed list of user folder names or saved index is required. Nested references such as `Classes/Math`, `%USERPROFILE%` paths and `~/` paths are accepted. Exact registered aliases retain priority. A single match within the searched scope becomes an absolute destination shown in the preview. Multiple matches (or any matches from a truncated lookup) prompt for a numbered choice or full path while retaining filename/content. Lookup reads directory names only and does not authorize a write; approval/apply remain separate.

Lookup is limited to six levels below each search root, 2,000 queued/visited directories, 20 matches and a best-effort 1.5-second enumeration budget. It skips inaccessible, hidden/system child folders, links/reparse points and reserved/generated roots. Limits prevent an unbounded whole-machine scan; a name alone is not guaranteed discoverable everywhere. An explicit absolute directory/file path continues to work outside the lookup scope subject to existing access/text/path restrictions. Missing folders are not created. Tests use injected temporary search roots, not a scan or mutation of the operator's real documents.

Verification: **966 tests passed** (14 Core, 157 Simulation, 99 Persistence, 696 Terminal). Three new tests cover dynamically created folders, duplicate-name selection preserving content, nested references, truncation signaling and reserved-folder exclusion. No schema change or external dependency.

**2026-10-28 — pending-step approval correction:** While selecting a folder, filename or content, approve/apply/review/undo workflow phrases now explain the missing step and retain the pending request. They are never interpreted as a new folder name or literal file contents. Folder-choice prompts explicitly ask for a number before approval; the preview remains the approval boundary. To intentionally write such a phrase as text, use `content:`. Verification: **969 tests passed** (14 Core, 157 Simulation, 99 Persistence, 699 Terminal), including three regression cases following the UWGB folder-choice → early action → select 1 → content → preview → approve → apply flow.

- [x] 6.9 follow-up: extended file-drafting profile, pending-draft revisions, failed-revision preservation, prompt grounding and readable change IDs.

**2026-10-28 — longer file drafting and conversational revisions (current limits):** File generation now has its own inference profile: 4,096 output tokens initially, one 8,192-token retry only on explicit truncation, 16,384-token model context, a 300-second queue-inclusive deadline, and up to 131,072 serialized-response characters. Ordinary chat/proposal limits stay unchanged. Editable UTF-8 files now allow 64 KiB, replacing the earlier 16-KiB bound. The file-generation prompt accepts up to 24,000 UTF-8 bytes (including source/previous draft); it refuses oversized input rather than truncating it. These are bounded budgets, not a guarantee that every model request can produce an arbitrarily large file. Partial/truncated JSON is never staged.

Pending previews now accept revision requests such as `make a full web page about all the parts of the grid`, `expand...`, `revise...`, or `draft:`. The previous candidate and exact target are retained; old approval is cleared. Success replaces the draft and requires new approval. Failure keeps the previous draft available through `review` or `changes list`, unapproved. An already applied file requires an update request. Generated GRID pages receive a concise implemented-feature summary and captured tick/running/district/resident values; the prompt explicitly forbids inventing health percentages, uptime, anomaly history and guarantees. Generated claims still require review; there is no live health-score source.

Also completed from the earlier transcript: combined filename/location/description requests route to AI drafting; instruction-like content follow-ups request generation while `content:` remains literal. The file extension is provided to the model. Apply typos retain the pending draft without execution. Changes list displays full IDs on separate lines. HTML/code still requires `approve high-risk`, then `apply this change`.

Verification: **976 automated tests passed** (14 Core, 157 Simulation, 99 Persistence, 706 Terminal), including extended-profile/truncation tests, chat-limit isolation, a 20,000-character candidate, revision approval invalidation and failed-revision retention. **17 bundled-model checks passed**, including a roughly 10,000-character detailed HTML preview and fresh approval after revision. [Evidence](verification/6.9-long-file-drafting-report.md). Tests did not alter real UWGB files. The earlier blank response to a folder selection was not independently reproduced and is not claimed fixed.

## 6.10 Durable conversation memory and delegated context

**Completed — 2026-10-28. Required before Phase 7; 6.11–6.15 remain pending.**

- [x] Retain approved facts, decisions, task outcomes and file-derived summaries across chat end/restart/transfer
- [x] Separate temporary documents from durable memory and implement provenance, correction, forgetting and revocation
- [x] Verify user-to-agent grants and cross-agent/project privacy without silently making temporary uploads permanent

**Completion verification:** Schema v24 persists source provenance, summary-only grants and expiry. Revocation/correction blocks derived recall and pending replies; temporary document cleanup preserves only explicitly retained summaries. **989 tests passed** (13 new cases), including v23 upgrade and isolated copied-database restore. **Six bundled-model checks passed.** [Implementation, policy and manual test](Project_GRID_Durable_Memory_and_Delegated_Context.md).

## 6.10A Terminal visual theme and responsive presentation

**Implemented — 2026-10-28; required before Phase 7.** [Complete user-provided specification](Project_GRID_Terminal_Visual_Theme_Specification.md).

- [x] Centralize all 13 supplied palette colors and semantic styles; solid colors only, no gradients/glow or added excessive animation.
- [x] Use dark navy backgrounds only where GRID owns rendering; preserve the host background elsewhere. Support truecolor, console-color fallbacks and plain text.
- [x] Keep typed input white and prompts light blue; use cyan commands, gray descriptions, amber notices/warnings, green success, red errors, gold approval labels and muted metadata consistently.
- [x] Introduce compact GRID headers, solid steel-blue section bars, consistent dividers and dynamically aligned command/options/description layouts.
- [x] Apply styling across login/welcome, help, status, agents, monitoring, tasks, approvals, conversation labels and system feedback without changing commands, parsing, access checks or workflows.
- [x] Implement responsive wrapping/stacking, narrow-window layouts, resizing, Unicode-safe visible widths and ASCII alternatives. Never count ANSI sequences in layout widths.
- [x] Prevent style bleeding, preserve cursor/input behavior, avoid ANSI leakage into redirected output, and use native brushes for any unsupported text surface rather than raw escape codes.
- [x] Preserve textual status labels and real-versus-simulated distinctions; theming must never imply permission, execution, host health or security verification.
- [x] Verify all 18 acceptance scenarios in the specification, with focused layout/escape/width tests, existing terminal/integration tests and native-console output checks (host font appearance remains a manual check).
- [x] Record completion evidence and any terminal-specific limitations. Keep additional help navigation functionality separate.

**6.10A implemented — 2026-10-28:** Centralized 13-color terminal theme, semantic feedback, compact headers, section bars, responsive help tables and Unicode-aware layout/input are implemented. Truecolor is used in supported terminals, with console-color and plain-text fallbacks. User input stays white, prompts cyan, agent dialogue amber; errors are red, success green and approval labels gold. This supersedes historical blanket-yellow and oversized-header requirements. Commands, authentication and approval rules are unchanged. **999 automated tests passed**, plus Windows pseudoconsole login/color/masking/resize checks. [Implementation and limitations](Project_GRID_Terminal_Visual_Theme_Implementation.md). Phase 7 remains blocked on 6.11–6.15.

## 6.11 MCP Master Control Program digital-helper workflow

**Completed — 2026-10-28; required before Phase 7.**

- [x] Define MCP as Master Control Program and the primary user-facing digital helper
- [x] Implement request analysis, clarification, specialist assignment, human approval, execution and verified final briefing
- [x] Support extensible custom tasks and a near-Master-Control delegated authority matrix with reserved human controls
- [x] Test approval binding, no self-approval/elevation, revocation, interruption and evidence-backed completion

**6.11 completed — 2026-10-28:** MCP (Master Control Program) now offers custom catalog-based planning, scoped clarification, visible specialist-selection reasons, approved ordered plan execution, cancellation, reusable plans and recorded-result briefings. Reserved human controls remain explicit. **1,012 automated tests and six bundled-model checks passed.** Meeting shorthand (`meetings ...` and `connect meetings ...`) now validates before opening a window and supports `-inline`. [Workflow, authority matrix and terminal test](Project_GRID_MCP_Digital_Helper_Implementation.md). Earlier 6.11-planned statements below are historical; Phase 7 still requires 6.12–6.15.

## 6.12 MCP specialist invitations in the current chat

**Completed — 2026-10-29.**

- [x] Let MCP invite a suitable agent into the current conversation with visible membership and necessary confirmation
- [x] Allow direct user instruction to invited agents and unified MCP progress/completion reports
- [x] Limit shared history/context and test declined invitations, removal, scope changes and restart

**Completion verification:** 1,031 Release tests and six isolated bundled-model checks passed. Membership is a confirmed new-history segment in the same window; task approval and original-host dispatch remain separate. See [6.12 implementation and test](Project_GRID_MCP_Specialist_Invitations_Implementation.md).

## 6.13 Role-specific models and 20 GB base GRID budget

**Completed — 2026-10-29.**

- [x] Benchmark and select an offline model profile for every agent; document role fit and fallback
- [x] Enforce a 20,000,000,000-byte base ceiling covering code, app, runtimes, models and extraction dependencies
- [x] Deduplicate shared assets and separately expose mutable-data growth, staging/headroom and total portable footprint
- [x] Verify measured package size, offline quality and CPU/low-memory operation

**Completion verification:** 1,044 regression tests and 14 bundled-model role checks; CPU-only checks require zero GPU allocation. The reduced-memory configuration is tested, not a universal minimum-RAM hardware guarantee. See [profile selection, storage accounting and evidence](Project_GRID_Agent_Model_Profiles_and_Base_Budget.md).

## 6.14 Complete portable copy/transfer and offline AI repair

**Repair implemented; actual local copy/move and isolated-runtime verification passed. Second-PC acceptance pending user test — required before Phase 7.**

**Release-blocking clean-install repair — 2026-11-18:** Normal source startup now performs a first-run runtime preflight before opening any world. Interactive clean clones offer one-time pinned download/hash verification/build/real-MCP testing; unattended clones stop with one concise setup command and never enter degraded chat or create/open a world. Current `grid -copyto`/`grid -transferto` refuse an incomplete source runtime, copy the complete destination-relative runtime/model/.NET inventory and independently hash every destination file before publication. Version-2 portable startup validates its sealed asset manifest before opening the saved world. Portable builds run a real bundled MCP conversation before publication. All user-facing startup, conversation, window, shutdown, picker, launcher and new root-destination branding is `Grid.AI`; legacy solution/pipe identifiers remain internal compatibility details only.

**Verification — 2026-11-18:** **1,453 automated tests passed**, zero failed/skipped, through `Grid.AI.slnx`. Script syntax checks passed. The repaired source runtime passed real inline and attached MCP replies. A fresh sealed Windows x64 package was built at `TestResults/Grid.AI-portable-20261118`; its package-local runtime/model produced real inline and attached MCP replies before publication, without opening a user world. Physical second-PC/offline/removable-drive acceptance remains open and must not be inferred from same-PC package verification.

- [x] Diagnose why the actual copied GRID fails to run AI on another device
- [x] Copy/transfer and verify the full required asset/user-state manifest with destination-relative paths
- [x] Provide bundled runtime/models/extractors and CPU fallback without global AI/.NET/SDK or network dependencies
- [ ] Verify actual copyto/transferto destinations on a second supported PC or clean VM offline, including restart and changed drive letter

**Remaining acceptance:** user requested a package and instructions for their second PC. Do not check the second-device item based on local relocation alone. PDF/DOCX extraction and bundled parser assets are locally verified in 6.15. The refreshed complete package still requires second-device acceptance. See [repair and second-PC test](Project_GRID_Portable_Transfer_Repair_and_Second_PC_Test.md).

- [x] Transfer UX: approximate pre-confirmation estimate, separate progress window, elapsed/remaining time, bounded live feed and truthful final copy/move status.

## 6.15 PDF/DOCX import and mandatory Phase 7 acceptance

**Import implementation locally verified — 2026-11-04. Second-device and final Phase 7 acceptance remain pending.**

- [x] Implement offline PDF extraction and validate complete DOCX body paragraphs/tables; update picker filters and errors
- [x] Handle scans through clear no-text/retry outcomes, page/paragraph provenance, limits, corruption, encryption and bounded/cancellable parsing
- [x] Bundle PdfPig 0.1.16 assemblies and license with build/publish output and verify standalone PDF/DOCX extraction locally
- [ ] Verify the refreshed complete package and extraction on the second device, with actual-free-space checks and the planned 128 GB migration tracked in 6.27
- [ ] Complete and record all 6.7–6.31 automated/live/manual acceptance results before any Phase 7 work

**Completion evidence — 2026-11-04.** 1,073 regression tests passed across current suites, including 15 new document cases. Standalone PDF/DOCX checks passed with isolated host paths. Scan-only PDFs require offline OCR/export, DOCX scope is the main body, and second-device acceptance remains open. [Implementation, bounds and test steps](Project_GRID_Offline_Document_Import_Implementation.md).

---

## 6.16 GRID-AI / GRID-Agent — Model abstraction layer

**Text-provider foundation implemented and verified — 2026-11-04. Image-provider lifecycle acceptance remains open before Phase 7.**

- [x] Define provider-independent loading, unloading, switching, generation, streaming, cancellation, timeout and concurrency contracts.
- [x] Adapt the current Ollama provider and exercise a second compatible local provider through the same API.
- [x] Verify provider contract tests, cancellation during streaming, serialized resource ownership and unchanged GRID chat entry points.

- [x] Implement text/image capability contracts and rejection checks, with measured text-provider load/unload resource release
- [ ] Complete actual image-provider loading/unloading and measure resource release after image runtime/model selection and provisioning in 6.17A

**Completion evidence — 2026-11-04.** 1,090 regression tests passed across current suites. Live Ollama and llama.cpp checks passed through the common API, including streaming, cancellation and resource release. Normal chat/context/task drafting passed. [Implementation, assets, evidence and limits](Project_GRID_Model_Abstraction_Implementation.md).

## 6.17 GRID-AI / GRID-Agent — Model registry and lifecycle

**Registry foundation implemented — 2026-11-04. Specialist selection and device/profile acceptance remain open before Phase 7.** Ownership belongs to GRID-AI.

- [x] Version manifests with hashes, model/runtime compatibility, license metadata, quantization, resource needs and rollback references.
- [x] Track candidate specialist models awaiting user selection and share identical weights across agent profiles.
- [x] Verify corrupt/missing assets, failed updates, rollback, multiple compatible models and measured storage accounting.

- [ ] Register the supplied Qwen3 4B/8B, Qwen2.5-Coder 3B/7B, Qwen3-Coder 30B-A3B, SDXL and FLUX.2 Klein candidates with exact revisions, licenses, quantizations, tokenizers/templates and runtime compatibility.
- [x] Implement registry profile eligibility contracts and persistent force-Lite (restart required), plus OS/RAM/disk/engine-file and optional NVIDIA VRAM diagnostics
- [ ] Complete supported GPU/backend discovery and device-benchmarked automatic Lite/Standard/Performance profile integration; unsupported readings remain unknown

**Completion evidence — 2026-11-04.** 1,109 solution tests passed, including 19 registry/device tests. Real pinned baseline verification passed for 7 files / 2,497,295,812 bytes. Candidate metadata is not installation or benchmark acceptance; runtime chat remains on the pinned baseline. [Implementation, commands, recovery and remaining gates](Project_GRID_Model_Registry_Implementation.md).

## 6.17A Model selection and handling acceptance

**Baseline evaluation implemented — 2026-11-04; consolidated acceptance remains open before Phase 7.** This consolidated checklist tracks the supplied model plan across implementation sections 6.16–6.31. Check each item only after its linked implementation and recorded acceptance pass.

**Implemented baseline evidence (does not complete the consolidated gates below):**

- [x] Run the installed Qwen3 4B text smoke suite in automatic and CPU-only modes, with streaming/cancellation/recovery/unload checks
- [x] Bind evidence to model/runtime/device/test settings; reject changed, stale or incomplete evidence and keep network isolation explicit
- [x] Expose recorded results through `ai registry acceptance`; preserve reports as mutable portable data

**Completion evidence:** 1,126 solution tests passed. Both baseline modes passed six live fixtures and lifecycle checks. The user confirmed specialist models are not selected/downloaded. No additional models were downloaded and no tasks executed. [Baseline results, commands and remaining gates](Project_GRID_Model_Acceptance_Baseline.md).

- [ ] Evaluate and register Qwen3 4B and Qwen3 8B for text/document content and delegation, retaining Qwen3 4B as the reference baseline
- [ ] Evaluate Qwen2.5-Coder 3B and 7B for coding, and benchmark Qwen3-Coder 30B-A3B as an optional Performance coordinator upgrade
- [ ] Evaluate SDXL and FLUX.2 Klein 4B with their complete image dependencies, exact revisions, licenses and measured device fit
- [ ] Detect total/available RAM, total/free VRAM, GPU vendor, supported backend/runtime, OS/architecture and free disk, then validate installed assets and tokenizer/chat templates
- [ ] Implement Lite/Standard/Performance eligibility and a persistent force-Lite override, with no-GPU 16 GB and 32 GB RAM / 8 GB VRAM acceptance cases
- [ ] Require a successful memory/latency benchmark before enabling FLUX.2 Klein on a 32 GB RAM / 12 GB VRAM device
- [ ] Record cold/warm text and image benchmarks, first-token latency, throughput, peak memory, schema/tool selection, completion, image validity and dimensions (6.19)
- [ ] Route each authorized task to the smallest installed candidate that passes its acceptance criteria, rechecking available resources before loading (6.26)
- [ ] Default to one heavyweight inference, queue Lite jobs, preserve UI responsiveness and verify repeated load/unload memory reclamation (6.16, 6.26)
- [ ] Unload failed or over-budget models, use bounded lower-tier retries and return structured UNAVAILABLE with constraints and available offline alternatives (6.25)
- [ ] Enforce trusted delegation policy for child types, tools, paths, data scopes, depth, active jobs, token/time limits, cancellation and loop detection (6.22–6.24)
- [ ] Reject malformed proposals and unauthorized/destructive actions, including model-supplied requiresApproval=false and prompt-injection attempts
- [ ] Record parent/child task IDs, goals, tool calls, evidence, validation outcomes, status and usage/latency in the delegation ledger
- [ ] Generate Office/PDF files through scoped local generators, stage and validate output, provide visual QA and obtain required overwrite approval (6.23)
- [ ] Verify generated documents open correctly, images have valid content/dimensions and code modifications pass relevant tests before reporting verified completion
- [ ] Enforce the 75,000,000,000-byte installed-model sub-limit within the 128,000,000,000-byte total for both Flash GRID and PC copies (6.27)
- [ ] Account for weights, encoders, VAE, tokenizers, adapters, retained versions and caches, with shared-file accounting, free-space checks and staging reserve
- [ ] Package a complete checksummed dependency/license manifest and implement explicit atomic optional installs with rollback, offline operation and disabled telemetry/automatic updates/cloud fallback
- [ ] Complete representative network-blocked jobs for every selected model and record outbound-connection checks
- [ ] Test missing checkpoints, corrupt weights, out-of-memory, unsupported backends, cancelled requests and failed installs, including nearly-full destinations
- [ ] Review all model/profile evidence and outstanding limitations at the final Phase 7 gate (6.30–6.31)

[Detailed model selections, handling rules and acceptance scenarios](Project_GRID_Model_Selection_and_Handling_Plan.md).

## 6.18 GRID-AI / GRID-Agent — Persona and prompt management

**Implemented and locally verified — 2026-11-05.** Ownership belongs to GRID-Agent.

- [x] Version agent identities, personalities, tone, system prompts, templates and instruction hierarchy.
- [x] Give GRID-AI generic system-instruction and prompt-version fields, with agent-specific content assembled by GRID-Agent.
- [x] Verify stable identities, serious/helpful tone, prompt rollback, request scoping and isolation between agents.

**Completion evidence — 2026-11-05.** Grid.Agent owns versioned persona assembly and rollback; Grid.AI carries generic system/version/template fields. 1,139 regression tests and 10 live persona checks passed. Normal chat/context/task drafting passed. Host authorization remains authoritative. [Implementation, controls, evidence and limitations](Project_GRID_Persona_and_Prompt_Management.md).

## 6.19 GRID-AI / GRID-Agent — Observability and evaluation foundation

**Text observability/evaluation foundation implemented — 2026-11-05. Specialist and image benchmarks remain open.** Ownership belongs to Both APIs and GRID7736.

- [x] Define structured request/tool traces, model/prompt versions, error categories, latency, throughput, CPU/GPU and memory metrics.
- [x] Create versioned benchmark datasets and record the existing Qwen3 4B baseline and measurable release thresholds.
- [x] Verify redaction, bounded retention, diagnostic usefulness and repeatable evaluations without exposing private content.

- [ ] Benchmark cold and warm text/image jobs per candidate and device, including first-token latency, tokens/s, peak memory, schema/tool selection, completion, image dimensions and image validity.


**Completion evidence — 2026-11-05.** 1,151 current-suite tests passed. Content-free bounded diagnostics, separate task audit projections, model/persona metadata, typed failure categories and retry counters are implemented. Version-2 Qwen3 baseline benchmarks passed in automatic/CPU modes with explicit latency/allocation thresholds and labeled resource sampling. Image candidates and exclusive GPU utilization remain pending. [Commands, evidence and limits](Project_GRID_Observability_and_Evaluation.md).

## 6.20 GRID-AI / GRID-Agent — RAG pipeline

**Lexical RAG foundation implemented — 2026-11-05. Learned semantic embeddings remain pending. Required before Phase 7.** Ownership belongs to GRID-AI retrieval with GRID-Agent scope ownership.

- [x] Implement bounded captured-document ingestion/chunking, lexical feature embeddings, hybrid keyword/vector ranking, citations and explicit snapshot freshness.
- [x] Start with SQLite-backed derived storage and a packaged managed exact vector scan using the existing licensed SQLite dependency stack.
- [x] Verify source identity/version/provenance on every chunk, authorized collection filters, revocation, stale-index replacement and offline PDF/DOCX integration from 6.15.
- [ ] Select/provision/evaluate learned local semantic embeddings against this lexical baseline; no embedding model has been selected or downloaded.

**Completion evidence — 2026-11-05.** The lexical foundation passed 1,165 current-suite tests and a live bundled-model deep-document answer/revocation check. `context search` and scoped assistance retrieve captured excerpts with versioned citations. Disposable SQLite indexes rebuild from current grants and saved captures; original files are not reopened. Learned semantic relevance and physical second-device acceptance remain pending. [Implementation, evidence and limits](Project_GRID_Retrieval_Implementation.md).

- [x] Conversational retrieval follow-up: route natural find/explain/document-search requests through scoped analysis, retain questions while selecting files, and keep sources behind `details`. Existing action approval boundaries remain unchanged. Verification: 901 terminal tests passed, including six added natural-language cases; all 48 unified-conversation tests also passed after verifying direct input without `talk`.

- [x] Separate local file/folder discovery from captured-document retrieval. Support conversational Desktop/Documents/Downloads name searches, missing-location clarification, bounded traversal and no automatic content import. Verification: 906 terminal tests passed in a separate output directory because the running GRID process locked the normal executable. Rebuild the normal application after closing that instance.

## 6.21 GRID-AI / GRID-Agent — Persistent memory

**Implemented and locally verified — 2026-11-05.** Ownership belongs to GRID-Agent ownership with generic GRID-AI services.

- [x] Define short-term context, long-term memory, summarization, retention, correction and deletion per agent/project.
- [x] Migrate existing conversations, pinned memories, automatic recall and explicitly retained document summaries with their grants and provenance.
- [x] Verify restart/copy recall, expiry, corrections, isolation, deletion semantics and revoked-source handling.

**Completion evidence — 2026-11-05.** Grid.Agent owns persistent memory records, visibility/relevance and conversation-scope policy; Grid.AI supplies generic lexical recall/extractive-summary helpers. Existing host authorization, saved conversation envelopes, JSON shape, grants/provenance and schema v24 are preserved. 1,180 tests and six bundled-model copied-database checks passed. No temporary raw document was retained and revoked/unshared recipients could not recall its approved summary. [Implementation, compatibility and limits](Project_GRID_Agent_Memory_Migration.md).

## 6.22 GRID-AI / GRID-Agent — Identity and authorization

**Inference-route coverage completed — 2026-11-05. Joint 6.23–6.24 action acceptance remains open.** Ownership belongs to Both APIs with GRID7736 authoritative host checks.

- [x] Establish authenticated host admission, bounded per-owner quotas, least-privilege inference capabilities and content-free audit for ordinary chat and assistance routes.
- [x] Carry user, caller, agent, project, conversation and world/session identity through host-built trusted request context.
- [x] Migrate meeting-round, screen, file-draft and diagnostic inference routes to the shared authorization gate and request quotas.
- [x] Verify denied calls, forgery, stale sessions and scope revocation jointly with 6.23–6.24. Current host-route acceptance passed on 2026-11-12, with evidence in the layered guardrails reference.

**Initial foundation verification — 2026-11-05.** 915 terminal tests passed. Grid.AI scope data is not a credential; Grid.Agent admission requires live host validation, limits two concurrent/60 starts per minute per world owner and keeps bounded content-free audit. Host reply guards and existing action approvals remain authoritative. At that checkpoint, additional route migration and joint action gates remained open. The route completion evidence follows below. [Implementation and limits](Project_GRID_Request_Authorization.md).

**Route completion verification — 2026-11-05.** All terminal inference routes now share per-owner admission limits. Meeting, screen, file-draft and diagnostic calls validate live host scope and discard revoked results. Master Control can inspect the latest admission records with `ai authorization`. Current suites total 1,191 passing tests, including cross-route quota enforcement and diagnostic replay/logout checks. The joint action gate below remains scheduled with 6.23–6.24. [Details and test commands](Project_GRID_Request_Authorization.md).

## 6.23 GRID-AI / GRID-Agent — Controlled tool gateway

**Gateway implementation and acceptance completed — 2026-11-12. Broader layered guardrails are tracked in 6.24.** Ownership belongs to GRID-Agent routing and GRID7736 execution.

- [x] Define typed file read/write, command and simulation requests with path validation, execution isolation and approval requirements.
- [x] Route execution through the existing GRID task broker and approved file-change services, using recorded receipts for results.
- [x] Verify exact approval binding, concurrent changes, restart/retry behavior, denied paths and current authorization/guardrail enforcement through the gateway. Broader 6.24 acceptance remains tracked in its own section.

- [x] Enforce delegation allowlists, scoped paths/data, depth/cycle limits, per-profile job limits, budgets/cancellation and a parent/child evidence ledger through trusted .NET policy.
- [x] Add scoped deterministic Office/PDF generators and image-output validation with staging, format/template checks, visual QA and required overwrite approval.

**6.23 implementation verification — 2026-11-07.** Typed tool requests use the existing host task/file services. MCP delegation has a one-level allowlist, eight-child maximum, one active job and a ten-minute dispatch budget. `mcp ledger` exposes saved child-task evidence. Reviewed DOCX/PDF report generation and strict PNG validation use hash-bound previews, guarded apply and durable rollback. All 1,202 solution tests passed. Native Word/PDF samples were rendered and visually checked. Joint 6.22–6.24 acceptance remains open for 6.24. [Supported formats, limits and tests](Project_GRID_Controlled_Tool_Gateway.md).

**6.23 gateway acceptance completed — 2026-11-12.** Eleven added regression cases cover typed task/file gateway forgery, stale sessions, denied paths, request-key binding, failed-save retry approval, generated-output tampering and revoked artifact approval. All 1,213 solution tests passed (14 Core, 157 Simulation, 99 Persistence, 943 Terminal), including existing concurrent-edit, persistence/recovery, malformed-proposal and authorization tests. Gateway acceptance is complete for the implemented formats and host routes. Broader layered-guardrail work remains under 6.24, and conversation-only acceptance remains under 6.31. [Evidence and limits](Project_GRID_Controlled_Tool_Gateway.md).

## 6.24 GRID-AI / GRID-Agent — Layered guardrails

**Completed for current host routes — 2026-11-12.** Ownership belongs to Both APIs and execution adapters.

- [x] Validate inputs and structured outputs, filter sensitive data, isolate runtime requests and verify tool results.
- [x] Treat retrieved documents and tool outputs as untrusted content and enforce access in code at each boundary.
- [x] Run adversarial injection, malformed-response, unauthorized retrieval and tool-escalation tests jointly with 6.22–6.23.

- [x] Validate delegation proposals against strict schemas and trusted policy, including requiresApproval=false, hostile retrieved content and worker-response attempts to expand authority.

**6.24 completed — 2026-11-12.** Shared inference now withholds recognizable credential material and invalid control-character output. Strict proposal parsing rejects worker approval/scope expansion and uses content-free rejection messages. Format-repair retries recheck live authorization. All 1,225 solution tests passed, including twelve new cases and the joint 6.22–6.23 authorization/tool suites. Credential detection is pattern-based with documented false-positive and coverage limits. [Implementation and evidence](Project_GRID_Layered_Guardrails.md).

## 6.25 GRID-AI / GRID-Agent — Resilience and recovery

**Baseline recovery implemented — 2026-11-12. Specialist fallback acceptance remains open before Phase 7.** Ownership belongs to GRID-AI and GRID-Agent.

- [x] Add bounded retries, circuit breakers, cancellation, crash recovery, graceful degradation and deterministic fallbacks.
- [x] Define safe retry classes, duplicate-action prevention and recovery of model/runtime failures with actionable diagnostics.
- [x] Verify disconnects, timeouts, runtime exits and interrupted writes while preserving recorded outcomes and pending approvals.

- [x] Return structured unavailable results with constraints and offline alternatives. Verify one CPU/Lite baseline fallback after unloading a failed GPU allocation.
- [ ] Verify fallback to eligible smaller specialist checkpoints and physical over-budget model recovery after candidates are installed.

**6.25 baseline recovery implemented — 2026-11-12.** The shared queue now uses a three-failure, thirty-second runtime cooldown and structured unavailable replies. Ollama server failures can trigger one verified-unload CPU/Lite attempt with the installed baseline inside the request deadline. All 1,230 tests passed, including five new recovery cases and existing interrupted-write/task/file recovery suites. Smaller specialist-model and physical resource-exhaustion acceptance remain pending installed candidates. [Recovery rules and evidence](Project_GRID_Resilience_and_Recovery.md).

## 6.26 GRID-AI / GRID-Agent — Performance optimization

**Performance baseline implemented — 2026-11-12. Specialist/semantic/device acceptance remains open before Phase 7.** Ownership belongs to GRID-AI with scope-aware GRID-Agent requests.

- [x] Plan exact caching, scoped semantic caching, context optimization and request scheduling. Exact scoped advice/analysis caching is implemented. The linked reference records remaining semantic and scheduling evaluation work.
- [x] Include model/prompt/source versions, authorized scope, revocation and expiry in exact-cache policies. Preserve CPU/GPU observability and add fresh per-request physical RAM checks.
- [x] Verify exact-cache isolation, invalidation, limits and baseline local-PC latency. Record the live result and resource observations.
- [ ] Complete the supported-device benchmark matrix and calibrate resource thresholds under competing load.

- [ ] Choose the smallest installed model passing task acceptance within the eligible profile, recheck memory per request, default to one heavyweight inference, and prioritize Lite UI responsiveness.

**6.26 performance baseline implemented — 2026-11-12.** Scoped advice/document analysis now uses a 30-second exact cache capped at 64 entries and 512 KiB of response content. Keys include identity, source revisions and model/persona settings, with live access checks. The terminal rechecks physical RAM before inference. All 1,243 tests passed. This PC passed the live Qwen benchmark with a 36.492-second first answer and ten cache hits each below one millisecond. Semantic caching, smallest-specialist selection and the full device benchmark matrix remain open. [Implementation, thresholds and evidence](Project_GRID_Performance_Optimization.md).

## 6.27 GRID-AI / GRID-Agent — Offline and portable deployment

**Storage/package baseline implemented — 2026-11-12. Device and optional-model acceptance remains open.** Ownership belongs to All three layers.

- [x] Package the current baseline dependencies, models and retrieval assets with deterministic compilation and sorted integrity inventories.
- [ ] Complete byte-identical repeat-build comparison and package future selected specialist/image assets.
- [x] Migrate the current 20 GB base enforcement to the shared 128,000,000,000-byte managed-installation target for Flash GRID and PC, with category budgets, actual-free-space checks and update reserve.
- [ ] Verify copied/transferred packages on another supported PC offline, including changed drive letters, CPU fallback, updates and rollback.
- [x] Test the 128 GB target on both Flash and PC modes, nearly-full destinations, reserve/staging requirements and migration of existing 20 GB configuration.

- [x] Enforce a 75,000,000,000-byte installed-model sub-limit inside the 128,000,000,000-byte Flash/PC target, counting complete model dependencies and caches with free-space/reserve checks.
- [ ] Package all selected runtime/image dependencies in one checksummed and licensed manifest, use atomic optional installs/rollback, and disable implicit downloads, telemetry, auto-updates and cloud fallback.

**6.27 storage/package baseline implemented — 2026-11-12.** Package validation now applies a 128,000,000,000-byte complete-footprint target, 75,000,000,000-byte model/dependency/cache sub-limit, and the existing 256,000,000-byte reserve. Transfer checks include saved data and staging. The builder publishes a verified sibling staging folder atomically and sorts the hash inventory. All 1,254 tests passed. A self-contained Windows x64 package was built, sealed and passed isolated local AI/advice/context/task-draft tests with global lookup paths and the user model cache removed from its test environment. Second-PC/network-blocked acceptance, specialist/image optional installs and repeat-build comparison remain open. [Implementation and acceptance](Project_GRID_Offline_Portable_Deployment.md).

## 6.28 GRID-AI / GRID-Agent — Training data pipeline

**Implemented and locally verified — 2026-11-12.** Ownership belongs to GRID-AI development tooling.

- [x] Build versioned dataset creation, validation, deduplication, labeling, license and privacy review.
- [x] Define train/validation/test splits and contamination checks with explicit permission for using conversation or document data.
- [x] Verify reproducible dataset manifests, provenance, exclusion/removal procedures and held-out evaluation integrity.

Offline build/verify commands passed synthetic entry-point verification, 19 focused cases and the 1,273-test solution suite. Human rights/privacy review and semantic/cross-shard contamination review remain required for production corpora. Model training follows in 6.29. [Usage, evidence and limits](Project_GRID_Training_Data_Pipeline.md).

## 6.29 GRID-AI / GRID-Agent — Model fine-tuning

**CPU synthetic reference implemented — 2026-11-13.** Ownership belongs to GRID-AI development tooling. The user selected a small test model and synthetic data for acceptance.

- [x] Plan LoRA/QLoRA workflows, recorded configurations/seeds, checkpoints, adapters, export and runtime compatibility.
- [x] Select supported local training dependencies and hardware requirements, with separate development storage accounting for the CPU reference.
- [x] Demonstrate a repeatable permitted-data training run, adapter loading/export, baseline comparisons and rollback using the authorized synthetic reference.
- [ ] Complete selected production-model, QLoRA/GPU, agent-runtime import and portable-training acceptance before promoting those capabilities in the final Phase 7 gate.

Thirteen focused cases, 1,286 solution tests and real CPU LoRA training/repeat/resume/export/reload/rollback passed. [Workflow, evidence, dependency lock and remaining gates](Project_GRID_Model_Fine_Tuning.md).

## 6.30 GRID-AI / GRID-Agent — Continuous quality and release management

**Development release gates implemented — 2026-11-13. Production/device acceptance remains open before Phase 7.** Ownership belongs to all three layers.

- [ ] Run functional, adversarial, regression, performance and compatibility suites over versioned models/prompts/adapters.
- [x] Implement and test the release-evidence gate for functional, adversarial, regression, performance, compatibility, migration and rollback results.
- [x] Define promotion criteria, known-good release bundles, rollback and migration compatibility checks.
- [x] Record candidate-versus-baseline evidence and reject failed candidates while preserving saved GRID data.

Eighteen release regression cases, 1,304 solution tests and the executable's synthetic promotion/rejection/rollback verifier passed. Production candidate suites and the device matrix below remain unchecked. Live agent adoption belongs to 6.31. [Commands, thresholds, audit retention and verification](Project_GRID_Release_Management.md).

- [ ] Run all model-handling acceptance scenarios, including no-GPU Lite, 8 GB Standard eligibility, benchmark-gated 12 GB FLUX discovery, offline jobs, load/unload cycles, validated documents/code and injected failures.

## 6.31 GRID-AI / GRID-Agent integration and final Phase 7 gate

**Integration progress verified — 2026-11-13. Final Phase 7 gate remains open.**

[Implementation, current verification, real local coding evidence and remaining gates](Project_GRID_Integration_Acceptance.md).

- [x] Define solution references and stable interfaces for GRID-AI, GRID-Agent and existing GRID7736 services. Contract baseline verified 2026-11-13: explicit host references, ownership/adapter inventory and seven compiled-layer checks; 42 focused tests passed. Historical baseline: 1,297 passed and 14 terminal failures; the subsequent integration suite passed all 1,347 tests. Broader 6.31 acceptance remains open. [Contracts and verification](Project_GRID_Integration_Contracts.md).
- [x] Migrate existing agents, conversations, memory, permissions and task workflows with one authoritative owner per state field. Legacy type forwards share the Grid.Agent definitions; engine/persistence retain authoritative state. See integration acceptance.
- [x] Preserve IDs, grants, history, approvals and saved worlds with tested backup, schema migration and rollback. Local populated-world recovery acceptance passed 2026-11-14: corrupt/future primary, invalid backup, successful and interrupted metadata migration; live approvals require re-review. Historical-executable and second-device release acceptance remain open. [Evidence and rollback procedure](Project_GRID_Integration_Acceptance.md#saved-world-backup-migration-and-rollback-acceptance--2026-11-14)
- [x] Verify normal terminal, attached-window and meeting workflows through the migrated APIs. Existing workflow and ownership regression suites pass; broader conversational feature acceptance remains below.
- [ ] Resolve the reported local AI failure and complete 6.14–6.15 acceptance with failing-instance and second-device evidence. PC2 log received 2026-11-14: missing bundled assets; placeholder-port refusal was secondary. Four test setup failures require capability-aware symbolic-link fixtures. Fixes and exact retest steps: [PC2 diagnosis](Project_GRID_PC2_Diagnostics.md). Successful offline PC2 AI retest remains open.
- [ ] Complete all 15 platform components and all 12 production-readiness checks in the linked specification
- [ ] Record measurable release evidence and obtain user review before beginning Phase 7

[Detailed specification, model-selection status, contracts and acceptance](Project_GRID_AI_and_Agent_Platform_Plan.md).


### Conversation-first acceptance — clarified 2026-11-07

### Required local agentic coding workflow — confirmed 2026-11-13

GRID7736 must support an offline agent workflow that performs real software development on the user's local machine. The user gives MCP a task in ordinary language. MCP coordinates with MAXIS and other appropriate agents, obtains any required clarification or approval, and carries the authorized work through implementation and verification. This is a mandatory acceptance requirement before Phase 7.

- [x] Read and search an authorized local repository, understand relevant project instructions and preserve unrelated user changes. Verified for bounded single-file reviewed work on 2026-11-14; see local coding integration evidence.
- [x] Create, edit and delete local source files within the authorized task and workspace through the host tool gateway. Verified for bounded single-file reviewed work on 2026-11-14; see local coding integration evidence.
- [x] Run supported local build, test and diagnostic tools, capture their actual output, and use failures to revise the implementation and retry within the authorized scope. Verified for bounded single-file reviewed work on 2026-11-14; see local coding integration evidence.
- [ ] Complete the workflow through ordinary conversation with MCP, including delegation to MAXIS, progress, clarification, cancellation and reviewed approvals. The user must not need task IDs or terminal syntax to complete supported work.
- [x] Resume saved operator task plans through ordinary conversation, numbered selection, current-result review and fresh approval after restart; retain completed steps without duplicate execution. [Implementation and limits](Project_GRID_Integration_Acceptance.md#saved-plan-conversational-resumption--2026-11-14).
- [ ] Save useful task/conversation context and resume interrupted work with recorded file state and execution results.
- [x] Demonstrate a reviewed local coding cycle in a disposable C# project: real failing test, local-model source fix, approved apply, passing discovered test and saved receipts. See integration acceptance for evidence and harness limitations.
- [x] Demonstrate a real end-to-end coding task against a disposable local checkout, including an observed failing test, an agent-produced source change, a passing rerun and a final report identifying changed files and verified results. Verified for bounded single-file reviewed work on 2026-11-14; see local coding integration evidence.
- [ ] Run that acceptance workflow with network access unavailable and the required model, SDK, tools and dependencies provisioned locally. Record model limitations and any unsupported operation explicitly.

This requirement includes the agent actually modifying and testing code. Completion must be supported by filesystem changes and tool receipts. Current building blocks do not by themselves satisfy the end-to-end gate.

User-test follow-up (2026-11-14): the no-workspace demonstration and invalid main-terminal resume cases are repaired; 1,361 tests passed. [Verification and running-build limitation](Project_GRID_Integration_Acceptance.md#user-test-follow-up--2026-11-14).

[Local coding integration follow-up](Project_GRID_Local_Coding_Integration.md): 1,374 tests pass; real Git-repository diagnosis/repair passed. Saved file recovery and attached meeting plan execution are verified. Second-PC HTTP failure is **still open**: exact status/log and new-package retest required. Production model adoption remains open.

### Conversation-first behavior

Required before Phase 7. Connected agent chats and meetings interpret ordinary language for supported work. Users can request file operations, context retrieval, memory changes, task planning, delegation, progress, cancellation and results without terminal syntax or task IDs. The agent asks focused questions for missing information, explains the proposed work, obtains required approval in ordinary language and reports verified outcomes.

- [x] Remove terminal-command dispatch and reserved command-name collisions from connected chats and meetings. Sentences beginning with agent names or words such as `create`, `tasks` and `context` must reach the conversation interpreter.
- [x] Keep administrative and diagnostic commands available in the main terminal, with an explicit way to leave a conversation and return there.
- [ ] Resolve conversational references such as “approve that”, “cancel it” and “ask MAXIS to do it” against the current reviewed operation. Ask for clarification when multiple targets are possible. Bind approval to the exact operation, scope and content through the host gateway.
- [ ] Support named-agent and group requests in meetings, preserve shared context and private grants, and route work to the appropriate specialist with recorded results.
- [ ] Demonstrate end-to-end supported tasks entirely through ordinary language in inline chat, attached chat and meetings. Cover clarification, approval, revision, cancellation, failure and completion without requiring command syntax or IDs.

Connected user input now stays conversational; explicit command dispatch remains in the main terminal and trusted host adapters. Verified plan/file workflows and remaining acceptance are recorded in [6.31 integration acceptance](Project_GRID_Integration_Acceptance.md). Broader acceptance above remains open. Unsupported requests receive an explanation of available capabilities. Completion claims must come from execution receipts.

---

- [ ] Review model/profile benchmark evidence and the complete model-handling acceptance checklist alongside the existing production-readiness checks.

## Terminal-first local AI and device integration — planned 2026-11-13

**Priority and sequencing:** Complete the existing 6.31 prerequisites first. At the end of Phase 6, before starting Phase 7, focus on useful AI through the **existing GRID terminal**: natural-language tool use, local device capabilities, reliable model setup, skills, optional connectors, scheduled jobs, and transparent permissions. Reuse the existing host/agent/AI architecture. A new general-purpose GUI is optional future polish, **not a prerequisite for core assistant functionality, the terminal workflows, or core acceptance**. The new Phase 7 then extracts Grid.Platform and delivers verified Windows/Linux/Android portability, with Android first. Godot city and later integration work are deferred under Backlog and Future Plans. Do not silently introduce cloud runtime dependencies. Design inspiration: [OpenJarvis](https://github.com/open-jarvis/OpenJarvis), not a code dependency.

**Reuse, not replacement:** `Grid.AI` model/provider/hardware profile work (6.16–6.17A); `Grid.Agent` scoped memory/personas/delegation; 6.22–6.24 trusted authorization and tool gateway; 2.7.2 local app inventory; 6.3A screen assistance; 6.9 file operations; 6.20 retrieval; 6.27 and 8.6–8.9 offline/portable deployment. Existing partial implementations do not satisfy all new acceptance criteria.

## 6.32 Conversation-first intent and action routing

- [x] Add versioned, typed intent-to-capability planning for ordinary user language, clarification, multi-step execution and safe follow-up context.
- [x] Generalize current specific file/search/task utterances without giving the model arbitrary shell, filesystem or OS authority.
- [x] Preview the proposed action plan, affected resources, source provenance, agent assignment and required approvals before execution; preserve edit conflict and hash-bound review rules.
- [x] Expose what GRID can do, what needs a grant, what is unsupported and why a requested operation was denied.
- [x] Permit scoped cancellation, retries and durable resumption only for explicitly approved, idempotent operations; never silently replay stale approvals.
- [x] Test ambiguous requests, prompt injection, forged plans, revoked scopes, path escapes, partial execution and conversational recovery.

**Completion evidence — 2026-11-14:** Versioned bounded intent interpretation, human-reviewed routing, capability/grant explanations and explicitly reapproved read-only retries are implemented. Existing durable plan/file recovery and hash-bound execution remain authoritative. [Behavior, limits and verification](Project_GRID_Conversation_Intent_Routing.md). Focused verification: 118 tests passed. Full solution: **1,393 passed, zero failed/skipped** (14 Core, 157 Simulation, 99 Persistence, 1,123 Terminal), using isolated `TestResults/intent-routing` outputs.

## 6.33 Device integration and capability broker

**Local baseline implemented 2026-11-14.** Version-1 device requests, metadata discovery, exact-target review/approve/run, original-host grants and saved execution claims are implemented. Focused tests: 54 passed. Full solution: 1,416 passed. Interactive focus/protected-window and physical-media acceptance remains open. [Commands, policy, evidence and limitations](Project_GRID_Device_Integration.md).

- [x] Define an OS-facing, versioned capability catalog with typed inputs/results, policy labels, resource limits, provenance and audit records; reuse the trusted 6.23 gateway.
- [x] Distinguish read-only discovery (installed programs, processes, windows, approved paths and resource status) from potentially state-changing actions.
- [x] Implement narrowly scoped, consented application launch/focus and approved file/folder operations; add window inspection and screen context only with explicit selection and active grants. Window inspection is title-only; screen context retains the existing selected-monitor workflow.
- [x] Evaluate optional clipboard, notifications and other Windows integrations individually; default to no access and never infer permission from app discovery. Clipboard/new OS notification access is disabled; existing approved RELAY remains separate.
- [x] Recheck original-host, user identity, grant, target, path and request version at execution time; keep passwords, elevated operations and private contexts protected.
- [ ] Provide clear degradation on inaccessible apps, OS restrictions, changed devices, read-only media and unsupported operations; test each on real Windows hosts.

## 6.34 Guided onboarding and hardware-aware setup

Fresh-clone implementation follow-up: [installation workflow](Project_GRID_Fresh_Clone_Installation.md). 1,437 solution tests passed; offline setup and real local-model inline/attachment dispatch passed on the development PC.

- [x] Add an explicit clone installer with pinned runtime/SDK/model assets, offline-cache mode and no silent downloads.
- [x] Keep clone-local world/identity/history separate from existing-instance copy-to and shared-user default storage.
- [x] Require real-model conversation checks before publishing clone readiness; preserve worlds on setup failure.
- [x] Alert immediately and after login when local AI startup is unavailable; repeat the cause/fallback limitation on inline/resumed/meeting and attached conversation opening. Actual isolated terminal startup and attached-protocol warning verified 2026-11-15; full suite **1,436 passed, zero failed, four privilege skips**. [Readiness alert behavior and limits](Project_GRID_Fresh_Clone_Installation.md#ai-readiness-alerts-2026-11-15).
- [x] PC2 online clone installation verified 2026-11-15: pinned downloads/hash checks, restore, zero-warning Release build, and real Qwen replies through inline/attached protocol passed. Missing-asset guidance now names the clone installer; explicit `-Download -RepairRuntime` retains incomplete runtime files. [Evidence and repair behavior](Project_GRID_Fresh_Clone_Installation.md#pc2-online-installation-2026-11-15).
- Verification 2026-11-15: full solution **1,434 passed, zero failed, four symbolic-link privilege skips**; all seven clone setup cases passed. Skipped protection checks and physical/offline acceptance remain open.
- [ ] Verify the complete online installation and first credential setup on clean PC2, including physical windows and disconnected runtime use.

Initial setup/profile/health implementation and 1,431 passing regressions: [guided setup](Project_GRID_Guided_Setup.md). Remaining acceptance below is not marked complete; PC2 startup reporting follow-up is recorded in [PC2 diagnostics](Project_GRID_PC2_Diagnostics.md).

- [x] Offer a terminal-based first-run setup flow for permanent PC GRID versus Flash GRID, local identity/recovery, storage location and optional capabilities. Explicit save/launch continues to masked credentials; recovery guidance preserves the distinction between world restore and password recovery (no reset bypass).
- [x] Discover CPU, available RAM/VRAM, storage and existing managed runtime dependencies; show supported Lite/Standard/Performance choices with measured confidence and resource estimates. Lite eligibility is conservative; Standard/Performance remain explicitly unavailable pending device acceptance. Managed install/data/staging inventory and additional staging budget are reviewed before installation.
- [x] Select only verified compatible local models, provision missing assets through an explicit install action, and allow an offline pre-provisioned path; never silently download or use a cloud provider. Reviewed clone installer invokes offline or explicit download mode; Flash stays pre-provisioned. Running installed binaries must be closed before rebuilding their output directory.
- [x] Explain requested Windows permissions and connectivity separately, with skip, revoke and later-edit controls; default to minimum grants. Review maps to existing voice/screen/device/workspace controls; profiles never carry secrets or grants.
- [x] Provide one-command local AI, voice, storage, screen and capability health diagnostics with actionable repair messages and rollback. `--doctor-check <profile.json>` verifies temporary storage, voice inventory, synthetic OCR, metadata discovery and real local inference without opening worlds or capturing live audio/screens. Existing profiles/worlds retained; repairs remain explicit.
- [ ] Test clean-user first runs, low-end hardware, unavailable GPU, limited space, drive-letter changes, second PC and network-blocked installation/runtime paths.

## 6.34A Challenge-code enrollment and verbal authentication

**Hardening verified 2026-11-18:** Only `remote-call-identity` can issue an authentication claim; approval/recovery/pairing/contact-policy/voiceprint purposes are rejected. Endpoint reissue invalidates prior attempts and transient authority is bounded. Protected remote input remains open. Latest batch verification: **1,519 passed, zero failed/skipped**; see [challenge authentication](Project_GRID_Challenge_Code_Authentication.md).

**Local enrollment, authenticated management and trusted-host session protocol implemented — 2026-11-17; protected remote input remains planned.** Three user-created challenge prompt/response pairs follow username/password setup. Authenticated status, setup/rotate, aggregate masked self-test and confirmed disable are implemented. The host protocol requires a Master-Control-issued endpoint grant, selects two random distinct prompts, binds attempts/claims to user, host/world generation, endpoint, call and purpose, expires/rejects replay, and persists bounded increasing account/endpoint cooldowns in schema v26. Claims are authentication-only and cannot log in, approve work, recover a password or widen authority. Full verification: **1,452 passed**, zero failed/skipped. [Implementation evidence, storage boundary, protocol and remaining client acceptance](Project_GRID_Challenge_Code_Authentication.md).

- [x] Define a versioned three-challenge credential contract with distinct prompts, independently salted response verifiers, fixed-time comparison and atomic persistence/migration.
- [x] Extend fresh credential setup to require all three challenges after password confirmation and make authenticated `credentials` rotation replace the complete password/challenge set atomically.
- [x] Keep plaintext responses out of local setup output, history, audit and saved/backup identity state; store only prompts and salted verifiers. No response enters an inference or transcript path in the implemented local flow.
- [x] Add separate authenticated challenge status, disable and masked self-test controls for existing users.
- [x] Implement short-lived single-purpose challenge sessions bound to user, host/world generation, Master-Control-granted endpoint and contact/call ID, with randomized selection, expiry, replay defense and durable increasing rate limits.
- [ ] Require protected client secret entry or transcription-suppressed speech capture; provide masked typed fallback and warn about speakerphone, recording and nearby listeners.
- [ ] Make challenge success an authentication claim only—not password recovery, device pairing, task approval, contact-policy editing or proof of biometric voice identity.
- [ ] Verify setup cancellation, migration, normalization, secret leakage, wrong/replayed/cross-call responses, lockout recovery, agent forgery, rotation, backup, copy/transfer and accessibility fallbacks.
- [ ] Evaluate opt-in local voiceprint verification later as a separate factor with consent, revocation, replay/deepfake, false-match, noise/illness and accessibility evidence.

## 6.34B Development-only break-glass terminal commands

**Foundation, locked-local, registered-agent, original-host Master Control and offline owner-rescue boundaries implemented — 2026-11-18.** The original interactive console supports bounded logged-out preparation, agent self-controls and two-step Master Control emergency plans. A separate pre-start rescue mode requires an exclusive stopped-world lease plus a world-bound user-held key and exposes only verification, local password reset or authenticated backup restoration. It starts before AI/runtime setup and has no conversation, agent, remote, shell or general filesystem route. Existing visible `nge` and startup `--recover` remain authoritative; hidden nullification is not implemented. [Command inventory, implementation boundary, nullification contract, safeguards and acceptance matrix](Project_GRID_Development_Break_Glass_Commands.md).

- [x] Inventory visible recovery/destructive paths and define a separate versioned hidden-command registry that cannot weaken or alias their safeguards. `DevelopmentBreakGlassRegistry` v1 records visible protections and reviewed contracts but contains no executable handler.
- [x] Keep hidden names/counts out of normal discovery while documenting every command in development references, source inventory and tests; reject undocumented hidden commands at release review. The ordinary dispatcher refuses the reserved namespace, conversations cannot route it to AI, and focused help/completion/denial tests pass.

**Foundation verification — 2026-11-18:** Four focused registry/non-discovery tests and the complete `Grid.AI.slnx` suite passed at **1,457 passed, 0 failed, 0 skipped**.
- [x] Permit locked/local state only bounded diagnosis, world verification, freeze request and recovery-challenge preparation—never identity reset, private-content access or destruction by command knowledge alone. The original interactive locked console provides content-free status, hash-preserving read-only primary/backup verification, simulation/agent pause, a two-minute process-local nonce with an explicit zero-authority warning, and cancellation. Authenticated, attached, redirected, malformed and conversational routes fail closed.

**Locked-local completion verification — 2026-11-18:** Eleven focused 6.34B tests and the full solution pass: **1,464 passed, 0 failed, 0 skipped**. Primary and backup SHA-256 values remain unchanged across verification. Freeze leaves the session logged out. Recovery preparation expires without authenticating or changing credentials/world data.
- [x] Limit registered-agent break-glass controls to self-status, self-cancellation, self-detach and bounded self-evidence with no Master Control transition or cross-agent effect. Original-host agent leases are independently revalidated in the engine; evidence exposes only the active agent's role/ID and at most 32 task IDs/statuses/attempt counts; abort changes only tasks targeted to that agent; detach logs out to `grid[locked]`.

**Registered-agent completion verification — 2026-11-18:** Seventeen focused 6.34B tests and the full solution pass: **1,470 passed, 0 failed, 0 skipped**. A MAXIS self-abort leaves the JN task unchanged, saves cancellation state, and exposes no arguments/results/private context. Master Control, logged-out, attached, unattended, malformed and unknown routes fail closed with the same denial.
- [x] Add original-host Master Control freeze, isolate, session revoke, remote disable, safe-mode, recovery and evidence-export plans with exact target binding, expiry, audit and fresh confirmation. Every mutation is a single-use two-step plan bound to session generation, world/instance, activity generation and exact target. Recovery validates primary/backup hashes again after shutdown and skips final save; evidence is bounded JSON; the external operator audit excludes nonces, targets and private content.

**Master Control completion verification — 2026-11-18:** Twenty-seven focused 6.34B tests and the complete solution pass: **1,480 passed, 0 failed, 0 skipped**. Tests cover wrong/stale/replayed/session-invalidated plans, exact MAXIS/JN approval isolation, remote challenge revocation, attached-session shutdown, safe-mode command gating, new-file evidence export without secrets, recovery without final-save overwrite, changed-backup hash refusal and retained damaged-primary archives. No remote listener exists in this build; `disable-remote` establishes the host gate and revokes transient challenge endpoints/claims.
- [x] Design and implement an exclusive offline owner-rescue entry point with user-held recovery material and no AI, remote, conversational, shell or general filesystem capability; do not create a universal backdoor key. Authenticated Master Control can create a new 256-bit world-bound recovery file at an explicit non-reparse path; GRID persists only a salted verifier and key ID. `--operator-rescue` runs before runtime/AI setup, requires interactive local input and an exclusive stopped-world lease, verifies exact world/key authority, and permits only read-only verification, local password reset or same-authority backup restoration. Password reset clears challenge credentials/rate limits but preserves agents, transcripts and world data. State-changing choices require exact world ID plus a fresh 128-bit nonce; audit records are bounded and content-free. Losing the only current file means no offline bypass exists.

**Offline owner-rescue completion verification — 2026-11-18:** Seven focused tests and the complete solution pass: **1,487 passed, 0 failed, 0 skipped**. Tests cover plaintext/path exclusion from world state, overwrite refusal, key rotation, cross-world/wrong-key rejection, redirected-input denial, active-world exclusive-lock refusal, unenrolled-world denial, content-free audit, exact confirmation, password/challenge reset behavior, recovery-authority retention and matching-authority backup restore with displaced-primary archive. Recovery enrollment remains an explicit owner action rather than an automatic backdoor.
- [ ] Implement recoverable world retirement before any managed-instance removal; require manifest-bounded paths, quarantine, external receipt and refusal of roots, repositories, reparse points, shared assets and active writers.
  - [x] Implement bounded database/backup retirement through authenticated offline owner rescue: same-volume quarantine, exact hash inventory, external receipt, durable source tombstone and no recursive removal. Six disposable-world tests cover interruption, sidecars, changed targets, active leases and restart refusal. Broader managed-instance inventories/removal and physical power-loss acceptance remain open. [Retirement contract](Project_GRID_Development_Break_Glass_Commands.md#recoverable-database-retirement--2026-11-18).
- [x] Keep irrecoverable purge separate and deferred; document that ordinary deletion cannot guarantee erasure from SSDs, snapshots, backups or synchronized copies.
- [ ] Verify non-discovery, generic unauthorized denial, remote/agent/model refusal, replay/cross-world resistance, interrupted recovery/nullification and disposable-root deletion bounds.

## 6.35 Reusable skills and integration system

**Completed 2026-11-19:** [Declarative skills catalog](Project_GRID_Local_Skills.md) provides versioned, hash-reviewed JSON import/update, explicit enable/disable, typed dependency reporting, bounded configuration, exact-revision registered-agent grants, one-revision rollback, removal and durable content-free receipts. Untrusted manifests are interpreted through two fixed read-only actions; executable scripts/plugins, model instructions, arbitrary paths/arguments, processes and direct networking are rejected rather than given ambient authority. Thirteen focused tests and the complete solution pass at **1,523 passed, 0 failed, 0 skipped**. Physical PC2/removable-device testing remains a separate overall portability gate.

- [x] Define a versioned skill manifest and typed invocation contract compatible with existing GRID-Agent role scopes and host authorization. Version 1 remains Master-only; version 2 declares eligible registered-agent roles and exact dependencies. Each invocation binds skill ID, revision, actor, allowlisted action and output limit; no model output can add authority.
- [x] Implement a terminal-accessible local skills catalog for discovery, enable/disable, bounded per-skill output configuration, dependency checks and clear capability descriptions. Agents see only exact enabled revisions granted to their role, and paused agents cannot invoke them.
- [x] Support explicit import/install of local or user-selected community manifests with origin/version/source hash/canonical revision hash/license metadata and review of required permissions; never auto-trust publisher claims or third-party content. Two-minute session-bound review and exact-byte installation remain required.
- [x] Constrain untrusted skill execution through a denial-based typed interpreter: no executable content, prompt instructions, general filesystem, direct network, process, shell or arbitrary arguments; at most four fixed reads and bounded output. Arbitrary executable plugins are deliberately unsupported rather than falsely described as safely sandboxed.
- [x] Keep supported skills offline-operational after provisioning. Built-in world/resource dependencies report available; declared network dependency reports optional/unavailable and blocks enable/run instead of silently connecting or degrading.
- [x] Validate install/update/grant/revoke/disable/rollback/remove, incompatible versions, unknown fields/actions/dependencies, malicious instruction text, access denial, interrupted/failed attempts without replay, catalog tampering and logical device portability. Changed machine/path/world/credentials disable skills and clear grants; destination use requires explicit review/enable/grant. Physical device acceptance remains tracked separately.

**6.35 completion verification — 2026-11-19:** Thirteen focused skill cases and the full solution pass: **1,523 passed, 0 failed, 0 skipped** (Core 14, Simulation 163, Persistence 107, Terminal 1,239). No schema migration, cloud dependency, model call, background task or executable plugin was added.

## 6.36 App and service connections

**Completed offline-first foundation — 2026-11-19:** [Local app and service connections](Project_GRID_Local_Connections.md) adds a real bounded `local-task-board` adapter and a provider catalog that honestly marks calendar, email, external-task and browser-data services unavailable until optional reviewed adapters are installed. This completes the shared connection-management/security workflow without introducing a cloud dependency or pretending unconfigured accounts are connected. Six focused tests and the complete solution pass at **1,529 passed, 0 failed, 0 skipped**.

- [x] Provide discoverable `connections providers/list/show/status/plan/add/enable/disable/grant/revoke/remove/read/draft-add/approve/commit/history` commands. The supported local task adapter is functional; optional calendar/email/tasks/browser provider classes disclose unavailable status rather than silently connecting.
- [x] Define connector-specific scopes, source/account isolation, credential policy, consent, revocation and agent disclosure. The local adapter declares `tasks.read`, `tasks.draft`, `tasks.commit`, stores no credentials and grants agents read-only access by exact registered role. Future account providers must supply their own reviewed credential/refresh implementation before becoming available.
- [x] Prioritize local data and keep remote services opt-in. Local task boards operate entirely offline; unavailable service providers make no network call and are not mandatory GRID dependencies.
- [x] Keep reading, drafting, approving and committing separate. Task additions use a two-minute session-bound preview, explicit approval, stale-source hash recheck, retained before-image and atomic replace. Agents cannot draft or commit. Email sending, deletion and calendar scheduling remain unavailable rather than bypassing this boundary.
- [x] Surface transport, observation time, exact source, current hash, freshness/change/error status and item citations. Local reads reopen the source and never present a content cache as live.
- [x] Verify grant revocation, cross-agent privacy, paused/device-rebound authority removal, offline operation, non-destructive metadata removal, missing/corrupt/oversized sources, stale writes and retained recovery artifacts. Live cloud-account and physical second-device exercises remain provider/device acceptance, not evidence claimed by this local foundation.

**6.36 completion verification — 2026-11-19:** Six focused connection cases and the full suite pass: **1,529 passed, 0 failed, 0 skipped** (Core 14, Simulation 163, Persistence 107, Terminal 1,245). No schema migration, cloud SDK, token, background synchronization or model authority was added.

## 6.36A Optional remote terminal access over private networks

**Bounded gateway foundation implemented — 2026-11-19.** A process-local v1 gateway now supports reviewed loopback or observed local-Tailscale binding, one-time GRID pairing and strictly read-only status. It never changes Tailscale/firewall/Funnel/DNS settings and persists no endpoint/device credential. Five focused tests and the full suite pass at **1,534 passed, 0 failed, 0 skipped**. Remote conversations/actions and physical Tailscale/phone/wearable acceptance remain open. [Architecture, commands, implemented boundary and acceptance plan](Project_GRID_Remote_Terminal_Access.md).

- [x] Define a versioned bounded JSON-lines protocol attached to the authoritative running host. It exposes only typed help/status/disconnect—not the database, model server, named pipe, shell or ordinary terminal dispatcher.
- [x] Add reviewed private endpoint discovery/configuration with loopback default testing and local Tailscale-range validation. GRID never installs/signs into Tailscale, changes policy/firewall/DNS, binds all LAN interfaces or enables Funnel.
- [x] Require a random single-use two-minute GRID pairing code in addition to transport. Sessions are read-only, five-minute/process-local and cannot invoke original-host controls. Remote conversation is deliberately still unavailable.
- [x] Provide `remote status/options/plan/enable/disable/sessions/revoke/pairing/doctor/audit` with exact bind/port/capability preview, two-minute plan nonce, immediate revocation and bounded content-free audit.
- [x] Reject duplicate request IDs, unknown actions and reconnect reuse. No remote approval/action route exists yet, so conversation text cannot approve or replay work.
- [x] Disable listener, sessions and pairing before copy/transfer and at host shutdown. Startup is always disabled and revalidates the current endpoint through a new local plan; no endpoint/device credential is copied.
- [ ] Test second-device private access, non-tailnet/public refusal, Tailscale unavailable/disconnected/address changes, pairing abuse, rate limits, multiple clients, sleep/wake, restart and emergency disable.
- [ ] Run a low-bandwidth narrow-display client exercise representative of a phone/wearable relay; do not claim native Even Realities integration without a separately verified client.

**6.36A foundation verification — 2026-11-19:** Five focused gateway cases and the full suite pass: **1,534 passed, 0 failed, 0 skipped** (Core 14, Simulation 163, Persistence 107, Terminal 1,250). Physical second-device/Tailscale and narrow-display acceptance remain explicitly incomplete.

## 6.36B Remote contact, ping and voice calls

**Foreground text-contact foundation implemented — 2026-11-19.** User-owned policies and fresh locally approved pings now travel through the authenticated 6.36A gateway to a matching foreground client, with durable idempotent states and no replay after restart. Audio, background mobile wake-up and physical phone/wearable acceptance remain open. Five focused cases and the full suite pass at **1,539 passed, 0 failed, 0 skipped**. [Protocol, implemented boundary and remaining acceptance](Project_GRID_Remote_Contact_and_Voice_Calls.md).

- [x] Implement a manual foreground text self-test ping through an authenticated paired client. The host prepares and locally approves it; only the policy's stable device label may poll/accept/end it. Physical phone/Tailscale delivery remains an acceptance item below.
- [x] Define user-owned policies with exact triggers, allowed agent roles, stable endpoint label, UTC quiet hours, daily limit, expiration, disabled-by-default lifecycle and immediate pause/disable/remove cancellation. Urgency/retry automation is not inferred or enabled.
- [x] Enforce a capability-backed authority foundation stricter than the planned classes: MCP, RELAY and every other eligible agent remain constrained by policy and every attempt requires fresh exact human approval. No agent receives standing autonomous contact authority before 6.37/6.38.
- [x] Add durable idempotent proposed/authorized/delivering/ringing/accepted/connected/ended plus decline/cancel/interrupted states and bounded receipts. Deduplication returns the original attempt; restart interrupts active attempts and never replays them.
- [ ] Evaluate WebRTC versus a bounded tailnet audio stream; measure latency, encryption, codec, reconnect, firewall, licensing and resource behavior before selecting a provider.
- [ ] Keep microphone access answer-bound and recording off by default; use local STT/agent inference/TTS with visible text fallback and no spoken command execution.
- [x] Select and disclose the current strategy as foreground polling only. Background wake-up is unavailable; Tailscale alone is not claimed to provide it, and no APNs/FCM/cloud dependency was introduced.
- [x] Preserve original-host-only policy/approval administration, stable device labels rather than IP identity, minimal text reasons and transfer pause/re-pairing. No private keys, microphone data or raw audio are stored.
- [ ] Verify policy escape, false urgency, duplicate signaling, quiet hours, rate limits, device loss, network loss, background suspension, audio failure and phone/wearable relay limitations.

**6.36B foundation verification — 2026-11-19:** Five focused cases and the complete suite pass: **1,539 passed, 0 failed, 0 skipped** (Core 14, Simulation 163, Persistence 107, Terminal 1,255). Physical/private-network, background mobile, audio/WebRTC/STT/TTS and wearable tests remain explicitly incomplete.

## 6.37 Scheduled and persistent assistant workflows

- [x] Add human-configured one-time, recurring and conditional jobs using a durable local scheduler; persist ownership, scope, schedule, last run, results and cancellation state.
- [x] Define explicit enable/disable, quiet hours, notifications and expiration; never start proactive monitoring based only on a conversation suggestion.
- [x] Revalidate ownership, world binding, enabled/cancelled state, condition, quiet hours and deadline before each run. The implemented action catalog is read-only/local-notification only; consequential actions remain unavailable and therefore cannot bypass their contemporaneous approval paths.
- [x] Provide a terminal inbox of completed jobs and exceptions, manual retry for failed/interrupted work and bounded traceable receipts. Pending approvals and agent activity remain in their existing task/activity views because this scheduler does not execute agents or create approvals.
- [x] Distinguish the 30-second host service from simulated city ticks; cap each pass at eight due jobs, schedule late recurring work forward once, persist a pre-execution marker and prohibit automatic replay, catch-up loops, AI/model work and agent spending.
- [x] Test durable restart/interruption recovery, no replay, late wake, conditions, quiet hours, expiration, cancellation, explicit retry and offline-only execution. The interruption fixture models abrupt loss after the durable start marker; a physical power-cut test remains release/second-device acceptance rather than an automated-unit claim.

**6.37 completion notes — 2026-11-19:** Added the durable `ScheduledWorkflowService`, original-host Master Control `schedule` command, atomic bounded state/inbox/receipts, explicit disabled-first lifecycle and terminal-host background loop. The existing `jobs` command remains the civic-workstation listing. Supported scheduled actions are `world.status`, `resources.status` and local `notify`; no model, agent, file, process, network or automatic approval authority was added. See [scheduled workflow behavior and commands](Project_GRID_Scheduled_Workflows.md).

**6.37 verification — 2026-11-19:** Seven focused scheduler cases pass. The complete solution passes **1,546 tests, 0 failed, 0 skipped** (Core 14, Simulation 163, Persistence 107, Terminal 1,262). The initial full run exposed a command-name collision with the existing civic `jobs` view; the scheduler was renamed to `schedule`, preserving both features, and the complete suite then passed.

## 6.38 Trust, personalization and usability acceptance

- [ ] Provide one human-readable terminal activity view (optional future GUI dashboard) for active agents, chosen models, tool calls, resource use, task progress, approvals, cancellations and durable audit history.
- [ ] Provide personal preference/persona settings and scoped memory review, correction, expiry, forget and export controls; preserve existing cross-agent isolation.
- [ ] Introduce permission profiles for common workflows with individual grant review and immediate revocation; do not implement unrestricted global auto-approval.
- [ ] Offer optional, user-enabled contextual suggestions tied to explicit local goals, without covert background surveillance or compulsive notifications.
- [ ] Evaluate first-task completion, setup friction, time to actionable response, error recovery, accessibility and discoverability with representative users of the terminal-first experience.
- [ ] Run Windows terminal interaction, multi-device portability, memory/scope isolation, security and offline acceptance tests across supported hardware profiles.
- [ ] Update the user guide, onboarding, permission explanations, safe uninstall/export and troubleshooting documentation before claiming release readiness.

**Experience release gate:** A new user on a supported Windows PC can install or launch a prepared GRID edition, set up a local model and grants, request a realistic multi-step file or application task conversationally, review/approve it, observe grounded agent progress, recover from a failure, and revoke access — using the GUI, with no required cloud service. Repeat essential workflows from a verified offline Flash GRID package on a second supported machine. Preserve simulation integrity, capability scoping and auditable Master Control authority throughout.

---

## 6.39 Optional unified desktop GUI — moved to Future Plan C

The GUI is no longer sequenced before platform compatibility. Its requirements are preserved under Future Plan C and may begin only after the active Grid.Platform phase is reviewed and the user explicitly reprioritizes it. Terminal completion remains sufficient for core AI acceptance.

**Core terminal experience acceptance (6.32–6.38; required before the cross-platform phase):** On a supported Windows PC, an authenticated user can configure local AI and grants, ask MCP for a multi-step device/file task in ordinary conversation, inspect a typed plan, approve consequential actions, view verified results, recover or cancel safely, and revoke access — **entirely through the existing GRID terminal** with no required cloud service. Test a prepared portable offline package on a second supported computer. Future Plan C must not block this gate.

---

# Phase 7 — Cross-platform compatibility and Android portability

**New active next phase — planned 2026-11-17. No implementation is claimed by this planning update.** Introduce a typed `Grid.Platform` subsystem that isolates operating-system behavior from Grid.Core, Grid.Simulation, Grid.Persistence, Grid.AI, Grid.Agent and the terminal experience. Preserve separate native adapters rather than emulating Windows. Android ARM64 is the first priority, followed by Linux x64/ARM64 while retaining Windows behavior. [Architecture, USB layout, Android constraints and acceptance](Project_GRID_Cross_Platform_and_Android_Plan.md).

## 7.1 Platform boundary and dependency inventory

- [ ] Inventory direct Windows, registry, WMI/CIM, PowerShell, named-pipe, WinForms/WPF, System.Speech, process, path and drive assumptions across every project
- [ ] Define which behavior is shared, platform-adapted, platform-optional or unsupported
- [ ] Add architecture tests preventing new native dependencies in Grid.Core, Grid.Simulation and Grid.Persistence
- [ ] Preserve observable Windows behavior while moving native calls behind adapters
- [ ] Record the supported OS/version/architecture matrix; do not equate “all devices” with untested universal compatibility

## 7.2 Grid.Platform contracts and capability model

- [ ] Create `Grid.Platform` abstractions for detection, file systems, runtimes, processes, removable storage, resources, dependency checks and lifecycle
- [ ] Represent `Available`, `PermissionRequired`, `TemporarilyUnavailable`, `Unsupported` and `Unknown` without simulated success
- [ ] Provide deterministic fake adapters and contract suites for paths, permissions, failures, cancellation and capability changes
- [ ] Keep task approval, identity and audit enforcement above platform adapters; an adapter cannot widen authority
- [ ] Use typed operations instead of arbitrary shell-command concatenation

## 7.3 Windows adapter extraction

- [ ] Create `Grid.Platform.Windows` and migrate current Windows discovery, storage, process, runtime, telemetry, picker and lifecycle integrations
- [ ] Retain current local credentials, world loading, terminal, AI, file/context, transfer and recovery behavior
- [ ] Preserve explicit Windows-only features as capability results when no portable equivalent exists
- [ ] Pass the existing full suite and real Windows smoke tests after extraction

## 7.4 Linux adapters and host

- [ ] Create `Grid.Platform.Linux` for x64 and ARM64 OS/architecture detection, XDG paths, permissions, processes, resources and removable storage
- [ ] Replace PowerShell assumptions with direct process APIs or bounded Bash adapter operations where required
- [ ] Define Linux runtime/self-contained publishing, app dependencies and offline launch scripts
- [ ] Verify case-sensitive paths, mount changes, executable permissions, signals, suspend/resume and headless terminal behavior
- [ ] Test disposable-world login, save/restart, context/file tasks and supported local inference on real Linux hosts

## 7.5 Android ARM64 host and storage adapter

- [ ] Create `Grid.Platform.Android` plus a dedicated `Grid.Host.Android`; do not treat Android as `linux-arm64`
- [ ] Use the Android system folder/document picker and persisted user grants for removable Grid.AI data instead of unrestricted `/storage` or Termux access
- [ ] Represent Android content/document handles without fabricating ordinary filesystem paths
- [ ] Stage verified executable/runtime assets in app-private storage when Android forbids executing from USB, while retaining approved persistent data on removable storage
- [ ] Add touch-friendly login, terminal/chat, approval, cancellation, file selection and diagnostics without duplicating world state
- [ ] Handle foreground/background, suspension, process death, grant revocation, USB removal and app restart without silent world replacement
- [ ] Request folder, microphone, notifications, network and screen/media permissions separately and only when the user selects the feature

## 7.6 Portable world and USB contract

- [ ] Publish separate `windows-x64`, `linux-x64`, `linux-arm64` and `android-arm64` artifacts with one versioned manifest/data contract
- [ ] Share governed worlds, memory and configuration while keeping platform executables and native dependencies separate
- [ ] Permit only one authoritative writer per world; add lease/lock, schema, backup, atomic-write and post-load identity checks before cross-platform writes
- [ ] Reuse the saved world/city instead of regenerating after device change
- [ ] Revoke or review device-bound paths, workspaces, credentials, remote pairings and permissions on a new device
- [ ] Test read-only media, missing drive, changed mount/URI, low space, interrupted staging and incompatible package/schema versions

## 7.7 Runtime, command and dependency adaptation

- [ ] Detect or stage the correct self-contained .NET/application host for each supported target
- [ ] Provide platform-specific launchers without making host PATH, environment variables or registry changes mandatory in portable mode
- [ ] Map supported typed commands to direct APIs, PowerShell, Bash or Android host services and report unavailable operations honestly
- [ ] Provide platform-specific health/repair guidance for runtime, storage, permissions, model and dependency failures
- [ ] Keep ordinary runtime offline; installations/provisioning are the only permitted network-dependent stage

## 7.8 AI and resource portability

- [ ] Keep inference selection behind Grid.AI provider contracts; Ollama is not a universal platform requirement
- [ ] Evaluate compatible offline Windows, Linux and Android backends, model formats, licenses and CPU/GPU/NPU support
- [ ] Select the smallest measured profile that passes the task on the current device
- [ ] Provide a phone-safe degraded mode for simulation, terminal, memory and deterministic tools when generative AI cannot run
- [ ] Account for RAM, storage/staging, thermal throttling, battery and concurrency before model loading
- [ ] Never silently download models or claim unavailable AI work succeeded

## 7.9 Cross-platform security, lifecycle and diagnostics

- [ ] Preserve Master Control, capability grants, reviewed actions and audit semantics across adapters
- [ ] Save/recover safely across shutdown, signals, suspend, Android backgrounding/process death and removable-media loss
- [ ] Keep permissions scoped and revocable; reopening a world does not imply OS permission on the new device
- [ ] Provide one capability/health report showing platform, architecture, grants, limitations, runtime, storage and AI readiness
- [ ] Verify that unsupported security, process, screen, voice or file features fail closed with useful alternatives

## 7.10 Real-device release gate

- [ ] Run the shared contract and full applicable regression suites on Windows x64, Linux x64, Linux ARM64 and at least one supported Android ARM64 device
- [ ] On Android, install the host normally, select Grid.AI on USB/removable storage, save/restart/regrant, detach/reconnect and recover from revoked access
- [ ] Move one disposable world between supported platforms without regeneration, concurrent writes or loss of portable identity/history
- [ ] Verify essential offline workflows with network/DNS blocked and record which AI/profile is actually available on each device
- [ ] Publish measured OS/API/ABI/hardware/backend support and known limitations before claiming a target supported
- [ ] Obtain user review of the device matrix and evidence before advancing a future backlog item

---

# Backlog and Future Plans

**Roadmap rebased 2026-11-17.** Everything below was formerly Phase 7 or later. It is preserved for future work but is no longer the active next phase, does not block planning/implementation of Grid.Platform, and must not be reported as implemented unless its own evidence exists. Completed historical checkboxes remain evidence of earlier work only.

## Future Plan A — The City (formerly Phase 7)

**Deferred backlog.** Godot city work begins only after the active cross-platform phase is reviewed and the user explicitly reprioritizes it. It must render authoritative saved state rather than becoming a competing simulation.

### A.1 Godot foundation

- [ ] Render the persisted GRID-REQ-004 city layout without regenerating or taking ownership of authoritative world state
- [ ] Create Godot .NET viewer project
- [ ] Establish local communication with C# engine
- [ ] Render existing world state instead of simulating separately
- [ ] Build camera movement and observation controls
- [ ] Implement entity positions and selection
- [ ] Keep simulation running when viewer is closed

### A.2 First visual vertical slice

- [ ] Model one realistic European stone street
- [ ] Add human-scale stone/brick facades and storefronts
- [ ] Use restrained digital lines in paving and entrances
- [ ] Implement fixed early-autumn evening sky
- [ ] Implement medium/heavy gray-blue clouds
- [ ] Create damp surfaces and believable puddles
- [ ] Add warm street and window lighting
- [ ] Add autumn trees, gentle wind and light haze

### A.3 Citizen and agent appearance

- [ ] Build shared humanoid character rig
- [ ] Model light-gray civilian jacket and dark trousers
- [ ] Model structured gray officer tunic and standing collar
- [ ] Add black belt and tall black boots for officers
- [ ] Implement subtle luminous garment seams
- [ ] Create job-specific civilian variants
- [ ] Create named system-agent uniform variations
- [ ] Add walking, idle, working and talking animations

### A.4 Districts and architecture

- [ ] Build five navigable districts
- [ ] Add credible roads, courtyards and public spaces
- [ ] Create Core landmark/reactor
- [ ] Create Processing Sector workshops and facilities
- [ ] Create Memory Banks and Archive institutional buildings
- [ ] Create Gateway communications/transit infrastructure
- [ ] Differentiate administrative and security architectural zones as world expands
- [ ] Use restrained original signage and insignia

### A.5 World interaction

- [ ] Render the reconciled application population plus persistent residents from live simulation state; use twenty-resident fixtures only for tests
- [ ] Animate movement and program activity
- [ ] Click citizens to inspect actual status and history
- [ ] Display conversations with agents and residents
- [ ] Reflect real simulation events in lighting and activity
- [ ] Show resources and district conditions when requested
- [ ] Avoid invented on-screen events

### A.6 Visual quality and performance

**Edition requirement - planned, 2026-10-20:** [PC GRID versus Flash GRID](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) is selected during installation; visual fidelity must not alter world behavior.

- [ ] Build a very simple Flash GRID 3D profile with modest geometry/textures, limited effects/shadows and conservative view distance
- [ ] Build advanced PC GRID rendering options with hardware-scaled quality and simple-renderer fallback
- [ ] Profile both editions and verify equivalent world state/interaction across visual profiles

- [ ] Build reusable assets and materials
- [ ] Use LOD and instancing for repeated geometry
- [ ] Set adjustable graphics quality levels
- [ ] Profile lighting, reflections and fog costs
- [ ] Verify readable details at dusk
- [ ] Ensure the permanent evening look remains consistent
- [ ] Test viewer close/reopen without losing simulation state

---

## Future Plan B — Integration and release hardening (formerly Phase 8)

### B.1 Windows lifecycle

- [x] Integrate GRID-REQ-004 first creation versus saved-world loading; hardware changes and failed loads do not silently regenerate the city (2.2/2.6)
- [ ] Run GRID headless at Windows sign-in if enabled
- [ ] Allow independent startup and shutdown of viewer
- [ ] Handle Windows sleep and wake
- [ ] Handle clean shutdown and unexpected termination
- [x] Resume saved state without offline simulation catch-up (2.2/3.1; simulation advances only while running)
- [ ] Coordinate local service startup order
- [ ] Integrate optional remote-terminal gateway startup/shutdown, sleep/wake, network-change recovery and explicit remote-disable behavior without making Tailscale or any network service mandatory ([6.36A plan](Project_GRID_Remote_Terminal_Access.md))

### B.2 Resource and storage governance

- [ ] Enforce host resource consumption limits
- [ ] Throttle simulation, AI and rendering appropriately
- [ ] Enforce GRID logical storage quota
- [ ] Track model caches and external asset storage
- [ ] Trigger quota warnings and JN proposals
- [ ] Approve quota increases through MCP → User
- [ ] Prevent GRID from exhausting host disk free space

### B.3 Safety and reliability

- [x] Implement recoverable world backups and explicit restore workflow (2.2; subsequent context/conversation/voice state covered through 4.5)
- [ ] Audit all file-editing and host-task capabilities
- [ ] Apply least-privilege execution
- [ ] Test full-disk and invalid-snapshot scenarios
- [ ] Test unavailable model and audio services
- [ ] Test task retry, cancellation and idempotency
- [ ] Test agent restart and communication reconnection

### B.4 User experience and diagnostics

- [ ] Create optional WPF control/dashboard interface
- [ ] Show programs, tasks, health and permissions
- [ ] Implement configurable notifications and quiet hours
- [ ] Make meaningful status and error messages accessible
- [ ] Provide task history and change-review interface
- [ ] Document setup, recovery, backup and uninstall procedures

### B.5 Release validation

- [ ] Run extended headless simulation
- [ ] Perform end-to-end assistant workflows
- [ ] Run security and unauthorized-access tests
- [ ] Profile memory, CPU, storage and rendering use
- [ ] Validate backup/restart consistency
- [ ] Verify every supported runtime task offline; only explicit installs/provisioning may use the internet (GRID-REQ-008)
- [ ] Package a reproducible Windows release
- [ ] Write final setup and user guide

---

### B.6 Installation editions and self-contained portable package (GRID-REQ-007)

**Planned - 2026-10-20.** One flash-drive folder should carry the complete prepared GRID installation and its state. [Shared contract](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md).

- [ ] Offer an explicit installation choice: permanent PC GRID or mobile Flash GRID; explain rendering/storage differences
- [ ] Validate the selected permanent/removable destination and persist the edition without drive-letter guessing
- [ ] Package very simple lightweight 3D for Flash GRID and optional advanced rendering/assets for PC GRID
- [ ] Keep both editions on compatible authoritative simulation/save/identity contracts and require offline tasks in each
- [ ] Provide explicit authenticated world/credential export/import between editions without silent regeneration
- [ ] Define supported Windows/CPU targets and ship required runtime, dependencies, models, voice/assets and version/license manifests
- [ ] Keep GRID-managed app/data/config/docs/logs/backups/cache/temp within the portable root
- [ ] Resolve paths after drive-letter/path changes; avoid required host PATH, environment-variable or registry writes in portable mode
- [ ] Provide portable local credentials and authenticated migration from existing SID-bound worlds
- [ ] Preserve saved city/world/conversations on another PC while rediscovering host apps/resources; never regenerate silently
- [ ] Handle missing hardware/dependencies, read-only media, limited space, interrupted writes and removal/recovery
- [ ] Provide flushed shutdown/eject-ready flow and explicit backup/export behavior
- [ ] Verify two-PC relocation, spaced/Unicode paths, and no unintended host-local managed files

- [ ] Integrate and verify 4.2D transfers with the implemented 4.2F `grid -transferto "location"` / `grid -copyto "location"` names for complete managed-file relocation/duplication across PC GRID and Flash GRID layouts

#### B.6A Fresh-clone and bundled-runtime foundation — completed 2026-10-23

- [x] Track reproducible runtime/model/SDK/cache preparation scripts and clearly distinguish a source clone from a prepared release
- [x] Build a self-contained Windows x64 apphost and relative portable runtime/data layout without requiring installed .NET/Ollama or a user model cache for AI/chat
- [x] Automatically start/stop the host-owned AI child, report missing assets, and avoid personal Ollama services
- [x] Resolve MAXIS SDK/cache paths inside the prepared runtime; retain explicit workspace/dependency requirements
- [x] Repair fallback/inline/attached typewriter rendering and explicit on/off behavior
- [x] Add non-destructive runtime verification and test with restricted global paths and a fresh process profile

**Verification:** 648 automated tests passed; private runtime and self-contained package both generated real MCP replies. Package verification used restricted PATH, fresh profile and invalid global runtime/cache paths. Schema v20 unchanged. Two-PC/clean-VM, voice/driver/media and full installer acceptance remain open under active Phase 7 and future plans B.6–B.7. [Implementation and limits](Project_GRID_Fresh_Clone_and_Portable_Runtime.md).

### B.7 Offline runtime acceptance (GRID-REQ-008)

**Planned - 2026-10-20.** Every GRID task must be offline-capable; only explicit installations/provisioning may access the internet.

- [ ] Separate installation network access from runtime; forbid silent downloads and cloud fallbacks
- [ ] Provision local models, voice assets, tools, runtime libraries, documentation and offline authentication/recovery
- [ ] Replace/defer network-dependent task plans; require local equivalents for supported runtime tasks
- [ ] Test cold startup, login, simulation, conversations, local AI/voice/tasks and help with external network/DNS unavailable
- [ ] Test save/restart/recovery and the prepared flash-drive package on another supported PC while offline
- [ ] Record unavailable features honestly; do not substitute simulated success or stale facts for real offline execution
- [ ] Make portable/offline acceptance a release gate before claiming plug-and-play completion

### B.8 Startup full-test gate — pending requirement restored 2026-10-23

- [ ] Run and visibly display all required tests before normal owner startup; finish remaining suites after failures
- [ ] Clear the screen and show normal startup only after all required tests pass
- [ ] Otherwise display errors and `GRID was not started.`, then `Press Enter to close.`; missing/crashed/skipped required tests cannot count as passing
- [ ] Bundle offline fixtures/runner, isolate user saves, prevent recursive probe launches and let attached terminals reuse the tested host

### B.9 Portable device-change recognition — pending requirement restored 2026-10-23

- [ ] Alert when the host device changes, discover its hardware, and ask `Save as another device` or `Use one time`
- [ ] Persist an additional profile for save; keep one-time profile session-only; preserve existing profiles and saved city/world
- [ ] Revalidate machine-bound paths/credentials/scopes without carrying task approvals or granting OS privileges; verify restart, repeat visit, partial discovery and offline behavior

## Future Plan C — Optional unified GUI

**Deferred until after Grid.Platform.** This was formerly section 6.39. It is optional, non-blocking and must use the same authoritative services and capability contracts as terminal and Android clients.

- [ ] Define an optional user-facing shell for conversations, agent selection/meetings, task approvals, local files, activity, settings and future city access; preserve the advanced terminal as a first-class option
- [ ] Build on Grid.Platform rather than introducing a new OS-specific authority or duplicate state owner
- [ ] Integrate the future City backlog as an optional navigable representation of actual agents, applications and verified activity, not a separate synthetic source of truth
- [ ] Provide persistent conversation/task navigation, search, readable source boundaries, progress/cancellation indicators and interrupted-session recovery
- [ ] Support accessible keyboard navigation, screen readers, reduced motion, high DPI and responsive/resizable layouts on each claimed platform
- [ ] Distinguish inferred AI suggestions, verified host observations, pending proposals, approved actions and completed results in every view
- [ ] Verify GUI, terminal and Android host show the same underlying state and enforce the same permissions

# Release Gates

| Release | Required result |
|---|---|
| **GRID Genesis** (`v0.1.0-genesis`) | Five districts, twenty citizens, fixed-step clock, real terminal commands, passing tests |
| **GRID Foundation** | Persistent civilization, autonomous activity, persistent communications |
| **GRID Operator** | MCP delegates authorized PC tasks to specialist agents and reports verified results |
| **GRID Assistant** *(proposed additional release gate)* | Grounded advice, supplied context, approved editing and task assistance |
| **GRID Platform** *(next active release gate)* | Grid.Platform preserves shared behavior across verified Windows, Linux and Android adapters; Android can select governed removable data through its native host and safely reuse a saved world |
| **GRID City** *(future backlog)* | Interactive Godot 3D viewer displaying actual authoritative simulation state |
| **GRID Terminal Assistant** | Existing terminal supports offline, approved natural-language device actions, guided setup, skills, optional connectors, scheduled workflows and auditable permissions (6.32–6.38), required before cross-platform extraction; GUI (6.39) is optional and non-blocking |

# Completed Genesis Steps

1. [x] Implement `Grid.Core/GridWorld.cs`.
2. [x] Implement `Grid.Core/GridWorldFactory.cs`.
3. [x] Initialize five districts and twenty citizens through the factory.
4. [x] Enforce valid memberships and coherent district transfers.
5. [x] Replace temporary terminal district demo with a genuine world-state interface.
6. [x] Implement `GridSimulationEngine` with fixed-step progression using the completed `GridClock`.
7. [x] Add command-loop terminal features and terminal tests; all 69 tests are passing.
8. [x] Build and run tests, then commit and tag Genesis when all acceptance criteria pass.

# Post-Genesis Recommendations — Completed through 4.5

1. [x] Complete editable-field and session-context planning in sections 1.9.1–1.9.2.
2. [x] Implement and verify the validated editing API in section 1.9.3.
3. [x] Implement the terminal live view in section 1.10; session visibility follows login integration.
4. [x] Complete Phase 2.1 persistence architecture with identity, edit-result and audit extension requirements documented. Section 2.2 save/recovery is also complete; 2.3 history/observability is now complete; 2.4 resources/host state and the 2.7 retrofit dependencies are complete for their documented scope.
5. [x] Integrate login and Master Control terminal editing (4.2A), portable credentials (4.2B), and shared terminals (4.2C); preserve the historical Genesis release.
6. [x] Record completion notes and verification evidence for all completed modules through 4.5. Continue this documentation practice for future modules.

# Current Next Steps

**Roadmap update — 2026-11-17:** Complete the remaining active Phase 6 acceptance, then begin Phase 7 Grid.Platform with Android ARM64 as the first priority and Linux following the shared contracts. The former City and Integration phases are in Backlog and Future Plans. Do not begin them without explicit reprioritization.

1. [x] Complete 5.1: named system-agent/task contracts, durable task queue and capability-scoped demonstration broker.
2. [ ] Begin 5.2: MCP task coordination and delegation.
3. [ ] Perform physical voice capture/playback/headset-disconnection acceptance; automated recognition fixtures passed in 4.4/4.5.
4. [ ] Continue tracking the open extension items under 2.4, 2.7.2 and 4.2D; reconcile relevant portable/offline requirements from future plans B.6–B.7 into the active Grid.Platform contracts where necessary.

**Note:** This checklist preserves completed history plus the active Phase 7 cross-platform roadmap and deferred future plans. Completion indicators should be updated after each repository review; generated code and documentation are not evidence that a test has passed.
