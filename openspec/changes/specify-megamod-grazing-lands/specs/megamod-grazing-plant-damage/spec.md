# Grazing nutrition and plant damage

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
S087, composed directly with S043. Proposed observable behavior only; not implemented or accepted.

## ADDED Requirements

### Requirement: Each rooted plant follows one consumption rule
The mod SHALL use crop-health consumption for a supported edible plant that can be sown, normally
dies on harvesting, and has a positive edible harvest yield. Other supported rooted plants SHALL
use S043's growth consumption. Eligibility SHALL retain S043's minimum growth, diet, access,
burial, reservation and current-state checks; edible harvest output alone SHALL NOT make the rooted
plant edible. Plants yielding wood or other non-food SHALL retain the growth rule when the eater
can eat the rooted plant. Classification SHALL depend on plant kind, not whether it currently stands
in a growing zone. A bite SHALL follow only one rule and produce no harvested item.

#### Scenario: Rice growing outside a zone
- **WHEN** an eligible animal grazes sowable rice growing outside a growing zone
- **THEN** the rice follows crop-health consumption just as the same kind inside a zone would

#### Scenario: Wild berry bush and woody tree
- **WHEN** an eligible eater grazes a regrowing berry bush or a supported tree whose harvest is wood
- **THEN** either plant follows growth consumption; the bush uses edible-yield nutrition and the tree uses its own plant nutrition, without spawning berries or wood

#### Scenario: Edible harvest does not grant a new diet
- **WHEN** a plant yields edible food but the pawn cannot eat that rooted plant
- **THEN** the feature does not make that plant selectable or feed its harvested product to the pawn

### Requirement: Yielding growth plants use one explicit nutrition allowance
The mod SHALL value the full growth of a growth-consumed plant with edible yield using the nutrition
of its nominal full harvest, multiplied by the common grazing nutrition scalar and any S043
regrowing bonus, then divided by the crop-consumption cost. Other growth plants SHALL use their
own plant nutrition and S043 bonuses. Only growth above the retained limit SHALL be available;
the bite SHALL be capped to requested nutrition. Nominal yield SHALL exclude harvesting skill,
harvest success, current plant health and the difficulty crop-harvest multiplier. This SHALL NOT
change actual harvest yields, work, item nutrition or item-eating effects.

#### Scenario: Regrowing food plant
- **WHEN** a mature regrowing bush normally yields ten items of 0.05 nutrition each, and default settings apply
- **THEN** its full-growth grazing allowance is 0.875 nutrition, incorporating 350% total regrowing nutrition and 200% consumption cost; exhausting growth down to 8% gives 0.805

#### Scenario: Small bite does not round to a whole berry
- **WHEN** that bush supplies a requested 0.01 nutrition
- **THEN** it supplies exactly 0.01 and loses the corresponding fraction of growth, without rounding the food up to one berry or granting harvested-berry effects

#### Scenario: Non-food harvest
- **WHEN** an eligible tree eater grazes a tree whose harvest is non-food
- **THEN** the grazing allowance uses the tree's plant nutrition rather than a fictional edible wood yield

### Requirement: Crop bites consume a finite share of current health
The mod SHALL give a crop-health plant a full-health grazing allowance equal to its nominal full
harvest nutrition multiplied by current growth and the common nutrition scalar, divided by the
crop-consumption cost. The available bite SHALL be that allowance multiplied by remaining health
fraction, capped to requested nutrition. Health loss SHALL be the corresponding share of maximum
health, rounded upward to a whole health point and capped at remaining health. This rule SHALL
leave growth and age unchanged and destroy the plant once its health is exhausted, without a
harvest drop. S043's regrowing bonus, retained-growth floor, species destruction bonus and pasture
loss roll SHALL NOT additionally apply to this crop-health result.

#### Scenario: Small bite from a healthy mature hay crop
- **WHEN** a mature crop has 85 of 85 health, yields eighteen items of 0.05 nutrition, and a grazer requests 0.09 at default settings
- **THEN** it supplies 0.09, loses 17 health, remains at 68 of 85 health and keeps its growth and age

#### Scenario: Exhaust the damaged crop
- **WHEN** the same unchanged crop at 68 of 85 health receives a request of at least 0.36
- **THEN** it supplies only 0.36 and is destroyed once, with no extra whole-plant or harvest reward

#### Scenario: Immature crop has less food
- **WHEN** an eligible full-health rice plant at 40% growth has nominal harvest nutrition 0.30 and a request of 0.10 under defaults
- **THEN** it supplies at most 0.06 and is destroyed, rather than supplying the nutrition of a mature rice plant

#### Scenario: Slight damage has a proportional effect
- **WHEN** otherwise identical crops have 85 and 84 health out of 85
- **THEN** the latter has 84/85 of the former's available grazing nutrition, without an abrupt halving of its yield

#### Scenario: Very small bites still cost health
- **WHEN** a positive crop bite would cost less than one health point
- **THEN** it costs one health point while granting only the requested available food; splitting a meal into tiny bites can waste more crop health and cannot create free repeated food

### Requirement: Pasture loss happens once at actual depletion
The mod SHALL give a growth-consumed plant with no positive harvest yield a **5%** default chance
of destruction when a successful bite reaches its retained-growth limit. The chance SHALL be the
displayed probability, with 0% never and 100% always destroying. An above-limit bite, a rejected
attempt, a plant already at the limit, or a plant with a positive harvest yield SHALL NOT roll.
Loss SHALL grant no extra nutrition or S043 destruction bonus. A surviving plant SHALL remain at
the limit without gaining growth. S043's qualifying selected-species destruction SHALL take
precedence, consume the plant once and skip the pasture-loss roll.

#### Scenario: Ordinary grass depletion
- **WHEN** a mature 0.50-nutrition grass plant supplies its available 0.46 under defaults
- **THEN** it is destroyed with exactly 5% probability and otherwise remains at 8% growth; either outcome gives only 0.46 nutrition

#### Scenario: Zero and certain loss
- **WHEN** the player sets pasture loss to 0% or 100% and an eligible grass plant is depleted
- **THEN** it respectively survives or is destroyed; merely opening settings, loading a save or inspecting the plant never performs that roll

#### Scenario: One opportunity per actual depletion
- **WHEN** a surviving depleted plant is approached again before positive eligible regrowth
- **THEN** no food or new loss chance is granted; after ordinary regrowth, a later successful depletion can create a new opportunity

#### Scenario: Species destruction is not doubled
- **WHEN** a selected destructive species qualifies for S043's whole-plant consumption
- **THEN** its one hunger-capped result applies without a second destruction event, pasture roll or combined bonus

### Requirement: Grazing controls have common scope and explicit consequences
The mod SHALL extend **Mod settings → Animals and plants → Grazing** with **Pasture loss chance**
at **5%, range 0–100%**, and **Crop-consumption cost** at **200%, range 100–500%**. The existing
nutrition scalar SHALL remain **100%, range 10–1000%**, applying once to all supported grazing.
All three SHALL use common Global default, Use global and Current save controls. Crop cost SHALL
explain both reduced food per growth in yielding growth plants and faster crop-health consumption;
the crop-health explanation SHALL mention whole-health-point rounding. Committed edits SHALL affect
subsequent bites, without altering any plant immediately. No extra tab or operational toggle SHALL
be added. All new labels, help and unavailable status SHALL use the seven baseline languages.

#### Scenario: Fresh settings and inherited save
- **WHEN** no prior settings exist and a new save opens grazing settings
- **THEN** it inherits 100% nutrition, 5% pasture loss and 200% crop cost, with no different first-run values

#### Scenario: Dormant override and scope isolation
- **WHEN** a player overrides crop cost in one save, then checks and unchecks Use global
- **THEN** the save follows the current global value while checked, disables its editor then, and restores its dormant override afterward without changing another save

#### Scenario: Main menu and invalid values
- **WHEN** no game is loaded or an invalid numeric edit is attempted
- **THEN** save controls and mutation actions are disabled at the main menu; invalid edits are rejected and invalid stored numbers recover to a bounded or default value with a diagnostic

#### Scenario: Policy edits do not feed or heal plants
- **WHEN** nutrition or crop cost changes after a pawn selects a plant but before it eats
- **THEN** the bite uses the new policy and current plant state; the edit itself grants no food, growth, health or age reset

### Requirement: Consumption and ordinary lifecycle remain separate
The mod SHALL retain native growth, health, age, normal environmental restrictions and successful
rooted-plant ingestion effects exactly once. Rejected, removed, unsupported or nonpositive bites
SHALL grant neither food nor successful-ingestion effects, and SHALL end or reselect without an
unchanged-target loop. Saving/loading SHALL preserve resulting plant state without replaying food,
damage or a loss roll. Ordinary harvesting SHALL use the remaining plant's native state and rules;
this feature SHALL NOT impose a second harvest-output deduction or heal grazed crops. Shared food
search SHALL use current plant facts and recheck eligibility at the bite.

#### Scenario: Other grazer wins the race
- **WHEN** another pawn consumes or destroys the target before the approached bite completes
- **THEN** the later eater receives no stale food or successful-ingestion effects and releases the failed attempt normally

#### Scenario: Harvest a damaged surviving crop
- **WHEN** a player orders an ordinary harvest of a still-harvestable grazed crop
- **THEN** native harvesting uses its remaining health and growth, normal work and ordinary yield rules, without a second subtraction of previously grazed nutrition

#### Scenario: Reload and winter
- **WHEN** the player reloads a grazed plant during conditions that prevent growth
- **THEN** its saved growth, health and age persist, it gives no repeated food, and neither grazing nor reload makes it regrow in unsuitable conditions

### Requirement: Unsupported providers disable a coherent grazing result
The mod SHALL admit this behavior together with the S043 grazing group, retaining unrelated
features when that group is unavailable. An active original Graze up or Grazing Lands SHALL own its overlapping behavior; the owned group SHALL remain
inactive with an explanatory settings status. Missing or ambiguous required game behavior SHALL
produce bounded startup warnings per skipped enabled patch identifying the unavailable behavior
and fallback. Compatible foreign changes SHALL remain allowed. No automatic source-settings import
or claim of safe source-save migration SHALL be made.

#### Scenario: Original provider installed
- **WHEN** original Grazing Lands is active
- **THEN** the owned combined grazing group stays inactive, leaving the original provider and unrelated owned lifespan behavior untouched

#### Scenario: Partial admission is unsafe
- **WHEN** the plant-consumption behavior cannot be supported but widened eligibility could otherwise be installed
- **THEN** the whole affected grazing group is withheld, with a warning for every skipped enabled patch and no partially widened food selection
