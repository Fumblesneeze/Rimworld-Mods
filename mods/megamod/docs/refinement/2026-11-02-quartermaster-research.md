# Quartermaster — source research and draft decisions

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
S009 / P1-09. Research notes, **not an observable spec or implementation**. Continue this source
before Nice Bill Tab (S010), its Expansion (S011), then the integrated Add bill browser (S012).
The [owning change](../../../../openspec/changes/specify-megamod-quartermaster-comparison/) records
the selected behavior. Open details are retained here instead of asking the user about existing behavior.

## Confirmed scope takes precedence

The [interview](2026-11-01-handwritten-notes.md) requests Quartermaster-inspired comparison **inside
Add bill**, not a standalone bottom tab or automatic combat-role loadout recommender. Keep source
inventory and selected functionality distinct. The future S009 source change must document what
is reused, adapted or left outside requested scope; the complete composed browser also gets S012.
Do not erase S009 just because its useful behavior will compose with another view.

Confirmed browser intent: search; output-category pills derived from this workbench; relevant output
stat sorting with commonly used stats first; craftability requires enough ingredients **and** at
least one colonist meeting recipe skill requirements. Compare at **Normal** quality with no selector.
**Use all materials** is unchecked initially: use the best compatible material with enough available
stock; checked considers unavailable materials too. Add the bill with the material used in the
comparison, with an alternate-material action (right-click was tentative). No extra menu/bottom tab.

## Documentation and identity

1. Read [Steam description](https://steamcommunity.com/sharedfiles/filedetails/?id=3737474260),
   installed About.xml and the author's
   [change notes](https://steamcommunity.com/sharedfiles/filedetails/changelog/3737474260) first.
   The description covers craftable armor sets, weapons, buff comparisons, material-locked bills,
   owned-item comparison, presets, pins and apparel-policy creation. It explicitly says Combat
   Extended calculations are not directly supported. Generic Steam removed/incompatible banners
   are not compatibility evidence.
2. Description page currently tags 1.6/1.7 and shows an October update, while the fetched change-log
   snapshot ends in August. Old change-log UI features are leads to verify, not current installed
   behavior. For example it mentions a classic UI toggle absent from the settings class read so far.
3. Installed **Quartermaster**, `slim.quartermaster`, author **SLIM** (Steam author displayed as
   Ad Matay), Workshop **3737474260**, supports 1.6/1.7. About declares no mod dependencies and only
   a Core load-after hint. `LoadFolders.xml` selects root plus `1.7` for this game version.
4. Read-only `F:/Steam/steamapps/workshop/content/294100/3737474260/1.7/Assemblies/Quartermaster.dll`,
   SHA256 **C7091A86432ECB8ABBB77A14758BF5A955EEF94077F3EC5250D5B5700D202B82**;
   assembly version **0.0.0.0**, no metadata version found in About. Decompiled to ignored
   `artifacts/Refinement/megamod-spec-20261102/quartermaster/decompiled`.
5. No `.cs`, README or license was found in the installed package, and the targeted public-source
   search returned no identified author repository. This does not prove none exists. License remains
   unresolved; use behavioral analysis for independent implementation, not copied code/assets.

## Initial source findings

Paths below are relative to `decompiled/Quartermaster`. These are static inspection, not live tests.

- **UI:** root `Defs/MainButtonDef.xml` defines `Quartermaster`, order 18, minimized icon-only bottom
  tab using `MainTabWindow_BestArmor`. The large window has armor, weapons and buffs, selected wearer,
  presets, pin/exclusion dialogs, material menus, Craft/Focus, policy creation and refresh controls.
  Its full interaction inventory is still to read. Source bottom-tab placement is explicitly rejected
  for our requested browser. Do not copy a legacy window merely for parity.
- **Persistence:** `QuartermasterSettings` stores global window rectangle/lock, excluded item and
  material Def names, pinned Def names, material picks, custom preset strings, four range-score flags
  (all initially true) and preset-seeding flags. `QuartermasterMod.Save` writes mod settings.
  `MaterialPickManager` serializes `itemDef|stuffDef` and lazily keeps a process-static dictionary.
  This is not the shared global-default/save-override model. The existing source's Steam claim about
  not writing saves must not be expanded into “read-only”: bills and apparel policies mutate gameplay.
- **Recipe inventory:** `ArmorEvaluator.GetAllArmorRecommendations` enumerates all recipes with an
  apparel `ProducedThingDef`, records research/bench/material facts, scores variants, then groups by
  product Def and keeps a preferred recipe. Our per-workbench browser cannot discard distinct recipes
  merely because their main product matches. Weapon enumeration remains to inspect in full.
- **Research:** `IsResearchDone` checks the recipe's single/list prerequisites and also the product's
  recipe-maker single/list prerequisites. These may not be equivalent for alternate recipes; compare
  native 1.7 availability before specifying a per-recipe result.
- **Benches:** `IsBenchAvailable` checks whether any colonist building exists for a recipe's
  `AllRecipeUsers`. It does not itself check power, reachability or worker skill. These are different
  concepts from selecting a recipe at the currently opened bench.
- **Materials/counts:** available Stuff uses allowed product materials, exclusions and
  `resourceCounter.GetCount(stuff) / product.costStuffCount`, requiring `MinCraftCount` (initially 1).
  Fixed-cost counts similarly use the product `costList`, not arbitrary recipe ingredient requirements.
  `CraftableCount` takes the best single material's count and fixed-cost minimum. This is inadequate
  evidence for our all-recipe ingredient feasibility, especially alternate/multi-product recipes.
- **Product stats:** apparel sharp/blunt/heat and insulation use abstract material-dependent stats;
  other utility values combine base/equipped offsets and worn-hediff analysis (the latter still to
  inspect). `_statMemo` and `_allowedStuffMemo` are cleared on full apparel refresh. Normal quality
  is the user's explicit comparison requirement, not something proved by an abstract-stat call.
- **Material choice:** source apparel material score is `6*sharp + 2*blunt + 0.5*heat`; tie score
  adds `10*(cold insulation + heat insulation)` to `maxHP - 20*deterioration`, then prefers higher
  material market value. Melee material score uses DPS and AP; ranged material primary score is zero,
  falling back to durability. Within 0.001 is treated as a tie. These formulas must not silently
  replace the user's chosen output stat as the ranking criterion.
- **Hidden material preference:** `PickBestStuff` always prefers any non-last-resort material over
  Gold/Jade/Silver, even before comparing primary scores. They are not removed from the catalog, but
  the preference can override “best stat.” Our confirmed best-compatible-material rule has no such
  automatic precious-material demotion. Record the deviation explicitly in the future proposal.
- **Material overrides:** a valid globally picked Stuff overrides both available and best-possible
  choice. Excluded/incompatible picks are ignored. `MaterialChoices` sorts by last-resort flag,
  armor score/tie score/value and reports copies. Per-bill alternate material choice is entity state;
  no source-global override is implied by the user's right-click suggestion.
- **Bill creation:** `TryCraftBill` chooses the first colonist `Building_WorkTable` found among
  `AllRecipeUsers`, constructs ordinary or unfinished-production bill, sets Repeat count at least 1,
  and toggles every product-compatible Stuff filter to allow only the selected Stuff. It then adds
  the bill and returns the bench. Our destination is the workbench whose Add bill view is open.
  Check shared ingredient filter effects on required fixed ingredients before deriving the contract.
- **Craft UI:** `DrawCraftButton` shows blocked reason when not available, otherwise shows count and
  queues the requested repetitions with bench/item/material feedback. Owned rows instead Focus on
  the object or wearer/equipped pawn. Existing owned-item navigation is not automatically selected
  for our new-recipe browser.
- **Material catalog:** all Stuff plus non-Stuff fixed cost ingredients from apparel/weapon products,
  grouped by the first Stuff category (Metals/Wood/Leather/Fabric/Stony/Other, plus Ingredients).
  This is distinct from the user's recipe-output category pills. Its per-language cache is not the
  map-owned shared Thing index requested for search and hauling.

## Completed follow-up source pass

These findings extend, and where stated qualify, the initial notes above. All are static analysis.

### Comparisons, sets and owned gear

- `ArmorEvaluator.FillWeaponStats`, `ApplyScoreBands` and `ComputeRangedRawDps` compute a primary-verb
  projectile estimate from damage, burst size and warmup/cooldown/burst spacing. The table's ranged
  score averages accuracy across the **selected** 3/12/25/40-cell bands, counting an unreachable
  selected band as zero; at least one band must remain selected. The final composite multiplies by
  `1 + 0.75 * max(AP, 0)`. The separate `GetBestWeapon` helper averages only reachable bands and can
  fall back to market value for choosing a weapon. These are source heuristics, not native combat
  predictions or the selected-stat ordering requested here.
- Melee DPS first asks the native abstract stat; its fallback sums tool power/cooldown with a
  two-second fallback cooldown. Other catches leave fields at zero. We will distinguish unknown
  from zero, and will not reproduce heuristic DPS or claim exact CE calculations.
- Apparel utilities add equipped offsets and raw stat bases; `ApplyWornHediffEffects` reads only
  the first stage's offsets and certain negative capacity limits from an apparel-caused hediff.
  This does not establish actual pawn-dependent effects, later stages or a universal buff total.
- `RunGreedy`/`RunPresetGreedy` use body coverage, pairwise wear compatibility, a maximum 20 pieces,
  default 200% cap, score weights and pins. Source set budgeting subtracts shared material needs and
  attempts alternate materials. This is more than independent recipe comparison; it is not selected
  merely because Quartermaster was a reference. Do not copy its formulas as an unnamed best-stat sort.
- Wearer choices include adult/child profiles and present races. `WearerFilter` checks developmental
  stage, body groups and optional HAR rules. Nudist presets have ordinary/strict variants. The HAR
  adapter reflects `AlienRace.RaceRestrictionSettings.CanWear(ThingDef, ThingDef)` and returns true
  after an exception with a warning. Our comparison is not a claim that a particular pawn can wear
  the output; no equivalent permissive race adapter is implied.
- Owned apparel/weapons, worn equipment and tainted choices feed distinct pools; existing objects
  provide material/quality and Focus navigation. The recipe browser concerns future production;
  these existing-item pools and their separate caches are outside this change.

### Window actions and persistence

- `PreOpen` refreshes on first use/map change. `DoWindowContents` refreshes only for an absent
  recommendation list; explicit Refresh and certain settings/pin/exclusion changes refresh other
  pools. No stock-change subscription or periodic refresh was found in the window. We require
  automatic freshness and action-time revalidation rather than making the player remember Refresh.
- Initial display flags: Show crafted on; Show unavailable, Best possible, tainted items and selected
  pawn gear off. All five weapon-type filters start included. Default weapon sort is composite score
  descending; selecting another column defaults numeric values high first except time/mass low first;
  names/materials alphabetical. Clicking an active header reverses direction.
- Buff search matches stat labels, item labels and apparel slot terms. Weapon/armor/buff navigation,
  body/slot groups, collapsible rows, diagrams, totals and shortfall displays belong to the source
  window; S012 will specify the user's different category/search layout.
- Material cells open a menu with Auto and materials for which at least one copy is counted, plus
  the current pick even if now unavailable. A pick persists globally per product, affecting other
  recipes/saves. Our explicit alternate choice belongs to the bill being created.
- Craft all loops the current available set and can report partial success at different benches.
  Create policy makes a new uniquely named native apparel policy, clears its allowances and allows
  the recommended apparel **types**, not material/quality-specific assignments. Neither action is
  selected for this one-recipe Add bill comparison.
- Exclusions are explicit player actions, individually or by category with confirmation (Shift can
  bypass confirmation); item exclusion unpins that item. Material exclusions use a separate global
  list. Suggested exclusion reasons inspect heavy-weapon extensions and tag/class keywords such as
  warcasket/exosuit; they do not prove the item is unusable. No automatic removal is inferred.
- Pin picker groups head/body/legs/accessories and supports search, pinned-only, individual unpin and
  section/all clear. Pins, exclusions and material picks are saved immediately in global settings.
- Custom presets serialize name, weighted stats, exclusions and nudist flags; malformed entries are
  skipped. First-load seeding and a V2 migration retire one exact old preset and add Heat/Cold.
  Restore defaults adds missing names rather than overwriting existing edits. Window position/lock
  also persist globally. None of these source settings is silently imported into megamod.

### Native 1.7 contracts inspected

Read-only game assembly: `F:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll`, inspected target
**1.7.4995 rev309**, SHA256
`56399B6144D9F1695F9C53D054DC82972F7AB35E68D1A19A7129F2F5860CCD21`.
Selected types are under ignored `quartermaster/game`; these are implementation research, not
claims of a successfully exercised player workflow.

- `ITab_Bills.FillTab` uses the selected table's `AllRecipes`, `AvailableNow` and `AvailableOnNow`,
  then adds applicable ideology-style choices. The click warns when a mechanitor or sufficiently
  skilled free colonist is absent but still creates the bill. It also preserves concept/tutorial
  notifications. A missing worker/material is not a reason to outlaw future production planning.
- `RecipeDef.AvailableNow` includes research, meme, faction and ideology restrictions; it does not
  add the product's recipe-maker research checks as Quartermaster does. `ProducedThingDef` is null
  for special products or anything other than exactly one fixed product; source enumeration thus
  omits those recipes. Keep such recipes accessible even when comparison is not supported.
- `BillUtility.MakeNewBill` selects unfinished, mech-resurrection, mech-production, autonomous or
  ordinary bill variants. Do not hand-create only the two variants supported by Quartermaster.
- `IngredientCount.CountRequiredOfFor` uses ingredient value and recipe-worker count overrides;
  ingredients are not always units of product construction cost. `WorkGiver_DoBill` also distinguishes
  mixing, fixed ingredients, whole-stack recipes, haul-source containers, radii, forbidden state,
  reservations and reachability. Shared candidate counts cannot replace those action-time checks.
- Both `Bill.IsFixedOrAllowedIngredient` and native ingredient selection accept fixed ingredients
  independently of the editable filter. This corrects the initial concern that toggling every Stuff
  allowance necessarily removes a fixed steel ingredient. More complex overlapping or non-fixed
  ingredient roles still need proof before promising material locking for those recipes.
- Native ingredient selection has unusual shared-count bookkeeping in the inspected code. The
  comparison must not advertise a successful allocation by counting the same unit twice; tests must
  exercise overlapping ingredient groups and compare against actual jobs. Do not port the algorithm
  mechanically or declare a native bug from decompilation alone.
- `GenRecipe.MakeRecipeProducts` takes Stuff from the dominant ingredient. A generic filter update
  is not by itself proof that the displayed material becomes the output; prove the dominant-ingredient
  route for each supported recipe family and fail the material-specific action visibly otherwise.
- `StatRequest.For(BuildableDef, ThingDef, QualityCategory)` defaults explicitly to **Normal**.
  Abstract values still need stat-worker-specific suitability checks; no random item creation,
  pawn spawning or equipment mutation is needed just to browse.
- `ResourceCounter` counts fresh, unfogged resource Things in storage slot groups, updates every
  204 game ticks, and returns zero for uncounted resources. This explains a concrete difference from
  the planned shared Thing index: a valid loose ingredient outside a stockpile can be missed, and
  stock counts are neither recipe allocation nor pawn access evidence.

### Incoming contracts and remaining boundaries

- Text search of installed Workshop/local mod XML, C#, JSON, Markdown and text for the package,
  Workshop ID and selected public type names found no external reference. A broader DLL string
  scan for Quartermaster returned RimPacts 1.6/1.7. Read-only decompilation of its 1.7 assembly found
  an unrelated `RimPacts.RptWarOffice.Quartermaster` enum used by `Hediff_RptWarOffice`, not a
  Quartermaster-mod binding. That candidate is `wowgag.RimPacts`, Workshop `3762723122`, inspected
  DLL SHA256 `9E37387FB2432755C3B146F84FE4C6C3986240DE41A6B861DFB4F4B36CD3D8DE`.
  Neither search proves absence of reflective or future consumers.
- Source exposes public evaluator/managers/window classes but declares no extension API. Its Main
  Button Def and global settings schema are observable source contracts; neither is reproduced or
  aliased by this source-inspired integration. Normal bills/policies remain native save objects.
- Quartermaster co-installation can retain its separate window; megamod does not patch, hide or
  control it. This is a proposed independent coexistence policy requiring a later present-provider
  run, not proof from metadata. Other Add bill providers are researched under S010/S011.
- License/source-host uncertainty remains. Independent implementation may use these behavior facts;
  no source assembly, code, art or translations is copied into product files.

## Open questions retained for later

| ID | Kind | Unresolved item and next evidence |
| --- | --- | --- |
| QM-Q1 | Native/provider implementation | Prove per-recipe material locking, ingredient feasibility, special bill types and exact stat-worker suitability on the 1.7 target; create semantic patch target documents only for actual hooks. |
| QM-Q2 | Composition / custom scope | S012 must finalize detailed search/category/sort ordering and alternate-material gesture. Multi-product or context-dependent output ranking needs an explicit policy; until then it stays visibly unranked with native bill access. No guessed aggregate score. |
| QM-Q3 | External contracts / provenance | Resolve the public source/license if reusable material is desired; audit any new consumers and exact co-installed providers before drop-in/coexistence claims. No migration or facade promised. |
| QM-Q4 | Performance / live verification | Measure shared-index freshness and recipe/material evaluation with realistic mod lists; prove resource conservation, Normal previews and actual chosen-material products through native player workflows. |

The detailed contract records proposed fallbacks and limits so these questions do not masquerade as
implemented behavior or block the next source's specification turn.

## Draft verification

The finished source-inspired change has 12 observable requirements / 36 scenarios. Typed strict
`openspec_validate` passed **30/30 items**; the eight-file scoped link check had **0 broken links**.
Independent read-only document review found **no issues** after checking intake/foundation and selected
source/native paths. All 16 implementation/acceptance tasks remain unchecked. No code was implemented,
upstream file modified or game launched; the next sequential source is S010 Nice Bill Tab.
