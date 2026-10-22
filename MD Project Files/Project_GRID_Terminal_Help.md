# GRID7736 - Terminal Help

## Conversation-window update — 4.2E (2026-10-21)

`connect` and `resume` now open a dedicated conversation window by default. The original terminal remains usable. Within the new window, `talk` returns a labeled typewriter response; any key skips animation. `transcript` is instant. `exit` closes the view while retaining an open conversation for later resume; `disconnect` ends it. Parent logout/identity changes revoke attached views. Meetings remain planned in 6.2A.

```text
connect
    - connect <program-id|name>
      # USAGE: Open a dedicated conversation window; omitted contact defaults to MCP.
    - connect -inline [program-id|name]
      # USAGE: Keep instant-text chat in this terminal, including original-host voice support.
    # EXAMPLE: connect MCP

resume
    - resume <conversation-id>
      # USAGE: Open a window for an existing open conversation owned by this participant.
    - resume -inline <conversation-id>
      # USAGE: Resume here for original-host voice or instant inline interaction.
    # EXAMPLE: resume -inline <full-conversation-id>

typewriter
    - typewriter on
      # USAGE: Animate new agent responses in this dedicated conversation window.
    - typewriter off
      # USAGE: Display agent responses instantly in this window.
    # EXAMPLE: typewriter off
```

`typewriter` is available in dedicated conversation windows. Set `GRID_TYPEWRITER=off` before launching GRID for reduced motion across inherited windows. Redirected input/output and dumb terminals always use instant text. For voice, use `connect MCP -inline` in the original host before `voice listen`. [Details and acceptance test](Project_GRID_Conversation_Windows_Implementation.md).


## VOLT and RELAY — 5.6 completed (2026-10-21)

VOLT now reports existing host telemetry and supports approved normal/eco/auto pacing of GRID's automatic simulation. Unknown/unsupported GPU data is preserved; Windows resources and manual tick behavior are unchanged. RELAY provides a saved local inbox, recorded-task briefings, human acknowledgements and explicitly approved offline speech with delivery/failure receipts. Schema v19 preserves mode and inbox history without automatic playback/replay. **551 tests passed** (14 Core, 157 Simulation, 97 Persistence, 283 Terminal). See [VOLT/RELAY implementation and acceptance test](Project_GRID_VOLT_RELAY_Implementation.md) for commands and boundaries. Next: **5.7 end-to-end verification**. Earlier milestone/schema/test statements below remain historical.


## JN storage — 5.5 completed (2026-10-21)

JN now inventories approved paths, reports possible duplicates, and prepares cleanup proposals without deleting source files. Defined GRID-managed storage is measured against an initial 20 GB decimal logical budget; MCP quota escalations require separate Master Control approval to change it. This is tracking/governance, not a hard write cap or disk reservation. Schema v18 preserves scope and quota history. **535 tests passed** (14 Core, 157 Simulation, 97 Persistence, 267 Terminal). See [JN implementation and acceptance test](Project_GRID_JN_Storage_Implementation.md) for limits, accounting roots, report retention and remaining planned automation. Next: **5.6 VOLT and RELAY**. Earlier milestone/schema/test statements below remain historical.


## SENTINEL security — 5.4 completed (2026-10-21)

SENTINEL now reads local Defender protection/scan timestamps, effective firewall profiles, and bounded Security-log metadata through individually approved tasks. Timestamped results explicitly distinguish real host observations, warnings and unavailable data from simulated incidents. No scans, setting changes, elevation or remediation are enabled. Schema v17 persists these results through the existing broker. **518 tests passed** (14 Core, 157 Simulation, 97 Persistence, 250 Terminal), including 13 new security cases and live read-only provider verification. See [SENTINEL implementation](Project_GRID_SENTINEL_Security_Implementation.md). Next: **5.5 JN — Storage and files**. Earlier milestone/schema/test statements below remain historical.


## MAXIS development — 5.3 completed (2026-10-21)

MAXIS now supports registered-workspace inspection, fixed .NET build/test execution, saved diagnostics and explicit replacement suggestions staged outside original source. Every task requires human approval; build/test executes trusted project code as the current Windows user (no OS/network sandbox). Schema v16 persists immutable, revocable workspace registrations. Other specialists remain demonstrations. **505 tests passed** (14 Core, 157 Simulation, 97 Persistence, 237 Terminal). See [MAXIS implementation](Project_GRID_MAXIS_Development_Implementation.md) for commands, offline prerequisites, artifact retention and limits. Next: **5.4 SENTINEL**. Earlier milestone/schema/test statements below remain historical.


**Implemented - 4.2B, 2026-10-20.** This bundled reference is available offline through `help -md`, which displays its path and Markdown in the terminal. Live `help` and Tab suggestions are capability-scoped; this full manual does not grant command access.

New worlds prompt for a username and masked password (6..24 characters), then use `login`. Existing Windows-owned worlds use `login master`, then `credentials` to migrate or rotate credentials as Master Control. Passwords are entered only at masked prompts, never as command arguments. Password login/setup refuse redirected input. Examples containing placeholders require actual values; `nge` deletes the selected world after confirmation.

## Login and identity

```text
login
    - login master
      # USAGE: Authenticate with masked local credentials, or the Windows owner for an unmigrated world.
    # EXAMPLE: login master
```

```text
credentials
    - credentials
      # USAGE: Master Control creates/replaces local credentials using masked prompts; sign in again afterward.
    # EXAMPLE: credentials
```

```text
whoami
    - whoami
      # USAGE: Show operator, active role, session and capabilities.
    # EXAMPLE: whoami
```

```text
register-agent
    - register-agent <id|name>
      # USAGE: Master Control registers an existing Agent identity.
    # EXAMPLE: register-agent MCP
```

```text
switch
    - switch master
      # USAGE: Select Master Control.
    - switch <agent-id|name>
      # USAGE: Select a registered agent identity.
    # EXAMPLE: switch MCP
```

```text
self
    - self
      # USAGE: Show the active identity and its own resident state.
    # EXAMPLE: self
```

```text
logout
    - logout
      # USAGE: Confirm with y; n/blank cancels. Confirmed logout clears context and returns to welcome.
    # EXAMPLE: logout
```

## Simulation and world

```text
start
    - start
      # USAGE: Start or resume simulation; Master Control required.
    # EXAMPLE: start
```

```text
pause
    - pause
      # USAGE: Pause simulation.
    # EXAMPLE: pause
```

```text
step / tick
    - step
      # USAGE: Advance one tick while paused.
    - tick
      # USAGE: Alias for step.
    # EXAMPLE: step
```

```text
status
    - status
      # USAGE: Read current world/clock state.
    # EXAMPLE: status
```

```text
city
    - city
      # USAGE: Read saved city layout and hardware provenance.
    # EXAMPLE: city
```

```text
districts
    - districts
      # USAGE: List district populations.
    - districts ids
      # USAGE: Show full identifiers.
    # EXAMPLE: districts ids
```

```text
programs
    - programs
      # USAGE: List resident programs.
    - programs -active
      # USAGE: Show fresh Running application observations.
    - programs -possible
      # USAGE: Show current/known application candidates; not proof of current installation.
    - programs -sleeping
      # USAGE: Show fresh On call or Sleeping applications.
    - programs ids
      # USAGE: Show full identifiers; combine with at most one availability filter.
    # EXAMPLE: programs -active ids
```

```text
inspect
    - inspect program <id|name>
      # USAGE: Inspect one program.
    - inspect district <id|name>
      # USAGE: Inspect one district.
    # EXAMPLE: inspect program MCP
```

```text
jobs
    - jobs
      # USAGE: Show simulated civic workstation assignments and vacancies.
    # EXAMPLE: jobs
```

## Monitoring

```text
live
    - live
      # USAGE: Open dashboard; Q/Esc returns to commands. Requires Master Control.
    # EXAMPLE: live
```

```text
behaviors
    - behaviors
      # USAGE: Show resident needs/actions and simulated work progress.
    # EXAMPLE: behaviors
```

```text
activity
    - activity
      # USAGE: Show measured application availability and working-set evidence.
    # EXAMPLE: activity
```

```text
social
    - social
      # USAGE: List opted-in social residents and fictional civic pools.
    - social <id|name>
      # USAGE: Show one resident's social memories/relationships.
    # EXAMPLE: social MCP
```

```text
resources
    - resources
      # USAGE: Show host measurements.
    - resources details
      # USAGE: Include measurement details and provenance.
    # EXAMPLE: resources details
```

```text
history
    - history
      # USAGE: Read saved events; requires a persistent world and Master Control.
    - history details
      # USAGE: Include structured event details.
    - history kind=<kind>
      # USAGE: Filter event kind.
    - history entity=<guid>
      # USAGE: Filter entity ID.
    - history severity=<level>
      # USAGE: Filter severity.
    - history since=<UTC>
      # USAGE: Use a round-trip UTC timestamp, e.g. 2026-10-20T00:00:00.0000000+00:00.
    - history limit=<1..500>
      # USAGE: Limit returned rows.
    - history offset=<0..>
      # USAGE: Skip rows; combine filters as needed.
    # EXAMPLE: history kind=Edit limit=20
```

## Conversations

```text
connect
    - connect
      # USAGE: Open a conversation window with MCP; Master Control can create the contact lazily.
    - connect <id|name>
      # USAGE: Open a directed conversation window; identities must be distinct.
    # EXAMPLE: connect MCP
```

```text
talk
    - talk <message>
      # USAGE: Send in the active conversation; replies are deterministic and labeled.
    # EXAMPLE: talk status
```

```text
conversations
    - conversations
      # USAGE: List conversations in which the active identity participates.
    # EXAMPLE: conversations
```

```text
transcript
    - transcript
      # USAGE: Read the active conversation.
    - transcript <conversation-id>
      # USAGE: Read a permitted stored transcript.
    # EXAMPLE: transcript
```

```text
resume
    - resume <conversation-id>
      # USAGE: Open a window for a permitted conversation; copy its ID from conversations.
    # EXAMPLE: resume <conversation-id>
```

```text
disconnect
    - disconnect
      # USAGE: End the active conversation and retain its transcript.
    # EXAMPLE: disconnect
```

## Master Control editing

```text
edit
    - edit <program-id|quoted-name> <field> <value>
      # USAGE: Preview name, occupation, energy, status or district. District needs a full ID. Non-demo energy/status are service-owned and cannot be edited.
    # EXAMPLE: edit MCP occupation "Local coordinator"
```

```text
apply-edit
    - apply-edit <preview-id>
      # USAGE: Commit the current preview if identity and revision still match.
    # EXAMPLE: apply-edit <preview-id>
```

```text
cancel-edit
    - cancel-edit
      # USAGE: Discard the current preview.
    # EXAMPLE: cancel-edit
```

## Help and terminal

```text
help
    - help
      # USAGE: Show capability-scoped console help.
    - help -md
      # USAGE: Display this bundled Markdown reference and its path offline in the terminal.
    # EXAMPLE: help
```

```text
clear
    - clear
      # USAGE: Clear display when supported; retain world state.
    # EXAMPLE: clear
```

```text
nge
    - nge
      # USAGE: Master Control only. Review the destructive warning; y confirms and n cancels. Requires interactive persistent session.
    # EXAMPLE: nge
```

```text
exit
    - exit
      # USAGE: Stop this local terminal host and save the persistent world.
    # EXAMPLE: exit
```

## Startup options (shell, not grid prompt)

```text
Grid7736
    - Grid7736 --start
      # USAGE: Request simulation start after successful login.
    - Grid7736 --world "<path>"
      # USAGE: Select a world file; parent directory must exist.
    - Grid7736 --recover
      # USAGE: Explicitly recover the selected/default world from backup.
    - Grid7736 --ephemeral
      # USAGE: Run without persistent storage; incompatible with --world/--recover.
    - Grid7736 --no-color
      # USAGE: Disable ANSI/console colors; combine with other valid options.
    # EXAMPLE: Grid7736 --no-color
```

## Login/welcome workflow

Opening GRID7736 graphic -> first-run credential setup if needed -> login -> clear screen -> welcome username and Master Control authority -> Enter -> terminal. Agent selection remains available after authentication. Logout asks y/n; cancellation retains context, confirmation clears the screen and returns to welcome. Expiry denies commands immediately and returns to welcome at the next input boundary. EOF/Ctrl+C exits through normal save/shutdown handling.

## Palette and completion

Base text is light blue (Console Cyan), input white, tables/dividers yellow, and errors red. `--no-color` and redirected output remain plain text. Tab cycles permitted command/static option prefixes without executing. Arrow keys, Home/End, Backspace/Delete edit input. Quoted names remain editable, but completion inside quotes is suppressed; no resident names, private IDs, paths, or secret values are suggested. Password entry is masked and has no completion or history. Q/Esc in live view returns to commands.

## Additional windows (4.2C)

```text
<read-only-command>
    - <read-only-command> -opninwin
      # USAGE: Open supported data as a labeled snapshot in a separate window; combine with normal command arguments.
    # EXAMPLE: programs -active ids -opninwin

live
    - live -opninwin
      # USAGE: Open an updating view of the same GRID; Q/Esc closes this view.
    # EXAMPLE: live -opninwin

ntm
    - ntm
      # USAGE: Open another terminal connected to the same running GRID, with its own command/chat/edit context and scoped authority.
    # EXAMPLE: ntm
```

Snapshot commands: status, city, districts, programs, inspect, behaviors, resources, history, jobs, activity, social, self, whoami, conversations, transcript and help. Permissions still apply; mutating commands cannot use the flag. Scroll a snapshot and press Enter to close. Live supports PageUp/PageDown and left/right event paging.

Attached `exit` detaches only that window. Original-host `exit` saves and disconnects all attachments. Logout/expiry/role changes revoke dependent windows. Agent attachments cannot promote themselves. Run NGE, transfers and credential changes in the original host. Reopen from an authenticated terminal after disconnection; clients never load another world automatically. See [window implementation](Project_GRID_Shared_Terminal_Windows_Implementation.md).

## Move/copy commands (4.2D / 4.2F)

Master Admin means the existing Master Control role. Both operations require an interactive persistent session in the original host and close it after confirmation and final save. Choose a new absolute destination directory with an existing parent. These commands are entered inside GRID.

```text
grid
    - grid -copyto "location"
      # USAGE: Copy the GRID application and selected save inventory after preview and y/n confirmation; keep the original.
    - grid -transferto "location"
      # USAGE: Move the GRID application and selected save inventory after preview and y/n confirmation; source cleanup runs after exit.
    # EXAMPLE: grid -copyto "E:\GRID7736 Copy"
```

Supply one operation and one destination; quote paths with spaces. Switches are case-insensitive. `copexist` and `tranexist` only show the new syntax and perform no transfer. Attached terminals and conversation windows must use the original host for transfers.

Use `launch.cmd` at the destination; a compatible .NET 10 runtime is required. For moves, wait for `transfer-status.txt` to report completion. Source cleanup failures retain the verified destination and report incomplete status. The transfer excludes unrelated host files, source repositories and other installed worlds. Configure local password credentials before using a different PC. See [transfer implementation](Project_GRID_Transfer_Implementation.md) for scope and recovery limits. These commands do not create attached terminals; use `ntm` for that. Transfers disconnect attachments before saving/copying.

## Password requirements - completed follow-up (2026-10-20)

**Implemented.** New passwords and password changes accept **6–24 characters inclusive**. Uppercase letters, lowercase letters, numbers and symbols are allowed; no character category or mixture is mandatory. Preserve password case and exact characters; do not silently trim or truncate passwords.

Creation/change validation, masked setup prompts, error messages and help now use **6–24 characters**. Existing saved credentials remain verifiable up to the former 256-character limit until the owner explicitly changes the password; the input reader retains that capacity for legacy login. Masked input, salted hashing and authorization are unchanged. Control characters remain rejected. **Completion verification: 390 tests passed** (14 Core, 155 Simulation, 91 Persistence, 130 Terminal), including 13 new cases for boundaries, permitted categories, exact case/spacing, confirmation mismatch, legacy 25/256-character login after restore, and authenticated rotation to the new policy. No saved credential migration or password reset is required.

## User-supplied context (4.3 / 4.3A)

```text
context
    - context add "file" <temporary|persistent> "project" [registered-agent-id|name]
      # USAGE: Master Control imports one explicit local file. Omit agent for owner-only access. Persistent imports save with the world; temporary imports end when the importing session changes identity/logs out or the host exits.
    - context list [project]
      # USAGE: List only context visible to the active identity, optionally filtered by project name.
    - context add -search [temporary|persistent "project" [registered-agent-id|name]]
      # USAGE: Open the Windows file dialog; Ctrl/Shift selects multiple files. Without options: temporary, Inbox, owner only. Cancel imports nothing.
    - context select <id|name> [id|name ...]
      # USAGE: Replace this terminal's file selection. With no arguments, show selection; use context select clear to clear it.
    - context read [id|name ...|-selected]
      # USAGE: Read one or several captured files without reopening sources. With no arguments or -selected, read the current selection. Quote filenames containing spaces; ambiguous names require an ID.
    - context revoke <id|name>
      # USAGE: Master Control removes agent access, retaining the owner's captured content.
    - context remove <id|name>
      # USAGE: Master Control removes the active import; historical backups/already-read output are not erased.
    # EXAMPLE: context add -search persistent "My Project" MCP
    # EXAMPLE: context select "notes.md" "design.docx"
    # EXAMPLE: context read
    # EXAMPLE: context read "notes.md" "design.docx"
```

Register the agent before granting access. Supported inputs: UTF-8 text/Markdown/common source code and DOCX body text. Limits: 1 MiB source, 256 Ki text characters per import, 64 imports and 1 Mi total text characters. No folder grants, automatic rereads, network paths, macro execution or silent truncation. PDF/DOC/RTF and unsupported encodings require exporting to a supported format. Re-import for a new captured version. `talk context` reports the recipient's granted-file count; it does not analyze or copy file bodies into transcripts. See [implementation and limits](Project_GRID_User_Supplied_Context_Implementation.md).

## Offline voice communication (4.4)

```text
voice
    - voice status
      # USAGE: Show microphone, language, mute status and unsent draft.
    - voice devices
      # USAGE: List local microphones, installed offline recognizers and Windows voice names.
    - voice microphone <-1|id>
      # USAGE: Choose a microphone; -1 uses the Windows default. IDs come from voice devices.
    - voice <microphone-id>
      # USAGE: Shortcut for microphone selection; voice 0 selects device 0.
    - voice language <culture>
      # USAGE: Choose an installed recognition culture, such as en-US.
    - voice listen
      # USAGE: Connect first. Hold Space to record, release to transcribe, Esc to cancel. Maximum 20 seconds; nothing sends automatically.
    - voice review
      # USAGE: Read the unsent transcript and intended conversation.
    - voice edit <replacement text>
      # USAGE: Correct the current draft before sending.
    - voice send
      # USAGE: Explicitly send the reviewed draft as a conversation message, never as a terminal command.
    - voice cancel
      # USAGE: Discard the draft. During recording/transcription/playback press Esc instead.
    - voice mute <on|off>
      # USAGE: Playback starts muted. Unmuting only permits explicit voice speak; no automatic audio.
    - voice speak
      # USAGE: Speak the connected agent's latest reply locally; Esc interrupts.
    - voice profile
      # USAGE: List visible saved per-agent voice settings.
    - voice profile <agent-id|name> "voice name" <rate> <volume>
      # USAGE: Master Control saves a registered Agent's voice; rate -10..10, volume 0..100. Requires a saved world.
    # EXAMPLE: voice profile MCP "Microsoft David Desktop" 0 80
    # EXAMPLE: voice listen
    # EXAMPLE: voice review
    # EXAMPLE: voice send
    # EXAMPLE: voice mute off
    # EXAMPLE: voice speak
```

Voice hardware requires the original interactive Windows host terminal and installed offline recognition/voice assets. Use `connect MCP` first; registration is required for profiles, not ordinary conversation. Attached terminals continue using `talk` and `resume`. Missing audio/language/voice support leaves typed chat available. No raw recordings are saved. See [offline voice implementation](Project_GRID_Offline_Voice_Implementation.md) for verification and limits.

Voice troubleshooting (2026-10-21): capture reports its duration; recordings shorter than 0.25 seconds are refused with a retry hint. Transcription waits for the recognizer completion event. If recognition still fails, report the displayed stage/exception/HRESULT. No raw audio or helper input is included in that diagnostic. `voice speak` requires a reply from `voice send` or `talk` first.

## Agent/task infrastructure (5.1)

```text
agents
    - agents initialize
      # USAGE: Master Control explicitly creates/registers six system-agent roles in a saved world. Names alone confer no capability.
    - agents
      # USAGE: List role IDs and allowed demonstration capabilities.
    # EXAMPLE: agents initialize

tasks
    - tasks list
      # USAGE: List tasks visible to your identity.
    - tasks propose <agent-id|name> <capability> "argument" [request-key]
      # USAGE: Create an immutable proposal awaiting human approval; a stable key deduplicates identical requests.
    - tasks inspect <id>
      # USAGE: Review the exact request, operator/agent attribution, result and audit before approval.
    - tasks approve <id>
      # USAGE: Master Control approves one attempt of the exact proposal.
    - tasks run
      # USAGE: Original host Master Control dispatches one approved demonstration task. No real host action is available.
    - tasks cancel <id>
      # USAGE: Cancel an authorized pending/running task; an attached terminal can cancel while the host waits.
    - tasks retry <id>
      # USAGE: Failed/timed-out/interrupted demonstrations can retry up to three attempts, with fresh approval each time.
    # EXAMPLE: tasks propose MAXIS maxis.demo "hello GRID" example-request-1
    # EXAMPLE: tasks inspect <returned-id>
    # EXAMPLE: tasks approve <returned-id>
    # EXAMPLE: tasks run
```

Capabilities are `mcp.demo`, `maxis.demo`, `sentinel.demo`, `jn.demo`, `volt.demo`, `relay.demo` (1..256 printable argument characters), plus `maxis.delay` (0..5000 ms; attempt timeout is one second). Agents may propose only for their own role, never approve or dispatch. Restart clears queued approvals and interrupts running tasks without automatic replay. No automatic chat-to-task interpretation. See [task broker implementation](Project_GRID_Task_Broker_Implementation.md).

## MCP coordination — 5.2 implemented

```text
mcp
    - mcp request "SPECIALIST: action argument; SPECIALIST: action argument" [request-key]
      # USAGE: Save 1..8 explicit demonstration proposals requiring individual human approval.
    - mcp plans
      # USAGE: View all plans and their child statuses/results.
    - mcp inspect <plan-id>
      # USAGE: Inspect one plan and its approval requirements.
    - mcp brief [plan-id]
      # USAGE: Report broker states/results for one or all plans.
    # EXAMPLE: mcp request "MAXIS: demo hello; JN: demo inventory" example-plan-1
```

Initialize roles with `agents initialize`. Only Master Control or registered MCP can coordinate. Actions are demo for specialists and delay for MAXIS; real specialist work remains planned. Inspect and approve each child with `tasks inspect <id>` and `tasks approve <id>`, then dispatch with `tasks run` on the original host. See [MCP coordination](Project_GRID_MCP_Coordination_Implementation.md).

## Task and MCP readability follow-up — 2026-10-21

Implemented compact agent/task tables with role names, unique short references, a compact `mcp plans` overview, and separated plan/task details, results, next commands and full audit identities. `mcp brief` still includes all plan details; `mcp inspect <reference>` reads one plan. Full IDs remain in detail views. Displayed short references (at least eight characters) are accepted by task inspect/approve/cancel/retry and MCP inspect/brief; ambiguous prefixes are rejected, and resolution is restricted to the current authorized scope. A copied trailing period or comma is tolerated. Plan/task mixups produce specific guidance. Approval boundaries and persisted data are unchanged. Persistence warnings now appear only on I/O/database errors.

Use `tasks list`, then `tasks inspect <displayed-task-reference>`. Review its argument before using the displayed approval command. Command examples are printed on separate lines without sentence punctuation. Verification: 219 terminal tests passed, including short-reference, punctuation, plan/task mismatch and cross-agent scope checks.

## MAXIS workspaces and development — 5.3

```text
maxis
    - maxis workspaces
      # USAGE: List permitted workspace registrations.
    - maxis register <alias> "absolute-root" "relative/project.csproj"
      # USAGE: Master Control registers a workspace; individual tasks still need approval.
    - maxis revoke <alias>
      # USAGE: Disable workspace execution and request cancellation of running work.
    # EXAMPLE: maxis register grid "C:\Users\jack\source\repos\Grid7736" "Grid.Core/Grid.Core.csproj"
```

```text
tasks
    - tasks propose MAXIS maxis.inspect "alias|relative/file.cs" [key]
      # USAGE: Propose source inspection with a saved report.
    - tasks propose MAXIS maxis.build <alias> [key]
      # USAGE: Propose fixed dotnet build --no-restore; executes trusted project code.
    - tasks propose MAXIS maxis.test <alias> [key]
      # USAGE: Propose fixed dotnet test --no-restore with diagnostic output/TRX.
    - tasks propose MAXIS maxis.suggest "alias|relative/file.cs|old|new" [key]
      # USAGE: Stage exactly one literal replacement in a separate working copy.
    # EXAMPLE: tasks propose MAXIS maxis.build grid build-1
```

MCP equivalent: `mcp request "MAXIS: build grid"`. Inspect the resulting task, approve it individually, then use `tasks run` from the original host. Builds/tests can change outputs and run project-defined code as your Windows user; they are not OS-sandboxed. Read [MAXIS implementation](Project_GRID_MAXIS_Development_Implementation.md) for supported formats and artifact locations. NGE does not delete these external reports/working copies.

## SENTINEL — read-only security (5.4)

```text
tasks
    - tasks propose SENTINEL sentinel.defender status [request-key]
      # USAGE: Read protection flags, signature update time and prior scan end times; no new scan.
    - tasks propose SENTINEL sentinel.firewall status [request-key]
      # USAGE: Read effective firewall profile settings; no configuration changes.
    - tasks propose SENTINEL sentinel.events recent [request-key]
      # USAGE: Read up to ten newest Security-log IDs/record IDs/timestamps in the last 24 hours.
    # EXAMPLE: tasks propose SENTINEL sentinel.defender status defender-check-1
```

MCP example: `mcp request "SENTINEL: defender status; SENTINEL: firewall status; SENTINEL: events recent"`. Inspect and approve each child, then use `tasks run` once per queued task. Results identify REAL HOST SECURITY and observation UTC. Completed means the query was recorded, including possible UNAVAILABLE access; it does not certify host safety. Remediation and elevation are disabled. See [SENTINEL implementation](Project_GRID_SENTINEL_Security_Implementation.md).

## JN — Storage and files (5.5)

```text
jn
    - jn scopes
      # USAGE: List approved read-only storage scopes.
    - jn register <alias> "absolute-file-or-directory"
      # USAGE: Master Control registers an immutable alias for an existing path.
    - jn revoke <alias>
      # USAGE: Revoke a scope and request cancellation of its running work.
    - jn quota
      # USAGE: Display the logical budget and MCP expansion proposals.
    - jn quota inspect <proposal-reference>
      # USAGE: Review the request, measurement, source task and approval history.
    - jn quota approve <proposal-reference>
      # USAGE: Master Control applies a reviewed logical budget increase; no allocation or deletion.
    # EXAMPLE: jn quota
```

```text
tasks
    - tasks propose JN jn.inventory <scope-alias> [key]
      # USAGE: Save bounded metadata inventory.
    - tasks propose JN jn.duplicates <scope-alias> [key]
      # USAGE: Save possible same-size/SHA-256 duplicate groups.
    - tasks propose JN jn.cleanup <scope-alias> [key]
      # USAGE: Propose manual review of duplicate groups without deleting files.
    - tasks propose JN jn.storage status [key]
      # USAGE: Measure defined GRID-managed storage against its logical budget.
    - tasks propose JN jn.quota <whole-GB> [key]
      # USAGE: Measure storage and escalate an expansion proposal to MCP, without applying it.
    # EXAMPLE: tasks propose JN jn.storage status
```

MCP example: `mcp request "JN: inventory jntest; JN: storage status"`. Inspect and approve each task individually, then dispatch with `tasks run`. Reports contain full paths and coverage limits; duplicates are not automatically safe to delete. See [JN implementation and repeatable test](Project_GRID_JN_Storage_Implementation.md). Quota approval is a separate command; NGE does not delete external JN reports.

## VOLT and RELAY — 5.6

```text
volt
    - volt status
      # USAGE: Display fresh/unknown host readings and GRID automatic pacing.
    # EXAMPLE: volt status

relay
    - relay inbox
      # USAGE: List saved local notifications and human acknowledgement state.
    - relay inspect <notification-reference>
      # USAGE: Read a full notification and its optional explicit speech proposal.
    - relay deliveries
      # USAGE: Inspect delivery attempt outcomes; Delivered does not mean read/heard.
    - relay ack <notification-reference>
      # USAGE: Master Control explicitly acknowledges a notification.
    # EXAMPLE: relay inbox

tasks
    - tasks propose VOLT volt.status current [key]
      # USAGE: Propose a saved host-resource briefing.
    - tasks propose VOLT volt.throttle <normal|eco|auto> [key]
      # USAGE: Propose a change to GRID automatic pacing only.
    - tasks propose RELAY relay.notify "message" [key]
      # USAGE: Propose local inbox delivery; no external messaging or sound.
    - tasks propose RELAY relay.brief <full-task-guid> [key]
      # USAGE: Propose delivery of a terminal task's actual recorded result.
    - tasks propose RELAY relay.speak <full-notification-guid> [key]
      # USAGE: Explicitly propose audio playback; inspect exact text before approval.
    # EXAMPLE: tasks propose RELAY relay.notify "GRID test notification"
```

MCP example: `mcp request "VOLT: status current; RELAY: notify Phase 5.6 test"`. Approve/dispatch each child individually. Inspect results using `volt status`, `relay inbox`, `relay deliveries`, and `mcp brief`. Local speech is separate from conversation voice mute preferences and occurs only on an approved relay.speak task. See [full acceptance test](Project_GRID_VOLT_RELAY_Implementation.md).
