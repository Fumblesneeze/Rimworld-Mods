## Why

Players want an additional slag-smelting batch at the existing electric smelter. Replacing Bulk
Smelting requires specifying its actual ingredient, work and output behavior, while intentionally removing the source's yield bonus. The user selected efficiency-only batching.

## What Changes

- Specify one ordinary three-chunk recipe alongside existing smelting bills: three steel-slag chunks
  or three mechanoid-slag chunks produce45 steel at the inspected baseline for1100 base work at the ordinary smelting speed.
- Preserve the source's same-kind batch restriction, native physical ingredient/work rules, ordinary
  bill controls and stable recipe identity. One execution is one bill repetition, not one repetition per output item.
- Keep the existing single-chunk recipe, other station recipes and unrelated filters unchanged.
- Guard the declarative addition, report skipped support at startup and yield to an active original
  provider or an existing recipe identity without overwriting it.
- Integrate with the contextual Add bill browser and shared workbench plans as an ordinary recipe;
  introduce no new menu, settings system, cache or special production lifecycle.

This change **specifies later work only**. Origin is **replacement**, S015 / P1-12. The user chose
piecewise-equivalent output: the inspected baseline is45 steel /1100 work versus45 /1200 for singles.
The source's60-steel batch is research provenance, not our selected yield. Keep homogeneous batches.
Follow admitted current single-operation outputs and withhold the recipe when that parity cannot
be established. Initial support is a new colony, not original-provider save import.

## Capabilities

### New Capabilities

- `megamod-bulk-slag-smelting`: Guarded additive three-chunk slag production using ordinary bills,
  physical resources and work, with explicit output/counting and compatibility boundaries.

### Modified Capabilities

None. Existing draft browser and whole-list workbench contracts already accept ordinary recipes.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
See [research](../../../mods/megamod/docs/refinement/2026-11-02-bulk-smelting-research.md) and
[replacement coverage](../../../mods/megamod/docs/replacements/bulk-smelting.md).

Future implementation adds guarded XML recipe content and baseline-language text. **XML Extensions**
(`imranfish.xmlextensions`, Workshop `2574315206`) is a **required** runtime dependency for its
conditional/package/log operations; declare it and its load order in the implementing slice.
Its Harmony dependency must be available through the normal external dependency graph, never bundled.
The source mod is **not required**; its presence selects the external provider instead of our addition.
No other mod, DLC, runtime library mod or Dev Gateway dependency is introduced by this feature.

The proposed stable recipe name supports ordinary saved-bill references. This is not a promise to
impersonate the source package, migrate foreign custom records or repair arbitrary existing loadouts.
No runtime file, dependency metadata, test, package or active mod list changes in this documentation slice.
