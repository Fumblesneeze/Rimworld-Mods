# Content selection

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. Specification draft; no implementation or acceptance claimed.

## ADDED Requirements

### Requirement: Browse an identifiable content catalogue in settings

The mod SHALL provide a Content selection section in mod settings. It SHALL list supported content
from Core, active DLC and active mods with a readable name, content kind, source and distinguishing
identifier. It SHALL retain excluded entries for restoration. The catalogue SHALL cover every
content family in the companion availability specification, with items, buildings, plants and pawn
races as distinct useful kinds. Generated blueprints, frames, unfinished products, the basic male
and female body types and the basic colonist pawn kind SHALL not be independently excludable.
Only factions ordinarily configurable at world creation SHALL appear as independent faction
choices. An incident-owned quest SHALL be controlled through its incident rather than listed twice.
Unsupported entries SHALL state that their exclusion is unavailable and SHALL not claim success.

#### Scenario: Identify two similarly named entries
- **WHEN** two active sources supply identically labelled content and the player opens the page
- **THEN** each row identifies its own source and content, and changing one does not change the other

#### Scenario: Missing and protected content
- **WHEN** an optional provider is absent or a protected generated entry would otherwise be listed
- **THEN** no actionable choice for that missing or protected entry appears
- **AND** stored choices for absent content remain preserved as unavailable choices for later return

### Requirement: Filters change browsing without changing content choices

The page SHALL compose the searchable type and source selectors specified by S020. Its separate
main search SHALL match a case-insensitive literal substring within the entry's name, identifier,
source or content kind; it SHALL not match text created only by joining two fields. All three
filters SHALL combine. Clearing a filter SHALL reveal the other matches without modifying any
content choice. Equal source display names SHALL remain distinct selections. Excluded rows SHALL
precede available rows; within those groups the page SHALL use stable readable-name order with
source and identifier tie-breaks. Full identities and exclusion reasons SHALL remain accessible
when visible text is shortened. Search, scroll and selector state SHALL not become save settings.

#### Scenario: Search and filter an excluded item
- **WHEN** the player narrows type and source and searches for an excluded item's identifier
- **THEN** the matching item remains visible with its exclusion state and reasons
- **AND** clearing the search does not enable it or reset the selected source or type

#### Scenario: Browsing cannot edit a hidden row
- **WHEN** an entry is filtered out or the player closes a selector without choosing an option
- **THEN** its content choice remains unchanged

### Requirement: Content choices use global defaults and save overrides

Every individual content choice and preset activation SHALL use the common settings model.
Built-in defaults SHALL allow every individual entry and leave every removal preset inactive.
New or missing save choices SHALL use global. Each row SHALL expose Use global, Current save and
Global default, and distinguish direct choices from the resulting effective exclusion. The save
editor SHALL be inactive while inheriting. Checking Use global SHALL retain the dormant override;
unchecking SHALL restore it or seed its first value from the currently effective global value.
Main-menu save editors and Use global controls, including their actions, SHALL remain inactive
with an explanation. There SHALL be no implicit previous-save target.

#### Scenario: Independent saves
- **WHEN** save A overrides a global exclusion to allow an entry and save B still uses global
- **THEN** A and B retain their separate choices through saving and loading
- **AND** changing the global default affects B's next application but does not overwrite A's override

#### Scenario: Main-menu and inherited controls
- **WHEN** the player opens settings without a loaded save, or views an inherited row in a save
- **THEN** the unavailable save controls cannot mutate a save, while the global editor remains usable

### Requirement: Apply a complete policy at a clear game boundary

The mod SHALL apply content choices when beginning a new game or loading a save, before the relevant
content choices and generation occur. New-game setup SHALL use global choices; a loaded save SHALL
use its own inheritance and overrides. Editing settings during play SHALL leave the current applied
policy unchanged and visibly label the requested changes as pending until the save is saved and
reloaded. The page SHALL distinguish requested, applied and unavailable states. Main-menu edits
SHALL affect the next newly opened game-setup flow. A setup already in progress SHALL require
reopening it, with an explanation, rather than silently invalidating its selected options.
Returning to the main menu SHALL stop using the previous save's policy. Application failure SHALL
leave an affected feature group wholly inactive, identify the unavailable behavior and preserve
the requested choices; unrelated admitted groups SHALL remain usable.

#### Scenario: Pending changes during play
- **WHEN** the player excludes an item in a running colony
- **THEN** the page shows that exclusion as pending and current play continues under the applied policy
- **AND** saving and reloading applies the saved choice before new production or generation

#### Scenario: Switch saves and return to setup
- **WHEN** the player leaves an overriding save, opens new-game setup, then loads an inheriting save
- **THEN** setup uses global choices and the loaded save resolves its own choices without retaining
  the first save's exclusions

#### Scenario: One unavailable content family
- **WHEN** an installed update prevents a content family from being controlled safely
- **THEN** the page identifies that family's unavailable exclusions and startup logs identify skipped
  behavior, while unrelated supported families keep their requested behavior

### Requirement: Presets contribute independent exclusion reasons

An active removal preset SHALL exclude its resolved members in addition to individual exclusions
and other active presets. The page SHALL show the preset's name, source and membership and explain
which reasons exclude an entry. Re-enabling a preset SHALL remove only that preset's contribution.
Allowing an individual entry SHALL not silently override an active removal preset; the remaining
reason SHALL stay visible. A nested preset SHALL contribute its members while the parent is active
without overwriting the nested preset's independently saved activation choice. Duplicate membership
SHALL have no additional effect. Cycles SHALL terminate without repeated work or repeated warnings.
Missing, malformed and unsupported members SHALL be reported without aborting other valid members.

#### Scenario: Overlapping presets and an individual exclusion
- **WHEN** an item is excluded individually and by two presets, and the player deactivates one preset
- **THEN** the item remains excluded for the individual choice and the other preset
- **AND** removing the remaining reasons restores eligibility on the next application

#### Scenario: Nested, cyclic and missing members
- **WHEN** a preset contains another preset, a repeated item, a cycle and a missing member
- **THEN** each valid reachable item is excluded once, unresolved membership is explained, and
  browsing or applying the preset completes without an endless loop
- **AND** deactivating the parent does not erase the child's independent activation choice

### Requirement: Preserve authored lists from compatible providers

Compatible mod-authored Cherry Picker removal lists SHALL remain available with their authored
names, source identity and current member additions, even when original Cherry Picker is absent.
Members added by another compatible mod SHALL not be discarded or duplicated in their effects.
Provider/DLC requirements SHALL remain respected. An unsupported provider binding SHALL produce
an explanation rather than an empty list falsely presented as working. The mod SHALL not pretend
to be the original package, satisfy unrelated original-only dependency checks, or claim that every
Cherry Picker add-on is compatible.

#### Scenario: Provider appends a preset member
- **WHEN** a compatible provider supplies a removal list and another compatible addition extends it
- **THEN** activating the list excludes the resolved original and added members together
- **AND** provider names and the added member's own identity remain visible

#### Scenario: Provider has an unsupported original-only binding
- **WHEN** a provider's list cannot be supplied without original Cherry Picker or required DLC
- **THEN** that integration is reported as unavailable without weakening the requirement or
  preventing ordinary content choices from working

### Requirement: Restoration preserves existing worlds and unrelated choices

Content exclusion SHALL not delete existing physical items, buildings, plants, pawns, settlements,
quests, learned abilities, health conditions, genes, memories or completed research. Existing
nutrition, value, components, material costs and work amounts SHALL not be rewritten merely to
prevent future acquisition. The companion specification SHALL determine which new actions are
refused. Re-enabling content SHALL restore its ordinary eligibility under other remaining rules;
it SHALL not force normally hidden or unavailable content to become available. Content that
disappears with an inactive provider SHALL retain its saved choices for later return.

#### Scenario: Exclude food already in storage
- **WHEN** a saved exclusion becomes active for a meal with physical stacks in the colony
- **THEN** those stacks remain identifiable, haulable and edible with their ordinary nutrition
- **AND** the future-acquisition restrictions do not turn them into zero-value or zero-nutrition items

#### Scenario: Restore without undoing another rule
- **WHEN** the player re-allows content that another active rule still makes unavailable
- **THEN** the owned exclusion ends at the next application but the other restriction remains
- **AND** unrelated entries, costs and completed progress remain unchanged

### Requirement: Original-provider coexistence and settings ownership are explicit

If original Cherry Picker is active, the owned content application and preset adaptation SHALL
remain inactive, identify the original as the selected provider and log a bounded warning for the
skipped owned behavior. The mod SHALL not uninstall, unpatch or modify the original. Owned choices
SHALL remain stored for later use. Original settings SHALL never be imported or changed silently.
There SHALL be no extra bottom tab or bottom-right content-editing toggle; this is configuration.

#### Scenario: Original provider is co-installed
- **WHEN** the game starts with both providers active
- **THEN** only the original applies its content choices, the owned page explains its inactive
  application, and neither provider's stored configuration is rewritten by migration

#### Scenario: Start without original settings migration
- **WHEN** the original is removed from the active list and owned content selection is first used
- **THEN** the owned defaults or previously saved owned choices apply
- **AND** the original configuration file is untouched and no imported-choice claim is displayed
