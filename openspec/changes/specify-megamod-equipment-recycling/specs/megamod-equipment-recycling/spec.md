# Weapons and apparel recycling

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Origin: original, S022 / P1-17. Specification only. Facility, refund, work and lifecycle choices
below are explicit proposals refining confirmed weapons/apparel scope; ER-Q1–4 retain later work.

## ADDED Requirements

### Requirement: Recycling is an explicit item order
The player SHALL be able to designate eligible loose weapons and apparel for recycling through the Orders tools and the selected item's contextual controls, without adding or editing a production bill.
The order marks only the selected physical items and quantities, not all future items of the same
type or everything later placed in a storage area. A drag uses the same eligibility as individual
selection and ignores concealed cells. Marked items show the pending action and can be cancelled.
No bottom tab or new work category is added. Selecting recycling is not instant destruction.

#### Scenario: Select raid equipment
- **WHEN** the player drags Recycle over loose eligible weapons, clothing and nearby raw steel
- **THEN** only the selected eligible weapons and clothing receive recycling orders
- **AND** the steel, existing bills and unrelated future drops remain unchanged.

#### Scenario: Individual command agrees with the tool
- **WHEN** the same item is examined through its contextual command and the Orders tool
- **THEN** both permit or reject recycling for the same reasons, including possible material recovery.

#### Scenario: Repeated designation
- **WHEN** an already marked item is designated again
- **THEN** it retains one pending order and cannot be processed twice for that designation.

### Requirement: Only eligible unused equipment is processed
Recycling SHALL accept weapons and apparel with known positive possible material recovery, while respecting native item restrictions, ownership, access, fire and reservations.
Buildings, minified buildings, resources, corpses and living pawns are outside this order. Relics
are excluded. Worn, equipped or personally kept equipment is not stripped or taken from a pawn.
Marking does not automatically unforbid an item, reveal it, grant access or override another job's
reservation. A valid order can wait for access or a station and shows the reason. Equipping or
wearing the target cancels its pending recycling, so dropping it later does not silently reactivate
disposal. Ordinary hauling of marked equipment preserves the outstanding quantity instead.

#### Scenario: Clothing in use
- **WHEN** equipment is worn, wielded or explicitly kept as personal inventory
- **THEN** it cannot be newly ordered for recycling through this feature.

#### Scenario: Equipment becomes worn before processing
- **WHEN** a pawn equips or wears an item with a pending recycling order
- **THEN** the order is cancelled without removing or damaging the equipment
- **AND** later dropping it requires a fresh recycling order.

#### Scenario: Forbidden or unavailable material
- **WHEN** marked equipment becomes forbidden, burns or cannot be reserved or reached
- **THEN** processing waits or stops safely, with the input preserved unless ordinary external damage destroys it
- **AND** the order itself does not bypass those restrictions.

#### Scenario: No recoverable output
- **WHEN** current supported recovery rules permit no possible material output
- **THEN** recycling is unavailable with an explanation, rather than silently becoming a destroy order.

### Requirement: Use a suitable functioning local facility
The worker SHALL physically carry one marked equipment unit at a time to a reachable suitable facility and perform recycling there under ordinary Crafting work eligibility and priority.
Smeltable equipment uses a workbench that offers the applicable native weapon or apparel smelting
operation and accepts that item. Other eligible equipment is scrapped at a tailoring bench or
crafting spot capable of making a basic shirt or tribalwear. The nearest eligible facility to the
item is preferred, with stable ties; a blocked or unusable nearer bench does not hide a usable one.
At the same work priority, existing native Crafting bill work is considered before automatic
recycling; explicit player work orders retain their normal precedence.
Both item and facility must be reachable and reservable for the actual worker, including its
interaction position. All routes respect power, fuel and other current facility conditions.
Native refueling can prepare a fuelable bench before recycling; it does not consume the equipment.
No original manufacturing research or skill is newly required merely to recover materials, while
ordinary worker and facility restrictions still apply. Capable colony mechs can participate only
where their actual work abilities permit Crafting; animals gain no crafting ability.

#### Scenario: Metal weapon and cloth shirt
- **WHEN** a suitable smelter and tailoring station are available for a marked metal weapon and cloth shirt
- **THEN** eligible workers carry the weapon to smelting and the shirt to scrapping
- **AND** no bill setup is required and no item is recycled remotely.

#### Scenario: Nearer station is blocked
- **WHEN** the closest matching bench cannot be reached or reserved but another suitable bench can
- **THEN** the reachable usable bench can be selected rather than leaving the job permanently stuck.

#### Scenario: Fuel preparation
- **WHEN** a suitable fueled station is empty and native refueling is permitted with available fuel
- **THEN** an ordinary refuel job can prepare it before recycling starts
- **AND** the marked equipment remains intact during preparation.

#### Scenario: Power lost while scrapping
- **WHEN** an electric tailoring bench loses power during recycling work
- **THEN** work stops without consuming the equipment or awarding materials and can resume when a valid facility is available.

### Requirement: Work takes time without creating extra skill or capacity
Each equipment unit SHALL require 1600 base work, modified by the worker's current Smelting Speed for smelting or General Labor Speed for scrapping and the facility's actual work-speed factor.
No positive work rate is invented when the worker/facility cannot work. Recycling grants no extra
Crafting experience or skill-based yield bonus. Carrying retains actual native limits and S021's
requirement to recover owned inventory cargo before adding a separate ordinary load. Work progress
survives a safe interruption and reload for the same surviving item unit; it cannot transfer to a
different item. Changing to another valid facility retains completed work but uses current speed.

#### Scenario: Slower facility
- **WHEN** the worker's applicable speed is 1 and the usable bench's factor is 0.5
- **THEN** 1600 remaining work takes 3200 working ticks, apart from carrying and other native delays.

#### Scenario: Draft during work
- **WHEN** the player drafts a worker halfway through recycling
- **THEN** work stops immediately through normal job interruption, with no recovered products yet
- **AND** another eligible attempt on that same item can finish its remaining work.

#### Scenario: Loaded recycler
- **WHEN** a worker already holds owned hauling cargo and recycling needs an ordinary hand-carried load
- **THEN** the cargo is safely recovered first and recycling does not add a second hauling allowance.

### Requirement: Material returns are explicit and tied to the actual item
Recycling SHALL base construction-material recovery on the item's current declared material costs adjusted for its actual material and physical unit volume, initially returning 25% of those quantities.
This is a nominal construction-cost basis, not a reconstruction of historical ingredients or a
market-value conversion. It includes ordinary nonmetal materials such as wood, stone, cloth and
leather. Complex ingredients such as components are excluded from initial support, including fixed
salvage. For each remaining material, combine the percentage-adjusted construction amount and its
separately declared fixed salvage once, then multiply by the item's **current HP / current maximum
HP**, limited to0–1. Fixed salvage is exempt from the construction percentage, not from condition
scaling. At full HP the original nominal return remains; a nearly ruined item yields very little.
Read condition immediately before consumption, so further damage cannot preserve an earlier full-HP
quote. Quality and worker skill add no recovery multiplier. Unsupported or invalid condition/cost
semantics withhold the route rather than assume an intact item or guessed material.
Round each material's final positive fractional amount probabilistically once; the fractional part
is the chance of rounding up. Never round the construction or fixed contribution separately first. Previewing never draws or changes the eventual result. The order displays material
types, possible quantities and fixed-salvage distinctions before the irreversible step.

#### Scenario: Actual cloth versus leather
- **WHEN** an intact cloth shirt and intact leather shirt each have a declared cost of 40 ordinary units and use the default rate
- **THEN** each returns 10 of its own actual material, not a default textile or a quality-dependent substitute.

#### Scenario: Fractional recovery and complex exclusion
- **WHEN** an intact autopistol with declared costs of 30 steel and 2 industrial components is recycled at defaults
- **THEN** it returns 7 or 8 steel with equal chance and no components
- **AND** the preview states that range rather than promising 8 steel.

#### Scenario: Small-volume material
- **WHEN** an intact supported equipment item costs 40 ordinary material units supplied by 400 physical units of a material with volume 0.1
- **THEN** the default construction return is 100 physical units, converting volume exactly once.

#### Scenario: Fixed salvage at zero construction return
- **WHEN** construction return is0%, an item is intact and it declares two fixed non-complex salvage units
- **THEN** those two fixed units can still be recovered and the preview explains why the construction percentage does not remove them.

#### Scenario: Damage scales all recovery
- **WHEN** an item costing40 cloth and declaring two fixed cloth salvage units has1 HP out of100 at consumption, with25% construction return
- **THEN** its expected cloth return is(10 +2) times0.01 =0.12, yielding one cloth with12% chance and otherwise none
- **AND** fixed salvage does not give a guaranteed intact-item return from a ruined item.

#### Scenario: Condition changes while work is interrupted
- **WHEN** a partly recycled intact item is damaged to half health before work completes
- **THEN** current preview and completion use half the nominal recovery, with no stale full-health refund.

#### Scenario: Positive chance but no actual return
- **WHEN** an item has a supported positive fractional construction return that rounds to zero on completion
- **THEN** the equipment is consumed with no material from that entry, as described by its preview
- **AND** retrying or reloading the completed operation cannot produce another roll or another item.

### Requirement: Completion conserves items and cannot run twice
Recycling SHALL consume exactly the authorized equipment unit once and make only its resolved outputs available, preserving a recoverable state if completion or output placement fails.
Materials appear through physical placement at or near the facility, respecting stack limits and
existing contents; nothing is remotely credited to storage. Output hauling is ordinary later work.
While the equipment remains intact, cancellation preserves it and awards no products. Once the
equipment has been consumed, cancellation cannot restore it or reroll its resolved outputs. If output
space is unavailable after consumption, the owed products remain identifiable and recoverable,
and retry releases only the undelivered remainder. Recovery status and an explicit retry action
remain reachable through contextual item-order controls even if the original bench disappears;
placing owed products requires neither another equipment item nor repeated recycling work.
The system must not leave usable refunds beside
an input that can be recycled again. Output generation does not run again because work is reloaded.

#### Scenario: Two workers target the same item
- **WHEN** two eligible workers attempt the same marked equipment
- **THEN** only one can reserve and process that unit and the colony receives at most one set of returns.

#### Scenario: Output cannot be fully placed
- **WHEN** the equipment has been consumed for three resolved products but only one can currently be placed
- **THEN** that product and the two remaining owed products together equal the single resolved result
- **AND** later recovery places only the remaining two without recreating the equipment or first product.

#### Scenario: Completion is unsupported before consumption
- **WHEN** a required processing or recovery capability is unavailable while the equipment is still intact
- **THEN** the equipment stays intact, no products become usable, and the order is visibly suspended.

#### Scenario: Original facility disappears during output recovery
- **WHEN** equipment has been consumed but some of its materials still await placement and the original bench is removed
- **THEN** the remaining output and retry action remain visible through item-order recovery controls
- **AND** retry places the owed material safely on the map when space permits, without another bench, input or recycling work.

### Requirement: Processing choices and quantities survive ordinary item movement
The mod SHALL retain surviving authorized recycling quantities through supported carrying, splitting, merging and saving, without capturing unmarked or suspended items merely because they share a type or stack.
Each portion retains its processing choice and active/suspended state. Merging or resuming another
portion cannot activate a suspended quantity. Process an active unit only when its attribution is
safe; otherwise wait with an explanation. Known ordinary consumption/removal retires the corresponding
order. If removal identifies only a count from mixed marked/unmarked material, reduce the outstanding
recycling quantity first, down to zero, rather than reassign it to survivors. If distinct processing
choices or suspension states prevent reliable attribution, pause affected surviving orders until the
player explicitly resolves their quantities. These are shared rules for owned item processing.
Only originally marked units are processed, one at a time. A current owned processing choice is
exclusive: explicitly choosing a different owned destruction or recipe-processing action replaces
the unfinished recycling choice safely. Unrelated orders are preserved unless consuming the actual
item makes them moot. Ordinary hauling can move pending material; once recycling owns the item,
normal reservations prevent a second job from taking it. Leaving the map suspends local processing
and clears obsolete station claims; returning material needs an explicit resume. Missing references
do not authorize a substitute. Taking marked material to keep cancels its recycling authorization.

#### Scenario: Marked stack merges with unmarked equipment
- **WHEN** two marked units merge with three unmarked compatible units
- **THEN** only two remain authorized for recycling and the other three survive processing.

#### Scenario: Active and suspended equipment merge
- **WHEN** three actively ordered units merge with two recycling units still requiring explicit resume
- **THEN** only the active three can be processed with safe attribution, or work waits visibly
- **AND** the other two retain their suspension through splitting and reload; resuming another portion does not resume them.

#### Scenario: Unspecified ordinary removal from a mixed stack
- **WHEN** another ordinary action consumes three units from two marked and three unmarked compatible units without identifying the removed portion
- **THEN** the recycling quantity falls to zero and the two physical survivors remain unordered unless the player explicitly marks them again.

#### Scenario: Change processing intent
- **WHEN** the player changes an unfinished recycling order to another supported owned processing action
- **THEN** recycling stops without consuming the item, its partial recycling work is discarded, and only the new action remains authorized
- **AND** unrelated orders on the surviving item are retained.

#### Scenario: Cancel or keep after pickup
- **WHEN** a player cancels recycling or takes the carried equipment to keep before it is consumed
- **THEN** no refund is produced, the surviving item remains physical, and pending recycling does not reappear after it is dropped.

#### Scenario: Save and reload interrupted work
- **WHEN** a save with partially worked intact equipment or materials awaiting placement from consumed equipment is reloaded
- **THEN** the surviving equipment resumes only its own remaining work, or materials from already consumed equipment resume only outstanding placement
- **AND** neither path duplicates an item, refund or random result.

### Requirement: Recycling preferences use common settings
The mod SHALL expose Construction material return initially 25% with whole-percent values from 0 through 100, and Show selected-item recycling command initially on through the common global-default and per-save controls.
Each row has Global default, Current save and Use global. New saves inherit; using global preserves
a dormant override and disables its per-save value control. Without a loaded save, all save-specific
controls and mutation callbacks are disabled. Show-command changes update contextual access without
hiding the Orders tool or cancelling existing orders. Recovery choices are captured when work first
starts on an item unit; changes apply to units not yet started. Partially completed work shows its
retained recovery rules. Cancelling and creating a fresh order discards old progress and adopts
current rules. Settings explain condition scaling, fixed salvage and the progression impact of high construction returns.
No initial control enables complex-ingredient recovery; that expansion is deferred.
Per-item designations, cancellation and progress remain gameplay state, not mod settings.

#### Scenario: Existing work and changed return rate
- **WHEN** the player changes return from 25% to 50% while an item has partly completed work
- **THEN** that item retains its displayed 25% terms and the next unstarted item uses 50%.

#### Scenario: Inheritance and main menu
- **WHEN** Use global is selected in a loaded save
- **THEN** its saved override is retained but inactive; clearing Use global restores it or seeds the first override from the current effective value
- **AND** at the main menu only Global default is editable, with no mutation of a prior or implicit save.

#### Scenario: Hide contextual command
- **WHEN** Show selected-item recycling command is disabled
- **THEN** new recycling remains accessible through Orders and existing orders remain inspectable and cancellable.

### Requirement: Unsupported integrations preserve ordinary play and unfinished inputs
The mod SHALL keep unsupported recycling routes unavailable with an explanation, while leaving ordinary bills and other supported routes usable and preserving intact unfinished equipment and materials still owed from already consumed equipment.
Compatible external equipment and facilities qualify by their actual supported capabilities, not
their display names. Unknown custom salvage or station restrictions are not ignored. If original
Recycle This is active, overlapping owned new recycling is withheld and unfinished owned work is
suspended until an explicit resume after safe activation; outputs from already consumed equipment remain
recoverable once. No mod is removed, source order imported or original work category impersonated.
Startup warnings identify each enabled unavailable integration, its reason and fallback without
per-job spam. An unrelated compatible modification alone is not a reason to disable recycling.

#### Scenario: Changed external station
- **WHEN** a custom workbench no longer supplies its validated processing capability
- **THEN** it is unavailable for that route with an explanation and another valid station can still be used
- **AND** no input is consumed merely because a previously cached station list included it.

#### Scenario: Original provider on an existing save
- **WHEN** the save is opened with original Recycle This and unfinished owned recycling
- **THEN** unfinished owned equipment stays intact and visibly suspended, while materials from previously consumed equipment remain recoverable
- **AND** removing the provider does not silently resume destructive work before the player explicitly resumes it.
