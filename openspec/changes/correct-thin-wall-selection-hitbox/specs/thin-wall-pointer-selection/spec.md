## ADDED Requirements

### Requirement: Pointer hits match the thin selection envelope

Thin walls and doors, including blueprints and frames, SHALL be admitted for pointer selection only within their existing settled
selection bracket rectangle: one cell along the edge and 34/60 cell across it, centered on the
shared edge. Bracket appearance and animation SHALL remain unchanged.

Owner: Thin Walls (`fumblesneeze.thinwalls`, `mods/ThinWalls`).

#### Scenario: Click inside or outside a thin edge
- **WHEN** the player clicks within either adjacent-cell half of the displayed rectangle
- **THEN** that thin segment is a selectable candidate, independent of its owner orientation
- **AND** clicking the remaining owner-cell space or beyond the segment ends does not select it

#### Scenario: Rotate and change construction phase
- **WHEN** north, east, south or west thin walls or doors exist as blueprints, frames or buildings
- **THEN** every phase uses the same edge-centered bracket and hit envelope

### Requirement: Keep native selection behavior outside the thin hit correction

The correction SHALL retain ordinary targets and their relative priority, native eligibility,
and repeated-click cycling. It SHALL inspect only the clicked neighborhood, not scan or cache
the map. Non-selection targeting queries SHALL remain unchanged.

#### Scenario: Multiple edges and nearby furniture
- **WHEN** multiple differently oriented edges occupy one cell beside an ordinary building
- **THEN** each thin edge enters the click candidates only where its rectangle contains the pointer
- **AND** the ordinary building remains selectable and overlapping candidates can still be cycled

#### Scenario: Unselectable edge or no nearby thin walls
- **WHEN** a thin structure is rejected by native selection eligibility, or there is no nearby edge
- **THEN** the rejected structure is not introduced and unrelated native candidates remain unchanged
