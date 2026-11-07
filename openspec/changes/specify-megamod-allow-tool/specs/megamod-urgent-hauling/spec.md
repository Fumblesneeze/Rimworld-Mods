# Urgent hauling

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Specification draft; nothing implemented. Source: Allow Tool, S008 / P1-08.

## ADDED Requirements

### Requirement: Explicit urgent item orders
The mod SHALL let the player mark revealed haulable objects outside valid best storage for urgent
hauling from Orders or selected-object controls. Marking unforbids the item and supplies an ordinary
haul order when the item requires one. Existing urgent marks are not duplicated. Shift asks free
colonists to reconsider current work through ordinary work selection; it does not override drafting,
incapacity, reservations, access, carrying limits or noninterruptible jobs. The separate Haul+
column is initially visible, starts enabled for eligible workers and obeys their work priorities.

#### Scenario: Order an urgent chunk haul
- **WHEN** the player directly marks an eligible loose forbidden chunk for urgent hauling
- **THEN** it becomes allowed, carries one visible urgent mark and can be hauled by an eligible worker
- **AND** repeating the action does not duplicate the order

#### Scenario: Shift respects work eligibility
- **WHEN** urgent hauling is requested with Shift while an eligible worker is doing interruptible
  lower-priority work and another pawn is drafted or incapable of hauling
- **THEN** the eligible worker can switch through ordinary work selection
- **AND** the other pawn is not made to abandon drafting or perform impossible hauling

### Requirement: Work-column presentation preserves player priorities
The mod SHALL preserve each pawn's chosen Haul+ and Finish priorities through save/load and column
visibility changes. A newly introduced work category initially uses ordinary enabled priority 3 for
eligible workers. Hidden-column mode uses automatic priority 3 for that category without rewriting
the stored visible-column choices; showing it again restores those choices. Native inability to
perform work still applies. Hiding Haul+ does not mean disabling hauling or merging its priority
with ordinary Haul; the UI explains this distinction. Finish follows the same rule and is initially hidden.
With simple priorities, or equal manual priorities, Haul+ follows Bed rest and precedes Basic work;
Finish follows Basic work and precedes Wardening. Firefighting, urgent patient care and doctoring
therefore retain their earlier native positions. Explicit unequal manual priorities take precedence
over this category ordering.

#### Scenario: Urgent work wins an equal-priority tie
- **WHEN** a pawn can perform urgent hauling, basic work and ordinary hauling at equal manual priorities
- **THEN** urgent hauling is considered before basic work and ordinary hauling
- **AND** equally prioritized bed rest remains ahead of it, while equally prioritized finishing
  follows basic work and precedes wardening

#### Scenario: Preserve a deliberately disabled worker
- **WHEN** the player disables Haul+ for a pawn, hides and later shows the column, or reloads the save
- **THEN** that pawn's visible-column choice remains disabled
- **AND** hidden-column automatic behavior is clearly indicated while hidden, rather than silently
  replacing the stored choice on map load

### Requirement: Urgency lasts until successful delivery or cancellation
The mod SHALL keep urgent orders until the ordered material reaches valid best storage, is consumed
or destroyed, or the player cancels the order. Temporary reservation, lack of a destination, pickup,
interruption or a failed placement does not count as delivery. After a partial haul, the remainder
still needing delivery remains urgent; delivered material does not create endless repeat hauling.
If ordered material is merged into a loose stack, only its outstanding ordered quantity stays urgent;
ordinary or cancelled units do not become urgent. Inspection shows the outstanding quantity.
Both ordinary urgent hauling and S021 batching take at most that quantity, leaving other units for
ordinary work. If a mixed stack cannot be collected with that distinction, defer its urgent pickup
with an explanation. Held urgent quantities can be cancelled in Gear without dropping the items;
loose-stack cancellation clears that stack's outstanding urgent quantity. Cancellation persists
through split, merge and reload; an explicit new urgent order may select eligible material again.
The feature does not create, duplicate or discard items to complete an order.

#### Scenario: Destination fills during travel
- **WHEN** another pawn fills the reserved destination before an urgent load is placed
- **THEN** the hauler can seek another valid destination through ordinary hauling
- **AND** failure or interruption does not silently remove urgency from material still needing delivery

#### Scenario: Partial and merged stacks
- **WHEN** only part of an urgent stack fits in a load or destination and the remainder merges with
  another loose stack
- **THEN** only the remaining ordered quantity retains urgency, delivered and previously ordinary
  material remain nonurgent, and total item quantity is unchanged by the order bookkeeping

#### Scenario: Owned urgency does not expand across a mixed stack
- **WHEN** three urgent units merge with two ordinary or explicitly cancelled units
- **THEN** inspection shows three urgent units out of five, and either ordinary urgent hauling or S021 batching takes at most three
- **AND** the other two do not regain urgency through pickup, dropping, merging or reload without a new explicit order

#### Scenario: Save and map lifecycle
- **WHEN** the game is saved and reloaded with outstanding urgent orders, then the player changes maps
- **THEN** each order remains attached to its surviving item on its own map
- **AND** despawned, destroyed or transferred objects are not used as stale haul targets on the old map

### Requirement: Shared hauling behavior respects actual capacity
The mod SHALL use the same urgent item orders with its own future inventory-hauling feature, without
requiring a second urgency action. An urgent inventory trip collects only eligible urgent items;
all carried and inventory loads count against actual supported carrying capacity. Candidates and
reported quantities agree with the shared item search as items move, split, merge or disappear,
while each job still checks that pawn's current access, reservation and storage eligibility.
Until owned inventory hauling exists, ordinary capacity-limited native hauling supplies the behavior.
Unverified external inventory-hauling integrations are not advertised as supported.

#### Scenario: Urgent multi-item trip
- **WHEN** a capable pawn starts an urgent trip with two urgent items and a nearby unmarked item
- **THEN** only eligible urgent items are added to that trip and its actual load stays within capacity
- **AND** failure of an optional inventory integration leaves ordinary urgent hauling available

#### Scenario: Item changes after search
- **WHEN** search displayed an urgent stack but another pawn reserves or removes it before job assignment
- **THEN** the new hauler does not act on the stale decision or duplicate the item

### Requirement: Explain missing urgent storage
The mod SHALL initially show a high-priority alert when the current map has unreserved urgent items
without a suitable better storage destination. The alert lists up to five affected item names with
an indication of additional items and permits locating affected targets. Reserved items are excluded
to avoid reporting an in-progress haul as blocked. Adding storage, changing its filters or priority,
canceling an order, removing an item or switching maps refreshes the result within two real-time
seconds while the map UI is active, including while paused. This is a destination alert, not a
claim that any particular pawn can reach or haul the item. The two-second bound is a proposed
acceptance target, not a measured upstream guarantee.

#### Scenario: Storage becomes available while paused
- **WHEN** the alert lists an unreserved urgent meal and the player enables meals in suitable storage
- **THEN** that meal disappears from the alert within two real-time seconds without unpausing
- **AND** switching maps never displays the old map's targets as targets on the new map

#### Scenario: Reserved load is not reported as abandoned
- **WHEN** a pawn reserves an urgent item for delivery
- **THEN** it is excluded from blocked-storage feedback within the same freshness bound
- **AND** it can reappear if the reservation ends and no destination exists
