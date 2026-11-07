## Why

Choosing a recipe should let the player compare the things it can actually produce, with a clear
connection between the displayed material, the ingredients available and the bill that is added.
Quartermaster supplies useful comparison ideas, but the requested destination is the workbench's
Add bill view, not its separate equipment optimizer.

## What Changes

- Specify product-stat comparison at Normal quality, with material choices ranked by the selected
  stat instead of Quartermaster's combined armor/weapon score or precious-material preference.
- Specify **Use all materials**, initially unchecked, and distinguish ingredient/skill availability
  from whether a recipe is unlocked and whether a worker can start the job immediately.
- Add a bill to the opened workbench with the compared material, preserving other required
  ingredients and ordinary planning when supplies or skills are missing.
- Keep unavailable or unsupported comparison results explicit instead of displaying invented zeroes
  or silently removing otherwise valid recipes.
- Record the source's remaining loadout, owned-gear, exclusion, preset and policy features as outside
  the confirmed scope. No new bottom tab, automatic equipment assignment or stat rebalance is proposed.

This change **specifies later work only**. No comparison UI, patch, cache, recipe or gameplay behavior
is implemented. S010 Nice Bill Tab, S011 its Expansion and S012 the original integrated browser each
retain their own sequential change; S012 will compose their navigation with this comparison contract.

## Capabilities

### New Capabilities

- `megamod-material-aware-recipe-comparison`: Compare recipe outputs and material variants, explain
  supplies and skill availability, and preserve the chosen variant when adding a native bill.

### Modified Capabilities

None.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Source reference: Quartermaster (`slim.quartermaster`, Workshop `3737474260`), S009 / P1-09.
See the [replacement report](../../../mods/megamod/docs/replacements/quartermaster.md) and
[research journal](../../../mods/megamod/docs/refinement/2026-11-02-quartermaster-research.md).

Future code affects Add bill comparison, native bill creation and the shared map Thing index used
by search/hauling. Recipe costs, research, work, quality rolls and ordinary production remain native.
Quartermaster, Nice Bill Tab and its Expansion are **reference sources, not required dependencies**.
Existing third-party providers are optional and require individual integration evidence; no blanket
Combat Extended or alien-race compatibility is claimed. No external runtime dependency is added by
these documents. Harmony becomes required if the implementation uses its types; XML Extensions
becomes required with any `XmlExtensions.*` use. The Dev Gateway remains development-only.
