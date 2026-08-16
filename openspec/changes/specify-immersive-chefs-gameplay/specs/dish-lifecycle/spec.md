## ADDED Requirements

**Owning mod:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Persistent sanitation state
Every reusable cookware set, plate, and cutlery set SHALL carry a serialized sanitation state of clean or dirty plus the provenance of its last completed wash: safe fixture/appliance, wild water, or none. The state and provenance SHALL survive saving, loading, hauling, storage, caravan transfer, and placement inside an item container without being reset. Chef's knives SHALL use only their intrinsic material-cleanliness stat and SHALL not receive this mutable sanitation component in the planned scope.

#### Scenario: Dirty ware survives a save cycle
- **WHEN** a dirty plate, cookware set, or cutlery set is saved and the game is loaded again
- **THEN** the restored item remains dirty with the same Stuff, craftsmanship quality, and stack count

#### Scenario: Sanitation state constrains stacking
- **WHEN** otherwise compatible clean and dirty ware occupy the same storage cell
- **THEN** they remain separate stacks, while items with matching sanitation, Stuff, and quality may stack normally

#### Scenario: Wash provenance constrains stacking
- **WHEN** otherwise compatible safely washed and wild-water-washed clean ware occupy the same storage cell
- **THEN** they remain separate stacks so the wild-water poisoning provenance cannot be erased by merging

### Requirement: Lossless service-ware metadata
A plated meal stack SHALL serialize one plate binding per serving, including the plate Def, Stuff, craftsmanship quality, remaining hit points, sanitation state, and wash provenance. Splitting or partially consuming a stack SHALL transfer the corresponding number of bindings, and merging SHALL occur only when the bindings can be combined without erasing material, quality, durability, sanitation, or wash-provenance distinctions.

#### Scenario: Split a plated meal stack
- **WHEN** three servings with three bound plates are split into stacks of one and two servings
- **THEN** the resulting stacks contain one and two plate bindings respectively and no plate is duplicated or lost

#### Scenario: Reject a lossy meal merge
- **WHEN** two otherwise identical meal stacks are plated with different Stuff or craftsmanship quality
- **THEN** the stacks do not merge in a way that discards either plate identity

### Requirement: Clean ware preference and emergency fallback
Ware-selection and reservation logic SHALL always prefer available clean ware. `DirtyWareFallback=Never` SHALL exclude dirty ware; `Urgent only` SHALL admit it only for a consumer at or below the configured urgent-hunger threshold or an explicitly identified urgent request; and `Always` SHALL admit it whenever no clean eligible equivalent exists. Cooking and plating SHALL additionally obey the `WareRequirementMode x DirtyWareFallback x emergency` precedence matrix in the meal-production contract. Serving and eating SHALL not block ingestion solely because no clean or permitted dirty service ware exists; they SHALL instead record the missing service ware for the dining-standard consequence. Selecting dirty ware SHALL retain its dirty state for downstream poisoning and thought calculations.

#### Scenario: Clean ware is available
- **WHEN** a pawn can reserve both a clean and a dirty suitable plate or cookware set
- **THEN** the pawn reserves the clean item and leaves the dirty item unused

#### Scenario: Hunger is urgent and only dirty ware exists
- **WHEN** no clean suitable ware can be reserved, the pawn is below the urgent-hunger threshold, and the fallback mode is `Urgent only`
- **THEN** the pawn may reserve dirty ware and the resulting meal or dining event records that dirty ware was used

#### Scenario: Dirty fallback is disabled
- **WHEN** only dirty ware exists and the configured fallback mode is disabled
- **THEN** normal cooking, serving, and eating jobs do not reserve that ware

#### Scenario: Dirty fallback is always enabled
- **WHEN** no clean suitable ware can be reserved and the fallback mode is `Always`
- **THEN** an ordinary job may reserve dirty ware before considering the configured missing-ware or blocking behavior

### Requirement: Cookware becomes dirty only through actual cooking
Once a cook performs at least one active cooking work tick for a meal, the reserved cookware SHALL be marked dirty when that cooking job completes or is interrupted and SHALL be dropped at the cooking station for collection. Merely reserving or hauling cookware before any cooking work tick SHALL NOT dirty it. Fixed glitterworld cookware is the sole exception and SHALL self-clean when released after active cooking without creating a washing job.

#### Scenario: Cooking completes
- **WHEN** a cook performs cooking work and finishes the meal
- **THEN** the cookware is no longer reserved, is dirty, and is placed at or beside the cooking station

#### Scenario: Hauling is interrupted before cooking
- **WHEN** a cook reserves and hauls clean cookware but the job ends before its first cooking work tick
- **THEN** the cookware remains clean and its reservation is released

#### Scenario: Active cooking is interrupted
- **WHEN** a cook has performed cooking work and the job is then interrupted
- **THEN** the cookware is released as dirty even though no meal was produced

#### Scenario: Self-cleaning cookware is released
- **WHEN** glitterworld cookware is released after at least one active cooking work tick
- **THEN** the same cookware remains clean and produces no dishwashing work

### Requirement: Plates are conserved through dining, expiry, and terminal destruction
One exact bound plate per serving SHALL be released as dirty when that serving is ingested or ceases to be an edible meal through rotting or expiration. Expiry/rot SHALL always recover the plate regardless of its flammability. On any other terminal meal destruction, each remaining plate binding SHALL be resolved independently. If the terminal `DamageInfo` is fire damage and that plate's effective `Flammability` stat resolved from its recorded Def and Stuff is greater than zero, that plate SHALL be destroyed; otherwise the exact plate SHALL survive and be released dirty. The implementation MUST NOT hard-code material names or assume that every metal is nonflammable.

For a spawned meal, a surviving plate SHALL appear at or adjacent to the meal's final position. For a meal in a holder, recovery SHALL first return the plate to that holder when it can accept the item and otherwise place it at the nearest valid map cell under normal holder-ejection rules. No recovery path may create a replacement plate after its binding has already been released or fire-destroyed. A cutlery set used for an eating job SHALL be released as dirty when the job ends after eating has begun, while cutlery reserved for an eating job that never begins SHALL retain its prior sanitation state.

#### Scenario: Pawn finishes a plated meal
- **WHEN** a pawn ingests one serving and used cutlery
- **THEN** exactly one bound plate and one cutlery set are released as dirty near the diner or to the responsible service job

#### Scenario: Plated meals rot in storage
- **WHEN** a stack of plated meals becomes rotten or expires
- **THEN** one dirty plate per remaining serving is recovered and no plate binding remains on the spoiled output

#### Scenario: A plated meal is destroyed without fire
- **WHEN** non-fire damage terminally destroys a plated meal
- **THEN** each exact bound plate survives with its recorded Def, Stuff, quality, and remaining hit points and is released dirty

#### Scenario: Fire destroys a flammable bound plate
- **WHEN** fire damage terminally destroys a plated meal whose bound plate has effective `Flammability` greater than zero
- **THEN** that plate binding is consumed without spawning a replacement plate

#### Scenario: A nonflammable plate survives meal fire
- **WHEN** fire damage terminally destroys a plated meal whose bound plate has effective `Flammability` zero
- **THEN** the exact plate survives and is released dirty at the recoverable holder or map position

#### Scenario: Eating is cancelled before ingestion starts
- **WHEN** a pawn reserved clean cutlery but cancels the eating job before beginning to eat
- **THEN** the cutlery reservation is released and the cutlery remains clean

### Requirement: Doing dishes is Cleaning work
Immersive Chefs SHALL add a `Doing dishes` work giver governed by the vanilla Cleaning work type. Eligible cleaners SHALL reserve dirty ware and its destination atomically. With `PreferDishwashers` enabled, they SHALL prefer hauling it to an available dishwasher and use hand washing only when no eligible dishwasher has load capacity or can be reached. With that setting disabled, the work giver MAY select either an eligible dishwasher or valid hand-washing source through ordinary priority, reachability, and reservation rules. Ordinary hauling logic MAY deliver dirty ware to dishwasher input storage, but SHALL NOT divert ware away from an already selected eligible dishwasher merely to enable hand washing.

Hand-washing duration SHALL scale with the physical abstraction represented by the exact item instead of using one duration for every product. At the default work scale, one plate SHALL take 250 ticks, one cutlery setting 125 ticks, and a cookware set 1,000 ticks. The configured hand-washing work scale SHALL multiply those baselines once. A stack split for one job SHALL use only the admitted physical unit's duration.

#### Scenario: Dishwasher has capacity
- **WHEN** a cleaner searches for work while dirty ware and a reachable eligible dishwasher with free capacity exist
- **THEN** the generated job reserves both and hauls the ware to that dishwasher instead of selecting a hand-washing source

#### Scenario: No dishwasher can accept the ware
- **WHEN** every dishwasher is full, disconnected, unpowered, forbidden, reserved, or unreachable
- **THEN** the cleaner may generate a hand-washing job at the highest-priority valid water source

#### Scenario: Cleaning work is disabled
- **WHEN** a pawn has the Cleaning work type disabled
- **THEN** the pawn is not assigned a `Doing dishes` job by normal work selection

#### Scenario: Compare a plate with a cookware set
- **WHEN** otherwise equal pawns hand-wash one plate and one cookware set at the same source and default settings
- **THEN** the plate completes after 250 work ticks while the complete pot/pan/lid cookware abstraction takes 1,000 work ticks

### Requirement: Optional nearby dish batching preserves individual work

When the validated Pick Up And Haul integration is active, an ordinary `Doing dishes` job SHALL use its tracked pawn-inventory system to collect a bounded nearby batch for either one exact hand-washing source or one exact dishwasher. Batch admission MUST preserve the ordinary dirty-ware, forbiddance, reachability, reservation, destination, appliance-capacity, and pawn-encumbrance rules.

For hand washing, the pawn SHALL travel to the selected source once, wash each admitted physical unit separately, and return the resulting clean batch together through Pick Up And Haul's native unload workflow. The job MUST NOT multiply one representative item's duration over mixed products or mark the whole batch clean atomically: every plate, cutlery setting, and cookware set SHALL consume its own configured work and source use before its sanitation transition commits.

For a dishwasher, the pawn SHALL travel to that appliance once and admit each tracked physical unit separately through the appliance owner's validated admission seam. Admission SHALL preserve each exact Thing identity, SHALL stop before the appliance's remaining capacity is exceeded, and SHALL remove an admitted unit from Pick Up And Haul tracking because the dishwasher now owns its lifecycle. Interruption before admission SHALL return the still-carried dirty units through Pick Up And Haul's native unload workflow; interruption after admission SHALL leave those exact units owned by the dishwasher and governed by its ordinary pause, completion, ejection, and hauling behavior. The completed appliance output SHALL remain ordinary clean haulable ware, allowing Pick Up And Haul's normal hauling work to collect nearby outputs together for clean storage without a second Immersive Chefs unloading system.

#### Scenario: Mixed batch retains per-item washing time

- **WHEN** one plate, one cutlery setting, and one cookware set form a Pick Up And Haul hand-washing batch at default work scale
- **THEN** the pawn completes separate 250-, 125-, and 1,000-tick wash phases at the selected source
- **THEN** each exact unit becomes clean only after its own phase and the tracked batch is subsequently unloaded through Pick Up And Haul

#### Scenario: One trip fills a dishwasher with a nearby dirty batch

- **WHEN** a cleaner with Pick Up And Haul active finds nearby dirty plates, cutlery, and cookware that all fit one reachable accepting dishwasher and the pawn's remaining carrying capacity
- **THEN** one ordinary `Doing dishes` job collects the bounded exact batch, travels to that dishwasher once, and admits each physical unit separately without exceeding its remaining capacity
- **AND** the appliance, rather than the pawn inventory, owns every admitted exact unit through its normal cycle

#### Scenario: Dishwasher admission is interrupted partway through a batch

- **WHEN** the pawn is interrupted after some collected units enter the selected dishwasher but before all collected units are admitted
- **THEN** admitted units remain in that exact dishwasher while every unadmitted dirty unit returns through Pick Up And Haul's native unload workflow
- **AND** no unit remains simultaneously tracked by Pick Up And Haul and owned by the appliance

### Requirement: A blocked cook may clean required cookware
When a covered cooking bill would otherwise be runnable but no permitted clean cookware set exists, the bill-owning Cooking work path SHALL look for an eligible dirty cookware set before reporting a permanent wait. If the cook can reserve that exact set and an eligible dishwasher or hand-washing source, the cook SHALL perform the ordinary identity-preserving washing job and then reconsider the original bill. This prerequisite wash belongs to the cook's attempt to satisfy the bill and SHALL be available even when that pawn has ordinary Cleaning work disabled; it MUST still honor forbiddance, reachability, reservations, source operation, water consumption, and dishwasher capacity. It SHALL not fabricate cookware, clean it instantly, or bypass the normal washing toils.

#### Scenario: A bill has only dirty cookware available
- **WHEN** a pawn eligible for Cooking but disabled for ordinary Cleaning scans a runnable covered bill and can reach one dirty cookware set plus a valid water source
- **THEN** the pawn washes that exact set through the normal washing job and may then start the bill with the same clean set

#### Scenario: The dirty cookware cannot be washed
- **WHEN** the only dirty cookware is forbidden, reserved, unreachable, or has no valid washing destination
- **THEN** the cooking scan reports the exact missing-clean-cookware reason and does not mutate or teleport the item

### Requirement: Connected water-source priority
Hand-washing jobs SHALL choose reachable, allowed water sources by validated capability rather than by Def name, translated label, package ownership, or a whitelist of known appliance Defs. When Dubs Bad Hygiene is active, every Thing supplied by Dubs or another mod that implements the validated native drinkable-fixture capability SHALL be considered through that capability's fixture kind, pawn permission, working report, current water supply, power, fuel, flick, breakdown, reachability, and reservation rules. A non-Dubs provider MAY expose the same behavior through a narrow registered source-capability adapter or product DefModExtension; absence or shape failure SHALL exclude only that provider rather than guessing from its name.

Eligible sources SHALL be ordered as supplied connected drinkable fixtures, self-contained or hauled-water drinkable fixtures, wells, and finally a safe standable water-terrain cell when `AllowTerrainHandwashing` is enabled. Equal-capability sources SHALL use ordinary distance and reservation ordering. A powered appliance SHALL be eligible only while powered, and an appliance or fixture whose integration exposes a water network SHALL be eligible only while connected to an operational supplied network.

#### Scenario: A third-party Dubs basin is available
- **WHEN** Dubs Bad Hygiene is active and a reachable, allowed, operational and supplied basin from another package implements Dubs' validated drinkable-fixture capability alongside lower-priority water sources
- **THEN** the hand-washing job selects that basin without its Def name or package ID appearing in an Immersive Chefs source whitelist

#### Scenario: A named sink has no water capability
- **WHEN** an unrelated building's Def name or translated label contains `sink` but no validated drinkable-water capability claims it
- **THEN** Immersive Chefs does not select it for dishwashing solely from that text

#### Scenario: A fixture is disconnected
- **WHEN** a sink or dishwasher requires power or a water network and that required connection is absent
- **THEN** the appliance is excluded from washing destinations until its connection is restored

#### Scenario: Terrain water is the only source
- **WHEN** no eligible water fixture exists, terrain handwashing is enabled, and a reachable safe water-terrain cell exists
- **THEN** the cleaner may wash dishes at that terrain cell

#### Scenario: Terrain fallback is disabled
- **WHEN** no eligible fixture exists and terrain handwashing is disabled
- **THEN** no hand-washing job is issued solely from a water-terrain cell

### Requirement: Visually integrated prep sink is a real supplied fixture

The base and optional-variation art for `ImmersiveChefs_PrepStation` visibly contains a sink. When Dubs Bad Hygiene is active and its validated 1.6 shape is available, the finalized prep-station Def SHALL therefore receive the supported pipe capability, participate in Dubs' ordinary human drinking-source search, and be an eligible safe dishwashing source. It SHALL require an operational supplied network for ingredient-preparation bills, drinking, and dishwashing; a merely connected empty network SHALL not count as supplied. Drinking SHALL follow Dubs' native thirst, reachability, reservation, water debit, contamination and job lifecycle rather than directly changing the pawn's need.

The prep station SHALL remain an Immersive Chefs worktable and SHALL not acquire a compile-time Dubs reference. Optional shape absence, an `Off` integration setting, or a changed upstream seam SHALL leave its base worktable behavior loadable, disable only the water/drinking bridge with one bounded warning when applicable, and SHALL not fabricate an unplumbed sink. No other Immersive Chefs workstation SHALL gain a water requirement unless its approved art also visibly contains a sink or a later explicit gameplay contract assigns that capability.

#### Scenario: Supplied prep station supports all three uses
- **WHEN** Dubs Bad Hygiene is active, the prep station is connected to an operational supplied network, and an eligible colonist can reach it
- **THEN** its preparation bill may run, Dubs may select it through the ordinary drink-water workflow, and `Doing dishes` may select it as a safe hand-washing source

#### Scenario: Prep station has no supplied water
- **WHEN** its pipe is disconnected or its connected network has no usable water
- **THEN** preparation bills report the unavailable water requirement, Dubs does not select it as a usable drink source, and no dishwashing job selects it

### Requirement: Wild-water washing is usable but not sanitary-equivalent

A completed wash at a recognized supplied fixture or dishwasher SHALL mark ware clean with safe-wash provenance. A completed wash at water terrain SHALL mark it clean with wild-water provenance. Caravan travel washing SHALL produce the same wild-water provenance. The provenance SHALL remain until a later completed wash replaces it and SHALL be included at the next applicable cooking or dining poisoning calculation even though the ware is selectable as clean.

Wash provenance is an internal risk input, not ordinary player knowledge. Inspect panes, labels, stack labels, and trade labels MUST NOT append `wild-water washed` or an equivalent hidden-provenance disclosure. Developer diagnostics and test-only state MAY still expose it.

#### Scenario: Terrain-washed plate is reused
- **WHEN** a dirty plate is washed at a water-terrain fallback and later used for an eligible meal
- **THEN** it is selectable as clean and its wild-water provenance contributes the specified plate risk to ingestion

#### Scenario: Dishwasher removes wild-water provenance
- **WHEN** wild-water-washed ware completes a powered dishwasher cycle
- **THEN** the same item remains clean and its wash provenance becomes safe

### Requirement: Players can separate clean and dirty ware in storage

Immersive Chefs SHALL add mutually exclusive `Clean kitchenware` and `Dirty kitchenware` special storage filters covering reusable cookware, plates, and cutlery. Existing stockpiles SHALL continue to accept both states when neither filter is deliberately excluded. Normal use, plate recovery, and interrupted jobs SHALL NOT automatically forbid dirty ware; player forbiddance SHALL remain authoritative for selection, hauling, and cleaning. When sanitation or wash provenance changes, the item SHALL notify normal storage logic so it can be hauled from a dirty-only stockpile to an eligible clean stockpile or vice versa without changing ownership.

#### Scenario: Player creates a dirty-dish stockpile
- **WHEN** a stockpile allows `Dirty kitchenware` and excludes `Clean kitchenware`
- **THEN** dirty reusable ware is eligible for that stockpile and safely or wild-water-washed clean ware is not

#### Scenario: A stored plate becomes clean
- **WHEN** a dirty plate from a dirty-only stockpile completes washing
- **THEN** the same plate becomes ineligible for that stockpile and can be hauled to an allowed clean storage destination

#### Scenario: Dirty ware is forbidden by the player
- **WHEN** the player forbids a dirty item
- **THEN** ordinary hauling and `Doing dishes` respect the forbiddance until the player allows it

### Requirement: Identity-preserving dishwashers
Immersive Chefs SHALL provide a dishwasher with a base capacity of 16 plate-equivalents and an industrial dishwasher with a base capacity of 64 plate-equivalents before applying `DishwasherCapacityScale`. A wash cycle SHALL preserve each input item's Def, Stuff, craftsmanship quality, hit points, stack count, and other components while changing only sanitation-related state, and clean output SHALL be available for hauling when the cycle completes.

For capacity accounting, the default load SHALL count a plate as 1 plate-equivalent, a cookware set as 4, a cutlery set as 0.25, and any future washable item by a Def-configurable value. The first admitted item SHALL open a Def-configured, save-persistent loading phase during which more dirty ware can join while capacity remains; every successful admission SHALL reset that phase, cleaning progress SHALL remain zero, and the inspector SHALL identify the loading state. When the loading phase closes, the cycle SHALL capture its exact input identities, work duration, load-scaled resource demand, and progress. With the validated Dubs adapter active, cycle start SHALL atomically verify and debit exactly one positive Def-configured water charge scaled to the final captured load. Insufficient supplied water SHALL retain the admitted dirty batch without starting the cycle and SHALL expose that reason. A paused/resumed cycle MUST NOT debit water again, and explicit cancellation, deconstruction, or terminal destruction SHALL NOT refund the already admitted charge.

Temporary loss of power, supplied water, or operability through breakdown SHALL pause captured cycle state without cleaning or ejecting items; restoration or repair SHALL automatically resume it. Explicit cancellation, deconstruction, or terminal destruction SHALL end the cycle and eject every recoverable original input dirty under normal holder rules. When the Dubs package is active in `Auto` mode but its required plumbing shape fails validation, both dishwashers SHALL be ineligible rather than silently washing without water; one actionable integration warning SHALL remain, and recognized non-Dubs hand-washing fallbacks SHALL still be eligible.

#### Scenario: Standard dishwasher reaches capacity
- **WHEN** `DishwasherCapacityScale` is `1.0` and a standard dishwasher contains ware totaling 16 plate-equivalents
- **THEN** it accepts no additional load until enough capacity is freed

#### Scenario: One place setting forms one batch
- **WHEN** a dirty plate enters an idle dishwasher and its matching dirty cutlery arrives during the loading phase
- **THEN** both exact items are admitted before cleaning progress or the Dubs water debit begins
- **THEN** the final cycle load and any water charge include both items

#### Scenario: Industrial cycle completes
- **WHEN** `DishwasherCapacityScale` is `1.0` and an industrial dishwasher completes a cycle containing mixed-Stuff, mixed-quality ware within its 64 plate-equivalent capacity
- **THEN** it exposes corresponding clean items with every non-sanitation property and count preserved

#### Scenario: Power is lost during a cycle
- **WHEN** a dishwasher loses power after making progress
- **THEN** the cycle pauses without cleaning or ejecting any item and without changing its captured progress or contents
- **THEN** restoring power automatically resumes that same cycle from the captured progress

#### Scenario: Dubs water is lost during a cycle
- **WHEN** Dubs Bad Hygiene is active and the dishwasher's supplied plumbing becomes unavailable after making progress
- **THEN** the cycle pauses without consuming completion output or losing progress
- **THEN** restoring a supplied connection automatically resumes the same cycle without a second water debit

#### Scenario: Dubs water is insufficient when the loading batch closes
- **WHEN** the validated Dubs adapter cannot atomically debit the cycle's captured load-scaled water charge
- **THEN** the dishwasher does not start, retains the dirty inputs for retry or hauling, and exposes insufficient supplied water as the reason

#### Scenario: A cycle is explicitly cancelled or the building is removed
- **WHEN** a player cancels a cycle or deconstructs or terminally destroys the dishwasher before completion
- **THEN** every recoverable original input is ejected with its pre-cycle dirty state and no clean replacement is created
- **THEN** an already debited Dubs water charge is not refunded

#### Scenario: Active Dubs integration changes shape
- **WHEN** Dubs Bad Hygiene is active in `Auto` mode but the expected plumbing member or Def shape is absent
- **THEN** dishwashers fail closed with one actionable warning while eligible hand-washing fallbacks remain available

### Requirement: Safe Processor Framework use
When the supported Processor Framework is active and its expected shape validates, Immersive Chefs SHALL drive dishwasher timing, progress, and presentation through an identity-preserving adapter over that framework. The adapter SHALL intercept its locally verified destructive fixed-output completion path and return the captured original input instances with all non-sanitation state intact. It SHALL use Processor Framework's normal pause/resume lifecycle for temporary power loss and SHALL add the equivalent supplied-water gate when Dubs Bad Hygiene is active. Processor Framework remains optional: only when it is absent, disabled, or shape-incompatible SHALL the dishwasher use the behaviorally equivalent Immersive Chefs local cycle.

#### Scenario: Stock processor cannot preserve identity
- **WHEN** the supported Processor Framework is active and its stock completion would replace a Stuff-made dirty item with a fixed Stuff-less clean output
- **THEN** the adapter suppresses that replacement and returns the original clean item with its material, quality, hit points, and components intact

#### Scenario: A queued fill job outlives the dirty state it selected
- **WHEN** Processor Framework queued a dishwasher fill job while a ware item was dirty but that exact item has become clean before the job reserves or carries it
- **THEN** the fill job fails before reserving or hauling the clean item
- **THEN** the clean item remains available for ordinary kitchen storage and cannot be fed back into the dishwasher by that stale job

#### Scenario: Processor Framework is unavailable or incompatible
- **WHEN** Processor Framework is absent, disabled in settings, or fails its expected-shape guard
- **THEN** the local identity-preserving cycle supplies the same capacity, interruption, sanitation, and output behavior without a missing dependency

### Requirement: Kitchen appliance research is explicit
Immersive Chefs SHALL add `ImmersiveChefs_Dishwashing` with vanilla `Electricity` as its prerequisite and the domestic dishwasher as its unlock. It SHALL add `ImmersiveChefs_ProfessionalKitchens` with `ImmersiveChefs_Dishwashing` and vanilla `Machining` as prerequisites; it SHALL unlock the industrial dishwasher plus the prep, sauce, meat, vegetable, and pastry stations. When Thermodynamics - Hot Meals is absent, the Immersive Chefs fallback microwave SHALL require vanilla `Electricity` directly. When exact package `Mlie.DThermodynamicsHotMeals` is active, Immersive Chefs SHALL add no microwave Def, research unlock, or designation and SHALL leave Thermodynamics' own progression untouched. Ordinary plates, cutlery, cookware, and chef's knives SHALL have no Immersive Chefs research prerequisite and SHALL instead use their specified workstation gates.

#### Scenario: Electricity unlocks domestic sanitation progression
- **WHEN** a colony completes vanilla `Electricity` but not `ImmersiveChefs_Dishwashing`
- **THEN** without Thermodynamics it can build the Immersive Chefs fallback microwave but cannot yet build either dishwasher
- **THEN** completing `ImmersiveChefs_Dishwashing` unlocks the domestic dishwasher

#### Scenario: Professional Kitchens unlocks the complete station set
- **WHEN** `ImmersiveChefs_Dishwashing` and vanilla `Machining` are complete and the colony finishes `ImmersiveChefs_ProfessionalKitchens`
- **THEN** the industrial dishwasher and all five prep/support stations become buildable together

### Requirement: Missing kitchenware produces one intent-aware alert
The map SHALL expose one aggregated `Missing kitchenware` alert rather than per-pawn or per-tick messages. The alert SHALL appear only in `WareRequirementMode=Strict` when all of the following are true on a player-home map: a player-owned operational cooking workstation explicitly opts into the alert contract; that workstation has an unsuspended covered-meal bill which is still runnable under its repeat/target-count rules; at least one spawned colonist has Cooking work active and is neither drafted, downed, nor in a mental state; and the map physically contains zero cookware sets or zero plates. The alert SHALL identify the absent product types, aggregate and cycle through the affected cooking workstations, update without letters or message spam, and clear when physical supply appears or the triggering intent stops.

Physical presence, not immediate usability or cleanliness, SHALL suppress this inventory-level alert. Dirty, forbidden, reserved, and temporarily unreachable ware SHALL count as existing, including ware retained in a map-held inventory, meal, or appliance container. A lack of clean supply SHALL remain ordinary bill/job feedback and MUST NOT create a global alert. Dirty-dish accumulation, a missing washing path, a loose or imported meal, ordinary dining, raw food, paste demand, an excluded food, and cutlery demand MUST NOT create this alert. The base fueled and electric stoves SHALL opt in. The base campfire and unknown or modded grills SHALL remain opted out unless a compatibility patch deliberately marks the station as a kitchenware-requiring workstation. Suspended/completed bills, unavailable stations, `Prefer`/`Off` modes, and maps whose potentially capable cooks are all drafted or otherwise ineligible SHALL remain silent.

#### Scenario: A strict stove bill has no kitchenware at all
- **WHEN** an owned fueled or electric stove is operational, has an unsuspended covered bill that should still run, has an eligible active cook, and its map physically contains no cookware or plates
- **THEN** `Missing kitchenware` identifies the absent types and cycles through the affected stove bill giver

#### Scenario: Only dirty forbidden ware exists
- **WHEN** the same map has dirty cookware and plates which are forbidden, reserved, or temporarily unreachable
- **THEN** no missing-kitchenware alert appears because the colony is not physically missing those products
- **THEN** the cooking and cleaning jobs communicate their own immediate blockers without a second global dirty-backlog alert

#### Scenario: The colony is not presently trying to use a proper kitchen
- **WHEN** colonists merely eat berries or loose meals, a campfire or grill has a cooking bill, the proper-stove bill is suspended or complete, its station is unavailable, every active cook is drafted, or ware mode is `Prefer` or `Off`
- **THEN** no missing-kitchenware alert appears

#### Scenario: A compatibility kitchen opts in
- **WHEN** a compatible mod marks one of its proper cooking workstations with the alert-station extension and a covered strict bill meets the normal trigger conditions
- **THEN** that workstation participates without Immersive Chefs classifying every unknown grill as a kitchen

### Requirement: Gastronomy service clearing
When Gastronomy integration is active, waiters and servers SHALL collect the required cutlery with a meal and SHALL retain the exact resulting plate and cutlery identities for clearing after the dining job finishes. If both items remain eligible, the waiter SHALL start one `Doing dishes` job and queue the other exact item, preferring one accepting dishwasher for both; it SHALL NOT substitute unrelated nearby dirty ware. Reservations and service claims SHALL be released when dining or clearing is cancelled so ordinary cleaners can recover the ware.

#### Scenario: Waiter completes table service
- **WHEN** a waiter serves a plated meal with cutlery and the guest finishes eating
- **THEN** Gastronomy clearing claims both exact dirty items before ordinary low-priority hauling and queues both to the same accepting dishwasher when one is available

#### Scenario: Waiter cannot clear the table
- **WHEN** a waiter-held clearing job is cancelled or becomes unreachable
- **THEN** its ware reservations are released and the `Doing dishes` workflow can collect the dirty items

#### Scenario: Pending service ware is not split by ordinary hauling
- **WHEN** a waiter has claimed an exact dirty plate and cutlery setting but has not yet dispatched its clearing jobs
- **THEN** non-forced `HaulGeneral` scans by that waiter or another pawn do not move either claimed item
- **AND** a player-forced haul order remains outside this ordinary-work exclusion

#### Scenario: Hard-unreachable washing releases the setting promptly
- **WHEN** the exact ware remains reachable but every configured dishwasher and hand-washing source is forbidden or unreachable to the waiter
- **THEN** the pending service claim and native reservations are released without waiting for their normal expiry
- **AND** a temporarily full but reachable dishwasher remains a retryable destination rather than cancelling the setting

#### Scenario: Active and queued clearing survive save and load
- **WHEN** the waiter is carrying one exact returned item with the other exact `Doing dishes` job queued and the game is saved and loaded
- **THEN** the same waiter, ware identities, jobs, shared dishwasher target, service claims, and native reservations are restored exactly once

### Requirement: Dishwashing settings are bounded and have explicit application timing
Immersive Chefs SHALL expose `PreferDishwashers` (default `On`), `AllowTerrainHandwashing` (default `On`), `DishwashingWorkScale` (default `1.0`, range `0.25`-`4.0`), and `DishwasherCapacityScale` (default `1.0`, range `0.5`-`4.0`). The two toggles SHALL apply live to newly selected work. `DishwashingWorkScale` SHALL multiply the base work required by hand-washing jobs and new appliance cycles, SHALL apply live when a new job or cycle starts, and SHALL not recalculate progress or duration already captured by an active job or cycle. `DishwasherCapacityScale` SHALL multiply the base 16/64 plate-equivalent capacities, SHALL be labeled restart-required because it changes appliance component properties, and SHALL take effect only after Def databases are rebuilt.

#### Scenario: Dishwasher preference is disabled live
- **WHEN** `PreferDishwashers` changes from `On` to `Off` while both a dishwasher and hand-washing source are eligible
- **THEN** the next work search may choose either destination through ordinary priority and pathing without changing an already reserved washing job

#### Scenario: Terrain fallback is enabled live
- **WHEN** `AllowTerrainHandwashing` changes from `Off` to `On` and safe reachable water terrain is the only source
- **THEN** the next work search may issue a terrain hand-washing job without a restart

#### Scenario: Work scale changes during an active cycle
- **WHEN** `DishwashingWorkScale` changes from `1.0` to `2.0` while one wash cycle is active
- **THEN** the active cycle retains its captured duration and the next cycle requires twice its base work

#### Scenario: Capacity scale waits for restart
- **WHEN** the player saves `DishwasherCapacityScale` as `2.0` during a running game
- **THEN** the settings UI reports that a restart is required and current capacities remain unchanged
- **THEN** after restart the domestic and industrial capacities are `32` and `128` plate-equivalents

### Requirement: Dishwasher utility state is real and player-readable
An Immersive Chefs dishwasher SHALL advance a captured washing cycle only while its native power trader is actually powered for the appliance's active draw. Merely being connected to a power net SHALL NOT count as sufficient power. If the active load exceeds current generation and stored-energy delivery, cycle progress SHALL remain unchanged until the network can supply it, including after a charged battery or additional generation becomes available. The same captured batch SHALL resume without a second water debit.

When Dubs Bad Hygiene is active, a connected pipe with an empty or otherwise unusable upstream water supply SHALL NOT count as supplied water. Loading MAY retain the batch, but washing SHALL not begin or advance until the exact load-scaled water charge is available and the supported Dubs fixture/network reports operational supply. The dishwasher's ordinary inspect pane SHALL explain actionable states such as `Not enough power` and `No supplied water` in player-facing language. It SHALL NOT expose raw pipe-network, sewage-network, grid, object, reflection, or implementation identifiers. Optional integration components attached to an Immersive Chefs dishwasher SHALL contribute no diagnostic-only inspect strings; useful operational state remains owned by the Immersive Chefs status line.

#### Scenario: A connected water tower is empty
- **WHEN** Dubs Bad Hygiene is active, the dishwasher is connected through supported plumbing, but the connected tower/network cannot supply the captured cycle charge
- **THEN** the exact batch remains retained with unchanged washing progress and the inspect pane says that supplied water is unavailable
- **THEN** no pipe-net ID, sewage ID, grid ID, component name, or other diagnostic identifier is visible

#### Scenario: Active draw exceeds the power network
- **WHEN** the dishwasher is connected to a network whose lone generator can cover idle draw but not the appliance's active washing draw
- **THEN** the retained batch does not become clean and cycle progress remains unchanged while the inspect pane reports insufficient power
- **THEN** after adequate generation or a charged battery is added, the same batch resumes rather than restarting or consuming water twice

#### Scenario: The appliance is fully supplied
- **WHEN** both the active native power draw and the exact Dubs water charge are available
- **THEN** the dishwasher advances through its ordinary cycle and ejects the exact clean ware identities
