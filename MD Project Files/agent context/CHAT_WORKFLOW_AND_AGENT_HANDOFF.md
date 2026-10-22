# Project GRID - Chat workflow and agent handoff

## Transfer command rename — 4.2F completed (2026-10-22)

Use `grid -copyto "location"` or `grid -transferto "location"` inside the GRID terminal. `copexist` and `tranexist` now only show migration guidance and never execute a transfer. Existing Master Control/original-host restrictions, preview, confirmation, final save and verified move/copy safeguards remain. **582 tests passed** (14 Core, 157 Simulation, 97 Persistence, 314 Terminal). No schema change or user-world relocation. See [transfer behavior and limits](../Project_GRID_Transfer_Implementation.md). Earlier command names and counts below describe historical milestones.


## Conversation windows — 4.2E delivered (2026-10-21)

Default `connect` and `resume` now open dedicated attached conversation windows. Agent replies animate with a skippable typewriter effect; `typewriter off` and `GRID_TYPEWRITER=off` provide instant text. Exit detaches without ending the saved conversation; parent revocation closes access. Explicit `connect MCP -inline` / `resume ID -inline` retains the original-host voice workflow and instant text. **572 tests passed** (14 Core, 157 Simulation, 97 Persistence, 304 Terminal). Schema remains v19; no AI/meeting implementation is implied. Multi-agent integration remains pending in 6.2A. See [implementation and acceptance test](../Project_GRID_Conversation_Windows_Implementation.md). Earlier milestone statements below remain historical.


## End-to-end task verification — 5.7 completed (2026-10-21)

MCP delegation, human approval, real MAXIS unit-test execution and RELAY recorded-result delivery are verified together, including actual passing/failing TRX evidence and SQLite reload. Nine new cases also cover all six agent roles, cancellation, durable-start recovery and duplicate dispatch prevention. **560 tests passed** (14 Core, 157 Simulation, 97 Persistence, 292 Terminal), zero failed/skipped. No runtime/schema change was needed; schema remains v19. See [verification matrix, boundaries and terminal test](../Project_GRID_End_to_End_Task_Verification.md). Next: **6.1 Shared local AI**. Earlier milestone counts and next-step statements below are historical.


## VOLT and RELAY — 5.6 completed (2026-10-21)

VOLT now reports existing host telemetry and supports approved normal/eco/auto pacing of GRID's automatic simulation. Unknown/unsupported GPU data is preserved; Windows resources and manual tick behavior are unchanged. RELAY provides a saved local inbox, recorded-task briefings, human acknowledgements and explicitly approved offline speech with delivery/failure receipts. Schema v19 preserves mode and inbox history without automatic playback/replay. **551 tests passed** (14 Core, 157 Simulation, 97 Persistence, 283 Terminal). See [VOLT/RELAY implementation and acceptance test](../Project_GRID_VOLT_RELAY_Implementation.md) for commands and boundaries. Next: **5.7 end-to-end verification**. Earlier milestone/schema/test statements below remain historical.


## JN storage — 5.5 completed (2026-10-21)

JN now inventories approved paths, reports possible duplicates, and prepares cleanup proposals without deleting source files. Defined GRID-managed storage is measured against an initial 20 GB decimal logical budget; MCP quota escalations require separate Master Control approval to change it. This is tracking/governance, not a hard write cap or disk reservation. Schema v18 preserves scope and quota history. **535 tests passed** (14 Core, 157 Simulation, 97 Persistence, 267 Terminal). See [JN implementation and acceptance test](../Project_GRID_JN_Storage_Implementation.md) for limits, accounting roots, report retention and remaining planned automation. Next: **5.6 VOLT and RELAY**. Earlier milestone/schema/test statements below remain historical.


## SENTINEL security — 5.4 completed (2026-10-21)

SENTINEL now reads local Defender protection/scan timestamps, effective firewall profiles, and bounded Security-log metadata through individually approved tasks. Timestamped results explicitly distinguish real host observations, warnings and unavailable data from simulated incidents. No scans, setting changes, elevation or remediation are enabled. Schema v17 persists these results through the existing broker. **518 tests passed** (14 Core, 157 Simulation, 97 Persistence, 250 Terminal), including 13 new security cases and live read-only provider verification. See [SENTINEL implementation](../Project_GRID_SENTINEL_Security_Implementation.md). Next: **5.5 JN — Storage and files**. Earlier milestone/schema/test statements below remain historical.


## MAXIS development — 5.3 completed (2026-10-21)

MAXIS now supports registered-workspace inspection, fixed .NET build/test execution, saved diagnostics and explicit replacement suggestions staged outside original source. Every task requires human approval; build/test executes trusted project code as the current Windows user (no OS/network sandbox). Schema v16 persists immutable, revocable workspace registrations. Other specialists remain demonstrations. **505 tests passed** (14 Core, 157 Simulation, 97 Persistence, 237 Terminal). See [MAXIS implementation](../Project_GRID_MAXIS_Development_Implementation.md) for commands, offline prerequisites, artifact retention and limits. Next: **5.4 SENTINEL**. Earlier milestone/schema/test statements below remain historical.


## MCP administration — 5.2 completed (2026-10-21)

Explicit `mcp request` clauses now create atomic saved specialist task plans. MCP reports actual child states/results; each child requires human approval and original-host dispatch. Adapters remain demonstrations. Natural-language planning and real specialist actions remain planned. Schema v15 preserves plan links and existing cities. **485 tests passed**, including 13 new coordination cases. Next: **5.3 MAXIS — Development**. See [MCP coordination implementation](../Project_GRID_MCP_Coordination_Implementation.md). Earlier milestone statements and schema/test counts below remain historical.


## Agent/task infrastructure - 5.1 completed (2026-10-21)

Explicit `agents initialize` registers MCP, MAXIS, SENTINEL, JN, VOLT and RELAY by stable ID. Schema v14 persists a bounded task queue, immutable proposals, one-attempt human approvals, results and audit. `tasks propose/inspect/approve/cancel/retry/run` enforce role-specific allowlists and live session authority at the service boundary. The broker saves a running record before invocation, prevents concurrent dispatch, supports cancellation/timeouts and requires fresh approval for retries/restart recovery. Agent sessions cannot self-approve or borrow capabilities. **Only labeled demonstration adapters are enabled; no real PC task, shell execution, natural-language delegation or autonomous producer is implemented.** Chat/voice messages remain conversation data.

**472 tests passed**, including 15 new task integration cases covering authorization, persistence barriers, cancellation/retry, deduplication, SQL rollback and restart/recovery. Existing v13 worlds migrate without regenerating the city or automatically creating agents. Next planned module: 5.2 MCP coordination. See [task broker implementation](../Project_GRID_Task_Broker_Implementation.md) for commands, bounds and limits. Earlier milestone/schema/test counts below remain historical.


## System-agent names updated - 2026-10-21

ARCHON is now **MCP** (coordination), FORGE is now **MAXIS** (development), and ARCHIVIST is now **JN** (storage/files). Current commands, examples, plans and checklist labels use the new names. These names do not grant privileges or implement the planned specialist services. The human **Master Control** role remains separate from MCP.

On world load, legacy-named Agent residents are renamed without changing IDs, registrations, context grants, voice-profile links or conversation ownership. Conversation participant labels update; historical messages/sender labels and event history remain intact. The coordinator retains its original identity-generation seed to avoid duplicate residents. Changes persist on the next normal save. A conflicting target name or exhausted revision skips the rename and records `AgentRenameSkipped`; resolve by ID rather than merging identities. Ordinary application/civilian names and the generic Archivist occupation are unchanged.

Verification: 457 tests passed (14 Core, 157 Simulation, 97 Persistence, 189 Terminal), including new legacy-name/idempotence and collision cases. Earlier completion dates and test counts remain historical evidence; documentation examples now use current names.


## Communications verification - 4.5 completed (2026-10-21)

All seven 4.5 checklist items have automated coverage. **455 tests passed** (14 Core, 155 Simulation, 97 Persistence, 189 Terminal), including three new end-to-end tests for authenticated conversation/context recovery through SQLite, malformed input without transcript mutation, and unrelated-agent privacy with logout revocation. Existing suites cover recipient routing, durable history/recovery, reviewed speech/typed-message equivalence, audio failure fallback and deterministic communication without AI. No production-code/schema change was needed. Physical microphone/playback/device-unplug checks remain manual; advanced voice and task-delegation features remain planned. See [verification matrix and limits](../Project_GRID_Communications_Verification.md). Earlier milestone counts below are historical.


## Voice transcription repair - 2026-10-21

A user-reported microphone transcription failure was reproduced with generated speech through the real C# helper connection: repeated synchronous `Recognize()` calls could reach an already-finished stream and throw `InvalidOperationException` (0x80131509), discarding recognized text. Recognition now uses `RecognizeAsync(Multiple)` and waits for `RecognizeCompleted`, gathering recognized phrases until completion. Speech/audio remain local and in memory. Explicit typed constructors avoid PowerShell overload ambiguity.

Failures now report a bounded stage, exception type and HRESULT rather than hiding the error or exposing raw helper stderr/audio. Capture reports duration and rejects recordings shorter than 0.25 seconds with a hold-Space hint. `voice 0` is a shortcut for `voice microphone 0`; no-draft hints recognize an already-connected conversation, and fallback advice is no longer duplicated.

**Verification:** 186 terminal tests pass, including four new real silent-PCM/generated-speech/invalid-audio and UI shortcut cases. The generated-speech case reproduced the failure before the completion-event fix and passes after it. No physical microphone recording or speaker playback was used; physical-device verification remains interactive. Earlier 448-test completion counts below are the original milestone's history.


## Offline voice - 4.4 Windows MVP completed (2026-10-21)

The original interactive host supports `voice devices/microphone/language/listen/review/edit/send/cancel/mute/speak/profile`. Hold Space to record (20-second maximum), release to transcribe with an installed offline Windows recognizer, review/correct, then explicitly send. Typed and microphone messages use the same dialogue service; speech never executes terminal commands. Playback starts muted and occurs only on explicit request. Saved registered-Agent profiles select Windows voice/rate/volume. Escape interrupts; identity/conversation changes prevent stale sends. Raw audio stays in transient memory and is not saved. Missing hardware or language/voice assets leave typed chat available.

Profiles are an additive validated field in existing schema-v13 communication JSON; legacy JSON defaults to no profiles. Restart and recovery preserve profiles and sent microphone-origin messages, not drafts/audio/device selection/mute state. Windows System.Speech is the implemented provider; whisper.cpp/Piper, attached-terminal audio, output-device selection and advanced voice options remain planned. 448 tests passed (18 new voice cases); real offline provider inventory succeeded. Physical microphone/speaker operation still needs an interactive smoke check. Earlier voice-as-planned statements below describe prior milestones. See [offline voice implementation](../Project_GRID_Offline_Voice_Implementation.md).


## Context convenience - 4.3A completed (2026-10-21)

`context add -search` now opens a local Windows multi-file picker (default: temporary, `Inbox`, owner only); optional retention/project/registered-Agent arguments are supported. Read/revoke/remove accept exact filenames or IDs. Multi-file `context read` and session-local `context select` support reading several captures together. Names must be unambiguous; reads recheck access and selections clear on identity change. Picker completion revalidates the session; batches validate before import and persistent batches save once. Main and attached terminals use the same host-owned context service, with no new schema or network requirement. Drag/drop and remote uploads remain unimplemented.

Completion verification: 430 tests passed, including 10 new 4.3A tests. Automated tests inject dialog results; visible Windows dialog interaction still needs manual verification. See the context implementation/help for command syntax and limits. Prior 4.3 notes below describe the original milestone.


## Local task context - 4.3 (2026-10-20)

Implemented `context add/list/read/revoke/remove` for explicit file captures, scoped Agent grants, project labels and temporary/persistent retention. Schema v13 and 420 passing tests. Read [context implementation](../Project_GRID_User_Supplied_Context_Implementation.md) for syntax, limits and privacy/backup behavior; AI ingestion/task execution remain future work.


## Shared terminal windows - 4.2C (2026-10-20)

Implemented `ntm` and `<command> -opninwin` with one authoritative host and scoped per-window contexts. Attached exit detaches; original-host exit disconnects clients before save. Lifecycle/credential commands remain host-only. 404 tests passed. See [window implementation](../Project_GRID_Shared_Terminal_Windows_Implementation.md) for commands, transport, authorization and verification limits.


## Installation editions and rendering - planned 2026-10-20

Installation will offer **PC GRID** (permanent machine installation, with advanced 3D options) or **Flash GRID** (mobile flash-drive package, with very simple lightweight 3D). The installer must explain the choice, validate the destination and retain the selected edition. Both share simulation/save/identity behavior and must run supported tasks offline after provisioning. Flash GRID keeps managed files with the portable package and requires no persistent host integration; graphics still run on the host PC. Quality differences must not regenerate or change the world.

See [edition details](../Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 7.6/8.6. Rendering budgets, packaging and migration remain planned; this addition changes documentation only.

## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](../Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all GRID runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](../Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 4.2B (implemented), 8.6 and 8.7 (planned). See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

## Local login and identity completion - 4.2A (2026-10-20)

**Implemented and verified.** Start with `login master`: the first successful Windows-token login binds the world owner; later logins must match. Sessions expire after eight hours. Master Control can register existing Agent identities, switch roles, and preview/confirm one-time program edits. Prompts/help/commands are role-aware; agent chats are participant-scoped and global/admin controls are denied. Switching/logout clears active chats and previews. Schema v11 preserves owner/agent registration without restoring authentication.

**362 tests passed**, plus isolated Windows login/agent restriction/confirmed edit/restart smoke. See [Local Login and Identity Implementation](../Project_GRID_Local_Login_and_Identity_Implementation.md) for commands, service authorization, audit and security limits. This authenticates the current Windows account, grants no OS elevation, and provides no independent agent passwords or real task service. NGE now requires Master Control; --start waits for login. Earlier dated pending-login/editing statements refer to prior milestones.

## Terminal conversations completion - 4.2 (2026-10-20)

**Implemented and verified.** Use connect (default MCP), talk, conversations, transcript, resume and disconnect. MCP is created on first explicit connection as a persistent coordinator Agent; a stable world-local operator handle supports reopening saved conversations. Replies to status/who are you are labeled verified GRID state; other replies are labeled deterministic simulated dialogue and claim no task execution/delegation. Input/reply pairs commit atomically and reuse schema v10.

**353 tests passed**, plus a three-launch isolated terminal save/resume/end smoke. See [Terminal Conversations](../Project_GRID_Terminal_Conversations.md) for syntax, identity/capacity limits and completion evidence. This local operator handle grants no authentication or admin authority; 4.2A login, real task delegation and LLM dialogue remain planned. Earlier dated pending-terminal-chat/agent-creation statements describe previous milestones.

## Communications foundation completion - 4.1 (2026-10-20)

**Implemented and verified.** Grid.Communication now defines recipient/message/conversation models and a local hub contract implemented by the simulation host. Exact name/full-ID routing, explicit participant identities, start/send/end/read operations, ordered bounded messages and schema v10 transactional transcript persistence are available. Conversations survive restart and backup recovery even when a historical application resident is absent; new sends require live original program identities.

**346 tests passed.** See [Communications Foundation](../Project_GRID_Communication_Foundation.md) for APIs, identity boundaries, bounds and recovery evidence. These trusted local APIs store messages without authenticating the caller, executing text, claiming external delivery or generating replies. Terminal chat is still 4.2; voice, real delegation, LLM dialogue and production login remain planned. Earlier dated pending-communication/schema statements describe previous milestones.

## Autonomy completion - 3.4 (2026-10-20)

**Implemented and verified.** The engine composes resource/shift/work/rest/travel and social decisions without an LLM. Equally ranked social choices now use a stable rank seeded by saved world identity, round and participant IDs; restore retains reproducibility without extra random state. Schema v9 gates this policy change. Preferences, affinity and world rules still take precedence.

**338 tests passed.** An accelerated one-hour simulation covered eight persistent agents, zero-to-512 fake application churn, resource scarcity/recovery, valid movement/events and concurrent snapshot readers. CPU, memory and latency measurements are recorded in [Autonomy and Verification](../Project_GRID_Autonomy_and_Verification.md). This is a simulated-hour test, not a real-time hour-long production soak. Host applications remain observation-only; automatic agent creation, admin configuration, real task execution and Phase 4 communications remain planned. Previous dated pending-autonomy/schema statements describe earlier milestones.

## Social civilization completion - 3.3 (2026-10-20)

**Implemented and verified.** Explicitly opted-in Agent/Special residents now have cooperative, competitive or reserved preferences; bounded relationships, memories and occupation history; and simulated district civic tokens with scarcity/recovery. Decisions run on simulation time, respect current work/travel and commit atomically with ticks. Host applications remain observation-only and receive no fictional social activity or resource consumption.

Schema v8 persists social state and partial rounds, upgrades existing worlds without inventing participants, and supports validated backup recovery. `social [id|name]` and program inspection provide read-only views. Trusted engine APIs configure social participation/pools; production login, terminal admin configuration, automatic agent creation, actual communication/delegation and Phase 3.4 sustained autonomy remain future work. **336 tests passed**, plus isolated terminal create/save/reopen. See [Social Civilization](../Project_GRID_Social_Civilization.md) for complete rules, limits and evidence. Earlier dated statements that social functionality is pending describe previous milestones.

## Movement and occupations completion - 3.2 (2026-10-20)

**Implemented and verified.** Saved city roads now support deterministic district travel for explicitly configured Agent/Special residents assigned to compatible capacity-one workstations. Five simulated seconds per edge; no work progress while traveling; shift/resource uncertainty freezes travel. Trusted assignment/release APIs preserve world ownership and revision checks. `jobs` and `activity` add read-only workstation and measured application views. Host applications acquire no fictional civic jobs.

Schema v7 preserves station assignments, remaining routes and edge timing in existing behavior JSON, validates them on restore and performs no offline catch-up. Older unassigned routines retain their 3.1 behavior. No agent population or interactive admin job configuration is added. **323 tests passed** in isolated Verification builds; disposable jobs/activity/save/reopen smoke passed. A running user Release session locked its binaries and was left untouched; rebuild the normal target after closing it to use the changes. See [Movement and Occupations](../Project_GRID_Movement_and_Occupations.md) for policy, limits and full completion evidence. Earlier statements that movement/workplaces are pending refer to previous milestones.

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

## Terminal commands and shutdown complete - 1.12 (2026-10-19)

Implemented grouped `help`, display-only `clear`, `programs -active`, `programs -possible`, `programs -sleeping`, and a GRID7736 ASCII farewell. Active selects fresh Running applications; sleeping selects fresh On call or Sleeping applications; possible includes current applications plus the saved known-application catalog. `ids` works with one filter. Unknown/stale observations are never classified as active or sleeping. Bare `programs` still includes agents and other residents.

**Schema v6** adds a separate ApplicationCatalog table (key, name, source, last catalog observation UTC). The catalog is captured with the simulation snapshot and saved atomically with world/history; backup/recovery and schema validation include it. Installed/observed metadata survives restart; ordinary residents, PID lifetimes, activity and resource readings are not restored. Last observation is catalog provenance, not proof of a launch or current installation. Entries missing from fresh discovery display Historical / unknown. Existing schema-v5-only statements below describe earlier milestones.

The ASCII banner appears after graceful shutdown and final save. Save/shutdown errors retain diagnostics and a nonzero exit code without a success message; ephemeral sessions explicitly say NOT SAVED. There is no keypress wait. Visual Studio can still append its own debug-console footer. Help uses portable ASCII separators and registered command metadata, including extensions; no-color/redirection behavior is preserved.

**Verification:** `dotnet test -c Release --no-restore`: **297 passed** (14 Core, 127 Simulation, 81 Persistence, 75 Terminal). Tests include filters/IDs/invalid flags/empty results, clear preserving the world and tick, successful/failed farewell, catalog save/reopen/recovery and v5-to-v6 migration. A disposable built-terminal run exercised all command groups, filters, redirected clear fallback and successful save/reopen of the same city. An ephemeral PTY run preserved tick1 through clear and displayed the NOT SAVED banner. This tool's PTY uses the clear fallback; physical screen clearing/color appearance and Visual Studio footer presentation were not visually verified in the user's console. No IDE settings or existing user worlds were modified.

## Latest population revision - 2026-10-19

The current population includes discoverable installed applications and current-session background processes. States are **Sleeping**, **On call**, **Running**, or **unknown** when evidence is insufficient. This supersedes earlier windowed-only/removal-on-exit descriptions below; those remain historical completion records. Ordinary application state is still rediscovered rather than persisted, and the saved city is reused. Full-machine coverage is not complete: see [Installed Application Availability](../Project_GRID_Installed_Application_Availability.md) for implemented scope, Windows inventory limits, remaining work and **290-test** verification.

## Program behavior completion - 2026-10-19

**3.1 implemented and verified.** Non-demo residents now have deterministic needs/goals/actions. Applications reflect fresh or unknown observations; explicitly configured Agent/Special residents perform resource-gated simulated occupation work/rest on simulation-time shifts. Schema v5 persists their routines, sourced budgets and progress without restoring live observations or adding offline work. Use behaviors and inspect for last-tick behavior. No OS task execution, movement or named agent services are claimed.

See [Program Needs and Behavior](../Project_GRID_Program_Behavior.md) for policy, APIs, limits and completion evidence: **278 Release tests passed**, build zero warnings/errors, disposable terminal smoke passed. Debug output was locked by running user sessions, which were left running. Earlier dated deferred-behavior/schema statements describe previous milestones; existing history and completed scope are preserved.

## Application resident completion - 2026-10-19

**2.7 / 2.7.1 implemented and verified:** production population now follows current-session windowed executable groups through read-only discovery and serialized lifecycle reconciliation. Application residents are transient; Agent/Special residents persist in schema v4. Before production conversion, legacy fictional citizens are preserved in a uniquely named validated archive. City/layout and clock remain intact. Unknown resource requirements display unknown condition; no host-control authority is implied.

Verification: **268 tests passed**, build zero warnings/errors, and a disposable Windows startup/reopen detected nine application groups. See [Application Residents Implementation](../Project_GRID_Application_Residents_Implementation.md) for grouping limits, discovery/freshness, storage/migration, editing, resource interpretation and completion evidence. Earlier dated statements that 2.7 is planned, production cities stay empty, or all residents persist describe previous milestones. Agent delegation/services, login, rendering and additional per-application telemetry remain future work.

## Hardware city completion - 2026-10-19

**2.6 / GRID-REQ-004 implemented and verified.** First-time creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates bounded district/street/building-lot geometry and saves actual layout and provenance in schema v3. Later startups reuse the saved world without discovery or regeneration. Existing worlds are preserved; new generated cities start empty pending 2.7. Inventory is creation provenance, separate from live resource telemetry and application identity. Rendering, atmosphere, fashion and application-resident integration remain planned.

Verification: **250 tests passed** (14 Core, 106 Simulation, 74 Persistence, 56 Terminal); build had zero warnings/errors. A fresh Windows city and unchanged reopen were verified in a temporary directory. Use city to inspect saved generation details; --world with a new filename explicitly creates a separate world.

See [Hardware City Generation](../Project_GRID_Hardware_City_Generation.md) for architecture, mappings, fallbacks, persistence and completion evidence. Earlier dated statements that hardware generation is planned or that normal creation always makes twenty fictional citizens describe previous milestones; this update supersedes those status statements while preserving their history.

## Phase 2 verification update - 2026-10-19

Section 2.5 is complete for the current implementation. **231 tests passed** (8 Core, 106 Simulation, 64 Persistence, 53 Terminal), with zero build warnings/errors. Eight new isolated cases verify headless terminal save/reopen/recovery, populated older-schema restoration, no offline catch-up, invalid/missing-file preservation and abrupt writer termination with SQLite journal recovery. A test-only helper under TestSupport is built by the persistence tests; production projects do not reference it.

See [Phase 2 Verification](../Project_GRID_Phase_2_Verification.md) for evidence and limits. Earlier test counts remain historical. Process-crash verification does not claim physical power-loss coverage; pending application energy/classification/lifecycle work and hardware-generated cities remain unimplemented. No production behavior was changed for 2.5.


## Resource/host foundation update - 2026-10-19

Section 2.4 now provides read-only host observations, explicit units/sources/freshness, fake-provider tests and a provenance-required additional-work resource-condition policy. Use resources for details or live for a compact CPU/RAM summary. Unsupported measurements and unknown application requirements remain unknown. Current observations are transient and cannot be edited into Windows allocations or restored from a save as fresh data.

Verification: 223 tests passed, build zero warnings/errors, and real Windows disposable-session sampling passed while simulation time stayed paused. See [Resources and Host State](../Project_GRID_Resources_and_Host_State.md) for collector coverage, the exact policy and limitations. Application discovery/classification, per-application resource attribution and replacing application energy drain remain dependent on 2.7. Existing fictional Genesis citizens retain their old rules. Earlier deferred-telemetry statements below describe prior milestones, not this update.


## History implementation update - 2026-10-18

Section 2.3 now persists meaningful committed changes with world snapshots in schema v2. The history command supports entity/kind/severity/time filters and bounded paging. Routine Tick records are excluded; pending-buffer overflow is reported explicitly. Changing edits include attribution/context, but denied attempts and no-ops are not a comprehensive security audit. Structured operational errors are emitted to terminal output. See [History and Observability](../Project_GRID_History_and_Observability.md) for retention, failure and API details.

Verification: 208 tests passed; solution build had zero warnings/errors; isolated terminal restart/query smoke passed. Earlier schema-v1 and deferred-history statements below describe the prior milestones. Live-view history remains process-local; the new saved-history path is separate. Application discovery, resource interpretation, resident classification and persistence filtering remain planned in 2.4/2.7.


## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

The user revised the product vision: ordinary programs represent currently running PC applications and are not individually saved; agents and explicitly designated special programs persist. Resources reflect application needs/usage and host conditions; energy is a defined interpretation, not arbitrary stamina. Read the shared contract and checklist 2.4/2.7 before further population work. Preserve historical 2.2 evidence without claiming this new model is implemented.

See [Host programs, lifecycles and resources](../Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


**Recorded:** 2026-10-18
**Purpose:** Help future agents continue this collaboration with the same continuity, accuracy and practical results.

This is a project handoff and a summary of observed user preferences, not a replacement for the agent's governing instructions, current user request, repository instructions or tool permissions. It describes visible working practices, not private model reasoning. Recheck the repository before relying on historical status.

## 1. Where work happens

- Repository: `C:\Users\jack\source\repos\Grid7736`
- Documentation: `MD Project Files`
- GitHub remote observed in this chat: `https://github.com/Scorpio7736/Grid7736.git`
- Environment used: Windows and PowerShell; user timezone America/Chicago.
- Main planning entry point: [Master Development Checklist](../Project_GRID_Master_Development_Checklist.md).
- Implementation evidence: [Current Implementation Status](../Project_GRID_Current_Implementation_Status.md), dated completion notes and actual source/tests.
- Requirements and architecture: [Master Overview](../Project_GRID_Master_Overview.md) and [Design Specification](../Project_GRID_Design_Specification.md).
- How to run and extend the terminal: [README](../../README.md).

The conversation progresses through numbered checklist modules, followed by verification, documentation and occasional release or terminal-launch requests. Examples include world management 1.4, clock 1.5, engine 1.6, terminal 1.7 and editing preparation/API 1.9.1-1.9.3. The user may refer to a module number without repeating its requirements; read the matching checklist section before acting.

## 2. How to interpret the user's requests

The user frequently dictates messages. Punctuation, repeated words and short corrections may be transcription artifacts. Interpret them using the surrounding task; ask only when ambiguity materially affects the outcome.

- Requests such as "work on 1.9.3" mean carry out that module, verify it and update its completion notes, not merely propose a plan.
- "Add this to the MD files" is a documentation/planning request unless implementation is also requested. Do not turn a planned feature into code without that scope.
- "Do the same thing" usually refers to the previous module's workflow, including completion evidence. Inspect the context rather than copying claims blindly.
- A correction or added requirement normally steers the active task. Preserve unfinished work unless the user clearly cancels or replaces it.
- When the user says they missed permission, reissue the actual blocked request. Do not assume a rejected or unanswered tool approval succeeded.
- A request to commit changes to GitHub authorizes a commit and push for that work. It is not blanket permission to publish every future edit.

The user values practical action, continuity and thorough documentation. Use reasonable defaults for routine reversible choices. Avoid repeatedly asking for authorization already provided, while respecting actual tool restrictions and unresolved decisions.

## 3. A successful task cycle

1. **Orient:** locate the repo, read applicable AGENTS.md files if present, inspect Git status and identify the relevant checklist and design sections. Look for newer commits or user edits before changing anything.
2. **State the action:** give a brief update describing the intended result and important assumptions. Keep ongoing updates focused on findings and remaining work.
3. **Resolve missing inputs:** locate referenced documents or ask for their location. Continue independent work when useful, but never claim to compare a supplied version that was not available.
4. **Do the scoped work:** preserve unrelated changes, follow the architecture and keep feature boundaries explicit. Prefer focused edits over replacing whole files with older copies.
5. **Verify proportionately:** use meaningful tests/builds for runtime changes; inspect diffs, links, formatting and checklist preservation for documentation-only changes. Do not invent successful tests or rerun broad checks without a reason.
6. **Document:** update the relevant checklist and supporting documents, recording what changed, what was verified and what remains deferred.
7. **Report:** lead with the outcome, list material changes and limitations, link useful files, and state whether changes are local, committed or pushed. Complete authorized publishing steps before reporting publication success.

Keep communication concise and plain. Explain what a change does and why it matters. A plan is not a substitute for completing an action request, and an intermediate progress message is not the final handoff.

## 4. Documentation rules established in this chat

The user explicitly liked dated completion notes and asked that they continue. The user also requested comprehensive documentation so changes are not left undocumented.

- Preserve completed checklist items and their historical evidence. Do not overwrite newer repository information with an older supplied document.
- Distinguish **planned**, **design complete**, **implemented**, and **verified**. A completed design does not establish working runtime behavior.
- Use checked items only when supported by observed implementation or the item's actual design deliverable. Keep future implementation and acceptance tests unchecked.
- Record dates, relevant commit references, exact verification commands and observed results where useful. Historical counts remain historical; label later totals separately.
- Distinguish a project scaffold from an implemented subsystem, a returned receipt from durable audit, and a command/API contract from a usable terminal workflow.
- Keep requirements consistent across overview, specification, roadmap, checklist and affected style/storage guides. Cross-link a shared detailed contract rather than creating contradictory copies.
- When asked to update all documents, audit their relevant implications. Style guides may need integration notes without rewriting the established art direction.
- Document unresolved choices and conflicts honestly. Never imply that missing files were supplied, an unavailable feature was tested, or a GitHub push completed without evidence.

Suggested completion-note format:

```markdown
**Implemented and verified - YYYY-MM-DD**

- Changed: concrete behavior and affected boundary.
- Verification: command/check and observed result.
- Remaining: deferred features, dependencies or known limitations.
```

For design-only work, use **Design documented** and explicitly say that no runtime feature was implemented or tested.

## 5. Architecture and boundaries to preserve

- C#/.NET owns authoritative world state and simulation mutations. Terminal and future viewers consume consistent snapshots and use validated services for changes.
- Extend explicit command/field registries; avoid hard-coding features into the input loop or allowing arbitrary property mutation.
- Keep simulation timing independent of display refresh and real wall-clock timestamps. Do not invent agent activity when programs are Idle.
- Human Master Control, an active agent identity and autonomous/system actions are distinct. Names, occupations, uniforms and hardware do not grant permissions.
- Administrative editing uses validation, serialized mutation, revisions and authorization. An injectable authorization contract is not a production login provider.
- Keep local-first operation, persistence, future host tasks and rendering responsibilities separate. Follow the current design documents for details.

Relevant contracts: [Program Editing Rules](../Project_GRID_Program_Editing_Rules.md), [Session Context](../Project_GRID_Session_Context_and_Authorization.md), and [Terminal Identity Plan](../Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md).

## 6. Recent context to verify before continuing

These are historical observations, not guarantees about the next checkout:

- Genesis was released under `v0.1.0-genesis`. Later editing/startup changes were separate work; do not move the release tag to include them.
- The 1.9.3 solution run passed 117 tests on 2026-10-17; the 1.10 live-view run passed 140 tests on 2026-10-18. Do not present that as a new test run or assume the count still matches later changes.
- The terminal supports optional `--start` automatic ticking. Live view was implemented in section 1.10 on 2026-10-18, with Q/Esc return, paging, bounded event history and console fallback. Check current source before relying on this historical status; login visibility filtering remains deferred.
- The Grid.Persistence scaffold was subsequently replaced by section 2.1 schema/migration implementation on 2026-10-18. Its 31 tests bring the solution to 171 passing tests. Section 2.2 subsequently implemented real engine save/load, autosave, orderly shutdown saves and explicit backup recovery. Its 2026-10-18 verification passed 200 solution tests (8 Core, 93 Simulation, 50 Terminal, 49 Persistence). Default storage is %LOCALAPPDATA%\GRID\world.db; --world selects an isolated file, --recover restores its validated backup, and --ephemeral keeps a disposable session. Do not launch smoke tests against the user's default world. Durable events and hardware-generated cities remain pending.
- GRID-REQ-004 was documented across eight existing documents on 2026-10-18: discover motherboard, CPU, GPU, RAM, storage and network hardware on first-time world creation, generate a unique city, save it and reuse the world on later startups.
- That requirement was documentation-only. Its detailed contract is in the design specification and its unchecked implementation tasks are in checklist 2.6, with later Phase 7/8 integration.
- Existing saved worlds take priority over hardware changes or regeneration. Preserve Genesis history and the existing city, atmosphere and fashion direction.
- The user clarified that the eight supplied documents were the current files in MD Project Files. Older Downloads copies contained no GRID-REQ-004. The written requirement supplied the addition; no separate updated file set was available for a version-to-version merge.

Reconcile source, tests, Git history and dated notes if they disagree. Preserve newer work and explain the discrepancy rather than silently reverting it.

## 7. Terminal, Git and permission practices

When asked to run GRID, open the requested interactive terminal and verify the application process starts. Typical automatic startup from the repository is:

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start
```

Minimizing the window lets the user continue working; it does not install a background service. Do not close an existing instance unless requested. If replacing one, identify only the relevant application/terminal processes. Check current persistence support before implying state survives exit.

Before committing or pushing, inspect the branch, remote, status and staged diff. Include only the authorized scope and preserve unrelated work. A created commit is not proof of a successful push. In this chat, Git Credential Manager once blocked a push with a Select an account window; ask the user to complete account selection and verify the eventual result. On resuming an interrupted turn, check whether the earlier operation completed before retrying it.

Use the active tool permission mechanism when required. If an operation is blocked, explain the concrete blocker and request only the action needed. Do not ask for credentials in chat, bypass restrictions or report an approval as execution success.

## 8. Starting a future session well

Read this guide, the latest checklist and relevant contracts, then inspect the current working tree. Identify the user's requested outcome, the last verified state and any unfinished work. Carry forward their preference for expandable design and completion notes, while treating the present user request as the task boundary.

Do not automatically start the next module, implement a planning request, launch processes or push commits merely because those actions occurred earlier in this conversation. The useful continuity is the project's context and working standards; each action still needs to fit the current request.


## Existing-code adaptation plan - 2026-10-18

The user requested explicit retrofit tracking for already completed modules affected by GRID-REQ-005. Use checklist 2.7.1 and the shared retrofit mapping; do not uncheck historic completion entries or claim new compliance from old tests. Address classification and safe lifecycle first, coordinate persistence/startup, then integrate telemetry, editing and live view before more Phase 3 behavior.

See [existing implementation retrofit](../Project_GRID_Host_Programs_and_Resources.md#existing-implementation-retrofit---planned-2026-10-18) and checklist 2.7.1. Documentation only; implementation remains pending.

## Task and MCP readability follow-up — 2026-10-21

Implemented compact agent/task tables with role names, unique short references, a compact `mcp plans` overview, and separated plan/task details, results, next commands and full audit identities. `mcp brief` still includes all plan details; `mcp inspect <reference>` reads one plan. Full IDs remain in detail views. Displayed short references (at least eight characters) are accepted by task inspect/approve/cancel/retry and MCP inspect/brief; ambiguous prefixes are rejected, and resolution is restricted to the current authorized scope. A copied trailing period or comma is tolerated. Plan/task mixups produce specific guidance. Approval boundaries and persisted data are unchanged. Persistence warnings now appear only on I/O/database errors.

Use `tasks list`, then `tasks inspect <displayed-task-reference>`. Review its argument before using the displayed approval command. Command examples are printed on separate lines without sentence punctuation. Verification: 219 terminal tests passed, including short-reference, punctuation, plan/task mismatch and cross-agent scope checks.
