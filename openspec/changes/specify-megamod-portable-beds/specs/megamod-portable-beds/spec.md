# Portable beds

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
S073 observable draft only. Nothing implemented or accepted.

## ADDED Requirements

### Requirement: Portable beds improve eligible ordinary rest
PB01. A player-controlled colonist, including a colony slave permitted to use the selected bed, SHALL be able to deploy a real portable bed when ordinary rest is currently permitted.
Deployment starts enabled away from player-home maps and disabled on home maps. It does not grant
rest to visitors, animals, prisoners, mechs, drafted pawns or pawns whose current duties/condition
forbid that rest or forbid bed use. Preserve timetable, disturbance, sleep-blocking conditions and
ordinary rest continuation. Do not replace a medical-rest instruction or S062 field-care decision.

Keep an already selected usable bed by default. The optional distance rule permits replacing an
ordinary selected bed at a straight-line distance **at least100 cells**, configurable0–300, when
enabled; it starts disabled. This measures the selected bed, not necessarily the pawn's owned bed.
It does not make an unsuitable portable bed eligible or override medical care. With no valid
deployment, retain ordinary rest, including the admitted S115 seat fallback before ground rest.
Eligible portable deployment takes precedence over that seat fallback. No new rest, comfort, healing or immunity bonus is
added beyond the actual bed and ordinary modifiers.

#### Scenario: Traveler sleeps in a carried bedroll
- **WHEN** an undrafted colonist on a non-home map needs and can ordinarily take rest, has no selected usable bed and carries an eligible bedroll
- **THEN** the colonist can physically deploy it and sleep in that bed, retaining its actual condition, material and quality.

#### Scenario: Existing bed and distance boundary
- **WHEN** the selected ordinary bed is100 cells away and the distance option is off, then enabled at100
- **THEN** the original bed is kept while off and a valid portable alternative is eligible while on; a selected bed at99 cells is retained.

#### Scenario: Ground rest is intentional
- **WHEN** a pawn's duty permits ground rest but forbids bed use, or the pawn is undergoing medical rest
- **THEN** portable deployment does not convert that rest into an otherwise prohibited bed job or redirect treatment.

### Requirement: Selection uses real spare beds without mutating stock
PB02. Selection SHALL prefer a usable bed in the sleeper's inventory, then reachable permitted ground stock, then genuinely spare carried stock from an eligible same-faction pawn on the same map.
Within own inventory retain ordinary inventory order; prefer nearer ground/donor candidates by
grid walking distance without diagonal shortcuts, with stable ties. Recheck actual paths and all
permissions before collection. A compatible minified ordinary bed is eligible, not only a bedroll;
medical, prisoner-incompatible, special-purpose-only, forbidden, already installing or already
committed cargo is ineligible. Respect body size, sleeping slots, ideology, faction, reservations,
allowed area and provider permissions. An unassigned packed ordinary bed can be considered for
the colonist/slave owner-type setup described below; this does not authorize conversion of a claimed,
medical or prisoner bed. Do not commandeer a fixed installed bed.

A donor keeps its protected personal quantities and any bed needed for its own ordinary sleep.
Unprotected surplus can be borrowed. A pack animal can supply an uncommitted compatible bed it
does not use. Having an assigned bed on another map alone does not make a sole bed spare. A partner's
sole bed can serve both only when it has sufficient compatible shared sleeping space; do not take
a single bed on the strength of the relationship alone. One physical bed has one transport custodian,
even when multiple sleepers share it. No automatic remote return to the former donor is promised.

Opening UI or searching for work SHALL not drop, transfer, reserve, create or install a bed. An
accepted job rechecks availability before changing anything. Beds selected for a forming caravan
or another transport manifest remain committed to that order until it is released.

#### Scenario: Search finds no placement
- **WHEN** a spare bed is found on another pawn but no valid placement exists
- **THEN** the donor keeps the bed and no installation order or reservation is left behind.

#### Scenario: Another pawn needs its sole bed
- **WHEN** a donor's only portable bed is protected personal stock or needed on this map despite an assigned distant home bed
- **THEN** it is not borrowed automatically and the sleeper keeps the original rest fallback.

#### Scenario: Shared partner bed and committed cargo
- **WHEN** a partner has one unprotected double bed usable by both, while another bed is selected for caravan loading
- **THEN** the double bed can provide one shared installation with one custodian, and the loading bed remains untouched.

### Requirement: Deployment preserves the map and the physical bed
PB03. Deployment SHALL move the selected real bed through permitted collection and installation, charging its full mass against the same inventory-and-hands capacity as personal supplies and hauling cargo.
No private bed inventory allowance, teleport, duplicate item or free manufactured bed is permitted.
Prefer qualifying indoor placements within4 cells of the bed's current held location, then12;
then qualifying outdoor placements within4, then12. Use the two nonredundant footprint orientations.
Keep selection local and require an actual usable path; an unreachable candidate never counts as
a usable zero-distance result. Actual sleeper access must also remain valid. These bounds are search
limits, not assurances of safe sleep.

The complete footprint must remain in bounds, outside zones and installed buildings, and usable
under ordinary bed placement, reachability, allowed-area, temperature, fire and vacuum restrictions.
Deployment must not clear plants, destroy/move unrelated things, erase player plans or use an
install-over replacement to make room. A transient pawn does not justify a global change to bed
construction: wait for a valid clear placement or use another candidate. The existing construction
enclosure safeguard excludes installing already-built furniture; this operation adds no such promise.
Deliver and unfold the existing bed without construction materials, a new quality roll or an added
fabrication timer; removal afterward uses the work below.

After successful installation, an unassigned ordinary bed is set for colonists or colony slaves as
needed for its intended sleeper, then claimed only if all intended sleepers are eligible under that
owner type and their corresponding ordinary ownership allowance is free. If an intended sleeper
already has a claim in that allowance, preserve it and use the eligible vacant temporary bed as a
rest target without adding a second ordinary claim; physical custody/recovery remains tracked.
This permits a slave to use a packed bed that reverted to colonist use while uninstalled.
Do not convert medical/prisoner beds, remove existing claims or override a player edit to manufacture
eligibility. A changed ownership choice during the attempt wins and requires fresh validation.

Interruption, failed reservation, drafting, changed access or load releases only this attempt's
temporary reservations/installation intent. Keep the bed at its actual holder/location, or use
normal permitted physical placement when recovery is necessary. Never destroy the item to roll back
a failed handoff. Repeated attempts with unchanged blocking conditions must not churn installations,
drop/pickup cycles or notifications; changed relevant conditions or an explicit retry permits recheck.

#### Scenario: Shelter and blocked footprint
- **WHEN** a valid indoor location exists within12 cells and a closer outdoor location exists, but part of the indoor footprint overlaps a zone
- **THEN** the invalid indoor location is rejected; another valid preferred location or the outdoor fallback is used without altering the zone.

#### Scenario: Full capacity or changed destination
- **WHEN** collection would exceed shared capacity, or a tree, building or player plan now occupies the intended footprint
- **THEN** the job leaves the bed physically recoverable and uses another valid opportunity or the original rest fallback, without deleting the obstruction or item.

#### Scenario: Interrupted handoff
- **WHEN** a deployment is interrupted after pickup but before installation and the game is saved and loaded
- **THEN** the same physical bed remains accounted for once, stale temporary orders are released, and only still-authorized work can resume.

#### Scenario: Colony slave deploys an ordinary packed bed
- **WHEN** an eligible colony slave deploys an unassigned ordinary packed bed that currently defaults to colonist use
- **THEN** the installed bed is set for slaves and claimed after validation; incompatible sharing or a later player ownership edit is not overridden.

### Requirement: Recovery respects sleep completion and occupied beds
PB04. The transport custodian SHALL recover the same temporary bed after its rest episode ends through permitted physical uninstalling and collection.
A brief disturbance, temporary sleep blocker or job search does not by itself end the episode and
trigger pack/redeploy loops. Once ordinary rest has ended and no current sleeper or valid ongoing
shared rest needs the bed, recovery becomes eligible through normal work selection. Respect current
forbidding, reachability, allowed areas, reservations, worker capability and S061 roof preparation.
Do not force an occupant out or destroy a co-owner's bed assignment merely to pack it. A deliberate
new assignment or reuse for unrelated sleep cancels automatic pickup and leaves an ordinary bed.

Use the bed's normal uninstall work and applicable speed modifiers: the inspected ordinary default
is200 work, reduced at Construction Speed×1.7 per game tick. Preserve earned work across save/load,
with no repeated completion rewards or experience from waiting. Completion retains the original
bed, its material, quality, damage and applicable contents. If another worker already packed it,
recover that exact nearby reachable packed item rather than uninstalling or creating it again.
Only successful physical collection clears pending recovery. If capacity becomes insufficient,
retain the installed/packed bed and an actionable waiting reason; do not split it into an unowned
lost object. No automatic carrying beyond capacity is granted to a drafted pickup command either.

Death, departure, destruction or map removal releases obsolete worker reservations without moving
the bed remotely. Surviving beds remain where they physically are. Disabling automation stops new
deployment; already committed work can finish safe recovery or remain visibly available for a
permitted manual pickup. Feature disablement is not an instruction to wake occupants.

#### Scenario: Disturbed then asleep again
- **WHEN** a sleeper briefly wakes while the same rest episode and bed remain valid
- **THEN** the bed remains deployed and the pawn can resume sleep without a pack/redeploy loop.

#### Scenario: Shared bed still occupied
- **WHEN** the custodian finishes resting but a second valid sleeper still uses the double bed
- **THEN** pickup waits; it does not eject that sleeper or replace the bed with two portable items.

#### Scenario: Already packed and now too heavy
- **WHEN** another worker packed the tracked bed and the custodian no longer has enough capacity
- **THEN** the exact packed bed remains at its real location with recovery pending, and later collection requires travel, permission and sufficient capacity.

#### Scenario: Saved removal and roof risk
- **WHEN** recovery is saved partway through removal or waits for required roof preparation
- **THEN** earned work remains, the bed stays physical, and completion occurs once only after the applicable preparation and access conditions hold.

### Requirement: Caravan distribution and personal supplies share one physical account
PB05. Caravan-entry redistribution SHALL offer real uncommitted spare beds to eligible free colonists without a usable carried bed, using each recipient's own compatibility and shared remaining capacity.
Keep donor sleep needs and personal retention, and transfer at most one suitable bed per recipient
per entry. Do not use the first colonist's body size as proof for everyone. Failed transfers leave
the original holder intact. Preserve the selected native drop, unload, shuttle and manifest behavior;
distribution does not make items exempt from those choices or recreate items moved by entry.
Away-world sleeping and caravan capacity remain native, with no new sleep engine or extra slots.

An explicitly stocked bed keeps its S062 target and retained role while temporarily deployed for
its custodian's rest. Show the actual carried count separately from the one deployed commitment.
That commitment covers the corresponding shortfall during this episode, preventing an automatic
replacement pickup; it is not a carried unit, freely available colony stock or haul cargo. End the
credit when the bed is lost, destroyed, deliberately released or no longer committed. Count each
physical object once in shared supply data. Personal retention does not duplicate the weight of
one item moving between inventory and hands, nor shield unrelated cargo from ordinary unloading.

#### Scenario: Different-sized arriving colonists
- **WHEN** a caravan carries a spare bed usable by its first colonist but too small for another
- **THEN** that other colonist is not given an unusable bed, and a compatible recipient still needs sufficient real capacity.

#### Scenario: Drop supplies on entry
- **WHEN** the selected arrival behavior drops supplies on the ground
- **THEN** redistribution does not secretly retain or recreate a second copy in a colonist's inventory.

#### Scenario: Stocked bed is deployed
- **WHEN** a colonist with a personal target of one deploys that bed for sleep
- **THEN** the UI can show zero carried and one deployed, no second bed is restocked for the covered shortfall, and the deployed bed is not advertised as free colony stock.

### Requirement: Contextual controls and common settings expose the current intent
PB06. Portable-bed configuration SHALL use common global-default and per-save settings under Pawns / Rest and portable beds.
Rows in order: automatic portable beds true; use on home maps false; replace a distant selected
ordinary bed false; minimum distance100, whole cells0–300; release bed claim on map departure false;
reclaim occupied remembered home slot false. Explain the effects and recovery limits. Keep the
distance value visible but inactive while its rule is off. The release row is shared with S115, not
duplicated. Values apply at the next relevant decision,
without executing jobs from UI rendering or interrupting valid current sleep.

Every row shows Global default, Use global and Current save. Missing save entries inherit the current
global value. Inheritance disables the save editor; the main menu disables both save editor and
Use global, including callbacks. Keep dormant overrides and restore them when inheritance is turned
off; first overrides start from the current effective value. Switching saves does not copy values
or remembered pawn/bed state between them. No exception to common inheritance is selected.

Selected temporary beds show their custodian/shared sleepers and let the player cancel automatic
pickup, leaving the physical bed and ordinary assignments intact. A pending portable job exposes
its waiting reason and **Retry** on the custodian and, when present, the tracked bed/installation.
Retry rechecks current conditions through ordinary work selection; it grants no forced-order
permission, roof-risk exception, occupancy override or extra capacity. A drafted custodian has a contextual
Pick up bed command for its tracked bed, with disabled reasons for occupancy, capacity, permission
or unavailable safe recovery. An accepted command uses normal movement/removal, not instant inventory
insertion. Successfully forcing removal has the same explicitly explained mountain-roof risk exception
as S061; other safeguards remain. Home-memory controls belong to the remembered bed/pawn. These are
entity actions, not mod settings. No new bottom tab, debug button or bottom-right toggle is added.

#### Scenario: Cancel temporary pickup
- **WHEN** the player cancels automatic pickup on a temporary bed
- **THEN** the bed stays installed with its ordinary owners, its special pickup commitment is released, and the pawn is not silently unassigned from another bed.

#### Scenario: Drafted pickup
- **WHEN** the player uses Pick up bed on an eligible drafted custodian
- **THEN** the pawn performs one permitted physical pickup job; an occupied or over-capacity bed instead explains why the command cannot start.

#### Scenario: Retry is not a forced pickup
- **WHEN** the player selects Retry on a waiting ordinary recovery while mountain-roof cancellation or an occupancy restriction still blocks it
- **THEN** the reason is refreshed and the bed remains intact; Retry does not accept forced-removal risk or eject a sleeper.

#### Scenario: Inheritance and main menu
- **WHEN** one save inherits and another overrides the distance, then the global value changes
- **THEN** only the inheriting save changes, dormant overrides survive reload, and main-menu callbacks cannot alter either save's settings or entity state.

### Requirement: Unavailable integrations preserve ordinary play and recoverable state
PB07. Each enabled integration SHALL activate only when its documented required behavior remains available, permitting unrelated compatible changes by other mods.
Missing or uncertain prerequisites skip the affected enabled patch/group and log one bounded startup
warning per skipped patch identifying target requirement, reason, lost behavior and fallback. Ordinary
rest and manually installed beds remain available. A failed dependency must not leave deployment
active without safe identity, ownership and recovery support. Unrelated admitted home memory or
tailoring can remain available when their own contracts hold.

When an original Use Bedrolls provider is active, it is the sole provider of overlapping new
automation/recipes. Do not spoof its identity, alter its settings or remove its changes. Saved owned
jobs stop initiating work under uncertain authority; retain their items and readable status for
permitted explicit cleanup. A missing recovery prerequisite leaves the job stopped and physical
items available to ordinary manual handling, not blindly replayed through the source provider.
Expected messages from an owned deliberate packing/claim release can be suppressed narrowly;
unrelated bed loss, destruction and other owners' changes retain their normal messages.

Source settings/jobs/remembered assignments are not automatically imported. Before provider removal,
finish or cancel source jobs and make a backup; no safe mid-save uninstall or blanket compatibility
claim is made. Unknown optional bed/level semantics disable only the affected candidate/adapter,
with an explanation, rather than pretending a flat-map or generic bed is supported.

#### Scenario: Compatible and incompatible changes
- **WHEN** another mod changes an unrelated bed effect, or instead removes required safe installation behavior
- **THEN** the unrelated change remains compatible, while the latter disables affected deployment with a bounded warning and preserves normal rest.

#### Scenario: Original returns during an owned save
- **WHEN** the original provider is enabled with owned temporary beds already present
- **THEN** new overlapping owned automation stays off, existing physical beds remain, and stale owned jobs do not execute under the other provider's authority.

#### Scenario: Real bed destroyed
- **WHEN** an unrelated assigned bed is destroyed while this feature is enabled
- **THEN** its ordinary lost-assignment warning remains; only the expected owned deliberate transition is eligible for suppression.
