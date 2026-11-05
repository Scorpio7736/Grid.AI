# Model abstraction layer — 6.16

**Text-provider foundation implemented and verified — 2026-11-04.** The common contracts, Ollama adaptation, second local text provider, lifecycle serialization, streaming, cancellation and text resource-release checks are implemented. Actual image-provider load/unload and resource-release acceptance remains open, linked to candidate provisioning and evaluation in 6.17A. Phase 7 retains that requirement.

## Architecture and entry points

The new `Grid.AI` .NET project provides `ILocalModelProvider`, `LocalModel`, `ModelRequest`, `ModelResult`, `ModelChunk`, `ModelCapabilities` and `ModelResources`. `LocalModelSession` coordinates model/provider switching, generation, streaming and unloading through one resource lease. Generic request contracts carry context/output budgets, timeout, JSON/schema mode and text/image capability requirements.

`Grid.Communication` references `Grid.AI`. Its existing `OllamaLocalBackend` delegates generation to `OllamaModelProvider`, preserving current agent prompt framing, profile budgets, bounded truncation retries and final-answer handling. The existing shared FIFO queue still serves terminal chat, assistance and task/file drafting. Agent identity, permissions, approvals and world state remain with the established host services.

The common provider API accepts trusted host configuration. A single host must share one `LocalModelSession` for consumers using its managed lifecycle, and retain ownership of provider disposal. Direct provider calls are lower-level adapter operations. Model registry, role routing and user model-selection controls are work in 6.17 onward. The default interactive terminal continues using the pinned Qwen3 4B Ollama path. The second provider is exercised through the explicit verification entry point.

## Providers

| Provider | Model source | Lifecycle | Resource evidence |
|---|---|---|---|
| Ollama | Installed local model ID | Explicit preload, generation, streaming and unload with `api/ps` confirmation | Runtime-reported model/GPU allocation |
| llama.cpp CPU | Explicit installed local GGUF and llama-server executable | Starts an owned loopback server and exits that process to unload | Owned process working set and confirmed process exit |

Both expose text, streaming and structured-output capabilities. Image requests are checked before loading/generation and rejected by these text adapters. The common contracts represent image requests/results so an evaluated image provider can be added through the same boundary. Actual image generation and its resource-release measurements remain pending.

The llama.cpp adapter uses a fixed maximum context capacity of 16,384, one server slot, CPU execution, explicit offline mode, disabled web UI/context shifting and a zero RAM prompt-cache setting. It counts prompt tokens before generation and rejects requests whose prompt plus output budget exceeds the requested context. Runtime/model paths must be absolute local files, with network paths and links refused. Inherited `LLAMA_ARG_*` overrides are removed before launching with explicit settings.

The HTTP transport accepts IPv4 loopback root endpoints, disables proxies/redirects and bounds response bodies and stream frames. Ollama model metadata is checked for cloud-backed models. The abstraction provides no model-download operation. Provisioning is an explicit installation step. Provider contracts are based on the documented local APIs. [Ollama generate API](https://docs.ollama.com/api/generate), [llama.cpp server API](https://github.com/ggml-org/llama.cpp/tree/b12110/tools/server).

## Streaming, cancellation and recovery

The session serializes model switching and generation with a semaphore. A bounded 16-frame producer channel lets cancellation/shutdown stop the provider even when a stream consumer pauses. A consumer that abandons enumeration triggers cancellation and resource-lease release. Completion is emitted only after the provider stream ends consistently. EOF without completion and data after completion are rejected.

Streaming text is provisional. Callers must consume the stream to successful completion and inspect the finish reason before accepting content. Length-limited or cancelled output must not become an approved task or file. Existing interactive chat retains its completed-response and typewriter flow. This milestone adds API streaming, with terminal token-by-token presentation left to a future UI change.

Request deadlines include waiting for the session lease and are bounded to five minutes. Model switching has a two-minute deadline, explicit unload thirty seconds and cleanup fifteen seconds. Failed loading attempts invoke cleanup before another model can own the session. If cleanup fails, the session retains the provider/model reference for a later cleanup attempt. These bounds depend on providers honoring their cancellation contracts. Both implemented HTTP/process adapters use cancellation-aware operations.

## Provisioned test assets

The optional Windows CPU runtime was downloaded from the official llama.cpp `b12110` release and verified against its published archive SHA256.

```text
llama-b12110-bin-win-cpu-x64.zip
98901E327C252875ACCAF2D1BF364E10E868843B0B63468BAAD1AA945CDC5DA2
```

The runtime is provisioned locally under `.grid-runtime/llamacpp/b12110`, with a checksummed inventory at `.grid-runtime/llamacpp/assets.json`. The archive is 24,708,445 bytes. The existing pinned Qwen3 4B GGUF blob is reused by both providers. Preserve the upstream license and bundled LLVM/OpenMP notice in deployment. Application output includes the llama.cpp license, and existing portable transfers include the optional runtime directory when present. [Pinned release](https://github.com/ggml-org/llama.cpp/releases/tag/b12110).

A fresh source checkout requires explicit provisioning of the optional runtime before running the second-provider verifier. It expects the pinned directory above and fails with setup guidance if absent. Normal Ollama operation requires the existing runtime provisioning. The 128 GB total and 75 GB model budget migrations remain tracked in 6.27.

## Verification

The latest regression suites passed 1,090 tests, including 17 provider-focused cases. Coverage includes model switching during generation, failed-load cleanup/recovery, deadlines, cancelled/abandoned streams, disposal with a paused consumer, explicit completion, capability rejection, local endpoint/path validation and Ollama lifecycle payload/release handling. Existing inference/profile behavior tests also pass.

Live verification passed on both Ollama and llama.cpp using the same installed model and common session API. It checks structured generation, streamed completion, cancellation, a subsequent request and unload. Recorded values were 3,180,247,121 bytes of Ollama model allocation, including the reported GPU allocation, and 6,750,261,248 bytes of llama.cpp CPU process working set. These measure different resource categories and should be interpreted separately. Both reported release after unload. [Captured results](verification/6.16-local-providers.json).

The normal `--verify-assistance` path passed readiness, MCP chat, advice, captured-context analysis and typed task drafting after the adapter change. It uses a disposable world. Early freeform protocol probes exhausted deliberately small output budgets. The protocol acceptance probe now uses a strict JSON fixture, while the normal assistance probe covers the existing freeform path. These are compatibility checks rather than broad model-quality benchmarks.

## Try the current integration

Restart GRID and run the existing commands.

```text
ai status
ai test
grid -start
connect mcp -inline
talk Explain your role in GRID in one sentence.
```

For the developer verification, run the built terminal with `--verify-model-providers` after provisioning both runtimes. It creates its own inference runtimes and reports the result file path. Physical second-device and image-provider acceptance remain open in the master checklist.
