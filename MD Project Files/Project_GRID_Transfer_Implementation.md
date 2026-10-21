# Project GRID - Move and Copy Implementation

## 4.2D delivery - 2026-10-20

Master Control (Master Admin) can run `tranexist "location"` to move GRID or `copexist "location"` to copy GRID. The destination must be a new absolute directory whose parent already exists. Agent and logged-out sessions are denied. Commands preview the source, selected world, destination, file count and size, then require interactive y/n confirmation. Both commands close the terminal after stopping workers and saving the latest state. Redirected confirmation is refused.

Examples:

```text
copexist "E:\GRID7736 Copy"
tranexist "E:\GRID7736"
```

### Actual file scope

The transfer inventories every file under the running application directory, including bundled dependencies and Markdown documentation, plus the selected world's database, backup, historical archives and matching sidecars. Live `.lock` files are excluded. An existing transferred package also includes its managed folders such as models, configuration, assets, logs and data. Destination files are organized under `app/` and `data/`, with `launch.cmd`, `grid-package.json`, `transfer-manifest.json` and `transfer-status.txt` at the root.

The source Git repository, unrelated host applications/user documents, other installed worlds, and shared user PATH launcher are not part of the movable installation inventory. Runtime/session handles are not transferred. The shared managed launcher is retargeted after a move only when it and the user Grid7736 variable point at the moved installation; unrelated registrations are preserved. Empty source directories, lease files and old generated package metadata can remain. Completion describes removal of the verified source file inventory, not recursive deletion of the entire source directory.

### Verified transfer and move handoff

One host retains its world lease while workers stop, final save completes and files are copied. The service rechecks Master Control before copying and publication, refuses changed file inventories, overlapping/existing destinations, missing parents, junctions/symlinks and insufficient free space. It stages the copy, verifies SHA-256 hashes against the source, loads the destination database through WorldStore, and then publishes the destination directory. Failure retains the source; failed staging directories remain visibly incomplete for diagnosis.

`copexist` retains the original. Its destination is an independent copy of the same saved world, with a separate physical database and lease; it is not another terminal attached to the original running simulation. Login is required. Host observations refresh on startup and the saved city is preserved.

`tranexist` starts a hidden local cleanup helper from the verified destination. It waits for the original process to exit, reacquires the source world lease, and locks/verifies every inventory file and corresponding copy before deleting any source inventory file. Active sources, altered data, or locked files cause refusal. The helper writes success/failure to `transfer-status.txt`; the parent reports cleanup as pending, never as already completed. Starting the destination is blocked while cleanup is pending. A later deletion/launcher-update error can leave some source files behind, with the verified destination retained and an incomplete status. A move is not an atomic cross-volume operation. Do not remove the destination during cleanup.

A helper timeout or failed launch leaves an incomplete status and source files; a crash before updating status may leave a pending marker. Inspect the manifest/status and verify the destination before manual recovery. Automatic resume/rollback of partial source deletion is not implemented.

### Starting the transferred GRID

Use the destination's `launch.cmd`. Package startup selects `data/world.db` and skips automatic host PATH/registry installation. Existing Windows-owner worlds still require their owner account; configure portable password credentials using `credentials` before moving between PCs. Password hashes and saved identities/transcripts travel with the database; authenticated sessions do not.

The current launcher/helper uses `dotnet`, so a compatible .NET 10 runtime must already be installed. This feature does not provision runtime/models or fulfill the full PC GRID/Flash GRID installer and offline release gates. Package transfer requires its standard `data/world.db`; transferring an arbitrary alternate world from an existing package is refused.

### Verification and remaining integration

377 tests passed: 14 Core, 155 Simulation, 91 Persistence and 117 Terminal. Nine new transfer tests cover quoted paths/Master Control denial, cancellation, redirected refusal, destination validation, revocation and changed inventory, copy integrity and world loading, latest-state final save, changed-copy/active-source cleanup refusal, exact source inventory cleanup, and the actual cleanup subprocess using disposable files. Build: zero warnings/errors. No user installation or world was moved during testing.

Future work remains for 4.2C attached-window coordination, 8.6 full PC/Flash edition packaging and migration, exhaustive low-space/power-loss/cross-machine tests, and automatic interrupted-move recovery. The existing current-host transfer path is implemented; those broader gates remain unchecked.
