# Visitor deliveries

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Source S027, Just Put It Over There. Specification only; no implementation or live acceptance.

## ADDED Requirements

### Requirement: Visitors offer delivery after actual item handover
The mod SHALL automatically attempt a storage delivery for actual item quantities received from an
on-map visiting caravan trade or visitor gift, including silver received as sale proceeds. The price, currency, ownership consequences,
gift chance/type/value, goodwill, ordinary messages and trade outcome SHALL remain unchanged.
Goods SHALL first be physically handed over through the ordinary trade/gift workflow and SHALL
reach storage only through a courier's actual movement, pickup and placement. A canceled/failed
trade or unsuccessful physical placement SHALL not create a delivery or replacement items.

Pawn purchases/sales, animals being traded, outgoing goods sold to visitors, orbital deliveries, world-map
transactions and unrelated mod-specific trading routes SHALL retain their ordinary behavior.
This feature SHALL not provide return-caravan unloading or general visitor hauling of colony stock.
It SHALL add no new bottom tab, gizmo, settings menu or general behavior-toggle button.

#### Scenario: Buy goods from a visiting caravan
- **WHEN** a completed on-map purchase physically hands over goods and a suitable courier and storage exist
- **THEN** the courier carries an eligible received load into storage without changing the paid price or creating more goods.

#### Scenario: Trade is canceled or handover fails
- **WHEN** the trade is canceled or no item was successfully placed by the handover
- **THEN** no courier assignment or synthetic receipt is created and ordinary trade/placement handling remains in control.

#### Scenario: Sell goods and receive silver
- **WHEN** a completed caravan trade sells colony goods and physically hands over silver to the colony, with or without purchased items
- **THEN** the received silver is eligible for the same finite delivery as other incoming goods in that trade event
- **AND** outgoing sold goods are not collected or returned, and no price or payment amount changes.

#### Scenario: Buy or sell a pawn
- **WHEN** a trade transfers an animal or humanlike pawn, or the colony sells an item to visitors
- **THEN** ordinary trade consequences apply and that transferred pawn/item is not treated as an incoming courtesy-delivery load.

### Requirement: Couriers belong to the relevant party and retain their ordinary duties
A courier SHALL be a currently available nonplayer, non-animal member of the corresponding visiting
trade party or actual gift-giver set, on the same map, capable of manipulating and carrying that load.
The main trader SHALL never be assigned, including when the purchased goods came from that trader's
own inventory. Purchased pawns, prisoners, unrelated visitors and colonists SHALL not be recruited
as couriers. Animals SHALL not be assigned or deliberately led after a courier into storage.

Sleeping, downed, mentally unavailable, fighting, fleeing, rescuing, tending, already hauling or
already assigned delivery pawns SHALL not have those duties interrupted for this courtesy. A merely
idle/ordinary visiting pawn can perform it without gaining general colony work permissions. Candidate
couriers SHALL be considered nearest first to the handed-over goods, using current item and storage
reachability, reservations and permissions. An unsuitable nearest candidate SHALL not prevent a
farther suitable party member from being considered. No suitable courier means ordinary ground fallback.

The assigned courier SHALL be allowed to handle its exact received quantity for this delivery even
when the trade party is otherwise prevented from taking back sold goods. This limited permission
SHALL not make that item available for visitor consumption or resale, release old stock in a merged
stack, or grant other visitors access. Player-forbidden items, restricted routes/destinations and
other provider access rules SHALL still prevent delivery; the courtesy permission ends with its assignment.

#### Scenario: Goods came from the main trader
- **WHEN** the purchase was carried in the main trader's own inventory
- **THEN** another eligible party member can deliver it, while the main trader and its following animals remain under their ordinary duties.

#### Scenario: Only a pack animal and trader remain
- **WHEN** no suitable non-trader party member is available
- **THEN** received items stay physically on the ground rather than recruiting the animal or a colonist.

#### Scenario: Nearest visitor cannot reach
- **WHEN** the closest otherwise eligible courier cannot reach the goods or usable storage but another can
- **THEN** the suitable farther courier can be selected without granting either pawn new access rights.

#### Scenario: Existing urgent work
- **WHEN** a possible giver is sleeping, rescuing someone or already hauling another load
- **THEN** the courtesy neither replaces that work nor appends delivery behind it as an indefinite obligation.

#### Scenario: Sold goods remain protected from visitor use
- **WHEN** received goods are protected against the visiting party taking them back and an eligible courier is assigned
- **THEN** that courier can pick up only its assigned received quantity for storage, while other visitor use and unrelated stock remain protected
- **AND** marking the item forbidden or restricting its destination still prevents the courtesy pickup or placement.

### Requirement: Courtesy effort is finite and uses ordinary carrying limits
Each handover event SHALL assign at most one ordinary single carrying load to each available
courier. One completed trade's received item batch is one event; one gift batch is one event.
An existing courier assignment SHALL not be replaced by a later item in that event or by a second
trade. Quantities SHALL fit the courier's current ordinary carrying allowance and the destination's
current acceptance. Existing carried items and personal inventory SHALL be retained; this feature
SHALL not provide an extra inventory allowance or enable inventory batch hauling for visitors.

Unassigned/excess quantities SHALL remain available on the ground. Completion or failure SHALL not
start another courtesy trip for the same event, collect unrelated nearby items, or build an unbounded
queue. A later distinct handover can use the visitor again once available. Delivery SHALL not wait
indefinitely for a reservation, missing path or changing destination to recover.

#### Scenario: More goods than available couriers
- **WHEN** a gift batch contains more separate loads than eligible visitors can carry in one trip each
- **THEN** each assigned visitor gets at most one load and the remaining goods stay on the ground for ordinary colony hauling.

#### Scenario: Partial stack fits
- **WHEN** only part of a received stack fits the courier or storage allowance
- **THEN** only that part is collected and the physical remainder stays available without increasing carrying capacity.

#### Scenario: Another trade while delivery is active
- **WHEN** the player completes another purchase before the first courier finishes
- **THEN** the first delivery remains intact and only other currently available couriers can take new loads.

### Requirement: Storage respects physical acceptance and the intended delivery kind
Delivery SHALL use reachable enabled player storage with a strictly better storage priority than
the goods' current valid storage, following ordinary storage selection. Stockpiles, shelves and
supported containers SHALL keep their actual filters, quality/condition limits, faction/access rules,
reservations, capacity and provider restrictions. The feature SHALL not promise the globally shortest
route or move an already adequately stored receipt sideways between equal-priority stores.

Linked storage SHALL share its configured policy without duplicating physical room or bypassing a
particular member's restrictions. Planned incoming quantities from couriers and colony haulers SHALL
compete for the same real space. Eligibility and space SHALL be checked again before pickup and
placement. A full, forbidden, destroyed, inaccessible or no-longer-accepting destination SHALL not
receive forced goods. Unsupported storage SHALL use a proven ordinary route or ground fallback.

Trade/gift receipts are deliveries, not stock-level-triggered automatic refilling: the separately
specified per-item group X/Y refill thresholds SHALL not prevent them or cap them at Y. The
storage-wide per-stack hauling ceiling SHALL still constrain the hauled portion, alongside each
item's real stack maximum. Neither control SHALL delete or resize existing stacks. Ground handover
itself is not a hauling action and SHALL not be changed to enforce these limits.

#### Scenario: Higher-priority compatible shelf
- **WHEN** a reachable higher-priority shelf accepts the receipt and has space while another rejects its type
- **THEN** the courier uses valid storage and does not exploit the rejecting shelf's linked-group membership.

#### Scenario: Two deliveries share the last space
- **WHEN** a courier and a colony hauler both consider the final available space
- **THEN** their combined allocated pickup does not exceed that space, and a later capacity loss produces safe fallback.

#### Scenario: Group refill target is already reached
- **WHEN** a purchase can fit physically but would take the group's total above its automatic-refill target Y
- **THEN** Y alone does not stop the courtesy delivery
- **AND** the per-stack hauling ceiling and actual item stack maximum still constrain the delivered amount.

#### Scenario: Goods landed in equal-priority valid storage
- **WHEN** handover already placed the goods in valid storage and no strictly better destination is usable
- **THEN** no courtesy job moves them merely to another equal-priority location.

### Requirement: Received quantities remain distinct from unrelated stock
The feature SHALL deliver only quantities actually received by the event, preserving item identity,
quality, condition and other consequential attributes. A receipt split across physical stacks SHALL
not become extra goods or extra delivery entitlement. A receipt merged with preexisting stock SHALL
not make the old stock part of the courtesy. Repeated notifications, reload, partial pickup and partial
placement SHALL not replay payment, gifting, handover or completed delivery quantities.

Items already moved, consumed or legitimately taken by another actor SHALL not be recreated or chased
into unrelated holders. Unsold trader stock, personal meals/equipment and old colony goods SHALL
remain untouched. Normal future events can affect these physical items; delivery tracking is not
immunity from fire, deterioration, ordinary use or destruction.

For an ordinary removal from identical mixed units that does not identify a portion, the mod SHALL
reduce pending receipt quantities first, oldest handover event first, preserving as much preexisting
stock as physically remains. Explicitly attributed pickup/removal SHALL affect only its identified
portion. This is the proposed accounting rule, shared with the cargo-first principle used for owned
hauling; no event can claim a removed quantity again or replace it with another event's units.

#### Scenario: Purchase merges with an older stack
- **WHEN** twenty purchased units merge with thirty compatible colony-owned units during handover
- **THEN** courtesy allocation is at most twenty units, subject to actual remaining quantity and capacity
- **AND** it does not acquire permission to haul the whole fifty-unit stack as the receipt.

#### Scenario: Another hauler takes part first
- **WHEN** twenty received units share a stack with thirty old units and another actor removes ten without identifying a portion
- **THEN** the remaining forty units contain only ten eligible receipt units and thirty old units
- **AND** the courier does not recreate the removed ten or take old units to make up the difference.

#### Scenario: Multiple receipts share a stack
- **WHEN** thirty old units share a stack with an older twenty-unit receipt and a newer fifteen-unit receipt, and an ordinary unassigned removal takes twenty-five units
- **THEN** the older receipt has no pending quantity, the newer receipt has ten, and the thirty old units remain outside courtesy allocation.

#### Scenario: Save after partial placement
- **WHEN** a partially delivered load is saved and reloaded
- **THEN** only its actual remaining physical quantity can continue, with no repeated trade effect or duplicate gift message.

### Requirement: Routine gifts can precede departure while urgent exits take precedence
Incoming trade receipts from a caravan already traveling to leave the map SHALL receive ordinary ground handover without a courtesy assignment.
This includes purchased goods and sale-proceeds silver.
A visiting courier who begins leaving, loses its party,
becomes hostile or receives an urgent survival/combat/rescue duty SHALL stop courtesy work and keep
the colony's received goods physically recoverable on the map before departure.

An actual routine farewell gift SHALL be allowed one bounded courtesy delivery by eligible givers
before they continue ordinary departure. This includes native visitors and supported Hospitality
guests whose gift is generated as their visit ends. It SHALL not restart or extend the visit into
another stay, change gift probability or prevent a guest from being sent away. Dismissal, hostile,
dangerous or emergency departure SHALL use ground fallback and take precedence over the courtesy.
After delivery, the visitor SHALL resume its ordinary party duty/gathering or departure; no new
allowed-area permission or permanent colony job is granted.

#### Scenario: Trade while the caravan is leaving
- **WHEN** a departing trade party completes an otherwise legal purchase or sell-only trade that hands over silver
- **THEN** its goods are handed over normally and no party member is sent back into the colony to store them.

#### Scenario: Hospitality farewell gift
- **WHEN** a supported Hospitality group gives an ordinary end-of-stay gift and suitable givers/storage exist
- **THEN** eligible givers can perform their finite delivery and then continue leaving
- **AND** the visit score, gift decision and ordinary departure notifications remain the provider's own.

#### Scenario: Raid or dangerous temperature during delivery
- **WHEN** the party must urgently fight or leave while a courier is carrying received goods
- **THEN** courtesy work yields to that duty and the load is safely left as physical colony-accessible goods instead of carried away as visitor property.

### Requirement: Interrupted delivery preserves actual cargo without retry loops
If pickup cannot occur, the received item SHALL remain at its actual location and any unused
delivery reservation SHALL be released. If delivery becomes impossible after pickup, the courier
SHALL place the actual remaining received load safely near its current location through ordinary
placement, without deleting it, returning it to trader inventory, marking it for personal use or
rerunning the trade. A partially accepted load SHALL leave only the remainder to recover. Existing
personal cargo SHALL not be dumped as if it were the receipt.

Downing/death SHALL leave carried goods through the ordinary physical drop/corpse workflow; the
feature SHALL not resurrect destroyed items. If immediate placement is impossible, preserve the
actual held remainder with a recoverable explanation, prevent its silent export/destruction and
avoid repeatedly issuing the same failing delivery. Save/load or temporary provider loss SHALL
preserve these obligations and exact physical quantities. Restoration SHALL reconcile actual
cargo before any continuation; it SHALL not restart the whole event or allocate additional loads.

#### Scenario: Shelf fills during the walk
- **WHEN** the destination can accept only part or none of a carried receipt by arrival
- **THEN** any accepted portion remains stored and the remainder is left safely near the courier
- **AND** no new courtesy trip is created to retry that event.

#### Scenario: Courier becomes downed
- **WHEN** a courier becomes downed or dies before delivery
- **THEN** the actual goods remain recoverable through ordinary physical handling and no new copy is generated from the pending receipt.

#### Scenario: No immediate drop location
- **WHEN** the courier cannot place its actual carried remainder immediately
- **THEN** the remainder remains physically held and visibly recoverable rather than deleted or silently taken off-map
- **AND** the feature does not loop on repeated failed assignments.

### Requirement: Hauling and optional providers use one admitted delivery owner
Owned Pick Up And Haul and Common Sense behavior SHALL not divert these visitor loads into personal
inventory, unrelated collection, cleaning or detours. Visitor courtesy SHALL not grant general
batch-hauling work to guests/nonplayer pawns. Ordinary colony haulers remain free to handle unassigned
ground goods using their own priorities, limits and current reservations. Storage and candidate facts
SHALL remain fresh across movement, splitting, merging, pickup, placement and map/save changes.

With the original Just Put It Over There provider active, the owned overlapping enhancement SHALL
be withheld with a bounded startup warning. Existing owned deliveries SHALL be safely retired or
recovered before new scheduling, without deleting cargo or handing ambiguous pending work to the
original mod. Unsupported required game/provider behavior SHALL disable only the affected delivery
path, warn at startup with its unavailable behavior and ground fallback, and preserve native trade
and gift workflows. Benign foreign patches SHALL not alone reject the feature. No package spoof,
automatic source-state import or safe-removal guarantee is included.

#### Scenario: Owned batch hauling is enabled
- **WHEN** a visitor is delivering a received load while the colony uses batch hauling and housekeeping
- **THEN** that visitor performs only its ordinary courtesy load and does not collect it into a separate hauling/personal inventory or delay it for unrelated side work.

#### Scenario: Original provider co-installed
- **WHEN** the original provider is active at startup
- **THEN** the log identifies the withheld owned delivery behavior and pending owned cargo is preserved through safe recovery rather than processed twice.

#### Scenario: Changed gift provider
- **WHEN** a provider's gift/departure behavior cannot support safe courtesy delivery
- **THEN** its affected delivery enhancement is withheld with a bounded warning while its ordinary gift handover remains available
- **AND** supported native trade delivery is not disabled merely because that optional gift integration is unavailable.
