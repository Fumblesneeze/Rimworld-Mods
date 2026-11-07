## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
Origin S057/P1-51. [Proposal](proposal.md), [observable contract](specs/megamod-preserved-food-preference/spec.md),
[research](../../../mods/megamod/docs/refinement/2026-11-04-preserved-food-preference-research.md).
The selected DLL is2.4.0.0 despite About2.5.0.0; included Common.cs is older unrelated behavior.
All selected classes were inspected. No implementation exists and upstream data redistribution is
prohibited; independently implement requirements without copying source/data/art.

## Goals / Non-Goals

**Goals:** bounded humanlike preference, source-derived map/caravan distinctions, actual-eater
baseline taste weighting, safe once-only trader provisions and direct shared-food composition.

**Non-Goals:** guaranteed fresh-food choice across every native fast path, global preference/quality
mutation, food-policy rewriting, universal inventory reordering, a new menu, or a source identity
facade. No new food content, medicine/drug administration rules, animal preference or private cache.

## Decisions

### 1. Derived preferences, not global mutable Def policy

Retain source thresholds/weights but compute a scoped contribution instead of editing shared
ingestible fields in a startup constructor. Global preferability mutation changes eligibility,
title rules and animal consumers beyond the stated preference. Per-eater thought handling and
caravan-only rating cannot be safely expressed as static XML. Thus no Def mutation/PatchOperation
is selected. A later real declarative change still requires guarded XML Extensions and dependency.

Classify finalized ThingDef food facts once per content lifecycle: ingestible/food rating, rot
component and intrinsic daysToRotStart, special native exclusions, ordinary food category, Stuff
requirement and provision candidacy. This derived catalogue belongs to shared owned food data;
physical Thing candidates remain in native listers/S033 shared map data. Do not create S057 map
scans/cache or retain stale pawn eligibility. Rebuild on supported content lifecycle; teardown clears
references. No off-thread Verse access. Missing/ambiguous unusual rot semantics leave that food's
owned adjustment unsupported rather than guessing from its name.

Preserve source RawBad and <=DesperateOnlyForHumanlikes reductions, RawTasty-or-better preservation
classification (including long-keeping raw produce), thresholds15 and3 days, caps-35/-50 and short
contributions5/12. BabyFood is explicitly exempt from both classes even if a provider changes its
rot time; this extends the source's current-Core exclusion safely. Kibble remains excluded from
preserved/short classification but its humanlike RawBad penalty still applies. No empty-calorie drug
or special substance becomes a provision merely through a food-like label.

### 2. One map score contribution with actual reactions

Let b be the finalized food's ordinary optimalityOffsetHumanlikes. Calculate f from the PF03 table.
At the proven map score seam contribute f-b once, preserving native distance, rot, genes, traits and
foreign additive terms. Never add to a rejection sentinel or treat a nonhumanlike as a humanlike.
Short-lived replacement intentionally replaces this one b contribution, not every other score term.
Existing preserved b<=cap remains untouched, including no extra taste compensation.

Compute applicable baseline reaction set from the current food's tasteThought/specialThoughtDirect
and its matching classic ateEvent precept **actually present for this eater**, deduplicated by thought
identity and matched to the actual ingestion-thought set. Use valid current contributing stages and
the same native mood-to-score conversion. Ingredient-derived thoughts and nonclassic ideology/trait
extras remain outside compensation. Do not evaluate all classic beliefs as if every eater had them,
nor cancel absent Ascetic-nullified thoughts. Reactions outside positive(0,20] or negative[-5,0)
bounds are unchanged. An absent mood system means no invented baseline mood compensation.

Prefer reusing an already-computed native thought result at the admitted seam or a bounded per-call
snapshot; native thought utilities use shared scratch lists, so never retain a mutable list across
calls or recursive scoring. No second full thought scan per candidate unless measurement shows it
necessary and acceptable; record any chosen seam refinement before implementation. Do not change
actual food thoughts, nutrition, category or raw-food recognition to implement preference.

S024's (6-2d)^2 current-spoilage bonus remains separate and applied once. S055 calls the same composed
score inside its established donor class; do not both patch FoodOptimality and add S057 again in
S055. Native successful/early carried-food results remain authoritative. Preserve first-inventory
selection and the native32point inventory comparison adjustment. No claim all fresh food wins.

### 3. Separate caravan rating

Current CaravanPawnsNeedsUtility.GetFoodScore(ThingDef,Pawn,float) uses numeric preferability (or2
for lowPriorityCaravanFood), not FoodOptimality. The Thing overload adds up to0.999 current-rot
tie-breaker; CaravanInventoryUtility then selects and Caravan_NeedsTracker actually ingests.
For humanlike preserved foods with native preferability>=MealSimple and low-priority flag false,
apply the scoped rating delta MealAwful-originalRating while preserving other proven independent
contributions. Do not modify the underlying Def or apply map point/taste bonuses to this small scale.
Animal branch, native nutrition/policy/drug/ideology checks and travel's explicit title exception
remain native. Actual ingestion owns physical consumption and shared food history.

### 4. Bounded arrival provisions form their own atomic branch

Retain the selected source's additional provision feature with explicitly documented corrections.
Capture the exact native trading incident's generated pawns, and commit only after successful
arrival/lord establishment. SendLetter by itself is insufficient: current code can later fail.
Use the narrow established arrival lifecycle or guarded capture/commit hooks; no global list of
every pawn or speculative reusable event framework. Compose with any owned arrival features by
direct calls, never patch their implementation.

Eligibility: living humanlike, food need, legitimate NPC trader arrival, carrying capability, no
currently edible RawTasty-or-better carried item under actual automatic eating rules. Do not use
native hunger-full state to conclude that an otherwise valid carried food is inedible; evaluate
diet/content permission independently of whether the just-arrived full pawn wants to eat now.
Do not provision player caravans, pack animals or repeated notices. No shared-container borrowing
expansion is introduced. Food in another pawn's inventory does not count as personal provisions.

Candidate pool: preserved, non-Stuff ordinary food category/ancestry, positive finite effective
nutrition, not drugs/BabyFood or special nonfood substances, and actual visitor diet. No category
name substring or broad fallback to arbitrary preserved ingestibles. Compatible unusual food is
allowed when semantic facts hold; source-like random provision does not add a faction-tech gate.
The resulting potential luxury ration grant is a retained balance choice requiring final review.

Uniformly select among types fitting at least one item. Compute q=min(stackLimit, floor(6/N), shared
capacity count); if q<1 exclude. Then random count max(1,floor(q/2))..q inclusive. Use effective item
nutrition and physical mass/capacity, not raw statBases or a six-item fallback. Creation, verified
whole insertion and once-only grant bookkeeping commit together; failure leaves original inventory
unchanged. Any cleanup removes only newly created unaccepted items/attributed owned additions, never
unrelated merged contents. Do not retry after uncertain insertion or create a second type to mask
partial failure. An unsupported insertion/rollback contract disables this branch before spawning.

Persist an event/pawn grant completion record only as needed for actual lifecycle deduplication;
reload/notification duplication must not provision again. Existing ordinary items remain physical
if support is disabled. Teardown releases transient references. No periodic refill, game-load grant,
source-save migration or UI button to farm provisions. Native trade/sale/eating rules still apply.

### 5. Per-patch semantic admission and independent fallback

Planned identifiers below require target documents, validators, tests and evidence before code.
Hashes in research are input identity, never runtime admission rules.

| Branch / candidate target | Consumed semantics and admission | Fallback |
| --- | --- | --- |
| PF-MAP: FoodUtility.FoodOptimality or narrower proven computation seam | Eater/final food/distance role, actual b/mood contribution, humanlike branch, sentinel behavior and single final score consumed by selection | Leave original score and ordinary search unchanged |
| PF-CARAVAN: GetFoodScore Def overload plus caller chain | Humanlike base rating, lowPriority flag, animal branch, Thing rot tie-break and native eligibility/ingestion remain in expected order | Leave native caravan choice unchanged |
| PF-ARRIVAL: exact trader capture/success/commit group | Correct generated pawn list, completed successful arrival, once-only event, whole quantity insert/capacity/rollback semantics | No owned provision created; ordinary visitors retained |

Any argument/result-changing hook needs structural IL/data-flow proof of the consumed semantics,
not merely existence/signature. Observer-only capture can require only callback shape if no body
execution fact is assumed. Account for relevant skip-original prefixes/transpilers, effective startup
ordering and supported late repatch revalidation. Validate incoming transpiler instructions on every
run; failure returns them unchanged. Do not repeatedly execute foreign transpilers as probes.
Compatible observers and unrelated body changes are allowed; no whole-method hash or owner blacklist.

Each branch is atomic. Missing/false/unknown predicates, validator exceptions or partial install
failure roll back newly installed owned hooks only. Warn once per skipped enabled patch with stable
package/patch/spec IDs, target, reason, unavailable behavior and fallback. Other branches can remain
active if independent. Explicit disable is not a failed enabled patch; absent optional content is
quiet. Active `seekiworksmod.no10` suppresses all S057 overlap, without undoing original Def changes
or provisions; fallback means ordinary **loaded-game** behavior, not pristine Core behavior.

### 6. Contracts, settings and package boundaries

Installed Immersive Chefs validates source package/assembly/types, startup constructor, internal
preserved set and exact caravan postfix/owner. It may invoke the source constructor. Our scoped
behavior cannot satisfy that binding with a namespace shim. No package spoof, copied field or own
constructor that pretends to mutate source Defs. Record unchanged external binding as unsupported;
any other repository product changes need separate ownership. No source custom saves/settings to
import; physical items already generated are ordinary game data.

No new menu or setting selected. Constants are named and balance values remain in the observable
spec. Later selected tuning uses the common global/save path, main-menu guards and existing settings
navigation. Food policies and visitor inventory are gameplay state. Seven baseline languages apply
to any added explanations/warnings; no new player-visible art is needed.

Harmony required with integration; DLCs/food providers optional and shape/Def guarded. No source
or repository-product dependency, no source data redistribution and no Gateway in product package.
If later static XML modifications are actually selected, use XML Extensions guards and merge
preservation with required `imranfish.xmlextensions`; this design currently needs none.

## Risks / Trade-offs

- Ingestible offsets and actual thoughts are intertwined → prove effective contribution and preserve
  outside modifiers; no runtime compatibility claim from original code alone.
- Native inventory early returns do not compare all meals → explicitly retain that limit; S123 may
  later specify a separate complete inventory-choice behavior with its own provenance.
- Source2.4 behavior is broader than the title → retain raw penalties and trader provisions explicitly;
  proposed personal-thought, classification and capacity corrections are documented.
- Global category changes would affect eligibility/animals → use scoped map/caravan contributions.
- Modded luxury rations can increase visitor resources → source-like candidate breadth is explicit,
  bounded by6nutrition, one stack/capacity/event; finalized market/progression review remains required.
- Other sources later adjust food choice → S111/S123/S135 remain later research, no invented conflict;
  S024/S055 composition is explicit now.

## Migration Plan

Documentation only. Implement with focused tests and separate branch admission. No foreign settings/
save rewrite or data migration; existing physical food is retained. Own event deduplication data must
survive save/load, tolerate absence and never backfill grants for existing visitors. A later explicit
migration would require backup, atomicity, restoration on failure and verified postconditions.

## Verification

Unit: bounded score/classification/eater reactions and provision quota/deduplication calculations.
Harmony: only owned hooks with cleanup, including supported/changed/benign effective shapes. Defs:
small constructed complete fixtures only. Loaded Core/food provider/Ideology/caravan globals,
PatchOperations and real jobs/arrivals require exact isolated1.7 processes. No mocked active mod.

Use actual hungry-pawn food selection and ingestion, real caravan departure/travel/consumption,
and native trader arrival/inspection/eating on the reviewed build. Retain before/action/after food
counts, nutrition, mood, order/policy and provenance; personally inspect screenshots. Include
preserved-only fallback, source-present suppression, nonhumanlike control, frozen/near-rot food,
individual beliefs, S024/S055 once-only composition, optional food and failed/duplicate arrival.
Seeded disposable setup is permitted; directly adding provisions or ingesting through diagnostics
does not prove the native workflow. Finalized calorie/mass/value comparators and source-like luxury
grant economics need a separate semantic balance pass. All implementation/acceptance remains open.

## Open Questions

PF-Q1–4 in research track selected deviations and final product review, effective native/consumer
contracts, shared food and finalized balance, implementation and native acceptance. The draft does
not pause sequential research to ask the user about existing source behavior.
