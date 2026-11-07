## Why

An existing floor should be an easy way to find its ordinary construction tool. S136 replaces
Floor Pipette Tool without bypassing research, revealing fogged terrain or accidentally issuing work.

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
Specification for later implementation only.

## What Changes

- Add Pick floor to Architect / Floors; hover identifies a visible floor and click selects its
  currently permitted build tool. Placement remains a separate normal construction action.
- Preserve the exact terrain variant, ordinary build restrictions, native controls and current
  owned Architect/Cherry Picker availability, including paused changes.
- Correct source fog-hover disclosure, stale click admission and its conflicting Cancel shortcut.
- Share Architect resolution, preserve native colored-floor sampling and existing removal tools,
  guard the additive XML entry and report unsupported behavior without changing terrain.

## Capabilities

### New Capabilities

- `megamod-floor-sampling`: contextual floor identification, permitted tool selection and safe composition.

### Modified Capabilities

None. Consume S036 Architect, S038 content availability and S067 floor-command contracts directly.

## Impact

Documentation only. Future runtime requires Core and XML Extensions (`imranfish.xmlextensions`,
Workshop2574315206) for additive guarded Architect integration. Source requires Harmony but has no
actual Harmony patch class; do not copy its empty PatchAll or add that dependency solely for S136.
Any future necessary native patch must receive its own target spec and dependency declaration.
No original provider, other repository mod or Dev Gateway runtime dependency. Optional floor content
uses current native construction eligibility, not a hard dependency list.

Reference `ferny.FloorPipetteTool`, Workshop3706877822, assembly1.0.0.0. Complete installed source and
binary checked, public commit pinned. No reuse license found; independently implement and create
owned UI art through the repository asset process. No source code, texture or assembly is copied.
