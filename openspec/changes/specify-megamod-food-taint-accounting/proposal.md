## Why

Moving food between stacks should move the food's existing taint, not create more tainted meals or
change which portion was eaten. Preserve Food Poisoning Stack Fix's finite-taint intent while
correcting its transfer arithmetic and preserving cooking contamination decisions, native illness
handling and external probability consumers.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Source S041/P1-32, **Food Poisoning Stack Fix (Continued)**,
`Mlie.FoodPoisoningStackFix`, Workshop `2843483188`, installed version **1.7.0**.

## What Changes

- Specify finite tainted quantities that remain bounded by actual food quantities through full and
  partial merges, splits, whole-stack moves, no-ops, consumption and destruction.
- Preserve native cooking/room risk, batch creation, illness modifiers and independent dangerous-food
  risks. Select moved/eaten portions fairly without replacement; use the actual consumed portion.
- Preserve attributable causes and ordinary hidden-state privacy. Move the source's developer
  inspection into a read-only top-debug-toolbar action; add no ordinary menu, toggle or preference.
- Specify owned persistence and one-time assignment of known native probability-only food. Ambiguous
  original-provider formats are not guessed or silently imported; a separate converter remains open.
- Specify mixed tracked/unsupported transfers: preserve ordinary stacking, retire the participating
  owned records only when food actually moves, and retain explicit unsupported provenance afterward.
- Preserve native probability interfaces rather than placing counts in them. Define original-provider
  suppression, optional meal-provider ownership, unavailable-path fallback and direct hauling/delivery
  composition, including the concrete Immersive Chefs interaction.

This delivers **specification only**, not runtime behavior or a verified fix. Full native/owned food
handling is intended; optional providers require demonstrated supported paths. **BREAKING relative
to source:** no integer reinterpretation of the native probability field/save key, no source debug
inspect text, and no safe add/remove or automatic source-save migration claim.
Finite consumed exposure deliberately changes illness distributions for mixed/multi-unit food and
some modifiers; preservation of the native calculation boundary is not a promise of identical odds.

## Capabilities

### New Capabilities

- `megamod-food-taint-accounting`: finite cooking-taint quantities, physical transfers and ingestion,
  cause/illness behavior, persistence/format boundaries, privacy and compatible fallback.

### Modified Capabilities

None. Hauling, visitor delivery, native illness and the foundation's admission/UI contracts retain
their ownership; this feature composes with their physical item actions directly.

## Impact

- Core required. Harmony becomes required when actual Harmony integration is implemented; no upstream
  mod or DLC is required. Original source and optional meal providers remain optional.
- Future guarded C# stack/ingestion/lifecycle integration and owned saved taint data. No static Def
  change is currently proposed. Any later XML adaptation requires XML Extensions guards, dependency
  metadata and merge preservation under its own target document.
- Existing modded food using admitted ordinary poisoning/stack paths participates without a title
  allowlist. Unsupported custom paths retain their provider rather than receiving a partial takeover.
- No new food, recipe, ingredient, stack limit, nutrition, carrying allowance or duplicate Thing cache.
  No reference/dependency on another repository mod or the Dev Gateway; optional adapters stay narrow.
- [Research](../../../mods/megamod/docs/refinement/2026-11-03-food-poisoning-stack-fix-research.md)
  and [replacement coverage](../../../mods/megamod/docs/replacements/food-poisoning-stack-fix.md)
  distinguish source behavior, deliberate corrections, actual consumers and remaining native evidence.
