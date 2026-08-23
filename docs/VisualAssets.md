# Immersive Chefs visual assets

Selected sprites are tracked in the mod package. Generated candidates, chroma-key sources,
comparison montages, and rejected variants are retained under ignored `artifacts/VisualAssets`
during development.

## Primitive stone cookware set

Two 1254×1254 original chroma-key candidates were generated on 2026-08-09 from the same brief:
one complete primitive cooking abstraction made from roughly carved stone, including a pot, shallow
pan, lids, wooden handles, and a wooden spatula, viewed from RimWorld's high three-quarter item
camera with no modern metal parts. Candidate 01 separated every component; candidate 02 nested the
complete set into one compact carried-item silhouette. Candidate 02 was selected because the pot,
pan, two stone lids, forked handle, and spatula stay recognizable together at 64 px, whereas
candidate 01 reads as several loose Things at map scale. The raw/normalized comparison remains under
ignored `artifacts/VisualAssets/PrimitiveCookware`.

The selected 256×256 diffuse uses `CutoutComplex`. Its mask assigns the carved stone to RimWorld's
primary Stuff channel and leaves the permanent wood/leather accents black, so modded `Stony` Stuff
can tint the stone without recoloring handles. Clean and dirty diffuse/mask pairs share exact alpha.
The selected clean diffuse SHA-256 is
`EBF421F3C6E79D9680D5B0C2645192DEE897E49C0C638DE5BB1DFD93045ECC0D`; the mask SHA-256 is
`839DDB50C8B4F9FD53A4F19527B6695CFE4604C68AA784622584F3F6F2B1F62A`.

Final map-scale inspection is retained in
`artifacts/EndToEndRuns/Grouped/20260810T004109168Z`: a pawn accepted the native bill order,
consumed exactly five granite blocks plus one wood, and produced the selected good-quality
granite set. Screenshot 000006 shows the exact crafted Thing selected both on the map and in its
native inspector; the compact pot/pan/lid/handle silhouette remains distinguishable at game scale.
That run's `product-deployment-evidence.json` binds the screenshot to product DLL SHA-256
`8ACE7D039A56D1F878E0A760BF867A19F4DB4D24D4694ACA3490648EF72FF030` and primitive diffuse
SHA-256 `EBF421F3C6E79D9680D5B0C2645192DEE897E49C0C638DE5BB1DFD93045ECC0D`.

## About and Workshop preview

Two original 1672×941 (16:9) chef-parody compositions were generated on 2026-08-09. Both briefs
asked for a blond, furious celebrity-chef analogue rendered as a RimWorld pawn in a colony kitchen,
a distressed apprentice pawn, a burnt meal, a large empty speech bubble, and a separate empty title
banner. Candidate 01 was selected because the pointing chef, apprentice, ruined meal, and speech
bubble preserve the familiar joke without using a photograph, while candidate 02's literal donkey
competes with the mod premise. The unselected comparison remains under ignored
`artifacts/VisualAssets/Preview`.

`scripts/Build-ImmersiveChefsPreview.ps1` owns the text, not the generator. It reproducibly writes
`YOU DONKEY!` into the horizontal and vertical center of the bubble and `IMMERSIVE CHEFS` into the banner, producing a 1280×720 Workshop
master and a 640×360 RimWorld `About/Preview.png`. Both are palette-compressed PNGs below 1 MiB.
The exact Workshop master is an internal 16:9 authoring choice; Valve's UGC API fixes the image
preview ceiling below 1 MiB but does not prescribe that exact pixel size. The 640×360 About image
matches the most common 16:9 size in the first 100 locally installed RimWorld preview files. The
deterministic output hashes are
`B61BC0C7E4C626C0C7643278C077466FCEF8BCDBB23092897550490004CDB936` (Workshop) and
`2C0F3B30588810F91C006FEC47AB9E6BD1576AD92E46F03ED167B5880A63E4B9` (About).

Rebuild both previews from the selected source without editing the generated PNGs:

```powershell
.\scripts\Build-ImmersiveChefsPreview.ps1
```

Keep `About/Preview.png` at 640×360 PNG and below 1 MiB. Treat the 1280×720 Workshop image as the
repository's authoring/export contract; verify the upload service's current byte and format rules
again when publishing because they are external rather than a RimWorld Def contract.

The centered build was finally inspected in RimWorld's native Mods screen at 1600×900 in
`artifacts/GatewaySmoke/20260810T-current-final-preview-135/20260810T175453038Z`. The exact visible,
non-maximized process loaded Harmony, Core, Immersive Chefs, and the Dev Gateway in that order; the
complete preview was uncropped, `YOU DONKEY!` was centered in its speech bubble, and the native
metadata panel showed `Author: Fumblesneeze` and `ID: fumblesneeze.immersivechefs`. The run binds
that view to final reviewed product DLL SHA-256
`1354240F7F31A1208C21299E70F3A8A96DB4AD078818EB2CBF3869355DFF7BD2`, About preview SHA-256
`2C0F3B30588810F91C006FEC47AB9E6BD1576AD92E46F03ED167B5880A63E4B9`; it exited through the
native window-close path with exit code zero, sanitized the Gateway credential, and restored both
normal configuration hashes. Its retained Gateway log contains only the expected unrestricted-
execution security warning and no unexpected warning or error. RimWorld does
not load the separate Workshop master; its SHA-256
`B61BC0C7E4C626C0C7643278C077466FCEF8BCDBB23092897550490004CDB936` is instead bound to the same
centered source/layout by the deterministic focused asset test and committed build output.

The final minimal player-workflow acceptance run is
`artifacts/EndToEndRuns/Grouped/20260810T-current-final-135/20260810T175016221Z`. Its six sequential native scenarios cover the
current-build crafting, trader stock and purchase, visitor dining and exact personal ware return,
cook-owned washing, force-dirty cooking, food-poisoning cause, toxic-ware ingestion, primitive art,
and player-facing inspection surfaces. All six passed on the same product DLL, their 38 causal
screenshots were personally inspected, and no Immersive Chefs warning or error appeared. Its only
structured warning was the Dev Gateway's intentional unrestricted-execution notice. Cleanup
restored the normal mod-list and preference hashes and removed the exact staged E2E bundles.

The required optional-material workflow ran on that same DLL in
`artifacts/GatewaySmoke/20260810T-current-final-expanded-materials-console/20260810T174058503Z`.
The exact active order was Harmony, Core, Argonic Core, Vanilla Expanded Framework, Expanded
Materials - Masonry, Expanded Materials - Metals, Immersive Chefs, and Gateway. Native one-shot
bills visibly progressed and produced wooden, adobe, and silver kitchenware; the selected adobe
plate's native inspector showed its completed four-unit stack and material profile. The native
developer console was opened and personally inspected in the same process: it showed both Expanded
Materials integrations initialized, no error, the expected Gateway warnings, and metadata warnings
belonging to unrelated downloaded-but-disabled mods. The process exited natively, sanitized its
credential, and restored both normal configuration hashes. The final preview run skipped deployment;
retained before/after hashes prove that it used the same `1354240F...F7BD2` DLL.

## Cookware set

The modern set was regenerated on 2026-08-19 after the shipped mask was found to contain 115
interior fixed-color components, 113 of them no larger than 16 source pixels. Steel tinting left
those islands dark while the surrounding material changed color, producing visible speckle and
blocky mip artifacts. Four independent candidates requested an original RimWorld-like, high
three-quarter set containing a deep lidded pot, shallow pan, loose lid, slotted spatula, and fixed
brown wood handles. Compact Candidate B was rejected because its overlapping forms collapsed into
an animal-like dark blob. Open Candidate A was also rejected by the context-free review because its
joined bodies and raised handles still read as an animal or scrap pile on soil and on the stove.
Selected Candidate D instead preserves the pot, pan, lid, and spatula as four disconnected,
unmistakable silhouettes with transparent space between their final contours at 40, 64, and 128 px.

The selected 256×256 diffuse uses broad, low-frequency shading and an eight-source-pixel
`portable-broad` exterior contour, which becomes two pixels in the 64 px package proxy. The weaker
four-source-pixel candidate was rejected after it faded into the workstation in game, and the usual
Core-brown contour was darkened from `#17130F` to the manifest-pinned near-black `#090705` to retain
contrast against the stove without expanding beyond the approved two-final-pixel contour. Candidate
B's generated input was translucent across 92.2% of its visible pixels, which made the cookware-to-
contour transition look jagged. Candidate D normalization instead makes the measured foreground
opaque and confines fractional alpha to the real silhouette boundary. AgentBrush's alpha-edge
smoothing was evaluated for the rejected input but not applied because smoothing a translucent
interior cannot reconstruct an opaque silhouette. The contour is built from an exact Euclidean
distance field at 4× source resolution and filtered down once, giving rounded fractional-alpha edges
instead of the rejected square binary dilation. The generic contour processor preserves every
existing nontransparent pixel byte-for-byte. `Cookware_m.png` assigns continuous cookware surfaces
to RimWorld's primary Stuff channel; only the wood handles and exterior contour stay fixed. The clean
mask contains no tiny interior fixed-color islands. The dirty sibling shares identical geometry and
uses broad irregular inset sauce/grease patches rather than concentric bands or per-pixel variation.
Clean diffuse/mask SHA-256 values are
`2D2AA6930A740BA0F3B219341218A21FB96C67D4399377087FF9EFE69817A7C9` and
`CEFC2F03DCDDA7AB2B2A1C1020918620A1B7733128865A9DA19BC01EFFD96B02`; dirty values are
`2E5A6853BBDA2F604C228C4698D4957444F9EDC53D912834DBD974E0CA8B3CFD` and
`D659A95F806644F5DDD68B63AC848C1238F4677E77F718F994A0511247B5F79C`.
The measured brief, raw candidates, deterministic normalization scripts, and comparison sheets stay
under ignored `artifacts/VisualAssets/MetalCookwareCorrection/20260819`.

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

The 2026-08-09 building-art correction reopened every building selection and directional/live
acceptance task below. The former `Graphic_Single` notes describe the rejected historical package,
not an accepted target. A controlled generator comparison used the same 1024×1024 four-cardinal
dishwasher brief and seed `424242` for built-in image generation plus local `flux2-nasa`, `flux2`,
`krea2`, `realvisxl`, `juggernaut`, and `zimage`. Raw sheets, normalized 512×256 drafts, and the
comparison montage remain under ignored
`artifacts/VisualAssets/BuildingGeneratorComparison/20260809`.

The focused exact run `artifacts/EndToEndRuns/Grouped/20260808T222729488Z` rendered every normalized
draft through a disposable real ThingDef, selected each draft through the native selection path,
and captured it beside the same Core butcher table, electric stove, and machining table in one
process. The acting agent inspected all eight retained screenshots. Built-in generation won: it was
the only route whose low bench, closed washer lid, rack, outline, and broad value groups remained
readable at the actual map zoom without becoming muddy or changing product identity. Flux2 NASA and
plain Flux2 collapsed into low-contrast tan cabinet shapes; Krea 2 became an underspecified olive
icon; RealVisXL and Juggernaut failed the requested workstation/cardinal composition; Z-Image
produced inconsistent generic cabinets. The disposable comparison Defs, textures, and E2E test were
removed after the decision and never enter the release package.

## Current directional building catalog

### Measured Core projection baseline

The 2026-08-09 directional correction was reopened after the generated cardinal frames proved
geometrically inconsistent. A read-only Unity asset extraction sampled twelve actual Core
production-bench families from RimWorld 1.6.4871 `resources.assets`; extracted rasters, contact
sheets, and machine-readable extraction metadata remain ignored under
`artifacts/VisualAssets/VanillaWorkbenchBaseline/20260809` and are not redistributed.

The measurements are durable in `docs/WorkbenchSpriteGeometry.xml`. Core's standard 3x1 benches
use a 224x96 north/south canvas and 96x224 east canvas for a 3.5x1.5 Def draw size: exactly 64
pixels per map cell. The clean hand-tailoring bench makes the shared construction especially clear.
Its centered tabletop is exactly 192x64 pixels, matching the 3x1 footprint, and its visible apron,
legs, or underframe occupy only the next 9 pixels on the **screen-bottom** edge. In the east frame,
the tabletop becomes 64x192 but the same 9-pixel underframe remains at the image bottom; it does
not rotate onto a long side. North and south likewise retain the same table/body projection while
their shears and thread move for the opposite interaction side. The butcher, machining, smithing,
stove, stonecutting, brewery, drug-lab, sculpting, and electric-tailoring samples confirm the same
canvas/footprint scale while documenting legitimate equipment projections beyond the bare table.

Immersive Chefs now derives two authoring templates at four times Core density. A 2x1 bench uses a
2.5x1.5 draw canvas: 640x384 horizontally and 384x640 vertically, with a centered 512x256 or
256x512 tabletop and a 36-pixel screen-bottom underframe. A 3x1 bench uses Core's 3.5x1.5 draw
canvas: 896x384 or 384x896, with a centered 768x256 or 256x768 tabletop and the same 36-pixel
underframe. These are hard composition guides, not merely resize targets. The table/body must be
the same construction across opposite frames; only the tools, controls, racks, vessels, and other
worker-facing equipment may rotate or change position.

The previously promoted south/west correction was rejected because it violated this baseline. In
particular, several east/west frames placed a cabinet face on a long side or image top, while several
south frames changed the apparent table height or construction. No earlier catalog run or hash is
acceptance evidence for the replacement below.

Built-in image generation then produced fourteen new four-cardinal sheets: base and VTEX alternate
families for the domestic and industrial dishwashers plus prep, sauce, meat, vegetable, and pastry
stations. The prompt used the measured bare Core table as a projection reference, fixed one camera
for all four cards, required the apron/underframe at screen bottom in every card, and explicitly
forbade manufacturing vertical or opposite views by pixel rotation. North places the worker below,
East left, South above, and West right. The countertop microwave was separately reopened and
regenerated on 2026-08-23 after its earlier square cardinal art proved too flat and lacked a measured
top/casing projection. The replacement preserves the already-correct local-south front mapping. Its
retained numeric plane contract and exact prompts are in `docs/MicrowaveSpriteGeneration.md`.

The raw sheets and normalization pipeline remain ignored under
`artifacts/VisualAssets/MeasuredCardinalRedraft/20260809`. Normalization uses direction-specific
source crops, preserves only the connected workstation silhouette plus intentionally enclosed green
ingredients, removes the chroma-connected background, neutralizes green outside preserved holes,
removes detached fragments, and resizes the result into the exact measured tabletop/underframe
bounds. A final two-pixel silhouette-edge decontamination pass produced zero green-dominant fringe
pixels across all 56 frames. Dark- and light-background contacts and a fixed-pixels-per-cell Core
comparison are retained in that directory.

Every workbench family uses `Graphic_Multi`/`Cutout` with exactly `_north`, `_east`, `_south`, and
`_west` files. A 2x1 station now uses a 640x384 horizontal or 384x640 vertical canvas and a
`(2.5,1.5)` draw size. The 3x1 industrial dishwasher and prep station use 896x384 or 384x896 and a
`(3.5,1.5)` draw size. Each opposite pair preserves one construction and fixed-camera
foreshortening while its tools, controls, racks, and worker-facing details change projection. Most
importantly, the `Rot4.South` view—with its interaction cell north of the footprint—keeps the
screen-near underframe at image bottom instead of presenting a bench viewed from the impossible
opposite camera. These buildings are fixed-color, so no Stuff mask is appropriate; portable
Stuff-aware wares retain their separate masked paths described above.

`docs/DirectionalSpriteApprovals.xml` pins the exact hashes and truthful world-space equipment
order of all 56 visually reviewed frames and links them to `docs/WorkbenchSpriteGeometry.xml`.
The first independent review rejected east/west equipment order in five families and then caught a
mislabeled base-prep sink during the correction. The final high-resolution review confirmed that
north/east preserve each canonical order, south/west reverse it, and every apron remains at screen
bottom. The focused package RED is
`artifacts/TestResults/20260809T122818689Z-30760-b04d8e17e1eb4579b06f80e0d6cc4a82`;
it failed on the old 2x1 dishwasher draw size. A later focused RED at
`artifacts/TestResults/20260809T162822134Z-59348-ac873c37815b4637ba123c7e7b5f31ce`
rejected approvals without direction-specific equipment order. The reviewed final 20-test visual
package GREEN is
`artifacts/TestResults/20260809T165649825Z-24268-7ba9ce5a349544dbb2b5c520ee6aee1e`.

The earlier base run is historical evidence for the rest of the catalog and defect evidence for the
rejected microwave, not acceptance evidence for the 2026-08-23 replacement. It is
`artifacts/EndToEndRuns/Grouped/20260809T001235413Z`. In the exact Core/Harmony/Immersive
Chefs/Gateway process it captured all eight concrete building Defs in four cardinal directions,
including 32 close same-zoom custom/Core pairs, Production-menu icons and labels, and the microwave
on both a real dining table and machining table. The same run then used the native Production `Designator_Place`
path to turn an empty marked area into an east-facing dishwasher; screenshots 41 and 42 retain the
before/action/after evidence. PID 50160 exited cleanly, the isolated config/preferences hashes were
unchanged, the staged bundles were removed, and credentials were sanitized.

That run predates the measured replacement and is retained as causal defect evidence, not
acceptance for the corrected binaries.

Final live acceptance is
`artifacts/EndToEndRuns/Grouped/20260809T165805515Z`. The exact
Core/Harmony/Immersive Chefs/Gateway process ran only
`immersive-chefs.base-building-visual-catalog` on Release product DLL SHA-256
`AFAB04A24BDA87E56B58F0AF9EE9B018E84AD1D95058E61E395A80DF34093F0E`.
Screenshots 2–5 show the complete north/east/south/west custom rows beside same-facing Core
benches; screenshots 9–40 retain each close same-zoom pair. The acting agent personally inspected
the exact frames and observed consistent map scale, constant tabletop/apron depth, world-rotated
equipment, and a screen-bottom apron in every south-facing/north-interaction dishwasher and station.

Screenshots 41/42, 43/44, 45/46, and 47/48 are causal before/after pairs for native Production
designator placement of north/east/south/west dishwashers beside same-facing machining benches.
The after frames visibly contain the selected player-placed building where the before frame was
empty; screenshot 46 also shows the interaction spot north of the south-facing dishwasher while its
apron remains at screen bottom. The focused scenario passed, the retained log contained no error
entries, PID 47552 exited normally through window close without force, normal config and preferences
hashes were unchanged, credentials were sanitized, and the staged test bundle was removed.

The following 2026-08-06 notes are retained only as rejection history for the superseded
single-view package. They are not descriptions of the shipped building art.

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

Selected on 2026-08-23 from two exact axis-aligned four-cardinal briefs and one measured top/casing
refinement per brief. The earlier diagonal three-quarter and ultra-thin low-body attempts were
rejected because they used the wrong RimWorld projection and read as a VHS/VCR. The warm light-grey
family with round controls is the base; the warm ivory family with square controls is the supported
`_Variant01`. Both are fixed-color 512×512 `Graphic_Multi`/`Cutout` families with four independently
illustrated fixed-camera views.

The frames now follow the convention measured from two installed runtime comparator families:
South is the full door/front, North is the rear, East is an axis-aligned vertical side with its narrow
front terminal on screen-right, and West is an axis-aligned vertical side with its narrow front
terminal on screen-left. The selected outlined bounds span 322–454 pixels wide and 338–448 pixels
high, retain at least 29 transparent source pixels at every edge, and satisfy the building contour's
three final-scale dark rings. Their substantial casing, distinct top/casing seam, normal microwave
cavity ratio, controls, and vents remain legible at the 64×64 proxy. They contain no counter,
cabinet, shelf, pedestal, floor, cast shadow, food, text, or powered display. Exact successful prompts
and numeric plane contracts are retained in `docs/MicrowaveSpriteGeneration.md`; raw sheets, rejected
proportions, chroma normalization, source/map-scale contacts, and outline candidates remain ignored
under `artifacts/VisualAssets/MicrowavePerspective-20260823`.

Final reviewed-build live acceptance is
`artifacts/EndToEndRuns/Grouped/20260823T195055338Z`. In isolated PID 67420, the exact ordered
Core/Harmony/Immersive Chefs/Gateway group used the native Production designator to place North,
East, South, and West microwaves on real steel tables. The acting agent personally inspected the
wide rotation rows in screenshots 2–5 and the unobstructed native after-placement frames 50, 52, 54,
and 56. North visibly shows the blank vented rear, East and West remain axis-aligned opposing side
profiles with their front terminals at opposite screen ends, and South visibly shows a normal dark
microwave cavity and separate controls. Every orientation retains the support table, a coherent
fixed-camera top/casing projection, and a continuous readable contour at the closest practical map
zoom.

The final fixture powers the already player-placed appliance only after native construction so the
irrelevant needs-power overlay cannot cover its one-cell art; this bounded visual setup was
independently reviewed and leaves placement causality intact. A fresh context-free reviewer received
only screenshots 2–5, 50, 52, 54, and 56, identified the object as a compact countertop microwave,
and found no perspective, outline, internal-edge, style, support-contact, cardinal-coherence, scale,
or zoom-legibility defect. The scenario passed, the retained complete log contained zero errors, the
reviewed product DLL SHA-256 was
`3C00C7E9FDD2F7E60322474B270626578C9E54CA97860F6EBCC158C2BB1009C4`, normal config and preference
hashes were unchanged, credentials and staged bundles were cleaned, and the exact process exited
normally through window close without force.

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
`Graphic_Multi`/`Cutout` contract.

The reviewed optional-mod run is
`artifacts/EndToEndRuns/Grouped/20260809T002924352Z`. Its exact
Core/Harmony/VEF/VTEX/Immersive Chefs/Gateway process (PID 44916) arranged all eight building
families in North, East, South, and West copies, then invoked each real VEF graphic-cycle gizmo until
the selected Thing visibly used `_Variant01`. Screenshots 2–9 retain the 32 selected alternate
views; the acting agent inspected the cardinal groups at their exact map scale and observed coherent
directional silhouettes and footprints. The test restored every two-member VEF family before a
native RimWorld save/load. Its checkpoint and screenshots 11–14 show that every same Thing retained
the exact `_Variant01` path and rotation after loading. Screenshots 10 and 15 likewise retain the
portable wood/stone/metal and clean/dirty graphics without changing sanitation state. The isolated
configuration and preferences hashes remained unchanged, the process exited cleanly, staged bundles
were removed, and credentials were sanitized. Rejected source candidates and comparison montages
remain under ignored `artifacts/VisualAssets/BuildingVariations`.

The exact Thermodynamics exclusion rerun is
`artifacts/EndToEndRuns/Grouped/20260809T004004598Z`. In PID 56300, the ordered
Core/Harmony/RimFridge/Thermodynamics/Immersive Chefs/Gateway group cooked a plated simple meal,
stored it in RimFridge, heated it through Thermodynamics' native `DMicrowave` job, and ate it.
The acting agent inspected all five retained frames: the selected frozen meal showed one bound
plate, culinary quality, and Thermodynamics' own temperature text; the diner visibly heated the
same meal at `DMicrowave`; and the exact dirty plate/cutlery setting appeared after ingestion. The
finalized Immersive Chefs microwave Def was absent, so none of its directional art materialized.
The exact process passed 35 steps, exited cleanly, removed its staged bundles and credentials, and
left both normal configuration hashes unchanged.

The exact base-package building catalog separately rendered all eight concrete building Defs in
their four rotations beside native production/dining references. Its retained frames show readable
selection brackets, footprints, inspectors, Production-menu icons and labels, and the appliance-only
fallback microwave visibly sharing cells with both a real dining table and a real machining table.

## Core-relative comic outlines

Every shipped Thing diffuse now carries a near-black exterior contour selected against measured
Core references at final map scale. Portable fine-detail art uses a 0.75–1 pixel contour, broad
portable art uses 1–2 pixels, and buildings use 2–3 pixels in the deterministic Pillow final-scale
proxy used for package approval. The
processor only fills originally transparent canvas-edge-connected background, preserves the prior
opaque art and protected internal gaps, and mirrors the final alpha into all 17 Stuff masks while
keeping the added contour fixed black. `docs/SpriteOutlineApprovals.xml` records the selected class,
stroke, topology oracle, protected runs, final-scale ring measurements, and output hashes for all 83
diffuses; focused tests separately enforce every one of the 17 masks and its alpha/class semantics.

The final reviewed package was exercised in three fresh minimized processes, all using product DLL
SHA-256 `FBFDCF5D5831B9E5D346AEBB5222D44E2EE9EA84F542283BF61FFF28A20A82BC`:

- `artifacts/EndToEndRuns/Grouped/20260811T105154314Z` visibly renders every base building family in
  all four directions beside same-direction Core benches, then uses native placement and rotation on
  the dishwasher. Its portable catalog shows primitive granite and modern steel cookware,
  glitterworld cookware, wood/stone/steel/silver/gold clean and dirty tableware, the chef's knife,
  and prepared ingredients with readable selection brackets, preserved cutlery-tine gaps, material
  tinting, and distinct sanitation art. The final native prepared-paste cooking/eating workflow
  visibly returns the exact outlined dirty steel plate and cutlery.
- `artifacts/EndToEndRuns/Grouped/20260811T104554802Z` visibly follows the ordinary dirty-cookware
  workflow from the selected dirty steel set through wild-water washing and native cooking, with the
  Stuff-tinted kitchenware prop centered before the chef. It separately retains the forced-dirty
  cooking action and the exact dirty cookware physically returned beside the stove after the bill
  completes.
- `artifacts/EndToEndRuns/Grouped/20260811T104809748Z` invokes the real VEF graphic-cycle gizmos and
  visibly renders all eight optional alternate building families in North, East, South, and West.
  Their contours, fixed-camera underframes, equipment order, and interaction-side perspective remain
  consistent. The portable VTEX wood/stone/metal and clean/dirty families retain their Stuff masks,
  contours, gaps, and sanitation state before and after native save/load.

The acting agent inspected those exact frames at original resolution on both light and dark terrain.
All three processes passed their admitted workflows, exited with code zero, restored the normal
configuration and preference hashes, removed staged bundles, and sanitized credentials. Their
developer-mode logs contain no product warning or error; the only warning-severity entry is the Dev
Gateway's intentional unrestricted-development notice.
