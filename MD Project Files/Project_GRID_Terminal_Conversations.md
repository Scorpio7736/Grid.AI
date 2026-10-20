# Project GRID - Terminal Conversations (4.2)

**Implemented and verified - 2026-10-20**

## Commands

```text
connect                         Start a new conversation with ARCHON.
connect <program-id|name>        Start a new directed conversation.
talk <message>                  Send a turn to the connected program.
conversations                   List this local operator's conversations.
transcript [conversation-id]     Read the active or selected transcript.
resume <conversation-id>        Reconnect to an open saved conversation.
disconnect                      End the active conversation; retain its transcript.
```

Example:

```text
connect
talk status
talk who are you
talk help
transcript
disconnect
```

Names with spaces are accepted by connect, as are full resident IDs. Use IDs for ambiguous names. `talk` consumes the rest of the parsed command; quote text containing apostrophes or literal quote characters according to the existing command parser (for example `talk "What's next?"`). Parsed tokens are joined with spaces, so this is not a byte-exact raw-input editor. Stored Unicode text is retained; transcript display replaces control characters with spaces and preserves ordinary Unicode. Quoting/unknown arguments, missing recipients and invalid conversation IDs produce helpful errors.

There is one active conversation per terminal instance. Ordinary commands such as status, step, live and exit continue working while connected; text without `talk` is still interpreted as a command. Starting/resuming another conversation requires ending the active one first. `disconnect` permanently ends that conversation; it is not a temporary detach. Normal exit leaves the conversation open so another launch can use conversations/resume. After restart the active selection is empty; open/ended transcripts and sequence numbers remain saved. Ended conversations can be read but not resumed.

## ARCHON and operator identity

The first `connect` (or `connect ARCHON`) creates a persistent Agent resident named ARCHON in the motherboard/Core district, with a stable world-derived UUID and occupation Local coordinator. In worlds without hardware geometry it uses the first district by stable ID. Existing uniquely named ARCHON agents are reused; conflicting non-agent names or ambiguous names fail without promoting/overwriting anyone. A reserved-ID collision also fails. Merely launching the terminal does not create ARCHON.

ARCHON is the early general coordination contact. It supplies deterministic local guidance and state summaries, not task delegation, an LLM, operating-system privileges or an autonomous job assignment. It has no automatic work routine or social opt-in. Other recipients use the same explicitly labeled dialogue adapter; host applications do not actually read or author these replies.

A Local operator human UUID is derived from the saved world UUID and a versioned namespace. It remains stable across restarts and differs across worlds. This is a convenience handle, **not login, authentication or Master Admin authority**. Conversation list/read/resume commands select this handle's conversations; this is not a security boundary against direct trusted API access. 4.2A login, role-specific access and identity switching remain planned.

## Deterministic replies and atomic turns

- `status` returns **[VERIFIED GRID STATE at tick N]** with clock state, elapsed simulation time and district/resident counts from one consistent snapshot.
- `who are you` (optionally with a question mark) returns labeled resident identity/kind/occupation and explicitly identifies the deterministic GRID adapter.
- `help` supplies **[SIMULATED DIALOGUE]** describing supported questions and current limitations.
- Other text receives **[SIMULATED DIALOGUE]** acknowledging storage and explicitly stating that no task was executed or delegated.

Matching trims surrounding whitespace and ignores case. Replies never interpret arbitrary terminal commands or claim successful host tasks. The verified label refers to GRID's current snapshot, not external facts or freshness of every host measurement.

Engine Talk stages both the human message and generated reply, validates the human participant and live program recipient, checks two free message slots and cancellation, then commits both under the world lock. A full transcript/cancelled turn stores neither half. Reply origin is SystemEvent; input origin is Terminal. IDs and timestamps are generated metadata, while reply content for the same query/state is deterministic. DialogueTurnStored history contains metadata, not message bodies; CoordinatorCreated records the initial resident creation.

## Persistence and scope

Existing schema v10 holds the unchanged conversation model; no migration is required for these commands. ARCHON persists as an Agent, conversations save with the world, and ordinary applications retain discovery-only population semantics. Autosave and orderly exit provide durability; unsaved turns can be lost on abrupt termination. Missing original program recipients block new turns while allowing transcript reads and human closure. Capacity remains 128 conversations, 2,048 total messages and 4,096 input characters; a dialogue turn consumes two messages. Transcript retention/deletion tools remain future work, so closed conversations still count toward limits.

## Completion verification

**353 tests passed:** 14 Core, 155 Simulation, 88 Persistence and 96 Terminal. Each project ran with `dotnet test <project>/<project>.csproj -c Verification --no-restore`, without build warnings. New coverage verifies default/directed conversations, deterministic and verified labels, normal commands during chat, cancellation/capacity atomicity, coordinator collision, no task execution, invalid commands, Unicode rendering, restored operator identity, open conversation continuation and ended-conversation rejection.

A real Verification terminal smoke used a fresh temporary world over three launches: connect/create ARCHON, talk status/help, save/exit, restart/resume, talk who are you, read/end, then reopen/read and reject resume of the ended conversation. The user's default save and running terminal were untouched. Rebuild/restart the normal launcher target to use these commands.

Still planned: 4.2A production login, authenticated admin/agent sessions, real task delegation, LLM-generated dialogue, voice/viewer adapters, richer intent recognition, group conversations and transcript retention UI. Earlier completion notes describing terminal chat/agent creation as deferred refer to earlier milestones; this milestone adds only the explicitly requested lazy ARCHON coordinator.
