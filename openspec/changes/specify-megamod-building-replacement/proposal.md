## Why

Improving a colony should not require demolishing a functioning wall, losing a workbench's bills or
re-entering storage rules. Replace Stuff combines several construction conveniences; an owned version
needs explicit costs, safe transitions and precise limits on what survives each operation.

## What Changes

- Specify material replacement, different-building upgrades and remembered automatic rebuilding as
  distinct workflows. Retain physical buildings until completion and preserve only defined state.
- Specify construction planning over rock/fog, required bridges, corner access and passers-by,
  with ordinary research, labor, resources and physical safety still applying.
- Specify one- and two-cell wall coolers/vents and their native thermal controls; preserve normal
  build menus and put actual configuration into common global/save settings.
- Compose directly with owned quality construction, linked storage and whole-list bill management.
  Record actual external consumers, unsupported bindings and guarded, additive content rules.
- Correct ambiguous old-building selection, unrelated construction suppression, stale state,
  incomplete bridge costs and configuration loss. These are explicit proposals, not claims that
  upstream bugs have been reproduced or that any gameplay has been implemented.

## Capabilities

### New Capabilities

- `megamod-building-replacement`: material changes, upgrades, costs, building use, cancellation,
  state transfer, automatic rebuilding and provider boundaries.
- `megamod-construction-planning`: rock/fog planning, support terrain, physical construction access
  and inherited construction preferences.
- `megamod-wall-temperature-devices`: ordinary and wide wall coolers/vents, placement, thermal
  behavior, costs, menu visibility and content coexistence.

### Modified Capabilities

None of the archived capabilities. Existing owned draft contracts retain their state, physical-capacity
and finite-budget rules; this change specifies their explicit replacement/rebuild handoff. S013's
draft wording is clarified in place: a material replacement produces its one completion quality
result without an additional automation roll or budget reset. Ordinary native rebuilding after
unrelated destruction still starts S013's fresh finite run.

## Impact

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Source **S028 / P1-22**, Replace Stuff - Continued, `Memegoddess.ReplaceStuff`, Workshop **3526354009**,
original author Uuugggg, continued by Meme Goddess. See
[research](../../../mods/megamod/docs/refinement/2026-11-02-replace-stuff-research.md) and
[coverage](../../../mods/megamod/docs/replacements/replace-stuff.md).

This is **documentation only**. Future Harmony use requires `brrainz.harmony`; the first guarded XML
content edits require **XML Extensions** (`imranfish.xmlextensions`, Workshop **2574315206**).
Neither dependency is added to the inert scaffold now. Original providers and catalogued content
mods are optional; Core workflows do not require them. DLC-specific content is conditional on its
DLC. Exact original co-installation suppresses owned overlap, with lossless pending-work recovery.
No source assembly, package impersonation, automatic legacy-save import, other repository mod
dependency, or developer Gateway dependency is introduced. New raster assets remain subject to
the repository's separate asset workflow; no artwork is selected or accepted here.
