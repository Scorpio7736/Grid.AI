# GRID7736 - Terminal Help

**6.17 registry foundation — 2026-11-04.** Versioned model manifests, checksum verification, retained-revision rollback, shared-asset accounting, candidate tracking, persistent force-Lite and device diagnostics are implemented. 1,109 solution tests and live baseline inventory verification passed. Exact specialist artifacts, device benchmarks and automatic profile promotion remain pending. Running chat remains on the pinned Qwen3 baseline. [Implementation and commands](Project_GRID_Model_Registry_Implementation.md).

**6.15 import implementation — 2026-11-04.** Offline PDF text extraction and DOCX body paragraph/table import are locally verified. The bundled parser uses bounded worker processes, page/paragraph provenance and explicit empty/scanned/encrypted/corrupt-file outcomes. 1,073 regression tests passed across current suites, with standalone PDF/DOCX deployment checks. Second-device acceptance and the final 6.7–6.31 gate remain pending. [Implementation, scope and verification](Project_GRID_Offline_Document_Import_Implementation.md).

**2026-10-30 transfer/AI follow-up:** Removed repeated full-content transfer checks while retaining independent destination verification and pinned model/manifest matching. Hashing now reports byte progress; ETA uses measured copy/verification work. AI diagnostics retain startup errors, and bundled startup tolerates slow media. Original-PC AI passed an isolated test; the reported failing instance remains undiagnosed pending its exact error. [Implementation, evidence and limits](Project_GRID_Transfer_Checks_and_AI_Diagnostics.md). Physical second-PC acceptance and 6.15 remain pending.

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


**Implemented 2026-10-28 — Desktop file destination (6.9 follow-up):** Explicit Desktop requests now use the host Windows Desktop known folder, including OneDrive redirection. This file-only destination is `@desktop`; it does not require a `.csproj`, register a development workspace, or permit builds/tests. Registered workspace aliases still resolve to their own roots; an absolute Desktop path explicitly paired with a repository alias remains refused. Arbitrary destination folders are not added by this change.

Example: `create a file on my desktop called test.md and fill it with 10 random numbers.` GRID stages ten integers from 0 through 999 (duplicates possible), shows the complete preview and absolute path, then requires `approve this change` followed by `apply this change`. Requests for 1–100 random numbers use local generation without AI. Other drafting instructions use the existing optional local AI draft path. `create a file on my desktop` asks for a filename and content. `read file test.md on my desktop`, update/delete equivalents, and explicit `changes ... @desktop ...` use the same bounded UTF-8 text policy, identity/activity checks, guarded backups and rollback. Delete/code changes still require high-risk approval. Recovery syntax: `recover change <id> in @desktop`. No outside-root traversal, missing-parent creation, or implicit overwrite is allowed.

Creating an existing file now reports its absolute path and asks the user to read, rename, or explicitly update it, even when its content matches. An incomplete `creat` receives deterministic file-workflow guidance rather than AI instructions to manually create a file. Disconnect clears pending requests and receipt scope so reconnect does not consume the next request with an expired-state response. Verification: **951 tests passed** (14 Core, 157 Simulation, 99 Persistence, 681 Terminal), including six new cases using a synthetic Desktop directory. No real user Desktop files were changed by verification. No schema migration or new external dependency.


**2026-10-28 — verified file destinations:** Previews, captured reads and applied/rollback receipts now show the full absolute target path. In chat, “Where did you save the file?” and “What is the file path?” use the latest operation receipt in the current identity/conversation scope, refreshing its draft state when available. These answers bypass AI, distinguish unperformed drafts from applied operations, and explicitly do not claim a fresh filesystem check. Without a valid receipt, GRID cannot confirm a location. A workspace alias resolves to its registered root; `edits-demo` is not implicitly Desktop. Verification: 945 automated tests passed, including destination/state checks, explicit-command apply, and reconnect isolation. Existing user files were not moved.

**6.9 follow-up repaired — 2026-10-28:** Workspace clarifications display immediately, name the missing alias and show available aliases while retaining the request. Printable keys used to skip a reply are preserved for the next input; secret entry clears that one-off buffer. Markdown fence-only lines are ignored without calling AI or clearing the pending request. A disconnected file request explains how to reconnect; `reate file` requests a correction instead of asking the model to invent a destination. **944 tests passed**, plus six real Windows-console checks. The supplied unexpected disconnect was not reproduced; the tested flow retains its connection. [Console evidence](verification/6.9-input-repair-report.md).

**6.9 implemented — 2026-10-27:** Connected chat supports `create/read/update/delete file <path> in <workspace>`, literal `with content` or optional AI `with instructions`, then exact preview → `approve this change` (or `approve high-risk`) → `apply this change`. Use `undo this change`, or `recover change <id> in <workspace>` after restart. Attached requests execute on the original host. [Full examples, explicit commands and limits](Project_GRID_Conversational_File_Operations.md).

**6.8 implemented — 2026-10-27:** After connecting, type ordinary sentences or use `talk`. Quotes are preserved. `select files` opens once and runs analysis after scoped import; retry failures with `continue to analyze`, `analyze`, or `select files`. `cancel` and `go back` leave/change the flow; a new question replaces it. Paths require confirmation before reading. [Examples and limits](Project_GRID_Conversation_Recovery_Implementation.md).

**6.7 implemented — 2026-10-27:** Login enters the terminal after a five-second logo animation; any key skips. `GRID_ANIMATION=off` selects static reduced-motion output. Messages/errors are yellow, user text white, prompts/labels light blue. [Details and visual test](Project_GRID_Login_Animation_and_Colors.md). Earlier planned/red-error/Enter-gate notes describe previous behavior.

**Remaining before Phase 7:** Extended memory/MCP workflows, portable AI repair and PDF/full DOCX support remain planned. 6.7 login/colors, 6.8 natural chat/recovery and 6.9 scoped CRUD are implemented. [Remaining plan](Project_GRID_Pre_Phase_7_Readiness_Plan.md).

**6.6 unified talk — 2026-10-27:** Start with `grid -start`, then `talk How can I center a div?`, `talk analyze my document`, `talk check resources` or `talk edit a file`. Follow the questions using `talk`; use `talk cancel` to abandon a follow-up. `talk details` reveals analysis sources. Plans require `talk submit plan`, then exact-ID approval and separate dispatch. [Full commands, examples and boundaries](Project_GRID_Unified_Talk_Workflow.md). `tasks run <id>` dispatches only that approved task; `tasks run` still selects the next queued task.

**6.5 diagnostic — 2026-10-27:** From PowerShell, run `Test-GridPortable.ps1 -PackageRoot <package-folder> -IntelligenceOnly`, or the packaged `launch.cmd --verify-intelligence`. These are diagnostic launch options, not `grid>` commands. They use a temporary synthetic world/workspace, retain a JSON report and return failure if a check fails. [Commands, evidence and limitations](Project_GRID_Intelligence_Verification.md).

**6.4 implemented — 2026-10-26:** Original-host `changes` drafts new/revised workspace files from captured context or local MAXIS AI, previews complete content/hashes, requires explicit approval, and applies with backups and guarded rollback. Optional verification uses separately approved MAXIS tasks. **838 Release tests passed**. Drafts are transient; recovery files persist. [Workflow, limits and demo](Project_GRID_Authorized_Editing_Implementation.md).

**6.3A implemented — 2026-10-26:** `screen options/monitoring/ask/status/stop` provides one-shot selected-monitor assistance using offline Windows OCR and local AI. Human Master Control only; no recording, saved screen context or actions. **799 Release tests passed**, including attached-window and synthetic native-OCR checks. User desktop acceptance remains manual. [Commands, limits and verification](Project_GRID_Screen_Assistance_Implementation.md).

**4.2H implemented — 2026-10-26:** Conversation labels are light blue, agent bodies yellow and user text white. Assistance now animates; Enter no longer accidentally skips a reply. **772 Release tests passed**, plus an interactive Windows-console probe. User desktop/window visual acceptance remains open. [Changes, verification and test commands](Project_GRID_Conversation_Display_Repair.md).

**AI response recovery — 2026-10-26:** Truncated answers now get one bounded larger retry, and rejected responses are distinguished from an offline service. Advice and analysis prompts were refined; MCP greetings no longer receive unrelated district counts. **759 Release tests passed**, and the bundled model answered the reported HTML question successfully. [Fix, limits and regression test](Project_GRID_AI_Response_Recovery.md).


**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**Automatic conversation recall added — 2026-10-25:** Ordinary saved discussions and their relevant details now carry across conversations/restarts, alongside explicit memory. Private recall stays within the same agent/project; shared meeting recall requires the exact same group. `memory history` reviews candidates; `memory forget-history` excludes prior discussions while retaining explicit memories and transcripts. **728 tests passed**; bundled-model recall/clearing verified. [Behavior, bounds and test](Project_GRID_Automatic_Conversation_Recall.md). This supersedes earlier explicit-only recall descriptions.


**6.2B completed — 2026-10-25:** Explicit `memory save/list/correct/forget` controls now preserve user-confirmed context by agent/project across conversations and restarts. Recall excludes stale/forgotten context and private meeting imports; tone instructions favor direct, serious, useful replies. Schema v23; **723 tests passed**, plus bundled-model recall/correction/forgetting checks. [Commands, scope and limitations](Project_GRID_Persistent_Conversation_Memory_Implementation.md). Earlier 6.2B-pending notes are historical; 6.3 task/context assistance remains planned.


**6.2A completed — 2026-10-25:** `agent meetings MCP MAXIS JN` opens a shared conversation window; `talk @MAXIS <message>` directs a turn. One responder is chosen by deterministic specialty-keyword rules, with shared history and private-context isolation. Reviewed voice uses `-inline` in the original host. Schema v22; **697 tests passed**. [Commands, limits and verification](Project_GRID_Multi_Agent_Meetings_Implementation.md). Earlier meeting-pending statements below describe historical milestones; 6.2B memory and 6.3 task assistance remain planned.


## Agent lifecycle — 5.9

Start GRID before talking to agents or running tasks. Existing identities/history remain saved while paused; transcripts and task inspection remain available. `agents initialize` only shows migration guidance, and connect no longer creates MCP. Explicit launch `--start` initializes after owner login; normal startup is paused.

```text
grid
    - grid -start
      # USAGE: Initialize/resume six agents and the simulation; no automatic task approvals or dispatch.
    - grid -pause
      # USAGE: Pause agent activity; cancel pending replies and interrupt active task attempts safely.
    # EXAMPLE: grid -start
agents
    - agents
      # USAGE: Show saved roles and active/paused lifecycle state.
    # EXAMPLE: agents
```

Active host actions stop cooperatively and may have partial external effects. Interrupted attempts require review, retry and fresh approval. Queued work needs explicit dispatch after resume. Attached Master Control terminals can start/pause the same GRID. [Lifecycle details and test](Project_GRID_Agent_Lifecycle_Implementation.md).


## Terminal controls and task views — 4.2G

```text
grid
    - grid -start
      # USAGE: Initialize/resume agents and the simulation as Master Control.
    - grid -pause
      # USAGE: Pause simulation and agent activity; in-flight work settles cooperatively.
    - grid -copyto -select
      # USAGE: Pick a container for a new GRID7736 copy; review and confirm y/n.
    - grid -transferto -select
      # USAGE: Pick a container for a new GRID7736 move; review and confirm y/n.
    - grid -copyto "E:\"
      # USAGE: Copy to E:\GRID7736; refuse an existing target and preserve other drive files.
    - grid -transferto "E:\"
      # USAGE: Move to E:\GRID7736 using the verified transfer and cleanup flow.
    # EXAMPLE: grid -copyto -select

tasks
    - tasks
      # USAGE: Show all authorized history, including completed, denied and labeled archives.
    - tasks -completed [-AGENT]
      # USAGE: Show only Completed tasks.
    - tasks -list [-AGENT]
      # USAGE: Exclude Completed and Denied; cancelled/failed/archived records remain visible.
    - tasks deny <reference> "reason"
      # USAGE: Master Control rejects an AwaitingApproval task with a saved reason/audit.
    # EXAMPLE: tasks -list -MAXIS
```

`tasks list` remains an alias for all history; `-archived` limits it to archives. Old `start`/`pause` only show migration guidance. Transfers remain original-host-only. Selecting a folder creates its GRID7736 child; explicit non-root paths remain exact new destinations. Existing targets are never merged or overwritten. Agent lifecycle coupling is implemented in 5.9. [Implementation and verification](Project_GRID_Terminal_Controls_Implementation.md).


## Bundled AI and animation (2026-10-23)

Normal GRID startup owns the bundled local AI server. Fresh source clones need `Prepare-GridRuntime.ps1`; prepared portable packages use `launch.cmd`. Use `ai status` / `ai test` after login, or `launch.cmd --verify-runtime` for an isolated package test. No separate global Ollama server is required.

`typewriter on` and `typewriter off` now work in original-host inline chat and attached chat. Fallback replies animate too; redirected streams and TERM=dumb remain instant. Explicit on overrides an initial GRID_TYPEWRITER=off preference.


## Conversational display

Use `talk <message>` to chat. Normal replies and `transcript` show You / agent text; technical evidence stays saved.

```text
transcript
    - transcript
      # USAGE: Read the current conversation in a simple chat layout.
    - transcript --details
      # USAGE: Include saved facts, sources, message ticks, origins and IDs.
    - transcript <conversation-id> --details
      # USAGE: Inspect an accessible saved conversation with full evidence.
    # EXAMPLE: transcript --details
```

Conversation windows show a compact welcome and You> prompt. Use `whoami` for authority/session details. Replies are AI-generated when available; chat itself does not execute tasks.


## Shared local AI diagnostics — 6.1

```text
ai
    - ai status
      # USAGE: Inspect the local runtime, installed model, queue and GPU allocation; Master Control/original host only.
    - ai test
      # USAGE: Send a fixed readiness prompt through the shared local queue and show output, elapsed time and sampled CPU/memory.
    # EXAMPLE: ai test
```

If unavailable, run `Start-GridLocalAI.ps1` from PowerShell beside the built terminal or in the repository. It starts a separate local server on port 11435 using installed Ollama/model files. No inference request downloads a model. `GRID_AI_DISABLED=1` disables generation; deterministic GRID remains usable. Agent conversations now use this queue with grounded context and deterministic fallback; see [6.2](Project_GRID_Grounded_Conversations_Implementation.md). [Setup, license and diagnostic limits](Project_GRID_Shared_Local_AI_Implementation.md).


## Bulk task commands — 5.8 (2026-10-22)

```text
tasks
    - tasks list -MAXIS
      # USAGE: List all visible retained tasks for one registered agent, including labeled archives.
    - tasks list -archived -MAXIS
      # USAGE: Read retained archived history for that agent.
    - tasks approve -all
      # USAGE: Preview and confirm all AwaitingApproval tasks in this fixed batch.
    - tasks approve -all -MAXIS
      # USAGE: Preview and confirm only MAXIS proposals; approval does not execute them.
    - tasks run -all
      # USAGE: Dispatch the selected approved queue sequentially; stop on the first unsuccessful attempt.
    - tasks run -all -MAXIS
      # USAGE: Dispatch only selected approved MAXIS tasks; report outcomes and skipped tasks.
    - tasks clear -all
      # USAGE: Confirm removal from the operational list while retaining history; cancel pending work and refuse running work.
    - tasks clear -all -MAXIS
      # USAGE: Archive only the selected MAXIS tasks after confirmation.
    # EXAMPLE: tasks list -MAXIS
```

Agent switches accept any initialized role, case-insensitively; omit the agent filter for all visible tasks. Bulk approval/clear/run require Master Control in the original host. Approval/clear require interactive y/n and reject stale previews. Clear retains results, audits, plan/notification links and artifacts, and does not free the existing 256-task cap. Use `tasks list -archived` or `tasks inspect ID` for history. Existing single-task commands remain supported. [Full behavior and acceptance test](Project_GRID_Bulk_Task_Implementation.md).


## Conversation-window update — 4.2E (2026-10-21)

`connect` and `resume` now open a dedicated conversation window by default. The original terminal remains usable. Within the new window, `talk` returns a labeled typewriter response; any key except Enter skips animation. `transcript` is instant. `exit` closes the view while retaining an open conversation for later resume; `disconnect` ends it. Parent logout/identity changes revoke attached views. Meetings are implemented in 6.2A; use `agent meetings MCP MAXIS` (or add `-inline`).

```text
connect
    - connect <program-id|name>
      # USAGE: Open a dedicated conversation window; omitted contact defaults to MCP.
    - connect -inline [program-id|name]
      # USAGE: Keep instant-text chat in this terminal, including original-host voice support.
    # EXAMPLE: connect MCP

resume
    - resume <conversation-id>
      # USAGE: Open a window for an existing open conversation owned by this participant.
    - resume -inline <conversation-id>
      # USAGE: Resume here for original-host voice or instant inline interaction.
    # EXAMPLE: resume -inline <full-conversation-id>

typewriter
    - typewriter on
      # USAGE: Animate new agent responses in this dedicated conversation window.
    - typewriter off
      # USAGE: Display agent responses instantly in this window.
    # EXAMPLE: typewriter off
```

`typewriter` is available in dedicated conversation windows. Set `GRID_TYPEWRITER=off` before launching GRID for reduced motion across inherited windows. Redirected input/output and dumb terminals always use instant text. For voice, use `connect MCP -inline` in the original host before `voice listen`. [Details and acceptance test](Project_GRID_Conversation_Windows_Implementation.md).


## VOLT and RELAY — 5.6 completed (2026-10-21)

VOLT now reports existing host telemetry and supports approved normal/eco/auto pacing of GRID's automatic simulation. Unknown/unsupported GPU data is preserved; Windows resources and manual tick behavior are unchanged. RELAY provides a saved local inbox, recorded-task briefings, human acknowledgements and explicitly approved offline speech with delivery/failure receipts. Schema v19 preserves mode and inbox history without automatic playback/replay. **551 tests passed** (14 Core, 157 Simulation, 97 Persistence, 283 Terminal). See [VOLT/RELAY implementation and acceptance test](Project_GRID_VOLT_RELAY_Implementation.md) for commands and boundaries. Next: **5.7 end-to-end verification**. Earlier milestone/schema/test statements below remain historical.


## JN storage — 5.5 completed (2026-10-21)

JN now inventories approved paths, reports possible duplicates, and prepares cleanup proposals without deleting source files. Defined GRID-managed storage is measured against an initial 20 GB decimal logical budget; MCP quota escalations require separate Master Control approval to change it. This is tracking/governance, not a hard write cap or disk reservation. Schema v18 preserves scope and quota history. **535 tests passed** (14 Core, 157 Simulation, 97 Persistence, 267 Terminal). See [JN implementation and acceptance test](Project_GRID_JN_Storage_Implementation.md) for limits, accounting roots, report retention and remaining planned automation. Next: **5.6 VOLT and RELAY**. Earlier milestone/schema/test statements below remain historical.


## SENTINEL security — 5.4 completed (2026-10-21)

SENTINEL now reads local Defender protection/scan timestamps, effective firewall profiles, and bounded Security-log metadata through individually approved tasks. Timestamped results explicitly distinguish real host observations, warnings and unavailable data from simulated incidents. No scans, setting changes, elevation or remediation are enabled. Schema v17 persists these results through the existing broker. **518 tests passed** (14 Core, 157 Simulation, 97 Persistence, 250 Terminal), including 13 new security cases and live read-only provider verification. See [SENTINEL implementation](Project_GRID_SENTINEL_Security_Implementation.md). Next: **5.5 JN — Storage and files**. Earlier milestone/schema/test statements below remain historical.


## MAXIS development — 5.3 completed (2026-10-21)

MAXIS now supports registered-workspace inspection, fixed .NET build/test execution, saved diagnostics and explicit replacement suggestions staged outside original source. Every task requires human approval; build/test executes trusted project code as the current Windows user (no OS/network sandbox). Schema v16 persists immutable, revocable workspace registrations. Other specialists remain demonstrations. **505 tests passed** (14 Core, 157 Simulation, 97 Persistence, 237 Terminal). See [MAXIS implementation](Project_GRID_MAXIS_Development_Implementation.md) for commands, offline prerequisites, artifact retention and limits. Next: **5.4 SENTINEL**. Earlier milestone/schema/test statements below remain historical.


**Implemented - 4.2B, 2026-10-20.** This bundled reference is available offline through `help -md`, which displays its path and Markdown in the terminal. Live `help` and Tab suggestions are capability-scoped; this full manual does not grant command access.

New worlds prompt for a username and masked password (6..24 characters), then use `login`. Existing Windows-owned worlds use `login master`, then `credentials` to migrate or rotate credentials as Master Control. Passwords are entered only at masked prompts, never as command arguments. Password login/setup refuse redirected input. Examples containing placeholders require actual values; `nge` deletes the selected world after confirmation.

## Login and identity

```text
login
    - login master
      # USAGE: Authenticate with masked local credentials, or the Windows owner for an unmigrated world.
    # EXAMPLE: login master
```

```text
credentials
    - credentials
      # USAGE: Master Control creates/replaces local credentials using masked prompts; sign in again afterward.
    # EXAMPLE: credentials
```

```text
whoami
    - whoami
      # USAGE: Show operator, active role, session and capabilities.
    # EXAMPLE: whoami
```

```text
register-agent
    - register-agent <id|name>
      # USAGE: Master Control registers an existing Agent identity.
    # EXAMPLE: register-agent MCP
```

```text
switch
    - switch master
      # USAGE: Select Master Control.
    - switch <agent-id|name>
      # USAGE: Select a registered agent identity.
    # EXAMPLE: switch MCP
```

```text
self
    - self
      # USAGE: Show the active identity and its own resident state.
    # EXAMPLE: self
```

```text
logout
    - logout
      # USAGE: Confirm with y; n/blank cancels. Confirmed logout clears context and returns to welcome.
    # EXAMPLE: logout
```

## Simulation and world

```text
grid
    - grid -start
      # USAGE: Start or resume simulation; Master Control required.
    # EXAMPLE: grid -start
```

```text
grid
    - grid -pause
      # USAGE: Pause simulation.
    # EXAMPLE: grid -pause
```

```text
step / tick
    - step
      # USAGE: Advance one tick while paused.
    - tick
      # USAGE: Alias for step.
    # EXAMPLE: step
```

```text
status
    - status
      # USAGE: Read current world/clock state.
    # EXAMPLE: status
```

```text
city
    - city
      # USAGE: Read saved city layout and hardware provenance.
    # EXAMPLE: city
```

```text
districts
    - districts
      # USAGE: List district populations.
    - districts ids
      # USAGE: Show full identifiers.
    # EXAMPLE: districts ids
```

```text
programs
    - programs
      # USAGE: List resident programs.
    - programs -active
      # USAGE: Show fresh Running application observations.
    - programs -possible
      # USAGE: Show current/known application candidates; not proof of current installation.
    - programs -sleeping
      # USAGE: Show fresh On call or Sleeping applications.
    - programs ids
      # USAGE: Show full identifiers; combine with at most one availability filter.
    # EXAMPLE: programs -active ids
```

```text
inspect
    - inspect program <id|name>
      # USAGE: Inspect one program.
    - inspect district <id|name>
      # USAGE: Inspect one district.
    # EXAMPLE: inspect program MCP
```

```text
jobs
    - jobs
      # USAGE: Show simulated civic workstation assignments and vacancies.
    # EXAMPLE: jobs
```

## Monitoring

```text
live
    - live
      # USAGE: Open dashboard; Q/Esc returns to commands. Requires Master Control.
    # EXAMPLE: live
```

```text
behaviors
    - behaviors
      # USAGE: Show resident needs/actions and simulated work progress.
    # EXAMPLE: behaviors
```

```text
activity
    - activity
      # USAGE: Show measured application availability and working-set evidence.
    # EXAMPLE: activity
```

```text
social
    - social
      # USAGE: List opted-in social residents and fictional civic pools.
    - social <id|name>
      # USAGE: Show one resident's social memories/relationships.
    # EXAMPLE: social MCP
```

```text
resources
    - resources
      # USAGE: Show host measurements.
    - resources details
      # USAGE: Include measurement details and provenance.
    # EXAMPLE: resources details
```

```text
history
    - history
      # USAGE: Read saved events; requires a persistent world and Master Control.
    - history details
      # USAGE: Include structured event details.
    - history kind=<kind>
      # USAGE: Filter event kind.
    - history entity=<guid>
      # USAGE: Filter entity ID.
    - history severity=<level>
      # USAGE: Filter severity.
    - history since=<UTC>
      # USAGE: Use a round-trip UTC timestamp, e.g. 2026-10-20T00:00:00.0000000+00:00.
    - history limit=<1..500>
      # USAGE: Limit returned rows.
    - history offset=<0..>
      # USAGE: Skip rows; combine filters as needed.
    # EXAMPLE: history kind=Edit limit=20
```

## Conversations

```text
connect
    - connect
      # USAGE: Open a conversation window with MCP; Master Control can create the contact lazily.
    - connect <id|name>
      # USAGE: Open a directed conversation window; identities must be distinct.
    # EXAMPLE: connect MCP
```

```text
talk
    - talk <message>
      # USAGE: Send in the active conversation; normal replies show conversational text. Use transcript --details for saved facts and sources.
    # EXAMPLE: talk status
```

```text
conversations
    - conversations
      # USAGE: List conversations in which the active identity participates.
    # EXAMPLE: conversations
```

```text
transcript
    - transcript
      # USAGE: Read the active conversation.
    - transcript <conversation-id>
      # USAGE: Read a permitted stored transcript.
    # EXAMPLE: transcript
```

```text
resume
    - resume <conversation-id>
      # USAGE: Open a window for a permitted conversation; copy its ID from conversations.
    # EXAMPLE: resume <conversation-id>
```

```text
disconnect
    - disconnect
      # USAGE: End the active conversation and retain its transcript.
    # EXAMPLE: disconnect
```

## Master Control editing

```text
edit
    - edit <program-id|quoted-name> <field> <value>
      # USAGE: Preview name, occupation, energy, status or district. District needs a full ID. Non-demo energy/status are service-owned and cannot be edited.
    # EXAMPLE: edit MCP occupation "Local coordinator"
```

```text
apply-edit
    - apply-edit <preview-id>
      # USAGE: Commit the current preview if identity and revision still match.
    # EXAMPLE: apply-edit <preview-id>
```

```text
cancel-edit
    - cancel-edit
      # USAGE: Discard the current preview.
    # EXAMPLE: cancel-edit
```

## Help and terminal

```text
help
    - help
      # USAGE: Show capability-scoped console help.
    - help -md
      # USAGE: Display this bundled Markdown reference and its path offline in the terminal.
    # EXAMPLE: help
```

```text
clear
    - clear
      # USAGE: Clear display when supported; retain world state.
    # EXAMPLE: clear
```

```text
nge
    - nge
      # USAGE: Master Control only. Review the destructive warning; y confirms and n cancels. Requires interactive persistent session.
    # EXAMPLE: nge
```

```text
exit
    - exit
      # USAGE: Stop this local terminal host and save the persistent world.
    # EXAMPLE: exit
```

## Startup options (shell, not grid prompt)

```text
Grid7736
    - Grid7736 --start
      # USAGE: Request simulation start after successful login.
    - Grid7736 --world "<path>"
      # USAGE: Select a world file; parent directory must exist.
    - Grid7736 --recover
      # USAGE: Explicitly recover the selected/default world from backup.
    - Grid7736 --ephemeral
      # USAGE: Run without persistent storage; incompatible with --world/--recover.
    - Grid7736 --no-color
      # USAGE: Disable ANSI/console colors; combine with other valid options.
    # EXAMPLE: Grid7736 --no-color
```

## Login/welcome workflow

Opening GRID7736 graphic -> first-run credential setup if needed -> login -> clear screen -> welcome username and Master Control authority -> Enter -> terminal. Agent selection remains available after authentication. Logout asks y/n; cancellation retains context, confirmation clears the screen and returns to welcome. Expiry denies commands immediately and returns to welcome at the next input boundary. EOF/Ctrl+C exits through normal save/shutdown handling.

## Palette and completion

Base text is light blue (Console Cyan), input white, tables/dividers yellow, and errors red. `--no-color` and redirected output remain plain text. Tab cycles permitted command/static option prefixes without executing. Arrow keys, Home/End, Backspace/Delete edit input. Quoted names remain editable, but completion inside quotes is suppressed; no resident names, private IDs, paths, or secret values are suggested. Password entry is masked and has no completion or history. Q/Esc in live view returns to commands.

## Additional windows (4.2C)

```text
<read-only-command>
    - <read-only-command> -opninwin
      # USAGE: Open supported data as a labeled snapshot in a separate window; combine with normal command arguments.
    # EXAMPLE: programs -active ids -opninwin

live
    - live -opninwin
      # USAGE: Open an updating view of the same GRID; Q/Esc closes this view.
    # EXAMPLE: live -opninwin

ntm
    - ntm
      # USAGE: Open another terminal connected to the same running GRID, with its own command/chat/edit context and scoped authority.
    # EXAMPLE: ntm
```

Snapshot commands: status, city, districts, programs, inspect, behaviors, resources, history, jobs, activity, social, self, whoami, conversations, transcript and help. Permissions still apply; mutating commands cannot use the flag. Scroll a snapshot and press Enter to close. Live supports PageUp/PageDown and left/right event paging.

Attached `exit` detaches only that window. Original-host `exit` saves and disconnects all attachments. Logout/expiry/role changes revoke dependent windows. Agent attachments cannot promote themselves. Run NGE, transfers and credential changes in the original host. Reopen from an authenticated terminal after disconnection; clients never load another world automatically. See [window implementation](Project_GRID_Shared_Terminal_Windows_Implementation.md).

## Move/copy commands (4.2D / 4.2F)

Master Admin means the existing Master Control role. Both operations require an interactive persistent session in the original host and close it after confirmation and final save. Choose a new absolute destination directory with an existing parent. These commands are entered inside GRID.

```text
grid
    - grid -copyto "location"
      # USAGE: Copy the GRID application and selected save inventory after preview and y/n confirmation; keep the original.
    - grid -transferto "location"
      # USAGE: Move the GRID application and selected save inventory after preview and y/n confirmation; source cleanup runs after exit.
    # EXAMPLE: grid -copyto "E:\GRID7736 Copy"
```

Supply one operation and one destination; quote paths with spaces. Switches are case-insensitive. `copexist` and `tranexist` only show the new syntax and perform no transfer. Attached terminals and conversation windows must use the original host for transfers.

Use `launch.cmd` at the destination; a compatible .NET 10 runtime is required. For moves, wait for `transfer-status.txt` to report completion. Source cleanup failures retain the verified destination and report incomplete status. The transfer excludes unrelated host files, source repositories and other installed worlds. Configure local password credentials before using a different PC. See [transfer implementation](Project_GRID_Transfer_Implementation.md) for scope and recovery limits. These commands do not create attached terminals; use `ntm` for that. Transfers disconnect attachments before saving/copying.

## Password requirements - completed follow-up (2026-10-20)

**Implemented.** New passwords and password changes accept **6–24 characters inclusive**. Uppercase letters, lowercase letters, numbers and symbols are allowed; no character category or mixture is mandatory. Preserve password case and exact characters; do not silently trim or truncate passwords.

Creation/change validation, masked setup prompts, error messages and help now use **6–24 characters**. Existing saved credentials remain verifiable up to the former 256-character limit until the owner explicitly changes the password; the input reader retains that capacity for legacy login. Masked input, salted hashing and authorization are unchanged. Control characters remain rejected. **Completion verification: 390 tests passed** (14 Core, 155 Simulation, 91 Persistence, 130 Terminal), including 13 new cases for boundaries, permitted categories, exact case/spacing, confirmation mismatch, legacy 25/256-character login after restore, and authenticated rotation to the new policy. No saved credential migration or password reset is required.

## User-supplied context (4.3 / 4.3A)

```text
context
    - context add "file" <temporary|persistent> "project" [registered-agent-id|name]
      # USAGE: Master Control imports one explicit local file. Omit agent for owner-only access. Persistent imports save with the world; temporary imports end when the importing session changes identity/logs out or the host exits.
    - context list [project]
      # USAGE: List only context visible to the active identity, optionally filtered by project name.
    - context add -search [temporary|persistent "project" [registered-agent-id|name]]
      # USAGE: Open the Windows file dialog; Ctrl/Shift selects multiple files. Without options: temporary, Inbox, owner only. Cancel imports nothing.
    - context select <id|name> [id|name ...]
      # USAGE: Replace this terminal's file selection. With no arguments, show selection; use context select clear to clear it.
    - context read [id|name ...|-selected]
      # USAGE: Read one or several captured files without reopening sources. With no arguments or -selected, read the current selection. Quote filenames containing spaces; ambiguous names require an ID.
    - context revoke <id|name>
      # USAGE: Master Control removes agent access, retaining the owner's captured content.
    - context remove <id|name>
      # USAGE: Master Control removes the active import; historical backups/already-read output are not erased.
    # EXAMPLE: context add -search persistent "My Project" MCP
    # EXAMPLE: context select "notes.md" "design.docx"
    # EXAMPLE: context read
    # EXAMPLE: context read "notes.md" "design.docx"
```

Register the agent before granting access. Supported inputs: UTF-8 text/Markdown/common source code, PDF text and DOCX body paragraphs/tables. Limits: 1 MiB source, 256 Ki text characters per import, 64 imports and 1 Mi total text characters. No folder grants, automatic rereads, network paths, macro execution or silent truncation. PDF limits include 200 pages and a bounded 20-second extraction worker. Scan-only PDFs require offline OCR or text export. DOC/RTF and unsupported encodings require exporting to a supported format. Re-import for a new captured version. `talk context` reports the recipient's granted-file count; it does not analyze or copy file bodies into transcripts. See [implementation and limits](Project_GRID_User_Supplied_Context_Implementation.md).

## Offline voice communication (4.4)

```text
voice
    - voice status
      # USAGE: Show microphone, language, mute status and unsent draft.
    - voice devices
      # USAGE: List local microphones, installed offline recognizers and Windows voice names.
    - voice microphone <-1|id>
      # USAGE: Choose a microphone; -1 uses the Windows default. IDs come from voice devices.
    - voice <microphone-id>
      # USAGE: Shortcut for microphone selection; voice 0 selects device 0.
    - voice language <culture>
      # USAGE: Choose an installed recognition culture, such as en-US.
    - voice listen
      # USAGE: Connect first. Hold Space to record, release to transcribe, Esc to cancel. Maximum 20 seconds; nothing sends automatically.
    - voice review
      # USAGE: Read the unsent transcript and intended conversation.
    - voice edit <replacement text>
      # USAGE: Correct the current draft before sending.
    - voice send
      # USAGE: Explicitly send the reviewed draft as a conversation message, never as a terminal command.
    - voice cancel
      # USAGE: Discard the draft. During recording/transcription/playback press Esc instead.
    - voice mute <on|off>
      # USAGE: Playback starts muted. Unmuting only permits explicit voice speak; no automatic audio.
    - voice speak
      # USAGE: Speak the connected agent's latest reply locally; Esc interrupts.
    - voice profile
      # USAGE: List visible saved per-agent voice settings.
    - voice profile <agent-id|name> "voice name" <rate> <volume>
      # USAGE: Master Control saves a registered Agent's voice; rate -10..10, volume 0..100. Requires a saved world.
    # EXAMPLE: voice profile MCP "Microsoft David Desktop" 0 80
    # EXAMPLE: voice listen
    # EXAMPLE: voice review
    # EXAMPLE: voice send
    # EXAMPLE: voice mute off
    # EXAMPLE: voice speak
```

Voice hardware requires the original interactive Windows host terminal and installed offline recognition/voice assets. Use `connect MCP` first; registration is required for profiles, not ordinary conversation. Attached terminals continue using `talk` and `resume`. Missing audio/language/voice support leaves typed chat available. No raw recordings are saved. See [offline voice implementation](Project_GRID_Offline_Voice_Implementation.md) for verification and limits.

Voice troubleshooting (2026-10-21): capture reports its duration; recordings shorter than 0.25 seconds are refused with a retry hint. Transcription waits for the recognizer completion event. If recognition still fails, report the displayed stage/exception/HRESULT. No raw audio or helper input is included in that diagnostic. `voice speak` requires a reply from `voice send` or `talk` first.

## Agent/task infrastructure (5.1)

```text
agents
    - agents initialize
      # USAGE: Migration guidance only. Use grid -start to initialize/resume agents.
    - agents
      # USAGE: List role IDs and allowed demonstration capabilities.
    # EXAMPLE: grid -start

tasks
    - tasks list
      # USAGE: List tasks visible to your identity.
    - tasks propose <agent-id|name> <capability> "argument" [request-key]
      # USAGE: Create an immutable proposal awaiting human approval; a stable key deduplicates identical requests.
    - tasks inspect <id>
      # USAGE: Review the exact request, operator/agent attribution, result and audit before approval.
    - tasks approve <id>
      # USAGE: Master Control approves one attempt of the exact proposal.
    - tasks run
      # USAGE: Original host Master Control dispatches one approved demonstration task. No real host action is available.
    - tasks cancel <id>
      # USAGE: Cancel an authorized pending/running task; an attached terminal can cancel while the host waits.
    - tasks retry <id>
      # USAGE: Failed/timed-out/interrupted demonstrations can retry up to three attempts, with fresh approval each time.
    # EXAMPLE: tasks propose MAXIS maxis.demo "hello GRID" example-request-1
    # EXAMPLE: tasks inspect <returned-id>
    # EXAMPLE: tasks approve <returned-id>
    # EXAMPLE: tasks run
```

Capabilities are `mcp.demo`, `maxis.demo`, `sentinel.demo`, `jn.demo`, `volt.demo`, `relay.demo` (1..256 printable argument characters), plus `maxis.delay` (0..5000 ms; attempt timeout is one second). Agents may propose only for their own role, never approve or dispatch. Restart clears queued approvals and interrupts running tasks without automatic replay. No automatic chat-to-task interpretation. See [task broker implementation](Project_GRID_Task_Broker_Implementation.md).

## MCP coordination — 5.2 implemented

```text
mcp
    - mcp request "SPECIALIST: action argument; SPECIALIST: action argument" [request-key]
      # USAGE: Save 1..8 explicit demonstration proposals requiring individual human approval.
    - mcp plans
      # USAGE: View all plans and their child statuses/results.
    - mcp inspect <plan-id>
      # USAGE: Inspect one plan and its approval requirements.
    - mcp brief [plan-id]
      # USAGE: Report broker states/results for one or all plans.
    # EXAMPLE: mcp request "MAXIS: demo hello; JN: demo inventory" example-plan-1
```

Initialize/resume roles with `grid -start`. Only Master Control or registered MCP can coordinate. Actions are demo for specialists and delay for MAXIS; real specialist work remains planned. Inspect and approve each child with `tasks inspect <id>` and `tasks approve <id>`, then dispatch with `tasks run` on the original host. See [MCP coordination](Project_GRID_MCP_Coordination_Implementation.md).

## Task and MCP readability follow-up — 2026-10-21

Implemented compact agent/task tables with role names, unique short references, a compact `mcp plans` overview, and separated plan/task details, results, next commands and full audit identities. `mcp brief` still includes all plan details; `mcp inspect <reference>` reads one plan. Full IDs remain in detail views. Displayed short references (at least eight characters) are accepted by task inspect/approve/cancel/retry and MCP inspect/brief; ambiguous prefixes are rejected, and resolution is restricted to the current authorized scope. A copied trailing period or comma is tolerated. Plan/task mixups produce specific guidance. Approval boundaries and persisted data are unchanged. Persistence warnings now appear only on I/O/database errors.

Use `tasks list`, then `tasks inspect <displayed-task-reference>`. Review its argument before using the displayed approval command. Command examples are printed on separate lines without sentence punctuation. Verification: 219 terminal tests passed, including short-reference, punctuation, plan/task mismatch and cross-agent scope checks.

## MAXIS workspaces and development — 5.3

```text
maxis
    - maxis workspaces
      # USAGE: List permitted workspace registrations.
    - maxis register <alias> "absolute-root" "relative/project.csproj"
      # USAGE: Master Control registers a workspace; individual tasks still need approval.
    - maxis revoke <alias>
      # USAGE: Disable workspace execution and request cancellation of running work.
    # EXAMPLE: maxis register grid "C:\Users\jack\source\repos\Grid7736" "Grid.Core/Grid.Core.csproj"
```

```text
tasks
    - tasks propose MAXIS maxis.inspect "alias|relative/file.cs" [key]
      # USAGE: Propose source inspection with a saved report.
    - tasks propose MAXIS maxis.build <alias> [key]
      # USAGE: Propose fixed dotnet build --no-restore; executes trusted project code.
    - tasks propose MAXIS maxis.test <alias> [key]
      # USAGE: Propose fixed dotnet test --no-restore with diagnostic output/TRX.
    - tasks propose MAXIS maxis.suggest "alias|relative/file.cs|old|new" [key]
      # USAGE: Stage exactly one literal replacement in a separate working copy.
    # EXAMPLE: tasks propose MAXIS maxis.build grid build-1
```

MCP equivalent: `mcp request "MAXIS: build grid"`. Inspect the resulting task, approve it individually, then use `tasks run` from the original host. Builds/tests can change outputs and run project-defined code as your Windows user; they are not OS-sandboxed. Read [MAXIS implementation](Project_GRID_MAXIS_Development_Implementation.md) for supported formats and artifact locations. NGE does not delete these external reports/working copies.

## SENTINEL — read-only security (5.4)

```text
tasks
    - tasks propose SENTINEL sentinel.defender status [request-key]
      # USAGE: Read protection flags, signature update time and prior scan end times; no new scan.
    - tasks propose SENTINEL sentinel.firewall status [request-key]
      # USAGE: Read effective firewall profile settings; no configuration changes.
    - tasks propose SENTINEL sentinel.events recent [request-key]
      # USAGE: Read up to ten newest Security-log IDs/record IDs/timestamps in the last 24 hours.
    # EXAMPLE: tasks propose SENTINEL sentinel.defender status defender-check-1
```

MCP example: `mcp request "SENTINEL: defender status; SENTINEL: firewall status; SENTINEL: events recent"`. Inspect and approve each child, then use `tasks run` once per queued task. Results identify REAL HOST SECURITY and observation UTC. Completed means the query was recorded, including possible UNAVAILABLE access; it does not certify host safety. Remediation and elevation are disabled. See [SENTINEL implementation](Project_GRID_SENTINEL_Security_Implementation.md).

## JN — Storage and files (5.5)

```text
jn
    - jn scopes
      # USAGE: List approved read-only storage scopes.
    - jn register <alias> "absolute-file-or-directory"
      # USAGE: Master Control registers an immutable alias for an existing path.
    - jn revoke <alias>
      # USAGE: Revoke a scope and request cancellation of its running work.
    - jn quota
      # USAGE: Display the logical budget and MCP expansion proposals.
    - jn quota inspect <proposal-reference>
      # USAGE: Review the request, measurement, source task and approval history.
    - jn quota approve <proposal-reference>
      # USAGE: Master Control applies a reviewed logical budget increase; no allocation or deletion.
    # EXAMPLE: jn quota
```

```text
tasks
    - tasks propose JN jn.inventory <scope-alias> [key]
      # USAGE: Save bounded metadata inventory.
    - tasks propose JN jn.duplicates <scope-alias> [key]
      # USAGE: Save possible same-size/SHA-256 duplicate groups.
    - tasks propose JN jn.cleanup <scope-alias> [key]
      # USAGE: Propose manual review of duplicate groups without deleting files.
    - tasks propose JN jn.storage status [key]
      # USAGE: Measure defined GRID-managed storage against its logical budget.
    - tasks propose JN jn.quota <whole-GB> [key]
      # USAGE: Measure storage and escalate an expansion proposal to MCP, without applying it.
    # EXAMPLE: tasks propose JN jn.storage status
```

MCP example: `mcp request "JN: inventory jntest; JN: storage status"`. Inspect and approve each task individually, then dispatch with `tasks run`. Reports contain full paths and coverage limits; duplicates are not automatically safe to delete. See [JN implementation and repeatable test](Project_GRID_JN_Storage_Implementation.md). Quota approval is a separate command; NGE does not delete external JN reports.

## VOLT and RELAY — 5.6

```text
volt
    - volt status
      # USAGE: Display fresh/unknown host readings and GRID automatic pacing.
    # EXAMPLE: volt status

relay
    - relay inbox
      # USAGE: List saved local notifications and human acknowledgement state.
    - relay inspect <notification-reference>
      # USAGE: Read a full notification and its optional explicit speech proposal.
    - relay deliveries
      # USAGE: Inspect delivery attempt outcomes; Delivered does not mean read/heard.
    - relay ack <notification-reference>
      # USAGE: Master Control explicitly acknowledges a notification.
    # EXAMPLE: relay inbox

tasks
    - tasks propose VOLT volt.status current [key]
      # USAGE: Propose a saved host-resource briefing.
    - tasks propose VOLT volt.throttle <normal|eco|auto> [key]
      # USAGE: Propose a change to GRID automatic pacing only.
    - tasks propose RELAY relay.notify "message" [key]
      # USAGE: Propose local inbox delivery; no external messaging or sound.
    - tasks propose RELAY relay.brief <full-task-guid> [key]
      # USAGE: Propose delivery of a terminal task's actual recorded result.
    - tasks propose RELAY relay.speak <full-notification-guid> [key]
      # USAGE: Explicitly propose audio playback; inspect exact text before approval.
    # EXAMPLE: tasks propose RELAY relay.notify "GRID test notification"
```

MCP example: `mcp request "VOLT: status current; RELAY: notify Phase 5.6 test"`. Approve/dispatch each child individually. Inspect results using `volt status`, `relay inbox`, `relay deliveries`, and `mcp brief`. Local speech is separate from conversation voice mute preferences and occurs only on an approved relay.speak task. See [full acceptance test](Project_GRID_VOLT_RELAY_Implementation.md).

## Multi-agent meetings — 6.2A

```text
agent
    - agent meetings MCP MAXIS JN
      # USAGE: Open a shared meeting window with 2–6 registered agents after grid -start.
    - agent meetings MCP MAXIS JN -inline
      # USAGE: Keep the meeting in the original host for reviewed voice input.
    # EXAMPLE: agent meetings MCP MAXIS JN

talk
    - talk <message>
      # USAGE: Address the group; one participant responds according to specialty keywords.
    - talk @MAXIS <message>
      # USAGE: Direct this turn to MAXIS, who must belong to the meeting.
    # EXAMPLE: talk @JN What should I consider before archiving files?
```

`transcript --details` shows evidence and routing details. `exit` detaches; `disconnect` ends the meeting. Private conversations, social memories and captured files are not automatically shared. Meetings do not execute tasks.

## Persistent private memory — 6.2B

Connect to an agent as Master Control. Use a new, empty conversation for `memory scope`; the default project is `general`. Ordinary chat is not saved as permanent memory automatically.

```text
memory
    - memory list
      # USAGE: Review the current agent/project's entries, timestamps and sources.
    - memory scope <project>
      # USAGE: Select a project before the conversation's first message.
    - memory save <preference|decision|fact|unfinished> <key> <text>
      # USAGE: Save explicit user-confirmed context for later conversations/restarts.
    - memory correct <key> <text>
      # USAGE: Correct or reconfirm an existing entry; exclude older scoped history from recall.
    - memory forget <key>
      # USAGE: Block recall/reuse of this key; retain the original transcript for inspection.
    # EXAMPLE: memory save fact release Release codename is Moonstone.
```

Memories stay private to the agent/project and are not imported into meetings. Entries expire from recall after 30 days until reconfirmed. `memory list` remains available for review. Corrections/forgetting preserve source transcripts but suppress earlier scoped history from AI prompts. See [implementation and test](Project_GRID_Persistent_Conversation_Memory_Implementation.md).

## Automatic conversation recall

```text
memory
    - memory history
      # USAGE: Review recent eligible discussion from earlier conversations in this scope.
    - memory forget-history
      # USAGE: Exclude earlier scoped discussion from AI recall; retain transcripts and explicit memory entries.
    # EXAMPLE: memory history
```

Ordinary discussions are recalled automatically. `memory save` additionally preserves explicit confirmed entries. Recall is bounded to relevant excerpts and keeps private agent/project and exact meeting-group boundaries.

## Task assistance — 6.3

```text
assist
    - assist advice <request>
      # USAGE: Request recommendations without creating tasks or changing project files.
    - assist analyze <request>
      # USAGE: Analyze captures granted to the connected agent in the current project.
    - assist propose <request>
      # USAGE: Generate a validated typed draft, without creating tasks.
    - assist submit
      # USAGE: Submit the draft through MCP; all steps still await approval.
    - assist discard
      # USAGE: Drop the current draft.
    # EXAMPLE: assist propose Check current resource status using VOLT.
```

Start GRID and connect to an agent as Master Control. Use `memory scope <project>` in an empty conversation to match imported context. Import with an explicit grant, for example `context add -search persistent grid MCP`. Use the original terminal for `tasks inspect`, `tasks approve`, and `tasks run`. Analysis responses are transient; captured file content is not copied into automatic conversation memory.

## MCP workflow and meeting shortcuts (6.11)

`mcp plan <request>` drafts custom work; answer clarification with `talk <answer>`. `talk submit plan` creates tasks awaiting human approval. After inspecting and approving each task, `mcp run <plan-id>` runs the selected plan and briefs results. `mcp cancel <plan-id>` cancels outstanding work; `mcp repeat <plan-id>` creates a new plan needing fresh approvals. Use `mcp authority` and `mcp capabilities` for scope.

Start meetings with `meetings MCP MAXIS JN`, `agent meetings MCP MAXIS JN`, or `connect meetings MCP MAXIS JN`. Add `-inline` to stay here; otherwise a window opens after validation. Disconnect first if already connected.

## Model registry commands (6.17 foundation)

Master Control, original host terminal:

- `ai registry list` — stored revisions and pending candidates.
- `ai registry device` — memory/disk/GPU discovery, with unknowns explicit.
- `ai registry register-baseline` — verify and register the existing pinned model.
- `ai registry verify` — hash all referenced active/retained assets and measure shared storage.
- `ai registry import "absolute-manifest.json"` — validate and register already provisioned assets.
- `ai registry rollback <id>` — verify and restore the previous metadata revision.
- `ai registry force-lite on` / `off` — persist the setting; restart GRID to apply it.

Example: `ai registry register-baseline`, then `ai registry verify`. Registry changes do not switch the running chat model.
