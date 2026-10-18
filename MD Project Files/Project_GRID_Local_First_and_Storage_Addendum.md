# Project GRID — Local-First Architecture and Storage Planning

## GRID-REQ-004 - Persistent hardware-generated worlds (planned, 2026-10-18)

First-time creation must discover motherboard, CPU, GPU, RAM, storage and network hardware locally, generate a unique city from that profile and save the resulting world. Subsequent startups load the saved city and evolving simulation state; they do not regenerate it. The [generation contract](Project_GRID_Design_Specification.md#grid-req-004---hardware-generated-city-contract) defines creation versus recovery.

Persist the stable world ID, creation hardware profile, per-world seed, generator/schema versions, component-to-city associations, generated layout, entities and clock/state together. Keep creation provenance separate from later host observations. Backups and migrations must preserve that provenance and the actual world, not just enough inputs to generate a new approximation.

Hardware changes, unavailable devices or a changed driver must not invalidate an existing civilization. Loading a saved world must not require successful current hardware discovery. Unknown or inaccessible hardware during first creation needs an explicit recorded fallback policy; corrupted or inaccessible storage must never be mistaken for a first launch.

Initialization must commit atomically and avoid competing creation by simultaneous processes. On disk-full or interrupted-write failure, preserve existing saves and report an incomplete creation; do not advertise persistence success. Durable completion must precede treating the world as initialized.

Discovery and storage remain local-first and need no paid API or AI model. Network hardware discovery is local adapter inventory, not network scanning. Retain only fields required by generation; avoid raw serial/MAC identifiers. The 20 GB budget remains a planning target; cap generated detail and include layout/provenance data in future quota accounting.

**Current status:** the application still creates a fresh in-memory Genesis world per run. Grid.Persistence exists as a scaffold only. No discovery, city generator, SQLite persistence or saved-world reuse is implemented. Phase 2 checklist 2.6 adds these requirements without changing historical Genesis completion.

## Implementation status - 2026-10-17

Current execution is an in-memory .NET terminal with five districts and twenty citizens. Every launch creates a fresh world. SQLite, save/load, durable audit, model downloads, quota enforcement and storage governance remain planned. The 20 GB budget and larger populations below are estimates, not measured installation sizes or enforced limits. LastTick retains only the latest tick result; edit receipts are returned to callers and are not stored history. Checklist 1.10 plans a bounded recent-events buffer before Phase 2 persistence. Persisted program revisions must not make previews from an old world/session reusable.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Design addendum:** v0.5  
**Date:** October 15, 2026  
**Purpose:** Consolidate design decisions and estimates discussed *after* the original `Project_GRID_Design_Specification.md` was generated. This is a companion document rather than a replacement.

> **Status legend:** **Requirement** = requested constraint; **Recommendation** = proposed design; **Estimate** = rough planning figure requiring measurement; **Example** = hypothetical scenario, not observed data.

## 1. New Primary Requirement: Minimal Cost

**Requirement — GRID-REQ-001: Local-first, zero-subscription operation**

Project GRID should run entirely on the user's existing Windows machine without depending on paid APIs, recurring subscriptions, or cloud hosting. The developer is willing to do additional implementation, setup, and maintenance to make the system free to operate using locally installed software.

### Acceptance criteria

- The core civilization, simulated agents, task scheduler, storage, terminal interface, and persistent history operate locally.
- Conversational AI, speech-to-text, and text-to-speech have fully local deployment options.
- After initial software/model downloads, essential operation can continue without internet access.
- Cloud integrations, if offered later, remain optional, off by default, and separate from core functionality.
- The simulation continues to function when local AI is unavailable: deterministic tasks, data access, and rule-based agent behavior still work.
- Resource consumption is bounded so ordinary PC use has priority.
- Per-model licenses are checked before distribution or integration; “free to download” does not automatically imply unlimited reuse rights.

**Cost expectation:** $0/month in required subscriptions or hosted infrastructure on existing hardware. **Not literally cost-free:** electricity, hardware wear, local disk usage, optional hardware upgrades, and developer time still apply.

## 2. Proposed Free / Local Technology Stack

| Subsystem | Proposed choice | Rationale / caveat |
|---|---|---|
| Simulation core and agents | C# / .NET | Existing programming skill set; deterministic local services |
| 3D city and visualization | Godot (C#/.NET-enabled edition) | No engine royalties; independent viewer |
| Windows administration UI | WPF | Familiar desktop controls and diagnostics |
| Persistence | SQLite | Embedded, transactional local storage |
| Local language model runtime | Ollama | Shared on-device inference; verify selected model license |
| Speech recognition | whisper.cpp | Locally transcribes microphone audio |
| Speech synthesis | Piper TTS | Local neural voices; verify voice/model licenses |
| Windows metrics and status | Windows/.NET APIs | Read-only telemetry wherever possible |
| Process-to-process communication | .NET named pipes | Local service-to-terminal/viewer interface |
| Testing | xUnit | Unit and integration tests |
| Source management | Git | Version-controlled development |

These are **choices for implementation**, not mandatory dependencies. Prefer interfaces (`IDialogueService`, `ISpeechRecognitionService`, `ISpeechSynthesisService`) so providers can be exchanged later.

### Important storage-location detail

Tools such as Ollama can keep downloaded model weights in their **own caches outside the GRID folder**. The storage monitor must either include those locations in its accounting or configure model storage locations consistently. Likewise, developer SDKs and editable Godot projects may occupy additional space outside a packaged production installation.

## 3. Resource-Efficient Local AI Architecture

**Recommendation:** Many citizens, one shared local AI service—not one model process per citizen.

Three tiers of behavior:

1. **Deterministic systems (very cheap):** pathfinding, job routines, world clock, economy, resources, scheduled scans, authorization checks.
2. **Utility-based decisions (cheap):** goal selection, prioritization, resource allocation, relationships, routine task selection.
3. **On-demand language-model inference (most expensive):** complex dialogue, summaries, interpretations of natural-language requests, bounded task planning.

The language model **must not be the source of truth** for Windows state or Grid state. Concrete facts come from monitored telemetry, the SQLite database, and validated tool results. AI-generated plans should be converted to typed, permission-checked operations before execution.

### Local runtime behavior

- Run a single shared model or shared inference queue for ARCHON and specialist programs.
- Keep model inference idle when no conversation or reasoning task requires it; unload idle models if memory pressure warrants.
- Queue or defer expensive requests when CPU/GPU/RAM are needed for ordinary PC activity.
- Simulate most citizens with rules; reserve richer AI for selected system programs or conversations.
- Stop rendering when the 3D viewer is closed without stopping the low-resource simulation.
- When the PC is powered off, the world is suspended; no offline catch-up simulation is required.

## 4. Hardware Guidance — Approximate, Not Guarantees

The following are rough planning categories. Actual speed depends on CPU architecture, VRAM, RAM bandwidth, inference model size/quantization, 3D detail, and concurrency.

| Hardware category | Expected design compromise |
|---|---|
| 8 GB RAM, integrated graphics | Rule-driven simulation, very small local AI model, simple viewer |
| 16 GB RAM, modest GPU | Small quantized language model with constrained concurrency |
| 32 GB RAM, roughly 8–12 GB VRAM | More practical simultaneous local AI and 3D tasks |
| 64 GB RAM, 16+ GB VRAM | Potential for larger models and more demanding workloads |

**Decision pending:** Gather host CPU, total RAM, GPU model, VRAM, disk size/free space, and acceptable background resource limits before choosing a default local model.

## 5. Base Installation Size — Recommended 20 GB Quota

**Recommendation:** Start with a **20 GB Grid storage quota**, not a 20 GB preallocated disk partition or file. Actual bytes should be consumed only when needed.

**Estimate:** A very basic engine without a downloaded LLM might use roughly 1–3 GB depending on packaged runtime and assets. Adding a small quantized model, offline voice components, and a moderate 3D world could make a 20 GB budget practical.

Illustrative baseline component ranges (not measurements):

| Component | Rough planning range |
|---|---:|
| .NET simulator and supporting services | 200–500 MB |
| Godot world/3D assets | 300 MB–1 GB |
| SQLite data at initial launch | 50–200 MB |
| Small quantized LLM | 2–5 GB |
| Speech recognition model/components | 100–500 MB |
| Local TTS model/components | 100–500 MB |
| Logs, working state, snapshots | 500 MB–2 GB |
| Runtime overhead / spare capacity | 2–5 GB |

These ranges are **not additive fixed requirements**. Installed runtimes, development artifacts, models, and duplicates can change the total significantly.

### One 20 GB budgeting illustration

| Category | Budget |
|---|---:|
| Local AI plus speech models | 5 GB |
| 3D city, models, textures | 4 GB |
| Engine and packaged software | 2 GB |
| Civilization state, event history, snapshots, backups | 3 GB |
| Unallocated growth allowance | 6 GB |
| **Total** | **20 GB** |

**Alternative illustration previously discussed:** 5 GB AI/voice, 4 GB engine/assets, 3 GB data/working space, and 8 GB unallocated reserve. Both are merely illustrative ways to partition the *same* 20 GB budget; they are not separate additional requirements.

## 6. Storage Allocation Governance

**Requirement / proposed policy:** ARCHIVIST manages storage planning, but cannot authorize more capacity on its own. ARCHON coordinates decisions and requests permission from the User.

### Responsibilities

- **ARCHIVIST (data management):** track actual Grid usage, growth rates, model cache usage, event history, snapshots and logs; estimate future need; identify safe cleanup candidates; prepare expansion or reclamation proposal.
- **ARCHON (coordinator):** review ARCHIVIST’s proposal, verify host disk free space and system policies, aggregate recommendations, and ask the User to approve or deny.
- **USER (owner):** sole authority for quota increases and sensitive deletion or system-level storage changes.
- **Storage controller (deterministic subsystem):** enforce quotas, minimum host free space, approved changes, and audit history; agents do not bypass it.

### Recommended alert thresholds

| Grid quota usage | Response |
|---|---|
| Below 60% | Normal |
| 60–75% | Track growth trends |
| 75–85% | Prepare proposal |
| 85–95% | Priority approval request |
| Greater than 95% | Restrict nonessential writes, preserve critical snapshots and recovery capacity |

Thresholds apply to **Grid-attributed storage**, not to the entire SSD. The host drive must also satisfy a separate minimum-free-space rule.

### Expansion workflow

1. ARCHIVIST measures used bytes, quota, recent rate of growth, and reclaimable bytes.
2. When a threshold or forecast is met, ARCHIVIST prepares a report with measurement period, confidence, and alternatives.
3. ARCHON validates the host’s available disk space and the policy minimum reserve.
4. ARCHON sends a terminal/dashboard notification and, if enabled, a spoken request through RELAY.
5. User approves an explicit amount, denies it, or chooses cleanup.
6. Storage controller records the decision and changes only the **logical Grid quota**—not disk partitions.
7. ARCHIVIST verifies the result and updates projections.

### Example approval scenario (hypothetical)

- Initial allocation: **20 GB**
- Current usage: **16.8 GB** (84%)
- Requested allocation: **30 GB**
- Increase: **10 GB**
- Forecast cited by ARCHIVIST: usage may exceed 90% in approximately 14 days **if supported by actual observed growth**.

Potential terminal commands:

```powershell
grid storage status
grid storage report
grid storage approve 10GB
grid storage deny
```

Commands are proposed syntax; these tools have **not** yet been implemented. A real system should identify a unique pending proposal ID to prevent accidentally approving the wrong request.

### Sample proposed policy

```json
{
  "initialQuotaGB": 20,
  "warningThreshold": 0.75,
  "requestThreshold": 0.85,
  "criticalThreshold": 0.95,
  "expansionIncrementGB": 10,
  "requireUserApproval": true,
  "minimumHostFreeSpaceGB": 25,
  "storeRawAudio": false,
  "retainConversationText": true,
  "snapshotRetentionCount": 5,
  "automaticLogRotation": true
}
```

**Implementation notes:** thresholds should use a consistently defined GB/GiB unit; projections should use measured bytes/day; transaction-safe backups, write-ahead logs, and recovery snapshots need explicit quota accounting. Reserve enough space for critical saves even when optional event logging is suspended.

## 7. Storage Directory Layout (Conceptual)

```text
C:\ProgramData\ProjectGRID\
├── Runtime\
├── World\
│   ├── world.db
│   └── world-events.db
├── Programs\
│   ├── identities.db
│   └── memories.db
├── Communications\
│   └── conversations.db
├── Tasks\
│   └── tasks.db
├── Models\
│   ├── Language\
│   ├── SpeechToText\
│   └── TextToSpeech\
├── Snapshots\
├── Logs\
└── Config\
    └── storage-policy.json
```

Configure restrictive filesystem access permissions and separate per-user preferences where appropriate. Model caches outside this directory must be accounted for. Avoid saving raw speech by default; persist transcripts unless the user opts out.

## 8. How Big a Civilization Might Fit Within 20 GB?

**Key distinction:** Storage determines how much the Grid can retain (assets, agent records, history). **CPU/GPU/RAM determine how many detailed behaviors, conversations and rendered characters can operate simultaneously.** Therefore a population estimate is not a guarantee of real-time performance.

### Scale illustrations

| Approximate number of simulated citizens | Experience | Storage interpretation |
|---|---|---|
| 20–50 | Small digital outpost; you recognize individuals | Easily accommodated in compact structured data |
| 100–500 | Town with recurring jobs and relationships | Typically easy for storage |
| 1,000–5,000 | City with multiple districts and departments | Plausible with compact agent records and shared assets |
| 10,000+ | Large civilization with distinct economies | Plausible on disk; efficient scheduling and simulation level-of-detail required |

One conceptual 20 GB city could feature:

- Thousands of simulated citizens with persistent identities and compressed memories.
- Five or more districts with buildings and transit routes.
- Six main system agents: **ARCHON, SENTINEL, ARCHIVIST, FORGE, VOLT, RELAY**.
- A shared local language model and separate voice components.
- A 3D environment using reusable assets, materials, and animations.
- Persistent conversation and city event histories spanning long periods, provided retention is managed.

**Important:** 10,000 citizens does **not** mean 10,000 concurrent LLM instances. Most citizens should use deterministic and utility-based AI; detailed AI should be scheduled selectively.

### Why storage stretches surprisingly far

- Citizens can share 3D models, meshes, textures, and animations.
- Citizens store small records: IDs, roles, position, goals, needs, relationships, and recent significant memories.
- The Grid need not store every footstep or routine tick as a permanent event.
- Events and conversations can be compressed, aggregated, or summarized.
- Only selected agents require costly long-form language-model context.

## 9. Event History Growth Example

One intentionally simple illustration from the discussion:

- **10,000 significant events/day**
- **~1 KB stored per event**, assumed *inclusive* of estimated metadata/index allowance
- **~10 MB/day**
- **~3.65 GB/year** (decimal units)
- **3 GB reserved for history ≈ 300 days** at that assumed rate

This is arithmetic based on illustrative assumptions, **not measured Grid performance**. Actual bytes/event will vary with indexes, SQLite page sizes, retention, compression, full-text contents, and event format. Backups or separate snapshots can substantially increase usage.

**Recommended history strategy:** store meaningful actions (e.g., journey started/completed, building constructed, task failed, relationship changed), but avoid recording every animation frame or routine step. Periodically summarize older lower-priority events while keeping critical audit logs and meaningful program memories.

## 10. Proposed Starting Civilization

**Recommendation for Genesis (early usable version):**

| Parameter | Starting target |
|---|---|
| Initial storage quota | 20 GB |
| Default requested expansion | +10 GB, user approval required |
| Starting population | ~100 simulated citizens, scaled to hardware |
| Key system agents | ARCHON, SENTINEL, ARCHIVIST, FORGE, VOLT, RELAY |
| City layout | Five interconnected districts |
| Language models | One shared local inference service, optional when idle |
| Persistence | SQLite with snapshots, audit/event logs |
| Audio retention | Off by default; keep text transcript |
| Offline behavior | Entirely local once installed |
| Viewer | Optional; closing it does not stop the headless simulation |

Start smaller during implementation (e.g., 20 citizens) before scaling to 100, then thousands. Use measured CPU, RAM, GPU, and disk consumption to decide expansion pace.

## 11. Project Roadmap Implications

A low-cost development sequence was recommended:

1. **Simulation core (C#)** — time, jobs, economy, identities, and agent decisions.
2. **Terminal communications** — inspect the world and converse with programs.
3. **SQLite persistence** — history, tasks, memories, snapshots.
4. **System agent framework** — permission-controlled tasks and reports.
5. **Local AI** — shared Ollama-hosted model for richer dialogue, not mandatory for core operations.
6. **Offline voice** — whisper.cpp and Piper with provider-swappable interfaces.
7. **Godot 3D viewer** — city visualization layered on the already-running simulation.
8. **ARCHIVIST storage governance** — quotas, forecasts, reclaim recommendations, and ARCHON/user approval.

In implementation, the basic storage quota guardrails and crash-safe persistence should be built **early**, alongside step 3; the sophisticated ARCHIVIST negotiation workflow can mature later.

## 12. Decisions and Open Questions

### Current requirements / agreed direction

- Free/local-first is preferred over paid managed services.
- Favor extra development work over recurring provider bills.
- Begin with a **20 GB logical storage allocation**.
- Agent-initiated storage expansion requires a chain of approval: **ARCHIVIST → ARCHON → User**.
- Preserve a minimum amount of free host disk space.
- Programs provide progress updates and can speak to the user through local TTS.
- The Grid should scale its civilization by shared assets and mostly inexpensive AI behaviors.
- The Grid's virtual clock pauses when the PC is powered off.

### Still to decide or measure

- Specific CPU, RAM, GPU/VRAM, and SSD capacity of the host PC.
- Which quantized LLM and voice models meet performance and licensing goals.
- Hard cap for headless Grid CPU/RAM usage when gaming or compiling.
- Exact rules for automatic safe cleanup and retention of audit records.
- Whether model weights and developer toolchains are inside or outside the 20 GB quota.
- Whether the initial 20 GB quota is per-user, per-installation, or per-world.
- Host free-space policy for machines with smaller system drives.
- Backup policy (local versus external drive), including space outside the active quota.

## 13. Bottom Line

A **20 GB initial budget is a reasonable starting point for a local-first Grid** with a small shared language model, offline speech services, persistent databases, a modest 3D city, and room for growth. It could store records for thousands of citizens and a substantial amount of history, but the **number simultaneously simulated with detailed AI** will depend primarily on host hardware and optimization.

Treat **20 GB as a configurable logical quota, not disk space consumed immediately**. ARCHIVIST should track actual usage and propose cleanup or an increase; ARCHON should present the evidence and obtain explicit user approval before the storage controller changes that quota.

---

*This addendum summarizes design conversations since the original Project GRID Markdown specification. Quantitative estimates are intentionally preliminary and should be replaced by measured prototype results.*
