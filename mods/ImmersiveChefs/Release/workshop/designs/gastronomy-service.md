# Dinner service, from order to table

- Showcase ID: `gastronomy-service`
- Design-record version: `1`
- Owner: `fumblesneeze.immersivechefs`
- Status: `draft`
- Intended crop: `1280x720, fixed view containing restaurant and adjacent kitchen`
- Colony brief: `Established temperate industrial colony, six staff, one warm restaurant and an expanded professional kitchen beside the colony's central path.`
- Exact presentation packages: `brrainz.harmony -> ludeon.rimworld -> orion.hospitality -> orion.cashregister -> orion.gastronomy -> fumblesneeze.immersivechefs`
- Native workflow: `Hospitality guest takes a Gastronomy dining spot -> waiter receives the order and supplies cutlery -> lead cook begins the meal and linked assistants work -> the same waiter serves the finished meal`

## References

| ID | Role in this scene | Inspected evidence | Eligible class |
|---|---|---|---|
| R01 | public/back-of-house edge | finished kitchen beside formal hall | recurrent |
| R02 | kitchen circulation | freezer buffer and clean worker island | recurrent |
| R07 | restaurant composition | clustered tables, counter and visible kitchen edge | recurrent |
| R20 | colony context | cantina as a public landmark | archetype |
| R21 | optional-mod ecosystem | Hospitality/Gastronomy restaurant inside a working colony | archetype |

## Applied rules

| Rule ID | Class | Sources/contract | Decision in this scene |
|---|---|---|---|
| public-service-edge | recurrent | R01/R07/R20/R21 | dining faces the colony path; register and waiter stand at the staff boundary |
| short-food-path | mechanical/recurrent | Gastronomy stock radius; R02 | pass shelf joins kitchen to dining without crossing cook interaction cells |
| simultaneous-line | mechanical | Immersive Chefs linked station contracts | stove and four linked stations retain distinct interaction cells in one visible lane |
| restaurant-dressing | recurrent | R01/R07/R20 | warm wood, stone accents, plants, art, lamps and several usable tables |

## Adjacency graph

`raw freezer --high-frequency--> prep --high-frequency--> stove --service--> pass shelf --high-frequency--> waiter --service--> guest table`

Guest circulation remains in dining; ingredient hauling and assistant movement remain behind the register boundary.

## Planned zones and placements

| Zone/object | Bounds or relation | Exact Def/rotation/material | Rule IDs | Rationale |
|---|---|---|---|---|
| restaurant | west 11x11 interior | granite walls, wood floor, Table2x2c and DiningChair/wood | public-service-edge, restaurant-dressing | reads as a colony venue instead of a test table |
| register boundary | shared doorway/pass | CashRegister_CashRegister/south/wood | public-service-edge | real Gastronomy coverage with a short waiter route |
| clean kitchen | east 13x11 interior | sterile tile, ElectricStove/south/steel | short-food-path, simultaneous-line | gives five cooks unobstructed work cells |
| support arc | around stove link radius | ImmersiveChefs_SauceStation, MeatStation, VegetableStation, PastryStation | simultaneous-line | the full roster stays visible while working |
| pass shelf | shared wall beside register | Shelf/east/steel, Fine-meal filter | short-food-path | waiter can collect output without crossing the line |

## Variation and history

The dining room retains older wood flooring and mixed chairs; the newer kitchen uses sterile tile and steel equipment. One short spare conduit branch suggests the restaurant was expanded after the colony core.

## Rejected drafts and deviations

| Draft/evidence | Rejection or deviation | Resulting rule/change |
|---|---|---|
| pending first live frame | none yet | inspect fixed-crop readability before activation |

## Live review

- Exact build/package identity: `pending live review`
- Exact process/evidence directory: `pending live review`
- Player action observed: `pending live review`
- Visible result observed: `pending live review`
- Geometry/material/traffic observations at final crop size: `pending live review`
- Remaining caveats: `pending live review`
