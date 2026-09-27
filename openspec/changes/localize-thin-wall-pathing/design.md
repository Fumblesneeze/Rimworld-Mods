## Context

Owner: Thin Walls (`fumblesneeze.thinwalls`), `mods/ThinWalls`. The 2026-09-16 user-session diagnostic found a 126x384 map, nonlocal stair RegionLinks and broad graph invalidation. Installed ASAB II assembly SHA256 was `8E62D57BC3698F334A80AEEFA5B2661136AEDA3FC2473F2924D7C3997BB0464F`. That inspection is diagnosis, not acceptance of this change.

The existing ThinEdgeRegionMakerPatch already divides native regions/rooms. Core PathFinderTick and FindPathNow complete scheduled jobs before GatherData. ConnectivitySource owns the native connectivity array; Burst PathFinderJob accepts only a complete immutable array view.

## Goals / Non-Goals

Goals: state proportional to owned edges, native cached reachability and mod links, safe edge blocking and door access, no whole-map graph or persistent duplicate connectivity grids.

Non-goals: rewriting native pathfinding, changing saved Things/room identity/rendering/balance, live-patching the user's process, or blanket compatibility claims.

## Decisions

1. Keep the existing native room/region split, indexing only completed shared edges. Refresh the changed edge from its two owner cells on lifecycle events. No subscription to ordinary path-cost/door/reservation activity invalidates a Thin graph because no such graph remains.
2. At the safe native GatherData boundary restore only the previously cleared bits, run native data gathering, then capture/apply current sparse wall masks. Apply even on native same-tick early return; restore even after final-edge removal. Do not mutate the borrowed native buffer from spawn/despawn callbacks. Preserve bits originally absent; union overlapping edge masks. This replaces the old double-buffer full-grid copy. Alternative: patch Burst execution, rejected as not a managed Harmony seam.
3. Ordinary jobs borrow the native masked buffer. Only differing request access (inaccessible Thin Doors or explicit wall bashing) needs a temporary full-array transport snapshot, unavoidable with the inspected Burst signature. These snapshots are not persistent map caches, are created lazily, and are disposed only after native scheduled-reader completion or PathFinder disposal. Bashing restores only bits actually removed by this mod.
4. Native positive reachability remains native. For a native negative, explore only accessible owned door endpoints (and bashable wall endpoints for an explicit bash request), asking native CanReach for connecting legs under unchanged TraverseParms. A scoped recursion guard prevents our own bridge recovery inside those native legs. Do not globally confirm with FindPathNow: ASAB rejects cross-band synchronous paths and segments actual movement itself. No external API or dependency is needed to retain native RegionLinks.
5. Preserve the native semantics hidden by the old graph: filter Touch destination regions to legal target-adjacent cells, retain Thin-edge target repair/bash exemption, and wrap the exact native cell-flood call's predicates so special destroyable/water traversal respects owned edges without copying the native flood/queue/grid. Preserve every native pass check. Door endpoints must be walkable and permitted, and native job/path movement remains authoritative.
6. Local changes need no new serialized data. Save/load reconstructs the edge index from spawned Things. Keep existing stale movement, diagonal endpoint blocking, room/temperature, and first/final-edge notifications.
7. Native region chunks containing owned edges, plus their cardinal receiver neighbors, use the edge-aware maker. Links incident to a directly affected chunk use matching one-cell spans on both sides; outer receiver boundaries retain native merged spans. This prevents asymmetric span lengths when a thin divider splits one side of a chunk boundary. Retained chunk keys still cover owned edges only, not the whole map.
8. First/final ownership of an incident native chunk invalidates its cells and one-cell receiver ring through Core's temperature-preserving region-dirty notification. This is at most two 14x14 rectangles (392 probes total), never a map sweep; additional edges inside the same owned chunks use only their ordinary two-cell notification. Interior Fence regions are included because their link format changes too. Permission bridges reject water endpoints in no-water modes and collect native allowed final destinations before searching, preventing immediate/same-district shortcuts from bypassing destination danger. Both native no-closed-door modes require FreePassage.
9. Native connecting legs treat only their synthetic endpoint region as transit through a scoped Region.Allows argument adjustment. Do not raise maxDanger or change TraverseParms: moderately dangerous rooms can remain transit toward a safe final destination, but forbidden dangerous transit and final destinations remain forbidden. Restore the scoped endpoint and recursion guard in finally.
10. The ASAB verification fixture must make the destination band visible before querying remote Go here: installed ASAB intentionally translates clicks on a non-visible band back to the pawn's band. Use native CameraJumper for view setup, retain a no-wall ladder round trip, assert native cross-band reachability before/after construction, then perform the actual remote Go here. Enclosure must leave the existing return ladder command present but disabled with No path. Earlier invisible-band arrival timeouts are fixture failures, not valid pre-fix compatibility evidence.

## Risks / Trade-offs

- Borrowed native memory / parallel readers → exact gather/completion lifecycle shape tests and live first/final-edge plus ordinary asynchronous movement. Never free engine memory.
- Unusual traversal modes / Touch shortcuts → focused regression per native seam before removing the graph.
- Thin-door owner Portal restrictions → retain owner-side movement and forbid/allow tests; do not treat the whole owner tile as a door crossing.
- Other mods changing native links → delegate native legs and do not cache them independently; exercise installed ASAB stairs and a blocked stair mouth.
- Performance claims → deterministic owned-state/work counts plus native active-colony evidence; report measured scope, never extrapolate a universal FPS gain.

## Migration Plan

No save migration. Build/install only the canonical local product package, run fresh isolated absent/present workflows, retain hashes and screenshots. Never edit Workshop content or normal saves/configuration. Rollback by rebuilding the previous source, not by retaining a duplicate installed package.

## Open Questions

None blocking implementation. Final acceptance remains gated by native movement/door/ASAB workflows and independent review.
