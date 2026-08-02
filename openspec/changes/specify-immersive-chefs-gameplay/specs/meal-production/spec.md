## ADDED Requirements

**Mod scope:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Plated meal recipes reserve reusable cookware and one plate per serving
Every covered cooking job SHALL reserve one reachable cookware set and one reachable plate for each meal portion produced, selected as eligible under the ware-precedence matrix below, in addition to the recipe's food ingredients. Clean ware SHALL remain the first choice. The cookware SHALL be a reusable tool and MUST NOT be consumed by the recipe. Each plate SHALL become attached meal metadata rather than an extra loose output. Silverware SHALL be acquired by the diner or restaurant service workflow and MUST NOT be a cooking ingredient. A chef's knife SHALL be optional wearable equipment and MUST NOT be reserved as a bill ingredient.

Reservations SHALL respect pawn reachability, forbidden state, storage and bill ingredient filters, competing reservations, Stuff and quality restrictions, and the current ware-requirement setting.

#### Scenario: Cook a four-portion meal batch
- **WHEN** a cook starts a covered recipe that produces four portions under Strict ware requirements
- **THEN** the job reserves one eligible clean cookware set, four eligible clean plates, and the food ingredients, consumes the four plates into the four plated portions, and returns the cookware as a reusable dirty item at the cooking station

#### Scenario: Two cooks compete for the same wares
- **WHEN** concurrent bills can see only one cookware set and four plates
- **THEN** the first accepted job owns those reservations and the second job waits or selects different eligible wares without duplicating an item or plate count

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

#### Scenario: Clean low-quality and dirty high-quality cookware are both available
- **WHEN** an ordinary covered cooking job searches for cookware
- **THEN** it reserves the clean cookware even when the dirty cookware has a higher material or crafting quality

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

### Requirement: Vanilla meal complexity uses an exact initial table
Immersive Chefs SHALL retain the internal `Simple`, `Advanced`, and `Elaborate` complexity names and multiply a classified recipe's original work amount by configurable defaults of 0.75, 2.0, and 3.0 respectively. The supported setting ranges SHALL be 0.25-2.0 for Simple, 1.0-5.0 for Advanced, and 1.0-8.0 for Elaborate. For the initial change, only these vanilla recipe Defs receive a built-in classification:

| Internal tier | Vanilla meaning | Recipe Defs |
| --- | --- | --- |
| `Simple` | Simple meal | `CookMealSimple`, `CookMealSimpleBulk` |
| `Advanced` | Fine meal | `CookMealFine`, `CookMealFine_Veg`, `CookMealFine_Meat`, `CookMealFineBulk`, `CookMealFineBulk_Meat`, `CookMealFineBulk_Veg` |
| `Elaborate` | Lavish meal | `CookMealLavish`, `CookMealLavish_Meat`, `CookMealLavish_Veg`, `CookMealLavishBulk`, `CookMealLavishBulk_Veg`, `CookMealLavishBulk_Meat` |

Classification SHALL be data-driven through an Immersive Chefs DefModExtension or compatibility XML so a later compatibility change can classify or exclude another recipe without compiled references. An unclassified recipe SHALL retain its original work amount: Immersive Chefs MUST NOT infer complexity from its label, preferability, ingredient structure, work amount, or serving count. Explicit classification SHALL always win. Vanilla Cooking Expanded recipe classification is deferred to a later compatibility change, while its unclassified meals MAY still participate in otherwise supported ware and provenance behavior.

#### Scenario: Compare vanilla meal work amounts
- **WHEN** the default settings are active and otherwise equal Simple, Fine, and Lavish meal recipes have original work amount `W`
- **THEN** their adjusted work amounts are `0.75W`, `2W`, and `3W` respectively

#### Scenario: A food mod classifies a custom recipe
- **WHEN** compatibility XML adds the Elaborate classification to a custom meal recipe
- **THEN** Immersive Chefs applies the Elaborate multiplier without referencing that mod's assembly and without changing the recipe's original Def

#### Scenario: A modded recipe is not explicitly classified
- **WHEN** a covered custom recipe has no Immersive Chefs complexity extension
- **THEN** its work amount remains unchanged in every game language while its compatible ware and provenance behavior can still operate

#### Scenario: Vanilla Cooking Expanded is active before its compatibility change
- **WHEN** `VanillaExpanded.VCookE` is active and one of its recipes has no explicit Immersive Chefs complexity extension
- **THEN** Immersive Chefs does not guess a tier or apply a complexity multiplier to that recipe

### Requirement: Handheld and non-meal foods are excluded
Pemmican, packaged survival meals, and registered travel foods SHALL be treated as handheld food and SHALL require no cookware, plate, silverware, culinary-quality metadata, temperature handling, or dish return. Raw foods, beverages, drugs, and baby food SHALL likewise remain outside the covered meal workflow. Compatibility patches SHALL be able to mark modded foods or recipes as handheld/excluded through a DefModExtension.

#### Scenario: Eat pemmican on a caravan
- **WHEN** a pawn obtains and eats pemmican
- **THEN** no kitchenware is reserved, no missing-silverware thought is added, and no dirty dish is produced

#### Scenario: Eat a registered packaged food from another mod
- **WHEN** a compatibility patch marks a modded travel meal as handheld
- **THEN** it follows the same exclusion behavior without a C# reference to the supplying mod

### Requirement: Ingredient provenance remains compatible
The production workflow SHALL preserve vanilla `CompIngredients` information and compatible provenance added by Variety Matters Improved Redux and Vanilla Food Variety Expanded. Plate attachment, ware state, culinary quality, and temperature metadata MUST NOT replace, erase, or collapse the meal's ingredient records. Where stacks cannot merge without losing different ingredient, plate, sanitation, quality, or temperature state, they SHALL remain separate.

#### Scenario: Cook a modded meal from several ingredients
- **WHEN** a covered Variety Matters or Vanilla Food Variety Expanded recipe completes
- **THEN** the output retains all ingredient identities while also retaining its exact plate count and Immersive Chefs metadata

### Requirement: Imported and externally spawned meals can be plated without recooking
Covered meals that enter a map without an attached plate, including trade goods, quest rewards, drop-pod contents, scenario starts, and outputs from unpatched mod recipes, SHALL enter a dedicated `Plate meals` work queue at a valid kitchen work surface. The job SHALL be governed by Cooking work, or by the owning server workflow when Gastronomy is active, and SHALL attempt to reserve and attach one eligible plate per portion under the same ware-precedence matrix. It SHALL preserve the meal's ingredients, nutrition, age, temperature, and existing quality, and SHALL not rerun cooking or award cooking experience.

Before an ordinary pawn selects an imported unplated meal, a reachable enabled worker SHALL receive one plating opportunity. In ordinary `Strict` mode the meal SHALL remain outside normal dining selection while clean/allowed ware is unavailable and the missing-kitchenware alert identifies the blocked plating surface; the emergency rule SHALL release it visibly unplated when a pawn reaches the hunger threshold. In `Prefer` mode a failed plating opportunity SHALL permit visibly unplated ordinary dining with the missing-ware consequence. In `Off` mode no plating job SHALL be created and the meal SHALL be ware-exempt. Diners SHALL continue to rank otherwise equivalent plated meals ahead of unplated meals.

#### Scenario: A trader delivers an unplated meal stack
- **WHEN** a clean plate supply and valid plating surface are available
- **THEN** a Cooking or Gastronomy-owned plating job attaches the corresponding number of plates before the meals enter ordinary dining circulation without changing their food data

#### Scenario: Prefer mode cannot plate an imported meal
- **WHEN** one plating opportunity finds no eligible plate for an imported meal in `Prefer` mode
- **THEN** an ordinary pawn may eat it visibly unplated and receives the missing-ware dining consequence rather than waiting indefinitely

#### Scenario: An unplated quest meal is urgently needed
- **WHEN** a starving pawn can reach an imported meal but no plate
- **THEN** the emergency rule permits eating it unplated and records the missing-ware dining consequence rather than blocking ingestion

### Requirement: Nutrient paste meals use plates but paste preparation does not
A normal nutrient paste meal request SHALL make the pawn carry one eligible plate to the dispenser, SHALL attach that exact plate to the dispensed meal, and SHALL leave silverware acquisition to the later eating workflow. Clean plates SHALL be preferred and Strict emergency fallback SHALL apply when a pawn is at or below the configured hunger threshold.

The dispenser SHALL also expose a distinct on-demand prepared-food operation. That operation SHALL consume hopper contents and produce a paste-derived prepared ingredient rather than an edible plated meal; it SHALL require neither plate nor silverware, hide exact source ingredients from its public label and ordinary inspection, retain broad dietary and ideology-relevant source flags, carry the configured low preparation quality default of 20, and contribute zero ingredient-origin food-poisoning chance. A finished cooked meal using that ingredient SHALL still combine it with other ingredients and SHALL still receive skill, kitchenware, sanitation, temperature, and non-paste risk effects.

#### Scenario: Dispense a nutrient paste meal
- **WHEN** a non-emergency pawn operates a nutrient paste dispenser in Strict mode with a clean plate
- **THEN** exactly one plate is transferred into the resulting paste meal and no silverware is consumed at the dispenser

#### Scenario: Dispense paste-derived prepared food for cooking
- **WHEN** a cook requests prepared food from a stocked nutrient paste dispenser
- **THEN** the output is an unplated cooking ingredient with preparation quality 20, obscured exact provenance, retained broad dietary flags, and no ingredient-origin poisoning chance

#### Scenario: A skilled chef combines paste preparation with fresh ingredients
- **WHEN** the paste-derived ingredient and other foodstuffs are cooked into a covered meal
- **THEN** the final meal can exceed the paste ingredient's low preparation quality through the normal skill and kitchen contribution calculation while preserving applicable dietary restrictions and other poisoning risks

### Requirement: Nutrient paste integrations remain optional
Vanilla nutrient paste behavior SHALL work without Vanilla Nutrient Paste Expanded. When `vanillaexpanded.vnutriente` is active, compatibility SHALL classify its equivalent dispensers and outputs through package-gated XML or reflection-isolated adapters, SHALL preserve its own hopper and network behavior, and MUST NOT make its assembly a build or load dependency.

#### Scenario: Vanilla Nutrient Paste Expanded is absent
- **WHEN** Immersive Chefs loads with only vanilla nutrient paste buildings
- **THEN** plate-aware paste meals and paste-derived preparation remain available without a missing type or Def error

#### Scenario: Vanilla Nutrient Paste Expanded is active
- **WHEN** its supported dispenser produces an equivalent paste meal or prepared ingredient
- **THEN** the same plate and preparation contracts apply while the supplying mod retains control of its network behavior

### Requirement: Production settings declare when a restart is required
Changes to ware-requirement modes, recipe classification, recipe inclusion, or work multipliers that are materialized into Defs SHALL be labeled as requiring a game restart and SHALL take effect only after Def databases are rebuilt. Runtime selection preferences and the emergency hunger threshold MAY apply immediately when they do not mutate Defs.

#### Scenario: Change the Elaborate work multiplier
- **WHEN** a player saves a new Elaborate multiplier during a running game
- **THEN** the settings UI indicates that a restart is required and existing recipe Defs are not partially mutated mid-session
