# Directed recreation

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: 1.7 beta.
Status: specified, not implemented. Settings use the foundation's global/save inheritance contract.

## ADDED Requirements

### Requirement: Order recreation at a supported building

The contextual menu SHALL offer an activity-specific recreation order for one selected,
undrafted, player-controllable colonist with a recreation need on a supported recreation building when
that activity can provide a valid job. Accepting it SHALL start the ordinary activity at that
building, replacing the current order and clearing prior queued orders. It SHALL not create a
separate bottom tab, change work priorities, or rewrite the saved timetable. Drafted pawns,
multi-selection and pawns without recreation SHALL receive no added order from this feature.

#### Scenario: Take a break during work
- **WHEN** a qualifying colonist below the admission threshold is ordered to use a reachable,
  available recreation building during scheduled Work
- **THEN** the colonist travels to it and performs its normal recreation activity.
- **AND** the work priorities and stored schedule remain unchanged.

#### Scenario: Unsupported or unusable target
- **WHEN** the clicked object provides no valid supported recreation activity for the selected pawn
- **THEN** this feature offers no executable recreation order for it and preserves unrelated menu options.

### Requirement: Admission threshold limits building orders, not their duration

The recreation admission threshold SHALL be configurable from 0% to 100%, initially 60% as the
explicit proposed default. A building order SHALL be unavailable when current recreation is
strictly above it, with an explanation that recreation is already too high. Equality SHALL be
allowed if other conditions pass. This threshold SHALL not become a target that ends an activity
the instant it is crossed. Reading and swimming SHALL retain their own normal admission conditions
rather than inherit this building-specific threshold implicitly.

#### Scenario: Exact threshold
- **WHEN** the threshold is 60% and a pawn with 60% recreation can otherwise use the building
- **THEN** the order is available.
- **AND** at 61% the order is unavailable with the admission explanation.

#### Scenario: Already ordered recreation passes the threshold
- **WHEN** a valid ordered activity raises recreation above the admission threshold
- **THEN** it follows ordinary completion and boredom rules rather than being cancelled solely by that threshold.

### Requirement: Orders preserve physical and activity eligibility

The menu SHALL preserve the activity's availability and reservation restrictions, and SHALL explain
an unavailable route or occupied target when that reason is known. Conditions SHALL be rechecked
when the order is accepted and used; losing the target or route SHALL end the order normally rather
than leave an endless retry. Opening or dismissing the menu SHALL not start or reserve an activity.

#### Scenario: Target becomes unavailable after opening the menu
- **WHEN** another pawn reserves the target before the player accepts the displayed order
- **THEN** the order does not steal the reservation or force simultaneous use beyond the activity's capacity.

#### Scenario: No reachable route
- **WHEN** the selected building cannot be reached
- **THEN** no executable order is offered and an available route failure explanation is shown.

### Requirement: An explicit break can continue without permanently changing schedules

An ongoing ordered building activity, ordered reading, or supported ordered swimming SHALL not be
cancelled merely because the current timetable says Work or Anything. This temporary accommodation
SHALL apply only to player-controlled colonists and the explicit ongoing order. Sleep and other
assignments SHALL not be silently treated as recreation by this feature. Natural recreation,
unrelated work, and other pawns SHALL retain their normal scheduling behavior.

#### Scenario: Reading during work
- **WHEN** the player orders a colonist to read during Work using the game's ordinary reading control
- **THEN** the colonist can continue the reading job despite the Work assignment.
- **AND** the saved timetable remains Work and resumes governing behavior when the order ends.

#### Scenario: Ordinary recreation is not forced
- **WHEN** a colonist begins recreation without a player order
- **THEN** this feature does not protect it from ordinary schedule-driven completion or interruption.

### Requirement: Recreation ends through normal job rules

An explicit recreation order SHALL retain the activity's ordinary completion, satisfaction, boredom,
failure and interruption rules. It SHALL remain cancellable by a new player order or drafting, and
SHALL not restart itself after completion. A saved ongoing order SHALL resume after load only while
its pawn, target and activity remain valid; ending or loading an invalid order SHALL leave no
permanent recreation override.

#### Scenario: Finish and return to scheduled activity
- **WHEN** the ordered recreation job completes normally or the player drafts its pawn
- **THEN** the recreation order ends and no temporary schedule accommodation remains.
- **AND** undrafting does not automatically restart the cancelled activity.

### Requirement: Odyssey swimming is optional and contextual

The contextual menu SHALL offer Go swimming when Odyssey is active and swimming is available,
for one selected undrafted colonist with recreation on water with a usable swimming route. Accepting
it SHALL start the normal walking/swimming activity. When the proposed destination is outdoors and
the game considers outdoor swimming unsuitable, the option SHALL be unavailable; at outdoor
temperature at most 10°C the explanation SHALL identify cold, otherwise unsuitable swimming.
Indoor swimming SHALL not be refused solely because outdoor conditions are poor. Without active
Odyssey, no swimming option or related errors SHALL appear.

#### Scenario: Warm indoor water during cold weather
- **WHEN** an otherwise usable indoor swimming destination exists while outdoor swimming is unsuitable
- **THEN** poor outdoor weather alone does not disable the indoor swimming order.

#### Scenario: Missing expansion or route
- **WHEN** Odyssey is inactive, or no usable swimming route exists
- **THEN** no executable swimming order is added and ordinary building recreation remains available.

### Requirement: Configuration and incompatibility are explicit

The threshold SHALL appear in searchable megamod recreation settings with global default, Current
save and Use global controls. Save controls SHALL be disabled from the main menu and while inherited.
Changing the threshold SHALL affect subsequent menu evaluation without rewriting ongoing jobs.
If an enabled integration is incompatible, startup SHALL explain the skipped behavior and preserve
unaffected native actions. With the original Chill the Fork Out active, megamod SHALL initially
defer this feature to that provider and warn rather than add duplicate orders.

#### Scenario: Change a save's threshold
- **WHEN** the player overrides the threshold in one save and reopens a building's contextual menu
- **THEN** that save uses the override, other inheriting saves use the global default, and the current
  recreation job is not terminated merely by the setting change.

#### Scenario: Incompatible swimming support
- **WHEN** the enabled swimming integration cannot safely run with the current game
- **THEN** startup identifies swimming as unavailable while independent building recreation remains usable.
