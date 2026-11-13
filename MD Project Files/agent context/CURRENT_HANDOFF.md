# GRID current handoff

Updated 2026-11-13. Verify the working tree and checklist before starting work.

## Working approach

Follow the concise [repository working rules](../../AGENTS.md). Use targeted reads and tests, retain completion evidence, and link detailed documentation instead of repeating it. The [historical chat workflow](CHAT_WORKFLOW_AND_AGENT_HANDOFF.md) preserves earlier decisions.

## Current milestone

6.29 CPU synthetic reference passed real LoRA training, repeatability, checkpoint resume, adapter reload, merged export and baseline rollback. The user authorized a small test model and synthetic data. All 1,286 solution tests passed. [Fine-tuning workflow and remaining production gates](../Project_GRID_Model_Fine_Tuning.md). The isolated environment and cached offline wheels are under `.grid-runtime/training`, with exact versions locked in `TestSupport/Training/requirements-cpu.lock.txt`.

6.28 offline training data tooling is implemented. Verification passed 1,273 solution tests, 19 focused dataset cases and the real executable's synthetic build/verify entry points. See [Training Data Pipeline](../Project_GRID_Training_Data_Pipeline.md) for commands, provenance, permission review, exclusions and contamination limits.

6.27 portable deployment evidence and its remaining acceptance are in [Offline Portable Deployment](../Project_GRID_Offline_Portable_Deployment.md).

The package is at `portable/GRID7736-6.27-20261112`. The complete footprint target is 128 GB decimal, with a 75 GB model/dependency/cache sub-limit and 256 MB reserve.

## Next work and open gates

- The next sequential item is 6.30, Continuous quality and release management, in the [master development checklist](../Project_GRID_Master_Development_Checklist.md). Production model/corpus selection, QLoRA/GPU, agent-runtime import and portable-training acceptance remain open.
- Second-PC offline/changed-drive acceptance, selected specialist/image assets and repeat-build comparison remain open. Specialist models have not yet been selected.
- Keep remaining Phase 7 prerequisites visible, including conversation-first routing acceptance under 6.31.
- The working tree contains existing implementation and documentation changes. Inspect and preserve them.

## Suggested next-chat request

Continue GRID with checklist item 6.30. Read AGENTS.md and the current handoff, verify current repository state, implement the item, and update its completion evidence. Keep output concise.
