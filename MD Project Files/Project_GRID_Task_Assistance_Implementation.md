# Task assistance and scoped context — 6.3

**6.8 update — 2026-10-27:** Connected natural sentences and `talk` share the same bounded assistance routing. File-picker imports now automatically execute their analysis continuation, including attached windows; no task approvals or actions are inferred. [Completion evidence](Project_GRID_Conversation_Recovery_Implementation.md).

**6.6 integration — 2026-10-27:** Natural `talk` requests can enter scoped analysis or MCP proposal creation. Local proposals now use an explicit JSON schema and independent validation; unified requests retry invalid formatting at most once. `talk submit plan` still creates only AwaitingApproval tasks. Ordinary advice retains conversation recall; file analysis remains outside it. [Unified workflow](Project_GRID_Unified_Talk_Workflow.md).

**6.5 verification — 2026-10-27:** Advice-only operation, uploaded injection, forged approvals, offline/timeout fallback and context removal/provenance are now covered by the phase verification suite. Live bundled-model analysis passed without creating tasks or conversation recall. [Results and repeatable checks](Project_GRID_Intelligence_Verification.md).

**6.4 implemented — 2026-10-26:** Original-host `changes` drafts new/revised workspace files from captured context or local MAXIS AI, previews complete content/hashes, requires explicit approval, and applies with backups and guarded rollback. Optional verification uses separately approved MAXIS tasks. **838 Release tests passed**. Drafts are transient; recovery files persist. [Workflow, limits and demo](Project_GRID_Authorized_Editing_Implementation.md).

**6.3A implemented — 2026-10-26:** `screen options/monitoring/ask/status/stop` provides one-shot selected-monitor assistance using offline Windows OCR and local AI. Human Master Control only; no recording, saved screen context or actions. **799 Release tests passed**, including attached-window and synthetic native-OCR checks. User desktop acceptance remains manual. [Commands, limits and verification](Project_GRID_Screen_Assistance_Implementation.md).

**AI response recovery — 2026-10-26:** Truncated answers now get one bounded larger retry, and rejected responses are distinguished from an offline service. Advice and analysis prompts were refined; MCP greetings no longer receive unrelated district counts. **759 Release tests passed**, and the bundled model answered the reported HTML question successfully. [Fix, limits and regression test](Project_GRID_AI_Response_Recovery.md).


Implemented 2026-10-26. Schema v23 unchanged. GRID uses its existing local inference service, captured-context store, registered capabilities, MCP coordinator and durable approval broker. No new online dependency or execution bypass is introduced.

## Intent and workflow

`assist advice`, `assist analyze` and `assist propose` explicitly select the operator's intent; the remainder is a natural-language request. Advice and analysis return recommendations only. Propose asks local AI for structured task steps and displays a validated draft. Ordinary `talk` remains conversational: it does not silently create or execute tasks. Unclear/unsupported command modes return usage rather than guessing authorization.

```text
assist
    - assist advice <request>
      # USAGE: Ask for recommendations without changing project files or creating tasks.
    - assist analyze <request>
      # USAGE: Analyze permitted captured context for the connected agent and project.
    - assist propose <request>
      # USAGE: Generate and validate a typed MCP task draft; creates no broker tasks yet.
    - assist submit
      # USAGE: Create the reviewed draft as an MCP plan whose steps await human approval.
    - assist discard
      # USAGE: Drop the current draft without creating tasks.
    # EXAMPLE: assist propose Check current resource status using VOLT.
```

Requires authenticated human Master Control, running GRID and an open one-to-one conversation with a registered agent. Works inline and in a dedicated conversation window. Meetings cannot borrow private assistance context. MCP collaboration means validated specialist delegation through the existing broker; it is not automatic sharing of private files, recursive agent execution or multi-model deliberation.

A submitted plan can contain up to eight specialist steps. Use the original terminal to inspect, approve and dispatch them:

```text
tasks
mcp plans
tasks inspect <task-reference>
tasks approve <task-reference>
tasks run
```

Every step starts `AwaitingApproval`, with zero attempts and no approval. Submission never runs a task. Existing approved task capabilities perform their documented real actions only when separately dispatched. Drafts are session-local and are replaced by another assistance request, discarded explicitly, or lost on window close/restart. Saved plans/results retain the existing broker history and `mcp brief` reporting.

## Captured project context

Import a file with an explicit agent grant, then choose the same project in a new conversation:

```text
context add "C:\path\notes.txt" persistent grid MCP
connect MCP -inline
memory scope grid
assist analyze Summarize the captured notes and recommend next steps.
```

Use a real file path. `context add -search persistent grid MCP` opens the existing picker. `memory scope` must precede the conversation's first message. Default project is `general`.

Assistance uses only stored captures granted to the connected agent and matching the conversation project, case-insensitively. Owner-only documents and other agents/projects are excluded even though the human operator is Master Control. Temporary imports retain existing session/grant lifetime rules; persistent captures work across new conversations and restarts. Source files are never reopened by assistance. Re-import to capture newer content; use `context revoke` or `context remove` to withdraw access.

At most three captures are considered by keyword relevance and capture time. Advice/proposal modes require a keyword match; analysis may inspect the project's available captures. At most the first 700 characters per capture are supplied under a 3,200-byte total prompt budget. The output lists included filenames, hash prefixes and capture times; excerpts can be incomplete or outdated. This is captured-data analysis, not full-project semantic search. Registered workspace/storage names are supplied in bounded catalogs (first 12 enabled entries each), without granting new filesystem access. The request itself is limited to 1,200 UTF-8 bytes.

Assistance answers/drafts are **not appended to ordinary conversation memory**. This keeps a revoked file from being silently reintroduced through automatic chat recall. The captured project context can be retrieved again while its grant remains valid; the response itself is transient. Ordinary conversation recall and explicit `memory save` controls remain as documented in 6.2B. Do not treat model recommendations as verified engine facts.

## Validation and lifecycle

Proposal generation uses the shared inference queue in structured JSON mode. The response must contain exactly `steps`, with 1–8 objects containing string fields `agent`, `capability` and `argument`. Unknown/duplicate properties, malformed JSON, unsupported roles/capabilities/arguments, recursive MCP steps, semicolon argument injection and invalid registered workspace/storage targets are refused. The broker validates the entire plan atomically in preview and again at submission; there are no partially-created plans.

Drafts retain the originating session, conversation, activity generation, context revision and memory/project revision. Context changes, logout, pause, project changes and competing assistance requests cancel pending inference or invalidate the draft. Submission rechecks relevant state under the engine lock. No stale proposal can silently acquire newer authority. On a save failure, tasks may remain in memory awaiting approval; retrying the same draft uses its stable request key and cannot duplicate the plan. Resolve persistence before continuing.

Unavailable inference produces an explicit failure with no task creation. The existing `context read`, `tasks propose` and `mcp request` commands remain usable without AI. Model JSON mode is a formatting aid, not authorization: independent typed capability checks remain mandatory.

## Acceptance test

After closing an older running GRID instance, launch the updated build, log in and run:

```text
grid -start
connect MCP -inline
assist advice How should we diagnose a failed software build?
assist propose Create one VOLT volt.status current step to inspect resource status.
assist submit
tasks
```

Before `assist submit`, there should be no new task. After submission, the VOLT task should be awaiting approval. Inspect it before deciding whether to approve and run. For a multi-agent draft, ask for VOLT resource status plus SENTINEL firewall status; each step must still validate and be approved independently. AI can produce invalid proposals; refusal is expected rather than automatic repair/execution.

The original debug instance was running during this implementation. It was left alone; its locked executable was not replaced. The updated development build is `Grid.Terminal/bin/Release/net10.0/Grid.Terminal.exe`. Its normal default save is the existing per-user GRID database, unless a custom `--world` path is supplied. The separately refreshed portable package uses its own package data directory; do not assume its world is the same as the development instance.

## Completion verification

**752 tests passed, zero failures/skips**, in Release: 14 Core, 157 Simulation, 99 Persistence, 482 Terminal. The 24 added cases cover granted/owner-only/other-agent/other-project context, persisted captures across restart without reopening files, advice/analysis isolation, typed multi-step preview/submission, malformed/unsupported JSON, missing workspace rejection, revocation/remove/pause/logout/project cancellation, stale draft refusal, idempotent save-failure retry, unavailable AI, discard, identity/meeting boundaries, attached-window assistance and JSON/plain inference queue routing.

Release publish succeeded. `Test-GridPortable.ps1 -PackageRoot .\portable\GRID7736-standalone -AssistanceOnly` passed with bundled Qwen3:4b and restricted global runtime paths: actual analysis identified Cobalt42 from a captured file, and actual proposal generation returned VOLT / volt.status / current. No user world was opened and no tasks were created or executed by that probe. Earlier full smoke runs exercised prior chat/memory behavior while exposing proposal failures; those failures were fixed and the focused assistance probe then passed. This is not clean-machine/firewall isolation or a guarantee of arbitrary model-answer quality. Screen/monitor assistance remains planned in 6.3A.
