# Localization

Immersive Chefs is a distributable product mod and ships complete English, German, Spanish, French, Simplified Chinese, and Russian catalogs. RimWorld Dev Gateway is explicitly development-only and is not localized for distribution.

## Catalog layout

- Canonical runtime text: `mods/ImmersiveChefs/Languages/English/Keyed/ImmersiveChefs.xml`
- Translated runtime text: `mods/ImmersiveChefs/Languages/<Language>/Keyed/ImmersiveChefs.xml`
- Translated Def fields: `mods/ImmersiveChefs/Languages/<Language>/DefInjected/<DefType>/*.xml`
- Canonical Def wording: the owned XML under `mods/ImmersiveChefs/Defs` and `mods/ImmersiveChefs/Patches`

The required RimWorld folder names are `English`, `German`, `Spanish`, `French`, `ChineseSimplified`, and `Russian`. Source and packaged catalogs must remain byte-identical.

## Terminology

The catalogs were authored from the in-game contexts by the implementing agent without a machine-translation service. Keep these concepts distinct when extending them:

| Concept | Meaning |
| --- | --- |
| cookware set | The reusable pot, pan, lids, handles, and utensils used while cooking; not a plate or cutlery. |
| kitchenware | The broad inventory category containing cookware, plates, and cutlery. |
| tableware | Plates and cutlery used by the diner. |
| cutlery | The reusable fork/spoon/knife eating set. Do not reintroduce “silverware” as the generic term. |
| sanitation | Clean/dirty state and how an item was washed. Wild-water provenance remains hidden from ordinary inspection. |
| culinary quality | The mod's accumulated meal-quality result, distinct from a Thing's crafting quality. |
| food poisoning | The vanilla illness risk and consequence. Do not label it poison or toxicity. |
| toxic buildup | The hidden, slow exposure caused only by supported lead or uranium food-contact ware and revealed through the pawn's vanilla hediff thresholds. |

Proper names such as mod names may remain invariant. Technical values such as `Auto` may remain invariant where that is the normal UI term, but any such equality should be deliberate and reviewable.

## Adding or changing player text

1. Put C# UI text behind a stable keyed translation entry. Put Def-owned fields in the appropriate `DefInjected` catalog; include Defs created by compatibility patches.
2. Update all six catalogs together and preserve placeholder indices and rich-text tags exactly.
3. Do not call `Translate()` from host-safe unit tests. Test the pure key-selection rule and let the localization release gate parse the catalogs.
4. Run the focused localization gate:

   ```powershell
   .\scripts\Invoke-Tests.ps1 -Suite ImmersiveChefs.Unit -Configuration Release -TestFilter 'FullyQualifiedName~LocalizationReleaseGateTests'
   ```

5. Build the product and verify that packaged language files are byte-identical to source.
6. Before release acceptance, inspect representative settings, Def/info-card, live work report, alert, and runtime-action text in a fresh isolated RimWorld process for each required language. Enable developer mode and inspect both the native console and complete log.

   ```powershell
   .\scripts\Invoke-RimWorldEndToEndTests.ps1 `
     -TestId immersive-chefs.localization-rendering `
     -Language German `
     -TimeoutSeconds 300 `
     -Output json
   ```

   Repeat with `English`, `Spanish`, `French`, `ChineseSimplified`, and `Russian`. The option changes only the run's disposable `Prefs.xml`; it does not touch the normal game preference.

The release gate derives exact keyed and Def-field inventories. It must reject missing, stale, duplicate, malformed, placeholder/tag-mismatched, or guarded raw player-facing strings; folder presence alone is never sufficient.
