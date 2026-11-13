# GRID current handoff

Updated 2026-11-12. Verify the working tree and checklist before starting work.

## Working approach

Follow the concise [repository working rules](../../AGENTS.md). Use targeted reads and tests, retain completion evidence, and link detailed documentation instead of repeating it. The [historical chat workflow](CHAT_WORKFLOW_AND_AGENT_HANDOFF.md) preserves earlier decisions.

## Current milestone

6.27 portable deployment baseline is implemented locally. The recorded verification is 1,254 passing tests and a self-contained package passing isolated local AI/advice/context/task-draft checks. Full evidence and remaining acceptance are in [Offline Portable Deployment](../Project_GRID_Offline_Portable_Deployment.md).

The package is at `portable/GRID7736-6.27-20261112`. The complete footprint target is 128 GB decimal, with a 75 GB model/dependency/cache sub-limit and 256 MB reserve.

## Next work and open gates

- The next sequential item is 6.28, Training data pipeline, in the [master development checklist](../Project_GRID_Master_Development_Checklist.md). Read its acceptance requirements before implementation.
- Second-PC offline/changed-drive acceptance, selected specialist/image assets and repeat-build comparison remain open. Specialist models have not yet been selected.
- Keep remaining Phase 7 prerequisites visible, including conversation-first routing acceptance under 6.31.
- The working tree contains existing implementation and documentation changes. Inspect and preserve them.

## Suggested next-chat request

Continue GRID with checklist item 6.28. Read AGENTS.md and the current handoff, verify current repository state, implement the item, and update its completion evidence. Keep output concise.
