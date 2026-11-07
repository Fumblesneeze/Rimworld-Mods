## Why

Players need to distinguish a power network's production, actual consumption and remaining battery
reserve. S102 makes those facts visible where a connected building is inspected, with clearly
qualified estimates rather than a promise that changing generation or demand will last forever.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S102/P2-32, More power info** (`arl85.MorePowerInfo`). This change specifies
future behavior only; it delivers no implementation or accepted compatibility.

## What Changes

- Show the selected network's current production, consumption, balance and usable stored energy.
- Show a current-rate depletion estimate and a separate estimate with generation removed, accounting
  for admitted battery losses and charging behavior. Distinguish empty, non-depleting, full, disabled
  and unavailable states; explain that these are snapshots of current operation.
- Keep disconnected, device-specific and other mods' inspection information intact. Add no bottom
  tab, control window, debug action or configuration solely for these readouts.
- Reflect actual Lights Out/RePowered demand through direct owned composition and native network
  membership, without applying their rates twice or creating another Thing index.
- Guard the inspection contribution and forecasting contracts independently; unsupported forecasting
  must not remove trustworthy totals or interfere with the power simulation.

## Capabilities

### New Capabilities

- `megamod-power-inspection`: Contextual network totals, qualified battery estimates, truthful
  boundary states, live lifecycle and compatibility-preserving presentation.

### Modified Capabilities

None. Existing Lights Out/RePowered rate and work-start rules remain authoritative and unchanged.

## Impact

Core is required; no DLC or external power mod is required. Harmony is required when implementing
the native inspection hook. External battery/power providers are optional and require admitted
semantics only where they differ from the native contracts. Original `arl85.MorePowerInfo` owns its
readout when co-installed, suppressing the overlapping owned contribution without unpatching it.

No XML changes are proposed. Any later `XmlExtensions.*` use requires XML Extensions metadata and
guards at first use. No upstream assembly, image or code is bundled; the inspected author repository
contains no identified license grant. Independently implement from the behavior specification.
