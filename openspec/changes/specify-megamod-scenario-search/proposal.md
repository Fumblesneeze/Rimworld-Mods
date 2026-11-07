## Why

Large mod lists make scenario choices difficult to find. Scenario Searchbars supplies useful
search fields, but the replacement must keep search local to editing and preserve the scenario's
actual choices, randomization and starting outcomes.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. Queue **S113**, origin **replacement**. This change specifies future
behavior only; no runtime implementation or native acceptance is delivered.

## What Changes

- Add independent search fields to supported scenario item, starting-animal, starting-research,
  stat-multiplier and incident selectors, including scheduled incidents.
- Search the visible localized choice names, retaining native eligibility, order, selection actions,
  quantities, materials, quality, multiplier and incident timing controls.
- Use literal case-insensitive search by default, with an explicit regular-expression mode.
  Invalid or excessively expensive expressions receive recoverable inline feedback.
- Keep each part's query stable while editing/reordering it and discard queries with their editor
  session. Queries do not change scenario data, dirty state, random generation or random pets.
- Preserve contextual navigation and compatible foreign additions. Skip unsafe selector integration
  with a bounded warning; do not claim arbitrary custom scenario UIs are automatically supported.

## Capabilities

### New Capabilities

- `megamod-scenario-search`: contextual scenario choice search with safe expression handling,
  transient per-part state, native action preservation and guarded provider composition.

### Modified Capabilities

None. Shared text-search components may be reused directly, without changing other owned search,
content-selection or settings contracts.

## Impact

- This change, [research](../../../mods/megamod/docs/refinement/2026-11-05-scenario-searchbars-research.md),
  [coverage](../../../mods/megamod/docs/replacements/scenario-searchbars.md) and refinement progress.
- Reference: `nimrag.scenariosearchbar`, Workshop `3579415129`; installed payload advertises through
  1.6, not 1.7. Earlier Workshop `3052467818` directs users to this release. Neither is a dependency.
- Harmony becomes required with actual Harmony use and is never bundled. No XML or XML Extensions
  operation is selected; any later use requires its dependency and guarded target in that slice.
  No hard DLC, source-provider, other product-mod or Dev Gateway dependency.
- An active source provider owns the overlap; the owned feature is disabled with a startup warning.
  No automatic mod removal, package spoof, external API facade, save migration or source asset copy.
- No new bottom tab, debug button, mod settings, map index or persistent search configuration.
  All seven baseline languages remain usable in the native scenario editor.
