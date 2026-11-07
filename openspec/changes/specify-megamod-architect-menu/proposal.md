## Why

Large building catalogs can make the architect menu expensive to redraw and awkward to navigate.
The megamod should reduce repeated menu work and offer optional paging without changing which
commands are available, their materials, or the native actions those commands perform.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S036 Architect Menu Optimizer (`MRK.architectmenuoptimizer`, Workshop
`3697920753`) is the inspected source reference, not a runtime dependency or co-owner.

## What Changes

- Specify responsive architect browsing with current availability, native ordering/grouping,
  material choices, labels, colors and layout, including paused and multi-map changes.
- Specify independent optional paging of category buttons and their command lists, preserving
  grouped/dropdown commands, search across pages and native command activation.
- Keep ordinary preferences in common global/save mod settings. Move statistics, refresh actions
  and independent optimization bypasses into developer tools; hide them with developer mode off.
- Consolidate resource-derived material information with the existing shared Thing-data plan,
  while keeping UI-only derived data separate and bounded. No per-port world scans.
- Record exact source algorithms/defaults, observable deviations, provider boundaries and semantic
  startup admission. Require measured responsiveness evidence rather than equating cache hits
  with a performance win.

This is **specification only**. No runtime implementation, host tests, performance improvement or
native gameplay acceptance is delivered here. The handwritten question mark is retained in the
intake; the user's later instruction to specify the whole selected list includes this draft.

## Capabilities

### New Capabilities

- `megamod-architect-menu`: responsive native architect browsing, optional pages, freshness,
  search/navigation, settings, developer diagnostics and compatibility fallback.

### Modified Capabilities

None. Existing owned construction, material-count, formatting and future content-filter behavior
compose directly; this change does not replace their separate contracts.

## Impact

- Future architect UI and derived-presentation code, common settings, shared material-data consumer
  and exact native adapters. No new bottom tab, buildings, recipes, research unlocks or job rules.
- Future Harmony hooks require `brrainz.harmony`; Better Architect Menu, Dubs Mint Menus and other
  menu providers are optional, separately admitted integrations. Source co-installation must select
  one overlapping implementation without removing foreign patches or spoofing package identities.
- No XML patch is presently needed; any later XML Extensions operation requires its dependency,
  content/package guards and merge-preserving edits. No upstream DLL/art or source configuration
  import, and no runtime dependence on other repository mods, profilers or the developer Gateway.
- [Research](../../../mods/megamod/docs/refinement/2026-11-03-architect-menu-optimizer-research.md)
  and [coverage](../../../mods/megamod/docs/replacements/architect-menu-optimizer.md) retain
  source claims, unknown license/source publication, provider mismatches and remaining evidence.
