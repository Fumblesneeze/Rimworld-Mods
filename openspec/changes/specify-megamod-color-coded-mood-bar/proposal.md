# Colonist-bar mood and condition information

## Why

The colonist bar should make mood risks and important conditions readable without repeatedly opening
individual pawns. P1-01 requests the functionality of CM Color Coded Mood Bar. Its title understates
its scope: the installed 1.7 edition also supplies health, relationship, schedule, portrait-filtering,
age and weapon displays. These need explicit contracts rather than a blanket promise of parity.

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. This change specifies future behavior; it delivers no gameplay yet.

## What Changes

- Specify mood fill and break-risk outlines, schedule outlines, illness, pregnancy, bleeding,
  temperature, suspended-pawn and relationship indicators in the existing colonist bar.
- Specify optional gender, biological age, equipped-weapon and filtered portrait presentations.
- Preserve native selection, drafting feedback and activity information.
- Move configuration into megamod's common settings system, with global defaults and save overrides.
  Add no bottom tab. A quick presentation toggle, if introduced later, must follow the bottom-right
  control rule and receive its own observable contract.
- Correct source inconsistencies such as reset changing the initial defaults, fractional weapon
  sizes being truncated, and suppressed portraits leaking extra overlays. Do not preserve stale
  indicators as compatibility behavior.
- Record optional integration and external-contract gaps without claiming the original mod replaced.

## Capabilities

### New Capabilities

- `megamod-color-coded-mood-bar`: configurable colonist-bar information, filtering and safe fallback.

### Modified Capabilities

None. Common settings and patch admission remain owned by the megamod foundation capability.

## Impact

Source: `CrashM.ColorCodedMoodBar.11`, Workshop `2006605356`; see the
[research and coverage report](../../../mods/megamod/docs/replacements/color-coded-mood-bar.md).
No source assembly, SettingHelper assembly or source texture is added to the product.

Future runtime dependency: Harmony (`brrainz.harmony`) is **required if the chosen implementation
uses Harmony**; the current inert package metadata stays unchanged. XML Extensions is not needed
by this proposed display-only behavior. Colony Groups, Pawn Badge, Job In Bar and Disabled Pawn
Markers are **optional** integration candidates, not prerequisites or verified compatibility claims.
The original CM mod is not a dependency. Framework/library integration details and source quirks
belong in design, not in the observable specification.
