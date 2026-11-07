## Context

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**, S099/P2-28.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-spine-manipulation-research.md) pins complete
source/selected binary and native 1.7.4998 capacity code. Source is one Harmony postfix: multiply
PawnCapacityWorker_Manipulation.CalculateCapacityLevel's result by native Spine tag efficiency,
passing the caller's optional impactor list. No custom spine class, XML, settings or entity state
exists in the selected source, despite an imprecise historical Workshop comment. Nothing implemented.

## Goals / Non-Goals

**Goals:** one anatomical factor, exact native stage/order, current explanatory contributors,
ordinary work consequences and independently admitted compatibility.

**Non-goals:** changing part HP, new paralysis injuries, altering Moving, replacing all capacity
math, retuning implants or bad back, adding race/job permissions, per-job penalties, settings,
private health caches, source binary/API impersonation or a new medical UI.

## Decisions

### Add one factor at the body-based capacity boundary

The inspected native worker computes manipulation limb efficiency with digit weighting 0.8 and
multiplies it by current Consciousness. The outer PawnCapacityUtility.CalculateCapacityLevel then
applies hediff/gene offsets and post-factors, maximum limits, minValue and rounded hundredths. It
also bypasses the worker when zeroIfCannotBeAwake applies. Patch/adapt the worker result, not the
final public capacity value: the latter would multiply later offsets and alter existing balance.
Preserve the outer `result > 0` branch; do not invent a positive-offset rescue when zero body-based
capacity normally skips those modifiers. A later nonzero provider minValue remains its own rule.

Compute the current Spine tag efficiency through the admitted native helper with maximum
float.MaxValue, default interpolation and bestPartEfficiencySpecialWeight = -1. The selected DLL's
IL confirms the six-parameter call, even though C# decompilation omits the default final argument.
Do not derive the factor from Moving, which includes legs, pelvis, breathing, pumping and its own
consciousness handling. Do not copy the manipulation limb formula into a replacement worker.
Multiply the existing admitted result once and preserve unrelated compatible contributions.

Static XML cannot express this dynamic anatomy-dependent capacity relationship at the required
stage. A guarded narrow Harmony adapter is justified; no new static body-part/hediff mutation or
XML dependency is proposed. Any later declarative XML change must separately use XML Extensions,
package/content/shape checks and merge preservation, with the dependency declared at first use.

### Retain native anatomy semantics without race-name lists

CalculateTagEfficiency iterates the body's tagged records, obtains native part efficiency and uses
their arithmetic mean when the special weight is -1; no records returns 1. Thus absent anatomy is
neutral but a missing existing part still contributes its native zero. Do not enumerate only
GetNotMissingParts or require the English label/defName Spine. No colonist, humanlike or faction
filter exists in source; actual manipulation capability and admitted anatomy determine scope.

Native CalculatePartEfficiency handles added ancestors, missing parents, direct added-part
efficiency, HP loss and stage partEfficiencyOffset/partIgnoreMissingHP. Preserve those semantics,
including efficiencies above 1; do not substitute raw remaining HP/maximum HP. BadBack changes
capacity offsets, not spine part efficiency. Core BionicSpine has default partEfficiency 1; its
betterThanNatural flag alone does not make the factor exceed 1. Installed RBSE has bionic/advanced
spines with efficiency 1 plus later Manipulation offsets of 0.10/0.20; do not multiply those offsets
into the factor or add them twice. These are comparators, not promised whole-provider compatibility.

Ordinary tagged custom parts need no hard-coded provider package list. A provider that changes
capacity calculation or helper semantics needs a narrow canonical-package adapter with complete
callable/semantic checks. Unsupported models fail only their independently identifiable contribution
scope. Do not infer that a differently named body has no spinal function when the native tag says it
does, or apply the multiplier to a custom capacity worker with unknown meaning.

### Preserve native cache, explanations and direct owned composition

PawnCapacitiesHandler caches final values and native health changes mark capacities dirty. Reuse that
lifecycle; no parallel per-pawn capacity or Thing index. Any extra invalidation hook discovered to be
necessary requires its own semantic target spec and causal native acceptance. Clear/recompute only
through the supported health path, without evaluating recursively through final Manipulation while
calculating its worker. No saved multiplier/impactor list, map scan or off-thread Verse access.

Normal calculation passes no impactor list. HealthCardUtility.GetPawnCapacityTip requests one,
recalculates through the same capacity path and deduplicates hediff/body-part entries for display.
Contribute native part-health/hediff impactors when requested, preserving existing entries. Stage
owned impactors temporarily and commit result plus contributors only after successful finite,
nonnegative evaluation; an adapter failure must not leave half an explanation or a changed result.
Valid zero is retained. Invalid NaN/infinity or an exception in the owned read leaves the existing
result/list intact with a bounded diagnostic; do not suppress unrelated native exceptions.

S007 compact health and later RimHUD consume the same native result, not separate recalculation
policies. S045/S046 supply their admitted Consciousness result, including their ordinary application
timing and limits; this factor consumes it once. S097 reads actual MiningYield, which can depend on
Manipulation. Do not insert a separate spinal criterion into its worker selection or directly edit
mining speed/yield. Other jobs similarly retain their existing stat weights and caps. Direct owned
health composition replaces any temptation to retain a self-patch stack.

No player configuration or global/save setting exception is needed. Injuries and implants remain
native pawn state. Use existing localized contributor labels, plus seven-language owned compatibility
messages where surfaced. No new raster assets, tab, debug command or bottom-right control.

### Semantic admission and single application

Canonical original package `jdalt.smmn` suppresses owned overlap. No original unpatching or repair of
its startup behavior, even if its legacy version would not run in this beta. Actual equivalent native
or provider behavior at the same stage also suppresses the additional factor with a clear status.
Do not infer equivalence from a method name, unchanged sample result, assembly hash or generic owner
presence. Verify the effective semantics or decline the affected contribution; benign unrelated
patches remain permissible. No external callable consumer was found in the bounded scan; the source
method is public within an internal type, so a speculative facade is unwarranted.

Candidate contracts below are not yet implemented target documents:

| Candidate | Minimal required semantics | Fallback |
| --- | --- | --- |
| SPM-T1 manipulation worker contribution | Correct pawn/HediffSet and optional impactors; returned limb/consciousness contribution before outer modifiers; no already-applied equivalent spinal factor; compatible effective foreign changes | Leave existing worker result and impactors; report unavailable/duplicate behavior |
| SPM-T2 spinal efficiency read | Actual tagged anatomy, mean/absent/missing/added-part semantics, valid current values and optional native contributor collection | No invented zero/neutral value for unknown data; leave owned result inactive in affected scope |
| SPM-T3 optional lifecycle/UI hook, only if needed | Correct health invalidation or capacity explanation consumer, no recursion or stale split value | Retain native lifecycle/UI and decline unsupported owned group |

Create stable per-patch documents and executable checks before code. Check original and effective
paths, including skip-original prefixes, result-changing postfixes and relevant transpilers. The
postfix name/signature alone cannot prove its input still has the required meaning. Do not depend
on no other patches, exact whole IL, local numbering or hash/version matches. Any transpiler must
validate the actual incoming stream on every execution and return it unchanged on failure; do not
speculatively rerun foreign transpilers. False/unknown predicates, validator exceptions or install
failure leave the coherent contribution group inactive, rolling back only new owned patches.
Later repatching requires invalidation/revalidation. Emit one bounded startup warning per skipped
enabled patch with IDs, target, failed predicate, unavailable behavior and fallback; no per-health-
query log spam. Ordinary anatomy data must not trigger a global disable merely for being modded.

Core and Harmony are required when implemented; source About omits the Harmony dependency despite
actual binary use, which must not be copied. Optional body/organ providers remain optional and
absent-safe. No source or Gateway assemblies/assets ship. The author explicitly reserves reuse
rights; independent implementation of the described behavior is the selected approach.

## Risks / Trade-offs

- Final-capacity multiplication changes direct offsets → keep the worker-stage boundary and test
  positive/negative offsets, factors, ceilings and zero-body behavior.
- Lost spine confused with spineless anatomy → retain the body's tagged records and native missing
  part evaluation, including multi-part anatomy and replaced ancestors.
- Another provider already models spinal control → admit a known single composition or defer;
  never square a factor based on guessed compatibility.
- Work effects vary by consumer → preserve actual stat weighting/caps and measure real workflows,
  rather than promising a universal speed percentage or adding extra job patches.

## Migration Plan

No custom save state, body Def, injury or setting is added/imported. On loading an existing save,
normal health calculation uses its current injuries and implants. Removing the owned contribution
returns to the remaining capacity behavior without deleting native health state. No source namespace
or save identity is impersonated. Future import or provider rewrite would need a separate contract.

## Open Questions

- **SPM-Q1:** exact effective worker/helper/outer-modifier admission, equivalent-provider detection,
  original coexistence and lifecycle support on the target beta.
- **SPM-Q2:** finalized anatomy/implant/modifier comparators and optional multi-part/ancestor models;
  actual work/stat effects and interactions with owned consciousness/mining rules.
- **SPM-Q3:** honest focused TDD, native/current explanation and seven-language proof, measured cost,
  independent review and final native injury/work/operation/save/provider acceptance. All runtime
  tasks remain unchecked; source availability does not grant code/art reuse rights.
