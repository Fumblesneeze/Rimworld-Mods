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
complete plate for the optional VTEX material-family selector; the fixed adobe Def reuses the owned
silhouette with its earth color. In the base package the plate uses `Cutout`, so its whole visible
surface receives RimWorld's Stuff color instead of being muted by a white secondary channel. The
exact VTEX gate restores `CutoutComplex`, where the packaged mask and selected family sprite own the
material treatment.

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

## Portable material and sanitation variations

The optional VTEX/VEF path owns eleven additional masked sprites while the default path keeps the
four ordinary base graphics. Wood plate candidate A was selected over the darker candidate B
because its broad grain and carved rim remain distinct after Stuff tinting at 64 px. Light granite
plate candidate A was selected over the charcoal slate candidate B because it reads as stone rather
than dark metal after tinting. Faceted stone cookware candidate A was selected over the smoother
candidate B because the low-tech material remains unambiguous; its fixed wooden handles stay in the
mask's black region. Dark walnut cutlery candidate B was selected over the chunkier candidate A for
its clearer three-utensil silhouette.

Two generated grime overlays were compared. Candidate A was retained because its rings, smears, and
crumbs remain readable at item scale; candidate B became too sparse. The selected overlay is clipped
to each exact item silhouette and despilled to muted brown so no green-screen pixels survive. Clean
and dirty families have byte-distinct diffuse art, matching alpha, and sibling `_m` masks. Dirt is a
visible state change for base cookware, plates, and cutlery and for the eligible wood or stone
families. Chef's knives intentionally retain intrinsic cleanliness and therefore have no dirty
family. Rejected candidates and processing comparisons remain under ignored
`artifacts/VisualAssets/PortableVariations`.

The portable selector is installed only by the exact VTEX/VEF package-and-shape gate. With either
package absent, the setting Off, or the inspected VEF API incompatible, the Defs retain their normal
`Graphic_Single` base artwork.

The exact base-package catalog renders all seven portable Defs and 33 real Things in columns above
native Wood, Granite blocks, Steel, Silver, and Gold references. It covers every Def-eligible Stuff
treatment, clean/dirty pairs, available stack overlays, native selection brackets, and selected-item
inspectors. The retained frames show full-surface material tint on base plates, masked material tint
on cookware and cutlery, readable adobe/glitterworld/prepared-food fixed art, and the three eligible
chef's-knife materials. The directly affected VTEX catalog was then rerun and retained the material
families and sanitation state across native save/load without missing textures.

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

## Sauce station

Selected: candidate A, generated 2026-08-06. Two empty small pans, an empty whisk bowl, closed
condiment bottles, induction pads, and controls remain identifiable at 2×1 map scale without
claiming active cooking. Candidate B was rejected because its central appliance became ambiguous
after final resizing. Both prompts requested state-neutral sauce and finishing equipment on pure
green chroma. The selected 512×256 fixed-color sprite has an exact 2:1 canvas and uses
`Graphic_Single`/`Cutout` for native rotation. Bright blue induction and display pixels were
replaced with unlit graphite controls so the unconditional sprite remains truthful while off,
unpowered, or broken.

## Meat station

Selected: candidate A, generated 2026-08-06. Its broad red-brown butcher block, hanging cleaver and
knives, tenderizer, mechanical scale, and closed insulated bin remain recognizable at 2×1 map scale
without depicting meat, blood, active machinery, or powered lights in every state. Candidate B was
rejected because its tall rear panel and more frontal view made it read less naturally as a
rotatable RimWorld worktable. Both prompts requested a clean state-neutral professional meat-prep
station on pure green chroma. The selected 512×256 fixed-color sprite has an exact 2:1 canvas and
uses `Graphic_Single`/`Cutout` for native rotation.

## Vegetable station

Selected: candidate A, generated 2026-08-06. Its broad pale cutting board, fixed mandoline, box
grater, peeler, dry perforated rinse basket, and closed drawers remain readable at 2×1 map scale
without permanently depicting vegetables, water, active machinery, or powered lights. Candidate B
was rejected because its large spiral slicer and full wooden cabinet made the silhouette busier and
more frontal at final scale. Both prompts requested distinct clean, dry, idle vegetable-preparation
equipment on pure green chroma. The selected 512×256 fixed-color sprite has an exact 2:1 canvas,
fills 480×240 visible pixels to match the sibling 2-cell stations, retains zero vivid-green key
pixels, and uses `Graphic_Single`/`Cutout` for native rotation.

## Pastry station

Selected: candidate B, generated 2026-08-06. Its idle cream stand mixer with empty bowl, broad pale
rolling slab, wooden rolling pin, unlit mechanical scale, nested empty trays, pastry brush, and
closed drawers remain identifiable at 2×1 map scale without permanently depicting dough, flour,
pastries, or powered activity. Candidate A was rejected because its dominant hand-cranked sheeter
risked reading as a pasta-only workstation at final scale. Both prompts requested clean, empty,
state-neutral bakery equipment on pure green chroma. The selected 512×256 fixed-color sprite fills
480×240 visible pixels and uses `Graphic_Single`/`Cutout` for native rotation.

## Fallback countertop microwave

Selected: new candidate B, generated 2026-08-06. Its rounded off-white enamel shell, dark closed
door, fixed handle, top/side vents, and unlit push controls read as one compact appliance in a steep
map view without including any counter, cabinet, shelf, pedestal, legs, or supporting furniture.
The former candidate 0 was rejected because its frontal presentation and cyan controls permanently
looked powered; new candidate A was rejected because its broad front and oversized top remained
flatter and less map-like. Both new prompts requested appliance-only art on pure green chroma. The
selected fixed-color 512×512 `Graphic_Single`/`Cutout` sprite has 346×320 visible bounds; on the
Def's 0.82×0.82 mesh it occupies roughly 0.55×0.51 cells so the real table/workbench remains visible.

## Optional building variation families

When exact VTEX/VEF integration is active, each dishwasher, specialist station, and the fallback
microwave has a two-member family: its selected standard sprite plus one selected `_Variant01`
sprite. VEF remains the graphic owner and supplies its native `Random Graphic` gizmo. The domestic
variant emphasizes an open rack and side controls; the industrial variant changes the wash tunnel,
rack lanes, and control placement. The prep variant rearranges its sink, board, and pans; sauce
changes vessel and hob layout; meat changes its butcher-block/tool composition; vegetable changes
its board, basket, and hand tools; pastry changes mixer/slab/tray layout; and the microwave changes
door, handle, vent, and control geometry while remaining a support-only countertop appliance.

For each concept, generated alternatives were compared against the standard sprite at final map
scale. The retained variants were the candidates whose silhouette and workstation purpose survived
that reduction without baking in food, steam, powered lights, water, or activity state. Every
variant uses the standard sprite's exact canvas and occupied footprint, retains transparency, has
no vivid chroma-key pixels or bright-blue powered cue, and uses the same fixed-color
`Graphic_Single`/`Cutout` contract. The exact VTEX E2E catalog visibly exercised the native gizmo on
all eight buildings, restored the real two-member lists, and captured the selected graphics before
and after a native RimWorld save/load at their real Def draw sizes. The same round trip visibly
preserved the concrete wood/stone/metal and clean/dirty portable textures on the same Stuff-bearing
Things without changing sanitation state. Rejected source candidates and comparison montages remain
under ignored `artifacts/VisualAssets/BuildingVariations`.

The exact base-package building catalog separately rendered all eight concrete building Defs in
their four rotations beside native production/dining references. Its retained frames show readable
selection brackets, footprints, inspectors, Production-menu icons and labels, and the appliance-only
fallback microwave visibly sharing cells with both a real dining table and a real machining table.
