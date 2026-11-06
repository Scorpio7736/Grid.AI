# Project GRID - Current Implementation and Verification

**6.22 inference authorization foundation — 2026-11-05.** Host-built identity/scope contracts, live validation, bounded per-owner admission quotas and content-free audit now protect ordinary chat and assistance. 915 terminal tests passed. Additional inference routes and joint 6.23–6.24 action verification remain open; no new action authority is enabled. [Implementation and limits](Project_GRID_Request_Authorization.md).

**6.21 agent memory migration — 2026-11-05.** Memory records, grants and recall policy now belong to Grid.Agent, with generic lexical/extractive helpers in Grid.AI. Existing conversation storage, schema v24, permissions, correction/expiry and source revocation remain compatible. 1,180 tests and six live copied-database memory checks passed. No new model or memory database is required. [Implementation and verification](Project_GRID_Agent_Memory_Migration.md).

**Local file discovery follow-up — 2026-11-05.** Connected Master Control conversations recognize `hey mcp can you find the stl file on my desktop` and `find UWGB in my documents` as local name searches, not uploaded-context analysis. `search my files for any STL file` asks for a location; repeat with Desktop, Documents or Downloads. Lookup lists observed paths without opening file contents, importing context, invoking the model or changing files. Private searches are refused in meetings. Limits: 2 seconds of checked traversal time, 10,000 entries, depth 8 and 50 results; hidden/system/reparse-point folders and inaccessible entries are skipped with an incomplete-result notice. An individual filesystem call can still wait on the OS. This is bounded local discovery, not unrestricted whole-PC search or STL content analysis. Windows Desktop/Documents locations are resolved through special-folder settings. Downloads currently uses the profile Downloads path; custom redirects there remain unsupported.


**Conversational retrieval follow-up — 2026-11-05.** In a connected conversation, type `find and explain INDIGO`, `find INDIGO in my uploaded documents` directly; `assist analyze` and `talk` are optional. Missing grants prompt for files in plain language and retain the question through selection. Analysis shows an agent reply, with `details` available for citations. Existing file/task review and approval boundaries remain unchanged. Routing supports these documented intents; arbitrary requests are not guaranteed to become executable operations.


**6.20 lexical retrieval foundation — 2026-11-05.** Scoped captured-document search and assistance now use SQLite-backed chunks, BM25/token-feature vectors, reranking and versioned citations. 1,165 current-suite tests and a live deep-document answer/revocation check passed. Learned semantic embeddings remain pending; original files are not reopened and Phase 7 is not cleared. [Implementation and limits](Project_GRID_Retrieval_Implementation.md).

**6.19 text observability foundation — 2026-11-05.** `ai diagnostics` exposes bounded content-free request records and a separate saved-task projection. The version-2 text benchmark has explicit latency/allocation thresholds and sampled CPU/memory/GPU-allocation evidence. 1,151 current-suite tests and both live baseline modes passed. Specialist/image, physical-device and network-blocked acceptance remain open. [Implementation and limits](Project_GRID_Observability_and_Evaluation.md).

**6.18 persona management completed — 2026-11-05.** Grid.Agent owns six versioned personas and prompt assembly; Grid.AI provides generic system/version/template fields. `ai personas` supports per-agent selection and rollback with portable settings. 1,139 regression tests, 10 live persona checks and normal assistance verification passed. Host permissions, memory scopes and approval rules are unchanged. [Details and test commands](Project_GRID_Persona_and_Prompt_Management.md).

**6.17A baseline evaluation — 2026-11-04.** Qwen3 4B passed automatic/CPU text smoke and lifecycle checks. Versioned evidence is tied to exact model/runtime/device/test settings; `ai registry acceptance` shows recorded results. 1,126 regression tests passed. Specialist models are not yet selected/downloaded; broad model, image, device, network-blocked and later-milestone acceptance remains open. [Results and remaining gates](Project_GRID_Model_Acceptance_Baseline.md).

**6.17 registry foundation — 2026-11-04.** Versioned model manifests, checksum verification, retained-revision rollback, shared-asset accounting, candidate tracking, persistent force-Lite and device diagnostics are implemented. 1,109 solution tests and live baseline inventory verification passed. Exact specialist artifacts, device benchmarks and automatic profile promotion remain pending. Running chat remains on the pinned Qwen3 baseline. [Implementation and commands](Project_GRID_Model_Registry_Implementation.md).

**6.16 text-provider implementation — 2026-11-04.** Grid.AI now provides common lifecycle, generation, streaming, timeout, cancellation and capability contracts. Existing Ollama chat uses the new adapter. A pinned optional llama.cpp CPU provider passed live checks using the existing Qwen weights. Current suites total 1,090 passing tests. Actual image-provider lifecycle/resource verification remains open, alongside model registry/profile acceptance and other Phase 7 gates. [Implementation and verification](Project_GRID_Model_Abstraction_Implementation.md).

**6.15 import implementation — 2026-11-04.** Offline PDF text extraction and DOCX body paragraph/table import are locally verified. The bundled parser uses bounded worker processes, page/paragraph provenance and explicit empty/scanned/encrypted/corrupt-file outcomes. 1,073 regression tests passed across current suites, with standalone PDF/DOCX deployment checks. Second-device acceptance and the final 6.7–6.31 gate remain pending. [Implementation, scope and verification](Project_GRID_Offline_Document_Import_Implementation.md).

**Model-handling planning update — 2026-11-04.** The seven supplied text/code/image candidates and profile, delegation, output-generation and offline handling rules are documented as planned. The 75 GB model sub-limit sits inside the 128 GB Flash/PC target. Existing Qwen3 4B implementation and historic benchmarks retain their scope. Candidate installation, routing, quota enforcement and acceptance require future implementation. [Model selection and handling plan](Project_GRID_Model_Selection_and_Handling_Plan.md).

**Planning update — 2026-11-04.** Planning added for the GRID-AI intelligence engine, GRID-Agent framework and migration of GRID7736 integration in 6.16–6.31. All are required before Phase 7. The user’s new storage target is 128 GB decimal for Flash and PC copies. Current model/provider and 20 GB enforcement implementation remain the historical baseline awaiting the planned changes. The reported AI failure and second-device acceptance remain open. [Full plan and acceptance gates](Project_GRID_AI_and_Agent_Platform_Plan.md).

**2026-10-30 transfer/AI follow-up:** Removed repeated full-content transfer checks while retaining independent destination verification and pinned model/manifest matching. Hashing now reports byte progress; ETA uses measured copy/verification work. AI diagnostics retain startup errors, and bundled startup tolerates slow media. Original-PC AI passed an isolated test; the reported failing instance remains undiagnosed pending its exact error. [Implementation, evidence and limits](Project_GRID_Transfer_Checks_and_AI_Diagnostics.md). Physical second-PC acceptance and 6.15 remain pending.

**Progress-window visibility repair — 2026-10-29:** The launcher previously used a hidden-window startup flag, which could hide the WinForms dashboard even though its ready event fired. It now suppresses only the PowerShell console and requests a normal visible GUI. A real Windows test confirmed `IsWindowVisible=true` for the named dashboard, then closed only that test window. All five progress regression tests passed. Failed window startup now prints an explicit text-fallback notice. Restart GRID for the fix; existing transfers are not hot-patched.

**Transfer progress follow-up — 2026-10-29:** Copy/move previews show an approximate duration range before confirmation. A separate Windows dashboard shows progress, elapsed/remaining time and a bounded live activity feed; validation and move cleanup cannot prematurely show completion. Closing the viewer leaves transfer running; text fallback remains available. See [progress window and verification](Project_GRID_Transfer_Progress_Window.md).

**6.14 repair — 2026-10-29:** Development-build transfers now include bundled runtime/models/.NET and available source; launch paths are destination-relative, assets/hash/budget are verified before publication, and device/location changes require grant/approval review. A synthetic-world second-PC package and [acceptance instructions](Project_GRID_Portable_Transfer_Repair_and_Second_PC_Test.md) are ready. Local verification passed 1,049 regression tests, 14 live profile checks and six MCP workflow checks. Second-PC offline/drive-letter/removable-media acceptance remains pending; PDF extraction stays in 6.15. Phase 7 remains blocked.

**6.13 completed — 2026-10-29:** Six explicit agent profiles share one pinned Qwen3 4B Q4_K_M snapshot. CPU-only/reduced-memory operation, role routing and the 20,000,000,000-byte base ceiling are implemented. `ai profiles` and `ai storage` expose configuration and base/data/staging/headroom. **14 live role checks passed**; physical low-RAM/second-device certification remains separate. See [profiles, measured evidence and limits](Project_GRID_Agent_Model_Profiles_and_Base_Budget.md). Earlier planned/fixed-model descriptions are historical; 6.14–6.15 still block Phase 7.

**6.12 completed — 2026-10-29:** MCP can recommend/invite registered specialists with human confirmation, show participants, accept direct specialist planning requests and display recorded MCP progress/results in the same window. Membership changes start a fresh saved history segment; earlier transcripts and private context are not automatically shared. Decline, removal, scope changes, restart and attached-window workflows are tested. **1,031 automated tests and six bundled-model checks passed.** See [workflow, privacy boundary and verification](Project_GRID_MCP_Specialist_Invitations_Implementation.md). Earlier statements that invitations are planned are historical; 6.13–6.15 still block Phase 7.

**2026-10-28 meeting round/addressing follow-up:** Explicit `each of you`, `everyone`, `all of you`, and `you all` requests now generate independent replies for every meeting participant, committed together with one user message. Cancellation/scope changes prevent partial round storage; unavailable inference is labeled per participant. `hey/hi/hello MAXIS ...` routes to MAXIS. While a meeting is connected, `maxis what is your favorite color` is chat; recognized management subcommands such as `maxis workspaces` and `maxis register ...` retain command behavior. Group introductions still use registered roles without model inference. Shared prompts prohibit impersonating other participants, distinguish fictional preferences from observed facts, and ask for a source when a reading request has none. No private context, task execution or automatic invitations are granted. Verification: **1,022 regression tests passed** (752 Terminal), plus bundled-model replay of the reported messages. Local-model answers can still be factually wrong; correct routing is not factual verification.

**2026-10-28 meeting identity repair:** Leading participant names now direct replies (`maxis`, `MAXIS, introduce yourself`) without requiring `@`; absent named agents are refused rather than silently routed to MCP. `each of you introduce yourselves` stores one user message followed by every participant’s registered role introduction, atomically and without AI inference. Ordinary meeting AI receives the authoritative participant roster and roles, with shared history only. General discussion still selects one speaker per turn; this does not add automatic invitations or general multi-agent broadcasts. Verification: 1,017 regression tests passed across the four suites (747 Terminal), and the bundled model correctly handled the reported introduction/name sequence in an isolated world. Evidence: `%TEMP%\GridMeetingIdentityProbe\report.txt`.

**6.11 completed — 2026-10-28:** MCP (Master Control Program) now offers custom catalog-based planning, scoped clarification, visible specialist-selection reasons, approved ordered plan execution, cancellation, reusable plans and recorded-result briefings. Reserved human controls remain explicit. **1,012 automated tests and six bundled-model checks passed.** Meeting shorthand (`meetings ...` and `connect meetings ...`) now validates before opening a window and supports `-inline`. [Workflow, authority matrix and terminal test](Project_GRID_MCP_Digital_Helper_Implementation.md). Earlier 6.11-planned statements below are historical; Phase 7 still requires 6.12–6.15.

**2026-10-28 follow-up — GRID logo animation restored at user request:** Startup/login and shutdown show the supplied GRID7736 artwork when it fits. The five-second login transition progressively reveals the logo from left to right, retains skip/reduced-motion support, and enters the terminal automatically. Narrow windows keep the compact fallback. The new 6.10A colors and responsive help remain. This supersedes the compact-logo replacement described below; other screens retain compact headers.

**6.10A implemented — 2026-10-28:** Centralized 13-color terminal theme, semantic feedback, compact headers, section bars, responsive help tables and Unicode-aware layout/input are implemented. Truecolor is used in supported terminals, with console-color and plain-text fallbacks. User input stays white, prompts cyan, agent dialogue amber; errors are red, success green and approval labels gold. This supersedes historical blanket-yellow and oversized-header requirements. Commands, authentication and approval rules are unchanged. **999 automated tests passed**, plus Windows pseudoconsole login/color/masking/resize checks. [Implementation and limitations](Project_GRID_Terminal_Visual_Theme_Implementation.md). Phase 7 remains blocked on 6.11–6.15.

**6.10 completed — 2026-10-28:** Approved document summaries and recorded task outcomes persist with provenance, explicit recipient/project grants, expiry, correction and revocation. Temporary raw uploads stay temporary. Schema v24; **989 automated tests and six bundled-model checks passed**, including copied-database recall and in-flight revocation/expiry. [Commands, retention policy, migration and test](Project_GRID_Durable_Memory_and_Delegated_Context.md). Earlier private-only or 6.10-planned statements are historical; 6.11–6.15 remain required before Phase 7.

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

**6.9 implemented — 2026-10-27:** Conversation-driven scoped CRUD, optional AI content drafts, attached requests executed by the host, recoverable deletion and explicit backup recovery after reload are implemented. 941 tests and 11 bundled-model workflow checks passed. [Commands, evidence and limits](Project_GRID_Conversational_File_Operations.md). 6.10–6.15 remain planned.

**6.8 implemented — 2026-10-27:** Ordinary connected chat preserves quoted text. One scoped file selection starts analysis automatically; upload errors, alternate continue/analyze replies, cancellation and changed requests no longer trap the conversation. 904 Release tests and six bundled-model checks passed. [Behavior and evidence](Project_GRID_Conversation_Recovery_Implementation.md). 6.9–6.15 remain planned.

**6.7 implemented — 2026-10-27:** New user banner, five-second skippable login animation, automatic terminal entry and yellow messages/white user text/light-blue prompts. 886 Release tests passed; real Windows-console login/color probe passed. [Behavior and evidence](Project_GRID_Login_Animation_and_Colors.md). 6.8–6.15 remain planned.

**New required work — 2026-10-27 (planned only):** 6.7–6.15 must finish before Phase 7. The user reports a literal-follow-up analysis loop and copied-device AI failure; neither is resolved by this documentation update. PDF remains unsupported and DOCX limited. Prior 6.6 test results remain historical. [Required changes and acceptance](Project_GRID_Pre_Phase_7_Readiness_Plan.md).

**6.6 implemented — 2026-10-27:** Unified `talk` supports conversation/advice, scoped analysis, file/monitor follow-ups, MCP drafts and reviewed editing. Explicit approvals and original-host execution remain. **873 Release tests and 12 bundled-model checks passed.** [Workflow and test commands](Project_GRID_Unified_Talk_Workflow.md). Earlier 6.6-planned statements below are historical.

**6.5 completed — 2026-10-27:** 847 Release tests and 171 focused intelligence tests passed. Ten bundled-model checks passed, including injected context, advice-only operation, approval enforcement and exact-byte editing rollback. New repeatable diagnostics and captured evidence: [Intelligence verification](Project_GRID_Intelligence_Verification.md). Model accuracy remains fallible; 6.6 is still planned.

**6.4 implemented — 2026-10-26:** Original-host `changes` drafts new/revised workspace files from captured context or local MAXIS AI, previews complete content/hashes, requires explicit approval, and applies with backups and guarded rollback. Optional verification uses separately approved MAXIS tasks. **838 Release tests passed**. Drafts are transient; recovery files persist. [Workflow, limits and demo](Project_GRID_Authorized_Editing_Implementation.md).

**6.3A implemented — 2026-10-26:** `screen options/monitoring/ask/status/stop` provides one-shot selected-monitor assistance using offline Windows OCR and local AI. Human Master Control only; no recording, saved screen context or actions. **799 Release tests passed**, including attached-window and synthetic native-OCR checks. User desktop acceptance remains manual. [Commands, limits and verification](Project_GRID_Screen_Assistance_Implementation.md).

**4.2H implemented — 2026-10-26:** Conversation labels are light blue, agent bodies yellow and user text white. Assistance now animates; Enter no longer accidentally skips a reply. **772 Release tests passed**, plus an interactive Windows-console probe. User desktop/window visual acceptance remains open. [Changes, verification and test commands](Project_GRID_Conversation_Display_Repair.md).

**AI response recovery — 2026-10-26:** Truncated answers now get one bounded larger retry, and rejected responses are distinguished from an offline service. Advice and analysis prompts were refined; MCP greetings no longer receive unrelated district counts. **759 Release tests passed**, and the bundled model answered the reported HTML question successfully. [Fix, limits and regression test](Project_GRID_AI_Response_Recovery.md).


**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**Automatic conversation recall added — 2026-10-25:** Ordinary saved discussions and their relevant details now carry across conversations/restarts, alongside explicit memory. Private recall stays within the same agent/project; shared meeting recall requires the exact same group. `memory history` reviews candidates; `memory forget-history` excludes prior discussions while retaining explicit memories and transcripts. **728 tests passed**; bundled-model recall/clearing verified. [Behavior, bounds and test](Project_GRID_Automatic_Conversation_Recall.md). This supersedes earlier explicit-only recall descriptions.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


## Agent lifecycle — 5.9 completed (2026-10-23)

`grid -start` initializes the six named agents on first use and resumes their activity thereafter. `grid -pause` blocks new agent replies, MCP coordination and task dispatch while retaining identities, memory, conversations and task history. `agents initialize` now gives migration guidance; connecting to MCP no longer creates it implicitly. `agents` reports whether activity is active or paused. Ordinary startup/reload remains paused; the explicit `--start` launch option takes the same initialization/save path after authenticated owner login.

In-flight dialogue is cancelled and cannot commit across a pause/resume generation. In-flight task attempts are cancelled cooperatively and recorded as Interrupted when the lifecycle changed; partial external effects may remain, so retry requires inspection and fresh approval. Queued tasks are not automatically dispatched on resume; pause retains existing queued approvals, while restart invalidates them through the existing recovery policy. Authenticated attached Master Control terminals can start/pause the shared host, including while its original terminal is busy. Transfer/NGE ownership restrictions remain unchanged.

**Verification:** 672 tests passed (14 Core, 157 Simulation, 99 Persistence, 402 Terminal), including eleven new lifecycle/window/startup cases. Existing historical fixtures now explicitly start their worlds before agent work. Schema remains v21. See [lifecycle contract and acceptance commands](Project_GRID_Agent_Lifecycle_Implementation.md). Earlier descriptions of independent initialization/lazy MCP or planned 5.9 coupling are historical and superseded.


### 4.2G folder-picker repair — 2026-10-23

Fixed a user-reported Windows dialog failure: FolderBrowserDialog supports Description, not Title. Removed the unsupported property, explicitly emit JSON null on cancellation (Windows PowerShell otherwise emits no JSON for null), suppress first-use progress and keep raw helper stderr/CLIXML out of terminal errors. **392 terminal tests passed**, including three new tests executing the actual Windows PowerShell helper with a real Windows Forms dialog object. Tests substitute only the modal ShowDialog result for selection/cancellation and verify concise failure output. Physical dialog interaction remains a manual check. Earlier injected-picker tests did not cover this platform failure. Restart GRID to load the corrected binary. Schema remains v21.


## 4.2G completed — 2026-10-23

`grid -start` / `grid -pause` replace the old start/pause commands, which now give migration guidance without changing state. `tasks` and `tasks list` show all authorized retained history, including labeled archives. `tasks -completed` shows Completed only; `tasks -list` excludes Completed and Denied (cancelled/failed and archived records remain visible). All views accept one agent filter. Master Control can use `tasks deny <reference> "reason"` on an AwaitingApproval task; denial is terminal, separately audited and saved in schema v21. Existing v20 history upgrades without rewriting cancellations as denials.

Transfers accept drive roots as containers: `grid -copyto "E:\"` targets `E:\GRID7736`. `grid -copyto -select` / `grid -transferto -select` select a container and create its new GRID7736 child. Existing targets are refused; no merging or overwriting. Original-host Master Control, interactive preview/y/n, saved-world verification and move cleanup safeguards remain. No files move when selection is cancelled, authority changes or media is unavailable.

**Verification:** 658 tests passed (14 Core, 157 Simulation, 98 Persistence, 389 Terminal), including ten new regression cases; zero skipped. Native dialog appearance and physical flash-drive removal/cross-PC acceptance remain manual release checks. The picker uses Windows' built-in local folder dialog, with no new external package/service. The saved city and user files were not relocated by verification. Agent lifecycle coupling (5.9), startup test gate (8.8), changed-device handling (8.9), meetings (6.2A), and full portable/disaster acceptance (8.6) remain planned. See [implementation and test commands](Project_GRID_Terminal_Controls_Implementation.md).


## Fresh-clone runtime repair — 2026-10-23

Implemented tracked runtime preparation, self-contained Windows package building and isolated verification. GRID now owns a bundled AI server/model cache and uses bundled MAXIS tooling; no personal Ollama service is required. Typewriter now works with fallback replies and inline/attached chat, with explicit on/off overriding the startup preference. **648 tests passed**, plus a real portable AI/chat probe with global runtime paths removed. Full two-PC/voice/device/installer acceptance remains planned. See [setup, package layout, evidence and limits](Project_GRID_Fresh_Clone_and_Portable_Runtime.md). Earlier global-runtime instructions below are historical.


## Conversation readability follow-up — implemented 2026-10-22

Normal chat and `transcript` now display spaced `You:` / agent-name replies. Saved snapshot facts, source labels, ticks, origins and IDs are hidden from the default display and remain accessible with `transcript --details` or `transcript CONVERSATION_ID --details`. Existing transcripts retain their full original contents; this is a display change with no schema migration. Human text is not parsed as an agent metadata envelope.

Conversation windows have a shorter greeting and `You> ` prompt. `whoami` remains available for identity/authority details. Typewriter animation and authorization checks remain intact. Chat still uses `talk <message>`; ordinary terminal commands are not silently interpreted as chat. AI-generated text is identified once on connection, and unavailable AI shows a brief basic-reply notice.

The model prompt now requests short, natural first-person conversation: greet greetings, introduce the agent simply, use facts only when relevant, and omit internal instructions/IDs/ticks/source boilerplate unless asked. Historical generated-message wrappers are removed from model history excerpts so real dialogue, rather than repeated metadata, supplies continuity. Full saved evidence is unchanged. Model wording remains variable; chat still does not execute tasks.

**Verification:** all 373 terminal tests passed, covering compact transcripts, full detail recovery, attached-window prompt/greeting, authorization and typewriter behavior. Release build succeeded with zero warnings/errors. An isolated real-model greeting returned a short natural response without the technical envelope in the normal display. Earlier full-suite totals remain historical.

```text
talk Hello MCP.
talk Who are you?
transcript
transcript --details
```

Restart the updated host terminal before testing; previously open windows use the running host's old code. To revisit an existing conversation, use `conversations` then `resume CONVERSATION_ID`.


## Grounded agent conversations — 6.2 completed (2026-10-22)

`talk` and confirmed `voice send` now use shared local AI in inline and dedicated conversation windows. Replies separate timestamped engine facts, unverified AI suggestions/dialogue and supplied sources. Context includes scoped conversation excerpts, a bounded extractive history summary and permitted saved simulation memories; six named agents have distinct profiles. Session revocation/stale turns are rejected; unavailable AI falls back deterministically. No automatic task execution or captured-file ingestion. **641 tests passed**; a real local MCP conversation completed in an isolated test world. Schema remains v20. Meetings (6.2A) and AI task assistance (6.3) remain planned. See [implementation, limits and acceptance test](Project_GRID_Grounded_Conversations_Implementation.md). Earlier milestone statements below remain historical.


## 6.1 follow-up verification — 2026-10-22

Fixed malformed numeric allocation responses in `ai status`: diagnostics return unavailable instead of throwing, and a subsequent valid response recovers. All **355 terminal tests passed**, including three new regression cases; Release build had zero warnings/errors. A fresh real local-model probe returned GRID LOCAL READY in **3,252 ms**. Earlier full-suite counts remain historical. See [task and AI manual acceptance steps](Project_GRID_Tasks_and_AI_Acceptance_Test.md). Agent dialogue integration remains planned for 6.2.


## Shared local AI — 6.1 completed (2026-10-22)

GRID now has a bounded shared local inference queue and a fixed-loopback Ollama adapter using installed Qwen3 4B. The dedicated server uses port 11435 with cloud disabled; the existing personal Ollama instance is unchanged. `ai status/test` provides original-host Master Control diagnostics and sampled CPU/memory/GPU allocation. **620 tests passed**; the real C# readiness probe returned GRID LOCAL READY in 3.232 seconds. Conversations remain deterministic until 6.2; no automatic task/tool execution, model context ingestion or schema change (v20). Runtime/model packaging inside Flash GRID remains planned; this installation uses the external Ollama cache. See [setup, licensing, limits and verification](Project_GRID_Shared_Local_AI_Implementation.md). Earlier milestone/schema/count statements below remain historical.


## Bulk tasks — 5.8 completed (2026-10-22)

Agent-filtered lists and `tasks approve/run/clear -all [-AGENT]` are implemented. Bulk mutations/dispatch require original-host Master Control; approval and clear require reviewed interactive confirmation. Batches capture fixed selections, revalidate state/authority and stop dispatch on the first unsuccessful attempt. Clear archives records and cancels pending work, retaining audit/results and MCP/RELAY links; active/unsettled work is refused. `tasks list -archived` reads retained history. **Schema v20; 603 tests passed** (14 Core, 157 Simulation, 97 Persistence, 335 Terminal). Archiving does not free the 256-task history capacity. See [commands, verification and limits](Project_GRID_Bulk_Task_Implementation.md). Earlier milestone/schema/count statements below remain historical.


## Transfer command rename — 4.2F completed (2026-10-22)

Use `grid -copyto "location"` or `grid -transferto "location"` inside the GRID terminal. `copexist` and `tranexist` now only show migration guidance and never execute a transfer. Existing Master Control/original-host restrictions, preview, confirmation, final save and verified move/copy safeguards remain. **582 tests passed** (14 Core, 157 Simulation, 97 Persistence, 314 Terminal). No schema change or user-world relocation. See [transfer behavior and limits](Project_GRID_Transfer_Implementation.md). Earlier command names and counts below describe historical milestones.


## Conversation windows — 4.2E delivered (2026-10-21)

Default `connect` and `resume` now open dedicated attached conversation windows. Agent replies animate with a skippable typewriter effect; `typewriter off` and `GRID_TYPEWRITER=off` provide instant text. Exit detaches without ending the saved conversation; parent revocation closes access. Explicit `connect MCP -inline` / `resume ID -inline` retains the original-host voice workflow and instant text. **572 tests passed** (14 Core, 157 Simulation, 97 Persistence, 304 Terminal). Schema remains v19; no AI/meeting implementation is implied. Multi-agent integration remains pending in 6.2A. See [implementation and acceptance test](Project_GRID_Conversation_Windows_Implementation.md). Earlier milestone statements below remain historical.


## End-to-end task verification — 5.7 completed (2026-10-21)

MCP delegation, human approval, real MAXIS unit-test execution and RELAY recorded-result delivery are verified together, including actual passing/failing TRX evidence and SQLite reload. Nine new cases also cover all six agent roles, cancellation, durable-start recovery and duplicate dispatch prevention. **560 tests passed** (14 Core, 157 Simulation, 97 Persistence, 292 Terminal), zero failed/skipped. No runtime/schema change was needed; schema remains v19. See [verification matrix, boundaries and terminal test](Project_GRID_End_to_End_Task_Verification.md). Next: **6.1 Shared local AI**. Earlier milestone counts and next-step statements below are historical.


## VOLT and RELAY — 5.6 completed (2026-10-21)

VOLT now reports existing host telemetry and supports approved normal/eco/auto pacing of GRID's automatic simulation. Unknown/unsupported GPU data is preserved; Windows resources and manual tick behavior are unchanged. RELAY provides a saved local inbox, recorded-task briefings, human acknowledgements and explicitly approved offline speech with delivery/failure receipts. Schema v19 preserves mode and inbox history without automatic playback/replay. **551 tests passed** (14 Core, 157 Simulation, 97 Persistence, 283 Terminal). See [VOLT/RELAY implementation and acceptance test](Project_GRID_VOLT_RELAY_Implementation.md) for commands and boundaries. Next: **5.7 end-to-end verification**. Earlier milestone/schema/test statements below remain historical.


## JN storage — 5.5 completed (2026-10-21)

JN now inventories approved paths, reports possible duplicates, and prepares cleanup proposals without deleting source files. Defined GRID-managed storage is measured against an initial 20 GB decimal logical budget; MCP quota escalations require separate Master Control approval to change it. This is tracking/governance, not a hard write cap or disk reservation. Schema v18 preserves scope and quota history. **535 tests passed** (14 Core, 157 Simulation, 97 Persistence, 267 Terminal). See [JN implementation and acceptance test](Project_GRID_JN_Storage_Implementation.md) for limits, accounting roots, report retention and remaining planned automation. Next: **5.6 VOLT and RELAY**. Earlier milestone/schema/test statements below remain historical.


## SENTINEL security — 5.4 completed (2026-10-21)

SENTINEL now reads local Defender protection/scan timestamps, effective firewall profiles, and bounded Security-log metadata through individually approved tasks. Timestamped results explicitly distinguish real host observations, warnings and unavailable data from simulated incidents. No scans, setting changes, elevation or remediation are enabled. Schema v17 persists these results through the existing broker. **518 tests passed** (14 Core, 157 Simulation, 97 Persistence, 250 Terminal), including 13 new security cases and live read-only provider verification. See [SENTINEL implementation](Project_GRID_SENTINEL_Security_Implementation.md). Next: **5.5 JN — Storage and files**. Earlier milestone/schema/test statements below remain historical.


## MAXIS development — 5.3 completed (2026-10-21)

MAXIS now supports registered-workspace inspection, fixed .NET build/test execution, saved diagnostics and explicit replacement suggestions staged outside original source. Every task requires human approval; build/test executes trusted project code as the current Windows user (no OS/network sandbox). Schema v16 persists immutable, revocable workspace registrations. Other specialists remain demonstrations. **505 tests passed** (14 Core, 157 Simulation, 97 Persistence, 237 Terminal). See [MAXIS implementation](Project_GRID_MAXIS_Development_Implementation.md) for commands, offline prerequisites, artifact retention and limits. Next: **5.4 SENTINEL**. Earlier milestone/schema/test statements below remain historical.


## MCP administration — 5.2 completed (2026-10-21)

Explicit `mcp request` clauses now create atomic saved specialist task plans. MCP reports actual child states/results; each child requires human approval and original-host dispatch. Adapters remain demonstrations. Natural-language planning and real specialist actions remain planned. Schema v15 preserves plan links and existing cities. **485 tests passed**, including 13 new coordination cases. Next: **5.3 MAXIS — Development**. See [MCP coordination implementation](Project_GRID_MCP_Coordination_Implementation.md). Earlier milestone statements and schema/test counts below remain historical.


## Agent/task infrastructure - 5.1 completed (2026-10-21)

Explicit `agents initialize` registers MCP, MAXIS, SENTINEL, JN, VOLT and RELAY by stable ID. Schema v14 persists a bounded task queue, immutable proposals, one-attempt human approvals, results and audit. `tasks propose/inspect/approve/cancel/retry/run` enforce role-specific allowlists and live session authority at the service boundary. The broker saves a running record before invocation, prevents concurrent dispatch, supports cancellation/timeouts and requires fresh approval for retries/restart recovery. Agent sessions cannot self-approve or borrow capabilities. **Only labeled demonstration adapters are enabled; no real PC task, shell execution, natural-language delegation or autonomous producer is implemented.** Chat/voice messages remain conversation data.

**472 tests passed**, including 15 new task integration cases covering authorization, persistence barriers, cancellation/retry, deduplication, SQL rollback and restart/recovery. Existing v13 worlds migrate without regenerating the city or automatically creating agents. Next planned module: 5.2 MCP coordination. See [task broker implementation](Project_GRID_Task_Broker_Implementation.md) for commands, bounds and limits. Earlier milestone/schema/test counts below remain historical.


## System-agent names updated - 2026-10-21

ARCHON is now **MCP** (coordination), FORGE is now **MAXIS** (development), and ARCHIVIST is now **JN** (storage/files). Current commands, examples, plans and checklist labels use the new names. These names do not grant privileges or implement the planned specialist services. The human **Master Control** role remains separate from MCP.

On world load, legacy-named Agent residents are renamed without changing IDs, registrations, context grants, voice-profile links or conversation ownership. Conversation participant labels update; historical messages/sender labels and event history remain intact. The coordinator retains its original identity-generation seed to avoid duplicate residents. Changes persist on the next normal save. A conflicting target name or exhausted revision skips the rename and records `AgentRenameSkipped`; resolve by ID rather than merging identities. Ordinary application/civilian names and the generic Archivist occupation are unchanged.

Verification: 457 tests passed (14 Core, 157 Simulation, 97 Persistence, 189 Terminal), including new legacy-name/idempotence and collision cases. Earlier completion dates and test counts remain historical evidence; documentation examples now use current names.


## Communications verification - 4.5 completed (2026-10-21)

All seven 4.5 checklist items have automated coverage. **455 tests passed** (14 Core, 155 Simulation, 97 Persistence, 189 Terminal), including three new end-to-end tests for authenticated conversation/context recovery through SQLite, malformed input without transcript mutation, and unrelated-agent privacy with logout revocation. Existing suites cover recipient routing, durable history/recovery, reviewed speech/typed-message equivalence, audio failure fallback and deterministic communication without AI. No production-code/schema change was needed. Physical microphone/playback/device-unplug checks remain manual; advanced voice and task-delegation features remain planned. See [verification matrix and limits](Project_GRID_Communications_Verification.md). Earlier milestone counts below are historical.


## Voice transcription repair - 2026-10-21

A user-reported microphone transcription failure was reproduced with generated speech through the real C# helper connection: repeated synchronous `Recognize()` calls could reach an already-finished stream and throw `InvalidOperationException` (0x80131509), discarding recognized text. Recognition now uses `RecognizeAsync(Multiple)` and waits for `RecognizeCompleted`, gathering recognized phrases until completion. Speech/audio remain local and in memory. Explicit typed constructors avoid PowerShell overload ambiguity.

Failures now report a bounded stage, exception type and HRESULT rather than hiding the error or exposing raw helper stderr/audio. Capture reports duration and rejects recordings shorter than 0.25 seconds with a hold-Space hint. `voice 0` is a shortcut for `voice microphone 0`; no-draft hints recognize an already-connected conversation, and fallback advice is no longer duplicated.

**Verification:** 186 terminal tests pass, including four new real silent-PCM/generated-speech/invalid-audio and UI shortcut cases. The generated-speech case reproduced the failure before the completion-event fix and passes after it. No physical microphone recording or speaker playback was used; physical-device verification remains interactive. Earlier 448-test completion counts below are the original milestone's history.


## Offline voice - 4.4 Windows MVP completed (2026-10-21)

The original interactive host supports `voice devices/microphone/language/listen/review/edit/send/cancel/mute/speak/profile`. Hold Space to record (20-second maximum), release to transcribe with an installed offline Windows recognizer, review/correct, then explicitly send. Typed and microphone messages use the same dialogue service; speech never executes terminal commands. Playback starts muted and occurs only on explicit request. Saved registered-Agent profiles select Windows voice/rate/volume. Escape interrupts; identity/conversation changes prevent stale sends. Raw audio stays in transient memory and is not saved. Missing hardware or language/voice assets leave typed chat available.

Profiles are an additive validated field in existing schema-v13 communication JSON; legacy JSON defaults to no profiles. Restart and recovery preserve profiles and sent microphone-origin messages, not drafts/audio/device selection/mute state. Windows System.Speech is the implemented provider; whisper.cpp/Piper, attached-terminal audio, output-device selection and advanced voice options remain planned. 448 tests passed (18 new voice cases); real offline provider inventory succeeded. Physical microphone/speaker operation still needs an interactive smoke check. Earlier voice-as-planned statements below describe prior milestones. See [offline voice implementation](Project_GRID_Offline_Voice_Implementation.md).


## Context convenience - 4.3A completed (2026-10-21)

`context add -search` now opens a local Windows multi-file picker (default: temporary, `Inbox`, owner only); optional retention/project/registered-Agent arguments are supported. Read/revoke/remove accept exact filenames or IDs. Multi-file `context read` and session-local `context select` support reading several captures together. Names must be unambiguous; reads recheck access and selections clear on identity change. Picker completion revalidates the session; batches validate before import and persistent batches save once. Main and attached terminals use the same host-owned context service, with no new schema or network requirement. Drag/drop and remote uploads remain unimplemented.

Completion verification: 430 tests passed, including 10 new 4.3A tests. Automated tests inject dialog results; visible Windows dialog interaction still needs manual verification. See the context implementation/help for command syntax and limits. Prior 4.3 notes below describe the original milestone.


## User-supplied context - 4.3 completed (2026-10-20)

Implemented explicit local `context add/list/read/revoke/remove` with project names, source SHA-256/provenance, temporary or persistent retention, and optional registered-Agent grants. Master Control selects files; agents only read granted captures. Strict UTF-8 text/source and DOCX main-body text are supported with bounded parsing. Schema v13 stores persistent captures transactionally; temporary bodies are excluded from save/backup. `talk context` reports availability without executing/ingesting file contents. **420 tests passed.** See [context implementation and limits](Project_GRID_User_Supplied_Context_Implementation.md). Earlier dated schema/status notes below retain their historical scope.


## Password requirements - completed follow-up (2026-10-20)

**Implemented.** New passwords and password changes accept **6–24 characters inclusive**. Uppercase letters, lowercase letters, numbers and symbols are allowed; no character category or mixture is mandatory. Preserve password case and exact characters; do not silently trim or truncate passwords.

Creation/change validation, masked setup prompts, error messages and help now use **6–24 characters**. Existing saved credentials remain verifiable up to the former 256-character limit until the owner explicitly changes the password; the input reader retains that capacity for legacy login. Masked input, salted hashing and authorization are unchanged. Control characters remain rejected. **Completion verification: 390 tests passed** (14 Core, 155 Simulation, 91 Persistence, 130 Terminal), including 13 new cases for boundaries, permitted categories, exact case/spacing, confirmation mismatch, legacy 25/256-character login after restore, and authenticated rotation to the new policy. No saved credential migration or password reset is required.


## GRID move/copy commands - current-host implementation (2026-10-20)

Master Control `tranexist "location"` and `copexist "location"` are implemented for the running installation and selected save inventory. Both preview/confirm, stop workers, save and close. Copies retain the original; moves defer verified source cleanup until process exit. Destination packages use a local launcher/default save, require login and currently need an installed .NET 10 runtime. Verification: 377 passing tests. See [transfer implementation](Project_GRID_Transfer_Implementation.md) for exact exclusions, pending/incomplete status, security and recovery limits. Future attached-terminal and full PC/Flash edition integration remains unchecked in 4.2D/8.6.

## Additional windows - implemented 4.2C (2026-10-20)

`<command> -opninwin` opens permitted snapshot data or an updating `live` view in a separate window; `ntm` opens another terminal attached to the same running GRID. One host retains the engine/workers/save lease. Attached exit detaches; original-host exit disconnects attachments before saving. Sessions have independent context and scoped, revocable authority. NGE, transfers and credentials remain host-only. **404 tests passed**, including a real attached subprocess. See [implementation and limits](Project_GRID_Shared_Terminal_Windows_Implementation.md) and completed checklist 4.2C.

## Current 4.2B completion - 2026-10-20

Terminal UX and local password credentials are implemented and verified by 368 passing tests. Schema v12 preserves existing owners and adds authenticated credential migration/rotation. New worlds use interactive setup; existing owners use `login master`, then `credentials`. Read [4.2B implementation notes](Project_GRID_Terminal_UX_Implementation.md) for exact behavior and limits. Earlier 4.2A/schema v11 notes below describe their historical milestone. Full Flash GRID packaging remains planned.


## Installation editions and rendering - planned 2026-10-20

Installation will offer **PC GRID** (permanent machine installation, with advanced 3D options) or **Flash GRID** (mobile flash-drive package, with very simple lightweight 3D). The installer must explain the choice, validate the destination and retain the selected edition. Both share simulation/save/identity behavior and must run supported tasks offline after provisioning. Flash GRID keeps managed files with the portable package and requires no persistent host integration; graphics still run on the host PC. Quality differences must not regenerate or change the world.

See [edition details](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 7.6/8.6. Rendering budgets, packaging and migration remain planned; this addition changes documentation only.

## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all GRID runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 4.2B (implemented), 8.6 and 8.7 (planned). See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

## Local login and identity completion - 4.2A (2026-10-20)

**Implemented and verified.** Start with `login master`: the first successful Windows-token login binds the world owner; later logins must match. Sessions expire after eight hours. Master Control can register existing Agent identities, switch roles, and preview/confirm one-time program edits. Prompts/help/commands are role-aware; agent chats are participant-scoped and global/admin controls are denied. Switching/logout clears active chats and previews. Schema v11 preserves owner/agent registration without restoring authentication.

**362 tests passed**, plus isolated Windows login/agent restriction/confirmed edit/restart smoke. See [Local Login and Identity Implementation](Project_GRID_Local_Login_and_Identity_Implementation.md) for commands, service authorization, audit and security limits. This authenticates the current Windows account, grants no OS elevation, and provides no independent agent passwords or real task service. NGE now requires Master Control; --start waits for login. Earlier dated pending-login/editing statements refer to prior milestones.

## Terminal conversations completion - 4.2 (2026-10-20)

**Implemented and verified.** Use connect (default MCP), talk, conversations, transcript, resume and disconnect. MCP is created on first explicit connection as a persistent coordinator Agent; a stable world-local operator handle supports reopening saved conversations. Replies to status/who are you are labeled verified GRID state; other replies are labeled deterministic simulated dialogue and claim no task execution/delegation. Input/reply pairs commit atomically and reuse schema v10.

**353 tests passed**, plus a three-launch isolated terminal save/resume/end smoke. See [Terminal Conversations](Project_GRID_Terminal_Conversations.md) for syntax, identity/capacity limits and completion evidence. This local operator handle grants no authentication or admin authority; 4.2A login, real task delegation and LLM dialogue remain planned. Earlier dated pending-terminal-chat/agent-creation statements describe previous milestones.

## Communications foundation completion - 4.1 (2026-10-20)

**Implemented and verified.** Grid.Communication now defines recipient/message/conversation models and a local hub contract implemented by the simulation host. Exact name/full-ID routing, explicit participant identities, start/send/end/read operations, ordered bounded messages and schema v10 transactional transcript persistence are available. Conversations survive restart and backup recovery even when a historical application resident is absent; new sends require live original program identities.

**346 tests passed.** See [Communications Foundation](Project_GRID_Communication_Foundation.md) for APIs, identity boundaries, bounds and recovery evidence. These trusted local APIs store messages without authenticating the caller, executing text, claiming external delivery or generating replies. Terminal chat is still 4.2; voice, real delegation, LLM dialogue and production login remain planned. Earlier dated pending-communication/schema statements describe previous milestones.

## Autonomy completion - 3.4 (2026-10-20)

**Implemented and verified.** The engine composes resource/shift/work/rest/travel and social decisions without an LLM. Equally ranked social choices now use a stable rank seeded by saved world identity, round and participant IDs; restore retains reproducibility without extra random state. Schema v9 gates this policy change. Preferences, affinity and world rules still take precedence.

**338 tests passed.** An accelerated one-hour simulation covered eight persistent agents, zero-to-512 fake application churn, resource scarcity/recovery, valid movement/events and concurrent snapshot readers. CPU, memory and latency measurements are recorded in [Autonomy and Verification](Project_GRID_Autonomy_and_Verification.md). This is a simulated-hour test, not a real-time hour-long production soak. Host applications remain observation-only; automatic agent creation, admin configuration, real task execution and Phase 4 communications remain planned. Previous dated pending-autonomy/schema statements describe earlier milestones.

## Social civilization completion - 3.3 (2026-10-20)

**Implemented and verified.** Explicitly opted-in Agent/Special residents now have cooperative, competitive or reserved preferences; bounded relationships, memories and occupation history; and simulated district civic tokens with scarcity/recovery. Decisions run on simulation time, respect current work/travel and commit atomically with ticks. Host applications remain observation-only and receive no fictional social activity or resource consumption.

Schema v8 persists social state and partial rounds, upgrades existing worlds without inventing participants, and supports validated backup recovery. `social [id|name]` and program inspection provide read-only views. Trusted engine APIs configure social participation/pools; production login, terminal admin configuration, automatic agent creation, actual communication/delegation and Phase 3.4 sustained autonomy remain future work. **336 tests passed**, plus isolated terminal create/save/reopen. See [Social Civilization](Project_GRID_Social_Civilization.md) for complete rules, limits and evidence. Earlier dated statements that social functionality is pending describe previous milestones.

## Movement and occupations completion - 3.2 (2026-10-20)

**Implemented and verified.** Saved city roads now support deterministic district travel for explicitly configured Agent/Special residents assigned to compatible capacity-one workstations. Five simulated seconds per edge; no work progress while traveling; shift/resource uncertainty freezes travel. Trusted assignment/release APIs preserve world ownership and revision checks. `jobs` and `activity` add read-only workstation and measured application views. Host applications acquire no fictional civic jobs.

Schema v7 preserves station assignments, remaining routes and edge timing in existing behavior JSON, validates them on restore and performs no offline catch-up. Older unassigned routines retain their 3.1 behavior. No agent population or interactive admin job configuration is added. **323 tests passed** in isolated Verification builds; disposable jobs/activity/save/reopen smoke passed. A running user Release session locked its binaries and was left untouched; rebuild the normal target after closing it to use the changes. See [Movement and Occupations](Project_GRID_Movement_and_Occupations.md) for policy, limits and full completion evidence. Earlier statements that movement/workplaces are pending refer to previous milestones.

## Bare Grid7736 command support - 2026-10-20

Startup now also installs a managed `Grid7736.cmd` launcher in `%LOCALAPPDATA%\GRID\bin` and adds that directory to the Windows user PATH, preserving existing entries and avoiding duplicates. Type `Grid7736` directly in PowerShell or Command Prompt; arguments such as `--start` are forwarded. The persistent `Grid7736` environment variable remains supported. An existing unmanaged launcher is not overwritten. This extends the original variable-only 1.14 behavior.

Restart the parent terminal app to inherit the new PATH. For the currently open PowerShell session, refresh it without closing the window:

```powershell
$env:Path += ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
Grid7736
```

Verification: **89 terminal tests passed**. The bare command resolved to the installed launcher and successfully ran/exited with `--ephemeral --no-color`; no saved world was opened.

## Startup launcher environment variable - 1.14 (2026-10-20)

**Implemented and verified.** On Windows startup, GRID ensures a persistent **user-scope** environment variable named `Grid7736` points to the terminal `.exe`. A missing, empty or broken path is registered using the current build's apphost; an existing valid absolute path is preserved, including another installed build. The current GRID process receives the same value. Values are stored without shell quotes, so paths with spaces remain usable. If no executable is available or Windows denies the environment update, startup reports a warning and GRID continues. Other platforms skip Windows registration. No administrator elevation, shell-profile changes or automatic simulation start are required. The follow-up above also adds a user-PATH launcher.

PowerShell launch (from a fresh environment):

```powershell
& $env:Grid7736
# Optional: begin simulation immediately
& $env:Grid7736 --start
```

Command Prompt launch:

```bat
"%Grid7736%"
```

The environment variable is a path value; the managed PATH launcher above separately provides the bare `Grid7736` command. Already-open shells do not inherit changes from GRID. Restart the parent terminal/IDE, or sign out/in if necessary. To refresh only the current PowerShell shell without restarting:

```powershell
$env:Grid7736 = [Environment]::GetEnvironmentVariable('Grid7736', 'User')
```

A development-build path remains tied to that build's `.exe`; rebuild that target to pick up changes. Launching another build preserves a still-valid registered installation. If the target is removed, launching GRID directly registers the current available apphost again. This is a convenience launcher, not an installer or authentication mechanism.

**Verification:** 87 terminal tests passed, including missing/empty/broken values, paths with spaces, preservation of an existing installation and rejection of a dotnet-host launcher. An ephemeral Release launch registered the actual Windows user variable and verified that its executable exists. No saved world was opened or modified. Current verified value: `C:\Users\jack\source\repos\Grid7736\Grid.Terminal\bin\Release\net10.0\Grid.Terminal.exe`.

## NGE implementation - 1.13 (2026-10-19)

**Initial local-terminal NGE implemented.** Type `nge` in an interactive saved-world session, review the world identity and exact file list, then enter `y` to confirm or `n`/blank to cancel. Invalid answers reprompt; EOF cancels deletion and follows normal EOF shutdown; Ctrl+C cancels deletion and follows normal graceful shutdown. Arguments such as `nge y` are rejected. Redirected/unattended input and ephemeral sessions cannot delete saves. This is local-operator confirmation, **not authenticated admin enforcement**; human-admin-only access remains future login/authorization work.

Confirmed NGE stops/awaits scheduler, autosave and telemetry, serializes against in-flight saves, revalidates world/backup identity, then removes only the active database, rolling `.backup` and exact `-journal`, `-wal`, `-shm` paths. It retains the session lease through completion and refuses symbolic-link/junction targets. Historical legacy/damaged archives, staging artifacts of uncertain ownership, external copies, other worlds and PC applications remain; the warning explicitly states this scope. The harmless session `.lock` file may remain after release. Interrupted recovery must be resolved before NGE can be prepared.

A flushed `.nullifying` marker is created before deletion. Partial failure retains that marker, blocks startup/save/recovery, reports remaining files and exits nonzero without a final save. **There is no automatic retry/resume command yet:** inspect the marker and remaining exact world artifacts before explicit manual cleanup; never remove the marker merely to bypass an unresolved operation. Successful NGE removes the marker and exits with the GRID7736 artwork and `GRID NULLIFIED // EXISTENCE TERMINATED`, never a saved-world claim. Next normal startup follows existing hardware-city creation; it does not reuse a managed backup. Independent historical/manual copies remain outside this deletion scope.

Verification: **306 Release tests passed** (14 Core, 127 Simulation, 83 Persistence, 82 Terminal). New coverage checks cancellation/blank/invalid/EOF input, uppercase confirmation, redirected/ephemeral rejection, worker shutdown before deletion, no final save on success/failure, owned-file deletion, unrelated-file preservation, rejected forged plans, blocked future writes/recovery, new identity after reopen and a Windows locked-file failure retaining the marker. All deletion tests use freshly created disposable worlds; no user save was deleted. Existing hardware-city creation tests cover the startup path; no manual destructive run against the user's world was performed. Visual Studio still owns its post-exit footer.

## Closing artwork and default color - 2026-10-19

The farewell now uses the user's supplied large GRID7736 ASCII artwork and blank-line spacing. Successful persistent exits retain `Stopping GRID.`, `World saved.`, then the artwork and `WORLD SAVED // USER DISCONNECTED`. Error and ephemeral messages remain truthful. Consoles narrower than 78 columns use a compact GRID7736 farewell. Ordinary terminal output and input echo default to light blue (bright cyan / ConsoleColor.Cyan); status, warning and error accents remain. The prior shell foreground is restored on exit. `--no-color`, NO_COLOR, TERM=dumb and redirected output continue to disable application color changes. Terminal verification: **75 tests passed**; no claim of visual color matching across user console themes.

## Terminal commands and shutdown complete - 1.12 (2026-10-19)

Implemented grouped `help`, display-only `clear`, `programs -active`, `programs -possible`, `programs -sleeping`, and a GRID7736 ASCII farewell. Active selects fresh Running applications; sleeping selects fresh On call or Sleeping applications; possible includes current applications plus the saved known-application catalog. `ids` works with one filter. Unknown/stale observations are never classified as active or sleeping. Bare `programs` still includes agents and other residents.

**Schema v6** adds a separate ApplicationCatalog table (key, name, source, last catalog observation UTC). The catalog is captured with the simulation snapshot and saved atomically with world/history; backup/recovery and schema validation include it. Installed/observed metadata survives restart; ordinary residents, PID lifetimes, activity and resource readings are not restored. Last observation is catalog provenance, not proof of a launch or current installation. Entries missing from fresh discovery display Historical / unknown. Existing schema-v5-only statements below describe earlier milestones.

The ASCII banner appears after graceful shutdown and final save. Save/shutdown errors retain diagnostics and a nonzero exit code without a success message; ephemeral sessions explicitly say NOT SAVED. There is no keypress wait. Visual Studio can still append its own debug-console footer. Help uses portable ASCII separators and registered command metadata, including extensions; no-color/redirection behavior is preserved.

**Verification:** `dotnet test -c Release --no-restore`: **297 passed** (14 Core, 127 Simulation, 81 Persistence, 75 Terminal). Tests include filters/IDs/invalid flags/empty results, clear preserving the world and tick, successful/failed farewell, catalog save/reopen/recovery and v5-to-v6 migration. A disposable built-terminal run exercised all command groups, filters, redirected clear fallback and successful save/reopen of the same city. An ephemeral PTY run preserved tick1 through clear and displayed the NOT SAVED banner. This tool's PTY uses the clear fallback; physical screen clearing/color appearance and Visual Studio footer presentation were not visually verified in the user's console. No IDE settings or existing user worlds were modified.

## Latest population revision - 2026-10-19

The current population includes discoverable installed applications and current-session background processes. States are **Sleeping**, **On call**, **Running**, or **unknown** when evidence is insufficient. This supersedes earlier windowed-only/removal-on-exit descriptions below; those remain historical completion records. Ordinary application state is still rediscovered rather than persisted, and the saved city is reused. Full-machine coverage is not complete: see [Installed Application Availability](Project_GRID_Installed_Application_Availability.md) for implemented scope, Windows inventory limits, remaining work and **290-test** verification.

## Terminal usability completion - 1.11 (2026-10-19)

Implemented aligned/wrapped tables for programs, districts, resources, behaviors and history. Narrow output falls back to labeled records; empty tables are explicit. `programs ids` and `districts ids` expose full identifiers; `resources details`, `history details` and `inspect` retain detailed provenance. Live view retains paging and resize handling, with prominent Esc/Q Back hints and a return-to-prompt message.

Colors supplement labels: green for running/available/work, yellow for unknown/stale/waiting/warnings, red for errors, cyan for navigation/table headings; default terminal color otherwise. Use `--no-color`, `NO_COLOR`, or `TERM=dumb` for monochrome. Redirected output suppresses application color changes. Compact tables sanitize controls and use ASCII-safe text; original names remain available in inspection.

Esc/Q return to commands; `exit` or Ctrl+C shuts down. Automated tests cover repeated live/prompt round trips, same-world and clock preservation, no save/dispose on Back, final save on exit, width20/60/120, long values, empty data, color policy and plain output. Existing tests cover resize, paging and high event volume. The actual PTY smoke used the single-frame fallback because this tool's terminal did not meet interactive redraw requirements; it returned to a Running prompt and advanced to tick5. Full-screen visual color/contrast assessment in the user's terminal remains a manual check, not claimed as verified. Combined suite: **290 tests passed**.

## Program behavior completion - 2026-10-19

**3.1 implemented and verified.** Non-demo residents now have deterministic needs/goals/actions. Applications reflect fresh or unknown observations; explicitly configured Agent/Special residents perform resource-gated simulated occupation work/rest on simulation-time shifts. Schema v5 persists their routines, sourced budgets and progress without restoring live observations or adding offline work. Use behaviors and inspect for last-tick behavior. No OS task execution, movement or named agent services are claimed.

See [Program Needs and Behavior](Project_GRID_Program_Behavior.md) for policy, APIs, limits and completion evidence: **278 Release tests passed**, build zero warnings/errors, disposable terminal smoke passed. Debug output was locked by running user sessions, which were left running. Earlier dated deferred-behavior/schema statements describe previous milestones; existing history and completed scope are preserved.

## Application resident completion - 2026-10-19

**2.7 / 2.7.1 implemented and verified:** production population now follows current-session windowed executable groups through read-only discovery and serialized lifecycle reconciliation. Application residents are transient; Agent/Special residents persist in schema v4. Before production conversion, legacy fictional citizens are preserved in a uniquely named validated archive. City/layout and clock remain intact. Unknown resource requirements display unknown condition; no host-control authority is implied.

Verification: **268 tests passed**, build zero warnings/errors, and a disposable Windows startup/reopen detected nine application groups. See [Application Residents Implementation](Project_GRID_Application_Residents_Implementation.md) for grouping limits, discovery/freshness, storage/migration, editing, resource interpretation and completion evidence. Earlier dated statements that 2.7 is planned, production cities stay empty, or all residents persist describe previous milestones. Agent delegation/services, login, rendering and additional per-application telemetry remain future work.

## Hardware city completion - 2026-10-19

**2.6 / GRID-REQ-004 implemented and verified.** First-time creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates bounded district/street/building-lot geometry and saves actual layout and provenance in schema v3. Later startups reuse the saved world without discovery or regeneration. Existing worlds are preserved; new generated cities start empty pending 2.7. Inventory is creation provenance, separate from live resource telemetry and application identity. Rendering, atmosphere, fashion and application-resident integration remain planned.

Verification: **250 tests passed** (14 Core, 106 Simulation, 74 Persistence, 56 Terminal); build had zero warnings/errors. A fresh Windows city and unchanged reopen were verified in a temporary directory. Use city to inspect saved generation details; --world with a new filename explicitly creates a separate world.

See [Hardware City Generation](Project_GRID_Hardware_City_Generation.md) for architecture, mappings, fallbacks, persistence and completion evidence. Earlier dated statements that hardware generation is planned or that normal creation always makes twenty fictional citizens describe previous milestones; this update supersedes those status statements while preserving their history.

## Phase 2 verification update - 2026-10-19

Section 2.5 is complete for the current implementation. **231 tests passed** (8 Core, 106 Simulation, 64 Persistence, 53 Terminal), with zero build warnings/errors. Eight new isolated cases verify headless terminal save/reopen/recovery, populated older-schema restoration, no offline catch-up, invalid/missing-file preservation and abrupt writer termination with SQLite journal recovery. A test-only helper under TestSupport is built by the persistence tests; production projects do not reference it.

See [Phase 2 Verification](Project_GRID_Phase_2_Verification.md) for evidence and limits. Earlier test counts remain historical. Process-crash verification does not claim physical power-loss coverage; pending application energy/classification/lifecycle work and hardware-generated cities remain unimplemented. No production behavior was changed for 2.5.


## Resource/host foundation update - 2026-10-19

Section 2.4 now provides read-only host observations, explicit units/sources/freshness, fake-provider tests and a provenance-required additional-work resource-condition policy. Use resources for details or live for a compact CPU/RAM summary. Unsupported measurements and unknown application requirements remain unknown. Current observations are transient and cannot be edited into Windows allocations or restored from a save as fresh data.

Verification: 223 tests passed, build zero warnings/errors, and real Windows disposable-session sampling passed while simulation time stayed paused. See [Resources and Host State](Project_GRID_Resources_and_Host_State.md) for collector coverage, the exact policy and limitations. Application discovery/classification, per-application resource attribution and replacing application energy drain remain dependent on 2.7. Existing fictional Genesis citizens retain their old rules. Earlier deferred-telemetry statements below describe prior milestones, not this update.


## History implementation update - 2026-10-18

Section 2.3 now persists meaningful committed changes with world snapshots in schema v2. The history command supports entity/kind/severity/time filters and bounded paging. Routine Tick records are excluded; pending-buffer overflow is reported explicitly. Changing edits include attribution/context, but denied attempts and no-ops are not a comprehensive security audit. Structured operational errors are emitted to terminal output. See [History and Observability](Project_GRID_History_and_Observability.md) for retention, failure and API details.

Verification: 208 tests passed; solution build had zero warnings/errors; isolated terminal restart/query smoke passed. Earlier schema-v1 and deferred-history statements below describe the prior milestones. Live-view history remains process-local; the new saved-history path is separate. Application discovery, resource interpretation, resident classification and persistence filtering remain planned in 2.4/2.7.


## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

Documentation now targets transient residents for running PC applications, persistent agents/special programs, and resource-based energy. None of that new behavior is implemented yet. Runtime still uses twenty fictional citizens, saves them all and applies fixed Genesis energy rates. The 200-test result below verifies 2.2, not this new requirement.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


**Reviewed:** 2026-10-17. Covers local HEAD `0b659e6` and the current working tree, including uncommitted changes. Remote synchronization was not checked.

## Persistence architecture update - 2026-10-18

Section 2.1 is complete: SQLite schema v1, explicit initialization, existing-file validation, foreign keys and atomic migrations. At the 2.1 milestone, 171 tests passed (including 31 persistence tests), with zero build warnings/errors. Section 2.2 subsequently completed engine save/load, snapshots and recovery; durable audit and GRID-REQ-004 remain pending. See [Persistence Architecture](Project_GRID_Persistence_Architecture.md). Earlier totals below describe their original verification milestones.

## Live-view update - 2026-10-18

Section 1.10 is implemented. The full solution now passes 140 tests (8 Core, 93 Simulation, 39 Terminal), and builds with zero warnings/errors. Historical 117-test evidence below remains dated to 1.9.3. See [Terminal Live View](Project_GRID_Terminal_Live_View.md) for controls, limitations and verification. No login, persistence or hardware-generated city functionality is added.

## Release and completion record

`v0.1.0-genesis` identifies commit `0c1de64`, the original verified Genesis release (69 tests at that milestone). Later changes do not move or alter that tag. Current verification totals supersede historical counts only for the current tree.

| Checklist area | Current completion |
|---|---|
| 1.1-1.3 | Repository, solution/project references and domain models complete; historical commit verification is in the master checklist. |
| 1.4 | World ownership, district membership, transfers and deterministic five-district/twenty-citizen factory complete. |
| 1.5 | Fixed-step clock, start/pause, manual steps and elapsed simulation time complete. |
| 1.6 | Serialized energy updates, scheduler, cancellation, tick events and snapshots complete. |
| 1.7-1.8 | Extensible terminal and original Genesis verification/tag complete. Optional automatic startup added after release. |
| 1.9.1-1.9.2 | Editing rules and identity/session authorization design complete. |
| 1.9.3 | Validated administrative editing API, revisions and authorization lease boundary implemented and tested. |
| 1.10 | Live status, bounded recent events, paging and console fallback implemented 2026-10-18; session visibility awaits login. |

## Runtime and startup

The .NET 10 solution `Grid7736.slnx` contains Grid.Core, Grid.Simulation, Grid.Terminal and Grid.Persistence, plus a matching test project for each. The factory uses seed 7736 and creates twenty citizens distributed across five districts, initially Idle with energy 100. Normal launches create and save Genesis once, then reload `%LOCALAPPDATA%\GRID\world.db`. Use --world <path> for a separate database, --recover for explicit validated backup restoration, or --ephemeral for a disposable in-memory world.

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj
dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start
```

The first command starts paused; the second enables automatic ticking immediately. The flag is case-insensitive. Normal terminal commands are `help`, `status`, `programs`, `districts`, `inspect`, `start`, `pause`, `live`, `step`/`tick`, and `exit`. There is no login or edit command yet. Enter `live` for updating state/events; Q/Esc returns to commands without changing running/paused state. Exit, EOF and Ctrl+C cancel and await the scheduler and autosave before a final save. Autosave runs every 30 seconds; failures are visible and return a nonzero result. A blocked console read can remain on a background thread until input or process exit. Scheduler failures are reported with a nonzero result.

The default tick duration is one simulation second. Working and Moving consume one energy unit per second; Recharging gains two; Idle and Offline leave energy unchanged. Values clamp to 0-100 on ticks. Status alone does not launch a job, travel route or Windows task. The scheduler advances once per delay without catching up missed wall-clock time, permits one runner and rejects external Tick calls while that runner owns automatic ticking. Manual Step requires a paused clock. Snapshots copy consistent read-only state under the world/engine lock.

## Changes after the Genesis release

World-owned program values now reject direct setter writes. Initialization before ownership remains supported; it is not a complete authenticated creation API. Internal mutation access is granted from Core to Simulation. GridWorld exposes an instance ID, and programs/snapshots expose a revision. Changing commits increment revision once, including tick-driven energy changes and world transfers; no-op transactions do not. Revision overflow is checked before mutation.

`GridSimulationEngine.PrepareEdit(session, request, cancellationToken)` and `ApplyEdit(session, preview, cancellationToken)` use typed contracts in `Grid.Simulation.Editing`. `EditableFields` exposes the explicit field registry. Preparation validates and stages the whole request without mutation. Apply reauthorizes, validates engine/world/operator/session bindings and target revision, then atomically commits all changes under the shared world lock. Unrelated program changes do not cause conflicts; any target value change does, even if later reversed. Successful apply and no-op apply consume the preview once.

Initial fields are name, occupation, energy, status and district. Name/occupation trim whitespace, reject controls and enforce 80/120 UTF-16-unit limits. Energy must be finite and in 0-100; decimal input has a 1024-character limit, forbids exponents/thousands separators and checks exact bounds before float conversion. Invariant formatting expands scientific notation to parser-compatible decimal. Status accepts defined names, not numeric enums. District must exist in the same world and transfers preserve membership/population. Unknown or duplicate fields fail the whole request.

Outcomes are Prepared, Applied, NoChange, ValidationFailed, NotFound, Forbidden, Conflict and Cancelled. Cancellation is checked before commit, never partway through mutation. An edit neither advances simulation time nor overwrites LastTick. Receipts contain request/operator/session attribution, target, old/new values, revisions, simulation tick/time and UTC timestamp. They are returned in memory; no durable audit or event buffer is implied.

One-time assignments do not pin values: the next tick can change edited energy. Labels and occupations do not grant permissions. Field registration is a trusted typed extension point with parser, normalization, read and staged-value functions; adding a domain field also requires mutation support and tests. Terminal commands have their own explicit registration mechanism. Neither registry loads arbitrary plugins.

## Authorization boundary and deferred login

The engine accepts an optional IProgramEditAuthorizer. Its default denies administrative edits. A trusted provider must resolve current authentication, programs.edit permission, world scope and session generation, returning an IProgramEditLease that holds authorization valid through the operation. Lock order is authorization lease, then engine/world lock. The engine additionally checks human Master Control identity and preview bindings. Role strings supplied by callers are not authentication.

TestSupport supplies a fake provider compiled only into test projects. Production has no credential store, login provider, session expiry implementation or general session service. Existing Genesis commands remain unauthenticated; the new editing API fails closed. Future agent login is an operator view with separate human attribution and active-agent permissions, without implicit administrator authority or takeover of autonomous work.

## Events, persistence and next work

Typed tick events include energy changes and tick completion. LastTick retains the latest tick; a separate bounded recent-event buffer now retains committed events across ticks. Programs initially Idle produce no invented activity. Checklist 1.10 implements a 250 ms live view, a default 256-entry event buffer with stable sequence IDs and eviction counts, committed transfer/edit summaries, paging, Q/Esc return, resize/cancellation handling and one-snapshot console fallback. Visibility filtering remains future login integration. Rendering must not drive simulation ticks.

Sections 2.1-2.2 now provide a persistent world, consistent snapshots and recovery. Durable events/audit remain 2.3 work. Offline catch-up is deliberately excluded; undo, persistent overrides and bulk multi-program editing are not implemented. The 20 GB quota remains a planning budget. Autonomous jobs, host telemetry/tasks, named-agent services, conversations, uploads, AI, voice, Godot/WPF clients and Windows startup integration remain later phases.

## Verification evidence

On 2026-10-17, after the --start change, `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false` passed **117 tests**, with zero failures or skips: Core 8, Simulation 85, Terminal 24. Simulation includes 48 editing cases covering validation, decimal boundaries, atomicity, membership, no-ops, revisions, stale/replayed/foreign previews, cancellation, authorization denial, generation changes, revocation ordering, typed extension and concurrency with ticks.

The previously recorded solution and terminal builds succeeded with zero warnings/errors. Startup smoke verification reported Running with --start; default status/step/inspect/exit smoke verification also passed. These checks establish current implementation behavior, not completion of future login; later persistence evidence is recorded below. Live-view verification was subsequently completed on 2026-10-18.

## Documentation map

- [README](../README.md)
- [City Design Style Guide](Project_GRID_City_Design_Style_Guide.md)
- [Design Specification](Project_GRID_Design_Specification.md)
- [Development and Languages Addendum](Project_GRID_Development_and_Languages_Addendum.md)
- [Environment Atmosphere Style Guide](Project_GRID_Environment_Atmosphere_Style_Guide.md)
- [Local First and Storage Addendum](Project_GRID_Local_First_and_Storage_Addendum.md)
- [Master Development Checklist](Project_GRID_Master_Development_Checklist.md)
- [Master Overview](Project_GRID_Master_Overview.md)
- [Program Editing Rules](Project_GRID_Program_Editing_Rules.md)
- [Program Fashion Style Guide](Project_GRID_Program_Fashion_Style_Guide.md)
- [Session Context and Authorization](Project_GRID_Session_Context_and_Authorization.md)
- [Terminal Identity and Master Control Addendum](Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md)

## Save and recovery completion - 2026-10-18

Section 2.2 is implemented. Normal startup creates and saves fixed Genesis once or validates and restores the existing world. Autosave runs every 30 seconds. Shutdown awaits the scheduler and periodic saver before the final save. World identities, program revisions, values, memberships, resources and clock state survive restart, with fresh runtime/session bindings and no offline advancement.

Verification: **200 tests passed**, zero failures/skips (8 Core, 93 Simulation, 50 Terminal, 49 Persistence). The solution build passed with zero warnings/errors. A temporary-world terminal smoke test verified creation, two steps, shutdown save, paused reopen at tick 2, corruption rejection and explicit --recover restoration. --ephemeral --start also passed. The user's default database was not used for verification. See [Persistence Architecture](Project_GRID_Persistence_Architecture.md) for operational details and limits; physical power-loss testing is not claimed.

Remaining work begins with 2.3 durable history/observability. Hardware discovery/generated cities remain 2.6, and login, terminal editing commands, archive retention, quota enforcement and Windows service startup remain planned.


## Existing-code adaptation plan - 2026-10-18

The existing implementation needs a planned retrofit of program classification, safe removal/reconciliation, production factory use, all-resident persistence, fixed energy rules, and editing/live-view field semantics. Clock behavior, transactional saves, backup recovery, command structure and consistent snapshots remain reusable. None of these retrofits is implemented by this note.

See [existing implementation retrofit](Project_GRID_Host_Programs_and_Resources.md#existing-implementation-retrofit---planned-2026-10-18) and checklist 2.7.1. Documentation only; implementation remains pending.

## Task and MCP readability follow-up — 2026-10-21

Implemented compact agent/task tables with role names, unique short references, a compact `mcp plans` overview, and separated plan/task details, results, next commands and full audit identities. `mcp brief` still includes all plan details; `mcp inspect <reference>` reads one plan. Full IDs remain in detail views. Displayed short references (at least eight characters) are accepted by task inspect/approve/cancel/retry and MCP inspect/brief; ambiguous prefixes are rejected, and resolution is restricted to the current authorized scope. A copied trailing period or comma is tolerated. Plan/task mixups produce specific guidance. Approval boundaries and persisted data are unchanged. Persistence warnings now appear only on I/O/database errors.

Use `tasks list`, then `tasks inspect <displayed-task-reference>`. Review its argument before using the displayed approval command. Command examples are printed on separate lines without sentence punctuation. Verification: 219 terminal tests passed, including short-reference, punctuation, plan/task mismatch and cross-agent scope checks.
