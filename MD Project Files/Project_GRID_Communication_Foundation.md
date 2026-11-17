# Project Grid.AI - Communications Foundation (4.1)

## Local login and identity completion - 4.2A (2026-10-20)

**Implemented and verified.** Start with `login master`: the first successful Windows-token login binds the world owner; later logins must match. Sessions expire after eight hours. Master Control can register existing Agent identities, switch roles, and preview/confirm one-time program edits. Prompts/help/commands are role-aware; agent chats are participant-scoped and global/admin controls are denied. Switching/logout clears active chats and previews. Schema v11 preserves owner/agent registration without restoring authentication.

**362 tests passed**, plus isolated Windows login/agent restriction/confirmed edit/restart smoke. See [Local Login and Identity Implementation](Project_GRID_Local_Login_and_Identity_Implementation.md) for commands, service authorization, audit and security limits. This authenticates the current Windows account, grants no OS elevation, and provides no independent agent passwords or real task service. NGE now requires Master Control; --start waits for login. Earlier dated pending-login/editing statements refer to prior milestones.

## Terminal conversations completion - 4.2 (2026-10-20)

**Implemented and verified.** Use connect (default MCP), talk, conversations, transcript, resume and disconnect. MCP is created on first explicit connection as a persistent coordinator Agent; a stable world-local operator handle supports reopening saved conversations. Replies to status/who are you are labeled verified Grid.AI state; other replies are labeled deterministic simulated dialogue and claim no task execution/delegation. Input/reply pairs commit atomically and reuse schema v10.

**353 tests passed**, plus a three-launch isolated terminal save/resume/end smoke. See [Terminal Conversations](Project_GRID_Terminal_Conversations.md) for syntax, identity/capacity limits and completion evidence. This local operator handle grants no authentication or admin authority; 4.2A login, real task delegation and LLM dialogue remain planned. Earlier dated pending-terminal-chat/agent-creation statements describe previous milestones.

**Implemented and verified - 2026-10-20**

## Architecture and implemented scope

Grid.Communication is a new dependency-free .NET contract/model project. It defines IGridCommunicationHub, Recipient, Conversation, GridMessage and CommunicationState. GridSimulationEngine implements the hub as the local authoritative host under its existing world lock. This allows conversation and simulation state to be captured together. Grid.Persistence stores the shared snapshot; the terminal and future voice/viewer adapters can use the same service without owning a second conversation network.

The implemented contract is synchronous and local: ResolveProgram, StartConversation, SendMessage, EndConversation and GetConversation. The asynchronous examples in the original design remain future adapter sketches. There is no external delivery, generated dialogue, OS process messaging or automatic execution of message contents. Sending text that looks like a terminal command only stores text.

## Identity and routing

Recipient identity is (Human or Program, UUID), with a display name. ResolveProgram accepts a current full program ID or a case-insensitive exact name, including spaces. Unknown or ambiguous names fail; callers use the full ID to disambiguate. Program identities are canonicalized against the live world and their current names. Agents, special programs, legacy citizens and observed application residents can be addressed; a display name or occupation does not grant authority or create a named agent.

Human UUID/name are supplied by a trusted host adapter. This is a service boundary, **not authentication**: participant membership checks prevent accidental cross-conversation sends but do not prove the caller owns the supplied identity. Production login, access control, per-user transcript visibility and authenticated sender contexts remain future work. No arbitrary external user should receive direct access to this trusted API.

Each conversation has exactly two distinct participants. Message sender must be one of them; the other participant is the recipient. Both program participants must still resolve to their original resident IDs when sending. There is no fallback to a different same-name resident. Application resident identity is not an OS delivery endpoint; discovery can change the process lifetimes associated with a still-present resident. If a resident disappears or is not rediscovered after restart, its historical transcript stays readable, but new messages fail until the original resident ID is available. A remaining human participant can close such a conversation.

## Lifecycle, ordering and limits

StartConversation returns a host-generated conversation ID. SendMessage appends a host-generated message ID, contiguous one-based sequence, sender identity/name, content, origin, simulation tick and UTC timestamp. Origins are Terminal, Microphone, WorldViewer and SystemEvent metadata; labeling an origin does not enable that interface. Conversation identity is represented by the containing conversation rather than duplicated in each message record.

EndConversation records the end tick and prevents further messages. Ending an already ended conversation is idempotent. Sending is not retry-idempotent: repeated successful calls create distinct messages; durable outbox/retry/delivery receipts are not implemented. Reads return owned read-only collections. Restore detaches caller-supplied lists, validates sequence/tick/identity constraints and retains open/ended status without replaying messages or actions.

Bounds are 128 conversations per world, 2,048 messages total and 4,096 UTF-16 characters per message. Blank bodies, invalid enum values, blank/overlong identities and control characters other than newline/carriage-return/tab in content are rejected. Reaching a limit fails before mutation; existing transcripts are never silently truncated. Conversation deletion, retention controls, pagination and a larger indexed archive remain future extensions; ended conversations still count toward limits.

Cancellation is checked before commit. Concurrent sends are serialized and receive unique sequence numbers. Communications do not advance the clock or modify resident work, energy, location or revision values. Start/send/end emit ConversationStarted, MessageStored and ConversationEnded history metadata; message bodies are kept in transcripts and are not copied into the event log.

## SQLite and recovery

Schema v10 adds WorldCommunications: one world-linked JSON snapshot row with a 60,000,000-character SQL bound (including escaped Unicode overhead), in addition to the tighter model count/content limits. This initial bounded snapshot design is intentionally not a full-text-search or paginated message store. Existing migrations retain their checksums. Older worlds without a row load empty and acquire the row on save.

WorldStore writes conversations inside the same transaction as world state. Autosave/orderly shutdown capture them automatically through GetSnapshot. Save acknowledgement is separate from SendMessage success: an abrupt exit can lose messages since the last save. Existing world lease, backup, recovery and NGE file boundaries apply. Tests verify a failed transcript insert rolls back the entire save and retains the prior transcript/backup.

Participant IDs are historical references, not foreign keys to live Programs. Ordinary application residents continue to be excluded from durable population state, while their conversation transcripts survive. Restoring an open conversation with persistent participants allows later messages to continue the sequence. Restoring does not synthesize a reply or replay side effects.

## Verification and next module

**346 tests passed:** 14 Core, 152 Simulation, 88 Persistence, 92 Terminal. All four projects ran with `dotnet test <project>/<project>.csproj -c Verification --no-restore`; initial project restore/build succeeded with zero warnings/errors. New tests cover name/ID routing, ambiguity, invalid/missing participants, lifecycle, cancellation, malformed messages, capacities, concurrent sequencing, snapshot isolation, continuation after restore, missing-application transcripts, populated v9 migration, save/reopen, atomic SQL failure and backup recovery. Existing social migration fixtures were updated to remove the added table when constructing an older-schema fixture.

No default user save or running terminal was modified. **4.2 remains next:** terminal connect/talk/continue/history/end commands and interface session identity. Generated replies, agent delegation, broadcast/group conversations, voice, authenticated login, delivery status, correlation/causation, action proposals and retention UI remain planned. The foundation makes local conversations representable and durable; it does not make programs intelligent or able to perform host tasks.
