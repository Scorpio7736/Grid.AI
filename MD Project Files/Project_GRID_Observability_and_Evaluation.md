# Observability and evaluation foundation — 6.19

**Text diagnostics and evaluation foundation implemented — 2026-11-05.** Specialist/image evaluation remains pending model provisioning. These changes expose operational metadata without storing private inference content or changing task authorization.

## Request diagnostics

`Grid.Communication/InferenceDiagnostics.cs` defines structured, content-free inference records. The shared inference queue creates a request ID and reports it through `InferenceReply.RequestId`. Records include agent, request kind, queue/run milliseconds, attempt count, typed outcome and available model revision/persona version. Known provider counters are accumulated across retries; missing/invalid/overflowing counters remain unknown. Tokens per second uses provider-reported generation duration, not end-to-end latency.

Outcomes distinguish success, disabled/stopped services, a busy queue, caller cancellation, timeout, output/context limits, incomplete/invalid output, transport errors and other unavailable results. Requests cancelled while queued are recorded when the worker observes cancellation. A reply may therefore arrive before its cancellation record is visible. Invalid input rejected before enqueue is outside this request trace. The normal bounded queue, validation, deadlines and approval pipeline remain in place.

The record schema has no prompt, response, schema body, document, file path, raw exception message or task argument fields. Model metadata is supplied by the trusted backend and diagnostic agent/persona labels are constrained. Private content is excluded by construction rather than copied and subsequently scrubbed. These operational records can still reveal agent use and timing, so access is limited to Master Control in the original host terminal.

Retention is at most 256 records, in memory only. Entries older than 24 hours are pruned on buffer read/write; future-dated entries after a clock reversal are removed. Records disappear when the host exits. Clear removes currently recorded entries; in-flight requests may subsequently add their own completion records. No diagnostic log or conversation export is created automatically.

## Task/tool view

The detailed command projects at most 20 existing saved task records into a separate typed view: task/agent IDs, agent role, status, attempts and latest started/terminal audit timestamps. Request keys, capability arguments, paths, results, operator IDs and audit detail text are omitted. This projection reuses the existing authorized task-state reader and does not create or execute tools.

The task view is not a new audit store. Existing task retention/persistence rules still apply. Clearing inference diagnostics does not delete task history. Terminal audit timestamps are not guaranteed to measure executable runtime. No request-to-task correlation is invented; explicit parent/child orchestration linkage remains part of the later delegation ledger work.

## Terminal commands

```text
ai test
ai diagnostics
ai diagnostics details
ai diagnostics clear
```

The compact table shows the latest 20 inference records, queue/run time, retries and token rate. `details` returns schema-versioned JSON with full IDs, model/persona versions, typed outcomes and the task projection. Use `ai status` for the existing current runtime measurements and `ai registry acceptance` for recorded benchmark results. No active task is changed by these diagnostics.

## Versioned benchmark dataset and thresholds

The six baseline fixtures now live in the embedded, version-controlled `Grid.AI/Benchmarks/grid-text-smoke-v2.json` dataset. Embedding carries the dataset in fresh builds and portable app binaries. It covers cold/warm JSON output, a small code correction, supplied context, delegation choice and an instruction embedded in quoted context. It does not execute code or tasks.

The version-2 baseline regression gate requires all six exact answers and complete output, plus:

| Measurement | Limit |
|---|---:|
| Model-cold response including load | 30,000 ms |
| Each warm fixture | 10,000 ms |
| First text in warm stream | 5,000 ms |
| Reported model allocation, including sampled peak when available | 8,000,000,000 bytes |
| CPU-only GPU allocation, including sampled values | 0 bytes |

Streaming completion, cancellation, recovery and unload must also pass. These are explicit regression thresholds for this text baseline, not universal user-experience guarantees or specialist/image acceptance criteria. Existing manifest/runtime/device/configuration binding and evidence age checks remain. Version-1 reports are preserved as historical evidence; they do not automatically qualify under version 2.

Run the existing standalone `--evaluate-model-baseline` entry point to evaluate automatic and CPU-only modes. It performs no downloads, opens no saved world and does not promote a model. Ordinary startup does not run these benchmarks.

## CPU, GPU and memory measurements

The standalone benchmark now samples approximately every 200 ms plus collector time:

- Maximum observed Ollama/llama-server process-family working set and CPU deltas between samples. Process identity includes start time so recycled PIDs do not produce false continuous CPU totals.
- Maximum reported model and GPU allocation from the owned runtime's `api/ps` endpoint.

Process-family counters include unrelated Ollama/llama-server clients and are explicitly nonexclusive. Sampling can miss brief peaks and CPU used before the first observation. Runtime GPU allocation is not GPU utilization, and GPU utilization remains unknown. Request-table rows do not attribute those benchmark measurements to individual production requests. Cold timing does not flush operating-system file caches. First-text latency is a warm streaming measurement. Network isolation and physical low-RAM-device certification remain unverified.

## Completion evidence

The current solution suites passed **1,151 tests**: Core 14, Simulation 157, Persistence 99 and Terminal 881. Twelve new diagnostic/resource tests cover bounded retention, expiry, clear, sensitive-content exclusion, disabled/cancelled/timed-out requests, request IDs, version/retry counters, unknown/overflowing metrics, task projection, bundled thresholds and process-ID reuse.

Both live baseline modes passed all six fixtures and lifecycle requirements with the version-2 thresholds. Reports include exact timings, token counters and sampled resource measurements:

- [Automatic baseline](verification/6.19-qwen3-4b-automatic.json)
- [CPU-only baseline](verification/6.19-qwen3-4b-cpu.json)

Still open: selected specialist benchmarks, actual image generation/format/dimension checks, image memory/latency thresholds, exclusive GPU utilization attribution, physical representative-device and network-blocked runs, and richer orchestration correlation. Earlier completion history remains intact and Phase 7 continues to require the outstanding gates.
