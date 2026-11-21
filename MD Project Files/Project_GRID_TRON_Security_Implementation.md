# Project Grid.AI — 5.4 TRON Security

## Implemented — 2026-10-21

TRON provides three real, local, read-only Windows observations through the existing human-approved task broker. It does not launch scans, change firewall/Defender settings, quarantine/delete files, clear event logs or request elevation. Sensitive remediation capabilities are deliberately rejected; any future implementation must define a separate scoped proposal, human approval and verification policy first.

## Test workflow

```text
agents initialize
mcp request "TRON: defender status; TRON: firewall status; TRON: events recent" tron-check-1
tasks list
```

For each displayed task reference, review and approve it individually:

```text
tasks inspect <task-reference>
tasks approve <task-reference>
tasks run
```

The original host terminal dispatches the next queued task. Repeat for all three children, then use `mcp brief` or `mcp inspect <plan-reference>`. Reusing tron-check-1 deduplicates the exact same request/actor; use a new key for a fresh observation. Displayed results are timestamped snapshots, not live monitoring. Normal chat/voice remains conversational.

Direct alternatives:

```text
tasks
    - tasks propose TRON tron.defender status [request-key]
      # USAGE: Read protection flags, running mode, signature timestamp and previous scan end times.
    - tasks propose TRON tron.firewall status [request-key]
      # USAGE: Read effective firewall profile settings from ActiveStore.
    - tasks propose TRON tron.events recent [request-key]
      # USAGE: Read at most ten newest Security-log event metadata records from the past 24 hours.
    # EXAMPLE: tasks propose TRON tron.defender status defender-check-1
```

Only the literal arguments status (Defender/firewall) and recent (events) are accepted. There is no arbitrary PowerShell, remote computer, log name, query, script or path parameter. MCP can delegate through these same capabilities; TRON can propose its own work but cannot approve it or borrow another role's capabilities.

## Observations and interpretation

Every result starts REAL HOST SECURITY - READ ONLY, identifies its Windows source, and records the observation UTC time. Results live in task records/audit and MCP briefings, separate from fictional city incidents; they do not alter simulated security state.

Defender reports AntivirusEnabled, RealTimeProtectionEnabled, AMServiceEnabled, AMRunningMode, signature update time, and the last quick/full scan end times when available. Missing/unrecorded dates are Unknown, never invented. A disabled protection component produces a factual warning with a prompt to review Windows Security and any third-party antivirus. No scan is started, and an enabled status is not a malware-free guarantee.

Firewall reads the ActiveStore profiles, reports enabled state and default inbound/outbound actions, and warns about disabled or missing profiles. This is profile configuration, not a complete rule audit or proof of network isolation. Unknown flags stay Unknown.

Events reads only Security-log ID, record ID and UTC timestamp. The window is the previous 24 hours with a hard maximum of ten newest entries; messages, account names, IP addresses and payloads are not collected or saved. An empty query is not proof of safety. This bounded sample does not classify individual records as attacks. Security-log access may be unavailable under the current Windows account.

Access denied, missing services/modules, unsupported hosts and provider failures are explicitly unknown/unavailable. The query can complete successfully with an UNAVAILABLE observation: Completed means the observation was recorded, not that the PC is healthy. Invalid/malformed/oversized provider data fails the task instead of producing a healthy result. Grid.AI does not ask for Windows elevation or bypass access restrictions.

## Execution, persistence and safety boundaries

A fixed absolute Windows PowerShell executable runs with NoProfile, NonInteractive and a fixed encoded script; user text never enters the script. Defender and NetSecurity modules are explicitly loaded from the system module directory. Commands are local read-only Get-MpComputerStatus, Get-NetFirewallProfile -PolicyStore ActiveStore and Get-WinEvent. No network download, model or online service is required.

Task execution still waits for individual human approval and a successful durable-start save. Concurrent dispatch is blocked; cancellation, logout and shutdown retain the existing authorization checks. TRON has a 30-second deadline with process-tree termination on cancellation; demo/MAXIS deadlines remain unchanged. Output streams are bounded to 32 KiB; summaries are bounded to the existing 2048-character saved-result limit. Only the selected metadata/summary is persisted; no new external artifact directory is created.

Schema v17 marks the expanded capability contract so older binaries cannot silently accept it. Existing worlds migrate without regeneration. Results/audits use the existing transactional WorldTasks record and database backup/transfer path. Restart preserves completed observations, clears queued approval and interrupts running work without automatic replay. Observation dates remain historical after transfer/restart.

Migration-history labels and checksums are immutable save-format data. The November 21, 2026 TRON rename therefore preserves migration 17's original recorded label internally while exposing only TRON in current source behavior, commands and documentation. A real-SQLite regression initializes and reopens the historical record to prevent another rename from making an existing world appear incompatible. No recovery, regeneration or manual database edit is required.

## Completion verification

**518 tests passed:** Core 14, Simulation 157, Persistence 97, Terminal 250. Thirteen new cases cover approval and durable-start fencing, unavailable observations, rejected remediation/arbitrary input, missing flags/dates, firewall warnings, bounded event metadata, malformed provider failure, role isolation, MCP delegation, actual local Windows provider queries and SQLite result recovery. The live provider smoke test accepts either structured data or explicit unavailability; it does not assert that the current machine is healthy.

**TRON rename compatibility verification — 2026-11-21:** **1,613 tests passed** (Core 14, Simulation 163, Persistence 108, Terminal 1,328). Focused persistence checks verify migration 17's immutable history name across initialization and reopen, while saved task/resident state is normalized to TRON before validation. A current-reference scan found no former-name occurrences in source or Markdown files.

Next: 5.5 JN — Storage and files. Continuous monitoring, event threat classification, third-party antivirus inventory, scans and remediation remain future work.

## API references

- [Microsoft: Get-MpComputerStatus](https://learn.microsoft.com/en-us/powershell/module/defender/get-mpcomputerstatus?view=windowsserver2025-ps)
- [Microsoft: Get-NetFirewallProfile](https://learn.microsoft.com/en-us/powershell/module/netsecurity/get-netfirewallprofile?view=windowsserver2025-ps)
- [Microsoft: Get-WinEvent](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.diagnostics/get-winevent?view=powershell-5.1)
