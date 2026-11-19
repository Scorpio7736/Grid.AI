# Local declarative skills

Implemented 2026-11-18 as a bounded first increment of 6.35. Only the original authenticated Master Control terminal of a persistent world exposes `skills`. Agent, attached, conversational and remote execution is not enabled. No cloud, model inference, executable scripts or background work is involved.

## Manifest and review

Save this example to an absolute local JSON path:

```json
{
  "Version": 1,
  "Id": "local-health",
  "Revision": "1",
  "Origin": "Local operator",
  "License": "Private",
  "Description": "Show world and observed host resource status",
  "Steps": ["world.status", "resources.status"]
}
```

Run `skills review "C:\path\health.json"`. Review the declared origin/license, description, fixed actions and source SHA-256. Origin is an unverified publisher claim. `skills install <review-id>` installs those exact bytes disabled; review expires after two minutes and is invalidated by logout or identity change. Editing the source requires fresh review. Updates retain one previous revision and disable the skill.

`skills show local-health` displays the canonical catalog hash and exact `skills enable <id> <hash>` command. Then `skills run local-health` executes manually. `skills disable <id>` immediately removes execution permission; `skills rollback <id>` restores the previous revision disabled; `skills remove <id>` removes both stored revisions. `skills list` discovers installed entries; `skills history` shows the last 100 content-free attempt/outcome receipts. A started attempt without completion is uncertain, never automatically replayed.

## Scope, persistence and limits

Version 1 supports only the typed actions `world.status` and `resources.status`. They read existing host status through fixed dispatch mappings; resource observations retain their source/timestamp and may be stale. Imported descriptions are data, never prompts. Unknown properties, versions, actions, path/script arguments and control characters are rejected. Imports are at most 16 KB, the catalog at most 64 skills, each invocation at most four status reads with bounded output. There is no filesystem-content, network or process capability to grant.

The world-adjacent `<world>.skills.json` catalog is replaced atomically after flushing a unique temporary file. Corrupt catalogs fail closed and are retained for diagnosis. Enabled state is bound to machine, catalog path, world and owner credentials; changed bindings require review/enable again. Catalog hashes detect accidental changes, not a malicious local administrator rewriting both content and hashes. This is an authority-limited declarative interpreter, not a sandbox for arbitrary executable plugins. The original host's world lease serializes normal catalog ownership.

Export by retaining the JSON manifests, then explicitly review/import on the destination. Uninstall with `skills remove`; its receipts remain bounded and content-free. World retirement moves database/backup only, retaining the catalog alongside the blocked source until the operator decides what to retain. No automatic erasure is offered.

## Remaining roadmap

6.35 remains open for scoped agent invocation, configurable action parameters/dependency resolution, richer capabilities and their approvals, package portability acceptance, and any executable-plugin sandbox. Sections 6.36–6.38 remain open: connectors, durable scheduling, combined activity/preferences/permission profiles and representative-user acceptance were not implemented by this increment. Physical PC2/offline/removable-device acceptance remains separate.

Regression coverage includes install/update/rollback/revoke/remove, restart, changed source/device/owner bindings, incompatible/unknown/malicious manifests, corruption, bounded receipts, failure without replay, logout-invalidated reviews, exact-hash enablement, original-host isolation and real command dispatch.
