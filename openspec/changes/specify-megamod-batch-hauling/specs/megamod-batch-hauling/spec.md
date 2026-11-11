# Capacity-limited batch hauling

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). **1.7 beta**.
Source S021: Pick Up and Haul - Continued, compared with Reworked and Armful, plus the current
While You're Up hauling intersection. Specification only; nothing implemented or accepted in game.
The selected composition is recorded in the proposal/design and refinement decision record;
it is not a claim of identical behavior or complete binary parity with any source.
Combined mass/volume, corrected starting threshold and finite trips remain explicit proposed
refinements of the user's intent. PH-Q1–4 retain implementation research and native proof.

## ADDED Requirements

### Requirement: Eligible haulers collect several items without gaining new abilities
The mod SHALL let eligible player-controlled haulers collect multiple item types in one inventory trip while retaining their ordinary work, training, manipulation, access and reservation restrictions.
Player humanlikes, including slaves where native hauling permits them, qualify. Allowed trained
animals and colony mechs qualify only with a usable inventory and positive supported capacity;
the feature does not create pack capacity, training or work abilities. Guests, quest lodgers,
prisoners and nonplayer pawns do not gain this work. Ordinary Hauling priority is retained and no
extra work column is added. S008 governs urgent work separately. Goods already owned by a cart,
other carrier, active bill or another protected provider workflow are not treated as loose spare
cargo merely because their position is visible. Existing reservations and supported personal
loadouts remain authoritative. Unknown ownership excludes the affected candidate, not all
ordinary unrelated goods.

#### Scenario: Several loose items
- **WHEN** an eligible hauler starts work on loose items of different types with suitable storage
- **THEN** the pawn walks to and collects eligible quantities before delivering them
- **AND** no item moves remotely or appears in storage before the pawn delivers it.

#### Scenario: Trained pack animal versus ordinary pet
- **WHEN** one allowed animal can natively haul and has usable pack capacity while another lacks it
- **THEN** only the capable animal can collect an inventory batch
- **AND** existing single-load hauling remains available wherever native rules permit it.

#### Scenario: Excluded or incapable worker
- **WHEN** a prisoner, guest, quest lodger or pawn incapable of hauling encounters eligible items
- **THEN** this feature does not grant that pawn inventory-hauling work or bypass its restrictions.

#### Scenario: Nearby cart or reserved bill materials
- **WHEN** a nearby visible stack belongs to a supported cart or is reserved for another active bill
- **THEN** an ordinary extra pickup leaves it with that owner unless the provider's ordinary authorized transfer explicitly permits the requested quantity.

#### Scenario: Provider ownership cannot be established
- **WHEN** a known external cargo provider's ownership information is unavailable for a candidate
- **THEN** the candidate is withheld with an explanation and ordinary unrelated loose goods remain eligible.

### Requirement: A batch uses one actual carrying allowance
Every owned inventory pickup SHALL fit both the pawn's current mass allowance and carrying-volume allowance after counting existing inventory and hand-carried items together.
Mass includes worn/equipped gear, personal supplies, hauled inventory and hands once each. Volume
includes all inventory and hands once each; worn/equipped gear is not inventory volume. The mass
allowance follows the game's current inventory capacity, whose inspected unmodified default is
35 kilograms per unit body size. The volume allowance follows the pawn's current Carrying Capacity
and each item's ordinary volume. Valid supported providers can alter these actual limits; there is
no multiplier or second allowance for hauling. Each physical stack still obeys its own maximum.
Only whole units that fit and have destination space are collected. Zero/unknown/invalid capacity
cannot mean unlimited, and nonpositive or unknown item measurements cannot create an unbounded batch.

#### Scenario: Existing supplies and a partial pickup
- **WHEN** a pawn has a 35 kg allowance, 10 kg of gear and personal supplies, and requests ten 3 kg items with sufficient volume and destination space
- **THEN** at most eight items are added, leaving the remaining items on the ground.

#### Scenario: Hands do not add another allowance
- **WHEN** the same pawn already holds a 6 kg load in its hands as well as 10 kg of gear and supplies
- **THEN** at most six of those 3 kg items can be added
- **AND** shifting an item between inventory and hands does not free a second mass allowance.

#### Scenario: Lightweight items reach the volume limit
- **WHEN** remaining mass permits ten units but remaining carrying volume permits only three
- **THEN** at most three are added, including the space already occupied by personal supplies.

#### Scenario: Capacity changes during travel
- **WHEN** injury, equipment, item state or a supported provider reduces capacity before the next pickup
- **THEN** the pawn recomputes the quantity, adds nothing beyond the new limit and turns to unloading existing cargo
- **AND** it does not destroy items or discard personal supplies merely to satisfy the new limit.

#### Scenario: Unusable measurements
- **WHEN** a candidate or capacity cannot be measured safely, including a nonpositive item mass or volume
- **THEN** that item is not added to an inventory batch and the player can use supported ordinary hauling
- **AND** the feature cannot collect indefinitely because an item appears to cost no capacity.

### Requirement: Batch collection follows a finite nearby route
The mod SHALL start with the chosen hauling item and consider nearby eligible items using a search distance equal to the greater of 12 cells and half the straight-line distance from that first item to its chosen destination.
Subsequent candidates are considered nearest to the last accepted pickup location within that
distance; equal-distance ties remain stable for the same unchanged situation. A trip considers a
finite set of candidates available when it is planned and then unloads. Newly arriving items wait
for a later trip rather than extending the current collection forever. Forced permission applies
only to the explicitly ordered item; extra pickups pass ordinary automatic eligibility. Items
already in valid best storage are not extra pickups merely because they are nearby.
S125's explicitly authorized, work-bound equal-priority supply relocation is the sole exception:
only its eligible supplies and destinations can participate, under its surplus and trip rules.
It does not authorize equal-priority moves by an ordinary batch.

#### Scenario: Longer delivery permits a wider collection search
- **WHEN** a seed item is 40 cells from its chosen destination
- **THEN** the additional-item search distance is 20 cells from each last accepted pickup
- **AND** a seed only 10 cells away still uses the 12-cell minimum.

#### Scenario: New arrivals do not postpone delivery forever
- **WHEN** more haulable items keep appearing while a planned trip is collecting
- **THEN** the pawn finishes or safely abandons that finite collection and unloads
- **AND** new items remain available to later work.

#### Scenario: A forced seed does not force every nearby item
- **WHEN** the player explicitly orders a valid seed and nearby items fail normal access or hauling rules
- **THEN** the extra items are left alone and the explicit order does not broaden their permissions.

### Requirement: A constrained hauling opportunity retains its purpose
When another owned feature requests a haul on the way to a pending job, the batch SHALL obey that
opportunity's permitted items, quantities, destinations and whole-trip travel limits as additional
restrictions on ordinary collection. Extra pickups and replacement destinations cannot silently
turn it into unrestricted hauling. The same allowance, reservations and cargo accounting apply;
one opportunity does not repeatedly insert another ahead of the pending job. S125 owns its full
opportunity and supply-relocation rules; S024's overlapping bill detour shares that decision instead
of scheduling a second trip. Pending bill material, worker and quantity choices remain those of
their owning production contracts. If no qualifying haul remains, continue the still-valid parent
job or let ordinary scheduling reconsider it, retaining any actual cargo for safe recovery.

#### Scenario: A nearby item would exceed the opportunity's travel limit
- **WHEN** an extra item fits capacity but collecting and delivering it would exceed the pending job's allowed detour
- **THEN** it is left for later work and the existing constrained trip does not expand its purpose.

#### Scenario: Two features notice the same bill detour
- **WHEN** the owned While You're Up and Common Sense behaviors both identify the same pending bill's storage opportunity
- **THEN** at most one constrained trip is scheduled before that bill attempt, without modifying either feature's saved preference.

#### Scenario: The detour becomes unusable
- **WHEN** its allowed destination or pending job becomes invalid after a pickup
- **THEN** further constrained collection stops, actual cargo remains recoverable and no recursive detour is created to rescue the obsolete plan.

### Requirement: Destination capacity is shared and rechecked
The mod SHALL collect only quantities allocated to valid better storage, or S125's explicitly authorized equal-priority supply destination, accounting for real remaining space, accepted item types and other active delivery commitments.
Shelves, stockpiles and supported containers retain their priorities and physical limits. Unlike
items can occupy different shelf stack spaces without being merged into one stack. Existing stack
contents, incoming quantities and per-item limits are counted once. S005 linked groups share
settings but do not duplicate physical space. S049's owned refill thresholds and storage-wide
stack ceiling constrain the same acceptance calculation directly; they do not require a patch
against this feature. Unsupported special storage uses its verified ordinary route instead.

#### Scenario: Work-bound equal-priority exception
- **WHEN** S125 authorizes selected surplus supplies for a distinct equal-priority store nearer the pending work
- **THEN** that constrained batch can allocate only its permitted quantities and destination space
- **AND** an ordinary batch without that authorization still cannot move already-valid supplies between equal-priority stores.

#### Scenario: Two haulers compete for the final space
- **WHEN** only five units fit and two pawns plan deliveries to that storage
- **THEN** their combined committed pickups do not exceed those five units
- **AND** neither treats the other's reserved space as free.

#### Scenario: Mixed shelf contents
- **WHEN** a shelf has separate remaining spaces for a shirt and a weapon
- **THEN** a qualifying trip can deliver both to their valid spaces without treating them as stackable together.

#### Scenario: Linked storage and refill limits
- **WHEN** a linked group has physical room but its applicable automatic-refill limit allows only four more units
- **THEN** batch planning permits at most four across that group and its active delivery commitments
- **AND** it does not bypass the limit through a different linked member or a later single-load fallback.

#### Scenario: Storage changes after planning
- **WHEN** a destination fills, changes filter or priority, is removed or becomes inaccessible before pickup or delivery
- **THEN** the pawn rechecks current valid destinations and reduces or cancels unsupported quantities
- **AND** already collected cargo remains physical and recoverable under the cargo-care contract.

### Requirement: Urgent orders compose with batching
An owned urgent trip SHALL collect only items carrying the same outstanding urgent-hauling intent and SHALL retain that intent through pickup, partial collection and interruption until actual delivery, consumption, destruction or cancellation.
S008's work priorities and eligibility remain authoritative. Ordinary batch trips do not acquire
urgent priority merely by finding urgent items. Composition uses owned S008 directly; an original
Allow Tool API/delegate adapter or compatibility handoff is outside scope.
Exact urgent quantities, held cancellation and mixed-stack protection apply to owned S008.
The pawn's Gear view shows outstanding urgent cargo and permits
cancelling a chosen held quantity without discarding it; ordinary cargo recovery remains. Cancelling
an urgent order on a loose stack clears its outstanding urgent quantity, not its physical items.
Merging urgent material with ordinary or cancelled units never promotes those other units. Both
batch pickup and ordinary urgent fallback take at most the outstanding urgent quantity. If safe
quantity-limited pickup is unavailable, defer that mixed stack with an explanation. Explicitly
issuing a new urgent order may mark the currently selected eligible material again.
For owned cargo, automatic ground-marker removal is not completion or player cancellation. Failed
delivery and safe nearby dropping preserve the outstanding order and its exact quantity. The
owned loose-stack marker denotes remaining urgent units rather than every merged unit. Cancelling
that loose stack clears its remaining urgent quantity; held Gear cancellation can select a smaller
quantity. These are owned order semantics, not a promise of compatibility with source designations.

#### Scenario: Urgent trip with an unmarked neighbor
- **WHEN** two urgent items and an unmarked item are close together and an urgent batch starts
- **THEN** only eligible urgent items enter that trip and all load limits still apply.

#### Scenario: Urgent material remains in inventory after interruption
- **WHEN** an urgent trip is interrupted after pickup but before delivery
- **THEN** remaining cargo still requires delivery and is not considered complete because it left the ground.

#### Scenario: Owned urgent portions share a loose stack
- **WHEN** the player cancels two of five held urgent units through owned Gear controls and the five later share one loose stack
- **THEN** owned S008 shows three urgent units, and either an owned batch or its ordinary urgent fallback takes at most those three
- **AND** the two cancelled units remain available for ordinary work without regaining urgency through merging or reload.

#### Scenario: Ground marker cleanup after owned pickup
- **WHEN** an owned batch collects an urgent item, its ground marker is removed and the player then drafts the hauler
- **THEN** the held quantity still shows an outstanding urgent delivery in Gear
- **AND** neither pickup nor automatic designation cleanup completes or cancels that delivery.

#### Scenario: Owned placement fails after marker cleanup
- **WHEN** an owned urgent marker is cleared before a cargo placement that subsequently fails
- **THEN** the undelivered quantity retains its urgent order
- **AND** if it is safely dropped nearby, its owned outstanding urgency is visible without duplicating the material or the order.

#### Scenario: Explicit cancellation of held or loose urgent material
- **WHEN** the player cancels a held quantity's urgent order in Gear or uses the owned cancel control on loose urgent material
- **THEN** the selected material loses that urgent intent and it is not restored by later cleanup, dropping or reloading
- **AND** the physical items remain, any held cargo can still unload ordinarily, and an uncancelled remainder keeps its own order.

#### Scenario: Save after the owned ground marker disappears
- **WHEN** a save is reloaded after ground-marker cleanup for undelivered owned cargo
- **THEN** surviving held quantities retain their urgent status and cancellation control
- **AND** delivery resumes only through current safe native work opportunities.

#### Scenario: Cancelled and urgent units share a loose stack
- **WHEN** two cancelled units and three still-urgent units are safely dropped or merged into one five-unit loose stack
- **THEN** the owned urgent marker represents only the three outstanding units and the current urgent quantity is visible when inspecting the stack
- **AND** an urgent batch or supported ordinary urgent fallback takes at most those three, leaving the two cancelled units for ordinary work
- **AND** if quantity-safe urgent pickup is unavailable, it defers rather than making all five urgent; owned cancellation on the loose stack clears the remaining three.

### Requirement: Ordinary single-load fallback cannot extend a loaded batch
The mod SHALL finish or safely unload existing owned cargo before starting ordinary single-load fallback for an item that cannot participate in batching.
This applies to an oversized individual item, excluded corpse, specialized hopper/container route
or unsupported destination. The fallback retains native single-load limits and all applicable owned
storage-refill rules. It does not globally prohibit vanilla heavy-item hauling, but it cannot carry
an oversized remainder on top of a filled owned hauling inventory. If safe unloading is unavailable,
the second load is deferred with an explanation.

#### Scenario: Heavy corpse outside the inventory allowance
- **WHEN** a corpse cannot fit the inventory mass allowance but native ordinary hauling permits it
- **THEN** the pawn first unloads owned hauling cargo and can then use the normal single-load job
- **AND** no new inventory allowance is granted for the corpse.

#### Scenario: Partial stack fills the batch
- **WHEN** only part of a loose stack fits into a batch
- **THEN** the remainder stays available for later hauling
- **AND** the pawn does not add that remainder in its hands beyond the combined batch limit.

### Requirement: Hauling preferences use common global and save controls
The mod SHALL expose corpse batching initially off, capable-animal batching initially on,
capable-mech batching initially on, drop hauled cargo when drafted initially off, and minimum free
starting mass capacity initially 20%, using the common global-default and per-save-override controls.
The minimum is a whole percentage from 0 through 100. It compares actual free combined mass space
before a new batch, including gear, personal inventory and hands; equality qualifies. At 0%, the
worker still needs positive capacity and room for an actual unit. At 100%, its occupied mass must
be zero. The threshold is permission to start, not a requirement to leave that fraction empty after
collection. Corpse/race preference changes apply to the next pickup decision: disallowed new pickups
stop, while existing cargo can still unload. A changed starting-space threshold applies to the next
new batch, not retrospectively to a batch already collecting. No setting increases the hard limits.
The draft-drop preference affects future transitions into drafted state; turning it on while a
pawn is already drafted does not retrospectively discard cargo. Its exact safe-drop behavior is
specified in cargo care. Retaining cargo by default deliberately differs from Continued's default
auto-drop while preserving an explicit opt-in for that source behavior.

#### Scenario: Threshold boundaries
- **WHEN** a pawn has exactly 20% free mass capacity under the default threshold
- **THEN** it can start a fitting batch; with less free capacity it first unloads owned cargo or uses a safe ordinary route.

#### Scenario: Inherit and override
- **WHEN** a new save has no hauling preference overrides
- **THEN** it uses current global defaults; checking Use global preserves a dormant override, and unchecking restores it or seeds the first override from the current effective value
- **AND** changing one save's value never changes the global default or another save.

#### Scenario: Main-menu controls
- **WHEN** settings are opened without a loaded save
- **THEN** Global default remains editable while Use global and Current save controls and their mutation actions are disabled.

#### Scenario: Disable animals or corpses while cargo exists
- **WHEN** the applicable preference is turned off during collection
- **THEN** no further now-disallowed pickups occur and the existing cargo remains eligible for safe unloading.

### Requirement: Search and hauling reflect the same current items
The mod SHALL keep item-search results and hauling candidates consistent as items move, split, merge, enter inventory, are delivered or disappear, while deciding each pawn's actual eligibility at use time.
A shared item fact does not grant access or a reservation. A held item cannot remain a loose target
on its former map. If the current candidate view is unavailable, use a proven current native query
or stop affected collection; stale results cannot authorize a pickup. This is a shared-data and
freshness contract, not a measured performance claim.

#### Scenario: Another pawn removes a search result
- **WHEN** search showed a loose item but another pawn picks it up before the current pawn reaches it
- **THEN** the current pawn does not pick it up again or retain a false loose-item candidate.

#### Scenario: Save and map switch
- **WHEN** cargo changes maps or a save is reloaded
- **THEN** search and new hauling decisions use the current physical locations and quantities
- **AND** neither acts on a former map's stale reservation or route.

### Requirement: Compatibility failure preserves ordinary work and physical cargo
Unsupported batch-hauling integration SHALL stop only the affected new collection, explain which behavior is unavailable and leave supported ordinary work and recoverable cargo intact.
An active original Pick Up And Haul, Continued, Reworked or Armful provider SHALL trigger a bounded
startup warning and the ordinary per-feature settings disable path for overlapping owned collection,
with the same effective off behavior as a user disabling it. This is best effort, not a cooperative
compatibility guarantee or control over foreign hauling. No original API/binary/save checks,
consumer-parity work or facade is required; no mod is removed or impersonated. Compatible unrelated
patches still use ordinary semantic admission.
An external opportunity provider that cannot respect the combined allowance and current owned
cargo likewise withholds only the affected batching route with an explanation; no private source
interface is emulated merely to activate its patches. Existing owned cargo uses a separately safe recovery path before ordinary hauling
can add another load. A custom capacity or storage system is never assumed unlimited because it
cannot be read. Startup logs identify each skipped enabled integration and its reason/fallback;
compatible unrelated modifications alone do not disable the feature.

#### Scenario: Custom inventory capacity cannot be read
- **WHEN** an active custom inventory system cannot provide a validated capacity limit
- **THEN** affected inventory batching stays unavailable with an explanation, rather than using a guessed allowance
- **AND** supported ordinary hauling remains available after safe cargo recovery.

#### Scenario: Original provider active
- **WHEN** startup detects an active original hauling provider
- **THEN** it warns and requests ordinary settings disablement of the owned overlap, best effort
- **AND** owned cargo follows the normal feature-off recovery contract without a compatibility guarantee about the original.

#### Scenario: Uncoordinated external opportunity would add a hand load
- **WHEN** an external opportunity provider cannot account for the owned cargo already filling a pawn's combined allowance
- **THEN** affected new owned batching remains unavailable until that interaction is supported, and existing cargo is safely recovered before another load can be added
- **AND** the unavailable integration is explained without giving the foreign provider a false legacy cargo view.
