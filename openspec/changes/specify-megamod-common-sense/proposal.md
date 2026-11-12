## Why

Colonists should combine small preparatory tasks with their existing work, use supplies thoughtfully,
and avoid pointless idle behavior without overriding player orders, needs or physical constraints.
Common Sense supplies a concrete behavior inventory; this change specifies our owned implementation
and the corrections required when composing it with the other selected features.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S024 / P1-18**, source `avilmask.CommonSense` (Workshop 1561769193).
The first selected implementation batch is scheduled potential-mood checking and its direct S138
tolerance composition. Other S024 behaviors remain specified but unimplemented. Native acceptance
remains open until the reviewed player workflows pass.

## What Changes

- Specify bounded cleaning around work, eating and patient care, a contextual per-building choice,
  and postponing snow/sand clearing while relevant precipitation continues.
- Specify capacity-limited bill ingredient collection, S125's shared useful-surplus storage detours, perishable
  ingredient/meal preference, meal separation/provenance, readable ingredient/category information,
  and explicit inventory unloading or return after interruption.
- Specify outdoor recreation preference, recreation completion/hysteresis, economical social drinks
  and meditation, potential-mood drug decisions, patient recreation deliveries and optional sleeping
  mood movement. Preserve actual policy, ingestion and medical requirements.
- Specify optional weather-aware and considerate idle wandering, with actual hazard immunity,
  destination ownership and bounded searches instead of copying identified source mistakes.
- Compose S021 hauling, S125's single bill-supply preference and parent-attempt decision, S009–S012
  bill browsing and S029/S030/S058/S062/S123 behavior directly.
  Do not introduce another hauling inventory, item cache or internal mod-patch stack.
- Apply common global/save preferences and native contextual entity controls; omit the source's
  capacity-bypass and startup-patch settings. Admit only supported behavior on the exact beta.

## Capabilities

### New Capabilities

- `megamod-contextual-housekeeping`: cleaning opportunities, limits, building control and precipitation.
- `megamod-practical-supplies`: bill collection, food/material choice, meal metadata, inventory and information.
- `megamod-practical-needs`: recreation, patient treats, drug policies, meditation and optional sleeping mood.
- `megamod-considerate-wandering`: idle shelter and destination choices without general pathfinding replacement.
- `megamod-common-sense-configuration`: complete preference inventory, scope, lifecycle and ordinary feature disablement.

### Modified Capabilities

None in archived capabilities. Existing unimplemented S021 capacity/quantity and S009–S012 browser
contracts remain authoritative; this change adds their Common Sense consumers rather than replacing them.

## Impact

Future native integration touches jobs, cleaning, bills, inventory/stack lifecycle, needs, ingestion,
idle destinations, room labels and information panels. **Harmony** and **XML Extensions**
(`imranfish.xmlextensions`, Workshop 2574315206) will be required for selected guarded integrations.
The scheduled-mood batch uses C# and the existing Harmony dependency; XML Extensions becomes
required when an actual owned XML consumer is implemented, rather than for this independent slice.
DLC and genuine external hygiene/inventory/path integrations remain optional. No repository-owned
product or Dev Gateway dependency, upstream assembly, package spoof or automatic foreign save import.

Recorded source consumers are research only; source API/binary/save compatibility checks, facades
and consumer parity are outside scope. Active original Common Sense produces a bounded startup
warning and uses the ordinary per-feature settings disable path for the owned overlap, best effort,
without promising cooperative operation or editing the original's settings. Detailed proposals and unresolved beta/provider work remain in
[research](../../../mods/megamod/docs/refinement/2026-11-02-common-sense-research.md).
