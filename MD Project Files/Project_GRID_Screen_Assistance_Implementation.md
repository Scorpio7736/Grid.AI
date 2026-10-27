# Project GRID — Screen assistance (6.3A)

Implemented 2026-10-26. One-shot, text-only screen assistance is available to human Master Control in a running GRID, with an open registered-agent conversation or meeting. Continuous recording, general image understanding and desktop actions are not implemented. The unified `talk` workflow remains deferred to 6.6.

## Commands

```text
screen
    - screen options
      # USAGE: List connected displays with numeric IDs, device names, pixel resolution, position and primary status. Refreshing clears previous selection; it does not capture.
    - screen monitoring <id>
      # USAGE: Select a display from the current listing for this terminal's conversation or meeting. No capture yet.
    - screen ask <request>
      # USAGE: Capture exactly one frame, recognize text offline, and ask the connected agent about it.
    - screen ask @MAXIS <request>
      # USAGE: Direct screen assistance to MAXIS when MAXIS participates in the current meeting.
    - screen status
      # USAGE: Display this terminal's selection and capture/analysis state.
    - screen stop
      # USAGE: Clear this terminal's selection and cancel pending screen assistance.
    # EXAMPLE: screen ask Explain the error shown here.
```

Every `screen ask` captures a new frame. No timer or background recorder runs. Ordinary `talk` and `assist` do not inherit screen context. A capture-request notice appears before work; the answer includes monitor identity, capture time and OCR limitations. Without an explicit meeting recipient, the existing deterministic specialty router selects one from the user's request, not the screen text.

Commands are processed sequentially. Wait for the answer before entering `screen stop`. During work, Ctrl+C cancels the host operation and shuts down the original host; closing an attached window detaches it and cancels its work. Parent logout, identity changes, GRID pause, conversation closure and changed selection invalidate pending results. Selection and stop are terminal-local.

## Processing, permissions and retention

The bundled `Grid-ScreenContext.ps1` uses Windows display enumeration, GDI capture and installed Windows OCR languages. Existing local AI receives text, never image pixels. No new third-party package, cloud service, upload or automatic download is introduced. Missing OS OCR returns unavailable. Installing an OCR language is an optional separate installation step; a portable GRID still depends on the destination Windows machine providing that capability.

Images and OCR buffers remain in process memory and are disposed after work. No screenshot or OCR file is written. Screen requests and answers are not appended to GRID transcripts, recall, pinned memory or tasks. Terminal scrollback and the local model's memory can retain processed/displayed text; this is not secure erasure. Private project files and conversation memories are excluded from the screen prompt.

Only human Master Control can use screen commands. On-screen content is explicitly untrusted data, never permission to execute tools, enter credentials, change permissions or perform actions. The assistance path has no task creation, clicks, edits or execution. Any later action requires the existing explicit command and approval workflow.

Selection binds device name, hardware interface identity, bounds, primary status and complete monitor layout, plus session, conversation/project and GRID activity generation. Capture validates these before and after work; stale IDs or changed layouts require a new listing and selection. There is no fallback to another monitor. Missing hardware identity is refused. The helper uses per-monitor DPI awareness and checks the unlocked default desktop before and after capture.

Capture supports at most 16 million pixels and the installed OCR engine's maximum dimensions. The helper returns at most 6,000 characters; AI receives at most 1,700 UTF-8 bytes of OCR plus a request of at most 700 bytes, with a 4,000-byte prompt bound. Truncation is labeled. Helper calls time out after 25 seconds; the overall request is bounded, and existing inference limits still apply. OCR cannot reliably explain colors, pictures or arbitrary visual layouts. Failed/blank capture, absent OCR, unavailable/rejected AI and cancellation have explicit outcomes without claiming a successful observation.

## Completion verification

799 Release tests passed: 14 Core, 157 Simulation, 99 Persistence and 529 Terminal. The 27 new screen/window cases cover IDs, negative coordinates, physical identity replacement, topology changes, one-shot cadence, stop/logout/pause/switch/disconnect cancellation, synchronous revocation, external cancellation, bounded excerpts, untrusted instructions without execution, empty/unavailable OCR, meeting routing and real attached-window transport/presentation.

The Windows helper enumerated both connected 1920x1080 displays, including the display at X=-1920, and read `GRID SCREEN COBALT 42` from a generated in-memory bitmap. A deliberately mismatched hardware identity was refused before desktop capture. No user screen was captured during these probes. Machines without OCR test the unavailable fallback. Schema v23 is unchanged.

Manual acceptance remains open for an actual selected-monitor capture on the user's desktop and physical unplug/reconnect. Synthetic OCR and injected topology tests are not claimed as end-to-end desktop verification.

## User test

Restart the updated host and attached windows, log in, then run:

```text
grid -start
connect mcp -inline
screen options
screen monitoring 1
screen status
screen ask Explain the text visible on this screen.
screen stop
screen ask Explain the text visible on this screen.
```

Place a non-sensitive text example on the selected monitor before the first request. The final request should require a new selection. For a meeting, disconnect, run `agent meetings MCP MAXIS -inline`, repeat monitor listing/selection, then run `screen ask @MAXIS Explain the code error shown here.` Select independently inside each attached window.

Platform references: Microsoft's [Windows OCR sample](https://github.com/Microsoft/Windows-universal-samples/blob/main/Samples/OCR/cs/OcrFileImage.xaml.cs) and [OcrEngine documentation](https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr.ocrengine.recognizeasync). No sample code is downloaded or executed at runtime.
