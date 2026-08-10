## ADDED Requirements
Owning mod: **Immersive Chefs** — package ID `fumblesneeze.immersivechefs`; repository path `mods/ImmersiveChefs`.

### Requirement: Every affected meal serving retains its culinary state
Each non-excluded meal serving SHALL carry a serialized culinary record containing its exact culinary-quality score and contamination sources. When Thermodynamics - Hot Meals is absent and Immersive Chefs owns temperature, that record SHALL additionally contain current temperature in degrees Celsius and microwave reheat count. Stacking MUST NOT discard or average away active per-serving records: merging SHALL append one record per serving, splitting SHALL transfer the same number of records to the split stack, and ingestion SHALL consume the record belonging to the consumed serving. Save/load, hauling, refrigeration, and map transitions SHALL preserve those records. A legacy or imported serving without a record SHALL initialize safely at culinary score `40` and no Immersive Chefs contamination; only the Immersive Chefs temperature-provider path SHALL also initialize current cell temperature and zero reheats. With Thermodynamics active, any shared-schema temperature/reheat fields remain inert and do not constrain stacking.

#### Scenario: Split and merge preserve individual records
- **WHEN** a stack containing servings with different quality scores, and different Immersive Chefs temperatures while the fallback provider is active, is split and later merged within the normal stack limit
- **THEN** the total number and exact values of the per-serving records remain unchanged

#### Scenario: State survives save and load
- **WHEN** a plated meal with culinary and contamination state, plus Immersive Chefs temperature and reheat state while the fallback provider is active, is saved and the game is reloaded
- **THEN** the same active state is available to its owning system for display, cooling, poisoning calculation, and ingestion

#### Scenario: Imported meal receives conservative defaults
- **WHEN** a compatible mod creates an affected meal without an Immersive Chefs culinary record
- **THEN** the mod initializes it without an error at score `40` and no custom contamination; only the active Immersive Chefs temperature path also initializes current ambient temperature and zero reheats

### Requirement: Culinary quality is a deterministic weighted score
With culinary quality enabled, the mod SHALL compute and clamp a `0`–`100` score from these normalized `0`–`100` components: lead Cooking skill 30%, ingredient diversity 15%, ingredient craftsmanship 10%, preparation quality 15%, cookware cooking-quality modifier 10%, chef's-knife cooking-quality modifier 5%, and accumulated assistant quality 15%. The normalized inputs SHALL be calculated as follows:

| Component | Normalization |
| --- | --- |
| Lead Cooking skill | `clamp(skill / 20 * 100, 0, 100)` at completion |
| Ingredient diversity | `min(100, 20 * distinct original ingredient Def count)` using retained ingredient provenance |
| Ingredient craftsmanship | Nutrition-weighted average of Awful `0`, Poor `25`, Normal `50`, Good `65`, Excellent `80`, Masterwork `90`, Legendary `100`; an ingredient without a quality component is Normal `50` |
| Preparation quality | Nutrition-weighted internal preparation-quality average; an unprepared ingredient contributes neutral `50` |
| Cookware and chef's knife | The respective `Culinary Tool Score = clamp(50 + 2 * Culinary Quality Modifier, 0, 100)` published by the kitchenware system; an absent cookware set or chef's knife contributes `0` |
| Assistant quality | The normalized accumulated input specified by cooperative cooking; no qualifying assistance contributes `0` |

Prepared ingredients MUST expand their retained provenance for the diversity and ingredient-craftsmanship inputs rather than counting only the prepared-food Def. Exact hidden nutrient-paste ingredients MUST remain hidden from player-facing inspection while still using the dietary provenance made available by the prepared-food contract.

#### Scenario: All maximum inputs produce maximum quality
- **WHEN** every normalized component is `100`
- **THEN** the final culinary-quality score is `100`

#### Scenario: Ordinary ingredients receive a defined default
- **WHEN** an ingredient has no RimWorld quality component
- **THEN** it contributes `50` to the nutrition-weighted ingredient-craftsmanship input instead of being omitted or treated as zero

#### Scenario: Prepared provenance counts original variety
- **WHEN** one prepared ingredient retains three distinct original ingredient Defs
- **THEN** those three Defs, rather than only the prepared-food Def, enter the diversity calculation

### Requirement: Quality bands drive a visible gauge and mood
The quality gauge SHALL label scores `0`–`19` Awful, `20`–`34` Poor, `35`–`49` Normal, `50`–`64` Good, `65`–`79` Excellent, `80`–`89` Masterwork, and `90`–`100` Legendary. After ingestion, the eater SHALL receive one non-stacking culinary-quality thought for one in-game day with base mood offsets Awful `-6`, Poor `-3`, Normal `0`, Good `+2`, Excellent `+4`, Masterwork `+6`, and Legendary `+8`, multiplied by `QualityMoodScale`. This thought SHALL supplement, not replace, compatible vanilla or modded thoughts about the meal Def or ingredient variety.

#### Scenario: Excellent meal grants its quality thought
- **WHEN** a pawn eats a serving with culinary-quality score `72` and `QualityMoodScale` is `1.0`
- **THEN** the serving is displayed as Excellent and the pawn receives a `+4` culinary-quality thought for one in-game day

#### Scenario: Repeated meals refresh rather than stack quality thoughts
- **WHEN** a pawn with an active culinary-quality thought eats another affected meal
- **THEN** the pawn has one current culinary-quality thought representing the newly eaten serving rather than multiple copies of that thought

### Requirement: Meals have physical temperature and defined thermal bands under the fallback provider
When Thermodynamics - Hot Meals is absent and Immersive Chefs owns temperature, a serving completed by an actual cooking recipe SHALL start at `70°C`. Its gauge SHALL display Steaming Hot at `55°C` or above, Warm from `35°C` through `54.9°C`, Room Temperature from `15°C` through `34.9°C`, Cold above `0°C` through `14.9°C`, and Frozen at `0°C` or below. Between updates, temperature SHALL move exponentially toward the containing cell's ambient temperature using `ambient + (old - ambient) * 2^(-elapsed / effective half-life)`. The base half-life SHALL be `ThermalHalfLifeHours`; ambient temperatures above `0°C` and at most `10°C` SHALL cool at twice the normal rate, and ambient temperatures at or below `0°C` SHALL cool at four times the normal rate.

#### Scenario: Freshly cooked meal is steaming hot
- **WHEN** a cook completes an affected meal recipe
- **THEN** every produced serving starts at `70°C` and displays Steaming Hot

#### Scenario: Refrigeration and freezing accelerate cooling
- **WHEN** otherwise identical hot servings spend the same duration at `21°C`, `5°C`, and `-5°C`
- **THEN** the serving at `5°C` uses half the base effective half-life, the serving at `-5°C` uses one quarter of it, and both approach their ambient temperatures faster than the serving at `21°C`

#### Scenario: Temperature crosses a gauge boundary
- **WHEN** a serving cools from `15.1°C` to `14.9°C`
- **THEN** its displayed thermal band changes from Room Temperature to Cold

### Requirement: Fallback temperature changes the eating experience
When Thermodynamics - Hot Meals is absent and Immersive Chefs owns temperature, Steaming Hot, Warm, Room Temperature, Cold, and Frozen servings SHALL produce temperature mood offsets `+2`, `+1`, `0`, `-3`, and `-6`, respectively, as one non-stacking temperature thought lasting one in-game day. Cold and Frozen SHALL also add their specified food-poisoning modifiers. The temperature used MUST be the consumed serving's current value, not the stack's average or the temperature when the eating job began.

#### Scenario: Frozen serving is unpleasant and risky
- **WHEN** a pawn ingests an affected serving at `-2°C`
- **THEN** the pawn receives the Frozen temperature thought with mood `-6`
- **THEN** the poisoning calculation includes the Frozen temperature modifier

#### Scenario: Meal cools while being carried
- **WHEN** a serving crosses from Warm to Room Temperature while a pawn is carrying it to a table
- **THEN** ingestion uses Room Temperature effects rather than the earlier Warm effects

### Requirement: One unified calculation applies custom food-poisoning risk
At ingestion the mod SHALL begin with the compatible base-game poisoning probability and add the following percentage-point deltas before one final poisoning roll:

| Source | Delta |
| --- | --- |
| Culinary quality | `(50 - quality score) * 0.20` |
| Cold temperature | `+3` |
| Frozen temperature | `+8` |
| Dirty cookware used to cook the serving | `+15` |
| Dirty plate | `+15` |
| Dirty cutlery | `+10` |
| Wild-water-washed cookware | `+5` |
| Wild-water-washed plate | `+4` |
| Wild-water-washed cutlery | `+3` |
| Plate service score | `(50 - normalized service score) * 0.03` |
| Cutlery service score | `(50 - normalized service score) * 0.03` |
| Microwave reheating | `MicrowaveExtraPoisonChance` for each completed reheat |

The normalized plate or cutlery service score SHALL be `clamp(0.75 * craftsmanship score + 0.25 * material-cleanliness score, 0, 100)`, using the kitchenware system's normalized values; an absent item contributes no service-score delta. Cold, Frozen, and Microwave rows SHALL be included only while Immersive Chefs owns temperature and SHALL contribute nothing when Thermodynamics is active. The sum of custom deltas SHALL be multiplied by `FoodPoisoningEffectScale`, the final probability SHALL be clamped from zero through `MaximumCustomPoisonChance`, and the mod MUST NOT lower a compatible base probability that already exceeds that configured cap. Dirty cookware contamination SHALL remain on the serving record even after the cookware itself is dropped. A pawn eating a serving involving any dirty cookware, plate, or cutlery SHALL additionally receive one non-stacking `Ate with dirty kitchenware` thought at mood `-6` for one in-game day.

The meal's ordinary inspect text MUST NOT reveal whether the serving is food-poisoned or explicitly state that it is not food-poisoned, even when developer mode is enabled; discovering latent food poisoning SHALL remain a consequence of ingestion. When ingestion actually causes food poisoning, Immersive Chefs SHALL attribute the resulting notification/health cause to the single largest positive contributor present in the final calculation. Dirty cookware, dirty plate, dirty cutlery, unsafe wild-water washing, cold/frozen temperature, low culinary quality, microwave reheating, and the compatible vanilla base source SHALL be eligible contributors. Ties SHALL use a deterministic priority matching the risk table's physical severity. The cause text SHALL use a readable label such as `dirty cookware`, `dirty plate`, or `frozen meal`, never `unknown` when the mod supplied a positive risk contributor. This food-poisoning attribution MUST NOT expose latent food-poison state before ingestion or change the actual probability, and is independent from material-specific vanilla toxic buildup owned by the material-kitchenware capability.

Wild-water provenance SHALL be evaluated independently from dirty state, SHALL be preserved in cookware contamination snapshots and embedded plate bindings, and SHALL use the same deltas whether the wash occurred at map water terrain or through caravan travel abstraction. The general `FoodPoisoningEffectScale` and `MaximumCustomPoisonChance` SHALL apply to these deltas. Safely washing an item before use SHALL remove its wild-water delta.

#### Scenario: Dirty full place setting is dramatically riskier
- **WHEN** a pawn eats a serving cooked with dirty cookware from a dirty plate using dirty cutlery at default settings
- **THEN** the custom risk includes `+40` percentage points from dirty-state sources before other deltas and the configured cap
- **THEN** the pawn receives one dirty-kitchenware thought rather than one thought per dirty item

#### Scenario: High quality can reduce custom risk
- **WHEN** a pawn eats a score-`80` serving that is warm, clean, and not reheated
- **THEN** culinary quality contributes `-6` percentage points before scaling and clamping

#### Scenario: Configured risk cap is enforced
- **WHEN** the scaled custom deltas would raise a base poisoning probability below the configured cap to more than `MaximumCustomPoisonChance`
- **THEN** the final probability is limited to `MaximumCustomPoisonChance`

#### Scenario: Wild-water place setting adds bounded risk
- **WHEN** a pawn eats a warm, otherwise clean serving cooked without dirty cookware from a wild-water-washed plate using wild-water-washed cutlery
- **THEN** the custom risk includes `+7` percentage points from wash provenance before scaling and the configured cap

#### Scenario: Inspect a poisoned serving before eating
- **WHEN** a covered meal's vanilla poison component has already selected it as poisoned and the player inspects it, including in developer mode
- **THEN** neither `poisoned` nor `not poisoned` appears in ordinary meal inspection

#### Scenario: Dirty cookware is the largest poisoning contributor
- **WHEN** ingestion causes food poisoning and the final dirty-cookware delta is larger than every other positive source
- **THEN** the resulting player-visible cause names dirty cookware rather than `unknown`

### Requirement: Pawns reheat eligible cold meals in a countertop microwave
When Thermodynamics - Hot Meals is absent, the mod SHALL provide a powered one-cell countertop microwave unlocked directly by vanilla `Electricity` that accepts one eligible plated meal serving per heating job. It SHALL require neither `ImmersiveChefs_Dishwashing` nor `ImmersiveChefs_ProfessionalKitchens`. The microwave SHALL render and construct at `BuildingOnTop`, SHALL be a non-edifice, SHALL not clear or replace the supporting building, and SHALL occupy the top-building altitude for that cell. Its placement worker SHALL require an already completed, spawned table or workbench cell whose Def provides an eating/item surface; bare terrain, blueprints, frames, beds, shelves/storage, and unrelated buildings SHALL be rejected. The support MAY be vanilla or modded and the rule SHALL be capability-based rather than a hard-coded Def-name list. Even More Linkables is an inspected implementation example, not a dependency.

The microwave SHALL retain its own power trader, flick, breakdown, reservation, interaction-cell, and heating state independently of the support. It MUST NOT block ordinary bills, interaction cells, dining use, or facility links of a multi-cell supporting table/workbench merely by sharing one surface cell. If the support becomes invalid through destruction, deconstruction, replacement, or another mod, any active heating job SHALL cancel without applying a completed-reheat mutation: it SHALL NOT set the microwave target temperature, subtract quality, increment the reheat count, add contamination, or replace/remove the embedded plate, while ordinary elapsed-time ambient temperature progression remains valid. The microwave SHALL become a recoverable minified building at that cell or the nearest valid standable cell rather than remain floating, disappear, or duplicate.

When meal temperature is enabled, a pawn intending to eat a serving below `AutoMicrowaveBelow` SHALL prefer a reachable, allowed, powered, and reservable microwave before ingesting it. A completed cycle SHALL set that serving to `60°C`, subtract `MicrowaveQualityLoss` from its culinary score without going below zero, increment its reheat count, and add the per-reheat poisoning delta at eventual ingestion. If no usable microwave exists, reheating MUST remain optional and MUST NOT prevent eating. Recipes excluded by the meal-production contract MUST remain excluded from automatic microwave jobs.

#### Scenario: Electricity makes microwave reheating available
- **WHEN** vanilla `Electricity` is complete but neither Immersive Chefs research project is complete
- **THEN** the microwave is available to construct while both dishwashers and all professional stations remain locked

#### Scenario: Microwave is placed on a table
- **WHEN** the player places a microwave blueprint over one cell of a completed dining table or powered workbench with a valid surface
- **THEN** placement is accepted at `BuildingOnTop`, construction preserves the support, and both buildings retain their own selection, power, and work/dining behavior

#### Scenario: Microwave cannot stand on the floor
- **WHEN** the player moves the same blueprint to bare floor, a blueprint/frame, a bed, a shelf, or an unrelated building
- **THEN** placement is rejected with a concise support requirement and no blueprint is created

#### Scenario: Supporting table is destroyed during reheating
- **WHEN** a pawn is reheating one reserved serving and the supporting table or workbench is destroyed
- **THEN** the heating job cancels without applying heat, quality loss, poison delta, or reheat count, the same serving remains recoverable, and the microwave becomes a single recoverable minified building rather than floating or duplicating

#### Scenario: Pawn reheats a cold meal
- **WHEN** a pawn selects an eligible serving at `5°C`, the threshold is `10°C`, and a powered reachable microwave is available
- **THEN** the pawn heats the serving before eating it
- **THEN** completion sets it to `60°C`, reduces its score by the configured quality loss, and increments its reheat count once

#### Scenario: Microwave is unavailable
- **WHEN** a hungry pawn selects a serving below the threshold but every microwave is unpowered, forbidden, unreachable, or reserved
- **THEN** the pawn may continue to eat the serving without waiting indefinitely for a microwave

#### Scenario: Repeated reheating has cumulative cost
- **WHEN** the same serving completes a second microwave cycle after cooling again
- **THEN** the quality loss is applied a second time and its reheat count becomes `2`

### Requirement: Gastronomy waiters reheat before service
When Gastronomy is active by package ID, Thermodynamics - Hot Meals is absent, and its service job selects an eligible serving below `AutoMicrowaveBelow`, the waiter SHALL insert a microwave-heating step before carrying the serving to the diner when a usable microwave exists. The integration MUST preserve Gastronomy's diner, table, reservation, and service state while reheating. If the microwave becomes unavailable, the waiter SHALL fall back to Gastronomy's normal delivery rather than abandoning or indefinitely reserving the meal.

#### Scenario: Waiter reheats cold order
- **WHEN** a Gastronomy waiter picks up a `4°C` eligible order, the threshold is `10°C`, and a usable microwave exists
- **THEN** the waiter completes the microwave step before delivering that same serving to the assigned diner

#### Scenario: Reheat path fails during service
- **WHEN** the waiter's reserved microwave loses power before heating completes
- **THEN** the microwave reservation is released and the waiter can deliver the original order through Gastronomy's normal flow

### Requirement: Thermodynamics - Hot Meals exclusively owns temperature when active
When exact package `Mlie.DThermodynamicsHotMeals` is active, Immersive Chefs SHALL yield the complete hot/cold-meal concern to that mod. A package-ID guard SHALL remove the Immersive Chefs microwave node before Def deserialization, so its research/designation entry, power/heating comps, and texture are never materialized or resolved. Immersive Chefs SHALL install or execute no meal-temperature initialization/progression, inspect gauge, temperature thought, temperature poisoning delta, microwave reheat count/quality loss, automatic reheat selection, heating job/toil, Gastronomy reheat insertion, or caravan-temperature code. Thermodynamics' `DMicrowave`, `HeatMeal`, `DHotMeals.JobDriver_HeatMeal`, food-temperature comps, ambient diffusion, UI, thoughts, settings, and Harmony patches SHALL remain authoritative.

The exclusion SHALL be package-presence based and SHALL happen before Def and Harmony ownership is finalized. Validation of the inspected Thermodynamics 1.6.6 shape SHALL be diagnostic only: if its known Def, job, class, comp, or assembly shape changes, Immersive Chefs SHALL log one actionable warning but MUST NOT activate its competing fallback in the same process. Immersive Chefs culinary quality, ingredient provenance, physical plate/cutlery lifecycle, sanitation, comfort, expectations, and all non-temperature food-poisoning inputs SHALL continue to operate without reading or rewriting Thermodynamics' temperature state. Shared Immersive Chefs serving records MAY retain inert serialized temperature fields, but those fields MUST NOT progress, display, affect stacking, or contribute an outcome while Thermodynamics owns the concern.

#### Scenario: Thermodynamics is active at startup
- **WHEN** Core, Harmony, Thermodynamics - Hot Meals, and Immersive Chefs load in their declared order
- **THEN** `DMicrowave` and `HeatMeal` exist, `ImmersiveChefs_Microwave` does not exist, and only Thermodynamics owns temperature progression, UI, thoughts, heating selection, jobs, and toils

#### Scenario: Thermodynamics meal still uses physical service ware
- **WHEN** a Thermodynamics-classified meal is cooked onto an Immersive Chefs plate, heated through Thermodynamics, and eaten with cutlery
- **THEN** Thermodynamics alone supplies the temperature behavior while Immersive Chefs preserves culinary quality and returns the exact plate and cutlery dirty once

#### Scenario: Thermodynamics changes its implementation shape
- **WHEN** exact package `Mlie.DThermodynamicsHotMeals` is active but one or more inspected 1.6.6 Def/type/member identities are absent
- **THEN** one warning reports that Thermodynamics compatibility could not be verified, the Immersive Chefs thermal stack remains suppressed, and every non-temperature Immersive Chefs system continues

#### Scenario: Thermodynamics and Gastronomy are both active
- **WHEN** a Gastronomy waiter serves a cold order while Thermodynamics is active
- **THEN** Immersive Chefs adds no microwave step or thermal toil and does not compete with whichever delivery/heating behavior those two upstream mods resolve

### Requirement: Meal-state settings are bounded and apply safely
The mod SHALL expose `CulinaryQualityEnabled` (default `On`), `QualityMoodScale` (default `1.0`, range `0.0`–`2.0`), `FoodPoisoningEffectScale` (default `1.0`, range `0.0`–`3.0`), `MaximumCustomPoisonChance` (default `50%`, range `5%`–`100%`), `MealTemperatureEnabled` (default `On`), `ThermalHalfLifeHours` (default `2.0`, range `0.25`–`12.0`), `AutoMicrowaveBelow` (default `10°C`, range `-10°C`–`30°C`), `MicrowaveQualityLoss` (default `5`, range `0`–`20`), and `MicrowaveExtraPoisonChance` (default `0.5` percentage points, range `0`–`5`). Disabling culinary quality SHALL retain serialized scores but suppress the custom quality gauge, mood, and poisoning delta. Disabling meal temperature SHALL suppress thermal progression, temperature thoughts and risk, and automatic reheating. Scalar changes SHALL apply immediately to future calculations and microwave cycles without rewriting stored records. When Thermodynamics - Hot Meals is active, the Immersive Chefs temperature and microwave controls SHALL be disabled or replaced by one informational ownership notice; their persisted values SHALL remain untouched and have no runtime effect.

#### Scenario: Culinary quality is disabled
- **WHEN** `CulinaryQualityEnabled` is changed to `Off`
- **THEN** existing culinary scores remain serialized but no custom quality gauge, quality thought, or quality poisoning delta is applied

#### Scenario: Temperature system is disabled
- **WHEN** `MealTemperatureEnabled` is `Off`
- **THEN** meals do not progress through custom thermal bands, generate custom temperature effects, or trigger automatic microwave jobs

#### Scenario: New microwave settings do not rewrite history
- **WHEN** the player changes `MicrowaveQualityLoss` or `MicrowaveExtraPoisonChance`
- **THEN** completed reheats retain their recorded score and reheat count while later completed cycles use the new values

#### Scenario: Thermodynamics owns the settings surface
- **WHEN** the Immersive Chefs settings window opens with `Mlie.DThermodynamicsHotMeals` active
- **THEN** culinary-quality and service-ware controls remain usable while the temperature/microwave controls are non-editable or replaced by a notice naming Thermodynamics as the active provider
