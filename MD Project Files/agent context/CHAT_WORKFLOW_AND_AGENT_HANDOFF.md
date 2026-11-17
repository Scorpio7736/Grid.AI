# Project GRID - Chat workflow and agent handoff

## Current starting point

Use the short [current handoff](CURRENT_HANDOFF.md) and [repository working rules](../../AGENTS.md) for new work. The 2026-11-12 efficiency update calls for selective reads, focused development tests, appropriate final verification and concise linked completion evidence. These working rules take precedence over older workflow guidance below where they differ. Historical implementation notes remain available here.

**6.27 storage/package baseline implemented — 2026-11-12.** Package validation now applies a 128,000,000,000-byte complete-footprint target, 75,000,000,000-byte model/dependency/cache sub-limit, and the existing 256,000,000-byte reserve. Transfer checks include saved data and staging. The builder publishes a verified sibling staging folder atomically and sorts the hash inventory. All 1,254 tests passed. A self-contained Windows x64 package was built, sealed and passed isolated local AI/advice/context/task-draft tests with global lookup paths and the user model cache removed from its test environment. Second-PC/network-blocked acceptance, specialist/image optional installs and repeat-build comparison remain open. [Implementation and acceptance](../Project_GRID_Offline_Portable_Deployment.md).

**6.26 performance baseline implemented — 2026-11-12.** Scoped advice/document analysis now uses a 30-second exact cache capped at 64 entries and 512 KiB of response content. Keys include identity, source revisions and model/persona settings, with live access checks. The terminal rechecks physical RAM before inference. All 1,243 tests passed. This PC passed the live Qwen benchmark with a 36.492-second first answer and ten cache hits each below one millisecond. Semantic caching, smallest-specialist selection and the full device benchmark matrix remain open. [Implementation, thresholds and evidence](../Project_GRID_Performance_Optimization.md).

**6.25 baseline recovery implemented — 2026-11-12.** The shared queue now uses a three-failure, thirty-second runtime cooldown and structured unavailable replies. Ollama server failures can trigger one verified-unload CPU/Lite attempt with the installed baseline inside the request deadline. All 1,230 tests passed, including five new recovery cases and existing interrupted-write/task/file recovery suites. Smaller specialist-model and physical resource-exhaustion acceptance remain pending installed candidates. [Recovery rules and evidence](../Project_GRID_Resilience_and_Recovery.md).

**6.24 completed — 2026-11-12.** Shared inference now withholds recognizable credential material and invalid control-character output. Strict proposal parsing rejects worker approval/scope expansion and uses content-free rejection messages. Format-repair retries recheck live authorization. All 1,225 solution tests passed, including twelve new cases and the joint 6.22–6.23 authorization/tool suites. Credential detection is pattern-based with documented false-positive and coverage limits. [Implementation and evidence](../Project_GRID_Layered_Guardrails.md).

**6.23 gateway acceptance completed — 2026-11-12.** Eleven added regression cases cover typed task/file gateway forgery, stale sessions, denied paths, request-key binding, failed-save retry approval, generated-output tampering and revoked artifact approval. All 1,213 solution tests passed (14 Core, 157 Simulation, 99 Persistence, 943 Terminal), including existing concurrent-edit, persistence/recovery, malformed-proposal and authorization tests. Gateway acceptance is complete for the implemented formats and host routes. Broader layered-guardrail work remains under 6.24, and conversation-only acceptance remains under 6.31. [Evidence and limits](../Project_GRID_Controlled_Tool_Gateway.md).

**6.23 implementation verification — 2026-11-07.** Typed tool requests use the existing host task/file services. MCP delegation has a one-level allowlist, eight-child maximum, one active job and a ten-minute dispatch budget. `mcp ledger` exposes saved child-task evidence. Reviewed DOCX/PDF report generation and strict PNG validation use hash-bound previews, guarded apply and durable rollback. All 1,202 solution tests passed. Native Word/PDF samples were rendered and visually checked. Joint 6.22–6.24 acceptance remains open for 6.24. [Supported formats, limits and tests](../Project_GRID_Controlled_Tool_Gateway.md).

**6.22 inference-route completion — 2026-11-05.** Meeting rounds, screen assistance, AI file drafts and diagnostics now share the existing chat/assistance authorization gate and per-owner quotas. `ai authorization` shows recent admission records for Master Control. Current suites total 1,191 passing tests. Joint action-route acceptance remains scheduled with 6.23–6.24. [Implementation and verification](../Project_GRID_Request_Authorization.md).

**6.22 inference authorization foundation — 2026-11-05.** Host-built identity/scope contracts, live validation, bounded per-owner admission quotas and content-free audit now protect ordinary chat and assistance. 915 terminal tests passed. Additional inference routes and joint 6.23–6.24 action verification remain open; no new action authority is enabled. [Implementation and limits](../Project_GRID_Request_Authorization.md).

**6.21 agent memory migration — 2026-11-05.** Memory records, grants and recall policy now belong to Grid.Agent, with generic lexical/extractive helpers in Grid.AI. Existing conversation storage, schema v24, permissions, correction/expiry and source revocation remain compatible. 1,180 tests and six live copied-database memory checks passed. No new model or memory database is required. [Implementation and verification](../Project_GRID_Agent_Memory_Migration.md).

**6.20 lexical retrieval foundation — 2026-11-05.** Scoped captured-document search and assistance now use SQLite-backed chunks, BM25/token-feature vectors, reranking and versioned citations. 1,165 current-suite tests and a live deep-document answer/revocation check passed. Learned semantic embeddings remain pending; original files are not reopened and Phase 7 is not cleared. [Implementation and limits](../Project_GRID_Retrieval_Implementation.md).

**6.19 text observability foundation — 2026-11-05.** `ai diagnostics` exposes bounded content-free request records and a separate saved-task projection. The version-2 text benchmark has explicit latency/allocation thresholds and sampled CPU/memory/GPU-allocation evidence. 1,151 current-suite tests and both live baseline modes passed. Specialist/image, physical-device and network-blocked acceptance remain open. [Implementation and limits](../Project_GRID_Observability_and_Evaluation.md).

**6.18 persona management completed — 2026-11-05.** Grid.Agent owns six versioned personas and prompt assembly; Grid.AI provides generic system/version/template fields. `ai personas` supports per-agent selection and rollback with portable settings. 1,139 regression tests, 10 live persona checks and normal assistance verification passed. Host permissions, memory scopes and approval rules are unchanged. [Details and test commands](../Project_GRID_Persona_and_Prompt_Management.md).

**6.17A baseline evaluation — 2026-11-04.** Qwen3 4B passed automatic/CPU text smoke and lifecycle checks. Versioned evidence is tied to exact model/runtime/device/test settings; `ai registry acceptance` shows recorded results. 1,126 regression tests passed. Specialist models are not yet selected/downloaded; broad model, image, device, network-blocked and later-milestone acceptance remains open. [Results and remaining gates](../Project_GRID_Model_Acceptance_Baseline.md).

**6.17 registry foundation — 2026-11-04.** Versioned model manifests, checksum verification, retained-revision rollback, shared-asset accounting, candidate tracking, persistent force-Lite and device diagnostics are implemented. 1,109 solution tests and live baseline inventory verification passed. Exact specialist artifacts, device benchmarks and automatic profile promotion remain pending. Running chat remains on the pinned Qwen3 baseline. [Implementation and commands](../Project_GRID_Model_Registry_Implementation.md).

**6.16 text-provider implementation — 2026-11-04.** Grid.AI contracts and Ollama/llama.cpp text adapters are locally verified with 1,090 current-suite tests and live lifecycle/streaming/cancellation checks. Preserve the existing host approval and conversation boundaries. Image-provider memory-release acceptance is still open. [Implementation and test commands](../Project_GRID_Model_Abstraction_Implementation.md).

**6.15 import implementation — 2026-11-04.** PDF/DOCX import and bundled parser deployment are locally verified with 1,073 current-suite regression passes. Preserve PDF page provenance, DOCX body scope, bounded worker isolation and explicit scan-only/no-text failures. Physical second-device and final Phase 7 gates remain open. [Implementation and verification](../Project_GRID_Offline_Document_Import_Implementation.md).

**Planning update — 2026-11-04.** All 15 GRID-AI/GRID-Agent roadmap components are planned in 6.16–6.30, with migration and final acceptance in 6.31, mandatory before Phase 7. The user is selecting specialist models. The new storage target is 128 GB decimal for both Flash and PC copies, with flash drives primary. Current 20 GB enforcement awaits planned migration. Preserve existing implementation history and keep the reported AI failure/second-device acceptance open pending evidence. [Full platform plan](../Project_GRID_AI_and_Agent_Platform_Plan.md).

**6.14 repair — 2026-10-29:** Development-build transfers now include bundled runtime/models/.NET and available source; launch paths are destination-relative, assets/hash/budget are verified before publication, and device/location changes require grant/approval review. A synthetic-world second-PC package and [acceptance instructions](../Project_GRID_Portable_Transfer_Repair_and_Second_PC_Test.md) are ready. Local verification passed 1,049 regression tests and 20 live checks. Second-PC offline/drive-letter/removable-media acceptance remains pending; PDF extraction stays in 6.15. Phase 7 remains blocked.

**6.13 completed — 2026-10-29:** Six explicit agent profiles share one pinned Qwen3 4B Q4_K_M snapshot. CPU-only/reduced-memory operation, role routing and the 20,000,000,000-byte base ceiling are implemented. `ai profiles` and `ai storage` expose configuration and base/data/staging/headroom. **14 live role checks passed**; physical low-RAM/second-device certification remains separate. See [profiles, measured evidence and limits](../Project_GRID_Agent_Model_Profiles_and_Base_Budget.md). Earlier planned/fixed-model descriptions are historical; 6.14–6.15 still block Phase 7.

**6.12 completed — 2026-10-29:** MCP can recommend/invite registered specialists with human confirmation, show participants, accept direct specialist planning requests and display recorded MCP progress/results in the same window. Membership changes start a fresh saved history segment; earlier transcripts and private context are not automatically shared. Decline, removal, scope changes, restart and attached-window workflows are tested. **1,031 automated tests and six bundled-model checks passed.** See [workflow, privacy boundary and verification](../Project_GRID_MCP_Specialist_Invitations_Implementation.md). Earlier statements that invitations are planned are historical; 6.13–6.15 still block Phase 7.

**6.11 completed — 2026-10-28:** MCP (Master Control Program) now offers custom catalog-based planning, scoped clarification, visible specialist-selection reasons, approved ordered plan execution, cancellation, reusable plans and recorded-result briefings. Reserved human controls remain explicit. **1,012 automated tests and six bundled-model checks passed.** Meeting shorthand (`meetings ...` and `connect meetings ...`) now validates before opening a window and supports `-inline`. [Workflow, authority matrix and terminal test](../Project_GRID_MCP_Digital_Helper_Implementation.md). Earlier 6.11-planned statements below are historical; Phase 7 still requires 6.12–6.15.

**2026-10-28 follow-up — GRID logo animation restored at user request:** Startup/login and shutdown show the supplied GRID.AI artwork when it fits. The five-second login transition progressively reveals the logo from left to right, retains skip/reduced-motion support, and enters the terminal automatically. Narrow windows keep the compact fallback. The new 6.10A colors and responsive help remain. This supersedes the compact-logo replacement described below; other screens retain compact headers.

**6.10A implemented — 2026-10-28:** Centralized 13-color terminal theme, semantic feedback, compact headers, section bars, responsive help tables and Unicode-aware layout/input are implemented. Truecolor is used in supported terminals, with console-color and plain-text fallbacks. User input stays white, prompts cyan, agent dialogue amber; errors are red, success green and approval labels gold. This supersedes historical blanket-yellow and oversized-header requirements. Commands, authentication and approval rules are unchanged. **999 automated tests passed**, plus Windows pseudoconsole login/color/masking/resize checks. [Implementation and limitations](../Project_GRID_Terminal_Visual_Theme_Implementation.md). Phase 7 remains blocked on 6.11–6.15.

**6.10 completed — 2026-10-28:** Approved document summaries and recorded task outcomes persist with provenance, explicit recipient/project grants, expiry, correction and revocation. Temporary raw uploads stay temporary. Schema v24; **989 automated tests and six bundled-model checks passed**, including copied-database recall and in-flight revocation/expiry. [Commands, retention policy, migration and test](../Project_GRID_Durable_Memory_and_Delegated_Context.md). Earlier private-only or 6.10-planned statements are historical; 6.11–6.15 remain required before Phase 7.
Retained summaries require explicit --retain and source grant/project checks. Never persist a temporary raw document merely because its summary was approved. Correction revokes old shares; recipient scopes cannot re-share. Historical transcripts/backups are not securely erased. Use schema v24, preserve v23 defaults, and keep the memory deadline check during inference.

2026-10-28: File generation uses a separate extended budget; preserve normal chat limits. Keep previous drafts on failed revisions, clear approval before redrafting and require approval of the new hashes. Current editable-file cap is 64 KiB; AI file prompt cap is 24,000 UTF-8 bytes. Never return partial model output as a file. GRID summaries get small factual snapshot context; still review model claims. Combined requests, instructional content follow-ups and apply-typo retention are implemented. 976 automated tests and 17 real bundled-model checks passed. See verification/6.9-long-file-drafting-report.md.

2026-10-28: File folder names now resolve dynamically through bounded directory-only lookup; ambiguous/truncated matches require a numbered choice or full path. Preserve the pending filename/content and identity scope. Do not replace this with a fixed list of user folder names or claim whole-machine exhaustive lookup. Explicit absolute paths bypass discovery, not validation/approval. 966 tests passed.

2026-10-28: File destinations now include common known folders and explicit absolute paths, with review/approval/apply and existing text/path restrictions. Do not reintroduce project-only validation after inference. Keep registered project and file-only destination rules separate. Random sample counts in natural fill requests are handled locally. Seven added regression cases; 963 total tests passed. Earlier Desktop-only limitations below are historical.

2026-10-28: Desktop AI generation must use `WithFileDestination` both before inference and in the generic post-inference staging callback. A prior replacement missed the generic call and caused Invalid MAXIS parameters for letters/prose despite numeric tests passing. Regression coverage now includes controlled AI create/update and adversarial responses for Desktop; 956 total tests passed.

2026-10-28: Desktop file operations are now implemented via explicit `@desktop` or natural “on my desktop” requests. Resolve the host Windows known folder, never guess a Desktop path or silently reinterpret a repository alias. Use the existing review/approve/apply flow. Tests inject a temporary Desktop root; do not test by editing the operator’s actual Desktop. Six new cases; 951 total tests passed. See the conversational file operations guide for current scope.

**2026-10-28 console repair:** GRID clarification messages must display immediately. Preserve printable typeahead when skipping model replies; clear buffered typeahead before password input. Ignore standalone Markdown fence markers without losing a pending workflow. Never silently execute a corrected typo. Use the actual registered workspace alias rather than the documentation placeholder. 944 tests and six native-console checks passed; unexpected user-reported disconnect remains unreproduced. [Evidence](../verification/6.9-input-repair-report.md).

**6.9 completed — 2026-10-27:** Scoped conversational CRUD and AI content drafts use typed mutation receipts and the original host `changes` service, including attached windows. Keep approvals exact and separate from apply; deletion must preserve durable bytes and restart recovery must validate hashes/root. 941 tests (37 new) and 11 bundled-model workflow checks passed. No schema/dependency change. 6.10–6.15 remain pending. [Guide](../Project_GRID_Conversational_File_Operations.md).

**6.8 completed — 2026-10-27:** Raw connected chat, preserved quotes, scoped automatic post-import analysis and recoverable follow-ups are implemented. Keep host/attached parsing aligned via `ParseInput`; never execute model output. Pasted paths need explicit import consent; stale/replaced callbacks must not run. 904 tests and six bundled-model checks passed. 6.9–6.15 remain pending. [Guide](../Project_GRID_Conversation_Recovery_Implementation.md).

**6.7 completed — 2026-10-27:** New banner at opening/login/shutdown, five-second monotonic login reveal, key skip and `GRID_ANIMATION=off` static mode. Prompts must use explicit light-blue rendering; ordinary messages and unstyled conversation footers are yellow, user text white. 886 tests and Windows ConPTY login/color verification passed. No new schema/dependency. 6.8–6.15 remain pending. [Guide](../Project_GRID_Login_Animation_and_Colors.md).

**Mandatory scheduling update — 2026-10-27:** The user requires 6.7–6.15 before Phase 7. This turn only plans changes; do not claim bugs fixed or new capabilities shipped. Prioritize the reported analysis loop and actual second-device AI failure. Preserve completed history, but follow the new MCP Master Control Program/CRUD/context/model/20 GB/color/animation/PDF requirements and acceptance gate. [Authoritative plan](../Project_GRID_Pre_Phase_7_Readiness_Plan.md).

**6.6 handoff — 2026-10-27:** Unified `talk` is implemented. 873 Release tests and 12 live checks passed. Preserve phrase-based routing, per-terminal scope guards, explicit grants, independent schema/capability validation, exact-ID approvals/dispatch and editing rollback. Ordinary advice retains recall; captured-file analysis does not enter memory. Explicit commands remain. [Workflow, evidence and limitations](../Project_GRID_Unified_Talk_Workflow.md). Earlier 6.6-planned notes are historical; manual desktop/window acceptance remains open.

**6.5 handoff — 2026-10-27:** Intelligence verification is complete: 847 Release tests, 171 phase-tagged tests and ten live bundled-model checks passed. See [verification guide](../Project_GRID_Intelligence_Verification.md) and its captured report. Keep model quality separate from enforced permissions; do not claim universal hallucination prevention. The diagnostic uses synthetic data and retains temporary artifacts. 6.6 unified `talk` remains planned. Preserve earlier implementation history and outstanding manual desktop acceptance.

**6.4 implemented — 2026-10-26:** Original-host `changes` drafts new/revised workspace files from captured context or local MAXIS AI, previews complete content/hashes, requires explicit approval, and applies with backups and guarded rollback. Optional verification uses separately approved MAXIS tasks. **838 Release tests passed**. Drafts are transient; recovery files persist. [Workflow, limits and demo](../Project_GRID_Authorized_Editing_Implementation.md).

**6.3A implemented — 2026-10-26:** `screen options/monitoring/ask/status/stop` provides one-shot selected-monitor assistance using offline Windows OCR and local AI. Human Master Control only; no recording, saved screen context or actions. **799 Release tests passed**, including attached-window and synthetic native-OCR checks. User desktop acceptance remains manual. [Commands, limits and verification](../Project_GRID_Screen_Assistance_Implementation.md).

**4.2H implemented — 2026-10-26:** Conversation labels are light blue, agent bodies yellow and user text white. Assistance now animates; Enter no longer accidentally skips a reply. **772 Release tests passed**, plus an interactive Windows-console probe. User desktop/window visual acceptance remains open. [Changes, verification and test commands](../Project_GRID_Conversation_Display_Repair.md).

**AI response recovery — 2026-10-26:** Truncated answers now get one bounded larger retry, and rejected responses are distinguished from an offline service. Advice and analysis prompts were refined; MCP greetings no longer receive unrelated district counts. **759 Release tests passed**, and the bundled model answered the reported HTML question successfully. [Fix, limits and regression test](../Project_GRID_AI_Response_Recovery.md).


**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](../Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**Automatic conversation recall added — 2026-10-25:** Ordinary saved discussions and their relevant details now carry across conversations/restarts, alongside explicit memory. Private recall stays within the same agent/project; shared meeting recall requires the exact same group. `memory history` reviews candidates; `memory forget-history` excludes prior discussions while retaining explicit memories and transcripts. **728 tests passed**; bundled-model recall/clearing verified. [Behavior, bounds and test](../Project_GRID_Automatic_Conversation_Recall.md). This supersedes earlier explicit-only recall descriptions.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](../Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](../Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


## Agent lifecycle — 5.9 completed (2026-10-23)

`grid -start` initializes the six named agents on first use and resumes their activity thereafter. `grid -pause` blocks new agent replies, MCP coordination and task dispatch while retaining identities, memory, conversations and task history. `agents initialize` now gives migration guidance; connecting to MCP no longer creates it implicitly. `agents` reports whether activity is active or paused. Ordinary startup/reload remains paused; the explicit `--start` launch option takes the same initialization/save path after authenticated owner login.

In-flight dialogue is cancelled and cannot commit across a pause/resume generation. In-flight task attempts are cancelled cooperatively and recorded as Interrupted when the lifecycle changed; partial external effects may remain, so retry requires inspection and fresh approval. Queued tasks are not automatically dispatched on resume; pause retains existing queued approvals, while restart invalidates them through the existing recovery policy. Authenticated attached Master Control terminals can start/pause the shared host, including while its original terminal is busy. Transfer/NGE ownership restrictions remain unchanged.

**Verification:** 672 tests passed (14 Core, 157 Simulation, 99 Persistence, 402 Terminal), including eleven new lifecycle/window/startup cases. Existing historical fixtures now explicitly start their worlds before agent work. Schema remains v21. See [lifecycle contract and acceptance commands](../Project_GRID_Agent_Lifecycle_Implementation.md). Earlier descriptions of independent initialization/lazy MCP or planned 5.9 coupling are historical and superseded.


### 4.2G folder-picker repair — 2026-10-23

Fixed a user-reported Windows dialog failure: FolderBrowserDialog supports Description, not Title. Removed the unsupported property, explicitly emit JSON null on cancellation (Windows PowerShell otherwise emits no JSON for null), suppress first-use progress and keep raw helper stderr/CLIXML out of terminal errors. **392 terminal tests passed**, including three new tests executing the actual Windows PowerShell helper with a real Windows Forms dialog object. Tests substitute only the modal ShowDialog result for selection/cancellation and verify concise failure output. Physical dialog interaction remains a manual check. Earlier injected-picker tests did not cover this platform failure. Restart GRID to load the corrected binary. Schema remains v21.


## 4.2G completed — 2026-10-23

`grid -start` / `grid -pause` replace the old start/pause commands, which now give migration guidance without changing state. `tasks` and `tasks list` show all authorized retained history, including labeled archives. `tasks -completed` shows Completed only; `tasks -list` excludes Completed and Denied (cancelled/failed and archived records remain visible). All views accept one agent filter. Master Control can use `tasks deny <reference> "reason"` on an AwaitingApproval task; denial is terminal, separately audited and saved in schema v21. Existing v20 history upgrades without rewriting cancellations as denials.

Transfers accept drive roots as containers: `grid -copyto "E:\"` targets `E:\GRID.AI`. `grid -copyto -select` / `grid -transferto -select` select a container and create its new GRID.AI child. Existing targets are refused; no merging or overwriting. Original-host Master Control, interactive preview/y/n, saved-world verification and move cleanup safeguards remain. No files move when selection is cancelled, authority changes or media is unavailable.

**Verification:** 658 tests passed (14 Core, 157 Simulation, 98 Persistence, 389 Terminal), including ten new regression cases; zero skipped. Native dialog appearance and physical flash-drive removal/cross-PC acceptance remain manual release checks. The picker uses Windows' built-in local folder dialog, with no new external package/service. The saved city and user files were not relocated by verification. Agent lifecycle coupling (5.9), startup test gate (8.8), changed-device handling (8.9), meetings (6.2A), and full portable/disaster acceptance (8.6) remain planned. See [implementation and test commands](../Project_GRID_Terminal_Controls_Implementation.md).


## Fresh-clone runtime repair — 2026-10-23

Implemented tracked runtime preparation, self-contained Windows package building and isolated verification. GRID now owns a bundled AI server/model cache and uses bundled MAXIS tooling; no personal Ollama service is required. Typewriter now works with fallback replies and inline/attached chat, with explicit on/off overriding the startup preference. **648 tests passed**, plus a real portable AI/chat probe with global runtime paths removed. Full two-PC/voice/device/installer acceptance remains planned. See [setup, package layout, evidence and limits](../Project_GRID_Fresh_Clone_and_Portable_Runtime.md). Earlier global-runtime instructions below are historical.


**6.2 readability follow-up (2026-10-22):** Default chat/transcript is now conversational; `transcript --details` retains technical evidence. Short window greeting and You> prompt; prompt/history updates discourage metadata recitation. 373 terminal tests and clean Release build verified. Preserve this separation in later work.

## Grounded agent conversations — 6.2 completed (2026-10-22)

`talk` and confirmed `voice send` now use shared local AI in inline and dedicated conversation windows. Replies separate timestamped engine facts, unverified AI suggestions/dialogue and supplied sources. Context includes scoped conversation excerpts, a bounded extractive history summary and permitted saved simulation memories; six named agents have distinct profiles. Session revocation/stale turns are rejected; unavailable AI falls back deterministically. No automatic task execution or captured-file ingestion. **641 tests passed**; a real local MCP conversation completed in an isolated test world. Schema remains v20. Meetings (6.2A) and AI task assistance (6.3) remain planned. See [implementation, limits and acceptance test](../Project_GRID_Grounded_Conversations_Implementation.md). Earlier milestone statements below remain historical.


## 6.1 follow-up verification — 2026-10-22

Fixed malformed numeric allocation responses in `ai status`: diagnostics return unavailable instead of throwing, and a subsequent valid response recovers. All **355 terminal tests passed**, including three new regression cases; Release build had zero warnings/errors. A fresh real local-model probe returned GRID LOCAL READY in **3,252 ms**. Earlier full-suite counts remain historical. See [task and AI manual acceptance steps](../Project_GRID_Tasks_and_AI_Acceptance_Test.md). Agent dialogue integration remains planned for 6.2.


## Shared local AI — 6.1 completed (2026-10-22)

GRID now has a bounded shared local inference queue and a fixed-loopback Ollama adapter using installed Qwen3 4B. The dedicated server uses port 11435 with cloud disabled; the existing personal Ollama instance is unchanged. `ai status/test` provides original-host Master Control diagnostics and sampled CPU/memory/GPU allocation. **620 tests passed**; the real C# readiness probe returned GRID LOCAL READY in 3.232 seconds. Conversations remain deterministic until 6.2; no automatic task/tool execution, model context ingestion or schema change (v20). Runtime/model packaging inside Flash GRID remains planned; this installation uses the external Ollama cache. See [setup, licensing, limits and verification](../Project_GRID_Shared_Local_AI_Implementation.md). Earlier milestone/schema/count statements below remain historical.


## Bulk tasks — 5.8 completed (2026-10-22)

Agent-filtered lists and `tasks approve/run/clear -all [-AGENT]` are implemented. Bulk mutations/dispatch require original-host Master Control; approval and clear require reviewed interactive confirmation. Batches capture fixed selections, revalidate state/authority and stop dispatch on the first unsuccessful attempt. Clear archives records and cancels pending work, retaining audit/results and MCP/RELAY links; active/unsettled work is refused. `tasks list -archived` reads retained history. **Schema v20; 603 tests passed** (14 Core, 157 Simulation, 97 Persistence, 335 Terminal). Archiving does not free the 256-task history capacity. See [commands, verification and limits](../Project_GRID_Bulk_Task_Implementation.md). Earlier milestone/schema/count statements below remain historical.


## Transfer command rename — 4.2F completed (2026-10-22)

Use `grid -copyto "location"` or `grid -transferto "location"` inside the GRID terminal. `copexist` and `tranexist` now only show migration guidance and never execute a transfer. Existing Master Control/original-host restrictions, preview, confirmation, final save and verified move/copy safeguards remain. **582 tests passed** (14 Core, 157 Simulation, 97 Persistence, 314 Terminal). No schema change or user-world relocation. See [transfer behavior and limits](../Project_GRID_Transfer_Implementation.md). Earlier command names and counts below describe historical milestones.


## Conversation windows — 4.2E delivered (2026-10-21)

Default `connect` and `resume` now open dedicated attached conversation windows. Agent replies animate with a skippable typewriter effect; `typewriter off` and `GRID_TYPEWRITER=off` provide instant text. Exit detaches without ending the saved conversation; parent revocation closes access. Explicit `connect MCP -inline` / `resume ID -inline` retains the original-host voice workflow and instant text. **572 tests passed** (14 Core, 157 Simulation, 97 Persistence, 304 Terminal). Schema remains v19; no AI/meeting implementation is implied. Multi-agent integration remains pending in 6.2A. See [implementation and acceptance test](../Project_GRID_Conversation_Windows_Implementation.md). Earlier milestone statements below remain historical.


## End-to-end task verification — 5.7 completed (2026-10-21)

MCP delegation, human approval, real MAXIS unit-test execution and RELAY recorded-result delivery are verified together, including actual passing/failing TRX evidence and SQLite reload. Nine new cases also cover all six agent roles, cancellation, durable-start recovery and duplicate dispatch prevention. **560 tests passed** (14 Core, 157 Simulation, 97 Persistence, 292 Terminal), zero failed/skipped. No runtime/schema change was needed; schema remains v19. See [verification matrix, boundaries and terminal test](../Project_GRID_End_to_End_Task_Verification.md). Next: **6.1 Shared local AI**. Earlier milestone counts and next-step statements below are historical.


## VOLT and RELAY — 5.6 completed (2026-10-21)

VOLT now reports existing host telemetry and supports approved normal/eco/auto pacing of GRID's automatic simulation. Unknown/unsupported GPU data is preserved; Windows resources and manual tick behavior are unchanged. RELAY provides a saved local inbox, recorded-task briefings, human acknowledgements and explicitly approved offline speech with delivery/failure receipts. Schema v19 preserves mode and inbox history without automatic playback/replay. **551 tests passed** (14 Core, 157 Simulation, 97 Persistence, 283 Terminal). See [VOLT/RELAY implementation and acceptance test](../Project_GRID_VOLT_RELAY_Implementation.md) for commands and boundaries. Next: **5.7 end-to-end verification**. Earlier milestone/schema/test statements below remain historical.


## JN storage — 5.5 completed (2026-10-21)

JN now inventories approved paths, reports possible duplicates, and prepares cleanup proposals without deleting source files. Defined GRID-managed storage is measured against an initial 20 GB decimal logical budget; MCP quota escalations require separate Master Control approval to change it. This is tracking/governance, not a hard write cap or disk reservation. Schema v18 preserves scope and quota history. **535 tests passed** (14 Core, 157 Simulation, 97 Persistence, 267 Terminal). See [JN implementation and acceptance test](../Project_GRID_JN_Storage_Implementation.md) for limits, accounting roots, report retention and remaining planned automation. Next: **5.6 VOLT and RELAY**. Earlier milestone/schema/test statements below remain historical.


## SENTINEL security — 5.4 completed (2026-10-21)

SENTINEL now reads local Defender protection/scan timestamps, effective firewall profiles, and bounded Security-log metadata through individually approved tasks. Timestamped results explicitly distinguish real host observations, warnings and unavailable data from simulated incidents. No scans, setting changes, elevation or remediation are enabled. Schema v17 persists these results through the existing broker. **518 tests passed** (14 Core, 157 Simulation, 97 Persistence, 250 Terminal), including 13 new security cases and live read-only provider verification. See [SENTINEL implementation](../Project_GRID_SENTINEL_Security_Implementation.md). Next: **5.5 JN — Storage and files**. Earlier milestone/schema/test statements below remain historical.


## MAXIS development — 5.3 completed (2026-10-21)

MAXIS now supports registered-workspace inspection, fixed .NET build/test execution, saved diagnostics and explicit replacement suggestions staged outside original source. Every task requires human approval; build/test executes trusted project code as the current Windows user (no OS/network sandbox). Schema v16 persists immutable, revocable workspace registrations. Other specialists remain demonstrations. **505 tests passed** (14 Core, 157 Simulation, 97 Persistence, 237 Terminal). See [MAXIS implementation](../Project_GRID_MAXIS_Development_Implementation.md) for commands, offline prerequisites, artifact retention and limits. Next: **5.4 SENTINEL**. Earlier milestone/schema/test statements below remain historical.


## MCP administration — 5.2 completed (2026-10-21)

Explicit `mcp request` clauses now create atomic saved specialist task plans. MCP reports actual child states/results; each child requires human approval and original-host dispatch. Adapters remain demonstrations. Natural-language planning and real specialist actions remain planned. Schema v15 preserves plan links and existing cities. **485 tests passed**, including 13 new coordination cases. Next: **5.3 MAXIS — Development**. See [MCP coordination implementation](../Project_GRID_MCP_Coordination_Implementation.md). Earlier milestone statements and schema/test counts below remain historical.


## Agent/task infrastructure - 5.1 completed (2026-10-21)

Explicit `agents initialize` registers MCP, MAXIS, SENTINEL, JN, VOLT and RELAY by stable ID. Schema v14 persists a bounded task queue, immutable proposals, one-attempt human approvals, results and audit. `tasks propose/inspect/approve/cancel/retry/run` enforce role-specific allowlists and live session authority at the service boundary. The broker saves a running record before invocation, prevents concurrent dispatch, supports cancellation/timeouts and requires fresh approval for retries/restart recovery. Agent sessions cannot self-approve or borrow capabilities. **Only labeled demonstration adapters are enabled; no real PC task, shell execution, natural-language delegation or autonomous producer is implemented.** Chat/voice messages remain conversation data.

**472 tests passed**, including 15 new task integration cases covering authorization, persistence barriers, cancellation/retry, deduplication, SQL rollback and restart/recovery. Existing v13 worlds migrate without regenerating the city or automatically creating agents. Next planned module: 5.2 MCP coordination. See [task broker implementation](../Project_GRID_Task_Broker_Implementation.md) for commands, bounds and limits. Earlier milestone/schema/test counts below remain historical.


## System-agent names updated - 2026-10-21

ARCHON is now **MCP** (coordination), FORGE is now **MAXIS** (development), and ARCHIVIST is now **JN** (storage/files). Current commands, examples, plans and checklist labels use the new names. These names do not grant privileges or implement the planned specialist services. The human **Master Control** role remains separate from MCP.

On world load, legacy-named Agent residents are renamed without changing IDs, registrations, context grants, voice-profile links or conversation ownership. Conversation participant labels update; historical messages/sender labels and event history remain intact. The coordinator retains its original identity-generation seed to avoid duplicate residents. Changes persist on the next normal save. A conflicting target name or exhausted revision skips the rename and records `AgentRenameSkipped`; resolve by ID rather than merging identities. Ordinary application/civilian names and the generic Archivist occupation are unchanged.

Verification: 457 tests passed (14 Core, 157 Simulation, 97 Persistence, 189 Terminal), including new legacy-name/idempotence and collision cases. Earlier completion dates and test counts remain historical evidence; documentation examples now use current names.


## Communications verification - 4.5 completed (2026-10-21)

All seven 4.5 checklist items have automated coverage. **455 tests passed** (14 Core, 155 Simulation, 97 Persistence, 189 Terminal), including three new end-to-end tests for authenticated conversation/context recovery through SQLite, malformed input without transcript mutation, and unrelated-agent privacy with logout revocation. Existing suites cover recipient routing, durable history/recovery, reviewed speech/typed-message equivalence, audio failure fallback and deterministic communication without AI. No production-code/schema change was needed. Physical microphone/playback/device-unplug checks remain manual; advanced voice and task-delegation features remain planned. See [verification matrix and limits](../Project_GRID_Communications_Verification.md). Earlier milestone counts below are historical.


## Voice transcription repair - 2026-10-21

A user-reported microphone transcription failure was reproduced with generated speech through the real C# helper connection: repeated synchronous `Recognize()` calls could reach an already-finished stream and throw `InvalidOperationException` (0x80131509), discarding recognized text. Recognition now uses `RecognizeAsync(Multiple)` and waits for `RecognizeCompleted`, gathering recognized phrases until completion. Speech/audio remain local and in memory. Explicit typed constructors avoid PowerShell overload ambiguity.

Failures now report a bounded stage, exception type and HRESULT rather than hiding the error or exposing raw helper stderr/audio. Capture reports duration and rejects recordings shorter than 0.25 seconds with a hold-Space hint. `voice 0` is a shortcut for `voice microphone 0`; no-draft hints recognize an already-connected conversation, and fallback advice is no longer duplicated.

**Verification:** 186 terminal tests pass, including four new real silent-PCM/generated-speech/invalid-audio and UI shortcut cases. The generated-speech case reproduced the failure before the completion-event fix and passes after it. No physical microphone recording or speaker playback was used; physical-device verification remains interactive. Earlier 448-test completion counts below are the original milestone's history.


## Offline voice - 4.4 Windows MVP completed (2026-10-21)

The original interactive host supports `voice devices/microphone/language/listen/review/edit/send/cancel/mute/speak/profile`. Hold Space to record (20-second maximum), release to transcribe with an installed offline Windows recognizer, review/correct, then explicitly send. Typed and microphone messages use the same dialogue service; speech never executes terminal commands. Playback starts muted and occurs only on explicit request. Saved registered-Agent profiles select Windows voice/rate/volume. Escape interrupts; identity/conversation changes prevent stale sends. Raw audio stays in transient memory and is not saved. Missing hardware or language/voice assets leave typed chat available.

Profiles are an additive validated field in existing schema-v13 communication JSON; legacy JSON defaults to no profiles. Restart and recovery preserve profiles and sent microphone-origin messages, not drafts/audio/device selection/mute state. Windows System.Speech is the implemented provider; whisper.cpp/Piper, attached-terminal audio, output-device selection and advanced voice options remain planned. 448 tests passed (18 new voice cases); real offline provider inventory succeeded. Physical microphone/speaker operation still needs an interactive smoke check. Earlier voice-as-planned statements below describe prior milestones. See [offline voice implementation](../Project_GRID_Offline_Voice_Implementation.md).


## Context convenience - 4.3A completed (2026-10-21)

`context add -search` now opens a local Windows multi-file picker (default: temporary, `Inbox`, owner only); optional retention/project/registered-Agent arguments are supported. Read/revoke/remove accept exact filenames or IDs. Multi-file `context read` and session-local `context select` support reading several captures together. Names must be unambiguous; reads recheck access and selections clear on identity change. Picker completion revalidates the session; batches validate before import and persistent batches save once. Main and attached terminals use the same host-owned context service, with no new schema or network requirement. Drag/drop and remote uploads remain unimplemented.

Completion verification: 430 tests passed, including 10 new 4.3A tests. Automated tests inject dialog results; visible Windows dialog interaction still needs manual verification. See the context implementation/help for command syntax and limits. Prior 4.3 notes below describe the original milestone.


## Local task context - 4.3 (2026-10-20)

Implemented `context add/list/read/revoke/remove` for explicit file captures, scoped Agent grants, project labels and temporary/persistent retention. Schema v13 and 420 passing tests. Read [context implementation](../Project_GRID_User_Supplied_Context_Implementation.md) for syntax, limits and privacy/backup behavior; AI ingestion/task execution remain future work.


## Shared terminal windows - 4.2C (2026-10-20)

Implemented `ntm` and `<command> -opninwin` with one authoritative host and scoped per-window contexts. Attached exit detaches; original-host exit disconnects clients before save. Lifecycle/credential commands remain host-only. 404 tests passed. See [window implementation](../Project_GRID_Shared_Terminal_Windows_Implementation.md) for commands, transport, authorization and verification limits.


## Installation editions and rendering - planned 2026-10-20

Installation will offer **PC GRID** (permanent machine installation, with advanced 3D options) or **Flash GRID** (mobile flash-drive package, with very simple lightweight 3D). The installer must explain the choice, validate the destination and retain the selected edition. Both share simulation/save/identity behavior and must run supported tasks offline after provisioning. Flash GRID keeps managed files with the portable package and requires no persistent host integration; graphics still run on the host PC. Quality differences must not regenerate or change the world.

See [edition details](../Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 7.6/8.6. Rendering budgets, packaging and migration remain planned; this addition changes documentation only.

## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](../Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all GRID runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](../Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 4.2B (implemented), 8.6 and 8.7 (planned). See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

## Local login and identity completion - 4.2A (2026-10-20)

**Implemented and verified.** Start with `login master`: the first successful Windows-token login binds the world owner; later logins must match. Sessions expire after eight hours. Master Control can register existing Agent identities, switch roles, and preview/confirm one-time program edits. Prompts/help/commands are role-aware; agent chats are participant-scoped and global/admin controls are denied. Switching/logout clears active chats and previews. Schema v11 preserves owner/agent registration without restoring authentication.

**362 tests passed**, plus isolated Windows login/agent restriction/confirmed edit/restart smoke. See [Local Login and Identity Implementation](../Project_GRID_Local_Login_and_Identity_Implementation.md) for commands, service authorization, audit and security limits. This authenticates the current Windows account, grants no OS elevation, and provides no independent agent passwords or real task service. NGE now requires Master Control; --start waits for login. Earlier dated pending-login/editing statements refer to prior milestones.

## Terminal conversations completion - 4.2 (2026-10-20)

**Implemented and verified.** Use connect (default MCP), talk, conversations, transcript, resume and disconnect. MCP is created on first explicit connection as a persistent coordinator Agent; a stable world-local operator handle supports reopening saved conversations. Replies to status/who are you are labeled verified GRID state; other replies are labeled deterministic simulated dialogue and claim no task execution/delegation. Input/reply pairs commit atomically and reuse schema v10.

**353 tests passed**, plus a three-launch isolated terminal save/resume/end smoke. See [Terminal Conversations](../Project_GRID_Terminal_Conversations.md) for syntax, identity/capacity limits and completion evidence. This local operator handle grants no authentication or admin authority; 4.2A login, real task delegation and LLM dialogue remain planned. Earlier dated pending-terminal-chat/agent-creation statements describe previous milestones.

## Communications foundation completion - 4.1 (2026-10-20)

**Implemented and verified.** Grid.Communication now defines recipient/message/conversation models and a local hub contract implemented by the simulation host. Exact name/full-ID routing, explicit participant identities, start/send/end/read operations, ordered bounded messages and schema v10 transactional transcript persistence are available. Conversations survive restart and backup recovery even when a historical application resident is absent; new sends require live original program identities.

**346 tests passed.** See [Communications Foundation](../Project_GRID_Communication_Foundation.md) for APIs, identity boundaries, bounds and recovery evidence. These trusted local APIs store messages without authenticating the caller, executing text, claiming external delivery or generating replies. Terminal chat is still 4.2; voice, real delegation, LLM dialogue and production login remain planned. Earlier dated pending-communication/schema statements describe previous milestones.

## Autonomy completion - 3.4 (2026-10-20)

**Implemented and verified.** The engine composes resource/shift/work/rest/travel and social decisions without an LLM. Equally ranked social choices now use a stable rank seeded by saved world identity, round and participant IDs; restore retains reproducibility without extra random state. Schema v9 gates this policy change. Preferences, affinity and world rules still take precedence.

**338 tests passed.** An accelerated one-hour simulation covered eight persistent agents, zero-to-512 fake application churn, resource scarcity/recovery, valid movement/events and concurrent snapshot readers. CPU, memory and latency measurements are recorded in [Autonomy and Verification](../Project_GRID_Autonomy_and_Verification.md). This is a simulated-hour test, not a real-time hour-long production soak. Host applications remain observation-only; automatic agent creation, admin configuration, real task execution and Phase 4 communications remain planned. Previous dated pending-autonomy/schema statements describe earlier milestones.

## Social civilization completion - 3.3 (2026-10-20)

**Implemented and verified.** Explicitly opted-in Agent/Special residents now have cooperative, competitive or reserved preferences; bounded relationships, memories and occupation history; and simulated district civic tokens with scarcity/recovery. Decisions run on simulation time, respect current work/travel and commit atomically with ticks. Host applications remain observation-only and receive no fictional social activity or resource consumption.

Schema v8 persists social state and partial rounds, upgrades existing worlds without inventing participants, and supports validated backup recovery. `social [id|name]` and program inspection provide read-only views. Trusted engine APIs configure social participation/pools; production login, terminal admin configuration, automatic agent creation, actual communication/delegation and Phase 3.4 sustained autonomy remain future work. **336 tests passed**, plus isolated terminal create/save/reopen. See [Social Civilization](../Project_GRID_Social_Civilization.md) for complete rules, limits and evidence. Earlier dated statements that social functionality is pending describe previous milestones.

## Movement and occupations completion - 3.2 (2026-10-20)

**Implemented and verified.** Saved city roads now support deterministic district travel for explicitly configured Agent/Special residents assigned to compatible capacity-one workstations. Five simulated seconds per edge; no work progress while traveling; shift/resource uncertainty freezes travel. Trusted assignment/release APIs preserve world ownership and revision checks. `jobs` and `activity` add read-only workstation and measured application views. Host applications acquire no fictional civic jobs.

Schema v7 preserves station assignments, remaining routes and edge timing in existing behavior JSON, validates them on restore and performs no offline catch-up. Older unassigned routines retain their 3.1 behavior. No agent population or interactive admin job configuration is added. **323 tests passed** in isolated Verification builds; disposable jobs/activity/save/reopen smoke passed. A running user Release session locked its binaries and was left untouched; rebuild the normal target after closing it to use the changes. See [Movement and Occupations](../Project_GRID_Movement_and_Occupations.md) for policy, limits and full completion evidence. Earlier statements that movement/workplaces are pending refer to previous milestones.

## Bare Grid.AI command support - 2026-10-20

Startup now also installs a managed `Grid.AI.cmd` launcher in `%LOCALAPPDATA%\GRID\bin` and adds that directory to the Windows user PATH, preserving existing entries and avoiding duplicates. Type `Grid.AI` directly in PowerShell or Command Prompt; arguments such as `--start` are forwarded. The persistent `Grid.AI` environment variable remains supported. An existing unmanaged launcher is not overwritten. This extends the original variable-only 1.14 behavior.

Restart the parent terminal app to inherit the new PATH. For the currently open PowerShell session, refresh it without closing the window:

```powershell
$env:Path += ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
Grid.AI
```

Verification: **89 terminal tests passed**. The bare command resolved to the installed launcher and successfully ran/exited with `--ephemeral --no-color`; no saved world was opened.

## Startup launcher environment variable - 1.14 (2026-10-20)

**Implemented and verified.** On Windows startup, GRID ensures a persistent **user-scope** environment variable named `Grid.AI` points to the terminal `.exe`. A missing, empty or broken path is registered using the current build's apphost; an existing valid absolute path is preserved, including another installed build. The current GRID process receives the same value. Values are stored without shell quotes, so paths with spaces remain usable. If no executable is available or Windows denies the environment update, startup reports a warning and GRID continues. Other platforms skip Windows registration. No administrator elevation, shell-profile changes or automatic simulation start are required. The follow-up above also adds a user-PATH launcher.

PowerShell launch (from a fresh environment):

```powershell
& ${env:Grid.AI}
# Optional: begin simulation immediately
& ${env:Grid.AI} --start
```

Command Prompt launch:

```bat
"%Grid.AI%"
```

The environment variable is a path value; the managed PATH launcher above separately provides the bare `Grid.AI` command. Already-open shells do not inherit changes from GRID. Restart the parent terminal/IDE, or sign out/in if necessary. To refresh only the current PowerShell shell without restarting:

```powershell
${env:Grid.AI} = [Environment]::GetEnvironmentVariable('Grid.AI', 'User')
```

A development-build path remains tied to that build's `.exe`; rebuild that target to pick up changes. Launching another build preserves a still-valid registered installation. If the target is removed, launching GRID directly registers the current available apphost again. This is a convenience launcher, not an installer or authentication mechanism.

**Verification:** 87 terminal tests passed, including missing/empty/broken values, paths with spaces, preservation of an existing installation and rejection of a dotnet-host launcher. An ephemeral Release launch registered the actual Windows user variable and verified that its executable exists. No saved world was opened or modified. Current verified value: `C:\Users\jack\source\repos\Grid7736\Grid.Terminal\bin\Release\net10.0\Grid.Terminal.exe`.

## NGE implementation - 1.13 (2026-10-19)

**Initial local-terminal NGE implemented.** Type `nge` in an interactive saved-world session, review the world identity and exact file list, then enter `y` to confirm or `n`/blank to cancel. Invalid answers reprompt; EOF cancels deletion and follows normal EOF shutdown; Ctrl+C cancels deletion and follows normal graceful shutdown. Arguments such as `nge y` are rejected. Redirected/unattended input and ephemeral sessions cannot delete saves. This is local-operator confirmation, **not authenticated admin enforcement**; human-admin-only access remains future login/authorization work.

Confirmed NGE stops/awaits scheduler, autosave and telemetry, serializes against in-flight saves, revalidates world/backup identity, then removes only the active database, rolling `.backup` and exact `-journal`, `-wal`, `-shm` paths. It retains the session lease through completion and refuses symbolic-link/junction targets. Historical legacy/damaged archives, staging artifacts of uncertain ownership, external copies, other worlds and PC applications remain; the warning explicitly states this scope. The harmless session `.lock` file may remain after release. Interrupted recovery must be resolved before NGE can be prepared.

A flushed `.nullifying` marker is created before deletion. Partial failure retains that marker, blocks startup/save/recovery, reports remaining files and exits nonzero without a final save. **There is no automatic retry/resume command yet:** inspect the marker and remaining exact world artifacts before explicit manual cleanup; never remove the marker merely to bypass an unresolved operation. Successful NGE removes the marker and exits with the GRID.AI artwork and `GRID NULLIFIED // EXISTENCE TERMINATED`, never a saved-world claim. Next normal startup follows existing hardware-city creation; it does not reuse a managed backup. Independent historical/manual copies remain outside this deletion scope.

Verification: **306 Release tests passed** (14 Core, 127 Simulation, 83 Persistence, 82 Terminal). New coverage checks cancellation/blank/invalid/EOF input, uppercase confirmation, redirected/ephemeral rejection, worker shutdown before deletion, no final save on success/failure, owned-file deletion, unrelated-file preservation, rejected forged plans, blocked future writes/recovery, new identity after reopen and a Windows locked-file failure retaining the marker. All deletion tests use freshly created disposable worlds; no user save was deleted. Existing hardware-city creation tests cover the startup path; no manual destructive run against the user's world was performed. Visual Studio still owns its post-exit footer.

## Terminal commands and shutdown complete - 1.12 (2026-10-19)

Implemented grouped `help`, display-only `clear`, `programs -active`, `programs -possible`, `programs -sleeping`, and a GRID.AI ASCII farewell. Active selects fresh Running applications; sleeping selects fresh On call or Sleeping applications; possible includes current applications plus the saved known-application catalog. `ids` works with one filter. Unknown/stale observations are never classified as active or sleeping. Bare `programs` still includes agents and other residents.

**Schema v6** adds a separate ApplicationCatalog table (key, name, source, last catalog observation UTC). The catalog is captured with the simulation snapshot and saved atomically with world/history; backup/recovery and schema validation include it. Installed/observed metadata survives restart; ordinary residents, PID lifetimes, activity and resource readings are not restored. Last observation is catalog provenance, not proof of a launch or current installation. Entries missing from fresh discovery display Historical / unknown. Existing schema-v5-only statements below describe earlier milestones.

The ASCII banner appears after graceful shutdown and final save. Save/shutdown errors retain diagnostics and a nonzero exit code without a success message; ephemeral sessions explicitly say NOT SAVED. There is no keypress wait. Visual Studio can still append its own debug-console footer. Help uses portable ASCII separators and registered command metadata, including extensions; no-color/redirection behavior is preserved.

**Verification:** `dotnet test -c Release --no-restore`: **297 passed** (14 Core, 127 Simulation, 81 Persistence, 75 Terminal). Tests include filters/IDs/invalid flags/empty results, clear preserving the world and tick, successful/failed farewell, catalog save/reopen/recovery and v5-to-v6 migration. A disposable built-terminal run exercised all command groups, filters, redirected clear fallback and successful save/reopen of the same city. An ephemeral PTY run preserved tick1 through clear and displayed the NOT SAVED banner. This tool's PTY uses the clear fallback; physical screen clearing/color appearance and Visual Studio footer presentation were not visually verified in the user's console. No IDE settings or existing user worlds were modified.

## Latest population revision - 2026-10-19

The current population includes discoverable installed applications and current-session background processes. States are **Sleeping**, **On call**, **Running**, or **unknown** when evidence is insufficient. This supersedes earlier windowed-only/removal-on-exit descriptions below; those remain historical completion records. Ordinary application state is still rediscovered rather than persisted, and the saved city is reused. Full-machine coverage is not complete: see [Installed Application Availability](../Project_GRID_Installed_Application_Availability.md) for implemented scope, Windows inventory limits, remaining work and **290-test** verification.

## Program behavior completion - 2026-10-19

**3.1 implemented and verified.** Non-demo residents now have deterministic needs/goals/actions. Applications reflect fresh or unknown observations; explicitly configured Agent/Special residents perform resource-gated simulated occupation work/rest on simulation-time shifts. Schema v5 persists their routines, sourced budgets and progress without restoring live observations or adding offline work. Use behaviors and inspect for last-tick behavior. No OS task execution, movement or named agent services are claimed.

See [Program Needs and Behavior](../Project_GRID_Program_Behavior.md) for policy, APIs, limits and completion evidence: **278 Release tests passed**, build zero warnings/errors, disposable terminal smoke passed. Debug output was locked by running user sessions, which were left running. Earlier dated deferred-behavior/schema statements describe previous milestones; existing history and completed scope are preserved.

## Application resident completion - 2026-10-19

**2.7 / 2.7.1 implemented and verified:** production population now follows current-session windowed executable groups through read-only discovery and serialized lifecycle reconciliation. Application residents are transient; Agent/Special residents persist in schema v4. Before production conversion, legacy fictional citizens are preserved in a uniquely named validated archive. City/layout and clock remain intact. Unknown resource requirements display unknown condition; no host-control authority is implied.

Verification: **268 tests passed**, build zero warnings/errors, and a disposable Windows startup/reopen detected nine application groups. See [Application Residents Implementation](../Project_GRID_Application_Residents_Implementation.md) for grouping limits, discovery/freshness, storage/migration, editing, resource interpretation and completion evidence. Earlier dated statements that 2.7 is planned, production cities stay empty, or all residents persist describe previous milestones. Agent delegation/services, login, rendering and additional per-application telemetry remain future work.

## Hardware city completion - 2026-10-19

**2.6 / GRID-REQ-004 implemented and verified.** First-time creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates bounded district/street/building-lot geometry and saves actual layout and provenance in schema v3. Later startups reuse the saved world without discovery or regeneration. Existing worlds are preserved; new generated cities start empty pending 2.7. Inventory is creation provenance, separate from live resource telemetry and application identity. Rendering, atmosphere, fashion and application-resident integration remain planned.

Verification: **250 tests passed** (14 Core, 106 Simulation, 74 Persistence, 56 Terminal); build had zero warnings/errors. A fresh Windows city and unchanged reopen were verified in a temporary directory. Use city to inspect saved generation details; --world with a new filename explicitly creates a separate world.

See [Hardware City Generation](../Project_GRID_Hardware_City_Generation.md) for architecture, mappings, fallbacks, persistence and completion evidence. Earlier dated statements that hardware generation is planned or that normal creation always makes twenty fictional citizens describe previous milestones; this update supersedes those status statements while preserving their history.

## Phase 2 verification update - 2026-10-19

Section 2.5 is complete for the current implementation. **231 tests passed** (8 Core, 106 Simulation, 64 Persistence, 53 Terminal), with zero build warnings/errors. Eight new isolated cases verify headless terminal save/reopen/recovery, populated older-schema restoration, no offline catch-up, invalid/missing-file preservation and abrupt writer termination with SQLite journal recovery. A test-only helper under TestSupport is built by the persistence tests; production projects do not reference it.

See [Phase 2 Verification](../Project_GRID_Phase_2_Verification.md) for evidence and limits. Earlier test counts remain historical. Process-crash verification does not claim physical power-loss coverage; pending application energy/classification/lifecycle work and hardware-generated cities remain unimplemented. No production behavior was changed for 2.5.


## Resource/host foundation update - 2026-10-19

Section 2.4 now provides read-only host observations, explicit units/sources/freshness, fake-provider tests and a provenance-required additional-work resource-condition policy. Use resources for details or live for a compact CPU/RAM summary. Unsupported measurements and unknown application requirements remain unknown. Current observations are transient and cannot be edited into Windows allocations or restored from a save as fresh data.

Verification: 223 tests passed, build zero warnings/errors, and real Windows disposable-session sampling passed while simulation time stayed paused. See [Resources and Host State](../Project_GRID_Resources_and_Host_State.md) for collector coverage, the exact policy and limitations. Application discovery/classification, per-application resource attribution and replacing application energy drain remain dependent on 2.7. Existing fictional Genesis citizens retain their old rules. Earlier deferred-telemetry statements below describe prior milestones, not this update.


## History implementation update - 2026-10-18

Section 2.3 now persists meaningful committed changes with world snapshots in schema v2. The history command supports entity/kind/severity/time filters and bounded paging. Routine Tick records are excluded; pending-buffer overflow is reported explicitly. Changing edits include attribution/context, but denied attempts and no-ops are not a comprehensive security audit. Structured operational errors are emitted to terminal output. See [History and Observability](../Project_GRID_History_and_Observability.md) for retention, failure and API details.

Verification: 208 tests passed; solution build had zero warnings/errors; isolated terminal restart/query smoke passed. Earlier schema-v1 and deferred-history statements below describe the prior milestones. Live-view history remains process-local; the new saved-history path is separate. Application discovery, resource interpretation, resident classification and persistence filtering remain planned in 2.4/2.7.


## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

The user revised the product vision: ordinary programs represent currently running PC applications and are not individually saved; agents and explicitly designated special programs persist. Resources reflect application needs/usage and host conditions; energy is a defined interpretation, not arbitrary stamina. Read the shared contract and checklist 2.4/2.7 before further population work. Preserve historical 2.2 evidence without claiming this new model is implemented.

See [Host programs, lifecycles and resources](../Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


**Recorded:** 2026-10-18
**Purpose:** Help future agents continue this collaboration with the same continuity, accuracy and practical results.

This is a project handoff and a summary of observed user preferences, not a replacement for the agent's governing instructions, current user request, repository instructions or tool permissions. It describes visible working practices, not private model reasoning. Recheck the repository before relying on historical status.

## 1. Where work happens

- Repository: `C:\Users\jack\source\repos\Grid7736`
- Documentation: `MD Project Files`
- GitHub remote observed in this chat: `https://github.com/Scorpio7736/Grid7736.git`
- Environment used: Windows and PowerShell; user timezone America/Chicago.
- Main planning entry point: [Master Development Checklist](../Project_GRID_Master_Development_Checklist.md).
- Implementation evidence: [Current Implementation Status](../Project_GRID_Current_Implementation_Status.md), dated completion notes and actual source/tests.
- Requirements and architecture: [Master Overview](../Project_GRID_Master_Overview.md) and [Design Specification](../Project_GRID_Design_Specification.md).
- How to run and extend the terminal: [README](../../README.md).

The conversation progresses through numbered checklist modules, followed by verification, documentation and occasional release or terminal-launch requests. Examples include world management 1.4, clock 1.5, engine 1.6, terminal 1.7 and editing preparation/API 1.9.1-1.9.3. The user may refer to a module number without repeating its requirements; read the matching checklist section before acting.

## 2. How to interpret the user's requests

The user frequently dictates messages. Punctuation, repeated words and short corrections may be transcription artifacts. Interpret them using the surrounding task; ask only when ambiguity materially affects the outcome.

- Requests such as "work on 1.9.3" mean carry out that module, verify it and update its completion notes, not merely propose a plan.
- "Add this to the MD files" is a documentation/planning request unless implementation is also requested. Do not turn a planned feature into code without that scope.
- "Do the same thing" usually refers to the previous module's workflow, including completion evidence. Inspect the context rather than copying claims blindly.
- A correction or added requirement normally steers the active task. Preserve unfinished work unless the user clearly cancels or replaces it.
- When the user says they missed permission, reissue the actual blocked request. Do not assume a rejected or unanswered tool approval succeeded.
- A request to commit changes to GitHub authorizes a commit and push for that work. It is not blanket permission to publish every future edit.

The user values practical action, continuity and thorough documentation. Use reasonable defaults for routine reversible choices. Avoid repeatedly asking for authorization already provided, while respecting actual tool restrictions and unresolved decisions.

## 3. A successful task cycle

1. **Orient:** locate the repo, read applicable AGENTS.md files if present, inspect Git status and identify the relevant checklist and design sections. Look for newer commits or user edits before changing anything.
2. **State the action:** give a brief update describing the intended result and important assumptions. Keep ongoing updates focused on findings and remaining work.
3. **Resolve missing inputs:** locate referenced documents or ask for their location. Continue independent work when useful, but never claim to compare a supplied version that was not available.
4. **Do the scoped work:** preserve unrelated changes, follow the architecture and keep feature boundaries explicit. Prefer focused edits over replacing whole files with older copies.
5. **Verify proportionately:** use meaningful tests/builds for runtime changes; inspect diffs, links, formatting and checklist preservation for documentation-only changes. Do not invent successful tests or rerun broad checks without a reason.
6. **Document:** update the relevant checklist and supporting documents, recording what changed, what was verified and what remains deferred.
7. **Report:** lead with the outcome, list material changes and limitations, link useful files, and state whether changes are local, committed or pushed. Complete authorized publishing steps before reporting publication success.

Keep communication concise and plain. Explain what a change does and why it matters. A plan is not a substitute for completing an action request, and an intermediate progress message is not the final handoff.

## 4. Documentation rules established in this chat

The user explicitly liked dated completion notes and asked that they continue. The user also requested comprehensive documentation so changes are not left undocumented.

- Preserve completed checklist items and their historical evidence. Do not overwrite newer repository information with an older supplied document.
- Distinguish **planned**, **design complete**, **implemented**, and **verified**. A completed design does not establish working runtime behavior.
- Use checked items only when supported by observed implementation or the item's actual design deliverable. Keep future implementation and acceptance tests unchecked.
- Record dates, relevant commit references, exact verification commands and observed results where useful. Historical counts remain historical; label later totals separately.
- Distinguish a project scaffold from an implemented subsystem, a returned receipt from durable audit, and a command/API contract from a usable terminal workflow.
- Keep requirements consistent across overview, specification, roadmap, checklist and affected style/storage guides. Cross-link a shared detailed contract rather than creating contradictory copies.
- When asked to update all documents, audit their relevant implications. Style guides may need integration notes without rewriting the established art direction.
- Document unresolved choices and conflicts honestly. Never imply that missing files were supplied, an unavailable feature was tested, or a GitHub push completed without evidence.

Suggested completion-note format:

```markdown
**Implemented and verified - YYYY-MM-DD**

- Changed: concrete behavior and affected boundary.
- Verification: command/check and observed result.
- Remaining: deferred features, dependencies or known limitations.
```

For design-only work, use **Design documented** and explicitly say that no runtime feature was implemented or tested.

## 5. Architecture and boundaries to preserve

- C#/.NET owns authoritative world state and simulation mutations. Terminal and future viewers consume consistent snapshots and use validated services for changes.
- Extend explicit command/field registries; avoid hard-coding features into the input loop or allowing arbitrary property mutation.
- Keep simulation timing independent of display refresh and real wall-clock timestamps. Do not invent agent activity when programs are Idle.
- Human Master Control, an active agent identity and autonomous/system actions are distinct. Names, occupations, uniforms and hardware do not grant permissions.
- Administrative editing uses validation, serialized mutation, revisions and authorization. An injectable authorization contract is not a production login provider.
- Keep local-first operation, persistence, future host tasks and rendering responsibilities separate. Follow the current design documents for details.

Relevant contracts: [Program Editing Rules](../Project_GRID_Program_Editing_Rules.md), [Session Context](../Project_GRID_Session_Context_and_Authorization.md), and [Terminal Identity Plan](../Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md).

## 6. Recent context to verify before continuing

These are historical observations, not guarantees about the next checkout:

- Genesis was released under `v0.1.0-genesis`. Later editing/startup changes were separate work; do not move the release tag to include them.
- The 1.9.3 solution run passed 117 tests on 2026-10-17; the 1.10 live-view run passed 140 tests on 2026-10-18. Do not present that as a new test run or assume the count still matches later changes.
- The terminal supports optional `--start` automatic ticking. Live view was implemented in section 1.10 on 2026-10-18, with Q/Esc return, paging, bounded event history and console fallback. Check current source before relying on this historical status; login visibility filtering remains deferred.
- The Grid.Persistence scaffold was subsequently replaced by section 2.1 schema/migration implementation on 2026-10-18. Its 31 tests bring the solution to 171 passing tests. Section 2.2 subsequently implemented real engine save/load, autosave, orderly shutdown saves and explicit backup recovery. Its 2026-10-18 verification passed 200 solution tests (8 Core, 93 Simulation, 50 Terminal, 49 Persistence). Default storage is %LOCALAPPDATA%\GRID\world.db; --world selects an isolated file, --recover restores its validated backup, and --ephemeral keeps a disposable session. Do not launch smoke tests against the user's default world. Durable events and hardware-generated cities remain pending.
- GRID-REQ-004 was documented across eight existing documents on 2026-10-18: discover motherboard, CPU, GPU, RAM, storage and network hardware on first-time world creation, generate a unique city, save it and reuse the world on later startups.
- That requirement was documentation-only. Its detailed contract is in the design specification and its unchecked implementation tasks are in checklist 2.6, with later Phase 7/8 integration.
- Existing saved worlds take priority over hardware changes or regeneration. Preserve Genesis history and the existing city, atmosphere and fashion direction.
- The user clarified that the eight supplied documents were the current files in MD Project Files. Older Downloads copies contained no GRID-REQ-004. The written requirement supplied the addition; no separate updated file set was available for a version-to-version merge.

Reconcile source, tests, Git history and dated notes if they disagree. Preserve newer work and explain the discrepancy rather than silently reverting it.

## 7. Terminal, Git and permission practices

When asked to run GRID, open the requested interactive terminal and verify the application process starts. Typical automatic startup from the repository is:

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start
```

Minimizing the window lets the user continue working; it does not install a background service. Do not close an existing instance unless requested. If replacing one, identify only the relevant application/terminal processes. Check current persistence support before implying state survives exit.

Before committing or pushing, inspect the branch, remote, status and staged diff. Include only the authorized scope and preserve unrelated work. A created commit is not proof of a successful push. In this chat, Git Credential Manager once blocked a push with a Select an account window; ask the user to complete account selection and verify the eventual result. On resuming an interrupted turn, check whether the earlier operation completed before retrying it.

Use the active tool permission mechanism when required. If an operation is blocked, explain the concrete blocker and request only the action needed. Do not ask for credentials in chat, bypass restrictions or report an approval as execution success.

## 8. Starting a future session well

Read this guide, the latest checklist and relevant contracts, then inspect the current working tree. Identify the user's requested outcome, the last verified state and any unfinished work. Carry forward their preference for expandable design and completion notes, while treating the present user request as the task boundary.

Do not automatically start the next module, implement a planning request, launch processes or push commits merely because those actions occurred earlier in this conversation. The useful continuity is the project's context and working standards; each action still needs to fit the current request.


## Existing-code adaptation plan - 2026-10-18

The user requested explicit retrofit tracking for already completed modules affected by GRID-REQ-005. Use checklist 2.7.1 and the shared retrofit mapping; do not uncheck historic completion entries or claim new compliance from old tests. Address classification and safe lifecycle first, coordinate persistence/startup, then integrate telemetry, editing and live view before more Phase 3 behavior.

See [existing implementation retrofit](../Project_GRID_Host_Programs_and_Resources.md#existing-implementation-retrofit---planned-2026-10-18) and checklist 2.7.1. Documentation only; implementation remains pending.

## Task and MCP readability follow-up — 2026-10-21

Implemented compact agent/task tables with role names, unique short references, a compact `mcp plans` overview, and separated plan/task details, results, next commands and full audit identities. `mcp brief` still includes all plan details; `mcp inspect <reference>` reads one plan. Full IDs remain in detail views. Displayed short references (at least eight characters) are accepted by task inspect/approve/cancel/retry and MCP inspect/brief; ambiguous prefixes are rejected, and resolution is restricted to the current authorized scope. A copied trailing period or comma is tolerated. Plan/task mixups produce specific guidance. Approval boundaries and persisted data are unchanged. Persistence warnings now appear only on I/O/database errors.

Use `tasks list`, then `tasks inspect <displayed-task-reference>`. Review its argument before using the displayed approval command. Command examples are printed on separate lines without sentence punctuation. Verification: 219 terminal tests passed, including short-reference, punctuation, plan/task mismatch and cross-agent scope checks.
