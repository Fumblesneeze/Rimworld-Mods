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

When `Argon.ExpandedMaterials.Metals` is active, cookware, metal plates, silverware, and chef's knives SHALL accept appropriate enabled Stuff including `EM_Iron`, `EM_MildSteel`, `EM_TemperedSteel`, `EM_Lead`, `EM_Bronze`, `EM_Copper`, `EM_StainlessSteel`, and `EM_Titanium`. The mod SHALL derive or explicitly map sanitation, speed, comfort, craftsmanship, and culinary effects without inventing a brass Def that is not present.

#### Scenario: Stainless cookware is crafted

- **WHEN** Expanded Materials metals is active and a stainless cookware bill is completed
- **THEN** the result retains `EM_StainlessSteel` as Stuff and receives its mapped culinary and cleanliness modifiers

### Requirement: Masonry and plastics use explicit classifiers

When `Argon.ExpandedMaterials.Masonry` is active, `EM_AdobeBricks` SHALL enable the fixed adobe plate path even though it is not Stuff. When `Mlie.SimplySublimeABSPolymer` is active and compatible, ABS SHALL be classified explicitly as plastic instead of inferred from its broad Metallic, Woody, or Stony tags. Incompatible legacy packages `Argon.VMEuP` and `Argon.ExpandedMaterials.Stones` SHALL NOT be activated on RimWorld 1.6.

#### Scenario: ABS does not become metal by tag

- **WHEN** the supported ABS polymer package is active
- **THEN** its service ware uses the plastic recipe and stat category despite its overlapping Stuff tags

### Requirement: Hygiene adapters prioritize valid water fixtures

When `Dubwise.DubsBadHygiene` is active and its adapter validates, a reachable operational kitchen sink SHALL be the preferred hand-washing fixture and SHALL be considered connected only when its plumbing and operating requirements pass. Each dishwasher SHALL likewise require a supplied Dubs plumbing connection and, at cycle admission, atomically verify and debit one positive Def-configured water charge scaled to its captured plate-equivalent load. Insufficient water SHALL prevent admission; loss/restoration after admission SHALL pause/resume the same cycle without another debit; cancellation or removal SHALL not refund the admitted charge. Without that mod, or when its integration is deliberately set to `Off`, dishwashers SHALL use their abstracted base water behavior and recognized bowls, wells, hauled-water fixtures, and enabled water terrain SHALL remain ordered hand-washing fallbacks. If the package is active in `Auto` mode but shape validation fails, dishwashers SHALL fail closed, one actionable warning SHALL identify the adapter failure, and eligible non-Dubs hand-washing fallbacks SHALL remain available.

#### Scenario: Plumbed sink outranks terrain

- **WHEN** a valid Dubs kitchen sink and reachable water terrain are both available
- **THEN** the dishwashing work giver selects the kitchen sink unless reservations or danger make it invalid

#### Scenario: Plumbed dishwasher loses its supply

- **WHEN** Dubs Bad Hygiene is active and a dishwasher's supplied connection fails during a cycle
- **THEN** its exact contents and progress remain captured while processing pauses and the same cycle resumes automatically after supply returns

### Requirement: Restaurant integration owns active service jobs

When `Orion.Gastronomy` and its required `Orion.CashRegister` dependency are active, the adapter SHALL extend waiter/server jobs for silverware delivery, cold-meal reheating, and immediate dish clearing without replacing Gastronomy order ownership. When either shape validation or the integration setting fails, vanilla Immersive Chefs eater/cleaner jobs SHALL remain available.

#### Scenario: Gastronomy is downloaded but inactive

- **WHEN** Gastronomy files exist locally but its package ID is not in the active mod list
- **THEN** no Gastronomy adapter patch is installed

### Requirement: Variety integrations preserve provenance components

With `Evyatar108.VarietyMattersImprovedRedux`, `VanillaExpanded.VanillaFoodVarietyExpanded`, or other compatible food-variety mods active, Immersive Chefs SHALL preserve `CompIngredients` and unknown ThingComps through preparation, cooking, plating, stacking, reheating, and spoilage. Variety calculations SHALL continue to observe the original ingredient data unless a specified paste-preparation rule intentionally hides exact sources.

#### Scenario: Modded meal retains ingredient history

- **WHEN** a Vanilla Food Variety Expanded recipe produces a plated meal
- **THEN** its ingredient component and Immersive Chefs culinary components coexist after save/load and reheating

### Requirement: Nutrient-paste adapters preserve dispenser ownership

When `VanillaExpanded.VNutrientE` and `OskarPotocki.VanillaFactionsExpanded.Core` are active, the adapter SHALL add plate acquisition and supported prepared-paste output without replacing their dispenser networks or optional ingredient behavior. The base-game dispenser SHALL behave correctly when neither package is active.

#### Scenario: Vanilla dispenser remains supported

- **WHEN** no nutrient-paste overhaul package is active and a pawn has a clean plate
- **THEN** the vanilla dispenser produces a plated nutrient paste meal using the Immersive Chefs lifecycle

### Requirement: Processor use is preferred and identity preserving

When `syrchalis.processor.framework` is active and its expected local shape validates, Immersive Chefs SHALL use its timing, progress, presentation, and power-pause lifecycle for dishwashers through an adapter that returns each original dish with its Stuff, quality, hit points, ownership, stack metadata, and other components intact. The adapter MUST intercept the stock destructive fixed-output completion path for reusable dishes. When Processor Framework is absent, disabled, or incompatible, only this optional adapter SHALL be disabled and a behaviorally equivalent local dishwasher cycle SHALL remain available.

#### Scenario: Stock processor cannot round-trip a dish

- **WHEN** the supported installed Processor Framework path would destroy an input dish and create a Stuff-less fixed output
- **THEN** the Immersive Chefs adapter suppresses that output and returns the original dish clean with all non-sanitation state intact

#### Scenario: Processor integration fails its shape guard

- **WHEN** Processor Framework is active but no longer exposes the validated process lifecycle
- **THEN** one actionable warning disables only that adapter and the local identity-preserving dishwasher cycle remains available

### Requirement: Integration settings are explicit

Each supported optional integration SHALL expose an `Auto` or `Off` setting, defaulting to `Auto`, and changes SHALL take effect after restart because they alter patches, generated Defs, or classification caches.

#### Scenario: Player disables an installed integration

- **WHEN** a player changes an optional integration from `Auto` to `Off` and restarts RimWorld
- **THEN** the integration contributes no Defs, patches, or adapters while the optional mod remains otherwise untouched
