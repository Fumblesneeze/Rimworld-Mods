# Practical supplies and inventory

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S024, Common Sense replacement; specification only. S021 shared capacity/quantity and S009–S012
bill-browser contracts remain authoritative. Proposed corrections are recorded under CS-Q1–4.

## ADDED Requirements

### Requirement: Collect bill ingredients within real shared capacity
With batch ingredient collection enabled, eligible workers SHALL collect multiple required ingredient portions in one trip where their real remaining carrying capacity permits, then physically deliver them to the same bill's work site.
S021 combined capacity includes personal supplies, ordinary inventory and hands; there is no separate
unlimited bill inventory or capacity-bypass option. Use smaller batches or the supported native
single-load fallback when necessary. Preserve recipe ingredient counts, allowed materials, dominant
material, reservations, unfinished-item association, bill quotas and native work/output. Changing
pickup order must not change the resulting product's material. Autonomous benches receive items in
their proper containers. Already present ingredients are counted once. A source-derived packing
delay is max(30 ticks, item count × base mass /10) for each inventory transfer; it creates no recipe
work or skill credit. Required native handling remains in addition.

For S014 linked bills, ingredient collection starts one shared iteration. All trips, intervening
cleaning, unfinished work and reload continuation belong to that same iteration. Its ingredient,
radius and worker choices remain those committed at its start; later bill edits govern new iterations.
Current physical access, reservations, safety and actual capacity still apply at each pickup. A
continuation neither releases nor claims a second shared quota merely because a trip or cleaning
phase ends. Ordinary cancellation or genuinely nonresumable work follows S014's recovery/count rules.

#### Scenario: Two materials fit together
- **WHEN** a worker can carry the required wood and components together under combined capacity
- **THEN** both are physically collected and delivered, and the bill consumes its full original amounts once.

#### Scenario: Existing supplies occupy capacity
- **WHEN** a worker carries personal food or equipment that leaves insufficient capacity for all ingredients
- **THEN** it collects a smaller legal batch and does not dump protected supplies or exceed capacity to finish in one trip.

#### Scenario: Collection route changes the pickup order
- **WHEN** the shorter route visits a secondary ingredient before the intended primary material
- **THEN** production still uses the selected primary material and ordinary output rules.

#### Scenario: Provider or bill becomes unavailable
- **WHEN** an unsupported custom bench or deleted bill prevents completing a partially collected batch
- **THEN** no substitute recipe runs, physical cargo remains accounted for, and supported native recovery handles it safely.

#### Scenario: Linked bill edited between collection trips
- **WHEN** a steel iteration has started collecting and the player changes the linked bill to plasteel with a smaller radius before its next trip
- **THEN** that existing iteration retains its original material and radius, subject to current physical access and reservations
- **AND** new iterations use the edited choices; saving and reloading during collection or cleanup neither duplicates the shared iteration nor its ingredients.

### Requirement: Storage detours must help the pending bill
With bill-supply storage detours enabled, a worker SHALL use the single composed S125 opportunity
decision for selected bill ingredients before their ordinary delivery. The destination must be
strictly nearer the work by a supported actual resource-to-work route; the legal haul includes at
least one extra unit beyond the immediate selected portion and meets S125's whole-trip limits.
A room boundary alone neither authorizes nor prohibits it. Storage filters, priority (including
only S125's explicit equal-priority supply exception), reservation, refill and S021 capacity rules
remain authoritative. No haul is invented solely because coordinates look close across levels.
This is the same effective preference as S125 bill supplies, not a separate scheduling producer.
At most one accepted detour belongs to a pending bill attempt through resumption and reload.
Existing queues, forced orders, emergencies and S125's bleeding/loading exclusions retain priority.
Unsupported routing or lack of useful surplus skips the detour, not the bill or its ordinary
ingredient collection. The current-play Hauling opportunities control can suppress this detour.

#### Scenario: Useful storage along the way
- **WHEN** a selected ingredient stack can be hauled to valid nearer storage before a nonforced bill
- **THEN** the worker makes one legal physical haul and returns to ordinary bill selection without a recursive detour chain.

#### Scenario: Storage across an unsupported level transition
- **WHEN** route length or destination access cannot be established reliably
- **THEN** the worker uses the ordinary bill path and no misleading distance comparison initiates a detour.

#### Scenario: Shared preference and no useful surplus
- **WHEN** S024 and S125 both encounter a bill but the legal load has no unit beyond its immediate selected portion
- **THEN** their single decision inserts no storage detour and ordinary bill ingredient collection proceeds.

### Requirement: Prefer eligible perishable supplies without broadening permission
With perishable ingredient preference enabled, ingredient selection SHALL favor eligible material nearer its ordinary rot-start threshold while preserving the choices applicable to that iteration, explicit material selection, native medical choices and current accessibility.
Use the source-derived order: greater medical potency first where the native recipe uses that
comparison, perishable before nonperishable, then smaller whole remaining spoilage hours. For an
output-material ingredient governed by an enabled Use best by bill choice, that explicit material
criterion takes precedence. An explicit Best Mix ingredient preference follows that output-material
criterion and precedes this spoilage ordering; Default contributes no ingredient criterion. Spoilage
applies among remaining ties. Other ingredient slots skip output-material comparison but retain an
explicit Best Mix criterion ahead of ordinary perishable ordering. The linked-group candidate
addition below remains enabled independently of which criterion ultimately decides. Surgery
selection is not reordered by this preference. When a discovered perishable candidate belongs to
linked storage, consider eligible contents throughout that actual storage group, retaining the
iteration's applicable search limit and actual access; do not treat group totals as locally reachable
supplies. For a started linked iteration, the preceding S014 rule determines its filters and radius.

With perishable meal preference enabled, ordinary food-choice scoring receives a bonus of
(6−2d) squared for d remaining rot-start days strictly between 0 and 3; outside that interval no bonus
is added. Native diet, ideology, forbidden state, social propriety and food safety remain permission
checks. The preference does not guarantee that every pawn selects the earliest-spoiling meal.

#### Scenario: Eligible ingredients in linked shelves
- **WHEN** a discovered ingredient is stored in a linked group with an older eligible portion within the bill's actual reach and radius and no higher-priority explicit criterion decides against it
- **THEN** the older portion participates in selection without permitting inaccessible or disallowed group contents.

#### Scenario: Material lock outranks spoilage
- **WHEN** a bill explicitly requires one material and another is nearer spoilage
- **THEN** only the permitted material can satisfy that ingredient.

#### Scenario: Meal bonus preserves diet
- **WHEN** allowed meals have one and two days of remaining rot progress
- **THEN** their ordinary scores receive bonuses of 16 and 4 respectively
- **AND** a forbidden meal receives no permission to be eaten merely because it would score well.

#### Scenario: Bill material preference precedes spoilage
- **WHEN** a supported bill explicitly prefers a higher-stat output material while a lower-ranked permitted material spoils sooner, with Best Mix left at Default
- **THEN** its sufficient higher-ranked material wins, while equally preferred material stocks still use this spoilage ordering
- **AND** secondary ingredients retain their ordinary perishable preference.

#### Scenario: Explicit ingredient mode precedes spoilage without shrinking discovery
- **WHEN** a bill explicitly selects Best Mix Nearest while linked-storage candidate expansion is enabled
- **THEN** eligible linked contents still participate within radius/access, but nearer supplies win before spoilage when higher-priority material choices tie.

### Requirement: Distinct ingredient concerns prevent meal merging
With meal separation enabled, otherwise stack-compatible prepared food SHALL remain separate when its recorded ingredient concerns differ between humanlike meat, other negative-thought ingredients, both, or neither.
Treat odd ingredients as ordinary defaults off; enabling it ignores only the nonhuman negative-thought
distinction. Humanlike ingredients remain separate. Positive ingredient effects alone do not create
a new category. This adds a restriction to native stacking and never forces an otherwise illegal
merge. Existing mixed stacks are not rewritten or split retroactively by a preference change.

#### Scenario: Insect and ordinary meals
- **WHEN** otherwise compatible insect-containing and ordinary meals meet with separation enabled
- **THEN** they do not merge unless Treat odd ingredients as ordinary is enabled.

#### Scenario: Humanlike ingredients stay distinct
- **WHEN** the odd-ingredient exception is enabled
- **THEN** food containing humanlike ingredients still cannot merge with otherwise comparable nonhuman food.

### Requirement: Optional generated provenance preserves real ingredients
When generated meal provenance is enabled, supported newly generated meals without actual ingredient history SHALL receive a stable neutral ingredient description compatible with a producing recipe, without changing their quantity, nutrition or production cost.
The option defaults off. Candidate ingredients exclude humanlike meat, negative ingredient thoughts,
nested prepared-food history and incompatible special ingredient tags. If no valid recipe/ingredient
choice exists, leave history unspecified rather than fail generation or invent incompatible content.
Generated provenance is identified as inferred, not a claim that physical ingredients were consumed.
Real cooking, dispensers and provider-supplied histories retain their actual ingredients. Splitting,
merging, saving and reloading do not reroll, erase or add synthetic history. Disabling the option
affects future generation; it does not wipe existing descriptions or alter another mod's provenance.

#### Scenario: Generated meal with no history
- **WHEN** a supported trader or event meal is created with the option enabled and compatible neutral choices exist
- **THEN** it gains inferred ingredient information once and retains it after save/load and splitting.

#### Scenario: Dispenser uses humanlike ingredients
- **WHEN** a real dispenser produces a meal from recorded humanlike ingredients
- **THEN** that actual history remains visible and is not replaced with neutral generated ingredients.

### Requirement: Ingredient information is read-only and current
With expanded ingredient information enabled, recipe/bill information SHALL show relevant material categories or allowed ingredients instead of an uninformative generic label, without changing the underlying recipe or filter.
Use the composed Add bill browser and native information surfaces. Current exclusions and selections
must remain visible; listing a category must not imply that every member is allowed. Respect a
provider's meaningful custom summary. Item information also shows available category paths, including
all assigned categories in details. No new bottom tab or hidden dummy crafting recipe is exposed.

#### Scenario: Filter changes while inspecting a recipe
- **WHEN** the player excludes one material and reopens ingredient information
- **THEN** the displayed eligibility reflects that exclusion and inspecting it does not change the filter.

### Requirement: Explicit unloading is physical and cancellable
The player SHALL be able to mark a selected whole-number quantity of controllable inventory items, or one removable worn/equipped item, for later unloading from the native Gear surface, with visible remaining quantity and cancellation.
Marking applies only to the selected available portion, never the whole merged stack by implication.
It temporarily releases that portion's previous keep protection for this explicit unload order.
Each successful placement reduces only the actually delivered quantity; undelivered portions remain
pending through split, merge, interruption and reload. Cancel can affect a selected still-pending
quantity and restores its prior keep/unowned status unless a newer explicit player choice supersedes
it. It cannot recover delivered items or alter unrelated portions. Existing S021 delivery quantities
are shown as already pending rather than given a second order; cancelling this new order cannot
cancel those pre-existing deliveries. Their keep/release controls remain the S021 workflow.
Quest/lender restrictions, locked apparel and current permissions apply when marking and again when
acting. The worker uses valid storage when available, retains normal removal/handling time and actually
places the item. The source-derived minimum for removing a weapon is 30 ticks; apparel uses its native
equip delay. If storage is unavailable, safely remove and drop the item nearby where legal, or wait
with a reason. No fallback loses an equipped item by pretending it was already in inventory.
Cancellation stops the selected future optional unloading and does not itself drop an item, including when the
pawn is over capacity; ordinary encumbrance handling remains independent. Completed drop/transfer,
new explicit equip/keep choices and cancellation clear obsolete intent only for their actual affected
portions. Supported remembered
sidearms are forgotten only after successful explicit unloading, not merely on a failed attempt.

#### Scenario: Unload worn apparel without storage
- **WHEN** the player marks removable apparel and no valid storage cell exists
- **THEN** the pawn removes and safely drops it through normal handling, or visibly waits if that cannot be done.

#### Scenario: Cancel an unload mark
- **WHEN** the player cancels a pending mark while the pawn is encumbered
- **THEN** cancellation removes that order without immediately dropping the item or resurrecting it after equip/reload.

#### Scenario: A restriction changes before unloading
- **WHEN** marked apparel becomes locked or otherwise nonremovable
- **THEN** the pending action cannot bypass the new restriction and the reason is visible.

#### Scenario: Partial unload and cancellation in a mixed stack
- **WHEN** three of five kept units are marked, one is delivered, and the remaining four merge with two pre-existing S021 cargo units
- **THEN** two units remain under the new unload order, two remain kept and two remain S021 cargo units
- **AND** cancelling one new pending unit leaves one new unload unit, three kept units and two S021 cargo units after reload, without recalling the delivered unit.

#### Scenario: Already pending cargo is selected again
- **WHEN** the player selects a quantity already scheduled for S021 delivery
- **THEN** the controls show its existing delivery intent without adding duplicate unload work
- **AND** cancellation of a separate new unload order does not cancel that original cargo delivery.

### Requirement: Interruption preserves actual inventory ownership
With return-to-inventory enabled, an item taken from a pawn's inventory for an interrupted activity SHALL return to that pawn's inventory when still appropriate and within shared capacity, otherwise use safe physical overflow recovery.
This never reverses a deliberate drop/unload/give order or reclaims an item already transferred to
someone else. S021 exact kept/cargo quantities remain authoritative through merge, split and partial
transfer. Do not mark unrelated merged inventory for unloading or silently erase an existing order.
Failures retain one physical copy and visible recoverable state; do not loop equip/drop or block
drafting, combat, baby handling or urgent medical work. Save/load preserves pending intent without
replaying completed transfers. Recovery remains available when its entry control is hidden.

#### Scenario: Interrupted inventory meal
- **WHEN** drafting interrupts a pawn holding its own uneaten inventory meal
- **THEN** the same remaining portion returns to inventory if capacity permits, otherwise is safely placed without duplication.

#### Scenario: Explicit drop overrides return
- **WHEN** the player explicitly orders that item dropped or handed over
- **THEN** interruption recovery does not put it back into the original pawn's inventory.

### Requirement: Food packing keeps native eligibility and shared limits
The source-derived food-packing opportunity SHALL become eligible above 30% current Food rather than 60%, while retaining ordinary packing requirements and S021 shared capacity.
This trigger does not force food collection regardless of stock, diet, existing supply or ability.
S123 [routine personal meals](../../../specify-megamod-routine-meal-carrying/specs/megamod-routine-meal-carrying/spec.md)
owns the complete shared reserve quantity, actual-eater nutrition, eligibility, pickup/retention
and explicit-pause behavior. It removes the native communal nutrition entry check so shortages
alone do not stop packing, while deliberately refining the absolute inventory threshold and minimum candidate-stack rule,
cargo attribution and indivisible-meal retention. Both use this same opportunity and S021 capacity
authority, never a second pack/unload loop. The colonist reserve promise excludes animals.

#### Scenario: Colonist above the lower packing threshold
- **WHEN** an eligible colonist has 40% Food, little personal food, an available suitable meal and capacity
- **THEN** it can perform ordinary food packing without waiting to exceed 60% Food.

#### Scenario: Shortage alone does not block packing
- **WHEN** communal food is scarce but an otherwise eligible colonist can reserve and carry an available suitable meal
- **THEN** packing remains possible without a minimum colony nutrition total; actual diet, claims and capacity still apply.
