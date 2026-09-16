## ADDED Requirements

**Mod scope:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Kitchenware has explicit gameplay identities
Immersive Chefs SHALL add four distinct kitchenware types: a cookware set, a plate, a cutlery setting, and a chef's knife set. One cookware item SHALL abstract one pot, one pan, their lids, handles, and small utensils; one plate and one cutlery item SHALL each serve one meal portion; and one chef's knife item SHALL represent a cook's personal knife set.

Player-facing terminology SHALL call the pot/pan/lid abstraction a `cookware set` everywhere. `Tableware` MAY collectively mean plates and cutlery, and `kitchenware` MAY name the broad catalog category containing all four product types, but alerts, bills, requirements, float-menu messages, inspect strings, and other actionable text MUST name the exact missing or used product rather than calling a cookware set `kitchenware`.

#### Scenario: Inspect each crafted kitchenware type
- **WHEN** a player inspects a cookware set, plate, cutlery setting, and chef's knife set
- **THEN** each item identifies its gameplay unit and displays its material, cleanliness, comfort, crafting quality only for cookware and knives, and every speed or culinary stat applicable to that type

### Requirement: Stuff-aware items preserve material identity and color
Metal, wood, registered stone-block, and compatible plastic kitchenware SHALL be stuff-aware, SHALL retain each unit's selected Stuff on every split, merge, haul, save, and load operation, and SHALL draw using that Stuff's color. Fixed-material ceramic and adobe plates and fixed glitterworld cookware SHALL use their own material-specific labels and colors. Plates and cutlery of the same ThingDef MAY stack across Stuff materials, preserving each unit's Stuff and hit points. Different sanitation, wash provenance, personal ownership or interrupted-session ownership MUST remain separate. Plates and cutlery SHALL have no crafting-quality component, grade, quality-based stacking restriction, or quality-derived value/durability modifier; cookware and knives retain crafting quality. Cookware and knives retain their existing stack rules.

#### Scenario: Craft visually distinct metal plates
- **WHEN** a crafter makes one plate batch from steel and another from gold
- **THEN** the resulting plates retain their respective Stuff and render in the corresponding Stuff colors, including after the batches merge into one mixed-material stack and are split again

#### Scenario: Reload kitchenware
- **WHEN** a saved game containing clean and dirty kitchenware of multiple materials and qualities is reloaded
- **THEN** every item retains its material, color source, applicable cookware/knife crafting quality, quantity, and sanitation state

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
Immersive Chefs SHALL NOT invent ceramic or porcelain content or make a ceramics provider mandatory. When exact package `zal.ceramics` (`Ceramics (Continued)`) is active and finalized Stuff Def `N7_Porcelain` plus the expected ceramics benches are present, Immersive Chefs SHALL explicitly register that Stuff as `Ceramic` for plates and expose a four-plate porcelain recipe at `CeramicsBench_Basic` and `CeramicsBench_Electric`. The recipe SHALL consume exactly 4 units of one registered ceramic Stuff, retain that exact Stuff and its color on all four output plates, have no crafting-quality grade, and retain the provider's `BasicCeramics` progression. Porcelain MUST NOT enter primitive-stone cookware, primitive-stone plates, cutlery, or chef's-knife recipes merely because the upstream Stuff also carries `Stony`.

When the package is absent, disabled, or its expected Def shape is missing, the porcelain recipe and classification SHALL be unavailable without a missing Def, hard dependency on the ceramics provider, or fallback to broad `Stony` inference. Because the locally available `EM_AdobeBricks` is a resource rather than Stuff, its current integration SHALL use a fixed-material adobe plate recipe and MUST NOT pretend that the output is made from Stuff. Both optional recipe patches SHALL match the provider's canonical package ID through the required XML Extensions patch engine rather than a translated display name.

#### Scenario: No compatible ceramics mod is loaded
- **WHEN** the player opens the plate recipes without a registered RimWorld 1.6 ceramic material
- **THEN** no ceramic or porcelain recipe or research is exposed and the game still exposes wood, metal, and any valid adobe or plastic paths without a missing Def, failed recipe, or hard dependency on a ceramics provider

#### Scenario: Ceramics (Continued) porcelain is available
- **WHEN** exact package `zal.ceramics` finalizes `N7_Porcelain`, `CeramicsBench_Basic`, `CeramicsBench_Electric`, and `BasicCeramics`
- **THEN** both ceramics benches expose the Immersive Chefs porcelain-plate recipe after the provider's research
- **THEN** one completed native bill consumes exactly 4 porcelain and produces four ungraded plates whose Stuff is exactly `N7_Porcelain`

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

The table expresses ordinary material-volume units, not raw stack items. Every kitchenware recipe SHALL honor RimWorld's `ThingDef.smallVolume` convention: one physical unit of a small-volume Stuff contributes `0.1` recipe unit, while one physical unit of ordinary Stuff contributes `1`. Consequently, choosing vanilla Silver or Gold SHALL require ten times the physical stack count shown in the table: 60 Silver or Gold for a 6-unit cookware or chef's-knife requirement, 40 for a 4-unit plate requirement, and 20 for a 2-unit cutlery requirement. This conversion SHALL apply through the shared ingredient-value calculation to any compatible small-volume Stuff and SHALL NOT multiply wood, stone, steel, porcelain, adobe, plastic, or other ordinary-volume ingredients.

These costs SHALL remain benchmarked against same-era Core objects instead of being treated as isolated tuning constants: one Core wall consumes 5 material units and one Core steel knife consumes 30, so primitive cookware costs one wall-equivalent of stone, metal cookware costs only 1.2 wall-equivalents, a non-weapon chef's knife set costs 20% of the combat knife, and one complete place setting costs 1.5 material units. A future rebalance SHALL record its comparator Defs and ratios before changing the table.

Cookware and knife recipes SHALL use the completing pawn's Crafting skill to assign a vanilla `QualityCategory` to their output. Plate and cutlery recipes SHALL produce ungraded items regardless of crafter skill. The wood in each cookware recipe SHALL represent handles, spatulas, and related non-metal parts rather than a second selectable Stuff. Every ingredient requirement SHALL use a semantic player label such as `any stony material`, `any intermediate metal`, or `wood`; no generated bill requirement MAY expose an internal category such as `root`. Plate and cutlery recipes SHALL have no Immersive Chefs research prerequisite: progression comes from access to `CraftingSpot`, vanilla `Smithing`/`Electricity` for the smithies, and vanilla `Machining` for `TableMachining`. The machining recipes SHALL remain a late universal route even for materials also available at an earlier station.

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
- **THEN** the smithy produces four of each with bronze Stuff and no crafting-quality grade without requiring an Immersive Chefs research project

#### Scenario: Craft kitchenware from Silver or Gold
- **WHEN** the player restricts a 6-unit, 4-unit, or 2-unit metal kitchenware bill to vanilla Silver or Gold
- **THEN** the native bill requires and consumes exactly 60, 40, or 20 physical material units respectively
- **THEN** choosing an ordinary-volume eligible metal for the same recipe continues to require exactly 6, 4, or 2 physical units

#### Scenario: Craft a batch of wood place settings
- **WHEN** a pawn completes each wood place-setting recipe at a crafting spot
- **THEN** 4 wood yields four wood plates and 2 wood yields four stackable wood cutlery settings, with no crafting-quality grade

### Requirement: Portable culinary ware enters appropriate trader stock
Every portable Immersive Chefs ware item that a player can reasonably carry and own SHALL be sellable. Trader stock SHALL remain low and thematic: neolithic bulk-goods traders MAY carry primitive cookware and inexpensive wood or stone tableware; outlander bulk-goods traders MAY carry ordinary cookware, plates, cutlery, and chef's knife sets; and exotic-goods traders MAY rarely carry glitterworld cookware. Prepared ingredients, dirty ware, installed buildings, and research-locked station/appliance products SHALL not be injected as ordinary stock merely to satisfy sellability. Stock generators SHALL preserve Stuff, applicable cookware/knife quality, sanitation, and stack counts and SHALL not create optional-mod package coupling.

#### Scenario: Buy everyday kitchen supplies
- **WHEN** an eligible bulk-goods trader generates stock
- **THEN** it may contain a low number of era-appropriate cookware sets, plates, cutlery settings, or chef's knife sets using valid Stuff and, for cookware/knives, quality
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
- **WHEN** clean adobe, wood, vanilla-steel, and registered-ceramic plates are compared
- **THEN** their normalized intrinsic material-cleanliness scores are `5`, `10`, `50`, and at least `70`, respectively
- **THEN** marking the wood or adobe plate clean does not erase its intrinsic material penalty

#### Scenario: Sort Expanded Materials cookware by cleanliness
- **WHEN** equal-quality clean cookware made from stainless steel, bronze, and a neutral metal is compared
- **THEN** its effective Kitchen Cleanliness orders stainless steel above bronze above the neutral metal

#### Scenario: Choose unsafe lead for availability
- **WHEN** a player deliberately crafts kitchenware from lead
- **THEN** the recipe remains valid and its info card exposes lead's ordinary sanitation and culinary disadvantages without revealing any latent toxicity state

### Requirement: Unsafe ware uses the active toxic or radiation health provider
Lead and uranium SHALL be the only default kitchenware materials that contribute unsafe-material exposure. A covered humanlike ingestion SHALL calculate a small dose for each unsafe ware item actually used: `0.020` severity-equivalent units for cookware recorded on that serving, `0.015` for its plate, and `0.010` for its cutlery. At the default scale, an all-unsafe place setting therefore contributes `0.045` units per meal. Lead SHALL always add its component dose to Core's `ToxicBuildup` Hediff.

Core RimWorld 1.6 has no separate pawn radiation-sickness Hediff. Uranium SHALL therefore select exactly one enabled, exact-shape-supported provider in this priority order: `Dubwise.Rimatomics`, then `Katavrik.CrashLanding`, then Core `ToxicBuildup`. Exact supported Rimatomics SHALL receive the converted strength through public `Rimatomics.DubUtils.applyRads(Pawn,float)` using the inspected `0.0028758333` default conversion. Exact supported Crash Landing SHALL receive the unchanged severity-equivalent dose through its owned `HediffDef Rad` when Rimatomics did not claim the exposure. Rimatomics SHALL win when both are active, matching Crash Landing's own radiation-provider behavior and preventing double application. Lead and uranium components in one place setting SHALL be split between providers without combining, dropping, or applying either component twice. An active incompatible provider SHALL emit one bounded warning and fall through only if it was not partially invoked; if no radiation provider is admitted, uranium SHALL use Core toxic buildup.

Exposure SHALL occur once only after RimWorld reports positive nutrition ingestion. Crafting, carrying, reserving, cooking, inspection, aborted ingestion, safe kitchenware, chef's knives, missing/legacy cookware provenance, animals, and excluded hand-eaten foods SHALL add no unsafe-material health effect. The cooked serving SHALL preserve only its hidden cookware-material provenance through stack split/merge and save/load; the embedded physical plate and exact dining cutlery remain authoritative for their own material. The meal, cookware, plate, and cutlery inspect strings MUST NOT reveal a toxicity/radiation flag, predicted dose, or current latent health state. Pawn Health inspection SHALL remain owned by the active health provider: Core controls `ToxicBuildup` visibility, Rimatomics controls its native radiation lifecycle, and Crash Landing controls `Rad` stages and visibility.

`ToxicKitchenwareExposureScale` SHALL be a live setting with default `1.0` and range `0.0`–`3.0`. It SHALL multiply only the material-dose sum at ingestion, clamp before application, and SHALL NOT affect food-poisoning probability, food-poisoning attribution, material cleanliness, or culinary quality. Setting it to zero disables new toxic-ware exposure without deleting an existing pawn Hediff or rewriting stored meal provenance.

#### Scenario: Repeated lead service accumulates vanilla buildup
- **WHEN** a humanlike pawn eats a covered meal cooked with lead cookware from a lead plate using lead cutlery at default settings
- **THEN** the completed native ingestion adds exactly `0.045` severity to the pawn's vanilla `ToxicBuildup` Hediff once
- **THEN** no kitchenware or meal inspection reveals that hidden dose or the pawn's latent Hediff state

#### Scenario: Uranium plate contributes only when used
- **WHEN** a humanlike pawn completes ingestion from an actual uranium plate while the cookware and cutlery are safe and Rimatomics is absent
- **THEN** exactly the `0.015` plate dose is added to Core `ToxicBuildup` after positive nutrition ingestion
- **THEN** merely crafting, carrying, reserving, or inspecting the uranium plate adds nothing

#### Scenario: Rimatomics owns uranium radiation
- **WHEN** a humanlike pawn completes native ingestion from an actual uranium plate while exact supported Rimatomics is active
- **THEN** Immersive Chefs invokes `Rimatomics.DubUtils.applyRads` once with strength `0.015 / 0.0028758333`, allowing Rimatomics settings and resistance to own the resulting radiation state
- **THEN** Immersive Chefs adds no Core `ToxicBuildup` for that uranium plate and does not reveal the latent radiation state on the consumed meal or returned plate

#### Scenario: Crash Landing owns uranium radiation when Rimatomics is absent
- **WHEN** a humanlike pawn completes native ingestion from an actual uranium plate while exact supported `Katavrik.CrashLanding` is active and Rimatomics is absent or disabled
- **THEN** exactly `0.015` severity is added once to Crash Landing's owned `Rad` Hediff
- **THEN** Immersive Chefs adds no Core `ToxicBuildup` and does not reveal latent radiation on the meal or returned plate

#### Scenario: Rimatomics wins when both radiation providers are active
- **WHEN** exact supported Rimatomics and Crash Landing are both active for uranium ingestion
- **THEN** the uranium dose is applied once through Rimatomics and Crash Landing's `Rad` severity remains unchanged

#### Scenario: Mixed lead and uranium service splits providers
- **WHEN** supported Rimatomics is active and a humanlike pawn completes native ingestion of a serving whose actual used ware includes both lead and uranium components
- **THEN** only the summed lead-component dose is added to Core `ToxicBuildup`
- **THEN** only the summed uranium-component dose is converted to upstream strength and applied once through Rimatomics

#### Scenario: Changed Rimatomics shape falls back safely
- **WHEN** exact package `Dubwise.Rimatomics` is active but the public static `void DubUtils.applyRads(Pawn,float)` shape is missing or changed
- **THEN** no upstream method is partially invoked, one bounded compatibility warning identifies the disabled integration, and uranium tries exact enabled Crash Landing before using Core `ToxicBuildup`

#### Scenario: Changed Crash Landing radiation Def falls back safely
- **WHEN** exact package `Katavrik.CrashLanding` is active without admitted Rimatomics but its owned `HediffDef Rad` shape is missing or changed
- **THEN** uranium uses Core `ToxicBuildup`, no foreign Hediff is changed, and one bounded compatibility warning identifies the disabled radiation integration

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
Cutlery SHALL stack across Stuff materials when all sanitation/ownership state is identical. Its per-unit material, durability and count SHALL be preserved through reservations, carrying, splitting, merging, saving, and loading, and one eating job SHALL reserve exactly one setting per diner.

#### Scenario: Reserve one setting from a clean stack
- **WHEN** a diner selects one setting from a stack of ten clean steel cutlery items
- **THEN** one item is reserved and split for that diner while the remaining nine stay available to other pawns

### Requirement: Mixed tableware stacks remain truthful throughout use
Mixed-material plate and cutlery stacks SHALL retain an ordered per-unit material/durability ledger. Old homogeneous stacks initialize from their actual current Stuff, hit points and count. Native sanitation and ownership remain homogeneous stack boundaries. Split, partial merge, full-stack transfer, failed transfer and save/load SHALL conserve every physical unit. A cooking or dining session SHALL use the actual extracted unit for eligibility, comfort, toxicity and other effects. Eligible plate counts SHALL count only eligible materials, and collection SHALL revalidate those units. Whole-stack and partial-transfer value and mass SHALL reflect the units actually present or transferred; trading SHALL not price gold units as wood or vice versa. Damage SHALL not make hidden units immune or recover lost durability through splitting.

#### Scenario: Haul and split a mixed pile
- **WHEN** a pawn hauls equally clean steel and gold plates into one stack, then carries a subset away
- **THEN** both resulting stacks conserve the exact material, hit points and total count, including after save/load
- **AND** clean and dirty or differently owned units never merge.

#### Scenario: Select eligible plates beneath an ineligible top unit
- **WHEN** a lavish cooking bill draws from a mixed wood-and-gold plate stack whose first unit is wood
- **THEN** only actual gold units count toward the bill requirement and are extracted for plating
- **AND** the remaining wooden plates stay available for eligible uses without being converted.

#### Scenario: Wash mixed-material tableware
- **WHEN** native hand-washing or dishwasher jobs clean an admitted mixed batch
- **THEN** every cleaned unit retains its material and durability, and partial admission preserves both remainders.

#### Scenario: Damage a mixed pile without hiding fragile units
- **WHEN** a mixed pile takes native damage, including outdoor deterioration or fire
- **THEN** the actual damage subtracts from every unit and destroys only units whose remaining hit points reach zero
- **AND** whole-pile destruction occurs only when no unit survives, regardless of ledger order.
- **AND** the pile uses the highest contained flammability and deterioration rate for environmental exposure; all units in the exposed pile share the resulting damage, so placing a nonflammable or durable unit first cannot protect hidden wood.

#### Scenario: Render the size and contents of a physical pile
- **WHEN** a ground stack contains one, two, five, or more than five plates or cutlery settings
- **THEN** the renderer reuses the current sprites to display respectively one, two, five, or five actual material-aware units
- **AND** plates overlap vertically while cutlery forms a compact group alongside one another, without increasing the physical item count
- **AND** open dishwasher contents resolve those same individual materials.

### Requirement: Ungraded everyday tableware
Plates (including adobe and provider-material plates) and cutlery SHALL have no vanilla quality component. Newly crafted, traded, embedded, carried, and loaded tableware SHALL not show a quality suffix or quality stat. Existing saves SHALL load the same physical tableware, materials, counts, sanitation, and per-unit durability while discarding obsolete quality data through the native component loading path. Existing absolute HP SHALL remain unchanged (bounded by ordinary engine rules); no quality compensation, price refund, or new material conversion is introduced. Cookware and chef's knives retain their current quality behavior.

#### Scenario: Craft and reload ordinary tableware
- **WHEN** a pawn crafts plates or cutlery and the player saves and reloads the game
- **THEN** the items retain their material, quantity, sanitation and durability with no crafting-quality grade in their label or stats
- **AND** tableware formerly saved at different quality grades may now stack when their remaining state permits it.

### Future idea (not scheduled for implementation)
Separate **fine plates** and **fine cutlery** item types may provide a later luxury-tableware progression. This is an idea only: do not add Defs, recipes, research, textures, quality grades, or balance values for them in this change.
