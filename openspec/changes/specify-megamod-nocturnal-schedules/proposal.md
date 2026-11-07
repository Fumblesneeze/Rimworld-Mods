## Why

Night-oriented pawns should receive a useful initial schedule without repeated manual painting.
Automatic Night Owl supplies this convenience; S070 specifies it as one-time native schedule setup
with explicit lifecycle, customization and optional-DLC boundaries.

## What Changes

- Assign qualifying player humanlikes Sleep from **11:00 to 19:00** and Anything otherwise once,
  covering starting pawns, arrivals, successful recruitment and enslavement, with existing-save handling.
- Retain Night Owl trait qualification and optional actual darkness-preference/sunlight-disliking
  behavior, including compatible content without hard-coded xenotype or meme names.
- Preserve customized schedules and later player edits, including travel/reload; do not continually
  resynchronize schedules when traits, genes, ideologies or settings change.
- Give the two optional criteria consistent **on** defaults, and make the initial sleep window
  configurable through common global/save settings. The source has inconsistent fresh/missing-key defaults.
- Keep pawn schedules on the native Schedule surface; explain this automation's initial-only scope
  in settings. No new tab, debug command or operational toggle.

Documentation only. No runtime implementation, dependency changes or gameplay acceptance delivered.

## Capabilities

### New Capabilities

- `megamod-nocturnal-schedules`: One-time nocturnal timetable setup, eligibility, preservation of
  player choices, common configuration, native composition and admission/provider fallback.

### Modified Capabilities

None. S001 schedule intent display, S002 temporary recreation and S031 child scheduling retain
their existing contracts; this feature supplies their shared native timetable state.

## Impact

Sole owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
Source **Automatic Night Owl**, `ben.automaticnightowl`, Workshop **2056012179**, selected assembly
**1.0.0.0**. [Research](../../../mods/megamod/docs/refinement/2026-11-04-automatic-night-owl-research.md)
records exact hashes, complete binary analysis, native evidence, intentional corrections and gaps.

Harmony becomes **required** for lifecycle admission hooks. Ideology and Biotech are **optional** for
their respective criteria; Core Night Owl behavior remains usable without either. Original source
and starting-pawn/schedule providers are optional; no source binary, other repository mod or Dev
Gateway dependency. No XML patch selected, so no new XML Extensions use. Source saved identity or
settings migration, arbitrary custom-provider support and safe removal remain unverified.
