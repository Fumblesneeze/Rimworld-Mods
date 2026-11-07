## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
S058 SmarterDoctors (Continued) is a1.6 reference; selected binary and native1.7.4998rev1299 were
inspected separately. [Research](../../../mods/megamod/docs/refinement/2026-11-04-smarter-doctors-research.md)
records identities, all source behavior, native seams, discrepancies and consumer limits. This is a
design draft, not executable patch-target documentation or implementation acceptance.

## Goals / Non-Goals

**Goals:** one owned decision for all eight automatic work choices; distinct urgency; current native
eligibility and batching; family-level fallbacks; bounded shared/native data access; honest provenance.

**Non-Goals:** global medical scheduler, automatic outside-faction rescue, treating existing mod
priorities as permissions, new menus/settings frameworks, per-port Thing caches, source identity
facades, unsafe global Def mutations, or implementing pending medical-source decisions now.

## Decisions

### 1. Use explicit ordered comparisons, not enormous additive scores

Derive a short-lived decision snapshot from the actual workgiver's eligible candidates. Use ordered
keys matching SW02–08: shared urgency and finite comparable fatal deadline before routine tending
class/minimum disease gap/blood-loss time; feeding current
empty-food malnutrition severity; next-trainable category and retained/completed steps; fertility,
hay and distance; plant-cut hay; frame kind; rescue pawn kind. Preserve native tie behavior after
horizontal squared distance. Never use NaN/infinity/missing state as an urgency value. Uninterpretable
required data abandons the owned comparison for the selection, not the patient/candidate.

Do not copy source10^20/10^10 float magnitudes: they erase useful differences. Native Thing selection
also uses Mathf.Approximately whereas cell selection uses exact equality; any chosen integration
must prove the observable comparator survives both. Prefer a narrow comparator/selection seam or a
bounded per-decision ordinal projection with exact ordering and preserved ties. Final seam choice
requires SD-Q3 inspection of the effective build; do not build a parallel scheduler or repeated
full-map sort framework merely to mimic source base-class postfixes.

Medical membership follows current HasImmunizableNotImmuneHediff semantics consistently (visible,
natural immunity, not fully immune, not injury/missing part). Calculate minimum immunity−severity
over that population. That population supplies routine disease ties only. The separate shared urgency rule uses native
life-threatening indication or reliable death prediction below the ordinary18-hour urgent window,
with actual relevant-action eligibility. Never infer fatal timing from immunity gap alone.

Trainable identities map Tameness, Haul, Rescue, Release, Obedience, Comfort, then other; use native
next trainable and validated step access, not a hard dependency on optional Comfort. Category order
is lexicographic, so huge modded step counts cannot cross categories. Definition-only settings are
not needed for this automatic source behavior; future configurable choices use the common system.

### 2. Integrate at selection while keeping eligibility and job creation native

Native JobGiver_Work respects work-type/priorityInType ordering, forced targets and existing queues.
S058 alone does not sort across workgivers or interrupt current jobs; S134 uses the shared urgency
policy across its rescue/tend candidates under its own documented readiness contract. Keep HasJob/JobOnThing/JobOnCell and native
reservation/reach/allowed-area/medical/food rules authoritative, rechecking mutable conditions at
use. Native training may first fetch food; native harvest anchors create local distance-ordered
batches. The owned ranking must not rewrite either lifecycle. S006 growing choices remain intact.

S060 [construction access](../specify-megamod-construction-access/design.md) now supplies a direct
within-category order: local blocking-frame preference and neighbor count before ordinary distance.
Keep SW07 category first, current enclosure/worker eligibility authoritative, and delivery separate.
This is one owned comparison, not a non-neutral foreign patch or a second sort of the selected job.

Do not patch rescue HasJobOnThing from false to true. The source's stale copied logic drops native
1.7 exclusions, substitutes40cells for current25cell automatic threat distance, leaves ShouldSkip
inconsistent and can reverse foreign vetoes. Deliberately retain only rescue ordering. A future
safe expansion needs its own eligibility, scanning, job creation, childcare and explicit scope
contract; it is not a hidden adapter in this change.

Native/non-neutral foreign ordering is authoritative unless an explicit adapter exists. Inspect
effective priorities once for the selection; if any external non-neutral result participates, leave
the entire provider ordering untouched. Do not compare foreign floats against owned ordinal bands,
or preserve only some targets' scores. No default prohibition on other Harmony owners. Avoid calling
foreign hooks twice as a probe. Lifecycle/data changes invalidate the transient choice normally.

### 3. Use native listers and live facts; add no parallel cache

Spawned patient/animal/downed listers and native grower/building/frame candidates already define
the populations. Use the actual map.fertilityGrid; the source's new wrapper per cell is unnecessary.
Pollution and building fertility remain current. Harvest hay classification uses the actual plant,
sowing the currently requested crop, through native plant-to-grow settings including buildings.

Keep snapshot ownership on the game thread and bounded to one selection. No serialized scores or
long-lived pawn/Map references. If profiling later demonstrates shared derived facts are needed,
use established map-owned data contracts and document freshness/invalidation/teardown rather than
introduce an S058 cache. Benchmark total selection/eligibility cost with native and composed users,
including large animal colonies and growing areas; no performance claim based on fewer allocations.

### 4. Guard each work family as an atomic integration

Before implementation, create/index actual target-spec documents with stable patch IDs, executable
predicates and owning SW scenarios. Candidate groups: SD-TEND, SD-FEED, SD-TRAIN, SD-SOW,
SD-HARVEST, SD-CUT, SD-CONSTRUCT, SD-RESCUE. Each includes every hook needed for both comparison and
participation in that selection; do not install priority evaluation without its corresponding
selection behavior or leave a half-modified fallback.

| Target responsibility | Required semantic evidence |
| --- | --- |
| Native work selection and candidate comparator | Current selected workgiver, its validated target set, distance/tie meaning, result-to-job flow, unchanged forced/work-type paths |
| Tending and feeding facts | Current qualifying health population, immunity/severity units, finite blood-loss prediction, food level/malnutrition and authoritative job eligibility |
| Training | Native next wanted/trainable choice, step meaning/access, optional identities and unchanged training/gather-food execution |
| Growing and cutting | Current map/cell/plant identity, effective fertility, grower-building support, normal sow/harvest/cut eligibility and batch creation |
| Frames and rescue | Correct target kind/buildable classification/pawn status and remaining native eligibility/job creation |

Method names alone cannot admit argument/return rewrites: validate the relevant incoming data flow
and uses. Conversely a pure observational callback requires only the callable shape it actually
consumes. Allow benign IL/local/offset changes and compatible owners; hashes identify evidence only.
Validate effective code, skip-original behavior and relevant current/transpiler effects. A transpiler
validates its actual incoming stream every run and returns it unchanged on unsupported/ambiguous
shape; do not execute foreign transpilers speculatively. Supported later repatches invalidate or
revalidate the affected group before it can influence another choice.

False/unknown/exception/install failure disables that group's newly installed hooks and restores
its unmodified path, removing only owned additions. Initialization is idempotent. Log one bounded
startup warning per skipped enabled patch with package/patch/target-spec ID, target, predicate,
unavailable behavior and fallback. Runtime unknown candidate data uses selection fallback; new
unsupported content diagnostics are bounded, never one message per candidate/tick. Independent
admitted groups continue. Shared-seam failure disables all groups depending on it, not half of them.

### 5. Compose existing and later work policies directly

S024 care-before-cleanup is a gate, not another medical score. S042 self-tending initiation and S056
emergency surgery remain separate; S055/S057 food selection remains downstream of patient selection.
S021/shared food quantities retain physical ownership. S062 Smart Medicine, S124 Drug Response and
S134 Priority Treatment uses SW02 urgency across rescue/tend, then family and within-family ties.
This user-selected composition supersedes source disease-first and blanket rescue-first ordering. S060/S061 construction research
must distinguish frame selection from obstruction/material/mining order. Do not self-patch megamod
or implement two schedulers that race. Record any changed observable precedence before code.

### 6. Co-installation, dependency and presentation boundaries

Case-insensitive active package checks for `zal.smarterdoctors` and original `maarx.smarterdoctors`
suppress all owned S058 groups, explaining source ownership without modifying it. No package spoof,
foreign unpatch or source configuration/save import. Bounded installed search found catalog entries
only; no proven executable consumer justifies a reflected facade. Reinspect before any drop-in claim.

No settings, Defs or new art selected; existing contextual UI stays. Harmony becomes a required
dependency with first implementation, absent-safe DLC/content remain optional. No XML operation is
selected because selection is dynamic. Any later XML mutation uses guarded merge-preserving XML
Extensions and declares it required. Product never depends on other repository mods or Gateway.
Translate any new player explanation in all seven baseline languages.

## Risks / Trade-offs

- Imperfect fatal forecasts can affect care order → explicit urgent/nonurgent and comparable-deadline
  rules, no invented disease forecasts, current rechecks and final medical playability review.
- Concentrated training can leave animals untrained, and hay/floors/traps can wait indefinitely under
  sustained higher-priority work → preserve source intent and explicit orders; measure real colonies
  before claiming playability; no invented aging/fairness policy.
- Prioritized global scans may cost more than nearest-target scans → use native data, bounded
  snapshots, focused combined profiling; preserve fallback and avoid duplicate eligibility calls.
- Candidate eligibility can change during preparation, travel or another pawn's work → native claim,
  reach/job checks remain decisive; fresh selection rather than stale reservations.
- Refusing unsupported foreign ordering can reduce S058 coverage → report limitation honestly;
  specific tested adapters can define a combined policy, not broad owner-count bans.
- Source is1.6 and beta is moving → semantic current admission plus final exact-build native runs;
  research hashes cannot stand in for compatibility.

## Migration Plan

No foreign state migration or new stored feature state. Enabling changes future automatic selection;
disabling restores remaining game selection without clearing jobs, training, health or plans. No
normal save/config mutation during research. Later implementation uses focused RED/GREEN, owned
package checks, independent review and minimized isolated native acceptance with retained evidence.

## Open Questions

SD-Q1–4 are tracked in the [refinement ledger](../../../mods/megamod/docs/refinement/specification-progress.md).
Resolve medical policy and actual provider integration before code; retain omitted rescue expansion
as an explicit decision. Host calculations and source inspection cannot accept actual worker behavior.
