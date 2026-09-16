# Meat-station vanilla remake — September 2026

Owner: Immersive Chefs. Built-in imagegen was used as authorized by the user. The tool returned no authoritative backend-model selection/metadata; appearance does not verify GPT Image 2.5.

Selected base: all four H cards (exec-70b71fa2-bae9-40a0-9255-15856d834e52.png). Selected alternate: all four F cards (exec-bf3553d5-e92c-45c0-a80e-04051d71b4ec.png). Independent static review scored every direction and both families9/10. Scoped package review found no issues, all 51 visual checks passed, and fresh native acceptance scored all 24 clean views 9/10 for quality and fit in blind review; selected exports are retained under meat-outline-h-clean and meat-outline-alt-f-clean.

Base equipment: muted red cutting board, one covered pan with bar handle, one separate cleaver, and a small platform scale with a simple dark control strip. North left-to-right and East top-to-bottom order is board–pan–cleaver–scale; South and West reverse it. Cleaver handle points down/left/up/right in N/E/S/W, with two round pins. Scale display edges rotate bottom/left/top/right, while all side thickness stays screen-bottom. The West equipment stack moves slightly upward to preserve a clear bottom working-plane strip, retaining all sizes and inter-equipment gaps.

Alternate equipment: larger square red board with one diagonal cleaver on it, one covered pan, and a small platform scale. North/East order board-with-cleaver–pan–scale; South/West reverse it. Cleaver handle directions SW/NW/NE/SE and scale control edges bottom/left/top/right follow world rotation. Three small round handle pins are retained consistently, as in the previous alternate; the requested two were an early visual preference, not a gameplay requirement. Pan handle axis is horizontal N/S and vertical E/W in both families. All equipment is empty and idle, without permanent food, blood or powered indicators.

Keep the existing Graphic_Multi/Cutout, 2x1 footprint, 2.5x1.5 draw size, native interaction directions and optional variation owner. Core butcher provides conceptual cutting-tool reference; electric stove provides fixed-color kitchen metal; hand tailoring provides bare counter geometry. Original Core resources SHA-256 7C0C69BDE01910DDE60205BC1417CD56A39E27AFFD2DE36735DCEDD6FACD6155, configured game1.6.4871rev590. Pixels stay ignored and are never packaged.

The measured templates are in docs/WorkbenchSpriteGeometry.xml: at256px/cell, horizontal640x384 canvas with tabletop512x256 and apron512x36; vertical384x640 with tabletop256x512 and apron256x36. Both have64px top/left padding. Exterior contour8sourcepx in#171412; final map canvases160x96 or96x160; one connected foreground and zero holes. Transparent RGB is zero; no unused Stuff masks are created for Cutout. Local processing removes chroma, normalizes measured body bounds, cleans silhouette spill and adds only the approved external contour.

Base H top bands N124.413/E125.618/S124.043/W124.228, apron/top ratios .676/.680/.691/.673. Alternate F bands N122.389/E122.823/S121.900/W121.082, ratios .710/.711/.719/.716. All meet the unchanged90–140 and.45–.85 regression limits. Original eight sprites intentionally failed the focused tests before generation. Base F had passed visual review but slightly exceeded the numeric top maximum, and its West board encroached on the sampled strip; G/H correct both. Earlier alternate edits corrected South rotation and E/W size, then removed accumulated green tint and oversaturated red.

Fresh native placement and variation at noon beside original Core stove/butcher passed on package B357FF646B8080BA30FA0AC7FADBD5DBFDCCFE7B848C17E885DE0326EEF3EF57. Root inspected all46 captures, including three zooms and supporting55%/15%health. Blind review scored each of24 clean views9/10 for quality and fit; see ignored review/meat-live-a.md for exact-process evidence. No gameplay, Def, recipe, cost, research, dependency, shader or footprint changes are made.

The exact generation history follows; raw responses and rejected candidates remain under ignored artifacts.

## meat-four-prompt-a.txt

Generate original RimWorld fixed-camera game sprite art: one FOUR CARDINAL sheet of a 2x1 MEAT PREPARATION WORKSTATION. Reference roles: sauce four-card sheet is ONLY the measured counter geometry, card composition and neutral gray material target; original meat north sprite is ONLY the equipment inventory; Core electric stove and butcher table are ONLY vanilla painting and kitchen style references. Repaint original art, do not copy reference pixels. Replace all sauce tools with meat tools.

Use exactly the same four-card arrangement as the sauce sheet: NORTH upper-left and SOUTH lower-left horizontal; EAST upper-right and WEST lower-right vertical. Same world size in every orientation. Fixed high RimWorld map camera; all bodies axis aligned, no isometric diagonals. Each separately rendered view is the same physical workstation rotated on the map, with painted depth always on SCREEN BOTTOM. Target sheet 1280x1280 with roomy isolated quadrants on solid #ff00ff.

Measured final geometry at 256 px/cell: N/S canvas640x384, tabletop512x256, shallow apron512x36 below; E/W canvas384x640, tabletop256x512, shallow apron256x36 below. No cabinet drawers, tall front, legs, deep raised perimeter, or outside shadow. Gray top target RGB roughly(130,130,131), apron(82,82,83), outline charcoal(23,20,18). Broad quiet matte shading like vanilla electric stove; no olive tint, bright pale rim, gloss, chipped texture or ornamental bevel. Thin dark outer contour; subtler internal charcoal edges.

Exact inventory: ONE muted brick-red rectangular cutting board, ONE rectangular covered stainless pan with a small lid handle, ONE meat cleaver with a broad gray blade and brown handle lying beside the pan, and ONE small low weighing scale with a square gray platform above a dark simple display on its screen-bottom-facing casing. No meat, food, stains, labels, text, extra knives, pots, jars, taps, sinks or invented machinery. NORTH left-to-right groups board, covered pan, cleaver, scale. SOUTH reverses to scale, cleaver, pan, board. EAST top-to-bottom board, pan, cleaver, scale; WEST top-to-bottom scale, cleaver, pan, board. N cleaver brown handle screen-bottom and blade screen-top; E handle left/blade right; S handle top/blade bottom; W handle right/blade left. Pan handle axis horizontal N/S, vertical E/W; redraw its depth from the fixed camera. Same equipment physical dimensions across directions, with plane dimensions swapping when rotated; all painted side thickness stays down.

At final package scale use approximate top-plane sizes: board156x170, pan132x158, cleaver32x132, scale80x96. Keep a clear narrow band of exposed countertop around all equipment and 18px between groups; fit each without collisions. Red board broad single calm red mass with just a thin darker edge; pan lid flat medium gray; scale simple recognizable tiny kitchen platform scale, no glowing electronics or readable marks. Do not turn the tabletop into four deep compartments. Low equipment depth less than15% of its smaller dimension. The central counter and each tool should feel useful and legible at vanilla map zoom, with sparse functional details. Background perfectly uniform magenta, generous empty spacing, no floor, shadow, typography, grid, scene or frame.

## meat-four-prompt-b.txt

Edit this four-card RimWorld meat workstation sheet. Preserve all four bench bodies, tabletop/apron geometry, gray/red palette and exact inventory. Correct two issues:

1. The kitchen scale's control pad must rotate with the furniture in world space. NORTH upper-left scale is correct: square raised weighing platform and dark display pad at bottom. EAST upper-right: square platform to the RIGHT of its short dark display pad (pad on LEFT), making the overall scale footprint horizontal. SOUTH lower-left: display pad ABOVE platform. WEST lower-right: display pad RIGHT of platform, overall scale footprint horizontal. Keep all sides/depth of platform and base painted down toward screen bottom, even where the control pad is above/left/right. It is a low tabletop appliance, not a wall-mounted control. Same platform dimensions in all cards. No digits or text.

2. ALL equipment in the vertical EAST/WEST cards is drawn bigger than its north/south counterpart. Redraw the E/W tool groups at matching physical scale, leaving more exposed gray countertop around them. Specifically reduce E/W board width by about20% with height unchanged; reduce E/W covered pan size about10%; reduce the E/W cleaver as a whole about12%; scale the redesigned E/W weighing platform about15% smaller than current before placing its left/right control pad. At normalized64px/cell map scale, the N/S board is approximately34px wide by35px tall; E/W must be about35px wide by34px tall, not42px wide. Apply equivalent rotated matching to the covered pan and blade. Keep each group's existing center and order unless a few pixels are needed for the scale's sideways footprint. Never shrink the bench.

N/S board, pan and cleaver sizes are the scale reference and must stay unchanged. Keep NORTH left-to-right board/pan/cleaver/scale and SOUTH scale/cleaver/pan/board. EAST top-to-bottom board/pan/cleaver/scale and WEST scale/cleaver/pan/board. Cleaver handle direction remains Ndown, Eleft, Sup, Wright. Pan handle axis remains horizontalN/S, verticalE/W. The visible shallow table apron remains screen-bottom in every card. No cabinets, legs, new parts, grime, labels or outside shadows. Preserve pure #ff00ff background, four card positions and sheet size.

## meat-four-prompt-c.txt

Edit this four-card RimWorld meat-workstation sheet with a precise consistency correction. Preserve the complete NORTH upper-left and SOUTH lower-left cards. Preserve the bench bodies, gray palette, cleavers and weighing scales in all four cards; their controls and handles now rotate correctly.

Only repaint the red cutting boards and covered pans in the EAST upper-right and WEST lower-right cards. Their top surfaces are currently squashed along the vertical long axis of the bench. A world rotation must not change their physical size.
- EAST red board: keep its current width and center, increase the top surface's vertical extent by about 30%, so it becomes almost square like the north/south board. Keep the shallow dark thickness separately painted towards screen bottom.
- WEST red board: match exactly the corrected EAST board footprint; reduce its width about 3% if needed and extend its top surface vertically about 25%. It must be the same board, without changing the bench.
- BOTH E/W covered pans: keep the current width and center, increase the lid's top-surface vertical extent by about 20%, to match the rotated north/south pan footprint. Retain the vertical handle axis, same knobless bar handle, restrained rim, and shallow screen-bottom depth. Do not uniformly enlarge the whole pan or turn it into a bulky cabinet.

Keep the clear gaps between all equipment. N/E order board, pan, cleaver, scale; S/W reverse. One covered pan only per card. No added details, grime, legs, text, perspective skew or new objects. The tabletop apron and all equipment depth stay screen-bottom in every card. Preserve the pure #ff00ff background, exact sheet size and four card positions.

## meat-four-prompt-d.txt

Revise this four-card meat-preparation workstation sprite sheet for vanilla RimWorld. Keep each bench body exactly the same size, shape, position, medium-gray top and shallow screen-bottom apron, with no added legs or cabinets. Keep the equipment inventory and world rotation order.

Redraw the cutting boards as the SAME square red board at the SAME physical size across all four cards. Their TOP faces must be square, not short horizontal rectangles. Board side length is just over HALF of the narrow tabletop width in EVERY card: about 53% of the short edge. In NORTH/SOUTH this means about one quarter of the long tabletop edge; in EAST/WEST it means just over half of the table width. Give the board a subtle 5-degree clockwise turn on the tabletop in each card to make its placement less rigid. Because its top is a plain square, a quarter-turn produces the same visible square angle; repaint depth towards screen bottom. Muted brick-red, one quiet rim, no elaborate texture or stains. Keep shallow board thickness outside the square top surface, always below it on screen.

Redraw the covered metal pans with the SAME square lid top surface across all cards. Lid side length is 45% of the narrow tabletop edge; retain only a small rim and a shallow body beneath the lid. North/south handle horizontal; east/west handle vertical. E/W lids must be square and not compressed in height. Do not make a deep container. Pan, board and remaining tools stay separated with visible gray margins.

Keep the weighing scales unchanged: display bottom in NORTH, left EAST, top SOUTH, right WEST, with fixed screen-bottom depth. Keep the cleaver sizes/directions, but simplify each handle to TWO small plain muted rivets, removing the decorative three bright diamonds. N cleaver handle down, E left, S up, W right.

N upper-left left-to-right board/pan/cleaver/scale. E upper-right top-to-bottom board/pan/cleaver/scale. S lower-left reverse; W lower-right reverse. Exactly one board, one covered pan, one cleaver, one scale per card. Match simple painted vanilla forms: broad quiet fills, dark clean contour, no glossy stacked rims or fussy micro-details. Keep every part within the tabletop. Preserve pure #ff00ff background, four card centers, original sheet dimensions.

## meat-four-prompt-e.txt

Edit this RimWorld four-card meat station sprite sheet. Preserve the successful bench geometry, board sizes/angles, equipment order, scale displays and cleaver directions.

Correct the covered pans:
- On the EAST upper-right bench, enlarge the pan vertically without widening it. In this 1254px source sheet, its lid currently spans roughly y232–330; redraw that lid from about y215 to y344, keeping its current width and centered vertical bar handle. Extend its shallow body below the lid only. Move the red board about15px upward if necessary to keep a clear gray gap. Leave the cleaver below the pan separate.
- On the WEST lower-right bench, similarly enlarge the lid vertically to about130px high at its existing width, extending its top and bottom edges rather than simply adding deeper body. Its top must be a broad nearly square surface like the north/south pan. Keep gray gaps to the cleaver above and board below; shift the board downward at most8px if necessary. Fixed screen-bottom depth.
- Across ALL FOUR pans, remove the bright cream/white inner rim. Use a restrained medium-gray lid and a single narrow dark boundary, with only one subtle gray edge highlight. The lid must read as plain utilitarian kitchen steel, with no luminous frame, elaborate bevel or stacked polished layers. Keep each lid's bar handle and one shallow lower body edge.

Across ALL FOUR cleavers, replace the THREE bright DIAMONDS on the brown handle with exactly TWO small dull-gray circular rivets. This must be an actual visible change: no third mark, no white diamonds, no dotted stripe. Preserve cleaver blade and handle size/orientation.

Do not alter the red-board top shapes, weighing scales, tabletop/apron geometry or card positions beyond the small spacing shifts above. Every card keeps one red board, one covered pan, one separate cleaver, one scale. All furniture and equipment depth stays toward screen bottom in every orientation. Keep original sheet size, pure #ff00ff background, no text, no new objects.

## meat-four-prompt-f.txt

Make a small directional-size correction to this four-card RimWorld meat station sheet. NORTH upper-left and SOUTH lower-left cards are final: preserve them unchanged. Preserve all bench bodies, neutral gray/red colors, camera projection, apron, weighing scales, cleavers and their two round pins.

Only in EAST upper-right and WEST lower-right:
1. EAST pan lid is now a little too tall. Reduce ONLY its top-surface vertical extent by about12%, keeping current width and center. The resulting lid top should read nearly square, matching the N/S lid top surface at the same physical scale. Keep the vertical handle and a shallow screen-bottom body edge.
2. WEST pan lid: reduce top-surface vertical extent about6%, same width/center, to match the corrected EAST pan.
3. EAST red-board top: increase its vertical extent about15% without widening it. It is a mildly clockwise-angled SQUARE board like north/south, with the dark thickness added below, not counted as part of the square surface.
4. WEST red-board top: reduce width about5%, increase vertical extent about10%, to match the corrected EAST square board. Keep the same mild clockwise angle and screen-bottom thickness.

Adjust spacing by a few pixels if needed so each object has a clear gray gap; do not move equipment outside its tabletop or resize the bench. Preserve order N/E board-pan-cleaver-scale and reverse S/W; scales still display bottom/left/top/right respectively and cleaver handles down/left/up/right. No added marks or parts, no text, no palette changes, no new highlights, no restored diamond rivets. Keep the original four-card positions, 1254px sheet size and pure #ff00ff background.

## meat-four-prompt-g.txt

FIRST image is the edit target: four cardinal meat counters with separate cleavers. SECOND image is a material reference only. Preserve the exact four-card sheet arrangement and each counter silhouette/footprint, with axis-aligned tabletop and shallow screen-bottom apron. Repaint only the materials into vanilla RimWorld matte values. Bare tabletop medium neutral gray centered nearRGB(125,126,125), broad quiet shading fromabout115near edges to130nearcenter. Apron dark neutral gray nearRGB(77,78,77). Remove green/cyan casts: red/green/blue channels of metal should be withinabout5levels. Pan lids and scale platforms subdued neutral gray nearRGB(155,156,152), charcoal outlines and a single muted gray rim, no polished white shine. Red boards muted brick red aroundRGB(190,77,44), lower saturation and no bright orange/red glow. Match the second image's red board and metal hue, but use the explicitly darker tabletop/apron values requested here. Preserve all existing equipment shapes, sizes, orientations, handles, counts and positions except any single placement correction explicitly specified. No extra parts, text, food, blood, external shadows or changes to solid #ff00ff background. Depth always screen-bottom.
One placement correction ONLY in WEST lower-right card: move its ENTIRE equipment stack (scale, separate cleaver, pan and red board together) upward20pixels relative to a256pixel-wide vertical tabletop. Do not scale or rotate it. This preserves all inter-equipment gaps while opening a clean strip of counter below the red board. Leave every other card's tool positions unchanged. Keep exactlytwo round pins on each separate cleaver. Scale display edges remain bottom/left/top/right forN/E/S/W; handle directionsdown/left/up/right. No geometry changes beyond the specified West stack translation.

## meat-four-prompt-h.txt

Restore one missing edge on ONLY the lower-right WEST sprite in this four-card RimWorld counter image. Its low dark apron has disappeared. Paint a straight horizontal dark neutral-gray apron band across the bottommost33pixels INSIDE that lower-right counter (approximatelysheet y1166 through1198 at current1254image size). This is the same shallow screen-bottom front edge as on the upper-right counter. Match its color nearRGB77,78,77 and its36px depth relative to a256px-wide normalized tabletop. Keep the exterior black contour and magenta background unchanged; do not change the counter's outer bounds.

Preserve every equipment item, ALL positions and sizes, the upward-shifted West equipment stack, board and cleaver shapes and rotations, pan and scale details, neutralgray tabletop, palette, and the other THREE cards completely unchanged. No other edits. Do not add legs, cabinet doors, text or outside shadow.

## meat-alternate-prompt-a.txt

Use case: precise-object-edit. Generate four separately rendered cardinal game sprites for the ALTERNATE MEAT PREPARATION WORKSTATION. Image1 is the geometry, simple material finish and sheet layout reference. Image2 is the old alternate equipment inventory ONLY: repaint original art, do not copy old cabinet or reference pixels. Keep Image1's exact four counter rectangles and fixed-camera depth: North upper-left, East upper-right, South lower-left, West lower-right on uniform #ff00ff.

Replace each equipment set with exactly THREE groups: a larger muted brick-red square cutting board with ONE meat cleaver lying diagonally ON it; ONE covered stainless pan with a simple raised bar handle; ONE small weighing scale with gray square platform and plain dark control panel. No separate cleaver, no meat/blood/food, extra parts, text or numbers.

At normalized 256 pixels per world cell: horizontal tabletop512x256 plus screen-bottom apron512x36; vertical tabletop256x512 plus screen-bottom apron256x36. No east-side extrusion. The board top is200x200 in all views, pan lid116x116, scale platform80x80 with a small control strip along one edge. Those are world-plane dimensions: keep same physical sizes in all four views. Board and pan stay square; do not compress the vertical cards' equipment. Shallow equipment thickness always projects screen-down (about8–12pixels), regardless of which edge holds a handle or control. Exposed clear countertop and gaps between groups, no overlap except cleaver lying on board.

North left-to-right: board+cleaver, pan, scale. East top-to-bottom: board+cleaver, pan, scale. South left-to-right: scale, pan, board+cleaver. West top-to-bottom: scale, pan, board+cleaver. Cleaver's brown handle points SOUTHWEST in North, NORTHWEST in East, NORTHEAST in South, SOUTHEAST in West, with blade opposite; exactly two round metal pins in each handle. Pan bar handle horizontal in North/South, vertical in East/West. Scale control strip faces screen-BOTTOM in North, LEFT in East, TOP in South, RIGHT in West; the scale body still extrudes down from the fixed camera in every card.

Match Image1's broad quiet medium neutral-gray counter and darker shallow apron, matte gray cookware with restrained charcoal contours and one muted gray rim. More vanilla RimWorld hand-painted flat shapes, not glossy 3D. Red board has simple flat low-detail paint and a thin dark edge; no woodgrain or fine scratches. Preserve all four silhouettes, projected depth and generous magenta gutters. No tall drawers, cabinet fronts, legs, external shadows, scene, background texture, typography or frame. Output one square four-card sheet with the same exact placement as Image1.

## meat-alternate-prompt-b.txt

Edit this four-card RimWorld meat workstation sheet, preserving all counter silhouettes, palette, card positions and fixed-camera screen-bottom depth. Correct only equipment scale and cleaver details.

The red cutting board must be the clearly largest top-plane object in every view: enlarge each red board about30percent, keeping it inside the counter with a clear margin. Reposition the three groups slightly only as needed for separation. Keep each board square and the same physical dimensions across all cards. It should be about1.5times the covered pan's lid width. The cleaver can keep its current size on the larger board.

NORTH upper-left and SOUTH lower-left currently have oversized pans and scales relative to EAST upper-right and WEST lower-right. Reduce only North/South pan lids and bodies by10percent and their entire scales by18percent. Match the East/West physical size of these same objects, with the correct square top planes. No widening or shortening the bench itself. Every view has precisely one large red board+one cleaver, one covered pan, one scale.

Correct SOUTH lower-left cleaver: brown handle points UP-RIGHT/NORTHEAST and the broad blade points DOWN-LEFT/SOUTHWEST. Preserve North handleDOWN-LEFT, EasthandleUP-LEFT, WesthandleDOWN-RIGHT. In all FOUR brown cleaver handles use exactlyTWO simple round silver pins, not three. Preserve scale control edges BOTTOM/LEFT/TOP/RIGHT in N/E/S/W and pan bar handle horizontal inN/S,vertical inE/W. All object side thickness still projects down.

Use subdued matte gray metal and muted red board, remove conspicuous white polished rims in favor of a single soft gray edge. No added texture or objects. No text, shadow, background scene or floor. Solid #ff00ff exterior unchanged.

## meat-alternate-prompt-c.txt

Make one small measured correction to this four-card RimWorld workstation sprite sheet. ONLY in the EAST upper-right and WEST lower-right vertical counters, reduce the red cutting board including its cleaver by7percent in width and height around its center. Also reduce the covered pan lid/body by7percent around its center. Keep their shallow painted side thickness directed screen-bottom. These tools must match the physical size of those in NORTH upper-left and SOUTH lower-left.

Keep BOTH horizontal cards completely unchanged. Keep all four counter silhouettes, tabletop/apron values and geometry, exact card positions, palettes, outer outlines, scale platforms/control panels and all their dimensions completely unchanged. Do not move or rotate groups. Preserve one cleaver on each board with exactly THREE small round handle pins as currently shown. Preserve handle directions SW/NW/NE/SE for N/E/S/W and scale controls bottom/left/top/right. Pan handles horizontal N/S and vertical E/W. No new equipment, text, ground shadow or background change. Solid #ff00ff exterior. This is only a7percent correction to the boards+cleavers and pans in the two RIGHTHAND cards; no other changes.

## meat-alternate-prompt-d.txt

Correct only the TWO RIGHT-HAND vertical workstation cards in the FIRST image. SECOND image is the exact earlier cleaver-size reference for those right-hand cards. All four counter bodies and the TWO LEFT horizontal cards in FIRST image must stay unchanged.

In FIRST image's upper-right EAST and lower-right WEST: enlarge only each RED BOARD by9percent around its center, keeping it square. Enlarge its CLEAVER separately by25percent around its center, restoring the cleaver physical size seen in SECOND image's corresponding right-hand card. Keep cleaver fully on board, preserve existing diagonal directions (East handleupperleft, West handlelowerright) and THREE round pins. Do not enlarge the whole equipment group uniformly: board+9%, cleaver+25%.

Also reduce each right-hand COVERED PAN by6percent in width and height around its center, keeping the square top and bar handle vertical, shallow depth downward. Do not move any scale or change its size/control edge. Keep all material colors, highlights, thin outline, parts, exact counter silhouette, apron depth, same 2x1 world proportions and magenta background. North upper-left and South lower-left pixel appearance unchanged. No new parts, text, floor or external shadows.

## meat-alternate-prompt-e.txt

Edit ONLY the red cutting boards and their cleavers in the two RIGHT-HAND cards of this four-card RimWorld counter sheet. The last adjustment made them slightly too wide. Keep both LEFT cards completely unchanged, and keep ALL countertops, aprons, pans and scales unchanged.

EAST upper-right board: reduce width8percent and height4percent around the same center. EAST cleaver: reduce whole diagonal silhouette5percent around same center, preserving its angle and handleUP-LEFT.
WEST lower-right board: reduce width9percent and height6percent around the same center. WEST cleaver: reduce whole diagonal silhouette9percent around same center, preserving its angle and handleDOWN-RIGHT.
The resulting red board top-plane should measure about154x154 pixels on a normalized256px-wide vertical tabletop (about60percent of the table width); both vertical boards should look the same world size as horizontal boards. Keep board depth as a very shallow screen-bottom dark band. Do not compress the tabletop or pan.

Keep three round silver handle pins, correct blade shape, quiet red paint, neutral-gray cookware, scale panels and all colors as closely as possible. Keep the exact sheet layout and solid #ff00ff outside objects. No new parts, text, scenery, floor, external shadows, or other changes.

## meat-alternate-prompt-f.txt

FIRST image is the edit target: four cardinal meat counters with cleavers ON the boards. SECOND image is only the BASE palette reference: do not copy its smaller empty boards or separate cleavers. Preserve the exact four-card sheet arrangement and each counter silhouette/footprint, with axis-aligned tabletop and shallow screen-bottom apron. Repaint only the materials into vanilla RimWorld matte values. Bare tabletop medium neutral gray centered nearRGB(125,126,125), broad quiet shading fromabout115near edges to130nearcenter. Apron dark neutral gray nearRGB(77,78,77). Remove green/cyan casts: red/green/blue channels of metal should be withinabout5levels. Pan lids and scale platforms subdued neutral gray nearRGB(155,156,152), charcoal outlines and a single muted gray rim, no polished white shine. Red boards muted brick red aroundRGB(190,77,44), lower saturation and no bright orange/red glow. Match the second image's red board and metal hue, but use the explicitly darker tabletop/apron values requested here. Preserve all existing equipment shapes, sizes, orientations, handles, counts and positions except any single placement correction explicitly specified. No extra parts, text, food, blood, external shadows or changes to solid #ff00ff background. Depth always screen-bottom.
Preserve ALL current corrected sizes and positions in FIRST image, including both vertical boards, their cleavers, pans and scale geometry. Do not resize or shift any equipment. Each board has ONE diagonal cleaver, with THREE small round handle pins. Handle directionsSW/NW/NE/SE forN/E/S/W; scale displaybottom/left/top/right. This is a palette-only correction across all four cards.
