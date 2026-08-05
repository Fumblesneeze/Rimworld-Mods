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
- Specify optional integrations for locally installed material, hygiene, restaurant, Common Sense, variety, and nutrient-paste mods without making them hard dependencies.
- Specify player-facing settings and safe emergency fallbacks so the simulation cannot deadlock or starve pawns.
- Specify `Dishwashing` and `Professional Kitchens` research while leaving ordinary tableware progression to the crafting spot, smithies, and machining table.
- Replace every player-visible placeholder texture with selected custom art, require multiple candidates per asset, compare candidates at game scale against RimWorld's live visual context, and ship only the selected alpha-clean result.
- Record food preservation/canning and food waste as future extension points; neither is in this change's implementation scope.
- Defer ceramic/porcelain content and recipe classification for Vanilla Cooking Expanded to later compatibility changes.
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
- `visual-assets`: Candidate generation, style/readability selection, Stuff-compatible color treatment, package wiring, and in-game visual acceptance for custom items and buildings.

### Modified Capabilities

None.

## Impact

The implementation primarily affects `mods/ImmersiveChefs`, including Defs, XML patches, Harmony patches, serialized components, jobs/work givers, buildings, settings, compatibility adapters, and tests. Harmony remains the sole required third-party mod; Processor Framework, Expanded Materials, Dubs Bad Hygiene, Hospitality, Gastronomy, Common Sense, Variety Matters, Vanilla Food Variety Expanded, Vanilla Expanded Framework, Vanilla Nutrient Paste Expanded, and compatible material mods remain optional.

## Affected Mods

- **Immersive Chefs** — package ID `fumblesneeze.immersivechefs`; repository path `mods/ImmersiveChefs`.
