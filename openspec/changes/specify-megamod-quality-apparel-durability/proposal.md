## Why

Better-made clothing and armor should lose less condition when damaged. S106 replaces Quality
Based Durability with explicit quality-dependent damage loss, including the ordinary wear and
deterioration paths its source also affects, and makes that scope understandable in item information.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S106/P2-36**, Quality Based Durability (`gabrieel1482.QualityBasedDurability`,
Workshop3798015624). This change specifies later implementation; no gameplay is delivered yet.

## What Changes

- Scale damage received by quality-bearing apparel using the source's seven quality defaults.
  Preserve ordinary damage handling, fractional rounding, armor protection and maximum hit points.
- Explicitly include worn/loose apparel, daily wear, environmental deterioration and clothing
  damage on the wearer's violent death. Do not describe the source's broad effect as combat-only.
- Show the current quality's loss multiplier in the item's native information, with clear scope
  and lower-is-better wording rather than a promise of an exact lifetime or greater protection.
- Move the seven global source options into common global/save/Use global settings. Preserve their
  configurable range, validate bad values and apply edits to subsequent damage without healing items.
- Compose directly with quality reworking, item destruction, armor and repair features. Suppress
  the owned overlap when the original provider is active, and fail safely on incompatible targets.

## Capabilities

### New Capabilities

- `megamod-quality-apparel-durability`: Quality-dependent apparel damage loss, current-item
  information, inherited settings, lifecycle and safe compatibility behavior.

### Modified Capabilities

None. Existing quality reworking, armor, fire and item-processing capabilities retain their contracts.

## Impact

Core and Harmony are required when the native integrations ship. No XML patch is planned for this
feature, so it introduces no XML Extensions dependency. Optional apparel and combat providers remain
optional and need their own supported-path evidence. No current package or dependency is changed.

Installed source and the selected1.6 DLL were inspected separately against native1.7 behavior; this
does not claim that the original supports1.7. No public repository or license was found in the
inspected package/page. Implement the behavior independently, retain author/source attribution, and
do not copy upstream code, binaries, text or artwork. No facade, package spoof or silent settings
import is justified by the bounded consumer search.
