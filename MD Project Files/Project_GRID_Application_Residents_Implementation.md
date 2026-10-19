# Project GRID - Application residents implementation

**2.7 and 2.7.1 implemented and verified - 2026-10-19.** This implements the initial GRID-REQ-005 population/persistence foundation. It does not implement autonomous named agents, login, task delegation or full application health diagnosis.

## Population and grouping policy v1

Normal terminal startup loads the saved city and persistent residents, archives any legacy fictional residents, then samples running applications. The monitor samples every two seconds independently of simulation ticks, including while paused. `--ephemeral` retains the original twenty-citizen demonstration without application discovery.

The Windows adapter represents **current-session windowed executable groups**, not every OS process. At least one observed main window initially qualifies an executable. All accessible same-executable processes in that session join one representative, including multiple independent windows and same-image helpers. Different executable paths remain separate even when display names match. Other sessions, session-zero services and GRID's own process are excluded. Shell/window-host processes can qualify; hosted applications may appear under their host executable. Cross-executable child ownership, background-only apps and per-window/per-instance representatives are outside this first policy.

Grouping keys hash the normalized full executable path plus session ID. Paths are used locally for grouping and are not saved in world/history. No titles, command lines, documents or credentials are collected. Names are display labels and grant no privileges. Process identity combines PID with UTC process start time; neither a PID nor display name alone identifies a lifetime.

Discovery uses new Process objects on each sample, with a five-second cooperative processing budget, 4,096 retained process rows, 512 groups, 512 active application residents and an 8,192-entry bounded lifetime cache. A blocked individual OS call is not forcibly interrupted; this is not a hard real-time deadline. Budget/access failures mark the sample incomplete. Resident-limit deferrals are visible in status. Unsupported platforms report unavailable discovery rather than fictional residents.

Main-window detection follows the platform's heuristic, not a universal application catalog: [Microsoft Process.MainWindowHandle](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.mainwindowhandle). Memory terminology follows [Microsoft Process.WorkingSet64](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.workingset64).

## Lifecycle and identities

Core defines Legacy, Application, Agent and Special resident kinds. Application residents receive a new resident ID and incarnation GUID on discovery; neither survives GRID restart. Explicit Agent/Special classification preserves durable identity but does not grant an authenticated role. Legacy is reserved for preserved old data and demos, not automatically promoted to Agent/Special.

The engine validates and detaches observations before reconciling under the same lock as ticks, edits and snapshots. Overlapping process lifetimes preserve an application's incarnation as helpers start/stop. A confirmed disjoint replacement creates a new incarnation, including PID reuse. Membership removal and addition are serialized; transfers and edit previews for expired resident IDs fail.

Absence from two consecutive complete observations removes a resident. Incomplete samples break that confirmation chain; missing entries become unknown, not confirmed exits. A complete OS PID enumeration may separately confirm that a previously observed lifetime has exited, even if another process denies access. All tracked lifetimes must be confirmed gone before that route removes a group. Partial groups retain unknown helper lifetimes; aggregate memory is unknown until a complete sample.

Samples older than ten seconds, future samples and out-of-order/duplicate timestamps cannot reconcile population. Reads expose expired observations as unknown. Missed short-lived apps and unobserved offline actions are never invented. There is no offline time catch-up. Applications initially enter the Processing Sector, or a stable existing district for a legacy city; district movement/aliases are transient presentation edits.

## Resources, energy and editing

Per-group working-set bytes are reported only with complete data. The total counts each process once, but shared physical pages can still overlap across processes; it is not unique RAM allocation or an application's minimum requirement. Per-application CPU, GPU/VRAM, disk and network attribution remain explicitly unknown. Host headroom observations from 2.4 remain separate and have their own freshness/provenance.

Non-demo residents use the 2.4 ResourceConditionPolicy for optional explicitly sourced additional-work budgets: weakest required headroom ratio, bounded 0–100, EMA factor 0.25 for new samples, unknown/reset on absent requirements or stale/unavailable inputs. `SetResidentRequirements` is a trusted, session-scoped configuration API. It does not infer minimum application requirements or persist a service budget. Production discovery currently supplies no budgets, so condition/energy displays **unknown**. Persistent agent service-specific budgets can be configured by future trusted services; no agent health score is fabricated now.

Non-demo residents do not drain energy for movement/open duration or gain fictional recharge. The retained numeric Energy field is compatibility state, not measured energy; terminal presentation uses the nullable interpreted Condition. Fixed Genesis rates remain only for Legacy fixtures. `programs`, `inspect program` and live view expose resident class and observation/condition state; inspection adds incarnation, timestamp, source and working-set units. `status` reports global discovery completeness, including valid empty or unavailable observations.

Energy/status are observation/service-owned for non-demo residents. Editing rejects these fields and alias descriptors that would mutate them; per-target metadata omits the built-in fields. Names, occupations and district assignments continue through existing authorized edit operations and remain transient for applications. Classification, process binding and incarnation are not editable registry fields. An observation grants no OS-control capability. No code here closes, suspends, restarts or throttles an application; fictional civic resources remain separate.

## Schema v4, saves and legacy policy

Migration v4 appends Programs.ResidentKind with default Legacy and constraints allowing only Legacy, Agent or Special durable rows. Migrations v1–v3 are unchanged. Saves validate the full snapshot, then omit Application rows and their district membership references inside the existing transactional write. City/layout, clock, persistent resident values/revisions and history remain durable. Live process bindings, resource observations, aliases and application state are not saved. Recovery loads durable state and production startup discovers current applications again.

Schema migration alone classifies old rows as Legacy without deleting them. Before normal terminal production conversion, `WorldStore.ArchiveLegacyResidents` makes a validated SQLite copy named `<world.db>.legacy-<unique id>.db`. This archive includes the full original residents, world state and history under the upgraded schema; it is a recoverable data copy, not a byte-identical pre-migration file. Only after successful archive validation does a transactional save remove Legacy residents from the active world. The city, clock and existing Agent/Special residents remain intact. The archive path is printed. Archives are not rolling backups and are never automatically overwritten or pruned.

Archive failure/cancellation prevents conversion. Write failure retains a recoverable archive and transactional rollback preserves the active population. Conversion is idempotent once no Legacy rows remain. Recovering an older legacy backup triggers a fresh archive before conversion. Generic WorldStore.Load and demo tests can still inspect Legacy rows; the terminal production path performs conversion explicitly. To inspect old fictional citizens, use a separate archive copy with the legacy-capable storage API; normal production terminal startup will convert that copy too. Existing recovery, exclusive lease and no-silent-regeneration rules remain in force.

## History and verification

ApplicationObserved/ApplicationExit history records contain resident ID, display label, incarnation, source, simulation tick/time and observation explanation. They do not contain paths, PIDs, full process bindings or command lines and cannot replay residents. Existing durable history retention remains unbounded until explicit future retention tooling; the pending queue is bounded at 10,000 with gap reporting and live events at 256 by default. No event per telemetry sample and no invented offline exit time is recorded.

Completion evidence on 2026-10-19:

- `dotnet test --no-restore`: **268 passed** (14 Core, 117 Simulation, 78 Persistence, 59 Terminal), including 18 new cases beyond 2.6.
- `dotnet build --no-restore`: zero warnings/errors.
- Fixtures verify executable grouping/helpers/independent windows, zero applications, denied/incomplete/stale observations, PID reuse, unknown helpers, confirmed exits, sourced condition interpretation, expired previews and observation-owned editing.
- A concurrent 100-tick/100-reconciliation churn test checks membership invariants. Persistence tests verify changed application sets across restart and recovery, Agent/Special continuity, application omission and legacy archive/cancellation/write-failure behavior. Existing versioned migration rollback and crash-recovery tests also pass.
- Real Windows disposable-world smoke detected nine windowed executable groups; reopening preserved the same six-district city and paused tick zero while rediscovering the population. No default user world was modified. The smoke did not launch or terminate user applications; controlled start/exit behavior is fixture-tested.

Known limits are the explicit initial grouping scope, unsupported per-application metrics, session-only optional budgets, cooperative collector timeout and no production agent task services yet. These do not authorize guessing application requirements, promoting a detected executable to an agent, or extending the feature into host control.
