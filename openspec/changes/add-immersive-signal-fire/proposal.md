## Why

Tribal colonies need a world-grounded way to request allied military aid before radio technology. Immersive Signal Fire revisits the old Tribal Signal Fire concept as a short, uncertain group activity whose contacts, visuals, and consequences are observable in ordinary play.

## What Changes

- Add an unlit, custom-rendered signal-fire building unlocked by a Neolithic smoke-signalling research project.
- Add pawn and no-pawn map right-click contact menus that list only allied Neolithic or Medieval factions with a settlement at most 10 world tiles from the current colony.
- Reuse RimWorld's begin-ritual participant and quality dialog for one caller plus up to two helpers, then run a 600-tick native pawn work sequence.
- Score the selected group's Melee and Social skills into immediate, delayed, misunderstood, or unnoticed outcomes and reuse RimWorld's native allied military-aid action for eligible responses.
- Use required Dynamic Effects Forge effects for the ritual flame and a dark, wind-drifting SOS smoke cadence; optionally render a synchronized fire blanket when either supported Show Me Your Tools variant is active.
- Treat the prepared hearth as single-use: every terminal active performance consumes the structure and scatters substantial cleanable soot.
- Add focused host, finalized-Def, optional-integration, and player-workflow verification. Gameplay behavior is implemented by this change, not deferred.

## Capabilities

### New Capabilities

- `immersive-signal-fire`: Construction, contact eligibility, participant selection, smoke signalling, military-aid outcomes, optional tool visuals, cleanup, persistence, and player-facing presentation.

### Modified Capabilities

None.

## Impact

- **Owner:** **Immersive Signal Fire**, package ID `fumblesneeze.immersivesignalfire`, repository path `mods/ImmersiveSignalFire`.
- Adds the product mod, its tests, localization, release metadata, and original raster assets.
- **Required runtime dependencies:** Harmony (`brrainz.harmony`) for the no-selected-pawn native map-menu seam and Dynamic Effects Forge (`blues.forge`) for ritual flame and smoke effects. Neither assembly is copied into the product package.
- **Optional runtime dependency:** Show Me Your Tools, including the installed fork using package ID `meathax.showmeyourtools`; the narrow adapter is absent-safe and shape-guarded.
- The product does not reference or depend on `fumblesneeze.rimworlddevgateway`. Two reusable E2E player-action additions needed for acceptance are specified and implemented under the separately owned `add-rimworld-dev-gateway` change.
