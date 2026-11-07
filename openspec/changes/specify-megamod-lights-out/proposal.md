## Why

Empty rooms and unused equipment consume resources without useful work. S052 selects LightsOut as
a reference for occupancy-driven lighting and standby consumption, with an explicit contract for
equipment that continues working without an attending pawn.

## What Changes

- Specify room departure, sleep, animal occupancy, outdoor exclusions and each light's Keep On choice.
- Specify standby for workbenches, research, drills, televisions, rechargeable equipment and the
  source's admitted DLC/provider equipment, preserving normal work and resource availability.
- Preserve source defaults of 1.5 seconds departure delay, 0% standby and 100% active consumption,
  for equipment outside enabled S053 budgets, including the documented 0.01 W electrical standby
  floor capped to smaller positive native demand. Make state changes cancel obsolete
  shutdowns and reconstruct actual use after loading or moving buildings.
- Use common global/per-save settings, one bottom-right lighting switch and contextual per-light
  controls. Keep diagnostic tools in the developer surface; add no bottom tab.
- Specify safe optional coverage, accurate power information, original-provider suppression and
  direct composition with owned work features. S053 uses this feature's actual-use activity for
  shared work equipment and supplies its costs once; other equipment and lamps retain these rules.

## Capabilities

### New Capabilities

- `megamod-lights-out`: Automatic room lighting, activity-based standby consumption, controls and
  safe provider/consumer behavior.

### Modified Capabilities

None. Foundation settings, patch admission and shared-data contracts remain authoritative.

## Impact

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Documentation only; no gameplay implementation or acceptance.

Future runtime requires Core, Harmony (`brrainz.harmony`) for admitted patches and XML Extensions
(`imranfish.xmlextensions`, Workshop2574315206) when the planned guarded additive building-state
patches are implemented. Ideology and Biotech are optional; their absence removes only their
equipment branches. External light/equipment providers and description consumers are optional,
package/content-resolved adapters, not content dependencies of the whole mod. The research and
design enumerate independently verified package identities and unresolved legacy providers.

No upstream assembly, package identity, resource network, settings importer or artwork is shipped.
No repository-mod or Dev Gateway runtime dependency is introduced. See the
[research](../../../mods/megamod/docs/refinement/2026-11-03-lights-out-research.md) and
[coverage report](../../../mods/megamod/docs/replacements/lights-out.md).
