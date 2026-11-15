# Fresh repository clone installation

A repository clone is a new GRID instance; `grid -copyto` transfers an existing instance. These are different workflows. Binary runtimes and model weights are not stored in Git.

## PC2: new instance from the repository

After pulling this revision, open 64-bit PowerShell in the cloned repository:

```powershell
.\Setup-GridClone.ps1 -Plan -Download
.\Setup-GridClone.ps1 -Download
.\Start-GridClone.ps1
```

`-Plan` is read-only. `-Download` explicitly permits downloading the pinned Ollama 0.40.9 archive, .NET SDK 10.0.401 and model blobs, plus restoring project NuGet dependencies. It needs Internet during installation and at least 20 GB free staging space. Runtime/model archives are checked against `Grid-Clone-Dependencies.json`; model blobs are checked against the tracked pinned manifest. A changed upstream model tag cannot silently substitute different weights. The small upstream manifest metadata blob is tracked separately because the installed pinned manifest references it as a layer.

Ollama archive digest was read from the [official release metadata](https://github.com/ollama/ollama/releases/tag/v0.40.9); the SDK archive hash from [Microsoft .NET 10 release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json). The [official Windows documentation](https://github.com/ollama/ollama/blob/main/docs/windows.mdx) describes the standalone archive. Model redistribution notices remain in THIRD_PARTY_RUNTIME_NOTICES.md and its retained license blob.

Layout:

- `.grid-runtime`: local SDK, Ollama, shared model weights and NuGet cache.
- `.grid-app`: built application and readiness marker. Setup must pass real inline/attached-protocol model greetings before marking it ready.
- `.grid-data/world.db`: this clone's world, identity and history, created only on first launch. First launch uses normal local credential setup. No PC1 world or shared LocalAppData world is imported.
- `.grid-install-*`: retained download/staging directories. Failures do not erase these or overwrite worlds. These directories and private data are ignored by Git and excluded from portable source gathering.

Always use `Start-GridClone.ps1` for this instance. Ordinary `dotnet run` retains its historical shared-user default world path. Restarting the clone launcher resumes the same clone world; setup is not a reset command. Existing PC2 data remains where it was. No automatic grant/credential migration occurs.

## Offline inputs and failures

With an already provisioned `.grid-runtime` and populated `.grid-runtime/nuget`, run `Setup-GridClone.ps1` without `-Download`. NuGet sources are cleared, so a cache miss fails rather than downloading. Alternatively explicitly provision selected local files using `Prepare-GridRuntime.ps1`, then run clone setup.

Incomplete existing runtimes are retained and reported; the installer does not silently overwrite them. A failed build/model test marks setup incomplete. Resolve the exact error and rerun setup. A new repository clone can be used to stage another fresh installation without deleting the failed instance. GRID_AI_DISABLED remains authoritative and must be reviewed if set. No global Ollama service, machine-wide SDK installation, cloud inference, security-policy change or user-world reset is part of setup.

## Evidence and limits (2026-11-14)

- Final offline setup passed with existing verified local assets: isolated build succeeded with zero warnings/errors; bundled Qwen3 4B answered Hello MCP through inline dispatch and the attached named-pipe protocol. No saved user world was opened.
- Six clone regression cases passed: read-only plans with/without network permission, clone-local world location, missing-dependency refusal/world preservation, unverified-launch refusal, corrupt-model refusal, and exact tracked manifest/blob pins.
- Full solution: 1,437 passed, zero failed/skipped. `TestResults/onboarding-pc2` outputs; `fresh-clone.trx` per test project.
- Official release metadata and small model component endpoints were checked. Full multi-gigabyte online download/extraction on a clean PC and PC2 physical login/window/offline acceptance are NOT yet verified. The online installer therefore still needs PC2 acceptance; the offline test is not represented as that evidence.
- The earlier MCP missing-workspace clarification failure remains open.
