## Why

High-quality or unusually protective armor should keep the rating its ordinary stats produce instead
of losing everything above the shared200% ceiling. Specify UnLimitedArmor's small change precisely,
including natural armor, penetration and the separate Gear summary, without inventing a combat system.

## Owner

Exactly one owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`.
Target **RimWorld1.7 beta**. S075 is source provenance, not another owner.

## What Changes

- Remove the inherited shared upper ceiling for ordinary Sharp, Blunt and Heat armor ratings,
  preserving quality, material, lower bounds and other stat calculations.
- Apply that same stat policy to apparel and natural armor, including non-colonists. Compatible
  added armor content follows the same shared stat contract; separately declared limits remain separate.
- Preserve the active armor system's hit coverage, layer order, penetration, wear and damage behavior.
  An individual rating over200% is not a promise of invulnerability or a new overall-protection metric.
- Keep normal item/stat inspection and existing consumers. Preserve the native capped Gear summary
  as a different metric, with future inventory UI composition required to retain that distinction.
- Replace the source's silent unconditional XML removal with minimal content/shape admission,
  idempotence, explicit provider authority and bounded startup diagnostics.

This is a **specification-only draft**. No gameplay, dependency installation, compatibility or balance
acceptance is delivered. No private settings UI, new armor, runtime toggle or arbitrary higher cap is added.

## Capabilities

### New Capabilities

- `megamod-uncapped-armor`: inherited armor ceilings, preserved combat/stat presentation, provider
  boundaries and guarded fallback.

### Modified Capabilities

None. Existing Normal-quality comparison and information navigation retain their own meanings.

## Impact

- Source **Paragon.Hanul.UnLimitedArmor**, Workshop1695493009. Complete XML and current native chain,
  hashes, source limits and comparators are in
  [research](../../../mods/megamod/docs/refinement/2026-11-04-unlimited-armor-research.md).
- Future **required** XML Extensions (`imranfish.xmlextensions`, Workshop2574315206) for guarded
  declarative mutation, with its required Harmony dependency. No owned Harmony patch is selected.
  Exact dependency1.7 loading remains preflight; do not bundle either library.
- Source provider, added content, Royalty/other DLC and combat providers are **optional**.
  Original-provider presence suppresses the owned contribution. Combat Extended is outside this
  feature's supported armor model and suppresses it without disabling unrelated megamod features.
  Yayo's ordinary stat consumption is inspected, but its current effective behavior remains unverified.
- Reuse native stats in S009/S010/S012, S074 and future S132 inventory UI; no independent stat cache,
  internal self-patching, source package facade or product dependency on another repository mod/Gateway.
