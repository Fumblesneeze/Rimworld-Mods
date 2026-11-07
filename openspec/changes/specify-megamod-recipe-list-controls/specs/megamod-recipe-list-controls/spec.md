# Recipe list controls

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Origin: selected and adapted Nice Bill Tab - Expansion interactions, S011. Proposed behavior only.
Compose with the [material comparison](../../../specify-megamod-quartermaster-comparison/specs/megamod-material-aware-recipe-comparison/spec.md)
and [inspection](../../../specify-megamod-recipe-inspection/specs/megamod-recipe-inspection/spec.md)
contracts. The original Add bill browser change owns final search, category, stat-menu and gesture
composition. Source-derived choices below are explicit draft assumptions, not implementation evidence.

## ADDED Requirements

### Requirement: Recipe hiding has an explicit gameplay scope
The Add bill view SHALL offer hiding a recipe at this workbench, at all workbenches of this type in
the current save, or at all workbenches in the current save. The scope SHALL be visible before the
action; this workbench SHALL be the initially offered scope. A hidden recipe includes all its style
and material variants, but not other recipes that happen to produce the same item. All applicable
hiding records SHALL apply together; choosing a scope for a later action SHALL NOT disable previous
records. Hiding SHALL affect browsing only, not research, recipe availability to jobs or existing bills.

#### Scenario: Hide one recipe at one bench
- **WHEN** the player hides a recipe at this workbench and opens another bench of the same type
- **THEN** the recipe is absent from the first bench's ordinary Add bill list but present at the second
- **AND** existing bills at both benches retain their order, configuration and ability to run

#### Scenario: Hide a type or save-wide recipe
- **WHEN** the player hides a recipe for all benches of this type or all benches in this save
- **THEN** every applicable bench on every map in that save, including a later-built bench, honors it
- **AND** another save's recipe lists are unchanged

#### Scenario: Combine scopes without conflating recipes
- **WHEN** a recipe has both bench and save-wide hiding records and the player chooses type scope
  for a different hiding action
- **THEN** the original recipe remains hidden wherever either of its records applies
- **AND** a distinct recipe with the same output stays visible unless separately hidden

### Requirement: Every active hiding choice remains recoverable
The view SHALL provide a clearly labeled hidden-only list and ordinary-list return action whenever
an available recipe is hidden. The hidden list SHALL identify the applicable hiding scopes and offer
removal of each record with its full effect stated, including effects on other benches. Removing one
record SHALL NOT silently remove others. Search and category filters SHALL remain usable in this
view, with a reachable clear-filters action even when no row matches. Hidden rows SHALL still allow
inspection and explicit bill addition under the ordinary creation rules; hiding is not a prohibition.

#### Scenario: Restore a recipe hidden at multiple scopes
- **WHEN** a recipe is hidden both here and save-wide and the player removes its bench-only record
- **THEN** it remains hidden in the ordinary list and the hidden list still explains the save-wide record
- **AND** removing that remaining record explicitly restores it at all affected benches without
  changing their existing bills

#### Scenario: Search conceals the hidden rows
- **WHEN** a search or category filter matches none of the hidden recipes
- **THEN** the hidden view states that no recipes match and keeps clear-filters and return actions reachable
- **AND** clearing those filters reveals the hidden recipes available at this workbench

#### Scenario: Add a hidden recipe deliberately
- **WHEN** the player explicitly adds a hidden recipe using a visible recipe, style and material choice
- **THEN** one ordinary bill is added under the same rules as a visible recipe
- **AND** the recipe remains hidden until its hiding record is explicitly removed

#### Scenario: Restore the last hidden recipe
- **WHEN** the last currently available hidden recipe is restored
- **THEN** the view returns to the ordinary recipe list without retaining an empty hidden-only mode

### Requirement: Visibility survives ordinary saves without leaking browsing state
Hiding records SHALL persist in their stated save and bench scopes across save/load. The same
workbench moved or minified and reinstalled SHALL retain its bench-specific records; a newly built
replacement SHALL inherit only applicable type/save-wide records. Missing recipes SHALL NOT hide
unrelated entries or break Add bill; their records SHALL remain dormant if that recipe later returns.
Closing Add bill or changing bench, map or save SHALL clear temporary hidden-view mode. Hiding
SHALL NOT automatically import settings or saved choices from the source mods.

#### Scenario: Reload and then change save
- **WHEN** the player reloads a save with hidden recipes and subsequently opens a different save
- **THEN** the first save's records survive reload while the second uses only its own records
- **AND** each newly opened Add bill view starts in ordinary-list mode

#### Scenario: Move versus rebuild a bench
- **WHEN** the same hidden-configured bench is reinstalled and a separate replacement bench is built
- **THEN** the reinstalled bench keeps its own hidden recipes
- **AND** the replacement uses type/save-wide records without copying the old bench's local choices

#### Scenario: Content temporarily disappears
- **WHEN** a previously hidden recipe is absent and later returns with the same recipe identity
- **THEN** other recipes remain usable during its absence and its applicable hiding choice resumes on return

### Requirement: Row addition uses the displayed choice once
When enabled, a clearly identified row Add action SHALL create exactly one bill at the opened
workbench for that row's displayed recipe, style and material, using the material-comparison
contract's action-time checks and native warnings. It SHALL NOT switch to an unspecified default
material, alter an existing bill or select a different row. The same input SHALL NOT also invoke a
row selection or second add action. Merely hovering, inspecting, hiding or changing filters SHALL
never add a bill. If the exact choice cannot be added safely, the view SHALL explain the failure and
leave the queue unchanged, retaining ordinary bill access where independently safe.

#### Scenario: Quick-add the ranked material
- **WHEN** the player clicks Add on a row showing a particular style and best material
- **THEN** exactly one bill uses that recipe, style and material under the ordinary creation rules
- **AND** any existing inspection comparison remains unchanged unless the underlying choice became invalid

#### Scenario: The choice becomes stale
- **WHEN** a recipe, style or material is no longer a valid choice when Add is clicked
- **THEN** the view explains the unavailable choice without substituting another one or adding a partial bill

#### Scenario: Separate row actions
- **WHEN** the player uses a row's hide or inspection action rather than its Add action
- **THEN** the queue does not change and that input performs only its stated action

### Requirement: Ingredient cues explain shortages without inventing sufficiency
When shortage cues are enabled, the list and details SHALL identify missing ingredient requirements
for one execution of the displayed choice using the same finite ingredient allocation as material
comparison. Each mark SHALL have a readable explanation; color or an unexplained slash alone is
insufficient. An unresolved or updating result SHALL be visibly distinct from enough ingredients.
Overlapping ingredient alternatives SHALL NOT count the same supply twice or mark an unrelated
requirement solely because it accepts the same item type. Skill failures SHALL be identified
separately from ingredient shortages; neither cue SHALL claim that a job can currently run.

#### Scenario: Two requirements compete for one stack
- **WHEN** two ingredient requirements could each use a stack but there is too little for both together
- **THEN** the choice shows an ingredient shortage consistent with allocating that stock only once
- **AND** the details identify the unsatisfied requirement from that allocation

#### Scenario: Data is not yet known
- **WHEN** the recipe's supported ingredient result is still being calculated or cannot be established
- **THEN** the list shows updating or unknown information rather than implying sufficiency by an absent mark
- **AND** native planning and information remain accessible where safe

#### Scenario: Ingredients are sufficient but skills are not
- **WHEN** ingredients suffice but no one eligible colonist meets all required skills
- **THEN** ingredient cues show no shortage, the skill restriction remains visible and the recipe does
  not receive the combined craftability highlight

#### Scenario: Plan with an unavailable material
- **WHEN** Use all materials selects a material with insufficient stock
- **THEN** its shortage remains visible even though the player can plan a bill with that material

### Requirement: Browsing updates without changing the player's intent
Visible ingredient and recipe information SHALL react to relevant changes while the game is paused
as well as running, displaying updating status while it is not current. Hiding, restoring or changing
visibility mode SHALL preserve the current search, still-applicable category/stat choices and the
relative order of remaining recipes under that ordering. If the filtered recipes no longer support
the active attribute, the stat sort SHALL clear with a visible explanation and ordinary ordering
SHALL resume, as required by material comparison. Refreshes SHALL NOT silently select another
material or add a bill. A selected inspection choice removed by the current filters SHALL be cleared with an
explanation under the inspection contract. No source-specific sorting mode is required by this slice.

#### Scenario: Stock changes during pause
- **WHEN** stock changes while paused and the recipe view remains open
- **THEN** the affected cues update without requiring game time to advance
- **AND** they show updating while their old answer is no longer current

#### Scenario: Hide and restore among similarly named recipes
- **WHEN** the player hides a row and restores it under unchanged filter and sort choices
- **THEN** the other rows retain their relative order and the restored row returns to its position
  under the current ordering
- **AND** distinct recipe/style entries remain distinguishable

#### Scenario: Hiding removes an inspected choice
- **WHEN** a hiding action removes the selected choice from the current list
- **THEN** the affected inspection choice is cleared with an explanation, without editing a bill or
  silently replacing the choice with a neighboring recipe

#### Scenario: Hiding removes the last relevant stat
- **WHEN** hiding or a visibility-mode change removes the last recipe supporting the active stat sort
- **THEN** the view clears that sort with an explanation and uses ordinary ordering
- **AND** it preserves the search and other applicable filters without substituting another attribute

### Requirement: Narrow layouts preserve access to actions
Within supported game resolutions and UI scales, the contextual Add bill view SHALL keep recipe
identity, status explanations, hiding recovery and bill-add actions reachable. A narrow layout SHALL
reflow or provide explicit scrolling or disclosure instead of making a partially clipped action
clickable or silently removing the only way to perform it. Compact icons SHALL retain readable
tooltips or equivalent text. Opening or resizing this view SHALL NOT mutate the queue.

#### Scenario: Long labels and a narrow view
- **WHEN** translated recipe names and controls do not fit beside the inspection details
- **THEN** the player can still read the identity/status and reach Add, hidden recovery and ordinary return
- **AND** truncated labels have a way to reveal their full text

#### Scenario: A row cannot fit a quick-add button
- **WHEN** quick-add is enabled and the row is too narrow for its button
- **THEN** an explicit accessible action or disclosure retains the same one-bill behavior
- **AND** the invisible overflow area cannot add a bill

### Requirement: Configuration uses the common global and save controls
The feature SHALL expose Show row Add action, built-in default **off**, followed by Show ingredient
shortage cues, built-in default **on**, in the Crafting / Add bill settings section. Both SHALL use
the common Global default, Current save and Use global controls, with new/missing save values
inheriting global. Inheritance SHALL disable only the save value editor in a loaded game, retaining
an editable Use global checkbox and dormant override. At the main menu both save controls SHALL
be disabled and unable to mutate a save. Changes SHALL apply to the next redraw of an open view,
without changing bills or hiding records. Hiding scopes and records SHALL remain contextual
gameplay choices; no separate visibility-mode default or source-scope setting is introduced.

#### Scenario: Consistent fresh defaults
- **WHEN** these settings are first used without prior megamod values
- **THEN** quick-add is off and shortage cues are on, including when a loaded older settings record
  does not yet contain these values
- **AND** ordinary Add and recipe hiding/recovery remain available

#### Scenario: Override, inherit and restore
- **WHEN** a loaded save overrides quick-add, then checks Use global, then unchecks it
- **THEN** it first follows the current global value and then restores its dormant override
- **AND** changing either value does not alter another save's override or any hidden-recipe record

#### Scenario: Main menu configuration
- **WHEN** the settings are opened without an active game
- **THEN** global values remain editable and the save editor and Use global checkbox are visibly disabled
- **AND** no previous or implicit save can be changed

#### Scenario: Hide the optional shortage decoration
- **WHEN** shortage cues are turned off while Add bill is open
- **THEN** those decorations disappear on the next redraw but comparison's ingredient facts and
  combined ingredient-plus-skill highlight keep their defined meaning

### Requirement: Controls preserve native and external bill behavior
These controls SHALL remain inside contextual Add bill, with configuration in mod settings and no
new bottom tab or whole-queue replacement. Unsupported provider-only actions SHALL remain available
through their ordinary working entry rather than disappearing silently. Missing optional providers
SHALL be normal. A changed provider or unsafe game integration SHALL disable only the affected
enhancement, retain an independently safe ordinary route, and give a bounded startup warning naming
the skipped behavior and reason. Compatible foreign changes SHALL NOT alone disable the feature.
If another active UI replacement prevents safe composition, it SHALL retain its entry and this
enhancement SHALL stay inactive with that warning. Existing bill configuration SHALL remain intact.

#### Scenario: Unrelated compatible modification
- **WHEN** another mod changes unrelated workbench presentation while the required actions remain valid
- **THEN** the safe recipe-list controls remain usable without requiring an otherwise untouched game

#### Scenario: Unsafe replacement view
- **WHEN** an active bill-view replacement cannot be safely composed with these controls
- **THEN** its existing entry remains available and the affected megamod enhancement is not applied
- **AND** startup logs explain the skipped behavior and fallback without repeated per-frame warnings

#### Scenario: Optional provider action is unsupported
- **WHEN** a provider supplies a bill action this view cannot safely represent
- **THEN** the player retains access through the ordinary working provider/native surface
- **AND** attempting to browse or inspect here does not silently replace, duplicate or reset its bill

#### Scenario: Disable the enhanced browser
- **WHEN** the enhanced browser is disabled or skipped
- **THEN** existing bills continue normally and hiding choices do not restrict the ordinary recipe list
- **AND** retaining megamod and re-enabling a safe browser restores its saved visibility choices
