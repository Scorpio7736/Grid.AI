# Grid.AI trust, personalization and usability acceptance

**6.38 terminal foundation implemented — 2026-11-19.** Grid.AI now consolidates its existing task, model, memory, resource and scheduler evidence under an original-host Master Control `trust` command. It also adds reviewed memory export, deny-first Trust Center profiles and explicitly enabled goal suggestions. Representative-user, physical second-device and optional GUI gates remain open.

## Trust Center commands

```text
trust overview
trust activity
trust memory
trust export plan "absolute-new.json"
trust export approve <id>
trust profiles
trust profile enable|disable <profile>
trust grant plan <profile> <agent>
trust grant approve <id>
trust revoke <profile> <agent>
trust goals
trust goal plan <name> <expiry-days|0> <text>
trust goal add <id>
trust goal enable|disable|remove <id|name>
trust suggestions on|off
trust suggest [agent]
trust receipts
trust acceptance
```

`trust activity` gives one readable view of agent activity, selected local model profiles, recent content-free inference diagnostics, task/tool progress and durable task audit, approval/cancellation counts, scheduler state and sampled resources. It links users to the detailed `tasks inspect`, `ai diagnostics details`, `schedule inbox` and `history` views rather than inventing correlations between separate records.

## Personalization and memory

The existing controls remain authoritative:

- `ai personas` reviews and selects versioned agent personas.
- `memory list`, `memory correct`, `memory expiry`, `memory forget`, `memory grant` and `memory revoke` preserve agent/project isolation.
- `trust memory` provides a content-hidden inventory across Master Control's world so scope, kind, expiry and grants are visible without printing private content.
- Memory export is an exact two-step operation bound to session generation and memory revision. It writes only a new absolute `.json` file, refuses overwrite, shows record count/size/hash first, expires after two minutes and includes a contract identifier and grants. The resulting private copy is controlled by the user and is not automatically deleted.

## Permission profiles and suggestions

Three immutable capability templates are supplied disabled and with no agents:

| Profile | Capability meaning |
| --- | --- |
| `goal-advisor` | May propose on-demand text suggestions from explicitly enabled local goals |
| `workspace-reviewer` | Documents the reviewed context/read and file-draft proposal pattern |
| `task-coordinator` | Documents the world-read and task-proposal pattern |

Each individual agent grant needs a two-minute preview and exact approval. Revocation is immediate. These profiles are Trust Center policy templates—not OS permissions—and never grant files, context, devices, processes, task approval, execution or auto-approval. Existing skill, connection, workspace, context, device and task controls remain separate and stronger.

Suggestions are off by default. Goals are added disabled after an exact preview. A suggestion is available only when the global switch is on, the goal is enabled and unexpired, `goal-advisor` is enabled and the selected agent has an individual grant. Suggestions run only when the user enters `trust suggest`; there is no background monitoring, AI inference, notification loop or automatic action. Output labels itself as a deterministic prompt rather than a fact.

## Safe export, removal and troubleshooting

- Before moving or removing Grid.AI, exit normally, retain a verified world/package backup and use the reviewed memory export only if a separate human-readable copy is wanted.
- Removing the application should not be confused with `NGE`; NGE intentionally destroys the selected world save. Do not use it for ordinary uninstall.
- Revoke Trust Center grants, disable suggestions/goals/profiles and review `trust receipts` before handing a package to another person or device.
- `trust overview` shows whether AI, schedules, suggestions and profiles are active. `trust acceptance` separates automated capability checks from manual and future gates.
- If trust state is malformed or its immutable profile catalog was edited, Grid.AI fails closed for Trust Center operations instead of accepting changed capabilities. The original file is retained for diagnosis.

## Acceptance protocol still requiring people/devices

The following cannot be truthfully completed by unit tests alone:

1. Ask representative new users to launch/login, inspect help, start GRID, connect to MCP, request a realistic file/application task, review its plan, approve it, inspect progress/result, recover from an injected failure and revoke access.
2. Record time to first actionable response, completion rate, points of confusion, recovery success, color/keyboard/readability accessibility findings and whether users can locate help, activity and revocation without coaching.
3. Repeat the essential workflow on supported Windows hardware with networking blocked after provisioning.
4. Repeat from a verified Flash Grid.AI package on a second supported machine and confirm world identity, local AI, memory isolation and grants.
5. Retest any future GUI separately. Future Plan C is optional and is not implemented by 6.38.

`trust acceptance` prints this boundary in the terminal. It never turns a manual gate into a pass.

## Automated verification

Focused tests cover disabled profiles/suggestions, exact reviewed grants, immediate revocation, disabled-first goals, on-demand goal-bound suggestions, no task creation, reviewed new-file-only memory export, hash reporting, readable activity, explicit manual-gate reporting, durable settings and fail-closed capability tampering.

**Automated evidence — 2026-11-19:** Five focused Trust Center cases pass. The complete Grid.AI solution passes **1,551 tests, 0 failed, 0 skipped** (Core 14, Simulation 163, Persistence 107, Terminal 1,267).
