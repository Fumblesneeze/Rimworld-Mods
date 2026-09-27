## Why

Thin Walls already draws edge-centered selection brackets, but pointer selection still admits
the whole owner cell and misses the neighboring half of the displayed rectangle.

## What Changes

- Make pointer selection of thin walls and doors, including blueprints and frames, use the
  existing bracket envelope without changing bracket appearance or animation.
- Discover edge targets from both adjacent cells; keep distinct orientations selectable and
  preserve native targeting eligibility, unrelated candidates and selection cycling.
- Implement and host-test now. Defer deployment and in-game acceptance until the user closes
  their running game and explicitly requests continuation.

## Capabilities

### New Capabilities

- `thin-wall-pointer-selection`: pointer hit area agrees with the existing edge selection box.

### Modified Capabilities

None of the archived capabilities; complements the active Thin Walls rendering contract.

## Impact

Owner: Thin Walls (`fumblesneeze.thinwalls`, `mods/ThinWalls`). Gameplay fix delivered by this
change, pending later live verification. Affects selection geometry and the native pointer
candidate seam. Core and Harmony remain required. No new dependency, assets, settings, map cache,
save format, placement, pathing or thermal changes; Gateway remains development-only.
