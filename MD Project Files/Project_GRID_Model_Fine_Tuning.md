# GRID model fine-tuning

## 6.29 scope and status

The CPU synthetic LoRA reference workflow is implemented and verified on 2026-11-13. The user selected a small test model and synthetic data for acceptance. `Grid.AI/FineTuningPreparation.cs` prepares reviewed inputs, `Grid.Terminal/FineTuningCommands.cs` exposes development commands, and `TestSupport/Training/verify_finetuning.py` performs actual reference training, checkpoint resume, export, reload, comparison and rollback.

The reference worker uses a seeded, randomly initialized one-layer GPT-2 with a fixed UTF-8 byte vocabulary. It trains adapters against ten authored synthetic examples, split 6/2/2 for training/validation/test. The repeated readiness response exercises training mechanics. Assistant capability evaluation remains part of candidate release acceptance.

## Recorded workflow

1. Generate the seed-629 base, save Safetensors and record hashes of weights/configuration/tokenizer description.
2. Build an explicitly permitted synthetic dataset through the 6.28 command, then prepare separate JSONL splits through the 6.29 command.
3. Record seed, rank, alpha, dropout, learning rate, epochs/step cap, sequence length, batch size, accumulation, checkpoint interval, target modules, device, base reference, dependency versions/licenses and export target.
4. Measure baseline loss for each split. Optimize response-token loss using training examples. Validation and test examples stay out of optimizer updates.
5. Save the adapter and a step-90 checkpoint with optimizer/RNG state. Repeat training from the same seed, and separately resume from the checkpoint.
6. Reload the adapter, merge/export a standalone Safetensors model, and verify loaded logits. Disable the adapter to verify baseline rollback. Compare original base hashes after training.
7. Store metrics, artifact hashes, dependency lock/worker hashes, memory and storage measurements in `report.json`. Acceptance thresholds are lower loss on all three splits, identical repeated adapter weight hashes, and maximum comparison error below 0.00001.

LoRA trains low-rank adapters around frozen base weights. PEFT exports include adapter parameters and configuration, and loading requires the corresponding base model. [LoRA documentation](https://huggingface.co/docs/peft/en/developer_guides/lora), [PEFT checkpoint format](https://huggingface.co/docs/peft/en/developer_guides/checkpoint).

## Commands and configuration

Run development commands from the repository root. The reference script creates a fresh output directory and drives the regular terminal executable before world initialization.

```powershell
.\.grid-runtime\training\python\Scripts\python.exe TestSupport/Training/verify_finetuning.py --terminal Grid.Terminal/bin/Debug/net10.0/Grid.Terminal.exe --output .grid-runtime/training/runs/my-new-test
```

To prepare an independently reviewed job using the generated `configuration.json` structure, replace its base identity/hash/license, settings and exact dependency inventory with the selected candidate's values.

```powershell
dotnet run --project Grid.Terminal -- --finetune-prepare configuration.json dataset.json new-job-directory
dotnet run --project Grid.Terminal -- --finetune-verify new-job-directory
```

These preparation commands verify the dataset, configuration, exported splits and hashes. The prepared job records a declared base manifest digest and dependency inventory. Actual installed assets and provider compatibility require a training worker's acceptance report. `Prepared` is the only state emitted by preparation. The reference worker is specialized to the synthetic CPU fixture. General candidate execution requires the selected model's tokenizer, module mapping and evaluation suite.

Prepared outputs publish through a sibling staging directory. Existing destinations are refused, failed staging is cleaned up, and dataset/split files are hash-bound. Source paths using filesystem links are rejected by the existing style of artifact containment checks. Integrity hashes require a separately trusted release record for authenticity.

## Dependencies, hardware and storage

The tested environment is Windows x64, Python 3.12.14, PyTorch 2.15.0+cpu, Transformers 5.23.0, PEFT 0.21.3, Accelerate 1.15.0 and Safetensors 0.8.0. All installed package versions are pinned in `TestSupport/Training/requirements-cpu.lock.txt`. The reference worker checks that lock before importing the training stack. License metadata for the principal packages is recorded in the report and installed distribution metadata remains available for the complete dependency review.

The reference runs on CPU with one thread. The host also reports an RTX 5060 Ti with 8,151 MiB VRAM. GPU eligibility requires a separate tested CUDA/bitsandbytes stack and candidate memory measurement. The CPU acceptance used roughly 350 MB peak working set. This measurement establishes the tiny fixture's observed footprint.

Development assets live under `.grid-runtime/training`. The reference uses a 2,000,000,000-byte development budget and a 10,000,000-byte checkpoint budget. Preparation checks input bytes plus checkpoint reservation and available disk space. The reference report accounts for the environment, cached wheels, runs and retained artifacts under the training root before writing the final report. The recorded environment is approximately 895 MB and its cached wheels approximately 179 MB. These development allocations also count toward the existing total package limit whenever included in a package.

The local virtual environment depends on its Python installation. Cross-machine training requires a compatible provisioned interpreter. Packaging a relocatable training interpreter and certifying it on another PC remain release work. GRID's ordinary inference installation continues to use its existing runtime provisioning.

## Offline reproduction

The installed package wheels are cached in `.grid-runtime/training/wheelhouse`. After provisioning a compatible Python 3.12 x64 interpreter and creating a fresh virtual environment, install the tested dependencies using local wheels.

```powershell
path-to-new-environment\Scripts\python.exe -m pip install --no-index --find-links .grid-runtime/training/wheelhouse -r TestSupport/Training/requirements-cpu.lock.txt
```

Provisioning can download packages as an installation step. The acceptance worker sets offline library flags and rejects Python socket connections throughout its run, with zero attempted connections recorded. Network-blocked OS-level and separate-device acceptance remain part of final deployment verification. Transformers supports locally cached models and offline operation. [Offline installation guidance](https://huggingface.co/docs/transformers/main/en/installation).

## QLoRA and runtime release plan

QLoRA configuration planning uses CUDA, bitsandbytes 4-bit loading, the exact base revision, recorded quantization/compute dtype, k-bit training preparation and PEFT adapters. The preparation contract requires a CUDA device declaration and a bitsandbytes version/license entry for QLoRA. Quantization settings and actual GPU execution remain to be implemented against a selected candidate. [PEFT quantized training](https://huggingface.co/docs/peft/main/en/developer_guides/quantization).

The verified export/reload target is Transformers plus PEFT. Production Ollama/llama.cpp conversion/import must verify the exact architecture, tokenizer/chat template, base/adapter match, output hashes and device profile. Merging is verified on the synthetic base. Candidate import, model-registry activation, retained-revision rollback and saved-world continuity require the existing release gates before an agent uses the candidate. [Ollama import formats](https://docs.ollama.com/import).

## Verification and remaining gates

- 13 focused preparation regression cases passed.
- All 1,286 solution tests passed.
- The live synthetic run passed repeatability, checkpoint resume, adapter reload, merged export/reload and rollback. Repeat, resume, reload and rollback logit differences were zero. Merged export maximum difference was approximately 2.24e-7.
- Baseline-to-candidate losses were approximately 5.5934 to 4.9498 for training, 5.6016 to 5.2489 for validation and 5.5930 to 5.2623 for test.
- Exact evidence is retained in [CPU acceptance report](../TestSupport/Training/acceptance-cpu-20261113.json). Large local run artifacts are under `.grid-runtime/training/runs/synthetic-629-acceptance`.
- The tested stack emits a Transformers gradient-checkpointing deprecation warning and a default causal-loss notice. Both are recorded in the observed run output and did not prevent acceptance.
- Production base selection, production corpus review, QLoRA/GPU validation, agent-runtime import and portable training installation remain open under candidate/final Phase 7 acceptance. The next sequential module is 6.30.
