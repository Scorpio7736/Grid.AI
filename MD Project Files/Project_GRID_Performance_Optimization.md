# GRID performance optimization — 6.26

## Implemented baseline — 2026-11-12

Connected-agent advice and captured-document analysis now use a bounded exact-response cache through the normal assistance route, including conversational requests that reach that route. The host attaches live authorization and source revisions. The shared queue validates that authorization before returning a hit and after inference. Structured proposals, file drafts, ordinary chat, screen requests and diagnostics continue through their existing generation paths.

Cache keys hash the complete prompt, agent profile, trusted world/user/caller/agent/session/generation/conversation/project/capability identity, activity generation, context revision and memory revision. The backend contributes its pinned model revision, assembled persona/template and configured memory/CPU settings. Changes to any keyed field require a fresh response. The normal host pipeline also validates scope after receiving the reply.

The cache retains at most 64 entries and 512 KiB of UTF-8 response content per inference service, plus bounded key/entry overhead. Entries expire after thirty seconds. Clock rollback expires affected entries. Cache data stays in process memory and ends with the service. Revoked source content can remain resident until expiry, while authorization and revised keys prevent reuse. Model output remains subject to the existing validation before caching. `ai diagnostics` reports `CacheHit` with zero model attempts.

## Scheduling and memory

The bounded FIFO queue retains one heavyweight inference at a time. Cached requests pass through the same queue and per-owner authorization quotas. Model work remains asynchronous so the terminal can show progress and handle cancellation. Existing retrieval budgets select captured excerpts within the request budget.

The normal terminal backend samples available physical RAM with Windows GlobalMemoryStatusEx immediately before each generation. Initial thresholds are an implementation choice pending device calibration. Below 1 GiB it returns an actionable unavailable result. Below 4 GiB it uses reduced context, including 8,192 tokens for file drafts. Explicit low-memory settings also apply. The pressure check alone preserves the configured GPU policy. A live force-Lite setting requests CPU execution. Unknown memory readings use configured policy and existing runtime failure handling.

These thresholds measure current free host memory. They are preliminary controls for the installed baseline. GPU allocation and existing CPU/process-family diagnostics remain available through the current observability tools. Device-specific scheduling needs measured model headroom, including the cost of already-loaded weights.

## Remaining acceptance and implementation plan

- [ ] Select and evaluate the specialist and embedding candidates, then select the smallest accepted installed artifact for the requested capability within current RAM/VRAM and profile limits.
- [ ] Add scoped semantic caching only after embedding-model and answer-equivalence evaluation. Its keys must include the same authorization/source versions, embedding revision and similarity policy. Action plans and writes must retain exact review/approval semantics.
- [ ] Benchmark the supported Flash/PC and Lite device matrix under competing load. Record cold/warm latency, CPU time, peak process memory and GPU allocation against the versioned benchmark thresholds.
- [ ] Calibrate memory thresholds and evaluate priority scheduling against starvation, cancellation and UI responsiveness tests.

The pinned Qwen baseline remains the available runtime model. Multi-model selection and semantic-cache acceptance depend on the pending artifacts and measured evaluations.

## Verification

The full solution passed 1,243 tests on 2026-11-12, comprising Core 14, Simulation 157, Persistence 99 and Terminal 973. Thirteen new cases cover cache hits, TTL, clock rollback, capacity, model/persona version changes, scope/source separation, revoked access through the normal connected-agent route and fresh memory measurements. The normal-route test verifies captured private content disappears from the subsequent prompt after grant revocation.

```text
dotnet test Grid7736.slnx --no-restore --verbosity quiet
Grid.Terminal.exe --verify-inference-cache
```

The live benchmark uses a synthetic captured fact and an isolated runtime without opening a saved world. Its predefined acceptance checks require the first answer within 120 seconds, ten identical cached answers, ten recorded hits and each hit within 500 milliseconds. It reports available RAM and runtime allocation alongside timings. Startup asset verification time is outside the first-answer timer.
