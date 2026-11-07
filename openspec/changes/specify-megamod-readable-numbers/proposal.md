## Why

Large quantities should be easy to read without changing their meaning, input behavior or precision.
The selected Readable Numbers feature should apply consistently across game and owned UI, while
avoiding number-like names, dates and technical text and respecting the user's common settings model.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S034; origin: replacement. **Specification only**, no implementation.

## What Changes

- Add configurable digit grouping and decimal punctuation to ordinary displayed quantities,
  preserving signs, units, fractional digits, original precision and actual numerical values.
- Retain three-digit, three-then-two, four-digit and no-grouping modes, six punctuation presets,
  a validated custom pair, minimum integer-digit threshold, small-decimal option and text exclusions.
- Apply thresholds per number, keep formatting stable across repeated display, and protect editable
  fields, names/identifiers, dates, versions, diagnostic output and rich-text attributes.
- Use consistent text measurement/drawing so quest rewards, map quantities, tooltips and ordinary
  native/modded UI stay usable. Unsupported representations retain their original text.
- Move configuration to the common global-default/save-override settings UI with live examples
  and immediate invalidation. Keep diagnostic actions in the top debug toolbar. Internal cache
  tuning is not a player-facing feature; the source's cache-size/debug settings are not copied as-is.
- Compose the selected RimHUD behavior directly with this formatter and preserve optional external
  HUD/readout/date/profiler contracts only where their actual shapes are admitted.

## Capabilities

### New Capabilities

- `megamod-readable-numbers`: observable quantity formatting, protected text, settings, display
  consistency and safe native/optional-provider failure behavior.

### Modified Capabilities

None. Foundation settings, UI placement, semantic admission and package rules apply unchanged.

## Impact

- Reference **Readable Numbers**, `7h0m4s.RimworldReadableNumbers`, Workshop **3762441264**.
  [Research](../../../mods/megamod/docs/refinement/2026-11-03-readable-numbers-research.md) records
  the pinned source, selected compiled 1.7 paths, native UI and consumed RimHUD method.
- **Harmony** (`brrainz.harmony`, Workshop2009463077) will be required for the planned guarded native
  integration; no source assembly or DLC is required. No XML changes are planned; any future XML
  Extensions operation must add its required dependency and guarded, merge-preserving contract.
- External RimHUD, Toggleable Readouts, Dawn of a New Day and Dubs Performance Analyzer are
  **optional**, independently admitted integrations; source-wide compatibility remains unproven.
  Selected owned RimHUD composes directly, without patches against our own feature.
- No copying source GPL-3.0 code/assemblies/assets, package identity emulation, automatic settings
  migration, game-state mutation or gameplay acceptance. Source co-installation suppresses our
  overlapping formatter with a startup warning and preserves the remaining owned UI features.
