## Why

Children's native growth-point rate does not incorporate Global Learning Factor, even when their
skill learning differs. S088 makes that same factor affect future growth-point gain while retaining
the existing need, age, growth-vat and reward rules.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**, S088/P2-17, Growth Speed Makes Sense Now. Specification only.

## What Changes

- Multiply the supported current growth-points-per-day rate by the pawn's effective Global Learning
  Factor once, including ordinary growth-vat rates; preserve native eligibility and accumulation.
- Let native growth-tier progress and its existing rate display reflect the adjusted result.
- Preserve age speed, learning-need satisfaction, skill XP rules, reward thresholds/choices and
  generated or already-earned growth points. Do not grant retrospective compensation.
- Add semantic admission, finite-rate safeguards and original-provider suppression instead of the
  source's unconditional postfix; retain compatible rate/stat providers and existing native callers.
- Correct earlier plant-draft forward references: this source changes child growth points, not crops.

## Capabilities

### New Capabilities

- `megamod-child-growth-learning`: factor-scaled future growth points, native eligibility/vat and
  display behavior, unchanged surrounding development, persistence and supported-provider fallback.

### Modified Capabilities

None. S031 toddler walking/manipulation progress is a separate biological-development system.
S103 Need Bar Overflow and other actual learning providers require direct composition only where
they change the consumed learning/stat rate; no assumed conflict or internal self-patch stack.

## Impact

Future narrow Harmony result adapter on the native growth-rate path. Core and external Harmony are
required when implemented. Biotech is required **for this feature**, optional for the overall megamod;
without it this feature is inactive while unrelated features remain available. Original GSMSN and
other providers are optional. No XML operation, new settings, tab, entity editor, asset, private cache
or source save schema is needed. A later XML operation must declare guarded XML Extensions at first use.

No foreign code/assembly reupload, package spoof, save migration, product Gateway dependency or
runtime acceptance. [Research](../../../mods/megamod/docs/refinement/2026-11-05-child-growth-learning-research.md)
records complete source/binary analysis, native consumers, public restrictions and CLG-Q1–4.
