# Original: routine personal meal reserve

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target1.7 beta.
Origin **original**, queueS123. The user's interview confirms routine meal carrying for all
colonists, excluding animals. It does not identify a source mod called Carry Food or confirm an
exact quantity. Native1.7 and the existing owned food/hauling specs ground the proposed defaults.
No upstream license/package/assembly is copied or impersonated.

[Research](../refinement/2026-11-06-routine-meal-carrying-research.md) retains native hashes, paths,
observed source logic, proposed deviations and MC-Q1–5.
[Proposal/design/tasks](../../../../openspec/changes/specify-megamod-routine-meal-carrying/) and
[observable contract](../../../../openspec/changes/specify-megamod-routine-meal-carrying/specs/megamod-routine-meal-carrying/spec.md).
Status: independently reviewed, strictly validated specification draft. No runtime implementation
or compatibility acceptance.

## Behavior inventory

| ID | Action → observable result | Default/edge | Status |
| --- | --- | --- | --- |
| MC01 | Eligible colonist reaches ordinary packing opportunity → collects a personal reserve | Current Food>30%; capable children/secure slaves included, animals and dependent-care cases excluded | Specified draft |
| MC02 | Reserve is low → collect fitting whole portions | Target1 full Food bar, refill<=40%, one oversize meal only with empty reserve, partial pickups allowed | Specified draft |
| MC03 | Personal meals mix with cargo/stock → each portion keeps its purpose | One S021 authority, explicit keep counts once and is not cancelled by automatic target | Specified draft |
| MC04–05 | Nearby communal food is considered → choose currently suitable meal |20cells, no communal-shortage cutoff, actual diet/nutrition/thoughts; shared scoring once | Specified draft |
| MC06 | Pawn approaches and takes → actual bounded portion moves | Combined mass/volume, no lost rejected split, food history preserved | Specified draft |
| MC07 | Diet/target/storage changes → reserve reconciles without loops | No immediate destruction/drop, protect explicit keep/current use; retain oversized meal | Specified draft |
| MC08 | Player unloads/resumes → deliberate control over replacement | Pending order suspends; successful explicit removal pauses until Resume; cancellation before removal preserves former state | Specified draft |
| MC09 | Pawn eats or shares → native use of actual food | S055 fallback and current-use protection preserved; donor cannot borrow to refill | Specified draft |
| MC10–11 | Settings/load/changed targets → coherent current state or visible unavailable path | Common global/save target0–3 by0.25, no replay/free food, atomic guarded opportunity | Specified draft |

## Interactions and composed result

S024's active packing requirement now explicitly delegates quantities/retention here while keeping
the30% trigger and removing the communal entry check. This intentionally refines native absolute0.4/0.8
inventory/stack thresholds, eater nutrition and cargo accounting. No second opportunity or patches
against owned Common Sense code remain. S021 supplies actual portions/capacity; S033 supplies shared
facts where useful, not a new food cache. S041 taint/ingredients follow real portions.

S055 sharing can use eligible kept meals for actual hunger; S123 cannot use sharing to restock.
S057 and S024 scores compose once. S111 excludes reserve packing, and its dining-room preference
is not imported here. S062 medical stock, S073 bedrolls and S133/S135 transport keep their ownership.
Future S132 inventory UI must expose the same pawn actions rather than a second stock model.

## UI audit and settings

Native automatic job and Gear inventory are the primary surfaces. Add reserve amount/target,
waiting/unavailable/paused status and the contextual pawn Pause/Resume action there. No bottom tab,
debug action, bottom-right global toggle or generic loadout manager is added.

One actual configuration value lives in common **Supplies and work / Personal meals**:
Reserve target in full Food bars, default1, range0–3 in0.25 steps;0 stops automatic collection and
releases automatic protection safely. It uses global defaults, per-save overrides and Use global
without scope exceptions. Save controls/callbacks are disabled at main menu and the override editor
is disabled while inheriting. Existing pause and quantity ownership are pawn gameplay state, not
settings. Seven baseline languages required on implementation.

## Consumed contracts and differences

| Contract | Treatment | Evidence needed later |
| --- | --- | --- |
| Native PackFood/ThinkTree and TakeInventory | Use one guarded opportunity and ordinary approach, with checked shared-capacity transfer | Effective targets plus actual native pickup/eating |
| Native explicit inventory-stock and keep | Count suitable personal food once, never overwrite player entries or add a second quota | Stock/unload/split/merge/player-edit workflows |
| Native FoodUtility/food policy/MaxNutrition | Current eater-specific suitability/quantity; no global policy rewriting | Different appetite/diet/title and modded prepared foods |
| Native inventory cleanup | Same reserve protection as packing, including oversize exception | Small pawn/large meal and absent-storage no-loop cases |
| Other packers/inventory providers | Optional narrow actual contract; one owner or proven cooperation | Package/shape-specific active runs; none yet accepted |

Intentional deviations from native: no communal shortage cutoff, scaled refill threshold, no minimum0.8 candidate-stack rule,
explicit full mass+volume acceptance, cargo exclusion, actual eater nutrition and thought stage,
title permission, one oversized meal retained coherently and explicit-removal pause. The original
request does not authorize a private food lock, generated food, a new drug reserve or a guaranteed
full reserve during scarcity. Quantity/tuning/eligibility details remain proposed MC-Q1 policy.

## Implementation and coverage

**Nothing implemented.** All runtime tasks remain unchecked; no target document or provider
compatibility is called verified. No source settings migration exists. Required Harmony/XML
Extensions dependencies must be declared with their first real use; optional content must remain
absent-safe and semantically guarded. Product code never depends on another repository mod or Gateway.

## Verification and acceptance

Source/decompile and document checks establish a draft only. Two independent reviews found no issues and strict validation passed145 items with0 failures,
tracked in [progress](../refinement/specification-progress.md). Later focused vertical TDD and
package checks precede independent code review and native player acceptance. Observe actual
stack→inventory→eating and inventory→sharing/unloading results, including no-loop/partial failures,
not merely declared counts, installed patches or clean logs. Retain exact reviewed-build identity,
ordered mods, action screenshots, config hashes and cleanup. Final balance review must check food
availability, work/care interruption, throughput, nutrition/mood and capacity on the finalized game.
