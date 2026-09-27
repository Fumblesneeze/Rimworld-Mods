## ADDED Requirements

### Requirement: Constrained visual offsets for narrow enclosures
Per-side measurements SHALL retain signed free space and intersect opposing translation bounds.
Choose the smallest feasible displacement from the native draw position, never the sum of conflicting
offsets. A one-cell U and all four rotations SHALL have before/action/after shelf-placement evidence.
An infeasible axis SHALL retain its native coordinate deterministically; no scaling, cropping, jitter,
logical relocation, or placement rejection may disguise an unresolved visual overlap. Such overlap
remains a failed visual gate, not an accepted zero-offset result.

#### Scenario: Simultaneous constraints have room
- **GIVEN** a west wall requires a 0.05-cell eastward shift and an east wall permits up to 0.10 cells
- **WHEN** the offset is resolved
- **THEN** the result is 0.05 cells east, not 0.15 cells

#### Scenario: Opposite walls cannot fit the sprite by translation
- **WHEN** the interval has no feasible displacement
- **THEN** that axis retains zero translation across repeated draws and mesh rebuilds
- **AND** live evidence reports any remaining occlusion rather than claiming a visual pass

Owning mod: **Thin Walls** (`fumblesneeze.thinwalls`) at `mods/ThinWalls`.

Rendering pivot: **2026-09-04**. This specification replaces the rejected generated-art and CPU-painted
60/120-pixel runtime-raster contracts. The implementation/research plan is
`references/2026-09-04-core-material-mesh-pivot.md`. Earlier drawings retain their observable geometry
requirements; earlier renderer outputs, hard-coded repair bands, decoration recipes, and visual approvals
are not acceptance evidence for this design.

### Requirement: All in-game art comes from installed base-game materials
The product SHALL own no gameplay texture files, embedded image resources, texture asset bundles,
generated bitmaps, procedural surface-painting recipes, or copied Core pixels. Walls, Thin Doors,
construction phases, damage, and command icons SHALL derive their appearance dynamically from installed
Core graphics/materials. Ordinary Stuff appearance, colors, masks, alpha, shader, and source texture
sampling SHALL remain authoritative. Wood uses Core Planks, stone Core Bricks, and metal Core Smooth;
bespoke OSB flakes, rivets, metal plates, painted latch marks, synthetic cracks, and arbitrary panel
darkening are superseded by the source-only pivot.

Runtime geometry and UV clipping/remapping MAY reshape those sources. Production wall/door rendering
SHALL use source-bound meshes rather than painting new diffuse/mask atlases. A material clone MAY retain
the source textures and change only the explicitly required native phase color/shader or draw ordering.
A temporary readback MAY measure a source once outside the render hot path; it SHALL NOT become a
repainted render input. Native shadow and damage materials are valid source materials, not owned textures.
About/Workshop images remain presentation-only exact in-game captures and never feed gameplay rendering.

#### Scenario: Source-only package
- **WHEN** the source tree, assembly resources, and exact positive-allowlist package are inspected
- **THEN** no gameplay raster, embedded bitmap, texture bundle, copied Core pixels, or Naname assembly is present
- **AND** each gameplay draw material resolves to an installed Core source rather than a mod-painted texture

#### Scenario: Native material identity
- **WHEN** equivalent stone, wood, and metal walls are inspected beside their Core counterparts
- **THEN** both use the same respective base-game surface detail and Stuff response without added painted marks

#### Scenario: Naname is inspiration only
- **WHEN** Thin Walls runs with Core and its declared Harmony dependency
- **THEN** the renderer requires neither Naname Walls nor any other repository product or dev-only mod

### Requirement: Source and target geometry have separate measured contracts
Before implementation, the renderer SHALL record the exact installed game/source identity, linked-atlas
layout, UV transform, top plane, front face, both lateral faces, exterior outlines/antialias, door
landmarks, and source damage regions. The previously inspected Core layout (320 atlas, 80 slot, 10 gutter,
60 inner sample, 33 top, 22 front, 11 west side, 10 east side) is reference data that SHALL be revalidated,
not a replacement for current measurements. A Thin top footprint remains 7/60 cell while front height
and lateral projection remain equal to the corresponding measured Core wall.

Horizontal and north-south forms SHALL have independently measured UV/position mappings for RimWorld's
fixed camera. Rotating or shrinking a complete horizontal sprite into the vertical form, shrinking the
front height with the top, painting a new outline, and pasting a post over the wall are forbidden.
The source outline weight and dark-top/light-face contrast SHALL be preserved at equal zoom. Geometry
partitioning SHALL preserve source-defined interior top/face corner seams without adding exterior
diagonal width transitions.

#### Scenario: Full height with a narrow top
- **WHEN** same-material Thin and regular walls are compared at the same close view in both axes
- **THEN** the Thin top is narrow but the full visible face height and lateral projections match Core
- **AND** the stone facade retains the Core three-course pattern without shortened courses or new vertical strokes

#### Scenario: Mod-added material and replacement atlas
- **WHEN** eligible Stuff supplies another appearance, a replacement wall texture, a mask, or a compatible shader
- **THEN** the renderer attempts native linked sampling using that resolved material and preserves its tint, mask, shader, and source binding
- **AND** texture names, pixel hashes, resolution, and absence from a Core allowlist do not reject rendering or construction

Source fingerprints are verification evidence only. Genuine missing sources may use the native
fallback appearance or an edge-sized single-slot fallback with one bounded warning per source.
Never print a complete atlas as a fallback, silently hide an existing collision boundary, or disable
new construction solely because content was added by another mod. Resolve appearance before
applying the final material's primary and secondary colors so an appearance wrapper does not reject
a valid secondary tint. Ordinary regular-wall linked rendering remains native outside admitted contacts.

### Requirement: Every phase uses the canonical edge and a balanced structural projection
The canonical undirected SharedEdge SHALL remain the placement, collision, room, topology, and save
identity. Ghost, blueprint, frame, completed wall, closed door/frame, selection, and attached damage
SHALL share one geometry contract. Door motion changes tangential position only.

Let b be the independently projected shared cell boundary and min/max the complete non-shadow structural
bounds on its normal axis. At the 60-source-pixel-per-cell comparison scale,
abs((b - min) - (max - b)) SHALL be at most one source pixel. This includes top, visible faces, contour,
and antialias, not just the dark top. Regular-wall native in-cell geometry and cast shadows are excluded.
The same bounds SHALL continue along each exposed constant-width arm of a junction. A junction's internal
occlusion SHALL NOT move an arm to a cell center.

#### Scenario: Both orientations share space equally
- **WHEN** horizontal and north-south Thin structures are built between visible cell boundaries
- **THEN** their complete structural bodies consume equal visible space from either adjacent cell within tolerance
- **AND** reversing the owner description yields identical geometry rather than an offset or doubled wall

#### Scenario: Preview matches placement
- **WHEN** Q/E rotates a one-cell hover preview and the player clicks without entering a second cell
- **THEN** the native placement uses that visible orientation and the resulting structure aligns with its preview
- **WHEN** the drag first contains a second distinct cell
- **THEN** its orientation changes to the right-hand side of the cardinal drag vector

#### Scenario: Native blueprint appearance on edge geometry
- **WHEN** the player hovers, drags, or places Thin Wall or Thin Door blueprints in any orientation
- **THEN** the preview uses the native regular-wall blueprint atlas and phase tint/transparency on the Thin mesh's sampled regions
- **AND** each blueprint is submitted only once through its cached print path, never as a full atlas or a full-cell inherited print beneath a realtime duplicate

#### Scenario: Native Architect and Build copy menu icons
- **WHEN** the player opens the Architect tool or selects a Thin Wall/Thin Door and sees Build copy
- **THEN** the icon uses Core's wall/door menu icon, including native Bricks/Planks wall variants and Stuff tint, rather than an entire linked atlas
- **AND** the Thin Def's fallback graphic is a single native menu icon so the native `Graphic_Appearances` icon override cannot leak atlas pixels; map surfaces continue resolving the installed Core wall/door graphics independently
- **AND** frames retain edge geometry and material sampling when construction progresses

#### Scenario: Selection follows the visible edge
- **WHEN** a player selects a completed, blueprinted, or framed Thin Wall or Thin Door
- **THEN** the native animated selection brackets are centered on its canonical shared edge and fit its one-cell-long, 34/60-cell-wide structural envelope in each cardinal orientation
- **AND** the logical owner cell, footprint, pathing, room membership, and save identity do not move

### Requirement: One shared-boundary mesh plan defines each linked junction
Topology SHALL retain all incident north/east/south/west None/Thin/Ordinary rays, actual regular-wall
quadrants, canonical owners, material identities, construction phases, and doors. It SHALL enumerate all
81 directional states, reject impossible incidences explicitly, and preserve every physically valid arm.
None/Thin states SHALL cover the complete 16-mask catalog.

Each valid state SHALL compile disjoint source-bound top/front/side mesh regions with shared vertex
coordinates at their boundaries. Surface ownership, occlusion, exterior contour, damage clipping, and
shadow casting SHALL derive from that final union. One stable owner emits each region once. A top/front
material boundary can be visible in the native style, but a structural gap or internal endpoint contour
cannot remain. Source UV phase SHALL be continuous along repeated straight runs; neither cell nor owner
boundaries may split a brick or add a full-height line. Meshes SHALL be region-based, not one quad per pixel.

#### Scenario: Thin-only straight, L, T and cross
- **WHEN** all valid Thin-only states are viewed from the fixed camera
- **THEN** they are continuous wall unions with no overhang, notch, pasted foreground post, raised cap, internal end face, or doubled contour
- **AND** north-south surfaces meet the horizontal surfaces with the same coherent depth ordering as Core

#### Scenario: Terminal rays stop at their vertex
- **WHEN** a Thin ray has no collinear or perpendicular Thin continuation at one endpoint
- **THEN** its complete source-derived terminal profile ends exactly at that grid vertex on the ray's longitudinal axis
- **AND** its full projected thickness may straddle the two adjacent cells only on the normal axis
- **AND** only an actual Thin L, T, or cross, or an admitted two-regular-wall side-T, may occupy adjoining junction space beyond that terminal axis

#### Scenario: Long runs do not reveal mesh partitions
- **WHEN** six or more segments form uninterrupted runs in either axis
- **THEN** geometry and material phase remain continuous through cell and render-owner boundaries at all required zooms

### Requirement: Regular contacts require a continuous two-cell receiving run
A Thin endpoint SHALL connect to ordinary walls only when it meets the shared boundary between exactly
two adjacent regular-wall cells that form one continuous receiving run: the four rotations of the
retained side-T drawing. A lone regular-wall cell or quadrant at the same vertex SHALL be ignored by
Thin topology. In that intentionally unlinked case Core prints the regular wall unchanged and Thin
prints its ordinary terminal endpoint unchanged, even when their screen silhouettes touch or overlap.
Parallel, offset, diagonal-touch-only, and merely overlapping screen silhouettes likewise SHALL NOT
infer a connection.

At a valid contact the two regular-wall cells SHALL retain one uninterrupted Core-width straight run.
Inside one centered, bounded junction aperture only, the Thin participant replaces its exposed terminal
with the previously accepted source-derived diagonal shoulder shown in the 2026-09-05 regression
reference: the narrow stem broadens symmetrically into the regular run without a cap, vertical seam,
pasted overlay, displaced branch, or change to the regular run's exterior contour. Geometry outside the
aperture SHALL remain identical to the native Core receiver print. The stem may use a shallow altitude
transition inside that junction aperture, but its completed half-edge outside the aperture SHALL retain
the same completed-wall altitude as its opposite endpoint. No one-cell mixed corner, L, or end-on contact
is admitted.

The Thin arm remains at its shared edge, retains the normal `34/60` projection, and uses the matching
Core straight source bands without a terminal cap at the receiving vertex. The regular run remains
ordinary-width, owns its usual contour and shadow, and stays byte/material/UV-equivalent to the same Core
run without Thin Walls. Contrasting Stuff therefore meets at the ordinary perpendicular boundary without
requiring a synthetic mixed surface.

#### Scenario: One arm of a Core T is Thin
- **WHEN** one branch of an otherwise regular T is supplied by an edge-mounted Thin Wall
- **THEN** the regular receiver remains an uninterrupted straight silhouette and the Thin stem joins it through one centered symmetric diagonal shoulder matching the previously accepted in-game rendering
- **AND** the shoulder is part of the final source-derived junction surface rather than a second overlay or terminal cap

#### Scenario: Side-T in all four directions
- **WHEN** a Thin endpoint meets each side of a continuous regular run
- **THEN** its centerline ends at the shared vertex without a terminal cap or longitudinal overhang and the centered diagonal shoulder has the same geometry under rotation/reflection
- **AND** both receiver cells retain the native Core material, continuous exterior contour, and unchanged geometry outside the declared junction aperture
- **AND** the same rule holds for the user's two opposite side-T connections between parallel regular runs

#### Scenario: A lone regular wall is intentionally ignored
- **WHEN** a Thin endpoint shares a vertex with only one adjacent regular-wall cell
- **THEN** the regular wall retains its exact native linked rendering and the Thin arm retains its normal terminal rendering
- **AND** neither renderer creates an aperture, connector, diagonal wedge, cap replacement, ownership suppression, or inferred mixed shadow

#### Scenario: Unaffected rendering and removal
- **WHEN** an affected regular wall is compared with its ordinary linked print
- **THEN** its complete geometry and source sampling are identical
- **WHEN** the final Thin contact is removed through native deconstruction
- **THEN** the exact native regular-wall print is restored without stale contact geometry or shadows
- **WHEN** either of the two regular receivers is removed or replaced through a native player action
- **THEN** the remaining Thin ray immediately returns to its ordinary capped terminal after map-mesh refresh, including when the changed cells cross a map-section boundary

### Requirement: Damage and doors reuse native art without violating ownership
Thin Door moving leaves SHALL use the measured Core simple-door source, not darkened wall panels with
painted latches. Fixed boundary geometry SHALL join the source-derived wall topology flush at the same
height. The closed leaves and fixed frames SHALL partition the edge without gaps or coplanar overlap.
Opening SHALL move the source-bound leaf meshes along that edge, clip them against the fixed opening,
and reveal a clear passage without hanging leaf blocks or invisible door identity. The native opening,
hold-open, closing, and access semantics remain unchanged.

Damage SHALL use installed Core damage materials selected through an inspected native source contract.
Deterministic selection/density/placement MAY express the existing moderate/heavy/severe HP grades,
but SHALL NOT invent damage pixels, paint surface colors, or cut arbitrary holes into Core diffuse/masks.
Damage meshes and UVs SHALL be clipped together against the final owner-visible surface polygons.
They SHALL move with a door leaf, remain off intact neighbors and regular-wall materials, and never
float beside the edge or fill a door opening. Native cell-centered damage drawing SHALL not also render
for these Things.

#### Scenario: Native door crossing
- **WHEN** a colonist natively opens and crosses a closed Thin Door
- **THEN** visibly recognizable Core-derived door leaves open on that edge, the colonist crosses the revealed passage, and fixed Thin/regular wall contacts stay continuous

#### Scenario: Progressive source-derived damage
- **WHEN** native damage takes horizontal and north-south stone, wood, and metal Thin structures through their three HP grades
- **THEN** damage is progressively discernible, stays on the correct visible surface, and uses Core damage art only
- **AND** close and ordinary views distinguish the grades while far useful views still distinguish heavy from severe

### Requirement: Shadows follow the structural footprint through the native sun path
Completed structures SHALL cast native SunShadowFade shadows from the final physical footprint union at
regular-wall height. A Thin footprint remains centered on its canonical edge with 7/60-cell thickness;
visible facade projection SHALL NOT become physical shadow thickness. Internal incident-ray and
render-partition boundaries SHALL not cast independent edges. Mixed regular/Thin shadow volumes SHALL
meet without overlap-darkening or a retained native shadow under the replacement. Door shadows follow
the actual visible moving/fixed geometry and celestial direction, not a translated diffuse copy.

#### Scenario: Coherent casting
- **WHEN** straight, L, T, cross, mixed-width, and open/closed door structures are viewed in shadow-visible lighting
- **THEN** they cast one coherent directional shadow with no gaps, rectangular full-cell Thin blob, duplicated edge, or detached post shadow

### Requirement: Buildings on both sides retain legal footprints and intact graphics
Building-crossing rules SHALL remain based exclusively on internal adjacencies of the final rotated
CellRect for completed, blueprint, and frame phases of both Thin Walls and Thin Doors. A visual offset
SHALL never legalize a crossing footprint. Both adjacent cells remain usable when their footprints do
not cross the edge.

The 2026-09-27 unified-offset revision in `add-thin-wall-building-adjustments` supersedes the former
measured sprite-clearance solver. Eligible completed player buildings SHALL use only the existing
0.2-cell appearance preset away from contacted Thin sides, with opposing sides cancelling per axis.
The old DrawPos/TrueCenter overrides and texture-measurement caches SHALL be removed. Explicit player
offset choices SHALL take precedence, including center; Shrink remains independent and manual.
Ordinary blueprints/frames SHALL retain native rendering. Logical positions, footprints, interaction
cells, jobs, reservations and rooms SHALL not change. Regular walls/doors, Thin structures and linked
graphics SHALL remain excluded from cosmetic controls. No universal sprite-fit guarantee is made;
the player can choose a smaller scale through the existing gizmo without changing gameplay geometry.

#### Scenario: Both-sided workbenches
- **WHEN** the player builds compatible workbenches on both sides of a Thin boundary in each orientation
- **THEN** both native placements remain legal and usable and use the same offset presets as their gizmos
- **AND** the player can adjust offset and scale without an additional hidden clearance displacement

#### Scenario: Crossing remains illegal in every phase and rotation
- **WHEN** a non-square footprint is rotated through all four directions across completed, blueprinted, or framed Thin Walls or Thin Doors
- **THEN** every internal-edge crossing is rejected while exterior-only contacts remain legal

#### Scenario: Opposing sides and compact U
- **WHEN** a building is surrounded by a U with only the south side open
- **THEN** opposing east/west contacts cancel and the south preset is automatically assigned
- **AND** cancellation is not claimed as proof of sprite fit; manual Shrink remains available

### Requirement: The replacement is accepted only on fresh reviewed live evidence
Final verification SHALL use a fresh isolated minimized RimWorld process controlled through the Dev
Gateway, on the independently reviewed exact build. Testable source/UV/geometry/coverage/footprint
invariants SHALL receive focused RED/GREEN tests before implementation. Source provenance and host
geometry assertions alone are not proof of live rendering.

The live catalog SHALL include the 16 Thin masks, every admitted two-receiver side-T rotation, isolated
one-regular-cell contacts that remain intentionally unlinked, offset noncontacts, all materials and construction phases,
closed/open doors, damage grades, shadows, and two-sided buildings at close, ordinary, and far useful
zoom. Native designation/construction/crossing/damage/deconstruction actions SHALL causally produce
representative outcomes. Personally inspected before/action/after captures SHALL retain build and
source identities, mod order, actions, camera, independent cell projections, measurements, classified
logs, normal-config hashes, graceful shutdown, and cleanup.

An independent reviewer SHALL first receive only unlabeled multi-zoom game screenshots and identify
the structures, connections, perspective, materials, damage, and doors without context. Publication
scenes SHALL additionally pass the separate itemized realistic-base placement audit. No previous
renderer capture or approval may close this replacement or feed its About/Workshop cards.

#### Scenario: Mechanical and visual gates both pass
- **WHEN** the replacement is proposed for acceptance
- **THEN** source-only tests, cumulative geometry/pixel measurements, native live workflows, personal inspection, and independent blind review all pass on the same reviewed build
- **AND** no outstanding artifact or unmeasured material/phase is described as complete

#### Scenario: Publication remains gated
- **WHEN** new About or Workshop cards are prepared
- **THEN** their gameplay panels use only freshly accepted in-game captures and the separate publication capability's placement and measurement gates
