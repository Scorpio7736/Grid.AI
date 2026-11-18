# GRID resilience and recovery — 6.25

## Implemented baseline — 2026-11-12

The shared inference worker now opens a thirty-second recovery cooldown after three runtime transport/I/O failures or active inference deadlines. During cooldown queued requests return a structured `CircuitOpen` result. After cooldown the serialized worker attempts the next eligible request. A validated answer resets the failure count. Caller cancellation, expired queue entries and rejected model content do not count as runtime outages. The cooldown is shared across agent profiles using the same backend and is held in memory.

Every unavailable `InferenceReply` from the shared service includes `Unavailable` with a machine-readable code, a constraint description and offline alternatives. Existing display text remains available. Alternatives identify world inspection, granted captured context and reviewed host tools. These paths retain their ordinary permissions and approval requirements.

## Bounded baseline fallback

For Ollama HTTP 500 or 503 during a GPU-enabled request, the backend unloads the installed Qwen baseline and verifies allocation release through the provider before attempting one CPU/Lite generation. The CPU attempt uses at most 4,096 context tokens for ordinary requests, or 8,192 for file drafts. Oversized reduced-memory file input is refused. The same prompt, persona and structured-output schema accompany the attempt.

The whole operation, including unloading and fallback, shares a 120-second deadline for ordinary requests or a 300-second deadline for file drafting. Each generation tier retains the existing single retry for explicitly truncated output. This yields at most four generation attempts when truncation and a server failure both occur. Cancellation is checked throughout. An unsuccessful unload prevents fallback allocation. An already CPU-only configuration returns the failure directly.

The current fallback uses the installed baseline under a reduced-memory configuration. Smaller specialist checkpoints, image candidates and measured per-device memory acceptance remain pending model selection and evaluation. The consolidated model-handling checklist retains that requirement before Phase 7. Runtime process termination is represented by transport/I/O failure and actionable unavailable output. Recovery retries generation after the service becomes available. Automatic process relaunch remains owned by the existing runtime startup lifecycle.

## Retry and durability rules

Inference retries produce suggestions or drafts. Host execution requires its recorded approval and dispatch lifecycle. The task broker deduplicates identical request keys, rejects conflicting reuse, saves before invoking an adapter, and restores interrupted tasks with approval cleared. Failed or interrupted execution requires fresh approval before another attempt. Completed task outcomes and receipts survive restoration.

File operations retain exact before/after hashes, durable backup preparation, concurrent-change checks and guarded recovery. Partial persistence failures report uncertain durability. Existing rollback and explicit recovery paths preserve evidence for operator review. Read-only world/context tools remain usable when model inference is unavailable.

The worker relies on backend cancellation support to finish in-flight work. Current HTTP providers honor cancellation. A third-party backend that ignores cancellation can delay worker shutdown and must pass lifecycle acceptance before integration.

## Verification — 2026-11-12

All 1,230 tests passed, comprising Core 14, Simulation 157, Persistence 99 and Terminal 960. Five new cases verify circuit opening/recovery with an injected clock, rejection/cancellation behavior, successful CPU fallback, failed CPU fallback and failed-unload refusal. Controlled HTTP handlers assert unload and allocation verification happen before CPU generation.

The full run also includes actual killed-writer SQLite recovery, interrupted task restoration, failed-save approval resets, exact file recovery, timeout/cancellation, partial-output rejection and receipt preservation. Existing test fixtures isolate temporary files and synthetic requests from user data.

```text
dotnet test Grid.AI.slnx --no-restore --verbosity minimal
```

Physical model exhaustion and fallback between specialist checkpoints await installed candidates and device acceptance. Baseline HTTP failure injection verifies the implemented recovery path.
