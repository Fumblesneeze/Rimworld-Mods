## ADDED Requirements

**Mod scope:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Plated meal recipes reserve reusable cookware and one plate per serving
Every covered cooking job SHALL reserve one reachable cookware set and one reachable plate for each meal portion produced, selected as eligible under the ware-precedence matrix below, in addition to the recipe's food ingredients. Clean ware SHALL remain the first choice. The cookware SHALL be a reusable tool and MUST NOT be consumed by the recipe. Each plate SHALL become attached meal metadata rather than an extra loose output. Cutlery SHALL be acquired by the diner or restaurant service workflow and MUST NOT be a cooking ingredient. A chef's knife SHALL be optional wearable equipment and MUST NOT be reserved as a bill ingredient.

Reservations SHALL respect pawn reachability, forbidden state, storage and bill ingredient filters, competing reservations, Stuff and quality restrictions, and the current ware-requirement setting.

#### Scenario: Cook a four-portion meal batch
- **WHEN** a cook starts a covered recipe that produces four portions under Strict ware requirements
- **THEN** the job reserves one eligible clean cookware set, four eligible clean plates, and the food ingredients, consumes the four plates into the four plated portions, and returns the cookware as a reusable dirty item at the cooking station

#### Scenario: Two cooks compete for the same wares
- **WHEN** concurrent bills can see only one cookware set and four plates
- **THEN** the first accepted job owns those reservations and the second job waits or selects different eligible wares without duplicating an item or plate count

### Requirement: Cooking work scans are reservation-neutral
Evaluating a prospective covered cooking job SHALL only verify that suitable ware is currently available. It MUST NOT reserve cookware or plates, open a cooking session, move dirty cookware off a work surface, or create an assistance claim until that exact job has been accepted and its driver starts. A prospective job discarded by RimWorld's work search SHALL therefore leave no mod-owned reservation or session behind.

Once an accepted job starts, Immersive Chefs SHALL reserve its selected ware exactly once. Completion, drafting, downing, interruption, job replacement, map removal, or another native job cleanup path SHALL explicitly release every exact ware reservation owned by that job, including cancellation before active cooking. Saving and loading SHALL NOT retain a reservation whose cooking job is neither current nor queued.

#### Scenario: RimWorld discards a speculative bill job
- **WHEN** `WorkGiver_DoBill.JobOnThing` constructs a covered candidate but the pawn never starts or queues that job
- **THEN** the candidate leaves no cookware or plate reservation, cooking session, work-surface movement, or assistance claim
- **THEN** another pawn can reserve the same eligible ware normally

#### Scenario: A cook is interrupted before active work
- **WHEN** an accepted cooking job has reserved ware and the cook is drafted, downed, put to bed, or otherwise changes jobs before the first cooking work tick
- **THEN** every exact reservation owned by that job is released and the same ware remains clean and available

#### Scenario: Save and load after an interrupted cook
- **WHEN** an interrupted cooking job is no longer current or queued and the game is saved and loaded
- **THEN** no null-job or orphaned cooking-ware reservation is restored for that pawn

### Requirement: Ware requirement modes are explicit and deadlock-safe
The `WareRequirementMode` setting SHALL provide `Strict`, `Prefer`, and `Off`, defaulting to `Strict`.

`DirtyWareFallback` SHALL provide `Never`, `Urgent only`, and `Always`, defaulting to `Urgent only`. A food emergency SHALL be observable when a consuming pawn's food need is at or below the configurable `EmergencyHungerThreshold`, default 15% and bounded from 5% through 35%, or when an urgent production request explicitly identifies such a pawn. Production SHALL use the following precedence matrix for each required cookware set and plate batch; entries are ordered from first choice to last choice:

| Ware requirement | Dirty fallback | Ordinary production | Food-emergency production |
| --- | --- | --- | --- |
| `Strict` | `Never` | clean, otherwise block | clean, otherwise explicit missing ware |
| `Strict` | `Urgent only` | clean, otherwise block | clean, dirty, otherwise explicit missing ware |
| `Strict` | `Always` | clean, dirty, otherwise block | clean, dirty, otherwise explicit missing ware |
| `Prefer` | `Never` | clean, otherwise explicit missing ware | clean, otherwise explicit missing ware |
| `Prefer` | `Urgent only` | clean, otherwise explicit missing ware | clean, dirty, otherwise explicit missing ware |
| `Prefer` | `Always` | clean, dirty, otherwise explicit missing ware | clean, dirty, otherwise explicit missing ware |
| `Off` | any value | do not select ware; produce a ware-exempt result | do not select ware; produce a ware-exempt result |

An explicit missing-ware result SHALL record every unavailable cookware or plate requirement and SHALL be visibly unplated when its plate is missing. A food-emergency result with any missing ware SHALL additionally carry an emergency-fallback tag. `Prefer` therefore avoids blocking but does not silently treat missing ware as present; `Strict` blocks ordinary production only after exhausting the ware states allowed by the selected dirty fallback. `Off` SHALL suppress ware-derived bonuses and penalties for newly produced meals rather than representing its deliberate exemption as dirty or missing ware.

#### Scenario: Ordinary Strict production has no clean plates
- **WHEN** no clean eligible plate can be reserved and no food emergency exists
- **THEN** the covered cooking bill reports the missing clean ware and does not start

#### Scenario: A pawn would otherwise starve
- **WHEN** a valid meal or paste request is for a pawn at or below the emergency hunger threshold and only dirty ware exists
- **THEN** the workflow uses dirty ware, records that state on the meal, and applies the downstream sanitation consequences

#### Scenario: No ware exists during a food emergency
- **WHEN** a pawn at or below the emergency hunger threshold has no reachable clean or dirty ware
- **THEN** the workflow produces or dispenses an emergency-unplated meal that is visibly marked, carries no ware benefits, and remains eligible to prevent starvation

### Requirement: Ware selection prefers cleanliness before secondary stats
The ware selector SHALL always rank clean cookware and plates ahead of dirty equivalents. It SHALL exclude dirty wares unless the precedence matrix permits them for the configured fallback and current emergency state. Among wares in the same allowed sanitation state, it SHALL apply player restrictions, material cleanliness, crafting quality, speed, comfort, culinary value, path cost, and stack availability as deterministic secondary considerations.

When the configured policy would ordinarily reject dirty cookware, a selected eligible cook SHALL receive a native right-click option on the covered bill giver to `Force cook with dirty cookware` if all food ingredients and plates are available and a reachable dirty cookware set is the only cookware blocker. Choosing it SHALL enqueue the ordinary `DoBill` job with a one-job dirty-cookware override; it SHALL not change the global fallback setting, pre-clean the set, suppress sanitation recording, or hide the resulting poisoning contribution. The option SHALL be absent when clean cookware exists, no eligible dirty set exists, the bill is not otherwise runnable, or the pawn cannot perform the bill.

The override SHALL retain the exact dirty cookware Thing selected by that candidate as serializable job-local intent without reserving it during the work scan. If that ordered job waits in a queue or is saved and loaded before starting, its accepted driver SHALL attempt that exact Thing even if a clean set later appears; it SHALL fail cleanly only if the chosen Thing itself is no longer eligible or available.

#### Scenario: Clean low-quality and dirty high-quality cookware are both available
- **WHEN** an ordinary covered cooking job searches for cookware
- **THEN** it reserves the clean cookware even when the dirty cookware has a higher material or crafting quality

#### Scenario: Player explicitly forces dirty cookware
- **WHEN** a selected eligible cook right-clicks a covered bill giver whose only cookware option is one reachable dirty set and chooses `Force cook with dirty cookware`
- **THEN** the normal bill job reserves and uses that exact set, records its dirty-cookware contamination on the meal, and leaves the global ware policy unchanged

### Requirement: Active cooking visibly uses the reserved cookware
Once the lead pawn begins the recipe's active cooking toil, the exact reserved cookware set SHALL render as a carried/placed work prop centered between the pawn and bill giver, comparable to RimWorld's visible ingredient handling. Its draw depth SHALL follow that work direction instead of using one unconditional foreground altitude: when the bill giver is north of the pawn and the pawn works south-to-north, the cookware SHALL render behind the pawn; when the bill giver is south and the pawn works north-to-south, it SHALL render in front. East/west work directions SHALL use neutral pawn-relative depth without a forced foreground bias. It SHALL remain one physical Thing held by the cooking session, preserve its Stuff tint and selected graphic, and disappear from the work prop when active cooking ends. It MUST NOT render during ingredient hauling alone, duplicate on the map, obscure the pawn contrary to that directional depth, or manufacture a cosmetic substitute.

#### Scenario: Watch a cook work at a stove
- **WHEN** the pawn reaches the active cooking toil with a reserved cookware set
- **THEN** that exact set is visibly centered between pawn and work surface with its material treatment
- **THEN** it depth-sorts behind a pawn working toward a north-side surface and in front of a pawn working toward a south-side surface, while east/west work has no forced foreground offset
- **THEN** completion or interruption removes the work prop and releases the same physical set under the sanitation lifecycle

### Requirement: Used cookware remains on its cooking station

After active work begins, completion or interruption SHALL return the exact reserved cookware dirty to an occupied surface cell of the bill giver rather than placing it arbitrarily nearby. The physical Thing, Stuff, quality, hit points, sanitation, forbiddance and reservation semantics SHALL be preserved. When a later covered cooking attempt is admitted at that bill giver, each unheld dirty cookware set still occupying its surface SHALL be moved intact to a valid nearby non-station cell before the new active cooking toil begins, making room for the current work without deleting, duplicating, merging, or cosmetically replacing the previous set. A job cancelled before active work SHALL retain its existing clean recovery behavior and SHALL NOT create dirty stove clutter.

#### Scenario: Cook finishes at a stove
- **WHEN** a covered cooking job performed active work with one exact reserved cookware set
- **THEN** that same set is dirty and remains visibly on an occupied stove cell after the job ends

#### Scenario: The next cook needs the occupied work surface
- **WHEN** a later covered cooking attempt is admitted while one or more dirty cookware sets remain on the bill giver surface
- **THEN** those exact dirty sets are moved to valid nearby non-station cells before the new active work begins
- **THEN** their sanitation, material, quality, hit points, forbidden state and physical unit count remain unchanged

#### Scenario: Cooking stops before work begins
- **WHEN** a reserved cooking job is interrupted during hauling before its active cooking toil starts
- **THEN** its exact cookware remains clean and follows ordinary recovery rather than being presented as used cookware on the stove

### Requirement: Meal complexity sets the minimum plate material
Plate eligibility SHALL use an explicit minimum material tier independently from crafting quality and sanitation. A `Simple` recipe or covered meal SHALL accept any registered plate material, including wood, adobe, and stone. An `Advanced`/Fine recipe or meal SHALL accept metal, registered plastic, or registered ceramic/porcelain and SHALL reject wood, adobe, and stone. An `Elaborate`/Lavish recipe or meal SHALL accept only silver, gold, or registered ceramic/porcelain. An unclassified covered mod recipe or meal SHALL default to the Simple plate tier unless compatibility XML explicitly supplies a minimum plate tier. Clean-first and dirty-fallback rules SHALL operate only within the eligible material set; an emergency MAY produce an explicitly unplated serving but MUST NOT silently downgrade to an ineligible plate material.

#### Scenario: Simple meal uses a wooden plate
- **WHEN** a Simple meal bill can reserve a clean wooden plate
- **THEN** that plate is eligible and may be embedded in the produced serving

#### Scenario: Fine meal sees only wood and steel plates
- **WHEN** an Advanced/Fine bill can reserve clean wood and steel plates
- **THEN** it rejects the wooden plate and uses the steel plate

#### Scenario: Lavish meal requires luxury service
- **WHEN** an Elaborate/Lavish bill can see steel, plasteel, silver, gold, and registered ceramic plates
- **THEN** only the silver, gold, and registered ceramic plates are eligible

#### Scenario: Unclassified mod meal has no compatibility tier
- **WHEN** a covered mod recipe has no explicit complexity or minimum-plate extension
- **THEN** it retains its original work amount and uses the Simple plate-material tier without a translated-label heuristic

### Requirement: Cookware becomes dirty only after cooking actually begins
A reserved cookware set SHALL remain clean while ingredients are merely being hauled or while the cook has not completed a cooking work tick. Once the lead cook performs cooking work, the set SHALL be dirtied exactly once for that job and SHALL be dropped or placed at the bill giver when the job completes or is interrupted. The dish lifecycle SHALL recover and route that exact item rather than spawn a replacement. Fixed glitterworld cookware is the sole exception: after active cooking it self-cleans and releases as the same clean item without a washing job.

#### Scenario: Job is cancelled while hauling ingredients
- **WHEN** a cook reserves clean cookware but the job ends before the first cooking work tick
- **THEN** the same cookware is released clean and no dirty replacement is spawned

#### Scenario: Job is interrupted after cooking starts
- **WHEN** at least one cooking work tick has occurred before interruption
- **THEN** the same cookware is left at the cooking station with its dirty state set

#### Scenario: Glitterworld cookware finishes active work
- **WHEN** at least one cooking work tick occurs with glitterworld cookware before completion or interruption
- **THEN** the same cookware is released clean under its self-cleaning material contract

### Requirement: Meal complexity uses exact data-driven tables
Immersive Chefs SHALL retain the internal `Simple`, `Advanced`, and `Elaborate` complexity names and multiply a classified recipe's original work amount by configurable defaults of 0.75, 2.0, and 3.0 respectively. The supported setting ranges SHALL be 0.25-2.0 for Simple, 1.0-5.0 for Advanced, and 1.0-8.0 for Elaborate. These vanilla recipe Defs receive the built-in base classification:

| Internal tier | Vanilla meaning | Recipe Defs |
| --- | --- | --- |
| `Simple` | Simple meal | `CookMealSimple`, `CookMealSimpleBulk` |
| `Advanced` | Fine meal | `CookMealFine`, `CookMealFine_Veg`, `CookMealFine_Meat`, `CookMealFineBulk`, `CookMealFineBulk_Meat`, `CookMealFineBulk_Veg` |
| `Elaborate` | Lavish meal | `CookMealLavish`, `CookMealLavish_Meat`, `CookMealLavish_Veg`, `CookMealLavishBulk`, `CookMealLavishBulk_Veg`, `CookMealLavishBulk_Meat` |

Classification SHALL be data-driven through an Immersive Chefs DefModExtension or package-gated compatibility XML so supported food ecosystems can classify or exclude exact recipes without compiled references. The `optional-gameplay-integrations` contract owns those installed package registries and any explicit fast-work exemption. An unclassified recipe SHALL retain its original work amount: Immersive Chefs MUST NOT infer complexity from its label, preferability, ingredient structure, work amount, serving count, `FoodType`, or inheritance alone. Explicit classification SHALL always win, while an unclassified compatible meal MAY still preserve existing ware and provenance without receiving a guessed work multiplier or fabricated plate.

#### Scenario: Compare vanilla meal work amounts
- **WHEN** the default settings are active and otherwise equal Simple, Fine, and Lavish meal recipes have original work amount `W`
- **THEN** their adjusted work amounts are `0.75W`, `2W`, and `3W` respectively

#### Scenario: A food mod classifies a custom recipe
- **WHEN** compatibility XML adds the Elaborate classification to a custom meal recipe
- **THEN** Immersive Chefs applies the Elaborate multiplier without referencing that mod's assembly and without changing the recipe's original Def

#### Scenario: A modded recipe is not explicitly classified
- **WHEN** a covered custom recipe has no Immersive Chefs complexity extension
- **THEN** its work amount remains unchanged in every game language while its compatible ware and provenance behavior can still operate

#### Scenario: Vanilla Cooking Expanded registry is active
- **WHEN** `VanillaExpanded.VCookE` is active and one of its exact full-meal recipes is present in the package-gated compatibility registry
- **THEN** Immersive Chefs applies its declared Simple, Advanced, or Elaborate behavior without classifying unrelated VCE products

### Requirement: Handheld and non-meal foods are excluded
Pemmican, packaged survival meals, and registered travel foods SHALL be treated as handheld food and SHALL require no cookware, plate, cutlery, culinary-quality metadata, Immersive Chefs temperature handling, or dish return. Raw foods, beverages, drugs, and baby food SHALL likewise remain outside the covered meal workflow. Compatibility patches SHALL be able to mark modded foods or recipes as handheld/excluded through a DefModExtension.

#### Scenario: Eat pemmican on a caravan
- **WHEN** a pawn obtains and eats pemmican
- **THEN** no kitchenware is reserved, no missing-cutlery thought is added, and no dirty dish is produced

#### Scenario: Eat a registered packaged food from another mod
- **WHEN** a compatibility patch marks a modded travel meal as handheld
- **THEN** it follows the same exclusion behavior without a C# reference to the supplying mod

### Requirement: Ingredient provenance remains compatible
The production workflow SHALL preserve vanilla `CompIngredients` information and compatible provenance added by Variety Matters Improved Redux and Vanilla Food Variety Expanded. Plate attachment, ware state, culinary quality, and active-provider temperature metadata MUST NOT replace, erase, or collapse the meal's ingredient records. Where stacks cannot merge without losing different ingredient, plate, sanitation, quality, or active-provider temperature state, they SHALL remain separate.

#### Scenario: Cook a modded meal from several ingredients
- **WHEN** a covered Variety Matters or Vanilla Food Variety Expanded recipe completes
- **THEN** the output retains all ingredient identities while also retaining its exact plate count and Immersive Chefs metadata

### Requirement: Imported and externally spawned meals preserve honest plate origin
Covered meals loaded from a save created before Immersive Chefs was active, or created without a plate by debug actions, scenario code, quest/drop contents, or unpatched mod code, SHALL remain explicitly unplated. If no real plate binding exists, ingestion, expiry, destruction, stack operations, and recovery MUST NOT return, duplicate, or invent a plate. Such meals that enter a map SHALL be safe to inspect, stack when otherwise compatible, haul, save/load, eat, or route through the dedicated `Plate meals` work queue without a null-reference or assumed-plate failure.

The `Plate meals` job SHALL run at a valid kitchen work surface under Cooking work, or the owning server workflow when Gastronomy is active, and SHALL attempt to reserve and attach one eligible plate per portion under the same sanitation precedence and complexity-material rules. It SHALL preserve the meal's ingredients, nutrition, age, active-provider temperature state, existing quality, and external ThingComps, and SHALL not rerun cooking or award Cooking experience.

Covered non-handheld meals generated under an Immersive Chefs-controlled origin as a raider's or visitor's personal inventory, an orbital/caravan trader's stock, or settlement trade stock SHALL instead receive one real clean embedded plate per serving at generation. The plate SHALL be `Poor` quality and selection SHALL first prefer the least-luxurious admissible service class, then the lowest-market-value currently registered material within that class. Primitive stone, adobe, and wood form primitive service; non-precious metals and plastic form ordinary service; silver, gold, registered ceramic, and glitterworld materials form luxury service. The binding SHALL transfer with the meal through inventory, capture, trade, hauling, and save/load; buying a meal transfers that exact plate rather than generating another. Pemmican, packaged/travel meals, raw food, drugs, drinks, and baby food remain excluded and receive no plate.

Before an ordinary pawn selects an imported unplated meal, a reachable enabled worker SHALL receive one plating opportunity. In ordinary `Strict` mode the meal SHALL remain outside normal dining selection while clean/allowed ware is unavailable and the plating job SHALL expose its ordinary immediate blocker without creating a global missing-kitchenware alert; the emergency rule SHALL release it visibly unplated when a pawn reaches the hunger threshold. In `Prefer` mode a failed plating opportunity SHALL permit visibly unplated ordinary dining with the missing-ware consequence. In `Off` mode no plating job SHALL be created and the meal SHALL be ware-exempt. Diners SHALL continue to rank otherwise equivalent plated meals ahead of unplated meals.

#### Scenario: An old save contains an unplated meal
- **WHEN** a covered meal was already spawned before Immersive Chefs was added and its serialized data contains no plate binding
- **THEN** loading and eating or expiring it returns no plate and causes no error
- **THEN** a later completed plating job returns only the exact plate it actually attached

#### Scenario: Debug or mod code spawns a meal directly
- **WHEN** external code creates and spawns a covered meal without using an origin-aware generator
- **THEN** the meal is safely marked unplated and may enter the ordinary plating queue without fabricating a plate

#### Scenario: A raider or visitor carries a meal
- **WHEN** pawn inventory generation gives a raider or visitor a covered non-handheld meal
- **THEN** that meal contains a real clean Poor-quality plate using the least-luxurious admissible service class and then the cheapest registered material within that class

#### Scenario: A player buys a meal from a trader or settlement
- **WHEN** covered meal stock is generated and then transferred through the native trade workflow
- **THEN** the purchased meal retains its exact generated plate and the transfer creates no second plate

#### Scenario: Prefer mode cannot plate an imported meal
- **WHEN** one plating opportunity finds no eligible plate for an imported meal in `Prefer` mode
- **THEN** an ordinary pawn may eat it visibly unplated and receives the missing-ware dining consequence rather than waiting indefinitely

#### Scenario: An unplated quest meal is urgently needed
- **WHEN** a starving pawn can reach an imported meal but no plate
- **THEN** the emergency rule permits eating it unplated and records the missing-ware dining consequence rather than blocking ingestion

### Requirement: Nutrient paste meals use plates but paste preparation does not
A normal nutrient paste meal request SHALL make the pawn carry one eligible plate to the dispenser, SHALL attach that exact plate to the dispensed meal, and SHALL leave cutlery acquisition to the later eating workflow. Clean plates SHALL be preferred and Strict emergency fallback SHALL apply when a pawn is at or below the configured hunger threshold.

The dispenser SHALL also expose a distinct on-demand prepared-food operation. That operation SHALL consume hopper contents and produce a paste-derived prepared ingredient rather than an edible plated meal; it SHALL require neither plate nor cutlery, hide exact source ingredients from its public label and ordinary inspection, retain broad dietary and ideology-relevant source flags, carry the configured low preparation quality default of 20, and contribute zero ingredient-origin food-poisoning chance. A finished cooked meal using that ingredient SHALL still combine it with other ingredients and SHALL still receive skill, kitchenware, sanitation, active-provider temperature, and non-paste risk effects.

#### Scenario: Dispense a nutrient paste meal
- **WHEN** a non-emergency pawn operates a nutrient paste dispenser in Strict mode with a clean plate
- **THEN** exactly one plate is transferred into the resulting paste meal and no cutlery is consumed at the dispenser

#### Scenario: Dispense paste-derived prepared food for cooking
- **WHEN** a cook requests prepared food from a stocked nutrient paste dispenser
- **THEN** the output is an unplated cooking ingredient with preparation quality 20, obscured exact provenance, retained broad dietary flags, and no ingredient-origin poisoning chance

#### Scenario: A skilled chef combines paste preparation with fresh ingredients
- **WHEN** the paste-derived ingredient and other foodstuffs are cooked into a covered meal
- **THEN** the final meal can exceed the paste ingredient's low preparation quality through the normal skill and kitchen contribution calculation while preserving applicable dietary restrictions and other poisoning risks

### Requirement: Nutrient paste integrations remain optional
Vanilla nutrient paste behavior SHALL work without Vanilla Nutrient Paste Expanded. When `vanillaexpanded.vnutriente` is active with its required Vanilla Expanded Framework package, compatibility SHALL classify the exact finalized native tap identity through a package- and setting-gated reflection-isolated adapter, SHALL preserve VNPE's own pipe-network behavior by using the shared dispenser methods, and MUST NOT make VNPE or PipeSystem a build or load dependency.

#### Scenario: Vanilla Nutrient Paste Expanded is absent
- **WHEN** Immersive Chefs loads with only vanilla nutrient paste buildings
- **THEN** plate-aware paste meals and paste-derived preparation remain available without a missing type or Def error

#### Scenario: Vanilla Nutrient Paste Expanded is active
- **WHEN** its supported dispenser produces an equivalent paste meal or prepared ingredient
- **THEN** the same plate and preparation contracts apply while the supplying mod retains control of its network behavior

#### Scenario: Vanilla Nutrient Paste Expanded is disabled
- **WHEN** the optional integration policy is Off while the base dispenser and native VNPE tap both exist
- **THEN** the native tap is excluded from Immersive Chefs preparation while the exact base-game dispenser remains supported

### Requirement: Production settings declare when a restart is required
Changes to ware-requirement modes, recipe classification, recipe inclusion, or work multipliers that are materialized into Defs SHALL be labeled as requiring a game restart and SHALL take effect only after Def databases are rebuilt. Runtime selection preferences and the emergency hunger threshold MAY apply immediately when they do not mutate Defs.

#### Scenario: Change the Elaborate work multiplier
- **WHEN** a player saves a new Elaborate multiplier during a running game
- **THEN** the settings UI indicates that a restart is required and existing recipe Defs are not partially mutated mid-session
