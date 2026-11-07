# Mining plans

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`). Target: **1.7 beta**.
Specification draft; nothing implemented. Source: Allow Tool, S008 / P1-08.

## ADDED Requirements

### Requirement: Extend mining orders along connected ore
The mod SHALL offer connected-ore mining from the Mine context menu. Revealed selected resource
rocks and revealed resource rocks already marked for mining seed expansion through same-kind ore
touching in any of the eight neighboring cells, including diagonal connections. Connected hidden
ore can receive mining orders without revealing its cells or identity; wholly hidden unseeded veins
are not discovered. Different ore kinds and ordinary rock do not bridge the expansion. Existing
orders are retained without duplication; reaching an edge or an invalid cell terminates that branch.

#### Scenario: Diagonal vein and map boundary
- **WHEN** the player uses connected mining on revealed ore touching more same-kind ore diagonally at a map edge
- **THEN** the connected ore receives mining orders without errors or a nonterminating action
- **AND** a separated vein or one connected only through another rock type remains unmarked

#### Scenario: No hidden seed discovery
- **WHEN** the map contains a revealed designated vein and a wholly hidden unseeded vein
- **THEN** connected mining expands only from the revealed seed and does not reveal fogged terrain

### Requirement: Preview and commit an adjustable strip-mine grid
The mod SHALL offer Strip mine in Orders and from the Mine context menu. Dragging a rectangle,
including one larger than 50×50 cells, previews one-cell-wide mining lines clipped to the map.
Default horizontal and vertical gaps are five untouched cells between lines; each gap is independently
adjustable from 1 to 50. The current-plan editor supports steps of one, Shift steps of five and mouse
wheel adjustment, showing the revised preview before acceptance. Valid and invalid mining cells are
visually distinct; hidden cells can receive planned orders without revealing their contents. Accept
creates ordinary mining orders; Cancel/Escape creates none and restores the previous plan parameters.
Mining still requires eligible workers, access and ordinary mining work, with no instant resource creation.

#### Scenario: Preview does not excavate
- **WHEN** the player drags a large strip-mine rectangle and changes a gap from five to ten
- **THEN** the preview updates with ten untouched cells between those lines
- **AND** no mining order or excavation occurs until acceptance

#### Scenario: Cancel and map change
- **WHEN** the player cancels the plan, changes map, leaves the map view or loads another save before accepting
- **THEN** the unfinished preview is discarded without applying orders to either map
- **AND** cancellation does not overwrite the previously accepted plan parameters

### Requirement: Separate current geometry from defaults
The mod SHALL initially align strip-mine lines to the drag-start cell and show the plan editor on
release. Fixed alignment reuses the last accepted grid origin on that map. Shift on release inverts
whether the editor opens or the current parameters are applied immediately. Current geometry and
last accepted origin belong to the map's planning state, persist with that save and never rewrite
global defaults. Initial gaps, initial alignment mode and editor-opening preference are common
global/save settings; a new map with no retained plan starts from their effective values. The
draggable editor remains visible within the screen and remembers its global UI position separately.

#### Scenario: Adjacent rectangles line up
- **WHEN** the player accepts one grid, switches to fixed alignment and draws an adjacent rectangle
- **THEN** its lines use the same map origin and line up with the first grid
- **AND** another map's saved origin is unchanged

#### Scenario: Quick apply and configuration scope
- **WHEN** Show plan editor is on and the player releases a drag while holding Shift
- **THEN** the current plan parameters create ordinary mining orders without opening the editor
- **AND** changing a plan's geometry does not change global defaults or another save
