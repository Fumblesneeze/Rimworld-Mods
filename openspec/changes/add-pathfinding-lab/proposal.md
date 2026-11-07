## Why

Large maps amplify repeated cell searches. This experiment tests whether a cached portal abstraction and rolling local refinement can reduce that work without changing a pawn's final order. It is not a claim of optimal paths or production readiness.

## What Changes

- Add Pathfinding Lab, an opt-in RimWorld 1.7 experimental product mod, with vanilla/experimental switching.
- Cache a connected-chunk portal graph, refine only a short prefix, and retain per-pawn coarse plans and bounded local path caches. Invalidate affected cached geometry when map costs change.
- Replace the common 1.7 search worker for every queued and synchronous request in experimental mode. Preserve native request preparation and traversal rules; use request-aware repair search when cached portal guidance is invalid. Never return partial paths to synchronous full-path callers.
- Add explicit experimental settlement-map sizes up to 1000 x 1000, leaving ordinary sizes and quest maps unchanged by default.
- Compare native pawn movement with vanilla and the experiment on isolated, minimized, muted runs; distinguish cold construction, warm reuse, path quality, and map viability.

## Capabilities

### New Capabilities
- `pathfinding-lab-routing`: Opt-in hierarchical partial refinement, conservative fallback, runtime switching, cache invalidation, and experimental map sizing.

### Modified Capabilities
- None.

## Impact

Single owner: Pathfinding Lab (`fumblesneeze.pathfindinglab`, `mods/PathfindingLab`). Requires Harmony and exact supported RimWorld 1.7 API shapes. No runtime dependency on other repository mods or the development Gateway. No XML schema extensions. Tests and evidence belong to this product; shared automation changes, if needed, require their own tooling-owned contract. Default behavior remains vanilla. Benchmark claims require the repository's real native-workload protocol; unavailable beta profiling support will be reported rather than replaced with misleading microbenchmarks.
