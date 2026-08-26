# Immersive Chefs player guide

Immersive Chefs is a pre-release RimWorld 1.6 mod that makes cooking, serving, eating, and washing reusable kitchenware part of the colony simulation. Harmony (`brrainz.harmony`) and XML Extensions (`imranfish.xmlextensions`) are its required third-party foundations. Every integration target below remains optional and is guarded by the active package ID and the supported RimWorld 1.6 Def or assembly shape.

## Loading the mod

Load Harmony before Core, then XML Extensions after Core and before Immersive Chefs. RimWorld's automatic ordering can place Immersive Chefs after the optional packages declared in its metadata; do not add the developer-only RimWorld Dev Gateway to an ordinary playthrough.

The practical first steps in a new colony are:

1. Make primitive cookware, plates, and cutlery at a crafting spot, or use smithies and machining tables as better materials become available.
2. Give Cooking-capable pawns access to clean cookware and eligible plates. Chef's knives are belt-slot apparel and are optional bonuses.
3. Assign at least one pawn to Cleaning so returned dishes can be washed. Dishwashers unlock through `Dishwashing`; the industrial machine and professional stations unlock through `Professional Kitchens` after `Machining`.
4. Use the clean/dirty kitchenware stockpile filters when separate service and wash-input storage is useful.

Simple meals accept any registered plate material. Fine/Advanced meals require metal, registered plastic, or registered ceramic such as Ceramics (Continued) porcelain. Lavish/Elaborate meals require silver, gold, or registered ceramic. Pemmican, packaged/travel meals, hardtack, preserved foods, snacks, raw food, drinks, drugs, baby food, and Meal Printer NutriBars remain hand foods.

## Settings

Open `Options > Mod settings > Immersive Chefs`. Restart-required settings change generated Defs, patches, or classification caches; changing them in a running game is not a completed configuration change until RimWorld is restarted. Live settings are read during job selection or outcome calculation.

| Setting | Default | Range or choices | Applies |
| --- | ---: | --- | --- |
| Ware requirement mode | Strict | Strict / Prefer / Off | Restart |
| Simple recipe time | 0.75× | 0.25–2× | Restart |
| Fine recipe time | 2× | 1–5× | Restart |
| Lavish recipe time | 3× | 1–8× | Restart |
| Prepared-food rot | 4× | 1–10× | Restart |
| Dishwasher capacity | 1× | 0.5–4× | Restart |
| Texture variation integration | Auto | Auto / Off | Restart |
| Show dirty ware textures | On | On / Off | Restart |
| Dirty-ware fallback | Urgent only | Never / Urgent only / Always | Live |
| Emergency hunger | 15% | 5–35% | Live |
| Prepared work reduction | 40% | 0–75% | Live |
| Paste preparation quality | 20 | 0–50 | Live |
| Automatically call cooking assistants | On | On / Off | Live |
| Maximum assistants | 4 | 0–4 | Live |
| Assistant effect | 1× | 0–3× | Live |
| Prefer dishwashers | On | On / Off | Live |
| Allow terrain handwashing | On | On / Off | Live |
| Dishwashing work | 1× | 0.25–4× | Live |
| Culinary quality | On | On / Off | Live |
| Quality mood | 1× | 0–2× | Live |
| Food-poisoning effect | 1× | 0–3× | Live |
| Maximum custom poison chance | 50% | 5–100% | Live |
| Meal temperature | On | On / Off | Live when Immersive Chefs owns temperature |
| Thermal half-life | 2 h | 0.25–12 h | Live when Immersive Chefs owns temperature |
| Automatically microwave below | 10 °C | -10–30 °C | Live when Immersive Chefs owns temperature |
| Microwave quality loss | 5 | 0–20 | Live when Immersive Chefs owns temperature |
| Microwave poison chance | 0.5 percentage points | 0–5 pp | Live when Immersive Chefs owns temperature |
| Colony dining standards | On | On / Off | Live |
| Royalty dining standards | On | On / Off | Live |

`Strict` blocks covered cooking until required clean ware is available, except for the configured emergency dirty-ware policy. `Prefer` uses ware when possible without making its absence a hard bill requirement. `Off` removes the new ware requirement after restart. The missing-kitchenware alert appears only for an owned, operational, opted-in kitchen with a runnable covered bill and no required ware at all; dirty, forbidden, or container-held ware counts as existing, so a “no clean ware” condition does not create alert spam.

When Thermodynamics - Hot Meals is active, its temperature system and microwave are exclusive. The five Immersive Chefs temperature controls are replaced by an ownership notice and cannot be used to enable a second temperature provider.

### Optional integration switches

These contributing adapters have individual `Auto`/`Off` switches and default to `Auto`: Processor Framework, Expanded Materials, Ceramics (Continued), ABS Polymer, Dubs Bad Hygiene, Gastronomy, Common Sense, Hospitality, Variety Matters, Vanilla Food Variety Expanded, Vanilla Expanded Framework, Vanilla Nutrient Paste Expanded, Adaptive Meal Bill, Overcooked Meals, Meals on Wheels, Prioritize Meals over Preserved Foods, Replimat plus Replimat Meals, Meal Printer, Food Texture Variety, Texture Variations, Pick Up And Haul, and Cook for Yourself.

Use `Off` to isolate a suspected integration problem, then restart RimWorld. The upstream mod remains loaded and untouched. Passive compatibility that only preserves another mod's ownership has no meaningless switch, and Thermodynamics conflict prevention intentionally has no switch that could enable duplicate providers.

## Compatibility matrix

Package-ID matching is case-insensitive; the canonical IDs below are the exact identities used by the implementation and verification groups. “Preserved” means the other mod remains authoritative and Immersive Chefs only carries its own ware/culinary state through that workflow.

### Materials, hygiene, service, and presentation

| Integration | Exact active package IDs | Immersive Chefs behavior |
| --- | --- | --- |
| Royalty | `Ludeon.RimWorld.Royalty` | Adds title-based dining expectations on top of colony expectations. |
| Processor Framework | `syrchalis.processor.framework` | Uses the supported processor timing/presentation for dishwashers while returning the original reusable Things; otherwise the local identity-preserving cycle remains available. |
| Expanded Materials | `Argon.CoreLib`, then `Argon.ExpandedMaterials.Metals` and/or `Argon.ExpandedMaterials.Masonry` | Registers audited metals and the fixed adobe plate path. No brass is invented when its Def is absent. |
| Ceramics (Continued) | `zal.ceramics` | Registers `N7_Porcelain` as a plate-only ceramic and adds a four-plate bill to both ceramics benches after `BasicCeramics`. Processor Framework and Vanilla Expanded Framework remain optional. |
| ABS Polymer | `Mlie.SimplySublimeABSPolymer` | Classifies the supported ABS Stuff explicitly as plastic despite its overlapping Stuff tags. |
| Dubs Bad Hygiene | `Dubwise.DubsBadHygiene` | Prefers a supplied kitchen sink for handwashing and requires/debits supplied plumbing for dishwashers. Shape failure makes Dubs dishwashers fail closed while non-Dubs handwashing fallbacks remain. |
| Gastronomy | `Orion.CashRegister`, `Orion.Gastronomy` | Waiters deliver cutlery and retain native order ownership; clearing is immediate and dishwasher-first. Reheating is added only when Immersive Chefs owns temperature. |
| Hospitality | `Orion.Hospitality` | Arrived guests prefer colony cutlery, then their own inventory, without changing Hospitality guest ownership. |
| Common Sense | `avilmask.CommonSense` | After completed dining, the diner—or nurse after feeding—claims the exact dirty setting and prefers an accepting dishwasher before handwashing. Gastronomy clearing remains authoritative when both are active. |
| Variety Matters | `Evyatar108.VarietyMattersImprovedRedux` | Preserves ingredient history and unknown meal components through the culinary and ware lifecycle. |
| Vanilla Food Variety Expanded | `OskarPotocki.VanillaFactionsExpanded.Core`, then `VanillaExpanded.VanillaFoodVarietyExpanded` | Preserves ingredient history and upstream variety behavior. |
| Vanilla Expanded Framework | `OskarPotocki.VanillaFactionsExpanded.Core` | Validated dependency for VCE, VNPE, Fried Meals, and VTEX paths; does not become a hard dependency. |
| Vanilla Nutrient Paste Expanded | `OskarPotocki.VanillaFactionsExpanded.Core`, `VanillaExpanded.VNutrientE` | Adds plate acquisition and prepared-paste output to the exact native tap while VNPE keeps pipe-network ownership. |
| Vanilla Textures Expanded - Variations | `OskarPotocki.VanillaFactionsExpanded.Core`, `VanillaExpanded.VTEXVariations` | Adds complete optional building families and Stuff/sanitation-aware portable variations; the base graphics remain the fallback. |

### Food ecosystems

| Ecosystem | Exact active package IDs and required chain | Ownership and scope |
| --- | --- | --- |
| Adaptive Meal Bill | `rabiosus.AdaptiveMealBill` | Adaptive chooses the concrete recipe; ware and culinary state attach once to the surviving final product. Keep it separate from No Vanilla Meals matrices. |
| Cook for Yourself | `lordfelix.CookForYourself` | Its own think nodes continue to choose one-off self/dependent meals, stations, recipes, ingredients, and delivery. The exact-shape adapter adds cookware/plate reservation, custom-driver work effects, culinary state, and ordinary dining ware lifecycle to covered meals without creating a bill. Baby food and every other excluded food remain upstream-only and kitchenware-free. The integration can be disabled independently with its `Auto`/`Off` setting. |
| Meals on Wheels Continued | `Memegoddess.MealsOnWheels` | Preserves borrowed/shuttle meal ownership and its embedded plate; the eater or nurse follows ordinary cutlery rules. |
| Replimat Meals | `sumghai.Replimat`, then `sumghai.ReplimatMeals` | Colonists bring eligible ware to the native terminal; Replimat retains feedstock, product choice, hidden ingredients, and no-poisoning behavior. Animal feed and packaged survival batches remain excluded. |
| Vanilla Cooking Expanded | `OskarPotocki.VanillaFactionsExpanded.Core`, `VanillaExpanded.VCookE`, plus any of `VanillaExpanded.VCookEBakery`, `VanillaExpanded.VCookEHaute`, `VanillaExpanded.VCookEStews`, `VanillaExpanded.VCookESushi`; Sushi also requires `VanillaExpanded.VCEF` | Explicit finalized full-meal registries classify Simple/Fine/Lavish/Gourmet products. Preservation, canning, condiments, ingredients, cheeses, snacks, non-meal desserts, and drinks remain upstream-only. |
| Fried Meals | `OskarPotocki.VanillaFactionsExpanded.Core`, `ucp.friedmeals` | Explicit Simple/Fine/Lavish/Gourmet fritter registry; upstream recipes and graphics remain authoritative. |
| Fast Meals | `Argon.CheapMeals` | Ordinary and Deluxe fast meals receive ware and dining rules while retaining their deliberately short native work amounts. |
| Food Texture Variety | `Goat.Food.Texture.Variety.Core`, `Goat.Food.Texture.Variety`; optional matching add-ons `Goat.Food.Texture.Variety.VECooking`, `Goat.Food.Texture.Variety.VEStew`, `Goat.Food.Texture.Variety.VESushi` | Preserves FTV graphic ownership and, in `Auto`, its selected texture group through current-schema save/load. Add-ons activate only beside the matching VCE packages. |
| Dynamic Meal Texture Replacer | `Thekiborg.DMTR` | Passive preservation: DMTR retains ingredient-driven graphic/atlas ownership and `CompIngredients`; Immersive Chefs does not select or flatten its texture. |
| Prioritize Meals over Preserved Foods | `seekiworksmod.no10` | Preserves upstream food ordering and caravan compensation; Immersive Chefs evaluates only the meal actually selected. |
| RimCuisine 2 | `syrchalis.processor.framework`, then `Mlie.RC2.Core`; meal coverage uses `Mlie.RC2.MaME`; `Mlie.RC2.BaBE` and `Mlie.RC2.SaSE` may coexist | Explicitly covers pottage, rubaboo, pizza, and extravagant meals. Hardtack, canned/preserved food, drinks, drugs, snacks, ingredients, and processing products remain excluded. |
| Meal Printer | `Mlie.MealPrinter` | Pawns bring a plate for native Simple/Fine outputs; printer configuration/feedstock remain upstream-owned and NutriBars are excluded. It fails closed when No Vanilla Meals removes required vanilla products. |
| RimFridge | `rimfridge.kv.rw` | Passive preservation: RimFridge owns storage and ambient temperature while exact ware and non-temperature culinary state survive storage/retrieval. |
| [sbz] Fridge | `adaptive.storage.framework`, then `sbz.NeatStorageFridge` | Passive preservation: Adaptive Storage owns the native holder, rendering, capacity, power/flick conditions, and adjusted item ambient temperature. Immersive Chefs follows that ambient value only while its fallback thermal system owns meal temperature; it adds no holder patch or second cooling multiplier, and exact serving/provenance/ware state survives transfer and save/load. |
| Overcooked Meals | `binchcannon.overcookedmeals` | The upstream replacement survives with copied ingredients; Immersive Chefs binds ware only to that final Thing and applies one explicit severe culinary penalty. |
| No Vanilla Meals | `Mlie.NoVanillaMeals` | Removed vanilla Defs are never resurrected or used by alerts/jobs. Remaining explicit replacement registries still work; external unplated meals remain safe and honestly unplated. |
| Thermodynamics - Hot Meals | `Mlie.DThermodynamicsHotMeals` | Exclusive temperature, thermal UI/thought/risk, reheating, and microwave owner. Immersive Chefs removes its own microwave and thermal paths while keeping quality, ingredients, ware, sanitation, and non-temperature risk. |

The maintained compatibility verification deliberately separates overlapping or incompatible concerns rather than loading every permutation together. Every Immersive Chefs group starts with Harmony + Core + XML Extensions:

1. Immersive Chefs alone after the common required prefix.
2. VEF + all installed VCE meal modules + Fried Meals + Adaptive Meal Bill + Overcooked Meals.
3. VEF + matching VCE modules + FTV Core/main/add-ons + DMTR + Variety Matters + Vanilla Food Variety Expanded.
4. Fast Meals + Meals on Wheels + Prioritize Meals over Preserved Foods.
5. Replimat + Replimat Meals + Dubs Bad Hygiene + Common Sense.
6. Hospitality + Meal Printer + Cash Register + Gastronomy.
7. Processor Framework + all RimCuisine 2 modules + No Vanilla Meals.
8. Adaptive Storage Framework alone, proving the incomplete optional chain remains safe.
9. Adaptive Storage Framework + [sbz] Fridge.
10. RimFridge + Thermodynamics - Hot Meals.
11. Cook for Yourself for covered self-cooking, conscious-patient feeding, interruption, and `Off` fallback around its custom non-bill job driver.
12. Biotech + Cook for Yourself for native baby-food cooking and `BottleFeedBaby` pass-through with strict kitchenware enabled and no kitchenware present.

Each group also includes Immersive Chefs and the developer-only Gateway in isolated verification. Meal Printer/Adaptive vanilla-product paths are intentionally not combined with No Vanilla Meals.

## Save behavior

Current development-schema saves made by the same build line are supported. Serialized meal servings retain culinary quality, ingredient/provenance snapshots, contamination, temperature fields when owned, and exact embedded plate bindings. Kitchenware retains Def, Stuff, quality, hit points, sanitation, and wash provenance. Stack split/merge rules preserve per-serving state rather than forcing a lossy merge.

Active cooking, dining, service, assistant, and dishwasher work is recovered after load. Process-local coordination is not serialized as if it were a Thing: held ware is returned safely and the ordinary job can retry. Dishwasher loading/progress and its captured exact inputs persist; loss of power, breakdown, or supplied Dubs water pauses the same cycle and restoration resumes it without charging the same water twice.

Meals already present before Immersive Chefs, or created through debug/third-party code without a serialized plate binding, remain unplated and return no fabricated plate. Covered raider/visitor inventory and trader/settlement stock receive their defined cheap plate only through the guarded external-generation path.

Before keeping an important colony, back it up and keep its mod list/settings stable. Migration between older unreleased development schemas and uninstall cleanup are not implemented. Do not remove Immersive Chefs from a valued save and expect embedded ware/components or active jobs to be rewritten automatically.

## Troubleshooting

### A covered cooking bill will not start

- In `Strict`, confirm clean cookware and an eligible plate exist, are reachable, allowed, and not reserved. Fine and Lavish meals have higher material requirements.
- Confirm the workbench is powered/fueled, owned, available, and the bill has valid ingredients. The kitchenware alert intentionally ignores non-runnable bills, raw-food-only colonies, campfires/grills, and `Prefer`/`Off` mode.
- For emergency behavior, check `Dirty-ware fallback` and `Emergency hunger`. Switch to `Prefer` or `Off` and restart only if you deliberately want to relax the system.

### Dirty ware is not being washed

- Assign a capable pawn to Cleaning, allow the ware, and provide a reachable destination.
- Check dishwasher power, research, capacity, loading/cycle state, and `Prefer dishwashers`. Dirty and clean stockpile filters are mutually exclusive and can be used to reveal incorrect hauling destinations.
- With Dubs Bad Hygiene in `Auto`, check a real supplied plumbing network. An incompatible Dubs shape deliberately disables its dishwasher path rather than pretending water exists.
- Handwashing falls through connected fixtures to water terrain only when `Allow terrain handwashing` is on. Terrain washing returns clean ware with wild-water provenance, which is usable but carries a small sanitation risk until a later safe wash.

### The microwave or temperature settings disappeared

This is expected when `Mlie.DThermodynamicsHotMeals` is active. Its `DMicrowave` and temperature system are the only provider; Immersive Chefs does not expose a switch that can create two competing systems.

### An optional integration reports a changed shape

Immersive Chefs logs one bounded warning and disables only the affected adapter. Set that integration to `Off`, restart, and confirm base behavior. When reporting the problem, include the exact active package IDs, mod versions, load order, and the complete warning—not copied Workshop DLLs or a save containing private data.

### A meal has no plate, or a texture mod shows a drawn plate

An imported/debug/third-party meal without an actual serialized binding is intentionally unplated. Conversely, a decorative plate in an upstream meal texture is not a physical Immersive Chefs plate. The inspector and the returned Thing after eating/expiry are authoritative.

### Dirty/variant graphics do not change

`Show dirty ware textures` and Texture Variations are restart-required. VTEX variation support also requires the exact supported Vanilla Expanded Framework shape. If another meal texture provider owns a Def, its resolved load-order graphic remains authoritative by design.

## Known limitations and deferred work

- This is pre-release: backward migration between unreleased schemas and uninstall cleanup are deferred until release preparation.
- Ceramics (Continued) porcelain is the only currently audited ceramic provider. Base silver/gold and Good-or-better steel keep the expectation tiers attainable without it; no imaginary ceramic or brass Def is created for other mods.
- Food preservation/canning and food waste are separate future systems. Compatible mods' preserved products remain deliberately outside Immersive Chefs meal/ware behavior.
- Compatibility is guaranteed only for the exact package chains and guarded shapes above. An upstream update may disable one adapter until its new shape is audited; unrelated base behavior should continue.
- A broad “all mods together” startup is a canary, not proof of every unsupported permutation. The maintained exact groups are the behavioral compatibility contract.

For development commands and evidence requirements, return to the repository [README](../README.md) and [testing-environment guide](TestingEnvironments.md). The accepted behavior contract lives in the [Immersive Chefs OpenSpec change](../openspec/changes/specify-immersive-chefs-gameplay/proposal.md).
