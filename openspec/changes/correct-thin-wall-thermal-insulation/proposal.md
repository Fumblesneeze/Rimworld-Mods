## Why

Completed thin edges already split Core rooms, but Core's wall conduction samples across an
occupied whole cell, skipping thin boundaries between adjacent air cells. Thin Walls therefore
needs explicit finite heat transfer rather than accidental perfect insulation.

## What Changes

- Deliver thermal conduction through completed solid thin walls at twice the Core regular-wall
  conductance per segment (half thermal resistance), regardless of Stuff, matching Core material policy.
- Preserve native room identities, volumes, roof/weather/heater calculations, door rates, and no roof support.
- Iterate only owned edges at Core's thermal cadence; retain no full-map or room-temperature cache.
- Verify native construction, room division, heating, breach/remerge and save/load in an isolated game.

## Capabilities

### New Capabilities

- `thin-wall-thermal-insulation`: conservative owned-edge heat exchange and native room integration.

### Modified Capabilities

None of the archived capabilities. This supersedes the perfect-insulation wording in the active
`add-thin-walls` room scenarios without reopening its unrelated rendering work.

## Impact

Owner: **Thin Walls**, `fumblesneeze.thinwalls`, `mods/ThinWalls`. This change delivers behavior now.
Affected components: its map component, room heat calculation, focused host and E2E tests.
Core and Harmony remain required; no new required or optional dependency, no Gateway product dependency.
