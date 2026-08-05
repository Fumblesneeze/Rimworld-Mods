## ADDED Requirements

**Owning mod:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Prepared food is a distinct cooking ingredient
The prep station, unlocked by `ImmersiveChefs_ProfessionalKitchens`, SHALL transform accepted edible ingredients into a distinct prepared-food item that remains a cooking ingredient rather than a finished meal. Its serialized data SHALL include the source ingredient provenance, conserved nutrition, preparation quality, preparer identity when available, and dietary and ideology-relevant source categories.

#### Scenario: Prep station awaits professional-kitchen research
- **WHEN** a colony has not completed `ImmersiveChefs_ProfessionalKitchens`
- **THEN** the prep station is not available to construct even when an ordinary stove is available

#### Scenario: Chef prepares mixed ingredients
- **WHEN** a chef finishes a prep-station bill using multiple accepted ingredient Defs
- **THEN** the produced prepared food records those source ingredients, their contributed nutrition, the chef's preparation quality, and all relevant dietary categories

#### Scenario: Prepared food is saved in storage
- **WHEN** prepared food is hauled, stored, saved, and loaded
- **THEN** it remains a cooking ingredient with unchanged nutrition, provenance, preparation quality, and dietary metadata

### Requirement: Preparation conserves nutrition
Creating prepared food SHALL consume its source stacks and SHALL conserve their total contributed nutrition within normal RimWorld rounding; splitting, merging, cooking, or destroying prepared food SHALL NOT duplicate nutrition. Prepared-food stacks SHALL merge only when their preparation quality and safety-relevant provenance can be combined without erasing distinctions needed by later cooking and dietary checks.

#### Scenario: Prepare a batch
- **WHEN** source ingredients contributing 1.25 nutrition are converted in one completed prep job
- **THEN** the resulting prepared food contributes 1.25 nutrition within the game's normal rounding and the consumed inputs cannot be reused

#### Scenario: Split and merge a prepared stack
- **WHEN** a prepared-food stack is split and compatible portions are merged again
- **THEN** total nutrition and provenance counts remain unchanged and no source contribution is duplicated

#### Scenario: Incompatible provenance meets in storage
- **WHEN** two prepared-food stacks differ in preparation quality or dietary or ideology-relevant source categories
- **THEN** they do not merge in a way that loses those distinctions

### Requirement: Preparation quality reflects the prep chef
Each completed prep job SHALL assign a deterministic internal preparation-quality score from 0 through 100, primarily derived from the active prep chef's Cooking skill at completion and modified only by documented preparation-tool, health, and station factors. The score SHALL be visible in inspection details and SHALL be carried into every meal that consumes the prepared food as a culinary-quality input.

#### Scenario: Skilled and unskilled chefs prepare equivalent inputs
- **WHEN** chefs with different effective Cooking skill prepare identical ingredients under otherwise identical conditions
- **THEN** the chef with the higher effective skill produces prepared food with a preparation-quality score no lower than the other chef's score

#### Scenario: Prepared food enters a meal recipe
- **WHEN** a cook completes a meal using prepared food
- **THEN** the resulting meal-quality calculation receives the prepared portion's serialized preparation-quality contribution

### Requirement: Prepared food rots rapidly
Prepared food SHALL be perishable and SHALL accumulate rot at a default multiplier of 4.0 relative to the equivalent unprocessed perishable ingredients under the same temperature and storage conditions. The multiplier SHALL be configurable, and refrigeration and freezing SHALL continue to affect rot through RimWorld's normal temperature mechanics.

#### Scenario: Prepared and raw food share storage conditions
- **WHEN** equivalent prepared and unprocessed perishable ingredients spend the same interval at the same temperature with the default setting
- **THEN** the prepared food accumulates four times the comparable rot progress

#### Scenario: Prepared food is frozen
- **WHEN** prepared food is held at a temperature where RimWorld stops food rot
- **THEN** its accelerated multiplier does not cause rot progress while the normal frozen-food rule suspends it

### Requirement: Prepared ingredients accelerate final cooking
Using prepared food in a meal recipe SHALL reduce the lead cook's final cooking work in proportion to the fraction of recipe nutrition supplied by prepared food, capped at a configurable default reduction of 40 percent for a fully prepared recipe. This work reduction SHALL NOT bypass ware reservations, station-assistant timing, recipe skill requirements, or actual cooking work.

#### Scenario: All recipe nutrition is prepared
- **WHEN** prepared food supplies all ingredient nutrition for an eligible meal and the default work-reduction setting applies
- **THEN** the final cooking job requires 60 percent of its otherwise applicable cooking work

#### Scenario: Half the recipe nutrition is prepared
- **WHEN** prepared food supplies half of the ingredient nutrition for an eligible meal and the default setting applies
- **THEN** the final cooking work receives a 20 percent reduction

#### Scenario: Recipe has no prepared input
- **WHEN** a meal is cooked entirely from unprocessed ingredients
- **THEN** prepared-food acceleration contributes no work reduction

### Requirement: Nutrient paste dispenser can make prepared paste
An operational nutrient paste dispenser SHALL expose an on-demand prepared-food output for cooking jobs. The output SHALL consume dispenser feedstock through normal dispenser rules, SHALL be identified to players as paste-derived prepared food rather than a meal, SHALL hide exact source ingredients from ordinary inspection and final meal ingredient display, and SHALL receive a configurable default preparation quality of 20 rather than a pawn-derived score.

#### Scenario: Cooking job requests prepared paste
- **WHEN** an eligible cooking job requests paste-derived prepared food from an operational dispenser with sufficient feedstock
- **THEN** the dispenser consumes feedstock and produces the required nutrition as prepared food with quality 20 and without creating a finished nutrient-paste meal

#### Scenario: Player inspects paste-derived preparation
- **WHEN** a player inspects prepared paste or a later meal's displayed ingredient list
- **THEN** exact hopper ingredient identities are hidden and the displayed source is nutrient paste

### Requirement: Hidden paste retains dietary and ideology safety
Paste-derived prepared food SHALL retain machine-readable aggregate flags for human meat, insect meat, animal products, vegetarian compatibility, and other dietary or ideology-relevant categories present in its feedstock even though exact sources are hidden. Ingredient eligibility, ideology precepts, food restrictions, and meal thoughts SHALL evaluate those flags and SHALL NOT treat provenance hiding as removal of a prohibited source.

#### Scenario: Paste includes a prohibited source
- **WHEN** dispenser feedstock contains a source forbidden by a pawn's food restriction or relevant ideology precept
- **THEN** prepared paste and meals made from it remain ineligible or produce the same applicable source-based consequence despite hiding the exact ingredient name

#### Scenario: Vegetarian-compatible paste is produced
- **WHEN** all consumed feedstock is vegetarian-compatible
- **THEN** the prepared paste retains a vegetarian-compatible aggregate classification

### Requirement: Prepared paste itself is poisoning-safe
Dispensing prepared paste SHALL add no food-poisoning chance, matching nutrient paste's machine-safe preparation. Later cooking SHALL still evaluate poisoning risk from the cook, other ingredients, dirty ware, the completed meal's quality, and temperature or microwave use only while the Immersive Chefs fallback temperature provider is active; Thermodynamics - Hot Meals remains the sole owner of its thermal consequences when installed. The fixed paste preparation quality SHALL be only one input and SHALL NOT cap the quality a skilled cook can achieve with additional ingredients.

#### Scenario: Prepared paste is dispensed
- **WHEN** an operational dispenser creates prepared paste from valid feedstock
- **THEN** the dispensing step contributes zero food-poisoning chance

#### Scenario: Skilled chef combines paste and fresh ingredients
- **WHEN** a skilled chef cooks a meal from prepared paste plus other eligible ingredients using clean high-quality ware
- **THEN** the meal can reach a respectable or higher culinary-quality tier and its final poisoning chance is calculated from the later cooking event rather than inherited from dispensing

### Requirement: Prepared-food settings are bounded and have explicit application timing
Immersive Chefs SHALL expose `PreparedWorkReduction` (default `40%`, range `0%`-`75%`), `PreparedRotMultiplier` (default `4.0`, range `1.0`-`10.0`), and `PastePreparationQuality` (default `20`, integer range `0`-`50`). `PreparedWorkReduction` SHALL apply live to cooking jobs accepted after the change without recalculating work already captured by an active job. `PreparedRotMultiplier` SHALL be labeled restart-required because it changes the prepared-food rot-rate definition and SHALL take effect only after Def databases are rebuilt without rewriting stored rot progress. `PastePreparationQuality` SHALL apply live to newly dispensed prepared paste and SHALL not rewrite serialized quality on existing prepared food.

#### Scenario: Work-reduction setting changes live
- **WHEN** `PreparedWorkReduction` changes to `75%` before a new eligible recipe supplied entirely by prepared ingredients is accepted
- **THEN** that new job requires `25%` of its otherwise applicable final-cooking work while an already active job keeps its captured work

#### Scenario: Rot multiplier waits for restart
- **WHEN** the player saves `PreparedRotMultiplier` as `6.0` during a running game
- **THEN** the settings UI reports that a restart is required and the active Def continues using the prior multiplier
- **THEN** after restart prepared food accumulates rot at six times the comparable raw-food rate without rewriting existing rot progress

#### Scenario: Paste quality changes live
- **WHEN** `PastePreparationQuality` changes from `20` to `30`
- **THEN** prepared paste dispensed afterward records quality `30` while existing prepared paste retains its serialized quality
