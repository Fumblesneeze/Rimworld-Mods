# Shared carried food

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin S055, Meals On Wheels - Continued. Observable draft only; nothing implemented or accepted.

## ADDED Requirements

### Requirement: MW01 Borrow food only when ordinary selection fails
The mod SHALL extend ordinary on-map self-feeding for free colonists who can normally seek food,
use tools and manipulate items. When ordinary food selection finds no source and permits inventory
and animal-inventory access, the colonist SHALL be able to obtain eligible food from another spawned
player-faction pawn on the same map, including colony animals. Successful ordinary choices SHALL
remain authoritative. The feature SHALL NOT create hunger, change eating-job priority, supply food
from another map or alter world-caravan eating. Animal eaters, foreign visitors, prisoners, slaves
and caregiver jobs SHALL gain no additional food-sharing action from this change; their existing
native behavior remains intact.

#### Scenario: Food on another colonist
- **WHEN** a free colonist's normal food search fails and another reachable colony pawn carries eligible food
- **THEN** the colonist can retrieve an appropriate portion and eat it through normal eating behavior.

#### Scenario: Existing ground or inventory food
- **WHEN** ordinary food selection succeeds, including food on the ground, in the eater's inventory or on a colony animal
- **THEN** the source remains selected even if another pawn carries more attractive or nearer-spoiling food.

#### Scenario: Inventory access intentionally excluded
- **WHEN** the requesting food search disallows inventory or animal-inventory access
- **THEN** this feature does not supply a carried-food fallback or move any food.

#### Scenario: No new feeding eligibility
- **WHEN** a pawn cannot normally feed itself, lacks manipulation or belongs to an excluded eater group
- **THEN** this feature adds no sharing job or exemption from its existing feeding rules.

#### Scenario: Map and world boundaries
- **WHEN** eligible food exists only on another map or in a traveling world caravan
- **THEN** it is not borrowed by this on-map fallback and ordinary caravan eating is unchanged.

### Requirement: MW02 Food permissions remain specific to the eater and action
The fallback SHALL select only currently ingestible food that provides positive nutrition to the
eater and is allowed by the action's current diet, food policy, ideology, title, freshness, corpse,
drug and minimum-food-preference rules. Hunger SHALL not grant new exceptions beyond that action's
normal permissions. Teetotalers SHALL not receive nutritious drugs through this fallback. The donor's
policy SHALL not replace the eater's. Non-nutritious drugs, medicine, water and other nonfood items
SHALL not become meals through sharing.

#### Scenario: Diet and ingredients
- **WHEN** a donor carries food forbidden to the eater by its respected food policy or ingredient/religious restrictions
- **THEN** the fallback rejects it even if no other food is available.

#### Scenario: Donor has a different policy
- **WHEN** the donor's diet permits an item that the eater's current respected policy forbids
- **THEN** borrowing does not substitute the donor's permission for the eater's.

#### Scenario: Nutritious drugs
- **WHEN** available food includes a nutritious drug
- **THEN** it can be considered only under the eater's applicable food/drug permissions and never for a teetotaler; this does not authorize medical or recreational doses.

#### Scenario: Freshness and desperation
- **WHEN** a candidate is rotten, a corpse or below the requesting action's allowed food quality
- **THEN** it is rejected unless that action's existing permissions allow it; sharing adds no new desperation exception.

#### Scenario: Zero nutrition is not food
- **WHEN** an inventory contains medicine, a nonfood drug or an item giving this eater zero nutrition
- **THEN** it is not selected to satisfy hunger by this feature.

### Requirement: MW03 Accessible donors and food are ranked predictably
The mod SHALL consider current eligible contents throughout each eligible donor inventory, excluding
the eater, inaccessible or forbidden holders/food, unavailable quantities and a donor currently
feeding a baby. Existing claims for another active use SHALL remain protected. Within the pawn
fallback, fresh non-drug food of at least awful-meal preference that will start rotting in strictly
less than **12 in-game hours** SHALL be considered before other permitted food. At exactly12hours it
SHALL receive no such priority. The remaining fallback covers permitted food from desperate-only
through lavish-meal preference, further restricted by MW02.

Within each class, the existing food-preference scoring including actual donor distance SHALL
choose the best eligible option; otherwise equal choices SHALL prefer the nearer donor, with stable
ties. Enabled common meal-spoilage preference SHALL affect this score once, without granting food
permission or displacing a successful ordinary source. Inventory insertion order SHALL not hide an
eligible later stack. These are selection rules, not new settings or a change to spoilage speed.

#### Scenario: Later stack is the suitable meal
- **WHEN** a donor's first stack is unsuitable or less preferred and a later stack is eligible
- **THEN** the later stack participates rather than being hidden by the earlier one.

#### Scenario: Near-spoilage boundary
- **WHEN** otherwise eligible fresh non-drug meals have11hours and12hours until rot starts
- **THEN** the11hour meal enters the first fallback class and the12hour meal does not.

#### Scenario: Compare donor distance
- **WHEN** two donors carry otherwise equally ranked eligible food at different reachable distances
- **THEN** the nearer donor is selected, independently of which pawn was created first.

#### Scenario: Baby feeding remains protected
- **WHEN** the only potential donor is currently feeding a baby
- **THEN** this fallback does not take from that donor until the feeding job has ended and eligibility is reconsidered.

#### Scenario: Common spoilage preference
- **WHEN** common meal-spoilage preference is enabled while two food options are compared within one fallback class
- **THEN** its existing score adjustment is applied once, preserving this feature's class order and all food permissions.

### Requirement: MW04 Retrieval conserves food and respects carrying limits
Food discovery SHALL be read-only. A chosen portion SHALL move only after a real retrieval job reaches
the current holder and rechecks ownership, accessibility, food permission, available quantity and
claims. The requested quantity SHALL be the ordinary amount wanted for this meal, limited by actual
availability and the shared carrying allowance; an appropriate smaller usable portion SHALL be
allowed when the full amount does not fit. No additional reserve meal SHALL be taken by this feature.

After successful transfer the eater SHALL receive the actual transferred portion, retaining its
ingredients, condition and food-poisoning history, then use ordinary ingestion, duration, thoughts
and nutrition. Only food actually eaten SHALL be consumed. Failure SHALL not pretend the transfer
succeeded, remotely unload the donor, exceed capacity or discard protected supplies to make room.

#### Scenario: Search without a job
- **WHEN** food availability is queried repeatedly without starting a retrieval job
- **THEN** donor inventories, shuttle contents, map stacks and cargo quantities remain unchanged.

#### Scenario: Exact physical transfer
- **WHEN** one meal is successfully borrowed from a stack of five
- **THEN** the donor retains four and the eater receives that one physical portion before eating; no other stack is substituted.

#### Scenario: Partial capacity
- **WHEN** only part of the ordinarily wanted quantity fits the eater's shared carrying allowance
- **THEN** at most that usable part is transferred and eaten, with no hidden extra inventory allowance.

#### Scenario: No capacity or failed transfer
- **WHEN** no edible portion fits or the holder refuses the transfer
- **THEN** the food remains recoverable with its actual holder, no success or nutrition is reported and protected inventory is not discarded.

#### Scenario: Food history follows the portion
- **WHEN** a meal with ingredients, condition or recorded tainted portions is split and borrowed
- **THEN** the actual portion retains the corresponding history and normal ingestion consequences, without removing contamination by transfer.

### Requirement: MW05 Passenger-shuttle fallback uses a real unloading action
With admitted Odyssey passenger-shuttle content present, the mod SHALL offer eligible food from a
reachable, player-owned, stationary and currently accessible passenger shuttle on the same map only
after the ordinary and pawn-inventory food searches find nothing. MW02 food permissions and MW04
quantities/capacity SHALL apply. Existing loading, launch and transport claims SHALL remain valid;
food committed to an active departure or protected transfer SHALL not be taken.
All contents of a shuttle actively loading or ready under a loading plan SHALL be treated as
committed transport cargo. A waiting shuttle alone SHALL not prove its food is uncommitted.

The colonist SHALL reach the shuttle's supported retrieval point before food is removed. Successful
unloading SHALL hand off the actual resulting portion, including a split or merge, to normal eating.
The remainder SHALL stay in the shuttle. A failed unload, inaccessible container or departing shuttle
SHALL leave food intact and abandon that attempt safely. Other pods, vehicles, containers and world
caravans SHALL not be implicitly included by this optional branch.

#### Scenario: No Odyssey
- **WHEN** Odyssey or the required passenger-shuttle content is absent
- **THEN** pawn food sharing remains available without missing-content warnings.

#### Scenario: Reach shuttle and take a portion
- **WHEN** a colonist has no ordinary or pawn-inventory food and an accessible idle passenger shuttle holds five suitable meals
- **THEN** the colonist travels to it, retrieves only the permitted meal quantity, eats the actual retrieved portion and leaves the remainder aboard.

#### Scenario: Departure takes precedence
- **WHEN** the shuttle begins departure or its food becomes committed to an active protected transport before retrieval
- **THEN** borrowing stops without unloading, stranding, duplicating or consuming its cargo.

#### Scenario: Loaded and ready still protects cargo
- **WHEN** a shuttle is waiting on the map but has an active or completed loading plan ready for launch
- **THEN** its contents are not offered for automatic borrowing and its loading plan is not canceled to obtain a meal.

#### Scenario: Drop or handoff fails
- **WHEN** unloading cannot produce a safely accessible portion or the eater cannot accept it
- **THEN** the action does not report success or target the still-stored remainder, and all actual food stays recoverable at its true location.

### Requirement: MW06 Borrowing composes with owned inventory and care
The mod SHALL use the same physical quantities, holder facts and capacity accounting as hauling and
food search, with current action-specific permission checks. Borrowed cargo SHALL reduce the exact
donor cargo quantity; remaining deliveries SHALL not recreate it or unload another kept stack.
When an ordinary removal from an identical mixed stack names no specific portion, S021's cargo-first
attribution SHALL apply. The transferred eating portion SHALL not become a new storage-delivery order.

A general keep-for-inventory choice SHALL protect against unloading, not automatically prohibit
this food-sharing use. Explicit supported provider restrictions and active use claims SHALL still
apply. Future routine meal carrying SHALL use this same model; it SHALL not repeatedly steal the
borrowed eating portion back as surplus. Feeding/care jobs SHALL retain priority and permissions.
No separate bottom tab, food-sharing window or sample setting SHALL be added.

#### Scenario: Borrow from mixed cargo and kept food
- **WHEN** a donor has two kept and five hauled identical meals and one unassigned portion is borrowed
- **THEN** two kept and four hauled meals remain with the donor, the eater owns the one eating portion and no phantom delivery remains.

#### Scenario: Kept meal can still be used
- **WHEN** no ordinary food exists and the only eligible meal is kept in another colonist's inventory without a separate use restriction
- **THEN** it can be borrowed for eating; keeping it does not by itself lock it against colony food use.

#### Scenario: Provider protects the quantity
- **WHEN** an admitted provider protects a food quantity for an incompatible active use
- **THEN** that quantity is excluded from borrowing; unknown protection is not treated as unrestricted.

#### Scenario: Routine carrying after borrowing
- **WHEN** a borrowed portion is awaiting ingestion while routine meal carrying or cargo cleanup runs
- **THEN** it remains attributed to the eater's current use and is not stolen back, duplicated or reclassified as cargo.

### Requirement: MW07 Changed state stops safely without a retry loop
The mod SHALL reconsider current holder, food and action state before transfer and ingestion. A
moving donor SHALL be approached through normal pathing; departure, destruction, changed permission,
changed quantity or unavailable access SHALL cancel or reselect safely. Concurrent eaters SHALL not
claim more than the available portions. Interrupted or saved retrieval SHALL preserve actual items,
release stale claims and resume only if its current prerequisites remain true. A failed candidate
SHALL not cause repeated same-tick job-start failures; another candidate or normal no-food behavior
SHALL remain possible. Already transferred food SHALL recover under its current holder's ordinary
interruption rules, without teleporting it back to the donor.

#### Scenario: Donor moves or leaves
- **WHEN** the chosen donor moves during approach or leaves the map before transfer
- **THEN** normal pathing follows a still-valid donor, or borrowing ends safely without a remote transfer.

#### Scenario: Competing hungry colonists
- **WHEN** two eaters seek the last available meal
- **THEN** at most one receives it and the other reselects or returns to ordinary no-food behavior without a rapid failure loop.

#### Scenario: Diet changes while approaching
- **WHEN** the eater's current food permission changes before taking the chosen meal
- **THEN** a now-forbidden portion is not transferred or eaten by that attempt.

#### Scenario: Interruption after transfer
- **WHEN** drafting or another job interrupts eating after food has been transferred
- **THEN** the actual remaining food stays with the eater or follows safe ordinary placement, retaining quantity and history rather than reappearing on the donor.

#### Scenario: Save and load
- **WHEN** a save is loaded during approach or after partial transfer
- **THEN** current physical ownership determines recovery; no old-game claim duplicates food or points at an unrelated holder.

### Requirement: MW08 Unavailable sharing preserves ordinary feeding
The feature SHALL preserve ordinary food selection when added sharing is unavailable. Food from an
unsupported holder SHALL not be offered as usable when it cannot be safely retrieved. A bounded
startup warning SHALL explain enabled sharing that could not become available, the reason and what
food access remains. Other compatible mods and unrelated changes SHALL not make sharing unavailable
merely by being present. If sharing becomes unavailable during play, already-obtained food SHALL
remain safe and usable under ordinary rules. Missing optional content SHALL be quiet.

Original Meals On Wheels or Continued co-installation SHALL leave the original in charge and disable
the overlapping owned sharing behavior with an explanation, without changing foreign files, patches
or the user's mod list. Unadapted external bindings SHALL not be advertised as drop-in compatibility.
Normal play SHALL not enable global debug logging or produce per-search log messages.

#### Scenario: Unsupported holder retrieval
- **WHEN** a changed food holder no longer permits supported safe retrieval
- **THEN** its contents are not offered by added sharing, a warning explains that limitation and ordinary food selection continues.

#### Scenario: Compatible food preference mod
- **WHEN** another compatible mod adjusts food scores without preventing safe food use
- **THEN** the owned fallback can still run and preserves a successful ordinary result.

#### Scenario: Original provider present
- **WHEN** either original Meals On Wheels package is active
- **THEN** owned overlapping sharing is inactive, the source remains untouched and the explanation does not claim our corrected behavior is active.

#### Scenario: Later incompatibility
- **WHEN** a later change invalidates an admitted branch after a colonist has already obtained food
- **THEN** new borrowing through that branch stops while the actual obtained portion remains recoverable and normally edible.

#### Scenario: Quiet normal eating
- **WHEN** many ordinary food searches run successfully or find no food
- **THEN** this feature does not enable global debug logging or flood the log with each search.
