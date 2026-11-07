# Passion display

Sole owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S128 replacement, **specified only**; no runtime acceptance.

## ADDED Requirements

### Requirement: Show the current skill's passion as a recognizable icon
The owned pawn HUD SHALL show the game's minor or major passion icon beside the corresponding
skill name and SHALL show no passion icon for no passion. A successfully displayed icon SHALL
replace that passion's textual marker without removing or recoloring unrelated skill text.
Skill values, progress, disabled-skill meaning and existing emphasis SHALL remain accurate.

#### Scenario: Ordinary passion levels
- **WHEN** a pawn with no, minor and major passions is inspected in the owned HUD
- **THEN** only the minor and major rows show their respective current game icons
- **AND** no duplicate star marker is shown for those successfully displayed icons.

#### Scenario: Emphasized skill name
- **WHEN** a skill name contains ordinary punctuation or visual emphasis
- **THEN** replacing its passion marker preserves the complete name and unrelated emphasis
- **AND** the skill value and progress retain the same meaning as before.

### Requirement: Use admitted providers' actual passion identity
With a supported optional passion provider active, the display SHALL use the icon and identity
belonging to the skill's actual registered passion, including additional provider-defined passions.
It SHALL respect the provider's current chosen texture, including a compatible texture replacement,
and SHALL NOT approximate unknown passions as a number of ordinary passion levels. Providers are
optional; their absence SHALL leave ordinary passion display usable.

#### Scenario: Additional registered passion
- **WHEN** a supported provider gives a skill an additional passion with a valid icon
- **THEN** the HUD shows that passion's icon and identifies the same passion in its details
- **AND** it does not show repeated native icons or stars based on the passion's numeric identity.

#### Scenario: Provider absent or texture replaced
- **WHEN** the HUD is used without an optional provider, or with an admitted replacement of an existing icon
- **THEN** the first case uses the current native icons and the second uses the current supplied texture
- **AND** neither case requires installing the original RimHUD Passion Icons mod.

### Requirement: Keep passion and skill details accessible without layout collisions
The icon SHALL fit proportionally beside the skill name, without covering the name, value, progress
or adjacent controls. A constrained row SHALL shorten the visible name as necessary and keep its
full name and passion details accessible on hover. Hovering the name or icon SHALL expose the same
complete skill details, including the passion's known name and learning effects. The icon SHALL
not create a new pawn action; an ordinary row click SHALL retain the HUD's existing skill action.
All owned labels and fallback explanations SHALL support the seven baseline product languages.

#### Scenario: Narrow HUD and long translated name
- **WHEN** a long skill name is displayed at a supported narrow width or enlarged UI scale
- **THEN** the icon remains distinct and proportional, and the row's value and controls remain readable
- **AND** hovering the shortened name or icon exposes the full name and skill details.

#### Scenario: Existing skill interaction
- **WHEN** the player clicks a skill row containing a passion icon
- **THEN** its existing skill-detail action occurs once for the displayed pawn
- **AND** clicking or hovering does not assign a passion or change a skill.

### Requirement: Fall back truthfully for incomplete passion data
An unavailable, malformed or unsupported passion icon SHALL leave the rest of the HUD usable.
The affected row SHALL show the known passion name as text when available; if its identity cannot
be resolved, it SHALL show a localized unavailable-passion indication without inventing a meaning.
Its details SHALL distinguish an unavailable picture from an unresolved identity. No placeholder
error texture, misleading native passion or unbounded repeated marker SHALL replace the missing
icon. Failures SHALL produce a bounded diagnostic rather than a message on every repaint.

#### Scenario: Known passion with missing artwork
- **WHEN** the displayed skill has a known passion but its icon cannot be obtained
- **THEN** that row identifies the passion in text and explains that its picture is unavailable
- **AND** other skill rows and native inspection remain usable.

#### Scenario: Unresolvable passion
- **WHEN** a skill's passion cannot be safely identified from the active content
- **THEN** the row shows an unavailable-passion indication and the remaining known skill details
- **AND** it neither treats the passion as a native level nor floods the log while displayed.

### Requirement: Follow the displayed pawn and existing HUD controls
Passion presentation SHALL follow S127's current subject, skill visibility, layout, refresh and
show/hide controls. Pawn selection or a known passion change SHALL update the affected display on
the next HUD refresh; switching saves or maps SHALL not retain a previous subject's icon or tooltip.
This feature SHALL add no separate configuration, bottom tab, toggle or saved passion copy.
Existing HUD configuration SHALL continue to use its common global/save inheritance rules.

#### Scenario: Change the selected pawn
- **WHEN** the player selects another pawn while the HUD is open
- **THEN** the next display refresh shows only that pawn's skills, passions and hover details
- **AND** a still-open action cannot operate on a newly selected pawn by accident.

#### Scenario: Change passion or hide the HUD
- **WHEN** a passion changes through ordinary gameplay, or the player hides the HUD
- **THEN** the next applicable refresh shows the changed passion, or no owned HUD icons respectively
- **AND** reopening or loading another save derives icons from its actual current pawn state.

### Requirement: Preserve gameplay and existing provider filter behavior
Displaying passions SHALL not alter skill gain, forgetting, passion assignment, pawn generation,
learning multipliers or existing Prepare Moderately filter choices, labels, matching and
randomization. For the inspected modern VSE/Prepare Moderately combination, this feature SHALL
leave its working integration authoritative and SHALL not add a second selection or startup repair.
Support for a separately demonstrated older broken combination requires its own reviewed contract.

#### Scenario: Existing extended-passion filter
- **WHEN** a player uses a working provider's passion filter or randomizes it with the owned display enabled
- **THEN** the resulting choices, labels and filtering retain that provider's behavior
- **AND** this display does not trigger another passion selection or erase the filter.

#### Scenario: Repeated pawn inspection
- **WHEN** the player repeatedly opens, hovers or closes the passion display
- **THEN** the pawn's passions, skill progression and subsequent generation/filter behavior are unchanged
- **AND** the display does not change another mod's settings or saved filters.

### Requirement: Isolate incompatible display integrations
An enabled integration SHALL remain inactive when it cannot meet its documented target requirements,
with one startup warning identifying the feature, failed
requirement, unavailable behavior and fallback. Compatible unrelated changes SHALL not by
themselves prevent activation. Original RimHUD co-installation SHALL follow S127: preserve its
authoritative display, withhold the owned overlap and warn without removing other mods' behavior.
No successful icon display SHALL imply support for an unverified external binary or package contract.

#### Scenario: Incompatible optional provider
- **WHEN** an enabled provider integration cannot safely supply the required passion information
- **THEN** startup reports the failed integration and the HUD remains usable with truthful fallback
- **AND** unaffected native passion display continues.

#### Scenario: Original HUD co-installed
- **WHEN** original RimHUD remains active alongside the megamod
- **THEN** the owned HUD and its icons are withheld with the documented warning
- **AND** the original HUD and any original Passion Icons behavior are not changed by this feature.
