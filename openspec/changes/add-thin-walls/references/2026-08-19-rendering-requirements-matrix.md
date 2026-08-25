# Thin Walls cumulative rendering requirement matrix

This matrix is the human-review checklist for the Thin Walls renderer. The capability specs remain normative; this document makes the accumulated decisions auditable in one place so a later correction cannot silently regress an earlier requirement.

Generated concept sheets are disposable requirements-elicitation aids only. A concept sheet passes review only when every active row below passes together. A positive annotation accepts only the named property at that location; it does not accept the whole sheet or waive another row. No generated concept pixel is a runtime, package, Workshop, or visual-style input.

## Edge placement and adjacent-cell readability

| ID | Requirement | Mechanical or visual acceptance |
| --- | --- | --- |
| `P-01` | A Thin Wall/Thin Door belongs to one canonical undirected shared edge, never a cell center. | Horizontal segments lie on a north/south cell boundary; north-south segments lie on a west/east cell boundary. The opposite owner description addresses the same slot. |
| `P-02` | Placement is balanced from the complete structural projection, not merely the dark top band. | At 60 source pixels per cell, the final non-shadow structural-alpha bounds have at most one pixel difference between their two normal-axis excursions from the projected shared boundary. |
| `P-03` | Horizontal structures preserve equal visible space north and south. | The complete top/front/outline body straddles the horizontal cell boundary; it is not centered inside either cell and is not assigned wholly to one side. |
| `P-04` | North-south structures preserve equal visible space west and east. | The complete side/top/outline body straddles the vertical cell boundary; it is not centered inside either cell. |
| `P-05` | Every phase uses one normal offset. | Hover ghost, rotated single-click preview, drag preview, blueprint, frame, completed wall, completed/closed door, fixed frame, selection target, and damage preserve the same balanced cross-section. Door leaves move only longitudinally. |
| `P-06` | Two-sided furnishings remain readable and usable without an artificial aisle. | Compatible buildings/workbenches may occupy both adjacent cells. Clearance is derived independently for each contacted side from the concrete final rotation/phase nonzero-alpha silhouette and complete wall silhouette, rounded upward to one `1/60` source-pixel quantum with a one-pixel safety band. The pinned Core Hand Tailoring Bench retains its `224x96` canvas and `[16,16,208,89)` alpha bound, but the rejected symmetric `22/60` split may not be reused: the accepted crop hid part of the north bench. No sprite is clipped, erased, bisected, overpainted, hidden behind, or fused into the wall, and no larger displacement may create an artificial aisle. Logical footprints/interactions never move. |
| `P-07` | Sun shadow does not redefine placement. | The physical `7/60` shadow base is centered on the edge. Directional cast shadow follows RimWorld's celestial vector and is excluded from `P-02` measurement. |

## Projection, height, and materials

| ID | Requirement | Mechanical or visual acceptance |
| --- | --- | --- |
| `G-01` | Thin and regular walls have one top elevation, one foundation elevation, and the same full apparent wall height. | A width transition never changes height. Only plan/top footprint width changes. |
| `G-02` | The top plane is the same material across a regular/Thin union. | The Thin top is the same dark Core-derived surface and tint as its same-material regular neighbor, merely narrower; no light cap or different top treatment appears. |
| `G-03` | Horizontal Thin walls retain the complete Core front face. | The seven-row top is paired with the full 22-row, three-course front face and Core-equivalent contour. No one-/two-course fence, rail, lip, or flat strip is accepted. |
| `G-04` | North-south Thin walls retain the complete Core perspective and material identity. | The seven-column top, eleven-column west side, ten-column east side, and complete 28-column by 22-row exposed south-end facade remain visible where exposed. Stone sides exclusively owned by North/South retain link-5 alpha/mask/profile while borrowing phase-aligned link-10 three-course facade detail; horizontal endpoint and H/V shared-owner mixed-junction caps are excluded from that remap. They may not become smooth dark rails. No needle, tower, post, or rotated horizontal sprite is accepted. |
| `G-05` | Dark top/light face separation remains legible. | Core top/front and top/side luminance ratios remain within the specified tolerance after Stuff tinting; orientation is readable at ordinary and far useful zoom. |
| `G-06` | Exterior outlines match Core. | Exactly the Core-equivalent two solid-dark exterior pixels plus one antialias transition surround only the final union. No thicker cartoon outline or missing contour is accepted. |
| `G-07` | Material families remain structurally appropriate. | Stony uses Core Bricks; Woody uses Planks plus deterministic OSB face marks while any retained structural timber is solid wood; Metallic uses Smooth plus deterministic plates/rivets. Rejected post/cap architecture is not revived. |

## Continuous Thin topology

| ID | Requirement | Mechanical or visual acceptance |
| --- | --- | --- |
| `T-01` | All 16 Thin-only masks use one linked-wall grammar. | Endpoints, straights, L, T, and + unions have constant-width axis-aligned arms and one connected silhouette. Every requested ray reaches its intended half-cell boundary. At the common vertex, a perpendicular or collinear continuation suppresses the incident ray's complete terminal face/cap before faces are derived; a north arm joined to either horizontal arm is not an exposed endpoint and retains none of its 28-by-22 south-facing terminal facade. A translated south arm meeting a non-door horizontal wall keeps a complete seven-pixel hidden bridge whose rows `31..37` render as horizontal front and only rows `38..44` remain exposed top. A door-only horizontal contact instead keeps only its three-pixel fixed-frame top bridge through rows `31..37`; doubled South ownership does not widen it. A mixed wall+door vertex uses the wall facade bridge. No shortened arm, terrain notch, bright rectangular tab, door-aperture slab, or internal outline is accepted. |
| `T-02` | Junctions and terminals have no pasted structural part or longitudinal overhang. | No post, pillar, crown, terminal side slab, cap, stitch, bridge ray, collar, ramp, miter, wedge, chamfer, overlapping arm plane, or raised top exists. Horizontal top/front/contour terminate at the same requested vertex; a side projection may not extend longitudinally beyond that endpoint. The camera-visible south facade of a genuinely exposed north-going arm remains the one explicit exception required for full wall height. |
| `T-03` | Faces and contours are derived only from the final union. | Only a boundary of the final structural union may create a visible face, side, outline, or antialias pixel. Collinear partitions, cell boundaries, ownership boundaries, and valid intersections have no black seam, groove, internal endpoint face, or ray-local leftover. |
| `T-04` | Continuous material phase crosses cells and vertices. | Stone keeps uninterrupted three-course running bond; mortar joints stop at course boundaries; wood/metal marks do not reveal 30/60-pixel partitions or create full-height lines. |
| `T-05` | Equivalent rotations/reflections are equivalent. | Every N/E/S/W rotation and mirror of an accepted endpoint, L, T, or + state preserves the same dimensions, balance, material phase, and contour rules. |

## Regular-wall hybrid topology

| ID | Requirement | Mechanical or visual acceptance |
| --- | --- | --- |
| `H-01` | A valid regular/Thin contact is one linked silhouette. | The regular wall uses its ordinary linked state to its cell perimeter; the constant-width Thin arm begins outside it. The shared top is continuous and has no internal outline. |
| `H-02` | Width changes are square and localized. | The transition is one abrupt axis-aligned shoulder at the regular tile perimeter. Non-straight Core slots classify affected pixels only; every final native top/front/side byte comes from straight link 10 or 5. No diagonal angle, staircase, bevel, stretched Core corner, or transition distributed through the regular cell appears. |
| `H-03` | Exact incidence controls connection. | Endpoint, corner, and side-T contacts join only when their topology shares the exact grid vertex/perimeter. Offset, parallel, or screen-overlapping near-misses keep separate contours. |
| `H-04` | A side-T remains vertex-centered. | Both adjacent regular tiles replace only their half of the seven-pixel contact's two-dark-plus-one-antialias contour depth with phase-aligned regular-material top/front/side structure; the receiving regular run stays straight; one canonical owner emits the exterior Thin gutter; no branch is displaced into either cell, and no transparent slit or regenerated outline survives inside the union. |
| `H-05` | Regular pixels remain regular. | Outside the declared semantic aperture, native diffuse and Stuff-mask bytes remain unchanged. Pre-existing native structural bytes are never overpainted; only intersected contour bytes become regular-material continuation. Native-region shoulders use regular material; gutter pixels use Thin material; mixed Stuff changes at one perpendicular boundary without a dark butt outline. |
| `H-06` | Multiple contacts compile once. | Straight/L/T/+ hybrids with one through four Thin rays include every ray in one nonoverlapping composition. No preferred bridge ray, dropped arm, second connector plane, or native wall underneath is accepted. |
| `H-07` | Removal restores native/Thin endpoints. | Removing a Thin participant restores the exact native regular print; removing the regular wall restores the correct Thin-only endpoint with no stale pixels or shadow. |

## Doors, damage, shadows, and phases

| ID | Requirement | Mechanical or visual acceptance |
| --- | --- | --- |
| `D-01` | Thin Doors use the same balanced edge geometry. | Both adjacent cells remain visually and physically usable; only crossing the door edge opens it. Fixed frames are flush, sparse, and never raised posts/caps. |
| `D-02` | Closed/open leaf geometry partitions the edge exactly. | Closed frame/leaves have no gap/overlap and retain a material-readable `0.84` panel tone; full open leaves a centered `0.60` passage with two visibly identifiable real `0.15` leaf panels and no hanging half-cell slab. |
| `D-03` | Damage belongs to its owner and final surfaces. | Moderate/heavy/severe marks are material-specific, progressive, clipped to top/front/side geometry, and never appear beside the wall, in a door opening, or on an intact participant. |
| `S-01` | Shadows come from the final top-footprint union. | One native `SunShadowFade` union follows the same completed Thin-only or hybrid top footprint. Casting edges are extracted only from its exterior boundary; an incident ray may not cast from a vertex endpoint or side that another ray occupies. Vertex intersections are owned once, raster-partition boundaries never cast, and there is no detached ray-local shadow, dark diffuse copy, missing shadow, full-cell Thin shadow, junction blob, overlap-darkened contact, or native shadow underneath. |
| `F-01` | Preview and construction phases cannot change topology. | A valid placement looks like the same object from hover through completion, aside from legal blueprint/frame/material/door-motion state. |
| `F-02` | Buildings never extend through a Thin edge. | For every rotation and Thin Wall/Thin Door completion phase, a footprint with cells on both sides is rejected; a footprint touching only the exterior perimeter remains legal and receives only the bounded render displacement in `P-06`. |

## Review and evidence gate

| ID | Requirement | Mechanical or visual acceptance |
| --- | --- | --- |
| `R-01` | Requirements are cumulative. | Every new prototype prompt and audit includes every active matrix row. Fixing the latest annotation while regressing an earlier row is an automatic rejection. |
| `R-02` | Positive examples are property-scoped. | The 2026-08-19 horizontal example around `(45.7%,36.9%)` approves boundary balance only; the vertical example around `(65.7%,30.4%)` approves boundary balance only. They do not approve height, materials, contours, or the entire sheet. |
| `R-03` | Concept sheets cannot accept product art. | Generated sheets remain ignored and nonproduct. Final product/publishing acceptance uses deterministic Core-derived runtime rendering and fresh in-game captures only. |
| `R-04` | Final review is in game and blind. | Close/ordinary/far screenshots show visible cell boundaries, all rotations/topologies/materials/damage/doors/shadows, and furnishings on both sides. An unlabeled independent reviewer identifies the object and reports no perspective, seam, outline, transition, or zoom defect. |

## 2026-08-19 annotation traceability

- Seventeen marked locations established `G-01`: width may change, wall height may not.
- Two marked regular/Thin contacts established `T-03` and `H-01`: an internal top seam is a regression.
- A marked horizontal arm established `P-01`/`P-03`: Thin geometry belongs to a cell boundary, not a cell center.
- The later horizontal note refined `P-02`: balance the complete rendered body, not only the top band.
- The later vertical note established the corresponding `P-04` rule.
- The two positively marked examples are retained only as `R-02` placement comparators.
- The 2026-08-20 annotated in-game catalog rejects horizontal terminal overhangs, internal L/T endpoint faces, shortened arms, ray-local junction shadows, and the malformed bottom-right L across every rotation/reflection.
- The later 2026-08-20 annotated publication feedback supersedes both nominal-plane `33/60` and symmetric `22/60`: actual wall-facing alpha bounds are directional, the north workbench must remain entirely visible, and visible excess gap is itself a failure.
- Perpendicular Thin-only L/T/+ contacts retain square exterior silhouettes while using Core-style one-pixel internal diagonal material-phase seams. Exact mixed regular-wall contacts preserve the receiving regular-wall structural bytes without that Thin-only seam. Neither rule permits an exterior wedge, ramp, chamfer, alpha gap, or gradual width transition.
