## Why

Players should be able to understand a pawn's equipment, condition and carried supplies at a glance,
while retaining the game's real item controls. S132 adopts RPG Style Inventory Revamped's useful
Gear layouts and composes them with the megamod's shared quality, cargo and personal-supply features.

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. **Specification for later implementation only**.

## What Changes

- Offer compact equipment slots, a simplified apparel grid and an ordinary list within existing
  pawn Gear inspection. Keep every actual item accessible, including unfamiliar or conflicting gear.
- Show an inspection portrait, current condition, quality, equipment statuses and authoritative
  weight, temperature and armor information without changing equipment capacity or combat rules.
- Preserve current native item actions, permissions, confirmations and provider restrictions;
  source-era copies of those rules are not authoritative for the beta.
- Share S131's quality palette and editor, S021/S126 cargo facts and S062/S123/S124 personal supplies
  directly. Keep distinct meanings visible rather than replacing one feature's indicator with another.
- Place actual configuration in inherited global/save settings, current view switches at the bottom
  right, entity supply orders in Gear, and developer equipment overrides in the top debug toolbar.
- Defer CE weight/bulk, loadout and item actions; preserve admitted RWoM enchantment information and
  Fortified's actual control authority only through admitted provider contracts. Otherwise retain
  the provider's native inspection/actions and explain unavailable owned presentation.
- Guard declarative tab integration and optional layout content, preserve unrelated inspector tabs,
  tolerate benign patching and withhold owned overlap when an original RPG inventory provider is active.
- Document legacy XML/type-binding and settings-import limits instead of claiming a binary drop-in.

## Capabilities

### New Capabilities

- `megamod-gear-presentation`: equipment layouts, truthful item/status/stat inspection, native and
  provider actions, scoped presentation preferences and safe contextual integration.

### Modified Capabilities

None. Existing owned settings, armor, palette, hauling and supply contracts are consumed directly.

## Impact

- Documentation only: proposal, observable contract, design, research, coverage and unchecked tasks.
- Core required; **Harmony** and **XML Extensions** (`imranfish.xmlextensions`, Workshop2574315206)
  become required with the selected guarded runtime/declarative implementation. RWoM, Fortified,
  original inventory providers and DLC-specific information remain optional. No whole-mod DLC gate.
- Installed source advertises1.2–1.6; its complete selected1.6 main/CE assemblies and layout XML were
  inspected against native1.7.5000 contracts. Author beta/support claims are not acceptance evidence.
- Public repository root/README were read, but no source revision or license was established.
  Independently engineer behavior; do not copy upstream assemblies, bundled Harmony or artwork.
- All seven languages, assets if needed, focused TDD, executable target admission, independent runtime
  review and causal native player-workflow acceptance remain future work. No installation performed.
