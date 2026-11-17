# GRID reusable skills and integration system — 6.35

Local implementation completed 2026-11-16. A GRID skill is a declarative, versioned recipe for one to eight existing specialist capabilities. It is not executable code. Community manifests cannot supply scripts, shell commands, model instructions, approval flags, device actions, credentials or new capabilities. Invoking an enabled skill creates an ordinary MCP plan whose exact child tasks remain AwaitingApproval until Master Control separately reviews, approves and runs them.

## Terminal workflow

Return to the main original Master Control terminal and start GRID. The catalog is stored beside the active world as `world.db.skills`, so it remains world-specific. Empty catalogs are not created merely by starting GRID.

```text
skills catalog
skills import "C:\path\skill.json"
skills install <review-id>
skills configure <skill-id> <declared-key> <value>
skills check <skill-id>
skills enable <skill-id>
skills invoke <skill-id>
skills disable <skill-id>
skills revoke <skill-id>
skills uninstall <skill-id>
skills rollback <skill-id>
```

`import` and `update` stage the same bounded review. GRID displays the manifest origin, semantic version, SHA256, license, offline status, permissions and declared configuration. The source must be an explicitly selected absolute local JSON file under 64 KiB, without links, junctions, network/device paths or alternate streams. Installation rereads the file and refuses it if the hash changed after review. Installation/update always starts disabled. Review IDs expire after five minutes and are one-use.

`configure` accepts only declared, printable non-secret values and disables the skill for fresh review. Manifests cannot request password, token, secret, credential or API-key configuration. `check` resolves configuration and verifies current workspace/storage dependencies without creating a task. `enable` repeats dependency checks. `invoke` rechecks all current dependencies and the existing task capability contract, then creates a new immutable plan. It does not approve or run anything. Existing task attempt, audit, cancellation, grant and saved-plan rules remain authoritative.

`disable` and `revoke` prevent new invocations but do not erase already-created task records or approvals; manage those through existing task controls. `uninstall` requires the skill to be disabled and retains the previous manifest for explicit rollback. Rollback restores a retained revision disabled, with configuration cleared for review. No rollback silently restores an approval.

## Manifest version 1

The exact fields are `manifestVersion`, `id`, `name`, `skillVersion`, `description`, `origin`, `license`, `networkRequired`, `permissions`, `configurationKeys` and `steps`. Unknown/duplicate fields and incompatible versions are refused. IDs and keys use bounded lowercase identifiers; versions use semantic `major.minor.patch` form. Permissions must exactly equal the distinct capability IDs used by the steps. Each step names MAXIS, SENTINEL, JN, VOLT or RELAY, one catalogued capability and one bounded argument. `${declared-key}` substitution is supported only in the argument; the resolved argument must pass the normal capability validator.

Version 1 skills do not invoke AI models directly. They may orchestrate deterministic existing GRID task adapters; model-powered skill steps need a future typed, reviewed capability. `networkRequired: true` may be installed for inspection but cannot be enabled or invoked. Network connectors are planned under 6.36. Offline-capable skills remain usable after provisioning when their underlying local workspace/storage grants and runtime dependencies are available.

## Security and resource boundary

The sandbox is the absence of a third-party code runtime: manifest text is parsed as untrusted data and never added to an AI prompt or executed. Skills cannot access arbitrary filesystem, process, network, clipboard, screen or device APIs. Only the existing capability allowlist runs, with its own target validation, time/resource limits, original-host checks and separate human task approval. The skills command requires original-host Master Control; attachments and agents cannot administer the catalog. Lifecycle audit events contain the skill ID/action/hash, not configuration values or source paths.

The world transfer inventory now includes `world.db.skills` when it exists. Skill configuration uses logical workspace/storage aliases rather than silently retargeting machine paths. After moving devices, re-register and review machine-bound scopes, run `skills check`, and enable again. Physical removable-device/second-PC acceptance remains deferred by the user; local relocation coverage does not certify that gate.

## Verification

Ten new regression cases cover strict manifest versions/fields, forged permission and agent expansion, executable fields, post-review tampering, credential requests, relative paths, offline network denial, reviewed install/configuration/enable/invoke, revoked workspace dependencies, attached-session denial, update/rollback/uninstall and catalog relocation. Invocation produces only AwaitingApproval tasks with zero attempts. The expanded skills/device/task boundary suite passed 56 tests. Full solution verification passed **1,466 tests, zero failures/skips** (14 Core, 157 Simulation, 99 Persistence, 1,196 Terminal) using isolated `TestResults/skills-acceptance` outputs. The first full run exposed eager creation of an empty catalog racing headless test cleanup; catalog creation is now lazy, and the two exact startup/persistence regressions plus the final full suite pass. NGE now removes the exact adjacent world skill directory after rejecting reparse content; focused NGE/startup persistence checks passed.

Known limits: no arbitrary plugin runtimes, parameter prompts, secret vault, direct model invocation, remote registry, network connector or unattended skill execution. Existing task records survive skill disable/uninstall by design and require their own cancellation. Phase 7 and physical portability acceptance remain open.
