## Why

One completed Thin Wall activates a duplicate whole-map reachability graph which is repeatedly discarded during ordinary colony activity. It also drops nonlocal native RegionLinks used by As Above So Below II stairs. The user approved replacing this with state scoped to owned edges and native connectivity.

## What Changes

- Remove the full-map Thin reachability graph and its broad invalidation subscriptions.
- Reuse the already edge-separated native regions, native reachability caches and native/mod RegionLinks.
- Index completed owned edges and apply their sparse masks to the engine-owned connectivity at its safe gather boundary; do not own a duplicate persistent map grid.
- Compose native reachability through permitted Thin Door endpoints when native room separation needs a bridge. Retain Touch, special traversal, bashing and stale-movement safety.
- Deliver this behavior, focused regressions, independent review and isolated live verification in this change. Do not alter the user's current save or configuration.

## Capabilities

### New Capabilities
- `thin-wall-local-pathing`: Sparse edge ownership, native region connectivity, door bridging and bounded local invalidation.

### Modified Capabilities
None archived. This change supersedes the whole-map graph/snapshot implementation prescriptions in the active `add-thin-walls` collision/design sections; their player-facing wall/door/collision/room requirements remain binding.

## Impact

Owner: **Thin Walls** (`fumblesneeze.thinwalls`), `mods/ThinWalls`. Changes its pathing/room integration and owning tests only. Harmony remains required. As Above So Below II (`astryl.asabovesobelow2`) remains optional; compatible native links are consumed without a product reference or new dependency. Gateway and Circinus remain dev-only. No balance, rendering, assets, progression, placement or save-format changes.
