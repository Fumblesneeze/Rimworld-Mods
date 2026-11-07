## Why

Hunters should be able to wait at a chosen spot or raised seat, notice selected game entering the
surrounding area, then leave to hunt it. This provides the user's intended hunting routine without
requiring each individual animal to be manually marked or turning a seat into an automatic turret.

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
**Origin: original**, S064/P1-58–59. There is no selected upstream hunting-station mod to reproduce.
**Specification only; no gameplay delivered.** User decisions are in the
[refinement notes](../../../mods/megamod/docs/refinement/2026-11-01-handwritten-notes.md);
native evidence and proposed choices are separated in
[research](../../../mods/megamod/docs/refinement/2026-11-04-hunting-stations-research.md).

## What Changes

- Specify a free hunting spot and a constructed raised hunting seat. Both provide one physical
  waiting position; the seat supplies ordinary comfort, not extra weapon range or attack bonuses.
- Give each station its own storage-style species filter, grouped by predator/non-predator and
  small/big game. Include species supported by that map's wildlife/arrival rules even when absent.
- Detect eligible prey within an initial configurable **40-cell radius**, without line of sight
  from the station. Preserve fog privacy and ordinary access, weapon, shooting and hunting rules.
- Have a hunter physically reach the station, wait, claim one prey, leave the waiting position,
  hunt and handle its actual corpse, then reconsider returning through normal work priorities.
- Define multiple-station coordination, manual-order precedence, cancellation, changed permissions,
  save/load and unavailable-provider behavior before implementation.
- Keep station choices in contextual gameplay controls and tuning in common inherited global/save
  settings. Compose future bonded companions and wandering through direct owned policies.

Specific unconfirmed choices are marked as proposed defaults in the spec/research and tracked in
HS-Q1–5: construction balance, adult-size cutoff, catalog scope, selection, interruption and tuning.
They are not additional interview answers. Hunt for Me remains rejected; no source behavior is
invented for it. S065 owns bonded finishing/retrieval and S066 owns directional animal wandering.

## Capabilities

### New Capabilities

- `megamod-hunting-stations`: station placement, species selection, waiting/detection, ordinary
  hunting, coordination, lifecycle, tuning and explained compatibility fallback.

### Modified Capabilities

None in the archived baseline. S008 drafted party hunting remains separate with its own visibility,
weapon-range and finishing rules. S021 hauling and S049 storage admission retain actual quantities
and capacity. The shared map-data and UI/settings foundation remains authoritative.

## Impact

Future Defs, jobs, gameplay state, native integration, localization and assets belong only to
megamod. The functional spot/seat serves this mechanic; no unrelated content pack, new hunting work
column or bottom main tab is proposed. Ordinary manual hunting remains usable without a station.

Harmony becomes required when guarded runtime hooks are implemented. DLC and third-party wildlife,
weapon and map providers are optional; only admitted paths are supported. New owned Defs are
declarative. Any actual XML patch uses XML Extensions presence/shape checks and merge rules, with
XML Extensions required at its first use. No other repository mod, experimental pathfinder or Dev
Gateway is a product dependency. No assets are selected or created in this documentation stage.
All implementation, focused TDD, independent review, balance and final native acceptance remain open.


Initial station filters select map-possible species with exactly0% retaliation chance (decision17), while preserving subsequent per-station choices.
