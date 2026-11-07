## Why

Loose chunks should not force players to clear a building's work position before they can even
plan it. S129 adopts Interaction Spot On X's limited placement convenience, with optional trees,
while keeping construction, movement and actual work subject to their ordinary requirements.

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. **Specification only**, no implemented or accepted game behavior.

## What Changes

- Allow ordinary building interaction positions to overlap qualifying chunks by default; offer
  an independent tree option, off by default. Use the same rule for every required interaction
  position and the current placement rotation, including supported install/reinstall workflows.
- Preserve map bounds, all other blocking objects and planned obstructions, the caller's ignored
  object, footprint/terrain restrictions and additional provider checks. Do not convert an
  unexplained rejection into success merely because a chunk or tree is nearby.
- Leave obstacle removal, hauling, cutting, construction costs and actual pawn use unchanged.
  Compose with owned orientation, replacement planning, plant clearing and construction access.
- Put the two behavior preferences in common global/save mod settings. Move optional diagnostic
  logging from upstream settings to the top debug toolbar; it is temporary, developer-only and off.
- Preserve original-provider authority when co-installed, withhold owned overlap and warn. Record
  source defects and missing external/native proof without claiming a blanket drop-in replacement.

## Capabilities

### New Capabilities

- `megamod-interaction-spot-placement`: obstacle-specific planning relaxation, retained placement
  and work restrictions, scoped preferences and compatibility fallback.

### Modified Capabilities

None. Related owned construction/placement drafts are consumed directly, retaining their ownership.

## Impact

- This change, S129 research, replacement coverage and refinement tracking only.
- Future game hook needs Harmony and exact semantic admission. Source mod is an optional reference,
  not a dependency; no DLC or other repository mod is required. No XML/content changes selected;
  XML Extensions would become required only if an actual future XML operation needs it.
- No copied upstream DLL/source/assets; no license file found in the pinned public source tree.
- Future focused TDD, target documents, seven-language settings, independent review and native
  placement-to-construction/use workflows remain unchecked. No game launch is required for this draft.
