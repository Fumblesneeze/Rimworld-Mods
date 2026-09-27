# Thin Walls: native-material mesh pivot

Owner: **Thin Walls** (`fumblesneeze.thinwalls`), `mods/ThinWalls`.
Status: researched replacement design; implementation and live acceptance are **not complete**.

## Direction and supersession

User direction, 2026-09-04: no mod-owned textures; derive everything dynamically from base-game
textures; inspect Naname Walls for inspiration.

Use installed Core material inputs and reshape their sampled surfaces with meshes/UVs. Do not treat
"no PNGs in the package" as sufficient: the present renderer still paints new OSB, rivet, latch, seam,
and damage pixels at runtime. Those painters and the baked 60/120-pixel diffuse/mask atlas path are
retired by this design, not accepted as its implementation.

This document and the current rendering capability supersede the implementation prescriptions in
`2026-08-19-core-derived-atlas-and-adjacent-building-plan.md` and the renderer-specific portions of
`2026-08-19-rendering-requirements-matrix.md`. Source measurements are inputs to revalidate, not
approvals of the rejected output. Existing user drawings and observable requirements still apply:
equal full height, narrow dark tops, balanced shared-edge occupancy, continuous Thin and regular
joins, no unintended exterior angles, no posts/caps, no seams through brick courses, native shadows,
attached damage, two usable furnished sides, rotation previews, room behavior, and no roof support.

The material-art requirements deliberately change: wood inherits vanilla Planks and metal vanilla
Smooth. No independently invented OSB or riveted-plate pattern is retained. Damage must come from
Core, not material-specific crack/dent/fiber painting. Core simple-door art replaces wall panels with
invented latches. This changes appearance, not resource cost, HP, traversal, room, or roof rules.
About/Workshop screenshots are presentation outputs, not gameplay texture inputs; their separate
in-game-only and realistic-placement requirements remain.

## Inspected Naname reference

- Installed Workshop item: `3543791340`, **Hull Style: NANAME Walls**, OELS.
- Package: `OELS.NanameWalls`; local metadata: `1.6.54`.
- Installed `1.6/Assemblies/NanameWalls.dll` SHA-256:
  `196CE53FF1FEEA3D55F883BBAE45EF91DDC5CDF1866024BE22883AB84283AC1A`.
- Installed `DefaultSettings.xml` SHA-256:
  `0507C1B43E6B887304FBB42B596CFAA264647B73066E36B88DED8EC7B5E739DB`.
- Public source inspected separately at commit
  `09f9f1021a61d0b9342f1f068b809b869a7eefd7`:
  [repository](https://github.com/oelsart/NANAMEWalls/tree/09f9f1021a61d0b9342f1f068b809b869a7eefd7).
  Installed binary/source equivalence has not been established; these are two explicitly identified
  inspection inputs, not a claim that the public commit built the local DLL.
- The source carries CC BY-NC-SA 4.0. This design takes architectural inspiration only; no source,
  numeric mesh presets, assembly, or asset is copied into Thin Walls.

### What it actually does

In [Graphic_LinkedDiagonal.cs](https://github.com/oelsart/NANAMEWalls/blob/09f9f1021a61d0b9342f1f068b809b869a7eefd7/Source/NANAMEWalls/NANAMEWalls/Graphic_LinkedDiagonal.cs),
`LinkedDrawMatFrom` chooses a native linked submaterial through
`MaterialAtlasPool.SubMaterialFromAtlas`. `GetMaterial` uses the original material or a linked
submaterial and caches a material clone. `PrintConditional` and `PrintDiagonal` submit independently
defined positions, source UVs, colors, and triangles to a `LayerSubMesh`; their repetition controls
material phase without authoring another wall bitmap. Native printing is retained or selectively
changed according to the topology/settings.

[MeshSettings.cs](https://github.com/oelsart/NANAMEWalls/blob/09f9f1021a61d0b9342f1f068b809b869a7eefd7/Source/NANAMEWalls/NANAMEWalls/Settings/MeshSettings.cs)
and the installed settings distinguish UV sources from destination vertices and different conditions.
[GenerateDefs.cs](https://github.com/oelsart/NANAMEWalls/blob/09f9f1021a61d0b9342f1f068b809b869a7eefd7/Source/NANAMEWalls/NANAMEWallsWorker/GenerateDefs.cs)
copies the original graphic settings before choosing the new linker.

The useful lesson is **separate source material/UVs from destination geometry**. It is not "copy
Naname's diagonal shapes", "clone every wall Def", or "add their mod as a dependency".
Naname also owns UI assets in its asset bundle; its existence does not establish that its entire
package is texture-free. Thin Walls' no-owned-gameplay-texture constraint is stricter.

## Current Thin Walls audit

Read-only installation check on 2026-09-04 still reports Core `1.6.4871 rev590` and
`Assembly-CSharp.dll` SHA-256
`5CF1B5BE399D5B1C9C56CA72C9D35B4ECF307FEACF5859D04AC5A1AA5926356A`.
This confirms the engine identity only; source texture/door/damage measurements and new live evidence
remain a separate task.

| Existing path | Replacement responsibility |
| --- | --- |
| `CoreDerivedWallMaterialCache.BuildThinMaterial` paints new diffuse/mask arrays | Source-bound material plus region-mesh cache; no new rendered diffuse/mask input |
| `BuildHybridRegularMaterial` and `HybridRegularRasterCompositor` bake participants into a 120-pixel raster and five sampled crops | One mixed-width geometric plan, partitioned by original material only where needed |
| `HybridWallSurfaceDecorationRecipe`, surface treatments/colorizer, door panel tone | Remove; native material pixels remain authoritative |
| `HybridWallRasterCompiler` encodes projection repair bands and derived painted contour | Replace with separately measured source/target surface regions and shared mesh boundaries |
| Per-owner raster damage | Core damage material geometry/UV clipping against final visible owner polygons |
| Existing shared-edge/topology/placement/room/pathing model | Retain unless focused tests expose an actual renderer-integration defect |
| Adjacent-building clearance | Retain the directional measurement contract; recompute against replacement geometry |

The current Defs already refer to Core wall/icon paths and no gameplay Textures directory was found.
That is useful packaging groundwork, not proof that the current visual pipeline fulfills the pivot.
Thin Door currently uses the wall source in its graphic declaration; the replacement must explicitly
resolve a Core door mover for its leaves instead.

## Runtime structure

Keep the implementation local to `Source/Rendering`; no generic mesh framework or additional runtime
dependency is needed. Separate three concrete responsibilities, not an interface hierarchy:

1. **Source descriptor:** current Core material, linked slot/UV transform, phase shader/colors,
   measured top/front/left/right/contour landmarks, mask identity, source fingerprint and layout version.
   Preserve source texture bindings, native filtering, and Stuff uniforms. Readback is permitted only
   for bounded source measurement/diagnostics, never to repaint an input. Handle atlas replacement UVs
   as actual native bindings rather than assuming a zero-offset whole texture.
2. **Mixed-width surface plan:** canonical incidences produce target surface polygons with source UV
   mappings, material/Thing ownership, and exact shared boundary vertices. Thin-only nodes have 16 masks;
   the 81 typed-ray states are an exhaustive input table, not 81 texture files. Regular quadrants and
   contacts refine that table. Invalid physical incidence is rejected explicitly.
3. **Submission and lifecycle:** region-sized meshes batch by original material for cached map geometry;
   moving doors use the same local-coordinate source regions as realtime meshes. Cache by topology,
   source/material identity and version, Stuff colors/mask/shader, phase, door role, and owner-visible
   damage state where applicable. Dirty the actual affected nodes/regular tiles on either participant's
   lifecycle or material change. Own/dispose only mod-created meshes/material clones, never Core assets.

Updated 2026-09-05: source identities are measurement evidence, not a content admission list. Attempt
loaded native atlases regardless of texture name, fingerprint, or resolution; preserve the resolved
material's mask, shader, tint, and UV transform. Do not block construction merely because another mod
supplies the source or Stuff. Diagnose an actually missing texture once while preserving gameplay and
a visible fallback; do not describe that fallback as accepted wall art. Mixed ownership additionally
requires a native linked-print capability; unknown custom print overrides retain their ordinary wall
print and the complete Thin terminal rather than silently losing either participant.

No per-cell/per-frame GPU readback, Texture2D creation, raster composition, per-pixel quads, serialized
mesh cache, or full-map topology rebuild belongs in the draw path. Measure worst-case region/vertex
counts and cache lifetime in the focused catalog before accepting performance; do not invent a passing
budget without an observed baseline.

## Geometry and UV decisions

- Define physical footprint, projected structural envelope, and source UV coordinates separately.
  Do not rotate a finished horizontal image to obtain the vertical form.
- Revalidate the old Core 33/22/11/10 top/front/west/east measurements against the current game.
  Thin top width is 7/60 cell. Preserve front height, both side projections, native contour, and
  dark-top contrast independently. Match the complete projected envelope to both adjacent cells,
  not merely the top centerline; derive any translation analytically, not from the old +8 repair band.
- Split source atlas regions at measured surface/outline boundaries. Position deformation may narrow
  the top without scaling the face's height or outline weight. Share target boundary vertices between
  regions; source seams follow genuine surface orientation only. Keep longitudinal source phase
  continuous across integer cells and canonical owner changes.
- Resolve the complete top union before visible faces and contours. Remove internal end faces and
  contours geometrically. Region meshes must have nonoverlapping interiors and matching boundaries;
  masks/alpha must not be used to conceal independently outlined overlapping pieces.
- A regular-wall contact is a mixed-width atlas state. Declare its minimal aperture from incidence and
  surfaces before sampling. It includes a receiving face region whenever that region would otherwise
  cap the Thin top. Change that region into the source-mapped connected top/face geometry; preserve
  native geometry and UVs outside the aperture. Restricting all edits to three outer outline pixels
  is explicitly rejected. This is how the Thin top can actually reach the regular top.
- Do not stretch a complete Core T/L tile: source top, face, contour, and native interior corner seams
  are separate regions. Any width change remains a square exterior shoulder. Internal Core perspective
  seams are allowed only at actual surface transitions, never across a coplanar top connection.
- A side-T shares one boundary definition across two regular cells and the edge arm. Compose multiple
  contacts before submitting a receiving tile. Differing Stuff splits material batches along the same
  geometric boundary, with no overlapping quads, gap, or Thin-colored brace crossing the ordinary top.
- Use measured Core door mover regions for moving leaves; source wall regions for fixed flush framing.
  Clip positions and UVs by the same longitudinal parameter. Re-measure closed/open coverage and native
  depth order rather than treating the previously patched leaf travel/altitudes as accepted values.
- Use native damage source materials and deterministic HP-grade selection/placement. Core inspection
  found Wall `damageData` uses `Damage/Corner` and `Damage/Edge`; the native damage section layer also
  obtains scratch materials from `BuildingsDamageSectionLayerUtility`. Resolve and measure the actual
  sources in the fresh game. Clip damage against final owner-visible polygons; no cell-centered second
  overlay and no arbitrary RGB/alpha damage painting.
- Shadows use final physical footprint union and Core's native sun material; never project the visible
  front facade as extra physical thickness. Keep internal boundaries non-casting.

## Vertical implementation and acceptance

Section 22 in tasks.md is the active queue. Work one source-to-player-output slice at a time:

1. Measure current sources and implement one horizontal straight, then independently the north-south
   straight. Focused RED/GREEN tests prove source identity, UV bounds, full height, thin top, correct
   outline, repeat phase, and full-body edge balance. A minimized native designation proves the draw
   path is really using that reviewed source-bound mesh.
2. Add Thin-only masks with shared-boundary/coverage tests, followed by a single mixed-width T tracer
   and then all rotations, side-T pairs, intentionally ignored one-regular-cell contacts, and offset noncontacts.
   Test the contact's continuous top and unchanged native regions independently, not just "rays present".
3. Add native phase/door/damage/shadow sources. Retain construction identity and source binding tests;
   prove door crossing and native damage visibly on the exact build.
4. Recompute two-sided building clearance from final mesh envelopes; verify native footprint rejection
   independently of render offsets for non-square buildings in all rotations and all constructible phases.
   Use independent same-camera structural difference stages and cell projections to measure the actual
   rendered overlap/clearance. Include opposite and perpendicular multi-wall contacts.
   Intersect all allowed translation intervals before choosing an offset. Empty intersection is an
   explicitly reported unsupported visual-packing case, preserving legal/native placement rather than
   cropping/resizing sprites or falsely passing cancellation. The pinned 3x1 bench projects to 1.140625
   cells along its one-cell footprint axis, so walls on both opposing perimeter sides are a deterministic
   infeasible comparator. Two benches beside one shared wall remain a required feasible case.
5. Remove the now-unused raster/painter paths and their obsolete implementation-shape tests only after
   replacement behavior tests exist. Do not disable a failing gameplay or cumulative visual requirement
   merely to make the removal pass.
6. Independently review the scoped code; run only affected test IDs/build/package checks, then fresh
   minimized Gateway actions and close/ordinary/far captures. Inspect them personally and obtain a
   context-free visual review. The old screenshots are negative regression references, never evidence
   of the new implementation. Keep tasks unchecked until their evidence exists.
7. Only after visual acceptance, build credible publishing scenes through the realistic-base workflow,
   obtain the itemized placement audit, and replace About/Workshop images with accepted game captures.

This research/specification update does not change or accept the running product, claim Naname
compatibility, or authorize Steam publication.

## Specification review record — 2026-09-04

Independent scoped review `review_source_only_pivot` identified infeasible opposing-wall translation,
conflicting historical-reference authority, and unspecified visible behavior on source mismatch.
The current specification resolves these with interval-feasibility reporting/native-position fallback,
explicit historical headers, and Core-material diagnostic edge visibility with localized source failure.
A follow-up read-only review reported no issues in those resolutions.

Typed `openspec_validate` passed all 12 changes with zero failures; scoped `git diff --check` passed.
Only research/specification task 22.1 is complete. No product code, tests, runtime assets, live game
state, or publishing output was changed or accepted by this documentation slice.

## Implementation measurements and first mixed-width tracer

The following records the subsequent implementation, not acceptance of the complete pivot.
Current Core linked slots have a 60-pixel inner tile. Unity UVs run bottom-up. Horizontal source
knots `0,3,25,58,60` map to `-17,-14,8,15,17`; vertical knots `0,3,14,47,57,60` map to
`-17,-14,-3,4,14,17`, all divided by 60. Thus the complete 34-pixel outlined body straddles
the shared edge equally. Only the 33-pixel top compresses to seven pixels. Front and both side
depths, longitudinal source phase, and contour thickness remain native.

Thin-only corners use complementary triangles at actual face transitions, with independent front
and side UVs. Their central top samples the actual native link mask, not a horizontal donor whose
front highlight would leave a line across a South-connected top. Mixed contacts now partition Thin
and regular regions by source ownership, so contrasting Stuff shares the same geometry without
requiring one material or painting either participant. Damage grades bind directly to installed Core
scratch materials; Thin Doors clip those sources to moving leaves and completed Thin Walls draw the
same source after their cached surface so it remains visible on the edge. These paths still require
the cumulative reviewed live gate; texture-free packaging alone does not close this work.

The next vertical tracer is a South thin stem entering the shared lower corner of two horizontal
regular-wall cells. Coordinates are relative to that shared corner, in source pixels / 60:

- Replace the regular front inside `x=-17..17, z=0..25`, plus the central front-rim region
  `x=-3..4, z=25..28`. Keep every native sample outside that aperture unchanged.
- The central `x=-3..4` top continues from the thin stem into the regular top using the native
  E/W/S link slot. The receiving front splits into its actual horizontal front and vertical side
  planes across two complementary internal perspective triangles. No exterior diagonal shoulder.
- Regular-front UV phase follows the receiving cell's unmodified `x mod 1`; the vertical side
  follows the stem's `z+0.5`. Do not stretch a whole T tile or reset brick phase at the aperture.
- Each receiving regular cell submits only its own aperture half. The contact owns the otherwise
  omitted last thin half-edge `z=-0.5..0`; that half-edge is partitioned by the same shared `x=0`
  boundary. No double coverage or ownership tied to designation direction.
- This tracer does not imply other mixed-width rotations/materials/contacts are accepted. Expand
  them one at a time with source/coverage tests and native-action render evidence.

## Simplified regular-contact and endpoint decision — 2026-09-05

The later one-regular-cell corner compositor is rejected. A Thin endpoint and a lone ordinary wall cell
at the same vertex now deliberately ignore one another for topology and rendering: Core retains the
entire native wall print and Thin retains its own terminal print. A mixed-width replacement is admitted
only when the endpoint meets the shared boundary of two adjacent regular-wall cells forming a continuous
receiving run, i.e. the exact four-rotation side-T already measured above. Screen overlap is not contact.

A terminal Thin half-ray is also bounded on its longitudinal axis by the grid vertex and edge midpoint.
For East/North its bounds are `[0, 0.5]`; for West/South they are `[-0.5, 0]`. The complete measured
`[-17/60, +17/60]` projection remains on the normal axis. This prevents a single horizontal ray from
occupying the adjacent left/right cell past its end (and the rotated equivalent) while retaining the
native terminal profile. Only actual Thin L/T/cross topology, or the admitted two-cell side-T replacement,
may occupy the adjoining junction area.

On 2026-09-04, the follow-up native tracer exercised South, North, East, and West side-T contacts in
Bricks, Planks, and Smooth, plus a contrasting Planks-to-Bricks junction. Each receiving wall and Thin
arm now samples its own installed Core atlas. The junction seam is the same complementary diagonal
surface partition used by the equivalent Thin-only union; it is no longer a rectangular arm printed
onto an intact regular-wall face. The retired `CoreDerivedWallMaterialCache`,
`HybridRegularRasterCompositor`, and their painted-composition tests were removed from the compiled
product after the source-only replacement tests passed. Multi-contact and final reviewed acceptance
remain open.

The context-free review of that intermediate replacement still read its diagonal receiver partitions as
wedges or inserted overlays. The accepted simplification therefore removes the mixed receiver mesh
entirely. In the exact two-receiver case, Core prints both regular cells unchanged and the Thin endpoint
uses only the matching native `HalfRay`: a capless straight half ending at the shared vertex with no
polygon clipping, altitude slope, or receiver aperture. Lone-wall contacts continue to use the ordinary
capped terminal and otherwise ignore Core.
