---
name: rimworld-asset-generation
description: Generate, edit, mask, normalize, package, and verify RimWorld 1.6 raster assets. Use for Thing/building sprites, About or Workshop previews, Graphic_Single/Graphic_Multi cardinal families, Stuff masks, material/sanitation variants, fixed-camera workbench perspective, Core measurement, generator comparisons, approval manifests, and live visual acceptance.
---

# RimWorld Asset Generation

Announce this skill before it changes or accepts art. Also follow the repository's
`rimworld-mod-development` skill and acceptance gate; static PNG review and passing tests do not
replace observing the reviewed package in a fresh RimWorld process.

## Route the task

Read only the references needed for the asset:

- Read [references/raster-contracts.md](references/raster-contracts.md) before authoring or editing
  any packaged texture, mask, XML graphic declaration, or directional family.
- Read [references/generation-workflow.md](references/generation-workflow.md) for model comparison,
  prompt patterns, chroma removal, resizing, masks, variants, and example ImageMagick commands.
- Read [references/verification.md](references/verification.md) before selecting a candidate,
  changing an approval manifest, or claiming in-game completion.

## Workflow

Asset development uses a **reference-first visual loop**, not the code TDD order. Static/package
tests still protect measured contracts, and renderer code changes still use TDD, but do not write or
generate the art before completing the reference and measurement brief below.

1. Research structural and conceptual references before drawing.
   - Inspect multiple comparable RimWorld Core assets and relevant locally subscribed-mod assets
     read-only. Choose references that match both the subject's structure and its gameplay concept;
     a convenient canvas shape alone is not a valid comparator.
   - Retain extracted reference pixels only under ignored evidence. Record source package/version,
     paths or bundle identities, hashes, and why each comparator is relevant. Never redistribute
     Core or Workshop pixels in the product.
   - View references in game when texture files alone do not reveal Unity filtering, shader,
     adjacency, altitude, or fixed-camera behavior.

2. Measure the runtime and visual contract.
   - Identify the owning Def, `graphicClass`, shader, `texPath`, `drawSize`, footprint, rotations,
     Stuff colors, optional graphic owner, and every required filename.
   - Measure source canvas, alpha bounds, pixels per cell, visible world width/depth, projection or
     fixed-camera angle, landmark coordinates, exterior-outline thickness/darkness at final map
     scale, the zoom levels where the object must remain distinguishable, and any progressive
     damage-overlay placement the object requires.
   - For every object that conveys height through painted projection, measure the top plane,
     south/front face, east/side face, bevel/transition, and contour as separate pixel and world-space
     components for every materially different orientation. Never infer height from only the total
     alpha bounds or a whole-object aspect ratio.
   - For low countertop appliances, measure and pin both a minimum and maximum casing-to-visible-top
     depth ratio from a structurally equivalent in-game comparator. "Low" does not mean that the
     casing may collapse into a decorative bevel: reject both the tall cube and the tray-flat
     overcorrection, and compare the result at final map scale before promotion.
   - When adapting a low object into a full-height one, inspect and measure both the low material
     reference and a real full-height structural comparator in game. Do not extrapolate height with a
     single scale factor. Treat transparent margins and the draw plane as part of the projection:
     replacing a padded full-cell sprite with a tight crop on a shorter mesh is a geometry change.
   - For About/Workshop art, inspect the consumer's real dimension/file-size contract and locally
     installed comparators before composing; do not assume one Steam image size is mandated.
   - For Workshop-page cards, inspect successful contemporary mod pages as layout comparators. Use
     wide sections that remain legible around the page's displayed width; the proven Immersive Chefs
     template is 1164×655 and its Steam additional-preview upload must remain below 1 MiB.
   - Inspect active optional mods and finalized Defs when another mod can replace graphic ownership.
   - For a building family, measure several comparable Core textures rather than guessing from one.
     Extract Core art read-only into ignored evidence; never redistribute it.

3. Write the exact generation brief and acceptance criteria before generation.
   - Specify the exact subject and part inventory, canvas and visible bounds, world footprint and
     draw size, fixed-camera projection/angles, material regions, outline behavior, cardinal or
     junction landmarks, permitted padding, required readability at each target zoom, and damage
     grades and attachment geometry where applicable.
   - For projected/extruded art, state the measured top:front and top:side pixel budgets explicitly
     and reject any candidate whose visible wall/machine height is compressed into a decorative bevel.
   - Specify the value hierarchy between top, front, and side planes as measurable luminance ratios.
     A geometrically present top plane that merges with the face at ordinary or far zoom is not a
     readable plane. Verify the hierarchy again after representative Stuff tinting, with the
     exterior contour retained as the darkest separate value group.
   - State what must be absent: text, floor, cast shadow, arbitrary perspective, detached fragments,
     matte fringe, invented parts, and reference pixels.
   - Pin required files, alpha/mask contract, material or cardinal variants, and measurable pass/fail
     thresholds. Save the actual prompt beside the ignored candidates so selection is reproducible.
   - After the visual brief exists, write the focused static/package RED test for those measured
     contracts. Renderer code follows the repository's normal TDD workflow.
   - For directional buildings, test the fixed-camera body projection and a reviewed per-direction
     equipment order, not merely that four differently encoded PNGs exist.
   - Retain the expected failing test result and reject zero-test runs.

4. Generate comparable candidates.
   - Use the image-generation skills available in the session. When model comparison is requested,
     give every model the same product identity, camera, composition, output grid, and seed where
     supported; retain the raw responses under ignored `artifacts/VisualAssets/...`.
   - Generate at least two candidates for an important asset. Compare them at the final map scale,
     not just at source resolution.
   - For cardinal workbenches, request four separately rendered cards from one fixed camera. Never
     manufacture north/south or east/west by raster rotation.

5. Normalize without changing the contract.
   - Crop each candidate deliberately, remove only background-connected chroma, preserve legitimate
     enclosed greens, remove detached fragments, despill silhouette edges, and normalize transparent
     RGB to black.
   - Place the trimmed subject into the exact canvas and measured body rectangle. Do not stretch a
     whole sheet or use canvas aspect alone as a proxy for correct projection.
   - Create `_m` masks only for a `CutoutComplex` path that consumes them. Match diffuse dimensions
     and alpha exactly.
   - Treat a Stuff diffuse as neutral illumination, not as the finished material color. RimWorld
     multiplies red/green mask regions by the Stuff color; a mid-gray metal diffuse multiplied by
     Core Steel's already-gray color becomes nearly black. Measure masked-pixel luminance, brighten
     only the material-bearing region, and simulate at least Core Steel before promotion. Keep
     fixed-black handles, grime, accents, alpha and outline pixels unchanged.

6. Compare references and candidates side by side, then package only the selection.
   - Create source-scale and final-map-scale contact sheets on both light and dark backgrounds with
     the relevant Core/subscribed references beside every candidate. Preserve labels outside the
     image cells so the comparison does not alter the art.
   - Review product identity, silhouette, equipment conservation, material response, alpha fringe,
     cardinal world-space order, and apparent tabletop/underframe proportions.
   - Copy only selected final PNGs into the mod. Keep raw generations, rejected candidates,
     extracted Core textures, and transient processing files ignored.
   - For Workshop feature cards, keep a mod-owned ordered manifest with token, alt text, source
     sprites and copy; keep the reusable frame and licensed font under `release/templates/workshop/`.
     Render deterministically, inspect a page-scale contact sheet and important full-size cards, and
     reject fallback fonts, cropped headlines, art/text collisions, or unreadable small copy.
     Do not assume Steam will preserve PNG transparency: inspect the uploaded page. If alpha corners
     are matted white, composite the full canvas onto the measured Steam Workshop page background
     (currently `#1b2838` for the reviewed Immersive Chefs cards) before drawing rounded panels, and
     pin the exact opaque corner color in the presentation gate.
   - Map every card's pictured subject to its actual headline and bullet claims before promotion.
     Reject a visually attractive but generic sprite collage when it does not depict the promised
     behavior. If a directional building appears in Workshop art, use only a cardinal frame whose
     rotated interaction-facing side has been inspected in game; never infer direction from a
     filename or assume that `South` means a front elevation toward the camera.
   - For in-game Workshop showcases, author a per-mod scene declaration before capture. Dress it as
     a plausible colony with finished rooms, appropriate materials, lighting, decor, storage,
     plumbing/power, linked benches, named pawns and short natural paths. Do not promote sterile
     cleared-map fixtures, test labels, selection brackets, learning helpers or debug UI.
   - Capture one high-detail still and, when motion matters, a raw-frame GIF candidate. Steam GIFs
     must remain below 1 MiB and at most five seconds; use fixed views and hard cuts between beats,
     never slow pans. Inspect the compressed GIF at page scale. If the limit destroys readability,
     keep the screenshot as the additional preview and retain the GIF only as a rejected candidate.

7. Verify the reviewed build in game at the actual viewing conditions.
   - Build/package after review changes.
   - Use a native player workflow: place or spawn through the real designator/job/gizmo path, select
     the object, rotate/place every claimed direction, and compare it at the same zoom with a
     same-facing Core reference.
   - Personally inspect exact-process before/action/after screenshots at close, ordinary, and far
     useful map zoom. The same object must remain identifiable without relying on selection brackets,
     debug labels, or a publishing illustration. For Stuff art, observe at least the materially
     distinct colors that exercise masked and fixed regions. For damageable art, capture undamaged,
     moderate, and severe damage and confirm every mark stays on the rendered object at straight,
     doubled, and junction geometry.
   - Frame related objects with the camera, then select only the one whose native inspect pane must
     be visible; multi-selection replaces the normal Thing inspector.
   - Record the build hash, exact mod list, zoom/camera state, screenshots, clean log, config
     restoration, and cleanup. Do not check the OpenSpec task if that causal visible evidence is
     missing.

8. Require a blind independent visual-identity review.
   - Give an independent reviewer sub-agent only the fresh in-game screenshots from different zooms
     and materials. Do not provide the mod name, intended identity, topology labels, implementation
     explanation, or your own verdict.
   - Ask it to explain what object and gameplay purpose it sees, whether the image is coherent from
     RimWorld's fixed camera, whether outlines/perspective/material/style resemble the surrounding
     game, and what breaks at each zoom.
   - Accept only when the reviewer independently identifies the intended object/purpose and reports
     no perspective, incoherent-style, outline, material, connection, or zoom-legibility defect.
     Treat ambiguity or a guessed identity as a failed review, revise the asset, and repeat the live
     capture and blind review.

## Measurement helper

Run the bundled read-only helper from the repository root:

```powershell
& .\.agents\skills\rimworld-asset-generation\scripts\Measure-RimWorldSprite.ps1 `
  -Path .\mods\Example\Textures\Example\Things\Building\Bench_north.png `
  -Output table
```

Use `-Output json` for an approval/test pipeline. The helper reports the exact canvas, alpha bounds,
visible coverage, channels, and SHA-256; it does not decide whether the perspective is visually
correct.

## Core-style outline processor

Use the bundled processor only after the asset has a reviewed entry in the mod's outline approval
manifest. It requires Python 3 and exactly Pillow 12.2.0 so its final-scale proxy matches the
recorded Core baseline. The command never overwrites inputs or occupied outputs:

```powershell
& .\.agents\skills\rimworld-asset-generation\scripts\Add-RimWorldSpriteOutline.ps1 `
  -InputPath .\mods\Example\Textures\Example\Things\Plate.png `
  -OutputPath .\artifacts\VisualAssets\Plate.stroke4.png `
  -StrokePixels 4 `
  -Baseline .\docs\SpriteOutlineBaseline.xml `
  -TopologyManifest .\docs\SpriteOutlineApprovals.xml `
  -AssetId Plate `
  -MaskInputPath .\mods\Example\Textures\Example\Things\Plate_m.png `
  -MaskOutputPath .\artifacts\VisualAssets\Plate.stroke4_m.png `
  -Output json
```

The approval entry names its `class`, pre-outline SHA-256, final canvas, component/hole counts, and
each at-risk horizontal or vertical `protectedGap` cross-section. Class stroke limits, ring-darkness
thresholds, and minimum edge clearance come from the baseline and cannot be weakened per asset.
The processor preserves every original nontransparent RGBA pixel, adds only to originally alpha-zero
canvas-edge-connected background, leaves enclosed holes alone, and mirrors new diffuse alpha into a
fixed-black Stuff-mask contour. It rejects a candidate before publication when topology, gap width,
ring darkness, or edge clearance fails. Pillow is a deterministic package proxy; the reviewed live
RimWorld render remains authoritative.
