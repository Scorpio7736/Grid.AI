# Local app and service connections

Completed 2026-11-19 as the offline-first 6.36 connection foundation. The current real adapter is `local-task-board`, a user-selected JSON task list. `connections providers` also discloses calendar, email, external-task and browser-data provider classes as unavailable: GRID does not install, authenticate, contact or simulate those services.

## Local task-board format

```json
{
  "Version": 1,
  "Items": [
    {
      "Id": "11111111-1111-1111-1111-111111111111",
      "Title": "Example task",
      "Completed": false,
      "UpdatedUtc": "2026-11-19T12:00:00+00:00"
    }
  ]
}
```

Use `connections plan local-task-board <alias> "<absolute-json>"`. GRID validates the bounded source and shows its exact path, SHA-256, scopes, transport and credential policy. Nothing is connected until `connections add <plan-id>` within two minutes. The new connection is disabled; use `connections show <alias>`, then `connections enable <alias> <source-hash>`.

`connections list/show/status/providers/history` disclose available providers, exact scopes, enabled state, source/account boundary, transport, agent sharing, freshness and failures. The local adapter stores no credential and makes no network call. The world-adjacent catalog contains connection metadata and content-free receipts, not task titles.

## Reads, grants and changes

`connections read <alias>` reopens the source every time. Output states local/offline transport, observation time, source path, current hash and per-task citations (`alias:item-id`). GRID never labels cached content as live because this adapter has no content cache. Missing, corrupt or changed sources are reported.

Master Control may grant only read access with `connections grant <alias> <agent-role>` and revoke it immediately. Agents see/read only enabled connections granted to their exact registered role while agents are running. They cannot add providers, configure scopes, draft, approve, commit, disable or remove connections. Changing machine, world, catalog path or owner credentials disables connections and clears grants.

Writes use three distinct steps:

1. `connections draft-add <alias> "task title"` creates a two-minute in-memory preview and records the source hash. The source is unchanged.
2. `connections approve <draft-id>` approves only that exact preview in the same identity session. The source is still unchanged.
3. `connections commit <draft-id>` rechecks connection authority and the source hash, retains a before-image beside the catalog, then atomically replaces the task-board file.

Logout/role change/restart discards pending plans and drafts. A changed source refuses commit without retry. `connections disable` revokes grants. `connections remove` removes GRID metadata but deliberately does not delete external data or retained recovery artifacts.

## Security and limits

- Original terminal only; no attached, conversational, remote, scheduled or model-driven invocation.
- Maximum 32 connections, 1 MiB per task board, 1,000 tasks and 100 content-free receipts.
- Absolute existing local `.json` sources only; links/junctions and unknown JSON members are refused.
- No passwords, tokens or OS credential-store entries exist for the local adapter.
- No cloud provider is a mandatory dependency. Optional providers stay unavailable until a separately reviewed adapter, account consent, credential storage and revocation implementation exists.
- The current mutation is task addition only. Sending email, deleting data and scheduling calendar events are not implemented and cannot be inferred from chat or connector metadata.

Six focused connection cases and the full solution pass at **1,529 passed, 0 failed, 0 skipped**. Tests cover consent/add/enable, fresh citations, bounded receipts, read grants and cross-agent denial, revoke/device rebinding, disabled/missing/corrupt/oversized sources, non-destructive removal, draft/approval separation, stale-source refusal, atomic commit and retained before-image. These are local automated tests, not live cloud-account or second-device evidence.
