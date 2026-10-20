# Project GRID - Chat workflow and agent handoff

## Terminal commands and shutdown complete - 1.12 (2026-10-19)

Implemented grouped `help`, display-only `clear`, `programs -active`, `programs -possible`, `programs -sleeping`, and a GRID7736 ASCII farewell. Active selects fresh Running applications; sleeping selects fresh On call or Sleeping applications; possible includes current applications plus the saved known-application catalog. `ids` works with one filter. Unknown/stale observations are never classified as active or sleeping. Bare `programs` still includes agents and other residents.

**Schema v6** adds a separate ApplicationCatalog table (key, name, source, last catalog observation UTC). The catalog is captured with the simulation snapshot and saved atomically with world/history; backup/recovery and schema validation include it. Installed/observed metadata survives restart; ordinary residents, PID lifetimes, activity and resource readings are not restored. Last observation is catalog provenance, not proof of a launch or current installation. Entries missing from fresh discovery display Historical / unknown. Existing schema-v5-only statements below describe earlier milestones.

The ASCII banner appears after graceful shutdown and final save. Save/shutdown errors retain diagnostics and a nonzero exit code without a success message; ephemeral sessions explicitly say NOT SAVED. There is no keypress wait. Visual Studio can still append its own debug-console footer. Help uses portable ASCII separators and registered command metadata, including extensions; no-color/redirection behavior is preserved.

**Verification:** `dotnet test -c Release --no-restore`: **297 passed** (14 Core, 127 Simulation, 81 Persistence, 75 Terminal). Tests include filters/IDs/invalid flags/empty results, clear preserving the world and tick, successful/failed farewell, catalog save/reopen/recovery and v5-to-v6 migration. A disposable built-terminal run exercised all command groups, filters, redirected clear fallback and successful save/reopen of the same city. An ephemeral PTY run preserved tick1 through clear and displayed the NOT SAVED banner. This tool's PTY uses the clear fallback; physical screen clearing/color appearance and Visual Studio footer presentation were not visually verified in the user's console. No IDE settings or existing user worlds were modified.

## Latest population revision - 2026-10-19

The current population includes discoverable installed applications and current-session background processes. States are **Sleeping**, **On call**, **Running**, or **unknown** when evidence is insufficient. This supersedes earlier windowed-only/removal-on-exit descriptions below; those remain historical completion records. Ordinary application state is still rediscovered rather than persisted, and the saved city is reused. Full-machine coverage is not complete: see [Installed Application Availability](../Project_GRID_Installed_Application_Availability.md) for implemented scope, Windows inventory limits, remaining work and **290-test** verification.

## Program behavior completion - 2026-10-19

**3.1 implemented and verified.** Non-demo residents now have deterministic needs/goals/actions. Applications reflect fresh or unknown observations; explicitly configured Agent/Special residents perform resource-gated simulated occupation work/rest on simulation-time shifts. Schema v5 persists their routines, sourced budgets and progress without restoring live observations or adding offline work. Use behaviors and inspect for last-tick behavior. No OS task execution, movement or named agent services are claimed.

See [Program Needs and Behavior](../Project_GRID_Program_Behavior.md) for policy, APIs, limits and completion evidence: **278 Release tests passed**, build zero warnings/errors, disposable terminal smoke passed. Debug output was locked by running user sessions, which were left running. Earlier dated deferred-behavior/schema statements describe previous milestones; existing history and completed scope are preserved.

## Application resident completion - 2026-10-19

**2.7 / 2.7.1 implemented and verified:** production population now follows current-session windowed executable groups through read-only discovery and serialized lifecycle reconciliation. Application residents are transient; Agent/Special residents persist in schema v4. Before production conversion, legacy fictional citizens are preserved in a uniquely named validated archive. City/layout and clock remain intact. Unknown resource requirements display unknown condition; no host-control authority is implied.

Verification: **268 tests passed**, build zero warnings/errors, and a disposable Windows startup/reopen detected nine application groups. See [Application Residents Implementation](../Project_GRID_Application_Residents_Implementation.md) for grouping limits, discovery/freshness, storage/migration, editing, resource interpretation and completion evidence. Earlier dated statements that 2.7 is planned, production cities stay empty, or all residents persist describe previous milestones. Agent delegation/services, login, rendering and additional per-application telemetry remain future work.

## Hardware city completion - 2026-10-19

**2.6 / GRID-REQ-004 implemented and verified.** First-time creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates bounded district/street/building-lot geometry and saves actual layout and provenance in schema v3. Later startups reuse the saved world without discovery or regeneration. Existing worlds are preserved; new generated cities start empty pending 2.7. Inventory is creation provenance, separate from live resource telemetry and application identity. Rendering, atmosphere, fashion and application-resident integration remain planned.

Verification: **250 tests passed** (14 Core, 106 Simulation, 74 Persistence, 56 Terminal); build had zero warnings/errors. A fresh Windows city and unchanged reopen were verified in a temporary directory. Use city to inspect saved generation details; --world with a new filename explicitly creates a separate world.

See [Hardware City Generation](../Project_GRID_Hardware_City_Generation.md) for architecture, mappings, fallbacks, persistence and completion evidence. Earlier dated statements that hardware generation is planned or that normal creation always makes twenty fictional citizens describe previous milestones; this update supersedes those status statements while preserving their history.

## Phase 2 verification update - 2026-10-19

Section 2.5 is complete for the current implementation. **231 tests passed** (8 Core, 106 Simulation, 64 Persistence, 53 Terminal), with zero build warnings/errors. Eight new isolated cases verify headless terminal save/reopen/recovery, populated older-schema restoration, no offline catch-up, invalid/missing-file preservation and abrupt writer termination with SQLite journal recovery. A test-only helper under TestSupport is built by the persistence tests; production projects do not reference it.

See [Phase 2 Verification](../Project_GRID_Phase_2_Verification.md) for evidence and limits. Earlier test counts remain historical. Process-crash verification does not claim physical power-loss coverage; pending application energy/classification/lifecycle work and hardware-generated cities remain unimplemented. No production behavior was changed for 2.5.


## Resource/host foundation update - 2026-10-19

Section 2.4 now provides read-only host observations, explicit units/sources/freshness, fake-provider tests and a provenance-required additional-work resource-condition policy. Use resources for details or live for a compact CPU/RAM summary. Unsupported measurements and unknown application requirements remain unknown. Current observations are transient and cannot be edited into Windows allocations or restored from a save as fresh data.

Verification: 223 tests passed, build zero warnings/errors, and real Windows disposable-session sampling passed while simulation time stayed paused. See [Resources and Host State](../Project_GRID_Resources_and_Host_State.md) for collector coverage, the exact policy and limitations. Application discovery/classification, per-application resource attribution and replacing application energy drain remain dependent on 2.7. Existing fictional Genesis citizens retain their old rules. Earlier deferred-telemetry statements below describe prior milestones, not this update.


## History implementation update - 2026-10-18

Section 2.3 now persists meaningful committed changes with world snapshots in schema v2. The history command supports entity/kind/severity/time filters and bounded paging. Routine Tick records are excluded; pending-buffer overflow is reported explicitly. Changing edits include attribution/context, but denied attempts and no-ops are not a comprehensive security audit. Structured operational errors are emitted to terminal output. See [History and Observability](../Project_GRID_History_and_Observability.md) for retention, failure and API details.

Verification: 208 tests passed; solution build had zero warnings/errors; isolated terminal restart/query smoke passed. Earlier schema-v1 and deferred-history statements below describe the prior milestones. Live-view history remains process-local; the new saved-history path is separate. Application discovery, resource interpretation, resident classification and persistence filtering remain planned in 2.4/2.7.


## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

The user revised the product vision: ordinary programs represent currently running PC applications and are not individually saved; agents and explicitly designated special programs persist. Resources reflect application needs/usage and host conditions; energy is a defined interpretation, not arbitrary stamina. Read the shared contract and checklist 2.4/2.7 before further population work. Preserve historical 2.2 evidence without claiming this new model is implemented.

See [Host programs, lifecycles and resources](../Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


**Recorded:** 2026-10-18
**Purpose:** Help future agents continue this collaboration with the same continuity, accuracy and practical results.

This is a project handoff and a summary of observed user preferences, not a replacement for the agent's governing instructions, current user request, repository instructions or tool permissions. It describes visible working practices, not private model reasoning. Recheck the repository before relying on historical status.

## 1. Where work happens

- Repository: `C:\Users\jack\source\repos\Grid7736`
- Documentation: `MD Project Files`
- GitHub remote observed in this chat: `https://github.com/Scorpio7736/Grid7736.git`
- Environment used: Windows and PowerShell; user timezone America/Chicago.
- Main planning entry point: [Master Development Checklist](../Project_GRID_Master_Development_Checklist.md).
- Implementation evidence: [Current Implementation Status](../Project_GRID_Current_Implementation_Status.md), dated completion notes and actual source/tests.
- Requirements and architecture: [Master Overview](../Project_GRID_Master_Overview.md) and [Design Specification](../Project_GRID_Design_Specification.md).
- How to run and extend the terminal: [README](../../README.md).

The conversation progresses through numbered checklist modules, followed by verification, documentation and occasional release or terminal-launch requests. Examples include world management 1.4, clock 1.5, engine 1.6, terminal 1.7 and editing preparation/API 1.9.1-1.9.3. The user may refer to a module number without repeating its requirements; read the matching checklist section before acting.

## 2. How to interpret the user's requests

The user frequently dictates messages. Punctuation, repeated words and short corrections may be transcription artifacts. Interpret them using the surrounding task; ask only when ambiguity materially affects the outcome.

- Requests such as "work on 1.9.3" mean carry out that module, verify it and update its completion notes, not merely propose a plan.
- "Add this to the MD files" is a documentation/planning request unless implementation is also requested. Do not turn a planned feature into code without that scope.
- "Do the same thing" usually refers to the previous module's workflow, including completion evidence. Inspect the context rather than copying claims blindly.
- A correction or added requirement normally steers the active task. Preserve unfinished work unless the user clearly cancels or replaces it.
- When the user says they missed permission, reissue the actual blocked request. Do not assume a rejected or unanswered tool approval succeeded.
- A request to commit changes to GitHub authorizes a commit and push for that work. It is not blanket permission to publish every future edit.

The user values practical action, continuity and thorough documentation. Use reasonable defaults for routine reversible choices. Avoid repeatedly asking for authorization already provided, while respecting actual tool restrictions and unresolved decisions.

## 3. A successful task cycle

1. **Orient:** locate the repo, read applicable AGENTS.md files if present, inspect Git status and identify the relevant checklist and design sections. Look for newer commits or user edits before changing anything.
2. **State the action:** give a brief update describing the intended result and important assumptions. Keep ongoing updates focused on findings and remaining work.
3. **Resolve missing inputs:** locate referenced documents or ask for their location. Continue independent work when useful, but never claim to compare a supplied version that was not available.
4. **Do the scoped work:** preserve unrelated changes, follow the architecture and keep feature boundaries explicit. Prefer focused edits over replacing whole files with older copies.
5. **Verify proportionately:** use meaningful tests/builds for runtime changes; inspect diffs, links, formatting and checklist preservation for documentation-only changes. Do not invent successful tests or rerun broad checks without a reason.
6. **Document:** update the relevant checklist and supporting documents, recording what changed, what was verified and what remains deferred.
7. **Report:** lead with the outcome, list material changes and limitations, link useful files, and state whether changes are local, committed or pushed. Complete authorized publishing steps before reporting publication success.

Keep communication concise and plain. Explain what a change does and why it matters. A plan is not a substitute for completing an action request, and an intermediate progress message is not the final handoff.

## 4. Documentation rules established in this chat

The user explicitly liked dated completion notes and asked that they continue. The user also requested comprehensive documentation so changes are not left undocumented.

- Preserve completed checklist items and their historical evidence. Do not overwrite newer repository information with an older supplied document.
- Distinguish **planned**, **design complete**, **implemented**, and **verified**. A completed design does not establish working runtime behavior.
- Use checked items only when supported by observed implementation or the item's actual design deliverable. Keep future implementation and acceptance tests unchecked.
- Record dates, relevant commit references, exact verification commands and observed results where useful. Historical counts remain historical; label later totals separately.
- Distinguish a project scaffold from an implemented subsystem, a returned receipt from durable audit, and a command/API contract from a usable terminal workflow.
- Keep requirements consistent across overview, specification, roadmap, checklist and affected style/storage guides. Cross-link a shared detailed contract rather than creating contradictory copies.
- When asked to update all documents, audit their relevant implications. Style guides may need integration notes without rewriting the established art direction.
- Document unresolved choices and conflicts honestly. Never imply that missing files were supplied, an unavailable feature was tested, or a GitHub push completed without evidence.

Suggested completion-note format:

```markdown
**Implemented and verified - YYYY-MM-DD**

- Changed: concrete behavior and affected boundary.
- Verification: command/check and observed result.
- Remaining: deferred features, dependencies or known limitations.
```

For design-only work, use **Design documented** and explicitly say that no runtime feature was implemented or tested.

## 5. Architecture and boundaries to preserve

- C#/.NET owns authoritative world state and simulation mutations. Terminal and future viewers consume consistent snapshots and use validated services for changes.
- Extend explicit command/field registries; avoid hard-coding features into the input loop or allowing arbitrary property mutation.
- Keep simulation timing independent of display refresh and real wall-clock timestamps. Do not invent agent activity when programs are Idle.
- Human Master Control, an active agent identity and autonomous/system actions are distinct. Names, occupations, uniforms and hardware do not grant permissions.
- Administrative editing uses validation, serialized mutation, revisions and authorization. An injectable authorization contract is not a production login provider.
- Keep local-first operation, persistence, future host tasks and rendering responsibilities separate. Follow the current design documents for details.

Relevant contracts: [Program Editing Rules](../Project_GRID_Program_Editing_Rules.md), [Session Context](../Project_GRID_Session_Context_and_Authorization.md), and [Terminal Identity Plan](../Project_GRID_Terminal_Identity_and_Master_Control_Addendum.md).

## 6. Recent context to verify before continuing

These are historical observations, not guarantees about the next checkout:

- Genesis was released under `v0.1.0-genesis`. Later editing/startup changes were separate work; do not move the release tag to include them.
- The 1.9.3 solution run passed 117 tests on 2026-10-17; the 1.10 live-view run passed 140 tests on 2026-10-18. Do not present that as a new test run or assume the count still matches later changes.
- The terminal supports optional `--start` automatic ticking. Live view was implemented in section 1.10 on 2026-10-18, with Q/Esc return, paging, bounded event history and console fallback. Check current source before relying on this historical status; login visibility filtering remains deferred.
- The Grid.Persistence scaffold was subsequently replaced by section 2.1 schema/migration implementation on 2026-10-18. Its 31 tests bring the solution to 171 passing tests. Section 2.2 subsequently implemented real engine save/load, autosave, orderly shutdown saves and explicit backup recovery. Its 2026-10-18 verification passed 200 solution tests (8 Core, 93 Simulation, 50 Terminal, 49 Persistence). Default storage is %LOCALAPPDATA%\GRID\world.db; --world selects an isolated file, --recover restores its validated backup, and --ephemeral keeps a disposable session. Do not launch smoke tests against the user's default world. Durable events and hardware-generated cities remain pending.
- GRID-REQ-004 was documented across eight existing documents on 2026-10-18: discover motherboard, CPU, GPU, RAM, storage and network hardware on first-time world creation, generate a unique city, save it and reuse the world on later startups.
- That requirement was documentation-only. Its detailed contract is in the design specification and its unchecked implementation tasks are in checklist 2.6, with later Phase 7/8 integration.
- Existing saved worlds take priority over hardware changes or regeneration. Preserve Genesis history and the existing city, atmosphere and fashion direction.
- The user clarified that the eight supplied documents were the current files in MD Project Files. Older Downloads copies contained no GRID-REQ-004. The written requirement supplied the addition; no separate updated file set was available for a version-to-version merge.

Reconcile source, tests, Git history and dated notes if they disagree. Preserve newer work and explain the discrepancy rather than silently reverting it.

## 7. Terminal, Git and permission practices

When asked to run GRID, open the requested interactive terminal and verify the application process starts. Typical automatic startup from the repository is:

```powershell
dotnet run --project Grid.Terminal/Grid.Terminal.csproj -- --start
```

Minimizing the window lets the user continue working; it does not install a background service. Do not close an existing instance unless requested. If replacing one, identify only the relevant application/terminal processes. Check current persistence support before implying state survives exit.

Before committing or pushing, inspect the branch, remote, status and staged diff. Include only the authorized scope and preserve unrelated work. A created commit is not proof of a successful push. In this chat, Git Credential Manager once blocked a push with a Select an account window; ask the user to complete account selection and verify the eventual result. On resuming an interrupted turn, check whether the earlier operation completed before retrying it.

Use the active tool permission mechanism when required. If an operation is blocked, explain the concrete blocker and request only the action needed. Do not ask for credentials in chat, bypass restrictions or report an approval as execution success.

## 8. Starting a future session well

Read this guide, the latest checklist and relevant contracts, then inspect the current working tree. Identify the user's requested outcome, the last verified state and any unfinished work. Carry forward their preference for expandable design and completion notes, while treating the present user request as the task boundary.

Do not automatically start the next module, implement a planning request, launch processes or push commits merely because those actions occurred earlier in this conversation. The useful continuity is the project's context and working standards; each action still needs to fit the current request.


## Existing-code adaptation plan - 2026-10-18

The user requested explicit retrofit tracking for already completed modules affected by GRID-REQ-005. Use checklist 2.7.1 and the shared retrofit mapping; do not uncheck historic completion entries or claim new compliance from old tests. Address classification and safe lifecycle first, coordinate persistence/startup, then integrate telemetry, editing and live view before more Phase 3 behavior.

See [existing implementation retrofit](../Project_GRID_Host_Programs_and_Resources.md#existing-implementation-retrofit---planned-2026-10-18) and checklist 2.7.1. Documentation only; implementation remains pending.
