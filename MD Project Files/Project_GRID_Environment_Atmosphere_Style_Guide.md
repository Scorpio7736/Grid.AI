# Project GRID — Environment & Atmosphere Style Guide

**Version:** 0.9 (design draft)  
**Status:** Proposed visual specification  
**Theme:** Eternal Autumn Evening  
**Related documents:** GRID City Design Style Guide; GRID Program Fashion Style Guide; Project GRID Design Specification

## 1. Vision

Project GRID exists in a **perpetual early-autumn evening**, under **medium-to-heavy clouds**, immediately **after a rainstorm**. The streets remain wet, autumn foliage persists, and warm architectural lighting contrasts with restrained cyan digital accents. The city is alive and animated, even though its season, time-of-day lighting, and post-rain condition do not progress.

The underlying architectural style is **Digital European Classicism**: lived-in London and Eastern European streetscapes, substantial masonry, classical civic buildings, clean sidewalks and squares, and subtle TRON-inspired illuminated inlays. The mood is calm, collected, dignified, quietly atmospheric—not abandoned, cyberpunk-chaotic, or uniformly monumental.

> **Design principle:** The citizens consider the environment normal. Permanent twilight and damp streets are fundamental properties of their world, not an ongoing unusual weather event.

## 2. Non-negotiable environmental rules

| Feature | Target behavior |
|---|---|
| Time of day | Always early evening; visual reference approximately 5–6 PM in autumn |
| Sun | Low, fixed apparent position; soft and diffuse through clouds |
| Sky | Always medium-to-heavy layered gray-blue cloud cover |
| Rain | Has just stopped; no routine active rainfall |
| Ground | Persistently damp; selectively reflective stone and pavement |
| Puddles | Present in plausible low spots; not flooding every street |
| Season | Permanently mid-autumn, without seasonal progression |
| Trees | Amber, ochre, rust, muted green, some fallen leaves |
| Atmosphere | Cool air, light haze/fog, soft long-distance depth |
| Wind | Gentle to moderate; subtle environmental animation |
| Streetlights | Always illuminated, with warm amber tones |
| Digital lighting | Restrained cyan/white inlays; responds to simulated systems |
| Day/night cycle | None |

These are **visual constants**, not a prohibition on animation or changes in building activity.

## 3. Lighting direction

The city should be bright enough to appreciate limestone, marble, stone details, outfits, facial expressions, and readable streets, but dim enough to make windows, lamps, and digital seams meaningful.

- **Primary ambient light:** muted, cool gray-blue from the clouded sky.
- **Sun direction:** low-angle, fixed evening direction, heavily diffused.
- **Horizon:** a narrow possibility of warmer color beneath the clouds—restrained, never a vivid sunset spectacle.
- **Street lamps and windows:** warm amber, softly reflected in wet ground.
- **Digital inlays:** pale cyan and white-blue; reserved for pavement routing, doors, buildings, program uniforms, and live systems.
- **Contrast:** cool ambient vs. warm inhabited interiors vs. controlled technological illumination.
- **Shadows:** soft, legible, and coherent across the scene.

Avoid intense neon saturation, excessively dark streets, or bloom that hides material details.

### Proposed working palette

These are starting values for art direction, **not final calibrated rendering values**.

| Color | Hex | Suggested use |
|---|---|---|
| Evening Sky | `#667789` | Sky ambient reference |
| Heavy Clouds | `#394755` | Cloud shadows and layers |
| Wet Limestone | `#96979A` | Rain-darkened light masonry |
| Rain-Darkened Stone | `#42474E` | Pavement and darker masonry |
| Autumn Amber | `#B87B3C` | Leaves and foliage accents |
| Fading Foliage | `#73764D` | Remaining green/olive leaves |
| Warm Streetlights | `#E6B578` | Street lantern and window warmth |
| Digital Cyan | `#66D9F1` | Subtle emissive infrastructure |

## 4. Rain and wet-surface art direction

The city is in the **aftermath** of rain, not the middle of a storm.

- Build wet-stone materials with tuned roughness, normal mapping, and controlled reflections.
- Put puddles at curbs, between stones, beside steps, and in recessed paved areas.
- Keep building walls and roofs damp, with realistic material variation.
- Use soft reflections of lamps, windows, and cyan linework; avoid mirror-like streets everywhere.
- Add occasional fine ripples or tiny surface disturbance if desired, without implying heavy active rainfall.
- Keep runoff, drainage, and pedestrian paths credible.
- Do not create constant rain particle effects in the default environment.

## 5. Autumn vegetation and ambient motion

Permanent mid-autumn should be evident without overwhelming the orderly European streets.

- Mix amber, ochre, rust, olive, and muted green foliage.
- Retain enough leaves for healthy recognizable trees; accumulate a few fallen leaves around curbs and courtyards.
- Use modest wind sway and infrequent leaves drifting along streets.
- Move clouds slowly, without transitioning into clear skies or heavy rain.
- Animate faint haze, puddle ripples, chimney/utility steam in appropriate districts, and practical signs of habitation.
- Allow window lighting, transit, pedestrians, and district systems to respond to program activity.

**Constant atmosphere does not mean static scenery.** Environmental movement should feel natural and understated.

## 6. District treatments

### Civilian neighborhoods

Human-scale historic streets with 3–7-story masonry buildings, window light, shopfronts, autumn trees, narrow side roads, wet sidewalks, and everyday pedestrian programs. Quietly lived-in, not sterile or abandoned.

### Administrative district — ARCHON

Larger pale stone civic facades, formal plazas, axial approaches, restrained banners and insignia, orderly lighting, and selective monumental landmarks. Preserve walkable streets and neighborhood continuity.

### Security district — SENTINEL

Austere institutional stone, disciplined geometry, guarded entries, subtle security wayfinding, slightly cooler lighting, and uncluttered public circulation.

### Engineering / processing — FORGE

Darker rain-treated masonry, iron and metal structural details, occasional steam, integrated glowing conduits, and useful industrial access without aggressive cyberpunk clutter.

### Archives — ARCHIVIST

Dignified archive halls, orderly colonnades, quiet enclosed courtyards, subdued teal/cyan detailing, and a contemplative atmosphere.

### Energy infrastructure — VOLT

Civic-industrial buildings with visible yet elegantly integrated energy conduits and carefully controlled brighter utility accents.

### Communications — RELAY

Clear public-facing buildings, transport or messaging nodes, streamlined illuminated wayfinding, restrained signal details, and bright but warm interiors.

## 7. Integration with program fashion

The environment should flatter the existing visual style:

- **Civilian programs:** understated casual and utility clothing, subtle light lines; visibly belong to the lived-in city.
- **Officer and higher agents:** fitted gray/light-gray/black uniforms, standing collars, black belts, formal trousers and tall black boots, controlled luminous seams; no hats, badges, chest ribbons, or pens.
- Strongest emissive uniform lines should remain legible in the cool dusk without being visually overpowering.
- Building, street, and clothing inlays should share a consistent light-material vocabulary.

## 8. Three independent clocks

Do **not** couple the city's apparent evening to real Windows time or simulated work schedules.

| Clock | Meaning | Behavior |
|---|---|---|
| **Simulation clock** | Citizens' tasks, events, resource cycles, internal history | Advances only while the Grid simulation operates; pauses when stopped |
| **Environmental clock** | Sky, sunlight direction, seasons | Fixed at early-evening, mid-autumn appearance |
| **Host clock** | Real Windows time, scheduled host tasks, actual message timestamps | Follows the operating system; tasks cannot execute while host is powered off |

When the PC shuts down, the civilization suspends and preserves state. On restart it resumes rather than advancing fictional time through the shutdown period. The sunset/season visuals never change in either case.

## 9. Proposed Godot organization

```text
Grid.Viewer/
└── Environment/
    ├── GridEveningEnvironment.tres
    ├── Sky/
    │   └── OvercastSky
    ├── Lighting/
    │   ├── EveningSun
    │   ├── AmbientLighting
    │   └── StreetLights
    ├── Weather/
    │   ├── CloudMovement
    │   ├── LightFog
    │   └── Wind
    ├── Materials/
    │   ├── WetCobblestone
    │   ├── WetMarble
    │   ├── DampLimestone
    │   └── ReflectivePuddles
    └── Vegetation/
        ├── AutumnTrees
        └── FallenLeaves
```

These are proposed scene/resource names, not engine-mandated file types for every entry.

### Local-first performance priorities

- Use a reusable fixed atmospheric preset instead of a complex dynamic weather simulator.
- Prefer well-authored PBR wet-surface materials over expensive global reflections everywhere.
- Limit screen-space reflection usage and reflection probe count, based on target GPU testing.
- Keep fog, cloud movement, leaf animation, and puddle effects lightweight and adjustable.
- Provide graphics quality levels and allow the 3D viewer to close while the simulation continues.
- Ensure atmospheric rendering does not become a prerequisite for the headless simulation.

## 10. Prototype: one street, one evening

Before city-scale development, create a vertical slice featuring:

1. A short European stone street with a domed or classical institutional building in the distance.
2. Rain-darkened paving with a few appropriately positioned puddles.
3. Autumn trees and selective leaf piles.
4. Layered gray-blue clouds, fixed low evening light, and slight haze.
5. Warm streetlights and lived-in windows.
6. Subtle embedded cyan lines along architectural and pedestrian elements.
7. At least one civilian outfit and one officer uniform for lighting/material validation.
8. Program movement and ambient city behavior driven by actual simulation state when available.

### Visual acceptance criteria

- [ ] Scene reads as **early evening**, not midnight or daytime.
- [ ] Cloud cover is visibly moderate-heavy and consistent across camera angles.
- [ ] Wetness is visible but puddles and reflections are controlled.
- [ ] Mid-autumn reads instantly through foliage and ground details.
- [ ] The street feels realistically inhabited, not empty or grandiose everywhere.
- [ ] Stone and classical urban architecture dominate over neon.
- [ ] Cyan details are integrated into materials and architecture, not pasted on.
- [ ] Civilian and officer outfits remain legible under dusk illumination.
- [ ] The scene looks convincing with digital emissions temporarily disabled.
- [ ] Performance is acceptable under chosen local hardware limits.

## 11. Formal requirement

### GRID-ENV-001 — Eternal Autumn Evening

**Requirement:** Project GRID's default environment shall maintain a permanent early-evening, medium-to-heavy overcast, mid-autumn appearance with persistently damp, post-rain streets and surfaces. The world shall have no ordinary day/night or seasonal cycle. Ambient motion, inhabitants, interiors, and responsive infrastructure may change without altering those permanent conditions.

**Rationale:** Establish a calm, recognizable, cohesive setting for the European-classical city and its human-adjacent programs, while keeping rendering and simulation costs manageable for local operation.

**Validation:** Review representative exterior and interior views across districts, inspect wet-material and lighting consistency, verify clock separation, and benchmark performance on target hardware.

## 12. Decisions to revisit during implementation

- Exact sun altitude and camera-dependent sky exposure.
- Level of fog/mist by district and distance.
- Weather animation intensity (still damp vs. slight occasional mist).
- Wet-road material fidelity across low/medium/high graphics settings.
- Whether some unique locations have distinct interior lighting atmospheres.
- Treatment of digital weather anomalies as *exceptional scripted world events*, if ever desired; these should not silently break the default permanent environment rule.

---

**Art-direction summary:** *GRID is an inhabited European stone city in perpetual autumn twilight. Thick clouds hang above the streets; the rain has just stopped; warm lamps and windows reflect in wet paving; autumn trees move quietly; and delicate cyan circuitry reveals the digital world underneath.*
