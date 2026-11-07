# Ingredient-surplus production

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: replacement, Everybody Gets One - Continued. Specification only; nothing implemented.

## ADDED Requirements

### Requirement: Offer a strict stock trigger for supported one-entry recipes
The mod SHALL offer Produce above ingredient stock on production recipes with exactly one ingredient entry and a supported resource-stock count for every currently eligible ingredient type.
One entry can permit several alternatives. Zero-entry, multi-entry and unsupported non-resource
ingredient recipes show an unavailable reason and retain their previous mode. A player-entered
nonnegative whole-number threshold is compared to the current stock: new work is eligible only
when stock is strictly greater than the threshold and the bill is not suspended. Actual work still
requires the normal usable bench, materials, skills, access and reservations. This mode has no
product target, population multiplier, product-count filters or pause/resume hysteresis.

#### Scenario: Equality does not start work
- **WHEN** a bill's threshold is one hundred and qualifying ingredient stock is one hundred
- **THEN** no new work starts through the surplus trigger; one hundred and one passes the trigger.

#### Scenario: One entry with alternatives
- **WHEN** one supported ingredient entry permits either of two resource types
- **THEN** their eligible physical units contribute to one stock total without requiring two ingredient entries.

#### Scenario: Unsupported ingredient set
- **WHEN** a recipe needs two distinct ingredient entries or its currently eligible alternatives include an uncountable non-resource
- **THEN** the mode is unavailable with a reason instead of silently ignoring part of the required stock.

### Requirement: Count the selected ingredient resources in physical units
The mod SHALL count actual fresh, unfogged resource units in eligible storage and supported resource containers on the bill's map using the recipe's ordinary fixed-or-allowed ingredient eligibility.
A mandatory fixed ingredient counts according to its fixed recipe entry, regardless of dormant bill
filters that native production ignores for that ingredient. For selectable alternatives, use the
intersection of the entry's choices, recipe restrictions and current bill ingredient filter. Show
the fixed ingredient's identity when its filter is not editable; do not require repairing a hidden
filter to use it. Count stacks by units and linked storage contents once. Do not count loose out-of-storage
resources, pawn inventory/equipment, off-map supplies, future output or merely reserved ingredients.
This resource-stock trigger is independent of the product-counting preferences and a product's selected
counting warehouse. Nutrition, work value and products per batch are not ingredient-stock units.
The visible count and next new-work decision agree, including after allowed-ingredient edits and
paused-state transfers. A missing ingredient reference or failed count is unavailable, not zero.

#### Scenario: Raw units rather than nutrition
- **WHEN** two permitted food resources have different nutrition per unit and ten stored units of each
- **THEN** the trigger displays twenty ingredient units, not a converted nutritional total.

#### Scenario: Product choices do not redefine the trigger
- **WHEN** Count inventories or Count loose resources is changed for product targets
- **THEN** the surplus bill's storage-resource trigger keeps its explained scope.

#### Scenario: Filter change while paused
- **WHEN** an allowed ingredient is disabled while paused
- **THEN** its stored units leave the next displayed trigger count and the next start decision together.

#### Scenario: Mandatory fixed ingredient and dormant filter
- **WHEN** a one-entry recipe requires a fixed resource but a retained bill filter would exclude that type
- **THEN** eligible stored units of that fixed ingredient still count just as they remain usable by native production
- **AND** the UI names the fixed ingredient without requiring an unavailable filter control.

### Requirement: A trigger is not a protected reserve
The mod SHALL explain that a qualifying start can consume a full ordinary batch and leave stock at or below the entered threshold.
There is no ingredient reservation budget or promise that simultaneous benches leave that much stock.
Recheck the current trigger before a genuinely new production start; exploratory job checks do not
consume quota. Once physical work is committed under the shared-plan rules, its ordinary continuation
and ready-product collection remain possible even if stock or population later falls below a trigger.
Bound unfinished work retains ordinary creator/access constraints. Autonomous work already forming
and ready output cannot become stranded solely because its consumed ingredients lowered the stock.
No second completion or new batch is credited by collecting existing output. Explicit suspension and
other native safety/availability constraints retain their ordinary meaning.

#### Scenario: A batch crosses the threshold
- **WHEN** stock is one hundred and one, the threshold is one hundred and an ordinary batch consumes ten
- **THEN** the batch can leave ninety-one; the next uncommitted batch is blocked until the trigger is met again.

#### Scenario: Resume unfinished work
- **WHEN** an already committed unfinished batch remains valid but stock has fallen below the threshold
- **THEN** its eligible worker can finish it under normal access/creator rules without authorizing another new batch.

#### Scenario: Collect formed output
- **WHEN** an autonomous batch has finished after consuming the stock that initially passed the trigger
- **THEN** its actual output can still be collected once even though a new batch would currently be blocked.

### Requirement: Keep the threshold contextual and shared with its plan
The mod SHALL show ingredient stock, strict greater-than comparison, threshold, scope and trigger-not-reserve explanation in the bill overview/details.
Use the same nonnegative input bounds and base step one as adaptive production controls. Retain a
valid existing numeric operand when switching to this mode; negative values require explicit
replacement or cancellation. No new global setting or bottom tab is introduced. The threshold and
ingredient choices save/copy with the bill and synchronize across the entire linked plan. A linked
group uses one map's stock, not the sum of identical counts from each bench. Copies are independent
plans and never clone committed physical work. Switching modes preserves dormant adaptive queries
and product-counting choices without applying those controls to the surplus trigger.

#### Scenario: Linked workbenches
- **WHEN** three linked benches share a surplus threshold of one hundred and the map has one hundred and twenty eligible units
- **THEN** all three display one hundred and twenty, not three hundred and sixty, and share threshold edits.

#### Scenario: Save and switch modes
- **WHEN** a surplus bill is saved/reloaded and later returns to its prior adaptive mode
- **THEN** the surplus threshold survives the reload and the retained adaptive query is available again with the displayed operand semantics.

### Requirement: Withhold unsafe triggers without reinterpreting saved work
The mod SHALL admit the surplus controls, supported stock evaluation, new-start decisions and committed-work continuation as a coherent behavior, with bounded startup warnings when required compatibility checks fail.
Saved affected bills retain their mode/threshold/ingredient choices but cannot start new work while
support is unavailable. Display the reason, not a trustworthy zero or an ordinary product target.
Benign unrelated patches are permitted. Original EGO coexistence withholds our overlapping trigger;
no source save conversion or package impersonation is implied. Restored support requires explicit
Resume, or an explicit explained switch to another supported mode. Existing output remains
recoverable through safely supported native completion/collection paths.

#### Scenario: Ingredient eligibility changes after reload
- **WHEN** a saved surplus recipe gains an unsupported ingredient alternative or loses a selected required reference
- **THEN** new work stops with an explanation and the player can repair the filter or deliberately choose another mode.

#### Scenario: Unsafe target shape
- **WHEN** required stock/start/continuation behavior cannot be safely supported at startup
- **THEN** the log identifies the withheld trigger and owned saved bills remain recoverable instead of running with a different meaning.
