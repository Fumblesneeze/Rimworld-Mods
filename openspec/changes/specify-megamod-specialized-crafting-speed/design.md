## Context

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target1.7 beta.
This is a documentation draft for S122. Read the [research](../../../mods/megamod/docs/refinement/2026-11-06-de-generalize-work-research.md)
for pinned source/native identities, inspected branches, consumers and DGW-Q1–5. No runtime edits
or proven compatibility follow from this design.

The source supplies three StatDefs and mostly XML transformations. Its small C# layer supplies
global compatibility settings and mode-conditional PatchOperations, not work scheduling. Native
recipe jobs already consume RecipeDef.workSpeedStat. Do not introduce a job patch for this behavior.

## Goals / Non-Goals

**Goals:** preserve task-specific speed modifiers and consumed Def names, classify effective
recipes correctly, compose the real VSE/FSF stat pipeline, expose honest restart configuration and
fail unsupported atomic groups without damaging unrelated data.

**Non-goals:** Work-tab splitting, hauling or caravan priority changes, new skills, trait content,
VSE expertise progression/passion replacement, stonecutting/tool additions, copied upstream code,
generic compatibility framework, cache framework or package/assembly impersonation.

## Decisions

### 1. Use declarative stats and native work consumption

Retain `SmithingSpeed`, `TailoringSpeed`, `SculptingSpeed` when admitted. Define explicit owned
StatDefs rather than adding an XML parent name to native GeneralLaborSpeed solely for inheritance.
DefaultBaseValue1, minValue0.1, PawnWork display and percent presentation retain the source;
GeneralLaborSpeed contributes once, with no second copy of capacity factors or WorkSpeedGlobal.
Preserve ordinary modifier evaluation and native explanations; no independent stat cache.

Use XML Extensions for required Def/node/package checks. This makes XML Extensions a required
dependency when implementation ships, with matching metadata/load order. Prefer specific existing
operations after inspecting their actual implementation. Do not invent operation class names here.
If inheritance-aware preflight or atomic commit needs a narrow owned XML operation, own and test
that operation rather than mutating resolved Defs in a startup C# loop.

### 2. Classify effective recipe inputs before editing

Native patch application precedes XML inheritance resolution. A raw selector demanding both fields
on one element is insufficient. Compute an immutable plan from the effective inherited values and
their actual provenance, then apply minimal scalar overrides at the appropriate recipe level. Keep
explicit custom speeds and child activity overrides. Validate parent lineage, conflicts and result
cardinality before any group mutation; refuse unresolved/ambiguous inheritance. Do not globally
change a parent in a way that silently remaps an excluded descendant.

| Incoming effective speed | Effective working activity | Owned result |
| --- | --- | --- |
| GeneralLaborSpeed | Smith / Smelt / Cook | SmithingSpeed |
| GeneralLaborSpeed | Tailor | TailoringSpeed |
| GeneralLaborSpeed | Sculpt | SculptingSpeed |
| VSE_TailoringSpeed, admitted Consolidated only | Tailor | TailoringSpeed |
| VSE_ArtSpeed, admitted Consolidated only | Sculpt | SculptingSpeed |
| Others | Any | Preserve |

Cover both RecipeDef and ThingDef.recipeMaker, including generated recipes. CookSpeed,
DrugSynthesisSpeed and native SmeltingSpeed are not GeneralLaborSpeed aliases. This source-derived
classification is an explicit assumption DGW-Q1, not a new guess based on product categories.

### 3. Keep conservative graphs narrow

Canonical optional package `vanillaexpanded.skills` gates the VSE adapter. Replace the actual
GeneralLaborSpeed edge, never every statFactors text node:

- VSE_ArtSpeed → SculptingSpeed;
- VSE_TailoringSpeed → TailoringSpeed;
- VSE_WeaponCreationSpeed and VSE_ComponentCraftingSpeed → SmithingSpeed;
- FSFBionicsCraftingSpeed → SmithingSpeed only with VSE and the admitted FSF provider.

Retain other independent factors, offsets, clamps, disabled-skill behavior and provider workers.
One existing equivalent intended edge is idempotent; repeated edges, a remaining duplicate general
edge or a path back into the child are not silently accepted. No blanket list replacement.
In Conservative mode, VSE expertise remains on its own child stat, intentionally multiplying with
the owned parent's modifiers. Neither alternative claims identical combined numeric results.

### 4. Consolidation requires a proven single contribution domain

Consolidated mode maps only admitted art/tailoring recipe cases and recognized modifiers into the
owned destination. Source BestGuess renames ExpertiseDef.statOffsets and selected HediffDef stage
statOffsets/statFactors, but deletes old stats and misses arbitrary external references. Retain
old VSE StatDef identities as compatibility observations of the effective owned value instead.

Use a narrow alias evaluator only if needed: its entire value, explanation and finalization path
must return the destination's finalized value without a second application of factors, capacities,
clamps, expertise, health or equipment. Do not implement aliases as mutually dependent statFactors.
An alias is not another independently boostable production stat. Known incoming modifiers must
be normalized once into the destination before alias evaluation. The destination's10% minimum
applies in Consolidated; Conservative retains VSE's existing child minimum0 and skill suppression.
Native work incapability/worker eligibility is unchanged in either mode.

Preflight the actual loaded contribution sites: ExpertiseDef offsets/factors, admitted HediffDef
stages, recipe speed consumers, independent statFactors and relevant equipment/trait references.
Preserve each numeric contribution's role. Existing source+destination entries require proven
merging semantics or rejection, not a blind XML rename producing duplicates. Do not widen to an
arbitrary recursive search-and-replace. Unknown stat workers/parts or runtime providers that can
add unaccounted contributions make consolidation unsupported until a narrow adapter exists.
DGW-Q2 blocks implementation acceptance of this path, not drafting its required behavior.

VSE's own patched StatWorker calls ExpertiseTracker.OffsetStat and MultiplyStat using StatDef
identity. Inspection of that code is evidence of an existing compatible patch, not grounds to
reject it merely because a Harmony owner exists. Any owned Harmony hook must document required
effective data flow, incoming transpiler validation and later repatch policy through foundation.
Prefer XML and an owned StatWorker where sufficient; do not patch native recipe work unnecessarily.

### 5. Scope settings to the actual lifecycle

Define one common setting `production.specializedCrafting.vseMode`, stable choices Conservative
and Consolidated, built-in Conservative. It is global-only with the concrete exemption that all
saves in a process share one Def graph. Use common load/store/validation/row rendering, disabled
Current save and Use global with a reason; no source DynamicPatch settings framework.

Track stored pending choice separately from startup requested choice and successfully active mode.
Source absence preserves a valid pending choice. Failed admission shows inactive plus reason,
not a false Conservative success. A change takes effect only after restart. Invalid unknown values
use Conservative with a bounded diagnostic. Source HeavyHand has no working selectable patch set
and is deliberately omitted. No automatic import of `compatibilityMode` or source save data.

### 6. Define atomic groups and real contract boundaries

| Planned target group | Minimum semantic admission | Failure/fallback |
| --- | --- | --- |
| DG-BASE: three stats and base recipe classifications | Native general-labor shape, unique compatible stat identities, resolvable recipe inputs, non-cyclic factor graph, safe full mutation plan | Preserve foreign/native data; no owned base remapping or partial stats. |
| DG-VSE: complete selected mode, including weapon/component edges | Active VSE package, consumed Def/type/member shapes, recipe and modifier plan; consolidated aliases proven if selected | Leave VSE integration unchanged, keep independent base mappings, visible inactive requested mode. |
| DG-FSF: bionics edge and any explicitly admitted health mappings | VSE admitted; exact FSF package plus consumed content, modifier semantics and unique nodes | Leave that optional adapter unchanged, bounded startup warning. If an unhandled FSF contribution invalidates consolidation, reject DG-VSE as well. |
| DG-CONSUMER: future narrow package-gated adapter | Proven active provider/content and user/provider enablement semantics | No resurrection of disabled content or package spoof; remain documented unsupported until proven. |

These are design IDs, not completed patch-target documents. Implementation must create one stable
target document per actual patch and declared atomic-group linkage, executable semantic validators,
tests and final evidence links. Any false/unknown predicate, validator exception or mutation failure
leaves no new owned partial edits; warn per skipped enabled patch with IDs, reason, lost behavior
and fallback. Explicitly absent optional providers are not failed enabled candidates.

For XML external targets use `XmlExtensions.FindMod` with packageId=true plus content/shape checks.
Admission must account for actual effective load/patch ordering, not only unmodified XML. Plan and
rollback the owned edits only; never unpatch or remove foreign data. Reinitialization is idempotent.
Hashes identify inputs/evidence, not admission. Compatible independent added factors are allowed.

If `Alias.DegeneralizeWork` is active, select it for this entire overlap before owned stat/patch
insertion; report the selection. If another provider such as Primitive Tools supplies compatible
stats, reuse only after validating the complete required group, including its actual recipe edits.
A partial collision must not trigger that provider's defective all-or-none fallback or our duplicates.
Its unrelated tool and stonecutting behavior stays outside ownership.

Additional Traits' MayRequire and conditional folder gates are real consumed package contracts;
equivalent names cannot reopen filtered content. Current1.6-only metadata and enable/disable
settings require exact1.7 analysis before a narrow adapter. Do not clone TraitDefs or bypass its
settings. Direct stat consumers can be preserved while package-gated parity remains unsupported.
No confirmed consumer justifies reproducing AliasDynamicPatch's public patch-operation framework.

### 7. Compose directly with other owned features

S013 bulk smelting retains SmeltingSpeed. S009–S019 bill browsing/counting/worker policy read actual
RecipeDefs and pawn stats; S121 uses the same native observations. Do not add another Thing index,
per-mod cache, bill counter or self-Harmony stack. This source does not change hauling or Work-tab
categories; correct earlier S135 cross-references that assumed it did.

## Risks / Trade-offs

- Raw inheritance/load order can misclassify children → immutable effective-input plan plus exact
  final-Def checks and native recipe workflows; do not accept source XML inspection alone.
- Consolidation changes balance and clamps → explicit225% versus200% example, named mode and
  source-derived baseline; preserve independent modifiers and document unavailable combinations.
- Alias implementations can double-finalize or hide provider contributions → whole-value and
  explanation equivalence tests plus active VSE runtime checks; no generic reflection framework.
- Old providers are installed but not proven on1.7 → absent/present/changed separate fixtures;
  do not represent package presence or decompilation as support acceptance.
- Faster recipes change time-based XP → preserve native rules, no unrequested compensation.

## Migration Plan

This draft ships nothing. Later implementation uses common global settings with neutral default;
no source mod settings/save conversion or user mod-list edit. After the declared restart, validate
the selected graph and expose active/fallback status. Disabling owned overlap stops future owned
startup transformations; no save cleanup or deletion of foreign serialized identities. Any future
migration requires its own backup, atomic restore and postcondition contract.

## Verification and Open Questions

DGW-Q1–5 remain in refinement. Unit tests cover pure classification/planning and settings resolution;
lightweight Def fixtures only in honest scoped Def tests. Actual XML inheritance, native/Core Defs,
active VSE/FSF and stat/quality behavior require fresh isolated RimWorld processes. No game test is
run in this documentation phase. Later use focused RED/GREEN, package checks and independent review,
then native bill orders with observed progress, consumed ingredients and completed products;
inspect stat/expertise explanations and shared settings in all required modes. Include benign
foreign additions, unsafe shapes, duplicate names, rollback, source co-installation and provider
absence. Retain reviewed-build identity, actions, before/after screenshots and cleanup evidence.

Final semantic balance review compares native general labor, native smelting and VSE art/tailoring
at equal workers/benches, with and without individual modifiers. Confirm no changed progression,
material cost, product quality, job reachability or worker priority beyond specified speed effects.
