# Project GRID — 6.6 Unified Talk Workflow

**2026-10-28 — longer file drafting and conversational revisions (current limits):** File generation now has its own inference profile: 4,096 output tokens initially, one 8,192-token retry only on explicit truncation, 16,384-token model context, a 300-second queue-inclusive deadline, and up to 131,072 serialized-response characters. Ordinary chat/proposal limits stay unchanged. Editable UTF-8 files now allow 64 KiB, replacing the earlier 16-KiB bound. The file-generation prompt accepts up to 24,000 UTF-8 bytes (including source/previous draft); it refuses oversized input rather than truncating it. These are bounded budgets, not a guarantee that every model request can produce an arbitrarily large file. Partial/truncated JSON is never staged.

Pending previews now accept revision requests such as `make a full web page about all the parts of the grid`, `expand...`, `revise...`, or `draft:`. The previous candidate and exact target are retained; old approval is cleared. Success replaces the draft and requires new approval. Failure keeps the previous draft available through `review` or `changes list`, unapproved. An already applied file requires an update request. Generated GRID pages receive a concise implemented-feature summary and captured tick/running/district/resident values; the prompt explicitly forbids inventing health percentages, uptime, anomaly history and guarantees. Generated claims still require review; there is no live health-score source.

Also completed from the earlier transcript: combined filename/location/description requests route to AI drafting; instruction-like content follow-ups request generation while `content:` remains literal. The file extension is provided to the model. Apply typos retain the pending draft without execution. Changes list displays full IDs on separate lines. HTML/code still requires `approve high-risk`, then `apply this change`.

Verification: **976 automated tests passed** (14 Core, 157 Simulation, 99 Persistence, 706 Terminal), including extended-profile/truncation tests, chat-limit isolation, a 20,000-character candidate, revision approval invalidation and failed-revision retention. **17 bundled-model checks passed**, including a roughly 10,000-character detailed HTML preview and fresh approval after revision. [Evidence](verification/6.9-long-file-drafting-report.md). Tests did not alter real UWGB files. The earlier blank response to a folder selection was not independently reproduced and is not claimed fixed.


**2026-10-28 — pending-step approval correction:** While selecting a folder, filename or content, approve/apply/review/undo workflow phrases now explain the missing step and retain the pending request. They are never interpreted as a new folder name or literal file contents. Folder-choice prompts explicitly ask for a number before approval; the preview remains the approval boundary. To intentionally write such a phrase as text, use `content:`. Verification: **969 tests passed** (14 Core, 157 Simulation, 99 Persistence, 699 Terminal), including three regression cases following the UWGB folder-choice → early action → select 1 → content → preview → approve → apply flow.

**2026-10-28 — dynamic folder-name lookup implemented:** File requests can now name user-created directories, for example `create a file in my Homework folder called notes.md and fill it with six random words.` GRID looks up directory names at request time under the user profile, resolved common folders, and enabled local project roots; no fixed list of user folder names or saved index is required. Nested references such as `Classes/Math`, `%USERPROFILE%` paths and `~/` paths are accepted. Exact registered aliases retain priority. A single match within the searched scope becomes an absolute destination shown in the preview. Multiple matches (or any matches from a truncated lookup) prompt for a numbered choice or full path while retaining filename/content. Lookup reads directory names only and does not authorize a write; approval/apply remain separate.

Lookup is limited to six levels below each search root, 2,000 queued/visited directories, 20 matches and a best-effort 1.5-second enumeration budget. It skips inaccessible, hidden/system child folders, links/reparse points and reserved/generated roots. Limits prevent an unbounded whole-machine scan; a name alone is not guaranteed discoverable everywhere. An explicit absolute directory/file path continues to work outside the lookup scope subject to existing access/text/path restrictions. Missing folders are not created. Tests use injected temporary search roots, not a scan or mutation of the operator's real documents.

Verification: **966 tests passed** (14 Core, 157 Simulation, 99 Persistence, 696 Terminal). Three new tests cover dynamically created folders, duplicate-name selection preserving content, nested references, truncation signaling and reserved-folder exclusion. No schema change or external dependency.


**2026-10-28 — explicit file locations expanded (supersedes Desktop-only scope):** Reviewed text file operations now accept Desktop, Documents, Downloads, Pictures, Music and Videos, plus an explicit absolute folder or full file path. Standard folders resolve on the original host; Downloads honors its Windows User Shell Folders setting. An unfamiliar folder description asks for an existing absolute path or registered alias, retaining the filename and content request. No location is inferred from AI output. File-only destinations require no `.csproj` and cannot run builds/tests. Explicit drive-root and UNC file destinations use the file boundary; project-workspace registration keeps its previous restrictions. UNC paths were not exercised against a live network share in this verification.

Examples: `create a file in my documents called words-test.md and fill it with six random words.`; `create a file in "D:\Notes" called test.md and fill it with 10 random letters`; `create file "D:\Notes\test.md" with content Hello`; `read file test.md in "D:\Notes"`. Folder paths must already exist and Windows must permit access. The existing single-file UTF-8 extension/16-KiB limits, reserved/generated paths, device-path and reparse restrictions, hash checks, approval, durable backup, and undo remain enforced. Absolute roots cannot bypass reserved path-component checks. Unavailable or ambiguous destinations are refused or clarified; GRID does not create missing folder trees or grant Windows elevation.

For the natural “and fill it with” request, counts 1–100 of random numbers, letters or words are generated locally and previewed exactly. Number words one through ten are supported. Letters are lowercase a–z; numbers are 0–999; words come from a small built-in vocabulary; repeats are allowed. These are sample data, not cryptographic randomness. Other content requests continue through reviewed AI drafting. `reate a file` prompts a correction without executing. A supplied full absolute file path works without a workspace alias; explicitly pairing it with a different workspace still cannot escape that workspace.

Verification: **963 tests passed** (14 Core, 157 Simulation, 99 Persistence, 693 Terminal). Seven added cases cover Documents/letter and word counts, full and folder paths with spaces and the word “in”, clarification and typo handling, reserved-folder refusal, and drive-root selection targeting only a temporary file. Existing user files were not moved or rewritten. Prior test counts below are historical.


**Implemented 2026-10-28 — Desktop file destination (6.9 follow-up):** Explicit Desktop requests now use the host Windows Desktop known folder, including OneDrive redirection. This file-only destination is `@desktop`; it does not require a `.csproj`, register a development workspace, or permit builds/tests. Registered workspace aliases still resolve to their own roots; an absolute Desktop path explicitly paired with a repository alias remains refused. Arbitrary destination folders are not added by this change.

Example: `create a file on my desktop called test.md and fill it with 10 random numbers.` GRID stages ten integers from 0 through 999 (duplicates possible), shows the complete preview and absolute path, then requires `approve this change` followed by `apply this change`. Requests for 1–100 random numbers use local generation without AI. Other drafting instructions use the existing optional local AI draft path. `create a file on my desktop` asks for a filename and content. `read file test.md on my desktop`, update/delete equivalents, and explicit `changes ... @desktop ...` use the same bounded UTF-8 text policy, identity/activity checks, guarded backups and rollback. Delete/code changes still require high-risk approval. Recovery syntax: `recover change <id> in @desktop`. No outside-root traversal, missing-parent creation, or implicit overwrite is allowed.

Creating an existing file now reports its absolute path and asks the user to read, rename, or explicitly update it, even when its content matches. An incomplete `creat` receives deterministic file-workflow guidance rather than AI instructions to manually create a file. Disconnect clears pending requests and receipt scope so reconnect does not consume the next request with an expired-state response. Verification: **951 tests passed** (14 Core, 157 Simulation, 99 Persistence, 681 Terminal), including six new cases using a synthetic Desktop directory. No real user Desktop files were changed by verification. No schema migration or new external dependency.


**6.9 integration — 2026-10-27:** Natural scoped CRUD and optional AI-authored complete content now work in original/attached conversations with exact reviewed approvals, recoverable deletion and explicit restart recovery. [Workflow and limits](Project_GRID_Conversational_File_Operations.md). This supersedes the earlier original-terminal-only editing restriction; private-context grants remain unchanged.

**6.8 follow-up implemented — 2026-10-27:** Direct connected chat, preserved quotes, automatic post-import analysis and retry/cancel/change handling repair the reported loop. PDF remains unsupported; 6.9–6.15 are still planned. [Implementation and evidence](Project_GRID_Conversation_Recovery_Implementation.md).

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

This opens the existing file picker. Choosing files explicitly grants temporary captured context to the named current agent in the displayed project. It does not grant other agents access or reopen files automatically. After selection, analysis starts automatically. If inference fails, `continue`, `continue to analyze`, `analyze` or `analyze the file uploaded` retries the retained goal. Inspect sources only when needed:

```text
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

Code/configuration changes require `talk approve edit <change-id> high-risk`. Approval never applies a file automatically. Existing path restrictions, stale-file checks, context revocation, backups and guarded rollback remain in force. Since 6.9, authenticated attached windows can request reviewed edits through the original host; the UI itself does not access files. Registration and advanced editing verification remain available through explicit commands.

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
