## Why

Computer-controlled humanlike combatants should be able to use the charged equipment they wear,
including jumps and deployable packs, rather than carry it unused. S083 selects Enemies Use Apparel
Too's equipment, generation and optional combat-talent behavior while preserving real item costs
and player control.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**; origin replacement **S083/P2-12**. Specification only.

## What Changes

- Let eligible non-player humanlike combatants use supported worn offensive apparel, jump toward
  melee targets, reposition for ranged fire or retreat when injured and threatened.
- Preserve the real equipment's charges, range, warmup, projectile/effect, cooldown, restrictions
  and remaining loot. Keep one action rather than duplicate original and converted commands.
- Extend appropriate ordinary equipment pools with source-selected armor and packs while preserving
  existing content and generation restrictions. Keep use and generation independently configurable.
- Specify optional Ideology combat-talent behavior, including the actually wired tribal berserker
  grant; distinguish unconnected commander and explosion-escape prototypes from shipped behavior.
- Replace implementation-switch settings with clear behavior settings under the common global/save
  model. Keep effects and inspection on native combat/pawn surfaces, with no new bottom tab.

## Capabilities

### New Capabilities

- `megamod-enemy-apparel-use`: equipped-item use, tactical jumps, real charge/effect ownership,
  control authority and isolated compatibility fallback.
- `megamod-enemy-loadout-variation`: additive equipment candidates, generation probabilities,
  eligibility, future-generation timing and inherited settings.
- `megamod-enemy-combat-talents`: optional non-player Ideology combat use and tribal talent grants,
  defeat/recruitment behavior and explicit unwired-source boundaries.

### Modified Capabilities

None. Owned weather-ready generation, specialist work autocast and burst retargeting keep their
contracts and compose directly; this does not grant automatic apparel use to player-controlled pawns.

## Impact

Future combat/generation feature code, semantic-guarded Harmony bridges and additive XML registrations.
Core and Harmony are required when implemented. XML Extensions (`imranfish.xmlextensions`,
Workshop2574315206) is required with the planned guarded XML. Royalty, Odyssey, Anomaly and Ideology
are optional for the whole mod and required only for their own content/features. Anomaly content is
not installed in the inspected game; source catalog coverage is not verified beta compatibility.
No source binary/assets, fake source package, new tradeable implant content, Gateway or another
repository-mod runtime dependency is introduced.

No gameplay implementation or acceptance is delivered. Source identity, discrepancies and open
questions are retained in [research](../../../mods/megamod/docs/refinement/2026-11-04-enemy-apparel-research.md).
