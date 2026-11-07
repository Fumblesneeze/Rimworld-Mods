## Why

Meals, drugs and unusually comfortable surroundings can supply more satisfaction than a pawn's
ordinary full need bar holds. S103 preserves a configurable reserve and shows it honestly, while
keeping ordinary decisions about eating, sleeping and recreation recognizable.

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin replacement **S103/P2-33, Need Bar Overflow** (`AmCh.NeedBarOverflow`). This is a specification
for later implementation; it delivers no gameplay or accepted compatibility.

## What Changes

- Allow admitted needs to retain gains above ordinary full, with per-need limits, food's minimum
  additional nutrition reserve, and configurable pawn-group/content exclusions.
- Preserve ordinary need-seeking thresholds and meaningful native losses, resets and side effects.
  Optional food consequences and overflow-dependent rest/recreation/kill-thirst rates remain off
  by default, as in the inspected binary. Document every proposed numeric default and correction.
- Show current reserves and ordinary full on existing need surfaces. Compose forecasts, mood bars,
  child growth and future RimHUD directly through owned need policy, without another pawn index.
- Put configuration in common global/save settings and unrestricted need adjustments in the top
  developer toolbar. Add no bottom tab or stand-alone settings window.
- Correct negative-gain clipping, recreation's wrong rate input, stale eligibility, cross-save
  health effects and unrelated UI/AI changes found in the reference implementation.

## Capabilities

### New Capabilities

- `megamod-need-overflow`: Need reserves, eligibility, configurable consequences, bounded contextual
  presentation, inherited configuration, lifecycle and compatibility fallbacks.

### Modified Capabilities

None. S003 forecasts already require actual overflow-aware amounts and rates. S088 growth continues
to scale the ordinary current growth rate once; S103 explicitly documents that composition.

## Impact

Core is required; relevant DLC/content is optional and needed only for its own needs. Harmony is
required at implementation of native hooks. XML Extensions becomes required at the first guarded
provider exclusion XML operation; it is not an optional dependency when those operations ship.
Original Need Bar Overflow owns the overlapping behavior whenever that original provider is loaded. No foreign owner is unpatched and no package identity is impersonated.

The source is GPL-3.0; independently implement observable behavior. No source code, assemblies or
illustrations are bundled. Source settings and the source's FoodOverflow health condition are not
silently imported. Research and replacement coverage retain the exact inspected identities and gaps.
