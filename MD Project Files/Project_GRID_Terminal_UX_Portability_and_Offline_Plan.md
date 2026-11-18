# Project Grid.AI - Terminal UX, Portability and Offline Operation

**Cross-platform roadmap update — 2026-11-17.** Portable operation will use separate Windows, Linux and Android builds over one governed data/manifest contract. Android receives a dedicated host and native folder-access adapter; it is not treated as generic Linux and may stage executable assets in app-private storage while retaining approved data on removable media. The former Phase 8 packaging items are preserved in future plans and relevant requirements are now driven by active Phase 7. [Cross-platform and Android plan](Project_GRID_Cross_Platform_and_Android_Plan.md).

**Planning update — 2026-11-04.** Flash drives are the primary planned deployment, with installers and local-only PC copies supported. Both Flash Grid.AI and PC copies target 128 GB decimal of managed installation storage. Preserve edition-specific rendering plans while introducing common capacity/free-space/reserve accounting and configuration migration in 6.27. The full GRID-AI/GRID-Agent integration and second-device acceptance must pass before Phase 7. [Full plan and acceptance gates](Project_GRID_AI_and_Agent_Platform_Plan.md).

**6.7 completed — 2026-10-27:** The supplied banner, five-second login reveal, skip/static modes and new palette are implemented and console/test-verified. This supersedes earlier login Enter gating and red-error colors. Portability repair and other 6.8–6.15 requirements remain planned. [Evidence](Project_GRID_Login_Animation_and_Colors.md).

**Superseding planned requirements — 2026-10-27:** Before Phase 7, replace the login Enter gate with a five-second ASCII animation; use yellow terminal messages including errors, white user text and light-blue prompts/labels. Prove actual copy/transfer AI operation offline on another device and include PDF/DOCX assets. [Scope and acceptance](Project_GRID_Pre_Phase_7_Readiness_Plan.md).

**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


## 4.2G completed — 2026-10-23

`grid -start` / `grid -pause` replace the old start/pause commands, which now give migration guidance without changing state. `tasks` and `tasks list` show all authorized retained history, including labeled archives. `tasks -completed` shows Completed only; `tasks -list` excludes Completed and Denied (cancelled/failed and archived records remain visible). All views accept one agent filter. Master Control can use `tasks deny <reference> "reason"` on an AwaitingApproval task; denial is terminal, separately audited and saved in schema v21. Existing v20 history upgrades without rewriting cancellations as denials.

Transfers accept drive roots as containers: `grid -copyto "E:\"` targets `E:\GRID7736`. `grid -copyto -select` / `grid -transferto -select` select a container and create its new Grid.AI child. Existing targets are refused; no merging or overwriting. Original-host Master Control, interactive preview/y/n, saved-world verification and move cleanup safeguards remain. No files move when selection is cancelled, authority changes or media is unavailable.

**Verification:** 658 tests passed (14 Core, 157 Simulation, 98 Persistence, 389 Terminal), including ten new regression cases; zero skipped. Native dialog appearance and physical flash-drive removal/cross-PC acceptance remain manual release checks. The picker uses Windows' built-in local folder dialog, with no new external package/service. The saved city and user files were not relocated by verification. Agent lifecycle coupling (5.9), startup test gate (8.8), changed-device handling (8.9), meetings (6.2A), and full portable/disaster acceptance (8.6) remain planned. See [implementation and test commands](Project_GRID_Terminal_Controls_Implementation.md).


## Fresh-clone runtime repair — 2026-10-23

Implemented tracked runtime preparation, self-contained Windows package building and isolated verification. Grid.AI now owns a bundled AI server/model cache and uses bundled MAXIS tooling; no personal Ollama service is required. Typewriter now works with fallback replies and inline/attached chat, with explicit on/off overriding the startup preference. **648 tests passed**, plus a real portable AI/chat probe with global runtime paths removed. Full two-PC/voice/device/installer acceptance remains planned. See [setup, package layout, evidence and limits](Project_GRID_Fresh_Clone_and_Portable_Runtime.md). Earlier global-runtime instructions below are historical.


## Conversation windows — 4.2E delivered (2026-10-21)

Default `connect` and `resume` now open dedicated attached conversation windows. Agent replies animate with a skippable typewriter effect; `typewriter off` and `GRID_TYPEWRITER=off` provide instant text. Exit detaches without ending the saved conversation; parent revocation closes access. Explicit `connect MCP -inline` / `resume ID -inline` retains the original-host voice workflow and instant text. **572 tests passed** (14 Core, 157 Simulation, 97 Persistence, 304 Terminal). Schema remains v19; no AI/meeting implementation is implied. Multi-agent integration remains pending in 6.2A. See [implementation and acceptance test](Project_GRID_Conversation_Windows_Implementation.md). Earlier milestone statements below remain historical.


## Password requirements - completed follow-up (2026-10-20)

**Implemented.** New passwords and password changes accept **6–24 characters inclusive**. Uppercase letters, lowercase letters, numbers and symbols are allowed; no character category or mixture is mandatory. Preserve password case and exact characters; do not silently trim or truncate passwords.

Creation/change validation, masked setup prompts, error messages and help now use **6–24 characters**. Existing saved credentials remain verifiable up to the former 256-character limit until the owner explicitly changes the password; the input reader retains that capacity for legacy login. Masked input, salted hashing and authorization are unchanged. Control characters remain rejected. **Completion verification: 390 tests passed** (14 Core, 155 Simulation, 91 Persistence, 130 Terminal), including 13 new cases for boundaries, permitted categories, exact case/spacing, confirmation mismatch, legacy 25/256-character login after restore, and authenticated rotation to the new policy. No saved credential migration or password reset is required.


**4.2B implemented - 2026-10-20; packaging/rendering/offline release gates remain planned.**

See [4.2B implementation and verification](Project_GRID_Terminal_UX_Implementation.md) for current behavior and limits. The requirements below retain the design specification; completion applies to terminal UX and local credentials only.

This records the user's additions after 4.2A. Existing implementation/completion history remains valid for its original scope. Terminal UX and local credentials are implemented; PC/Flash packaging and full product offline validation are not. See [Terminal Help](Project_GRID_Terminal_Help.md) for the new reference artifact and the [master checklist](Project_GRID_Master_Development_Checklist.md) for unchecked implementation work.

## GRID-REQ-006 - Terminal presentation and interaction

### Help format and Markdown help

For each command, show its options/flags/switches as an indented dashed list below the command. Each option includes a `# USAGE:` description; each command block ends with an example. Preserve actual parser syntax: do not turn bare options such as `ids` or `details` into invented dash flags.

```text
programs
    - programs -active
      # USAGE: List freshly observed Running applications.
    - programs -possible
      # USAGE: List current and previously known application candidates.
    - programs -sleeping
      # USAGE: List freshly observed On call or Sleeping applications.
    - programs ids
      # USAGE: Show full identifiers; can accompany one availability filter.
    # EXAMPLE: programs -active ids
```

Create and ship a local Markdown help reference, opened through **help -md**. The reference is supplied in this documentation update; the flag displays the bundled document in the terminal. Resolve the reference from the application/package root, not a hard-coded repository path or online URL. Use an offline-capable local viewer; if unavailable, render/read the Markdown in the terminal and show its path. Never require a download or browser network request just to read help. The full packaged manual may describe admin commands, but live help and Tab suggestions must remain scoped to active capabilities. Help never grants permission to execute a documented command. Keep registered usage metadata, console help and Markdown synchronized; validate examples in tests.

### Opening graphic and login flow

Add a Grid.AI opening graphic with the project's established digital/Tron-inspired visual direction. Supply a text/ASCII version that works offline without fonts/assets/network dependencies; adapt to narrow terminals. Opening/welcome artwork is separate from the existing shutdown artwork. The implemented text fallback:

```text
+======================================================+
|                                                      |
|                    G R I D 7 7 3 6                   |
|                    ENTER THE GRID                    |
|                                                      |
+======================================================+
```

Startup displays the graphic and welcome/login screen. When no local username/password credential configuration exists for a new or explicitly migrated portable world, guide the user through username creation, password creation and password confirmation. If such a local-credential option/provider has not yet been implemented, implement it as part of this work. Never treat absent credential files in an already protected world as permission to reset its owner automatically.

On successful login: clear the terminal, display **Welcome, <username>**, show active identity and authority level (Master Control or the selected agent role), then display **Press Enter to continue to terminal**. Enter opens the normal prompt. The screen must not imply OS elevation. Failed login remains on the login screen without revealing protected state.

On logout: ask **Log out? [y/N]**; y confirms, n/blank cancels, and invalid input reprompts. EOF/Ctrl+C cancels the confirmation and follows the existing orderly shutdown rules. A confirmed logout revokes the session, clears pending previews/chat/completion context, clears the terminal, and displays the opening welcome/login screen again. Cancellation preserves identity and context. Expiry/security revocation must still revoke immediately without waiting for a yes/no answer; return to the welcome/login screen.

For redirected input/noninteractive sessions, do not hang on Enter/confirmation or echo passwords. Specify an explicit secure noninteractive authentication policy during implementation; default to a safe refusal for credential setup, while existing graceful EOF handling and plain-text diagnostics remain. Any migration must preserve owner/agent IDs, transcripts, roles and edit authorization semantics.

### Colors and Tab completion

| Element | Required color |
| --- | --- |
| Base/output text | Light blue |
| User-typed input | White |
| Tables and divider lines | Yellow |
| Errors and access denials | Red |

Retain --no-color and redirected-output/plain-text fallbacks. Restore the prior console colors on exit and after colored spans. Do not convey errors or authority solely through color. This is a complete palette requirement; existing cyan/default output is not proof that input, tables and errors already satisfy it.

Tab completion: type a prefix and press Tab to complete a permitted command, option, flag or switch; offer/cycle matching choices where ambiguous. Support quoting, cursor editing and names containing spaces. Suggestions must use the current command registry/capabilities, refresh after login/switch, and clear on logout. Do not suggest restricted commands, unrelated transcript IDs, passwords, or inaccessible paths. Secret/password input disables completion/history. Completion must not execute commands. Keep a line-input fallback when interactive key handling is unavailable. Verify that live view still returns with Q/Esc and the line editor regains input ownership.

## Portable credentials - extension of GRID-REQ-003

Original 4.2A login binds to a Windows SID and is unsuitable as the sole authentication mechanism for a world carried between PCs. Plan a local username/password provider with explicit first-run setup, maintained salted password hashing/KDF, masked input, bounded retry handling and no plaintext password logging/storage. Passwords must not appear in command-line arguments, environment variables, shell history or completion buffers. Exact library/KDF parameters require implementation-time selection and verification.

The portable identity store travels with the package. Preserve the stable owner ID, registered agent identities and role policy; credentials authenticate the human, who may then select authorized agents. Agent selection does not inherit Master Control permissions. Provide an authenticated migration from a SID-bound world; copying to a different PC alone must not permit ownership takeover. Define offline recovery and credential rotation without an online account. Do not rely exclusively on the old machine's SID, user profile or OS-bound encryption key. Storage encryption, if added, needs portable key/recovery handling. 4.2B implements setup, authenticated rotation/migration and portable hash verification. Lost-password recovery remains a future design; there is no unauthenticated reset.

## GRID-REQ-007 - Self-contained portable Grid.AI

Goal: keep Grid.AI in one movable folder/package on a flash drive, plug into another supported PC, launch locally and continue the same saved world. Initial compatibility target remains supported Windows/CPU architectures; a flash drive does not make binaries universal across operating systems. Missing host permissions/hardware must degrade clearly rather than require administration or silently replace the world.

### Installation choice: PC Grid.AI or Flash Grid.AI

**User addition - 2026-10-20; planned, not implemented.** Installation must explicitly offer two editions before provisioning files:

| Choice | Intended use | Storage and host integration | 3D rendering |
| --- | --- | --- | --- |
| **PC Grid.AI - permanent machine installation** | Remain installed on one supported PC | User-selected permanent location with clearly disclosed data paths; optional shortcuts/startup integration | Advanced rendering options, scaled to the host GPU/resources |
| **Flash Grid.AI - mobile flash-drive installation** | Carry the prepared package between supported PCs | User-selected removable-drive folder; package-relative data/config/models/docs/cache; no required host install or persistent PATH/registry changes | Very simple, lightweight 3D rendering by default |

The installer must explain these differences, ask for the destination, check compatibility/free space/write access, and save the edition choice in configuration. Do not infer the edition merely from a drive letter or silently change it on relocation. Installation here includes an explicit portable provisioning flow; Flash Grid.AI must launch directly from its folder after preparation.

Flash Grid.AI's viewer should use simple geometry, small textures/material sets, limited view distance and conservative effects/shadows, prioritizing readability and low resource/storage requirements. PC Grid.AI may offer richer geometry/materials, lighting, shadows, atmosphere and longer view distances where supported. Exact asset budgets and performance targets require implementation-time profiling; no current 3D implementation or measured performance is claimed. Both retain the existing city/art direction, with different detail levels. Flash Grid.AI rendering runs on the host PC; the flash drive stores the portable package.

Both editions share the authoritative simulation, identity/permission model, offline task requirements and compatible world/save format. Rendering quality must not change simulation rules, agent authority, tasks, historical state or the saved city. Prefer shared source/services with edition-specific packaging/assets and viewer presets. Advanced PC rendering is optional and must have a simpler fallback when hardware is insufficient; a terminal-only fallback remains usable if 3D is unavailable.

Both editions must perform every supported runtime task offline after provisioning. Provide a future explicit export/import or edition-migration path that carries the existing world and credentials without duplicating live writers or silently regenerating state. PC-to-Flash migration must include the simple viewer assets and required local dependencies; Flash-to-PC may add advanced assets through the explicit installation exception. Credential/ownership migration remains authenticated and must not be bypassed by changing an edition flag.

Proposed package layout (planned, names subject to implementation):

```text
GRID7736/
    launch/
    app/
    data/          # world, identities, transcripts, history, backups, recovery markers
    config/
    models/
    assets/
    docs/
    logs/
    cache/
    temp/
```

All GRID-managed persistent data and working/cache files must use the package root or an explicitly chosen managed subdirectory. Ship required runtime/dependencies, local models, tokenizers, voice assets and documentation, with version/license manifests. Resolve paths relative to the package; handle changed drive letters, paths with spaces and relocation without broken references. Avoid default reliance on %LOCALAPPDATA%, machine-specific PATH/env variables, registry installation or absolute model paths. Portable launch must work directly without mutating the host's PATH/registry. Existing installed-mode behavior can remain a separate explicit mode; it must not silently redirect a portable session to an old host-local world.

User-approved work on files outside the package is distinct from GRID-managed storage and must use explicit scoped paths/permissions. External host applications are not bundled automatically. Rediscover the current host's applications and telemetry; unavailable dependencies/resources remain unavailable/unknown. Preserve the saved city, world identity, conversations and creation-hardware provenance when moving PCs. Do not regenerate the city just because the host motherboard/CPU/GPU changed. Host adaptation is an explicit separate design decision.

Handle flash-drive space limits, read-only media, wear-conscious write batching, exclusive world leases, interrupted writes, drive removal and backup recovery. Provide a safe shutdown/eject-ready state after workers stop and writes flush. Recovery must not regenerate or claim a save succeeded after an interrupted write. Keep default backups with the package and optionally support explicit external backups. Validate relocation across drive letters and two supported machines, missing peripherals, Unicode/spaced paths and no host-local data leakage. Do not claim portability based only on a relocated executable.

## GRID-REQ-008 - Offline operation is mandatory

All Grid.AI runtime tasks must work without internet access, online accounts, subscriptions, cloud inference, remote authentication or network-required license checks. The sole network exception is explicit user-requested installation/provisioning of dependencies, models or updates; no runtime task may silently download missing files. After installation, the full intended task path must be offline-capable, including login/recovery, simulation, persistence, communications, local AI, voice, local-context work, task execution and help/viewers.

Bundle/pre-provision all required runtime assets and keep models/services local. Missing assets produce a clear local unavailable result and an explicit installation path, never a silent cloud fallback or background download. Any optional online integration described in older plans is not part of the accepted runtime requirement; redesign it as a local/offline equivalent or explicitly defer/remove it. Internet browsing, remote messaging and remote-data retrieval cannot be counted as offline-compliant Grid.AI tasks. Do not misrepresent stale/local data as current external facts.

Separate installer network permissions from normal runtime capabilities. Define how local on-machine IPC/model endpoints work with no external networking; that does not authorize LAN/cloud dependency. Verify with external networking disabled/blocked and DNS unavailable, including cold startup, login, model/voice availability, conversations, tasks, restart and recovery. Test the prepared portable package on another supported PC with internet disabled. Record missing features honestly: the current deterministic core works locally, but future AI/voice/host tasks are not yet implemented or verified as a complete offline product.

## Implementation order and acceptance

1. Implement terminal help layout/help -md, opening/welcome screens, palette and Tab editor while preserving command authorization.
2. Implement credential setup/provider and authenticated migration, then integrate login/logout UI and test isolation/confirmation.
3. Implement the PC Grid.AI / Flash Grid.AI installation choice, edition-specific viewer profiles, package-relative portable storage/dependency provisioning and launch without host integration side effects.
4. Validate relocation, recovery and all supported runtime tasks offline; update release gates with measured evidence.

Track completed terminal work in 4.2B, active cross-platform package/offline acceptance in Phase 7, and deferred edition hardening in future plans B.6–B.7.

## Optional remote terminal extension — planned 2026-11-17

Remote attachment through a user-managed private network is optional connected functionality, not an exception to mandatory local/offline operation. The planned Tailscale transport must attach a bounded client to the one authoritative host, remain disabled by default, avoid public Funnel/broad LAN exposure, require GRID authentication beyond tailnet membership and discard active/machine-bound sessions during copy or transfer. Local terminal, AI and task workflows must continue when Tailscale or all networking is unavailable. See [Remote Terminal Access](Project_GRID_Remote_Terminal_Access.md) and checklist 6.36A for the authoritative plan and acceptance gates.

## Additional windows - planned 4.2C (2026-10-20)

**Implemented in 4.2C.** The original requirements below are fulfilled by local scoped attachments; see [implementation and completion evidence](Project_GRID_Shared_Terminal_Windows_Implementation.md). The requested spelling `-opninwin` is retained.

- `<command> -opninwin` opens supported data output in a separate window so the user can inspect and scroll it without displacing the main terminal. Examples: `programs -opninwin`, `status -opninwin`, `resources details -opninwin`. Default data windows show a labeled snapshot; if `live -opninwin` is supported, label it as updating and show connection/freshness status. Define the supported read-only command list during implementation and reject unsupported combinations clearly. Opening a data view must not execute a mutating command or grant extra permissions.
- `ntm` opens a new terminal window attached to the same running Grid.AI instance and world. It is another command interface to the existing simulation, not a second simulation process independently loading/writing the save. Commands and state changes are shared through the authoritative host; each window has its own input, view, active conversation and pending edit context.

Use local, offline communication between windows and a single authoritative simulation/save owner. Do not bypass the existing exclusive world lease by launching a second ordinary writer against the same database. New windows must authenticate or receive a securely scoped attachment; never pass passwords in launch arguments, environment variables or files. Revalidate capabilities, expiry and revocation for each request; opening a window must not promote an agent to Master Control.

Closing a data window or attached terminal detaches that view without stopping the shared GRID. The original terminal owns the host: attached exit/closure detaches, and original-host exit/Ctrl+C stops attachments before final save; preserve orderly save/flush and NGE authorization/confirmation. All windows must show disconnection if the host stops or NGE removes the world, and must not silently create/reload another world. Original-host `exit` retains its save/shutdown behavior; attached `exit` only detaches.

Provide a clear error and retain the current terminal if window creation or attachment fails. Keep paths portable and compatible with the planned PC GRID/Flash Grid.AI editions; no internet service is required. Verify shared state, one simulation clock/writer, per-window authorization/context, logout/expiry, window closure, host loss and save recovery before marking 4.2C complete.

## Move or copy Grid.AI - planned 4.2D (2026-10-20)

**Implementation update:** current-host transfers are implemented with 377 passing tests. Read [actual scope and limits](Project_GRID_Transfer_Implementation.md). The following original requirements retain the broader target, including PC/Flash integration; attached-window shutdown is now integrated; they are not all complete.

User-requested Master Admin commands (the existing Master Control authority):

| Command | Planned result |
| --- | --- |
| `grid -transferto "location"` | Move the complete Grid.AI installation and its managed files to the specified destination; remove the original only after the destination is verified and the move can finish safely. |
| `grid -copyto "location"` | Copy the complete Grid.AI installation and its managed files to the specified destination; retain the original. |

Both commands now implement the current-host scope. Require authenticated Master Control at dispatch and immediately before executing the operation; agent sessions must be denied. Parse the quoted destination as a filesystem path, including spaces, without shell interpolation. Show resolved source/destination, file scope, estimated size and move/copy behavior, then request explicit y/n confirmation. Cancellation changes no files.

“All files” means the complete GRID-managed installation and state: application/launcher/runtime dependencies, world save, identity and credential hashes, transcripts/history, backups/recovery metadata, configuration, local models, assets, documentation, logs and caches. Inventory currently scattered managed files explicitly rather than assuming everything already lives under one directory. Do not move/copy unrelated host programs, user documents, or external task-context files merely because Grid.AI references them; report external dependencies and references separately. Do not transfer active authentication sessions, plaintext passwords, live lock handles or machine-specific attachment endpoints as reusable state. A copied/moved world keeps its saved identity and city; opening it requires authentication and must not regenerate it for new hardware.

Preflight source/destination boundaries, write access, free space, compatibility, symlinks/junctions and existing destination contents. Reject identical, nested or overlapping source/destination paths and unsafe filesystem roots; do not overwrite unrelated destination files. Use a fresh destination directory or a clearly defined safe empty destination. Operations must work offline and support both planned PC Grid.AI and Flash Grid.AI layouts.

Coordinate all attached windows through the one authoritative host. Pause mutations and flush/checkpoint saves before taking a consistent transfer snapshot. Use staging and a file manifest with integrity verification; retain the source if copying, validation or handoff fails. Never claim completion for a partial copy. A cross-volume move is a verified copy followed by source cleanup, not an assumed atomic rename. Account for locked running binaries through an orderly shutdown/helper or next-launch handoff; do not delete the running installation prematurely. Design interrupted-transfer recovery and report any remaining cleanup explicitly.

For `grid -transferto`, release old leases/endpoints, update GRID-managed launch/configuration references as appropriate for the selected edition, and require reconnect/restart at the verified destination. For `grid -copyto`, the original remains usable; the copy must use independent runtime endpoints/leases and cannot attach to the original just because the saved world ID matches. Do not allow two writers on the same physical save. Define and test the copy's independent startup and local host rediscovery while preserving historical world data. Flash Grid.AI must not introduce persistent host PATH/registry integration.

Implement alongside package/storage relocation work in 8.6. Verify permission denial, confirmation cancellation, quoted paths, consistent saves, full manifests, low space, interrupted transfer, locked files, source retention on failure, restart/login, recovery and attached-window behavior before marking complete.
