## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S021 now uses **Pick Up and Haul - Continued**, `Zzyxzz.PickUpAndHaulReworked`, Workshop
**3829549663**, as primary reference, with Reworked **3813653998** and Armful **3808100512**
as comparison inputs. The [selected composition](../../../mods/megamod/docs/refinement/2026-11-03-hauling-composition-decisions.md)
reconciles PH-Q5 using those audits and the narrow modern While You're Up intersection. This is a
specification draft, not implemented behavior or native acceptance. Full S125 remains separate.
[Historical research](../../../mods/megamod/docs/refinement/2026-11-02-pick-up-and-haul-research.md)
on **Mehni.PickUpAndHaul / 1279012058** pins public
source, installed 1.6 assemblies/XML, native 1.7 capacity code and actual consumer bindings.
Its installed source files and selected package inputs match public checkout
09f1fac96e70cc2a93d647615a5abd37734a3eba. Main DLL SHA-256 is
CDAA18CD402A4CD7F9E3381F63EC000A6693440F34420B864C4FDD6CA04C86C6. These fingerprints identify evidence,
not runtime compatibility predicates. MIT provenance is recorded; independently implement behavior.

That historical source uses the pawn's real inventory with a HashSet<Thing> comp, not a second physical inventory.
It applies mass limits normally, but then can queue a separate native hand load; native MassUtility
does not count hands. Source CE helpers are stubs and replace the pickup clamp when name detection
activates them. Reference-only merged-stack recovery can mistake personal supplies for cargo.
Those findings motivate coherent capacity and quantity tracking, not a blanket upstream bug claim.
Choose an owned inventory-based physical model with exact saved quantities, native transfers and
one combined capacity check. Continued informs worker/search defaults, Reworked quantity/recovery
rules, and While You're Up grouped delivery and constrained opportunities. Armful's required visible
stacks (S126) render real cargo without adopting its multiple independently limited hand stacks.
No upstream assembly, arbitrary stack-count multiplier or whole private patch stack is adopted.

## Current reference evidence

The [Continued audit](../../../mods/megamod/docs/refinement/2026-11-03-pick-up-and-haul-continued-research.md)
supersedes the old defects as descriptions of that fork: it isolates personal/cargo stack merging,
removes same-Def recovery substitution and implements reflective CE weight/bulk accounting and
loadout protection. However, its 1.3 default capacity factor (up to 2), separate ordinary remainder
job and permissive missing-per-pawn CE fallback do not meet our actual combined allowance.
Preference-gated recovery and unchecked drop-result removal remain source-level risks. Its static
candidate buffers are not the required shared item index. All 17 C# files were inspected, with
selected installed 1.7 bodies and exact XML/keyed matches; no live verification is claimed.
The [Reworked audit](../../../mods/megamod/docs/refinement/2026-11-03-pick-up-and-haul-reworked-research.md)
adds all 44 decompiled behavior/API/support files and public guide/API evidence. Its quantity ledger,
physical recovery and native retention are useful alternatives. It still adds independent hand
capacity, supports humanlike colonists rather than Continued's broader workers, and introduces
different search, merging, loading and recovery behavior. New API identities are not legacy parity.
The [Armful audit](../../../mods/megamod/docs/refinement/2026-11-03-armful-haul-research.md) adds all
17 source files and selected installed bodies. Native job reuse and visible hand stacks are useful
alternatives, but independent full per-stack limits, source-private caches and incomplete failed-drop
recovery cannot supply our combined allowance/data/lifetime rules unchanged. Its selected visual
subset has a separate [S126 change](../specify-megamod-visible-haul-cargo/).
The [modern While You're Up intersection](../../../mods/megamod/docs/refinement/2026-11-03-while-youre-up-hauling-intersection.md)
shows legacy private bindings present by name in Continued, absent in Reworked/Armful. It replaces
unloading with grouped destinations but reintroduces a same-Def recovery guess. Owned composition
must preserve exact quantities rather than recreate those private bindings. Neither name matching
nor static analysis proves live compatibility. Source-specific risks and mismatches stay in research.

## Goals / Non-Goals

**Goals:** useful multi-item trips, physical conservation, one load allowance, supported native
storage and workers, direct urgent/storage composition, protected personal supplies, current shared
data and graceful recovery. Retain documented options through the common settings mechanism.

**Non-goals:** globally rewrite vanilla heavy-item hauling; grant inventories/training to animals;
copy source job/cache/private hooks; automatically import foreign saves; provide every legacy binary
binding; claim CE/cross-map/custom-storage compatibility without proof; implement this draft now.

## Decisions

### Current capacity is evaluated at each mutation boundary

Use a small owned capacity calculation fed by actual native/provider values, not a larger container
or source-style whole-stack estimate. Mass budget is native MassUtility.Capacity minus actual gear,
inventory and hand mass, with hands included exactly once. Native default is BodySize * 35; use
runtime values, not that number as a replacement for patched behavior. Volume budget is current
StatDefOf.CarryingCapacity minus inventory-plus-hand quantities times actual VolumePerUnit.
Worn/equipped gear affects mass only. This extension of the native hand-volume model to all inventory
is the explicit PH-Q1 proposed interpretation; it remains in the observable spec for review.

For each positive finite unit mass/volume, take the minimum of source quantity, requested quantity,
current free-mass floor, free-volume floor, per-transfer stack limit, reservation entitlement and
destination allocation. If a provider adds bulk, slots, nested-container accounting or another real
limit, its validated fit result further constrains the count. Do not replace the count with an
external result that can increase it. Avoid double-counting nested contents already included by a
provider stat. Unknown nested-capacity semantics disable affected batching, not guessed recursion.
Nonpositive/NaN/infinite measurements fail the candidate safely; zero space never means unlimited.

Recheck at plan creation, before SplitOff/transfer, after partial acceptance and before extending a
load. Atomic movement between own inventory and hands does not add capacity. Reduced capacity stops
new additions and permits unloading existing physical cargo, even if now overweight. Ordinary native
single-load fallback can still move a heavy corpse that exceeds inventory kilograms, but only after
owned cargo has been unloaded. Do not inject a global restriction into unrelated vanilla hauling.
For owned jobs, no leftover stack is added as an independent hand load above the combined budget.

### Shared candidates, local finite plans and destination commitments

Use native listerHaulables as the first source. The shared map-owned Thing subsystem used by Ctrl+F,
buildable counts and other consumers owns reusable physical facts: identity, map/holder/location,
type/material/quality/quantity, storage/stack compatibility and revision. It does not cache a pawn's
permission, reservation or job result. Candidate enumerations can use a shared spatial view where
measured reuse justifies it. Do not copy/sort the entire native list twice per request or retain a
static skipCells/skipThings across callers as the source does.

Construct a local finite route snapshot, starting at the explicit seed, with radius max(12, seed
delivery distance * 0.5) and nearest next candidates from the last accepted pickup. The 12-cell
minimum and 0.5 factor are source-derived named constants and specified behavior, not new arbitrary
settings. Use stable identity tie-breaking. Remove tested candidates from this plan, stop on no
positive progress or capacity, and do not recursively queue a newly generated collection plan before
unloading. Zero-cost/invalid item measurements and changing spawns cannot create an endless trip.
Current native reservation, forbidden/access, reachability, work capability, destination and forced
seed rules remain the action gates. Additional pickups use non-forced eligibility. Respect actual
cart/holder and active bill claims; unavailable ownership information excludes affected candidates,
not every ordinary map item.

An owned S125/S024 opportunity supplies its allowed quantities, destination constraints, parent job
and whole-trip travel bounds to this same planner. Those are additional restrictions, never replaced
by the ordinary nearby-search allowance. One parent job owns at most one pending detour; pending
production/material decisions remain authoritative. When parent work becomes invalid, return to the
ordinary scheduler while preserving safe cargo delivery/recovery. Full S125 thresholds, filters and
its explicit work-bound equal-priority exception are now in
[the opportunity contract](../specify-megamod-opportunistic-hauling/specs/megamod-opportunistic-hauling/spec.md).
That exception is request-scoped, not permission for ordinary equal-priority batching; no inherited
temporary storage-priority mutation is needed.

One destination budget accounts for native space, accepted items, existing stacks and active incoming
commitments. Keep stack compatibility distinct from a multi-stack shelf's ability to accept different
types. Use exact map/cell/container identities, not cell coordinates without their map. A route reserves
only the quantities and spaces it actually allocates; release unused allocations after partial pickup,
interruption or failed delivery. Native reservations remain authoritative, including foreign owners.
Do not reserve an entire shelf unnecessarily where supported native stack/quantity claims suffice.

S005 linked storage shares filters and priority but each member has its own physical constraints.
S049 will provide its per-type group refill/hysteresis and storage-wide stack ceiling directly to this
same budget, including incoming committed counts. Native fallback for an owned request must retain
those limits, not escape them through another work giver. Until a required custom destination policy
can be read, use only a proven ordinary provider route or withhold that destination. No broad
IHoldMultipleThings emulation or unconditional extra stack count.

Invalidate/requery spawn/despawn, split/merge, destruction, holder transfer, quantity/material/state,
filter/priority/group/capacity edits and reservations before the next relevant decision. Clear map
facts on teardown, reconstruct after load and keep Verse/Unity access on the owning thread. Saved
routes are not authority. Benchmark combined search/count/hauling plus invalidation/memory overhead
on representative maps before claiming an optimization; no performance target is invented here.

### Cargo quantities, personal ownership and physical conservation

Use owned saved per-pawn cargo/kept records associated with actual physical Things and quantities,
not a copied foreign comp schema. This can live in the owning game state and track off-map holders;
no blanket ThingDef comp injection is selected. Record successful transfer results, including the
surviving merged stack, not the pre-merge SplitOff reference. Partial rejection returns unaccepted
units to their real source/held location without duplicating or deleting them. Keep transfer state
local and recoverable if a callback fails partway through; verify postconditions before committing
logical cargo changes.

Track cargo and explicit kept portions when they share a stack; neither may exceed physical count.
Splits/merges require actual quantities and stack compatibility, not same Def. Known removal actions
reduce the affected portion. For an unqualified removal from a mixed identical stack, reduce cargo
first, then any remaining removed units from personal quantities, clamping to actual survivors;
this preserves the maximum still-possible kept portion. Explicit release/drop/keep commands target
their selected portion instead. This attribution is a proposed policy; add observable examples before
implementing it if it changes another consumer's contract. Unknown transfer semantics preserve personal
items and expose unresolved cargo; never repair by choosing any same-type object or UnloadEverything.

Unloading uses current storage, portions bounded by native hand/stack/destination acceptance, and
ordinary native movement/place actions. Mark delivered only after physical acceptance. A 3-tick
handling pause per unloaded portion retains the source default; actual provider delays are additional
only when required by its real contract. Do not use an arbitrary 20-tick delay because a display name
matches Extended Storage. When no destination exists, try a safe nearby drop of cargo only; a failed
drop leaves it held and ends the attempt with status. Prevent immediate recollection without newly
valid storage. Urgency follows undelivered material; dropped is not delivered.

Select the next physical destination group by its nearest reachable valid delivery cell's
straight-line distance, with stable ties, then deliver its still-valid committed portions before
moving to another group. A shelf, stockpile or container is a physical group; linked settings do
not combine separate shelves into one location. Revalidate per-item acceptance and opportunity
travel bounds; failed portions do not stop valid portions or become falsely delivered. This is a
bounded greedy order, not a shortest-route claim or one literal drop for all stacks.

After a failed automatic recovery, retain a per-pawn remaining delay of 600 simulation ticks for
that unchanged problem, inspired by Reworked. Persist the remaining delay; rendered frames do not
control it. Relevant destination/access/load changes or a safe explicit retry can permit an earlier
attempt. Identical idle/inventory notifications cannot reset or bypass the wait, and one pending
attempt cannot recursively schedule another. Emergency work and preventing physical item loss
remain authoritative. This named constant is not another settings row.

If hands contain a baby or another incompatible entity, do not free them by dropping that entity.
Wait/yield or perform a separately proven safe recovery action. SimpleBabyCarry's old private-toil
patch does not establish support for our jobs. Keep ordinary emergencies, drafting, explicit orders,
work disable and pawn capability authoritative. Idle/inventory/native haul notifications schedule
at most one recovery, never recursively replace a current emergency. Setting changes disable further
now-ineligible pickups while preserving recovery, including when animal/corpse batching is off.

Drafting retains cargo by default. If the shared draft-drop preference is enabled at the transition
into drafting, attempt safe placement of owned cargo only without delaying the drafted state.
Failed quantities stay held and identifiable; kept items and carried pawns remain untouched.
Preserve urgent intent across drops. Enabling the preference for an already drafted pawn does not
cause a drop, and no failed transition drop repeats through idle checks while drafted.

Native TakeInventory provides movement/reservation/pickup boundaries, but source-wide int.MaxValue
permission replacements are unnecessarily broad. Expose the owned capacity-limited Take and keep
action only in its actual player-controlled context, using native action paths with documented guards.
Kept/release/unload/retry are pawn gameplay state, not mod settings. S123 meal carrying will use the
same ownership/capacity accounting directly; it is not implemented or fully specified here.
Supported loadout/sidearm/medicine policies supply exact protected quantities through narrow
adapters; temporary cargo neither fills their personal quotas nor becomes ordinary unused inventory
for cleanup. Unknown protection disables affected new collection, with independently safe existing
cargo recovery. This does not prevent normal consumption or explicit provider-authorized actions.

### Settings, UI and save scope

Five actual preferences use the common definition/load/resolve/save/row-rendering path: allow corpses
false, allowed capable animals true, allowed capable mechs true, minimum free starting mass percent
20 in integer range 0–100, and Drop hauled cargo when drafted false. Draft-drop off deliberately
differs from Continued's on default; the safe opt-in behavior is retained. Correct the source's gear-only check to all occupied mass and inclusive
boundary. Positive capacity and a fitting unit are required even at 0%; 100% requires zero burden.
The threshold gates a new batch only. Inherit current globals by default, keep dormant overrides,
and disable Current save/Use global plus mutation callbacks at the main menu. Corpse/race preference
edits affect the next pickup; threshold edits affect the next new trip; draft-drop affects the next
transition into drafting. No preference inflates limits.

Gear displays cargo/kept quantities in text, optionally a secondary tint, plus contextual reason and
unload/retry action. Do not color by the first Color.white getter in a generic row. Verify seven
languages, screen/UI scale, same-type mixed quantities and actions while drafted/incapable. No bottom
tab, unused global keep-default control or extra Hauling work column. Job text should distinguish
collection, delivery and unresolved cargo without exposing internal bookkeeping terminology.

### Contracts and optional integrations

The research table lists actual consumers; no full PickUpAndHaul assembly, class tree, Def-name or
ThingsHauledToInventory save emulation is promised. Several consumers patch private toils, fields
and generated pickup methods. A facade that happens to have the right names would not preserve their
control flow and could activate unsafe assumptions. Preserve observable cooperation through narrow
adapters where feasible, record unsupported bindings explicitly and avoid original package spoofing.

Owned S008 invokes the batch planner directly with exact urgent quantities and shared order lifetime.
Its held cancellation and mixed loose-stack attribution use the same quantity rules as the
external bridge below; both owned batch and ordinary urgent fallback clamp to the outstanding
portion. A positive loose-stack marker never authorizes cancelled/ordinary units. S008's capability
and tasks explicitly share this contract, including saved cancellation and contextual quantities.
Optional original Allow Tool can use a narrow adapter to its public static
WorkGiver_HaulUrgently.JobOnThingDelegate (nested TryGetJobOnThing(Pawn,Thing,bool) -> Job).
Resolve canonical UnlimitedHugs.AllowTool, its designation and delegate shape/lifecycle. The source
consumer currently locates our absent upstream work-giver type by name; a direct delegate adapter
preserves the public behavior without creating that type. Capture the actual existing delegate;
do not overwrite an unknown foreign replacement. Only compose with a proved native/default or
compatible delegate. On unsupported batching, invoke the preserved supported ordinary route with
the same seed/forced meaning. Do not duplicate urgent work categories or keep self-patches. If another
mod later replaces the delegate, detect ownership loss and revalidate or relinquish; never overwrite
that later owner during cleanup.

The delegate is insufficient for urgency lifetime. The inspected provider's
DesignationCleanupHandler.CleanupDesignations removes urgent marks when Things leave native
haulables (scheduled every 60 ticks per map); ToilsHaul_PlaceInCell_Patch clears the mark before
invoking the placement initAction. S008 is withheld while original Allow Tool is active, so its
owned designation lifecycle cannot be assumed to intercept either path. The external adapter must
capture urgent intent before each successful pickup, attach it to the actual accepted cargo quantity
and preserve it through PH-T2 split/merge/transfer accounting independently of ground designations.
Automatic source cleanup and pre-placement removal do not cancel or complete this owned intent.
Successful physical placement retires only the delivered portion. A failed placement/nearby drop
retains intent and reconciles one original urgent designation on each eligible loose Thing; it
cannot create duplicates or redesignate explicitly cancelled portions. Unrelated source jobs and
its finish-off designations remain untouched.

Resolve actual explicit cancellation paths separately from automated RemoveDesignation calls;
removal alone is not evidence of player intent. External cancellation of loose material clears the
selected outstanding quantity; Gear supplies Cancel urgent delivery for held quantities. This is a
contextual order action, not a setting or a second designation. Cancellation leaves the physical
items and ordinary cargo recovery intact. Save the urgency/cancellation outcome with exact owned
quantities, reconcile current holders on load and never recreate a cancelled order from an old
route or absent marker. Provider loss disables new integration work, not owned saved-state recovery
or cancellation. Exact cancellation/cleanup/place boundary shapes remain PH-Q3 implementation
research; do not guess a safe hook from a common method name.

The external designation is whole-Thing, so retain owned urgency quantities on dropped/merged loose
stacks as well as held cargo. A reconciled external mark means positive outstanding urgency, not
permission to promote every unit of a mixed stack. Show the exact urgent count in contextual item
inspection. Split/merge transfers must retain urgent, cancelled/ordinary and kept attribution without
converting one portion into another. External cancel on a loose stack clears all its urgent units;
held Gear cancellation can select a portion. For dispatch, clamp both the batch and any supported
ordinary urgent fallback to the current outstanding urgent count at planning and successful pickup.
The source delegate has no quantity parameter: use a verified native job-count/actual-pickup seam
with per-job ownership and effective semantics, not a guessed field assignment or generic seed
designation check. Opportunistic additions also need current urgency quantities. If a fallback
cannot honor this bound, defer that mixed stack's urgent work with a contextual reason; do not call
the unbounded preserved delegate for it. Preserve ordinary external fallback for fully urgent,
unmixed source items wherever its route is proved safe. This mixed-stack protection is part of
saved-state recovery when the collection adapter later becomes unavailable, not optional UI sugar.

Treat delegate installation plus this narrow lifetime bridge as one admission group within PH-T6.
Individual target documents must prove required marking/cancellation provenance, mixed loose-stack
quantity limits including fallback pickup, timing around successful pickup and placement, and
effective cleanup/pre-placement behavior, with compatible
foreign edits and later repatching accounted for. If any required predicate fails, do not install a
delegate-only partial adapter: leave the supported ordinary external route and bounded warning.
Existing owned cargo continues through independently admitted protection/recovery. This is proposed
integration behavior for our cargo, not a claim that original Allow Tool already provides it.

Keyz 1.6 has a direct binary/package-gated bridge; its 1.7 loadFolders omits PUAH support. It is not
automatically compatible. Modern external While You're Up still discovers legacy private PUAH
types/fields/methods, then uses GetHashSet and takenToInventory without complete discovery guards.
Continued supplies those names; Reworked and Armful do not. Its unload substitution can turn stale
references into whole same-Def personal stacks, and its ordinary fallback cannot see our ledger.
Do not expose a misleading HashSet facade. If an external opportunity path can add an independent
load or mutate owned quantities without safe coordination, withhold affected new collection with a
warning; preserve independently admitted existing-cargo protection/recovery. This does not reject
unrelated foreign patches. VMF and Shared Storage Access also need explicit adapter work. VMF cross-map destinations are
outside initial same-map routing; normal movement between maps invalidates the plan. External
While You're Up binding gaps do not describe future owned S125: it and S024 use one direct
opportunity/parent-job decision. Neither writes the other's or a foreign mod's global preferences.
Stack gap and external Kanban require exact acceptance budgets or provider-native fallback;
own S049 calls directly.
IHoldMultipleThings can be reflected through a supported provider's actual loaded interface and
package identity, with bool success/out-capacity and stackability semantics guarded. Do not copy its
downloaded contract assembly. No provider was established as currently requiring an owned facade.

CE inventory integration is deferred outside initial support. Other custom inventory capability is optional. Prove actual package identity, capacity/fit/update
semantics and effective native interactions before enabling its batch route. Missing knowledge
with an active custom capacity system yields unsupported batching, not a fake true/whole-stack helper.
No CE provider was found in the bounded local metadata search; this is a recorded research gap.
Continued's reflective CE weight/bulk/loadout adapter is a useful reference, but permissive fallback
when per-pawn capability is absent is not accepted as proof of our combined limits.
Immersive Chefs remains an independent repository product. Its old assembly/package-gated adapter
does not activate for this mod; a later narrow optional consumer update needs its own owner/change,
not a required runtime link between products.

### Patch admission and recovery groups

These are candidate boundaries, not completed target documents or executable validators:

| Candidate | Minimal consumed semantics | Failure isolation |
| --- | --- | --- |
| PH-T1 collection/native work entry | Exact worker/seed/forced route, native rejection authority and selection of inventory versus ordinary haul | Withhold affected new collection; preserve known ordinary hauling |
| PH-T2 actual transfer and removal | Correct affected Thing/count/holder and success timing for split, merge, pickup, drop, destruction and inventory notifications | Required cargo-conservation group; no collection without safe tracking/recovery |
| PH-T3 recovery opportunities | Genuine safe work/idle/delivery transition and pawn/job context; observing enumerable creation alone is not delivery proof | No duplicate recovery or priority override; expose manual supported recovery |
| PH-T4 Gear/context commands | Actual pawn and row/item identity, appropriate UI action scope, native eligibility and result | Cosmetic marker can fail independently; unsafe command stays unavailable |
| PH-T5 data invalidation/destination use | Consumed state mutation, map/holder identities, effective acceptance and capacity at commitment/use | Fresh native fallback or affected candidate/destination unavailable |
| PH-T6 optional provider adapters | Canonical active package, actual type/member/Def shapes, required data flow and delegate ownership/provider invariants; external urgency requires cleanup versus explicit-cancel provenance, actual pickup/placement outcomes and saved quantity lifetime | Admit delegate and urgency bridge together or leave ordinary external hauling; preserve independently admitted saved-cargo recovery, no partial ownership takeover |

Before each actual Harmony or XML patch, create its individual stable target document, predicate,
tests and startup/later-repatch policy. Method existence suffices only for an observer that needs
no stronger body guarantee. An argument/result/flow change must prove the consumed semantic snippet,
required operands and unambiguous cardinality. Account for effective transpilers and skip-original
prefixes; original IL alone cannot prove the runtime flow. Every transpiler validates its actual
incoming stream on every run and returns it unchanged on failure. Never require exact whole-method
hashes, a pristine method or no other patch owners; never rerun foreign transpilers speculatively.

Activate collection only with its required capacity, cargo conservation and recovery group admitted.
Missing/unknown predicates, validator exceptions or install failure roll back only newly added owned
patches in the group. Warn once per skipped enabled patch with package/patch/spec ID, target, reason,
unavailable behavior and fallback. Deliberately disabled preferences are not failure spam. Source
co-installation with Mehni PUAH, Continued, Reworked or Armful canonically withholds overlapping new work, while saved owned cargo remains visible
and safely recoverable. If even recovery/protection cannot be established, fail activation explicitly;
do not reinterpret owned cargo as untracked personal inventory or let another allowance build atop it.

Use additive owned Job/WorkGiver Defs. No blanket XML injection into every Pawn is selected. Any later
XML modification requires XML Extensions package/Def/node existence guards, merge preservation and
required dependency metadata on first XmlExtensions use. No runtime Def mutation substitute, original
Def collision, downloaded binary or Gateway dependency in the product.

## Risks / Trade-offs

- Combined volume is stricter than source inventory-only mass → clearly proposed in PH-Q1; measure
  common lightweight/bulky loads and preserve vanilla single-heavy-load access after unloading.
- Destination commitments can become stale → reserve real quantities, invalidate changes and recheck
  use; treat unknown external multi-stack behavior as unsupported rather than free capacity.
- Quantity ownership spans native split/merge/removal → cover exact transfer results and failure
  conservation before collection can activate; protect personal supplies on ambiguity.
- Direct source consumers have incompatible private assumptions → prefer narrow proved adapters and
  document unsupported contracts; no package impersonation or promise of complete binary parity.
- A finite trip can unload sooner than source's repeated collection → deliberate delivery/fairness
  correction, benchmark actual throughput and emergency responsiveness rather than assuming faster.

## Migration Plan

No runtime migration occurs in this documentation change. Later add owned saved cargo/kept state,
safe recovery and current settings resolution before enabling any collection. On upgrade/load,
reconcile only surviving exact identities/quantities and invalidate old route commitments. Original
source save keys/jobs/comps are not silently imported. Any future foreign migration needs its own
backup, atomic conversion, restoration on failure and native verified postconditions. Source/provider
removal and repeated initialization must not lose items or duplicate job ownership.

## Open Questions

PH-Q1 retains the mass/volume and ordinary-heavy-load interpretation; PH-Q2 exact beta worker,
transfer and storage lifecycle; PH-Q3 real legacy/public adapters; PH-Q4 custom provider capability,
UI/languages, measurement and acceptance. No question interrupts sequential specification.

Semantic pass: batches reduce travel, not item creation/cost, research or free work eligibility.
Capacity is real, storage limits are shared, emergencies remain authoritative and actual delivery
defines completion. Final proof needs native jobs picking up and depositing measured mixed loads,
personal supplies remaining held, interruptions/recovery and supported provider scenarios on the
reviewed build. Startup patches, queried quantities or a prearranged storage scene are insufficient.
