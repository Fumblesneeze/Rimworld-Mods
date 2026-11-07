## Why

Colonists should combine small preparatory tasks with their existing work, use supplies thoughtfully,
and avoid pointless idle behavior without overriding player orders, needs or physical constraints.
Common Sense supplies a concrete behavior inventory; this change specifies our owned implementation
and the corrections required when composing it with the other selected features.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: replacement, **S024 / P1-18**, source `avilmask.CommonSense` (Workshop 1561769193).
**Specification only:** no runtime feature or compatibility is delivered or accepted here.

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
- `megamod-common-sense-configuration`: complete preference inventory, scope, lifecycle and source coexistence.

### Modified Capabilities

None in archived capabilities. Existing unimplemented S021 capacity/quantity and S009–S012 browser
contracts remain authoritative; this change adds their Common Sense consumers rather than replacing them.

## Impact

Future native integration touches jobs, cleaning, bills, inventory/stack lifecycle, needs, ingestion,
idle destinations, room labels and information panels. **Harmony** and **XML Extensions**
(`imranfish.xmlextensions`, Workshop 2574315206) will be required for selected guarded integrations.
Common Sense, DLC and external hygiene/inventory/path providers remain optional. No repository-owned
product or Dev Gateway dependency, upstream assembly, package spoof or automatic foreign save import.

Existing consumers include writable source settings, exact assembly/type lookups and a transpiler
against source internals. Their bindings and unsupported differences are documented; a complete
binary drop-in claim is not made. Original Common Sense active withholds overlapping owned behavior
without editing its settings. Detailed proposals and unresolved beta/provider work remain in
[research](../../../mods/megamod/docs/refinement/2026-11-02-common-sense-research.md).
