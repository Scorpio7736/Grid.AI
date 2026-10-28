# Project GRID — Conversation windows and typewriter responses (4.2E)

**6.9 update — 2026-10-27:** Authenticated attached conversation dispatchers now expose reviewed `changes` operations and conversational CRUD. All file access remains in the host; the UI process only sends requests. Drafts/approvals are per dispatcher, parent revocation blocks work, and applied backups can be recovered by ID after closing/restarting. [Verification](Project_GRID_Conversational_File_Operations.md).

**6.8 update — 2026-10-27:** Attached windows use the same raw chat parsing as the host, preserving quotes. The host awaits analysis after a picker reply and returns styled/typewriter output. Only scoped registered operations run; lifecycle/file-edit authority remains with the original host. [Tests and behavior](Project_GRID_Conversation_Recovery_Implementation.md).

**4.2H implemented — 2026-10-26:** Conversation labels are light blue, agent bodies yellow and user text white. Assistance now animates; Enter no longer accidentally skips a reply. **772 Release tests passed**, plus an interactive Windows-console probe. User desktop/window visual acceptance remains open. [Changes, verification and test commands](Project_GRID_Conversation_Display_Repair.md).

**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**Automatic conversation recall added — 2026-10-25:** Ordinary saved discussions and their relevant details now carry across conversations/restarts, alongside explicit memory. Private recall stays within the same agent/project; shared meeting recall requires the exact same group. `memory history` reviews candidates; `memory forget-history` excludes prior discussions while retaining explicit memories and transcripts. **728 tests passed**; bundled-model recall/clearing verified. [Behavior, bounds and test](Project_GRID_Automatic_Conversation_Recall.md). This supersedes earlier explicit-only recall descriptions.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


**Implemented and verified — 2026-10-21.** One-to-one conversations now open in dedicated attached windows. Multi-agent meeting integration remains planned under 6.2A; no meeting command, responder-selection model or new AI capability is claimed here. Schema remains v19.

## Commands and behavior

From an authenticated original host or ordinary `ntm` terminal:

```text
connect MCP
resume <full-conversation-id>
```

Both open a dedicated conversation window and leave the launching terminal available. Each view has its own scoped identity and active conversation. Inside it, use `talk <message>`, `transcript`, `conversations`, `disconnect`, `connect`, `resume`, `context`, `clear`, `help`, `logout` and `exit`. Connecting/resuming inside that dedicated view reuses it; it does not recursively open more windows. Administrative/task/simulation commands belong in the ordinary terminal. Context commands retain their existing identity permissions. Meetings remain unavailable.

`exit`, Ctrl+C or closing the dedicated window detaches that view without stopping the host or ending the conversation. Use `conversations` and `resume ID` to return to its retained open history. `disconnect` explicitly ends the conversation and retains its transcript. Logout still asks y/n and detaches only that child; parent logout, expiry, credential change or identity switch revokes dependent windows. Host shutdown closes all attachments through the existing shutdown path. The existing 16-window limit includes conversation windows.

The host validates the launching session and the actual attached process before starting/resuming the conversation. Failed launches and revoked pending attachments create no conversation. Invalid targets/IDs are reported in the child and close that unsuccessful attachment. Resuming checks participant identity; an unrelated agent cannot read a private transcript. An agent that actually participates retains its legitimate access.

## Typewriter display

New agent replies in dedicated windows appear with a typewriter effect. The user message appears immediately; the agent's labeled response follows at approximately 20 ms per Unicode text element. Any key except Enter skips the rest of the current animation; that key is consumed. Commands are read only after the response finishes, so turns cannot interleave within the view. Transcript/history output remains instant.

```text
typewriter
    - typewriter on
      # USAGE: Enable animation in this conversation window.
    - typewriter off
      # USAGE: Display new replies instantly in this window.
    # EXAMPLE: typewriter off
```

Preference is per window, not persisted. Set the environment variable `GRID_TYPEWRITER=off` before launching GRID to disable animation in all inherited windows for reduced-motion/accessibility needs. Redirected input/output and `TERM=dumb` also force instant text. `typewriter on` overrides the initial `GRID_TYPEWRITER=off` preference, but cannot override redirected streams or `TERM=dumb`.

The engine generates and records the complete turn before display. Animation never calls the engine, appends partial messages or executes tasks. Existing host autosave/orderly-shutdown persistence applies; animation does not add an immediate durable-save guarantee. Attachment heartbeats run during animation (every 16 displayed text elements), checking revocation and keeping the pipe alive. Cancellation stops rendering without removing or duplicating the recorded turn. This is presentation animation, not streamed model generation.

## Inline compatibility and voice

```text
connect MCP -inline
resume <full-conversation-id> -inline
```

These explicitly use the existing inline conversation path with instant text. They preserve microphone/voice controls in the original interactive host, and provide an alternative if a window cannot launch. `-inline` can precede the recipient/ID; duplicate flags are rejected. Ordinary `connect`/`resume` open windows by default in the application. Headless dispatcher/library consumers without a window hub retain inline behavior.

Attached windows still do not own microphone/speaker controls. For voice, use `connect MCP -inline` in the original host, then the existing `voice` commands. No voice device, credential or separate world writer is created in a conversation window. Local deterministic replies remain the implemented dialogue provider.

## Verification and acceptance

**572 tests passed:** 14 Core, 157 Simulation, 97 Persistence, 304 Terminal; no failures/skips. Twelve new tests cover real conversation subprocess output, separate histories, resume/disconnect/exit/logout, unrelated-agent privacy, pending-attachment revocation, launch failure, inline compatibility/completion, Unicode rendering, instant output, skip, cancellation and revocation during rendering. Existing persistence tests continue to cover saved communication state. No schema migration was needed. Release build (`dotnet build Grid.Terminal/Grid.Terminal.csproj -c Release --no-restore --verbosity minimal`) succeeded with zero warnings/errors.

Commands used: `dotnet test <project>/<project>.csproj -c Verification --no-restore --verbosity minimal`, serially for Grid.Core.Tests, Grid.Simulation.Tests, Grid.Persistence.Tests and Grid.Terminal.Tests. Terminal tests were rerun after terminal-only fixes. Automated subprocess tests use hidden redirected consoles; physical window placement, colors and animation timing require the following manual smoke test.

1. Launch the updated GRID, log in, and run `connect MCP`. Confirm a new conversation window opens while the original terminal still accepts `status`.
2. In the new window, run `talk status`. Confirm the MCP label and typewriter response. Press Escape during a reply to finish it immediately.
3. Run `typewriter off`, then `talk who are you`; confirm instant output. `transcript` must contain one user/agent pair per turn.
4. Run `exit` in the conversation window. Confirm the main GRID remains running. Use `conversations`, copy the full ID, and run `resume ID` to reopen the retained conversation.
5. Confirm parent logout revokes the view. Separately, test `connect MCP -inline` in the original host if using microphone commands.

Future 6.2A meetings should reuse the dedicated view, scoped attachment lifecycle and renderer metadata, while adding a separate participant/turn-routing contract. Do not mark meetings implemented until that module is delivered.
