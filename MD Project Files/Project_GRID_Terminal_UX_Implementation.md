# Project GRID - Terminal UX and Portable Credentials Implementation

**4.2H implemented — 2026-10-26:** Conversation labels are light blue, agent bodies yellow and user text white. Assistance now animates; Enter no longer accidentally skips a reply. **772 Release tests passed**, plus an interactive Windows-console probe. User desktop/window visual acceptance remains open. [Changes, verification and test commands](Project_GRID_Conversation_Display_Repair.md).

## Password requirements - completed follow-up (2026-10-20)

**Implemented.** New passwords and password changes accept **6–24 characters inclusive**. Uppercase letters, lowercase letters, numbers and symbols are allowed; no character category or mixture is mandatory. Preserve password case and exact characters; do not silently trim or truncate passwords.

Creation/change validation, masked setup prompts, error messages and help now use **6–24 characters**. Existing saved credentials remain verifiable up to the former 256-character limit until the owner explicitly changes the password; the input reader retains that capacity for legacy login. Masked input, salted hashing and authorization are unchanged. Control characters remain rejected. **Completion verification: 390 tests passed** (14 Core, 155 Simulation, 91 Persistence, 130 Terminal), including 13 new cases for boundaries, permitted categories, exact case/spacing, confirmation mismatch, legacy 25/256-character login after restore, and authenticated rotation to the new policy. No saved credential migration or password reset is required.


## 4.2B completion - 2026-10-20

Implemented terminal help blocks with dashed options, usage lines and examples; bundled offline `help -md`; responsive ASCII opening; first-run credential setup; authenticated migration/rotation; clear/welcome/authority/Enter after login; confirmed logout; requested palette; and capability-aware static Tab completion.

### Using an existing world

Run `login master` on the original Windows owner account, then `credentials`. Enter a username, matching password, and exactly three distinct challenge prompts with matching masked responses. Migration saves the complete credential set before access can be granted, revokes the current session, and requires `login` again. Once configured, Windows-token login cannot bypass the password. The owner identity, registered agents, roles, world, city and transcripts are preserved. Subsequent `credentials` requires Master Control and atomically rotates the password and all three challenges without replacing identities.

New interactive worlds guide setup automatically. New passwords are 6..24 characters; usernames are normalized, trimmed, 1..64 printable characters, and matched case-insensitively. Challenge prompts and responses are both 1..64 printable characters; responses normalize Unicode/case and repeated whitespace for future speech use, and cannot duplicate each other, the username, or password. Secrets never belong in command arguments. Redirected setup/password login is refused; existing unmigrated Windows-owned worlds retain Windows-token login. Redirected logout cancels rather than implying confirmation. EOF/Ctrl+C follow orderly shutdown/save; a pending confirmation never authorizes logout or destructive actions.

### Storage and security boundary

Schema v12 preserves the v11 WorldIdentities JSON row with an optional credential; older worlds retain their owner registry and explicitly migrate after owner authentication. The schema gate prevents older binaries silently dropping credential data. Credentials use the .NET PBKDF2-HMAC-SHA256 implementation, 600,000 iterations, random 16-byte salt and 32-byte hash. Verification compares derived bytes in constant time. Five failed password attempts impose a 30-second in-process cooldown; restarting can reset that cooldown. No online account or network is used.

Only salted hashes are persisted. Password input is masked, has no history/completion and is not audited. Managed password strings remain garbage-collector-owned; complete memory erasure is not claimed. Credentials protect application-level access, not against someone who can modify the database or executable. Database encryption and lost-password recovery are not implemented. There is no unauthenticated owner reset. Maintain normal world backups.

Authenticated sessions remain transient with eight-hour expiry and existing capability/edit/chat checks. Credential changes revoke sessions; permission checks deny expired sessions immediately. The welcome display refreshes at input boundaries, and live mode retains its periodic authorization check. Save failure never grants a new login; credential updates may remain in memory and require a successful later save rather than rolling back a possibly committed database write.

### Presentation and input

`help -md` reads `docs/Project_GRID_Terminal_Help.md` relative to the executable and displays its path and contents directly in the terminal. Missing/unreadable files report a local error and suggest `help`. No browser, download or external viewer is needed. Full manual visibility does not grant administrative access; runtime help/completion remain scoped.

Base output is Console Cyan (light blue), typed input White, table/divider lines Yellow, and error/denial lines Red. No-color and redirected output retain readable plain text. ASCII artwork collapses to a short welcome in narrow terminals. Existing shutdown artwork and save diagnostics remain.

The cancellable line editor supports arrows, Home/End, Backspace/Delete, horizontal scrolling, and Tab cycling at the end of an unquoted command prefix. It filters suggestions against current capabilities each time. Quoted names can be typed/edited normally, but completion inside quotes is deliberately suppressed. Only static command/options metadata is suggested; no private identifiers, paths, names or passwords. Command history is not retained. Live Q/Esc returns control to the command editor.

### Verification and limitations

368 tests passed: 14 Core, 155 Simulation, 91 Persistence, 108 Terminal. Coverage includes setup/masked-read routing, welcome and logout transitions, EOF/refused redirected secrets, hash salting and retry handling, owner-authorized migration to a different identity provider, agent restrictions, credential restart/backup recovery, help/manual command coverage, palette and completion privacy. Existing live-return, shutdown, authorization, save/recovery and simulation regressions pass. Verification builds use a separate output configuration and disposable worlds.

Automated UI tests use injected input; physical terminal appearance and keyboard behavior have not been manually verified on every terminal host. The cross-machine test restores on a different simulated Windows identity provider; it is not a physical two-PC package test.

PC GRID / Flash GRID installer choices, package-relative full storage, portable dependencies, advanced/simple 3D editions and full-product offline release validation remain planned in 7.6/8.6/8.7. Password portability alone does not make the current executable a complete flash-drive product.
