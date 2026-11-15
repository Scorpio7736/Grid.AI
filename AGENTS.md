# GRID agent working rules

## Scope and context

- Work on the requested checklist item and the dependencies needed to complete it.
- Start with `MD Project Files/agent context/CURRENT_HANDOFF.md`, then read the relevant master checklist section and implementation reference.
- Search for relevant symbols and read bounded file sections. Expand investigation when evidence requires it.
- Check the working tree and preserve existing user changes and implementation history.

## Verification

- Before promoting changes to production `main`, follow [main branch requirements](MD%20Project%20Files/Project_GRID_Main_Branch_Requirements.md): the final candidate must pass the entire solution suite with no skipped tests, and installation must remain reproducible using the tracked setup and verified dependency pins. Record fresh-install evidence when installation behavior or dependencies change. Broader production rules are deferred.

- Run focused tests during development. Run one full suite at completion when appropriate for the change and its acceptance criteria.
- Add regression coverage for behavior that existing tests would miss. Documentation-only changes normally need link/content review and `git diff --check`.
- Record actual verification results and keep unverified acceptance items open.

## Documentation and communication

- Update the master checklist and the relevant implementation reference with concise completion evidence. Link other affected documents to that reference when needed.
- Keep architecture and user-facing documentation consistent with changed behavior. Avoid copying the same completion narrative into multiple files.
- Give brief progress updates. Keep the final summary under 150 words unless the user requests detail or the result requires more explanation.
- At milestone boundaries, refresh the short current handoff with next work, verification and open gates. Keep historical detail in its existing references.
- Prefer relevant error excerpts and file paths when requesting diagnostics.
- Use a new chat at a milestone boundary when the user chooses to do so, with the current handoff as its starting context.
