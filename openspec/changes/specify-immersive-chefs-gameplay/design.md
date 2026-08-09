## Context

Immersive Chefs has a loadable RimWorld 1.6/Harmony implementation baseline. The requested simulation crosses recipes, reservations, Thing stacking, ingestion, rot, temperatures, work givers, linked buildings, caravans, Royalty expectations, and several independently optional mods. Harmony is the only required third-party mod. Local Workshop inspection found usable Expanded Materials metals and adobe plus the requested hygiene, restaurant, dispenser/printer, recipe-replacement, selection, storage, meal-content, and variety ecosystems. It did not find a suitable RimWorld 1.6 ceramic material mod or brass Def. The installed Vanilla Cooking Expanded family, Fried Meals, Adaptive Meal Bill, Overcooked Meals, RimCuisine 2, Replimat Meals, Meal Printer, and No Vanilla Meals are therefore classified through exact package/Def contracts rather than translated labels or generic `FoodType` guesses.

The inspected RimWorld 1.6 VTEX Variations package is `VanillaExpanded.VTEXVariations` (Workshop `2493234474`), depends on Vanilla Expanded Framework, and exposes building variation through `VEF.Buildings.CompProperties_RandomBuildingGraphic`; that upstream component is deliberately not assumed to support portable items. The installed ingredient-driven meal mod is Dynamic Meal Texture Replacer package `Thekiborg.DMTR` (Workshop `3388822990`): its 1.6 XML assigns `DynamicMealTextureReplacer.Graphic_IngredientsVariant`, atlas metadata in `DynamicMealTextureReplacer.ModExtension_DynamicMealTextureReplacer`, and a vanilla attachment fallback to covered meal Defs. Food Texture Variety package `Goat.Food.Texture.Variety` and core `Goat.Food.Texture.Variety.Core` are a separate system that primarily owns their own variety-meal Defs and graphic classes.

The supported Thermodynamics continuation is exact package `Mlie.DThermodynamicsHotMeals` (Workshop `2909103255`, inspected mod version `1.6.6`, assembly `Hot Meals`). It owns food-temperature comps, ambient diffusion, temperature thoughts, automatic heating injection, `HeatMeal`/`DHotMeals.JobDriver_HeatMeal`, and building `DMicrowave`. When that package is active, it is the exclusive provider for the whole hot/cold-meal and microwave concern. For the Immersive Chefs fallback microwave, the inspected supported reupload of Even More Linkables is package `hobbes.bamba.evenmorelinkables16` (Workshop `3536014093`); its tabletop Defs demonstrate `BuildingOnTop`, `blocksAltitudes/BuildingOnTop`, `building/isEdifice=false`, and `clearBuildingArea=false`. Immersive Chefs adopts those vanilla Def mechanics without depending on that mod and adds stricter table/workbench support validation.

This document defines the implementation architecture and acceptance contract; completed behavior is tracked in `tasks.md` and is accepted only after its in-game observation task is complete.

## Goals / Non-Goals

**Goals:**

- Make culinary tools, preparation, teamwork, serving ware, sanitation, temperature, and dining standards form one understandable simulation.
- Preserve meal and ware state across save/load, stacking, splitting, hauling, ingestion, rot, and optional-mod workflows.
- Keep optional integrations absent-safe and compatible by using package IDs, Def discovery, narrow patches, and adapters.
- Keep the colony playable under shortages, starvation, job interruption, and unsupported third-party recipes.
- Expose conservative settings with stable defaults and explicit restart behavior.

**Non-Goals:**

- Food waste, canning, fermentation, and other preservation systems. The data model will leave extension points, but no inactive feature will alter play.
- Drinks, drugs, baby food, animal feeding, knife washing, detergent, dish breakage, utensil wear, or general cross-contamination simulation beyond the explicit wild-water wash provenance.
- Player-visible caravan dishwashing jobs or caravan washing buildings; travel washing is an automatic journey abstraction.
- Replacing Variety Matters, food-variety content mods, or nutrient-paste overhaul logic.
- Compile-time references to optional mod assemblies.
- Ceramic/porcelain content or research, compatibility migrations between unreleased development schemas, and compatibility for food mods outside the explicit registry.

## Decisions

### 1. Persistent state belongs to small composable ThingComps

Use separate, versioned components for sanitation, embedded serving ware, preparation provenance, and culinary meal state. Components serialize stable Def/package identifiers and numeric snapshots rather than runtime object references. Each embedded plate binding includes Def, Stuff, quality, hit points, and sanitation so terminal recovery can return the exact surviving item. Stack compatibility SHALL include component state; split and merge operations SHALL preserve per-serving plate counts and reject lossy merges.

This is preferred to one global map registry because Things can leave maps, enter containers, split, rot, or be transformed. A registry is easier initially but becomes fragile across save/load and third-party holders.

### 2. Classification is explicit and extensible

Use DefModExtensions and configured Def-name/category lists for meal coverage, complexity, hand-eaten exclusions, fabrication tiers, plastics, ceramic-like materials, water sources, service ware, and compatible appliances. Built-in fallbacks cover vanilla materials. Optional definitions are added through package-ID/Def-presence-gated XML patches. Broad Stuff tags alone are insufficient because the locally installed ABS polymer is also tagged Metallic, Woody, and Stony.

The complexity table is deliberately exact: vanilla Simple recipe Defs map to `Simple`, Fine recipe Defs map to the internal `Advanced` tier, and Lavish recipe Defs map to `Elaborate`. Package-gated registries extend those tiers to the inspected Vanilla Cooking Expanded family, Fried Meals, Fast Meals, RimCuisine 2, Adaptive Meal Bill's selected concrete recipes, and other explicitly named products. There is no label, preferability, ingredient-count, translated-text, `FoodType`, or inheritance-only heuristic. Complexity classification is separate from meal coverage, so an unclassified external meal can preserve honest existing plate/provenance state without receiving a guessed multiplier or fabricated plate. Pemmican, packaged/travel survival meals, hardtack, canned/preserved foods, snacks, raw food, drinks, drugs, baby food, and printer nutrient bars are excluded.

Vanilla Cooking Expanded Simple products map to `Simple`; Fine products map to `Advanced`; Lavish and Gourmet products map to `Elaborate`. Bakery, Haute, Stews, Sushi, and Fried Meals contribute only their explicit full-meal recipe/product registry: preservation/canning, condiments, ingredient resources, cheeses, snacks, and drinks remain excluded. Fast Meals' ordinary fast meal is `Simple` and deluxe fast meal is `Advanced`, but their deliberately short native work amounts are exempt from the generic Fine/Advanced work multiplier. RimCuisine 2 explicitly adds thin pottage, thick pottage, rubaboo, pizza, and extravagant meal; its bulk recipes that still produce vanilla `MealSimple`, `MealFine`, or `MealLavish` are covered only while those product Defs remain finalized. Its preservation and non-meal modules remain upstream-owned and excluded. Adaptive Meal Bill is classified from the concrete recipe it selects at the native product boundary, never from the adaptive wrapper.

The same explicit classification also controls minimum plate material without changing the work-multiplier rule: Simple accepts every registered plate material; Advanced/Fine requires metal, registered plastic, or registered ceramic/porcelain; Elaborate/Lavish requires silver, gold, or registered ceramic/porcelain. An unclassified covered meal defaults to the Simple plate tier unless compatibility XML declares otherwise. Material eligibility is not inferred from translated labels.

### 3. Culinary work uses reservations and emergency degradation

Cooking jobs reserve the required cookware and clean plates before ingredients, then reserve the lead stove. Clean ware is preferred. Under an urgent recipe or when a pawn's food need crosses the configured emergency threshold, the system can use dirty ware or produce an unplated meal, with explicit risk and thought consequences; it MUST never create an infinite job loop or allow avoidable starvation.

Meal origin is explicit. A meal loaded from an older save, created by debug action, or spawned by unpatched mod code has no plate unless a real plate binding was serialized or later attached by the plating workflow; recovery never fabricates one. In contrast, covered meals generated as raider/visitor personal inventory or trader/settlement trade stock receive one real clean Poor-quality embedded plate per serving. Selection first prefers the least-luxurious admissible service class—primitive materials for Simple meals, ordinary metal/plastic service for Fine meals, and precious metal or registered ceramic for Lavish meals—then the least-market-value material within that class. This avoids treating vanilla Silver's low unit price as cheap everyday Fine service. Transferring or buying the meal transfers that exact plate and ownership state.

Player-visible custom Things use custom art rather than renamed vanilla resources or worktables. Each distinct item/building concept receives at least two independently generated candidates. Candidate review uses alpha-clean, final-scale previews composited against retained live RimWorld map captures, and the selected asset is then rendered by the real Def in a fresh game beside representative vanilla content. Custom workbenches and appliances use the same restrained, painted, map-scale visual language as Core production benches: an orthographic/top-down read, strong low-frequency silhouette, bounded contrast, restrained micro-detail, and comparable visual mass and transparent padding at the occupied footprint. Glossy front-elevation concept renders are rejected even when attractive in isolation.

Every rotatable custom building uses a complete `Graphic_Multi` family. North and south frames are authored for the unrotated footprint, east and west frames are separately authored/reframed vertical views of the same physical bench or appliance, and each direction preserves the same equipment identity and worker-facing layout. RimWorld's map camera does not rotate with the Thing: every cardinal frame therefore retains screen-consistent tabletop foreshortening, lighting, and a visible near-side vertical face while its equipment rotates in world space. No opposite frame is manufactured with a 180-degree raster transform. This is especially visible when the default southern interaction offset rotates north in `Rot4.South`; the reverse-facing sprite keeps the cabinet/apron on the screen-south edge while its equipment faces the northern worker.

The geometry is not left to image-generator judgment. A read-only extraction of Core 1.6.4871 `resources.assets` samples twelve standard production-bench families. Their 3x1 Defs consistently use a 3.5x1.5 draw canvas; the canonical clean tailoring bench resolves to a 224x96 texture at 64 px/cell, a centered 192x64 tabletop/footprint rectangle, and a 9 px screen-south underframe. Its east frame uses the same projection rotated in world footprint only: a 96x224 canvas, centered 64x192 tabletop, and the same 9 px underframe at the image bottom. The durable measurement record names all inspected assets and alpha bounds but does not copy Core art. Immersive Chefs authors at 4x that density: 2x1 families derive a 640x384/384x640 canvas around a 512x256/256x512 tabletop, while 3x1 families use 896x384/384x896 around a 768x256/256x768 tabletop. Both keep a 36 px underframe below the footprint. This shared projection template applies to the base and optional VTEX families; station-specific equipment is then composed inside or deliberately projecting from that common physical structure.

Optional VTEX building variants obey the same complete directional contract. Candidate review and live acceptance compare the custom building and an appropriate Core production bench at the same camera zoom, lighting, and screenshot crop in all four rotations. Stuffable kitchenware uses a neutral value structure, explicit material-region masks, and the appropriate Stuff-capable shader so wood, stone, common metal, and luxury metal remain recognizable without baking the Stuff color into the diffuse sprite. Non-selected candidates remain ignored review inputs and do not enter release packages.

The base package always provides one sensible masked fallback per portable ware and one complete fallback graphic per building. When `VanillaExpanded.VTEXVariations` and its validated Vanilla Expanded Framework shape are both active, a package-gated XML patch adds the real `VEF.Buildings.CompProperties_RandomBuildingGraphic` component to supported appliances and stations so their upstream-owned random/cycle/save behavior remains authoritative. Portable cookware, plates, cutlery, and chef's knives cannot use that building-only component; in the same exact active-mod group, a narrow Immersive Chefs graphic selector instead chooses a compatible texture family from product, Stuff material class, and sanitation state. Wood and registered stone receive silhouettes/textures that read as wood and stone rather than merely recolored metal, while the ordinary metal/plastic family remains the fallback. A dirty overlay or dirty family is cosmetic and follows the existing sanitation component without changing identity, statistics, stack admission, or cleaning behavior. Package absence, disabled validation, or changed VEF shape leaves every Def on its base fallback with no missing type or texture.

Cookware becomes dirty only after at least one actual cooking tick. Plates remain embedded per serving until ingestion, rot-to-nonmeal transformation, or explicit recovery. Cutlery is acquired by the eater or Gastronomy server and becomes dirty after eating.

### 4. Washing is one state machine with source adapters

A sanitation job selects, in order: an available powered dishwasher, a Dubs Bad Hygiene kitchen sink, another recognized connected appliance, a hauled-water source, then a reachable water terrain tile if enabled. A powered appliance is connected when its required power/fuel and water contract are satisfied; a Dubs appliance additionally requires its plumbing contract. Support stations are connected by RimWorld's linked-facility mechanic to the lead cooking building.

The domestic and industrial dishwasher expose 16 and 64 plate-equivalent units of capacity before the capacity setting. When the supported Processor Framework is active, an Immersive Chefs adapter SHALL use its process timing and presentation while intercepting the destructive completion path and returning each original Thing with Stuff, quality, hit points, ownership, stack metadata, and sanitation intact. A PF-absent or shape-incompatible installation uses the behaviorally equivalent local cycle rather than failing the base mod. A newly admitted item opens a Def-configured, save-persistent loading phase so the remaining pieces of a place setting can join the same batch; each further admission resets that phase, and no cleaning progress occurs during it. The closed batch then captures its contents, duration, and progress; loss of power or a temporary breakdown pauses it, and restoration or repair resumes the same cycle. With a validated Dubs Bad Hygiene adapter active, a dishwasher also requires a supplied plumbing connection and atomically debits one positive, load-scaled, Def-configured water charge when the loading phase closes and the cycle starts; insufficient water retains the admitted dirty batch without starting it, later supply loss pauses the cycle, and resume never charges again. Cancellation/removal does not refund already admitted water. If Dubs is active but its shape is incompatible, dishwashers fail closed while non-Dubs hand-washing fallbacks remain available. Explicit cancellation, deconstruction, or terminal destruction ejects every recoverable exact input still dirty and never fabricates replacement output.

### 5. Assistant effects accrue, not merely attach

When the lead cooking job starts its ingredient-hauling phase, linked sauce, meat, vegetable, and pastry stations advertise claims to available cooks. A claimed assistant contributes only on ticks when the lead pawn is actively cooking and the assistant is actively manning an applicable station. Contribution snapshots use the assistant's Cooking skill, are accumulated over time, and are capped by the configured assistant count/effect scale. Jobs release claims immediately on cancellation, loss of linkage, danger, higher-priority needs, or lead-job completion.

This avoids granting a bonus for walking to or idling at a station and keeps interruption behavior compatible with RimWorld's normal job priorities.

### 6. Meal outcomes use bounded snapshots

Culinary quality is a 0–100 snapshot calculated at completion from lead skill (30%), ingredient diversity (15%), ingredient quality (10%), preparation quality (15%), cookware (10%), knife (5%), and accrued assistants (15%). Inputs absent in vanilla, such as ordinary ingredient craftsmanship, default to the Normal midpoint instead of failing. Every component clamps its value; optional modifiers operate through documented hooks.

Without Thermodynamics - Hot Meals, meal temperature starts at 70 °C and moves toward ambient using a configurable two-hour baseline half-life. Refrigeration accelerates cooling by 2× and freezing by 4×. Temperature bands are steaming hot (>=55), warm (35–54.9), room temperature (15–34.9), cold (>0–14.9), and frozen (<=0). The fallback microwave restores a serving to 60 °C, removes five culinary-quality points, and adds 0.5 percentage points of poisoning risk. It is a one-cell non-edifice appliance at `BuildingOnTop` altitude, not a freestanding cabinet: construction requires one completed spawned table or workbench cell with an item/eating surface below it, does not clear or replace that support, and rejects bare floor, blueprints, frames, beds, storage, and other lookalikes. Its own power, reservation, interaction-cell, breakdown, and heating lifecycle remain independent from the support. The final custom poisoning contribution is capped independently of vanilla risk.

With exact package `Mlie.DThermodynamicsHotMeals` active, Immersive Chefs does not load its microwave Def or run any thermal initialization/progression, temperature gauge/thought/risk, automatic reheat selection, reheat job/toil, Gastronomy reheat insertion, or caravan temperature code. The shared culinary component may retain inert serialized fields for schema simplicity, but no Immersive Chefs runtime path reads or mutates them as temperature state. Thermodynamics owns temperature even if its inspected shape validation later warns, because enabling a fallback in the same process would recreate the conflict the guard exists to prevent. Culinary quality, ingredient provenance, physical plates, cutlery, sanitation, and their non-temperature poisoning inputs continue normally alongside Thermodynamics.

### 7. Expectations are satisfiable tiers, not item-label checks

Dining standards compare material-category scores, plate/cutlery comfort, meal complexity, and culinary quality against deterministic expectation and title tables. Royalty requirements combine component-wise with colony expectations and progress from Refined service at low titles to silver and then gold. Immersive Chefs does not invent ceramic when no compatible ceramic path exists: registered ceramic and stainless qualify as Refined when available, Good-or-better vanilla steel is the no-optional-mod Refined substitute, and base-game silver/gold keep later tiers attainable. Prisoners receive ware when available but no wealth expectation; colonists, slaves, and guests use their applicable expectation/title rules; animals are excluded. One combined thought reports the most important unmet dining standard to avoid mood stacking.

### 8. Optional integrations are adapters with safe no-op fallbacks

Integration activation requires both a known package ID and the expected Def/type/member shape. Reflection is isolated behind adapters and cached after validation. Failure disables only that adapter and logs one actionable warning. Harmony patches target the narrowest stable public behavior, use prepare guards, preserve original return values/state, and avoid transpilers where a prefix/postfix or Def patch can express the behavior.

The base implementation preserves `CompIngredients` and other unknown ThingComps. This is the main compatibility boundary for Variety Matters and food-variety mods. Dynamic Meal Texture Replacer remains authoritative for the ingredient-driven graphic of any Def it patched: Immersive Chefs loads after it, preserves its finalized `graphicData`, extension, attachment fallback, atlas inputs, and `CompIngredients`, and never bakes plate, culinary-quality, temperature, or sanitation state back into DMTR's ingredient choice. Food Texture Variety and its exact VCE add-ons retain the same ownership over their Defs. Its installed Core 1.6 comp does not serialize the transient selected `Graphic[]`; a shape-gated, setting-controlled adapter records only the stable start index of that upstream-selected three-graphic group and reconstructs the same references from Food Texture Variety's finalized `subGraphics` array on load. It never chooses an ingredient group itself, and missing/invalid state returns ownership to the original first-load branch. If multiple upstream texture mods touch one meal Def, Immersive Chefs preserves the graphic that their resolved load order produced rather than arbitrating between them. Prepared paste's hidden exact provenance MUST NOT be exposed through a texture selector.

Compatibility uses an ownership rule at every shared boundary: the upstream mod owns recipe choice, food search, dispenser feedstock/networking, output replacement, graphic selection, storage temperature, restaurant orders, or Def removal; Immersive Chefs adds ware reservation/lifecycle, culinary state, and sanitation exactly once around the final physical meal. Transformed outputs such as Overcooked Meals bind only after the final replacement exists. Dispensers and printers receive a real reserved plate before native dispensing, then bind that exact plate to the native product. Existing or externally spawned meals without a binding remain honestly unplated.

Replimat's colonist terminal path, animal-feeder path, and terminal survival-batch path are separate upstream workflows. Only the first enters Immersive Chefs' dining-session and ware lifecycle. When Common Sense is active, that completed colonist path queues both exact returned items on the responsible Cleaning-capable pawn; a supplied Dubs-connected dishwasher remains the first destination and admits the pair as 1.25 place-setting capacity. The animal feeder retains its native loose-feed products and animal ingestion behavior, while the terminal's native survival-batch dialog retains feedstock ownership and produces hand-eaten packaged meals. E2E confirmation of that version-specific native dialog uses a Gateway-owned exact type/assembly/field/callback allowlist; the product test names only the expected window type, and an unknown or drifted shape fails closed rather than exposing a generic reflective callback.

The compatibility E2E matrix is bounded by patch-surface risk rather than the power set of installed mods. Each row is one fresh process with an exact active package order, all tests for that row run sequentially, and cleanup removes roofs (including overhead mountain) before map contents. The maintained rows are: base inverse; VCE/Fried/Adaptive/Overcooked product collision; VCE plus Food Texture Variety add-ons/DMTR/Variety Matters/Vanilla Food Variety Expanded visual provenance; Fast Meals/Meals on Wheels/Prioritize Meals food selection; Replimat/Replimat Meals/Dubs/Common Sense sanitation; Meal Printer/Hospitality/Gastronomy service; Processor Framework/RimCuisine 2/No Vanilla Meals replacement; Adaptive Storage Framework alone as the incomplete optional chain; Adaptive Storage Framework/[sbz] Fridge fallback-temperature storage; and RimFridge/Thermodynamics temperature ownership. A full supported-package startup canary MAY supplement these rows but does not replace behavior assertions. Semantically conflicting ecosystems are separated: No Vanilla Meals does not share a behavior row with vanilla-Def-dependent Meal Printer or Adaptive Meal Bill, and Thermodynamics is tested as the sole thermal provider rather than alongside the fallback provider.

The installed [sbz] Fridge is exact package `sbz.NeatStorageFridge` (Workshop `3486264784`) and requires `adaptive.storage.framework` (Workshop `3033901359`). Its 1.6 Defs inherit `AdaptiveStorageBase` and declare `AdaptiveStorage.Extension.temperature` with `coolingOffset=100` and `coolingMin=-10`; the required framework assembly is `AdaptiveStorageFramework, Version=1.2.4.0`. Adaptive Storage patches `Thing.AmbientTemperature` for items registered in its `ThingClass` storage cells and activates that adjustment only while its native power/switch/fuel conditions pass. Immersive Chefs already evaluates a meal through `parent.AmbientTemperature`, so compatibility is deliberately passive: do not patch the holder, copy items, inspect the fridge label, or add a duplicate cooling multiplier. The exact group must instead prove native hauling into and out of the real holder, upstream power behavior, native save/load, and conservation of every per-serving/plate field. Thermodynamics remains a separate exclusive-provider row so this fallback group actually exercises Immersive Chefs against the upstream-adjusted ambient value.

Thermodynamics ownership is a package-presence exclusion rather than an adapter fallback. A pre-deserialization patch operation checks the exact active `ModContentPack` package ID and removes the Immersive Chefs fallback microwave node before Def construction; the same exact package identity suppresses every temperature-owned Harmony/runtime path before those features can initialize. Shape validation of `DMicrowave`, `HeatMeal`, `DHotMeals.JobDriver_HeatMeal`, and the inspected food-temperature comp family is diagnostic only: incompatibility emits one warning but never enables the competing Immersive Chefs temperature stack in that same process. No compile-time reference is allowed.

Hospitality support recognizes an arrived guest only through the active `Orion.Hospitality` adapter and its validated `Hospitality.Utilities.GuestUtility.IsArrivedGuest` shape; colony service ware remains the first source and the guest's inventory is the fallback. Vanilla Textures Expanded - Variations support is keyed to exact package `VanillaExpanded.VTEXVariations` plus the inspected VEF 1.6 building-graphic component shape; it uses no compile-time reference, loads after both optional packages, and disables only its cosmetic variation path when either shape is absent or incompatible.

Common Sense support is a map-only post-dining adapter. Local Workshop inspection identifies package `avilmask.CommonSense`, assembly `CommonSense`, public `CommonSense.Settings.adv_cleaning_ingest : bool`, and public static `CommonSense.Utility.IncapableOfCleaning(Verse.Pawn) : bool` as the compatibility shape; the implementation MUST revalidate that exact public shape without a hard assembly reference before enabling the adapter. After a completed self-eating job, the diner owns opportunistic cleanup of the exact dirty plate and cutlery from that serving. After `FeedPatient`, the nurse owns it instead. An accepting dishwasher outranks hand washing; an unavailable cleaning path leaves ware for ordinary cleaners without delaying dining, retaining reservations, or starting a retry loop. Gastronomy-owned clearing remains authoritative when both integrations are active, and caravan washing remains governed by the travel abstraction.

### 9. Travel and assisted dining reuse the same physical ware lifecycle

Eligible meals keep cooling while held by a caravan, using the caravan tile's outdoor temperature as ambient. A caravan diner uses the serving's exact embedded plate or, for an imported unplated serving, attaches an eligible loose caravan plate at dining selection; cutlery is selected from caravan inventory. After ingestion the same plate and cutlery return to caravan inventory. The journey then abstracts routine washing by marking those items clean with wild-water provenance. Wild-water washing is not equivalent to safe fixture or dishwasher cleaning: it leaves the item usable and clean while adding a small deterministic poisoning-risk input until a later safe wash replaces that provenance.

Map guests use available colony service ware first and may fall back to their own inventory. A child enters the ordinary dining workflow when vanilla allows independent eating; baby food, milk, and other toddler-feeding exclusions remain untouched. For patient feeding, the feeder acquires and carries the cutlery but the dining result belongs to the patient. A missing-cutlery thought applies only when that patient is conscious, and eating or feeding without cutlery creates one bounded vanilla dirt event at the actual eating location when a map exists.

Dirty ware remains ordinarily haulable and is not automatically forbidden. Player forbiddance remains authoritative. Two mutually exclusive special storage filters expose clean and dirty kitchenware so players can create dedicated wash-input and clean-service stockpiles; sanitation changes notify normal storage hauling so cleaned ware can leave dirty-only storage.

### 10. Settings separate restart-required classification from live tuning

| Setting | Default | Allowed values | Application |
|---|---:|---:|---|
| Ware requirement mode | Strict | Strict / Prefer / Off | Restart |
| Dirty-ware fallback | Urgent only | Never / Urgent only / Always | Live |
| Emergency hunger threshold | 15% | 5–35% | Live |
| Simple recipe time multiplier | 0.75 | 0.25–2 | Restart |
| Advanced recipe time multiplier | 2 | 1–5 | Restart |
| Elaborate recipe time multiplier | 3 | 1–8 | Restart |
| Prepared work reduction | 40% | 0–75% | Live |
| Prepared rot multiplier | 4 | 1–10 | Restart |
| Paste preparation quality | 20 | 0–50 | Live |
| Auto-call assistants | On | On / Off | Live |
| Maximum assistants | 4 | 0–4 | Live |
| Assistant effect scale | 1 | 0–3 | Live |
| Prefer dishwashers | On | On / Off | Live |
| Allow terrain handwashing | On | On / Off | Live |
| Dishwashing work scale | 1 | 0.25–4 | Live |
| Dishwasher capacity scale | 1 | 0.5–4 | Restart |
| Culinary quality | On | On / Off | Live |
| Quality mood scale | 1 | 0–2 | Live |
| Food-poisoning effect scale | 1 | 0–3 | Live |
| Maximum custom poison chance | 50% | 5–100% | Live |
| Meal temperature | On | On / Off | Live |
| Thermal half-life hours | 2 | 0.25–12 | Live |
| Auto-microwave below | 10 °C | -10–30 °C | Live |
| Microwave quality loss | 5 | 0–20 | Live |
| Microwave extra poison chance | 0.5 pp | 0–5 pp | Live |
| Colony dining standards | On | On / Off | Live |
| Royalty dining standards | On | On / Off | Live |
| Each optional integration | Auto | Auto / Off | Restart |

Restart-required settings alter generated Defs, recipe users, or classification caches. Live settings are read at job selection or outcome calculation. Save/load within the same development schema SHALL preserve state, but backward migration between unreleased schemas and uninstall cleanup are deliberately deferred until release readiness.

### 11. Progression comes from workstations and two kitchen researches

Kitchenware recipes have no Immersive Chefs research prerequisite. The actual vanilla workstation gates provide progression: a crafting spot supports primitive stone cookware and soft service ware; fueled/electric smithies support medieval cookware and intermediate metals; and the machining table supports modern cookware, chef's knives, and a late universal service-ware route. A data-driven fabrication tier distinguishes soft, intermediate, and modern materials; unknown compatible metals default to the smithy tier unless explicitly registered otherwise.

`ImmersiveChefs_Dishwashing` requires vanilla `Electricity` and unlocks the domestic dishwasher. `ImmersiveChefs_ProfessionalKitchens` requires `ImmersiveChefs_Dishwashing` and vanilla `Machining`, and unlocks the industrial dishwasher plus the prep, sauce, meat, vegetable, and pastry stations. The microwave uses vanilla `Electricity` directly. Glitterworld cookware has no recipe or research and appears only through trade or quest rewards. Ceramic/porcelain progression remains absent until a compatible provider is selected.

### 12. Playtest corrections use player-visible semantics and Core benchmarks

The complete pot/pan/lid abstraction is always player-facing `cookware set`; `tableware` means plates plus cutlery, while `kitchenware` is only the umbrella catalog/category. Ingredient requirements are rendered from product/tier semantics rather than the implementation filter summary, so internal categories such as `Root` never leak into bills.

Recipe costs are comparator decisions rather than isolated numbers. The retained Core 1.6 anchors are a 5-unit material wall and a 30-unit steel combat knife: primitive cookware costs 5 stone plus 1 wood, medieval/modern cookware 6 material plus 1 wood, the non-weapon chef's knife set 6 material, plates 4 material per four, and cutlery 2 per four. The stone path accepts finalized `Stony` Stuff after explicit registration/exclusion precedence so loaded material packs work without Def-name enumeration. Primitive stone cookware is a distinct product Def and sprite because it is a rough carved set, not a gray modern pan.

Trade injection follows product/era intent: neolithic bulk traders carry only primitive/soft wares, outlander bulk traders carry small quantities of ordinary portable wares, and exotic traders very rarely carry self-cleaning glitterworld cookware. All portable wares are sellable; prepared ingredients and installed buildings are not added as ordinary stock. Exact trader Defs and counts are regression-tested so the matrix does not silently grow.

Sanitation remains mechanically meaningful without leaking hidden state. Ordinary inspection hides the vanilla poisoned/not-poisoned string and wild-water provenance. If poisoning occurs, the largest positive contributor from the actual final calculation is retained only long enough to name the resulting cause. Hand-wash duration derives from the existing plate-equivalent abstraction. A cook blocked only by dirty cookware first executes the real washing job; the native right-click dirty override is explicit, one-job scoped, and still records contamination. The exact held cookware is rendered only during active recipe work.

Travel ownership is captured before holder transfer. World caravans continue automatic wild-water washing, while a visiting trader/guest on a colony map receives its own brought plate back dirty in personal inventory. Colony/Gastronomy service provenance instead returns colony ware to clearing.

Preview art uses a generated original parody composition plus deterministic text. The release source keeps a 1280x720 Workshop master and packages a 640x360 `About/Preview.png`; both are 16:9 PNGs below 1 MiB. Primitive cookware and preview selections each require at least two candidates and focused live rendering before acceptance.

## Risks / Trade-offs

- **[Stack metadata causes fragmentation]** → Quantize culinary quality and temperature for stack compatibility, preserve exact plate counts, and prefer correctness over forced merging.
- **[Patches collide with food overhauls]** → Preserve unknown components, patch narrow lifecycle seams, add prepare guards, and test supported combinations in-game.
- **[Assistant jobs create priority churn]** → Use short-lived station claims, bounded searches, cancellation checks, and cooldowns after failed assignment.
- **[Dirty ware makes food unavailable]** → Provide explicit urgent/starvation fallback and clear alerts rather than hard job failure.
- **[Ingredient hiding bypasses dietary rules]** → Paste preparation hides exact ingredient identities from the player-facing meal but retains broad meat/plant, ideology, allergy, and dietary flags for validation.
- **[Temperature updates cost too much]** → Store the last temperature/time/ambient sample and calculate lazily on inspection, job choice, ingestion, or holder transition.
- **[Water connectivity differs by mod]** → Put source recognition behind adapters and never assume a building is usable from Def name alone.
- **[Processor Framework loses dish identity]** → Intercept its stock output path behind a version/shape guard, retain the original items, and fall back locally only when PF is absent or incompatible.
- **[Royalty demands become impossible]** → Validate every tier against base-game or fixed-recipe materials and degrade missing optional categories to documented equivalents.

## Implementation Rollout

1. Introduce serialized components and settings behind feature toggles, initializing newly covered or imported Things to clean, Normal quality, and ambient temperature where no evidence exists.
2. Add kitchenware and buildings before enabling strict recipe requirements; verify Def generation and save round trips.
3. Enable ware lifecycle, then preparation/assistants, then quality/temperature/expectations in independently reversible slices.
4. Add optional adapters one product boundary at a time with absent-mod, changed-shape, exact-Def integration, and bounded multi-mod E2E checks.
5. Before every real-game verification, write an isolated `ModsConfig.xml` under a dedicated `-savedatafolder`. If any workflow ever touches the normal configuration, hash and back it up first, restore it in `finally`, and verify the restored hash even after failure. Only recorded process IDs may be terminated.
6. Rollback disables the relevant setting/adapter and removes its Harmony owner patches. A public save-migration and uninstall-cleanup plan is release work, not a pre-release implementation requirement.

## Open Questions

- Food preservation and food waste need separate future OpenSpec changes; waste hooks must distinguish edible leftovers, spoilage, discarded prep, and sanitation residues before implementation.
- Ceramic/porcelain remains data-driven and deferred until an actual supported provider is installed; no compatibility adapter invents that material.
- Food preservation and food waste remain future systems even where a supported meal mod also ships preserving recipes or outputs.

## Affected Mods

- **Immersive Chefs** — package ID `fumblesneeze.immersivechefs`; repository path `mods/ImmersiveChefs`.
