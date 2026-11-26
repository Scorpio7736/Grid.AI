# Grid.AI current handoff

## 2026-11-25 — active roadmap decision (supersedes platform-first order)

User directed development toward **core functionality before transferability**. Preserve implemented Windows transfer/package code and tests, but place actual copy/move portability completion, second-PC/USB acceptance, Grid.Platform, Linux and Android in the future backlog. Immediate work is Windows-first: repair measured local model selection and low-RAM startup, simplify fresh-host installation, complete MCP/MAXIS useful end-to-end agent work with evidence and resumption, then polish terminal/agent UI and optionally begin a GUI without waiting for Android. Maintain truthful offline/local-model claims, permissions, backups, and platform-friendly contracts. See [new authoritative roadmap](../Project_GRID_Core_Features_First_Roadmap.md) and updated [master checklist](../Project_GRID_Master_Development_Checklist.md). Documentation change only; no tests or cross-device acceptance have been run by this decision.

## 2026-11-22 — bound host permission plans

The typed system operation that generated MCP's permission question is retained on the pending intent. Natural Once/Session/Always consent executes that exact validated object; GRID does not ask AI to recreate the plan after approval. The consent schema is request-kind validated and no longer falls back to file-preview wording. A safe injected adapter passed the full one-minute clock request/consent/receipt flow without touching the real clock. All 1,654 solution tests passed; actual Windows UAC/clock mutation remains manual. See [dynamic access permissions](../Project_GRID_Dynamic_Access_Permissions.md).

## 2026-11-21 — conversation window closure

Closing a dedicated conversation or meeting window now ends its saved host conversation, retains the transcript and clears pending per-window authorization/intent state. Host cleanup covers `/close`, title-bar/process close and pipe loss; a new `connect` starts cleanly and ended conversations cannot resume. Ordinary attached terminals/data views still only detach. All 36 window cases and all 1,623 solution tests passed. See [terminal conversations](../Project_GRID_Terminal_Conversations.md).

## 2026-11-21 — dynamic access permissions

MCP host requests now follow outcome → route → access → permission → execution → conversational result. Natural permission questions/results are model-generated; the host validates broad access paths and typed operations. `host.system.read` supplies date/Bluetooth evidence. High-risk `host.system.write` supports bounded relative, exact-date and current-month-day Windows clock changes with GRID consent and UAC. Extra natural whitespace is normalized; refusals and missing-information questions cannot create permission state. Schema v28 stores Normal/Medium/High Once/Session/Always grants. Exact file/task approval remains separate. Real Qwen read and non-executing natural calendar-date planning acceptance passed; actual clock mutation remains manual. All 1,622 solution tests passed. See [dynamic access permissions](../Project_GRID_Dynamic_Access_Permissions.md).

## Latest verified batch — 2026-11-19

Completed 6.35 [local declarative skills](../Project_GRID_Local_Skills.md) on the existing 6.34/onboarding foundation. V2 manifests declare typed dependencies and eligible agent roles; Master Control owns exact-byte review, lifecycle, bounded configuration and exact-revision grants. Registered agents can manually run only granted fixed read-only actions while GRID is running. Direct networking, model prompts, scripts/processes, general files and arbitrary arguments remain unavailable.

Full solution verification: **1,523 passed, 0 failed, 0 skipped** (Core 14, Simulation 163, Persistence 107, Terminal 1,239). Thirteen focused skill cases pass. Historical results below belong to their earlier checkpoints; this is not new physical-device evidence.

Completed local foundations through 6.38. The new original-host `trust` command consolidates activity evidence, reviewed memory export, disabled immutable permission templates, individually reviewed grants/immediate revoke, disabled-first goals and on-demand deterministic suggestions. Trust profiles do not grant host access or approvals. Five focused and all 1,551 solution tests pass. Representative-user and physical second-device/offline acceptance remain open, so 6.38 and the pre-platform experience gate are not fully released. No running Grid service was restarted or local-model session switched.

- Release-blocking repair 2026-11-18: clean source starts now provision/repair the private local-AI runtime before opening a world; non-interactive missing-runtime launches fail closed instead of entering simulated chat. Setup and portable builds run real MCP verification. Current copyto/transferto refuses incomplete runtime sources, copies and independently hashes the complete private runtime/model/.NET inventory, and v2 package startup validates its sealed manifest before world load. User-visible branding, solution (`Grid.AI.slnx`), attached conversation headers, launch command and new root destinations are `Grid.AI`/`GridAI`; only legacy named-pipe recognition remains internal. All 1,453 tests passed. Source and fresh sealed `TestResults/Grid.AI-portable-20261118` runtimes passed real inline/attached MCP replies. Second-PC offline acceptance is still required.

Updated 2026-11-18 for the Grid.AI product, solution and runtime-preflight repair. Historical milestone evidence remains intact; the legacy attachment prefix is accepted internally for compatibility. Verify the working tree and checklist before starting work.

## Working approach

Follow the concise [repository working rules](../../AGENTS.md). Use targeted reads and tests, retain completion evidence, and link detailed documentation instead of repeating it. The [historical chat workflow](CHAT_WORKFLOW_AND_AGENT_HANDOFF.md) preserves earlier decisions.

## Current milestone

6.34 local onboarding implementation updated 2026-11-16: reviewed offline/online installer, retained-file inventory and staging admission checks, explicit save-to-secure-login launch, first-run offer, identity/world-recovery guidance and capability revoke help. `--doctor-check <profile.json>` performs bounded storage/voice-inventory/synthetic-OCR/device-metadata/local-inference checks. All five passed on this Windows host; 53 focused tests passed; full suite **1,456 passed, zero failures/skips** and `git diff --check` passed. See [guided setup](../Project_GRID_Guided_Setup.md). Physical clean-user/PC2/removable-media/disconnected acceptance and Standard/Performance promotion remain open; do not count simulated tests as physical evidence. Next local implementation item is 6.35 when requested; earlier milestone entries below are historical.

AI readiness alerts added 2026-11-15: immediate host startup and post-login warning plus inline/resume/meeting and attached opening notices retain cause and explain fallback-only operation. Actual disabled ephemeral terminal and attached-protocol regressions passed. Full suite 1,436 passed, zero failed, four symbolic-link privilege skips. See [alert evidence/limits](../Project_GRID_Fresh_Clone_Installation.md#ai-readiness-alerts-2026-11-15). This reports startup availability, not continuous backend monitoring.

PC2 fresh-clone online installation completed 2026-11-15: pinned runtime/SDK/model downloads verified, Release build clean, real inline and attached-protocol Qwen greetings passed, readiness published. Launch with Start-GridClone.ps1; existing terminals need restart. Explicit -Download -RepairRuntime retains an incomplete old runtime; startup guidance points to the online installer. Physical windows, first login and disconnected PC2 acceptance remain open. See [installation evidence](../Project_GRID_Fresh_Clone_Installation.md#pc2-online-installation-2026-11-15). Earlier development-PC evidence below is historical.

Current verification: full suite 1,434 passed, zero failed, four symbolic-link privilege skips; seven clone setup cases passed. `git diff --check` passed. No commit/push performed in this follow-up.

Fresh clone path implemented: Setup-GridClone.ps1 -Download installs pinned dependencies; Start-GridClone.ps1 uses .grid-data/world.db with no imported world. Final offline setup and real inline/attachment model chats passed. Full suite 1,437 passed. Complete online clean-PC2 acceptance remains open. Latest PC2 report confirms missing bundled runtime/model assets. See [fresh clone installation](../Project_GRID_Fresh_Clone_Installation.md). User authorized commit/push to Scorpio7736/Grid7736.

6.34 initial setup/profile/health implementation is in progress; see [guided setup](../Project_GRID_Guided_Setup.md) for remaining work. PC2 report took priority: ordinary chat now preserves startup failures; corrupt pinned-model validation degrades to explicit unavailable status. Real local Qwen greetings passed through inline and attached named-pipe dispatch on this development PC. Full suite: 1,431 passed, zero failures/skips. Current PC2 ai status and launch path requested; actual PC2 root cause/retest remains open. See [PC2 diagnostics](../Project_GRID_PC2_Diagnostics.md).

6.33 local device-broker baseline implemented 2026-11-14. Original-host metadata discovery and reviewed launch/focus/title-inspection/folder creation use live grants, exact target binding, one-attempt execution and saved audit claims. Existing file/screen controls remain authoritative. All 54 focused and 1,416 solution tests passed. Interactive window focus/protected-window and physical read-only-media acceptance remains open. See [device integration](../Project_GRID_Device_Integration.md). Rebuild/restart the ordinary terminal to use device commands; the running instance was not replaced.

6.32 conversation-first intent routing is implemented. See [intent routing](../Project_GRID_Conversation_Intent_Routing.md) for the bounded model contract, review/approval flow and read-only retry policy. Focused tests: 118 passed. Full solution: 1,393 passed, zero failed/skipped, using `TestResults/intent-routing`. Rebuild/restart the ordinary terminal before manually trying the changes; the running instance is not replaced.

6.31 integration and supported conversation workflows verified 2026-11-13. Latest full verification on 2026-11-14 passed all 1,378 solution tests. Grid.Agent owns migrated contracts with legacy type forwards and one engine state owner. Inline/attached input stays conversational. Reviewed plans and file operations support ordinary approval/execution phrases. A real bundled-model C# coding cycle passed failing-test -> AI source fix -> reviewed apply -> passing-test -> saved evidence. See [Integration acceptance](../Project_GRID_Integration_Acceptance.md) for evidence and limitations. Final Phase 7 acceptance remains open.

6.30 development release gates are implemented. Eighteen focused tests, 1,304 solution tests and `--verify-releases` passed. Promotion uses hash-bound evidence, fixed criteria, baseline comparison and verified rollback. Production model/device suites remain open. [Release management](../Project_GRID_Release_Management.md).

6.29 CPU synthetic reference passed real LoRA training, repeatability, checkpoint resume, adapter reload, merged export and baseline rollback. The user authorized a small test model and synthetic data. All 1,286 solution tests passed. [Fine-tuning workflow and remaining production gates](../Project_GRID_Model_Fine_Tuning.md). The isolated environment and cached offline wheels are under `.grid-runtime/training`, with exact versions locked in `TestSupport/Training/requirements-cpu.lock.txt`.

6.28 offline training data tooling is implemented. Verification passed 1,273 solution tests, 19 focused dataset cases and the real executable's synthetic build/verify entry points. See [Training Data Pipeline](../Project_GRID_Training_Data_Pipeline.md) for commands, provenance, permission review, exclusions and contamination limits.

6.27 portable deployment evidence and its remaining acceptance are in [Offline Portable Deployment](../Project_GRID_Offline_Portable_Deployment.md).

The package is at `portable/GRID7736-6.27-20261112`. The complete footprint target is 128 GB decimal, with a 75 GB model/dependency/cache sub-limit and 256 MB reserve.

Local 6.31 saved-world preservation acceptance passed 2026-11-14: six populated-world scenarios verify backup recovery, version refusal, failed migration rollback and fresh approval after restore. See the integration reference for fixture limitations and the matching-package rollback procedure.

Saved operator plan resumption is implemented: `resume my work` -> numbered plan review -> explicit approval/run. SQLite restart, partial completion, stale selection and private-meeting restrictions are covered. File-edit focus and general autonomous coding recovery remain open.

The reported demonstration/workspace mismatch and invalid main-terminal resume window are fixed and covered by the exact user sequence. Last tests used isolated outputs because the running Grid.AI locked its normal executable; close and rebuild before normal-launch retesting. See the integration reference.

6.31 local coding follow-up verified: repository inspection/search, root-to-target instruction hashes, recorded-failure diagnosis and selected-file repair, source CRUD, saved file recovery and attached meeting plans. Real bundled Qwen repaired a failing disposable Git project without being told the fix; tests and unrelated user file stayed unchanged. [Coding evidence and limits](../Project_GRID_Local_Coding_Integration.md).

## Next work and open gates

- Historical roadmap 2026-11-17 (superseded for priority on 2026-11-25): the next active phase was Grid.Platform cross-platform compatibility. Prioritize a dedicated Android ARM64 host with native system folder/document access, then Linux x64/ARM64 while preserving Windows behavior. Separate platform artifacts share a governed world/data manifest; Android is not generic Linux and may need private executable staging. Former City and Integration phases are now Backlog and Future Plans. No platform code is implemented by this planning update. See [cross-platform plan](../Project_GRID_Cross_Platform_and_Android_Plan.md) and the rebased [master checklist](../Project_GRID_Master_Development_Checklist.md#phase-7--cross-platform-compatibility-and-android-portability).

- User direction 2026-11-17: 6.34A enrollment, management and trusted-host challenge sessions are implemented. Schema v25 stores three independently salted 1..64-character challenge verifiers; schema v26 persists bounded increasing account/endpoint cooldowns. Master-Control-granted endpoints receive two-minute, two-random-prompt attempts and five-minute exact-purpose claims bound to user/host/world/endpoint/call. Replay, cross-call, forgery, expiry, rotation and restart fail closed; claims never log in or approve work. Full solution: 1,452 passed, zero failed/skipped. Protected client secret input, transcription suppression, actual remote call integration and voiceprints remain open. See [implementation and remaining plan](../Project_GRID_Challenge_Code_Authentication.md).

- User direction 2026-11-17: add a development-only hidden terminal command framework. Registry/non-discovery, locked-local, agent-self and original-host Master Control emergency boundaries are implemented. Master mutations use two-minute world/session/activity/target-bound plans and typed random nonces. Offline rescue and hidden nullification remain unavailable. Hidden names are not authorization; no fixed backdoor or logged-out destructive bypass exists. Existing visible `nge`/startup `--recover` behavior is unchanged. See [the break-glass command reference](../Project_GRID_Development_Break_Glass_Commands.md).

- User direction 2026-11-14: defer copy/transfer and second-PC acceptance until the transfer device is ready. Do not treat missing device evidence as passed or block unrelated local milestones on it.

- The user confirmed on 2026-11-13 that an offline local agentic coding workflow is mandatory. MCP/MAXIS must actually edit repository files, run builds/tests, fix failures and report verified results through ordinary conversation. The master checklist now includes a disposable-checkout end-to-end acceptance gate before Phase 7. A reviewed disposable-project cycle is demonstrated; broader autonomous repair, Git-checkout/instruction handling, resume and disconnected-device gates remain open.

- The 6.33 local implementation is complete; retain its manual window/physical-media acceptance item. Next sequential local implementation is 6.34 guided onboarding and hardware-aware setup. Carry outstanding 6.31 acceptance from the integration reference: general autonomous coding/resume, full meeting tool workflows, live release-model adoption and failing-instance/second-device evidence. The previous 14 terminal failures are resolved. Carry forward production model/corpus selection, QLoRA/GPU and portable-training acceptance. Specialist selection and external-device evidence require user input.
- PC2 evidence received: required bundled AI assets are missing and inference is disabled; placeholder port 1 explains ConnectionRefused. Four tests failed creating symlinks without Windows privilege. [PC2 diagnosis and retained output](../Project_GRID_PC2_Diagnostics.md) records fixes and complete-package/provisioning steps. Successful offline PC2 retest remains open. Specialist/image assets and live catalog-to-agent adoption remain unselected/unverified.
- Keep remaining Phase 7 prerequisites visible, including conversation-first routing acceptance under 6.31 and the newer terminal-first milestones 6.32-6.38. GUI work formerly numbered 6.39 is now Future Plan C so Grid.Platform comes first. Phase 7 now means Grid.Platform; Godot city work is deferred backlog.
- The working tree contains existing implementation and documentation changes. Inspect and preserve them.

## Suggested next-chat request

Continue Grid.AI on master with the **2026-11-25 core-first roadmap**. First repair existing RAM-aware automatic model selection and verify honest low-memory behavior on a supported Windows host; then complete simple Windows first-run setup and the P0 6.39 MCP/MAXIS workflow. Preserve existing transfer implementation and all unchecked second-PC/portable/Android/Linux gates as future work. Read AGENTS.md and current handoff, inspect current branch and tests, implement one verified feature increment, and update its evidence.

MCP testing follow-up: five of six executable workflow checks passed; missing-workspace clarification returned Invalid clarification response. Interactive UI and Desktop HTML delivery remain unverified. See the integration acceptance reference, MCP executable testing follow-up.

# 2026-11-18 — 6.34B break-glass safety foundation

- Added `DevelopmentBreakGlassRegistry` v1 as a non-executable, source-controlled inventory of visible recovery safeguards and reviewed hidden contracts.
- Reserved all `::grid-*` names away from ordinary command registration.
- Direct and connected-conversation reserved input now receives one generic denial before any AI/talk routing.
- Ordinary help and completion remain unchanged and expose no hidden names or counts.
- Four focused registry/non-discovery tests and the full solution passed at the foundation boundary (1,457 passed, 0 failed, 0 skipped).

## 2026-11-18 — 6.34B locked-local recovery boundary

- Added a separate original-console executor for locked `::grid-rescue` status, read-only world verification, activity freeze, two-minute process-local recovery preparation and cancel.
- Verification checks primary/backup integrity, schema and exact world identity without migration or writes; automated SHA-256 checks confirm both files remain unchanged.
- Recovery preparation cannot authenticate, reset credentials, approve work, expose private content, recover or nullify. Authenticated, attached, redirected, malformed and conversational routes fail closed.
- Eleven focused tests and the full solution passed at the locked-local checkpoint (1,464 passed, 0 failed, 0 skipped); the registered-agent boundary below supersedes that checkpoint as current status.

## 2026-11-18 — 6.34B registered-agent self boundary

- Added original-console `::grid-agent-break` self-status, bounded self-evidence, self-task abort and detach-to-locked controls.
- The engine independently binds access to an original-host registered-agent lease. Evidence contains only that agent's identity plus bounded task IDs/status/attempt counts; no arguments, results, context or other-agent records.
- Abort changes only tasks targeted to the active agent and saves when persistence is available. MAXIS/JN isolation is covered directly. Detach cannot activate Master Control.
- Seventeen focused tests and the full solution passed at the registered-agent checkpoint (1,470 passed, 0 failed, 0 skipped); the Master Control boundary below supersedes that checkpoint as current status.

## 2026-11-18 — 6.34B original-host Master Control boundary

- Added two-step plans for freeze, isolate, session/approval revocation, remote disable, safe mode, validated backup recovery and bounded evidence export.
- Plans expire in two minutes and bind session generation, world/instance, activity generation, action and exact target. Wrong, stale, replayed and changed-target commits fail closed.
- Recovery exits without final save, rechecks hashes/world identity, archives the displaced primary and requires restart. Evidence export is new-file-only and secret/body-free.
- A bounded content-free operator JSONL audit survives recovery and excludes nonces and target paths. Safe mode gates ordinary commands and closes attachments; no remote listener exists in this build.
- Twenty-seven focused tests and the full solution pass (1,480 passed, 0 failed, 0 skipped). Offline owner rescue is the next 6.34B item; hidden nullification remains unavailable.

## 2026-11-18 — 6.34B offline owner-rescue boundary

- Added explicit Master Control recovery-file enrollment/rotation. Each new file contains a random 256-bit world-bound secret; schema v27 stores only its key ID and salted verifier, never plaintext or the file path. There is no universal recovery key.
- Added a pre-start `--operator-rescue <absolute-world-path>` route before runtime/AI setup. It requires an interactive local console, exclusive stopped-world lease, current-schema integrity and the matching user-held file.
- Rescue exposes only verify-and-exit, local username/password reset and validated same-authority backup restoration. Mutations require exact world ID plus a fresh 128-bit nonce. No AI, conversations, agents, remote access, shell or general filesystem interface is constructed.
- Password reset preserves agents/transcripts/world data while clearing challenge credentials/cooldowns. Backup restore archives the displaced primary. Audit is bounded/content-free and excludes secrets and paths.
- Seven focused tests and the complete solution pass (1,487 passed, 0 failed, 0 skipped), including key rotation and cross-world refusal. The next 6.34B item is recoverable world retirement; hidden nullification/removal remains unavailable.
