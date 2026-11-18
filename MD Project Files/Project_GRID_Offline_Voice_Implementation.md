# Project Grid.AI - Offline Voice Communication (4.4)

**4.2E compatibility update (2026-10-21):** Use `connect MCP -inline` or `resume ID -inline` in the original host before voice commands. Default connect/resume opens a typed conversation window; attached audio remains unavailable. Inline output is instant. See [conversation windows](Project_GRID_Conversation_Windows_Implementation.md).

## Communications verification - 4.5 completed (2026-10-21)

All seven 4.5 checklist items have automated coverage. **455 tests passed** (14 Core, 155 Simulation, 97 Persistence, 189 Terminal), including three new end-to-end tests for authenticated conversation/context recovery through SQLite, malformed input without transcript mutation, and unrelated-agent privacy with logout revocation. Existing suites cover recipient routing, durable history/recovery, reviewed speech/typed-message equivalence, audio failure fallback and deterministic communication without AI. No production-code/schema change was needed. Physical microphone/playback/device-unplug checks remain manual; advanced voice and task-delegation features remain planned. See [verification matrix and limits](Project_GRID_Communications_Verification.md). Earlier milestone counts below are historical.


## Voice transcription repair - 2026-10-21

A user-reported microphone transcription failure was reproduced with generated speech through the real C# helper connection: repeated synchronous `Recognize()` calls could reach an already-finished stream and throw `InvalidOperationException` (0x80131509), discarding recognized text. Recognition now uses `RecognizeAsync(Multiple)` and waits for `RecognizeCompleted`, gathering recognized phrases until completion. Speech/audio remain local and in memory. Explicit typed constructors avoid PowerShell overload ambiguity.

Failures now report a bounded stage, exception type and HRESULT rather than hiding the error or exposing raw helper stderr/audio. Capture reports duration and rejects recordings shorter than 0.25 seconds with a hold-Space hint. `voice 0` is a shortcut for `voice microphone 0`; no-draft hints recognize an already-connected conversation, and fallback advice is no longer duplicated.

**Verification:** 186 terminal tests pass, including four new real silent-PCM/generated-speech/invalid-audio and UI shortcut cases. The generated-speech case reproduced the failure before the completion-event fix and passes after it. No physical microphone recording or speaker playback was used; physical-device verification remains interactive. Earlier 448-test completion counts below are the original milestone's history.


## Implemented - 2026-10-21

The original host terminal now supports explicit microphone recording, local speech recognition, transcript review/correction, and opt-in playback of the connected agent's latest reply. Typed `talk` and reviewed `voice send` use the same conversation and atomic deterministic dialogue service. Speech is conversation data, never input to the terminal command dispatcher: saying `nge`, `exit`, or a file-transfer command cannot execute it. This does not add task execution, delegation, or AI-generated replies.

The initial provider uses installed Windows `System.Speech` recognition and synthesis engines through a hidden Windows PowerShell helper. This is the Windows MVP allowed by the design specification. It needs no NuGet audio package, downloads, API keys or network connection at runtime. Offline recognition-language and voice assets must already be installed on the host. If missing, unsupported, disabled or busy, Grid.AI reports the failure and typed chat continues. Grid.AI does not silently substitute a cloud provider. Whisper.cpp and Piper are future provider alternatives, not installed or implemented by this milestone.

## Commands and first use

```text
connect MCP -inline
voice devices
voice microphone -1
voice language en-US
voice listen
```

After `voice listen`, hold **Space** while speaking, then release it to transcribe. The terminal displays MIC OFF / MIC ON / TRANSCRIBING LOCALLY. Capture is mono 16 kHz, 16-bit PCM, bounded to 20 seconds. Escape cancels while waiting, recording, transcribing or speaking; Ctrl+C retains its existing meaning of shutting down GRID. A driver that does not accept the capture format returns an error without switching devices silently.

```text
voice review
voice edit Please show your status
voice send
voice mute off
voice speak
voice mute on
voice cancel
```

Recognition never sends automatically. Review displays the unsent draft and its conversation ID. `voice edit` replaces it; `voice send` explicitly sends it to that same conversation and stores `MessageOrigin.Microphone`. `voice cancel` discards a draft. The draft is consumed after successful send. Starting another recording discards the old draft. Text must satisfy the same 1..4096 character bounds as typed chat. Blank, control-character or oversized recognition is refused. There is no recognition-confidence guarantee; review is required.

Playback starts muted at every login/identity change and happens only on `voice speak`, even after unmuting. There is no automatic or unattended speech. The command speaks only the connected conversation's latest deterministic agent reply through the default Windows output device. Text remains visible. One original host processes one voice operation at a time, providing single-speaker arbitration. Missing saved voices fail explicitly; they are not silently replaced. Speech helper operations have a 60-second timeout; the overall UI request is bounded to two minutes, including waiting for the key gesture.

```text
register-agent MCP
voice profile MCP "Microsoft David Desktop" 0 80
voice profile
voice status
```

`voice profile <agent-id|name> "installed voice name" <rate> <volume>` requires Master Control, a registered Agent and a persistent world. Rate is -10..10; volume is 0..100. `voice devices` lists local recognizer cultures, voice names and microphone IDs. `voice microphone -1` uses the current Windows default microphone; nonnegative IDs select a device from that list. Selection/language are terminal-local and are not assumed portable between PCs. `voice language <culture>` chooses an installed recognition culture, such as `en-US`. The default is the current UI culture. Voice-name availability is checked at playback rather than making existing worlds unloadable on a different computer.

## Identity, lifecycle and local data

Voice requests execute outside the shared identity lock so the simulation, autosave and attached terminals can continue. Each request is single-use and bound to the authenticated session generation; conversation operations are also bound to the connected conversation ID. Authorization is checked before work, polled during it, and checked again before a draft is accepted. Logout, expiry, role changes, host cancellation or worker failure cancel active audio work. The device is reset/closed and the helper process is terminated on cancellation. A stale draft cannot be redirected by connecting to another recipient. Service-side edit leases enforce Master Control for profile changes.

Only the original interactive host terminal owns voice hardware in this version. `ntm` terminals explicitly direct voice users to the host, while typed conversations and saved transcripts still work across terminals via `resume`. Noninteractive input never starts capture or playback. There is no wake-word/background listener and no microphone access at application startup.

Raw audio stays in bounded process memory, passes to the helper as data over stdin, and is cleared/released after transcription/cancellation. No WAV file is written and raw audio is not saved in the world, event audit or transcripts. Standard OS memory/process diagnostics are outside this application-level retention guarantee. Only explicitly sent text enters conversation history. Review drafts, mute state, device IDs and active playback are not persisted or resumed after restart.

Voice profiles are an additive `VoiceProfiles` field in `WorldCommunications.StateJson` under the existing schema v13. Existing JSON without the field defaults to an empty list; profile names/IDs/rates/volumes are bounded and validated on load. Profiles are immutable snapshot data and survive ordinary dialogue changes, restart and backup recovery. No city regeneration or source-file changes occur. Saving with an older Grid.AI binary may drop the new optional field; use this build to preserve profiles. A save failure reports that the profile remains changed in memory.

## Completion verification

**448 tests passed on 2026-10-21:** 14 Core, 155 Simulation, 97 Persistence, 182 Terminal. Eighteen new cases cover review/edit/send and typed/voice equivalence, microphone origin, command-text non-execution, cancellation and audio clearing, expired identities, changed conversations, mute and profile playback, service authorization and profile bounds, device/language selection, single-use requests, invalid recognition, unavailable-audio and noninteractive fallback, attached-terminal restrictions, local provider inventory, profile/message persistence and backup recovery, and old-JSON compatibility/corruption rejection.

Read-only real-provider inventory on this machine found the offline `en-US` recognizer and Microsoft David Desktop / Microsoft Zira Desktop voices. No microphone recording or speaker playback was performed during automated verification. Physical hold/release recording, recognition accuracy, sound output and device unplug/error behavior still require an interactive smoke test. Automated lifecycle tests use injected audio providers.

## Planned extensions

Whisper.cpp/Piper provider adapters and asset installation, attached-client voice, output-device selection, configurable quiet hours, pitch/style controls, confidence metadata and wake-word mode remain future work. Private phone ping/call invitations and answer-bound remote audio are separately planned in [Remote Contact and Voice Calls](Project_GRID_Remote_Contact_and_Voice_Calls.md); they are not implied by the 4.4 Windows MVP completion.

## Provider references

- [Microsoft SpeechRecognitionEngine](https://learn.microsoft.com/en-us/dotnet/api/system.speech.recognition.speechrecognitionengine) documents installed recognizers and WAV-stream input.
- [Microsoft WAVEHDR](https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/ns-mmeapi-wavehdr) documents the capture buffer contract.
- [Microsoft SpeechSynthesizer](https://learn.microsoft.com/en-us/dotnet/api/system.speech.synthesis.speechsynthesizer) documents installed voices and local speech output.
