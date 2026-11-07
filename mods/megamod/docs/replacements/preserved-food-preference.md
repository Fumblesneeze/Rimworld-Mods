# Prioritize Meals over Preserved Foods

## Identity and provenance

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
Origin replacement S057/P1-51, `seekiworksmod.no10`, author seeki,
[Workshop2899127245](https://steamcommunity.com/sharedfiles/filedetails/?id=2899127245).
Inspected2026-11-04. About/Manifest2.5.0.0; selected1.7 assembly/file2.4.0.0 SHA256
`D40471125013DB212F0397CDCCB661802FFF661A53AF18E280BA613A04B81352`.
Included Common.cs is stale; current behavior was decompiled read-only. Source notice prohibits
redistributing its data. No code/art/binary is copied or licensed by this analysis.

[Research](../refinement/2026-11-04-preserved-food-preference-research.md) records all selected classes,
current native map/caravan/arrival comparisons, source XML comparators and actual reflected consumer.
Status: analyzed, observable draft independently reviewed and strictly validated. Nothing implemented.
[Proposal](../../../../openspec/changes/specify-megamod-preserved-food-preference/proposal.md),
[design](../../../../openspec/changes/specify-megamod-preserved-food-preference/design.md),
[spec](../../../../openspec/changes/specify-megamod-preserved-food-preference/specs/megamod-preserved-food-preference/spec.md),
[unchecked tasks](../../../../openspec/changes/specify-megamod-preserved-food-preference/tasks.md).

## Behavior inventory

All rows are specified, not implemented or verified.

| ID | Intended action/result | Boundary |
| --- | --- | --- |
| PF01 | Compare food without changing permission or real effects | Humanlike preference only; diet, access, current action and actual nutrition/mood/history retained |
| PF02 | Classify shelf life | Intrinsic >=15days/no rot preserved; <=3days short; native rating thresholds, BabyFood/Kibble exclusions |
| PF03 | Prefer ordinary map food through finite contributions | Emergency -200, raw bad -100, preserved caps -35/-50, short5/paste12; native early inventory choices retained |
| PF04 | Weight baseline taste for the actual eater |98% reduction of bounded pleasant preservation preference; bounded mild short-food aversion counterweight; actual mood unchanged |
| PF05 | Eat through travel supplies | Humanlike preserved simple-or-better ranked awful only for caravan choice; normal low-priority/rot/animal rules |
| PF06 | Combine selected food features once | S024 bonus and S055 donor class/native-first fallback preserved; shared physical quantity/history |
| PF07 | Arrive without usable food → bounded provisions | Successful NPC trader arrival only; actual diet/capacity, positive nutrition, at most6nutrition/one stack/event |
| PF08 | Load or encounter unavailable branch → honest fallback | No accumulated penalty/grant; original source suppresses owned overlap; useful bounded startup warnings |

## Selected deviations and shared behavior

The source globally rewrites food offsets/categories using a startup thought catalog. The owned
draft scopes changes to humanlike map/caravan choices and actual applicable baseline reactions.
It preserves animal behavior, real food quality/title eligibility and actual mood. Baby food stays
excluded even under a changed rot-time provider. Stronger existing preserved penalties remain intact;
short-lived food still replaces its one food-specific contribution as explicitly specified.

The selected binary's additional trader provisions are retained, with usable-food/diet checking,
effective nutrition, capacity, successful-arrival and once-only safeguards. Arbitrary nonfood/Stuff
fallback and zero-count/failed-insertion behavior are omitted. Compatible luxury modded preserved
food remains possible; no new faction-tech gate is silently added. This source-like economic effect
requires finalized balance review, not an unsupported claim that the mod changes only ordering.

S024 Common Sense contributes its selected spoilage bonus once. S055 Meals On Wheels retains
native-first failure fallback and about-to-rot donor grouping. S021/S041 own portions/capacity/history;
S033 shared facts avoid another Thing cache. S111 Room Food, S123 personal meal carrying and S135
caravan loading remain future sequential composition work, with no assumed conflict.

## UI, settings and persistence

No source settings/bottom tab and none selected for this feature. Constants remain named and
documented; later tuning uses common global/save inheritance with main-menu protection. Food policy
is contextual gameplay state and is never rewritten to make travel food available.

Only the necessary owned arrival deduplication state is saved. Existing food remains ordinary
physical food. No startup/load resupply, source custom-data migration or whole-package removal safety
promise. No new art is needed; any new text requires all baseline languages.

## External contracts

Installed Immersive Chefs1.6 actually validates the source package/assembly, Main startup type,
internal Foods.preservedFoods HashSet and exact private SendLetter postfix/owner. It can invoke the
source constructor. A namespace facade does not satisfy that behavior. Unchanged binding is not
supported; no package spoof or dependency on another repository mod. RimPy provides only package
catalog metadata. The bounded scan found no further executable consumer, not universal absence.

Source present means suppress all owned S057 overlap, preserving source Def changes and provisions.
No foreign unpatching or save editing. Any explicit future migration requires backups, atomicity,
failure restoration and verified postconditions under a separate contract.

## Coverage and verification

**Nothing implemented.** Runtime target documents, score/arrival integration, honest RED/GREEN,
package/languages, independent implementation review and final native acceptance remain open.
No global food-category edits, guaranteed universal fresh-first selection, personal inventory
reordering, periodic trader refill, copied source data or unrestricted provider claim is selected.

PF-Q1–4 in the [ledger](../refinement/specification-progress.md) retain selected policy review,
effective beta/consumer contracts, finalized/shared-food balance and runtime acceptance. The draft
has 8 requirements, 32 scenarios and 23 unchecked tasks. Independent review found no issues.
Typed strict validation passed 79 items, zero failed; eight scoped documents passed local links and
whitespace checks.
Acceptance must show actual map/caravan eating and native trader arrival/provision/consumption;
score queries or directly inserted food are supporting setup, not proof. No game or build run here.
