# Performance benchmarking

RimWorld Dev Gateway owns the repository's performance harness. Circinus is the canonical profiler for comparable local runs; Dubs Performance Analyzer is reserved for a separate, noncanonical diagnostic path. Neither profiler nor any performance-test assembly is a dependency of a distributable gameplay mod.

## Run the calibration

Circinus must be installed locally as package `astryl.Circinus`. The runner discovers marked fixture assemblies, validates their complete package order, builds and stages one test bundle, and launches one fresh minimized RimWorld process per benchmark and repetition:

```powershell
.\scripts\Invoke-RimWorldPerformanceTests.ps1 -DryRun -Output json
.\scripts\Invoke-RimWorldPerformanceTests.ps1 -CreateBaselineCandidate -Output json
.\scripts\Invoke-RimWorldPerformanceTests.ps1 -Output json
```

The default command runs only instrumented benchmarks and averages their ordinary stochastic repetitions. Use `-IncludeProfilerControls` only for the slower armed-disabled/disarmed overhead calibration, `-BenchmarkId <exact-id>` for a focused canary or explicit control, and `-GroupId <exact-group-id>` for one exact mod matrix. `-WarmUpTicks`, `-SampleTicks`, and `-Repetitions` are recorded overrides; changing the source defaults requires a new workload version. Do not use performance runs as ordinary correctness tests.

The launcher uses isolated savedata and exact mod lists, forces Circinus into local-only/manual operation, disables music, enables background execution and developer mode, and starts the game minimized. It does not upload runs. It verifies exact process cleanup and restores the user's normal `ModsConfig.xml`, `Prefs.xml`, and Circinus settings.

## Baseline review and regression policy

Ordinary runs compare against reviewed `performance/baselines/*.accepted.json` files using the tracked `performance/thresholds.json` policy. A run with no accepted baseline for one of its benchmark/lens cases fails closed; it does not silently bless its own result. To establish or replace a baseline, use `-CreateBaselineCandidate`. The candidate is written beneath the unique ignored run directory by default, never overwrites an existing path, and is rejected if the current snapshot contains runtime errors. Review it, rename its status from `candidate` to `accepted`, place it under `performance/baselines/<descriptive-name>.accepted.json`, and commit it in a separate reviewable change. Never copy a fresh result directly over an accepted file.

Every comparison identity pins the benchmark and workload version, exact ordered packages, game version, measured product assembly hash, compiled fixture identity/MVID/hash, Circinus assembly hash and document schema, declared profiling selectors/control mode, isolated Circinus settings and sampling policy, hardware/OS fingerprint, warm-up/sample ticks, game speed, repetition count, and aggregation policy. The game is deliberately allowed to behave normally: the runner does not seed global randomness, reset ID counters, or require identical pawn jobs, positions, needs, inventories, or state hashes between repetitions. Repetitions are collapsed only after the actual benchmark configuration matches, retaining each raw value plus count, mean, minimum, maximum, and sample standard deviation per exact scope/selector/metric/unit/denominator/claim. Baseline and current metric sets must match in both directions. A missing or newly materialized metric, changed unit/denominator/claim, runtime error, Circinus incomplete/truncated result, workload/policy/package drift, or exceeded tracked threshold fails the run; healthy gameplay variation does not. If no reviewed threshold applies, the compatible historical delta is retained as informational rather than turned into a synthetic failure.

Each optional threshold names one benchmark, evidence lens, scope, exact selector, metric, and unit. When both absolute and relative increases are configured, either independent ceiling can fail. Before a profiled metric is thresholded, its recorded context must satisfy Circinus' native bounds: complete positive call/timed/cycle/window context, duty in `(0,100]`, adaptive shift `0..8`, and profiler-window ticks equal to the declared sample window. Naturally varying valid call counts, duty, cycles, shift, and wall time are preserved and reported on both sides; they are not required to be byte-identical. Prefer denominator-normalized metrics such as `ms/call`, `ms/cycle`, or window share over raw totals.

For per-call metrics, a repetition with no calls is retained explicitly as an unobserved value with weight zero. The aggregate uses total measured time divided by total calls across repetitions; it never invents `0 ms/call`. Min/max/sample deviation describe only repetitions that actually observed calls, while the raw null/value vector and call weights retain every run. Every method and patch also emits `gross-ms-per-estimated-call-observation-rate` with one `0` or `1` per repetition, so an entirely uncalled selector remains visible instead of disappearing from the report.

Reports retain passing and failing threshold evaluations with baseline/current values, absolute and relative deltas, configured ceilings, exact method identity, calls, timed calls, duty, shift, cycles, and profiler window when Circinus provides them. Mod-level metrics retain window context; method and patch metrics additionally require their sampled-call sidecars. `-InformationalCrossVersion` permits only game/product/test/Circinus/schema/hardware identity differences to be reported without thresholds; the command and aggregate status are `informational`, not `passed`. Numeric informational deltas require the same scope, unit, denominator, and claim; semantic drift is labeled and has no fabricated delta. Workload, package, timing, repetition, aggregation, or policy drift remains a failure in that mode.

Useful focused forms are:

```powershell
.\scripts\Invoke-RimWorldPerformanceTests.ps1 `
  -BenchmarkId gateway.circinus-calibration.instrumented `
  -CreateBaselineCandidate -Output json

.\scripts\Invoke-RimWorldPerformanceTests.ps1 `
  -BaselineDirectory .\performance\baselines `
  -BaselinePolicyPath .\performance\thresholds.json `
  -Output json

.\scripts\Invoke-RimWorldPerformanceTests.ps1 `
  -InformationalCrossVersion -Output json
```

## Evidence lenses and claims

An ordinary historical benchmark runs the instrumented workload only, with at least three fresh repetitions. When profiler overhead itself needs investigation, an optional calibration family runs these three compatible lenses:

- `instrumented`: Circinus wrappers are armed and timing is enabled;
- `armed-disabled-wrapper`: the same wrappers remain attached but timing and recording are disabled;
- `fully-disarmed`: run-owned wrappers are removed while Circinus' recorder/sample collection remains present.

Compute the controls only within one compatible workload family:

- instrumented minus armed-disabled estimates active timing/sampling overhead;
- armed-disabled minus fully disarmed estimates wrapper overhead;
- instrumented minus fully disarmed estimates total method-instrumentation overhead.

The fully disarmed lens is not analyzer-absent. Circinus is still loaded and its recorder/sample collection is common to all three processes. These live-game differences are noisy control observations, not causal product costs or statistical rankings. Circinus method/patch/mod rows are gross attribution. A product-absent workload can be run explicitly as a separate stochastic diagnostic, but the runner does not pair unrelated random repetitions or manufacture a per-run net distribution. Compare their independently averaged reports side by side; use a statistically designed analysis if a formal net estimate is later needed.

Whole-assembly selection is a diagnostic opt-in. Prefer exact Harmony owners, methods, types, tick overrides, and curated targets because broad instrumentation can dominate the workload being measured.

## Calibration accepted on 2026-08-11

Run `20260811T202256042Z` used RimWorld `1.6.4871 rev591` and exact order `brrainz.harmony`, `ludeon.rimworld`, `astryl.circinus`, `fumblesneeze.rimworlddevgateway`. Gateway DLL SHA-256 was `B8200CA68C95D2116D39BD9D638B1B450EBE45037F96B4B33E4CDBA33D47E5FF`.

The historical `gateway-calibration/v4` fixture normalized its disposable empty-map geometry and climate because it measured profiler mechanics, not gameplay. The current runner does not apply a global RimWorld random seed or reset engine ID state, including in calibration fixtures.

The calibration then attached 32,768 identical no-op `MapComponent` instances. RimWorld—not fixture code—called their override through its ordinary `MapComponentTick` loop during 300 superfast warm-up ticks and 3,000 measured ticks. This deliberately dense synthetic calibration forces disabled wrappers and active timing above RimWorld's superfast frame/tick floor; it is not a gameplay workload or a product-cost estimate. Screenshots were captured before and after, outside the measured window.

The three final frames were personally inspected and showed the empty, roofless soil calibration map and expected UI. No developer-log window was open. The retained logs contain no unexpected error/warning; the expected Gateway unrestricted-execution warning remains part of dev-only startup. Every process cleaned its temporary components, exact stage, process, credentials, and isolated configuration.

| Lens | Wall time for 3,000 ticks | Circinus recorded cycles | Profiler window | Samples | Run-owned sidecars |
| --- | ---: | ---: | ---: | ---: | ---: |
| Instrumented | 17,530.8828 ms | 104 | 4,637.385726 ms / 3,000 ticks | 17 | 3 |
| Armed, timing disabled | 16,563.9568 ms | 0 | 0 ms / 0 ticks | 16 | 3 |
| Fully disarmed | 4,343.7530 ms | 0 | 0 ms / 0 ticks | 4 | 0 |

The one-observation control differences were:

- active timing/sampling: `+966.9260 ms` (`+5.8375%`);
- wrapper: `+12,220.2038 ms` (`+281.3282%`);
- total method instrumentation: `+13,187.1298 ms` (`+303.5884%`).

The instrumented lens observed a `30.232558%` profiler duty share. Its hot native method sidecar retained `24,150,016` observed calls, `520,832` timed calls, and a final adaptive shift of `1`, proving that adaptive timing was active instead of merely copying an inspected policy constant. Circinus' native policy remains 60 recorded frames followed by roughly 240 skipped frames, adaptive timing above 64 calls per recorded frame, maximum shift 8, a 2,000-recorded-frame ring, a 7,200-sample run ceiling, and stopped-document caps of 3,000 patch rows and 1,000 method rows. The short-run observed values do not replace or redefine those native limits.

One observation per lens is sufficient to calibrate and prove the control paths, but insufficient for a historical baseline. The repository now has immutable reviewed-candidate and explicit per-metric threshold machinery; an accepted historical baseline still requires repeated compatible samples and human review.

Raw and normalized evidence is generated under ignored `artifacts/PerformanceRuns/<run-id>/`; it is not committed as product content.
