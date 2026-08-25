# Visual verification and acceptance

## Static/package checks

Before launching RimWorld, run focused tests that assert the contract actually at risk:

- expected diffuse/cardinal/mask files exist and are staged byte-for-byte;
- exact width, height, aspect, alpha bounds, and transparent corners;
- diffuse/mask alpha equality and meaningful red/black regions;
- no vivid background chroma or silhouette fringe;
- projected top/front/side/bevel/contour pixel budgets match the recorded component measurements;
- the declared top/front/side luminance ordering and ratios pass before and after representative
  Stuff tinting, and each plane remains separately legible at ordinary and far useful zoom;
- padded-canvas margins and runtime draw size match the comparator; a tight crop rendered on a short
  compensating mesh is rejected because it changes apparent height and orientation;
- base/variant and clean/dirty sprites are visibly distinct where required;
- every selected cardinal frame has an explicit reviewed hash plus direction-specific equipment
  order; north/east order and reversed south/west order are consistent;
- Def `graphicClass`, shader, `texPath`, `drawSize`, Stuff behavior, and optional patches select the
  intended files under both present and absent dependency matrices.

Do not accept “all files differ” as cardinal proof. A rotated raster, wrong underframe, or swapped
east/west family also differs.

## In-game candidate comparison

Use disposable comparison Defs only while selecting a generator/model. Render all candidates in one
fresh process beside the same Core references at the same camera zoom. Select each candidate through
the native selection path so its footprint/bracket and inspect UI are visible. Remove comparison
Defs and textures after choosing; do not ship the rejected catalog.

Capture each viable candidate at close, ordinary, and far useful map zoom, with no selection brackets
or debug labels in the identity frames. For Stuff assets, repeat the same camera framing with the
representative material families. Record the exact camera/zoom state and do not resize one screenshot
to impersonate another zoom.

For damageable objects, include undamaged, moderate, and severe states and inspect straight,
doubled, and junction cases. Reject damage projected at the owning-cell center, beside the object,
beyond its silhouette, or with an orientation inconsistent with the fixed camera.

## Final native workflow

On the independently reviewed Release package:

1. Start a fresh isolated saved-data folder with the smallest exact mod list.
2. Hash normal config before/after and bind every action/screenshot to the launched PID.
3. Place the object through the native Architect/designator, crafting, hauling, job, or gizmo path.
4. For a cardinal building, place north/east/south/west copies beside same-facing Core workbenches.
5. Capture before placement, the player action, and close after views at one comparable zoom.
6. Select each result and inspect the actual texture/material state. Explicitly inspect the
   `Rot4.South` view for a screen-bottom underframe and player-facing doors/controls on the
   screen-bottom edge. The fixed camera, visible appliance face, and native interaction side are
   separate contracts: verify each from the authored Def and observed player workflow, and do not
   silently change an already-published interaction offset merely to match a redrawn front face.
   Frame multiple comparison objects through the camera first, then select only the target whose
   single-Thing inspect panel is evidence; a multi-selection screenshot does not show that panel.
7. For Stuff masks, observe wood/stone/metal or other materially distinct Things; verify fixed
   accents do not tint and dirty overlays remain visible. For damageable art, verify undamaged,
   moderate, and severe damage remains attached to its true draw geometry.
8. Personally view the screenshots. Record concrete observations, not just automation assertions.
9. Require a clean relevant log, exact build/package hash, restored settings, sanitized artifacts,
   and graceful exact-PID shutdown.

## Blind independent visual review

After personal inspection, give an independent reviewer sub-agent only the unlabeled, fresh in-game
screenshots covering the required zoom and material states. Do not disclose the mod, asset name,
intended identity, implementation, topology names, reference selection, or expected answer. Ask the
reviewer to describe what it sees and infer the gameplay purpose, then assess fixed-camera
perspective, silhouette, outline, material response, adjacency/connection coherence, surrounding
RimWorld style, and degradation across zoom levels.

The review passes only if the reviewer correctly recognizes the intended object and purpose without
prompting and finds no perspective, outline, incoherent-style, material, connection, or zoom-level
legibility defect. Any ambiguous identification, reliance on selection/UI text, or material defect
returns the asset to candidate iteration. Retain the exact screenshot inventory, context-free review
prompt, and reviewer response with the reviewed build evidence.

For a publication rendering that includes a furnished or inhabited scene, also apply the independent
placement-audit gate from `rimworld-realistic-base-generation`. The reviewer must enumerate visible beds,
chairs, tables/counters, workbenches, doors, interaction spots, and main aisles and explicitly identify any
non-aesthetic, implausible, blocked, floating, wrongly oriented, clipped, or rule-breaking placement. This is
separate from object-identification review and cannot be satisfied by a generic visual-coherence verdict.
Whenever a claimed placement, offset, boundary balance, or non-overlap can be measured from final in-game
pixels, add a TDD-backed deterministic measurement over the exact promoted source capture and rendered card;
manual inspection remains required but may not waive a failing mechanical gate.

A direct spawn may arrange a catalog, but it does not prove player placement. A log saying a Graphic
resolved, a checksum, an HTTP success, or a diagnostic query is supporting evidence only.

## Approval manifest

Pin the selected files only after visual review. A useful directional manifest records:

- path and SHA-256;
- game/source geometry contract;
- fixed-camera landmark such as `screen-bottom underframe`;
- direction-specific equipment order or other visible identity landmarks;
- review date and the exact contact/live evidence directory.

If any packaged PNG changes, invalidate the approval and final live evidence. Rerun only the focused
affected visual scenario during feature work; reserve full compatibility matrices for release or a
deliberate regression pass.
