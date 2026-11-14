# GRID current handoff

Updated 2026-11-14. Verify the working tree and checklist before starting work.

## Working approach

Follow the concise [repository working rules](../../AGENTS.md). Use targeted reads and tests, retain completion evidence, and link detailed documentation instead of repeating it. The [historical chat workflow](CHAT_WORKFLOW_AND_AGENT_HANDOFF.md) preserves earlier decisions.

## Current milestone

6.31 integration and supported conversation workflows verified 2026-11-13. Latest full verification on 2026-11-14 passed all 1,374 solution tests. Grid.Agent owns migrated contracts with legacy type forwards and one engine state owner. Inline/attached input stays conversational. Reviewed plans and file operations support ordinary approval/execution phrases. A real bundled-model C# coding cycle passed failing-test -> AI source fix -> reviewed apply -> passing-test -> saved evidence. See [Integration acceptance](../Project_GRID_Integration_Acceptance.md) for evidence and limitations. Final Phase 7 acceptance remains open.

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

- The user confirmed on 2026-11-13 that an offline local agentic coding workflow is mandatory. MCP/MAXIS must actually edit repository files, run builds/tests, fix failures and report verified results through ordinary conversation. The master checklist now includes a disposable-checkout end-to-end acceptance gate before Phase 7. A reviewed disposable-project cycle is demonstrated; broader autonomous repair, Git-checkout/instruction handling, resume and disconnected-device gates remain open.

- Continue outstanding 6.31 acceptance from the integration reference: general autonomous coding/resume, full meeting tool workflows, live release-model adoption and failing-instance/second-device evidence. The previous 14 terminal failures are resolved. Carry forward production model/corpus selection, QLoRA/GPU and portable-training acceptance. Specialist selection and external-device evidence require user input.
- User confirmed the second PC still fails with an HTTP error; exact status/log/path have not been provided. HTTP diagnostics now expose safe status/category and next steps. Retest a new copied package and collect ai status, ai test and runtime/ai-runtime.log. Do not claim the failure fixed. Specialist/image assets and live catalog-to-agent adoption remain unselected/unverified.
- Keep remaining Phase 7 prerequisites visible, including conversation-first routing acceptance under 6.31 and the newer terminal-first milestones 6.32-6.38. New GUI work in 6.39 is optional and non-blocking.
- The working tree contains existing implementation and documentation changes. Inspect and preserve them.

## Suggested next-chat request

Continue GRID with checklist item 6.31. Read AGENTS.md and the current handoff, verify current repository state, implement the item, and update its completion evidence. Keep output concise and preserve outstanding model/device acceptance gates.
