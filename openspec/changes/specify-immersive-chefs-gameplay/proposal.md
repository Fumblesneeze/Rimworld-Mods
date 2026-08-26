## Why

Vanilla cooking largely treats tools, service ware, preparation, kitchen teamwork, and meal condition as invisible abstractions. Immersive Chefs needs one coherent, testable gameplay contract before those interacting systems are implemented, especially because several integrations are optional and meal metadata must survive stacking, saving, hauling, eating, and spoilage.

## What Changes

- Specify stuff-aware cookware, plates, cutlery, and belt-worn chef's knives with cleanliness, comfort, speed, craftsmanship, culinary-quality effects, workstation progression, primitive stone cookware, and trade-only self-cleaning glitterworld cookware.
- Specify ware reservation, consumption, dirty-state lifecycle, fire-aware plate conservation, hand washing, Processor-backed automatic dishwashers, power/water interruption behavior, preferential hauling to connected appliances, and actionable supply alerts.
- Specify origin-safe legacy/external meals, cheap embedded plates for generated visitor/raider inventories and trader stock, and increasing plate-material requirements for Simple, Fine, and Lavish meals.
- Specify prepared ingredients, nutrient-paste preparation, rapidly perishable provenance, and preparation-quality effects.
- Specify manned linked kitchen stations whose assistants contribute only while the lead cook is actively cooking.
- Specify culinary quality, temperature, reheating, poisoning risk, dining thoughts, expectations, and Royalty standards.
- Specify caravan cooling and reusable tableware, guest and Hospitality ware sourcing, child and patient feeding, wild-water sanitation provenance, missing-cutlery dirt, and clean/dirty stockpile filters.
- Specify optional integrations for the locally installed material, hygiene, restaurant, dispenser/printer, recipe-replacement, food-selection, storage, variety, and meal-content ecosystem without making those integration targets hard dependencies. XML Extensions is the required declarative package-ID patch engine.
- Specify an absent-safe Pick Up And Haul integration that gathers a nearby hand-washing batch, washes each physical dish separately, and returns the clean batch through that mod's tracked inventory/unload workflow.
- Specify an absent-safe Cook for Yourself integration that preserves its one-off self/dependent meal decisions while routing its custom non-bill cooking job through the ordinary Immersive Chefs cookware, plating, quality, assistance, and dining lifecycle.
- Make every distributed Immersive Chefs player-facing string localizable and ship complete context-authored English, German, Spanish, French, Simplified Chinese, and Russian catalogs guarded by the repository release checks.
- Specify player-facing settings and safe emergency fallbacks so the simulation cannot deadlock or starve pawns.
- Specify `Dishwashing` and `Professional Kitchens` research while leaving ordinary tableware progression to the crafting spot, smithies, and machining table.
- Replace every player-visible placeholder texture with selected custom art, require multiple candidates per asset, compare candidates at game scale against RimWorld's live visual context, and ship only the selected alpha-clean result.
- Correct playtest findings by Core-benchmarking recipe costs, broadening stony material discovery, adding thematic low trader stock, distinguishing primitive cookware art, conserving personally owned visitor-caravan dishes, hiding latent sanitation/poison diagnostics, attributing actual poisoning, scaling washing work, letting cooks wash or explicitly force dirty cookware, rendering active cookware, and adding game/Workshop preview art.
- Record food preservation/canning and food waste as future extension points; neither is in this change's implementation scope.
- Integrate the installed RimWorld 1.6 `Ceramics (Continued)` provider so its porcelain Stuff can be shaped into Stuff-retaining plates without inventing ceramic content when that optional package is absent.
- This change is the gameplay contract governing the implementation and its remaining in-game acceptance work.

## Capabilities

### New Capabilities

- `material-kitchenware`: Stuff-aware cookware, plates, cutlery, chef's knives, recipes, stats, and material classification.
- `meal-production`: Ware requirements, recipe timing, exclusions, reservations, and nutrient-paste dispensing behavior.
- `dish-lifecycle`: Persistent clean/dirty state, washing jobs and sources, dishwashers, recovery, and service clearing.
- `prepared-food`: Ingredient preparation, provenance, quality, rot, paste-derived preparation, and cooking acceleration.
- `cooperative-cooking`: Linked manned stations, early assistant requests, contribution accounting, and interruption behavior.
- `meal-state`: Meal culinary quality, temperature, poisoning modifiers, thoughts, refrigeration, and microwave reheating.
- `dining-standards`: Cutlery acquisition, expectations, Royalty requirements, comfort, and unmet-standard thoughts.
- `optional-gameplay-integrations`: Package-ID-gated compatibility behavior for the supported local mod ecosystem.
- `visual-assets`: Candidate generation, style/readability selection, Stuff-compatible masks, package wiring, optional Vanilla Textures Expanded - Variations families, and in-game visual acceptance for custom items and buildings.
- `localization`: Complete keyed and Def-injected product localization plus distributable-mod release coverage.

### Modified Capabilities

None.

## Impact

The implementation primarily affects `mods/ImmersiveChefs`, including Defs, XML patches, Harmony patches, serialized components, jobs/work givers, buildings, settings, language catalogs, compatibility adapters, and tests. Harmony and XML Extensions are required third-party foundations. Every inspected content, material, hygiene, hauling, restaurant, dispenser/printer, selection, storage, variety, replacement-meal, and framework package named by the optional-integration contract remains optional.

## Affected Mods

- **Immersive Chefs** — package ID `fumblesneeze.immersivechefs`; repository path `mods/ImmersiveChefs`.
