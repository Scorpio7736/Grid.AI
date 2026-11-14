# GRID integration contracts and ownership - 6.31

## Scope

Contract baseline defined 2026-11-13 for the first task of 6.31. This reference records existing in-process APIs and their migration boundaries. It does not declare the full integration, conversation-first or production release gate complete. Existing source and saved-data formats remain authoritative.

## Solution dependencies

All production projects target .NET 10. Arrows below mean a permitted dependency, not a second state owner.

| Project | Direct project references |
| --- | --- |
| Grid.Core | None |
| Grid.AI | None |
| Grid.Agent | Grid.AI |
| Grid.Communication | Grid.AI, Grid.Agent |
| Grid.Simulation | Grid.Core, Grid.Communication, Grid.AI, Grid.Agent |
| Grid.Persistence | Grid.Core, Grid.Simulation, Grid.AI, Grid.Agent, Grid.Communication |
| Grid.Terminal | Grid.Simulation, Grid.Persistence, Grid.AI, Grid.Agent, Grid.Communication |

Core is the simulation domain model. AI is independently reusable and cannot reference the domain model, Agent, communication, persistence or terminal. Agent depends only on AI. Communication remains the compatibility facade for inference, conversation envelopes and role profiles. Simulation owns application policy and mutations; Persistence serializes the host's state; Terminal composes adapters and authenticates user interactions. Existing transitive Core access from Terminal remains permitted.

Simulation, Persistence and Terminal now declare their direct AI/Agent/communication contract dependencies explicitly where applicable. The graph has no cycles. Moving an API into Agent must not introduce a reverse reference to Communication or Simulation; move a generic DTO down first or inject a host callback. Do not copy the implementation into a second service.

The existing four test projects remain in Grid7736.slnx. Provider, policy and host integration tests currently run in Grid.Terminal.Tests; simulation and durable storage coverage remain in their respective test projects. Separate AI/Agent test projects can be extracted later without duplicating tests. IntegrationBoundaryTests checks compiled GRID assembly references against the layer allowlist, including detection of newly introduced host dependencies in AI/Agent.

## Stable integration surface

These are source-level, in-process contracts. There is no independently versioned public network protocol. Changes to required members, meaning, identity binding or serialized shape require compatibility review and caller tests; the baseline number in this document is not a runtime authentication mechanism.

| Surface and source | Contract to preserve | Caller and enforcement |
| --- | --- | --- |
| [ModelContracts](../Grid.AI/ModelContracts.cs) | ILocalModelProvider: LoadAsync, UnloadAsync, ResourcesAsync, GenerateAsync, StreamAsync and async disposal; LocalModel, ModelRequest, ModelResult, ModelChunk, ModelResources | Providers implement capability checks, budgets, cancellation and completion. Provider access alone grants no host authority. |
| [TrustedRequestContext](../Grid.AI/TrustedRequestContext.cs) | World/user/caller/agent/session/generation/conversation/project/capability identity | Construct only from authenticated host state. A supplied record is not a credential. |
| [RequestAuthority](../Grid.Agent/RequestAuthority.cs) | Acquire(context, hostAllows), disposable Lease, IsCurrent and audit | One authority per engine, shared across original and attached sessions. Host callbacks revalidate live identity/scope. Keep quotas and checks before and after inference. |
| [LocalInference](../Grid.Communication/LocalInference.cs) | ILocalInference and IInferenceBackend; InferenceReply availability, rejection, request identity and structured unavailable result | Compatibility facade used by existing terminal consumers. SharedLocalInference owns the bounded queue; OllamaLocalBackend adapts provider/persona work. Default interface fallbacks are not evidence that arbitrary adapters support strict schemas or scoped caching. |
| [CommunicationState](../Grid.Communication/CommunicationState.cs) | IGridCommunicationHub and saved Conversation/GridMessage/CommunicationState records | GridSimulationEngine implements the trusted hub. Terminal adapters authenticate; direct hub methods do not authenticate their caller. Preserve participant IDs, sequence and project/meeting scope. |
| [ConversationMemory](../Grid.Agent/ConversationMemory.cs), [MemoryPolicy](../Grid.Agent/MemoryPolicy.cs) | Memory records, grants, provenance, expiry, relevance and recall-scope comparison | Agent owns record/policy definitions. The host remains the authoritative writer, applies source grants and persists through CommunicationState. Policy visibility alone is not authentication. |
| [ToolContracts](../Grid.Agent/ToolContracts.cs), [host tools](../Grid.Simulation/GridSimulationEngine.Tools.cs) | TaskToolRequest, FileToolRequest, FileReadReceipt, ToolExecutionReceipt; ProposeTool, ReadFileTool, StageFileTool, ToolReceipts | Host methods require EditSessionReference and use existing task/file services. Staging or proposing is not execution approval. Keep exact target/content hashes and receipt-based outcomes. |
| [Retrieval](../Grid.AI/Retrieval.cs) | ILocalTextEmbedding, RetrievalDocument/Chunk/Hit and versioned citations | Terminal filters captured context using current grants; Persistence owns the disposable SQLite index. Generic retrieval does not grant permission to read a source path. |

AgentPersonas remains in Grid.Agent. AgentModelProfiles and the shared inference facade remain in Grid.Communication during incremental migration. Existing release catalog commands in Terminal operate on Grid.AI release metadata; selecting a development catalog entry does not yet activate a model in live agents.

## Authoritative state ownership

| State | Authoritative owner and storage boundary |
| --- | --- |
| World/program/agent IDs, registration, simulation clock and city | GridSimulationEngine and its validated Core state; persisted by Grid.Persistence. Model/provider adapters never create another world or agent registry. |
| Conversations, messages, project membership and recall cutoffs | Engine-owned CommunicationState. Original terminal, attached windows and meetings access the same engine. UI-selected conversation IDs are navigation state. |
| Memory content, source references, grants, corrections, expiry and revisions | Engine-owned saved memory collection using Grid.Agent records/policy. A shared DTO or retrieved copy is not a writable second history. |
| Authentication, sessions and approval validity | Existing host identity/session services and engine authorization. Runtime sessions/leases are not restored as valid authentication from saved data. |
| Task proposals, parent/child links, approvals, attempts, results and audit | Engine task broker with existing persistence barriers and recovery. Tool receipts are projections of those records, not a separate execution queue. |
| File drafts, previews, original/candidate hashes and recovery bytes | Existing host file-change services and durable recovery artifacts. Session review/approval remains tied to exact content and scope. |
| Inference queue, rate limits, diagnostics, cache and model allocations | Shared runtime services; transient state is recreated, not restored as approvals. Provider lifecycle owns allocation release. |
| Model manifests, persona settings and development release catalog | Existing managed configuration/registry stores. Preserve their distinct formats and rollback rules; do not conflate catalog promotion with host adoption. |
| Captured context and retrieval index | Host-approved captures are authoritative; the derived index is disposable and rebuilt from accessible captured versions. |

## Compatibility and migration rules

1. Preserve current schema v24, stable GUIDs, enum values, JSON property meanings and legacy defaults. This contract-definition task does not introduce a database migration.
2. Keep the original terminal, attached-window and meeting paths on the same host authority. Test all three before replacing their adapters. Never migrate by creating parallel conversations or task brokers.
3. Inventory each moving field, its writer, persisted representation, validators and readers before relocating it. Retain existing authorization and revision checks at the adapter boundary.
4. Before a future schema change, back up a validated saved world and its required recovery artifacts. Specify forward migration, unsupported-version refusal and rollback to the matching executable/data backup. Never ask an older executable to open an incompatible newer schema.
5. Revalidate pending work when device, session, scope, target or content changes. Failed/interrupted task execution cannot inherit a consumed approval. Avoid duplicate dispatch during compatibility transitions.
6. Preserve content-free diagnostics, source/version citations, cancellation and unavailable results. Generated replies cannot establish that a host action completed.

These rules define the next migration work; they do not substitute for migration/rollback execution evidence. Existing memory migration, gateway and persistence tests retain their documented scope.

## Verification and remaining work

Verification on 2026-11-13:

- `dotnet test Grid.Terminal.Tests/Grid.Terminal.Tests.csproj --no-restore --filter "FullyQualifiedName~IntegrationBoundaryTests|FullyQualifiedName~ModelProviderTests|FullyQualifiedName~AgentMemoryMigrationTests|FullyQualifiedName~RequestAuthorityTests|FullyQualifiedName~AuthorizationRouteTests" --verbosity minimal`: 42 passed, zero failed. This includes seven new architecture checks and existing provider, memory and authorization tests.
- `dotnet test Grid7736.slnx --no-restore --verbosity minimal`: 1,297 passed, 14 failed, zero skipped (Core 14 passed; Simulation 157 passed; Persistence 99 passed; Terminal 1,027 passed and 14 failed). All production projects and test projects compiled. The command returned exit code 1.
- Failures are in terminal conversation/voice/attached-window expectations affected by the existing routing edits. Examples: a `remember this conversation` utterance is now intercepted as memory rather than stored dialogue; connected `voice listen`, `tasks approve`, `screen options` and `memory scope` inputs no longer dispatch as terminal commands. Existing exit/typewriter expectations also conflict with the revised connected-input route. These five pre-existing runtime files were not changed by this task. No clean-baseline comparison was run, so this is diagnosis from the failing assertions and existing diff, not a claim of a measured baseline pass.
- Local links in this reference resolve and `git diff --check` passed. No database/schema/model changes were made and no live model or second-device acceptance was performed.

The first master-checklist task is complete. The full suite is not green; fix and verify conversation migration before claiming the broader integration acceptance.

The rest of 6.31 remains open: authoritative-owner migration acceptance, saved-world backup/rollback acceptance, normal/attached/meeting workflow acceptance, conversation-only routing, live model-release adoption, failing-instance diagnostics, production model/device evaluation and second-PC offline evidence. The five pre-existing modified terminal files are outside this contract-definition change and must be preserved.

The 2026-11-13 master checklist also requires terminal-first milestones 6.32-6.38 before Phase 7. GUI milestone 6.39 is optional and non-blocking. A stale "using the GUI" sentence in 6.38 conflicts with that explicit terminal-only acceptance and should not be used to add a GUI prerequisite.
