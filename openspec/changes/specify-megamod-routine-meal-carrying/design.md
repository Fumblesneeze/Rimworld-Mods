## Context

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, target1.7 beta.
Original S123; [research](../../../mods/megamod/docs/refinement/2026-11-06-routine-meal-carrying-research.md)
retains native input hashes and the boundary between confirmed intent and proposed defaults.
No runtime work is delivered. Read the S021 quantity/capacity contracts, S024 practical supplies,
S041 food history, S055 sharing, S057 preference and S111 scope before implementation.

## Goals / Non-Goals

**Goals:** colonist-wide optional personal reserves, one physical quantity authority, useful bounded
pickups, actual eater-specific food suitability, coherent retention and explicit player control.

**Non-goals:** a second inventory, new Hauling work eligibility, automatic animal provisioning,
caregiver meal delivery, caravan ration allocation, generated meals, stock policy rewrites, source
mod impersonation or a generic food-management window. Do not impose S021's batch-hauler eligibility
on personal self-packing merely because both use its capacity and quantity services.

## Decisions

### 1. Extend one packing opportunity

Native Humanlike ThinkTree already reaches JobGiver_PackFood after important colonist duties and
stock/unload handling. S024 lowers the Food gate from0.6 to0.3; this draft revises S024's active
packing requirement to delegate complete reserve planning here. Keep one opportunity and one
Food gate. Do not independently patch owned S024 or add another ThinkTree branch that races it.

Admit native colonist membership and normal earlier tree exclusions, then require usable food
need, inventory, self-feeding, movement and manipulation. Native IsColonist includes secure slaves
and excludes subhumans; it is not synonymous with IsFreeNonSlaveColonist. Preserve ordinary care
for babies/dependents and native drafting/danger/forced duties. Personal packing is not a new
Hauling job, nor permission for a guest to do S021 batch hauling. Native world-caravan food use
and transport requests remain outside this opportunity.

### 2. Plan nutrition and whole units explicitly

Use current Food.MaxLevel multiplied by effective common target factor as target B. Evaluate
NutritionForEater for eligible physical portions, not only the Thing's generic Nutrition stat;
reject nonfinite/nonpositive nutrition. Native prepared food currently receives no additional
raw-food gene multiplier here; differing prepared-food nutrition is an admitted-provider case. Let E be personally available eligible nutrition, excluding
cargo and incompatible active claims. Existing player-kept and native-stock food counts once.
Do not consume pending incoming stock/eating claims twice or issue a parallel refill while those
claims are resolving for the pawn.

At a natural opportunity, refill only if B>0 and E<=0.4B and E<B. For each candidate with nutrition n,
the normal desired quantity is floor((B-E)/n). With E=0 only, use at least one unit even when n>B.
Then limit to physically available, reservable and S021 mass/volume-fitting whole units. Candidates
with no useful fitting quantity are not jobs; another candidate can be considered. The native
candidate-stack0.8 nutrition minimum is deliberately removed. No persistent refill-until-full loop
is introduced: each opportunity recomputes current facts and the40% gate. Partial reserves above
that gate can remain below the target until consumed.

Source-native defaults0.4/0.8 are absolute nutrition, while this proposed refill threshold scales
with actual appetite/target. Record this explicit refinement in S024, not as unchanged native parity.
The usual target1 and meal0.9 produce one meal; a target0.35 with that meal retains one rather
than oscillating. There is no oversize top-up when E>0. All numeric policy constants are named;
reserve factor is common configuration, default1, finite0–3 by0.25. Its upper bound is a proposed
anti-hoarding tuning limit, still subject to final playability review. Shared capacity and actual available portions constrain every value. There is no second capacity multiplier.

### 3. Reuse exact quantity ownership

Represent automatic reserve protection as a reason on S021's exact per-holder quantity records,
alongside explicit keep, cargo and active-use intent. Do not create another stack boolean or clone
physical inventory. Preserve quantity identity through split, merge, movement, ingestion and
transfer; use S041's portion history for actual changes. Public/optional native-stock adapters must
use current authoritative entries, not a cached target count copied into several modules.

Reconcile eligible unclaimed personal inventory before new collection. Keep already protected
appropriate reserve portions first, then deterministic existing inventory order; never convert
delivery/transport/bill quantities. Explicit keep cannot be removed by lowering the automatic target.
Release unnecessary automatic units newest-acquired first. The one-unit oversize exception applies
only to a remaining otherwise-empty automatic reserve; do not accumulate several oversized stacks.
Automatic timestamps/order belong to the shared quantity provenance, with stable fallback for
preexisting items, not a parallel inventory scanner or fake source save migration.

Native FirstUnloadableThing otherwise uses a Food.MaxLevel ceiling and can release a whole meal
larger than that ceiling. Its retention path must consume the same reserve plan; installing pickup
without that cooperation is forbidden. Retention remains protectable if new job admission fails
at runtime: preserve held food/current use and let safe recovery finish rather than orphaning it.

### 4. Keep selection permission distinct from scoring

Use native map listers or S033 shared candidates for spawned food; do not search donors, generic
containers or idle shuttles for reserve stock. The20-cell bound is straight-line horizontal distance
with native ordinary safe reachability and permitted traversal. Retain forbidden, social, reservation
and known provider claim checks. Uncertain provider ownership excludes the affected candidate.
The inspected native packer has a1.5-nutrition-per-colonist communal entry guard. The owned
opportunity deliberately removes that guard under decision16: colonists still pack during shortages.
Do not substitute a days-of-food cutoff. Actual portions, individual suitability, access, claims and
capacity remain authoritative; no guaranteed reserve or diet-specific fair allocation is promised.

Require fresh IngestibleNow prepared food, native preference>=MealAwful, actual positive nutrition,
non-drug/non-corpse and native diet/ingredient/veneration/title acceptance. Do not turn the native
careIfNotAcceptableForTitle=false packing shortcut into an owned promise. Inspect the actual
ingestion thoughts applicable to the pawn/stage with shared S057/S024 helpers rather than source
stage0. No test should infer hypothetical tableless thoughts from a packing action; actual dining
and resulting thoughts remain native. Baby/caregiver food, dispenser operation and raw substitutes
are separate feeding workflows, not automatic reserve acquisition.

Compose ordinary FoodOptimality with S024's spoilage contribution and S057's humanlike preservation
score once. Use no artificial per-port second scoring pass. For otherwise equal source scores use
nearer distance then stable physical identity. S111 explicitly excludes refilling and remains so;
S055 fallback only runs for actual authorized hunger, never a reserve planner.

### 5. Perform a safe native pickup and recovery

Native TakeInventory approaches/reserves a source and delegates to Toils_Haul.TakeToInventory.
Its mass clamp depends on job.checkEncumbrance, whose native default/reset is false; PackFood does
not set it. Thus reading the initial one-unit mass test is not proof of a full-request mass limit.
The native toil also calls TryAdd on a split without inspecting its result in the inspected code.
Do not copy that as the owned atomic transfer contract.

Use the ordinary approach, reservation and item transfer path with S021's current quantity/capacity
bound at pickup and a checked accepted portion. Preserve source remainder or safely recover any
unaccepted split, then credit only accepted units. Respect both mass and volume, carried hands and
protected companions/babies. No generic patch should change unrelated inventory jobs simply to fix
this opportunity. Native handling time and pickup sounds remain; no new artificial work delay or XP.
Direct setup/spawn or inventory assertions cannot accept this runtime behavior.

### 6. Player intent and ordinary use have separate lifecycles

Pause/resume belongs to Pawn gameplay state and the native Gear/inventory context (future S132
must render the same action), not mod settings or a new tab. A player pause suppresses new pickups
but retains current automatic reserve protection. Pending explicit unload/drop/give claims suspend
refill while unresolved. Successful deliberate removal of a reserve portion sets the persistent
pause with a visible explanation; cancellation before any removal leaves the former state alone.
Only explicit Resume clears a completed-removal pause. Use action provenance so automatic eating,
S055 sharing, spoilage, cargo unloading and map transfers do not masquerade as player removal.

Sharing decreases the actual reserve without creating a pause. The donor can refill only from
eligible communal map food at a later natural opportunity, never from the eater's current-use
portion. Mixed unassigned removals retain S021 cargo-first attribution. S024 interrupted-meal
recovery and S055 actual-ingestion claims remain authoritative; there is no recollect-as-surplus step.

### 7. Common configuration and lifecycle

Register `supplies.personalMeals.reserveFoodBars` under Supplies and work / Personal meals with
full common global/save/Use-global semantics, default1. Inherited settings read the current global
value; disabled override editors and mutation callbacks work at main menu and while inheriting.
Validate finite range and increments through the common path; malformed persisted values fall
back to the built-in default with a bounded diagnostic. Zero means no new automatic collection
and release automatic protection, with safe ordinary unloading only.

Recompute at the next safe evaluation; pickup commits recheck target/permissions/capacity after
edits, without invalidating items currently eaten or discarding a split. Persist pawn pause and
shared portion ownership with their entities. Rebuild derived facts/claims at map/game load,
switch and teardown; never refill by spawning or replaying completed requests. Unknown future
optional provider state fails the affected path closed. Seven baseline languages cover scope,
nutrition/bar units, actual reserve, pause reason and unavailable-state UI.

### 8. Semantic admission, ownership and failure

Planned target families are MC-OPPORTUNITY (ThinkTree/giver), MC-TRANSFER (native approach/checked
quantity transfer), MC-RETENTION (shared inventory cleanup) and MC-INTENT (explicit versus automatic
removal and contextual controls). Implementation must create a target document per actual patch;
these names alone are not those documents. Plan pickup/quantity/retention as one atomic required
group; admit optional external inventory representations only behind narrow package/content/type
guards. A known competing packer needs a declared single-authority or proven cooperative adapter,
not a blanket ban on all other patches or a source identity invented for this original feature.

For changed XML use XML Extensions Def/node checks and packageId=true FindMod on external content;
declare XML Extensions required on first use. Preserve sibling ThinkTree nodes/order rather than
replacing the entire tree. Any C# argument-changing prefix must guard required effective data flow;
each transpiler validates its incoming stream and returns it unchanged on mismatch. Harmony is
required if used. Do not require exact hashes or no foreign owners. Document later repatch handling.

On false/unknown target, validator error or install failure, rollback only newly applied owned
group operations, warn once per skipped enabled patch with IDs/reason/lost behavior/fallback, and
retain independent native/provider behavior. Do not promise unmodified native packing has the
owned capacity/retention corrections when owned admission fails. A runtime failure preserves held
items, releases stale claims and waits for a later valid opportunity without same-tick job churn.

## Risks / Trade-offs

- Reserve quantity was not specified by the user → explicit proposed native-grounded nutrition
  target and tuning range; MC-Q1 retains playability review without blocking specification work.
- Low capacity/large meals can make target rounding inconsistent → one-unit empty-reserve exception
  shared by pickup/retention, numerical boundary scenarios and child/modded-food native runs.
- Native packer conflates cargo with food → S021 provenance distinguishes delivery from available
  personal quantities; do not change ordinary ingestion's legitimate cargo consumption semantics.
- Reserve privacy would break S055 → keep only prevents unloading; actual sharing respects its
  native-first and eligibility limits and current-use protection.
- Extra UI/settings could become a loadout manager → one quantity setting and one entity pause
  action; native explicit stock/diet controls remain theirs. No new category filters or global tab.

## Migration Plan

Nothing ships in this draft. Later initialization uses common default inheritance and reconciles
real held food; it does not duplicate food, rewrite food policies or import a nonexistent source
mod's state. Existing explicit stock/keep entries stay authoritative. Disabling new collection or
missing optional adapters must leave held items recoverable. Any future state migration requires
its own backup, atomic restore and postcondition contract.

## Verification and Open Questions

MC-Q1–5 in refinement retain target/eligibility playability, exact1.7 effective targets, shared
quantity/source ordering, optional inventory/UI providers and native acceptance. Unit tests cover
pure sizing/reconciliation/pause/settings rules; lightweight Def fixtures belong in honest Defs
tests. Loaded ThinkTrees, real Core/Workshop foods, active inventory providers and actual ingestion
require fresh isolated RimWorld processes. Use focused vertical RED/GREEN and independent review,
then reviewed-build native pickup, hauling/unload, sharing/eating, explicit unload/resume, save/load,
settings and failure workflows with personally inspected causal evidence. No runtime tests now.

Final balance compares adult one-meal packing, small appetite with large portions and tight shared
capacity against native communal stock/food use. Check no food generation, nutrition or mood change,
care starvation, storage loop, capacity bypass, hidden delivery loss or new work priority.
