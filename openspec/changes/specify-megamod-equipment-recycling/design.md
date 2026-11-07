## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Original feature S022 / P1-17. The user confirmed weapons/apparel scope, not complete Recycle This
parity. [Research](../../../mods/megamod/docs/refinement/2026-11-02-equipment-recycling-research.md)
pins installed 1.7, public source, native calculation paths and a real external WorkType consumer.
This document specifies future implementation; all tests, runtime admission and acceptance remain open.

## Goals / Non-Goals

**Goals:** explicit per-item recovery, physical facility work, meaningful material returns, safe
cancellation and persistence, one processing owner and current capability-based integration.

**Non-goals:** general crafted-object recycling, on-site melee destruction (S023), all Micro
Designations operations (S096), invisible bill insertion, original work-column emulation, automatic
stockpile disposal, historic ingredient reconstruction or importing source settings/jobs.

## Decisions

### Owned item jobs and direct composition

Use owned additive DesignationDef, JobDef and Crafting WorkGiverDef, with a narrow per-item processing
record for quantity, phase, facility, work and completion. Place the workgiver after existing native
Crafting bill work initially; preserve native worktype and explicit player priorities, not the
reference's separate work type or global priority edits. Record exact beta workgiver ordering before
implementation. Native eligibility, danger, reservations and current work abilities remain authority.
Do not infer an ability from canBeDoneByMechs alone or add Crafting to arbitrary races.

One choice owns unfinished processing of an exact unit. S023/S096 must call this ownership directly;
they are not patches to an internal RecycleThis clone. Explicit replacement/cancel invalidates work
authorization before the next irreversible step and discards prior recycling progress. Preserve
unrelated designations, including urgent hauling, until physical consumption makes them moot.
Native bills, linked-bill lists and counts remain untouched: no hidden bills that other features
would copy, count or synchronize. Do not preempt emergencies or force Crafting on disabled pawns.

For a compatible stack, authorizing the current quantity is distinct from authorizing later merged
units. Extend the owned physical-quantity attribution used by S021 rather than making a private
second item list. Ordinary carrying retains authorization and waits for a valid local processing
opportunity; worn/equipped/kept transitions cancel before removal of the item can be mistaken for
consumption. Map departure suspends and drops local facility/reservation claims; return requires
explicit Resume. Unknown references never match by Def alone. The source stackLimit<2 shortcut is
not adopted; unsupported custom stacking semantics must fail closed for those affected items.

Retain each portion's processing choice and active/suspended state through split, merge and reload.
Resume affects only its selected portion. A native removal with known attribution retires that
portion's order; a quantity-only removal from marked/unmarked material debits processing authorization
first, down to zero. Do not use min(old authorized count, remaining stack count), which can promote
unmarked survivors. Where distinct choices/suspension states make allocation uncertain, visibly
suspend affected remaining orders for explicit resolution. One-unit recycling does not remove this
attribution requirement; S021's cargo/kept policy alone does not supply it. Compose S022/S023/S096
through this owned rule.

### Resolve facilities by capability, then check the actual actor

Smelting requires the matching finalized SmeltWeapon/SmeltApparel recipe, item eligibility and
effective provider acceptance. Non-smeltable scrapping uses finalized capability to make
Make_Apparel_BasicShirt or Make_Apparel_TribalA, covering tailors and crafting spots without assuming
an exact building name. These anchor recipes identify a suitable physical facility; original-item
manufacture research/skill requirements are not copied into recycling. Relics are explicitly rejected
before the non-smeltable fallback, preserving native protection instead of routing a relic to scrap.

Use native listers/designation queries and the shared Thing subsystem. Static recipe/capability
metadata can be shared after Def finalization; actual instances, power, fuel, interaction cells,
access, path and reservations must be checked per worker at selection and use. Never copy/sort every
Thing for each recycler, cache per-pawn permission as an item fact or rely on a process-lifetime
list built before optional recipes finalize. Provider changes invalidate affected capability data.

Choose the nearest eligible bench from the item with stable identity ties. Prove both pawn-to-item
and item-to-interaction-cell travel under normal safe traversal; a start-point search alone is not
proof of the worker's whole route. Use native fueling work when it is genuinely available, then
re-evaluate. Respect CurrentlyUsableForBills (or validated provider equivalent) for **both** smelting
and scrapping, at work and before completion. Validate fueled custom work semantics rather than
assuming a CompRefuelable presence proves the entire consumption model. No constructor Def mutation.

### Work and return calculation

Base work is a named **1600** constant/owned JobDef value, matching native/reference smelting work.
SmeltingSpeed or GeneralLaborSpeed is multiplied by the current bench WorkTableWorkSpeedFactor;
no fallback rate of 1 masks an unavailable bench. Preserve native comfort and interruption paths;
award no extra XP. Progress is per equipment unit, saved across interruptions and facility changes.
Do not reset work merely because a new JobDriver is constructed. A new processing choice resets it.

Build a pure recovery plan from CostListAdjusted(actual Stuff), reading but never mutating its cached
list. Convert Stuff volume through that native basis once. Reject missing/invalid identity, volume
or quantities; never substitute DefaultStuff for an unresolved actual material. This is declared
cost recovery, not recipe enumeration or a history of manufacture. The native base implementation
filters to smeltable non-intricate costs; our proposed recycling intentionally includes ordinary
wood/stone/textiles too. Complex recovery is **excluded from initial scope**, for both adjusted costs
and explicit smeltProducts; do not add an enabling setting in the initial UI. The construction-return slider is integer **0–100**, initially **25**;
fixed salvage is not scaled by it. If both lists name the same material, combine their separately
defined contributions only after applying their rules. Expose this to the player rather than
silently dropping an intentional native extra product or treating it as a duplicated entry.

Use current finite HP/maxHP clamped0..1 at commitment for both construction and fixed salvage.
Combine each material's two contributions before this condition scaling and one stochastic rounding.
Invalid condition semantics reject the route. Quality/skill add no separate multiplier; do not
evaluate reference recipeMaker efficiency on the input Thing.
Eligibility checks positive possible output, not fixed Math.Round(cost*0.25). Preview computes ranges
and chances without calling an iterator that instantiates products or consumes RNG. Resolve outputs
once at commitment and persist them for recovery; opening UI or retrying cannot reroll. Fixed salvage
must be valid and supported, especially Stuff-required or custom output creation. A custom override
of SmeltProducts or special consume behavior cannot be silently replaced by base cost arithmetic:
admit a specific adapter or leave that item's route unsupported. Ordinary compatible content Defs
do not need a package-name allowlist. External executable boundaries do need package/shape validation.

Capture effective refund settings at first work on the unit, save them with progress and show them
when current preferences differ. Rate edits affect unstarted units; current condition is deliberately re-read before consumption. Cancelling and creating
a new order resets work and adopts current values. Completed products never change retrospectively.
The proposed economic trade-offs, including low-cost zero returns and high construction-return rates, remain
ER-Q1 and require actual finalized comparators/playability before implementation acceptance.

### Physical completion and recovery

Use native carrying, ingredient placement and work opportunities, with exact success/count checks.
Do not blindly reproduce the reference's PlaceHauledThingInCell target or enumerate products before
input consumption with no recovery record. Establish the actual 1.7 input-to-work and output-placement
seams first. S021 capacity/recovery applies when a worker arrives with owned hauling inventory.

Prepare a bounded immutable output plan and a persisted phase record. Before commitment, keep the
input recoverable and no products usable. Commit exactly once through an admitted native consume
path; thereafter the input cannot be reprocessed and the saved result owns all undelivered outputs.
Release outputs through safe native placement respecting stack limits; failed placement retains
the exact owed remainder in owned recovery state and exposes contextual recovery/status at the
surviving item/facility or worker. If the facility disappears, retain map recovery status and a
reachable item-order recovery action; a saved output claim cannot require a destroyed bench's UI.
Resuming output recovery does not require repeating 1600 work or consuming another equipment item.

Pin exception boundaries around custom consumption and placement. Preflight unknown irreversible
providers before starting; do not catch an exception after possible mutation and rerun the entire
conversion. Determine actual surviving input and issued output quantities, retain a visible suspended
record when the outcome is ambiguous, and prevent either input reprocessing or output duplication.
The record's lifecycle must survive save/load and provider loss; it is feature state, not a generic
transaction framework or a fake host-only proof. This is the main ER-Q2 runtime work.

### UI and common settings

Orders tool plus selected-item contextual command, inspectable pending status/preview, Cancel and
Resume; no bottom tab or debug deletion. Ordinary order state and progress are not mod settings.
Show selected-item command defaults on, while Orders remains accessible if hidden. The two rows
live in the ordered Item processing / Recycling section, using the common loader, saved override
model and row renderer: Global default, Current save, Use global with dormant overrides and disabled
main-menu save callbacks. Hiding the command affects UI immediately without deleting state.

Use seven languages for material summaries, fractional chances, captured versus current rules,
blocked reasons, cancellation/resume, committed-output recovery and source coexistence. Native drag
and contextual input must agree, including stale selection. Existing native icons may be used only
after applicable asset rules are satisfied; no source bitmap is selected or copied in this draft.

### Compatibility, patches and declarative registration

Reference provider Mlie.RecycleThis is optional. If active, withhold owned new and unfinished
consumption, preserve input/progress as suspended and allow only committed-output recovery.
After safe activation without that provider, require explicit Resume. Owned job loading must honor
this before any final toil can consume. Do not copy source designations/save keys or make its
WorkType exist to activate [AV] Mechtech's package-gated XML. Ordinary Crafting capability may allow
that mech to work naturally, but its installed 1.6 contract is not proof of target-beta support.
Custom bench/salvage adapters are optional and absent-safe. No repository-product runtime dependency.

Candidate boundaries, not approved target documents or implemented validators:

| Candidate | Consumed semantics | Failure behavior |
| --- | --- | --- |
| ER-T1 Orders XML registration | Orders category and expected class-list node exist; additive insertion absent/deduplicated | Skip affected registration with startup reason, no whole-list replacement |
| ER-T2 selected-item command | Exact item/pawn context, native command lifecycle and ownership; observer-only shape if sufficient | Keep Orders access; no stale item action |
| ER-T3 physical identity/lifecycle | Actual successful quantity/holder transitions, equipment/keep changes, map departure and split/merge | No new processing without safe authorization/recovery tracking |
| ER-T4 work/consumption/output seam if a patch is necessary | Correct actor, authorized unit, usable station, actual consume/placement result and save/load timing | Suspend before new consumption; retain committed output recovery |
| ER-T5 external provider | Canonical package and actual recipe/Def/member/return/side-effect capabilities | Only affected route unavailable; never assume refund or station validity |

Prefer direct owned jobs/public APIs; no patch is needed for a direct call to our own policy. Every
actual patch selected from these candidates needs an individual stable target document, executable
startup predicate, exact required semantic snippet/data flow, owned atomic group and future-repatch
policy. Existence-only guards are sufficient only for true observers. Account for effective code,
skip-original prefixes and incoming transpiler shape on every run; tolerate benign foreign changes,
not exact hashes/pristine methods. Return incoming IL unchanged on mismatch, and never replay foreign
transpilers speculatively. Installation failure rolls back only newly owned patches in that group.
Warn once per skipped enabled patch with package/patch/spec/target, reason, unavailable behavior and
fallback; no per-tick spam. Recovery/authorization admission is prerequisite to destructive work.

Use XML Extensions for Orders existence/shape checks and merge-preserving class insertion. It becomes
a required dependency with Harmony; update metadata/release declarations in the implementation
slice. External XML also needs packageId=true guards plus actual content/node checks. Preserve native
Crafting/Art priorities, work lists and all unrelated entries; no whole-category or fuel-list replace.
Owned Defs are additive. No downloaded assemblies, interface library or Gateway ships.

## Risks / Trade-offs

- Nominal-cost refunds can differ from actual manufacture → name the basis, measure ordinary and
  provider costs, keep custom recovery unsupported until coherent and document any economy changes.
- Shared stack quantities and cancellation can diverge → exact successful transfer attribution and
  authorization checks before consume; never repair by item type or overwrite unrelated marks.
- Consumption/output exceptions can duplicate or lose material → durable single result and bounded
  recovery, tested in real native workflows; no blanket retry of an uncertain irreversible call.
- Bench availability may change → recheck at each work/commit boundary and keep partial work on the
  same item. Do not infer usability from a cached matching recipe alone.

## Migration Plan

No runtime migration occurs now. Future owned state starts without importing foreign orders or
settings. On load, reconcile surviving identities and phase before assigning work; suspend on failed
admission/coexistence and retain committed results. A future source-save importer requires a separate
backup/atomic conversion/restoration/verified-postcondition contract. Do not claim safe mod removal
from a save containing owned work or output debt without a verified removal procedure.

## Open Questions

S096 now supplies the [item-recipe processing participant](../specify-megamod-item-recipe-orders/design.md).
Native smelting retains its actual recipe work/yield; it does not inherit this custom recycling
balance. Both use the already specified exclusive choice, authorized portions and recovery access.
Recipe interruption follows its native rules; this recycling operation retains its own durable work.

ER-Q1 retains proposed balance/UX choices; ER-Q2 exact beta jobs, consumption and recovery; ER-Q3
provider contracts and direct processing composition; ER-Q4 tests, UI, shared-data measurement and
native acceptance. None interrupts sequential specification. A document/schema pass does not prove
finalized costs, actual mechs, live power loss, material refunds or compatibility.
