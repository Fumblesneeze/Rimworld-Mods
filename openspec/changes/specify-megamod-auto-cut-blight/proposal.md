## Why

New blight should not require repeated manual designation as it spreads. Auto-Cut Blight also gives
this cutting work priority within Plant Cutting; S069 specifies both functions against native 1.7
scheduling, with ordinary orders and explicit cancellation/scope boundaries.

## What Changes

- Automatically place an ordinary cut order on a newly blighted eligible revealed plant while
  enabled, without replacing existing orders or clearing healthy neighboring crops. Existing tree
  extraction takes precedence over adding or prioritizing a competing cut.
- Prioritize currently blighted plants with cut orders within Plant Cutting, using native emergency
  eligibility and real plant-cutting work rather than forcing every pawn to abandon its current task.
- Preserve event-driven scope: loading/enabling does not scan and designate an existing outbreak.
  Native/S008 manual blight selection remains available and joins the same priority policy.
- Propose one initially enabled bottom-right behavior control, backed by common global/save settings.
  Turning it off leaves existing ordinary orders and work intact but stops new owned automation/priority.
- Correct unguarded/repeated designation, fog/protected-plant and pawn-eligibility bypasses; use native
  cut orders/jobs instead of source-specific saved designations and job classes.

Documentation only: no implementation, package changes or gameplay acceptance are delivered.

## Capabilities

### New Capabilities

- `megamod-auto-cut-blight`: New-infection cut designation, native Plant Cutting priority, ordinary
  cancellation/work, settings/UI placement and compatible source/manual-provider composition.

### Modified Capabilities

None. S008 manual orders and S006 linked growing zones retain their own contracts.

## Impact

Sole owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`, **1.7 beta**.
Source: **Auto-Cut Blight - 1.6**, `Defi.AutoCutBlight`, Workshop `3520167264`, assembly 1.1.1.0.
[Research](../../../mods/megamod/docs/refinement/2026-11-04-auto-cut-blight-research.md) records complete
source-binary/XML analysis, current native scheduling, deviations, consumers and open questions.

Harmony is **required** when implementing the event bridge. XML Extensions (`imranfish.xmlextensions`,
Workshop `2574315206`) is **required** for the planned guarded work-provider XML contribution; its
actual 1.7 loading remains to be proved. Original source and Smart Farming are optional, never
bundled. Original-source provider selection prevents a duplicate implementation; no DLC, other
repository product or Dev Gateway runtime dependency. No source-save migration or safe-uninstall
claim is made from using ordinary jobs alone.
