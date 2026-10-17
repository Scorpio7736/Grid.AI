# Project GRID — Master Overview

**Project:** GRID — Hybrid Digital Civilization and Autonomous PC Agent System  
**Document type:** High-level project overview  
**Version:** 1.0 (consolidated overview)  
**Status:** Design and planning; features described here are not yet implemented  
**Source:** The six GRID design documents provided with this overview

---

## 1. Project vision

**GRID** is a TRON-inspired digital civilization that exists locally on a Windows computer. It is simultaneously a simulated society—with districts, inhabitants, jobs, relationships, resources, and history—and an interface for useful computer tasks carried out by carefully permissioned system programs.

Three defining principles:

1. **Autonomous life:** Programs act, work, interact, and develop their own histories rather than waiting for every action to be scripted by the User.
2. **Host-connected world:** Selected, approved Windows activity (CPU, RAM, disk, network, development activity, and security status) influences fictional world events without giving the civilization control over unrestricted host resources.
3. **Powered existence:** Simulation time advances only while GRID runs. Shutdown suspends the world; restart resumes saved state without simulating time while the machine was off.

GRID is not intended to replace Windows, nor is it merely a screensaver, chatbot, or animated task manager.

## 2. The civilization and its districts

The initial simulation is centered on five functional districts:

| District | Real-world input | Fictional function |
|---|---|---|
| The Core | General host status | Reactor, central administration, energy distribution |
| Processing Sector | CPU load | Industry, manufacturing, research |
| Memory Banks | Memory activity and pressure | Research, active storage, resource allocation |
| Archive Sector | Disk statistics | History, records, long-term storage |
| Gateway | Network activity | Communication, trade, transit |

Further development can add administrative, security, residential, engineering, energy, archives, and communications neighborhoods. The world includes a fictional resource economy and citizens who can move, recharge, perform work, exchange resources, form relationships, and initiate construction or other civic changes. Meaningful events become part of a durable history.

**Important boundary:** Fictional energy shortages or resource decisions never directly remove resources from your real computer.

## 3. Programs and autonomous agents

GRID distinguishes two kinds of inhabitant:

- **Civilization programs:** Digital citizens acting wholly within the simulation, without Windows privileges.
- **System programs:** Recognizable in-world characters linked to narrowly authorized real Windows services and tasks.

The initial named agents are:

| Agent | Responsibility |
|---|---|
| **ARCHON** | Administration, task coordination, briefings, permission requests |
| **SENTINEL** | Read-only security monitoring, alerts, incident reporting |
| **FORGE** | Approved software builds, unit tests, repository diagnostics |
| **ARCHIVIST** | GRID storage management, approved file inventories, cleanup and expansion proposals |
| **VOLT** | PC resource monitoring and GRID performance throttling |
| **RELAY** | Text, voice, messaging, notifications, transmissions |

Agents have stable identities, roles, memories, personalities, and potentially relationships. Their personalities affect communication, not their permissions. The proposed first useful agent workflow is: **ask ARCHON to assign FORGE an approved unit-test run and receive the verified result**.

## 4. Communication and interaction

All interaction uses a **unified communications hub** shared by:

- A `grid` terminal interface in Windows Terminal/PowerShell.
- Microphone input transcribed locally to text.
- Local text-to-speech for spoken agent responses.
- Eventually, conversation overlays and program selection inside the 3D city.

The system preserves conversations, messages, relevant agent memories, task updates, and event history in local storage. A conversation may begin in the terminal and continue through voice or the 3D viewer. Optional incoming transmissions can notify the User of significant changes.

Commands are handled deterministically; conversational AI can interpret questions and propose plans, but **never becomes the authoritative source of world state or gains the right to execute arbitrary commands**.

## 5. Real PC tasks and safety

System agents act through explicitly registered capabilities, bounded parameters, agent-specific permissions, and approval checks. The system tracks task state, results, failures, and audit history. Examples include approved repository tests, Defender status checks, resource readings, and preparation of file-organization proposals.

Key constraints:

- ARCHON coordinates agents but does not inherit all of their privileges.
- Untrusted model output must never be executed directly as PowerShell, SQL, or other system commands.
- No unrestricted access to private files, silent deletion, or self-elevation of privileges.
- Sensitive changes require explicit User authorization.
- GRID should yield CPU, GPU, and memory resources to normal PC use.

## 6. Local-first and free operation

**Hard requirement — GRID-REQ-001:** Core GRID functionality must be able to operate locally on the existing Windows machine, without required paid APIs, cloud hosting, or subscriptions. Essential functions should remain available offline after necessary downloads.

Proposed no-subscription technology choices:

| Function | Technology |
|---|---|
| Simulation and Windows applications | C# / .NET |
| Native/performance-sensitive components | C++ |
| AI experimentation and analysis | Python |
| World and message persistence | SQLite / SQL |
| 3D presentation | Godot (.NET edition) |
| Administrative desktop UI | WPF |
| Local model runtime | Ollama with a suitably licensed model |
| Speech recognition | whisper.cpp |
| Speech synthesis | Piper TTS; optionally Windows speech voices |
| Testing and version control | xUnit, Git |

Most citizens should use inexpensive deterministic or utility-based behavior. Selected agents share **one on-demand local language-model service** rather than running a model for every citizen. Electricity, hardware limits, disk use, and development time remain real costs.

## 7. Storage and growth

The recommended initial GRID storage **quota is 20 GB**, not a preallocated 20 GB partition or file. Installed models, 3D assets, databases, logs, and snapshots share that budget; actual size will depend on implementation choices.

A planning-only example allocates **5 GB for AI/voice models, 4 GB for 3D assets, 2 GB for software, 3 GB for civilization data and backups, and 6 GB as reserve**.

**ARCHIVIST → ARCHON → User** is the expansion path:

1. ARCHIVIST measures usage and forecasts capacity needs.
2. ARCHIVIST proposes cleanup or a larger quota.
3. ARCHON verifies policy and host free-space constraints and requests approval.
4. The User approves or denies a specific increase (a suggested increment is 10 GB).
5. A deterministic controller changes the logical quota and audits the decision.

An illustrative 20 GB deployment could store thousands of compact program records and long histories, but the number of *concurrently active intelligent programs* depends much more on CPU, RAM, GPU, AI model size, and simulation design. Avoid retaining raw microphone audio by default.

## 8. City architecture — Digital European Classicism

The preferred city is **a real-feeling, lived-in European city that happens to be digital**. Its everyday streets take inspiration from London and Russian/Slavic/Eastern European masonry urbanism, including Saint Petersburg-like classical civic architecture. Soviet monumental influences are reserved chiefly for selected institutional landmarks, not every building.

The dominant qualities are quiet order, dignity, human scale, and coherent urban blocks:

- Most everyday buildings are approximately **3–7 stories** tall, with stone or brick facades, cornices, tall windows, street-facing doors, shops, and courtyards.
- Limestone, granite, marble accents, wrought-iron-inspired trim, and practical paving dominate rather than glass megatowers.
- Public headquarters, archives, bridges, domes, and civic squares provide occasional moments of grandeur.
- TRON-inspired cyan or amber light is **inlaid into paving, stone joints, building access points, and infrastructure** instead of coating everything in neon.
- Districts visibly serve their inhabitants' jobs and the simulation's systems.

The city's civilian neighborhoods must feel inhabited, not like empty ceremonial film sets.

## 9. Permanent environment — Eternal Autumn Evening

**GRID-ENV-001** establishes an unchanging visual environment:

| Feature | Direction |
|---|---|
| Season | Permanently **mid-autumn** |
| Time of day | Permanently **early evening**, visually around 5–6 PM |
| Sky | Medium-to-heavy gray-blue clouds |
| Weather | The rain has **just stopped**; routine active rainfall is absent |
| Surfaces | Damp stone and wet streets with believable selective puddles |
| Trees | Amber, rust, ochre, olive, and fading green foliage |
| Atmosphere | Light haze, slow cloud motion, gentle wind |
| Lighting | Warm lamps/windows contrasted with restrained digital cyan |

The environmental appearance never moves into winter, midday, or midnight, but clouds, leaves, reflections, street activity, windows, and infrastructure may animate. **Simulation time, the fixed environmental appearance, and real Windows time are three separate concepts.**

*Cross-document clarification:* An earlier city guide entertained future day/night and clear-weather support. The later, explicit environment guide establishes permanent evening, cloud cover, and post-rain conditions as the current intended direction.

## 10. Civilian clothing and officer/agent uniforms

All inhabitants should be **human or human-adjacent**: recognizable, expressive people with believable clothing and a restrained digital identity. Their fashion and architecture share a principle of **real materials plus precisely embedded illuminated seams**.

**Civilians:** The preferred everyday reference is a light-gray waist-length zip jacket with a raised collar, dark utility trousers, black belt, black lace-up boots, and minimal cool-white light tracing. Civilian variants include technicians (amber utility accents), service programs (blue details), and field/maintenance workers (muted teal, weather-resistant layers). Civilians should look approachable and functional, not militarized.

**Officers and major agents:** The approved uniform is a tailored light/medium-gray high-collar jacket or tunic, charcoal/black side panels, broad black belt with simple rectangular buckle, gray trousers tucked into polished tall black boots, and narrow ice-blue or cool-white seams. No hats, ribbon bars, medals, pens, or decorative chest badges. Command variants may use longer formal coats; specialist variants remain distinct while sharing consistent tailoring.

Role lighting may vary subtly: ARCHON (pale gold/white), SENTINEL (blue), FORGE (amber), ARCHIVIST (teal), VOLT (warm yellow), and RELAY (violet/cyan). Rank is not communicated by color alone.

## 11. Software architecture and programming languages

C#/.NET is the authority for simulation state, persistence coordination, permissions, and system services. Python can provide AI and analysis components; C++ can supply native processing where justified. Inter-language requests use explicit contracts and validated responses.

Other considered languages have targeted roles: PowerShell for controlled Windows administration scripts; GDScript for selected Godot scene behavior; TypeScript for a future dashboard; Lua for sandboxed world scripts; Rust or C for specialized native needs; and possibly a custom GRID behavior language in the distant future.

**Architectural boundary:** The Godot viewer observes the C# simulation; it must not create a second authoritative civilization. The headless GRID world must run without the viewer, audio devices, or a local LLM.

## 12. Development roadmap

Build the engine and validate it before investing heavily in visualization:

| Milestone | Deliverable |
|---|---|
| **1. Genesis** | .NET solution, world classes, fixed-step clock, terminal, tests; seed five districts and twenty programs |
| **2. The Core** | SQLite state, snapshots, events, shutdown and restart recovery |
| **3. First Programs** | Autonomous jobs, needs, movement, utility-based decisions |
| **4. Communications** | Persistent terminal dialogue, microphone transcription, spoken replies |
| **5. System Agents** | ARCHON, FORGE, SENTINEL, ARCHIVIST, VOLT, RELAY and scoped host capabilities |
| **6. Intelligence** | Shared locally hosted AI, grounded conversations, validated action proposals |
| **7. The City** | Godot streets, districts, citizens, inspection and interaction |
| **8. Integration** | Startup, quotas, notifications, throttling, backups, crash recovery |

Use one Git repository, short feature branches, xUnit and language-appropriate tests, versioned service contracts, and architecture decision records. The first executable milestone should display the *actual* status of a five-district, twenty-program world in the terminal.

For the **first visual vertical slice**, make one European stone street with wet paving, autumn trees, overcast early-evening lighting, subdued digital seams, a civilian outfit, and an officer uniform before creating an entire city.

## 13. Open decisions and boundaries

The following remain matters for future implementation or testing rather than finalized facts:

- Host computer specifications and appropriate local AI model size.
- Actual storage consumption, population limits, and resource budgets.
- Exact IPC protocol, AI/speech adapter selection, and runtime packaging.
- Detailed agent personalities, authority hierarchy, and individual appearance.
- City topology, building construction mechanics, and level-of-detail strategy.
- Final visual palette and materials after testing in Godot.

**Current design priorities:** Free and local operation; believable autonomous world; user-controlled agent actions and storage; coherent digital-European architecture; permanent rainy-autumn-evening atmosphere; unified civilian/officer clothing language.

---

## 14. Source documents

This overview condenses the six companion design documents. Keep them for implementation-level detail; this file serves as a short entry point.

1. `Project_GRID_Design_Specification(1).md` — Original concept, simulation, agents, communications, persistence and safety.
2. `Project_GRID_Local_First_and_Storage_Addendum(1).md` — Free local architecture, model choices, storage, quotas, and scaling.
3. `Project_GRID_Development_and_Languages_Addendum(1).md` — Development roadmap, initial setup, polyglot architecture and testing.
4. `Project_GRID_City_Design_Style_Guide(1).md` — Digital European Classicism, districts and city construction.
5. `Project_GRID_Program_Fashion_Style_Guide(1).md` — Civilian wear, officer uniforms, materials and styling rules.
6. `Project_GRID_Environment_Atmosphere_Style_Guide(1).md` — Fixed early evening, mid-autumn, post-rain environment and rendering guidance.
