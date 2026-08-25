## ADDED Requirements

Owning mod: **Thin Walls** (`fumblesneeze.thinwalls`) at `mods/ThinWalls`.

### Requirement: Completed shared edges block cardinal pawn movement
A completed Thin Wall on an undirected cardinal edge SHALL remove normal pawn traversal in both directions across that edge while leaving both adjacent cells individually standable. Either owner description addresses the same unique structure; the player cannot place a second opposite owner.

#### Scenario: Pathfinder routes around an exposed endpoint
- **WHEN** a drafted colonist receives a native move order to a cell across a finite completed Thin Wall line and an open route exists around an endpoint
- **THEN** the computed path goes around the endpoint and the colonist arrives without crossing any occupied wall edge

#### Scenario: Closed edge enclosure is unreachable
- **WHEN** completed Thin Walls form a closed cardinal edge enclosure around a colonist and the player gives a native move order outside it
- **THEN** RimWorld reports or exhibits no reachable path and the colonist never changes cells through the enclosure

#### Scenario: Collision is symmetric
- **WHEN** pawns on opposite sides of the same completed shared edge attempt native movement toward each other
- **THEN** neither pawn crosses that edge regardless of which adjacent cell owns the segment

### Requirement: Wall endpoints prevent diagonal corner slipping
Normal diagonal traversal SHALL be removed when the center-to-center diagonal intersects either endpoint of a completed thin-wall edge. Pawns SHALL still be able to walk around an exposed endpoint using an open sequence of cardinal cells.

#### Scenario: L junction blocks diagonal escape
- **WHEN** two completed perpendicular Thin Walls meet at an L endpoint and a pawn attempts a diagonal step through their common vertex
- **THEN** the diagonal connection is unavailable and the pawn must use a genuinely open route

#### Scenario: Exposed endpoint can be walked around
- **WHEN** a single finite Thin Wall ends beside otherwise open cells
- **THEN** a pawn can reach the other side by walking cardinally around the endpoint without crossing the wall segment

### Requirement: Path jobs consume an edge-aware connectivity snapshot
Normal `PathFinderJob` instances SHALL receive a map-sized connectivity snapshot based on vanilla RimWorld 1.6 connections with completed thin-wall cardinal and endpoint-crossing diagonal bits removed. The snapshot SHALL update after spawn, completion, destruction, deconstruction, load, and relevant vanilla path-data changes without mutating an array while a scheduled Burst job may read it.

#### Scenario: Completing a segment invalidates an existing route
- **WHEN** a pawn has an existing path that would cross an edge and construction completes a Thin Wall on that edge before the pawn enters the next cell
- **THEN** the stale movement is rejected, a new path is requested, and the pawn does not cross the new wall

#### Scenario: Destroying the structure reopens the edge
- **WHEN** the completed Thin structure on a shared edge is destroyed
- **THEN** the next safe path-data gather restores vanilla connections for that edge and a normal pawn can path across it

### Requirement: Reachability and immediate Touch checks respect thin walls
An accepted vanilla region-reachability answer SHALL be refined by the edge graph on maps containing completed Thin Walls, and immediate Touch reachability SHALL require at least one target adjacency not separated by a Thin Wall.

#### Scenario: Work cannot start through a wall edge
- **WHEN** a pawn stands adjacent to a target but the only Touch adjacency crosses a completed Thin Wall
- **THEN** immediate reachability is false and the pawn does not treat itself as arrived through the wall

#### Scenario: Alternate open adjacency remains usable
- **WHEN** a multi-cell target has another valid adjacent cell not separated by a Thin Wall
- **THEN** the pawn can path to and use that open adjacency

### Requirement: Bash-capable traversal attacks a concrete segment
A pawn whose active job explicitly permits door bashing SHALL be allowed to plan to the edge with destroyable traversal and SHALL receive an edge-aware blocker job against the one concrete `Building_ThinWall` on that edge instead of moving through it. Hostile identity alone SHALL NOT bypass the edge. The blocker job SHALL invoke the pawn's ordinary RimWorld melee verb and damage from the pawn's current side rather than directly mutating hit points.

#### Scenario: Bash-capable job attacks a blocking segment
- **WHEN** a pawn's explicitly bash-capable path reaches a completed Thin Wall edge
- **THEN** the pawn attacks the concrete blocking segment and crosses only after that structure is destroyed

### Requirement: Thin Door behavior applies only when crossing its edge
A completed Thin Door SHALL leave both adjacent cells normally standable and SHALL add no movement delay to a pawn that enters, leaves, or traverses either cell without crossing the door's shared edge. A pawn whose next cardinal step crosses the edge and can open the concrete door SHALL use RimWorld's ordinary manual-door opening wait and may cross only after the door has opened. A pawn that cannot open it SHALL path around it or report no path; an explicitly bash-capable pawn MAY attack the concrete door instead.

Because a completed Thin Door deliberately splits vanilla rooms and regions, an authorized cross-door request MAY recover a false vanilla region-reachability answer only when the edge graph accepts the complete traversal parameters and RimWorld's native pathfinder returns a concrete path that actually crosses a Thin Door. The recovery SHALL NOT promote an unrelated vanilla rejection, recursively validate a different request, or reuse a cached answer across a pathing revision, pawn-access change, or path-relevant traversal-parameter change.

#### Scenario: Colonist waits for the edge door
- **WHEN** a colonist receives a native move order whose path crosses a closed friendly Thin Door
- **THEN** the colonist stops on the current side, the two visible leaves open through the normal door delay, and only then does the pawn enter the opposite cell

#### Scenario: Walking inside the owner cell does not operate the door
- **WHEN** a colonist enters or leaves the Thin Door owner cell through any other open edge
- **THEN** the Thin Door remains uninvolved and the cell pays no door-opening delay

#### Scenario: Inaccessible Thin Door is routed around
- **WHEN** a pawn that cannot open a closed Thin Door has an alternate open path to the destination
- **THEN** its computed path uses that alternate route and never repeatedly attempts the closed door edge

#### Scenario: Hold-open and closing are visible
- **WHEN** the player toggles hold-open through the ordinary selected-door command and a colonist crosses the Thin Door
- **THEN** the door remains visibly open while held, and after hold-open is disabled it closes through ordinary door timing without obstructing either adjacent cell

### Requirement: Completed edge structures divide vanilla rooms and temperatures
Vanilla region generation SHALL NOT flood or create a `RegionLink` across a shared edge occupied by a completed Thin Wall or Thin Door. The resulting Core `Room` objects SHALL drive ordinary indoor/outdoor classification, room cell counts, room roles/stats, and temperature trackers. Thin Door opening SHALL NOT merge room identities, matching vanilla door portals, but its temperature exchange SHALL involve only the two rooms across its owned edge. Maps without a completed Thin edge structure SHALL retain vanilla region generation unchanged.

#### Scenario: Thin Wall enclosure creates an indoor room
- **WHEN** completed Thin Walls form a closed edge perimeter around roofed usable cells without touching the map edge
- **THEN** those cells share one Core Room that does not touch the map edge, excludes surrounding cells, reports the expected cell count, and exposes ordinary room stats/role behavior

#### Scenario: Thin Wall prevents temperature leakage
- **WHEN** two roofed rooms with different temperatures are separated only by a completed Thin Wall edge
- **THEN** they retain distinct Room identities and temperatures rather than equalizing through that edge

#### Scenario: Thin Door exchanges only across its edge
- **WHEN** a Thin Door separates two rooms and a third room is cardinally adjacent to its owner cell but not across the door edge
- **THEN** closed/open door temperature exchange affects the two rooms across the owned edge only and never the unrelated third room

#### Scenario: Removing the final edge remerges rooms
- **WHEN** the player deconstructs the final Thin Wall or Thin Door separating two otherwise contiguous regions
- **THEN** the next native region rebuild places both cell sets in one Core Room and preserves ordinary contents/stats

#### Scenario: Vanilla rooms remain unchanged on an unaffected map
- **WHEN** a map contains no completed Thin Wall or Thin Door
- **THEN** `RegionMaker.TryGenerateRegionFrom` follows the unmodified vanilla path and ordinary wall/door rooms behave exactly as before

### Requirement: Building footprints cannot cross thin-wall edges
Native building placement SHALL reject any rotated occupied footprint whose internal cardinal adjacency crosses a completed, blueprinted, or framed Thin Wall. The same footprint SHALL remain placeable when it lies wholly on one side and otherwise satisfies vanilla rules.

This predicate SHALL apply identically to Thin Walls and Thin Doors, both canonical owner descriptions, every north/east/south/west orientation, and every rotation of a non-square footprint. It SHALL enumerate the final occupied `CellRect` internal edges rather than infer collision from the owner cell, rendered alpha, or visual displacement. A render-only displacement SHALL never make a crossing footprint legal or make an exterior-only footprint illegal.

#### Scenario: Multi-cell building crossing a completed wall is rejected
- **WHEN** the player previews or designates a two-or-more-cell building with occupied cells on both sides of a completed Thin Wall edge
- **THEN** the ghost is rejected with a localized crossing reason and no blueprint is created

#### Scenario: Planned wall also reserves its edge
- **WHEN** a Thin Wall blueprint or frame reserves an edge and the player tries to designate a building footprint across it
- **THEN** building placement is rejected before either construction can create an invalid overlap

#### Scenario: Thin wall cannot be planned through an existing footprint
- **WHEN** an existing completed building, blueprint, or frame spans an edge and the player tries to designate a Thin Wall on that edge
- **THEN** Thin Wall placement is rejected and the existing constructible is unchanged

#### Scenario: Same-cell one-cell building is allowed
- **WHEN** a one-cell building is designated in a Thin Wall owner cell and its footprint crosses no cell edge
- **THEN** ordinary placement remains accepted and construction preserves both Things

#### Scenario: Multi-cell building wholly on one side is allowed
- **WHEN** a multi-cell building includes a Thin Wall owner cell but none of its internal adjacencies crosses that wall's edge
- **THEN** the Thin Wall adds no placement rejection beyond vanilla rules

#### Scenario: Every rotated internal edge is rejected
- **WHEN** the player rotates a non-square building through all four rotations and any completed, blueprinted, or framed Thin Wall or Thin Door occupies one of the resulting footprint's internal cardinal adjacencies
- **THEN** every crossing orientation is rejected before designation, while the same edge on the final footprint perimeter is not rejected by Thin-edge geometry
