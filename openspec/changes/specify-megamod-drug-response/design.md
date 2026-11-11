## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target1.7 beta.
S124 is a specification-only replacement of Mlie.DrugResponse. Public documentation/PDF, pinned
source, matching installed1.6 binary and narrow native1.7/S134 extracts are recorded in
[research](../../../mods/megamod/docs/refinement/2026-11-06-drug-response-research.md).
No runtime proof follows from offline decompilation. Source paths below refer to that retained input.

## Goals / Non-Goals

Goals: one comprehensible response editor, truthful dose history, one owned dose decision for shared
medical reasons, real supplies and consumption, ordinary player authority, semantic compatibility.
Preserve separate provenance for S124 and S134 while composing internally through owned calls.

Non-goals: new drug content, curing arbitrary conditions, replacing tending or ordinary policy,
automatic amputations/implants, S134's complete rescue/doctor specification, S137's recipe-filter
implementation, automatic source-save import, binary/package spoofing or multiplayer support claims.

## Decisions

### 1. Separate configuration from patient state

The source MainButtonDef/1100×764 window is overwhelmingly global-for-this-world configuration.
Move it into common settings instead of recreating its bottom tab. Four pain selections and intervals
are regular shared setting definitions. Represent each condition's complete two-slot rule as an
independently scoped value in the common settings path, keyed by stable condition identity. The
visible list is the union of relevant global and save-local/dormant identities, with unavailable
content retained. Do not clone/freeze the entire collection when one rule changes.

An explicit empty override means disabled in this save; absent override means inherit, including
future global additions. Global deletion resolves inherited rows to the built-in empty rule and
does not erase independent or dormant save overrides. Current-save Clear all creates empty overrides
for currently configured conditions only, with its future-inheritance semantics in the confirmation.
Use-global restores current global values while retaining the dormant local bundle; first override
seeds the current effective bundle. Use one common definition/serialization/resolution/row renderer,
with global and save rule controls together for each condition. No parallel settings framework.

The source has no patient-specific policy. The proposed added Health hold/status is contextual
patient gameplay state; it is not a Use-global setting or replacement for S032's native drug-policy
editor. General availability/source catalogs may be shared; active selection, search and scroll are
window-local. Language sorting must not change stable execution order. Reuse S007/S062 Health
composition directly. S137 can eventually use actual recipe-effect facts, but a UI match is never
an automatic-treatment authorization.

### 2. Typed rules and facts, not source string encodings

Replace semicolon/comma records and static WorldComponent state with save-owned typed rule values,
patient holds/history and pending references. Configuration globals remain in common global config;
inheritance/overrides remain with the current save. Retain unavailable stable identities and labels,
with no arbitrary name-based substitution. Do not deserialize source DRSettings/MSPainlessData
types or copy source cachedMinor/Serious/Intense/Extreme references across saves.

Rule matching uses current visible condition instances, deduplicated by condition type for dosing.
S062 per-instance No-care and full immunity remove individual reasons, not unrelated instances.
Pain thresholds, ordinary dose size, source waits/ranges, anesthesia and effect thresholds remain
explicit in the observable spec. Exact1% and80% behavior is corrected in text, not rounded display
comparisons. A missing band stays None rather than guessing a lower-tier substitute.

Baseline condition Def names and optional candidates are fully inventoried in research. Optional
catalog adapters bind actual canonical package IDs and each Def shape before implementation; do not
reuse ModLister.HasActiveModWithName or a single sentinel for a whole catalog. The special MSMental
exclusion needs its concrete provider binding. Unknown external types require a new admitted mapping,
not string similarity. The publicly documented MSPainDrug.ManagesPain marker remains source
research. Do not implement or audit that legacy XML/API identity for replacement compatibility.
Use actual effect facts and any explicitly owned pain-eligibility metadata; genuine optional drug
content still needs its package/Def/effect checks, not a facade or original-consumer parity scan.

### 3. Declarative setup, shared map facts and direct native actions

Use guarded XML for comp/Def marker additions if needed, never source-style static runtime Def
mutation. XML Extensions becomes required with first use; choose actual supported operations,
FindMod packageId=true for external content, individual target nodes, additive duplicate-free merge,
no replacement of comps/extensions or unrelated lists. Missing optional packages are inactive, not
failed enabled adapters; present incompatible content gets bounded diagnostics.

Inspect existing native listers and share S033/S021 map candidates, stack quantities and claims.
Use S062 MC07's operation boundary for administration: ordinary operation supply plus admitted
surgeon inventory with its switch, not MC05's tending-only patient/other-colonist/animal donor
extension, a private drug cache or the patient-based source FindDrugFor. S062's explicit Surgery
uses best choice still owns operation medicine ceilings. Own-inventory self-use and caravan stock have their native scope;
do not quietly add map donors to self-use. Do not reserve all available stock at rule evaluation.
Take a bounded real claim when making a job, recheck before transfer/consumption and release on
failure. Personal stock and cargo metadata remain S021/S062-owned. Explicit source takeExtraIngestibles
is omitted because ordinary shared refilling already owns that quantity and capacity decision.

Native RecipeDefGenerator's Administer_<drug> convention is evidence, not sufficient admission.
Resolve recipes from patient-compatible available recipes, inspect actual worker/ingredient/effect
shape and reject ambiguous incompatible substitutes. Native Recipe_AdministerIngestible consumes
through Ingested, not ConsumeIngredient; watching only the latter would miss completion. Preserve
actual duration, costs, thoughts, provider effects and native drug history. A bill creation return
value and startup marker cannot prove a successful dose.

Both condition slots use bills for enabled on-map prisoners, including a configured first-slot
self-use route. Keep that explicit source rule alongside the distinct caravan route. S086's Medical
go-juice classification affects current categories and action-specific permissions; do not add a
universal manual-use veto to every response route.

S085 owns complete fractional quantity/effect/risk handling. A new S124 response requests one full
dose equivalent; one physical remainder is not one dose. Recipes/collection unable to handle the
needed fractional input exclude it instead of promising an impossible whole dose. Observe actual
committed positive exposure, including on interruption, and record its quantity and last time once.
The same active action can finish only its existing budget; after cancellation, no automatic new
order/restart before interval expiry. A player hold and consumed interval are independent: Resume
clears only the hold. Do not enable S085 paths with unresolved risk/history schedules or reinterpret
existing leftovers as whole stock after admission loss. This adds no alternative pharmacology.

### 4. One pending-dose authority across S124 and S134

Keep this small and local to medical responses: patient identity, exact drug, optional chemical,
route, originating rule reasons, actual job/bill identity and player-controlled/owned status.
S134 supplies an eligible dependency/addiction request; S124 supplies configured requests. Native
and foreign pending work is inspected conservatively and never adopted for cleanup. Suspended bills
still block equivalent scheduling. A patient+chemical claim prevents source-style duplicate recipes
using different drugs for the same chemical. Independent drugs can queue within native15-bill limit;
execution rechecks effects and stock. This is not a generic colony task scheduler.

Priority: preserve current/player work; consider eligible S134 unmet-chemical assistance; then S124
pain, then stable condition identity and slot. S134's actual doctor/bed/capacity eligibility and
ordinary surgery risks remain its responsibility; no blanket injury/scar veto is selected. No speculative priority conflict with unrelated
mods is assumed. S124 scheduled-drug suppression does not block S134's separately authorized
incapacitated dependency assistance. S124 intervals do not redefine native dependency progression.
S134's full change must reference this same shared result and verify its own quill/rescue scopes.

Use existing native ingestion records where adequate and a semantically admitted successful-dose
observation for exact timing. All doses of the same actual drug update S124 drug-history comparison;
an owned pain dose also updates its shared pain interval. Do not start timers at scheduling. Recheck
all permissions/reasons immediately before unconsumed use; time expiry never itself grants a dose.
Source75% effect comparison and native overdose deferral are limited guards, not a prediction engine
for arbitrary mod pharmacology. Unsupported effects stay honestly labeled; provider effect adapters
require their own contract and failure behavior.

### 5. Cancellation, control transfer and lifecycle

Record whether a withdrawal is automatic cleanup or actual player cancellation. Player deleting/
suspending an automatic-origin dose or overriding its self-use establishes a patient drug/chemical hold across
both producers. Explicit Resume is contextual; it does not resume a suspended bill or change drug
policy. Player edits/reorders transfer ownership, leaving that operation untouched on recovery,
while retaining automatic-origin provenance solely for later cancellation/hold behavior. Never
retroactively mark a wholly player-created or foreign bill as automatic-origin.
Automatic failure does not produce a player hold. A dose loses invalid reasons individually and is
withdrawn only when none remains. Do not interrupt a native irreversible consumption callback or
attempt to undo an ingested item. Admission for reliable cancel/consume observation is atomic with
scheduling; do not enable only the producer and leave guards absent.

Save only stable identities and necessary history/holds/pending relationships. Reconcile against real
loaded jobs/bills before new scheduling. Clear runtime map indices on teardown, rebind on load;
never persist live object references in a global cache. Map-to-caravan entry must reconcile any
unconsumed map pending action before a caravan claim; retained medical bills cannot be ignored as
permission to dose twice. The source's ThingComp.CompTick may not supply caravan ticking on1.7:
inspect a native caravan needs dispatch, preserving its membership/capability ordering. Calls to
IngestDrug alone bypass important caller checks. Unsupported incapacitated caravan route waits.

### 6. Actual patch targets need minimal executable semantic admission

These are research groups, not already implemented target specs. Before code, create each actual
patch's stable target document and test its exact relevant effective semantics:

| Group | Required semantics / failure boundary |
| --- | --- |
| Map opportunities and eligibility | Discretionary work versus forced/queued work, drafting/sleep/medical duties, actual drug rule trigger; missing guard disables dependent S124 scheduling |
| Successful consumption and native history | Distinguish attempted order from actual one-unit ingestion for self-use, administration and caravan; observe once, preserve provider effects and current job ownership |
| Pending bill/job creation, edits and cancellation | Actual bill identity, player gesture vs automatic removal, suspended-equivalent detection and ownership transfer; no duplicate callback or erased manual bill |
| Supply/recipe admission | Patient-compatible ordinary recipe, doctor source access and shared reservation/physical quantity checks; no Administer-name-only or patient-reach-only test |
| Caravan dispatch | Real membership and ordinary use capability, night rest, stable transferred history/holds and carried-item ownership |
| Health UI/common settings | Real selected patient, row/operation gestures, common scope callbacks, no swallowed S007/S062/S137 interaction |
| XML/provider catalog | Canonical package, each required Def/node/effect shape, merge/idempotence and independently unavailable rows |

Original/effective streams and skip-original/transpiler interactions matter. Compatible foreign
observers and unrelated edits pass. No hash/version/no-Harmony-owner admission; hashes only record
inputs. A transpiler validates its own incoming stream on every run and returns it unchanged on
failure. Late repatches need supported invalidation/revalidation; no speculative replay of foreign
transpilers. An atomic group's failure rolls back newly owned patches only and emits one bounded
startup warning per skipped enabled patch with spec/patch/target/predicate/behavior/fallback.

Active original Mlie.DrugResponse, pelador.drugresponse or Dreadarm.PriorityTreatment triggers a
bounded startup warning and the ordinary per-feature settings disable path for owned overlapping
automatic medication scheduling, best effort. Use the existing disable/withdrawal semantics and
shared pending authority, not a source-specific handoff or alternate recovery path. Unrelated
features continue through their own settings. Do not promise coordinated operation with the
original or test its API/binary/save parity. Never rewrite its settings, unpatch it or adopt its bills.

## Risks / Trade-offs

- Conservative source-like off/None defaults → player explicitly selects medication; no automatic
  luciferium or newly added-drug use from an empty rule collection.
- Successful-dose timing intentionally differs from source → retain pending claims so delayed bills
  cannot be repeated; include manual/native consumption and failed action cases in focused tests.
- Per-condition inheritance needs explicit empty versus absent values → common scoped rows,
  predictable global additions/deletions and clear-all semantics; never freeze the collection.
- Source immunity-on/75% effect guard may waste medication or miss complex risks → treat as explicit
  proposed policy, preserve actual side effects, measure native/provider cases before acceptance.
- Manual cancellation needs distinguishable native gestures → no scheduling if ownership/cancel
  guards cannot be admitted; one patient hold prevents requeue loops and remains visible.
- Generic effect inference cannot prove a selected drug cures an illness → do not advertise that;
  S137 surgery discovery is separately specified and never authorizes automation.
- Old source license and continuation license differ → independent reimplementation, provenance
  retained; resolve actual reusable-file rights before any reuse rather than copying assemblies.

## Migration Plan

No automatic upstream migration or external-state mutation. New owned data initializes independently.
Existing native bills remain ordinary player/provider-controlled bills. Disable/re-admit only owned
unconsumed work with preserved items/history/holds. An eventual import would require its own explicit
backup, atomic conversion, failure restoration and verified-postcondition contract. Removing the mod
mid-action remains unverified; do not claim save-removal safety from the author's advertisement.

## Open Questions

DR-Q1: drafted UI, pending real usability/localization. DR-Q2: observed overlap and proposed shared
contract, pending full S134 review. DR-Q3: exact1.7 dispatch/consume/cancel/recipe/optional XML targets.
DR-Q4: actual pharmacology/interval/immunity and caravan incapacity playability. DR-Q5: actual
genuine optional content package identities and license provenance; earlier source extension/API
consumer-parity work is superseded and out of scope. DR-Q6:
player-edit/cancel ownership across providers/save/map/caravan. DR-Q7: reviewed-build native UI,
real consumption, genuine content-provider present/absent/drift and composed performance evidence.
