## Context

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target1.7 beta.
S055/P1-48 replaces Meals On Wheels - Continued (`Memegoddess.MealsOnWheels`, Workshop3538082807).
[Research](../../../mods/megamod/docs/refinement/2026-11-03-meals-on-wheels-research.md) pins the public
docs, all five included source files, installed versus repository binaries, current native paths and
one actual consumer. No runtime implementation or acceptance occurs in this change.

## Goals / Non-Goals

Goals: complete fallback discovery→physical transfer→normal ingestion; preserve successful native
food choices, actor/action permissions, quantity identity, shared capacity and safe interruption.

Non-goals: new food/recipe content, world-caravan feeding, generalized container looting, caregiver
replacement, routine meal packing (S123), medical/drug response (S124/S134), foreign settings import,
an upstream identity facade or another bottom tab. No provider dependency on a repository mod.

## Decisions

### 1. Restrict fallback to the native self-feeding lifecycle

Source is a Priority.Low postfix on FoodUtility.TryFindBestFoodSourceFor with eight bound parameters.
It only acts on native false, eater.IsFreeColonist, both inventory flags and a tool-using/manipulating
getter. It ignores several caller flags and can receive getter!=eater without proving that caller can
retrieve another pawn's held food. Owned MW01 narrows the admitted path to normal self-feeding;
other callers need a separate proven lifecycle rather than globally supplying an unspawned result.

Current1.7 JobGiver_GetFood already turns another pawn's inventory result into TakeFromOtherInventory
with normal WillIngestStackCountOf count. That job approaches the holder and transfers to the eater's
inventory; it does not itself queue or perform ingestion. Prove the subsequent normal food/eating
decision consumes the received portion instead of creating an unload/refill oscillation. Native
animal-inventory selection already exists and chooses a close reachable animal; never overwrite
native success merely to duplicate that feature.

Honor the real request's food flags and eater WillEat/positive NutritionForEater. Source BestFoodInInventory
uses donor as getter context and returns the first acceptable stack, not a ranked best. Owned selection
uses the real eater/getter context and current food policy. Do not grant corpse, veneration, rotten
food or minimum-preference exceptions the originating search did not permit. Nutritious drugs keep
native permissions and teetotaler exclusion; no nonfood drug or medical-dose scope.

### 2. Explicit ranking, one shared candidate owner

Start with native map pawn/holder listers and the existing shared map candidate/index subsystem where
derived held-item facts are actually needed. Do not add a second per-source inventory cache. Recheck
holder faction/map, current owner, forbidden state, reachability, claims and actual food properties
at use time. Facts can be shared; eligibility is per eater/action. Invalidate movement, transfers,
stack splits/merges, destruction, holder departure, condition and map/game teardown; never persist
live references or enumerate game objects off-thread. Benchmark combined consumers before claiming
an optimization.

Source has two passes over pawn enumeration: first candidate from each holder at MealAwful+ and
fresh<30000ticks, then first at DesperateOnly..MealLavish with nutritious drugs allowed for non-teetotalers.
It can overlook a later spoiling meal and gives arbitrary pawn order priority. MW03 deliberately
inspects all eligible contents, preserves the12hour urgent class, then ranks by ordinary FoodOptimality
with current holder distance (the native Manhattan-distance input), including S024's optional meal
bonus exactly once. Nearer equal scores then stable holder/item identity break ties. The threshold
is source-derived and named in code, not a new user setting. No rot-rate change or second spoilage
scoring policy. Future S057 preserved-food policy must reconcile this exact boundary when researched.

### 3. Transfer quantities and current-use protection compose directly

Use native TakeFromOtherInventory as the pawn transfer path where safe; add narrowly owned checks
for actual count/claims/capacity/lifecycle as needed. Native TryMakePreToilReservations returns true
without reservations, so CanReserve at selection does not prove exclusive quantities. Acquire and
release actual portion claims through the shared action owner and recheck before mutation. A failed
transfer must not repeatedly enqueue the same unusable target in one tick.

Use S021's single carrying allowance for the actual receiver, including gear, hands and all inventory.
Clamp the meal request to available/fitting whole units; if none fits, preserve native no-source
behavior rather than inventing a capacity exemption. No reserve-meal pickup; S123 owns that choice.
Cargo/kept attribution changes only for actual moved quantities. Mixed ordinary removals reduce cargo
first under S021; the eater's obtained portion is current-use inventory, not a new delivery order.
S024 interruption and S041 food-taint rules operate on the same actual split/transfer result. Keep
means protected from unloading, not blanket refusal of food use; explicit provider restrictions and
active feeding/ingestion claims still apply. Unknown provider quantity protection fails that candidate
closed. No internal Harmony patches between owned features.

### 4. Shuttle support needs a distinct safe workflow

Source's Odyssey branch enumerates player PassengerShuttle, gets CompTransporter.innerContainer and
calls TryDrop(...,1,out_) during the search. It ignores failure and returns the original food reference.
Native ThingOwner.TryDrop can split a new object, merge placement, or restore a failed split: the
original reference is not a reliable transferred portion. Source also omits holder reach/forbidden/
launch-state validation and does not guard a missing container before Count. These are static risks,
not claimed reproduced bugs.

Retain optional shuttle intent with an owned retrieval job at the actual supported unload/access
point, after both earlier search tiers fail. Current PassengerShuttle has a rotated interaction cell
offset(2,0,0), CompTransporter/CompShuttle/CompLaunchable and ITab_ContentsTransporter. Native contents
drop uses that rotated offset and calls Notify_ThingRemoved to invalidate mass. Use the current
interaction cell and preserve holder notifications; do not copy the UI's unchecked SplitOff/drop.
TransportShip.Waiting includes a null current job; LeavingSoonAutomatically inspects current/queued
FlyAway. Neither waiting nor ShowLoadingGizmos alone proves accessibility. Require a spawned,
player-owned waiting shuttle, no automatic departure, no active/ready loading plan or protected
transfer, current legal interaction-cell access and valid container. LoadingInProgressOrReadyToLaunch
uses groupID>=0 (or caravan state), so even a fully loaded stationary shuttle remains protected.
CancelLoad/CleanUpLoadingVars drops all contents and clears plans: never call it to borrow a meal.
CompLaunchable.TryLaunch transfers cargo into an ActiveTransporter and records lastLaunchTick;
recheck actual ownership/launch state immediately before mutation. Do not offer shuttle contents
until that entire branch is proven. Choose an early job decision seam
that can create the retrieval action; a FoodUtility result alone cannot represent this action safely.
Do not spawn an arbitrary drop during a read-only query or pretend a stored item is a ground source.
Respect departure and transport quantities; source fixes and S133/S135 future loading must share
claims. Whole transaction means stable physical ownership/result identity, not rollback by spawning
replacement meals. If part of a real action has completed, recover from that actual location.

### 5. Minimal per-patch target specs and groups

Before each implementation slice create stable target documents, indexed with owning MW scenarios,
validators, tests and final evidence. Candidate groups:

| Group | Required effective semantics | Failure result |
| --- | --- | --- |
| Pawn discovery and self-feeding routing | Food request permissions/result shape; only native false; actual self-feeding caller converts holder result to transfer; correct eater/holder identities | No owned pawn fallback, native selection unchanged |
| Pawn transfer/current-use lifecycle | Approach touches current holder; requested actual portion accepted; correct result identity; safe cancellation, claims and capacity before mutation; later eating/recovery | Entire pawn fallback inactive rather than offering unusable inventory |
| Shuttle discovery/retrieval | Odyssey/content presence, valid holder/container, accessible player retrieval point, launch/loading/claim lifecycle, safe actual portion transfer | Entire shuttle branch inactive; pawn fallback independent |
| Shared candidate/quantity updates | Real movement/owner/split/merge/removal callbacks affect exactly the indexed facts | Dependent branch inactive or a proven current native enumeration fallback, never stale counts |

Method presence can suffice for an observational callback, but mutation/argument-use/skip-original
contracts need minimal IL/data-flow checks. Check effective semantics, tolerate benign changes and
other owners, and do not use exact hashes or whole bodies as admission. Each transpiler checks its
incoming stream on every run and returns it unchanged on mismatch; do not probe by replaying foreign
transpilers. Later supported repatching revalidates/suspends the group. Roll back only newly added
owned patches, retain already-transferred items and emit one bounded startup warning per skipped
enabled patch with IDs, target, predicate/reason, unavailable behavior and real fallback. No per-search
debug logging or global Harmony.DEBUG changes.

Original packages `Memegoddess.MealsOnWheels` and `Uuugggg.MealsOnWheels` suppress the whole overlapping
owned feature with a warning; do not alter source patches or mod lists. Preserve native food-provider
success. Harmony becomes required with runtime use; Odyssey/shuttle content and external providers
are optional. No XML needed now; future declarative edits require XML Extensions package/content/node
guards, merge preservation and the corresponding hard dependency, not runtime Def-list rewriting.

### 6. UI, persistence and actual consumer

Source Settings class has an unused sample checkbox/key, while Mod's settings initialization and
window methods are commented out. Do not copy this dead UI or introduce inheritance exceptions.
Ordinary diet and kept/cargo quantities are pawn gameplay state on their native contextual surfaces,
not global mod settings. Native job reports show retrieval/eating; unavailable optional support is
reported in bounded diagnostics. No custom bottom tab or extra debug toggle. Future genuine settings
must use foundation's common global/save model and main-menu protection.

Source has no owned jobs/Defs/save state. Future owned shuttle job state needs stable owned identity
and recovery based on actual current holders; no source migration or source-key reuse is needed.
Save/load cannot rely on old static candidate/claim references. Standard food/cargo state remains
with its real object; disabled/unavailable branches must safely release pending owned claims.

Installed Immersive Chefs1.6 is an actual reflected consumer: canonical source package gating plus
Meals_On_Wheels assembly/FoodGrabbing.Postfix eight-argument shape, source Harmony owner and low
priority on a17-parameter FoodUtility target. Its current adapter validates coexistence/logs and
leaves upstream selection authoritative; no other reads of its Enabled property were found. A
namespace alias alone cannot satisfy package/owner checks. Record unchanged consumer unsupported,
do not spoof identities or create a runtime/load-order dependency on another repository mod. Any
future change to Immersive Chefs belongs to a separately owned change. This source contract neither
proves nor prevents ordinary independent coexistence; exact native proof remains open.

## Risks / Trade-offs

- Read-only discovery versus source shuttle side effects → full retrieval job and actual-result
  handoff; inspected native lifecycle still needs effective admission and a naturally reachable idle-
  cargo player workflow before that branch is accepted.
- Food sharing and keep/loadout policies → explicit ordinary-use versus unload protection, actual
  quantity claims, native restrictions and conservative handling when protection is unknown.
- Two-step native transfer→eating → protect current-use portion and test hauling/refill interruption;
  a successful transfer is not proof a meal was eaten.
- Source first-match ordering changes → observable12hour class and native score/distance order;
  measure pathing/candidate costs rather than asserting this is faster.
- Installed and repository DLLs differ → selected installed build is the behavior reference;
  repository DEBUG logging/global Harmony changes are not imported.
- Beta advances → evidence hashes identify exact inspected1.7.4998rev1299, while semantic admission
  and real player workflows are required on the final implemented build.

## Migration Plan

No runtime migration or deployment now. Implement and admit pawn sharing first, then shuttle support
only after its access/departure/quantity contracts are pinned. Every runtime task remains unchecked.
Use focused RED/GREEN, independent review and fresh minimized native workflows before marking any
behavior complete. Roll back only owned groups, preserving actual food and standard save data.

## Open Questions

MW-Q1: effective beta food/transfer/ingest lifecycle, current-use claims and safe interruption.
MW-Q2: effective shuttle access/departure/loading and natural idle-cargo workflows, provider protections and actual consumer support.
MW-Q3: finalized food rankings, S057 preserved-food and S123/S133/S135 composition, shared-data cost.
MW-Q4: tests, language/UI reporting, package/review and final observed native acceptance.
Record these for future work; do not ask the user to explain source behavior.
