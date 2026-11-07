# Recipe inspection

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
This is proposed behavior for later implementation, derived from the selected Nice Bill Tab UI
reference. The material, availability, ranking and add-action rules are owned by the
[material-aware comparison capability](../../../specify-megamod-quartermaster-comparison/specs/megamod-material-aware-recipe-comparison/spec.md).
The original integrated Add bill browser will compose navigation and layout with this capability.

## ADDED Requirements

### Requirement: Inspect recipes in the workbench Add bill view
The enhanced Add bill view SHALL show details for the selected recipe at the opened workbench.
Details include the recipe name, description, output identity and quantities, selected material and
style when applicable, requirements and available product information. They SHALL distinguish
different recipes for the same product. Selecting, comparing or closing details SHALL leave the
bill queue, existing bill configuration, resources and production unchanged.

#### Scenario: Selecting a recipe is inspection
- **WHEN** the player selects a recipe from the opened workbench's Add bill view
- **THEN** its details appear without creating a bill, modifying an existing bill or starting work
- **AND** the workbench and selected recipe remain identifiable

#### Scenario: Alternative recipes remain distinct
- **WHEN** two recipes produce the same item using different quantities or ingredients
- **THEN** selecting each shows that recipe's own requirements and output quantities
- **AND** neither is collapsed into an interchangeable product entry

#### Scenario: Closing a preview preserves the queue
- **WHEN** the player changes material previews and then closes Add bill without adding anything
- **THEN** every existing bill retains its order, repetition rules, material filter and other settings

### Requirement: Explain outputs without inventing a predictable result
The detail view SHALL show each declared output and quantity for one recipe execution. A batch of
one item type SHALL be distinguished from several output types. Variable or input-dependent results
SHALL be labeled as such; unknown quantities SHALL not be shown as zero or a guaranteed yield.
Recipes without a single predictable product SHALL remain inspectable and accessible through their
ordinary bill action even when stat ranking is unavailable.

#### Scenario: Batch and multiple outputs
- **WHEN** one recipe makes four meals and another makes two distinct products
- **THEN** the first identifies four meals per execution and the second lists both products and their quantities
- **AND** the second is not presented as a single combined product score

#### Scenario: Input-dependent processing
- **WHEN** the player inspects a butchering or recycling recipe whose result depends on the input
- **THEN** details explain that dependency and show known result categories or information where available
- **AND** the view does not promise a fixed yield, fabricate a comparison score or prevent ordinary bill creation

#### Scenario: Missing presentation data
- **WHEN** a recipe lacks a usable product picture or description
- **THEN** its readable name and available requirements remain accessible with a neutral picture or absent description
- **AND** one missing presentation field does not remove the recipe or other details

### Requirement: Present ingredient requirements for one execution
The view SHALL show every ingredient requirement, the acceptable alternatives and their required
quantities in the recipe's own units. Alternatives within a requirement SHALL not be displayed as
additional simultaneous costs. A chosen output material SHALL narrow only the corresponding material
requirement; other ingredients remain visible. Supply status SHALL use the same one-execution,
finite-stock allocation as material-aware comparison, with an explicit unknown or updating state
when a reliable result is unavailable.

#### Scenario: Different ingredients have different equivalent amounts
- **WHEN** a requirement accepts alternatives whose required counts differ
- **THEN** inspecting the alternatives shows each count or the applicable common quantity unit
- **AND** an example icon does not imply that its item is the only accepted ingredient

#### Scenario: Choosing material preserves other costs
- **WHEN** a recipe needs a selected metal plus an independent component or other required ingredient
- **THEN** choosing the metal updates that requirement and leaves the independent cost visible
- **AND** neither previewing nor adding the bill silently removes that cost

#### Scenario: Overlapping ingredient choices
- **WHEN** the same available stack could satisfy two separate ingredient requirements but cannot cover both
- **THEN** the detail view reports the shortage consistently with the recipe's craftability highlight
- **AND** it does not count that stack twice or reserve it merely by displaying the requirements

#### Scenario: Availability is not known yet
- **WHEN** supplies changed or a recipe's ingredient rules cannot yet be evaluated reliably
- **THEN** the affected requirements show updating or unknown status rather than a stale sufficient result
- **AND** the ordinary recipe action remains available subject to its normal rules

### Requirement: Separate work and skill requirements from job readiness
The view SHALL show the recipe's stated work amount and every required skill and minimum level.
Work amount SHALL not be presented as a guaranteed completion time. The skilled-worker indication
SHALL require one living free colonist on the current map to meet all listed requirements, as in
material-aware comparison. Ingredients, skills and any additional known native prerequisite SHALL
remain distinguishable; a favorable display does not promise that work can begin immediately.

#### Scenario: Skills are split between colonists
- **WHEN** one colonist meets the first required skill and another meets only the second
- **THEN** all skill requirements remain listed and the view reports that no single colonist meets them all
- **AND** their separate skills are not combined into a favorable craftability result

#### Scenario: Requirements do not imply immediate work
- **WHEN** supplies and skills are sufficient but the workbench lacks power or an eligible worker cannot currently reach it
- **THEN** the view does not describe the job as already running or guaranteed to start
- **AND** ordinary job eligibility and planning warnings still apply

#### Scenario: No skill requirement or variable work
- **WHEN** a recipe has no skill minimum or its work cannot be expressed as a fixed amount
- **THEN** the relevant absence or uncertainty is stated without inventing a required skill or completion time

### Requirement: Show meaningful product facts with their basis
Product details SHALL use Normal quality and the currently displayed material, consistent with
material-aware comparison. Known applicable facts include ordinary product statistics, apparel
layers and coverage, declared equipment bonuses, and ordinary item information. Values SHALL retain
their labels, units, sign and distinction between zero, not applicable and unknown. Any market value
shown SHALL be labeled per item and shall not promise a trader's price or profit. Effects requiring
a particular pawn, target, ammunition or other unchosen context SHALL not be presented as an actual
outcome for the colony.

#### Scenario: Material changes armor but not a quality roll
- **WHEN** the player previews different materials for armor
- **THEN** supported armor values update for the shown material at Normal quality
- **AND** no quality selector appears and actual production quality remains governed by the game

#### Scenario: Facts have different meanings
- **WHEN** the selected item has mass, armor, an equipment bonus or apparel coverage information
- **THEN** each is labeled with its own meaning and unit or covered parts/layers
- **AND** an equipment bonus is not combined into an unlabeled prediction of a pawn's final ability

#### Scenario: Zero is a known value
- **WHEN** an applicable property has value zero, a negative value, or cannot be evaluated
- **THEN** zero and negative values remain recognizable as values and an unevaluable property is marked unknown
- **AND** the display does not silently substitute another material to obtain a number

#### Scenario: Conditional combat or bodily effects
- **WHEN** damage, implant effects or other results depend on an unchosen actor or combat context
- **THEN** the view shows available item facts and ordinary information-card access
- **AND** it does not invent effective damage, guaranteed safety, protection or a final pawn result

#### Scenario: Market value of a batch
- **WHEN** a recipe produces several units and a product's market value is displayed
- **THEN** the per-item value is clearly distinguished from the batch quantity
- **AND** no materials-plus-work heuristic is presented as a guaranteed sale value or profit

### Requirement: Compare two explicitly chosen recipe variants
The player SHALL be able to select an explicit comparison between at most two recipe/material/style
choices from the same workbench's current Add bill choices. Both identities and their Normal-quality
basis SHALL remain visible. Shared properties SHALL compare only like meanings and units, following
the direction used by material-aware ranking. A numeric value and a readable difference or
better/equal/worse indication SHALL accompany any color cue. Unmatched or unknown properties SHALL
remain separate rather than becoming zero-valued opponents.

#### Scenario: Compare two material variants
- **WHEN** the player explicitly compares the same apparel recipe in two materials
- **THEN** the view identifies both materials and compares the same armor properties at Normal quality
- **AND** browsing does not alter an existing apparel bill

#### Scenario: Different properties cannot be paired
- **WHEN** one item has medical potency and another has a different medical limit, or a provider uses different units for a similarly named property
- **THEN** the view does not compare them as if they were the same statistic
- **AND** unknown or unshared facts remain individually identifiable

#### Scenario: A third choice replaces the comparison partner
- **WHEN** the player chooses a third variant while comparing a reference and a partner
- **THEN** the reference remains and the chosen variant replaces the partner
- **AND** the player can remove the partner to return to a single detail view without creating an order

#### Scenario: Comparison does not choose a bill implicitly
- **WHEN** the player uses Add while two variants are displayed
- **THEN** the action identifies which variant will be added and applies the existing material-aware bill-creation rules exactly once
- **AND** the other variant is not added or used to modify an existing bill

### Requirement: Inspection actions have limited explicit effects
The player SHALL be able to open ordinary recipe and item information from details. For a named
ingredient, an explicit Find on map action SHALL navigate only to a currently visible, selectable
instance on the same map, or report that none is available. Inspection SHALL not queue ingredient
production on another bench, change bills, issue jobs or reveal hidden objects. Opening an information
card SHALL preserve the current Add bill selection when the card closes.

#### Scenario: Open product or ingredient information
- **WHEN** the player requests information for a recipe, output or named ingredient
- **THEN** the corresponding ordinary information card opens
- **AND** closing it returns to the same recipe and comparison choices without making an order

#### Scenario: Navigate to an existing ingredient
- **WHEN** the player explicitly uses Find on map and a visible selectable instance still exists on this map
- **THEN** the camera and selection move to that instance using ordinary game navigation
- **AND** no item is hauled, reserved, unforbidden, consumed or produced by the navigation action

#### Scenario: The ingredient is absent or hidden
- **WHEN** no visible selectable instance exists, or the selected candidate disappeared before navigation
- **THEN** the action reports that no instance is available and preserves the current map and camera
- **AND** it does not search another map, reveal fogged contents or create a production bill

### Requirement: Keep inspection state local and fresh
Recipe and comparison choices SHALL be temporary browsing state, not global preferences or saved
bill configuration. Closing Add bill, changing workbench or map, and switching or loading saves SHALL
clear those choices. A recipe removed from the current available or filtered choices SHALL leave
the comparison with an explanation. Supply and skill changes SHALL update the displayed status while
paused as well as while time is running; refreshing details SHALL not change the player's chosen
material silently.

#### Scenario: Move to another context
- **WHEN** the player closes Add bill, selects another workbench or map, or loads another save
- **THEN** a subsequently opened view starts without the previous recipe or comparison reference
- **AND** no action can target the former workbench through a retained preview

#### Scenario: The selected choice is removed
- **WHEN** research availability, provider rules or the current filter removes a displayed recipe choice
- **THEN** its preview or comparison entry is cleared with a readable explanation
- **AND** it is not quietly replaced by another recipe with the same product

#### Scenario: Stock changes while paused
- **WHEN** supplies or colonist skills change while the Add bill view remains open in a paused game
- **THEN** its status refreshes consistently with material-aware comparison, using updating status until resolved
- **AND** the chosen material is not silently exchanged for one that now ranks better

### Requirement: Fit the contextual workflow and preserve ordinary controls
The detail and comparison view SHALL belong to Add bill at the selected workbench, with no new
bottom tab, separate global crafting window or required debug controls. Long text and lists SHALL
remain reachable through wrapping or scrolling at supported interface scales. Recipe and comparison
choices SHALL use contextual controls; this slice introduces no independent setting or exception to
the common global-default/save-override settings model. Ordinary queue controls and provider-owned
bill restrictions SHALL remain effective when entering and leaving the view.

#### Scenario: Long localized details
- **WHEN** the player inspects a recipe with long translated names and many ingredient alternatives
- **THEN** the complete labels, quantities, information actions and Add or close actions remain reachable
- **AND** color alone is not required to understand shortages or comparisons

#### Scenario: Existing bills remain under their usual control
- **WHEN** the player opens and closes recipe inspection beside ordinary or externally managed bills
- **THEN** existing bills retain their status and restrictions
- **AND** inspection offers no shortcut that edits, deletes, suspends or changes the materials of a protected bill

### Requirement: Degrade visibly when enhanced inspection cannot coexist
The affected enhancement SHALL be skipped when required game behavior or an optional integration is
incompatible, with a startup warning identifying the unavailable feature and reason. Compatible
changes by other mods SHALL not alone disable it. Missing optional providers SHALL not prevent ordinary
inspection. A failed stat provider SHALL affect only its facts when those can be isolated safely;
an incompatible replacement of the Add bill workflow SHALL retain that workflow without layering a
second competing view over it. No upstream settings, saved selections or package identity SHALL be
silently imported or impersonated.

#### Scenario: A compatible other patch is present
- **WHEN** another mod changes an unrelated part of the bill workflow while the required behavior remains usable
- **THEN** enhanced inspection remains available and ordinary bill actions still work

#### Scenario: An optional fact provider changed
- **WHEN** an optional provider cannot supply a supported statistic safely
- **THEN** that fact is unavailable with an explanation and the failure is logged once for the affected integration
- **AND** other recipe details and ordinary bill access remain usable when independently safe

#### Scenario: Another bill view owns the interaction
- **WHEN** an active replacement such as Nice Bill Tab prevents the required Add bill interaction from being used safely
- **THEN** megamod leaves the existing view available, skips its conflicting enhancement and logs the reason at startup
- **AND** it neither removes the other mod nor applies two competing versions of the interaction

#### Scenario: Remove the enhancement
- **WHEN** the enhancement is disabled or unavailable on a later load
- **THEN** ordinary saved bills remain accessible with their existing configuration
- **AND** no Nice Bill Tab settings or selection state is migrated automatically
