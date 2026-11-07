## Why

The richer Add bill view needs recoverable recipe organization, trustworthy shortage cues and an
unambiguous quick-add action. Nice Bill Tab - Expansion supplies useful reference interactions,
but its separate visibility lists, material-blind quick-add and independent stock caches need
deliberate adaptation to the user's composed workbench workflow.

## What Changes

- Propose contextual recipe hiding at this bench, this bench type, or all benches in this save,
  with explicit recovery and no effect on existing bills. Applicable hiding records combine;
  changing an action's scope does not silently switch which records are honored.
- Add an optional row Add action that uses the displayed recipe, style and material, preserving
  S009's native bill semantics and S010's read-only inspection.
- Show ingredient shortages against the same finite allocation used by comparison, distinguishing
  shortages, unresolved information and skill restrictions instead of treating every unmarked icon
  as sufficient stock.
- Preserve meaningful recipe order and access to all controls as the contextual view narrows.
  Search/category/stat composition and final overall layout belong to S012.
- Use common inherited settings for quick-add and shortage cues. Keep hiding choices with their
  gameplay scope and keep all controls within Add bill or mod settings, without a bottom tab.
- Record source-only queue layout, clone import/export and tag-generated bills as outside this
  UI-reference slice. Selected later BWM/Best Mix functionality will compose directly.

This change **specifies later work only**. Hiding scopes, their combined interpretation and the two
setting defaults are explicit proposed details derived from the reference, not additional interview
answers. Open questions are recorded for later; no implementation or compatibility is claimed.

## Capabilities

### New Capabilities

- `megamod-recipe-list-controls`: Recoverable recipe visibility, material-aware row addition,
  ingredient status cues and resilient contextual browsing.

### Modified Capabilities

None. Consume S009 material comparison/creation and S010 inspection without changing their rules.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Source: **Nice Bill Tab - Expansion** (`HICON.NiceBillTabExpansion`, Workshop `3721023311`), S011 /
interview:P1-09. See the [report](../../../mods/megamod/docs/replacements/nice-bill-tab-expansion.md)
and [research](../../../mods/megamod/docs/refinement/2026-11-02-nice-bill-tab-expansion-research.md).

Future work touches Add bill, saved recipe-visibility choices and shared recipe/map information.
Native production costs, research, jobs and existing queue configuration remain unchanged.
Nice Bill Tab, its Expansion and Quartermaster are references, **not runtime dependencies**.
BWM, Best Mix, Dubs Mint Menus and Compositable Loadouts are optional provider cases; this draft
does not promise their custom controls. No external dependency is introduced by prose. Harmony
becomes required if used; any later XML Extensions operation requires XML Extensions. Do not ship
upstream assemblies or reference the developer Gateway from the product.
