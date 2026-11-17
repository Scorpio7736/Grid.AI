# Grid.AI device integration and capability broker - 6.33

## Implemented baseline - 2026-11-14

Grid.AI exposes a bounded local device catalog and reviewed device actions through the original Master Control terminal. The implementation uses Grid.Agent typed contracts, the engine's existing TaskLease authorization and history, and the 6.23 file-destination gateway for folder creation. Existing file review/apply and screen-assistance services remain authoritative. This does not add arbitrary commands, elevation, unattended app control or cloud access.

Sources: [contracts](../Grid.Agent/DeviceCapabilities.cs), [host broker](../Grid.Simulation/GridSimulationEngine.Devices.cs), [Windows adapter](../Grid.Terminal/WindowsDeviceIntegration.cs), [terminal commands](../Grid.Terminal/DeviceCommands.cs).

## Commands and review

Return to the main terminal with `return to terminal` if a conversation is connected. Log in as Master Control and start GRID.

```text
device catalog
device apps
device paths
device processes
device windows
device resources
```

Apps uses the existing saved installed/running application catalog, with last-observed timestamps and a stale-data notice. Paths lists enabled workspace/storage registrations for this machine without reading contents. Processes/windows reads current-session process IDs and names, with no command lines or window titles. The result has source/time and an incomplete flag. It stops at 256 rows or a checked two-second traversal budget; individual Windows calls can take longer. Resources delegates to the existing timestamped telemetry view. Discovery never grants action authority.

Choose exactly one target:

```text
device select launch "C:\Program Files\Example\Example.exe"
device select focus 12345
device select inspect 12345
device select mkdir "C:\Users\jack\Documents\New Folder"
```

These are examples; use a real observed PID or selected local path. Inspect reads only the selected window title after approval; it does not read controls, pixels, passwords or clipboard contents. Focus does not type or click.

Selection prints a review ID, operation, exact target, fingerprint and expiry. Review it, then issue the displayed commands separately:

```text
device approve <review-id>
device run <review-id>
device revoke <review-id>
```

Revoke cancels an unexecuted grant/approval. Selection expires after two minutes. At most 32 selections are retained per engine, with expired/current-session stale entries pruned on new selection. The ID is session-bound, not a reusable credential. There is no global approval or implied grant from app discovery. Main-terminal commands are intentional for these new capabilities; conversation help points to `device catalog`, but arbitrary conversational app control is not yet integrated.

## Policy and target binding

- Typed requests accept version 1 and only Launch, Focus, InspectWindow or CreateFolder. The broker owns immutable review data; callers cannot supply approval flags or replace the stored request at execution.
- The trusted identity provider marks original-host leases. The default is false. Agent sessions and attached windows cannot select, approve or execute device work. World/session/generation/operator, simulation activity generation, current device identity, request version and target fingerprint are checked before approval and again after the persistence fence.
- Launch requires an explicit existing local .exe with no arguments. SHA256 binds executable bytes, with a 256 MiB verification bound. Shortcuts, URLs, UNC/device paths, streams, reparse paths and known command/script hosts are refused. Process.Start uses UseShellExecute=false and requests no elevation. This is a user-approved application launch, not a sandbox for the application's subsequent behavior.
- Window actions bind PID, process start time and main-window handle in the same desktop session. PID reuse or a changed handle invalidates selection. Process metadata is not authentication; the host lease remains mandatory.
- Folder creation validates the selected existing parent through WithFileDestination and existing protected/reparse/path rules, then creates exactly one empty child. Native CreateDirectory refuses an existing target. File edits/deletions remain on the existing exact-hash preview/approval/apply/rollback path.
- Revocation, logout/login, agent switching, expiry, pause/restart, changed device and changed target invalidate outstanding authority. Grants and approvals are never serialized or restored across process restart. Re-selection is required.
- Filesystem/process checks reduce accidental stale-target use but are not an OS security boundary against malicious concurrent replacement by another process. No secure-desktop access, privilege escalation or OS sandbox is claimed.

Windows may refuse a foreground request. Grid.AI reports that refusal and asks for manual selection; it does not attach input queues or bypass Windows foreground policy. See [Microsoft SetForegroundWindow documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow).

## Audit and failure behavior

Execution requires the persistent host save callback. Ephemeral terminals can discover/review but cannot execute through the terminal command without saved audit support. The engine records a claim with request hash, operator/session/generation and action, then awaits save before invoking the OS. It rechecks authority and target after saving. Concurrent dispatch of the same review is refused. Revocation during the save prevents invocation.

An attempt is consumed before OS invocation and never automatically retried, including unavailable/uncertain outcomes. Successful completion returns a typed receipt with ID, action, status, time and source. Launch completion means Windows returned a process ID; it does not certify application readiness. Audit history records action/status and request hash without paths, window titles or credentials. Detailed target/output remains in the current review/receipt display, not a new persistent private-content store.

The final status is saved after invocation. Failure to save that outcome returns OutcomeNotSaved and explicitly warns against blind retry. A crash after the saved claim but before the final save leaves an uncertain claim; no approval or device operation is automatically restored. A failed pre-action save invokes no OS action. A caller cancellation or scope change during the save likewise prevents invocation. Once a synchronous Windows operation has begun, cancellation cannot undo it; identity revocation waits for the short held host lease. Low-level OS calls and executable hashing do not have hard preemptive deadlines.

## Optional integrations

Clipboard access is disabled and has no grant route. New Windows notifications are not enabled; the existing explicitly approved RELAY inbox/speech capabilities retain their own receipts. Screen assistance remains an explicit one-shot selected-monitor workflow with active-scope checks; selecting a window does not grant screen capture or background monitoring. Password entry and elevated actions are unsupported. Read-only discovery, inspection, launch/focus and file writes have distinct policies in the catalog.

## Verification

Added 23 cases in [DeviceCapabilityTests](../Grid.Terminal.Tests/DeviceCapabilityTests.cs). Focused device/boundary/identity/task verification passed 54 tests. Coverage includes invalid versions/operations/targets, discovery without approval, separate review/approval/run, one-use receipts, original-host isolation, revocation, expiry, device/session/activity changes, changed target fingerprints, failed-save refusal, revocation during save, concurrent claims, uncertain final persistence, runtime unavailability and restart without grants.

Real Windows checks on this PC passed process discovery, exact-file hashing and launch of argument-free Windows where.exe, command-host refusal, temporary empty-folder creation, existing-folder refusal and the normal engine file-gateway path. Temporary folders were removed. The initial discovery assertion was corrected because a capped/incomplete inventory need not include the test process.

Full solution: **1,416 passed, zero failed/skipped** (Core 14, Simulation 157, Persistence 99, Terminal 1,146). Isolated outputs preserve any running ordinary Grid.AI instance.

```powershell
dotnet test Grid.Terminal.Tests/Grid.Terminal.Tests.csproj --artifacts-path TestResults/device-integration -p:NuGetAudit=false -p:RestoreIgnoreFailedSources=true --filter "FullyQualifiedName~DeviceCapabilityTests|FullyQualifiedName~IntegrationBoundaryTests|FullyQualifiedName~IdentitySessionTests|FullyQualifiedName~TaskBrokerTests" --verbosity minimal
dotnet test Grid7736.slnx --artifacts-path TestResults/device-integration -p:NuGetAudit=false -p:RestoreIgnoreFailedSources=true --verbosity quiet --logger "trx;LogFileName=device-integration.trx"
```

## Remaining real-device acceptance

The last 6.33 checklist item remains open. On a deliberately selected disposable application, check focus success/refusal, selected-title inspection, window close/relaunch between review and run, and inaccessible/protected-window handling. Verify physical read-only media and representative OS restrictions on supported Windows hosts. Deterministic injected failures establish broker behavior, not every physical-device outcome. Copy/transfer and second-PC acceptance remain deferred until the user has the transfer device.

Rebuild/restart the ordinary terminal before using the new commands; this work did not replace the running process. No new schema, installer, resident population or model was introduced. Broader Phase 7 release gates remain open.
