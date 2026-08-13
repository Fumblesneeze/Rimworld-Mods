---
name: rimworld-realistic-base-generation
description: Research, design, arrange, and review believable RimWorld colonies and presentation scenes from player-built visual references, gameplay constraints, installed-mod Defs, and optional Real Ruins blueprint samples. Use when creating showcase colonies, E2E/performance fixtures meant to look lived-in, room layouts, kitchens, restaurants, prisons, workshops, settlements, or reusable base-generation rules; use it before writing scene setup code instead of guessing room shapes, placement, materials, decor, or traffic flow.
---

# RimWorld Realistic Base Generation

Create colonies that look accumulated through play, not drawn as abstract test diagrams. Ground each scene in
several inspected player references, then reconcile aesthetics with the exact jobs, Def footprints, links,
reservations, power, plumbing, temperature, storage, and pathing needed by the requested workflow.

Follow root `AGENTS.md`. For executable game setup and capture, also use `rimworld-dev-gateway`; for Workshop
presentation, also use `release-rimworld-mods`. This skill informs arrangement and visual review. It does not
waive native-action or live-observation requirements.

## Route the task

- Read [references/design-rules.md](references/design-rules.md) for every colony or room design.
- Read [references/source-catalog.md](references/source-catalog.md) before collecting or citing visual references.
- Read [references/showcase-archetypes.md](references/showcase-archetypes.md) for kitchens, restaurants,
  dishwashing, prison dining, or production-chain scenes.
- Read [references/real-ruins.md](references/real-ruins.md) only when a Real Ruins cache or bounded blueprint
  sample could add evidence about footprints, materials, or installed-mod buildings.
- Read [references/design-record.md](references/design-record.md) and create its per-scene record before writing
  setup code for a showcase or other presentation fixture.

## Research before arranging

1. Write a scene brief: biome, tech/wealth stage, population, colony form, required packages, exact visible
   workflow, camera footprint, and the story the room should tell.
2. Collect at least 12 relevant references for a new archetype:
   - four whole-colony references showing context and growth;
   - four close room/workflow references;
   - two references using the requested optional-mod ecosystem when available;
   - two contrasting examples, such as compact versus spacious or grid versus organic.
3. Prefer current in-game screenshots and high-resolution colony renders. Use mechanics documentation to explain
   function. Do not treat an image-search caption, Pinterest repost, AI description, or optimization sketch as
   sufficient evidence.
4. Download research images only beneath ignored `artifacts/BaseDesignResearch/<date>-<topic>/`. Make a labeled
   contact sheet and inspect it at original resolution. Commit attribution links and observations, not other
   players' copyrighted image files.
5. Record recurring patterns, exceptions, source IDs, and confidence. One image may inspire a candidate; it
   cannot establish a general rule by itself.
6. Create `mods/<ModName>/Release/workshop/designs/<showcase-id>.md` from the design-record template. Bind every
   placement decision to a mechanics contract or eligible reference ID. Do not count unattributed supplemental
   images as support for recurrent rules.

## Convert evidence into a plan

Analyze four layers separately:

1. **Colony topology:** compact block, connected compound, town, mountain adaptation, terrain-following growth.
2. **Adjacency and traffic:** production inputs, workers, outputs, diners, guests, prisoners, cleaners, animals,
   doors, corridors, and public/private boundaries.
3. **Room grammar:** usable work cells, building rotation, storage buffers, light, flooring, power/plumbing,
   furniture, art, and circulation.
4. **History and visual rhythm:** additions, repairs, unequal room sizes, material transitions, retained natural
   features, incomplete expansion, and restrained incidental clutter.

Draft an adjacency graph before cell coordinates. Then choose room rectangles or irregular footprints that can
grow out of that graph. Explain each asymmetry through terrain, chronology, function, or status; random notches
and scattered props are not organic design.

Retain the brief, references, classified rules, adjacency graph, planned zones and placement rationale in the
scene's design record. If a draft changes after live inspection, record the rejected arrangement and why. This
is the reviewable bridge between the research corpus and executable cell coordinates.

For presentation scenarios, place the complete action geography inside one stable camera view or two deliberate
hard-cut views. Preserve enough surrounding colony to make the room credible without burying the subject.

## Arrange through exact game contracts

- Resolve exact active-mod Defs, sizes, rotations, interaction cells, link facilities, support requirements,
  storage settings, research, power, plumbing, and faction/access rules before placement.
- Use materials and furniture consistent with one colony stage and local palette. A polished restaurant beside
  raw dirt, temporary sleeping spots, and random crash supplies is usually a contradiction unless the scene
  explicitly tells that story.
- Give named pawns believable roles, apparel, tools, schedules, and nearby destinations. Keep incidental pawns
  and animals out of critical work cells.
- Let ordinary room, power-net, plumbing, temperature, reachability, reservation, storage, and job systems settle.
  Inspect their actual state before capture.
- Setup code may construct the colony and seed inputs. The visible outcome must still come from the ordinary
  bill, order, ingest, hauling, serving, washing, or other native player workflow.

## Review like a player

Reject a scene when any answer is no:

- Does the crop read as part of a colony before the showcased mechanic is explained?
- Can a viewer tell where inputs come from, where work happens, and where outputs go?
- Are room sizes, doors, work cells, paths, and storage plausible for the visible population?
- Are material and floor changes deliberate rather than a uniform fill or random patchwork?
- Are the required mod buildings used in the way their own gameplay expects?
- Is there a believable amount of lighting, decor, wear, stored goods, and empty circulation space?
- Are debug UI, learning helper, test labels, selection brackets, fixture names, and manufactured outcomes absent?
- Does the native action remain legible at final crop size and during a sub-five-second GIF?

Capture a rejected-frame note and feed the reason back into the rule catalog. Do not patch a weak scene by adding
more arbitrary clutter.

## Expand the skill carefully

When adding a rule, record its evidence class:

- **mechanical:** established by RimWorld or an exact optional-mod contract;
- **recurrent:** observed across at least three independent player colonies;
- **archetype:** recurring only within a named style, biome, tech stage, or mod ecosystem;
- **candidate:** useful hypothesis awaiting more references.

Keep counterexamples. Prefer probability and variation ranges over one canonical blueprint. Update the source
catalog's access date and direct links, and never silently convert a showcase-specific decision into a universal
colony rule.
