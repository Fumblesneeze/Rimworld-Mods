## ADDED Requirements

### Requirement: Half-resistance conduction across completed solid thin walls

Owner: Thin Walls (`fumblesneeze.thinwalls`, `mods/ThinWalls`). A completed solid Thin Wall SHALL
have half a Core regular wall's thermal resistance per segment: twice its conductance for equal
temperature differences, boundary length and air-cell volumes. Core's material-independent policy
SHALL apply to every accepted Stuff, including mod-added materials. Existing Thin Door exchange
SHALL remain separate at native door rates, and ordinary walls and roof/weather calculations SHALL
remain unchanged. This replaces the older perfect-insulation interpretation.

#### Scenario: Equal-size hot and cold rooms exchange heat
- **WHEN** equal-size roofed rooms separated by one solid thin edge have unequal temperatures
- **THEN** heat flows from hot to cold at twice the normal-wall coefficient, equally and oppositely,
  without merging their native Room identities or jumping immediately to outdoor temperature

#### Scenario: Small pockets and unequal room sizes
- **WHEN** different-volume rooms or a one-cell U-shaped pocket exchange heat through thin edges
- **THEN** temperature changes respect their actual air-cell counts, conserve energy between finite
  rooms, and do not overshoot equilibrium; multiple edges on one cell remain independent

#### Scenario: Outside, doors, unfinished walls and non-boundaries
- **WHEN** one side is outdoors, or a segment is a door, blueprint, frame, or both sides share a room
- **THEN** outdoors stays a reservoir, doors use only their existing door calculation, unfinished
  edges do not insulate, and a same-room solid edge adds no heat; absent air rooms are not invented

### Requirement: Native room lifecycle and simulation remain authoritative

Thin edges SHALL create native enclosed rooms upon construction and remerge them upon breach.
The same volumes and temperatures SHALL drive native heaters and room temperature displays, persist
through save/load, and preserve Core's roof heat loss without granting roof support. Thermal work
SHALL scale with completed owned edges, with no full-map or duplicate room cache.

#### Scenario: Build, heat, reload and breach
- **WHEN** the player completes a thin enclosure, runs a native heat source, saves/loads and
  deconstructs an edge
- **THEN** the enclosure becomes an indoor room, visibly warms during simulation while exchanging
  finite heat across its walls, reloads with its room temperature, and rejoins surrounding air after breach

#### Scenario: Cardinal and no-edge regression
- **WHEN** horizontal and vertical boundaries are constructed, or the final thin edge is removed
- **THEN** all cardinal descriptions use the same physical-edge coefficient, opposite descriptions
  do not double-count, and a map without solid thin walls does no thin-wall thermal work
