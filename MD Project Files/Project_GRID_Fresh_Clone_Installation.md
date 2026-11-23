# Fresh repository clone installation

**PC 2 acceptance follow-up — 2026-11-23:** After pulling the isolated persistence test-preflight fix, all 9 focused Phase Two tests passed. Full `dotnet test .\Grid.AI.slnx` completed with **1,650 passed, 0 failed, 4 skipped** (Windows symbolic-link privilege). Restore and build pass. On PC 2, live MCP conversation still needs separate acceptance: one prompt returned invalid output and a subsequent prompt was rejected with the runtime's below-1-GiB-available-RAM guard and switched to simulated dialogue. No evidence yet that normal sustained inference is reliable on this machine. Check OS available RAM, `ai status`, and memory pressure; do not weaken memory protection or treat the full test pass as a live-model acceptance result.


**PC 2 follow-up — 2026-11-23 (awaiting re-test):** On a freshly cloned Windows machine, `dotnet restore .\Grid.AI.slnx` and `dotnet build .\Grid.AI.slnx` passed. The full suite reported 1,648 passed, two failed persistence subprocess tests, and four symbolic-link privilege skips. Both failures returned code 2 from noninteractive `Grid.Terminal` starts: the first-run bundled AI preflight can refuse an unprepared test host before the seeded world is exercised. The isolated persistence-test child environment now sets `GRID_AI_DISABLED=1` and includes captured stdout/stderr in exit-code assertions; normal app launch, onboarding, and real-model acceptance remain unchanged. A PC 2 test rerun and offline/portable acceptance are still required. The user requested a guided installer as the next checklist item, not a completed installer. Normal terminal execution on PC 2 subsequently provisioned its local baseline and reported successful real inline/attached MCP greetings; `--help` is currently an unsupported terminal flag and should be handled more clearly in future onboarding.


A repository clone is a new Grid.AI instance; `grid -copyto` transfers an existing instance. These are different workflows. Binary runtimes and model weights are not stored in Git.

**First-run repair — 2026-11-18:** `Start-GridClone.ps1` now detects an unprepared clean clone before any world is opened, explains the approximately 6 GB baseline, and offers to run the pinned download, hash verification, build and real MCP conversation test automatically. Direct interactive source launches use the same preflight. Non-interactive launches never fall through into misleading simulated chat: they stop with the exact setup command and state that no world was opened. A corrupt partial runtime selects the retained-file `-RepairRuntime` path. Explicit `GRID_AI_DISABLED=1` remains the only opt-out for a deliberately AI-disabled session.

Verification passed all **1,453 solution tests** through `Grid.AI.slnx`. The source runtime produced real inline and attached MCP replies. A newly built sealed package at `TestResults/Grid.AI-portable-20261118` independently produced both real replies from its destination-local runtime/model before publication. This proves current same-PC provisioning and package completeness, not physical second-device compatibility.

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

Use `Start-GridClone.ps1` for this clone-local instance. It now provisions missing verified assets interactively on first run. Ordinary `dotnet run` uses its historical shared-user default world path but performs the same runtime preflight before opening it. Restarting the clone launcher resumes the same clone world; setup is not a reset command. Existing PC2 data remains where it was. No automatic grant/credential migration occurs.

## Offline inputs and failures

With an already provisioned `.grid-runtime` and populated `.grid-runtime/nuget`, run `Setup-GridClone.ps1` without `-Download`. NuGet sources are cleared, so a cache miss fails rather than downloading. Alternatively explicitly provision selected local files using `Prepare-GridRuntime.ps1`, then run clone setup.

Incomplete existing runtimes are retained and reported; the installer does not silently overwrite them. A failed build/model test marks setup incomplete. Resolve the exact error and rerun setup. A new repository clone can be used to stage another fresh installation without deleting the failed instance. GRID_AI_DISABLED remains authoritative and must be reviewed if set. No global Ollama service, machine-wide SDK installation, cloud inference, security-policy change or user-world reset is part of setup.

To replace an incomplete runtime, close Grid.AI and run `Setup-GridClone.ps1 -Download -RepairRuntime`. This explicitly moves the old runtime into a unique `.grid-install-retained-*` directory before downloading verified replacements. It requires 20 GB free space in addition to the retained files. A download failure leaves the old files in that retained directory; it does not restore them automatically. World and identity files are untouched. Without `-Download`, repair refuses before moving files.

## PC2 online installation (2026-11-15)

On the user's PC2 clone, `Setup-GridClone.ps1 -Download` completed the full online path: pinned Ollama/SDK archives, pinned Qwen model blobs, hash/size verification, NuGet restore, Release build (zero warnings/errors), and real-model inline plus named-pipe attached greetings. The installer published `.grid-app/clone-ready.json` with `ready: true`. MCP answered “Hello. I'm MCP, your local Grid.AI coordinator.” No saved user world or credential setup was opened by verification. Start this installation with `Start-GridClone.ps1`; an already running terminal must be closed and restarted.

This establishes online installation and actual inference on PC2, not compatibility with every machine. Supported baseline remains Windows x64, sufficient storage/memory, and Internet for first installation. Physical conversation windows, first credential setup and disconnected operation on PC2 remain open acceptance checks. Earlier development-PC evidence below is historical.

Verification after the follow-up changes: `dotnet test Grid.AI.slnx --verbosity quiet -p:NuGetAudit=false` exited 0: **1,434 passed, zero failed, four skipped** (symbolic-link fixtures require privileges unavailable on this account). Seven fresh-clone cases passed, including partial-runtime repair guidance, offline repair refusal/file preservation and corrupt-model refusal. The Windows PowerShell child test environment now initializes its own module paths instead of inheriting PowerShell 7 paths. `git diff --check` passed. Full online replacement of a deliberately damaged runtime was not repeated; successful online installation above used an absent runtime.

## AI readiness alerts (2026-11-15)

If bundled AI startup is unavailable (including missing/corrupt assets, disabled inference, startup failure or timeout), the host now immediately prints `WARNING: AI unavailable — setup or repair required.` with the retained cause and `ai status` guidance. The warning repeats after login so the welcome display cannot hide it. It explicitly states that simulation startup does not enable AI and that connections can only provide basic fallback replies. Inline connections, resumed conversations and meetings include the warning before their connection text; attached conversation windows show it in both the parent opening result and their initial conversation response. Available runtimes receive no warning. Ordinary commands remain usable; the alert does not grant authority, install dependencies or prevent conversation-history access.

This is the result of the startup check, not a continuous health monitor or a guarantee that every generation will succeed. Later inference failures retain the existing per-turn diagnostics and fallback notice.

Verification: actual terminal process with `GRID_AI_DISABLED=1 --ephemeral --no-color` displayed the warning, exact disabled cause and fallback limitation before login, then exited 0 on closed input. No saved world was opened. Added regression cases exercise a genuinely missing runtime, recovery guidance and retained line breaks, healthy-warning suppression, connect/resume warnings, and attached parent plus initial named-pipe reply. Full solution: **1,436 passed, zero failed, four symbolic-link privilege skips**. `git diff --check` passed. Physical window rendering remains unverified.

## Evidence and limits (2026-11-14)

- Final offline setup passed with existing verified local assets: isolated build succeeded with zero warnings/errors; bundled Qwen3 4B answered Hello MCP through inline dispatch and the attached named-pipe protocol. No saved user world was opened.
- Six clone regression cases passed: read-only plans with/without network permission, clone-local world location, missing-dependency refusal/world preservation, unverified-launch refusal, corrupt-model refusal, and exact tracked manifest/blob pins.
- Full solution: 1,437 passed, zero failed/skipped. `TestResults/onboarding-pc2` outputs; `fresh-clone.trx` per test project.
- Official release metadata and small model component endpoints were checked. Full multi-gigabyte online download/extraction on a clean PC and PC2 physical login/window/offline acceptance are NOT yet verified. The online installer therefore still needs PC2 acceptance; the offline test is not represented as that evidence.
- The earlier MCP missing-workspace clarification failure remains open.
