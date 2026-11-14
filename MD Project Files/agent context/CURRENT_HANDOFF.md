# GRID current handoff

Updated 2026-11-13. Verify the working tree and checklist before starting work.

## Working approach

Follow the concise [repository working rules](../../AGENTS.md). Use targeted reads and tests, retain completion evidence, and link detailed documentation instead of repeating it. The [historical chat workflow](CHAT_WORKFLOW_AND_AGENT_HANDOFF.md) preserves earlier decisions.

## Current milestone

6.31 contract-definition task completed 2026-11-13. Explicit project references, stable in-process surfaces, state ownership and adapter/test boundaries are recorded in [Integration contracts](../Project_GRID_Integration_Contracts.md). Seven new compiled-layer checks and all 42 focused tests passed. The full current-tree run had 1,297 passes and 14 terminal failures associated with the five existing conversation-routing edits; those edits were preserved. The full 6.31 gate remains open.

6.30 development release gates are implemented. Eighteen focused tests, 1,304 solution tests and `--verify-releases` passed. Promotion uses hash-bound evidence, fixed criteria, baseline comparison and verified rollback. Production model/device suites remain open. [Release management](../Project_GRID_Release_Management.md).

6.29 CPU synthetic reference passed real LoRA training, repeatability, checkpoint resume, adapter reload, merged export and baseline rollback. The user authorized a small test model and synthetic data. All 1,286 solution tests passed. [Fine-tuning workflow and remaining production gates](../Project_GRID_Model_Fine_Tuning.md). The isolated environment and cached offline wheels are under `.grid-runtime/training`, with exact versions locked in `TestSupport/Training/requirements-cpu.lock.txt`.

6.28 offline training data tooling is implemented. Verification passed 1,273 solution tests, 19 focused dataset cases and the real executable's synthetic build/verify entry points. See [Training Data Pipeline](../Project_GRID_Training_Data_Pipeline.md) for commands, provenance, permission review, exclusions and contamination limits.

6.27 portable deployment evidence and its remaining acceptance are in [Offline Portable Deployment](../Project_GRID_Offline_Portable_Deployment.md).

The package is at `portable/GRID7736-6.27-20261112`. The complete footprint target is 128 GB decimal, with a 75 GB model/dependency/cache sub-limit and 256 MB reserve.

## Next work and open gates

- Continue 6.31 with authoritative-owner migration and conversation workflow verification; its first contract-definition task is complete. Resolve the 14 current terminal test failures before claiming integration acceptance. See the [master development checklist](../Project_GRID_Master_Development_Checklist.md). Carry forward 6.30 production suites/device acceptance, production model/corpus selection, QLoRA/GPU, agent-runtime import and portable-training acceptance. Development release catalog selection still requires host integration before live agent adoption.
- Second-PC offline/changed-drive acceptance, selected specialist/image assets and repeat-build comparison remain open. Specialist models have not yet been selected.
- Keep remaining Phase 7 prerequisites visible, including conversation-first routing acceptance under 6.31 and the newer terminal-first milestones 6.32-6.38. New GUI work in 6.39 is optional and non-blocking.
- The working tree contains existing implementation and documentation changes. Inspect and preserve them.

## Suggested next-chat request

Continue GRID with checklist item 6.31. Read AGENTS.md and the current handoff, verify current repository state, implement the item, and update its completion evidence. Keep output concise and preserve outstanding model/device acceptance gates.
