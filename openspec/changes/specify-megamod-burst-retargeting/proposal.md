## Why

A weapon should be able to use its remaining burst shots against another valid threat after its
automatic pawn target goes down, while keeping explicit fire orders and weapon restrictions intact.
S081 selects Spray Transfer's purpose and corrects its broad, unguarded target replacement.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**; origin replacement **S081/P2-10**. This is specification only.

## What Changes

- Redirect eligible scheduled shots of an existing automatic ranged burst after its pawn target
  becomes downed/dead, choosing another native-valid threat from the shooter's current position.
- Retain Fire at will/Hold fire, explicit pawn and turret targets, hunting targets, range, line of
  fire, special weapon/turret restrictions, native shot cost/cadence/accuracy and burst completion.
- Add no new settings or UI controls; use existing entity orders and native attack-target data.
- Record source behavior, intentional restrictions/corrections, compatible external weapon boundaries,
  source co-installation policy and semantic target admission before any implementation.

## Capabilities

### New Capabilities

- `megamod-burst-retargeting`: remaining-burst target transfer, explicit-control precedence,
  current shooting eligibility, unchanged shot accounting and isolated compatibility failure.

### Modified Capabilities

None. Existing explicit hunting/finishing and native firing controls remain authoritative.

## Impact

Future combat feature-local code and narrowly guarded native shooting integration. Core and Harmony
are required when implemented; source/provider mods remain optional inspection/integration inputs.
No DLC, XML mutation, XML Extensions dependency, source assembly copy, public facade or cache is
currently justified. Any later XML operation needs the foundation's guards/merge contract and
XML Extensions dependency. No Gateway or other repository-mod runtime dependency/reference.

No code, build, installation or gameplay acceptance is delivered. Evidence and deferred decisions:
[research](../../../mods/megamod/docs/refinement/2026-11-04-spray-transfer-research.md).
