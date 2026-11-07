# Scenario choice search

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. S113 replacement specification; implementation remains pending.

## ADDED Requirements

### Requirement: SCS01 Search belongs to the existing scenario editor
The native scenario editor SHALL offer a labeled search field for each supported item-choice,
starting-animal, starting-research, stat-multiplier and incident part, including scheduled incidents.
Each field controls only its own part's primary choice list. Search SHALL remain within that
context, with no new bottom tab, debug action, bottom-right toggle or mod-settings page.
Material, quality, quantity and incident timing controls retain their existing purpose and remain
accessible. This scope does not add search to every other scenario-part family or to Add part,
material or quality selectors.

#### Scenario: Find an item for one starting-item part
- **WHEN** the player enters a search in a supported starting-item part and opens its item selector
- **THEN** that selector shows matching items from its ordinary list with their existing availability while the part's material, quality and count controls remain usable

#### Scenario: Supported choice families
- **WHEN** the player edits starting animals, starting research, stat multipliers, disabled incidents or scheduled incidents
- **THEN** each supported primary selector has its own search field in the existing editor

#### Scenario: Search does not filter a material list
- **WHEN** the player has searched for an item and opens that item's material or quality selector
- **THEN** those selectors retain their ordinary choices rather than interpreting the item query as a material or quality filter

### Requirement: SCS02 Search narrows ordinary displayed choices
Search SHALL default to case-insensitive literal substring matching against each choice's current
localized display name, including the full stat name shown in the list. An empty query restores
the ordinary list. Search SHALL retain native/provider list membership, selection restrictions and
relative order, including duplicate names representing different choices. It SHALL NOT add content
that the ordinary selector omits because it is removed, unloaded or excluded by eligibility. An
option intentionally displayed disabled by that selector SHALL remain visible when its name
matches, with its disabled state and explanation preserved. Search is not a new eligibility rule
outside the displayed choice list and SHALL NOT enable a disabled option.

The animal selector's native Random pet action SHALL remain available separately from filtered
animal names. When no ordinary displayed content choice matches, the selector SHALL show an explicit
no-matches indication without choosing an item or hiding the clear-search control. Existing selected
values remain visible even when they do not match the query.

#### Scenario: Literal punctuation and case
- **WHEN** the player types part of a displayed name with different capitalization or literal punctuation
- **THEN** the ordinary search finds that substring without treating punctuation as expression syntax

#### Scenario: Eligibility and ordering survive search
- **WHEN** matching names include ordinary native and modded choices plus content omitted by the active selector
- **THEN** only matches from the ordinary displayed list appear in their original relative order, retaining disabled states and distinct identically named choices

#### Scenario: Search the name actually shown
- **WHEN** a stat's full displayed name differs from its short name or the game uses another supported language
- **THEN** search matches the displayed localized name

#### Scenario: Matching disabled provider option
- **WHEN** the ordinary selector displays a disabled provider option with an explanation and its name matches the query
- **THEN** search retains that disabled row and explanation without making it selectable or adding candidates the ordinary selector omits

#### Scenario: Empty results are recoverable
- **WHEN** no ordinary displayed content choice matches the query, including any disabled rows
- **THEN** the list reports no matches, retains Random pet where applicable and permits clearing the query without changing the selected value

#### Scenario: Clear search restores choices
- **WHEN** the player clears the query
- **THEN** the ordinary current choices return in order, including compatible provider additions and their existing disabled states

### Requirement: SCS03 Expression search is explicit and recoverable
Each search field SHALL offer an explicitly labeled regular-expression mode, initially off, as
transient editor state. Turning it on interprets the current query as a case-insensitive expression
against the same display names. Invalid or excessively expensive expressions SHALL show a nearby
explanation and suppress filtered content choices until corrected, cleared or returned to literal
mode. They SHALL NOT crash the editor, freeze it indefinitely, silently switch matching rules or
change scenario data. Random pet remains available as a separate action.

#### Scenario: Explicit expression alternatives
- **WHEN** the player enables expression mode and enters a valid alternative pattern
- **THEN** the list shows matching choices from the ordinary displayed list, preserving their disabled states and native ordering

#### Scenario: Invalid expression
- **WHEN** expression mode contains an unfinished or otherwise invalid pattern
- **THEN** an inline error explains that the expression cannot be used and the player can correct it or switch to literal search

#### Scenario: Expensive expression
- **WHEN** an expression exceeds the supported evaluation budget
- **THEN** matching stops with a recoverable explanation rather than leaving the editor indefinitely unresponsive

#### Scenario: Literal recovery
- **WHEN** the player turns expression mode off after an expression error
- **THEN** the same text is searched literally and ordinary editing continues without resetting the selected scenario value

### Requirement: SCS04 Search preserves native selection actions
Selecting a visible result SHALL perform its original selection action exactly once. Search SHALL
preserve the item's normal material/default-quality update, the animal choice, research assignment,
stat assignment and incident assignment. Counts, multipliers, minimum/maximum incident days, repeat
controls and other unrelated values SHALL change only through their ordinary player actions.
Search SHALL NOT create duplicate menus, overwrite provider callbacks or apply a stale choice
after its part has been removed or its eligibility has changed.

#### Scenario: Choose an item
- **WHEN** the player chooses a matching item
- **THEN** that part selects it once, applies the normal material and quality defaults and retains its count

#### Scenario: Choose research or an animal
- **WHEN** the player selects a matching research project, animal or Random pet
- **THEN** only the intended native selection changes, with ordinary constraints and animal count retained

#### Scenario: Change a stat or incident
- **WHEN** the player chooses a filtered stat or incident
- **THEN** exactly one menu and selection action occurs while the existing multiplier, incident timing and repeat values remain unchanged

#### Scenario: Stale menu choice
- **WHEN** the underlying part is removed or a provider makes a displayed choice ineligible before selection
- **THEN** the stale result cannot change another part or select unavailable content, and reopening the selector reflects current choices

### Requirement: SCS05 Queries are stable transient editor state
Each part's query and expression mode SHALL remain independent and follow that same part through
reordering, scrolling and edits to its selected value or quantity. Clearing one field SHALL NOT
clear another. Removing a part discards its search state. Loading or generating another scenario,
closing the editor or beginning the game discards the departed editor's queries. A newly opened
editor starts with empty queries and literal mode.

Queries SHALL NOT be stored in scenario files, saves or global configuration, count as scenario
edits, or create a save prompt by themselves. These controls are contextual editing state, not
settings; no global/default/save inheritance row is appropriate.

#### Scenario: Independent same-kind parts
- **WHEN** two starting-item parts contain different queries and the player changes one part's item and count
- **THEN** both queries remain attached to their original parts and neither field unexpectedly resets

#### Scenario: Reorder or remove a part
- **WHEN** the player reorders a searched part or removes it and creates another
- **THEN** reordering retains that part's query, while the newly created part starts empty and does not inherit the removed part's query

#### Scenario: Search alone is not a scenario edit
- **WHEN** the player types, clears or changes search mode without changing scenario choices
- **THEN** scenario contents and dirty state remain unchanged and search alone causes no save prompt

#### Scenario: Editor and scenario lifecycle
- **WHEN** the player leaves the editor, starts the game, or loads or generates another scenario
- **THEN** departed queries are discarded and reopening starts empty in literal mode without modifying a global default or prior save

### Requirement: SCS06 Search never alters scenario execution
Search SHALL affect only displayed editor choices. Randomization, starting-pet generation,
starting-item generation, starting research, incident scheduling and saved scenario behavior SHALL
retain their ordinary semantics regardless of queries used during editing. Typing or drawing search
SHALL NOT consume gameplay randomness, execute a scenario action, grant content, or modify another
selector's candidate source.

#### Scenario: Random pets ignore the editor query
- **WHEN** the player searches for an animal, selects Random pet and begins a game
- **THEN** ordinary random-pet eligibility and weighting determine the starting animal, without restricting it to the old search

#### Scenario: Empty matches do not break randomization
- **WHEN** the player leaves a query with no matches and uses an ordinary scenario randomization action
- **THEN** that action uses its normal candidate set rather than failing or sampling an empty search result

#### Scenario: Chosen scenario effects stay native
- **WHEN** a searched scenario is saved, reloaded and used to start a game
- **THEN** selected items, research, animals, stat effects and incidents follow ordinary scenario data, with no saved search restriction

### Requirement: SCS07 Compatibility failure leaves ordinary editing available
Search SHALL activate only for selector integrations whose required behavior is compatible. Other
mods having changed a method is not itself incompatibility. An unsafe or unavailable integration
SHALL leave that selector's ordinary editor usable and record one bounded startup warning per
skipped enabled patch identifying the target, failed requirement, unavailable search and fallback.
Other independently safe selectors remain available. Later incompatible changes SHALL remove only
the affected owned enhancement before it can apply an unsafe search or selection.

An active `nimrag.scenariosearchbar` provider SHALL disable this owned overlapping feature with a
startup warning, without disabling or removing the provider. Supported inherited native selectors
and compatible provider additions remain searchable; wholly custom editors receive no universal
support promise. All owned labels, mode/error/no-match feedback and controls SHALL remain usable
in English, German, Spanish, French, Simplified Chinese, Russian and Japanese, with supported UI
scales, long labels and text input. The feature SHALL add no hard DLC or original-provider requirement.

#### Scenario: Compatible foreign candidates
- **WHEN** an admitted provider adds choices through a supported native selector
- **THEN** its ordinary displayed choices participate in search with their existing disabled states and original actions, without requiring the original search mod

#### Scenario: Unsafe selector fallback
- **WHEN** one selector no longer satisfies its search integration requirements
- **THEN** its ordinary editor remains available, a bounded warning explains the skipped search, and independently safe selectors continue working

#### Scenario: Original provider present
- **WHEN** the original Scenario Searchbars package is active
- **THEN** the owned overlap is disabled with a startup explanation and the provider is left untouched

#### Scenario: Languages and layout
- **WHEN** the player searches and selects with long translated labels, text composition or a supported UI scale
- **THEN** the search field, expression control, feedback and existing scenario controls remain legible and independently operable
