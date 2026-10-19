# Project GRID - Chat workflow and agent handoff

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
- A Grid.Persistence project scaffold was observed later. Its presence alone did not implement save/load.
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
