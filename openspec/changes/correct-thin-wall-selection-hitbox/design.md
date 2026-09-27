## Context

Owner: Thin Walls (`fumblesneeze.thinwalls`, `mods/ThinWalls`). Existing brackets are correct:
`ThinWallSelectionPatch` supplies a structural edge center and size 1 by 34/60 cells (rotated for
vertical edges). Core `Selector.SelectableObjectsUnderMouse` calls `GenUI.ThingsUnderMouse` with
`mustBeSelectable=true`; that query currently admits the whole owner cell. Core's custom selector
rect is an integer CellRect and cannot represent this narrow between-cell rectangle.

## Goals / Non-Goals

Goals: the same stable world-space rectangle for brackets and pointer hit-testing; both adjacent
halves; all cardinal orientations and construction phases; native selection eligibility/cycling.
Non-goals: changing bracket animation/art, drag-box selection, keyboard cell cycling, combat
targeting, placement, map caches, economy, dependencies or saved state.

## Decisions

- Extract the existing size/center into one pure geometry helper consumed by brackets and hits.
  Hit bounds include their border, exclude any point outside, and ignore pointer altitude.
  Use the settled bracket envelope, not its brief native selection animation expansion.
- Postfix the native pointer Thing query, only when `mustBeSelectable` is set. This is a narrow
  design-time Harmony fallback: no fractional native selector-rect seam exists. Do not replace
  Selector's native cycling, colonist-bar, zone, plan or pawn handling.
- Remove only thin candidates whose box misses the pointer. Discover missing thin candidates
  through at most the clicked cell and its eight neighbors, covering both sides and endpoint
  borders. Respect `CanTarget` and append once; preserve all existing candidates' relative order.
  The owner cell is always within this fixed neighborhood because each envelope is at most one
  cell long and straddles one owner edge. No full-map enumeration or retained index is needed.
  Keep candidate reconciliation independent of game state so host tests can exercise missed-edge
  removal, neighbor-hit inclusion, eligibility and deduplication without fake loaded Defs/maps.
- Use existing `TryGetOwnedEdge` for walls, doors, blueprints and frames, including supported
  subclasses, without a material whitelist. No new occupied-cell or rendering offsets.

## Risks / Trade-offs

- Junction boxes overlap: native repeated-click cycling remains authoritative; do not invent a
  new tie-breaking UI. Non-thin candidates retain native ordering; newly discovered edges append.
- Other mods may patch the pointer query: keep a local additive/filtering postfix and native
  eligibility. Do not claim untested integrations.
- The running game must remain untouched: host-only tests/build now, no mod deployment or launch.
  Final player-click verification remains unchecked until explicit user continuation.

## Migration Plan

After authorization, build/install the reviewed package and restart in an isolated minimized
Gateway process. No save migration. Exercise native pointer clicks, not direct Select(Thing)
actions, on each side, outside bounds, co-located L/U edges and ordinary overlapping furniture.

## Scoped playability review

This correction changes only pointer candidate admission. It adds no costs, work, stats, research,
acquisition paths, material restrictions or required/optional dependencies. Native eligibility and
cycling remain authoritative so furniture and other co-located edges can still be selected. Actual
player-click verification, including construction phases and fog eligibility, remains deferred.
