# Project GRID - Phase 4.5 Communications Verification

## Completed - 2026-10-21

The seven 4.5 checklist items are covered by automated verification across the communication model, simulation hub, identity-aware terminal, local speech adapter and SQLite persistence. Three new integration tests join the existing routing, chat, context, voice and attached-window suites. No production feature, schema migration, world regeneration or user-data modification was needed for 4.5.

## Verification matrix

| Checklist item | Evidence and expected behavior |
|---|---|
| Message recipient routing | `CommunicationTests` validates exact ID/case-insensitive name routing, ambiguous/absent recipients, participant checks and ordered concurrent messages. `ChatCommandTests` verifies explicit/default recipients. New `UnrelatedAgentCannotReadOrResumeOwnersOtherConversation` proves unrelated conversations stay hidden, intended participants can read shared transcripts, and parent logout revokes attached access. |
| Persistent conversation history | `CommunicationPersistenceTests` covers save/reopen, ended conversations, missing application residents, migration, failed-save atomicity and backup recovery. `ChatCommandTests` verifies explicit resume with the same operator/coordinator. New combined integration verifies continued message order/count after a real SQLite reopen. |
| Invalid recipients and malformed messages | Existing service tests reject unknown/ambiguous recipients, unrelated senders, invalid origins, controls, empty/oversized messages and capacity overflow. New `MalformedTerminalMessagesAndRecipientsNeverAlterConversationHistory` covers malformed quoting/IDs, empty/control/oversized text, and successful Unicode input after rejection, with no partial transcript writes. |
| Cross-session context recovery | `ContextPersistenceTests` covers persistent versus temporary captures, grants/revocation, restart, migration and recovery. New `SavedConversationAndContextReopenWithAuthenticationAndRevocableGrants` deletes the source after saving, confirms persistent captured content survives while temporary content does not, requires fresh login and explicit conversation resume, verifies agent grant then immediate revocation, and saves the revocation with the continued transcript and voice profile. |
| STT-to-message equivalence | `VoiceTests.VoiceRequiresReviewAndStoresMicrophoneOriginThroughSameDialogue` compares identical typed/reviewed voice messages through the same deterministic hub, preserving distinct origin metadata. Real-provider fixtures separately verify generated speech and silence through the actual local process connection, plus invalid-audio diagnostics. Recognized terminal-command text is only conversation data. This verifies routing equivalence, not word-for-word microphone recognition accuracy. |
| Audio unavailable fallback | `VoiceTests` injects recognizer failure and cancellation and verifies typed chat continues. Noninteractive input cannot capture audio. Unsupported/missing provider errors are surfaced. Mute, stale session/conversation, bounded output, malformed recognition and Escape behavior are covered. Physical device-unplug behavior remains a manual test. |
| Communication without AI | Deterministic `connect/talk/resume/transcript` tests run without an LLM provider, model, endpoint or API key. Replies retain verified-state/simulated-dialogue labels and claim no host actions. Speech uses installed offline Windows engines; generated test speech never plays over speakers. This does not claim an airplane-mode/network-disconnection experiment. |

## Results

**455 tests passed:** 14 Core, 155 Simulation, 97 Persistence, 189 Terminal. No failures or skipped tests were reported on this Windows host. Commands ran sequentially with `dotnet test <project>/<project>.csproj -c Verification --no-restore`; test databases and context files were disposable fixtures. Existing saved worlds and running GRID instances were not used by these tests.

The three new tests live in `Grid.Terminal.Tests/CommunicationVerificationTests.cs`. Existing coverage lives in `Grid.Simulation.Tests/CommunicationTests.cs`, `Grid.Persistence.Tests/CommunicationPersistenceTests.cs`, `Grid.Persistence.Tests/ContextPersistenceTests.cs`, and the terminal chat/context/identity/window/voice suites. Real-provider tests are platform/installed-language conditional; their Windows speech branches were available on this host. Passing on a machine without those prerequisites does not establish real-provider operation there.

## Scope and remaining manual checks

Phase 4.5 is complete for the implemented Windows offline MVP. Physical microphone capture, hold/release ergonomics, headset disconnect/reconnect, audible playback quality and recognition accuracy still require an interactive hardware check. The repaired completion-event transcription path is regression-tested with synthesized speech held in memory. No recording of the user or speaker playback occurred in this verification run.

Voice remains original-host-only; attached windows retain typed chat and scoped context. Whisper.cpp/Piper, advanced voice options, real agent task execution/delegation, and generative AI are future features, not implied by passing 4.5. Earlier implementation notes and test counts remain historical evidence; the next planned checklist module is 5.1.
