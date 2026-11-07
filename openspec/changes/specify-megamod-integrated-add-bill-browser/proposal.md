## Why

The player wants to find, compare and queue the right recipe directly at a workbench, with material
choices carried into the bill. A separate equipment browser or a collection of source-mod windows
would split that workflow; this original feature composes the three researched reference slices.

## What Changes

- Replace the ordinary Add bill dropdown, when enabled and safely supported, with a contextual
  browser containing search, product-category pills, a relevant stat-sort menu and Use all materials.
- Define search and category matching for distinct recipes/styles and multiple outputs, including
  empty-state recovery and recipes without a predictable categorized product.
- Give common product properties an explicit initial menu priority while keeping unknown facts,
  material-dependent values and multi-output ranking honest.
- Compose S009 comparison/creation, S010 inspection and S011 list controls directly. Specify clear
  selection, comparison, Add and alternate-material gestures, including ordinary unrestricted
  material planning when no stat is selected.
- Keep native queue controls and provider-only actions available. Add one inherited browser-enable
  setting; there is no new main tab or replacement queue-management system.

**Origin: original**, from the user's interview. This is **specification only**, with no runtime
behavior delivered. Search matching, category selection semantics, common-property ordering and
specific gestures are proposed details where the interview did not fix them; remaining questions
are recorded rather than interrupting the sequential work.

## Capabilities

### New Capabilities

- `megamod-integrated-add-bill-browser`: One contextual recipe browser with composed search,
  filtering, ranking, inspection and explicit native bill actions.

### Modified Capabilities

None. This change supplies the composition decisions deliberately left to S012 by the proposed
comparison, inspection and list-control capabilities; it does not redefine their quantities or actions.

## Impact

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
See [original-feature report](../../../mods/megamod/docs/replacements/integrated-add-bill-browser.md)
and [refinement](../../../mods/megamod/docs/refinement/2026-11-02-integrated-add-bill-browser-refinement.md).
Nice Bill Tab, its Expansion and Quartermaster are behavioral references, not required dependencies.
Other bill/stat/recipe providers are optional; support requires exact guarded integration and live
evidence. No new dependency or package contents result from this prose change. Harmony becomes
required if used; any future XML Extensions operation requires XML Extensions. The developer
Gateway and upstream assemblies remain excluded from the product.

Later code affects the workbench Add bill entry, temporary browser state and common settings/data.
It does not change production costs, actual quality rolls, research, work priorities or existing bills.
Whole-list workbench linking has its own later owner and must consume the single native add action.
