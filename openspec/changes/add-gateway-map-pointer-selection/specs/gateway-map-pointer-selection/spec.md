## ADDED Requirements

### Requirement: Minimized native map selection
Owner: repository tooling RimWorld Dev Gateway (`mods/RimWorldDevGateway`). The Gateway SHALL expose
`map.pointer.select` with finite numeric x/z through the authenticated semantic API and typed E2E.
It SHALL execute native left mouse-down/up selection at those map coordinates without moving the
desktop cursor, foregrounding the game, directly selecting IDs, or bypassing candidate eligibility.

#### Scenario: Fractional pointer and repeated selection
- **WHEN** a caller clicks an unobstructed point on the settled current map
- **THEN** native selection and repeated-click cycling run with that fractional pointer
- **AND** before/after selected IDs are recorded and visible brackets/inspect pane reflect the result

#### Scenario: Invalid or obstructed action
- **WHEN** coordinates are nonnumeric, nonfinite, outside map/view or the map is blocked by UI/input tools
- **THEN** the action rejects without selecting an object or affecting desktop input

### Requirement: Restore temporary input overrides
The operation SHALL validate before mutation and restore Event.current, drag state and its own
temporary Harmony prefixes after success or failure. Native selection effects SHALL remain observable.
No save or configuration SHALL be edited by this action; isolated-run evidence SHALL retain normal
configuration hashes, exact process/build identity, action/results/screenshots and cleanup.

#### Scenario: Native failure
- **WHEN** native selection throws after the input scope is acquired
- **THEN** the action reports failure and removes its input overrides without removing unrelated patches
