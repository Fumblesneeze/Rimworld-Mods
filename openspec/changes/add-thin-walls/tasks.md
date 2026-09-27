> **2026-09-04 pivot:** checked rendering/asset/publication tasks below record historical work, not
> acceptance of the replacement. All earlier visual approvals and publishing captures are superseded.
> Section 22 is the active source-only mesh work queue. Section 21's still-required placement and
> measurement outcomes carry into it; its CPU-painted phase-seam implementation is cancelled, not
> completed. Gameplay contracts remain required and need focused regression where the new renderer
> or phase wiring affects them.

## 1. mods/ThinWalls — Product and test scaffolding

- [x] 1.1 Create the product project, package metadata, Harmony/Core dependency order, Def folders, localization folders, release-source layout, and `fumblesneeze.thinwalls` repository-local package output.
- [x] 1.2 Create and register honest Thin Walls unit, explicit-Harmony, in-game integration, and E2E projects without placing test assemblies in the product package.
- [x] 1.3 Add solution and focused test-wrapper registrations, and prove every new filtered test command rejects a zero-test run.

## 2. mods/ThinWalls — Edge model vertical TDD

- [x] 2.1 RED: add one focused public-behavior test for east-drag to south-owned-edge mapping and retain the nonzero failing result.
- [x] 2.2 GREEN: implement the minimum cardinal direction/owned-edge/shared-edge value model to pass the tracer test and retain the passing result.
- [x] 2.3 RED/GREEN: add and implement west/north/south mapping, zero-length retained direction, canonical opposite ownership, duplicate identity, and four-orientations-per-cell one behavior at a time.
- [x] 2.4 RED/GREEN: add and implement shared-edge cardinal plus inclusive-endpoint diagonal connection removal, final-owner reopening, and doubled-owner persistence one behavior at a time.
- [x] 2.5 REFACTOR: deepen the edge geometry/index interface, remove duplication, and rerun only the affected focused edge tests after each refactor.

## 3. mods/ThinWalls — Native designation and construction vertical TDD

- [x] 3.1 RED: add a focused source/Def contract test for one special Structure designator, no default designator, the cardinal-only draw style, cost 3, base HP 150, compatible Stuff categories, standable/non-edifice behavior, and explicit no-roof fields.
- [x] 3.2 GREEN: add `TW_ThinWall`, its generated Def support, `Designator_ThinWall`, package text keys, and minimum custom Thing classes needed to pass the focused contract.
- [x] 3.3 RED/GREEN: test and implement parameterless special-designator resolution, right-side rotation assignment, suppressed rotation controls, and edge-aligned drag/ghost preview.
- [x] 3.4 RED/GREEN: test and implement exact-owned-edge duplicate rejection while allowing different orientations and opposite owners.
- [x] 3.5 RED/GREEN: test and implement blueprint/frame/building phase recognition, derived map-index rebuild, register/unregister notifications, and save/load-safe ownership identity.
- [x] 3.6 REFACTOR: keep native construction/resource/damage/deconstruction behavior in RimWorld classes and rerun the affected focused construction tests.

## 4. mods/ThinWalls — Edge-aware building placement vertical TDD

- [x] 4.1 RED: add a focused public placement-rule test proving a two-cell footprint crossing a completed shared edge is rejected.
- [x] 4.2 GREEN: implement rotated-footprint internal-edge validation and the narrow `GenConstruct.CanPlaceBlueprintAt_NewTemp` bridge.
- [x] 4.3 RED/GREEN: cover blueprinted/framed edges, wall-through-existing-footprint rejection, same-cell partial/full one-cell acceptance, multi-cell wholly-on-one-side acceptance, and floors/zones/items one behavior at a time.
- [x] 4.4 RED/GREEN: cover construction overlap/wipe invariants through the exact `GenSpawn.SpawningWipes`, blocking-Thing, blueprint-to-frame, and frame-to-building seams.
- [x] 4.5 REFACTOR: consolidate placement and constructible-phase rules behind one narrow public module and rerun the affected focused placement/Harmony tests.

## 5. mods/ThinWalls — Edge-aware pathing vertical TDD

- [x] 5.1 RED: add an explicit-Harmony target/shape test for RimWorld 1.6 `PathFinderMapData.GatherData`, `ParameterizePathJob`, `Reachability.CanReach`, `ReachabilityImmediate.CanReachImmediate`, and `Pawn_PathFollower` movement seams.
- [x] 5.2 GREEN: implement the per-map native connectivity overlay, safe gather-boundary rebuild, normal-job parameterization, path-cell invalidation, cache versioning, and map-removal disposal.
- [x] 5.3 RED/GREEN: add and implement bounded edge-aware reachability for accepted vanilla answers plus Touch adjacency that cannot act through a completed edge.
- [x] 5.4 RED/GREEN: add and implement stale-path setup/entry guards proving the pawn position cannot change through a newly blocked cardinal or diagonal step.
- [x] 5.5 RED/GREEN: add and implement destroyable-traversal planning plus ordinary melee blocker jobs against concrete single and doubled owners.
- [x] 5.6 REFACTOR: remove hot-path allocations, bound searches/caches, retain exact patch ownership, and rerun the affected focused pathing/Harmony tests.

## 6. mods/ThinWalls — Original wall art and renderer vertical TDD

- [x] 6.1 Measure locally installed Core and relevant subscribed-mod structural/conceptual wall comparators read-only and write a focused visual contract for final canvases, alpha, filenames, map width, fixed-camera angles, exterior outlines, cardinal landmarks, Stuff tint, blueprint treatment, and close/ordinary/far zoom readability.
- [x] 6.2 RED: add a focused asset/package test for the declared cardinal diffuse/blueprint files and approved visual manifest; retain the nonzero failing result.
- [x] 6.3 Write the measured detailed generation prompt and acceptance criteria, generate at least two original wall-art candidate sets with the built-in image-generation path, retain raw candidates only under ignored evidence, normalize transparent RGBA outputs, and compare them beside the references at source and final map scale on light/dark contact sheets.
- [x] 6.4 GREEN: select and package one reviewed outlined cardinal wall/blueprint set, pin its dimensions/alpha/outline metrics/SHA-256, and make the focused asset test pass.
- [x] 6.5 RED/GREEN: test and implement visibly outlined completed, blueprint, frame, and designator strip geometry at the owned edge.
- [x] 6.6 RED/GREEN: test and implement visibly continuous outlined straight/L/T/+ endpoint nodes, doubled-owner thickness, final-owner thinning, regular-wall overlap, and affected mesh dirtying.
- [x] 6.7 REFACTOR: consolidate static/realtime geometry calculations, material selection, and incident-segment enumeration while green.

## 7. mods/ThinWalls — Localization and package contracts

- [x] 7.1 RED: add an exact English/German inventory test for every source and packaged Def/runtime key plus placeholder/tag parity.
- [x] 7.2 GREEN: author contextual English and German labels, descriptions, rejection reasons, inspect text, and About metadata; pass the focused catalog test.
- [x] 7.3 RED/GREEN: add and satisfy positive-allowlist package checks for About/Defs/Patches/Languages/Textures/assembly files, zero test/Gateway assemblies, and no Gateway AssemblyRef.
- [x] 7.4 Build the owning product in Release with zero warnings/errors and inspect the repository-local `artifacts/Mods/fumblesneeze.thinwalls/1.6` package byte-for-byte.

## 8. mods/ThinWalls — Real-loader and native E2E verification projects

- [x] 8.1 RED/GREEN: add a staged main-menu integration test for finalized Def values, texture/material resolution, special designator presence, and exactly-once Thin Walls Harmony ownership under exact Core/Harmony/Thin-Walls/Gateway order.
- [x] 8.2 RED/GREEN: add a grouped E2E test that uses the native Architect designator and ordinary Construction jobs for all four orientations, multiple owners per cell, opposing owners, 3-Stuff consumption, half durability comparison, cancellation, damage, deconstruction, and save/load.
- [x] 8.3 RED/GREEN: add a grouped E2E test for native same-cell one-cell building acceptance and native crossing-footprint/planned-edge rejection.
- [x] 8.4 RED/GREEN: add a grouped E2E test for native pawn routing around an endpoint, closed-enclosure non-crossing, L-corner diagonal blocking, stale path invalidation, reopened movement, and explicitly bash-capable traversal of endpoint/single/doubled owners.
- [x] 8.5 RED/GREEN: extend the grouped E2E visual catalog to capture cardinal/Stuff strips, blueprints/frames, straight/L/T/+ nodes, doubled thickness, owner removal, regular-wall joins, and undamaged/moderate/severe damage on standard/doubled/junction geometry without selection brackets at close, ordinary, and far useful zoom.
- [x] 8.6 RED/GREEN: add a grouped E2E roof scenario where native removal of the final real support causes the visible roof outcome despite single and doubled Thin Walls.

## 9. mods/ThinWalls — Focused regression and specification gate

- [x] 9.1 Run every new focused unit/Harmony/asset/localization test selection with nonzero executed counts, then the owning Thin Walls suites affected by shared behavior.
- [x] 9.2 Run the selected Thin Walls finalized-loader and visual E2E stable IDs only; inspect every exact-run multi-zoom/material screenshot personally and retain causal observations.
- [x] 9.3 Run the owning Release build, package/AssemblyRef inspection, E2E dry run, and `openspec validate --all --strict --no-interactive`.

## 10. mods/ThinWalls — Independent code and asset review

- [x] 10.1 Run an independent scoped code review for correctness, pathing performance/thread safety, construction lifecycle, KISS/YAGNI, test honesty, localization, and package boundaries.
- [x] 10.2 Resolve or evidence-reject every finding, rerun each affected focused test/build/package check, and invalidate any earlier live evidence affected by review changes.
- [x] 10.3 Review selected wall art and reference/source/map-scale contact-sheet evidence for cardinal identity, Core-relative outlines, alpha fringe, Stuff response, node continuity, doubled thickness, regular-wall joins, zoom readability, and source/package hash parity.

## 11. mods/ThinWalls — Final reviewed in-game acceptance

- [x] 11.1 Historical optional check: the reviewed Release product ran with Core/Harmony/Thin Walls and no Gateway, retaining unique savedata/log/PID evidence, native Mods-screen inspection, native Architect visibility, clean developer console/log classification, and normal-config hash restoration. This completed check does not require future runs to omit Gateway.
- [x] 11.2 On a fresh reviewed gateway-assisted process, execute the actual native construction/designation, movement, building-placement, bashing, deconstruction, save/load, and roof-support workflows and personally inspect before/action/after views from that exact PID.
- [x] 11.3 Personally inspect and record every required cardinal/Stuff/junction/doubled/regular-wall visual outcome at close, ordinary, and far useful zoom on the reviewed package; retain build hash, ordered mods, camera states, actions, screenshots, logs, config hashes, graceful shutdown, and cleanup together.

## 13. mods/ThinWalls — Reopened visual identity acceptance

- [x] 13.1 Retain the exact stone-fence, OSB, riveted-plate, and damage reference inventory, measurements, prompt, candidate files, and side-by-side source/map-scale comparison sheets under ignored evidence.
- [x] 13.2 Capture fresh unlabeled in-game identity frames for wood, stone, and steel standard/doubled segments, straight/L/T/+ junctions, Core-wall joins, and undamaged/moderate/severe material-specific damage at close, ordinary, and far useful zoom.
- [x] 13.3 Give only those screenshots to a context-free independent reviewer sub-agent; retain its object/purpose interpretation and visual-coherence verdict.
- [x] 13.4 Resolve every blind-review perspective, outline, material, connection, style, or zoom-legibility defect and repeat the live capture/review until the reviewer correctly recognizes the intended thin edge walls with no defect.
- [x] 13.5 Rerender and re-inspect every publishing asset affected by the selected wall art; promote only reviewed exact in-game captures into feature-card panels with pinned provenance and hashes, and keep the presentation outputs explicitly separate from causal acceptance evidence.

## 12. mods/ThinWalls — Publishing descriptions and previews

- [x] 12.1 Author player-facing Markdown and Steam BBCode descriptions covering every implemented benefit and limitation without developer jargon or unimplemented claims.
- [x] 12.2 RED: add presentation tests for the title master, About derivation, at least three ordered feature cards, exact copy/alt/source manifest, licensed font, dimensions, opaque corner matte, overflow, sub-1-MiB bytes, and deterministic hashes.
- [x] 12.3 Generate at least two title-art candidates with the built-in image-generation path, select against the reviewed in-game geometry, and retain rejected candidates only under ignored evidence.
- [x] 12.4 GREEN: implement the deterministic presentation renderer and produce the approved 1280×720 master, 640×360 About preview, 1164×655 feature cards, and page-scale contact sheet.
- [x] 12.5 Personally inspect every full-size publishing image and the native Mods-screen preview, verify clean offline rerender and package parity, and record the immutable presentation digest.
- [x] 12.6 Stop at the local publishing handoff without creating or updating any Steam Workshop item or invented published-file ID.

## 14. mods/ThinWalls — Stone, regular-wall join, Thin Door, and room extension

- [x] 14.1 Research and retain the current Core brick-wall atlas, simple-door mover/icon/Def, `Building_Door`, `Pawn_PathFollower`, `PathFinder`, `RegionMaker`, `RegionAndRoomUpdater`, `Room`, and temperature contracts; measure Core-relative contour weight and exact Thin Door/connector dimensions before asset generation.
- [x] 14.2 Update the measured generation brief with uninterrupted stone upper/lower courses, Core-matched exterior contour acceptance, material-aware regular-wall connector overlays, and closed/open Thin Door leaf geometry; generate at least two original candidates and retain side-by-side source/map-scale comparisons.
- [x] 14.3 RED/GREEN: add asset/package tests for stone course continuity, measured contour metrics, connector/door canvases and masks, exact paths, alpha bounds, dimensions, and manifest hashes; package the selected reviewed set.
- [x] 14.4 RED/GREEN: add renderer tests and implement additive regular-wall connector printing that preserves the vanilla wall graphic, selects the regular wall's Stuff family, suppresses a competing Thin Wall post, and dirties joins after either participant changes.
- [x] 14.5 RED/GREEN: add Def/designator/placement/lifecycle tests and implement `TW_ThinDoor` with right-hand edge designation, 13-Stuff/80-HP balance, one boundary element per shared edge, standable/non-edifice/no-roof behavior, native construction, hold-open, damage, save/load, and edge-aligned blueprint/frame rendering.
- [x] 14.6 RED/GREEN: shape-test the exact door/pathfinder seams and implement pawn-specific Thin Door connectivity, ordinary native opening waits, stale-entry prevention, no delay for non-crossing cell movement, inaccessible-door rerouting, explicit bashing, and scheduled snapshot disposal.
- [x] 14.7 RED/GREEN: shape-test the exact RimWorld 1.6 region fields/methods and implement an edge-aware `RegionMaker.TryGenerateRegionFrom` path that preserves vanilla behavior while preventing flood/link spans across completed Thin Walls and Thin Doors.
- [x] 14.8 RED/GREEN: dirty affected regions across construction/destruction/load, keep Thin Doors as persistent room dividers, redirect their temperature exchange to exactly the two rooms across the edge, and cover indoor classification, room IDs/cell counts/roles/stats, temperature isolation, final-edge remerge, vanilla-door preservation, and no-Thin-structure bypass.
- [x] 14.9 Extend grouped E2E with native Thin Door construction/open/wait/cross/hold/close/damage/save/load/bashing workflows and native room-inspect/temperature/final-edge-remerge workflows, retaining causal before/action/after screenshots.
- [x] 14.10 Extend the visual catalog with uninterrupted long stone runs, Core-outline side-by-side views, explicit wood/stone/metal regular-wall connectors, and Thin Door closed/open/mixed-run/damage views at close, ordinary, and far useful zoom.
- [x] 14.11 Run independent code and asset review, resolve every finding, rerun affected focused suites/build/package/spec validation, then repeat the reviewed finalized-loader, selected gameplay E2E, and exact visual E2E runs.
- [x] 14.12 Give only the final unlabeled multi-zoom connector/door/wall screenshots to a context-free reviewer sub-agent and repeat until it identifies the edge walls, doors, regular-wall joins, materials, and perspective with no defect.
- [x] 14.13 Update English/German catalogs, About/Workshop descriptions, presentation manifest, title/About/feature cards, deterministic hashes, package parity, and local publishing handoff for the expanded implemented scope.

## 15. mods/ThinWalls — Superseded sprite/ramp correction (historical)

> The user rejected this strip/post/stitch/perspective-ramp architecture after live inspection. Tasks 15.1–15.4 record work that occurred but their assets, contracts, screenshots, and publishing outputs are not valid product inputs. The unfinished renderer/acceptance tasks were cancelled rather than completed. Section 16 is the replacement work queue.

- [x] 15.1 Reinspect the exact short/tall stone-fence and current Core wall pixels plus the user's two correction drawings; record separate top-plane/front-course measurements, running-bond joint phases, forbidden cell-boundary seams, and the required west/east/south/north ordinary-wall overlap/wrap geometry.
- [x] 15.2 Replace the incorrect no-upper/lower-joint generation brief with an exact prompt and measurable acceptance contract for three-course running bond, collinear stitches, and west/east/south/north ordinary-wall wraps.
- [x] 15.3 RED: add focused asset contracts proving joints exist in every stone facade course, vertical joints terminate at horizontal mortar lines, upper/lower and half-brick-offset middle phases are correct, collinear stitch alpha/contours are exact, and every directional regular-wall extension overlaps far enough into both participants to cover the local ordinary-wall outline.
- [x] 15.4 GREEN: author at least two original corrected stone/stitch/regular-wall-wrap candidate sets, retain source/map-scale light/dark comparisons beside the measured references, select one, package it, pin recipe and output hashes, and pass the focused asset contracts.
- [x] 15.5 Record the user rejection, invalidate the unfinished ramp renderer, all generated Thin Walls art and recipes, every screenshot/card from that renderer, and the planned 15.6–15.9 acceptance path; none may be promoted or used as replacement references.

## 16. mods/ThinWalls — Core-derived hybrid linked-topology reset

- [x] 16.1 RESEARCH: inspect the exact RimWorld 1.6.4871 `Graphic_Linked`, `MaterialAtlasPool`, `Printer_Plane`, Bricks/Planks/Smooth wall atlases, Core wall Def, and door/damage seams read-only; retain source identity, hashes, 16-state index order, 80/60/10 atlas layout, and separately measured top/front/side/outline bands without copying Core pixels into the package.
- [x] 16.2 SPEC: replace the post/stitch/ramp design with the explicit north/east/south/west `None|Thin|Ordinary` topology, concrete resolved-atlas quadrant sampling, 81-state truth table, Core-derived sprite-space projection measurements, transient diffuse/mask runtime tile compilation, affected-regular-tile replacement (never overlay), single-union contour rules, non-generative asset prohibition, complete live catalog, and in-game-only publishing contract.
- [x] 16.3 TDD RED: add pure topology tests that enumerate all 81 directional states and fail on missing rays, disconnected unions, internal endpoint contours, duplicate/coplanar polygons, non-manifold overlaps, unstable ownership, forbidden post/stitch/ramp/bridge/miter/wedge/chamfer components, non-axis-aligned square contacts, inferred offset/parallel contacts, or changes outside a declared regular-wall aperture.
- [x] 16.4 TDD GREEN: implement the deterministic `HybridWallTopologyCompiler`, final-union outline/top/front/side mesh plan, canonical per-vertex ownership, exact cache key/invalidation, unique shared-edge geometry, and phase material substitution without reading or packaging the rejected textures.
- [x] 16.5 TDD RED/GREEN: implement runtime Core Bricks/Planks/Smooth material resolution and UV-band mapping that narrows only the 33/60 top footprint to 7/60 while preserving the 22/60 front and 11/60+10/60 side projections, Core outline weight, dark-top ratios, world-space surface phase, and deterministic OSB/rivet annotations.
- [x] 16.6 TDD RED/GREEN: replace the additive regular-wall connector postfix with padded semantic square-arm substitution; OR exact incident N/E/S/W contact bits into the native Core link state, retain the augmented ordinary-width arm only inside the centered native tile, stop the constant-width Thin arm at the tile boundary, and derive two axis-aligned shoulders with no diagonal/stair-step samples. Carry each contact's exact source atlas/Stuff/family/damage/door identity into one resolved raster. Prove unknown atlases fail closed, mixed Stuff uses one perpendicular material boundary, a regular-contact door caches only the square contact plus three-pixel fixed frame, offset/parallel near-misses do not connect, exact side-T contacts do connect, and the replacement emits no second bridge plane or overlap. Cover every wall quadrant and straight/L/T/+ rotation with one through four Thin rays and prove off-aperture bytes restore after removal.
- [x] 16.7 TDD RED/GREEN: move designator, blueprint, frame, completed walls, shadows, owner-clipped graded damage, and Thin Door closed/open/frame/damage rendering onto the same topology compiler; implement Q/E-rotatable single-cell hover/click orientation with right-hand authority only after a second drag cell; enforce one constructible phase per canonical shared edge; remove doubled-edge product fixtures plus the post, stitch, eight `RegularJoin*`, jamb, generated damage, and generated door/wall asset dependencies from code, package, tests, and manifests.
- [x] 16.8 Run the focused renderer/Def/package suites with nonzero counts, the owning Release build, package parity, and `openspec validate --all --strict --no-interactive`; then use the independent code-review workflow on the complete topology delta and resolve every finding.
- [x] 16.9 On the reviewed build, launch a fresh isolated exact-PID RimWorld process through the Dev Gateway while minimized; use native designation/construction/opposite-owner rejection/two-sided building placement/damage/door/deconstruction workflows and retain the 16 Thin-only masks plus every physically distinct hybrid straight/L/T/+ rotation, ordinary quadrant, side-T, square end contact, deliberate non-contact offset, multi-ray, material, shadow, damage, and door state at close, ordinary, and far useful zoom.
- [x] 16.10 Personally inspect the exact-run screenshots and causal before/action/after evidence, then give only unlabeled multi-zoom screenshots to an independent reviewer sub-agent; repeat implementation and the reviewed live run until the reviewer identifies the edge walls, linked junctions, regular-wall unions, doors, materials, shadows, and perspective with no artifact or incoherence.
- [x] 16.11 Delete/supersede every rejected generated wall/door/damage/presentation input, promote only the accepted in-game captures, rebuild the title/About/Workshop cards through deterministic text/layout composition with `imageGenerationUsed: false`, inspect the full-size and page-scale outputs, verify package hashes, and stop at the local publishing handoff without Steam mutation.
- [x] 16.12 Preserve the three 2026-08-18 user drawings under `references/` with SHA-256, record undirected placement identity, exact-versus-offset contact incidence, square end/corner/side-T geometry, rotation/reflection equivalence, and the two-sided furnished-cell requirement; supersede every analytic-miter and doubled-owner product contract.

## 17. mods/ThinWalls — Vertex-centered side-T and believable preview correction

- [x] 17.1 SPEC: record that a Thin endpoint meeting the side of a continuous regular run is centered on the shared grid vertex, with `16/17` native half-arms contributed by the two adjacent regular tiles, one canonical exterior gutter, and no complete branch displaced into either tile; require discriminating close views in all rotations.
- [x] 17.2 RED/GREEN: add focused public compositor tests that fail on the current one-tile owner, non-straight Core-slot miter/bevel sampling, and pasted side-T collars; then implement deterministic two-tile seven-pixel contour-aperture composition for diffuse, Stuff mask, damage boundary, and shadow while retaining the receiving straight run unchanged, exactly one gutter owner, and untouched bytes outside each declared aperture.
- [x] 17.3 Apply the realistic-base workflow to the lived-in preview before editing its fixture: reject the sealed-shell/floating-bed scene, record a functional exterior entrance, cardinal circulation, exact furniture footprints, and a wall-headed bed with a usable approach.
- [x] 17.4 Extend the native visual E2E fixture with isolated north/east/south/west side-T comparators plus the revised room; on the reviewed build, run RimWorld minimized through the Dev Gateway, personally inspect close/ordinary/far captures, and retain exact action-to-result evidence.
- [x] 17.5 Independently review the scoped code and unlabeled in-game visual evidence, resolve every finding, rerun affected tests/build/package/spec validation, and mark the rendering correction accepted only after the junction centerlines and room layout are visually coherent.
- [x] 17.6 Invalidate the accepted status and publishing use of `000059`/`000060`, promote only the corrected exact-run captures, and deterministically rebuild and inspect the title, About preview, and affected Workshop cards with `imageGenerationUsed: false`.

## 18. mods/ThinWalls — Cumulative projection and two-sided readability correction

- [x] 18.1 SPEC: consolidate every active rendering decision into the normative 2026-08-19 requirement matrix; define boundary placement from the complete non-shadow structural-alpha envelope rather than only the seven-pixel top centerline; center the physical shadow base while excluding directional cast shadow from that metric; and record generated concept sheets as nonproduct elicitation aids only.
- [x] 18.2 TDD RED/GREEN: add public geometry tests for the final outlined horizontal and north-south structural bounds at the 60-pixel contract scale; require at most one source pixel of normal-axis imbalance around the shared edge for ghost, blueprint, frame, completed wall, closed door/frame, selection, damage, every Thin-only L/T/+ arm, and every exposed hybrid gutter; then implement one shared boundary-balance transform without changing canonical edge identity.
- [x] 18.3 TDD RED/GREEN: center both horizontal and north-south `SunShadowFade` bases on the shared edge, retain native celestial cast direction, and prove the base/shadow never creates a full-cell Thin volume, junction blob, overlap-darkened hybrid contact, or second owner.
- [x] 18.4 On the reviewed build, use native designators while RimWorld remains minimized through the Dev Gateway to place horizontal and north-south Thin Walls/Doors between furnished adjacent cells; retain close/ordinary/far screenshots with visible cell boundaries and buildings/workbenches on both sides, and mechanically record equal structural intrusion plus intact sprites and usable cells.
- [x] 18.5 Re-run the complete cumulative rendering matrix over Thin-only and mixed straight/L/T/+/side-T/door/material/damage/shadow states, personally inspect the exact in-game captures, obtain an unlabeled blind visual review, and invalidate every publishing capture/card until all rows pass together.

## 19. mods/ThinWalls — Core-derived atlas execution and adjacent-building clearance

- [x] 19.1 SPEC: record the exact installed-Core source families, 60x60 Thin and 120x120 hybrid runtime partitions, complete structural/material cache keys, the integral `+8` horizontal raster transform approximating the ideal `+7.5/60` correction without a world-space shift, centered-shadow exception, invalidation, exhaustive footprint matrix, and measured `33/60` render-only adjacent-building displacement in a normative implementation plan.
- [x] 19.2 TDD RED/GREEN: enumerate representative non-square occupied rectangles through all four rotations and prove every completed/blueprinted/framed Thin Wall and Thin Door on an internal north/east/south/west adjacency rejects placement while every exterior-only contact remains legal.
- [x] 19.3 TDD RED/GREEN: implement the shared structural placement transform across preview, blueprint, frame, completed, door, selection, damage, and hybrid gutter/aperture paths; mechanically prove the final outlined straight/L/T/+/door/hybrid bounds, arm-local North endpoint clipping against horizontal walls/door frames at standard and doubled widths, and both centered shadow bases.
- [x] 19.4 TDD RED/GREEN: implement cached ordinary-building/blueprint/frame `DrawPos` displacement for all four exterior contact sides, opposite-side cancellation, perpendicular composition, exclusions, and Thin-edge-version invalidation without changing any logical footprint or gameplay coordinate.
- [x] 19.5 REVIEW/VERIFY: run focused tests/build/package/OpenSpec validation, resolve independent review, then perform fresh minimized Gateway native placement/use/door/regular-merge workflows; personally inspect close/ordinary/far screenshots and obtain the unlabeled blind visual verdict before accepting the cumulative matrix.

## 20. mods/ThinWalls — Final-union topology, shadow, and close-clearance correction

- [x] 20.1 SPEC: retain the two 2026-08-20 annotated in-game references with SHA-256; require faces, terminal sides, contours, and shadow casting edges to derive only from the final union; forbid horizontal endpoint overhangs and internal L/T/+ endpoint remnants; and supersede nominal-plane `33/60` furnishing clearance with the measured nonzero-alpha `22/60` Core Hand Tailoring Bench clearance.
- [x] 20.2 TDD RED/GREEN: add exhaustive final-raster assertions over all 16 Thin masks and doubled variants proving every ray reaches its expected boundary, horizontal terminals are flush, occupied vertices contain no internal terminal side/face/outline, the north endpoint facade survives only on genuinely exposed halves, and L/T/+ shadows contain no raster-boundary or incident-ray-internal casting edge; then implement the minimum final-union compiler and shadow changes.
- [x] 20.3 TDD RED/GREEN: pin the Core Hand Tailoring Bench 224x96 canvas and `[16,16,208,89)` alpha bound, derive `22/60` from its actual 73-row normal silhouette plus the final 35/60 wall envelope, and verify phase-appropriate completed/blueprint/frame realtime positions, completed/blueprint cached graphic meshes, and completed shadow meshes without changing any logical footprint.
- [x] 20.4 REVIEW/VERIFY: run focused tests, owning Release build, package checks, and strict OpenSpec validation; obtain independent scoped code review and resolve every finding before live acceptance.
- [x] 20.5 LIVE: on the reviewed build, run fresh minimized exact-PID Gateway player workflows and retain close/ordinary/far screenshots for all 16 Thin masks, representative shadows, and opposing workbenches; personally inspect them and give only unlabeled screenshots to an independent reviewer. Supersede every earlier topology/workbench/publishing capture until this passes.
- [x] 20.6 RELEASE INPUTS: complete the fresh Gateway-based native player workflow, promote only newly accepted in-game renders, deterministically rebuild affected About/Workshop cards with no generated imagery, verify hashes/package parity, and stop at the local publishing handoff.

## 21. mods/ThinWalls — Superseded work queue; outcomes carried into section 22

- Superseded by section 22 — 21.1 SPEC: retain the three 2026-08-20 annotated user references with SHA-256; formalize the itemized publication-placement audit, zero-gap/facing chair rule, unobstructed door approaches, wall-headed bed, edge-balance pixel measurement, direction-specific building-clearance measurement, Thin-only internal Core-style perpendicular surface seams, and seam-free regular-wall side contacts without diagonal exterior width transitions.
- Superseded by section 22 — 21.2 TDD RED/GREEN: replace the symmetric `22/60` adjacent-building assumption with actual direction/rotation/phase alpha-clearance measurement; prove the pinned north/south Core Hand Tailoring Bench pair has no final-pixel wall overlap or excessive aisle across completed, blueprint, frame, and cached map-mesh rendering.
- Superseded by section 22 — 21.3 TDD RED/GREEN: derive designator-card cell boundaries from measured in-game grid/structural pixels and fail if the complete horizontal or north-south Thin silhouette is not bisected within one output pixel; remove hard-coded misleading overlay coordinates.
- Superseded by section 22 — 21.4 TDD RED/GREEN: add deterministic Thin-only L/T/+ phase-seam geometry and seam-free mixed regular-wall side contacts from measured Core donors; require continuous horizontal top/facade, one-pixel internal diagonal phase seams only for Thin-only perpendicular unions, byte-continuous regular-wall tops, unchanged alpha/exterior contour/shadow, and no wedge, tab, pasted overlap, or shortened arm in every rotation.
- Superseded by section 22 — 21.5 REDESIGN: revise the realistic-base design record and native room fixture so the divider position and both regular-wall contacts align, the bed head is visibly against its backing wall, every pictured chair is immediately adjacent to and faces its served table/workbench, every door/approach and primary aisle is clear, and no unexplained prop remains.
- Superseded by section 22 — 21.6 REVIEW/VERIFY: run focused tests, the owning Release build, package checks, strict OpenSpec validation, and independent scoped code review; resolve every finding before live acceptance.
- Superseded by section 22 — 21.7 LIVE: on the reviewed build, run fresh isolated minimized Gateway player workflows for preview rotation/placement, two-sided workbenches, all Thin-only L/T/+ rotations, mixed regular-wall unions, doors, and the redesigned room; personally inspect close/ordinary/far screenshots and retain the exact causal actions and pixel measurements.
- Superseded by section 22 — 21.8 PUBLICATION: give unlabeled in-game renders to an independent visual reviewer and separately require an itemized publication-placement audit; only after both pass, promote the fresh captures, deterministically rebuild every affected title/About/Workshop card, verify exact source/output measurements and hashes, and present the renders to the user for review without Steam mutation.

## 22. mods/ThinWalls — Source-only native-material mesh pivot

- [x] 22.1 RESEARCH/SPEC: inspect the installed Naname reference read-only, pin source/version identities, distinguish its material/UV mechanism from owned UI assets, record the source-only plan and explicit supersession, and independently review/strictly validate the revised contract.
- [ ] 22.2 MEASURE: revalidate current Core wall/door/damage/phase material identities and independent top/front/both-side/contour landmarks; retain source/reference comparisons and numeric mesh/UV target maps without adopting Naname coordinate presets or copying any product pixels.
- [ ] 22.3 TDD/TRACER: RED/GREEN one native-source horizontal straight and its native designation-to-render workflow, then the independently mapped north-south form and both owner descriptions. Prove source texture/mask/shader identity, full height, 7/60 top, balanced projection, Core outline/contrast, and repeat-phase continuity before adding junctions.
- [ ] 22.4 TDD/THIN UNION: implement the 16 Thin masks as shared-boundary source-region meshes, with exact surface ownership, no per-pixel quads, internal end faces, extra contours, gaps, or pasted posts. Clip each single-ray terminal exactly to its grid vertex longitudinally while retaining the full balanced normal projection; verify fixed-camera L/T/+ occlusion and source-defined internal perspective seams.
- [x] 22.5 TDD/MIXED UNION: specialize only the exact four-rotation, two-receiver regular-wall side-T by retaining one uninterrupted Core-width receiver run and restoring the previously accepted centered source-derived diagonal shoulder inside one bounded aperture; forbid terminal caps, exterior-contour changes, displaced branches, vertical seams, and pasted overlays; prove a lone regular-wall cell is ignored without suppressing either native participant, and restore native printing on final-contact removal.
- [ ] 22.6 TDD/PHASES: wire hover/blueprint/frame/completed/selection to the same geometry, retain Q/E versus multi-cell drag authority, use Core door mover sources for clipped moving leaves, and use only Core damage/shadow materials clipped/derived from final owner surfaces.
- [ ] 22.7 TDD/COEXISTENCE: retain exhaustive footprint-crossing rejection for wall/door phases in all rotations; recalculate minimal directional building render displacement from actual alpha and new mesh bounds; verify no occlusion or artificial aisle in independently measured live renders.
- [ ] 22.8 RETIRE/VERIFY: remove obsolete surface painters, runtime painted-atlas/material-color baking, rejected renderer paths, superseded tests and publishing inputs only after replacement coverage; run focused nonzero tests, owning build/package checks, independent code review, and strict OpenSpec validation.
- [ ] 22.9 LIVE/REVIEW: on the reviewed build run fresh minimized Gateway native workflows and cumulative close/ordinary/far material/phase/junction/door/damage/shadow/two-sided-building evidence; personally inspect it, pass context-free independent visual review, and retain exact build/source/process/actions/measurements/log/config/cleanup identities.
- [ ] 22.10 PUBLICATION: redesign credible scenes with the realistic-base skill, pass itemized placement audits and pixel gates, promote only fresh accepted in-game captures, rebuild affected About/Workshop cards and source-only material descriptions, and present local renders for user review without Steam mutation.

### 22.x implementation evidence in progress — 2026-09-04

- Horizontal and north-south Thin-only source meshes are implemented from the measured Core 60-pixel
  Bricks/Planks/Smooth inner tiles. Focused tests retain the top/front/side/outline landmarks, balanced
  34/60-cell envelope, 7/60 top, UV phase, and actual linked material identity.
- All 16 Thin-only linked masks are implemented as native-material rectangles and complementary
  face triangles. Fresh native designations produced an ordinary/far catalog at 40.9088135 and
  15.5553589 pixels per cell without a `TW_` texture binding. The context-free visual review found
  no gap, overlap, perspective, connection, or far-zoom identity defect in this narrow catalog.
- The temporary capless-butt interpretation of the mixed side-T was superseded after direct comparison
  with the previously accepted in-game reference. South, north, east, and west Thin contacts into exact
  two-cell regular runs now retain the receiver's actual linked Core atlas state outside one bounded
  aperture and restore the centered source-derived diagonal shoulder inside it. The receiver run remains
  uninterrupted; no branch is displaced, no terminal cap survives inside the admitted junction, and
  one-cell regular contacts are deliberately ignored with both native terminal silhouettes intact.
- The retired 120-pixel regular compositor, runtime texture builder, and their implementation-shape
  tests are removed. Static Thin Wall damage now uses a realtime attached pass bound directly to Core
  scratch materials; `20260904T202204186Z` visibly retained severe scratches on the edge surface.
- The rejected one-regular-cell corner compositor is removed. Single-ray endpoint bounds failed in all
  four rotations at `20260904T225116935Z` and passed with exact longitudinal `[vertex, midpoint]` bounds,
  full normal projection, convex winding, and fan area at `20260904T230620278Z`. Fresh minimized native
  designations in `20260904T230101520Z` retained all four admitted two-receiver side-Ts and showed four
  intentionally ignored one-wall contacts with independent native endpoints.
- The Core atlas gate now hashes the one-time decoded RGBA readback per concrete texture instance instead
  of accepting name and dimensions alone. The missing runtime comparison failed at
  `20260904T225842722Z`; the exact Bricks/Planks/Smooth identity tests passed at `20260904T225947893Z`,
  and the running Bricks source produced the pinned
  `9103d223a5c8c5c38cda6b3b7c423e2d8c8dfaa5fcc719311ef0013d68b12044` identity in
  `20260904T225700101Z`.
- The ignored-contact terminal shadow initially retained one inclusive center row/column for its south
  and west rays: four-direction bounds failed 2/4 cases at `20260904T231241125Z` and passed 4/4 after
  the exact `Center - 1` correction at `20260904T231308100Z`; the full Unit suite then passed 340/340
  at `20260904T231317572Z`, and independent review found no remaining issue in this resolution.
- Corrected reviewed build DLL `362157A5DFE4300AD39C8171277648C283AC26E7EE3BCE5749DDCA4CE87D7746`
  passed the fresh minimized contact-boundary player workflow `20260905T082133808Z` and cumulative
  37-capture native visual/material workflow `20260905T082350594Z`. The focused workflow used native
  designation and deconstruction across a real map-section boundary: all four exact two-receiver
  side-Ts restored the centered diagonal shoulder while preserving the native Core receiver, all four
  lone receiver contacts kept independent caps, the offset pair remained disconnected, and receiver
  removal immediately restored the ordinary Thin terminal. Personal inspection found no gap, displaced
  centerline, longitudinal endpoint overhang, or shadow discontinuity. A fresh context-free reviewer
  independently read all four close-ups as coherent rotations of the same thick-run/thin-branch T and
  found no rejection-level seam, hole, overdraw, displaced centerline, endpoint overhang, or shadow
  discontinuity; the wider catalog remained readable. Unit passed 327/327 at
  `20260905T082046087Z`, Harmony passed 7/7 at `20260905T082051014Z`, and the scoped source/asset Def
  contracts passed 8/8 at `20260905T082103365Z`. Package digest
  `B020637B896A47BE4DC050A61516A5E67AC43ACC704326A47D949EBE6C2CA077`
  passed the allowlist check; strict OpenSpec validation passed 12/12. Broader unresolved visual rows
  remain explicit under 22.9 rather than being waived by acceptance of this corrected mixed-union slice.
- Retained RED/GREEN evidence includes `20260904T175507838Z` -> `20260904T175557044Z` for the superseded
  first mixed aperture, the live painted-source RED `20260904T175924311Z` -> native-source run
  `20260904T180200016Z`, capless-plan compile RED `20260904T232535441Z` -> 8/8 GREEN
  `20260904T232714160Z`, removed-Core-interception RED `20260904T234012707Z` -> GREEN
  `20260904T234316089Z`, and admission-API compile RED `20260904T234138909Z` -> 17/17 GREEN
  `20260904T234300011Z`. Independent scoped code review found no remaining issue in the exact
  native-receiver/capless-stem slice. The unsupported future Core-source diagnostic fallback and the
  broader visual findings remain explicit open work rather than being waived by this slice.

## 23. mods/ThinWalls — Construction phases, content compatibility, and compact enclosures

- [x] 23.1 WORK: reduce Thin Wall base construction work to 67.5 (half Core's 135), retaining native Stuff factors; verify the finalized same-material ratio and an ordinary hauling/construction workflow. Thin Door remains 425 (half 850). Reviewed native runs: `20260905T110606171Z` and `20260905T110916829Z`.
- [ ] 23.2 MATERIALS: replace the former exact atlas fingerprint admission gate with capability-based native material resolution; preserve shader, mask, both colors, and source UV transforms. Verify actual material families and retain explicit limits on untested optional-mod claims.
- [ ] 23.3 PHASES: use the native wall blueprint atlas with the Thin mesh once, including the pre-click designator ghost; preserve distinct same-cell edges through native click/rotate/click and blueprint-to-frame construction. Reject only the duplicate physical edge, including an opposite-owner designation.
- [ ] 23.4 SELECTION: keep native selection bracket graphics/animation but measure their edge-centered 1 by 34/60 envelope for wall/door blueprints, frames, and completed buildings in all orientations.
- [ ] 23.5 COMPACT U: test all four one-cell-interior U orientations and a native small shelf, retaining three edge identities, legal placement, stable logical position, and bounded non-jittering render offsets. Measure opposing clearances. If the native sprite cannot fit by translation, retain an explicit failed visual gate pending an agreed rendering policy; do not silently crop or scale.
- [ ] 23.6 REVIEW/LIVE: resolve independent scoped review, run affected host/package/spec checks, then personally inspect the reviewed build's native construction, placement, material, selection, and mixed-contact workflows in fresh minimized Gateway processes. Keep unresolved visual rows and publication work open.

### 23.x implementation/acceptance boundary — 2026-09-05

The current reviewed package passed six scoped minimized native workflows (materials/menu icons,
ordinary wall lifecycle, funded co-located door frames, footprint boundaries, mixed contacts, and four
compact U rotations). The full-atlas map/icon defects are removed; different-edge blueprint IDs and
funded frame resources survive further designations; selection follows the canonical edge. Source
fingerprints no longer reject content. Actual Core wood/granite/steel/plasteel/uranium/gold/jade placements
passed; diagnostic Material cloning is supporting evidence only, not a blanket optional-mod claim.

The compact U remains a failed visual case: opposing walls leave 26/60 cells of projected width while
the native small shelf occupies 64/60. Translation alone cannot fit it. The impossible-axis offset stays
stable at zero, logical placement is valid, and no scaling/cropping has been silently added. A size/depth
policy needs user direction before visual acceptance. Broader phase/junction/material/zoom and publication
rows remain open. The local evidence ledger is
`artifacts/ThinWalls/2026-09-05-construction-phases/review-and-verification.md`.

## 24. mods/ThinWalls — Remove unnecessary scans and inactive-map work

- [ ] 24.1 RED/GREEN: prove ordinary native movement on a map without Thin structures allocates no Thin connectivity grid, then remove repeated map-component scans and bypass empty-map connectivity work while preserving first/final-edge transitions.
- [x] 24.2 SIMPLIFY: remove unnecessary door-list sorting/filter allocations, inactive door countdown occupancy scans and duplicate owner-region invalidation; preserve door access and scheduled buffer lifetime.
- [x] 24.3 REVIEW/VERIFY: resolve independent scoped review, run affected nonzero host checks, validate the package/spec, and retain reviewed build identity.
- [x] 24.4 LIVE: personally inspect fresh minimized native movement, first-edge construction/final removal, closed/corner routing, door opening/access/hold-open/closing and save/load workflows; retain action/result screenshots, logs and cleanup. Scope performance conclusions to measured evidence.

2026-09-05 scan-cleanup verification: the reviewed DLL `9E7EB2A717BE3CF4A10C1B8CD87127BD349511DD365DDAB615D08CA8DA999BA9`
is installed locally. Five scoped native workflows passed in six fresh minimized runs, including a
reviewed extension that visibly captures a forbidden door's disabled move menu before allowing and
crossing it. The acting agent inspected all retained action/result screenshots. Thirty focused host
checks pass, normal configuration/preferences match before/after, and all exact processes, temporary
test staging and credentials were cleaned. Evidence:
`artifacts/ThinWalls/2026-09-05-remove-unnecessary-scans/review-and-verification.md`.
24.1 remains open only for its historical native empty-overlay RED record: host component-cache
RED/GREEN and the final native empty-overlay GREEN exist, but the old-build failing native allocation
run was blocked while the user game was open and was not retrospectively manufactured. No new colony
FPS or stall-reduction measurement is claimed.

## 25. mods/ThinWalls — Enclosed-room access, bridge correctness, and stale path-delta fix

- [x] 25.1 RED (live): reproduced the player-reported failures on an isolated Harmony/Core/ThinWalls/Gateway
  map after native thin-wall/door construction: repeated native `ran out of path nodes while pathing`
  errors when ordering a drafted colonist into the sealed room, and the reachability bridge
  intermittently returning false (`bridge=False, native=True`) after natural construction. Captured in
  the gateway log ring of run `20260920T130416937Z-manual` and preserved under
  `artifacts/ThinWalls/2026-09-20-thin-room-pathing/`.
- [x] 25.2 RED→GREEN (native E2E): new `thin-walls.native-enclosed-room-access` workflow — natural
  colonist construction of a 19-wall room plus one east Thin Door, thick-wall roof-extension anchor,
  repeated drafted interior/exterior orders, and a zero-new-pathfinder-failure assertion. Failed on the
  unfixed build (`bridge=False` menu rejection and truncated 2-node corner-slip diagnostic path) and
  passes after the fixes: run `20260920T190935377Z`.
- [x] 25.3 GREEN (host): `ThinEdgeBridgeReachability` no longer discards a crossing after its first
  attempt, so searches re-process it from every newly opened cell; focused regression
  `APawnAlreadyOnTheFarSideCanStillReachTheFirstCrossingsExit` added; ThinWalls.Unit 359/359.
- [x] 25.4 GREEN (host): `NotifyCompletedEdgeChanged` now issues native path-data deltas for every
  `ThinWallConnectivity.Removals` cell (six per edge) instead of only the two owners, so vanilla's
  incremental gather cannot leave endpoint-crossing diagonal bits stale; covered by the E2E
  `pathNodes > 3` corner-slip assertion plus existing SparseEdgeMask connectivity contracts.
- [x] 25.5 REVIEW/LIVE: independent scoped code review resolved (bridge termination verified, vacuous
  roof assertion tightened, dead fields/duplication removed), remaining focused host suites rerun
  green, package/spec validation passed, and the reviewed build (`FC68B857...`) passed the minimized
  live player workflow: native designator room + door, natural construction, drafted native move
  order into the sealed interior arrived with a clean log, and completed-wall selection brackets were
  personally inspected hugging the edge envelope (`selection-closeup2.png`). Evidence:
  `artifacts/ThinWalls/2026-09-20-thin-room-pathing/review-and-verification.md`.
