# Project GRID - Hardware-generated city creation

## Program behavior completion - 2026-10-19

**3.1 implemented and verified.** Non-demo residents now have deterministic needs/goals/actions. Applications reflect fresh or unknown observations; explicitly configured Agent/Special residents perform resource-gated simulated occupation work/rest on simulation-time shifts. Schema v5 persists their routines, sourced budgets and progress without restoring live observations or adding offline work. Use behaviors and inspect for last-tick behavior. No OS task execution, movement or named agent services are claimed.

See [Program Needs and Behavior](Project_GRID_Program_Behavior.md) for policy, APIs, limits and completion evidence: **278 Release tests passed**, build zero warnings/errors, disposable terminal smoke passed. Debug output was locked by running user sessions, which were left running. Earlier dated deferred-behavior/schema statements describe previous milestones; existing history and completed scope are preserved.

## Application resident completion - 2026-10-19

**2.7 / 2.7.1 implemented and verified:** production population now follows current-session windowed executable groups through read-only discovery and serialized lifecycle reconciliation. Application residents are transient; Agent/Special residents persist in schema v4. Before production conversion, legacy fictional citizens are preserved in a uniquely named validated archive. City/layout and clock remain intact. Unknown resource requirements display unknown condition; no host-control authority is implied.

Verification: **268 tests passed**, build zero warnings/errors, and a disposable Windows startup/reopen detected nine application groups. See [Application Residents Implementation](Project_GRID_Application_Residents_Implementation.md) for grouping limits, discovery/freshness, storage/migration, editing, resource interpretation and completion evidence. Earlier dated statements that 2.7 is planned, production cities stay empty, or all residents persist describe previous milestones. Agent delegation/services, login, rendering and additional per-application telemetry remain future work.

**Section 2.6 / GRID-REQ-004 implemented and verified - 2026-10-19.** This is authoritative headless city data, not a rendered 3D city. Application population, classification and resource/energy binding remain pending in 2.7.

## Creation and reuse

Normal startup uses `WorldStartup.LoadOrCreateAsync`. Under the existing exclusive WorldStore lease, an existing database is validated and loaded without calling discovery or generation. A genuinely absent world with no backup, recovery marker or SQLite sidecars can be created. Corrupt, incompatible and incomplete files are errors, never permission to regenerate. Existing Genesis worlds keep their five districts, twenty citizens, identities, edits, history and clock.

For a new world, collect a profile, choose a cryptographic random seed, generate a new world ID and semantic layout, and commit the initial snapshot before reporting creation. Later launches load the saved blueprint directly, including after hardware changes or backup recovery. No offline time is added; startup stays paused unless `--start` is supplied. New cities have six districts and **zero residents** until application population is implemented. The original fixed factory remains available for tests and `--ephemeral` demonstrations.

To deliberately create a separate world, use `dotnet run --project Grid.Terminal -- --world "C:\path\to\new-world.db"` with a new filename. This does not reset the existing world or its history. `city` reports saved generator/seed, geometry, discovery coverage and capacity/unit field counts; it does not query hardware or expose raw model labels. Existing legacy worlds report that their layout is preserved.

## Discovery and normalization

`IHardwareDiscovery` is a Core boundary. The Terminal Windows adapter launches a hidden local Windows PowerShell collector with a constant encoded script. It queries local CIM only, with a three-second query timeout and 25-second overall budget, concurrent bounded output reads and owned-process cleanup. Non-Windows hosts report Unsupported; failed collection reports Unavailable. There is no elevation requirement, network peer scan, user-file access, serial-number or MAC collection.

| Category | CIM source / retained fields | City district and lot use |
|---|---|---|
| Motherboard | Win32_BaseBoard manufacturer/product | The Core / Administration |
| CPU | Win32_Processor name/logical processor count | Processing Sector / Workshop |
| GPU | Win32_VideoController name | Visual Compute / Studio |
| RAM | Win32_PhysicalMemory manufacturer/part number/capacity bytes | Memory Banks / Research |
| Storage | Win32_DiskDrive model/size bytes | Archive Sector / Archive |
| Network | Win32_NetworkAdapter name/physical-adapter flag | Gateway / Communications |

All six categories retain collection time, source, status and notes. Available means devices were returned; NotDetected means a successful empty query; Unavailable means no trustworthy query result; Unsupported identifies an unavailable platform adapter. Individual unknown capacities/units stay null. GPU capacity is intentionally omitted because the CIM AdapterRAM field is a uint32 and is unsuitable for reliable modern VRAM capacity. Integrated/discrete GPU rows are retained without inventing a classification. Network virtual flags are recorded where provided; virtual adapters are retained. This inventory is a creation snapshot, not current usage telemetry or a claim about physical motherboard coordinates.

Normalization orders categories and components canonically, uses Unicode normalization and bounded sanitized labels, preserves repeated models as separate devices, and retains at most 32 components per category with an explicit truncation flag. Capacities are positive bytes; processor units are positive logical counts. The SHA-256 profile fingerprint excludes collection time and diagnostic text. Stored labels are local provenance, not credentials or authorization evidence.

Local-query semantics: [Microsoft Get-CimInstance documentation](https://learn.microsoft.com/en-us/powershell/module/cimcmdlets/get-ciminstance?view=powershell-5.1). GPU field constraints: [Microsoft Win32_VideoController](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-videocontroller).

## Versioned bounded generation

`hardware-city-v1` uses the profile fingerprint, seed and generator version with SHA-256 and an explicit xorshift PRNG. Equal normalized profile/seed/version yields equal initial geometry and district IDs; world IDs remain independently unique. Different hardware influences the hash and capacities influence density, but global visual uniqueness is not guaranteed.

Six 112-by-112-metre districts occupy a 3-by-2 arrangement, with centres 160 metres apart. Each has 4–16 nonoverlapping 16-by-16-metre lots. A six-edge perimeter road loop and optional middle connection are eight metres wide; central corridors remain clear. Lot count is clamped from `4 + min(device count, 4) + floor(log2(1 + capacity GiB)) + processor units / 4`, with bounded capacity/unit inputs. Missing categories still receive four fallback lots. Component keys associate every retained device with its district. No hardware count creates residents or unbounded assets.

CityBlueprint validates geometry, unique IDs/categories, component associations, profile fingerprint, canonical inventory and connected streets. It stores immutable JSON and returns detached read copies. This geometry supports later street-level art direction, but rendering, facades, atmosphere, fashion, animations and pathfinding remain future work. Live load cannot reshape the saved city.

## Persistence and failure behavior

Schema v3 appends WorldGeneration with a world foreign key and bounded valid BlueprintJson. Migrations v1/v2 remain unchanged. The blueprint carries the actual districts, lots, roads, profile, associations, seed and generator version; WorldInfo retains stable world identity and generation metadata. These save in the same transaction as simulation state and entities. Save rejects removal or replacement of an established blueprint and mismatched metadata/district membership. Legacy worlds need no invented hardware provenance.

Cancellation before creation produces no world. SQLite write failure rolls back world, district, program, state and generation rows. Schema initialization may leave an empty database; subsequent startup fails visibly rather than silently regenerating it. Keep failure artifacts for diagnosis/recovery, or select a new path explicitly. Existing backup/recovery and exclusive-writer protections also cover generated worlds. Physical power-loss guarantees remain limited by SQLite, the OS and storage hardware.

## Completion verification - 2026-10-19

- `dotnet test`: **250 passed** (14 Core, 106 Simulation, 74 Persistence, 56 Terminal). This adds 19 focused cases to the 231-test 2.5 baseline.
- Deterministic fixtures cover canonical ordering, seed/profile sensitivity, fallback categories, repeated/many components, bounded geometry, immutable copies and invalid provenance/layout rejection.
- Persistence cases cover create/save/reopen/recovery without rediscovery, legacy preservation, corrupt/backup/sidecar/recovery-marker refusal, cancellation and competing creation attempts. A test-only connection configuration forces genuine SQLITE_FULL using SQLite's maximum-page limit: creation rolls back completely and later startup refuses the incomplete file. This does not claim a physically exhausted disk was tested.
- Existing abrupt-process-termination/journal-recovery and older-schema tests also pass against schema v3. Abrupt termination is tested during an update; failed initial creation is separately tested through transactional SQLITE_FULL rollback.
- Windows disposable-world smoke: all six categories available; CPU unit field, two RAM capacity fields and one storage capacity field present. Six districts, six streets and 56 lots persisted; close/reopen retained world ID, seed, geometry and tick 1, paused with zero programs. No default user world was changed.
- `dotnet build --no-restore`: zero warnings and zero errors. No Godot rendering, hardware-upgrade automation, application residency, login or authenticated editing implementation is claimed.
