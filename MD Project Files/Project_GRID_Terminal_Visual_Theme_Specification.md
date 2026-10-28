# 6.10A — Terminal visual theme

**Status: Planned — next development item after completed 6.10; required before Phase 7.** Added 2026-10-28. No theme implementation is included in this planning update.

The complete user-supplied specification follows unchanged. It refines the earlier yellow-output convention: generic notices remain amber, while structured errors, success, descriptions and approvals gain semantic colors. It also calls for compact major-screen headers rather than oversized logos; existing banner/animation history is preserved in earlier documents, but the new presentation requirements govern this item. Keep command semantics, authentication, authorization, confirmation and simulation behavior unchanged. A new `help <command>` feature is outside this presentation-only scope if not already implemented.

Implementation order: centralized palette and capability fallbacks; semantic renderer/layout integration; prompts and responses; help; remaining screens; automated and native-console verification. Preserve readable monochrome/redirected output and the operator's host background where GRID does not own the surface.

---

# PROJECT GRID — Terminal Visual Theme Implementation Specification

## Objective

Implement a consistent, modern terminal color theme across Project GRID.

The terminal should resemble a sophisticated digital command-and-control interface inspired by TRON, with restrained cyan accents, deep navy backgrounds, clean typography, and clearly differentiated information.

The visual experience should feel:

- Professional and sophisticated
- Technologically advanced
- Organized and consistent
- Easy to read for extended periods
- Integrated with GRID's existing visual identity
- Functional rather than excessively decorative

**Use solid colors only.** Do not introduce gradients, glowing effects, excessive animations, or unnecessary decorative elements.

This is primarily a presentation-layer update. Existing command behavior, authorization, simulation functionality, and data handling must remain unchanged.

---

## 1. Global Color Palette

Create a centralized terminal theme containing the following colors.

| Color name | HEX | RGB | Intended purpose |
|---|---|---|---|
| Background | `#101820` | 16, 24, 32 | Primary terminal background |
| Header Background | `#172C3C` | 23, 44, 60 | Major title bars |
| Section Background | `#234A5F` | 35, 74, 95 | Category heading bars |
| Primary Cyan | `#59D8F3` | 89, 216, 243 | Main titles and important highlights |
| Command Cyan | `#57D7F1` | 87, 215, 241 | Command names and syntax |
| Primary Text | `#FFFFFF` | 255, 255, 255 | User-entered text and important values |
| Secondary Text | `#C2CBD4` | 194, 203, 212 | Descriptions and informational text |
| Muted Text | `#8C9FAE` | 140, 159, 174 | Hints, metadata, and supporting information |
| Border | `#34505F` | 52, 80, 95 | Dividers and box outlines |
| Warning Amber | `#F3C76A` | 243, 199, 106 | Warnings, notices, and tips |
| Success Green | `#63DB9C` | 99, 219, 156 | Successful operations and ready states |
| Error Red | `#F07878` | 240, 120, 120 | Errors and failed operations |
| Administrative Gold | `#C8AD72` | 200, 173, 114 | Administrative and approval-related labels |

These colors must be defined centrally rather than hardcoded throughout individual commands.

The terminal should use the same semantic colors for equivalent information everywhere.

---

## 2. Background and General Appearance

The terminal's preferred background is:

`#101820`

This dark navy background should replace pure black wherever GRID owns the terminal rendering surface.

Use:

- Dark navy as the primary background
- Steel blue for section headers
- Cyan for commands and important identifiers
- White for user-entered text
- Light gray for descriptions
- Muted blue-gray for secondary information

Avoid applying colored backgrounds to ordinary paragraphs or every individual command.

The background should remain visually quiet, allowing important information to stand out.

For terminals where GRID cannot control the host background, retain the user's existing background and use the foreground palette with appropriate contrast.

---

## 3. Preserve Existing Terminal Color Behavior

GRID already defines certain terminal color conventions.

Preserve these conventions while expanding the visual theme.

### User input

User-entered commands and messages must remain white.

**Color:** `#FFFFFF`

Example:

`grid[Master Control]> status`

The user's typed command should display in white regardless of its contents.

Do not automatically recolor individual words while the user is entering a command.

### Command prompts

Use light blue for terminal prompts and interactive labels.

**Color:** `#59D8F3`

This includes:

- `grid[Master Control]>`
- Agent-specific terminal prompts
- Input requests
- Selection prompts
- Interactive field labels

### General system messages

Existing yellow system-output behavior should remain recognizable.

Use `#F3C76A` for notices, guidance, and generic terminal messages where the existing convention applies.

However, structured command output should use semantic colors where a specific role is known.

For example:

- Descriptions: light gray
- Successful results: green
- Failed results: red
- Section titles: cyan
- Warnings: amber

Do not make all structured output yellow.

### Errors

Use error red (`#F07878`) for explicit failures and errors.

This is an intentional refinement of the earlier yellow-message convention.

Warnings should remain amber.

---

## 4. Terminal Header Design

Major terminal screens should use a recognizable GRID header.

The header should feature:

- A solid dark-blue background when supported
- A cyan project title
- A descriptive screen label
- A clean separator beneath the header

Example:

```text
  PROJECT GRID                                  MASTER CONTROL
  ════════════════════════════════════════════════════════════
  COMMAND TERMINAL                                  HELP / INDEX
```

Formatting:

- `PROJECT GRID`: primary cyan
- `MASTER CONTROL`: secondary text
- Horizontal divider: border color
- `COMMAND TERMINAL`: white or primary cyan
- Screen identifier: muted text

Do not use oversized ASCII-art logos.

Headers should remain compact and readable at standard terminal sizes.

---

## 5. Section Header Design

Use solid-colored section headers to separate major command categories.

Example:

```text
  01 / SIMULATION CONTROL
  ────────────────────────────────────────────────────────────

  grid          Start, pause, copy, or transfer GRID
  step          Advance the simulation by one tick
  tick          Alias for step
```

The section heading should ideally use:

- Background: `#234A5F`
- Foreground: `#FFFFFF`

Command names:

- Foreground: `#57D7F1`

Descriptions:

- Foreground: `#C2CBD4`

The horizontal divider:

- Foreground: `#34505F`

For this design, prefer full-width solid heading bars over large rectangular boxes surrounding each category.

Solid bars are easier to scan and create less visual clutter.

---

## 6. Command List Formatting

All command lists should use consistent spacing and alignment.

Use three logical columns when appropriate:

1. Command name
2. Arguments or options
3. Description

Example:

```text
  COMMAND          OPTIONS                 DESCRIPTION
  ────────────────────────────────────────────────────────────
  grid             -start                  Start simulation
  grid             -pause                  Pause simulation
  programs         -active                 Show running programs
  resources        details                 Show resource details
```

Color assignments:

- Column headings: primary cyan
- Command names: command cyan
- Arguments: white or secondary text
- Descriptions: secondary text
- Dividers: border color

Column spacing should be calculated dynamically.

Do not insert ANSI color sequences before calculating the visible width of the text.

A colored command must occupy the same visual width as its uncolored equivalent.

Long descriptions should wrap into the description column without disrupting the alignment of following entries.

---

## 7. Command-Specific Help Screens

The detailed command-reference interface should follow a consistent structure.

Recommended sections:

1. Command name
2. Description
3. Usage
4. Options and arguments
5. Examples
6. Important notes
7. Related commands

Example:

```text
  PROJECT GRID                                    COMMAND HELP
  ════════════════════════════════════════════════════════════

  GRID
  ────────────────────────────────────────────────────────────

  CATEGORY     Simulation Control
  ACCESS       Master Control

  DESCRIPTION
    Manage simulation execution and GRID transfers.

  USAGE
    grid <option> [arguments]

  OPTIONS
    -start               Start or resume simulation
    -pause               Pause simulation
    -copyto              Preview a GRID copy
    -transferto          Preview a GRID transfer

  EXAMPLES
    > grid -start
    > grid -pause

  IMPORTANT
    Copy and transfer operations require confirmation.

  ────────────────────────────────────────────────────────────
```

Suggested formatting:

- Command title: primary cyan
- Section headings: administrative gold or cyan
- Syntax: command cyan
- Arguments: white
- Descriptions: light gray
- Examples: command cyan
- Important notices: warning amber

This is a proposed visual layout. Adding `help <command>` is a separate functionality change if that syntax is not currently implemented.

---

## 8. Status and System Feedback

Status indicators should use consistent terminology and color coding.

### Success

Color: `#63DB9C`

Examples:

```text
  [SUCCESS]  Operation completed successfully.
  [READY]    GRID is ready.
  [ACTIVE]   Simulation running.
```

### Warning

Color: `#F3C76A`

Examples:

```text
  [WARNING]  Simulation is currently paused.
  [NOTICE]   Confirmation is required.
```

### Error

Color: `#F07878`

Examples:

```text
  [ERROR]    Command not recognized.
  [FAILED]   Operation could not be completed.
```

### Informational

Color: `#59D8F3`

Examples:

```text
  [INFO]     Loading simulation data.
  [SYSTEM]   GRID initialized.
```

### Neutral

Color: `#8C9FAE`

Examples:

```text
  [IDLE]     No pending operations.
  [UNKNOWN]  Resource information unavailable.
```

Use color as an enhancement, not the only means of communicating state.

All status indicators must remain understandable in monochrome terminals.

For commands that display actual host measurements or security information, do not imply that a simulated status establishes the security or health of the host computer.

---

## 9. Agent Identity Presentation

GRID's named system agents should share the overall terminal palette.

Do not introduce a completely different visual theme for each agent.

Use a consistent structure:

```text
  AGENT       MCP
  ROLE        Administration & Coordination
  STATUS      ACTIVE
```

Apply:

- Agent identifier: primary cyan
- Field labels: muted text
- Field values: white
- Status: semantic status color

The following agent names should always use consistent capitalization:

- MCP
- SENTINEL
- MAXIS
- JN
- VOLT
- RELAY

Administrative or approval-related headings may use the gold accent (`#C8AD72`).

Agent-specific colors may be introduced later, but they should not override status meanings or reduce readability.

---

## 10. Approval and Security-Related Output

GRID uses human authorization for sensitive operations.

These actions must be visually distinct.

Use administrative gold for approval-related headings and labels.

Example:

```text
  APPROVAL REQUIRED
  ────────────────────────────────────────────────────────────

  OPERATION    Apply workspace modification
  TARGET       <workspace file>
  STATUS       Awaiting approval

  Review the proposed change before continuing.
```

Formatting:

- `APPROVAL REQUIRED`: administrative gold
- Operation labels: muted text
- Values: white
- Pending approval: amber
- Approved state: green
- Rejected state: red

Color changes must not alter the existing approval workflow.

Theming must never:

- Automatically approve tasks
- Bypass confirmation
- Change permission checks
- Conceal high-risk actions
- Misrepresent pending changes as applied

The visual interface should accurately reflect the existing authorization state.

---

## 11. Terminal Layout and Width Handling

Build the formatting system to accommodate different terminal widths.

Recommended behavior:

- Normal layout for terminals at least 80 columns wide
- Reduced padding for moderately narrow windows
- Stacked command descriptions when columns no longer fit
- No horizontal clipping of essential information

Avoid hardcoding a single terminal width.

Box-drawing characters may be used when supported.

Provide ASCII-compatible replacements where Unicode rendering is unavailable.

Use line-drawing characters consistently:

```text
  ─────────────────────────────────────────
```

Avoid mixing several unrelated border styles in the same screen.

---

## 12. C#/.NET Implementation Guidance

Implement a reusable terminal styling layer.

Suggested architecture:

**GridTerminalTheme**

Centralizes the colors and semantic text styles.

**GridTerminalRenderer**

Handles terminal output, text formatting, colored segments, and background fills.

**GridTerminalLayout**

Handles alignment, column widths, separators, wrapped text, and responsive terminal layouts.

Suggested methods:

```csharp
WriteTitle(string text)
WriteSection(string title)
WriteCommand(string name, string description)
WriteDescription(string text)
WriteSuccess(string message)
WriteWarning(string message)
WriteError(string message)
WriteInfo(string message)
WritePrompt(string prompt)
WriteDivider()
```

These are suggested interfaces, not requirements to rename or replace existing GRID classes.

Integrate the theme into the existing terminal rendering architecture wherever possible.

### Color support

Prefer ANSI truecolor (24-bit RGB) rendering where supported.

Example:

```text
ESC[38;2;89;216;243m
```

This sets the foreground to GRID's primary cyan.

For colored backgrounds, use the corresponding ANSI background sequence.

Example:

```text
ESC[48;2;35;74;95m
```

This applies the section-header steel-blue background.

Reset styling appropriately after each formatted segment to prevent color bleeding.

Do not assume every terminal supports ANSI truecolor.

Provide a fallback using the nearest available console colors, and a plain-text rendering mode.

If the existing terminal uses WPF text elements, apply the same palette through WPF brushes rather than emitting ANSI escape sequences into unsupported text controls.

### Text rendering safeguards

The styling system must:

- Preserve text content
- Preserve command input behavior
- Support redirected output without raw escape codes
- Prevent color bleeding
- Respect Unicode character width where supported
- Handle terminal resizing
- Keep prompt/input cursor positioning correct
- Avoid splitting Unicode characters during wrapping
- Never count ANSI escape codes toward visible column widths

Terminal colors should be controlled by reusable semantic styles.

Avoid scattered `Console.ForegroundColor` assignments or embedded escape sequences across individual command handlers.

---

## 13. Scope of Implementation

Apply the theme consistently to:

- Main terminal header
- Login and welcome output
- Command prompt
- General command responses
- Help command
- Simulation status
- Agent information
- Monitoring and analytics
- Task management
- Approval previews
- Conversation labels
- System messages
- Warnings and errors

The initial implementation should focus on color consistency and visual formatting.

Do not change command names, arguments, parsing behavior, or functionality as part of the theme update.

An improved help navigation system can be implemented separately.

---

## 14. Verification Requirements

Verify the theme against the following scenarios:

- [ ] Terminal starts with the correct background where supported.
- [ ] Prompts display in light blue.
- [ ] User input remains white.
- [ ] Section headings use solid steel-blue backgrounds.
- [ ] Commands use cyan.
- [ ] Descriptions use light gray.
- [ ] Warnings use amber.
- [ ] Success messages use green.
- [ ] Errors use red.
- [ ] Approval notices are visually distinct.
- [ ] Colors reset properly after each output segment.
- [ ] Help output remains aligned.
- [ ] Long descriptions wrap correctly.
- [ ] Narrow terminal windows remain usable.
- [ ] Unsupported terminals fall back gracefully.
- [ ] Redirected output contains no unintended ANSI codes.
- [ ] Existing terminal commands continue functioning.
- [ ] Existing authentication and approval rules remain unchanged.

Run the existing terminal and integration tests after implementation.

Add focused tests for formatting, escape-sequence handling, and visible-width calculations where appropriate.

---

## 15. Final Design Principle

**GRID should look like a sophisticated digital operating environment, not a conventional debugging console.**

Its visual identity should combine:

- The precision of professional development tools
- The authority of an administrative command interface
- The restraint of traditional institutional design
- The subtle digital character of TRON

The goal is to give every terminal screen a recognizable GRID identity while maintaining clarity, efficiency, and accessibility.

**Implementation priority:**

1. Establish the central color theme.
2. Integrate semantic output styling.
3. Apply the theme to prompts and general responses.
4. Redesign help output using consistent sections and columns.
5. Extend styling across agent, status, monitoring, and approval screens.
6. Verify behavior, accessibility, and compatibility.

Preserve all existing application functionality throughout the implementation.