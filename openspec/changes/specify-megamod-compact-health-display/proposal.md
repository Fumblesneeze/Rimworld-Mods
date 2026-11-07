## Why

Dense health lists make it difficult to compare injured body parts, distinguish individual wounds
and follow treatment progress. Compact Hediffs provides useful presentation behavior that needs an
independent, explicit contract and current 1.7 rendering verification before it is replaced.

## What Changes

- Specify body-part labels, grouped conditions, urgency ordering, individual bleeding/tending icons,
  health and progress bars, tooltips, information cards and native combat-log navigation.
- Preserve customization through common global/save settings, with compact-view switching on the
  bottom-right controls and diagnostic/override actions in the top developer toolbar.
- Correct grouped-row overlap, incomplete toggle disabling and misleading progress limits; preserve
  distinguishable condition details instead of hiding meaningful differences in a merged label.
- Record optional medicine/body-part/mutation contracts and explicit 1.7 research gaps.

This is specification work only. No health display, assets or gameplay behavior is implemented.

## Capabilities

### New Capabilities

- `megamod-compact-health-display`: Configurable, readable health-condition presentation and interaction.

### Modified Capabilities

None.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Source: Compact Hediffs, `PeteTimesSix.CompactHediffs`, Workshop `2031734067`, S007 / P1-07.
Core is required; Harmony becomes required when used. Original Compact Hediffs is not required.
Smart Medicine, Choose Your Medicine, EBF, Pawnmorpher and relevant DLC are optional integrations;
their absence must leave ordinary health display usable. XML Extensions is required only if its
operations are introduced. No upstream assembly, game code or unaudited raster asset will be shipped.
