# Grid.AI — Core-Features-First Roadmap

**Decision: 2026-11-25. Status: authoritative sequencing change; planning only.** The user has redirected development from transferability/cross-platform expansion toward completing Grid.AI's main local AI and agent capabilities first. Existing work is preserved. This decision **supersedes 2026-11-17 and 2026-11-21 sequencing statements wherever they imply portability is the next required phase**; it does not erase their implementation history or automatically close their checkboxes.

## Product target

Build a dependable, useful, local-first multi-agent assistant on **one supported Windows host** before optimizing moving that installation/world/models between devices. Core capability means practical, verified assistance, not only architectural scaffolding.

- **MCP:** understand ordinary-language goals, coordinate agents, require the correct approvals, maintain a durable plan, and accurately report status.
- **MAXIS:** inspect approved workspaces, propose source edits, build/test/diagnose, perform reviewed repair iterations and report real evidence.
- **TRON, JN, VOLT and RELAY:** deliver their supported security, storage, resource and communications functions with correct scope, truthful availability and useful workflows.
- **Shared experience:** contextual conversations, persistent scoped memory, live meetings, notifications, task progress, interruption/resumption and understandable error states.
- **Host-controlled safety:** deterministic policies, narrowly scoped permissions, explicit human authorization, audit evidence and one authoritative task/world owner.

## Current implementation versus open work

Existing code already includes the Windows terminal, Qwen3 4B local model baseline, agent registry and meetings, SQLite persistence/memory, typed approval/task systems, bounded coding assistance, transfer commands, verified package foundations, and portions of the 6.39 experience work. **Preserve all of them.** Neither a completed autonomous coding product nor cross-device portability may be claimed until the respective physical/end-to-end tests pass.

## Active implementation order

### P0 — Local inference and startup reliability

- [ ] Repair and integrate measured RAM/VRAM-aware model selection using existing discovery, eligibility and registry components; keep verified Qwen3 4B as the fallback.
- [ ] Resolve low-memory behavior on 16 GB Windows machines, diagnostics, recovery, and actual repeated conversations; do not silently choose unavailable models or cloud inference.
- [ ] Finish a user-friendly **single-host Windows** initial setup/repair workflow based on the existing pinned private runtime and hash verification. Multi-PC relocation is not a requirement for this installer.
- [ ] Measure cold/warm latency, quality, memory and resource use from genuine agent tasks. Carry forward 6.39A and the existing release-model gates.

### P0 — Agents that complete useful work

- [ ] Complete ordinary-language MCP planning, named-agent delegation, clarification, approval, cancellation, progress, outcomes and follow-up routing (6.31 / 6.39B–C).
- [ ] Complete MAXIS's bounded multi-step edit/build/test/diagnose/repair loop in authorized Git worktrees, with exact-content review, retry budgets, reproducible evidence and safe resume.
- [ ] Prove scoped memory and meeting collaboration across restarts and interruptions, without leaking another agent's context or repeating side effects.
- [ ] Expand specialist actions from observed use cases instead of inventing broad automation authority.

### P1 — Main user experience and agent customization

- [ ] Make the existing terminal conversations and meetings clear, responsive and usable, with live tracked task states, approvals, notifications, search/history and recovery.
- [ ] After core service contracts stabilize, begin an **optional GUI product shell** informed by the separate Grid.AI GUI demo: agent cards/meetings, workflow and task views, review center, voice/sound settings and activity/status. A GUI must consume the same host state, never create another authority or task engine. **The GUI no longer waits for cross-platform support.**
- [ ] Prototype configurable agent voices locally and offline using the existing voice foundation. The Voice Studio and Fish Audio alternative-engine decision remain exploratory, with no mandated paid cloud dependency.
- [ ] Build practical RELAY notifications, controlled scheduler, and selected G2/phone integrations behind verified, revocable active-host clients. Defer optional device features where they delay core tasks.

### P2 — Windows single-host product acceptance

- [ ] Validate realistic end-to-end local AI tasks, relevant full automated tests, genuine offline inference after provisioning, backup/recovery, access revocation, performance and first-user experience.
- [ ] Publish a **Windows-first working release** with honest known limitations. Do not require a flash drive, second PC, Android or Linux to call the core feature set accepted.
- [ ] Keep local-first operation, installed-model integrity, portable-data-friendly schema boundaries, and no silent host-specific credential reuse as architectural constraints.

## Future Plan — Transferability, deployment editions and cross-platform (deferred)

**Preserve everything already implemented. Do not delete, rewrite or regress existing copy/transfer commands, portable-runtime packaging, relative-path asset inventory, manifests, transfer dashboards, sealed verification, recovery and tests.** They remain supported to their **previously verified scope**, with open acceptance checks remaining unchecked.

When the core product is useful and verified, revisit:
1. Reliable Windows-to-Windows package/world/model migration and second-PC offline verification, including actual USB removal, low space, crashes, and destination relative paths.
2. Complete PC/Flash installation editions, self-contained package management and portable identity/permissions migration.
3. Grid.Platform adapters with real Windows, Linux x64/ARM64 and Android ARM64 acceptance, native Android document-tree and removable-media permissions, platform-appropriate runtime backends and no unsupported universal-compatibility claims.

Details and historical checklists remain in [transfer implementation](Project_GRID_Transfer_Implementation.md), [offline portable deployment](Project_GRID_Offline_Portable_Deployment.md), [cross-platform/Android architecture](Project_GRID_Cross_Platform_and_Android_Plan.md), the master checklist's retained Future Plan D and Future Plan B.6–B.9. None is marked completed solely by this roadmap revision.

### Minimal design guardrails while portability is deferred

Use stable, explicit interfaces for model providers, data storage, filesystem access and host actions; maintain schema migrations/backup compatibility; avoid scattering new absolute Windows paths; protect file/OS-grant boundaries; document platform-specific functionality. **Do not** construct Android/Linux implementations, a full Grid.Platform extraction, USB migration UI or comprehensive second-device release tests just to unblock core agent work.

## Exit criteria for returning to portability

Reconsider the transfer roadmap after a Windows user can: launch a prepared local runtime; hold useful MCP/agent conversations; approve and complete a realistic multi-file MAXIS task; inspect actual build/test results; resume safely after interruption; manage memory and permissions; and use the intended primary terminal or GUI workflow without developer coaching. Then decide portability targets based on demonstrated need and available hardware.

**Documentation rule:** dated prior status notes and already-passing tests are historical evidence, not deleted; unsatisfied real-device, offline transfer and broader feature acceptance items stay open.