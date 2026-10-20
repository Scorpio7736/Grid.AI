# Project GRID - NGE (Nullify Grid Existence)

## NGE implementation - 1.13 (2026-10-19)

**Initial local-terminal NGE implemented.** Type `nge` in an interactive saved-world session, review the world identity and exact file list, then enter `y` to confirm or `n`/blank to cancel. Invalid answers reprompt; EOF cancels deletion and follows normal EOF shutdown; Ctrl+C cancels deletion and follows normal graceful shutdown. Arguments such as `nge y` are rejected. Redirected/unattended input and ephemeral sessions cannot delete saves. This is local-operator confirmation, **not authenticated admin enforcement**; human-admin-only access remains future login/authorization work.

Confirmed NGE stops/awaits scheduler, autosave and telemetry, serializes against in-flight saves, revalidates world/backup identity, then removes only the active database, rolling `.backup` and exact `-journal`, `-wal`, `-shm` paths. It retains the session lease through completion and refuses symbolic-link/junction targets. Historical legacy/damaged archives, staging artifacts of uncertain ownership, external copies, other worlds and PC applications remain; the warning explicitly states this scope. The harmless session `.lock` file may remain after release. Interrupted recovery must be resolved before NGE can be prepared.

A flushed `.nullifying` marker is created before deletion. Partial failure retains that marker, blocks startup/save/recovery, reports remaining files and exits nonzero without a final save. **There is no automatic retry/resume command yet:** inspect the marker and remaining exact world artifacts before explicit manual cleanup; never remove the marker merely to bypass an unresolved operation. Successful NGE removes the marker and exits with the GRID7736 artwork and `GRID NULLIFIED // EXISTENCE TERMINATED`, never a saved-world claim. Next normal startup follows existing hardware-city creation; it does not reuse a managed backup. Independent historical/manual copies remain outside this deletion scope.

Verification: **306 Release tests passed** (14 Core, 127 Simulation, 83 Persistence, 82 Terminal). New coverage checks cancellation/blank/invalid/EOF input, uppercase confirmation, redirected/ephemeral rejection, worker shutdown before deletion, no final save on success/failure, owned-file deletion, unrelated-file preservation, rejected forged plans, blocked future writes/recovery, new identity after reopen and a Windows locked-file failure retaining the marker. All deletion tests use freshly created disposable worlds; no user save was deleted. Existing hardware-city creation tests cover the startup path; no manual destructive run against the user's world was performed. Visual Studio still owns its post-exit footer.

## Status and intent

**Initial implementation complete - 2026-10-19.** User-requested terminal command `NGE` / `nge`, named **Nullify Grid Existence**, deletes the currently opened GRID world's save so the next normal startup can create a new world. The current implementation and limits are recorded above; earlier acceptance requirements below remain the design reference. Track implementation in checklist **1.13**.

## Initial command and confirmation contract

Initially the command is available to the local terminal operator, with a warning and explicit y/n confirmation. This initial version has no authenticated administrator enforcement; do not label it secure/admin-only until authentication exists. Do not add a force flag or confirmation bypass.

Proposed prompt (substitute actual identity and absolute paths):

```text
WARNING: NGE - NULLIFY GRID EXISTENCE
This will permanently delete the current GRID world's save and its managed recovery copies.
World: <name> (<world ID>)
Save: <absolute database path>
Files to remove:
  <exact verified paths>
Saved city, residents/agents, clock, event history and application catalog will be lost.
Installed PC applications and their files will not be deleted.
Copies outside this managed file list will remain.
GRID will close without saving. Next startup will create a new world.
Continue? [y/N]
```

Accept only an explicit `y` (case-insensitive, trimmed) as confirmation; `n`, blank input, EOF or cancellation abort safely. Other answers should prompt again without acting. Consume confirmation as a separate input interaction, not as a normal command or queued startup argument. Require interactive confirmation; reject redirected/unattended invocation in the initial version. Cancel returns to the same session with its prior running/paused state intact. Never interpret the command name itself as confirmation.

## Deletion boundary and lifecycle

- Target the active world's canonical database path and verified identity only. No arbitrary path argument, directory deletion, recursive deletion or broad wildcard cleanup.
- Include the primary save, its managed rolling `.backup`, and associated SQLite journal/WAL/SHM files. Determine any owned recovery marker/staging artifacts and legacy/damaged archives through exact validated paths and ownership/identity evidence; display them before confirmation. If ownership is uncertain, leave the file in place and report it. External/manual copies and separately named worlds are outside scope; do not promise secure erasure or destruction of all copies.
- Validate the deletion manifest before asking for confirmation. Revalidate target identity and ownership immediately before deleting; changed targets require a fresh warning/confirmation. Keep other GRID sessions from accessing this world using the existing exclusive session lease throughout the operation.
- After `y`, stop and await the clock, telemetry and autosave workers; serialize against any in-flight persistence writes. Suppress the ordinary final-save path before removing files, so shutdown cannot recreate the save. Close database handles while retaining appropriate exclusive ownership until cleanup completes.
- Delete only the verified file manifest. Handle lock/permission/IO failures explicitly: list removed and remaining files, report incomplete nullification, and use a nonzero exit code. Do not print success, resume simulation or generate a replacement world after a partial deletion. A retry must revalidate the remaining artifacts. A multi-file delete is not inherently atomic; interrupted operations must not silently restore or recreate the old world.
- Successful NGE exits the process without saving or creating a new city in that session. The next normal startup follows existing first-world creation rules, generating and saving a new hardware-based city. No managed stale backup should be left able to resurrect the deleted world. Independent exports/manual copies may still exist.
- `--ephemeral` has no persistent save: report that NGE has no saved world to delete and do not delete any default-world files.
- This deletes GRID simulation data only. It never uninstalls or terminates host applications, removes project source, deletes another world, or changes host hardware.

## Shutdown presentation

Reuse the GRID7736 artwork with an NGE-specific result. Successful completion should say `GRID NULLIFIED // EXISTENCE TERMINATED`, with a plain explanation that the next startup creates a new world. Never print `World saved.` or `WORLD SAVED` after NGE. Cancellation is not an exit; failure uses explicit incomplete-deletion diagnostics. Visual Studio may still append its own process-exit footer.

## Future authenticated restriction

Once terminal login/authorization is implemented, expose NGE only to the authenticated **human user admin / Master Control** role. Agent logins, head-agent delegation, ordinary programs and unauthenticated sessions cannot execute it. Enforce authorization at the destructive operation boundary, not merely by hiding it from help. Recheck authorization at confirmation/commit; an expired or changed session cancels the operation. Display-name matching, occupation, self-declared roles and model-generated instructions do not grant this capability. Admin users must still confirm y/n.

## Acceptance tests and completion evidence

- Warning identifies the exact active world and deletion scope before any mutation.
- `n`, blank, EOF, invalid input and cancellation never delete saves or change the session state; redirected invocation cannot bypass confirmation.
- Confirmed deletion removes the primary and intended recovery artifacts, leaves unrelated worlds/files intact, and never triggers autosave/final-save recreation.
- Exercise running/paused sessions, in-flight autosave, open handles, permission failures, changed identity, interrupted/partial deletion and ephemeral mode using disposable worlds only.
- Verify the next startup produces a new world identity and normal hardware-city creation, while cancellation preserves the original identity.
- Verify success/failure banners and exit codes; no false saved or fully-deleted claims.
- When authentication arrives, test human-admin authorization and rejection of agent/ordinary/expired sessions at both request and commit.
- Record implementation date, test results and limitations in this document, the master checklist and current implementation status when delivered. See current completion evidence above; future authenticated enforcement is not yet implemented.
