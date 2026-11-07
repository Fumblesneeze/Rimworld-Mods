# On-site item destruction by melee

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Original feature S023 / P1-17. Specification only. Real melee and HP-using items are confirmed;
worker, loose-wrapper, quantity and stalled-work choices are proposed and retained under MD-Q1–4.

## ADDED Requirements

### Requirement: Designate only destroyable loose items that use HP
The player SHALL be able to mark visible, destroyable loose items that actually use hit points for on-site destruction through an Orders tool and selected-item controls.
This includes eligible apparel, weapons, resources, corpses and loose packed-object items; installed
buildings, plants and living pawns are excluded. A packed object's item wrapper must itself qualify;
the order is not permission to attack its installed form later. HP-less items remain ineligible,
regardless of their sale value, stack size or apparent fragility. Worn, equipped, personally kept
or otherwise held items cannot be newly marked through a ground-item tool. Repeating a designation
does not create another order. Drag and single-item selection apply the same eligibility and do not
reveal or affect fogged cells. Native nondestroyable restrictions are retained.

#### Scenario: Clothing beside HP-less currency
- **WHEN** the player drags Destroy by melee across an eligible coat, HP-less silver and an installed wall
- **THEN** only the coat receives the order and neither currency nor wall is deleted or damaged by the order.

#### Scenario: Packed item versus installed building
- **WHEN** a packed building is a loose HP-using destroyable item and the player selects it
- **THEN** its packed item can be marked, with normal contained-object destruction consequences
- **AND** installing it cancels this item order instead of authorizing attacks on the resulting building.

#### Scenario: Corpse versus living pawn
- **WHEN** a corpse with HP lies beside a living downed pawn
- **THEN** the corpse can be marked and the living pawn cannot become this order's attack target.

### Requirement: An eligible worker approaches the selected item without transporting it
The order SHALL be performed by an eligible player-controlled worker with ordinary Hauling assignment, access, manipulation and the ability to perform melee, using one worker per target at a time.
Normal Hauling work is considered before automatic disposal at the same work priority. Explicit
native work orders retain their normal precedence but do not bypass inability to perform violence
or melee. This feature grants no new animal training, race work type or mech ability. A colony mech
can participate only if its real work and melee capabilities permit it. The worker uses an ordinary
reachable safe adjacent work position; forbidden targets, restricted areas, unavailable reservations
and unreachable positions prevent work. It does not carry the target to a bench, create a disposal
bill, bash a door to reach it or pursue it into a dangerous combat route.

#### Scenario: On-site work
- **WHEN** an eligible worker is assigned a marked item in reachable open ground
- **THEN** the worker walks into melee reach and starts destroying it where it lies, without using a workbench.

#### Scenario: Blocking door or forbidden target
- **WHEN** an otherwise eligible item is inaccessible behind a blocking door or becomes forbidden
- **THEN** the job waits or ends with the reason and does not damage the door or override the prohibition.

#### Scenario: Worker cannot fight
- **WHEN** a hauling-capable worker cannot perform violence or has no usable melee attack
- **THEN** that worker cannot execute this destruction order and the item remains intact until a capable worker acts.

#### Scenario: Two workers select the same item
- **WHEN** two workers attempt the same marked target
- **THEN** only one owns the disposal job on that target and the other can take different work.

### Requirement: Each damaging action is a real ordinary melee attack
The worker SHALL repeatedly use its ordinary usable melee attacks against the authorized item, retaining real attack selection, wind-up, cooldown, damage variation and applicable weapon/body effects until the target is destroyed or work stops.
Actual melee equipment and body capabilities can affect the result. The feature does not fire a
ranged weapon, silently swap equipment, add a general labor speed damage multiplier, invent a fixed
disposal duration or reduce HP merely because time passed. Existing combat recovery time remains
after each attack and cannot be skipped by cancelling/restarting the order. The feature grants no
extra XP; ordinary immobile item attacks do not become Melee training. It never falls back to instant
deletion when attacks are unavailable. A progress display represents current remaining HP/marked
quantity, not a promise that a countdown guarantees destruction.

#### Scenario: Several blows are required
- **WHEN** the marked item's HP exceeds the damage of the first ordinary melee strike
- **THEN** the player can observe the strike and reduced HP, followed by the normal recovery before another attack
- **AND** the item is destroyed only when actual damage produces its normal destruction outcome.

#### Scenario: Equipment changes while work is interrupted
- **WHEN** the worker changes its melee-capable equipment before resuming
- **THEN** subsequent attacks use the current legitimate melee capabilities and the item's surviving HP
- **AND** no saved DPS estimate continues damaging it independently.

#### Scenario: Restart during cooldown
- **WHEN** the order is cancelled and issued again during an ordinary attack recovery period
- **THEN** the worker must still finish the existing recovery before another attack can start.

### Requirement: Normal damage and destruction consequences remain
The order SHALL preserve the selected attacks' normal damage handling and the item's normal damage/destruction effects, without adding recycling refunds or suppressing hazards, filth, damage records, quest effects or applicable native social consequences.
There is no guarantee of clean or harmless disposal. Waste, explosives, corpses and objects with
contents keep their normal consequences, including possible loss of contained equipment. The
contextual description identifies known hazardous or contained-item consequences where supported;
it does not promise safety when a custom effect is unknown. No copied Destroy Item-specific thoughts
or custom destruction record are added merely because work begins. Ordinary native drops or side
effects remain possible; they are not a second recycling payout. Already released effects can
continue after cancellation or target destruction.

#### Scenario: Hazardous waste
- **WHEN** actual melee damage to an eligible wastepack causes its ordinary gas or destruction pollution
- **THEN** those effects occur through the normal damage path, without being suppressed or triggered a second time by the disposal order.

#### Scenario: Corpse with possessions
- **WHEN** the player destroys a qualifying corpse whose native destruction removes contained possessions
- **THEN** those possessions are subject to that normal outcome and are not automatically stripped or refunded by this feature.

#### Scenario: Normal destructive side effect
- **WHEN** a selected item's native damage response starts a fire or explosion
- **THEN** its ordinary consequences remain, including possible collateral effects
- **AND** the worker does not intentionally select nearby unmarked objects as follow-up attack targets.

### Requirement: Damage authorization follows only marked quantities
The mod SHALL retain surviving authorized quantities through supported splitting, merging and ordinary hauling, without treating later unmarked arrivals or suspended portions as authorized targets.
Each portion retains its processing choice and active/suspended state; merging, splitting or
resuming another portion cannot grant it permission to be destroyed. Ordinary consumption/removal
retires the removed portion's order when that portion is known. If a removal identifies only a count
from mixed marked/unmarked material, reduce the outstanding destruction quantity first, down to
zero, rather than transfer permission to the survivors. If competing processing choices or suspension
states prevent reliable attribution, pause the affected surviving orders with an explanation until
the player explicitly resolves their quantities; uncertainty never activates additional units.
An ordinary homogeneous native stack keeps its normal shared HP and whole-stack damage behavior;
the feature does not multiply its required damage by unit count. If a physical stack mixes currently
attack-authorized units with unmarked, suspended or differently ordered units, the worker must first
separate the currently authorized portion safely in the same cell,
preserving current HP and item identity traits, before attacking it. If safe same-cell separation
cannot preserve the unmarked portion, attacks wait with an explanation; there is no attack-then-
restore shortcut. Recheck the actual target before each attack. Native released area effects are
distinct from intentional target authorization. Other items are not substituted by name or type.

#### Scenario: Marked and unmarked units merge
- **WHEN** two marked units merge with three unmarked matching units before the worker attacks
- **THEN** only the marked two can become the direct attack target after safe same-cell separation
- **AND** the three unmarked units retain their HP and cannot be deleted to satisfy the order.

#### Scenario: No safe separation space
- **WHEN** the same mixed stack cannot be separated safely in its current cell
- **THEN** the worker does not attack it, explains the blocked separation and leaves all units physical.

#### Scenario: Active and suspended quantities merge
- **WHEN** three active destruction units merge with two units still requiring Resume or Retry
- **THEN** only the active three can be separated for direct attacks, or work waits if safe separation is unavailable
- **AND** the other two keep their own suspended state through separation and reload; resuming another portion does not resume them.

#### Scenario: Ordinary consumption removes an unspecified portion
- **WHEN** a native recipe consumes three units from a stack containing two marked and three unmarked units without identifying the removed portion
- **THEN** the destruction quantity falls to zero and the two physical survivors are not automatically destroyed
- **AND** the player can explicitly mark survivors again if desired.

#### Scenario: Hauling moves the target
- **WHEN** ordinary hauling picks up marked material before the destruction worker reserves it
- **THEN** attacks stop while it is held and can target only the surviving marked material after it is loose again
- **AND** the worker does not attack its former cell or the carrying pawn.

### Requirement: Cancel and interruptions stop further attacks without repairing damage
Cancellation, drafting, a superseding player order or an ordinary higher-priority interruption SHALL stop new disposal attacks and cancel any pending wind-up that has not released its effect, while preserving existing damage and already released effects.
An interrupted but uncancelled order remains pending at the actual item's current HP. Cancelling
removes its authorization and does not return lost materials, heal the target or reactivate on later
drop/reload. Equipping, wearing, installing or taking the target to keep cancels the item order.
Changing to another owned processing action replaces this choice safely; S022/S096 compose directly.
Unrelated designations remain until the physical target no longer exists. Leaving the map suspends
the order and clears local claims; return requires explicit Resume. Save/load preserves pending or
suspended state and actual HP without replaying an already applied strike or destruction effect.

#### Scenario: Cancel a partly damaged item
- **WHEN** the player cancels after a blow has reduced HP but before the next attack is released
- **THEN** no further disposal attack is released, the pending wind-up is cancelled and the item keeps its reduced HP.

#### Scenario: Draft and later resume
- **WHEN** a worker is drafted during disposal and later becomes available again
- **THEN** drafting takes priority, existing damage remains, and an uncancelled order can later continue through ordinary work rules.

#### Scenario: Switch to recycling
- **WHEN** an eligible damaged weapon is changed from destruction to owned recycling
- **THEN** no further destruction attack is released and only recycling remains the processing choice
- **AND** the weapon retains the damage already caused and unrelated orders are not broadly erased.

#### Scenario: Reload with pending work
- **WHEN** a save is reloaded with a partly damaged target and a pending or explicitly suspended order
- **THEN** the target retains its actual HP and that order state, with no automatic replay of the last hit
- **AND** an item already destroyed before saving is not recreated or destroyed again.

### Requirement: Ineffective or unsupported work ends visibly
The order SHALL stop the current attempt with a contextual reason if no legitimate attack can start, or after ten completed attack cycles without reducing the lowest HP observed during that attempt or the remaining marked quantity.
Ordinary cooldown/wind-up is not a failed attack cycle. Merely trying to start an attack is not a
completed cycle. A new lower HP or smaller authorized quantity resets the ineffective-cycle count.
The ineffective count and lowest observed HP survive ordinary interruptions, unchanged job restarts,
split/merge and save/load. These events do not refresh the allowance. Blocked no-progress work stays
suspended until explicit Retry or a genuine change in available attack or target susceptibility;
merely selecting another otherwise equivalent worker does not refresh the allowance. Unchanged
automatic scanning cannot restart the same loop. No fixed damage or
direct deletion bypasses the obstruction. This ten-cycle limit is a proposed named safety policy,
not a destruction-speed multiplier.

#### Scenario: Damage is consistently absorbed
- **WHEN** ten completed attacks produce no lower target HP and no reduction in marked quantity
- **THEN** the worker stops, the order reports ineffective attacks and the same unchanged job is not immediately assigned again.

#### Scenario: Long ordinary cooldown
- **WHEN** the worker is legitimately recovering from its last attack
- **THEN** waiting ticks do not consume the ineffective-attack allowance or create extra attacks.

#### Scenario: Restart after nine ineffective attacks
- **WHEN** an unchanged order is interrupted and reloaded after nine completed ineffective attacks
- **THEN** the next completed ineffective attack reaches the ten-cycle limit and suspends work instead of starting a fresh allowance.

#### Scenario: Retry after fixing a capability
- **WHEN** a player changes the worker's available melee capability and retries a suspended target
- **THEN** a fresh bounded attempt uses the new real attacks and current HP without restoring damage or guaranteeing success.

### Requirement: UI and compatibility do not turn disposal into a cheat
The feature SHALL expose ordinary Orders/contextual controls, HP and marked-quantity status, blocked reasons, Cancel, Resume and Retry without a new bottom tab, debug-delete control or separate work category.
It introduces no global/save preference; target choices are gameplay state. Instant mode, source
damage/speed multipliers and source simultaneous-worker settings are not adopted. Compatible
ordinary content qualifies by actual item/HP/melee behavior. Unsupported native or provider paths
leave affected attacks unavailable, preserve item damage/order state and explain the reason.
Startup logs identify each skipped enabled integration and fallback without per-job spam. Original
Destroy Item active withholds overlapping owned attacks and suspends saved orders; after safe
activation they require explicit Resume. No mod is removed and no original order/save is imported.
Bench disposal from another mod is not itself proof of global incompatibility, but conflicting
processing orders on the same item must be resolved without running two owners simultaneously.

#### Scenario: Original provider active
- **WHEN** a save with owned disposal orders is loaded alongside original Destroy Item
- **THEN** owned attacks remain suspended, existing damage is preserved and the player sees the overlap reason
- **AND** removing that provider does not silently resume destructive work.

#### Scenario: Unsupported melee behavior
- **WHEN** a custom attack's targeting, cancellation or damage behavior cannot be used safely for this order
- **THEN** the affected route remains unavailable with an explanation, while other supported ordinary gameplay remains usable
- **AND** no timer or delete action substitutes for the required real attacks.
