## Why

Grazing Lands adds persistent pasture, configurable pasture loss and crop damage tied to food eaten.
It overlaps Graze up's plant-consumption rules, so importing both calculations would give plants
contradictory nutrition and survival outcomes. This draft defines their combined behavior once.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**, S087/P2-16, Grazing Lands. Specification only; nothing implemented.

## What Changes

- Retain S043's common grazing eligibility, rooted eating, settings and finite-growth pasture.
- Use edible harvest nutrition for yielding plants; retain plant nutrition for other supported
  grazing plants. A cultivated food crop normally destroyed by harvest loses health when eaten;
  other plants lose growth. Neither route produces harvested items.
- Add a 5% default destruction chance when a non-yielding pasture plant reaches its retained-growth
  limit. Keep the selected-species destruction rule distinct, with no second roll or extra food.
- Consolidate the nutrition control and add crop-consumption cost to the existing grazing settings.
- Bound every bite by hunger and actual remaining growth/health; correct source rounding, damaged
  crop discontinuity, zero-progress and first-run-default defects.
- Explicitly revise the still-unimplemented S043 draft's nutrition/survival examples. The combined
  arithmetic and balance are proposed choices, not user-confirmed numbers or a claim of source parity.

## Capabilities

### New Capabilities

- `megamod-grazing-plant-damage`: plant classification, yield-based nutrition, finite crop health
  consumption, pasture loss and their common configuration/fallback.

### Modified Capabilities

No published capability is modified. The unarchived `megamod-grazing` draft in
[S043](../specify-megamod-grazing/specs/megamod-grazing/spec.md) is revised in place to reference this
single combined amounts contract. Its eligibility and separate plant-lifespan capability remain.

## Impact

Future guarded Harmony integration with the existing grazing group; external Harmony is required
when implemented. Core is required; original Grazing Lands/Graze up, DLC and other plant providers
are optional. No XML operation is needed for eater-dependent calculations; any later XML addition
must use guarded XML Extensions and declare its dependency at first use.

No new content, bottom tab, independent cache, upstream DLL/API impersonation or migration.
S077 tree longevity retains its ordinary lifecycle behavior; S088 concerns child growth, not plants.
S085 partial items excludes rooted plants. [Research](../../../mods/megamod/docs/refinement/2026-11-05-grazing-lands-research.md)
records exact sources, deviations, provisional balance and GL-Q1–4. All implementation and native
acceptance tasks remain open.
