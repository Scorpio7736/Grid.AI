# Project GRID - History and observability

**Section 2.3 implemented and verified - 2026-10-18.** This extends the current Genesis runtime. GRID-REQ-005 application discovery, resident classification and resource interpretation remain planned in 2.4/2.7; history does not implement those features.

## Captured events

The engine records meaningful committed changes: clock start/pause, direct district transfers, changing administrative edits, energy changes and district resource changes through `SetDistrictResource`. Ordinary tick-completed messages remain in live view but are excluded from durable history. No-op resource assignments and unchanged/no-op/failed edits do not create committed-change events. The resource API updates fictional district state, not Windows allocations; external callers must not mutate an engine-owned world directly.

Each record contains a stable event GUID, observation UTC timestamp, simulation tick/time, kind, severity, optional entity ID/name, detail and JSON context. Transfer context includes source/destination district IDs; energy/resource context includes before/after values; changing edits include request/operator/session attribution, revisions and formatted field changes. This is committed-change history, not a comprehensive security audit of denied requests, no-ops or all attempted operations. No authenticated terminal login or host-app lifecycle events are invented.

Entity references deliberately have no foreign key to active program rows. A future application can disappear while its historical events remain queryable. Querying or restoring events never creates or replays residents. Schema v2 also keeps history independent of the current snapshot-replacement deletes; one world per file remains the application rule.

## Commit, failure and retention policy

Schema migration 2 adds `WorldEvents` and world/time/entity indexes without editing migration 1. Existing v1 files upgrade transactionally on open. History and the captured world state are written in the same transaction on the existing 30-second autosave and final shutdown save. After successful save/backup publication, the engine acknowledges only the captured event prefix; newer events remain pending. Stable event IDs make a retry after a committed primary save but failed backup refresh idempotent.

The pending journal is separate from the 256-entry live-view buffer and holds at most 10,000 meaningful events. Overflow discards the oldest pending entries and includes an explicit `HistoryGap` Warning with a dropped count in the next captured batch. This is bounded, best-effort history, not a lossless audit stream. Capture/acknowledgement methods are trusted persistence orchestration APIs; callers must acknowledge only a successfully saved batch from that engine.

Abrupt termination can lose pending events since the last committed snapshot. Transaction failures preserve the previous saved state/history and leave pending entries for retry. Recovery restores the history contained in the selected backup and can lose newer primary commits; damaged files remain archived under the existing recovery policy. No automatic disk-history pruning, archive quota or full security-audit retention policy is implemented yet. Database growth and high-volume energy changes require later measured retention/aggregation work.

Direct `WorldStore.Save` callers must provide the captured event batch to persist history; `WorldPersistenceSession` does this automatically. The low-level state-only API remains available for trusted callers/tests. Resource changes made outside the engine API are not observed retroactively. Ordinary process-local live events/sequence numbers still reset on restart, while committed history survives.

## Querying history

The persistent terminal registers:

```text
history
history kind=Transfer limit=20
history severity=Warning
history entity=<full-guid> limit=50 offset=50
history since=2026-10-18T00:00:00.0000000+00:00
```

Filters can be combined. `since` accepts a round-trip timestamp; kind/severity matching ignores case. Results are newest recorded UTC first, with insertion order breaking ties. Defaults: 50 rows, offset 0; maximum 500 rows per query. Offset pagination is a live query, not a fixed transaction spanning multiple commands. Values are SQL parameters; terminal control characters are sanitized. Queries read saved history only, so pending changes appear after autosave or the next launch after orderly exit. `--ephemeral` has no saved-history command.

The `HistoryQuery` API provides the same kind, entity, severity, since, limit and offset filters. History does not grant access beyond the existing local terminal; login-based filtering remains future work.

## Structured operational diagnostics

Scheduler, autosave, shutdown-save and top-level startup/session failures emit a JSON diagnostic alongside the readable error: UTC timestamp, Error severity, component, operation, optional world ID, exception type and message. Startup/top-level diagnostics use stderr; host lifecycle diagnostics use the host output writer. These records remain observable even when SQLite is unavailable. They are not automatically stored in the failed world database or a separate rotating log file; a caller can capture output as needed.

## Completion verification

- `dotnet test Grid7736.slnx --no-restore --verbosity minimal`: **208 passed**, zero failed/skipped (Core 8, Simulation 93, Persistence 56, Terminal 51).
- `dotnet build Grid7736.slnx --no-restore --verbosity minimal`: zero warnings/errors.
- Added eight cases covering history across repeated saves/reopen/recovery, entity/kind/time/paging queries, transaction failure and retry, backup failure deduplication, acknowledgement ordering, independent pending/live buffers and overflow warnings, populated v1 migration, inactive entity references and terminal argument validation.
- Isolated terminal smoke: start/pause/step/exit, reopen the same world, query two saved Clock events, and verify no Tick records. No user default world was opened for this verification.

The prior 200-test result remains historical evidence for 2.2. Next work is 2.4 resources/host state and the 2.7 resident retrofit; these completion notes do not mark them implemented.
