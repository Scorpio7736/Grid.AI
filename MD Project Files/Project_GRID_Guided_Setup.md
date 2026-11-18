# Guided setup and health diagnostics (6.34)

Grid.AI guided setup: initial local implementation, 2026-11-14. Full milestone acceptance remains open.

## Use

- `Grid.Terminal.exe --setup [absolute-new-profile.json]`: review PC versus Flash deployment, an existing local world-directory choice, hardware estimates and off/Lite inference. Enter `skip` to cancel; only `save` writes a new preference file. Existing profiles are never overwritten. No world is opened or moved.
- `Grid.Terminal.exe --configured <profile.json>`: explicitly launch the selected world and inference preferences. No other launch flags are accepted. Existing local identity/login remains required. Profiles contain no secrets or grants.
- `Grid.Terminal.exe --doctor`: read-only hardware, managed-asset and capability guidance. It verifies pinned model hashes when Lite resources are sufficient; it does not start inference, record audio, capture screens, change permissions or repair files.
- Rollback: launch normally for the original default world, or select an earlier profile. To edit preferences, create a new profile. This does not restore/delete world data.

PC profiles require an absolute local database path with an existing parent; network paths, alternate streams and reparse ancestors are refused. Flash profiles require an existing portable package and use package-relative `data/world.db`. A different machine requires setup again; no grants move with a profile.

Lite uses the pinned installed Qwen3 4B model with reduced context and CPU requests, requiring measured Windows x64 compatibility, 8 GB available RAM and 256 MB free reserve. These are conservative eligibility estimates, not measured performance or full installation-space estimates. Standard and Performance remain unavailable pending model/device acceptance. GPU details use the existing bounded optional driver collector and may be unknown.

Missing assets require the existing explicit `Prepare-GridRuntime.ps1` provisioning step with selected local inputs, or a complete offline package. Setup never downloads, falls back to global Ollama, or uses cloud inference. Model hashes do not authenticate arbitrary runtime binaries. Existing disable environment settings remain authoritative.

## Verification and remaining work

12 setup regressions cover resource thresholds, unknown memory, unsupported hosts, Flash relocation, changed devices, missing package/assets, profile round trip, overwrite refusal, ambiguous paths and cancellation. Combined startup/window focused run: 24 passed. Full solution: 1,431 passed, zero failures/skips.

Remaining: integrated first-run identity/recovery management, direct reviewed installer integration, complete installation-space accounting, active voice/screen/storage repair diagnostics, and clean-user/read-only-media/network-blocked/second-PC physical acceptance. Current health output explicitly labels unprobed capabilities. No full 6.34 completion is claimed.

The local foundation for [three-challenge enrollment and verbal authentication](Project_GRID_Challenge_Code_Authentication.md) is implemented in interactive world credential setup: it follows username/password confirmation, saves all three entries atomically and keeps challenge responses outside profiles, transcripts and inference. The separate `--setup` deployment-profile flow still contains no secrets. Remote challenge sessions and voice verification remain planned.

Fresh-clone provisioning and world-isolation follow-up: [fresh clone installation](Project_GRID_Fresh_Clone_Installation.md). The explicit installer now covers dependency acquisition; complete online PC2 and broader hardware/permission acceptance remain open. Latest full suite: 1,437 passed.
