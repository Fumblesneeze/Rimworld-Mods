# S057 — Prioritize Meals over Preserved Foods research

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement P1-51. Inspected 2026-11-04. Selected binary/source/native/consumer analysis
recorded; observable draft independently reviewed and strictly validated. Nothing implemented or
accepted in game.

## Public documentation, identity and licensing

Read [Workshop2899127245](https://steamcommunity.com/sharedfiles/filedetails/?id=2899127245)
first. It advertises automatic preference for ordinary meals while retaining preserved food for
travel/fallback, including modded food, with no settings. Its FAQ says access, diet, distance and
individual preferences still matter and animal preference is not the intended target. The author's
comments distinguish Baby Food and Room Food behavior. None is proof of actual compatibility.
The page and About explicitly prohibit redistribution of the mod's data. Independently engineer
behavior; no source code, binary or image reuse is authorized or selected.

Package `seekiworksmod.no10`, author seeki, installed root
`F:/Steam/steamapps/workshop/content/294100/2899127245`. Supports1.3–1.7; LoadFolders selects only
`1.7`, **not Common**. About/Manifest report2.5.0.0; selected DLL assembly/file version is2.4.0.0.
Requires Harmony; load-after hints Core/Royalty/Ideology/Biotech are not DLC requirements.

| Exact input | SHA256 |
| --- | --- |
| About.xml | `F5DC5CE221E930141967B9DE5AD60B56685B432B191D6945E6167FDCE71F4F9B` |
| Selected1.7/Prioritize Meals over Preserved Foods.dll | `D40471125013DB212F0397CDCCB661802FFF661A53AF18E280BA613A04B81352` |
| Included Common/Sources/Common.cs, stale | `E8D19C5546E6B389F9C12BF8AE7B0DE1CEE5C750247F51FB3AEB637356595A32` |
| Native Assembly-CSharp1.7.4998rev1299 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |
| Installed ImmersiveChefs1.6 consumer | `0937C7721341DDA4B925A75B3107A0FD1AC0ECF404F9FF48E1CB408BB240E147` |

Ignored evidence root `artifacts/Refinement/megamod-spec-20261104/preserved-food-preference/`:
`compiled`, `native`, `consumer-immersivechefs`. Current FoodUtility original decompilation is retained
under `artifacts/Refinement/megamod-spec-20261103/meals-on-wheels/native/` against the same game hash.
All five selected behavior classes and AssemblyInfo/generated project were read fully. Public and
installed About/Manifest/LoadFolders were read. No selected XML/Defs/languages/settings exist.

The included Common.cs and AssemblyInfo were read fully; the included project framework, compile
inputs, references/output paths and absence of custom build targets were inspected. Its Common.cs
uses namespace `Prioritize_Mealss_over_Preserved_Foods`, category-name regex matching and simply
sets preserved foods to MealTerrible. Its AssemblyInfo names an unrelated old mod and version1.0.
It does **not** describe the selected1.7 DLL. The reconstructed selected project is net10.0 and
references Assembly-CSharp/0Harmony; this is decompiler output, not current author build source.
No upstream build was run. Bundled old bin/obj assemblies are not selected load-folder evidence.

## Complete selected implementation

Main's static constructor installs blanket Harmony owner
`Prioritize_Meals_over_Preserved_Foods.HarmonyPatch`, then mutates loaded food Defs. It reflects
FoodUtility.FoodOptimalityEffectFromMoodCurve; if the field is absent, Calculation supplies the
same six-point curve: (-100,-600),(-10,-100),(-5,-70),(-1,-50),(0,0),(100,800).
There is no semantic startup validation, rollback or general exception handling around this pass.

Native enum values: Undefined0, NeverForNutrition1, DesperateOnly2, DesperateOnlyForHumanlikes3,
RawBad4, RawTasty5, MealTerrible6, MealAwful7, MealSimple8, MealFine9, MealLavish10.

| Branch | Exact mutation |
| --- | --- |
| Any ingestible preferability<=3 | Subtract200 from optimalityOffsetHumanlikes |
| RawBad | Subtract100 from that offset |
| Remaining candidate foods | Ingestible, preferability>=5, defName not exactly Kibble; no food-category/positive-nutrition check for preference changes |
| Rot component with daysToRotStart>=15 | Add to preserved set; if existing offset>-35, replace with-35 then subtract98% of eligible positive-thought curve contribution |
| Rot component with daysToRotStart<=3, preferability>5, not BabyFood | Replace offset with12 for native nutrient paste, otherwise5; subtract eligible negative-thought curve contributions, thereby offsetting those aversions |
| No rot component, including null comps | Add to preserved set; if offset>-50, replace with-50 then subtract98% of eligible positive-thought curve contribution |
| Preserved set members with preferability>=8 | Globally change preferability to MealAwful7 |
| Stored Foods.preservedFoods | If any preserved candidate is non-Stuff and has a category whose name contains food case-insensitively, retain only those; otherwise retain the entire preserved set |

Thought candidates are a deduplicated set of tasteThought, specialThoughtDirect (both checked for
nonempty stages), plus all classic PreceptDef memory thoughts matching the food's ateEvent when
Ideology is active. The latter path checks thought/event non-null, not nonempty thought stages.
Always reads stage0. Positive mood strictly>0 and<=20 is discounted98%; short-lived-food negative
mood>=-5 and<0 is fully counterweighted. This is a global Def computation, **not specific to the
actual eater's traits/ideology/current thoughts**. Existing offset at or below the preserved cap
skips both replacement and thought compensation. Short-lived replacement ignores previous offset.

PMOPF_DefOf supplies native MealNutrientPaste; Foods holds an internal static HashSet<ThingDef>.
Calculation supplies the fallback curve. No new job, save key, Def, menu, bottom tab or settings UI.
Preference Def changes are not themselves saved; inventories created below persist normally.

## Additional trader-provision behavior

Patch_IncidentWorker_TraderCaravanArrival attaches an int.MinValue-priority postfix to SendLetter,
receiving `ref List<Pawn> pawns`. For each humanlike with an inventory and **no inventory item with
ingestible preferability>=5**, it picks a random entry from Foods.preservedFoods and creates it.
It starts quota6 items, or floor(6 / raw statBases Nutrition) if a positive explicit Nutrition entry
exists; quota is capped to stackLimit. Count is random inclusive floor(quota/2)..quota, then TryAdd.
No food-needs, pawn diet, actual current nutrition, body mass capacity, empty candidate set, zero-count,
repeat invocation or TryAdd-result guard. No Stuff supplied to ThingMaker. It does not search pack
animals, existing trade stock or another pawn before creating food. It never removes existing items.

This additional content-generation path is absent from the public feature overview but present in
the selected DLL and a concrete external consumer's contract. It must be explicitly specified or
listed as deferred/omitted; not silently lost during a food-preference port.

Static risks, not reproduced bugs: empty preserved set can fail RandomElement; fallback set can
contain Stuff foods; nutrition>6 yields zero quota, absent/invalid nutrition uses six arbitrary items;
quota1 permits zero count; disallowed/unusable inventory food suppresses supply; unconsumable random
provisions and failed insertion are not checked. More calls can provision again after contents change.

## Native 1.7 contracts and comparators

Read complete current IncidentWorker_TraderCaravanArrival, FoodPreferability and CaravanPawnsNeedsUtility;
targeted full TryGetBestFood, food-needs ingestion, FoodOptimality, TryFindBestFoodSourceFor and
ThoughtsFromIngesting/AddThoughtsFromIdeo. Read Core meal inheritance and Pemmican, Biotech BabyFood,
Core paste precept/taste and fine-meal thought XML. Original code/source XML is not finalized active-mod
or live acceptance evidence.

- FoodOptimality starts300-distance, handles emergency preference/sentinels, current rot bonus12
  when fresh with<30000ticks to rot (except inventory-taking), actual ingestion thoughts through
  the curve, humanlike/animal-specific offsets, raw-digestion gene bonus and trait modifiers.
- Global offset mutations chiefly affect humanlikes; global preferability mutation is broader:
  selection eligibility/minimum quality, title rules and caravan code can consume it. Therefore
  'animals unchanged' is not proved merely by the field named optimalityOffsetHumanlikes.
- TryFindBestFoodSourceFor has important bypasses: nonplayer getter's carried food and soon-rotting
  personal food can return before scoring; only one first qualifying inventory item is considered.
  Inventory-versus-map comparison subtracts32 from the inventory score. Existing source does not
  guarantee a fresh meal wins every search. S055 only runs after native search failure.
- Caravan food selection uses CanEatForNutritionNow, then GetFoodScore, **not FoodOptimality**.
  Humanlike base score is preferability number, or2 for lowPriorityCaravanFood; current rot adds
  at most0.999. Animals use a separate classification/nutrition score. Source's downgrade to7
  explains its caravan effect; just porting map offsets would omit that behavior.
- Caravan eligibility uses nutrition, WillEat with title refusal disabled by native travel rules,
  auto-food flag, drug/ideology permission and hunger restrictions. Need resolution calls Ingested
  and removes destroyed inventory contents; retaining ordinary food policy never rewrites policies.
- Native trader arrival spawns pawns, fills food need, sends letter, finds gathering spot and creates
  trade lord. SendLetter occurs before that final gathering-step success, so notification alone does
  not establish a completed arrival event or safe once-only provisioning lifecycle.

| Core/Biotech source comparator | Shelf life / preference | Humanlike offset before source | Selected source result |
| --- | --- | --- | --- |
| Simple meal |4days /8|16 inherited|Unchanged16 and8; not within short-life<=3 branch |
| Fine meal |4days /9|16, +5 taste|Unchanged base; normal +40 taste contribution |
| Packaged survival meal |No rot /8|-5|-50, preference7 |
| Pemmican |70days /8|6|-35, preference7 |
| Nutrient paste |0.75day /7|16 inherited|12 before applicable classic-thought compensation; -4 mood curve is-65 so with that candidate becomes77 |
| Baby food |14days /6|No explicit food offset|Neither preserved nor short-life branch in current source XML |

Normal simple versus pemmican raw offset difference becomes51; simple versus packaged survival66.
These are finite preferences, not bans; distance, actual thought/trait terms and candidate eligibility
still matter. Paste compensation depends on source Ideology-active catalog path even though a default
classic paste precept is defined in Core. Do not silently claim equivalent no-DLC and DLC outcomes.

## Actual consumers and direct owned composition

Bounded installed DLL ASCII and UTF16 name/package searches found own versioned DLLs plus installed
ImmersiveChefs1.6. XML/C#/JSON search found About metadata and RimPy's package catalog. The scan is
not universal proof of no other consumer. Consumer source, selected compiled PrioritizeMealsAdapter
and PrioritizeMealsCompatibility were read; exact installed hash above.

Immersive Chefs detects `seekiworksmod.no10`, expects the exact source assembly, Main startup attribute,
Foods.preservedFoods as nonpublic static HashSet<ThingDef>, and private static void
SendLetter_Postfix(ref List<Pawn>) on its named class, all in one assembly. It can run the validated
Main class constructor, then verifies the source owner on the exact caravan postfix. Version is
reported but not gated. The adapter sets Enabled and logs coexistence; bounded source search found
no external use of its Enabled property. No independent repository-product change is authorized here.
Namespace-only facades or package spoofing would not preserve this startup/owner/behavior contract.

S024's selected perishable bonus is (6-2d)^2 for0<d<3 remaining days; it must occur once alongside
S057. S055's native-first fallback and fresh<12hour donor class must remain intact, with any S057
score component applied once within that class. S041 carries food history; S021 owns physical
quantity/capacity. S123 carrying, S111 Room Food and S135 caravan loading need later sequential
reconciliation. No separate Thing cache or self-patches are justified by this source.

## Draft direction and remaining work

The [owned change](../../../../openspec/changes/specify-megamod-preserved-food-preference/) now
specifies scoped humanlike map/caravan preference, native permissions/effects, source numeric weights
and direct S024/S055 composition. It retains raw penalties, short-life contribution replacement and
bounded trader provisions. Actual-eater applicable baseline reactions replace the global thought
catalog; ingredient/special-belief reactions stay intact. Scoped caravan ranking replaces global
preferability mutation. BabyFood is exempt even if another provider changes its rot time.

Trader corrections: successful arrival, food-need/diet permission, positive finite effective nutrition,
non-Stuff ordinary food, shared physical capacity and one event/pawn grant. Empty/invalid catalogs
skip safely; quota is min(stack limit, capacity count, floor(6/nutrition)), and only quota>=1 is eligible.
Random count is max(1,floor(quota/2))..quota. Whole insertion/deduplication is atomic with safe owned-only
cleanup. No fallback to arbitrary items, zero stacks or repeated grants. Luxury modded food is still
possible; no new tech/research gate is imposed. These are selected draft choices, not user-confirmed
balance or proven runtime behavior. Native early inventory returns and first-item selection stay.

| ID | Open research/specification work | Required resolution |
| --- | --- | --- |
| PF-Q1 | Selected policy review | Draft now covers all source branches with explicit scoped/actual-eater and provision corrections; review final product/balance intent before implementation, not a request to explain source behavior |
| PF-Q2 | Effective beta and consumer contracts | Minimal score/arrival semantic guards, actual source co-install policy and unsupported external binding documentation; no exact-hash gates or copied source startup |
| PF-Q3 | Shared food ranking, data and balance | Source-XML values versus finalized candidates; S024/S055/S041/S021 and later S111/S123/S135 composition; actual capacity and no repeated provisions |
| PF-Q4 | Implementation and native acceptance | No runtime work yet; eventual honest RED/GREEN, independent review, real eating/caravan/trader workflows and scoped package/evidence checks |
