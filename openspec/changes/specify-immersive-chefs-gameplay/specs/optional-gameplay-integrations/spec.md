## ADDED Requirements

**Owning mod:** Immersive Chefs (`fumblesneeze.immersivechefs`) at `mods/ImmersiveChefs`.

### Requirement: Optional integrations never become hard dependencies

Harmony (`brrainz.harmony`) SHALL be the only required third-party mod. Every optional integration SHALL require a recognized active package ID plus expected Def or reflected member shape, SHALL avoid compile-time references to optional assemblies, and SHALL disable only itself with one actionable warning when validation fails.

#### Scenario: Optional mod is absent

- **WHEN** Immersive Chefs loads without any recognized optional package IDs
- **THEN** base behavior initializes without missing-assembly or missing-Def errors

#### Scenario: Recognized package has changed shape

- **WHEN** an active optional mod has the known package ID but its required Def or reflected member is absent
- **THEN** only that adapter is disabled and exactly one diagnostic identifies the incompatible integration

### Requirement: Supported food ecosystems use audited package identities

The compatibility registry and grouped test runner SHALL use the exact active package IDs and required dependency chains below. Downloaded folders, Workshop titles, translated labels, and assembly presence without an active package SHALL NOT activate an integration. The runner SHALL resolve the active RimWorld 1.6 content folder and record the loaded assembly identity for every adapter that reflects code.

| Ecosystem | Exact package IDs | Required chain or role |
| --- | --- | --- |
| Adaptive Meal Bill | `rabiosus.AdaptiveMealBill` | Harmony; adaptive recipe owner |
| Meals on Wheels Continued | `Memegoddess.MealsOnWheels` | Harmony; alternate food-source owner |
| Replimat / Replimat Meals | `sumghai.Replimat`, `sumghai.ReplimatMeals` | Replimat before its meal add-on |
| Vanilla Cooking Expanded | `VanillaExpanded.VCookE`, `VanillaExpanded.VCookEBakery`, `VanillaExpanded.VCookEHaute`, `VanillaExpanded.VCookEStews`, `VanillaExpanded.VCookESushi` | Harmony and `OskarPotocki.VanillaFactionsExpanded.Core`; Sushi also requires `VanillaExpanded.VCEF` |
| Fried Meals / Fast Meals | `ucp.friedmeals`, `Argon.CheapMeals` | Fried Meals requires Harmony and Vanilla Expanded Framework |
| Food Texture Variety | `Goat.Food.Texture.Variety.Core`, `Goat.Food.Texture.Variety`, `Goat.Food.Texture.Variety.VECooking`, `Goat.Food.Texture.Variety.VEStew`, `Goat.Food.Texture.Variety.VESushi` | Core before main; VCE add-ons only with their matching VCE packages |
| Prioritize Meals over Preserved Foods | `seekiworksmod.no10` | Harmony; ingestible-priority owner |
| RimCuisine 2 | `Mlie.RC2.Core`, `Mlie.RC2.MaME`, `Mlie.RC2.BaBE`, `Mlie.RC2.SaSE` | Processor Framework before Core; Core before modules; Harmony where declared |
| Meal Printer | `Mlie.MealPrinter` | Harmony; vanilla meal Defs remain present |
| RimFridge | `rimfridge.kv.rw` | Harmony; storage/ambient-temperature owner |
| Overcooked Meals | `binchcannon.overcookedmeals` | Harmony; final product replacement owner |
| No Vanilla Meals | `Mlie.NoVanillaMeals` | finalized vanilla meal/recipe removal owner |

#### Scenario: Workshop content is downloaded but inactive

- **WHEN** a supported Workshop folder and assembly exist but its exact package ID is absent from the active mod list
- **THEN** the compatibility registry, settings, Harmony adapter, and exact-mod tests for that package remain inactive

#### Scenario: Dependency chain is incomplete

- **WHEN** an add-on package is active without the required base package or expected finalized Defs
- **THEN** Immersive Chefs does not guess a partial integration and reports one actionable changed-shape/dependency diagnostic for that adapter

### Requirement: Expanded Materials metals extend stuff-aware recipes

When `Argon.ExpandedMaterials.Metals` is active, cookware, metal plates, cutlery, and chef's knives SHALL accept appropriate enabled Stuff including `EM_Iron`, `EM_MildSteel`, `EM_TemperedSteel`, `EM_Lead`, `EM_Bronze`, `EM_Copper`, `EM_StainlessSteel`, and `EM_Titanium`. The mod SHALL derive or explicitly map sanitation, speed, comfort, craftsmanship, and culinary effects without inventing a brass Def that is not present.

#### Scenario: Stainless cookware is crafted

- **WHEN** Expanded Materials metals is active and a stainless cookware bill is completed
- **THEN** the result retains `EM_StainlessSteel` as Stuff and receives its mapped culinary and cleanliness modifiers

### Requirement: Masonry and plastics use explicit classifiers

When `Argon.ExpandedMaterials.Masonry` is active, `EM_AdobeBricks` SHALL enable the fixed adobe plate path even though it is not Stuff. When `Mlie.SimplySublimeABSPolymer` is active and compatible, ABS SHALL be classified explicitly as plastic instead of inferred from its broad Metallic, Woody, or Stony tags. Incompatible legacy packages `Argon.VMEuP` and `Argon.ExpandedMaterials.Stones` SHALL NOT be activated on RimWorld 1.6.

#### Scenario: ABS does not become metal by tag

- **WHEN** the supported ABS polymer package is active
- **THEN** its service ware uses the plastic recipe and stat category despite its overlapping Stuff tags

### Requirement: Hygiene adapters prioritize valid water fixtures

When `Dubwise.DubsBadHygiene` is active and its adapter validates, a reachable operational kitchen sink SHALL be the preferred hand-washing fixture and SHALL be considered connected only when its plumbing and operating requirements pass. Each dishwasher SHALL likewise require a supplied Dubs plumbing connection. After the save-persistent loading window closes, cycle start SHALL atomically verify and debit exactly one positive Def-configured water charge scaled to the final captured plate-equivalent load. Insufficient water SHALL retain the admitted dirty batch without starting it; loss/restoration after cycle start SHALL pause/resume the same cycle without another debit; cancellation or removal SHALL not refund the admitted charge. Without that mod, or when its integration is deliberately set to `Off`, dishwashers SHALL use their abstracted base water behavior and recognized bowls, wells, hauled-water fixtures, and enabled water terrain SHALL remain ordered hand-washing fallbacks. If the package is active in `Auto` mode but shape validation fails, dishwashers SHALL fail closed, one actionable warning SHALL identify the adapter failure, and eligible non-Dubs hand-washing fallbacks SHALL remain available.

#### Scenario: Plumbed sink outranks terrain

- **WHEN** a valid Dubs kitchen sink and reachable water terrain are both available
- **THEN** the dishwashing work giver selects the kitchen sink unless reservations or danger make it invalid

#### Scenario: Plumbed dishwasher loses its supply

- **WHEN** Dubs Bad Hygiene is active and a dishwasher's supplied connection fails during a cycle
- **THEN** its exact contents and progress remain captured while processing pauses and the same cycle resumes automatically after supply returns

### Requirement: Restaurant integration owns active service jobs

When `Orion.Gastronomy` and its required `Orion.CashRegister` dependency are active, the adapter SHALL validate the installed waiter/diner job drivers and the static four-`TargetIndex` `Gastronomy.Waiting.Toils_Waiting.ClearOrder(...) -> Toil` factory before extending waiter/server jobs. It SHALL deliver cutlery before that native clear-order init action can end a free-service job, then always allow the native action to run. It SHALL retain Gastronomy order ownership, add immediate exact-item dish clearing, and add cold-meal reheating only while Thermodynamics - Hot Meals is absent and Immersive Chefs owns temperature. When either shape validation or the integration setting fails, vanilla Immersive Chefs eater/cleaner jobs SHALL remain available.

#### Scenario: Gastronomy is downloaded but inactive

- **WHEN** Gastronomy files exist locally but its package ID is not in the active mod list
- **THEN** no Gastronomy adapter patch is installed

#### Scenario: Free restaurant service ends during native order clearing

- **WHEN** a waiter reaches Gastronomy's native clear-order toil for an order that charges no silver
- **THEN** the distinct colony cutlery is attached to the diner before native clearing may end the waiter job, and native clearing still executes even if the optional delivery bridge fails

### Requirement: Hospitality guests preserve host and inventory ownership

When `Orion.Hospitality` is active and the locally supported `Hospitality.Utilities.GuestUtility.IsArrivedGuest` shape validates, Immersive Chefs SHALL recognize arrived guests without a compile-time Hospitality reference. Eligible guests SHALL use colony cutlery first and personal inventory cutlery only as fallback, while Hospitality retains visitor ownership, allowed-area, shopping, food-source, lord, and departure behavior. If validation fails, only the Hospitality adapter SHALL disable itself and ordinary non-Hospitality guest behavior SHALL remain available.

#### Scenario: Hospitality is downloaded but inactive
- **WHEN** Hospitality Continued files exist locally but `Orion.Hospitality` is absent from the active mod list
- **THEN** Immersive Chefs installs no Hospitality reflection or Harmony integration

#### Scenario: Arrived guest eats with colony service
- **WHEN** the validated Hospitality adapter identifies an arrived guest and reachable colony cutlery exists
- **THEN** the guest uses that setting without changing Hospitality's guest status, allowed area, or departure ownership

#### Scenario: Hospitality changes its guest utility shape
- **WHEN** `Orion.Hospitality` is active but the expected arrived-guest method is absent or incompatible
- **THEN** one actionable warning disables only Hospitality-specific recognition and Immersive Chefs continues its base dining behavior

### Requirement: Common Sense assigns opportunistic post-dining cleanup

When `avilmask.CommonSense` is active and the locally supported `CommonSense` assembly exposes public `CommonSense.Settings.adv_cleaning_ingest : bool` plus public static `CommonSense.Utility.IncapableOfCleaning(Verse.Pawn) : bool`, Immersive Chefs SHALL extend completed map dining with one opportunistic cleanup handoff. After self-eating, the diner SHALL claim the exact dirty plate and cutlery released by that serving; after `FeedPatient`, the nurse SHALL claim them instead of the patient. The responsible pawn SHALL prefer hauling the ware to a reachable, reservable, accepting dishwasher and SHALL otherwise use the ordinary Immersive Chefs hand-washing source order. This handoff SHALL start only after eating or feeding has committed, SHALL preserve exact Thing identity and sanitation state, and SHALL NOT delay ingestion, duplicate a Gastronomy-owned clearing job, apply on caravans, retain reservations when no path is viable, or retry indefinitely. If the package is absent, disabled, or shape-incompatible, ordinary Immersive Chefs cleaning remains available and only this adapter is disabled.

#### Scenario: Diner brings used service ware to a dishwasher

- **WHEN** a self-diner completes an eligible meal with Common Sense active and the exact resulting dirty plate and cutlery can enter an accepting dishwasher
- **THEN** that diner claims both items after eating and hauls them to the dishwasher before considering a hand-washing source

#### Scenario: Nurse clears after assisted feeding

- **WHEN** a nurse completes `FeedPatient` and the serving releases a dirty plate and used cutlery
- **THEN** the nurse, not the patient, claims that exact ware and follows the same dishwasher-first cleaning order

#### Scenario: Common Sense cleanup has no viable route

- **WHEN** every dishwasher and hand-washing source is unavailable, forbidden, full, reserved, disconnected, or unreachable
- **THEN** dining remains complete, the responsible pawn releases every cleanup reservation, and the dirty ware remains eligible for ordinary cleaning without an immediate retry loop

#### Scenario: Common Sense changes its supported shape

- **WHEN** `avilmask.CommonSense` is active but the expected public settings surface is absent or incompatible
- **THEN** one actionable warning disables only opportunistic post-dining cleanup and ordinary Immersive Chefs sanitation continues

### Requirement: Variety integrations preserve provenance components

With `Evyatar108.VarietyMattersImprovedRedux`, `VanillaExpanded.VanillaFoodVarietyExpanded`, `Thekiborg.DMTR`, `Goat.Food.Texture.Variety`, `Goat.Food.Texture.Variety.Core`, or other compatible food-variety mods active, Immersive Chefs SHALL preserve `CompIngredients` and unknown ThingComps through preparation, cooking, plating, stacking, active-provider reheating, and spoilage. Variety calculations and ingredient-driven graphics SHALL continue to observe the original ingredient data unless a specified paste-preparation rule intentionally hides exact sources.

#### Scenario: Modded meal retains ingredient history

- **WHEN** a Vanilla Food Variety Expanded recipe produces a plated meal
- **THEN** its ingredient component and Immersive Chefs culinary components coexist after save/load and any active-provider reheating

### Requirement: Adaptive Meal Bill preserves the selected concrete recipe

When `rabiosus.AdaptiveMealBill` is active and its supported `GenRecipe.MakeRecipeProducts` recipe-substitution shape validates, Adaptive Meal Bill SHALL remain the sole owner of selecting the concrete subrecipe. Immersive Chefs SHALL classify the selected concrete Simple, Fine, or Lavish recipe at the final product boundary, reserve one cookware set per cooking job and one admissible plate per actual serving, and attach culinary state and the exact plate to each final product once. It SHALL preserve the adaptive bill's ingredient selection, bill counters, product count, and completion semantics and SHALL NOT classify the wrapper recipe, invoke completion twice, or attach ware to an abandoned intermediate product.

#### Scenario: Adaptive Fine bill selects a concrete subrecipe

- **WHEN** an adaptive Fine bill substitutes a supported concrete Fine recipe during `GenRecipe.MakeRecipeProducts`
- **THEN** that concrete recipe receives the Advanced plate and culinary rules once while Adaptive Meal Bill retains recipe choice and bill accounting

#### Scenario: Adaptive product is later replaced

- **WHEN** another supported final-product owner replaces an adaptive recipe output before it is returned
- **THEN** Immersive Chefs attaches the plate and culinary state only to the surviving final physical meal

### Requirement: Meals on Wheels extends food location without owning dining ware

When `Memegoddess.MealsOnWheels` is active and its supported low-priority `FoodUtility.TryFindBestFoodSourceFor` postfix validates, it SHALL remain authoritative for selecting a soon-rotting meal from another colony pawn's inventory or an Odyssey shuttle after vanilla finds no source. Immersive Chefs SHALL preserve any existing embedded plate throughout the transfer, SHALL let the eater or nurse acquire cutlery through the ordinary dining path, and SHALL return the exact plate after completed ingestion. It SHALL NOT fabricate ware, take ownership of the source pawn/shuttle reservation rules, or move a plate separately before the meal is eaten.

#### Scenario: Pawn eats a borrowed plated meal

- **WHEN** Meals on Wheels selects a plated meal from another pawn's inventory
- **THEN** the meal reaches the eater with its plate still embedded, the ordinary cutlery policy runs, and the exact plate appears only after completed ingestion

### Requirement: Replimat meals require a real plate before native dispensing

When `sumghai.Replimat` and `sumghai.ReplimatMeals` are active and the exact public `ReplimatUtility.PickMeal(Pawn, Pawn) -> ThingDef` plus terminal `TryDispenseFood(Pawn, Pawn, ThingDef, int)` shapes validate from the expected assembly, Replimat SHALL retain ownership of network feedstock, meal choice, terminal availability, ingredients-hidden behavior, and its no-food-poisoning guarantee. Immersive Chefs SHALL ask the native picker for the concrete product during reservation and pass that same choice through the terminal's native optional product argument, rather than reclassifying the terminal placeholder after Replimat's search scope ends. An eligible humanlike getter SHALL reserve and bring one admissible clean plate and ordinary cutlery before requesting a Simple, Fine, or Lavish Replimat meal; Immersive Chefs SHALL attach that exact plate only after the terminal creates the native product. Failed dispensing SHALL release the same plate unchanged. Animals, hand-eaten travel products, and Replimat survival batches SHALL not require ware.

#### Scenario: Colonist requests a Fine Replimat meal

- **WHEN** a colonist reaches a valid terminal with a reserved Fine-admissible plate and native Replimat dispensing succeeds
- **THEN** one feedstock-owned Fine meal is created with that exact plate embedded and Replimat's hidden-ingredient and poisoning behavior unchanged

#### Scenario: Replimat cannot dispense

- **WHEN** the native terminal rejects the requested product after ware was reserved
- **THEN** no meal or plate is fabricated and the original clean plate becomes ordinarily available again

#### Scenario: Common Sense clears a completed Replimat setting

- **WHEN** a Cleaning-capable colonist completes native Replimat dining while Common Sense and a powered, supplied Dubs-connected dishwasher are active
- **THEN** the same responsible pawn queues both exact dirty returned items, carries them through ordinary `Doing dishes` jobs, and admits the one plate plus one cutlery setting to that dishwasher as exactly 1.25 place-setting capacity before any hand-washing route

#### Scenario: A Replimat animal feeder serves an animal

- **WHEN** a Replimat animal feeder creates its native loose feed and an animal eats that feed through an ordinary ingest job
- **THEN** no plate, cutlery, dining session, culinary state, tableware thought, or Immersive Chefs food-poisoning consequence is introduced and any nearby clean ware remains untouched

#### Scenario: A player batches Replimat survival meals

- **WHEN** the player uses the terminal's native batch-survival-meal command and confirms one packaged survival meal through its native dialog
- **THEN** Replimat consumes its own feedstock and creates the ordinary hand-eaten survival meal without embedded ware, culinary state, or changes to nearby clean ware

### Requirement: Vanilla Cooking Expanded and Fried Meals use an explicit full-meal registry

With the matching exact packages active, Vanilla Cooking Expanded base, Bakery, Haute Cuisine, Stews, Sushi, and Fried Meals SHALL be classified by explicit recipe/product Def registries finalized from their RimWorld 1.6 content. Supported Simple meals SHALL use `Simple`; Fine meals SHALL use `Advanced`; Lavish and Gourmet meals SHALL use `Elaborate`. Fried Meals SHALL classify its Simple and Fine fritters accordingly and SHALL classify Lavish fritters plus `VCE_CookFritterGourmet` as `Elaborate`. The registry SHALL exclude preservation/canning, condiments, raw or processed ingredients, cheeses, snacks, desserts that are not full meals, and drinks unless a separate explicit entry later brings one into scope. Upstream recipe workers, graphics, ingredient comps, research, and workstations SHALL remain authoritative.

#### Scenario: VCE Gourmet meal is cooked

- **WHEN** a registered VCE or Fried Gourmet recipe produces its final meal
- **THEN** the recipe receives Elaborate timing and plate eligibility while its upstream recipe worker, ingredients, and final ThingDef remain unchanged

#### Scenario: VCE preservation output is produced

- **WHEN** a VCE-family recipe produces a preserved ingredient, canned good, condiment, cheese, snack, or drink absent from the full-meal registry
- **THEN** Immersive Chefs adds no cookware, plate, cutlery, culinary, temperature, or dish lifecycle to that product

### Requirement: Fast Meals preserves its deliberate service speed

When `Argon.CheapMeals` is active, `CM_CookFastMeal` and its bulk recipe SHALL be covered Simple meals, and the Deluxe fast-meal recipes SHALL be covered Advanced meals. Their upstream work amounts SHALL be preserved as an explicit fast-service exemption from the generic Fine/Advanced work multiplier; every ordinary ware, plate-tier, sanitation, quality, poisoning, and dining rule SHALL still apply.

#### Scenario: Deluxe fast meal is cooked

- **WHEN** a cook completes a Deluxe fast-meal bill
- **THEN** the result uses Advanced plate eligibility and full ware lifecycle without replacing the mod's intentionally short work amount

### Requirement: Prioritize Meals over Preserved Foods retains food ordering

When `seekiworksmod.no10` is active, its finalized `optimalityOffsetHumanlikes`, preferability changes, and trader-caravan inventory compensation SHALL remain authoritative. Immersive Chefs SHALL add culinary, temperature-provider, plate, and sanitation evaluation after the upstream food-source ordering without resetting those fields or causing preserved food to outrank an otherwise valid perishable plated meal. Any trader/visitor meal added by that mod SHALL follow the ordinary external-stock rule and retain exactly one real plate only when it is a covered non-travel meal.

#### Scenario: Perishable meal competes with preserved food

- **WHEN** an eater can reach both an acceptable plated perishable meal and preserved food under the supported priority mod
- **THEN** upstream selection still prefers the perishable meal and Immersive Chefs applies dining consequences only to the selected serving

#### Scenario: A foodless trader caravan receives upstream compensation

- **WHEN** RimWorld's native trader-caravan arrival passes a humanlike trader with no acceptable food through the supported priority mod's finalized compensation postfix
- **THEN** that upstream postfix remains authoritative for adding preserved food and Immersive Chefs neither replaces it nor plates hand-eaten Pemmican or packaged survival meals
- **THEN** only an independently added covered non-travel meal follows the ordinary external-stock plate rule

### Requirement: RimCuisine 2 classifies meals without adopting preservation

When the supported Processor Framework, `Mlie.RC2.Core`, and `Mlie.RC2.MaME` chain is active, Immersive Chefs SHALL explicitly cover `RC2_ThinPottage` and `RC2_Rubaboo` as Simple, `RC2_ThickPottage` as Advanced, and `RC2_Pizza` plus `RC2_ExtravagantMeal` as Elaborate. RC2 recipes `CookSimpleMealBulk`, `RC2_CookFineMealBulk`, and `RC2_CookLavishMealBulk` SHALL use the ordinary Simple/Advanced/Elaborate registry only while their exact vanilla product Defs remain finalized; when No Vanilla Meals removes those products, Immersive Chefs SHALL neither resurrect them nor treat the now-invalid recipe path as a replacement. `Mlie.RC2.BaBE` and `Mlie.RC2.SaSE` SHALL be allowed to coexist but their drinks, drugs, processing products, and preservation outputs SHALL remain outside Immersive Chefs. `RC2_Hardtack`, `RC2_CannedMeal`, canned goods, dried/pickled food, snacks, and raw/processed ingredients SHALL not require plate, cutlery, or cookware.

#### Scenario: RimCuisine replacement suite runs without vanilla meals

- **WHEN** RimCuisine 2 and No Vanilla Meals are active and a registered pottage or extravagant meal is cooked, or a registered pizza enters through a valid upstream spawn/trade path
- **THEN** the registered remaining meal receives exactly one appropriate ware lifecycle without any lookup of a removed vanilla meal Def

#### Scenario: RimCuisine canned food is produced

- **WHEN** the cannery or another RimCuisine preservation process creates canned or preserved food
- **THEN** Processor Framework and RimCuisine retain full ownership and Immersive Chefs adds no plate, cookware, or dining requirement

### Requirement: Meal Printer plates ordinary printed meals but excludes NutriBars

When `Mlie.MealPrinter` is active, vanilla meal Defs remain finalized, and the exact public `Building_MealPrinter.GetMealThing() -> ThingDef` shape validates from the expected assembly, a humanlike pawn SHALL use that native configured product to reserve and bring an admissible plate before invoking the supported dispenser path. The printer SHALL retain configuration and feedstock ownership; Immersive Chefs SHALL attach the exact plate after native Simple or Fine output creation and release it unchanged on failure. `MealPrinter_NutriBar` SHALL be treated as a hand-eaten caravan/emergency product and SHALL never require or return plate or cutlery. Meal Printer SHALL fail its integration guard rather than dereference missing vanilla meal Defs when No Vanilla Meals is active.

#### Scenario: Printer creates a Fine meal

- **WHEN** a pawn brings a Fine-admissible plate and the configured printer successfully creates its native Fine meal
- **THEN** that exact plate is embedded once while printer feedstock/configuration remain upstream-owned

#### Scenario: Printed meal is served to an arrived guest

- **WHEN** the exact Meal Printer, Hospitality, Cash Register, and Gastronomy package group serves a printed plated meal through a native waiter order
- **THEN** the waiter delivers distinct colony cutlery, the guest's personal fallback remains clean, and both exact used colony items enter the Gastronomy-owned dishwasher-first clearing path

#### Scenario: Pawn takes a NutriBar

- **WHEN** a pawn prints or eats `MealPrinter_NutriBar`
- **THEN** Immersive Chefs does not seek, bind, dirty, or return plate or cutlery

### Requirement: RimFridge preserves ware while owning storage temperature

When `rimfridge.kv.rw` is active, its storage and `CompRefrigerator` behavior plus its `Thing.AmbientTemperature` patch SHALL remain authoritative. Immersive Chefs SHALL preserve embedded plate, culinary, ingredient, and sanitation components through storage and retrieval. If Thermodynamics - Hot Meals is also active, Thermodynamics SHALL remain the sole meal-temperature provider and Immersive Chefs SHALL not double-apply refrigerator or freezer cooling.

#### Scenario: Plated meal is stored in a powered fridge

- **WHEN** a plated meal enters and later leaves a working RimFridge building
- **THEN** its exact ware and non-temperature culinary state survive while the active upstream temperature owner alone observes the fridge environment

### Requirement: Overcooked Meals owns final product replacement

When `binchcannon.overcookedmeals` is active and its supported `GenRecipe.PostProcessProduct` replacement shape validates, it SHALL remain authoritative for replacing a cooked output with `OvercookedMeals_MealOvercooked`, its nutrition, thought, poisoning behavior, and copied ingredient data. Immersive Chefs SHALL bind the reserved plate and culinary state only to the final surviving output, apply one explicit severe culinary-quality penalty, dirty cookware once, and preserve the active temperature provider once. It SHALL NOT attach a plate to the destroyed original or duplicate Overcooked Meals' poisoning or thought effects.

#### Scenario: Cook burns a plated meal

- **WHEN** Overcooked Meals replaces the original recipe product
- **THEN** the destroyed original has no ware binding and the surviving overcooked meal holds the exact reserved plate, copied ingredients, one quality penalty, and one temperature-provider state

### Requirement: No Vanilla Meals is safe after finalized Def removal

When `Mlie.NoVanillaMeals` is active, Immersive Chefs SHALL not require `ThingDefOf.MealSimple`, `ThingDefOf.MealFine`, `ThingDefOf.MealLavish`, `ThingDefOf.MealNutrientPaste`, their recipes, scenarios, or instructions to remain non-null. Startup registries, Harmony preparation, alerts, settings, and jobs SHALL tolerate their finalized removal. Only remaining explicit package-gated meal entries SHALL receive new ware requirements; removed workbench recipes SHALL not trigger missing-kitchenware alerts. Existing/external serialized meals with no plate binding remain honestly unplated and crash-safe.

#### Scenario: Vanilla meals are removed and no replacement suite is active

- **WHEN** No Vanilla Meals finalizes its Def removals before Immersive Chefs initializes coverage
- **THEN** the game reaches the main menu and a map without null lookups, invented meals, or alerts for removed recipes

#### Scenario: Explicit replacement suite remains active

- **WHEN** No Vanilla Meals and a supported replacement ecosystem such as RimCuisine 2 are active
- **THEN** only the replacement registry receives ware and culinary behavior and no removed vanilla Def is used as a fallback

### Requirement: Dynamic Meal Texture Replacer retains ingredient-graphic ownership

When exact package `Thekiborg.DMTR` is active, Immersive Chefs SHALL load after it and SHALL preserve the finalized RimWorld 1.6 DMTR surfaces on covered meals: `DynamicMealTextureReplacer.Graphic_IngredientsVariant`, `DynamicMealTextureReplacer.ModExtension_DynamicMealTextureReplacer`, its atlas dimensions/mappings, attachment fallback, and vanilla `CompIngredients` input. Plate attachment and recovery, culinary quality, active-provider temperature change and reheating, sanitation, stacking, trading, caravan transfer, and save/load MUST NOT replace the DMTR graphic class, remove its extension, flatten its atlas result, or mutate ingredient provenance merely to select an Immersive Chefs texture.

Food Texture Variety package `Goat.Food.Texture.Variety` with core `Goat.Food.Texture.Variety.Core` SHALL retain equivalent graphic ownership over its own variety-meal Defs. Exact add-ons `Goat.Food.Texture.Variety.VECooking`, `Goat.Food.Texture.Variety.VEStew`, and `Goat.Food.Texture.Variety.VESushi` SHALL activate only beside their matching Vanilla Cooking Expanded package and SHALL retain their finalized graphic classes and serialized texture index. If more than one upstream texture mod changes a shared meal Def, Immersive Chefs SHALL preserve the final graphic resolved by those mods' load order and SHALL NOT install a competing meal-graphic owner. The actual embedded plate remains governed by Immersive Chefs even when an upstream meal graphic contains a decorative serving-dish image; consuming, expiring, destroying, or transferring that meal SHALL conserve the real bound plate rather than infer one from pixels.

#### Scenario: DMTR meal is cooked and reheated
- **WHEN** a covered vanilla meal is cooked from mapped ingredients with DMTR active, receives an embedded physical plate, and passes through the active provider's cooling and reheating workflow
- **THEN** DMTR continues to render the ingredient-appropriate atlas region while the same plate binding and Immersive Chefs culinary components survive unchanged and the active temperature provider retains sole ownership of its state

#### Scenario: Unplated imported meal has an ingredient texture
- **WHEN** debug or third-party code spawns a DMTR-rendered meal with `CompIngredients` but no serialized plate binding
- **THEN** the meal renders from its ingredients, remains honestly unplated, and returns no plate after ingestion or expiry

#### Scenario: Prepared paste hides exact ingredients
- **WHEN** a meal is cooked from prepared paste whose exact source provenance is intentionally hidden
- **THEN** DMTR or another ingredient texture selector sees only the permitted exposed ingredient data or its fallback and does not reveal hidden feedstock through the rendered atlas choice

#### Scenario: DMTR and Food Texture Variety are both active
- **WHEN** a DMTR-patched vanilla meal and a Food Texture Variety-owned meal Def coexist in one exact active-mod group
- **THEN** each retains its upstream graphic class and ingredient behavior while both participate in the same Immersive Chefs plate, culinary-quality, and sanitation lifecycle and whichever temperature provider is active remains its sole owner

### Requirement: Thermodynamics - Hot Meals is the exclusive temperature provider

Exact active package `Mlie.DThermodynamicsHotMeals` SHALL suppress every Immersive Chefs-owned temperature and reheating surface before Def deserialization and Harmony initialization. The exclusion SHALL remove the Immersive Chefs microwave Def before construction so its assets are never resolved, and SHALL cover temperature state progression and UI, temperature mood and poisoning effects, automatic reheat selection, self-dining/patient/Gastronomy heating jobs and toils, caravan cooling, and all corresponding patches. Immersive Chefs SHALL declare load-after ordering for Thermodynamics, SHALL use no compile-time reference to assembly `Hot Meals`, and SHALL not patch or replace Thermodynamics Def `DMicrowave`, job `HeatMeal`, driver `DHotMeals.JobDriver_HeatMeal`, temperature comps, UI, settings, or Harmony owners.

Package presence is sufficient to suppress the fallback even if diagnostic validation of the inspected 1.6.6 shape fails; running two providers is less safe than leaving a broken optional provider visible to its owner. One bounded warning MAY report the changed shape. Culinary quality, ingredients, plating, cutlery, sanitation, dishwashing, expectations, and non-temperature risk SHALL remain active and coexist with Thermodynamics state. This conflict-prevention rule is mandatory and SHALL not expose an `Off` switch that allows duplicate temperature providers.

#### Scenario: Only one microwave provider loads
- **WHEN** Thermodynamics - Hot Meals and Immersive Chefs are both active
- **THEN** the finalized Def database contains `DMicrowave` and not `ImmersiveChefs_Microwave`, and the Immersive Chefs assembly owns no active heating or temperature patch

#### Scenario: Non-temperature gameplay coexists
- **WHEN** Thermodynamics heats a plated meal that later passes through Immersive Chefs dining and sanitation
- **THEN** Thermodynamics' temperature state is untouched and the exact physical plate/cutlery, culinary quality, contamination, thoughts, and cleaning lifecycle each occur once under their respective owner

### Requirement: Nutrient-paste adapters preserve dispenser ownership

When `VanillaExpanded.VNutrientE` and `OskarPotocki.VanillaFactionsExpanded.Core` are active, the adapter SHALL add plate acquisition and supported prepared-paste output without replacing their dispenser networks or optional ingredient behavior. Support SHALL be limited to finalized Def `VNPE_NutrientPasteTap`, runtime type `VNPE.Building_NutrientPasteTap` from assembly `VNPE`, its direct `RimWorld.Building_NutrientPasteDispenser` base, and exactly one `PipeSystem.CompProperties_Resource`. The product assembly MUST NOT reference VNPE or PipeSystem directly, MUST NOT accept unrelated method-name lookalikes, and SHALL invoke the shared vanilla dispenser methods so VNPE's own Harmony prefixes retain pipe-network ownership. A disabled integration setting or a guarded native invocation failure SHALL reject only the VNPE tap. The base-game dispenser SHALL behave correctly when neither package is active or VNPE support is disabled.

#### Scenario: Vanilla dispenser remains supported

- **WHEN** no nutrient-paste overhaul package is active and a pawn has a clean plate
- **THEN** the vanilla dispenser produces a plated nutrient paste meal using the Immersive Chefs lifecycle

#### Scenario: Native VNPE tap dispenses prepared paste

- **WHEN** a player orders prepared cooking paste from the exact powered VNPE tap connected to a filled native vat
- **THEN** VNPE consumes one unit from its pipe network and Immersive Chefs creates paste-derived prepared ingredients through the ordinary pawn job

#### Scenario: VNPE-shaped lookalike is present

- **WHEN** a Def, runtime type, assembly, base type, or pipe component differs from the supported finalized identity
- **THEN** the guarded adapter rejects it without reflective fallback or disabling the base vanilla dispenser

### Requirement: Processor use is preferred and identity preserving

When `syrchalis.processor.framework` is active and its expected local shape validates, Immersive Chefs SHALL use its timing, progress, presentation, and power-pause lifecycle for dishwashers through an adapter that returns each original dish with its Stuff, quality, hit points, ownership, stack metadata, and other components intact. The adapter MUST intercept the stock destructive fixed-output completion path for reusable dishes. When Processor Framework is absent, disabled, or incompatible, only this optional adapter SHALL be disabled and a behaviorally equivalent local dishwasher cycle SHALL remain available.

#### Scenario: Stock processor cannot round-trip a dish

- **WHEN** the supported installed Processor Framework path would destroy an input dish and create a Stuff-less fixed output
- **THEN** the Immersive Chefs adapter suppresses that output and returns the original dish clean with all non-sanitation state intact

#### Scenario: Processor integration fails its shape guard

- **WHEN** Processor Framework is active but no longer exposes the validated process lifecycle
- **THEN** one actionable warning disables only that adapter and the local identity-preserving dishwasher cycle remains available

### Requirement: Compatibility verification uses a bounded exact-mod matrix

The integration runner SHALL group E2E tests by declared exact package requirements and exclusions, launch one fresh RimWorld process per distinct ordered group, run that group's tests sequentially, and aggregate the results. Before each E2E case it SHALL remove roofs including overhead mountain, then clear map contents and create only the declared fixture. The maintained behavior matrix SHALL contain these groups:

1. Core, Harmony, Immersive Chefs, and Gateway as the base inverse.
2. Vanilla Expanded Framework; all installed Vanilla Cooking Expanded meal modules; Fried Meals; Adaptive Meal Bill; Overcooked Meals; Immersive Chefs; and Gateway for the shared final-product boundary.
3. Vanilla Expanded Framework; matching Vanilla Cooking Expanded modules; Food Texture Variety Core/main/VCE add-ons; Dynamic Meal Texture Replacer; Variety Matters; Vanilla Food Variety Expanded; Immersive Chefs; and Gateway for ingredient and graphic provenance.
4. Fast Meals; Meals on Wheels; Prioritize Meals over Preserved Foods; Immersive Chefs; and Gateway for food search, mobile holders, and fast-work exemptions.
5. Replimat; Replimat Meals; Dubs Bad Hygiene; Common Sense; Immersive Chefs; and Gateway for dispenser ware and cleanup.
6. Hospitality; Meal Printer; Cash Register; Gastronomy; Immersive Chefs; and Gateway for printed and served meals. Hospitality precedes Meal Printer per the printer's declared load-after rule, and Cash Register precedes Gastronomy per Gastronomy's required dependency.
7. Processor Framework; all RimCuisine 2 modules; No Vanilla Meals; Immersive Chefs; and Gateway for replacement Defs and preservation exclusions.
8. RimFridge; Thermodynamics - Hot Meals; Immersive Chefs; and Gateway for single-owner temperature behavior.

Every named supported food mod SHALL appear in at least one maintained exact group. Host tests SHALL cover pure policy and package grouping, loaded main-menu integration tests SHALL verify finalized Defs and Harmony ownership, and E2E tests SHALL prove native player-observable cooking, dispensing, selection, serving, storage, and eating behavior. A broad all-supported startup canary MAY be added, but it SHALL NOT substitute for these behavioral groups or claim that every unsupported permutation is compatible.

#### Scenario: Tests have the same exact mod requirements

- **WHEN** multiple E2E cases declare the same ordered required and excluded package set
- **THEN** the runner launches one fresh game process and executes those cases sequentially with isolated map cleanup between them

#### Scenario: Incompatible ecosystems are requested

- **WHEN** one test requires No Vanilla Meals while another requires vanilla-Def-dependent Meal Printer or Adaptive Meal Bill
- **THEN** the runner creates separate groups rather than loading the conflicting requirements together

#### Scenario: Upstream update changes a shared patch surface

- **WHEN** a periodic compatibility run detects a changed Def, reflected member, Harmony owner, final product, or observable outcome
- **THEN** the affected exact group fails with retained artifacts while unrelated groups continue and are reported independently

### Requirement: Integration settings are explicit

Each supported optional integration that contributes Immersive Chefs Defs, patches, graphic owners, or runtime adapters SHALL expose an `Auto` or `Off` setting, defaulting to `Auto`. A passive compatibility guarantee that only preserves upstream ownership unchanged, such as DMTR graphic preservation, SHALL NOT add a meaningless toggle. A mandatory conflict-prevention exclusion, such as yielding temperature ownership to Thermodynamics, SHALL NOT expose a toggle that permits duplicate providers. Switching any other contributing integration to `Off` MAY fail closed immediately when an adapter can safely stop classifying new work without removing patches or generated Defs; all remaining changes, including enabling an adapter that was not initialized at startup, SHALL take effect after restart because they alter patches, generated Defs, graphic caches, or classification caches.

#### Scenario: Player disables an installed integration

- **WHEN** a player changes an optional integration from `Auto` to `Off` and restarts RimWorld
- **THEN** the integration contributes no Defs, patches, or adapters while the optional mod remains otherwise untouched
