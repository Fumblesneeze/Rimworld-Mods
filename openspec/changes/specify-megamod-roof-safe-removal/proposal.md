## Why

Removing a wall, support or rock should clear endangered roofs before workers remove their support.
Smarter Deconstruction and Mining supplies this automation, but its settings, temporary roof orders,
interruption behavior and optional level handling need explicit, independently engineered contracts.

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S061/P1-55, Smarter Deconstruction and Mining (Continued),
`Mlie.SmarterDeconstructionAndMining`, Workshop 3261302741, originally by legodude17.
**Specification only; no gameplay delivered.**
[Research](../../../mods/megamod/docs/refinement/2026-11-04-smarter-deconstruction-research.md)
records the public documentation, installed/source agreement, native jobs and actual consumer.

## What Changes

- Specify roof preparation for mining, deconstruction and uninstalling roof supports, including
  connected roofs, concurrent removal orders and a fresh check before support disappears.
- Retain real roof-removal work, ordinary worker permissions and native mining/removal results;
  inaccessible required work must wait rather than silently count as completed preparation.
- Define mountain-roof cancellation, explicit forced-risk behavior, optional removal and research
  choices. Mountain removal starts off; no provider silently turns it on.
- Preserve temporary roof-area intent, player edits and one continuation across cancellation,
  overlapping work, map changes and save/load. Compose owned quality and replacement work directly.
- Put configuration in common global/save mod settings; keep actual area orders on the map.
- **BREAKING relative to source parity:** correct map-ambiguous cleanup, incomplete job restoration,
  missing-research permission, worker-option coupling and stale safety decisions. Do not reproduce
  the upstream automatic research-name heuristic or expose its internal algorithm as a facade.

## Capabilities

### New Capabilities

- `megamod-roof-safe-removal`: roof preparation, support removal, mountain policy, scoped roof orders,
  worker options, continuation and provider-aware fallback.

### Modified Capabilities

None in the archived baseline. This draft composes S008 mining plans, S013 quality cycles and S028
building replacement directly; their worker, quantity, cycle and physical-state contracts remain.
S060 construction access governs closing spaces, independently of this removal-stage roof check.

## Impact

Future runtime work belongs only to megamod. Harmony will be required when the guarded integration
is implemented. External roof/worker/level providers are optional and require separate admission;
neither source mod identity is a dependency. No other repository mod or Dev Gateway dependency,
XML mutation, new content recipe, selected raster asset or new bottom tab is involved.

No upstream assemblies/code are copied, no old settings/save migration is selected, and no universal
compatibility, multiplayer, safe-uninstall or performance claim is made. Every implementation and
native acceptance task remains open.
