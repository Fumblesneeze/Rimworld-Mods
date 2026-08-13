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

1. Discover the runtime contract before drawing.
   - Identify the owning Def, `graphicClass`, shader, `texPath`, `drawSize`, footprint, rotations,
     Stuff colors, optional graphic owner, and every required filename.
   - For About/Workshop art, inspect the consumer's real dimension/file-size contract and locally
     installed comparators before composing; do not assume one Steam image size is mandated.
   - For Workshop-page cards, inspect successful contemporary mod pages as layout comparators. Use
     wide sections that remain legible around the page's displayed width; the proven Immersive Chefs
     template is 1164×655 and its Steam additional-preview upload must remain below 1 MiB.
   - Inspect active optional mods and finalized Defs when another mod can replace graphic ownership.
   - For a building family, measure several comparable Core textures rather than guessing from one.
     Extract Core art read-only into ignored evidence; never redistribute it.

2. Write a focused observable spec and RED test.
   - Pin required files, canvas dimensions, alpha/mask contract, and material or cardinal variants.
   - For directional buildings, test the fixed-camera body projection and a reviewed per-direction
     equipment order, not merely that four differently encoded PNGs exist.
   - Retain the expected failing test result and reject zero-test runs.

3. Generate comparable candidates.
   - Use the image-generation skills available in the session. When model comparison is requested,
     give every model the same product identity, camera, composition, output grid, and seed where
     supported; retain the raw responses under ignored `artifacts/VisualAssets/...`.
   - Generate at least two candidates for an important asset. Compare them at the final map scale,
     not just at source resolution.
   - For cardinal workbenches, request four separately rendered cards from one fixed camera. Never
     manufacture north/south or east/west by raster rotation.

4. Normalize without changing the contract.
   - Crop each candidate deliberately, remove only background-connected chroma, preserve legitimate
     enclosed greens, remove detached fragments, despill silhouette edges, and normalize transparent
     RGB to black.
   - Place the trimmed subject into the exact canvas and measured body rectangle. Do not stretch a
     whole sheet or use canvas aspect alone as a proxy for correct projection.
   - Create `_m` masks only for a `CutoutComplex` path that consumes them. Match diffuse dimensions
     and alpha exactly.

5. Review and package.
   - Create a contact sheet on both light and dark backgrounds and a map-scale comparison beside the
     relevant Core asset.
   - Review product identity, silhouette, equipment conservation, material response, alpha fringe,
     cardinal world-space order, and apparent tabletop/underframe proportions.
   - Copy only selected final PNGs into the mod. Keep raw generations, rejected candidates,
     extracted Core textures, and transient processing files ignored.
   - For Workshop feature cards, keep a mod-owned ordered manifest with token, alt text, source
     sprites and copy; keep the reusable frame and licensed font under `release/templates/workshop/`.
     Render deterministically, inspect a page-scale contact sheet and important full-size cards, and
     reject fallback fonts, cropped headlines, art/text collisions, or unreadable small copy.
   - Map every card's pictured subject to its actual headline and bullet claims before promotion.
     Reject a visually attractive but generic sprite collage when it does not depict the promised
     behavior. If a directional building appears in Workshop art, use only a cardinal frame whose
     rotated interaction-facing side has been inspected in game; never infer direction from a
     filename or assume that `South` means a front elevation toward the camera.

6. Verify the reviewed build in game.
   - Build/package after review changes.
   - Use a native player workflow: place or spawn through the real designator/job/gizmo path, select
     the object, rotate/place every claimed direction, and compare it at the same zoom with a
     same-facing Core reference.
   - Personally inspect exact-process before/action/after screenshots. For Stuff art, observe at
     least the materially distinct colors that exercise masked and fixed regions.
   - Frame related objects with the camera, then select only the one whose native inspect pane must
     be visible; multi-selection replaces the normal Thing inspector.
   - Record the build hash, exact mod list, screenshots, clean log, config restoration, and cleanup.
     Do not check the OpenSpec task if that causal visible evidence is missing.

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
