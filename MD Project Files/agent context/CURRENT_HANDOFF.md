# GRID current handoff

Updated 2026-11-16. Verify the working tree and checklist before starting work.

## Working approach

Follow the concise [repository working rules](../../AGENTS.md). Use targeted reads and tests, retain completion evidence, and link detailed documentation instead of repeating it. The [historical chat workflow](CHAT_WORKFLOW_AND_AGENT_HANDOFF.md) preserves earlier decisions.

## Current milestone

Flash-drive AI cold-start repair 2026-11-16: the reported portable assets/runtime were ready, but first generation ended at 122.7 seconds, matching the former two-minute deadline while weights loaded from D:. Verified portable packages now get one bounded five-minute allowance through shared inference, Ollama and attached transport until first success, then return to two minutes. Installed/source timing is unchanged. 105 focused and 1,443 full-suite tests passed; four privilege skips remain. [Evidence/limits](../Project_GRID_Offline_Portable_Deployment.md#flash-drive-ai-cold-start-repair--2026-11-16). Fresh physical transfer/retest remains open; the reported flash package has the old executable.

Flash-drive move cleanup repair 2026-11-15: read-only inspection of D:/GRID7736 status/receipt confirmed duplicate retained Test-GridPortable.ps1 entries caused self-locking before deletion. Cleanup now holds one reader per canonical source and verifies every destination copy. 39 focused transfer tests and 1,440 full-suite tests passed; four privilege skips remain. [Evidence/recovery limits](../Project_GRID_Transfer_Implementation.md#duplicate-source-cleanup-repair--2026-11-15). Existing flash package remains unchanged with old executable; do not blindly retry its receipt after source rebuilds. Fixed-helper physical flash acceptance remains open. Changes remain on Updates_Local.

Future production branch ideas are now recorded as unchecked, deferred items in the [main requirements reference](../Project_GRID_Main_Branch_Requirements.md#future-requirements-deferred). No additional branch gates or GitHub protections were activated. Documentation/link review and `git diff --check` passed.

Initial `main` requirements agreed 2026-11-15: full candidate suite must pass with zero skipped tests; installation must remain reproducible through tracked setup and pinned verified dependencies. [Policy and enforcement status](../Project_GRID_Main_Branch_Requirements.md). Added to AGENTS.md and master checklist; other proposed branch rules are deferred. GitHub branch protection/status checks are not configured. Existing four-skip development results do not satisfy this production gate. Documentation-only verification: link/content review and `git diff --check`.

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

The reported demonstration/workspace mismatch and invalid main-terminal resume window are fixed and covered by the exact user sequence. Last tests used isolated outputs because the running GRID locked its normal executable; close and rebuild before normal-launch retesting. See the integration reference.

6.31 local coding follow-up verified: repository inspection/search, root-to-target instruction hashes, recorded-failure diagnosis and selected-file repair, source CRUD, saved file recovery and attached meeting plans. Real bundled Qwen repaired a failing disposable Git project without being told the fix; tests and unrelated user file stayed unchanged. [Coding evidence and limits](../Project_GRID_Local_Coding_Integration.md).

## Next work and open gates

- User direction 2026-11-14: defer copy/transfer and second-PC acceptance until the transfer device is ready. Do not treat missing device evidence as passed or block unrelated local milestones on it.

- The user confirmed on 2026-11-13 that an offline local agentic coding workflow is mandatory. MCP/MAXIS must actually edit repository files, run builds/tests, fix failures and report verified results through ordinary conversation. The master checklist now includes a disposable-checkout end-to-end acceptance gate before Phase 7. A reviewed disposable-project cycle is demonstrated; broader autonomous repair, Git-checkout/instruction handling, resume and disconnected-device gates remain open.

- The 6.33 local implementation is complete; retain its manual window/physical-media acceptance item. Next sequential local implementation is 6.34 guided onboarding and hardware-aware setup. Carry outstanding 6.31 acceptance from the integration reference: general autonomous coding/resume, full meeting tool workflows, live release-model adoption and failing-instance/second-device evidence. The previous 14 terminal failures are resolved. Carry forward production model/corpus selection, QLoRA/GPU and portable-training acceptance. Specialist selection and external-device evidence require user input.
- PC2 evidence received: required bundled AI assets are missing and inference is disabled; placeholder port 1 explains ConnectionRefused. Four tests failed creating symlinks without Windows privilege. [PC2 diagnosis and retained output](../Project_GRID_PC2_Diagnostics.md) records fixes and complete-package/provisioning steps. Successful offline PC2 retest remains open. Specialist/image assets and live catalog-to-agent adoption remain unselected/unverified.
- Keep remaining Phase 7 prerequisites visible, including conversation-first routing acceptance under 6.31 and the newer terminal-first milestones 6.32-6.38. New GUI work in 6.39 is optional and non-blocking.
- The working tree contains existing implementation and documentation changes. Inspect and preserve them.

## Suggested next-chat request

Continue GRID with checklist item 6.34 while retaining the open 6.33 real-device acceptance checks. Copy/transfer and PC2 acceptance are deferred until the user has the transfer device ready. Read AGENTS.md and the current handoff, verify current repository state, implement the item, and update its completion evidence. Keep output concise and preserve outstanding model/device acceptance gates.

MCP testing follow-up: five of six executable workflow checks passed; missing-workspace clarification returned Invalid clarification response. Interactive UI and Desktop HTML delivery remain unverified. See the integration acceptance reference, MCP executable testing follow-up.
