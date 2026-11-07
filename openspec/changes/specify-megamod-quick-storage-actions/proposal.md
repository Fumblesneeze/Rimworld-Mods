## Why

Selecting an item should be enough to create suitably filtered storage or stop its current storage
from accepting that type. S107 replaces Quick Stockpile Creation while making batch placement,
linked-policy edits and undo predictable and safe for existing items and filter choices.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S107/P2-37**, Quick Stockpile Creation (`longwater.smartstockpilecreation`,
Workshop1742151109). This change specifies later implementation only.

## What Changes

- Create fresh stockpile zones, small/large shelves and small/large bookcases filtered for selected
  item types or shared categories, including meaningful supported special-state variants.
- Retain source priority defaults and optional promotion above current storage, applying one
  selection-order-independent rule to zones and buildings. Use common global/save configuration.
- Keep ordinary construction, material costs, rotation, research and intrinsic storage limits.
  Make preview and placement agree without altering world contents or shared game definitions.
- Give new zones useful localized names without renaming earlier zones or extending a different
  stockpile accidentally. Preserve native filter persistence through construction and save/load.
- Exclude selected types/categories from their current effective storage policies, including
  supported shelves and linked groups. Keep physical items and unrelated policy unchanged.
- Replace broad category reallow with precise bounded undo that restores only this command's
  changes, preserving prior exclusions and later unrelated edits.
- Consolidate controls on existing item/storage surfaces; compose directly with linked storage,
  presets, refill controls and shared hauling/search data.

## Capabilities

### New Capabilities

- `megamod-quick-storage-creation`: Item-derived storage choices, fresh zones, filtered building
  blueprints, placement/naming, priority and inherited configuration.
- `megamod-storage-exclusion-undo`: Contextual type/category exclusion, linked-policy transactions,
  precise recent undo, lifecycle and safe compatibility behavior.

### Modified Capabilities

None. S005 linking, S049 refill and S050 presets retain their existing contracts.

## Impact

Core and Harmony are required when the planned native integrations ship. No Def or XML mutation is
planned for this feature, so it adds no XML Extensions dependency. All four building templates
exist in inspected Core; optional item/filter/storage providers need their own supported contracts.
No current package or dependency changes in this draft.

Original-provider presence disables the overlapping owned actions with bounded diagnostics, without
removing or unpatching that provider. No public source repository/license was found in inspected
documentation/package; behavior is derived from read-only selected-binary inspection. Independently
implement it and credit slofa; copy no upstream assembly, code, text or art. No actual API consumer
or migration contract justifies source identity spoofing, a facade or silent settings import.
