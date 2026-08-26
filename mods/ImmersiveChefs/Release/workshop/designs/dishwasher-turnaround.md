# From a mountain of dishes to a tidy kitchen

- Showcase ID: `dishwasher-turnaround`
- Design-record version: `1`
- Owner: `fumblesneeze.immersivechefs`
- Status: `draft`
- Intended crop: `1280x720, fixed view containing dining tables, service lane and clean shelf`
- Colony brief: `Established arid industrial colony after breakfast rush, one compact dining hall joined to a newer sterile kitchen and outdoor utility lane.`
- Exact presentation packages: `brrainz.harmony -> ludeon.rimworld -> imranfish.xmlextensions -> syrchalis.processor.framework -> dubwise.dubsbadhygiene -> mehni.pickupandhaul -> fumblesneeze.immersivechefs`
- Native workflow: `player enables the named cleaner's ordinary cleaning work -> Pick Up And Haul groups nearby dirty settings -> Doing dishes loads the Processor dishwasher -> native cycle cleans them -> ordinary hauling returns the clean batch to the kitchen shelf`

## References

| ID | Role in this scene | Inspected evidence | Eligible class |
|---|---|---|---|
| R02 | compact clean kitchen | worker island, buffers and separated dining route | recurrent |
| R03 | lived-in food district | mixed furniture, stocked room and visible use | recurrent |
| R11 | service-wall storage | short kitchen/dining buffer | recurrent |
| R19 | clean service path | kitchen airlock and meal shelf | recurrent |
| RR-20260814 | 5,000 metadata / 2,199 parsed Real Ruins layouts | exact table/workstation/conduit/utility placement statistics and 20 inspected schematics | recurrent/archetype |

## Applied rules

| Rule ID | Class | Sources/contract | Decision in this scene |
|---|---|---|---|
| return-at-dining-edge | candidate/mechanical | R02/R03; dining lifecycle | dirty settings begin near tables and service exit |
| wet-service-lane | mechanical | Dubs plumbing and Processor holder contracts | dishwasher, water line and clean shelf share one kitchen-side lane |
| dense-batch-radius | mechanical | Pick Up And Haul integration | six dirty units remain close enough for one batch collection |
| established-context | recurrent | R02/R03/R11/R19 | stove, prep bench, ingredients, tables, lamps and plants remain visible |

## Adjacency graph

`dining tables --dirty-return--> service exit --batch-haul--> dishwasher --clean-output--> clean shelf --supply--> kitchen`

The utility path does not cross the stove or prep-station interaction cells.

## Planned zones and placements

| Zone/object | Bounds or relation | Exact Def/rotation/material | Rule IDs | Rationale |
|---|---|---|---|---|
| dining | west room with eight visible seats | Table2x4c/north/wood with four chairs along each long side, wood floor | return-at-dining-edge, established-context, RR-20260814 | the finalized 2x4 footprint supports all eight seats and retains circulation on both short ends |
| kitchen | east room, two opposed wall runs | north-facing Dishwasher, 3x1 Dubs KitchenSink and 3x1 PrepStation on the north wall; 3x1 ElectricStove/south on the south wall; sterile tile | established-context, RR-20260814 | the loaded footprints keep the wet/prep line together while the stove faces a separate two-cell central aisle; each worker has a distinct interaction and approach cell |
| dishwasher lane | inside the north kitchen wall | ImmersiveChefs_Dishwasher/north, Dubs hidden pipe under the shared service wall | wet-service-lane | ordinary 2x1 appliance remains wholly inside the room rather than replacing a wall |
| clean shelves | south kitchen wall | two Shelf/north/wood buildings, clean-only critical filters | wet-service-lane | eight distinct stocked/washed stacks exceed one shelf's six-slot capacity, so the paired wall run is the honest endpoint |
| dirty cluster | dining exit and cook line | Plate/wood-steel-granite-silver, Cutlery/wood, Cookware/steel | dense-batch-radius | visually varied post-rush overflow without blocking doors |
| exterior utility yard | outside kitchen service wall | WaterTowerS/native orientation; ChemfuelPoweredGenerator/native non-rotatable orientation | RR-20260814, exact Defs | keeps water storage and fueled generation out of occupied rooms |
| utility routes | north, south and east wall cells plus service corridor | PowerConduit plus Dubs pipe | RR-20260814 | every powered wall run has a nearby concealed transmitter; cables/pipes disappear under walls and avoid arbitrary floor crossings |

## Variation and history

Older wood dining furniture contrasts with a sterile-tile kitchen retrofit. Granite walls, one lower-corner plant
pot, one small dining sculpture and one service shelf keep the small colony believable without decorative clutter.

## Rejected drafts and deviations

| Draft/evidence | Rejection or deviation | Resulting rule/change |
|---|---|---|
| recovered 2026-08-13 rehearsal | wilderness-only bootstrap, object cluster and an unconnected improvised generator | rejected the scene; rebuilt it as adjacent dining, kitchen and utility rooms with native chemfuel power and Dubs plumbing |
| 2026-08-13 four-frame draft | chairs spaced around a table too small for them; indoor water tower; rotated non-rotatable generator; open-floor cables; scattered benches | rejected completely; no asset or coordinate reuse. Added exact footprint/fixed-rotation/utility-yard/wall-route/bench-run gates and replaced the 12-blueprint shortcut with RR-20260814 |
| 2026-08-14 RR first live draft (`20260814T000330522Z`) | corrected structure but the 12-cell-deep kitchen remained visually empty; service run lacked sink/counter rhythm; paved utility yard had no enclosure | rejected for publication; narrowed kitchen, added exact Dubs sink to the wet wall and fenced the exterior service yard before any capture work |
| 2026-08-14 RR second live draft (`20260814T000606144Z`) | exact table/service/utility contracts held, but the whole settlement still read as one detached scenario rectangle in wilderness | rejected for publication; add connected bedroom wing, meal/freezer buffer and rec context drawn from compact-compound strata before retrying |
| 2026-08-14 RR third live draft (`20260814T001129797Z`) | connected compound and freezer improved the silhouette, but the north wing was one bare barracks and the oversized common room remained under-furnished | rejected for publication; subdivide four private bedrooms, furnish a seated recreation nook and put a dirty-return shelf on the dining-to-kitchen route |
| 2026-08-14 footprint review | north-wall fixture roots were placed on the wall row; a 3x1 stove overran the east wall; the furnishing script disagreed with the layout's cooler cell; the cooler exhaust was fenced; a plant pot occupied the freezer wall | rejected; derive every occupied and interaction cell from finalized loaded Defs, share one cooler root, clear both cooler sides and validate decorations like production buildings |
| 2026-08-14 first exact-geometry run (`20260814T012428760Z`) | the geometry plan was correct, but a large nested C# preflight expression exceeded the practical Mono REPL compilation seam and returned HTTP 500 | kept the geometry, moved it into one separate bounded scenario step, and left furnishing as a simple spawn step |
| 2026-08-14 player placement review (`20260814T031124305Z`) | collision checks passed, but later phases still derived their origin from a mobile pawn; the stove/prep corner shared an approach cell; the cooler sat low on its wall; and the plant/lamp/sculpture cluster read as misplaced clutter | rejected the retained media; persist one immutable origin, split the stove onto the opposite wall, reserve distinct approach aisles, center the cooler on its service wall, and give the plant its own lower-corner alcove |
| 2026-08-14 detailed player review (`20260814T034301601Z`) | door thresholds were bare, dirty cutlery missed its shelf, service shelves faced the wrong aisles, the west fence duplicated the adjacent wall, the east chess chair was one cell too far away, and beds/end tables ignored their actual head cells | rejected the static approval; floor every threshold, derive recreation seats from the table root, face shelves into their aisles, remove the redundant fence run, and require native bed-facility links with every pillow against the bedroom wall |
| 2026-08-14 full native workflow (`20260814T080546862Z`) | the wash cycle conserved and cleaned all eight stocked/washed units, but one two-cell clean shelf had only six distinct-stack slots and left two units without a valid haul destination | rejected the final beat; use two wall-aligned clean shelves and require every unit to reach their combined native storage footprint before capture |

## Live review

- Superseded evidence: `20260814T034301601Z is retained only as a rejected design iteration after detailed player review.`
- Rejected workflow evidence: `20260814T084734178Z, PID 63432, process start 2026-08-14T10:47:44+02:00, exact package order Harmony -> Core -> Processor Framework -> Dubs Bad Hygiene -> Pick Up And Haul -> Immersive Chefs -> Gateway.`
- Player workflow observed: `ordinary Cleaning work started Doing dishes; Mara collected multiple exact dirty units through Pick Up And Haul; all six units entered the Processor dishwasher at 0%; the native cycle reached 100% while output hauling was held; ordinary Hauling started Processor Framework EmptyProcessor; the same six units became clean and all eight total clean units finished on the two kitchen shelves.`
- Visual review: `the native workflow succeeded, but the wide colony framing, remaining set dressing and test-like presentation were rejected by the user and are not eligible for Workshop publication.`
- Rejected media: `dishwasher-turnaround.png SHA-256 0A89987E0F291D35AD49361085679298D8198E5B3DB56E98099D0CD0D12E20AA; dishwasher-turnaround.gif SHA-256 32761E2C8DACA74989F2DB1758F3DF687D3A37A2F7E56F13868758AF15BC9B89; retained only as an ignored draft, not staged release art.`
- Log review: `500 retained live log entries contained no Error or Exception entries; the sole Warning was the expected Dev Gateway developer-only API startup notice.`
- Remaining caveats: `replace the rejected wide draft with a tight, believable action crop before this design can become live-reviewed.`
