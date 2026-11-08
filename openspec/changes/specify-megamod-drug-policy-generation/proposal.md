## Why

Creating a drug policy from a large current drug list is repetitive, and a policy called safe can
conceal unsupported assumptions. S139 keeps the useful generation idea while making suggestions,
limits and medical permissions visible before the player creates a new policy.

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target: **RimWorld 1.7 beta**. Specification and research plan only; no gameplay behavior is delivered.

## What Changes

- Add Generate to the native drug-policy window, opening an editable preview with reasons and an
  explicit Create new policy action. Never overwrite, assign, make default or regenerate on load.
- Use the current S029 catalog and its applicable native defaults, including visible addiction/medical
  permission. Preserve every existing policy and actual consumption history.
- Present risk-reduction suggestions, not universal safety. Unknown or unsupported pharmacology
  receives no autonomous schedule and a clear reason. Unestablished calculations remain explicit
  implementation-blocking research tasks; this draft is not numerically ready for implementation.
- Give reset operations full, deterministic semantics; reject stale previews and restore all owned
  changes if creation fails. Repeated delivery of one confirmation creates at most one new policy.
- Compose directly with S024, S124, S134 and S138. Use common inherited configuration for actual
  reusable generation preferences, while draft and created policy choices remain gameplay state.
- Retain only the bounded original-loaded warning and best-effort ordinary feature-off handling;
  original-mod APIs, imports and compatibility tests are outside scope.

## Capabilities

### New Capabilities

- `megamod-drug-policy-generation`: previewed explicit creation, honest risk suggestions, current
  content validation, failure atomicity, policy-state preservation and generation preferences.

### Modified Capabilities

None. Consume existing owned policy and medical contracts without editing their change directories.

## Impact

Future work belongs to the megamod policy UI/domain, S029 publication boundary and common settings.
Core is required; Harmony (`brrainz.harmony`) is required for the planned native policy-window hook
and is already a product dependency. No XML Extensions operation is planned. Drug DLC/content is
optional and classified from actual available capabilities. No source generator/tolerance mod,
Prepatcher, other repository product or Dev Gateway runtime dependency is introduced.

Idea source: SeoHyeon's Safe Drug Policy Generator, `seohyeon.bestdrugpolicy`, Workshop 2582640021.
[Research](../../../mods/megamod/docs/refinement/2026-11-08-safe-drug-policy-generator-research.md)
records pinned source/DLL behavior and static weaknesses separately from the user's unverified
historical experience. No source algorithm, Beer shortcut, implied global template or numeric safety
promise is adopted. Independent design, calculation research, focused TDD, review, native workflow
acceptance and balance review remain mandatory.
