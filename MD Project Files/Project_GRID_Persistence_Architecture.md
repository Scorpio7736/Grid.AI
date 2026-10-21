# Project GRID - Persistence architecture and schema history

## User-supplied context - 4.3 completed (2026-10-20)

Implemented explicit local `context add/list/read/revoke/remove` with project names, source SHA-256/provenance, temporary or persistent retention, and optional registered-Agent grants. Master Control selects files; agents only read granted captures. Strict UTF-8 text/source and DOCX main-body text are supported with bounded parsing. Schema v13 stores persistent captures transactionally; temporary bodies are excluded from save/backup. `talk context` reports availability without executing/ingesting file contents. **420 tests passed.** See [context implementation and limits](Project_GRID_User_Supplied_Context_Implementation.md). Earlier dated schema/status notes below retain their historical scope.


## Current 4.2B completion - 2026-10-20

Terminal UX and local password credentials are implemented and verified by 368 passing tests. Schema v12 preserves existing owners and adds authenticated credential migration/rotation. New worlds use interactive setup; existing owners use `login master`, then `credentials`. Read [4.2B implementation notes](Project_GRID_Terminal_UX_Implementation.md) for exact behavior and limits. Earlier 4.2A/schema v11 notes below describe their historical milestone. Full Flash GRID packaging remains planned.


## Installation editions and rendering - planned 2026-10-20

Installation will offer **PC GRID** (permanent machine installation, with advanced 3D options) or **Flash GRID** (mobile flash-drive package, with very simple lightweight 3D). The installer must explain the choice, validate the destination and retain the selected edition. Both share simulation/save/identity behavior and must run supported tasks offline after provisioning. Flash GRID keeps managed files with the portable package and requires no persistent host integration; graphics still run on the host PC. Quality differences must not regenerate or change the world.

See [edition details](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 7.6/8.6. Rendering budgets, packaging and migration remain planned; this addition changes documentation only.

## New UI, portable-package and offline requirements - 2026-10-20

**4.2B implemented - 2026-10-20.** GRID-REQ-006 provides dashed per-command option lists with usage/examples, bundled `help -md`, opening artwork, username/password setup when not configured, clear/welcome/authority/Enter after login, confirmed logout returning to welcome, a light-blue/white/yellow/red palette, and capability-aware Tab completion. The [Markdown help reference](Project_GRID_Terminal_Help.md) is bundled and displayed offline by `help -md`.

GRID-REQ-007 requires a self-contained flash-drive package with portable identity and package-relative managed storage/dependencies. GRID-REQ-008 requires all GRID runtime tasks to work offline; only explicit installs/provisioning may use the internet. Portable password authentication is implemented; host-local storage and PATH registration still do not fulfill full package portability. Earlier optional-cloud/online runtime plans are superseded by this offline requirement. Preserve saved city and world history across host changes.

See [the shared plan](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 4.2B (implemented), 8.6 and 8.7 (planned). See the 4.2B completion notes for verification; earlier dated implementation notes retain their historical scope.

## Local login and identity completion - 4.2A (2026-10-20)

**Implemented and verified.** Start with `login master`: the first successful Windows-token login binds the world owner; later logins must match. Sessions expire after eight hours. Master Control can register existing Agent identities, switch roles, and preview/confirm one-time program edits. Prompts/help/commands are role-aware; agent chats are participant-scoped and global/admin controls are denied. Switching/logout clears active chats and previews. Schema v11 preserves owner/agent registration without restoring authentication.

**362 tests passed**, plus isolated Windows login/agent restriction/confirmed edit/restart smoke. See [Local Login and Identity Implementation](Project_GRID_Local_Login_and_Identity_Implementation.md) for commands, service authorization, audit and security limits. This authenticates the current Windows account, grants no OS elevation, and provides no independent agent passwords or real task service. NGE now requires Master Control; --start waits for login. Earlier dated pending-login/editing statements refer to prior milestones.

## Terminal conversations completion - 4.2 (2026-10-20)

**Implemented and verified.** Use connect (default ARCHON), talk, conversations, transcript, resume and disconnect. ARCHON is created on first explicit connection as a persistent coordinator Agent; a stable world-local operator handle supports reopening saved conversations. Replies to status/who are you are labeled verified GRID state; other replies are labeled deterministic simulated dialogue and claim no task execution/delegation. Input/reply pairs commit atomically and reuse schema v10.

**353 tests passed**, plus a three-launch isolated terminal save/resume/end smoke. See [Terminal Conversations](Project_GRID_Terminal_Conversations.md) for syntax, identity/capacity limits and completion evidence. This local operator handle grants no authentication or admin authority; 4.2A login, real task delegation and LLM dialogue remain planned. Earlier dated pending-terminal-chat/agent-creation statements describe previous milestones.

## Communications foundation completion - 4.1 (2026-10-20)

**Implemented and verified.** Grid.Communication now defines recipient/message/conversation models and a local hub contract implemented by the simulation host. Exact name/full-ID routing, explicit participant identities, start/send/end/read operations, ordered bounded messages and schema v10 transactional transcript persistence are available. Conversations survive restart and backup recovery even when a historical application resident is absent; new sends require live original program identities.

**346 tests passed.** See [Communications Foundation](Project_GRID_Communication_Foundation.md) for APIs, identity boundaries, bounds and recovery evidence. These trusted local APIs store messages without authenticating the caller, executing text, claiming external delivery or generating replies. Terminal chat is still 4.2; voice, real delegation, LLM dialogue and production login remain planned. Earlier dated pending-communication/schema statements describe previous milestones.

## Autonomy completion - 3.4 (2026-10-20)

**Implemented and verified.** The engine composes resource/shift/work/rest/travel and social decisions without an LLM. Equally ranked social choices now use a stable rank seeded by saved world identity, round and participant IDs; restore retains reproducibility without extra random state. Schema v9 gates this policy change. Preferences, affinity and world rules still take precedence.

**338 tests passed.** An accelerated one-hour simulation covered eight persistent agents, zero-to-512 fake application churn, resource scarcity/recovery, valid movement/events and concurrent snapshot readers. CPU, memory and latency measurements are recorded in [Autonomy and Verification](Project_GRID_Autonomy_and_Verification.md). This is a simulated-hour test, not a real-time hour-long production soak. Host applications remain observation-only; automatic agent creation, admin configuration, real task execution and Phase 4 communications remain planned. Previous dated pending-autonomy/schema statements describe earlier milestones.

## Social civilization completion - 3.3 (2026-10-20)

**Implemented and verified.** Explicitly opted-in Agent/Special residents now have cooperative, competitive or reserved preferences; bounded relationships, memories and occupation history; and simulated district civic tokens with scarcity/recovery. Decisions run on simulation time, respect current work/travel and commit atomically with ticks. Host applications remain observation-only and receive no fictional social activity or resource consumption.

Schema v8 persists social state and partial rounds, upgrades existing worlds without inventing participants, and supports validated backup recovery. `social [id|name]` and program inspection provide read-only views. Trusted engine APIs configure social participation/pools; production login, terminal admin configuration, automatic agent creation, actual communication/delegation and Phase 3.4 sustained autonomy remain future work. **336 tests passed**, plus isolated terminal create/save/reopen. See [Social Civilization](Project_GRID_Social_Civilization.md) for complete rules, limits and evidence. Earlier dated statements that social functionality is pending describe previous milestones.

## Movement and occupations completion - 3.2 (2026-10-20)

**Implemented and verified.** Saved city roads now support deterministic district travel for explicitly configured Agent/Special residents assigned to compatible capacity-one workstations. Five simulated seconds per edge; no work progress while traveling; shift/resource uncertainty freezes travel. Trusted assignment/release APIs preserve world ownership and revision checks. `jobs` and `activity` add read-only workstation and measured application views. Host applications acquire no fictional civic jobs.

Schema v7 preserves station assignments, remaining routes and edge timing in existing behavior JSON, validates them on restore and performs no offline catch-up. Older unassigned routines retain their 3.1 behavior. No agent population or interactive admin job configuration is added. **323 tests passed** in isolated Verification builds; disposable jobs/activity/save/reopen smoke passed. A running user Release session locked its binaries and was left untouched; rebuild the normal target after closing it to use the changes. See [Movement and Occupations](Project_GRID_Movement_and_Occupations.md) for policy, limits and full completion evidence. Earlier statements that movement/workplaces are pending refer to previous milestones.

## NGE implementation - 1.13 (2026-10-19)

**Initial local-terminal NGE implemented.** Type `nge` in an interactive saved-world session, review the world identity and exact file list, then enter `y` to confirm or `n`/blank to cancel. Invalid answers reprompt; EOF cancels deletion and follows normal EOF shutdown; Ctrl+C cancels deletion and follows normal graceful shutdown. Arguments such as `nge y` are rejected. Redirected/unattended input and ephemeral sessions cannot delete saves. This is local-operator confirmation, **not authenticated admin enforcement**; human-admin-only access remains future login/authorization work.

Confirmed NGE stops/awaits scheduler, autosave and telemetry, serializes against in-flight saves, revalidates world/backup identity, then removes only the active database, rolling `.backup` and exact `-journal`, `-wal`, `-shm` paths. It retains the session lease through completion and refuses symbolic-link/junction targets. Historical legacy/damaged archives, staging artifacts of uncertain ownership, external copies, other worlds and PC applications remain; the warning explicitly states this scope. The harmless session `.lock` file may remain after release. Interrupted recovery must be resolved before NGE can be prepared.

A flushed `.nullifying` marker is created before deletion. Partial failure retains that marker, blocks startup/save/recovery, reports remaining files and exits nonzero without a final save. **There is no automatic retry/resume command yet:** inspect the marker and remaining exact world artifacts before explicit manual cleanup; never remove the marker merely to bypass an unresolved operation. Successful NGE removes the marker and exits with the GRID7736 artwork and `GRID NULLIFIED // EXISTENCE TERMINATED`, never a saved-world claim. Next normal startup follows existing hardware-city creation; it does not reuse a managed backup. Independent historical/manual copies remain outside this deletion scope.

Verification: **306 Release tests passed** (14 Core, 127 Simulation, 83 Persistence, 82 Terminal). New coverage checks cancellation/blank/invalid/EOF input, uppercase confirmation, redirected/ephemeral rejection, worker shutdown before deletion, no final save on success/failure, owned-file deletion, unrelated-file preservation, rejected forged plans, blocked future writes/recovery, new identity after reopen and a Windows locked-file failure retaining the marker. All deletion tests use freshly created disposable worlds; no user save was deleted. Existing hardware-city creation tests cover the startup path; no manual destructive run against the user's world was performed. Visual Studio still owns its post-exit footer.

## Terminal commands and shutdown complete - 1.12 (2026-10-19)

Implemented grouped `help`, display-only `clear`, `programs -active`, `programs -possible`, `programs -sleeping`, and a GRID7736 ASCII farewell. Active selects fresh Running applications; sleeping selects fresh On call or Sleeping applications; possible includes current applications plus the saved known-application catalog. `ids` works with one filter. Unknown/stale observations are never classified as active or sleeping. Bare `programs` still includes agents and other residents.

**Schema v6** adds a separate ApplicationCatalog table (key, name, source, last catalog observation UTC). The catalog is captured with the simulation snapshot and saved atomically with world/history; backup/recovery and schema validation include it. Installed/observed metadata survives restart; ordinary residents, PID lifetimes, activity and resource readings are not restored. Last observation is catalog provenance, not proof of a launch or current installation. Entries missing from fresh discovery display Historical / unknown. Existing schema-v5-only statements below describe earlier milestones.

The ASCII banner appears after graceful shutdown and final save. Save/shutdown errors retain diagnostics and a nonzero exit code without a success message; ephemeral sessions explicitly say NOT SAVED. There is no keypress wait. Visual Studio can still append its own debug-console footer. Help uses portable ASCII separators and registered command metadata, including extensions; no-color/redirection behavior is preserved.

**Verification:** `dotnet test -c Release --no-restore`: **297 passed** (14 Core, 127 Simulation, 81 Persistence, 75 Terminal). Tests include filters/IDs/invalid flags/empty results, clear preserving the world and tick, successful/failed farewell, catalog save/reopen/recovery and v5-to-v6 migration. A disposable built-terminal run exercised all command groups, filters, redirected clear fallback and successful save/reopen of the same city. An ephemeral PTY run preserved tick1 through clear and displayed the NOT SAVED banner. This tool's PTY uses the clear fallback; physical screen clearing/color appearance and Visual Studio footer presentation were not visually verified in the user's console. No IDE settings or existing user worlds were modified.

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


## Resource/host foundation update - 2026-10-19

Section 2.4 now provides read-only host observations, explicit units/sources/freshness, fake-provider tests and a provenance-required additional-work resource-condition policy. Use resources for details or live for a compact CPU/RAM summary. Unsupported measurements and unknown application requirements remain unknown. Current observations are transient and cannot be edited into Windows allocations or restored from a save as fresh data.

Verification: 223 tests passed, build zero warnings/errors, and real Windows disposable-session sampling passed while simulation time stayed paused. See [Resources and Host State](Project_GRID_Resources_and_Host_State.md) for collector coverage, the exact policy and limitations. Application discovery/classification, per-application resource attribution and replacing application energy drain remain dependent on 2.7. Existing fictional Genesis citizens retain their old rules. Earlier deferred-telemetry statements below describe prior milestones, not this update.


## History implementation update - 2026-10-18

Section 2.3 now persists meaningful committed changes with world snapshots in schema v2. The history command supports entity/kind/severity/time filters and bounded paging. Routine Tick records are excluded; pending-buffer overflow is reported explicitly. Changing edits include attribution/context, but denied attempts and no-ops are not a comprehensive security audit. Structured operational errors are emitted to terminal output. See [History and Observability](Project_GRID_History_and_Observability.md) for retention, failure and API details.

Verification: 208 tests passed; solution build had zero warnings/errors; isolated terminal restart/query smoke passed. Earlier schema-v1 and deferred-history statements below describe the prior milestones. Live-view history remains process-local; the new saved-history path is separate. Application discovery, resource interpretation, resident classification and persistence filtering remain planned in 2.4/2.7.


## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

Section 2.2 currently saves every program and remains implemented as documented below. Planned 2.7 adds explicit entity classification, filtered persistence and versioned legacy migration. Save city/agents/special programs; rediscover ordinary running applications after normal load and backup recovery. Preserve recoverable legacy saves; do not silently delete old citizens or edit schema v1.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


**Sections 2.1 and 2.2 implemented and verified - 2026-10-18.** SQLite schema v1 now supports engine save/load, periodic consistent snapshots, explicit backup recovery and terminal lifecycle integration. Durable events/audit and hardware-generated cities remain later work.

## Project and connection boundary

Grid.Persistence replaces its empty Class1 scaffold with SqliteGridDatabase, GridSchema, WorldStore and WorldPersistenceSession. Microsoft.Data.Sqlite 10.0.13 is retained. Terminal references Persistence; Persistence references Simulation and Core. Simulation has no database dependency: it supplies validated snapshot restoration; Terminal owns orchestration.

SqliteGridDatabase takes an explicit filesystem path, normalized to an absolute path at construction. It rejects empty paths, SQLite file URIs and the in-memory special name. The parent folder must already exist. Connection strings use SqliteConnectionStringBuilder, not interpolation of user input.

- `Initialize()` explicitly creates an empty schema or migrates a recognized existing GRID database. It does not create a world, seed citizens or mark GRID-REQ-004 generation complete.
- `OpenExisting()` requires an existing, initialized file, validates/migrates recognized versions and returns an open SqliteConnection. The caller must dispose it. A missing or empty file is not silently initialized.
- Each connection enables and checks foreign-key enforcement, uses private cache, disables pooling and selects FULL synchronization. Busy operations have a five-second default timeout. Journal mode is not changed; WAL tuning is deferred until persistence workloads justify it.
- Callers group related SQL writes in an explicit transaction and commit only on success. Disposal without commit rolls back. SQL values must use parameters. Returning a connection is a trusted application boundary, not an authorization API or support for arbitrary terminal SQL.
- Connections/transactions are not shared concurrently. WorldPersistenceSession serializes snapshot capture and saves; the engine releases its lock before disk I/O. WorldStore serializes its operations and holds an exclusive file lease for the session lifetime.

## Schema v1

Tables use SQLite STRICT typing plus NOT NULL, primary/foreign keys and CHECK constraints.

| Table | Key and relationships | Stored data |
|---|---|---|
| SchemaMigrations | Version primary key | Migration name, SHA-256 checksum, UTC application timestamp |
| Worlds | WorldId primary key | Name, UTC creation timestamp, optional generation seed and generator version |
| Districts | (WorldId, DistrictId); world foreign key | Name and description |
| Programs | (WorldId, ProgramId); world and same-world district foreign keys | Name, occupation, energy, named status, CurrentDistrictId, revision |
| DistrictResources | (WorldId, DistrictId, ResourceKey); district foreign key | Named float-compatible resource amount |
| SimulationStates | WorldId primary/foreign key | Tick count, elapsed TimeSpan ticks, tick-duration TimeSpan ticks, WasRunning, UTC save timestamp |

Program membership is derived from Programs.CurrentDistrictId. There is no second stored district-membership list that could disagree. IX_Programs_District supports resident lookup. Composite keys allow deterministic Genesis entity IDs to repeat in separate worlds while preventing cross-world references and duplicates within a world.

Deleting an occupied district is rejected. Deleting a world cascades to its owned data; resource rows also cascade with district removal. No terminal deletion command is provided. The eventual repository/service must authorize destructive operations separately.

Energy is required and bounded to 0-100. Status is stored by one of the five current enum names, not an unstable ordinal. Revisions and tick counts are nonnegative integers. Tick duration is positive; elapsed ticks must equal tick count multiplied by tick duration without exceeding Int64. WasRunning is restricted to 0/1. Resource amounts must be finite and fit the current float range; negative resource quantities are not prohibited here because section 2.4 has not defined resource mechanics.

IDs use canonical GUID text. Schema checks are supplemented by WorldStore and GridSimulationEngine.ValidateSnapshot: canonical nonzero GUIDs, UTC round-trip timestamps, valid enums, finite/bounded energy, finite resources, nonnegative revisions, unique IDs, exact district membership and consistent clock arithmetic. Program labels obey trimmed, control-free 80/120 UTF-16-unit limits. Invalid data is rejected before a restored engine is published.

## Durable world identity versus runtime identity

Worlds.WorldId is GridWorld.Id, the persistent identity. GridWorld.InstanceId remains a new identity on every restoration and scopes edit previews. Restored engines use default-deny editing authorization; no sessions, leases or previews are persisted. Program revisions retain their exact saved values without reviving pre-restart edit authority.

Clock storage uses integer .NET TimeSpan ticks. WasRunning records the captured state, but restoration always starts paused; --start explicitly resumes automatic ticking. Loading never adds elapsed wall time during shutdown. LastTick, recent events and sequence counters are process-local and reset on restoration.

## Migration policy

GridSchema.CurrentVersion is 1. PRAGMA application_id identifies GRID (`0x47524944`); PRAGMA user_version stores the committed schema version. SchemaMigrations records each applied migration's name and checksum. SQL checksums normalize CRLF to LF so identical migrations remain portable across checkout line endings.

The migration runner acquires an immediate transaction before inspecting versions. Concurrent first initializers serialize rather than both applying v1. For a new file, initialization is permitted only when application/version markers are zero and no user schema objects exist. Foreign applications, unversioned nonempty databases and future schema versions are rejected without replacing their content.

For existing versions, history must be contiguous and match the bundled migration names/checksums. The entire pending migration chain, history entries and version/application markers commit together. Failures roll back DDL and markers as well as data changes. A foreign_key_check runs before commit. SQLite read/format errors propagate; there is no automatic deletion, reset, downgrade or repair.

Add future migrations with contiguous version numbers and increment CurrentVersion. Never edit the SQL/name of a released migration; make a new migration and test populated older databases. Production includes only v1 today. Tests use synthetic v2 migrations to verify upgrade and rollback mechanics; they do not establish a released v2 schema.

History validation is not a full schema-tamper detector. Section 2.2 adds integrity/domain validation and explicit backup recovery. Neither milestone establishes a guarantee against physical media loss or hardware that fails to honor flush/locking semantics.

## Planned extensions

- **2.2 complete:** atomic snapshot save, validated rehydration, periodic saves, paused restoration and backup recovery; operational details follow below.
- **2.3:** durable world events and administrative receipts, retention, querying and write-failure policy. The 256-entry live buffer remains transient and is not automatically flushed to SQLite.
- **GRID-REQ-003:** operator identities, roles and audit records need separate migrations and service authorization. Do not persist live leases, credential material or reusable session previews in world-state rows.
- **GRID-REQ-004 / 2.6:** hardware profile, generated layout, component associations and initialization provenance require future versioned tables. Nullable seed/version columns only reserve basic provenance; they do not implement hardware discovery, procedural generation or durable city reuse. Existing worlds must survive hardware changes.

## Verification evidence

On 2026-10-18:

- `dotnet test Grid.Persistence.Tests/Grid.Persistence.Tests.csproj --verbosity minimal -p:NuGetAudit=false`: 31 passed.
- `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false`: 171 passed, zero failed/skipped (Core 8, Simulation 93, Terminal 39, Persistence 31).
- `dotnet build Grid7736.slnx --no-restore --verbosity minimal`: zero warnings/errors.

Tests use isolated temporary file-backed databases. Coverage includes initialization/reopen without row loss, per-connection foreign keys, stored-value constraints, same-world membership, cascades, caller rollback, failed initial/upgrade migration rollback, synthetic upgrade idempotence, missing/empty/foreign/future/invalid files, history disagreement, foreign-key violations and concurrent initialization. These 2.1 row round trips were SQL-level checks; the subsequent 2.2 verification below covers engine save/load.

API behavior was checked against [Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions), [connection settings](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings), and [SQLite PRAGMA documentation](https://www.sqlite.org/pragma.html).

See [Master Development Checklist](Project_GRID_Master_Development_Checklist.md) for remaining Phase 2 work.

## Save and recovery operations (2.2) - 2026-10-18

`WorldStore` manages one world per database, although schema v1 retains composite keys for future expansion. It refuses metadata/world-ID replacement. Its `.lock` file is held with exclusive sharing throughout the session; a second GRID process must choose another path or wait for the first to exit. Lock files can remain after exit; the open handle, not file existence, determines ownership. External SQL tools are trusted maintenance tools and must not edit an active world.

Normal startup uses `%LOCALAPPDATA%\GRID\world.db`, creates its folder and persists the fixed Genesis seed before beginning a session. An explicit `--world <path>` requires an existing parent folder. Existing but empty, unrecognized, malformed or corrupt files cause startup failure rather than regeneration. Missing primary files with backup, recovery marker or SQLite sidecars also cannot trigger automatic creation. An interrupted first creation without a valid backup requires manual inspection; GRID does not erase its evidence.

Each save captures the engine under its tick lock, copies/validates state and writes world metadata, districts, resources, programs, revisions and clock in one parameterized SQLite transaction. Cancellation is checked before commit; SQL failure or cancellation rolls back the whole transaction. SQLite rollback-journal recovery handles interrupted transactions when the database is reopened. A final durable commit is never cancelled halfway through. The original v1 migration and its checksum are unchanged.

After each commit, SQLite's backup API creates a standalone staged copy. GRID validates it before atomically publishing `<database>.backup`. This is one rolling last-known-good snapshot, not a versioned backup archive. A backup-refresh failure explicitly reports that the primary save committed and the earlier backup remains; it is not described as a rolled-back save. Staged `.tmp-*` files left by an abrupt process termination are not selected as recovery sources.

`--recover` explicitly validates the backup, stages a standalone replacement, marks recovery in progress, archives the primary and its SQLite sidecars as `.damaged-<UTC>-<id>*`, and installs the replacement. Invalid backups never replace the primary. If recovery is interrupted, `.recovering` blocks ordinary load/save/create until recovery is retried. Damaged archives are retained for inspection and have no automated retention/quota policy yet. Recovery can roll back changes newer than the backup; it is never performed silently.

`WorldPersistenceSession` performs consistent saves every 30 seconds, including while the terminal is in live view. Terminal shutdown cancels and awaits the scheduler and periodic saver before saving with an uncancelled token. Exit, EOF and Ctrl+C share this path. Autosave/final-save failures are displayed and return a nonzero exit code. Abrupt termination cannot run a final save: only the last committed snapshot is recoverable. Disk operations run outside the UI/simulation lock, but slow storage can delay orderly shutdown.

```powershell
# Normal persistent world; initially paused
 dotnet run --project Grid.Terminal/Grid.Terminal.csproj
# A separate world in an existing folder
 dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --world C:\GRID\example.db --start
# Explicit recovery of that world's backup
 dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --world C:\GRID\example.db --recover
# Disposable session for demos; cannot combine with --world or --recover
 dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --ephemeral
```

### Completion verification

On 2026-10-18, `dotnet test Grid7736.slnx --verbosity minimal --no-restore` passed **200 tests**, zero failed/skipped: Core 8, Simulation 93, Terminal 50, Persistence 49. The final solution build also passed with zero warnings/errors. This adds 18 persistence cases and 11 terminal cases to the 171-test 2.1 baseline. Coverage includes real world round trips, revisions/resources/memberships/clock, rollback after an injected mid-transaction SQL failure, cancellation, corruption rejection, backup refresh failure, explicit recovery and damaged-file preservation, interrupted-recovery markers, exclusive sessions, periodic saves, shutdown ordering and visible failure results.

A built-terminal smoke test against an isolated temporary database created Genesis, stepped to tick 2, exited, reopened the same world paused at tick 2, rejected injected corruption, and recovered the same world/tick using --recover. --ephemeral --start also reported Running. The user's default world was not opened by this test. These checks do not simulate physical power loss, exhaustive filesystem failure, or promise preservation of changes after the last committed snapshot.

References: [Microsoft.Data.Sqlite backup API](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup) and [SQLite atomic commit and crash recovery](https://www.sqlite.org/atomiccommit.html).


## Existing-code adaptation plan - 2026-10-18

Coordinate classification-aware saves and versioned legacy migration with production startup replacement. Preserve the original schema v1 checksum, recoverable legacy data, transactions, backup publication and final-save ordering. After load or backup recovery, rediscover application residents rather than restoring them. Do not switch production startup while persistence still assumes every resident is durable.

See [existing implementation retrofit](Project_GRID_Host_Programs_and_Resources.md#existing-implementation-retrofit---planned-2026-10-18) and checklist 2.7.1. Documentation only; implementation remains pending.
