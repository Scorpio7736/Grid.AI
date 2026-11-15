# Main branch requirements

Agreed 2026-11-15. `main` is the production branch. The initial requirements are limited to the two rules below. Other review, approval, device-matrix and release-protection proposals are deferred.

## 1. All tests pass

Before promoting a candidate to `main`, run the full solution test suite against the final candidate commit and retain the command, commit ID and results. Every discovered test must pass: failures, skipped tests, missing test-project results and incomplete runs do not satisfy this requirement. Run in an environment capable of executing the tests; missing local privileges are not evidence that a skipped test passed. A later code change invalidates earlier evidence for the candidate.

Baseline command from the repository root:

```powershell
dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false
```

Existing PC2 results with four symbolic-link privilege skips remain valid development evidence, but do not satisfy this stricter production promotion requirement. This rule does not authorize changing Windows privileges or disabling tests.

## 2. Keep installation reproducible

A fresh clone must be installable using the tracked setup instructions and verified dependencies, without relying on a developer's absolute paths, globally installed Ollama, personal model/package caches, credentials or saved worlds. Internet is permitted for the explicit online setup. After preparation, the offline path may use the documented provisioned dependency cache.

Keep `Setup-GridClone.ps1`, `Start-GridClone.ps1`, `Grid-Clone-Dependencies.json`, pinned model manifests and the [installation instructions](Project_GRID_Fresh_Clone_Installation.md) consistent. Runtime/model/SDK inputs must remain versioned and hash verified; do not silently substitute mutable upstream tags or remove integrity checks. Dependency updates must update their recorded pins and verification evidence together.

When installation inputs or behavior change, verify installation from a clean clone on the documented supported baseline, retaining the candidate commit, commands and results. Verification must include dependency checks, build and the installer's real-model conversation check. Re-running setup against an existing private cache alone does not demonstrate a fresh installation. This requirement means repeatable installation behavior, not byte-identical generated binaries or a claim of compatibility with every device.

The currently implemented baseline is Windows x64. A broader supported-device matrix remains future work; neither rule is tied to PC1 or PC2.

## Future requirements (deferred)

The following ideas are recorded for later adoption. They are not additional active `main` branch gates. Activate individual requirements explicitly and record their enforcement and verification before treating them as required. Existing project acceptance gates elsewhere remain in force.

- [ ] **Protected promotion path:** accept changes through pull requests from `updates_CodeReview`; prevent direct and force pushes to `main`, and require configured automated checks before merging.
- [ ] **Specifications and evidence:** each candidate describes the requested behavior, acceptance criteria and evidence that the final changes meet them, including known limitations.
- [ ] **Stronger-model review:** use the developer's selected review model to inspect correctness, security, regressions and unnecessary scope. Resolve critical/high-severity and other blocking findings. If review introduces fixes, rerun affected checks and review the final diff; a model's approval alone is not verification.
- [ ] **Solo-developer release approval:** require the owner's explicit final merge decision after reviewing results, without requiring a second human reviewer.
- [ ] **Supported-device matrix:** define supported operating systems, processor architectures, minimum memory, CPU/GPU inference modes and permanent/portable installations. Require compatibility evidence across declared configurations rather than acceptance on one named PC. Record missing coverage as open; do not promise all possible hardware.
- [ ] **Checks appropriate to the change:** add automated platform coverage where runners are available and real-world checks for affected configurations. Runtime/model changes need actual inference checks; installation and portable changes need clean-install, relocation and disconnected-use checks; persistence changes need save/load, migration and recovery checks. Documentation-only changes should not trigger unrelated heavyweight device checks. The current full-suite requirement remains unchanged until explicitly revised.
- [ ] **Clear unsupported-device behavior:** report hardware/resource limits and unavailable capabilities accurately, offer supported fallback modes, and avoid crashes or misleading AI-readiness claims.
- [ ] **Data and permission preservation:** verify upgrades and device moves preserve compatible worlds/history, support recovery and revalidate machine-bound permissions without silently carrying approvals to a new host.
- [ ] **Documentation consistency:** keep affected user instructions, architecture references, implementation evidence and the master checklist aligned with released behavior.
- [ ] **Repository hygiene:** check changes for credentials, personal worlds/history, model weights, downloaded runtimes and build outputs; keep these outside production source history.
- [ ] **Recoverable releases:** tag approved production versions, retain a known-working package and document and verify rollback, including saved-data compatibility limits.

Keep development test results distinct from production release acceptance. Future checks should reuse existing evidence where applicable, while remaining bound to the final candidate and its affected supported configurations.

## Enforcement status

These are repository working requirements. GitHub required status checks and branch protection are not configured by this change. Do not claim automated enforcement or current production qualification from this document alone.
