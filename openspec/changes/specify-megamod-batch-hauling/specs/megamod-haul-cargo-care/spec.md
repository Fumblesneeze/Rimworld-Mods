# Hauling cargo and personal supplies

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). **1.7 beta**.
Source S021: Pick Up and Haul - Continued with Reworked/Armful comparison and the current While
You're Up hauling intersection reconciled into the selected composition. Specification only;
nothing implemented or accepted in game. The user's required visible hauled stacks are tracked under
S126 and compose directly with this physical-cargo contract; detailed visual scenarios are drafted
in [that source's change](../../../specify-megamod-visible-haul-cargo/specs/megamod-visible-haul-cargo/spec.md).
Exact quantity tracking, explicit keep/release actions and safe recovery are selected corrections
and composition choices, not claims of identical behavior or complete source binary parity.

## ADDED Requirements

### Requirement: Track only cargo that was actually taken
The mod SHALL distinguish the actual quantities held for delivery from personal or otherwise unowned inventory, preserving item identity, material, quality, condition and total quantity through pickup and transfer.
Only successfully accepted units become cargo. Partial or failed pickup leaves the rest at its
valid physical location. Splitting or merging never converts all items of the same type into cargo.
Removal/consumption adjusts the actual affected quantities; it cannot create phantom cargo or
cause a different personal stack to be unloaded as a replacement. When an ordinary removal from an
identical mixed stack does not name a portion, reduce its cargo quantity first and preserve as many
kept units as physically remain. Explicit keep/release/drop choices affect their selected portion.
This is a proposed attribution rule. If mixed-stack attribution is
uncertain, preserve personal units and expose the unresolved cargo instead of guessing by type.

#### Scenario: Personal medicine and hauled medicine
- **WHEN** a pawn keeps two medicine units and collects five matching units for delivery
- **THEN** unloading delivers only the five cargo units and retains the two kept units, even if they share a physical stack.

#### Scenario: Partial inventory acceptance
- **WHEN** only three of five requested units actually enter inventory
- **THEN** only three are marked as cargo and the other two remain physical and available at their source.

#### Scenario: One quality disappears
- **WHEN** a hauled shirt is consumed, destroyed or removed while another shirt of different quality remains personal inventory
- **THEN** the personal shirt is not reclassified as cargo merely because it has the same item type.

#### Scenario: Ordinary consumption from a mixed stack
- **WHEN** a stack contains two kept and five hauled identical units and ordinary use removes three without naming a portion
- **THEN** the four surviving units contain two kept and two hauled units
- **AND** no removed unit is recreated by unloading.

### Requirement: Unloading physically delivers current cargo
The pawn SHALL unload recorded cargo through real movement and ordinary storage placement, rechecking each destination's acceptance, capacity and reservations and retaining undelivered quantities until placement succeeds.
The default handling pause is 3 game ticks per unloaded stack portion, followed by ordinary movement
and provider handling time; there is no hard-coded longer delay based only on a mod's display name.
Unloading splits to the actual hand, stack and destination limits. Cargo is considered delivered only
when physically accepted. Another destination can be used when the first becomes invalid.
Delivery groups currently valid portions by physical storage destination: one shelf, stockpile
zone or container, rather than every linked member sharing its settings. The next reachable
destination is chosen by the nearest valid delivery cell's straight-line distance from the pawn,
with stable ties. Deliver the valid committed portions for that destination before walking to the
next destination; movement between its cells and separate physical placements remain necessary.
Grouping never overrides per-item filters, capacity, urgency or an originating opportunity's
travel limits. Changed storage can require another visit; there is no promise of a globally
shortest route or a single drop operation.

#### Scenario: Destination accepts only part of a stack
- **WHEN** five cargo units remain but the selected destination can accept two
- **THEN** two are physically delivered and the other three retain their delivery status until accepted elsewhere or safely recovered.

#### Scenario: Transfer or placement fails
- **WHEN** a transfer into hands or storage rejects the item
- **THEN** the rejected units remain at their actual location with correct cargo status
- **AND** no success is reported and no substitute personal item is selected.

#### Scenario: Hands hold a pawn
- **WHEN** a hauler's hands are occupied by a baby, rescued pawn or another incompatible carried entity
- **THEN** unloading does not evict, overwrite or drop that entity to make room
- **AND** cargo waits for a safe ordinary opportunity or supported recovery.

#### Scenario: Several cargo types share a storage destination
- **WHEN** a pawn holds steel, wood and clothing with valid committed spaces in the same stockpile and other cargo for a different shelf
- **THEN** it completes the currently valid stockpile deliveries during that visit before traveling to the shelf, or completes the nearer shelf first if that destination is selected first
- **AND** each material physically enters its permitted space in bounded stack portions.

#### Scenario: Linked storage members are separate visits
- **WHEN** two distant shelves share linked storage settings
- **THEN** their physical destinations remain separate for travel and capacity, and grouping does not teleport cargo between them.

#### Scenario: One portion becomes invalid during a grouped visit
- **WHEN** a destination stops accepting one cargo type while accepting others
- **THEN** valid portions can still be delivered there and the rejected portion retains its identity and quantity for another permitted destination or recovery.

### Requirement: Failed storage has bounded recovery without dumping personal inventory
When no valid delivery is available, the mod SHALL attempt safe nearby placement of the affected cargo only, retain it if placement fails, and end the failed attempt without a repeating immediate pickup/drop loop.
It reports the remaining problem through the pawn's contextual status. A nearby drop is not a
successful storage delivery; any outstanding urgent order remains on the undelivered material.
The feature never sets a dump-everything action to repair its own bookkeeping. New collection waits
until the stranded cargo has a safe delivery/recovery path. Unrelated personal supplies stay held.
After an unsuccessful automatic recovery attempt, repeated automatic attempts for the same
unchanged problem wait at least **600 simulation ticks**. A relevant destination, access or held-load
change can permit an earlier new attempt; repeated notifications without a changed condition cannot.
An available explicit retry may try immediately. This delay never postpones emergency work,
physical item-loss prevention or the player's valid order. Loading a save preserves the remaining
delay; rendered frame rate does not shorten it. The 600-tick default is a proposed adoption of
Reworked's recovery interval, applied consistently to failed owned automatic recovery.

#### Scenario: Storage removed during a trip
- **WHEN** all acceptable destinations disappear after collection
- **THEN** only the affected cargo is safely placed nearby if possible, otherwise retained with an explanation
- **AND** the pawn does not repeatedly recollect it without a valid destination.

#### Scenario: No safe drop cell
- **WHEN** neither storage nor nearby placement can accept the cargo
- **THEN** its physical quantity and cargo status are preserved, the attempt stops and the pawn shows the unresolved delivery
- **AND** no personal items are discarded as a recovery shortcut.

#### Scenario: Idle checks repeat after failed placement
- **WHEN** safe placement fails and unchanged idle or inventory checks recur during the next 600 simulation ticks
- **THEN** they do not repeatedly restart the same recovery attempt, the pawn's cargo remains inspectable, and urgent needs remain eligible.

#### Scenario: Fix storage during the recovery delay
- **WHEN** the player creates usable destination space or explicitly requests an available retry before that delay ends
- **THEN** a new current recovery attempt can proceed without duplicating a pending attempt or discarding kept supplies.

### Requirement: Interruptions preserve cargo without overriding urgent needs
Drafting, player orders, emergencies and ordinary work interruption SHALL stop further collection through native job rules without deleting, duplicating or silently abandoning held cargo.
When the pawn can safely resume ordinary work, existing cargo is unloaded before starting a new
batch. Idle and ordinary hauling opportunities can schedule that recovery, but cannot override a
drafted state, medical emergency or a valid explicit order. Recovery requests are not duplicated
each time an idle or inventory check occurs. A batch does not repeatedly add new collection work
ahead of unloading.
By default, drafting retains cargo. If **Drop hauled cargo when drafted** is enabled, the transition
into drafting attempts safe nearby placement of only the hauled quantities, preserving their actual
identity and any outstanding urgent order. Failed placement leaves those quantities held and
identified; personal/kept supplies and a carried pawn are never selected for this automatic drop.
The pawn still enters the requested drafted state without a repeating drop attempt. Dropping is
not storage delivery. Undrafting allows ordinary safe recovery under the same current policies.

#### Scenario: Draft during collection
- **WHEN** the player drafts a pawn after the first pickup with Drop hauled cargo when drafted off
- **THEN** the pawn stops adding cargo, obeys drafting and retains the collected items
- **AND** after undrafting, safe unloading can resume before another batch.

#### Scenario: Emergency interrupts a loaded hauler
- **WHEN** a higher-priority emergency interrupts the pawn
- **THEN** the emergency remains eligible and inventory recovery does not continually replace it
- **AND** the existing cargo is still identifiable afterward.

#### Scenario: Opt-in drafting drop with mixed medicine
- **WHEN** the preference is enabled and a pawn carrying two kept medicine units and five hauled units becomes drafted
- **THEN** only the five cargo units are considered for safe nearby placement, the two kept units remain, and any undelivered urgent quantities keep their order.

#### Scenario: Drafting drop fails
- **WHEN** the enabled automatic drop cannot safely place some cargo
- **THEN** the pawn still becomes drafted, failed quantities remain held and identifiable, and no immediate repeating drop or success report occurs.

#### Scenario: Enable the preference on an already drafted pawn
- **WHEN** the player enables Drop hauled cargo when drafted while a pawn is already drafted
- **THEN** its present cargo is not automatically dropped; the preference applies at a future transition into drafting.

### Requirement: Personal keep orders are explicit and capacity-limited
The mod SHALL let an ordinarily controllable eligible pawn take reachable items to keep through contextual item commands, showing the actual quantity that fits and a reason when none fits.
This can include ordinary items that native home-map take-group limits would otherwise hide, but
never bypasses physical access, reservations, supported inventory restrictions or the batch-hauling
combined mass/volume limits. Taking to keep does not mark units for storage delivery. The pawn's
Gear controls allow release of a chosen kept quantity for ordinary delivery, or ordinary immediate
drop. These are per-pawn gameplay choices, not global/save mod settings. Automatic meal carrying
belongs to S123 and must use the same protection/capacity accounting when implemented.
Supported personal loadout, sidearm and medicine policies protect their quantities through the
same actual ownership calculation. Hauled goods do not satisfy a personal loadout merely because
they are temporarily in inventory, and an ordinary unused-inventory cleanup does not remove them
before their owned delivery/recovery decision. If a required provider's protection cannot be read
or maintained safely, affected new collection remains unavailable instead of assuming nothing is
protected. Explicit player choices and ordinary consumption remain governed by their real provider
contracts; this feature does not blanket-lock every inventory item.

#### Scenario: Keep field medicine
- **WHEN** the player orders a capable pawn to take a fitting quantity of medicine to keep
- **THEN** the pawn physically collects it and ordinary cargo unloading leaves it available for intended use.

#### Scenario: Take request exceeds available space
- **WHEN** only two requested units fit the combined capacity limit
- **THEN** the command shows or clamps to two and takes no more, even if the inventory could physically hold a larger stack.

#### Scenario: Release a kept quantity
- **WHEN** the player releases one of two kept units for delivery
- **THEN** one becomes eligible for unloading and the other remains protected
- **AND** releasing it does not teleport either unit to storage.

#### Scenario: Temporary cargo and a personal loadout
- **WHEN** a supported loadout reserves two medicine units and the pawn additionally hauls five matching units
- **THEN** the temporary five do not replace the two required personal units, and delivery or ordinary inventory cleanup cannot discard the protected two as cargo.

#### Scenario: Provider protection becomes unavailable
- **WHEN** an active supported personal-inventory provider can no longer report its protected quantities safely
- **THEN** affected new pickup stops with a reason, known existing personal quantities remain protected, and held cargo retains a separately safe recovery path.

### Requirement: Cargo and kept state remain understandable across lifecycle changes
The pawn's existing Gear/inventory view SHALL distinguish hauled and kept quantities with readable text in addition to any color cue, and the saved state SHALL follow surviving physical items through save/load and supported transfers.
No new bottom tab is added. A contextual unload/retry action is available when the pawn can safely
perform it; unavailable actions show a reason. Changing maps or forming a caravan clears obsolete
routes/reservations but preserves actual held cargo and kept quantities; map deliveries do not run
while the pawn is off-map. Death, faction/control loss or native inventory drops leave actual items
under native ownership and remove obsolete hauling claims without spawning copies. An unresolved
saved reference is not replaced by any same-type item. No foreign source save is silently imported.

#### Scenario: Save with a mixed inventory
- **WHEN** a save containing hauled items and kept supplies is reloaded
- **THEN** the same surviving quantities retain their respective status and the next delivery uses current storage rather than an obsolete route.

#### Scenario: Caravan leaves and returns
- **WHEN** a loaded pawn leaves the map in a caravan and later returns
- **THEN** no off-map storage delivery or old-map reservation persists
- **AND** surviving cargo can be recovered against current storage without classifying all caravan goods as cargo.

#### Scenario: Explicit retry
- **WHEN** a player fixes storage and uses the pawn's available unload/retry control
- **THEN** the pawn attempts a real current delivery while preserving kept supplies
- **AND** success is visible as the correct physical items entering storage.

#### Scenario: Pawn dies or loses player control
- **WHEN** native gameplay drops or transfers a former hauler's possessions
- **THEN** no duplicate cargo is spawned, obsolete route claims are released and new nonplayer hauling is not assigned.
