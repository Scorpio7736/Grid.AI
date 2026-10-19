# Project GRID - Persistence architecture and schema v1

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
