---
name: rimworld-realistic-base-generation
description: Research, design, arrange, and review believable RimWorld colonies and presentation scenes from player-built visual references, gameplay constraints, installed-mod Defs, and large Real Ruins blueprint corpora. Use when creating showcase colonies, E2E/performance fixtures meant to look lived-in, room layouts, kitchens, restaurants, prisons, workshops, settlements, or reusable base-generation rules; use it before writing scene setup code instead of guessing room shapes, placement, materials, decor, utilities, or traffic flow.
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
- Read [references/real-ruins.md](references/real-ruins.md) whenever a new layout or placement rule could be
  measured from the Real Ruins corpus. Do not substitute a token sample for the bulk workflow.
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
7. For a new layout family, run the corpus analyzer against at least 5,000 metadata rows and safely parse at least
   2,000 blueprint bodies. Retain the corpus identity and rejection count, then inspect a stratified schematic
   subset before promoting aggregate measurements. The checked-in 2026-08-14 benchmark cohort is documented in
   [references/real-ruins.md](references/real-ruins.md).

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
Before every retained segment, reapply and read back the exact camera center/root size immediately before the
capture call. Background/minimized RimWorld can edge-scroll between setup and capture even when the capture
route itself correctly leaves the camera unchanged. Reject any segment whose retained crop does not contain the
declared action geography; never trust a center copied from notes when the live camera state can be read.

After the first playable draft exists, keep one persistent RimWorld process as the authoring source of truth.
Inspect the rendered map, correct individual buildings/floors/utilities through typed Gateway and native player
controls, let systems settle, and create named native save checkpoints. Do not repeatedly regenerate the whole
colony from a setup script to fix a misplaced shelf, fence, conduit, door, or decoration; return to scripted setup
only when the live state is genuinely unrecoverable or the fixture itself is the product under test.

## Arrange through exact game contracts

- Resolve exact active-mod Defs, sizes, rotations, interaction cells, link facilities, support requirements,
  storage settings, research, power, plumbing, and faction/access rules before placement.
- Persist the chosen scene origin as immutable scenario state. Never recompute later furnishings, camera targets,
  utility checks, or captures from a pawn position: pawns are mobile even when setup initially drafted them.
- Never treat a `Thing.Position`/spawn root as its visual center or occupied footprint. Before spawning, call
  RimWorld's finalized `GenAdj.OccupiedRect(root, rotation, def.size)` and, where applicable,
  `ThingUtility.InteractionCellWhenAt(def, root, rotation, map)`. Even-sized Defs shift asymmetrically with
  rotation; a root that looks centered in source can occupy a wall or neighboring fixture.
- Derive the required room interior from the sum and orientation of actual loaded footprints. If a proposed wall
  run does not fit, turn a fixture onto a second wall or redesign the room; never shrink the Defs mentally or
  allow spawning to replace walls.
- Derive seating from the actual table footprint. Never place chairs from a guessed visual center.
- Treat `<rotatable>false</rotatable>` as an asset contract. Do not rotate a Graphic_Single object to make it fit.
- Route visible conduits and plumbing through wall cells or service corridors unless an inspected reference and
  functional constraint justify a crossing.
- Put large water storage and noisy/fueled utilities in an exterior service yard unless an exact building contract
  requires indoor placement. A roofed technical enclosure is not the same as a dining/kitchen room.
- Align production benches to a wall run or a coherent shared island, and retain every interaction cell and aisle.
  Scattered independent benches are a rejected presentation grammar.
- Validate the complete backing wall for every wall-run footprint, not only the root or one bounding edge. Reserve
  both the interaction cell and at least one inward approach cell for each bench; two benches that merely avoid
  physical overlap can still form an unusable or visually cramped corner when their worker approaches collide.
- Distinguish **against a wall** from **in a wall**. Ordinary worktables belong wholly inside the room with one
  occupied edge adjacent to the wall. Only an exact `canPlaceOverWall` appliance may replace a wall cell.
- For two-sided wall appliances, validate both functional sides. A Core cooler's cold cell is
  `root + South.RotatedBy(rotation)` and its exhaust is `root + North.RotatedBy(rotation)`; the former must be in
  the roofed cold room and the latter unobstructed in a different room or outdoors. Keep the layout's reserved
  opening and furnishing spawn cell as one shared value.
- Validate decorative furniture through the same occupied-rect preflight. A plant pot or lamp is not harmless
  clutter when its root is a boundary cell, doorway, freezer wall, interaction spot, or hauling aisle.
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
- Are chairs attached to the real occupied cells of a sufficiently large table, with believable circulation?
- Are fixed-orientation utilities using their native sprite direction, and are tanks/generators in a plausible
  service location rather than dropped into an occupied public room?
- Do cables and pipes follow walls/service runs instead of crossing open floors without a reason?
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
