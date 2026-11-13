# GRID training data pipeline

## 6.28 implementation

Implemented 2026-11-12 in `Grid.AI/TrainingDataset.cs`, with offline development commands in `Grid.Terminal/TrainingDatasetCommands.cs`. Commands run before world initialization. Inputs are explicitly supplied JSON files. Reviewed examples carry source, group, split and label information.

## Create and verify a dataset

From the repository root, run the synthetic fixture through the regular executable entry point. Choose a fresh output filename for each run.

```powershell
dotnet run --project Grid.Terminal -- --dataset-build TestSupport/Training/dataset-smoke-v1.json dataset-smoke-v1.json
dotnet run --project Grid.Terminal -- --dataset-verify dataset-smoke-v1.json
```

The fixture demonstrates all three splits. Use its JSON structure for a reviewed dataset request. Source `Kind` accepts `synthetic`, `public`, `document` or `conversation`. Each included source requires an origin, license basis, reviewer, ISO review date, and explicit `LicenseApproved`, `PrivacyApproved` and `TrainingPermission` attestations. Conversation/document collection requires the data owner's permission before preparing an input. Reviewers must substantiate license rights and inspect personal data before approving a source. Existing world conversations and context entries are outside this command's input discovery.

`Label` records the supervised task category. `Group` identifies a related content family, including paraphrases identified during review. Every example explicitly selects `train`, `validation` or `test`. Reviewers choose dataset proportions. Small shards may contain a subset of splits, so training/release tooling must check the complete selected corpus has adequate held-out coverage.

## Reproducibility and held-out integrity

- The manifest stores schema/version, input SHA256, content SHA256, reviewed source metadata, retained examples, duplicate provenance and exclusions. Identical inputs produce identical manifests. Input ordering is part of the request digest.
- Unicode compatibility normalization, collapsed whitespace and invariant case normalization identify equivalent prompts. Equivalent answers deduplicate deterministically by example ID. Conflicting answers require review.
- Sources, content groups and equivalent prompts cannot span splits. Bundled text benchmark prompts are rejected from training and validation splits.
- A revision can supply the prior manifest as the fourth command argument. This records the parent digest and checks source/group/prompt/example-ID split continuity against that revision.

```powershell
dotnet run --project Grid.Terminal -- --dataset-build request-v2.json dataset-v2.json dataset-v1.json
```

Always supply the prior accepted manifest for revisions. Preserve its digest in the release record. Verification checks integrity and validation rules against the stored digest. External trusted digest records establish authenticity. Semantic similarity, paraphrase contamination and falsely declared source permissions still require human review. Cross-shard review must cover the complete selected corpus before training.

## Exclusion and removal

1. Keep the affected source identifier in the new request's `Sources` and add an exclusion, for example `{"SourceId":"withdrawn-source","Reason":"Owner withdrew permission"}`.
2. Use a new dataset version and supply the previous accepted manifest.
3. Build and verify the revision. All examples and source metadata for excluded sources are omitted. The exclusion retains its ID and reason. The input fingerprint retains a digest of the original request.
4. Retire affected earlier datasets, exported copies and dependent adapters/checkpoints under the operator's retention/removal process. Record their locations and disposition. Rebuild or retrain dependent outputs before release. A new manifest alone does not erase prior copies or remove knowledge from an existing model.

Outputs publish through a flushed sibling temporary file and a single move to a new target. Existing outputs are refused. Failure cleans up the temporary file. Explicit output paths remain under the operating user's filesystem permissions.

## Bounds and verification

Requests/manifests are limited to 32 MiB at the file boundary. In-memory builds allow up to 10,000 sources, 100,000 examples and 16 Mi characters of retained prompt/response text, with 100,000 characters per prompt or response. These are initial development-tool limits. Split larger corpora into reviewed shards. Credential-pattern checks reuse `InferenceContentGuard`, alongside control-character rejection. Pattern checks have false positives and incomplete secret/PII coverage.

Verification on 2026-11-12 passed 19 dataset regression cases and 1,273 solution tests. Coverage includes review/permission rejection, deterministic output, duplicates/conflicting answers, split isolation, revision continuity, exclusions, tampering, benchmark protection, malformed inputs, publication refusal and command handling. The synthetic fixture supports offline entry-point verification. Model fine-tuning and a permitted production training corpus continue under 6.29.
