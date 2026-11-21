# Project GRID — 6.7 Login Animation and Terminal Palette

**Grid.AI logo replaced and responsively refined — 2026-11-20.** Startup, login animation and graceful shutdown now use a centered 78-column framed Grid.AI mark for normal terminal widths. It applies the established Grid.AI palette: bright cyan `GRID`, cyan `AI` and frame, a white connector accent, and a dark-gray intelligence subtitle. The supplied 12-line, 118-column artwork remains embedded exactly and appears only when a window is at least 140 columns wide, centered with safe margins. Python and external artwork files are not runtime dependencies. Redirected/monochrome output preserves geometry without escape codes; sub-78-column windows use compact branding. The five-second reveal, skip behavior, truthful shutdown status and reduced-motion behavior are unchanged. Fourteen focused logo/animation/palette tests passed. The older GRID7736 artwork below is retained only as historical evidence and is superseded by this note.

**Implemented 2026-10-27.** The user-supplied block-letter GRID7736 banner replaces the earlier opening and closing artwork. Successful login now reveals it over five seconds and enters the terminal automatically, without the post-login Enter prompt. Welcome name and authority remain visible. The artwork uses Unicode box/block characters, as supplied; narrow windows use a compact text logo.

```text
+======================================================================+
|                                                                      |
|   ██████╗ ██████╗ ██╗██████╗     ███████╗███████╗██████╗  ██████╗    |
|  ██╔════╝ ██╔══██╗██║██╔══██╗    ╚════██║╚════██║╚════██╗██╔════╝    |
|  ██║ ███╗██████╔╝██║██║  ██║        ██╔╝    ██╔╝ █████╔╝███████╗    |
|  ██║   ██╗██╔══██╗██║██║  ██║       ██╔╝    ██╔╝  ╚═══██╗██╔══██╗    |
|  ╚██████╔╝██║  ██║██║██████╔╝       ██║     ██║  ██████╔╝╚██████╔╝    |
|   ╚═════╝ ╚═╝  ╚═╝╚═╝╚═════╝        ╚═╝     ╚═╝  ╚═════╝  ╚═════╝    |
|                                                                      |
+======================================================================+
```

## Behavior

- The animation uses a monotonic elapsed-time deadline rather than five seconds of accumulated frame delays. It progressively reveals the logo and shows a progress bar. Ordinary display/scheduling overhead can add a small amount to the observed duration.
- Any key skips the animation and shows the complete logo and welcome. The skip key is consumed; type commands after the terminal prompt appears.
- Set `GRID_ANIMATION=off` for reduced motion. Redirected output, `TERM=dumb`, or an unavailable clear operation shows static artwork immediately. No redirected input is consumed by the transition. Password authentication still requires interactive masked input.
- The final banner is static on shutdown; saved/ephemeral/error/Nullification status remains truthful. No second animation delays exit.
- Yellow: all terminal-generated messages, agent bodies, errors, tables, dividers and source/detail footers. Light blue: explicit prompts and speaker labels. White: typed user input and displayed user-message bodies.
- Errors retain their error/refusal wording. They are no longer red. Live output, original and attached terminals use the shared palette. `--no-color`, `NO_COLOR`, redirected output and dumb terminals retain plain text without injected escape sequences.

To disable animation for the current PowerShell session before launching GRID:

```powershell
$env:GRID_ANIMATION = 'off'
```

Remove that environment variable or set it to `on` to restore the default animation. `GRID_TYPEWRITER` remains a separate preference for agent replies.

## Verification

**886 Release tests passed**: 14 Core, 157 Simulation, 99 Persistence and 616 Terminal. Thirteen new 6.7 cases cover real/fake elapsed clocks, slow frames, immediate skip, cancellation, static fallback, reduced motion, banner/shutdown text and explicit prompt/footer colors. Updated login tests prove no extra Enter is consumed; existing real attached-process and conversation-presentation tests pass.

A Windows ConPTY probe exercised the real authentication UI, console input, animation and output renderer with an isolated synthetic identity. Login reached the prompt in **5.109 seconds** without another Enter. Captured console color sequences confirmed yellow output, light-blue prompts and white input. The password was not echoed. [Captured report](verification/6.7-console-report.md).

The probe explicitly bound its synthetic process to ConPTY console handles, since the test runner otherwise inherits redirected handles. It did not open the user's saved world. A preliminary dumb-terminal run confirmed static behavior; the interactive evidence used `TERM=xterm`. This validates the Windows console path, not every font/theme or a manual inspection of the user's particular open window. Full user-window acceptance remains in the pre-Phase-7 readiness gate (6.15).

Run the focused suite from repository PowerShell:

```powershell
dotnet test Grid.Terminal.Tests/Grid.Terminal.Tests.csproj -c Release --filter "Phase=6.7"
```

For a visual check, restart GRID, log in, wait without pressing Enter, then repeat and skip with a key. Confirm the authority text is readable, exercise an error and a table, and inspect an agent conversation/attached window. Verify static behavior with `GRID_ANIMATION=off`.

No schema migration, additional runtime dependency, or change to agent permissions. Sections 6.8–6.15 remain planned; the conversation loop, PDF support and reported second-device AI failure are not fixed by 6.7.
