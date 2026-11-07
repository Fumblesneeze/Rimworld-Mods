## Why

Ordinary social fights can select sharp natural attacks such as claws and bites, causing injuries
the player did not intend as part of a brawl. Preserve the source's restraint rule and its Bloodlust
exception without making social fights harmless or changing normal combat.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld 1.7 beta**. S037 **[DN] Blunt Social Fights**, `dani.BluntSocialFighting`,
Workshop `2886174990`, is a read-only source reference, not an owner or runtime dependency.

## What Changes

- Specify exclusion of attacks with sharp primary damage while their attacker is actually social
  fighting, unless that attacker currently has an active Bloodlust trait.
- Preserve other attack restrictions, relative selection weights, ordinary blunt damage, existing
  wounds, fight initiation/recovery and behavior outside social fighting.
- Specify empty eligible-attack handling, current trait/state changes and save/load without stale
  permission. Do not replace a forbidden attack with a fabricated punch or rewrite its damage.
- Record the source's narrower behavior than its About description, no-settings/no-new-UI surface,
  primary-damage boundary, consumers and semantic compatibility requirements.

This is **specification only**. No gameplay code, tests, live acceptance or beta compatibility is
delivered. The installed/published reference supports through 1.6; native 1.7 code was inspected as
design evidence rather than assuming that binary is a working beta port.

## Capabilities

### New Capabilities

- `megamod-blunt-social-fights`: restrained attack selection during social fights, per-attacker
  Bloodlust exception, preserved combat consequences, lifecycle and compatibility fallback.

### Modified Capabilities

None. Existing owned medical, social and combat features retain their separate contracts.

## Impact

- Future narrow combat-availability integration, pure eligibility decision and guarded startup.
  Harmony is required when eagerly used. No source binary, DLC or optional combat provider becomes
  required solely for this behavior; Core supplies the relevant trait, state and damage categories.
- No new settings, bottom tab, debug feature, content Def, save component or shared Thing cache.
  No XML patch is proposed; any later declarative patch follows the repository's XML Extensions
  dependency/content/merge rules.
- Preserve practical consumed contracts only when evidence establishes them. No source package or
  assembly identity emulation, automatic migration, foreign unpatching or internal self-patches.
- [Research](../../../mods/megamod/docs/refinement/2026-11-03-blunt-social-fights-research.md)
  and [coverage](../../../mods/megamod/docs/replacements/blunt-social-fights.md) retain exact
  source/native evidence, deliberate boundaries and remaining work.
