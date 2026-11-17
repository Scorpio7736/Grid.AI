# Project GRID — 6.14 Portable Transfer Repair and Second-PC Acceptance

**6.15 import implementation — 2026-11-04.** Offline PDF text extraction and DOCX body paragraph/table import are locally verified. The bundled parser uses bounded worker processes, page/paragraph provenance and explicit empty/scanned/encrypted/corrupt-file outcomes. 1,073 regression tests passed across current suites, with standalone PDF/DOCX deployment checks. Second-device acceptance and the final 6.7–6.31 gate remain pending. [Implementation, scope and verification](Project_GRID_Offline_Document_Import_Implementation.md).

**2026-10-30 transfer/AI follow-up:** Removed repeated full-content transfer checks while retaining independent destination verification and pinned model/manifest matching. Hashing now reports byte progress; ETA uses measured copy/verification work. AI diagnostics retain startup errors, and bundled startup tolerates slow media. Original-PC AI passed an isolated test; the reported failing instance remains undiagnosed pending its exact error. [Implementation, evidence and limits](Project_GRID_Transfer_Checks_and_AI_Diagnostics.md). Physical second-PC acceptance and 6.15 remain pending.

**Transfer progress follow-up — 2026-10-29:** Copy/move previews show an approximate duration range before confirmation. A separate Windows dashboard shows progress, elapsed/remaining time and a bounded live activity feed; validation and move cleanup cannot prematurely show completion. Closing the viewer leaves transfer running; text fallback remains available. See [progress window and verification](Project_GRID_Transfer_Progress_Window.md).

Status: implementation and local verification complete; second-PC acceptance is explicitly pending the user's test. PDF extraction remains planned in 6.15. Do not mark Phase 7 ready.

## Failure and repair

A development-build copy previously inventoried the terminal output and save only. It omitted the repository's `.grid-runtime` and copied a framework-dependent apphost without a destination-local .NET binding. Such a copy could work on the development PC through installed dependencies and fail on another PC. The former package marker did not prove runtime completeness.

The production transfer path now requires the bundled AI/model/.NET assets. From a repository build it includes `.grid-runtime` under destination `runtime/`, an available source snapshot, and the selected save/context/history/backups/JN/MAXIS artifacts. Shared repository source/runtime files are retained even when moving the development terminal and save. From a prepared package it copies the package-owned inventory. Runtime transient `home/`, `temp/` and AI log are excluded; saved user state remains included. Unrelated host files and external workspace contents are not silently imported.

`launch.cmd` sets `DOTNET_ROOT`, `DOTNET_ROOT_X64` and disables multilevel lookup, using its own location. Bundled runtime/model discovery stays destination-relative. Model SHA256 and required runtime files are checked; `portable-assets.json` records relative base paths, lengths and hashes. Every copied file is hash-checked, the copied database is loaded, the base budget is remeasured, and only then is staging published. The original source is retained if verification fails. A move waits for the original host to exit, checks the exact recorded inventory again, then removes only verified move-owned files; shared repository dependencies are not removed. Residual marker/launcher/receipt folders can remain at the old location.

`--seal-portable-assets <root>` is a packaging operation: after deliberate app/docs/source changes, regenerate the base manifest and validate the package. It is not a repair for an unknown or corrupt model; pinned model checks still apply. `--verify-portable` validates a copied package, reads its saved world ID without modifying it, and runs isolated profile and MCP workflow checks. Verification tools never certify that the network is physically disconnected or that the host is a different PC.

## Device/location boundary

A new device or changed package root triggers hardware discovery and a visible notice; the saved city and world identity are preserved. Before agent activity, workspace/storage grants are disabled and queued task approvals are invalidated; interrupted work is marked explicitly. Nothing is automatically retargeted to another machine's paths. Re-register scopes with new aliases and review approvals on that device. Saved file-recovery records remain evidence; existing exact-root/session review still applies.

After authentication, choose **s** to save this device profile or Enter to use once. Device records are stored in `data/devices.json` (maximum 32 devices), keyed by a hash of the local machine identity. One-time hardware data stays session-only; permission revocations are saved. The first changed-device/location session enables CPU fallback. `GRID_AI_CPU_ONLY=1` explicitly keeps CPU mode on later runs. The device chooser does not grant Windows elevation or replace the saved city. The original 8.9 hardware/device acceptance checklist remains subject to physical-device validation.

## Second-PC test package

Prepared destination: `portable/GRID.AI-SecondPC-614`. This is a **disposable test world**, not the user's live world. Copy the entire folder to a flash drive; do not copy only `app/` or the executable. Keep at least the measured package size plus free space for saves/logs and staging. Supported target for this test: Windows x64, adequate RAM and built-in Windows PowerShell. No global .NET SDK, Ollama, model cache or network should be needed. External-project dependencies must already be provisioned before offline MAXIS builds; the bundled SDK does not make arbitrary projects dependency-free.

1. On the second PC, record Windows version, RAM, CPU/GPU and the flash-drive letter. Disconnect Wi-Fi/Ethernet. Do not install Ollama or .NET to make a failed test pass; record the failure instead.
2. Open the package and run `launch.cmd`. Set up credentials for the test world if prompted. Confirm the device/location notice, choose one-time use, then run:

```text
ai status
ai profiles
ai storage
grid -start
connect MCP -inline
hello MCP
invite VOLT
accept invitation
@VOLT explain the difference between memory used and available
participants
disconnect
exit
```

3. Restart with `launch.cmd`. Confirm the saved world ID/city is unchanged. Save this device with **s**. Exit and restart again; the same device/location should be recognized.
4. In PowerShell opened in the package folder, run the bundled test runner (it uses the package's own app/runtime and an isolated profile):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Test-GridPortable.ps1 -PackageRoot $PWD.Path -TransferAcceptance *> .\second-pc-results.txt
```

Expect all profile checks and MCP workflow checks to pass. The command is read-only against the packaged world and uses synthetic worlds for AI/task tests. Keep the report paths it prints. Exit codes/failures must be included in the results.
5. Test `grid -copyto "E:\GRID614-copy"` with your actual writable drive/folder. The destination must not exist. Start the resulting destination with its own `launch.cmd`; repeat AI status/test and the verification runner there.
6. From that disposable copy, test `grid -transferto "E:\GRID614-moved"`. Wait for `transfer-status.txt` to say move complete, then launch and verify the destination. Do not use the live personal world for this test.
7. Safely eject after exit, reconnect under a different drive letter (or move the folder), and repeat startup/AI/verification. Expect a location reauthorization notice, with the saved world intact.
8. In a separate disposable copy, rename a required model blob, then request transfer/verification. Expect a clear missing/changed-asset refusal, no completed destination, and the source retained. Restore the filename before another launch. Test interrupted removable-media copy only with disposable copies and record staging/source retention; do not unplug while using the personal world.

Return `second-pc-results.txt`, generated report paths/results, the machine specification, whether networking was disconnected, and outcomes for copy, move, restart and changed drive letter. **PDF extraction is not an expected pass yet; complete that acceptance after 6.15.** DOCX/text use the bundled managed import implementation.

## Evidence and remaining gate

**Local verification — 2026-10-29:** 1,049 solution tests passed (14 Core, 157 Simulation, 99 Persistence, 779 Terminal); 33 transfer-focused tests passed again after the missing-manifest guard. The production transfer implementation made an actual copy from the framework-dependent repository build, including its previously omitted runtime. It then moved that disposable copy to `portable/GRID.AI-SecondPC-614`; destination validation passed and the source copy's world was removed only after verified cleanup. The live user world was untouched.

The relocated destination passed the stripped-environment runner using its own .NET/AI assets: **14 role-profile checks and six MCP workflow checks** passed, including CPU-only inference and recorded approved task results. Saved test-world ID: `6504a516-4f7f-4ef9-8fa2-3dc2a6dfdfb0`. The audited base was **9,926,842,663 bytes** (about 9.93 GB), below 20 GB; total was 9,927,129,383 bytes before the final documentation/evidence refresh. Runtime logs and verification evidence change total size; use `ai storage` for current values.

Evidence is included in the test package under `data/acceptance/`: isolated runner output, role profile report and MCP workflow report. The user's configured `Grid.AI` Debug launcher was rebuilt. Final package documentation/source updates are resealed with SHA256 integrity metadata. These are local results only. Same-PC relocated execution and stripped environment are supplementary evidence, not substitutes for the user's second-PC results. No second-device or removable-media pass is claimed until observed.
