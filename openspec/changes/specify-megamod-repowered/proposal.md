## Why

S053 selects Turn It On and Off - RePowered to make equipment power costs depend on useful work
and to expose predictable idle/active budgets. Combining it with S052 LightsOut must produce one
consistent activity and consumption policy, including machinery that works without an attending pawn.

## What Changes

- Specify work, door, turret, hydroponics, linked-facility and admitted external activity with safe
  readiness, multi-map operation and truthful consumption displays.
- Retain RePowered's configurable 10 W idle, 2.5× active and 10× door defaults and its four vanilla
  rate presets for its admitted equipment. Give per-building-type overrides highest precedence.
- Use S052 activity for overlapping devices; RePowered rates replace, rather than multiply, their
  LightsOut rates. Lighting and non-overlapping equipment retain S052 behavior.
- Correct source gaps in reservation/schedule definitions, world-view updates, closing-door activity,
  native power adjustments, missing-content settings retention and insufficient-power forecasting.
- Put configuration in common global/per-save settings with searchable building-type rows; retain
  native entity controls and add no bottom tab. Preserve external contracts only where proved.

## Capabilities

### New Capabilities

- `megamod-repowered`: Activity-based equipment budgets, rate configuration, work admission,
  information and supported provider composition.

### Modified Capabilities

None in the canonical capability tree. The pending S052 LightsOut draft is updated alongside this
change to state the shared boundary explicitly; both remain owned by the same product.

## Impact

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. This change specifies future behavior only; it implements nothing.

Future runtime requires Core and Harmony (`brrainz.harmony`) for admitted patches. XML Extensions
(`imranfish.xmlextensions`, Workshop2574315206) becomes required when guarded external definition
adaptation is implemented. No upstream power controller is required. DLC, content providers and
Relevant Stats In Description are optional, independently admitted branches. RePower Patch Pack's
hard dependency on the original package is an explicitly unsatisfied contract, not a new dependency
or a reason to spoof that package. No foreign assemblies, upstream settings importer, artwork or
Dev Gateway dependency are shipped. See the [research](../../../mods/megamod/docs/refinement/2026-11-03-repowered-research.md).
