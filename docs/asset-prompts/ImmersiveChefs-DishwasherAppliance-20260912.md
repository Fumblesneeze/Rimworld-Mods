# Immersive Chefs domestic appliance candidates —2026-09-12

Owner: Immersive Chefs. Built-in imagegen; backend model unverified. Original Core and user-photo references are read-only inspection inputs and are not redistributed.

These eight one-cell candidates were tested in fresh native run20260912T000417259Z and rejected for ambiguous identity. Their earlier independent source craft/contour/coherence scores were9, with side/rear recognition8. The fresh context-free native reviewer instead read a cabinet/counter or generic appliance, scoring identity4/3/2 and surrounding-style fit8/8/7 at close/ordinary/far zoom. No visual acceptance is claimed. Retained evidence is artifacts/VisualAssets/DishwasherAppliances-20260911/review/domestic-native-exploration-root.md and artifacts/VisualReviews/20260912-SetV/review.md. The current development files remain candidates for redesign; previous two-cell and first-contour attempts remain rejected evidence.

Footprint1x1, drawSize1.5x1.5, RGBA384x384, fixed roof144pixels and casing176pixels at256pixels/cell. The local-south interaction,16capacity, cost/research/power/water and existing cosmetic owner stay unchanged.

Normalization: background-connected chroma removal, uniform whole-canvas resizing, partial-edge RGB despill, alpha-zero RGB cleanup, then clearance of only exterior-connected alpha<96 residue using the existing flood rule. All alpha>=96 RGBA and material samples remain unchanged. The8pixel contour preserves every resulting nontransparent source pixel; source and96px proxy topology1/0, baseline ring gates unchanged.

Exact raw/normalized inventories, geometry brief, two-background contacts and independent reviews are retained in ignored artifacts/VisualAssets/DishwasherAppliances-20260911. Approval manifests pin these development output identities with reviewState=rejected-native-identity.

| Packaged candidate | SHA-256 |
|---|---|
| Dishwasher_north.png | 5b7a94c9246ba06fe7a6aeec28f6ff2b9ace2279f4e03b8a9f9a8f550552ddc9 |
| Dishwasher_Variant01_north.png | 4c37a021b98aa1f0daa4ffc0f228d13997d97ffe77a83e2cb69d89530bb4a734 |
| Dishwasher_east.png | cb67eb0f5a9db191bc73a7d5138cbdcbb23266a295ee5ae2fd8be23f05dbefa5 |
| Dishwasher_Variant01_east.png | 3f7653383ba8723faa9a11c71de9417b533b9cf78546397a9ad26b1f870eb744 |
| Dishwasher_south.png | ee4babef2f85ed9c744e35a3bb5659914838b186f378dd0cc3e1b8edef58d76e |
| Dishwasher_Variant01_south.png | 61fd043ab2f4731152cc22e1481f95992f870318207f18b80eebc6049d6b465a |
| Dishwasher_west.png | 2da8212467b60a4ffc20d0c1a7325148af434463d1d890a4e53b1102deb73088 |
| Dishwasher_Variant01_west.png | 1a7e9104510356669bba453da0edeea3ef21ba74e4fb6043972e42e65349b08e |

## Exact selected-generation prompts

### domestic-north-prompt-f.txt

Prompt SHA-256: 815c6db1e10e43ed4acc77722af434a46d1557cda7ab450f43e1506ca3da85bf

```text
Use case: precise-object-edit. Edit the first image, a geometry guide for ONE vanilla RimWorld domestic dishwasher game sprite. KEEP THE ENTIRE GEOMETRY: trace every rectangle, gap, button, dial and outline at exactly the same relative positions. Do not redesign, rearrange, crop, reframe or change any proportions. Make it a finished but very simple matte painted sprite by subtly shading WITHIN the existing filled regions only. The second image supplies vanilla paint/line style but no geometry and no pixels.
The guide has a full roof, dark horizontal controls and a large CLOSED rectangular front door. Keep these boundaries exactly. The roof ends at y176/384 of the image; controls start at180/384 and end at212/384; the door starts at221/384 and ends at329/384. Do not move the control group upward or shorten the roof. Preserve the narrow gap between roof and controls and between controls and door. Roof width256/384, depth144/384; body front depth176/384. Whole silhouette spansx64–320,y32–352 on the384logical canvas. Keep4pixel intrinsic contour, fine2–3pixel seams and no extra bevel band across the roof. Single fixed high south-side RimWorld camera with all lines parallel to image axes.
Color only: roof broad mediumgrayRGB132,132,130; large front-door interior darker grayRGB104,104,102; cabinet trim104gray; dark control strip50gray; muted unlit pale buttons and knob. Very subtle paint variation, no gradients, shine, grain, dirt or white edges. Do not add text, decals, plates, dishes, basket, faucet, sink, glass or a round laundry door. No missing or duplicated parts. Keep the untouched background perfectly uniformflat#ff00ff. No shadow/floor. Return one entire square sprite, not a comparison sheet.
```

### domestic-alt-north-prompt-b.txt

Prompt SHA-256: faffca2018535506917f203c4940048d04025160999a3c4858b621c50a8ecebb

```text
Use case: precise-object-edit. Edit this exact RimWorld kitchen dishwasher sprite. Change only the warm-gray enamel on the lower closed door and its casing/frame: darken those enamel surfaces by about 9 percent, toward RGB102,99,90. Preserve the wood countertop completely, preserve all lines and controls, preserve exact component geometry and canvas. The large door must be noticeably darker than the wood top, with muted matte finish. No new detail, no different camera or shape. Same perfectly flat solid magenta #ff00ff background, no shadow or floor.
```

### domestic-east-prompt-i.txt

Prompt SHA-256: 10aeb70b05e3f0bcf9b9c3bfb2083d0eda2ec7d94637c5b5134452136f1abeca

```text
Use case: precise-object-edit.
Image1 is the target side-view sprite. Image2 is only a reference for the smooth gray material of the ROOF.
Remove the cloudy, blotchy pattern from ONLY image1's large top rectangle. Make this gray roof as quiet and smooth as the roof in image2: nearly flat muted medium gray, average RGB around(130,130,127), with only very subtle broad shading and its existing narrow bevel. No mottled grain or smoky patches.
Keep ALL geometry and all other pixels' appearance unchanged: same body position, exact roof/face seam, lower gray casing and pressed panel, left long trim, tiny attached left knob, plinth, exterior black outlines and magenta background. Do not copy front controls or door from image2. One whole sprite.
```

### domestic-alt-east-prompt-b.txt

Prompt SHA-256: 75f22ad0d47b30061528d5f9c995524614598a62e5f703315be0f200835b78f4

```text
Use case: precise-object-edit.
Edit ONLY the wood texture inside the roof of image1. Its many fine vertical stripes are too busy for RimWorld. Replace them with the much quieter wood appearance in image2: nearly flat muted brown with barely perceptible broad variation, no fine stripes, no plank lines, no knots. Keep image1's existing average roof brightness and brown hue.
Preserve every geometric landmark of image1: exact roof edges and bevel, full appliance body and lower warm-gray side panel, horizontal divider, tiny LEFT knob, left trim, bottom plinth and black outline. Do not move the knob to the right as in image2. Image2 is a wood-style sample only. Do not change the lower face. Uniform magenta background, same whole square canvas. No new parts, dishes, text or shadow.
```

### domestic-south-prompt-b.txt

Prompt SHA-256: d98a7045d75bf8217897e1bb35ef54751fb41322c7afbc2b468c04b9bb9481bf

```text
Use case: precise-object-edit.
Image1 is the target rear-view sprite. Image2 is only the reference for the ROOF material. Smooth ONLY image1's roof to match image2's nearly flat muted gray roof, averageRGB around(130,130,127). Remove cloudy mottling; keep only barely perceptible broad shading and the existing narrow edge bevel.
Preserve the exact target geometry, whole body, top/face dividing line, TWO rear ventilation slots, rear rectangular service cover, plinth, outer contour and magenta background. Do not add any front controls, knob or door from image2. The broad lower casing stays dark gray aroundRGB(108,108,105). No new parts, text, floor, shadow or dishes. One whole sprite.
```

### domestic-alt-south-prompt-a.txt

Prompt SHA-256: 29035c28055e088cad616af06727cbae8dda52890d7a2b469cd8f55b4a62c7f4

```text
Use case: precise-object-edit, material variant.
Image1 is the exact rear-view dishwasher to recolor. Image2 supplies only the selected wood-top/enamel palette. Preserve image1's geometry and all rear details.
Recolor the bare roof to the same simple subdued brown countertop as image2, almost flat with barely visible broad HORIZONTAL grain, averageRGB about(143,122,95). No fine stripes, distinct planks or strong wood pattern.
Recolor its lower casing to matching dark warm-gray enamel, averageRGB about(103,99,90), clearly darker than the roof. Leave the two rear ventilation slots and thin rectangular service-cover seam readable and muted.
Preserve all landmarks, outline, body proportions, horizontal roof/face junction and bottom plinth. There are no controls, knob, door pull or exposed cables on this rear. Do not copy those from image2. No dishes, text, floor or shadows. Same whole square canvas and perfectly uniform magenta background.
```

### domestic-west-prompt-d.txt

Prompt SHA-256: f63e4a866fb6e6032af5d1bbc9bbc7c6db1b22e33a034be2acd6dcf09b5dbc2d

```text
Use case: precise-object-edit.
Image1 is the target side-view sprite. Image2 is only the reference for the smooth gray material of the ROOF.
Remove the cloudy blotches from ONLY image1's large top rectangle. Match the quiet smooth roof in image2: nearly flat muted medium gray, average RGB around(130,130,127), with just very subtle broad shading and its existing narrow bevel. No mottled grain or smoky patches.
Keep all geometry and everything else unchanged: body position, roof/face dividing line, lower gray casing and pressed panel, right long trim, tiny attached right knob, plinth, black outlines, magenta background. Preserve the lower face's current dark gray value around106. Do not copy front controls or door from image2. One whole sprite.
```

### domestic-alt-west-prompt-a.txt

Prompt SHA-256: dde1f312b80f28876fbd597e147ed4120ade2ae0d0f6f0034a1e63c4fb4cfbd7

```text
Use case: precise-object-edit, material variant.
Image1 is the exact target side-view dishwasher; preserve its entire geometry and composition. Image2 supplies only the approved warm wood-countertop/enamel palette, not front-facing equipment.
Change image1's gray roof to the muted brown wood of image2. Keep it extremely simple and almost flat, with only one very faint broad suggestion of grain running vertically in this side view. No many fine stripes, planks, seams or pronounced wood pattern. AverageRGB about(143,122,95).
Change the lower gray casing to quiet warm-gray enamel, averageRGB(106,101,91), visibly darker than the top. Keep the side panel seam subtle.
All outlines and parts stay in their exact places: roof/face seam, closed full-height lower body, panel, plinth, narrow RIGHT edge trim and small RIGHT knob near the top. No front buttons/handle, no moved parts or changed proportions, no dishes, text, floor or shadow. Same whole square canvas and uniform magenta background.
```
