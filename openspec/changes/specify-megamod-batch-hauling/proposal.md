## Why

Haulers should collect several compatible loads during a trip without receiving an extra carrying
allowance or losing track of the player's personal supplies. Shared item data and coherent delivery
rules should make this cooperate with urgent hauling, storage controls and item search.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement, **S021 / P1-16**, **Pick Up and Haul - Continued**,
`Zzyxzz.PickUpAndHaulReworked`, Workshop **3829549663**. Also compare **Pick Up And Haul - Reworked**
(`Dreadarm.PickUpAndHaul`, 3813653998) and **dd.Armful Haul** (`dd.armfulhaul`, 3808100512).
**Specification only:** no gameplay implementation or native acceptance is delivered.

The [comparison and selected composition](../../../mods/megamod/docs/refinement/2026-11-03-hauling-composition-decisions.md)
reconcile Continued, Reworked, Armful and the narrow modern While You're Up intersection. Use one
owned inventory-based model: Continued's eligible workers and ordinary trip shape, exact cargo
quantities informed by Reworked, grouped deliveries informed by While You're Up, and the required
Armful-inspired visible cargo. Source audits retain exact provenance and omissions. Full S125
opportunity behavior and S133/S135 loading remain separate work; no whole fork is adopted unchanged.

## What Changes

- Specify collection of multiple eligible item types, actual pickup/delivery, reservations,
  destination capacity, interruption recovery and player-kept inventory.
- Apply one coherent load calculation across inventory and hands. Propose explicit mass and
  carrying-volume interpretation of the user's capacity requirement, retaining ordinary single-load
  hauling only after owned cargo is unloaded, rather than adding a second allowance to a full batch.
- Preserve source corpse/animal/mech preferences and the 20% starting-space default through common
  global/save settings, correcting misleading gear-only and boundary behavior. Include optional
  draft-time cargo dropping, initially off to preserve safe retention; never drop personal supplies.
- Compose urgent hauling and future owned storage rules directly, with the shared Ctrl+F item data.
  Preserve real optional contracts where supported; do not copy a private mod cache or internal patch stack.
- Make delivery progress, kept supplies, failed destinations and unavailable integrations observable;
  use exact cargo quantities, grouped physical destination visits and bounded recovery. An unchanged
  failed automatic recovery waits 600 simulation ticks; relevant change or a safe manual retry can
  resume sooner. Preserve real personal-inventory and item-ownership rules.
- Compose the now-selected While You're Up behavior (S125) directly with the chosen hauling rules.
  Include **visible hauled stacks on the pawn** from the selected Armful subset (S126) at minimum,
  preserving actual capacity and quantity rather than granting extra allowances for visual stacks.
  Apply an opportunity's whole-trip bounds and parent-job purpose to its batch, with one shared
  S024/S125 detour decision. Measure data and routing cost before claiming an optimization.

## Capabilities

### New Capabilities

- `megamod-batch-hauling`: eligible multi-item trips, bounded capacity, destination allocation,
  urgency, settings, shared-data freshness and compatibility fallback.
- `megamod-haul-cargo-care`: physical cargo tracking, unload/recovery, personal keep orders,
  inventory presentation and save/lifecycle conservation.

### Modified Capabilities

None in archived capabilities. S008 already requires direct urgent/batch composition and actual
capacity. S005 supplies linked storage semantics; S049 includes the requested additional storage limits.

## Impact

Future work affects ordinary hauling and trained-hauler job paths, inventory/hands transfers,
storage reservations, Gear UI, native take orders, saved cargo and common settings. It introduces
no new bottom tab or extra work category. Own urgent work retains S008's existing priority contract.

Required future runtime dependency: **Harmony** for selected native seams. No required Pick Up And
Haul, IHoldMultipleThings DLL or other repository-owned product mod. Owned Job/WorkGiver Defs are
additive; no blanket pawn-Def comp mutation is selected. If XML Extensions operations become necessary,
XML Extensions becomes a required dependency and every mutation needs content/shape guards.

Allow Tool is an optional external integration candidate through its actual urgent-job delegate
and a guarded urgency-lifetime bridge for our cargo; automatic source designation cleanup must not
erase an undelivered order, and explicit cancellation must remain effective. Owned S008 uses direct
calls. Combat Extended integration is deferred. Other custom inventory or storage providers require explicit
capability validation and bounded adapters; unsupported capacity cannot silently mean unlimited.
Continued has reflective CE handling, unlike the historical Mehni stubs; its permissive fallbacks
are not our admission policy. Broader private
hooks and source save identities are recorded as compatibility gaps, not silently promised emulation.
Co-installing original PUAH, Continued, Reworked or Armful withholds overlapping owned collection
while preserving safe recovery of existing owned cargo. External opportunity hooks also need proven
owned-cargo protection. No automatic source migration, package impersonation or upstream assembly ships.

[Current decisions and linked source research](../../../mods/megamod/docs/refinement/2026-11-03-hauling-composition-decisions.md)
separate confirmed user intent, source facts, proposed corrections and pending provider/native proof.
