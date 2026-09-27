## ADDED Requirements

Owner: **Thin Walls** (`fumblesneeze.thinwalls`), `mods/ThinWalls`.

### Requirement: Thin pathing retains only owned-edge state
Thin Walls SHALL NOT retain a duplicate whole-map reachability graph or persistent connectivity grid. Its persistent pathing state SHALL scale with completed owned edges and their affected cells. Ordinary reservation, path-cost and regular-door events SHALL NOT reconstruct whole-map Thin data. Native connectivity ownership and scheduled-reader lifetime SHALL be preserved.

#### Scenario: One wall does not impose full-map rebuilds
- **WHEN** ordinary pawns move, reserve work and open regular doors on a large map with a fixed small Thin Wall layout
- **THEN** Thin-owned retained connectivity entries remain bounded by those edges, and no Thin whole-map graph is constructed

#### Scenario: Overlapping masks and native blocked directions remain correct
- **WHEN** intersecting Thin edges are added and removed beside native blocked connections
- **THEN** removing one edge preserves the remaining edge's cardinal/diagonal blocks and never enables a direction that native connectivity had already disabled

#### Scenario: First and final edges update safely
- **WHEN** the player constructs a Thin Wall across a route and later removes the last edge, including a same-tick path request
- **THEN** movement detours while blocked and crosses after removal without stale masks, changed logical footprints or invalid native memory

#### Scenario: Temporary permission buffers outlive their readers but not their map
- **WHEN** a permission-specific native path request needs a temporary buffer and Core unregisters map components before disposing its pathfinder
- **THEN** the buffer remains valid until native readers complete and is released during pathfinder disposal even after hot-path component lookup has been unregistered

#### Scenario: A local region split preserves boundary links
- **WHEN** the first Thin edge is built inside a previously generated native region chunk, or a divider ends exactly at its boundary
- **THEN** both sides of each affected native boundary retain reciprocal links and a pawn can use the open route through the neighboring chunk

### Requirement: Native connectivity and optional nonlocal links remain authoritative
Thin Walls SHALL reuse native edge-separated regions, reachability caches and native/mod RegionLinks rather than deriving a second graph from neighboring cells. With As Above So Below II installed, valid cross-level routes SHALL remain available when Thin Walls exist elsewhere. A Thin Wall enclosing the stair entrance SHALL still prevent access.

#### Scenario: Cross-level native move with a distant thin wall
- **WHEN** a player orders a pawn through a linked installed-mod staircase with a completed Thin Wall elsewhere
- **THEN** the native move option remains enabled and the pawn reaches the destination through the actual staircase

#### Scenario: Blocked stair entrance
- **WHEN** Thin Walls close every usable approach to the staircase
- **THEN** the pawn cannot use that staircase through the enclosing edges

### Requirement: Sparse thin-door bridges preserve traversal permissions
A native negative caused by separated Thin Door rooms SHALL be recovered only through permitted owned crossings and native-reachable legs under the original traversal parameters. Unrelated native rejection SHALL NOT be promoted. Door permission changes SHALL take effect without a stale map graph. Explicit bashing SHALL still attack a concrete blocking structure.

#### Scenario: Two doors and a native segment
- **WHEN** an authorized pawn is ordered through two Thin Doors separated by an ordinary region or a supported native mod connection
- **THEN** it reaches the destination through ordinary movement/opening and neither door bridge bypasses a native obstruction

#### Scenario: Forbidden exit and usable owner side
- **WHEN** a Thin Door is forbidden, a pawn walks along its owner side, and the player then allows crossing
- **THEN** parallel owner-side movement remains usable, forbidden crossing remains unavailable, and allowed crossing opens the door normally

#### Scenario: A bridge exit is not an exemption from destination restrictions
- **WHEN** the bridge exit itself is a water-forbidden or danger-forbidden destination, or a no-closed-doors traversal encounters a closed Thin Door
- **THEN** the recovery rejects that route under the original traversal parameters; native immediate-reach shortcuts do not grant an exemption created by the bridge

#### Scenario: A synthetic endpoint remains transit toward a safe destination
- **WHEN** a Danger.None pawn can traverse a Danger.Some room through a regular door to a Thin Door and the final destination beyond it is safe
- **THEN** the intermediate bridge endpoint is evaluated as transit, the safe final destination remains reachable, and the original danger limit is not raised

### Requirement: Local edge checks cover native reachability variants
Touch targets SHALL require a legal touching cell, including non-immediate region destination collection. Native cell-based destroyable/water traversal SHALL retain its native predicate and additionally reject blocked Thin crossings unless explicit bashing permits them. Endpoint diagonal and stale-path guards SHALL remain effective.

#### Scenario: Touch cannot select a region through the wall
- **WHEN** a work target has no legal reachable Touch adjacency except through a Thin Wall
- **THEN** a non-adjacent pawn is not offered reachable work through that edge

#### Scenario: Special traversal does not silently cross thin edges
- **WHEN** a non-bashing destroyable or no-closed-doors-or-water request targets a closed Thin enclosure
- **THEN** native reachability and movement reject the crossing, while an explicitly bash-capable job can attack the real segment
