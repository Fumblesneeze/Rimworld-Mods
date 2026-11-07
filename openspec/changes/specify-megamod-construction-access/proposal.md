## Why

Automatic construction should avoid closing small spaces around colonists or unfinished work, and
builders should finish a wall from an accessible side. Smarter Construction provides that intent,
but its local search limits, forced-order bypass, recovery and interactions need explicit contracts.

Owner **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; target **1.7 beta**.
Origin replacement S060/P1-54, Smarter Construction, `dhultgren.smarterconstruction`, Workshop
2202185773. **Specification only; no gameplay delivered.**
[Research](../../../mods/megamod/docs/refinement/2026-11-04-smarter-construction-research.md)
records the current public source, installed 1.7 binary, native paths and actual external consumers.

## What Changes

- Specify bounded small-enclosure checks before automatic completion, protection for player pawns
  and unfinished blueprints/frames, and real movement to a valid outside work position.
- Retain explicit forced construction as a bypass of this feature's safeguards, without bypassing
  ordinary permissions, materials or worker requirements.
- Prefer locally constrained blocking frames within 15 cells, composed inside S058's construction
  categories; retain the source's less-than-100-cell enclosure scope without claiming general safety.
- Preserve physical materials, earned work and ordinary completion consequences through waiting,
  relocation, interrupted movement and save/load, with bounded recovery instead of repeated retries.
- Compose owned quality/replacement construction directly and document unsupported installation,
  provider geometry and source co-installation. Share current map facts without duplicating caches.
- **BREAKING relative to source parity:** validate the builder's actual route and permissions before
  relocation, prevent stale cross-map/worker decisions, avoid full-work retry farming, and distinguish
  legitimate transit/waiting from a stalled construction journey. No internal implementation facade
  or package impersonation is selected for consumers that patch the old algorithm.

## Capabilities

### New Capabilities

- `megamod-construction-access`: small-enclosure protection, local construction order, safe-side
  completion, forced actions, physical recovery and provider-aware fallback.

### Modified Capabilities

None in the archived baseline. S058's in-progress automatic-work draft receives a narrow construction
tie-order clarification. S013 and S028 retain their existing costs, worker choices and replacement
state while using this common completion decision where applicable.

## Impact

Future runtime changes belong only to megamod's construction selection, completion and movement
paths. Harmony will be required when implemented. External construction/robot, vehicle and level
providers are optional and require exact admission; Smarter Construction itself is not a dependency.
No other repository mod or Dev Gateway dependency, XML mutation or selected raster asset is involved.

The source has no player settings interface or persistent custom gameplay records. This draft adds
no settings window or bottom tab. Numeric policy limits are explicit and must be named constants;
possible user controls remain recorded for later refinement. No source code, assemblies or artwork
are copied, and no broad save-removal, multiplayer, performance or external drop-in claim is made.
