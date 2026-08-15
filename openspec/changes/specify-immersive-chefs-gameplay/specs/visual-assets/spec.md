## ADDED Requirements

**Owning mod:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Every custom Thing uses selected custom art
Every player-visible Immersive Chefs item and building SHALL resolve to a custom texture owned by this mod rather than a vanilla resource, weapon, worktable, stove, or laboratory placeholder. This includes ordinary and glitterworld cookware, plates including the fixed adobe path, cutlery, the chef's knife set, prepared ingredients, domestic and industrial dishwashers, ingredient prep station, sauce, meat, vegetable, and pastry stations, plus the fallback microwave only while its Def is active. Reuse of one selected base plate silhouette for Stuff-colored and fixed-adobe variants is allowed when their real material/color treatment remains visibly distinct.

Primitive stone cookware SHALL use its own selected silhouette rather than tinting the modern metal cookware sprite gray. It SHALL visibly read as a rough carved stone pot, shallow stone pan, and lids with minimal wooden handle/tool accents, while remaining Stuff-masked for every eligible stony material. The modern cookware family SHALL remain metal/plastic in construction and visually distinct at map and info-card scale.

When its fallback Def is active, the microwave artwork SHALL depict only a compact countertop appliance with no cabinet, legs, pedestal, full-height base, or baked counter surface. Its draw size and transparent padding SHALL visibly sit on a real table/workbench cell at `BuildingOnTop` without swallowing the support sprite, appearing to float, or obscuring adjacent workbench details and interaction cues. When `Mlie.DThermodynamicsHotMeals` is active, the Immersive Chefs microwave Def SHALL be removed before Def deserialization, its texture SHALL never be resolved or rendered, and Thermodynamics' own `DMicrowave` art remains untouched.

The fallback microwave's four cardinal sprites SHALL preserve the fixed RimWorld camera rather than rotate one raster. Its front door and controls SHALL have distinct cardinal faces: screen-top for `North`, screen-bottom for `South`, screen-right for `East`, and screen-left for `West`. The visual correction SHALL preserve the published Def interaction offset and saved rotation semantics. Workshop art that depicts the fallback microwave SHALL use the reviewed `South` frame, whose door faces the fixed camera.

#### Scenario: Countertop microwave is rendered on two supports
- **WHEN** the real Immersive Chefs fallback microwave Def is rendered on one dining-table cell and one production-workbench cell
- **THEN** it reads as the same compact appliance resting on each existing surface, with the underlying table/workbench still plainly visible and selectable

#### Scenario: The release package is built
- **WHEN** the finalized Defs and package contents are inspected
- **THEN** every listed custom Thing resolves to an existing selected texture below the Immersive Chefs texture namespace
- **THEN** none of those Defs points at its former vanilla placeholder

### Requirement: The mod has game and Workshop preview artwork
The release SHALL include `About/Preview.png` as a 640x360, 16:9 PNG below 1 MiB for RimWorld's mod list. It SHALL also retain a 1280x720, 16:9 Workshop preview master below 1 MiB in the release-source area, from which the About preview is deterministically downscaled. Both SHALL depict a recognizable angry celebrity-chef-inspired RimWorld pawn shouting through a speech bubble containing the exact text `YOU DONKEY!`, with a large, legible `IMMERSIVE CHEFS` banner. The bubble text's visible bounds SHALL be horizontally and vertically centered within the body of the speech bubble with balanced white space. The composition SHALL be an original RimWorld-style parody illustration rather than copied television photography or meme raster, and the exact title/bubble text SHALL be applied deterministically after image generation so spelling, centering, and safe margins are testable.

At least two generated compositions SHALL be compared at both final sizes. The selected image SHALL keep the pawn, speech bubble, banner, and kitchen context readable in RimWorld's mod list without cropping, tiny text, or misleading gameplay imagery. Rejected candidates SHALL remain in ignored evidence paths and only the selected preview files SHALL enter release sources/package.

#### Scenario: Browse Immersive Chefs in the mod list
- **WHEN** the player selects Immersive Chefs in RimWorld's native Mods screen
- **THEN** the 640x360 preview visibly shows the chef pawn, exact `YOU DONKEY!` bubble, and `IMMERSIVE CHEFS` banner without clipping or unreadable text
- **AND** `YOU DONKEY!` is centered horizontally and vertically in the speech-bubble body rather than crowding one edge

#### Scenario: Validate the Workshop preview source
- **WHEN** release package tests inspect the 1280x720 master and 640x360 About derivative
- **THEN** both are 16:9 PNG files below 1 MiB, share the selected composition, and contain the exact deterministic text within their safe areas

### Requirement: Every distinct asset is selected from multiple candidates
At least two distinct raster candidates SHALL be generated for every distinct item or building concept before selection. Candidate review SHALL record the prompt/variant identity, final-scale preview, selection outcome, and concise reason. Rejected candidates and review montages SHALL remain in ignored working/evidence paths and SHALL NOT enter the release package; the selected final and durable provenance/prompt record SHALL remain in the repository.

Before the custom-building art direction is finalized, one controlled representative dishwasher brief SHALL be sampled through the built-in image generator and every locally registered headless workflow available through the repository operator's `local-image-generation` skill: `flux2-nasa`, `flux2`, `krea2`, `realvisxl`, `juggernaut`, and `zimage`. The samples SHALL use the same functional brief, cardinal-sheet layout, output dimensions, and seed where the workflow supports it. They SHALL be chroma-removed and normalized to identical game canvases without otherwise correcting a model's composition. Selection SHALL be based on the resulting real-Def sprites rendered beside Core production benches at the same live map scale; a model SHALL NOT win merely because its full-resolution source is more polished.

#### Scenario: An agent selects a cookware sprite
- **WHEN** two or more cookware candidates have been generated
- **THEN** each is compared at the actual intended map/UI scale against a retained live RimWorld visual context
- **THEN** only the strongest readable, stylistically coherent candidate is wired into the shipped Def

#### Scenario: Local generator routes produce different dishwasher drafts
- **WHEN** the same representative brief has completed through the built-in generator and all six locally registered routes
- **THEN** every successful draft is normalized to the same directional canvases and rendered in one focused live comparison beside the same Core bench references
- **THEN** the retained decision records which route best matches RimWorld's map-scale style and why, plus any route failure, without silently substituting a different generator

### Requirement: Raster assets have clean game-ready silhouettes
Selected textures SHALL be PNGs with alpha, transparent corners, no chroma fringe, no baked floor/contact shadow, no text or watermark, and enough transparent padding to avoid cropping under selection brackets. Items SHALL remain recognizable at their intended 48–64 pixel review scale and buildings at their real footprint/draw size.

Every shipped Immersive Chefs Thing sprite SHALL also carry a deliberate dark comic-style exterior contour comparable to representative Core art at the same final raster density. `docs/SpriteOutlineBaseline.xml` SHALL record multiple portable item and production-bench textures read-only with exact game/source identity and extracted-image hash, without redistributing Core pixels. Measurement SHALL resize portable art to 64x64 with Pillow 12.2.0 `Image.resize(..., Image.Resampling.LANCZOS, box=fullCanvas, reducing_gap=None)`, whose RGBA path converts to premultiplied RGBa, applies its separable three-lobe/radius-3 filter to stored 8-bit sRGB values without linear-light conversion, and converts back to straight RGBA. Buildings SHALL be evaluated at 64 pixels per occupied map cell. The measurement SHALL classify foreground at alpha `>=96`; 4-neighbor flood-fill exterior background from every canvas-edge pixel below that threshold; define each successive inward ring as foreground 8-adjacent to the current exterior background before removing that ring and repeating the flood-fill; and classify a ring pixel as dark when Rec.709 luma is `<=80`. Enclosed holes are not exterior boundaries.

After the same final-scale resize, `portable-fine` art SHALL have at least `0.95` dark fraction in ring 1 and `0.40` in ring 2, `portable-broad` art SHALL have at least `0.95` in ring 1 and `0.75` in ring 2, and building art SHALL have at least `0.95` in rings 1, 2, and 3. The manifest SHALL classify chef's knives and cutlery as `portable-fine`; cookware, primitive/glitter cookware, plates, and prepared ingredients as `portable-broad`; and every appliance/station cardinal diffuse as `building`. Permitted added exterior expansion is three-quarters to one final pixel for fine art, one or two final pixels for broad portable art, and two or three final pixels for buildings, allowing two bounded candidates per class without accepting a subpixel hairline. The minimum fully transparent normalized-source clearance SHALL be one final pixel for portable art and two for buildings, matching the retained Core sample minima; Pillow's proxy fringe outside that normalized source bound is not mistaken for authored occupancy.

Before any product diffuse is processed, `docs/SpriteOutlineApprovals.xml` SHALL record its pre-outline hash and final-scale topology oracle: the exact count of 8-connected foreground components and 4-connected background holes whose area is at least two pixels, plus an explicit horizontal or vertical final-scale cross-section for every narrow semantic gap that a selected stroke could close. Each protected cross-section SHALL record a unique stable ID, its fixed coordinate, inclusive start/end coordinates, and the minimum consecutive run of pixels that must remain below alpha `96`; a single protected point is insufficient. When ordinary exterior dilation would still invade that approved gap or seal a tine opening after proxy resampling, the same sprite entry MAY add one or more reviewed source-space `sourceContourExclusion` rectangles with exact bounds, a semantic `reason`, and the exact protected-gap ID it serves. At normalized final scale the rectangle MUST intersect that referenced cross-section and remain wholly within the cross-section bounds expanded by four final pixels on each side, failing closed when the ID is absent, duplicated, unknown, spatially unrelated, or oversized. An exclusion suppresses only newly added contour pixels; it cannot erase or recolor source art, may cover only the local protected-gap/tine region named by its reason, and is not a general hand-painted exception mask. The outlined result SHALL preserve those component/hole counts exactly and every protected transparent run at or above its recorded width. The manifest SHALL record the selected stroke, output hash, post-outline ring fractions, and reviewed evidence. A deterministic processor MAY then apply the selected bounded stroke, but every added contour coordinate MUST have been fully transparent and part of the 4-connected canvas-edge exterior background before outlining. Enclosed holes MUST NOT receive contour, while an exterior-connected semantic gap MAY acquire bounded edge contour only if it preserves its approved minimum transparent run. The processor MUST preserve every pre-existing nontransparent source pixel; leave canvas dimensions and Def draw size unchanged; retain the class-specific minimum of fully transparent normalized-source pixels between the resulting nonzero-alpha bounds and every canvas edge; and preserve cardinal equipment identity and sanitation details. It MUST NOT merge distinct cookware/cutlery parts, close utensil tines or inter-part gaps, turn fine handles into blobs, or create an external cast shadow.

For every `CutoutComplex` diffuse, the outlined sibling mask SHALL have byte-for-byte equal alpha. Newly added contour pixels SHALL be fixed black in the mask rather than red/green Stuff regions, so wood, stone, steel, silver, and gold tint only the intended material surfaces while the outline stays dark. Every pre-existing nontransparent mask pixel and material classification SHALL remain byte-identical; formerly transparent pixels are exempt only where the processor adds the fixed-black contour with diffuse-matching alpha. Fixed-color/Cutout sprites SHALL gain no unnecessary mask. The exact Pillow resize is a deterministic package proxy for comparing candidates and detecting regressions, not a claim that Unity imports or samples textures with Pillow's kernel. Authoritative final-scale acceptance SHALL inspect captured live RimWorld rendering at the same tested camera zoom/import settings on both light and dark map contexts; full-resolution source inspection or the proxy metric alone is insufficient.

Every rotatable Immersive Chefs building, including every base appliance/station, the fallback microwave, and every optional VTEX building variant, SHALL use `Graphic_Multi` and provide valid `_north`, `_east`, `_south`, and `_west` textures. All four cardinal frames SHALL be separately authored or deliberately reframed views of the same physical building from RimWorld's fixed map camera; no opposite frame MAY be manufactured by rotating another raster. North/south SHALL fit the unrotated footprint, while east/west SHALL fit the rotated vertical footprint. Turning the building SHALL rotate its equipment and worker-facing layout in world space while the visible near-side vertical face, tabletop foreshortening, lighting, and fixed-camera perspective remain screen-consistent. The four views SHALL preserve recognizable equipment placement, visual mass, footprint occupancy, worker-facing orientation, and transparent padding. No rotatable custom building MAY use `Graphic_Single` as an escape from directional coverage.

Custom workbenches and appliances SHALL align with the visual language of representative Core production benches at map scale: painted rather than photorealistic rendering, an orthographic/top-down read, restrained highlights and micro-detail, a strong silhouette, bounded contrast, and comparable apparent height and density within the occupied cells. Candidate and final comparisons SHALL use the same live camera zoom, map lighting, footprint scale, and crop for the custom building and its vanilla comparator; a source render that looks polished in isolation but reads as glossy front-elevation concept art in game SHALL be rejected.

Directional workbench art SHALL be constrained by measured Core projection geometry rather than generated as four unrelated illustrations. The retained baseline SHALL sample multiple unpacked Core workbench `Texture2D` families and record the game version, canvas, alpha bounds, Def footprint/draw size, centered tabletop projection, and visible underframe depth without shipping any extracted Core raster. Immersive Chefs' 2x1 benches SHALL use the derived 2.5x1.5 canvas and its 3x1 benches SHALL use the Core 3.5x1.5 canvas. At 256 authored pixels per map cell, their horizontal frames SHALL therefore be exactly 640x384 and 896x384 pixels respectively, with rotated frames exactly 384x640 and 384x896. The projected tabletop SHALL occupy the centered footprint rectangle and the shallow visible underframe SHALL remain on the screen-south edge in every cardinal frame. North/south SHALL share the same horizontal table/body projection and east/west the same vertical table/body projection; only equipment placement, control orientation, and other world-facing details rotate. A cabinet/apron on the image top or long side, or four inconsistent apparent table heights, SHALL be rejected as an impossible camera perspective.

#### Scenario: Chroma removal damages an edge
- **WHEN** alpha inspection finds opaque corners, key-color residue, clipped geometry, or a halo at final scale
- **THEN** that candidate is corrected and revalidated or rejected before Def wiring

#### Scenario: A selected sprite has a soft generator edge
- **WHEN** the diffuse is downscaled to its intended map density beside a representative Core item or same-facing production bench
- **THEN** its exterior silhouette retains the selected Core-relative dark contour without swallowing intentional holes, gaps, handles, tines, or equipment separation
- **THEN** the same contour remains dark under materially distinct Stuff colors and on both light and dark terrain

#### Scenario: A Stuff mask follows an outlined diffuse
- **WHEN** a bounded outline adds alpha outside the prior material-bearing silhouette
- **THEN** the sibling mask gains exactly the same alpha at those pixels and classifies the added contour as fixed black
- **THEN** every coordinate that was already nontransparent in the diffuse or mask remains byte-identical, and only formerly transparent coordinates receiving the new contour are exempt

#### Scenario: A two-cell station is rotated east
- **WHEN** the player rotates the finalized station from north to east through the native placement or reinstall command
- **THEN** RimWorld resolves the station's authored east texture and vertical footprint instead of rotating a horizontal front elevation
- **THEN** the same tools, work surface, and worker-facing identity remain recognizable in the vertical view

#### Scenario: A station's interaction spot rotates to the north
- **WHEN** the player rotates a workbench so its interaction cell is north of the occupied footprint
- **THEN** RimWorld resolves the separately authored reverse-facing texture rather than a 180-degree transform of the opposite frame
- **THEN** tools and controls face the northern worker while the bench's visible near-side face and tabletop perspective remain consistent with the fixed map camera and nearby Core workbenches

#### Scenario: Directional sprites are normalized against measured Core benches
- **WHEN** the selected 2x1 and 3x1 workbench families are inspected in all four cardinal directions
- **THEN** their canvases, centered tabletop bounds, shallow underframe depth, transparent padding, and apparent height match the recorded Core-derived projection templates
- **THEN** every visible underframe remains on the screen-bottom edge while equipment rotates toward the interaction side
- **THEN** north/south and east/west no longer read as unrelated bench constructions or raster rotations

#### Scenario: A polished candidate conflicts with the vanilla map style
- **WHEN** the candidate is shown beside a Core stove, machining table, butcher table, or other representative production bench at identical map scale and lighting
- **THEN** excessive perspective, gloss, micro-detail, visual height, or transparent-padding mismatch keeps the candidate unselected even if its standalone render is attractive

### Requirement: Stuff-aware art preserves material identity with explicit masks
Stuffable cookware, plates, cutlery, and chef's knives SHALL use neutral value separation, a Stuff-compatible shader, and a matching mask texture for every selected diffuse path so RimWorld can color material-bearing surfaces from the Thing's actual Stuff while preserving outlines, highlights, handles, and other deliberately non-Stuff accents. Directional textures SHALL have matching directional masks. Fixed-material assets MAY use a deliberate base color and MAY omit a Stuff mask. The selected artwork SHALL remain legible and materially distinct for every eligible representative Core material: wood, stone, steel, silver, and gold, with optional registered materials checked in their exact active-mod groups.

#### Scenario: One plate is rendered from several materials
- **WHEN** wooden, granite, steel, silver, and gold versions use the selected plate texture where each is eligible
- **THEN** their live map sprites retain the same semantic plate silhouette while visibly reflecting their actual Stuff colors without muddy highlights or disappearing outlines

#### Scenario: A masked cookware set contains permanent handles
- **WHEN** the same masked cookware texture is rendered once from granite and once from steel
- **THEN** the pot, pan, and lid surfaces take their respective Stuff colors while the abstracted wooden handles and dark readable outlines remain deliberate non-Stuff accents

#### Scenario: Core steel kitchenware is compared at the same lighting
- **WHEN** a steel plate, cutlery setting, and cookware set are rendered beside representative Core steel Things at the same map zoom, terrain, lighting, and Stuff color
- **THEN** the intended metal-bearing surfaces read as comparably light steel rather than near-black or muddy gray
- **THEN** the matching `CutoutComplex` masks tint every intended metal surface while keeping only handles, sanitation details, highlights that are deliberately fixed, and the comic outline outside the Stuff region
- **THEN** no base Def silently ignores a packaged mask that is required for this material response

### Requirement: Chef's knives are simple at map scale
The chef's-knife set SHALL use a compact, low-detail silhouette that is immediately recognizable as a cook's personal knife roll at ordinary map zoom. Its visible blades, handles, roll, and straps SHALL use a small number of broad shapes rather than realistic micro-detail, tiny rivets, bevels, stitching, or specular texture that collapses under mipmapping. The exterior contour SHALL satisfy the `portable-fine` Core-relative outline contract without turning blade gaps or handles into a blob. Its Stuff mask SHALL tint the blade-bearing material regions deliberately while preserving the roll, handles, straps, and dark outline as fixed accents.

#### Scenario: Chef's knives are reviewed at 64 pixels and in context
- **WHEN** at least two simplified candidates are compared at 64 pixels and beside other carried kitchenware on the live map
- **THEN** the selected candidate remains readable without relying on fine rivets, stitching, tiny blade reflections, or full-resolution detail
- **THEN** its outline remains comic-like and its protected inter-knife gaps stay open

### Requirement: Dirty kitchenware is unmistakable at map scale
When dirty-texture rendering is enabled, every dirty cookware, primitive cookware, plate, and cutlery family SHALL use a deliberately exaggerated fixed-color grime treatment that is immediately distinguishable from its clean sibling at ordinary map zoom. The treatment SHALL favor broad low-frequency grease, sauce, crust, rings, crumbs, and smears over realistic fine speckles. It SHALL cover enough of the usable surface to remain obvious after mipmapping while preserving the product silhouette, handles, utensil separation, exterior comic outline, and enough clean material surface to identify the underlying Stuff.

Every grime-bearing coordinate SHALL be fixed black in the matching `CutoutComplex` mask rather than part of the red or green Stuff regions. The same strongly visible sanitation treatment SHALL therefore survive Steel, wood, stone, silver, gold, and registered optional-material tinting without becoming muddy, disappearing into the Stuff color, or changing sanitation state or gameplay identity. At the 64x64 package proxy, each dirty family SHALL retain multiple connected high-contrast grime marks, including at least one broad mark rather than only isolated one-pixel flecks; package tests SHALL compare the simulated material render rather than accepting byte inequality alone.

#### Scenario: Steel ware returns dirty after ordinary use
- **WHEN** a steel cookware set becomes dirty through native cooking and a steel plate and cutlery setting become dirty through native dining
- **THEN** their map sprites show bold brown/yellow fixed-color grime that is immediately visible without selecting the items or reading the inspector
- **THEN** the remaining metal surfaces still read as Core-relative steel and the exterior outline remains unchanged

#### Scenario: Dirty variants are tinted from several materials
- **WHEN** clean and dirty steel, wood, and stone families are rendered at the same map zoom and lighting
- **THEN** every dirty sibling remains conspicuously different after its actual Stuff color is applied
- **THEN** the grime retains its fixed authored color while only the intended material-bearing surface changes with Stuff

### Requirement: Vanilla Textures Expanded - Variations is an absent-safe cosmetic integration
The optional texture-variation integration SHALL key to exact package ID `VanillaExpanded.VTEXVariations` and SHALL load after that package and `OskarPotocki.VanillaFactionsExpanded.Core` without making either a required dependency. With the inspected compatible RimWorld 1.6 shape active, supported Immersive Chefs appliances and stations SHALL use the real `VEF.Buildings.CompProperties_RandomBuildingGraphic` contract for randomized/player-cyclable building families. Because that upstream contract is building-only, portable cookware, plates, and cutlery SHALL always use a reflection-free Immersive Chefs selector for their base-owned sanitation-state rendering. The exact VTEX package gate SHALL enable only the selector's optional wood/stone material-family choices; dirty base-family selection MUST remain available when VTEX and VEF are absent. The portable selector SHALL choose only cosmetic variants compatible with product kind, Stuff material class, and sanitation state; it MUST NOT alter gameplay stats, identity, stack admission, save semantics, or cleaning ownership.

Every base and optional building path supplied to the upstream component SHALL name a complete four-direction family satisfying the same authored-horizontal/authored-vertical contract as the fallback. Cycling variants MUST NOT revert a building to a single raster or a mechanically rotated front view.

The active integration SHALL provide visibly appropriate wood and registered-stone families for each eligible portable product rather than presenting those materials as merely brown or gray metal. Ordinary metal/plastic art SHALL remain a sensible fallback for unclassified materials. A dirty state SHALL be visibly distinguishable through a bounded dirt overlay or dirty texture family when one exists, while clean and dirty renderings preserve the actual Stuff tint through their masks. Cosmetic choices SHALL remain stable across an ordinary save/load and SHALL not fabricate sanitation state.

Immersive Chefs SHALL expose `TextureVariationIntegration` as `Auto` or `Off`, defaulting to `Auto`, and `ShowDirtyWareTextures` as a boolean defaulting to `true`. Changing either setting SHALL require restart because it changes finalized graphic ownership or cached graphics. `TextureVariationIntegration=Off` SHALL retain the same complete masked base artwork and base-owned sanitation selector while adding no VEF comp and selecting no optional wood/stone family even when the optional packages remain active. `ShowDirtyWareTextures=false` SHALL keep the same base or optional clean material family while suppressing only its dirty sibling.

#### Scenario: The optional package is absent
- **WHEN** Core, Harmony, and Immersive Chefs load without VTEX Variations or Vanilla Expanded Framework
- **THEN** every item and building resolves its complete base custom texture, dirty cookware/plates/cutlery still select their base dirty siblings, no optional type is referenced by a finalized Def, and no texture or XML error is logged

#### Scenario: The player disables cosmetic variation
- **WHEN** VTEX Variations is installed but `TextureVariationIntegration` is `Off` after restart
- **THEN** every Thing uses its complete base material artwork, dirty cookware/plates/cutlery still select the base dirty family, and Immersive Chefs installs no upstream building variation comp or optional wood/stone variation

#### Scenario: Material and sanitation variants are active
- **WHEN** VTEX Variations and its compatible VEF dependency load with wooden and granite plates, wooden cutlery, granite cookware, and matched clean/dirty ware
- **THEN** each portable Thing uses the appropriate wood or stone family, dirty ware has a visible but readable dirt treatment, every diffuse has a valid Stuff mask, and the underlying Stuff color and sanitation state remain unchanged

#### Scenario: A player cycles a supported kitchen building
- **WHEN** the player invokes the upstream graphic-cycle gizmo on an Immersive Chefs appliance or station and then saves and reloads
- **THEN** the real VEF component changes among only the declared complete building variants and preserves the selected graphic through the upstream save contract

#### Scenario: A player views one variant in every direction
- **WHEN** north-, east-, south-, and west-facing copies of each supported non-minifiable kitchen building are changed to the same alternate family through their native VEF graphic-cycle gizmos
- **THEN** every copy resolves the matching authored directional texture from that alternate family, preserves its footprint and equipment identity, and retains the same direction and selected family after RimWorld save/load

#### Scenario: The optional API shape changes
- **WHEN** the package ID is active but the inspected `VEF.Buildings.CompProperties_RandomBuildingGraphic` shape is unavailable or incompatible
- **THEN** Immersive Chefs logs one actionable compatibility warning, disables only cosmetic variation, and continues rendering every Thing with its base fallback

### Requirement: Final art is accepted through live game rendering
Static source inspection and local thumbnails are supporting evidence only. The reviewed built package SHALL be loaded in a fresh isolated RimWorld process, every selected asset SHALL be rendered through its real finalized Def beside representative vanilla content, and the acting agent SHALL personally inspect retained screenshots. The visual catalog SHALL cover map rendering, selection brackets, stack overlays for stackable items, item scale, building footprint/rotation, and ordinary inspector or build-menu presentation where applicable. For buildings, retained close screenshots SHALL place each custom building and an appropriate Core production bench in the same live scene at the same zoom and lighting, and SHALL show north, east, south, and west views closely enough to judge both style and directional consistency.

#### Scenario: A selected source image looks good outside the game
- **WHEN** the real Def renders it too small, too large, muddy after Stuff tint, directionally incomplete, visually ambiguous, or inconsistent beside vanilla assets
- **THEN** the asset remains unaccepted and selection/wiring is revised before its task is checked
