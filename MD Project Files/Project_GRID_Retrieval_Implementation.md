# Project GRID — Scoped retrieval implementation

**Local file discovery follow-up — 2026-11-05.** Connected Master Control conversations recognize `hey mcp can you find the stl file on my desktop` and `find UWGB in my documents` as local name searches, not uploaded-context analysis. `search my files for any STL file` asks for a location; repeat with Desktop, Documents or Downloads. Lookup lists observed paths without opening file contents, importing context, invoking the model or changing files. Private searches are refused in meetings. Limits: 2 seconds of checked traversal time, 10,000 entries, depth 8 and 50 results; hidden/system/reparse-point folders and inaccessible entries are skipped with an incomplete-result notice. An individual filesystem call can still wait on the OS. This is bounded local discovery, not unrestricted whole-PC search or STL content analysis. Windows Desktop/Documents locations are resolved through special-folder settings. Downloads currently uses the profile Downloads path; custom redirects there remain unsupported.


**Conversational retrieval follow-up — 2026-11-05.** In a connected conversation, type `find and explain INDIGO`, `find INDIGO in my uploaded documents` directly; `assist analyze` and `talk` are optional. Missing grants prompt for files in plain language and retain the question through selection. Analysis shows an agent reply, with `details` available for citations. Existing file/task review and approval boundaries remain unchanged. Routing supports these documented intents; arbitrary requests are not guaranteed to become executable operations.


**6.20 lexical RAG foundation implemented and locally verified — 2026-11-05.** Learned semantic embeddings remain planned, pending model selection and evaluation. This is offline captured-document retrieval, not permission to browse the computer or execute document instructions.

## Implemented behavior

`context search "project" "query"` searches the current identity's authorized captured documents. Agent assistance uses the same retrieval layer to select relevant excerpts beyond the former first-700-character cutoff. Private documents remain excluded from meetings. Source files are never reopened: re-import explicitly to capture changes. A citation describes the imported version, not the current original file.

Grid.AI owns generic document/chunk/embedding contracts and deterministic text processing. Grid.Persistence stores derived chunks and vectors in an in-memory SQLite database. Grid.Terminal supplies fresh agent/project-filtered captures and checks identity and context revision before returning search results. Assistance also checks context changes during inference and suppresses revoked or expired answers.

Every search rebuilds the disposable index from the current authorized captures. There is no secondary disk index, stale grant cache or vector sidecar to copy. Persistent context remains in the existing saved world; after restart its authorized snapshots rebuild the index. Temporary context retains existing logout/host-exit lifetimes. Revocation does not erase previously displayed output or historical backups.

## Ranking and bounds

- Chunks are approximately 600 UTF-16 characters with 80-character overlap, avoiding surrogate splits and captured PDF page-marker boundaries.
- Ranking combines BM25 keyword relevance (70%), deterministic 128-dimensional token-feature cosine similarity (20%) and exact-phrase bonus (10%). Token vectors are lexical features, **not learned semantic embeddings**. A lexical match is required; vector collisions alone cannot retrieve a result.
- Exact bounded vector scans run in managed code, with stable reranking, at most two excerpts per document and six default results. Search supports 1–12 results internally.
- Analysis may use an explicitly labeled overview fallback when no lexical match exists; ordinary search returns no matches.
- Limits: 64 documents, 256 Ki captured characters per document, 1 Mi total captured characters, 4,096 chunks, 1,200 query characters. Invalid or oversized inputs are refused. Existing prompt budgets can further reduce supplied excerpts.

Chunks retain document identity, source SHA256, extracted-text SHA256, chunk SHA256, capture time, format, UTF-16 offsets, line numbers and versioned chunk/ranking/embedding methods. Display citations use abbreviated hashes; full hashes remain in the derived records. PDF page numbers are labeled **captured page markers**; they are not independently validated page coordinates. Citations identify supplied evidence, not proof that every model claim is correct.

## Dependencies and portability

The existing Microsoft.Data.Sqlite 10.0.13 stack is reused (MIT package metadata), with SQLitePCLRaw bundle/native package 2.1.13 (Apache-2.0 package metadata). The vector scan and feature hashing are original managed code: no new native vector extension, service, download or model is required. Windows x64 build, SQLite queries and PDF/DOCX capture retrieval passed locally. Preserve existing runtime notices in distribution; this is not a redistribution audit. Physical second-device acceptance remains separate.

## Verification

Current suites total **1,165 passed**: Core 14, Simulation 157, Persistence 99 and Terminal 895. Fourteen retrieval cases cover deep matches, source/extraction version changes, scope replacement, no-match behavior, parameterized query inputs, vector determinism, Unicode/page boundaries, budget rejection, PDF/DOCX after original-file deletion, agent/project isolation, revoke-during-inference, terminal search and persistent capture restart.

The bundled local model passed `Grid.Terminal.exe --verify-retrieval` in a disposable synthetic world: it found a release code beyond character 700, answered correctly with captured citations, and retrieval returned nothing after revocation. [Recorded evidence](verification/6.20-retrieval.json). No user world or source file was modified by this check. The existing live assistance verification also passed chat, advice, captured-context analysis and reviewed task drafting. Final solution build passed with zero warnings/errors.

## Manual check

Import and grant a document using the existing context workflow, then search its project:

```text
context list
context search "general" "a distinctive phrase in your document"
grid -start
connect MCP -inline
assist analyze Find and explain that distinctive phrase
```

Use the document's actual project and query. `context revoke <id>` removes agent access; the Master Control owner may still search retained content. `context remove <id>` removes the active capture. Re-import changed originals explicitly.

## Remaining before full semantic RAG acceptance

Select, provision and evaluate a learned local embedding model and compare semantic relevance against this lexical baseline. No specialist model was downloaded or silently selected. Larger-corpus/persistent-index optimizations are not implemented; any later cache must preserve current authorization/version invalidation. Second-device offline verification and the other Phase 7 gates remain open. No permissions, task approvals or storage budgets changed in this milestone.
