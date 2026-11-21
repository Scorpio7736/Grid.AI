# GRID continuous quality and release management

## 6.30 implementation status

Development release gates were implemented and verified on 2026-11-13. `Grid.AI/ModelReleases.cs` provides evidence comparison and an atomic release catalog. `Grid.Terminal/ReleaseCommands.cs` exposes development entry points. `ModelReleaseVerification` exercises the lifecycle with isolated synthetic assets through the executable.

The full production model/device acceptance matrix remains open. The current verifier exercises release-management mechanics using declared fixture results. Model-quality measurements must come from the selected candidate's actual evaluation runner. Existing Qwen acceptance, synthetic LoRA evidence, hardware-profile tests and guarded host regression tests retain their existing scopes.

## Promotion criteria and evidence

A release request contains `Policy`, `ExpectedActive` and `Candidate`. The candidate contains a `Definition` and `Evidence`. Produce request JSON with the public C# records and `System.Text.Json`.

- The definition records a unique release ID and scope, the existing `ModelManifest`, dataset/suite/device SHA256 identities, exact runtime compatibility, world schema and host API version.
- The model manifest inventories all released weights, prompts, adapters and evaluation artifacts with sizes and hashes. A prompt artifact and evidence artifact are required. Include every dependency needed to reproduce and load the candidate. Catalog control files are excluded from artifact inventories.
- Evidence binds to `ReleaseEvaluation.Hash(definition)` and `ReleaseEvaluation.Hash(policy)`, records UTC time, and supplies exactly one result for each required check. The checks are functional, adversarial, regression, performance, compatibility, migration and rollback.
- Each result records pass/fail, a normalized score, elapsed milliseconds and peak memory bytes. Performance measurements must be positive. Missing, duplicate, failed, non-finite or out-of-range results fail the gate.
- The policy defines its version, minimum score, maximum latency/memory, allowed baseline-relative latency/memory ratios, score drop and evidence lifetime. Criteria are supplied explicitly before evaluation and stay fixed within a release track.
- Candidate comparison requires identical scope, dataset, suite, device, runtime, world schema and host API. A separately reviewed track handles compatibility or criteria changes. Migration/rollback check results must be backed by the corresponding tests before use in a real release.

The synthetic verifier chooses a 0.9 score floor, 1,000 ms latency ceiling, 1,000,000-byte memory ceiling, 1.2 regression ratios, a 0.02 score-drop allowance and 30-day evidence lifetime. These values exercise the fixture. Candidate owners must choose appropriate production thresholds and scoring definitions.

The gate validates attested results and artifact hashes. It cannot establish that an operator-authored report truthfully describes a test run. Preserve runner logs, suite revisions and independently reviewed evidence with the bundle. A trusted release record is needed to establish authenticity beyond local JSON integrity.

## Catalog, publication and rollback

The asset root contains `model-release-catalog.json` and an exclusive lock file. Accepted release definitions/evidence are retained with their previous release ID. Each promotion or rejection records the candidate evidence, candidate hash, baseline ID, criteria hash, timestamp and reason. The audit retains the latest 512 attempts. Each track permits 128 accepted releases and an 8 MiB metadata file.

A promotion takes the catalog lock, checks the expected active baseline, evaluates the candidate and verifies both baseline and candidate assets using the existing `ModelRegistry.VerifyAsync`. It then flushes a sibling temporary file and atomically replaces catalog metadata. Retrying the exact active release verifies its assets and returns success. Reusing an accepted release ID for different content is refused. Concurrent attempts either acquire the lock and recheck state or fail for retry.

Rejected candidates preserve the active pointer and accepted releases while recording their failed attempt. Initial rejection records an empty track. Cancelled operations preserve the prior committed state. Rollback requires the expected current release and verifies the retained predecessor's assets before restoring its pointer. Historical acceptance remains available for rollback after its original admission window expires.

Asset retention is the operator's responsibility. The catalog references hashed files under the asset root. Keep version-specific assets and their evidence available for rollback. Missing or changed predecessor files cause rollback to fail. The catalog supports development release selection. Host routing and live agent adoption remain part of 6.31 integration.

## Commands

Run from the repository root. Commands execute before ordinary world startup.

```powershell
dotnet run --project Grid.Terminal -- --verify-releases
```

Expected output includes two successful promotions, a rejected regression, rollback to `v1` and a final synthetic lifecycle pass. The verifier creates and cleans up its own temporary directory, including a synthetic world canary.

```powershell
dotnet run --project Grid.Terminal -- --release-check "C:\release-assets" request.json
dotnet run --project Grid.Terminal -- --release-promote "C:\release-assets" request.json
dotnet run --project Grid.Terminal -- --release-rollback "C:\release-assets" expected-active-id
```

`--release-check` evaluates and hashes assets. `--release-promote` records the decision in that development catalog. `--release-rollback` restores its verified predecessor. Existing `ai registry` commands continue to manage their established model inventory. Release catalog commands operate separately from saved GRID worlds.

## Verification and remaining acceptance

Verification passed 18 focused release regression cases and all 1,304 solution tests. The executable's `--verify-releases` also passed. Tests cover promotion/retry, score and resource regressions, failed/missing/stale/future/badly bound evidence, asset corruption, failed rollback, stale baselines, criteria changes, compatibility changes, cancellation, concurrent promotion and preservation of world/registry canary files.

Production functional/adversarial/performance evaluation for each selected model/prompt/adapter combination remains pending candidate availability. No-GPU Lite, actual 8 GB Standard, 12 GB FLUX eligibility, network-blocked jobs, provider load/unload, validated document/code output, injected failures and separate-device acceptance remain in the master checklist. The next sequential integration module is 6.31, with these outstanding gates carried forward.
