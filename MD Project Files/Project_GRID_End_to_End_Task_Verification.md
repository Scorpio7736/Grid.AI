# Project Grid.AI — End-to-end task verification (5.7)

**Verified 2026-10-21.** Phase 5's explicit, human-approved task workflow is verified. No production-code or schema changes were needed; schema remains v19. Natural-language planning and shared local inference remain planned for Phase 6, starting with 6.1.

## Verification matrix

| Checklist item | Evidence |
| --- | --- |
| MCP delegates unit tests to MAXIS | `EndToEndTaskTests` creates an actual SDK/xUnit project and delegates `MAXIS: test fixture` from an MCP agent identity. |
| Scope and permissions | A registered temporary workspace is required; unapproved work produces no artifacts. `MaxisTests` also covers traversal, invalid parameters, revoked scopes. |
| Approved operation executes | Human approval dispatches the real installed dotnet runner. A passing fixture reports two passed tests; an intentional failure reports one passed and one failed. Both outer verification cases pass by asserting the expected outcomes. |
| Actual results and logs | Assertions inspect tests.trx counters, report.txt exit code, stdout.txt and stderr.txt; task status/result/audit are saved to a temporary SQLite world. |
| Results return through RELAY | A second MCP plan delegates `RELAY: brief FULL-TASK-GUID`. After separate approval, the inbox contains the source task's exact recorded result/status, including failures. Links and text survive SQLite reload. |
| Rejected and unauthorized requests | Existing broker/coordination/MAXIS tests cover invalid grammar, invalid capabilities, scope revocation and atomic rejection; new tests exercise every registered role. |
| Cancellation, recovery, duplicate dispatch | A delegated inspection pauses at the durable start barrier; concurrent dispatch is rejected, cancellation prevents adapter invocation, and restoring the saved Running record yields Interrupted with no approval. Retry still needs fresh consent. Repeated real-test plan keys and dispatch after completion do not rerun tests. Existing MAXIS tests cover actual process cancellation and broker tests cover timeouts and save failures. |
| No cross-role privileges | All six agent roles are denied another role's task proposal, capability substitution, human approval and dispatcher execution; task sessions do not grant world-edit leases. |

## Reproduction and observed results

Run each command serially from the repository with the .NET 10 SDK and restored dependencies:

```powershell
dotnet test Grid.Core.Tests/Grid.Core.Tests.csproj -c Verification --no-restore
dotnet test Grid.Simulation.Tests/Grid.Simulation.Tests.csproj -c Verification --no-restore
dotnet test Grid.Persistence.Tests/Grid.Persistence.Tests.csproj -c Verification --no-restore
dotnet test Grid.Terminal.Tests/Grid.Terminal.Tests.csproj -c Verification --no-restore
```

**560 tests passed, zero failed/skipped:** 14 Core, 157 Simulation, 97 Persistence, 292 Terminal. Nine new cases are in `Grid.Terminal.Tests/EndToEndTaskTests.cs`. Release verification: `dotnet build Grid.Terminal/Grid.Terminal.csproj -c Release --no-restore --verbosity minimal` succeeded with zero warnings/errors. The nested real xUnit runs are test fixtures and are not added to this outer suite count. For the focused verification, add `--filter FullyQualifiedName~EndToEndTaskTests` to the Terminal command.

The fixture uses a fresh temporary directory, its own database and separate MAXIS artifacts. Its restore clears all package sources and disables package audit, using the already cached package versions required by Grid.AI's tests. It downloads nothing. Missing cached dependencies cause a failure, not a skip. Temporary worlds, sources and evidence are deleted after verification; ordinary user task artifacts remain retained under the configured world artifact directory. The user's world is never loaded by these tests.

## Terminal acceptance test

As Master Control, register a test project, using a fresh alias if this one already exists or was revoked:

```text
agents initialize
maxis register coretests "C:\Users\jack\source\repos\Grid7736" "Grid.Core.Tests/Grid.Core.Tests.csproj"
mcp request "MAXIS: test coretests" phase57-tests-1
tasks list
```

Replace TASK with the displayed MAXIS task reference:

```text
tasks inspect TASK
tasks approve TASK
tasks run
tasks inspect TASK
```

MAXIS uses Debug and `--no-restore`; dependencies must already be restored. Review the scope before approval: the trusted project executes as the current Windows user and writes its normal build outputs. Avoid a running Debug Grid.AI instance locking those outputs. Inspect the reported artifact directory's tests.trx and stdout.txt for actual discovery/counts (currently 14 Core tests). Exit code zero alone does not establish that tests were discovered; the production result deliberately retains this caveat.

Copy the **full task GUID** from inspection, replace FULL-TASK-GUID below, then approve the newly created RELAY task:

```text
mcp request "RELAY: brief FULL-TASK-GUID" phase57-relay-1
tasks list
tasks inspect RELAY-TASK
tasks approve RELAY-TASK
tasks run
relay inbox
mcp brief
```

Use a new request key for a new run; repeating the same key deduplicates the plan. The inbox contains the recorded MAXIS outcome and report location, not a newly inferred test summary. RELAY briefing does not play audio or send externally. Optional `relay inspect NOTE` shows the full message; `relay ack NOTE` records human acknowledgement.

## Limits and next work

Recovery is verified by loading a durable Running snapshot into a separate engine, not by forcibly terminating the user's process or simulating power loss. Existing persistence tests cover database rollback and backup recovery. These checks establish service-level role isolation, not an OS sandbox for trusted project code. No physical speaker test is part of 5.7. MAXIS does not yet interpret TRX counts into its returned summary; the verification reads the actual artifact directly. Model-based planning, autonomous approval, external messaging and broader specialist powers are not introduced. Next: **6.1 Shared local AI**.
