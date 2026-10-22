# Project GRID — 5.3 MAXIS Development

## End-to-end task verification — 5.7 completed (2026-10-21)

MCP delegation, human approval, real MAXIS unit-test execution and RELAY recorded-result delivery are verified together, including actual passing/failing TRX evidence and SQLite reload. Nine new cases also cover all six agent roles, cancellation, durable-start recovery and duplicate dispatch prevention. **560 tests passed** (14 Core, 157 Simulation, 97 Persistence, 292 Terminal), zero failed/skipped. No runtime/schema change was needed; schema remains v19. See [verification matrix, boundaries and terminal test](Project_GRID_End_to_End_Task_Verification.md). Next: **6.1 Shared local AI**. Earlier milestone counts and next-step statements below are historical.


## Implemented — 2026-10-21

MAXIS now performs real, explicitly approved development actions: read a registered source file, run a fixed .NET build/test command, or stage a user-specified literal replacement in a separate working copy. MCP can delegate these actions. Every task uses the existing per-attempt human approval, durable-start barrier, live identity checking, cancellation and saved result/audit workflow. Other specialists remain demonstrations.

## Register a workspace

```text
agents initialize
maxis register grid "C:\Users\jack\source\repos\Grid7736" "Grid.Core/Grid.Core.csproj"
maxis workspaces
```

Only human Master Control can register/revoke. Registration records an alias, absolute root, relative .csproj, machine name and enabled flag. It does not approve tasks. Up to 32 aliases are supported; aliases are case-insensitively unique, immutable and never reused, including after revocation. Use a different alias for another project or machine. MCP/MAXIS can see workspace metadata; other agents cannot. Registry state persists in schema v16 WorldTasks JSON; v15 worlds migrate with an empty registry and no regeneration.

Registration requires an existing local directory/project, refuses drive roots, UNC paths and symbolic links/junctions, and prevents overlap with GRID's artifact directory. Source paths must be relative, without traversal or alternate data streams. Runtime rechecks the registry, machine and paths; revoked/missing workspaces cannot execute. This is a trusted-workspace feature: external concurrent filesystem changes and hostile project code are not contained by an OS sandbox.

## Commands and approval

```text
maxis
    - maxis workspaces
      # USAGE: List authorized workspace registrations.
    - maxis register <alias> "absolute-root" "relative/project.csproj"
      # USAGE: Master Control registers an immutable workspace target.
    - maxis revoke <alias>
      # USAGE: Prevent further work and request cancellation of current work.
    # EXAMPLE: maxis revoke grid
```

```text
tasks propose MAXIS maxis.inspect "grid|Grid.Core/GridProgram.cs" inspect-1
tasks propose MAXIS maxis.build grid build-1
mcp request "MAXIS: build grid" mcp-build-1
```

Each proposal requires `tasks inspect <task-reference>`, `tasks approve <task-reference>`, then `tasks run` from the original host terminal. Run selects the next approved queued task. Do not approve duplicate direct/MCP proposals unintentionally. Task details show the registered root/project and real-code-execution warning. Approval covers the configured action against the workspace's current files at execution; source contents are not frozen at proposal time. Ordinary chat/voice still does not dispatch work.

To test a test project, register it under a separate alias:

```text
maxis register coretests "C:\Users\jack\source\repos\Grid7736" "Grid.Core.Tests/Grid.Core.Tests.csproj"
tasks propose MAXIS maxis.test coretests tests-1
```

Fixed invocation uses the installed Program Files/dotnet/dotnet.exe with ArgumentList and no command shell: build/test, registered project, --no-restore, --configuration Debug, --verbosity minimal, --disable-build-servers. Test additionally requests TRX in the attempt directory. No user-supplied executable, flags or shell command is accepted. An installed SDK and previously restored dependencies are required. MAXIS itself does not restore/download dependencies; CLI telemetry and first-time setup are disabled.

**Build/test runs project-defined code as the current Windows user in the registered workspace.** It may write bin/obj and other outputs or invoke project-defined actions; fixed CLI arguments are not a security sandbox. Only approve trusted offline projects. MAXIS does not enforce a network firewall; arbitrary project targets/tests may use the network or external paths. An exit code of zero means the process succeeded, not proof that tests were discovered or that application behavior is correct.

## Inspection and suggested changes

Arguments use `|` separators. Full argument length remains 256 printable characters. Inspect syntax: `alias|relative/file`. Suggest syntax: `alias|relative/file|old-literal|new-literal`. Delimiters cannot occur inside fields; MCP also reserves semicolons. Suggestions require exactly one old-literal match, support an empty replacement, and do not support multiline patches or natural-language code generation.

```text
tasks propose MAXIS maxis.suggest "grid|relative/file.cs|OldName|NewName" suggestion-1
```

Replace the example path/literals with a file and change you have reviewed. After approval, GRID writes a candidate under the attempt's working-copy directory, preserves original.txt, and writes a replacement report. **The original source is never automatically changed by inspect/suggest.** The working copy contains the changed file, not a complete buildable repository checkout. Applying/merging it remains a manual review step; there is no automatic source-apply capability.

Supported text extensions: .cs, .csproj, .sln, .slnx, .props, .targets, .md, .txt, .json, .xml, .yml, .yaml. Files must be valid UTF-8 text, without NUL, and at most 1 MiB at the size check. Inspection saves full text to report.txt rather than flooding the terminal. Registered source scope may contain sensitive text; reports are local files with inherited filesystem permissions.

## Results, lifecycle and portability

Artifacts are stored beside the selected database in `<database-path>.maxis-artifacts/<task-id-without-hyphens>-<attempt>/`. Build/test records report.txt, stdout.txt, stderr.txt and TRX when the test runner produces it. Each output stream captures up to 1 MiB and reports truncation; stdout/stderr continue draining. Nonzero exit codes fail the broker task. Completed tasks retain a concise result pointing to the report. Failed/timed-out attempts identify their artifact directory if created. No output directory is overwritten on retry.

MAXIS attempts have a 120-second deadline; demos retain one second. Cancellation, workspace revocation, logout and host shutdown cancel work and request process-tree termination. Already performed external actions cannot be rolled back. Retries require fresh human approval, up to the existing three-attempt limit. Restart clears queued approval, marks running attempts Interrupted, and never automatically replays or reattaches to a possibly orphaned external process. Inspect partial logs and running processes after a crash before retrying.

SQLite saves/backups contain workspace registrations, task results and audit, not artifact file contents. Back up the companion artifact directory too. GRID copy/move transfers now include that selected world's artifact directory; registered external repositories are not copied. Historical absolute report paths in saved results are not rewritten on transfer: locate the same task/attempt folder beside the destination database. Registrations on a different machine refuse execution; register a new local alias. NGE continues deleting its displayed database/backup targets; external MAXIS reports/working copies are retained and require separate manual cleanup. Automatic artifact pruning is not implemented.

## Completion verification

505 tests passed: Core 14, Simulation 157, Persistence 97, Terminal 237. Seventeen new MAXIS cases cover source preservation, unique replacement, traversal/stream/argument rejection, authority and revocation, actual disposable MSBuild build/test targets, captured failures, process cancellation, linked paths, persistence fencing, SQLite round-trip and v15 migration. A transfer test verifies artifact inclusion. Fixture test-target execution checks invocation/output; it does not claim real unit-test discovery.

Implemented checklist scope includes explicit suggested replacements and working-copy staging. Autonomous code generation, applying source patches, full repository sandbox builds, OS/network isolation, configurable runners and artifact archival remain future work. Next module: 5.4 SENTINEL — Security.
