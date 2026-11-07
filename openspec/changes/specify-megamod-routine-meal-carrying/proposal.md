## Why

Colonists should routinely have a suitable meal available while working away from storage. One
shared reserve policy should keep that food distinct from deliveries, respect actual carrying
limits and avoid repeatedly picking up food that another owned inventory rule immediately unloads.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld1.7 beta**, origin **original**, queue **S123**. The user confirmed routine meal
carrying for all colonists, excluding animals. Other defaults below are explicit proposed refinements,
grounded in native behavior; no source mod named Carry Food is selected or replaced.

## What Changes

- Use the existing colonist packing opportunity for a personal food reserve, with the already
  selected S02430% Food trigger without a communal-supply cutoff. Do not add a parallel packing loop.
- Distinguish personally available food from delivery cargo, protected transport, current eating
  portions and another pawn's supplies. Use S021's single quantity and capacity authority.
- Propose a default reserve target of one full Food bar, refill at40% or less of that target, allow
  partial useful pickups and retain one indivisible meal when it exceeds the target. Make the
  reserve target common global/save configuration; explicit pawn pause/resume stays contextual.
- Keep diet, actual nutrition, title, freshness, access and safe self-packing eligibility meaningful.
  Preserve ordinary eating and S055 sharing rather than treating a reserve as private food nobody
  else can use. Manual unloading must not start an immediate replacement loop.
- Show reserve and pause information in native inventory context with no new bottom tab, no food
  generation and no new per-source cache.

**Specification only.** [Research](../../../mods/megamod/docs/refinement/2026-11-06-routine-meal-carrying-research.md)
separates native facts, original intent, proposed changes and remaining evidence. Nothing is
implemented, tested in game, migrated or accepted here.

## Capabilities

### New Capabilities

- `megamod-routine-meal-carrying`: personal reserve eligibility, quantity, physical pickup and
  retention, supply/capacity limits, sharing and cleanup, contextual control and shared settings.

### Modified Capabilities

No archived capability changes. The active S024 `megamod-practical-supplies` draft is revised in
place in the same slice: its packing requirement delegates complete quantities/retention to S123
and retains its30% trigger while removing the communal shortage check. Both sources remain owned by this one product;
their behavior composes directly, not through internal patches.

## Impact

- RimWorld required; other inventory/food providers optional and shape-admitted. No original-mod
  co-installation rule is invented for this original feature. Actual overlapping packers must have
  a single selected authority; unsupported overlap disables only this owned opportunity with warning.
- Harmony becomes required if the owned inventory/packing hooks use it. XML Extensions
  (`imranfish.xmlextensions`, Workshop2574315206) is required for the guarded ThinkTree/XML work
  if that implementation is selected. Do not mutate resolved definitions as an XML shortcut.
- Reuse S021/S033 quantities/facts, S024 food preferences/recovery, S041 food history, S055 sharing,
  S057 finite food scoring and native inventory stock. S111 room preference deliberately excludes
  reserve packing. Medical, bedroll and transport supplies retain their existing ownership.
- No food-policy edits, new food content, capacity multiplier, automatic source-settings import,
  runtime library mod or dependency on another repository product/Dev Gateway.
