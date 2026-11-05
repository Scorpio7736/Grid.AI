# Project GRID — Agent Model Profiles and Base Budget (6.13)

**6.17 registry foundation — 2026-11-04.** Versioned model manifests, checksum verification, retained-revision rollback, shared-asset accounting, candidate tracking, persistent force-Lite and device diagnostics are implemented. 1,109 solution tests and live baseline inventory verification passed. Exact specialist artifacts, device benchmarks and automatic profile promotion remain pending. Running chat remains on the pinned Qwen3 baseline. [Implementation and commands](Project_GRID_Model_Registry_Implementation.md).

**6.16 text-provider implementation — 2026-11-04.** Grid.AI now provides common lifecycle, generation, streaming, timeout, cancellation and capability contracts. Existing Ollama chat uses the new adapter. A pinned optional llama.cpp CPU provider passed live checks using the existing Qwen weights. Current suites total 1,090 passing tests. Actual image-provider lifecycle/resource verification remains open, alongside model registry/profile acceptance and other Phase 7 gates. [Implementation and verification](Project_GRID_Model_Abstraction_Implementation.md).

**Model-handling planning update — 2026-11-04.** The user supplied seven model candidates and model-handling rules. Preserve the implemented Qwen3 4B benchmark as the baseline, then evaluate exact artifacts for Lite/Standard/Performance profiles. The planned installed-model allocation is capped at 75 GB decimal within the 128 GB total Flash/PC target. Hardware discovery thresholds require measured fit and latency acceptance. [Model selection and handling plan](Project_GRID_Model_Selection_and_Handling_Plan.md).

**Planning update — 2026-11-04.** Qwen3 4B and the completed 6.13 role benchmarks remain the reference baseline while the user researches specialist brains. The user’s planned storage target is now 128 GB decimal for both Flash and PC installations. Implement edition-consistent accounting, update reserve and migration of current 20 GB enforcement in 6.27. Registry/license/resource/quality checks precede model promotion. All platform work in 6.16–6.31 gates Phase 7. [Full plan and acceptance gates](Project_GRID_AI_and_Agent_Platform_Plan.md).

Implemented 2026-10-29. This completes role profiles, shared assets, measured base enforcement and normal/CPU reduced-memory verification. Phase 7 still requires 6.14–6.15, including real second-device acceptance and offline extraction assets.

## Model selection and role fit

All six agents use one installed **Qwen3 4B Q4_K_M** snapshot with distinct immutable role guidance. No six-fold weight duplication and no model download/cloud substitution occur. Only the already provisioned candidate was selected; this is not a claim that it outperforms every available model. Candidate configurations were the ordinary accelerator mode and the CPU-only reduced-memory fallback. Small controlled role checks supplement the existing grounded conversation, task-planning and file-draft acceptance suites; they are not a comprehensive model-quality benchmark.

| Profile | Role guidance | Context | Initial / retry output |
|---|---|---:|---:|
| MCP | Complete planning, clarification, approval vs. execution | 4,096 | 512 / 1,536 |
| MAXIS | Correct code, debugging and honest test claims | 4,096 | 512 / 1,536 |
| JN | Document/storage provenance and verified duplicates | 4,096 | 512 / 1,536 |
| SENTINEL | Evidence-based security and explicit uncertainty | 4,096 | 512 / 1,536 |
| VOLT | Capacity, utilization, headroom and unknown telemetry | 4,096 | 512 / 1,536 |
| RELAY | Faithful concise summaries; draft vs. delivery | 4,096 | 512 / 1,024 |

Model manifest SHA256: `31D9857B5705086D352D4D6DB04E86C5D0F6A2B5D59A4DC7CE5CDB271CCF9141`. Main weight SHA256: `3E4CB14174460404E7A233E531675303B2FBF7749C02F91864FE311AB6344E4F` (2,497,280,480 bytes). Runtime snapshot used: Ollama 0.40.9. Model metadata reports Q4_K_M and 4.0B. The manifest hash pins the tested snapshot even if the upstream tag changes. Startup checks the manifest and required blob sizes; packaging/audit checks all referenced blob hashes. A changed manifest needs deliberate rebenchmarking/code approval, not a silent tag update.

Conversation replies and each meeting participant select their own profile. MCP coordinates structured proposals; MAXIS handles reviewed file drafts; one-to-one analysis and screen OCR use the addressed agent. An immutable per-request role passes through one bounded FIFO (8 waiting requests), one active inference and one loaded model. Separate clients cannot overwrite one another's role. Role guidance grants no host permissions.

## CPU and reduced-memory fallback

`GRID_AI_CPU_ONLY=1` forces CPU requests (`num_gpu=0`). `GRID_AI_LOW_MEMORY=1`, or reported .NET memory availability below 8,000,000,000 bytes, selects reduced memory and CPU. Set these before starting GRID. Normal chat keeps 4,096 context tokens. Reviewed file drafting reduces context from 16,384 to 8,192 and output from 4,096/8,192 to 2,048/4,096; inputs over 12,000 characters are refused before inference, without truncation. One loaded model is retained for at most one idle minute. Missing assets, insufficient resources or rejected output leave deterministic commands available with a visible failure; no model is fetched and no remote service substituted. This still needs approximately the model's measured RAM allocation plus runtime/OS/application memory: forced reduced-memory tests on this PC do not certify operation on every 4 GB or 8 GB machine. GPU-driver failure recovery and supported second-device hardware remain 6.14 acceptance work.

## Storage accounting and enforcement

The decimal base ceiling is **20,000,000,000 bytes**. `ai profiles` shows model/role/context/fallback information; `ai storage` measures the portable installation and shows base, mutable data, in-package staging/old versions, total, headroom, full-replacement staging requirement and drive free space. Development checkouts are explicitly not presented as complete release packages.

Base includes app/source/code, runtimes, model blobs/configuration, libraries, documentation/manifests and any future extraction dependencies. Unknown files count as base. Mutable categories are `data/`, `runtime/home/`, `runtime/temp/` and the runtime log. Explicit `staging/`, `old-versions/` and top-level `.staging-` folders are exposed separately. External sibling staging folders and unrelated installations are outside this root's total; account for them through drive free space. A replacement needs another base-sized allocation plus 256 MB reserve. No automatic deletion is performed. Existing JN managed-data governance, retention and reviewed cleanup remain separate from the base ceiling; mutable saves/recovery data is not silently counted as free base headroom.

Build-GridPortable includes a source snapshot and refuses completion if the measured package exceeds the ceiling or pinned shared models fail verification. Provisioning copies unique model digests once and refuses a changed model snapshot or oversized prepared runtime. Copy/transfer preflight budgets its actual inventory plus metadata headroom, requires free space for the full copied inventory plus reserve, and measures staging again before publishing. Startup refuses bundled AI from an oversized package. Symbolic links/junctions are refused in package audits to prevent an apparent small package depending on outside files. This does not complete the full source-install transfer manifest repair planned in 6.14.

Use `Grid.Terminal.exe --audit-package "absolute-package-root"` for a full size/hash audit and `Grid.Terminal.exe --verify-model-profiles` for the opt-in isolated role benchmark. Neither opens the user's world. The latter starts an owned local runtime twice and saves results under `%TEMP%/Grid613Verification-<id>/`.

## Completion verification

- Release solution: 1,044 regression tests (Core 14, Simulation 157, Persistence 99, Terminal 774), covering profile isolation in the shared queue, all six role bindings, CPU/reduced file limits, exact decimal cap boundaries, asset/source/data accounting and rejection of unbenchmarked manifests.
- **14/14 live role checks passed**, seven in each mode. Cases cover MCP ordered delegation, MAXIS addition repair, JN unverified duplicate claims, SENTINEL unknown security state, VOLT headroom conversion at two values, and RELAY summary/delivery accuracy. CPU mode also requires one loaded model and zero reported GPU allocation.
- The first benchmark failed VOLT's 75%-headroom calculation in both modes. Its role guidance now states the general conversion formula; the original and a second 40%-available case both pass. This failure is retained as evidence that generated reasoning still needs checks.
- Live report: `Grid613Verification-03773268b30949e48c3b2e22bd8c14c5/report.json`; first failed candidate: `Grid613Verification-fc23030a5c994a39869c07ea90dee5d9/report.json`.

| Agent / case | Mode | Milliseconds | Model allocation bytes | GPU bytes | Result |
|---|---|---:|---:|---:|---|
| MCP | standard | 2463 | 3180247121 | 3180247121 | Pass |
| MAXIS | standard | 195 | 3180247121 | 3180247121 | Pass |
| JN | standard | 193 | 3180247121 | 3180247121 | Pass |
| SENTINEL | standard | 187 | 3180247121 | 3180247121 | Pass |
| VOLT | standard | 225 | 3180247121 | 3180247121 | Pass |
| VOLT | standard | 172 | 3180247121 | 3180247121 | Pass |
| RELAY | standard | 263 | 3180247121 | 3180247121 | Pass |
| MCP | cpu-reduced-memory | 4556 | 3186108660 | 0 | Pass |
| MAXIS | cpu-reduced-memory | 2037 | 3186108660 | 0 | Pass |
| JN | cpu-reduced-memory | 2025 | 3186108660 | 0 | Pass |
| SENTINEL | cpu-reduced-memory | 1821 | 3186108660 | 0 | Pass |
| VOLT | cpu-reduced-memory | 2143 | 3186108660 | 0 | Pass |
| VOLT | cpu-reduced-memory | 834 | 3186108660 | 0 | Pass |
| RELAY | cpu-reduced-memory | 2373 | 3186108660 | 0 | Pass |

Latency is one local sample per case, including model loading on the first case; no throughput or minimum-hardware guarantee is implied. Package measurement is recorded after the final app/docs/source refresh below.

## Final package audit

The refreshed Windows x64 package, including its source snapshot, passed the full model hash audit: **9,901,889,423 base bytes** (about 9.902 GB), **1,153,572 mutable bytes**, zero in-package staging/old-version bytes, and **9,903,042,995 total bytes** across 16,340 files. Base headroom was 10,098,110,577 bytes. This is a point-in-time measurement; documentation refreshes and runtime logs change it. `ai storage` shows current values. Audit log: `%TEMP%/grid613-package-audit.txt`.

The final profile-enabled build also passed all six 6.12 invitation/planning/report/removal/restart checks (`Grid612Verification-07eddc76009e44d89e16358f9cf249d9/report.json`). The 6.12 portable refresh interrupted before this milestone is now included.
