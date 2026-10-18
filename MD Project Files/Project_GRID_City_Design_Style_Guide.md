# PROJECT GRID — City Design & Architectural Style Guide

## GRID-REQ-004 - Hardware-informed city composition (planned, 2026-10-18)

On first-time world creation, discover motherboard, CPU, GPU, RAM, storage and network hardware and use them to procedurally generate a unique city. Save the resulting layout and reuse the world on later startups; the viewer must not generate a new city when it opens. See the [generation contract](Project_GRID_Design_Specification.md#grid-req-004---hardware-generated-city-contract).

Proposed translation: motherboard connectivity guides the city framework; CPU influences processing districts; GPU influences visual-computation facilities; RAM influences memory/research areas; storage influences archives; network adapters influence gateways. These are initial design proposals, not implemented mappings or literal physical measurements. Precise component placement is not assumed discoverable.

Preserve Digital European Classicism: human-scale streets, ordinary neighborhoods, restrained landmarks, stone/material palettes and useful urban functions. Hardware-informed macro layout must still produce connected, navigable streets and coherent blocks. It must not turn every building into an exposed chip or replace the existing visual direction with a circuit-board diagram. Bound district/detail counts independently of raw device capacity.

The motherboard layout and CPU images in Design Concepts/World remain inspiration. They neither implement the feature nor require one particular host layout. Future Godot scenes should represent saved semantic layout data, stable entity IDs and component associations. The current five logical Genesis districts remain the implemented baseline; procedural city geometry is planned.

Hardware upgrades do not redraw an established city. Any future expansion/migration must be explicit and preserve existing identities and history.

## Implementation status - 2026-10-17

The runtime currently contains five logical districts and twenty citizens; no Godot city or navigation geometry is implemented. Administrative district edits transfer membership immediately, without a travel route. Future city views should consume engine snapshots and represent actual state changes; the visual districts below remain design targets.

See [Current implementation and verification](Project_GRID_Current_Implementation_Status.md).

**Document type:** Visual design specification  
**Status:** Concept / design direction  
**Version:** 0.7 (City Design)  
**Project:** GRID — Local-first digital civilization

---

## 1. Vision

GRID is a digital civilization living inside a local computer. Its city should feel like a **real, inhabited European city that happens to be digital**, not a generic cyberpunk skyline or a uniformly grand futuristic capital.

The intended architectural identity is **Digital European Classicism**: traditional stone-built urban streets informed by Russian, Slavic, and broader Eastern European cities, alongside London's Georgian, Victorian, and Edwardian streetscapes. Subtle TRON-inspired luminous lines reveal the city's digital nature.

**North-star statement:**

> GRID's streets are calm, deliberate, elegant, and lived-in. Stone and masonry establish permanence; classical detailing establishes identity; discreet illuminated infrastructure reveals an underlying computational world.

### Desired emotional qualities

- Calm, collected, and orderly
- Proud and dignified without being overwhelmingly monumental
- Atmospheric and slightly severe, but also welcoming and habitable
- Mature, understated, and grounded
- Visibly populated by human and human-adjacent programs
- Beautiful even in daylight when digital light lines are difficult to see

### Avoid

- Endless rows of giant futuristic towers
- Excessively empty plazas and oversized ceremonial structures everywhere
- Generic neon cyberpunk clutter, billboards, and crowded holograms
- Entire buildings made of glowing metal and glass
- Direct reproductions of political insignia, propaganda, or real-world state emblems
- Streets that look like stages rather than functioning places to live

---

## 2. Visual References and Their Roles

### Primary — The user-provided European street photograph

An overcast, wet street with traditional masonry facades, regular street widths, classical columns, shopfronts, pedestrian sidewalks, and an understated urban atmosphere. **This is the most important reference for ordinary GRID streets.**

### Secondary — London streetscapes

Use Georgian, Victorian, and Edwardian influences: disciplined facade rhythms, stone civic buildings, coherent blocks, restrained ironwork, elegant storefronts, small public squares, and historic street lamps.

### Secondary — Russian / Slavic and Eastern European urbanism

Draw from St. Petersburg-like neoclassical avenues, classical porticoes, robust masonry, composed street elevations, domes, monumental-but-integrated public buildings, and occasionally formal squares. Soviet-era monumental civic planning may inspire select public institutions, but not the scale of every neighborhood.

### Digital influence — TRON

Translate the aesthetic into inlaid light pathways, luminous seam lines, restrained geometry, subtle data glyphs, district accents, and energy-flow infrastructure. Digital effects should complement stone rather than replace it.

### Latest generated concept image

A rainy-dusk streetscape with warm lamps and illuminated windows, dark coats with thin cyan accents, wet stone streets, elegant historic buildings, and a monumental headquarters rising in the background. Treat this as a **mood and composition reference**, not a requirement that every street have a palace skyline.

---

## 3. Urban Structure and Scale

### Everyday neighborhoods — the default

- Predominantly **3–7 story** buildings, with variation by district
- Continuous street-facing facades and coherent urban blocks
- Sidewalks, side streets, pedestrian crossings, and practical street furnishings
- Mixed-use ground floors: shops, workrooms, service entrances, transit stops, cafes or their GRID-world equivalents
- Courtyards and smaller squares as important neighborhood spaces
- Architectural individuality within a consistent city-wide material language

### Civic landmarks — the exception

Large administrative headquarters, archives, security buildings, bridges, monumental stairways, and plazas should stand out **because most of the surrounding city is comparatively ordinary**.

- Formal symmetry and heavy masonry are appropriate for landmark buildings
- Strong roofline features such as domes, cupolas, clock-like towers, and raised porticoes
- Civic buildings can dominate long street vistas, but should not overwhelm every neighborhood
- Tall digital towers should be rare and tied to specific world systems

### Planning principles

1. **Human scale first:** streets should accommodate programs as inhabitants, not just act as distant cinematic backdrops.
2. **Readable street hierarchy:** main avenues, secondary streets, lanes, and courtyards.
3. **Useful urban functions:** buildings should visibly correspond to jobs, residents, institutions, and simulation systems.
4. **Quiet composition:** carefully paced lighting and signage, long sightlines, and low visual noise.
5. **Layered historical feel:** different facade styles can coexist while following consistent proportions and materials.

---

## 4. Architectural Vocabulary

| Element | GRID direction |
|---|---|
| Walls | Pale limestone, gray stone, marble accents, brick, masonry |
| Facades | Repeated tall windows, pilasters, cornices, recessed entryways |
| Civic buildings | Colonnades, porticoes, arches, stairs, domes, strong symmetry |
| Ordinary buildings | 3–7 story masonry blocks, modest ornament, functional ground floors |
| Roofs | Parapets, pitched roofs, mansards, cupolas, occasional domes |
| Metalwork | Dark wrought-iron-inspired railings, brushed metal, restrained trim |
| Road surfaces | Stone paving, asphalt insets, cobbles or setts in appropriate streets |
| Glass | Conventional windows with warm interiors rather than full glass facades |
| Digital layers | Thin cyan and amber inlaid seams, door glyphs, subtle status panels |

Marble is most effective at ceremonial entrances, major halls, stairs, statues, and special facades. Standard neighborhoods can rely more on convincing limestone, brick, and rendered masonry to preserve both visual realism and development performance.

---

## 5. Materials, Color, and Lighting

### Architectural base palette (proposed)

| Material / role | Color | HEX |
|---|---|---|
| Pale limestone | Warm off-white | `#D8D4CA` |
| Weathered stone | Light gray-beige | `#AEA99F` |
| Cool granite | Slate gray | `#737C82` |
| Charcoal masonry | Deep graphite | `#30363C` |
| Ironwork | Near-black | `#20272D` |
| Interior lamplight | Warm amber | `#E8B878` |
| Digital infrastructure | Soft cyan | `#55CDEB` |
| Digital secondary accent | Deep teal | `#298BA5` |
| Command accent | Subdued pale gold | `#C8AD72` |

These are starting art-direction colors, not locked material values. Physically based material properties, exposure, fog, and weather will significantly affect the final appearance.

### Lighting rules

- Warm street lamps and windows contrast with cool digital linework.
- Use cyan sparingly: pavement joints, transit lines, doorway identification, bridge edges, columns, and civic emblems.
- Most surfaces remain non-emissive stone, masonry, or metal.
- A rainy environment may emphasize reflected light; dry daytime streets must still work visually.
- Reserve strong, dynamic glow for notable events, command facilities, system warnings, and energy infrastructure.

### Weather and time of day

**Primary mood:** overcast daylight, rain afterglow, mist, and dusk. Support full day-night cycles later, including clear weather. The overall character must survive changes in weather.

---

## 6. Digital Architecture — How TRON Enters the City

The city is not a normal historical city with neon pasted onto it. Its infrastructure is designed around simulated computational functions.

- **Data paths:** thin luminous lanes set within roads or curbs, indicating important transport routes.
- **Building seams:** softly illuminated reveals in stone facades, especially around entrances and public infrastructure.
- **Access glyphs:** original GRID symbols identifying buildings, occupations, and districts.
- **Power conduits:** readable but subtle accents on utility facilities and the reactor.
- **Status changes:** lighting may pulse, dim, or change color in response to world events.
- **Transit:** digital vehicles or program transport systems can feel modern while moving through otherwise classical street corridors.

**Rule:** architecture comes first; digital illumination explains the world rather than replacing its visual identity.

---

## 7. District Design

### 7.1 Central Civic / Administrative District — ARCHON

- Grand but credible civic buildings in masonry and marble
- Offices, ministry-like halls, formal entrances, institutional squares
- Carefully controlled cyan and pale-gold accents
- One major landmark headquarters visible from surrounding avenues
- More formal landscaping, banners bearing original GRID insignia, and ceremonial lighting

### 7.2 Security District — SENTINEL

- Austere institutional stonework and strong vertical proportions
- Security headquarters, inspection stations, guarded courtyards
- Darker stone and cooler light accents
- A secure architectural feel without turning the entire district into a fortress

### 7.3 Engineering / Processing District — FORGE

- Functional urban-industrial architecture embedded in the city
- Brick, stone, steel, restrained utilitarian glass
- Workshops, service courts, maintenance corridors, engineering institutes
- Warmer amber and copper accent lighting, visible energy routing where warranted

### 7.4 Energy and Infrastructure — VOLT

- Substations and power buildings that resemble dignified public utilities
- Stone exteriors with precise modern mechanical interiors
- Select high-contrast luminous conduits and reactor-facing facilities
- City infrastructure remains believable and navigable

### 7.5 Archives and Memory — ARCHIVIST

- Libraries, records halls, repositories, quiet courtyards
- Elegant colonnades, stone staircases, refined detail
- Cool teal or pale cyan wayfinding
- Architecture communicates longevity and protection of history

### 7.6 Communications and Transit — RELAY

- Stations, exchanges, communications halls, bridges, and interconnected streets
- Signs and signal systems designed with small, readable glyphs
- Subtle violet-blue or cyan accents when a distinct function color is useful
- Lively but not visually chaotic hubs

### 7.7 Residential / Civilian Neighborhoods

- Primarily 3–6 story masonry streets
- Local gathering spaces, shops, courtyards, transit access
- More varied facades and subtle personalization
- Minimal hierarchy symbols and less glowing ornamentation than command buildings
- The visual proof that GRID is a society rather than only a command installation

---

## 8. Programs in the City

Programs should be human or human-adjacent. Their apparel helps communicate social role while keeping them visibly part of the same civilization.

| Program tier | Fashion | Presence in streets |
|---|---|---|
| Elite named agents | Structured officer coats, refined tailoring, restrained luminous insignia | Civic buildings and occasional public appearances |
| Specialists | Service jackets, technical wear, moderate light tracing | Workplaces, transit, institutional spaces |
| Workers and civilians | Simpler coats, everyday clothes, utility outfits, minimal luminous seams | Shops, public squares, sidewalks, residences |

Everyday streets should include an ordinary mix of civilians and working programs; avoid filling the city with identically dressed officers. Crowd density should be moderate, with active routines and credible destinations.

---

## 9. Streets, Public Realm, and Environmental Storytelling

### Street furnishings

- Traditional or simplified cast-metal street lamps
- Benches, railings, bollards, transit shelters, planters, restrained signage
- Historic-looking masonry bridges and stairs where terrain demands them
- Small digital access terminals integrated into stone entrances

### Street activity

- Programs walking to work, waiting at crossings, exchanging messages, maintaining infrastructure
- Some service vehicles and transit, but not constant high-speed traffic
- Light changes and crowd movement corresponding to simulation events
- Localized district activity rather than endless cinematic spectacle

### Environmental storytelling

- Buildings associated with actual simulation services and roles
- Public displays reflecting district output or important historical events
- Construction and repair work triggered by simulated decisions
- Memorials and inscriptions based on significant world history, using fictional GRID civic motifs

---

## 10. 3D Production and Performance Guidelines

GRID remains a **free, local-first** project, with a constrained rendering budget so the civilization can run alongside normal PC activities.

### Recommended production approach

1. Begin with one representative street block, not the whole capital.
2. Build a modular kit of facades, windows, cornices, roofs, doors, pavements, and lamps.
3. Use reusable materials and trim sheets to reduce memory and disk use.
4. Apply modular facade variations to avoid visibly repetitive streets.
5. Use low-cost emissive materials for most digital linework; reserve complex lighting for focal areas.
6. Use level-of-detail, occlusion culling, instancing, and distance-based citizen simulation.
7. Keep the Godot viewer separate from the authoritative C# simulation engine.
8. Make the viewer optional; world state continues when rendering is closed.

### Initial visual prototype: one street

**Scene:** A calm, wet, slightly overcast masonry street. Four- to six-story stone buildings line both sides, with modest shops, a rounded classical corner building, iron street lamps, subtle thin cyan pavement lines, a few pedestrians, and a distant administrative landmark.

**Minimum acceptance criteria:**

- Believably European in silhouette *without* the neon
- Distinctive as GRID *with* the neon
- Looks inhabited rather than abandoned
- Shows everyday civilian fashion alongside at least one formal agent
- Runs acceptably on the user's existing PC (hardware-specific benchmark to be established)
- Reuses a modular asset kit that can expand into adjacent streets

---

## 11. Art Review Checklist

For every significant street or building concept, ask:

- [ ] Does the city look like a place that programs can actually inhabit?
- [ ] Is ordinary street scale preserved?
- [ ] Is European classical influence legible without copying a specific building?
- [ ] Are the stone and masonry materials convincing?
- [ ] Are digital glow elements restrained and infrastructural?
- [ ] Would the scene still be attractive with all emissive effects disabled?
- [ ] Do important buildings stand apart from ordinary streets?
- [ ] Are pedestrians dressed appropriately for their program roles?
- [ ] Does the lighting maintain a calm, dignified atmosphere?
- [ ] Can this environment be built efficiently with modular assets?

---

## 12. Decisions and Open Questions

### Established direction

- Human-scaled European stone streets are the default.
- London and Russian / Slavic / Eastern European architecture are primary influences.
- Classical civic grandeur is reserved mainly for landmarks and administrative districts.
- TRON-style lightlines are integrated subtly into physical architecture.
- Atmosphere should be quiet, dignified, and lived-in.
- Named agents dress more formally; civilians and workers dress more casually.
- The whole environment must remain suitable for local, cost-conscious 3D development.

### Still to decide

- Whether the city has a canonical name separate from GRID
- Which real-world periods and cities should carry the strongest visual weight
- The standard placement and meanings of district insignia
- Default weather and day/night settings
- Whether transport is conventional, digitally stylized, or a mixture
- How much historical ornament versus simpler modern masonry each neighborhood receives
- The precise Godot rendering settings after measuring the host PC

---

## 13. Short Style Statement

> **GRID is a living digital European city.** Its ordinary streets draw on the stone facades, traditional proportions, and calm urban atmospheres of London and Eastern European cities. Its civic landmarks express measured grandeur, while subtle lines of cyan and amber light reveal a computational infrastructure embedded beneath the masonry. The city is elegant, inhabited, disciplined, and believable before it is spectacular.

**Design shorthand:** *St. Petersburg / London streets × integrated TRON lightlines × a functional autonomous digital civilization.*

---

*This guide complements the earlier GRID architecture, local-first/storage, development/language, and program visual-design discussions. It is an art-direction specification, not a finalized 3D asset inventory or performance benchmark.*
