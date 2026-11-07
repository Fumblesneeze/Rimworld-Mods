# Nice Bill Tab — sequential research and draft contract

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S010 / interview:P1-09. Research evidence, not implementation or live observation. The resulting
[draft change](../../../../openspec/changes/specify-megamod-recipe-inspection/) owns the selected
inspection slice. S011 Expansion and S012 the composed original Add bill browser follow separately.
Open questions stay here instead of interrupting the user.

## Confirmed scope and composition

The [interview](2026-11-01-handwritten-notes.md) names Nice Bill Tab and its Expansion as UI references
for richer Add bill, not blanket requests for every upstream function. The completed
[Quartermaster comparison contract](../../../../openspec/changes/specify-megamod-quartermaster-comparison/)
already owns Normal-quality stat/material comparison, one-execution ingredient allocation, one
colonist meeting **all** skill requirements and native material-specific bill creation. S010 must
identify its additional useful inspection/navigation behavior and its omissions without creating a
second incompatible comparison or availability rule. S012 will compose the final search/category
pills/stat menu and alternate-material gesture. No separate bottom tab or independent cache per port.

## Public documentation first

Read the [author's Workshop description](https://steamcommunity.com/sharedfiles/filedetails/?id=3520130671&l=english)
and [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3520130671) on
2026-11-02. The first plain page request returned HTTP 429; the English page then loaded normally.
Generic Steam removed/incompatible banners are not compatibility evidence.

The author describes a workbench task-creation replacement with name/category filtering,
alternative/material comparison, ingredient subtask creation and configurable list layouts. Shift
selects comparisons; active tasks animate. The description claims BWM cooperation and CE-specific
ammunition calculations but warns about inaccurate CE range estimates, conditional item mechanics,
multiplayer desync and language changes. The author's October 23 comment leaves 1.7 CE integration
unconfirmed. These are documentation claims to inspect, not our compatibility conclusions.

Change notes explicitly mark the October 22 update for 1.7. June notes mention ingredient previews
(default on), filter-focus fixes and unknown-price display; August notes mention selected-task
material refresh, mechanoid weapons and experimental options. Screenshot links are present but their
small thumbnail responses did not establish additional behavior. No source-host link was found in
the inspected description/About; targeted search did not identify an author repository.

## Installed identity and evidence

- **Nice Bill Tab**, `Andromeda.NiceBillTab`, author **Andromeda**, Workshop **3520130671**.
- Read-only `F:/Steam/steamapps/workshop/content/294100/3520130671`.
  About supports **1.5/1.6/1.7** and requires **Harmony** (`brrainz.harmony`). Load-after hints name
  CE/core-weapons-armor-melee packages, `kathanon.floatsubmenu`, `kathanon.searchablemenus`; hints are
  not additional hard requirements. No LoadFolders.xml found; version folders and shared languages
  are present. Source metadata declares assembly/file version **1.0.0.0**.
- `1.7/Assemblies/NiceBillTab.dll`, SHA256
  **DD6E002AC3936D7513DB576D01AFF8B8D37F15CACBC66005F1AE14E81FDD85DB**.
- Bundled `1.7/Assemblies/FloatSubMenu.dll`, SHA256
  **30BFDDAF20DEE3BAFB67B4AB8BA0BE5CCFA2C38A9C077689CE4B1AAC2744CB55**.
  Its API/license/package interaction is still to inspect; do not copy it into megamod.
- No shipped `.cs`, `.csproj`, README or license was found. A read-only listing of `Assets.zip`
  found texture assets and no matching source/readme/license entries. Assembly metadata has a 2025
  copyright notice, not a reuse license. Provenance remains unresolved for any code/art reuse.
- Read-only ILSpy project output: ignored
  `artifacts/Refinement/megamod-spec-20261102/nice-bill-tab/decompiled`.
  Installed English/Russian keyed strings provide option/status names, not proof of behavior.

## Files and behavior inspected so far

Paths here are relative to decompiled `NiceBillTab/`. The initial pass below is retained with its
then-open questions; the continuation records what was subsequently resolved. Bounded reads covered
the relevant action/state/stat paths; no claim of exhaustive pixel-drawing or every provider audit.

- `NiceBillTabMod`: Harmony owner `Andromeda.NiceBillTab`, unguarded `PatchAll`, then optional
  integrations. Megamod must use documented semantic admission, not reproduce this startup policy.
- `ITab_Bills_FillTab_Patch`: replaces the worktable Bills inspector when enabled; left queue/recipe
  list, expanding right comparison panes. It adds a source-enabled checkbox, settings shortcut and
  developer logging control inside that tab. No source MainButtonDef was found. The final selected
  interaction must follow the user's Add bill scope and UI decision tree; any retained diagnostics
  belong in the top debug bar, not the bill view.
- Constructor patch mutates `ITab_Bills.WinSize` and reflected instance size; failure to write the
  static readonly field produces a warning. Width derives from 330 + optional details button + extra
  width; each right pane is 484, height capped below the inspect pane. These mechanics are research
  inputs, not an approved fixed layout or a compatibility predicate.
- Close-tab/close-inspector patches resize/reset selection; closing a BillConfig dialog refreshes
  every selected recipe cache. `ModIntegration` new/load-game callbacks reset a locked selection
  and run stat scans and optional integrations. Full lifetime/invalidation analysis remains open.
- Optional integrations identified by package/assembly: CE, BWM (`falconne.bwm` / ImprovedWorkbenches),
  Research Reinvented (`petetimessix.researchreinvented` / ResearchReinvented), VEF
  (`oskarpotocki.vanillafactionsexpanded.core` / VEF). Their actual contracts and error isolation remain
  to inspect. BWM is a later selected source, so the eventual owned composition must be direct.
- `Utils.GetAvailableRecipes` mirrors native selected-table recipe availability and ideology-style
  variants, then calls Research Reinvented prototype integration. Do not assume that prototype
  behavior exists in ordinary Core or accidentally remove a supported external action.
- `TabBillsDrawer.TryAddBillToQueue(RecipeSelection)` checks the selected table still spawned,
  warns about absent mechanitor/skills but proceeds, uses native factory/style and notifications,
  applies material, optionally renames, then selects the bill or jumps to another workbench.
  `SetMaterialToBill` clears the editable ingredient filter and allows only the selected material
  for a single Stuff-made product. Fixed-role bypass is native; non-fixed ingredient roles still
  need the stricter material-lock proof already tracked under QM-Q1.
- `Utils.QueryCraft` finds an ingredient recipe and nearby workbench, jumps/selects and queues a
  subtask with repetitions rounded up by first product count. Its `TryAddBillToQueue` overload
  **returns without adding** after a missing-worker/mechanitor warning, unlike the main selection
  action. This is a source difference to classify, not a new requested global planning rule.
- `Utils.InsertBillBizarre` temporarily removes the queue tail, runs an action, then reinserts it.
  This lacks visible guaranteed restoration around the callback; do not port this mutation pattern.
- `RecipeComparator` compares exactly two selected stat caches and pairs common stat records;
  failure optionally shows a rejection. Selection limits, shift interaction and actual stat units
  still require their owning UI/cache source reads.

### Settings and defaults

`Settings` is global static state with a ModSettings serializer; it is not the shared save-inheritance
model. The following are inspected defaults, not adopted megamod settings:

| Choice | Initial value / persistence detail |
| --- | --- |
| EnabledMod | true; changed by source tab/settings, but **not serialized** in inspected ExposeData |
| RecipeListMode | initializer 1 (Compact); serializer missing-field default 0 (Wide); Categorized = 2 |
| PreviewRecipeIngredients | true; control shown for Compact/Categorized |
| CompareBarsWithColors, CompareWhenSelectMaterial, GlowingYellowHintEnabled | true |
| UseSpecialSymbolToFilterByCategory, UseNonLinearBars, ShowBarTicks | true |
| ShowUsableWithShieldsInfo | true; settings control shown only with VEF |
| EnableScrollUpWhenBillAdded, ReplaceInfinityTextToSymbol, ExtraWideQueue/Details button | true |
| EnableClassicSuspendButton, ShowStoppingPower, EnableAutoNaming, ColorlessMode | false |
| ShowFlatDamageForRanged / ShowFlatDamageForMelee | false / true; ShowDPS and ShowFlatDamageForMech fixed true |
| TabHeight | 480, UI range 400–900 with integer rounding and screen clamp |
| ListingExtraWidth | 0, UI range 0–200 with integer rounding |

Settings tabs are Common, Items and Experimental; the experimental tab has sample stat bars.
The missing-field versus newly-created defaults need deliberate normalization if adopted. Settings
must go through common global/save load/store/render, with main-menu save editing disabled. Contextual
recipe/material/queue decisions remain entity or browsing state.

### Availability findings — distinguish called paths from unused helpers

- `TabBillsDrawer.CheckRequirementsColonists`, called while drawing required skills, tests whether
  **any** `PlayerPawnsForStoryteller` pawn satisfies **any one** of the recipe requirements; it caches
  that bool on the selection. This differs from the user's one-colonist/all-requirements contract.
  The main add action uses native `PawnSatisfiesSkillRequirements`, so display and add warning can
  disagree for multi-skill recipes. Preserve the stricter drafted rule rather than this discrepancy.
- `Utils.AvailableOnMap` uses the first free colonist and native item availability; with no free
  colonist it returns true. Caller/context and quantity meaning still need tracing.
- A separate `BillValidator` class skips ingredient checks for the first pawn, accepts any one skill,
  and its ingredient predicate does not reject a positive-but-insufficient quantity. **No caller was
  found anywhere else in this assembly's decompiled source.** Do not report these unused helper
  branches as observed UI bugs. External callers remain part of the incoming-consumer audit.
- Price helpers and UI need further analysis: `Utils.GetPriceInternal` sums per-product market values
  without multiplying by each product count; the displayed materials/work row adds `workAmount *
  0.0036` only when workAmount > 2. Older third-party assertions are not used as technical evidence.
  Determine actual displayed units and contexts before specifying a correction or excluding it.

## Continuation: selection, filters and queue

Read `RecipeSelection`, `FilterMappingHelper`, `CategoryUtils`, `CategoryTreeBuilder` and
`CategoryTreeNode`; bounded `TabBillsDrawer` action sections from DrawLeftPart through selection,
ingredients and stats. These are source findings, not outcomes observed in a running game.

- A single search string filters both existing bills and recipes. Plain text lowercases recipe
  labels; special prefixes are **@ category**, **% apparel layer**, **# source mod**. Official
  sources also match `vanilla`. Category aliases cover the current category and immediate parent;
  the passed special-filter string is not lowercased by FilterItemByCategory. Categories use
  ProducedThingDef; tree grouping uses the first product and deepest category. Null products go to
  the tree root. This is not the user's category-pill contract and does not define multi-output search.
- Tree expansion is a static category-Def dictionary, initially expanded, not serialized in the
  inspected path. Recipes are refreshed on bench change; the list sorts by first-product category's
  parent label and then product label. DrawRecipeModeSettings and Utils.AvailableOnMap have no
  internal callers found; do not describe those helpers as active UI behavior.
- Source modes are Wide, Compact and Categorized. Single click selects; a second click within the
  source threshold adds a bill. Shift adds a comparison; SelectRecipe keeps at most **two** selections,
  with an optional locked selection reinserted after ordinary selection clears. ClearCache resets
  filter/scroll/selections but does not clear LockedSelection; new/load-game initialization does.
  Our proposed two-choice comparison resets at context boundaries rather than pinning across benches.
- Recipe details show a picture/style, product or recipe description, recipe info card, ingredients,
  work and stats. Long descriptions expand on hover. Ingredient pictures cycle through alternatives;
  a picture is not proof that the recipe requires that one item exclusively.
- Queue rows expose delete, suspend (icon by default; classic button optional), details, repeat mode,
  count +/- and clipboard actions. Plus/minus from Forever switches to RepeatCount 1; counts use
  native adjustment multipliers. Target-count edits also alter unpauseWhenYouHave. Special repeat
  names `TD_PersonCount` and `TD_XPerPerson` receive source-specific adjustments without a package
  check here. These intersect later selected Everybody Gets One/BWM work, not this inspection slice.
- Queue drag uses an index in **filteredBills** to insert into the full BillStack, a concrete hidden-row
  ordering hazard in HandleBillDrop. Native top Paste lacks the recipe-admission checks present in
  source row/empty-area paste; empty filtered-list paste can insert at actual queue index zero.
  Utils.InsertBillBizarre temporarily removes/reinserts the tail without visible guaranteed cleanup.
  These patterns are not imported into our ordinary queue controls.
- Processed state checks a current bill of a spawned, living, non-downed free colonist on the current
  map, with a 30-hash-tick refresh. Autonomous bills inspect their own state; ShouldDoNow false maps
  to Done. Enum names Paused/NoOneCanDo are not proof that GetBillStatus diagnoses those states.
  No claim that its animation covers every worker/mech or explains all blocked work.

## Continuation: material, ingredients and facts

- `RecipeSelection.DoCache` keeps fixed ingredients separate. With no chosen material it lists
  acceptable alternatives constrained by recipe/bill filters. With a chosen material, every
  **non-fixed** ingredient slot keeps only that material; independent non-fixed roles can disappear.
  The proposed contract instead preserves all roles and shares S009's actual finite allocation.
- `SelectBetterMaterial` on an **existing bill** calls SetMaterialToBill, then recaches; on a recipe
  it previews and optionally creates a comparison with the old material. Thus the source's material
  menu is not universally read-only. Our Add bill preview never receives an editable existing bill.
- OpenIdeaMenu lists materials with raw map-lister stack counts and the first non-fixed requirement's
  required count. Maximize searches all or sufficient-on-map materials using the current stat value.
  No general all-role allocation, pawn reachability or hidden/forbidden filtering is established by
  those sums. QueryMaximize returns only a strictly better value, not a stable general ranking.
- Ingredient menu opens the info card, finds/jumps to an instance, or creates a bill at another
  workbench via QueryCraft. The latter uses the first available recipe and closest matching bench,
  with rounded-up repetitions. We retain explicit information/navigation, not implicit production.
- DrawIngredients shows WorkAmountTotal; mech products get bandwidth **instead of** the skill row.
  Ordinary skill status uses the previously recorded any-one-skill bug. Both information and all-skill
  correctness are explicit in our proposed contract, without turning estimates into job promises.
- The source labels material-cost-plus-work and market value in the same row. Market value sums
  per-product values without multiplying quantities; work uses the recorded **0.0036** heuristic.
  Its tooltip enumerates hardcoded quality-price multipliers. Our slice shows clearly labeled
  per-item native value if available, quantities separately, and no guaranteed profit/trader quote.
- GetDistinctProductsFromIngredients enumerates butcher/smelt outputs and uses a **0.25** CostList
  recovery heuristic for non-intricate ingredients. This is not evidence of actual finalized yields.
  Special recipes stay visible with variable/unknown result descriptions rather than guessed amounts.

### Stat surfaces and limitations

Read relevant `StatCache`, `StatRequestWorker`, `StatRecordBar`, `StatRecordText`, accuracy/coverage
record paths and `StatMechanoidWorker`, plus optional adapter members. Source panels include:

- Ranged/melee damage, DPS, penetration, fire/attack timing, range, accuracy, burst count; optional
  stopping power and VEF shield use. Food/drugs show nutrition, serum duration, special display
  stats, safe-dose estimates and royal-title information. Apparel shows armor, insulation, shield
  energy, body coverage/layers and equipped stat offsets. Medicine shows quality cap and potency.
- Implants traverse surgery recipes, added parts, human body descendants and hediff special stats.
  Mechs show generated-pawn weapon/DPS, armor, movement, shield, work activities and recharger.
  General display includes selected beauty values, mass and HP with category exclusions.
- `GetStatRangeWithPercentiles` delegates to all-linear min/max, ignoring its percentile arguments.
  Bars can use nonlinear display; negative timing values are transformed for preference direction.
  Pairing is by StatCategoryEnum; text records never compare. Medical potency and quality maximum
  are both assigned MedicalTendQualityMax, so source bars can pair different facts. Our facts use
  semantic identity and unit, not that category or a localized label.
- Several stat getters omit zero/nonpositive values; beauty is shown only outside [-4,4]. Unknown,
  valid zero and negative values are distinct in our contract. CacheFor catches any exception and
  retries with default Stuff, which can misrepresent the selected material; we do not adopt it.
- Ranged DPS uses primary projectile plus first non-melee verb timing and assumes hits; fire adds
  **12 DPS** (the flat-damage path also calls that helper). Melee flat damage takes the largest tool
  damage; attack timing averages tools without weights. These are not a colony combat prediction.
- Mech stat sampling generates a pawn and caches by ThingDef; no disposal is visible in that path.
  Initial ammo can affect cached results; CE recalc exists in the stat path. Our previews do not
  generate actors or simulate production to obtain a number.
- CE AmmoSystemEnabled is hardcoded true despite an unused settings-query helper. Ammo/projectile,
  magazine and ToolCE fields are reflected; missing type checks are incomplete (ToolCE check repeats
  AmmoSetDef). Secondary damage sums amounts without chances, and penetration chooses sharp else
  blunt. CE uses millimeter armor formatting broadly. A source support claim is not verified 1.7
  compatibility or permission to combine unlike units.
- Research Reinvented resolves IsAvailableOnlyForPrototyping and appends prototype recipe/style
  entries. VEF calls ShieldUtility.UsableWithShields. BWM reflects Main.Instance, copy/paste handler,
  ExtendedBillDataStorage and resource icons for links, clipboard and output destination controls.
  All require exact future optional-adapter evidence; none establishes a hard product dependency.

## Concrete incoming consumers and dependency inspection

A read-only `rg -l -a -F NiceBillTab` scan across installed Workshop and local Mods `.dll`, `.cs`,
`.xml`, `.json` and `.md` inputs found the following candidates. This text/metadata scan is bounded
evidence, not proof that no computed/reflected consumer exists anywhere. Installed does not mean active.

| Consumer | Evidence and actual binding | Consequence |
| --- | --- | --- |
| Nice Bill Tab - Expansion, `HICON.NiceBillTabExpansion`, Workshop 3721023311 | About hard-depends on source; inspected NiceBillTabPatchInstaller resolves TabBillsDrawer, RecipeHolder/Selection, source constructor/FillTab patch classes, category helpers and Utils. Patches recipe lists/rows, ingredients, copy/link buttons and source patch methods. | Direct owned composition after S011; do not retain these hooks against megamod. Full feature deep dive remains S011. |
| Colony Manager Redux, `ilyvion.colonymanagerredux`, Workshop 3310027356 | LoadFolders conditionally loads Integrations/NiceBillTab/1.7 for Andromeda.NiceBillTab. Compiled integration patches DrawBillQueue, DrawIngredients and DrawRecipeInfo; consumes RecipeSelection.SelectedBill, MadeFromStuff and DrawBillPreview shape. ManagerJob_Production.FindManagingJob suppresses managed-bill edits/material changes and supplies managed icon. | A concrete binary/source-identity dependency. Native queue retention and read-only inspection preserve restrictions by design; no binary drop-in claim or fake source package. |
| Nice Inventory Tab, `Andromeda.NiceInventoryTab`, Workshop 3609897594 | Load-after source plus IfModNotActive source loads a NiceBillTab/1.7 folder; DLL scan hit FloatSubMenu, not a demonstrated direct NBT API call. | Shared dependency-loading interaction; do not infer a gameplay feature dependency or copy assemblies. |
| Nice Plants Menu, `Andromeda.NicePlantsMenu`, Workshop 3685058533; Nice Research Tab, `Andromeda.NiceResearchTab`, Workshop 3773496046 | LoadFolders source-absence branches; Plants 1.7 branch points at NiceBillTab/1.6, Research only has v1.6 in inspected file. No direct consumer from the name hit alone. | Record conditional loader identities; 1.7 behavior remains provider-specific, not a claim here. |

Ignored consumer-only ILSpy outputs are `nice-bill-tab/consumer-expansion` and
`nice-bill-tab/consumer-colony-manager` under this turn's evidence root. SHA256 identities:

- Expansion `1.7/Assemblies/NiceBillTabExpansion.dll`:
  **E4FFD4EE3B9C79DA1E2767D5751BB7175032957D4E287AC0652E37C1D550F212**.
- Colony Manager `Integrations/NiceBillTab/1.7/Assemblies/ColonyManagerRedux.NiceBillTab.dll`:
  **10344D2ECB54AA3D188FEC1F789222FB30DF9220D66FF0C558AD95CEE48FC3D0**.

[Show Possible Materials](https://steamcommunity.com/sharedfiles/filedetails/?id=3338920230&l=english)
is **not installed at the checked Workshop path**; the earlier public compatibility lead is not an
installed match. Its author documents Ctrl ingredient-location indications and right-click selection,
and Nice Bill Tab support. Inspected [public source](https://github.com/emipa606/ShowPossibleMaterials)
at **340dff41c64c69f565420da17d863ada357be0c7**:
`Source/ShowPossibleMaterials/TabBillsDrawer_DrawBillPreview.cs` checks the source package ID and
targets `NiceBillTab.TabBillsDrawer:DrawBillPreview(Rect recipePreviewRect, Bill bill, bool selected,
BillStatus status, bool dragging, bool drawButtons)`. Its prefix takes `Bill bill, Rect recipePreviewRect`
and delegates to Bill_Production_DoConfigInterface.AddIngredientLocations; those prefix parameters
bind by name, not target position. The separate native path still needs a
real present-provider test with our queue. The repository's LICENSE.md is MIT (Mlie); that license
does not establish reuse permission for Nice Bill Tab. No code was copied into product files.

The bundled FloatSubMenu assembly was decompiled to ignored `nice-bill-tab/float-submenu`.
Metadata identifies Jesper Mattsson 2022, version 1.0.0.0. It supplies FloatSubMenus.FloatSubMenu and
related UI classes and installs its own Harmony owner `kathanon.FloatSubMenu` for menu distance
handling; it is not merely inert data. No NiceBillTab type binding was found in this bounded source
search. A copyright notice is not a license; no bundled-library reuse is proposed or authorized.

## Draft disposition and open work

The [coverage report](../replacements/nice-bill-tab.md) classifies all identified functional groups.
Inspection, two-choice comparison and explicit info/navigation are adapted. S009 owns numerical
comparison and creation; S012 owns final browser interaction. Queue redesign, cross-bench production,
automatic renaming and guessed contextual-stat panels are not silently adopted. S011 gets its own
full documentation-first research after this slice; consumer inspection did not complete it.

| ID | Kind | Remaining work |
| --- | --- | --- |
| NB-Q1 | Native implementation research | Exact 1.7 entry/lifecycle, safe stat/ingredient/work contexts and semantic patch admission; unknown/native fallback until supported |
| NB-Q2 | Composed custom behavior | S011/S012 final gestures, layout, stat ordering and enablement; proposed two-choice/read-only behavior is explicit |
| NB-Q3 | Contracts/provenance | Optional providers, consumer workflows, original-view coexistence, any justified facade and unresolved source/library/art license |
| NB-Q4 | Acceptance/performance | Shared-data measurements, paused updates, seven-language readable UI, protected bills and native before/action/after verification |

No host test, gameplay task, drop-in claim or live acceptance is completed by this source study.
