## Why

Items with improvised melee attacks can clutter weapon choices even when the player only wants to
use them as resources or consumables. S090 lets the player exclude selected item types from weapon
classification and new equip actions without removing the items or their ordinary uses.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S090/P2-19, Is This A Weapon? Specification only.

## What Changes

- Add a searchable weapon-classification section to mod settings, following the user's explicit
  Mod Config note. Each type has a global default and inherited or overridden save value.
- Preserve the shipped source's melee-tool candidate gate, ranged filter and listed-row bulk actions;
  exclude worn apparel, and make reset scope and affected rows explicit.
- Disabled types stop counting as weapons and cannot be newly equipped through admitted paths.
  Re-enabling restores ordinary classification and restrictions rather than creating combat ability.
- Correct queued-action, cached-list and save-switch gaps; retain existing physical items/equipment.
- Specify Simple Sidearms' actual menu and direct-switch boundaries instead of treating its public
  compatibility claim as proof that every path respects the setting.

## Capabilities

### New Capabilities

- `megamod-weapon-classification`: candidate catalogue, inherited classification settings, scoped
  search/bulk/reset controls, prospective equipment policy, current data and provider admission.

### Modified Capabilities

None. Shared search/hauling indexes consume the same current classification. Owned equipment
policies compose directly; this change does not replace Simple Sidearms itself.

## Impact

Future narrow Harmony adapters on native classification and equip paths, plus an optional verified
Simple Sidearms adapter. Core and external Harmony are required when implemented. No DLC required;
source mod, Simple Sidearms and other equipment providers optional. No XML is currently proposed;
if needed, guarded XML Extensions and its required dependency must be added at first use.

Use the common settings and map facts, no independent inventory scanner or source-package façade.
No source settings import, forced disarm, item deletion, attack-stat rebalance, category-list rewrite,
new tab or debug action. [Research](../../../mods/megamod/docs/refinement/2026-11-05-weapon-classification-research.md)
records source/code differences, consumer evidence and WCL-Q1–5. Nothing implemented or accepted.
