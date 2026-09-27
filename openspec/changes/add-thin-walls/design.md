## Context

Thin Walls is a new RimWorld 1.6 product mod owned exclusively by **Thin Walls** (`fumblesneeze.thinwalls`) at `mods/ThinWalls`. The installed design target is RimWorld `1.6.4871 rev590`; the inspected `Assembly-CSharp.dll` SHA-256 is `5CF1B5BE399D5B1C9C56CA72C9D35B4ECF307FEACF5859D04AC5A1AA5926356A`.

Vanilla RimWorld represents walkability, edifices, rooms, and construction footprints primarily per cell. A thin wall instead occupies one undirected cardinal edge between two cells. Either adjacent cell/side pair may describe that edge during designation, but there is only one physical placement slot. Up to four differently oriented segments may coexist with a pawn or ordinary building on one cell when they occupy four different shared edges.

RimWorld 1.6's Burst-compiled `PathFinderJob` already consumes a `NativeArray<CellConnection>` containing eight directional connection bits per cell. This is the stable engine seam for an edge blocker; transpiling the Burst job or pretending an edge wall is an impassable cell would either be unreliable or violate the core free-cell requirement.

The product requires Harmony because placement, path-job parameterization, immediate reachability, stale-path execution, construction wiping, and frame rendering have no complete XML/public extension surface. The product never references or ships the Dev Gateway. The Gateway and dynamically staged tests are verification tools only.

## Rendering authority after the 2026-09-04 pivot

The user requires all gameplay art to derive dynamically from installed base-game textures, with no
mod-owned textures. The current CPU-painted runtime raster implementation is rejected, not an accepted
implementation of this pivot. The source-bound mesh plan in
`references/2026-09-04-core-material-mesh-pivot.md` and the current rendering capability supersede
earlier raster/cache/decoration prescriptions and visual approvals. Prior drawings remain geometry
references; the older implementation plans are historical. Gameplay, no-roof, native workflows,
two-sided building use, and in-game publishing gates remain required.

## Goals / Non-Goals

**Goals:**

- Expose one Stuff-selecting Structure designator that places straight cardinal runs and derives their owned edge from the right-hand side of the drag vector.
- Preserve one segment's identity, hit points, damage, cancellation, construction, deconstruction, and save/load on each unique undirected collision edge.
- Keep the owning cell standable and otherwise available for floors, zones, items, pawns, one-cell buildings, and compatible portions of multi-cell buildings.
- Prevent native player placement of any building footprint whose internal cell adjacency crosses a completed, blueprinted, or framed thin-wall edge.
- Remove blocked cardinal and endpoint-crossing diagonal connections from normal pawn path jobs, reject stale crossings at movement execution, and let destroyable-traversal jobs attack the encountered segment rather than pass through it.
- Render Core-derived narrow Stuff-tinted edge structures as visibly continuous straight, L, T, and + unions plus square west/east/north/south regular-wall end and side contacts, without diagonal width transitions.
- Add edge-mounted Thin Doors that leave both adjacent cells free, use ordinary RimWorld door access/opening delays, and open only for a path step that crosses their owned edge.
- Split vanilla regions and rooms at completed Thin Wall and Thin Door edges so room identity, room stats, roles, and temperature containment match the visible boundary.
- Explicitly provide no roof support and no automatic roof area.
- Produce reproducible, player-facing About/Workshop art and descriptions plus native in-game evidence for the reviewed package.

**Non-Goals:**

- Replacing, patching out, or changing vanilla walls, doors, fences, or columns.
- Diagonal or curved edge segments, adjustable thickness, powered Thin Doors, automatic conversion of existing walls, or optional-mod integrations.
- Treating Thin Walls as cover, projectile interception, line-of-sight, animal-pen, gas, wind, light, or vacuum boundaries. Room identity, room statistics, roles, and temperature are included; those unrelated systems require their own explicit edge-aware contracts.
- Making thin walls support roofs.
- Publishing or mutating a Steam Workshop item. This change prepares reviewed publishing inputs only.

## Decisions

### 1. Use owned cardinal designations and unique canonical shared-edge keys

The domain model uses `OwnedEdge(cell, side)` where `side` is North, East, South, or West. `SharedEdge` canonicalizes the two adjacent cells plus axis, so `(cell, North)` and `(cell + North, South)` address the same placement, collision, rendering, room, and save identity.

Up to four `OwnedEdge` values may exist at one cell when their `SharedEdge` values differ. Placement rejects any completed building, blueprint, frame, or pending constructible phase whose `SharedEdge` equals the candidate, including the opposite owner description from the adjacent cell. Direction reversal may select the same physical edge but cannot create a second Thing.

Alternative considered: allow two Things to own opposite descriptions of the same edge. Rejected by the user's 2026-08-18 designation reference because the duplicate produces ambiguous construction and malformed rendering. Legacy/corrupt doubled data may be handled defensively, but the player workflow never creates it and no product visual depends on it.

### 2. Combine a rotatable click preview with right-hand multi-cell drag orientation

`Designator_ThinWall` is a parameterless special Structure designator backed by `TW_ThinWall`; the Def disables its default `Designator_Build`. A Thin-Walls-owned `DrawStyleCategoryDef` exposes only the vanilla cardinal `Line` worker. Outside a multi-cell drag, `placingRot` is the authoritative single-cell orientation and ordinary Q/E designator rotation cycles it through the four edges; ghost and highlight rendering use it directly. Mouse-down does not change it while the drag remains on one cell.

Once the drag contains a second distinct cell, the designator reads the drag origin/current endpoint, chooses the dominant cardinal axis exactly as `DrawStyle_Line` does, and maps the vector's right side as follows:

| Drag direction | Owned edge |
| --- | --- |
| East | South |
| North | East |
| West | North |
| South | West |

A newly selected tool defaults to South. A single click uses the Q/E-selected edge. Multi-cell right-hand orientation may become the next hover orientation after release, but pointer jitter inside the start cell or returning to that cell cannot overwrite an explicitly selected click edge. Drag previews draw the actual edge structures rather than full-cell ghosts.

Alternative considered: four separate build commands. Rejected because one rotatable click preview provides the same control while preserving the requested origin-to-destination rule for lines. Arbitrary angled lines are rejected because one constant cell-edge side cannot represent them without discontinuous or ambiguous stair steps.

### 3. Represent every phase with ordinary constructible Things

`TW_ThinWall` is a Stuff-made, standable, non-edifice `Building_ThinWall` with explicit `holdsRoof=false`, `allowAutoroof=false`, zero path cost, no floor coverage, and zone coexistence. The base wall comparison is Core `Wall`: cost `5`, base maximum hit points `300`. Thin Walls uses ceiling-half material cost `3` and base maximum hit points `150`; Stuff factors apply normally to both.

Blueprints use `Blueprint_ThinWall` and retain edge rotation. Frames remain vanilla `Frame` Things and are recognized through `entityDefToBuild`. All phases therefore use RimWorld's native resource delivery, construction work, cancellation, failure, damage, deconstruction, selection, and Scribe persistence. The derived map index is rebuilt from spawned Things and is never an independently serialized source of truth.

Alternative considered: serialize edge records only in a `MapComponent`. Rejected because it would require recreating construction, damage, selection, ownership, save compatibility, and resource refunds outside native gameplay.

### 4. Make placement edge-aware while preserving cell coexistence

A prefix on `GenConstruct.CanPlaceBlueprintAt_NewTemp` first applies a pure `ThinWallPlacementRules` check:

- thin-wall placement rejects an out-of-bounds opposite cell, an exact owned-edge duplicate, or a completed/planned/in-progress crossing footprint;
- ordinary building placement rejects any internal cardinal adjacency of its rotated occupied rectangle that has a completed, blueprinted, or framed thin wall;
- overlap checks ignore thin-wall phases after the edge-crossing check passes, so even a one-cell full building may use the owner cell exactly as it could use an otherwise free cell.

`GenSpawn.SpawningWipes` is forced false between thin-wall constructible phases and otherwise compatible Things so blueprint-to-frame-to-building transitions never erase another orientation, a floor, or a co-located building. Construction blocking is guarded by the same public placement rules, and shared-edge uniqueness prevents an opposite-owner phase from being created.

Alternative considered: four invisible attachment buildings. Rejected because vanilla attachments still inherit cell placement/wiping assumptions and cannot validate arbitrary multi-cell footprint crossings.

### 5. Overlay vanilla path connectivity before Burst jobs are scheduled

`ThinWallMapComponent` maintains a `NativeArray<CellConnection>` with the same length as the map's vanilla connectivity. A postfix on `PathFinderMapData.GatherData` rebuilds the overlay only after vanilla data sources finish and only when path data changed. The rebuild copies vanilla connectivity, then removes:

- both directions of the cardinal connection crossing each completed shared edge; and
- both directions of the four diagonal pairs whose center-to-center segment intersects either endpoint of that wall edge.

Inclusive endpoint blocking prevents diagonal corner slipping through a wall endpoint or an L/T/+ junction; pawns can still walk around an exposed endpoint by taking cardinal cells around it.

A postfix on `PathFinderMapData.ParameterizePathJob` supplies the overlay to normal path jobs. Only a request that explicitly permits door bashing receives vanilla connectivity so its bash path can reach the wall; hostile identity alone and a generic destroyable traversal request without executing bash permission remain edge-blocked. The overlay is updated only at the pathfinder's gather/scheduling boundary, avoiding mutation while Burst jobs may be reading an earlier array. A wall change dirties both adjacent path cells and invalidates edge-reachability labels.

Alternative considered: transpile `PathFinderJob.Execute`. Rejected because the installed job is Burst-compiled and a Harmony-managed IL patch is not a reliable native-job contract. Alternative considered: mark the owner cell impassable. Rejected because it violates pawn/building coexistence.

### 6. Guard reachability and movement execution

The 2026-09-05 live stall investigation found repeated `Map.GetComponent<ThinWallMapComponent>` list scans even on a map with no completed Thin structures. Use a map-lifetime component lookup and retain the existing no-structure reachability exit. Apply the same inactive-map exit before connectivity allocation, copying and job substitution. Keep previously published buffers alive until the native scheduled-reader completion/disposal boundary; removing the last edge selects native connectivity rather than prematurely freeing a published array.

Necessary door-access enumeration uses the maintained native list directly, without sorting it on every request or constructing a filtered temporary list. Create a private request snapshot only when the first inaccessible door requires one. Closed doors and inactive countdowns do not need owner-cell occupancy scans. Remove the duplicate owner-region dirty call because the required walkability notification already covers its neighborhood. Preserve the edge graph, full traversal-parameter checks and native door recovery in this slice; replacing that graph or narrowing the engine's connectivity invalidation requires separate behavior evidence.

Vanilla region reachability can produce false positives because it is cell/region based. A postfix on `Reachability.CanReach` refines an accepted answer through a reusable edge-aware subregion graph: cells are component-labeled inside each vanilla Region, Thin Wall edges split those components, and query traversal still calls `Region.Allows` with the complete `TraverseParms`. Only explicitly bash-capable traversal remains allowed through a segment. A deliberate Thin Door room split can instead make vanilla return false for an authorized crossing. That one case is recovered only after the edge graph accepts and an exact-request native `FindPathNow` returns a real path that actually crosses a concrete Thin Door. An exact-request re-entrancy scope admits only the path request's nested validation; tick-, pathing-revision-, complete-`TraverseParms`-, and door-access-scoped caches prevent the recovery from promoting unrelated vanilla failures or rebuilding the full graph for every repeated query.

`ReachabilityImmediate.CanReachImmediate` rejects Touch adjacency when every relevant step crosses a thin wall. Prefixes on `Pawn_PathFollower.SetupMoveIntoNextCell` and `TryEnterNextPathCell` reject stale normal paths before tweening or changing `Pawn.Position`. A pawn whose active job explicitly permits door bashing plans through destroyable traversal and receives an edge-aware blocker job against the concrete `Building_ThinWall` Thing when it reaches the edge. That job invokes the pawn's ordinary RimWorld melee verb and damage while remaining on its current side; it does not directly mutate wall hit points.

Alternative considered: rely only on the pathfinder overlay. Rejected because already-issued paths, immediate Touch checks, and cached region answers can otherwise cross or act through a newly completed segment.

### 7. Remap native wall-atlas materials onto one mixed-width mesh

Resolve the installed Core wall appearance for each participant, retain its material/texture/mask/shader
identity, and use the native linked-atlas UV transform. Compile explicit disjoint top/front/side/contour
regions with shared target vertices; remap source UVs rather than painting new pixel arrays. Thinness
changes the top footprint, not the measured face height or lateral projection. Horizontal and
north-south forms have separate fixed-camera maps. The complete non-shadow body is balanced across the
canonical edge, and every phase consumes the same transform.

All 16 Thin-only masks and physically valid mixed None/Thin/Ordinary states share this topology path.
Each regular contact replaces the complete geometrically necessary aperture, including face regions
that would otherwise block top continuity. A contour-only edit is not sufficient. The rest of the
regular tile retains native geometry and sampling. A side-T coordinates both receiving tiles and one
external Thin owner. Different source materials split draw batches but never split geometry or leave
an exterior outline across the join. Removal restores the exact native regular-wall path.

The source-only design removes custom OSB/rivet/latch/phase-stroke/damage painters, raster atlas
composition, baked Stuff colors, and the five-texture native/gutter workaround. Native linked corner
art may supply actual internal perspective seams; square exterior width transitions remain mandatory.
Shadows use the final physical union with the native sun shader. Core damage materials are selected
deterministically and clipped with their UVs to owner-visible surfaces; they are not painted into a
diffuse atlas.

The inspected Naname reference demonstrates this material/UV/mesh separation through
`MaterialAtlasPool.SubMaterialFromAtlas` and `LayerSubMesh` geometry. Its diagonal meshes, coordinate
presets, code, textures, Def cloning, and optional integrations are not adopted. Thin Walls remains
independent, cardinal, and edge-based. Source provenance and exact implementation/acceptance slices are
recorded in the 2026-09-04 plan.

Alternative rejected: retain the existing runtime painter merely because it does not ship PNGs. It
still authors material details, bakes Stuff response, and reconstructs rather than reuses native
appearance. Alternative rejected: scale a whole Core tile. It changes face height and outline weight,
reintroducing the failed projection.

### 8. Put Thin Doors on the edge while reusing vanilla door behavior

`TW_ThinDoor` is a Stuff-made `Building_ThinDoor` derived from `Building_Door`, but remains standable and non-edifice so both adjacent cells stay ordinary map cells. It uses the same right-hand cardinal line contract as Thin Walls, costs ceiling-half of the Core simple door (`ceil(25 / 2) = 13` Stuff), and has half its base durability (`160 / 2 = 80`). A shared edge contains at most one Thin Wall or Thin Door constructible phase.

The pathfinder keeps Thin Wall edges closed. A Thin Door edge is available to a pawn that can open that concrete door or while the door has free passage, closed to a pawn that cannot open it, and available to an explicitly bash-capable request so the concrete door can be attacked. Request-specific door access is applied to an isolated connectivity snapshot whose lifetime extends through the scheduled Burst job; no array is mutated while a job can read it. A postfix on `Pawn_PathFollower.NextCellDoorToWaitForOrManuallyOpen` returns the concrete edge door only when the pawn's next cardinal step crosses it. Vanilla then performs its ordinary manual-open wait and stance. Movement entry still checks the exact edge, so walking within either adjacent tile is unaffected and a stale path cannot cross a closed inaccessible door.

Thin Doors retain cached fixed endpoint geometry and realtime moving leaves. The new leaves reuse the
measured Core simple-door mover material and UV regions, not wall pixels darkened by a panel-tone
recipe or decorated with painted latch marks. The frame is flush wall-derived geometry. Closed leaves
and frames partition the edge without overlap; opening moves/clips leaf geometry and UVs together
along the edge without changing the normal projection or doorway gameplay. Native damage textures are
attached to the same moving/clipped surface domain. The exact source door landmarks, animation
coverage, outline, and depth order must pass the new measured and live gates before any prior numeric
leaf travel/altitude prescription is reused. Fixed and moving shadows follow their actual physical
volumes and never double-cast at a contact.

Alternative considered: place an invisible vanilla door edifice in one adjacent cell. Rejected because it would make every approach to that cell pay door behavior, prevent compatible buildings there, and violate the free-cell requirement.

### 9. Split vanilla regions at completed edge boundaries

Completed Thin Walls and Thin Doors are room boundaries even though their Things are non-edifices. The product replaces only `RegionMaker.TryGenerateRegionFrom` when a map contains completed Thin edge structures, reproducing RimWorld 1.6's region construction while adding one rule: the cardinal flood and generated `RegionLink` spans may not cross an occupied completed edge. The implementation preserves the expected `RegionType`, one-cell portal handling for actual vanilla doors, extents, map-edge flags, link canonicalization, and region lister registration. Maps without completed Thin edge structures remain on the unmodified vanilla method.

Spawning, completing, despawning, destroying, deconstructing, and loading a Thin Wall or Thin Door dirties both incident vanilla regions before the next room rebuild. A closed or open Thin Door remains a room divider, matching vanilla door portals; its inherited temperature tick is redirected to exchange heat only between the two rooms immediately across its owned edge, at vanilla simple-door open/closed rates. Enclosed cells report an indoor room with scoped room roles/stats. The `correct-thin-wall-thermal-insulation` change supersedes the original perfect-insulation assumption: solid thin edges exchange finite heat at half a regular wall's thermal resistance. Thin edge structures still contribute zero roof support and do not seed automatic roofing.

Alternative considered: maintain a parallel mod-only room database and patch individual room consumers. Rejected because it would miss Core and modded systems that consume `Room`, `District`, room roles, or temperature trackers.

### 10. Keep release art non-generative, reproducible, and separate from acceptance evidence

The mod owns a 1280×720 title master, a deterministically derived 640×360 `About/Preview.png`, and wide 1164×655 Workshop cards below 1 MiB. Source manifests bind copy, alt text, dimensions, local font, exact accepted in-game capture hashes, and output hashes. Every pictured wall or door is an exact crop from the accepted minimized Gateway run. Typography, labels, arrows, comparison layout, and the no-roof symbol may be deterministic vector/raster overlays, but neither generated imagery nor a reconstructed wall/door diagram is permitted. The title master must also be composed from accepted in-game renders and deterministic typography rather than generated concept art. Cards show the actual edge geometry and benefits they claim; they do not use unresolved Steam URLs.

The complete in-game acceptance sessions remain under ignored evidence. Eight reviewed captures from the accepted minimized Gateway run are explicitly promoted into the tracked Workshop source tree with their original screenshot names, run identity, visibility/minimized state, and byte hashes. Promotion happens only after live and independent visual acceptance, so the cards consume evidence without becoming the evidence. The furnished-room captures follow the accepted realistic-base design record; the catalog/junction captures remain clearly labeled in-game technical comparisons.

## Risks / Trade-offs

- **[Risk] Another pathfinding mod replaces the same `ParameterizePathJob` or connectivity lifecycle.** → Use narrow postfixes, exact signature/startup ownership tests, no private copies of optional assemblies, one bounded compatibility warning, and fail closed at movement execution.
- **[Risk] Copying the full connectivity byte grid after a path-data change adds main-thread work.** → Rebuild only when `GatherData` reports a change, use one byte per cell plus an edge iteration, retain allocation-free buffers, and benchmark a large-map wall catalog in the live process.
- **[Risk] Edge room boundaries require a narrow replacement of vanilla region generation.** → Shape-test the exact installed method and fields, reproduce the current algorithm line-for-line with only the edge predicate added, bypass the patch on maps without completed Thin edge structures, and prove native room IDs, stats, temperatures, spawn/despawn rebuilds, vanilla doors, and ordinary no-Thin-Wall maps in isolated RimWorld processes.
- **[Risk] Thin Door access differs per pawn while the Burst pathfinder consumes immutable connectivity.** → Reuse the shared wall snapshot when possible and allocate a bounded request-specific snapshot only when concrete door access differs; retain it through job completion and dispose it in the exact pathfinder lifecycle.
- **[Risk] A segment constructed after path gathering leaves one tick of stale connectivity.** → Dirty path data immediately and enforce both setup and final-entry movement guards; the next gather publishes the new overlay safely.
- **[Risk] Multiple selectable Things in one cell can make a particular segment harder to click.** → Move each segment's draw/selection center to its edge, use narrow orientation-specific draw size, retain vanilla selection cycling, and verify deconstruction of each orientation through the native UI.
- **[Risk] A deterministic hybrid mesh can still reproduce the wrong projection or retain hidden atlas outlines.** → Pin the exact Core atlas bands and hashes, test all 81 typed-arm states and every aperture rotation, compare source UV regions and output silhouettes mechanically, then inspect the complete linked-topology catalog at close, ordinary, and far useful zoom in RimWorld.
- **[Risk] Room sealing could be mistaken for every regular-wall system.** → Publishing and inspect text name room/temperature integration and explicitly exclude roof support, cover, projectile, sight, pen, gas, wind, light, and vacuum behavior unless later specified.

## Migration Plan

1. Add the new package and test projects without changing Immersive Chefs or the Dev Gateway package boundary.
2. Build repository-local output, then deploy only `fumblesneeze.thinwalls` for intentional isolated runs.
3. Launch every verification process with a unique `-savedatafolder`, dedicated log, exact PID/start identity, and Harmony/Core/Thin-Walls order; append Gateway only for gateway-assisted evidence.
4. Hash the user's normal `ModsConfig.xml` before and after. No workflow writes it; if an existing repository launcher temporarily stages a package, its exact marker-owned stage is removed in guaranteed cleanup.
5. Rollback is removal/disablement of the new package. No vanilla Def, save, Workshop directory, or other mod file is edited. Saves without the mod lose Thin Walls Things through RimWorld's ordinary missing-Def behavior; uninstall recovery is not claimed.

## Open Questions

None. The user's roof-support, in-game graphics-verification, Thin Door, hybrid linked regular-wall merge, and room-system clarifications are incorporated into the normative specs and task gates.
