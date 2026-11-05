# Project GRID — Agent memory migration (6.21)

**Implemented and locally verified — 2026-11-05.** This is the ownership migration of existing persistent memory and recall into the GRID-Agent/GRID-AI architecture. It preserves saved content, grants and provenance rather than introducing a second memory database. Phase 7 remains blocked by other checklist gates.

## Ownership and compatibility

`Grid.Agent` now owns `ConversationMemory`, its kinds/source/provenance/grant records, and versioned `MemoryPolicy`. The policy controls agent/project visibility, explicit delegation, expiry, preference/relevance selection, exact-participant conversation scope and the six-message recent-history window. `Grid.AI.RecallText` supplies generic lexical scoring, relevant excerpts and extractive summary selection. It neither authenticates callers nor grants access.

`Grid.Communication` retains the saved conversation envelope and validation that a memory actually originates in the correct human message. `Grid.Simulation` remains the authoritative host for authenticated mutations, locks, registration checks, source revocation, revision invalidation and prompt assembly. `Grid.Persistence` still saves the same communication JSON in world.db. There are no new services, model downloads, external stores or changed file permissions.

Memory types moved from the Grid.Communication namespace to Grid.Agent; C# consumers must import Grid.Agent and rebuild. JSON property names and enum values did not change. Existing schema version 24, older migration paths, tombstones, source references, retention and grants remain intact. No serialized runtime type metadata or destructive database conversion is introduced.

## Retention contract

| Layer | Current behavior |
|---|---|
| Recent context | Up to six recent eligible messages, clipped within the existing prompt budget. |
| Older active-conversation summary | Two ranked original excerpts, labeled lossy/untrusted. This is extractive selection, not a newly verified fact or automatically saved model summary. |
| Prior conversation recall | Up to three relevant messages from the same project and exact participant identities/kinds; meetings must match the same group. Recall still labels agent responses unverified. |
| Explicit long-term memory | Facts, preferences, decisions, unfinished work, outcomes and summaries; at most three relevant live records per prompt. Preferences remain eligible without keyword matches. |
| Document summaries | Only explicitly approved retained text survives; document ID/hash and temporary-source provenance remain attached. Raw temporary text is not silently retained. |
| Correction/deletion | Correction preserves source history, revokes old shares and invalidates earlier scoped recall. Forget clears active memory text and retains a nonreusable tombstone; transcripts/backups are not securely erased. |
| Expiry | Default 30 days since latest save/correction unless an explicit expiry was set. Expired memories cannot enter prompts or complete an in-flight reply after their deadline. |
| Source revocation | Explicit source removal/revocation forgets linked summaries and invalidates shares and pending replies. Routine temporary-source cleanup preserves separately approved durable summaries. |

Existing limits remain 256 memories, 600 characters each, 32 source messages per key, 32 grants per memory, and bounded saved conversations/prompt budgets. Retained discussions are recalled automatically; no model output is promoted to confirmed durable facts. This migration does not promise perfect recall of everything ever said or add semantic embedding memory search.

## Verification

**1,180 tests passed:** Core 14, Simulation 157, Persistence 99, Terminal 910. Existing tests cover restart and copied SQLite worlds, old schema migration, automatic recall, agent/project/meeting isolation, correction, forgotten-key tombstones, expiry, source revocation and in-flight cancellation. Four new tests cover legacy JSON defaults/type-independent storage, delegated visibility/expiry, exact identity-kind scope matching and generic extractive summaries.

Terminal/Core/Simulation checks used a separate output folder while GRID was running. Three persistence subprocess checks assume the standard output location and initially failed under the alternate layout; all 99 persistence checks passed when rebuilt/rerun at their expected location with `UseAppHost=false`. That build completed with zero warnings/errors. The running user process was not stopped.

The existing `--verify-durable-memory` check was rerun against the migrated binaries and bundled Qwen model. All six checks passed: runtime readiness, approved summary/grant, copied database without temporary source body, authorized recipient recall after reload, unshared-agent isolation and revoked-recipient exclusion. [Recorded 6.21 evidence](verification/6.21-agent-memory.json). The report retains its original 6.10 test-category labels because it verifies that existing behavior survived this migration. No real saved world was opened. This is local copied-database evidence, not a second-device portable acceptance claim.

## Manual check

After rebuilding/restarting GRID, start it and connect to MCP. In the conversation say:

```text
remember fact release621: The release codename is Amber621.
disconnect
connect MCP -inline
What release codename do you remember?
```

MCP should recall Amber621. Saved-world sessions retain it after a normal save/exit/restart. Connect to the same agent/project for recall. `memory list` shows provenance and grants; `memory correct release621 The release codename is Jade621.` changes it; `memory forget release621` blocks the old key and invalidates earlier scoped recall. Explicit commands remain available for precise memory administration.

The next numbered item is 6.22 identity and authorization; the 6.20 learned semantic embedding follow-up remains open pending selection/evaluation.
