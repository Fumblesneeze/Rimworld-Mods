## ADDED Requirements

**Mod scope:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Kitchenware has explicit gameplay identities
Immersive Chefs SHALL add four distinct, quality-bearing kitchenware types: a cookware set, a plate, a cutlery setting, and a chef's knife set. One cookware item SHALL abstract one pot, one pan, their lids, handles, and small utensils; one plate and one cutlery item SHALL each serve one meal portion; and one chef's knife item SHALL represent a cook's personal knife set.

#### Scenario: Inspect each crafted kitchenware type
- **WHEN** a player inspects a cookware set, plate, cutlery setting, and chef's knife set
- **THEN** each item identifies its gameplay unit and displays its material, crafting quality, cleanliness, comfort, and every speed or culinary stat applicable to that type

### Requirement: Stuff-aware items preserve material identity and color
Metal, wood, registered stone-block, and compatible plastic kitchenware SHALL be stuff-aware, SHALL retain the selected Stuff on every split, merge, haul, save, and load operation, and SHALL draw using that Stuff's color. Fixed-material ceramic and adobe plates and fixed glitterworld cookware SHALL use their own material-specific labels and colors. Kitchenware with different Stuff, crafting quality, or sanitation state MUST NOT merge into one stack.

#### Scenario: Craft visually distinct metal plates
- **WHEN** a crafter makes one plate batch from steel and another from gold
- **THEN** the resulting plates retain their respective Stuff, render in the corresponding Stuff colors, and do not stack together

#### Scenario: Reload quality-bearing kitchenware
- **WHEN** a saved game containing clean and dirty kitchenware of multiple materials and qualities is reloaded
- **THEN** every item retains its material, color source, crafting quality, quantity, and sanitation state

### Requirement: Material eligibility is capability-based and extensible
Cookware and chef's knives SHALL accept compatible metallic Stuff, including vanilla steel, silver, and gold and supported modded lead, iron, steel variants, stainless steel, brass, bronze, copper, aluminium, titanium, and other metals. Primitive cookware SHALL additionally accept only registered block Stuffs `BlocksSandstone`, `BlocksGranite`, `BlocksLimestone`, `BlocksSlate`, and `BlocksMarble`; broad `Stony` membership MUST NOT admit jade or raw stone. Plates SHALL accept compatible metal, wood, ceramic, adobe, and registered plastic materials. Cutlery SHALL accept compatible metal, wood, and registered plastic materials.

The classifier SHALL use explicit kitchen-material registrations plus narrowly applicable Stuff properties, SHALL allow compatibility patches to include or exclude individual StuffDefs, and MUST NOT classify a material solely from one broad category tag. Each eligible material SHALL receive a product-aware fabrication tier of `PrimitiveStone`, `Soft`, `Intermediate`, or `Modern`. Registered wood and lead SHALL be `Soft` for plates and cutlery; iron, copper, bronze, brass, silver, gold, aluminium, and otherwise compatible unknown metals SHALL default to `Intermediate`; vanilla steel, Expanded Materials steel variants/stainless/titanium, plasteel, and registered plastics SHALL be `Modern` for their supported products. This rule SHALL permit the locally downloaded ABS polymer to be registered as plastic despite also advertising metallic, woody, and stony categories, and SHALL permit a future brass Stuff without requiring a hard-coded package ID.

#### Scenario: Expanded Materials - Metals is active
- **WHEN** `argon.expandedmaterials.metals` supplies `EM_Iron`, `EM_MildSteel`, `EM_TemperedSteel`, `EM_Lead`, `EM_Bronze`, `EM_Copper`, `EM_StainlessSteel`, and `EM_Titanium`
- **THEN** all eight StuffDefs are valid choices for metal kitchenware and remain distinguishable by Stuff, color, and material stats

#### Scenario: A compatible brass mod is added later
- **WHEN** a loaded mod supplies metallic brass Stuff that is not explicitly excluded
- **THEN** metal kitchenware recipes accept it without Immersive Chefs referencing that mod's assembly or package ID

#### Scenario: ABS polymer is registered explicitly
- **WHEN** `mlie.simplysublimeabspolymer` is active and its ABS Stuff carries several broad material categories
- **THEN** the compatibility registration classifies ABS as plastic for plates and cutlery but does not accidentally classify every metallic, woody, or stony Stuff as plastic

### Requirement: Deferred ceramic and supported adobe shapes have safe recipe paths
Immersive Chefs SHALL NOT require an unsupported pre-1.6 ceramics package and SHALL NOT invent ceramic or porcelain content, research, or recipes in the current change. A later compatibility change MAY use the existing registration seam to add ceramic Stuff or a fixed ceramic ingredient-to-plate recipe after a supported RimWorld 1.6 provider is selected. Because the locally available `EM_AdobeBricks` is a resource rather than Stuff, its current integration SHALL use a fixed-material adobe plate recipe and MUST NOT pretend that the output is made from Stuff.

#### Scenario: No compatible ceramics mod is loaded
- **WHEN** the player opens the plate recipes without a registered RimWorld 1.6 ceramic material
- **THEN** no ceramic or porcelain recipe or research is exposed and the game still exposes wood, metal, and any valid adobe or plastic paths without a missing Def, failed recipe, or hard dependency on a ceramics mod

#### Scenario: Expanded Materials - Masonry is active
- **WHEN** `argon.expandedmaterials.masonry` provides the non-Stuff resource `EM_AdobeBricks`
- **THEN** the crafting recipe consumes that resource and produces fixed-material adobe plates with a stable adobe label and color

### Requirement: Recipes model complete kitchenware sets
Kitchenware recipes SHALL use the following baseline resource costs and outputs:

| Product and fabrication route | Workstation | Ingredients | Output |
| --- | --- | --- | --- |
| Primitive stone cookware | `CraftingSpot` | 40 units of one registered vanilla stone-block Stuff plus 5 wood | 1 cookware set |
| Medieval cookware | `FueledSmithy` or `ElectricSmithy` | 50 units of one eligible intermediate metal Stuff plus 5 wood | 1 cookware set |
| Modern cookware | `TableMachining` | 50 units of one eligible modern metal Stuff plus 5 wood | 1 cookware set |
| Chef's knife set | `TableMachining` | 30 units of one eligible metal Stuff | 1 chef's knife set |
| Soft or fixed plates | `CraftingSpot` | 20 units of wood, registered lead, or the fixed adobe ingredient | 4 plates |
| Soft cutlery | `CraftingSpot` | 12 units of wood or registered lead | 4 cutlery settings |
| Intermediate metal plates | `FueledSmithy` or `ElectricSmithy` | 20 units of one eligible intermediate metal Stuff | 4 plates |
| Intermediate metal cutlery | `FueledSmithy` or `ElectricSmithy` | 12 units of one eligible intermediate metal Stuff | 4 cutlery settings |
| Universal metal/plastic plates | `TableMachining` | 20 units of any eligible metal or registered plastic Stuff | 4 plates |
| Universal metal/plastic cutlery | `TableMachining` | 12 units of any eligible metal or registered plastic Stuff | 4 cutlery settings |

Every recipe SHALL use the completing pawn's Crafting skill to assign a vanilla `QualityCategory` to all items in its output batch. The wood in each cookware recipe SHALL represent handles, spatulas, and related non-metal parts rather than a second selectable Stuff. Plate and cutlery recipes SHALL have no Immersive Chefs research prerequisite: progression comes from access to `CraftingSpot`, vanilla `Smithing`/`Electricity` for the smithies, and vanilla `Machining` for `TableMachining`. The machining recipes SHALL remain a late universal route even for materials also available at an earlier station.

#### Scenario: Craft one cookware abstraction
- **WHEN** a pawn completes a cookware recipe using 50 stainless steel and 5 wood at a machining table
- **THEN** exactly one stainless-steel cookware item is produced, it represents the full cookware set, and its quality is generated from that crafting operation

#### Scenario: Begin with stone cookware
- **WHEN** a tribal colony has no research and completes the primitive recipe using 40 granite blocks and 5 wood at `CraftingSpot`
- **THEN** it receives one Stuff-colored granite cookware set with the primitive performance and durability profile

#### Scenario: Smith intermediate place settings
- **WHEN** a colony with a fueled smithy crafts bronze plates and cutlery from registered bronze
- **THEN** the smithy produces four of each with bronze Stuff and one crafting quality per output batch without requiring an Immersive Chefs research project

#### Scenario: Craft a batch of wood place settings
- **WHEN** a pawn completes each wood place-setting recipe at a crafting spot
- **THEN** 20 wood yields four wood plates and 12 wood yields four stackable wood cutlery settings, with one shared crafting quality per batch

### Requirement: Material and craftsmanship drive visible performance stats
Immersive Chefs SHALL expose data-driven kitchenware stats with the following applicability:

| Item | Required performance stats |
| --- | --- |
| Cookware | Kitchen Cleanliness, Cooking Speed Factor, Kitchen Comfort, Culinary Quality Modifier |
| Plate | Kitchen Cleanliness, Eating Speed Factor, Dining Comfort |
| Cutlery | Kitchen Cleanliness, Dining Comfort |
| Chef's knife | Kitchen Cleanliness, Cooking Speed Factor, Kitchen Comfort, Culinary Quality Modifier |

Each final stat SHALL combine the item's base value, its material profile, and applicable vanilla crafting-quality stat factors. `Culinary Quality Modifier` SHALL be a displayed, calculated quality-point offset composed from a material offset and the standard quality offsets Awful -10, Poor -5, Normal 0, Good +3, Excellent +6, Masterwork +10, and Legendary +15. For meal-quality calculation, the kitchenware system SHALL also publish `Culinary Tool Score = clamp(50 + 2 * Culinary Quality Modifier, 0, 100)`. The displayed signed offset remains the player-facing explanation; the normalized score is the deterministic input consumed by the meal-state contract. Compatibility XML SHALL express material differences through Stats or narrowly targeted patches rather than optional-mod type references in compiled code.

Kitchen Comfort SHALL represent tool ergonomics while cooking; Dining Comfort SHALL represent the feel of a plate or cutlery setting while eating. Those values SHALL be credited only during the corresponding active job and SHALL remain bounded by the normal Comfort need range.

#### Scenario: Compare equal-quality cookware made from different metals
- **WHEN** a player compares two Normal-quality cookware sets made from lead and stainless steel
- **THEN** their info cards show different material-derived cleanliness, speed, comfort, or culinary values, with lead carrying a material hygiene and culinary penalty relative to stainless steel

#### Scenario: Compare craftsmanship within one material
- **WHEN** a player compares Awful and Legendary cookware made from the same Stuff
- **THEN** the calculated craftsmanship portion of Culinary Quality Modifier is -10 and +15 respectively and any quality-sensitive stats reflect their vanilla quality factors

#### Scenario: Convert the signed modifier for a meal
- **WHEN** otherwise neutral-material cookware has a displayed Culinary Quality Modifier of `-10` or `+15`
- **THEN** it publishes normalized Culinary Tool Scores of `30` and `80`, respectively
- **THEN** values outside the representable range clamp at `0` or `100`

### Requirement: Known materials have coherent hygiene profiles
The default and Expanded Materials compatibility profiles SHALL make material choices observably meaningful. A clean adobe plate SHALL contribute an intrinsic normalized material-cleanliness score of `5`, and a clean wooden plate or cutlery setting SHALL contribute `10`, on the kitchenware system's `0`-through-`100` material-cleanliness scale. Those porous-material penalties SHALL remain even while the item's mutable sanitation state is clean. Registered ceramic SHALL contribute at least `70` and ordinary vanilla steel SHALL contribute `50`, so clean wood and adobe remain materially worse than common durable service ware.

Primitive stone cookware SHALL have intrinsic material-cleanliness `15`, Cooking Speed Factor `0.60`, Kitchen Comfort `0.10`, and material Culinary Quality Modifier `-12`; after Stuff and quality factors its maximum hit points SHALL be no more than half those of equal-quality vanilla-steel cookware. Lead SHALL have a strong cleanliness and culinary penalty; iron SHALL be less clean and less efficient than ordinary steel; stainless steel SHALL be the cleanest general-purpose metal; bronze and brass SHALL favor comfort while remaining below stainless steel for cleanliness; and advanced steel variants and titanium SHALL favor speed and culinary performance. Where Expanded Materials supplies existing cleanliness offsets, the integration SHALL preserve at least the observed ordering of `EM_StainlessSteel` (+0.5) above `EM_Bronze` (+0.2) above an otherwise neutral material.

#### Scenario: Primitive cookware remains deliberately inferior
- **WHEN** equal-quality granite and vanilla-steel cookware are compared
- **THEN** the granite set displays cleanliness `15`, speed `0.60`, comfort `0.10`, material culinary offset `-12`, and no more than half the steel set's maximum hit points

#### Scenario: Compare clean porous and durable plates
- **WHEN** equal-quality clean adobe, wood, vanilla-steel, and registered-ceramic plates are compared
- **THEN** their normalized intrinsic material-cleanliness scores are `5`, `10`, `50`, and at least `70`, respectively
- **THEN** marking the wood or adobe plate clean does not erase its intrinsic material penalty

#### Scenario: Sort Expanded Materials cookware by cleanliness
- **WHEN** equal-quality clean cookware made from stainless steel, bronze, and a neutral metal is compared
- **THEN** its effective Kitchen Cleanliness orders stainless steel above bronze above the neutral metal

#### Scenario: Choose unsafe lead for availability
- **WHEN** a player deliberately crafts kitchenware from lead
- **THEN** the recipe remains valid but the info card and downstream meal-risk calculation expose lead's sanitation and culinary disadvantages

### Requirement: Glitterworld cookware is exceptional acquisition-only equipment
Immersive Chefs SHALL add fixed-material glitterworld cookware that has no crafting recipe and no research unlock. It SHALL enter the colony only through eligible trader stock or quest rewards, SHALL generate at `Good` crafting quality or better, SHALL be nonflammable, and SHALL expose intrinsic material-cleanliness `100`, Cooking Speed Factor `1.35`, Kitchen Comfort `0.90`, and material Culinary Quality Modifier `+15`.

Glitterworld cookware SHALL be self-cleaning: when a cooking job that performed at least one active work tick releases it on completion or interruption, it SHALL remain or return clean and SHALL NOT produce a dirty-dish hauling or washing job. Reserving and hauling it without cooking SHALL leave it unchanged. This is the only cookware dirty-state exception in the current change.

#### Scenario: Receive glitterworld cookware from a quest
- **WHEN** an eligible quest reward generates glitterworld cookware
- **THEN** the item is Good quality or better, displays the exceptional fixed profile, and no recipe exists at any player workstation

#### Scenario: Self-clean after cooking
- **WHEN** a cook performs active cooking work with glitterworld cookware and releases it
- **THEN** the same item is clean and available for reuse without entering a dishwasher or hand-washing job

### Requirement: Chef's knives occupy the belt slot and stay personal
Chef's knife sets SHALL be wearable equipment on RimWorld's belt/utility slot, SHALL not replace a pawn's weapon, and SHALL contribute their speed, cleanliness, comfort, and culinary modifiers only while worn by the pawn performing cooking or preparation work. Cooking SHALL remain possible without a knife set, but an eligible equipped set SHALL be preferred over any communal or unequipped knife and SHALL never be consumed by a recipe.

Chef's knives SHALL carry and display their inherent material cleanliness stat but SHALL not carry the reusable-ware clean/dirty sanitation component, become dirty through cooking or preparation, or participate in the dirty-dish washing lifecycle in this planned scope.

#### Scenario: A cook wears a chef's knife set
- **WHEN** a pawn wearing a Good-quality steel chef's knife performs cooking or ingredient preparation
- **THEN** that same knife set supplies its visible cooking modifiers, remains equipped after the job, and occupies the belt/utility slot rather than the weapon slot

#### Scenario: A cook has no knife set
- **WHEN** an otherwise valid cooking job is assigned to a pawn without a chef's knife
- **THEN** the job can proceed without a knife bonus and does not reserve or consume another pawn's worn knife

#### Scenario: Personal knife finishes culinary work
- **WHEN** a pawn completes cooking or preparation while wearing a chef's knife set
- **THEN** the knife retains its intrinsic material cleanliness and equipped state without creating dirty ware or a dishwashing job

### Requirement: Cutlery is safely stackable
Cutlery SHALL stack when Stuff, crafting quality, and sanitation state are identical. Its stack count SHALL be preserved through reservations, carrying, splitting, merging, saving, and loading, and one eating job SHALL reserve exactly one setting per diner.

#### Scenario: Reserve one setting from a clean stack
- **WHEN** a diner selects one setting from a stack of ten clean, Normal-quality steel cutlery items
- **THEN** one item is reserved and split for that diner while the remaining nine stay available to other pawns
