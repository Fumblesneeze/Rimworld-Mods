# Evidence-grounded colony design rules

These are generation constraints, not a single optimal base. Confidence labels are defined in the parent skill.

## Colony form and growth

- **Recurrent (R01, R02, R04, R14):** Start with a functional heart—food storage, cooking, dining/rec—and let
  residential, production, medical, guest, and defensive districts grow around it. Large references frequently
  retain this legible center even when later wings become irregular.
- **Recurrent (R02, R03, R04, R14):** Use a primary circulation spine plus secondary local paths. Connected
  compounds often use two- or three-cell main corridors and narrower service links; town forms use paved outdoor
  paths and small courtyards instead of one giant hallway.
- **Archetype (R15, R16, R18):** Organic colonies follow mountain edges, marsh, water, fertile soil, or earlier structures.
  Their irregularity is constrained. Avoiding marsh and following contours produces convincing non-square rooms;
  random concavities do not.
- **Recurrent (R03, R04, R14, R15):** Show chronology. Early wood or rough stone can remain in an older core while later stone,
  sterile tile, metal, and specialist furniture appear in newer or wealthier rooms. One perfectly uniform
  material across every function reads as a scenario generator.
- **Recurrent (R04, R14, R15, R21):** Leave expansion evidence: a short unfinished road, a spare conduit branch, a repurposed room,
  a patched floor, or one new wing. Use at most one or two such cues in a close showcase crop.

## Adjacency and circulation

- **Mechanical/recurrent (R02, R03, R06, R11, R19, RimWorld Wiki):** Put high-frequency producer and consumer pairs close. Food path is generally
  farm/hunting intake -> raw freezer -> prep/butchery -> clean cooking -> meal buffer -> dining.
- **Mechanical (R02, R11, R19, RimWorld Wiki):** Keep butchery and dirty intake separate from the clean kitchen. Do not make the kitchen a
  through-route to the freezer or dining room; diners and animals should not track through it.
- **Recurrent (R02, R03, R11, R19):** Use high-priority shelves or fridges beside workstations as small buffers. Bulk storage belongs
  nearby but outside the worker's repeated one- or two-cell loop.
- **Recurrent (R01, R04, R07, R20, R21):** Put public rooms—dining, recreation, guest restaurant, shop—on a legible approach. Put kitchens,
  utility rooms, bulk storage, and staff circulation behind or beside them.
- **Mechanical (RimWorld and exact optional-mod Def/job contracts):** Reserve complete interaction cells, linked-building lanes, doors, seats, and hauling paths.
  A visually attractive placement that blocks a native job is invalid.
- **Mechanical (RimWorld `GenAdj`/`ThingUtility`):** Spawn roots are not footprints. Resolve every rotated
  `OccupiedRect` and interaction cell before spawning, reject fixture/fixture and fixture/interaction overlap,
  and require every ordinary furniture cell to lie strictly inside its intended room. This is especially
  important for even-width objects, whose root shifts toward a different edge under opposite rotations.
- **Mechanical (scenario lifecycle):** Store one immutable map-cell origin for a generated scene and use it in
  every later phase. A named pawn may locate the scene for human review, but its changing `Position` must never
  become the origin for later furniture, utilities, checks, or camera framing.

## Room scale and furnishing

- **Recurrent (Real Ruins corpus RR-20260814, n=6,049):** 78.3% of resolved Core production workstations touch
  a wall. Default to a wall-aligned bench run. Use a central island only when shared storage/linking/workflow
  explains it and every interaction cell retains a clear aisle.
- **Mechanical/recurrent:** A credible wall run needs an uninterrupted backing wall across the whole rotated
  footprint and separate worker approach space. Root-cell adjacency and non-overlapping footprints alone do not
  prevent a pinched L-corner where two simultaneous workers need the same approach cell.
- **Mechanical/recurrent (Core Defs; RR-20260814, n=4,869 tables):** Size seating from the real occupied table
  footprint. Corpus medians are 2 chairs for 1x2, 4 for 2x2, 7 for 2x4 and 6 for 3x3 tables. Do not place more
  chairs than the selected table can visually and mechanically support.

- **Archetype starting heuristic (R17):** One community example cites a 7x3 interior for a two-station kitchen and 7x4
  for a freezer. Treat these as compact starting modules, then enlarge for Immersive Chefs stations,
  dishwashers, sinks, shelves, doors, and several simultaneous workers.
- **Recurrent (R01, R02, R03, R07):** Furniture tends to line walls, form islands, or define one side of a passage. Do not center every
  object or fill every cell. Keep negative space for pawn traffic and readable sprites.
- **Recurrent (R01, R03, R06, R07):** Dining rooms provide at least several seats, not one test chair. Larger rooms combine seating
  clusters with art, plants, lights, temperature control, recreation, service counters, or nearby meal storage.
- **Recurrent (R02, R03, R11):** Workshops and food-production rooms group compatible benches around shared storage and
  tool/link facilities. Keep outputs
  and raw stock visually differentiated from walk space.
- **Recurrent (R01, R02, R03, R04):** Use two to four dominant materials/tones in one crop, plus small accents. Floor changes should
  mark function, status, cleanliness, or construction phase.
- **Recurrent (R01, R02, R07, R12):** Light work cells, tables, doors, and focal art. Avoid perfectly even lamp grids unless the
  colony is deliberately institutional or high-tech.

## Lived-in detail without noise

- **Mechanical (RimWorld cell connectivity):** Treat walls, natural rock, doors, fences, and fence gates as
  connected only through the four cardinal neighbors. A cell touching another only diagonally does not close a
  room, pen, yard, or defensive line. Validate an enclosure with a cardinal flood fill around its complete
  boundary; do not approve it from the rendered silhouette alone.
- **Mechanical/recurrent (perimeter topology):** Build a one-cell-thick enclosure boundary. `T` and `+`
  junctions are valid when paths or partitions meet, but reject accidental double rows and any solid 2x2 block
  made solely from adjoining wall/fence/door/gate cells. When a building wall or natural rock replaces part of a
  fence line, join the two materials through cardinal neighbors and keep every transition explicit.
- **Recurrent (RR-20260814, n=904,440 conduit cells):** Route power through wall cells or alongside walls/service
  corridors. 66.1% of resolved conduits share a wall cell and 75.4% are under or cardinally adjacent to one.
- **Mechanical (Core power-net contract):** A powered building does not need a conduit beneath every occupied
  cell—or beneath the building at all. Keep a sparse concealed backbone in walls or a service corridor and let
  the building connect across RimWorld's native connection radius. Add a short branch only when the live inspect
  pane still reports no connection after the power net has settled for several ordinary ticks. Reject conduit
  carpets beneath every appliance: they waste resources, add visual noise, and usually indicate that footprint
  adjacency was confused with power-network range.
- **Archetype (RR-20260814, n=459 `WaterTowerS`; Dubs utility contract):** Keep water towers in an exterior
  utility yard or purpose-built service enclosure. Never put one inside a kitchen or dining room.
- **Mechanical (Core Def):** `ChemfuelPoweredGenerator` is a non-rotatable 2x2 Graphic_Single. Use its native
  orientation and a ventilated/service location; do not rotate it as furniture.
- **Mechanical (Core `Building_Cooler`):** A cooler replaces one deliberate wall cell. Its rotated south side is
  the cold side and its rotated north side the hot exhaust. Require a roofed freezer cell on the cold side, a
  clear unroofed or distinct-room exhaust cell, retained wall segments on both perpendicular sides, and no
  fence, conduit-only assumption, furniture, or decoration blocking either functional cell.
- **Mechanical:** Preflight plant pots, lamps, sculpture and shelves like production buildings. Never place a
  decorative root on a room edge and assume the sprite offset makes it acceptable.

- **Recurrent (R01, R02, R03, R07, R11):** Include stocked shelves, a few meals or ingredients, one cleaning/utility cue, art or plants,
  and context beyond the room. These tell more than loose items scattered over walkways.
- **Recurrent (R01, R02, R03, R07, R11):** Use repeated objects in plausible groups: chairs at tables, bins by production, fridges in a
  service wall, planters along a public approach. Single unrelated props placed at regular intervals look fake.
- **Candidate (R03, R04, R14):** One visually imperfect detail—a mixed chair set, repaired wall, old floor strip, or full shelf—
  can help a small scene. Do not use filth, rubble, starvation, corpses, or broken power unless the requested
  story needs them.
- **Mechanical (RimWorld Wiki; visual counterexample R05 is supplemental only):** Keep animal zones and hauling traffic from contaminating a clean kitchen. Animals may appear
  in a broader colony crop but should not wander through the cooking showcase.

## Variation model

Choose one option from each row rather than copying a reference:

| Dimension | Plausible choices |
|---|---|
| Form | compact compound; expanded corridor base; separated township; mountain-following; courtyard cluster |
| Stage | improvised early; established industrial; wealthy specialist; spacer hospitality |
| Main material | local stone; mixed stone and wood; dressed stone; metal/composite accents |
| Food service | adjacent rooms; wall-fridge pass-through; service corridor; restaurant counter |
| Dining | communal hall; several small tables; counter seating; prisoner common room |
| Visual history | retained starter room; later wing; material repair; landscape-constrained outline |

Reject combinations that contradict the scenario's tech, wealth, climate, or loaded Defs.
