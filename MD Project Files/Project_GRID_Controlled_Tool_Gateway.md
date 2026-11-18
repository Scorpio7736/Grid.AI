# GRID controlled tool gateway — 6.23

**6.24 completed — 2026-11-12.** Shared inference now withholds recognizable credential material and invalid control-character output. Strict proposal parsing rejects worker approval/scope expansion and uses content-free rejection messages. Format-repair retries recheck live authorization. All 1,225 solution tests passed, including twelve new cases and the joint 6.22–6.23 authorization/tool suites. Credential detection is pattern-based with documented false-positive and coverage limits. [Implementation and evidence](Project_GRID_Layered_Guardrails.md).

## Implemented on 2026-11-07

Grid.Agent defines typed task, file-operation and receipt contracts. Grid.Simulation validates live identity and destination scope before passing requests to its existing task broker and file-change services. Capability-specific command and simulation adapters retain their execution isolation, argument allowlists, approval binding and cancellation behavior.

MCP may delegate one level to MAXIS, SENTINEL, JN, VOLT or RELAY, with at most eight child tasks. Recursive delegation is refused. This implementation retains the existing single active broker job across all profiles and adds a ten-minute cancellation budget per ordered dispatch. Remaining children stay undispatched after failure or cancellation. These conservative limits are the initial implementation choice. Profile-specific concurrency increases and deeper delegation require later evaluation.

`mcp ledger <plan-id>` projects the saved parent/child task records into receipts containing status, attempts, start/finish times and SHA256 request/result fingerprints. Existing approval and task audit records supply detailed evidence. Saved tasks recover through the existing persistence path. Token-cost accounting is outside this receipt format.

## Generated documents and image validation

Master Control can stage a fixed report through `changes generate`. The output includes a native candidate and an HTML layout preview. `changes preview` repeats their locations. Review them, approve the exact change and then apply it. Both approval and apply verify candidate/preview hashes. Existing destination hashes are checked again before replacement. Durable original/candidate backups support guarded rollback and recovery by change ID after restart.

Supported report formats are DOCX and PDF. The initial template uses Letter paper, Courier, half-inch margins, 72-character wrapping and 54 lines per page. Input is limited to 10,000 printable ASCII characters with line breaks and eight pages. Generated files are limited to 1 MiB. DOCX packages have deterministic entries and timestamps. PDF generation uses the bundled PdfPig library. Native files are reopened for structural validation. Other Office formats, Unicode report templates and rich layouts remain future work.

`changes image` reads one explicitly scoped PNG and validates its signature, chunk checksums, dimensions and decompressed pixel rows before staging it. The initial supported subset is 8-bit RGB/RGBA, non-interlaced, at most 2048 by 2048 pixels and 1 MiB, containing IHDR, IDAT and IEND chunks. Metadata-bearing PNGs and other image formats require later support. Image-model generation remains pending specialist-model selection.

Scopes use the existing registered workspace or file-destination mechanism. Parent directories must exist. Existing protected-path, traversal and reparse-point checks apply. Text edits retain their 64 KiB bound. Generated artifacts require the existing `high-risk` acknowledgement, including overwrites. Up to sixteen drafts are held per terminal session. Staged previews and applied recovery evidence are retained on disk. Unapplied approval state expires with the session. Recovery of generated binary changes requires this version or newer.

## Manual test

Log in as Master Control, then use an existing writable destination such as Desktop.

```text
changes generate @desktop grid-report.pdf "GRID report" "This is a reviewed local report."
changes preview <change-id>
changes approve <change-id> high-risk
changes apply <change-id>
changes rollback <change-id>
```

Open the printed candidate and preview paths before approving. Repeat with `grid-report.docx` for Word output. For an existing supported PNG, use the following command and follow the same review/apply lifecycle.

```text
changes image @desktop reviewed.png @desktop source.png
mcp ledger <plan-id>
```

## Completion verification

On 2026-11-07, all 1,202 solution tests passed, comprising Core 14, Simulation 157, Persistence 99 and Terminal 932. New coverage verifies native report content/pagination, deterministic DOCX bytes, unsupported inputs, PNG corruption/pixel validation, delegation restrictions, receipts across restore, binary replacement/rollback after recovery, preview tampering and changed destinations.

The `--verify-generated-artifacts` diagnostic generated representative three-page DOCX/PDF reports in a temporary directory. Microsoft Word opened the DOCX and exported it to PDF for review. Poppler rendered both native PDF and Word-exported PDF. All six page images were inspected for complete text, wrapping, margins and clipping. Word was used only for this development check. Report generation itself uses bundled .NET/PdfPig components. Poppler reported missing fallback Symbol/ArialUnicode fonts during rendering, while the Courier sample text rendered legibly on all pages.

```text
dotnet test Grid.AI.slnx --no-restore --verbosity minimal
Grid.Terminal.exe --verify-generated-artifacts
```

The broader joint 6.22–6.24 adversarial/action acceptance gate remains assigned to 6.24. Phase 7 readiness also retains the other documented deployment and model acceptance gates.

## Gateway acceptance follow-up — 2026-11-12

Eleven new regression cases exercise the gateway contracts and generated-output path directly. The full solution passed 1,213 tests, comprising Core 14, Simulation 157, Persistence 99 and Terminal 943.

| Acceptance area | Recorded coverage |
| --- | --- |
| Exact request and approval binding | TypedGatewayRetryRequiresFreshApprovalAfterFailedSave checks identical-request deduplication, changed-request refusal and fresh approval after a failed durable save |
| Forgery and stale identity | TypedFileGatewayRejectsForgedAndExpiredSessions and TypedGatewayRejectsUnknownCapabilitiesAndForgedSessions |
| Denied paths | TypedFileGatewayRejectsDeniedPaths covers traversal, protected folders and alternate data streams |
| Artifact revocation | GeneratedArtifactApprovalExpiresWithAuthority covers workspace revocation, pause, logout and identity switch |
| Candidate integrity | GeneratedCandidateTamperingAfterApprovalPreventsApply rejects changed candidate bytes after approval |
| Concurrent destination changes | ArtifactApprovalRejectsTamperedPreviewAndConcurrentTargetChanges and existing workspace-edit regression cases |
| Restart and evidence | GeneratedArtifactsRequireApprovalAndRecoverAfterRestart and ToolLedgerBindsParentRequestAndRecordedOutcomeAcrossRestore |
| Model output and authority | Existing AssistanceTests, McpWorkflowTests, TaskBrokerTests and AuthorizationRouteTests run in the same full-suite verification |

The first retry-test run exposed two incorrect test expectations about existing exception behavior. The assertions now require IOException for repeated save failure and InvalidOperationException for an unapproved retry. The final full run passed. Production gateway behavior passed the additional checks without a runtime code change.

This completes gateway-specific acceptance. Section 6.24 retains broader layered-guardrail implementation and adversarial acceptance. Section 6.31 retains the conversation-only requirement for chats and meetings. Report and PNG bounds documented above continue to define the implemented generator scope.
