# Project GRID — Conversation display repair (4.2H)

Implemented 2026-10-26. This is a terminal presentation change; schema v23, stored conversations, recall, permissions and task approval remain unchanged. The unified `talk` workflow is still deferred to 6.6.

## Changes

- Speaker labels and chat headings use light blue (the existing Cyan palette); agent bodies use yellow; displayed user messages and keyboard input use white. Real command failures and AI rejection/unavailability notices use red. An agent discussing an error keeps its yellow message color.
- Inline chat, dedicated conversation windows, attached ordinary terminals, meetings and transcripts share explicit presentation spans. Labels are taken from the actual participants, never guessed from text inside a message. The spans travel through the authenticated window protocol and are not saved into conversation content.
- New `talk` and `assist advice/analyze/propose` responses animate. Assistance previously returned plain text without animation metadata: its answers could never animate even when typewriter was enabled. Assistance headings, body and source/review notes now have blank-line separation.
- Agent text uses approximately 20 ms per Unicode text element. Labels, user messages and assistance footers appear immediately. Existing blank lines, code fences and indentation are retained; the console wraps long lines at its width. This is plain terminal text, not Markdown syntax highlighting or model token streaming.
- A leftover Enter no longer skips a reply. Press Escape, Space or another non-Enter key to finish it immediately; the skip key is consumed. Wait for the prompt before typing the next command.
- `typewriter off` displays replies instantly while preserving colors. `typewriter on` enables animation and overrides an initial `GRID_TYPEWRITER=off` preference in that terminal. Preferences remain per terminal and are not persisted. Redirected input/output and `TERM=dumb` intentionally remain instant. `--no-color`, `NO_COLOR`, redirected output and `TERM=dumb` suppress colors; no-color by itself does not disable animation.
- Transcript and detailed-transcript output stays instant with the same speaker/body palette. Detailed evidence is retained. Animation validates the session and existing attached-window heartbeats continue; cancellation never appends or duplicates messages.

## Completion verification

**772 Release tests passed:** 14 Core, 157 Simulation, 99 Persistence and 502 Terminal. The terminal suite includes 13 new renderer/palette cases; existing assistance, meeting and named-pipe window tests now check presentation and animation metadata. Coverage includes Unicode, code indentation, text that resembles a speaker label, words such as “error” inside dialogue, skip/instant output, invalid spans, no-color output, transcript behavior, cancellation and revocation.

The normal Release executable was also exercised through a real interactive Windows ConPTY console with `--ephemeral`, temporary in-memory credentials and bundled local AI. The probe verified default inline animation, light-blue labels, white input/user messages, incremental yellow agent text despite an extra queued Enter, a yellow animated assistance answer with immediate blue source notes, and `typewriter off` instant output. It exited normally without saving a world. The probe shell inherited `TERM=dumb` and `NO_COLOR`; those settings were overridden only in the probe process. No user preference was changed to enable color or animation.

One CSS-advice probe reached the model's response limit after its existing bounded retry; a shorter advice request succeeded. The refusal is not a renderer failure, and this repair does not promise every AI request succeeds. AI refusal/unavailability text is now explicitly tested as red.

**Still awaiting user visual acceptance:** the user's own desktop terminal and detached meeting/conversation windows. Real named-pipe tests verify the window transport; the native console probe verifies rendering. These are not a claim that the user's desktop window was visually inspected. Earlier completion notes remain historical.

## Try it after restarting GRID

```text
grid -start
connect mcp -inline
typewriter on
talk Hello. Tell me briefly what you help with.
assist advice Reply with one short sentence about using status.
transcript
typewriter off
talk Hello again.
disconnect
agent meetings MCP MAXIS -inline
typewriter on
talk @MAXIS Say ready in one sentence.
disconnect
connect mcp
```

In the new conversation window, run `talk hello`, then `transcript`. For the meeting window, disconnect and run `agent meetings MCP MAXIS` from the host. Check labels, body colors, blank lines and visible typing; press Escape during a longer reply to verify skip. Closing an attached view only detaches it; the original host still owns the world. Restart both host and attached views so all use the updated executable.
