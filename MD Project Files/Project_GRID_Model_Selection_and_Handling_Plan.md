# GRID model selection and handling

**6.17 registry foundation — 2026-11-04.** Versioned model manifests, checksum verification, retained-revision rollback, shared-asset accounting, candidate tracking, persistent force-Lite and device diagnostics are implemented. 1,109 solution tests and live baseline inventory verification passed. Exact specialist artifacts, device benchmarks and automatic profile promotion remain pending. Running chat remains on the pinned Qwen3 baseline. [Implementation and commands](Project_GRID_Model_Registry_Implementation.md).

**6.16 text-provider implementation — 2026-11-04.** Grid.AI now provides common lifecycle, generation, streaming, timeout, cancellation and capability contracts. Existing Ollama chat uses the new adapter. A pinned optional llama.cpp CPU provider passed live checks using the existing Qwen weights. Current suites total 1,090 passing tests. Actual image-provider lifecycle/resource verification remains open, alongside model registry/profile acceptance and other Phase 7 gates. [Implementation and verification](Project_GRID_Model_Abstraction_Implementation.md).

**Version 1.0 — 2026-11-04. Planned requirements supplied by the user.** Implementation and device acceptance belong to mandatory pre-Phase-7 sections 6.16–6.31. The completed 6.13 Qwen3 4B implementation supplies the reference baseline. The candidates below require exact artifact selection, license review and measured acceptance before promotion.

## Storage and deployment

Both Flash GRID and local PC copies target 128 GB decimal (128,000,000,000 bytes) of total managed installation storage. Installed models have a nested cap of 75 GB decimal (75,000,000,000 bytes). This model allocation is included in the 128 GB total. Flash drives are the primary deployment, alongside installers and local-only PC copies.

Count every installed model asset, including weights, tokenizer/configuration files, image text encoders, VAE, adapters and retained model versions. Count physically shared assets once and account for additional physical copies and model caches. Report runtime libraries, dependency wheels/container images, retrieval assets, user data, logs, backups, development artifacts and staging in their appropriate categories and in the overall footprint. Apply both quota checks and actual destination free-space/reserve checks before installing optional models or updates. Define temporary update headroom explicitly. Use atomic installation and rollback for incomplete downloads or failed verification.

The supplied Standard-weight estimate is 16.65 GB. Record it as an unverified planning estimate until revisions, quantizations and the complete manifest have been selected and measured. Dependencies and caches add space. Current runtime enforcement still uses the historical 20 GB base ceiling and awaits the planned budget migration in 6.27.

## Candidate models and profiles

| Candidate | Intended evaluation | Profile guidance |
|---|---|---|
| Qwen3 4B | General text, document content and delegation planning | Lite planner default and existing reference baseline |
| Qwen3 8B | General text, document content and delegation planning | Standard/Performance planner default candidate |
| Qwen2.5-Coder 3B Instruct | Code generation, edits and tests | Evaluate as a smaller coding candidate |
| Qwen2.5-Coder 7B Instruct | Code generation, edits and tests | Evaluate as a larger coding candidate |
| Qwen3-Coder 30B-A3B Instruct | Multi-step coding delegation and complex tool orchestration | Performance upgrade candidate, requiring memory and task benchmarks |
| SDXL base 1.0 | Local image generation | Enable only for a compatible installed runtime and successful device benchmarks |
| FLUX.2 Klein 4B | Local image generation | Performance candidate requiring actual memory and image benchmarks |

Reuse the selected Qwen3 general model for delegation where it passes the task benchmark. Keep the model registry separate from agent definitions so sharing a planner does not duplicate coordinator weights. Final coding/image profile assignments, quantizations, revisions, latency thresholds and backend support must be recorded during evaluation.

Profiles describe capability eligibility. A no-GPU 16 GB RAM device is the supplied Lite acceptance scenario. A 32 GB RAM / 8 GB VRAM desktop is a Standard candidate when runtime support and smoke tests pass. A 32 GB RAM / 12 GB VRAM device is a discovery scenario for Performance evaluation. The attachment reports about 13 GB VRAM for the publisher's standard FLUX.2 Klein setup. Treat that figure as a supplied reference to verify against the pinned revision. A 12 GB discovery threshold cannot establish that the model fits. CPU/GPU offload requires measured memory and latency acceptance.

## Startup and request handling

1. Detect total/available RAM, GPU vendor, total/free VRAM, supported compute backends, OS, architecture, free disk and supported inference engines. Consider CUDA, ROCm, Metal, DirectML and CPU where the chosen runtime/OS supports them. Record unknown measurements explicitly. GPU brand alone cannot establish usable VRAM.
2. Read installed candidates from a local registry. Validate checksums, expected model architecture, quantization, license acknowledgment, runtime compatibility, tokenizer and chat template. Installation requires an explicit provisioning action.
3. Select the highest eligible profile using available RAM, available VRAM, backend support and measured smoke tests together. Persist a user override that can force Lite. Profile capability is an upper bound for request routing.
4. Benchmark installed candidates on the current device with cold and warm runs. Record text time to first token, tokens per second, peak RAM/VRAM, JSON-schema validity, tool-name selection and completion success. Record image generation latency, peak memory, actual dimensions and image validity. Cache benchmark evidence by device/runtime/model configuration and invalidate it after relevant changes.
5. For each authorized request, classify the task, recheck available memory and installed assets, filter candidates by profile/backend/license/latency, then choose the smallest model that passes that task's acceptance tests. Reserve resources before loading and cap context, output, time and memory usage.
6. On loading failure, exceeded memory/latency thresholds or rejected output, release resources and try an eligible lower-tier installed candidate within bounded retries. Preserve cancellation and validated operation state across fallback. Verify the result before committing an approved output.
7. Run one heavyweight inference at a time by default. Keep agent definitions resident while loading model weights on demand. Queue Lite jobs and prioritize terminal responsiveness. Logical job concurrency does not authorize simultaneous heavyweight model loading beyond the measured resource policy.
8. If no compliant candidate is available, return a structured `UNAVAILABLE` result containing the hardware/runtime constraint and a suggested available offline alternative. Include a clear unavailable outcome when no alternative is installed. Cloud fallback is disabled.

```text
request -> authorization/policy -> task classification
        -> available-memory and installed-model checks
        -> eligible profile/backend/license/latency candidates
        -> smallest model passing task acceptance
        -> execute -> validate output -> commit approved output
        -> failure -> release resources -> bounded lower-tier retry
```

## Delegation and ownership

The attachment calls for a deterministic GRID-AI .NET orchestrator. Within the approved three-layer architecture, GRID-Agent owns agent delegation policy and task orchestration, GRID-AI enforces inference scheduling/resource limits, and GRID7736's trusted host services authorize and execute application operations. This maps the supplied behavior onto the established ownership boundaries.

The model proposes delegation. Trusted .NET policy enforces the following requirements.

- Allowed child-agent types, tool allowlists, filesystem scopes and data classifications.
- Maximum delegation depth, proposed initially as 2, with cycle/loop detection.
- Active-job limits, proposed as Lite 1, Standard 1–2 and Performance benchmark-dependent. Final limits require recorded evaluation.
- Token/time budgets, cancellation propagation and parent/child task lifecycle.
- Explicit approval for irreversible, privileged or external actions, strict tool-call schemas and validation of every path. Existing GRID approval requirements also apply.
- Prompt-injection boundaries that keep retrieved documents and agent replies from expanding permissions.
- A delegation ledger containing parent/child task IDs, requested goal, tool calls, evidence, status, usage/cost and latency metrics. Local resource/token usage must be distinguished from any monetary cost.
- Worker results with evidence and validation outcome. Failed or uncertain work returns to the parent with that state intact.

The supplied proposal example is retained as a contract-design input.

```json
{
  "schemaVersion": 1,
  "taskId": "sample-001",
  "action": "delegate",
  "agentType": "coding",
  "goal": "Write unit tests for the parser",
  "inputs": ["approved/project/path"],
  "expectedOutputs": ["test files", "test report"],
  "requestedTools": ["read_project", "write_tests", "run_tests"],
  "maxSeconds": 120,
  "requiresApproval": false
}
```

Validate the proposal against a strict versioned schema and authenticated request context. Map agent types and tool names through the host's registered catalog. Resolve real paths and replace proposed limits with trusted policy limits. The host determines approval requirements even when `requiresApproval` is false. Preserve existing preview, exact approval and apply workflows.

## Documents, files and images

LLMs generate text, structured content and tool proposals. Generate binary documents through local deterministic libraries selected and licensed for the target format, such as Open XML SDK or ClosedXML for Office documents and an evaluated PDF library. Plan template validation and visual preview/QA alongside format validation.

Write generated files into a scoped staging area, verify their format and expected content, present the output for review and obtain required overwrite authorization before publication. Code changes require relevant test results before reporting verified completion. Image generation requires a supported image runtime and validation of decoded image content, dimensions and output format. All file operations pass through scoped host services.

## Offline packaging

Package model weights, tokenizer configurations, inference runtimes, image text encoders, VAE and required wheels/container images during explicit installation. Maintain one dependency manifest containing each file's size, checksum, exact model revision, quantization, license and provenance.

Enable offline runtime operation and disable telemetry, automatic model updates and cloud fallback. Validate representative jobs with networking disconnected or blocked and record evidence of outbound-connection checks. Include license acknowledgment and redistribution review for each pinned artifact, especially SDXL and alternate image checkpoints. Provider documentation and Qwen-Agent are evaluation references for compatibility. Dependency adoption requires an explicit architecture and packaging decision.

## Required acceptance before Phase 7

- [ ] Detect a no-GPU 16 GB RAM laptop and select Lite through installed candidates, with explicit provisioning required for additional models or SDXL.
- [ ] Detect a 32 GB RAM / 8 GB VRAM desktop and select Standard only when runtimes and smoke tests pass.
- [ ] Require measured memory acceptance for FLUX.2 Klein on a 32 GB RAM / 12 GB VRAM device.
- [ ] Complete a representative offline job with each selected model and verify outbound-connection behavior.
- [ ] Repeatedly load/unload text and image models, recording memory reclamation and latency.
- [ ] Reject malformed delegation JSON, unauthorized paths, circular delegation, unapproved destructive tools and retrieved-content attempts to override policy.
- [ ] Verify generated documents open correctly and code modifications have relevant passing tests before reporting verified completion.
- [ ] Simulate missing checkpoints, out-of-memory, corrupt weights, unsupported backends and cancellation, then verify bounded fallback.
- [ ] Verify the 75 GB nested model cap and 128 GB total target on Flash and PC copies, including shared assets, caches, atomic failed installs and nearly-full destinations.

## Supplied model and framework references

These URLs are retained from the user's version 1.0 document. Exact revisions, licenses, dependency sizes and hardware claims await release evaluation.

- [Qwen3 4B GGUF](https://huggingface.co/Qwen/Qwen3-4B-GGUF)
- [Qwen3 8B GGUF](https://huggingface.co/Qwen/Qwen3-8B-GGUF)
- [Qwen2.5-Coder 3B Instruct GGUF](https://huggingface.co/Qwen/Qwen2.5-Coder-3B-Instruct-GGUF)
- [Qwen2.5-Coder 7B Instruct GGUF](https://huggingface.co/Qwen/Qwen2.5-Coder-7B-Instruct-GGUF)
- [Qwen3-Coder 30B-A3B Instruct GGUF](https://huggingface.co/ggml-org/Qwen3-Coder-30B-A3B-Instruct-GGUF)
- [SDXL base 1.0](https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0)
- [FLUX.2 Klein 4B](https://huggingface.co/black-forest-labs/FLUX.2-klein-4B)
- [Qwen function calling](https://github.com/QwenLM/Qwen3/blob/main/docs/source/framework/function_call.md)
- [Qwen-Agent](https://github.com/QwenLM/Qwen-Agent)

## Checklist mapping

| Master item | Added requirement |
|---|---|
| 6.16–6.17 | Provider capabilities, installed registry, profiles, overrides, load/unload, artifact compatibility |
| 6.19 | Cold/warm device benchmarks and text/image acceptance metrics |
| 6.22–6.24 | Deterministic delegation, bounded jobs/depth, approved output generators and strict validation |
| 6.25–6.26 | Resource-aware scheduling, bounded downgrade/fallback and structured unavailable results |
| 6.27 | Complete offline manifests and nested 75 GB / 128 GB budgets |
| 6.30–6.31 | Candidate promotion, all device/failure scenarios and migration acceptance |

**Documentation verification — 2026-11-04.** The seven supplied model candidates, nine reference links, seven startup/handling requirements, delegation proposal and safeguards, artifact-generation workflow, offline packaging and eight supplied acceptance scenarios are represented. The additional budget acceptance check ties the model sub-limit to the existing installation target. Implementation and measured candidate results remain pending.
