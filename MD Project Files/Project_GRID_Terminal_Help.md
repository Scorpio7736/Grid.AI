# GRID7736 - Terminal Help

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
      # USAGE: Start a new conversation with MCP; Master Control can create it lazily.
    - connect <id|name>
      # USAGE: Start a directed conversation; identities must be distinct.
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
      # USAGE: Select an open permitted conversation; copy its ID from conversations.
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

## Move/copy commands (4.2D)

Master Admin means the existing Master Control role. Both commands require an interactive persistent session and close it after confirmation and final save. Choose a new absolute destination directory with an existing parent.

```text
tranexist
    - tranexist "location"
      # USAGE: Move the GRID application and selected save inventory after preview and y/n confirmation; source cleanup runs after exit.
    # EXAMPLE: tranexist "E:\GRID7736"

copexist
    - copexist "location"
      # USAGE: Copy the GRID application and selected save inventory after preview and y/n confirmation; keep the original.
    # EXAMPLE: copexist "E:\GRID7736 Copy"
```

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
