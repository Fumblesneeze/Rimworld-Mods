## Why

Automatic miners should prefer scarce resources and finish useful excavation without wasting
deposits through avoidably low yield. S097 makes those choices explicit for surface mining and
deep drilling while preserving player orders and the game's actual extraction mechanics.

## Owner

Exactly one owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`).
Target **RimWorld 1.7 beta**. S097/P2-26, Mining Priority (Continued). **Specification only.**

## What Changes

- Order eligible automatic mining targets by resource rarity, with optional preference for partly
  excavated targets; order deep drills by their next resource and optional proximity to exhaustion.
- Keep rarity distinct from selling price, preserve every priority level without weighted-score
  crossover, and consider the drill's complete supported extraction footprint.
- Optionally reserve yield-sensitive extraction for workers meeting a configured fraction of the
  best suitable worker's actual Mining Yield. Compare mining and drilling separately.
- Keep an explicit busy-worker option, meaningful availability checks and current explanations;
  eliminate stale cross-map, cross-job and caller-dependent benchmark reuse.
- Preserve ordinary work assignments, jobs already running, explicit player-forced work, mining
  plans, costs, production, power, danger and access rules.
- Move source preferences through common global/save settings, without exposing cache timing as a
  player responsibility. Document unsupported provider contracts and source coexistence.

## Capabilities

### New Capabilities

- `megamod-mining-priorities`: automatic target ordering, yield-based worker eligibility, busy-worker
  policy, contextual explanations, configuration and compatibility fallback.

### Modified Capabilities

None. Compose with existing mining plans, shared worker facts and work selection directly.

## Impact

Future native mining/deep-drill work selection and contextual explanations. Core and external
Harmony are required when implemented. Original/continued Mining Priority is a coexistence input,
not a dependency; custom mining/drill providers are optional and independently admitted. No DLC,
new resource, XML patch, upstream assembly or Gateway dependency is required. Any later XML patch
requires XML Extensions (`imranfish.xmlextensions`, Workshop2574315206), guarded content checks and
merge preservation in that implementation slice. No foreign settings migration, runtime behavior
or in-game acceptance is delivered by this draft.
