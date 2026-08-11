## Why

Correct behavior is insufficient if a mod silently makes RimWorld's hot tick, job-search, rendering, or Harmony paths expensive. A repeatable benchmark layer is needed to detect regressions across repository history, game updates, and exact optional-mod combinations without manually configuring an interactive profiler.

## What Changes

- Add a reusable performance-runner capability built on the E2E process-grouping and isolated-launch model.
- Integrate with the locally installed Circinus package `astryl.Circinus` through an exact package/assembly/schema/shape guard and no hard product-mod dependency.
- Dynamically register the owning repository mods' Harmony patches, tick/update methods, world/map components, work givers, job givers, and explicitly declared hotspots with the analyzer for the duration of a benchmark.
- Collect Circinus' raw timestamped run document, per-mod and per-patch costs, registered-method metrics, relevant curated tick/update/work/pathing targets, and TPS/FPS/frame/tick/heap samples together with the exact warm-up and sample windows.
- Add one substantial generic Immersive Chefs benchmark colony with many colonists and animals, rooms and non-trivial paths, cooking/dishwashing/prep/support work, patients and nurses, visitors, and caravans.
- Run the benchmark under exact selectable optional-mod matrices, retain raw and normalized metrics plus exact game/mod/assembly/build identities, and emit stable JSON/table/CSV reports suitable for historical comparison.
- Add baseline comparison with explicit absolute/relative regression thresholds, but keep first-run baselines opt-in and never silently rewrite accepted history.
- Generalize scenario and metric registration so future repository mods can contribute benchmark fixtures without modifying the Gateway core.
- Keep a bounded automated Dubs Performance Analyzer command outside canonical benchmark groups for separate internal-call or transpiled-IL diagnosis after Circinus identifies a regression.
- Specify this performance capability now and implement it after the E2E harness it depends on is operational; it does not alter shipping gameplay behavior.

## Affected Mods

- **RimWorld Dev Gateway** — package ID `fumblesneeze.rimworlddevgateway`; repository path `mods/RimWorldDevGateway`; owning mod.

## Capabilities

### New Capabilities

- `dev-gateway-performance-benchmarking`: Circinus automation, reusable benchmark fixtures, exact mod matrices, metric capture, stable reports, and historical regression comparison.

### Modified Capabilities

None.

## Impact

This adds test-only performance contracts and fixtures, guarded reflection adapters for the installed Circinus recorder/profiler and the separate DPA diagnostic lifecycle, host orchestration/reporting, and ignored benchmark artifacts. Circinus remains a test-run dependency only and is never referenced by or copied into a shipping gameplay mod. Dubs Performance Analyzer is not part of the canonical performance matrix or baseline identity.
