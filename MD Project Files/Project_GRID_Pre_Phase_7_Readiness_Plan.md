# Project GRID — Required Work Before Phase 7

**6.10 completed — 2026-10-28:** Approved document summaries and recorded task outcomes persist with provenance, explicit recipient/project grants, expiry, correction and revocation. Temporary raw uploads stay temporary. Schema v24; **989 automated tests and six bundled-model checks passed**, including copied-database recall and in-flight revocation/expiry. [Commands, retention policy, migration and test](Project_GRID_Durable_Memory_and_Delegated_Context.md). Earlier private-only or 6.10-planned statements are historical; 6.11–6.15 remain required before Phase 7.

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


**Implemented 2026-10-28 — Desktop file destination (6.9 follow-up):** Explicit Desktop requests now use the host Windows Desktop known folder, including OneDrive redirection. This file-only destination is `@desktop`; it does not require a `.csproj`, register a development workspace, or permit builds/tests. Registered workspace aliases still resolve to their own roots; an absolute Desktop path explicitly paired with a repository alias remains refused. Arbitrary destination folders are not added by this change.

Example: `create a file on my desktop called test.md and fill it with 10 random numbers.` GRID stages ten integers from 0 through 999 (duplicates possible), shows the complete preview and absolute path, then requires `approve this change` followed by `apply this change`. Requests for 1–100 random numbers use local generation without AI. Other drafting instructions use the existing optional local AI draft path. `create a file on my desktop` asks for a filename and content. `read file test.md on my desktop`, update/delete equivalents, and explicit `changes ... @desktop ...` use the same bounded UTF-8 text policy, identity/activity checks, guarded backups and rollback. Delete/code changes still require high-risk approval. Recovery syntax: `recover change <id> in @desktop`. No outside-root traversal, missing-parent creation, or implicit overwrite is allowed.

Creating an existing file now reports its absolute path and asks the user to read, rename, or explicitly update it, even when its content matches. An incomplete `creat` receives deterministic file-workflow guidance rather than AI instructions to manually create a file. Disconnect clears pending requests and receipt scope so reconnect does not consume the next request with an expired-state response. Verification: **951 tests passed** (14 Core, 157 Simulation, 99 Persistence, 681 Terminal), including six new cases using a synthetic Desktop directory. No real user Desktop files were changed by verification. No schema migration or new external dependency.


**6.9 completed — 2026-10-27:** Scoped single-text-file CRUD, reviewed AI drafts, host execution for attached requests, deletion backups and explicit restart recovery pass acceptance. [941 tests / 11 bundled workflow checks](Project_GRID_Conversational_File_Operations.md). Sections 6.10–6.15 remain mandatory; Phase 7 is still blocked.

**6.8 completed — 2026-10-27:** Natural connected chat, raw quoted text and upload/follow-up recovery now work in original and attached terminals. 904 tests and six live bundled-model checks passed. [Completion evidence](Project_GRID_Conversation_Recovery_Implementation.md). Sections 6.9–6.15 remain planned; PDF extraction and second-device AI remain unresolved.

**Status update — 2026-10-27:** 6.7 is implemented: user-supplied banner, elapsed-time login animation, automatic entry and revised colors; 886 tests and native console verification passed. [6.7 implementation](Project_GRID_Login_Animation_and_Colors.md). The planning text below records the approved requirements; 6.8–6.15 remain unimplemented and Phase 7 stays blocked.

**Planned — 2026-10-27. Documentation only; none of the additions below is implemented by this planning update.** All sections 6.7–6.15 in the master checklist are mandatory before Phase 7 begins. Completed milestones and their dated verification remain historical evidence, not proof that the newly reported failures are resolved.

## Reported problems and design direction

The supplied terminal transcript demonstrates a rejected PDF upload and a conversation trapped in a literal `continue` follow-up. Requests such as `analyze the file uploaded` and `continue to analyze` are not accepted there. Repeating `select files` also causes unnecessary extra questions. These are usability defects, not user mistakes.

The user reports that a copied GRID cannot use AI on another device. Treat portable AI as an unresolved release blocker; the precise cause must be diagnosed. Existing same-PC/private-runtime probes do not establish second-device operation.

The supplied screenshot is a visual reference for a clean AI-oriented terminal: clear prompt, spacious output and an obvious place to describe work. It is not an instruction to execute the commands visible in the image or to adopt another product's permissions/model setup.

## 6.7 Login animation and terminal colors

Replace the post-login “Press Enter to continue” gate with a five-second ASCII GRID7736 logo animation, then enter the terminal automatically. Preserve the welcome name and authority information in a readable transition. Use approximately five seconds of elapsed time, not a frame-count delay dependent on hardware speed. Offer a skip/reduced-motion setting; redirected output shows a static logo without animation or an input wait.

Use yellow for all terminal-generated messages, including agent answers, notices, errors, tables and dividers; white for user-entered text and displayed user-message bodies; light blue for prompts and speaker labels. Prompts/labels are the explicit exception to “all messages.” Errors remain unmistakable through wording and markers rather than red. This supersedes the previous red-error/light-blue-body rules when implemented. Apply consistently to login, ordinary terminal, live view, inline/attached chat, meetings and shutdown, while honoring no-color terminals.

Acceptance: timed interactive startup reaches the prompt without Enter; skip and redirected modes work; screenshots/console checks cover each output class and both host/attached windows.

## 6.8 Conversational intent and follow-up repair

Make connected chat accept ordinary user sentences as well as the existing `talk` prefix. Preserve explicit commands without misrouting a pasted path or normal sentence as a command. Retain quoted text, including HTML IDs, rather than losing quote characters during chat parsing.

Resolve follow-ups using the pending goal and the user's meaning. `continue`, `continue to analyze`, `analyze`, and `analyze the file uploaded` should resume the same pending analysis when unambiguous. Provide a natural way to cancel, replace the request, go back or change the target. Do not trap an unrelated question in a modal loop. Show one concrete example instead of repetitive instructions that resemble commands to copy literally.

One `select files` request should open the picker when the intended agent/project grant is already clear. After a successful import, continue the requested analysis once the declared grant/consent is satisfied, without another mandatory magic phrase. On cancellation, unsupported input or extraction failure, preserve the original goal and offer a useful retry. Handle paths with spaces, optional surrounding quotes and missing files without getting stuck. Never treat a path alone as permission to read arbitrary files or broaden an agent grant.

Acceptance: replay the supplied PDF/HTML transcript using its alternate replies; obtain analysis without a loop. Cover direct sentences, `talk` aliases, quoted CSS, interruption, cancellation, stale sessions, and host/attached windows. Include a basic CSS-answer correctness case: width 100% alone must not be presented as visibly centering a narrower element. Distinguish model factual quality from tool execution tests.

## 6.9 Complete file operations through conversation

Support Create, Read, Update and Delete through ordinary requests in connected chat. Ask only for missing target, workspace/path scope, intended content or ambiguity. Show the interpreted operation and target before consequential changes. Provide extensible, typed file tools rather than requiring users to memorize separate commands.

Read only explicitly available files/context. Create and update use reviewed content and preserve existing hash/path/backup checks. Delete requires clear target/count, explicit approval and recoverable quarantine or equivalent undo where feasible; explain irrecoverable cases before approval. Never infer bulk deletion, overwrite permission or a scope grant from model text. Preserve save data and concurrent edits; report partial failure accurately. Extend original-host services to handle approved work requested from attached agent chats without granting clients independent filesystem authority.

Acceptance: end-to-end conversation-only create/read/update/delete, Unicode and spaced paths, existing destinations, denied/out-of-scope paths, stale content, cancellation, attached-window requests, recovery/rollback and restart. Approval must bind to the exact operation/content/targets, not an indefinite future request.

## 6.10 Durable memory and user-to-agent context

Keep useful conversation facts, decisions, preferences and unfinished work across disconnect, window close, process restart and copy/transfer. Build on existing automatic recall and pinned memories; do not erase or relabel that implemented history. The reported desired behavior extends to task outcomes and user-authorized file-derived summaries, which current analysis intentionally excludes from recall.

Distinguish raw document retention, extracted context, conversation summaries and explicit pinned memory. Show what is retained, its source and participating agents. Ask when a temporary upload would need durable retention; ending a chat must not silently discard a user-approved durable memory or silently promote temporary raw files to permanent storage. Provide review, correction, forgetting, expiry and project/agent scope controls. Revocation must invalidate pending use and derived recall as defined by the retention policy; explain historical backups and transcripts honestly.

Acceptance: end a chat, restart, reconnect and recall an approved detail with provenance; repeat after portable copy. Verify another agent cannot retrieve private details without a grant, forgotten/corrected facts do not reappear, and temporary documents stay temporary unless the user approves retention.

## 6.10A Terminal visual theme — planned next

Implement the [complete supplied theme specification](Project_GRID_Terminal_Visual_Theme_Specification.md) after completed 6.10 and before 6.11. Centralize the exact palette, semantic output, compact headers and responsive layouts; verify truecolor/fallback/monochrome/redirected behavior and unchanged authorization. This is presentation-only. All 18 acceptance checks remain open. Phase 7 remains blocked until 6.10A and 6.11–6.15 are completed.

## 6.11 MCP — Master Control Program

MCP means **Master Control Program**. It is the user's principal digital helper, separate from the human Master Control identity. Planned workflow: receive the request → analyze requirements → clarify/confirm what is needed → choose the best specialist → assign/prepare its work → request human approval when required → execute approved work → brief the user with verified results, changed files and remaining issues. Assignment/planning must be distinguished from execution so assignment does not bypass the requested approval step.

MCP should handle custom user requests through an extensible capability/tool registry, not only a fixed list of command phrases. Add reusable request plans, specialist selection with a reason, progress/cancellation, structured results, failure recovery and accurate completion briefings. Do not claim completion from a generated response alone.

Design an explicit authority matrix giving MCP nearly all Master Control operational capabilities under delegated user authority. Separate permission to inspect, propose, coordinate and execute from permission to approve. MCP must not impersonate the human, approve its own requests, change authentication or expand its own authority. Define handling for NGE, transfers, credentials, agent grants and other destructive/security-sensitive operations before implementation; retain direct human confirmation for these. GRID authority grants no Windows elevation. Record delegated authority, approvals and actions; allow the human to revoke them.

Acceptance: real multi-step custom request, correct specialist assignment, meaningful clarification, exact approval binding, rejection/revocation, interrupted work and an evidence-backed final briefing. Verify MCP can perform approved operational work while reserved human controls remain protected.

## 6.12 Invite specialists into MCP conversations

Let MCP bring the best-fitting agent into the current conversation when work requires it, rather than forcing a disconnected chat. Explain the invitation and shared context; obtain confirmation where membership expands private access. Let the user instruct that specialist directly and let MCP coordinate the result in the same conversation. Show who is speaking, who owns each task and whether an agent joined or left.

Share only selected task context with explicit provenance and grants. Joining must not expose all historical messages, private memories or uploaded files. Revalidate pending replies/plans when membership changes. Persist appropriate membership/task history and retain separate private conversations.

Acceptance: MCP invites MAXIS for an edit, user gives MAXIS direct instructions, approval is requested and MCP reports verified completion. Test declined invitation, least necessary history, removal/revocation, cross-project isolation and restart.

## 6.13 Agent model profiles and a 20 GB base package

Assign every agent a model profile chosen for its actual duties: MCP planning/coordination, MAXIS coding, JN document/storage reasoning, SENTINEL security analysis, VOLT resource reasoning and RELAY communication. Benchmark candidates offline before selecting exact model names, versions, quantization and context limits. A separate profile does not require duplicating identical model weights: agents may share a model when measurements show it fits both roles. Document each choice and fallback; no silent cloud substitution.

**New base package ceiling: 20 GB (20,000,000,000 bytes, matching the existing project's decimal convention).** Include application binaries, source/code, bundled runtimes, installed model weights/tokenizers, libraries, offline extraction assets, manifests and other required base components. Audit what copy/transfer actually carries. Share identical assets, avoid per-agent weight copies and verify budget from actual files, not download estimates. If an approved optional model would exceed this ceiling, replace/reduce another base component or propose a separately approved optional installation; do not silently increase the base cap.

Distinguish this package ceiling from the existing JN 20 GB managed-data governance. Saves, retained context, task artifacts, recovery backups and logs need a separate visible growth budget, free-space check and cleanup policy; they must not silently consume space required by the base installation. Account for temporary staging, duplicate old versions and update headroom. Display both base size and total portable footprint.

Acceptance: measured clean base ≤20 GB with all required models/runtime present; per-agent model selection and offline fallbacks; low-memory/CPU-only operation; scheduling/load limits; no missing or duplicate required weights. Record task-quality and performance results before selecting the final model set.

## 6.14 Complete copy/transfer and second-device offline AI

Diagnose the reported failure using a copy produced by `grid -copyto` and a transfer produced by `grid -transferto`, not only a separately published development package. Define a manifest for all required and user-retained GRID files: application/code, AI executable/runtime, every active model profile and model blob, tokenizer/configuration, document extraction tools, dependencies, database, approved context/memory, task/recovery artifacts, launcher and licenses. Preserve source files outside GRID only when explicitly imported/authorized; do not silently copy unrelated PC data.

Resolve assets relative to the destination root; eliminate reliance on the source PC's installed AI, caches, absolute paths, global PATH, development checkout or .NET installation. Preflight missing assets and refuse to declare a portable copy complete if any are missing. Verify copied hashes and destination readiness; retain the transfer source until verification succeeds. Ensure delegated file/workspace grants tied to the old machine are reauthorized instead of silently retargeted to unrelated files.

On the new device, identify supported OS/architecture and changed hardware, use a bundled CPU fallback when the original GPU backend is unavailable, and show actionable diagnostics. Integrate the existing new-device/save-as-device plan while preserving the saved world. No global AI installation, administrator rights, network or first-run model download may be required for normal operation on a supported device. Installation/preparation can download approved assets beforehand; runtime tasks must remain offline.

Acceptance: on an actual second supported PC or clean VM with no AI service/models or development SDK installed, disconnect networking and launch the actual copy/transfer destination. Verify chat, each agent model profile, PDF/DOCX extraction, scoped memory, a harmless approved task, restart and relocation/drive-letter change. Include CPU-only fallback, removable-media interruption and missing/corrupt assets. Capture evidence; same-device path/profile isolation is supplementary, not a substitute. Cross-platform support beyond tested OS/architecture is not implied.

## 6.15 PDF/DOCX imports and Phase 7 readiness gate

Implement offline PDF text extraction and complete reliable DOCX import, including paragraphs and tables. DOCX already has limited support; PDF is currently unsupported. Update picker filters, path imports, MIME/extension validation, errors and packaging consistently. Retain file hash, original name, page/section references, extraction status and truncation notices so an agent can cite what it actually read.

Handle text PDFs, scanned PDFs through bundled offline OCR or an explicit no-text/retry result, mixed documents, Unicode, large files, corrupt/locked/encrypted inputs, unsupported content and empty extraction. Never report a successful useful import with silently empty text. Define page/byte/time limits, cancellation, archive/decompression limits and safe parser behavior; do not execute macros, embedded scripts or external document links. Include extraction dependencies in the 20 GB base and portable manifest. No installed Office, external PDF application, cloud converter or network dependency.

Phase 7 is blocked until 6.7–6.15 are complete and documented. Replay the exact conversation-loop regression; complete conversation CRUD and scoped recall, MCP delegation/invitation/approval/briefing, role-model quality and size checks, and second-device offline copy/transfer acceptance. Include representative manual terminal/window checks and truthful AI-answer tests. Record tool/test versions, artifact paths, actual package size, supported device matrix and unresolved limitations. Existing 873 tests and 12 live checks are a baseline, not evidence these new requirements pass. The human reviews the acceptance results before starting city rendering work.

## Implementation order

1. Repair conversation follow-ups and define portable asset/storage/model contracts (6.8, design portions of 6.13–6.14).
2. Implement login/color changes and offline document extraction (6.7 and document work in 6.15).
3. Complete CRUD and durable context/memory (6.9–6.10).
4. Expand MCP authority/tool orchestration and in-chat specialist invitations (6.11–6.12).
5. Finalize benchmarked model profiles, base-size enforcement and portable transfers (6.13–6.14).
6. Run all acceptance gates in 6.15; only then begin Phase 7.

Existing Phase 8 packaging, installation, storage and device-handling headings remain useful for later hardening. Any portion required for this pre-Phase-7 acceptance is brought forward here and cannot be postponed to Phase 8.
