## Context

S038 replaces Cherry Picker's broad content-selection behavior under one owner:
**Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Read the [proposal](proposal.md), both capability specs and the
[research inventory](../../../mods/megamod/docs/refinement/2026-11-03-cherry-picker-research.md).
The source has 45 catalogue types plus three optional families, six attributed patch classes and
one dynamic patch. It combines global settings, destructive shared-Def mutations and partial
restoration. Those internals do not satisfy our save-isolation and compatibility rules.

This is a design for later vertical implementation, not evidence that every required native seam
is already found. No runtime code, metadata dependency, patch or migration is added by these docs.

## Goals / Non-Goals

**Goals:** preserve the full observable catalogue and practical authored-preset contract; define
precise exclusion effects and scope; compose owned selectors/policies directly; prevent cost loss,
endless generation, ambiguous restoration and cross-save contamination.

**Non-Goals:** arbitrary Def editing, automatic original-settings import, universal interception of
foreign direct spawns, original package/assembly impersonation, copying upstream assemblies, new
bottom tabs or speculative shared-cache frameworks. No live compatibility or performance claim.

## Decisions

### Policy is data; finalized definitions retain their identity

Use stable owned content keys carrying resolved type/namespace, defName and source identity.
Resolve authored legacy strings separately; never use display labels or a simple-type-name-only
cache as identity. Missing keys remain serializable dormant choices. Guard malformed strings,
ambiguous types and absent providers independently so one bad member cannot abort the catalogue.

Maintain a catalogue of finalized native Defs and optional admitted adapters. Prefer native lists;
derive display metadata once per catalogue revision. Do not remove definitions from DefDatabase
or zero their shared nutrition/value/cost/component fields. Availability belongs at supported
selection, production and grant boundaries, preserving object identity for existing saves.
This replaces destructive mutation/snapshot restoration: a snapshot could overwrite another mod's
later changes and would still leak a policy across saves. No general-purpose Def mutation framework.

Per-save dynamic policy cannot be faithfully encoded as static startup XML. Use direct owned calls
and narrow runtime seams for that policy; reserve XML for declarative preset adaptation and any
future genuinely static definition changes. This is the concrete reason for C# here.

### One common settings path with deliberate application timing

Register individual Allowed and preset Active choices with the common load/store/resolve/row
renderer. Both inherit globally by default; retain dormant overrides. The displayed effective state
also carries all preset/individual reasons. Identity is stable across translations and sorting.

Capture an immutable applied policy at new-game/load boundary, after save overrides are available
but before policy-dependent generation or action eligibility. During play, edits change requested
settings only. Save/reload publishes the next policy; main-menu global edits affect the next setup
flow. Never publish a partial policy or use a previous game's snapshot after teardown. Define and
prove exact load timing in CK-Q1 before coding dependent hooks. If native initialization needs a
later availability phase, delay the affected behavior coherently rather than using global values
temporarily and generating content before overrides arrive.

Main-menu controls disable both save rendering and callbacks. No global-only exception is proposed:
source reliance on global mutable Defs is not an adequate reason to deny the user's save model.
Reload-only timing is an explicit proposed deviation to simplify consistent transitions and prevent
mid-job cost/output changes; it is not source parity. On reload, blocked saved jobs/order state must
stop before new output while preserving recoverable ingredients/progress without duplication.

### Exclusion reasons, not destructive preset expansion

Resolve each active preset through a visited-key graph, maintaining contribution provenance.
Duplicates are idempotent; cycles stop at visited nodes. Root activation, nested reachability and
individual exclusions remain separate. Deactivating a root recomputes contributions without
rewriting any member's direct setting. Bound diagnostics per invalid identity and show unresolved
members. Unavailable adapters contribute no claimed exclusion. Preserve provider list order for
membership inspection while effective membership is a set.

Legacy `DefList` semantics are adapted deliberately: unchecking a source removal-list row activates
its exclusion, but the owned UI explicitly labels preset activation. Its inverse does not reproduce
the source's broad removal of member keys. `TMM_CherryPicker_All` is therefore a group of exclusions,
not an unconditional reset of every independent user choice; make this deviation visible.

### Dependency behavior is explicit

For recipes, check every required ingredient group and ordinary product, plus fixed construction
costs. Keep original quantities, work, skill and required resources. Return blocked eligibility with
an explanatory reason instead of deleting costs. Already-created bills remain; S012 views do not
resurrect disabled recipes via cached lists. The explicit butchering exception omits excluded
byproducts while retaining ordinary allowed quantities; if no product remains, refuse that job.
Ordinary destruction still occurs but omits excluded killedLeavings without compensation. Other
special recipe paths require precise classification against these rules before admission.
Fixed scenario starting supplies are omitted with a setup explanation, without rewriting the
scenario definition or substituting goods; do not rely only on random ThingSetMaker filtering.

For research and VPE prerequisite chains, compute the surviving ancestors through excluded nodes,
union independent requirements and detect cycles. This is a policy projection, not in-place list
rewriting. Preserve completed progress. Source removal's implicit unlock effect is now explicit
in spec and UI. No free unlock on a missing or cyclic edge.

Generation chooses among valid ordinary candidates without changing relative weights. An impossible
required choice terminates with a bounded result. Do not substitute Colonist for a required faction
member, fabricate supplies, use extreme combat values or leave a rejection loop unbounded.
Existing-world state is not regenerated. Existing apparel wear/hediff/new quest restrictions are
kept as distinct action gates; existing learned genes/abilities and worn apparel are retained.

SpecialThingFilter hiding does not rewrite existing filters into a broader allowed set. WorkType
visibility remains separate from actual work eligibility. Treat unsupported hard-reference paths
as named coverage limitations, not blanket guarantees.

### Narrow authored-preset compatibility adapter

Actual installed consumer `Wara.toomanymods` emits `CherryPicker.DefList` nodes and patches their
`defs` children. Preserve defName, label, member strings, source attribution and the XML element name
for those XPath additions. Do not copy provider list contents or create duplicate legacy CLR types.

The inspected default 1.7 `DirectXmlToObjectNew.DefFromNodeNew` honors Class; the legacy parser does
not use it for initial type lookup. A proposed guarded XML adapter can retain the node's name and
set Class to an owned DefList implementation while replacing a **sole** original-package MayRequire
with an owned-compatible condition. Perform no transformation when the original is active. Keep
unrelated attributes and provider/DLC gates. Reject unsupported composite MayRequire/MayRequireAnyOf
logic instead of weakening it. An existing provider Class must be admitted or left untouched.

Use `XmlExtensions.FindMod` with `packageId=true` for external provider gates and
`XmlExtensions.Conditional` for exact Def/node shapes. Installed operation inventory includes
`XmlExtensions.PatchOperationAttributeAdd`, `PatchOperationAttributeRemove` and
`PatchOperationAttributeSet`; their actual schemas and failure behavior must be read before use.
The guessed name `XmlExtensions.AttributeSet` is not an established operation and must not be used.
Validate all required attributes before changing any; preserve/recover original nodes on failure.
Retain provider patch ordering: no rename or removal before patches targeting the original node.
An adapter must prove the active parser's Class behavior and provider additions, not just presence
of the parser type. Unsupported legacy mode remains inactive with a warning and honest UI status.

This requires XML Extensions (`imranfish.xmlextensions`, 2574315206) and its Harmony dependency in
the eventual package. Both are external required dependencies, never bundled. Original CP, preset
providers and DLC remain optional. External code asking whether `Owlchemist.CherryPicker` is active
will still receive false. Early original-only package-gated patches, hard metadata dependencies and
compiled assembly/type consumers are not satisfied by this adapter. Record these limits instead of
inventing a facade. S020's external scrollbar patch stays attached to the real original; owned
selectors need no such API emulation.

### Startup admission and failure isolation

Create a stable target document per **actual patch**, linked to its scenario, validator, tests and
eventual native evidence. The following groups are an implementation inventory, not pre-approved
targets or a license to share one vague validator across multiple patches:

| Group | Minimal semantics to establish before implementation |
| --- | --- |
| Catalogue/application lifecycle | Finalized inputs, exact active DLC/provider identity, loaded-save overrides before decisions, teardown and setup flow boundaries; observe-only hook needs only the actual callable contract unless original execution matters |
| Ordinary generation | Exact candidate/weight/required-choice use for stock/rewards/resources/pawns/spawners/weather/events; rejection cannot cause endless retry; preserve prior exclusions |
| Production/construction | Product and required ingredient/cost flow, saved job resumption and native refusal reason path; no costs/ingredients consumed after rejection |
| Research/prerequisites | Current dependent and ancestral query semantics, independent requirements, invalid-chain handling and progress retention |
| Apparel/health/quests | Wear's actual garment argument, AddHediff's condition argument and state-changing body, quest-root use in add/letter paths; method signature alone cannot prove those argument semantics |
| Appearance/social/UI | Current selection routes, random versus explicit interactions, work-column versus job behavior, hidden filters versus their acceptance predicates, native tab/selector safety |
| Optional providers | Canonical package first, current types/members/Defs second; active content, not merely installed content; VEF/VPE/Ancients isolated from Core paths |
| Authored-list XML | Provider package and exact nodes, parser behavior, original absence, unchanged dependency semantics, provider member additions, all-or-none attribute update |

Inspect original versus effective methods and any relevant prefix/transpiler ordering. A compatible
foreign patch or unrelated body edit is not grounds for rejection. If a prefix changes an argument,
validate the consumed data flow and required control-flow context. If an original can be skipped,
do not count its body as executed evidence. Every transpiler validates its actual incoming stream
on every run and returns it unchanged if incompatible; do not invoke foreign transpilers repeatedly
as speculative probes. Support later repatch invalidation/revalidation.

On false/unknown predicates, validator exceptions or install failures, keep the affected patch or
declared atomic family inactive and roll back only newly installed owned hooks. Log one bounded
startup warning per skipped enabled patch: package/patch/spec ID, target, predicate/reason, missing
behavior and fallback. Explicitly disabled candidates and absent optional providers do not emit
failure spam. Do not mark a row excluded if its essential boundary is inactive. Retain requests for
later compatible startup. Hashes in research identify evidence, never dictate runtime admission.

### Coexistence, composition and data ownership

Original CP active: select original, disable owned content application and XML adaptation; preserve
owned settings and emit bounded warnings for enabled owned patches skipped by that declared policy.
Never unpatch foreign owners. Original mutation may already have changed native data; do not claim
owned preservation guarantees while that provider is selected.

Compose S020 selectors, S029 drug-catalogue reconciliation, S012 recipe browser, S014 whole-list
workbench linking and S036 Architect directly. Content blocking preserves S014's shared bill identity,
recoverable started work and quota accounting; no completion/credit is issued for a blocked job.
No self-patches. Prepare policy publication and the dependent drug-row reconciliation together.
Original `Mlie.DrugPolicyFix` suppresses owned S029 maintenance/presentation under its existing
contract. It enumerates unchanged ingestible/drug-category fields and cannot see this separate
availability policy. Therefore withhold the **drug-item exclusion group** when that original is
active or S029's maintenance is not admitted; retain requested reasons and show unavailable status,
without disabling unrelated item families. Do not claim cooperative original-provider support.

For admitted owned maintenance, capture prior affected policy rows, prepare all relevant policy
results against the candidate drug catalogue, then publish rows and applied drug choices together.
If application fails, restore prior rows and leave that drug exclusion group inactive before player
actions resume. This is cross-feature orchestration around S029's per-policy atomic repair, not a
second maintenance implementation or foreign mutation. Tests must fail on a later policy to prove
earlier prepared/committed results do not leak a partial application. Successful publication causes
one coherent reconciliation; stock scans and later normal loads cannot resurrect excluded rows.
S136 future floor sampling must respect current terrain eligibility without a second list of rules.
Physical Thing data remains map-owned and shared with search/hauling; exclusion is an availability
predicate, not deletion from the physical index. Inspect native listers first. Do not scan maps per
content row or allocate one cache per port. Clear policy/catalogue views at their respective
game/mod-list lifetimes; never persist live references or read Verse/Unity objects off-thread.
Benchmark combined consumers and invalidation before promising any optimization.

## Risks / Trade-offs

- **Breadth of 45+3 families** → implement one observable vertical slice at a time; no "complete
  Cherry Picker replacement" claim while any promised family lacks reviewed native evidence.
- **Source behavior changes existing items** → intentional preservation deviations are explicit;
  source fidelity does not require reproducing zero-nutrition, free-cost or lost-component defects.
- **Reload-only application is less immediate** → display pending state and effect timing in every
  relevant view, including global edits affecting inherited saves. No undocumented restart fallback.
- **Exclusions can remove all progression/threat choices** → explain dependencies, block impossible
  requests and preserve valid alternatives; do not impose an invented balancing multiplier.
- **Fixed supplies and incidental drops are intentionally reduced** → show omitted starting supplies,
  preserve allowed yields without compensation, and refuse empty-output butchering. This is content
  exclusion, not a claim that every acquisition leaves total resource yield unchanged.
- **1.6 original and optional dependencies on 1.7** → source support labels are not compatibility;
  exact absent/present/changed-provider evidence and semantic admission are still required.
- **Legacy authored contracts cannot all be emulated** → narrow real data adapter, documented
  unsatisfied checks and no package spoof or duplicate source CLR type.

## Migration Plan

S068 composition clarification: thought acquisition exclusions do not remove existing memories or
their types from the fading catalog. Retained memories continue under current fading/expiry policy;
catalog rows distinguish applied acquisition restrictions, pending requests and fading support.
Use the applied S038 snapshot, not pending settings, in native acquisition decisions.

Native counselling reports success after a void TryGainMemory call, which can be refused. Direct
owned composition must correlate actual acquisition and report no granted bonus for refused
Counselled/Counselled_MoodBoost awards, without replacement compensation. If truthful reporting
cannot be admitted, an admitted native action guard refuses the affected counselling action before
cost/cooldown/effects. If neither route is supported, do not activate the coupled exclusion group:
retain requested choices, expose unavailable status and log the skipped patch/group. Never call an
exclusion applied while permitting a false granted-award report. Other content groups stay independent.
Exact targets/admission and reviewed-build native action evidence remain future work under S068/MF-Q2–4.

No automatic import or foreign-file write. Owned settings use stable keys, default inheritance and
dormant missing entries. Source `keys` strings are documented for a possible future opt-in migration,
which needs its own preview, backup, atomic write, verified postconditions and failure restoration
contract before implementation. Reverting the feature leaves existing physical entities intact and
owned requested choices dormant; it must not force other mods' restrictions back to defaults.

## Verification and Open Questions

CK-Q1–6 in the research and refinement ledger cover exact native seams, XML/provider bindings,
proposed timing/dependency behavior, optional integrations, future migration and native acceptance.
Use Unit for key/graph/policy calculations, Harmony for individually owned shape/admission tests,
Defs only for scoped constructed fixtures, and fresh isolated games for real loaded databases,
XML, providers and selection/generation/UI workflows. No fake active mods or copied Core Def state.

Retain RED/GREEN before each implementation; package/dependency/localization checks; independent
review; then personally observe actual reviewed-build UI/job/generation consequences through the
minimized Gateway workflow. Direct setup/spawning is precondition evidence only. Save switches,
blocked bills, existing-food consumption, native wear, quest offers, work columns and composed views
must be exercised rather than inferred from patch presence. Root config/PID/cleanup evidence applies.
Final balance pass checks research, acquisition, empty required sets, resource conservation and
unsupported-provider playability. Documentation validation cannot close those tasks.
