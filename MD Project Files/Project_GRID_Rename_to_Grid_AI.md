# Grid.AI project rename

## Implemented — 2026-11-17

The active product name is **Grid.AI**. Maintained source, terminal branding, startup and shutdown artwork, window titles, named-pipe prefixes, help text, transfer/package defaults, tests, solution commands and documentation now use that name. The solution entry point is `Grid.AI.slnx`.

Windows startup now creates the `Grid.AI` user environment variable and the managed `%LOCALAPPDATA%\GRID\bin\Grid.AI.cmd` launcher. In PowerShell, the dot in the variable name requires braced syntax:

```powershell
& ${env:Grid.AI}
```

`Grid7736` remains only as a compatibility alias. Startup can migrate a valid legacy variable, and a transfer can retarget a legacy managed launcher that already points at the transferred installation. GRID does not replace an unrelated valid installation or unmanaged launcher.

The physical checkout folder, GitHub repository slug, immutable test fixtures and historical evidence were not renamed. Raw verification logs, observed paths and commit URLs therefore retain their original identifiers. Project and assembly folders such as `Grid.Core`, `Grid.Agent` and `Grid.Terminal` also remain unchanged; `Grid.AI` is the product/solution name, not a namespace-wide assembly rename.

## Verification

- Focused branding, launcher, window, transfer and fresh-clone suite: **132 passed**.
- Full `dotnet test Grid.AI.slnx`: **1,466 passed, zero failed/skipped** (14 Core, 157 Simulation, 99 Persistence and 1,196 Terminal).
- A child PowerShell process successfully set and read `${env:Grid.AI}`.
- `git diff --check` passed; line-ending notices were informational.

No repository remote, checkout directory, saved world or external installation was renamed by this change.
