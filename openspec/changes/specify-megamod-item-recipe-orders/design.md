## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S096/P2-25. [Research](../../../mods/megamod/docs/refinement/2026-11-05-item-recipe-orders-research.md)
records the selected Micro Designations1.6 source/binary, native1.7.4998 and actual legacy consumer.
The source advertises1.0–1.6, not1.7. Its public repository's selected DLL is byte-identical to the
installed reference; no license grant was found. Implement the specified behavior independently.

The source derives reverse item gizmos and workgivers from recipes. It attaches a detached bill to
a job, relaxes every null-stack bill's lifetime check, deep-saves such bills through every job, and
re-adds designations after failures. This supplies useful explicit processing, but broad patches,
incomplete material checks and repeated failure are not selected contracts.

## Goals / Non-Goals

**Goals:** truthful contextual operations, native recipe outcomes, exact quantities, ordinary worker
eligibility, bounded recovery, shared owned processing and narrow external compatibility.

**Non-goals:** another bill manager, new bottom tab or work category, a generic arbitrary-job runner,
multi-input/batch recipe automation, medical operations, source binary/save parity, automatic repair,
copied assemblies or a separate private Thing cache. S015 owns bulk smelting; S137 owns health-driven
medical bill filtering. This feature does not create missing recipes or provider content.

## Decisions

### Discover capabilities from finalized definitions, recheck actual items

Reuse the shared definition/recipe metadata already needed by the add-bill browser. Enumerate actual
RecipeDef.AllRecipeUsers and appropriate building bill-givers/work routes. Do not repeat the source's
test of the first AllowedDef's stackLimit or assume one IngredientCount entry means one physical
unit. Native IngredientCount.CountRequiredOfFor incorporates the recipe worker and ingredient value;
the actual bill context and selected item's material matter. Require a complete admitted material
plan of exactly one selected unit, no additional ingredients and a supported item-processing job.
Reject zero, invalid, variable/unsupported and multi-unit plans rather than coerce countQueue to1.
Recipes for live pawns/placed buildings use other workflows. Batch recipes remain normal bills.

Keep hard recipe filters, ingredient-slot filters, special filters, current corpse/item state,
provider restrictions and RecipeDef.AvailableNow distinct from the default ingredient preferences
of a newly created Bill. For an explicit choice, configure only that one-item intent as an ordinary
bill could be configured; do not bypass effective Bill.IsFixedOrAllowedIngredient or other provider
restrictions. Native fresh butchery defaults to animal corpses but permits configurable human
corpses; explicit authorization retains ideology/mood and ordinary consequences. No blanket override
of a relic, quest, repair or provider restriction. Unsupported semantics yield a disabled explanation.

Do not mutate every foreign ThingDef.comps, generate one workgiver per recipe, or append unsorted
workgivers at startup. Use owned declarative job/designation definitions and runtime recipe metadata.
If actual Def/Orders membership must change, use XML Extensions guards and merge rather than C# Def
mutation; first use adds the required dependency. Dynamic source Def identities are not owned IDs.
Ordinary item contextual commands suffice; source cell/drag methods alone do not prove an exposed
Architect tool. The quantity confirmation defaults to all currently selected units and allows fewer;
mark no future spawns. Shared action presentation must disambiguate similarly labeled recipes.

Plan before research/building availability, but do not plan an unsupported input or worker protocol.
Refresh definitions at their valid lifecycle boundary; re-evaluate mutable research, recipe
availability, current facility, item state and worker facts before display/claim/start/completion.
Use native map listers/shared map facts for candidate Things; share the S021/Ctrl+F candidate index
only where it already supplies useful facts. Never cache pawn-specific permission as a shared fact.
Invalidate on state, spawn/despawn, holder, split/merge and map changes; rebuild after load, clear at
teardown and keep Verse/Unity access on the game thread. No unbounded polling or new global cache.

### Give the job valid context without changing ordinary bill semantics

Keep item orders separate from the bench's visible BillStack. S014/S017 link, copy and synchronize
real bills, so temporarily inserting a hidden ordinary bill is not safe. Use one owned execution
context bound to the physical input, recipe, actual bench and operation identity. Native recipe
helpers may receive a properly owned Bill/BillStack context if their complete lifecycle supports it;
the exact minimal mechanism must be proven under MDR-Q1. Do not select a global null-bill bypass.
Persist only owned execution state through its owned lifecycle, not a prefix rewriting all Job.bill
serialization. All native/provider work callbacks must receive the context they actually require.

Use native eligibility for the worker, work type, recipe availability and skill requirements before
claiming anything. WorkGiver_DoBill performs checks that constructing JobDefOf.DoBill alone bypasses.
The native driver adds input/bench/interaction reservations; preserve these and reserve the exact
authorized unit. Resolve the full path and ordinary danger/access conditions. Choose the nearest
eligible facility to the item with a deterministic stable tie, not the source's zero-for-all-fueled
sort key. Consider a fuelable bench's UsableForBillsAfterFueling and native refuel path; never pass a
null bench onward after exhausting unusable candidates. Existing bills at equal work priority win.
An admitted Work Tab integration inherits the original underlying workgiver's priority; do not add
an independently prioritized entry for each recipe or rely on a translated workgiver name.

Follow native work amount, WorkSpeedStat, worktable factor, WorkSkill/LearnFactor, products,
quality, efficiency, ingredient consumption and notifications. Native Toils_Recipe performs work,
skill learning, MakeRecipeProducts, ConsumeIngredients, iteration notifications, records and quest
effects; preview must not call those effectful paths. Drop products physically, as the source does,
then leave further hauling to ordinary jobs. Do not reroute native recipes through S022's1600work/
25% salvage model. In the inspected Core: steel slag400work→15steel; combined SmeltOrDestroyThing
uses its recipe's smeltingWorkAmount1600 for a smeltable item and workAmount400 otherwise. These
are recipe fields, not item-specific work stats; ordinary separate equipment smelting also uses1600.
These are current native inputs, not owned balance knobs. Other finalized recipes retain their values.

### Compose the existing processing state, not another order system

Use S022/S023's processing-choice and authorized-portion model, extending the same S021 quantity/
holder primitives where necessary. The hauling ownership record alone is insufficient: processing
must also retain choice, active/suspended state and completion phase for each authorized portion.
An owned recipe choice replaces only another unfinished owned processing choice, never all
designations on the Thing. Recheck intent immediately before each irreversible effect.

Conserve marked versus unmarked quantities through split, merge and movement. Known consumption
retires its portion; unattributed count removal debits marked first. Ambiguous differing choices or
suspension states pause affected work. Never activate suspended quantities by merging or resuming a
neighbor. Equip/wear/install/personal keep cancels; normal hauling preserves; map departure suspends
and requires explicit Resume on return. Physical single-unit isolation must retain native metadata;
unsupported custom stack identity cannot justify consuming the whole stack.

Work progress follows each actual recipe's normal unfinished-work rules. Unlike S022's independently
defined durable recycling work, a normal recipe that loses interrupted work does not gain artificial
cross-job progress. The same active saved job retains native work state. This distinction belongs to
the operation, while authorization and completion conservation remain common. For supported custom
mending, successful nonconsuming mutation ends the order; later damage does not reauthorize it.

An ordinary recoverable shortage waits on its relevant condition, not a failed pickup loop. Suspend
after the first unexplained/unsupported execution failure and retain reason across restart/reload.
Explicit Retry or a relevant capability change permits another bounded attempt; merely selecting a
new pawn or letting time pass does not erase the failure. No optimistic re-add from EndCurrentJob.
Completion is a separately owned phase: input present, effects/results committed, output owed,
delivered. Track exact resolved output custody and completion-effect delivery so partial placement
does not regenerate products, duplicate notifications/XP or refund consumed input. Use the same
recovery access as S022, including absent input/bench. Unknown custom effect transactions are not
safe merely because their worker subclasses a native class; withhold them before starting work.

| Owned participant | Direct composition |
| --- | --- |
| [S022 recycling](../specify-megamod-equipment-recycling/design.md) / [S023 melee destruction](../specify-megamod-melee-item-destruction/design.md) | One explicit choice/quantity, distinct work/yield, common suspension and recovery, unrelated orders preserved |
| [S054 repair knowledge](../specify-megamod-repair-knowledge/design.md) | Call its target-item policy before admitted mending effects; no alternate repair bypass |
| S014 linked workbenches / S017 workbench management | No hidden general bill to copy/link/count or synchronize |
| S021 hauling / S033 Ctrl+F | Shared physical quantity/holder facts and map candidates; actual capacity/access checks at use |
| S052 Lights Out / S053 RePower | Direct supported work-activity demand and native notifications; preserve fuel/power, no internal Harmony stack |
| S009–S011 add-bill UI | Shared recipe facts; item-order entry remains contextual, no duplicate production browser |

### Settings and source contracts

Common definitions under Production → Item recipe orders, ordered as in the spec:

| Preference | Built-in default | Scope / timing |
| --- | --- | --- |
| Hide recipes with unmet colony requirements | false | Recipe.AvailableNow conditions, including research/ideology/faction; common global/save inheritance, next contextual refresh |
| Hide recipes with no built facility | false | Same; a missing bench can still leave a planned order |
| Hide recipes whose facilities are all unresearched | false | Same; existing built compatible facility qualifies as source intended, but recipe execution still obeys availability |

These are fresh-source defaults; its missing saved research key defaults true, an inconsistency we
do not import. The first label clarifies the source's actual AvailableNow check beyond research;
worker skill, access and bench presence remain separate conditions. Existing orders/cancel/recovery
are never hidden. Use the common load/store/render
path, dynamic inheritance, dormant overrides and main-menu callback guards. No settings exception.
Translate English, German, Spanish, French, ChineseSimplified, Russian and Japanese. Item choices,
quantities and progress are saved gameplay state. No new top/bottom toolbar toggle is appropriate.
Use normal keybinding behavior only when an action is unambiguous; show an explicit choice if a
shared binding would match several item operations. Icon assets are not selected by this draft.

Public source contracts are documented in research: RecipeJobDef, DefExtention.Ignore (actual
spelling), DesignatorHotKey.hotKey, dynamic recipeName+Designation, MicroWorkGiverDef and source
save/settings keys. The only bounded external executable-data consumer found is historical Simple
Recycling XML in its1.3/1.4 folders; it names two types absent from the selected1.6 assembly and is
not selected by its1.5 folder. Do not create those nonexistent types or spoof the package to activate
that historical patch. An actual future consumer requires a narrowly tested facade or documented
unsupported binding. No whole-mod drop-in claim follows from preserving player workflows.

Custom worker/job adapters, Work Tab and BetterLoading are source integration references, not
dependencies or confirmed installed current providers. Resolve actual canonical packages and
consumed shapes before any adapter. No BetterLoading hook is needed just to imitate startup order;
use the normal admitted finalized-definition lifecycle. The public Medieval Overhaul repeated-job
report motivates context/retry tests but is not a verified current provider conflict.

### Admission, coexistence and declarative changes

| Candidate boundary | Minimal consumed semantics / atomic scope | Failure behavior |
| --- | --- | --- |
| MDR-T1 contextual commands | Exact selected physical item/quantity and native gizmo action route; validity plus mutation route | No actionable command if safe authorization cannot be installed; status/cancel stays reachable |
| MDR-T2 recipe execution | Actual eligible input count, recipe availability/worker checks, bench context, normal effect and completion paths | Withhold affected recipe route before work; intact input remains owned and safe |
| MDR-T3 quantity/holder lifecycle | Successful split/merge/consume/equip/install/map transfer and per-portion attribution | Suspend affected processing; no substitute or unmarked promotion |
| MDR-T4 owned persistence/completion | Owned context load after admission; effect/result/custody recovery before job resumption | Restore inactive/suspended state or outstanding output only; no replay |
| MDR-T5 optional worker/work-priority adapter | Canonical provider, callable shape, required context, eligibility and commit semantics | Only affected adapter unavailable; ordinary provider bills remain provider-controlled |
| MDR-T6 any actual XML addition | Real package/content/node checks and idempotent additive membership | Skip only guarded atomic addition with bounded reason; preserve unrelated content |

These are design candidates, not completed target documents. Prefer owned jobs/direct APIs where
possible. Every actual Harmony/XML patch needs a registered stable target spec and executable
minimal semantic admission. A genuine observer may need only existence/callable shape; argument or
control-flow changes require the consumed use, operands, cardinality and data flow in original and
effective code, accounting for skip-original prefixes. Validate actual incoming transpiler IL on
every run; return it unchanged on failure. Do not repeatedly execute foreign transpilers to probe.
Tolerate benign foreign owners/edits; hashes and exact versions identify evidence only. Revalidate
supported later repatching or suspend safely. Atomic install failure removes only newly owned
patches. Emit one bounded startup warning per skipped enabled patch with package/patch/spec/target,
failed predicate, unavailable behavior and fallback; missing optional providers are quiet.

Owned Defs remain declarative/additive. Any external XML patch uses XmlExtensions.FindMod with
packageId=true plus actual content/shape guards; first XML Extensions use adds its required runtime
metadata. Preserve unrelated lists/filters, merge duplicate-free, no whole-list replacement here.
No source assembly, source Harmony or dev Gateway enters the package.

Original avilmask.microdesignations active withholds S096 new/resumed work and suspends unfinished
orders. Removal needs explicit Resume after admission, not auto-destructive resumption. Recover
already committed outputs once. S022/S023's separate actions do not receive a blanket package ban;
however an external order on their actual target must be reconciled by an admitted ownership
adapter or affected owned processing stays visibly blocked. Never remove/unpatch foreign owners.

## Risks / Trade-offs

- Arbitrary custom recipe callbacks can mutate before returning products → admit only proven
  effect/context/recovery contracts; no speculative execute-and-undo preview.
- More supported stack shapes expand authorization risk → use the already required shared portion
  rules and physical-unit isolation, with focused mixed-stack tests before advertising support.
- Planned orders can never become runnable → clearly separate missing conditions from unsupported
  protocols, show reasons and retain Cancel; visibility preferences cannot grant capability.
- Native recipe outcome variety is large → preserve native values and consequences, prove concrete
  corpse/chunk/equipment/mending paths, and do not claim universal provider support.

## Migration Plan

No runtime or data migration now. Future owned state loads inactive until identity and admission
checks pass; retain outstanding outputs and explicit suspension, clear obsolete claims. Foreign
billSaved jobs, dynamic Def names and global settings are not imported or aliased. Any future import
requires its own backup, atomic conversion/restoration and verified postconditions before rollout.

## Open Questions

MDR-Q1 exact native execution/transaction/target proof; MDR-Q2 providers, consumers and lawful reuse;
MDR-Q3 finalized recipe/worker/balance comparators; MDR-Q4 shared quantity/composition/retry cases;
MDR-Q5 seven-language UI, focused TDD, independent review and final native workflow acceptance.
These are later research/implementation work, not a request to explain existing mod behavior.
