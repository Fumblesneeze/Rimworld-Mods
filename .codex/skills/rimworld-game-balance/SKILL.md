---
name: rimworld-game-balance
description: Balance RimWorld recipes, resource costs, work amounts, stats, stack limits, availability, and trader stock against measured Core and installed-mod comparators. Use when adding or rebalancing Things, recipes, research, market values, trade tags, stock generators, or optional material support, especially after playtest feedback that a value feels too cheap, costly, common, rare, fast, or slow.
---

# RimWorld Game Balance

Use measured finalized Defs and native player workflows. Never invent a value merely because it sounds plausible.

## Establish the contract

1. Read the owning OpenSpec requirement and `.codex/skills/rimworld-mod-development/SKILL.md`.
2. Identify the gameplay abstraction: one physical item, one place setting, one cookware set, one batch, or one building. Do not compare quantities before normalizing the output unit.
3. Record the intended tech level, availability, durability, labor, quality range, and niche. Treat balance constants as provisional unless the contract fixes them.
4. Preserve Stuff categories where the design intends modded materials. Prefer finalized category membership over enumerating known material Def names.

## Measure comparators

Choose at least three nearby comparators, including the closest Core item and any overlapping active mod item. Inspect source XML for provenance, then inspect finalized Defs in an exact active-mod process when inheritance, PatchOperations, Stuff, or optional mods can change the result.

Useful sources:

```powershell
rg -n "<defName>|<costList>|<costStuffCount>|<workToMake>|<BaseMarketValue>" `
  "F:\Steam\steamapps\common\RimWorld\Data" `
  "F:\Steam\steamapps\workshop\content\294100"

.\scripts\Invoke-GatewaySmoke.ps1 -Quicktest `
  -AdditionalModIds @('brrainz.harmony','fumblesneeze.immersivechefs') `
  -AdditionalModProjectPaths @('.\mods\ImmersiveChefs\ImmersiveChefs.csproj')
```

Use `POST /api/v1/defs/export` with exact names and fields for finalized values. Follow pagination and retain the ordered mod list. Do not treat exported JSON as canonical XML.

Build a small comparison table containing:

- output units and stack limit;
- ingredient units, ingredient market value, and total mass;
- work amount and required skill/station/research;
- hit points, beauty, cleanliness, comfort, speed, quality effects, and market value;
- acquisition paths and expected stock quantity.

Normalize costs into ratios that players already understand. For example, compare stone-block consumption with walls and passive furniture, metal consumption with weapons and workbenches, and labor with similarly skilled recipes. A cookware *set* can cost more than one loose utensil, but its magnitude must still make sense beside those anchors. If a wall consumes five stone blocks, a primitive set consuming many wall-equivalents needs an explicit gameplay justification.

## Set values

Use this priority order:

1. Player-recognizable Core economy and construction anchors.
2. The closest item with the same function and tech level.
3. Installed compatibility-mod conventions.
4. The intended gameplay niche and progression.
5. A provisional interpolation, documented for playtesting.

Avoid compensating one extreme with another—for example, an excessive material cost does not become sound merely because work is low. Check the combined material, labor, research, durability, utility, and resale result. Prevent profitable craft loops by comparing input market value, quality multipliers, and output value.

For Stuff recipes, test at least the cheapest eligible, a common baseline, and an expensive eligible material. Confirm modded Stuff enters through the intended category and that recipe text names the material category intelligibly rather than exposing internal roots or placeholder labels.

## Choose trader stock

Apply this decision tree separately to each product tier:

1. **Would settlements plausibly manufacture and move it in quantity?**
   - Yes: consider bulk-goods and suitable faction-base traders.
   - No: continue.
2. **Is it specialized, high-tech, unusually high-quality, or imported?**
   - Yes: consider exotic-goods, orbital, quest, or reward acquisition with low commonality.
   - No: continue.
3. **Is it primitive and locally produced?**
   - Yes: consider tribal/primitive traders and settlements; exclude industrial-only variants.
4. **Does it require refrigeration, installation, licensing, or another contextual constraint?**
   - Yes: sell it only where that context makes sense, often minified for buildings.
5. **Would stocking every Stuff variant flood the catalog?**
   - Yes: use bounded commonality/count and appropriate Stuff selection; do not enumerate every installed material.

Typical kitchenware mapping:

- primitive or ordinary sets: low quantities at matching settlement/bulk traders;
- modern manufactured sets: low quantities at industrial/bulk traders;
- luxury plate/cutlery variants: sparse exotic stock;
- glitterworld, self-cleaning, or otherwise uncraftable sets: very rare exotic/orbital, quest, or reward acquisition—not ordinary bulk stock.

Inspect the real `TraderKindDef` and `StockGenerator` chain before patching. Prefer conditional XML for stable Def targets and a narrow absent-safe adapter only when dynamic logic is genuinely required. Avoid load coupling to an optional trader mod.

## Verify with TDD and the game

1. RED/GREEN deterministic cost, category, and stock-selection rules in the honest host test environment.
2. Verify finalized XML/Defs in an exact mod-list integration process when patching or optional Stuff is involved.
3. Build the package with zero warnings and inspect the exact shipped XML/assets.
4. Independently review the scoped diff.
5. On the reviewed build, perform the native bill or construction order and observe exact resources consumed, work performed, and product produced.
6. For trade claims, open the native trade dialog, observe the item and quantity, complete a real purchase, and observe delivery. Directly spawning the product does not prove trader availability.
7. Inspect developer-mode console and the complete Player log for warnings/errors, then retain the exact build, mod list, screenshots, config hashes, and cleanup evidence.

Do not run every trader or compatibility matrix during each adjustment. Run focused affected groups while iterating; reserve the consolidated matrix for release or an explicit regression checkpoint.
