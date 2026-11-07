# Considerate idle wandering

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
S024 Common Sense replacement; specification only. Both preferences default off. These are idle
destination rules, not the broader S030 temperature feature or a replacement for pen/herd pathfinding.

## ADDED Requirements

### Requirement: Idle shelter choice uses the pawn's real hazard susceptibility
With weather-aware wandering enabled, idle player-faction pawns on a home map without an assigned duty SHALL prefer permitted shelter when exposed to unenjoyable weather they care about, harmful toxic fallout or vacuum they cannot safely tolerate.
Use actual current immunity/protection for the relevant hazard. Fully immune animals are not sent
indoors merely because fallout exists; lacking a mood need alone does not protect a susceptible
animal from toxicity. Mechs/nonbreathing pawns retain native immunity. Roofed vacuum is not safe air.
The destination must actually protect against the relevant exposure and have a valid route suitable
for escaping the pawn's present danger or remaining sheltered, as distinguished below;
home/allowed areas, pens/rooms, doors and provider restrictions remain binding.

#### Scenario: Susceptible and immune animals in fallout
- **WHEN** idle susceptible and fully toxin-immune player animals are outside during fallout
- **THEN** the susceptible animal can seek valid shelter while fallout alone does not redirect the immune animal.

#### Scenario: Roof without pressure
- **WHEN** a vacuum-susceptible pawn considers a roofed but depressurized room
- **THEN** the roof alone does not qualify that room as protective shelter.

### Requirement: Shelter searches and sheltered movement terminate safely
Shelter selection SHALL use bounded searches and keep idle movement from repeatedly choosing the same unsafe excursion when no permitted shelter or path exists.
The proposed search considers at most 100 regions and nearby shelter destinations within 7 cells of
the selected protected region point. An exposed pawn can jog toward confirmed safe shelter; it does
not gain movement speed beyond ordinary jogging. An already sheltered pawn's idle trip is bounded
to 400–800 ordinary movement-cost ticks and no more than 200 exposed movement-cost ticks, and ends
at a location safe for its actual relevant hazard. If no suitable move exists, remain at the current
safe location or yield to ordinary urgent survival behavior. Do not replace a forced move, active
work, rescue, combat or duty. A sheltered susceptible pawn does not take an optional trip through
harmful vacuum/toxins merely because the path is short. An already exposed pawn may cross the hazard
it is escaping on a direct permitted route toward confirmed shelter; requiring every escape cell to
be already protected must not strand it outside. Ordinary traversal/danger restrictions still apply,
no immunity is granted, and the route cannot introduce an unrelated hazard solely for idle movement.

#### Scenario: Animal in a single-cell room
- **WHEN** an idle confined animal has no legal alternative destination
- **THEN** selection ends without an endless search, repeated immediate jobs or an escape outside its permitted space.

#### Scenario: No safe shelter is reachable
- **WHEN** the search cannot find a valid safe destination
- **THEN** it stops without repeatedly forcing the same dangerous walk or blocking higher-priority survival work.

#### Scenario: Escape from existing fallout exposure
- **WHEN** a susceptible pawn already outside during fallout can reach protective shelter only by crossing a short exposed route that its ordinary danger rules permit
- **THEN** it can take that route into shelter without treating existing exposure as an absolute ban on escape
- **AND** a pawn already sheltered does not take the same exposed route merely to wander.

### Requirement: Considerate wandering checks the destination room
With considerate wandering enabled, nonhostile idle pawns SHALL avoid choosing another pawn's bedroom, a hospital, prison, freezer storeroom or containment room merely as a wandering destination.
Bedroom ownership is checked for the destination, not the room the pawn is leaving. A pawn can idle
in its own bedroom subject to ordinary access. Required travel through these rooms, assigned work,
visits, bed use and explicit player destinations remain governed by their normal rules. No hostile
pawn gains new access, and absent optional room types do not disable other choices.

#### Scenario: Owner leaves a different bedroom
- **WHEN** a pawn is currently in somebody else's room and its own bedroom is an eligible idle destination
- **THEN** destination ownership permits considering its own room and still excludes other owners' bedrooms.

#### Scenario: Work route crosses a hospital
- **WHEN** the ordinary route to assigned work passes through a hospital
- **THEN** the idle-destination preference alone does not forbid that necessary travel.

### Requirement: Freezer classification remains an informative native room role
A qualifying storeroom with an inward-facing cooler configured below 0°C SHALL be able to display a freezer-room role and participate in considerate idle-destination avoidance.
The room must already qualify as storage; the cooler's passable cold side must serve that room.
Configuration expresses intended freezer use even during temporary power loss, not proof the room
is currently frozen. Do not classify every cold bedroom or unconnected cooler as a freezer. Other
native room-role competition remains in force; the label does not alter temperature, spoilage, storage
filters or access. Supported custom coolers require actual equivalent capability, not a name match.

#### Scenario: Freezer loses power
- **WHEN** a qualifying storeroom's configured cooler temporarily loses power
- **THEN** its intended freezer classification can remain, without claiming that food is still frozen.

#### Scenario: Cooler faces another room
- **WHEN** a cooler's cold side serves a different room or the room is not a storeroom
- **THEN** that cooler does not by itself give the room the freezer role.
