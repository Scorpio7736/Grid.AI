# Automatic conversation recall — 6.2B follow-up

**6.10 completed — 2026-10-28:** Approved document summaries and recorded task outcomes persist with provenance, explicit recipient/project grants, expiry, correction and revocation. Temporary raw uploads stay temporary. Schema v24; **989 automated tests and six bundled-model checks passed**, including copied-database recall and in-flight revocation/expiry. [Commands, retention policy, migration and test](Project_GRID_Durable_Memory_and_Delegated_Context.md). Earlier private-only or 6.10-planned statements are historical; 6.11–6.15 remain required before Phase 7.

**Planned extension — 2026-10-27:** Existing automatic recall remains implemented. Before Phase 7, 6.10 must retain approved task outcomes and file-derived summaries across chat end/restart/transfer while separating temporary raw documents, durable memory and agent grants, with correction/forgetting and provenance. [Plan](Project_GRID_Pre_Phase_7_Readiness_Plan.md).

Implemented 2026-10-25 at the user's request. Agents now recall ordinary conversations and relevant details without requiring `memory save`. Explicit memory requests continue to work alongside automatic discussion recall. Schema v23 is unchanged: saved transcripts, project scope and recall cutoffs already persist in SQLite.

## Behavior

Relevant excerpts from prior conversations are supplied automatically when a new question matches their contents. A broad question such as “What did we discuss earlier?” retrieves recent scoped discussion. User statements remain attributed as things the user said. Agent replies are identified by speaker and labeled as unverified suggestions, never promoted to confirmed facts or proof of actions. UTC source times and message/conversation IDs remain available through `transcript --details`.

Up to three prior-message excerpts are considered, ranked by keyword relevance, then user-source priority and recency. Excerpts are bounded to 240 characters around a matching word, under the existing total prompt budget. Explicit memories take priority in that budget. This is deterministic excerpt retrieval, not unlimited recall, a semantic search index or an automatically inferred fact database. Information outside the selected excerpts may be missed. Historical conversation text does not expire after 30 days; it is labeled as historical, and newer/corrected information should take precedence. Explicit memory entries retain their existing stale-entry rules.

Private recall requires the same human, agent and project. Meeting recall requires exactly the same participant identities and project; name/order changes alone do not change membership. A meeting never imports a participant's private chats or memories. Adding or removing a participant starts a separate recall scope. Program identities do not gain human recall-control privileges.

Existing `memory correct` and `memory forget` cutoffs also exclude prior scoped discussions from automatic recall. Source messages belonging to explicit memories are not re-imported as ordinary history. Automatic excerpts are drawn from the saved original transcript; they do not create duplicate confirmed-memory records.

## Controls

```text
memory
    - memory history
      # USAGE: Review the latest 20 eligible prior messages for this conversation's scope. The AI selects relevant excerpts from eligible history.
    - memory forget-history
      # USAGE: Exclude earlier discussions in this agent/project or exact meeting group from future AI recall. Original transcripts and explicit memories remain.
    # EXAMPLE: memory history
```

`memory forget-history` sets persistent recall cutoffs for all existing matching conversations, including the current one, and invalidates pending replies. New messages after the cutoff may be recalled normally. It does not erase transcripts/backups or clear explicit memories; use `memory forget <key>` for those. Review/control commands work in inline and dedicated conversation windows.

## Test

Restart the updated app and log in, then:

```text
grid -start
connect MCP -inline
talk Our project mascot is Copperfin. We still need to test the installer.
disconnect
connect MCP -inline
talk What is our project mascot, and what work remains?
memory history
memory forget-history
disconnect
connect MCP -inline
talk What is our project mascot?
```

The first recall should include Copperfin and installer testing; after clearing history, the agent should report the detail is absent. Recall also survives a normal save/restart. Default project is `general`; set `memory scope <project>` in a new empty conversation for another project.

## Verification

Full solution: **728 tests passed**, zero failed/skipped (14 Core, 157 Simulation, 99 Persistence, 458 Terminal). Added coverage for ordinary details across restart, private scope boundaries, clearing history while preserving explicit memory, exact meeting membership, long-message detail retrieval and unverified agent advice. After refining relevance ranking and speaker attribution, all **30 memory tests** passed again.

The bundled Qwen3:4b smoke test recalled Copperfin from a previous ordinary conversation without any explicit save, then reported no mascot after history clearing. Existing explicit-memory recall/correction/forgetting and meeting checks also passed. Release package refreshed; verification used an isolated test world, not the user's world. Model answer quality and completeness remain bounded; no host tasks are automatically executed.
