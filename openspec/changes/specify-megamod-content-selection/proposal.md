## Why

Players should be able to exclude unwanted parts of Core, DLC and mods without removing their
whole providers. Cherry Picker offers that breadth and useful grouped removal presets, but
its destructive changes, uneven restoration and global-only state need a deliberate replacement.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. S038 **Cherry Picker**, `Owlchemist.CherryPicker`, Workshop
`3521312241`, is a read-only source. This change delivers **specification only**.

## What Changes

- Specify the full source catalogue, its protected entries and the concrete effect of excluding
  each kind of content, including optional Ancients and Psycasts content.
- Compose the S020 selectors directly into one content-settings page, with identifiable rows,
  common global defaults/save overrides, visible exclusion reasons and pending application state.
- Specify independent individual and preset choices, overlapping/nested presets, missing content,
  restored eligibility, existing-object preservation and bounded failure behavior.
- **BREAKING relative to the source:** apply configuration at new-game/load boundaries; preserve
  prices, nutrition, components, construction costs and existing objects instead of destroying
  their shared definition data. Block unavailable production rather than making it free.
- **BREAKING relative to the source:** preset re-enabling removes only that preset's exclusions;
  it cannot erase independent choices. No silent import of the source's global settings.
- Keep owned grouped presets and their observable exclusion behavior. Do not implement or test
  Cherry Picker's API, XML format, package checks or binary/save contracts for replacement parity.

The source's 1.6 binary is research evidence, not a verified 1.7 implementation. Remaining technical
questions are tracked without interrupting the sequential specification work.

## Capabilities

### New Capabilities

- `megamod-content-selection`: content catalogue, configuration scope/application, preset reasons,
  restoration, existing-world preservation and ordinary feature-disable behavior.
- `megamod-content-availability`: observable exclusion effects across production, research,
  population, world generation, events, pawn features and interface choices.

### Modified Capabilities

None. S020's searchable selectors and S029's drug-policy reconciliation keep their existing
contracts and are composed directly. Their source-specific interactions are made explicit here.

## Impact

- Future owned content policy, settings UI and narrow guarded runtime integrations. Harmony is
  required when its types are used. XML Extensions (`imranfish.xmlextensions`, Workshop `2574315206`)
  is required only when an actual owned declarative patch uses it; the legacy preset adapter is removed.
- Core-only use remains supported. DLC, VFE Ancients, VEF and Vanilla Psycasts Expanded remain
  optional content integrations with their own admission and absent/present/changed-provider checks.
- No new bottom tab, entity editor, runtime library mod, duplicate Thing cache or foreign DLL.
  An active original produces a bounded startup warning and uses the ordinary per-feature settings
  disable path, best effort. This neither promises cooperative compatibility nor requires a
  source-provider-present compatibility run; it does not unpatch or uninstall the original.
- Settings use owned storage. Automatic migration and arbitrary definition editors are outside
  this change. Existing source config files and installed providers remain untouched.
- [Research](../../../mods/megamod/docs/refinement/2026-11-03-cherry-picker-research.md) and
  [replacement coverage](../../../mods/megamod/docs/replacements/cherry-picker.md) retain provenance,
  deviations and unresolved implementation/acceptance work. Recorded consumer bindings are historical
  research, not a facade or compatibility-testing obligation.
