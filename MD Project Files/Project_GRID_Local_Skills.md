# Local declarative skills

Completed 2026-11-19 as the bounded declarative implementation of 6.35. Original-host Master Control owns installation, configuration, enablement and grants. A registered system agent may manually invoke only an enabled exact revision explicitly granted to its role while GRID agents are running. Attached, conversational, remote and background execution is not enabled. No cloud, model inference or executable scripts are involved.

## Manifest and review

Save this example to an absolute local JSON path:

```json
{
  "Version": 2,
  "Id": "local-health",
  "Revision": "1",
  "Origin": "Local operator",
  "License": "Private",
  "Description": "Show world and observed host resource status",
  "Steps": ["world.status", "resources.status"],
  "AllowedAgents": ["VOLT"],
  "Dependencies": ["grid.world", "grid.resources"],
  "RequiresNetwork": false
}
```

Run `skills review "C:\path\health.json"`. Review the declared origin/license, description, fixed actions and source SHA-256. Origin is an unverified publisher claim. `skills install <review-id>` installs those exact bytes disabled; review expires after two minutes and is invalidated by logout or identity change. Editing the source requires fresh review. Updates retain one previous revision and disable the skill.

`skills show local-health` displays the canonical catalog hash and exact `skills enable <id> <hash>` command. `skills dependencies local-health` shows each dependency and whether it is available. `skills configure <id> output-limit <512..8000> <hash>` sets the only current per-skill option without running it. Then `skills run local-health` executes manually.

Master Control may use `skills grant <id> <agent-role> <hash>` only for a role listed by the v2 manifest, and `skills revoke <id> <agent-role>` removes it. A switched registered agent sees only enabled skills granted to its role and may use `list`, `show`, `dependencies` and `run`; it cannot manage the catalog. Pausing GRID immediately refuses agent skill use. Disable, update, rollback, device/world/owner rebinding and removal revoke grants. Version 1 manifests remain supported as Master-Control-only skills.

`skills disable <id>` immediately removes execution permission and grants; `skills rollback <id>` restores the previous revision disabled; `skills remove <id>` removes both stored revisions. `skills list` discovers authorized entries; `skills history` shows Master Control the last 100 content-free attempt/outcome receipts. A started attempt without completion is uncertain and never automatically replayed.

## Scope, persistence and limits

Versions 1 and 2 support only the typed actions `world.status` and `resources.status`. They read existing host status through fixed mappings; they are not arbitrary terminal command strings. Each invocation carries a typed skill ID, revision, actor role, action and output limit. Resource observations retain their source/timestamp and may be stale.

Version 2 must declare every action dependency. `grid.world` and `grid.resources` are built-in and offline. A manifest may declare `network`, but the dependency is visibly unavailable and the skill cannot be enabled or run; a future connector must provide a separately reviewed adapter rather than silently widening this interpreter. Imported descriptions, origins and licenses are untrusted data, never model or host instructions.

Unknown properties, versions, actions, roles, dependencies, path/script arguments and control characters are rejected. Imports are at most 16 KB, the catalog at most 64 skills, each invocation at most four fixed reads and 512–8,000 characters per action. There is no filesystem-content, model, direct-network, process, shell or arbitrary-argument capability to grant. This is a denial-based capability interpreter for untrusted declarative manifests, not an OS sandbox for arbitrary executable plugins; executable plugins remain unsupported and would require a separate security design.

The world-adjacent `<world>.skills.json` catalog is replaced atomically after flushing a unique temporary file. Corrupt catalogs fail closed and are retained for diagnosis. Enabled state and agent grants are bound to machine, catalog path, world and owner credentials; changed bindings return every skill disabled and ungranted. Catalog hashes detect accidental changes, not a malicious local administrator rewriting both content and hashes. The original host's world lease serializes normal catalog ownership.

Export by retaining the JSON manifests, then explicitly review/import on the destination. Uninstall with `skills remove`; its receipts remain bounded and content-free. World retirement moves database/backup only, retaining the catalog alongside the blocked source until the operator decides what to retain. No automatic erasure is offered.

## Completion evidence and remaining boundaries

Thirteen focused tests and the complete `Grid.AI.slnx` suite pass at **1,523 passed, 0 failed, 0 skipped**. Coverage includes v1 compatibility, v2 dependency validation, offline refusal, bounded configuration, exact hash review, eligible/ineligible agent roles, pause denial, prompt-injection text, install/update/rollback/revoke/remove, restart, changed source/device/owner bindings, incompatible/unknown/malicious manifests, corruption, bounded receipts, failure without replay, original-host isolation and fixed real command adapters.

Physical PC2/removable-device acceptance remains a separate overall portability gate; automated moved-binding tests prove authority is removed, not that every device has been physically exercised. Sections 6.36–6.38 remain open: connectors, durable scheduling, combined activity/preferences/permission profiles and representative-user acceptance were not implemented by 6.35.
