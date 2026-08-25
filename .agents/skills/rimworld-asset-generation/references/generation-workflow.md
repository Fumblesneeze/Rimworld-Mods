# Generation, masking, and prompt patterns

## Candidate discipline

Do not begin with generation or a RED package test. First inspect structurally and conceptually
similar Core and locally subscribed-mod art read-only, measure the fixed-camera/world/draw contract,
and write the exact prompt plus measurable acceptance criteria. Renderer code remains TDD-backed;
the visual asset loop is reference → measurement → prompt → candidates → comparison → live review.

Keep one brief constant across generators: object identity, RimWorld-like hand-painted map view,
camera, number of views, pure chroma background, no cast shadow outside the silhouette, and no text.
Varying the brief while varying the model makes the comparison meaningless. Generate multiple
candidates, retain rejected outputs only under ignored artifacts, and select in game at final zoom.

For local ComfyUI comparisons, use the installed `local-image-generation` skill and its headless
workflow rather than inventing API payloads. For built-in generation, use the `imagegen` skill.

The saved prompt must name the exact canvas, expected visible bounds, pixels/cell or draw size,
camera elevation/azimuth or fixed-camera landmark, every required part, material-tinted versus fixed
regions, exterior outline target measured from references, required junction/adjacency behavior, and
close/ordinary/far zoom acceptance. For a damageable object, also name each progressive damage
grade, its material response, and the exact silhouette/geometry to which it must remain attached.
Reject a visually attractive response that violates any of those measurements.

When the sprite paints apparent height, record and prompt each visible plane separately. At minimum,
measure the top surface, south/front face, east/side face, bevel/transition, and contour in source
pixels and final pixels per cell. Compare top:front and top:side ratios against both the structural
reference and any shorter/taller conceptual comparator. Never use one whole-silhouette aspect ratio
to stand in for those plane proportions.

Record mean-luminance samples for the same planes and state the required ordering and maximum ratio
in the prompt. Recheck it under every representative Stuff color: a top surface that is dimensionally
correct but value-identical to the face disappears at map zoom. The exterior contour must remain
darker than every modeled plane after tinting.

## Item prompt template

```text
RimWorld-style hand-painted game sprite of [one exact item/set], high three-quarter map view,
compact readable silhouette at 64 pixels, neutral gray [Stuff-colorable surfaces], fixed-color
[handles/dirt/food], no pawn, no text, no floor and no cast shadow, isolated on pure #00ff00 green.
Return one centered object with generous even padding.
```

Name every required part and forbidden ambiguity. For a cookware “set,” say whether lids, pan,
pot, and utensils must all remain visible; otherwise a generator may silently omit them.

## Four-cardinal workbench prompt template

```text
One 2x2 sprite sheet for the same [2x1 or 3x1] RimWorld production workbench. Fixed map camera for
all four cards: NORTH top-left, EAST top-right, SOUTH bottom-left, WEST bottom-right. Render every
card independently; never rotate a raster. Use the measured Core tabletop and underframe
proportions. The apron/legs remain at the bottom edge of every card. Rotate this exact equipment
identity in world space: [ordered list]. North and east preserve the canonical order; south and
west reverse it. No label, arrow, worker, floor, room, wall, or external shadow. Pure #00ff00.
```

After generation, audit all four cards for the same legs, shelves, controls, basins, racks, and
tools. “Same workstation” in a prompt is not sufficient; generators commonly omit equipment in one
vertical view or swap east/west.

## ImageMagick recipes

Always write intermediate/output files under ignored artifacts until a candidate is selected.
Quote paths and inspect every result.

Inspect dimensions, image type, bit depth, and channels:

```powershell
magick identify -format '%f|%wx%h|%z|%[type]|%[channels]\n' .\candidate.png
```

Use the skill's `Measure-RimWorldSprite.ps1` helper for nonzero-alpha bounds. Generic ImageMagick
`%@` is trim geometry against the image background color, not alpha-channel bounds, and is wrong for
uniform opaque images or files with hidden RGB.

Trim and place a subject without stretching its aspect ratio:

```powershell
magick .\keyed.png -trim +repage -resize '512x292>' .\trimmed.png
magick -size 640x384 xc:none .\trimmed.png -gravity northwest `
  -geometry +64+64 -composite -depth 8 -define png:color-type=6 .\normalized.png
```

Use exact `!` resizing only after the authored projection has already been measured and approved;
otherwise it hides bad perspective by deforming it.

Create a full-primary red Stuff mask by cloning the diffuse canvas and preserving its exact alpha:

```powershell
magick .\diffuse.png -channel RGB -fill '#ff0000' -colorize 100% +channel `
  -depth 8 -define png:color-type=6 .\diffuse_m.png
```

For selective masks, paint a binary selection image for the tintable surface, multiply it by the
diffuse alpha, then composite it onto an opaque black RGB canvas carrying the same alpha. Never use
a fuzzy material selection without visually checking handles and edge antialiasing.

Normalize invisible RGB to black:

```powershell
magick .\input.png -alpha on -channel RGBA -fx 'a==0?0:u' `
  -depth 8 -define png:color-type=6 .\normalized.png
```

Chroma removal must distinguish background-connected green from legitimate green food or lights.
A global “remove every green pixel” command corrupts vegetables. Flood-fill or connected-component
the background matte from the canvas edge, preserve enclosed green components, then run an
edge-only despill. Inspect on both white and charcoal backgrounds.

Build a labeled contact sheet:

```powershell
$inputs = Get-ChildItem .\candidates -Filter '*.png' | Sort-Object Name |
  ForEach-Object FullName
magick montage @inputs -thumbnail 256x256 -tile 4x -geometry 280x300+8+16 `
  -background '#20242a' -fill white -set label '%t' .\contact-dark.png
```

Repeat with a light background. A dark-only review misses dark halos; a light-only review misses
white fringes.

## Adding a Core-relative comic contour

1. Extract several comparable Core textures read-only and retain their exact source/image hashes in
   ignored evidence; never commit extracted Core pixels.
2. Measure the proxy at the actual final canvas, record the product source hash/topology, and define a
   uniquely identified horizontal or vertical minimum transparent run through every gap the candidate
   radius could close. A source-space exclusion must name that ID, intersect its final-scale run, and
   stay wholly inside the run's four-final-pixel neighborhood; unrelated or oversized exception
   rectangles fail closed.
3. Generate the two class-approved depths from the already approved art (fine `0.75/1`, broad
   `1/2`, building `2/3` final pixels). Use
   `Add-RimWorldSpriteOutline.ps1` so the candidates preserve source pixels and Stuff-mask semantics.
4. Compare both candidates beside the Core sample at map scale on light and dark backgrounds. Reject
   blobby handles, sealed openings, visible cast-shadow reads, and contours that dominate fine tools.
5. Promote only the selected candidate, update its output hash/ring fractions, then verify the real Def
   in RimWorld. The Pillow proxy is deterministic regression evidence, not a substitute for Unity's
   imported/mipped render.
