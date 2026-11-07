# Material-aware recipe comparison

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: Quartermaster-inspired comparison, adapted by the confirmed Add bill interview.
Status: observable contract drafted; no implementation or gameplay acceptance.

## ADDED Requirements

### Requirement: Comparison belongs to the selected workbench
The feature SHALL present comparisons within the selected workbench's Add bill workflow, without
adding its own bottom tab or separate equipment-management menu. It SHALL retain the recipes and
style choices that can currently be added there under ordinary game restrictions. Distinct recipes
for the same product SHALL remain distinct. A recipe lacking a supported comparison SHALL remain
available through its ordinary bill action.
For a bench in a whole-list workbench group, current addable choices SHALL respect that group's
common supported recipe catalog, including the ordinary fallback action; comparison does not bypass
group eligibility. Unlinked benches retain their own ordinary catalog.

#### Scenario: Two ways to produce the same item
- **WHEN** the opened bench offers two recipes for the same item with different ingredient quantities
- **THEN** both remain selectable and each comparison uses its own requirements
- **AND** adding either creates that recipe's bill on this bench

#### Scenario: A recipe belongs to another bench or remains locked
- **WHEN** an item is craftable elsewhere or its recipe is not yet available at this bench
- **THEN** comparison does not make that recipe addable here or bypass its unlock restrictions

#### Scenario: Recipe with a special result
- **WHEN** the bench offers an ordinary bill action whose output cannot be compared reliably
- **THEN** the action remains accessible and its comparison is clearly marked unavailable

### Requirement: Compare stated attributes at Normal quality
The feature SHALL show the selected product attribute and material using **Normal quality** wherever
quality applies, with no quality selector. Values SHALL describe the product, or a clearly labeled
declared equipped/worn bonus, rather than an unspecified pawn's final performance. The comparison
SHALL not alter actual production quality, recipe costs, output quantities, work or game statistics.
It SHALL not present a combined armor score, estimated combat outcome or monetary value as though
it were the selected attribute.

#### Scenario: Compare blunt protection
- **WHEN** the player sorts two armor recipes by highest blunt protection
- **THEN** the displayed values and order compare Normal-quality blunt protection in their stated materials
- **AND** sharp protection, price and Quartermaster's combined score do not outweigh that attribute

#### Scenario: Skilled crafter can exceed the preview quality
- **WHEN** the player adds a recipe compared at Normal quality and a skilled colonist later makes it
- **THEN** the ordinary production-quality outcome remains in effect
- **AND** browsing did not promise that the actual product would be Normal quality

#### Scenario: A bonus depends on a wearer
- **WHEN** an output has a declared equipment bonus but its final effect depends on the wearer
- **THEN** any supported bonus comparison is labeled as that bonus
- **AND** the view does not claim a final pawn stat, wearability or combat result

### Requirement: Rank by the chosen attribute and direction
The feature SHALL offer only comparison attributes applicable to at least one of the currently
filtered recipe outputs, label the direction of each sort, and rank known values in that direction.
Higher-is-better and lower-is-better choices SHALL be distinguishable, such as highest blunt protection
and lowest mass. Equal values SHALL use a stable alphabetical recipe/material order, with a stable
remaining tie order. Unknown or inapplicable values SHALL be visibly unranked after known values,
independently of direction; they SHALL not be treated as zero.

#### Scenario: Lower mass is the requested comparison
- **WHEN** the player chooses lowest mass
- **THEN** the lightest known output is first and a missing value does not win by appearing as zero

#### Scenario: Filtering removes the active attribute
- **WHEN** the filtered recipes no longer have a comparable value for the active attribute
- **THEN** that sort is cleared with a visible explanation and the remaining recipes use their ordinary order
- **AND** the view does not silently substitute a different attribute

#### Scenario: Unchanged inputs have tied values
- **WHEN** the same recipes and materials are refreshed without changes to their comparison values
- **THEN** tied rows retain the same order

### Requirement: Available-material comparison is the default
The feature SHALL place **Use all materials** beside the stat-sort control, initially unchecked.
With a stat sort selected and the checkbox unchecked, each supported recipe SHALL be compared using
the compatible material that ranks best for that stat and has enough eligible stock for **one complete
execution of that recipe**, including its other ingredients. A batch recipe SHALL use one batch's
requirements, not the cost of one product. Material price or being gold, jade or silver SHALL not
silently demote a better result. A row without a feasible material SHALL say so and SHALL not display
an unavailable material as its available choice.

#### Scenario: Better material is insufficient
- **WHEN** a stronger compatible material is insufficient for one execution but a weaker one is sufficient
- **THEN** the unchecked mode compares the weaker available material and identifies it

#### Scenario: Precious material has the best selected value
- **WHEN** enough gold is available and it has the best value for the selected attribute
- **THEN** the automatic choice is not displaced solely because gold is expensive

#### Scenario: Enough material for one item but not the batch
- **WHEN** a batch recipe consumes more than the colony has
- **THEN** that material is not labeled sufficient merely because one output item's cost would be covered

### Requirement: All-material comparison supports future plans
With **Use all materials** checked, the feature SHALL choose the best compatible material for the
selected stat without requiring current stock. It SHALL keep the chosen material's actual shortages
and skill availability visible. Checking the box SHALL not unlock recipes, change their ingredients,
grant materials, or mark an unavailable variant ready. Unchecking SHALL return to the available-material
rule. A material-independent output SHALL retain its own value in either mode.

#### Scenario: Plan an unavailable material
- **WHEN** the best compatible material is absent and the player checks Use all materials
- **THEN** its value and name become the comparison and the row still identifies its missing ingredients
- **AND** it can be added as a future bill under the ordinary game rules

#### Scenario: Return to available materials
- **WHEN** the player unchecks Use all materials after examining an unavailable stronger variant
- **THEN** the comparison returns to the best feasible variant or explicitly reports that none is feasible

### Requirement: Ingredient availability is a finite allocation
The feature SHALL evaluate the recipe's actual ingredient quantities, alternatives, mixing rules and
material restrictions for one execution. A physical unit SHALL be counted at most once across that
recipe's overlapping requirements. The count SHALL include eligible loose items on the bench's map
as well as eligible stored items; it SHALL exclude hidden, destroyed, forbidden or disallowed items,
items on other maps or caravans, and personal inventory/equipment that the bill cannot ordinarily use.
Supported ingredient containers SHALL contribute only their accessible contents. A result whose
ingredient rules cannot be evaluated reliably SHALL say **Availability unknown**.

These are supply estimates for a new bill's ordinary ingredient filters and search area. They do not
reserve goods, promise pawn-specific access, or allocate supplies across every recipe row or existing bill.

#### Scenario: Loose stock outside a stockpile
- **WHEN** enough allowed, visible ingredients lie on the map within the new bill's search area
- **THEN** the estimate does not reject them merely because they are outside a storage zone

#### Scenario: One stack matches two requirements
- **WHEN** two ingredient requirements can use the same stack but its total quantity cannot satisfy both
- **THEN** the recipe is not highlighted as having enough ingredients

#### Scenario: Alternatives and nutrition quantities
- **WHEN** a recipe permits mixed ingredients counted by nutrition or another supported quantity
- **THEN** sufficiency uses those quantities and the recipe's mixing rules, without counting units twice

#### Scenario: Separate recipes compete for supplies
- **WHEN** two rows each have enough supplies individually but not enough to execute both
- **THEN** each estimate remains explicitly per recipe and neither browsing nor adding a bill reserves stock

### Requirement: Highlight supplies and skill without promising immediate work
The feature SHALL highlight a recipe variant only when it has enough ingredients and at least one
living free colonist on the bench's map meets all of that recipe's skill requirements. It SHALL show
missing ingredients and missing skill separately, including both when both fail. The status SHALL
be described as ingredients and skill availability, not a guarantee of immediate work. Power, work
assignment, health, reachability, reservations and any additional native worker restrictions SHALL
continue to govern the actual job. Animals, prisoners and colonists on another map SHALL not satisfy
this colonist skill check.

#### Scenario: Supplies exist but no suitably skilled colonist does
- **WHEN** every ingredient is available but no current-map free colonist meets all required skills
- **THEN** the row reports the missing skill and is not highlighted as ready
- **AND** the player can still queue the recipe with the ordinary missing-worker warning

#### Scenario: Different colonists meet different requirements
- **WHEN** one colonist meets one requirement and a second meets another but neither meets all of them
- **THEN** the skill check fails rather than combining their abilities

#### Scenario: The bench has no power
- **WHEN** ingredients and a sufficiently skilled colonist exist but the bench lacks power
- **THEN** the supply/skill estimate does not grant or imply an executable job
- **AND** the native power requirement still prevents work

### Requirement: Adding a compared recipe preserves the chosen material
An explicit add action on a supported compared variant SHALL add exactly one ordinary bill to the
workbench whose view is open, with its selected recipe/style and the displayed output material.
Required fixed ingredients and unrelated ingredient choices SHALL remain usable. The new bill SHALL
retain ordinary repetition and other defaults unless another owned feature explicitly changes them.
Comparing, sorting or opening an information card SHALL not add bills, consume or reserve resources,
create products or alter existing bills.

#### Scenario: Chosen fabric plus a fixed metal ingredient
- **WHEN** the player adds a recipe compared in a particular fabric that also requires a fixed metal
- **THEN** the new bill uses that fabric for its output while still permitting the required metal
- **AND** no other workbench or existing bill is changed by this comparison action

#### Scenario: Native special bill variant
- **WHEN** an addable recipe uses unfinished production or another native production-bill type
- **THEN** its ordinary bill type, style choice and production behavior are preserved

#### Scenario: Compare without committing
- **WHEN** the player changes sort/material mode and closes the view without adding anything
- **THEN** the colony's items, orders, bills and pawn equipment remain unchanged

### Requirement: Alternate materials are explicit bill choices
The feature SHALL provide an alternate-material action for a recipe, showing compatible materials,
their comparison values where known and their shortages. Choosing one SHALL add that material's bill
to the same bench under ordinary planning rules, even when that material is not currently sufficient.
This explicit choice SHALL override the automatic choice for that new bill only; it SHALL not change
global preferences, other saves, existing bills or subsequent automatic choices. Canceling SHALL
leave the bill list unchanged. The final gesture is composed by the integrated browser change;
right-click remains a recorded candidate, not a requirement to guess.

#### Scenario: Choose a cheaper alternate explicitly
- **WHEN** the player chooses a compatible lower-ranked material through the alternate action
- **THEN** the new bill uses that material and subsequent automatic comparisons still follow the selected stat

#### Scenario: Cancel the alternate action
- **WHEN** the player opens the material choices and cancels
- **THEN** no bill or preference is changed

### Requirement: Unsupported comparisons and material locks fail visibly
The feature SHALL distinguish a genuine zero from an unknown, inapplicable or context-dependent value.
Recipes with several distinct outputs or unpredictable results SHALL remain unranked until their
comparison meaning is explicitly supported; their outputs and ordinary bill action SHALL remain
inspectable. A material whose selected value is unknown SHALL not be silently treated as worse than
known candidates: automatic best-material selection SHALL be marked unavailable if that uncertainty
could change the winner. If an output material cannot be enforced without breaking another ingredient
requirement, the material-specific action SHALL explain the limit and offer the ordinary bill workflow
without claiming a material lock. It SHALL not leave a partial or misleading bill behind.

#### Scenario: Foreign stat cannot be evaluated
- **WHEN** a compatible material has an unreadable value that could affect the chosen winner
- **THEN** the view reports an incomplete comparison instead of advertising another material as definitively best
- **AND** explicit supported bill choices and unrelated comparisons remain usable

#### Scenario: Recipe produces two distinct item types
- **WHEN** the outputs have different values for the selected attribute and no ranking rule is supported
- **THEN** the view labels the recipe as having multiple outputs and leaves it unranked
- **AND** it does not sum or average their values into an invented score

#### Scenario: Material restriction cannot be represented safely
- **WHEN** a requested output-material lock would invalidate another ingredient requirement
- **THEN** the player receives an explanation before a material-specific bill is added
- **AND** choosing the ordinary bill workflow preserves its normal filters without promising the displayed material

### Requirement: Refresh and revalidate without changing the player's intent
While the view remains open, the feature SHALL update affected comparisons when stock, skills,
research or recipe availability changes, without requiring a manual refresh. Results awaiting a
refresh SHALL be identified as checking rather than shown as current ready results. Before adding,
the feature SHALL recheck the selected bench, recipe and variant. A stale available-material choice
SHALL prompt review of the refreshed choice instead of silently adding a different material. A failed
add SHALL leave the existing bill list intact. Changing map, bench or save SHALL discard stale context.

#### Scenario: Another job consumes the available material
- **WHEN** the stock supporting a displayed automatic choice is consumed before the add action
- **THEN** the row refreshes and the player can review the new choice or explicitly plan the old material
- **AND** the action does not silently substitute a material

#### Scenario: Bench disappears or recipe becomes unavailable
- **WHEN** the player attempts to add through a stale view after that change
- **THEN** the action reports the change, adds nothing and preserves existing bills

#### Scenario: Load another save
- **WHEN** the player leaves one save and opens a workbench in another
- **THEN** no old-map stock, skill result or in-progress material choice is reused

### Requirement: Scope controls and safe coexistence
The feature SHALL use the common settings row for **Recipe comparisons enabled**, default **on**, in
the Crafting / Add bill section. Global defaults and inherited/per-save overrides SHALL follow the
foundation settings contract, taking effect on the next opening of the view. With no active save,
Use global and the save editor SHALL be disabled while the global control remains editable. In a
loaded save Use global SHALL remain usable; checking it retains a dormant override. Disabling
comparison SHALL preserve ordinary bill access and existing bills.

Search, active sort, Use all materials and the pending alternate choice are contextual browsing
state, not colony policy: they SHALL reset when the Add bill view is freshly opened, with Use all
materials unchecked. Created bill ingredient choices SHALL persist as ordinary bill state. No
Quartermaster settings or window state SHALL be imported automatically. If the original is installed,
this feature SHALL leave its independently supplied window/settings alone. An incompatible optional
integration SHALL disable only its affected comparison/action and log a bounded startup warning with
the missing behavior and fallback, leaving ordinary bill access available.

#### Scenario: Inherited setting and dormant override
- **WHEN** a loaded save switches from a local override to Use global and later switches back
- **THEN** the effective value follows the current global default while inherited
- **AND** the previous local override is restored without changing another save or the global default

#### Scenario: Settings from the main menu
- **WHEN** the player opens settings without an active save
- **THEN** the global default is editable and every save-specific edit path is disabled

#### Scenario: Disable comparison after creating a material-specific bill
- **WHEN** the player disables the feature and reopens Add bill
- **THEN** ordinary bill creation remains available and the earlier bill retains its ingredient choices

#### Scenario: Original provider and changed integration
- **WHEN** Quartermaster is installed and another provider has an unsupported comparison integration
- **THEN** megamod does not remove Quartermaster's tab, impersonate its settings or change its controls
- **AND** the unsupported affected integration reports its fallback while ordinary bill access remains usable
