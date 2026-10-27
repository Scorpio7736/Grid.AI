# Project GRID — 6.5 Intelligence Verification

Completed 2026-10-27. This milestone verifies the existing intelligence features and adds repeatable diagnostics; it does not implement the planned 6.6 unified `talk` workflow.

## Results

- Full Release regression suite: **847 passed** (14 Core, 157 Simulation, 99 Persistence, 577 Terminal).
- Focused intelligence suite: **171 passed**, selected with `Phase=6.5`.
- Bundled `qwen3:4b` diagnostic: **10 of 10 checks passed**. The captured run uses UTC timestamps; see the [saved report](verification/6.5-bundled-model-report.md).
- No database migration or additional runtime dependency. Existing schema remains v23.

## Coverage

| Checklist requirement | Verification |
| --- | --- |
| Grounded factual answers | GroundedDialogueTests and live model answering the known synthetic district count; detailed transcript retains captured facts and sources. |
| Hallucinated state and unsupported assertions | IntelligenceVerificationTests injects false counts and claims of execution; trusted evidence remains separate, world/tasks remain unchanged, and generated history is labeled untrusted. |
| Uploaded prompt injection | Three adversarial uploaded-file cases, plus live document containing hostile instructions alongside a known fact. |
| Model-generated action permissions | Forged approval fields and unsupported destructive capabilities are rejected; existing assistance tests cover the separately approved task workflow. |
| Timeout and offline fallback | Real SharedLocalInference with controlled timeout/offline backends through the command pipeline; grounded fallback and no tasks. LocalInferenceTests covers queue/deadline behavior. |
| Context/provenance retention and removal | Imported filename/hash/time survive SQLite restore; revoke removes agent access, removal survives restore. ConversationMemoryTests covers recall scopes and forgetting. |
| File approvals and rollback | WorkspaceChangeTests covers approval, hashes, paths, backups, stale changes and rollback; live model produces a draft that cannot apply before approval, then applies and rolls back to original bytes. |
| Advice-only and approved editing demonstrations | Live synthetic document analysis and edit workflow; assistance does not create tasks or enter conversation recall. |

`IntelligenceVerificationTests.cs` adds nine regression cases. Six existing test classes also carry the phase category, including ScreenTests for synthetic screen-context boundaries.

## Repeat the checks

Run these from PowerShell in the repository, not at the `grid>` prompt:

```powershell
dotnet test Grid7736.slnx -c Release
dotnet test Grid.Terminal.Tests/Grid.Terminal.Tests.csproj -c Release --filter "Phase=6.5"
.\Test-GridPortable.ps1 -PackageRoot .\portable\GRID7736-standalone -IntelligenceOnly
```

The portable build now includes `Test-GridPortable.ps1` in its root. From that root:

```powershell
.\Test-GridPortable.ps1 -PackageRoot . -IntelligenceOnly
```

Alternatively, `launch.cmd --verify-intelligence` runs the diagnostic directly, without the wrapper's restricted environment. The bundled runtime and model must already be present. The diagnostic never downloads them. The wrapper allows eight minutes and reports a failure if the process times out or returns a nonzero result. `-AssistanceOnly` and `-IntelligenceOnly` cannot be combined.

## Isolation and evidence

The diagnostic creates a new synthetic in-memory world and a temporary workspace under `GridIntelligenceVerification-<id>`. It does not load the user's saved world or capture the desktop. Its test identity approves only the synthetic edit. It exercises the ordinary command handlers, context scoping and editing safeguards.

It prints each result and saves `report.json` with runtime status, quality/boundary/workflow categories and observed model output. Failed checks return a nonzero exit code. Temporary reports, synthetic files and edit recovery artifacts remain available for inspection. The wrapper also retains its isolated verification-home directory in the package.

The wrapper excludes global PATH tools, the global .NET root and the user model cache. The successful run establishes bundled-runtime operation under those restrictions, not a clean-machine or firewall-isolation test.

## Limits and remaining work

Model accuracy and permission enforcement are different checks. Known-answer checks use bounded expected text; they are not a universal semantic evaluator. Freeform dialogue can still contain incorrect facts or false claims of execution. Detailed transcripts retain trusted observations separately from unverified model text; model text does not acquire permission to execute commands or edit files.

Passing these finite examples does not establish immunity to hallucinations or prompt injection. Existing manual desktop visual acceptance and actual user-monitor checks remain manual. The 6.6 unified `talk` workflow remains planned.
