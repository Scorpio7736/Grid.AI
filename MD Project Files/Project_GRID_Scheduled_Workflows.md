# Grid.AI scheduled and persistent workflows

**Implementation status — 6.37 foundation complete, 2026-11-19.** Grid.AI now has a durable local host scheduler for human-configured one-time, recurring and conditional jobs. It is intentionally independent of simulated city ticks and local AI availability.

## What is implemented

- Original-host Master Control can prepare a two-minute exact plan, add it disabled, inspect it, explicitly enable/disable/cancel/remove it, set UTC quiet hours and set expiration.
- Supported schedules are one-time, recurring and conditional. Initial conditions are `always`, `world-running` and `world-paused`.
- Supported actions are bounded and offline: `world.status`, `resources.status` and a local `notify` inbox message.
- Durable state records owner, world, local scope, schedule, next/last run, result, enable/cancel state, bounded inbox items and content-free receipts.
- The host checks due work every 30 seconds. It attempts at most eight due jobs in one pass. A late recurring job runs once and is scheduled forward from the wake time; missed intervals are never replayed in a catch-up loop.
- A durable active-run marker is written before execution. If startup finds an uncertain active run, it records `Interrupted`, disables the job and does not replay it. Master Control may explicitly retry a failed/interrupted job once.
- Transfer preparation pauses the service. Attached terminals, remote clients, agent conversations and model output cannot configure or enable schedules.

## Safety boundary

This foundation does not run arbitrary terminal commands, agents, model inference, files, processes or network requests. It cannot approve tasks or perform consequential host changes. Those operations require their existing current-session review and approval path. Conversation suggestions never create or enable jobs.

Jobs are limited to 64; inbox items to 256; receipts to 500; arguments to 300 characters and results to 1,000 characters. State is atomically replaced and limited to 2 MiB. Ownership, saved-world binding, enabled/cancelled state, expiration, quiet hours and condition are revalidated at every run.

## Terminal use

```text
schedule list
schedule plan once <name> <ISO-8601-UTC> <world.status|resources.status|notify> [text]
schedule plan recurring <name> <minutes> <action> [text]
schedule plan conditional <name> <minutes> <always|world-running|world-paused> <action> [text]
schedule add <plan-id>
schedule show <id|name>
schedule enable|disable|cancel|remove <id|name>
schedule quiet <id|name> <start-hour-utc> <end-hour-utc>
schedule expiry <id|name> <ISO-8601-UTC>
schedule inbox [read]
schedule retry <id|name>
schedule run-due
schedule doctor
```

Example:

```text
schedule plan recurring hourly-health 60 world.status
schedule add <plan-id-shown-by-GRID>
schedule show hourly-health
schedule enable hourly-health
schedule inbox
```

## Verification and limits

Automated tests cover disabled-by-default creation, explicit enablement, one-time execution, persistence, ownership/world binding, late recurring wake without catch-up, conditional execution, quiet-hour deferral, expiration, cancellation, local notification, unsupported-action refusal, uncertain-run restart recovery, no replay, explicit retry and session-bound terminal planning.

**Automated evidence — 2026-11-19:** Seven focused scheduler cases pass. The complete Grid.AI solution passes **1,546 tests, 0 failed, 0 skipped** (Core 14, Simulation 163, Persistence 107, Terminal 1,262).

The interruption test writes a durable active-run marker and reopens the service, modeling abrupt loss after the start marker. It is not evidence of physically removing power. Since supported actions are local and offline, disconnected network/device state does not block them. Future expansion to agent work or consequential actions must add a new typed action with contemporaneous approval; this foundation does not grant that authority.
