## ADDED Requirements

**Mod scope:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Kitchenware has explicit gameplay identities
Immersive Chefs SHALL add four distinct, quality-bearing kitchenware types: a cookware set, a plate, a cutlery setting, and a chef's knife set. One cookware item SHALL abstract one pot, one pan, their lids, handles, and small utensils; one plate and one cutlery item SHALL each serve one meal portion; and one chef's knife item SHALL represent a cook's personal knife set.

Player-facing terminology SHALL call the pot/pan/lid abstraction a `cookware set` everywhere. `Tableware` MAY collectively mean plates and cutlery, and `kitchenware` MAY name the broad catalog category containing all four product types, but alerts, bills, requirements, float-menu messages, inspect strings, and other actionable text MUST name the exact missing or used product rather than calling a cookware set `kitchenware`.

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
Cookware and chef's knives SHALL accept compatible metallic Stuff, including vanilla steel, silver, and gold and supported modded lead, iron, steel variants, stainless steel, brass, bronze, copper, aluminium, titanium, and other metals. Primitive cookware and stone plates SHALL accept any Stuff with the finalized `Stony` material category so loaded material mods participate without an Immersive Chefs Def-name allowlist. Explicit Immersive Chefs exclusions and more-specific material registrations SHALL take precedence over broad categories, so a deliberately excluded material remains unavailable and an explicitly registered plastic or metal is not misclassified merely because it also advertises `Stony`. Plates SHALL accept compatible metal, wood, stone, ceramic, adobe, and registered plastic materials. Cutlery SHALL accept compatible metal, wood, and registered plastic materials.

The classifier SHALL use explicit kitchen-material registrations plus narrowly applicable Stuff properties and SHALL allow compatibility patches to include or exclude individual StuffDefs. Each eligible material SHALL receive a product-aware fabrication tier of `PrimitiveStone`, `Soft`, `Intermediate`, or `Modern`. Registered wood and lead SHALL be `Soft` for plates and cutlery; any otherwise-unclassified `Stony` Stuff SHALL be `PrimitiveStone` for cookware and plates; iron, copper, bronze, brass, silver, gold, aluminium, and otherwise compatible unknown metals SHALL default to `Intermediate`; vanilla steel, Expanded Materials steel variants/stainless/titanium, plasteel, and registered plastics SHALL be `Modern` for their supported products. This rule SHALL permit the locally downloaded ABS polymer to be registered as plastic despite also advertising metallic, woody, and stony categories, and SHALL permit future stony or brass Stuff without requiring a hard-coded package ID.

#### Scenario: Expanded Materials - Metals is active
- **WHEN** `argon.expandedmaterials.metals` supplies `EM_Iron`, `EM_MildSteel`, `EM_TemperedSteel`, `EM_Lead`, `EM_Bronze`, `EM_Copper`, `EM_StainlessSteel`, and `EM_Titanium`
- **THEN** all eight StuffDefs are valid choices for metal kitchenware and remain distinguishable by Stuff, color, and material stats

#### Scenario: A compatible brass mod is added later
- **WHEN** a loaded mod supplies metallic brass Stuff that is not explicitly excluded
- **THEN** metal kitchenware recipes accept it without Immersive Chefs referencing that mod's assembly or package ID

#### Scenario: ABS polymer is registered explicitly
- **WHEN** `mlie.simplysublimeabspolymer` is active and its ABS Stuff carries several broad material categories
- **THEN** the compatibility registration classifies ABS as plastic for plates and cutlery but does not accidentally classify every metallic, woody, or stony Stuff as plastic

#### Scenario: A material mod adds a new stone block Stuff
- **WHEN** a loaded mod supplies an otherwise-unregistered Stuff carrying the finalized `Stony` category
- **THEN** primitive cookware and stone-plate recipes accept it at the crafting spot without an Immersive Chefs package-specific patch
- **THEN** any explicit exclusion or explicit non-stone registration for that Stuff still wins

### Requirement: Optional ceramic and supported adobe shapes have safe recipe paths
Immersive Chefs SHALL NOT invent ceramic or porcelain content or make a ceramics provider mandatory. When exact package `zal.ceramics` (`Ceramics (Continued)`) is active and finalized Stuff Def `N7_Porcelain` plus the expected ceramics benches are present, Immersive Chefs SHALL explicitly register that Stuff as `Ceramic` for plates and expose a four-plate porcelain recipe at `CeramicsBench_Basic` and `CeramicsBench_Electric`. The recipe SHALL consume exactly 4 units of one registered ceramic Stuff, retain that exact Stuff and its color on all four output plates, use Crafting quality, and retain the provider's `BasicCeramics` progression. Porcelain MUST NOT enter primitive-stone cookware, primitive-stone plates, cutlery, or chef's-knife recipes merely because the upstream Stuff also carries `Stony`.

When the package is absent, disabled, or its expected Def shape is missing, the porcelain recipe and classification SHALL be unavailable without a missing Def, hard dependency, or fallback to broad `Stony` inference. Because the locally available `EM_AdobeBricks` is a resource rather than Stuff, its current integration SHALL use a fixed-material adobe plate recipe and MUST NOT pretend that the output is made from Stuff.

#### Scenario: No compatible ceramics mod is loaded
- **WHEN** the player opens the plate recipes without a registered RimWorld 1.6 ceramic material
- **THEN** no ceramic or porcelain recipe or research is exposed and the game still exposes wood, metal, and any valid adobe or plastic paths without a missing Def, failed recipe, or hard dependency on a ceramics mod

#### Scenario: Ceramics (Continued) porcelain is available
- **WHEN** exact package `zal.ceramics` finalizes `N7_Porcelain`, `CeramicsBench_Basic`, `CeramicsBench_Electric`, and `BasicCeramics`
- **THEN** both ceramics benches expose the Immersive Chefs porcelain-plate recipe after the provider's research
- **THEN** one completed native bill consumes exactly 4 porcelain and produces four quality-bearing plates whose Stuff is exactly `N7_Porcelain`

#### Scenario: Porcelain's broad Stony tag does not leak
- **WHEN** `N7_Porcelain` is registered as ceramic for plates
- **THEN** it is not accepted by primitive-stone cookware or plate recipes and is not accepted for cutlery or chef's knives

#### Scenario: Ceramics provider shape is incomplete
- **WHEN** exact package `zal.ceramics` is active but its porcelain Stuff, research, recipe, or either ceramics bench identity is missing
- **THEN** the porcelain integration fails closed with one actionable diagnostic while ordinary Immersive Chefs plate recipes remain available

#### Scenario: Expanded Materials - Masonry is active
- **WHEN** `argon.expandedmaterials.masonry` provides the non-Stuff resource `EM_AdobeBricks`
- **THEN** the crafting recipe consumes that resource and produces fixed-material adobe plates with a stable adobe label and color

### Requirement: Recipes model complete kitchenware sets
Kitchenware recipes SHALL use the following baseline resource costs and outputs:

| Product and fabrication route | Workstation | Ingredients | Output |
| --- | --- | --- | --- |
| Primitive stone cookware | `CraftingSpot` | 5 units of one eligible stony Stuff plus 1 wood | 1 cookware set |
| Medieval cookware | `FueledSmithy` or `ElectricSmithy` | 6 units of one eligible intermediate metal Stuff plus 1 wood | 1 cookware set |
| Modern cookware | `TableMachining` | 6 units of one eligible modern metal Stuff plus 1 wood | 1 cookware set |
| Chef's knife set | `TableMachining` | 6 units of one eligible metal Stuff | 1 chef's knife set |
| Soft or fixed plates | `CraftingSpot` | 4 units of wood, registered lead, or the fixed adobe ingredient | 4 plates |
| Soft cutlery | `CraftingSpot` | 2 units of wood or registered lead | 4 cutlery settings |
| Intermediate metal plates | `FueledSmithy` or `ElectricSmithy` | 4 units of one eligible intermediate metal Stuff | 4 plates |
| Intermediate metal cutlery | `FueledSmithy` or `ElectricSmithy` | 2 units of one eligible intermediate metal Stuff | 4 cutlery settings |
| Universal metal/plastic plates | `TableMachining` | 4 units of any eligible metal or registered plastic Stuff | 4 plates |
| Universal metal/plastic cutlery | `TableMachining` | 2 units of any eligible metal or registered plastic Stuff | 4 cutlery settings |

These costs SHALL remain benchmarked against same-era Core objects instead of being treated as isolated tuning constants: one Core wall consumes 5 material units and one Core steel knife consumes 30, so primitive cookware costs one wall-equivalent of stone, metal cookware costs only 1.2 wall-equivalents, a non-weapon chef's knife set costs 20% of the combat knife, and one complete place setting costs 1.5 material units. A future rebalance SHALL record its comparator Defs and ratios before changing the table.

Every recipe SHALL use the completing pawn's Crafting skill to assign a vanilla `QualityCategory` to all items in its output batch. The wood in each cookware recipe SHALL represent handles, spatulas, and related non-metal parts rather than a second selectable Stuff. Every ingredient requirement SHALL use a semantic player label such as `any stony material`, `any intermediate metal`, or `wood`; no generated bill requirement MAY expose an internal category such as `root`. Plate and cutlery recipes SHALL have no Immersive Chefs research prerequisite: progression comes from access to `CraftingSpot`, vanilla `Smithing`/`Electricity` for the smithies, and vanilla `Machining` for `TableMachining`. The machining recipes SHALL remain a late universal route even for materials also available at an earlier station.

The bill-configuration requirement SHALL remain a semantic description of the materials that recipe accepts. The info card for an existing Stuff-made kitchenware Thing SHALL instead select the producing recipe tier appropriate to that Thing's actual Stuff and show the exact material for the batch cost. It MUST NOT inherit the unrelated first producing recipe merely because RimWorld enumerated that recipe first.

#### Scenario: Steel plate reports its real production material
- **WHEN** the player opens the native info card for an existing steel plate while primitive, soft, intermediate, and machining plate recipes are all finalized
- **THEN** the Ingredients row reports the machining batch cost using steel, does not report `any stony material`, and the primitive bill still advertises its broad stony requirement

#### Scenario: Craft one cookware abstraction
- **WHEN** a pawn completes a cookware recipe using 6 stainless steel and 1 wood at a machining table
- **THEN** exactly one stainless-steel cookware item is produced, it represents the full cookware set, and its quality is generated from that crafting operation

#### Scenario: Begin with stone cookware
- **WHEN** a tribal colony has no research and completes the primitive recipe using 5 granite blocks and 1 wood at `CraftingSpot`
- **THEN** it receives one Stuff-colored granite cookware set with the primitive performance and durability profile

#### Scenario: Smith intermediate place settings
- **WHEN** a colony with a fueled smithy crafts bronze plates and cutlery from registered bronze
- **THEN** the smithy produces four of each with bronze Stuff and one crafting quality per output batch without requiring an Immersive Chefs research project

#### Scenario: Craft a batch of wood place settings
- **WHEN** a pawn completes each wood place-setting recipe at a crafting spot
- **THEN** 4 wood yields four wood plates and 2 wood yields four stackable wood cutlery settings, with one shared crafting quality per batch

### Requirement: Portable culinary ware enters appropriate trader stock
Every portable Immersive Chefs ware item that a player can reasonably carry and own SHALL be sellable. Trader stock SHALL remain low and thematic: neolithic bulk-goods traders MAY carry primitive cookware and inexpensive wood or stone tableware; outlander bulk-goods traders MAY carry ordinary cookware, plates, cutlery, and chef's knife sets; and exotic-goods traders MAY rarely carry glitterworld cookware. Prepared ingredients, dirty ware, installed buildings, and research-locked station/appliance products SHALL not be injected as ordinary stock merely to satisfy sellability. Stock generators SHALL preserve Stuff, quality, sanitation, and stack counts and SHALL not create optional-mod package coupling.

#### Scenario: Buy everyday kitchen supplies
- **WHEN** an eligible bulk-goods trader generates stock
- **THEN** it may contain a low number of era-appropriate cookware sets, plates, cutlery settings, or chef's knife sets using valid Stuff and quality
- **THEN** the player can buy or sell eligible portable ware through the native trade dialog

#### Scenario: Find glitterworld cookware
- **WHEN** an eligible exotic-goods trader generates stock repeatedly
- **THEN** glitterworld cookware appears only at its declared low chance and never appears as routine bulk stock

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

Primitive stone cookware SHALL have intrinsic material-cleanliness `15`, Cooking Speed Factor `0.60`, Kitchen Comfort `0.10`, and material Culinary Quality Modifier `-12`; after Stuff and quality factors its maximum hit points SHALL be no more than half those of equal-quality vanilla-steel cookware. Lead SHALL have a strong cleanliness and culinary penalty; uranium SHALL be treated as a distinct modern metal with an unsafe culinary profile rather than an anonymous `OtherMetal`; iron SHALL be less clean and less efficient than ordinary steel; stainless steel SHALL be the cleanest general-purpose metal; bronze and brass SHALL favor comfort while remaining below stainless steel for cleanliness; and advanced steel variants and titanium SHALL favor speed and culinary performance. Where Expanded Materials supplies existing cleanliness offsets, the integration SHALL preserve at least the observed ordering of `EM_StainlessSteel` (+0.5) above `EM_Bronze` (+0.2) above an otherwise neutral material.

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
- **THEN** the recipe remains valid and its info card exposes lead's ordinary sanitation and culinary disadvantages without revealing any latent toxicity state

### Requirement: Only toxic ware materials add vanilla toxic buildup
Lead and uranium SHALL be the only default kitchenware materials that contribute toxic exposure. A covered humanlike ingestion SHALL add a small dose to the eater's vanilla `ToxicBuildup` Hediff for each toxic ware item actually used: `0.020` severity for toxic cookware recorded on that serving, `0.015` for its toxic plate, and `0.010` for its toxic cutlery. At the default scale, an all-toxic place setting therefore adds `0.045` per meal; compared with Core's `0.08` severity-per-day recovery, repeated ordinary use can accumulate slowly while occasional exposure normally recovers. Lead and uranium SHALL use the same initial dose table until playtesting justifies a material-specific distinction.

Exposure SHALL occur once only after RimWorld reports positive nutrition ingestion. Crafting, carrying, reserving, cooking, inspection, aborted ingestion, non-toxic kitchenware, chef's knives, missing/legacy cookware provenance, animals, and excluded hand-eaten foods SHALL add no toxic buildup. The cooked serving SHALL preserve only its hidden cookware-material provenance through stack split/merge and save/load; the embedded physical plate and exact dining cutlery remain authoritative for their own material. The meal, cookware, plate, and cutlery inspect strings MUST NOT reveal a toxicity flag, predicted dose, or current latent toxic state. Pawn Health inspection SHALL remain entirely vanilla-owned: the initial buildup stays hidden while vanilla marks its stage `becomeVisible=false`, and the Hediff becomes player-visible only at vanilla's current threshold.

`ToxicKitchenwareExposureScale` SHALL be a live setting with default `1.0` and range `0.0`–`3.0`. It SHALL multiply only the material-dose sum at ingestion, clamp before application, and SHALL NOT affect food-poisoning probability, food-poisoning attribution, material cleanliness, or culinary quality. Setting it to zero disables new toxic-ware exposure without deleting an existing pawn Hediff or rewriting stored meal provenance.

#### Scenario: Repeated lead service accumulates vanilla buildup
- **WHEN** a humanlike pawn eats a covered meal cooked with lead cookware from a lead plate using lead cutlery at default settings
- **THEN** the completed native ingestion adds exactly `0.045` severity to the pawn's vanilla `ToxicBuildup` Hediff once
- **THEN** no kitchenware or meal inspection reveals that hidden dose or the pawn's latent Hediff state

#### Scenario: Uranium plate contributes only when used
- **WHEN** a humanlike pawn completes ingestion from an actual uranium plate while the cookware and cutlery are non-toxic
- **THEN** exactly the `0.015` plate dose is added after positive nutrition ingestion
- **THEN** merely crafting, carrying, reserving, or inspecting the uranium plate adds nothing

#### Scenario: Native cooking records toxic cookware separately from service ware
- **WHEN** a pawn performs an ordinary `DoBill` job using actual uranium cookware, the resulting meal receives an ordinary steel plate, and the pawn later completes native ingestion using actual uranium cutlery
- **THEN** the cooked serving retains uranium as its hidden cookware material while the exact physical steel plate and uranium cutlery remain authoritative for themselves
- **THEN** positive ingestion adds exactly `0.030` severity once (`0.020` cookware plus `0.010` cutlery), while cooking itself adds nothing

#### Scenario: Ordinary ware and animals remain toxicity-free
- **WHEN** a humanlike pawn uses steel, wood, stone, silver, gold, ceramic, plastic, or glitterworld ware, or an animal eats a meal associated with any ware
- **THEN** Immersive Chefs adds no toxic buildup

#### Scenario: Native visibility threshold owns disclosure
- **WHEN** toxic-ware ingestion raises an eater from a hidden vanilla `ToxicBuildup` severity below the first visible stage to or above that stage
- **THEN** the buildup becomes visible only on the pawn's native Health tab
- **THEN** the consumed meal and returned dirty ware still expose no toxicity diagnostic

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

In RimWorld 1.6, the native Gear tab groups belt/utility apparel under the visible `Equipment` heading and its apparel float-menu action is localized as `Force equip`. This presentation SHALL be accepted as belt apparel only when the finalized Def remains `Apparel`, declares no weapon equipment type or `CompEquippable`, and the pawn's existing weapon remains equipped.

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
