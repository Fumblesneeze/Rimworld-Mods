## Why

Translation cannot always fit an ordinary building between opposing Thin Walls. Players need explicit,
reversible cosmetic control rather than an automatic scale change or a rejected legal footprint.

## What Changes

- Add separate optional Shrink and Offset gizmos to eligible player buildings in Thin Walls.
- Shrink cycles 100, 90, 80, 70, 60, 50, then 100 percent. Offset cycles the center and eight 0.2-cell nudges.
- Save each building's choices and preserve native gameplay geometry. Nearby Thin Walls choose an
  automatic preset through this same offset system; remove the older sprite-clearance renderer.
- Reuse native textures and materials; add no textures, external dependency, or copied upstream code.

## Capabilities

### New Capabilities
- `thin-wall-building-adjustments`: Persistent player-controlled cosmetic scale and offset.

### Modified Capabilities
`thin-wall-rendering`: replace measured automatic sprite clearance with the same 0.2-cell offset presets.

## Impact

Owner: **mods/ThinWalls** (`fumblesneeze.thinwalls`). Production, localization and its focused tests only.
Perspective: Buildings (Continued) is a read-only design reference, not a dependency.
