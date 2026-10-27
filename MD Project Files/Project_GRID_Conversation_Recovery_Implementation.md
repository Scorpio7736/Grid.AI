# Project GRID — 6.8 Natural Conversation and Follow-up Recovery

**Implemented 2026-10-27.** Connected original and attached terminals accept ordinary sentences as well as `talk <message>`. The reported upload/analysis loop is fixed. This does not implement the later CRUD, memory, specialist-invitation, portable-AI or PDF-extraction milestones.

## Conversation and commands

Connect first, then type normally:

```text
grid -start
connect MCP -inline
How can I horizontally center a 200px div?
Give CSS for a div with id "Test".
analyze my document and summarize its main point
select files
```

`connect MCP` opens the usual separate window; `-inline` keeps the conversation in the original terminal. Outside a connected conversation, ordinary input remains commands; `talk` can still open MCP automatically. Existing explicit command names remain reserved. Use `talk` when discussing one, for example `talk explain the status command`. `help me ...` is recognized as a sentence while `help` remains the command. Quotes, apostrophes, spacing inside the message and unmatched quote characters are chat content, not command syntax. Empty/quote-only messages and prohibited control characters remain rejected.

## Upload and follow-up behavior

- One `select files` opens the picker when the current one-to-one agent/project is known. Both terminal types show the temporary sharing scope before opening it. Selecting confirms that scoped import; cancellation grants nothing.
- Successful selection automatically starts the original analysis. There is no mandatory `continue` step. Results retain conversation colors/typewriter metadata in attached windows too. Use `talk details` for optional source information.
- Failed/cancelled/unsupported imports preserve the original goal. Use `select files` again, supply another path, or cancel. An unavailable model retains the analysis request for retry.
- Pending analysis recognizes `continue`, `continue to analyze`, `analyze`, and `analyze the file uploaded`, with or without `talk`. These are bounded aliases for the existing pending goal, not arbitrary model-generated tools.
- `cancel`, `cancel that`, `never mind` and `nevermind` abandon the pending flow. `go back`, `back`, `change target` or `choose another file` return to target selection. Cancellation does not remove context already imported or undo completed edits.
- A new unrelated sentence replaces the pending request and is handled normally. It is not interpreted as file consent or forced through the old question. The abandoned request is not a persistent task; state that request again if needed.
- Pasted paths, including spaces and optional surrounding quotes, only propose a target. GRID names the agent/project and asks for `yes` before reading and sharing temporary context. Missing files produce a retryable error. Relative paths resolve against the host working directory; use an absolute path for predictable results across windows/devices.
- File pickers and queued analysis are bound to the original identity, conversation, activity and context scope. Cancellation, replacement, logout, pause/resume or revocation cannot silently reuse an old grant or selection. No model response is executed as a command.

For example, after a rejected PDF, type `select files` once and select a supported HTML/text document. The retained analysis runs automatically. **PDF remains unsupported until 6.15; DOCX retains its existing limited extraction.** This milestone repairs recovery from that failure, not PDF parsing. Private meeting grants remain explicit; use a one-to-one conversation for file analysis. File-analysis answers are still excluded from automatic conversation memory, pending 6.10.

## CSS quality and verification

The conversational prompt now distinguishes centering a box from centering its text. `width: 100%` with automatic margins does not visibly center a narrower box. A constrained-width box with automatic side margins, or a parent flex/grid layout, is appropriate; vertical centering requires available height.

**904 Release tests passed:** 14 Core, 157 Simulation, 99 Persistence, 634 Terminal. Eighteen new 6.8 cases cover direct sentences, preserved CSS quotes, pending paraphrases, PDF/HTML recovery, missing/spaced paths, explicit grants, interruptions/cancellation, changing targets, stale sessions, and automatic execution of the analysis continuation in both the original host and an attached named-pipe client. Existing authorization, attached-process and editing regression tests remain green. Older quote-rejection expectations were updated to the new raw-chat behavior.

**Six live bundled-model checks passed.** The model produced working `width: 200px; margin-left: auto; margin-right: auto;` CSS and recovered from the rejected synthetic PDF to answer `Indigo73` from uploaded HTML without another prompt. No tasks were created/executed. [Captured report](verification/6.8-conversation-recovery-report.md). These finite model-quality checks do not guarantee all generated advice is correct. Window protocol tests are not a manual visual inspection of every console theme or native file dialog; final physical-device/UI acceptance remains in 6.15.

Repeat the opt-in model probe from a complete portable app folder:

```powershell
.\Grid.Terminal.exe --verify-conversation-recovery
```

It uses an isolated synthetic identity and temporary files, starts only the bundled local AI runtime, prints the report path and never loads the user's saved world. No new dependency or save-schema migration is introduced.
