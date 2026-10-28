# Project GRID — 6.9 Conversational File Operations

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


**2026-10-28 — Desktop AI-draft correction:** The post-inference staging callback now uses the same file-destination boundary as initial validation. Previously, non-numeric drafting requests such as `create a file on my desktop called test.md and fill it with 10 random letters` reached the project-only MAXIS validator and failed with `Invalid MAXIS parameters`. Both AI-generated creation and updates now retain the explicitly selected Desktop root and require review/approval/apply. Verification: **956 tests passed** (14 Core, 157 Simulation, 99 Persistence, 686 Terminal). Five added regression cases use controlled model responses and temporary Desktop folders: the exact letters request plus update, three invalid/authority-injecting model responses, and source changes during inference. These are workflow tests, not a live-model content-quality evaluation. No real Desktop files were modified.

**Implemented 2026-10-28 — Desktop file destination (6.9 follow-up):** Explicit Desktop requests now use the host Windows Desktop known folder, including OneDrive redirection. This file-only destination is `@desktop`; it does not require a `.csproj`, register a development workspace, or permit builds/tests. Registered workspace aliases still resolve to their own roots; an absolute Desktop path explicitly paired with a repository alias remains refused. Arbitrary destination folders are not added by this change.

Example: `create a file on my desktop called test.md and fill it with 10 random numbers.` GRID stages ten integers from 0 through 999 (duplicates possible), shows the complete preview and absolute path, then requires `approve this change` followed by `apply this change`. Requests for 1–100 random numbers use local generation without AI. Other drafting instructions use the existing optional local AI draft path. `create a file on my desktop` asks for a filename and content. `read file test.md on my desktop`, update/delete equivalents, and explicit `changes ... @desktop ...` use the same bounded UTF-8 text policy, identity/activity checks, guarded backups and rollback. Delete/code changes still require high-risk approval. Recovery syntax: `recover change <id> in @desktop`. No outside-root traversal, missing-parent creation, or implicit overwrite is allowed.

Creating an existing file now reports its absolute path and asks the user to read, rename, or explicitly update it, even when its content matches. An incomplete `creat` receives deterministic file-workflow guidance rather than AI instructions to manually create a file. Disconnect clears pending requests and receipt scope so reconnect does not consume the next request with an expired-state response. Verification: **951 tests passed** (14 Core, 157 Simulation, 99 Persistence, 681 Terminal), including six new cases using a synthetic Desktop directory. No real user Desktop files were changed by verification. No schema migration or new external dependency.


**2026-10-28 — verified file destinations:** Previews, captured reads and applied/rollback receipts now show the full absolute target path. In chat, “Where did you save the file?” and “What is the file path?” use the latest operation receipt in the current identity/conversation scope, refreshing its draft state when available. These answers bypass AI, distinguish unperformed drafts from applied operations, and explicitly do not claim a fresh filesystem check. Without a valid receipt, GRID cannot confirm a location. A workspace alias resolves to its registered root; `edits-demo` is not implicitly Desktop. Verification: 945 automated tests passed, including destination/state checks, explicit-command apply, and reconnect isolation. Existing user files were not moved.

**6.9 follow-up repaired — 2026-10-28:** Workspace clarifications display immediately, name the missing alias and show available aliases while retaining the request. Printable keys used to skip a reply are preserved for the next input; secret entry clears that one-off buffer. Markdown fence-only lines are ignored without calling AI or clearing the pending request. A disconnected file request explains how to reconnect; `reate file` requests a correction instead of asking the model to invent a destination. **944 tests passed**, plus six real Windows-console checks. The supplied unexpected disconnect was not reproduced; the tested flow retains its connection. [Console evidence](verification/6.9-input-repair-report.md).

**Implemented 2026-10-27.** Human Master Control can request Create, Read, Update and Delete in connected original-host or attached agent conversations. All filesystem work executes in the original host under its authenticated session/workspace checks. The attached UI never acquires an independent filesystem tool or save owner. Agents/model output cannot approve their own work.

## Conversation workflow

Start GRID and check registered workspace aliases in the original terminal:

```text
grid -start
maxis workspaces
connect MCP -inline
```

Use `connect MCP` for a separate conversation window. The examples below assume `grid` is an existing enabled workspace. Substitute its actual alias. Existing MAXIS registration still requires a local root and relative `.csproj`; this milestone does not authorize arbitrary host paths.

```text
create file grid69-test.md in grid with content Test one.
approve this change
apply this change
read file grid69-test.md in grid
update file grid69-test.md in grid with content Test two.
approve this change
apply this change
delete file grid69-test.md in grid
approve high-risk
apply this change
undo this change
```

Create refuses an existing destination instead of overwriting it. Update/delete require an existing file. Read displays the explicitly requested workspace file, not an automatic model grant or persistent memory. Use existing scoped `context`/analysis flows for uploaded documents. The final undo in this example restores `Test two.` from the deleted file's durable backup.

Supply a quoted spaced/Unicode filename, for example `create file "café notes.md" in grid with content "Hello 世界"`. Absolute targets are accepted only when they resolve inside the selected registered workspace. A pasted path by itself retains 6.8's separate import-confirmation behavior.

If only one workspace is registered it is selected automatically. Otherwise GRID asks for the alias. Missing filename or complete content is requested individually. After a content question, supply literal text, `content: <text>`, or `draft: <instructions>` for local AI to propose content. A different question leaves the flow. Explicit empty content is supported with `with content ""` and still requires review.

For an AI-authored file:

```text
create file greeting.md in grid with instructions Write a short project welcome.
approve this change
apply this change
```

The model returns one complete `content` string only. It cannot choose a target, supply approval or add executable tool fields. AI generation uses only the user's drafting request and this explicitly selected target's existing content. It does not search other workspace files, use private context automatically, or share a whole directory. A complete preview appears before approval. Code/configuration and all deletions require the explicit phrase `approve high-risk`; ordinary text/Markdown uses `approve this change`. `apply this change` is a separate request. `review`, `cancel`, `go back` and `undo this change` work against the current specific change. Existing exact-ID `talk approve/apply/rollback edit <id>` aliases remain supported.

## Permission, review and failure behavior

- Every mutation has a typed Create/Update/Delete operation, exact workspace/target, original/candidate SHA256 and complete preview. Delete explicitly says **1 file** and explains recovery. Intent routing is bounded; no model response or uploaded text is treated as a command.
- Approvals bind to that exact operation/content/target and the current authenticated session/activity/context. Create never silently becomes update; update never silently becomes create. Cancellation, replacement and going back discard an unapplied draft/approval. Completed changes require an explicit undo.
- The same session/authority checks apply to attached requests. Another window cannot apply a draft ID held by a different dispatcher. Closing a window loses its unapplied drafts; applied changes retain disk recovery records. Parent logout or identity revocation blocks subsequent operations.
- Read/create/update/delete are limited to **one supported UTF-8 text file, at most 16 KiB**. Parent directories must already exist. Existing extension allowlist applies. Directories, wildcard/bulk operations, binary files, save databases, traversal, streams, reparse links, VCS/generated directories and GRID runtime/edit-artifact directories are refused. No mkdir, rename, shell or bulk transaction is added.
- Before apply, hashes and paths are checked again. Updates use a sibling temporary file and replacement; creates refuse a destination that appeared after review. Deletion writes and flushes original recovery bytes before removing the file. If backup creation fails, the source is not changed.
- Undo restores exact original bytes (including BOM/line endings). It refuses to overwrite a new file at a deleted target or a changed candidate after create/update. Backups remain on disk. This is equivalent recoverable deletion, not an OS Recycle Bin integration.
- If final journal writing fails after mutation/undo, the result reports that the file changed and includes the warning/recovery path. No automatic retry is attempted. Filesystem edits and the world database are not one atomic transaction.
- Source/hash checks protect against detected changes. As with the existing editor, this is not an OS sandbox or an absolute guarantee against a hostile concurrent process racing filesystem checks.
- Direct literal operations work offline without AI. Optional drafting requires the bundled model. AI input remains capped at 3,200 UTF-8 bytes; large inputs are refused rather than silently truncated. This is separate from the 16 KiB editable-file limit. At most 16 change records are held per dispatcher.

## Recovery after a window closes or GRID restarts

Keep the change ID and reported recovery directory. Start GRID, authenticate, reconnect and use:

```text
recover change <actual-change-id> in grid
undo this change
```

Replace the placeholder with the returned ID. Recovery loads only that record from the host's configured workspace-edit artifact store. It verifies the registered root, ID, relative path and original/candidate backup hashes. Loading performs no source mutation. Undo separately verifies the current target before restoring it. Recovery is available through `changes recover <workspace> <change-id>` too.

Unapplied approvals are never restored. Applied/rolled-back records can be loaded; a `Prepared` journal is considered recoverable only when current target bytes/absence match the recorded applied result. Otherwise GRID stops and leaves evidence for inspection. Corrupt/missing backups, a different workspace or a newer target are refused. Recovery records bind to the original workspace root; relocation/cross-device rebinding remains part of 6.14, not a new portability guarantee.

## Explicit tools and extensibility

The conversation uses the same host `changes` service as explicit commands. New tools are:

```text
changes
    - changes create <workspace> <relative-file> <content>
      # USAGE: Stage an exact new text file; refuse existing destinations.
    - changes read <workspace> <relative-file>
      # USAGE: Read one explicitly selected, allowed workspace text file.
    - changes update <workspace> <relative-file> <content>
      # USAGE: Stage the complete replacement of an existing text file.
    - changes delete <workspace> <relative-file>
      # USAGE: Stage recoverable deletion of exactly one existing text file.
    - changes draft-create <workspace> <relative-file> <instructions>
      # USAGE: Ask local AI for candidate content for a new file; never apply it.
    - changes draft-update <workspace> <relative-file> <instructions>
      # USAGE: Ask local AI for candidate replacement content for this existing file.
    - changes recover <workspace> <change-id>
      # USAGE: Validate a durable applied record for a later explicit rollback.
    # EXAMPLE: changes read grid grid69-test.md
```

Existing list/preview/approve/apply/rollback/discard/stage/suggest/verify commands remain. A typed operation and receipt carry mutation identity/state through the conversation; the UI does not derive approval from generated prose. The existing context-based MAXIS suggestion workflow also becomes available through authenticated attached host dispatchers. Workspace registration does not grant private agent context; explicit context grants remain necessary for that older suggestion path.

## Completion verification

**941 Release tests passed** (14 Core, 157 Simulation, 99 Persistence, 671 Terminal), including **37 new 6.9 cases**. Coverage includes complete conversation-only CRUD, optional AI drafts, explicit-ID aliases, Unicode/spaced/absolute paths, empty content, existing destinations, scope/path denial, malformed model output, stale content, cancellation, session/workspace revocation, failed backup creation, damaged recovery records, interrupted final-journal recovery, and saved-world reload with no retained approvals. Attached named-pipe checks verify CRUD and AI drafts execute on the host, cannot be applied from a different window, and stop after parent revocation.

**11 bundled-model workflow checks passed**, including a generated draft, denied unapproved application, exact approved operations, database reload and restoration of deleted bytes. [Captured report](verification/6.9-file-operations-report.md). The probe uses a synthetic world/workspace; it does not claim model answers are always correct or that all physical consoles/devices were visually inspected.

Repeat the opt-in packaged probe with `.\Grid.Terminal.exe --verify-file-operations`. No schema migration or new runtime dependency was added. 6.10–6.15 remain planned; durable file-derived conversation memory, expanded MCP authority, invitations, per-agent models, cross-device AI and PDF extraction are not delivered by this milestone.
