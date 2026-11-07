## Why

A burning colonist should be able to concentrate on extinguishing the fire instead of running out
of cover, and heat-resistant clothing should help prevent ignition. S079 Stop, Drop, And Roll! [BAL]
provides these two independently configurable behaviors.

## What Changes

- Specify stationary native self-extinguishing, enabled for colonists by default, with independent
  options for other humanlikes and animals.
- Specify apparel-based ignition resistance for all three groups by default, retaining source
  strength4, threshold25% and independent checks for the clothing covering a sampled body part.
- Keep direct burn damage, native fire/environment behavior and inability to act separate from
  ignition prevention. Retain the native150-tick self-extinguishing action rather than instant relief.
- Explicitly propose correcting source body sampling to include legs and use ordinary flame-hit
  weighting while keeping the documented hands/feet exemption. Restrict effects to configured groups,
  handle unsupported anatomy safely and avoid altering ineligible/already-burning attachment calls.
- Move the source's global options into common global/save inheritance, with validated values and
  clear scope. No bottom tab, pawn toggle, new rolling animation or source save import.

## Capabilities

### New Capabilities

- `megamod-fire-response`: In-place self-extinguishing and configurable apparel ignition resistance,
  with safe anatomy, fire lifecycle and provider behavior.

### Modified Capabilities

None.

## Impact

Sole owner **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **1.7 beta**.
This is a future specification; no runtime behavior is delivered or accepted.

Reference: **Stop, Drop, And Roll! [BAL]**, `balistafreak.StopDropAndRoll`, Workshop2362707956,
by balistafreak with Taranchuk coding credit. It is optional research input; when active it owns
both behaviors and our contributions remain inactive to avoid double checks/job overrides.

Harmony is required at implementation. No XML mutation, additional XML Extensions use or DLC
dependency is selected. Optional race/apparel/combat providers need actual package/shape support;
Combat Extended compatibility is not presumed. Gateway remains developer-only. No foreign package,
assembly, settings type or source asset is impersonated or shipped.

[Research](../../../mods/megamod/docs/refinement/2026-11-04-stop-drop-roll-research.md) distinguishes
documentation, selected binary and current native facts; [coverage](../../../mods/megamod/docs/replacements/stop-drop-roll.md)
will track the intentional body-sampling differences, retained contracts and future verification.
