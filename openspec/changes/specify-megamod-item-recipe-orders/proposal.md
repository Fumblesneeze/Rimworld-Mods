## Why

Processing a particular corpse, chunk or piece of equipment should not require configuring a general
production bill that might consume other items. S096 brings applicable recipe actions to the selected
item, composing them with the already specified recycling and melee-destruction choices.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S096/P2-25, Micro Designations. **Specification only.**

## What Changes

- Offer contextual, one-time orders for supported recipes that process one physical item unit,
  including ordinary butchery, stone cutting, smelting, burning and admitted custom operations.
- Discover actions from actual recipe capabilities. Distinguish unavailable research, facilities,
  workers and unsupported processing instead of allowing a designation that can never work.
- Perform real hauling and recipe work with normal eligibility, costs, results and consequences.
  Keep ordinary bills unchanged and give their automatic work precedence at equal work priority.
- Track only the authorized item quantities through movement, stacks, interruptions and saving.
  Choosing another owned processing action replaces that choice while retaining unrelated orders.
- Retain understandable completion and recovery state; avoid duplicate products, repeated repair,
  silent substitution and unchanged failed jobs endlessly picking up and dropping an item.
- Retain the three source visibility preferences through common global/save settings, correcting
  inconsistent saved defaults and clarifying colony requirements. Retain planning before a bench
  exists, with reliable refresh and explicit waiting reasons.
- Integrate directly with owned recycling, destruction, repair knowledge, hauling and workbench
  behavior. Preserve proven external contracts where feasible and document unsupported bindings.

## Capabilities

### New Capabilities

- `megamod-item-recipe-orders`: contextual recipe selection, exact item quantities, work and result
  preservation, interruption/recovery, configuration and compatibility fallback.

### Modified Capabilities

None. S022/S023 already require exclusive composed processing choices and quantity conservation;
this change supplies the recipe-processing participant without changing those contracts.

## Impact

Future selected-item UI, item-processing state and native recipe/job integration. Core and external
Harmony are required when implemented; Micro Designations is a coexistence input, not a dependency.
Work Tab, custom recipe providers and their worker integrations are optional and separately admitted.
No DLC or foreign assembly is required. Owned declarations remain additive. Any actual XML patch
must use guarded XML Extensions and add `imranfish.xmlextensions` (Workshop2574315206) as required in
that implementation slice. No source package impersonation, foreign save/settings conversion,
runtime implementation, game launch or gameplay acceptance is delivered here.
