## ADDED Requirements

Owning mod: **Thin Walls** (`fumblesneeze.thinwalls`) at `mods/ThinWalls`.

### Requirement: One tool provides a rotatable click preview and right-hand line drags
Thin Walls SHALL expose one Stuff-selecting Structure build command. While the tool is active but not dragging beyond its start cell, the hovered cell SHALL show one edge preview in the tool's selected cardinal orientation. Q/E SHALL rotate that preview through north/east/south/west, and a click/release that never leaves the start cell SHALL designate exactly that previewed edge.

Only after the active drag contains at least two distinct cells SHALL the drag vector override the selected click orientation. Every segment in that multi-cell cardinal line SHALL then occupy the edge to the right of the origin-to-destination vector. A one-cell mouse movement/jitter inside the start cell, mouse-down, or return to the start cell SHALL NOT accidentally change the stored click orientation. A newly selected tool SHALL default to South, and the selected orientation after any multi-cell drag MAY remain as the resulting right-hand side until the player rotates it again.

#### Scenario: Eastward drag owns south edges
- **WHEN** the player drags the Thin Wall command east from an origin cell to a destination cell
- **THEN** one constructible segment is designated on the south edge of every cell in the resulting cardinal line

#### Scenario: Cardinal right-hand mapping
- **WHEN** the player drags north, west, or south
- **THEN** the designated owned edges are respectively east, north, or west of every line cell

#### Scenario: One-cell placement retains direction
- **WHEN** the player rotates the hover preview to North with Q/E and clicks without dragging into a second cell
- **THEN** one north-edge segment is designated exactly where previewed

#### Scenario: Rotation input affects only click orientation
- **WHEN** the player rotates the single-cell preview, then drags into a second cell toward the east
- **THEN** the multi-cell preview changes to South because the right-hand drag rule takes authority

#### Scenario: Click jitter cannot change orientation
- **WHEN** the player presses on the previewed cell and releases without the dragger producing a second distinct cell
- **THEN** the designated edge remains the previewed orientation even if pointer motion occurred inside that cell

### Requirement: Unique shared edges coexist without losing identity
Each north, east, south, or west segment SHALL be an independently selectable, constructible, damageable, deconstructible, and persisted Thing when it occupies a different canonical `SharedEdge`. The game SHALL allow up to four differently oriented segments at one cell, but SHALL allow exactly one Thin Wall or Thin Door constructible phase on each undirected shared edge. The opposite owner description from the adjacent cell is the same physical placement slot, not a second owner.

#### Scenario: Four orientations coexist on one cell
- **WHEN** the player validly designates and constructs north, east, south, and west thin walls owned by the same cell
- **THEN** all four Things remain present with independent hit points and the cell remains standable

#### Scenario: Same-owner duplicate is rejected
- **WHEN** a completed, blueprinted, framed, or otherwise pending north-edge segment already belongs to a cell and the player tries to designate that same cell's north edge again
- **THEN** placement is rejected with a localized occupied-edge reason and no resources or existing Things are changed

#### Scenario: Opposite-owner designation is rejected
- **WHEN** one cell already owns its north shared edge in any constructible phase and the player addresses that same edge as the adjacent northern cell's south edge
- **THEN** native preflight rejects the second designation with the same localized occupied-edge reason and no second Thing, designation, resource cost, Stuff partition, or hit-point pool is created

#### Scenario: Forward and reverse drags cannot double an edge
- **WHEN** two valid line drags in opposite directions would resolve one segment to the same canonical shared edge
- **THEN** the first designation remains and the second is refused even though its `OwnedEdge` cell/side description differs

### Requirement: Thin walls use half wall durability and ceiling-half material cost
The `TW_ThinWall` Def SHALL use the same Metallic, Woody, and Stony Stuff categories as Core `Wall`, SHALL cost `ceil(5 / 2) = 3` units of the selected Stuff, and SHALL have base maximum hit points `300 / 2 = 150`. Normal Stuff stat factors SHALL apply to both values through RimWorld's native construction/stat system.

#### Scenario: Matching Stuff comparison
- **WHEN** the player inspects a Thin Wall and a Core wall made from the same Stuff
- **THEN** the Thin Wall blueprint requires 3 Stuff instead of 5 and its base durability contribution is 150 instead of 300 before the same Stuff factors

#### Scenario: Native construction consumes the declared cost
- **WHEN** a colonist completes an ordinary Thin Wall blueprint using one selected Stuff
- **THEN** exactly 3 units of that Stuff are consumed and one Thin Wall Thing is produced

### Requirement: Thin Doors use the same owned-edge construction model
Thin Walls SHALL expose a separate Stuff-selecting Thin Door command using the same right-hand cardinal line mapping and free-cell construction lifecycle. `TW_ThinDoor` SHALL use the Core simple door's Metallic, Woody, and Stony Stuff categories, SHALL cost `ceil(25 / 2) = 13` Stuff, SHALL have base maximum hit points `160 / 2 = 80`, and SHALL remain standable, non-edifice, non-roof-supporting, and independently selectable on its owned edge.

#### Scenario: Thin Door is built on the designated edge
- **WHEN** the player drags the Thin Door command east and an eligible colonist completes an ordinary blueprint
- **THEN** one Thin Door is built on each designated south edge, each consumes 13 selected Stuff, and both adjacent cells remain ordinarily usable

#### Scenario: One boundary element occupies every shared edge
- **WHEN** a Thin Wall or Thin Door already occupies either owner description of one shared edge
- **THEN** every Thin Wall or Thin Door designation on that shared edge is rejected regardless of phase or direction

#### Scenario: Thin Door lifecycle persists
- **WHEN** a Thin Door is opened, set to hold open, damaged, saved, and reloaded
- **THEN** the same owned-edge Thing, Stuff, hit points, open/hold state, and edge identity are restored exactly once

### Requirement: The owning cell remains ordinarily usable
A completed, blueprinted, or framed thin wall SHALL NOT make its owner cell unstandable, add movement cost inside the cell, cover or replace its floor, erase items, prevent compatible zones, or prevent a building from occupying that cell when the building footprint does not cross an occupied thin-wall edge. This includes a one-cell full building such as a regular wall; that building's own ordinary passability still applies.

The same rule applies independently to both adjacent cells. Workbenches and other compatible one-cell or multi-cell buildings MAY occupy either or both sides when no internal footprint adjacency crosses the Thin edge. Neither owner-side choice nor the visual projection may reserve one side as unusable. Their completed, blueprint, and frame graphics MAY receive only the bounded render displacement defined by the rendering capability; their logical cells, interaction cells, jobs, bills, reservations, reachability, and room identity SHALL remain unchanged.

#### Scenario: Pawn and one-cell building share the owner cell
- **WHEN** a Thin Wall exists on one edge of a standable cell and the player constructs a compatible one-cell building in that cell
- **THEN** the building is constructed, the Thin Wall remains, and a colonist can stand on the cell

#### Scenario: Workbenches occupy both sides
- **WHEN** the player places compatible workbenches in both cells adjacent to a Thin Wall and neither footprint crosses that shared edge
- **THEN** both native placements are accepted, both buildings remain usable, and the Thin Wall remains on their common boundary

#### Scenario: Full one-cell building shares the owner cell
- **WHEN** the player designates a regular one-cell Core wall in a Thin Wall owner cell
- **THEN** the Core wall and Thin Wall remain independently present because the one-cell footprint crosses no edge
- **THEN** the Core wall's ordinary impassability applies while it exists

#### Scenario: Floor and zone survive construction
- **WHEN** the player constructs a Thin Wall on a floored cell inside a compatible zone
- **THEN** the floor and zone remain unchanged and no item in the cell is wiped solely by Thin Wall construction

### Requirement: Native lifecycle and save/load preserve edge state
Blueprint delivery, frame work, completion, cancellation, construction failure, damage, repair, deconstruction, destruction, and Scribe save/load SHALL use ordinary RimWorld Things. Rebuilt edge indexes SHALL derive from those Things and SHALL NOT create, drop, merge, or duplicate segments.

#### Scenario: Blueprint completes through ordinary Construction work
- **WHEN** a player designates a Thin Wall with god mode disabled and an eligible colonist delivers resources and finishes construction
- **THEN** the visible blueprint becomes a frame and then one completed segment on the same cell edge

#### Scenario: Cancellation refunds without affecting co-located content
- **WHEN** the player cancels a Thin Wall blueprint or frame sharing a cell with other allowed content
- **THEN** RimWorld applies its ordinary refund rules only to that segment and the co-located content remains

#### Scenario: Native save/load restores unique edges
- **WHEN** the player saves and reloads a map containing several orientations on one cell and no duplicated shared edge
- **THEN** the same segment Things, shared edges, Stuff, and hit-point values are restored exactly once and opposite-owner duplicates are not synthesized

### Requirement: Thin walls never support or request roofs
`TW_ThinWall`, its blueprint, and its frame SHALL NOT hold a roof, extend roof-support distance, or create an automatic roof area.

#### Scenario: Unsupported roof collapses despite a completed thin wall
- **WHEN** a real roof is temporarily supported by a qualifying structure, a completed Thin Wall remains under that roof, and the player removes the last qualifying support through the native deconstruction path
- **THEN** the roof is no longer supported and the Thin Wall does not prevent normal roof collapse

#### Scenario: Thin edge is not a column
- **WHEN** a completed Thin Wall occupies an edge below a roof and no qualifying roof support is in range
- **THEN** it contributes zero roof support

### Requirement: Room integration does not create roof support or unrelated wall behavior
Completed Thin Walls and Thin Doors SHALL divide RimWorld rooms and room temperatures across their shared edge. They SHALL continue to provide zero roof support and no automatic roof area, and SHALL NOT claim cover, projectile interception, line-of-sight, animal-pen, gas, wind, light, or vacuum separation.

#### Scenario: Player-facing description states the exact boundary
- **WHEN** the player reads the Thin Wall or Thin Door info description or Workshop mechanics text
- **THEN** it describes movement, room/temperature, and crossing-footprint behavior; states that Thin Doors open only for crossing their edge; and does not claim roof support or unrelated environmental/ballistic behavior
