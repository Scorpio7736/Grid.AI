# Project Grid.AI - Autonomy and Verification (3.4)

**Implemented and verified - 2026-10-20**

## Decision loop and rules

The existing engine tick now composes the complete Phase 3 decision loop: validate current world, evaluate resource/shift/work/rest/travel priorities, evaluate eligible social interactions against projected locations/actions, preflight revisions, check cancellation, then commit state and emit events. This reuses the 3.1-3.3 state machines rather than adding a competing scheduler. RunAsync schedules fixed simulation steps and retains pause/resume/cancellation behavior; no LLM, network service or host task executor is involved.

Applications reflect observations only. Explicit Agent/Special routines require sourced resource evidence; unknown or insufficient capacity blocks work/travel, shifts take precedence, and travel follows saved roads to a compatible exclusive station. Available opted-in agents can cooperate or compete under bounded social rules. Social configuration and workstation assignment remain trusted APIs; this milestone does not invent agents, automatically assign jobs or add production admin login.

## Reproducible variation

DecisionRandom.Rank is a stateless, version-one seeded rank. Its input is the persisted world UUID (16 Guid bytes), social round (signed 64-bit little endian), actor UUID and candidate UUID. SHA-256 produces a stable rank using the first unsigned 64-bit little-endian digest word. No System.Random state, process hash, wall-clock time or collection enumeration order is involved.

The rank breaks ties only after social preference and remembered affinity. Stable candidate ID breaks a rank collision. Existing rotating candidate order and one-pair-per-resident limit remain. A different world identity or round may produce different tied choices; unchanged saved state resumes the same choices. This seed is the world UUID, not the hardware-city numeric generation seed. Reproducibility requires the same world/resident identities, policy version and observation inputs; rediscovered application IDs, history event UUIDs and real telemetry are not promised to match across independent sessions.

Schema v9 is a version gate for this decision contract, preventing older binaries from silently continuing/saving with the prior tie policy. No new state column is needed because world identity and social round are already saved. Existing schema migration checksums remain unchanged; populated v7 migration tests now pass through v8 and v9. Earlier 3.3 documentation describes its original stable-ID-only tie rule.

## Accelerated one-hour scenario

AutonomyTests executes 3,600 one-second simulation ticks with controlled time, eight persistent agents and fake applications. Every ten ticks it supplies a new observation, cycling among zero, 32 and 512 applications. New PID lifetimes and confirmed exits exercise churn; foreground/background states alternate. Resource headroom cycles between unavailable and available, and a configured archivist travels to a workstation, waits, works and rests while other opted-in agents interact.

A concurrent task repeatedly reads and validates complete snapshots. The test verifies bounded social memories, persistent agent count, observational application behavior, zero and maximum observed populations, valid memberships/routes, movement events on actual saved road edges, ordered event ticks, work/social events and work resumption after scarcity. A test history consumer drains the bounded journal every ten ticks and asserts no drops; production must acknowledge only after a successful durable save. Existing persistence tests cover that separate save/recovery boundary.

This is **one simulated hour accelerated to about four real seconds**, not an hour-long wall-clock soak. It exercises growing observation load and reader contention; it does not impose arbitrary external CPU saturation or validate a real Windows collector for an hour. It does not prove absence of long-term leaks, storage growth problems or a universal latency guarantee.

## Measured results

Final focused scenario run on 2026-10-20, Verification build:

| Measurement | Observed value |
| --- | ---: |
| Simulated time | 3,600 seconds |
| Wall time | 4.215 seconds |
| Test-process CPU time delta | 5.031 seconds |
| Process peak working set | 105,426,944 bytes |
| Managed heap at start | 5,324,640 bytes |
| Sampled managed heap peak | 30,063,688 bytes |
| Managed heap at end (before forced GC) | 13,844,688 bytes |
| Tick p95 / max | 2.260 / 18.225 ms |
| Concurrent snapshot reads | 246 |
| Snapshot read p95 / max | 17.028 / 54.797 ms |

CPU time can exceed elapsed time because multiple process threads run concurrently. Memory and CPU include the test host; sampled heap peak is not a complete allocation profile, process peak may include setup, and GC timing affects end values. These are observations on this PC, not production budgets. No scheduler/backpressure changes were needed for this tested envelope. Reads made progress during maximum population load and the reader shut down within its ten-second test deadline.

## Completion evidence and remaining scope

**338 tests passed:** 14 Core, 146 Simulation, 86 Persistence, 92 Terminal. Commands: `dotnet test <test-project>/<test-project>.csproj -c Verification --no-restore` for each project. The focused autonomy run also uses `--filter FullyQualifiedName~AutonomyTests --logger "console;verbosity=detailed"` to print measurements. Replay testing resumes mid-round and compares 100 additional ticks of social decisions. Existing tests continue covering shifts, cancellation, overflow rollback, route constraints, resource freshness, bounded state, save/recovery and terminal behavior.

No user's default save or existing running terminal was used. Rebuild/restart the normal launcher target to use the new policy. Production identity, host task execution, LLM integration and Phase 4 communication/delegation remain planned. A real-time hour-long production soak and broader machine/performance profiling remain additional validation, not evidence claimed by this milestone.
