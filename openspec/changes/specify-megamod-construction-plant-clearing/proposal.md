## Why

Placing construction or roof orders should make the necessary plant work visible to plant cutters
before builders arrive. Cut plants before building supplies this convenience, but its actual scope,
tree extraction, worker restriction and interactions need an explicit independently owned contract.

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement **S063/P1-57**, Cut plants before building (Continued),
`Mlie.CutPlantsBeforeBuilding`, Workshop 3286376165, originally by tammybee / 民火.
**Specification only; no gameplay delivered.**

## What Changes

- Specify advance plant orders for accepted building, floor, installation and roof work on the
  actual affected map, including rotated footprints and native automatic roof-area additions.
- Retain ordinary cutting, harvest and optional tree extraction, special-tree exclusions, real
  workers, labor, yield and physical extracted trees. Existing explicit orders retain precedence.
- Keep the operational switch at bottom right with a rebindable shortcut; persist its state per
  game. Put configuration in the common global/save settings, with explicit defaults and timing.
- Compose plant clearing directly with owned construction, replacement, growing and roof work;
  sampling a floor or selecting a construction option must not issue clearing orders.
- **BREAKING relative to source parity:** use actual obstructions instead of a 200-work proxy,
  avoid harvest-only orders that leave the obstruction alive, preserve unrelated designations,
  and require successful placement rather than any return from a placement callback.
- **BREAKING relative to source parity:** scope the Plant Cutting worker restriction to incidental
  construction/installation/roof clearing; do not silently restrict crop harvesting or sowing.
  Keep this configurable restriction independent of the advance-order switch, as in the source.

## Capabilities

### New Capabilities

- `megamod-construction-plant-clearing`: advance plant orders, action choice, order lifecycle,
  construction worker policy, operational controls, settings and provider-aware fallback.

### Modified Capabilities

None in the archived baseline. S006 growing choices, S008 plant orders, S013 quality cycles, S028
replacement, S058 work priorities, S060 access and S061 roof safety retain their contracts. S136
Floor Pipette Tool remains a later separate change; sampling is expressly outside clearing scope.

## Impact

Future runtime work belongs only to megamod. Harmony becomes required when these guarded hooks are
implemented; source providers and external map providers are optional, never runtime dependencies.
No source assemblies, settings migration, copied textures, new content or bottom tab are selected.
No XML mutation is currently proposed; any later XML patch must use the repository's XML Extensions
presence/shape guards and merge rules, with the required dependency declared at first use.

Research and replacement coverage remain separate documents. Actual consumed external bindings,
intentional deviations and unsupported provider cases are recorded there and in design. All focused
TDD, package, independent implementation review and native player-workflow tasks remain unchecked.
