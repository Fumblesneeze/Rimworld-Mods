## Context

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target **1.7 beta**.
S062 source inspection is recorded in [research](../../../mods/megamod/docs/refinement/2026-11-04-smart-medicine-research.md).
The selected source has 38 runtime files spanning care policies, medicine selection, stocking,
surgery, field beds and optional UI/Combat Extended adapters. This document specifies future design,
not deployed code. No source assembly, external runtime library or Gateway enters the product.

Current 1.7 already provides doctor-inventory medicine, holder-aware tending and per-pawn inventory
stocking. Extend these concrete paths before introducing new jobs. Source overwrite prefixes,
generated-local patches, query-time policy mutation and duplicated quantity caches are not a design
template. Existing specs remain binding, particularly S007 compact health, S021/S126 physical cargo,
S024 practical supplies/housekeeping, S042 self-tend permission, S049 refill limits, S055 food sharing
and S058 work order.

## Goals / Non-Goals

**Goals:** one scoped medical decision through eligibility, count, collection and use; contextual
entity orders; native-cost field work; one supply quantity per physical unit; minimal semantic
admission and independent failure groups; honest source compatibility and coverage.

**Non-goals:** perfect survival predictions, automatic emergency preemption between patients,
cheaper-equally-effective medicine optimization absent from current source, new medical content,
unrestricted dosing, globally altered suspendability, source identity spoofing or automatic imports.
Do not introduce a general treatment framework or parallel per-mod inventory database. Later drug,
priority-treatment and stabilization analyses must refine a combined direct policy before coding.

## Decisions

### 1. Separate saved orders from transient treatment decisions

Save condition care/rest exceptions against exact Hediff identities, individual surgery preference
against the pawn, and owned supply requests against pawn and Def identities. Native stock/drug
orders retain their native storage. Missing entries use the defined defaults; removed conditions
clear only their state. Unresolved optional Def references retain an unavailable selection where
recoverable, with no automatic replacement item. Owned transient job state records real targets,
quantities and current phase, not a second saved global medical-care value.

Build one ephemeral decision for the current patient/action from current care advice, exact selected
conditions, ordinary batching, medicine permissions and live candidates. Counts and actual
TendUtility application must consume the same allowed set. Revalidate before treatment without
mutating `Pawn_PlayerSettings.medCare`. A count/read query is pure. No source-style global
`__beep_beep_MinimalMedicineAvailable`, static Bill or phase flag spanning unrelated jobs.
Use explicit bounded call context if an unavoidable native callback needs it, with nesting and
exception cleanup; prefer owned direct calls over thread-global patch coordination.

Preserve native TendUtility quality/time/consumption, including self factor0.7, medicine max quality,
bed offset and special tend-all grouping. When an ordinary group crosses care permissions, partition
the actual group rather than just reducing fetched count. Include economy mode in compatibility:
None excludes minor conditions from medicated urgent batches; Lowest requires the same selected
lowest eligible medicine before sharing. The proposed urgent→care-tier→native order
is a deliberate correction, with separate patient selection unchanged. An exact NoCare general
patient plus explicit condition exception needs an admitted eligibility path as well as use/count;
no saved general-care escalation as a shortcut.

### 2. Reuse physical native holder transfers and shared cargo ownership

Native 1.7 WorkGiver_Tend creates target C for an external holder and JobDriver_TendPatient provides
CollectMedicineToils, TakeFromOtherInventory and ordinary pickup/finalize paths. Prefer these paths;
do not drop every inventory item at job creation. A secondary-source plan retains exact returned
split references and physical compatible stack identity. Equal potency is not merge compatibility.
Never clear forbidden flags, assume a reservation authorizes its whole stack, or transform a
spawned primary target into null. A job can select a smaller valid set than full patient need and
continue ordinary care afterward; no fictitious requested quantities.

Use existing shared S021 quantity roles and capacity budget for equipment, hands, personal supplies,
bill ingredients and medicine. Mark exact medical allocation once; completion, split, merge,
donation, cancellation and recovery must adjust real quantities in that same ownership system.
No full-Def keep flags or separate medicine carrying allowance. Releasing a medical reservation
does not erase a pending cargo destination or silently turn leftovers into personal stock.

Medicine candidates come from native listers and the shared map-owned Thing data used by Ctrl+F,
hauling, storage and ingredient search. Extend actual fields/consumers only: Thing identity/Def,
holder/map, quantity, minified inner identity and medicine attributes. Spawn/despawn/move/holder
transfer/split/merge/destruction and relevant metadata changes invalidate shared facts. Rebuild after
load and clear on teardown; never persist live references in a cache or touch Verse off-thread.
Reservation, doctor reachability, care, donor permission, capacity and deadlines remain per-use
decisions, not cache entries. Native fallback is required if shared facts are unavailable.

### 3. Make distance preferences distinct from emergency time estimates

Cell-detour preferences use `(Manhattan(doctor,supply)+Manhattan(supply,patient)-
Manhattan(doctor,patient))/2`; default6 for equal ground,12 for other donors. This preserves source
UI meaning without comparing tick path costs to cells. Actual eligible path costs order journeys;
failed routes are unavailable. Doctor/patient roles precede generic donor classification.
Additional useful sources are within8 Manhattan cells of the selected primary origin, bounded by
need/capacity. The radius is a documented behavioral constant, not a hidden evolving search horizon.

Emergency estimates use current doctor-to-patient and patient-to-bed routes and actual worker
carrying movement rather than source doctor-to-bed/0.6. Take the minimum supported fatal estimate;
do not return the first under625. Include the entire planned multi-stop route, holder transfers,
handling and one real tending duration in fetch safety; recheck before every optional extra pickup
and omit it if the combined itinerary no longer fits. Respect
unknown/unreliable provider physiology; an adapter cannot invent a prediction. Estimates influence
choice, not health or guaranteed rescue. Bound work to actual candidate selection, release pooled
paths and measure combined cost before any performance claim. Do not launch full searches per
frame or repeatedly on unreachable unchanged candidates.

### 4. Ground rest is a disposable patient workflow, not a free hospital

Prefer a native lying/resting path if it can provide the required player workflow. If a temporary
ThingDef is necessary, create an owned ID and ordinary sleeping-spot-equivalent stats; guard all
placement and cleanup and expose the native contextual cancel action. A failed placement yields no
invalid LayDown job. Associate one exact patient/owner, tolerate reservation changes, save/load and
interruption, and remove only the owned idle object. Reuse is safe only for the same current purpose;
never destroy another patient's current rest. Cleanup bound is1game hour, including rare-tick delay.
Do not create a lasting buildable/assignable bed or blanket surgery authorization.

The ordinary FeedPatient standing, hunger, faction and medical-rest checks remain; extend the bed
predicate only for actual supported field rest. After FinalizeTend, reconsider whether another
treatment or ordinary bed/rescue transition is allowed. Preserve valid drafted waiting through
required treatment; do not end unrelated player jobs. S024 cleanup must see remaining care first.

Deferred CE research only (outside initial implementation): stabilization would require an atomic separate adapter. Source `CE_Utility.CanBeStabilized`,
HediffComp_Stabilize.Stabilize, stabilization JobDef and ConceptDef are research leads, not certified
API contracts. The exact installed provider must prove candidate, medicine-holder transfer, quantity,
native action and result before acceptance. Source donor-inclusive inventory-only Find is not proof
the provider can use that donor. CE absent means no CE workgiver/DefOf activation. S080 later chooses
the combined stabilization behavior; no simultaneous owned/provider action on one dose/condition.

### 5. Consolidate 1.7 personal targets instead of running a second stock loop

Native Pawn_InventoryStockTracker persists group→(ThingDef,count); native DrugPolicy and pack-food
also retain quantities. Aggregate applicable per-item minimums with max, showing contributors;
don't append duplicate ThingDefCount entries and trust native first-match behavior. Use one actual
stocking decision per pawn/item. Integrate native setters/edit limits for native entries, owned
entries for extra catalogue items; no migration or silent rewrite of existing choices.

Target0 has no owned retention. Manual drop cancels only the owned contributor and visibly explains
surviving native/drug requests; explicit unload/drop still follows S024 protected/forced rules.
Copy is a detached value snapshot, not a live Pawn; validate complete paste before atomic assignment.
Partial UI updates roll back to previous values. Clearing/deleting source pawn does not corrupt the
snapshot. Save targets, not the clipboard or cached derived counts.

Scarcity is one map/item total of currently eligible physical supply divided by summed effective
spawned free-colonist targets. Count minified contents once, subtract job-committed quantities and
use the same shared facts as hauling/search. A denominator of0 creates no job; threshold0 disables
only scarcity. Real pickup still requires pawn-specific reachability, forbidden/reservation checks.
Return only owned unprotected excess to current valid storage. Avoid a return/reacquire loop by
using consistent total supply through transfer and the same effective target at both decisions.

No separate per-source 625-tick stale cache. Medicine-count alerts use current usable inventory facts
without reservations or side effects; retain the native alert when admission makes availability
uncertain. Native inventory-stock and drug-policy job admission must share the composed target and
medical-work deferral, otherwise a second stock loop could evade the rules. Disabling owned stocking
leaves native orders intact and stops only owned contributions/errands, with safe physical recovery.

Do not produce nutrient paste in a ThinkNode query. If an eligible real stock job dispenses it, use
the same native dispenser/ingredient/action path as gameplay, with real costs and cancellation.
Initially only physical existing meals count toward scarcity; threshold0 allows dispensing without
that gate. This conservative choice is explicit for review with S123, not an invisible virtual stock
estimate. Ingest buttons remain native actions; S124/S134 own future medical-drug policy.

### 6. Contextual UI and common settings

Use ordinary and owned compact Health rows for single/group choices and mixed-state indication;
native Health/Assign for individual surgery; Gear and the later S132 owned inventory view for supply
orders. No new main tab or developer-only real pickup. All source globals and the save-level new-pawn
surgery default use the shared inherited settings path. A committed new-pawn default initializes
an individual choice once; it never replays during generic ResetMedicalCare and overwrites a player.
No common-inheritance exception is needed. Translate all seven baseline languages; measure real
localized/narrow UI, and keep per-condition icons/details accessible across mixed groups.

The source NiceHealthTab translation cache option is intentionally omitted: it is an internal
optimization with no selected gameplay effect and can retain stale language text. Any future
translation cache must follow language lifecycle without a separate user-facing tuning checkbox.
Optional health adapters must provide access to ordinary controls if their custom surface fails;
an invisible native path is not an adequate fallback.

### 7. External contracts and co-installation

Original/Continued Smart Medicine active: disable owned S062 overlap at startup, preserve saved
owned orders as inactive and warn. Do not unpatch/reconfigure external providers. Native stock data
remains native; no foreign jobs or saved components are relabeled as owned. Gameplay outcomes are
separate from class/package drop-in compatibility.

| Contract | Plan / limitation |
| --- | --- |
| Compact Hediffs package-gated care delegates | Direct S007 calls within megamod. External Compact adapter must be owned/guarded and separately verified; a facade cannot satisfy `Memegoddess.SmartMedicine` presence without forbidden spoofing |
| Mod Medicine Patch assembly/old-method binding | No fake SmartMedicine assembly. Extended categories are unsupported until explicit semantic adapter; fail affected owned care closed, preserve remaining provider behavior |
| Dreadarm SmartMedicine V1 query in Reworked hauling | Different source identity, not evidence Continued exports V1. Owned retention uses direct shared code; no invented API alias |
| Pharmacist dynamic advice | Optional package-identified read of advice; validate type, signature, category semantics and lifecycle. Explicit condition choices override advice only for their scope |
| NiceHealthTab / RPG / CE Gear UI | Canonical package plus actually consumed methods/Defs; external UI shape failure isolates adapter. S132 integrates owned RPG view directly |
| Use Bedrolls | `Memegoddess.UseBedrolls` plus portable Bedroll existence; stocking does not implement use or rename provider data |
| Androids repair extensions / optional Medical table | Discover exact provider identity before support; no global type-only activation or XML content-only package assumption |

Installed optional provider gaps stay SM-Q3 rather than unsupported compatibility claims. A future
facade is justified only by a proved consumer that can actually discover it, with exact return/error
and save lifecycle. Never export all source types as a speculative compatibility shell.

### 8. Prospective patch admission and XML plan

Before implementation, split every actual Harmony/XML integration below into a stable target-spec
document from the mod template, with owned scenarios, validator, atomic group and test/evidence
links. The table is a design inventory, **not implemented checks or accepted target documents**.

| Candidate group / native seam | Minimal required semantics / failure boundary |
| --- | --- |
| Individual care eligibility/count/use: HealthAIUtility, Medicine.GetMedicineCountToFullyHeal, TendUtility grouping/application and TendPatient failure checks | Same exact condition set/permission reaches eligibility, count and actual treatment; no saved-policy mutation. Admit the dependent group atomically; method existence alone is insufficient |
| Rest exceptions: HealthAIUtility rest choice | Condition contributes specifically to elective rest, not global immunity/healing or urgent rest. Scope to rest selection; unrelated health use remains unchanged |
| Supply selection: FindBestMedicine, WorkGiver_Tend.JobOnThing, holder-aware collection and pickup/finalize | Actual onlyUseInventory argument controls intended source scope; target A/B/C/queue/count roles, splits, reservations and native consumption preserved. Changed argument use disables dependent supply enhancement |
| Surgery: WorkGiver_DoBill care, ingredient validation/collection | Actual selected Bill/patient/recipe and predicate flow; own-inventory ingredients reach physical native collection. No static Bill leak or broad all-bill drop rewrite |
| Field eligibility/continuation/rest/feed | Original/effective posture check, real selected doctor/patient, actual finalized tend callback, native LayDown cleanup and narrowly scoped InBed feeding branch |
| Stock admission: native inventory stock, drug retention, think tree and owned supply jobs | One effective target and actual physical pickup/return; doctor-ready deferral cannot be bypassed by a second native branch; lifecycle preserves exact personal/cargo allocation |
| Health/Gear/Assign controls | Exact pawn/group/rect/action scope, native row behavior, selected drop/ingest callback identity and permission; compatible extra icons/columns remain |
| Low-medicine count | Native base membership plus additional inventories counted once; failed supplement never hides native warning |
| Cleanup/invalidation observers | Callback exists with used identity/arguments; if original execution is necessary, account for skip-original prefixes. No needless exact-body check |
| Optional adapters | Canonical package plus consumed Def/type/member semantics and active-provider lifecycle; absent neutral, present incompatible warning and adapter-off |

For each seam inspect original versus effective input, relevant prefix/original-skipping behavior,
transpiler order and later repatch revalidation. Validate actual incoming transpiler streams on every
run, return unchanged on false/unknown/exception, and never execute foreign transpilers speculatively.
Tolerate unrelated instructions, locals, offsets and compatible owners. Hash/version identifies
evidence only. Install once; failures roll back only newly owned patches in the declared group.
One bounded startup warning per skipped enabled patch includes package/patch/spec ID, target,
failed predicate, unavailable behavior and fallback. Explicitly disabled or absent optional features
are not failures. Saved choices whose enforcement is inactive must be labeled unavailable.

Use Def/XML for declarative work: an owned temporary spot if needed, ordinary job declarations,
think-tree insertion and Assign/optional Medical column additions. XML Extensions checks required
Defs/nodes/shape before mutations, optional content also `XmlExtensions.FindMod packageId=true`.
Verify actual supported operation names at implementation, update required dependency/load order
in that same slice. Merge once; preserve unrelated entries/order, do not replace lists or force
TendPatient suspendable=false globally. Unavailable required declarative pieces disable their
dependent feature coherently; no partial column/job claim. Optional CE declarations are package-
and-content-gated rather than unconditional workgivers with absent runtime targets.

## Risks / Trade-offs

- More condition partitions can consume more medicine than native mixed batching → document actual
  costs, keep ordinary grouping within valid sets, test count/use agreement and review balance.
- Emergency prediction is approximate → never promise survival; preserve permissions, actual routes
  and work order, measure two-hour margin and handling of unknown physiology.
- Multiple native/owned target contributors are confusing → show them and their maximum explicitly;
  manual drop reports remaining requests rather than silently altering drug policy.
- Source mods load stale reflection contracts → no fake identity, explicit adapter admission and
  exact installed-provider tests; don't mistake load-order hints for compatibility.
- Candidate scans/pathfinding can become expensive → shared facts plus bounded action-time work,
  combined-consumer measurements, no speculative cache framework or optimization claims.
- Temporarily disabled care cannot enforce saved exceptions → visibly inactive state and bounded
  startup explanation; preserve remaining game behavior rather than silently interpret as Best.

## Migration Plan

No source import, source removal automation or user-save mutation in this documentation change.
Future implementation introduces owned state additively and preserves native controls/order data.
Unsupported saved choices remain visible; game load does not execute actions or claim successful
treatment. Any later source migration requires its own preflight, backup, atomic conversion,
restoration-on-failure and verified postconditions. Test outstanding source jobs/temporary spots
before advising removal. Disablement/recovery releases only owned work and retains real supplies.

## Open Questions

SM-Q1–4 in [research](../../../mods/megamod/docs/refinement/2026-11-04-smart-medicine-research.md)
track provisional tuning/UX, exact runtime target contracts, optional providers and later medical/
meal/inventory composition. Do not pause sequential specification to ask the user to explain sources.
Implementation still requires focused RED/GREEN, independent review, typed package checks, fresh
minimized native player workflows on the reviewed build and balance/playability review. Static
analysis or startup inspection cannot accept any of the above behavior.


## Initial support scope — decision2

Multiplayer and Combat Extended adapters, synchronized sessions, CE-only controls/properties and
provider acceptance are deferred. Any source CE/MP implementation details above are retained research
for a future scoped change, not initial work. Implement and accept the single-player non-CE contracts.
Ordinary save/load of colonies started with megamod remains required.
