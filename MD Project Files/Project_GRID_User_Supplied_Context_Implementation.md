# Project GRID - User-Supplied Context Implementation

## 4.3 completion - 2026-10-20

GRID now imports an explicitly selected local file as captured task data, grouped under a named project. Master Control chooses retention and optionally grants the captured file to one registered Agent. This is a terminal file-selection/import workflow; a graphical file picker, drag/drop and remote uploads are not implemented.

```text
context add "C:\Work\notes.md" persistent "My Project"
context add "C:\Work\design.docx" temporary "My Project" ARCHON
context list
context list "My Project"
context read <context-id>
context revoke <context-id>
context remove <context-id>
```

The optional agent must already be registered (`connect ARCHON`, then `register-agent ARCHON` for the coordinator if needed). Omitting the agent keeps content owner-only. Temporary imports clear when the importing session changes identity, logs out, detaches or ends; use a separately attached agent terminal to inspect a temporary grant without changing the importing session. Persistent imports require a saved world; ephemeral sessions can use temporary imports. Import/revoke/remove require Master Control, including service-side edit-authorization leases. Agents can only list/read explicitly granted imports; they cannot select a host file, grant, revoke or remove context.

### Approved scope and captured versions

Each `context add` explicitly selects one absolute local file, never its parent directory. Project names are organizational labels, not filesystem grants. Reads return captured text and provenance without reopening the source. There is no folder crawling, wildcard import, automatic refresh, external-document fetch, macro/script execution or permission to write the source file. A source may change or disappear without changing the captured version. Re-import explicitly to create a new immutable context ID/version; earlier imports remain until removed.

Provenance records source path, raw-source SHA-256 fingerprint, source byte length, last-modified UTC, capture UTC, extraction format, project name and optional agent grant. The fingerprint identifies the captured source version; it is not a sequential revision number. DOCX text is extracted from the fingerprinted original archive. Source paths travel as historical provenance when moving GRID; they are never automatically opened on another PC. These imports are not automatically inserted into transcripts or passed to an AI provider.

Terminal identity adapters supply the authenticated actor and session to the retrieval API; request payloads never choose an actor ID. Persistent owner-only documents are visible to Master Control sessions, not agents. Temporary owner-only documents are visible only to the importing session. Registered Agent grants permit retrieval of that captured file, not other files in the project or on the host. Attached terminals use the same host service and their own session identity.

### Formats and bounds

Supported text is strict UTF-8 with an optional UTF-8 BOM. Text extensions: `.txt`, `.md`, `.markdown`, `.cs`, `.csproj`, `.sln`, `.py`, `.js`, `.ts`, `.tsx`, `.jsx`, `.json`, `.xml`, `.yaml`, `.yml`, `.toml`, `.ini`, `.csv`, `.sql`, `.html`, `.css`, `.cpp`, `.c`, `.h`, `.java`, `.rs`, `.go`, `.ps1`, `.sh`. Source code is read as text, never executed.

`.docx` imports main-body paragraph text, including paragraphs inside tables. Images, embedded objects, headers/footers, comments, tracked formatting and external links are not imported. The XML reader prohibits DTD/entity resolution and does not follow relationships. PDF, legacy DOC, RTF, encrypted/unsupported documents, binaries and other encodings require an explicit export to a supported format; they are rejected rather than guessed or silently truncated.

Limits: 1 MiB source file; 256 Ki UTF-16 text characters per import; at most 64 imports and 1 Mi total text characters across persistent and temporary content. DOCX allows at most 256 ZIP entries and a bounded 1 MiB document XML body. Oversized compressed expansion is rejected. Control characters other than tab/newline/carriage return are refused so file content cannot inject terminal escape sequences. Relative, UNC/device and mapped-network paths are refused. Symlink/junction components are refused, and Windows verifies the opened handle resolves to the selected path. Content is labeled untrusted task data and cannot grant execution authority.

### Retention, revocation and persistence

Persistent content is stored in schema v13's bounded WorldContext JSON row in the same world transaction. Existing worlds migrate with empty context; identities and history are retained. Imports and durable revoke/remove operations immediately request a world save. A failed save reports an error and leaves the in-memory change visible; resolve it before relying on restart durability. Backups/recovery and GRID copy/move carry persistent context automatically with the database.

Temporary content is held only in the current host's memory and excluded from snapshots written to the database/backup. Identity/session cleanup removes it. Audit events contain operation/context IDs and retention/action metadata, not source paths or file bodies. These event IDs may remain after temporary content expires.

`context revoke` immediately removes the agent grant but keeps the owner's captured text. `context remove` removes the import from the active world. Both apply to subsequent reads; they cannot erase content already displayed, manually copied, or retained in historical backups. Removal is logical deletion, not forensic wiping of SQLite pages or managed memory. Restoring an older backup restores its then-current grants/content; review access after recovery. A fresh explicit import can share the file again.

`talk context` reports only how many captured files are explicitly shared with the conversation recipient. It does not append file bodies to a durable transcript, execute file instructions or perform analysis. Local AI ingestion, semantic retrieval, task execution/delegation and richer document support remain later work. `context` is a mixed read/mutation command, so `-opninwin` is not supported; use `ntm` for a separate context terminal.

### Completion verification

420 tests passed: 14 Core, 155 Simulation, 95 Persistence and 156 Terminal. Sixteen new context cases verify immutable source versions, scoped grants, owner-only/session-only visibility, denial of agent mutation, immediate revoke/remove, logout/role-change cleanup, temporary exclusion from database/backup, text and source quotas, invalid UTF-8/binaries, unsupported paths/formats, DOCX body extraction and entity/expansion rejection, save-error reporting, schema v12 migration, durable restart/recovery and transactional rollback. Existing window, identity, transfer/NGE, simulation and persistence tests pass. Build succeeded with zero warnings/errors. Tests used disposable fixtures; no user documents were imported or changed during verification.
