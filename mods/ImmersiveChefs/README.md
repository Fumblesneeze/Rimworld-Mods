# Immersive Chefs

Package ID: `fumblesneeze.immersivechefs`

Immersive Chefs is a RimWorld 1.6 gameplay mod that makes cooking tools, reusable service ware, ingredient preparation, kitchen teamwork, culinary quality and temperature, dining standards, and dishwashing part of colony simulation.

## Requirements and loading

- RimWorld 1.6
- Harmony (`brrainz.harmony`)
- XML Extensions (`imranfish.xmlextensions`, Workshop `2574315206`)

Harmony provides runtime code patching, while XML Extensions provides package-ID-aware declarative compatibility patches. All material, hygiene, restaurant, storage, food, texture, hauling, and dispenser integration targets remain optional, package-ID gated, shape guarded, and isolated behind narrow adapters. Load Harmony before Core and XML Extensions after Core but before Immersive Chefs; RimWorld can place Immersive Chefs after any active optional packages named in its metadata.

Never add the developer-only RimWorld Dev Gateway to an ordinary playthrough. Immersive Chefs does not reference or depend on it.

## Getting started

1. Craft primitive cookware, plates, and cutlery at a crafting spot, then move to smithies and machining tables as better Stuff becomes available.
2. Give Cooking-capable pawns access to clean cookware and eligible plates. Chef's knives are optional belt-slot bonuses.
3. Assign Cleaning work so returned ware can be washed. Research unlocks powered dishwashers and professional kitchen stations.
4. Use the separate clean/dirty stockpile filters when you want service storage and wash input in different places.

Strict ware mode blocks covered cooking until suitable clean equipment exists, subject to the configured emergency fallback. Fine and Lavish meals require increasingly suitable plate materials. Hand foods such as pemmican, packaged survival meals, snacks, raw food, drinks, drugs, and baby food stay outside the tableware system.

## Major systems

- Stuff-aware cookware, plates, cutlery, primitive cookware, and wearable chef's knives.
- Exact reusable-ware identity through cooking, stacking, hauling, eating, washing, spoilage, fire, trade, and save/load.
- Prepared ingredients and paste-derived preparation with provenance and quality effects.
- Linked prep, meat, vegetable, sauce, and pastry assistants that contribute only while a lead cook is working.
- Culinary quality, food-poisoning attribution, temperature, reheating, dining expectations, and Royalty standards.
- Handwashing and powered dishwashers with interruption/recovery behavior and guarded Dubs Bad Hygiene water use.
- Optional compatibility for the maintained package chains without turning them into hard dependencies.

The full settings table, compatibility matrix, save behavior, and troubleshooting guide are in [`docs/ImmersiveChefs.md`](../../docs/ImmersiveChefs.md).

## Saves and limitations

Back up valued colonies and keep their mod list and restart-required settings stable. Current saves retain per-serving culinary state and exact embedded ware, but migration between older unreleased development schemas and uninstall cleanup are not implemented.

Food preservation/canning and food waste are separate future systems. Optional compatibility is guaranteed only for the exact guarded package chains in the player guide; an upstream shape change disables the affected adapter rather than guessing.

## Development

Use the repository MCP/CLI from the repository root:

```powershell
$mcp = '.\tools\RimWorldModding.Mcp\RimWorldModding.Mcp.csproj'

dotnet run --project $mcp -- tool call mod_build `
  --arguments '{"packageId":"fumblesneeze.immersivechefs","configuration":"Release"}' -o json

dotnet run --project $mcp -- tool call test_run `
  --arguments '{"suite":"ImmersiveChefs.Unit","filter":null,"configuration":"Release"}' -o json

dotnet run --project $mcp -- tool call e2e_run_start `
  --arguments '{"testId":"immersive-chefs.countertop-microwave-native-reheat","language":"English","timeoutSeconds":300,"dryRun":true}' -o json
```

Host environments are intentionally separated into `ImmersiveChefs.Unit`, `ImmersiveChefs.Harmony`, and `ImmersiveChefs.Defs`; use only the environment that honestly owns the behavior under test. Real loaded mods, finalized Defs, PatchOperations, and player workflows belong in isolated RimWorld processes.

The accepted gameplay contract is [`openspec/changes/specify-immersive-chefs-gameplay`](../../openspec/changes/specify-immersive-chefs-gameplay/). The status and replacements for legacy scenario descriptors are in [`docs/ScenarioMigrationInventory.md`](../../docs/ScenarioMigrationInventory.md).
