# GRID layered guardrails — 6.24

## Completed host-route verification — 2026-11-12

The current local inference and execution routes combine bounded requests, host identity checks, scoped retrieval, strict proposal parsing, human approval and recorded execution results. The 6.24 additions close credential-content and error-message gaps and recheck authorization during the proposal-format retry.

## Content validation and sensitive data

`Grid.AI.InferenceContentGuard` checks the shared inference queue before backend dispatch and before accepting generated output. Recognizable credential material causes the whole request or response to be withheld with a content-free explanation. This applies to chat, structured proposals and file drafting through `SharedLocalInference`. File content remains available for a fresh request after the operator removes the credential material from shared context.

The initial detector recognizes private-key headers, bearer-token strings of at least twelve token characters, and assignments to api_key/api-key/apikey, access_token/access-token/accesstoken, client_secret/client-secret/clientsecret and password with values of at least eight characters. Quoted JSON and escaped quote forms are included. It uses the .NET non-backtracking regex engine. Placeholder examples matching these patterns also trigger withholding. Short, obfuscated or differently named credentials can evade these patterns. General personal information and arbitrary secrets require additional detection rules if that coverage is desired.

Output validation also rejects empty or excessive responses and terminal control characters, while permitting ordinary line breaks and tabs. Existing output limits are 8,192 characters for ordinary replies and 131,072 characters for serialized file drafts. Diagnostics record outcome metadata. Rejected content stays outside accepted replies.

## Proposal and execution boundaries

Delegation proposals require exactly one root `steps` field and exactly `agent`, `capability` and `argument` string fields per step. Duplicate and unknown fields, unsupported capabilities, recursive MCP targets and invalid arguments are rejected. Worker-supplied approval flags, operator identity and expanded scope fields are rejected. The host creates every submitted task awaiting human approval.

Proposal errors use fixed descriptions instead of interpolating the rejected worker payload. The bounded format-repair retry rechecks both live conversation state and the original authorization lease before accepting its answer.

Captured documents and worker replies remain data throughout this flow. Retrieval filters enforce agent/project grants before constructing the prompt. Meetings retain private-data scope checks. Tool execution uses the existing host broker, checked file destinations, exact reviewed changes, single-use approval and recovery evidence. Ordinary generated text is displayed as model output. Actual task and file outcomes come from host receipts and checked artifacts.

Runtime isolation uses the shared bounded queue, cancellation/deadline checks, loopback-only model connections and existing process adapters. The shared inference boundary covers the current production agent routes. Direct provider APIs remain low-level components whose callers must supply authorization and content checks.

## Verification

The full solution passed 1,225 tests on 2026-11-12, comprising Core 14, Simulation 157, Persistence 99 and Terminal 955. Twelve new cases cover credential withholding before dispatch and after generation, continued queue operation, terminal control sequences, hostile retrieved instructions, duplicate worker identity, worker approval/scope fields and content-free proposal errors.

Existing suites run jointly with these cases and cover unauthorized retrieval, revoked grants, stale sessions, forged identities, quotas, malformed outputs, command-shaped model answers, exact approval binding, concurrent destination changes, failed-save retries and recovered receipts. The hostile-document tests assert that the injected text actually reached the captured prompt before checking refusal and absence of tasks.

```text
dotnet test Grid7736.slnx --no-restore --verbosity minimal
```

Tests use synthetic credentials and controlled model responses for reproducible boundary checks. They establish host behavior for the tested routes. Model factual accuracy and specialist-model acceptance continue under their evaluation milestones. Conversation-only chat and meeting acceptance remains scheduled under 6.31.
