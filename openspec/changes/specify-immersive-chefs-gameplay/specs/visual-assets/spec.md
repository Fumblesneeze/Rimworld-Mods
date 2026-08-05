## ADDED Requirements

**Owning mod:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Every custom Thing uses selected custom art
Every player-visible Immersive Chefs item and building SHALL resolve to a custom texture owned by this mod rather than a vanilla resource, weapon, worktable, stove, or laboratory placeholder. This includes ordinary and glitterworld cookware, plates including the fixed adobe path, cutlery, the chef's knife set, prepared ingredients, domestic and industrial dishwashers, the microwave, ingredient prep station, and sauce, meat, vegetable, and pastry stations. Reuse of one selected base plate silhouette for Stuff-colored and fixed-adobe variants is allowed when their real material/color treatment remains visibly distinct.

#### Scenario: The release package is built
- **WHEN** the finalized Defs and package contents are inspected
- **THEN** every listed custom Thing resolves to an existing selected texture below the Immersive Chefs texture namespace
- **THEN** none of those Defs points at its former vanilla placeholder

### Requirement: Every distinct asset is selected from multiple candidates
At least two distinct raster candidates SHALL be generated for every distinct item or building concept before selection. Candidate review SHALL record the prompt/variant identity, final-scale preview, selection outcome, and concise reason. Rejected candidates and review montages SHALL remain in ignored working/evidence paths and SHALL NOT enter the release package; the selected final and durable provenance/prompt record SHALL remain in the repository.

#### Scenario: An agent selects a cookware sprite
- **WHEN** two or more cookware candidates have been generated
- **THEN** each is compared at the actual intended map/UI scale against a retained live RimWorld visual context
- **THEN** only the strongest readable, stylistically coherent candidate is wired into the shipped Def

### Requirement: Raster assets have clean game-ready silhouettes
Selected textures SHALL be PNGs with alpha, transparent corners, no chroma fringe, no baked floor/contact shadow, no text or watermark, and enough transparent padding to avoid cropping under selection brackets. Items SHALL remain recognizable at their intended 48–64 pixel review scale and buildings at their real footprint/draw size. Any Def retaining `Graphic_Multi` SHALL provide a complete valid directional texture set; otherwise it SHALL intentionally use a single graphic whose non-rotation is visually acceptable.

#### Scenario: Chroma removal damages an edge
- **WHEN** alpha inspection finds opaque corners, key-color residue, clipped geometry, or a halo at final scale
- **THEN** that candidate is corrected and revalidated or rejected before Def wiring

### Requirement: Stuff-aware art preserves material identity
Stuffable cookware, plates, cutlery, and chef's knives SHALL use neutral value separation and a Stuff-compatible shader so RimWorld can color them from their actual material. Fixed-material assets MAY use a deliberate base color. The selected artwork SHALL remain legible and materially distinct for every eligible representative Core material: wood, stone, steel, silver, and gold, with optional registered materials checked in their exact active-mod groups.

#### Scenario: One plate is rendered from several materials
- **WHEN** wooden, granite, steel, silver, and gold versions use the selected plate texture where each is eligible
- **THEN** their live map sprites retain the same semantic plate silhouette while visibly reflecting their actual Stuff colors without muddy highlights or disappearing outlines

### Requirement: Final art is accepted through live game rendering
Static source inspection and local thumbnails are supporting evidence only. The reviewed built package SHALL be loaded in a fresh isolated RimWorld process, every selected asset SHALL be rendered through its real finalized Def beside representative vanilla content, and the acting agent SHALL personally inspect retained screenshots. The visual catalog SHALL cover map rendering, selection brackets, stack overlays for stackable items, item scale, building footprint/rotation, and ordinary inspector or build-menu presentation where applicable.

#### Scenario: A selected source image looks good outside the game
- **WHEN** the real Def renders it too small, too large, muddy after Stuff tint, directionally incomplete, visually ambiguous, or inconsistent beside vanilla assets
- **THEN** the asset remains unaccepted and selection/wiring is revised before its task is checked
