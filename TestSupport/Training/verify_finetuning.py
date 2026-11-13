"""Offline 6.29 acceptance using a tiny synthetic GPT-2 base and reviewed synthetic data."""
import argparse
import hashlib
import importlib.metadata
import json
import os
from pathlib import Path
import socket
import subprocess
import sys
import time

os.environ.update(HF_HUB_OFFLINE="1", TRANSFORMERS_OFFLINE="1", HF_HUB_DISABLE_TELEMETRY="1", TOKENIZERS_PARALLELISM="false")
network_attempts = []

def deny_network(*args, **kwargs):
    network_attempts.append("blocked")
    raise OSError("Network disabled for GRID training acceptance")

socket.socket.connect = deny_network
socket.create_connection = deny_network

lock_path = Path(__file__).with_name("requirements-cpu.lock.txt")
for requirement in lock_path.read_text(encoding="utf-8-sig").splitlines():
    if requirement.strip():
        package, version = requirement.split("==", 1)
        if importlib.metadata.version(package) != version:
            raise RuntimeError(f"Training dependency version differs from tested lock: {package}")

import torch
import psutil
from transformers import GPT2Config, GPT2LMHeadModel
from peft import LoraConfig, PeftModel, get_peft_model


def digest(path):
    with open(path, "rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest().upper()


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def invoke(terminal, *args):
    subprocess.run([str(terminal), *map(str, args)], check=True, timeout=120)


def batch(example):
    prompt = list((example["Prompt"] + "\n").encode("utf-8"))
    answer = list(example["Response"].encode("utf-8")) + [256]
    tokens = prompt + answer
    if len(tokens) > 128:
        raise ValueError("Synthetic fixture exceeds sequence budget")
    return {"input_ids": torch.tensor([tokens]), "attention_mask": torch.ones(1, len(tokens), dtype=torch.long),
            "labels": torch.tensor([[-100] * len(prompt) + answer])}


def losses(model, examples):
    model.eval()
    with torch.no_grad():
        return {split: sum(model(**batch(e)).loss.item() for e in examples if e["Split"] == split)
                / sum(e["Split"] == split for e in examples) for split in ("train", "validation", "test")}


def logits(model, example):
    model.eval()
    with torch.no_grad():
        return model(**batch(example)).logits.detach().clone()


def new_adapter(base_path, config):
    torch.manual_seed(config["Seed"])
    base = GPT2LMHeadModel.from_pretrained(base_path, local_files_only=True)
    return get_peft_model(base, LoraConfig(r=config["Rank"], lora_alpha=config["Alpha"],
        lora_dropout=config["Dropout"], target_modules=config["TargetModules"],
        fan_in_fan_out=True, task_type="CAUSAL_LM", bias="none"))


def train(model, config, examples, output, start=0, state=None):
    output.mkdir()
    optimizer = torch.optim.AdamW((p for p in model.parameters() if p.requires_grad), lr=config["LearningRate"])
    if state is not None:
        optimizer.load_state_dict(state["optimizer"])
        torch.set_rng_state(state["rng"])
    training = [e for e in examples if e["Split"] == "train"]
    model.train()
    for step in range(start, config["MaxSteps"]):
        optimizer.zero_grad()
        loss = model(**batch(training[step % len(training)])).loss
        loss.backward()
        optimizer.step()
        if step + 1 == config["CheckpointSteps"]:
            checkpoint = output / "checkpoint"
            model.save_pretrained(checkpoint, safe_serialization=True)
            torch.save({"step": step + 1, "optimizer": optimizer.state_dict(), "rng": torch.get_rng_state()}, checkpoint / "optimizer.pt")
    model.save_pretrained(output / "adapter", safe_serialization=True)
    checkpoint_bytes = sum(p.stat().st_size for p in output.rglob("*") if p.is_file())
    if checkpoint_bytes > config["CheckpointBudgetBytes"]:
        raise RuntimeError("Checkpoint budget exceeded")
    return model


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--terminal", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    terminal = args.terminal.resolve(strict=True)
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    started = time.monotonic()
    torch.set_num_threads(1)
    torch.use_deterministic_algorithms(True)
    torch.manual_seed(629)
    base_path = output / "base"
    base = GPT2LMHeadModel(GPT2Config(vocab_size=258, n_positions=128, n_embd=32, n_layer=1, n_head=2,
        resid_pdrop=0, embd_pdrop=0, attn_pdrop=0, bos_token_id=256, eos_token_id=256, pad_token_id=257, use_cache=False))
    base.save_pretrained(base_path, safe_serialization=True)
    write_json(base_path / "byte-tokenizer.json", {"encoding": "utf-8 bytes", "eos": 256, "pad": 257, "vocabulary": 258})
    base_manifest = {"id": "grid-synthetic-gpt2", "revision": "seed-629-v1", "license": "Project-generated random test weights",
        "assets": [{"path": p.name, "sha256": digest(p), "bytes": p.stat().st_size} for p in sorted(base_path.iterdir()) if p.is_file()]}
    write_json(output / "base-manifest.json", base_manifest)
    request = {"Version": "grid-finetuning-synthetic-v1", "Sources": [], "Examples": [], "Exclusions": []}
    for split, count in (("train", 6), ("validation", 2), ("test", 2)):
        request["Sources"].append({"Id": split, "Origin": "GRID authored synthetic readiness fixture", "Kind": "synthetic",
            "License": "Project-authored synthetic fixture", "Reviewer": "GRID fixture author", "ReviewDate": "2026-11-13",
            "LicenseApproved": True, "PrivacyApproved": True, "TrainingPermission": True})
        for number in range(count):
            request["Examples"].append({"Id": f"{split}-{number}", "SourceId": split, "Group": split,
                "Split": split, "Label": "synthetic readiness", "Prompt": f"{split} readiness check {number}", "Response": "GRID ready."})
    write_json(output / "request.json", request)
    invoke(terminal, "--dataset-build", output / "request.json", output / "dataset.json")
    packages = []
    for name in ("torch", "transformers", "peft", "accelerate", "safetensors"):
        metadata = importlib.metadata.metadata(name)
        packages.append({"Name": name, "Version": importlib.metadata.version(name),
            "License": (metadata.get("License-Expression") or metadata.get("License") or "See bundled distribution metadata")[:500]})
    packages.append({"Name": "python", "Version": sys.version.split()[0], "License": "PSF-2.0"})
    config = {"Schema": 1, "RunId": "synthetic-lora-629", "Method": "lora", "Seed": 629,
        "Rank": 8, "Alpha": 16, "Dropout": 0.0, "LearningRate": 0.01, "Epochs": 30, "MaxSteps": 180,
        "SequenceLength": 128, "BatchSize": 1, "GradientAccumulation": 1, "CheckpointSteps": 90,
        "TargetModules": ["c_attn", "c_proj"], "Device": "cpu", "BaseModelId": base_manifest["id"],
        "BaseRevision": base_manifest["revision"], "BaseManifestSha256": digest(output / "base-manifest.json"),
        "BaseLicense": base_manifest["license"], "BaseTrainingPermission": True, "Packages": packages,
        "DevelopmentBudgetBytes": 2_000_000_000, "CheckpointBudgetBytes": 10_000_000,
        "ExportFormat": "peft-safetensors", "TargetProvider": "transformers", "TargetProviderVersion": importlib.metadata.version("transformers")}
    environment_bytes = sum(p.stat().st_size for p in Path(sys.prefix).rglob("*") if p.is_file())
    wheelhouse = Path(sys.prefix).parent / "wheelhouse"
    wheelhouse_bytes = sum(p.stat().st_size for p in wheelhouse.glob("*") if p.is_file())
    if environment_bytes + wheelhouse_bytes + config["CheckpointBudgetBytes"] >= config["DevelopmentBudgetBytes"]:
        raise RuntimeError("Local development environment exceeds the training budget")
    write_json(output / "configuration.json", config)
    invoke(terminal, "--finetune-prepare", output / "configuration.json", output / "dataset.json", output / "job")
    invoke(terminal, "--finetune-verify", output / "job")
    examples = []
    for split in ("train", "validation", "test"):
        examples.extend(json.loads(line) for line in (output / "job" / f"{split}.jsonl").read_text(encoding="utf-8").splitlines())
    baseline_losses = losses(base, examples)
    probe = next(e for e in examples if e["Split"] == "test")
    baseline_logits = logits(base, probe)
    first = train(new_adapter(base_path, config), config, examples, output / "first")
    candidate_losses = losses(first, examples)
    candidate_logits = logits(first, probe)
    with first.disable_adapter():
        rollback_error = (logits(first, probe) - baseline_logits).abs().max().item()
    second = train(new_adapter(base_path, config), config, examples, output / "repeat")
    repeat_error = (logits(second, probe) - candidate_logits).abs().max().item()
    reloaded = PeftModel.from_pretrained(GPT2LMHeadModel.from_pretrained(base_path, local_files_only=True), output / "first" / "adapter", local_files_only=True)
    reload_error = (logits(reloaded, probe) - candidate_logits).abs().max().item()
    checkpoint = output / "first" / "checkpoint"
    resumed = PeftModel.from_pretrained(GPT2LMHeadModel.from_pretrained(base_path, local_files_only=True), checkpoint, is_trainable=True, local_files_only=True)
    state = torch.load(checkpoint / "optimizer.pt", map_location="cpu", weights_only=True)
    resumed = train(resumed, config, examples, output / "resumed", start=state["step"], state=state)
    resume_error = (logits(resumed, probe) - candidate_logits).abs().max().item()
    merged = reloaded.merge_and_unload()
    merged.save_pretrained(output / "merged", safe_serialization=True)
    merged = GPT2LMHeadModel.from_pretrained(output / "merged", local_files_only=True)
    merge_error = (logits(merged, probe) - candidate_logits).abs().max().item()
    repeat_hash_equal = digest(output / "first" / "adapter" / "adapter_model.safetensors") == digest(output / "repeat" / "adapter" / "adapter_model.safetensors")
    base_intact = all(digest(base_path / a["path"]) == a["sha256"] for a in base_manifest["assets"])
    run_bytes = sum(p.stat().st_size for p in output.rglob("*") if p.is_file())
    development_root = Path(sys.prefix).parent
    development_bytes = sum(p.stat().st_size for p in development_root.rglob("*") if p.is_file())
    if not output.is_relative_to(development_root):
        development_bytes += run_bytes
    memory = psutil.Process().memory_info()
    passed = (all(candidate_losses[s] < baseline_losses[s] for s in baseline_losses) and
        max(rollback_error, repeat_error, reload_error, resume_error, merge_error) < 0.00001 and
        repeat_hash_equal and base_intact and not network_attempts and
        development_bytes < config["DevelopmentBudgetBytes"])
    report = {"schema": 1, "passed": passed, "scope": "tiny synthetic CPU LoRA acceptance",
        "baseline_loss": baseline_losses, "candidate_loss": candidate_losses,
        "rollback_max_error": rollback_error, "repeat_max_error": repeat_error, "reload_max_error": reload_error,
        "resume_max_error": resume_error, "merge_max_error": merge_error, "repeat_adapter_hash_equal": repeat_hash_equal,
        "base_intact": base_intact, "python_socket_connections_blocked": True, "network_attempts": len(network_attempts),
        "dependency_lock_sha256": digest(lock_path), "environment_bytes": environment_bytes, "run_bytes": run_bytes,
        "worker_sha256": digest(Path(__file__)), "wheelhouse_bytes": wheelhouse_bytes,
        "development_bytes_before_report": development_bytes,
        "peak_working_set_bytes": getattr(memory, "peak_wset", memory.rss),
        "elapsed_seconds": round(time.monotonic() - started, 3), "python": sys.version, "packages": packages,
        "base_manifest_sha256": config["BaseManifestSha256"], "job_manifest_sha256": digest(output / "job" / "job.json"),
        "artifacts": [{"path": str(p.relative_to(output)), "bytes": p.stat().st_size, "sha256": digest(p)}
                      for p in sorted(output.rglob("*")) if p.is_file()],
        "limits": ["Synthetic repeated response measures pipeline mechanics, not assistant quality.",
                   "Production base, QLoRA GPU acceptance, Ollama/llama.cpp import and cross-device portability remain pending."]}
    write_json(output / "report.json", report)
    print(json.dumps({key: report[key] for key in ("passed", "baseline_loss", "candidate_loss", "elapsed_seconds")}))
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
