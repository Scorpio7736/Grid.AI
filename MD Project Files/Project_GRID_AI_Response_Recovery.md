# AI response rejection and recovery — 6.3 follow-up

Implemented 2026-10-26 after a user reported `assist advice how i can center a div in html` returning `InvalidDataException` while `ai test` and `ai status` worked.

## Cause and behavior

A healthy local model can still produce a response GRID refuses: truncated output, incomplete final text, an unfinished thinking block or invalid output. Previously, the inference worker collapsed these cases into a generic “Local AI unavailable” message. The original log did not preserve the specific rejection reason. A tiny readiness answer from `ai test` does not exercise a longer advice/code response.

The backend now retries an explicitly length-limited response once, increasing the answer budget from 512 to 1,536 tokens. The original prompt, JSON/plain mode, 4,096-token context allocation and shared queue deadline remain unchanged. The partial first answer is discarded; a second incomplete answer is rejected. Structured task proposals still require complete JSON and independent capability validation. No partial proposal can create or run tasks.

Other malformed/incomplete responses are not repeatedly retried. Validation rejection is reported separately from transport/service unavailability. The user sees a useful reason for the output limit, incomplete response or unfinished final answer. The service remains available for subsequent requests. Internal/model reasoning is never returned as an answer.

Advice prompts ask for concise explanations and a minimal code example when appropriate. File-analysis prompts instead focus on the requested captured fact, avoiding unnecessary code generation. Assistance replies remain suggestions, not actions or automatic memory entries.

## MCP identity grounding

Greeting prompts no longer include unrelated world district/population counts. Those measurements remain available for relevant world/status questions. MCP's profile explicitly describes coordination of GRID as a whole and prohibits inferring a numbered-district authority assignment from world counts or previous generated replies. This improves grounding; it is not a guarantee against every model hallucination. Existing transcripts are preserved.

## Verification and test

**759 Release tests passed**, zero failures/skips: 14 Core, 157 Simulation, 99 Persistence, 489 Terminal. Seven added cases cover plain/JSON retry budgets, final-answer-only delivery, bounded repeated truncation, accurate rejection reporting and worker recovery, malformed response without retry, cancellation before retry, assistance error labeling and greeting/world-status grounding (the retry case runs in both modes).

The bundled-runtime assistance probe now includes the user's exact HTML question and `talk hello?`, alongside captured-file analysis and typed drafting. The final probe passed: MCP introduced itself as GRID's coordinator, the advice response provided a CSS centering example, analysis identified Cobalt42, and proposal generation returned VOLT / volt.status / current. No user world was opened and no host task was created or executed. The probe uses the bundled runtime with restricted global runtime paths; it is not a clean-machine or network-firewall isolation test.

After closing the older GRID instance, launch the updated Release build and test:

```text
grid -start
connect MCP -inline
talk hello?
assist advice how i can center a div in html
```

A remaining rejection now explains the response problem rather than claiming that a working service is offline. Requests can still fail if a complete answer does not fit the larger bound or the local model returns invalid output. Schema v23 is unchanged; saves and task approvals are unaffected.
