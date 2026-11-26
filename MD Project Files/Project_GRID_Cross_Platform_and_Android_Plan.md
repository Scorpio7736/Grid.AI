# Grid.AI cross-platform and Android plan

## Status

**Deferred future feature — 2026-11-25.** Previously planned as the next active phase on 2026-11-17; that priority has been superseded by the [core-features-first roadmap](Project_GRID_Core_Features_First_Roadmap.md). The full requirements below are retained **without closing their checkboxes or implying current Linux/Android support**.

Grid.AI will introduce a dedicated **Grid.Platform** compatibility layer instead of spreading Windows, Linux or Android checks through the simulation, intelligence and terminal code. Android is the first priority because it has a distinct application lifecycle, permission system and storage model; it must not be treated as an ordinary Linux distribution. Linux desktop/server support follows the same shared contracts.

The former City, Integration, and desktop GUI plans are preserved in the master checklist. Grid.Platform/Android/USB work is now **Future Plan D**, not the next required milestone. A Windows-first GUI may begin when shared agent/service contracts are ready; it does **not** depend on completing Grid.Platform first.

## Architecture boundary

```text
Grid.Terminal / Grid.AI / Grid.Agent
        |
        v
Grid.Platform abstractions
        |
        +-- Grid.Platform.Windows
        +-- Grid.Platform.Linux
        +-- Grid.Platform.Android
                    |
                    +-- Grid.Host.Android

Grid.Core / Grid.Simulation / Grid.Persistence
remain shared and platform-neutral
```

`Grid.Platform` selects capabilities and adapters. It does not emulate Windows. Shared layers may request a typed capability, but only an adapter may use native paths, process APIs, removable-storage discovery, shell commands, device telemetry or app lifecycle services.

Planned projects:

```text
Grid.Platform/
  Abstractions/
  Detection/
  FileSystem/
  Processes/
  Capabilities/
Grid.Platform.Windows/
Grid.Platform.Linux/
Grid.Platform.Android/
Grid.Host.Android/
```

## Required adapter contracts

| Capability | Contract responsibility | Priority |
|---|---|---|
| Platform detection | OS family, version, architecture, application host and capability inventory | Critical |
| File system | Native paths/content handles, permissions, atomic writes, free space, case sensitivity and safe traversal | Critical |
| Runtime | Locate or stage a compatible Grid.AI/.NET host and report unsupported combinations | Critical |
| Removable storage | Discover/select removable media without guessing identity or silently changing grants | Critical |
| Process/application discovery | Enumerate only what the host permits; report unsupported visibility honestly | High |
| Command execution | Typed operations mapped to PowerShell, direct process launch, Bash or Android host services; never concatenate an arbitrary shell command | High |
| AI backend | Select a compatible offline inference provider/model or degrade explicitly when the device cannot run one | High |
| Resources | CPU, memory, storage, thermal/battery and concurrency observations with units, freshness and unknown states | High |
| Dependency/health checks | Explain missing runtime, permissions, model, storage or host features with platform-specific repair guidance | High |
| Lifecycle | Startup, foreground/background, suspend/resume, shutdown, save coordination and cancellation | Critical |

Platform capability results must distinguish `Available`, `PermissionRequired`, `TemporarilyUnavailable`, `Unsupported` and `Unknown`. Unsupported features must not be simulated or reported as successful.

## Android host requirements

Android support uses a dedicated application host rather than launching a `linux-arm64` build in the Android environment.

- Use the Android system folder/document picker and persist the user-granted directory handle for the selected Grid.AI data folder. Do not depend on direct `/storage` traversal or Termux permissions.
- Treat Android document/content identifiers as native handles; do not force them into fake Windows/Linux paths inside shared code.
- Keep executable application files in the app's permitted private storage when Android prevents execution from USB/removable media. Persistent world data, approved documents, configuration and compatible model assets may remain in the selected USB folder when the provider supports required access.
- Revalidate the persisted folder grant at every launch. If the drive, grant or document provider is unavailable, open read-only/recovery guidance or fail closed without creating a replacement world.
- Respect foreground/background lifecycle limits. Save before suspension when possible, recover through the existing durable save rules, and never promise an always-running background simulation without a separately justified Android service design.
- Request only capabilities needed by an explicit feature. Folder access, microphone, notifications, network and screen/media projection remain separate grants.
- Support Android ARM64 first. Record API-level, ABI, storage-provider and hardware requirements from measured builds rather than assuming all Android devices are compatible.
- Provide touch-friendly login, terminal/chat input, approval, cancellation, file selection and diagnostics. A native host shell may wrap the existing terminal experience without duplicating simulation state.

## Portable USB layout

The prepared drive contains separate platform builds and one governed data area; it is not a universal executable:

```text
Grid.AI/
  bin/
    windows-x64/
    linux-x64/
    linux-arm64/
    android-arm64/
  data/
    worlds/
    memory/
    configuration/
  models/
  manifests/
  grid.ps1
  grid.sh
```

Android's `bin/android-arm64` entry represents host/package assets, not an executable launched directly from the drive. The Android app may stage verified executable/runtime assets privately while continuing to use the selected removable data root.

Only one authoritative host may open a world for writing. Cross-platform opening requires a package/world manifest, schema compatibility check, backup, lock/lease, atomic save probe and post-load identity check. Device-bound grants, registered workspaces, remote pairings and OS credentials must be reauthorized on the destination device. World identity, city, conversations and allowed portable credentials are reused; hardware discovery must not silently regenerate the world.

## AI and resource portability

- Keep provider selection behind existing Grid.AI model abstractions; do not make Ollama a universal requirement.
- Define measured backend choices for Windows, Linux and Android, including CPU/GPU/NPU availability, supported model formats and licensing.
- Select the smallest validated model/profile that satisfies the task and current device limits.
- Provide a phone-safe degraded mode for simulation, terminal, saved memory and deterministic tools when local generation cannot run.
- Never download a model or dependency during ordinary offline runtime. Installation/provisioning remains explicit.
- Account for model copies, staging space, memory pressure, thermal throttling and battery state before inference.

## Phase order

1. Inventory and prohibit new native dependencies in `Grid.Core`, `Grid.Simulation` and `Grid.Persistence`.
2. Define Grid.Platform contracts, capability states and fake adapters for deterministic tests.
3. Move current Windows behavior behind `Grid.Platform.Windows` without intentional user-facing regression.
4. Add Linux x64/ARM64 path, process, runtime, removable-storage and resource adapters.
5. Build the Android ARM64 host and Storage Access Framework-backed folder adapter.
6. Make persistence, identity, file/context operations and USB relocation pass cross-platform compatibility tests.
7. Add platform-aware offline AI backend selection and explicit degraded operation.
8. Publish separate signed/versioned artifacts plus one portable manifest/data contract.
9. Run real-device acceptance on Windows, Linux and at least one supported Android ARM64 device.

## Acceptance gates

- Shared projects contain no new direct Windows/Linux/Android native calls; automated architecture tests enforce the boundary.
- Existing Windows behavior passes after adapter extraction.
- Linux x64 and ARM64 builds start, authenticate, load/save a disposable world and run supported offline terminal workflows.
- Android installs through a normal app package, lets the user select the Grid.AI folder on removable storage, survives restart/regrant, and never relies on unrestricted `/storage` access.
- A world created on one supported platform opens on another without regeneration, concurrent-write corruption or silent loss of portable state.
- Device-bound permissions and paths are visibly revoked/reviewed after transfer.
- Missing USB storage, revoked folder grants, read-only media, low space, unsupported AI, background suspension and interrupted staging fail safely with actionable diagnostics.
- Separate builds share a versioned manifest, schema and world contract; incompatible versions refuse before writes.
- Network/DNS-blocked runtime tests cover login, save/restart, terminal/chat fallback, context, deterministic tasks and available local AI.
- Actual device/OS/API/backend combinations are recorded in a support matrix. “All devices” means all combinations that pass this matrix, not every device in existence.

## Explicit non-goals for this phase

- Emulating Windows on Linux or Android.
- Treating Android as generic Linux.
- Running executables directly from USB where the host OS forbids it.
- Concurrently writing the same world from multiple devices.
- Claiming every Android phone can run the full desktop AI/model profile.
- Beginning a second platform-specific state owner or unreviewed migration while delivering future platform support. **A Windows-first GUI can be developed before this deferred gate** as long as it shares the authoritative host contracts.
