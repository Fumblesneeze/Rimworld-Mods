# Vegetable-station vanilla remake — September 2026

Owner: Immersive Chefs. Built-in imagegen was used as authorized. No model selector or authoritative backend metadata was returned; appearance does not verify GPT Image 2.5.

Selected base: four B cards, exec-be73d17a-16b2-4be5-914e-e44e14315957.png, from A exec-8c988813-cd61-45d7-917c-89e996bb9e43.png. Selected alternate: four B cards, exec-9592e723-3efc-49ef-919e-7b79719b4112.png, from A exec-c591b700-5710-4958-8f5b-64e960bcdcb1.png. All eight selected directions and both families scored 9/10 in independent static review. Original A scores remain retained: base N9/E8/S9/W8, family8; alternate N9/E8/S8/W6, family7. B corrects grips, blade slopes, peeler rotation and physical tool dimensions, particularly the compressed alternate West. No live acceptance is implied by the static scores.

Base equipment is a wooden board, compact slicer, flat handled grater and draining tray. North/East order is board–slicer–grater–tray; South/West reverse it. Slicer grips face bottom/left/top/right in N/E/S/W; diagonal blades rotate with the top plane. Grater loops and tray tabs face top/right/bottom/left. Selected broad perforation grids use 3x4 and rotated 4x3 marks; this is a reviewed simplification of the initial approximately 3x5 prompt. The narrow board is approximately 94x168 source pixels, with swapped axes in East/West.

Alternate equipment is a pale square board, draining basket, mandoline with a side knob and Y peeler. North/East order is board–basket–mandoline–peeler; South/West reverse it. Basket and peeler grips face bottom/left/top/right. Mandoline ends face top/right/bottom/left, side knobs left/top/right/bottom. The approximately 134x136 board stays square after rotation; West retains horizontal tools and a visible lower working-plane strip. All painted thickness stays screen-bottom. No permanent food, text, detached tools, floor or cast shadow is included.

Original Core butcher, electric stove and hand-tailoring tables were inspected as conceptual kitchen, fixed-color metal and table geometry references. Source resources.assets SHA256: 7C0C69BDE01910DDE60205BC1417CD56A39E27AFFD2DE36735DCEDD6FACD6155. Core pixels remain ignored, read-only inputs. The measured contract in WorkbenchSpriteGeometry.xml retains the 2x1 footprint, 2.5x1.5 draw size and Graphic_Multi/Cutout shader. Canvas 640x384 or384x640; top512x256 or256x512, shallow screen-bottom apron36, padding64. No Stuff mask is consumed. Eight-source-pixel #171412 exterior contours normalize to approximately2 pixels at64px/cell, with one foreground component and no alpha holes.

Selected top bands measure120.706–128.802 and apron/top ratios .694–.743 within unchanged90–140 and .45–.85 limits. Local processing removes chroma, normalizes measured bounds, cleans silhouette spill and applies the approved contour; subject corrections were made by imagegen. Exports are vegetable-outline-b-clean and vegetable-outline-alt-b-clean under ignored VanillaRemake-20260909 artifacts; product hashes and reviewed cardinal landmarks are pinned in both approval manifests.

The eight-case RED on the original sprites executed8 with7 failures and1 pass. Following B promotion, all59 visual package checks pass. A dedicated native placement/variation workflow covers both layouts, four rotations, three zooms and supporting health states beside original Core stove/butcher at noon. Its fresh live visual review remains pending. No product C#, Def, stats, cost, recipe, research, dependency or interaction offset changes are made.

The exact four generation prompts follow. Raw and rejected candidates remain ignored.

## vegetable-four-prompt-a.txt

Generate original RimWorld game art: a FOUR CARDINAL sheet of a 2x1 VEGETABLE PREPARATION WORKSTATION. Reference roles: the four-card meat sheet supplies ONLY the neutral-gray counter geometry, shallow screen-bottom depth and exact four-card layout; the old vegetable sprite supplies ONLY equipment identity; the small Core stove/tailor images supply vanilla painted style and fixed camera. Do not copy reference pixels. Remove all red boards, pans, cleavers and scales from the meat reference.
Match the four-card layout exactly: NORTH upper-left and SOUTH lower-left horizontal; EAST upper-right and WEST lower-right vertical. Same physical workstation turned in world space, each freshly rendered from the same fixed high RimWorld camera. No isometric diagonals, raster rotation, taper, variable world scale or oblique furniture body.
Measured output contract at256px/cell: N/S canvas640x384, tabletop512x256, apron512x36; E/W canvas384x640, tabletop256x512, apron256x36 on SCREEN BOTTOM. Keep64px top/left canvas padding; sheet has generous magenta gutters. In this sheet use the reference's exact bench outer rectangles/card positions. No east-side extrusion, legs, cabinets or tall face. Preserve a clear narrow gray working strip along the lower tabletop before its dark shallow apron. Neutral gray tabletop aroundRGB125 with restrained broad shading; apron aroundRGB80. Outer contour near#171412, straight and continuous.
Paint in vanilla Core's low-detail language: matte broad wood and gray metal shapes, quiet large value transitions, thin dark tool outlines, one darker screen-bottom thickness band per object. No bright glossy rim, bevel stacks, photoreal texture, scratches, lettering, buttons or tiny fasteners. Tools are idle and empty. No food, permanent vegetables, blood, grime, floor, shadows outside the silhouette, labels, frames or text. Pure flat #ff00ff background.
All tool sizes below describe physical top footprints; quarter-turn their axes in E/W without changing area. Keep each group separate and away from the outer contour. Every protruding grip, blade, tab or knob must rotate with its tool, while its painted depth always faces SCREEN BOTTOM.
BASE INVENTORY, exactly four groups:
1) One warm tan wooden cutting board, top136x176, plain broad wood with one shallow darker bottom edge.
2) One compact vegetable slicer on a shallow dark-gray cradle, top72x148, one simple diagonal silver cutting edge and one short dark end grip. Avoid dense mechanisms.
3) One flat gray rectangular hand grater, top72x144 with a short rounded rectangular loop grip. Show only3columns by5rows of broad dark grating marks, not tiny realistic holes.
4) One shallow charcoal draining tray, top88x148, simple gray rim and3columns by5rows of broad dark perforations; three small tabs on its far short edge.
NORTH left-to-right board–slicer–grater–tray. EAST top-to-bottom same order. SOUTH left-to-right tray–grater–slicer–board. WEST top-to-bottom reversed order.
For N/E/S/W the slicer end grip faces bottom/left/top/right; grater loop faces top/right/bottom/left; tray tabs face top/right/bottom/left. Slicer diagonal blade rotates coherently by90degrees per turn. Wooden board long axis vertical in N/S and horizontal in E/W. Keep light metal slicer/grater distinct from the darker draining tray. No alternate peeler in this base layout.


## vegetable-alternate-prompt-a.txt

Generate original RimWorld game art: a FOUR CARDINAL sheet of a 2x1 VEGETABLE PREPARATION WORKSTATION. Reference roles: the four-card meat sheet supplies ONLY the neutral-gray counter geometry, shallow screen-bottom depth and exact four-card layout; the old vegetable sprite supplies ONLY equipment identity; the small Core stove/tailor images supply vanilla painted style and fixed camera. Do not copy reference pixels. Remove all red boards, pans, cleavers and scales from the meat reference.
Match the four-card layout exactly: NORTH upper-left and SOUTH lower-left horizontal; EAST upper-right and WEST lower-right vertical. Same physical workstation turned in world space, each freshly rendered from the same fixed high RimWorld camera. No isometric diagonals, raster rotation, taper, variable world scale or oblique furniture body.
Measured output contract at256px/cell: N/S canvas640x384, tabletop512x256, apron512x36; E/W canvas384x640, tabletop256x512, apron256x36 on SCREEN BOTTOM. Keep64px top/left canvas padding; sheet has generous magenta gutters. In this sheet use the reference's exact bench outer rectangles/card positions. No east-side extrusion, legs, cabinets or tall face. Preserve a clear narrow gray working strip along the lower tabletop before its dark shallow apron. Neutral gray tabletop aroundRGB125 with restrained broad shading; apron aroundRGB80. Outer contour near#171412, straight and continuous.
Paint in vanilla Core's low-detail language: matte broad wood and gray metal shapes, quiet large value transitions, thin dark tool outlines, one darker screen-bottom thickness band per object. No bright glossy rim, bevel stacks, photoreal texture, scratches, lettering, buttons or tiny fasteners. Tools are idle and empty. No food, permanent vegetables, blood, grime, floor, shadows outside the silhouette, labels, frames or text. Pure flat #ff00ff background.
All tool sizes below describe physical top footprints; quarter-turn their axes in E/W without changing area. Keep each group separate and away from the outer contour. Every protruding grip, blade, tab or knob must rotate with its tool, while its painted depth always faces SCREEN BOTTOM.
ALTERNATE INVENTORY, exactly four groups:
1) One pale tan wooden cutting board top156x176 with a simple dark bottom thickness.
2) One dark shallow draining basket top104x144. Use a simple sparse rectangular grate (3x4 broad openings) and ONE short rounded grip on one short edge.
3) One slim matte metal mandoline slicer, top64x156. One broad diagonal cutting blade, three broad parallel guide grooves, one rounded rectangular end handle and one small dark round side knob. No dense mechanical detail.
4) One Y-shaped vegetable peeler top42x120: dark brown handle, open gray Y head and one simple transverse blade.
NORTH left-to-right board–basket–mandoline–peeler. EAST top-to-bottom same order. SOUTH left-to-right peeler–mandoline–basket–board. WEST top-to-bottom reversed order.
For N/E/S/W: basket grip bottom/left/top/right; mandoline end handle top/right/bottom/left; mandoline side knob left/top/right/bottom; peeler handle bottom/left/top/right, opposite its Y head. Mandoline diagonal blade rotates90degrees per turn. Preserve equal physical sizes after rotation; all top planes are clearly visible from fixed high camera, side thickness SCREEN BOTTOM. Do not add the base grater or a second tray.


## vegetable-four-prompt-b.txt

Correct ONLY these specific geometry details in the existing four-card vegetable workstation sheet. Preserve North and South, every counter body/apron, all materials, inventory, group positions, grater and draining-tray details.
EAST upper-right: move the small dark end grip on the compact slicer from its RIGHT edge to its LEFT edge. Change the slicer's diagonal cutting edge from upper-left-to-lower-right to lower-left-to-upper-right. Keep its shallow screen-bottom thickness.
WEST lower-right: move the slicer grip from LEFT to RIGHT. Keep its existing lower-left-to-upper-right blade slope.
In BOTH East and West, slightly reduce the wooden board's top-plane width by3percent and top-plane height by10percent around its center, keeping a shallow screen-bottom dark thickness. The boards should be the same physical rectangles as the North/South boards, only quarter-turned; they are currently too broad in the short dimension.
The East slicer's top plane is also slightly too broad in its short dimension: reduce ONLY its top-plane short dimension by8percent, with its entire grip/body kept attached. Keep North/South unchanged and do not resize an entire card.
All loop handles on graters and tabs on draining trays already rotate correctly: preserve them. Retain all perforation grids, quiet gray/wood painting and straight dark counter boundaries. No added shadows, text, food or parts. Same sheet dimensions and pure #ff00ff background.


## vegetable-alternate-prompt-b.txt

Correct the actual world rotations of equipment in this existing four-card vegetable workstation sheet. Keep the bench bodies, gray top/apron boundaries, materials and North upper-left card unchanged.
EAST upper-right: move the draining basket's grip from its RIGHT edge to LEFT. Preserve its size and lattice. The mandoline's end handle RIGHT and knob TOP are already correct; change only its diagonal cutting blade to slope lower-left-to-upper-right. Keep the East peeler handle LEFT/head RIGHT.
SOUTH lower-left: rotate/repaint ONLY the Y peeler180degrees in world space: brown handle at TOP and open metal Y head at BOTTOM. Maintain its original size, position and painted depth facing screen bottom. Everything else on South unchanged.
WEST lower-right needs the proper quarter-turned versions of all North equipment in reverse order, from TOP to BOTTOM: horizontal Y peeler, horizontal mandoline, draining basket, wooden board.
- Peeler must lie horizontally, head LEFT and brown handle RIGHT, same physical length/width as North.
- Mandoline must lie horizontally with rounded end handle LEFT, round side knob BOTTOM and diagonal cutting edge lower-left-to-upper-right. Keep the same physical length/width as North. Its side thickness stays SCREEN BOTTOM.
- Basket grip RIGHT. Its top should match North's physical size rotated90degrees; do not compress its short dimension.
- Restore the board to the same near-square top shape and physical area as North. West board is currently flattened vertically and must grow back to a square, with shallow screen-bottom thickness.
Arrange the corrected West group stack with generous clean gaps; move the whole stack slightly upward if needed so the board's bottom ends at least25raw sheet pixels ABOVE the apron boundary. No tool may cover the last gray working strip or touch the exterior contour. There is room because the corrected horizontal peeler and mandoline require less vertical space.
No added tools, no size changes in North/East/South, no dense new details, no extra shadows or text. Retain the same sheet and card rectangles, pure flat #ff00ff background, fixed high map camera and all screen-bottom depth.



## Final B/B acceptance

Independent scoped package review found no issues. Fresh native live A passed with the exact reviewed B/B package. Root personally inspected all46full captures; fresh context-free review recognized kitchen preparation and scored each24clean frame9/10 quality and9/10 vanilla fit. Both variants and allfour directions passed close/ordinary/far comparison beside original Core stove and butcher artwork at noon. Supporting damaged-state views remained coherent. Exact digest, native actions, source-to-blind frame mapping, unchanged normal configuration hashes and successful cleanup are retained in artifacts/VisualAssets/VanillaRemake-20260909/review/vegetable-live-a.md. All8vegetable PNGs are accepted.
