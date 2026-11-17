# Project GRID — Request identity and authorization (6.22)

**6.24 completed — 2026-11-12.** Shared inference now withholds recognizable credential material and invalid control-character output. Strict proposal parsing rejects worker approval/scope expansion and uses content-free rejection messages. Format-repair retries recheck live authorization. All 1,225 solution tests passed, including twelve new cases and the joint 6.22–6.23 authorization/tool suites. Credential detection is pattern-based with documented false-positive and coverage limits. [Implementation and evidence](Project_GRID_Layered_Guardrails.md).

**Inference-route coverage completed — 2026-11-05.** The shared host authorization gate covers ordinary chat, meeting rounds, advice, document analysis, reviewed proposals, screen assistance, AI file drafts and AI status/test requests. Joint action-route acceptance remains scheduled with 6.23–6.24.

## Ownership and trusted scope

Grid.AI defines TrustedRequestContext with world, world owner, active caller, recipient, session/generation, conversation, project and capability. Trusted host code derives these fields from the live terminal identity and engine state. Authentication comes from the host validation callback. The record carries scope information through the request lifecycle.

Grid.Agent RequestAuthority admits the capabilities `ai.chat`, `ai.analyze`, `ai.propose`, `ai.screen`, `ai.file-draft` and `ai.diagnostic`. Each lease requires current host authorization before work and before accepting its result. RequestAuthority and its validation callback belong to the trusted in-process host. Any future network API will require an independently authenticated boundary.

GRID.AI checks the live identity, session generation, world and attached engine, conversation/project membership and request lifecycle. Screen assistance, analysis, planning, file drafting and diagnostics require Master Control. Conversation-bound specialist work checks agent registration. Ordinary resident chat uses the existing conversation membership rules. Existing lifecycle guards cover relevant context/memory revisions, agent activity, expiry and single-use operations.

Standalone diagnostic and file-draft requests have an empty conversation ID. Diagnostics use the diagnostics scope and an empty agent ID. File drafting records the MAXIS resident ID when available and the selected workspace alias when it is a valid project label. Direct directory destinations use the file-draft scope. The existing file-destination service validates the selected target and exact file state when preparing/staging the draft.

## Quotas and audit

One RequestAuthority instance is shared by all routes and terminal sessions attached to an engine. Budgets are keyed by world and owner. Limits are two concurrent requests and 60 starts per rolling minute. Meeting rounds count each participant inference separately. Screen requests acquire capacity before capture. Each status/test diagnostic is single-use. Draft and proposal requests retain their existing bounded retry behavior within one logical request.

Disposing a lease releases concurrent capacity. Rate counts expire after one minute. Idle expired buckets are removed and at most 128 owner budgets are tracked. Existing queue, prompt, time and output limits also apply. Restarting the host resets this in-memory accounting.

The 128-entry audit stores timestamps, world/user/agent/session IDs, generation, conversation ID, recognized capability and admission/revocation/release outcome. Scope history also remains available through the existing host identity events. Master Control can run `ai authorization` in the original terminal to see the latest 20 admission records and current configured limits. Project names, message bodies, file paths and credentials are excluded from this audit. Durable audit export remains future work.

## Verification

Current suites total **1,191 passing tests**, comprising Core 14, Simulation 157, Persistence 99 and Terminal 921. Six added route tests exercise shared quota exhaustion through ordinary chat followed by meeting, screen, file-draft or diagnostic requests, diagnostic replay rejection and logout during active diagnostic inference. They verify refusal before model invocation, screen capture or file creation. Existing scope-forgery, stale-session, revocation, meeting, attachment, file-edit, context and approval tests also pass.

The full suite first ran with UseAppHost disabled to avoid a previously observed executable lock. That setting omitted the document worker executable and caused 20 PDF/DOCX tests to fail. A normal build restored the required worker and all suites passed. The final terminal rerun includes the additional logout and engine-binding checks.

## Implementation history

The initial foundation passed 915 terminal tests and live bundled-model checks for greeting, CSS advice, captured Cobalt42 analysis and a VOLT status proposal. Its five new gate tests covered admission denial, revocation, disposed leases, concurrency across sessions, rate expiry, world isolation and bounded audit. This follow-up connects the remaining routes to that gate and exposes its audit through the terminal.

## Remaining joint gate

6.22 inference integration is complete. The checklist retains joint denied-call, forgery, stale-session and scope-revocation verification with 6.23–6.24 as a prerequisite for migrated action routes. Existing approved task and file-change services continue to enforce their execution checks. Those later milestones cover typed tools, exact approval binding, adversarial input and tool escalation.

## Manual check

Restart the rebuilt terminal and run the following commands.

```text
ai test
ai authorization
grid -start
connect MCP -inline
hello MCP
disconnect
ai authorization
```

The audit should show diagnostic and chat admission/release records. When quota is exhausted, GRID asks the caller to wait. Logging out during a running request cancels or discards its output.
