# Model registry and lifecycle — 6.17

**6.17A baseline evaluation — 2026-11-04.** Qwen3 4B passed automatic/CPU text smoke and lifecycle checks. Versioned evidence is tied to exact model/runtime/device/test settings; `ai registry acceptance` shows recorded results. 1,126 regression tests passed. Specialist models are not yet selected/downloaded; broad model, image, device, network-blocked and later-milestone acceptance remains open. [Results and remaining gates](Project_GRID_Model_Acceptance_Baseline.md).

**Registry foundation implemented and verified — 2026-11-04.** Exact specialist artifacts, device-specific benchmarks and automatic profile promotion remain open. This section does not declare all of 6.17 or 6.17A complete.

## Implemented ownership and persistence

`Grid.AI/ModelRegistry.cs` owns schema-versioned local model metadata. Each manifest records an immutable model ID/revision, source, license, quantization, tokenizer/template identity, output kind, declared RAM/VRAM requirements, minimum profile, exact runtime compatibility tuples and relative asset paths with sizes/SHA256 hashes. Declared compatibility and resources are not proof of model quality, device fit or license redistribution approval.

Registry metadata lives at `runtime/model-registry.json` in a portable package, or `.grid-runtime/model-registry.json` in a checkout. Paths are relative to that runtime root, so copying the runtime preserves references. Registry metadata/lock files are mutable package state, copied with the runtime but excluded from the immutable portable asset seal. Model weights remain in the seal. Model files remain shared; there are no copies per agent. The existing six agent profiles still use the pinned Qwen3 4B baseline.

Updates take an exclusive cross-process file lease, freeze supplied metadata, validate the full next inventory and hash assets before replacing metadata through a same-directory atomic rename. Metadata is bounded to 4 MiB, 128 revisions, 512 assets per manifest and 16 runtime declarations per manifest. No download, deletion, provider execution or model switching occurs during registration. The registry is trusted host configuration, not model-generated permission or execution authority.

Revisions are immutable. New revisions retain their predecessor. Rollback verifies the previous revision before changing the active metadata reference, including when the newer version has become corrupt. A full inventory audit still reports corrupt retained assets. Missing rollback assets leave the active reference unchanged. Cancellation or failed verification does not publish the new state. Corrupt registry JSON is reported rather than silently reset.

Asset paths reject traversal, absolute paths, Windows device names, alternate streams, registry-control files and reparse links. Metadata inspection is not a fresh integrity check. `ResolveActiveAsync` checks exact provider/version/OS/architecture/backend compatibility and verifies the selected revision's assets before returning it. Future routing must call this boundary and recheck device headroom; metadata alone cannot authorize a load.

## Storage and candidates

Verification counts each referenced path once across all retained revisions and models; different physical paths count separately even if their content matches. Conflicting hashes or sizes for one shared path are rejected. The registry audit covers referenced model assets, not all caches, runtimes or unregistered files. Use package accounting separately. The 75 GB model / 128 GB total enforcement migration remains in 6.27; this milestone does not change the historical package ceiling.

The existing Qwen3 4B manifest is pinned by its previously approved SHA256 and imports seven asset references, including the Ollama manifest, configuration, weights, template, parameters, license and nested manifest. The GGUF's embedded tokenizer is covered by the weights hash. The registry baseline uses Q4_K_M and the local bundled license. Its 8 GB RAM requirement is a conservative declared policy threshold, not a newly measured minimum or a low-RAM certification. CPU compatibility metadata names the tested Ollama 0.40.9 and llama.cpp b12110 configurations. GPU/runtime/model combinations still require explicit acceptance.

`ModelCandidates` tracks Qwen3 4B/8B, Qwen2.5-Coder 3B/7B, Qwen3-Coder 30B-A3B, SDXL base 1.0 and FLUX.2 Klein 4B. Unselected candidates have explicit pending status. No hashes, revisions, licensing conclusions, installed states or benchmark results are invented for them. The user is still selecting specialist artifacts.

## Device profiles and force-Lite

`ai registry device` reports OS/process architecture, total/available physical RAM, runtime-volume free disk and bundled engine-file presence. On Windows, an optional system NVIDIA driver utility supplies per-adapter total/free VRAM in bounded output with a three-second timeout. It is not installed or downloaded by GRID. Missing/unsupported GPU collectors produce unknown values; GPU memory is never pooled across adapters. Engine-file presence and NVIDIA detection do not establish a usable inference backend.

`ModelProfileEligibility` accepts manifests already verified and benchmarked by a trusted caller. It filters by matching runtime, available RAM/VRAM and profile ceilings. It returns unavailable when no eligible text model remains. Force-Lite excludes higher-tier models; it does not relabel them as Lite. Unknown GPU headroom cannot enable Standard/Performance. A 32 GB / 12 GB discovery case does not enable image generation or FLUX. Synthetic tests cover the supplied no-GPU 16 GB and 32 GB / 8 GB scenarios.

The persistent force-Lite switch is loaded before the bundled AI process/backend starts. **Restart GRID after changing it.** It applies the existing CPU-only/reduced-context mode. Clearing it restores normal policy evaluation and does not override other low-memory/CPU environment settings. Unreadable registry settings cause a reported conservative force-Lite startup, preserving the registry for repair.

Automatic device-benchmark evidence storage, full AMD/Intel/backend discovery, dynamic model routing and automatic profile promotion are not wired into chat yet. The current baseline remains active. These gaps remain visible in 6.17/6.17A and depend on the evaluation/routing work in 6.19/6.26.

## Terminal commands

Use the original host terminal as Master Control. Registry work is not available from an attached conversation terminal.

```text
ai registry list
ai registry device
ai registry register-baseline
ai registry verify
ai registry force-lite on
ai registry force-lite off
ai registry import "C:\path\model-manifest.json"
ai registry rollback <model-id>
```

Start with `register-baseline`, then `verify`. Hash verification reads the complete referenced files and can take longer on flash storage. Import expects an existing local JSON `ModelManifest`, at most 1 MiB, with all referenced assets already provisioned under the runtime root. It performs no downloads. Registration, import and rollback update metadata; they do not switch a running chat model. Baseline-only registration has no predecessor to roll back to.

The manifest JSON uses the public record's property names and numeric enums: schema version 1, output kind 0 for text/1 for image, and minimum profile 1 for Lite/2 for Standard/3 for Performance. Runtime declarations specify `Provider`, `Version`, `OS`, `Architecture`, `Backend`; asset declarations specify `Path`, `Bytes`, `Sha256`, `Purpose`. Review exact artifacts and complete dependencies before importing. Specialist provisioning/evaluation belongs to 6.17A.

## Verification and remaining gates

On 2026-11-04 the current solution suites passed **1,109 tests**: Core 14, Simulation 157, Persistence 99 and Terminal 839. Nineteen registry/device tests cover shared storage, multiple registrations, corrupt and missing assets, failed updates, immutable revisions, rollback recovery, cancellation, unsafe paths, runtime mismatch, persisted settings, corrupt metadata, profile filtering and GPU measurement parsing.

The read-only `--verify-model-registry` entry point verified the real pinned baseline: **7 files, 2,497,295,812 bytes**. It also captured this PC's device readings without altering registry state or the saved world. [Recorded live result](verification/6.17-model-registry.txt). File contents and device measurements are snapshots, not a continuing guarantee.

Still open before Phase 7: exact specialist revisions and full dependency/license manifests, measured model/device acceptance, automatic eligible-profile integration, unsupported GPU/backend collectors, real multi-model device tests, image lifecycle acceptance and the physical second-device offline test. Synthetic models in regression tests establish registry correctness, not acceptance of unprovisioned candidate weights.
