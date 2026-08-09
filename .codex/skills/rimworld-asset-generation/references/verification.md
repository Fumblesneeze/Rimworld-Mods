# Visual verification and acceptance

## Static/package checks

Before launching RimWorld, run focused tests that assert the contract actually at risk:

- expected diffuse/cardinal/mask files exist and are staged byte-for-byte;
- exact width, height, aspect, alpha bounds, and transparent corners;
- diffuse/mask alpha equality and meaningful red/black regions;
- no vivid background chroma or silhouette fringe;
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

## Final native workflow

On the independently reviewed Release package:

1. Start a fresh isolated saved-data folder with the smallest exact mod list.
2. Hash normal config before/after and bind every action/screenshot to the launched PID.
3. Place the object through the native Architect/designator, crafting, hauling, job, or gizmo path.
4. For a cardinal building, place north/east/south/west copies beside same-facing Core workbenches.
5. Capture before placement, the player action, and close after views at one comparable zoom.
6. Select each result and inspect the actual texture/material state. Explicitly inspect the
   north-interaction (`Rot4.South`) view for a screen-bottom underframe.
7. For Stuff masks, observe wood/stone/metal or other materially distinct Things; verify fixed
   accents do not tint and dirty overlays remain visible.
8. Personally view the screenshots. Record concrete observations, not just automation assertions.
9. Require a clean relevant log, exact build/package hash, restored settings, sanitized artifacts,
   and graceful exact-PID shutdown.

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
