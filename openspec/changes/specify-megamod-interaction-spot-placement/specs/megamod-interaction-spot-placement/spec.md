# Interaction spot placement

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S129 replacement; observable specification only, no runtime acceptance.

## ADDED Requirements

### Requirement: Relax only the selected interaction-position obstacle
The feature SHALL allow a building plan whose interaction position would otherwise be blocked
solely by enabled obstacle kinds, provided every other placement condition is satisfied. Chunks
SHALL be enabled by default and trees disabled by default. Chunks include the game's recognized
stone and other chunk kinds, including qualifying added content; they do not include arbitrary
items, rocks or objects merely named chunk. Trees include plants recognized as trees, without
automatically extending the exception to bushes or other plants. Disabling an exception SHALL
restore ordinary placement behavior rather than forbid placements the game already allows.

#### Scenario: Plan over a stone or slag chunk
- **WHEN** the only obstacle at a building's interaction position is a recognized chunk and all other placement rules pass
- **THEN** default settings allow the plan without first moving that chunk.

#### Scenario: Tree preference and unrelated plant
- **WHEN** an otherwise valid plan's interaction position is blocked by a tree
- **THEN** it remains blocked by default and becomes eligible when tree placement is enabled
- **AND** a different blocking plant receives no exception merely because trees are enabled.

#### Scenario: Both exceptions disabled
- **WHEN** both options are off and a proposed interaction position is already acceptable to the game
- **THEN** it stays acceptable, while a chunk/tree that the game rejects receives no owned exception.

### Requirement: Check the complete current placement geometry
The feature SHALL evaluate every required interaction position using the building's current
location and rotation. Any unexempted obstruction at any required position SHALL retain rejection,
including an obstructing planned building. A qualifying obstacle sharing a cell with another blocker
SHALL not hide that other blocker. Supported new construction, installation and reinstallation SHALL
use the same rule, preserving the identity and properties of the object being moved.

#### Scenario: Several positions with mixed blockers
- **WHEN** one required interaction position contains a permitted chunk and another contains a wall or blocking blueprint
- **THEN** the plan remains rejected with the applicable obstruction reason.

#### Scenario: Several objects in one position
- **WHEN** a permitted chunk and an unexempted blocking object share an interaction position
- **THEN** the plan remains rejected despite the chunk exception.

#### Scenario: Rotate or reinstall
- **WHEN** the player rotates a preview or selects a new destination for a supported reinstall
- **THEN** placement uses that destination and rotation's interaction positions
- **AND** an accepted reinstall preserves the moved object's native identity, material, quality and condition.

### Requirement: Preserve all unrelated placement restrictions
This exception SHALL preserve map boundaries, the actual building footprint, terrain support,
existing/planned building restrictions, the ordinary exclusion of an object being moved and any
additional provider restrictions. It SHALL not turn an unexplained rejection into acceptance or
require an obstacle removal that the ordinary ignored-object workflow does not require. It SHALL
not add fog-planning or hidden-content discovery; any separately owned planning permission keeps
its own reveal and revalidation rules. Preview eligibility and final placement SHALL use current
state, so moving an obstacle or changing preferences before confirmation cannot admit a stale plan.

#### Scenario: Interaction position outside the map
- **WHEN** a rotated plan's interaction position lies outside map bounds
- **THEN** it is rejected normally without an exception, invalid access or misleading success.

#### Scenario: Unrelated terrain or provider rejection
- **WHEN** a plan also violates terrain support, its building footprint or a provider's additional placement restriction
- **THEN** permitting chunks or trees does not remove that rejection.

#### Scenario: Ignore the moved object and recheck new obstructions
- **WHEN** a supported move ordinarily ignores its own old object while checking a destination
- **THEN** that exclusion remains effective
- **AND** a new unexempted obstruction introduced before confirmation prevents the final placement.

### Requirement: Permission to plan does not clear or guarantee use
This feature SHALL change only placement eligibility. It SHALL not move, destroy, damage, forbid,
reserve or create haul/cut orders for the overlapping obstacle, and SHALL not change movement,
construction cost/work or a pawn's ability to reach and use the finished building. Existing native
or separately specified construction/clearing behavior SHALL retain its own authority and scope.
Removing an actual obstruction through ordinary gameplay SHALL make work possible only when the
remaining job requirements are met. Turning the exception off SHALL not delete existing plans.

#### Scenario: Accepted plan leaves its interaction obstacle alone
- **WHEN** the player confirms a permitted plan over a chunk or enabled tree
- **THEN** this feature adds no clearing order and leaves that obstacle unchanged
- **AND** placement does not guarantee that a pawn can immediately perform work there.

#### Scenario: Ordinary clearing and later work
- **WHEN** the player clears the interaction obstacle through an ordinary haul or cut action and the building/job otherwise becomes usable
- **THEN** the pawn can use the normal work path without any movement, cost or work-rate exemption from this feature.

#### Scenario: Disable after planning
- **WHEN** the player disables an exception after placing a plan
- **THEN** the plan remains, future eligibility checks use the new preference, and ordinary construction/job checks still apply.

### Requirement: Use common scoped construction settings
The two preferences SHALL appear in mod settings under Construction → Interaction positions, in
the order chunks then trees, with built-in defaults on and off respectively. Each SHALL provide
Global default, Current save and Use global through the common settings model. New or missing save
entries SHALL inherit; checking Use global SHALL retain a dormant override and unchecking SHALL
restore it or seed the first override from the effective default. Inherited save controls SHALL be
inactive, and at the main menu both save controls and Use global SHALL be inactive. Changes SHALL
affect the next preview and final check, survive reload and remain isolated between saves. All
owned text SHALL be available in the seven baseline languages. No new bottom tab or operational
bottom-right toggle SHALL be added for these configuration choices.

#### Scenario: Set a global default and save exception
- **WHEN** the player enables trees globally while one save explicitly overrides trees off
- **THEN** inherited saves allow the exception and that overridden save retains ordinary tree restrictions
- **AND** checking Use global takes the current global value while preserving the dormant off override.

#### Scenario: Main-menu configuration
- **WHEN** the player opens these preferences without a loaded game
- **THEN** the global controls are editable, save controls and Use global are disabled, and no prior or implicit save is changed.

### Requirement: Keep diagnostics optional and developer scoped
Optional detailed placement diagnostics SHALL live in the top debug toolbar, visible only in
developer mode, initially off and temporary for the current session. They SHALL not alter placement
outcomes, throw on an empty obstacle list or flood the log during mouse movement. Ordinary disabled
features SHALL not produce compatibility-failure warnings. Required skipped-integration warnings
SHALL remain available even when detailed diagnostics are off.

#### Scenario: Inspect a rejected preview with diagnostics
- **WHEN** a developer enables diagnostic detail and moves a preview across invalid, empty or edge cells
- **THEN** placement remains safe and unchanged and the log is not flooded by repeated preview evaluations
- **AND** returning to ordinary play or a new session does not retain an active diagnostic toggle.

### Requirement: Admit the narrow feature or retain existing behavior
An enabled patch SHALL remain inactive when its documented target requirements cannot be
established, with one startup warning identifying the target, failed requirement, unavailable
placement exception and fallback. Compatible unrelated changes SHALL not alone prevent activation.
With original Interaction Spot On X active, the owned overlapping feature SHALL be withheld with
a warning and the original remain authoritative; the megamod SHALL not remove or edit that mod.

#### Scenario: Changed game or provider rule
- **WHEN** required placement semantics are missing or ambiguous during admission
- **THEN** the owned exception remains inactive with a bounded startup warning and the remaining placement behavior is preserved.

#### Scenario: Original provider co-installed
- **WHEN** original Interaction Spot On X is active
- **THEN** only the original supplies this overlap, the owned settings explain that they are not applied, and startup reports the withheld owned feature.
