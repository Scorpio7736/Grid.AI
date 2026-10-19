# Project GRID - Persistence architecture and schema v1

**Section 2.1 implemented and verified - 2026-10-18.** This is the SQLite database foundation. Saving/loading an engine world, periodic snapshots, recovery workflows and terminal startup integration remain section 2.2 and later work.

## Project and connection boundary

Grid.Persistence now replaces its empty Class1 scaffold with SqliteGridDatabase and GridSchema. The existing Microsoft.Data.Sqlite 10.0.13 dependency is retained. Grid.Persistence and Grid.Persistence.Tests are included in Grid7736.slnx; neither the terminal nor the simulation engine is wired to persistence yet.

SqliteGridDatabase takes an explicit filesystem path, normalized to an absolute path at construction. It rejects empty paths, SQLite file URIs and the in-memory special name. The parent folder must already exist. Connection strings use SqliteConnectionStringBuilder, not interpolation of user input.

- `Initialize()` explicitly creates an empty schema or migrates a recognized existing GRID database. It does not create a world, seed citizens or mark GRID-REQ-004 generation complete.
- `OpenExisting()` requires an existing, initialized file, validates/migrates recognized versions and returns an open SqliteConnection. The caller must dispose it. A missing or empty file is not silently initialized.
- Each connection enables and checks foreign-key enforcement, uses private cache, disables pooling and selects FULL synchronization. Busy operations have a five-second default timeout. Journal mode is not changed; WAL tuning is deferred until persistence workloads justify it.
- Callers group related SQL writes in an explicit transaction and commit only on success. Disposal without commit rolls back. SQL values must use parameters. Returning a connection is a trusted application boundary, not an authorization API or support for arbitrary terminal SQL.
- Connections/transactions must not be shared concurrently between operations. The persistence layer does not hold engine locks across disk I/O; section 2.2 must define consistent snapshot capture and commit coordination.

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

IDs use canonical GUID text by application convention. Database checks require nonempty/nonzero 36-character IDs, but do not fully parse GUID syntax. Text checks establish nonempty names/occupations/keys; they do not replace the engine's exact UTF-16/control-character editing rules. UTC strings use round-trip formatting by application convention, with no timezone parser in the schema. Section 2.2 must validate full domain invariants on serialization and rehydration.

## Durable world identity versus runtime identity

Worlds.WorldId is the planned persistent world identity, not GridWorld.InstanceId, which identifies a running instance and scopes edit previews. Reopening a future saved world must create fresh runtime/session bindings. Persisted program revisions must never revive a pre-restart preview or authentication session.

Clock storage uses integer .NET TimeSpan ticks rather than rounded wall-clock seconds. WasRunning records saved state; it does not itself define automatic-resume policy. Loading must not add elapsed wall time during shutdown. Snapshot capture, restoration APIs and restore/start behavior remain section 2.2 decisions.

## Migration policy

GridSchema.CurrentVersion is 1. PRAGMA application_id identifies GRID (`0x47524944`); PRAGMA user_version stores the committed schema version. SchemaMigrations records each applied migration's name and checksum. SQL checksums normalize CRLF to LF so identical migrations remain portable across checkout line endings.

The migration runner acquires an immediate transaction before inspecting versions. Concurrent first initializers serialize rather than both applying v1. For a new file, initialization is permitted only when application/version markers are zero and no user schema objects exist. Foreign applications, unversioned nonempty databases and future schema versions are rejected without replacing their content.

For existing versions, history must be contiguous and match the bundled migration names/checksums. The entire pending migration chain, history entries and version/application markers commit together. Failures roll back DDL and markers as well as data changes. A foreign_key_check runs before commit. SQLite read/format errors propagate; there is no automatic deletion, reset, downgrade or repair.

Add future migrations with contiguous version numbers and increment CurrentVersion. Never edit the SQL/name of a released migration; make a new migration and test populated older databases. Production includes only v1 today. Tests use synthetic v2 migrations to verify upgrade and rollback mechanics; they do not establish a released v2 schema.

History validation is not a full schema-tamper detector or crash-recovery system. Missing/modified tables outside managed migrations and damaged storage may require additional integrity/recovery checks in section 2.2. Do not label this foundation as completed backup, migration recovery UX or power-loss testing.

## Planned extensions

- **2.2:** atomic engine snapshot save, domain rehydration, periodic saves, restore policy, backups and recovery. The current application still creates a fresh in-memory Genesis world per launch.
- **2.3:** durable world events and administrative receipts, retention, querying and write-failure policy. The 256-entry live buffer remains transient and is not automatically flushed to SQLite.
- **GRID-REQ-003:** operator identities, roles and audit records need separate migrations and service authorization. Do not persist live leases, credential material or reusable session previews in world-state rows.
- **GRID-REQ-004 / 2.6:** hardware profile, generated layout, component associations and initialization provenance require future versioned tables. Nullable seed/version columns only reserve basic provenance; they do not implement hardware discovery, procedural generation or durable city reuse. Existing worlds must survive hardware changes.

## Verification evidence

On 2026-10-18:

- `dotnet test Grid.Persistence.Tests/Grid.Persistence.Tests.csproj --verbosity minimal -p:NuGetAudit=false`: 31 passed.
- `dotnet test Grid7736.slnx --verbosity minimal -p:NuGetAudit=false`: 171 passed, zero failed/skipped (Core 8, Simulation 93, Terminal 39, Persistence 31).
- `dotnet build Grid7736.slnx --no-restore --verbosity minimal`: zero warnings/errors.

Tests use isolated temporary file-backed databases. Coverage includes initialization/reopen without row loss, per-connection foreign keys, stored-value constraints, same-world membership, cascades, caller rollback, failed initial/upgrade migration rollback, synthetic upgrade idempotence, missing/empty/foreign/future/invalid files, history disagreement, foreign-key violations and concurrent initialization. Row round trips are SQL-level checks, not a claim that an engine world can yet be saved/reloaded.

API behavior was checked against [Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions), [connection settings](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings), and [SQLite PRAGMA documentation](https://www.sqlite.org/pragma.html).

See [Master Development Checklist](Project_GRID_Master_Development_Checklist.md) for remaining Phase 2 work.
