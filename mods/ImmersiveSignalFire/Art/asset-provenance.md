# Immersive Signal Fire asset provenance

The packaged textures are original generated-and-normalized assets. Workshop and Core material was used only as read-only scale and silhouette reference; none was copied into the package.

## Single-use wood-stack revision brief (written before generation)

The prior selected stone-ring/triangular-rack building is rejected by the revised player direction. It is too detailed, too gradient-heavy, visibly stone-dominant, and reads as a permanent hearth rather than a prepared single-use signal stack.

Read-only fixed-camera references were compared on both charcoal and pale terrain at the same 256-pixel contact scale under `artifacts/AssetGeneration/ImmersiveSignalFire/SingleUseRevision`:

- old Tribal Signal Fire `SignalFire.png`: 128×128 RGBA, alpha bounds `120x78+4+50`, 2,550 source colors, SHA-256 `7F25D0A424E8A3078E9747D6A84A26E4A83FF4F76621B0101AAF6B894FEF5A47`; its broad low four-log silhouette and heavy contour remain readable, but its exact pixels and composition are not reused;
- Core `Table2x2_north.png`: 256×256 RGBA, alpha bounds `128x143+64+64`, 106 colors, SHA-256 `D83DB3DD4E38289ABF391745379544388AC3A9E4D83044A7BD7CBB8B7A592CA7`; comparator for a centered 2×2 footprint and extremely restrained value groups;
- Core `TableStoveFueled_north.png`: 224×96 RGBA, alpha bounds `192x76+16+13`, 982 colors, SHA-256 `20272AFEBB975B24B94153F8C29A766B7656F2F97919FD23AE7B4FA3D0D32EF2`; comparator for a low fixed-camera work surface and legible broad parts;
- Core `TableButcher_north.png`: 224×96 RGBA, alpha bounds `192x73+16+16`, 1,176 colors, SHA-256 `C64F44063D45F4AE465EA3DCC33A453DB2631D98DC021C4E38872D29D68CCEE8`; comparator for simple top/near-face separation at ordinary zoom.

Constant generation brief for the replacement candidates:

> One original RimWorld-style map sprite on an exact 512×512 transparent RGBA canvas for a non-rotating 2×2 building with `(2.35,2.35)` draw size. Fixed orthographic high three-quarter map camera, centered alpha bounds approximately 396–430 pixels wide by 260–320 pixels tall. Depict one neat, deliberate, single-use signal pyre: ten to fourteen short logs stacked in three low, orderly alternating criss-cross layers around one clear central emission gap. Most logs are freshly cut pale yellow-green/sage wood; a smaller number are medium warm brown seasoned wood; cut ends are light and plainly visible. Use a chunky near-black exterior contour, broad matte shapes, no more than four authored tones per material, restrained cel-like shading, and very little bark or grain detail. Preserve a clean silhouette at 64 pixels and ordinary RimWorld map zoom. No stones, rock ring, soil basin, ash, charcoal, tripod, teepee poles, rope, cloth, pawn, tools, flame, embers, glow, smoke, ground, cast shadow, text, label, or watermark. No photorealism, high-frequency texture, glossy rendering, soft airbrushed gradients, or background. Return exactly one isolated object with generous even transparent padding.

The pre-generation height estimate was amended after the first equal-scale contact sheet: the candidate's `346`-pixel height was retained instead of vertically compressing the deliberately chunky three-layer stack. The reviewed measurable height band is therefore `320–370` pixels; width, palette, material, silhouette, and padding gates were unchanged. This amendment is recorded explicitly rather than treating the original estimate as if it had passed unchanged.

Measurable acceptance: strict 8-bit RGBA; transparent corners and black invisible RGB; alpha bounds `396–430` pixels wide by the amended `320–370` pixels tall; at least ten visibly separate log bodies; pale/green fresh wood is the majority visible material and brown wood remains a clear minority accent; no stone-like detached perimeter pieces; normalized opaque RGB palette at most 48 colors; dark exterior contour survives a 64-pixel preview; the object remains identifiable as a deliberately prepared burn stack at close, ordinary, and far useful in-game zoom.

## Reference measurements

- Installed Tribal Signal Fire reference: `Textures/SignalFire.png`, 128×128 RGBA, SHA-256 `7F25D0A424E8A3078E9747D6A84A26E4A83FF4F76621B0101AAF6B894FEF5A47`, Def draw size 1.5 over a 2×2 footprint. It is a small four-log silhouette intended to receive vanilla flame overlays.
- Core Campfire Def: single unrotated sprite, `PassThroughOnly`, 0.20 fill, 80 hit points, 200 work. Its texture is packed in the installed game data and was not extracted.
- Royalty Brazier Def: low 0.15-fill fire furnishing, 80 hit points, 1×1. Its texture is packed in the installed game data and was not extracted.
- New target: one 512×512 8-bit RGBA source on a 2×2 footprint, 2.35 draw size, with enough cold-hearth detail to remain legible at an approximately 180-pixel final-scale comparison.

## Historical generated candidates and rejected selection

Built-in image generation produced two independent cold-hearth candidates on 2026-08-27:

- A, radial logs in a circular stone ring: source SHA-256 `440ECF885D826D1DD5A451ACBAFB17ED3AAEB2B3F1C1B2F8D7CEB924318667E8`.
- B, sparse stone hearth with a tied triangular log rack: source SHA-256 `08AB0F5B437A8B2B035E7A263484E41C98C19989A3EE778958F0B25DD662790D`.

Both were trimmed, scaled to the same 410-pixel maximum subject dimension, centered on 512×512 transparent canvases, and compared at 180 pixels on light and dark terrain. Candidate B was historically selected because its open triangular signal rack remained distinct from a normal campfire. That selection is superseded and rejected by the single-use wood-stack revision above. The historical comparison sheet remains at `artifacts/AssetGeneration/ImmersiveSignalFire/contact-sheet.png`.

Final generation prompt, normalized for the selected building:

> Original cold tribal signal-fire hearth for a 2×2 colony-simulation map footprint; fixed slightly tilted top-down camera; sparse dark fieldstone perimeter, three thick weathered logs tied as a triangular rack above a broad soot-blackened basin, small kindling bundle; hand-painted chunky matte forms; completely unlit; no flame, embers, glow, smoke, people, tools, text, shadow, or watermark; isolated background for alpha removal.

## Single-use revision candidates and selection

Built-in image generation produced two independent candidates from the constant brief above and one targeted lower-profile follow-up on 2026-08-27. Raw and normalized files plus light/dark 256-, 180-, and 64-pixel comparisons remain ignored under `artifacts/AssetGeneration/ImmersiveSignalFire/SingleUseRevision`.

- Candidate A source: 1,254×1,254 RGBA, SHA-256 `504BF02550FA4C00DB5641FF815885B0294E915969E8FD328A5082F27B3D019B`; orderly three-layer criss-cross stack with a clear center and a strong pale/green majority.
- Candidate B source: 1,254×1,254 RGBA, SHA-256 `116226D5621CFF5450C7A9D97195FD59116AA3AD58591BC781C2BC3246D19CEC`; rejected because crowded end-caps and overlaps make it visually busier at ordinary and 64-pixel scales.
- Candidate C source: 1,470×1,070 RGBA, SHA-256 `7CCBEAF624BF88B7B04F3734468FD678936926C4DEF97219C891AA91C4EBDC44`; targeted lower-profile follow-up, rejected because its sparse flat grid reads more like a log foundation than a prepared pyre and gives brown wood too much visual weight.
- A targeted edit with SHA-256 `BA4D971E1EA74E0776B6D954651840FF4D24E17AA487D176C1EC3B6D5030C8DD` baked a checkerboard into an opaque RGB image and was rejected before normalization.

Candidate A was selected. It is centered without deformation on a 512×512 transparent canvas, normalized to 35 total colors and strict 8-bit RGBA, with alpha bounds `420x346+46+88`. Mechanical sampling over pixels with alpha at least 32 records 31 visible RGB colors and a 76.7% pale/green share among classified fresh-versus-brown wood pixels. On charcoal and pale contacts it keeps the central emission gap, organized log identity, green/brown separation, and near-black contour at 180 and 64 pixels. Final approval remains pending fresh in-game close/ordinary/far inspection and blind context-free review.

## Packaged textures

- `SignalFire.png`: 512×512 8-bit RGBA, SHA-256 `1FF1DC25DB541854D7393C942E4E430308C0ED87C77799864ABCAD7315B9BF5B`; selected single-use wood stack with alpha-zero RGB normalized to black, live approval pending.
- `DarkSmoke.png`: 128×128 8-bit RGBA, SHA-256 `57CAD10FDD420111D8FAA197C594E21C6F395DB134E4447E2904485B6CBB7DA8`; one neutral grayscale puff generated as a repeatable fleck, tinted dark by the effect Def, with alpha-zero RGB normalized to black.
- `Flame.png`: 128×128 8-bit RGBA, SHA-256 `1248A9EC0B9BD6F579FC38440FFDD7D73EBE3F9FCB6F14DD3FADE86A180B3696`, rendered from the repository-owned two-shape `flame-source.svg`; its visible pixels are white at two authored alpha levels plus raster antialiasing and its alpha-zero RGB is black, allowing Dynamic Effects Forge's glow tint to produce a simple flame without the stock grayscale flecks' dark source lobes. The source is intentionally flat and abstract, with no gradient or copied Workshop/Core pixels.
- `SignalBlanket.png`: 128×256 8-bit RGBA, SHA-256 `BA5BE7AC26ED6B54768FFD4238F928B0BFA6B37C42DCC38029BB4C622C60DACA`; chroma-key source processed with the installed image-generation skill's soft matte and despill helper, then normalized to black alpha-zero RGB.

`scripts/Build-ImmersiveSignalFirePreview.ps1` deterministically composites the promoted wood stack into the 640×360 About and Workshop card. Both outputs have SHA-256 `FA0D17C2B5E949947ABF4BC5A59E840266089E7BC25608F160650D526A24E473`; the player-facing subtitle explicitly identifies the ritual as single-use.

The exact final prompts used built-in image generation. The building and smoke outputs already contained transparent pixels; the blanket used the requested flat `#00ff00` key followed by local alpha removal. All three were normalized with ImageMagick to strict 8-bit RGBA.
