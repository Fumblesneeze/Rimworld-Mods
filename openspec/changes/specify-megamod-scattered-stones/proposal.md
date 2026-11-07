## Why

Natural chunks and cliff edges look overly regular. Scattered Stones adds varied chunk placement
and colored rocky debris; its replacement must also specify the debris's real cleaning and room
effects rather than describing the entire feature as cosmetic.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Queue **S112**, origin **replacement**. Specification only; no runtime
implementation, asset selection or gameplay/visual acceptance is delivered here.

## What Changes

- Give supported ground stone chunks stable variation in scale, rotation and two-dimensional
  position, while preserving their physical cells, quantities, properties and current textures.
- Preserve storage-owned placement and restore ground scattering after hauling out of storage.
  Avoid the source's correlated offsets, persistent centering and reload-dependent appearance.
- Add one colored debris patch per eligible cell during initial map setup, and at actual stone-chunk
  yield locations when Dirty mining is enabled. Keep initial storage, cliff exposure and water rules explicit.
- Retain source-derived debris effects: 240 base cleaning work, beauty -10, outdoor beauty 0,
  cleanliness -2, one thickness, no tracking on pawns and a 60–90 day base lifetime.
- Keep optional persistence while covered by rock separate from cleaning or other removal. Use
  common settings, truthful help and direct composition with terrain-sensitive filth lifetimes.
- Guard declarative graphic changes and runtime behavior, preserve foreign graphic ownership, and
  document original-provider overlap and legacy saved-data boundaries without a universal compatibility claim.

## Capabilities

### New Capabilities

- `megamod-scattered-stones`: stable ground-chunk scattering, colored cleanable rocky debris,
  coherent lifecycle/settings and scoped rendering/content compatibility.

### Modified Capabilities

None. S105 remains the owner of creation-time lifetime scaling; this feature supplies ordinary
non-liquid debris and its separate optional protection from natural aging.

## Impact

- This change, [research](../../../mods/megamod/docs/refinement/2026-11-05-scattered-stones-research.md),
  [coverage](../../../mods/megamod/docs/replacements/scattered-stones.md) and the refinement ledger.
- Required game1.7 beta. Future implementation requires Harmony and **XML Extensions**
  (`imranfish.xmlextensions`, Workshop2574315206) for guarded declarative graphic changes; metadata
  must change with first use. Never bundle their assemblies or the source mod. No hard DLC,
  texture/storage-provider or Gateway dependency.
- Source **Owlchemist.ScatteredStones**, installed Workshop3528503211 by kkirspel, references
  Owlchemist's original and the1.5 continuation. All share the package ID; if active, the original
  provider owns the overlap and the owned feature is inactive with a warning.
- No new bottom tab, operational toggle, private Thing index or source settings import. Debris is
  saved gameplay state, not a cosmetic cache. Legacy source Def/type migration remains a separately
  gated contract question; no remove-anytime or automatic legacy conversion claim.
- Native/source visuals are research references only. Owned debris art and any renderer presentation
  must pass the asset skill's measured reference-first process and independent live visual review.
  No raster asset is created, selected or copied in this draft. Seven baseline languages are required.
