# Persistent conversation memory and professional tone — 6.2B

**6.3 completed — 2026-10-26:** `assist advice/analyze/propose` now provides scoped local-AI guidance and typed MCP drafts. `assist submit` creates only tasks awaiting approval; no automatic execution. Captured context stays within explicit agent/project grants, and assistance outputs are not copied into chat recall. **752 Release tests passed**; bundled-model analysis/drafting verified. [Commands and limits](Project_GRID_Task_Assistance_Implementation.md). Earlier 6.3-pending statements are historical; monitor assistance remains planned in 6.3A.


**Automatic conversation recall added — 2026-10-25:** Ordinary saved discussions and their relevant details now carry across conversations/restarts, alongside explicit memory. Private recall stays within the same agent/project; shared meeting recall requires the exact same group. `memory history` reviews candidates; `memory forget-history` excludes prior discussions while retaining explicit memories and transcripts. **728 tests passed**; bundled-model recall/clearing verified. [Behavior, bounds and test](Project_GRID_Automatic_Conversation_Recall.md). This supersedes earlier explicit-only recall descriptions.


Implemented 2026-10-25. Schema v23 stores explicit user-confirmed context alongside conversations in the existing local SQLite save. No new service, online dependency or external memory store is required.

## What is remembered

In a one-to-one agent conversation, Master Control can save preferences, decisions, project facts and unfinished work. Saving is explicit with `memory save`; ordinary chat and AI replies are **not automatically mined or promoted into permanent facts**. The agent receives relevant saved context in later conversations and after restarts. Ordinary conversation history is additionally retrieved automatically under the scoped rules in the follow-up above. These are user-confirmed assertions, distinct from measured engine observations and unverified model suggestions.

Scope is the persistent agent ID plus a project label. A new conversation defaults to `general`. Use `memory scope <project>` before its first message to choose another project. Project/key labels are case-insensitive, normalized to lowercase, and allow 1–48 ASCII letters, digits, dots, hyphens or underscores. Project labels partition memory; they do not register filesystem access or grant project/task capabilities.

Each saved item retains its key, kind, content, UTC update time, and original human message references. Corrections retain prior source references. Sources remain in the original transcript, with their sender, timestamp and conversation identity. AI output is never a memory source through these controls.

## Commands

```text
memory
    - memory list
      # USAGE: Review this agent/project's entries, active/stale/forgotten state, update times and source references.
    - memory scope <project>
      # USAGE: Choose a project in a new, empty one-to-one conversation.
    - memory save <preference|decision|fact|unfinished> <key> <text>
      # USAGE: Explicitly save 1–600 characters of user-confirmed context and its human source message.
    - memory correct <key> <text>
      # USAGE: Replace an existing entry, retaining its kind and provenance. Also reconfirms stale entries.
    - memory forget <key>
      # USAGE: Disable recall and block this key from reuse. Original transcripts remain available.
    # EXAMPLE: memory save fact release Release codename is Moonstone.
```

Commands work in the original terminal's inline conversation and in dedicated conversation windows. Only the authenticated human Master Control identity may review or edit private memory. Meeting windows reject private-memory controls; meetings never import these entries. Program identities do not inherit the human's memory-control authority. Paused GRID still permits administrative memory review/edits; it does not generate agent replies until started.

## Recall, conflicts and forgetting

Retrieval stays within the selected agent and exact project. Up to three relevant entries are considered under the existing 3,200-byte prompt budget. Keyword matches rank entries; preferences apply generally within their scope. Asking what the agent remembers allows broader scoped retrieval. Long entries may not fit; `memory list` always shows stored content. This is bounded deterministic retrieval, not semantic vector search or unlimited recall.

Entries become stale after 30 days and are excluded from model recall until explicitly corrected/reconfirmed. Original memory source messages are excluded from ordinary history retrieval; model responses citing stale or forgotten entries are also excluded. Review shows stale entries rather than silently treating old facts as current.

An existing key cannot be overwritten with `save`: use `correct` to resolve a conflict explicitly. Different keys are not semantically reconciled; the user must review contradictory assertions. A correction or forget advances history recall cutoffs across existing private conversations for that agent/project. Earlier transcript excerpts are then excluded from prompts, including potentially derived old answers. This intentionally sacrifices some earlier conversational continuity to avoid reintroducing obsolete facts. New messages remain eligible.

Forgetting empties the memory value and retains a tombstone plus provenance. The original transcripts remain readable; this is **forgetting for AI recall, not secure deletion of transcripts or backups**. The forgotten key cannot be reused automatically or through `correct`. A deliberate new key can explicitly save new information. Forgotten or corrected source history is not automatically re-ingested; eligible ordinary discussion can now be recalled. Explicitly retyping an old fact into a new user message supplies it again; forgetting cannot erase a human's later input.

Memory/scope revisions invalidate and cancel pending inference so an old response cannot commit after a correction or forget. Changes are saved through the host's normal save callback. Save failures are reported and may leave the change only in memory. Storage limits: 256 entries including tombstones, 32 source revisions per entry, and existing conversation/message limits; capacity failures do not partially append a source. No automatic eviction is implemented.

## Tone

Shared backend and agent prompts now request professional, direct, serious, useful answers, concrete next steps and respectful disagreement. They discourage flattery, excessive reassurance/politeness, repeated disclaimers and reciting internal metadata. Saved context is answered plainly; source information remains available through detailed transcripts and memory review. MCP is encouraged to give useful advice rather than refuse because it coordinates; MAXIS is explicitly a software engineer, and JN should not invent policies.

This is a model instruction policy, not a guarantee of perfect tone or factual accuracy. Local-model output remains unverified and may still invent details. Automatic task proposals/execution, semantic memory extraction, collaboration over private context and project-file ingestion remain outside this item; see 6.3 and later work.

## Acceptance test

Run in the original terminal after logging in as Master Control:

```text
grid -start
connect MCP -inline
memory scope demo
memory save preference style Keep answers brief and direct.
memory save fact release Release codename is Moonstone.
memory save unfinished next Run the release tests.
memory list
disconnect
connect MCP -inline
memory scope demo
talk What is the release codename?
memory correct release Release codename is Sunstone.
talk What is the release codename?
memory forget release
talk What is the release codename?
```

Expected: Moonstone, then Sunstone, then an acknowledgment that the codename is not saved. `memory list` shows the tombstone. Repeating with MAXIS or another project must not disclose MCP/demo memory. For restart recall, save before forgetting, exit GRID normally, relaunch, start GRID, connect MCP and select `demo` before asking. Use another key for repeated tests after forgetting.

## Completion verification

**723 tests passed, zero failures/skips:** 14 Core, 157 Simulation, 99 Persistence, 453 Terminal. Twenty-six additional cases cover all four kinds, new-conversation/restart recall, source attribution, correction, forgetting and retained history, agent/project/meeting isolation, identity restrictions, staleness, relevance and prompt bounds, malformed state, capacity failure, pending-reply cancellation, dedicated-window controls/logout and representative tone instructions. SQLite v22-to-v23 migration preserves existing conversation data and defaults absent memory fields safely.

Release publish and `Test-GridPortable.ps1 -PackageRoot .\portable\GRID7736-standalone` passed with the bundled Qwen3:4b runtime and restricted global runtime paths. Actual model replies recalled “Moonstone,” changed to “Sunstone,” then stated the codename was not saved after forgetting. Representative greetings, disagreement with an unsupported code-quality claim, and build-debugging advice were exercised. The revised memory answers omitted internal trust labels/timestamps. This smoke test does not prove arbitrary-answer quality; the model can still hallucinate incidental world details. No user world was opened and no tasks were executed. Physical clean-machine/offline-firewall/microphone acceptance remains separate.
