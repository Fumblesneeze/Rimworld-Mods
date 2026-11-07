## Why

The player wants unwanted items destroyed where they lie by repeated real pawn melee attacks.
The order must retain ordinary damage and destruction effects while staying bound to the selected
items, with cancellation and interruption preventing further authorized attacks.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: **original**, S023 / P1-17. Destroy Item is a research comparator, not a full replacement
selection. **Specification only:** no gameplay implementation or native acceptance is delivered.

## What Changes

- Specify Orders/contextual designation of destroyable items that actually use HP, excluding
  installed buildings, plants, living pawns and HP-less items. There is no instant-delete mode.
- Have an eligible worker approach and repeatedly perform normal melee attacks, using real attack
  selection, cooldown, damage and item consequences; never substitute a work-timer/DPS calculation.
- Preserve current HP on cancellation/interruption, exact selected quantities through item movement,
  and the chosen target without combat auto-retargeting or bashing a blocking door.
- Compose item-processing choices directly with S022 recycling and S096 Micro Designations, and
  use shared item facts rather than another full-map cache.
- Propose one worker per target, ordinary Hauling assignment, bounded ineffective-attack handling
  and explicit suspension/resume when safe native execution cannot be established.

## Capabilities

### New Capabilities

- `megamod-melee-item-destruction`: item eligibility, real on-site attacks, native effects,
  quantity/order ownership, interruption, stalled work, UI and compatibility boundaries.

### Modified Capabilities

None in archived capabilities. S022 already requires one mutually exclusive owned processing
choice; S096 will compose other recipe actions through that same direct policy.

## Impact

Future work affects native item orders, Hauling job assignment, melee action selection, item/stack
lifecycle and saved order suspension. No bottom tab, new work column, battle target automation,
source speed/instant/multi-worker settings or copied source thoughts/records are introduced.

Required future dependencies: **Harmony** for selected native UI/lifecycle seams and **XML Extensions**
(`imranfish.xmlextensions`, Workshop `2574315206`) for guarded additive Orders registration.
Original Destroy Item, Recycle This, combat/content providers and DLC remain optional. No original
package, save, Def or assembly impersonation, and no repository-product or Dev Gateway dependency.
Original Destroy Item present withholds overlapping owned attacks and suspends existing orders;
safe later activation requires explicit resume. Unsupported native/provider semantics never fall
back to deleting the target directly.

[Research and open questions](../../../mods/megamod/docs/refinement/2026-11-02-melee-item-destruction-research.md)
separate confirmed real-melee scope from proposed item classification, worker and recovery choices.
