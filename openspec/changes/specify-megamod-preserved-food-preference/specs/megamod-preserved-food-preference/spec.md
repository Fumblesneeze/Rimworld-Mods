# Preserved-food preference and trader provisions

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S057/P1-51. Draft only; no implementation or acceptance. Source-derived values
and proposed corrections below are explicit draft choices, not newly confirmed user preferences.

## ADDED Requirements

### Requirement: PF01 Food preference preserves permission and food properties
The feature SHALL prefer ordinary meals for humanlike eaters through bounded preferences while
retaining their food policies, current action's permissions, access, hunger, diet, title rules where
applicable, and real ingredient concerns. Preserved food remains edible when allowed. Nutrition,
mood effects, ingredients, food quality/category and animal feeding preferences remain unchanged.
Forced consumption and medicine/drug administration are not reselected by this feature.

#### Scenario: Rations are the only permitted food
- **WHEN** a hungry humanlike has only permitted reachable preserved food
- **THEN** it remains selectable and the pawn can eat its ordinary portion without changing food policy

#### Scenario: Nearby meal is forbidden or unsuitable
- **WHEN** a fresh meal violates the current eater's policy, access or dietary requirements
- **THEN** its preference gives no permission to use it and an allowed alternative remains available

#### Scenario: Animal or explicit player order
- **WHEN** an animal chooses food or the player explicitly orders ingestion of a permitted item
- **THEN** this feature does not change the animal's preference or substitute a different item for that order

#### Scenario: Food effects remain real
- **WHEN** a pawn eats food selected by this preference
- **THEN** its nutrition, ingredient consequences, poisoning history and actual taste or ideology mood effects remain normal

### Requirement: PF02 Shelf life determines the ordinary preference class
The feature SHALL classify edible human food of at least the ordinary tasty-raw-food rating as
preserved when its type never rots or normally starts rotting after at least 15 days. Current
freezing or remaining freshness does not change that type classification. Food rated above tasty
raw food with an intrinsic rot-start time of at most 3 days is short-lived; the interval between
3 and 15 days receives neither adjustment. Kibble is excluded from these two classes. Baby food
receives neither class adjustment. Supported modded foods use the same rules; names alone do not
establish shelf life. Current eligibility remains required for any choice.

#### Scenario: Pemmican and packaged food
- **WHEN** pemmican normally lasts 70 days and a packaged survival meal never rots
- **THEN** both are preserved foods while an ordinary four-day simple meal is not

#### Scenario: Frozen fresh meal
- **WHEN** a four-day meal is frozen indefinitely or a preserved food is near its actual rot time
- **THEN** their intrinsic class stays unchanged while ordinary current-spoilage considerations still apply

#### Scenario: Boundary and added food
- **WHEN** supported food has a three-day, fourteen-day or fifteen-day normal shelf life
- **THEN** it is short-lived, neither, or preserved respectively, subject to the stated food-rating rules

#### Scenario: Baby food and kibble
- **WHEN** baby food or kibble is considered for a humanlike
- **THEN** it receives no short-lived or preserved-food adjustment; ordinary permission and any separate raw-food penalty still apply

### Requirement: PF03 Map choices use finite documented preferences
For humanlike map food comparisons, the feature SHALL apply these preference contributions once.
The ordinary food-specific contribution means the food's existing preference before this feature;
distance, current spoilage, traits and unrelated bonuses remain separate and unchanged.

| Food class | Selected food-specific contribution |
| --- | --- |
| Food ordinarily reserved for desperation | Existing contribution minus 200 points |
| Unpleasant raw food | Existing contribution minus 100 points |
| Preserved food with a rot process | Keep a contribution already at or below -35; otherwise use -35, followed by PF04 taste weighting |
| Preserved food that never rots | Keep a contribution already at or below -50; otherwise use -50, followed by PF04 taste weighting |
| Short-lived food | 5 points, or 12 for nutrient paste, followed by PF04 taste weighting |
| Other food | Existing contribution |

Rejected/nonfood choices stay rejected. The rules do not force a full-map search or replace native
early choices of a visitor's carried food or personal food about to rot. They do not reorder every
personal inventory stack. Finite preference is not a promise that all fresh meals always win.

#### Scenario: Ordinary meals win otherwise comparable choices
- **WHEN** equally accessible map simple meals, pemmican and packaged survival meals have no differing personal concerns or other bonuses
- **THEN** the simple meal is preferred, using food contributions 16, -35 and -50 for the inspected Core foods

#### Scenario: Distance or personal preference can outweigh preservation
- **WHEN** an allowed preserved food gains enough ordinary distance or personal-preference advantage to outweigh its penalty
- **THEN** it can be selected instead of a distant ordinary meal

#### Scenario: Stronger existing preserved-food penalty
- **WHEN** a preserved food already contributes -60 points before this feature
- **THEN** that contribution remains -60 and no additional preservation taste compensation is applied

#### Scenario: Native carried-food exception
- **WHEN** ordinary selection immediately uses a visitor's carried food or personal food about to rot before comparing candidates
- **THEN** that native choice remains intact and no new inventory reorder or map scan is forced

### Requirement: PF04 Taste weighting is specific to the actual eater
The feature SHALL use only applicable baseline food reactions: the food's direct taste/special
reaction, or its matching ordinary baseline eating belief that the current eater actually has.
Count a reaction once. When PF03 replaces a preserved-food contribution, reduce the choice benefit
of each such pleasant reaction of more than 0 and at most 20 mood by 98%. For short-lived food,
counterweight each such unpleasant reaction from -5 through less than 0 mood in the choice score.
Use the game's normal conversion of mood to preference points. Do not cancel ingredient aversions,
special nonbaseline beliefs, traits or reactions absent for this eater; actual mood is never changed.

#### Scenario: Pleasant preserved food remains less attractive for storage use
- **WHEN** an eligible preserved meal has a baseline +5 taste reaction whose ordinary preference benefit is 40 points and PF03 replaces its food contribution
- **THEN** that reaction contributes only 0.8 points to this selection comparison while eating still gives the real +5 reaction

#### Scenario: Ordinary paste aversion is counterweighted for choice
- **WHEN** an eater has the ordinary -4 paste reaction worth -65 choice points
- **THEN** its -65 choice penalty is counterweighted for eligible short-lived paste, while the -4 mood effect after eating remains

#### Scenario: Ascetic or different belief
- **WHEN** a reaction is absent for an ascetic or a pawn has a different special eating belief
- **THEN** no compensation is invented from another pawn's or the default population's reaction

#### Scenario: Extreme or ingredient reaction
- **WHEN** an applicable reaction lies outside the stated bounds or concerns disliked ingredients
- **THEN** that reaction keeps its ordinary choice effect and is not cancelled by this rule

### Requirement: PF05 Caravan eating keeps its distinct rules
For humanlike caravan eating, the feature SHALL rank a preserved food ordinarily rated simple-meal
or better at the ordinary awful-meal rating, preserving the normal current-spoilage tie-breaker.
Other food ratings and the native low-priority-travel-food rule stay unchanged for that comparison.
This changes caravan choice only, not the food's actual category, title suitability elsewhere,
animal preference, nutrition or mood. Map point adjustments are not added to caravan scores.

#### Scenario: Travel with both ordinary and preserved meals
- **WHEN** a caravan carries permitted simple meals and pemmican or packaged survival meals
- **THEN** humanlikes use simple meals first under otherwise ordinary travel rules, then can eat the preserved supplies

#### Scenario: Low-priority or emergency travel food
- **WHEN** a food is marked low-priority for caravan use or ordinary hunger rules prohibit emergency food
- **THEN** this feature does not promote it above that restriction

#### Scenario: Mixed caravan
- **WHEN** a caravan contains humanlikes and animals
- **THEN** only the humanlikes receive the preserved-meal ranking change and animals retain normal food scoring

#### Scenario: Food policy survives a journey
- **WHEN** the caravan leaves a map, consumes food and returns
- **THEN** the feature has not changed any pawn's food policy, and remaining food retains its real category and effects

### Requirement: PF06 Food preferences compose once with shared selection
The feature SHALL contribute once to the food comparison already being performed. Common Sense's
selected current-spoilage bonus remains one separate contribution. Shared carried-food borrowing
remains a fallback after ordinary search fails, retaining its about-to-rot donor class before
ranking within that class. Physical portions, cargo attribution, capacity and food history keep
their shared owners. Ranking alone never moves, creates, consumes or reserves food.

#### Scenario: Spoilage preference and preservation are both active
- **WHEN** an eligible preserved food with one day of remaining rot progress is compared with both features active
- **THEN** it receives one preservation contribution and the selected Common Sense bonus of 16 points once, while native other considerations remain

#### Scenario: Food borrowed from a colonist
- **WHEN** native food search fails and shared carried-food selection considers eligible donors
- **THEN** the established donor class ordering is retained and this preference contributes once within the appropriate comparison
- **AND** actual retrieval and eating still use the real portion, capacity and ownership rules

#### Scenario: Inspect or repeat a search
- **WHEN** food choice is calculated repeatedly without an eating action
- **THEN** no inventory, food policy, reservation, nutrition or stack quantity changes because of that calculation

### Requirement: PF07 Trading visitors receive bounded usable provisions once
After a successful trading-caravan arrival, the feature SHALL give each living humanlike visitor
who needs food, can carry supplies and lacks any currently edible carried food of at least tasty-raw
rating one bounded supply of preserved food. Existing carried food must satisfy that visitor's
normal automatic-eating permission to count. Animals and player caravans do not receive this grant.
Existing items are not removed. This is an arrival provision, not recurring refilling or a reward.

Choose uniformly from eligible preserved ordinary-food types that need no material choice, supply
positive finite nutrition, are not drugs, baby food or special nonfood substances, satisfy this
visitor's eating rules and fit at least one unit. Compatible modded food can participate without a
new faction-tech/research gate. For the chosen type let the maximum count be the smallest of its
stack limit, count fitting actual remaining carrying capacity, and whole units fitting 6 nutrition.
Grant a random whole count from max(1, floor(maximum/2)) through maximum inclusive. If no type fits,
leave the visitor unchanged. Failed or repeated arrival notifications do not create another grant.

#### Scenario: Unprovisioned visitor
- **WHEN** a successfully arrived eligible visitor can carry six permitted 0.9-nutrition survival meals and has no suitable food
- **THEN** it receives one stack of three through six meals and no existing inventory is removed

#### Scenario: Already provisioned or unsuitable contents
- **WHEN** one visitor already carries edible food while another carries only food its diet forbids
- **THEN** the first receives nothing and the second is eligible for permitted provisions

#### Scenario: Capacity, nutrition or empty catalog
- **WHEN** only one unit fits, nutrition per unit exceeds six, or no safe preserved food type exists
- **THEN** the first case grants one unit and the other cases grant nothing, without zero-sized stacks or exceeded capacity

#### Scenario: Repeat notification, reload or failed arrival
- **WHEN** an arrival is reported twice, saved visitors are reloaded, or the arrival fails
- **THEN** no extra provision is created for that event and no existing item is duplicated or deleted

#### Scenario: Provisions are physical food
- **WHEN** a visitor later eats granted provisions
- **THEN** the actual carried stack decreases through ordinary eating and supplies its real nutrition and mood consequences
- **AND** no automatic replacement grant occurs

### Requirement: PF08 Unavailable behavior and lifecycle remain truthful
The feature SHALL preserve ordinary loaded-game behavior for any unavailable preference or provision
branch, with one bounded startup warning identifying an enabled unavailable behavior, reason and
fallback. An active original Prioritize Meals over Preserved Foods suppresses all overlapping owned
branches with a warning, without editing its data or commands. Compatible unrelated changes are
not alone a reason to disable the feature. Repeated startup/load never accumulates penalties or
reissues provisions. Existing physical food remains when a feature becomes unavailable.

#### Scenario: Original present
- **WHEN** the original source mod is active
- **THEN** only its overlapping behavior remains active and our startup warning explains the suppression without undoing its changes

#### Scenario: A branch cannot safely operate
- **WHEN** map scoring, caravan scoring or arrival provision support is unavailable
- **THEN** that branch falls back to ordinary loaded-game behavior with a useful warning and no partial score or supply mutation

#### Scenario: Load or change food content
- **WHEN** a save reloads or supported food definitions differ in a new game session
- **THEN** preferences use the current food properties without doubled penalties, stale candidate choices or repeated visitor grants

#### Scenario: Existing controls and settings
- **WHEN** the player manages food policies or opens mod settings
- **THEN** policies remain their own contextual gameplay state and this feature adds no bottom tab or separate tuning menu
- **AND** any later selected configuration must use the common global/default and per-save controls
