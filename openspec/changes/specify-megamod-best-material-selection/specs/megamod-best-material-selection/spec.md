## ADDED Requirements

### Requirement: Each supported bill has an independent material preference
Make Rimworld Great Again SHALL offer Use best by in the contextual configuration of a production
bill whose recipe makes one unambiguous material-dependent output kind through non-mixing ingredient
selection. Producing several units of that kind does not itself make the recipe unsupported. The
control SHALL start off, retain its selected criterion when turned off, and clearly distinguish an
enabled preference from an unavailable one. Different unlinked bills SHALL retain independent
choices even when their recipes produce the same item. These are saved bill choices, not mod
settings, and SHALL add no bottom tab or general behavior-toggle button.

Recipes without a material-dependent output, with ambiguous different outputs, or whose material
selection cannot support this preference SHALL retain ordinary crafting and filters. When the
control is shown for such a recipe, it SHALL be unavailable with a reason; it SHALL never promise
automatic selection that cannot run. Medical orders, butchering and mixing recipes are outside this
automatic preference. Viewing their bill configuration SHALL remain usable.

#### Scenario: Two bills for the same garment
- **WHEN** the player enables Armor on one garment bill and Insulation on another unlinked bill for that same garment
- **THEN** each retains its own enabled choice, including after reopening either bill or reloading the save.

#### Scenario: Disable without forgetting
- **WHEN** the player disables a bill's chosen criterion and later enables it again
- **THEN** the criterion remains selected and ordinary selection applied while it was disabled.

#### Scenario: Unsupported recipe remains usable
- **WHEN** the player opens a mixing, butchering or ambiguous multiple-output recipe
- **THEN** ordinary bill controls remain usable and no enabled best-material control claims to affect it.

### Requirement: Comparisons have visible meanings and source-derived defaults
The criterion menu SHALL offer supported material-sensitive product properties with clear units and
direction. Product comparisons SHALL use the Normal-quality reference already used by the bill
browser, without predicting or changing the worker's actual crafted quality. Single-property choices
SHALL prefer higher values except Work to make and Flammability, which SHALL prefer lower values.
Directions SHALL remain explicit even where another view uses a different useful direction, such as
the browser's lowest-mass comparison. Identically labeled properties SHALL remain distinguishable.

The following named combinations SHALL use their stated raw game values, without hidden
normalization. They are convenience comparisons, not promises of damage, survival or temperature
comfort. Their explanations SHALL display components and weights. They SHALL be offered only where
their component meanings are supported; an unavailable component SHALL not be treated as zero.

| Choice | Comparison, highest first | Initial bill choice |
| --- | --- | --- |
| Average melee damage per second | Product's ordinary average melee DPS | Melee weapon |
| Attack strength | Average melee DPS + 4 × average melee armor penetration | Additional melee choice |
| Armor | 2 × sharp armor + blunt armor + 0.2 × heat armor | Apparel |
| Insulation | Cold insulation + heat insulation | Additional apparel choice |
| Other relevant product properties | Named single property and visible direction | First alphabetically displayed supported single property for other products, or when the usual initial choice is unavailable |

The feature SHALL expose one single-property or named combined criterion at a time. Custom weights
and an ordered multi-criterion editor are not included. These bill choices SHALL not add weighted
combat scores to the integrated Add bill browser's separate single-property sort menu.

#### Scenario: Weighted armor is explained
- **WHEN** the player inspects Armor for two supported materials
- **THEN** the explanation identifies sharp, blunt and heat components with weights 2, 1 and 0.2
- **AND** the greater resulting comparison value ranks first without changing actual armor stats.

#### Scenario: Lower work requirement
- **WHEN** Work to make is selected and eligible materials require different work for the same product
- **THEN** the lower-work material ranks first and the direction is visibly stated.

#### Scenario: Comparison quality is not a crafting result
- **WHEN** a skilled crafter completes a bill ranked using Normal reference values
- **THEN** actual quality still follows ordinary crafting rules rather than being forced to Normal.

#### Scenario: Unsupported weighted component
- **WHEN** an optional product cannot meaningfully supply a required combined-score component
- **THEN** that combined choice is unavailable with a reason and supported individual properties remain usable.

### Requirement: Preference selects eligible sufficient material within ordinary search scope
For a fresh supported iteration with Use best by enabled, the bill SHALL prefer the highest-ranked
eligible output material among supplies discovered by the ordinary ingredient search. It SHALL
retain that search's physical reach, bill radius and stopping scope, including the bounded linked-
storage addition when Common Sense perishable-ingredient preference is enabled: once the search
discovers an eligible perishable ingredient in a linked storage group, eligible contents throughout
that group SHALL participate before selecting material, even when the initially discovered stock
is already sufficient. The bill's applicable radius and current physical access still constrain all
members' contents. This does not promise the best material everywhere on the map or expand an
unrelated storage group. Several eligible stacks of the same material can jointly supply
the required amount. A higher-ranked material insufficient for the ordinary requirement SHALL not
block a lower-ranked sufficient permitted material. It SHALL not mix otherwise non-mixable materials
or invent supplies to reach the higher score. If no permitted complete selection is possible, the
bill SHALL wait for ingredients through its ordinary workflow.

Recipe-specific quantity rules, fixed ingredients, bill filters and explicit material locks,
forbidden state, access, reservations and provider restrictions SHALL remain authoritative. Other
ingredient slots SHALL not be ranked as if they determined the output's material. The feature SHALL
not consume the same units twice across ingredient requirements. The chosen actual ingredients
SHALL determine product material through ordinary crafting. Work, efficiency, output quantities,
research and worker requirements SHALL remain unchanged.

#### Scenario: Preferred material is short
- **WHEN** the search finds an insufficient amount of the highest-ranked material and enough of a lower-ranked allowed one
- **THEN** the bill can use the lower-ranked material without mixing the two or increasing their counts.

#### Scenario: Enough material split across stacks
- **WHEN** two reachable permitted stacks of one preferred material jointly meet the recipe requirement
- **THEN** their actual units can satisfy the requirement without requiring a single large stack.

#### Scenario: Better material is inaccessible or undiscovered
- **WHEN** a better material is forbidden, reserved elsewhere, outside the radius, or beyond both the ordinary search and any enabled linked-storage addition
- **THEN** its existence does not guarantee its selection or make it usable
- **AND** the control's help explains the ordinary-search scope.

#### Scenario: Better material elsewhere in the discovered linked group
- **WHEN** Common Sense perishable-ingredient preference is enabled and the search finds enough lower-ranked perishable material in one linked shelf while another member holds enough higher-ranked permitted material within the bill's radius and access
- **THEN** the higher-ranked material participates and wins even if the first shelf alone would satisfy ordinary search
- **AND** an inaccessible or out-of-radius group member supplies no usable material.

#### Scenario: Fixed and variable ingredients coexist
- **WHEN** a recipe requires a fixed component plus a selectable output material
- **THEN** the fixed component follows ordinary eligibility and quantity rules, and only the output-material choice receives this preference
- **AND** each physical unit is consumed at most once.

### Requirement: Explicit choices and perishable supplies compose in a known order
An explicit material lock SHALL take precedence over automatic preference. Changing Use best by
SHALL not broaden filters or substitute for a material selected when adding the bill. For a fresh
output-material selection, the enabled criterion SHALL precede an explicit Best Mix ingredient
preference, which in turn precedes the Common Sense perishable-supply preference. Best Mix Default
contributes no extra criterion; its explicit Nearest or Random acts only within the higher-priority
material constraints. Among equally preferred eligible output materials and their stacks after any
explicit ingredient criterion, enabled perishable
preference SHALL choose perishable before nonperishable, then smaller whole remaining rot-start
hours, followed by shorter straight-line horizontal distance to the work location. Without
perishable preference, equal scores SHALL use that distance directly. Exact remaining ties SHALL
retain ordinary discovery order. Other ingredient slots SHALL skip output-material comparison but
retain an explicit Best Mix criterion before ordinary/Common Sense ties. Default/inactive ingredient
preferences preserve their ordinary ordering, including native medical precedence where applicable.
Surgery SHALL remain unaffected.

#### Scenario: Locked material loses the score comparison
- **WHEN** a bill added with an explicit material has Use best by enabled and another material would score higher
- **THEN** the bill still uses only the locked material and waits if it is unavailable.

#### Scenario: Better material versus older stock
- **WHEN** two permitted output materials have different selected scores, the lower-ranked one will spoil sooner, and no explicit Best Mix criterion distinguishes the higher-score ties
- **THEN** the higher-ranked sufficient material wins this bill's output-material choice
- **AND** equally ranked supplies still use the enabled spoilage preference before distance.

#### Scenario: Secondary ingredient has no output-material role
- **WHEN** an eligible secondary ingredient spoils sooner than another, Common Sense preference is enabled, and no explicit Best Mix criterion decides between them
- **THEN** its ordinary perishable ordering remains effective independently of the output-material criterion.

#### Scenario: Explicit ingredient preference follows output choice
- **WHEN** a bill prefers a higher-stat output material while Best Mix Cheapest would favor another allowed material
- **THEN** the sufficient higher-stat material wins, and Cheapest orders remaining material ties and secondary ingredients before spoilage.

### Requirement: Ingredient display sorting does not alter production choices
The bill ingredient view SHALL offer a separate display-sort control, initially Default, alongside
supported single-property and named combined comparisons. It SHALL sort material rows within their
existing categories, preserve category membership, checkboxes, search and scroll access, and retain
ordinary order for equal values. Non-material or unevaluable rows SHALL remain visible in ordinary
relative order after ranked rows, with no fabricated comparison value. Returning to Default SHALL
restore ordinary ordering. Display sorting SHALL never change Use best by, ingredient permissions,
the recipe's output material or another open bill's view. Closing and reopening the view SHALL reset
this display-only choice to Default; saved crafting preference SHALL remain unchanged.

#### Scenario: Sort without enabling preference
- **WHEN** the player sorts the ingredient list by Armor on a bill with automatic preference off
- **THEN** the list changes order while crafting preference and ingredient checkboxes stay unchanged.

#### Scenario: Restore default and reopen
- **WHEN** the player restores Default or closes and reopens the bill's ingredient view
- **THEN** ordinary display order returns without resetting the bill's saved crafting criterion.

#### Scenario: A non-material component shares the view
- **WHEN** the ingredient list includes a component without a meaningful product-material comparison
- **THEN** it stays visible and selectable according to its ordinary rules and is not given a fake zero score.

### Requirement: Material information remains local accurate and readable
Hovering a material in the supported bill ingredient view SHALL preserve its ordinary description
and add the relevant material properties used by that product, distinguishing raw properties,
multipliers and additions with appropriate units. Same-name properties with different meanings
SHALL not collapse into one ambiguous entry. Normal-reference product comparisons and raw material
modifiers SHALL be labeled distinctly. Material information SHALL remain available without enabling
automatic preference. Unavailable data SHALL have an explanation instead of a plausible invented
value. Other windows and other bills SHALL not inherit stale descriptions, sorting or controls.

#### Scenario: Compare a material without changing the bill
- **WHEN** the player hovers an allowed material while Use best by is off
- **THEN** relevant factors and offsets appear with the ordinary description and no bill choice changes.

#### Scenario: Leave the bill window
- **WHEN** the player closes the bill window and opens an unrelated storage or information view
- **THEN** that view does not acquire the previous bill's product-specific sorting or descriptions.

### Requirement: Bill snapshots and linked edits preserve the complete choice
Duplicate, copy/paste and whole-list linking SHALL include activation and the selected criterion,
including dormant selections, as part of the bill's complete editable state. Copies SHALL be
independent; linked members SHALL intentionally share their one logical bill's choice. An off or
absent preference copied onto a previously active destination SHALL not leave old activation behind.
If a consequential choice cannot be represented at the destination, paste/link SHALL reject the
whole operation with a reason and preserve both prior states, following the workbench-management
contract. Preview/cancel SHALL not mutate either bill. Display sort SHALL not be copied as gameplay state.

Changes SHALL govern new iterations. A started iteration, including ingredient collection and
unfinished work, SHALL retain its original material choice through linked edits, cleaning, batch
trips, interruption and save/load, subject to current access and reservations. If committed supplies
become unusable, it SHALL use the existing interrupted-work recovery rather than silently switching
materials or duplicating work. Hauling route order SHALL not change the selected output material.

#### Scenario: Copy inactive preference
- **WHEN** an off bill with a retained Insulation choice is pasted over an active Armor bill
- **THEN** the destination becomes off with Insulation retained, and later edits to either unlinked bill do not alter the other.

#### Scenario: Incompatible paste
- **WHEN** the destination cannot represent a copied active criterion
- **THEN** the complete paste is rejected with the reason and all prior bill choices remain intact.

#### Scenario: Linked edit while ingredients are being carried
- **WHEN** a linked bill's criterion changes while a worker is collecting the old iteration's material
- **THEN** every member displays the new choice for new iterations, while that started iteration retains its original material through cleanup, additional trips and reload.

#### Scenario: Shorter collection route
- **WHEN** batch collection reaches a secondary ingredient before the selected primary material
- **THEN** the actual finished product still uses the selected primary material with unchanged costs and output count.

### Requirement: Unavailable choices and compatibility failures preserve player intent
Owned bill choices SHALL persist independently through saving, loading, game switching and temporary
content/provider absence, without requiring a window to have been opened. An active choice whose
property or product meaning disappears SHALL remain recorded and visibly unavailable. New iterations
using that unavailable automatic choice SHALL wait with a reason until the player repairs or disables
it; the game SHALL not silently pick a different criterion. Disabling it SHALL allow ordinary
selection under the existing filters. Already made items SHALL remain untouched.

When the original Use Best Materials provider is active, owned overlapping selection and UI SHALL
be withheld with a bounded startup warning and a clear indication that the original provider governs
that behavior. Owned preferences SHALL remain stored; the source is not promised to honor them.
Other incompatible integrations SHALL disable only the affected behavior, with a startup warning
identifying the skipped function and fallback. An enabled owned automatic preference that cannot
be honored SHALL not silently consume a different material: new affected work waits, or requires
the player to disable that preference before ordinary work resumes. Bills with no owned automatic
preference SHALL retain ordinary selection. Benign other mods SHALL not alone cause rejection.

No automatic import of original configuration/save data, original package impersonation, or safe
removal guarantee is included. Optional alternate bill windows and copy tools SHALL show/copy the
same owned choice only when supported; unsupported integration SHALL leave native bill controls
usable and explain the missing enhancement.

#### Scenario: Selected property disappears
- **WHEN** an active saved choice loses its supporting content
- **THEN** the choice remains visible as unavailable and new affected work waits without losing its stored identity
- **AND** disabling it restores ordinary selection with the existing ingredient filters.

#### Scenario: Save switching with identical recipes
- **WHEN** the player loads another save containing the same recipe with a different criterion
- **THEN** the new save's own bill choices apply without inheriting a comparator or activation from the previous save.

#### Scenario: Original provider co-installed
- **WHEN** the original provider is active at startup
- **THEN** the log identifies the withheld owned overlap, the UI identifies original-provider control, and owned preferences survive a subsequent save/load without claiming that the source applies them.

#### Scenario: Material-selection enhancement is incompatible
- **WHEN** the required selection behavior is unavailable while an owned automatic preference is enabled
- **THEN** the startup warning identifies the unavailable behavior and affected new work does not silently use another material
- **AND** the player can disable the preference and continue ordinary crafting under the unchanged filter.
