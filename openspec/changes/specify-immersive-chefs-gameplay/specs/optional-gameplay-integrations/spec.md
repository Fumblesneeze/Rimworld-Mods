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

When `Orion.Gastronomy` and its required `Orion.CashRegister` dependency are active, the adapter SHALL extend waiter/server jobs for cutlery delivery and immediate dish clearing without replacing Gastronomy order ownership. It SHALL additionally add cold-meal reheating only while Thermodynamics - Hot Meals is absent and Immersive Chefs owns temperature. When either shape validation or the integration setting fails, vanilla Immersive Chefs eater/cleaner jobs SHALL remain available.

#### Scenario: Gastronomy is downloaded but inactive

- **WHEN** Gastronomy files exist locally but its package ID is not in the active mod list
- **THEN** no Gastronomy adapter patch is installed

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

### Requirement: Dynamic Meal Texture Replacer retains ingredient-graphic ownership

When exact package `Thekiborg.DMTR` is active, Immersive Chefs SHALL load after it and SHALL preserve the finalized RimWorld 1.6 DMTR surfaces on covered meals: `DynamicMealTextureReplacer.Graphic_IngredientsVariant`, `DynamicMealTextureReplacer.ModExtension_DynamicMealTextureReplacer`, its atlas dimensions/mappings, attachment fallback, and vanilla `CompIngredients` input. Plate attachment and recovery, culinary quality, active-provider temperature change and reheating, sanitation, stacking, trading, caravan transfer, and save/load MUST NOT replace the DMTR graphic class, remove its extension, flatten its atlas result, or mutate ingredient provenance merely to select an Immersive Chefs texture.

Food Texture Variety package `Goat.Food.Texture.Variety` with core `Goat.Food.Texture.Variety.Core` SHALL retain equivalent graphic ownership over its own variety-meal Defs. If more than one upstream texture mod changes a shared meal Def, Immersive Chefs SHALL preserve the final graphic resolved by those mods' load order and SHALL NOT install a competing meal-graphic owner. The actual embedded plate remains governed by Immersive Chefs even when an upstream meal graphic contains a decorative serving-dish image; consuming, expiring, destroying, or transferring that meal SHALL conserve the real bound plate rather than infer one from pixels.

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

Exact active package `Mlie.DThermodynamicsHotMeals` SHALL suppress every Immersive Chefs-owned temperature and reheating surface before Def loading and Harmony initialization. The exclusion SHALL cover the Immersive Chefs microwave Def/assets/load folder, temperature state progression and UI, temperature mood and poisoning effects, automatic reheat selection, self-dining/patient/Gastronomy heating jobs and toils, caravan cooling, and all corresponding patches. Immersive Chefs SHALL declare load-after ordering for Thermodynamics, SHALL use no compile-time reference to assembly `Hot Meals`, and SHALL not patch or replace Thermodynamics Def `DMicrowave`, job `HeatMeal`, driver `DHotMeals.JobDriver_HeatMeal`, temperature comps, UI, settings, or Harmony owners.

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

### Requirement: Integration settings are explicit

Each supported optional integration that contributes Immersive Chefs Defs, patches, graphic owners, or runtime adapters SHALL expose an `Auto` or `Off` setting, defaulting to `Auto`. A passive compatibility guarantee that only preserves upstream ownership unchanged, such as DMTR graphic preservation, SHALL NOT add a meaningless toggle. A mandatory conflict-prevention exclusion, such as yielding temperature ownership to Thermodynamics, SHALL NOT expose a toggle that permits duplicate providers. Switching any other contributing integration to `Off` MAY fail closed immediately when an adapter can safely stop classifying new work without removing patches or generated Defs; all remaining changes, including enabling an adapter that was not initialized at startup, SHALL take effect after restart because they alter patches, generated Defs, graphic caches, or classification caches.

#### Scenario: Player disables an installed integration

- **WHEN** a player changes an optional integration from `Auto` to `Off` and restarts RimWorld
- **THEN** the integration contributes no Defs, patches, or adapters while the optional mod remains otherwise untouched
