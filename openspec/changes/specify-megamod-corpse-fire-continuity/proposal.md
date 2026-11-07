## Why

A pawn dying should not extinguish its existing fire merely because the game moves it into a new
corpse. S091 restores the ordinary fire handoff intended by the death path, while preserving normal
fire placement, extinguishing and corpse handling.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S091/P2-20, Keep Burning, Corpse! Specification only.

## What Changes

- Preserve the dying pawn's existing fire strength and origin through corpse creation, and let the
  successful on-map corpse placement continue into the game's ordinary cell-fire rules once.
- Keep environmental restrictions, existing fires, failed/no corpse placement and off-map/container
  outcomes unchanged. No permanent or corpse-attached flame is introduced.
- Isolate simultaneous/nested deaths and failed death paths instead of copying the source's shared
  temporary Fire reference or broad attachment-call replacement.
- Compose with owned fire response: a fire extinguished before corpse creation stays extinguished.

## Capabilities

### New Capabilities

- `megamod-corpse-fire-continuity`: ordinary flame continuity at a placed corpse, bounded eligible
  death outcomes, normal subsequent fire behavior, independent state and guarded coexistence.

### Modified Capabilities

None. S079 Stop, Drop, And Roll! controls living-pawn response/ignition; S091 consumes the remaining
fire only. Anomaly's ordinary corpses/resurrection are optional content, not a new resurrection policy.

## Impact

Future narrow Harmony adaptation of native death/corpse fire data flow. Core and external Harmony
required on implementation; no DLC required. Anomaly and external fire/death providers optional.
No XML currently needed; any later XML operation requires guarded XML Extensions and its dependency
at first use. No new settings, UI tab, asset, private cache, item/stat rebalance or save migration.

[Research](../../../mods/megamod/docs/refinement/2026-11-05-corpse-fire-continuity-research.md)
records the source, current native holder-extinguishing chain, source hazards and KBC-Q1–3.
No foreign assembly reuse, package spoof or runtime acceptance is claimed.
