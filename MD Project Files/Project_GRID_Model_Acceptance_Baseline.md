# Model selection and handling acceptance — 6.17A baseline

**Baseline evaluation implemented — 2026-11-04. The complete 6.17A gate remains open.** The user confirmed that specialist models have not yet been selected or downloaded. This work evaluates the installed Qwen3 4B reference model and provides scoped, reproducible evidence. It does not select or provision additional models.

## Implemented evaluation

`Grid.AI/ModelAcceptance.cs` defines versioned evidence and a deterministic evidence check. `Grid.Terminal/ModelAcceptanceVerification.cs` runs the baseline suite through the common local provider/session API. It verifies the pinned model inventory, hashes every bundled Ollama runtime file, and binds results to the manifest, runtime, evaluation/provider assemblies, .NET version, device/hardware fingerprint, OS, available NVIDIA driver file hash, execution mode, suite version and token budgets.

Evidence changes when these recorded identifiers change. Results are rejected if required cases are missing/duplicated, any case fails or truncates, any request exceeds the 120-second smoke limit, lifecycle checks are incomplete, resource measurements are unknown, CPU mode reports GPU allocation, or the evidence is future-dated/older than 30 days. This is a local text smoke-test decision, not tool authorization, model installation, specialist qualification or automatic profile promotion. Local JSON reports are not signed certificates and must remain trusted host evidence; model replies never create acceptance records.

Two sequential owned-runtime modes are exercised: automatic placement and CPU-only. Each has six cases:

- Model-cold structured response, including load time.
- Warm structured response.
- A small C# correction with an exact expected answer; no code executes.
- Reading a marker from supplied context.
- Selecting VOLT then RELAY for a simple observation/notification request; no tasks execute.
- Ignoring an instruction embedded inside a quoted document while reading its marker.

Each mode also checks streamed completion, time to first nonempty text, cancellation after streamed output, a successful subsequent request, and confirmed unload. There is one active evaluation session/model at a time. Run this standalone with normal GRID sessions closed for uncontended measurements; the verifier does not terminate unrelated processes. It never opens the saved world or changes registry active revisions.

The Ollama adapter now retains optional runtime-reported output token counts and generation duration. Missing/malformed/negative counters remain unknown. They do not change generated content, tool permissions or existing retries. Report throughput can be calculated as `OutputTokens / (GenerationNanoseconds / 1e9)` when both values are valid and duration is positive.

## Results and interpretation

The full solution passed **1,126 regression tests**: Core 14, Simulation 157, Persistence 99, Terminal 856. Seventeen new cases cover evidence invalidation, stale/future reports, correctness/completion/lifecycle gaps, network-isolation separation, changed runtime content/dependencies, mutable report storage and optional provider metrics.

Both live modes passed all six fixtures and lifecycle checks on this PC. Captured reports:

- [Automatic mode](verification/6.17A-qwen3-4b-automatic.json)
- [CPU-only mode](verification/6.17A-qwen3-4b-cpu.json)

The reports contain exact timings, allocations and token counters. They are snapshots. Cold means the model was unloaded from the owned runtime before loading; operating-system file caches were not flushed. First-text latency is measured on a warm JSON stream. Allocation values are the largest observations after requests, not continuously sampled process/GPU peaks. Latency thresholds are generous smoke-test timeouts, not product responsiveness targets.

**Network isolation is explicitly unverified.** The provider uses loopback and the bundled runtime disables cloud use, but this run did not block or audit all network connections. The policy rejects these reports when network-blocked evidence is required. CPU-only execution on this PC is not a physical 16 GB no-GPU device test. These small correctness fixtures do not establish broad coding ability, reliable delegation, document quality or comprehensive prompt-injection resistance.

## Commands and files

In the original host terminal as Master Control:

```text
ai registry acceptance
ai registry device
ai registry list
```

`acceptance` displays recorded results and age/configuration consistency within the report. It explicitly does not perform a fresh runtime/device comparison or promote models. To regenerate evidence, close GRID and run the built terminal with:

```powershell
.\Grid.Terminal\bin\Debug\net10.0\Grid.Terminal.exe --evaluate-model-baseline
```

For a prepared portable package, use `launch.cmd --evaluate-model-baseline` from the package directory. This optional developer verification can take several minutes and reads the full model/runtime files. It is not part of ordinary startup, performs no downloads and has a fifteen-minute overall cancellation budget.

Latest reports are saved to `.grid-runtime/model-evaluations/qwen3-4b-{automatic|cpu}.json` in a checkout, or `runtime/model-evaluations/` in a package. They are mutable package data, included in copies and excluded from the immutable executable/model seal. Report replacement uses a same-directory temporary file and rename. Failed/incomplete evidence cannot pass; no automatic fallback installation occurs.

## What remains before the complete 6.17A gate closes

| Requirement group | State and next evidence |
|---|---|
| Qwen3 4B reference | This PC's text smoke/lifecycle evaluation passed; broader task/quality/device/offline benchmarks remain |
| Qwen3 8B and coding specialists | Exact artifacts and complete manifests pending the user's selection; then provision and evaluate |
| SDXL and FLUX.2 Klein | Complete dependencies/runtime/licenses and actual image validity, dimensions, memory and latency tests pending |
| Device profiles | Eligibility/force-Lite foundation exists; physical 16 GB and 32 GB / 8 GB cases and automatic integration remain open |
| Observability | Cold/warm smoke timing and provider counters available; peak sampling, benchmark datasets and meaningful quality/latency thresholds remain in 6.19 |
| Routing/fallback | Accepted-model routing, current headroom rechecks, structured failures and bounded fallback remain in 6.25–6.26 |
| Delegation | Policy, scope, child limits and ledger acceptance remain in 6.22–6.24; this smoke suite executes no tools |
| Document/image artifacts | Deterministic generators, validation and visual QA remain in 6.23 and selected-image-runtime work |
| Budgets/provisioning | 75 GB model/128 GB total enforcement, staging reserve, complete optional installers and nearly-full destination tests remain in 6.27 and provisioning work |
| Offline/portability | Network-blocked jobs, outbound checks, physical second-device tests and final 6.30–6.31 review remain open |

No unchecked consolidated acceptance item is marked complete solely because this baseline smoke suite passed. When models are selected, record their exact artifact links/revisions, local paths, quantizations, tokenizer/templates, full dependencies, licenses and intended role. Preserve existing candidate plans and historical completed work.
