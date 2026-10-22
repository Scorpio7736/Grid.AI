# Project GRID — 5.6 VOLT and RELAY

## Bulk tasks — 5.8 completed (2026-10-22)

Agent-filtered lists and `tasks approve/run/clear -all [-AGENT]` are implemented. Bulk mutations/dispatch require original-host Master Control; approval and clear require reviewed interactive confirmation. Batches capture fixed selections, revalidate state/authority and stop dispatch on the first unsuccessful attempt. Clear archives records and cancels pending work, retaining audit/results and MCP/RELAY links; active/unsettled work is refused. `tasks list -archived` reads retained history. **Schema v20; 603 tests passed** (14 Core, 157 Simulation, 97 Persistence, 335 Terminal). Archiving does not free the 256-task history capacity. See [commands, verification and limits](Project_GRID_Bulk_Task_Implementation.md). Earlier milestone/schema/count statements below remain historical.


## End-to-end task verification — 5.7 completed (2026-10-21)

MCP delegation, human approval, real MAXIS unit-test execution and RELAY recorded-result delivery are verified together, including actual passing/failing TRX evidence and SQLite reload. Nine new cases also cover all six agent roles, cancellation, durable-start recovery and duplicate dispatch prevention. **560 tests passed** (14 Core, 157 Simulation, 97 Persistence, 292 Terminal), zero failed/skipped. No runtime/schema change was needed; schema remains v19. See [verification matrix, boundaries and terminal test](Project_GRID_End_to_End_Task_Verification.md). Next: **6.1 Shared local AI**. Earlier milestone counts and next-step statements below are historical.


## Implemented — 2026-10-21

VOLT reads the existing host telemetry feed and controls GRID's automatic simulation pacing through approved tasks. RELAY delivers approved messages and recorded task-result briefings to a saved local inbox, records delivery outcomes, supports human acknowledgement, and speaks a reviewed notification only after a separate approved speech task. No external messaging, automatic speech, Windows allocation changes or process-priority changes are introduced.

## VOLT monitoring and pacing

```text
volt
    - volt status
      # USAGE: Read the current pacing mode, multiplier, effective wait and timestamped host measurements.
    # EXAMPLE: volt status
```

```text
tasks
    - tasks propose VOLT volt.status current [key]
      # USAGE: Save a timestamped CPU/RAM/GPU telemetry briefing as a task result.
    - tasks propose VOLT volt.throttle normal [key]
      # USAGE: Restore the original automatic scheduler wait after approval.
    - tasks propose VOLT volt.throttle eco [key]
      # USAGE: Increase the automatic simulation wait to four times the base interval.
    - tasks propose VOLT volt.throttle auto [key]
      # USAGE: Adapt automatic pacing to fresh headroom measurements after approval.
    # EXAMPLE: tasks propose VOLT volt.throttle eco
```

MCP can delegate the same actions, e.g. `mcp request "VOLT: status current; VOLT: throttle eco"`. The default and migrated mode is normal, so existing worlds keep their prior timing until an approved change. Mode persists with the world. Master Control dispatches changes; VOLT cannot self-approve. Master Control, MCP and VOLT can read the direct status command.

Monitoring reuses the existing two-second host sampling loop and ten-second freshness rule. It reports CPU available percent (headroom, not utilization), RAM available/capacity, and GPU/VRAM observations when the configured provider supplies them. The current Windows adapter collects CPU/RAM and explicitly marks GPU/VRAM Unsupported; this module does not add a GPU collector or invent measurements. CPU scope may be the primary processor group. Missing, stale and unsupported data remain unknown. The short briefing shows at most six relevant readings with units/source/time; use resources details for all scopes.

Auto mode uses 4x wait when fresh CPU headroom is below 15%, fresh available RAM is below 10% of matching host capacity, or a supplied fresh GPU headroom reading is below 10%. Pressure holds the 4x wait for ten seconds after its last observation to reduce oscillation. With no pressure/hold, missing or stale CPU/RAM data uses a conservative 2x wait; otherwise 1x. Unsupported GPU alone does not force throttling when CPU/RAM are usable. Eco always uses 4x and normal always 1x. Waits are clamped to the scheduler's supported timer range.

Pacing applies to the next automatic wait and slows GRID's simulated progress relative to wall time. Tick duration, deterministic per-tick calculations, manual step, pause/resume and cancellation remain unchanged; delayed work does not replay catch-up ticks. Telemetry, autosave and user input retain their existing independent loops. This prioritizes host responsiveness by reducing GRID simulation frequency, not by reallocating Windows resources. MAXIS builds/tests, other explicit task adapters, model inference and GPU workloads are not dynamically governed by this initial pacing control. Broader workload throttling and performance guarantees remain future work.

## RELAY inbox and task-result briefings

```text
tasks
    - tasks propose RELAY relay.notify "message" [key]
      # USAGE: Deliver 1..256 printable characters to the local GRID inbox after approval.
    - tasks propose RELAY relay.brief <full-task-guid> [key]
      # USAGE: Copy a terminal task's actual recorded capability, status and result into an inbox notification.
    - tasks propose RELAY relay.speak <full-notification-guid> [key]
      # USAGE: Explicitly request local audio playback of a reviewed saved notification.
    # EXAMPLE: tasks propose RELAY relay.notify "Phase 5.6 notification test"
```

```text
relay
    - relay inbox
      # USAGE: List local notifications with short references and acknowledgement state.
    - relay inspect <notification-reference>
      # USAGE: Read the complete saved notification, source task and exact optional speech command.
    - relay deliveries
      # USAGE: Inspect inbox/speech attempts and Delivered, Failed, Cancelled or Interrupted outcomes.
    - relay ack <notification-reference>
      # USAGE: Human Master Control explicitly acknowledges a notification.
    # EXAMPLE: relay inbox
```

Master Control, MCP and RELAY can view the inbox/delivery records. Other agent roles cannot. Only human Master Control acknowledges messages. Direct command references accept a unique eight-or-more-character prefix or full ID; capability arguments for relay.brief and relay.speak require full GUIDs so the immutable approved request names an exact target.

A briefing requires a completed/failed/cancelled/interrupted source task with a recorded result; queued/running/unexecuted work is refused. It copies recorded evidence and retains the source task ID. It does not claim that an external operation succeeded beyond its original result, and remains a historical snapshot if the source task is retried. This provides the local result-return path for the later 5.7 end-to-end verification milestone. Messages are local inbox records, not email, Slack messages, OS toasts or remote transmissions.

## Explicit speech and delivery evidence

RELAY speech uses the existing installed offline Windows speech provider and the saved RELAY voice profile when present. The task inspector previews the exact notification text and warns that audio will play on the original host. No new microphone capture, online service or download occurs. An unavailable audio/voice provider fails the task and records the failure. Automated tests use a fake speech sink and do not play sound; actual device/voice availability is checked on delivery.

Speech requires its own relay.speak proposal, human approval and original-host tasks run. It is independent of the conversation-specific voice mute setting: approval of relay.speak is the explicit audio opt-in. There is no automatic speech on message arrival, status changes, restart or login. The 30-second task deadline and existing cancellation/logout/shutdown checks stop playback through the provider. Audio already heard cannot be undone; cancellation/interruption records that playback may have been partial. A deliberate retry needs fresh approval and may replay speech.

The broker saves the running task before invoking the adapter, then saves delivery intent before any audible output. Inbox and speech receipts include task/notification links, attempt number, UTC start/finish times, channel, status and bounded detail. Delivered to inbox means stored, not read; speech Delivered means the provider completed, not that someone heard it. Human acknowledgement is a separate explicit record. A save error can leave in-memory records; resolve persistence before continuing. Retries reuse an existing notification from the same originating task and add an attempt receipt rather than duplicating its text.

## Persistence and boundaries

Schema v19 adds the persisted VOLT mode and bounded RELAY state to WorldTasks. v18 worlds migrate to normal pacing and an empty inbox. Limits: 256 notifications, 512 delivery receipts, 4096 characters per notification, 512 per delivery detail, plus the existing 256-task/three-attempt broker limits. Capacity failures retain history rather than pruning it. Validation checks notification/task content/identity links, speech targets, attempt numbers, timestamps and acknowledgement identity. Snapshot copies protect collection ownership.

Restart preserves mode, notifications, finished deliveries and acknowledgements. Started delivery receipts become Interrupted and active tasks follow existing recovery; neither speech nor queued work automatically replays. Auto-mode pressure hold and live readings are transient and are recomputed after restart. Database backup, transfer and NGE naturally include/exclude these records with the world database; RELAY introduces no external artifact directory. Messages are saved as local text, not retained audio.

## Acceptance test

```text
agents initialize
volt status
mcp request "VOLT: status current; RELAY: notify Phase 5.6 test" volt-relay-test-1
tasks list
```

For each new task, replace TASK with its displayed short task reference:

```text
tasks inspect TASK
tasks approve TASK
tasks run
```

Then run:

```text
relay inbox
relay deliveries
mcp brief
```

Expect a VOLT result with actual freshness/unsupported states and one local inbox message. Use relay inspect NOTE and relay ack NOTE with the displayed notification reference. Acknowledgement should change from No to Yes. Nothing speaks during this test.

Optional pacing test: propose/approve/run `tasks propose VOLT volt.throttle eco`; `volt status` should show multiplier 4. Restore with a separately approved `tasks propose VOLT volt.throttle normal`. An optional auto-mode test uses `tasks propose VOLT volt.throttle auto`; the multiplier depends on current observations, not a fixed expected host load.

Optional briefing: copy the full GUID from a finished task's inspection, then propose/approve/run `tasks propose RELAY relay.brief FULL-TASK-GUID`. Its new inbox message should contain that recorded result.

Optional audio: inspect a notification and use the exact full-ID relay.speak proposal printed there, then inspect/approve/run that task. This intentionally plays sound if the installed provider/device works; otherwise relay deliveries should record the failure. Never substitute a short task reference for a full notification GUID in a speech proposal.

## Completion verification

**551 tests passed:** Core 14, Simulation 157, Persistence 97, Terminal 283. Sixteen new cases cover approval-before-pacing, unchanged manual steps, pressure/hold/stale/unsupported readings, inbox acknowledgement, actual-result briefings, speech success/failure/cancellation through a fake provider, durable intent barriers, deduplicated retries, role isolation, SQLite persistence, restart without replay, v18 migration, command presentation and forged delivery-link rejection.

Next: 5.7 end-to-end task verification. External notification channels, continuous push notifications, automatic speech, a GPU collector and broad task/model/OS resource control remain planned.
