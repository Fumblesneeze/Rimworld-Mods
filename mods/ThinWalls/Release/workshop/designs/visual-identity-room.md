# Thin Walls lived-in partition room

- Showcase ID: `thin-walls-visual-identity-room`
- Design-record version: `7`
- Owner: `fumblesneeze.thinwalls`
- Status: `redesign implemented; fresh capture and review required`
- Intended crop: `13x9 cells at ordinary and close useful zoom`
- Colony brief: `Established industrial temperate colony; a compact granite-and-wood common room was subdivided with Stuff-matched Thin Walls while retaining normal use of the cells beside the partition.`
- Exact presentation packages: `brrainz.harmony`, `ludeon.rimworld`, `fumblesneeze.thinwalls`; `fumblesneeze.rimworlddevgateway` is capture tooling only.
- Native workflow: `player designates the Thin Wall partition and Thin Door through Architect > Structure -> a colonist receives a native move order across the partition -> the leaves visibly open and the colonist crosses.`

## References

| ID | Role in this scene | Inspected evidence | Eligible class |
|---|---|---|---|
| R01 | finished public-room context | furniture follows walls and leaves a legible central approach | recurrent |
| R03 | restrained mixed furniture and lived-in rhythm | grouped seating and storage, not isolated test props | recurrent |
| R06 | compact dining circulation | table seating remains close to the room boundary without blocking passage | recurrent |
| R14 | established but still compact colony module | a functional room can show a later material partition without a decorative megabase | recurrent |

## Applied rules

| Rule ID | Class | Sources/contract | Decision in this scene |
|---|---|---|---|
| player-readable-room | recurrent | R01/R03/R14 | retain an outer Core-wall shell, flooring, grouped furniture, and open circulation so the crop reads as a colony room before the mod is explained |
| real-footprints | mechanical | `GenAdj.OccupiedRect`, Core furniture Def sizes | validate every furniture footprint before spawning and keep it inside the shell |
| table-seating | mechanical/recurrent | Core table/chair interaction; R01/R03/R06 | one 2x2 dining table with four usable chairs, clear of the partition and door approach |
| thin-partition | mechanical | Thin Walls shared-edge and room contracts | use one continuous internal Thin Wall run with one Thin Door; both adjacent cell rows remain usable and the door opens only for the crossing step |
| functional-entrance | mechanical/presentation acceptance | Core cardinal reachability; realistic-base design rules | replace one south exterior wall cell with a native Core Door and keep a clear route from outdoors to both sides of the partition |
| wall-headed-bed | mechanical/presentation acceptance | Core `Bed` 1x2 footprint; realistic-base design rules | place the bed with its head against the north exterior wall and keep a side or foot approach clear |
| restrained-palette | recurrent | R01/R03/R14 | granite outer shell, warm timber furniture, and one Stuff-matched partition family; no arbitrary prop scatter |

## Adjacency graph

`outdoors --high-frequency--> Core exterior Door --high-frequency--> west common-room aisle`

`west common-room aisle --medium-frequency--> Thin Door --medium-frequency--> east sleeping zone`

`north wall --backs--> bed --side/foot approach--> east sleeping-zone aisle`

The table/chairs and bed/work furniture stay out of both the exterior-door route and the two-cell Thin Door approach. The partition remains the only internal boundary and therefore remains the visual subject, but the shell is a usable building rather than a sealed display box.

## Planned zones and placements

| Zone/object | Bounds or relation | Exact Def/rotation/material | Rule IDs | Rationale |
|---|---|---|---|---|
| outer room shell | local `x=-6..5, z=-23..-15`, one-cell Core-wall boundary | `Wall`, granite blocks | player-readable-room, restrained-palette | establishes ordinary full-height context around two usable rooms; the divider is intentionally one cell east of the earlier rejected centerline so its complete regular-wall contacts and east-side bed remain visually separated |
| exterior entrance | local root `(-1,-23)`, replacing that south boundary wall; clear interior aisle at `(-1,-22)..(-1,-19)` | `Door`, wood, cardinally connected to outdoors and the west common room | functional-entrance, real-footprints | enters the public room first; sleeping-space access remains behind the internal Thin Door |
| interior floor | local `x=-5..4, z=-22..-16` | `WoodPlankFloor` | player-readable-room, restrained-palette | separates the fixture from the empty test terrain |
| internal partition | local `x=1` West-owned north-south edges, walls at `z=-22..-20` and `-18..-16`, Thin Door at `z=-19` | `TW_ThinWall` plus one `TW_ThinDoor`, wood | thin-partition | moves the full divider one cell east of the rejected composition while retaining complete, aligned contacts with both regular-wall runs |
| dining group | table root `(-4,-18)`, occupied rect `x=-4..-3,z=-18..-17`; chairs at `(-4,-19)` North, `(-4,-16)` South, `(-5,-18)` East, and `(-2,-18)` West | `Table2x2c` with four `DiningChair` footprints | real-footprints, table-seating | every chair is cardinally adjacent to the exact table rect with no empty-cell gap and faces the table; all remain outside the door aisles |
| sleeping/rest cues | bed root `(3,-17)`, occupied cells `(3,-17)..(3,-16)`, `Rot4.South`, actual `BedUtility` head at `(3,-16)` backed by wall `z=-15`; keep foot approach `(3,-18)` and west-side approach `(2,-17)` clear | `Bed`, wood, `Rot4.South`; no armchair unless a live footprint preflight proves it leaves those approaches clear | real-footprints, wall-headed-bed, player-readable-room | uses RimWorld's actual head/feet semantics rather than the sprite author's assumed north convention, so the pillow/head is visibly wall-backed and the foot remains approachable |
| camera | room shell plus one cell of exterior context | native camera framing | player-readable-room | keeps the complete action geography in one crop at ordinary zoom and a deliberate close hard cut around the partition/door |

## Variation and history

The granite shell represents the older durable room. The internal Stuff-matched Thin Wall is a later subdivision that preserves floor area. Warm furniture and the wood floor keep the room readable without competing with the partition materials. No random damage or clutter is added to this context view; damage remains in the separate controlled material-grade catalog.

## Rejected drafts and deviations

| Draft/evidence | Rejection or deviation | Resulting rule/change |
|---|---|---|
| isolated desert visual catalog, `20260817T163045623Z` | blind reviewer read horizontal pieces as possible benches/counters and north-south pieces as poles because no room use established their purpose | add an ordinary furnished room shell and native crossing view; retain the isolated catalog only for exact material/geometry comparisons |
| first fully-open mixed-run frame, `e2e-screenshot-000016.png` | the central opening was technically present but too small to read confidently at ordinary zoom | increase the fully-open leaf travel while retaining both retracted leaves and fixed endpoint jambs |
| accepted room frames `20260818T113610616Z/e2e-screenshot-000059.png` and `000060.png` | the side-T shoulders at both ends of the north-south partition were assigned wholly to one adjacent regular-wall tile, displacing the wide connection from the Thin centerline; the shell also had no exterior door and the bed floated away from a backing wall | reject the captures and every title/About/feature card derived from them; require two-tile vertex-centered side-T composition, a functional exterior Core Door, and a north-wall-headed bed before recapture |
| centered-arm diagnostic run `20260818T150717729Z`, especially `000016..000019` and room `000024..000025` | the branch centerline, exterior Door, and bed arrangement were corrected, but the renderer still sampled Core's non-straight T slots, leaving triangular bevel/wedge pixels at the square shoulders | reject the run for publishing; treat augmented link indices as topology only and sample added arm surfaces from orientation-matched straight Core donors with one continuous world phase |
| straight-donor diagnostic run `20260818T153652343Z`, especially `000016..000019` and room `000024..000025` | straight donors removed the diagonal wedges, but the ordinary-width perpendicular side-T arm remained visible as a rectangular collar over the receiving wall; the five-cell comparator and odd-width room also placed a grid-vertex branch half a cell off the displayed run center, and the exterior entrance opened into the bedroom | reject the run; side-Ts clear only a seven-pixel contour aperture with no perpendicular arm, use symmetric four-cell comparators/even room width, and enter through the common room |
| first collar-free live run `20260818T160135145Z`, especially `000016..000019`, `000027`, and `000029` | cardinal shoulders were centered and collar-free, but a filtered alpha hairline crossed every receiving wall at the shared tile boundary; the open mixed door also projected an unclipped retracted leaf slab south of its jamb | reject the run; split native and exterior sampling into five disjoint tightly cropped/clamped textures and clip translated leaves plus UVs to the fixed frame aperture before submission |

## Accepted live review

The evidence and promotion notes below are historical only. The user rejected the resulting public composition on 2026-08-20 because the bed head faced away from its backing wall, one chair had an empty-cell gap from the table, the divider contacts were visually incomplete/misaligned, and a bottom contact artifact remained. None of these captures or cards is an accepted publication input. The next revision must update the exact furniture roots/rotations and divider position, pass the itemized placement audit and mechanical pixel gates, and receive fresh in-game and independent review.

- Exact build/package identity: `ThinWalls.dll` SHA-256 `C5050F1D6D52C5C88A369104EC7305F8D5CB7B43D56C488DA40BA23069043DDB`; exact active product order `brrainz.harmony`, `ludeon.rimworld`, `fumblesneeze.thinwalls`, with `fumblesneeze.rimworlddevgateway` appended only as minimized capture/control tooling.
- Final two-sided building-clearance process/evidence: `artifacts/EndToEndRuns/Grouped/20260820T020606902Z`; session `smoke-001/20260820T020624747Z/SavedData/DevGateway/Sessions/3f7b3b5426ae495899118400e2612571`. Native placement and visual-identity workflows passed on the reviewed build. Frames `000002..000004` are the close, ordinary, and far UI-free WoodPlankFloor views of two Core workbenches placed on opposite sides of one completed Thin Wall; neither workbench overlaps the complete outlined wall body.
- Final cumulative visual/room process/evidence: `artifacts/EndToEndRuns/Grouped/20260820T013908908Z`; session `smoke-001/20260820T013927106Z/SavedData/DevGateway/Sessions/489b6eb405ee428fa2473995e8da2e31`. The minimized native workflow passed with complete cleanup. Frames `000009`, `000010`, and `000011` isolate complete equal-scale L, T, and + unions with their corrected native-winding shadows; `000020..000023` isolate square regular-wall contacts in each direction; `000030..000031` show one mixed regular-wall Thin Door closed and genuinely open; `000033..000035` show moderate/heavy/severe damage separately for stone, wood, and steel; and `000037` shows the furnished shell after the pawn traversed both the internal Thin Door and the exterior Core Door and both doors closed normally.
- Final current-build designator process/evidence: `artifacts/EndToEndRuns/Grouped/20260820T021022450Z`; session `smoke-001/20260820T021039269Z/SavedData/DevGateway/Sessions/6609d360a92d43a7b4ec6a7637bea1fc`. The native designator showed a retained one-cell South preview, Q rotated it East, E returned it South, and the ordinary click/drag workflow completed the directional catalog while the process remained minimized.
- Player actions observed: a colonist received native movement orders that crossed the furnished-room Thin Door and then the only exterior Core Door. The Thin Door reached at least 95% open during the crossing, the pawn reached the exterior marker, and both inherited door movers returned to closed before the final room capture. The internal Thin Door remained the only route between the common room and sleeping zone, and the Core Door remained the only cardinal route outdoors.
- Personal inspection: the final junction captures contain one connected, constant-width silhouette with no post, tab, diagonal wedge, collar, gap, or internal endpoint contour. The regular-wall contact retains the full Core wall while the narrow arm meets it at one centered square shoulder. Horizontal stone courses remain continuous; tops stay distinctly darker than faces; north-south arms sit behind foreground horizontal faces; door leaves remain attached to their fixed frames while opening a real passage; shadows remain attached; and material-specific damage is clipped to its owner at all three grades.
- Product-only acceptance: `artifacts/ThinWallsProductOnly/20260820T015055124Z` records a fresh exact-PID process with only Harmony, Core, and Thin Walls on the reviewed runtime. RimWorld launched isolated and minimized; the native Architect workflow opened Structure, selected Wooden thin wall, captured the horizontal single-cell hover, rotated the same cell vertically with `E`, placed the blueprint, and selected the native result. No Gateway was present, shutdown was graceful, the log had zero exception signals, and the user's normal configuration hashes remained unchanged.
- Presentation promotion: fourteen accepted screenshots are copied byte-for-byte beneath the three exact current-build folders `sources/in-game/accepted-20260820T013908908Z`, `accepted-20260820T020606902Z`, and `accepted-20260820T021022450Z`. Superseded 2026-08-19 inputs and one unmanifested current-run frame were moved recoverably to ignored evidence. Schema-v2 `presentation.json` binds all three run/session identities, minimized state, dimensions, source hashes, the pinned local font and ImageMagick build, exact copy/alt text, and output hashes with `imageGenerationUsed: false`. The renderer rejects any undeclared PNG in the accepted-source tree. The title, About preview, four feature cards, and contact sheet contain only deterministic crops/resizes, declared cell-boundary annotations, text, and framing over actual accepted in-game screenshots; the final files are lossless truecolor PNGs with no palette quantization, tonal alteration, or generated/reconstructed wall art.
- Publication composition: the edge card uses the two native Q/E preview frames and the isolated opposing-workbench capture. The junction card gives separate equal-scale complete L/T/+ captures and one square regular-wall union. The door card pairs a furnished room containing a real exterior Core Door with same-crop closed/open Thin Door details. The damage card presents three uncropped-height in-game rows, each retaining its moderate/heavy/severe Stone, Wood, or Steel silhouettes.
- Independent publication review: a context-free reviewer received only the final presentation PNG paths. It correctly identified edge-mounted walls and Thin Doors, usable adjacent cells, square L/T/+ and regular-wall unions, distinct stone/wood/steel families, attached progressive damage, coherent perspective and shadows, and distinct closed/open door states at both full and reduced scale. Its first pass rejected the title because the exited pawn's head remained below the gold crop frame; after the deterministic crop was tightened, its exact re-review confirmed the pawn was gone while the exterior entrance, north-wall bed, and both divider contacts remained coherent, and returned `PASS`.
