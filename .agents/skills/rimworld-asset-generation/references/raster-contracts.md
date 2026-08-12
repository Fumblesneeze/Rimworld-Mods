# RimWorld raster contracts

## Files and paths

- Package lossless 8-bit RGBA PNGs under `Textures/<texPath>.png`.
- `texPath` omits `Textures/` and the extension. Treat path and filename case as significant even on
  Windows because Workshop consumers may use case-sensitive filesystems.
- `Graphic_Single` consumes one diffuse. `Graphic_Multi` normally consumes exact `_north`, `_east`,
  `_south`, and `_west` siblings. Do not assume a missing direction will fail visibly during build.
- `Cutout` does not consume a two-color mask. `CutoutComplex` expects an exact sibling `_m.png` for
  each selected diffuse/cardinal path that needs masked coloring.
- Diffuse and mask must have identical width, height, and alpha at every pixel. Keep transparent
  pixels transparent and normalize their RGB to black to prevent colored fringes during filtering.
- Mipmapped downscaling is the real legibility target. Inspect item art around 64 px and building art
  at ordinary map zoom.
- Core-style Thing readability includes a deliberate near-black exterior contour measured at final
  map density. Derive it from several same-scale Core comparators; do not guess a source-canvas brush
  width or darken all internal edges indiscriminately.
- Treat only 4-connected background flood-filled from the canvas edge as exterior. Enclosed plate
  centers, cabinet openings, and holes are not contour targets. Record minimum transparent runs
  across narrow handles, tines, and equipment separations before processing so a thick candidate
  fails instead of silently closing them.
- Preserve every original nontransparent pixel exactly. A `CutoutComplex` mask must gain the same
  alpha as the diffuse only at new contour coordinates, and those new mask pixels must be black so
  Stuff tint does not recolor the line.

## About and Workshop previews

- `About/Preview.png` is the image RimWorld renders on its native Mods screen. Measure locally
  installed packages before choosing a house size; this repository uses 640×360 because it is the
  common local 16:9 shape, not because RimWorld declares one universal mandatory dimension.
- Steam Workshop preview upload has a file-size ceiling; it does not mandate this repository's
  1280×720 authoring master. Keep the reviewed upload below the current proven limit and derive the
  About image deterministically from the same owned source.
- Render banners and speech-bubble text in a deterministic build script, not by repeatedly editing
  generated PNG pixels. Center text against the measured visible bubble/banner bounds at every output
  size and test cropping/file size.
- Verify the final About image in RimWorld's native Mods screen with the correct package ID, author,
  dependency order, and metadata visible. A static image preview does not prove the game crops or
  scales it acceptably.

## Stuff masks

RimWorld's conventional `CutoutComplex` mask uses opaque red for the primary color/Stuff-tinted
region, opaque green for the secondary color region, and opaque black for fixed-color diffuse
details. Confirm the concrete Graphic implementation when a mod owns the shader or supplies custom
color semantics.

- Mask only surfaces that should change material color. Handles, dirt, labels, food, status lamps,
  and shadows usually remain black/fixed.
- A completely Stuff-colored object may be better served by ordinary `Cutout` and the Graphic's
  overall color than a full-red mask.
- A dirty variant must keep the same silhouette and mask as its clean sibling unless the runtime
  selector deliberately changes geometry.

## Canvas, draw size, and footprint

`drawSize` controls the world mesh; the PNG canvas controls sampling inside it. Match their aspect
ratios exactly unless an inspected upstream Graphic intentionally crops or offsets the image.
Footprint is not draw size: Core commonly gives a 3x1 workbench a `(3.5,1.5)` draw size so its art
has padding and an apron.

For the measured RimWorld 1.6.4871 Core production-bench baseline in this repository:

| Footprint | Def draw size | Horizontal canvas | Horizontal tabletop | Vertical canvas | Vertical tabletop | Screen-bottom underframe |
|---|---|---|---|---|---|---|
| 2x1 | 2.5x1.5 | 640x384 | 64,64,512,256 | 384x640 | 64,64,256,512 | 36 px |
| 3x1 | 3.5x1.5 | 896x384 | 64,64,768,256 | 384x896 | 64,64,256,768 | 36 px |

These are repository examples derived at 256 px/cell. Re-measure if the game version, source family,
footprint, or intended draw density differs.

## Fixed-camera cardinal buildings

RimWorld rotates the object in map space; it does not rotate the player's camera. Therefore:

- the visible apron, legs, or underframe stays on the **screen-bottom** edge in north, east, south,
  and west PNGs;
- the tabletop changes from horizontal to vertical, but its apparent depth and the underframe depth
  remain consistent;
- equipment rotates in world space. If north's canonical long-axis order is `A|B|C`, east keeps
  `A|B|C` from screen top to bottom, while south and west show `C|B|A`;
- `Rot4.South` can place the interaction cell north of the footprint. That does not authorize an
  impossible camera from the north or an apron at image top;
- opposite frames may redraw worker-facing equipment and occlusion, but must share one body
  construction. A 180-degree pixel rotation is wrong.

Measure several clean Core benches in `resources.assets`, including a bare or nearly bare table,
before defining a new family. Record source game version/hash, exact canvases, alpha bounds,
tabletop, underframe, and pixels/cell in a durable text/XML manifest. Keep extracted Core PNGs in
ignored evidence and never ship them.
