# Project GRID — Shared local AI foundation (6.1)

## 6.1 follow-up verification — 2026-10-22

Fixed malformed numeric allocation responses in `ai status`: diagnostics return unavailable instead of throwing, and a subsequent valid response recovers. All **355 terminal tests passed**, including three new regression cases; Release build had zero warnings/errors. A fresh real local-model probe returned GRID LOCAL READY in **3,252 ms**. Earlier full-suite counts remain historical. See [task and AI manual acceptance steps](Project_GRID_Tasks_and_AI_Acceptance_Test.md). Agent dialogue integration remains planned for 6.2.


**Implemented and verified — 2026-10-22.** One optional shared inference queue and local Ollama adapter now exist. This is the service foundation: `talk` remains deterministic; grounded agent context/conversation integration is 6.2. No model output creates tasks, grants permissions or executes tools. Schema remains v20.

## Installed runtime and model

This PC already had Ollama 0.40.6. Installed `qwen3:4b` (2,497,293,931 bytes), digest `359d7dd4bcdab3d86b87d73ac27966f4dbb9f5efdfcc75d34a8764a09474fae7`, into the existing Ollama cache. Existing models were retained. Model tags can change on a future manual pull; this digest records the verified installation, not a runtime hash pin.

Qwen3-4B is Apache-2.0 licensed, verified against the [official model license](https://huggingface.co/Qwen/Qwen3-4B/blob/main/LICENSE) and the installed `ollama show qwen3:4b --license` output. Ollama uses the [MIT license](https://github.com/ollama/ollama/blob/main/LICENSE). Preserve applicable licenses/notices when distributing runtime or model files; neither is redistributed in this Git repository. Full bundled/portable packaging remains an 8.6 deliverable.

Hardware measured: Intel Core Ultra 7 265K (20 logical processors), 33,795,002,368 bytes RAM, RTX 5060 Ti with 8,151 MiB reported by nvidia-smi. WMI's 32-bit AdapterRAM value was not used as authoritative VRAM. An existing gpt-oss:20b cold test worked but took roughly 74.5 seconds; the smaller 4B model was selected for this 8 GB GPU. The initial 4B load/compile took 26.7 seconds; a later unloaded-model smoke took 3.232 seconds. These are observations, not latency guarantees.

## Startup and offline boundaries

Run `Start-GridLocalAI.ps1` from PowerShell in the repository (also copied beside the built terminal). It starts a hidden, GRID-dedicated Ollama server on **127.0.0.1:11435** with child-process settings:

- `OLLAMA_NO_CLOUD=1`
- `OLLAMA_NUM_PARALLEL=1`
- `OLLAMA_MAX_LOADED_MODELS=1`
- `OLLAMA_CONTEXT_LENGTH=4096`
- `OLLAMA_KEEP_ALIVE=1m`

The existing personal Ollama service on 11434 is unchanged. The dedicated server was started during this implementation; it remains idle between requests and the model expires after one minute. Runtime logs and a diagnostic PID file are in `%LOCALAPPDATA%\GRID\ai-runtime`. The script reports an already-running endpoint without replacing its configuration. If another server occupies 11435, verify its configuration before using it. GRID does not autostart/stop the external runtime on every launch; rerun the script after reboot or if `ai status` reports unavailable. No startup task/service or persistent environment-variable changes were installed. Closing GRID stops its queue, not the external Ollama daemon.

Model installation used `ollama pull qwen3:4b`; this was the network-dependent installation step. Subsequent application requests use only fixed loopback HTTP paths (`api/show`, `api/generate`, `api/version`, `api/tags`, `api/ps`), with proxy use and redirects disabled. The adapter rejects remote-model metadata and has no pull, cloud fallback or arbitrary endpoint path. The runtime log verified cloud disabled and the model runner's offline mode. This is application-local operation, not an OS firewall/network-isolation policy for Ollama or other processes. See [Ollama configuration](https://docs.ollama.com/faq).

Weights currently live in the user's external `.ollama/models` cache, not within GRID's managed 20 GB inventory. Current GRID copy/move commands do not bundle that cache or Ollama. A destination PC needs the runtime/model installed or provisioned separately; the core simulation remains usable without them. Self-contained Flash GRID and model-cache accounting/relocation remain planned in 8.6, not completed by 6.1.

## Shared queue and response contract

`ILocalInference` and `SharedLocalInference` live in Grid.Communication. The original host owns one shared service, available to later authorized agent consumers; no model process is created per citizen. A single FIFO worker permits one active request and eight waiting requests. Excess submissions receive an explicit unavailable/busy response instead of unbounded growth. Each request has a 120-second total deadline including queue wait. Cancellation/shutdown cancels backend work and settles queued requests. Cancelled queued work never reaches the model. Provider exceptions produce unavailable results without killing the queue worker.

Inputs are bounded to 8,192 characters. Requests use a 4,096-token context, a 512-output-token budget, temperature zero, non-streaming output, thinking disabled, and a one-minute keep-alive. Character bounds are not a token-budget guarantee; context construction and long-conversation budgeting belong to 6.2. The adapter caps each HTTP body at 1 MiB and final text at 8,192 characters; incomplete/token-limited/empty responses are refused. It ignores the separate thinking field and strips embedded Qwen thinking prefixes when present. Generated text is labeled unverified. These request controls follow the [Ollama generate API](https://docs.ollama.com/api/generate).

No prompts, model outputs or queue jobs are written to world persistence by this foundation. Queue state is transient and is not replayed on restart. No context files, transcripts or screen images are automatically sent to the model. Model/tool authority integration remains future work; existing task approvals are unaffected. Disabled/unavailable/busy/timeout results leave deterministic simulation, dialogue, persistence and task commands operational. Set `GRID_AI_DISABLED=1` before launching GRID to disable inference submissions; diagnostics may still inspect the local runtime.

## Terminal commands and measurements

```text
ai
    - ai status
      # USAGE: Inspect runtime version, installed model, queue state and loaded model/GPU allocation.
    - ai test
      # USAGE: Send a fixed text-only readiness prompt through the shared queue; display final output and timing.
    # EXAMPLE: ai test
```

Diagnostics require Master Control in the original host. Session changes cancel outstanding work and discard stale output. `ai test` is not an agent conversation, task proposal or arbitrary shell action. Its expected response is `GRID LOCAL READY`; output is still model-generated and not guaranteed exact.

`ai status` reports model and GPU allocation from [Ollama's running-model API](https://docs.ollama.com/api/ps), not GPU utilization percentage. Both commands sample working sets and CPU time across local `ollama` and `llama-server` processes. CPU figures are aggregate CPU milliseconds over the command wall-time interval, not a percentage. Measurements may include other Ollama users/servers and are labeled accordingly; process churn/access failures may produce unavailable measurements. This is diagnostic sampling, not exclusive per-task billing or a new GPU collector for VOLT.

Real C# probe on 2026-10-22 returned exactly `GRID LOCAL READY` in **3,232 ms**. Post-inference aggregate process working set was **953,753,600 bytes**; sampled CPU was **2,562 ms over 3,249 wall ms**. Ollama reported **3,180,247,121 bytes** model allocation and the same GPU allocation. Measurements are specific to this run; cold starts, contention and different hardware vary.

## Verification and acceptance

**620 tests passed:** 14 Core, 157 Simulation, 97 Persistence, 352 Terminal; zero failures/skips. Seventeen new tests verify queue serialization/order/capacity, cancellation, deadlines, disabled mode, shutdown, provider-failure recovery, output/HTTP bounds, local endpoint/options, rejection of remote models, thinking separation, malformed diagnostics and Master Control isolation. Test providers are deterministic; these suites do not require downloading or running a real model.

A separate opt-in real probe verifies the exact production adapter, shared queue and terminal diagnostic path without opening a world file:

```powershell
.\Start-GridLocalAI.ps1
dotnet run --project TestSupport/Grid.AI.Probe/Grid.AI.Probe.csproj -c Verification -- --local-ai
```

The probe refuses to run without `--local-ai`. It checks availability, exercises real generation, and reports before/after diagnostics. Full regression commands were `dotnet test <project>/<project>.csproj -c Verification --no-restore --verbosity minimal` serially for the four test projects. Release build `dotnet build Grid.Terminal/Grid.Terminal.csproj -c Release --no-restore --verbosity minimal` succeeded with zero warnings/errors. The hang found during development was fixed: malformed output now returns fallback and does not terminate the queue worker.

For user acceptance, restart the updated GRID, log in, run `ai status`, `ai test`, then `ai status` before the one-minute idle unload. Existing `connect MCP` / `talk status` still use deterministic dialogue; grounded model responses are the next module, 6.2. Do not claim screen vision, meetings, persistent AI memory or autonomous tool execution from this foundation.
