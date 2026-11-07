## Why

Players want one production plan across linked workbenches, without repeatedly copying every bill
or fixing divergent filters and ordering. Sharing the whole list must also avoid multiplying requested
production or losing unfinished work when the group changes.

## What Changes

- Specify contextual creation, joining, merging, naming, leaving and dissolving of whole-list
  workbench groups, with explicit source-list selection and a preview of overwritten destination bills.
- Synchronize every logical bill, its order and all supported editable choices. Keep the actual
  workstation, pawn, ingredients, unfinished item and machine progress local to the work being done.
- Treat finite repetition counts as a shared remaining order, with concurrent work accounted for;
  target-count bills count the selected map/storage scope once and share their pause/resume state.
- Preserve ordinary bench, worker, resource, research and provider restrictions. Cover ordinary,
  unfinished-product, autonomous and mech production on compatible bill-bearing workstations;
  do not confuse pawn medical orders with workbench production.
- Define recoverable save/load, removal/movement, missing-reference and unsupported-provider
  behavior, with guarded admission and no duplicate production after a failed integration.
- Compose with the selected Add bill browser, linked storage, quality reconstruction and later
  selected bill-management features through direct owned behavior.

This change **specifies later work only**. Origin is **original**, S014 / P1-11. Whole-list
synchronization is confirmed user intent. Same-map grouping, a common recipe catalog, a shared finite
quota, explicit finite-quota allocation on separation and recovery details are proposed refinements
grounded in the inspected native 1.7 bill lifecycle, not additional user confirmations.

## Capabilities

### New Capabilities

- `megamod-linked-workbench-bills`: Whole-list production groups with synchronized choices, shared
  counts, bench-local physical work and recoverable group membership/lifecycle.

### Modified Capabilities

None. This capability supplies the explicit propagation boundary for the existing draft browser
contracts; later selected bill modifiers must compose with it rather than self-patching.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
See [refinement research](../../../mods/megamod/docs/refinement/2026-11-02-workbench-linking-research.md)
and [coverage](../../../mods/megamod/docs/replacements/linked-workbenches.md).

Future code affects contextual bills UI, native bill edits and execution, saved group membership,
production accounting and optional-provider adapters. Work remains ordinary paid production; this
does not rebalance yields, costs, work, skill, quality, machine time or research. Group choices are
gameplay state, not global/save configuration rows. No new bottom tab or cache framework is proposed.

No upstream replacement package, runtime library mod or new dependency is required by these documents.
Optional third-party bill systems need canonical package/shape checks and concrete contracts. Harmony
is required if its types are used; XML Extensions becomes required at its first operation. Nothing
from downloaded mods or the developer Gateway is bundled or referenced by the product. No migration
from another mod's link records, publication or native compatibility acceptance is claimed.
