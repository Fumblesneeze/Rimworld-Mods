## Why

A rare healer serum should treat the condition the player intended, rather than leave that decision
to the game's worst-condition ranking. S089 preserves an ordinary paid treatment while letting the
player select its eligible target and safely cancel before spending the item.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S089/P2-18, Healer Mech Serum Choice. Specification only.

## What Changes

- After normal serum use or medical administration, offer an explicit condition choice for the
  recipient, including eligible animals; consume one item only for the confirmed treatment.
- Preserve ordinary cure semantics, including restoration of a missing limb and conditions whose
  native cure removes all instances together. Explain those consequences before confirmation.
- Correct stale choices, duplicate confirmations, unavailable items and cancellation handling.
- Keep supported optional healing items and automatic external healing separate. Preserve proven
  native component contracts where possible rather than repeating the source's broad shaman patch.
- Use a contextual treatment window, with no new bottom tab, settings menu or cheat action.

## Capabilities

### New Capabilities

- `megamod-targeted-healing-items`: eligible condition selection, confirmation and physical cost,
  cancellation/lifecycle, native use/administration and scoped provider compatibility.

### Modified Capabilities

None. Drug Response, Priority Treatment and Smart Medicine compose scheduling and medical policy
directly when specified/implemented; none gains permission to choose or spend rare treatment items.

## Impact

Future owned usable/effect/recipe adapters and guarded XML changes. Core and XML Extensions
(`imranfish.xmlextensions`, Workshop2574315206) are required when those changes are implemented;
external Harmony becomes required with the first Harmony hook, if one is needed. No DLC is required.
Optional original provider, healing-item providers, More Faction Interaction and Multiplayer remain
optional. Their presence does not establish support; individual admitted contracts govern activation.

No new item, recipe cost, healing power, settings framework, private cache, source assembly reupload,
package spoof or save migration. Multiplayer's existing HMSChoice adapter will not bind to our
package; multiplayer choice behavior remains inactive until its own supported synchronization
contract exists. [Research](../../../mods/megamod/docs/refinement/2026-11-05-healer-serum-choice-research.md)
records source evidence, actual consumers, deviations and HMSC-Q1–5. No runtime acceptance claimed.
