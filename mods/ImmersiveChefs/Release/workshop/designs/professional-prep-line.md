# Prep ahead, cook fast, keep it hot

- Showcase ID: `professional-prep-line`
- Design-record version: `1`
- Owner: `fumblesneeze.immersivechefs`
- Status: `draft`
- Intended crop: `1280x720, fixed view containing freezer buffer, prep lane, stove and hot pass`
- Colony brief: `Wealthy industrial kitchen serving an adjacent dining hall, with two named chefs, a compact cold store and a recently added Thermodynamics hot pass.`
- Exact presentation packages: `brrainz.harmony -> ludeon.rimworld -> mlie.dthermodynamicshotmeals -> fumblesneeze.immersivechefs`
- Native workflow: `repeat prep bill keeps one chef at the prep station -> second chef takes prepared food and meat from cold storage -> ordinary Fine-meal bill cooks quickly -> finished meal moves to Thermodynamics hot storage at the pass`

## References

| ID | Role in this scene | Inspected evidence | Eligible class |
|---|---|---|---|
| R02 | prep/freezer/cook flow | refrigerated inputs around clean worker island | recurrent |
| R03 | production scale | active district and stocked freezer | recurrent |
| R11 | cold-service wall | fridge buffer between production and consumers | recurrent |
| R19 | meal output buffer | diners access output without entering kitchen | recurrent |

## Applied rules

| Rule ID | Class | Sources/contract | Decision in this scene |
|---|---|---|---|
| dual-worker-lanes | mechanical/recurrent | prep and DoBill work cells; R02 | prep and line cooks have distinct parallel interaction cells |
| cold-buffer | mechanical/recurrent | ingredient search; R02/R11/R19 | raw meat and prepared food sit between freezer and workstations |
| linked-station-arc | mechanical | Immersive Chefs link and assistant contracts | support stations surround stove without blocking workers |
| hot-pass-output | mechanical/recurrent | Thermodynamics hot storage; R11/R19 | final meal buffer sits at dining edge after cooking |

## Adjacency graph

`raw freezer --high-frequency--> prep station --output--> prepared shelf --ingredient--> stove --output--> hot pass --service--> dining`

The prep worker and line cook use parallel paths and never share the same interaction cell.

## Planned zones and placements

| Zone/object | Bounds or relation | Exact Def/rotation/material | Rule IDs | Rationale |
|---|---|---|---|---|
| cold store | west 5x9 interior | granite wall, sterile tile, RimFridge/steel if available or shelf in refrigerated room | cold-buffer | shows distinct meat and vegetable inputs |
| prep lane | central-west | ImmersiveChefs_PrepStation/south | dual-worker-lanes | continuous work stays visible beside output shelf |
| cook line | central-east | ElectricStove/south/steel | dual-worker-lanes, linked-station-arc | ordinary fine meal production is the focal action |
| assistant stations | stove link radius | Sauce/Meat/Vegetable/Pastry stations | linked-station-arc | simultaneous professional roster remains readable |
| hot pass | east dining boundary | `DHeatLamp`/south/steel | hot-pass-output | the installed 1.6 Def is a powered two-cell heated storage pass, so finished meals enter the other mod's ordinary temperature path |

## Variation and history

The freezer retains rough granite, while the expanded cooking line uses sterile tile and steel. A wood-trim pass and one plant soften the public edge without making the kitchen decorative.

## Rejected drafts and deviations

| Draft/evidence | Rejection or deviation | Resulting rule/change |
|---|---|---|
| guessed “hot plate” wording | the installed mod exposes no such Def | inspect the installed 1.6 XML and use exact `DHeatLamp`, whose own label is “heat lamp” |

## Live review

- Exact build/package identity: `pending live review`
- Exact process/evidence directory: `pending live review`
- Player action observed: `pending live review`
- Visible result observed: `pending live review`
- Geometry/material/traffic observations at final crop size: `pending live review`
- Remaining caveats: `pending live review`
