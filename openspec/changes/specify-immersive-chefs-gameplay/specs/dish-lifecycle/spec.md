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

#### Scenario: Dishwasher has capacity
- **WHEN** a cleaner searches for work while dirty ware and a reachable eligible dishwasher with free capacity exist
- **THEN** the generated job reserves both and hauls the ware to that dishwasher instead of selecting a hand-washing source

#### Scenario: No dishwasher can accept the ware
- **WHEN** every dishwasher is full, disconnected, unpowered, forbidden, reserved, or unreachable
- **THEN** the cleaner may generate a hand-washing job at the highest-priority valid water source

#### Scenario: Cleaning work is disabled
- **WHEN** a pawn has the Cleaning work type disabled
- **THEN** the pawn is not assigned a `Doing dishes` job by normal work selection

### Requirement: Connected water-source priority
Hand-washing jobs SHALL choose reachable, allowed water sources in this order: an operational Dubs Bad Hygiene kitchen sink when that integration is active; another recognized connected water fixture such as a sink, water bowl, or well; and a safe standable water-terrain cell as the configurable final fallback when `AllowTerrainHandwashing` is enabled. A powered appliance SHALL be eligible only while powered, and an appliance or fixture whose integration exposes a water network SHALL be eligible only while connected to an operational supplied network.

#### Scenario: Plumbed kitchen sink is available
- **WHEN** Dubs Bad Hygiene is active and a reachable powered, plumbed, supplied kitchen sink is available alongside lower-priority water sources
- **THEN** the hand-washing job selects the kitchen sink

#### Scenario: A fixture is disconnected
- **WHEN** a sink or dishwasher requires power or a water network and that required connection is absent
- **THEN** the appliance is excluded from washing destinations until its connection is restored

#### Scenario: Terrain water is the only source
- **WHEN** no eligible water fixture exists, terrain handwashing is enabled, and a reachable safe water-terrain cell exists
- **THEN** the cleaner may wash dishes at that terrain cell

#### Scenario: Terrain fallback is disabled
- **WHEN** no eligible fixture exists and terrain handwashing is disabled
- **THEN** no hand-washing job is issued solely from a water-terrain cell

### Requirement: Wild-water washing is usable but not sanitary-equivalent

A completed wash at a recognized supplied fixture or dishwasher SHALL mark ware clean with safe-wash provenance. A completed wash at water terrain SHALL mark it clean with wild-water provenance. Caravan travel washing SHALL produce the same wild-water provenance. The provenance SHALL remain until a later completed wash replaces it and SHALL be included at the next applicable cooking or dining poisoning calculation even though the ware is selectable as clean.

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

#### Scenario: Processor Framework is unavailable or incompatible
- **WHEN** Processor Framework is absent, disabled in settings, or fails its expected-shape guard
- **THEN** the local identity-preserving cycle supplies the same capacity, interruption, sanitation, and output behavior without a missing dependency

### Requirement: Kitchen appliance research is explicit
Immersive Chefs SHALL add `ImmersiveChefs_Dishwashing` with vanilla `Electricity` as its prerequisite and the domestic dishwasher as its unlock. It SHALL add `ImmersiveChefs_ProfessionalKitchens` with `ImmersiveChefs_Dishwashing` and vanilla `Machining` as prerequisites; it SHALL unlock the industrial dishwasher plus the prep, sauce, meat, vegetable, and pastry stations. The microwave SHALL require vanilla `Electricity` directly. Ordinary plates, cutlery, cookware, and chef's knives SHALL have no Immersive Chefs research prerequisite and SHALL instead use their specified workstation gates.

#### Scenario: Electricity unlocks domestic sanitation progression
- **WHEN** a colony completes vanilla `Electricity` but not `ImmersiveChefs_Dishwashing`
- **THEN** it can build a microwave but cannot yet build either dishwasher
- **THEN** completing `ImmersiveChefs_Dishwashing` unlocks the domestic dishwasher

#### Scenario: Professional Kitchens unlocks the complete station set
- **WHEN** `ImmersiveChefs_Dishwashing` and vanilla `Machining` are complete and the colony finishes `ImmersiveChefs_ProfessionalKitchens`
- **THEN** the industrial dishwasher and all five prep/support stations become buildable together

### Requirement: Kitchenware shortages produce bounded actionable alerts
The map SHALL expose two aggregated player alerts rather than per-pawn or per-tick messages. `Missing kitchenware` SHALL appear when an active non-excluded bill, plating workflow, paste request, or ordinary dining workflow cannot reserve required clean cookware, plates, or cutlery under the configured ware rules. Its explanation SHALL group affected targets and distinguish genuinely absent, dirty-only, forbidden, unreachable, and reserved supply. `Dirty tableware backlog` SHALL appear after a stable one-hour in-game grace period when dirty cookware, plates, or cutlery is blocking normal workflows or has no viable washing path; ware inside an eligible accepting or actively progressing dishwasher SHALL not by itself trigger the backlog.

Both alerts SHALL aggregate by map and cause, update without repeated letters/messages, support click-through or target cycling to the affected bill giver, diner/service target, dirty stack, or blocked washing appliance, and clear automatically when eligible clean supply or a viable cleaning path recovers. They SHALL be suppressed for excluded handheld/travel foods, animal feeding, deliberate emergency-unplated results, and every workflow while `WareRequirementMode=Off`. A dishwasher paused for missing power, supplied water, or repair is a blocked path and MAY trigger the backlog after the grace period.

#### Scenario: A cooking bill has no clean plate supply
- **WHEN** a covered active bill in Strict mode can reach no eligible clean plates
- **THEN** `Missing kitchenware` identifies that bill and reports whether plates are absent, dirty-only, forbidden, unreachable, or reserved

#### Scenario: Dirty dishes have no washing path
- **WHEN** dirty tableware remains for one in-game hour and every dishwasher and hand-washing source is ineligible
- **THEN** `Dirty tableware backlog` appears, drills down to the dirty ware or blocked source, and does not emit repeated message spam

#### Scenario: The shortage is resolved
- **WHEN** eligible clean ware becomes reservable or a viable washing path begins accepting the backlog
- **THEN** the corresponding alert clears automatically

#### Scenario: Tableware simulation is disabled
- **WHEN** `WareRequirementMode=Off` or the only relevant food is explicitly excluded
- **THEN** neither kitchenware alert is raised for that workflow

### Requirement: Gastronomy service clearing
When Gastronomy integration is active, waiters and servers SHALL collect the required cutlery with a meal and SHALL claim and haul the resulting dirty plate and cutlery promptly after the dining job finishes. Reservations and service claims SHALL be released when dining or clearing is cancelled so ordinary cleaners can recover the ware.

#### Scenario: Waiter completes table service
- **WHEN** a waiter serves a plated meal with cutlery and the guest finishes eating
- **THEN** a Gastronomy clearing job claims the dirty plate and cutlery before ordinary low-priority hauling

#### Scenario: Waiter cannot clear the table
- **WHEN** a waiter-held clearing job is cancelled or becomes unreachable
- **THEN** its ware reservations are released and the `Doing dishes` workflow can collect the dirty items

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
