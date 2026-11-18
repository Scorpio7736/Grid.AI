# Grid.AI

**A local-first AI platform for conversations, software development, and controlled computer assistance.**

Grid.AI is an offline-capable, multi-agent system built with **C# and .NET**. It combines conversational AI, specialized agents, local computer tools, persistent memory, and a simulation-based environment into a single terminal-first application.

The project is designed around a straightforward principle: **AI should be able to help you do real work on your own computer without requiring cloud inference or giving a language model unrestricted control of the machine.**

Grid.AI originated as **GRID7736**. Some source identifiers, commands, filenames, and packaging conventions retain the original name for compatibility.

## Overview

Grid.AI connects a shared local language model with six specialized system agents. These agents work through a coordinated task system that can inspect registered projects, prepare plans, review files, run approved development operations, report system information, and preserve information across conversations and application restarts.

Unlike a simple local chatbot, Grid.AI includes an execution layer, identity and permissions, persistent task records, and a dedicated interface for reviewing actions before they occur.

### Key features

- **Local AI inference:** conversations use a bundled local model rather than a hosted AI API. The current bundled inference path uses Qwen3 4B and a managed local Ollama runtime.
- **Specialized agents:** multiple agents with distinct responsibilities, conversation histories, and access boundaries.
- **Software development assistance:** repository inspection and search, build/test workflows, recorded-error diagnosis, reviewed source edits, and verification.
- **Persistent memory:** conversation recall and explicit memory save, correction, listing, and forgetting controls, scoped by agent and project.
- **Multi-agent conversations:** shared meetings, directed messages, and coordinated specialist task planning.
- **Computer integration:** read-only system discovery and explicitly approved, target-bound local actions.
- **Task orchestration:** proposals, human approval, execution, audit records, cancellation, retries where supported, and saved results.
- **Offline-first operation:** the installed AI runtime and project data can operate locally, including in prepared portable deployments.
- **Terminal-first interface:** interactive command-line operation, dedicated conversation windows, and supported offline voice features.

## Meet the agents

| Agent | Role | What it helps with |
| --- | --- | --- |
| **MCP** | Coordinator | Interprets requests, coordinates specialists, and prepares reviewed task plans. |
| **MAXIS** | Software development | Inspects registered code workspaces, builds and tests projects, and assists with source repairs. |
| **SENTINEL** | Security | Reports supported Windows security and protection observations without autonomously changing security settings. |
| **JN** | Storage and files | Inventories registered storage, identifies possible duplicates, and prepares cleanup proposals. |
| **VOLT** | Resources and performance | Reports host telemetry and manages supported simulation pacing controls. |
| **RELAY** | Communications | Maintains local notifications, task briefings, acknowledgements, and reviewed speech delivery. |

**MCP coordinates the work; permissions and execution remain controlled by the host.** Agent messages and model-generated plans do not themselves authorize operations.

## How Grid.AI works

Grid.AI is more than a model interface. Its services combine conversational processing, controlled task execution, local persistence, and a simulated environment.

```text
                       USER
                        |
                Grid.AI Terminal
                        |
              Conversations / MCP
                        |
                 Local AI Model
                        |
              Reviewed Task System
                        |
       +----------------+----------------+
       |                |                |
   Specialist       Host Tools       Persistence
     Agents         & Services         / History
       |                |                |
       +----------------+----------------+
                        |
              Local Computer / World
```

The implementation is organized around .NET components such as **Grid.Agent**, **Grid.Simulation**, **Grid.Persistence**, and **Grid.Terminal**, with shared types and services supporting identities, conversations, the simulation engine, and task handling. SQLite stores the local world and supported persistent state.

The language model assists with interpretation and content generation, while the host application validates requests, applies access restrictions, checks approval state, and records outcomes.

## Development with MAXIS

A central goal of Grid.AI is to provide useful AI coding assistance directly on a local machine.

For supported, registered workspaces, the existing reviewed development flow can:

1. Inspect repository contents and applicable project instructions.
2. Search supported source files and prepare build or test tasks.
3. Capture actual diagnostic evidence from a recorded failed build or test.
4. Propose a bounded source-file repair using the local model.
5. Display the complete proposed change for human review.
6. Apply an explicitly approved edit, then run separately approved verification.

The current workflow is intentionally bounded. It is **not** an unrestricted autonomous coding agent or an arbitrary shell executor. Workspaces and supported file operations have explicit scopes, and consequential steps require review.

For implementation details, see [Local Coding Integration](MD%20Project%20Files/Project_GRID_Local_Coding_Integration.md) and [Conversation Intent Routing](MD%20Project%20Files/Project_GRID_Conversation_Intent_Routing.md).

## Memory and collaboration

Grid.AI distinguishes private conversations, shared agent meetings, explicitly saved memories, and automatically retrieved conversational context.

Supported recall is scoped to authorized agents and projects; meetings use their own shared conversation boundaries. Memory controls allow information to be reviewed, corrected, or excluded from future recall. Conversations and task histories persist locally across restarts when saved to the world.

The goal is to make an assistant more useful over time without treating conversational context as permission to read files or execute work.

## Local computer access and safety

Grid.AI's agents operate through a host-controlled authorization system. The system distinguishes between **suggesting an action**, **approving that action**, and **executing it**.

Supported local capabilities include registered-workspace file access, development builds and tests, read-only security and resource observations, and selected device actions such as launching an explicitly selected executable, focusing a window, inspecting a selected window title, and creating a folder.

Important safeguards include scoped registrations and access grants, original-host restrictions for sensitive operations, immutable task records, explicit approvals, target and file-hash checks, saved audit trails, and revalidation before execution.

These controls reduce accidental or unauthorized actions, but they are not an operating-system security sandbox: approved builds and tests may execute trusted project code with the current user's permissions. Grid.AI does not claim unrestricted or risk-free control of Windows.

See [Device Integration](MD%20Project%20Files/Project_GRID_Device_Integration.md) and [Integration Acceptance](MD%20Project%20Files/Project_GRID_Integration_Acceptance.md).

## Getting started

### Requirements

Grid.AI currently targets **Windows x64** and uses a local .NET runtime, bundled inference dependencies, and persistent local storage. A fresh installation needs internet access to download pinned dependencies; everyday model inference does not require a cloud service after setup.

The guided Lite mode uses conservative eligibility checks, including at least 8 GB of available RAM. Actual resource requirements depend on the model, runtime, and deployment configuration. The fresh-clone installer requests at least 20 GB of free staging space.

### Install a fresh instance

Clone the repository into a direct child of the standard repositories folder, then open **64-bit PowerShell** in its root. The explicit destination prevents Git or an IDE from introducing an owner folder such as `Scorpio7736\Grid.AI`:

```powershell
$gridReposRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'source\repos'
New-Item -ItemType Directory -Path $gridReposRoot -Force | Out-Null
Set-Location -LiteralPath $gridReposRoot
git clone https://github.com/Scorpio7736/Grid.AI.git 'Grid AI'
Set-Location -LiteralPath (Join-Path $gridReposRoot 'Grid AI')
.\Setup-GridClone.ps1 -Plan -Download
.\Setup-GridClone.ps1 -Download
.\Start-GridClone.ps1
```

The resulting checkout is `%USERPROFILE%\source\repos\Grid AI`. When cloning through Visual Studio instead, set the Clone Repository **Path** to that exact location before starting the clone. Visual Studio owns its default clone-location setting; repository contents cannot override a different path selected by the IDE.

The setup preview is read-only. The explicit installer downloads and verifies the pinned dependencies, builds the application, and checks real local-model conversation readiness.

The launcher uses `.grid-data/world.db` for this clone's separate local world and history. **A fresh clone does not import or erase an existing Grid.AI instance.**

For incomplete runtime installations, close the running application and consult the installer guide before using the explicit `-RepairRuntime` option. Installation assets and model weights are not committed to Git.

Full details: [Fresh Clone Installation](MD%20Project%20Files/Project_GRID_Fresh_Clone_Installation.md).

### Optional diagnostic and profile tools

The terminal executable also offers a guided profile setup and a read-only diagnostic mode:

```powershell
Grid.Terminal.exe --setup
Grid.Terminal.exe --doctor
```

Run these from a deployment containing the executable. Guided configuration is still under development, and not all first-run or hardware acceptance scenarios have been completed. See [Guided Setup](MD%20Project%20Files/Project_GRID_Guided_Setup.md).

## Project direction

Grid.AI is an **actively developed project**, not a finished general-purpose desktop automation product. Its primary focus is reliable offline AI, coordinated agent workflows, persistent local state, and verified computer assistance.

The terminal remains the main interface while core functionality is developed. A graphical interface is a later, optional milestone. Broader autonomous coding, production-model validation, and certain hardware/portable-deployment acceptance scenarios are not yet complete.

Development plans, milestone evidence, test reports, and known limitations are maintained separately so this README stays focused on explaining the product.

## Documentation

- [Current development handoff](MD%20Project%20Files/agent%20context/CURRENT_HANDOFF.md)
- [Local coding workflow](MD%20Project%20Files/Project_GRID_Local_Coding_Integration.md)
- [Agent integration and acceptance](MD%20Project%20Files/Project_GRID_Integration_Acceptance.md)
- [Conversation intent routing](MD%20Project%20Files/Project_GRID_Conversation_Intent_Routing.md)
- [Device integration](MD%20Project%20Files/Project_GRID_Device_Integration.md)
- [Installation and runtime setup](MD%20Project%20Files/Project_GRID_Fresh_Clone_Installation.md)
- [Guided configuration and diagnostics](MD%20Project%20Files/Project_GRID_Guided_Setup.md)
- [Offline portable deployment](MD%20Project%20Files/Project_GRID_Offline_Portable_Deployment.md)

## Repository

**Project:** Grid.AI  
**Original project name:** GRID7736  
**Technology:** C# / .NET, SQLite, local AI inference  
**Primary platform:** Windows x64  
**Repository:** [github.com/Scorpio7736/Grid.AI](https://github.com/Scorpio7736/Grid.AI)

---

*Grid.AI — local intelligence, coordinated agents, and real work under your control.*
