# Project GRID — Authorized editing workflows (6.4)

**6.5 live verification — 2026-10-27:** The bundled model generated a reviewable synthetic edit; generation did not write, unapproved application was refused, explicit approval allowed application, and rollback restored the original bytes. This extends the earlier 6.4 regression evidence. [Captured report and repeatable demo](Project_GRID_Intelligence_Verification.md).

Implemented 2026-10-26. The original host's `changes` command supports bounded single-file creation/replacement in a registered MAXIS workspace. Human Master Control must review and approve every change. Stage/AI generation never applies files. The existing `maxis.suggest` capability remains a staged literal suggestion; it has not gained automatic apply rights.

## Commands

```text
changes
    - changes stage <workspace> <relative-file> <context-id|name>
      # USAGE: Stage the captured context's complete text as a new or revised file. Context project must match the workspace alias. Owner-only context is permitted for this explicit non-AI operation.
    - changes suggest <workspace> <relative-file> <context-id|name> <request>
      # USAGE: Ask local MAXIS AI for the complete candidate file using explicitly MAXIS-granted context in that workspace project. The user fixes the destination; the model cannot choose paths or commands.
    - changes list
      # USAGE: List this host's draft, approved, applied and rolled-back records.
    - changes preview <id>
      # USAGE: Show complete before/after text and SHA256 hashes before application; show result and recovery location afterward.
    - changes approve <id> [high-risk]
      # USAGE: Approve the exact reviewed candidate and source hashes. Code/configuration requires the literal high-risk argument. No write occurs yet.
    - changes apply <id>
      # USAGE: Revalidate authority, scope, workspace and source hash, create durable recovery material, then apply the approved file.
    - changes rollback <id>
      # USAGE: Restore original bytes, or remove a newly created file, only if the current file still matches the applied candidate. Recovery copies remain.
    - changes discard <id>
      # USAGE: Discard an unapplied draft. Applied records retain recovery information.
    - changes verify <id> <build|test>
      # USAGE: Propose a saved MAXIS task for the workspace. It still requires tasks approve and tasks run; no process starts automatically.
    # EXAMPLE: changes list
```

Commands require a running GRID and human Master Control; editing is unavailable in attached windows. Existing MAXIS workspace registration requires a saved world, a local workspace directory and its relative `.csproj` path. The destination parent directory must already exist. There is no mkdir, rename, arbitrary deletion, shell execution, multi-file transaction or automatic patch application command.

## Review and scope

`stage` uses an explicitly chosen captured document without reopening its source file. `suggest` receives only the current target text, the selected MAXIS-granted document and the user's request. Context project must equal the workspace alias. Other private files, memories and unrelated project context are excluded. AI input is limited to 3,200 UTF-8 bytes without silent truncation. Responses must be exactly one JSON object containing a single string property `content`; extra/duplicate fields, paths, malformed output and empty replacements are rejected. AI input is untrusted data, never execution permission. Suggestions can be wrong and always require full review.

The preview contains the complete old/new text with minus/plus prefixes, target, operation, risk and hashes. It is a complete before/after review, not a minimal unified patch. Control and formatting characters are visibly escaped in previews. Markdown/text is classified as document risk; supported code and configuration extensions require explicit `high-risk` approval. Every class still requires separate approval and apply commands.

An unapplied draft is bound to session generation, GRID activity and context revision. Logout, identity change, pause/resume or context changes require a new draft; revoking the workspace prevents writes. Existing sources are rechecked at approval and immediately before application, and AI generation rejects a changed destination. New-file drafts cannot overwrite a file that appeared after preview. Approval never grants unrestricted editing permission.

Files are strict UTF-8, at most 16 KiB before/after. Existing UTF-8 BOMs are retained; candidate line endings are taken from supplied/generated text, and rollback restores exact original bytes. NUL/binary data and empty replacements are refused. Allowed extensions are `.md`, `.txt`, `.cs`, `.csproj`, `.sln`, `.slnx`, `.props`, `.targets`, `.json`, `.xml`, `.yml`, `.yaml`, `.html`, `.css`, `.js`, `.ts`, `.tsx`, `.jsx`, `.py`, and `.sql`. Absolute/traversal/stream paths, linked/reparse paths, Windows device names, ambiguous trailing dots/spaces, version-control metadata, agent configuration directories and generated bin/obj paths are refused.

## Apply, recovery and verification

Recovery directories are placed under the configured MAXIS artifacts root at `workspace-edits/<change-id>`. Before source mutation, GRID writes and flushes `original.bak` for replacements, `candidate.bak` and a prepared `report.json`. The report records destination, original/candidate hashes, human approval session, context identity/source hash and operation phase. Source changes use a sibling temporary file and atomic replacement/creation. A post-write report failure is reported as a warning after the file has changed, not as a successful no-op. Backups are never automatically deleted.

Rollback refuses newer edits, missing files or redirected paths. It restores original bytes for replacements; an explicit rollback of a new-file creation removes that file only when it still matches the applied hash. It retains the candidate backup. Workspace revocation also blocks rollback through GRID; recovery files remain available for deliberate manual restoration.

Drafts and approvals are host-memory only, with at most 16 records. Logout clears unapplied drafts; applied recovery records remain available for the same host after authorized login. Restart loses the command's in-memory records and approval state, but disk backups/reports remain. After a crash/restart, compare hashes in the report and manually recover from the backup if needed; automatic recovery/reapproval is not implemented. Filesystem edits and the SQLite world are not one transaction. A report left at Prepared requires inspection of actual file hashes before recovery.

Workspace checks protect against normal stale work and detected links; they are not an OS security boundary against malicious concurrent filesystem manipulation. Build/test executes project-defined code as the current OS user using existing fixed MAXIS commands, timeouts, logs and --no-restore behavior. It is separately approved and does not provide network isolation. A successful test process does not prove tests were discovered; inspect task results/logs. Failed verification does not automatically roll back source or remove project-generated outputs.

## Completion verification

838 Release tests passed: 14 Core, 157 Simulation, 99 Persistence and 568 Terminal. The 39 new workflow cases exercise create/replace/rollback, byte-exact BOM/line-ending restoration, high-risk acknowledgement, path and symlink refusal, changed-source guards, replay refusal, backup-write failure, permission/context/workspace revocation, strict AI response parsing, scope cancellation, context isolation, safe previews and separately approved build/test handoff. Actual local dotnet build/test targets were invoked in temporary workspaces; those tiny targets verify process execution, not application test discovery. AI draft tests use controlled model responses; live model output quality is not claimed. Schema remains v23 and no new runtime dependency is introduced.

## Safe demo

Restart GRID and log in to the original host. Use a new workspace alias, or an existing registered alias with matching context project. This example uses the checked-in tiny candidate file and creates a separate Markdown file in the repository.

```text
grid -start
maxis register edits-demo "C:\Users\jack\source\repos\Scorpio7736\Grid7736" "Grid.Core/Grid.Core.csproj"
context add "C:\Users\jack\source\repos\Scorpio7736\Grid7736\MD Project Files\examples\6.4-candidate.md" temporary edits-demo MAXIS
changes stage edits-demo GRID-6.4-demo.md 6.4-candidate.md
changes preview <returned-change-id>
changes approve <returned-change-id>
changes apply <returned-change-id>
changes rollback <returned-change-id>
```

If `GRID-6.4-demo.md` already exists, choose another unused filename. To test AI generation, use `changes suggest edits-demo GRID-6.4-ai-demo.md 6.4-candidate.md Rewrite this as two short bullet points`, then review its returned draft before approving. To verify an applied edit, use `changes verify <id> build`, then separately approve and run the returned task ID. No build/test is required for the Markdown demo itself.
