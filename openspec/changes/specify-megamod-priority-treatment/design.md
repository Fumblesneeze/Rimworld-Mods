## Context

Sole owner Make Rimworld Great Again (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
Documentation only. [Research](../../../mods/megamod/docs/refinement/2026-11-07-priority-treatment-research.md)
pins Dreadarm0.1.22, complete16-file assembly inspection, public guide/API and native contracts.
Use the [observable contract](specs/megamod-priority-treatment/spec.md) for behavior.

## Goals / Non-Goals

**Goals:** ready emergency responders, honest native treatment, one S124/S134 dose authority,
quill automation with player holds, narrow startup admission and shared data.

**Non-goals:** optimal medical triage, direct healing, automatic arbitrary surgery, broader faction
rescue, hardcoded prescriptions, original API/binary/save compatibility checks or facades,
source settings migration, a second patient cache or patches against owned code.

## Decisions

### One medical decision path

Use native map pawn/bed/bill listers and the shared Thing candidates where supplies are queried.
Do not duplicate source per-doctor full-lister drug scans or cache pawn-specific eligibility.
Recheck current doctor readiness, access, reservations, care, recipe and dependency at scheduling
and execution. Bound work to actual decisions; profile candidate and update cost together before
claiming performance gains. Clear derived state on map/game teardown and rebuild after loading.

Separate emergency permission (priority1 and DoctorReadiness semantics) from ordinary medical-bill
scheduling (enabled Doctor work of any priority). Readiness never itself dispatches work. Use the
shared urgency classification from S058 across all eligible rescue/tend candidates before family
ordering; equal urgency uses rescue first and then the remaining family ties. S062 supplies field
care for the selected casualty. A treatable life-threatening disease is eligible even when the
routine Include sickness option is off. Preserve active care and deliberate orders. An admitted
external ordering must satisfy this combined contract; otherwise withhold owned emergency selection
and warn, never mix incompatible foreign scores or claim urgency-first while retaining rescue-first.
S042 owns self-tend permission, S062 medicine/field rest and S080 stabilization; compose directly.
Automatic self-care can yield while explicit self-care stays protected.

Use native Rescue/TendPatient/DoBill jobs, sleeping-slot reservations, ingredients and effects.
The source's special same-bed reservation fallback cannot create a free slot. Verify the actual
native shared-bed sequence before retaining the capability; no fake bed or reservation override.

### Operations and shared orders

PT03 adds no injury predicate or new operation veto patch. Use current native operation eligibility
at planning and execution, including actual capacity and ordinary risks. Scars are not a categorical
ban; injured doctors can perform drug/quill operations if otherwise eligible. Avoid a patch solely
to emulate the source's rejected restriction.

S124 DR09/10/13 owns pending dose, reasons, provenance, player takeover, holds and actual exposure.
Contribute an S134 dependency reason, not another bill queue. Keep per-chemical exclusion, suspended
foreign/manual blockers, 15-bill limit, current policy allowedForAddiction, operation inventory
limits and real consumption. No general S124 scheduled-interval exclusion on this producer.
Reason records must include exact patient, chemical/drug and current operation identity. Availability
must include both recipe availability and the current pawn, not just the source's AvailableOnNow.
Nearest eligible supply replaces native lister insertion order deliberately; do not reserve a
specific physical stack forever or take protected supplies beyond S062's operation contract.

Quills use the same small order-ownership/hold lifecycle with a quill-removal reason, not chemical
dose history. Native Recipe_RemoveHediff removes all visible matching quills, targetsBodyPart=false.
Deleting source bills would immediately requeue; selected patient hold deliberately corrects that.
No speculative generic surgery-automation framework. S137 recipe discovery is strictly separate.

### Configuration and UI

Common stable settings under health.priorityTreatment: initialEnabled=true, includeSickness=false,
wakeDoctors=true, allowMeals=true, dependencyAssistance=true,
quillRemoval=true. Share load/store/resolve/rendering and save lifecycle; new save inheritance,
dormant override and disabled menu callbacks follow PT07. Store the initialized operational switch
with the save separately from its default. No source settings-key import. Patient status/Resume
extend S124 Health surface; quill hold is a separately named patient action. Diagnostics, if needed
for development, stay in the top debug toolbar and never become a player-facing healing tool.

### Target-spec families before implementation

Create stable per-patch target documents and executable validators at implementation time:

| Planned family | Minimal semantic prerequisites | Atomic fallback |
| --- | --- | --- |
| PT-EMERGENCY-SCHEDULE | Humanlike constant think-tree target and child list exist; additive owned node admits only native interruptible jobs and respected player/medical work; native rescue/tend callable semantics and reservation effects remain | Withhold owned emergency scheduling, leave native scheduling |
| PT-DEPENDENCY-ORDER | Current chemical need/dependency, policy, recipe, patient eligibility and native Bill_Medical lifecycle satisfy shared S124 atomic order/consumption/cancellation admission | No owned automatic dependency order; manual care remains |
| PT-QUILL-ORDER | Odyssey package and specific quill/recipe present, whole-pawn visible removals, current work/ingredient/anesthesia/effects match admitted semantics; ownership/cancellation surface available | No automatic quill order; native menu remains |
| PT-SETTINGS-STATUS | Exact native contextual callbacks, save identity and read/write roles survive; data missing is visibly unavailable | No stale controls or implicit save mutation |

XML scheduling integration must use inspected XML Extensions existence/node guards and an additive,
idempotent operation; preserve all sibling entries. Declare required XML Extensions in the same
runtime slice. Optional XML content requires FindMod packageId=true plus actual Def/node guards.
No whole think-tree/list replacement and no runtime mutation as a substitute for XML.

Validate semantic operands/dataflow, not whole method hashes or absence of foreign owners. Check
effective prefix/transpiler behavior where original execution matters; do not rerun foreign
transpilers as probes. Any owned transpiler validates actual incoming instructions every run and
returns them unchanged if unsupported. Support revalidation/invalidation on later repatching.
Apply groups atomically, rollback only newly owned patches, report each skipped enabled patch once
at startup with package/patch/target/spec/predicate/behavior/fallback. Explicit off is not failure.
Optional content absent is normal inactive scope; present but incompatible enabled paths warn.

### Source research and ordinary feature disablement

Active canonical dreadarm.prioritytreatment triggers a bounded startup warning and the ordinary
per-feature settings disable path for owned S134 overlap, best effort. Use the same effective off
behavior, pending-reason withdrawal and safe physical completion as a user disabling the feature.
This is not a compatibility guarantee or a separate cooperative scheduling mode. Source public API
Dreadarm.PriorityTreatment.Api.V1, ContractVersion2, exposes bool BlocksSurgery(Pawn,Thing,bool),
MatchesEmergencyPolicy(Pawn), CanRespondToEmergency(Pawn). They are read-only predicates, not job
dispatch or universal permission. The documented binder checks package, assembly, API type,
ContractVersion>=1 and exact signature, caches delegates after startup and uses neutral fallback.
These are research facts only; no binder/consumer-parity scan, source API facade or source save/job
compatibility test is required. Do not spoof package identity. Native medical bills and owned
pending/hold state retain their normal save-continuity obligations. S124 uses its ordinary settings
disable path for detected original overlap; it does not coordinate with source schedulers.

## Risks / Trade-offs

- Priority1-only preemption can surprise checked-priority users → explicit label; do not silently
  rewrite priorities. Ordinary dependency/quill work remains available at other doctor priorities.
- Urgency estimates can be incomplete → shared explicit native-danger/finite-deadline classification,
  no invented disease clocks, current rechecks and honest fallback. Verify actual care outcomes.
- Injuries alter actual capability/risk → preserve those rules while removing the categorical veto.

- Source advertises safe removal/general compatibility → neither claim is adopted or made an
  original-provider verification obligation; owned save continuity remains required.
- Concurrent/manual/foreign operations → common authority, current rechecks, preserved ownership.

## Migration Plan

No automatic source-settings/save migration. Deploy future code with only owned new saved state;
default missing fields to inheritance/no hold, and reconstruct pending identity without creating
bills. Never clear recognized player holds on load or disable. Any later source migration requires
separate backup, atomic application, failure restoration and verified postconditions.

## Open Questions

PT-Q1 inventory is resolved for specification. PT-Q2 records the selected urgency-first policy and
actual-capability operation admission; PT-Q3's source consumer/save parity work is superseded and
out of scope; PT-Q4 effective target/optional Def admission;
PT-Q5 shared lifecycle/performance; PT-Q6 reviewed implementation, translations and native acceptance.
No user interrogation is needed to continue the sequential documentation work.
