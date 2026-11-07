## Why

Repeated baby-naming prompts interrupt play when the player is content with generated names.
Auto Name Babies provides that convenience, but its name detection and notification handling need
explicit safeguards so automation never replaces a chosen name or conceals a failed naming attempt.

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), **RimWorld 1.7 beta**.
Origin: replacement, **S044 / P1-35**, Auto Name Babies (`cruesoe.autonamebabies`, Workshop 3782676886).
This change **specifies future behavior only**; nothing is implemented or accepted in game.

## What Changes

- Automatically name confirmed unnamed player babies using the native name generator, including
  supported stillbirths and pending names encountered on load, without changing birth outcomes.
- Retain Father, Mother, Random parent and optional Ideology surname policies, with documented
  fallback order and no repeated random surname selection for an already prepared baby.
- Preserve birth/adoption information and ordinary notification navigation while removing required
  naming prompts only after successful automatic naming. Keep ordinary voluntary renaming available.
- Preserve intentional names, including “Baby”; correct language-sensitive detection, foreign-birth
  surname changes and notification changes based merely on the spelling of an existing name.
- Put surname policy in the common global/save settings UI. Keep names and naming history as pawn
  gameplay state, with safe load/reload behavior and no automatic source-configuration import.

## Capabilities

### New Capabilities

- `megamod-auto-name-babies`: Automatic baby names, parental surname rules, notifications, safe
  catch-up and common configuration.

### Modified Capabilities

None. S031 Toddlers continues to own development/care. Its toddlers retain native Baby-stage
eligibility for confirmed pending-name catch-up; chosen names and life stages remain unchanged.
Other owned naming/UI features must compose directly when specified.

## Impact

Future owning code: native birth/adoption/naming lifecycle and letter adapters, shared settings and
minimal per-pawn naming history. Core and **Harmony (`brrainz.harmony`)** are required for the proposed
implemented hooks. **Biotech is required for this feature**, but optional for the megamod as a whole:
without it, this feature is unavailable and unrelated features remain usable. Ideology is optional;
without it a stored Ideology surname policy temporarily uses Random parent. The original source is
optional and suppresses the overlapping owned feature if co-installed. No XML patch is planned;
any later XML Extensions use must add its required dependency and guarded merge-preserving checks.
No source, private Harmony or Dev Gateway assembly may be packaged.

[Research](../../../mods/megamod/docs/refinement/2026-11-03-auto-name-babies-research.md) records exact
installed/native evidence, source defects and remaining work. [Coverage](../../../mods/megamod/docs/replacements/auto-name-babies.md)
distinguishes the draft from implementation and makes no binary/API or source-save migration claim.
