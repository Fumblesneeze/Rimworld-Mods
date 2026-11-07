# Nice Bill Tab - Expansion — sequential source research

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S011 / interview:P1-09. Source research supports the proposed
[recipe-list controls](../../../../openspec/changes/specify-megamod-recipe-list-controls/) and
[coverage report](../replacements/nice-bill-tab-expansion.md). No implementation or live acceptance.
This source follows the completed S010 [inspection draft](../../../../openspec/changes/specify-megamod-recipe-inspection/).
S012 will compose the final original Add bill browser. Record questions here instead of asking the
user to explain source behavior or interrupting for implementation choices.

## Confirmed scope

Nice Bill Tab and its Expansion are **UI references** for richer Add bill, not blanket requests for
every upstream feature. Preserve the user's search/category pills, relevant stat menu, Normal quality,
available-material default, explicit alternate material and ingredient-plus-all-skills highlight.
S009 owns those comparison/creation semantics; S010 owns read-only details and comparison. Additional
Expansion behavior must be explicitly selected, adapted or omitted, with no self-patches between owned
features. Actual hidden-recipe choices are gameplay/entity state, not global mod configuration.

## Primary documentation first

Read the [Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3721023311),
[change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3721023311) and
[older notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3721023311?p=2) on 2026-11-02.
The English-parameter description initially failed; the plain description succeeded. Cached variants
reported different update counts: the description shows October 28/14 notes, the first changelog
response stopped at October 27/13 notes. A parameterized refresh failed. Do not infer installed binary
identity from those cache discrepancies; its hash is recorded below.

Author-described functions: hiding recipes at bench/type/everywhere scope, hidden-only recovery view,
quick-add buttons, missing-ingredient slashes, optional separate bill/recipe columns and BWM,
Compositable Loadouts, Best Mix and Dubs Mint Menus integrations. The author says compact ingredient
previews and some focus/vertical-fit fixes moved upstream. Quick-add is documented off by default;
missing slashes on. Vanilla-sort behavior is still documented problematic. These are source claims,
not verified behavior or automatically selected megamod features.

Change notes record category/hiding fixes in May, additional provider buttons and clone integration
in June, initialization moved into loading in July, and 1.7 in October. The latest description has an
author October 28 comment saying the reported Compositable button issue was fixed; no live result was
checked. Ignore generic Steam removed/incompatible banners as compatibility evidence.

No source repository/license link was established from the inspected description/About and targeted
search. Public third-party mirrors and HugsLib logs were search hits only, not technical evidence.

## Installed identity

- **Nice Bill Tab - Expansion**, author **HICON**, `HICON.NiceBillTabExpansion`, Workshop **3721023311**.
- Read-only `F:/Steam/steamapps/workshop/content/294100/3721023311`.
  About supports **1.6/1.7**, hard-depends on **Harmony** and **Andromeda.NiceBillTab**, and hints after
  `Wiri.compositableloadouts`. LoadFolders selects `/` plus the corresponding 1.6/1.7 directory.
- `1.7/Assemblies/NiceBillTabExpansion.dll`, assembly/file **1.0.0.0**, informational **1.0.0**,
  SHA256 **E4FFD4EE3B9C79DA1E2767D5751BB7175032957D4E287AC0652E37C1D550F212**.
- Installed file search found About, LoadFolders and English keyed strings, but no `.cs`, `.csproj`,
  Markdown or license. No code/art/assembly reuse permission is established. Do not copy these inputs.
- Existing read-only decompilation from S010 consumer inspection:
  `artifacts/Refinement/megamod-spec-20261102/nice-bill-tab/consumer-expansion` (ignored).
  Keep using that exact evidence rather than duplicating it. Paths below are relative to this root.

## Settings and persistence inspected

Read `NiceBillTabExpansionMod`, `NiceBillTabExpansionSettings`, `HideScope`,
`NiceBillTabExpansionGameComponent`, `Storage/HiddenRecipeStore`, and `Patches/WorkTableExposeDataPatch`.
Settings are global source ModSettings, not inherited save overrides.

| Source setting | New-object value | Scribe key / missing-field default |
| --- | --- | --- |
| HideScope | Everywhere | hideScope / Everywhere |
| SideBySideRecipes | true | sideBySideRecipes / true |
| ShowDubsMintMenusClonesTab | true | showDubsMintMenusClonesTab / true |
| MatchVanillaRecipeSorting | true | matchVanillaRecipeSorting / true |
| ShowQuickAddBillButton | false | showQuickAddBillButton / false |
| ShowHideRecipeButton | true | showHideRecipeButton / true |
| ShowMissingIngredientSlashes | **false** (no initializer) | **outlineMissingIngredientIcons / true** |

The last setting has a confirmed new-object versus deserialized-missing-field discrepancy. Read
Verse.Mod.GetSettings and LoadedModManager.ReadModSettings in the exact inspected 1.7 game:
no file/failure/no ModSettings node returns a new object; a loaded record uses Scribe. Evidence is
under the earlier `chill-the-fork-out/game` decompilation. Megamod explicitly proposes consistent
**on** for fresh and missing values; the author's default claim alone did not prove this source behavior.

Settings UI lists those six checkboxes then scope radio choices. Sorting changes invalidate recipe
availability; scope changes refresh filtering. Hiding state uses recipe **defName**, not style or
material identity, so a hide applies to all style/material variants of that recipe within its scope.

- Bench-specific records are ConditionalWeakTable state on the Building_WorkTable, serialized with
  **NiceBillTabExpansion_HiddenRecipes** through an ExposeData postfix.
- Type records are a game-component dictionary keyed by bench defName, stored as `bench|recipe`
  strings under **NiceBillTabExpansion_HiddenRecipeWorkbenchTypePairs**.
- Everywhere records are a game-component set under **NiceBillTabExpansion_HiddenRecipesEverywhere**.
  Everywhere means this game, across maps, not every save. Nonempty unknown Def names remain strings;
  malformed type pairs are ignored. Specific bench state lifecycle on minification/rebuilding needs
  native research before a migration or retention promise.
- Crucially, CurrentScope **selects one independent collection for both reading and writing**.
  The three lists do not union and scope changes do not migrate entries. A recipe hidden in one scope
  can reappear in another while the former record remains dormant. This is source behavior to
  classify deliberately; it is not yet the approved megamod hiding policy.

## Hiding, row actions and filter paths

Read `RecipeFilteringPatch`, `HiddenRecipeViewMode`, `RecipeRowHideButton`,
`RecipeVisibilityToggleButton`, `RecipeRowQuickAddButton`; source patch installer was read in S010.

- Normal mode excludes hidden recipes; hidden mode shows **only** hidden recipes. A contextual
  visibility button appears when AnyHidden is true; it does not depend on ShowHideRecipeButton.
  Disabling the row hide control therefore need not undo hiding or offer an unhide action.
- Hide/unhide and visibility toggles clear the selected bill/recipe through source SelectBill(null).
  Removing the last hidden record returns to ordinary mode. The view-mode cache keys the active
  bench's thingIDNumber, not an explicit save identity. A bounded caller search found no reset beyond
  ensuring the bench, handling hidden changes and toggling mode; save-switch safety is not established.
  Hidden flags do not delete/suspend existing bills.
- AnyHidden differs by scope: bench/type check stored count, while everywhere intersects the bench's
  AllRecipes. Missing/inapplicable recipe records can therefore affect whether the toggle appears.
- Filtering caches by bench, input-list reference/count and HiddenRecipeStore.Version. Scope/hiding
  changes increment that version and refresh the source tree/list. InvalidateAvailableRecipes callers
  found by the bounded search include the settings sort change; this does not establish automatic
  handling of every in-place recipe/provider mutation.
- Quick-add defaults off and only appears when the row is wide enough. It preserves the source
  selections and locked selection, calls SelectRecipe(recipe, style, material:null, bill:null), invokes
  source TryAddBillToQueue, then restores selections. Restoration is not in a finally block.
  **It does not use the material currently being compared.** Our composed quick action must use S009's
  visible selected-material contract, and one click must not trigger the row's normal double-click.
  Full compact/ordinary row prefixes prepare and handle quick-add, reserve its left space, then
  prepare/handle hide. Postfixes draw controls; compact also reserves and clears ingredient space.
  No finalizer restores that space if the original throws. Native Widgets.ButtonInvisible is a
  GUI.Button wrapper; source reading alone does not prove actual click consumption/isolation.
  The compact transpiler adjusts a Rect.xMax access after SimpleIngredientCache without an explicit
  unmatched-target warning. None of these source hook shapes are megamod target admission rules.

## Missing-ingredient indicators and costs

Read `MissingIngredientIconSlashes` matching/cache/action sections,
`RecipePreviewMissingIngredientSlashes` and `DrawIngredientsMissingSlashesPatch`.

- Compact/category previews draw red slashes over ingredient icons; detail panes draw over cached
  alternative-icon batches. Detail mapping asks whether a missing requirement allows **any** batch
  Def, so overlapping requirements can mark more than the exact corresponding role. No textual
  pending/unknown state is established by these helpers; false includes disabled, unavailable data,
  deferred calculation and no missing ingredients.
- The main path gathers candidate Things, then calls reflected
  WorkGiver_DoBill.TryFindBestBillIngredientsInSet(List<Thing>, Bill, List<ThingCount>, IntVec3, bool,
  List<IngredientCount>). The return value is ignored, and the missing-output list is used. On absence
  or exception it falls back to independent per-requirement counts, which can reuse the same stack
  for multiple roles and does not establish the full native mixing/custom-worker semantics.
- Stock snapshots have **90 game ticks** of lifetime; missing results **240 ticks**, max result cache
  **16384**, compact calculations at most **6 per frame**. Cache keys include recipe/material/scope/
  bill identity, not every mutable bill-filter or stock revision. Game-tick-only expiry can remain
  stale while paused. Shared S009 data and explicit updating/unknown states must replace separate
  port-local inventory caches and optimistic fallbacks.
- Snapshots use map.listerThings.GetAllThings with include flag true, filter destroyed/nonpositive,
  wrong MapHeld, invalid position and faction-forbidden things; the local predicate has no fog check.
  Native 1.7 ListerThings.GetAllThings(lookInHaulSources:true) enumerates AllThings and registered
  IHaulSource.GetDirectlyHeldThings with the same predicate. It does not imply arbitrary nested
  containers or personal inventories, and does not add a fog check. Autonomous bench innerContainer
  is separately deduplicated. New native read-only evidence is under
  `artifacts/Refinement/megamod-spec-20261102/nice-bill-tab-expansion/game` (ListerThings and Widgets).
- Search radius and Bill.IsFixedOrAllowedIngredient constrain ordinary snapshot candidates.
  Cache scope is the map for recipe previews on maps within a squared diagonal of 998001 (999²),
  otherwise the bench; existing bills/autonomous benches always use the bench. That assumes default
  radius behavior; a provider's different defaults require evidence.
- Temporary bills are cached by recipe/material, not style. It creates Bill_Production directly,
  or on exception uses GetUninitializedObject and fills a few fields including radius **999**.
  Chosen material clears the editable filter. Neither constructing an invalid partial bill nor
  losing independent ingredient roles is an acceptable replacement for supported native semantics.

## Layout, ordering and preloading

Read `SideBySideBillsTabDrawer` main flow, filtering/cache and sizing sections; FillTab/constructor
patches; `RecipeListDisplayOrder`; full category capture/refresh paths and BillTabPreloader paths.

- Side-by-side uses independent bill and recipe search/scroll columns, with optional source detail
  panes and Dubs Bills/Clones sub-tabs. Bill search uses Bill.LabelCap; recipe search uses holder label,
  then source special category filtering. Search text is synchronized into source static fields.
  Switching bench clears both searches and source selections; other availability refresh remains
  dependent on invalidation; the bounded caller audit does not prove universal live/provider freshness.
- Width uses fixed recipe/detail columns and shrinks the bill column no smaller than **260**.
  This is an attempted fit improvement, not proof the entire UI fits arbitrary small/scaled screens.
  Side-by-side exceptions log once and return to original Nice Bill Tab rendering.
- Vanilla-sort mode skips the source's additional sorting; disabling it recreates category/product
  ordering. It does not itself prove all grouped modes have native order. CategoryTreeOrderStabilizer
  captures holder object references, first-product deepest-category path indexes and original recipe
  indexes, for category mode 2. Unknown holders sort with an empty path/max index. Category RefreshTree
  clears capture and requests recapture; hiding calls that refresh too. It is not a persistent global
  order guarantee. Parent-category traversal is not cycle-bounded in this helper.
- New/load game schedules Precompute via LongEventHandler.ExecuteWhenFinished. It finds colonist
  worktables on all maps, gathers recipes for all worktable Defs, warms optional integrations and
  source category/icon caches, and optionally evaluates shortages per bench/recipe. Exceptions clear
  preload data and log once, falling back to first-open initialization. No measured performance claim.
- A preloaded recipe snapshot is used once after comparing current native available base recipes.
  Prototype/style/provider changes beyond that base set are not proven covered by the comparison.
  These caches are source implementation facts; megamod must share metadata and candidate data with
  its other consumers rather than transplant each cache separately.

## Optional adapters and incoming consumers

Read full BestMixBillButton and CompositableLoadoutsAutoBillButton; relevant Dubs clone actions/bindings
and BWM bill buttons. These adapters mostly resolve types by name, not canonical package IDs.

- **Best Mix:** `BestMix.BMBillUtility.GetBillBmTex(Thing,Bill)` and `SetBillBmVal(Thing,Bill)`,
  non-surgery production bill only; click delegates actual policy change. Failures disable the source
  button for the session. Later selected Best Mix owns its behavior; do not recreate a patch to ours.
- **Compositable Loadouts:** `Inventory.LoadoutComponent/Loadout/Tag/Item/Textures/InvBillRepeatModeDefOf/
  LoadoutManager`. Reads AllTags/requiredItems, RepresentativeDef/Members/IsProsthetic/Quantity,
  Print/SubsetOf/CopyTo, SetTagForBill, BillsGizmoTex and W_PerTag. Only single-member items get bills.
  It collects current-map free-colonist tags, takes the first available recipe by output, suppresses
  already-covered per-tag bills, creates/configures native production bills and adds them. Target is
  1 except prosthetics use quantity; repeatCount 0, includeEquipped true. Availability tests AvailableNow
  but not AvailableOnNow here. Errors can leave a partial set of new bills; no transaction is visible.
  This is more than exposing a native button and is not implicitly selected as new megamod behavior.
- **Dubs Mint Menus:** resolves `DubsMintMenus.Patch_BillStack_DoListing.DoClones(Rect,HashSet<Building>)`
  and static billtab/searchString. Shows Bills/Clones within the contextual recipe column, sets selected
  tables to the one bench and delegates the clone UI. Finally clears the temporary table set and
  restores GUI state. Export/import semantics belong to the provider and are not verified by this call.
- **BWM:** delegates through original NBT BetterWorkbenchesIntegration for individual bill settings
  copy/paste and workbench copy/paste/link; improves Copy all bills tooltip. Best Mix buttons share
  this row-postfix path. Workbench buttons draw link (when paste is possible), copy all and paste,
  with 20-pixel offsets. If the adapter is active but disabled after an error, it still returns true,
  suppressing the caller's fallback toolbar. Megamod must retain an actually working ordinary route.
- Full topbar/layout/settings-cog inspection: topbar rewrite relies on Stloc_0, a following selection
  count branch and 110f literals. Reserve is 166 plus 64 for BWM and 24 for Compositable. The cog opens
  **NiceBillTabMod.instance settings**, not Expansion settings. These are observed implementation
  details, not adopted predicates/layout constants or an acceptable megamod configuration destination.
- Installed About files confirm canonical identities: BWM **falconne.BWM** / **935982361**; Best Mix
  **Mlie.BestMix** / **2195986094**; Dubs Mint Menus **Dubwise.DubsMintMenus** / **1446523594**;
  Compositable Loadouts **Wiri.compositableloadouts** / **2679126859**. This establishes identities,
  not activation, version support or live integration. Full provider deep dives remain sequential.
- Bounded incoming scan `rg -l -a -F NiceBillTabExpansion` across installed Workshop/local Mods
  `.dll/.cs/.xml/.json/.md` found **only this mod's metadata/strings/1.6+1.7 assemblies**. No external
  incoming contract identified by that scan; computed bindings are not ruled out.

## Draft selection and unresolved work

The installed 1.7 DLL was rehashed on 2026-11-02 and still matches the identity above. Native evidence
uses **1.7.4995 rev309**, Assembly-CSharp SHA256
**56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21**.
These identities connect the inspection to its inputs, not a claim that the source runs correctly.

Selected proposals are scoped hiding/recovery, optional exact-material row Add, shared per-requirement
shortage cues, stable relative ordering and responsive access. Hiding combines applicable records
instead of choosing one global scope; the scope is contextual gameplay state. Recovery removes
explicit records and always remains reachable. Quick-add remains off initially, shortage cues are
uniformly on. The original Add bill owner will compose these details with S009/S010.

Source whole-queue side-by-side replacement, separate queue search, clone import/export and
Compositable auto-generated tag bills are outside this confirmed UI-reference slice. BWM/Best Mix
selected functionality belongs to their later owners. No upstream cache, preload, static selection,
source setting import or binary facade is copied. The report inventories each disposition.

- **NE-Q1:** exact native admission/persistence/click paths, including same-Thing minification and
  map transfer; no runtime compatibility inferred from source method names.
- **NE-Q2:** final S012 layout and gestures, proposed combined hiding-scope policy/recovery wording.
- **NE-Q3:** actual provider workflows, further incoming contracts and unresolved reuse provenance.
- **NE-Q4:** paused freshness, combined data costs, supported resolutions/scales, all seven languages
  and reviewed-build native acceptance. Source analysis does not satisfy these later tasks.

Independent document review and strict validation are tracked in the
[progress ledger](specification-progress.md). All implementation and live tasks remain unchecked.
