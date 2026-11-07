# S085 — FoodTracker research

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
2026-11-05. Specification only; no source execution, product implementation or game launch.
[Change](../../../../openspec/changes/specify-megamod-partial-ingestibles/),
[coverage](../replacements/food-tracker.md), [ledger](specification-progress.md).

## Public documentation first

Attempted [Workshop3795037221](https://steamcommunity.com/sharedfiles/filedetails/?id=3795037221)
and its English variant; web retrieval failed. Installed About was readable. The author's
[Nexus page](https://www.nexusmods.com/rimworld/mods/771) identifies Food Tracker (Partial Ingestibles),
version1.4.0, updated2026-09-13. It describes preserved interrupted food/drink/drug portions, scaled
duration/effects, remaining mass/value, per-piece stacks and caravan estimates. Its small-leftover
claim uses0.01 nutrition; inspected code instead uses4.5% of an item. Broad custom-meal/VEF/VCE
compatibility statements are author claims, not accepted support. The page requires permission for
file modification/asset reuse and disallows redistribution; no affirmative source reuse license was
found in the bounded clone/distribution scan. Independent implementation only.

Then inspected [public repository](https://github.com/Boarlock/FoodTracker). README is a code-size
report, not a behavior contract. Source files and selected installed decompilation fill the gaps.
FoodTracker is **partial ingestion**, not a colony food-count dashboard; the initial task commentary
was corrected after reading About. No fictitious dashboard feature is selected.

## Identity and evidence

| Input | Value |
| --- | --- |
| Entry | S085/P2-14; FoodTracker |
| Author/package | B0ARL0CK / `b0arl0ck.foodtracker` |
| Installed root | `F:/Steam/steamapps/workshop/content/294100/3795037221` |
| Declared supported game | 1.6 only |
| Selected DLL | `Assemblies/FoodTracker.dll` |
| DLL SHA256 | `ECA6DD778A9B245B5BCED51909F157829ADF5B94D4061DE6EF68E72784217A43` |
| About SHA256 | `74651B6A931C66DE29B61DE82EE1CE5E5B9CC8E5B1444E64D4DBFB19B08B4564` |
| Assembly/file metadata | 1.0.0.0; informational `1.0.0+a33a17d152cac55c483b95336260073f3697fc83` |
| Public HEAD | `c08eed778661fee6a63a5dcb0f2245dc265b0e54` |
| Native input | 1.7.4998 rev1299, game-root Assembly-CSharp.dll |
| Native SHA256 | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Ignored evidence: `artifacts/Refinement/megamod-spec-20261105/food-tracker/` contains `upstream`,
selected assembly's full `binary` decompilation and selected `native` types. Fetched informational
commit read-only; HEAD differs only in README/cloc report, not active source. No committed binary
or fresh reproducible build proves byte equality. Critical decompiled chewing, amount calculation,
filter/cleanup and fractional effect paths agree with inspected source. Full behavioral analysis
uses the active source, not every merged Harmony class. Hashes identify inputs, not admission rules.

Project targets netstandard2.1, references1.6 paths and ILRepack-internalizes0Harmony. It excludes
`Experiments/` and `--obsolete--DestroyedFoodRecovery.cs`; neither is selected functionality. Installed
About has no declared Harmony dependency because it bundles Harmony. Owned implementation must
instead require external Harmony and never ship a foreign assembly. No source language catalog,
bottom tab or real gameplay settings was found. Only verbose logging is exposed, defaultfalse.

## Active source inventory and actual behavior

All18 root active C# files were inspected; generated/obsolete/experiment files were excluded.
The following is code-derived evidence, not observed gameplay. Several debug logs are unconditional.

| Source files / classes | Observed behavior and boundaries |
| --- | --- |
| FoodTrackerMod / Settings | GetSettings, unconditional PatchAll, settings category Food Tracker, English developer logging checkbox; serialized verboseLoggingfalse |
| CompFoodTracker / Utility / Properties | Singleton thisMealFraction and list remainingFractions; spawn normalization, Scribe and legacy nutrition conversion |
| IngestionState / FoodTrackerIngestionTracker | Static Pawn-keyed active attempts, original and generated Defs, batch count/fractions, start/duration ticks and deferred-destruction list; no Scribe/teardown here |
| FoodTrackingHelpers | Exact six original drug names; generated names are not recognized; original-name helper; MinimumPartialFraction0.045; direct food-need/record mutation |
| IngestionPatch: ChewIngestiblePatch | Admission via WillEat/nutrition/supported-drug OR; dynamically generates Def, snapshots ingredients/fractions and job.count; partial duration multiplier max(40/baseTicks,totalFraction); wraps init and registers attempt |
| IngestionPatch: cleanup | Prefix computes clamped elapsed ticks/TotalTicks. Success removes state; other endings manually handle interruption, then immediate destruction and removal. Reentrancy flag has no guaranteed finally cleanup |
| IngestionPatch: amount postfix | Normalizes tracked state, sums first numTaken fractional entries against NutritionForEater, only reduces item count, replaces returned nutrition; does not scale the rest of Thing.Ingested |
| IngestionPatch: TryDropPatch | Suppresses exact ThingOwner.TryDrop overload whenever tracked Thing is being ingested, without supplying an ordinary successful drop |
| IngestionInterruptionHandler | Consumes totalFraction × current-attempt progress; removes whole entries, mutates one fractional remainder or creates a partial Thing; discards remainders below0.045; manual food/drug effects on interruption |
| PartialMealFactory / DeferredFoodDestruction | Makes new generated Thing and copies only ingredient list, then drops near pawn.Map/Position; no explicit rot/HP/poison/quality/provider-state copy. 'Schedule' destroys immediately, not asynchronously |
| DynamicMealDefFactory / ThingFilter patch | Memberwise-clones original Def; English Partial suffix, added comp, virtual parent/categories/short hashes/resource-counter refresh. Calls SetAllow(original,true) for every food policy. Any FoodTracker_ Def makes ThingFilter.Allows returntrue |
| MergeAndSplitPatches | Transpilers around native MakeThing/CeilToInt with fixed local assumptions; fraction lists move from front in exact counts. Validation drops/pads entries with0.01 or normalizes singleton state; unmatched injections silently leave code unchanged |
| FoodTrackerDrugEffects | Manual hardcoded six-drug effect table; all effects use current-attempt fraction, not consumed original dose. Direct need/heddiff writes, addiction/OD RNG and extensive unconditional logs; see below |
| FoodTrackerGenerateFromLoad | Patches Game.ExposeSmallComponents to create named generated Defs before loading; saved generatedDefs list plus legacy scan of food policies/map-root things; records discovered Def names |
| FoodPatch | Fully replaces nine-argument ApproxDaysWorthOfFood; reconstructs native simulation with fractional averages by Def, snapshots and private native fields/delegate. Retains broad food/forage/grazing/baby simulation; custom-generated forage branch draws random fractions during estimate |
| CaravanPatch | Static TransferableOneWay → List<Thing>; tracks matches from generic TransferableMatching, clears at four dialogs' CalculateAndRecacheTransferables; labels tracked rows '(varies)' |
| MassPatch | Patches two CollectionsMassCalculator overloads, sums first requested fractions times whole stat; separate DrawMass text replacement. Does not establish every hauling/general stat consumer |
| MarketValuePatch | Selected partial fractions × original abstract value, then native price/full abstract value ratio, preserving getter sign; buy/sell getter postfixes and DrawPrice/DrawMarketValue label transpilers |
| GetExplanationPatch / GetStatDrawEntryLabelPatch | English per-item Nutrition/Mass/MarketValue explanations and stack-total displayed labels; nutrition skips exact recognized original drug names, not generated variants. Presentation is not universal stat accounting |

### Important edge cases and source discrepancies

- Source initial IsDrug checks the current Def; a generated partial drug can lose that classification
  even though the interruption handler later resolves its original Def. Normal completion then runs
  native outcome/comp callbacks with only nutrition adjusted. Broad 'partial drugs work' is not proved.
- For a half-dose attempt interrupted halfway again, source's manual effects use0.5 rather than0.25
  of the original dose. Repetition and native completion can over-credit effects. This is a static
  correctness finding, not a reproduced game exploit.
- Interrupted nutrition uses abstract Nutrition, whereas native NutritionForEater includes relevant
  raw-food recipient modifiers. Direct need credit is capped to available need and records only the
  capped amount; ordinary finalization records the returned ingestion amount. These must be reconciled.
- A created partial only receives ingredients explicitly. Original rot progress, HP, poison cause,
  quality and modded state have no equivalent explicit transfer here. The owned contract forbids
  freshening/cleaning by cancellation.
- Comp normalization can shrink physical stackCount to list count, remove fractional entries, or
  invent0.01 portions. Split/merge repair can pad0.01 instead. Neither is a conservation contract.
- The saved generated-def list and legacy nutrition fields are not a serialized in-progress attempt.
  Legacy fallback scans only food policies/map-root things, not all nested holders/world inventories.
  Full source-save/mid-eating correctness remains unverified.
- Caravan tracking includes all matched Things and the estimator sums fractions by Def; selected
  counts do not identify exact selected portions. Its custom forage branch mutates Unity RNG. Trade
  and mass each reconstruct subsets independently. A coherent selected transfer must replace this.
- No minimum-chunk disposal or source drug-duration patch is necessary to preserve partial food;
  rejecting them is an intentional owned balance choice, not an upstream behavior claim.

### Six-drug interruption table

Values below are **source literals**, not owned beta balance constants. Full-dose effects must come
from finalized native/provider content and recipient rules. Column order is drug-desire/recreation/
rest/high/tolerance/existing-addiction offset. Source subtracts the last column.

| Drug | Source values |
| --- | --- |
| Beer | 0.2 /0.17 /0 /0.15 /0.016 /0.20 |
| Ambrosia | 0.2 /0.50 /0 /0.50 /0.032 /0.10 |
| Psychite tea | 0.2 /0.40 /0.10 /0.75 /0.030 /0.15 |
| Smokeleaf | 0.2 /0.80 /−0.10 /0.50 /0.030 /0.06 |
| Go-juice | 0.3 /0.40 /0.40 /0.50 /0 /0.20; psyfocus0.15 |
| Flake | 0.3 /0.70 /0.20 /0.75 /0.040 /0.30 |

High severity uses tolerance/body-size modifiers except go-juice; tolerance uses both. Chemical
addiction-need relief is props.needLevelOffset ×0.9 × attempt fraction. New-addiction probability
uses native tolerance/genetic factors then attempt fraction and a new random roll. Go-juice/flake
only get source overdose handling: gene gate, fractional large-overdose chance, random target85–99%
scaled again by fraction, otherwise body-size-adjusted random offset scaled by fraction. No generic
native event/letter/history/dependency parity follows from this table. Yayo, wake-up, penoxycyline,
luciferium and arbitrary added drugs are outside the source helper's six-name list; the broader
Chew admission predicate is not proof they are safely unsupported or safely handled.

## XML and native1.7 findings

Installed DrugPatches.xml unconditionally replaces go-juice and ambrosia baseIngestTicks with100 and
adds beer120. These lack XML Extensions content/shape checks. No list replacement was found here;
the issue is unguarded scalar mutation/possible duplicate scalar, not a claimed category wipe.
Owned draft omits these changes. Any future static change must use guarded, idempotent operations
with required XML Extensions and retain unrelated content.

Native types inspected read-only: Toils_Ingest, Thing, CompDrug, IngestionOutcomeDoer_GiveHediff,
Caravan_NeedsTracker, IngestibleProperties, Pawn_JobTracker and NutritionForEater in FoodUtility.
Native chewing has interval actions and a durationMultiplier-dependent ticksLeftThisToil; cleanup
ends in nullable carryThingAfterJobOverride and clears driver/job before its final drop decision.
Source targets a nonnullable final bool. This is a concrete shape mismatch in the inspected beta,
not a live compatibility test.

Thing.Ingested includes thoughts/history, joy, poisoning, hediff/gene notifications, splitting,
PrePostIngested, outcome doers, destruction and PostIngested. FinalizeIngest separately credits food
need/records and contextual meal thoughts; caravans directly call Ingested without map chewing.
GiveHediff uses configured/default severity rather than multiplying by ingestedCount. CompDrug adds
positive existing-addiction offsets, unlike the source's subtraction, and supplies overdose,
notifications, drug-policy/history and combat/recreation timestamps. These paths are why dose
handling cannot be reduced to a nutrition postfix or a manually copied list of benefits.

Three Core source comparators: go-juice80ticks,0.1mass,53value; ambrosia80ticks,0.1mass,15value,
0.2nutrition; beer0.3mass,12value,0.08nutrition with no explicit duration override in its Def/base
and the inspected IngestibleProperties default500ticks. Tea210, flake650 and smokeleaf720 ticks
provide additional duration anchors. These are inspected XML/defaults, **not finalized active-mod
values or balance acceptance**. Owned behavior retains whichever supported full-portion values
actually apply; it introduces no new research, crafting yield or market multiplier.

## Consumed contracts, UI, lifecycle and composition

Public symbols include the FoodTracker assembly/namespace, CompFoodTracker and its properties,
FoodTrackerGameComponent.GeneratedDefNames, DynamicMealDefFactory.Prefix, static tracking dictionaries,
helpers and patch classes. Save keys: thisMealFraction, remainingFractions, generatedDefs;
legacy nutritionThisMeal/nutritionEntries/remainingNutrition; global verboseLogging. Generated Def
names are saved identities, not a drop-in API the megamod automatically adopts.

Bounded installed text scan for FoodTracker/package/helper terms returned only its own About.
An ASCII DLL scan for package and principal tracker/component names outside the source returned no
matches. This does not inspect every UTF-16/#US/reflection string or prove external consumers absent.
No facade, package alias, source-save import or VEF/VCE compatibility promise is made.

No source bottom tab. Keep useful quantity UI in existing inspection/inventory/trade/travel surfaces;
replace literal '(varies)' with accessible amounts/details. All seven languages remain required.
Item fractions are entity state. Source verbose logging is diagnostic-only; any owned control belongs
in the top debug toolbar, transient/off by default, with no gameplay settings row. Actual future
configuration would use the common global/save inheritance model.

Owned composition covers the shared Ctrl+F/hauling index and capacity, personal meal/medical supplies,
storage limits, poisoning, preserved-food preference, medical go-juice, Drug Policy Fix, Drug Response,
Priority Treatment, RPG inventory, Bulk Load and Basic Caravan Loading. These are direct interactions,
not patches to owned code. Actual remaining quantity is shared; eligibility/priorities are still
decided by their owner at action time. No dependency on another repository mod.

## Open work and validation status

| ID | Open work / resolution boundary |
| --- | --- |
| FTR-Q1 | Exact fractional addiction/overdose/poison risks and categorical-event schedules, including split/reload/multiple recipients; affected paths stay inactive until specified |
| FTR-Q2 | Complete native/provider effects and item/path eligibility, including thoughts/history/chemical dependency/notifications; no benefits-only adapter |
| FTR-Q3 | Resumed/recombined batch work and changed-eater timing; save/mid-toil feeding/holder recovery and missing-content behavior; source migration remains separately contracted |
| FTR-Q4 | S041's uniform-without-replacement selection versus exact unequal-portion quotes; read-only previews/failed transfers, recipes, supply equivalents and physical stack thresholds; joint path gated until reconciled |
| FTR-Q5 | Actual incoming consumers/optional providers, selected UI/languages and combined-consumer performance; no universal compatibility or optimization claim |
| FTR-Q6 | Honest RED/GREEN, executable target checks, review, finalized balance and exact reviewed native workflows/evidence |

Implementation tasks remain unchecked. Both independent document reviews and focused re-reviews are
complete; final rereviews found no remaining issues. Typed strict validation passes107 items with
zero failures. The draft has16 requirements,37 scenarios and25 unchecked tasks. These checks do not
accept gameplay. Next selected entry is S086 Go-juice is medical.

Independent reviews identified two precise composition gaps. Native JobDriver_Ingest (same hashed
beta input, retained under `artifacts/Refinement/megamod-spec-20261103/meals-on-wheels/native/`)
uses one chewing duration per batch. A three-item batch interrupted after75% leaves0.75 item but
25% of original batch work; per-item recalculation would instead yield75% of one-item time. FTR-Q3
now explicitly gates the unresolved batch/resumption policy. S041 requires uniform selection without
replacement and no future-allocation change for failed transfers, whereas unequal partial units
need an exact quoted/delivered subset. FTR-Q4 now names this interaction and gates the joint path;
neither fair taint selection nor read-only previews are silently overridden. The public member
GeneratedDefNames is also distinguished from its serialized key generatedDefs.
