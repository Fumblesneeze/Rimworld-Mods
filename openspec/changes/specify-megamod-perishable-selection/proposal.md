## Why

Setting up storage for vulnerable food and corpses requires many individual checkboxes. Select
Perishable supplies a useful bulk edit; the owned replacement must explain its actual scope and
respect the selected storage's restrictions and linked policy.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Queue **S114**, origin **replacement**. This change specifies future
behavior only; it delivers no runtime implementation or native acceptance.

## What Changes

- Add one contextual Select/Deselect perishable action to supported storage configuration.
  It adds or removes qualifying item types without clearing unrelated choices or restrictions.
- Preserve the source's food, raw-product and corpse category scope, including descendants, and
  its rot-or-deterioration meaning. This is not a refrigeration-only filter or every degradable item.
- Determine the action's state from the types editable in the current storage policy, not every
  definition in the game. Empty scopes are disabled with an explanation.
- Compose directly with shared linked-storage policy, storage presets and refill rules; preserve
  physical limits, native hauling notifications and bill-output validation.
- Make failures safe, labels readable in seven languages, and original-provider overlap explicit.

## Capabilities

### New Capabilities

- `megamod-perishable-selection`: scoped bulk selection/deselection of perishable storage types,
  retained native restrictions, linked-policy composition and guarded contextual UI.

### Modified Capabilities

None. S005 linked storage, S014 linked workbenches, S049 refill and S050 presets retain their
contracts; this feature uses their owned policy/mutation/recovery paths directly.

## Impact

- This change, [research](../../../mods/megamod/docs/refinement/2026-11-06-select-perishable-research.md),
  [coverage](../../../mods/megamod/docs/replacements/select-perishable.md) and progress ledger.
- Reference `Danoweb.SelectPerishable`, Workshop `3667956712`, installed mod1.1 advertising1.6.
  It is not a dependency. Its active presence disables the owned overlap with a startup warning.
- Harmony becomes required with actual use and is never bundled. No XML operation is selected;
  later XML Extensions use requires its dependency and guarded content targets in that slice.
  No hard DLC, optional storage provider, other product-mod or Dev Gateway dependency.
- No new bottom tab, global/save setting, persistent perishable mode, source API facade, custom
  save migration, map scan/cache or source texture. Storage choices remain native gameplay state.
