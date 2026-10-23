# Fresh clone and prepared portable runtime

**Implemented and verified — 2026-10-23.** This fixes hidden runtime assumptions exposed by deleting the old checkout and cloning again. It is a portable-runtime foundation, not completion of all PC/Flash edition, voice, device migration or clean-machine release gates.

## Source clone versus prepared release

Git contains source, tests and preparation scripts. It intentionally does not contain multi-gigabyte proprietary/third-party runtime binaries, model weights, user worlds or local caches. A clone needs a .NET SDK for building and one-time asset provisioning. Deleting uncommitted changes deletes work that a new clone cannot recover. Commit and push source/documentation changes before relying on a future clone; distribute a prepared release for users who should not build or provision anything.

From a source checkout on a provisioning machine:

```powershell
.\Prepare-GridRuntime.ps1
dotnet run --project Grid.Terminal -- --verify-runtime
dotnet run --project Grid.Terminal
.\Build-GridPortable.ps1
.\Test-GridPortable.ps1 -PackageRoot .\portable\GRID7736
```

`Prepare-GridRuntime.ps1` accepts explicit OllamaSource, ModelCache, DotnetSource, NugetCache and Destination paths. Defaults import existing assets once. It validates Qwen blob sizes/SHA256, copies the Ollama executable/libraries and only Qwen's referenced model blobs, copies the SDK and available NuGet cache, and records model manifest hash/runtime version in assets.json. It does not download a model or overwrite an existing runtime folder. If assets are unavailable, provision them during installation and rerun with their paths. Missing dependencies fail clearly instead of relying on an undocumented global server.

The prepared runtime is `.grid-runtime/` in a source checkout. The package layout is:

```text
GRID7736/
  launch.cmd
  grid-package.json
  THIRD_PARTY_RUNTIME_NOTICES.md
  app/          self-contained Windows x64 terminal and bundled documentation
  data/         saved world, backups and task artifacts created at runtime
  runtime/
    ollama/     executable and native libraries
    models/     private Qwen3:4b manifest and blobs
    dotnet/     SDK/toolchain for MAXIS
    nuget/      provisioned package cache
    assets.json
    home/       child runtime/tool state, created on demand
    temp/       child runtime temporary files
    ai-runtime.log
```

The preparation/build scripts preserve source assets. Build refuses an existing destination and marks partial packages pending until publish/copy completes. Publish may need network access to obtain .NET runtime packs during installation/build. Runtime inference never downloads assets. `launch.cmd` uses the packaged executable, not PATH or global dotnet. Portable world storage is package-relative and portable startup skips the user PATH launcher installer. Copy/move launchers and cleanup helpers now use apphosts; transferring an already prepared package includes its bundled runtime directory.

MAXIS now receives the bundled SDK path instead of assuming Program Files/dotnet; child CLI home and package cache resolve inside runtime. Arbitrary third-party workspaces still need their own sources, valid machine-bound scope and pre-restored project dependencies compatible with the bundled SDK. No tool can supply unknown project dependencies automatically while remaining offline. The provisioned NuGet cache may contain private packages; review its contents/licenses before distributing a release.

## AI lifecycle

Normal owner startup automatically starts a hidden bundled Ollama child on an available IPv4 loopback port. Its model cache, HOME/USERPROFILE, LOCALAPPDATA and temporary folders point inside the bundle. Cloud is disabled, one model/request runs at a time, and no global installed Ollama or user model cache is used as fallback. The host owns and stops only its own child process tree. Other personal Ollama servers are left alone. Logs are bounded to 5 MiB per runtime log; readiness has a 30-second startup limit. A missing/incomplete bundle or startup failure produces an explicit diagnostic and deterministic operation.

`ai status` reports the actual per-host endpoint; port 11435 is no longer a prerequisite. `GRID_AI_DISABLED=1` still intentionally disables AI. `Start-GridLocalAI.ps1` is a compatibility notice directing users to automatic startup/provisioning; it no longer launches a global service.

`--verify-runtime` starts the bundled runtime, sends a real readiness request and grounded MCP turn, checks animation metadata, and shuts it down. It uses an in-memory world and never opens the user's saved world.

## Typewriter repairs

Unavailable AI replies previously explicitly removed animation metadata. Fallback now retains the correct offset. The original terminal now renders animated replies too, with `typewriter on/off`; attached ordinary terminals also honor animation metadata. An explicit `typewriter on` overrides the initial GRID_TYPEWRITER=off preference. Redirected input/output and TERM=dumb remain intentionally instant. Any key skips; revocation checks continue during rendering. Visible timing on a real interactive console remains a manual check; automated tests exercise Unicode, skip, cancellation, authorization and output metadata.

## Verification and remaining gates

**648 tests passed:** 14 Core, 157 Simulation, 98 Persistence and 379 Terminal. Six new cases cover runtime-root relocation, missing assets without global fallback, loopback endpoint restrictions, inline controls and fallback animation. Real private-runtime startup, grounded chat and orderly shutdown succeeded from the source tree.

A self-contained package was built and Test-GridPortable.ps1 passed with PATH restricted to System32, a fresh USERPROFILE/HOME/LOCALAPPDATA, invalid global DOTNET_ROOT and an empty external model-cache path. The packaged runtime/model still returned GRID LOCAL READY and an actual MCP answer. This verifies independence from those global paths on this PC; it is not a fresh Windows VM, a second physical PC, or a firewall-isolated network test. The tests do not establish model factual accuracy.

Windows x64, supported hardware drivers and Windows facilities remain platform requirements. Speech recognizers/voices, security services, alternate hardware, read-only/unplugged media, PC/Flash installer choice, credential/device migration and full offline feature acceptance remain under 8.6/8.7 and the restored planning sections. Do not label all of GRID fully plug-and-play yet. No schema change (v20) or user-world migration occurred.

Final package observation: 9,872,514,113 bytes (about 9.9 GB) including the provisioned SDK/cache. No GRID-owned Ollama/llama-server processes remained after verification. Final focused runtime/typewriter rerun: 11 tests passed; self-contained publish completed successfully.
