## Why

KanbanStockpile reduces repeated small hauling trips, but its stack-local controls do not express
the user's desired stock targets across linked shelves and stockpiles. Specify one coherent refill
policy that shares demand with the owned hauling features while preserving ordinary storage capacity.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: replacement, **S049 / P1-43**, KanbanStockpile Continued (`endag.kanbanstockpile`,
Workshop3767756434), with confirmed custom group totals, refill cycles and per-stack hauling limits.
This change delivers **specification only**; every implementation and live-acceptance task remains open.

## What Changes

- Add contextual per-item-type refill quantities: begin below X, continue until Y, count all linked
  storage once, and show actual stock separately from incoming deliveries.
- Retain an optional similar-stack-count limit, explicitly adapted to the same item-type/group
  model. A separate single stack-fill slider defaults to Unlimited and limits automatic hauling
  into each stack without changing any item's physical stack size or resizing existing stacks.
- Coordinate normal and batch automatic hauling, reservations, partial delivery and cancellation
  so multiple workers do not each fill the same unmet demand. Retain native work, carrying and
  destination restrictions; no artificial capacity or balancing transfers within a satisfied group.
- Keep production output and explicit placement distinct from automatic refill: they can exceed
  the target. Refill controls neither invalidate existing stored items nor demand their removal.
- Extend S005's complete linked policy and copy/paste lifecycle directly. Keep controls on the
  contextual storage surface, with no new bottom tab or per-storage entries in mod settings.
- **BREAKING relative to the reference:** replace stack-local percentages with item quantities and
  a persistent refill cycle; omit the reservation-check opt-out and original PUAH fallback switch.
  No automatic conversion of the different source save schema or third-party identity spoofing.

## Capabilities

### New Capabilities

- `megamod-storage-refill`: contextual refill targets, linked totals, stack controls, coordinated
  automatic delivery, lifecycle, non-refill boundaries and compatible fallback.

### Modified Capabilities

None of the archived capabilities. S005 linked storage, S021 hauling and S033 shared data are
unimplemented sibling drafts; this change supplies their concrete refill consumer contract.

## Impact

Core is required. Harmony becomes a required runtime dependency when the feature is implemented;
none is added to the inert product here. XML Extensions is not required by this proposed C# feature.
Original KanbanStockpile is a reference/co-installation exclusion, not a dependency. External
storage and hauling providers are optional, package-resolved and admitted only for verified shapes.
Owned S005/S021/S027/S033 and future S050/S125/S126 behavior composes directly, without self-patches.
No Workshop code, binary, art, configuration or save is modified or imported.

See the [research record](../../../mods/megamod/docs/refinement/2026-11-03-kanban-stockpile-research.md)
and [replacement coverage](../../../mods/megamod/docs/replacements/kanban-stockpile.md) for provenance,
source controls, deliberate deviations and remaining questions.
