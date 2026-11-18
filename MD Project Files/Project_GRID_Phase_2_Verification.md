# Project Grid.AI - Phase 2 verification

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

**Section 2.5 completed - 2026-10-19.** This verifies the current 2.1–2.4 implementation: schema v2, all-Genesis-citizen persistence, history, backup recovery and the read-only telemetry/policy foundation. It does not claim completion of hardware-generated cities (2.6), application-resident lifecycle/persistence changes (2.7), or application-specific resource/energy binding.

## Evidence by checklist item

| Requirement | Verification and result |
|---|---|
| Save → close → reopen consistency | Real terminal subprocess creates a temporary world, steps it, records clock events and exits. A second process loads the same world paused, queries saved history, and exits. Tests compare world identity, programs/values/revisions, district membership/resources and clock state across reopen. Passed. |
| Transaction rollback and crash recovery | Existing injected SQL failure/cancellation tests verify atomic rollback. A new test-only writer opens a transaction, deletes programs, changes clock state and spills dirty SQLite pages using a small cache and a 2 MiB insertion. After an explicit readiness handshake and a nonempty journal check, the parent terminates that exact child. Reopening recovers the committed programs, clock and history; uncommitted table/data are absent, integrity_check returns ok, and the backup bytes remain unchanged. Passed. |
| Migration from an older schema | Creates a populated schema-v1 database using the unchanged original migration, with world metadata, one district/program, revision, working status, resource balance and clock state. Opens through current WorldStore, restores a paused engine, verifies original values, then saves and recovers through the current backup path. Existing migration tests cover version/checksum rejection and failed migration rollback. Passed. |
| No offline catch-up | Saves a running engine, disposes the store, reopens it, and restores with a TimeProvider reporting 2099. Tick count/time remain exactly saved, restoration is paused, and one manual step advances exactly once. Runtime instance identity is fresh; transient telemetry and pending events do not reappear. Passed. |
| Invalid or missing database behavior | Missing, empty, foreign and future-version files fail load without creating/replacing the primary. Real terminal startup rejects a deliberately corrupt primary and leaves it intact; explicit --recover restores the saved world and preserves the damaged file. Existing tests cover invalid backup, missing primary with backup, interrupted-recovery markers and semantic invalidity. Passed. |
| Persistent world without graphics or AI | Real dotnet terminal subprocesses run with redirected streams and no window, using only the existing terminal/simulation/persistence stack. Creation, stepping, saved-history query, shutdown, reopen and recovery succeed without loading a viewer or invoking a model service. Passed. |

## Test implementation and isolation

`Grid.Persistence.Tests/PhaseTwoVerificationTests.cs` adds eight cases (four facts and four invalid/missing-file theory cases). `TestSupport/Grid.Persistence.Probe` is a test-only executable built through a non-assembly project reference from the persistence tests. It is not referenced by production projects or shipped as a runtime service. Its only role is to hold an uncommitted transaction until the parent terminates it.

Subprocess tests start dotnet directly with structured argument lists, redirected streams and CreateNoWindow. They use unique temporary directories and explicit --world paths. Readiness and exit waits have timeouts; cleanup terminates only the child created by that test and disposes database connections before removing temporary files. The user's default world is never opened by these tests.

The crash probe exercises SQLite interruption recovery through the same connection/schema lifecycle, rather than adding production fault-injection hooks. Existing production-save tests separately inject SQL failure during state/history writes, verify transaction rollback and retry, and verify deduplication when backup publication fails after primary commit.

## Commands and results

On 2026-10-19:

```powershell
dotnet test Grid.AI.slnx --verbosity minimal
dotnet build Grid.AI.slnx --no-restore --verbosity minimal
```

Full suite: **231 passed**, zero failed/skipped: Core 8, Simulation 106, Persistence 64, Terminal 53. Build: zero warnings/errors. The earlier 223-test total is retained as historical evidence for 2.4, and 208/200 totals remain evidence for 2.3/2.2.

## Scope limits

This tests abrupt process termination and SQLite journal recovery on the current Windows environment, not physical power loss, failed storage hardware, every filesystem, all disk-full points or every possible interruption during file replacement. Existing explicit backup recovery tests complement journal recovery but do not prove exhaustive failure coverage.

Headless verification establishes no viewer/model dependency in the tested path; it is not a claim that an uninstalled GPU driver or disabled networking was tested. Telemetry reads local host counters but does not require a model, GPU renderer or cloud request. Restoration deliberately discards live telemetry and refreshes it through normal startup.

No production behavior changes were needed to pass this module. Completed 2.5 verification must not be used to mark pending 2.4 application binding, 2.6 or 2.7 work complete.
