# Quality reworking

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S082 documentation only. Numeric defaults below are inspected source baselines, with corrections
explicitly stated; no balance or gameplay acceptance is implied.

## ADDED Requirements

### Requirement: A contextual order requests one quality attempt
The player SHALL be able to request one Increase quality attempt on a player building with quality,
an accessible unowned weapon/apparel item on the ground, or a player sculpture prepared for work at
a suitable art bench. Equipped, worn, inventoried, forbidden-to-worker, burning, enemy-owned or
inaccessible targets are not silently taken. A sculpture must be safely uninstalled before bench
work; it is not worked on while installed or automatically reinstalled at an invented location.
An already minified sculpture is handled as the same physical item. Commands and Architect area
orders use the same eligibility, do not duplicate orders and do not reveal fogged objects.

Show current quality, maximum allowed result, material cost, eligible station/work and the possible
improvement, unchanged and deterioration outcomes. Show actual chances for the chosen worker when
one is known; otherwise label the reference skill rather than imply a universal chance. A target
already at or above the configured ceiling cannot start another attempt, including after a ceiling
change. Each completion removes only its own rework order, regardless of outcome. No automatic
repeat-until-quality behavior is introduced.

#### Scenario: One attempt on a weapon
- **WHEN** the player designates a qualifying ground weapon and an eligible worker completes reworking
- **THEN** one outcome occurs, one attempt's inputs are spent, and another attempt requires a new player order

#### Scenario: Equipped item and prepared sculpture
- **WHEN** the player tries to rework a worn coat or a sculpture
- **THEN** the coat must first become an eligible ground item, and sculpture work waits for safe uninstallation and a usable art bench without duplicating the sculpture

#### Scenario: Masterwork is the initial maximum
- **WHEN** an Excellent item improves under the default ceiling
- **THEN** it becomes Masterwork and cannot receive another reworking attempt
- **AND** neither a setting nor repeated paid attempts provides a Legendary reworking path.

#### Scenario: Ceiling lowered before work
- **WHEN** the quality ceiling is lowered to the ordered item's current quality before work completes
- **THEN** the attempt stops without consuming uncommitted inputs or making a quality roll and explains the ceiling

### Requirement: Reworking uses a capable worker and an operable station
Buildings SHALL be worked on in place by assigned Construction workers using Construction skill;
apparel uses Tailoring work and Crafting skill; weapons use Crafting work and skill; sculpture uses
Art work and Artistic skill. Selecting a worker does not require an unrelated Crafting assignment
for art or tailoring. Eligible work-capable mechs use their actual supported skill. Native disabled
work, manipulation, reservations, reachability, ownership and current work priorities remain.

Portable items require an operable station with a supported ordinary manufacturing route for that
item. The selected route's research, minimum worker skills, material and provider restrictions remain;
power/fuel and interaction-space requirements must also hold. Alternative valid routes are allowed,
but a descriptive station name alone does not prove eligibility. No supported route means the item
is unavailable for reworking. Revalidate before starting, while working and before completion.
The owned repair-knowledge policy also applies because reworking can repair durability; it does not
grant knowledge merely by possessing a high-tech item. Buildings being used incompatibly wait safely.

#### Scenario: Tailor and artist assignments
- **WHEN** an otherwise eligible pawn has Tailoring or Art enabled but general Crafting disabled
- **THEN** the corresponding coat or sculpture rework is available under its actual work assignment

#### Scenario: Unpowered or unlearned route
- **WHEN** the only suitable station lacks power or the worker/colony lacks the selected route's required skill or research
- **THEN** no quality attempt completes through that route, an actual reason is shown, and ordinary valid work remains available

### Requirement: Materials are quoted per item and consumed exactly once
Each attempt SHALL quote and consume a positive whole quantity of the selected reworking material.
Initial source-derived pricing uses the item's material count; where a material-made item has fixed
ingredients as well, use its largest fixed-ingredient quantity and that ingredient, with declaration
order breaking equal quantities. Otherwise use its construction material and material quantity.
For an item not made from selectable material, use wood at pre-industrial technology, steel at
industrial technology and plasteel at spacer or higher technology: the base quantity is the larger
of **10** or the nearest whole number of **10% of baseline item market value divided by that
material's baseline unit value**, with halves rounded to even. Baseline values exclude the item's
current damage and quality. Missing, zero-value or unsupported costs are unavailable, not free.

Multiply the base quantity by the current-quality factor below and by the item's declared bulk-recipe
count when positive, otherwise by **1**; round up and require at least **1** unit. Bulk count
scaling and largest-fixed-ingredient pricing deliberately retain source economics provisionally;
they do not authorize upgrading an entire stack for a single item's payment. A supported replacement
material mapping must name the actual paid ingredient consistently in the quote and delivery.

| Current quality | Material factor |
| --- | --- |
| Awful | 0.2 |
| Poor | 0.6 |
| Normal | 0.9 |
| Good | 1.25 |
| Excellent | 2 |

An attempt concerns one item. Never consume an entire unrelated merged stack or lose the target in
ingredient handling. All completed outcomes pay the same quoted amount once. Before a completed
attempt, cancellation recovers surviving delivered inputs and leaves quality/durability unchanged.
Changed prices require an updated quote and confirmation before further commitment.

#### Scenario: Normal wooden chair
- **WHEN** a baseline Normal wooden dining chair with a 45-unit material cost is reworked
- **THEN** the quoted material is 41 wood, and exactly 41 is consumed for that one completed attempt

#### Scenario: Fractional factor and merged ingredients
- **WHEN** the configured factor is zero or delivered material merges with a larger stack
- **THEN** the attempt still costs at least one unit and consumes only the quoted delivered quantity, preserving unrelated units

### Requirement: Work and outcomes are explicit and bounded
Reworking SHALL use the following baseline labor and outcome rules. Building work is its ordinary
construction work limited to **20–200**. Portable/art work is its ordinary make-work divided by
the worker's relevant skill, using **0.01** as the divisor at zero skill, limited to **1–200**.
Advance work at **1.3 times Construction speed** for buildings or **1.3 times general work speed**
for portable/art work; gain **0.08** relevant experience per active work tick with normal learning
modifiers. No work progresses at unusable speed. These unusually short source caps require later
measured comparison with new construction, normal manufacture and paid reconstruction.

Use effective relevant skill limited to **0–20** for the probability calculation. Improvement chance
is the table's base improvement times skill divided by **14**, limited to 0–100%. Deterioration
chance is its base deterioration plus **15 percentage points times (20 minus skill) divided by 20**,
limited to the probability left after improvement; Awful cannot deteriorate. The remainder is
unchanged. Improvement raises quality by exactly one step, deterioration lowers it by one, and the
ceiling is never exceeded. One completion produces one outcome; load, UI redraw or messaging failure
cannot reroll it.

| Current quality | Base improvement | Base deterioration |
| --- | --- | --- |
| Awful | 95% | 0%, always |
| Poor | 90% | 2% |
| Normal | 85% | 7% |
| Good | 60% | 12% |
| Excellent | 25% | 19% |

On improvement restore full durability; on unchanged outcome repair **10%** of current maximum HP
up to full; on deterioration remove **10%** of the new maximum HP, limited to at least **1** remaining
HP. Round that 10% to the nearest integer with halves to even; clamp final HP to its new valid maximum
in every case. The minimum-1 deterioration floor is a deliberate correction, not a destruction order.
Completion experience uses the starting quality index (Awful **0** through Excellent **4** for eligible starting items) times **80**
for improvement, **50** for unchanged, **40** for deterioration, subject to normal learning rules.
Keep identity, material, style, art description, ownership and supported provider state. Update actual
stats and visible quality/graphics consistently and report the worker, item and outcome without
depending on a particular punctuation or translation in the item's name.

#### Scenario: Skill fourteen Normal attempt
- **WHEN** a skill-14 worker attempts a Normal item with default chances
- **THEN** improvement is 85%, deterioration 11.5% and unchanged 3.5%, with one paid outcome and the corresponding durability change

#### Scenario: Skill twenty low quality
- **WHEN** a skill-20 worker attempts an Awful item under defaults
- **THEN** improvement is capped at 100%, no negative remaining probability is shown, and the result is Poor rather than skipping quality levels

#### Scenario: Deterioration on a badly damaged item
- **WHEN** a completed attempt lowers the quality of an item already below the deterioration damage amount
- **THEN** it remains one item with at least 1 HP, its quality-dependent stats and graphics agree, and the completed attempt cannot be rerolled by reloading

### Requirement: Shared settings control policy without owning entity orders
Configuration SHALL use the common global-default, per-save override and **Use global** rows under
Work / Upgrades and quality reworking. The maximum result defaults to **Masterwork**, selectable
Awful through Masterwork; Awful permits no quality attempt. Initial reworking cannot create Legendary
quality or rework a Masterwork/Legendary item. Ordinary acquisition and manufacture of Legendary
items are unaffected. Five improvement bases and four non-Awful deterioration bases default to the
table above with **0–100%** ranges; five material factors default
to the pricing table with **0–10** ranges. All values must be finite and within range. Explain
reference skill, actual worker-dependent probabilities and the one-unit minimum. Hidden higher-quality
rows retain their values when the ceiling is lowered.

New/missing save values inherit the current global values dynamically. An override affects only its
save; enabling Use global retains the dormant override, disabling it restores that value or seeds
the first override from the effective global value. Disable both save editor and Use global at the
main menu, including their callbacks and save resets, while leaving global editing available.
Scoped resets restore only the selected scope. Prices and eligibility refresh at the next relevant
decision; a change to a committed attempt's cost, ceiling or chance policy pauses it for confirmation
or safe cancellation, never silently makes a cheaper repeat or rerolls a result. Entity orders and
worker/station choices remain contextual gameplay state, not inherited settings.

#### Scenario: Inheritance and main menu
- **WHEN** the player edits globals at the main menu and then loads an inherited save and an overridden save
- **THEN** only the inherited effective values follow the new globals, and no save control was editable or mutated at the main menu

#### Scenario: Policy edit during an attempt
- **WHEN** chances or material factors change during an already committed attempt
- **THEN** the attempt pauses with its current work and surviving inputs for an explicit new quote or cancellation rather than silently changing its bargain

### Requirement: Reworking composes with owned features and preserves recovery
Reworking SHALL keep its single attempt separate from quality-construction reconstruction, material
replacement, building conversion, ordinary repair, recycling and destruction. One target cannot be
worked by incompatible owned operations at once; a conflicting new order explains the conflict and
requires canceling the old one first. A rework outcome does not automatically initiate or replenish
a quality-construction retry run. While its stored target remains unmet, preserve that target and
spent allowance, including exhaustion. If reworking reaches the stored target, ordinary quality-
construction completion applies: end that run and clear its count. Later reworking that lowers the
item below that achieved target shows the unmet quality but does not start reconstruction; a fresh
run requires an explicit contextual quality-construction request. Rework does not count as producing
a new item for bill quotas or linked-production iteration accounting.

Save/load retains the actual item, location, earned work and delivered inputs without replaying a
completed outcome. Target loss, canceled orders, invalid stations and loss of eligibility stop work
safely; no late mutation of another item or endless retry occurs. Normal hauling capacity and access
restrictions still apply. No new bottom tab, duplicate settings page or unrestricted quality editor
is introduced. Localize every new visible label, outcome,
tooltip and refusal in all seven baseline languages. Any debug override belongs in the top debug UI.
Original-provider ownership and local, warned compatibility failure follow building conversion's
admission requirement; compatible unrelated modifications are allowed and foreign changes retained.

#### Scenario: Competing quality operations
- **WHEN** a player requests reworking while the same building has an active conversion or quality-reconstruction order
- **THEN** the conflict is explained and no second job starts, and later reworking that still misses its stored target cannot reset the reconstruction budget or credit a produced item

#### Scenario: Reworking achieves an exhausted construction target
- **WHEN** an exhausted Normal building is reworked to its stored Good target
- **THEN** the quality-construction run ends and its count clears under the ordinary achieved-target rule, without scheduling another replacement
- **AND** a later rework deterioration to Normal shows the target as unmet but starts no reconstruction until the player explicitly requests a new quality-construction run

#### Scenario: Interrupted bench work
- **WHEN** a saved rework is interrupted by a lost station, canceled order or removed target
- **THEN** surviving inputs and the actual item remain recoverable, no quality outcome occurs late, and no other object inherits the work
