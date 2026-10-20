# Project GRID - Program needs and behavior

## Autonomy completion - 3.4 (2026-10-20)

**Implemented and verified.** The engine composes resource/shift/work/rest/travel and social decisions without an LLM. Equally ranked social choices now use a stable rank seeded by saved world identity, round and participant IDs; restore retains reproducibility without extra random state. Schema v9 gates this policy change. Preferences, affinity and world rules still take precedence.

**338 tests passed.** An accelerated one-hour simulation covered eight persistent agents, zero-to-512 fake application churn, resource scarcity/recovery, valid movement/events and concurrent snapshot readers. CPU, memory and latency measurements are recorded in [Autonomy and Verification](Project_GRID_Autonomy_and_Verification.md). This is a simulated-hour test, not a real-time hour-long production soak. Host applications remain observation-only; automatic agent creation, admin configuration, real task execution and Phase 4 communications remain planned. Previous dated pending-autonomy/schema statements describe earlier milestones.

## Social civilization completion - 3.3 (2026-10-20)

**Implemented and verified.** Explicitly opted-in Agent/Special residents now have cooperative, competitive or reserved preferences; bounded relationships, memories and occupation history; and simulated district civic tokens with scarcity/recovery. Decisions run on simulation time, respect current work/travel and commit atomically with ticks. Host applications remain observation-only and receive no fictional social activity or resource consumption.

Schema v8 persists social state and partial rounds, upgrades existing worlds without inventing participants, and supports validated backup recovery. `social [id|name]` and program inspection provide read-only views. Trusted engine APIs configure social participation/pools; production login, terminal admin configuration, automatic agent creation, actual communication/delegation and Phase 3.4 sustained autonomy remain future work. **336 tests passed**, plus isolated terminal create/save/reopen. See [Social Civilization](Project_GRID_Social_Civilization.md) for complete rules, limits and evidence. Earlier dated statements that social functionality is pending describe previous milestones.

## Movement and occupations completion - 3.2 (2026-10-20)

**Implemented and verified.** Saved city roads now support deterministic district travel for explicitly configured Agent/Special residents assigned to compatible capacity-one workstations. Five simulated seconds per edge; no work progress while traveling; shift/resource uncertainty freezes travel. Trusted assignment/release APIs preserve world ownership and revision checks. `jobs` and `activity` add read-only workstation and measured application views. Host applications acquire no fictional civic jobs.

Schema v7 preserves station assignments, remaining routes and edge timing in existing behavior JSON, validates them on restore and performs no offline catch-up. Older unassigned routines retain their 3.1 behavior. No agent population or interactive admin job configuration is added. **323 tests passed** in isolated Verification builds; disposable jobs/activity/save/reopen smoke passed. A running user Release session locked its binaries and was left untouched; rebuild the normal target after closing it to use the changes. See [Movement and Occupations](Project_GRID_Movement_and_Occupations.md) for policy, limits and full completion evidence. Earlier statements that movement/workplaces are pending refer to previous milestones.

## Latest population revision - 2026-10-19

The current population includes discoverable installed applications and current-session background processes. States are **Sleeping**, **On call**, **Running**, or **unknown** when evidence is insufficient. This supersedes earlier windowed-only/removal-on-exit descriptions below; those remain historical completion records. Ordinary application state is still rediscovered rather than persisted, and the saved city is reused. Full-machine coverage is not complete: see [Installed Application Availability](Project_GRID_Installed_Application_Availability.md) for implemented scope, Windows inventory limits, remaining work and **290-test** verification.

**3.1 implemented and verified - 2026-10-19.** Deterministic needs/goals and simulated occupation routines now run within the authoritative tick. Host applications remain observation-driven. Movement, workplaces, social behavior, real task execution and named agent services remain later work.

## Model and ownership

Each non-demo resident exposes a ResidentBehavior snapshot: Action, Goal, Need, Priority, work progress, remaining rest, completed simulated units and an optional WorkRoutine. Legacy demo citizens retain their original rules. No routine is assigned automatically based on an application name, display occupation or identity claim.

Trusted service code can call `ConfigureRoutine(programId, routine)` for explicitly classified Agent/Special residents. Configuration assigns an Administrator, Archivist, Researcher or Technician routine, copies its inputs, increments the resident revision and records a RoutineConfigured event. Reconfiguration deliberately resets progress. It grants no permissions and performs no Windows work. Existing terminal authentication/configuration controls remain planned; this release adds only the read-only `behaviors` command and richer inspection/status presentation.

Configured occupations have distinct simulated goals: civic-record review, archive indexing, research-record analysis or maintenance-record inspection. Their completed units are counters representing simulated work, not proof of processed user files, completed delegated tasks, invented host activity or civic-resource production. The occupation display field alone cannot assign or change a routine.

## Deterministic priority policy

| Resident/input | Action | Need/priority | Progress |
|---|---|---|---|
| Application, fresh confirmed observation | Observing | Observation / 10 | No routine or work units |
| Application, stale/unavailable observation | Unknown | Observation / 100 | No invented activity |
| Agent/Special, no routine | Idle | Assignment / 0 | None |
| Configured resident outside shift | OffDuty | Schedule / 90 | Frozen |
| In-shift resident with unknown resource condition | Unknown | Resource evidence / 100 | Frozen |
| In-shift resident below required headroom threshold | WaitingForResources | Resources / 100 | Frozen |
| Sufficient headroom and outstanding rest | Resting | Rest / 80 | Reduce rest by simulation step |
| Sufficient headroom and no outstanding rest | Working | Assigned work / 50 | Advance bounded simulated work |

Rows are evaluated in this order for the applicable resident kind; numeric priorities describe the selected need. Shifts use elapsed simulation hours modulo 24, support midnight wrap and never use wall-clock scheduling or offline catch-up. Start hour is 0–23; shift length is 1–24 hours. Configuration permits work bursts/rest periods from one second through one simulation day. Unit duration must divide the work burst exactly. Work/rest transitions occur on tick boundaries; large steps stop at the current burst/rest boundary and do not spend excess time replaying several cycles.

Entering/re-entering work requires condition >=60; a currently Working resident may continue at >=40. This hysteresis prevents threshold flicker. The existing 2.4 condition calculation remains the weakest sourced headroom ratio with EMA 0.25 and explicit unknown handling. The thresholds are GRID simulation policy, not claims about application health. Rest is scheduled simulated duty, not measured fatigue or magic energy recharge. Resource uncertainty and off-duty scheduling freeze both work and rest progress.

Legal transitions are owned by the policy, not arbitrary status commands. Application actions are Observing/Unknown; unassigned agents remain Idle; configured persistent residents select Working/Resting/WaitingForResources/Unknown/OffDuty. Persisted progress and configuration are validated. Completed-unit overflow, invalid configuration or revision exhaustion fail before tick state is committed. Tick updates stage all residents first, honor cancellation before commit, then update clock, behavior, revisions and history under the shared engine lock.

## Resource and editing integration

WorkRoutine includes up to 32 explicitly sourced headroom requirements. No requirements means unknown, not free capacity. Fresh host measurements are required after restart. Working-set usage does not become an application's inferred minimum requirement. No arbitrary non-demo energy drain/recharge was added. The compatibility Energy field is not overwritten with a health claim; terminal energy remains the nullable interpreted condition.

Requirements assigned with a routine persist with that routine. The earlier `SetResidentRequirements` API remains session-scoped for residents without a routine; it rejects attempts to silently override a configured routine. Use ConfigureRoutine to change its budget explicitly. Current applications are not automatically assigned requirements. Persistent agent-specific service requirements must still be supplied by trusted services with provenance.

Energy/status remain protected from non-demo edits. Behavior progress/configuration changes increment revision and invalidate stale edit previews through the existing checks. Display aliases/occupation labels are separate from the routine's typed occupation. Snapshots and the behaviors command do not advance work or the clock. Behavior is reported as the last committed tick state; live application availability and resource freshness remain separately observable while paused.

## Persistence and history

Schema v5 adds ResidentBehaviors, keyed to the persistent Programs row with cascading deletion and bounded JSON (20,000 characters). Migrations v1–v4 remain unchanged. Routine, sourced requirements, shift, counters and rest progress save with the world transaction. Application behavior is filtered out with transient residents. Old saves without behavior rows load with unassigned defaults; no fictional agents are created.

Restore retains last committed behavior/progress, starts paused and discards live observations/smoothing. The next tick waits if resource evidence is unknown; it does not advance the routine for elapsed offline time. Backup recovery preserves the same behavior data. RoutineConfigured, Behavior transitions and WorkCompleted feed existing durable/live history. Progress-only ticks do not emit per-sample events; completed units and state transitions are explicit. Existing history retention/buffering limits remain unchanged.

## Completion verification - 2026-10-19

- `dotnet test -c Release --no-restore`: **278 passed** (14 Core, 125 Simulation, 79 Persistence, 60 Terminal). Ten new tests cover work/rest cycles, resource hysteresis/unknown inputs, midnight shifts, observation-only application behavior, validation, cancellation/read-only snapshots, unassigned residents, whole-tick overflow rollback, save/reopen/recovery and terminal inspection.
- `dotnet build -c Release --no-restore`: zero warnings/errors.
- Debug verification was blocked by binaries locked by an already-running GRID instance and Visual Studio. Those sessions were left running; Release provided separate output. The existing test-only crash probe was added to the solution so Release tests build it in the matching configuration. This corrected an exposed test infrastructure issue, not a production dependency.
- Disposable Release terminal smoke: a step showed nine applications as Observing, with zero simulated units and no energy changes; exit/reopen retained the city and paused tick 1. Persistent routine progression/recovery was fixture-tested with explicit budgets. The user's default world was not modified.
- This completes 3.1's behavior foundation. It does not complete 3.2 movement/workplaces, 3.3 social systems, 3.4 extended autonomy/performance acceptance or later task execution/agent delegation.
