# Immersive Chefs visual assets

Selected sprites are tracked in the mod package. Generated candidates, chroma-key sources,
comparison montages, and rejected variants are retained under ignored `artifacts/VisualAssets`
during development.

## Cookware set

Selected: candidate A, generated 2026-08-06. It keeps the pot, pan, two lids, handles, and
spatula readable at 64 px; its taller circular silhouette also remains distinct from plates.
Candidate B was rejected because its low bundled silhouette became indistinct at item scale.

Both candidates requested original RimWorld-like, hand-painted, high three-quarter item art on a
pure green chroma background. Candidate A requested a nested deep pot, shallow pan, two loose lids,
and small spatula with neutral gray stuff-colorable surfaces and fixed brown handles. Candidate B
requested the same abstract set in a low, wide, mildly worn bundle. The selected image was keyed,
despilled, trimmed, and reduced to 256 px; transparent pixels were normalized to black to avoid
chroma leakage in renderers. `Cookware_m.png` marks the neutral cookware surfaces red for RimWorld's
primary Stuff tint and leaves the brown handles unmasked.

## Plate family

Selected: candidate B, generated 2026-08-06. Its broad, subtly faceted rim remains recognizable at
64 px and gives wood, stone, and metal tints useful silhouette character. Candidate A was rejected
because its smoother oval read as a generic dish at final scale. Both prompts requested a neutral
gray, empty, shallow plate in high top-down view on pure green chroma, without food or cutlery. The
selected 256 px diffuse has zero green-dominant fringe after matte cleanup. `Plate_m.png` masks the
complete plate for primary Stuff tinting; the fixed adobe Def reuses the owned silhouette with its
earth color.

## Cutlery setting

Selected: candidate B, generated 2026-08-06. The crossed fork, spoon, and blunt table knife remain
one compact, recognizable setting at 64 px. Candidate A was rejected because its parallel layout
read as three unrelated loose Things at final scale. Both prompts requested exactly those three
utensils in neutral gray on pure green chroma. The selected 256 px diffuse has zero green-dominant
fringe, and `Cutlery_m.png` masks the complete setting for wood, metal, silver, gold, and plastic
Stuff tinting.

## Chef's knife set

Selected: candidate B, generated 2026-08-06. Its closed leather roll reads as one belt-ready kit at
64 px while preserving three distinct culinary blade sizes. Candidate A was rejected because its
open roll reads more like a work-surface display than worn personal equipment. Both prompts asked
for a three-knife professional kitchen set on pure green chroma. The selected 256 px diffuse has
zero green-dominant fringe. `ChefsKnife_m.png` uses red only for the neutral blade and rivet regions;
the dark leather roll and wooden handles remain fixed black-mask accents.

## Glitterworld cookware set

Selected: candidate B, generated 2026-08-06. Its nested pearl-alloy pot, smart lids, clipped utensil,
graphite grips, and cyan seams remain one compact advanced kit at 64 px. Candidate A was rejected
because its separated pot, pan, lids, and spoon read as four loose map objects. Both prompts asked
for fixed-color self-cleaning cookware on pure green chroma. The selected 256 px sprite uses the
ordinary `Cutout` shader without a Stuff mask or global Def tint, preserving the authored alloy and
status-light colors.

## Prepared ingredients

Selected: candidate B, generated 2026-08-06. Its compartment tray and partly folded lid read as
stored mise en place while the orange, green, pale, and red portions remain visibly varied at 64 px.
Candidate A was rejected because its parchment layout risked reading as a finished platter. Both
prompts requested clearly raw, chopped mixed ingredients on pure green chroma. The selected 256 px
fixed-color sprite uses `Cutout`; edge-only despill removes chroma without muting the legitimate
opaque green vegetables.

## Domestic dishwasher

Selected: candidate B, generated 2026-08-06. Its steep map view, twin top-loading racks, visible
plates, stainless shell, and near-edge controls remain legible across a 2×1 footprint. Candidate A
was rejected because its frontal view would read like a UI icon pasted onto the map and rotate
poorly. Both prompts requested a domestic powered dishwasher on pure green chroma. The selected
512×256 fixed-color sprite uses `Graphic_Single`/`Cutout`; RimWorld rotates the one authored map-view
sprite with the building rather than resolving a nonexistent directional collection.

## Industrial dishwasher

Selected: candidate A, generated 2026-08-06. Its enclosed central wash chamber, paired plate-rack
lanes, top vents, and symmetric controls remain distinct at a 3×1 map scale and visually relate to
the domestic machine. Candidate B was rejected because its flatter front view would rotate poorly.
Both prompts requested high-throughput commercial equipment on pure green chroma. The selected
768×256 fixed-color sprite uses `Graphic_Single`/`Cutout`. Baked steam was removed so idle,
unpowered, and broken machines remain visually truthful; the visible machine was reframed to fill
720 of 768 horizontal pixels and match its 3-cell collision footprint.

## Ingredient prep station

Selected: candidate B, generated 2026-08-06. Its empty rinse basin, broad cutting board, fixed knife
rail, covered bins, and empty portioning pans communicate washing, chopping, and portioning without
claiming ingredients are permanently stored. Candidate A was rejected because its prefilled bins
would falsely depict food in every state. Both prompts requested a professional 3-cell worktable on
pure green chroma. The selected 768×384 fixed-color sprite is reframed to the Def's 3.5×1.5 draw
ratio and uses `Graphic_Single`/`Cutout` for native rotation.
