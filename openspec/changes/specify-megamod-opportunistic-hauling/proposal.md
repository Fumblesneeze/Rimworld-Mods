# Useful hauling detours

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **RimWorld 1.7 beta**.
Origin: replacement, S125 **While You're Up**, selected modern reference `patch1.7.whileyoureup`
(Workshop3832180338); original CodeOptimist2034960453 retains attribution. **Specification only.**

## Why

Pawns can use a trip to another job to deliver nearby goods, or move extra ingredients closer before
working. These opportunities should save later hauling without inflating capacity, continually
postponing the original work, or creating a second hauling system beside S021 and S024.

## What Changes

- Specify finite general opportunities, three search choices, bounded whole-trip travel, separate
  destination filters, and construction/bill supply relocation, including a narrow equal-priority
  exception. Preserve ordinary worker, safety, storage and material restrictions.
- Compose directly with S021's actual quantity/capacity/delivery rules and S024's bill detours.
  One pending work attempt receives at most one detour; linked settings never imply shared space.
- Put configuration in common global/save settings, the current-play enable switch at bottom right,
  job purpose in contextual reports, and route diagnostics in developer controls. No new bottom tab.
- Deliberately omit the port's capacity multiplier, foreign settings edits, per-type cargo
  substitution, unbounded search expansion and unsupported legacy binary impersonation. Preserve
  source differences, external consumers and pending questions in the replacement ledger.

## Capabilities

### New Capabilities

- `megamod-opportunistic-hauling`: useful, bounded storage detours before other work, storage-type
  configuration, interruption/recovery, visible purpose and safe provider fallback.

### Modified Capabilities

No archived baseline capabilities change. Reconcile the active S021 `megamod-batch-hauling` draft
and S024 `megamod-practical-supplies` draft in their existing owning changes: ordinary batches still
require better storage, S125 alone permits the described work-bound equal-priority relocation,
and both bill-detour sources share one attempt. Their ownership remains this same mod.

## Impact

Future native job admission, construction supply and bill selection, shared item facts, S021 cargo
and S049 refill rules are affected. No code, Def, dependency metadata or gameplay delivery changes
in this documentation slice. S126 already owns visible cargo; S133/S135 own later loading work.

RimWorld is required; Harmony becomes required with actual patches. No new XML mutation is planned;
if one is needed, XML Extensions becomes required in that slice. DLC, original opportunity/hauling
providers, Smart Medicine, Common Sense and custom storage/vehicle-map providers are optional
inspection/integration inputs, never product dependencies. Original opportunity providers suppress
the owned overlapping scheduling whenever those originals are loaded. No package spoofing, migration,
source assembly copying or automatic subscription/configuration changes.

[Research](../../../mods/megamod/docs/refinement/2026-11-06-while-youre-up-research.md) and
[coverage](../../../mods/megamod/docs/replacements/while-youre-up.md) retain provenance and omissions.
