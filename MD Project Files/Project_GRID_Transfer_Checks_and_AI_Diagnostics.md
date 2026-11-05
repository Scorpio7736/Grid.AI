# Transfer checks and AI diagnostics — 2026-10-30

## Implemented changes

Transfer preflight checks required files, model manifest identity, lengths, paths, space and the base budget without reading every model byte. Copying hashes the source bytes as they are written, holds the source read lease through destination verification, and independently reads/hashes the destination once. Copying an existing package also compares its original portable-manifest hashes; corrupt source assets cannot simply be sealed as a new valid package.

Final validation reuses the verified destination hashes to check the pinned model digests and portable inventory. It still checks paths, lengths, missing receipts, required runtime structure, saved database loadability, authorization and budget before publication. It no longer rereads the entire package or model at the 90% phase. Standalone audits still perform full content verification. Move cleanup retains its independent source/destination rechecks before deleting source files because those files can change between copying and cleanup.

Path validation uses one attributes request per ancestor rather than separate file-existence, directory-existence and attributes requests. Junction/link refusal remains in place. No source files are deleted by a copy, and unsuccessful staging is retained for diagnosis.

Destination hashing emits chunk progress. Final asset validation emits the current inventory path. Remaining copy time uses actual copied-plus-verified bytes and elapsed copy-phase time, rather than extrapolating total wall time from the visual phase percentage. Final metadata/cleanup time is explicitly unestimated. After ten seconds without an update the window shows how long it has been waiting; this does not claim the operation has failed or cancel it. The bar remains a phase-weighted indicator, not a literal percentage of seconds remaining.

## Local AI diagnosis and limits

The reported failure was not reproduced in an isolated original-PC run: bundled AI readiness, chat, context analysis and task drafting succeeded without opening the user's saved world. The failing instance's exact error and machine are still needed. This is **not a claim that the user's AI failure is resolved**.

Bundled startup now permits two minutes instead of thirty seconds for slow storage/first-time hardware discovery. `ai status` and `ai test` retain the original startup result, with runtime-log location on startup failure. Connection errors include the endpoint and exception message. Disabled chat inference retains the specific startup failure rather than replacing it with a generic disabled message. Restart the original terminal after rebuilding; an attached conversation is not a separate AI host.

No global Ollama fallback, cloud service, model download, package-size increase or new dependency is introduced. Existing unrelated/orphan AI processes were not terminated. A copied package must be refreshed explicitly to receive these application changes; existing flash-drive copies are not hot-patched.

## Verification

The user's saved dashboard report confirms the earlier USB transfer completed on October 29 at 23:59:21 America/Chicago. Its final asset-validation phase ran from 23:28:06 to 23:59:20, approximately 31 minutes. That is evidence about the old implementation, not a benchmark of the revised transfer on that USB drive.

Regression coverage includes destination-verification byte events, copy-time estimates, receipt matching/missing/mismatched hashes, changed lengths, no content reread during receipt validation, and preservation of AI startup errors in chat and diagnostics. Final test results and disposable-copy evidence are recorded below after execution.

The physical second-PC/offline/removable-drive acceptance gate and PDF extraction in 6.15 remain pending. Phase 7 remains blocked.
