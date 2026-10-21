# GRID7736 - Terminal Help

**Implemented - 4.2B, 2026-10-20.** This bundled reference is available offline through `help -md`, which displays its path and Markdown in the terminal. Live `help` and Tab suggestions are capability-scoped; this full manual does not grant command access.

New worlds prompt for a username and masked password (12..256 characters), then use `login`. Existing Windows-owned worlds use `login master`, then `credentials` to migrate or rotate credentials as Master Control. Passwords are entered only at masked prompts, never as command arguments. Password login/setup refuse redirected input. Examples containing placeholders require actual values; `nge` deletes the selected world after confirmation.

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
    # EXAMPLE: register-agent ARCHON
```

```text
switch
    - switch master
      # USAGE: Select Master Control.
    - switch <agent-id|name>
      # USAGE: Select a registered agent identity.
    # EXAMPLE: switch ARCHON
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
    # EXAMPLE: inspect program ARCHON
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
    # EXAMPLE: social ARCHON
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
      # USAGE: Start a new conversation with ARCHON; Master Control can create it lazily.
    - connect <id|name>
      # USAGE: Start a directed conversation; identities must be distinct.
    # EXAMPLE: connect ARCHON
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
    # EXAMPLE: edit ARCHON occupation "Local coordinator"
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

## Planned commands - not currently supported (4.2C)

These entries document future syntax; current runtime help/Tab completion does not advertise them.

```text
<read-only-command>
    - <read-only-command> -opninwin
      # USAGE: PLANNED: open supported command data in a separate window for easier viewing and scrolling.
    # EXAMPLE: programs -opninwin

ntm
    - ntm
      # USAGE: PLANNED: open a new terminal window connected to the same running GRID, simulation and world.
    # EXAMPLE: ntm
```

Attached windows must retain authorization and share one simulation/save owner. Closing an attached window should detach it; whole-GRID shutdown behavior will be defined before implementation. See [window plan](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md#additional-windows---planned-42c-2026-10-20).

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

Use `launch.cmd` at the destination; a compatible .NET 10 runtime is required. For moves, wait for `transfer-status.txt` to report completion. Source cleanup failures retain the verified destination and report incomplete status. The transfer excludes unrelated host files, source repositories and other installed worlds. Configure local password credentials before using a different PC. See [transfer implementation](Project_GRID_Transfer_Implementation.md) for scope and recovery limits. These commands do not create attached terminals; `ntm` remains planned.

## Password requirements - next fix (2026-10-20)

**Requested; not implemented yet.** New passwords and password changes must accept **6–24 characters inclusive**. Uppercase letters, lowercase letters, numbers and symbols are allowed; no character category or mixture is mandatory. Preserve password case and exact characters; do not silently trim or truncate passwords.

Current runtime validation still uses **12–256 characters**. Update creation/change validation, masked setup prompts, error messages and help together. Preserve login compatibility for existing saved credentials, including passwords longer than 24 characters, until their owner explicitly changes the password. Keep masked input, salted hashing and authorization unchanged. Verify lengths 5/6/24/25, each allowed character category, mixed input, confirmation matching and existing-credential login before marking complete.
