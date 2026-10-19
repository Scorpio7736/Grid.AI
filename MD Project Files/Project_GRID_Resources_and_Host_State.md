# Project GRID - Resources and host state

## Phase 2 verification update - 2026-10-19

Section 2.5 is complete for the current implementation. **231 tests passed** (8 Core, 106 Simulation, 64 Persistence, 53 Terminal), with zero build warnings/errors. Eight new isolated cases verify headless terminal save/reopen/recovery, populated older-schema restoration, no offline catch-up, invalid/missing-file preservation and abrupt writer termination with SQLite journal recovery. A test-only helper under TestSupport is built by the persistence tests; production projects do not reference it.

See [Phase 2 Verification](Project_GRID_Phase_2_Verification.md) for evidence and limits. Earlier test counts remain historical. Process-crash verification does not claim physical power-loss coverage; pending application energy/classification/lifecycle work and hardware-generated cities remain unimplemented. No production behavior was changed for 2.5.


**2.4 telemetry and policy foundation implemented and verified - 2026-10-19.** Read-only host measurements and a tested resource-condition policy now exist. Applying that policy to discovered application residents and replacing their placeholder energy behavior depends on the still-planned 2.7 classification/lifecycle retrofit. Current saved Genesis citizens remain fictional and retain their old energy rules; they must not be presented as measured applications.

## Architecture and measurement contract

`IHostTelemetryProvider` supplies observations. `HostTelemetryMonitor` takes an initial sample through terminal startup and samples every two seconds independently of simulation ticks. `GridSimulationEngine` validates and copies observations under its lock; commands and live view read detached snapshots. The Windows adapter lives in Grid.Terminal; Grid.Simulation's contracts and policy have no Windows API dependency. Tests use a fake provider and controllable time.

Each reading has a metric/category, scope, unit, nullable value, status, observed UTC timestamp, source and explanatory detail. Status is Available, WarmingUp, Unsupported, Unavailable or Stale. Only Available permits a numeric value. Values must be finite/nonnegative; percentages are bounded to 0–100 and booleans to 0/1. Duplicate metric/scope pairs and batches above 512 readings are rejected before replacing current state. Different scopes must not be silently aggregated.

Available values older than ten seconds, or dated in the future relative to the current clock, are exposed as Stale with no numeric value. Unknown does not mean zero. Expected I/O/access failures invalidate current values; an unexpected adapter defect ends monitoring through the host's visible error/shutdown path. The host cancels and awaits monitoring before its final save. Sampling does not advance the clock, mutate citizen energy or fictional district resources, allocate Windows resources, or generate a durable event for every sample.

Telemetry is transient: it is not written into world snapshots/backups or restored as a current observation. A restarted world obtains fresh host samples. This separates current host conditions from GRID-REQ-004 creation provenance and from saved fictional resource balances.

## Windows collector coverage

| Measurement | Units/scope | Current behavior |
|---|---|---|
| CPU available fraction | Percent; `cpu:primary-group` | Idle-time delta divided by kernel-plus-user delta; first sample is WarmingUp. This is observed idle headroom, not guaranteed future processing capacity. |
| RAM available and capacity | Bytes; host | Available physical memory and total physical memory. Available includes reclaimable standby memory; utilization alone is not a health/failure claim. |
| Storage available and capacity | Bytes; Windows system volume | Space available to the current user and total volume size. Other drives are not included by this collector. |
| Network receive/send | Bytes/second; individual interface | Counter deltas using monotonic elapsed time; first sample, resets, disconnected links or gaps over ten seconds yield unknown rates. Interfaces are not summed, avoiding false totals from overlapping virtual/physical interfaces. |
| Network connected | Boolean; individual interface | Local link up/down, not proof of Internet reachability or an application's network requirement. No interfaces yields unknown coverage, not a fabricated connected state. |
| GPU available / VRAM available | Percent / bytes | Explicitly Unsupported; no collector configured. |
| Storage read/write throughput | Bytes/second | Explicitly Unsupported; no collector configured. |

Network collection excludes loopback and caps work at the first 32 interfaces in stable ID order; additional interfaces produce an explicit incomplete-coverage observation. Only counters/link status are read—no packet contents, endpoints, files, application command lines or process control. Non-Windows execution reports unsupported metrics. CPU sampling covers only the calling thread's primary processor group on machines with more than 64 processors; the scope is labeled rather than advertised as an all-group total.

The underlying APIs were checked against [GetSystemTimes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getsystemtimes) and [MEMORYSTATUSEX](https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/ns-sysinfoapi-memorystatusex). `GetSystemTimes` kernel time includes idle time; the calculation does not add idle twice.

## Resource-condition policy v1

The implemented `ResourceConditionPolicy` is an explicit headroom interpretation for **additional work**, not a measured health meter and not a deduction of an application's minimum requirements from current usage. Its caller supplies a policy instance per subject, fresh observations and requirements with metric, exact scope, positive required amount and provenance (for example, a configured task budget). Requirement units come from the metric. No requirements means unknown.

Supported requirements concern CPU/GPU available percent, available RAM/VRAM/storage bytes and local connectivity. Requirements for observed throughput or total capacity are rejected: those values are not interchangeable with free capacity. Network is omitted for work that does not require it; a connectivity requirement must be exactly 1. Percent requirements cannot exceed 100. Duplicate or unsourced requirements are invalid.

For each required category:

`category score = 100 × clamp(available / required additional headroom, 0, 1)`

The target score is the minimum category score, so a well-supplied category cannot hide another required category's shortage. A first valid observation uses the target directly. Subsequent distinct observations use `previous + 0.25 × (target − previous)`, bounded to 0–100, for both deterioration and recovery. Repeated evaluation of the same sample does not advance smoothing. Changing requirements resets smoothing. Missing, ambiguous, unavailable, unsupported, stale, future-dated or out-of-order required observations return unknown and reset prior smoothing. Sample frequency therefore matters; the default collector cadence is two seconds.

This API is tested but is not automatically assigned to existing citizens. The terminal truthfully reports application energy as unknown because application discovery and configured application requirements do not yet exist. Section 2.7 must bind a policy instance and provenance to each appropriate resident, define the user-facing requirement configuration, and stop applying arbitrary Genesis drain to those application residents. No low score may stop, suspend, throttle or relaunch host applications. Such actions require a separate explicitly authorized task capability.

## Terminal use

`resources` displays raw observations, units, statuses, scopes, timestamps and sources, including unsupported metrics. `live` shows a compact CPU/RAM summary on its existing status line; full scope/source details remain available through `resources`. Readouts update even while the simulation is paused; viewing them never advances simulation time. Raw measurements are not smoothed or falsified for appearance—the optional condition score is the smoothed interpretation.

## Verification and remaining work

- `dotnet test Grid7736.slnx --no-restore --verbosity minimal`: **223 passed**, zero failed/skipped (8 Core, 106 Simulation, 56 Persistence, 53 Terminal).
- `dotnet build Grid7736.slnx --no-restore --verbosity minimal`: zero warnings/errors.
- Fifteen added cases cover deterministic fake-provider capture, defensive copying, stale/future/unknown values, invalid metric/unit bounds, provenance and duplicate requirements, weakest-category scoring, smoothing/repeated samples, requirement changes, unavailable-provider handling, cancellation, terminal formatting and monitor failure/shutdown.
- A hidden disposable terminal smoke on Windows read real RAM values, observed CPU transition from WarmingUp to Available, reported GPU as Unsupported, sampled while paused and left the simulation at tick zero. No default saved world was opened or modified.

Application grouping/discovery, per-application usage, resident persistence filtering and energy binding remain 2.7 work. GPU/VRAM, disk-throughput collectors and broader drive coverage are explicit extension work, not claimed implemented measurements. Hardware-generated cities remain 2.6. The earlier 208-test milestone belongs to 2.3; it is preserved as historical evidence.
