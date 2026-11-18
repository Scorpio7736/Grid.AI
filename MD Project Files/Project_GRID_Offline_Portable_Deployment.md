# Grid.AI offline and portable deployment — 6.27

## Implemented storage and packaging baseline — 2026-11-12

Flash and PC packages now share a 128,000,000,000-byte managed-installation target. The measured footprint includes application/source files, runtime assets, saved data, logs, caches, staging and retained versions. The existing 256,000,000-byte reserve is included when checking usable headroom. Completed packages must fit within 127,744,000,000 bytes of occupied space. Destination free-space checks independently require the incoming footprint plus reserve.

The installed-model allowance is 75,000,000,000 bytes inside the managed target. Accounting conservatively includes all packaged `runtime` content except `runtime/dotnet`, including inference libraries, checkpoints, model dependencies, caches and runtime metadata. Copies under staging and retained-version folders count again because they occupy additional space. The application/source/.NET portions count toward the installation target. Future model installers must place their complete managed assets under this runtime layout and enforce the same inventory before publication.

`PackageBudget.Measure`, portable validation, transfer preflight, final transfer validation, packaged runtime startup and the storage diagnostic share this accounting. Transfer preflight includes mutable data and staging, plus 65,536 bytes for generated metadata. Final inventory validates the actual copied files. Ordinary ongoing writes continue through existing save/file services, with package audits and subsequent startup/transfer checks detecting growth. Continuous per-write package quota enforcement remains a future integration concern.

The old 20 GB package constant migrates through the updated executable. Existing package markers and saves remain readable. JN's persisted quota proposals are a separate audited user-governance mechanism, with their existing approval history retained. A saved JN quota can therefore still display 20 GB independently of the new package ceiling.

## Build and integrity workflow

`Prepare-GridRuntime.ps1` and `Build-GridPortable.ps1` apply the new runtime/model budget checks. Builds use self-contained Windows x64 publish with deterministic/continuous-integration compilation. The immutable asset inventory is sorted by relative path and records lengths and SHA256 hashes. The package includes the existing runtime notices, license files, source snapshot, local .NET/SDK, local Ollama/Qwen assets, SQLite and PDF extraction dependencies.

The builder uses a uniquely named sibling staging folder and publishes with a same-volume directory rename only after sealing and validation. A failed build leaves a marked staging directory for inspection. Existing destination folders are refused. Rollback consists of retaining the previous complete package and its saved-world backup until the replacement passes acceptance. Optional specialist/image installation transactions remain pending their selected artifacts and license review.

Reproducibility is scoped to deterministic compilation and a checksummed inventory for the supplied source/runtime inputs. Runtime provisioning metadata, selected SDK versions and mutable user data can differ between packages. A complete byte-identical two-build comparison remains pending release acceptance.

## Verification and remaining acceptance

All 1,254 solution tests passed on 2026-11-12, comprising Core 14, Simulation 157, Persistence 99 and Terminal 984. Eleven added/updated boundary cases cover the revised ceiling, legacy 20 GB boundary, total mutable/staged growth, the model sub-limit, retained model versions, .NET accounting and reserve-sensitive free space for Flash and PC.

```powershell
dotnet test Grid.AI.slnx --no-restore --verbosity minimal
.\Build-GridPortable.ps1 -Destination "C:\path\to\new-package"
.\Test-GridPortable.ps1 -PackageRoot "C:\path\to\new-package" -AssistanceOnly
```

- [ ] Run the resulting package on a second Windows x64 PC with networking disconnected, a different drive letter and empty global model caches.
- [ ] Record CPU fallback, changed-device handling, update/rollback and saved-world continuity on that device.
- [ ] Package and license-check the selected specialist/image dependencies, then verify atomic optional installation and rollback.
- [ ] Record repeat-build comparison and the complete supported-device storage/resource matrix before Phase 7.

The isolated local test removes global program/model lookup paths. Physical second-device and network-blocked evidence must be recorded separately. Existing runtime configuration uses local model assets and loopback inference with explicit provisioning for installs.

### Built package

The sealed package is at `portable/GRID7736-6.27-20261112`. Its measured footprint before the isolated runtime test was 10,048,689,662 bytes in 16,670 files. AI runtime/model dependencies occupied 8,597,526,860 bytes. Both budgets passed. The immutable inventory SHA256 is `54FE01F41C55110487C46005A49466D4703CF5E7C940F929D7B73A65DB4DEC63`.

The package is a fresh installation containing the source/runtime snapshot captured by the build. Start `launch.cmd` to create a world, or use the existing transfer commands to package a selected saved world.

### Isolated local runtime test passed

`Test-GridPortable.ps1 -AssistanceOnly` returned exit code 0 for the new package. The child used a package-local temporary profile, a system-only PATH and an empty global model-cache location. The bundled runtime returned Grid.AI LOCAL READY, answered a greeting and advice request, retrieved the synthetic captured release code Cobalt42 and produced a typed VOLT task draft. The test opened no saved world. This verifies the local package boundary on this PC. Second-device and disconnected-network acceptance remain open.
