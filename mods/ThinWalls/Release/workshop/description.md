# Thin Walls

Build walls and doors on tile edges instead of giving up the whole tile.

Thin Walls adds separate Stuff-selecting Structure tools for Thin Walls and Thin Doors. Before clicking, the one-cell edge preview can be rotated with Q or E. Once a drag enters a second cell, every piece follows the right-hand side of the origin-to-destination direction. The owner tile stays standable and can still hold floors, zones, items, colonists, and compatible buildings.

## Edge-sized architecture

- Thin Walls block pawn movement across their occupied edge while both adjacent tiles remain usable.
- Thin Doors open only when a pawn crosses their door-side; movement through the other sides of the tile is unobstructed.
- Multi-tile building footprints cannot cross completed, planned, or framed Thin Walls or Thin Doors.
- North, east, south, and west Thin Walls can coexist on one tile when they occupy different edges. Each undirected shared edge accepts exactly one Thin Wall or Thin Door phase, so reverse designation cannot create a malformed duplicate.
- Straight runs, L corners, T junctions, + junctions, Thin Doors, and regular walls connect visually. Exact contacts use square shoulders; nearby offset silhouettes do not connect.
- Regular walls keep their normal vanilla linked rendering outside the affected contact. The affected atlas state is extended deterministically into the Thin arm, including side-T contacts, without a pasted connector or diagonal wedge.
- Compatible workbenches and buildings can occupy both adjacent tiles when neither footprint crosses the shared edge.

Completed Thin Walls and Thin Doors divide ordinary RimWorld rooms and participate in room statistics and temperature containment. A Thin Door remains a room boundary while opening and exchanges heat only between the rooms across its own edge.

Thin Walls use 3 units of selected Stuff and have 150 base hit points—half a Core wall's base durability. Thin Doors use 13 Stuff and have 80 base hit points—half a Core simple door. Wall, door, damage, shadow, and junction surfaces are derived deterministically at runtime from the installed Core linked-wall materials; the mod ships no generated wall sprites.

They use normal RimWorld blueprints, hauling, frames, construction, damage, repair, cancellation, save/load, hold-open commands, and deconstruction.

## Important limits

Thin Walls and Thin Doors do not support roofs or create automatic roof areas, even when a room is enclosed. Room and temperature integration does not imply cover, projectile interception, line-of-sight blocking, animal-pen containment, gas, wind, light, or vacuum separation.

Regular walls and doors are not replaced and remain available whenever you need their full behavior.

## Requirements

- RimWorld 1.6
- Harmony

Adds package `fumblesneeze.thinwalls`. No optional mod integrations are required.
