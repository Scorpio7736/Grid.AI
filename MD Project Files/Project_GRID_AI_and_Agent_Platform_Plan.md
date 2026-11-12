# GRID-AI and GRID-Agent development specification

**6.17A baseline evaluation — 2026-11-04.** Qwen3 4B passed automatic/CPU text smoke and lifecycle checks. Versioned evidence is tied to exact model/runtime/device/test settings; `ai registry acceptance` shows recorded results. 1,126 regression tests passed. Specialist models are not yet selected/downloaded; broad model, image, device, network-blocked and later-milestone acceptance remains open. [Results and remaining gates](Project_GRID_Model_Acceptance_Baseline.md).

**Model-handling planning update — 2026-11-04.** Added the supplied seven model candidates, hardware-aware Lite/Standard/Performance eligibility, cold/warm benchmarks, bounded fallback, deterministic delegation, validated artifact generation and offline manifests. Installed models have a 75 GB decimal sub-limit within the shared 128 GB Flash/PC target. All related acceptance remains required before Phase 7. [Model selection and handling plan](Project_GRID_Model_Selection_and_Handling_Plan.md).

**Planning addition — 2026-11-04.** The supplied 15-step roadmap is approved as planned work before Project GRID Phase 7. Existing milestone completion evidence retains its original scope. The user has supplied model candidates and handling requirements. Exact artifacts and measured promotion remain pending.

## Scope and sequence

The master checklist maps the supplied roadmap to 6.16–6.30 and adds 6.31 for integration, migration and the final acceptance gate. Every item is required before Phase 7, including training and fine-tuning. The attachment's suggestion to defer advanced capabilities is superseded by the user's request to add all of this before Phase 7. Development of GRID-Agent can proceed against stable GRID-AI contracts while later intelligence-engine work continues.

The five stages are foundation (6.16–6.19), knowledge/memory (6.20–6.21), authorization/tools/guardrails (6.22–6.24), reliability/performance/deployment (6.25–6.27), and model development/releases (6.28–6.30). Stage 3 is a joint execution gate. Existing approvals remain authoritative during migration, and migrated filesystem-write/command routes require the joint tests before activation. Existing 6.14 repairs, second-device acceptance and 6.15 document extraction remain dependencies.

## Project and ownership boundaries

| Planned solution area | Responsibility | Dependency direction |
|---|---|---|
| GRID-AI, proposed C# project Grid.AI | Local inference, model registry/lifecycle, token/context budgeting, embeddings, generic retrieval, caching, benchmarking, training and model-level safeguards | Provider contracts and local runtime adapters |
| GRID-Agent, proposed C# project Grid.Agent | Agent identities/personas/prompts, memory ownership, collection grants, tool selection, conversations, agent-to-agent communication, planning and approvals | GRID-AI contracts and typed application capability interfaces |
| GRID7736, existing Grid.Core / Grid.Simulation / Grid.Persistence / Grid.Terminal | World, districts, programs, task execution, persisted application state and UI | GRID-Agent services adapted to authoritative GRID host services |

The API contracts may initially run in-process in the .NET solution. A separate network service or additional server installation requires a justified design decision. Keep contracts transport-independent and make caller authentication explicit. Define ownership of each state field during migration so one authoritative writer manages each agent, conversation, memory and task record.

GRID-AI accepts generic system instructions and authenticated scope. GRID-Agent assembles agent-specific prompts and selects an approved model profile for each request. Model choice confers no permissions. Preserve existing MCP, MAXIS, JN, SENTINEL, VOLT and RELAY identities and role definitions. The example collection assignments in the supplied prose illustrate scoping and do not change those roles.

## Interfaces and enforcement

Define versioned request/response contracts carrying request ID, authenticated caller, authorized collection scope, model/profile/version, prompt version, resource budget, deadline and cancellation. Streaming must have explicit completion, cancellation and error states. Generation results carry provenance and model identity. Distinguish generated suggestions, retrieved information, verified tool results and simulation observations in the result contract and readable UI.

Generic retrieval contracts include `SearchAsync(query, collection, filters)`, `GenerateEmbeddingAsync(text)` and `IndexDocumentAsync(document)`. These are proposed contract shapes. GRID-Agent resolves accessible collections, and GRID-AI validates the caller's authorized scope again. Each retrieved chunk includes source identity, version, access scope and provenance. Enforce grants before retrieval, context assembly, caching and return of results.

GRID-AI enforces caller authentication/quotas, resource limits, input/output validation, malformed-response handling and runtime isolation. GRID-Agent enforces agent-specific access, memory isolation, tool permissions, approval workflows and injection defenses for retrieved/tool content. Trusted application services make authorization decisions and execute approved operations. Prompts and generated text cannot substitute for authorization.

Security, observability and evaluation apply across every stage. Log safe metadata and bounded tool traces. Every release requires measurable functionality, security, offline portability, quality and recovery acceptance. Components must support replacement, including replacing Ollama without redesigning the Agent API. Version prompts, manifests, adapters, configuration, migrations and known-good releases for recovery.

## Technology and model decisions

C#/.NET remains the host architecture. Ollama and the pinned Qwen3 4B implementation provide the initial reference adapter/model and measured baseline. The user has supplied specialist candidates in the model selection and handling plan. Record candidates with intended task, model/version/hash, license, quantization, runtime compatibility, context size, RAM/VRAM, storage, benchmark results and fallback. Final selections and promotion remain unchecked until evaluated and approved. Multiple agents may share one model where results support that choice.

Use SQLite-backed knowledge storage initially. Evaluate a local vector-search component and embedding/reranking models for packaging, licensing, resource use and offline operation before selection. Fine-tuning tooling may use a separately packaged local worker if required by the selected training stack. Define its typed boundary, supported hardware and dependency/license inventory before implementation. Provisioning/install steps may acquire assets explicitly. Supported runtime, retrieval, evaluation and provisioned training workflows must operate offline.

The user updated the storage target on 2026-11-04 to 128 GB decimal (128,000,000,000 bytes) for both Flash GRID and PC copies. Flash drives are the primary deployment, with installers and local-only PC copies also supported. This supersedes the earlier 20 GB planning constraint for both editions. Plan a per-installation total managed footprint covering application code, runtimes, models, extraction/retrieval assets, saved data, indexes, logs, backups and managed optional/development assets. Report category usage separately within that total. Check actual destination free space and filesystem limits, plus update/staging reserve, before installation, copying, model acquisition or updates. A nominal 128 GB drive may have less usable free capacity, so operations must fit the lower available-space bound. The installed-model sub-budget is 75,000,000,000 bytes within the 128 GB total. Define the other numeric sub-budgets and reserve policy during 6.27. Current code still enforces its existing 20 GB base ceiling until the planned migration is implemented and verified.

## Roadmap mapping

| Supplied step | Master item | Component | Ownership |
|---|---|---|---|
| 1 | 6.16 | Model abstraction layer | GRID-AI |
| 2 | 6.17 | Model registry and lifecycle | GRID-AI |
| 3 | 6.18 | Persona and prompt management | GRID-Agent |
| 4 | 6.19 | Observability and evaluation foundation | Both APIs and GRID7736 |
| 5 | 6.20 | RAG pipeline | GRID-AI retrieval with GRID-Agent scope ownership |
| 6 | 6.21 | Persistent memory | GRID-Agent ownership with generic GRID-AI services |
| 7 | 6.22 | Identity and authorization | Both APIs with GRID7736 authoritative host checks |
| 8 | 6.23 | Controlled tool gateway | GRID-Agent routing and GRID7736 execution |
| 9 | 6.24 | Layered guardrails | Both APIs and execution adapters |
| 10 | 6.25 | Resilience and recovery | GRID-AI and GRID-Agent |
| 11 | 6.26 | Performance optimization | GRID-AI with scope-aware GRID-Agent requests |
| 12 | 6.27 | Offline and portable deployment | All three layers |
| 13 | 6.28 | Training data pipeline | GRID-AI development tooling |
| 14 | 6.29 | Model fine-tuning | GRID-AI development tooling |
| 15 | 6.30 | Continuous quality and release management | All three layers |

## Detailed tasks and acceptance

### 6.16 Model abstraction layer

- [x] Define provider-independent loading, unloading, switching, generation, streaming, cancellation, timeout and concurrency contracts.
- [x] Adapt the current Ollama provider and exercise a second compatible local provider through the same API.
- [x] Verify provider contract tests, cancellation during streaming, serialized resource ownership and unchanged GRID chat entry points.

- [x] Implement capability-aware contracts and verify text-provider load/unload resource release
- [ ] Verify actual image-provider lifecycle/resource release after provisioning

**2026-11-04 implementation.** Grid.AI text-provider contracts, Ollama integration and llama.cpp live verification are implemented. Image-provider resource acceptance remains open. [Evidence](Project_GRID_Model_Abstraction_Implementation.md).

### 6.17 Model registry and lifecycle

- [x] Version manifests with hashes, model/runtime compatibility, license metadata, quantization, resource needs and rollback references.
- [x] Track candidate specialist models awaiting user selection and share identical weights across agent profiles.
- [x] Verify corrupt/missing assets, failed updates, rollback, multiple compatible models and measured storage accounting.

- [ ] Register the supplied Qwen3 4B/8B, Qwen2.5-Coder 3B/7B, Qwen3-Coder 30B-A3B, SDXL and FLUX.2 Klein candidates with exact revisions, licenses, quantizations, tokenizers/templates and runtime compatibility.
- [x] Implement registry profile eligibility contracts and persistent force-Lite (restart required), plus OS/RAM/disk/engine-file and optional NVIDIA VRAM diagnostics
- [ ] Complete supported GPU/backend discovery and device-benchmarked automatic Lite/Standard/Performance profile integration; unsupported readings remain unknown

**Completion evidence — 2026-11-04.** 1,109 solution tests passed, including 19 registry/device tests. Real pinned baseline verification passed for 7 files / 2,497,295,812 bytes. Candidate metadata is not installation or benchmark acceptance; runtime chat remains on the pinned baseline. [Implementation, commands, recovery and remaining gates](Project_GRID_Model_Registry_Implementation.md).

### 6.18 Persona and prompt management

- [x] Version agent identities, personalities, tone, system prompts, templates and instruction hierarchy.
- [x] Give GRID-AI generic system-instruction and prompt-version fields, with agent-specific content assembled by GRID-Agent.
- [x] Verify stable identities, serious/helpful tone, prompt rollback, request scoping and isolation between agents.

**Completion evidence — 2026-11-05.** Grid.Agent owns versioned persona assembly and rollback; Grid.AI carries generic system/version/template fields. 1,139 regression tests and 10 live persona checks passed. Normal chat/context/task drafting passed. Host authorization remains authoritative. [Implementation, controls, evidence and limitations](Project_GRID_Persona_and_Prompt_Management.md).

### 6.19 Observability and evaluation foundation

- [x] Define structured request/tool traces, model/prompt versions, error categories, latency, throughput, CPU/GPU and memory metrics.
- [x] Create versioned benchmark datasets and record the existing Qwen3 4B baseline and measurable release thresholds.
- [x] Verify redaction, bounded retention, diagnostic usefulness and repeatable evaluations without exposing private content.

- [ ] Benchmark cold and warm text/image jobs per candidate and device, including first-token latency, tokens/s, peak memory, schema/tool selection, completion, image dimensions and image validity.


**Completion evidence — 2026-11-05.** 1,151 current-suite tests passed. Content-free bounded diagnostics, separate task audit projections, model/persona metadata, typed failure categories and retry counters are implemented. Version-2 Qwen3 baseline benchmarks passed in automatic/CPU modes with explicit latency/allocation thresholds and labeled resource sampling. Image candidates and exclusive GPU utilization remain pending. [Commands, evidence and limits](Project_GRID_Observability_and_Evaluation.md).

### 6.20 RAG pipeline

- [x] Implement bounded captured-document ingestion/chunking, lexical feature embeddings, hybrid keyword/vector ranking, citations and explicit snapshot freshness.
- [x] Start with SQLite-backed derived storage and a packaged managed exact vector scan using the existing licensed SQLite dependency stack.
- [x] Verify source identity/version/provenance on every chunk, authorized collection filters, revocation, stale-index replacement and offline PDF/DOCX integration from 6.15.
- [ ] Select/provision/evaluate learned local semantic embeddings against this lexical baseline; no embedding model has been selected or downloaded.

**Completion evidence — 2026-11-05.** The lexical foundation passed 1,165 current-suite tests and a live bundled-model deep-document answer/revocation check. `context search` and scoped assistance retrieve captured excerpts with versioned citations. Disposable SQLite indexes rebuild from current grants and saved captures; original files are not reopened. Learned semantic relevance and physical second-device acceptance remain pending. [Implementation, evidence and limits](Project_GRID_Retrieval_Implementation.md).

### 6.21 Persistent memory

- [x] Define short-term context, long-term memory, summarization, retention, correction and deletion per agent/project.
- [x] Migrate existing conversations, pinned memories, automatic recall and explicitly retained document summaries with their grants and provenance.
- [x] Verify restart/copy recall, expiry, corrections, isolation, deletion semantics and revoked-source handling.

**Completion evidence — 2026-11-05.** Grid.Agent owns persistent memory records, visibility/relevance and conversation-scope policy; Grid.AI supplies generic lexical recall/extractive-summary helpers. Existing host authorization, saved conversation envelopes, JSON shape, grants/provenance and schema v24 are preserved. 1,180 tests and six bundled-model copied-database checks passed. No temporary raw document was retained and revoked/unshared recipients could not recall its approved summary. [Implementation, compatibility and limits](Project_GRID_Agent_Memory_Migration.md).

### 6.22 Identity and authorization

- [x] Establish authenticated host admission, bounded per-owner quotas, least-privilege inference capabilities and content-free audit for ordinary chat and assistance routes.
- [x] Carry user, caller, agent, project, conversation and world/session identity through host-built trusted request context.
- [x] Migrate meeting-round, screen, file-draft and diagnostic inference routes to the shared authorization gate and request quotas.
- [x] Verify denied calls, forgery, stale sessions and scope revocation jointly with 6.23–6.24. Current host-route acceptance passed on 2026-11-12, with evidence in the layered guardrails reference.

**Initial foundation verification — 2026-11-05.** 915 terminal tests passed. Grid.AI scope data is not a credential; Grid.Agent admission requires live host validation, limits two concurrent/60 starts per minute per world owner and keeps bounded content-free audit. Host reply guards and existing action approvals remain authoritative. At that checkpoint, additional route migration and joint action gates remained open. The route completion evidence follows below. [Implementation and limits](Project_GRID_Request_Authorization.md).

**Route completion verification — 2026-11-05.** All terminal inference routes now share per-owner admission limits. Meeting, screen, file-draft and diagnostic calls validate live host scope and discard revoked results. Master Control can inspect the latest admission records with `ai authorization`. Current suites total 1,191 passing tests, including cross-route quota enforcement and diagnostic replay/logout checks. The joint action gate below remains scheduled with 6.23–6.24. [Details and test commands](Project_GRID_Request_Authorization.md).

### 6.23 Controlled tool gateway

- [x] Define typed file read/write, command and simulation requests with path validation, execution isolation and approval requirements.
- [x] Route execution through the existing GRID task broker and approved file-change services, using recorded receipts for results.
- [x] Verify exact approval binding, concurrent changes, restart/retry behavior, denied paths and current authorization/guardrail enforcement through the gateway. Broader 6.24 acceptance remains tracked in its own section.

- [x] Enforce delegation allowlists, scoped paths/data, depth/cycle limits, per-profile job limits, budgets/cancellation and a parent/child evidence ledger through trusted .NET policy.
- [x] Add scoped deterministic Office/PDF generators and image-output validation with staging, format/template checks, visual QA and required overwrite approval.

**6.23 implementation verification — 2026-11-07.** Typed tool requests use the existing host task/file services. MCP delegation has a one-level allowlist, eight-child maximum, one active job and a ten-minute dispatch budget. `mcp ledger` exposes saved child-task evidence. Reviewed DOCX/PDF report generation and strict PNG validation use hash-bound previews, guarded apply and durable rollback. All 1,202 solution tests passed. Native Word/PDF samples were rendered and visually checked. Joint 6.22–6.24 acceptance remains open for 6.24. [Supported formats, limits and tests](Project_GRID_Controlled_Tool_Gateway.md).

**6.23 gateway acceptance completed — 2026-11-12.** Eleven added regression cases cover typed task/file gateway forgery, stale sessions, denied paths, request-key binding, failed-save retry approval, generated-output tampering and revoked artifact approval. All 1,213 solution tests passed (14 Core, 157 Simulation, 99 Persistence, 943 Terminal), including existing concurrent-edit, persistence/recovery, malformed-proposal and authorization tests. Gateway acceptance is complete for the implemented formats and host routes. Broader layered-guardrail work remains under 6.24, and conversation-only acceptance remains under 6.31. [Evidence and limits](Project_GRID_Controlled_Tool_Gateway.md).

### 6.24 Layered guardrails

- [x] Validate inputs and structured outputs, filter sensitive data, isolate runtime requests and verify tool results.
- [x] Treat retrieved documents and tool outputs as untrusted content and enforce access in code at each boundary.
- [x] Run adversarial injection, malformed-response, unauthorized retrieval and tool-escalation tests jointly with 6.22–6.23.

- [x] Validate delegation proposals against strict schemas and trusted policy, including requiresApproval=false, hostile retrieved content and worker-response attempts to expand authority.

**6.24 completed — 2026-11-12.** Shared inference now withholds recognizable credential material and invalid control-character output. Strict proposal parsing rejects worker approval/scope expansion and uses content-free rejection messages. Format-repair retries recheck live authorization. All 1,225 solution tests passed, including twelve new cases and the joint 6.22–6.23 authorization/tool suites. Credential detection is pattern-based with documented false-positive and coverage limits. [Implementation and evidence](Project_GRID_Layered_Guardrails.md).

### 6.25 Resilience and recovery

- [x] Add bounded retries, circuit breakers, cancellation, crash recovery, graceful degradation and deterministic fallbacks.
- [x] Define safe retry classes, duplicate-action prevention and recovery of model/runtime failures with actionable diagnostics.
- [x] Verify disconnects, timeouts, runtime exits and interrupted writes while preserving recorded outcomes and pending approvals.

- [x] Return structured unavailable results with constraints and offline alternatives. Verify one CPU/Lite baseline fallback after unloading a failed GPU allocation.
- [ ] Verify fallback to eligible smaller specialist checkpoints and physical over-budget model recovery after candidates are installed.

**6.25 baseline recovery implemented — 2026-11-12.** The shared queue now uses a three-failure, thirty-second runtime cooldown and structured unavailable replies. Ollama server failures can trigger one verified-unload CPU/Lite attempt with the installed baseline inside the request deadline. All 1,230 tests passed, including five new recovery cases and existing interrupted-write/task/file recovery suites. Smaller specialist-model and physical resource-exhaustion acceptance remain pending installed candidates. [Recovery rules and evidence](Project_GRID_Resilience_and_Recovery.md).

### 6.26 Performance optimization

- [ ] Plan exact caching, scoped semantic caching, context optimization and request scheduling.
- [ ] Include model/prompt/source versions, authorized scope, revocation and expiry in cache policies, and monitor CPU/GPU and memory budgets.
- [ ] Benchmark supported hardware and prove isolation, stale-cache invalidation and compliance with recorded latency/resource thresholds.

- [ ] Choose the smallest installed model passing task acceptance within the eligible profile, recheck memory per request, default to one heavyweight inference, and prioritize Lite UI responsiveness.

### 6.27 Offline and portable deployment

- [ ] Package required dependencies, models and retrieval assets with reproducible builds and integrity inventories.
- [ ] Migrate the current 20 GB base enforcement to the shared 128,000,000,000-byte managed-installation target for Flash GRID and PC, with category budgets, actual-free-space checks and update reserve.
- [ ] Verify copied/transferred packages on another supported PC offline, including changed drive letters, CPU fallback, updates and rollback.

- [ ] Enforce a 75,000,000,000-byte installed-model sub-limit inside the 128,000,000,000-byte Flash/PC target, counting complete model dependencies and caches with free-space/reserve checks.
- [ ] Package all selected runtime/image dependencies in one checksummed and licensed manifest, use atomic optional installs/rollback, and disable implicit downloads, telemetry, auto-updates and cloud fallback.

### 6.28 Training data pipeline

- [ ] Build versioned dataset creation, validation, deduplication, labeling, license and privacy review.
- [ ] Define train/validation/test splits and contamination checks with explicit permission for using conversation or document data.
- [ ] Verify reproducible dataset manifests, provenance, exclusion/removal procedures and held-out evaluation integrity.

### 6.29 Model fine-tuning

- [ ] Plan LoRA/QLoRA workflows, recorded configurations/seeds, checkpoints, adapters, export and runtime compatibility.
- [ ] Select supported local training dependencies and hardware requirements, with separate development storage accounting.
- [ ] Demonstrate a repeatable permitted-data training run, adapter loading/export, baseline comparisons and rollback before Phase 7.

### 6.30 Continuous quality and release management

- [ ] Run functional, adversarial, regression, performance and compatibility suites over versioned models/prompts/adapters.
- [ ] Define promotion criteria, known-good release bundles, rollback and migration compatibility checks.
- [ ] Record candidate-versus-baseline evidence and reject failed candidates while preserving saved GRID data.

- [ ] Run all model-handling acceptance scenarios, including no-GPU Lite, 8 GB Standard eligibility, benchmark-gated 12 GB FLUX discovery, offline jobs, load/unload cycles, validated documents/code and injected failures.

## 6.31 Integration and migration

- [ ] Inventory the current inference, role-profile, communication, memory, context, task-approval and persistence contracts before extraction.
- [ ] Define the Grid.AI and Grid.Agent project references, typed interfaces, adapter boundaries and test projects. Resolve any cyclic dependencies before migration.
- [ ] Wrap the current implementation first, then migrate incrementally through existing terminal, attached-window and meeting entry points.
- [ ] Preserve agent IDs, world IDs, conversations, memory provenance/grants/expiry, workspaces, task records, approvals and recovery receipts with explicit schema/version handling.
- [ ] Assign one authoritative owner per state field and test that legacy/new paths cannot produce duplicate agents, independent histories or duplicate execution.
- [ ] Define backed-up migration and rollback procedures, including handling incompatible database versions and interrupted migrations. Revalidate pending approvals where scope or target identity changes.
- [ ] Record the existing reported AI failure and transfer acceptance status separately from platform planning. Obtain exact failing-instance diagnostics and verify the repair through normal entry points.
- [ ] Run all 12 acceptance checks below, record evidence and have the user review the release gate before starting Phase 7.

- [ ] Review model/profile benchmark evidence and the complete model-handling acceptance checklist alongside the existing production-readiness checks.


### Conversation-first acceptance — clarified 2026-11-07

Required before Phase 7. Connected agent chats and meetings interpret ordinary language for supported work. Users can request file operations, context retrieval, memory changes, task planning, delegation, progress, cancellation and results without terminal syntax or task IDs. The agent asks focused questions for missing information, explains the proposed work, obtains required approval in ordinary language and reports verified outcomes.

- [ ] Remove terminal-command dispatch and reserved command-name collisions from connected chats and meetings. Sentences beginning with agent names or words such as `create`, `tasks` and `context` must reach the conversation interpreter.
- [ ] Keep administrative and diagnostic commands available in the main terminal, with an explicit way to leave a conversation and return there.
- [ ] Resolve conversational references such as “approve that”, “cancel it” and “ask MAXIS to do it” against the current reviewed operation. Ask for clarification when multiple targets are possible. Bind approval to the exact operation, scope and content through the host gateway.
- [ ] Support named-agent and group requests in meetings, preserve shared context and private grants, and route work to the appropriate specialist with recorded results.
- [ ] Demonstrate end-to-end supported tasks entirely through ordinary language in inline chat, attached chat and meetings. Cover clarification, approval, revision, cancellation, failure and completion without requiring command syntax or IDs.

Current implementation supports selected conversational intents and retains explicit command routing. Full acceptance above remains planned. Unsupported requests receive an explanation of available capabilities. Completion claims must come from execution receipts.

## Production-readiness acceptance

- [ ] Local inference works without internet access.
- [ ] Multiple compatible models work through one API.
- [ ] Agent identities and memory scopes are isolated.
- [ ] Relevant documents are retrieved with verifiable provenance.
- [ ] Unauthorized file and tool operations are rejected.
- [ ] Model and runtime failures recover predictably.
- [ ] Diagnostics are useful and protect sensitive data.
- [ ] Defined latency, memory and storage budgets are met.
- [ ] Functional, adversarial and regression evaluations pass.
- [ ] Models can be updated and rolled back while preserving GRID data.
- [ ] Portable deployment passes on another supported PC.
- [ ] Fine-tuning and evaluation are repeatable.

Each completed gate must link to the implementation revision, model/prompt/dataset hashes, test commands and results, hardware/runtime details, measured resource/storage figures, failures and limitations, migration/rollback evidence and reviewer decision. Define thresholds before candidate evaluation. Historical test counts establish baselines for their milestones.

## Planning verification

All 15 supplied components, five engineering rules, 12 acceptance checks and the subsequent three-layer architecture clarification are represented here. The copied diagram's generated CSS is omitted and its component relationships are expressed in the ownership table and contracts. The master checklist and readiness plan include the new Phase 7 gate. This update records planned work, with completion boxes awaiting implementation and evidence.

Documentation verification on 2026-11-04 passed for unique sections 6.16–6.31, placement before Phase 7, the 15-component mapping, all 12 acceptance checks, the 128 GB target and local links in the platform plan, master checklist and readiness plan. The whitespace check passed. Runtime verification belongs to the future implementation milestones.
