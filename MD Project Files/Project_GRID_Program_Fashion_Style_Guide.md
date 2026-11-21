# Project GRID — Program Fashion & Uniform Style Guide

## Installation editions and rendering - planned 2026-10-20

Installation will offer **PC GRID** (permanent machine installation, with advanced 3D options) or **Flash GRID** (mobile flash-drive package, with very simple lightweight 3D). The installer must explain the choice, validate the destination and retain the selected edition. Both share simulation/save/identity behavior and must run supported tasks offline after provisioning. Flash GRID keeps managed files with the portable package and requires no persistent host integration; graphics still run on the host PC. Quality differences must not regenerate or change the world.

See [edition details](Project_GRID_Terminal_UX_Portability_and_Offline_Plan.md) and checklist 7.6/8.6. Rendering budgets, packaging and migration remain planned; this addition changes documentation only.

## Program behavior completion - 2026-10-19

**3.1 implemented and verified.** Non-demo residents now have deterministic needs/goals/actions. Applications reflect fresh or unknown observations; explicitly configured Agent/Special residents perform resource-gated simulated occupation work/rest on simulation-time shifts. Schema v5 persists their routines, sourced budgets and progress without restoring live observations or adding offline work. Use behaviors and inspect for last-tick behavior. No OS task execution, movement or named agent services are claimed.

See [Program Needs and Behavior](Project_GRID_Program_Behavior.md) for policy, APIs, limits and completion evidence: **278 Release tests passed**, build zero warnings/errors, disposable terminal smoke passed. Debug output was locked by running user sessions, which were left running. Earlier dated deferred-behavior/schema statements describe previous milestones; existing history and completed scope are preserved.

## Application resident completion - 2026-10-19

**2.7 / 2.7.1 implemented and verified:** production population now follows current-session windowed executable groups through read-only discovery and serialized lifecycle reconciliation. Application residents are transient; Agent/Special residents persist in schema v4. Before production conversion, legacy fictional citizens are preserved in a uniquely named validated archive. City/layout and clock remain intact. Unknown resource requirements display unknown condition; no host-control authority is implied.

Verification: **268 tests passed**, build zero warnings/errors, and a disposable Windows startup/reopen detected nine application groups. See [Application Residents Implementation](Project_GRID_Application_Residents_Implementation.md) for grouping limits, discovery/freshness, storage/migration, editing, resource interpretation and completion evidence. Earlier dated statements that 2.7 is planned, production cities stay empty, or all residents persist describe previous milestones. Agent delegation/services, login, rendering and additional per-application telemetry remain future work.

## Hardware city completion - 2026-10-19

**2.6 / GRID-REQ-004 implemented and verified.** First-time creation discovers motherboard, CPU, GPU, RAM, storage and network hardware, generates bounded district/street/building-lot geometry and saves actual layout and provenance in schema v3. Later startups reuse the saved world without discovery or regeneration. Existing worlds are preserved; new generated cities start empty pending 2.7. Inventory is creation provenance, separate from live resource telemetry and application identity. Rendering, atmosphere, fashion and application-resident integration remain planned.

Verification: **250 tests passed** (14 Core, 106 Simulation, 74 Persistence, 56 Terminal); build had zero warnings/errors. A fresh Windows city and unchanged reopen were verified in a temporary directory. Use city to inspect saved generation details; --world with a new filename explicitly creates a separate world.

See [Hardware City Generation](Project_GRID_Hardware_City_Generation.md) for architecture, mappings, fallbacks, persistence and completion evidence. Earlier dated statements that hardware generation is planned or that normal creation always makes twenty fictional citizens describe previous milestones; this update supersedes those status statements while preserving their history.

## GRID-REQ-005 - Host-connected programs and resources (planned, 2026-10-18)

Preserve the existing civilian/agent wardrobe vocabulary. Ordinary application residents use transient incarnations and shared visual templates; persistent agents/special programs retain individual appearance choices. Clothing and application branding confer no identity privileges. This is planned rendering behavior, not an implemented outfit system.

See [Host programs, lifecycles and resources](Project_GRID_Host_Programs_and_Resources.md) for the shared contract. This planning update changes Markdown only.


## GRID-REQ-004 - Program identity in hardware-generated cities (planned, 2026-10-18)

First-time creation will discover motherboard, CPU, GPU, RAM, storage and network hardware and generate a unique city from those components. That world must be saved and reused on later startups. Hardware discovery and generated-city reuse remain planned; fixed Genesis save/reuse is implemented in 2.2; see the [generation contract](Project_GRID_Design_Specification.md#grid-req-004---hardware-generated-city-contract).

The generated city changes the setting, not the approved wardrobe rules. Preserve civilian coats, formal agent silhouettes, materials and restrained luminous seams. Component-themed districts may inform future occupational accents, but such variants remain art proposals and must stay consistent with the existing uniform vocabulary.

Hardware does not grant security roles, choose an authenticated Master Control identity, or make names/occupations into capabilities. Agent and special-program identities, memberships, edited values and persistent appearance choices survive reload. Ordinary application residents are rediscovered and do not retain saved individual appearance/state; shared application templates may provide a consistent visual vocabulary without resurrecting a resident.

Hardware replacement must not recreate persistent residents or reset their uniforms; live application residents continue to follow observed application lifetimes. Existing fashion/head/eye references and their newer repository organization remain intact. Runtime wardrobe generation and rendering remain deferred; documenting this requirement implements neither.

## Implementation status - 2026-10-17

The program model and validated name/occupation/status/energy/district editing API are implemented; wardrobe rendering and uniform assignment remain planned. Display names, occupations, uniforms, and role colors confer no permissions. Future visual updates should read authoritative snapshots; login and terminal editing controls remain deferred.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Document:** Visual Design Specification  
**Version:** 0.8 — Draft  
**Scope:** Civilian, worker, specialist, officer, and high-level agent clothing  
**Status:** Concept direction approved in discussion; individual details remain adjustable.

---

## 1. Visual identity

Project GRID's inhabitants are human or human-adjacent digital programs living in a city influenced by London and Eastern European streetscapes: restrained masonry buildings, clean stone streets, traditional urban scale, and subtle integrated digital lighting.

**Design statement:** GRID clothing combines believable European-inspired everyday dress and structured military-officer tailoring with precise, embedded luminous seams. Authority is expressed through cut, materials, and restraint—not excessive armor, medals, badges, or bright neon.

All garments should feel wearable, engineered, and shared by a coherent civilization. Even when the glow is disabled, the design must remain visually compelling.

## 2. Common rules

- **Silhouettes:** clear human proportions; tailored but practical; no exaggerated cyberpunk armor.
- **Base palette:** black, charcoal, medium grey, light grey, and limited off-white. Civilian outfits may have slightly more muted fabric variation.
- **Illumination:** thin, inset light channels rather than bulky exterior tubes; light follows garment seams and functional panel divisions.
- **Finish:** matte or subtly textured textiles, occasional semi-rigid technical panels, restrained metallic hardware.
- **Wearability:** plausible closures, pocket placement, articulated knees and elbows, belts, and usable boots.
- **Consistency:** role-specific lines and color accents should coexist with consistent silhouettes and standardized hardware.
- **No requirement for headgear:** hats were expressly rejected for the officer design.
- **No decorative achievement clutter:** officer concepts should exclude badge bars, chest medals, and pens.

## 3. Civilian program wardrobe

### 3.1 Everyday civilian — preferred reference direction

The approved civilian example is an approachable, understated outfit consisting of:

- A **light-grey, waist-length jacket** with a raised collar and front zipper.
- A darker underlayer and **charcoal/black utility-style trousers**, with restrained pocket detailing.
- A narrow black belt, functional construction, and **black lace-up boots**.
- Small, cool-white light seams on jacket edges and selected trouser seams.
- Minimal accessories and no formal rank insignia.

**Front view:** show the jacket's zipped opening, collar construction, cuffs, natural fabric folds, pockets, trouser silhouette, and boots. Illumination is minimal and directional.

**Back view:** maintain the exact same jacket, sleeves, and trouser design; use a single clean vertical back light channel and limited sleeve or leg accents, not a complex circuit pattern.

**Tone:** normal daily life in a beautiful, orderly digital European city—not a soldier out of uniform.

### 3.2 Civilian-associated occupational variations

| Variant | Clothing direction | Suggested linework | Main practical features |
|---|---|---|---|
| **Civilian / everyday** | Light-grey zip jacket, dark trousers, practical boots | Cool-white, sparse | Standard pockets; lightweight fabric |
| **Technician** | Work jacket, cargo trousers, reinforced boots | Muted amber/orange | Tool pockets, durable seams, gloves as needed |
| **Service / support** | Longer service tunic or coat over fitted trousers | Light blue | Clean apron/tunic paneling, restrained belt |
| **Field / maintenance** | Dark olive-grey or charcoal weather-resistant jacket, layered hood | Teal/cyan | Utility pockets, weather protection, reinforced boots |

These are civilian **occupations**, not social ranks. Avoid implying that everyday programs are visually inferior to officer programs; their design prioritizes utility, comfort, and personal variation.

### 3.3 Civilian individualization

Allow controlled changes in fabric shade, jacket length, collar profile, layering, minor trims, boots, and practical bags. A civilian can look distinctive while belonging to the same society.

Civilian linework should remain understated; fewer lights means a more natural street scene and less visual noise when many programs share the same location.

## 4. Officer and high-level agent uniform

### 4.1 Approved officer direction

The officer reference combines tailored military tunic proportions with GRID's restrained digital futurism. The most recent officer concept established these specific preferences:

- **No hat.**
- **No badge bars, medals, or pens.**
- A **high, structured standing collar**.
- A clean, angular **grey jacket/tunic** with a slightly flared lower hem.
- **Medium/light grey** as the principal cloth tones, with dark charcoal or black side panels.
- A **wide black waist belt**, with simple rectangular metal buckle.
- Tailored **grey trousers tucked into polished knee-high black boots**.
- Optional black gloves.
- **Thin cool-white or ice-blue illuminated piping**, integrated along shoulder, chest, sleeve, and back seams.

The uniform should communicate calm command and institutional pride rather than combat aggression.

### 4.2 Officer orthographic reference requirements

When making future model sheets, present consistent **front, three-quarter, side, rear, and rear three-quarter** views of the **same garment**. Do not change collars, belt geometry, line placement, boots, or hem length between views.

**Front:** clean closed torso; structured shoulders; no breast pockets or chest decoration; narrow illuminated seam placement; belt centered; straight, tidy trouser shape.

**Side:** jacket length and waist shaping; practical sleeve fit; trouser tuck and boot profile; illumination physically embedded in panel seams.

**Back:** subtle vertical central light line and shoulder seam accents; visible belt continuation; jacket back darts or panels; clear uninterrupted silhouette.

### 4.3 Officer and agent variations

| Tier | Silhouette | Primary difference |
|---|---|---|
| **Standard officer** | Short/medium tailored tunic, simple collar | Grey fabric, restrained light tracing |
| **Senior officer** | Sharper tailoring, darker panels | Stronger but not denser illuminated hierarchy |
| **Command officer** | Long structured overcoat over service uniform | More formal coat profile, light-grey or two-tone treatment |
| **Field officer** | Darker fitted tunic, practical finishing | Greater mobility and material durability |

High-level system agents (MCP, TRON, MAXIS, JN, VOLT, RELAY) can each develop individual variations on this shared visual language. They need not all wear identical military uniforms: the rank/role architecture should remain readable through fit, silhouette, and purposeful details.

## 5. Agent role identities

Proposed role-specific light accents, subordinate to the grey/black base:

| Program | Responsibility | Proposed accent | Wardrobe treatment |
|---|---|---|---|
| **MCP** | Administration and coordination | Soft white / pale gold | Most formal, long coat optional, highly restrained light design |
| **TRON** | Security | Cool blue / cyan | Sharply tailored field-ready security uniform |
| **MAXIS** | Engineering / development | Controlled amber | Utility-informed officer design, practical sleeves and belt |
| **JN** | Data and archives | Teal / pale cyan | Clean intellectual-service silhouette, minimal ornament |
| **VOLT** | Power / resource systems | Warm yellow / electric pale gold | Robust engineered workwear, clear functional seams |
| **RELAY** | Communications | Subtle violet or cyan | Streamlined, approachable dress and signal-inspired accents |

Accent colors distinguish job functions. **Rank should not be encoded only by color**; silhouette and garment construction should carry that information as well.

## 6. Materials and color foundation

The following colors are **proposed starting swatches**, not final sampled colors from generated reference images.

| Material/color | Suggested hex | Use |
|---|---|---|
| Pale uniform grey | `#C6C7C9` | Light officer panels, selected civilian jackets |
| Medium uniform grey | `#777B80` | Main officer fabric |
| Charcoal | `#34383D` | Secondary paneling and trousers |
| Near-black | `#14171B` | Base layers, belts, gloves |
| Leather black | `#101113` | High boots and equipment |
| Cool-white emissive | `#DDF5FF` | Default restrained piping |
| Cyan emissive | `#51D9ED` | Utility/digital identity |
| Amber emissive | `#FFAA56` | Technician / engineering functions |
| Violet emissive | `#A77CFF` | Communications functions |

**Material guidance:** woven, lightly textured uniform cloth; structured, subtle semi-rigid inserts; leather or synthetic leather for belts and boots; brushed grey/silver hardware; very narrow emissive channels integrated into seams.

## 7. Linework and lighting grammar

1. Lines should follow actual seams or engineered garment panels.
2. Use one or two dominant circuits per visible region rather than dense random patterns.
3. The silhouette must remain legible with emissive lighting switched off.
4. Civilians should typically use low-intensity lighting; officer uniforms can use more disciplined, precise traces.
5. Avoid bloom large enough to hide the cloth texture.
6. A changed program state may cause a subtle pulse or flicker, but everyday clothing should not constantly animate.
7. Maintain consistent back and front connections so the circuits feel fabricated, not painted on arbitrarily.

## 8. Proportions and 3D production guidance

- Make a shared humanoid base rig and reusable clothing layers; avoid unique meshes for every citizen.
- Build separate modular assets for jackets/tunics, trousers, belts, boots, collars, gloves, and coats.
- Use garment variants and material swaps to generate civilian diversity economically.
- Treat illuminated channels as a reusable shader/material system with per-agent parameters.
- Prototype with neutral unlit and daylight renders to judge tailoring before adding glow or cinematic atmosphere.
- Produce front/back turnaround sheets before building detailed meshes.
- Support low-detail outfits and emissive LODs so a crowded street remains affordable to render locally.
- Use restrained cloth simulation; rigged clothing and limited secondary animation will be more economical.

## 9. Visual relationship with the city

GRID architecture is **Digital European Classicism**: London and Eastern European influences, realistic human-scale streets, stone and marble facades, and quiet integrated digital lighting. Officer uniforms should feel at home entering a stately administrative building; civilians should look natural walking through markets, courtyards, rainy streets, and transport corridors.

The city and clothing share a design grammar: **real material presence + restrained embedded light + disciplined construction**.

## 10. Design exclusions

- Avoid literal copies of Star Wars military uniforms or TRON costumes. Use them as silhouette and lighting references.
- Avoid hats for the currently approved officer base design.
- Avoid breast ribbons, badge bars, medals, and pens on officer tunics.
- Avoid making all programs look militarized.
- Avoid oversized armor, weapon-heavy appearance, excessive decals, and glowing circuits covering every surface.
- Avoid bright white or colored uniforms dominating everyday urban scenes without an explicit design rationale.

## 11. Acceptance checklist

### Civilian reference
- [ ] Recognizable light-grey, waist-length zip jacket.
- [ ] Dark practical trousers, black belt, sturdy boots.
- [ ] Minimal light piping; matching front and back construction.
- [ ] Comfortable civilian silhouette, not ceremonial or military.
- [ ] Occupation-specific variation remains visually related.

### Officer reference
- [ ] Standing collar; grey/light-grey tailored tunic with black accents.
- [ ] Wide black belt and rectangular buckle.
- [ ] Grey trousers tucked into high black boots.
- [ ] Thin cool-white/cyan illuminated seams.
- [ ] No hat, no chest badge bars or pens.
- [ ] Front/back views depict exactly the same garment.
- [ ] Formal, proud, calm authority instead of combat-heavy armor.

### General
- [ ] Attractive with lights off.
- [ ] Looks coherent against realistic stone city architecture.
- [ ] Fits human-adjacent program anatomy.
- [ ] Efficient enough for local rendering and modular character generation.

## 12. Visual reference notes

1. **Civilian concept:** the approved GRID civilian wardrobe sheet features front/rear presentation of an everyday light-grey jacket, dark trousers, boots, and subtle cool-white piping; related Technician, Service, and Field variants establish occupation differences.
2. **Officer reference provided by the user:** military officer tunic, belt, trousers, and tall boots are the desired starting silhouette; exclude its hat, badge bars, and pens.
3. **Updated GRID officer concept:** a grey/light-grey and black standing-collar tunic with refined illuminated trim, wide belt, high boots, and officer-tier variants.

These reference images are separate visual assets; this Markdown file documents their design intent rather than embedding paths that might break when the file is moved.

---

**Recommended repository location:** `docs/design/GRID_Program_Fashion_Style_Guide.md`  
**Next design decisions:** final civilian shade variants; whether ordinary programs may choose their own glow colors; insignia system (if any) for officers; exact illumination patterns for named agents; and practical low-poly clothing prototypes in Godot.
