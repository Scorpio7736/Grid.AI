# Project GRID — Authorized editing workflows (6.4)

**2026-10-28 — longer file drafting and conversational revisions (current limits):** File generation now has its own inference profile: 4,096 output tokens initially, one 8,192-token retry only on explicit truncation, 16,384-token model context, a 300-second queue-inclusive deadline, and up to 131,072 serialized-response characters. Ordinary chat/proposal limits stay unchanged. Editable UTF-8 files now allow 64 KiB, replacing the earlier 16-KiB bound. The file-generation prompt accepts up to 24,000 UTF-8 bytes (including source/previous draft); it refuses oversized input rather than truncating it. These are bounded budgets, not a guarantee that every model request can produce an arbitrarily large file. Partial/truncated JSON is never staged.

Pending previews now accept revision requests such as `make a full web page about all the parts of the grid`, `expand...`, `revise...`, or `draft:`. The previous candidate and exact target are retained; old approval is cleared. Success replaces the draft and requires new approval. Failure keeps the previous draft available through `review` or `changes list`, unapproved. An already applied file requires an update request. Generated GRID pages receive a concise implemented-feature summary and captured tick/running/district/resident values; the prompt explicitly forbids inventing health percentages, uptime, anomaly history and guarantees. Generated claims still require review; there is no live health-score source.

Also completed from the earlier transcript: combined filename/location/description requests route to AI drafting; instruction-like content follow-ups request generation while `content:` remains literal. The file extension is provided to the model. Apply typos retain the pending draft without execution. Changes list displays full IDs on separate lines. HTML/code still requires `approve high-risk`, then `apply this change`.

Verification: **976 automated tests passed** (14 Core, 157 Simulation, 99 Persistence, 706 Terminal), including extended-profile/truncation tests, chat-limit isolation, a 20,000-character candidate, revision approval invalidation and failed-revision retention. **17 bundled-model checks passed**, including a roughly 10,000-character detailed HTML preview and fresh approval after revision. [Evidence](verification/6.9-long-file-drafting-report.md). Tests did not alter real UWGB files. The earlier blank response to a folder selection was not independently reproduced and is not claimed fixed.


**2026-10-28 — dynamic folder-name lookup implemented:** File requests can now name user-created directories, for example `create a file in my Homework folder called notes.md and fill it with six random words.` GRID looks up directory names at request time under the user profile, resolved common folders, and enabled local project roots; no fixed list of user folder names or saved index is required. Nested references such as `Classes/Math`, `%USERPROFILE%` paths and `~/` paths are accepted. Exact registered aliases retain priority. A single match within the searched scope becomes an absolute destination shown in the preview. Multiple matches (or any matches from a truncated lookup) prompt for a numbered choice or full path while retaining filename/content. Lookup reads directory names only and does not authorize a write; approval/apply remain separate.

Lookup is limited to six levels below each search root, 2,000 queued/visited directories, 20 matches and a best-effort 1.5-second enumeration budget. It skips inaccessible, hidden/system child folders, links/reparse points and reserved/generated roots. Limits prevent an unbounded whole-machine scan; a name alone is not guaranteed discoverable everywhere. An explicit absolute directory/file path continues to work outside the lookup scope subject to existing access/text/path restrictions. Missing folders are not created. Tests use injected temporary search roots, not a scan or mutation of the operator's real documents.

Verification: **966 tests passed** (14 Core, 157 Simulation, 99 Persistence, 696 Terminal). Three new tests cover dynamically created folders, duplicate-name selection preserving content, nested references, truncation signaling and reserved-folder exclusion. No schema change or external dependency.


**2026-10-28 — explicit file locations expanded (supersedes Desktop-only scope):** Reviewed text file operations now accept Desktop, Documents, Downloads, Pictures, Music and Videos, plus an explicit absolute folder or full file path. Standard folders resolve on the original host; Downloads honors its Windows User Shell Folders setting. An unfamiliar folder description asks for an existing absolute path or registered alias, retaining the filename and content request. No location is inferred from AI output. File-only destinations require no `.csproj` and cannot run builds/tests. Explicit drive-root and UNC file destinations use the file boundary; project-workspace registration keeps its previous restrictions. UNC paths were not exercised against a live network share in this verification.

Examples: `create a file in my documents called words-test.md and fill it with six random words.`; `create a file in "D:\Notes" called test.md and fill it with 10 random letters`; `create file "D:\Notes\test.md" with content Hello`; `read file test.md in "D:\Notes"`. Folder paths must already exist and Windows must permit access. The existing single-file UTF-8 extension/16-KiB limits, reserved/generated paths, device-path and reparse restrictions, hash checks, approval, durable backup, and undo remain enforced. Absolute roots cannot bypass reserved path-component checks. Unavailable or ambiguous destinations are refused or clarified; GRID does not create missing folder trees or grant Windows elevation.

For the natural “and fill it with” request, counts 1–100 of random numbers, letters or words are generated locally and previewed exactly. Number words one through ten are supported. Letters are lowercase a–z; numbers are 0–999; words come from a small built-in vocabulary; repeats are allowed. These are sample data, not cryptographic randomness. Other content requests continue through reviewed AI drafting. `reate a file` prompts a correction without executing. A supplied full absolute file path works without a workspace alias; explicitly pairing it with a different workspace still cannot escape that workspace.

Verification: **963 tests passed** (14 Core, 157 Simulation, 99 Persistence, 693 Terminal). Seven added cases cover Documents/letter and word counts, full and folder paths with spaces and the word “in”, clarification and typo handling, reserved-folder refusal, and drive-root selection targeting only a temporary file. Existing user files were not moved or rewritten. Prior test counts below are historical.


**2026-10-28 — Desktop AI-draft correction:** The post-inference staging callback now uses the same file-destination boundary as initial validation. Previously, non-numeric drafting requests such as `create a file on my desktop called test.md and fill it with 10 random letters` reached the project-only MAXIS validator and failed with `Invalid MAXIS parameters`. Both AI-generated creation and updates now retain the explicitly selected Desktop root and require review/approval/apply. Verification: **956 tests passed** (14 Core, 157 Simulation, 99 Persistence, 686 Terminal). Five added regression cases use controlled model responses and temporary Desktop folders: the exact letters request plus update, three invalid/authority-injecting model responses, and source changes during inference. These are workflow tests, not a live-model content-quality evaluation. No real Desktop files were modified.

**Implemented 2026-10-28 — Desktop file destination (6.9 follow-up):** Explicit Desktop requests now use the host Windows Desktop known folder, including OneDrive redirection. This file-only destination is `@desktop`; it does not require a `.csproj`, register a development workspace, or permit builds/tests. Registered workspace aliases still resolve to their own roots; an absolute Desktop path explicitly paired with a repository alias remains refused. Arbitrary destination folders are not added by this change.

Example: `create a file on my desktop called test.md and fill it with 10 random numbers.` GRID stages ten integers from 0 through 999 (duplicates possible), shows the complete preview and absolute path, then requires `approve this change` followed by `apply this change`. Requests for 1–100 random numbers use local generation without AI. Other drafting instructions use the existing optional local AI draft path. `create a file on my desktop` asks for a filename and content. `read file test.md on my desktop`, update/delete equivalents, and explicit `changes ... @desktop ...` use the same bounded UTF-8 text policy, identity/activity checks, guarded backups and rollback. Delete/code changes still require high-risk approval. Recovery syntax: `recover change <id> in @desktop`. No outside-root traversal, missing-parent creation, or implicit overwrite is allowed.

Creating an existing file now reports its absolute path and asks the user to read, rename, or explicitly update it, even when its content matches. An incomplete `creat` receives deterministic file-workflow guidance rather than AI instructions to manually create a file. Disconnect clears pending requests and receipt scope so reconnect does not consume the next request with an expired-state response. Verification: **951 tests passed** (14 Core, 157 Simulation, 99 Persistence, 681 Terminal), including six new cases using a synthetic Desktop directory. No real user Desktop files were changed by verification. No schema migration or new external dependency.


**6.9 CRUD expansion implemented — 2026-10-27:** Scoped conversational create/read/update/delete, recoverable deletion, AI-authored content and original-host execution for attached requests are complete. [Commands, 941 tests and captured evidence](Project_GRID_Conversational_File_Operations.md). Earlier 6.4–6.6 completion notes remain historical.

**6.6 integration — 2026-10-27:** `talk edit a file` asks for workspace, target and explicitly granted MAXIS context, then uses the existing review/approval/apply/rollback implementation. Exact-ID `talk` aliases preserve all safeguards and original-host restrictions. [Steps and verification](Project_GRID_Unified_Talk_Workflow.md).

**6.5 live verification — 2026-10-27:** The bundled model generated a reviewable synthetic edit; generation did not write, unapproved application was refused, explicit approval allowed application, and rollback restored the original bytes. This extends the earlier 6.4 regression evidence. [Captured report and repeatable demo](Project_GRID_Intelligence_Verification.md).

Implemented 2026-10-26. The original host's `changes` command supports bounded single-file creation/replacement in a registered MAXIS workspace. Human Master Control must review and approve every change. Stage/AI generation never applies files. The existing `maxis.suggest` capability remains a staged literal suggestion; it has not gained automatic apply rights.

## Commands

```text
changes
    - changes stage <workspace> <relative-file> <context-id|name>
      # USAGE: Stage the captured context's complete text as a new or revised file. Context project must match the workspace alias. Owner-only context is permitted for this explicit non-AI operation.
    - changes suggest <workspace> <relative-file> <context-id|name> <request>
      # USAGE: Ask local MAXIS AI for the complete candidate file using explicitly MAXIS-granted context in that workspace project. The user fixes the destination; the model cannot choose paths or commands.
    - changes list
      # USAGE: List this host's draft, approved, applied and rolled-back records.
    - changes preview <id>
      # USAGE: Show complete before/after text and SHA256 hashes before application; show result and recovery location afterward.
    - changes approve <id> [high-risk]
      # USAGE: Approve the exact reviewed candidate and source hashes. Code/configuration requires the literal high-risk argument. No write occurs yet.
    - changes apply <id>
      # USAGE: Revalidate authority, scope, workspace and source hash, create durable recovery material, then apply the approved file.
    - changes rollback <id>
      # USAGE: Restore original bytes, or remove a newly created file, only if the current file still matches the applied candidate. Recovery copies remain.
    - changes discard <id>
      # USAGE: Discard an unapplied draft. Applied records retain recovery information.
    - changes verify <id> <build|test>
      # USAGE: Propose a saved MAXIS task for the workspace. It still requires tasks approve and tasks run; no process starts automatically.
    # EXAMPLE: changes list
```

Commands require a running GRID and human Master Control; attached windows request these operations through the original host (6.9). Existing MAXIS workspace registration requires a saved world, a local workspace directory and its relative `.csproj` path. The destination parent directory must already exist. There is no mkdir, rename, arbitrary/bulk deletion, shell execution, multi-file transaction or automatic patch application command. 6.9 adds explicitly reviewed, recoverable single-file deletion.

## Review and scope

`stage` uses an explicitly chosen captured document without reopening its source file. `suggest` receives only the current target text, the selected MAXIS-granted document and the user's request. Context project must equal the workspace alias. Other private files, memories and unrelated project context are excluded. AI input is limited to 3,200 UTF-8 bytes without silent truncation. Responses must be exactly one JSON object containing a single string property `content`; extra/duplicate fields, paths, malformed output and empty replacements are rejected. AI input is untrusted data, never execution permission. Suggestions can be wrong and always require full review.

The preview contains the complete old/new text with minus/plus prefixes, target, operation, risk and hashes. It is a complete before/after review, not a minimal unified patch. Control and formatting characters are visibly escaped in previews. Markdown/text is classified as document risk; supported code and configuration extensions require explicit `high-risk` approval. Every class still requires separate approval and apply commands.

An unapplied draft is bound to session generation, GRID activity and context revision. Logout, identity change, pause/resume or context changes require a new draft; revoking the workspace prevents writes. Existing sources are rechecked at approval and immediately before application, and AI generation rejects a changed destination. New-file drafts cannot overwrite a file that appeared after preview. Approval never grants unrestricted editing permission.

Files are strict UTF-8, at most 16 KiB before/after. Existing UTF-8 BOMs are retained; candidate line endings are taken from supplied/generated text, and rollback restores exact original bytes. NUL/binary data and empty replacements are refused. Allowed extensions are `.md`, `.txt`, `.cs`, `.csproj`, `.sln`, `.slnx`, `.props`, `.targets`, `.json`, `.xml`, `.yml`, `.yaml`, `.html`, `.css`, `.js`, `.ts`, `.tsx`, `.jsx`, `.py`, and `.sql`. Absolute/traversal/stream paths, linked/reparse paths, Windows device names, ambiguous trailing dots/spaces, version-control metadata, agent configuration directories and generated bin/obj paths are refused.

## Apply, recovery and verification

Recovery directories are placed under the configured MAXIS artifacts root at `workspace-edits/<change-id>`. Before source mutation, GRID writes and flushes `original.bak` for replacements, `candidate.bak` and a prepared `report.json`. The report records destination, original/candidate hashes, human approval session, context identity/source hash and operation phase. Source changes use a sibling temporary file and atomic replacement/creation. A post-write report failure is reported as a warning after the file has changed, not as a successful no-op. Backups are never automatically deleted.

Rollback refuses newer edits, missing files or redirected paths. It restores original bytes for replacements; an explicit rollback of a new-file creation removes that file only when it still matches the applied hash. It retains the candidate backup. Workspace revocation also blocks rollback through GRID; recovery files remain available for deliberate manual restoration.

Drafts and approvals are host-memory only, with at most 16 records. Logout clears unapplied drafts; applied recovery records remain available for the same host after authorized login. Restart loses the command's in-memory records and approval state, but disk backups/reports remain. After a crash/restart, use changes recover <workspace> <change-id> to validate and load applied evidence, then explicitly request rollback; approvals are never restored. Filesystem edits and the SQLite world are not one transaction. A report left at Prepared is recoverable only when actual file hashes/absence match the recorded applied state; otherwise stop for manual inspection.

Workspace checks protect against normal stale work and detected links; they are not an OS security boundary against malicious concurrent filesystem manipulation. Build/test executes project-defined code as the current OS user using existing fixed MAXIS commands, timeouts, logs and --no-restore behavior. It is separately approved and does not provide network isolation. A successful test process does not prove tests were discovered; inspect task results/logs. Failed verification does not automatically roll back source or remove project-generated outputs.

## Completion verification

838 Release tests passed: 14 Core, 157 Simulation, 99 Persistence and 568 Terminal. The 39 new workflow cases exercise create/replace/rollback, byte-exact BOM/line-ending restoration, high-risk acknowledgement, path and symlink refusal, changed-source guards, replay refusal, backup-write failure, permission/context/workspace revocation, strict AI response parsing, scope cancellation, context isolation, safe previews and separately approved build/test handoff. Actual local dotnet build/test targets were invoked in temporary workspaces; those tiny targets verify process execution, not application test discovery. AI draft tests use controlled model responses; live model output quality is not claimed. Schema remains v23 and no new runtime dependency is introduced.

## Safe demo

Restart GRID and log in to the original host. Use a new workspace alias, or an existing registered alias with matching context project. This example uses the checked-in tiny candidate file and creates a separate Markdown file in the repository.

```text
grid -start
maxis register edits-demo "C:\Users\jack\source\repos\Scorpio7736\Grid7736" "Grid.Core/Grid.Core.csproj"
context add "C:\Users\jack\source\repos\Scorpio7736\Grid7736\MD Project Files\examples\6.4-candidate.md" temporary edits-demo MAXIS
changes stage edits-demo GRID-6.4-demo.md 6.4-candidate.md
changes preview <returned-change-id>
changes approve <returned-change-id>
changes apply <returned-change-id>
changes rollback <returned-change-id>
```

If `GRID-6.4-demo.md` already exists, choose another unused filename. To test AI generation, use `changes suggest edits-demo GRID-6.4-ai-demo.md 6.4-candidate.md Rewrite this as two short bullet points`, then review its returned draft before approving. To verify an applied edit, use `changes verify <id> build`, then separately approve and run the returned task ID. No build/test is required for the Markdown demo itself.
