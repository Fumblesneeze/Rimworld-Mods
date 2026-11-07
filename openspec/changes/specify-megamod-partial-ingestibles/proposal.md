## Why

Interrupting a meal should preserve both the food already eaten and the usable remainder. FoodTracker
also addresses drinks, drugs, stacking and travel; specifying those together prevents a leftover from
becoming a free full meal or dose elsewhere in the game.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**; replacement **S085/P2-14**, FoodTracker. Specification only.

## What Changes

- Preserve consumed and remaining portions through interruption, resumption, stacking, transfers,
  save/load and ordinary map or caravan consumption. Preserve item identity, restrictions and condition.
- Make remaining nutrition, supported dose effects, mass, value and selected travel supplies agree.
  Show the quantities in existing inspection, inventory, trade and travel surfaces.
- Correct filter broadening, lost item condition, repeated-dose amplification and display-only
  accounting. Keep ordinary whole-item timing and balance; do not copy source drug-duration XML.
- Keep unsupported ingestion effects on their ordinary path until their complete behavior is
  specified and admitted. Record stochastic effects and categorical-event decisions explicitly as
  unresolved work; this draft does not authorize a partial implementation of those paths.
- Compose directly with owned hauling, medical supply, drug policy/response, poisoning and caravan
  features. No separate food dashboard, bottom tab or duplicated map index.

## Capabilities

### New Capabilities

- `megamod-partial-ingestion`: supported partial consumption, remaining item conservation, timing,
  effects, condition, save lifecycle, permission and provider boundaries.
- `megamod-remaining-supply-accounting`: remaining quantities in stacks, capacity, trade, recipes,
  travel estimates and contextual UI, with consistent physical transfers.

### Modified Capabilities

None. This change supplies remaining-quantity behavior to existing owned policies without changing
their eligibility, priority, carrying limits, storage limits or medical authorization.

## Impact

Future owned item/ingestion state, guarded native integration, common quantity facts and translated
contextual UI. Core and external Harmony (`brrainz.harmony`) will be required for implementation;
do not bundle the source's internalized Harmony. XML Extensions (`imranfish.xmlextensions`,
Workshop2574315206) becomes required with the first declarative guarded component/Def integration
that uses it. No dependency metadata or product behavior is changed by this documentation slice.

FoodTracker and other content providers remain optional; the original owns overlap when active.
Biotech/Royalty/Ideology effects are optional and only available with their content. No package spoof,
foreign save import, source code/assets/assemblies, repository-mod dependency or Gateway content.
[Research](../../../mods/megamod/docs/refinement/2026-11-05-food-tracker-research.md) records provenance,
source defects, deliberate deviations and implementation-blocking questions.
