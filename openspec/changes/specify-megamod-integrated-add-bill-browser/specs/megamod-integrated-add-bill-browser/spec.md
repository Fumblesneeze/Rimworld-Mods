# Integrated Add bill browser

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin: **original**, S012 / P1-09 and the confirmed interview. Proposed observable behavior only.
Compose the proposed [comparison](../../../specify-megamod-quartermaster-comparison/specs/megamod-material-aware-recipe-comparison/spec.md),
[inspection](../../../specify-megamod-recipe-inspection/specs/megamod-recipe-inspection/spec.md) and
[list-control](../../../specify-megamod-recipe-list-controls/specs/megamod-recipe-list-controls/spec.md)
contracts; their material, allocation, skill, persistence and safe-add rules remain authoritative.

## ADDED Requirements

### Requirement: Open one rich browser from the bench's Add bill action
When enabled and supported, the ordinary Add bill action SHALL open a rich browser for that selected
workbench, with search and product-category pills at the top, a stat-sort dropdown beside Use all
materials, a recipe list and contextual details/actions. The bench SHALL remain identifiable.
The browser SHALL retain the bench's ordinary addable recipe/style choices and expose its ordinary
bill route for choices the enhancement cannot represent safely. It SHALL NOT create a separate
global crafting menu, bottom main tab, or replacement bill queue. Opening, browsing and closing
SHALL NOT alter existing bills, create products or issue work.
For a bench in a whole-list workbench group, addable recipes/styles and the ordinary fallback route
SHALL respect that group's common supported catalog and identify the group being edited. Unlinked
benches retain their own ordinary catalog; grouping does not make an unrepresentable comparison a
reason to remove an otherwise supported ordinary bill action.

#### Scenario: Enter from a workbench
- **WHEN** the player selects a bench and chooses Add bill
- **THEN** the browser shows that bench's recipes with the search, category and comparison controls
- **AND** the existing queue retains its order, settings and provider restrictions

#### Scenario: A special recipe cannot be enhanced
- **WHEN** a native or supported provider recipe is addable but cannot use enhanced comparison/details
- **THEN** it remains recognizable and its ordinary working add route is reachable
- **AND** it is not removed solely because it has an unusual result or bill type

#### Scenario: Browse and leave
- **WHEN** the player searches, filters and closes the browser without an add action
- **THEN** no bill, job, item, research state or material reservation changes

#### Scenario: Browse a linked group's common catalog
- **WHEN** the opened bench belongs to a whole-list workbench group with a smaller common recipe catalog
- **THEN** both enhanced Add and ordinary fallback obey that catalog and show the group scope
- **AND** unlinking restores the bench's own ordinary addable choices without changing existing bills

### Requirement: Search matches visible recipe and output names predictably
Search SHALL match the displayed recipe name, style name where present and the names of all known
outputs, ignoring letter case and leading/trailing spaces. Each space-separated search term SHALL
match at least one of those fields; the terms need not occur in the same field. Search SHALL combine
with category and visibility choices without merging distinct recipes or styles. This initial search
SHALL NOT imply matching ingredient names, arbitrary descriptions or internal identifiers. Clearing
search SHALL retain other applicable choices. An empty result SHALL explain that no recipe matches
and offer clearing search/category filters without adding or deleting anything.

#### Scenario: Several terms identify a styled recipe
- **WHEN** one search term matches the recipe's name and another its displayed style name
- **THEN** that choice matches regardless of letter case
- **AND** a choice that matches only one term does not match the complete search

#### Scenario: Match the second output
- **WHEN** a recipe has several outputs and the query matches only the second output's name
- **THEN** the recipe appears with all its outputs still represented in details

#### Scenario: No result and clear filters
- **WHEN** the combined search/category selection has no matches and the player clears those filters
- **THEN** the list returns to the current ordinary or hidden visibility mode's recipes
- **AND** it does not restore hidden records or change a bill

### Requirement: Category pills follow actual products and their hierarchy
Category pills SHALL derive from the currently addable recipes at the opened bench, including their
known products' categories and parent categories, with no meaningless root category. Their catalog
SHALL remain independent of the current text query and ordinary/hidden view so filtering does not
make the available category controls disappear. Categories with identical labels SHALL be
distinguishable by their parent context. Selecting a parent SHALL include descendants; selecting
several categories SHALL include recipes matching any selected category. The category result SHALL
then be combined with search and current visibility. With no category selected, all categories
SHALL be allowed. Recipes without any known product category SHALL remain accessible through All
and an Other recipes pill when such recipes exist. If a selected category disappears from the
bench's available category catalog after a content/availability change, that selection SHALL clear
with an explanation rather than remain as an invisible filter.

#### Scenario: Filter a weapons bench
- **WHEN** the bench offers melee and ranged weapon recipes and the player selects Weapons
- **THEN** both descendants match, while selecting only Melee weapons narrows the result accordingly

#### Scenario: Select two categories or a multi-output recipe
- **WHEN** the player selects two pills and a recipe has at least one output in either category
- **THEN** that recipe appears once per ordinary recipe/style choice
- **AND** it does not need every output to match every selected category

#### Scenario: Same leaf label under different parents
- **WHEN** two offered categories share a label such as Headgear
- **THEN** the controls distinguish their parent contexts rather than silently treating them as one category

#### Scenario: Variable or uncategorized result
- **WHEN** a recipe has no known categorized product
- **THEN** it remains available under All and Other recipes
- **AND** the view does not invent a category from its first ingredient

#### Scenario: A selected category is no longer offered
- **WHEN** an availability change removes the last addable recipe supporting a selected category
- **THEN** that category selection clears with an explanation and the remaining applicable filters persist
- **AND** no invisible category restriction keeps the list empty

### Requirement: Stat choices follow the filtered products with common properties first
The dropdown SHALL begin with Recipe order, meaning the bench's ordinary recipe order without stat
ranking. It SHALL then offer only supported product attributes applicable to the current search,
category and visibility result, with the chosen direction clearly named. The initial common-property
priority SHALL be the order below, omitting irrelevant entries; remaining supported attributes SHALL
follow in alphabetical display-name order with distinct meanings/units kept separate. This order is
an explicit proposed default, not a claim of measured player usage. No aggregate combat score or
unstated pawn outcome SHALL be offered. If no remaining recipe supports the active attribute, the
sort SHALL clear to Recipe order with an explanation, without substituting another attribute.

| Initial common-property order | Direction / meaning |
| --- | --- |
| Blunt armor, sharp armor, heat armor | Highest rating, each separately |
| Cold insulation, heat insulation | Highest protection against the named temperature condition |
| Weapon accuracy: close, short, medium, long | Highest weapon accuracy at the stated range; not final shot hit chance |
| Ranged cooldown | Lowest recovery time after firing; not complete firing-cycle time |
| Nutrition | Highest per-item nutrition, not nutrition per recipe batch or per ingredient cost |
| Medical potency, maximum medical quality | Highest value of each separate medicine property |
| Maximum hit points | Highest undamaged product maximum |
| Mass | Lowest per-item mass |
| Market value | Highest per-item informational value; not sale profit |

#### Scenario: Armor remains after filtering
- **WHEN** the filtered recipes include comparable armor
- **THEN** applicable armor/insulation choices precede the remaining supported general attributes
- **AND** selecting highest blunt armor compares that attribute alone at Normal quality

#### Scenario: Filter changes to meals
- **WHEN** a filter removes all products supporting the active armor sort and leaves meal recipes
- **THEN** the view explains the cleared sort and offers relevant food/general properties
- **AND** an irrelevant armor entry does not remain selected or automatically turn into nutrition

#### Scenario: A provider property shares a label
- **WHEN** two supported properties have the same displayed label but different meanings or units
- **THEN** the menu distinguishes them and never compares one as if it were the other

#### Scenario: Unknown or multiple-output ranking
- **WHEN** a filtered recipe has an unknown value or several distinct outputs without a supported ranking meaning
- **THEN** it remains visibly unranked after known values according to the comparison contract
- **AND** filtering does not hide it merely because ranking is unavailable

### Requirement: Material mode and no-stat planning remain explicit
Use all materials SHALL start unchecked beside the stat dropdown. Under a stat sort it SHALL use
the comparison contract's best sufficient material when unchecked and best compatible material when
checked, without hiding actual shortages. With Recipe order selected, the control SHALL remain
visible but inactive with an explanation that automatic best-material choice requires a stat.
Material-dependent recipes without an explicit material preview SHALL say Any allowed material;
ordinary Add in that state SHALL retain native ingredient filters and promise no specific output
material. Material-dependent values SHALL not pretend to describe a chosen material. The player
SHALL be able to choose a compatible preview material explicitly even without a stat sort.
Material-independent products SHALL remain usable without a material choice or picker.

Outside comparison mode, an inspected recipe without an explicitly chosen preview material SHALL
follow the player's direct changes to the stat sort or Use all materials: its displayed material
and main Add choice SHALL update together to the resulting list choice. Selecting Recipe order
SHALL return that following preview to Any allowed material. While the new automatic choice is
unresolved or absent, the view SHALL state that fact rather than add an unrestricted or old-material
bill as though it were the new comparison result. An explicit preview material SHALL remain chosen
and visibly identified through sort/mode changes. A visible Use list choice action in the single
detail view SHALL clear that override and adopt the current automatic choice, or Any allowed material
under Recipe order. Pinned comparison references and partners SHALL retain their chosen materials
when list controls change; selecting a new partner remains explicit. Background refreshes SHALL
retain the comparison contract's review-before-substitution rule rather than silently changing
the inspected material. The view SHALL distinguish an explicit preview from a choice following the list.

#### Scenario: Fresh browser without a stat
- **WHEN** a material-dependent recipe is inspected before selecting a stat or preview material
- **THEN** Any allowed material is explicit, dependent values await a material choice and ordinary Add
  uses the recipe's native filters
- **AND** the view does not silently advertise or enforce a default material

#### Scenario: Select a stat and include unavailable materials
- **WHEN** the player selects a supported stat, then checks Use all materials
- **THEN** automatic rows change to that stat's best compatible material, including unavailable ones
- **AND** the name and shortages of each shown material remain visible

#### Scenario: No feasible automatic choice
- **WHEN** unchecked mode has no sufficient material for a recipe
- **THEN** the row states that no sufficient material is available
- **AND** explicit alternate-material planning or the clearly labeled ordinary bill route remains reachable

#### Scenario: Inspect one material without sorting
- **WHEN** the player explicitly picks a compatible preview material under Recipe order
- **THEN** the details describe that material at Normal quality and the detail Add action names that choice
- **AND** the rest of the list keeps Recipe order and no global material preference is changed

#### Scenario: Inspect first and then choose a stat
- **WHEN** the player inspects a shirt as Any allowed material and then chooses highest cold insulation
- **THEN** the following preview and main Add both identify the current best material under the checkbox mode
- **AND** using main Add creates that material's bill, not an unrestricted bill from the earlier preview

#### Scenario: Change material mode while following the list
- **WHEN** the player checks and then unchecks Use all materials while inspecting a recipe that follows the list
- **THEN** both its preview and main Add first use the best compatible material and then the best
  sufficient material, or clearly state that no reliable choice exists
- **AND** no unresolved transition silently adds a former material or unrestricted bill

#### Scenario: Preserve and release an explicit preview override
- **WHEN** the player explicitly previews a material, changes the stat or material mode, then uses Use list choice
- **THEN** the explicit preview stays visibly chosen until that action
- **AND** the action adopts the current automatic material, or Any allowed material if Recipe order is selected

#### Scenario: Change the list sort beside a pinned comparison
- **WHEN** two variants are compared and the player changes the list stat or material mode
- **THEN** the list follows the new controls while both compared variants retain their identified materials
- **AND** main Add continues to name the active compared variant until an explicit new selection

### Requirement: Inspection and comparison gestures never queue implicitly
A single primary click on a recipe row SHALL inspect it without adding a bill. A double-click SHALL
also remain inspection, not a hidden add shortcut. A labeled Compare action on the current explicit
recipe/material/style choice SHALL begin comparison with that choice as the reference. While this
mode is visibly active, selecting another choice SHALL choose the comparison partner; selecting a
third SHALL replace only the partner. Removing the partner or leaving comparison SHALL return to
the reference's single detail view. Both choices and the active add choice SHALL remain explicit.
Choosing a different preview material SHALL change inspection only, not an existing bill or the
automatic ranking of other rows. If the reference becomes unavailable or is filtered out, comparison
SHALL end with an explanation, retaining only a still-valid partner as the single inspected choice;
if neither choice remains valid, inspection SHALL clear.

#### Scenario: Double-click a recipe
- **WHEN** the player double-clicks a recipe row
- **THEN** its details appear and no bill is added

#### Scenario: Compare and replace a partner
- **WHEN** the player begins Compare on A, selects B, then selects C while comparison mode is active
- **THEN** A remains the reference, C replaces B as partner and C is the visibly identified active add choice
- **AND** leaving comparison returns to A without adding either choice

#### Scenario: Compare two materials of one recipe
- **WHEN** the player uses an explicit second compatible material as the partner to a recipe reference
- **THEN** both materials are identified and comparable properties follow the inspection contract
- **AND** preview changes do not edit the bill queue or force other rows to use that material

#### Scenario: Filter removes the reference
- **WHEN** a filter removes the reference while its comparison partner still matches
- **THEN** comparison ends with an explanation and the partner remains as the single inspected choice
- **AND** no action targets the removed reference or silently substitutes a new one

### Requirement: Every add action identifies one variant and commits once
The main detail Add action SHALL identify its recipe, style where relevant and explicit material or
Any allowed material state. The optional row Add SHALL use that row's displayed choice, independent
of another inspected reference. Both SHALL use the same ordinary bill-creation rules and action-time
checks. Adding SHALL retain the browser and its applicable filters, show success for the actual
variant, and allow a subsequent deliberate add as a separate bill. Inputs belonging to search,
selection, comparison, hiding or menus SHALL NOT also create a bill. Native bill limits, warnings,
special bill behavior and provider restrictions SHALL remain effective. A failed add SHALL leave
the prior queue intact and explain the result.

#### Scenario: Add the partner once
- **WHEN** A and B are compared, B is the active choice, and the player uses the main Add action
- **THEN** exactly one bill for B is added with its displayed material/style or ordinary unrestricted state
- **AND** A is not added, the success message identifies B and the browser stays open

#### Scenario: Use row Add while inspecting another recipe
- **WHEN** row Add is enabled and the player adds row C while inspecting A
- **THEN** exactly one bill for C's displayed choice is added and A's inspection is unchanged

#### Scenario: Existing limit or changed context rejects the action
- **WHEN** the native bill limit is reached or the bench/recipe/variant is no longer valid
- **THEN** no extra or partial bill is created and the view explains the restriction or changed context

#### Scenario: Add the same recipe twice deliberately
- **WHEN** the first addition succeeds and the player deliberately invokes Add again
- **THEN** a second ordinary bill is created if still permitted
- **AND** this is distinct from one input accidentally triggering two add paths

### Requirement: Alternate-material addition is explicit and discoverable
Every safely supported material-dependent recipe SHALL offer a visible Add with another material
action; right-clicking its row SHALL be a shortcut to that same choice list. Opening or canceling
the list SHALL NOT add a bill. The list SHALL show compatible materials, known selected-stat values
when applicable and shortages, including unavailable materials. Explicitly choosing a material
SHALL add exactly one bill with it under the comparison contract, without changing automatic choices,
the current preview or another bill. Unsupported material locking SHALL explain its limitation and
offer the ordinary route without claiming a lock. Right-click SHALL NOT be the only discoverable
way to reach this action.

#### Scenario: Choose a cheaper material through the visible action
- **WHEN** the player opens Add with another material and selects a cheaper compatible material
- **THEN** exactly one bill uses that material and the automatic ranking/preview remain unchanged

#### Scenario: Right-click then cancel
- **WHEN** the player right-clicks a recipe and cancels the material list
- **THEN** no bill, preview or preference changes

#### Scenario: Unavailable alternate is a plan
- **WHEN** the player chooses a compatible material whose shortage is displayed
- **THEN** ordinary planning can add that material's bill without granting resources or marking it craftable

### Requirement: Compose visibility, availability and refresh without losing intent
The browser SHALL use the list-control capability's explicit hiding scopes/recovery and the comparison
capability's single ingredient-plus-all-skills result. A favorable highlight SHALL mean enough
ingredients for one execution and one current-map eligible colonist meeting all skill requirements,
not reserved supplies or guaranteed work. Search/category/visibility filtering SHALL happen before
the relevant-stat menu is determined. Refreshes while paused or running SHALL expose updating/unknown
states and SHALL NOT silently replace the inspected material or add a bill. If filters remove an
inspected choice, clear that choice with the inspection explanation; if they invalidate the stat,
clear the sort with its explanation. Existing queue configuration SHALL remain untouched.

#### Scenario: Hiding the last relevant recipe
- **WHEN** hiding removes the last recipe supporting the selected stat and the inspected choice
- **THEN** the sort and affected inspection are cleared with readable explanations
- **AND** hidden recovery remains reachable and existing bills remain unchanged

#### Scenario: Supplies change during pause
- **WHEN** stock changes while the browser is open in a paused game
- **THEN** list cues, details and comparison use the same refreshed result, with updating status meanwhile
- **AND** an add against a stale automatic choice requires review instead of substituting another material

#### Scenario: Skills are distributed across colonists
- **WHEN** separate colonists each meet only part of a recipe's required skills
- **THEN** the browser does not combine them into one successful skill result or a craftability highlight

### Requirement: Context and configuration have distinct lifetimes
The feature SHALL provide Enhanced Add bill browser enabled, default **on**, before Recipe comparisons
enabled and the list-control settings in Crafting / Add bill. It SHALL use common global defaults,
Use global and save overrides, taking effect on the next opening. The comparison setting SHALL govern
stat ranking/material optimization and the two-choice comparison mode; turning it off SHALL leave
search, categories, single-recipe details, explicit material planning and native Add usable. A disabled
whole browser SHALL return to the ordinary entry. Main-menu save controls SHALL be inactive and unable
to mutate a save; loaded-game inheritance SHALL retain a dormant override and an editable Use global.

All temporary search, category, sort, Use all materials, selected preview and comparison state SHALL
reset on fresh opening, bench/map/save change or game load. Fresh state SHALL mean empty search,
All categories, Recipe order, Use all materials unchecked and no inspected choice. Information cards
and the alternate-material popup SHALL return to the same still-valid browser context rather than
counting as a fresh opening. Hiding records and created bills SHALL retain their separately specified
persistence. No source settings/window state SHALL be imported.

#### Scenario: Disable only comparisons
- **WHEN** comparison is disabled and the browser is reopened
- **THEN** stat sorting, automatic best-material choice and two-choice comparison are unavailable
- **AND** search, category pills, one-recipe inspection and ordinary/explicit-material addition still work

#### Scenario: Fresh context versus information card
- **WHEN** the player closes an information card within a valid browser, then later closes and reopens Add bill
- **THEN** the card return preserves current choices and the later fresh opening resets browsing state
- **AND** neither action removes persisted hiding choices or changes existing bills

#### Scenario: Global and save configuration
- **WHEN** a loaded save toggles inheritance or the settings are opened at the main menu
- **THEN** common dormant-override behavior applies in the loaded save, while both save-specific
  controls are disabled without an active game and global values remain editable

### Requirement: Responsive access and safe fallback preserve gameplay
The browser SHALL keep its filter, status, detail and add/return actions reachable at supported
resolutions and UI scales using wrapping, scrolling or explicit disclosure. Category overflow and
long translations SHALL NOT conceal the only recovery/add control or create invisible clickable
actions. Status SHALL be understandable without color alone. If the game or an optional provider
cannot safely support the enhanced entry, the affected enhancement SHALL be skipped with a bounded
startup warning naming the missing behavior, reason and fallback. Compatible foreign modifications
SHALL remain allowed; an active incompatible replacement SHALL retain its own working entry.

#### Scenario: Many categories and long names
- **WHEN** many modded product categories and long translated names fill a narrow browser
- **THEN** the player can reach every offered category and read full identities/status explanations
- **AND** Add, clear filters, hidden recovery and return remain usable

#### Scenario: Incompatible entry while native bills still work
- **WHEN** the enhanced entry cannot be admitted safely
- **THEN** the player retains the independently working native/provider Add bill path and existing bills
- **AND** startup logs describe the skipped enhancement rather than silently installing competing views
