## Why

RimWorld walls consume an entire cell, which prevents compact layouts where a boundary should occupy only the edge between two otherwise usable cells. Thin Walls adds an explicit edge-based construction system whose movement, building-placement, damage, and rendering behavior remains understandable through ordinary player workflows.

## What Changes

- Add a new RimWorld 1.6 mod, Thin Walls, without replacing or modifying the player's access to regular walls.
- Add one Architect build tool for dragging straight cardinal runs of thin walls; each segment occupies the cell edge to the right of the origin-to-destination vector.
- Allow pawns and ordinary single-cell or multi-cell buildings to occupy the cells beside thin walls while blocking movement and building footprints that cross an occupied edge.
- Allow independently constructed north, east, south, and west thin-wall segments to coexist on one cell, while treating the two opposite owner descriptions of one undirected shared edge as one physical placement slot and rejecting either description once any wall/door phase occupies it.
- Make a thin-wall segment cost half as much material and have half the hit points of the equivalent regular wall made from the same Stuff, with deterministic integer rounding recorded in the gameplay contract.
- Never let a thin wall, blueprint, or frame count as roof support; roof support continues to come only from vanilla and other qualifying structures.
- Add Stuff-selecting Thin Doors on the same owned-edge model: the adjacent cells remain normally usable, while crossing that door edge invokes ordinary door opening, waiting, hold-open, closing, and access behavior.
- Make completed Thin Walls and Thin Doors divide RimWorld rooms and preserve separate room statistics and temperatures across their edge, while still contributing no roof support or automatic roof area.
- Render Thin Walls, Thin Doors, and their contacts with regular walls through one deterministic linked-topology system derived at runtime from RimWorld's own wall materials. Every local direction is absent, ordinary-width, or thin-width; the final silhouette is composed once for straight, corner, T, cross, door, and mixed ordinary/thin combinations. Exact endpoint/perimeter contacts use square butt unions, including Thin runs entering the side of continuous regular-wall runs; offset or parallel near-misses remain unconnected. Regular wall pixels outside the exact union aperture remain vanilla, while the narrow contact loses its separating outline and the regular wall keeps its ordinary linked silhouette. No generated wall image, endpoint post, seam stitch, diagonal ramp, miter, wedge, chamfer, bridge decal, or synthetic cap is shipped or rendered.
- Persist constructed segments and rebuild edge/pathing caches correctly across save/load, destruction, deconstruction, construction cancellation, and map removal.
- Add focused host tests, isolated finalized-loader checks, native construction/pathing/placement E2E workflows, and product-only startup evidence.
- Add release-ready English/German player text, an About preview, Workshop preview cards, and source descriptions for publishing.

This change specifies and implements the gameplay, verification, and publishing artifacts in one owned mod.

## Capabilities

### New Capabilities

- `thin-wall-construction`: Player designation, construction, material cost, durability, edge identity, coexistence, cancellation, destruction, and save/load behavior.
- `thin-wall-collision`: Edge-aware pawn pathfinding and execution, Thin Door traversal, prevention of building footprints that cross completed, planned, or in-progress edge structures, and integration with RimWorld rooms and temperatures.
- `thin-wall-rendering`: Core-derived linked edge geometry, square hybrid ordinary/thin wall topology, Thin Door motion, attached damage, shadows, and L/T/+ visual junctions.
- `thin-wall-publishing`: Localized metadata, publishing descriptions, About preview, Workshop feature cards built from accepted in-game captures, deterministic non-generative composition, and package presentation checks.

### Modified Capabilities

None. Thin Walls is a new independently owned mod.

## Impact

- Adds `mods/ThinWalls`, its product assembly, Defs, localization, deterministic rendering code, release sources, and package metadata. Thin-wall structural art is derived from installed Core materials at runtime and does not redistribute Core pixels.
- Adds owning host, in-game integration, and end-to-end test projects under `tests/`, registered in the repository build/test tooling.
- Adds narrow Harmony patches at RimWorld's path expansion, movement execution, placement validation, construction lifecycle, and map invalidation seams; Harmony remains the only required third-party mod.
- Adds an edge registry and renderer maintained per map. No code or metadata dependency on `fumblesneeze.rimworlddevgateway` is introduced.
- Uses isolated RimWorld processes and repository-local build output for verification; the user's normal configuration and Workshop content remain unchanged.

## Affected Mods

- **Thin Walls** (`fumblesneeze.thinwalls`) at `mods/ThinWalls`: new playable mod; all behavior and publishing artifacts in this change are implemented for RimWorld 1.6.
