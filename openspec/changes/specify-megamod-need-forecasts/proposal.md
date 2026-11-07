## Why

Need bars show current levels but make it difficult to judge how much time remains before hunger,
exhaustion or other penalties. P1-03 requests Improved Need Indicator's forecasts so players can
make those decisions without reconstructing the game's need calculations themselves.

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. This is a specification change only, with no runtime behavior delivered.

## What Changes

- Append conditional time estimates to existing Food, Rest, Recreation, Indoors and Outdoors
  tooltips, retaining the original descriptions and other compatible tooltip additions.
- Offer a held-key detailed view with full-level comparisons and rest-recovery estimates.
- Make forecast assumptions visible and handle frozen, unknown or changing rates without false timers.
- Reimplement against verified 1.7 rules. The installed source declares only 1.2–1.6 support;
  old formulas and private-field assumptions are research inputs, not our compatibility contract.
- Record inactive source code for Mood, Comfort and Beauty as omitted from this source's delivered
  feature set; do not silently expand the request based on unused classes or translation strings.

## Capabilities

### New Capabilities

- `megamod-need-forecasts`: contextual need-transition estimates and detailed comparison tooltips.

### Modified Capabilities

None. Guarded patch admission and shared settings policies remain foundation capabilities.

## Impact

Source: `AmCh.ImprovedNeedIndicator`, Workshop `3211635205`; see the
[research report](../../../mods/megamod/docs/replacements/improved-need-indicator.md).
The original is not required. Harmony is a **required future dependency if used** for tooltip
augmentation. No XML Extensions operations are proposed. Need Bar Overflow is a separately selected
future feature and optional external integration candidate, not a prerequisite for forecasts.
Ordinary needs continue without DLC; DLC/mod-specific needs require actual supported contracts.
No bottom tab, autonomous pawn action or need/balance change is introduced.
