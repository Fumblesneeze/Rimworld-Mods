## Why

The player should be able to mark unwanted weapons and apparel for real material recovery without
building a separate disposal bill for each item. Recycling should use reachable, working facilities,
preserve cancellation and physical items on failure, and cooperate with the other owned item orders.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: **original**, S022 / P1-17. Recycle This (Continued) is a research comparator, not a selected
full replacement. **Specification only:** no implementation or native acceptance is delivered.

## What Changes

- Specify per-item recycling orders for weapons and apparel, with contextual controls and an Orders
  tool; use ordinary physical carrying and work, without creating or modifying the player's bills.
- Propose smelting at an appropriate smelter and non-smeltable equipment scrapping at a suitable
  tailoring bench or crafting spot, respecting current access, power, fuel and reservations.
- Make returned materials, fractional quantities, work, cancellation and save/reload outcomes
  explicit. Use the accepted25% basic construction-material return, exclude complex ingredients,
  and scale construction plus fixed salvage by remaining HP. Expose common scoped controls for
  construction return and contextual-command visibility.
- Preserve unrelated orders, protect equipment in use, and compose mutually exclusive processing
  choices directly with future S023 destruction and S096 Micro Designations.
- Use shared current item facts and native designation/bench queries, not another mod-specific cache.

## Capabilities

### New Capabilities

- `megamod-equipment-recycling`: designation, eligibility, facilities/work, material recovery,
  cancellation, lifecycle, settings and compatibility of weapons/apparel recycling.

### Modified Capabilities

None in archived capabilities. S021 supplies coherent carrying/cargo accounting; S023 and S096
remain separate features and will compose their processing choices with this order directly.

## Impact

Future work touches item orders, Crafting jobs, bench use, saved processing state and contextual UI.
It adds no bottom tab or independent work category and does not move native smelting out of Crafting.
No original Recycle This assembly, package identity, save schema or optional mech-work contract is
impersonated. Installing that reference provider withholds overlapping owned new recycling and
suspends unfinished owned consumption with an explanation; it does not authorize a second processor.

Required future dependencies: **Harmony** for selected native UI/lifecycle seams and **XML Extensions**
(`imranfish.xmlextensions`, Workshop `2574315206`) for guarded additive registration in the native
Orders category. Original Recycle This, external content/providers and Biotech are optional; the
base humanlike workflow requires none of them. No other repository product or Dev Gateway dependency.
Keep provider-specific recovery/workbench rules behind exact, absent-safe capability adapters.

[Research and open questions](../../../mods/megamod/docs/refinement/2026-11-02-equipment-recycling-research.md)
retain confirmed scope, native/reference evidence and proposed defaults. Refund balance and exact
beta/provider lifecycle remain explicit research/verification work, not reasons to interrupt drafting.
