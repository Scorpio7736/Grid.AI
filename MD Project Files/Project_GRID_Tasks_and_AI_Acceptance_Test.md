# Tasks and shared local AI — manual acceptance test

Run these tests in the updated terminal as Master Control in the original host window. Task commands require a saved world. These tests cover existing task execution and the 6.1 inference foundation; AI-driven conversations are now implemented in 6.2; see the [conversation acceptance test](Project_GRID_Grounded_Conversations_Implementation.md).

## Task test

```text
agents initialize
tasks list
mcp request "MAXIS: demo GRID task smoke test"
tasks list -MAXIS
```

The new plan should contain one MAXIS demonstration task in AwaitingApproval. Copy its task reference (not the plan reference), replacing TASK_ID below:

```text
tasks inspect TASK_ID
tasks approve TASK_ID
tasks list
```

Expect Queued after approval. Before dispatch, check that there are no older Queued tasks: `tasks run` executes the next queued task, not an ID-selected task. If other work is queued, use a separate test world or defer dispatch rather than unintentionally executing that work.

```text
tasks run
tasks inspect TASK_ID
mcp brief
```

Expect Completed, a recorded demonstration result and a corresponding MCP plan result. The demo does not build code or change host files; task/plan/audit records are saved. This routing is deterministic and does not need the AI runtime. Do not use bulk approval/clearing just to test one task.

## 6.1 local AI test

If the runtime is unavailable, start it from PowerShell:

```powershell
Set-Location 'C:\path\to\Grid.AI'
.\Start-GridLocalAI.ps1
```

Inside GRID:

```text
ai status
ai test
ai status
```

Expect endpoint 127.0.0.1:11435, model qwen3:4b and Installed: True. The first status may say idle. The test should display LOCAL MODEL OUTPUT with GRID LOCAL READY and elapsed time. Model output is not guaranteed byte-for-byte exact. Run the last status within one minute to see loaded model/GPU allocation. Allocation is not GPU utilization. No new tasks or conversations should be created by these commands.

Optional disabled-mode test: close GRID normally, set `$env:GRID_AI_DISABLED='1'` in PowerShell and launch GRID from that same shell. `ai test` should report disabled while `status` and ordinary task commands still work. After closing that instance, remove the temporary setting with `Remove-Item Env:GRID_AI_DISABLED` and relaunch. `ai status` may still inspect the local runtime while inference is disabled. Do not open a second owner of the same saved world for this test.

## Automated follow-up verification (2026-10-22)

All 355 terminal tests passed, including three new malformed-allocation cases (fractional, oversized integer and string values). Status now returns unavailable for those runtime responses and recovers on the next valid response. Release build passed with zero warnings/errors. The real production C# probe returned GRID LOCAL READY in 3,252 ms without opening a saved world. The earlier full four-project regression (620 tests before these three additions) is retained in the [6.1 implementation record](Project_GRID_Shared_Local_AI_Implementation.md).
