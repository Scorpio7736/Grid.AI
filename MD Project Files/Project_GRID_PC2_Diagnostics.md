# PC2 diagnosis — 2026-11-14

[Original user-supplied output](verification/6.31-pc2-reported-errors.txt) is retained as observed evidence. It reports 1,374 tests, 1,370 passed, four failed, no skips, then GRID ai test/status output. Paths in the stack identify a source checkout at `C:\Users\jack\source\repos\Grid7736`. This does not establish that a complete portable package was transferred or launched.

## Confirmed causes

1. **AI assets missing.** The startup report explicitly says `Local AI assets missing`, and inference is disabled. The later `ConnectionRefused` targets `127.0.0.1:1`, GRID's placeholder endpoint when no bundled server started. This report does not support a firewall, GPU or model-inference diagnosis. The log did not identify exactly which asset was absent.
2. **Test-fixture privilege missing.** Four cases fail in Windows CreateSymbolicLink with `A required privilege is not held by the client`, before reaching reparse-policy assertions: MaxisTests.ReparseWorkspaceAndMissingProjectAreRefused, JnTests.LinksAreNotFollowedAndLimitsProducePartialReports, WorkspaceChangeTests.ReplacedParentLinkCannotRedirectApprovedWrite, and ConversationalFileTests.DeleteAndReadRejectReparseTargetsAndPreserveOriginalBytes. This is separate from AI startup.

## Changes

- Disabled `ai status` now preserves the startup reason and does not probe the placeholder HTTP endpoint. Missing-assets startup messages show the resolved runtime root and absent required files (assets.json, bundled Ollama executable, pinned Qwen model manifest). Assets behind a present manifest still pass the existing package verification.
- The four link-dependent tests use a capability-aware Fact attribute. It actually probes file and directory symbolic links once; only Windows privilege error 1314 produces an explicit skip. Other setup failures remain errors. No Windows policy or privilege is changed. These skips are coverage gaps, not passing reparse checks.
- Missing-project and inventory-limit/root-refusal assertions were separated so they continue to run without symbolic-link privileges. The real reparse checks continue running on capable test hosts.
- Corrected the diagnostic CPU/Lite command to `ai registry force-lite on`. It is not the remedy for this missing-assets failure.

## Recovery and PC2 retest

Use a complete package from the working PC: after building the latest source, use `grid -copyto -select`, wait for verified completion, and launch the destination's `launch.cmd` on PC2. Keep the app and runtime/model directories together. Do not launch an unprovisioned clone or copy only the executable. A complete package is intended to use its own AI runtime; global Ollama is not a fallback.

For a source checkout instead, provision `.grid-runtime` with `Prepare-GridRuntime.ps1` and the script's verified local runtime/model/SDK inputs. The script is a provisioning step, not a promise that missing assets are already present on PC2. A source clone alone does not include them.

Then capture startup, `ai status`, `ai test`, and a chat response with the network disconnected. Keep any new exact error and runtime log. Passing the repaired test suite does not prove AI readiness. No successful PC2 AI retest has been supplied yet; 6.31 second-device acceptance remains open.

## Verification

Focused verification passed 15 cases with no skips on the development host. With the explicit test-only `GRID_TEST_DISABLE_SYMLINKS=1` capability simulation, two independent checks passed and the four link cases were reported skipped, with no failures. This simulation does not substitute for rerunning on PC2. Full solution verification passed **1,378 tests**, no failures or skips, on the development host (14 Core, 157 Simulation, 99 Persistence, 1,108 Terminal). Command: `dotnet test --artifacts-path TestResults/pc2-fixes -p:NuGetAudit=false -p:RestoreIgnoreFailedSources=true --verbosity quiet`.
