## Why

Room purposes and zone contents should be readable directly on the colony map, with contextual
names and appearance that survive ordinary editing and travel. S093 replaces Labels on Floor
(continued) with an owned display feature that follows our settings, UI and compatibility rules.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S093/P2-22. Specification only; no gameplay delivered by this change.

## What Changes

- Label meaningful rooms and zones automatically, including growing-zone crops; allow contextual
  names, colors, visibility, orientation and offsets without changing room or storage mechanics.
- Provide classic edge placement and a clearer-space placement option, readable font choices,
  overlap preferences, zoom limits and optional hiding during supported screenshot workflows.
- Put the current display mode in one small bottom-right control, with a keyboard shortcut.
  Put presentation defaults in common global/save settings and individual labels in their editor.
- Make edits transactional, preserve explicit overrides through reload, refresh after actual
  geometry/content changes, and carry supported labels with native Odyssey gravship transfers.
- Correct silent orientation fallback, missing-glyph disappearance, fog leakage and inconsistent
  source defaults. Keep source provenance and unsupported bindings explicit.

## Capabilities

### New Capabilities

- `megamod-floor-labels`: automatic and customized map labels, placement and visibility, contextual
  editing, persistence, inherited preferences and optional screenshot/gravship behavior.

### Modified Capabilities

None. Compose with owned Architect and zone features directly; their mechanics remain unchanged.

## Impact

Future map drawing/input, native room/zone notifications, small saved label records, common settings
and font resources. Core and external Harmony are required on implementation. Odyssey is optional
and required only for its gravship workflow. Progress Renderer is an optional screenshot provider;
its absence does not disable native labels or native screenshot hiding. Source Labels on Floor,
HugsLib, other repository mods and the Dev Gateway are not dependencies.

No XML patch is planned. Any later XML Extensions use requires its dependency and guarded,
merge-preserving operations in the same slice. No foreign assembly or font asset is selected for
shipping by this document; font licensing and live visual acceptance remain implementation gates.
