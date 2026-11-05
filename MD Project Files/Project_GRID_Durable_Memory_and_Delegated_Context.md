# 6.10 Durable memory and delegated context

**6.21 agent memory migration — 2026-11-05.** Memory records, grants and recall policy now belong to Grid.Agent, with generic lexical/extractive helpers in Grid.AI. Existing conversation storage, schema v24, permissions, correction/expiry and source revocation remain compatible. 1,180 tests and six live copied-database memory checks passed. No new model or memory database is required. [Implementation and verification](Project_GRID_Agent_Memory_Migration.md).

Implemented 2026-10-28. Schema v24. Phase 7 remains blocked by 6.11–6.15.

## What is retained

Existing saved conversations, automatic recall and explicit facts/decisions/preferences remain intact. New explicit memory kinds include `Outcome` and `Summary`. Human Master Control controls retention in a one-to-one agent conversation and an explicit project scope. No AI response or uploaded document is automatically promoted to durable memory.

A retained document summary is the exact user-approved text (1–600 characters), linked to its source document ID, captured SHA256, source retention mode, and the operator's approval message. The raw temporary document is not copied into saved memory. Temporary imports still expire with the existing session/host lifecycle; the explicitly retained summary survives it. A task-outcome memory is generated from a finished task's recorded ID, capability, status and attempt count, with a provenance fingerprint. It does not retain the task's raw output or claim that external state is independently verified.

## Commands

Connect to an agent and select a project with `memory scope <project>` in a new, empty conversation.

| Command | Effect |
|---|---|
| `memory list` | Review owned memories and currently available delegated summaries, sources, owner/project, grants, and expiry. |
| `memory save fact release The release codename is Pinecone.` | Existing explicit user-confirmed memory. Other kinds: preference, decision, unfinished, outcome, summary. |
| `memory save-summary <context-id> <key> <approved summary> --retain` | Explicitly retain only the approved summary; source must be granted to the current agent/project and visible in the operator's context scope. Without `--retain`, nothing is retained. |
| `memory save-outcome <task-id> <key> --retain` | Retain the recorded terminal outcome of a finished task. Pending/running tasks are refused. |
| `memory grant <key> <agent> <project>` | Share this exact owned memory with a named registered agent in a named project. No transcript, raw file, host permissions or task approval is granted. |
| `memory revoke <key> <agent> <project>` | Revoke that recipient scope and exclude earlier recipient-scope conversation history from recall. |
| `memory correct <key> <replacement>` | Replace owned content, retain source history, invalidate old recall and revoke existing shares. Re-grant explicitly if desired. |
| `memory forget <key>` | Remove active content and block recall; the key remains a tombstone and cannot be reused. |
| `memory expiry <key> <days>` | Set expiry from now (0–3650 days). Zero expires immediately. |
| `memory expiry <key> never` | Explicitly remove automatic expiry. |

The default expiry remains 30 days after the latest save/correction. Expiry blocks prompt use rather than physically erasing stored records. Received shares are read-only: recipients cannot modify or re-share the owner's memory. The operator can connect to the owning agent/project to manage it. A plain manually supplied summary has user-message provenance; use `save-summary` for document-linked revocation.

## Approval and source example

```text
context add "C:\Notes\release.txt" temporary general MCP
connect MCP -inline
context list general
memory save-summary <context-id> release The approved release codename is Pinecone. --retain
memory grant release MAXIS general
memory list
```

Replace `<context-id>` with the imported document's actual ID. The source file must exist; this example is not executed automatically. Summary retention is an explicit user action, not a claim that GRID verified the summary against every sentence in the document.

## Recall and revocation policy

Recall is scoped to the owning agent/project unless the operator explicitly grants another agent/project. Meetings do not silently import private memories. Correction, forgetting, source revocation/removal and grant revocation invalidate pending inference through memory revision changes. Expiry also prevents an already running reply from committing after its memory deadline.

Revoking or removing a captured source forgets its linked summaries and invalidates their shares. Routine cleanup of a temporary source does **not** erase a separately approved durable summary. Once that temporary source is gone, use `memory forget` or `memory revoke` to change the retained summary's permissions.

To avoid reintroducing corrected/revoked details through old generated answers, invalidation excludes earlier conversation history in the affected owner/recipient scopes. This is deliberately broader than deleting one sentence. New information independently supplied by the operator is a new source. Transcripts, audit metadata and historical backups remain; forgetting/revocation is not secure deletion of every historical copy. No other agent's private conversation is shared with a memory grant.

## Persistence and portability

Provenance, expiry and explicit grants live with the existing bounded communication JSON in `world.db`. Migration 24 is an additive version gate; existing version-23 memories deserialize with empty grants, no extra provenance and their original expiry behavior. Old binaries cannot open the upgraded schema. No new external runtime or database service is required.

Copies/transfers retain approved summaries and grants because they travel inside the saved world; temporary raw documents are absent from its snapshot. The integration check copies an isolated closed SQLite database and reloads it, not a live user installation. Existing portable copy/transfer mechanisms continue to carry the world database. No raw-file path permission is inferred from stored provenance.

Limits remain 256 memories, 600 characters per memory and 32 source messages per key; each memory permits at most 32 recipient/project grant records. Forgotten keys are retained, not recycled. Source hashes/IDs are provenance, not authority to reopen a file.

## Verification

989 automated tests passed: 14 Core, 157 Simulation, 99 Persistence, 719 Terminal. Thirteen new tests cover retention consent, agent/project source access, delegation isolation, no re-sharing, correction, expiry, source revoke/remove, pending cancellation, invalid metadata, actual recorded task outcomes, v23 migration and database-copy persistence.

Six bundled-model checks passed: runtime readiness, explicit retained summary/grant, copied-database retention without temporary body, recipient recall after reload, exclusion of an unshared agent, and exclusion after grant revocation. [Recorded evidence](verification/6.10-durable-memory-report.md). `--verify-durable-memory` repeats the isolated integration check without opening the user's world.

## Manual test

After restarting the updated GRID host:

```text
grid -start
connect MCP -inline
memory scope demo610
memory save fact release The release codename is Pinecone.
memory grant release MAXIS demo610
disconnect
connect MAXIS -inline
memory scope demo610
talk What release codename do you remember?
```

MAXIS should receive the approved detail. In a new conversation with JN in `demo610`, it should not. Return to MCP in `demo610` and run `memory revoke release MAXIS demo610`; reconnect MAXIS in that project and verify the earlier shared detail is no longer supplied to its prompt. Use `transcript --details` and `memory list` to inspect provenance. Use a new project/key if this demo already exists.
