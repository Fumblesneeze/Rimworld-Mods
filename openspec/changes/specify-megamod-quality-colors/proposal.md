## Why

Quality should be recognizable across item labels without changing what an item does. S131 adopts
Quality Colors (Continued)'s palette and display coverage, while correcting stale colors, language
assumptions, inconsistent opacity and interference with unrelated text or status indicators.

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. **Specification for later implementation only**.

## What Changes

- Color the seven quality labels, item quality/condition annotations, neutral transferable item
  names, inspection quality text and optionally the complete single-item inspect title.
- Preserve six named palettes and per-quality color editing, including opacity. Keep quality
  readable through neutral text/tooltip alternatives, and preserve trade restrictions and statuses.
- Put palette and title preferences in the common global/save settings UI, with explicit scope,
  Use global, transactional previews and immediate application without requiring simulation ticks.
- Keep formatting correct after truncation, localization, palette/save changes and actual item
  quality changes. Preserve names, quantities, durability, taint, forced/locked indications and actions.
- Share the owned palette directly with S132's future inventory presentation; no internal patch stack
  or separate item-quality cache. S132 decides its layout and non-quality colors independently.
- Record the actual Dynamic Trade Interface and Nice Inventory Tab consumer contracts and their
  package-bound discovery limits. Do not promise binary drop-in compatibility or spoof source IDs.
  Their additional bridges remain explicit research/future scope until separately specified.
- Withhold owned overlapping coloration when the original provider is active and explain the
  unavailable owned behavior with a bounded warning. Never rewrite foreign settings or unpatch it.

## Capabilities

### New Capabilities

- `megamod-quality-colors`: consistent quality presentation, six palettes, scoped customization,
  safe formatting, truthful compatibility fallback and unchanged item mechanics.

### Modified Capabilities

None. Existing settings and shared data contracts are consumed directly; S132 retains its own change.

## Impact

- Research, coverage, proposal, observable contract, design and unchecked future tasks only.
- Future native UI hooks require **Harmony**. Original Quality Colors, RPG Inventory, Dynamic Trade
  Interface and Nice Inventory Tab are **optional**, never megamod dependencies. No DLC requirement.
  No XML operation/Def mutation is selected, so this change introduces no XML Extensions dependency.
- Author documentation and complete selected 1.7 DLL inspection are available. The linked public
  repository could be viewed only at its root; source retrieval failed. No license was established.
  Independently implement behavior; do not redistribute upstream code, assemblies or assets.
- All seven languages, focused TDD, executable target checks, independent runtime review and native
  player-workflow acceptance remain future work. No mod installation or runtime change is delivered.
