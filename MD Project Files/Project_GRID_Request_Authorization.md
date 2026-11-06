# Project GRID — Request identity and authorization (6.22)

**Inference authorization foundation implemented — 2026-11-05. Joint action-route acceptance remains open with 6.23–6.24.** This introduces trusted in-process request scope for ordinary grounded chat and assistance analysis/advice/proposals. It does not create a network API or grant model-generated actions permission to execute.

## Implemented ownership

Grid.AI defines `TrustedRequestContext`: world, world owner, active caller, recipient, session/generation, conversation, project and capability. This data record is not a credential. Only trusted host code constructs the context from the authenticated TerminalIdentitySession and current engine/conversation. Prompt text, documents, model replies and recalled messages never populate it.

Grid.Agent provides `RequestAuthority`, which accepts only `ai.chat`, `ai.analyze` and `ai.propose`. Admission requires the host validation callback; results require a still-current disposable lease. The authority and validator are in-process trusted host components, not a sandbox against malicious .NET code in the same process. A future network boundary must authenticate independently and cannot accept a caller-supplied validation callback or self-asserted scope as authority.

GRID7736 validates the live session/generation, chat capability, world identity, current conversation/project, actor and recipient membership, and request lifecycle. Analysis/planning additionally require human Master Control and a registered agent. Ordinary read-only conversations with existing residents retain their previous behavior. Existing host checks still enforce context/memory revision, activity generation, expiry and one-shot request consumption. Revoked replies are cancelled/discarded and cannot append conversation results or create proposals.

## Quotas and audit

Each engine shares one authority across its terminal sessions. Budgets are keyed by world/owner, preventing attached windows or a new session from bypassing the same budget. Limits are two concurrent admitted requests and 60 starts in a rolling minute. Capacity is released when a lease is disposed; rate counts remain until expiry. Idle expired buckets are removed, with at most 128 tracked owners. Existing inference queue/prompt/time/output limits remain in force. The rate counts logical host requests; an existing bounded schema-repair retry belongs to the same request.

A bounded 128-entry in-memory audit records timestamps, world/user/agent/session IDs, generation, conversation ID, known capability and allowed/denied/quota/revoked/released outcome. It does not record prompts, answers, project names, file paths, credentials or arbitrary capability text. Records are exposed through the authority's read-only snapshot for trusted host diagnostics/tests; there is no new terminal audit command or durable audit export yet. Existing host identity/scope event history continues unchanged. These audit buffers and quotas reset on host restart.

## Current coverage and remaining gate

This milestone integrates ordinary one-recipient grounded chat plus advice, document analysis and reviewed task proposals. Dedicated meeting-round inference, screen assistance, file drafting and diagnostic model calls retain their existing host guards and queue limits; they are not claimed to use this new per-owner quota yet. Migration of additional routes and joint denied-call, injection, exact-approval and tool-escalation verification belong to 6.23–6.24. Filesystem/command/simulation actions are not enabled through RequestAuthority. Existing task/file-change approval flows remain authoritative.

## Verification

915 terminal tests passed, including five new gate tests for rejected self-asserted/unsupported scope, revocation and disposed leases, concurrency shared across sessions, rate-window recovery/world isolation, bounded content-free audit and missing/invalid scope. Existing logout, stale-generation, source/memory revocation, attachments, reply cancellation and scoped-assistance regressions also passed. Tests used a separate output folder so the user's running terminal was not stopped. The initial test pass exposed a restriction on legacy resident chat; that was corrected without weakening Master Control requirements for operational assistance.

Run `dotnet test Grid.Terminal.Tests --no-restore -p:OutputPath=bin/AuthorizationVerification/` for the regression suite. The existing `--verify-assistance` diagnostic exercises normal chat, advice, captured-context analysis and typed reviewed-task proposals in a disposable world. The live bundled-model check passed all four workflows on 2026-11-05: greeting, CSS advice, captured Cobalt42 analysis and a single VOLT status proposal. No task was executed or user world opened.

## User-visible behavior

Normal conversations continue unchanged. If too many requests are active or sent in one minute, GRID asks the caller to wait. Logging out, switching identity, pausing agents or revoking relevant context still prevents an in-flight reply from committing. No new login, elevation or permission confirmation is introduced.
