# Project GRID - Resources and host state

## Autonomy completion - 3.4 (2026-10-20)

**Implemented and verified.** The engine composes resource/shift/work/rest/travel and social decisions without an LLM. Equally ranked social choices now use a stable rank seeded by saved world identity, round and participant IDs; restore retains reproducibility without extra random state. Schema v9 gates this policy change. Preferences, affinity and world rules still take precedence.

**338 tests passed.** An accelerated one-hour simulation covered eight persistent agents, zero-to-512 fake application churn, resource scarcity/recovery, valid movement/events and concurrent snapshot readers. CPU, memory and latency measurements are recorded in [Autonomy and Verification](Project_GRID_Autonomy_and_Verification.md). This is a simulated-hour test, not a real-time hour-long production soak. Host applications remain observation-only; automatic agent creation, admin configuration, real task execution and Phase 4 communications remain planned. Previous dated pending-autonomy/schema statements describe earlier milestones.

## Social civilization completion - 3.3 (2026-10-20)

**Implemented and verified.** Explicitly opted-in Agent/Special residents now have cooperative, competitive or reserved preferences; bounded relationships, memories and occupation history; and simulated district civic tokens with scarcity/recovery. Decisions run on simulation time, respect current work/travel and commit atomically with ticks. Host applications remain observation-only and receive no fictional social activity or resource consumption.

Schema v8 persists social state and partial rounds, upgrades existing worlds without inventing participants, and supports validated backup recovery. `social [id|name]` and program inspection provide read-only views. Trusted engine APIs configure social participation/pools; production login, terminal admin configuration, automatic agent creation, actual communication/delegation and Phase 3.4 sustained autonomy remain future work. **336 tests passed**, plus isolated terminal create/save/reopen. See [Social Civilization](Project_GRID_Social_Civilization.md) for complete rules, limits and evidence. Earlier dated statements that social functionality is pending describe previous milestones.

## Latest population revision - 2026-10-19

The current population includes discoverable installed applications and current-session background processes. States are **Sleeping**, **On call**, **Running**, or **unknown** when evidence is insufficient. This supersedes earlier windowed-only/removal-on-exit descriptions below; those remain historical completion records. Ordinary application state is still rediscovered rather than persisted, and the saved city is reused. Full-machine coverage is not complete: see [Installed Application Availability](Project_GRID_Installed_Application_Availability.md) for implemented scope, Windows inventory limits, remaining work and **290-test** verification.

## Program behavior completion - 2026-10-19

**3.1 implemented and verified.** Non-demo residents now have deterministic needs/goals/actions. Applications reflect fresh or unknown observations; explicitly configured Agent/Special residents perform resource-gated simulated occupation work/rest on simulation-time shifts. Schema v5 persists their routines, sourced budgets and progress without restoring live observations or adding offline work. Use behaviors and inspect for last-tick behavior. No OS task execution, movement or named agent services are claimed.

See [Program Needs and Behavior](Project_GRID_Program_Behavior.md) for policy, APIs, limits and completion evidence: **278 Release tests passed**, build zero warnings/errors, disposable terminal smoke passed. Debug output was locked by running user sessions, which were left running. Earlier dated deferred-behavior/schema statements describe previous milestones; existing history and completed scope are preserved.

## Application resident completion - 2026-10-19

**2.7 / 2.7.1 implemented and verified:** production population now follows current-session windowed executable groups through read-only discovery and serialized lifecycle reconciliation. Application residents are transient; Agent/Special residents persist in schema v4. Before production conversion, legacy fictional citizens are preserved in a uniquely named validated archive. City/layout and clock remain intact. Unknown resource requirements display unknown condition; no host-control authority is implied.

Verification: **268 tests passed**, build zero warnings/errors, and a disposable Windows startup/reopen detected nine application groups. See [Application Residents Implementation](Project_GRID_Application_Residents_Implementation.md) for grouping limits, discovery/freshness, storage/migration, editing, resource interpretation and completion evidence. Earlier dated statements that 2.7 is planned, production cities stay empty, or all residents persist describe previous milestones. Agent delegation/services, login, rendering and additional per-application telemetry remain future work.

## Hardware city completion - 2026-10-19

**2.6 / GRID-REQ-004 implemented and verified.** First-time creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates bounded district/street/building-lot geometry and saves actual layout and provenance in schema v3. Later startups reuse the saved world without discovery or regeneration. Existing worlds are preserved; new generated cities start empty pending 2.7. Inventory is creation provenance, separate from live resource telemetry and application identity. Rendering, atmosphere, fashion and application-resident integration remain planned.

Verification: **250 tests passed** (14 Core, 106 Simulation, 74 Persistence, 56 Terminal); build had zero warnings/errors. A fresh Windows city and unchanged reopen were verified in a temporary directory. Use city to inspect saved generation details; --world with a new filename explicitly creates a separate world.

See [Hardware City Generation](Project_GRID_Hardware_City_Generation.md) for architecture, mappings, fallbacks, persistence and completion evidence. Earlier dated statements that hardware generation is planned or that normal creation always makes twenty fictional citizens describe previous milestones; this update supersedes those status statements while preserving their history.

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
