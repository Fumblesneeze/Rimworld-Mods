# Nice Bill Tab - Expansion — recipe-list controls

## Identity and provenance

- Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
- Origin: **replacement/reference adaptation**, S011 / interview:P1-09. The user named this as a
  richer Add bill UI reference, not a request for every upstream queue or integration feature.
- Source: **Nice Bill Tab - Expansion**, **HICON**, package **HICON.NiceBillTabExpansion**, Workshop
  **3721023311**. Inspected 2026-11-02, documentation first, then installed read-only decompilation.
- [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3721023311),
  [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3721023311),
  [older notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3721023311?p=2).
  Cached pages differ in update count; the binary identity controls this inspection's provenance.
- Read-only root `F:/Steam/steamapps/workshop/content/294100/3721023311`, 1.7 assembly/file version
  **1.0.0.0**, informational **1.0.0**, SHA256
  **E4FFD4EE3B9C79DA1E2767D5751BB7175032957D4E287AC0652E37C1D550F212**.
  About supports 1.6/1.7 and requires Harmony/Nice Bill Tab; those are source dependencies.
- No public source repository or license was established from the inspected sources. Do not copy
  code, icons or assemblies. Attribution here identifies behavioral provenance, not reuse permission.
- [Research journal](../refinement/2026-11-02-nice-bill-tab-expansion-research.md) gives exact source
  paths, keys, defaults, native inputs and evidence limitations. Decompilation demonstrates code
  paths, not successful game behavior. No live test was performed.

Owning change: [specify-megamod-recipe-list-controls](../../../../openspec/changes/specify-megamod-recipe-list-controls/).
Observable contract: [megamod-recipe-list-controls](../../../../openspec/changes/specify-megamod-recipe-list-controls/specs/megamod-recipe-list-controls/spec.md).

## Behavior inventory

All selected rows are **specified, not implemented**. Source facts and intentional changes remain
separate in the evidence/deviation columns.

| ID | Native input/action → proposed result | Options/defaults and edges | Evidence / owning requirement | Status |
| --- | --- | --- | --- | --- |
| NE-01 | Hide recipe → ordinary list omits all its style/material variants within selected bench/type/save scope | This-bench initial action scope; applicable records combine; existing bills unaffected | HiddenRecipeStore, source filtering; “Recipe hiding has an explicit gameplay scope” | Specified; combined scopes intentionally differ |
| NE-02 | Open hidden list/remove scoped record → recover recipe without deleting other records | Filters remain usable; clear-filters reachable; explicit broad effects; hidden recipes can still be added | HiddenRecipeViewMode, row/toggle buttons; “Every active hiding choice remains recoverable” | Specified; recovery strengthened |
| NE-03 | Save/reload or move same bench → retain stated hiding records | No cross-save leak; new replacement bench has no old local record; missing recipe dormant | Source ExposeData/game component; “Visibility survives ordinary saves without leaking browsing state” | Specified; exact native lifetime NE-Q1 |
| NE-04 | Click optional row Add → one bill for displayed recipe/style/material | Off initially; stale choice explains failure; no inspection mutation or duplicate event | RecipeRowQuickAddButton and row prefixes; “Row addition uses the displayed choice once” | Specified; source null-material path replaced |
| NE-05 | Inspect list/details → understand exact shortages or unknown/updating | On initially; one finite allocation; skills distinct; no job-readiness promise | MissingIngredientIconSlashes and detail patch; “Ingredient cues explain shortages without inventing sufficiency” | Specified; source overlap/fallback/cache behavior replaced |
| NE-06 | Hide/restore or change stock while paused → current information with stable ordering intent | No silent material substitution; filter-removed inspection clears with explanation | Source ordering/preload/filter paths; “Browsing updates without changing the player's intent” | Specified; final ordering S012 |
| NE-07 | Narrow/scale the view → controls and full-label explanations remain reachable | Responsive disclosure/scrolling; no invisible actionable overflow | Source side-by-side sizing/row reserve; “Narrow layouts preserve access to actions” | Specified; no fixed source-column promise |
| NE-08 | Change configuration → inherited global/save values apply next redraw | Quick-add off, shortage cues on consistently; entity records remain contextual | Source settings/native loading; “Configuration uses the common global and save controls” | Specified; source global-only/default discrepancy changed |
| NE-09 | Unsupported provider/UI boundary → safe existing route and bounded warning | No pristine-method requirement; active incompatible original keeps its view | Source patch installer/adapters; “Controls preserve native and external bill behavior” | Specified; compatibility not accepted |
| NE-10 | Source separate whole Bills/recipes columns and bill search | Includes source queue edits and fixed widths | SideBySideBillsTabDrawer | Deliberately omitted from this Add bill reference slice |
| NE-11 | Source provider buttons for BWM/Best Mix | External bill configuration/clipboard semantics | BWM/BestMix buttons | Deferred to those selected owners; no self-patches |
| NE-12 | Source Dubs clone UI and Compositable tag-driven bill creation | Dubs delegates clone UI; tag action can add multiple bills, with partial failure possible | Dubs/Compositable adapters | Deliberately omitted; no new import/auto-production request |
| NE-13 | Source early preload and separate stock/shortage caches | Not a player contract or measured speedup | BillTabPreloader, shortage cache | Not ported; shared data and measured cost required |

## Interactions and composed result

[Quartermaster comparison](quartermaster.md) owns the material choice, Normal-quality facts, finite
ingredient use, all-skills check and actual add action. [Nice Bill Tab inspection](nice-bill-tab.md)
owns read-only details and two-choice comparison. This Expansion slice adds contextual controls;
S012 will compose the final browser directly. There will be no Expansion-style Harmony patches
against megamod's own UI. Row Add and the main Add action must use the same exact chosen variant.

BWM/Best Mix are selected later sources. Do not preempt their contracts with the source adapter's
assumptions or duplicate their policy here. Native bills, research and ordinary jobs remain unchanged.
No demonstrated PUAH/Allow Tool conflict is implied by shared ingredient data. Shared candidates
serve Ctrl+F/hauling; pawn/job-specific restrictions remain native action-time checks.

## UI audit and relocation

The source adds no independent bottom main tab. It augments the selected bench's Bills inspect tab
and mod settings; internal Bills/Clones tabs are contextual, not main-bar additions.

| Source entry / purpose | Classification | Megamod destination / change |
| --- | --- | --- |
| Recipe hide/unhide, hidden toggle | Entity/gameplay browsing state | Contextual Add bill; explicit scope and always-reachable recovery |
| Quick-add and shortage marks | Contextual production action/information | Add bill rows/details using shared variant and allocation |
| Global scope/options in Expansion settings | Mixed entity choice/configuration | Scope moves to hiding action; two true settings use common inherited Crafting / Add bill rows |
| Separate bill/recipe columns and clone tab | Queue management/import UI | Not copied; existing queue/provider route retained |
| BWM/BestMix/Compositable buttons | Bill configuration or generation | No extra main tab; later selected owners or explicitly omitted custom generation |
| Settings cog opens original Nice Bill Tab settings | Configuration navigation | Any megamod configuration entry opens megamod's common settings |

Two settings, ordered row Add then shortage cues, use global defaults and saved overrides, with
Use global editable in a loaded save and the override editor disabled during inheritance. Main menu
disables both save controls. Dormant overrides survive inheritance; hiding data is entity/game state,
not a settings exception. Hidden-view mode is temporary; no general bottom-right toggle is added
for state that exists only inside the contextual browser. Seven-language and scaled-layout native
acceptance remain required.

## Contracts consumed by other mods or saves

| Consumer or provider evidence | Binding / lifecycle | Disposition and later verification |
| --- | --- | --- |
| Bounded installed incoming-name scan | NiceBillTabExpansion found only in own metadata/strings/assemblies | No external incoming contract identified; computed bindings not excluded; no facade promised |
| Nice Bill Tab, required source dependency | Expansion patches/reflection over NiceBillTab UI, source patch methods and static fields | Direct owned composition replaces this dependency; not a binary drop-in replacement |
| BWM `falconne.BWM` | Source NBT BetterWorkbenchesIntegration copy/paste/link and bill settings | Later BWM owner; keep actual native/provider action accessible; test present provider before support |
| Best Mix `Mlie.BestMix` | BMBillUtility.GetBillBmTex(Thing,Bill), SetBillBmVal(Thing,Bill) | Later Best Mix owner; optional canonical package/shape guards; no copied source adapter |
| Compositable `Wiri.compositableloadouts` | Inventory types, tags, W_PerTag and SetTagForBill; creates bills | Auto-generation not selected here; native provider flows remain outside our Add bill slice |
| Dubs `Dubwise.DubsMintMenus` | Patch_BillStack_DoListing.DoClones(Rect,HashSet<Building>), billtab/searchString | Clone embedding not selected; no import/export contract or compatibility inferred |
| Source settings | hideScope, sideBySideRecipes, showDubsMintMenusClonesTab, matchVanillaRecipeSorting, showQuickAddBillButton, showHideRecipeButton, outlineMissingIngredientIcons | No automatic import; only two adopted settings use owned common IDs/defaults |
| Source save keys | NiceBillTabExpansion_HiddenRecipes; HiddenRecipeWorkbenchTypePairs and HiddenRecipesEverywhere with same prefix | No automatic key migration; owned records follow observable scope and lifetime |

Source type-only integration checks are insufficient for ours. Installation of any provider does
not prove it is active or supported. A source BWM error path reports the toolbar handled despite
drawing nothing; our fallback must remain usable. No upstream package/assembly impersonation.
Co-installation follows semantic admission: compatible changes allowed; an active incompatible
replacement retains its own UI and our affected enhancement skips with a warning.

## Implementation and coverage

**Nothing is implemented.** NE-01–09 have proposed observable contracts and unchecked tasks.
NE-10/12/13 are explicit omissions; NE-11 belongs to later selected owners. Key deviations are
combined visibility scopes, consistent shortage default, recoverable hiding controls, chosen-material
quick-add, shared paused-fresh allocation and responsive access instead of source fixed-width behavior.
These differences can affect a player's expectations of source parity and must remain documented.

No runtime target IDs, validators or acceptance evidence exist yet. Future hooks need individual
minimal semantic target specs, startup admission, effective-code handling and isolated fallback;
no file hash or foreign-owner absence can stand in for that proof. No XML edit is selected. Any later
one requires guarded package/Def/shape checks, merge preservation and XML Extensions metadata.
No original settings or saves are mutated by this documentation work. Later migration would require
an explicit backup/atomicity/restoration/postcondition contract. Browser disabling retains owned
records while leaving native bills/list unrestricted; saving after uninstall does not promise record retention.

## Verification and acceptance

Document validation/review is tracked in the [progress ledger](../refinement/specification-progress.md).
NE-Q1–4 remain open for native targets/persistence, final composition, providers/provenance and
performance/visual/native acceptance. The source journal distinguishes exact evidence from inference.

Later verification must include nonzero focused RED/GREEN tests in honest environments, owner
build/package checks, independent review and a fresh minimized Gateway-controlled game on the reviewed
build. Exercise actual hide/recover/add/production, paused supply changes, save and bench lifetime,
global/save settings, main-menu guards and supported provider routes. Retain causal screenshots,
ordered mod list, build identity, configuration hashes and cleanup. Do not mark a scenario accepted
from source inspection, loaded patches, logs or synthetic state checks alone.

Semantic intent: organization and information do not change recipe costs, quantities, research,
skills or native production. Hidden entries remain legal plans; supply cues do not reserve stock.
No performance gain, complete source replacement or live compatibility is claimed by this draft.
