# Project GRID — 6.6 Unified Talk Workflow

**Reported defects / planned follow-up — 2026-10-27:** Current literal `continue` handling can trap natural replies, file selection can repeat unnecessarily, and PDF import is unsupported. 6.8 must fix the supplied transcript; 6.9–6.12 extend actionable chat, memory, MCP and invitations. 6.6 completion is historical, not proof of these additions. [Mandatory pre-Phase-7 plan](Project_GRID_Pre_Phase_7_Readiness_Plan.md).

Implemented 2026-10-27. `talk <message>` is now the usual entry point for conversation, advice, permitted file analysis, screen assistance, MCP task planning and reviewed workspace editing. Explicit commands remain available.

## Start here

Log in as Master Control and start GRID. In a terminal without an active conversation, `talk` opens an inline MCP conversation automatically. Each window retains its own conversation and follow-ups.

```text
grid -start
talk How can I center a div in HTML?
talk check resources
```

The first request is ordinary remembered conversation/advice. The second produces a specialist task draft, not a running task. Review the table, then:

```text
talk submit plan
talk inspect task <full-task-id>
talk approve task <full-task-id>
talk run task <full-task-id>
```

Use the returned task ID, without angle brackets. Submission creates tasks awaiting approval. Approval queues the exact task; running is a separate request in the original terminal. `tasks run <id>` is also available and runs that exact approved task, not another queued task. The original `tasks run` still selects the next queued task. `talk discard plan` discards the transient proposal.

## Files and project context

For a new conversation, choose a project before its first saved message if needed:

```text
talk project demo
talk analyze my document
```

When no file is granted to the current agent/project, GRID asks for a selection:

```text
talk select files
```

This opens the existing file picker. Choosing files explicitly grants temporary captured context to the named current agent in the displayed project. It does not grant other agents access or reopen files automatically. After selection:

```text
talk continue
talk details
```

Analysis displays a short agent response. `talk details` shows the latest analysis's source metadata on request. Details expire when the session, conversation, activity, context or memory scope changes. File analysis is not added to conversation recall. Existing `context` commands still support persistence, removal, revocation, selection and raw inspection.

`talk select files` can also begin another import. `talk cancel` abandons a pending follow-up; it does not undo an already imported file or an applied edit.

## Screen assistance

```text
talk help me with my screen
talk 2
talk capture
```

Use a monitor number from the displayed list, not necessarily 2. GRID asks for confirmation before the first capture after selection. `capture` reads one frame using local OCR; it is not image understanding or continuous recording. A later explicit screen request with a valid selected monitor captures one new frame. `talk stop screen` clears selection. Screen content is not stored as conversation memory. Changed monitor topology and expired identities are still rejected.

## Reviewed file editing

In the original terminal:

```text
talk edit the note to explain the release steps
```

GRID asks for a registered MAXIS workspace, a relative file path, and captured context. Reply to each question using `talk <answer>`. If context is missing, `talk select files` explicitly grants selected temporary files to MAXIS under that workspace alias. Otherwise select a listed context filename or ID. The result is a complete draft preview with before/after hashes.

```text
talk review edit <change-id>
talk approve edit <change-id>
talk apply edit <change-id>
talk rollback edit <change-id>
```

Code/configuration changes require `talk approve edit <change-id> high-risk`. Approval never applies a file automatically. Existing path restrictions, stale-file checks, context revocation, backups and guarded rollback remain in force. Attached windows cannot edit; GRID directs the user to the original terminal. Registration and advanced editing verification remain available through explicit commands.

## Memory and meetings

Ordinary dialogue/advice still uses saved conversation recall, professional tone and scoped preferences. Optional aliases include:

```text
talk remember preference style: Keep answers short and direct.
talk correct memory style: Give examples when helpful.
talk show memories
talk forget memory style
talk forget conversation history
```

`remember` defaults to fact; explicit kinds are preference, fact, decision and unfinished. The colon separates the key from its text. Ordinary conversational requests to remember something still go through automatic recall when they do not use this explicit syntax.

Meetings and `talk @MAXIS <message>` retain existing participant routing and shared transcript scope. Private file analysis/editing requires a one-to-one conversation; meeting participation never implies a private file grant. Freeform meeting replies cannot execute tasks.

## Routing and authority

Routing is bounded and phrase-based. Common polite prefixes such as “please” and “could you” are accepted. Questions and unknown conversational wording use grounded dialogue; recognized vague action requests ask for a target instead of executing. Examples include “build,” “test,” “plan,” “check resources,” “analyze,” “summarize,” “edit,” and “help me with my screen.” This is not an unrestricted autonomous tool loop or a guarantee of understanding every paraphrase.

MCP uses the existing specialist catalog to produce typed plans. The local model now receives an explicit proposal JSON schema, and GRID independently validates every result. A unified proposal may retry invalid formatting once; it never executes or converts rejected output into commands. Model text, recalled history, uploads and OCR cannot provide authorization. Review each draft: schema validity does not prove relevance or factual correctness.

Schema-based local output uses the existing bundled runtime, with no new package or cloud dependency. See the [Ollama structured-output contract](https://docs.ollama.com/capabilities/structured-outputs). Explicit `assist` remains available for advanced control. If AI is unavailable, ordinary chat falls back; model-dependent planning/editing produces no executable work.

Pending follow-ups are per-terminal and transient. Identity changes clear them. Pause/resume, conversation/project changes, memory changes and context revocation invalidate them. Cancel a pending flow before starting a different request. Database schema remains v23.

## Verification and evidence

- **873 Release tests passed:** 14 Core, 157 Simulation, 99 Persistence, 603 Terminal.
- **26 new regression cases:** file consent/scope/revocation, advisory memory, screen selection/capture, task submission/approval/exact-ID dispatch, editing/rollback/path traversal, ambiguous/denied requests, offline fallback, schema propagation and bounded retry. Existing meeting and window isolation tests also pass.
- **12 live bundled-model checks passed** through the unified workflow: known world fact, injected document analysis, source separation/removal, draft-only editing, approval refusal, approved apply, exact-byte rollback, specialist plan and unapproved submission. [Captured report](verification/6.6-bundled-talk-report.md).
- Live development probes initially rejected malformed task JSON. The explicit schema fixed the observed failure; the captured successful run proposes only VOLT resource status. Draft content can still differ from the requested wording and requires review.

Run automated checks from repository PowerShell:

```powershell
dotnet test Grid7736.slnx -c Release
dotnet test Grid.Terminal.Tests/Grid.Terminal.Tests.csproj -c Release --filter "Phase=6.6"
```

From the standalone package folder, run `launch.cmd --verify-talk`. This uses a synthetic world and temporary workspace, never the user's saved world or desktop. It prints results and retains a JSON report and edit recovery files under the temporary path shown. Failed checks return a nonzero exit code. It requires the already bundled model/runtime and downloads nothing.

Native picker appearance, real user-desktop OCR and window visual acceptance remain manual checks. Finite model examples do not establish universal hallucination or prompt-injection immunity. The probe does not claim clean-machine or firewall isolation.
