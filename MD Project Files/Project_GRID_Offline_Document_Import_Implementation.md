# Offline PDF and DOCX import — 6.15

**6.20 lexical retrieval foundation — 2026-11-05.** Scoped captured-document search and assistance now use SQLite-backed chunks, BM25/token-feature vectors, reranking and versioned citations. 1,165 current-suite tests and a live deep-document answer/revocation check passed. Learned semantic embeddings remain pending; original files are not reopened and Phase 7 is not cleared. [Implementation and limits](Project_GRID_Retrieval_Implementation.md).

**Implemented and locally verified — 2026-11-04.** PDF text extraction, DOCX body paragraphs/tables, picker support, bounded parsing and import diagnostics are implemented. Second-device acceptance and the final 6.7–6.31 readiness gate are pending.

## Import workflow

Use `context add -search` to select PDF, DOCX or existing supported text/source files. Existing explicit-path imports and connected-chat file selection use the same reader. Context remains scoped to the selected project and recipient, with temporary/persistent retention governed by existing grants.

```text
context add "C:\Documents\example.pdf" temporary docs
context list
context read example.pdf
```

Substitute an existing absolute file path. A PDF import captures its source path/name, byte count, modification time, SHA256, capture time, recipient and extracted text. Page markers use `[PDF page N of M]`. DOCX captures use numbered body paragraphs and table row/cell markers. `context read` reports capture provenance and the extracted content. Later reads use the captured snapshot. Re-import explicitly to capture file changes.

PDF pages with no extractable text receive a visible blank/scanned-page notice. A wholly empty or image-only PDF is refused with instructions to use offline OCR or export a text PDF and retry. OCR is outside this implementation. Mixed documents preserve extracted text and page-specific no-text notices. Embedded images, handwriting and graphical table structure require further interpretation or OCR. Reading order follows the PDF content-order extractor and can differ from complex visual layouts.

DOCX extraction reads the main body, including paragraphs, adjacent runs, tabs, line breaks, tables, nested paragraphs and Unicode in transitional or strict WordprocessingML namespaces. Headers, footers, comments, footnotes and image text are outside the `docx-body` scope. Linked/embedded `altChunk` or `subDoc` body content requires a flattened export. This prevents successful imports that silently omit those body sections. Macros, external relationships, scripts and links are not executed by the extraction code.

## Bounds and failure handling

Existing source/context quotas apply to every format. Source files are limited to 1 MiB, extracted content to 256 Ki characters per import, and the collection to 64 imports / 1 Mi total characters. Text and PDF limits reject oversized content with an actionable split/export message. PDF page count is limited to 200. Encrypted PDFs, including documents with an empty user password, require an unlocked export.

PDF and DOCX parsing runs in a child instance of the bundled terminal, entering the extraction worker before world loading, login or AI startup. The parent sends captured bytes through standard input. The worker returns bounded JSON through standard output. It receives the format and captured bytes, with no source-file path passed as an argument.

The parent enforces a 20-second deadline, bounded output, cancellation and a monitored 512 MiB working-set threshold. The worker has a 256 MiB managed-heap limit. Working-set monitoring is sampled and can transiently overshoot its threshold. Worker termination and parser failure preserve the import's all-or-nothing outcome. This is process/resource isolation under the current Windows user account, not an OS permission sandbox. No claim of arbitrary hostile-code containment is made.

DOCX parsing prohibits DTDs and external XML resolution, limits archives to 256 entries and 16 MiB declared expanded content, and reads at most 1 MiB of body XML. Source path checks retain local-drive, opened-path and junction restrictions. File-selection batches commit only after every selected import succeeds. Host shutdown cancellation propagates through the import reader to the child worker. The current synchronous command interface supports shutdown cancellation and the bounded deadline rather than a new interactive extraction-cancel command.

## Dependencies, packaging and persistence

PdfPig 0.1.16 is pinned as a build dependency. Its managed libraries and license are copied to normal build/publish output and travel with application files during copy/transfer. The license includes upstream component notices and is stored at `licenses/PdfPig-LICENSE`. The package was also copied into the local `.grid-runtime/nuget` offline build cache. Fresh source setups provision dependencies during their explicit restore/install step. [Pinned upstream source](https://github.com/UglyToad/PdfPig/tree/a7bb35662bbbf405efddad50aedc9bcdcf515afc).

The document parser uses these packaged .NET libraries without Office, a PDF application, Python or a cloud converter at runtime. `pdf-pages` is an added accepted context-format value in the existing context record. Save/reload preserves its extracted content and provenance through the current schema. Older application versions that reject this format require a pre-import backup for rollback.

The published extraction test application occupied 92,012,304 bytes at verification time. PdfPig assemblies contributed 5,721,600 bytes. These figures exclude the separate AI/runtime/model package. Both fit within the currently implemented 20 GB base check. The planned 128 GB installation target and nested 75 GB model cap remain work for 6.27.

## Completion verification

- All 803 terminal tests passed, including 15 new document cases. The current Core 14, Simulation 157 and Persistence 99 suites also passed, totaling 1,073 passing regression tests across the latest relevant runs.
- Coverage includes normal command import, saved-world reload, source hash/page provenance, mixed/empty pages, page limits, corrupt/signature-mismatched inputs, password protection, DOCX Unicode/tables/strict namespace/nested paragraphs, linked-body refusal, cancellation and atomic picker batches.
- An initial full run exposed a process-exit race during working-set sampling. The race was repaired and the complete terminal suite passed afterward.
- Self-contained Windows x64 publishing succeeded. PDF and DOCX worker checks passed from the published app with isolated PATH/profile settings and invalid global DOTNET_ROOT paths. The test uses a synthetic document and opens no user world.
- Local deployment evidence is stored in `portable/GRID7736-615-documents/probe/report.json`. This is an extraction-only test build. A full refreshed GRID package is required for the second-PC AI/world tests.
- A physical second-PC, changed-drive-letter and network-blocked acceptance run remains pending. Phase 7 also requires completion of 6.16–6.31.

## Second-device acceptance

Create a fresh portable copy after rebuilding. On the second supported Windows x64 PC, disable networking, launch that copy, and import a text PDF, a DOCX with multiple table cells, a mixed text/scanned PDF and a scan-only PDF. Confirm captured page/cell content and clear no-text results. Repeat after restart and a changed drive letter, then inspect persistent captures and their original hashes. Record package size, dependency integrity, runtime/device details, import results and any limitations with the existing 6.14 acceptance report.
