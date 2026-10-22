# Project GRID - Shared Terminal Windows Implementation

## Conversation windows — 4.2E delivered (2026-10-21)

Default `connect` and `resume` now open dedicated attached conversation windows. Agent replies animate with a skippable typewriter effect; `typewriter off` and `GRID_TYPEWRITER=off` provide instant text. Exit detaches without ending the saved conversation; parent revocation closes access. Explicit `connect MCP -inline` / `resume ID -inline` retains the original-host voice workflow and instant text. **572 tests passed** (14 Core, 157 Simulation, 97 Persistence, 304 Terminal). Schema remains v19; no AI/meeting implementation is implied. Multi-agent integration remains pending in 6.2A. See [implementation and acceptance test](Project_GRID_Conversation_Windows_Implementation.md). Earlier milestone statements below remain historical.


## 4.2C completion - 2026-10-20

`ntm` opens a visible Windows terminal attached to the current GRID host. `<command> -opninwin` opens permitted data in a separate console window without replacing the original terminal output. `live -opninwin` continuously updates; other views are labeled fixed snapshots. Existing command arguments can precede or follow the flag, for example `programs -active ids -opninwin` and `resources details -opninwin`.

Snapshot support: `status`, `city`, `districts`, `programs`, `inspect`, `behaviors`, `resources`, `history`, `jobs`, `activity`, `social`, `self`, `whoami`, `conversations`, `transcript`, and `help`. Capabilities still apply. Mutating commands reject the flag; duplicate flags are refused. Queries with invalid arguments retain their normal parser diagnostics. Snapshots can be scrolled in their console and closed with Enter. Live views support Q/Esc, PageUp/PageDown for programs, and left/right arrows for event pages. Q/Esc returns to the prompt in an attached terminal or closes a dedicated live window.

### One host and separate client contexts

The original process owns the engine, clock/scheduler, telemetry/discovery workers, autosave and exclusive WorldStore lease. Attached processes are UI-only; they never create an engine, open a save or start a replacement world. Commands are executed on the original host against that one engine. Each window has its own identity session, active conversation, input editor and edit preview. Authorized edits use a shared session registry and the existing service-side authorization leases. Closing one attached window does not stop the engine, even if it is the last attachment; the original terminal remains the host.

Each attachment uses a fresh single-client local named pipe with CurrentUserOnly protection. On Windows the server verifies the connecting process ID against the process it launched. Attachment endpoints are random and expire after 60 seconds if unused. No password or saved authentication session is written to a file or passed to the client. The endpoint name identifies the local pipe; authorization remains on the host. The pipe protocol bounds frames to 2 MiB, commands to 8192 characters, and enforces I/O timeouts. A host permits at most 16 attached/pending windows. No network service or internet access is used.

### Authority and revocation

Opening a window requires an authenticated session. It inherits a scoped attachment from the opener. A Master Control attachment can choose its own registered agent role; an attachment opened by an agent cannot switch to Master Control or another agent. Parent and child contexts are separate but share an authorization lock, so mutation authorization and revocation remain serialized. A parent logout, role change, credential rotation, or eight-hour expiry revokes dependent attachments, including descendants. A child logout does not log out its parent or siblings.

Client requests recheck identity and command capabilities. Input waits send heartbeats every 500 ms; snapshots clear/disconnect on detected revocation or host loss. Live requests recheck authority each frame. Already displayed information cannot be retroactively erased from screenshots or terminal scrollback. Opening a data window before logout does not allow reading its snapshot after logout: the initial attachment is revalidated before disclosure. Password/login/setup is not performed in an attached window; reopen `ntm` from an authenticated terminal if the attachment expires.

### Shutdown and destructive actions

- Attached `exit`, EOF, Ctrl+C or window closure detaches only that window. Attached logout asks y/n; cancellation retains the session, confirmation logs out and detaches.
- Original-host `exit` or Ctrl+C disconnects and drains attached request handlers before final save. Host loss clears the client display and reports disconnection; clients never silently open another save. Forced OS termination/console closure still relies on the existing autosave/recovery behavior and cannot promise a final save.
- NGE, `tranexist`, `copexist`, login and credential changes must run in the original terminal. Attached requests report this requirement, and production attached help/completion omits these commands. Confirmed NGE/transfer disconnects attachments before deletion or final save/copy. Move cleanup retains its existing locked-file checks; if a client process has not released binaries yet, cleanup reports incomplete rather than forcing deletion.
- Closing a snapshot/live view does not change world state. Full Markdown help may describe host-only commands, but reading it does not grant permission.

### Launch and UI behavior

Launch uses the local Grid.Terminal.exe apphost with structured arguments and a visible window, resolved relative to the running assembly. Missing apphost, unsupported platform or failed launch reports an error and leaves the host usable. Window creation currently targets Windows; prepared package paths work without new PATH/registry installation. Attached entry points run before ordinary world startup/environment registration. No-color is inherited; redirected output stays readable. Help and static Tab completion include `ntm` and supported `-opninwin` options.

### Completion verification

404 tests passed: 14 Core, 155 Simulation, 91 Persistence and 144 Terminal. Fourteen new window tests cover shared ticks/state, independent chats/edit previews, authorized edits, agent restrictions, parent logout/role change/expiry/credential rotation, initial snapshot revocation, read-only snapshots/live frames, launch failure, protocol bounds, wrong-process rejection, client detach, host disconnection, and exclusive world lease/final-save ordering. A real attached UI subprocess ran commands against the host without opening a save or scheduler. Existing save/recovery, NGE/transfer, identity, live-return and simulation regressions pass. One concurrent verification build encountered a shared output-file lock; the persistence suite passed when rerun serially.

Tests use disposable state and injected launchers; the real subprocess test redirects input/output. Visible console appearance and shell window placement were not manually verified across Windows terminal hosts. Full PC/Flash edition installation/provisioning and physical cross-machine acceptance remain planned.
