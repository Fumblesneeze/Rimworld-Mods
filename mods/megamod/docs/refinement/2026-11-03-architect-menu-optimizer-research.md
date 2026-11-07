# Architect Menu Optimizer research — S036

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, target **1.7 beta**. Inspected
2026-11-03, read-only. This is evidence and design input, not an implemented replacement or live
compatibility claim. The handwritten question mark remains provenance; the sequential specification
request includes this selected entry.

## Documentation and identity

Read the [author's Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3697920753)
before decompilation. It describes visibility, ordering, labels, icon colors, material lookup and
text-measurement caching; independent switches; and optional command/category pagination. It claims
compatibility with Scrollable Gizmos and Dubs Mint Menus. For Better Architect Menu it says
optimization/category pagination work but submenu pagination does not, because that provider owns
the submenu. These are author claims, not megamod acceptance evidence. Both attempts to read the
change-notes endpoint failed; no changelog conclusions are drawn.

- Title **Architect Menu Optimizer**, author **Marukun**, package `MRK.architectmenuoptimizer`,
  Workshop **3697920753**.
- Local root `F:/Steam/steamapps/workshop/content/294100/3697920753`. About advertises 1.6/1.7,
  requires/loads after Harmony. LoadFolders explicitly selects root plus the matching version folder.
- Selected `1.7/Assemblies/ArchitectMenuOptimizer.dll`: assembly/file/informational version
  **1.0.0.0 / 1.0.0.0 / 1.0.0**, Release, .NET 10; game, UnityStubs, Harmony and Waylith references.
- No C# source, license or source-repository declaration was supplied in the inspected package.
  Bounded public exact-title/source searches did not locate an author repository. Source publication
  and reuse license remain unknown, not proven nonexistent. Independent implementation is planned;
  do not copy source code, DLLs or art into the product.
- `Assets.zip` contains a page-switch DDS texture and bake/texture metadata, not source. English and
  Simplified Chinese keyed settings were inspected. No Def or XML patch files were found.

| Identity input | SHA-256 |
| --- | --- |
| About/About.xml | `42F1619D0D28BC7DC263E3D5F8BF21F74AA6756BB31F615AF88307EAAEDC8781` |
| LoadFolders.xml | `1B1B5599AAD26471655F13E5B2C019905EF1780798FB8E4F488720B54D7D6423` |
| Selected 1.7 DLL | `FE69A90623DD643C6424A9E5F296074B5EAFF1843407BF8313E93FB633041772` |
| Assets.zip | `DB59A35EB37C766E697BE016DA213E25D290D0FFE19F9C28DDE6CD69E1E9A2B8` |
| Native Assembly-CSharp.dll, 1.7.4995 rev309 | `56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21` |
| Native UnityStubs.dll | `07B33DC47D1C5136B50793AE11F566261ED844CB4E7D9DBE7A441D0954F5A90C` |
| Harmony selected 1.7 DLL | `C8EF7856B2BE3CF24B345C9B5C2F197F68FCF92649635064FCE7B3CCC8F77CB0` |
| Better Architect supplied 1.6 DLL | `E49D74051D5E0E04C83453B0CE031165304FFE8DE130BCF540290E75B4F26620` |

Hashes identify inspected evidence; they are not runtime admission rules. ILSpy 11.2.0.10050 output
is local under `artifacts/Refinement/megamod-spec-20261103/architect-menu-optimizer/`: `compiled`
resolves game bodies, `attribute-project` resolves Harmony attribute enums, `native` holds inspected
game types, and `better-architect` the consumed provider subset. All **39 runtime C# files** plus
assembly metadata were read: 15 main-namespace files and 24 patch-namespace files. This is a complete
source-binary inventory, not an execution test.

## Source settings and UI

All 19 fields use global ModSettings persistence under their own field names. No save component,
import or migration was found. The five integer fields clamp to at least one on load, without the
UI's upper bounds. Source labels and UI were read rather than inferred from field names.

| Fields | Source defaults | UI / timing |
| --- | --- | --- |
| cacheVisibility, cacheGizmoGrid, cacheLabels, cacheIconColor, cacheStuffDef, cacheTextMeasure | All true | Six independent ordinary switches |
| visibilityCacheMaxAgeTicks | 120 | Slider 30–600 game ticks |
| stuffDefCacheMaxAgeTicks | 204 | Slider 60–600 game ticks |
| enablePagination; itemsPerPage | false; 48 | Command list, 12–120 |
| enableCategoryPagination; categoriesPerPage | false; 20 | Categories, 6–40 |
| showExperimental | false | Hides/reveals four switches; turning it off also sets all four false |
| amortizedVisibility; visibilityBatchSize | false; 10 | Round-robin visits/frame, slider 5–50 |
| cacheGizmoGrouping; eventDrivenInvalidation; cacheGizmoWidth | All false | Experimental switches |
| debugMode | false | Statistics overlay; source does not require native developer mode |

`SettingsUI` is a fixed-height scroll form. Its configuration snapshot clears every cache on a
relevant edit; debugMode/showExperimental alone are not snapshot fields. A Clear all caches button
also resets pages and statistics and sends a positive message. `DebugOverlay` draws English hit/miss
ratios at the upper left, colors ratios above 90% green and above 50% yellow, and changes text font
without restoring the exact previous value. Hit rates do not prove frame-time improvement. No source
bottom tab is added. The owned contract moves diagnostics to developer tools and uses common
global/save preferences for the five normal settings; source tuning knobs are not blindly copied.

## Complete algorithm inventory

| Source files | Inspected behavior and limits |
| --- | --- |
| ArchitectMenuOptimizerMod | Loads settings, blanket PatchAll, then three manual adapters; unconditional loaded/applied log. Not an admissible owned startup process. |
| Settings; SettingsUI | Fields, persistence and editing above. |
| CacheManager | ClearAll clears all caches, grouping, page state and counters. DirtyAll invalidates visibility/label/icon/material generations and ordering. |
| VisibilityCache | Keyed by build-designator reference, map, tick and generations. Default 120-tick age; negative age invalid. Building prerequisite generation affects only commands with building prerequisites. God mode bypasses storage. Stale reads can ignore age in amortized mode. |
| StuffDefCache | Build-designator reference/map/tick/generation, 204-tick age; only default suggestions, not explicit StuffDefRaw. |
| LabelCache | Reference plus entity Def/label, writeStuff/MadeFromStuff, selected Stuff identity, precept/LabelCap, active language and generation. Does not prove all foreign label dependencies or in-place material-label changes. |
| IconColorCache | Reference plus placing Def, Stuff, map, primary ideology/color, minor-ideology count and generation; expires after 60 render frames. Count alone does not identify changed minor ideologies/colors. |
| TextMeasureCache | Two 512-entry LRU caches for height/size; text, width, font and wrapping keys. No independent UI-scale/backend generation. Source interception affects all Command rendering, not only Architect. |
| GizmoWidthCache; EngineFrame | Width keyed by Gizmo/maxWidth for one draw/frame; all Command instances bypass width caching, including custom subclasses. EngineFrame reads Waylith frame count. |
| ReferenceIdentityComparer | Reference equality/runtime identity hashing. |
| AmortizedVisibilityUpdater | Registered reference list/set, round-robin batch of ten visited entries per frame by default, not ten guaranteed recalculations. Stale age-expired values can remain until visited. Clear empties registration/index but does not reset lastUpdatedFrame. |
| DebugStats; DebugOverlay | Basic/grouping/width counters, stale reads and ratios, zero when no samples; display described above. |

The patch namespace has **19 Harmony-attributed classes**, three manual Apply adapters, one grouping
helper and one category-state holder. All are accounted for below.

| Patch file(s) | Target / behavior |
| --- | --- |
| Patch_Visible | Designator_Build.Visible getter; last-priority prefix can return cached result, first-priority postfix stores only if original ran. Thread-static refresh guard restores in finally. |
| Patch_Label; Patch_IconColor | Label/IconDrawColor getters, first-priority prefix/postfix; store only original results. |
| Patch_StuffDef | StuffDef getter, last prefix/first postfix; preserves explicit choice and original-run check. |
| Patch_TextMeasure | Replaces CalcHeight/CalcSize calls in Command.GizmoOnGUIInt; warns on zero anchors. Broad source scope exceeds this owned feature. |
| Patch_GizmoGrid | DrawGizmoGrid stable sorting, width calls and optional group restore/store. Sort reuse compares comparer identity, input references and Order values. Group anchors lie between gizmoGroups.Clear and firstGizmos.Clear. Missing grouping anchors can leave other mutations applied. |
| Patch_GizmoGrouping | Only native designator types or own page command with native grouping/merge methods qualify. Snapshots groupable, hotkey, label, icon identity/aliveness, group key and ignore-content key. Rejects any patches on GroupsWith/MergeWith or foreign DrawGizmoGrid transpilers. This blanket-owner rejection is deliberately not adopted. |
| Patch_BuildingPagination | Replaces category ResolvedAllowedDesignators access, evaluates Visible, stable-sorts, then slices raw visible designators before native grouping. Per-category reference state; Next page command order -9999, no cell designation, wraps with Tick_Tiny sound. Force-activated command reveals its raw page. General search does not route pages. |
| Patch_CategoryPagination; CategoryPaginationState | Slices all category panels, including disabled ones; redirects panel reads and adds 26-pixel page row. Requires panel anchors and exactly one row-height anchor or returns original stream. One shared page index, wrap and ArchitectCategorySelect sound. |
| Patch_WinHeight | Separate getter prefix returns ceil(configured categories/2)*32+26+28; uses configured count even on short last page. No atomic coupling to content transpiler success. |
| Patch_DebugOverlay | ExtraOnGUI checks god-mode changes, optional difficulty hash, provider polling and amortized refresh; postfix draws stats. |
| Patch_Invalidation_BuildingAdded / BuildingRemoved | ListerBuildings.Add/Remove invalidate building-prerequisite visibility. |
| Patch_Invalidation_GameLoad / MapInit / MapSwitch | Game.LoadGame, Map.FinalizeInit and MapInterface.Notify_SwitchedMap clear all and reset tracking. |
| Patch_Invalidation_ResearchFinished | ResearchManager.FinishProject invalidates visibility/labels. |
| Patch_Invalidation_ResourceTick | Current-map ResourceCounter.UpdateResourceCounts invalidates material/label/icon caches. |
| Patch_Invalidation_SetStuffDef | Selected build material invalidates that designator's label/icon. |
| Patch_Invalidation_HiddenItemDiscovered | HiddenItemsManager.SetDiscovered invalidates visibility only with experimental event invalidation enabled. |
| Patch_Invalidation_AnomalyLevel | Manual adapter, Anomaly active and callback exists: GameComponent_Anomaly.Notify_LevelChanged; invalidates visibility only when experimental option enabled. |
| Patch_Invalidation_GravEngineInspected | Manual type/method lookup Building_GravEngine.Inspect; same conditional invalidation, no package/DLC gate. |
| Patch_Invalidation_BetterArchitect | Manual reflection and edit-mode polling/hooks, detailed mismatch below. |

Source fallback differs per hook. Zero-anchor warnings and method presence do not establish complete
effective semantics, freshness or atomic layout. No source guard is accepted as our target spec.

## Native 1.7 reference

Inspected `MainTabWindow_Architect`, `ArchitectCategoryTab`, `DesignationCategoryDef`,
`Designator_Build`, `GizmoGridDrawer`, `Command` and `Designator_Dropdown` in the root game assembly.

- Native category order descends by category order, uses two columns and preferred-column handling,
  and displays disabled categories while rejecting their selection. Resolved commands include
  orders, terrain and ideology/style variants, and obey game designator rules.
- Native grid stable-sorts then groups/merges before rendering. Combined commands can select the
  first enabled representative. The owned page unit must preserve full groups/dropdowns; slicing
  first can fragment a source group. This is a correction, not a source-parity claim.
- Native search scans the entire resolved category content. CacheSearchState counts matching labels
  without requiring Visible; highlighting/dimming is not a filtered-only list. Search chooses an
  open category; Accept can set a unique force-activated command. Hidden commands do not become
  actionable merely because their label matches. Owned paging must reveal valid matches without
  redefining search or repeatedly overriding manual page navigation.
- Visible considers god mode, faction technology, research, Anomaly level, difficulty restrictions,
  PlaceWorkers, required colonist buildings, discovery and inspected grav engine. Stateful external
  rules cannot safely be inferred from a short timer or a no-other-patches check.
- StuffDef honors an explicit raw choice, otherwise native default Stuff, then native resource-count
  fallback when the default is insufficient. It does not select the best-quality material. Labels
  include selected Stuff/precept context; icon color includes ideology/native/material decisions.
- Native reopen resets search/focus; tool extra controls and building details belong to the normal
  category workflow. Page movement must not create a blueprint, lose material/style/rotation or
  duplicate a unique-search activation.

These are inspected bodies, not proof of the current effective Harmony chain or a real UI run.

## Providers, consumers and contracts

| Evidence | Finding / boundary |
| --- | --- |
| [Better Architect Menu](https://steamcommunity.com/sharedfiles/filedetails/?id=3563882422), installed `ferny.BetterArchitect` | Public required-items list includes Architect Menu Optimizer; installed About requires Harmony and Architect Icons. This relationship does not grant megamod that package identity. |
| Supplied Better Architect source/DLL | About advertises 1.6/1.7, but inspected LoadFolders declares only 1.6 and supplied DLL is under 1.6. This is a reference, not an accepted beta-selected provider. |
| BetterArchitectMod, BetterArchitectSettings, EditModeRuntime, ArchitectCategoryTab_DesignationTabOnGUI_Patch | The supplied mod has static **field `settings`**, not static **property `Settings`** expected by the optimizer. Its editMode is static. Optimizer initialization therefore leaves the inspected adapter unavailable before registering hooks/polling. InvalidateAllCaches is public and RebuildDefaultCaches private; provider submenu prefix owns drawing and returns false. |
| [Dubs Mint Menus](https://steamcommunity.com/sharedfiles/filedetails/?id=1446523594), `Dubwise.DubsMintMenus` | Installed About advertises through 1.7, modVersion 1.3.1704; public build-menu search/grid/list/radial behavior read. Its effective binary interaction remains unverified. |
| Scrollable Gizmos claim | No exact installed About-title candidate found. Workshop 3828060392 is Orders Bar (`toshtaru.OrdersBar`), not this provider; it is not substituted as identity evidence. |
| Installed C#/XML/JSON reference search | Bounded searches for optimizer package/type names found own metadata, no demonstrated external callable consumer. This cannot exclude binary/reflection consumers or the public required-item relationship above. |

No game save component or Def identity is supplied by this source. Settings serialization and public
helper types are not automatically megamod contracts. Preserve demonstrated consumers where feasible,
but do not claim a package/assembly drop-in, spoof IDs, import source settings or remove foreign
patches. With the original optimizer active, the owned overlapping enhancements remain inactive as
a group with a bounded startup warning. Provider-owned submenus remain theirs; independent admitted
optimizations/category paging can survive an unavailable submenu adapter.

## Owned decisions, evidence gaps and handoff

The [observable specification](../../../../openspec/changes/specify-megamod-architect-menu/specs/megamod-architect-menu/spec.md)
and [design](../../../../openspec/changes/specify-megamod-architect-menu/design.md) define independent
implementation: next-redraw freshness or native evaluation, complete grouped page units, full-scope
search, atomic layout fallback, viewport fitting, five common preferences, top-only ephemeral
diagnostics, direct shared-material/formatting composition and semantic admission.

Source ages, batching and broad global interception are recorded algorithms, not adopted player
promises. No startup warning may be hidden by the owned logging feature. No native content costs,
quantities, research, construction eligibility, quality or job rules change. Performance still needs
matched workload measurements including invalidation/update/memory cost, not a cache-hit percentage.

- **AM-Q1:** effective native hooks, semantic prerequisites, freshness and repatch/rollback coverage.
- **AM-Q2:** exact provider/consumer/licensing evidence, including current Better Architect binding,
  Dubs and absent Scrollable Gizmos; any future compatibility facade needs a real consumer.
- **AM-Q3:** shared-data/layout/formatting integration, seven-language viewport coverage, bounded
  memory/update cost and measured responsiveness for independently admitted optimizations.
- **AM-Q4:** future TDD, review, package checks and causal native menu/settings/provider acceptance.

No runtime code, test implementation, installation or game launch was performed for this draft.
