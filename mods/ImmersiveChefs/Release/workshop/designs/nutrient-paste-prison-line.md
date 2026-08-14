# Prison chow still needs a plate

- Showcase ID: `nutrient-paste-prison-line`
- Design-record version: `1`
- Owner: `fumblesneeze.immersivechefs`
- Status: `draft`
- Intended crop: `1280x720, fixed view containing cell doors, common room, plate shelf and dispenser`
- Colony brief: `Established industrial prison wing for four named prisoners, durable and orderly rather than luxurious, with a staff-only raw-food service room.`
- Exact presentation packages: `brrainz.harmony -> ludeon.rimworld -> ocarina.prisonerjumpsuits -> fumblesneeze.immersivechefs`
- Native workflow: `ordinary prisoner food search chooses a plate -> prisoners queue at the powered nutrient-paste dispenser -> paste is dispensed onto each plate without cutlery -> featured conscious prisoner eats -> native Needs thoughts reveal Ate without cutlery`

## References

| ID | Role in this scene | Inspected evidence | Eligible class |
|---|---|---|---|
| R08 | prison service wall | dispenser facing prisoners with secured supply side | recurrent |
| R09 | common-room grammar | dispenser, outside hoppers, tables and chairs | recurrent |
| R10 | compact utility relation | dispenser and power in an early food room | recurrent |
| R19 | prison iteration | service boundary and common-room circulation | recurrent |

## Applied rules

| Rule ID | Class | Sources/contract | Decision in this scene |
|---|---|---|---|
| secured-dispenser-wall | mechanical/recurrent | Core dispenser contract; R08/R09/R10 | interaction side faces prison, hoppers stay in staff room |
| plate-before-paste | mechanical | Immersive Chefs dispenser integration | critical plate shelf is one cell before the dispenser queue |
| institutional-common-room | recurrent | R08/R09/R19 | repeated tables, bright lamps, concrete/stone floor and visible cell doors |
| prisoner-role-signal | archetype | requested Prison Jumpsuits ecosystem | each adult prisoner wears a risk-coloured jumpsuit where the installed Def permits |

## Adjacency graph

`prison cells --meal-seeking--> plate shelf --high-frequency--> nutrient dispenser --carry--> common-room seat`

`staff freezer --secured-supply--> hopper --feed--> nutrient dispenser`

## Planned zones and placements

| Zone/object | Bounds or relation | Exact Def/rotation/material | Rule IDs | Rationale |
|---|---|---|---|---|
| prison common room | south 13x9 interior | granite walls, concrete floor, Table2x2c/wood | institutional-common-room | enough space for a visible four-pawn line and dining |
| cell/barracks doors | west edge | Door/east/steel and prison sleeping furniture | institutional-common-room | communicates a real prison wing without labels |
| dispenser wall | north divider | NutrientPasteDispenser/south/steel | secured-dispenser-wall | correct interaction side faces prisoners |
| staff supply | north 5x5 room | Hopper/steel plus raw rice | secured-dispenser-wall | functional but inaccessible feedstock |
| plate shelf | beside queue origin | Shelf/east/steel, clean Plate only | plate-before-paste | makes the plate pickup legible before dispensing |

## Variation and history

The common room uses a repaired granite-and-slate floor strip and mismatched wooden seating, suggesting an older wing upgraded with a modern dispenser.

## Rejected drafts and deviations

| Draft/evidence | Rejection or deviation | Resulting rule/change |
|---|---|---|
| initial package inspection | the requested item was not initially installed, so no apparel placeholder was permitted | owner-visible Workshop item `3402467889` was acquired read-only; use exact 1.6 package `ocarina.prisonerjumpsuits` and its Blue/Orange/Red apparel Defs |

## Live review

- Exact build/package identity: `pending live review`
- Exact process/evidence directory: `pending live review`
- Player action observed: `pending live review`
- Visible result observed: `pending live review`
- Geometry/material/traffic observations at final crop size: `pending live review`
- Remaining caveats: `pending live review`
