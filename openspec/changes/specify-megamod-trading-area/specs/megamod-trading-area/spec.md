# Dedicated trading area

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
**Origin: original**, S040. Specification only; S039 owns eligible party/lifecycle behavior.
The specific defaults below are proposed owned behavior, not additional confirmed interview answers.

## ADDED Requirements

### Requirement: Each map has one editable dedicated trading area
The mod SHALL provide one dedicated Trading area per map, initially empty. Its footprint SHALL be
editable, including separate painted parts; "fixed" SHALL not mean immutable cells or a preset
size. It SHALL not be a pawn allowed area, require pawn assignment, consume an allowed-area slot or
replace any other area. Drawing it SHALL require no physical marker, construction work, material
cost or research. No per-faction areas, area-priority list or manual party assignment is introduced.

#### Scenario: First use on a map
- **WHEN** the player opens Trading area on a map with no prior footprint
- **THEN** it is empty and can be drawn immediately without constructing an object or assigning pawns
- **AND** existing allowed areas and their available slots are unchanged

#### Scenario: Add a second separate patch
- **WHEN** the player paints another patch away from the first one
- **THEN** both belong to the same map's Trading area without creating a second competing area

### Requirement: Area tools expose contextual editing and status
The mod SHALL expose Trading area through Architect's area tools. Opening it SHALL show its
footprint, current mode and contextual **Expand**, **Erase** and **Clear all** gizmos, including when
the footprint is empty. The controls SHALL affect only that map's trading area. The feature SHALL
add no bottom tab, global behavior-toggle button, generic allowed-area picker or mod-settings row
for these entity actions. Labels and status SHALL remain understandable without color alone.

#### Scenario: Reopen an existing footprint
- **WHEN** the player opens Trading area through Architect
- **THEN** the current map's painted cells and editing gizmos are available without selecting a marker

#### Scenario: Start again after clearing
- **WHEN** the footprint has been cleared and the player reopens the tool
- **THEN** Expand remains available and the empty state is explained

### Requirement: Painting edits only the intended cells
Expand SHALL add discovered in-bounds map cells and Erase SHALL remove painted cells. A completed
gesture SHALL commit its previewed changes; cancelling an unfinished gesture SHALL leave the prior
committed footprint unchanged. Repainting existing cells or erasing absent cells SHALL be harmless.
Clear all SHALL empty only the current map's trading footprint. Painting SHALL not build, mine,
unfog, demolish, clean, roof or change permissions. The area SHALL be allowed to overlap storage,
growing zones and other areas without altering their cells, contents, configuration or assignments.

Blocked or currently unusable painted cells SHALL remain in the footprint until explicitly erased;
painting a cell SHALL not certify that visitors can use it. Erase SHALL remain possible even when
a painted cell is now obstructed or hidden. Drawing outside the map or into undiscovered cells
SHALL not reveal hidden terrain or create hidden designations.

#### Scenario: Paint over storage and a wall
- **WHEN** a gesture covers a discovered stockpile and a wall cell
- **THEN** the trading footprint includes those cells without changing the stockpile or demolishing the wall
- **AND** the wall cell is still unusable for gathering while ordinary rules reject it

#### Scenario: Cancel a stroke
- **WHEN** the player previews an expansion and cancels before completing the gesture
- **THEN** no part of that unfinished stroke changes the committed footprint or party destination

#### Scenario: Erase an obstructed cell
- **WHEN** a previously painted cell has become blocked and the player erases it
- **THEN** that cell leaves the footprint without changing the obstruction

#### Scenario: Clear all
- **WHEN** the player activates Clear all
- **THEN** only this map's trading footprint becomes empty and S039's ordinary fallback applies
- **AND** other zones, areas, buildings, source markers and maps remain unchanged

### Requirement: Gathering uses actual painted cells
Eligible parties SHALL use an actually painted, currently usable cell as their ordinary gathering
location. The mod SHALL retain a still-usable current location rather than continually change it.
It SHALL not substitute the center of the footprint's outline if that cell is unpainted or unusable.
Ordinary gathering members SHALL physically reach usable painted cells before settling into area
idle behavior; stopping just outside because the destination is nearby SHALL not count as using
the area. Native movement speeds, party roles and S039's safety/lifecycle rules SHALL remain intact.

#### Scenario: Hollow or L-shaped area
- **WHEN** the footprint's center falls in an unpainted gap
- **THEN** the party uses a valid painted location instead of treating the gap as part of the area

#### Scenario: Near the area but outside it
- **WHEN** an arriving gathering member is close to the destination but still outside the footprint
- **THEN** it continues ordinary physical movement to a usable painted cell before settling there,
  unless its role or a higher-priority need/emergency takes precedence

#### Scenario: Existing location stays usable
- **WHEN** an unrelated area edit leaves the party's current gathering location usable
- **THEN** the edit alone does not repeatedly move the party between different valid cells

### Requirement: Ordinary idling respects the footprint without confining other work
The mod SHALL choose eligible gathering members' ordinary idle destinations from usable painted
cells within their normal gathering range. Normal travel paths, following/escort roles, legitimate
needs, S027 deliveries, combat, rescue and departure SHALL retain their own movement rules, including
travel or positions outside the footprint. The footprint SHALL not grant new access, imprison
visitors, constrain unrelated pawns or become an invisible wall. When ordinary gathering resumes,
the current usable footprint SHALL apply without restarting the visit.

#### Scenario: Idle beside an irregular edge
- **WHEN** an eligible member chooses a new ordinary idle destination near the footprint's edge
- **THEN** that destination is in a usable painted cell within its normal gathering range

#### Scenario: Path leaves and re-enters the area
- **WHEN** the usable path between two painted locations runs outside the footprint
- **THEN** normal movement can use that path without adding unpainted cells to the footprint

#### Scenario: Courier or carrier leaves the painted cells
- **WHEN** an S027 courier delivers goods or a carrier follows its trader near an edge
- **THEN** its legitimate role is not cancelled merely for crossing or standing outside the area
- **AND** delivery quantities, following ownership and ordinary departure remain unchanged

### Requirement: Unusable cells and small footprints terminate safely
Gathering selection SHALL respect current native safety, standability, reachability, occupancy and
the relevant party members' actual permissions. Separate painted parts SHALL be candidates only
when the party can use them; an inaccessible part SHALL not invalidate another usable part or split
members across incompatible routes. No arbitrary minimum footprint size or fixed pawn-per-cell
capacity SHALL be imposed. The mod SHALL not reserve the whole area against other parties.

When no new idle movement is possible, a member already safely gathering in a valid painted cell
SHALL be allowed to wait normally. If the party has no usable gathering location, S039's native
fallback SHALL apply. A lone painted cell, temporary crowding or a fully blocked area SHALL not
cause endless searching, repeated failing jobs, teleports or invented movement permissions.

#### Scenario: One usable cell
- **WHEN** the footprint contains a single cell that the party can use under ordinary rules
- **THEN** gathering can use it and normal waiting does not require finding a different painted cell

#### Scenario: Disconnected parts with different access
- **WHEN** one painted part is inaccessible to the party but another is usable
- **THEN** a usable location is selected without forcing members through the inaccessible part

#### Scenario: Nothing usable remains
- **WHEN** every painted destination is blocked, unsafe or inaccessible for the party
- **THEN** native gathering/failure behavior takes over, the unusability is visible and searching terminates

#### Scenario: Two parties share a small footprint
- **WHEN** two eligible parties consider the same limited space
- **THEN** each respects current native occupancy and movement rules without claiming exclusive use
  of every painted cell or bypassing checks because the other party was evaluated earlier

### Requirement: Area edits update ordinary gathering after commitment
Committed changes SHALL reach existing eligible parties at their next ordinary gathering decision
under S039. Previewing a stroke SHALL not move parties. Removing the current target SHALL cause a
usable replacement or ordinary fallback; an unchanged valid target SHALL not restart jobs. Clearing
the footprint SHALL not leave a deleted location permanently forced. Protected needs/delivery jobs,
emergencies and departures SHALL keep their normal precedence, visit progress and gifts.

#### Scenario: Erase the current location during a visit
- **WHEN** the player commits erasure of the gathering cell and other usable cells remain
- **THEN** that same party adopts a usable remaining location at its next ordinary gathering decision
- **AND** its stay, stock and pending legitimate transactions are preserved

#### Scenario: Edit while visitors leave
- **WHEN** the player expands or clears the area while a party is departing
- **THEN** that party continues its departure instead of beginning another visit

### Requirement: The view distinguishes footprint from actual availability
The area view SHALL distinguish an empty footprint, a drawn footprint awaiting eligible parties,
a particular party's inability to use it, and unavailable integration. It SHALL explain actionable
reasons using player-visible information without claiming all parties can reach every painted cell.
An unusable area with ordinary fallback SHALL not generate repeated messages or persistent alerts.
If S039 redirection or required painted-cell behavior is unavailable, the affected route SHALL be
visibly inactive; merely drawing cells SHALL not be presented as successful party control.

#### Scenario: No party is present
- **WHEN** a footprint is drawn before visitors arrive
- **THEN** the view shows it as designated, without claiming tested accessibility for every possible party

#### Scenario: Original provider suppresses routing
- **WHEN** original PL Trading Spot is active under S039's co-installation policy
- **THEN** the footprint can be retained/edited while the view explains why owned routing is inactive
- **AND** source objects and settings are left untouched

#### Scenario: Painted-cell integration is unsupported
- **WHEN** safe ordinary idle containment is unavailable after a game/provider change
- **THEN** the affected area-routing behavior is withheld with a bounded startup warning and native fallback
- **AND** it does not silently promise area control while applying only point-based redirection

### Requirement: Area state and unfinished edits stay with their map and save
Committed footprints SHALL persist independently for each map/save. New or missing map state SHALL
start empty, and loading one save SHALL not copy another save's footprint or edit mode. Switching
maps, leaving map view or unloading the game SHALL cancel unfinished gestures before they can
commit to a different context. Removing a map SHALL discard its editor/routing references. Moving
pawns, buildings or caravans to another map SHALL not copy that map's footprint automatically.
Area cells SHALL remain gameplay state with no global-default or Use global controls.

#### Scenario: Switch maps during a stroke
- **WHEN** the player starts painting on map A and switches to map B before completing the gesture
- **THEN** the unfinished edit changes neither map and B displays only its own committed footprint

#### Scenario: Save and reload two different footprints
- **WHEN** maps A and B have different committed footprints and the game is saved and loaded
- **THEN** each footprint returns to its own map without duplicating areas or changing pawn assignments

#### Scenario: Enter the world view
- **WHEN** the player leaves the map view with an unfinished area gesture
- **THEN** that gesture is cancelled and later returning to a map does not commit stale input
