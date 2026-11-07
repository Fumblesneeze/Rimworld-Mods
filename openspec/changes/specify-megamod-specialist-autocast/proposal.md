## Why

Useful specialist commands require repeated manual activation before relevant work. S051 selects
Auto-Cast Specialist Commands as a behavior reference; its exact trigger coverage, shared switches
and optional providers need an observable contract before independent implementation.

## What Changes

- Specify the four Ideology work commands and three optional provider commands, including precise
  work triggers, source defaults, current eligibility and unchanged ability effects/costs.
- Attempt once at the start of actual work, after travel and preparation, with normal casting and
  safe job continuation. This intentionally replaces the source's generic job-step timing heuristic.
- Keep automatic activation work-triggered; no permanent aura, periodic recasting or automatic role
  grants. Preserve native manual casting and gameplay restrictions.
- Move shared live switches out of pawn right-click input into one compact bottom-right control;
  separate current-game operational choices from configurable defaults in common mod settings.
- Specify absent-safe optional content, source co-installation suppression, startup admission,
  persistence and direct integration with owned bill/work features.

## Capabilities

### New Capabilities

- `megamod-specialist-autocast`: Work-triggered specialist commands, controls, persistence and safe
  optional integrations.

### Modified Capabilities

None. Existing foundation settings, UI placement and patch admission remain authoritative.

## Impact

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. This change delivers documentation only; all runtime work is future.

Future runtime needs Core and Harmony (`brrainz.harmony`) when patches are implemented. Ideology
(`Ludeon.RimWorld.Ideology`) is required for this feature, optional for the whole megamod; absent
Ideology leaves it unavailable. Cooking Specialist (`SierraOnline.AdvCannibal`, identity still to
verify against provider metadata), Void Specialist (`SierraOnline.VoidSpecialist`, same limitation),
Anomaly, Vanilla Ideology Expanded (`VanillaExpanded.VMemesE`, with its Royalty-gated Majordomo),
Royalty and Research Reinvented (`PeteTimesSix.ResearchReinvented`) are optional, with narrow guarded
branches. This does not implement or require those providers' content. Vanilla Expanded Framework
is not needed merely for an icon. No XML changes or XML Extensions dependency arise here.

No source package identity, settings format, assemblies or artwork are copied. No repository mod
dependency or Dev Gateway product dependency is introduced. See the [research record](../../../mods/megamod/docs/refinement/2026-11-03-specialist-autocast-research.md)
and [replacement report](../../../mods/megamod/docs/replacements/specialist-autocast.md).
