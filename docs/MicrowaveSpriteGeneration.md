# Countertop microwave sprite generation

Owning mod: `fumblesneeze.immersivechefs`

This is the retained source contract for the fallback microwave's two cosmetic `Graphic_Multi` families. It does not apply when `Mlie.DThermodynamicsHotMeals` removes the fallback Def.

## Runtime contract and measured references

- Runtime Def: `ImmersiveChefs_Microwave`, one cell, `drawSize=(0.82,0.82)`, `BuildingOnTop`, and `interactionCellOffset=(0,0,-1)`.
- Package canvas: 512x512 RGBA for every cardinal frame, with at least 24 fully transparent pixels at every edge after the contour.
- Read-only runtime comparators:
  - `Vanilla Furniture Expanded - Props and Decor`: `Microwave_south.png`, `Microwave_north.png`, and `Microwave_east.png` on 188x188 canvases. Their nontrivial-alpha bounds are approximately 138x152, 139x145, and 139x153.
  - `[D] Thermodynamics - Hot Meals`: `DMicrowave_south.png`, `DMicrowave_north.png`, and `DMicrowave_east.png`, using the same structural convention.
- Both comparator families establish RimWorld's actual axis-aligned `Graphic_Multi` convention: South is the full door/front, North is the rear, East is a vertical side profile with the narrow front terminal at screen-right, and West is a vertical side profile with the narrow front terminal at screen-left. East and West are not diagonal or isometric diamonds.
- The front view needs a normal microwave cavity and separate controls. A thin letterbox door and shallow deck silhouette read as a VHS/VCR and are rejected.

The initial square/front-elevation family, the subsequent diagonal three-quarter family, and the ultra-thin low-body refinement are rejected intermediates. None is a reusable prompt or source asset.

## Exact successful initial-generation prompt

The following prompt was submitted once per identity with the three measured read-only comparator images supplied as references:

```text
Generate a NEW original 2x2 production sprite sheet for a RimWorld countertop microwave. Use the three referenced read-only microwave sprites ONLY to match their fixed map-camera projection and cardinal axes; do not copy their pixels, exact shapes, or palette. The output has four equal square cards, no labels, gutters, dividers, frames, text, arrows, or shadows. Background is one flat chroma green color. Exactly one microwave per card; no counter, table, cabinet, floor, wall, cord, food, person, or detached object.

CRITICAL RIMWORLD CAMERA CONTRACT: this is NOT isometric, NOT a 3/4 product view, and NOT a diagonal diamond. Long silhouette edges remain aligned to the screen axes, just like the references. North and South are HORIZONTAL front/rear views. East and West are VERTICAL side views rotated 90 degrees in map space. Small corner bevel cuts are allowed; long body edges must never run diagonally across a card. All four cards show the same medium-height countertop appliance under one fixed near-top-down orthographic map camera and one light from screen upper-left.

CARD ORDER AND FACES: upper-left NORTH: horizontal body, blank rear toward the viewer, two broad rear ventilation groups, no door and no controls. Upper-right EAST: vertical body, broad top running screen top-to-bottom, side casing toward the viewer, only the thin front/door edge visible on the SCREEN-RIGHT end. Lower-left SOUTH: horizontal body, the full recognizable front toward the viewer, one large dark glass cooking-cavity door on the left and a separate control strip on the right. Lower-right WEST: vertical body, broad top running screen top-to-bottom, side casing toward the viewer, only the thin front/door edge visible on the SCREEN-LEFT end. East and West are independently drawn, not mirrored raster edits.

RECOGNIZABLE MICROWAVE PROPORTIONS: It must read immediately as a microwave, never as a VHS/VCR, media player, drawer, or flat electronic deck. In the South/front view the whole body is about 390-420 px wide and 285-325 px deep/high within its card. The projected top occupies about 150-175 px of screen depth. The front casing is a substantial 105-125 px band, about 38-45 percent of the visible body depth, not an ultra-thin strip. The dark door cavity is about 235-270 px wide by 100-120 px high, with a width-to-height ratio between 2.0 and 2.5, rounded corners, a thick visible frame, and a small warm interior reflection. The control strip is 65-80 px wide with two large readable controls and one small indicator. Add two broad top ventilation groups and short feet. East/West side cards are about 285-325 px wide and 390-420 px tall, with a clearly vertical footprint and a plain side panel; the narrow front edge and controls occur only at the specified left/right end.

Use restrained hand-painted RimWorld-like sprite art with broad low-frequency value groups, slightly imperfect painted edges, sparse highlights, a continuous dark exterior contour that is visually 10-14 px at source-card scale, and internal lines 5-8 px. No photorealism, glossy render, logo, lettering, tiny buttons, excessive texture, or concept-art background. Keep at least 35 px clear around every object. Before finishing, verify: South unmistakably looks like a microwave front; North is its rear; East/West are axis-aligned vertical side views; there are no diagonal 3/4 silhouettes; and no door is a long VHS slot.
```

Candidate A appended this exact identity:

```text
Candidate A identity: warm light-grey powder-coated metal, charcoal door glass, black gasket, two large round dark dials and one amber indicator, muted grey vents, modest rounded 1970s appliance corners.
```

Candidate B appended this exact identity:

```text
Candidate B identity: warm ivory enamel, charcoal door glass, black gasket, two large square dark buttons and one amber indicator, muted grey vents, slightly squarer industrial appliance corners.
```

The initial outputs established the correct cardinal axes and recognizable microwave identity. They were not promoted because their top and casing planes were still too flat.

## Exact successful top-and-casing refinement prompt

The following prompt was submitted to each corresponding initial sheet:

```text
Edit the referenced 2x2 microwave sheet as a production RimWorld Graphic_Multi family. Preserve its correct four-card order, palette, controls, flat green background, and AXIS-ALIGNED cardinal silhouettes: upper-left North horizontal, upper-right East vertical, lower-left South horizontal, lower-right West vertical. Do not introduce any diagonal diamond, 3/4 product view, isometric rotation, label, divider, shadow, counter, floor, wall, food, person, cord, or extra object.

The current sheet is still too flat and elevation-like. REDRAW every card to show the fixed near-top-down RimWorld camera as two clearly different visible planes: (1) a broad vented TOP SURFACE and (2) a substantial near vertical casing face. The top surface is not the rear wall and the door is not painted on the top.

Treat the output as a 1254x1254 sheet with four conceptual 627x627 cards. For NORTH and SOUTH, keep the long body horizontal. Place the far/top body edge around local y=120-145, the top-to-casing seam around local y=325-345, the casing bottom around local y=455-475, and feet no lower than y=495. The top surface is therefore a broad 180-215 px-deep trapezoid; its far edge is slightly shorter than its near seam, but both long edges remain horizontal. The vertical rear/front casing is 110-130 px high. In SOUTH, put the microwave door only inside that vertical front band: a charcoal rounded cavity about 260-290 px wide by 90-110 px high, ratio 2.4-2.8, with a thick gasket and warm interior glint; put the large controls in a separate 75-90 px strip. This must look like a microwave oven, not a VHS/VCR slot or a wall oven. In NORTH, use the identical top/casing projection but show a blank rear casing with two broad rear vent groups and no door or controls.

For EAST and WEST, keep the body footprint vertical on screen, never diagonal. Show one elongated top trapezoid running screen top-to-bottom, approximately 300-330 px wide by 300-340 px deep, with two broad top vents. Its near side-casing seam is approximately horizontal around local y=390-415 and its near side face ends around y=490-510. The physical front is only the narrow terminal edge: SCREEN-RIGHT in East and SCREEN-LEFT in West. Show a recognizable dark door edge plus the two controls on that narrow terminal edge, without turning the whole card into a straight side elevation. East and West are independently painted but share one projection.

Keep the appliance medium-height and chunky enough to read as a microwave at 64 px. Use broad painted value groups, a strong dark outer contour, 5-8 px internal lines, readable vents and controls, modest bevels, and transparent-safe separation from the green. Before finalizing, verify there is a visible top/casing seam in every card, the South cavity is microwave-shaped rather than letterbox-thin, North/South are horizontal, East/West are vertical, and no long silhouette edge is diagonal.
```

Candidate A appended:

```text
Preserve Candidate A's warm light-grey metal, dark round dials, amber indicator, muted vents, and rounded 1970s casing identity.
```

Candidate B appended:

```text
Preserve Candidate B's warm ivory enamel, large square buttons, amber indicator, muted vents, and slightly squarer industrial casing identity.
```

The selected refined sheets are:

- Candidate A / base family: `exec-74753597-4b0e-4496-83ef-d662a430ee00.png`.
- Candidate B / `_Variant01` family: `exec-4d1ff76f-27c0-4162-a92b-90d386db6d50.png`.

## Deterministic normalization

The generator emitted 1254x1254 sheets and allowed some silhouettes to cross conceptual quadrant boundaries. Equal quadrant crops would clip or contaminate the card, so processing uses recorded non-overlapping source windows around each independently generated object, removes the measured green border with a soft matte and spill cleanup, and trims each object to its alpha bounds.

One family-wide scale is applied before centering each view on a transparent 512x512 canvas. The largest pre-contour dimension is capped at 400 pixels. A one-source-pixel edge cleanup precedes the approved 24-source-pixel `#171412` building contour, which becomes three readable pixels at the 64x64 proxy. No raster rotation or mirroring creates a missing view.

Final exterior alpha bounds are:

- base: North `29,86,454,340`; East `89,62,334,387`; South `31,87,450,338`; West `95,60,322,392`;
- variant: North `31,64,450,384`; East `86,32,339,448`; South `29,55,454,402`; West `83,33,346,445`.

Exact per-frame crop hashes, packaged hashes, alpha bounds, top/side/casing/feature probes, projection-pair identities, and all three proxy contour rings are retained in `DirectionalSpriteApprovals.xml` and `SpriteOutlineApprovals.xml`. The source/map-scale comparison is retained at `artifacts/VisualAssets/MicrowavePerspective-20260823/generated/selected-axis-source.png` and `selected-axis-64.png`.

## Rejection criteria

Reject the family if South is not the full door/front; North is not a blank rear apart from vents; East does not terminate at the front on screen-right; West does not terminate at the front on screen-left; any long silhouette edge becomes a diagonal isometric diamond; the top and casing are not visibly separate planes; the South cavity ratio leaves the microwave looking like a VHS/VCR; the outline or internal edge stair-steps at map scale; chroma remains; the support surface is swallowed at `drawSize 0.82`; or the door, controls, vents, and cardinal orientation disappear at the 64x64 proxy.
