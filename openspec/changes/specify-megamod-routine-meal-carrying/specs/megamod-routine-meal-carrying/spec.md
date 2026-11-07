# Routine personal meal reserve

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), RimWorld1.7 beta.
Origin **original**, S123. Colonist-only routine carrying is confirmed user intent. Numeric defaults,
eligibility details and controls below are proposed refinements, not further interview confirmations.
Specification only; no gameplay acceptance claimed.

## ADDED Requirements

### Requirement: MC01 Colonists maintain a reserve through ordinary self-packing

Eligible colonists SHALL routinely obtain suitable carried food through the existing packing
opportunity, with no role or caravan-only restriction. This includes capable colonist children
and secure colony slaves recognized by ordinary colonist behavior. Animals, mechs, prisoners,
foreign visitors and non-colonist pawns SHALL gain no reserve-packing work. A pawn must be on a
map, have a positive food need and usable inventory, and be able to move, manipulate and feed
itself safely. Babies, downed pawns and other dependent patients SHALL remain under ordinary care.
No race shall receive new abilities merely because it is player-controlled.

Packing SHALL require current Food strictly above30%, retaining S024's selected trigger. Urgent
care, danger, drafting, forced orders, active feeding and higher-priority native duties SHALL not
be displaced for optional reserve collection. The feature SHALL not create hunger or change
world-caravan feeding and transport loading rules.

#### Scenario: Ordinary worker away from a caravan
- **WHEN** a capable colonist with50% Food and no personal reserve reaches an ordinary packing opportunity near suitable communal food
- **THEN** it can collect a reserve regardless of its work role or whether it will join a caravan

#### Scenario: Eligible child or slave
- **WHEN** a self-feeding capable colonist child or secure colony slave meets the same packing conditions
- **THEN** it can maintain an appropriate reserve without being required to have an adult-sized appetite

#### Scenario: Dependent or excluded pawn
- **WHEN** an animal, prisoner, visitor, mech, baby or dependent patient needs food
- **THEN** this feature adds no independent reserve-collection job and leaves ordinary feeding/care available

#### Scenario: Hunger and orders take precedence
- **WHEN** current Food is30% or lower, the pawn is drafted, or an urgent/forced duty requires it
- **THEN** optional packing does not preempt that state or job; ordinary eating and duties retain authority

### Requirement: MC02 Reserve amounts use actual appetite and whole portions

The default desired reserve SHALL equal one full Food bar's nutrition for the actual pawn,
independent of how full its current Food bar is. Refill SHALL be considered when eligible personal
reserve nutrition is at or below40% of that target and below the target itself. Existing suitable
personal food SHALL satisfy the target before collecting more.

For a chosen food, collect the largest whole-unit quantity that fits the remaining target, limited
by actual available units and shared capacity. When the reserve is empty and a single indivisible
meal exceeds the target, one such meal SHALL be allowed if it fits capacity. This exception SHALL
not authorize a second oversized meal or a top-up beyond the target when a smaller reserve already
exists. A useful partial pickup SHALL be allowed; no requirement to find a full0.8 nutrition stack
shall prevent collecting a smaller appropriate portion. No pickup is issued for zero useful units.

#### Scenario: Typical adult meal
- **WHEN** a pawn's target is1 nutrition, its reserve is empty and each suitable meal provides0.9 nutrition
- **THEN** it collects one meal, not two, and does not refill again merely because0.9 is below1

#### Scenario: Smaller appetite and an indivisible meal
- **WHEN** the target is0.35 nutrition, the reserve is empty and a0.9 nutrition meal fits the pawn's carrying allowance
- **THEN** one meal can be collected and retained without an automatic pickup/unload cycle

#### Scenario: Partial capacity
- **WHEN** the target is1, the reserve is empty and capacity permits only two suitable0.2 nutrition portions
- **THEN** at most two portions are collected; another opportunity can refill at the40% boundary only if useful units then fit

#### Scenario: No whole top-up fits the target
- **WHEN** a pawn already holds0.3 personal nutrition toward a target of1 and only0.9 nutrition meals are available
- **THEN** it retains the existing food and does not add an oversized top-up or repeatedly start a zero-quantity job

### Requirement: MC03 Reserve ownership distinguishes personal food from deliveries

Only food personally available to the pawn SHALL count as its reserve. Delivery cargo, committed
transport supplies, bill ingredients, another pawn's food and portions claimed for an incompatible
active use SHALL not satisfy or be converted into a personal reserve. Existing explicit keep or
inventory-stock choices SHALL remain authoritative and count suitable personal food once; they
SHALL not cause an extra automatic quota of the same food. Explicitly kept excess SHALL not be
released merely to meet the automatic target.

Automatic reserve quantities SHALL use the same physical portion ownership as S021. Split, merge,
transfer and partial removal SHALL preserve the actual kept/cargo quantities. Pending eating or
care use and pending explicit food-stock collection SHALL be reconciled before further reserve
packing; no simultaneous requests shall count the same incoming portion twice.

#### Scenario: Meals are delivery cargo
- **WHEN** a colonist carries five meals assigned for delivery but no personally available food
- **THEN** the meals remain delivery cargo rather than being quietly kept as the colonist's reserve

#### Scenario: Explicit stock already supplies food
- **WHEN** a native inventory-stock choice keeps two suitable meals for a pawn
- **THEN** those meals satisfy the automatic target without a third automatic meal or removal of the explicitly kept second meal

#### Scenario: Identical meals merge
- **WHEN** one reserved meal merges with four delivery meals
- **THEN** one reserved and four cargo units remain accounted for, and delivery cannot unload the reserved unit as an extra cargo item

### Requirement: MC04 Packing respects food suitability and communal access

Packing SHALL consider only fresh, presently edible, haulable prepared food of at least awful-meal
preference that gives the actual pawn positive finite nutrition. Drugs, medicine, baby food for a
dependent recipient, raw-food substitutes, corpses, plants, dispensers and food still held by other
pawns or vehicles SHALL not become reserve sources. A modded prepared meal can qualify normally.

The pawn's diet, ingredients, ideology, title and ordinary non-desperate food permissions SHALL
apply. Food with an actual negative food-ingestion mood effect for that pawn SHALL not be selected
for optional reserves; a generic thought stage for a different situation shall not reject an otherwise
suitable meal. Packing SHALL never bypass a food policy or grant a starvation exception.

Map sources must be reachable within20 cells, non-forbidden, socially proper, reservable and free
of incompatible claims. Communal food shortage SHALL NOT suppress otherwise eligible packing.
There is no minimum colony-wide nutrition total or days-of-food cutoff. Actual food availability,
suitability, claims and carrying capacity still limit pickup; ordinary hunger and care retain priority.

#### Scenario: Communal shortage
- **WHEN** three spawned colonists have less than4.5 communal nutrition
- **THEN** an otherwise eligible colonist can still pack an available suitable meal; ordinary eating and feeding remain possible

#### Scenario: Diet or title rejects the meal
- **WHEN** an accessible meal conflicts with the pawn's applicable diet, ingredients or title requirements
- **THEN** it is not packed merely because it is nearby or the pawn has no reserve

#### Scenario: Diet-specific nutrition
- **WHEN** the same prepared meal supplies different nutrition to two admitted colonists
- **THEN** each reserve amount and pickup quantity uses the nutrition that colonist would actually receive

#### Scenario: No suitable prepared food
- **WHEN** only raw ingredients, medicine, nutritious drugs or food in another pawn's inventory are available
- **THEN** reserve collection waits instead of replacing the meal requirement with those sources

### Requirement: MC05 Shared preferences do not create another food search policy

Within the eligible nearby map sources, ordinary food preference scoring SHALL choose food with
S024 perishable-meal and S057 preserved-food contributions applied once when enabled/applicable.
Otherwise equal candidates SHALL prefer nearer reachable food with stable ties. Existing suitable
reserve food SHALL not be swapped simply because a more attractive meal appears.
S111 dining-room preference and S055 donor/shuttle fallback SHALL not extend into reserve packing;
their ordinary eating scopes remain unchanged. Shared candidate facts SHALL not replace current
access, suitability, capacity or ownership checks.

#### Scenario: Better meal appears after packing
- **WHEN** a pawn has a sufficient suitable reserve and a more attractive meal is placed nearby
- **THEN** it keeps the reserve rather than repeatedly exchanging food

#### Scenario: Food in a dining room versus a nearer source
- **WHEN** reserve packing compares two eligible map foods and one is in a dining room
- **THEN** that room alone gives no reserve-packing priority; the composed ordinary score and tie rules decide

### Requirement: MC06 Collection is physical and capacity-limited

The pawn SHALL travel to the selected source, reserve and recheck the real available portion, then
physically transfer only the accepted whole units. All owned pickups SHALL obey S021's combined
mass and volume limits, including gear, personal supplies, cargo and hands as specified there.
No medicine, bedroll, carried baby or other protected item SHALL be discarded to make room.
Unknown or nonpositive measurements SHALL not authorize unlimited space or nutrition.

Accepted food SHALL retain ingredients, condition, spoilage and S041 food-poisoning history.
Rejected units SHALL remain recoverable with their actual holder; no success, nutrition or reserve
credit is granted for an attempted but failed insertion. Food discovery SHALL have no transfer side effect.

#### Scenario: A search without pickup
- **WHEN** reserve availability is inspected repeatedly without a pawn reaching and taking food
- **THEN** no stack or inventory changes and no reserve ownership is granted

#### Scenario: Capacity decreases en route
- **WHEN** equipment or injury reduces remaining capacity before collection
- **THEN** only the newly fitting useful quantity is taken, or the attempt ends without displacing protected supplies

#### Scenario: Failed insertion or shared contention
- **WHEN** another pawn claims the food or inventory insertion accepts only part of the request
- **THEN** only actually accepted units become this pawn's reserve and all remaining food stays physically recoverable without duplication

### Requirement: MC07 Retention and changed eligibility do not loop

Automatic unloading SHALL retain the intended personal reserve, including the one oversized-meal
exception. When the target decreases, qualifying food changes, or reserve carrying is disabled,
only automatic reserve protection SHALL be reduced; explicit keep, active-use and delivery ownership
SHALL remain intact. Release automatic surplus newest-acquired first, without replacing an already
suitable reserve for cosmetic preference. Released food SHALL follow ordinary safe unloading when
possible, not vanish or teleport. Lack of storage SHALL not trigger repeated unload/refill attempts.

If suitable unclaimed food already in inventory can fill the reserve, it SHALL be used before a
new pickup, with existing reserved portions retained first and stable ties for other food. Food
that is no longer eligible SHALL stop satisfying the target without automatically being consumed
or changing the pawn's diet to permit it.

#### Scenario: Diet changes after packing
- **WHEN** an automatic reserve becomes forbidden by a changed food policy
- **THEN** it stops satisfying the reserve, its automatic protection is released for ordinary unloading, and a replacement is sought only within current permissions and capacity

#### Scenario: Reduce the target
- **WHEN** a player lowers the reserve target while a pawn has automatic and explicitly kept food
- **THEN** only unnecessary automatic protection is released, explicit keep remains and the food is physically unloaded through an ordinary valid action if available

### Requirement: MC08 Explicit unloading pauses replacement predictably

Native inventory context SHALL show automatic meal carrying status and allow the player to pause
or resume it for that pawn. A pause SHALL stop new automatic reserve collection while retaining
existing food and its protection. Explicit pending unload/drop/give orders for personal reserve
food SHALL suspend replacement while pending. Once such a player order actually removes a reserve
portion, meal packing SHALL remain paused until the player resumes it, with that result shown in
the inventory context. Cancelling before any removal SHALL leave the previous pause state unchanged.
Automatic eating, authorized sharing and ordinary spoilage SHALL not create a player pause.

#### Scenario: Player drops a reserved meal
- **WHEN** the player deliberately drops a colonist's reserved meal
- **THEN** the actual portion is dropped, reserve packing is visibly paused for that pawn, and the next packing check does not pick it straight back up

#### Scenario: Cancel before unloading
- **WHEN** a pending reserve unload is cancelled before any portion leaves the pawn
- **THEN** the existing reserve and prior pause state remain, with no new pause or replacement pickup caused by the cancelled order

#### Scenario: Resume deliberately
- **WHEN** the player resumes meal packing in that pawn's inventory context
- **THEN** the next ordinary eligible opportunity can replenish its reserve without creating food or immediately interrupting a current duty

### Requirement: MC09 Eating and sharing retain their existing meaning

Personally reserved meals SHALL remain available to ordinary ingestion, with native nutrition,
duration, thoughts and eating-place selection. A reserve does not force its immediate consumption.
S055 can still borrow an eligible kept meal under its existing native-first and eater-eligibility
rules; reserve status alone SHALL not forbid that sharing. Current-use and transport claims remain
protected. A donor SHALL refill only from eligible map stock, never steal the borrowed portion back
or borrow from another pawn merely to refill a reserve. S021's cargo-first removal rule SHALL
continue when a removal from a mixed stack identifies no specific portion.

#### Scenario: Another colonist needs the kept meal
- **WHEN** S055's eligible food-sharing fallback takes a personally reserved meal for immediate eating
- **THEN** the actual portion transfers once, the donor's reserve decreases without a player pause, and the donor cannot reclaim that current-use portion

#### Scenario: Interrupted eating
- **WHEN** eating a carried meal is interrupted
- **THEN** S024's current-use recovery preserves the actual remaining portion within shared capacity or safely places it, without duplicate reserve credit or reversal of a player drop order

### Requirement: MC10 Configuration and entity state are distinct

The shared **Supplies and work / Personal meals** section SHALL offer **Reserve target (full Food
bars)**, built-in1, allowed0–3 in0.25 steps. Zero disables automatic reserve collection and releases
only automatic protection under MC07. This provisional bounded range SHALL be explained as a
nutrition target rather than an item count; indivisible meals and actual capacity can change the
number carried. Use the common global default / Current save / Use global load, store, resolve and
render path. New saves use global; inherited overrides stay disabled; main-menu save controls and
their callbacks stay inactive. Invalid stored values SHALL use the built-in default with a bounded
diagnostic; invalid edits SHALL be rejected with an explanation. Changes apply at the next safe reserve evaluation without immediate
dropping, invalidating current ingestion or discarding pending physical transfers.

Pawn pause state and portion ownership SHALL persist with their gameplay entities, not as settings.
After save/load or map transfer, rebuild current eligibility and capacity before a new action; do not
replay completed transfers or grant a free starting reserve. All new labels, explanations and status
SHALL support the seven baseline languages. No new bottom tab or generic food-management window is added.

#### Scenario: Global default and save override
- **WHEN** one save inherits target1 and another overrides it to2, then the global target changes to1.5
- **THEN** the first save follows1.5 and the overridden save remains2, with no automatic item movement in either closed save

#### Scenario: Main menu and reload
- **WHEN** settings are opened without a game or a saved paused pawn is reloaded
- **THEN** save-specific editors remain disabled at the main menu, and reloading retains the pawn's pause and actual food without performing a second pickup

### Requirement: MC11 Guards and recovery isolate unavailable packing

Every owned patch SHALL require its documented minimal semantic target conditions, including
the shared quantity/capacity/retention cooperation needed for this opportunity. Compatible unrelated
patches SHALL be allowed. An unsupported overlapping packer, missing target, ambiguous shape or
failed atomic installation SHALL disable the affected owned opportunity without a second packing
loop or partial new reserve behavior, with a bounded startup warning naming the patch/spec, reason,
unavailable behavior and fallback. Independent native/provider behavior SHALL remain intact and
shall not be described as owned compatibility acceptance.

On runtime invalidation or cancellation, already-held food SHALL remain recoverable with its actual
owner, stale reservations SHALL be released and another valid opportunity can be considered later.
Repeated failures SHALL not cause same-tick job churn or starve ordinary eating and work.

#### Scenario: Retention integration is unavailable
- **WHEN** the owned pickup path could run but its required retention contract cannot be admitted
- **THEN** the complete owned reserve opportunity stays inactive with a warning instead of repeatedly acquiring food that another rule immediately unloads

#### Scenario: Source disappears during approach
- **WHEN** the target food is removed or becomes forbidden before pickup
- **THEN** the attempt ends without reserve credit, stale claims are released and ordinary eating/work remains available without repeated immediate failed jobs
