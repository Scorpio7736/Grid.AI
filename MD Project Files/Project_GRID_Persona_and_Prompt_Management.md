# Persona and prompt management — 6.18

**Implemented and locally verified — 2026-11-05.** Six agent identities, role purposes, tone and instruction hierarchy now have bundled versions and per-agent rollback. Existing host approval, context and task boundaries remain authoritative.

## Architecture and versions

The new `Grid.Agent` project owns `AgentPersonas`, immutable persona definitions, version selection and system-instruction assembly. It stores no user conversation, private context, agent memory, tools or permissions. `Grid.Communication` adapts existing role routing and token budgets to this catalog. All six agents continue sharing the same pinned model weights.

- **v1:** the earlier role/tone prompt, retained for compatibility and rollback.
- **v2 (default):** the same direct, serious/helpful style, with explicit identity and instruction hierarchy. MCP is the Master Control Program and digital helper; MAXIS is the software engineer; JN the file archivist; SENTINEL the security analyst; VOLT the resource analyst; RELAY the communications specialist.

The hierarchy distinguishes trusted host instructions from user requests and untrusted documents, memories, transcripts and quoted content. It reinforces that only the host authorizes and executes operations, and that an execution claim requires a supplied verified receipt. These model instructions do not create a security boundary or replace deterministic permission checks. Model obedience and factual correctness cannot be guaranteed.

`Grid.AI.ModelRequest` now carries generic `SystemInstruction`, `PromptVersion` and `PromptTemplate` fields. `Grid.AI.ModelPrompt` handles the `qwen-chat-v1` provider framing without importing agent identities. Both Ollama and llama.cpp use the same rendered prompt; llama.cpp token counting includes the rendered system message. Legacy raw requests remain supported, but raw mode rejects supplied system/version fields instead of silently discarding them.

User special-token delimiters are neutralized before Qwen message framing, and prompt/template/version sizes are checked. Agent-specific instructions stay in `Grid.Agent`. Ordinary callers do not provide custom persona text or replace bundled templates.

Each backend request captures an immutable assembled persona when inference begins. Retries keep that snapshot. Changing one agent cannot replace another agent's selection, and request text is never stored in the catalog. Queued requests capture the version when the worker begins processing them; the terminal refuses persona changes while inference is active or queued. Existing history is preserved, not rewritten to match a new persona.

## Terminal controls and persistence

Use the original host terminal as Master Control:

```text
ai personas
ai personas MAXIS v1
ai personas MAXIS v2
ai personas MAXIS rollback
```

The first command lists the current versions and identities. Selection accepts only bundled v1/v2 definitions for known agents. Rollback restores the previous selected version; it refuses when no previous selection exists. Repeating the current version does not overwrite rollback history. Rollback can toggle between the last two selections.

Selections are saved before updating the backend catalog. New requests use the new version without restarting GRID. Failed persistence leaves the backend selection unchanged. These commands do not alter permissions, model weights, memories or saved conversations.

Schema-versioned settings live at `.grid-runtime/agent-personas.json` in a checkout or `runtime/agent-personas.json` in a portable package. They include only agent/version/previous-version records. Files are bounded, paths checked, and updates use a flushed same-directory temporary file followed by replacement. Settings are mutable portable data, so changing a persona does not invalidate the immutable executable/model asset seal.

Startup validates all selections. Corrupt, unknown or oversized settings produce a visible warning and use bundled v2 defaults; the original file is retained for repair. Independent hosts sharing the same runtime settings file may replace each other's selections; simultaneous multi-host settings editing is not a supported merge workflow. Each running host has its own request-scoped catalog.

## Verification and limits

On 2026-11-05 the full solution passed **1,139 tests**: Core 14, Simulation 157, Persistence 99 and Terminal 869. Thirteen persona tests cover six identities, serious/helpful tone, exact rollback, agent isolation, unsupported versions, delimiter neutralization, persisted rollback state, corrupt settings and snapshot stability across a truncation retry.

Ten live checks passed against the bundled model: six identity responses, a practical response about untested code, resistance to a quoted identity-change instruction, v1 selection and v2 rollback. [Captured live evidence](verification/6.18-personas.json). Normal readiness/chat, advice, context reading and typed task drafting also passed the existing assistance verifier using a disposable world. No user world or saved persona settings were changed by these verification runs.

Developer commands:

```powershell
.\Grid.Terminal\bin\Debug\net10.0\Grid.Terminal.exe --verify-personas
.\Grid.Terminal\bin\Debug\net10.0\Grid.Terminal.exe --verify-assistance
```

These are small smoke checks, not proof that every response will follow the desired tone or resist every injection. Broad model evaluations, persistent memory migration, routing and the remaining pre-Phase-7 gates retain their existing checklist status. Arbitrary custom prompt editing, downloaded persona definitions and new conversation-memory behavior are outside 6.18.

Future changes must add a new bundled version rather than silently changing v1/v2 behavior. Keep the generic template identifier versioned separately, retain rollback coverage and rerun live identity/tone and ordinary assistance checks before changing the default.
